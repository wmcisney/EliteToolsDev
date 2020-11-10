using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;

namespace RadialReview.Accessors {
	public class SettingsAccessor {
		private static AmazonDynamoDBClient client = new AmazonDynamoDBClient(RegionEndpoint.USWest2);

		public class SettingsKey {
			private SettingsKey(string key) {
				Key = key;
			}

			private string Key { get; set; }

			public static SettingsKey DatabaseUpdate(string applicationVersion,string method) {
				return new SettingsKey("DatabaseUpdate-" + applicationVersion+"-"+method);
			}

			public override string ToString() {
				return Key;
			}
		}			   

		public static string GetProductionSetting(SettingsKey key) {
			Table settings = Table.LoadTable(client, "TT-Settings");
			GetItemOperationConfig config = new GetItemOperationConfig {
				AttributesToGet = new List<string> { "Key", "Value" },
				ConsistentRead = true
			};
			Document document = settings.GetItem(key.ToString(), config);
			if (document == null) {
				return null;
			}
			return document["Value"].AsString();
		}

		public static void SetProductionSetting(SettingsKey key, string value) {
			Table settings = Table.LoadTable(client, "TT-Settings");
			var item = new Document();
			item["Key"] = key.ToString();
			item["Value"] = value;
			settings.PutItem(item);
		}

		public static SettingsViewModel GenerateViewSettings(UserOrganizationModel oneUser, string nameStr, bool isManager, bool superAdmin) {
			var settings = new SettingsViewModel();
			try {
				settings = new SettingsViewModel() {
					user = new SettingsViewModel.User() {
						isOrganizationAdmin = oneUser.NotNull(x => x.ManagingOrganization),
						userName = oneUser.NotNull(x => x.GetEmail()),
						userId = oneUser.NotNull(x => x.Id),
						isSupervisor = isManager,
						isSuperAdmin = superAdmin,
						name = nameStr,
					},
				};
			} catch (Exception e) {
			}
			try {
				if (oneUser != null && oneUser.Organization != null) {
					settings.organization = new SettingsViewModel.Org() {
						name = oneUser.Organization.GetName(),
						organizationId = oneUser.Organization.Id
					};
				}
			} catch (Exception) {
			}
			try {
				var signalrEndpoint = Config.GetSignalrEndpoint();
				settings.signalr.endpoint_count = signalrEndpoint.EndpointCount;
				settings.signalr.endpoint_pattern = signalrEndpoint.EndpointPattern;
			} catch (Exception) {
			}

			return settings;
		}

	}
}