using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview;
using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static RadialReview.Models.PermItem;

namespace TractionTools.Tests.Permissions {
	[TestClass]
	public class RockPermissionTests : BasePermissionsTest {

		[TestMethod]
		[TestCategory("Permissions")]
		public async Task ViewRock() {
			var c = await Ctx.Build();
			var l10 = await L10Accessor.CreateBlankRecurrence(c.Manager, c.Id, false);

			MockHttpContext();
			var rock = await L10Accessor.CreateAndAttachRock(c.Manager, l10.Id, c.Middle.Id, "rock");
			//Without Restriction
			{
				DbCommit(s => {
					var o = s.Get<OrganizationModel>(c.Org.Id);
					o._Settings.OnlySeeRocksAndScorecardBelowYou = false;
					s.Update(o);
				});

				c.AssertAll(p => p.ViewRock(rock.Id), c.AllUsers);
			}

			//With Restriction
			{
				DbCommit(s => {
					var o = s.Get<OrganizationModel>(c.Org.Id);
					o._Settings.OnlySeeRocksAndScorecardBelowYou = true;
					s.Update(o);
				});

				c.AssertAll(p => p.ViewRock(rock.Id), c.Middle, c.Manager);

			}
			//With Restriction and L10
			{
				await L10Accessor.AddAttendee(c.Manager, l10.Id, c.E5.Id);
				c.AssertAll(p => p.ViewRock(rock.Id), c.Middle, c.Manager, c.E5);
			}
		}

		[TestMethod]
		[TestCategory("Permissions")]
		public async Task EditRock_SeeAllOrgRocks_ManagerCannotEditSelf_UserCannotEditSelf() {
			var c = await Ctx.Build();
			var l10 = await L10Accessor.CreateBlankRecurrence(c.Middle, c.Id, false);
			MockHttpContext();
			var rock = await L10Accessor.CreateAndAttachRock(c.Manager, l10.Id, c.E5.Id, "rock");
			OrganizationModel org = null;
			DbCommit(s => {
				org = s.Get<OrganizationModel>(c.Org.Id);
				org._Settings.OnlySeeRocksAndScorecardBelowYou = false;
				org._Settings.ManagersCanEditSelf = false;
				org._Settings.EmployeesCanEditSelf = false;
				s.Update(org);
			});
			Assert.IsFalse(org.Settings.OnlySeeRocksAndScorecardBelowYou);
			Assert.IsFalse(org.Settings.EmployeesCanEditSelf);
			Assert.IsFalse(org.Settings.ManagersCanEditSelf);

			var perm = new Action<PermissionsUtility>(p => p.EditRock(rock.Id));

			c.AssertAll(perm, c.Manager, c.Middle, c.E1);

			//Add attendee E5
			await L10Accessor.AddAttendee(c.Manager, l10.Id, c.E5.Id);
			c.AssertAll(perm, c.Manager, c.Middle, /*c.E5, Cannot Edit Self*/ c.E1);

			//Add attendee E4
			await L10Accessor.AddAttendee(c.Manager, l10.Id, c.E4.Id);
			c.AssertAll(perm, c.Manager, c.Middle, /*c.E5, Cannot Edit Self*/ c.E1, c.E4);


			///Revoke permissions
			var allPerms = PermissionsAccessor.GetPermItems(c.Manager, l10.Id, ResourceType.L10Recurrence);
			//Remove Creator
			{
				var creator = allPerms.Items.First(x => x.AccessorType == AccessType.Creator);
				PermissionsAccessor.EditPermItem(c.Manager, creator.Id, false, false, null);
				c.AssertAll(perm, c.Manager, /*c.E5, Cannot Edit Self*/ c.E1, c.E4);
			}
			//Remove Admin
			{
				var admin = allPerms.Items.First(x => x.AccessorType == AccessType.Admins);
				PermissionsAccessor.EditPermItem(c.Manager, admin.Id, false, false, null);
				c.AssertAll(perm, /*c.E5, Cannot Edit Self*/ c.Manager, c.E1, c.E4);
			}

			//Remove members
			{
				var member = allPerms.Items.First(x => x.AccessorType == AccessType.Members);
				PermissionsAccessor.EditPermItem(c.Manager, member.Id, false, false, null);
				c.AssertAll(perm, c.Manager, c.E1);
			}
		}

