﻿using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Utilities;
using log4net;

namespace RadialReview.Hubs
{
	public class BaseHub : Hub {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		protected UserAccessor _UserAccessor = new UserAccessor();
		public static String REGISTERED_KEY = "BaseHubRegistered_";

		private UserOrganizationModel _CurrentUser = null;
		private string _CurrentUserOrganizationId = null;
		private UserOrganizationModel ForceGetUser(ISession s, string userId)
		{
			var user = s.Get<UserModel>(userId);
			if (user.IsRadialAdmin) {
				_CurrentUser = s.Get<UserOrganizationModel>(user.CurrentRole);
				if (Config.IsTest()) {
					_CurrentUser._IsTestAdmin = true;
				}

			} else {
				if (user.CurrentRole == 0) {
					if (user.UserOrganizationIds != null && user.UserOrganizationIds.Count() == 1) {
						user.CurrentRole = user.UserOrganizationIds[0];
						s.Update(user);
					} else {
						throw new OrganizationIdException();
					}
				}

				var found = s.Get<UserOrganizationModel>(user.CurrentRole);
				if (found.DeleteTime != null || found.User.Id == userId) {
					//Expensive
					var avail = user.UserOrganization.ToListAlive();
					_CurrentUser = avail.FirstOrDefault(x => x.Id == user.CurrentRole);
					if (_CurrentUser == null)
						_CurrentUser = avail.FirstOrDefault();
					if (_CurrentUser == null) {
						try {
							log.Info($@"No user exists: ({user.CurrentRole}) ({found.User.Id}) ({found.DeleteTime})");
						} catch (Exception) {
						}
						throw new NoUserOrganizationException("No user exists.");
					}
				} else {
					_CurrentUser = found;
				}


			}
			return _CurrentUser;
		}

		protected UserOrganizationModel GetUser(){
			if (_CurrentUser != null)
				return _CurrentUser;

			var userId = Context.User.Identity.GetUserId();
			if (userId==null)
				throw new LoginException("Not logged in.");

			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					return ForceGetUser(s, userId);
				}
			}
		}		

		protected UserOrganizationModel GetUser(ISession s){
			if (_CurrentUser != null)
				return _CurrentUser;
			var userId = Context.User.Identity.GetUserId();
			if (userId == null)
				throw new LoginException("Not logged in.");
			return ForceGetUser(s, userId);
		}		
	}
}