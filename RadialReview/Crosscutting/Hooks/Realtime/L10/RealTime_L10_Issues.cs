﻿using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.Issues;
using System.Threading.Tasks;
using RadialReview.Models.L10;
using Microsoft.AspNet.SignalR;
using RadialReview.Hubs;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models;
using RadialReview.Utilities.Synchronize;
using RadialReview.Exceptions;
using RadialReview.Utilities.RealTime;

namespace RadialReview.Crosscutting.Hooks.Realtime.L10 {
    public class RealTime_L10_Issues : IIssueHook {
        public bool CanRunRemotely() {
            return true;
		}
		public bool AbsorbErrors() {
			return false;
		}
		public HookPriority GetHookPriority() {
            return HookPriority.UI;
        }

        public async Task CreateIssue(ISession s, UserOrganizationModel callr, IssueModel.IssueModel_Recurrence issueRecurrenceModel) {
            var caller = issueRecurrenceModel.CreatedBy;
            var recurrenceId = issueRecurrenceModel.Recurrence.Id;
            var r = s.Get<L10Recurrence>(recurrenceId);

            var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
            var meetingHub = hub.Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(recurrenceId));

            meetingHub.appendIssue(".issues-list", IssuesData.FromIssueRecurrence(issueRecurrenceModel), r.OrderIssueBy);
            var message = "Created issue.";
            var showWhoCreatedDetails = true;
            if (showWhoCreatedDetails) {
                try {
                    if (caller != null && caller.GetFirstName() != null) {
                        message = caller.GetFirstName() + " created an issue.";
                    }
                } catch (Exception) {
                }
            }

            meetingHub.showAlert(message, 1500);

            var updates = new AngularRecurrence(recurrenceId) {
                //Focus = "[data-issue='" + issueRecurrenceModel.Id + "'] input:visible:first"
            };
            updates.IssuesList.Issues = AngularList.Create<AngularIssue>(AngularListType.Add, new[] { new AngularIssue(issueRecurrenceModel) });
            meetingHub.update(new AngularUpdate() { updates });

            if (RealTimeHelpers.GetConnectionString() != null) {
                var me = hub.Clients.Client(RealTimeHelpers.GetConnectionString());
                me.update(new AngularUpdate() { new AngularRecurrence(recurrenceId) {
                    Focus = "[data-issue='" + issueRecurrenceModel.Id + "'] input:visible:first"
                } });
            }
        }

        public async Task UpdateIssue(ISession s, UserOrganizationModel caller, IssueModel.IssueModel_Recurrence issueRecurrence, IIssueHookUpdates updates) {
            var updatesText = new List<string>();
            var recurrenceId = issueRecurrence.Recurrence.Id;
            var issueRecurrenceId = issueRecurrence.Id;
            var now = DateTime.UtcNow;

            var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
            var group = hub.Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(recurrenceId), RealTimeHelpers.GetConnectionString());

            if (updates.MessageChanged)
                group.updateIssueMessage(issueRecurrenceId, issueRecurrence.Issue.Message);

            if (updates.OwnerChanged)
                group.updateIssueOwner(issueRecurrenceId, issueRecurrence.Owner.Id, issueRecurrence.Owner.GetName(), issueRecurrence.Owner.ImageUrl(true, ImageSize._32));

            if (updates.PriorityChanged)
                group.updateIssuePriority(issueRecurrenceId, issueRecurrence.Priority);

			if (updates.RankChanged) {
				group.updateIssueRank(issueRecurrenceId, issueRecurrence.Rank, true);

				if (issueRecurrence.Rank == 3) {
					//group.floatTopThreeIssues(issueRecurrence.Id);
				}
			}

            if (updates.CompletionChanged) {
                var added = issueRecurrence.CloseTime == null;
                var completed = issueRecurrence.CloseTime != null;

                var others = s.QueryOver<IssueModel.IssueModel_Recurrence>()
                    .Where(x => x.DeleteTime == null && x.Issue.Id == issueRecurrence.Issue.Id)
                    .List().ToList();

                foreach (var o in others) {
                    using (var rt = RealTimeUtility.Create()) {
                        rt.UpdateRecurrences(o.Recurrence.Id).AddLowLevelAction(x => x.updateModedIssueSolve(o.Id, completed));
                        var recur = new AngularRecurrence(o.Recurrence.Id);

                        var issue = new AngularIssue(issueRecurrence);
                        if(issue.CloseTime == null) {
                            issue.CloseTime = Removed.Date();
                        }
                        recur.IssuesList.Issues = AngularList.CreateFrom(added ? AngularListType.ReplaceIfNewer : AngularListType.Remove, issue);
                        rt.UpdateRecurrences(o.Recurrence.Id).Update(recur);
                    }
                }
            }

            if (updates.AwaitingSolveChanged) {

				var name = "";
				if (updates.MovedToRecurrence.HasValue) {
					name = s.Get<L10Recurrence>(updates.MovedToRecurrence.Value).NotNull(x=>x.Name);
				}

                group.updateIssueAwaitingSolve(issueRecurrence.Id, issueRecurrence.AwaitingSolve);
				group.setIssueMoveLocation(issueRecurrence.Id, name);

			}

			group.update(new AngularUpdate() { new AngularIssue(issueRecurrence) });
        }
    }
}