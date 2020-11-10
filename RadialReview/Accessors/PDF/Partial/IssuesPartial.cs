using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Controllers;
using RadialReview.Utilities;

namespace RadialReview.Accessors.PDF.Partial {

	public class IssuesPartialModel {
		public string Number { get; set; }
		public string Issues { get; set; }
		public string Owner { get; set; }
	}

	public class IssuesPartialViewModel {
		public string Company { get; set; }
		public string Image { get; set; }
		public List<IssuesPartialModel> Issues { get; set; }
	}

	public class IssuesPartial : IPdfPartial {
		private readonly IssuesPartialViewModel _viewModel;
		public IssuesPartial(IssuesPartialViewModel viewModel) {
			_viewModel = viewModel;
		}

		private const string _partialView = "~/Views/Quarterly/IssuesPartial.cshtml";

		public string Title => "Issues";

		/// <summary>
		/// Generate Html String from the given template
		/// </summary>
		/// <returns></returns>
		public string Generate() {
			return ViewUtility.RenderPartial(_partialView, _viewModel).Execute();
		}
	}
}