using RadialReview.Models;
using RadialReview.Utilities.PermissionsListers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace RadialReview.Accessors {
	public class SelectListAccessor {

		public static List<SelectListItem> GetL10RecurrenceAdminable(UserOrganizationModel caller, long userId, Func<NameIdPermissions, bool> selected = null, bool displayNonAdmin = true) {
			selected = selected ?? new Func<NameIdPermissions, bool>(x => false);
			return L10PermissionsHelper.GetL10RecurrencesAndPermissionsForUser(caller, userId)
							.Where(x => displayNonAdmin || x.CanAdmin)
							.Select(x => new SelectListItem {
								Disabled = !x.CanAdmin,
								Selected = selected(x),
								Text = WrapName(x.Name, "meeting") + (x.CanAdmin ? "" : " <small><i>(You are not an admin for this meeting)<i></small>"),
								Value = "" + x.Id
							}).OrderBy(x => x.Disabled).ToList();
		}

		public static List<SelectListItem> GetL10RecurrenceEditable(UserOrganizationModel caller, long userId, Func<NameIdPermissions, bool> selected = null, bool displayNonEditable = true) {
			selected = selected ?? new Func<NameIdPermissions, bool>(x => false);
			return L10PermissionsHelper.GetL10RecurrencesAndPermissionsForUser(caller, userId)
						.Where(x => displayNonEditable || x.CanEdit)
						.Select(x => new SelectListItem {
							Disabled = !x.CanEdit,
							Selected = selected(x),
							Text = WrapName(x.Name, "meeting") + (x.CanEdit ? "" : " <small><i>(You are not permitted to edit this meeting)<i></small>"),
							Value = "" + x.Id
						}).OrderBy(x => x.Disabled).ToList();
		}


		public static List<SelectListItem> GetUsersWeCanCreateRocksFor(UserOrganizationModel caller, long userId, Func<NameIdCreatablePermissions, bool> selected = null, bool displayNonCreatable = true) {
			selected = selected ?? new Func<NameIdCreatablePermissions, bool>(x => false);
			return UserPermissionsHelper.GetUsersWeCanCreateRocksFor(caller, caller.Id, caller.Organization.Id)
						.Where(x => displayNonCreatable || x.CanCreate)
						.Select(x => new SelectListItem {
							Disabled = !x.CanCreate,
							Selected = selected(x),
							Text = WrapName(x.Name, "user") + (x.CanCreate ? "" : " <small><i>(You are not permitted to edit rocks for this user)<i></small>"),
							Value = "" + x.Id
						}).OrderBy(x => x.Disabled).ToList();
		}

		public static List<SelectListItem> GetUsersWeCanCreateMeaurableFor(UserOrganizationModel caller, long userId, Func<NameIdCreatablePermissions, bool> selected = null, bool displayNonCreatable = true) {
			selected = selected ?? new Func<NameIdCreatablePermissions, bool>(x => false);
			return UserPermissionsHelper.GetUsersWeCanCreateMeasurablesFor(caller, caller.Id, caller.Organization.Id)
						.Where(x => displayNonCreatable || x.CanCreate)
						.Select(x => new SelectListItem {
							Disabled = !x.CanCreate,
							Selected = selected(x),
							Text = WrapName(x.Name,"user") + (x.CanCreate ? "" : " <small><i>(You are not permitted to edit measurables for this user)<i></small>"),
							Value = "" + x.Id
						}).OrderBy(x => x.Disabled).ToList();
		}

		private static string WrapName(string name,string type) {
			if (string.IsNullOrWhiteSpace(name))				
				return "<i>-unnamed "+type+"-</i>";
			return name;
		}
	}
}