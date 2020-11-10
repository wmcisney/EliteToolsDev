//@ts-check
// console.info("[RealTimeHub] Initializing RealTimeHub");
window.settings = window.settings || {};
window.settings.signalr = window.settings.signalr || {};
window.Constants = window.Constants || {};

var RealTime = new function () {
    console.info("[RealTimeHub] Initializing RealTimeHub");

	/*
	 * Wraps up the signalR hub. Allows us to join multiple hubs. 
	 * Also allows us to slowly initialize the listeners
	 */

    //Collect methods to run after we're done initializing
    this.afterInitialize = [];
    runAfterStart = [];
    this.shouldAutoJoin = true;
    if (typeof (window.shouldAutoJoin) !== "undefined") {
        this.shouldAutoJoin = window.shouldAutoJoin;
    }

	/*
	 * start the hub up.
	 */
    this.start = function () {
        var args = arguments;
        console.log("[RealTimeHub] Scheduling start:", args);
        this.afterInitialize.push(function (s) { s.start.apply(s, args); });
    };

	/*
	 * prevent the hub from starting up.
	 */
    this.disable = function () {
        console.log("[RealTimeHub] Disabling the hub.");
        this.isDisabled = true;
        clearTimeout(this.startUpTimeout);
        try {
            $.connection.hub.stop();
            console.log("[RealTimeHub] Connection stopped.");
        } catch (e) {
            console.log("[RealTimeHub] Failed to stop hub.", e);
        }
    };

	/*
	 * join({
	 *	meetingIds: [],
	 *	surveys: [{ surveyContainerId:long?, surveyId:long? },...],
	 *  organizationIds: [],
	 *  vtoIds: []
	 * });
	 */
    this.join = function () {
        var args = arguments;
        console.log("[RealTimeHub] Scheduling join:", args);
        this.afterInitialize.push(function (s) { s.join.apply(s, args); });
    };

    this.autoJoin = function (shouldAutoJoin) {
        console.log("[RealTimeHub] Setting AutoJoin:" + shouldAutoJoin);
        this.shouldAutoJoin = shouldAutoJoin;
    }

    this.afterStart = function (func) {
        if (typeof (func) !== "function") {
            console.error("[RealTimeHub] Expcecting function.");
            return;
        }
        console.log("[RealTimeHub] Scheduling after start:", func);
        runAfterStart.push(func);
    };

    var starting = false;
    var started = false;
    var privateVariables = { joined: false };
    var startQueue = [];
    var updateMethods = false;

    this.client = {};
    this.server = {};
    this.connectionId = null;
    this.id = null;
    this.initializedEnpoint = false;
    this.isDisconnected = true;
    this.isDisabled = false;
    var tryingToReconnect = false;


    privateVariables._clientObserver = {
        lastClient: {},
    };

    var me = this;
    privateVariables._clientObserver.observe = function (callback) {

        var lastClient = privateVariables._clientObserver.lastClient;
        var lastKeys = Object.keys(lastClient);
        var curKeys = Object.keys(me.client);

        //any removed?
        for (var i = 0; i < lastKeys.length; i++) {
            var lk = lastKeys[i];
            if (curKeys.indexOf(lk) == -1) {
                callback("remove", lk);
                //console.info("[RealTimeHub] Removed function from client:" + lk);
            } else {
                if (lastClient[lk] !== me.client[lk]) {
                    callback("update", lk, me.client[lk]);
                    //console.info("[RealTimeHub] Updated function on client:" + lk);
                }
                //already seen
                delete curKeys[lk];
            }
        }

        for (var i = 0; i < curKeys.length; i++) {
            var ck = curKeys[i];
            if (lastKeys.indexOf(ck) == -1) {
                callback("add", ck, me.client[ck]);
                //console.info("[RealTimeHub] Added function to client:" + ck);
            }
        }
        privateVariables._clientObserver.lastClient = $.extend({}, me.client);
    };


    privateVariables._clientObserver.lastClient = {};
    if (privateVariables._clientObserver.interval) {
        clearTimeout(privateVariables._clientObserver.interval);
    }
    privateVariables._clientObserver.interval = setInterval(function () {
        if (updateMethods) {
            privateVariables._clientObserver.observe(function (type, key, func) {
                switch (type) {
                    case "add": $.connection.realTimeHub.on(key, func); break;
                    case "update": $.connection.realTimeHub.off(key); $.connection.realTimeHub.on(key, func); break;
                    case "remove": $.connection.realTimeHub.off(key); break;
                    default: console.error("[RealTimeHub] Unknown observer type:" + type);
                }
            });
        }
    }, 100);

    var self = this;

    this.startUpTimeout = setTimeout(function () {

        window.settings.signalr.endpoint_pattern = window.settings.signalr.endpoint_pattern || "https://s{0}.traction.tools/signalr";
        window.settings.signalr.endpoint_count = window.settings.signalr.endpoint_count || 21;
        if (!window.settings.signalr.endpoint) {
            var n = Math.floor(Math.random() * window.settings.signalr.endpoint_count);
            window.settings.signalr.endpoint = window.settings.signalr.endpoint_pattern.replace("{0}", n);
        }
        console.log("[RealTimeHub] Using: " + window.settings.signalr.endpoint);
        $.connection.hub = $.hubConnection(window.settings.signalr.endpoint, { useDefaultPath: false });
        $.extend($.connection, $.connection.hub.createHubProxies());


        self._connection = $.connection;

        //Wait until we're actionally loaded
        waitUntil(function () {
            return typeof ($.connection) !== "undefined" && typeof ($.connection.realTimeHub) !== "undefined" /*&& typeof ($.connection.hub.id) !== "undefined"*/;
        }, function () {
            console.log("[RealTimeHub] Loaded.");
            updateMethods = true;

            if (self.isDisabled) {
                console.log("[RealTimeHub] Not starting. Disabled.");
                return;
            }


            //Copy server methods in the the hub
            for (var i in self.server) {
                if (Object.prototype.hasOwnProperty.call(self.server, i)) {
                    self.hub.server[i] = self.server[i];
                }
            }
            self.server = $.connection.realTimeHub.server;

            var performAfterStart = function (func, startIfUnstarted) {
                if (typeof (func) !== "function")
                    console.warn("[RealTimeHub] Expected a function. Found: " + q)

                if (typeof (startIfUnstarted) === "undefined")
                    startIfUnstarted = true;

                if (startIfUnstarted && !started && !starting)
                    self.start();

                if (!started) {
                    startQueue.push(func);
                } else {
                    if (!self.isDisconnected || func.ignoreDisconnect) {
                        func();
                    } else {
                        startQueue.push(func);
                    }
                }
            };

            var executeStartQueue = function () {
                for (var i = 0; i < startQueue.length; i++) {
                    try {
                        var q = startQueue[i];
                        if (typeof (q) === "function") {
                            q();
                        } else {
                            console.error("Expected function in queue. Found" + q);
                        }
                    } catch (e) {
                        console.error("[RealTimeHub] Error in start queue.", e);
                    }
                }
                startQueue = [];
            };

            var joinFallbackTimeout;
            var backoff = 0;
            var allowReconnectAttempt = true;
            var reconnectStartTime = +new Date();
            //replace the start method.
            self.start = function (callback, errorCallback) {
                if (started) {
                    console.warn("[RealTimeHub] Already started.");
                    if (typeof (errorCallback) !== "undefined") {
                        errorCallback({
                            error: "already-started",
                            reconnecting: tryingToReconnect,
                            connectionId: self.connectionId,
                            realTime: self,
                            disconnected: self.isDisconnected,
                        });
                    }
                    return;
                }
                if (starting) {
                    console.warn("[RealTimeHub] Already starting.");
                    //debugger;
                    if (typeof (errorCallback) !== "undefined") {
                        errorCallback({
                            error: "already-started",
                            reconnecting: tryingToReconnect,
                            connectionId: self.connectionId,
                            realTime: self,
                            disconnected: self.isDisconnected,
                        });
                    }
                    return;
                }
                starting = true;

                //reconnection variables

                var startHub = function (successCallback, errorCallback) {
                    if (self.isDisabled) {
                        console.log("[RealTimeHub] Not starting. Disabled.");
                        return;
                    }


                    var startSettings = $.extend({ xdomain: true }, Constants.StartHubSettings);
                    $.connection.hub.start(startSettings).done(function () {
                        console.log("[RealTimeHub] " + (tryingToReconnect ? "Restarted" : "Started"));
                        //reinitialize
                        backoff = 0;

                        //set variables
                        self.connectionId = $.connection.hub.id;
                        self.id = $.connection.hub.id;
                        console.log("[RealTimeHub] Adding Id:" + self.connectionId);

                        started = true;
                        starting = false;
                        tryingToReconnect = false;

                        executeStartQueue();
                        if (typeof (callback) !== "undefined") {
                            callback();
                        }

                        joinFallbackTimeout = setTimeout(function () {
                            //ensure that we join the default hubs, even if we don't explictly call it.
                            try {
                                console.info("[RealTimeHub] Connection state:" + $.connection.hub.state);
                            } catch (e) {
                                console.error("[RealTimeHub] Error:", e);
                            }

                            if (privateVariables.joined == false) {
                                if (self.shouldAutoJoin) {
                                    console.warn("[RealTimeHub] Fallback: you didn't explicitly call RealTimeHub.join(). I'm calling it for you! [" + (+new Date()) + "]");
                                    self.join({ fallback: true });
                                } else {
                                    console.info("[RealTimeHub] Skipping Autojoin.");
                                }
                            }

                        }, 120);

                        if (typeof (successCallback) === "function") {
                            successCallback();
                        }
                    }).fail(function (a) {
                        if (typeof (errorCallback) === "function") {
                            errorCallback(a);
                        }
                    });
                };
                startHub();

                //Reconnection...
                var reconnectFunc = function () {

                    if (self.isDisabled) {
                        console.log("[RealTimeHub] Not reconnecting. Disabled.");
                        return;
                    }


                    if (backoff < 10) {
                        //See if it worked after exponential backoff
                        var timeoutDuration = 3000 + Math.pow(1.5, backoff) * 1000;
                        if (backoff == 0) {
                            timeoutDuration = 0;
                        }
                        if (backoff == 1) {
                            if (tryingToReconnect == true) {
                                showAlert("Connection lost. Reconnecting.", 20000);
                            }
                        }
                        var blockTimeout = false;

                        setTimeout(function () {
                            if (tryingToReconnect == true) {
                                if (blockTimeout)
                                    clearTimeout(blockTimeout);
                                blockTimeout = setTimeout(function () {
                                    $("#overlay-offline-container").css("display", "block");
                                    clearTimeout(blockTimeout);
                                }, 200)
                                backoff += 1;
                                console.log("[RealTimeHub] Reconnecting. (Attempt: " + backoff + ")");

                                //check status and maybe try again...
                                console.info("[RealTimeHub] (10) joined=false");
                                privateVariables.joined = false;

                                var successCallback = function () {
                                    backoff = 0;
                                    function joinSuccessCallback() {
                                        clearAlerts();
                                        clearTimeout(blockTimeout);
                                        $("#overlay-offline-container").css("display", "none");
                                        clearAlerts();
                                        var dur = new Date() - reconnectStartTime;
                                        if (dur > (Constants.SilentReconnectDuration || 600)) {
                                            showAlert("Reconnected!", "alert-success", "Success", 3000);
                                            console.info("[RealTimeHub] Reconnected after:" + dur);
                                        } else {
                                            console.info("[RealTimeHub] Silently reconnected.");
                                        }
                                    }
                                    self.rejoin(joinSuccessCallback);
                                }


                                //Try to start the hub.
                                startHub(successCallback, function (error) {
                                    if (error == "already-started" || error == "already-starting") {
                                        successCallback();
                                        $("#overlay-offline-container").css("display", "none");
                                    }
                                    clearAlerts();
                                });

                                //Hey! Reconnect called automatically by disconnect. No need to call it here.
                            }
                        }, timeoutDuration);
                    } else {
                        allowReconnectAttempt = false;
                        clearAlerts();
                        showAlert("Could not connect. Please check your internet connection and refresh the page.", 3600000);
                        console.error("Backoff count exceeded 10. Error message shown.")
                    }
                };

                var reconnect = Debounce(reconnectFunc, 3000, true);

                $.connection.hub.reconnecting(function () {
                    reconnectStartTime = new Date();
                    console.info("[RealTimeHub] Reconnecting...");
                    tryingToReconnect = true;
                });

                $.connection.hub.reconnected(function () {
                    //not sure when/if this is called
                    console.info("[RealTimeHub] Reconnected.");
                    reconnect();
                });

                $(window).bind("beforeunload", function () {
                    console.info("[RealTimeHub] Detected unload. Not trying to reconnect...");
                    allowReconnectAttempt = false;
                });

                var disconnectFunc = function () {
                    console.info("[RealTimeHub] disconnected()");
                    starting = false;
                    started = false;
                    this.isDisconnected = true;
                    try {
                        if ($.connection.hub.lastError) {
                            console.error("[RealTimeHub] Reason: " + $.connection.hub.lastError.message);
                        } else {
                            console.info("[RealTimeHub] Disconnected. No errors.");
                        }
                    } catch (e) {
                        console.error(e);
                    }

                    if (allowReconnectAttempt) {
                        tryingToReconnect = true;
						if (!self.isDisabled) {
							$("#overlay-offline-container").css("display", "block");
						}
                        //showAlert("Disconnected. Trying to reconnect.", 3600000);
                        reconnectStartTime = +new Date();
                        reconnect();
                    } else {
                        console.info("[RealTimeHub] disconnect fallback - queued");
                        setTimeout(function () {
                            console.info("[RealTimeHub] disconnect fallback - executing");
                            showAlert("Disconnected.", 3600000);
                        }, 10000);
                    }
                };

                $.connection.hub.disconnected(Debounce(disconnectFunc, 6000, true));

                function slowConnection() {
                    var key = "RealTimeSlow1";
                    var value = Cookies.get(key);
                    if (value === undefined) {
                        Cookies.set(key, "" + (+new Date()), { expires: 3 });
                        showAlert("Your connection appears slow. This may cause issues.<br/>Please try refreshing.", 8000);
                    } else {
                        var diffMs = (+new Date()) - (+value);
                        //less than 5 minutes...
                        if (diffMs < 5 * 60 * 1000) {
                            var key = "RealTimeSlow2";
                            if (Cookies.get(key) === undefined) {
                                Cookies.set(key, 'true', { expires: 90 });
                                showAlert("Your connection still appears slow.<br/> Let us know if this continues.", 8000);
                            }
                        }
                    }
                }
                $.connection.hub.connectionSlow(slowConnection);
            };

            var allJoinOptions = [];

            var rejoinBackOff = 0;
            self.join = function (options, successCallback, failCallback) {

                if (self.isDisabled) {
                    console.log("[RealTimeHub] Not joining. Disabled.");
                    return;
                }

                options = options || {};
                if (!options.rejoining) {
                    allJoinOptions.push(options);
                }

                //deregister fallback.
                privateVariables.joined = true;
                clearTimeout(joinFallbackTimeout);

                var joinMessage = "[RealTimeHub] Joining: ";
                if (options && options.fallback == true) {
                    joinMessage = "[RealTimeHub] Joining via fallback: ";
                }
                //create the join function
                var func = function () {
                    options.connectionId = $.connection.hub.id;
                    console.log(joinMessage, options, "[" + (+new Date()) + "]");
                    //Perform Join
                    $.connection.realTimeHub.server.join(options).done(function (e) {
                        rejoinBackOff = 0;
                        //Has error
                        if (e && e.Error) {
                            console.error("[RealTimeHub] Error: RealTimeHub.join " + e.Message);
                            if (e.Refresh) {
                                try {
                                    showAlert(e.Message, 4000);
                                    setTimeout(function () {
                                        window.location.reload();
                                    }, 2000);
                                    return;
                                } catch (e) {
                                    console.error(e);
                                }
                            }

                            if (typeof (failCallback) === "function") {
                                failCallback(e);
                            }
                            return;
                        }
                        //No error
                        console.log("[RealTimeHub] Success: RealTimeHub.join");
                        self.isDisconnected = false;
                        executeStartQueue();
                        if (typeof (successCallback) === "function") {
                            successCallback(e);
                        }

                    }).fail(function (d) {
                        //Join Failed
                        if (typeof (failCallback) === "function") {
                            console.error("[RealTimeHub] Failed: RealTimeHub.join ", d);
                            failCallback(d);
                        } else {
                            rejoinBackOff += 1;
                            if (rejoinBackOff < 30) {
                                setTimeout(function () {
                                    console.error("[RealTimeHub] Failed: RealTimeHub.join [" + rejoinBackOff + "] ", d);
                                    self.rejoin();
                                }, 1000);
                            } else {
                                showAlert("Could not connect");
                            }
                        }
                    });
                };

                func.ignoreDisconnect = true;


                //wait until ready, then call the join function.
                performAfterStart(func, true);
            }

            self.rejoin = function (successCallback, failCallback) {
                var allJoinOptionsCount = allJoinOptions.length;
                var allSuccess = true;
                var executedCallback = false;
                function afterAll(isFail) {
                    return function () {
                        //debugger;
                        if (isFail) {
                            allSuccess = false;
                        }
                        allJoinOptionsCount -= 1;
                        if (!executedCallback && allJoinOptionsCount == 0) {
                            if (allSuccess && typeof (successCallback) === "function") {
                                executedCallback = true;
                                successCallback();
                            }
                            if (!allSuccess && typeof (failCallback) === "function") {
                                executedCallback = true;
                                failCallback();
                            }
                        }
                    }
                }

                try {
                    for (var i = 0; i < allJoinOptions.length; i++) {
                        var options = $.extend({ rejoining: true, rejoining_idx: i }, allJoinOptions[i]);
                        options.connectionId = RealTime.connectionId;
                        self.join(options, afterAll(false), afterAll(true));
                    }
                } catch (e) {
                    console.error("fatal error rejoining", e);
                    if (!executedCallback && typeof (failCallback) === "function") {
                        failCallback()
                    }
                }
            }

            self.afterStart = function (func) {
                performAfterStart(func, false);
            };
            for (var i = 0; i < runAfterStart.length; i++) {
                var f = runAfterStart[i];
                if (typeof (f) === "function") {
                    self.afterStart(f);
                } else {
                    console.error("[RealTimeHub] Expecting function");
                }
            }
            runAfterStart = [];


            //Perform all initialization functions..
            for (var i = 0; i < self.afterInitialize.length; i++) {
                self.afterInitialize[i](self);
            }
        }, function () {
            console.error("[RealTimeHub] Failed to connect to hub.");
        });
    }, 2);
};
RealTime.start();

