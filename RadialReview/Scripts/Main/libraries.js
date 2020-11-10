
//Debounce
(function (n, t) { var $ = n.jQuery || n.Cowboy || (n.Cowboy = {}), i; $.throttle = i = function (n, i, r, u) { function o() { function o() { e = +new Date; r.apply(h, c) } function l() { f = t } var h = this, s = +new Date - e, c = arguments; u && !f && o(); f && clearTimeout(f); u === t && s > n ? o() : i !== !0 && (f = setTimeout(u ? l : o, u === t ? n - s : n)) } var f, e = 0; return typeof i != "boolean" && (u = r, r = i, i = t), $.guid && (o.guid = r.guid = r.guid || $.guid++), o }; $.debounce = function (n, r, u) { return u === t ? i(n, r, !1) : i(n, u, r !== !1) } })(this);

//tabbable.js
//https://github.com/marklagendijk/jquery.tabbable/blob/master/jquery.tabbable.min.js
!function (e) { "use strict"; function t(t) { var n = e(t), a = e(":focus"), r = 0; if (1 === a.length) { var i = n.index(a); i + 1 < n.length && (r = i + 1) } n.eq(r).focus() } function n(t) { var n = e(t), a = e(":focus"), r = n.length - 1; if (1 === a.length) { var i = n.index(a); i > 0 && (r = i - 1) } n.eq(r).focus() } function a(t) { function n(t) { return e.expr.filters.visible(t) && !e(t).parents().addBack().filter(function () { return "hidden" === e.css(this, "visibility") }).length } var a, r, i, u = t.nodeName.toLowerCase(), o = !isNaN(e.attr(t, "tabindex")); return "area" === u ? (a = t.parentNode, r = a.name, t.href && r && "map" === a.nodeName.toLowerCase() ? (i = e("img[usemap=#" + r + "]")[0], !!i && n(i)) : !1) : (/input|select|textarea|button|object/.test(u) ? !t.disabled : "a" === u ? t.href || o : o) && n(t) } e.focusNext = function () { t(":focusable") }, e.focusPrev = function () { n(":focusable") }, e.tabNext = function () { t(":tabbable") }, e.tabPrev = function () { n(":tabbable") }, e.extend(e.expr[":"], { data: e.expr.createPseudo ? e.expr.createPseudo(function (t) { return function (n) { return !!e.data(n, t) } }) : function (t, n, a) { return !!e.data(t, a[3]) }, focusable: function (t) { return a(t, !isNaN(e.attr(t, "tabindex"))) }, tabbable: function (t) { var n = e.attr(t, "tabindex"), r = isNaN(n); return (r || n >= 0) && a(t, !r) } }) }(jQuery);

//jquery.autoResize.js
//https://github.com/alexbardas/jQuery.fn.autoResize/blob/master/jquery.autoresize.js
/*(function (n) {
	n.fn.autoResize = function (t) {
		var i = n.extend({ onResize: function () { }, animate: !1, animateDuration: 150, animateCallback: function () { }, extraSpace: 0, limit: 1e3, useOriginalHeight: !1 }, t), u, r; return this.destroyList = [], u = this, r = null, this.filter("textarea").each(function () {
			var t = n(this).css({ resize: "none", "overflow-y": "hidden" }), c = i.useOriginalHeight ? t.height() : 0, e = function () { var f = {}, i; 
			return n.each(["height", "width", "lineHeight", "textDecoration", "letterSpacing"], function (n, i) { f[i] = t.css(i) }), i = t.clone().removeAttr("id").removeAttr("name").css({ position: "absolute", top: 0, left: -9999 }).css(f).attr("tabIndex", "-1").insertBefore(t), r != null && n(r).remove(), r = i, u.destroyList.push(i), i }(), o = null, f = function () {
				var f = {}, r, u; if (n.each(["height", "width", "lineHeight", "textDecoration", "letterSpacing"], function (n, i) { f[i] = t.css(i) }), e.css(f), e.height(0).val(n(this).val()).scrollTop(1e4), r = Math.max(e.scrollTop(), c) + i.extraSpace, u = n(this).add(e), o !== r) {
					if (o = r, r >= i.limit) { n(this).css("overflow-y", ""); return } i.onResize.call(this);
					r = Math.max(16, r);; i.animate && t.css("display") === "block" ? u.stop().animate({ height: r }, i.animateDuration, i.animateCallback) : u.height(r)
				}
			}, s, h; t.unbind(".dynSiz").bind("keyup.dynSiz", f).bind("keydown.dynSiz", f).bind("change.dynSiz", f); s = function () { f.call(t) }; n(window).bind("resize.dynSiz", function () { clearTimeout(h); h = setTimeout(s, 100) }); setTimeout(function () { f.call(t) }, 1)
		}), this.destroy = function () { for (var t = 0; t < this.destroyList.length; t++) n(this.destroyList[t]).remove(); this.destroyList = [] }, this
	}
})(jQuery);*/
/*
 * jQuery.fn.autoResize 1.1
 * --
 * https://github.com/jamespadolsey/jQuery.fn.autoResize
 * --
 * This program is free software. It comes without any warranty, to
 * the extent permitted by applicable law. You can redistribute it
 * and/or modify it under the terms of the Do What The Fuck You Want
 * To Public License, Version 2, as published by Sam Hocevar. See
 * http://sam.zoy.org/wtfpl/COPYING for more details. */

