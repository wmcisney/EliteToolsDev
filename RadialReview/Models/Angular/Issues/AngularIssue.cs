﻿using RadialReview.Models.Angular.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Issues;
using RadialReview.Utilities;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using RadialReview.Crosscutting.AttachedPermission;

namespace RadialReview.Models.Angular.Issues
{
	public class AngularIssue : BaseAngular, IAttachedPermission
	{
		public AngularIssue(IssueModel.IssueModel_Recurrence recurrenceIssue) : base(recurrenceIssue.Id)
		{
			var issue = recurrenceIssue.Issue;
			DetailsUrl = Config.BaseUrl(null, "/Issues/Pad/" + issue.Id); //Config.NotesUrl() + "p/" + issue.PadId + "?showControls=true&showChat=false";
			Name = issue.Message;
			Details = issue.Description;
			CompleteTime = recurrenceIssue.CloseTime;
			CreateTime = recurrenceIssue.CreateTime;
			Children = recurrenceIssue._ChildIssues.NotNull(x => 
				x.Select(y => new AngularIssue(y)).ToList()
			)?? new List<AngularIssue>();
			Complete = recurrenceIssue.CloseTime != null;
			if (recurrenceIssue.Owner!=null)
				Owner = AngularUser.CreateUser(recurrenceIssue.Owner);
            Priority = recurrenceIssue.Priority;
			CloseTime = recurrenceIssue.CloseTime;
            Origin = recurrenceIssue.Recurrence.NotNull(x => x.Name);
            if (recurrenceIssue.Recurrence != null)
                OriginId = recurrenceIssue.Recurrence.Id;
            //Delete = recurrenceIssue.DeleteTime != null;
        }
		public AngularIssue()
		{
			
		}

		[JsonProperty(Order = -10)]
		public String Name { get; set; }
		public AngularUser Owner { get; set; }
		public string DetailsUrl { get; set; }
        [Obsolete]
		public DateTime? CompleteTime { get; set; }
		public bool? Complete { get; set; }

		public DateTime? CreateTime { get; set; }
		public DateTime? CloseTime { get; set; }

		[IgnoreDataMember]
		public String Details { get; set; }

        public string Origin { get; set; }
        public long? OriginId { get; set; }

		[IgnoreDataMember]
		public List<AngularIssue> Children { get; set; }
		[IgnoreDataMember]
		public int? Priority { get; set; }
        public PermissionDto Permission { get;set; }
    }
}