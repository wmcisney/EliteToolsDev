using FluentNHibernate.Mapping;
using Newtonsoft.Json;
using NHibernate;
using RadialReview.Controllers;
using RadialReview.Models.Application;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview {
	public class Variable {

		public static class Names {
			//Do not change these strings!! They are DB constants
			public static readonly string LAST_CAMUNDA_UPDATE_TIME = "LAST_CAMUNDA_UPDATE_TIME";
			public static readonly string USER_RADIAL_DATA_IDS = "USER_RADIAL_DATA_IDS";
			public static readonly string CONSENT_MESSAGE = "CONSENT_MESSAGE";
			public static readonly string PRIVACY_URL = "PRIVACY_URL";
			public static readonly string TOS_URL = "TOS_URL";
			public static readonly string DELINQUENT_MESSAGE_MEETING = "DELINQUENT_MESSAGE_MEETING";
			public static readonly string UPDATE_CARD_SUBJECT = "UPDATE_CARD_SUBJECT";
			public static readonly string TODO_DIVISOR = "TODO_DIVISOR";
			public static readonly string INJECTED_SCRIPTS = "INJECTED_SCRIPTS";
			public static readonly string LOG_ERRORS = "LOG_ERRORS";
			public static readonly string LAYOUT_WEIGHTS = "LAYOUT_WEIGHTS";
			public static readonly string LAYOUT_SETTINGS = "LAYOUT_SETTINGS";
			public static readonly string EOSI_REFERRAL_EMAIL = "EOSI_REFERRAL_EMAIL";
			public static readonly string CLIENT_REFERRAL_EMAIL = "CLIENT_REFERRAL_EMAIL";
			public static readonly string JOIN_ORGANIZATION_UNDER_MANAGER_BODY = "JOIN_ORGANIZATION_UNDER_MANAGER_BODY";

			public static readonly string MEETING_HUB_SETTINGS= "MEETING_HUB_SETTINGS";
			public static readonly string SOFTWARE_VERSION = "SOFTWARE_VERSION";
			public static readonly string READ_ONLY_MODE = "READ_ONLY_MODE";

			public static readonly string SHOULD_AUTOGENERATE_NOTIFICATION = "SHOULD_AUTOGENERATE_NOTIFICATION";

			public static readonly string V2_LANDING_INJECT = "V2_LANDING_INJECT";
			public static readonly string V2_LANDING_VIDEOS = "V2_LANDING_VIDEOS";
			public static readonly string V2_LANDING_BOTTOMVIDEOS = "V2_LANDING_BOTTOMVIDEOS";
			public static readonly string V2_LOGIN_URL = "V2_LOGIN_URL";
			public static readonly string V2_LOGIN_REDIRECT = "V2_LOGIN_REDIRECT";

			public static readonly string V2_PORT_SCRIPT = "V2_PORT_SCRIPT";
			public static readonly string V2_MIGRATIONDONE_BODY = "V2_MIGRATIONDONE_BODY";

			public static readonly string CLOSE_MEETING_AFTER = "CLOSE_MEETING_AFTER";
		}

		public virtual string K { get; set; }
		public virtual string V { get; set; }
		public virtual DateTime LastUpdate { get; set; }

		public Variable() {
			LastUpdate = DateTime.UtcNow;
		}

		public class Map : ClassMap<Variable> {
			public Map() {
				Id(x => x.K).GeneratedBy.Assigned();
				Map(x => x.V).Length(1024);
				Map(x => x.LastUpdate);
			}
		}
	}

}
namespace RadialReview.Variables {
	
	public class VariableAccessor {
		public static string Get(string key, Func<string> defaultValue) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var v = s.GetSettingOrDefault(key, ()=>defaultValue());
					tx.Commit();
					s.Flush();
					return v;
				}
			}
		}

		public static T Get<T>(string key, Func<T> defaultValue) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var v = s.GetSettingOrDefault(key, defaultValue);
					tx.Commit();
					s.Flush();
					return v;
				}
			}
		}

		public static T Get<T>(string key, T defaultValue) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var v = s.GetSettingOrDefault(key,()=> defaultValue);
					tx.Commit();
					s.Flush();
					return v;
				}
			}
		}
	}

	public static class VariableExtensions {
		#region Session
		private static Variable _GetSettingOrDefault(this ISession s, string key, Func<string> defaultValue = null) {
			var found = s.Get<Variable>(key);
			if (found == null) {
				found = new Variable() {
					K = key,
					V = defaultValue == null ? null : defaultValue()
				};
				s.Save(found);
			}
			return found;
		}
		public static string GetSettingOrDefault(this ISession s, string key, Func<string> defaultValue = null) {
			return _GetSettingOrDefault(s, key, defaultValue).V;
		}
		public static string GetSettingOrDefault(this ISession s, string key, string defaultValue) {
			return _GetSettingOrDefault(s, key, () => defaultValue).V;
		}
		public static T GetSettingOrDefault<T>(this ISession s, string key, Func<T> defaultValue) {
			return JsonConvert.DeserializeObject<T>(_GetSettingOrDefault(s, key, () => JsonConvert.SerializeObject(defaultValue())).V);
		}
		public static T GetSettingOrDefault<T>(this ISession s, string key, T defaultValue) {
			return JsonConvert.DeserializeObject<T>(_GetSettingOrDefault(s, key, () => JsonConvert.SerializeObject(defaultValue)).V);
		}
		public static Variable UpdateSetting<T>(this ISession s, string key, T newValue) {
			return UpdateSetting(s, key, JsonConvert.SerializeObject(newValue));
		}
		public static Variable UpdateSetting(this ISession s, string key, string newValue) {
			var found = _GetSettingOrDefault(s, key, () => newValue);
			if (found.V != newValue) {
				found.V = newValue;
				found.LastUpdate = DateTime.UtcNow;
				s.Update(found);
			}
			return found;
		}
		#endregion
		#region StatelessSession
		private static Variable _GetSettingOrDefault(this IStatelessSession s, string key, Func<string> defaultValue = null) {
			var found = s.Get<Variable>(key);
			if (found == null) {
				found = new Variable() {
					K = key,
					V = defaultValue == null ? null : defaultValue()
				};
				s.Insert(found);
			}
			return found;
		}
		public static T GetSettingOrDefault<T>(this IStatelessSession s, string key, T defaultValue) {
			return JsonConvert.DeserializeObject<T>(_GetSettingOrDefault(s, key, () => JsonConvert.SerializeObject(defaultValue)).V);
		}
		#endregion
	}
}
