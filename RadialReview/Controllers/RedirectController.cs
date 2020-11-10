using NHibernate;
using RadialReview.Accessors;
using RadialReview.Utilities;
using RadialReview.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers {
	public class RedirectController : BaseController {

        [Access(AccessLevel.Any)]
        public ActionResult BuyBooks() {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
					//REPLACE_ME
                    var url = s.GetSettingOrDefault("EOS_LIBRARY_LINK", "dlptools.com");
                    tx.Commit();
                    s.Flush();
                    return Redirect(url);
                }
            }
        }

        [Access(AccessLevel.Any)]
        public ActionResult Feedback() {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
					//REPLACE_ME
                    var url = s.GetSettingOrDefault("BETA_FEEDBACK_LINK", "https://dlptools.happyfox.com/new");
                    tx.Commit();
                    s.Flush();
                    return Redirect(url);
                }
            }
        }
    }
}