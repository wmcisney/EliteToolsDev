using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Models.VideoConference;
using RadialReview.Nhibernate;
using RadialReview.Utilities;
//using ListExtensions = WebGrease.Css.Extensions.ListExtensions;
//using System.Web.WebPages.Html;
using RadialReview.Utilities.RealTime;
using System;

namespace RadialReview.Accessors {
	public partial class L10Accessor : BaseAccessor {

		#region Video

		public static void SetVideoProvider(UserOrganizationModel caller, long recurrenceId, long vcProviderId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create()) {
						var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

						var found = s.Get<AbstractVCProvider>(vcProviderId);
						if (found.DeleteTime != null) {
							throw new PermissionsException("Video Provider does not exist");
						}

						perms.ViewUserOrganization(found.OwnerId, false);

						var user = s.Get<UserOrganizationModel>(found.OwnerId);
						if (user.DeleteTime != null) {
							throw new PermissionsException("Owner of the Video Conference Provider no longer exists");
						}

						found.LastUsed = DateTime.UtcNow;
						s.Update(found);

						var l10Meeting = _GetCurrentL10Meeting(s, perms, recurrenceId, true);
						if (l10Meeting != null) {
							l10Meeting.SelectedVideoProvider = found;

							s.Update(l10Meeting);
						}

						rt.UpdateRecurrences(recurrenceId)
						  .AddLowLevelAction(x => {
							  var resolved = (AbstractVCProvider)s.GetSessionImplementation().PersistenceContext.Unproxy(found);
							  x.setSelectedVideoProvider(resolved);
						  });

						tx.Commit();
						s.Flush();
					}
				}
			}
		}

		public static void SetJoinedVideo(UserOrganizationModel caller, long userId, long recurrenceId, long vcProviderId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create()) {
						var perms = PermissionsUtility.Create(s, caller)
							.ViewL10Recurrence(recurrenceId)
							.Self(userId);

						var found = s.Get<AbstractVCProvider>(vcProviderId);
						if (found.DeleteTime != null) {
							throw new PermissionsException("Video Provider does not exist");
						}

						perms.ViewUserOrganization(found.OwnerId, false);

						var user = s.Get<UserOrganizationModel>(found.OwnerId);
						if (user.DeleteTime != null) {
							throw new PermissionsException("Owner of the Video Conference Provider no longer exists");
						}

						found.LastUsed = DateTime.UtcNow;
						s.Update(found);

						var link = new JoinedVideo() {
							RecurrenceId = recurrenceId,
							UserId = userId,
							VideoProvider = vcProviderId,
						};

						var recur = s.Get<L10Recurrence>(recurrenceId);
						recur.SelectedVideoProviderId = found.Id;
						s.Update(recur);

						var l10Meeting = _GetCurrentL10Meeting(s, perms, recurrenceId, true);
						if (l10Meeting != null) {
							link.MeetingId = l10Meeting.Id;
						}

						try {
							s.Unproxy(found);
						} catch (Exception e) {
							log.Error("Error unproxy video provider", e);
						}

						try {
							rt.UpdateRecurrences(recurrenceId).AddLowLevelAction(x => {
								x.setSelectedVideoProvider(found);
							});
						} catch (Exception e) {
							log.Error(e);
						}


						s.Save(link);

						tx.Commit();
						s.Flush();
					}
				}
			}
		}


		#endregion
	}
}
