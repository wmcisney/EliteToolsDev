using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticLoadBalancing;
using Amazon.S3;
using Amazon.S3.Model;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RadialReview.Utilities {
	public class AwsUtil {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private const string ENVIRONMENT_NAME_TAG = "elasticbeanstalk:environment-name";

		public static List<Amazon.EC2.Model.Tag> GetTags(string instanceId) {
			try {
				using (AmazonEC2Client client = new AmazonEC2Client(RegionEndpoint.USWest2)) {
					var request = new DescribeInstancesRequest() {
						InstanceIds = new List<string> { instanceId }
					};
					var response = client.DescribeInstances(request);
					var tags = response.Reservations.SelectMany(x => x.Instances.SelectMany(y => y.Tags));
					return tags.ToList();
				}
			} catch (Exception e) {
				log.Info(e);
				return new List<Amazon.EC2.Model.Tag>();
			}
		}

		public static string GetVersionLabelGivenInstanceId(string instanceId) {
			if (instanceId == null) {
				throw new ArgumentNullException(nameof(instanceId));
			}

			var tags = GetTags(instanceId);
			if (!tags.Any()) {
				throw new KeyNotFoundException("No tags found");
			}
			var envNameTag = tags.FirstOrDefault(x => x.Key.ToLower() == ENVIRONMENT_NAME_TAG.ToLower());
			if (envNameTag == null) {
				throw new KeyNotFoundException("No tags with key: " + ENVIRONMENT_NAME_TAG);
			}

			var envName = envNameTag.Value;
			if (envName == null) {
				throw new KeyNotFoundException("EnvironmentName not found");
			}

			using (var ebc = new AmazonElasticBeanstalkClient(RegionEndpoint.USWest2)) {
				using (var ELBClient = new AmazonElasticLoadBalancingClient(RegionEndpoint.USWest2)) {
					var envs = ebc.DescribeEnvironments();

					foreach (var e in envs.Environments) {
						if (e.EnvironmentName == envName) {
							return e.VersionLabel;
						}
					}

					//var envLookup = envs.Environments.ToDefaultDictionary(x => x.EndpointURL, x => x, null);
					//var loadBalancers = ELBClient.DescribeLoadBalancers().LoadBalancerDescriptions;
					//foreach (var lb in loadBalancers) {						
					//	if (lb.Instances.Any(x => x.InstanceId.ToLower() == instanceId.ToLower())) {
					//		var env = envLookup[lb.DNSName];
					//		if (env != null) {
					//			return env.VersionLabel;
					//		}
					//	}
					//}
				}
			}
			throw new KeyNotFoundException("Version not found");
		}


		[Obsolete("Do not use. Very unsafe")]
		public static List<S3Object> GetObjectsInFolder(string bucket, string prefix) {
			var client = new AmazonS3Client(Amazon.RegionEndpoint.USEast1);
			var request = new ListObjectsRequest();
			request.BucketName = bucket;
			request.Prefix = prefix.EndsWith("/") ? prefix : prefix + "/";


			var allExisting = new List<S3Object>();

			do {
				var response = client.ListObjects(request);
				allExisting.AddRange(response.S3Objects.Where(x => x.Key != request.Prefix));

				if (response.IsTruncated) {
					request.Marker = response.NextMarker;
				} else {
					request = null;
				}
			} while (request != null);
			return allExisting;
		}

		public static Stream GetObject(string bucket, string key) {
			using (var client = new AmazonS3Client(Amazon.RegionEndpoint.USEast1)) {
				var request = new GetObjectRequest { BucketName = bucket, Key = key };
				using (var response = client.GetObject(request)) {
					using (var s = response.ResponseStream) {
						return StreamUtil.ReadIntoStream(s);
					}
				}
			}
		}
	}
}