		[TestMethod]
		[TestCategory("Permissions")]
		public async Task EditRock_OnlySeeUnder_ManagerCannotEditSelf_UserCannotEditSelf() {
			var c = await Ctx.Build();
			var l10 = await L10Accessor.CreateBlankRecurrence(c.Middle, c.Id, false);
			MockHttpContext();
			var rock = await L10Accessor.CreateAndAttachRock(c.Manager, l10.Id, c.E5.Id, "rock");
			OrganizationModel org = null;
			List<PermItem> permItems = null;
			DbCommit(s => {
				org = s.Get<OrganizationModel>(c.Org.Id);
				org._Settings.OnlySeeRocksAndScorecardBelowYou = true;
				org._Settings.ManagersCanEditSelf = false;
				org._Settings.EmployeesCanEditSelf = false;
				s.Update(org);
				permItems = s.QueryOver<PermItem>().Where(x => x.ResId == l10.Id && x.ResType == ResourceType.L10Recurrence).List().ToList();
			});


			Assert.IsTrue(org.Settings.OnlySeeRocksAndScorecardBelowYou);
			Assert.IsFalse(org.Settings.EmployeesCanEditSelf);
			Assert.IsFalse(org.Settings.ManagersCanEditSelf);
		


			var perm = new Action<PermissionsUtility>(p => p.EditRock(rock.Id));

			c.AssertAll(perm, c.Middle, c.Manager, c.E1);

			//Add attendee E5
			await L10Accessor.AddAttendee(c.Manager, l10.Id, c.E5.Id);
			c.AssertAll(perm, c.Manager, c.Middle, /* c.E5, Cannot Edit Self */ c.E1);

			//Add attendee E4
			await L10Accessor.AddAttendee(c.Manager, l10.Id, c.E4.Id);
			c.AssertAll(perm, c.Manager, c.Middle, /* c.E5, Cannot Edit Self */ c.E1, c.E4);


			///Revoke permissions
			var allPerms = PermissionsAccessor.GetPermItems(c.Manager, l10.Id, ResourceType.L10Recurrence);
			//Remove Creator
			{
				var creator = allPerms.Items.First(x => x.AccessorType == AccessType.Creator);
				PermissionsAccessor.EditPermItem(c.Manager, creator.Id, false, false, null);
				c.AssertAll(perm, c.Manager, /* c.E5, Cannot Edit Self */ c.E1, c.E4);
			}
			//Remove Admin
			{
				var admin = allPerms.Items.First(x => x.AccessorType == AccessType.Admins);
				PermissionsAccessor.EditPermItem(c.Manager, admin.Id, false, false, null);
				c.AssertAll(perm, /* c.E5, Cannot Edit Self */ c.Manager, c.E1, c.E4);
			}

			//Remove members
			{
				var member = allPerms.Items.First(x => x.AccessorType == AccessType.Members);
				PermissionsAccessor.EditPermItem(c.Manager, member.Id, false, false, null);
				c.AssertAll(perm, c.Manager, c.E1);
			}
		}

