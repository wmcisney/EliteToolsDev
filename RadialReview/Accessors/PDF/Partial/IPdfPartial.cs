using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Accessors.PDF.Partial {
	interface IPdfPartial {
		string Generate();
		string Title { get; }
	}
}