$(function () {
    RealTime.client.forceRefresh = function (minutes, message) {
        if (typeof (minutes) === "undefined")
            minutes = 5;
        minutes = Math.max(0, minutes);
        var seconds = minutes * 60 * Math.random();
        console.log("Forcing refresh in " + (Math.round(seconds + 30)) + " seconds.");
        setTimeout(function () {
            if (typeof (message) === "string") {
                showAlert(message, 30000);
                console.log("Refresh in 30 seconds.");
                setTimeout(function () {
                    location.reload(true);
                }, 30000);
            } else {
                showAlert("Refreshing page. Please wait.", 60000);
                setTimeout(function () {
                    location.reload(true);
                }, 2500);
            }
        }, seconds * 1000);
    };

    RealTime.client.logoff = function () {
        console.log("Logging off..");

        showAlert("Logging out...", 5000);
        setTimeout(function () {
            location.reload(true);
        }, 5000);

    };
    RealTime.client.logoff = function () {
        console.log("Logging off..");

        showAlert("Logging out...", 5000);
        setTimeout(function () {
            location.reload(true);
        }, 5000);
    };

    RealTime.client.fileCompleted = function (model) {
        var id = model.Id;
        var name = model.FileName
        var download = model.OutputMethod == "Download";
        var save = model.OutputMethod == "Save";
        var print = model.OutputMethod == "Print";

        if (!save && !download && !print) {
            save = true;
        }


        console.log("File received: " + id + " = " + name);
        if (typeof (id) === "undefined") {
            console.error("file id was undefined");
            return;
        }

        if (typeof (name) === "undefined") {
            name = "File";
        }
        download = download || false;
        var message = name + " completed.";
        var duration = download ? 5000 : 120000;
        var url = "/documents/open/" + id;
        if (save) {
            message += " <a href='" + url + "' onclick='clearAlerts()'>(<u>Download</u>)</a>";
        }

        if (save || download) {
            clearAlerts();
            showAlert(message, duration);
        }
        if (download) {
            setTimeout(function () {
                $.fileDownload(url);
            }, 500);
        }
        if (print) {
            setTimeout(function () {
                var w = window.open(url);
                w.print();
            }, 500);
        }
    };


    RealTime.client.notification = function (model) {
        console.log("Notification", model);
        Notifications.closeDropdown();
        Notifications.add(model);
    };

    RealTime.client.clearNotifications = function () {
        console.log("Clear Notifications");
        Notifications.clear();
    };

    RealTime.client.clearNotification = function (id) {
        console.log("Clear Notification - " + id);
        Notifications.remove(id);
    };

    RealTime.client.unclearNotification = function (id) {
        console.log("Unclear Notification - " + id);
        Notifications.add(id);
    };

})

