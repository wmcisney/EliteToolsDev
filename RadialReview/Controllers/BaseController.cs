using log4net;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Security;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using Newtonsoft.Json;
using NHibernate;
using PdfSharp.Pdf;
using RadialReview.Accessors;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Engines;
using RadialReview.Exceptions;
using RadialReview.Exceptions.MeetingExceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Application;
using RadialReview.Models.ClientSuccess;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.Notifications;
using RadialReview.Models.UserModels;
using RadialReview.NHibernate;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.Files;
using RadialReview.Utilities.Productivity;
using RadialReview.Utilities.Serializers;
using RadialReview.Variables;
using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using static RadialReview.Models.UserOrganizationModel;

namespace RadialReview.Controllers {
	public class UserManagementController : BaseController {

		public UserManagementController() : this(new NHibernateUserManager(new NHibernateUserStore())) //this(new UserManager<ApplicationUser>(new NHibernateUserStore<UserModel>(new ApplicationDbContext())))
		{
		}

		protected UserManagementController(NHibernateUserManager userManager) {
			UserManager = userManager;
		}

		protected async Task SignInAsync(UserModel user, bool isPersistent = false, TimeSpan? expiration = null) {
			AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);
			var identity = await UserManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
			var settings = new AuthenticationProperties() { IsPersistent = isPersistent };
			if (expiration != null) {
				settings.ExpiresUtc = DateTime.UtcNow.Add(expiration.Value);
				settings.AllowRefresh = false;
			}
			AuthenticationManager.SignIn(settings, identity);
			await NotificationAccessor.AutoGenerateNotificationsUnsafe(user.CurrentRole);

		}

