using log4net;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using PdfSharp.Drawing;
using RadialReview.Accessors;
using RadialReview.App_Start;
using RadialReview.Crosscutting.AttachedPermission;
using RadialReview.Utilities;
using RadialReview.Utilities.Integrations;
using RadialReview.Utilities.NHibernate;
using RadialReview.Utilities.Productivity;
using RadialReview.Utilities.Security;
using RadialReview.Utilities.Serializers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Web.Helpers;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace RadialReview {

	public class MvcApplication : System.Web.HttpApplication {

		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public object AttachedPermissionConfig { get; private set; }

		protected static bool FirstRequest = true;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		protected async void Application_Start() {
			FirstRequest = true;
			//Logger
			try { log4net.GlobalContext.Properties["instanceid"] = AwsMetadataAccessor.GetInstanceId(); } catch (Exception) { }
			try { log4net.GlobalContext.Properties["awsenv"] = Config.GetAwsEnv(); } catch (Exception) { }
			try { log4net.GlobalContext.Properties["EventID"] = 1; } catch (Exception) { }

			ChromeExtensionComms.SendCommand("appStart");
			AntiForgeryConfig.SuppressXFrameOptionsHeader = true;

			AreaRegistration.RegisterAllAreas();

			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			//SwaggerConfig.Register(GlobalConfiguration.Configuration);
			GlobalConfiguration.Configure(WebApiConfig.Register);

			BundleConfig.RegisterBundles(BundleTable.Bundles);

			HookConfig.RegisterHooks();
			PermissionRegistry.RegisterPermission(new IssueAttachedPermissionHandler());
			PermissionRegistry.RegisterPermission(new HeadlineAttachedPermissionHandler());
			PermissionRegistry.RegisterPermission(new MeasurableAttachedPermissionHandler());
			PermissionRegistry.RegisterPermission(new RockAttachedPermissionHandler());
			PermissionRegistry.RegisterPermission(new TodoAttachedPermissionHandler());
		
//#if DEBUG
//			if (Config.IsLocal()) {
//				HibernatingRhinos.Profiler.Appender.NHibernate.NHibernateProfiler.Initialize();
//			}
//#endif

			//Add Angular serializer to SignalR
			var serializerSettings = new JsonSerializerSettings();
			serializerSettings.Converters.Add(new AngularSerialization(true));
			serializerSettings.Converters.Add(new AttachedPermissionSerializer());
			var serializer = JsonSerializer.Create(serializerSettings);
			GlobalHost.DependencyResolver.Register(typeof(JsonSerializer), () => serializer);

			//NHibernate ignore proxy
			JsonConvert.DefaultSettings = () => new JsonSerializerSettings {
				Converters = new List<JsonConverter> { new NHibernateProxyJsonConvert() }
			};

			ApplicationAccessor.EnsureApplicationExists();

			ViewEngines.Engines.Clear();
			IViewEngine razorEngine = new RazorViewEngine() { FileExtensions = new[] { "cshtml" } };
			ViewEngines.Engines.Add(razorEngine);

			ModelBinders.Binders.Add(typeof(DateTime), new DateTimeModelBinder());
			ModelBinders.Binders.Add(typeof(DateTime?), new DateTimeModelBinder());

			//install fonts
			InstallFonts();

			//120 minutes
			GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromSeconds(int.Parse(Config.GetAppSetting("SignalrDisconnectTimeout","7200")));


			log.Info("Application_Start");
			try {
				Slack.SendNotification("Application_Start");
			} catch (Exception e) {
				log.Error(e);
			}

		}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

		protected void Application_BeginRequest() {

			//Special case for CORS SignalR.
			try {
				if (Request.Url.AbsoluteUri.Contains("/negotiate")) {
					string allowedOrigin = null;
					if (CorsUtility.TryGetAllowedOrigin(Request.Url, out allowedOrigin)) {
						HttpContext.Current.Response.AddHeader("Access-Control-Allow-Origin", allowedOrigin);
						HttpContext.Current.Response.AddHeader("Access-Control-Allow-Methods", "*");
						HttpContext.Current.Response.AddHeader("Access-Control-Allow-Headers", "*");
					}
				}
			} catch (Exception) {
				//hmm
			}
		}


		protected void Application_Error(object sender, EventArgs e) {
			Exception ex = Server.GetLastError();

			if (Config.IsLocal()) {
				Slack.SendNotification("Application_Error: " + ex.Message, ex.StackTrace);
			}
			string message = null;
			var shouldLog = true;

			if (ex is HttpException) {
				var ee = (HttpException)ex;
				if (ee.Message.StartsWith("A potentially dangerous Request.Path value was detected from the client (:)")) {
					HttpContext.Current.Server.ClearError();
					HttpContext.Current.Response.Redirect("~/Home/Index");
					shouldLog = false;
				}
				if (ee.Message.StartsWith("A public action method '")) {
					HttpContext.Current.Server.ClearError();
					message = "Page does not exist.";
					shouldLog = false;
				}
			}



			// Create an arbitrary controller instance
			try {
				var view = ViewUtility.RenderView("~/views/Error/Index.cshtml", ex);
				view.ViewData["HasBaseController"] = true;
				view.ViewData["NoAccessCode"] = true;
				if (!string.IsNullOrWhiteSpace(message)) {
					view.ViewData["Message"] = message;
				}
				string errorCode = null;
				if (shouldLog) {
					errorCode = AdminAccessor.LogError(new HttpContextWrapper(HttpContext.Current), ex);
				}
				view.ViewData["ErrorCode"] = errorCode ?? "1";
				view.ViewData["Settings"] = SettingsAccessor.GenerateViewSettings(null, "error", false, false);
				var html = view.Execute();
			
				HttpContext.Current.Server.ClearError();
				var response = HttpContext.Current.Response;
				response.TrySkipIisCustomErrors = true;
				response.ClearContent();
				response.StatusCode = 500;
				response.Write(html);

			} catch (Exception ee) {
				if (Config.IsLocal()) {
					var response = HttpContext.Current.Response;
					response.ClearContent();
					try {
						response.StatusCode = 500;
					} catch (Exception eee) {
					}
					response.Write("<html><body><h1>Error: "+ee.Message+"</h1><h2>"+ee.StackTrace+"</h2></body></html>");
				}
			
			}

			try {
				HttpContext.Current.Server.ClearError();
			} catch (Exception) {
			}

		}

		protected void Application_End() {
			var wasKilled = ChromeExtensionComms.SendCommandAndWait("appEnd").GetAwaiter().GetResult();
			var inte = 0;
			inte += 1;
			Slack.SendNotification("Application_End");
		}


		[DllImport("gdi32.dll", EntryPoint = "AddFontResourceW", SetLastError = true)]
		public static extern int AddFontResource([In][MarshalAs(UnmanagedType.LPWStr)]string lpFileName);
		protected int InstallFonts() {
			if (!Config.IsLocal()) {
				var fonts = new[] { "Arial Narrow Bold.TTF", "Arial Narrow.TTF", "arial.ttf" };
				var installed = 0;
				foreach (var f in fonts) {
					try {
						var result = AddFontResource(@"c:\\Windows\\Fonts\\" + f);
						var error = Marshal.GetLastWin32Error();
						installed = installed + (error == 0 ? 1 : 0);
					} catch (Exception) {
					}
				}

				var assembly = Assembly.GetExecutingAssembly();

				foreach (var resourceName in assembly.GetManifestResourceNames()) {
					try {
						if (resourceName.ToLower().EndsWith(".ttf")) {
							using (var resourceStream = assembly.GetManifestResourceStream(resourceName)) {
								XPrivateFontCollection.Add(resourceStream);//, resourceName.Substring(0, resourceName.Length - 4).Split('.').Last());
							}
						}
					} catch (Exception e) {
						throw e;
					}
				}

				return installed;
			}
			return 0;
		}


		private static object lck = new object();

		void Application_EndRequest(Object Sender, EventArgs e) {
			if (FirstRequest && Config.IsLocal()) {
				Slack.SendNotification("Application Started.");
			}
			FirstRequest = false;

			if ("POST" == Request.HttpMethod) {
				try {
					Request.InputStream.Seek(0, SeekOrigin.Begin);
					var bytes = Request.BinaryRead(Request.TotalBytes);
					var s = Encoding.UTF8.GetString(bytes);
					if (!String.IsNullOrEmpty(s) && !s.ToLower().Contains("password")) {
						var QueryStringLength = 0;
						if (0 < Request.QueryString.Count) {
							QueryStringLength = Request.ServerVariables["QUERY_STRING"].Length;
							Response.AppendToLog("&");
						}

						if (4100 > (QueryStringLength + s.Length)) {
							Response.AppendToLog(s);
						} else {
							// append only the first 4090 the limit is a total of 4100 char.
							Response.AppendToLog(s.Substring(0, (4090 - QueryStringLength)));
							// indicate buffer exceeded
							Response.AppendToLog("|||...|||");
							// TODO: if s.Length >; 4000 then log to separate file
						}
					}
					Request.InputStream.Seek(0, SeekOrigin.Begin);
				} catch (Exception) {
					Response.AppendToLog("~Error~");
				}
			}
		}

	}
}
