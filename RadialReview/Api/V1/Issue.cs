using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.Issues;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using static RadialReview.Accessors.IssuesAccessor;

namespace RadialReview.Api.V1 {

    [TTActionWebApiFilter]
    [RoutePrefix("api/v1")]
    public class IssuesController : BaseApiController {

        #region Models
        public class CreateIssueModel
        {
            ///<summary>
            ///Level 10 meeting ID
            ///</summary>
            [Required]
            public long meetingId { get; set; }
            ///<summary>
            ///Title for the issue
            ///</summary>
            [Required]
            public string title { get; set; }
            ///<summary>
            ///Owner's user ID (Default: you)
            ///</summary>
            public long? ownerId { get; set; }
            ///<summary>
            ///Optional issue notes (Default: none)
            ///</summary>
            public string notes { get; set; }
        }

        public class UpdateIssueModelCompletion
        {            ///<summary>
                     ///Set the issue completion
                     ///</summary>
            public bool? complete { get; set; }
        }

        public class UpdateIssueModel
        {
            ///<summary>
            ///Title for the issue
            ///</summary>
            public string title { get; set; }
            ///<summary>
            ///Owner's user ID
            ///</summary>
            public long? ownerId { get; set; }

            /// <summary>
            /// The compartment this issue belongs to. Short Term = Level 10, Long Term = V/TO 
            /// </summary>
            public IssueCompartment? compartment { get; set; }
        }
        #endregion       

        #region POST

        /// <summary>
        /// Create a new issue in for a Level 10
        /// </summary>
        /// <returns>The created issue</returns>
        // Put: api/Issue/mine
        [Route("issues/create")]
        [HttpPost]
        public async Task<AngularIssue> CreateIssue([FromBody]CreateIssueModel body)
        {
            body.ownerId = body.ownerId ?? GetUser().Id;
            //var issue = new IssueModel() { Message = body.title, Description = body.details };
            var creation = IssueCreation.CreateL10Issue(body.title, body.notes, body.ownerId, body.meetingId);
            var success = await IssuesAccessor.CreateIssue(GetUser(), creation);// body.meetingId, body.ownerId.Value, issue);
            return new AngularIssue(success.IssueRecurrenceModel);
        }

        /// <summary>
        /// Move issue to VTO
        /// </summary>
        /// <returns>HTTP response 200</returns>
        // Put: api/Issues/movetovto
        [Route("issues/{ISSUE_ID}/movetovto/")]
        [HttpPost]
        public async Task<IHttpActionResult> MoveToVto(long ISSUE_ID)
        {
            L10Accessor.MoveIssueToVto(GetUser(), ISSUE_ID, null);
            return Ok();
        }

        /// <summary>
        /// Move issue from VTO
        /// </summary>
        /// <returns>HTTP response 200</returns>
        // Put: api/Issues/movefromvto
        [Route("issues/{ISSUE_ID}/movefromvto/")]
        [HttpPost]
        public async Task<IHttpActionResult> MoveFromVto(long ISSUE_ID)
        {
            var vtoItem = VtoAccessor.GetVTOIssueByIssueId(GetUser(), ISSUE_ID);
            await L10Accessor.MoveIssueFromVto(GetUser(), vtoItem.Id);
            return Ok();
        }

        /// <summary>
        /// Move issue to another meeting
        /// </summary>
        /// <returns>HTTP response 200</returns>
        // Put: api/Issues/complete
        [Route("issues/{ISSUE_ID}/movetomeeting/{MEETING_ID}")]
        [HttpPost]
        public async Task<IHttpActionResult> MoveToMeeting(long ISSUE_ID, long MEETING_ID)
        {
            IssuesAccessor.CopyIssue(GetUser(), ISSUE_ID, MEETING_ID);
            return Ok();
        }

        /// <summary>
        /// Mark issue as completed
        /// </summary>
        /// <returns>HTTP response 200</returns>
        // Put: api/Issues/complete
        [Route("issues/{ISSUE_ID}/complete/")]
        [HttpPost]
        public async Task<IHttpActionResult> Complete(long ISSUE_ID, [FromBody]UpdateIssueModelCompletion body)
        {
            //await L10Accessor.CompleteIssue(GetUser(), ISSUE_ID);
            await IssuesAccessor.EditIssue(GetUser(), ISSUE_ID, complete: body.complete ?? true);
            return Ok();
        }
        #endregion

        #region PUT
        /// <summary>
		/// Update an issue
		/// </summary>
		/// <param name="ISSUE_ID">Issue ID</param>
		/// <returns></returns>
		[Route("issues/{ISSUE_ID}")]
        [HttpPut]
        public async Task EditIssue(long ISSUE_ID, [FromBody]UpdateIssueModel body)
        {
            await IssuesAccessor.EditIssue(GetUser(), ISSUE_ID, message: body.title, owner: body.ownerId, compartment: body.compartment);
        }
        #endregion

        #region GET

        /// <summary>
        /// Get a specific issue
        /// </summary>
        /// <param name="ISSUE_ID">Issue ID</param>
        /// <returns>The specified issue</returns>
        // GET: api/Issue/5
        [Route("issues/{ISSUE_ID}")]
        [HttpGet]
        public AngularIssue Get(long ISSUE_ID)
        {
            var model = IssuesAccessor.GetIssue_Recurrence(GetUser(), ISSUE_ID);
            var response = new AngularIssue(model);
            return response;
        }

        /// <summary>
        /// Get all issues you own.
        /// </summary>
        /// <returns>List of your issues</returns>
        [Route("issues/users/mine")]
        [HttpGet]
        public IEnumerable<AngularIssue> GetMineIssues()
        {
            List<IssueModel.IssueModel_Recurrence> list = IssuesAccessor.GetVisibleIssuesForUser(GetUser(), GetUser().Id);
            return list.Select(x => new AngularIssue(x));
        }
        /// <summary>
        /// Get all issues owned by a user.
        /// </summary>
        /// <param name="USER_ID"></param>
        /// <returns>List of the user's issues</returns>
        [Route("issues/users/{USER_ID:long}")]
        [HttpGet]
        public IEnumerable<AngularIssue> GetUserIssues(long USER_ID)
        {
            List<IssueModel.IssueModel_Recurrence> list = IssuesAccessor.GetVisibleIssuesForUser(GetUser(), USER_ID);
            return list.Select(x => new AngularIssue(x));
        }

        #endregion












    }
}
