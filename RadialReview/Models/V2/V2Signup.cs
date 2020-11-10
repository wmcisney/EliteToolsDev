using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;

namespace RadialReview.Models.V2 {


	public class V2Signup {
		public virtual long Id { get; set; }
		public virtual long ByUser { get; set; }
		public virtual long OrgId { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual bool Monday { get; set; }
		public virtual bool Tuesday { get; set; }
		public virtual bool Wednesday { get; set; }
		public virtual bool Thursday { get; set; }
		public virtual bool Friday { get; set; }
		public virtual bool NeedPeopleTools { get; set; }
		public virtual bool NeedCustomMeetings { get; set; }
		public virtual bool NeedMobileApp { get; set; }
		public virtual bool NeedQuarterly { get; set; }

		public virtual bool NeedApi { get; set; }
		public virtual bool NeedTextingActions { get; set; }
		public virtual bool NeedScorecardFormulas { get; set; }

		public virtual long? HaltedBy { get; set; }
		public virtual string HaltedReason { get; set; }
	
		public virtual long PrimaryContactId { get; set; }
		//Migrate now vs migrate when features are ready.
		public virtual bool ImmediateSignup { get; set; }

		public virtual DateTime? MigrationExecuted { get; set; }
		public virtual long? MigratedBy { get; set; }

		public virtual string ByUserEmail { get; set; }

		public virtual string WaitingOn() {
			var b = new List<string>();
			if (NeedPeopleTools) {
				b.Add("People Tools");
			}
			if (NeedCustomMeetings) {
				b.Add("Custom Meetings");
			}
			if (NeedMobileApp) {
				b.Add("Mobile App");
			}

			return string.Join(",", b);
		}


		public virtual bool AllowedOnDate(DateTime time) {
			if (time.DayOfWeek == DayOfWeek.Monday && Monday) {
				return true;
			}

			if (time.DayOfWeek == DayOfWeek.Tuesday && Tuesday) {
				return true;
			}

			if (time.DayOfWeek == DayOfWeek.Wednesday && Wednesday) {
				return true;
			}

			if (time.DayOfWeek == DayOfWeek.Thursday && Thursday) {
				return true;
			}

			if (time.DayOfWeek == DayOfWeek.Friday && Friday) {
				return true;
			}

			return false;
		}

		public virtual string DaysOfWeek() {
			var b = new List<string>();
			if (Monday) {
				b.Add("Monday");
			}
			if (Tuesday) {
				b.Add("Tuesday");
			}
			if (Wednesday) {
				b.Add("Wednesday");
			}
			if (Thursday) {
				b.Add("Thursday");
			}
			if (Friday) {
				b.Add("Friday");
			}
			var res = string.Join(",", b);
			if (string.IsNullOrWhiteSpace(res)) {
				res = "None specified?";
			}

			return res;
		}

		public class Map : ClassMap<V2Signup> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.ByUser);
				Map(x => x.OrgId);

				Map(x => x.Monday);
				Map(x => x.Tuesday);
				Map(x => x.Wednesday);
				Map(x => x.Thursday);
				Map(x => x.Friday);
				Map(x => x.NeedPeopleTools);
				Map(x => x.NeedCustomMeetings);
				Map(x => x.NeedMobileApp);
				Map(x => x.NeedQuarterly);
				Map(x => x.PrimaryContactId);
				Map(x => x.HaltedBy);
				Map(x => x.HaltedReason);
				Map(x => x.ImmediateSignup);

				Map(x => x.MigrationExecuted);
				Map(x => x.MigratedBy);

				Map(x => x.NeedApi);
				Map(x => x.NeedTextingActions);
				Map(x => x.NeedScorecardFormulas);
			}

		}
	}
}