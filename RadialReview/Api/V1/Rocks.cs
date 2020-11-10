using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.ViewModels;
using RadialReview.Models.Json;
using RadialReview.Models.Accountability;
using RadialReview.Models.UserModels;
using RadialReview.Models.Askables;
using RadialReview.Models.Rocks;
using RadialReview.Models.Angular.Rocks;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using RadialReview.Models.Enums;

namespace RadialReview.Api.V1
{

    [TTActionWebApiFilter]
    [RoutePrefix("api/v1")]
    public class RocksController : BaseApiController
    {
        /// <summary>
        /// Get a list of your rocks
        /// </summary>
        /// <returns></returns>
        [Route("rocks/user/mine")]
        [HttpGet]
        public async Task<IEnumerable<AngularRock>> GetYourRocks(bool Include_Origin = false)
        {
            var response = await GetRocksForUser(GetUser().Id, Include_Origin);
            return response;
        }

        /// <summary>
        /// Get a list of your archived rocks
        /// </summary>
        /// <returns></returns>
        [Route("archivedrocks/user/mine")]
        [HttpGet]
        public async Task<IEnumerable<AngularRock>> GetYourArchivedRocks()
        {
            return await GetArchivedRocksForUser(GetUser().Id);
        }

        /// <summary>
        /// Get milestones for a particular rock
        /// </summary>
        /// <param name="ROCK_ID">Rock ID</param>
        /// <returns>List of milestones</returns>
        [Route("rocks/{ROCK_ID}/milestones")]
        [HttpGet]
        public IEnumerable<AngularMilestone> GetRocksMilestones(long ROCK_ID)
        {
            return RockAccessor.GetMilestonesForRock(GetUser(), ROCK_ID).Select(x => new AngularMilestone(x));
        }

        public class CreateMilestoneModel
        {
            /// <summary>
            /// Milestone title
            /// </summary>
            [Required]
            public string title { get; set; }
            /// <summary>
            /// Milestone due date
            /// </summary>
            [Required]
            public DateTime dueDate { get; set; }
        }

        /// <summary>
        /// Add a milestone to a particular rock
        /// </summary>
        /// <param name="ROCK_ID">Rock ID</param>
        /// <returns></returns>
        [Route("rocks/{ROCK_ID}/milestones")]
        [HttpPost]
        public async Task<AngularMilestone> AddRocksMilestones(long ROCK_ID, [FromBody]CreateMilestoneModel body)
        {
            var milestone = await RockAccessor.AddMilestone(GetUser(), ROCK_ID, body.title, body.dueDate);
            return new Models.Angular.Rocks.AngularMilestone(milestone);
        }

        public class CreateRockModel
        {
            /// <summary>
            /// Rock name
            /// </summary>
            [Required]
            public string title { get; set; }

            /// <summary>
            /// Rock owner (Default: you)
            /// </summary>
            public long? accountableUserId { get; set; }
        }

        /// <summary>
        /// Create a new rock
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        [Route("rocks/create")]
        [HttpPost]
        public async Task<AngularRock> CreateRock([FromBody]CreateRockModel body)
        {
            var ownerId = (body.accountableUserId == 0 || body.accountableUserId == null) ? GetUser().Id : body.accountableUserId.Value;
            var rock = await RockAccessor.CreateRock(GetUser(), ownerId, message: body.title);
            return new AngularRock(rock, false);
        }

        public class UpdateRockModel
        {
            /// <summary>
            /// Rock name
            /// </summary>
            public string title { get; set; }
            /// <summary>
            /// Rock owner (Default: you)
            /// </summary>
            public long? accountableUserId { get; set; }
            /// <summary>
            /// Rock completion status
            /// </summary>
            public RockState? completion { get; set; }
        }
        /// <summary>
        /// Update a rock
        /// </summary>
        /// <param name="ROCK_ID">Rock ID</param>
        /// <returns></returns>
        [Route("rocks/{ROCK_ID}")]
        [HttpPut]
        public async Task UpdateRocks(long ROCK_ID, [FromBody]UpdateRockModel body)
        {
            await RockAccessor.UpdateRock(GetUser(), ROCK_ID, body.title, body.accountableUserId, body.completion);
        }

        /// <summary>
        /// Delete a rock
        /// </summary>
        /// <param name="ROCK_ID">Rock ID</param>
        /// <returns></returns>
        [Route("rocks/{ROCK_ID}")]
        [HttpDelete]
        public async Task DeleteRocks(long ROCK_ID)
        {
            await RockAccessor.ArchiveRock(GetUser(), ROCK_ID);
        }