(function ($) {
	var defaults = autoResize.defaults = {
		onResize: function () { },
		animate: {
			duration: 200,
			complete: function () { }
		},
		extraSpace: 0,
		minHeight: 'original',
		maxHeight: 500,
		minWidth: 'original',
		maxWidth: 500
	};
	autoResize.cloneCSSProperties = [
		'lineHeight', 'textDecoration', 'letterSpacing',
		'fontSize', 'fontFamily', 'fontStyle', 'fontWeight',
		'textTransform', 'textAlign', 'direction', 'wordSpacing', 'fontSizeAdjust',
		'width', 'padding'
	];
	autoResize.cloneCSSValues = {
		position: 'absolute',
		top: -9999,
		left: -9999,
		opacity: 0,
		overflow: 'hidden'
	};
	autoResize.resizableFilterSelector = 'textarea,input:not(input[type]),input[type=text],input[type=password]';
	autoResize.AutoResizer = AutoResizer;
	$.fn.autoResize = autoResize;
	function autoResize(config) {
		this.filter(autoResize.resizableFilterSelector).each(function () {
			new AutoResizer($(this), config);
		});
		return this;
	}
	function AutoResizer(el, config) {
		config = this.config = $.extend(autoResize.defaults, config);
		this.el = el;
		this.nodeName = el[0].nodeName.toLowerCase();
		this.originalHeight = el.height();
		this.previousScrollTop = null;
		this.value = el.val();
		if (config.maxWidth === 'original') config.maxWidth = el.width();
		if (config.minWidth === 'original') config.minWidth = el.width();
		if (config.maxHeight === 'original') config.maxHeight = el.height();
		if (config.minHeight === 'original') config.minHeight = el.height();
		if (this.nodeName === 'textarea') {
			el.css({
				resize: 'none',
				overflowY: 'hidden'
			});
		}
		el.data('AutoResizer', this);
		this.createClone();
		this.injectClone();
		this.bind();
	}
	AutoResizer.prototype = {
		bind: function () {
			var check = $.proxy(function () {
				this.check();
				return true;
			}, this);
			var focus = $.proxy(function () {
				this.focus();
				return true;
			}, this);
			var blur = $.proxy(function () {
				this.blur();
				return true;
			}, this);
			this.unbind();
			this.el.bind('keyup.autoResize', check)
				//.bind('keydown.autoResize', check)
				.bind('change.autoResize', check)
				.bind('focus.autoResize', focus)
				.bind('blur.autoResize', blur);
			var self = this;
			self.check(null, true);
		},
		focus: function () {
			var self = this;
			this.focused = true;
			try {
				var rescroll = function () {
					try {
						if (self.el[0].scrollTop != 0) {
							self.el[0].scrollTop = 0;
						}
						if (self.focused) {
							window.requestAnimationFrame(rescroll);
						}
					} catch (e) {
					}
				}
				window.requestAnimationFrame(rescroll);
			} catch (e) {
			}
		},
		blur: function () {
			this.focused = false;
			//clearInterval(this.focusInterval);
		},
		unbind: function () {
			this.el.unbind('.autoResize');
		},
		createClone: function () {
			var el = this.el, clone;
			if (this.nodeName === 'textarea') {
				clone = el.clone().height('auto');
			} else {
				clone = $('<span/>').width('auto').css({ whiteSpace: 'nowrap' });
			}
			this.clone = clone;
			$.each(autoResize.cloneCSSProperties, function (i, p) {
				clone[0].style[p] = el.css(p);
			});

			if (typeof (this.config.style) === "object") {
				var styles = this.config.style;
				$.each(styles, function (i, p) {
					clone[0].style[i] = p;
				});
			}
			clone.removeAttr('name').removeAttr('id').attr('tabIndex', -1).css(autoResize.cloneCSSValues);
			//$("body").append(clone);
		},
		check: function (e, immediate) {
			var self = this;
			var checkFunc = function () {
				var config = self.config, clone = self.clone, el = self.el, value = el.val();
				el.scrollTop = 0;
				if (self.nodeName === 'input') {
					clone.text(value);
					// Calculate new width + whether to change
					var cloneWidth = clone.width(),
						newWidth = (cloneWidth + config.extraSpace) >= config.minWidth ?
							cloneWidth + config.extraSpace : config.minWidth,
						currentWidth = el.width();
					newWidth = Math.min(newWidth, config.maxWidth);
					if ((newWidth < currentWidth && newWidth >= config.minWidth) || (newWidth >= config.minWidth && newWidth <= config.maxWidth)) {
						config.onResize.call(el);
						el.scrollLeft(0);
						if (config.animate && !immediate) {
							//debugger;
							el.stop(1, 1).animate({ width: newWidth }, config.animate);
						} else {
							el.width(newWidth);
						}
					}
					return;
				}
				// TEXTAREA
				clone.height(0).val(value).scrollTop(10000);
				clone.css({ 'width': $(el).outerWidth() });
				var scrollTop = clone[0].scrollTop + config.extraSpace;
				// Don't do anything if scrollTop hasen't changed:
				if (self.previousScrollTop === scrollTop) {
					return;
				}
				var forceImmediate = false;
				if (self.previousScrollTop <= scrollTop) {
					forceImmediate = true;
				}
				self.previousScrollTop = scrollTop;
				if (scrollTop >= config.maxHeight) {
					el.css('overflowY', '');
					return;
				}
				el.css('overflowY', 'hidden');
				if (scrollTop < config.minHeight) {
					scrollTop = config.minHeight;
				}
				config.onResize.call(el);
				// Either animate or directly apply height:

				if (config.animate && !immediate && !forceImmediate) {
					//debugger;
					//setTimeout(function () {
					//	debugger;
					//	el[0].scrollTop = 0;
					//}, 100);
					el[0].scrollTop = 0;
					el.stop(1, 1).animate({ height: scrollTop }, config.animate);
				} else {
					el.height(scrollTop);
				}
			};
			try {
				window.requestAnimationFrame(checkFunc);
			} catch (e) {
				checkFunc();
			}

		},
		destroy: function () {
			this.unbind();
			this.el.removeData('AutoResizer');
			this.clone.remove();
			delete this.el;
			delete this.clone;
		},
		injectClone: function () {
			(autoResize.cloneContainer || (autoResize.cloneContainer = $('<arclones/>').appendTo('body'))).append(this.clone);
		}
	};

})(jQuery);



