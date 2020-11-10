using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Dashboard;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using RadialReview.Models.ViewModels;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
	public class DashboardAccessor {

		public static int TILE_HEIGHT = 5;

		public static List<Dashboard> GetDashboardsForUser(UserOrganizationModel caller, long userId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetDashboardsForUser(s, perms, userId);

				}
			}
		}

		public static WorkspaceDropdownVM GetWorkspaceDropdown(UserOrganizationModel caller, long userId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetWorkspaceDropdown(s,perms, userId);

				}
			}
		}

		public static WorkspaceDropdownVM GetWorkspaceDropdown(ISession s, PermissionsUtility perms, long userId) {
			perms.Self(userId);
			var allDashboards = DashboardAccessor.GetDashboardsForUser(s, perms, userId);
            //var l10s = L10Accessor.GetVisibleL10Recurrences(s, perms, userId);
            //var l10s = L10Accessor.GetViewableL10Meetings_Tiny(s, perms, userId);
            var l10s = L10Accessor.GetViewableL10Meetings_Tiny(s, perms, userId).OrderByDescending(x => x.StarDate).ThenBy(x => x.Name).ToList();
            var user = s.Get<UserOrganizationModel>(userId);
			var primaryDash = user.PrimaryWorkspace ?? new UserOrganizationModel.PrimaryWorkspaceModel() {
				WorkspaceId = allDashboards.Where(x => x.PrimaryDashboard).Select(x => x.Id).FirstOrDefault(),
				Type = DashboardType.Standard
			};
			var custom = allDashboards.Where(x => !x.PrimaryDashboard).Select(x => new NameId(x.Title, x.Id)).ToList();

			var defaultWorkspaceName = "Default Workspace";
			if (user.UserIds.Length > 1)
				defaultWorkspaceName = "Default Workspace (cross-account)";

			var originals = s.QueryOver<Dashboard>()
				.Where(x => x.DeleteTime == null && x.ForUser.Id == user.User.Id && x.PrimaryDashboard)
				.List()
				.Select((x,i)=>new NameId(defaultWorkspaceName + (i>0?" ("+i+")":""),x.Id))
				.ToList();


			custom.AddRange(originals);

			return new WorkspaceDropdownVM() {
				AllMeetings = l10s,
				CustomWorkspaces = custom,
				DefaultWorkspaceId = allDashboards.FirstOrDefault(x => x.PrimaryDashboard).NotNull(x => x.Id),
				PrimaryWorkspace = primaryDash
			};
		}

		public static async Task SetHomeWorkspace(UserOrganizationModel caller, long userId, DashboardType type, long dashboardId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					await SetHomeWorkspace(s, perms, userId, type, dashboardId);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static async Task SetHomeWorkspace(ISession s, PermissionsUtility perms, long userId, DashboardType modelType, long modelId) {
			perms.ViewDashboard(modelType, modelId);
			perms.Self(userId);

			var user = s.Get<UserOrganizationModel>(userId);
			user.PrimaryWorkspace = new UserOrganizationModel.PrimaryWorkspaceModel() {
				WorkspaceId = modelId,
				Type = modelType,
			};
			s.Update(user);
		}

		public static List<Dashboard> GetDashboardsForUser(ISession s, PermissionsUtility perms, long userId) {
			var user = s.Get<UserOrganizationModel>(userId);
			if (user == null || user.User == null) {
				throw new PermissionsException("User does not exist.");
			}

			perms.ViewDashboardForUser(user.User.Id);
			return s.QueryOver<Dashboard>().Where(x => x.DeleteTime == null && x.ForUser.Id == user.User.Id).List().ToList();

		}

		public static Dashboard GetPrimaryDashboardForUser(UserOrganizationModel caller, long userId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return GetPrimaryDashboardForUser(s, caller, userId);
				}
			}
		}

		public static Dashboard GetPrimaryDashboardForUser(ISession s, UserOrganizationModel caller, long userId) {
			var user = s.Get<UserOrganizationModel>(userId);
			if (user == null || user.User == null) {
				throw new PermissionsException("User does not exist.");
			}

			PermissionsUtility.Create(s, caller).ViewDashboardForUser(user.User.Id);
			return s.QueryOver<Dashboard>()
				.Where(x => x.DeleteTime == null && x.ForUser.Id == user.User.Id && x.PrimaryDashboard)
				.OrderBy(x => x.CreateTime).Desc
				.Take(1).SingleOrDefault();
		}

		public static Dashboard CreateDashboard(UserOrganizationModel caller, string title, bool primary, bool defaultDashboard = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					if (caller.User == null) {
						throw new PermissionsException("User does not exist.");
					}

					Dashboard dash = CreateDashboard(s, caller, title, primary, defaultDashboard);

					tx.Commit();
					s.Flush();
					return dash;
				}
			}
		}

		public static Dashboard CreateDashboard(ISession s, UserOrganizationModel caller, string title, bool primary, bool defaultDashboard) {
			if (primary) {
				var existing = s.QueryOver<Dashboard>().Where(x => x.DeleteTime == null && x.ForUser.Id == caller.User.Id && x.PrimaryDashboard).List();
				foreach (var e in existing) {
					e.PrimaryDashboard = false;
					s.Update(e);
				}
			} else {
				//If this the first one, then override primary to true
				primary = (!s.QueryOver<Dashboard>().Where(x => x.DeleteTime == null && x.ForUser.Id == caller.User.Id).Select(x => x.Id).List<long>().Any());
			}

			var dash = new Dashboard() {
				ForUser = caller.User,
				Title = title,
				PrimaryDashboard = primary,
			};
			s.Save(dash);
			if (defaultDashboard) {
				var perms = PermissionsUtility.Create(s, caller);
				//x: 0, y: 0, w: 1, h: 1
				CreateTile(s, perms, dash.Id, 1, 1 * TILE_HEIGHT, 0, 0 * TILE_HEIGHT, "/TileData/UserProfile2", "Profile", TileType.Profile);
				CreateTile(s, perms, dash.Id, 1, 1 * TILE_HEIGHT, 0, 1 * TILE_HEIGHT, "/TileData/FAQTips", "FAQ Guide", TileType.FAQGuide);
				/*if (caller.IsManager()) {
					//x: 0, y: 1, w: 1, h: 3
					CreateTile(s, perms, dash.Id, 1, 2 * TILE_HEIGHT, 0, 2 * TILE_HEIGHT, "/TileData/UserManage2", "Managing", TileType.Manage);
				}*/
				//x: 1, y: 2, w: 3, h: 2
				CreateTile(s, perms, dash.Id, 4, 2 * TILE_HEIGHT, 0, 2 * TILE_HEIGHT, "/TileData/UserTodo2", "To-dos", TileType.Todo);
				//x: 1, y: 0, w: 6, h: 2
				CreateTile(s, perms, dash.Id, 6, 2 * TILE_HEIGHT, 1, 0 * TILE_HEIGHT, "/TileData/UserScorecard2", "Scorecard", TileType.Scorecard);
				//x: 4, y: 2, w: 3, h: 2
				CreateTile(s, perms, dash.Id, 3, 2 * TILE_HEIGHT, 4, 2 * TILE_HEIGHT, "/TileData/UserRock2", "Rocks", TileType.Rocks);

			}

			return dash;
		}

		public static Dashboard GetDashboard(UserOrganizationModel caller, long dashboardId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var dash = s.Get<Dashboard>(dashboardId);
					if (dash == null) {
						return null;
					}

					PermissionsUtility.Create(s, caller).ViewDashboardForUser(dash.ForUser.Id);
					return dash;
				}
			}
		}

		public static TileModel CreateTile(ISession s, PermissionsUtility perms, long dashboardId, int w, int h, int x, int y, string dataUrl, string title, TileType type, string keyId = null) {
			perms.EditDashboard(DashboardType.Standard, dashboardId);
			if (type == TileType.Invalid) {
				throw new PermissionsException("Invalid tile type");
			}

			var uri = new Uri(dataUrl, UriKind.Relative);
			if (uri.IsAbsoluteUri) {
				throw new PermissionsException("Data url must be relative");
			}

			var dashboard = s.Get<Dashboard>(dashboardId);

			var tile = (new TileModel() {
				Dashboard = dashboard,
				DataUrl = dataUrl,
				ForUser = dashboard.ForUser,
				Height = h,
				Width = w,
				X = x,
				Y = y,
				Type = type,
				Title = title,
				KeyId = keyId,
			});

			s.Save(tile);
			return tile;
		}

		public static TileModel CreateTile(UserOrganizationModel caller, long dashboardId, int w, int h, int x, int y, string dataUrl, string title, TileType type, string keyId = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var perms = PermissionsUtility.Create(s, caller);
					var tile = CreateTile(s, perms, dashboardId, w, h, x, y, dataUrl, title, type, keyId);
					tx.Commit();
					s.Flush();
					return tile;
				}
			}
		}

		public static TileModel EditTile(ISession s, PermissionsUtility perms, long tileId, int? w = null, int? h = null, int? x = null, int? y = null, bool? hidden = null, string dataUrl = null, string title = null) {
			var tile = s.Get<TileModel>(tileId);

			tile.Height = h ?? tile.Height;
			tile.Width = w ?? tile.Width;
			tile.X = x ?? tile.X;
			tile.Y = y ?? tile.Y;
			tile.Hidden = hidden ?? tile.Hidden;
			tile.Title = title ?? tile.Title;

			if (dataUrl != null) {
				//Ensure relative
				var uri = new Uri(dataUrl, UriKind.Relative);
				if (uri.IsAbsoluteUri) {
					throw new PermissionsException("Data url must be relative.");
				}

				tile.DataUrl = dataUrl;
			}

			s.Update(tile);

			return tile;
		}

		public static TileModel EditTile(UserOrganizationModel caller, long tileId, int? h = null, int? w = null, int? x = null, int? y = null, bool? hidden = null, string dataUrl = null, string title = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).EditTile(tileId);

					var o = EditTile(s, perms, tileId, w, h, x, y, hidden, dataUrl, title);

					tx.Commit();
					s.Flush();
					return o;
				}
			}
		}

		public static void EditTiles(UserOrganizationModel caller, long dashboardId, IEnumerable<Controllers.DashboardController.TileVM> model) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).EditDashboard(DashboardType.Standard, dashboardId);

					var editIds = model.Select(x => x.id).ToList();

					var old = s.QueryOver<TileModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(editIds).List().ToList();

					if (!SetUtility.AddRemove(editIds, old.Select(x => x.Id)).AreSame()) {
						throw new PermissionsException("You do not have access to edit some tiles.");
					}

					if (old.Any(x => x.Dashboard.Id != dashboardId)) {
						throw new PermissionsException("You do not have access to edit this dashboard.");
					}

					foreach (var o in old) {
						var found = model.First(x => x.id == o.Id);
						o.X = found.x;
						o.Y = found.y;
						o.Height = found.h;
						o.Width = found.w;
						s.Update(o);
					}


					tx.Commit();
					s.Flush();
				}
			}
		}

		public static List<TileModel> GetTiles(UserOrganizationModel caller, long dashboardId) {
			List<TileModel> tiles;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewDashboard(DashboardType.Standard, dashboardId);

					tiles = GetTiles(s, dashboardId);

				}
			}
			//foreach (var tile in tiles) {
			//	tile.ForUser = null;
			//	tile.Dashboard = null;
			//}
			return tiles;
		}

		public static List<TileModel> GetTiles(ISession s, long dashboardId) {
			return s.QueryOver<TileModel>()
				.Where(x => x.DeleteTime == null && x.Dashboard.Id == dashboardId && x.Hidden == false)
				.List().OrderBy(x => x.Y).ThenBy(x => x.X).ToList();
		}

		public static void RenameDashboard(UserOrganizationModel caller, long dashboardId, string title) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).EditDashboard(DashboardType.Standard, dashboardId);
					var d = s.Get<Dashboard>(dashboardId);
					d.Title = title;
					s.Update(d);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static void DeleteDashboard(UserOrganizationModel caller, long dashboardId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).EditDashboard(DashboardType.Standard, dashboardId);
					var d = s.Get<Dashboard>(dashboardId);
					d.DeleteTime = DateTime.UtcNow;
					s.Update(d);
					tx.Commit();
					s.Flush();
				}
			}
		}


		public class DashboardAndTiles {
			public Dashboard Dashboard { get; set; }
			public List<TileModel> Tiles { get; set; }
			public DashboardAndTiles(Dashboard d) {
				Dashboard = d;
				Tiles = new List<TileModel>();
			}
		}


		public static DashboardAndTiles GenerateDashboard(UserOrganizationModel caller, long id, DashboardType type,int? width) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					switch (type) {
						//case DashboardType.DirectReport:
						//	(perms.ManagesUserOrganizationOrSelf(id);)>
						//	return GenerateUserDashboard(s,id);
						//	break;
						//case DashboardType.Client:
						//	(perms.ViewClient(id);)>
						//	return GenerateClientDashboard(s, id);
						case DashboardType.L10:
							return GenerateL10Dashboard(s, perms, id, width);
						default:
							throw new ArgumentOutOfRangeException("DashboardType", "" + type);
					}
				}
			}
		}


		private static DashboardAndTiles GenerateL10Dashboard(ISession s, PermissionsUtility perms, long id,int? width) {
			perms.ViewL10Recurrence(id);
			var recur = s.Get<L10Recurrence>(id);
			var now = DateTime.UtcNow;

			var d = new Dashboard() {
				Id = -1,
				CreateTime = DateTime.UtcNow,
				Title = recur.Name ?? " L10 Dashboard",
			};
			var o = new DashboardAndTiles(d);

			var measurableRowCounts = L10Accessor.GetMeasurableCount(s, perms, id);
			//2 is for header and footer...
			var scorecardHeight = (int)Math.Ceiling(2.0 + Math.Ceiling((measurableRowCounts.Measurables)*18.0/19.0 + 0.47)+ Math.Round(measurableRowCounts.Dividers/5.0));
			var scorecardCount = (double)scorecardHeight / (double)TILE_HEIGHT;

			if (measurableRowCounts.Measurables == 0) {
				scorecardHeight = 4;
			}

			//Rocks, todos, issues
				var nonScorecardTileHeight = (int)Math.Max(3 * TILE_HEIGHT, Math.Ceiling((5.0 - scorecardCount) * TILE_HEIGHT));
			var issueTileHeight = (int)Math.Ceiling(0.5 * nonScorecardTileHeight);

			width = Math.Max(1156, width ?? 1156);

			int w =Math.Min(4,(int) Math.Floor(width.Value / 580.0+0.33));


			//						  x, y									w										h
			o.Tiles.Add(new TileModel(0, 0, w*3, scorecardHeight, "Scorecard", TileTypeBuilder.L10Scorecard(id), d, now));
			o.Tiles.Add(new TileModel(0, scorecardHeight, w, issueTileHeight, "Rocks", TileTypeBuilder.L10Rocks(id), d, now));

			o.Tiles.Add(new TileModel(0, scorecardHeight + issueTileHeight, w, nonScorecardTileHeight - issueTileHeight, "Stats", TileTypeBuilder.L10Stats(id), d, now));

			o.Tiles.Add(new TileModel(w, scorecardHeight, w, nonScorecardTileHeight, "To-dos", TileTypeBuilder.L10Todos(id), d, now));
			o.Tiles.Add(new TileModel(w*2, scorecardHeight, w, issueTileHeight, "Issues", TileTypeBuilder.L10Issues(id), d, now));
			o.Tiles.Add(new TileModel(w*2, scorecardHeight + issueTileHeight,w, nonScorecardTileHeight - issueTileHeight, "People Headlines", TileTypeBuilder.L10PeopleHeadlines(id), d, now));

			return o;
		}

		public static UserOrganizationModel.PrimaryWorkspaceModel GetHomeDashboardForUser(UserOrganizationModel caller, long userId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.Self(userId);

					var user = s.Get<UserOrganizationModel>(userId);
					var pws = user.PrimaryWorkspace;
					if (pws == null) {
						var primary = DashboardAccessor.GetPrimaryDashboardForUser(s, caller, userId);
						if (primary == null) {
							primary = DashboardAccessor.CreateDashboard(s, caller, null, false, true);
							tx.Commit();
							s.Flush();
						}

						pws = new UserOrganizationModel.PrimaryWorkspaceModel() {
								Type = DashboardType.Standard,
								WorkspaceId = primary.Id,
							};

					}
					return pws;
				}
			}
		}
	}
}
