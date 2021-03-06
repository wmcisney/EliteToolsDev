﻿using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TractionTools.Tests.Utilities;
using RadialReview.Accessors;
using RadialReview.Models.Issues;
using System.Threading.Tasks;
using RadialReview.Utilities;
using static RadialReview.Models.PermItem;
using RadialReview.Exceptions;
using RadialReview.Models.Todo;
using System.Collections.Generic;
using RadialReview.Models;

namespace TractionTools.Tests.Permissions {
	[TestClass]
	public class ManagerPermissions : BasePermissionsTest {

		[TestMethod]
		[TestCategory("Permissions")]
		public async Task ManagerAtOrganization() {
			var c =await Ctx.Build();
			foreach (var u in c.AllManagers) {
				c.AssertAll(p => p.ManagerAtOrganization(u.Id, c.Id), c.AllUsers);
			}

			foreach (var u in c.AllNonmanagers) {
				c.AssertAll(p => p.ManagerAtOrganization(u.Id, c.Id));
			}

		}

		[TestMethod]
		[TestCategory("Permissions")]
		public async Task ManagingOrganization() {
			var c =await Ctx.Build();
			c.AssertAll(p => p.ManagingOrganization(c.Id), c.Manager);
		}
		/*
		 
		[TestMethod]
		[TestCategory("Permissions")]
		public void XXX() {
			var c =await Ctx.Build();
			c.AssertAll(p => p.XXX(YYY), c.Manager);
		}

		 */
	}
}
