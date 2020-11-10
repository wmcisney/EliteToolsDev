using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNet.SignalR;
using NHibernate;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Downloads;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Accessors {

	public class FileNotification {
		public bool Notify { get; set; }
		public string ConnectionId { get; set; }

		[Obsolete("do not use")]
		public FileNotification() {
		}

		private FileNotification(bool notify, string connectionId) {
			Notify = notify;
			ConnectionId = connectionId;
		}

		public static FileNotification DoNotNotify() {
			return new FileNotification(false, null);
		}

		public static FileNotification NotifyCaller() {
			return new FileNotification(true, null);
		}
		public static FileNotification NotifyCaller(string connectionId) {
			return new FileNotification(true, connectionId);
		}
		public static FileNotification NotifyCaller(UserOrganizationModel caller) {
			return new FileNotification(true, caller._ConnectionId);
		}
	}


	public class FileAccessor {

		//private static object DOC_REPO_SETTINGS = Config.GetDocumentRepositorySettings();
		//private static string BUCKET = "radial-documentrepo";
		//private static string DOCUMENTREPO_PATH = "DocumentRepo";

		#region Getters
		public class TinyFile {
			public long Id { get; set; }
			public string Name { get; set; }
			public string Description { get; set; }
			public long Size { get; set; }
			public DateTime CreateTime { get; set; }
		}
		public static List<TinyFile> GetVisibleFiles(UserOrganizationModel caller, long forUserId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.Self(forUserId);
					var filePerms = perms.GetAllPermItemsForUser(PermItem.ResourceType.File, forUserId);

					var files = s.QueryOver<EncryptedFileModel>().Where(x => x.DeleteTime == null)
						.WhereRestrictionOn(x => x.Id)
						.IsIn(filePerms.Select(x => x.ResId).ToArray()).List();

					return files.Select(x => new TinyFile() {
						Id = x.Id,
						CreateTime = x.CreateTime,
						Description = x.FileDescription,
						Name = x.FileName,
						Size = x.Size,
					}).ToList();
				}
			}
		}

		public static async Task<string> GetFileUrl(UserOrganizationModel caller, long fileId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.CanViewFile(fileId);
					var file = s.Get<EncryptedFileModel>(fileId);
					return GeneratePreSignedURL_Unsafe(file.FilePath, file.FileName, file.FileType);
				}
			}
		}
		#endregion

		#region Local only
		public class LocalFile {
			public string FileType { get; set; }
			public byte[] Bytes { get; set; }
			public string FileName { get; set; }
		}

		private static Dictionary<string, LocalFile> _LocalUploads = new Dictionary<string, LocalFile>();
		public static LocalFile GetLocalFile(string fileKey) {
			if (Config.UploadFiles()) {
				throw new Exception("Excepted local access (1)");
			}
			if (!Config.IsLocal()) {
				throw new Exception("Excepted local access (2)");
			}
			if (!_LocalUploads.ContainsKey(fileKey)) {
				throw new Exception("File not found. Local storage is ephemeral and deleted after server restarts");
			}
			return _LocalUploads[fileKey];

		}
		#endregion

		#region Save
		public static long SaveGeneratedFilePlaceholder_Unsafe(ISession s, long creatorId, string name, string type, string description, FileOrigin fileOrigin, FileOutputMethod outputMethod, PermTiny[] additionalPermissions, params TagModel[] tags) {
			return SaveFilePlaceholder_Unsafe(s, creatorId, name, type, description, fileOrigin, outputMethod, additionalPermissions, tags).Id;
		}

		public static async Task Save_Unsafe(ISession s, long fileId, Stream stream, FileNotification notifyRecipient) {
			var file = s.Get<EncryptedFileModel>(fileId);
			var filePath = file.FilePath;

			if (file.Size != 0) {
				throw new Exception("File already exists");
			}
			if (string.IsNullOrWhiteSpace(filePath)) {
				throw new Exception("File path was empty");
			}
			if (filePath.Length < 10) {
				throw new Exception("File path was too short");
			}
			long size;
			using (var ms = new MemoryStream()) {
				await stream.CopyToAsync(ms);
				stream.Seek(0, SeekOrigin.Begin);
				ms.Seek(0, SeekOrigin.Begin);
				var fileKey = GenerateFileKey(filePath);

				if (Config.UploadFiles()) {
					var fileTransferUtilityRequest = new TransferUtilityUploadRequest {
						BucketName = Config.GetDocumentRepositorySettings().Bucket,
						InputStream = ms,
						StorageClass = S3StorageClass.Standard,
						Key = fileKey,
						CannedACL = S3CannedACL.Private,
					};
					//fileTransferUtilityRequest.Headers.CacheControl = "public, max-age=604800";

					var fileTransferUtility = new TransferUtility(GetS3Client());
					fileTransferUtility.Upload(fileTransferUtilityRequest);
					size = stream.Length;

				} else {
					_LocalUploads[fileKey] = new LocalFile() {
						FileType = file.FileType,
						Bytes = ms.ToArray(),
						FileName = file.FileName
					};
					size = ms.Length;
				}
			}
			CompletedUpload_Unsafe(s, fileId, size, notifyRecipient, file.FileOutputMethod);
		}

		public static async Task<long> Save_Unsafe(long creatorId, Stream file, string name, string type, string description, FileOrigin origin, FileOutputMethod outputMethod, FileNotification notify, PermTiny[] additionalPermissions, params TagModel[] tags) {
			EncryptedFileModel fileObj;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					fileObj = SaveFilePlaceholder_Unsafe(s, creatorId, name, type, description, origin, outputMethod, additionalPermissions, tags);
					tx.Commit();
					s.Flush();
				}
			}
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					await Save_Unsafe(s, fileObj.Id, file, notify);
					tx.Commit();
					s.Flush();
				}
			}
			return fileObj.Id;
		}

		#endregion

		#region Intermediate Steps
		public class FileCompleteModel {
			public FileCompleteModel(long id, string fileName, FileOutputMethod outputMethod) {
				Id = id;
				FileName = fileName;
				OutputMethod = "" + outputMethod;
			}
			public long Id { get; set; }
			public string FileName { get; set; }
			public string OutputMethod { get; set; }

		}
		private static void CompletedUpload_Unsafe(ISession s, long fileId, long size, FileNotification notify, FileOutputMethod outputMethod) {
			var f = s.Get<EncryptedFileModel>(fileId);
			f.Complete = true;
			f.Generating = false;
			f.Size = size;
			s.Update(f);

			notify = notify ?? FileNotification.DoNotNotify();
			if (notify.Notify) {
				try {
					var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
					if (notify.ConnectionId == null) {
						hub.Clients.Group(RealTimeHub.Keys.UserId(f.CreatorId)).fileCompleted(new FileCompleteModel(f.Id, f.FileName, outputMethod));
					} else {
						hub.Clients.Client(notify.ConnectionId).fileCompleted(new FileCompleteModel(f.Id, f.FileName, outputMethod));
					}
				} catch (Exception) {
				}
			}
		}

		private static EncryptedFileModel SaveFilePlaceholder_Unsafe(ISession s, long creatorId, string name, string type, string description, FileOrigin fileOrigin, FileOutputMethod outputMethod, PermTiny[] additionalPermissions, params TagModel[] tags) {
			string filePath;
			var creator = s.Get<UserOrganizationModel>(creatorId);
			var fileObj = new EncryptedFileModel() {
				CreatorId = creatorId,
				EncryptionType = EncryptionType.None,
				FileDescription = description,
				FileName = name,
				FileType = type,
				FileOrigin = fileOrigin,
				FileOutputMethod = outputMethod,
				Generating = fileOrigin.IsGenerated(),
				OrgId = creator.Organization.Id,
			};
			fileObj.FilePath += type.NotNull(x => "." + x.Trim('.')) ?? "";
			filePath = fileObj.FilePath;
			s.Save(fileObj);

			if (tags != null) {
				foreach (var tag in tags) {
					s.Save(new EncryptedFileTagModel() {
						FileId = fileObj.Id,
						Tag = tag.Tag,
						ForModel = tag.ForModel,
						CreateTime = fileObj.CreateTime
					});
				}
			}


			//Permissions
			var additionalPermissionsList = (additionalPermissions ?? new PermTiny[0]).ToList();
			additionalPermissionsList.Add(PermTiny.Creator());
			PermissionsAccessor.CreatePermItems(s, creator, PermItem.ResourceType.File, fileObj.Id, additionalPermissionsList.ToArray());

			return fileObj;
		}
		#endregion

		#region Utilities
		private static string GenerateFileKey(string filePath) {
			return Config.GetDocumentRepositorySettings().Path + "/" + filePath;
		}

		private static string AppendType(string path, string type) {
			if (path == null) {
				return null;
			}

			return path + type.NotNull(x => "." + x.Trim('.')) ?? "";
		}

		private static string CleanFileName(string fileName) {
			return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
		}

		private static string GeneratePreSignedURL_Unsafe(string filePath, string filename, string type) {
			if (!Config.UploadFiles()) {
				return "/Export/LocalFile?id=" + GenerateFileKey(filePath);
			}

			string urlString = "";
			try {
				GetPreSignedUrlRequest request1 = new GetPreSignedUrlRequest {
					BucketName = Config.GetDocumentRepositorySettings().Bucket,
					Key = GenerateFileKey(filePath),
					Expires = DateTime.UtcNow.AddMinutes(5),
				};
				request1.ResponseHeaderOverrides.ContentDisposition = "attachment; filename=" + CleanFileName(AppendType(filename, type) ?? filePath);
				urlString = GetS3Client().GetPreSignedURL(request1);
			} catch (AmazonS3Exception e) {
				Console.WriteLine("Error encountered on server. Message:'{0}' when writing an object", e.Message);
			} catch (Exception e) {
				Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
			}
			return urlString;
		}
		private static AmazonS3Client GetS3Client() {
			return new AmazonS3Client(Amazon.RegionEndpoint.USEast1);
		}
		#endregion



	}
}