        /// <summary>
        /// Get a list of rocks for a user
        /// </summary>
        /// <param name="USER_ID">User ID</param>
        /// <returns></returns>
        [Route("rocks/user/{USER_ID:long}")]
        [HttpGet]
        public async Task<IEnumerable<AngularRock>> GetRocksForUser(long USER_ID, bool Include_Origin = false)
        {
            //return await L10Accessor.GetOrGenerateAngularRecurrence(GetUser(), MEETING_ID);
            var response = RockAccessor.GetRocks(GetUser(), USER_ID).Select(x => new AngularRock(x, false));
            if (Include_Origin)
            {
                response = response.ToList();
                foreach (var rock in response)
                {
                    var l10s = RockAccessor.GetRecurrencesContainingRock(GetUser(), rock.Id);
                    foreach (var l10 in l10s)
                    {
                        //var recurrence = L10Accessor.GetL10Recurrence(GetUser(), l10.RecurrenceId, LoadMeeting.False());
                        rock.Origins.Add(new Models.Application.NameId(l10.Name, l10.RecurrenceId));
                    }
                }
            }
            return response;
        }

        /// <summary>
        /// Get a list of archived rocks for a user
        /// </summary>
        /// <param name="USER_ID">User ID</param>
        /// <returns></returns>
        [Route("archivedrocks/user/{USER_ID:long}")]
        [HttpGet]
        public async Task<IEnumerable<AngularRock>> GetArchivedRocksForUser(long USER_ID)
        {
            return RockAccessor.GetArchivedRocks(GetUser(), USER_ID).Select(x => new AngularRock(x, false));
        }

        /// <summary>
        /// Restore a rock from the archive
        /// </summary>
        /// <param name="ROCK_ID">Rock ID</param>
        /// <returns></returns>
        [HttpPut]
        [Route("rocks/{ROCK_ID:long}/restore")]
        public async Task RestoreRock(long ROCK_ID)
        {
            await RockAccessor.UnArchiveRock(GetUser(), ROCK_ID);
        }

        /// <summary>
        /// Archive the rock
        /// </summary>
        /// <param name="ROCK_ID">Rock ID</param>
        /// <returns></returns>
        [Route("rocks/{ROCK_ID}/archive")]
        [HttpPut]
        public async Task ArchiveRock(long ROCK_ID)
        {
            await RockAccessor.ArchiveRock(GetUser(), ROCK_ID);
        }

        /// <summary>
        /// Get a specific rock
        /// </summary>
        /// <param name="ROCK_ID">Rock ID</param>
        /// <returns>The specified rock</returns>
        // GET: api/rocks/5
        [Route("rocks/{ROCK_ID}")]
        [HttpGet]
        public AngularRock GetRock(long ROCK_ID, bool Include_Origin = false)
        {
            var rock = RockAccessor.GetRock(GetUser(), ROCK_ID);
            var response = new AngularRock(rock, false);
            if (Include_Origin)
            {
                var l10s = RockAccessor.GetRecurrencesContainingRock(GetUser(), rock.Id);
                foreach (var l10 in l10s)
                {
                    response.Origins.Add(new Models.Application.NameId(l10.Name, l10.RecurrenceId));
                }
            }
            return response;
        }
    }

	[RoutePrefix("api/v1")]
	public class MilestonesController : BaseApiController {

		/// <summary>
		/// Get a particular milestone
		/// </summary>
		/// <param name="MILESTONE_ID">Milestone ID</param>
		/// <returns>The specified milestone</returns>
		//[GET/POST/DELETE] /milestones/{MILESTONE_ID}
		[Route("milestones/{MILESTONE_ID}")]
		[HttpGet]
		public AngularMilestone GetMilestones(long MILESTONE_ID) {
			return new AngularMilestone(RockAccessor.GetMilestone(GetUser(), MILESTONE_ID));
		}

		public class UpdateMilestoneModel {
			/// <summary>
			/// Milestone title (Default: unchanged)
			/// </summary>
			public string title { get; set; }//
											 /// <summary>
											 /// Milestone due date (Default: unchanged)
											 /// </summary>
			public DateTime? dueDate { get; set; }
			/// <summary>
			/// Milestone status (Default: unchanged)
			/// </summary>
			public MilestoneStatus? status { get; set; }
		}


		/// <summary>
		/// Update a milestone
		/// </summary>
		/// <param name="MILESTONE_ID">Milestone ID</param>
		/// <param name="body"></param>
		[Route("milestones/{MILESTONE_ID}")]
		[HttpPut]
		public async Task UpdateMilestones(long MILESTONE_ID, [FromBody]UpdateMilestoneModel body)//[FromBody]string title = null, [FromBody]DateTime? dueDate = null, [FromBody]MilestoneStatus? status = null)
		{
			await RockAccessor.EditMilestone(GetUser(), MILESTONE_ID, body.title, body.dueDate, null, body.status);
		}

		/// <summary>
		/// Delete a milestone
		/// </summary>
		/// <param name="MILESTONE_ID">Milestone ID</param>
		[Route("milestones/{MILESTONE_ID}")]
		[HttpDelete]
		public async Task RemoveMilestones(long MILESTONE_ID) {
			await RockAccessor.DeleteMilestone(GetUser(), MILESTONE_ID);
		}


	}

}
