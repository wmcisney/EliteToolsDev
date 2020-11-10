using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SelectPdf;
using TractionTools.Utils.Pdf.Generators;

namespace TractionTools.Utils.Pdf.Generators {
	public class SelectPdfGenerator : IPdfGenerator {

		private readonly HtmlToPdf _converter = new HtmlToPdf();

		private bool IsHtml(string text) {
			// consider html when there are tags inside
			return new Regex(@"<[^>]+>").IsMatch(text);
		}

		private bool IsImage(string text) {
			return new Regex(@"(?i)\.(jpg|png|gif)$").IsMatch(text);
		}

		public IPdfGenerator AddHeader(string text, bool isLeft = true) => AddHeaderFooter(text, isLeft, pdfSection => _converter.Header.Add(pdfSection));
		public IPdfGenerator AddFooter(string text, bool isLeft = true) => AddHeaderFooter(text, isLeft, pdfSection => _converter.Footer.Add(pdfSection));

		private IPdfGenerator AddHeaderFooter(string text, bool isLeft, Action<PdfSectionElement> addElement) {
			if (IsHtml(text)) {
				// HTML Section is always positioned on the left side. The alignment is based on HTML
				var footerHtml = new PdfHtmlSection(10, 0, text, "") {
					AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment,
					AutoFitWidth = HtmlToPdfPageFitMode.NoAdjustment,
					EmbedFonts = true
				};

				addElement(footerHtml);
			} else if (IsImage(text)) {
				var footerHtml = new PdfImageSection(0, 0, 20, text);
				addElement(footerHtml);
			} else {
				// We're using text for Page Number and Total Number of pages special text strings
				// "Page: {page_number} of {total_pages}  "

				PdfTextSection textSection = new PdfTextSection(10, 3, text,
					new System.Drawing.Font("Open Sans", 8)) {
					//new System.Drawing.Font("Arial", 8)) {

					// right now we are only using left and right. But note that there are more options available for this
					HorizontalAlign = isLeft ? PdfTextHorizontalAlign.Left : PdfTextHorizontalAlign.Right
				};
				if (text.Contains("{page_number}") == false)
					textSection.ForeColor = Color.LightGray;

				addElement(textSection);
			}

			return this;
		}


		public byte[] Generate(string htmlSource, string baseUri) {
			var doc = GenerateDocument(htmlSource, null);
			var output = doc.Save();
			doc.Close();

			return output;
		}

		public async Task<Stream> Generate(string htmlSource, bool includeFooter, PdfPageSettings settings) {
			var doc = GenerateDocument(htmlSource, settings);

			var memStream = new MemoryStream();
			doc.Save(memStream);
			return memStream;
		}



		private PdfDocument GenerateDocument(string htmlSource, PdfPageSettings settings) {
			_converter.Options.AutoFitWidth = HtmlToPdfPageFitMode.AutoFit;
			_converter.Options.CssMediaType = HtmlToPdfCssMediaType.Print;
			_converter.Options.MarginTop = 25;
			_converter.Options.EmbedFonts = true;
			_converter.Options.KeepTextsTogether = true;

			// make sure settings is not null
			settings = settings ?? new PdfPageSettings();

			switch (settings.Orientation) {
				case PdfPageOrientation.Portrait:
					_converter.Options.PdfPageOrientation = SelectPdf.PdfPageOrientation.Portrait;
					break;
				case PdfPageOrientation.Landscape:
					_converter.Options.PdfPageOrientation = SelectPdf.PdfPageOrientation.Landscape;
					break;

				default: // default value is landscape
					_converter.Options.PdfPageOrientation = SelectPdf.PdfPageOrientation.Landscape;
					break;
			}


			_converter.Options.DisplayFooter = true;
			_converter.Footer.DisplayOnFirstPage = settings.HasFooterOnFirstPage;

			return _converter.ConvertHtmlString(htmlSource);
		}
	}
}