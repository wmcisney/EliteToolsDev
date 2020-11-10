window.TT = window.TT || {};

// hasOwnIndex
function arrayHasOwnIndex(array, prop) {
	return array.hasOwnProperty(prop);// && /^0$|^[1-9]\d*$/.test(prop) && prop <= 4294967294; // 2^32 - 2
}


Constants = {
	StartHubSettings: { /*transport: ['webSockets', 'longPolling']*/
		pingInterval: 20000
	},
	SilentReconnectDuration : 1000
};

function showDebug(show) {
	$("body").toggleClass("show-debug", show);
}

function UrlEncodingFix(str) {
	str = replaceAll("%26%2339%3B", "%27", str);
	str = replaceAll("%26%2334%3B", "%22", str);
	str = replaceAll("%26%2313%3B", "%0A", str);
	str = replaceAll("%26%2310%3B", "%0D", str);
	return str;
}

function escapeString(str) {
	if (typeof (str) !== "string")
		return str;
	str = str.replace(/"/g, "&quot;");
	str = str.replace(/'/g, "&#39;");
	return str;
}

function generateGuid(dash) {
	var result, i, j;
	result = '';
	for (j = 0; j < 32; j++) {
		if (j == 8 || j == 12 || j == 16 || j == 20)
			if (dash != false) {
				result = result + '-';
			}
		i = Math.floor(Math.random() * 16).toString(16).toUpperCase();
		result = result + i;
	}
	return result;
}

function goToUserDetails() {
	window.location = "/user/details/" + UserId;
}


function metGoal(direction, goal, measured, alternate) {

	if (!$.trim(measured)) {
		return undefined;
	} else if ($.isNumeric(measured)) {
		var m = +((measured + "").replace(/,/gi, "."));
		var g = +((goal + "").replace(/,/gi, "."));
		if (direction == "GreaterThan" || direction == 1) {
			return m >= g;
		} else if (direction == "LessThan" || direction == -1) {
			return m < g;
		} else if (direction == "LessThanOrEqual" || direction == -2) {
			return m <= g;
		} else if (direction == "GreaterThanNotEqual" || direction == 2) {
			return m > g;
		} else if (direction == "EqualTo" || direction == 0) {
			return m == g;
		} else if (direction == "Between" || direction == -3) {
			var ag = +((alternate + "").replace(/,/gi, "."));
			return g <= m && m <= ag;
		} else {
			console.log("[Radial] Error: goal met could not be calculated. Unhandled direction: " + direction);
			return undefined;
		}
	} else {
		return undefined;
	}
}


function imageListFormat(state) {
	if (!state.id) {
		return state.text;
	}
	var $state = $('<span><img style="max-width:32;max-height:32px"  src="' + $(state.element).data("img") + '" class="img-flag" /> ' + state.text + '</span>');
	return $state;
};

jQuery.cachedScript = function (url, options) {
	options = $.extend(options || {}, {
		dataType: "script",
		cache: true,
		url: url
	});
	return jQuery.ajax(options);
};


/**
 * @brief Wait for something to be ready before triggering a timeout
 * @param {callback} isready Function which returns true when the thing we're waiting for has happened
 * @param {callback} success Function to call when the thing is ready
 * @param {callback} [error] Function to call if we time out before the event becomes ready
 * @param {int} [count] Number of times to retry the timeout (default 300 or 6s)
 * @param {int} [interval] Number of milliseconds to wait between attempts (default 20ms)
// */
function waitUntil(isready, success, error, count, interval) {
	if (count === undefined) {
		count = 300;
	}
	if (interval === undefined) {
		interval = 20;
	}
	if (isready()) {
		success();
		return;
	}
	// The call back isn't ready. We need to wait for it
	setTimeout(function () {
		if (!count) {
			// We have run out of retries
			if (error !== undefined) {
				error();
			}
		} else {
			// Try again
			waitUntil(isready, success, error, count - 1, interval);
		}
	}, interval);
}

function waitUntilVisible(selector, onVisible, duration) {
	if (typeof (duration) === "undefined")
		duration = 60 * 50;

	var interval = 50;
	var count = Math.max(duration / interval, 1);

	waitUntil(function () {
		return $(selector).is(":visible");
	}, onVisible, function () { }, count, interval);
}

function isIOS() {
	var iOS = /iPad|iPhone|iPod/.test(navigator.userAgent) && !window.MSStream;
	return iOS;
}

if (isIOS()) {

	function setTextareaPointerEvents(value) {
		var nodes = document.getElementsByClassName('scrollOver');
		for (var i = 0; i < nodes.length; i++) {
			nodes[i].style.pointerEvents = value;
		}
	}

	document.addEventListener('DOMContentLoaded', function () {
		setTextareaPointerEvents('none');
	});

	document.addEventListener('touchstart', function () {
		setTextareaPointerEvents('auto');
	});

	document.addEventListener('touchmove', function () {
		e.preventDefault();
		setTextareaPointerEvents('none');
	});

	document.addEventListener('touchend', function () {
		setTimeout(function () {
			setTextareaPointerEvents('none');
		}, 0);
	});

	var b = document.getElementsByTagName('body')[0];
	b.className += ' is-ios';
}

function isSafari() {
	var ua = navigator.userAgent.toLowerCase();
	if (ua.indexOf('safari') != -1) {
		if (ua.indexOf('chrome') > -1) {
		} else {
			return true;
		}
	}
	return false;
}

function chromeVersion() {
	try {
		var rawChrome = navigator.userAgent.match(/Chrom(e|ium)\/([0-9]+)\./);
		rawChrome = rawChrome ? parseInt(rawChrome[2], 10) : false;
		return rawChrome;
	} catch (e) {
		console.error(e);
	}
	return false;
}


function setFormula(measurableId) {
	var id = measurableId;
	showModal("Edit formula", "/scorecard/formulapartial/" + id, "/scorecard/setformula?id=" + id, null, function () {
		showAlert("Updating formula...");
	}, function (d) {
		clearAlerts();
		showAlert("Formula updated!");
	});
}

function checkEmail(email) {
	var re = /^(([^<>()\[\]\\.,;:\s@"]+(\.[^<>()\[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
	return re.test(email);
}

function checkReferral(model) {
	if (model.EmailTo.trim() == "")
		return "Email is required";

	if (!checkEmail(model.EmailTo))
		return "Please check the email.";
}


var Notifications = new function () {

	/**
	 * Add a notification to the list
	 * @param {any} notification
	 */
	this.add = function (notification) {
		var id;
		try {
			if (typeof (notification) === "number") {
				id = notification;
				if (!window.settings.notifications.some(function (x) { return x.id == id; })) {
					window.settings.notifications.push({
						id: id,
						canMarkSeen: true
					});
				}
			} else if (typeof (notification) === "object") {
				id = notification.id;
				var canMarkSeen = notification.canMarkSeen;

				if (typeof (notification.id) === "undefined") {
					id = notification.Id;
				}
				if (typeof (notification.canMarkSeen) === "undefined") {
					canMarkSeen = notification.CanMarkSeen;
				}
				if (!window.settings.notifications.some(function (x) { return x.id == id; })) {
					window.settings.notifications.push({
						id: id,
						canMarkSeen: canMarkSeen,
					});
				}
			} else {
				throw "Cannot add notification";
			}
		} catch (e) {
			console.error(e, notification);
		}
		try {
			$("[data-notification='" + id + "'] .notification-seen").attr("data-seen", "null");
		} catch (e) {
			console.error(e, notification);
		}


		Notifications.refreshCount();
	};
	/**
	 * Remove a notification to the list
	 * @param {any} notification
	 */
	this.remove = function (id) {
		try {
			window.settings.notifications = window.settings.notifications.filter(function (x) {
				return x.id != id;
			});
		} catch (e) {
			console.error(e, id);
		}

		try {
			$("[data-notification='" + id + "'] .notification-seen").attr("data-seen", "true");
		} catch (e) {
			console.error(e, notification);
		}

		Notifications.refreshCount();
	}

	/**
	 * Clear notifications that can be marked seen
	 */
	this.clear = function () {
		console.log("[Notifications] ClearNotification");
		try {
			window.settings.notifications = window.settings.notifications.filter(function (x) {
				return !x.canMarkSeen;
			});
		} catch (e) {
			console.error(e);
		}
		Notifications.refreshCount();
	};

	/**Unmark the notification  */
	this.toggle = function (id, override) {
		var on = override;

		if (typeof (on) === "undefined") {
			on = !window.settings.notifications.some(function (x) { return x.id == id; });
		}

		if (on) {
			$.ajax({
				url: "/notification/MarkUnread/" + id,
			});
		} else {
			$.ajax({
				url: "/notification/MarkRead/" + id,
			});
		}
	}

	/**Recalculate the notification flag count  */
	this.refreshCount = function () {
		try {
			var n = window.settings.notifications.length;
			$(".notifications-label").html("" + n);
			$(".notifications-label").attr("data-notifications", "" + n);
		} catch (e) {
			console.error(e);
		}
	}

	this.showAllButton = false;

	/**
	 * Close the dropdown
	 * @param {any} e
	 */
	this.openDropdown = function (e) {
		try {
			if (e.shiftKey && settings && settings.user && settings.user.isSuperAdmin) {
				showModal("Create Notification", "/notification/admincreate?userId=" + settings.user.userId, "/notification/admincreate");
				return;
			}
		} catch (e) {
			console.error(e);
		}

		if ($(e.target).closest("#notifications-dropdown").length > 0) {
			return;
		}

		var num = +$(".notifications-label").attr("data-notifications");
		$.ajax({
			url: "/notification/data",
			success: function (data) {
				$("#notifications-dropdown").html("");
				var showAll = this.showAllButton ? "<a class='notifications-view-all' href='/notification/index'>View All</a>" : "";
				var builder = "<div class='notifications-dropdown'><div class='notifications-header'>Notifications " + showAll + "</div><div class='notification-scroller noselect'>";
				builder += "<ul>";
				if (data.length == 0) {
					builder += "<li class='notifications-none'>No notifications</li>";
				}

				for (var i = 0; i < data.length; i++) {
					var row = data[i];
					builder += "<li data-notification='" + row.Id + "' data-canmarkseen='" + row.CanMarkSeen + "'>";
					builder += " <div class='notification-seen' data-seen='" + row.Seen + "' onclick='Notifications.toggle(" + row.Id + ")'></div>";
					builder += " <div class='notification-message'>" + (row.Message || "") + "</div>";
					builder += " <div class='notification-details'>" + (row.Details || "") + "</div>";
					builder += "</li>";
				}


				builder += "</ul>";
				builder += "</div></div>";

				$("#notifications-dropdown").html(builder);

				$.ajax({ url: "/notification/ClearNotifications", method: "post" });

				$(document).on("click.notifications-dropdown", function (evt) {
					if ($(evt.target).closest("#notifications-dropdown").length == 0) {
						Notifications.closeDropdown();
					}
				});

			}
		});
	}

	/**Close the notifications dropdown  */
	this.closeDropdown = function () {
		$("#notifications-dropdown").html("");
		$(document).off(".notifications-dropdown");
	}
}

$(function () {
	try {
		$.placeholder.shim();
	} catch (e) {
		console.error("[Radial] Error initializing placeholder shim");
	}
});

function ensureVersionMatch(currentServerVersion) {
	try {
		if (typeof (currentServerVersion) === "undefined") {
			console.warn("[Radial] Server Software Version is undefined.");
			return;
		}

		var myVersion = Cookies.get('software-version');
		if (typeof (myVersion) === "undefined" || myVersion == "undefined") {
			console.log("[Radial] Software Version: undefined");
			if (typeof (currentServerVersion) !== "undefined") {
				console.log("[Radial] Updating Software Version: " + currentServerVersion);
				Cookies.set('software-version', currentServerVersion);
			}
		} else {
			if (+myVersion !== currentServerVersion) {
				console.log("[Radial] Software Version not equal (mine:" + myVersion + "!= server:" + currentServerVersion + "). Reloading.");
				Cookies.set('software-version', currentServerVersion);
				location.reload(true);
			} else {
				console.log("[Radial] Software Version: " + myVersion + "(matches server)");
			}
		}
	} catch (e) {
		console.error(e);
	}
};


function prefetch(url) {
	try {
		var elem = document.createElement('link');
		elem.setAttribute('rel', 'preload');
		elem.setAttribute('href', url);
		elem.setAttribute('as', 'fetch');
		document.body.prepend(elem);
	} catch (e) {
		console.error("[Radial] Prefetch error:", e);
	}
}



// Credit David Walsh (https://davidwalsh.name/javascript-debounce-function)

// Returns a function, that, as long as it continues to be invoked, will not
// be triggered. The function will be called after it stops being called for
// N milliseconds. If `immediate` is passed, trigger the function on the
// leading edge, instead of the trailing.
function Debounce(func, wait, immediate) {
	var timeout;

	return function executedFunction() {
		var context = this;
		var args = arguments;

		var later = function () {
			timeout = null;
			if (!immediate) func.apply(context, args);
		};

		var callNow = immediate && !timeout;

		clearTimeout(timeout);

		timeout = setTimeout(later, wait);

		if (callNow) func.apply(context, args);
	};
};