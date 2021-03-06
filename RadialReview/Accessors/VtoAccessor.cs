﻿using Microsoft.AspNet.SignalR;
using NHibernate;
using Novacode;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.CompanyValue;
using RadialReview.Models.Angular.VTO;
using RadialReview.Models.Askables;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.VTO;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Synchronize;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
	public class VtoAccessor : BaseAccessor {

		public static void UpdateAllVTOs(ISession s, long organizationId, string connectionId, Action<dynamic> action) {
			var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
			var vtoIds = s.QueryOver<VtoModel>().Where(x => x.Organization.Id == organizationId).Select(x => x.Id).List<long>();
			foreach (var vtoId in vtoIds) {
				var group = hub.Clients.Group(RealTimeHub.Keys.GenerateVtoGroupId(vtoId), connectionId);
				action(group);
			}
		}

		public static void UpdateVTO(ISession s, long vtoId, string connectionId, Action<dynamic> action) {
			var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
			var group = hub.Clients.Group(RealTimeHub.Keys.GenerateVtoGroupId(vtoId), connectionId);
			action(group);
		}

		public static List<VtoModel> GetAllVTOForOrganization(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ManagingOrganization(organizationId);

					return s.QueryOver<VtoModel>().Where(x => x.Organization.Id == organizationId && x.DeleteTime == null).List().ToList();
				}
			}
		}

		public static AngularVTO GetVTO(UserOrganizationModel caller, long vtoId, bool vision, bool traction,bool showIssues) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetVTO(s, perms, vtoId, vision, traction, showIssues);
				}
			}
		}

		public static AngularVTO GetSharedVTO(UserOrganizationModel caller, long orgId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetSharedVTO(s, perms, orgId);
				}
			}
		}

		private static AngularVTO GetSharedVTO(ISession s, PermissionsUtility perms, long orgId) {
			var l10 = s.QueryOver<L10Recurrence>()
				.Where(x => x.DeleteTime == null && x.OrganizationId == orgId && x.ShareVto == true)
				.Take(1).SingleOrDefault();

			if (l10 != null) {
				perms.Or(x => x.ViewOrganization(orgId), x => x.ViewL10Recurrence(l10.Id));
				var shared = GetVTO(s, perms, l10.VtoId, true, false, false);
				return shared;
			}
			return null;
		}

		public static AngularVTO GetVTO(ISession s, PermissionsUtility perms, long vtoId, bool vision, bool traction, bool showIssues) {
			if (!vision && !traction) {
				throw new PermissionsException();
			}
			var permsChecked = false;
			if (vision) {
				perms.ViewVTOVision(vtoId);
				permsChecked = true;
			}
			if (traction) {
				perms.ViewVTOTraction(vtoId);
				permsChecked = true;
			}

			if (showIssues) {
				//Overrides the value.
				showIssues = perms.IsPermitted(x=>x.ViewVTOTractionIssues(vtoId));
			}

			if (!permsChecked)
				throw new PermissionsException();


			var model = s.Get<VtoModel>(vtoId);

			if (vision) {
				model._Values = OrganizationAccessor.GetCompanyValues_Unsafe(s.ToQueryProvider(true), model.Organization.Id, null);
			}
			var uniquesQ = s.QueryOver<VtoItem_String>().Where(x => x.Type == VtoItemType.List_Uniques && x.Vto.Id == vtoId && x.DeleteTime == null).Future();//.List().ToList();
			var looksLikeQ = s.QueryOver<VtoItem_String>().Where(x => x.Type == VtoItemType.List_LookLike && x.Vto.Id == vtoId && x.DeleteTime == null).Future();//.List().ToList();
			var goalsQ = s.QueryOver<VtoItem_String>().Where(x => x.Type == VtoItemType.List_YearGoals && x.Vto.Id == vtoId && x.DeleteTime == null).Future();//.List().ToList();

			var threeYearHeadersQ = s.QueryOver<VtoItem_KV>().Where(x => x.Type == VtoItemType.Header_ThreeYearPicture && x.Vto.Id == vtoId && x.DeleteTime == null).Future();//.List().ToList();
			var oneYearHeadersQ = s.QueryOver<VtoItem_KV>().Where(x => x.Type == VtoItemType.Header_OneYearPlan && x.Vto.Id == vtoId && x.DeleteTime == null).Future();//.List().ToList();
			var rockHeadersQ = s.QueryOver<VtoItem_KV>().Where(x => x.Type == VtoItemType.Header_QuarterlyRocks && x.Vto.Id == vtoId && x.DeleteTime == null).Future();//.List().ToList();
																																									   //model.._GoalsForYear = s.QueryOver<VtoModel.VtoItem_String>().Where(x => x.Type == VtoItemType.List_Issues && x.Vto.Id == vtoId && x.DeleteTime == null).List().ToList();
																																									   //var rocksQ= s.QueryOver<Vto_Rocks>().Where(x => x.Vto.Id == vtoId && x.DeleteTime == null).Future();
			var rocksQ = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
				.Where(x => x.DeleteTime == null && x.L10Recurrence.Id == model.L10Recurrence && x.VtoRock)
				.Future();

			if (traction) {
				model._Issues = s.QueryOver<VtoItem_String>().Where(x => x.Type == VtoItemType.List_Issues && x.Vto.Id == vtoId && x.DeleteTime == null).List().Select(x => new VtoIssue() {
					Id = x.Id,
					BaseId = x.BaseId,
					CopiedFrom = x.CopiedFrom,
					CreateTime = x.CreateTime,
					Data = x.Data,
					DeleteTime = x.DeleteTime,
					ForModel = x.ForModel,
					Ordering = x.Ordering,
					Type = x.Type,
					Vto = x.Vto,
				}).ToList();
			}
			if (vision) {
				var getMarketStrategy = s.QueryOver<MarketingStrategyModel>().Where(x => x.Vto == vtoId && x.DeleteTime == null).List();
				foreach (var item in getMarketStrategy) {
					item._Uniques = s.QueryOver<VtoItem_String>().Where(x => x.Type == VtoItemType.List_Uniques && x.MarketingStrategyId == item.Id && x.DeleteTime == null).List().ToList();
				}

				model._MarketingStrategyModel = getMarketStrategy.ToList();

				model.ThreeYearPicture._Headers = threeYearHeadersQ.ToList();
				model.ThreeYearPicture._LooksLike = looksLikeQ.ToList();
			}
			if (traction) {

				model.OneYearPlan._Headers = oneYearHeadersQ.ToList();
				model.OneYearPlan._GoalsForYear = goalsQ.ToList();

				model.QuarterlyRocks._Headers = rockHeadersQ.ToList();
				model.QuarterlyRocks._Rocks = rocksQ.ToList().Where(x => x.ForRock.DeleteTime == null /* && x.Rock.CompanyRock*/)
					.Select(x => AngularVtoRock.Create(x)).ToList();

				var issuesAttachedToRecur = model._Issues
					.Where(x => x.ForModel != null && x.ForModel.ModelType == ForModel.GetModelType<IssueModel.IssueModel_Recurrence>())
					.Select(x => x.ForModel.ModelId)
					.Distinct().ToArray();

				if (issuesAttachedToRecur.Any()) {
					var foundIssues = s.QueryOver<IssueModel.IssueModel_Recurrence>().WhereRestrictionOn(x => x.Id).IsIn(issuesAttachedToRecur).List().ToList();
					foreach (var i in model._Issues) {
						if (i.ForModel != null && i.ForModel.ModelType == ForModel.GetModelType<IssueModel.IssueModel_Recurrence>()) {
							i.Issue = foundIssues.FirstOrDefault(x => x.Id == i.ForModel.ModelId);
							i._Extras["Owner"] = i.Issue.NotNull(x => x.Owner.NotNull(y => y.GetName()));
							i._Extras["OwnerInitials"] = i.Issue.NotNull(x => x.Owner.NotNull(y => y.GetInitials()));
						}
					}
				}
			}

			var output = AngularVTO.Create(model);
			if (!vision) {
				output.CoreValueTitle = null;
				output.CoreFocus = null;
				output.IncludeVision = false;
				output.Strategies = null;
				output.Strategy = null;
				output.TenYearTarget = null;
				output.TenYearTargetTitle = null;
				output.ThreeYearPicture = null;
				output.Values = null;
			}

			if (!traction) {
				output.Issues = null;
				output.IssuesListTitle = null;
				output.OneYearPlan = null;
				output.QuarterlyRocks = null;
				output._TractionPageName = null;
				output.IncludeTraction = false;
			}


			if (!showIssues) {
				output.Issues = null;
				output.IssuesDisabled = true;
			}

			return output;
		}

		public static AngularVTO GetAngularVTO(UserOrganizationModel caller, long vtoId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);

					var vision = perms.IsPermitted(x => x.ViewVTOVision(vtoId));
					var traction = perms.IsPermitted(x => x.ViewVTOTraction(vtoId));
					var showIssues = perms.IsPermitted(x => x.ViewVTOTractionIssues(vtoId));
					if (!vision && !traction) {
						throw new PermissionsException("Cannot view this item");
					}
					var ang = GetVTO(s, perms, vtoId, vision, traction, showIssues);

					if (ang.L10Recurrence != null) {
						try {
							var recur = L10Accessor.GetL10Recurrence(s, perms, ang.L10Recurrence.Value, LoadMeeting.False());
							var orgVto = GetSharedVTO(s, perms, ang._OrganizationId);
							if (recur.TeamType != L10TeamType.LeadershipTeam && orgVto == null) {
								ang.IncludeVision = false;
							}
							if (orgVto != null) {
								ang.ReplaceVision(orgVto);

							}

						} catch (Exception) {

						}
					}
					return ang;
				}
			}
		}
		public static VtoModel CreateRecurrenceVTO(ISession s, PermissionsUtility perm, long recurrenceId) {
			perm.EditL10Recurrence(recurrenceId);
			var recurrence = s.Get<L10Recurrence>(recurrenceId);
			perm.ViewOrganization(recurrence.OrganizationId);

			var model = new VtoModel();
			model.Organization = s.Get<OrganizationModel>(recurrence.OrganizationId);

			s.SaveOrUpdate(model);

			model.CoreFocus.Vto = model.Id;
			model.MarketingStrategy.Vto = model.Id;


			model.OneYearPlan.Vto = model.Id;
			model.QuarterlyRocks.Vto = model.Id;
			model.ThreeYearPicture.Vto = model.Id;
			model.L10Recurrence = recurrenceId;

			model.Name = recurrence.Name;

			s.Update(model);

			recurrence.VtoId = model.Id;
			s.Update(recurrence);
			return model;
		}

		public static VtoModel CreateVTO(ISession s, PermissionsUtility perm, long organizationId) {
			perm.ViewOrganization(organizationId).CreateVTO(organizationId);

			var model = new VtoModel();
			model.Organization = s.Get<OrganizationModel>(organizationId);
			s.SaveOrUpdate(model);

			model.CoreFocus.Vto = model.Id;
			model.MarketingStrategy.Vto = model.Id;
			model.OneYearPlan.Vto = model.Id;
			model.QuarterlyRocks.Vto = model.Id;
			model.ThreeYearPicture.Vto = model.Id;

			s.Update(model);
			return model;
		}
		public static VtoModel CreateVTO(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);

					var model = CreateVTO(s, perm, organizationId);

					tx.Commit();
					s.Flush();

					return model;
				}
			}
		}


		public static MarketingStrategyModel CreateMarketingStrategy(UserOrganizationModel caller, long vtoId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);
					MarketingStrategyModel obj = new MarketingStrategyModel();
					obj.Vto = vtoId;
					s.Save(obj);
					tx.Commit();
					s.Flush();
					return obj;
				}
			}
		}


		public static async Task RemoveMarketingStrategy(UserOrganizationModel caller, long strategyId, string connectionId) {
			await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateStrategy(strategyId), async s => {
				var strategy = s.Get<MarketingStrategyModel>(strategyId);
				PermissionsUtility.Create(s, caller).EditVTO(strategy.Vto);
				strategy.DeleteTime = DateTime.UtcNow;
				s.Update(strategy);
				var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
				var group = hub.Clients.Group(RealTimeHub.Keys.GenerateVtoGroupId(strategy.Vto));
				group.update(new AngularUpdate(){new AngularVTO(strategy.Vto) {
						Strategies = AngularList.CreateFrom(AngularListType.Remove,new AngularStrategy(strategyId))
					} });
			});
		}


		public static async Task UpdateVtoString(UserOrganizationModel caller, long vtoStringId, String message, bool? deleted, string connectionId = null) {
			long? update_VtoId = null;
			VtoItem_String str = null;
			await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateVtoItem(vtoStringId), async s => {
				str = s.Get<VtoItem_String>(vtoStringId);
				var perm = PermissionsUtility.Create(s, caller).EditVTO(str.Vto.Id);
				str.Data = message;
				if (str.BaseId == 0) {
					str.BaseId = str.Id;
				}

				if (deleted != null) {
					if (deleted == true && str.DeleteTime == null) {
						str.DeleteTime = DateTime.UtcNow;
						connectionId = null;
					} else if (deleted == false) {
						str.DeleteTime = null;
					}
				}
				s.Update(str);
				update_VtoId = str.Vto.Id;
				//Update IssueRecurrence
				if (str.ForModel != null) {
					if (str.ForModel.ModelType == ForModel.GetModelType<IssueModel.IssueModel_Recurrence>()) {
						var issueRecur = s.Get<IssueModel.IssueModel_Recurrence>(str.ForModel.ModelId);
						if (perm.IsPermitted(x => x.EditL10Recurrence(issueRecur.Recurrence.Id))) {
							issueRecur.Issue.Message = message;
							s.Update(issueRecur.Issue);
						}
					}
				}
			});
			if (update_VtoId != null) {
				var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
				var group = hub.Clients.Group(RealTimeHub.Keys.GenerateVtoGroupId(update_VtoId.Value), connectionId);
				str.Vto = null;
				group.update(new AngularUpdate(){
					AngularVtoString.Create(str)
				});
			}
		}

		public static async Task UpdateVtoKV(UserOrganizationModel caller, long vtoKVId, string key, string value, bool? deleted, string connectionId = null) {
			long? update_VtoId = null;
			VtoItem_KV kv = null;
			await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateVtoItem(vtoKVId), async s => {
				kv = s.Get<VtoItem_KV>(vtoKVId);
				var perm = PermissionsUtility.Create(s, caller).EditVTO(kv.Vto.Id);
				kv.K = key;
				kv.V = value;
				if (kv.BaseId == 0) {
					kv.BaseId = kv.Id;
				}

				if (deleted != null) {
					if (deleted == true && kv.DeleteTime == null) {
						kv.DeleteTime = DateTime.UtcNow;
						connectionId = null;
					} else if (deleted == false) {
						kv.DeleteTime = null;
					}
				}
				s.Update(kv);
				update_VtoId = kv.Vto.Id;
			});
			if (update_VtoId != null) {
				var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
				var group = hub.Clients.Group(RealTimeHub.Keys.GenerateVtoGroupId(update_VtoId.Value), connectionId);
				kv.Vto = null;
				group.update(new AngularUpdate(){
					AngularVtoKV.Create(kv)
				});
			}
		}


		[Untested("ESA")]
		public static async Task UpdateVto(UserOrganizationModel caller, long vtoId, String name = null, String tenYearTarget = null, String tenYearTargetTitle = null, String coreValueTitle = null, String issuesListTitle = null, string connectionId = null) {
			var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
			var group = hub.Clients.Group(RealTimeHub.Keys.GenerateVtoGroupId(vtoId), connectionId);
			await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateVto(vtoId), async s => {

				PermissionsUtility.Create(s, caller).EditVTO(vtoId);
				var vto = s.Get<VtoModel>(vtoId);


				vto.Name = name;
				vto.TenYearTarget = tenYearTarget;
				vto.TenYearTargetTitle = tenYearTargetTitle;
				vto.CoreValueTitle = coreValueTitle;
				vto.IssuesListTitle = issuesListTitle;

				s.Update(vto);

				group.update(new AngularVTO(vtoId) {
					Name = vto.Name,
					TenYearTarget = vto.TenYearTarget,
					TenYearTargetTitle = vto.TenYearTargetTitle,
					CoreValueTitle = vto.CoreValueTitle,
					IssuesListTitle = vto.IssuesListTitle
				});
			});

		}

		public static async Task Update(UserOrganizationModel caller, BaseAngular model, string connectionId) {
			if (model.Type == typeof(AngularVtoString).Name) {
				var m = (AngularVtoString)model;
				await UpdateVtoString(caller, m.Id, m.Data, null, connectionId);
			} else if (model.Type == typeof(AngularVTO).Name) {
				var m = (AngularVTO)model;
				await UpdateVto(caller, m.Id, m.Name, m.TenYearTarget, m.TenYearTargetTitle, m.CoreValueTitle, m.IssuesListTitle, connectionId);
			} else if (model.Type == typeof(AngularCompanyValue).Name) {
				var m = (AngularCompanyValue)model;
				await UpdateCompanyValue(caller, m.Id, m.CompanyValue, m.CompanyValueDetails, null, connectionId);
			} else if (model.Type == typeof(AngularCoreFocus).Name) {
				var m = (AngularCoreFocus)model;
				await UpdateCoreFocus(caller, m.Id, m.Purpose, m.Niche, m.PurposeTitle, m.CoreFocusTitle, connectionId);
			} else if (model.Type == typeof(AngularStrategy).Name) {
				var m = (AngularStrategy)model;
				await UpdateStrategy(caller, m.Id, m.TargetMarket, m.ProvenProcess, m.Guarantee, m.MarketingStrategyTitle, m.Title, connectionId);
			} else if (model.Type == typeof(AngularVtoRock).Name) {
				var m = (AngularVtoRock)model;
				await UpdateRock(caller, m.Id, m.Rock.Name, m.Rock.Owner.Id, null, connectionId);
			} else if (model.Type == typeof(AngularOneYearPlan).Name) {
				var m = (AngularOneYearPlan)model;
				await UpdateOneYearPlan(caller, m.Id, m.FutureDate, m.OneYearPlanTitle, connectionId);
			} else if (model.Type == typeof(AngularQuarterlyRocks).Name) {
				var m = (AngularQuarterlyRocks)model;
				await UpdateQuarterlyRocks(caller, m.Id, m.FutureDate, m.RocksTitle, connectionId: connectionId);
			} else if (model.Type == typeof(AngularThreeYearPicture).Name) {
				var m = (AngularThreeYearPicture)model;
				await UpdateThreeYearPicture(caller, m.Id, m.FutureDate, m.ThreeYearPictureTitle, connectionId);
			} else if (model.Type == typeof(AngularVtoKV).Name) {
				var m = (AngularVtoKV)model;
				await UpdateVtoKV(caller, m.Id, m.K, m.V, null, connectionId);
			}
		}

		public static async Task UpdateThreeYearPicture(UserOrganizationModel caller, long id, DateTime? futuredate = null, string threeYearPictureTitle = null, string connectionId = null) {
			await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateThreeYearPicture(id), async s => {
				var threeYear = s.Get<ThreeYearPictureModel>(id);
				var vtoId = threeYear.Vto;

				var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
				var group = hub.Clients.Group(RealTimeHub.Keys.GenerateVtoGroupId(vtoId), connectionId);
				PermissionsUtility.Create(s, caller).EditVTO(vtoId);

				threeYear.FutureDate = futuredate;
				//threeYear.RevenueStr = revenue;
				//threeYear.ProfitStr = profit;
				//threeYear.Measurables = measurables;
				threeYear.ThreeYearPictureTitle = threeYearPictureTitle;
				s.Update(threeYear);

				group.update(new AngularUpdate(){new AngularThreeYearPicture(id) {
						FutureDate = futuredate,
						//Revenue = revenue,
						//Profit = profit,
						//Measurables = measurables,
						ThreeYearPictureTitle=threeYearPictureTitle
					}});
			});
		}
		public static async Task UpdateQuarterlyRocks(UserOrganizationModel caller, long id, DateTime? futuredate = null, string revenue = null, string profit = null, string measurables = null, string rocksTitle = null, string connectionId = null) {
			await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateQuarterlyRocks(id), async s => {
				var quarterlyRocks = s.Get<QuarterlyRocksModel>(id);
				var vtoId = quarterlyRocks.Vto;

				var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
				var group = hub.Clients.Group(RealTimeHub.Keys.GenerateVtoGroupId(vtoId), connectionId);


				PermissionsUtility.Create(s, caller).EditVTO(vtoId);
				quarterlyRocks.FutureDate = futuredate.HasValue ? futuredate.Value.Date : futuredate;

				//quarterlyRocks.RevenueStr = revenue;
				//quarterlyRocks.ProfitStr = profit;
				//quarterlyRocks.Measurables = measurables;
				quarterlyRocks.RocksTitle = rocksTitle;
				s.Update(quarterlyRocks);

#pragma warning disable CS0618 // Type or member is obsolete
				group.update(new AngularUpdate(){new AngularQuarterlyRocks(id) {
						FutureDate = futuredate,
						//Revenue = revenue,
						//Profit = profit,
						//Measurables = measurables,
						RocksTitle=rocksTitle,
					}});
#pragma warning restore CS0618 // Type or member is obsolete
			});
		}

		public static async Task UpdateOneYearPlan(UserOrganizationModel caller, long id, DateTime? futuredate = null, string oneYearPlanTitle = null, string connectionId = null) {
			await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateOneYearPlan(id), async s => {
				var plan = s.Get<OneYearPlanModel>(id);
				var vtoId = plan.Vto;

				var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
				var group = hub.Clients.Group(RealTimeHub.Keys.GenerateVtoGroupId(vtoId), connectionId);


				PermissionsUtility.Create(s, caller).EditVTO(vtoId);
				plan.FutureDate = futuredate.HasValue ? futuredate.Value.Date : futuredate;
				//plan.FutureDate = futuredate;
				//plan.RevenueStr = revenue;
				//plan.ProfitStr = profit;
				//plan.Measurables = measurables;
				plan.OneYearPlanTitle = oneYearPlanTitle;
				s.Update(plan);

#pragma warning disable CS0618 // Type or member is obsolete
				group.update(new AngularUpdate(){new AngularOneYearPlan(id) {
						FutureDate = futuredate,
						//Revenue = revenue,
						//Profit = profit,
						//Measurables = measurables,
						OneYearPlanTitle=oneYearPlanTitle
					}});
#pragma warning restore CS0618 // Type or member is obsolete
			});
		}

		public static async Task UpdateStrategy(UserOrganizationModel caller, long strategyId, String targetMarket = null, String provenProcess = null, String guarantee = null, String marketingStrategyTitle = null, String title = null, string connectionId = null) {

			await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateStrategy(strategyId), async s => {
				var strategy = s.Get<MarketingStrategyModel>(strategyId);
				var vtoId = strategy.Vto;

				var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
				var group = hub.Clients.Group(RealTimeHub.Keys.GenerateVtoGroupId(vtoId), connectionId);

				PermissionsUtility.Create(s, caller).EditVTO(vtoId);

				strategy.ProvenProcess = provenProcess;
				strategy.Guarantee = guarantee;
				strategy.TargetMarket = targetMarket;
				strategy.MarketingStrategyTitle = marketingStrategyTitle;
				strategy.Title = title;
				s.Update(strategy);

#pragma warning disable CS0618 // Type or member is obsolete
				group.update(new AngularUpdate(){new AngularStrategy(strategyId) {
						ProvenProcess = provenProcess,
						Guarantee = guarantee,
						TargetMarket = targetMarket,
						MarketingStrategyTitle=marketingStrategyTitle,
					}});
#pragma warning restore CS0618 // Type or member is obsolete
			});
		}

		public static async Task UpdateCoreFocus(UserOrganizationModel caller, long coreFocusId, string purpose, string niche, string purposeTitle, string coreFocusTitle, string connectionId) {
			await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateCoreFocus(coreFocusId), async s => {
				var coreFocus = s.Get<CoreFocusModel>(coreFocusId);
				PermissionsUtility.Create(s, caller).EditVTO(coreFocus.Vto);
				coreFocus.Purpose = purpose;
				coreFocus.Niche = niche;
				coreFocus.PurposeTitle = purposeTitle;
				coreFocus.CoreFocusTitle = coreFocusTitle;
				s.Update(coreFocus);

				var update = new AngularUpdate() { AngularCoreFocus.Create(coreFocus) };
				UpdateVTO(s, coreFocus.Vto, connectionId, x => x.update(update));
			});
		}

		public static async Task UpdateCompanyValue(UserOrganizationModel caller, long companyValueId, string message, string details, bool? deleted, string connectionId) {
			var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
			await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateCompanyValue(companyValueId), async s => {
				var companyValue = s.Get<CompanyValueModel>(companyValueId);
				PermissionsUtility.Create(s, caller).EditCompanyValues(companyValue.OrganizationId);


				if (message != null) {
					companyValue.CompanyValue = message;
					s.Update(companyValue);
				}

				if (details != null) {
					companyValue.CompanyValueDetails = details;
					s.Update(companyValue);
				}

				if (deleted != null) {
					if (deleted == false) {
						companyValue.DeleteTime = null;
					} else if (companyValue.DeleteTime == null) {
						companyValue.DeleteTime = DateTime.UtcNow;
						connectionId = null;
					}
					s.Update(companyValue);
				}
				var update = new AngularUpdate();
				update.Add(AngularCompanyValue.Create(companyValue));
				UpdateAllVTOs(s, companyValue.OrganizationId, connectionId, x => x.update(update));

			});
		}


		public static async Task UpdateRock(UserOrganizationModel caller, long recurrenceRockId, string message, long? accountableUser, bool? deleted, string connectionId) {
			await SyncUtil.EnsureStrictlyAfter(caller, s => {
				var recurRock = s.Get<L10Recurrence.L10Recurrence_Rocks>(recurrenceRockId);
				return SyncAction.UpdateRockCompletion(recurRock.ForRock.Id);
			}, async s => {
				var perms = PermissionsUtility.Create(s, caller);
				var recurRock = s.Get<L10Recurrence.L10Recurrence_Rocks>(recurrenceRockId);

				if (deleted == true) {
					await L10Accessor.SetVtoRock(s, perms, recurrenceRockId, false);
				} else {
					if (deleted == false) {
						await L10Accessor.SetVtoRock(s, perms, recurrenceRockId, true);
					}
					await RockAccessor.UpdateRock(s, perms, recurRock.ForRock.Id, message, accountableUser);
				}
			});
		}

		public static void JoinVto(UserOrganizationModel caller, long vtoId, string connectionId) {
			var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewVTOVision(vtoId);

					hub.Groups.Add(connectionId, RealTimeHub.Keys.GenerateVtoGroupId(vtoId));
					Audit.VtoLog(s, caller, vtoId, "JoinVto");
				}
			}
		}

		public static async Task AddKV(UserOrganizationModel caller, long vtoId, VtoItemType type, Func<VtoModel, BaseAngularList<AngularVtoKV>, IAngularId> updateFunc, bool skipUpdate = false, ForModel forModel = null, string key = null, string value = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					AddKV(s, perms, vtoId, type, updateFunc, skipUpdate, forModel, key, value);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static VtoItem_KV AddKV(ISession s, PermissionsUtility perms, long vtoId, VtoItemType type, Func<VtoModel, BaseAngularList<AngularVtoKV>, IAngularId> updateFunc, bool skipUpdate = false, ForModel forModel = null, string key = null, string value = null) {
			perms.EditVTO(vtoId);
			var vto = s.Get<VtoModel>(vtoId);
			var organizationId = vto.Organization.Id;

			var items = s.QueryOver<VtoItem_KV>()
				.Where(x => x.Vto.Id == vtoId && x.Type == type && x.DeleteTime == null)
				.List().ToList();

			var count = items.Count();

#pragma warning disable CS0618 // Type or member is obsolete
			var kv = new VtoItem_KV() {
				Type = type,
				Ordering = count,
				Vto = vto,
				ForModel = forModel,
				//Data = value,
				K = key,
				V = value,
			};
#pragma warning restore CS0618 // Type or member is obsolete

			s.Save(kv);
			items.Add(kv);
			var angularItems = AngularList.Create(AngularListType.ReplaceAll, AngularVtoKV.Create(items));

			if (updateFunc != null) {
				if (skipUpdate) {
					UpdateVTO(s, vtoId, null, x => x.update(updateFunc(vto, angularItems)));
				}

				UpdateVTO(s, vtoId, null, x => x.update(new AngularUpdate() { updateFunc(vto, angularItems) }));
			}
			return kv;
		}

		public static VtoItem_String AddString(ISession s, PermissionsUtility perms, long vtoId, VtoItemType type, Func<VtoModel, BaseAngularList<AngularVtoString>, IAngularId> updateFunc, bool skipUpdate = false, ForModel forModel = null, string value = null, long? marketingStrategyId = null) {
			perms.EditVTO(vtoId);
			var vto = s.Get<VtoModel>(vtoId);
			var organizationId = vto.Organization.Id;

			var items = s.QueryOver<VtoItem_String>().Where(x => x.Vto.Id == vtoId && x.Type == type && x.DeleteTime == null && x.MarketingStrategyId == marketingStrategyId).List().ToList();
			var count = items.Count();

#pragma warning disable CS0618 // Type or member is obsolete
			var str = new VtoItem_String() {
				Type = type,
				Ordering = count,
				Vto = vto,
				ForModel = forModel,
				Data = value,
				MarketingStrategyId = marketingStrategyId
			};
#pragma warning restore CS0618 // Type or member is obsolete

			s.Save(str);

			items.Add(str);
			var angularItems = AngularList.Create(AngularListType.ReplaceAll, AngularVtoString.Create(items));

			if (updateFunc != null) {
				if (skipUpdate) {
					UpdateVTO(s, vtoId, null, x => x.update(updateFunc(vto, angularItems)));
				}

				UpdateVTO(s, vtoId, null, x => x.update(new AngularUpdate() { updateFunc(vto, angularItems) }));
			}
			return str;
		}

		public static void AddString(UserOrganizationModel caller, long vtoId, VtoItemType type, Func<VtoModel, BaseAngularList<AngularVtoString>, IAngularId> updateFunc, bool skipUpdate = false, ForModel forModel = null, long? marketingStrategyId = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					AddString(s, perms, vtoId, type, updateFunc, skipUpdate, forModel, null, marketingStrategyId);
					tx.Commit();
					s.Flush();
				}
			}
		}


