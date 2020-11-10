using System;

namespace TractionTools.Utils.Pdf {
	public enum PdfPageOrientation {
		Portrait,
		Landscape
	}

	public class PdfPageSettings {

		public bool HasFooterOnFirstPage { get; set; }
		public PdfPageOrientation Orientation { get; set; }

		// add settings that is only specific to that generator
		//public object Custom { get; set; }
		// add settings here
	}

}