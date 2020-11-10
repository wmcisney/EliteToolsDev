using PdfSharp.Drawing;
using RadialReview.Models;

namespace RadialReview.Accessors.PDF {
    public partial class AccountabilityChartPDF {
		//public class AccountabilityChartSettings {
		//	public Color FillColor { get; set; }
		//	public Color TextColor { get; set; }
		//	public Color BorderColor { get; set; }
		//}


		public class AccountabilityChartSettings {
			public XUnit pageWidth { get; set; }
			public XUnit pageHeight { get; set; }
			public XUnit margin { get; set; }
            public XColor? lineColor { get; set; }
            public XPen linePen() {
                return new XPen(lineColor ?? XColors.Gray, .5 * scale) {
                    LineJoin = XLineJoin.Miter,
                    MiterLimit = 10,
                    LineCap = XLineCap.Square,
                };
			}
			public XColor? boxColor { get; set; }
			public XColor? emphasisColor { get; set; }
			public XColor? demphasisColor { get; set; }
			public XPen boxPen(NodeEmphasis emphasis=NodeEmphasis.Normal) {
				var color = boxColor ?? XColors.Black;
				var width = 1;
				switch (emphasis) {
					case NodeEmphasis.Emphasize:
						color = emphasisColor ?? color;
						width = 4;
						break;
					case NodeEmphasis.Deemphasize:
						color = demphasisColor ?? color;
						break;
					default:
						break;
				}

				return new XPen(color, width*scale);
            }
			public XBrush brush = new XSolidBrush(XColors.Transparent);

			public double scale = 1;

			public XUnit allowedWidth {get {return pageWidth - 2 * margin;}}
			public XUnit allowedHeight {get {return pageHeight - 2 * margin;}}

			public AccountabilityChartSettings() {}
			

			public AccountabilityChartSettings(OrganizationModel.OrganizationSettings settings) {
				boxColor = settings.PrimaryColor.ToXColor();
				lineColor = XColors.Gray;//settings.PrimaryColor.ToXColor();
                //  linePen = new XPen(, .5) {
				//	LineJoin = XLineJoin.Miter,
				//	MiterLimit = 10,
				//	LineCap = XLineCap.Square
				//};

			}
		}
	}
}