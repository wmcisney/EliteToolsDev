using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Models;
using RadialReview.Models.Askables;
using TractionTools.Tests.TestUtils;
using TractionTools.Tests.Utilities;
using System.Threading.Tasks;
using RadialReview.Utilities.DataTypes;
using RadialReview.Models.L10;
using RadialReview.Models.Todo;
using RadialReview.Models.Issues;
using RadialReview.Accessors;
using RadialReview.Crosscutting.Schedulers;

namespace TractionTools.Tests.Meeting {
	[TestClass]
	public class MeetingConclude : BaseTest {
		//[TestMethod]
		//public async Task ConclusionTimeoutFix() {
		//	var org = await OrgUtil.CreateOrganization();

		//	var ratio_b1c2e5 = new Ratio { Numerator = 0.00000m, Denominator = 15.00000m, };
		//	var ratio_d76746 = new Ratio { Numerator = 4.00000m, Denominator = 9.00000m, };
		//	var ratio_b376b2 = new Ratio { Numerator = 6.00000m, Denominator = 10.00000m, };
		//	var ratio_1ba07a = new Ratio { Numerator = 59.00000m, Denominator = 7.00000m, };
		//	var ratio_c03076 = new Ratio { Numerator = 0.00000m, Denominator = 0.00000m, };
		//	var ratio_21744c = new Ratio { Numerator = 2.00000m, Denominator = 9.00000m, };
		//	var organizationModel_76997 = org.Organization;
		//	var userOrganizationModel_77233 = org.Manager;
		//	var userOrganizationModel_77003 = org.Employee;

		//	L10Recurrence l10Recurrence_15981 = null;


		//	DbCommit(s => {

		//		//s.Save(userOrganizationModel_77233);
		//		//s.Save(userOrganizationModel_77003);			
		//		l10Recurrence_15981 = new L10Recurrence { Name = @"Safety Meeting", CreateTime = new DateTime(636707189290000000), OrganizationId = organizationModel_76997.Id, Pristine = false, CountDown = true, PrintOutRockStatus = false, AttendingOffByDefault = false, CurrentWeekHighlightShift = 0, IncludeIndividualTodos = false, IncludeAggregateTodoCompletion = false, IncludeAggregateTodoCompletionOnPrintout = true, SegueMinutes = 5.00000m, ScorecardMinutes = 5.00000m, RockReviewMinutes = 5.00000m, HeadlinesMinutes = 5.00000m, TodoListMinutes = 5.00000m, IDSMinutes = 60.00000m, ConclusionMinutes = 5.00000m, DefaultTodoOwner = userOrganizationModel_77233.Id, ReverseScorecard = false, CreatedById = userOrganizationModel_77003.Id, VideoId = @"15a42073-6f1b-4187-84cb-29c2885e961a", VtoId = 17259, ShareVto = false, OrderIssueBy = @"data-rank", EnableTranscription = false, PreventEditingUnownedMeasurables = false, HeadlinesId = @"8a9057b5-176c-4af3-b85e-1cb174630df6", ShowHeadlinesBox = false, HeadlineType = RadialReview.Model.Enums.PeopleHeadlineType.HeadlinesList, RockType = RadialReview.Model.Enums.L10RockType.Original, MeetingType = RadialReview.Models.L10.MeetingType.L10, TeamType = RadialReview.Models.L10.L10TeamType.DepartmentalTeam, IsLeadershipTeam = true, CombineRocks = true, Prioritization = RadialReview.Models.L10.PrioritizationType.Rank, ForumStep = RadialReview.Models.L10.ForumStep.AddIssues, };

		//		s.Save(l10Recurrence_15981);
		//		//s.Save(organizationModel_76997);

