using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Enums;
using RadialReview.Utilities;

namespace RadialReview.Accessors.PDF.Partial {


	public class RocksPartialModel {
		public string Description { get; set; }
		public string Type { get; set; }
		public string Owner { get; set; }
		public RockState? Status { get; set; }

	}

	public class RockGroup {
		public string Owner { get; set; }
		public int Completed { get; set; }
		public int Total { get; set; }

		public string PercentCompletion {
			get { return Total <= 0 ? "0%" : Convert.ToDouble((Convert.ToDouble(Completed) / Convert.ToDouble(Total))).ToString("P0"); }
		}

		public string CompletionDisplay {
			get { return $"{Completed}/{Total} = {PercentCompletion.Replace(" ", "")}"; }
		}
		public List<RocksPartialModel> Rocks { get; set; }
	}

	public class RocksPartialViewModel {
		public string Company { get; set; }
		public string Image { get; set; }
		public string FutureDate { get; set; }

		public List<Tuple<string, string>> Headers { get; set; }

		public List<RocksPartialModel> CompanyRocks { get; set; }
		public List<RocksPartialModel> IndividualRocks { get; set; }

		public RockGroup CompanyRockGroup { get; set; }
		public List<RockGroup> IndvidualRockGroups { get; set; }
	}

	public class RocksPartial : IPdfPartial {
		private readonly RocksPartialViewModel _viewModel;
		private const string _partialView = "~/Views/Quarterly/RocksPartial.cshtml";

		public string Title => "Quarterly Rocks";

		public RocksPartial(RocksPartialViewModel viewModel) {
			_viewModel = viewModel;
		}
		public string Generate() {
			_viewModel.CompanyRockGroup = ComputeCompanyRockGroup(_viewModel.CompanyRocks);
			_viewModel.IndvidualRockGroups = ComputeRockGroups(_viewModel.IndividualRocks);

			return ViewUtility.RenderPartial(_partialView, _viewModel).Execute();

		}

		private RockGroup ComputeCompanyRockGroup(List<RocksPartialModel> viewModelCompanyRocks) {
			return new RockGroup {
				Owner = "COMPANY ROCKS",
				Completed = viewModelCompanyRocks.Count(rock => rock.Status.GetValueOrDefault() == RockState.Complete),
				Total = viewModelCompanyRocks.Count(),
				Rocks = viewModelCompanyRocks
			};
		}

		private List<RockGroup> ComputeRockGroups(List<RocksPartialModel> viewModelIndividualRocks) {

			return viewModelIndividualRocks.GroupBy(ir => ir.Owner)
				.Select(ir2 => new RockGroup {
					Owner = ir2.Key,
					Rocks = ir2.ToList(),
					Completed = ir2.Count(rock => rock.Status.GetValueOrDefault() == RockState.Complete),
					Total = ir2.Count(),
				})
				.ToList();
		}
	}
}