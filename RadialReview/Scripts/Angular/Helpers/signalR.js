angular.module('signalRModule', []).factory('signalR', ['$rootScope', "$timeout", "$window", function ($rootScope, $timeout, $window) {
	throw "Stop using SignalR.js";
	/*
	if (typeof (window.angularSharedSignalR) === 'undefined' || window.angularSharedSignalR === null) {
		angularSharedSignalR = {
			globalConnection: $.hubConnection(),
			proxies: {}
		};
	}

	/*function getConnection() {
		return $.hubConnection();//$.connection;//window.angularSharedSignalR.globalConnection;
	}*

	function startHub(callback, errorCallback) {
		if (typeof (window.RealTime) === "undefined") {
			throw "[SignalR] RealTimeHub undefined. Initialize before calling.";
		}
		window.RealTime.start(function () { }, function (e) {
			if (e.error == "already-started" || e.error == "already-starting") {
				console.log("[SignalR] Start error",e.error);
				clearAlerts();
				if (e.disconnected==false) {
					$("#overlay-offline-container").css("display", "none");
				}
				if (e.reconnecting) {
					showAlert("Reconnected!", "alert-success", "Success", 3000);
				}
			}
			if (typeof (errorCallback) === "function") {
				errorCallback(e);
			}
		});
		if (callback) {
			window.RealTime.afterStart(function () {
				if (callback) {
					callback(window.RealTime);
				}
			});
		}
	}


	function signalRFactory(callback, errorCallback) {
		var disconnected = false;
		startHub(callback, errorCallback);

		var isUnloading = false;
		window.addEventListener("beforeunload", function () {
			isUnloading = true;
		});

		signalRFactory.reconnectCount = 0;

		$.connection.hub.disconnected(function () {
			console.log("[SignalR] Hub disconnect. " + new Date());
			if (!isUnloading) {
				if (signalRFactory.reconnectCount < 10) {
					signalRFactory.reconnectCount += 1;
					setTimeout(function () {
						clearAlerts();
						showAlert("Connection lost. Reconnecting.", 10000);
						disconnected = true;
						setTimeout(function () {
							startHub(function () {
								if (callback) {
									callback(window.RealTime);
								}
								if (window.RealTime.isDisconnected == false) {
									clearAlerts();
									showAlert("Connected.", 3000);
									signalRFactory.reconnectCount = 0;
								}
							});
						}, 5000); // Restart connection after 5 seconds.
					}, 1000);

				} else {
					$("#overlay-offline-container").css("display", "block");
					showAlert("Could not connect. Please check your internet connection and refresh the page.", 3600000);

				}
			}
		});

		var numberOutstanding = 0;

		var o = {
			hub: window.RealTime,
			afterStart: function (callback) {
				window.RealTime.afterStart(function () { callback(window.RealTime) });
			},
			disconnected: disconnected,
			on: function (eventName, callback) {
				RealTime.client[eventName] = function (result) {
					numberOutstanding += 1;
					$rootScope.$emit("BeginCallbackSignalR", numberOutstanding);
					$rootScope.$apply(function () {
						try {
							if (callback) {
								callback(result);
							}
						} catch (e) {
							console.error(e);
						}finally {
							$timeout(function () {
								numberOutstanding -= 1;
								$rootScope.$emit("EndCallbackSignalR", numberOutstanding);
							}, 0);
						}
					});
				};
			},
			invoke: function (methodName, callback, args) {
				debugger;
				console.error("[SignalR] HEY THIS METHOD IS DEPRICATED");
				var ags = [];
				try {
					ags = Array.prototype.slice.call(arguments, 2);
				} catch (e) {
					console.error(e);
				}

				proxy.invoke(methodName, ags)
					.done(function (result) {
						$rootScope.$apply(function () {
							if (callback) {
								callback(result);
							}
						});
					});
			}
		};

		return o;
	};
	return signalRFactory;*/
}]);

