using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Controllers;
using RadialReview.Utilities;

namespace RadialReview.Accessors.PDF.Partial {

	public class HeadlinesPartialModel {
		public string Headline { get; set; }
		public string Owner { get; set; }
	}

	public class HeadlinesPartialViewModel {
		public string Company { get; set; }
		public string Image { get; set; }
		public List<HeadlinesPartialModel> Headlines { get; set; }
	}

	public class HeadlinesPartial : IPdfPartial {
		private readonly HeadlinesPartialViewModel _viewModel;
		public HeadlinesPartial(HeadlinesPartialViewModel viewModel) {
			_viewModel = viewModel;
		}

		private const string _partialView = "~/Views/Quarterly/HeadlinesPartial.cshtml";

		public string Title => "People Headlines";

		/// <summary>
		/// Generate Html String from the given template
		/// </summary>
		/// <returns></returns>
		public string Generate() {
			return ViewUtility.RenderPartial(_partialView, _viewModel).Execute();
		}
	}
}