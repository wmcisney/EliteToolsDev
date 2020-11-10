using System;
using System.Web.Http;
using RadialReview.Accessors;
using RadialReview.Models.Rocks;
using RadialReview.Models.L10;
using RadialReview.Models.Angular.Headlines;
using System.Threading.Tasks;
using RadialReview.Api.V0;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Api.V1
{
    [TTActionWebApiFilter]
    [RoutePrefix("api/v1")]
    public class HeadlinesController : BaseApiController
    {
		/// <summary>
		/// Get a specific people headline
		/// </summary>
		/// <param name="HEADLINE_ID">People headline ID</param>
		/// <returns>The people headline</returns>
        //[GET/POST/DELETE] /headline/{id}
        [Route("headline/{HEADLINE_ID}")]
        [HttpGet]		
        public AngularHeadline GetHeadline(long HEADLINE_ID, bool Include_Origin = false)
        {
            var response = new AngularHeadline(HeadlineAccessor.GetHeadline(GetUser(), HEADLINE_ID));
            if (Include_Origin && response.OriginId != 0)
                response.Origin = L10Accessor.GetL10Recurrence(GetUser(), response.OriginId, LoadMeeting.False()).NotNull(x => x.Name);
            return response;
        }

		/// <summary>
		/// Update a People Headline
		/// </summary>
		/// <param name="HEADLINE_ID">People headline ID</param>
		/// <param name="title">Updated title</param>
		[Route("headline/{HEADLINE_ID}")]
        [HttpPut]
        public async Task UpdateHeadlines(long HEADLINE_ID, [FromBody]TitleModel body)
        {
			await HeadlineAccessor.UpdateHeadline(GetUser(), HEADLINE_ID, body.title);
        }

		/// <summary>
		/// Delete a people headline
		/// </summary>
		/// <param name="HEADLINE_ID"></param>
		/// <returns></returns>
		[Route("headline/{HEADLINE_ID}")]
        [HttpDelete]
        public async Task RemoveHeadlines(long HEADLINE_ID)
        {
			var nil_l10Id = 0; /*RecurrenceId is not needed*/
            await L10Accessor.Remove(GetUser(), new AngularHeadline() { Id = HEADLINE_ID }, nil_l10Id, null);
        }

        /// <summary>
		/// Get all headlines owned by a user.
		/// </summary>
		/// <param name="USER_ID"></param>
		/// <returns>List of the user's headlines</returns>
		[Route("headline/users/{USER_ID:long}")]
        [HttpGet]
        public IEnumerable<AngularHeadline> GetUserHeadlines(long USER_ID, bool Include_Origin = false)
        {
            var response = HeadlineAccessor.GetHeadlinesForUser(GetUser(), USER_ID).Select(x => new AngularHeadline(x));
            if (Include_Origin)
            {
                response = response.ToList();
                foreach (var headline in response)
                    if (headline.OriginId != 0)
                        headline.Origin = L10Accessor.GetL10Recurrence(GetUser(), headline.OriginId, LoadMeeting.False()).NotNull(x => x.Name);
            }            
            return response;
        }

        /// <summary>
		/// Get headlines you own
		/// </summary>		
		/// <returns>List of the headlines own by you</returns>
        [Route("headline/users/mine")]
        [HttpGet]
        public IEnumerable<AngularHeadline> GetMineHeadlines(bool Include_Origin = false)
        {
            var response = HeadlineAccessor.GetHeadlinesForUser(GetUser(), GetUser().Id).Select(x => new AngularHeadline(x));
            if (Include_Origin)
            {
                response = response.ToList();
                foreach (var headline in response)
                    if (headline.OriginId != 0)
                        headline.Origin = L10Accessor.GetL10Recurrence(GetUser(), headline.OriginId, LoadMeeting.False()).NotNull(x => x.Name);
            }
            return response;
        }
    }
}