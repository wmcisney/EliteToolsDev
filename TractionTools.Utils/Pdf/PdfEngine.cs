using PdfSharp.Drawing;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TractionTools.Utils.Pdf.Generators;
using TractionTools.Utils.Pdf.Generators.PdfGeneration.Engine.Generators;

namespace TractionTools.Utils.Pdf {
	public enum PdfGeneratorType {
		puppeteer,
		itext,
		selectPdf

	}
	public class PdfEngine {
		// this can be Injected Programmatically or via the Web.Config

		//public static IPdfGenerator PUPPETEER_GENERATOR = new PuppeteerGenerator(Config.GetPuppeteerChromePath());
		//public static IPdfGenerator SELECT_PDF_GENERATOR = new SelectPdfGenerator();
		//public static IPdfGenerator ITEXT_GENERATOR = new ITextGenerator();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="key">itext - iText
		///select - SelectPdf
		/// puppet - Puppeteer </param>
		/// <returns></returns>
		public static IPdfGenerator GetGenerator(PdfGeneratorType key) {
			switch (key) {
				case PdfGeneratorType.itext:
					return new ITextGenerator();
				case PdfGeneratorType.selectPdf:
					return new SelectPdfGenerator();
				case PdfGeneratorType.puppeteer:
					return new PuppeteerGenerator();
				default:
					throw new NotImplementedException("Generator does not exists");
			}
		}



		private async Task<Stream> GenerateStreamFromRawHtmlAsync(IPdfGenerator generator, string htmlString, bool includeFooters, PdfPageSettings settings) {
			return await generator.Generate(htmlString, includeFooters, settings);
		}


		/// <summary>
		/// Generate htmlSource from htmlSource String
		/// </summary>
		/// <param name="htmlString"></param>
		/// <param name="headers">First String contains Text, second one is if it is left Aligned</param>
		/// <param name="footers">First String contains Text, second one is if it is left Aligned</param>
		/// <param name="generator"></param>
		/// <returns></returns>
		public static async Task<Stream> GenerateFromHtmlString(string htmlString,
										IEnumerable<Tuple<string, bool>> headers = null,
										IEnumerable<Tuple<string, bool>> footers = null,
										IPdfGenerator generator = null,
										PdfPageSettings settings = null) {

			var pdfEngine = new PdfEngine();

			// add headers and footers
			headers?.ToList().ForEach(h => generator.AddHeader(h.Item1, h.Item2));
			footers?.ToList().ForEach(h => generator.AddFooter(h.Item1, h.Item2));

			return await pdfEngine.GenerateStreamFromRawHtmlAsync(generator, htmlString, true, settings);

		}

		public class StreamAndMeta {
			public Stream Content { get; set; }
			public string Name { get; set; }
			public int Pages { get; set; }
			public bool DrawPageNumber { get; set; } = true;
		}

		public static async Task<List<StreamAndMeta>> GeneratePageStreamsFromHtmlString(IPdfGenerator generator, PdfHtmlSource htmlSource, PdfPageSettings settings = null, bool includeFooters = true) {

			var pdfEngine = new PdfEngine();

			// add headers and footers
			if (includeFooters) {
				htmlSource.Headers?.ToList().ForEach(h => generator.AddHeader(h.Template, h.IsLeft));
				htmlSource.Footers?.ToList().ForEach(h => generator.AddFooter(h.Template, h.IsLeft));
			}
			var pages = new List<StreamAndMeta>();

			foreach (var p in htmlSource.HtmlPages) {
				if (p.Disable)
					continue;

				Stream content;
				if (p.HasStream()) {
					content = p.GetStream();
				} else {
					content = await pdfEngine.GenerateStreamFromRawHtmlAsync(generator, p.Html, includeFooters, settings);
				}

				var ps = new StreamAndMeta() {
					Name = p.Title,
					Content = content,
				};

				ps.Pages = PdfEngine.GetPageCount(ps.Content);
				pages.Add(ps);
			}

			return pages;

		}

		public static int GetPageCount(Stream stream) {
			try {
				var mode = PdfDocumentOpenMode.Import;
				var inputDocument = PdfReader.Open(stream, mode);
				return inputDocument.PageCount;
			} catch (Exception) {
				return 0;
			}
		}

		public static Stream Merge(IEnumerable<StreamAndMeta> pdfs) {
			// we only have Pdfsharp as our working merger for now
			var outputDocument = new PdfSharp.Pdf.PdfDocument();
			var curPage = 1;
			foreach (var sam in pdfs) {
				var stream = sam.Content;
				// Attention: must be in Import mode
				var mode = PdfDocumentOpenMode.Import;
				var inputDocument = PdfReader.Open(stream, mode);

				int totalPages = inputDocument.PageCount;
				for (int pageNo = 0; pageNo < totalPages; pageNo++) {
					// Get the page from the input document...
					var page = inputDocument.Pages[pageNo];


					// ...and copy it to the output document.
					var newPage = outputDocument.AddPage(page);

					if (sam.DrawPageNumber) {
						// Get an XGraphics object for drawing
						XGraphics gfx = XGraphics.FromPdfPage(newPage);
						XFont font = new XFont("Verdana", 8, XFontStyle.Regular);
						try {
							var topCorner = 28;
							var leftCorner = 36;
							gfx.DrawString("" + curPage, font, XBrushes.DarkGray, new XRect(newPage.Width - leftCorner, newPage.Height - topCorner, leftCorner, topCorner), XStringFormats.TopLeft);
						} catch (Exception e) {
							throw;
						}
					}

					curPage += 1;


				}
			}

			// Save the document
			var output = new MemoryStream();
			outputDocument.Save(output, false);
			return output;
		}

	}
}