// $().waitUntilExist
; (function ($, window) {

	var intervals = {};
	var removeListener = function (selector) {

		if (intervals[selector]) {

			window.clearInterval(intervals[selector]);
			intervals[selector] = null;
		}
	};
	var found = 'waitUntilExists.found';

	/**
     * @function
     * @property {object} jQuery plugin which runs handler function once specified
     *           element is inserted into the DOM
     * @param {function|string} handler 
     *            A function to execute at the time when the element is inserted or 
     *            string "remove" to remove the listener from the given selector
     * @param {bool} shouldRunHandlerOnce 
     *            Optional: if true, handler is unbound after its first invocation
     * @example jQuery(selector).waitUntilExists(function);
     */

	$.fn.waitUntilExists = function (selector, handler, shouldRunHandlerOnce, isChild) {

		//var selector = this.selector;
		var $this = $(selector);
		var $elements = $this.not(function () { return $(this).data(found); });

		if (selector === 'remove') {

			// Hijack and remove interval immediately if the code requests
			removeListener(selector);
		}
		else {

			// Run the handler on all found elements and mark as found
			$elements.each(handler).data(found, true);

			if (shouldRunHandlerOnce && $this.length) {

				// Element was found, implying the handler already ran for all 
				// matched elements
				removeListener(selector);
			}
			else if (!isChild) {

				// If this is a recurring search or if the target has not yet been 
				// found, create an interval to continue searching for the target
				intervals[selector] = window.setInterval(function () {

					$this.waitUntilExists(selector, handler, shouldRunHandlerOnce, true);
				}, 500);
			}
		}

		return $this;
	};

}(jQuery, window));