		[TestMethod]
		[TestCategory("Permissions")]
		public async Task EditRock_OnlySeeUnder_ManagerCannotEditSelf_UserEditSelf() {
			var c = await Ctx.Build();
			var l10 = await L10Accessor.CreateBlankRecurrence(c.Middle, c.Id, false);
			MockHttpContext();
			var rock = await L10Accessor.CreateAndAttachRock(c.Manager, l10.Id, c.E5.Id, "rock");
			OrganizationModel org = null;
			List<PermItem> permItems = null;
			DbCommit(s => {
				org = s.Get<OrganizationModel>(c.Org.Id);
				org._Settings.OnlySeeRocksAndScorecardBelowYou = true;
				org._Settings.ManagersCanEditSelf = false;
				org._Settings.EmployeesCanEditSelf = true;
				s.Update(org);
				permItems = s.QueryOver<PermItem>().Where(x => x.ResId == l10.Id && x.ResType == ResourceType.L10Recurrence).List().ToList();
			});


			Assert.IsTrue(org.Settings.OnlySeeRocksAndScorecardBelowYou);
			Assert.IsTrue(org.Settings.EmployeesCanEditSelf);
			Assert.IsFalse(org.Settings.ManagersCanEditSelf);



			var perm = new Action<PermissionsUtility>(p => p.EditRock(rock.Id));

			c.AssertAll(perm, c.Middle, c.E5, c.Manager, c.E1);

			//Add attendee E5
			await L10Accessor.AddAttendee(c.Manager, l10.Id, c.E5.Id);
			c.AssertAll(perm, c.Manager, c.Middle, c.E5, c.E1);

			//Add attendee E4
			await L10Accessor.AddAttendee(c.Manager, l10.Id, c.E4.Id);
			c.AssertAll(perm, c.Manager, c.Middle, c.E5, c.E1, c.E4);


			///Revoke permissions
			var allPerms = PermissionsAccessor.GetPermItems(c.Manager, l10.Id, ResourceType.L10Recurrence);
			//Remove Creator
			{
				var creator = allPerms.Items.First(x => x.AccessorType == AccessType.Creator);
				PermissionsAccessor.EditPermItem(c.Manager, creator.Id, false, false, null);
				c.AssertAll(perm, c.Manager, c.E5, c.E1, c.E4);
			}
			//Remove Admin
			{
				var admin = allPerms.Items.First(x => x.AccessorType == AccessType.Admins);
				PermissionsAccessor.EditPermItem(c.Manager, admin.Id, false, false, null);
				c.AssertAll(perm, c.E5, c.Manager, c.E1, c.E4);
			}

			//Remove members
			{
				var member = allPerms.Items.First(x => x.AccessorType == AccessType.Members);
				PermissionsAccessor.EditPermItem(c.Manager, member.Id, false, false, null);
				c.AssertAll(perm, c.Manager, c.E5, c.E1);
			}
		}

		[TestMethod]
		[TestCategory("Permissions")]
		public async Task EditRock_OutsideMeeting() {
			var c = await Ctx.Build();
			var rock = new RockModel() {
				ForUserId = c.E2.Id,
				Rock = "Rock"
			};
			MockHttpContext();
			await RockAccessor.EditRocks(c.Middle, c.E2.Id, rock.AsList(), false, false);
			var perm = new Action<PermissionsUtility>(p => p.EditRock(rock.Id));

			await OrganizationAccessor.Edit(c.Manager, c.Id, managersCanEditSelf: false, employeesCanEditSelf: false);
			c.AssertAll(perm, c.Manager, c.Middle);
			await OrganizationAccessor.Edit(c.Manager, c.Id, managersCanEditSelf: true, employeesCanEditSelf: false);
			c.AssertAll(perm, c.Manager, c.Middle, c.E2);
			await OrganizationAccessor.Edit(c.Manager, c.Id, managersCanEditSelf: false, employeesCanEditSelf: true);
			c.AssertAll(perm, c.Manager, c.Middle, c.E2);


			rock = new RockModel() {
				ForUserId = c.E6.Id,
				Rock = "Rock2"
			};
			await RockAccessor.EditRocks(c.Middle, c.E6.Id, rock.AsList(), false, false);

			await OrganizationAccessor.Edit(c.Manager, c.Id, managersCanEditSelf: false, employeesCanEditSelf: false);
			c.AssertAll(perm, c.Manager, c.Middle, c.E2);
			await OrganizationAccessor.Edit(c.Manager, c.Id, managersCanEditSelf: true, employeesCanEditSelf: false);
			c.AssertAll(perm, c.Manager, c.Middle, c.E2);
			await OrganizationAccessor.Edit(c.Manager, c.Id, managersCanEditSelf: false, employeesCanEditSelf: true);
			c.AssertAll(perm, c.Manager, c.Middle, c.E2, c.E6);


		}


		/*
		 
		[TestMethod]
		[TestCategory("Permissions")]
		public void XXX() {
			var c = await Ctx.Build();
			c.AssertAll(p => p.XXX(YYY), c.Manager);
		}

		 */
	}
}
