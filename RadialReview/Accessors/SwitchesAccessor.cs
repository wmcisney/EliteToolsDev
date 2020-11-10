using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Accessors {

	public class GroupedFeatureSwitches {
		public long Id { get; set; }
		public string FeatureSelector { get; set; }
		public bool Production { get; set; }
		public bool Beta { get; set; }
		public bool SuperAdmin { get; set; }
		public bool WHITE_LABEL { get; set; }


		public IEnumerable<FeatureSwitchFlag> ToFeatureSwitcheFlags() {
			if (Production)
				yield return FeatureSwitchFlag.Production;
			if (Beta)
				yield return FeatureSwitchFlag.Beta;
			if (SuperAdmin)
				yield return FeatureSwitchFlag.SuperAdmin;
			if (WHITE_LABEL)
				yield return FeatureSwitchFlag.WHITE_LABEL;
		}
	}

	public class FeatureSwitchSettings {
		public FeatureSwitchSettings(List<GroupedFeatureSwitches> switches) {
			LastUpdate = DateTime.UtcNow;
			Switches = switches;
			StyleCache = new DefaultDictionary<FeatureSwitchFlag, MvcHtmlString>(x => GenerateStyle(x));
		}

		public DateTime LastUpdate { get; set; }
		public List<GroupedFeatureSwitches> Switches { get; set; }
		private DefaultDictionary<FeatureSwitchFlag, MvcHtmlString> StyleCache;

		public MvcHtmlString ToStyles(FeatureSwitchFlag accountSwitch) {
			return StyleCache[accountSwitch];
		}

		private MvcHtmlString GenerateStyle(FeatureSwitchFlag accountSwitch) {
			var selector = new Dictionary<FeatureSwitchFlag, Func<GroupedFeatureSwitches, bool>> {
				{ FeatureSwitchFlag.Production, x=>x.Production },
				{ FeatureSwitchFlag.Beta, x=>x.Beta },
				{ FeatureSwitchFlag.SuperAdmin, x=>x.SuperAdmin },
				{ FeatureSwitchFlag.WHITE_LABEL, x=>x.WHITE_LABEL },
			};

			var builder = new StringBuilder();
			var selectors = new List<string>();
			foreach (var sw in Switches) {
				var disabled = selector[accountSwitch](sw);
				if (disabled) {
					selectors.Add(sw.FeatureSelector);
				}
			}
			builder.Append("<!--Feature Switches-->\n");
			if (selectors.Any()) {
				builder.Append("<!--Flag: "+accountSwitch+"-->\n");
				builder.Append("<style>");
				builder.Append(string.Join(",", selectors));
				builder.Append("{display:none;}</style>\n");
			} else {
				builder.Append("	<!--None-->\n");
			}
			builder.Append("<!--End Feature Switches-->");
			return new MvcHtmlString(builder.ToString());
		}

	}

	public class SwitchesAccessor {
		private static FeatureSwitchSettings CachedSettings { get; set; }
		private static TimeSpan Timeout = TimeSpan.FromSeconds(60);
		
		public static MvcHtmlString GetSwitchStyles(string url,bool superAdmin) {
			url = url.ToLower();
			var settings = GetSwitchSettings();
			
			//Ordered Switches (do not reorder)
			if (superAdmin && !url.Contains("featureswitch=")) {
				return settings.ToStyles(FeatureSwitchFlag.SuperAdmin);
			}
			if (url.Contains("//traction.tools")) {
				return settings.ToStyles(FeatureSwitchFlag.Production);
			}
			if (url.Contains("//tractiontoolsbeta.com")) {
				return settings.ToStyles(FeatureSwitchFlag.Beta);
			}
			if (url.Contains("//white-label.com")) {
				return settings.ToStyles(FeatureSwitchFlag.WHITE_LABEL);
			}

			//Query params. All other options must fail first.
			if (url.Contains("featureswitch=production"))
				return settings.ToStyles(FeatureSwitchFlag.Production);
			if (url.Contains("featureswitch=beta"))
				return settings.ToStyles(FeatureSwitchFlag.Beta);
			if (url.Contains("featureswitch=white_label"))
				return settings.ToStyles(FeatureSwitchFlag.WHITE_LABEL);


			return new MvcHtmlString("<!--No Feature Switches-->");
		}


		public static FeatureSwitchSettings GetSwitchSettings() {
			if (CachedSettings == null || CachedSettings.LastUpdate + Timeout < DateTime.UtcNow) {
				CachedSettings = new FeatureSwitchSettings(GetSwitches());
			}
			return CachedSettings; 
		}

		//Do not make async. Async cannot be used in the BaseController.OnActionExecuting needed for GetSwitchStyles()
		public static List<GroupedFeatureSwitches> GetSwitches() {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var featureSwitch = s.QueryOver<FeatureSwitch>().Where(x => x.DeleteTime == null).List().ToList();

					var results = new List<GroupedFeatureSwitches>();
					foreach (var group in featureSwitch.GroupBy(x => x.FeatureSelector)) {
						var gfs = new GroupedFeatureSwitches() {
							Id = group.Min(x => x.Id),
							FeatureSelector = group.Key,
							Production = group.Any(x => x.ForFlag == FeatureSwitchFlag.Production && x.Disabled),
							Beta = group.Any(x => x.ForFlag == FeatureSwitchFlag.Beta && x.Disabled),
							SuperAdmin = group.Any(x => x.ForFlag == FeatureSwitchFlag.SuperAdmin && x.Disabled),
							WHITE_LABEL = group.Any(x => x.ForFlag == FeatureSwitchFlag.WHITE_LABEL && x.Disabled),
						};
						results.Add(gfs);
					}

					//Force Update Cache
					CachedSettings = new FeatureSwitchSettings(results);

					return results;

				}
			}
		}

		public static async Task RemoveSelector(UserOrganizationModel caller, string featureSelector) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller)
						.RadialAdmin();

					var fs = s.QueryOver<FeatureSwitch>()
						.Where(x => x.DeleteTime == null && x.FeatureSelector == featureSelector)
						.List().ToList();

					var now = DateTime.UtcNow;
					foreach (var f in fs) {
						f.DeleteTime = now;
						s.Update(f);
					}

					if (CachedSettings != null) {
						CachedSettings.LastUpdate = DateTime.MinValue;
					}


					tx.Commit();
					s.Flush();
				}
			}
		}


		public static async Task<GroupedFeatureSwitches> Update(UserOrganizationModel caller, GroupedFeatureSwitches model) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller)
						.RadialAdmin();

					var featureSwitch = s.QueryOver<FeatureSwitch>()
											.Where(x => x.DeleteTime == null && x.FeatureSelector == model.FeatureSelector)
											.List().ToList();

					var minId = long.MaxValue;
					if (featureSwitch.Any()) {
						minId = Math.Min(minId, featureSwitch.Min(x => x.Id));
					}

					var addRemove = SetUtility.AddRemove(featureSwitch.Where(x => x.Disabled).Select(x => x.ForFlag), model.ToFeatureSwitcheFlags());

					foreach (var add in addRemove.AddedValues) {
						//Update existing
						var anyUpdate = false;
						foreach (var fs in featureSwitch.Where(x => x.ForFlag == add && x.Disabled == false)) {
							fs.Disabled = true;
							s.Update(fs);
							anyUpdate = true;
						}
						if (!anyUpdate) {
							//Add if not exist.
							var newFs = new FeatureSwitch() {
								FeatureSelector = model.FeatureSelector,
								ForFlag = add,
								Disabled = true,
							};
							s.Save(newFs);
							minId = Math.Min(minId, newFs.Id);

						}
					}

					foreach (var remove in addRemove.RemovedValues) {
						foreach (var fs in featureSwitch.Where(x => x.ForFlag == remove)) {
							fs.Disabled = false;
							s.Update(fs);
						}
					}

					if (CachedSettings != null) {
						CachedSettings.LastUpdate = DateTime.MinValue;
					}

					tx.Commit();
					s.Flush();

					model.Id = minId;
					return model;
				}
			}
		}
	}
}