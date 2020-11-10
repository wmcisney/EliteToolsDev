using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Meeting;
using RadialReview.Models.ViewModels;
using RadialReview.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
	public partial class L10Accessor {

		public static async Task<MeetingSummarySettings> GetMeetingSummarySettings(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetMeetingSummarySettings(s, perms, recurrenceId);
				}
			}
		}

		private static MeetingSummarySettings GetMeetingSummarySettings(ISession s, PermissionsUtility perms, long recurrenceId) {
			perms.ViewL10Recurrence(recurrenceId);

			var settings = s.QueryOver<MeetingSummaryWhoModel>()
				.Where(x => x.DeleteTime == null && x.RecurrenceId == recurrenceId)
				.List().ToList();

			return new MeetingSummarySettings() {
				SendTo = settings,
				RecurrenceId = recurrenceId,
			};
		}

		public static async Task<MeetingSummaryWhoModel> AddMeetingSummarySetting(UserOrganizationModel caller, long recurrenceId, string who, MeetingSummaryWhoType type) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.AdminL10Recurrence(recurrenceId);

					var whoModel = new MeetingSummaryWhoModel() {
						RecurrenceId = recurrenceId,
						Type = type,
						Who = who,
						
					};
					if (type == MeetingSummaryWhoType.UserOrganization) {
						var whoId = long.Parse(who);
						perms.ViewUserOrganization(whoId, false);
					} else if (type ==MeetingSummaryWhoType.Email) {
						if (!Emailer.IsValid(who))
							throw new PermissionsException("Email address invalid");
					}
					s.Save(whoModel);
					tx.Commit();
					s.Flush();
					return whoModel;

				}
			}
		}

		public static async Task<MeetingSummaryWhoModel> EditMeetingSummarySetting(UserOrganizationModel caller, long id, string who, MeetingSummaryWhoType? type) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var whoModel = s.Get<MeetingSummaryWhoModel>(id);
					perms.AdminL10Recurrence(whoModel.RecurrenceId);
					if (who!=null)
						whoModel.Who = who;
					if (type!=null)
						whoModel.Type = type.Value;
					if (type == MeetingSummaryWhoType.UserOrganization) {
						var whoId = long.Parse(who);
						perms.ViewUserOrganization(whoId, false);
					} else if (type == MeetingSummaryWhoType.Email) {
						if (!Emailer.IsValid(who))
							throw new PermissionsException("Email address invalid");
					}
					s.Update(whoModel);
					tx.Commit();
					s.Flush();
					return whoModel;

				}
			}
		}

		public static async Task<MeetingSummaryWhoModel> RemoveMeetingSummarySetting(UserOrganizationModel caller, long id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var whoModel = s.Get<MeetingSummaryWhoModel>(id);
					perms.AdminL10Recurrence(whoModel.RecurrenceId);

					whoModel.DeleteTime = DateTime.UtcNow;

					s.Update(whoModel);
					tx.Commit();
					s.Flush();
					return whoModel;

				}
			}
		}

	}
}