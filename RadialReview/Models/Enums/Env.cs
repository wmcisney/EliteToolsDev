using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Enums
{
	public enum Env
	{
		invalid,
		local_sqlite,
		local_mysql,
		production,
        local_test_sqlite,
		dev_testing,
	}

	public enum ApplicationVersion {
		invalid,
		local,
		alpha,
		beta,
		production,
		qa,
		dev,
	}
}