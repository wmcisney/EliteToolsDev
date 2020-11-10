using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Todo;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using RadialReview.Utilities.DataTypes;

namespace RadialReview.Api.V1
{    
    [RoutePrefix("api/v1")]
	public class PostQuarterController : BaseApiController
    {
        public class CreateNewQuarterModel
        {
            /// <summary>
            /// New Quarter title
            /// </summary>
            [Required]
            public string name { get; set; }
            /// <summary>
            /// New Quarter date
            /// </summary>
            [Required]
            public DateTime quarterenddate { get; set; }

            /// <summary>
            /// Meeting ID
            /// </summary>
            [Required]
            public long meetingId { get; set; }

        }

        /// <summary>
        /// Create a new post quarter
        /// </summary>
        /// <returns>HTTP response 200</returns>        
        //[Obsolete("Not for public use")]
        [Route("postquarter/create")]
        [HttpPost]
        public async Task<long> Create([FromBody]CreateNewQuarterModel body)
        {
            var postQuarter = new Models.PostQuarter.PostQuarterModel();
            postQuarter.L10RecurrenceId = body.meetingId;
            postQuarter.QuarterEndDate = body.quarterenddate.Date;
            postQuarter.Name = body.name;
            var newQuarter = await PostQuarterAccessor.CreatePostQuarter(GetUser(), postQuarter);            
            return newQuarter.Id;
        }
        
    }
}
