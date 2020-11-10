function getSuccessStart() {
	return $("#successStart").is(":checked");
} function getSuccessJoin() {
	return $("#successJoin").is(":checked");
}

function simulateConnect(self) {

}
function simulateSpike(self) {
	var originalStart = initializeSignalrShim.successStart;
	var originalJoin = initializeSignalrShim.successJoin;
	initializeSignalrShim.successJoin = true;
	$(self).attr("disabled", true);
	initializeSignalrShim.trigger("onDisconnected");

	setTimeout(function () {
		initializeSignalrShim.successJoin = originalJoin;
		$(self).attr("disabled", false);
		initializeSignalrShim.trigger("onDisconnected");
	}, 100);

}
function simulateDisconnect(self) {
	initializeSignalrShim.trigger("onDisconnected");

}

function initializeSignalrShim() {

	var originalConsole = console;
	function logger(color) {
		return function () {
			color = color || "black";
			var line = $("<div style='color:"+color+"'>");
			for (var i = 0; i < arguments.length; i++) {
				var a = arguments[i];
				if (typeof (a) !== "object") {
					line.append("<span>" + a + "</span>");
				} else {
					line.append("<span>" + JSON.stringify(a) + "</span>");
				}
//				originalConsole.log(arguments[i]);
			}
			$("code").prepend(line);
		}
	}


	console = {
		log: logger("gray"),
		error: logger("red"),
		warn: logger("darkgoldenrod"),
		info: logger("blue"),
	};

	var events = {
		onDisconnected: [],
		onReconnected: [],
		onReconnecting: [],
		onConnectionSlow:[]
	};
	initializeSignalrShim.events = events;
	
	var addCallback = function (event) {
		return function (callback) {
			events[event].push(callback);
		};
	};

	initializeSignalrShim.trigger = function (event) {
		for (var i = 0;i<events[event].length; i++) {
			var e = events[event][i];
			try {
				e();
			} catch (er) {
				console.error("[SignalRShim] Error:",er);
			}


		}
	};



	$.hubConnection = function () {
		return {
			createHubProxies: function () { },
			start: function () {
				var result = {};
				var success = getSuccessStart();
				result.done = function (callback) {
					if (success) {
						callback();
					}
					return result;
				};
				result.fail = function (callback) {
					if (!success) {
						callback();
					}
					return result;
				};
				return result;
			},
			reconnecting: addCallback("onReconnecting"),
			reconnected: addCallback("onReconnected"),
			disconnected: addCallback("onDisconnected"),
			connectionSlow: addCallback("onConnectionSlow"),
		};
	};
	$.connection = $.connection || {};
	$.connection.hub = $.connection.hub || {};
	$.connection.realTimeHub = $.connection.realTimeHub || {};
	$.connection.realTimeHub.server = $.connection.realTimeHub.server || {};
	$.connection.realTimeHub.on = $.connection.realTimeHub.on || function () { };
	$.connection.realTimeHub.server.join = $.connection.realTimeHub.server.join || function () {
		var result = {};
		var success = getSuccessJoin();
		result.done = function (callback) {
			if (success) {
				callback();
			}
			return result;
		};
		result.fail = function (callback) {
			if (!success) {
				callback();
			}
			return result;
		};
		return result;
	};

}
//$.connection.hub.start = $.connection.hub.