/*!
 * JavaScript Cookie v2.2.0
 * https://github.com/js-cookie/js-cookie
 *
 * Copyright 2006, 2015 Klaus Hartl & Fagner Brack
 * Released under the MIT license
 */
; (function (factory) {
	var registeredInModuleLoader;
	if (typeof define === 'function' && define.amd) {
		define(factory);
		registeredInModuleLoader = true;
	}
	if (typeof exports === 'object') {
		module.exports = factory();
		registeredInModuleLoader = true;
	}
	if (!registeredInModuleLoader) {
		var OldCookies = window.Cookies;
		var api = window.Cookies = factory();
		api.noConflict = function () {
			window.Cookies = OldCookies;
			return api;
		};
	}
}(function () {
	function extend() {
		var i = 0;
		var result = {};
		for (; i < arguments.length; i++) {
			var attributes = arguments[i];
			for (var key in attributes) {
				result[key] = attributes[key];
			}
		}
		return result;
	}
	function decode(s) {
		return s.replace(/(%[0-9A-Z]{2})+/g, decodeURIComponent);
	}
	function init(converter) {
		function api() { }
		function set(key, value, attributes) {
			if (typeof document === 'undefined') {
				return;
			}
			attributes = extend({
				path: '/'
			}, api.defaults, attributes);
			if (typeof attributes.expires === 'number') {
				attributes.expires = new Date(new Date() * 1 + attributes.expires * 864e+5);
			}
			// We're using "expires" because "max-age" is not supported by IE
			attributes.expires = attributes.expires ? attributes.expires.toUTCString() : '';
			try {
				var result = JSON.stringify(value);
				if (/^[\{\[]/.test(result)) {
					value = result;
				}
			} catch (e) { }
			value = converter.write ?
				converter.write(value, key) :
				encodeURIComponent(String(value))
					.replace(/%(23|24|26|2B|3A|3C|3E|3D|2F|3F|40|5B|5D|5E|60|7B|7D|7C)/g, decodeURIComponent);
			key = encodeURIComponent(String(key))
				.replace(/%(23|24|26|2B|5E|60|7C)/g, decodeURIComponent)
				.replace(/[\(\)]/g, escape);
			var stringifiedAttributes = '';
			for (var attributeName in attributes) {
				if (!attributes[attributeName]) {
					continue;
				}
				stringifiedAttributes += '; ' + attributeName;
				if (attributes[attributeName] === true) {
					continue;
				}
				// Considers RFC 6265 section 5.2:
				// ...
				// 3.  If the remaining unparsed-attributes contains a %x3B (";")
				//     character:
				// Consume the characters of the unparsed-attributes up to,
				// not including, the first %x3B (";") character.
				// ...
				stringifiedAttributes += '=' + attributes[attributeName].split(';')[0];
			}
			return (document.cookie = key + '=' + value + stringifiedAttributes);
		}
		function get(key, json) {
			if (typeof document === 'undefined') {
				return;
			}
			var jar = {};
			// To prevent the for loop in the first place assign an empty array
			// in case there are no cookies at all.
			var cookies = document.cookie ? document.cookie.split('; ') : [];
			var i = 0;
			for (; i < cookies.length; i++) {
				var parts = cookies[i].split('=');
				var cookie = parts.slice(1).join('=');
				if (!json && cookie.charAt(0) === '"') {
					cookie = cookie.slice(1, -1);
				}
				try {
					var name = decode(parts[0]);
					cookie = (converter.read || converter)(cookie, name) || decode(cookie);
					if (json) {
						try {
							cookie = JSON.parse(cookie);
						} catch (e) { }
					}
					jar[name] = cookie;
					if (key === name) {
						break;
					}
				} catch (e) { }
			}
			return key ? jar[key] : jar;
		}

		api.set = set;
		api.get = function (key) {
			return get(key, false /* read as raw */);
		};
		api.getJSON = function (key) {
			return get(key, true /* read as json */);
		};
		api.remove = function (key, attributes) {
			set(key, '', extend(attributes, {
				expires: -1
			}));
		};
		api.defaults = {};
		api.withConverter = init;
		return api;
	}
	return init(function () { });
}));

////Fix Submenus
//$('ul.dropdown-menu [data-toggle=dropdown]').on('click', function (event) {
//	event.preventDefault();
//	event.stopPropagation();
//	$('ul.dropdown-menu [data-toggle=dropdown]').parent().removeClass('open');
//	$(this).parent().addClass('open');
//});



/*
* jQuery File Download Plugin v1.4.5
*
* http://www.johnculviner.com
*
* Copyright (c) 2013 - John Culviner
*
* Licensed under the MIT license:
*   http://www.opensource.org/licenses/mit-license.php
*
* !!!!NOTE!!!!
* You must also write a cookie in conjunction with using this plugin as mentioned in the orignal post:
* http://johnculviner.com/jquery-file-download-plugin-for-ajax-like-feature-rich-file-downloads/
* !!!!NOTE!!!!
*/

(function ($, window) {
	// i'll just put them here to get evaluated on script load
	var htmlSpecialCharsRegEx = /[<>&\r\n"']/gm;
	var htmlSpecialCharsPlaceHolders = {
		'<': 'lt;',
		'>': 'gt;',
		'&': 'amp;',
		'\r': "#13;",
		'\n': "#10;",
		'"': 'quot;',
		"'": '#39;' /*single quotes just to be safe, IE8 doesn't support &apos;, so use &#39; instead */
	};

	$.extend({
		//
		//$.fileDownload('/path/to/url/', options)
		//  see directly below for possible 'options'
		fileDownload: function (fileUrl, options) {

			//provide some reasonable defaults to any unspecified options below
			var settings = $.extend({

				//
				//Requires jQuery UI: provide a message to display to the user when the file download is being prepared before the browser's dialog appears
				//
				preparingMessageHtml: null,

				//
				//Requires jQuery UI: provide a message to display to the user when a file download fails
				//
				failMessageHtml: null,

				//
				//the stock android browser straight up doesn't support file downloads initiated by a non GET: http://code.google.com/p/android/issues/detail?id=1780
				//specify a message here to display if a user tries with an android browser
				//if jQuery UI is installed this will be a dialog, otherwise it will be an alert
				//Set to null to disable the message and attempt to download anyway
				//
				androidPostUnsupportedMessageHtml: "Unfortunately your Android browser doesn't support this type of file download. Please try again with a different browser.",

				//
				//Requires jQuery UI: options to pass into jQuery UI Dialog
				//
				dialogOptions: { modal: true },

				//
				//a function to call while the dowload is being prepared before the browser's dialog appears
				//Args:
				//  url - the original url attempted
				//
				prepareCallback: function (url) { },

				//
				//a function to call after a file download successfully completed
				//Args:
				//  url - the original url attempted
				//
				successCallback: function (url) { },

				//
				//a function to call after a file download request was canceled
				//Args:
				//  url - the original url attempted
				//
				abortCallback: function (url) { },

				//
				//a function to call after a file download failed
				//Args:
				//  responseHtml    - the html that came back in response to the file download. this won't necessarily come back depending on the browser.
				//                      in less than IE9 a cross domain error occurs because 500+ errors cause a cross domain issue due to IE subbing out the
				//                      server's error message with a "helpful" IE built in message
				//  url             - the original url attempted
				//  error           - original error cautch from exception
				//
				failCallback: function (responseHtml, url, error) { },

				//
				// the HTTP method to use. Defaults to "GET".
				//
				httpMethod: "GET",

				//
				// if specified will perform a "httpMethod" request to the specified 'fileUrl' using the specified data.
				// data must be an object (which will be $.param serialized) or already a key=value param string
				//
				data: null,

				//
				//a period in milliseconds to poll to determine if a successful file download has occured or not
				//
				checkInterval: 100,

				//
				//the cookie name to indicate if a file download has occured
				//
				cookieName: "fileDownload",

				//
				//the cookie value for the above name to indicate that a file download has occured
				//
				cookieValue: "true",

				//
				//the cookie path for above name value pair
				//
				cookiePath: "/",

				//
				//if specified it will be used when attempting to clear the above name value pair
				//useful for when downloads are being served on a subdomain (e.g. downloads.example.com)
				//
				cookieDomain: null,

				//
				//the title for the popup second window as a download is processing in the case of a mobile browser
				//
				popupWindowTitle: "Initiating file download...",

				//
				//Functionality to encode HTML entities for a POST, need this if data is an object with properties whose values contains strings with quotation marks.
				//HTML entity encoding is done by replacing all &,<,>,',",\r,\n characters.
				//Note that some browsers will POST the string htmlentity-encoded whilst others will decode it before POSTing.
				//It is recommended that on the server, htmlentity decoding is done irrespective.
				//
				encodeHTMLEntities: true

			}, options);

			var deferred = new $.Deferred();

			//Setup mobile browser detection: Partial credit: http://detectmobilebrowser.com/
			var userAgent = (navigator.userAgent || navigator.vendor || window.opera).toLowerCase();

			var isIos;                  //has full support of features in iOS 4.0+, uses a new window to accomplish this.
			var isAndroid;              //has full support of GET features in 4.0+ by using a new window. Non-GET is completely unsupported by the browser. See above for specifying a message.
			var isOtherMobileBrowser;   //there is no way to reliably guess here so all other mobile devices will GET and POST to the current window.

			if (/ip(ad|hone|od)/.test(userAgent)) {

				isIos = true;

			} else if (userAgent.indexOf('android') !== -1) {

				isAndroid = true;

			} else {

				isOtherMobileBrowser = /avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|playbook|silk|iemobile|iris|kindle|lge |maemo|midp|mmp|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|symbian|treo|up\.(browser|link)|vodafone|wap|windows (ce|phone)|xda|xiino/i.test(userAgent) || /1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|e\-|e\/|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(di|rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|xda(\-|2|g)|yas\-|your|zeto|zte\-/i.test(userAgent.substr(0, 4));

			}

			var httpMethodUpper = settings.httpMethod.toUpperCase();

			if (isAndroid && httpMethodUpper !== "GET" && settings.androidPostUnsupportedMessageHtml) {
				//the stock android browser straight up doesn't support file downloads initiated by non GET requests: http://code.google.com/p/android/issues/detail?id=1780

				if ($().dialog) {
					$("<div>").html(settings.androidPostUnsupportedMessageHtml).dialog(settings.dialogOptions);
				} else {
					alert(settings.androidPostUnsupportedMessageHtml);
				}

				return deferred.reject();
			}

			var $preparingDialog = null;

			var internalCallbacks = {

				onPrepare: function (url) {

					//wire up a jquery dialog to display the preparing message if specified
					if (settings.preparingMessageHtml) {

						$preparingDialog = $("<div>").html(settings.preparingMessageHtml).dialog(settings.dialogOptions);

					} else if (settings.prepareCallback) {

						settings.prepareCallback(url);

					}

				},

				onSuccess: function (url) {

					//remove the perparing message if it was specified
					if ($preparingDialog) {
						$preparingDialog.dialog('close');
					}

					settings.successCallback(url);

					deferred.resolve(url);
				},

				onAbort: function (url) {

					//remove the perparing message if it was specified
					if ($preparingDialog) {
						$preparingDialog.dialog('close');
					};

					settings.abortCallback(url);

					deferred.reject(url);
				},

				onFail: function (responseHtml, url, error) {

					//remove the perparing message if it was specified
					if ($preparingDialog) {
						$preparingDialog.dialog('close');
					}

					//wire up a jquery dialog to display the fail message if specified
					if (settings.failMessageHtml) {
						$("<div>").html(settings.failMessageHtml).dialog(settings.dialogOptions);
					}

					settings.failCallback(responseHtml, url, error);

					deferred.reject(responseHtml, url);
				}
			};

			internalCallbacks.onPrepare(fileUrl);

			//make settings.data a param string if it exists and isn't already
			if (settings.data !== null && typeof settings.data !== "string") {
				settings.data = $.param(settings.data);
			}


			var $iframe,
				downloadWindow,
				formDoc,
				$form;

			if (httpMethodUpper === "GET") {

				if (settings.data !== null) {
					//need to merge any fileUrl params with the data object

					var qsStart = fileUrl.indexOf('?');

					if (qsStart !== -1) {
						//we have a querystring in the url

						if (fileUrl.substring(fileUrl.length - 1) !== "&") {
							fileUrl = fileUrl + "&";
						}
					} else {

						fileUrl = fileUrl + "?";
					}

					fileUrl = fileUrl + settings.data;
				}

				if (isIos || isAndroid) {

					downloadWindow = window.open(fileUrl);
					downloadWindow.document.title = settings.popupWindowTitle;
					window.focus();

				} else if (isOtherMobileBrowser) {

					window.location(fileUrl);

				} else {

					//create a temporary iframe that is used to request the fileUrl as a GET request
					$iframe = $("<iframe style='display: none' src='" + fileUrl + "'></iframe>").appendTo("body");
				}

			} else {

				var formInnerHtml = "";

				if (settings.data !== null) {

					$.each(settings.data.replace(/\+/g, ' ').split("&"), function () {

						var kvp = this.split("=");

						//Issue: When value contains sign '=' then the kvp array does have more than 2 items. We have to join value back
						var k = kvp[0];
						kvp.shift();
						var v = kvp.join("=");
						kvp = [k, v];

						var key = settings.encodeHTMLEntities ? htmlSpecialCharsEntityEncode(decodeURIComponent(kvp[0])) : decodeURIComponent(kvp[0]);
						if (key) {
							var value = settings.encodeHTMLEntities ? htmlSpecialCharsEntityEncode(decodeURIComponent(kvp[1])) : decodeURIComponent(kvp[1]);
							formInnerHtml += '<input type="hidden" name="' + key + '" value="' + value + '" />';
						}
					});
				}

				if (isOtherMobileBrowser) {

					$form = $("<form>").appendTo("body");
					$form.hide()
						.prop('method', settings.httpMethod)
						.prop('action', fileUrl)
						.html(formInnerHtml);

				} else {

					if (isIos) {

						downloadWindow = window.open("about:blank");
						downloadWindow.document.title = settings.popupWindowTitle;
						formDoc = downloadWindow.document;
						window.focus();

					} else {

						$iframe = $("<iframe style='display: none' src='about:blank'></iframe>").appendTo("body");
						formDoc = getiframeDocument($iframe);
					}

					formDoc.write("<html><head></head><body><form method='" + settings.httpMethod + "' action='" + fileUrl + "'>" + formInnerHtml + "</form>" + settings.popupWindowTitle + "</body></html>");
					$form = $(formDoc).find('form');
				}

				$form.submit();
			}


			//check if the file download has completed every checkInterval ms
			setTimeout(checkFileDownloadComplete, settings.checkInterval);


			function checkFileDownloadComplete() {
				//has the cookie been written due to a file download occuring?

				var cookieValue = settings.cookieValue;
				if (typeof cookieValue == 'string') {
					cookieValue = cookieValue.toLowerCase();
				}

				var lowerCaseCookie = settings.cookieName.toLowerCase() + "=" + cookieValue;

				if (document.cookie.toLowerCase().indexOf(lowerCaseCookie) > -1) {

					//execute specified callback
					internalCallbacks.onSuccess(fileUrl);

					//remove cookie
					var cookieData = settings.cookieName + "=; path=" + settings.cookiePath + "; expires=" + new Date(0).toUTCString() + ";";
					if (settings.cookieDomain) cookieData += " domain=" + settings.cookieDomain + ";";
					document.cookie = cookieData;

					//remove iframe
					cleanUp(false);

					return;
				}

				//has an error occured?
				//if neither containers exist below then the file download is occuring on the current window
				if (downloadWindow || $iframe) {

					//has an error occured?
					try {

						var formDoc = downloadWindow ? downloadWindow.document : getiframeDocument($iframe);

						if (formDoc && formDoc.body !== null && formDoc.body.innerHTML.length) {

							var isFailure = true;

							if ($form && $form.length) {
								var $contents = $(formDoc.body).contents().first();

								try {
									if ($contents.length && $contents[0] === $form[0]) {
										isFailure = false;
									}
								} catch (e) {
									if (e && e.number == -2146828218) {
										// IE 8-10 throw a permission denied after the form reloads on the "$contents[0] === $form[0]" comparison
										isFailure = true;
									} else {
										throw e;
									}
								}
							}

							if (isFailure) {
								// IE 8-10 don't always have the full content available right away, they need a litle bit to finish
								setTimeout(function () {
									internalCallbacks.onFail(formDoc.body.innerHTML, fileUrl);
									cleanUp(true);
								}, 100);

								return;
							}
						}
					}
					catch (err) {

						//500 error less than IE9
						internalCallbacks.onFail('', fileUrl, err);

						cleanUp(true);

						return;
					}
				}


				//keep checking...
				setTimeout(checkFileDownloadComplete, settings.checkInterval);
			}

			//gets an iframes document in a cross browser compatible manner
			function getiframeDocument($iframe) {
				var iframeDoc = $iframe[0].contentWindow || $iframe[0].contentDocument;
				if (iframeDoc.document) {
					iframeDoc = iframeDoc.document;
				}
				return iframeDoc;
			}

			function cleanUp(isFailure) {

				setTimeout(function () {

					if (downloadWindow) {

						if (isAndroid) {
							downloadWindow.close();
						}

						if (isIos) {
							if (downloadWindow.focus) {
								downloadWindow.focus(); //ios safari bug doesn't allow a window to be closed unless it is focused
								if (isFailure) {
									downloadWindow.close();
								}
							}
						}
					}

					//iframe cleanup appears to randomly cause the download to fail
					//not doing it seems better than failure...
					//if ($iframe) {
					//    $iframe.remove();
					//}

				}, 0);
			}


			function htmlSpecialCharsEntityEncode(str) {
				return str.replace(htmlSpecialCharsRegEx, function (match) {
					return '&' + htmlSpecialCharsPlaceHolders[match];
				});
			}
			var promise = deferred.promise();
			promise.abort = function () {
				cleanUp();
				$iframe.attr('src', '').html('');
				internalCallbacks.onAbort(fileUrl);
			};
			return promise;
		}
	});

})(jQuery, this || window);


/**
 * Polyfill
 * https://developer.mozilla.org/en-US/docs/Web/API/CustomEvent/CustomEvent
 */
(function () {

	if (typeof window.CustomEvent === "function") return false;

	function CustomEvent(event, params) {
		params = params || { bubbles: false, cancelable: false, detail: null };
		var evt = document.createEvent('CustomEvent');
		evt.initCustomEvent(event, params.bubbles, params.cancelable, params.detail);
		return evt;
	}

	CustomEvent.prototype = window.Event.prototype;

	window.CustomEvent = CustomEvent;
})();