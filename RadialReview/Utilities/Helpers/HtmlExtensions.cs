using Newtonsoft.Json;
using RadialReview;
using RadialReview.Models;
using RadialReview.Properties;
using RadialReview.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace System.Web {

	public enum TimeOfDay {
		Beginning,
		End
	}

	public static class HtmlExtensions {
		public static MvcHtmlString DisableToReadonly(this MvcHtmlString helper) {
			if (helper == null) {
				throw new ArgumentNullException();
			}

			var html = helper.ToString();
			html = html.Replace("disabled=\"disabled\"", "readonly=\"readonly\"");
			return new MvcHtmlString(html);
		}

		public static MvcHtmlString Disable(this MvcHtmlString helper, bool disabled) {
			if (helper == null) {
				throw new ArgumentNullException();
			}

			if (disabled) {
				string html = helper.ToString();
				int startIndex = html.IndexOf('>');

				html = html.Insert(startIndex, " readonly=\"readonly\"");
				return MvcHtmlString.Create(html);
			}

			return helper;
		}

		public static string VideoConferenceUrl(this HtmlHelper html, string resource = null) {
			return Config.VideoConferenceUrl(resource);
		}

		public static string GetBaseUrl(this HtmlHelper html, string resource = null) {
			var server = Config.BaseUrl((OrganizationModel)html.ViewBag.Organization).TrimEnd('/');
			if (resource != null) {
				server = server + "/" + resource.TrimStart('/');
			}

			return server;
		}
		public static UserOrganizationModel UserOrganization(this HtmlHelper html) {
			return (UserOrganizationModel)html.ViewBag.UserOrganization;
		}

		public static OrganizationModel Organization(this HtmlHelper html) {
			return (OrganizationModel)html.ViewBag.Organization;
		}
		public static DateTime ConvertFromUtc(this HtmlHelper html, DateTime time) {
			var user = (UserOrganizationModel)html.ViewBag.UserOrganization;
			if (user != null) {
				return time.AddMinutes(-user.GetTimezoneOffset());
			}
			var org = (OrganizationModel)html.ViewBag.Organization;
			if (org != null) {
				return org.ConvertFromUTC(time);
			}

			return time;
		}

		public static HtmlString ConvertFromUtcLocal(this HtmlHelper html, DateTime utcDate, string format = null) {
			var guid = "date_" + Guid.NewGuid().ToString().Replace("-", "");
			var formatArg = format == null ? "" : ",\"" + format + "\"";
			var str = $@"<span class=""display-date local {guid}"" id=""{guid}""><script>document.getElementById(""{guid}"").innerHTML=getFormattedDate(ConvertFromServerTime({utcDate.ToJavascriptMilliseconds()}){formatArg});</script></span>";
			return new HtmlString(str);
		}

		public static string ProductName(this HtmlHelper html) {
			return Config.ProductName(html.Organization());
		}

		public static string ReviewName(this HtmlHelper html) {
			return Config.ReviewName(html.Organization());
		}

		public static MvcHtmlString CollapseSection(this HtmlHelper html, String title, String viewName, object model, string checkboxClass = null) {
			html.ViewData["PartialViewName"] = viewName;
			html.ViewData["SectionTitle"] = title;
			html.ViewData["CheckboxClass"] = checkboxClass;
			return html.Partial("Partial/Collapsable", model, html.ViewData);
		}

		public static HtmlString ViewOrEdit(this HtmlHelper html, bool edit, bool icon = true) {
			if (icon) {
				return new HtmlString(edit ? "<span class='glyphicon glyphicon-pencil viewEdit edit'></span>" : "<span class='glyphicon glyphicon-eye-open viewEdit view'></span>");
			}

			return new HtmlString(edit ? "Edit" : "View");
		}

		public static HtmlString GrayScale(this HtmlHelper html, double value, double neg, double pos, double alpha) {
			double scale = 0;
			if (pos - neg != 0) {
				scale = (value - neg) / (pos - neg) * 255.0;
			}

			int coerced = (int)(255 - Math.Max(0, Math.Min(scale, 255.0)));

			return new HtmlString(String.Format("rgba({0},{0},{0},{1})", coerced, alpha));

		}

		public static HtmlString Color(this HtmlHelper html, double value, double neg, double zero, double pos, double alpha) {
			double v = 0;
			var redValue = 0.0;
			var greenValue = 0.0;
			// value is a value between 0 and 511; 
			// 0 = red, 255 = yellow, 511 = green.
			if (value > zero) {
				if (pos - zero == 0) {
					v = 255;
				} else {
					v = (int)((value - zero) / (pos - zero) * 255.0 + 255.0);
				}
			} else {
				if (zero - neg == 0) {
					v = 0;
				} else {
					v = (int)((value - neg) / (zero - neg) * 255.0);
				}
			}

			v = Math.Max(0, Math.Min(511, v));


			if (v < 255) {
				redValue = 255;
				greenValue = Math.Sqrt(v) * 16;
				greenValue = Math.Round(greenValue);
			} else {
				greenValue = 255;
				v = v - 255;
				redValue = 256 - (v * v / 255);
				redValue = Math.Round(redValue);

			}

			int red = Math.Min(255, Math.Max(0, (int)redValue));
			int green = Math.Min(255, Math.Max(0, (int)greenValue));

			var hexColor = String.Format("rgba({0},{1},{2},{3})", red, green, 0, alpha);

			return new HtmlString(hexColor);
		}

		public static HtmlString ShowNew(this HtmlHelper html, DateTime showUntil) {
			if (DateTime.UtcNow < showUntil) {
				return new HtmlString("<span class='show-new-marker' style='color:red;font-size:70%;opacity:0.7;pointer-events:none;width:0px;display:inline-block;'>New!</span>");
			}
			return new HtmlString("");
		}

		public static HtmlString EditFirstButton(this HtmlHelper html, List<string> items, bool edit = true) {
			var count = items.Count();
			var name = "" + count;
			var after = "";
			var joined = String.Join(", ", items);
			if (count == 1) {
				name = items.First();
			} else if (count == 0) {
				name = "<i>None</i>";
				joined = "None";
			} else {
				name = items.First() + "<span class='hidden'>" + String.Join(",", items.Skip(1)) + "</span>";
				after = "(+" + (count - 1) + ")";
				joined = String.Join(",", items);
			}

			//return new HtmlString("<span class='editFirst'><span class='icon'>" + ViewOrEdit(html, edit).ToHtmlString() + "</span><span title='" + joined + "' class='text'><span class='uncollapsable'>" + after + "</span><span class='collapsable'>" + name + "</span></span></span>");
			return new HtmlString("<span class='editFirst'><span title='" + joined + "' class='text'><span class='uncollapsable'>" + after + "</span><span class='collapsable'>" + name + "</span></span></span>");
		}

		public static HtmlString Badge<T>(this HtmlHelper<T> html, Func<T, int> count) {
			var c = count(html.ViewData.Model);
			if (c != 0) {
				return new HtmlString(@"<span class=""badge"">" + c + "</span>");
			}

			return new HtmlString("");
		}

		public static HtmlString ShowModal(this HtmlHelper html, String title, String pullUrl, String pushUrl, String callbackFunction = null, String preSubmitCheck = null, String onComplete = null, String onCompleteFunction = null) {

			//if (newTab)
			//    return new HtmlString(@"showModal('" + title + @"','" + pullUrl + @"','" + pushUrl + "',null,null,null,true)");


			title = title.Replace("'", "\\'");
			if (onComplete != null || onCompleteFunction != null) {
				var c = "";
				if (onComplete != null) {
					c = "'" + onComplete + "'";
				} else {
					c = onCompleteFunction;
				}

				return new HtmlString(@"showModal('" + title + @"','" + pullUrl + @"','" + pushUrl + "','" + callbackFunction + "','" + preSubmitCheck + "'," + c + ")");
			}
			if (preSubmitCheck != null) {
				return new HtmlString(@"showModal('" + title + @"','" + pullUrl + @"','" + pushUrl + "','" + callbackFunction + "','" + preSubmitCheck + "')");
			} else if (callbackFunction != null) {
				return new HtmlString(@"showModal('" + title + @"','" + pullUrl + @"','" + pushUrl + "','" + callbackFunction + "')");
			} else {
				return new HtmlString(@"showModal('" + title + @"','" + pullUrl + @"','" + pushUrl + "')");
			}
		}

		public static HtmlString AlertBoxDismissableJavascript(this HtmlHelper html, String messageVariableName, String alertType = "alert-danger") {
			return new HtmlString("\"<div class=\\\"alert " + alertType + " alert-dismissable\\\"><button type=\\\"button\\\" class=\\\"close\\\" data-dismiss=\\\"alert\\\" aria-hidden=\\\"true\\\">&times;</button><strong>" + MessageStrings.Warning + "</strong> <span class=\\\"message\\\">\" + " + messageVariableName + " + \"</span></div>\"");

		}
		public static HtmlString AlertBoxDismissable(this HtmlHelper html, String message, String alertType = null, String alertMessage = null) {
			if (String.IsNullOrWhiteSpace(alertType)) {
				alertType = "alert-danger";
			}

			if (String.IsNullOrWhiteSpace(alertMessage)) {
				alertMessage = MessageStrings.Warning;
			}

			if (!String.IsNullOrWhiteSpace(message)) {
				return new HtmlString("<div class=\"alert " + alertType + " alert-dismissable\"><button type=\"button\" class=\"close\" data-dismiss=\"alert\" aria-hidden=\"true\">&times;</button><strong>" + alertMessage + "</strong> <span class=\"message\">" + message + "</span></div>");
			}

			return new HtmlString("");
		}


		public static HtmlString BootstrapValidationSummary(this HtmlHelper html, Boolean excludePropertyErrors) {
			var errors = html.ViewData.ModelState.Values.SelectMany(x => x.Errors);
			var output = @"<div class=""validation-summary-" + (errors.Any() ? "errors" : "valid") + @" alert alert-error"" data-valmsg-summary=""true"">";
			output += @"<button type=""button"" class=""close"" data-dismiss=""alert"">&times;</button><table><tr><td><strong>Error:</strong></td><td><ul>";
			if (errors.Any()) {
				foreach (var li in errors) {
					output += @"<li>" + li.ErrorMessage + "</li>";
				}
			} else {
				output += @"<li style=""display:none""></li>";
			}
			output += "</ul></td></tr></table></div>";
			return new HtmlString(output);
		}
		public static HtmlString ValidationSummaryMin(this HtmlHelper html, Boolean excludePropertyErrors = false) {

			/*
             *
             * <div class="validation-summary-errors" data-valmsg-summary="true">
             * <ul><li>Value must be between 1 and 10.</li>
                <li>Value must be between 1 and 10.</li>
                <li>Value must be between 1 and 10.</li>
                <li>Value must be between 1 and 10.</li>
                <li>Value must be between 1 and 10.</li>
                </ul></div> 
             * 
             */

			var errors = html.ViewData.ModelState.Values.SelectMany(x => x.Errors);
			var output = @"<div class=""validation-summary-" + (errors.Any() ? "errors" : "valid") + @" alert alert-error"" data-valmsg-summary=""true"">";
			//output += @"<button type=""button"" class=""close"" data-dismiss=""alert"">&times;</button><table><tr><td><strong>Error:</strong></td><td>";
			output += "<ul>";
			if (errors.Any()) {
				foreach (var li in errors.GroupBy(x => x.ErrorMessage)) {
					output += @"<li>" + li.First().ErrorMessage + "</li>";
				}
			} else {
				output += @"<li style=""display:none""></li>";
			}
			output += "</ul>" +/*"</td></tr></table>"*/ "</div>";
			return new HtmlString(output);
		}

		public static MvcHtmlString ArrayToString<T>(this HtmlHelper html, IEnumerable<T> items, bool format = false) {
			//	var unproxied = items.Select(x => NHibernateProxyRemover.From(x));

			return new MvcHtmlString(JsonConvert.SerializeObject(items, new JsonSerializerSettings() {
				Formatting = format ? Formatting.Indented : Formatting.None
			}).Replace("</script>", "<\\/script>"));
			//Json.Encode());
		}

		public class Builder<T> {
			public class HtmlDataTable {
				public List<Expression<Func<T, object>>> Columns { get; set; }
				public string EditUrl { get; set; }
				public string CreateUrl { get; set; }
				public string DeleteUrl { get; set; }
				public string Title { get; set; }

				public HtmlDataTable(string title, params Expression<Func<T, object>>[] columns) {
					Title = title;
					Columns = columns.ToList();
				}
			}

			public Builder2 Build(string title, params Expression<Func<T, object>>[] columns) {
				return new Builder2(new HtmlDataTable(title, columns.ToArray()));
			}
			public class Builder2 {
				private HtmlDataTable inner { get; set; }
				public Builder2(HtmlDataTable table) {
					this.inner = table;
				}

				public Builder2 EditUrl(string url) {
					inner.EditUrl = url;
					return this;
				}
				public Builder2 CreateUrl(string url) {
					inner.CreateUrl = url;
					return this;
				}
				public Builder2 DeleteUrl(string url) {
					inner.DeleteUrl = url;
					return this;
				}
				public HtmlDataTable ToDataTable() {
					return inner;
				}
			}

		}


		public static MvcHtmlString ToDataTable<T>(this HtmlHelper<List<T>> html, Func<Builder<T>, Builder<T>.Builder2> table) {
			return ToDataTable(html, table(new Builder<T>()).ToDataTable());
		}

		private static MvcHtmlString ToDataTable<T>(this HtmlHelper<List<T>> html, Builder<T>.HtmlDataTable table) {
			var builder = $@"DataTable({{";
			if (table.CreateUrl != null) {
				builder += $@"
	clickAdd: {{ 
		title: ""Add {table.Title}"",
		fields: {html.ToJsonFields(table.Columns.ToArray()).ToHtmlString()},
		pushUrl: ""{table.CreateUrl}"" 
	}},";
			}
			var cells = table.Columns.ToList();
			if (table.EditUrl != null) {
				cells.Add(x => "edit");
				builder += $@"
	clickEdit:true,
	postEdit:""{table.EditUrl}"",";
			}

			if (table.DeleteUrl != null) {
				cells.Add(x => "remove");
			}
			builder += $@"
	cells: { html.FieldToRow(cells.ToArray()).ToHtmlString()},
	data: {html.ArrayToString(html.ViewData.Model).ToHtmlString()},
}});";


			return new MvcHtmlString(builder);
		}

		private static string GenerateJsonField<T>(Expression<Func<T, object>> field) {
			var name = field.GetMemberName();
			var builder = "{";
			builder += "name:\"" + name + "\",";
			builder += "text:\"" + field.GetPropertyDisplayName() + "\",";
			var type = field.GetPropertyType();
			if (type.IsEnum) {
				builder += "type:\"select\",";
				builder += "options:[";
				foreach (var e in Enum.GetValues(type)) {
					builder += $@"{{text:""{ExpressionExtensions.SplitCamelCase(Enum.GetName(type, e))}"", value: ""{Enum.GetName(type, e)}""}},";
				}
				builder += "],";
			}
			if (field.GetMemberName().ToLower() == "id") {
				builder += "type:\"hidden\"";
			} else if (type == typeof(bool)) {
				builder +=
$@"	type:""radio"",
	options:[
		{{text:""yes"",value:true,  checked:function(x){{ return x.value==""true"";}} }},
		{{text:""no"", value:false, checked:function(x){{ return x.value!=""true"";}} }}
	],";

			} else if (type == typeof(long) || type == typeof(int) || type == typeof(long?) || type == typeof(int?)) {
				builder += "type:\"number\",";
			} else if (type == typeof(DateTime) || type == typeof(DateTime?)) {
				builder += "type:\"datetime\",";
			}
			builder += "}";
			return builder;
		}

		private static MvcHtmlString ToJsonFields<T>(this HtmlHelper<List<T>> html, params Expression<Func<T, object>>[] fields) {

			var builder = "[";
			foreach (var field in fields) {
				builder += GenerateJsonField(field) + ",";
			}
			builder += "]";
			return new MvcHtmlString(builder);
		}

		public static MvcHtmlString FieldToRow<T>(this HtmlHelper<List<T>> html, params Expression<Func<T, object>>[] fields) {

			var unusedProps = typeof(T).GetProperties().Select(x => x.Name).ToList();

			var builder = "[";
			foreach (var field in fields) {
				//Remove and Edit switchs.
				if (field.Body is ConstantExpression) {
					var obj = ((ConstantExpression)field.Body).Value;
					if (obj is string) {
						var str = ((string)obj).ToLower();
						builder += "";
						switch (str) {
							case "edit":
								builder += "{edit:true,classes:\"noWidth\"},";
								break;
							case "remove":
								builder += "{remove:true,classes:\"noWidth\"},";
								break;
							default:
								break;
						}
					}
				} else {
					var name = field.GetMemberName();
					//remove from unused list
					unusedProps.Remove(name);

					//build the column
					builder +=
$@"{{
	id:""{name}"",
	name:""{field.GetPropertyDisplayName()}"",
";
					//builder += "id:\"" + name + "\",";
					//builder += "name:\"" + field.GetPropertyDisplayName() + "\",";
					var type = field.GetPropertyType();
					if (type == typeof(DateTime) || type == typeof(DateTime?)) {
						builder += "	contents:function(x){ return getFormattedDate(x." + name + "); },";
					} else if (type.IsEnum) {
						var enumLookup1 = "";
						var enumLookup2 = "";
						foreach (var e in Enum.GetValues(type)) {
							enumLookup1 += "enumLookup[\"" + e + "\"]=\"" + ExpressionExtensions.SplitCamelCase(Enum.GetName(type, e)) + "\";\n";
							try {
								enumLookup2 += "enumLookup[" + (int)e + "]=\"" + ExpressionExtensions.SplitCamelCase(Enum.GetName(type, e)) + "\";\n";
							} catch (Exception) {
							}
						}
						builder +=
$@"	contents:function(x){{ 
		var enumLookup = {{}};
		{enumLookup1}
		{enumLookup2}
		if (x.{field.GetMemberName()} in enumLookup){{
			return enumLookup[x.{field.GetMemberName()}];
		}} 
		return x.{field.GetMemberName()};
	}},";
					} else if (type == typeof(bool) || type == typeof(bool?)) {
						builder += "	contents:function(x){ return \"\"+x." + name + ";},";
					} else {
						builder += "	contents:function(x){ return x." + name + ";},";
					}

					builder += $@"	input: {GenerateJsonField(field)} ,";

					builder += "},\n";
				}
			}

			foreach (var name in unusedProps) {
				builder +=
$@"{{
	id:""{name}"",
	contents:function(x){{return x.{name};}},
	hideColumn:true, 
	input:{{
		type:""hidden"",
		name:""{name}"",
	}} 
}},";

			}


			builder += "]";
			return new MvcHtmlString(builder);
		}

		public static MvcHtmlString ToJson<T>(this HtmlHelper html, T item) {
			if (item == null)
				return new MvcHtmlString("null");
			return new MvcHtmlString(JsonConvert.SerializeObject(item));
		}

		public static IEnumerable<object> AdaptArray<T>(this HtmlHelper html, IEnumerable<T> items, Func<T, object> converter) {
			return items.Select(x => converter(x));
		}

		public static MvcHtmlString ArrayToString<T>(this HtmlHelper html, IEnumerable<T> items, Func<T, object> converter) {//params Tuple<string,Expression<Func<T,object>>>[] nameColumns) {
																															 //	var unproxied = items.Select(x => NHibernateProxyRemover.From(x));

			//var convert = items.Select(x => {
			//	var o = new Dictionary<string, object>();
			//	foreach (var nc in nameColumns) {
			//		o[nc.Item1] = x.Get(nc.Item2);
			//	}
			//	return o;
			//});

			var convert = AdaptArray(html, items, converter);

			return new MvcHtmlString(JsonConvert.SerializeObject(convert));
			//Json.Encode());
		}

		public static string NewGuid(this HtmlHelper html) {
			return "g" + Guid.NewGuid().ToString().Replace("-", "");
		}


		public static HtmlString ClientDateFor<T>(this HtmlHelper<T> html, Expression<Func<T, DateTime>> serverDateSelector, TimeOfDay timeOfDay, string classes = null) {
			var model = (T)html.ViewData.Model;
			var name = html.NameFor(serverDateSelector);
			var id = html.IdFor(serverDateSelector);
			var serverDate = serverDateSelector.Compile();
			var guid = html.NewGuid();
			return BuildClientDate(guid, id, name, serverDate(model), timeOfDay, classes);
		}

		public static HtmlString ClientDate(this HtmlHelper html, string name, DateTime serverDate, TimeOfDay timeOfDay, string classes = null) {
			var n = new MvcHtmlString(name);
			var guid = html.NewGuid();
			return BuildClientDate(guid, n, n, serverDate, timeOfDay, classes);
		}

		private static HtmlString BuildClientDate(string guid, MvcHtmlString id, MvcHtmlString name, DateTime serverDate, TimeOfDay timeOfDay, string classes) {
			var date = "";
			var builder = $@"
<div class='{guid} client-datepicker {classes}'></div>
<script>
	setTimeout(function(){{
		var options = {{
			selector : $("".{guid}.client-datepicker""),
			serverTime : new Date(""{serverDate.ToString("yyyy-MM-dd HH:mm:ss")}""),
			displayAsLocal : true,
			name:""{name}"",
			id:""{id}"",
			datePickerOptions:undefined,
			endOfDay: {(timeOfDay == TimeOfDay.End ? "true" : "false")},
		}};
		Time.createClientDatepicker(options);
	}},1);
 </script>
";
			return new HtmlString(builder);
		}





		//public class TableOptions {
		//	public string @class { get; set; }
		//}

		//public static MvcHtmlString Table<T>(this HtmlHelper html, IEnumerable<T> row,TableOptions options, params Func<T,MvcHtmlString>[] cells) where T : ILongIdentifiable {
		//	StringBuilder
		//}


		#region Blocks
		private class ScriptBlock : IDisposable {
			private const string scriptsKey = "scripts";

			private string UniqueKey = null;
			public static List<string> pageScripts {
				get {
					if (HttpContext.Current.Items[scriptsKey] == null) {
						HttpContext.Current.Items[scriptsKey] = new List<string>();
					}

					return (List<string>)HttpContext.Current.Items[scriptsKey];
				}
			}

			public static List<string> uniqueKeys {
				get {
					if (HttpContext.Current.Items[scriptsKey + "_uniqueKeys"] == null) {
						HttpContext.Current.Items[scriptsKey + "_uniqueKeys"] = new List<string>();
					}

					return (List<string>)HttpContext.Current.Items[scriptsKey + "_uniqueKeys"];
				}
			}


			WebViewPage webPageBase;

			public ScriptBlock(WebViewPage webPageBase, string uniqueKey) {
				this.webPageBase = webPageBase;
				this.webPageBase.OutputStack.Push(new StringWriter());
				this.UniqueKey = uniqueKey;
			}

			public void Dispose() {
				if (UniqueKey == null || !uniqueKeys.Contains(UniqueKey)) {
					if (UniqueKey != null) {
						uniqueKeys.Add(UniqueKey);
					}

					pageScripts.Add(((StringWriter)this.webPageBase.OutputStack.Pop()).ToString());
				}
			}
		}

		private class StyleBlock : IDisposable {
			private const string styleKey = "styles";
			private string UniqueKey = null;
			public static List<string> pageStyles {
				get {
					if (HttpContext.Current.Items[styleKey] == null) {
						HttpContext.Current.Items[styleKey] = new List<string>();
					}

					return (List<string>)HttpContext.Current.Items[styleKey];
				}
			}

			public static List<string> uniqueKeys {
				get {
					if (HttpContext.Current.Items[styleKey + "_uniqueKeys"] == null) {
						HttpContext.Current.Items[styleKey + "_uniqueKeys"] = new List<string>();
					}

					return (List<string>)HttpContext.Current.Items[styleKey + "_uniqueKeys"];
				}
			}

			WebViewPage webPageBase;

			public StyleBlock(WebViewPage webPageBase, string uniqueKey) {
				this.webPageBase = webPageBase;
				this.webPageBase.OutputStack.Push(new StringWriter());
				this.UniqueKey = uniqueKey;
			}

			public void Dispose() {
				if (UniqueKey == null || !uniqueKeys.Contains(UniqueKey)) {
					if (UniqueKey != null) {
						uniqueKeys.Add(UniqueKey);
					}

					pageStyles.Add(((StringWriter)this.webPageBase.OutputStack.Pop()).ToString());
				}
			}
		}

		public static IDisposable BeginScripts(this HtmlHelper helper, string uniqueKey = null) {
			return new ScriptBlock((WebViewPage)helper.ViewDataContainer, uniqueKey);
		}

		public static MvcHtmlString PageScripts(this HtmlHelper helper) {
			return MvcHtmlString.Create(string.Join(Environment.NewLine, ScriptBlock.pageScripts.Select(s => s.ToString())));
		}
		public static IDisposable BeginStyles(this HtmlHelper helper, string uniqueKey = null) {
			return new StyleBlock((WebViewPage)helper.ViewDataContainer, uniqueKey);
		}

		public static MvcHtmlString PageStyles(this HtmlHelper helper) {
			return MvcHtmlString.Create(string.Join(Environment.NewLine, StyleBlock.pageStyles.Select(s => s.ToString())));
		}
		#endregion



	}


}