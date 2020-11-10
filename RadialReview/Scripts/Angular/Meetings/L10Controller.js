angular.module('L10App').controller('L10Controller', ['$scope', '$http', '$timeout', '$location',
	'radial', 'meetingDataUrlBase'/*, 'dateFormat'*/, 'recurrenceId', "meetingCallback", "$compile", "$sce", "$q", "$window", "$filter",
	function ($scope, $http, $timeout, $location, radial, meetingDataUrlBase, recurrenceId, meetingCallback, $compile, $sce, $q, $window, $filter) {

		$scope.trustAsResourceUrl = $sce.trustAsResourceUrl;
		if (recurrenceId == null)
			throw Error("recurrenceId was empty");
		$scope.disconnected = false;
		$scope.recurrenceId = recurrenceId;
		
		$scope.dateFormat = window.dateFormat || "MM-dd-yyyy";		

		function updateScorecard(data) {
			var startTime = +new Date();
			$scope.ScoreLookup = $scope.ScoreLookup || {};
			var luArr = [];
			if (data.Scorecard != null && data.Scorecard.Scores != null) {
				luArr.push(data.Scorecard.Scores);
			}

			if (typeof (data.L10Scorecards) !== "undefined") {
				for (var sc in data.L10Scorecards) {
					if (arrayHasOwnIndex(data.L10Scorecards, sc)) {
						var i = data.L10Scorecards[sc];
						if (typeof (i.Contents) !== "undefined" && typeof (i.Contents.Scores) !== "undefined") {
							luArr.push(i.Contents.Scores);
						}
					}
				}
			}

			for (var luidx in luArr) {
				if (arrayHasOwnIndex(luArr, luidx)) {
					var lu = luArr[luidx];
					for (var key in lu) {
						if (arrayHasOwnIndex(lu, key)) {
							var value = lu[key];
							if (!(value.ForWeek in $scope.ScoreLookup))
								$scope.ScoreLookup[value.ForWeek] = {};
							if (value.Measurable) {
								var foundKey = $scope.ScoreLookup[value.ForWeek][value.Measurable.Id];
								var newKey = value.Key;
								if (typeof (foundKey) !== "undefined" && foundKey.localeCompare(value.Key) > 0) {
									newKey = foundKey;
								}
								$scope.ScoreLookup[value.ForWeek][value.Measurable.Id] = newKey;
							}
						}
					}
				}
			}

			console.log("[L10Controller] Updated Scorecard: "+((+new Date)-startTime)+"ms");
		};

		var r = radial($scope, { hubs: { recurrenceIds: [recurrenceId] } });
		
		r.updater.postResolve = updateScorecard;

		$scope.functions = $scope.functions || {};
		$scope.filters = $scope.filters || {};

		if (typeof (dataDateRange) === "undefined") {
			dataDateRange = {};
		}
		if (typeof (dataDateRange.startDate) === "undefined") {
			dataDateRange.startDate = moment().add('days', 1).toDate();
		} else {
			dataDateRange.startDate = moment(dataDateRange.startDate).toDate();
		}
		if (typeof (dataDateRange.endDate) === "undefined") {
			dataDateRange.endDate = moment().add('days', 1).toDate();
		} else {
			dataDateRange.endDate = moment(dataDateRange.endDate).toDate();
		}
		$scope.model = $scope.model || {};
		$scope.model.dataDateRange = dataDateRange;
		$scope._alreadyLoaded = { startDate: dataDateRange.startDate.getTime(), endDate: dataDateRange.endDate.getTime() };
		$scope.$watch('model.dataDateRange', function (newValue, oldValue) {
			if (newValue.startDate < $scope._alreadyLoaded.startDate) {
				var range1 = {
					startDate: newValue.startDate,
					endDate: Math.min($scope._alreadyLoaded.startDate, newValue.endDate)
				};
				$scope.functions.reload(true, range1);
			}
			if (newValue.endDate > $scope._alreadyLoaded.endDate) {
				var range2 = {
					startDate: Math.max(newValue.startDate, $scope._alreadyLoaded.endDate),
					endDate: newValue.endDate
				};
				$scope.functions.reload(true, range2);
			}
			$scope._alreadyLoaded.startDate = Math.min($scope._alreadyLoaded.startDate, newValue.startDate);
			$scope._alreadyLoaded.endDate = Math.max($scope._alreadyLoaded.endDate, newValue.endDate);
		});
		var dateToNumber = function (date) {
			var type = typeof (date);
			if (type == 'number') {
				return date;
			} else if (typeof (date._d) !== 'undefined') {
				return +date;
			} else if (date.getDate !== undefined) {
				return date.getTime();
			}
			console.error("[L10Controller] Can't process:" + date);
		}

		$scope.functions.showFormula = function (id) {
			setFormula(id);
		};

		$scope.functions.startCoreProcess = function (coreProcess) {
			$http.get("/CoreProcess/Process/StartProcess/" + coreProcess.Id)
				.then(function (data) {
					$scope.functions.showAlert(data, true);
				}, function (data) {
					$scope.functions.showAlert(data);
				});
		}

		$scope.functions.adjustToMidnight = function (date) {
			//adjusts local time to end of day local time
			return Time.adjustToMidnight(date);
		}

		$scope.$watch('model.LoadUrls.length', function (newValue, oldValue) {
			if (newValue != 0 && $scope.model && $scope.model.LoadUrls && $scope.model.LoadUrls.length) {
				var urls = [];
				for (var u in $scope.model.LoadUrls) {
					if (arrayHasOwnIndex($scope.model.LoadUrls, u)) {
						if ($scope.model.LoadUrls[u].Data != null) {
							urls.push($scope.model.LoadUrls[u].Data);
						}
						$scope.model.LoadUrls[u].Data = null;
					}
				}
				$scope.model.LoadUrls = [];
				$timeout(function () {
					for (var u in urls) {
						if (arrayHasOwnIndex(urls, u)) {
							loadDataFromUrl(urls[u]);
						}
					}
				}, 10);
			}
		});

		$scope.$watch('model.Focus', function (newValue, oldValue) {
			if (newValue) {
				var setFocus = function (count) {
					if (!count)
						count = 1;
					if (count > 5) {
						$scope.model.Focus = null;
						return;
					}
					try {
						var toFocus = $($scope.model.Focus);
						console.log("[L10Controller] Setting Focus: ", toFocus);
						if (toFocus.length > 0) {
							$($scope.model.Focus).focus();
							$scope.model.Focus = null;
							return;
						} else {
							$timeout(function () {
								setFocus(count + 1);
							}, 20);
						}
					} catch (e) {
						console.error("[L10Controller] Set Focus", e);
					}
					$scope.model.Focus = null;
				};
				setFocus(1);
			}
		});

		var firstLoad = true;
		var canUseInitialValue = true;
		function loadDataFromUrl(url) {
			var stD = new Date();

			var processSuccess = function (data, status) {
				var ddr = undefined;
				if (typeof ($scope.model) !== "undefined" && typeof ($scope.model.dataDateRange) !== "undefined")
					ddr = $scope.model.dataDateRange;

				r.updater.convertDates(data);

				if (firstLoad) {
					r.updater.clearAndApply(data);
				} else {
					r.updater.applyUpdate(data);
				}

				if (typeof ($scope.model) !== "undefined" && typeof ($scope.model.dataDateRange) === "undefined")
					$scope.model.dataDateRange = ddr;

				if (meetingCallback) {
					meetingCallback();
				}

				firstLoad = false;
				$scope.isReloading = false;
				firstLoadCompleted = true;

			};

			var processError = function (a, b, c, d, e, f) {
				showAngularError(a, b, c, d, e, f);
				$scope.isReloading = false;
				firstLoadCompleted = true;
			};

			if (canUseInitialValue && $window.InitialModel) {
				console.info("[L10Controller] Using InitialModel:", $window.InitialModel);
				var initModel = $window.InitialModel
				if (typeof (initModel) === "function") {

					var sPS = function (data) {
						$scope.$apply(function () {
							processSuccess(data);
						});
					};
					var sPE = function (a, b, c, d, e, f) {
						$scope.$apply(function () {
							processError(a, b, c, d, e, f);
						});
					};
					initModel(sPS, sPE);
				} else {
					processSuccess(initModel);
				}
			} else {
				$http({ method: 'get', url: url })
					.success(processSuccess)
					.error(processError);
			}

			canUseInitialValue = false;
		}

		$scope.isReloading = false;

		var initialLoad = true;
		var firstLoadCompleted = false;
		var firstLoadStarted = false;

		$timeout(function () {
			initialLoad = false;
			console.log("[L10Controller] Load initialized");
		},2000);

		var throttledReload = angular.throttle(function (reload, range, first) {
			console.warn("[L10Controller] status", firstLoadStarted, firstLoadCompleted, initialLoad);
			

			if (firstLoadStarted == true && /*firstLoadCompleted != true &&*/ initialLoad == true) {
				console.info("[L10Controller] Still loading first request");
				return;
			}
			//console.info("Started reloading");
			firstLoadStarted = true;
			
			if ($scope.isReloading) {
				//console.log("Already reloading.");
				//console.info("Already reloading");
				//if (reload !== true) {
				//	return;
				//} else {
				//	console.info("Reloading anyway");
				//}
			}
			if (typeof (reload) === "undefined") {
				reload = true;
			}
			if (typeof (first) === "undefined") {
				firstLoad = false;
			} else {
				firstLoad = first;
			}
			if (reload) {
				$scope.isReloading = true;
				Time.tzoffset();
				console.log("[L10Controller] Reloading Data. ");
				var url = meetingDataUrlBase;
				if (meetingDataUrlBase.indexOf("{0}") != -1) {
					url = url.replace("{0}", $scope.recurrenceId);
				} else {
					console.error("[L10Controller] RecurrenceId placeholder missing from meetingDataUrl, {0}");
					//url = url + $scope.recurrenceId;
				}

				url = Time.addTimestamp(url);
				if (typeof (range) !== "undefined" && typeof (range.startDate) !== "undefined")
					url += "&start=" + dateToNumber(range.startDate);
				if (typeof (range) !== "undefined" && typeof (range.endDate) !== "undefined")
					url += "&end=" + dateToNumber(range.endDate);
				if (firstLoad)
					url += "&fullScorecard=true";
				loadDataFromUrl(url);
			}
		}, 500);




		$scope.functions.reload = function (reload, range, first) {
			//console.info("Attempting reload");
			throttledReload(reload, range, first);
		}

		$scope.functions.allHidden = function (items) {			
			for (var i = 0; i < items.length; i++) {
				if (typeof (items[i].Hide) === "undefined")
					continue;
				if (items[i].Hide != true) {
					return false;
				}
			}
			return true;
		}

		$scope.functions.numberOfGroups = function (groups) {
			//debugger;
			if (groups)
				return Object.keys(groups).length;
			return 0;
		}

		$scope.functions.orderScorecard = function (reverse) {
			return function (d) {
				if (d && d.ForWeekNumber) {
					if (reverse)
						return -d.ForWeekNumber;
					return d.ForWeekNumber
				} else {
					return 0;
				}
			};
		}

		function MeasurableOrderDict(scorecard) {
			var dict = {};
			if (scorecard && scorecard.MeasurableOrder) {
				for (var i in scorecard.MeasurableOrder) {
					var m = scorecard.MeasurableOrder[i];
					dict[m.MeasurableId] = m.Ordering;
				}
			}
			return dict;
		}

		$scope.functions.orderMeasurables = function (scorecard) {
			var dict = MeasurableOrderDict(scorecard);
			return function (a) {
				if (a.Id in dict)
					return dict[a.Id];
				if (a.Ordering)
					return a.Ordering;
				return a.Id;
			};
		}

		$scope.functions.reload(true, $scope.model.dataDateRange, true);

		$scope.functions.setHtml = function (element, data) {
			var newstuff = element.html(data);
			$compile(newstuff)($scope); // loads the angular stuff in the new markup
			$scope.$apply();
		};

		$scope.functions.setPage = function (page) {
			$http.get("/meeting/SetPage/" + $scope.model.RecurrenceId + "?page=" + page + "&connection=" + $scope.functions.getConnectionId());
			if (!$scope.model.FollowLeader || $scope.model.IsLeader) {
				$scope.model.CurrentPage = page;
			}
		};

		$scope.functions.subtractDays = function (date, days, shift) {
			var d = new Date(date);
			if (typeof (shift) === "undefined" || shift == true)
				d = new Date(moment(d).add(new Date().getTimezoneOffset(), "minutes").valueOf())
			d.setDate(d.getDate() - days);
			return d;
		};
		$scope.functions.scorecardId = function (s, measurableId, weekId) {
			if (!s)
				return "sc_" + measurableId + "_" + weekId;
			return "sc_" + s.Id;
		};
		$scope.functions.scorecardColor = function (s) {
			if (!s)
				return "";

			var v = s.Measured;
			var goal = s.Target;
			var altgoal = s.AltTarget;
			var dir = s.Direction;

			if (typeof (goal) === "undefined")
				goal = s.Measurable.Target;
			if (typeof (altgoal) === "undefined")
				altgoal = s.Measurable.AltTarget;
			if (typeof (dir) === "undefined")
				dir = s.Measurable.Direction;
			if (typeof (goal) === "undefined") {
				var item = $("[data-measurable=" + s.Measurable.Id + "][data-week=" + s.ForWeek + "]");
				goal = item.data("goal");
				if (typeof (altgoal) === "undefined")
					altgoal = item.data("alt-goal");
				console.log("[L10Controller] Goal not found, trying element. Found: " + goal);
			}

			if (!$.trim(v)) {
				return "";
			} else {
				var met = metGoal(dir, goal, v, altgoal);
				if (met == true)
					return "success";
				else if (met == false)
					return "danger";
				else
					return "error";
			}
		};

		$scope.proxyLookup = {};
		$scope.ScoreIdLookup = null;

		$scope.functions.setValue = function (keyStr, value) {
			$scope[keyStr] = value;
		}

		$scope.functions.getFcsa = function (measurable) {

			var builder = {
				resize: true,
				localization: $scope.localization
			};

			if (measurable.Modifiers == "Dollar") {
				builder = {
					prepend: "$",
					resize: true,
					localization: $scope.localization
				};
			} else if (measurable.Modifiers == "Percent") {
				builder = {
					append: "%",
					resize: true,
					localization: $scope.localization
				};
			} else if (measurable.Modifiers == "Euros") {
				builder = {
					prepend: "€",
					resize: true,
					localization: $scope.localization
				};
			} else if (measurable.Modifiers == "Pound") {
				builder = {
					prepend: "£",
					resize: true,
					localization: $scope.localization
				};
			}
			return builder;
		};

		$scope.functions.lookupScoreFull = function (week, measurableId, scorecardKey) {
			var scorecard = $scope.model.Lookup[scorecardKey];
			var scores = scorecard.Scores;
			for (var s in scores) {
				if (arrayHasOwnIndex(scores, s)) {
					var score = $scope.model.Lookup[scores[s].Key];
					if (score.ForWeek == week && score.Measurable.Id == measurableId) {
						if (!(week in $scope.ScoreLookup))
							$scope.ScoreLookup[week] = {};
						$scope.ScoreLookup[week][measurableId] = scores[s].Key;
						return scores[s].Key;
					}
				}
			}
			return null;
		};

		$scope.functions.lookupScore = function (week, measurableId, scorecardKey) {
			if ($scope.ScoreLookup == null) {
				$scope.ScoreLookup = {};
				var scorecard = $scope.model.Lookup[scorecardKey];
				for (var w in scorecard.Weeks) {
					if (arrayHasOwnIndex(scorecard.Weeks, w)) {
						var wn = scorecard.Weeks[w].ForWeekNumber;
						$scope.ScoreLookup[wn] = {};
						for (var m in scorecard.Measurables) {
							if (arrayHasOwnIndex(scorecard.Measurables, m)) {
								var mn = scorecard.Measurables[m].Id;
								$scope.ScoreLookup[wn][mn] = $scope.functions.lookupScoreFull(wn, mn, scorecardKey);
							}
						}
					}
				}
			}

			if (week in $scope.ScoreLookup && measurableId in $scope.ScoreLookup[week]) {
				var lu = $scope.model.Lookup[$scope.ScoreLookup[week][measurableId]];
				if (lu != null)
					return lu;
			}

			var wKey = week;
			if (!(wKey in $scope.proxyLookup))
				$scope.proxyLookup[wKey] = {};
			if (!(measurableId in $scope.proxyLookup[wKey])) {
				var measurable = { Id: measurableId };
				if (("AngularMeasurable_" + measurableId) in $scope.model.Lookup)
					measurable = $scope.model.Lookup["AngularMeasurable_" + measurableId];

				$scope.proxyLookup[wKey][measurableId] = {
					Id: -1, Type: "AngularScore", ForWeek: week, Measured: null,
					Measurable: measurable,
					Target: measurable.Target, // WRONG (scores generated before nearest)
					Direction: measurable.Direction
				};
			}

			return $scope.proxyLookup[wKey][measurableId];
		};
		$scope.functions.updateAssign = function (self, assigned) {
			self.Assigned = assigned || false;
		}

		$scope.functions.updateComplete = function (self) {
			//debugger;
			var instance = self.todo;
			if (!instance)
				instance = self.issue;
			if (!instance) {
				instance = self;
			}
			if (instance.Complete) {
				instance.CompleteTime = new Date();
			} else {
				instance.CompleteTime = null;
			}
		};

		$scope.possibleOwners = {};
		$scope.loadPossibleOwners = function (id) {
			if (typeof ($scope.model) !== "undefined" && typeof ($scope.model.Attendees) !== "undefined") {
				var attendes = [];
				for (var i = 0; i < $scope.model.Attendees.length; i++) {
					if ($scope.model.Attendees[i].Managing) {
						attendes.push($scope.model.Attendees[i]);
					}
				}

				$scope.possibleOwners[id] = attendes;
				$scope.possibleOwners[id];
			} else {
				if (!(id in $scope.possibleOwners)) {
					$scope.possibleOwners[id] = null;
					$http.get('/Dropdown/AngularMeetingMembers/' + id + '?userId=true').success(function (data) {
						r.updater.convertDates(data);
						$scope.possibleOwners[id] = data;
					});
				}
			}
		};
		$scope.possibleDirections = [];
		$scope.loadPossibleDirections = function () {
			return $scope.possibleDirections.length ? null : $http.get('/Dropdown/Type/lessgreater').success(function (data) {
				r.updater.convertDates(data);
				$scope.possibleDirections = data;
			});
		};

		$scope.now = moment();
		$scope.rockstates = [{ name: 'Off Track', value: 'AtRisk' }, { name: 'On Track', value: 'OnTrack' }, { name: 'Done', value: 'Complete' }];

		$scope.opts = {
			ranges: {
				'Active': [moment().add(1, 'days'), moment().add(9, 'days')],
				'Today': [moment().subtract(1, 'days'), moment().add(9, 'days')],
				'Last 7 Days': [moment().subtract(6, 'days'), moment().add(9, 'days')],
				'Last 14 Days': [moment().subtract(13, 'days'), moment().add(9, 'days')],
				'Last 30 Days': [moment().subtract(29, 'days'), moment().add(9, 'days')],
				'Last 90 Days': [moment().subtract(89, 'days'), moment().add(9, 'days')]// [sevenMin, sevenMax]
			},
			separator: '  to  ',
			showDropdowns: true,
			format: 'MMM DD, YYYY',
			opens: 'left'
		};
		$scope.filters.taskFilter = function () {
			return function (item) {
				return !(item.Hide == true || item.Complete == true);
			};
		}
		$scope.filters.byRange = function (fieldName, minValue, maxValue, forceMin, period) {
			if (minValue === undefined) minValue = -Number.MAX_VALUE;
			if (maxValue === undefined) maxValue = Number.MAX_VALUE;


			if (typeof (forceMin) !== "undefined") {
				minValue = Math.min(minValue, maxValue - forceMin * 24 * 60 * 60 * 1000);
			}

			if (typeof (period) !== "undefined") {
				if (period == "Monthly" || period == "Quarterly") {
					minValue = Math.min(minValue, maxValue - (366) * 24 * 60 * 60 * 1000);
				}
			}

			return function predicateFunc(item) {
				var d = item[fieldName];
				if (!d) return true;
				if (d instanceof Date) d = d.getTime();
				if (minValue instanceof Date) minValue = minValue.getTime();
				if (maxValue instanceof Date) maxValue = maxValue.getTime();

				if (fieldName == "ForWeek")
					d -= 7 * 24 * 60 * 60 * 1000;

				return minValue <= d && d <= maxValue || moment(d).format("MMDDYYYY") == moment(maxValue).format("MMDDYYYY");
			};
		};

		$scope.selectedTab = $location.url().replace("/", "");

		$scope.filters.completionFilterItems = [
			{ name: "Incomplete", value: { Completion: true }, short: "Incomplete" },
		];

		$scope.options = {};
		$timeout(function () {
			$scope.options.l10teamtypes = $scope.loadSelectOptions('/dropdown/type/l10teamtype');
		}, 500);

		/**
		 * You must format all dates to be server dates before entering this function
		 */
		$scope.functions.sendUpdate = function (self, args) {
			var dat = angular.copy(self);
			//Angular automatically converts dates to UTC from local time zone.
			r.updater.convertDatesForServer(dat, Time.tzoffset());
			console.warn("[L10Controller] Dates were not converted for server, please confirm");
			var builder = "";
			args = args || {};

			if (!("connectionId" in args))
				args["connectionId"] = $scope.functions.getConnectionId();

			for (var i in args) {
				if (arrayHasOwnIndex(args, i)) {
					builder += "&" + i + "=" + args[i];
				}
			}
			var url = Time.addTimestamp("/L10/Update" + self.Type) + builder;
			$http.post(url, dat).then(function () { }, showAngularError);
		};

		var updateDebounce = {};
		$scope.functions.sendUpdateDebounce = function (self, args) {
			function debounce(key, func, wait, immediate) {
				if (!(key in updateDebounce)) {
					updateDebounce[key] = null;
				}
				var timeout = updateDebounce[key];
				return function () {
					var context = this, args = arguments;
					var later = function () {
						timeout = null;
						if (!immediate) func.apply(context, args);
					};
					var callNow = immediate && !timeout;
					clearTimeout(timeout);
					updateDebounce[key] = setTimeout(later, wait);
					if (callNow) func.apply(context, args);
				};
			};
			debounce(self.Key, function () {
				console.warn("[L10Controller] Debounce sent");
				$scope.functions.sendUpdate(self, args);
			}, 300)();

		};


		var updateDebounce = {};
		$scope.functions.sendUpdateDebounce = function (self, args) {
			function debounce(key, func, wait, immediate) {
				if (!(key in updateDebounce)) {
					updateDebounce[key] = null;
				}
				var timeout = updateDebounce[key];
				return function () {
					var context = this, args = arguments;
					var later = function () {
						timeout = null;
						if (!immediate) func.apply(context, args);
					};
					var callNow = immediate && !timeout;
					clearTimeout(timeout);
					updateDebounce[key] = setTimeout(later, wait);
					if (callNow) func.apply(context, args);
				};
			};
			debounce(self.Key, function () {
				console.warn("[L10Controller] Debounce sent");
				$scope.functions.sendUpdate(self, args);
			}, 300)();
		};



		$scope.functions.checkFutureAndSend = function (self) {
			var m = self;
			var icon = { title: "Update options" };

			var values = ["None", "Dollar", "Percent", "Pound", "Euros"];
			var unitTypes = values.map(function (x) { return { text: x, value: x, checked: (x == m.Modifiers) } });

			var fields = [{
				type: "label",
				value: "Update historical goals?"
			}, {
				name: "history",
				value: "false",
				type: "checkbox"
			}, {///Cumulative
				type: "label",
				value: "Show Cumulative?"
			}, {
				name: "showCumulative",
				value: self.ShowCumulative || false,
				type: "checkbox",
				onchange: function () {
					var show = $(this).is(":checked") != true;
					$("#cumulativeRange").toggleClass("hidden", show);
				}
			}, {
				classes: self.ShowCumulative == true ? "" : "hidden",
				name: "cumulativeRange",
				value: self.CumulativeRange || new Date(),
				type: "date"
			}, {///Average
				type: "label",
				value: "Show Average?"
			}, {
				name: "showAverage",
				value: self.ShowAverage || false,
				type: "checkbox",
				//type: "yesno"
				onchange: function () {
					var show = $(this).is(":checked") != true;
					$("#averageRange").toggleClass("hidden", show);
				}
			}, {
				classes: self.ShowAverage == true ? "" : "hidden",
				name: "averageRange",
				value: self.AverageRange || new Date(),
				type: "date"
			}, {
				type: "label",
				value: "Unit type?"
			}, {
				name: "unitType",
				value: self.UnitType,
				type: "select",
				options: unitTypes,
			}]

			if (self.Direction == "Between" || self.Direction == -3) {
				icon = "info";
				fields.push({
					type: "number",
					text: "Lower-Boundary",
					name: "Lower",
					value: self.Target,
				});
				fields.push({
					type: "number",
					text: "Upper-Boundary",
					name: "Upper",
					value: self.AltTarget || self.Target,
				});
			}

			$scope.functions.showModal("Edit measurable", "/scorecard/editmeasurable/" + self.Id, "/L10/UpdateAngularMeasurable");
		}

		$scope.functions.removeMeasurableRow = function (event, self, hideImmediately) {
			var row = self;
			var rid = "";
			if ($scope.model && $scope.model.Id) {
				rid = "?recurrenceId=" + $scope.model.Id;
			}

			$scope.functions.showModal("Archive Measurable", "/scorecard/archivemeasurable/" + self.Id + rid, "/scorecard/archivemeasurable/", null, null, function (data) {
				//debugger;
			});
		};

		$scope.functions.removeRow = function (event, self, hideImmediately) {
			var dat = angular.copy(self);
			var _clientTimestamp = new Date().getTime();
			var origArchive = self.Archived;
			self.Archived = true;
			if (hideImmediately) {
				self.Hide = true;
			}
			$(".editable-wrap").remove();
			var url = Time.addTimestamp("/L10/Remove" + self.Type + "/?recurrenceId=" + $scope.recurrenceId + "&connectionId=" + $scope.functions.getConnectionId());
			$http.post(url, dat).error(function (data) {
				showJsonAlert(data, false, true);
				if (data.Revert != false) {
					self.Archived = origArchive;
					if (hideImmediately) {
						self.Hide = false;
					}
				}
			}).finally(function () {
				// reload
				if (self.Type != "AngularRock" && self.Type != "AngularMeasurable" && self.Type != "AngularTodo")
					$scope.functions.reload(true, $scope.model.dataDateRange, false);
			});
		};

		$scope.functions.unarchiveRow = function (event, self) {
			var dat = angular.copy(self);
			var _clientTimestamp = new Date().getTime();
			var origArchive = self.Archived;
			self.Archived = false;
			$(".editable-wrap").remove();

			var url = Time.addTimestamp("/L10/Unarchive" + self.Type + "/?recurrenceId=" + $scope.recurrenceId + "&connectionId=" + $scope.functions.getConnectionId());

			$http.post(url, dat).error(function (data) {
				showJsonAlert(data, false, true);
				self.Archived = origArchive;
			}).finally(function () {
				// reload
				if (self.Type != "AngularRock")
					$scope.functions.reload(true, $scope.model.dataDateRange, false);
			});
		};

		$scope.functions.addRow = function (event, type, args) {
			if (!$(event.target).hasClass("disabled")) {
				var _clientTimestamp = new Date().getTime();
				var controller = angular.element($("[ng-controller]"));
				controller.addClass("loading");
				$(event.target).addClass("disabled");

				if (typeof (args) === "undefined")
					args = "";

				var url = Time.addTimestamp("/L10/Add" + type + "/" + $scope.recurrenceId + "?connectionId=" + $scope.functions.getConnectionId());

				$http.get(url + args)
					.error(showAngularError)
					.finally(function () {
						controller.removeClass("loading");
						$(event.target).removeClass("disabled");
					});
			}
		};

		$scope.functions.checkAllNotifications = function () {
			var items = $scope.model.Notifications;
			if (items) {
				for (var i in items) {
					if (arrayHasOwnIndex(items, i)) {
						var item = items[i];
						item.Seen = true;
						$scope.functions.sendUpdate(item);
					}
				}
			}
		};

		$scope.ShowSearch = false;
		$scope.functions.showUserSearch = function (event) {
			$scope.functions.showModal("Add Attendee", "/L10/AddAttendee?meetingId=" + $scope.recurrenceId, "/L10/AddAttendee", null, function (d) {
				if (d.SelectPage == "false") {
					required = [];
					if (d["Object.FirstName"] == "")
						required.push("First name");
					if (d["Object.LastName"] == "")
						required.push("Last name");
					if (d["Object.Email"] == "" && d["Object.PlaceholderOnly"]=="False")
						required.push("Email");

					err = "";
					if (required.length == 1) {
						return required[0] + " is required.";
					} else if (required.length > 1) {
						for (var i = 0; i < required.length - 1; i++) {
							var r = required[i];
							if (i > 0)
								r = r.toLowerCase();
							err += r + ", ";
						}
						err += " and " + required[i].toLowerCase() + " are required.";
						//debugger;
						return err;
					}
					return true;
				}
			});
		};
		$scope.functions.showMeasurableSearch = function (event) {
			$scope.functions.showModal("Add Measurable", "/L10/AddMeasurableModal?meetingId=" + $scope.recurrenceId, "/L10/AddMeasurableModal");
			//$scope.functions.showModal("Add Measurable", "/Scorecard/CreateMeasurable?recurrenceId=" + $scope.recurrenceId, "/Scorecard/CreateMeasurable");

		};
		$scope.functions.showRockSearch = function (event) {
			$scope.functions.showModal("Add Rock", "/L10/AddRockModal?meetingId=" + $scope.recurrenceId, "/L10/AddRockModal", null, function (x) {
				if (x.SelectPage == "false" && x["Object.Title"] == "") {
					return "Title required";
				}
			});
		};

		$scope.functions.addAttendee = function (selected) {
			var event = { target: $(".user-list-container") };
			$scope.functions.addRow(event, "AngularUser", "&userid=" + selected.item.id);
		}

		$scope.functions.createUser = function () {
			$timeout(function () {
				$scope.functions.showModal('Add managed user', '/User/AddModal', '/nexus/AddManagedUserToOrganization?meeting=' + $scope.recurrenceId + "&refresh=false");
			}, 1);
		}

		$scope.functions.goto = function (url) {
			$window.location.href = url;
		}

		$scope.functions.blurSearch = function (self, noHide) {
			angular.element(".searchresultspopup").addClass("ng-hide");
			self.visible = false;
			$scope.ShowSearch = false;
			$scope.model.Search = '';
		}

		$scope.userSearchCallback = function (params) {
			var defer = $q.defer();
			var attendees = $scope.model.Attendees || [];
			var ids = $.map(attendees, function (item) {
				return item.Id;
			})
			$http.get("/User/Search?q=" + params + "&exclude=" + ids)
				.then(function (response) {
					if (!response.data || !response.data.Object) {
						defer.resolve([]);
					}
					defer.resolve(response.data.Object);
				})
				.catch(function (e) {
					defer.reject(e);
				});

			return defer.promise;
		};

		$scope.functions.setHash = function (value) {
			$timeout(function () {
				$window.location.hash = value;
			}, 1);
		};

		$scope.functions.uploadUsers = function () {
			$window.location.href = "/upload/l10/Users?recurrence=" + $scope.recurrenceId;
		};

		$scope.scorecardSortListener = {
			accept: function (sourceItemHandleScope, destSortableScope) {
				return true;
			},
			orderChanged: function (event) {
				var mid = $scope.recurrenceId;
				if (mid <= 0)
					mid = event.source.itemScope.measurable.RecurrenceId;


				//Adj order
				var ordered = $scope.model.Scorecard.Measurables.slice().sort(function (a, b) { return a.Ordering - b.Ordering; })
				var adjArr = [];
				var adj = 0;
				for (var i = 0; i < ordered.length; i++) {
					var o = ordered[i];
					adjArr.push(adj);
					if (o.Id < 0 && !o.IsDivider)
						adj += 1;
				}

				var dat = {
					id: event.source.itemScope.measurable.Id,
					recurrence: mid,
					oldOrder: event.source.index - adjArr[event.source.index],
					newOrder: event.dest.index - adjArr[event.dest.index],
				}
				var dict = MeasurableOrderDict($scope.model.Scorecard);

				console.info("[L10Controller] MOVING " + event.source.index + "(" + event.source.itemScope.measurable.Id + ") to " + (event.dest.index));
				if (event.dest.index < event.source.index) {
					console.info("[L10Controller] (up)");
					for (var idx in $scope.model.Scorecard.MeasurableOrder) {
						var item = $scope.model.Scorecard.MeasurableOrder[idx];
						if (item.MeasurableId == event.source.itemScope.measurable.Id) {
							console.info("[L10Controller] Setting " + item.Ordering + "(" + item.MeasurableId + ") to " + (event.dest.index));
							item.Ordering = event.dest.index;
							break;
						}
					}
					//Up
					for (var idx in $scope.model.Scorecard.MeasurableOrder) {
						var item = $scope.model.Scorecard.MeasurableOrder[idx];
						if (event.dest.index <= item.Ordering && item.Ordering < event.source.index && item.MeasurableId != event.source.itemScope.measurable.Id) {
							console.info("[L10Controller] Incrementing " + item.Ordering + "(" + item.MeasurableId + ") to " + (item.Ordering + 1));
							item.Ordering += 1;
						} else if (item.MeasurableId != event.source.itemScope.measurable.Id) {
							console.info("[L10Controller] No change " + item.Ordering);
						}
					}
					
				} else if (event.dest.index > event.source.index) {
					console.info("[L10Controller] (down)");
					//Move down
					
					for (var idx in $scope.model.Scorecard.MeasurableOrder) {
						var item = $scope.model.Scorecard.MeasurableOrder[idx];
						if (event.source.index < item.Ordering && item.Ordering <= event.dest.index) {
							console.info("[L10Controller] Decrementing " + item.Ordering + "(" + item.MeasurableId + ") to " + (item.Ordering - 1));
							item.Ordering -= 1;
						} else if (item.MeasurableId != event.source.itemScope.measurable.Id) {
							console.info("[L10Controller] No change " + item.Ordering );
						}
					}
					for (var idx in $scope.model.Scorecard.MeasurableOrder) {
						var item = $scope.model.Scorecard.MeasurableOrder[idx];
						if (item.MeasurableId == event.source.itemScope.measurable.Id) {
							console.info("[L10Controller] Setting " + item.Ordering + "(" + item.MeasurableId + ") to " + (event.dest.index));
							item.Ordering = event.dest.index;
							break;
						}
					}
					
				}

				var url = Time.addTimestamp("/L10/OrderAngularMeasurable");

				$http.post(url, dat).then(function () { }, showAngularError);
			},
			clone: false,//optional param for clone feature.
			allowDuplicates: false, //optional param allows duplicates to be dropped.
		};

		function decideOnDate(week, selector) {

			var forWeek = new Date(70, 0, 4);
			forWeek.setDate(forWeek.getDate() + 7 * (week.ForWeekNumber - 1));
			forWeek = new Date(+forWeek - 1);

			var dat = $scope.functions.startOfWeek(forWeek, selector.ScorecardWeekDay);

			if (selector.Period == "Monthly" || selector.Period == "Quarterly") {
				dat = new Date(70, 0, 4);
				dat.setDate(dat.getDate() + 7 * (week.ForWeekNumber - 1));
			}
			return dat;
		}

		$scope.functions.topDate = function (week, selector) {
			var dat = decideOnDate(week, selector);
			var date = $scope.functions.subtractDays(dat, 0, false);
			return $filter('date')(date, selector.DateFormat1);
		};
		$scope.functions.bottomDate = function (week, selector) {
			var dat = decideOnDate(week, selector);
			var date = $scope.functions.subtractDays(dat, -6, false);
			if (selector.Period == "Monthly" || selector.Period == "Quarterly") {
				date = $scope.functions.subtractDays(dat, 0, false);
			}
			return $filter('date')(date, selector.DateFormat2);
		};

		$scope.functions.startOfWeek = function (date, startOfWeek) {
			var getWeekNumber = moment(date).weekday();
			var diff = getWeekNumber - dayOfWeekAsInteger(startOfWeek);
			if (diff < 0) {
				diff += 7;
			}

			var date_new = $scope.functions.subtractDays(date, 1 * diff, false);
			return date_new;
		}

		function dayOfWeekAsInteger(day) {
			return ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"].indexOf(day);
		}

	}
]);



