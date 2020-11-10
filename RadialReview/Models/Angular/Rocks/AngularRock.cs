using System;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Askables;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using System.Runtime.Serialization;
using RadialReview.Crosscutting.AttachedPermission;
using System.Collections.Generic;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Application;

namespace RadialReview.Models.Angular.Rocks
{
	public class AngularRock : BaseAngular, IAttachedPermission
    {
        public AngularRock() { }
        public AngularRock(long rockId):base(rockId) {}

		public AngularRock(L10Meeting.L10Meeting_Rock meetingRock) : this(meetingRock.ForRock, meetingRock.VtoRock) {
		}

		public AngularRock(L10Recurrence.L10Recurrence_Rocks recurRock) : this(recurRock.ForRock, recurRock.VtoRock)
        {
            RecurrenceRockId = recurRock.Id;
			if (recurRock.DeleteTime != null)
				Archived = true;  

        }

		public AngularRock(RockModel rock, bool? vtoRock) : base(rock.Id)
		{
			Name = rock.Rock;
			Owner = AngularUser.CreateUser(rock.AccountableUser);
			Complete = rock.CompleteTime != null;
			DueDate = rock.DueDate;
			Completion = rock.Completion;
			VtoRock = vtoRock;//rock.CompanyRock;
            CreateTime = rock.CreateTime;
			Archived = rock.Archived;
            Origins = new List<NameId>();
		}       
        public string Name { get; set; }
		public AngularUser Owner { get; set; }
		public DateTime? DueDate { get; set; }
		public bool? Complete { get; set; }
		public RockState? Completion { get; set; }
        public DateTime? CreateTime { get; set; }
		public bool? Archived { get; set; }      

		[IgnoreDataMember]
		public long? RecurrenceRockId { get; set; }
		[IgnoreDataMember]
        public bool? VtoRock { get; set; }
		[IgnoreDataMember]
		public long? ForceOrder { get; set; }
        public PermissionDto Permission { get; set; }
        public List<NameId> Origins { get; set; }        
    }
}