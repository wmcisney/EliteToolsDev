using RadialReview.Accessors.Hooks;
using RadialReview.Crosscutting.Hooks.CrossCutting;
using RadialReview.Crosscutting.Hooks.CrossCutting.Formula;
using RadialReview.Crosscutting.Hooks.Integrations;
using RadialReview.Crosscutting.Hooks.Payment;
using RadialReview.Crosscutting.Hooks.QuarterlyConversation;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Crosscutting.Hooks.CrossCutting;
using RadialReview.Crosscutting.Hooks.CrossCutting.ActiveCampaign;
using RadialReview.Crosscutting.Hooks.CrossCutting.Payment;
using RadialReview.Crosscutting.Hooks.Meeting;
using RadialReview.Crosscutting.Hooks.Realtime;
using RadialReview.Crosscutting.Hooks.Realtime.Dashboard;
using RadialReview.Crosscutting.Hooks.Realtime.L10;
using RadialReview.Crosscutting.Hooks.UserRegistration;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Crosscutting.AttachedPermission;

namespace RadialReview.App_Start
{
    public class AttachedPermissionConfig
    {
        public static void RegisterPermission()
        {
            ///HooksRegistry.RegisterHook(new UpdatePlaceholder());
            PermissionRegistry.RegisterPermission(new IssueAttachedPermissionHandler());
        }
    }
}