		public NHibernateUserManager UserManager { get; protected set; }
	}

	public class BaseController : Controller {
		#region Helpers
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		#region ViewBag

		protected void ShowAlert(string status, AlertType type = AlertType.Info) {
			switch (type) {
				case AlertType.Info:
					ViewBag.InfoAlert = status;
					break;
				case AlertType.Error:
					ViewBag.Alert = status;
					break;
				case AlertType.Success:
					ViewBag.Success = status;
					break;
				default:
					throw new ArgumentOutOfRangeException("AlertType:" + type);
			}
		}
		#endregion
		#region Engines
		protected static UserEngine _UserEngine = new UserEngine();
		protected static ChartsEngine _ChartsEngine = new ChartsEngine();
		protected static ReviewEngine _ReviewEngine = new ReviewEngine();
		#endregion
		#region Accessors
		protected static RockAccessor _RockAccessor = new RockAccessor();
		protected static RoleAccessor _RoleAccessor = new RoleAccessor();
		protected static UserAccessor _UserAccessor = new UserAccessor();
		protected static TaskAccessor _TaskAccessor = new TaskAccessor();
		protected static TeamAccessor _TeamAccessor = new TeamAccessor();
		protected static NexusAccessor _NexusAccessor = new NexusAccessor();
		protected static ImageAccessor _ImageAccessor = new ImageAccessor();
		protected static GroupAccessor _GroupAccessor = new GroupAccessor();
		protected static OriginAccessor _OriginAccessor = new OriginAccessor();
		protected static ReviewAccessor _ReviewAccessor = new ReviewAccessor();
		protected static AskableAccessor _AskableAccessor = new AskableAccessor();
		protected static PaymentAccessor _PaymentAccessor = new PaymentAccessor();
		protected static KeyValueAccessor _KeyValueAccessor = new KeyValueAccessor();
		protected static PositionAccessor _PositionAccessor = new PositionAccessor();
		protected static QuestionAccessor _QuestionAccessor = new QuestionAccessor();
		protected static CategoryAccessor _CategoryAccessor = new CategoryAccessor();
		protected static PrereviewAccessor _PrereviewAccessor = new PrereviewAccessor();
		protected static ScorecardAccessor _ScorecardAccessor = new ScorecardAccessor();
		protected static PermissionsAccessor _PermissionsAccessor = new PermissionsAccessor();
		protected static OrganizationAccessor _OrganizationAccessor = new OrganizationAccessor();
		//protected static DeepSubordianteAccessor _DeepSubordianteAccessor = new DeepSubordianteAccessor();
		protected static ResponsibilitiesAccessor _ResponsibilitiesAccessor = new ResponsibilitiesAccessor();
		#endregion
		#region GetUserModel
		protected UserModel GetUserModel(bool styles = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return GetUserModel(s, styles);
				}
			}
		}

		protected UserModel GetUserModel(ISession s, bool styles = false) {
			return new Cache().GetOrGenerate(CacheKeys.USER + "~" + styles, x => {
				x.LifeTime = LifeTime.Request/*Session*/;
				var id = User.Identity.GetUserId();

				var user = _UserAccessor.GetUserById(s, id);
				if (styles) {
					user._StylesSettings = s.Get<UserStyleSettings>(id);
				}

				return user;

			});
		}
		#endregion
		#region GetUser
		//private long? _CurrentUserOrganizationId = null;

		private UserOrganizationModel MockUser = null;
		private bool TransformAngular = true;

		protected string[] AllAdmins = new[] { "" };

		protected ServerSettings<int> SoftwareVersion = ServerSettings.Create(s => {
			return s.GetSettingOrDefault(Variable.Names.SOFTWARE_VERSION, () => 1);
		}, TimeSpan.FromMinutes(2));


		private UserOrganizationModel PopulateUserData(UserOrganizationModel user, PermissionsOverrides overrides = null) {


			if (user != null && Request != null) {
				if (overrides != null) {
					user._PermissionsOverrides = overrides;
				}

				user._IsRadialAdmin = user.IsRadialAdmin;

				user._ClientTimestamp = Request.Params.Get("_clientTimestamp").TryParseLong();
				user._ClientOffset = Request.Params.Get("_tz").TryParseInt();
				user._ClientRequestId = Request.Params.Get("_rid");
				user._ConnectionId = Request.Params.Get("connectionId");

				if (user._ClientTimestamp != null && user._ClientOffset == null) {
					var diff = (int)(Math.Round((user._ClientTimestamp.Value.ToDateTime() - DateTime.UtcNow).TotalMinutes / 30.0) * 30.0);
					user._ClientOffset = diff;// Thread.SetData(Thread.GetNamedDataSlot("timeOffset"), diff);
				}

				HookData.SetData("ConnectionId", Request.Params.Get("connectionId"));
				HookData.SetData("ClientTimestamp", user._ClientTimestamp);
				HookData.SetData("ClientTimezone", user._ClientOffset);
				HookData.SetData("ClientRequestId", user._ClientRequestId);

			}
			return user;
		}
		public UserOrganizationModel GetUser() {
			return PopulateUserData(_GetUser());
		}
		public UserOrganizationModel GetUser(ISession s, PermissionsOverrides overrides) {
			return PopulateUserData(_GetUser(s), overrides);
		}
		public UserOrganizationModel GetUser(long userOrganizationId) {
			return PopulateUserData(_GetUser(userOrganizationId));
		}
		private UserOrganizationModel GetUserOrganization(ISession s, long userOrganizationId, String redirectUrl)//, Boolean full = false)
		{
			var cache = new Cache();

			return cache.GetOrGenerate(CacheKeys.USERORGANIZATION, x => {
				var id = User.Identity.GetUserId();
				var found = _UserAccessor.GetUserOrganizations(s, id, userOrganizationId, redirectUrl);
				if (found != null && found.User != null && !cache.Contains(CacheKeys.USER)) {
					cache.Push(CacheKeys.USER, found.User, LifeTime.Request/*Session*/);
				}
				x.LifeTime = LifeTime.Request/*Session*/;
				return found;
			}, x => x.Id != userOrganizationId);
		}

		// ReSharper disable once RedundantOverload.Local
		private UserOrganizationModel _GetUser(ISession s) {
			if (MockUser != null) {
				return MockUser;
			}

			return _GetUser(s, null);
		}
		private UserOrganizationModel _GetUser() {
			if (MockUser != null) {
				return MockUser;
			}

			long? userOrganizationId = null;

			if (userOrganizationId == null) {
				var orgIdParam = Request.Params.Get("setUserOrganizationId");
				if (orgIdParam != null) {
					userOrganizationId = long.Parse(orgIdParam);
				}
			}

			var cache = new Cache();

			if (userOrganizationId == null && cache.Get(CacheKeys.USERORGANIZATION_ID) is long) {
				userOrganizationId = (long)cache.Get(CacheKeys.USERORGANIZATION_ID);
			}



			if (cache.Get(CacheKeys.USERORGANIZATION) is UserOrganizationModel && userOrganizationId == ((UserOrganizationModel)cache.Get(CacheKeys.USERORGANIZATION)).Id) {
				return (UserOrganizationModel)cache.Get(CacheKeys.USERORGANIZATION);
			}

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return _GetUser(s, null);
				}
			}
		}
		private UserOrganizationModel _GetUser(long userOrganizationId) {
			if (MockUser != null && MockUser.Id == userOrganizationId) {
				return MockUser;
			}

			var cache = new Cache();

			if (cache.Get(CacheKeys.USERORGANIZATION) is UserOrganizationModel && userOrganizationId == ((UserOrganizationModel)cache.Get(CacheKeys.USERORGANIZATION)).Id) {
				return (UserOrganizationModel)cache.Get(CacheKeys.USERORGANIZATION);
			}

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return _GetUser(s, userOrganizationId);
				}
			}
		}
		private UserOrganizationModel _GetUser(ISession s, long? userOrganizationId = null)//long? organizationId, Boolean full = false)
		{

			/**/
			if (userOrganizationId == null) {
				try {
					var orgIdParam = Request.Params.Get("setUserOrganizationId");
					if (orgIdParam != null) {
						userOrganizationId = long.Parse(orgIdParam);
					}
				} catch (Exception) {
					//var o = false;
				}
			}
			var cache = new Cache();

			if (userOrganizationId == null && cache.Get(CacheKeys.USERORGANIZATION_ID) != null) {
				userOrganizationId = (long)cache.Get(CacheKeys.USERORGANIZATION_ID);
			}
			if (userOrganizationId == null) {
				userOrganizationId = GetUserModel(s).GetCurrentRole();
			}


			var user = cache.Get(CacheKeys.USERORGANIZATION);

			if (user is UserOrganizationModel && userOrganizationId == ((UserOrganizationModel)user).Id) {
				return (UserOrganizationModel)user;
			}

			if (userOrganizationId == null) {
				var returnPath = Server.HtmlEncode(Request.Path);

				var found = GetUserOrganizations(s, returnPath);
				if (found.Count() == 0) {
					throw new NoUserOrganizationException();
				} else if (found.Count() == 1) {
					var uo = found.First();
					if (uo.User != null) {
						uo.User.CurrentRole = uo.Id;
						s.Update(uo.User);
					}
					//_CurrentUserOrganizationId = uo.Id;
					cache.Push(CacheKeys.USERORGANIZATION, uo, LifeTime.Request/*Session*/);
					cache.Push(CacheKeys.USERORGANIZATION_ID, uo.Id, LifeTime.Request/*Session*/);
					return uo;
				} else {
					throw new OrganizationIdException(Request.Url.PathAndQuery);
				}
			} else {
				var uo = GetUserOrganization(s, userOrganizationId.Value, Request.Url.PathAndQuery);
				//_CurrentUserOrganizationId = uo.Id;
				cache.Push(CacheKeys.USERORGANIZATION, uo, LifeTime.Request/*Session*/);
				cache.Push(CacheKeys.USERORGANIZATION_ID, userOrganizationId.Value, LifeTime.Request/*Session*/);
				return uo;

			}
		}

		protected void AllowAdminsWithoutAudit() {
			try {
				var user = GetUser();
				if (user != null && user._PermissionsOverrides != null) {
					user._PermissionsOverrides.Admin.AllowAdminWithoutAudit = true;
				}
			} catch (Exception) {
			}
		}

		protected List<UserOrganizationModel> GetUserOrganizations(String redirectUrl) //Boolean full = false)
		{
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return GetUserOrganizations(s, redirectUrl);
				}
			}
		}
		protected int GetUserOrganizationCounts(ISession s, String redirectUrl)//Boolean full = false)
		{
			var id = User.Identity.GetUserId();
			return _UserAccessor.GetUserOrganizationCounts(s, id, redirectUrl/*, full*/);
		}
		protected List<UserOrganizationModel> GetUserOrganizations(ISession s, String redirectUrl)//Boolean full = false)
		{
			var id = User.Identity.GetUserId();
			return _UserAccessor.GetUserOrganizations(s, id, redirectUrl/*, full*/);
		}
		#endregion
		#region Validation
		private List<String> ToValidate = new List<string>();
		private NameValueCollection ValidationCollection;
		private bool SkipValidation = false;
		protected void ValidateValues<T>(T model, params Expression<Func<T, object>>[] selectors) {
			if (SkipValidation) {
				return;
			}

			foreach (var e in selectors) {
				//var meta = ModelMetadata.FromLambdaExpression(e, new ViewDataDictionary<T>());
				var name = e.GetMvcName();//meta.DisplayName;
				if (!ToValidate.Remove(name)) {
					throw new PermissionsException("Validation item does not exist.");
				}

				SecuredValueValidator.ValidateValue(ValidationCollection, name);
			}
		}
		#endregion

		protected string ReadBody() {
			Request.InputStream.Position = 0;
			var body = new StreamReader(Request.InputStream).ReadToEnd();
			return body;
		}

		protected void ManagerAndCanEditOrException(UserOrganizationModel user) {
			if (!user.IsManagerCanEditOrganization()) {
				throw new PermissionsException();
			}
		}
		protected ActionResult RedirectToLocal(string returnUrl) {
			if (Url.IsLocalUrl(returnUrl)) {
				return Redirect(returnUrl);
			} else {
				throw new RedirectException("Return URL is invalid.");
			}
		}

		private static string CleanFileName(string fileName) {
			return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
		}
		protected ActionResult Xls(SLDocument document, string name = null) {

			name = name ?? ("export_" + DateTime.UtcNow.ToJavascriptMilliseconds());
			//name = name;

			Response.Clear();
			Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
			Response.AddHeader("Content-Disposition", "attachment; filename=" + name + ".xlsx");
			document.SaveAs(Response.OutputStream);
			Response.End();
			return new EmptyResult();
		}

		protected ActionResult Download(byte[] contents, string fileName, string fileType) {
			MemoryStream stream = new MemoryStream(contents);
			Response.Clear();
			Response.ContentType = MimeTypeMap.GetMimeType(fileType);
			var name = fileName;
			if (!name.ToLower().EndsWith(fileType.ToLower())) {
				if (!fileType.StartsWith(".")) {
					name = name + ".";
				}
				name = name + fileType;
			}

			Response.AddHeader("Content-Disposition", "filename=\"" + name + "\"");

			Response.AddHeader("content-length", stream.Length.ToString());
			Response.BinaryWrite(stream.ToArray());
			stream.Close();
			return new EmptyResult();
		}

		protected ActionResult Pdf(PdfDocument document, string name = null, bool inline = true) {
			name = name ?? document.Info.Title;

			MemoryStream stream = new MemoryStream();
			document.Save(stream, false);
			Response.Clear();
			Response.ContentType = "application/pdf";
			if (name != null) {
				if (!name.ToLower().EndsWith(".pdf")) {
					name += ".pdf";
				}

				name = CleanFileName(name);
				Response.AddHeader("Content-Disposition", "filename=\"" + name + "\"");
			}
			Response.AddHeader("content-length", stream.Length.ToString());
			Response.BinaryWrite(stream.ToArray());
			stream.Close();
			return new EmptyResult();
		}
		private static string MappedAppPath() {
			string APP_PATH = System.Web.HttpContext.Current.Request.ApplicationPath.ToLower();
			if (APP_PATH == "/") //a site 
{
				APP_PATH = "/";
			} else if (!APP_PATH.EndsWith(@"/")) //a virtual 
{
				APP_PATH += @"/";
			}

			string it = System.Web.HttpContext.Current.Server.MapPath(APP_PATH);
			if (!it.EndsWith(@"\")) {
				it += @"\";
			}

			return it;
		}

		protected FileResult Pdf(Document document, string name = null, bool inline = true) {
#pragma warning disable CS0618 // Type or member is obsolete
			var pdfRenderer = new PdfDocumentRenderer(true, PdfFontEmbedding.None);
#pragma warning restore CS0618 // Type or member is obsolete

			pdfRenderer.Document = document;

			pdfRenderer.RenderDocument();
			if (pdfRenderer.PageCount == 0) {
				var doc = new Document();
				var s = doc.AddSection();
				s.AddParagraph("No pages.");
				return Pdf(doc, name, inline);
			}


			var stream = new MemoryStream();

			pdfRenderer.Save(stream, false);
			name = name ?? (Guid.NewGuid() + ".pdf");
			if (inline) {
				//Response.Headers.Remove("Content-Disposition");
				//var f = Response.Filter;
				//Response.Filter = null;
				//if (Response.Headers.AllKeys.Any(x => x == "Content-Encoding"))
				//    Response.Headers.Remove("Content-Encoding");
				//Response.AppendHeader("Content-Disposition", "inline; filename=output.pdf");
				//Response.AppendHeader("Cache-Control", "private");

			}
			return new FileStreamResult(stream, System.Net.Mime.MediaTypeNames.Application.Pdf);
			//return file;
		}


		#endregion
		#region User Status
		protected void SignOut() {
			try {
				var uid = GetUser().Id;
				var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
				hub.Clients.Group(RealTimeHub.Keys.UserId(uid)).logoff();
			} catch (Exception) {
			}

			new Cache().Invalidate(CacheKeys.USERORGANIZATION_ID);
			AuthenticationManager.SignOut();
			FormsAuthentication.SignOut();
			HttpContext.User = new GenericPrincipal(new GenericIdentity(string.Empty), null);
			Session.Clear();




		}
		protected bool IsLoggedIn() {
			if (User == null || User.Identity == null) {
				return false;
			}

			return User.Identity.GetUserId() != null;
		}
		public IAuthenticationManager AuthenticationManager {
			get {
				return HttpContext.GetOwinContext().Authentication;
			}
		}
		#endregion
		#region Overrides
		private static MethodInfo GetActionMethod(ControllerContext filterContext) {
			Type controllerType = filterContext.Controller.GetType();// Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(x => x.Name == requestContext.RouteData.Values["controller"].ToString());
			ControllerContext controllerContext = new ControllerContext(filterContext.RequestContext, Activator.CreateInstance(controllerType) as ControllerBase);
			ControllerDescriptor controllerDescriptor = new ReflectedControllerDescriptor(controllerType);
			ActionDescriptor actionDescriptor = controllerDescriptor.FindAction(controllerContext, controllerContext.RouteData.Values["action"].ToString());
			return (actionDescriptor as ReflectedActionDescriptor).MethodInfo;
		}

		private ActionResult RedirectToActionUnlessChild(ControllerContext context, string actionName, string controllerName, object routeValues) {
			if (context.IsChildAction) {
				log.Info($"Skipping render of {controllerName}/{actionName} within child.");
				return Content("");// $@"<span data-info='Skipping render of {controllerName}/{actionName}. within child'></span>");
			}
			return RedirectToAction(actionName, controllerName, routeValues);
		}

		protected override void OnException(ExceptionContext filterContext) {
			bool shouldLog = false;
			Uri url = null;
			Uri urlReferrer = null;

			if (AdminAccessor.InReadOnly()) {
				filterContext.ExceptionHandled = true;
				filterContext.HttpContext.Response.Clear();
				ViewBag.Message = filterContext.Exception.Message;
				Exception err = null;
				if (filterContext.Exception is PermissionsException) {
					err = filterContext.Exception;
				}

				filterContext.Result = PartialView("~/Views/ReadOnly/Error.cshtml", err);
				return;
			}

			try {
				try {
					url = Request.Url;
					urlReferrer = Request.UrlReferrer;
				} catch (Exception) {
				}
				ChromeExtensionComms.SendCommand("pageError");
				var f = filterContext.HttpContext.Response.Filter;
				try {
					filterContext.HttpContext.Response.Filter = null;
				} catch (Exception) {
					//eat it.
				}
				if (filterContext.HttpContext.Response.Headers.AllKeys.Any(x => x == "Content-Encoding")) {
					filterContext.HttpContext.Response.Headers.Remove("Content-Encoding");
				}

				var action = GetActionMethod(filterContext);

				var isJsonResult = typeof(JsonResult).IsAssignableFrom(action.ReturnType);
				isJsonResult = isJsonResult || (typeof(Task<JsonResult>)).IsAssignableFrom(action.ReturnType);

				if (isJsonResult) {
					var exception = new ResultObject(filterContext.Exception);
					HttpStatusCode? statusOverride = null;
					if (filterContext.Exception is RedirectException) {
						var re = ((RedirectException)filterContext.Exception);
						if (re.Silent != null) {
							exception.Silent = re.Silent.Value;
						}

						exception.NoErrorReport = re.NoErrorReport;

						if (re.ForceReload) {
							exception.Refresh = true;
						}

						if (re.StatusCodeOverride != null) {
							statusOverride = re.StatusCodeOverride.Value;
						}
					}

					filterContext.ExceptionHandled = true;
					filterContext.HttpContext.Response.Clear();
					filterContext.HttpContext.Response.StatusCode = (int)(statusOverride ?? System.Net.HttpStatusCode.InternalServerError);
					filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
					filterContext.Result = new JsonResult() { Data = exception, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
					return;
				}

				if (filterContext.ExceptionHandled) {
					return;
				}
				//ViewBag.Settings = PopulateSettings( null, "error", false, false);


				if (filterContext.Exception is LoginException) {
					SignOut();
					var redirectUrl = ((RedirectException)filterContext.Exception).RedirectUrl;
					if (redirectUrl == null) {
						redirectUrl = Request.Url.PathAndQuery;
					}

					log.Info("Login: [" + Request.Url.PathAndQuery + "] --> [" + redirectUrl + "]");
					filterContext.Result = RedirectToAction("Login", "Account", new { area = "", message = filterContext.Exception.Message, returnUrl = redirectUrl });
					filterContext.ExceptionHandled = true;
					filterContext.HttpContext.Response.Clear();
				} else if (filterContext.Exception is OrganizationIdException) {
					var redirectUrl = ((RedirectException)filterContext.Exception).RedirectUrl;
					log.Info("Organization: [" + Request.Url.PathAndQuery + "] --> [" + redirectUrl + "]");
					filterContext.Result = RedirectToAction("Role", "Account", new { area = "", message = filterContext.Exception.Message, returnUrl = redirectUrl });
					filterContext.ExceptionHandled = true;
					filterContext.HttpContext.Response.Clear();
				} else if (filterContext.Exception is PermissionsException) {
					filterContext.HttpContext.Response.Clear();
					var returnUrl = ((RedirectException)filterContext.Exception).RedirectUrl;
					log.Info("Permissions: [" + Request.Url.PathAndQuery + "] --> [" + returnUrl + "]");
					ViewBag.Message = filterContext.Exception.Message;
					if (typeof(PartialViewResult).IsAssignableFrom(action.ReturnType)) {
						filterContext.Result = PartialView("~/Views/Error/Index.cshtml", filterContext.Exception);
					} else {
						filterContext.Result = View("~/Views/Error/Index.cshtml", filterContext.Exception);
					}
					shouldLog = true;
					filterContext.ExceptionHandled = true;
				} else if (filterContext.Exception is MeetingException) {
					var type = ((MeetingException)filterContext.Exception).MeetingExceptionType;
					log.Info("MeetingException: [" + Request.Url.PathAndQuery + "] --> [" + type + "]");
					filterContext.Result = RedirectToAction("ErrorMessage", "L10", new { area = "", message = filterContext.Exception.Message, type });
					filterContext.ExceptionHandled = true;
					filterContext.HttpContext.Response.Clear();
				} else if (filterContext.Exception is AdminSetRoleException) {
					HandleAdminSetRoleException(filterContext);
				} else if (filterContext.Exception is RedirectToActionException) {
					var ex = (RedirectToActionException)filterContext.Exception;
					filterContext.Result = RedirectToAction(ex.Action, ex.Controller);
					log.Info("RedirectToAction: [" + Request.Url.PathAndQuery + "] --> [" + ex.Controller + ": " + ex.Action + "]");
					//filterContext.Result = RedirectToAction("Index", "Error", new { area = "", message = filterContext.Exception.Message, returnUrl = returnUrl });
					filterContext.ExceptionHandled = true;
					filterContext.HttpContext.Response.Clear();
				} else if (filterContext.Exception is RedirectException) {
					var returnUrl = ((RedirectException)filterContext.Exception).RedirectUrl;
					log.Info("Redirect: [" + Request.Url.PathAndQuery + "] --> [" + returnUrl + "]");
					filterContext.Result = RedirectToAction("Index", "Error", new { area = "", message = filterContext.Exception.Message, returnUrl = returnUrl });
					filterContext.ExceptionHandled = true;
					filterContext.HttpContext.Response.Clear();
				} else if (filterContext.Exception is HttpAntiForgeryException) {
					log.Info("AntiForgery: [" + Request.Url.PathAndQuery + "] --> []");
					filterContext.Result = RedirectToAction("Login", "Account", new { area = "", message = "Session Timeout. Please try again." /*filterContext.Exception.Message*/ });
					filterContext.ExceptionHandled = true;
					filterContext.HttpContext.Response.Clear();
				} else {
					log.Error("Error: [" + Request.Url.PathAndQuery + "]<<" + filterContext.Exception.Message + ">>", filterContext.Exception);
					base.OnException(filterContext);
					/*ViewBag may not work. Instead see view.ViewData in Application_error of global.asax.cs*/
					ViewBag.Settings = SettingsAccessor.GenerateViewSettings(null, "error", false, false);
				}
			} catch (Exception e) {
				log.Error("OnException(Exception)", e);
				filterContext.Result = Content(e.Message + "  " + e.StackTrace);
			}

			string errorCode = null;
			if (shouldLog) {
				try {
					var userId = filterContext.HttpContext.User.Identity.Name;
					errorCode = AdminAccessor.LogError(userId, Request.HttpMethod, url, urlReferrer, filterContext.Exception);
				} catch (Exception) {
				}
			}
			ViewBag.ErrorCode = errorCode ?? "1";


		}

		private void HandleAdminSetRoleException(ExceptionContext filterContext) {
			var ex = (AdminSetRoleException)filterContext.Exception;
			var redirectUrl = ex.RedirectUrl;
			log.Info("AdminSetRoleException: [" + Request.Url.PathAndQuery + "] --> [" + redirectUrl + "]");
			if (ex.AccessLevel == Models.Admin.AdminAccessLevel.View) {
				filterContext.Result = RedirectToActionUnlessChild(filterContext, "AdminSetRole", "Account", new { area = "", message = filterContext.Exception.Message, returnUrl = redirectUrl, Id = ex.RequestedRoleId });

			}

			if (ex.AccessLevel == Models.Admin.AdminAccessLevel.SetAs) {
				filterContext.Result = RedirectToActionUnlessChild(filterContext, "SetAsUser", "Account", new { area = "", message = filterContext.Exception.Message, returnUrl = redirectUrl, Id = ex.RequestedEmail });
			}

			filterContext.ExceptionHandled = true;
			if (!filterContext.IsChildAction) {
				filterContext.HttpContext.Response.Clear();
			}
		}

		protected void CompressContent(ActionExecutedContext filterContext) {
			var encodingsAccepted = filterContext.HttpContext.Request.Headers["Accept-Encoding"];
			var contentType = filterContext.HttpContext.Request.Headers["Content-Type"];
			if (filterContext.IsChildAction) {
				return;
			}

			if (string.IsNullOrEmpty(encodingsAccepted)) {
				return;
			}

			if (contentType != null && contentType.ToLower().Contains("pdf")) {
				return;
			}

			if (contentType != null && contentType.ToLower() == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet") {
				return;
			}

			try {
				encodingsAccepted = encodingsAccepted.ToLowerInvariant();
				var response = filterContext.HttpContext.Response;
				if (encodingsAccepted.Contains("deflate")) {
					Response.Headers.Remove("Content-Encoding");
					response.AppendHeader("Content-Encoding", "deflate");
					response.Filter = new DeflateStream(response.Filter, CompressionMode.Compress);
				} else if (encodingsAccepted.Contains("gzip")) {
					Response.Headers.Remove("Content-Encoding");
					response.AppendHeader("Content-Encoding", "gzip");
					response.Filter = new GZipStream(response.Filter, CompressionMode.Compress);
				}
			} catch (Exception) {
				//I guess just eat it..
			}
		}

		protected override void OnActionExecuting(ActionExecutingContext filterContext) {
			try {
				var accessAttributes = filterContext.ActionDescriptor.GetCustomAttributes(typeof(AccessAttribute), false).Cast<AccessAttribute>();
				if (accessAttributes.Count() == 0) {
					throw new NotImplementedException("Access attribute missing.");
				}

				var accessLevel = (AccessLevel)accessAttributes.Max(x => (int)x.AccessLevel);

				var readonlyMode = false;
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {

						var prefetchData = Prefetch(s);

						readonlyMode = AdminAccessor.InReadOnly(s);

						//Secure hidden fields
						ValidationCollection = filterContext.RequestContext.HttpContext.Request.Form;
						foreach (var f in ValidationCollection.AllKeys) {
							if (f != null && f.EndsWith(SecuredValueFieldNameComputer.NameSuffix)) {
								ToValidate.Add(f.Substring(0, f.Length - SecuredValueFieldNameComputer.NameSuffix.Length));
							}
						}
						//Access Level Filtering

						var ignorePaymentLockout = accessAttributes.Any(x => x.IgnorePaymentLockout);
						var permissionsOverrides = new PermissionsOverrides() {
							IgnorePaymentLockout = ignorePaymentLockout,
						};

						var adminShortCircuit = false;
						switch (accessLevel) {
							case AccessLevel.SignedOut: {
									if (Request.IsAuthenticated) {
										SignOut();
									}
								}
								break;
							case AccessLevel.Any:
								break;
							case AccessLevel.User:
								GetUserModel(s);
								break;
							case AccessLevel.UserOrganization:
								var u1 = GetUser(s, permissionsOverrides);
								LockoutUtility.ProcessLockout(u1);
								break;
							case AccessLevel.Manager:
								var u2 = GetUser(s, permissionsOverrides);
								LockoutUtility.ProcessLockout(u2);
								if (!u2.IsManager()) {
									throw new PermissionsException("You must be a " + Config.ManagerName() + " to view this resource.");
								}

								break;
							case AccessLevel.Radial:
								if (!(GetUserModel(s).IsRadialAdmin || GetUser(s, permissionsOverrides).IsRadialAdmin)) {
									throw new PermissionsException("You do not have access to this resource.");
								}

								adminShortCircuit = true;
								break;
							case AccessLevel.RadialData:
								var ids = s.GetSettingOrDefault(Variable.Names.USER_RADIAL_DATA_IDS, () => "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => long.Parse(x)).ToList();
								var u3 = GetUser(s, permissionsOverrides);
								if (u3 != null && ids.Contains(u3.Id)) {
									LockoutUtility.ProcessLockout(u3);
									break;
								}
								if (!(GetUserModel(s).IsRadialAdmin || GetUser(s, permissionsOverrides).IsRadialAdmin)) {
									throw new PermissionsException("You do not have access to this resource.");
								}

								adminShortCircuit = true;
								break;
							default:
								throw new Exception("Unknown Access Type");
						}

						// eat the cookie (if any) and set the culture
						//if (Request.Cookies["lang"] != null) {
						//    HttpCookie cookie = Request.Cookies["lang"];
						//    string lang = cookie.Value;
						//    var culture = new System.Globalization.CultureInfo(lang);
						//    Thread.CurrentThread.CurrentCulture = culture;
						//    Thread.CurrentThread.CurrentUICulture = culture;
						//}

						var viewBag = filterContext.Controller.ViewBag;

						viewBag.IsLocal = Config.IsLocal();
						filterContext.Controller.ViewBag.HasBaseController = true;
						filterContext.Controller.ViewBag.AppVersion = GetAppVersion();
						filterContext.Controller.ViewBag.IsRadialAdmin = false;
						filterContext.Controller.ViewBag.Settings = SettingsAccessor.GenerateViewSettings(null, "", false, false);
						//filterContext.Controller.ViewBag.WorkspaceDropdown = new WorkspaceDropdownVM();

						bool isRadialAdmin = false;
						if (IsLoggedIn()) {
							var userOrgsCount = GetUserOrganizationCounts(s, Request.Url.PathAndQuery);

							UserOrganizationModel oneUser = null;
							try {
								oneUser = GetUser(s, permissionsOverrides);
								if (oneUser == null) {
									throw new NoUserOrganizationException();
								}

#pragma warning disable CS0618 // Type or member is obsolete
								var lu = s.Get<UserLookup>(oneUser.Cache.Id);
#pragma warning restore CS0618 // Type or member is obsolete
								var actualUserModel = GetUserModel(s);
								isRadialAdmin = oneUser.IsRadialAdmin || actualUserModel.IsRadialAdmin;

								SetupPermissionsOverride(oneUser, actualUserModel, isRadialAdmin, adminShortCircuit);

								oneUser._IsRadialAdmin = isRadialAdmin;
								filterContext.Controller.ViewBag.IsRadialAdmin = isRadialAdmin;

								if (!isRadialAdmin) {
									lu.LastLogin = DateTime.UtcNow;
									s.Update(lu);

									var ol = s.QueryOver<OrganizationLookup>().Where(x => x.OrgId == oneUser.Organization.Id).Take(1).SingleOrDefault();
									if (ol == null) {
										ol = new OrganizationLookup() {
											OrgId = oneUser.Organization.Id,
											CreateTime = oneUser.Organization.CreationTime
										};
									}
									ol.LastUserLogin = oneUser.Id;
									ol.LastUserLoginTime = lu.LastLogin.Value;
									s.SaveOrUpdate(ol);


								}
							} catch (OrganizationIdException) {
							} catch (NoUserOrganizationException) {
							}

							filterContext.Controller.ViewBag.UserName = MessageStrings.User;
							filterContext.Controller.ViewBag.UserImage = "/i/placeholder";
							filterContext.Controller.ViewBag.UserInitials = "";
							filterContext.Controller.ViewBag.UserColor = 0;
							filterContext.Controller.ViewBag.IsManager = false;
							filterContext.Controller.ViewBag.ShowL10 = false;
							filterContext.Controller.ViewBag.ShowReview = false;
							filterContext.Controller.ViewBag.ShowSurvey = false;
							filterContext.Controller.ViewBag.ShowPeople = false;
							filterContext.Controller.ViewBag.Organizations = userOrgsCount;
							filterContext.Controller.ViewBag.Hints = true;
							filterContext.Controller.ViewBag.ManagingOrganization = false;
							filterContext.Controller.ViewBag.Organization = null;
							filterContext.Controller.ViewBag.UserId = 0L;
							filterContext.Controller.ViewBag.ConsoleLog = false;
							filterContext.Controller.ViewBag.LimitFiveState = true;
							filterContext.Controller.ViewBag.ShowAC = false;
							filterContext.Controller.ViewBag.ShowCoreProcess = false;
							filterContext.Controller.ViewBag.EvalOnly = false;
							filterContext.Controller.ViewBag.PrimaryVTO = null;




							if (oneUser != null) {
								OneUserViewBagSetup(filterContext, s, userOrgsCount, oneUser);
								SetupToolTips(filterContext.Controller.ViewBag, s, oneUser, Request.NotNull(x => x.Path));
							} else {
								var user = GetUserModel(s);
								filterContext.Controller.ViewBag.Hints = user.Hints;
								filterContext.Controller.ViewBag.UserName = user.Name() ?? MessageStrings.User;
								filterContext.Controller.ViewBag.UserColor = user.GetUserHashCode();
								SetupAccessibilty(filterContext, user);
							}

						}

						filterContext.Controller.ViewBag.SoftwareVersion = SoftwareVersion.Get(s);
						filterContext.Controller.ViewBag.SwitchStyles = SwitchesAccessor.GetSwitchStyles(filterContext.RequestContext.HttpContext.Request.Url.AbsoluteUri, isRadialAdmin);

						tx.Commit();
						s.Flush();
					}
				}
				//DataCollection.CommentMarkProfile(2, "End");
				base.OnActionExecuting(filterContext);

				HandleReadOnly(filterContext, readonlyMode);

			} catch (LoginException) {
				var f = filterContext.HttpContext.Response.Filter;
				filterContext.HttpContext.Response.Filter = null;
				SignOut();
				var redirectUrl = Request.Url.PathAndQuery;
				log.Info("Login: [" + Request.Url.PathAndQuery + "] --> [" + redirectUrl + "]");
				filterContext.Result = RedirectToAction("Login", "Account", new { area = "", returnUrl = redirectUrl });
				//filterContext.ExceptionHandled = true;
				filterContext.HttpContext.Response.Clear();
			} catch (PermissionsException e) {
				var f = filterContext.HttpContext.Response.Filter;
				filterContext.HttpContext.Response.Filter = null;
				filterContext.Result = RedirectToAction("Index", "Error", new {
					area = "",
					message = e.Message,
					redirectUrl = e.RedirectUrl
				});
			}
		}

		public class PrefetchData {
			public bool ReadOnlyMode { get; set; }
			public string InjectedScript { get; set; }
			public UserModel User { get; set; }
			public List<TooltipSeen> SeenTooltips { get; set; }
			public List<FeatureSwitch> FeatureSwitch { get; set; }
		}

		private PrefetchData Prefetch(ISession s) {
			//Fetch1
			var a = s.QueryOver<Variable>().WhereRestrictionOn(x => x.K).IsIn(new[] { Variable.Names.READ_ONLY_MODE, Variable.Names.INJECTED_SCRIPTS }).Future();
			var userId = User.Identity.GetUserId();
			IEnumerable<TooltipSeen> ttSeen = null;
			IEnumerable<UserModel> userF = null;
			if (userId != null) {
				userF = s.QueryOver<UserModel>().Where(x => x.Id == userId).Future();
				ttSeen = s.QueryOver<TooltipSeen>().Where(x => x.UserId == userId).Future();
			}
			var d = s.QueryOver<FeatureSwitch>().Where(x => x.DeleteTime == null).Future();
			var aa = a.ToList();



			//Fetch2
			UserModel user = null;
			if (userF != null) {
				user = userF.NotNull(x => x.ToList().FirstOrDefault());

			}

			return new PrefetchData() {
				FeatureSwitch = d.ToList(),
				InjectedScript = a.ToList().FirstOrDefault(x => x.K == Variable.Names.INJECTED_SCRIPTS).NotNull(x => x.V),
				ReadOnlyMode = a.ToList().FirstOrDefault(x => x.K == Variable.Names.READ_ONLY_MODE).NotNull(x => x.V.ToLower() == "true"),
				SeenTooltips = ttSeen.NotNull(x => x.ToList()),
				User = user
			};
		}

		private void HandleReadOnly(ActionExecutingContext filterContext, bool readonlyMode) {
			try {
				var actionDisc = filterContext.ActionDescriptor;
				var ctrl = actionDisc.ControllerDescriptor.ControllerName.ToLower();
				var action = actionDisc.ActionName.ToLower();
				if (readonlyMode) {
					try {
						if (ctrl == "l10" && action == "meeting") {
							try {
								filterContext.Result = RedirectToAction("meeting", "Readonly", filterContext.RequestContext.RouteData.Values);
							} catch (Exception) {
								filterContext.Result = RedirectToAction("Index", "Readonly");
							}
						} else if (ctrl != "readonly" && !(ctrl == "account" && action == "login") && ctrl != "admin" && !(ctrl == "issues" && action == "pad")) {
							filterContext.Result = RedirectToAction("Index", "Readonly");
						}
					} catch (Exception) {
						filterContext.Result = RedirectToAction("Index", "Readonly");
					}
				} else {
					if (ctrl == "readonly") {
						filterContext.Result = RedirectToAction("Index", "Home");
					}
				}
			} catch (Exception) {

			}
		}

		private void SetupPermissionsOverride(UserOrganizationModel oneUser, UserModel actualUserModel, bool isRadialAdmin, bool adminShortCircuit) {
			if (oneUser._PermissionsOverrides == null) {
				oneUser._PermissionsOverrides = new PermissionsOverrides();
			}

			oneUser._PermissionsOverrides.Admin = new AdminShortCircuit() { IsRadialAdmin = isRadialAdmin, };
			if (actualUserModel != null) {
				oneUser._PermissionsOverrides.Admin.ActualUserId = actualUserModel.Id;
				if (oneUser.User != null) {
					oneUser._PermissionsOverrides.Admin.IsMocking = oneUser.User.Id != actualUserModel.Id;
				}
			}
			if (adminShortCircuit) {
				AllowAdminsWithoutAudit();
			}
		}

		protected string GetAppVersion() {
			var version = Assembly.GetExecutingAssembly().GetName().Version;
			//var buildDate = new DateTime(2000, 1, 1).AddDays(version.Build).AddSeconds(version.Revision * 2);
			return version.ToString();
		}

		private void SetupToolTips(dynamic ViewBag, ISession s, UserOrganizationModel oneUser, string path) {
			try {
				var username = oneUser.User.NotNull(x => x.Id);
				var enabled = !oneUser.User.NotNull(x => x.DisableTips);
				if (username != null && path != null && enabled) {
					ViewBag.TooltipsEnabled = true;
					ViewBag.Tooltips = SupportAccessor.GetTooltips(s, username, path);
				}
			} catch (Exception) {
				//Eat it! Get yourself a fork and feed it.
			}
		}

		private static void OneUserViewBagSetup(ActionExecutingContext filterContext, ISession s, int userOrgsCount, UserOrganizationModel oneUser) {
			var nameStr = oneUser.GetName();
			var name = new HtmlString(nameStr);

			if (userOrgsCount > 1) {
				//name = new HtmlString(oneUser.GetNameAndTitle(1));
				try {
					name = new HtmlString(name + " <span class=\"visible-md visible-lg\" style=\"display:inline ! important\">at " + oneUser.Organization.Name.Translate() + "</span>");
				} catch (Exception e) {
					log.Error(e);
				}
			}
			filterContext.Controller.ViewBag.UserImage = oneUser.ImageUrl(true, ImageSize._img);
			filterContext.Controller.ViewBag.UserInitials = oneUser.GetInitials();
			filterContext.Controller.ViewBag.UserColor = oneUser.GeUserHashCode();
			filterContext.Controller.ViewBag.UsersName = oneUser.GetName();

			filterContext.Controller.ViewBag.UserOrganization = oneUser;
			filterContext.Controller.ViewBag.ConsoleLog = oneUser.User.NotNull(x => x.ConsoleLog);
			SetupAccessibilty(filterContext, oneUser.User);

			filterContext.Controller.ViewBag.TaskCount = 0;

			filterContext.Controller.ViewBag.UserName = name;
			filterContext.Controller.ViewBag.ShowL10 = oneUser.Organization.Settings.EnableL10 && !oneUser.EvalOnly;
			filterContext.Controller.ViewBag.ShowReview = oneUser.Organization.Settings.EnableReview && !oneUser.IsClient;
			filterContext.Controller.ViewBag.ShowSurvey = oneUser.Organization.Settings.EnableSurvey && oneUser.IsManager() && !oneUser.EvalOnly;
			filterContext.Controller.ViewBag.ShowPeople = oneUser.Organization.Settings.EnablePeople;// && oneUser.IsManager();
			filterContext.Controller.ViewBag.ShowCoreProcess = oneUser.Organization.Settings.EnableCoreProcess && !oneUser.EvalOnly;// && oneUser.IsManager();
			filterContext.Controller.ViewBag.EvalOnly = oneUser.EvalOnly;// && oneUser.IsManager();

			//VTO
			try {
				filterContext.Controller.ViewBag.PrimaryVto = null;
				filterContext.Controller.ViewBag.PrimaryVto = L10Accessor.GetSharedVTOVision(oneUser, oneUser.Organization.Id);
			} catch (Exception) {
				//Eat it.
			}

			var isManager = oneUser.ManagerAtOrganization || oneUser.ManagingOrganization || oneUser.IsRadialAdmin;
			filterContext.Controller.ViewBag.LimitFiveState = oneUser.Organization.Settings.LimitFiveState;

			var superAdmin = oneUser.IsRadialAdmin || (filterContext.Controller.ViewBag.IsRadialAdmin ?? false);

			filterContext.Controller.ViewBag.IsRadialAdmin = superAdmin;
			filterContext.Controller.ViewBag.IsManager = isManager;
			filterContext.Controller.ViewBag.ManagingOrganization = oneUser.ManagingOrganization || oneUser.IsRadialAdmin;
			filterContext.Controller.ViewBag.UserId = oneUser.Id;
			filterContext.Controller.ViewBag.OrganizationId = oneUser.Organization.Id;
			filterContext.Controller.ViewBag.Organization = oneUser.Organization;
			filterContext.Controller.ViewBag.Hints = oneUser.User.NotNull(x => x.Hints);

			//var settings = new SettingsViewModel() {
			//	user = new SettingsViewModel.User() {
			//		name = nameStr,
			//		isOrganizationAdmin = oneUser.ManagingOrganization,
			//		isSupervisor = isManager,
			//		userId = oneUser.Id,
			//		userName = oneUser.GetEmail(),
			//		isSuperAdmin = superAdmin
			//	},
			//};

			var settings = SettingsAccessor.GenerateViewSettings(oneUser, nameStr, isManager, superAdmin);
			filterContext.Controller.ViewBag.Settings = settings;
			var forceGetLayoutData = false;
			var isJsonResult = false;
			var isPartialResult = false;
			var isContentResult = false;
			var isFileResult = false;
			try {
				var action = GetActionMethod(filterContext);
				isJsonResult = typeof(JsonResult).IsAssignableFrom(action.ReturnType) || typeof(Task<JsonResult>).IsAssignableFrom(action.ReturnType);
				isPartialResult = typeof(PartialViewResult).IsAssignableFrom(action.ReturnType) || typeof(Task<PartialViewResult>).IsAssignableFrom(action.ReturnType);
				isContentResult = typeof(ContentResult).IsAssignableFrom(action.ReturnType) || typeof(Task<ContentResult>).IsAssignableFrom(action.ReturnType);
				isFileResult = typeof(FileResult).IsAssignableFrom(action.ReturnType) || typeof(Task<FileResult>).IsAssignableFrom(action.ReturnType);
			} catch {
				forceGetLayoutData = true;
			}



			//Only for pages, not partials
			if (forceGetLayoutData || (!isJsonResult && !isPartialResult && !isContentResult && !isFileResult)) {
				filterContext.Controller.ViewBag.ShowAC = PermissionsAccessor.IsPermitted(s, oneUser, x => x.CanView(PermItem.ResourceType.AccountabilityHierarchy, oneUser.Organization.AccountabilityChartId)); // oneUser.Organization.acc && oneUser.IsManager();
				filterContext.Controller.ViewBag.InjectedScripts = VariableAccessor.Get(Variable.Names.INJECTED_SCRIPTS, () => "<script>/*none*/</script>");
				//try {
				//	var perms = PermissionsUtility.Create(s, oneUser);
				//	filterContext.Controller.ViewBag.WorkspaceDropdown = DashboardAccessor.GetWorkspaceDropdown(s, perms, oneUser.Id);
				//} catch (Exception) {
				//	filterContext.Controller.ViewBag.WorkspaceDropdown = new WorkspaceDropdownVM();
				//}
				try {
					settings.notifications = NotificationAccessor.GetNotificationCountForUser_Unsafe(s, oneUser.Id, NotificationDevices.Computer)
						.Select(x => new SettingsViewModel.Notification() {
							id = x.Id,
							canMarkSeen = x.CanMarkSeen,
						}).ToList();
				} catch (Exception e) {
				}
			}
			//filterContext.Controller.ViewBag.Settings = settings;
		}

		protected static void SetupAccessibilty(ActionExecutingContext filterContext, UserModel user) {
			try {
				if (user != null) {
					filterContext.Controller.ViewBag.AccessibilityClasses = user.ColorMode.ToClassName();
				}
			} catch (Exception) {
				//ops
			}

		}

		protected override void OnActionExecuted(ActionExecutedContext filterContext) {
			HibernateSession.CloseCurrentSession();
			if (ToValidate.Any()) {
				var err = "Didn't validate: " + String.Join(",", ToValidate);
				TempData["Message"] = err;
				throw new PermissionsException(err);
			}

			if (TempData["ModelState"] != null && !ModelState.Equals(TempData["ModelState"])) {
				ModelState.Merge((ModelStateDictionary)TempData["ModelState"]);
			}

			if (TempData["Message"] != null) {
				ViewBag.Message = TempData["Message"];
			}

			if (TempData["InfoAlert"] != null) {
				ViewBag.InfoAlert = TempData["InfoAlert"];
			}

			CompressContent(filterContext);
			base.OnActionExecuted(filterContext);
		}
		#region Angular Json Overrides
		protected new JsonResult Json(object data) {
			if (data is IAngular && (Request == null || Request.Params["transform"] == null)) {
				return base.Json(AngularSerializer.Serialize((IAngular)data));
			}

			if (data is IEnumerable<IAngular> && (Request == null || Request.Params["transform"] == null)) {
				return base.Json(((IEnumerable<IAngular>)data).Select(x => AngularSerializer.Serialize(x)).ToArray());
			}

			return base.Json(data);
		}

		protected new JsonResult Json(object data, JsonRequestBehavior behavior) {
			if (data is IAngular && TransformAngular && (Request == null || Request.Params["transform"] == null)) {
				return base.Json(AngularSerializer.Serialize((IAngular)data), behavior);
			}

			if (data is IEnumerable<IAngular> && TransformAngular && (Request == null || Request.Params["transform"] == null)) {
				return base.Json(((IEnumerable<IAngular>)data).Select(x => AngularSerializer.Serialize(x)).ToArray(), behavior);
			}

			return base.Json(data, behavior);
		}
		protected new JsonResult Json(object data, string contentType) {
			if (data is IAngular && TransformAngular && (Request == null || Request.Params["transform"] == null)) {
				return base.Json(AngularSerializer.Serialize((IAngular)data), contentType);
			}

			if (data is IEnumerable<IAngular> && TransformAngular && (Request == null || Request.Params["transform"] == null)) {
				return base.Json(((IEnumerable<IAngular>)data).Select(x => AngularSerializer.Serialize(x)).ToArray(), contentType);
			}

			return base.Json(data, contentType);
		}
		protected new JsonResult Json(object data, string contentType, JsonRequestBehavior behavior) {
			if (data is IAngular && TransformAngular && (Request == null || Request.Params["transform"] == null)) {
				return base.Json(AngularSerializer.Serialize((IAngular)data), contentType, behavior);
			}

			if (data is IEnumerable<IAngular> && TransformAngular && (Request == null || Request.Params["transform"] == null)) {
				return base.Json(((IEnumerable<IAngular>)data).Select(x => AngularSerializer.Serialize(x)).ToArray(), contentType, behavior);
			}

			return base.Json(data, contentType, behavior);
		}
		protected new JsonResult Json(object data, string contentType, Encoding encoding) {
			if (data is IAngular && TransformAngular && (Request == null || Request.Params["transform"] == null)) {
				return base.Json(AngularSerializer.Serialize((IAngular)data), contentType, encoding);
			}

			if (data is IEnumerable<IAngular> && TransformAngular && (Request == null || Request.Params["transform"] == null)) {
				return base.Json(((IEnumerable<IAngular>)data).Select(x => AngularSerializer.Serialize(x)).ToArray(), contentType, encoding);
			}

			return base.Json(data, contentType, encoding);
		}
		protected new JsonResult Json(object data, string contentType, Encoding encoding, JsonRequestBehavior behavior) {
			if (data is IAngular && TransformAngular && (Request == null || Request.Params["transform"] == null)) {
				return base.Json(AngularSerializer.Serialize((IAngular)data), contentType, encoding, behavior);
			}

			if (data is IEnumerable<IAngular> && TransformAngular && (Request == null || Request.Params["transform"] == null)) {
				return base.Json(((IEnumerable<IAngular>)data).Select(x => AngularSerializer.Serialize(x)).ToArray(), contentType, encoding, behavior);
			}

			return base.Json(data, contentType, encoding, behavior);
		}

		#endregion
		#endregion
	}

}


namespace RadialReview {
	public class SettingsViewModel {
		public SettingsViewModel() {
			user = new User();
			organization = new Org();
			notifications = new List<Notification>();
			signalr = new SignalR();
		}
		public class User {
			public long userId { get; set; }
			public string userName { get; set; }
			public string name { get; set; }
			public bool isOrganizationAdmin { get; set; }
			public bool isSupervisor { get; set; }
			public bool isSuperAdmin { get; set; }
		}
		public class Org {
			public string name { get; set; }
			public long organizationId { get; set; }
		}
		public class Notification {
			public long id { get; set; }
			public bool canMarkSeen { get; set; }
		}
		public class SignalR {
			public string endpoint_pattern { get; set; }
			public string endpoint_count { get; set; }
		}

		public SignalR signalr { get; set; }
		public User user { get; set; }
		public Org organization { get; set; }
		public List<Notification> notifications { get; set; }


		public HtmlString ToJson() {
			return new HtmlString(JsonConvert.SerializeObject(this));
		}
	}
}