		//		var l10Meeting_226937 = new L10Meeting { CreateTime = new DateTime(636742844980000000), StartTime = new DateTime(636742844980000000), OrganizationId = organizationModel_76997.Id, Organization = organizationModel_76997, L10RecurrenceId = l10Recurrence_15981.Id, L10Recurrence = l10Recurrence_15981, MeetingLeaderId = userOrganizationModel_77003.Id, MeetingLeader = userOrganizationModel_77003, Preview = false, SendConcludeEmailTo = RadialReview.Models.L10.VM.ConcludeSendEmail.None, };
		//		var l10Meeting_225668 = new L10Meeting { CreateTime = new DateTime(636741874190000000), StartTime = new DateTime(636741874190000000), CompleteTime = new DateTime(636742569690000000), OrganizationId = organizationModel_76997.Id, L10RecurrenceId = l10Recurrence_15981.Id, L10Recurrence = l10Recurrence_15981, MeetingLeaderId = userOrganizationModel_77233.Id, MeetingLeader = userOrganizationModel_77233, TodoCompletion = ratio_21744c, Preview = false, AverageMeetingRating = ratio_c03076, SendConcludeEmailTo = RadialReview.Models.L10.VM.ConcludeSendEmail.AllAttendees, };
		//		var l10Meeting_198902 = new L10Meeting { CreateTime = new DateTime(636711627290000000), StartTime = new DateTime(636711627290000000), CompleteTime = new DateTime(636711660650000000), OrganizationId = organizationModel_76997.Id, L10RecurrenceId = l10Recurrence_15981.Id, L10Recurrence = l10Recurrence_15981, MeetingLeaderId = userOrganizationModel_77233.Id, MeetingLeader = userOrganizationModel_77233, TodoCompletion = ratio_c03076, Preview = false, AverageMeetingRating = ratio_1ba07a, SendConcludeEmailTo = RadialReview.Models.L10.VM.ConcludeSendEmail.AllAttendees, };
		//		var l10Meeting_214533 = new L10Meeting { CreateTime = new DateTime(636729769560000000), StartTime = new DateTime(636729769560000000), CompleteTime = new DateTime(636730612440000000), OrganizationId = organizationModel_76997.Id, L10RecurrenceId = l10Recurrence_15981.Id, L10Recurrence = l10Recurrence_15981, MeetingLeaderId = userOrganizationModel_77233.Id, MeetingLeader = userOrganizationModel_77233, TodoCompletion = ratio_b376b2, Preview = false, AverageMeetingRating = ratio_c03076, SendConcludeEmailTo = RadialReview.Models.L10.VM.ConcludeSendEmail.AllAttendees, };
		//		var l10Meeting_220028 = new L10Meeting { CreateTime = new DateTime(636735828490000000), StartTime = new DateTime(636735828490000000), CompleteTime = new DateTime(636737641800000000), OrganizationId = organizationModel_76997.Id, L10RecurrenceId = l10Recurrence_15981.Id, L10Recurrence = l10Recurrence_15981, MeetingLeaderId = userOrganizationModel_77233.Id, MeetingLeader = userOrganizationModel_77233, TodoCompletion = ratio_d76746, Preview = false, AverageMeetingRating = ratio_c03076, SendConcludeEmailTo = RadialReview.Models.L10.VM.ConcludeSendEmail.AllAttendees, };

		//		s.Save(l10Meeting_226937);
		//		s.Save(l10Meeting_225668);
		//		s.Save(l10Meeting_198902);
		//		s.Save(l10Meeting_214533);
		//		s.Save(l10Meeting_220028);

		//		l10Recurrence_15981.MeetingInProgress = l10Meeting_226937.Id;
		//		s.Update(l10Recurrence_15981);

		//		var l10Meeting_226936 = new L10Meeting { CreateTime = new DateTime(636742844760000000), StartTime = new DateTime(636742844760000000), CompleteTime = new DateTime(636742845140000000), OrganizationId = organizationModel_76997.Id, L10RecurrenceId = l10Recurrence_15981.Id, L10Recurrence = l10Recurrence_15981, MeetingLeaderId = userOrganizationModel_77003.Id, MeetingLeader = userOrganizationModel_77003, TodoCompletion = ratio_b1c2e5, Preview = true, AverageMeetingRating = ratio_c03076, SendConcludeEmailTo = RadialReview.Models.L10.VM.ConcludeSendEmail.None, };
		//		s.Save(l10Meeting_226936);