#pragma warning disable CS0618 // Type or member is obsolete
		public static void AddUniques(UserOrganizationModel caller, long vtoId, long marketingStrategyId) {
			//AddString(caller, vtoId, VtoItemType.List_Uniques, (vto, items) => new AngularStrategy(vto.MarketingStrategy.Id) { Uniques = items });
			AddString(caller, vtoId, VtoItemType.List_Uniques, (vto, items) => new AngularStrategy(marketingStrategyId) { Uniques = items }, false, null, marketingStrategyId);
		}
		public static void AddThreeYear(UserOrganizationModel caller, long vtoId) {
			AddString(caller, vtoId, VtoItemType.List_LookLike, (vto, list) => new AngularThreeYearPicture(vto.ThreeYearPicture.Id) { LooksLike = list });
		}
		public static void AddYearGoal(UserOrganizationModel caller, long vtoId) {
			AddString(caller, vtoId, VtoItemType.List_YearGoals, (vto, list) => new AngularOneYearPlan(vto.OneYearPlan.Id) { GoalsForYear = list });
		}
		public static void AddIssue(UserOrganizationModel caller, long vtoId) {
			AddString(caller, vtoId, VtoItemType.List_Issues, (vto, list) => new AngularVTO(vto.Id) { Issues = list }, true);
		}
