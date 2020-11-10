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
using RadialReview.Models.Angular.Users;

namespace RadialReview.Api.V1 {
    [RoutePrefix("api/v1")]
    public class Users_Controller :BaseApiController {
        [Route("users/{USER_ID:long}")]
        [HttpGet]
        public AngularUser GetUser(long USER_ID) {
            return AngularUser.CreateUser(UserAccessor.GetUserOrganization(GetUser(), USER_ID, false, false));
        }

        [Route("users/mine")]
        [HttpGet]
        public AngularUser GetMineUser() {
            return GetUser(GetUser().Id);
        }

        /// <summary>
        /// Get direct reports for a particular user
        /// </summary>        
        /// <returns></returns>
        [Route("users/{userId}/directreports")]
        [HttpGet]
        public IEnumerable<AngularUser> GetDirectReports(long userId) 
        {
            return new UserAccessor().GetDirectSubordinates(GetUser(), userId).Select(x => AngularUser.CreateUser(x));
        }

        /// <summary>
        /// Get my direct reports 
        /// </summary>        
        /// <returns></returns>
        [Route("users/minedirectreports")]
        [HttpGet]
        public IEnumerable<AngularUser> GetMineDirectReports()
        {
            return GetDirectReports(GetUser().Id);            
        }

        /// <summary>
        /// Get all viewable users
        /// </summary>        
        /// <returns></returns>
        [Route("users/mineviewable")]
        [HttpGet]
        public IEnumerable<AngularUser> GetMineViewable()
        {
            return OrganizationAccessor.GetAngularUsers(GetUser(), GetUser().Organization.Id);
        }
    }
}