		//		var userOrganizationModel_77277 = new UserOrganizationModel { _IsRadialAdmin = false, _IsTestAdmin = false, EmailAtOrganization = @"scott.zietz@vantageplastics.com", ManagerAtOrganization = true, ManagingOrganization = false, IsRadialAdmin = false, IsImplementer = false, AttachTime = new DateTime(636590432540000000), CreateTime = new DateTime(636546821060000000), CountPerPage = 0, EvalOnly = false, IsClient = false, IsPlaceholder = false, UserModelId = @"709f14d0-706a-4fe3-b72c-a8bf00beac68", };
		//		var userOrganizationModel_86456 = new UserOrganizationModel { _IsRadialAdmin = false, _IsTestAdmin = false, EmailAtOrganization = @"darwin.curtis@vantageplastics.com", ManagerAtOrganization = true, ManagingOrganization = false, IsRadialAdmin = false, IsImplementer = false, AttachTime = new DateTime(636588999550000000), CreateTime = new DateTime(636572374060000000), CountPerPage = 0, EvalOnly = false, IsClient = false, IsPlaceholder = false, UserModelId = @"34b7f431-89e5-47f1-8301-a8bd0145b7d8", };
		//		var userOrganizationModel_126893 = new UserOrganizationModel { _IsRadialAdmin = false, _IsTestAdmin = false, EmailAtOrganization = @"tom.lockwood@vantageplastics.com", ManagerAtOrganization = true, ManagingOrganization = false, IsRadialAdmin = false, IsImplementer = false, AttachTime = new DateTime(636699584210000000), CreateTime = new DateTime(636687455500000000), CountPerPage = 0, EvalOnly = false, IsClient = false, IsPlaceholder = false, UserModelId = @"669ffe0f-b4a7-44b4-8530-a93d01425d47", };
		//		var userOrganizationModel_77294 = new UserOrganizationModel { _IsRadialAdmin = false, _IsTestAdmin = false, EmailAtOrganization = @"dave.cook@vantageplastics.com", ManagerAtOrganization = true, ManagingOrganization = false, IsRadialAdmin = false, IsImplementer = false, AttachTime = new DateTime(636600900910000000), CreateTime = new DateTime(636546825720000000), CountPerPage = 0, EvalOnly = false, IsClient = false, IsPlaceholder = false, UserModelId = @"ed7b8bc7-0ece-4d73-bd89-a8cb00ec9fee", };

		//		s.Save(userOrganizationModel_77277);
		//		s.Save(userOrganizationModel_86456);
		//		s.Save(userOrganizationModel_126893);
		//		s.Save(userOrganizationModel_77294);

