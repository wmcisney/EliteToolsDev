using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TractionTools.Utils {
	public class Config {
		public static string GetPuppeteerChromePath() {
			return Config.GetAppSetting("PuppeteerPath", "C:\\Puppeteer\\Win64-674921\\");
		}

		public static string GetAppSetting(string key, string deflt = null) {
			var config = System.Configuration.ConfigurationManager.AppSettings;
			return config[key] ?? deflt;
		}
	}
}
