using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace TractionTools.Utils.Pdf.Generators {
	public class PuppeteerGenerator : IPdfGenerator {

		private static ConcurrentBag<Browser> _browsers = new ConcurrentBag<Browser>();

		private List<string> footerList = new List<string>();

		public PuppeteerGenerator() {
		}
		
		private static async Task<Browser> GetBrowser() {
			Browser item;
			if (_browsers.TryTake(out item))
				return item;

			var puppeteerPath = Config.GetPuppeteerChromePath();
			if (string.IsNullOrEmpty(puppeteerPath))
				throw new ArgumentNullException();

			// check if chromium exists, then download if not
			var chromePath = Path.Combine(puppeteerPath, "chrome-win\\chrome.exe");
			if (!File.Exists(chromePath)) {
				chromePath = await DownloadPuppeteerToPath(puppeteerPath);
			}

			var options = new LaunchOptions {
				Headless = true,
				ExecutablePath = chromePath
			};

			return await Puppeteer.LaunchAsync(options);
		}

		private static void PutBrowser(Browser item) {
			_browsers.Add(item);
		}

		public async Task<Stream> Generate(string htmlSource, bool includeFooters, PdfPageSettings settings) {
			//if (string.IsNullOrEmpty(PuppeteerPath))
			//	throw new ArgumentNullException();

			//// check if chromium exists, then download if not
			//var chromePath = Path.Combine(PuppeteerPath, "chrome-win\\chrome.exe");
			//if (!File.Exists(chromePath)) {
			//	chromePath = await DownloadPuppeteerToPath(PuppeteerPath);
			//}

			//var options = new LaunchOptions {
			//	Headless = true,
			//	ExecutablePath = chromePath
			//};

			//using (var browser = await Puppeteer.LaunchAsync(options)) {
			var browser = await GetBrowser();
			try {
				using (var page = await browser.NewPageAsync()) {

					await page.SetContentAsync(htmlSource);
					var result = await page.GetContentAsync();
					var pdfOptions = GetPdfOptions(includeFooters, settings);
					return await page.PdfStreamAsync(pdfOptions);
				}
			} finally {
				PutBrowser(browser);
			}
			//}
		}


		public IPdfGenerator AddHeader(string text, bool isLeft = true) {
			throw new NotImplementedException();
		}

		public IPdfGenerator AddFooter(string text, bool isLeft = true) {
			if (isLeft) {
				footerList.Add($"<div style=''>{text}</div>");
			} else {
				footerList.Add($"<div style=''>{text}</div>");
			}
			return this;
		}

		private PdfOptions GetPdfOptions(bool includeFooter, PdfPageSettings settings) {
			return new PdfOptions {
				Landscape = settings.Orientation == PdfPageOrientation.Landscape,
				Format = PaperFormat.Letter,
				DisplayHeaderFooter = true,
				FooterTemplate = includeFooter ? GenerateFooter(footerList) : "<div></div>",
				MarginOptions = new MarginOptions() { Bottom = "50px", Top = "5px" }
			};
		}

		private string GenerateFooter(List<string> list) {
			return $"<div style=\"font-size:10px !important; color:#808080; padding-left:10px; text-align:'center'; width: 95%; \"  >{string.Concat(list)}</div>";
		}

		// check if puppeteer exists locally. Download if not
		private static async Task<string> DownloadPuppeteerToPath(string path) {

			var downloadPath = path.Replace($"Win64-{BrowserFetcher.DefaultRevision}\\", "");
			var fetcherOptions = new BrowserFetcherOptions { Path = downloadPath, Platform = Platform.Win64 };

			BrowserFetcher browserFetcher = new BrowserFetcher(fetcherOptions);
			var revs = browserFetcher.LocalRevisions();

			// download if not existing in local
			if (revs == null || !revs.Any())
				await browserFetcher.DownloadAsync(BrowserFetcher.DefaultRevision);

			return browserFetcher.GetExecutablePath(BrowserFetcher.DefaultRevision);
		}
	}
}