angular.module('L10App').directive('fixHeadWidth', ["$interval", function ($interval) {
	return {
		link: function (scope, element, attr) {
			var token = $interval(function () {
				var parent = element.closest("[fix-head-parent]");
				var loc = parent.find("[fix-head-location]");
				var w = Math.max(loc.width(), element.width());
				if (w > 0) {
					$interval.cancel(token);
					loc.width(w + 3+6);
				}
			}, 100);
		}
	};
}]);

(function (angular) {
	var throttle = function (func, wait, options) {
		options || (options = {});
		var context, args, result;
		var timeout = null;
		var previous = 0;
		var later = function () {
			previous = options.leading === false ? 0 : (new Date().getTime());
			timeout = null;
			result = func.apply(context, args);
			context = args = null;
		};
		return function () {
			var now = (new Date().getTime());
			if (!previous && options.leading === false) {
				previous = now;
			}
			var remaining = wait - (now - previous);
			context = this;
			args = arguments;
			if (remaining <= 0) {
				clearTimeout(timeout);
				timeout = null;
				previous = now;
				result = func.apply(context, args);
				context = args = null;
			} else if (!timeout && options.trailing !== false) {
				timeout = setTimeout(later, remaining);
			}
			return result;
		};
	};
	angular.extend(angular, { throttle: throttle });

}(angular));


