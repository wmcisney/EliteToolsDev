using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Controllers;
using RadialReview.Utilities;

namespace RadialReview.Accessors.PDF.Partial {

	public class TodosPartialModel {
		public string Number { get; set; }
		public string Todo { get; set; }
		public string Owner { get; set; }
		public string DueDate { get; set; }
	}

	public class TodosPartialViewModel {
		public string Company { get; set; }
		public string Image { get; set; }
		public List<TodosPartialModel> Todos { get; set; }
	}

	public class TodosPartial : IPdfPartial {
		private readonly TodosPartialViewModel _viewModel;
		public TodosPartial(TodosPartialViewModel viewModel) {
			_viewModel = viewModel;
		}

		private const string _partialView = "~/Views/Quarterly/TodosPartial.cshtml";

		public string Title => "To-do List";

		/// <summary>
		/// Generate Html String from the given template
		/// </summary>
		/// <returns></returns>
		public string Generate() {
			return ViewUtility.RenderPartial(_partialView, _viewModel).Execute();
		}
	}
}