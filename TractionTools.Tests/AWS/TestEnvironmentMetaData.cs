using Amazon;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticLoadBalancing;
using Amazon.ElasticLoadBalancing.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TractionTools.Tests.AWS {
	[TestClass]
	public class TestEnvironmentMetaData {
		[TestMethod]
		public void TestGetEnvironmentMetadata() {
			using (var ebc = new AmazonElasticBeanstalkClient(RegionEndpoint.USWest2)) {
				using (var ELBClient = new AmazonElasticLoadBalancingClient(RegionEndpoint.USWest2)) {
					var envs = ebc.DescribeEnvironments();
					var envLookup = envs.Environments.ToDefaultDictionary(x => x.EndpointURL, x => x, null);
					var loadBalancers = ELBClient.DescribeLoadBalancers().LoadBalancerDescriptions;

					foreach (var lb in loadBalancers) {
						var env = envLookup[lb.DNSName];
						Console.WriteLine(string.Join(", ", lb.Instances.Select(x => x.InstanceId)) + ":");
						if (env != null) {
							Console.WriteLine("\t" + env.VersionLabel);
						} else {
							Assert.Fail();
						}
					}

				}
			}
		}

		[TestMethod]
		public void TestGetVersionGivenInstanceId() {
			try {
				AwsUtil.GetVersionLabelGivenInstanceId("");
				Assert.Fail();
			} catch (KeyNotFoundException) {
				//expected error
			}

			string lastInstanceId;
			LoadBalancerDescription lb;
			using (var ELBClient = new AmazonElasticLoadBalancingClient(RegionEndpoint.USWest2)) {
				lb = ELBClient.DescribeLoadBalancers().LoadBalancerDescriptions.Last();
				lastInstanceId = lb.Instances.Last().InstanceId;
			}

			var a = AwsUtil.GetVersionLabelGivenInstanceId(lastInstanceId);
			Assert.IsNotNull(a);
			Assert.IsTrue(a.StartsWith("v20"));
		}


		[TestMethod]
		public void TestParticularInstanceId() {
			var id = "i-05da986a5a2f9e40c";
			var a = AwsUtil.GetVersionLabelGivenInstanceId(id);
			Assert.IsTrue(a.StartsWith("v20"));
		}
	}

}
