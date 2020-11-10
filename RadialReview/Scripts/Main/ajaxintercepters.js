/////////////////////////////////////////////////////////////////
//Ajax Interceptors


var interceptAjax = function (event, request, settings) {
	try {
		var result = $.parseJSON(request.responseText);
		try {
			if (result.Refresh) {
				if (result.Silent !== undefined && !result.Silent) {
					result.Refresh = false;
					StoreJsonAlert(result);
				}
				location.reload();
			} else if (result.Redirect) {
				var url = result.Redirect;
				if (result.Silent !== undefined && !result.Silent) {
					result.Refresh = false;
					result.Redirect = false;
					StoreJsonAlert(result);
				}
				if (interceptAjax.AllowRedirect != false) {
					var timeout = setTimeout(function () {
						window.location.href = url;
					}, 100);
					window.HaltRedirect = function () {
						//Halting..
						clearTimeout(timeout);
						interceptAjax.AllowRedirect = false;
						UnstoreJsonAlert();
					};
				} else {
					//was previously halted
					UnstoreJsonAlert();
				}

			} else {
				if (result.Silent !== undefined && !result.Silent) {
					showJsonAlert(result, true, true);
				}
			}
		} catch (e) {
			console.log(e);
		}
	} catch (e) {
	}
};


window.HaltRedirect = function () {
	interceptAjax.AllowRedirect = false;
}

$(document).ajaxSuccess(interceptAjax);
$(document).ajaxError(interceptAjax);
var requesterId = generateGuid();
$(document).ajaxSend(function (event, jqX, ajaxOptions) {
	if (ajaxOptions.prefetch == true) {
		//console.log(jqX, ajaxOptions);		
		//console.log("prefetching", jqX);
		ajaxOptions.crossDomain = true;
		//debugger;
		//var jjqX = jqX;
		//jqX.setRequestHeader = function (name, value) {
		//	debugger;
		//	// Ignore the X-Requested-With header
		//	if (name == 'X-Requested-With') return;
		//	// Otherwise call the native setRequestHeader method
		//	// Note: setRequestHeader requires its 'this' to be the xhr object,
		//	// which is what 'this' is here when executed.
		//	jjqX.setRequestHeader.call(this, name, value);

		//};
		return;
	}

	if (ajaxOptions.url == null) {
		ajaxOptions.url = "";
	}

	if (typeof (ajaxOptions.data) === "string" && ajaxOptions.data.indexOf("_clientTimestamp") != -1) {
		return;
	}
	if (ajaxOptions.url.indexOf("_clientTimestamp") == -1) {
		ajaxOptions.url = Time.addTimestamp(ajaxOptions.url);
	}
	if (ajaxOptions.url.indexOf("_rid") == -1) {
		if (ajaxOptions.url.indexOf("?") == -1) {
			ajaxOptions.url += "?nil=0";
		}
		ajaxOptions.url += "&_rid=" + requesterId;
	}
	console.info(ajaxOptions.type + " " + ajaxOptions.url);
	if (typeof (ajaxOptions.type) === "string" && ajaxOptions.type.toUpperCase() == "POST" && !(ajaxOptions.url.indexOf("/support/email") == 0)) {
		console.info(ajaxOptions.data);
	}
});
/////////////////////////////////////////////////////////////////
