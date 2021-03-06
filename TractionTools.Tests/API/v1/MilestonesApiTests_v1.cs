﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors;
using RadialReview.Models.Todo;
using RadialReview.Utilities.DataTypes;
using System.Collections.Generic;
using RadialReview.Utilities;
using RadialReview.Models.Enums;
using TractionTools.Tests.TestUtils;
using RadialReview.Models.L10;
using RadialReview.Models;
using System.Linq;
using RadialReview.Api.V0;
using static TractionTools.Tests.Permissions.BasePermissionsTest;
using System.Threading.Tasks;
using RadialReview.Models.Angular.Todos;
using RadialReview.Controllers;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Scorecard;
using static RadialReview.Controllers.L10Controller;
using RadialReview.Models.Askables;
using RadialReview.Models.Angular.Accountability;
using TractionTools.Tests.Properties;
using RadialReview.Api.V1;

namespace TractionTools.Tests.Api.v1 {
	[TestClass]
	public class MilestonesApiTests_v1 : BaseApiTest {
		public MilestonesApiTests_v1() : base(VERSION_1) {
		}

		[TestMethod]
		[TestCategory("Api_V1")]
		public async Task TestGetMilestones() {
			var c = await Ctx.Build();
			var milestonesController = new MilestonesController();
			milestonesController.MockUser(c.E1);

			var _recurrence = await L10Accessor.CreateBlankRecurrence(c.E1, c.E1.Organization.Id, false);
			await L10Accessor.AddAttendee(c.E1, _recurrence.Id, c.E1.Id);
			//var rock = new RockModel() {
			//	OrganizationId = c.E1.Organization.Id,
			//	ForUserId = c.E1.Id,
			//};
			MockHttpContext();
			var rock = await L10Accessor.CreateAndAttachRock(c.E1, _recurrence.Id, c.E1.Id, null, true);
			var getRocks = RockAccessor.GetRocks(c.E1, c.E1.Id);

			string name = "TestMilestone";
			DateTime date = DateTime.UtcNow.AddDays(7);
			var milestone =await RockAccessor.AddMilestone(c.E1, getRocks.FirstOrDefault().Id, name, date);

			var getRocksMilestones = milestonesController.GetMilestones(milestone.Id);
			CompareModelProperties(/*APIResult.MilestonesApiTests_v0_TestGetMilestones*/ getRocksMilestones);
			Assert.AreEqual(name, getRocksMilestones.Name);

			Assert.IsTrue(Math.Abs((getRocksMilestones.DueDate - date).Value.TotalSeconds) <= 1);

			Assert.AreEqual(milestone.Id, getRocksMilestones.Id);
		}

		[TestMethod]
		[TestCategory("Api_V1")]
		public async Task TestUpdateMilestones() {
			var c = await Ctx.Build();
			var milestonesController = new MilestonesController();
			milestonesController.MockUser(c.E1);

			var _recurrence = await L10Accessor.CreateBlankRecurrence(c.E1, c.E1.Organization.Id, false);
			await L10Accessor.AddAttendee(c.E1, _recurrence.Id, c.E1.Id);

			//var rock = new RockModel() {
			//	OrganizationId = c.E1.Organization.Id,
			//	ForUserId = c.E1.Id,
			//};
			var name = "TestMilestone_updated";

			MockHttpContext();
			var rock = await L10Accessor.CreateAndAttachRock(c.E1, _recurrence.Id, c.E1.Id, null);
			//await L10Accessor.CreateRock(c.E1, _recurrence.Id, AddRockVm.CreateRock(_recurrence.Id, rock, true));
			var getRocks = RockAccessor.GetRocks(c.E1, c.E1.Id);

			var addRocksMilestones =await RockAccessor.AddMilestone(c.E1, getRocks.FirstOrDefault().Id, "TestMilestone", DateTime.Now.AddDays(7));

			//Update Milestone
			milestonesController.UpdateMilestones(addRocksMilestones.Id, new MilestonesController.UpdateMilestoneModel {
				title = name,
			});

			var getRocksMilestones = milestonesController.GetMilestones(addRocksMilestones.Id);

			Assert.AreEqual(name, getRocksMilestones.Name);
		}


		[TestMethod]
		[TestCategory("Api_V1")]
		public async Task TestRemoveMilestones() {
			var c = await Ctx.Build();
			var milestonesController = new MilestonesController();
			milestonesController.MockUser(c.E1);

			var _recurrence = await L10Accessor.CreateBlankRecurrence(c.E1, c.E1.Organization.Id, false);
			await L10Accessor.AddAttendee(c.E1, _recurrence.Id, c.E1.Id);

			//var rock = new RockModel() {
			//	OrganizationId = c.E1.Organization.Id,
			//	ForUserId = c.E1.Id,
			//};
			MockHttpContext();
			var rock = await L10Accessor.CreateAndAttachRock(c.E1, _recurrence.Id, c.E1.Id, null);
			//await L10Accessor.CreateRock(c.E1, _recurrence.Id, AddRockVm.CreateRock(_recurrence.Id, rock, true));
			var getRocks = RockAccessor.GetRocks(c.E1, c.E1.Id);

			var addRocksMilestones =await RockAccessor.AddMilestone(c.E1, getRocks.FirstOrDefault().Id, "TestMilestone", DateTime.Now.AddDays(7));

			//remove milestone
			milestonesController.RemoveMilestones(addRocksMilestones.Id);

			var getRocksMilestones = RockAccessor.GetMilestone(c.E1, addRocksMilestones.Id);

			Assert.IsNotNull(getRocksMilestones.DeleteTime);
		}
	}
}
