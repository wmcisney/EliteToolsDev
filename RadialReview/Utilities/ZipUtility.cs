using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace RadialReview.Utilities {
	public class ZipUtility {

		public class File {
			public string Name { get; set; }
			public string Contents { get; set; }
		}

		public static byte[] Zip(params File[] files) {
			using (var outStream = new MemoryStream()) {
				using (var archive = new ZipArchive(outStream, ZipArchiveMode.Create, true)) {
					foreach (var f in files) {
						byte[] fileBytes = Encoding.UTF8.GetBytes(f.Contents);
						var fileInArchive = archive.CreateEntry(f.Name, CompressionLevel.Optimal);
						using (var entryStream = fileInArchive.Open())
						using (var fileToCompressStream = new MemoryStream(fileBytes)) {
							fileToCompressStream.CopyTo(entryStream);
						}
					}
				}
				return outStream.ToArray();
			}
		}
	}
}