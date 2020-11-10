using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Enums {
	public enum GenderType {
		NotSpecified = 0,
		Male = 1,
		Female = 2
	}


	public enum ColorMode {
		[Display(Name="No Color Blindness")]
		NoColorBlindness = 0,
		[Display(Name= "Red Blindness")]
		RedBlind = 1,
		[Display(Name = "Green Blindness")]
		GreenBlind = 2,
		[Display(Name = "Blue Blindness")]
		BlueBlind = 3
	}

	public static class ColorModeExtensions {
		public static string ToClassName(this ColorMode mode) {
			switch (mode) {
				case ColorMode.NoColorBlindness:
					return "";
				case ColorMode.RedBlind:
					return "cb-redblind";
				case ColorMode.GreenBlind:
					return "cb-greenblind";
				case ColorMode.BlueBlind:
					return "cb-blueblind";
				default:
					return "cb-undefined";
			}
		}
	}
}