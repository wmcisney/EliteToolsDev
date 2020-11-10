using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Controllers;
using RadialReview.Utilities;

namespace RadialReview.Accessors.PDF.Partial {

	public class CoverPagePartialViewModel {
		public QuarterlyController.PrintoutPdfOptions PrintoutPdfOptions { get; set; }
		public string UpperHeading { get; set; }
		public string Image { get; set; }
		public string LowerHeading { get; set; }
		public bool UseLogo { get; set; }
		//public string Year { get; set; }
		//public string MeetingName { get; set; }
		//public string DateGenerated { get; set; }

		public IEnumerable<string> CoreValues { get; set; }
		public string PrimaryHeading { get; internal set; }
		public List<QuarterlyPdf.PageNumber> PageNumbers { get; internal set; }
	}
	public class CoverPagePartial : IPdfPartial {
		private readonly CoverPagePartialViewModel _viewModel;
		public CoverPagePartial(CoverPagePartialViewModel viewModel) {
			_viewModel = viewModel;
		}

		private const string _partialView = "~/Views/Quarterly/CoverPagePartial.cshtml";

		public string Title => null;

		/// <summary>
		/// Generate Html String from the given template
		/// </summary>
		/// <returns></returns>
		public string Generate() {
			return ViewUtility.RenderPartial(_partialView, _viewModel).Execute();
		}
	}
}