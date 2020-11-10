using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.Security {
	public class CorsUtility {
		public static bool UrlIsManaged(Uri uri) {
			if (Config.IsLocal()) {
				return true;
			} else {				
				//REPLACE_ME
				if (uri.Host.ToLower() == "dlptools.com" || uri.Host.ToLower().EndsWith(".dlptools.com")) {
					return true;
				} else {
					return false;
				}
			}
		}

		public static bool TryGetAllowedOrigin(Uri uri, out string origin) {
			if (UrlIsManaged(uri)) {
				origin = uri.GetLeftPart(UriPartial.Authority);
				return true;
			} else {
				origin = null;
				return false;
			}
		}
	}
}