#pragma warning restore CS0618 // Type or member is obsolete

		public static void AddCompanyValue(UserOrganizationModel caller, long vtoId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).EditVTO(vtoId);

					var vto = s.Get<VtoModel>(vtoId);

					var organizationId = vto.Organization.Id;
					var existing = OrganizationAccessor.GetCompanyValues(s.ToQueryProvider(true), perms, organizationId, null);
					existing.Add(new CompanyValueModel() { OrganizationId = organizationId });
					OrganizationAccessor.EditCompanyValues(s, perms, organizationId, existing);

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static List<RockModel> GetVtoRocks_Unsafe(ISession s, long vtoId) {
			var vto = s.Get<VtoModel>(vtoId);
			if (vto.L10Recurrence == null) {
				throw new NotImplementedException("Vto rocks not implemented");
			}

			return s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
				.Where(x => x.DeleteTime == null && x.L10Recurrence.Id == vto.L10Recurrence && x.VtoRock)
				.List().Select(x => x.ForRock)
				.ToList();

		}



		public static async Task CreateNewRock(UserOrganizationModel caller, long vtoId, string message = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					await CreateNewRock(s, perms, vtoId, caller.Id, message);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static async Task CreateNewRock(ISession s, PermissionsUtility perms, long vtoId, long ownerId, string message = null) {
			var vto = s.Get<VtoModel>(vtoId);
			await L10Accessor.CreateAndAttachRock(s, perms, vto.L10Recurrence.Value, ownerId, message, true);
		}

		private static string ParseVtoHeader(Novacode.Cell cell, string searchFor) {
			searchFor = searchFor.ToLower();
			var found = cell.Paragraphs.Where(x => x.StyleName != "ListParagraph").Where(x => x.Text.ToLower().Contains(searchFor)).FirstOrDefault().NotNull(x => x.Text);
			if (found != null) {
				var sp = found.Split(':');
				if (sp.Length > 1) {
					found = string.Join(":", sp.Skip(1));
				} else {
					found = sp[0].SubstringAfter(searchFor);
				}
			}
			return found;
		}

		public static async Task<VtoModel> UploadVtoForRecurrence(UserOrganizationModel caller, DocX doc, long recurrenceId, List<Exception> exceptions) {

			exceptions = exceptions ?? new List<Exception>();


			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).AdminL10Recurrence(recurrenceId);
					var recur = s.Get<L10Recurrence>(recurrenceId);
					var vtoId = recur.VtoId;
					if (vtoId <= 0) {
						throw new PermissionsException("V/TO does not exist.");
					}

					perms.EditVTO(vtoId);
					var vto = s.Get<VtoModel>(vtoId);
					if (vto == null) {
						throw new PermissionsException("V/TO does not exist.");
					}

					#region Initialize defaults
					var corevaluesTitle = "CORE VALUES";
					var threeYearTitle = "3-YEAR PICTURE™";
					var coreFocusTitle = "CORE FOCUS™";
					var tenYearTargetTitle = "10-YEAR TARGET™";
					var marketingStrategyTitle = "MARKETING STRATEGY";
					var rocksTitle = "ROCKS";
					var issuesTitle = "ISSUES LIST";
					var oneYearTitle = "1-YEAR PLAN";

					List<string> corevaluesList = new List<string>();

					string threeYearFuture = "";
					string threeYearRevenue = "";
					string threeYearProfit = "";
					string threeYearMeasurables = "";
					var threeYearLooksList = new List<string>();

					string purpose = "<could not parse>";
					string niche = "<could not parse>";
					string purposeTitle = "Purpose/Cause/Passion";

					Cell tenYearCell = null;
					var marketingDict = new DefaultDictionary<string, string>(x => "<could not parse>");
					var uniques = new List<string>();

					string oneYearFuture = "";
					string oneYearRevenue = "";
					string oneYearProfit = "";
					string oneYearMeasurables = "";

					List<string> oneYearPlanGoals = new List<string>();
					string rocksFuture = "";
					string rocksRevenue = "";
					string rocksProfit = "";
					string rocksMeasurables = "";

					List<Row> rocksList = new List<Row>();
					List<string> issuesList = new List<string>();
					#endregion

					#region Page 1
					try {
						if (doc.Tables.Count < 2) {
							throw new FormatException("Could not find the V/TO.");
						}

						var page1 = doc.Tables[0];

						if (page1.Rows.Count != 5) {
							throw new FormatException("Could not find Vision Page.");
						}

						var corevaluesRow = page1.Rows[0];
						var threeYearPictureDetailsRow = page1.Rows[1];
						var coreFocusRow = page1.Rows[2];
						var tenYearRow = page1.Rows[3];
						var marketingStrategyRow = page1.Rows[4];

						try {
							//Core values
							if (corevaluesRow.Cells.Count != 3 || corevaluesRow.Cells[0].FillColor.Name != "bfbfbf") {
								throw new FormatException("Could not find Core Values.");
							}

							if (corevaluesRow.Cells[0].Paragraphs.Count == 1 && !string.IsNullOrWhiteSpace(corevaluesRow.Cells[0].Paragraphs[0].Text)) {
								corevaluesTitle = corevaluesRow.Cells[0].Paragraphs[0].Text;
							}

							var corevaluesCell = corevaluesRow.Cells[1];
							if (corevaluesCell.Lists.Count != 1 || !corevaluesCell.Lists[0].Items.Any()) {
								throw new FormatException("Could not find Core Values list.");
							}

							corevaluesList = corevaluesCell.Lists[0].Items.Select(x => x.Text).ToList();
						} catch (Exception e) {
							exceptions.Add(e);
						}
						try {
							//3 year picture
							if (corevaluesRow.Cells.Count != 3 || corevaluesRow.Cells[2].FillColor.Name != "bfbfbf" || coreFocusRow.Cells.Count != 3) {
								throw new FormatException("Could not find Three Year Picture.");
							}

							if (corevaluesRow.Cells[2].Paragraphs.Count == 1 && !string.IsNullOrWhiteSpace(corevaluesRow.Cells[2].Paragraphs[0].Text)) {
								threeYearTitle = corevaluesRow.Cells[2].Paragraphs[0].Text;
							}

							if (threeYearPictureDetailsRow.Cells.Count != 3) {
								throw new FormatException("Could not find Three Year Picture details.");
							}

							var threeYearCell = threeYearPictureDetailsRow.Cells[2];

							//    throw new FormatException("Could not find Three Year Picture (What does it look like).");
							//var

							//var threeYearTop = threeYearCell.Paragraphs.Where(x => x.StyleName != "ListParagraph").ToList();

							//3 year picture - Headings
							try {
								threeYearFuture = ParseVtoHeader(threeYearCell, "Future Date");
								threeYearRevenue = ParseVtoHeader(threeYearCell, "Revenue");
								threeYearProfit = ParseVtoHeader(threeYearCell, "Profit");
								threeYearMeasurables = ParseVtoHeader(threeYearCell, "Measurables");
							} catch (Exception e) {
								exceptions.Add(new FormatException("Could not add Three Year Picture heading", e));
							}
							try {
								if (threeYearCell.Lists.Count != 0) {
									threeYearLooksList = threeYearCell.Lists.Last().Items.Select(x => x.Text.Trim()).ToList();
								}
							} catch (Exception e) {
								exceptions.Add(new FormatException("Could not add Three Year Picture heading", e));
							}
						} catch (Exception e) {
							exceptions.Add(e);
						}
						try {
							//Core Focus
							if (coreFocusRow.Cells.Count != 3 || coreFocusRow.Cells[0].FillColor.Name != "bfbfbf") {
								throw new FormatException("Could not find Core Focus.");
							}

							if (coreFocusRow.Cells[0].Paragraphs.Count == 1 && !string.IsNullOrWhiteSpace(coreFocusRow.Cells[0].Paragraphs[0].Text)) {
								coreFocusTitle = coreFocusRow.Cells[0].Paragraphs[0].Text;
							}

							var coreFocusCell = coreFocusRow.Cells[1];

							var nicheParagraphTuple = coreFocusCell.Paragraphs.Select((x, i) => Tuple.Create(i, x)).Where(x => x.Item2.Text.ToLower().Contains("niche")).FirstOrDefault();

							if (nicheParagraphTuple == null && coreFocusCell.Paragraphs.Count == 2 && coreFocusCell.Paragraphs[0].MagicText.Count > 0) {
								purposeTitle = coreFocusCell.Paragraphs[0].MagicText[0].text;
								purpose = string.Join("", coreFocusCell.Paragraphs[0].MagicText.Skip(1).Select(x => x.text)).TrimStart(':').Trim();
								niche = string.Join("", coreFocusCell.Paragraphs[1].MagicText.Skip(1).Select(x => x.text)).TrimStart(':').Trim();
							} else {
								var purposeParagraphs = coreFocusCell.Paragraphs.Where((x, i) => i < nicheParagraphTuple.Item1).SelectMany(x => x.MagicText).ToList();
								var nicheParagraphs = coreFocusCell.Paragraphs.Where((x, i) => i >= nicheParagraphTuple.Item1).SelectMany(x => x.MagicText).ToList();

								purpose = string.Join("", purposeParagraphs.Skip(1).Select(x => x.text)).TrimStart(':').Trim();
								niche = string.Join("", nicheParagraphs.Skip(1).Select(x => x.text)).TrimStart(':').Trim();

								if (purposeParagraphs.Count > 0) {
									purposeTitle = purposeParagraphs[0].text;
								}
							}
						} catch (Exception e) {
							exceptions.Add(new FormatException("Could not add Core Focus.", e));
						}

						try {
							//10 year target
							if (tenYearRow.Cells.Count != 3 || tenYearRow.Cells[0].FillColor.Name != "bfbfbf") {
								throw new FormatException("Could not find Ten Year Target.");
							}

							if (tenYearRow.Cells[0].Paragraphs.Count == 1 && !string.IsNullOrWhiteSpace(tenYearRow.Cells[0].Paragraphs[0].Text)) {
								tenYearTargetTitle = tenYearRow.Cells[0].Paragraphs[0].Text;
							}

							tenYearCell = tenYearRow.Cells[1];
						} catch (Exception e) {
							exceptions.Add(new FormatException("Could not add Ten Year Target.", e));
						}

						try {
							//Marketing Strategy
							if (marketingStrategyRow.Cells.Count != 3 || marketingStrategyRow.Cells[0].FillColor.Name != "bfbfbf") {
								throw new FormatException("Could not find Marketing Strategy.");
							}

							if ((marketingStrategyRow.Cells[0].Paragraphs.Count == 1 || marketingStrategyRow.Cells[0].Paragraphs.Count == 2) && !string.IsNullOrWhiteSpace(string.Join(" ", marketingStrategyRow.Cells[0].Paragraphs.Select(x => x.Text)))) {
								marketingStrategyTitle = string.Join(" ", marketingStrategyRow.Cells[0].Paragraphs.Select(x => x.Text));
							}

							var marketingStrategyCell = marketingStrategyRow.Cells[1];


							var targetTuple = Tuple.Create("target", marketingStrategyCell.Paragraphs.Select((x, i) => Tuple.Create(i, x)).Where(x => x.Item2.Text.ToLower().Contains("target market") || x.Item2.Text.Contains("The List")).FirstOrDefault());
							var uniquesTuple = Tuple.Create("uniques", marketingStrategyCell.Paragraphs.Select((x, i) => Tuple.Create(i, x)).Where(x => x.Item2.Text.ToLower().Contains("uniques")).FirstOrDefault());
							var provenTuple = Tuple.Create("proven", marketingStrategyCell.Paragraphs.Select((x, i) => Tuple.Create(i, x)).Where(x => x.Item2.Text.ToLower().Contains("proven")).FirstOrDefault());
							var guaranteeTuple = Tuple.Create("guarantee", marketingStrategyCell.Paragraphs.Select((x, i) => Tuple.Create(i, x)).Where(x => x.Item2.Text.ToLower().Contains("guarantee")).FirstOrDefault());

							// <name, <location, paragraph>>
							var marketStratList = new List<Tuple<string, Tuple<int, Paragraph>>>() { targetTuple, uniquesTuple, provenTuple, guaranteeTuple };

							var ordering = marketStratList.Where(x => x.Item2 != null).OrderBy(x => x.Item2.Item1).ToList().Where(x => x.Item2.Item2 != null).ToList();


							if (ordering.Any()) {
								for (var i = 0; i < ordering.Count; i++) {
									var start = ordering[i].Item2.Item1;
									var end = 0;
									if (i != ordering.Count - 1) {
										end = ordering[i + 1].Item2.Item1;
									} else {
										end = marketingStrategyCell.Paragraphs.Count;
									}
									//Grab this section's paragraphs
									//merge the magic text together, skip the first one (usually the title)
									var sectionTitle = ordering[i].Item1;
									marketingDict[sectionTitle] = string.Join("", marketingStrategyCell.Paragraphs.Where((x, j) => start <= j && j < end).SelectMany(x => x.MagicText).Skip(1).Select(x => x.text));
								}
							}
							if (marketingStrategyCell.Lists.Count == 1) {
								uniques = marketingStrategyCell.Lists[0].Items.Select(x => x.Text).ToList();
							} else if (marketingStrategyCell.Lists.Count > 1) {
								var uniquesHeadingLoc = marketingStrategyCell.Xml.Value.IndexOf(uniquesTuple.Item2.Item2.Xml.Value);
								uniques = marketingStrategyCell.Lists.FirstOrDefault(x => marketingStrategyCell.Xml.Value.IndexOf(x.Xml.Value) > uniquesHeadingLoc).NotNull(y => y.Items.Select(x => x.Text).ToList()) ?? uniques;
							}
						} catch (Exception e) {
							exceptions.Add(new FormatException("Could not add Marketing Strategy.", e));
						}
					} catch (Exception e) {
						exceptions.Add(e);
					}


					#endregion
					#region Page 2
					try {
						var page2 = doc.Tables[1];

						//if (page2.Rows.Count != 2)
						//	throw new FormatException("Could not find Traction Page.");

						var headingsRow = page2.Rows[0];
						var tractionRow = page2.Rows[1];

						if (headingsRow.Cells.Count != 3 || headingsRow.Cells.Any(x => x.FillColor.Name != "bfbfbf")) {
							throw new FormatException("Could not find Traction Page headings.");
						}

						if (tractionRow.Cells.Count != 3) {
							throw new FormatException("Could not find Traction Page data.");
						}

						if (headingsRow.Cells[0].Paragraphs.Count == 1 && !string.IsNullOrWhiteSpace(headingsRow.Cells[0].Paragraphs[0].Text)) {
							oneYearTitle = headingsRow.Cells[0].Paragraphs[0].Text;
						}

						if (headingsRow.Cells[1].Paragraphs.Count == 1 && !string.IsNullOrWhiteSpace(headingsRow.Cells[1].Paragraphs[0].Text)) {
							rocksTitle = headingsRow.Cells[1].Paragraphs[0].Text;
						}

						if (headingsRow.Cells[2].Paragraphs.Count == 1 && !string.IsNullOrWhiteSpace(headingsRow.Cells[2].Paragraphs[0].Text)) {
							issuesTitle = headingsRow.Cells[2].Paragraphs[0].Text;
						}

						//One Year Plan
						try {
							var oneYearPlanCell = tractionRow.Cells[0];
							if (oneYearPlanCell.Tables.Count != 1 || oneYearPlanCell.Tables[0].ColumnCount > 2) {
								throw new FormatException("Could not find One Year Plan goals.");
							}
							//One year target - Headings
							try {
								oneYearFuture = ParseVtoHeader(oneYearPlanCell, "Future Date");
								oneYearRevenue = ParseVtoHeader(oneYearPlanCell, "Revenue");
								oneYearProfit = ParseVtoHeader(oneYearPlanCell, "Profit");
								oneYearMeasurables = ParseVtoHeader(oneYearPlanCell, "Measurables");
							} catch (Exception e) {
								exceptions.Add(new FormatException("Could not add One Year Goals heading.", e));
							}

							try {
								oneYearPlanGoals = oneYearPlanCell.Tables[0].Rows
									.Select(x => string.Join("\n", x.Cells.Last().Paragraphs.Select(y => y.Text)))
									.Where(x => !string.IsNullOrWhiteSpace(x))
									.ToList();
							} catch (Exception e) {
								exceptions.Add(new FormatException("Could not add One Year Goals.", e));
							}

						} catch (Exception e) {
							exceptions.Add(e);
						}

						//Rocks
						try {
							var rocksCell = tractionRow.Cells[1];

							try {
								//One year target - Headings
								rocksFuture = ParseVtoHeader(rocksCell, "Future Date");
								rocksRevenue = ParseVtoHeader(rocksCell, "Revenue");
								rocksProfit = ParseVtoHeader(rocksCell, "Profit");
								rocksMeasurables = ParseVtoHeader(rocksCell, "Measurables");
							} catch (Exception e) {
								exceptions.Add(new FormatException("Could not add Rocks heading.", e));
							}
							try {
								var bestTable = rocksCell.Tables.OrderByDescending(x => x.ColumnCount).Where(x => x.ColumnCount <= 3).FirstOrDefault();

								if (bestTable == null) {
									throw new FormatException("Could not find Rocks list.");
								}
								//if (rocksCell.Tables.Count != 1 || rocksCell.Tables[0].ColumnCount > 3)
								rocksList = bestTable.Rows;
							} catch (Exception e) {
								exceptions.Add(new FormatException("Could not add Rocks.", e));
							}
						} catch (Exception e) {
							exceptions.Add(e);
						}
						//Issues List
						try {
							var issuesCell = tractionRow.Cells[2];
							if (issuesCell.Tables.Count != 1 || issuesCell.Tables[0].ColumnCount > 2) {
								throw new FormatException("Could not find Issues List.");
							}

							issuesList = issuesCell.Tables[0].Rows
								.Select(x => string.Join("\n", x.Cells.Last().Paragraphs.Select(y => y.Text.Trim())))
								.Where(x => !string.IsNullOrWhiteSpace(x))
								.ToList();
						} catch (Exception e) {
							exceptions.Add(new FormatException("Could not add Issues List.", e));
						}
						#endregion
					} catch (Exception e) {
						exceptions.Add(e);
					}
					#region Update VTO
					//Headings
					vto.CoreValueTitle = corevaluesTitle;
					vto.CoreFocus.CoreFocusTitle = coreFocusTitle;
					vto.TenYearTargetTitle = tenYearTargetTitle;
					vto.MarketingStrategy.MarketingStrategyTitle = marketingStrategyTitle;
					vto.ThreeYearPicture.ThreeYearPictureTitle = threeYearTitle;

					vto.OneYearPlan.OneYearPlanTitle = oneYearTitle;
					vto.QuarterlyRocks.RocksTitle = rocksTitle;
					vto.IssuesListTitle = issuesTitle;


					//Core Values
					var organizationId = vto.Organization.Id;
					var existing = OrganizationAccessor.GetCompanyValues(s.ToQueryProvider(true), perms, organizationId, null);
					foreach (var cv in corevaluesList) {
						existing.Add(new CompanyValueModel() { OrganizationId = organizationId, CompanyValue = cv });
					}
					OrganizationAccessor.EditCompanyValues(s, perms, organizationId, existing);

#pragma warning disable CS0219 // Variable is assigned but its value is never used
					var currencyStyle = NumberStyles.AllowCurrencySymbol | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowLeadingWhite | NumberStyles.AllowParentheses | NumberStyles.AllowThousands | NumberStyles.AllowTrailingWhite | NumberStyles.Currency;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
					var currentCulture = Thread.CurrentThread.CurrentCulture;
					//Three Year Picture
					vto.ThreeYearPicture.FutureDate = threeYearFuture.TryParseDateTime();
					//vto.ThreeYearPicture.RevenueStr = threeYearRevenue;//.TryParseDecimal(currencyStyle, currentCulture);
					VtoAccessor.AddKV(s, perms, vtoId, VtoItemType.Header_ThreeYearPicture, null, skipUpdate: true, key: "Revenue", value: threeYearRevenue);
					//vto.ThreeYearPicture.ProfitStr = threeYearProfit;//.TryParseDecimal(currencyStyle, currentCulture);
					VtoAccessor.AddKV(s, perms, vtoId, VtoItemType.Header_ThreeYearPicture, null, skipUpdate: true, key: "Profit", value: threeYearProfit);
					//vto.ThreeYearPicture.Measurables = threeYearMeasurables;
					VtoAccessor.AddKV(s, perms, vtoId, VtoItemType.Header_ThreeYearPicture, null, skipUpdate: true, key: "Measurables", value: threeYearMeasurables);

					foreach (var t in threeYearLooksList) {
						VtoAccessor.AddString(s, perms, vtoId, VtoItemType.List_LookLike, null, skipUpdate: true, value: t);
					}

					//Core Focus 
					vto.CoreFocus.Niche = niche;
					vto.CoreFocus.Purpose = purpose;
					vto.CoreFocus.PurposeTitle = purposeTitle;


					//Ten Year Target
					if (tenYearCell != null) {
						vto.TenYearTarget = string.Join("\n", tenYearCell.Paragraphs.Select(x => x.Text));
					}

					//Marketing Strategy 

					vto.MarketingStrategy.TargetMarket = marketingDict["target"];
					vto.MarketingStrategy.ProvenProcess = marketingDict["proven"];
					vto.MarketingStrategy.Guarantee = marketingDict["guarantee"];

					foreach (var t in uniques) {
						VtoAccessor.AddString(s, perms, vtoId, VtoItemType.List_Uniques, null, skipUpdate: true, value: t);
					}

					//One Year Plan
					vto.OneYearPlan.FutureDate = oneYearFuture.TryParseDateTime();
					//vto.OneYearPlan.RevenueStr = oneYearRevenue;//.TryParseDecimal(currencyStyle, currentCulture);
					VtoAccessor.AddKV(s, perms, vtoId, VtoItemType.Header_OneYearPlan, null, skipUpdate: true, key: "Revenue", value: oneYearRevenue);
					//vto.OneYearPlan.ProfitStr = oneYearProfit;//.TryParseDecimal(currencyStyle, currentCulture);
					VtoAccessor.AddKV(s, perms, vtoId, VtoItemType.Header_OneYearPlan, null, skipUpdate: true, key: "Profit", value: oneYearProfit);
					// vto.OneYearPlan.Measurables = oneYearMeasurables;
					VtoAccessor.AddKV(s, perms, vtoId, VtoItemType.Header_OneYearPlan, null, skipUpdate: true, key: "Measurables", value: oneYearMeasurables);

					foreach (var t in oneYearPlanGoals) {
						VtoAccessor.AddString(s, perms, vtoId, VtoItemType.List_YearGoals, null, skipUpdate: true, value: t);
					}

					//Rocks
					vto.QuarterlyRocks.FutureDate = rocksFuture.TryParseDateTime();
					//vto.QuarterlyRocks.RevenueStr = rocksRevenue;//.TryParseDecimal(currencyStyle, currentCulture);
					VtoAccessor.AddKV(s, perms, vtoId, VtoItemType.Header_QuarterlyRocks, null, skipUpdate: true, key: "Revenue", value: rocksRevenue);
					//vto.QuarterlyRocks.ProfitStr = rocksProfit;//.TryParseDecimal(currencyStyle, currentCulture);
					VtoAccessor.AddKV(s, perms, vtoId, VtoItemType.Header_QuarterlyRocks, null, skipUpdate: true, key: "Profit", value: rocksProfit);
					//vto.QuarterlyRocks.Measurables = rocksMeasurables;
					VtoAccessor.AddKV(s, perms, vtoId, VtoItemType.Header_QuarterlyRocks, null, skipUpdate: true, key: "Measurables", value: rocksMeasurables);

					var allUsers = TinyUserAccessor.GetOrganizationMembers(s, perms, vto.Organization.Id);
					Dictionary<string, DiscreteDistribution<TinyUser>> rockUserLookup = null;
					if (rocksList.Any() && (rocksList[0].ColumnCount == 3 || rocksList[0].ColumnCount == 2)) {
						var rockUsers = rocksList.Select(x => string.Join("\n", x.Cells.Last().Paragraphs.Select(y => y.Text)));
						rockUserLookup = DistanceUtility.TryMatch(rockUsers, allUsers);
					}

					try {
						foreach (var r in rocksList) {
							var owner = caller.Id;
							if (r.ColumnCount == 2 || r.ColumnCount == 3) {
								var ownerTup = new TinyUser() {
									FirstName = "",
									LastName = "",
									UserOrgId = owner
								};
								rockUserLookup[string.Join("\n", r.Cells.Last().Paragraphs.Select(y => y.Text))].TryResolveOne(ref ownerTup);
							}

							var message = r.Cells.Reverse<Cell>().Skip(1).FirstOrDefault().NotNull(x => string.Join("\n", x.Paragraphs.Select(y => y.Text)));
							if (!string.IsNullOrWhiteSpace(message)) {
								await CreateNewRock(s, perms, vtoId, owner, message);
							}
						}
					} catch (Exception e) {
						exceptions.Add(new FormatException("Could not upload Rocks.", e));
					}

					//Issues
					foreach (var i in issuesList) {
						VtoAccessor.AddString(s, perms, vtoId, VtoItemType.List_Issues, null, skipUpdate: true, value: i);
					}
					#endregion

					tx.Commit();
					s.Flush();

					return vto;
				}
			}

		}

		public static VtoItem_String GetVTOIssueByIssueId(UserOrganizationModel caller, long issueId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var vtoItem = s.QueryOver<VtoItem_String>().Where(x => x.DeleteTime == null && x.ForModel.ModelType == ForModel.GetModelType<IssueModel.IssueModel_Recurrence>() && x.ForModel.ModelId == issueId).SingleOrDefault();
					perms.ViewVTOTraction(vtoItem.Id);
					return vtoItem;
				}
			}
		}
	}
}