angular.module('L10App').directive('fixHeadScroller', ["$timeout", function ($timeout) {
	return {
		link: function (scope, element, attr) {
			var firstGen = true;
			var parent = element.closest("[fix-head-parent]");
			//debugger;
			var throttleScroll1 = angular.throttle(function (evt) {
				var f = parent.find("[fix-head-location]");
				if (f.length) {
					var sTop = this.scrollTop;
					var sLeft = this.scrollLeft;
					var type = this.getAttribute("fix-head-scroller");
					
					var g = {};
					if (type == "left")
						g.left = -sLeft;
					if (type == "top")
						g.top = sTop;
					f.css(g);
				}
			}, 100);

			parent.bind('scroll', throttleScroll1);

			element.bind('scroll', function (evt) {
				if (firstGen) {
					$timeout(function () {
						try {
							var leftScroller = parent.parent().find("[fix-head-scroller='left']")[0];
							if (leftScroller) {
								var left =  leftScroller.scrollLeft;

								parent.find("[fix-head]").each(function (a, b) {
									var e = $(b);
									var p = e.position();
									var c = e.clone();
									c.removeAttr("fix-head");
									c.attr("fix-head-clone", true);
									
									c.css({
										position: "absolute",
										left: p.left,// + left,
										heigth: e.height(),
										width: e.width(),
										fontFamily: e.css("fontFamily"),
										fontSize: e.css("fontSize"),
										color: e.css("color"),
										backgroundColor: e.css("backgroundColor"),
										fontWeight: e.css("fontWeight"),
										top: p.top,
									});

									c.data("top", p.top);

									var parent = e.closest("[fix-head-parent]");
									var f = parent.find("[fix-head-location]");
									f.append(c);


								});
							}
						} catch (e) {
							console.warn(e);
						}
					}, 1);
					firstGen = false;
				}

				var f = parent.find("[fix-head-location]");
				if (f.length) {
					var sTop = this.scrollTop;
					//var sRight = this.scrollWidth - this.scrollLeft;
					var sRight = this.scrollWidth - $(this).width() - this.scrollLeft;
					var type = this.getAttribute("fix-head-scroller");
					angular.throttle(function () {
						var g = {};
						if (type == "left")
							g.right = -sRight;
						if (type == "top")
							g.top = sTop;
						f.css(g);
					}, 1500)();
				}

			});
		}
	};
}]);

angular.module('L10App').directive('fixHead', ["$timeout", function ($timeout) {
	return {
		link: function (scope, element, attr) {
			$timeout(function () {
			}, 100);
		}
	};
}]);

