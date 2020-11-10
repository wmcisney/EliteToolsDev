using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using RadialReview.Accessors;
using RadialReview.Models.Application;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Scorecard;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace RadialReview.Api.V1 {

	[RoutePrefix("api/v1")]
	[SwaggerName(Name = "Week")]
	public class Week_Controller : BaseApiController {
		/// <summary>
		/// Get the current week
		/// </summary>
		/// <returns></returns>
		// GET: api/Scores/5
		[Route("weeks/current")]
		[HttpGet]
		public AngularWeek Get() {
			var org = GetUser().Organization;
			var now = DateTime.UtcNow;
			var periods = TimingUtility.GetPeriods(org, now, now, /*null,*/ true, true);

			return new AngularWeek(periods.FirstOrDefault(x => x.IsCurrentWeek));
		}
	}


	[RoutePrefix("api/v1")]
	public class Scores_Controller : BaseApiController {

        #region Models
        public class UpdateMeasurableModel
        {
            /// <summary>
            /// The name of the measurable
            /// </summary>
            public string name { get; set; }
            public Models.Enums.LessGreater direction { get; set; }
            public decimal target { get; set; }
            public Models.Enums.UnitType unitType { get; set; }
            public decimal? altTarget { get; set; }
        }

        public class CreateMeasurableModel
        {
            /// <summary>
            /// Measurable name
            /// </summary>
            public string name { get; set; }

            public Models.Enums.LessGreater direction { get; set; }
            public decimal target { get; set; }
            public Models.Enums.UnitType unitType { get; set; }

            /// <summary>
            /// Measurable owner (Default: you)
            /// </summary>
            public long? accountableUserId { get; set; }
        }

        public class UpdateScoreModel
        {
            /// <summary>
            /// The score's new value. If null, score is deleted.
            /// </summary>
            public decimal? value { get; set; }
        }
        #endregion

        #region POST
        #endregion

        #region PUT
        /// <summary>
		/// Update a score
		/// </summary>
		/// <param name="SCORE_ID">Score ID</param>
		[HttpPut]
        [Route("scores/{SCORE_ID:long}")]
        public async Task Put(long SCORE_ID, [FromBody]UpdateScoreModel body)
        {
            await ScorecardAccessor.UpdateScore(GetUser(), SCORE_ID, body.value);
            ///L10Accessor.UpdateScore(GetUser(), SCORE_ID, body.value, null, true);
        }
        #endregion

        #region GET
        /// <summary>
        /// Get a particular score
        /// </summary>
        /// <param name="SCORE_ID"></param>
        /// <returns></returns>
        // GET: api/Scores/5
        [Route("scores/{SCORE_ID}")]
        [HttpGet]
        public AngularScore Get(long SCORE_ID, bool Include_Origin = false)
        {
			var response = new AngularScore(ScorecardAccessor.GetScore(GetUser(), SCORE_ID), null);
			if (Include_Origin) {
				response.Origins = ScorecardAccessor.GetMeasurablesRecurrences(GetUser(), response.MeasurableId);				
			}
			return response;
		}
        #endregion




    }

	[RoutePrefix("api/v1")]
	public class ScorecardController : BaseApiController {

		[Route("scorecard/user/mine")]
		[HttpGet]
		public async Task<AngularScorecard> GetMineMeasureables() {
			return await GetMeasureablesForUser(GetUser().Id);
		}

		[Route("scorecard/user/{USER_ID}")]
		[HttpGet]
		public async Task<AngularScorecard> GetMeasureablesForUser(long USER_ID) {
			return await ScorecardAccessor.GetAngularScorecardForUser(GetUser(), USER_ID, 13);
		}

		[Route("scorecard/meeting/{MEETING_ID}")]
		[HttpGet]
		public async Task<AngularScorecard> GetScorecardForMeeting(long MEETING_ID) {
			return (await L10Accessor.GetOrGenerateAngularRecurrence(GetUser(), MEETING_ID)).Scorecard;
		}

	}

	[RoutePrefix("api/v1")]
	public class Measurables_Controller : BaseApiController {

        #region Models
        #endregion

        #region POST
        /// <summary>
		/// Create a new measurable
		/// </summary>
		/// <param name="body"></param>
		/// <returns></returns>
		[Route("measurables/create")]
        [HttpPost]
        public async Task<AngularMeasurable> CreateMeasurable([FromBody]Scores_Controller.CreateMeasurableModel body)
        {
            var ownerId = body.accountableUserId == null || body.accountableUserId == 0 ? GetUser().Id : body.accountableUserId.Value;
            var builder = MeasurableBuilder.Build(body.name, ownerId, null, body.unitType, body.target, body.direction);
            var measurable = await ScorecardAccessor.CreateMeasurable(GetUser(), builder);
            return new AngularMeasurable(measurable);
        }
        #endregion

        #region PUT
        /// <summary>
        /// Update a score for a particular measureable
        /// </summary>
        /// <param name="MEASURABLE_ID">Measurable ID</param>
        /// <param name="WEEK_ID">Week ID</param>
        // PUT: api/Scores/5
        [HttpPut]
        [Route("measurables/{MEASURABLE_ID}/week/{WEEK_ID}")]
        public async Task UpdateScore(long MEASURABLE_ID, long WEEK_ID, [FromBody]Scores_Controller.UpdateScoreModel body)
        {
            //await L10Accessor.UpdateScore(GetUser(), MEASURABLE_ID, WEEK_ID, body.value, null, true);
            await ScorecardAccessor.UpdateScore(GetUser(), MEASURABLE_ID, TimingUtility.GetDateSinceEpoch(WEEK_ID), body.value);

        }

        /// <summary>
        /// Update a measureable
        /// </summary>
        /// <param name="MEASURABLE_ID">Measurable ID</param>
        [HttpPut]
        [Route("measurables/{MEASURABLE_ID}")]
        public async Task UpdateMeasurable(long MEASURABLE_ID, [FromBody]Scores_Controller.UpdateMeasurableModel body)
        {
            //await L10Accessor.UpdateScore(GetUser(), MEASURABLE_ID, WEEK_ID, body.value, null, true);
            await ScorecardAccessor.UpdateMeasurable(GetUser(), MEASURABLE_ID, name: body.name, direction: body.direction, target: body.target, unitType: body.unitType, altTarget: body.altTarget);

        }
        #endregion

        #region GET
        /// <summary>
        /// Get measurables that you own
        /// </summary>
        /// <returns></returns>
        [Route("measurables/user/mine")]
        [HttpGet]
        public IEnumerable<AngularMeasurable> GetMineMeasureables(bool Include_Origin = false)
        {
            var response = ScorecardAccessor.GetUserMeasurables(GetUser(), GetUser().Id, true, true).Select(x => new AngularMeasurable(x));
            if (Include_Origin)
            {
                response = response.ToList();
                foreach (var measurable in response)
                {
                    measurable.Origins = ScorecardAccessor.GetMeasurablesRecurrences(GetUser(), measurable.Id);
                }
            }
            return response;
        }

        /// <summary>
        /// Get measurables for a particular user
        /// </summary>
        /// <param name="USER_ID">User ID</param>
        /// <returns></returns>
        [Route("measurables/user/{USER_ID}")]
        [HttpGet]
        public IEnumerable<AngularMeasurable> GetUserMeasureables(long USER_ID, bool Include_Origin = false)
        {
            var response = ScorecardAccessor.GetUserMeasurables(GetUser(), USER_ID, true, true)
                .Select(x => new AngularMeasurable(x));
            if (Include_Origin)
            {
                response = response.ToList();
                foreach (var measurable in response)
                {
                    measurable.Origins = ScorecardAccessor.GetMeasurablesRecurrences(GetUser(), measurable.Id);
                }
            }
            return response;
        }

        /// <summary>
        /// Get scores for a particular measurable
        /// </summary>
        /// <param name="MEASURABLE_ID">Measurable ID</param>
        /// <returns></returns>
        [Route("measurables/{MEASURABLE_ID}/scores")]
        [HttpGet]
        public IEnumerable<AngularScore> GetMeasurableScores(long MEASURABLE_ID, bool Include_Origin = false)
        {
            var response = ScorecardAccessor.GetMeasurableScores(GetUser(), MEASURABLE_ID)
                                    .OrderBy(x => x.DataContract_ForWeek)
                                    .Select(x => new AngularScore(x, null));
			if (Include_Origin) {
				response = response.ToList();
				var origin = ScorecardAccessor.GetMeasurablesRecurrences(GetUser(), MEASURABLE_ID);
				foreach (var score in response)
					score.Origins = origin;
			}

			return response;
        }

        /// <summary>
        /// Get a measurable
        /// </summary>
        /// <param name="MEASURABLE_ID">Measurable ID</param>
        /// <returns></returns>
        [Route("measurables/{MEASURABLE_ID:long}")]
        [HttpGet]
        public AngularMeasurable Get(long MEASURABLE_ID,bool Include_Origin = false)
        {
            var found = ScorecardAccessor.GetMeasurable(GetUser(), MEASURABLE_ID);
            if (found == null)
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            else
            {
                var response = new AngularMeasurable(found);
                if (Include_Origin)
                {
                    response.Origins = ScorecardAccessor.GetMeasurablesRecurrences(GetUser(), response.Id);
                }
                return response;
            }

        }
        #endregion


        ///// <summary>
        ///// Update a score for a particular measureable
        ///// </summary>
        ///// <param name="MEASURABLE_ID">Measurable ID</param>
        ///// <param name="WEEK_ID">Week ID</param>
        //// PUT: api/Scores/5
        //[HttpPut]
        //[Obsolete("Use other")]
        //[Route("measurables/{MEASURABLE_ID}/week/{WEEK_ID}/score")]
        //public void Put_OLD(long MEASURABLE_ID, long WEEK_ID, [FromBody]Scores_Controller.UpdateScoreModel body) {
        //	L10Accessor.UpdateScore(GetUser(), MEASURABLE_ID, WEEK_ID, body.value, null, true);
        //}





        //      /// <summary>
        ///// Delete a measurable
        ///// </summary>
        ///// <param name="MEASURABLE_ID"></param>
        ///// <returns></returns>
        //[Route("measurables/{MEASURABLE_ID}")]
        //      [HttpDelete]
        //      public async Task RemoveMeasurable(long MEASURABLE_ID)
        //      {
        //          await ScorecardAccessor.DeleteMeasurable(GetUser(),MEASURABLE_ID);
        //      }
    }
}
