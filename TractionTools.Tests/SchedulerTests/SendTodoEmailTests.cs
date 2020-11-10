using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Controllers;
using RadialReview.Crosscutting.ScheduledJobs;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Enums;
using RadialReview.Models.Tasks;
using RadialReview.Models.Todo;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TractionTools.Tests.TestUtils;
using TractionTools.Tests.Utilities;
using static RadialReview.Crosscutting.ScheduledJobs.TodoEmailsScheduler;
using static TractionTools.Tests.Permissions.BasePermissionsTest;

namespace TractionTools.Tests.SchedulerTests {
	[TestClass]
	public class SendTodoEmailTests : BaseTest {

		[TestMethod]
		[TestCategory("Scheduler")]
		public async Task TestRedirect() {
			var result = (string)await TaskAccessor.d_ExecuteTaskFunc(null, new ScheduledTask() { Url = "https://jigsaw.w3.org/HTTP/300/302.html" }, DateTime.MinValue);
			Assert.IsTrue(result.Contains("Redirect test page"));

		}


		[TestMethod]
		[TestCategory("Scheduler")]
		public async Task TestTodoQuery() {
			using (HibernateSession.SetDatabaseEnv_TestOnly(Env.local_mysql)) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {

						var found = TodoEmailHelpers._QueryTodo(s, 10, /*13, 1,*/ new DateTime(2017, 10, 27), new DateTime(2017, 11, 4), new DateTime(2017, 11, 4));

						int a = 0;

					}
				}
			}
		}

		[TestMethod]
		[TestCategory("Scheduler")]
		public async Task TestEmailOverdue() {

			var org = await OrgUtil.CreateOrganization("asdf");


			await org.RegisterUser(org.Manager);
			var l10_1 = await L10Utility.CreateRecurrence("afds1");
			var l10_2 = await L10Utility.CreateRecurrence("afds2");
			var l10_3 = await L10Utility.CreateRecurrence("afds3");
			var l10_4 = await L10Utility.CreateRecurrence("afds4");
			var l10_5 = await L10Utility.CreateRecurrence("afds5");
			var l10_6 = await L10Utility.CreateRecurrence("afds6");
			var l10_7 = await L10Utility.CreateRecurrence("afds7");
			var l10_8 = await L10Utility.CreateRecurrence("afds8");
			var l10_9 = await L10Utility.CreateRecurrence("afds9");
			var l10_10 = await L10Utility.CreateRecurrence("afds10");

			var accountableUser = org.Manager.Id;
			var sendTime = 19;
			UserModel manager = null;

			DbCommit(s => {
				manager = s.Get<UserModel>(org.Manager.User.Id);
				var orgR = s.Get<OrganizationModel>(org.Id);

				orgR._Settings.TimeZoneId = null;
				s.Update(orgR);

				manager.SendTodoTime = sendTime;
				s.Update(manager);

				//s.Save(new TodoModel() {
				//	Ordering = -585277,
				//	CreateTime = new DateTime(2018, 05, 17, 19, 22, 15),
				//	DeleteTime = null,
				//	CompleteTime = null,
				//	ForModel = "TodoModel",
				//	ForModelId = -1,
				//	Message = "Email to team on Google drive project",
				//	Details = null,
				//	CreatedById = accountableUser,
				//	AccountableUserId = accountableUser,
				//	CreatedDuringMeetingId = null,
				//	ForRecurrenceId = null,
				//	OrganizationId = org.Id,
				//	DueDate = new DateTime(2018, 09, 19, 03, 59, 59),
				//	PadId = "p",
				//	CompleteDuringMeetingId = null,
				//	ClearedInMeeting = null,
				//	TodoType = TodoType.Personal,
				//	CloseTime = null,
				//});

				//s.Save(new TodoModel() {
				//	Id = 896384,
				//	Ordering = -896384,
				//	CreateTime = new DateTime(2018, 10, 01, 21, 09, 36),
				//	DeleteTime = null,
				//	CompleteTime = null,
				//	ForModel = "TodoModel",
				//	ForModelId = -1,
				//	Message = "test",
				//	Details = null,
				//	CreatedById = accountableUser,
				//	AccountableUserId = accountableUser,
				//	CreatedDuringMeetingId = null,
				//	ForRecurrenceId = l10.Id,
				//	OrganizationId = org.Id,
				//	DueDate = new DateTime(2018, 09, 21, 04, 59, 59),
				//	PadId = "p",
				//	CompleteDuringMeetingId = null,
				//	ClearedInMeeting = null,
				//	TodoType = TodoType.Recurrence,
				//	CloseTime = null,
				//});

				//s.Save(new TodoModel() { Ordering = 0, CreateTime = new DateTime(2018, 03, 14, 18, 02, 55), DeleteTime = null, CompleteTime = new DateTime(2018, 03, 14, 18, 05, 29), ForModel = "TodoModel", ForModelId = -1, Message = "null", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = l10_3, OrganizationId = org.Id, DueDate = new DateTime(2018, 03, 16, 07, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 03, 15, 17, 03, 08), });
				//s.Save(new TodoModel() { Ordering = 1, CreateTime = new DateTime(2018, 08, 22, 12, 05, 09), DeleteTime = null, CompleteTime = new DateTime(2018, 09, 18, 02, 53, 13), ForModel = "TodoModel", ForModelId = -1, Message = "Bi-Weekly 1-on-1", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = l10_5, OrganizationId = org.Id, DueDate = new DateTime(2018, 08, 24, 03, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 09, 18, 02, 54, 26), });
				//s.Save(new TodoModel() { Ordering = 1, CreateTime = new DateTime(2018, 08, 22, 12, 06, 35), DeleteTime = null, CompleteTime = new DateTime(2018, 08, 31, 15, 26, 20), ForModel = "TodoModel", ForModelId = -1, Message = "Fill out roll call survey", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = l10_5, OrganizationId = org.Id, DueDate = new DateTime(2018, 08, 23, 23, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 09, 13, 12, 44, 29), });
				//s.Save(new TodoModel() { Ordering = 2, CreateTime = new DateTime(2018, 04, 05, 02, 06, 27), DeleteTime = null, CompleteTime = new DateTime(2018, 04, 25, 16, 33, 22), ForModel = "TodoModel", ForModelId = -1, Message = "Compare Rocks in Traction to Rocks Doc ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = l10_3, OrganizationId = org.Id, DueDate = new DateTime(2018, 04, 18, 03, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = null, });
				//s.Save(new TodoModel() { Ordering = 3, CreateTime = new DateTime(2018, 03, 14, 18, 01, 54), DeleteTime = null, CompleteTime = new DateTime(2018, 07, 14, 22, 13, 28), ForModel = "TodoModel", ForModelId = -1, Message = "Continue learning Traction Tools ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = l10_3, OrganizationId = org.Id, DueDate = new DateTime(2018, 04, 18, 03, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = null, });
				//s.Save(new TodoModel() { Ordering = 4, CreateTime = new DateTime(2018, 03, 20, 19, 54, 09), DeleteTime = null, CompleteTime = new DateTime(2018, 04, 03, 16, 24, 54), ForModel = "TodoModel", ForModelId = -1, Message = "Convert meetings from old format to Traction ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = l10_3, OrganizationId = org.Id, DueDate = new DateTime(2018, 03, 28, 19, 54, 08), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = null, });
				//s.Save(new TodoModel() { Ordering = 5, CreateTime = new DateTime(2018, 03, 14, 18, 01, 42), DeleteTime = null, CompleteTime = new DateTime(2018, 03, 20, 19, 53, 42), ForModel = "TodoModel", ForModelId = -1, Message = "Discuss Weekly Challenges", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = l10_3, OrganizationId = org.Id, DueDate = new DateTime(2018, 03, 23, 07, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 03, 26, 14, 49, 17), });
				//s.Save(new TodoModel() { Ordering = 5, CreateTime = new DateTime(2018, 03, 19, 18, 53, 05), DeleteTime = null, CompleteTime = new DateTime(2018, 03, 19, 19, 40, 35), ForModel = "TodoModel", ForModelId = -1, Message = "null", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = l10_6, OrganizationId = org.Id, DueDate = new DateTime(2018, 03, 19, 07, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 03, 20, 16, 39, 38), });
				//s.Save(new TodoModel() { Ordering = 5, CreateTime = new DateTime(2018, 04, 10, 20, 13, 42), DeleteTime = null, CompleteTime = new DateTime(2018, 04, 25, 16, 33, 12), ForModel = "TodoModel", ForModelId = -1, Message = "Discuss meeting reminder emails w/ Jason", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = l10_3, OrganizationId = org.Id, DueDate = new DateTime(2018, 04, 17, 19, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = null, });
				//s.Save(new TodoModel() { Ordering = 6, CreateTime = new DateTime(2018, 04, 04, 18, 10, 31), DeleteTime = null, CompleteTime = new DateTime(2018, 04, 25, 16, 33, 17), ForModel = "TodoModel", ForModelId = -1, Message = "Discuss Minutes format w/ Jason", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = l10_3, OrganizationId = org.Id, DueDate = new DateTime(2018, 04, 18, 03, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = null, });
				//s.Save(new TodoModel() { Ordering = 6, CreateTime = new DateTime(2018, 03, 14, 18, 01, 04), DeleteTime = null, CompleteTime = new DateTime(2018, 03, 20, 19, 53, 36), ForModel = "TodoModel", ForModelId = -1, Message = "Discuss Weekly Victories", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = l10_3, OrganizationId = org.Id, DueDate = new DateTime(2018, 03, 23, 07, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 03, 26, 14, 49, 17), });
				//s.Save(new TodoModel() { Ordering = 8, CreateTime = new DateTime(2018, 07, 09, 18, 28, 24), DeleteTime = null, CompleteTime = new DateTime(2018, 07, 14, 22, 13, 35), ForModel = "TodoModel", ForModelId = -1, Message = "Set up appointment for leaders to complete job pros next week", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = l10_1, OrganizationId = org.Id, DueDate = new DateTime(2018, 07, 16, 23, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 07, 16, 19, 05, 50), });
				//s.Save(new TodoModel() { Ordering = 8, CreateTime = new DateTime(2018, 04, 23, 18, 07, 47), DeleteTime = null, CompleteTime = new DateTime(2018, 04, 25, 16, 33, 24), ForModel = "TodoModel", ForModelId = -1, Message = "Remove individual rocks from HR meeting", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = l10_1, OrganizationId = org.Id, DueDate = new DateTime(2018, 04, 30, 23, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 04, 30, 19, 04, 18), });
				//s.Save(new TodoModel() { Ordering = 10, CreateTime = new DateTime(2018, 04, 30, 16, 48, 36), DeleteTime = null, CompleteTime = new DateTime(2018, 06, 18, 13, 21, 55), ForModel = "TodoModel", ForModelId = -1, Message = "Interview Department Heads on Meeting Technology Needs", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = l10_2, OrganizationId = org.Id, DueDate = new DateTime(2018, 06, 01, 03, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = null, });
				//s.Save(new TodoModel() { Ordering = 10, CreateTime = new DateTime(2018, 04, 10, 16, 17, 27), DeleteTime = null, CompleteTime = new DateTime(2018, 04, 10, 19, 22, 26), ForModel = "TodoModel", ForModelId = -1, Message = "Add Matt to call", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = l10_6, OrganizationId = org.Id, DueDate = new DateTime(2018, 04, 17, 19, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 04, 17, 16, 40, 49), });
				//s.Save(new TodoModel() { Ordering = 14, CreateTime = new DateTime(2018, 04, 02, 19, 15, 08), DeleteTime = null, CompleteTime = new DateTime(2018, 04, 03, 16, 24, 49), ForModel = "TodoModel", ForModelId = -1, Message = "Add Lisa Wenner to meeting ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = l10_7, OrganizationId = org.Id, DueDate = new DateTime(2018, 04, 03, 04, 00, 00), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 05, 07, 19, 51, 18), });
				//s.Save(new TodoModel() { Ordering = 43, CreateTime = new DateTime(2018, 06, 09, 04, 03, 20), DeleteTime = null, CompleteTime = new DateTime(2018, 07, 14, 22, 13, 46), ForModel = "TodoModel", ForModelId = -1, Message = "Expand Rock Form to Include rocks by employee with a tab for each company/department", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = l10_4, OrganizationId = org.Id, DueDate = new DateTime(2018, 06, 19, 23, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = null, });
				//s.Save(new TodoModel() { Ordering = -484299, CreateTime = new DateTime(2018, 03, 29, 03, 50, 15), DeleteTime = null, CompleteTime = new DateTime(2018, 04, 04, 17, 54, 52), ForModel = "TodoModel", ForModelId = -1, Message = "Build Amy's meeting", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = null, OrganizationId = org.Id, DueDate = new DateTime(2018, 03, 30, 03, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Personal, CloseTime = null, });
				//s.Save(new TodoModel() { Ordering = -484298, CreateTime = new DateTime(2018, 03, 29, 03, 49, 31), DeleteTime = null, CompleteTime = new DateTime(2018, 03, 30, 04, 33, 43), ForModel = "TodoModel", ForModelId = -1, Message = "Finish Meeting Notes ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = null, OrganizationId = org.Id, DueDate = new DateTime(2018, 03, 30, 03, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Personal, CloseTime = null, });
				//s.Save(new TodoModel() { Ordering = -484300, CreateTime = new DateTime(2018, 03, 29, 03, 50, 53), DeleteTime = null, CompleteTime = new DateTime(2018, 04, 03, 16, 24, 48), ForModel = "TodoModel", ForModelId = -1, Message = "Clean Appfolio apps", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = null, OrganizationId = org.Id, DueDate = new DateTime(2018, 03, 30, 03, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Personal, CloseTime = null, });
				//s.Save(new TodoModel() { Ordering = -585277, CreateTime = new DateTime(2018, 05, 17, 19, 22, 15), DeleteTime = null, CompleteTime = null, ForModel = "TodoModel", ForModelId = -1, Message = "Email to team on Google drive project", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = null, OrganizationId = org.Id, DueDate = new DateTime(2018, 09, 19, 03, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Personal, CloseTime = null, });
				//s.Save(new TodoModel() { Ordering = -568530, CreateTime = new DateTime(2018, 05, 10, 13, 26, 55), DeleteTime = null, CompleteTime = new DateTime(2018, 07, 14, 22, 13, 43), ForModel = "TodoModel", ForModelId = -1, Message = "Review all rocks for D4G members and make sure they are all entered", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = null, OrganizationId = org.Id, DueDate = new DateTime(2018, 05, 17, 23, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Personal, CloseTime = null, });
				//s.Save(new TodoModel() { Ordering = -568537, CreateTime = new DateTime(2018, 05, 10, 13, 28, 54), DeleteTime = null, CompleteTime = new DateTime(2018, 05, 15, 14, 24, 40), ForModel = "TodoModel", ForModelId = -1, Message = "Finish Meeting Recaps-HR, Tech, Asset & Dev", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = null, OrganizationId = org.Id, DueDate = new DateTime(2018, 05, 11, 03, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Personal, CloseTime = null, });
				//s.Save(new TodoModel() { Ordering = -568546, CreateTime = new DateTime(2018, 05, 10, 13, 30, 28), DeleteTime = null, CompleteTime = new DateTime(2018, 05, 15, 14, 24, 33), ForModel = "TodoModel", ForModelId = -1, Message = "Finish Peak Performance ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = null, OrganizationId = org.Id, DueDate = new DateTime(2018, 05, 10, 04, 00, 00), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Personal, CloseTime = null, });
				//s.Save(new TodoModel() { Ordering = -568539, CreateTime = new DateTime(2018, 05, 10, 13, 29, 39), DeleteTime = null, CompleteTime = new DateTime(2018, 06, 18, 13, 21, 14), ForModel = "TodoModel", ForModelId = -1, Message = "Meditate & Pray", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = null, OrganizationId = org.Id, DueDate = new DateTime(2018, 05, 17, 23, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Personal, CloseTime = null, });
				//s.Save(new TodoModel() { Ordering = 42, CreateTime = new DateTime(2018, 06, 09, 04, 04, 59), DeleteTime = null, CompleteTime = new DateTime(2018, 08, 07, 13, 20, 10), ForModel = "TodoModel", ForModelId = -1, Message = "Build Out All New Rocks in Traction Tools for all users and companies", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = l10_4, OrganizationId = org.Id, DueDate = new DateTime(2018, 06, 30, 23, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = null, });
				//s.Save(new TodoModel() { Ordering = 4, CreateTime = new DateTime(2018, 03, 19, 18, 52, 54), DeleteTime = null, CompleteTime = new DateTime(2018, 03, 19, 19, 40, 35), ForModel = "TodoModel", ForModelId = -1, Message = "null", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = l10_6, OrganizationId = org.Id, DueDate = new DateTime(2018, 03, 19, 07, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 03, 20, 16, 39, 38), });
				//s.Save(new TodoModel() { Ordering = 3, CreateTime = new DateTime(2018, 08, 22, 12, 04, 30), DeleteTime = null, CompleteTime = new DateTime(2018, 08, 31, 15, 26, 09), ForModel = "TodoModel", ForModelId = -1, Message = "Update Rocks in Traction", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = l10_5, OrganizationId = org.Id, DueDate = new DateTime(2018, 08, 23, 23, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 09, 13, 12, 44, 29), });
				//s.Save(new TodoModel() { Ordering = 1, CreateTime = new DateTime(2018, 03, 14, 18, 02, 28), DeleteTime = null, CompleteTime = new DateTime(2018, 03, 15, 16, 45, 27), ForModel = "TodoModel", ForModelId = -1, Message = "Create Meeting in Traction ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = l10_3, OrganizationId = org.Id, DueDate = new DateTime(2018, 03, 16, 07, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 03, 15, 17, 03, 08), });
				//s.Save(new TodoModel() { Ordering = -702395, CreateTime = new DateTime(2018, 07, 14, 23, 11, 06), DeleteTime = null, CompleteTime = new DateTime(2018, 08, 14, 20, 42, 37), ForModel = "TodoModel", ForModelId = -1, Message = "Look into adding direct reports tile on end user.", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = null, OrganizationId = org.Id, DueDate = new DateTime(2018, 07, 21, 23, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Personal, CloseTime = null, });
				//s.Save(new TodoModel() { Ordering = 6, CreateTime = new DateTime(2018, 03, 19, 18, 57, 42), DeleteTime = null, CompleteTime = new DateTime(2018, 03, 19, 19, 40, 36), ForModel = "TodoModel", ForModelId = -1, Message = "null", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = l10_6, OrganizationId = org.Id, DueDate = new DateTime(2018, 03, 19, 07, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 03, 20, 16, 39, 38), });
				//s.Save(new TodoModel() { Ordering = 7, CreateTime = new DateTime(2018, 04, 05, 02, 06, 28), DeleteTime = null, CompleteTime = new DateTime(2018, 04, 10, 19, 17, 08), ForModel = "TodoModel", ForModelId = -1, Message = "Edit Jason's meetings /w links", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = l10_3, OrganizationId = org.Id, DueDate = new DateTime(2018, 04, 13, 06, 06, 27), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = null, });
				//s.Save(new TodoModel() { Ordering = 2, CreateTime = new DateTime(2018, 08, 22, 12, 05, 54), DeleteTime = null, CompleteTime = new DateTime(2018, 08, 31, 15, 26, 20), ForModel = "TodoModel", ForModelId = -1, Message = "Get halfway though the book", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = l10_5, OrganizationId = org.Id, DueDate = new DateTime(2018, 08, 23, 23, 59, 59), PadId = "padId", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 09, 13, 12, 44, 29), });
				//s.Save(new TodoModel() { Ordering = -585277, CreateTime = new DateTime(2018, 05, 17, 19, 22, 15), DeleteTime = null, CompleteTime = null, ForModel = "TodoModel", ForModelId = -1, Message = "Email to team on Google drive project", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, ForRecurrenceId = null, OrganizationId = org.Id, DueDate = new DateTime(2018, 09, 19, 03, 59, 59), PadId = "FirstOne", ClearedInMeeting = null, TodoType = TodoType.Personal, CloseTime = null, });

				//s.Save(new TodoModel() {Ordering = -895287,CreateTime = new DateTime(2018, 10, 01,18,57,46),DeleteTime = null,CompleteTime = null,ForModel = "IssueModel",ForModelId = 697005,Message = "Look into uniform options",Details = "RESOLVE ISSUE: DLP Store",CreatedById = accountableUser,AccountableUserId = accountableUser,ForRecurrenceId = l10_5,OrganizationId = org.Id,DueDate = new DateTime(2018,10,09,03,59,59),PadId = "padid",CompleteDuringMeetingId = null,ClearedInMeeting = null,TodoType = TodoType.Recurrence,CloseTime = null,});

				s.Save(new TodoModel() { Id = 457080, Ordering = 6, CreateTime = new DateTime(2018, 03, 14, 18, 01, 04), DeleteTime = null, CompleteTime = new DateTime(2018, 03, 20, 19, 53, 36), ForModel = " TodoModel  ", ForModelId = -1, Message = " Discuss Weekly Victories ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_1, OrganizationId = org.Id, DueDate = new DateTime(2018, 03, 23, 07, 59, 59),																				PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 03, 26, 14, 49, 17), });
				s.Save(new TodoModel() { Id = 457083, Ordering = 5, CreateTime = new DateTime(2018, 03, 14, 18, 01, 42), DeleteTime = null, CompleteTime = new DateTime(2018, 03, 20, 19, 53, 42), ForModel = " TodoModel  ", ForModelId = -1, Message = " Discuss Weekly Challenges  ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_1, OrganizationId = org.Id, DueDate = new DateTime(2018, 03, 23, 07, 59, 59),						 													PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 03, 26, 14, 49, 17), });
				s.Save(new TodoModel() { Id = 457085, Ordering = 3, CreateTime = new DateTime(2018, 03, 14, 18, 01, 54), DeleteTime = null, CompleteTime = new DateTime(2018, 07, 14, 22, 13, 28), ForModel = " TodoModel  ", ForModelId = -1, Message = " Continue learning Traction Tools ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_1, OrganizationId = org.Id, DueDate = new DateTime(2018, 04, 18, 03, 59, 59), 																	PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = null, });
				s.Save(new TodoModel() { Id = 457089, Ordering = 1, CreateTime = new DateTime(2018, 03, 14, 18, 02, 28), DeleteTime = null, CompleteTime = new DateTime(2018, 03, 15, 16, 45, 27), ForModel = " TodoModel  ", ForModelId = -1, Message = " Create Meeting in Traction ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_1, OrganizationId = org.Id, DueDate = new DateTime(2018, 03, 16, 07, 59, 59), 																			PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 03, 15, 17, 03, 08), });
				s.Save(new TodoModel() { Id = 457092, Ordering = 0, CreateTime = new DateTime(2018, 03, 14, 18, 02, 55), DeleteTime = null, CompleteTime = new DateTime(2018, 03, 14, 18, 05, 29), ForModel = " TodoModel  ", ForModelId = -1, Message = "null", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_1, OrganizationId = org.Id, DueDate = new DateTime(2018, 03, 16, 07, 59, 59), 																									PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 03, 15, 17, 03, 08), });
				s.Save(new TodoModel() { Id = 463927, Ordering = 4, CreateTime = new DateTime(2018, 03, 19, 18, 52, 54), DeleteTime = null, CompleteTime = new DateTime(2018, 03, 19, 19, 40, 35), ForModel = " TodoModel  ", ForModelId = -1, Message = "null", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_2, OrganizationId = org.Id, DueDate = new DateTime(2018, 03, 19, 07, 59, 59), 																									PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 03, 20, 16, 39, 38), });
				s.Save(new TodoModel() { Id = 463928, Ordering = 5, CreateTime = new DateTime(2018, 03, 19, 18, 53, 05), DeleteTime = null, CompleteTime = new DateTime(2018, 03, 19, 19, 40, 35), ForModel = " TodoModel  ", ForModelId = -1, Message = "null", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_2, OrganizationId = org.Id, DueDate = new DateTime(2018, 03, 19, 07, 59, 59), 																									PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 03, 20, 16, 39, 38), });
				s.Save(new TodoModel() { Id = 463945, Ordering = 6, CreateTime = new DateTime(2018, 03, 19, 18, 57, 42), DeleteTime = null, CompleteTime = new DateTime(2018, 03, 19, 19, 40, 36), ForModel = " TodoModel  ", ForModelId = -1, Message = "null", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_2, OrganizationId = org.Id, DueDate = new DateTime(2018, 03, 19, 07, 59, 59), 																									PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 03, 20, 16, 39, 38), });
				s.Save(new TodoModel() { Id = 465018, Ordering = 9, CreateTime = new DateTime(2018, 03, 20, 01, 42, 34), DeleteTime = null, CompleteTime = new DateTime(2018, 03, 20, 01, 55, 21), ForModel = " TodoModel  ", ForModelId = -1, Message = " n/a  ", Details =" AOS ? " , CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_3, OrganizationId = org.Id, DueDate = new DateTime(2018, 03, 18, 08, 00, 00), 																							PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 03, 20, 15, 44, 23), });
				s.Save(new TodoModel() { Id = 467676, Ordering = 4, CreateTime = new DateTime(2018, 03, 20, 19, 54, 09), DeleteTime = null, CompleteTime = new DateTime(2018, 04, 03, 16, 24, 54), ForModel = " TodoModel  ", ForModelId = -1, Message = " Convert meetings from old format to Traction ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_1, OrganizationId = org.Id, DueDate = new DateTime(2018, 03, 28, 19, 54, 08), 														PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = null, });
				s.Save(new TodoModel() { Id = 484298, Ordering = -484298, CreateTime = new DateTime(2018, 03, 29, 03, 49, 31), DeleteTime = null, CompleteTime = new DateTime(2018, 03, 30, 04, 33, 43), ForModel = " TodoModel  ", ForModelId = -1, Message = " Finish Meeting Notes ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = null, OrganizationId = org.Id, DueDate = new DateTime(2018, 03, 30, 03, 59, 59), 																			PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Personal, CloseTime = null, });
				s.Save(new TodoModel() { Id = 484299, Ordering = -484299, CreateTime = new DateTime(2018, 03, 29, 03, 50, 15), DeleteTime = null, CompleteTime = new DateTime(2018, 04, 04, 17, 54, 52), ForModel = " TodoModel  ", ForModelId = -1, Message = " Build Amy's meeting  ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = null, OrganizationId = org.Id, DueDate = new DateTime(2018, 03, 30, 03, 59, 59), 																			PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Personal, CloseTime = null, });
				s.Save(new TodoModel() { Id = 484300, Ordering = -484300, CreateTime = new DateTime(2018, 03, 29, 03, 50, 53), DeleteTime = null, CompleteTime = new DateTime(2018, 04, 03, 16, 24, 48), ForModel = " TodoModel  ", ForModelId = -1, Message = " Clean Appfolio apps  ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = null, OrganizationId = org.Id, DueDate = new DateTime(2018, 03, 30, 03, 59, 59), 																			PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Personal, CloseTime = null, });
				s.Save(new TodoModel() { Id = 490100, Ordering = 14, CreateTime = new DateTime(2018, 04, 02, 19, 15, 08), DeleteTime = null, CompleteTime = new DateTime(2018, 04, 03, 16, 24, 49), ForModel = " TodoModel  ", ForModelId = -1, Message = " Add Lisa Wenner to meeting ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_4, OrganizationId = org.Id, DueDate = new DateTime(2018, 04, 03, 04, 00, 00), 																		PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 05, 07, 19, 51, 18), });
				s.Save(new TodoModel() { Id = 495681, Ordering = 6, CreateTime = new DateTime(2018, 04, 04, 18, 10, 31), DeleteTime = null, CompleteTime = new DateTime(2018, 04, 25, 16, 33, 17), ForModel = " TodoModel  ", ForModelId = -1, Message = " Discuss Minutes format w/ Jason  ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_1, OrganizationId = org.Id, DueDate = new DateTime(2018, 04, 18, 03, 59, 59), 																	PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = null, });
				s.Save(new TodoModel() { Id = 496741, Ordering = 2, CreateTime = new DateTime(2018, 04, 05, 02, 06, 27), DeleteTime = null, CompleteTime = new DateTime(2018, 04, 25, 16, 33, 22), ForModel = " TodoModel  ", ForModelId = -1, Message = " Compare Rocks in Traction to Rocks Doc ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_1, OrganizationId = org.Id, DueDate = new DateTime(2018, 04, 18, 03, 59, 59), 																PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = null, });
				s.Save(new TodoModel() { Id = 496742, Ordering = 7, CreateTime = new DateTime(2018, 04, 05, 02, 06, 28), DeleteTime = null, CompleteTime = new DateTime(2018, 04, 10, 19, 17, 08), ForModel = " TodoModel  ", ForModelId = -1, Message = " Edit Jason's meetings /w links ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_1, OrganizationId = org.Id, DueDate = new DateTime(2018, 04, 13, 06, 06, 27), 																		PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = null, });
				s.Save(new TodoModel() { Id = 505898, Ordering = 10, CreateTime = new DateTime(2018, 04, 10, 16, 17, 27), DeleteTime = null, CompleteTime = new DateTime(2018, 04, 10, 19, 22, 26), ForModel = " TodoModel  ", ForModelId = -1, Message = " Add Matt to call ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_2, OrganizationId = org.Id, DueDate = new DateTime(2018, 04, 17, 19, 59, 59), 																					PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 04, 17, 16, 40, 49), });
				s.Save(new TodoModel() { Id = 507223, Ordering = 5, CreateTime = new DateTime(2018, 04, 10, 20, 13, 42), DeleteTime = null, CompleteTime = new DateTime(2018, 04, 25, 16, 33, 12), ForModel = " TodoModel  ", ForModelId = -1, Message = " Discuss meeting reminder emails w/ Jason ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_1, OrganizationId = org.Id, DueDate = new DateTime(2018, 04, 17, 19, 59, 59), 															PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = null, });
				s.Save(new TodoModel() { Id = 531347, Ordering = 8, CreateTime = new DateTime(2018, 04, 23, 18, 07, 47), DeleteTime = null, CompleteTime = new DateTime(2018, 04, 25, 16, 33, 24), ForModel = " TodoModel  ", ForModelId = -1, Message = " Remove individual rocks from HR meeting  ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_5, OrganizationId = org.Id, DueDate = new DateTime(2018, 04, 30, 23, 59, 59), 															PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 04, 30, 19, 04, 18), });
				s.Save(new TodoModel() { Id = 544987, Ordering = 10, CreateTime = new DateTime(2018, 04, 30, 16, 48, 36), DeleteTime = null, CompleteTime = new DateTime(2018, 06, 18, 13, 21, 55), ForModel = " TodoModel  ", ForModelId = -1, Message = " Interview Department Heads on Meeting Technology Needs ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_10, OrganizationId = org.Id, DueDate = new DateTime(2018, 06, 01, 03, 59, 59), 											PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = null, });
				s.Save(new TodoModel() { Id = 568530, Ordering = -568530, CreateTime = new DateTime(2018, 05, 10, 13, 26, 55), DeleteTime = null, CompleteTime = new DateTime(2018, 07, 14, 22, 13, 43), ForModel = " TodoModel  ", ForModelId = -1, Message = " Review all rocks for D4G members and make sure they are all entered  ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = null, OrganizationId = org.Id, DueDate = new DateTime(2018, 05, 17, 23, 59, 59), 							PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Personal, CloseTime = null, });
				s.Save(new TodoModel() { Id = 568537, Ordering = -568537, CreateTime = new DateTime(2018, 05, 10, 13, 28, 54), DeleteTime = null, CompleteTime = new DateTime(2018, 05, 15, 14, 24, 40), ForModel = " TodoModel  ", ForModelId = -1, Message = " Finish Meeting Recaps-HR, Tech, Asset & Dev  ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = null, OrganizationId = org.Id, DueDate = new DateTime(2018, 05, 11, 03, 59, 59), 													PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Personal, CloseTime = null, });
				s.Save(new TodoModel() { Id = 568539, Ordering = -568539, CreateTime = new DateTime(2018, 05, 10, 13, 29, 39), DeleteTime = null, CompleteTime = new DateTime(2018, 06, 18, 13, 21, 14), ForModel = " TodoModel  ", ForModelId = -1, Message = " Meditate & Pray  ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = null, OrganizationId = org.Id, DueDate = new DateTime(2018, 05, 17, 23, 59, 59), 																				PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Personal, CloseTime = null, });
				s.Save(new TodoModel() { Id = 568546, Ordering = -568546, CreateTime = new DateTime(2018, 05, 10, 13, 30, 28), DeleteTime = null, CompleteTime = new DateTime(2018, 05, 15, 14, 24, 33), ForModel = " TodoModel  ", ForModelId = -1, Message = " Finish Peak Performance  ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = null, OrganizationId = org.Id, DueDate = new DateTime(2018, 05, 10, 04, 00, 00), 																		PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Personal, CloseTime = null, });
				s.Save(new TodoModel() { Id = 585277, Ordering = -585277, CreateTime = new DateTime(2018, 05, 17, 19, 22, 15), DeleteTime = null, CompleteTime = null, ForModel = " TodoModel  ", ForModelId = -1, Message = " Email to team on Google drive project  ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = null, OrganizationId = org.Id, DueDate = new DateTime(2018, 09, 19, 03, 59, 59), 																							PadId = "FirstOne", ClearedInMeeting = null, TodoType = TodoType.Personal, CloseTime = null, });
				s.Save(new TodoModel() { Id = 629271, Ordering = 43, CreateTime = new DateTime(2018, 06, 09, 04, 03, 20), DeleteTime = null, CompleteTime = new DateTime(2018, 07, 14, 22, 13, 46), ForModel = " TodoModel  ", ForModelId = -1, Message = " Expand Rock Form to Include rocks by employee with a tab for each company/department ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_6, OrganizationId = org.Id, DueDate = new DateTime(2018, 06, 19, 23, 59, 59), 			PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = null, });
				s.Save(new TodoModel() { Id = 629272, Ordering = 42, CreateTime = new DateTime(2018, 06, 09, 04, 04, 59), DeleteTime = null, CompleteTime = new DateTime(2018, 08, 07, 13, 20, 10), ForModel = " TodoModel  ", ForModelId = -1, Message = " Build Out All New Rocks in Traction Tools for all users and companies  ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_6, OrganizationId = org.Id, DueDate = new DateTime(2018, 06, 30, 23, 59, 59), 							PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = null, });
				s.Save(new TodoModel() { Id = 688858, Ordering = 8, CreateTime = new DateTime(2018, 07, 09, 18, 28, 24), DeleteTime = null, CompleteTime = new DateTime(2018, 07, 14, 22, 13, 35), ForModel = " TodoModel  ", ForModelId = -1, Message = " Set up appointment for leaders to complete job pros next week  ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_5, OrganizationId = org.Id, DueDate = new DateTime(2018, 07, 16, 23, 59, 59), 									PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 07, 16, 19, 05, 50), });
				s.Save(new TodoModel() { Id = 702395, Ordering = -702395, CreateTime = new DateTime(2018, 07, 14, 23, 11, 06), DeleteTime = null, CompleteTime = new DateTime(2018, 08, 14, 20, 42, 37), ForModel = " TodoModel  ", ForModelId = -1, Message = " Look into adding direct reports tile on end user.  ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = null, OrganizationId = org.Id, DueDate = new DateTime(2018, 07, 21, 23, 59, 59), 											PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Personal, CloseTime = null, });
				s.Save(new TodoModel() { Id = 740141, Ordering = 22, CreateTime = new DateTime(2018, 07, 31, 10, 10, 19), DeleteTime = null, CompleteTime = new DateTime(2018, 08, 07, 12, 10, 22), ForModel = " TodoModel  ", ForModelId = -1, Message = " Adjust Rocks for this meeting to just include Main Company Rocks ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_7, OrganizationId = org.Id, DueDate = new DateTime(2018, 08, 08, 03, 59, 59), 									PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 08, 07, 13, 04, 36), });
				s.Save(new TodoModel() { Id = 790704, Ordering = 4, CreateTime = new DateTime(2018, 08, 20, 19, 17, 05), DeleteTime = null, CompleteTime = new DateTime(2018, 09, 17, 17, 12, 22), ForModel = " IssueModel ", ForModelId = 716654, Message = " Research Fleet Leases  ", Details = "RESOLVE ISSUE: Mileage reimbursement and travel policy", CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_5, OrganizationId = org.Id, DueDate = new DateTime(2018, 08, 28, 03, 59, 59), 					PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 09, 17, 19, 01, 49), });
				s.Save(new TodoModel() { Id = 796210, Ordering = 3, CreateTime = new DateTime(2018, 08, 22, 12, 04, 30), DeleteTime = null, CompleteTime = new DateTime(2018, 08, 31, 15, 26, 09), ForModel = " TodoModel  ", ForModelId = -1, Message = " Update Rocks in Traction ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_8, OrganizationId = org.Id, DueDate = new DateTime(2018, 08, 23, 23, 59, 59), 																			PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 09, 13, 12, 44, 29), });
				s.Save(new TodoModel() { Id = 796211, Ordering = 1, CreateTime = new DateTime(2018, 08, 22, 12, 05, 09), DeleteTime = null, CompleteTime = new DateTime(2018, 09, 18, 02, 53, 13), ForModel = " TodoModel  ", ForModelId = -1, Message = " Bi-Weekly 1-on-1 ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_8, OrganizationId = org.Id, DueDate = new DateTime(2018, 08, 24, 03, 59, 59), 																					PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 09, 18, 02, 54, 26), });
				s.Save(new TodoModel() { Id = 796214, Ordering = 2, CreateTime = new DateTime(2018, 08, 22, 12, 05, 54), DeleteTime = null, CompleteTime = new DateTime(2018, 08, 31, 15, 26, 20), ForModel = " TodoModel  ", ForModelId = -1, Message = " Get halfway though the book  ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_8, OrganizationId = org.Id, DueDate = new DateTime(2018, 08, 23, 23, 59, 59), 																		PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 09, 13, 12, 44, 29), });
				s.Save(new TodoModel() { Id = 796220, Ordering = 1, CreateTime = new DateTime(2018, 08, 22, 12, 06, 35), DeleteTime = null, CompleteTime = new DateTime(2018, 08, 31, 15, 26, 20), ForModel = " TodoModel  ", ForModelId = -1, Message = " Fill out roll call survey  ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_8, OrganizationId = org.Id, DueDate = new DateTime(2018, 08, 23, 23, 59, 59), 																		PadId = "pad",  ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = new DateTime(2018, 09, 13, 12, 44, 29), });
				s.Save(new TodoModel() { Id = 895287, Ordering = -895287, CreateTime = new DateTime(2018, 10, 01, 18, 57, 46), DeleteTime = null, CompleteTime = null, ForModel = " IssueModel ", ForModelId = 697005, Message = " Look into uniform options  ", Details = "RESOLVE ISSUE: DLP Store", CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_5, OrganizationId = org.Id, DueDate = new DateTime(2018, 10, 09, 03, 59, 59), 																			PadId = "pad", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = null, });
				s.Save(new TodoModel() { Id = 896384, Ordering = -896384, CreateTime = new DateTime(2018, 10, 01, 21, 09, 36), DeleteTime = null, CompleteTime = null, ForModel = " TodoModel  ", ForModelId = -1, Message = " test ", Details = null, CreatedById = accountableUser, AccountableUserId = accountableUser, CreatedDuringMeetingId = null, ForRecurrenceId = l10_9, OrganizationId = org.Id, DueDate = new DateTime(2018, 09, 21, 04, 59, 59), 																															PadId = "SecondOne", ClearedInMeeting = null, TodoType = TodoType.Recurrence, CloseTime = null, });

			});

			DbQuery(async s => {
				{
					var now = new DateTime(2018, 10, 02, 19, 03, 00);
					var selected = TodoEmailHelpers._GetAllTodosForSendTime(s, sendTime, now);
					var forUser = selected[org.Manager.User.Email];
					Assert.AreEqual(forUser.Count, 2);

					Assert.IsTrue(forUser.Any(x => x.PadId == "FirstOne"));
					Assert.IsTrue(forUser.Any(x => x.PadId == "SecondOne"));

					var unsent = new List<Mail>();
					await TodoEmailHelpers._ConstructTodoEmail(sendTime, unsent, now, selected[org.Manager.User.Email]);
					Assert.IsTrue(unsent.Count == 1);
				}

				{
					var now = new DateTime(2018, 10, 05, 19, 03, 00);
					var selected = TodoEmailHelpers._GetAllTodosForSendTime(s, sendTime, now);
					var forUser = selected[org.Manager.User.Email];
					Assert.AreEqual(forUser.Count, 3);

					Assert.IsTrue(forUser.Any(x => x.PadId == "FirstOne"));
					Assert.IsTrue(forUser.Any(x => x.PadId == "SecondOne"));

					var unsent = new List<Mail>();
					await TodoEmailHelpers._ConstructTodoEmail(sendTime, unsent, now, selected[org.Manager.User.Email]);
					Assert.IsTrue(unsent.Count == 1);
				}
			});

		}


		[TestMethod]
		[TestCategory("Scheduler")]
		public async Task TestModulo() {
			var divisor = 13;
			var emailTime = 1;

			var nowUtc = new DateTime(2017, 10, 27);
			var yesterday = nowUtc.AddDays(-1);

			var tomorrow = nowUtc.Date.AddDays(2).AddTicks(-1);
			var rangeLow = nowUtc.Date.AddDays(-1);
			var rangeHigh = nowUtc.Date.AddDays(4).AddTicks(-1);
			var nextWeek = nowUtc.Date.AddDays(7);
			if (nowUtc.DayOfWeek == DayOfWeek.Friday)
				rangeHigh = rangeHigh.AddDays(1);

			var org = await OrgUtil.CreateOrganization();
			MockHttpContext();
			var dupIndex = 0L;
			var users = new List<UserOrganizationModel>();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					for (var i = 0; i < divisor + 1; i++) {
						var user = await OrgUtil.AddUserToOrg(org, "" + i);
						await org.RegisterUser(user);
						users.Add(user);

						var auid = user.Id;

						if (i == 0) {
							dupIndex = auid % (long)divisor;
						}
						await TodoAccessor.CreateTodo(org.Manager, TodoCreation.GeneratePersonalTodo("", accountableUserId: auid, dueDate: yesterday));

					}
					tx.Commit();
					s.Flush();
				}
			}
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					foreach (var user in users) {
						var u = s.Get<UserOrganizationModel>(user.Id);
						u.User.SendTodoTime = emailTime;
						s.Update(u);
					}

					tx.Commit();
					s.Flush();
				}
			}

			var expectedCounts = new List<int>();
			for (var i = 0; i < divisor; i++)
				expectedCounts.Add(dupIndex == i ? 2 : 1);

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					for (var remainder = 0; remainder < divisor; remainder += 1) {
						var todos = TodoEmailsScheduler.TodoEmailHelpers._QueryTodo(s, emailTime, /*divisor, remainder,*/ rangeLow, rangeHigh, nextWeek);
						Assert.AreEqual(expectedCounts[remainder], todos.Count);
					}
					for (var remainder = 0; remainder < divisor; remainder += 1) {
						var todos = TodoEmailsScheduler.TodoEmailHelpers._QueryTodo(s, emailTime + 1,/*divisor, remainder,*/ rangeLow, rangeHigh, nextWeek);
						Assert.AreEqual(0, todos.Count);
					}

					//Noone listening at emailTime+1
					for (var remainder = 0; remainder < divisor; remainder += 1) {
						var unsentMail = new List<Mail>();
						await TodoEmailsScheduler.TodoEmailHelpers._ConstructTodoEmails(emailTime + 1, unsentMail, s, nowUtc/*, divisor, remainder*/);
						Assert.AreEqual(0, unsentMail.Count);
					}

					for (var remainder = 0; remainder < divisor; remainder += 1) {
						var unsentMail = new List<Mail>();
						await TodoEmailsScheduler.TodoEmailHelpers._ConstructTodoEmails(emailTime, unsentMail, s, nowUtc/*, divisor, remainder*/);
						Assert.AreEqual(expectedCounts[remainder], unsentMail.Count);
					}
				}
			}



		}
	}
}
