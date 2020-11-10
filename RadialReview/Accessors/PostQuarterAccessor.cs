using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using FluentNHibernate.Utils;
using Microsoft.AspNet.SignalR;
using NHibernate;
using NHibernate.Linq;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Components;
using RadialReview.Models.L10;
using RadialReview.Models.PostQuarter;
using RadialReview.Utilities;
using RadialReview.Utilities.Synchronize;
using RadialReview.Utilities.RealTime;
using RadialReview.Models.Application;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Angular.Base;
using RadialReview.Utilities.DataTypes;
using RadialReview.Models.Angular.Meeting;
using System.Threading.Tasks;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Utilities.Hooks;
using static RadialReview.Accessors.L10Accessor;
using RadialReview.Models.Enums;
using RadialReview.Models.ViewModels;
using System.Web.Mvc;
using RadialReview.Utilities.NHibernate;
using Dangl.Calculator;
using static RadialReview.Utilities.GraphUtility;
using static RadialReview.Utilities.FormulaUtility;
using NHibernate.Criterion;

namespace RadialReview.Accessors
{
    public class PostQuarterAccessor
    {
        public static async Task<PostQuarterModel> CreatePostQuarter(UserOrganizationModel caller, PostQuarterModel postQuarter)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {                    
                    var perms = PermissionsUtility.Create(s, caller).EditL10Recurrence(postQuarter.L10RecurrenceId);                    
                    var existingPostQuarter = s.QueryOver<PostQuarterModel>().Where(x => x.DeleteTime == null
                        && x.L10RecurrenceId == postQuarter.L10RecurrenceId
                        && x.OrganizationId == caller.Organization.Id
                        && x.QuarterEndDate == postQuarter.QuarterEndDate.Date).List().FirstOrDefault();
                        
                    if (existingPostQuarter != null)
                    {
                        postQuarter.Id = existingPostQuarter.Id;
                        existingPostQuarter.Name = postQuarter.Name;                        
                        s.Update(existingPostQuarter);
                    }
                    else
                    {
                        postQuarter.OrganizationId = caller.Organization.Id;
                        postQuarter.CreatedBy = caller.Id;
                        s.Save(postQuarter);                        
                    }
                    tx.Commit();
                    s.Flush();

                    return postQuarter;
                }
            }
        }
    }
}