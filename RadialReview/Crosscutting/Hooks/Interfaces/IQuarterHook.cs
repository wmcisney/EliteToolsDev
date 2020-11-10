using NHibernate;
using RadialReview.Models.Quarterly;
using RadialReview.Utilities.Hooks;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Hooks.Interfaces {
	public class IQuarterHookUpdates {
		public bool UpdatedName { get; set; }
		public bool UpdatedQuarter { get; set; }
		public bool UpdatedYear { get; set; }
		public bool UpdatedStartDate { get; internal set; }
		public bool UpdatedEndDate { get; internal set; }
	}
	public interface IQuarterHook : IHook {
		Task GenerateQuarter(ISession s, QuarterModel model);
		Task UpdateQuarter(ISession s, QuarterModel model, IQuarterHookUpdates updates);
	}
}