using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadialReview.Controllers
{
    public enum AccessLevel
    {
		//NoDataInitialize = -2, //Skip the ViewBag initialization
        SignedOut=-1,
		Any = 1,
		User = 2,
        UserOrganization = 3,
        Manager = 4,
		Radial = 5,
		RadialData = 6,
	}
}