		//		var issueModel_756680 = new IssueModel { CreateTime = new DateTime(636711636160000000), Message = @"First responder team needed>?", PadId = @"pad-id", CreatedById = userOrganizationModel_77233.Id, CreatedBy = userOrganizationModel_77233, CreatedDuringMeetingId = l10Meeting_198902.Id, CreatedDuringMeeting = l10Meeting_198902, _Order = -636743313835024787, _Priority = 0, ForModel = @"IssueModel", ForModelId = -1, OrganizationId = organizationModel_76997.Id, _Rank = 0, };
		//		var issueModel_805863 = new IssueModel { CreateTime = new DateTime(636729759700000000), Message = @"Work injuries due to strains", PadId = @"pad-id", CreatedById = userOrganizationModel_77233.Id, CreatedBy = userOrganizationModel_77233, _Order = -636743313835530040, _Priority = 0, ForModel = @"IssueModel", ForModelId = -1, OrganizationId = organizationModel_76997.Id, _Rank = 0, };
		//		var issueModel_823645 = new IssueModel {
		//			CreateTime = new DateTime(636735842220000000),
		//			Message = @"Airpark safety",
		//			PadId = @"pad-id",
		//			CreatedById = userOrganizationModel_86456.Id,
		//			CreatedBy = userOrganizationModel_86456,
		//			CreatedDuringMeetingId = l10Meeting_220028.Id,
		//			CreatedDuringMeeting = l10Meeting_220028,
		//			_Order = -636743313836000159,
		//			_Priority = 0,
		//			ForModel = @"IssueModel",
		//			ForModelId = -1,
		//			OrganizationId = organizationModel_76997.Id,
		//			_Rank = 0,
		//		};
		//		var issueModel_756699 = new IssueModel { CreateTime = new DateTime(636711637620000000), Message = @"What are we going to dow tih safety walkthroughs?", PadId = @"pad-id", CreatedById = userOrganizationModel_77233.Id, CreatedBy = userOrganizationModel_77233, CreatedDuringMeetingId = l10Meeting_198902.Id, CreatedDuringMeeting = l10Meeting_198902, _Order = -636743313836470416, _Priority = 0, ForModel = @"IssueModel", ForModelId = -1, OrganizationId = organizationModel_76997.Id, _Rank = 0, };
		//		var issueModel_812874 = new IssueModel { CreateTime = new DateTime(636733934070000000), Message = @"anti vibrating gloves or material to add to handle of router", PadId = @"pad-id", CreatedById = userOrganizationModel_77233.Id, CreatedBy = userOrganizationModel_77233, _Order = -636743313836940535, _Priority = 0, ForModel = @"IssueModel", ForModelId = -1, OrganizationId = organizationModel_76997.Id, _Rank = 0, };
		//		var issueModel_841830 = new IssueModel { CreateTime = new DateTime(636741865290000000), Message = @"more strain incidents", PadId = @"pad-id", CreatedById = userOrganizationModel_77233.Id, CreatedBy = userOrganizationModel_77233, _Order = -636743313837420641, _Priority = 0, ForModel = @"IssueModel", ForModelId = -1, OrganizationId = organizationModel_76997.Id, _Rank = 0, };
		//		var issueModel_841724 = new IssueModel { CreateTime = new DateTime(636741855370000000), Message = @"work boots", PadId = @"pad-id", CreatedById = userOrganizationModel_77233.Id, CreatedBy = userOrganizationModel_77233, _Order = -636743313837895767, _Priority = 0, ForModel = @"IssueModel", ForModelId = -1, OrganizationId = organizationModel_76997.Id, _Rank = 0, };


		//		s.Save(issueModel_756680);
		//		s.Save(issueModel_805863);
		//		s.Save(issueModel_823645);
		//		s.Save(issueModel_756699);
		//		s.Save(issueModel_812874);
		//		s.Save(issueModel_841830);
		//		s.Save(issueModel_841724);

