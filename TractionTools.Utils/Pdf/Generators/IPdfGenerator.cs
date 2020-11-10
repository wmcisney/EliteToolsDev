using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace TractionTools.Utils.Pdf.Generators {
	public interface IPdfGenerator {

		Task<Stream> Generate(string htmlSource, bool includeFooters, PdfPageSettings settings);


		[Obsolete("Ignored")]
		IPdfGenerator AddHeader(string text, bool isLeft = true);
		IPdfGenerator AddFooter(string text, bool isLeft = true);
	}
}