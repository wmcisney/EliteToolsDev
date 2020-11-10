using System.Web.Http;
using RadialReview.Accessors;
using System.Threading.Tasks;
using System.Web.Http.Description;
using RadialReview.Crosscutting.Zapier;
using RadialReview.Crosscutting.Hooks.CrossCutting.Zapier;

namespace RadialReview.Api.V1 {
	[RoutePrefix("api/v1")]
	[ApiExplorerSettings(IgnoreApi = true)]
	public class ZapierController : BaseApiController {

		public class ZapierSubscriptionViewModel {
			public string target_url { get; set; }
			public ZapierEvents @event { get; set; }
			public long zapier_id { get; set; }
			public long? filter_on_accountable_user_id { get; set; }
			public long? filter_on_meeting_id { get; set; }
			public long? filter_on_item_id { get; set; }


		}

		[Route("zapier/subscribe")]
		[HttpPost]
		public async Task<IHttpActionResult> PostZapierSubscription(ZapierSubscriptionViewModel zapierSubscription) {
			
			var sub = await ZapierAccessor.SaveZapierSubscription(GetUser(), GetUser().Id, GetUser().Organization.Id, 
				zapierSubscription.zapier_id, zapierSubscription.target_url, zapierSubscription.@event,zapierSubscription.filter_on_item_id, zapierSubscription.filter_on_accountable_user_id, zapierSubscription.filter_on_meeting_id);
			return Ok(new {
				subscription_id = sub.Id,
				zapier_id = sub.ZapierId,
			});
		}

		[Route("zapier/unsubscribe/{zapierId}")]
		[HttpDelete]
		public async Task<IHttpActionResult> DeleteZapierSubscription(long zapierId) {
			await ZapierAccessor.DeleteZapierSubscriptions(GetUser(), GetUser().Id, zapierId);
			return Ok();
		}
	
	}
}