		//		var todoModel_816312 = new TodoModel { CreateTime = new DateTime(636711647970000000), DueDate = new DateTime(636748271990000000), Message = @"Develop first responder procedure ", CreatedById = userOrganizationModel_77233.Id, CreatedBy = userOrganizationModel_77233, CreatedDuringMeetingId = l10Meeting_198902.Id, CreatedDuringMeeting = l10Meeting_198902, ForRecurrenceId = l10Recurrence_15981.Id, ForRecurrence = l10Recurrence_15981, AccountableUserId = userOrganizationModel_126893.Id, AccountableUser = userOrganizationModel_126893, Ordering = 13, ForModel = @"IssueModel", ForModelId = issueModel_756680.Id, OrganizationId = organizationModel_76997.Id, PadId = @"229c3d55-41a3-4faa-ab1f-cf7985d07e25", TodoType = RadialReview.Models.Todo.TodoType.Recurrence, };
		//		var todoModel_867685 = new TodoModel { CreateTime = new DateTime(636729792980000000), DueDate = new DateTime(636736175990000000), Message = @"do ergonomics study on weight and ROM on parts lifting", CreatedById = userOrganizationModel_77233.Id, CreatedBy = userOrganizationModel_77233, CreatedDuringMeetingId = l10Meeting_214533.Id, CreatedDuringMeeting = l10Meeting_214533, ForRecurrenceId = l10Recurrence_15981.Id, ForRecurrence = l10Recurrence_15981, AccountableUserId = userOrganizationModel_86456.Id, AccountableUser = userOrganizationModel_86456, Ordering = 0, ForModel = @"IssueModel", ForModelId = issueModel_805863.Id, OrganizationId = organizationModel_76997.Id, PadId = @"4b0a0e79-02fa-47dd-bbe8-cf67ed61cb18", TodoType = RadialReview.Models.Todo.TodoType.Recurrence, };
		//		var todoModel_867772 = new TodoModel { CreateTime = new DateTime(636729801310000000), DueDate = new DateTime(636747408000000000), Message = @"Make a standard height for all product, then have plan on how to get stack height to customer requests.  Put in work instructions.  Evaulate area by area", CreatedById = userOrganizationModel_77233.Id, CreatedBy = userOrganizationModel_77233, CreatedDuringMeetingId = l10Meeting_214533.Id, CreatedDuringMeeting = l10Meeting_214533, ForRecurrenceId = l10Recurrence_15981.Id, ForRecurrence = l10Recurrence_15981, AccountableUserId = userOrganizationModel_86456.Id, AccountableUser = userOrganizationModel_86456, Ordering = 1, ForModel = @"IssueModel", ForModelId = issueModel_805863.Id, OrganizationId = organizationModel_76997.Id, PadId = @"6d15fc2f-cf9a-4f2f-a43d-cb8e8bff993d", TodoType = RadialReview.Models.Todo.TodoType.Recurrence, };
		//		var todoModel_886007 = new TodoModel { CreateTime = new DateTime(636735847430000000), DueDate = new DateTime(636742223990000000), Message = @"check what is needed for safety pins on machine 11 for set up", CreatedById = userOrganizationModel_77233.Id, CreatedBy = userOrganizationModel_77233, CreatedDuringMeetingId = l10Meeting_220028.Id, CreatedDuringMeeting = l10Meeting_220028, ForRecurrenceId = l10Recurrence_15981.Id, ForRecurrence = l10Recurrence_15981, AccountableUserId = userOrganizationModel_77294.Id, AccountableUser = userOrganizationModel_77294, Ordering = 5, ForModel = @"TodoModel", ForModelId = -1, OrganizationId = organizationModel_76997.Id, PadId = @"ad02c8af-959f-42b2-a336-18eed9cb09a5", TodoType = RadialReview.Models.Todo.TodoType.Recurrence, };
		//		var todoModel_886016 = new TodoModel { CreateTime = new DateTime(636735848390000000), DueDate = new DateTime(636742223990000000), Message = @"talk to team leads", CreatedById = userOrganizationModel_77233.Id, CreatedBy = userOrganizationModel_77233, CreatedDuringMeetingId = l10Meeting_220028.Id, CreatedDuringMeeting = l10Meeting_220028, ForRecurrenceId = l10Recurrence_15981.Id, ForRecurrence = l10Recurrence_15981, AccountableUserId = userOrganizationModel_86456.Id, AccountableUser = userOrganizationModel_86456, Ordering = 3, ForModel = @"IssueModel", ForModelId = issueModel_823645.Id, OrganizationId = organizationModel_76997.Id, PadId = @"de36bdb9-3588-4c22-b93b-5292442afc05", TodoType = RadialReview.Models.Todo.TodoType.Recurrence, };
		//		var todoModel_886042 = new TodoModel { CreateTime = new DateTime(636735852340000000), DueDate = new DateTime(636742223990000000), Message = @"assign zones to safety team", CreatedById = userOrganizationModel_77233.Id, CreatedBy = userOrganizationModel_77233, CreatedDuringMeetingId = l10Meeting_220028.Id, CreatedDuringMeeting = l10Meeting_220028, ForRecurrenceId = l10Recurrence_15981.Id, ForRecurrence = l10Recurrence_15981, AccountableUserId = userOrganizationModel_77233.Id, AccountableUser = userOrganizationModel_77233, Ordering = 8, ForModel = @"IssueModel", ForModelId = issueModel_756699.Id, OrganizationId = organizationModel_76997.Id, PadId = @"366bee5e-e41c-4de9-8e09-f6be11a809e4", TodoType = RadialReview.Models.Todo.TodoType.Recurrence, };
		//		var todoModel_886054 = new TodoModel { CreateTime = new DateTime(636735854970000000), DueDate = new DateTime(636741902970000000), Message = @"anti-vibrating gloves/material", CreatedById = userOrganizationModel_77233.Id, CreatedBy = userOrganizationModel_77233, CreatedDuringMeetingId = l10Meeting_220028.Id, CreatedDuringMeeting = l10Meeting_220028, ForRecurrenceId = l10Recurrence_15981.Id, ForRecurrence = l10Recurrence_15981, AccountableUserId = userOrganizationModel_77277.Id, AccountableUser = userOrganizationModel_77277, Ordering = 6, ForModel = @"IssueModel", ForModelId = issueModel_812874.Id, OrganizationId = organizationModel_76997.Id, PadId = @"3c899c51-0cd7-4bdb-bbfb-c4af7cc997c2", TodoType = RadialReview.Models.Todo.TodoType.Recurrence, };
		//		var todoModel_904012 = new TodoModel { CreateTime = new DateTime(636741889200000000), DueDate = new DateTime(636748271990000000), Message = @"work on getting a stronger and set rotation of work duties", CreatedById = userOrganizationModel_77233.Id, CreatedBy = userOrganizationModel_77233, CreatedDuringMeetingId = l10Meeting_225668.Id, CreatedDuringMeeting = l10Meeting_225668, ForRecurrenceId = l10Recurrence_15981.Id, ForRecurrence = l10Recurrence_15981, AccountableUserId = userOrganizationModel_86456.Id, AccountableUser = userOrganizationModel_86456, Ordering = 4, ForModel = @"IssueModel", ForModelId = issueModel_841830.Id, OrganizationId = organizationModel_76997.Id, PadId = @"f55e324b-85bc-499f-9baa-9f0f7b6ab597", TodoType = RadialReview.Models.Todo.TodoType.Recurrence, };
		//		var todoModel_904051 = new TodoModel { CreateTime = new DateTime(636741892980000000), DueDate = new DateTime(636748271990000000), Message = @"create job description for in-house trainer/supervisor", CreatedById = userOrganizationModel_77233.Id, CreatedBy = userOrganizationModel_77233, CreatedDuringMeetingId = l10Meeting_225668.Id, CreatedDuringMeeting = l10Meeting_225668, ForRecurrenceId = l10Recurrence_15981.Id, ForRecurrence = l10Recurrence_15981, AccountableUserId = userOrganizationModel_126893.Id, AccountableUser = userOrganizationModel_126893, Ordering = 12, ForModel = @"IssueModel", ForModelId = issueModel_841830.Id, OrganizationId = organizationModel_76997.Id, PadId = @"c6bf982e-ca34-4f73-bfdf-95c5ede16bd0", TodoType = RadialReview.Models.Todo.TodoType.Recurrence, };
		//		var todoModel_904082 = new TodoModel { CreateTime = new DateTime(636741894630000000), DueDate = new DateTime(636748271990000000), Message = @"when have an incident, share video clip w/team leaders to review in huddle meeting.", CreatedById = userOrganizationModel_77233.Id, CreatedBy = userOrganizationModel_77233, CreatedDuringMeetingId = l10Meeting_225668.Id, CreatedDuringMeeting = l10Meeting_225668, ForRecurrenceId = l10Recurrence_15981.Id, ForRecurrence = l10Recurrence_15981, AccountableUserId = userOrganizationModel_77233.Id, AccountableUser = userOrganizationModel_77233, Ordering = 11, ForModel = @"IssueModel", ForModelId = issueModel_841830.Id, OrganizationId = organizationModel_76997.Id, PadId = @"09010951-2c05-4f31-b63a-a045e01de6c1", TodoType = RadialReview.Models.Todo.TodoType.Recurrence, };
		//		var todoModel_904128 = new TodoModel { CreateTime = new DateTime(636741898810000000), DueDate = new DateTime(636748271990000000), Message = @"P I C N I C", CreatedById = userOrganizationModel_77233.Id, CreatedBy = userOrganizationModel_77233, CreatedDuringMeetingId = l10Meeting_225668.Id, CreatedDuringMeeting = l10Meeting_225668, ForRecurrenceId = l10Recurrence_15981.Id, ForRecurrence = l10Recurrence_15981, AccountableUserId = userOrganizationModel_86456.Id, AccountableUser = userOrganizationModel_86456, Ordering = 2, ForModel = @"IssueModel", ForModelId = issueModel_841830.Id, OrganizationId = organizationModel_76997.Id, PadId = @"60fe3cff-9a18-4b9e-849e-d5f965c7cfae", TodoType = RadialReview.Models.Todo.TodoType.Recurrence, };
		//		var todoModel_904129 = new TodoModel { CreateTime = new DateTime(636741898810000000), DueDate = new DateTime(636748271990000000), Message = @"P I C N I C", CreatedById = userOrganizationModel_77233.Id, CreatedBy = userOrganizationModel_77233, CreatedDuringMeetingId = l10Meeting_225668.Id, CreatedDuringMeeting = l10Meeting_225668, ForRecurrenceId = l10Recurrence_15981.Id, ForRecurrence = l10Recurrence_15981, AccountableUserId = userOrganizationModel_126893.Id, AccountableUser = userOrganizationModel_126893, Ordering = 14, ForModel = @"IssueModel", ForModelId = issueModel_841830.Id, OrganizationId = organizationModel_76997.Id, PadId = @"a8a43c08-4f53-4285-80bd-2165c9d6f89d", TodoType = RadialReview.Models.Todo.TodoType.Recurrence, };
		//		var todoModel_904177 = new TodoModel { CreateTime = new DateTime(636741904380000000), DueDate = new DateTime(636748271990000000), Message = @"create recoginition of things employees does a good deed/action.  Get vending machine money for Darwin and TL.  Vending machine 'bucks' we can hand out to use in machine. Or a 'credit' card for TL to use in machine", CreatedById = userOrganizationModel_77233.Id, CreatedBy = userOrganizationModel_77233, CreatedDuringMeetingId = l10Meeting_225668.Id, CreatedDuringMeeting = l10Meeting_225668, ForRecurrenceId = l10Recurrence_15981.Id, ForRecurrence = l10Recurrence_15981, AccountableUserId = userOrganizationModel_77233.Id, AccountableUser = userOrganizationModel_77233, Ordering = 9, ForModel = @"IssueModel", ForModelId = issueModel_841830.Id, OrganizationId = organizationModel_76997.Id, PadId = @"f3dbfd25-c538-4c34-97ab-9c08608ef3a4", TodoType = RadialReview.Models.Todo.TodoType.Recurrence, };
		//		var todoModel_904184 = new TodoModel { CreateTime = new DateTime(636741904860000000), DueDate = new DateTime(636748271990000000), Message = @"anaylze data from prior years strains", CreatedById = userOrganizationModel_77233.Id, CreatedBy = userOrganizationModel_77233, CreatedDuringMeetingId = l10Meeting_225668.Id, CreatedDuringMeeting = l10Meeting_225668, ForRecurrenceId = l10Recurrence_15981.Id, ForRecurrence = l10Recurrence_15981, AccountableUserId = userOrganizationModel_77233.Id, AccountableUser = userOrganizationModel_77233, Ordering = 7, ForModel = @"IssueModel", ForModelId = issueModel_841830.Id, OrganizationId = organizationModel_76997.Id, PadId = @"c1d5f795-96b3-4671-ae58-870c64aacd7f", TodoType = RadialReview.Models.Todo.TodoType.Recurrence, };
		//		var todoModel_904256 = new TodoModel { CreateTime = new DateTime(636741911590000000), DueDate = new DateTime(636748271990000000), Message = @"what guidelines does w/c insurance have for safety shoes/boots", CreatedById = userOrganizationModel_77233.Id, CreatedBy = userOrganizationModel_77233, CreatedDuringMeetingId = l10Meeting_225668.Id, CreatedDuringMeeting = l10Meeting_225668, ForRecurrenceId = l10Recurrence_15981.Id, ForRecurrence = l10Recurrence_15981, AccountableUserId = userOrganizationModel_77233.Id, AccountableUser = userOrganizationModel_77233, Ordering = 10, ForModel = @"IssueModel", ForModelId = issueModel_841724.Id, OrganizationId = organizationModel_76997.Id, PadId = @"b67ba9d8-0f05-4013-b8d6-55bee4ccae3d", TodoType = RadialReview.Models.Todo.TodoType.Recurrence, };


