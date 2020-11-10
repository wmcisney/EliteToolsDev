using RadialReview.Crosscutting.Hooks.CrossCutting;
using RadialReview.Crosscutting.Hooks.CrossCutting.Formula;
using RadialReview.Crosscutting.Hooks.Integrations;
using RadialReview.Crosscutting.Hooks.Payment;
using RadialReview.Crosscutting.Hooks.QuarterlyConversation;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Crosscutting.Hooks.CrossCutting.Payment;
using RadialReview.Crosscutting.Hooks.Meeting;
using RadialReview.Crosscutting.Hooks.Realtime;
using RadialReview.Crosscutting.Hooks.Realtime.Dashboard;
using RadialReview.Crosscutting.Hooks.Realtime.L10;
using RadialReview.Crosscutting.Hooks.UserRegistration;
using RadialReview.Utilities;
using System;
using RadialReview.Crosscutting.Hooks.CrossCutting.Zapier;
using RadialReview.Hooks;
using RadialReview.Hooks.CrossCutting.AgileCrm;
using log4net;
using RadialReview.Crosscutting.Hooks.Internal;
using RadialReview.Crosscutting.Hooks.Notifications;

namespace RadialReview.App_Start {



	public class HookConfig {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


		public static void RegisterHooks() {
			//HooksRegistry.RegisterHook(new CreateUserOrganization_UpdateHierarchy());

			HooksRegistry.RegisterHook(new UpdateUserModel_TeamNames());
			HooksRegistry.RegisterHook(new UpdateRoles_Notifications());
			HooksRegistry.RegisterHook(new UpdateUserCache());

			//HooksRegistry.RegisterHook(new TodoWebhook());
			//HooksRegistry.RegisterHook(new IssueWebhook());

			//HooksRegistry.RegisterHook(new ActiveCampaignEventHooks());
			//HooksRegistry.RegisterHook(new ActiveCampaignFirstThreeMeetings());
			HooksRegistry.RegisterHook(new EnterpriseHook(Config.EnterpriseAboveUserCount()));

			HooksRegistry.RegisterHook(new ZapierEventSubscription());

			/*try {
				HooksRegistry.RegisterHook(new AgileCrmOrgEventHook());
				HooksRegistry.RegisterHook(new AgileCrmUserEventHooks());
				HooksRegistry.RegisterHook(new AgileCrmMeetings());
			} catch (Exception e) {
				log.Error(e);
			}*/

			HooksRegistry.RegisterHook(new InternalZapierHooks());

			HooksRegistry.RegisterHook(new DepristineHooks());
			HooksRegistry.RegisterHook(new MeetingRockCompletion());
			HooksRegistry.RegisterHook(new AuditLogHooks());

			//HooksRegistry.RegisterHook(new RealTime_Tasks());
			HooksRegistry.RegisterHook(new RealTime_L10_Todo());
			HooksRegistry.RegisterHook(new RealTime_Dashboard_Todo());
			HooksRegistry.RegisterHook(new RealTime_L10_Issues());

			HooksRegistry.RegisterHook(new Realtime_L10Scorecard());
			HooksRegistry.RegisterHook(new RealTime_L10_UpdateRocks());
			HooksRegistry.RegisterHook(new RealTime_VTO_UpdateRocks());
			HooksRegistry.RegisterHook(new RealTime_Dashboard_UpdateL10Rocks());
			HooksRegistry.RegisterHook(new RealTime_Dashboard_Scorecard());
			HooksRegistry.RegisterHook(new RealTime_L10_Headline());
			HooksRegistry.RegisterHook(new RealTime_Dashboard_Headline());



			HooksRegistry.RegisterHook(new CalculateCumulative());
			HooksRegistry.RegisterHook(new AttendeeHooks());
			HooksRegistry.RegisterHook(new SwapScorecardOnRegister());

			HooksRegistry.RegisterHook(new CreateFinancialPermItems());

			HooksRegistry.RegisterHook(new UpdatePlaceholder());
			HooksRegistry.RegisterHook(new RealTime_L10_Milestone());
			//HooksRegistry.RegisterHook(new TodoEdit())
			HooksRegistry.RegisterHook(new CascadeScorecardFormulaUpdates());
			HooksRegistry.RegisterHook(new RealTime_Positions());

			HooksRegistry.RegisterHook(new CascadeScorecardFormulaUpdates());

			HooksRegistry.RegisterHook(new ExecutePaymentCardUpdate());
			HooksRegistry.RegisterHook(new FirstPaymentEmail());
			HooksRegistry.RegisterHook(new SetDelinquentFlag());
            HooksRegistry.RegisterHook(new CardExpireEmail());
            HooksRegistry.RegisterHook(new UnlockOnCard());



            HooksRegistry.RegisterHook(new QuarterlyConversationCreationNotifications());
			HooksRegistry.RegisterHook(new SetPeopleToolsTrial());

			HooksRegistry.RegisterHook(new NotificationOnNewQuarterHooks());


			//Todo Integrations
			HooksRegistry.RegisterHook(new AsanaTodoHook());

			//HooksRegistry.RegisterHook(new TodoEdit())
		}
	}
}
