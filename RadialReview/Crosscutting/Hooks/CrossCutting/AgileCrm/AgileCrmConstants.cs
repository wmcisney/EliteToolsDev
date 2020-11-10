using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
namespace RadialReview.AgileCrm {
	public static class AgileCrmConstants {

		public static class AgileCrmConst {
			public const string EMAIL = "email";
			public const string NAME = "name";
			public const string FIRST_NAME = "first_name";
			public const string LAST_NAME = "last_name";
			public const string COMPANY = "company";
			public const string USERID = "UserId";
			public const string ORGID = "OrgId";
			public const string BEEN_A_MEMBER_SINCE = "Been a Member Since";
			public const string IS_AN_ACCOUNT_ADMIN = "Is an Account Admin";
			public const string FIRST_LOGIN_COMPLETED = "First Login Completed";
			public const string FIRST_LOGIN_TIMESTAMP = "First Login Timestamp";
			public const string PAYMENT_STATUS = "Payment Status";
			public const string BILLING_NAME = "Billing Name";
			public const string MEMBER_SINCE = "Member Since";
			public const string COACH = "Coach";
			public const string COACH_TYPE = "Coach Type";
			public const string ENABLE_PEOPLE = "Enable People";
			public const string ENABLE_REVIEWS = "Enable Reviews";
			public const string ENABLE_L10 = "Enable L10";
			public const string ENABLE_AC = "Enable AC";
			public const string OWNER_EMAIL = "owner_email";
			public const string CONTACT_ID = "contact_id";

			public const string SYSTEM = "SYSTEM";
			public const string CUSTOM = "CUSTOM";
		}


		public static readonly string[] OrderedQueues = new[]{
			AgileCrmConst.EMAIL,
			AgileCrmConst.NAME,
			AgileCrmConst.FIRST_NAME,
			AgileCrmConst.LAST_NAME,
			AgileCrmConst.COMPANY,
			AgileCrmConst.USERID,
			AgileCrmConst.ORGID,
			AgileCrmConst.BEEN_A_MEMBER_SINCE,
			AgileCrmConst.IS_AN_ACCOUNT_ADMIN,
			AgileCrmConst.FIRST_LOGIN_COMPLETED,
			AgileCrmConst.FIRST_LOGIN_TIMESTAMP,
			AgileCrmConst.PAYMENT_STATUS,
			AgileCrmConst.BILLING_NAME,
			AgileCrmConst.MEMBER_SINCE,
			AgileCrmConst.COACH,
			AgileCrmConst.COACH_TYPE,
			AgileCrmConst.ENABLE_PEOPLE,
			AgileCrmConst.ENABLE_REVIEWS,
			AgileCrmConst.ENABLE_L10,
			AgileCrmConst.ENABLE_AC,
			AgileCrmConst.SYSTEM,
			AgileCrmConst.CUSTOM,
		};
	}
}