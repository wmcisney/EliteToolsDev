using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TractionTools.Utils.Pdf {

	public class HeaderFooterSource {
		public HeaderFooterSource(string template, bool isLeft) {
			Template = template;
			IsLeft = isLeft;
		}
		public string Template { get; set; }
		public bool IsLeft { get; set; }
	}

	public class PageAndMeta {
		private PageAndMeta() {}

		public static PageAndMeta CreateFromHtml(string title, string html) {
			return new PageAndMeta {
				Title = title,
				Html = html
			};
		}
		public static PageAndMeta CreateFromStream(string title, Stream stream) {
			return new PageAndMeta {
				Title = title,
				Content = stream
			};
		}

		public static PageAndMeta CreateDisabledPage() {
			return new PageAndMeta {
				Title = "-disabled-",
				Disable = true
			};
		}

		public bool HasStream() {
			return Content != null;
		}

		public Stream GetStream() {
			return Content;
		}

		public string Html { get; private set; }

		public string Title { get; private set; }
		public bool Disable { get; private set; }
		//public bool WasGenerated { get { return Content == null; } }
		private Stream Content { get; set; }

		//private int? pages;
		//public int Pages {
		//	get {
		//		if (pages == null)
		//			pages = PdfEngine.GetPageCount(GeneratedContent);
		//		return pages.Value;
		//	}
		//}
	}

	public class PdfHtmlSource {
		public PdfHtmlSource() { }
		public PdfHtmlSource(PageAndMeta page) {
			HtmlPages = new List<PageAndMeta>(){
				page
			};
		}
		public PdfHtmlSource(string title, string html) {
			HtmlPages = new List<PageAndMeta>(){
				PageAndMeta.CreateFromHtml(title,html)
			};
		}
		public IEnumerable<PageAndMeta> HtmlPages { get; set; }
		public IEnumerable<HeaderFooterSource> Headers { get; set; }
		public IEnumerable<HeaderFooterSource> Footers { get; set; }
		public string FlattenPages() {
			var pageBreak = @"<div class=""print-page-break""></div>";
			return string.Join(pageBreak, HtmlPages.Select(x => x.Html));
		}
	}
}