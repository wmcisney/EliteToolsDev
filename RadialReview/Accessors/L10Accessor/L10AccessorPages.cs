using System;
using System.Linq;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using NHibernate;
//using ListExtensions = WebGrease.Css.Extensions.ListExtensions;
//using System.Web.WebPages.Html;

namespace RadialReview.Accessors {
	public partial class L10Accessor : BaseAccessor {
		
		#region Pages


		public static L10Recurrence.L10Recurrence_Page GetPageInRecurrence(UserOrganizationModel caller, long pageId, long recurrenceId) {
#pragma warning disable CS0618 // Type or member is obsolete
			var page = GetPage(caller, pageId);
#pragma warning restore CS0618 // Type or member is obsolete
			if (page.L10RecurrenceId != recurrenceId)
				throw new PermissionsException("Page does not exist.");
			return page;
		}

		[Obsolete("Should you use GetPageInRecurrence?")]
		public static L10Recurrence.L10Recurrence_Page GetPage(UserOrganizationModel caller, long pageId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var page = GetPage(s, perms, pageId);
					return page;
				}
			}
		}
		[Obsolete("Should you use GetPageInRecurrence?")]
		public static L10Recurrence.L10Recurrence_Page GetPage(ISession s, PermissionsUtility perms, long pageId) {
			var page = s.Get<L10Recurrence.L10Recurrence_Page>(pageId);
			perms.ViewL10Recurrence(page.L10Recurrence.Id);
			if (page.DeleteTime != null)
				throw new PermissionsException("Page does not exist.");

			return page;
		}


		public static L10Recurrence.L10Recurrence_Page EditOrCreatePage(UserOrganizationModel caller, L10Recurrence.L10Recurrence_Page page) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var existingPage = s.Get<L10Recurrence.L10Recurrence_Page>(page.Id);

					if (existingPage == null) {
						var ordering = s.QueryOver<L10Recurrence.L10Recurrence_Page>().Where(x => x.DeleteTime == null && x.L10RecurrenceId == page.L10RecurrenceId).RowCount();
						existingPage = new L10Recurrence.L10Recurrence_Page() {
							L10RecurrenceId = page.L10RecurrenceId,
							_Ordering = ordering
						};
					}

					perms.AdminL10Recurrence(existingPage.L10RecurrenceId);
					if (existingPage.L10RecurrenceId != page.L10RecurrenceId)
						throw new PermissionsException("RecurrenceIds do not match");

					existingPage.PageType = page.PageType;
					existingPage.Minutes = page.Minutes;
					existingPage.Title = page.Title;
					existingPage.Subheading = page.Subheading;
					existingPage.DeleteTime = page.DeleteTime;
					existingPage.Url = page.Url;

					s.SaveOrUpdate(existingPage);

					tx.Commit();
					s.Flush();

					return existingPage;
				}
			}
		}

		/*public static string GetPageType_Unsafe(ISession s, string pageName) {
			long pageId;
			if (pageName!=null && long.TryParse(pageName.SubstringAfter("-"), out pageId)) {

				var page = s.Get<L10Recurrence.L10Recurrence_Page>(pageId);
				if (page!=null && page.PageTypeStr!=null)
					return page.PageTypeStr.ToLower();
			}
			return (pageName??"").ToLower();
		}
		*/

		public static void ReorderPage(UserOrganizationModel caller, long pageId, int oldOrder, int newOrder) {

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var found = s.Get<L10Recurrence.L10Recurrence_Page>(pageId);
					PermissionsUtility.Create(s, caller).AdminL10Recurrence(found.L10RecurrenceId);

					var items = s.QueryOver<L10Recurrence.L10Recurrence_Page>().Where(x => x.DeleteTime == null && x.L10RecurrenceId == found.L10RecurrenceId).List().ToList();

					Reordering.Create(items, pageId, found.L10RecurrenceId, oldOrder, newOrder, x => x._Ordering, x => x.Id)
							  .ApplyReorder(s);


					tx.Commit();
					s.Flush();
				}
			}
		}

		#endregion
	}
}