		//		s.Save(todoModel_816312);
		//		s.Save(todoModel_867685);
		//		s.Save(todoModel_867772);
		//		s.Save(todoModel_886007);
		//		s.Save(todoModel_886016);
		//		s.Save(todoModel_886042);
		//		s.Save(todoModel_886054);
		//		s.Save(todoModel_904012);
		//		s.Save(todoModel_904051);
		//		s.Save(todoModel_904082);
		//		s.Save(todoModel_904128);
		//		s.Save(todoModel_904129);
		//		s.Save(todoModel_904177);
		//		s.Save(todoModel_904184);
		//		s.Save(todoModel_904256);

		//		var permItem_75299 = new PermItem { IsArchtype = false, CreatorId = userOrganizationModel_77003.Id, CreateTime = new DateTime(636707189290000000), OrganizationId = organizationModel_76997.Id, CanView = true, CanEdit = true, CanAdmin = true, _Color = 0, AccessorType = RadialReview.Models.PermItem.AccessType.Creator, AccessorId = userOrganizationModel_77003.Id, ResType = RadialReview.Models.PermItem.ResourceType.L10Recurrence, ResId = l10Recurrence_15981.Id, };
		//		var permItem_75300 = new PermItem { IsArchtype = false, CreatorId = userOrganizationModel_77003.Id, CreateTime = new DateTime(636707189290000000), OrganizationId = organizationModel_76997.Id, CanView = true, CanEdit = true, CanAdmin = true, _Color = 0, AccessorType = RadialReview.Models.PermItem.AccessType.Members, AccessorId = -1, ResType = RadialReview.Models.PermItem.ResourceType.L10Recurrence, ResId = l10Recurrence_15981.Id, };
		//		var permItem_75301 = new PermItem { IsArchtype = false, CreatorId = userOrganizationModel_77003.Id, CreateTime = new DateTime(636707189290000000), OrganizationId = organizationModel_76997.Id, CanView = true, CanEdit = true, CanAdmin = true, _Color = 0, AccessorType = RadialReview.Models.PermItem.AccessType.Admins, AccessorId = -1, ResType = RadialReview.Models.PermItem.ResourceType.L10Recurrence, ResId = l10Recurrence_15981.Id, };
		//		var permItem_75302 = new PermItem { IsArchtype = false, CreatorId = userOrganizationModel_77003.Id, CreateTime = new DateTime(636707190610000000), OrganizationId = organizationModel_76997.Id, CanView = true, CanEdit = true, CanAdmin = true, _Color = 0, AccessorType = RadialReview.Models.PermItem.AccessType.RGM, AccessorId = 77232, ResType = RadialReview.Models.PermItem.ResourceType.L10Recurrence, ResId = l10Recurrence_15981.Id, };



		//		s.Save(permItem_75299);
		//		s.Save(permItem_75300);
		//		s.Save(permItem_75301);
		//		s.Save(permItem_75302);


		//	});

		//	using (Scheduler.Mock()) {
		//		await L10Accessor.ConcludeMeeting(org.Manager, l10Recurrence_15981.Id, new System.Collections.Generic.List<Tuple<long, decimal?>>(), RadialReview.Models.L10.VM.ConcludeSendEmail.None, false, false, null);
		//	}



		//}
	}
}
