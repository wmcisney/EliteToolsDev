angular.module("tree", []).directive("tree", ["$timeout", "$window", function ($timeout, $window) {
	return {
		templateNamespace: 'svg',
		restrict: "A",
		scope: {
			graph: "=graph", ttEnter: "=?ttEnter", ttUpdate: "=?ttUpdate", ttExit: "=?ttExit", ttWatch: "=?ttWatch", ttPostExit: "=?ttPostExit",
			ttDragStart: "=?ttDragStart", ttDragEnd: "=?ttDragEnd",
			ttCollapse: "=?ttCollapse", ttExpand: "=?ttExpand",
			compact: "=?ttCompact"
		},
		//transclude: true,
		replace: true,
		template: '<g><g class="pz-zoom"> <rect class="pz-rect" width="100%" height="100%" style="width:100%;height:100%;fill:transparent;"></rect>' +
			'<g class="pz-pan"><g class="tt-tree"></g></g>' +
			'</g></g>',
		controller: ["$element", "$scope", "$rootScope", "$timeout", "$q", function ($element, $scope, $rootScope, $timeout, $q) {

			var svg = $element.closest("svg");
			try {
				var filter = d3.select(svg[0]).append("filter");

				filter.attr("id", "glow")
					.attr("width", "10px")
					.attr("height", "10px")
					.attr("x", -.75)
					.attr("y", -.75);
				filter.append("feFlood")
					.attr("result", "flood")
					.attr("flood-color", "#7d726c")
					.attr("flood-opacity", "1");
				filter.append("feComposite")
					.attr("in", "flood")
					.attr("result", "mask")
					.attr("in2", "SourceGraphic")
					.attr("operator", "in");
				filter.append("feMorphology")
					.attr("in", "mask")
					.attr("result", "dilated")
					.attr("radius", "2")
					.attr("operator", "dilate");
				filter.append("feGaussianBlur")
					.attr("in", "dilated")
					.attr("result", "blurred")
					.attr("stdDeviation", "5");
				var femerge = filter.append("feMerge");
				femerge.append("feMergeNode").attr("in", "blurred");
				femerge.append("feMergeNode").attr("in", "SourceGraphic");

				var filter = d3.select(svg[0]).append("filter");
				filter.attr("id", "biggerGlow")
					.attr("width", "10px")
					.attr("height", "10px")
					.attr("x", -.75)
					.attr("y", -.75);
				filter.append("feFlood")
					.attr("result", "flood")
					.attr("flood-color", "#7d726c")
					.attr("flood-opacity", ".75");

				filter.append("feComposite")
					.attr("in", "flood")
					.attr("result", "mask")
					.attr("in2", "SourceGraphic")
					.attr("operator", "in");
				filter.append("feMorphology")
					.attr("in", "mask")
					.attr("result", "dilated")
					.attr("radius", "5")
					.attr("operator", "dilate");
				filter.append("feGaussianBlur")
					.attr("in", "dilated")
					.attr("result", "blurred")
					.attr("stdDeviation", "5");
				var femerge = filter.append("feMerge");
				femerge.append("feMergeNode").attr("in", "blurred");
				femerge.append("feMergeNode").attr("in", "SourceGraphic");

				var filter = d3.select(svg[0]).append("filter");
				filter.attr("id", "orangeGlow")
					.attr("width", "10px")
					.attr("height", "10px")
					.attr("x", -.75)
					.attr("y", -.75);
				filter.append("feFlood")
					.attr("result", "flood")
					.attr("flood-color", "#005ed7")
					.attr("flood-opacity", ".5");

				filter.append("feComposite")
					.attr("in", "flood")
					.attr("result", "mask")
					.attr("in2", "SourceGraphic")
					.attr("operator", "in");
				filter.append("feMorphology")
					.attr("in", "mask")
					.attr("result", "dilated")
					.attr("radius", "2")
					.attr("operator", "dilate");
				filter.append("feGaussianBlur")
					.attr("in", "dilated")
					.attr("result", "blurred")
					.attr("stdDeviation", "5");
				var femerge = filter.append("feMerge");
				femerge.append("feMergeNode").attr("in", "blurred");
				femerge.append("feMergeNode").attr("in", "SourceGraphic");

			} catch (e) {
				console.error(e);
			}

			$scope.duration = 0;//250;

			function getNode(source) {
				if (typeof (source) === "string") {
					source = +source;
				}
				if (typeof (source) === "number") {
					return d3.select("[data-id='" + source + "']").datum();
				}
				if (typeof (source) === "object") {
					return source;
				}
				return false;
			}

			$scope.getNode = getNode;

			function getNodeId(source) {
				if (source == null || typeof(source)==="undefined") {
					return false;
				}
				if (typeof (source) === "string") {
					return source = +source;
				}
				if (typeof (source) === "number") {
					return source
				}

				if (typeof (source) === "object") {
					return source.Id;
				}
				return false;
			}
			$scope.expandAll = function () {
				var dive = function (d) {
					if (d && d._children) {
						d.children = d._children;
						d._children = null;
					}
					if (d.children) {
						for (var i in d.children)
							if (arrayHasOwnIndex(d.children, i))
								dive(d.children[i]);
					}
				}
				var d = $scope.root;
				dive(d);
				/*$timeout(function () {
					$scope.centerNode(d);
				}, 20);*/
			};

			$scope.setLevel = function (n) {
				n = n || 0;
                var dive = function (d, depth) {
                    d.depth = depth || 0;
					if (d && d.children && n <= d.depth) {
						d._children = d.children;
						d.children = null;
					}
					if (d && d._children && n > d.depth) {
						d.children = d._children;
						d._children = null;
					}
					if (d.children) {
						for (var i in d.children)
							if (arrayHasOwnIndex(d.children, i))
                                dive(d.children[i], d.depth + 1);
					}

				}
				var d = $scope.root;
				dive(d);
				$timeout(function () {
					$scope.centerNode(d);
				}, 20);
			};

			$scope.collapseAll = function (n) {
				n = n || 0;
				var dive = function (d, depth) {
					if (d.children) {
						for (var i in d.children)
							if (arrayHasOwnIndex(d.children, i))
								dive(d.children[i]);
					}
					if (d && d.children && n <= d.depth) {
						d._children = d.children;
						d.children = null;
					}
				}
				var d = $scope.root;
				dive(d);
				$timeout(function () {
					$scope.centerNode(d);
				}, 20);
			};

			$scope._collapseNode = function (d) {
				if (d && d.children) {
					if (d._children) {
						//Somehow we have children in both. Handle gracefully.
						d.children = d.children.concat(d._children);
					}
					d._children = d.children;
					//d._children.forEach($scope.collapse);
					d.children = null;
				}
			}

			$scope._expandNode = function (d) {
				if (d && d._children) {
					if (d.children) {
						//Somehow we have children in both. Handle gracefully.
						d._children = d._children.concat(d.children);
					}
					d.children = d._children;
					d._children = null;
				}
			}

			$scope.expand = function (d) {
				d = getNode(d);
				$scope.centerNode(d, null, 400);
				$scope._expandNode(d);
			};
			$scope.collapse = function (d) {
				d = getNode(d);
				$scope.centerNode(d);
				$scope._collapseNode(d);
			};
			$scope.toggle = function (d) {
				d = getNode(d);
				if (d && d._children) {
					$scope.expand(d);
				} else if (d && d.children) {
					$scope.collapse(d);
				}
			};

			$scope.centerRootNode = function (ms, animate, offset) {
				$scope.centerNode($scope.root, ms, animate, offset);
			}

			$scope.centerNode = function (source, ms, animate, offset) {
				if (typeof (ms) === "undefined")
					ms = 200;

				offset = offset || $scope.defaultOffset || {};
				offset.x = offset.x || 0;
				offset.y = offset.y || 0;

				$scope.showNode(source);
				return $q(function (resolve, reject) {
					$timeout(function () {
						try {
							var scale = 1;
							var pz3;
							var pz = $element.find('.pz-pan');
							if (pz) {
								var panzoom = pz.scope().panzoom;
								if (panzoom)
									scale = panzoom.scale();
							}
							source = getNode(source);
							//if (typeof (source) === "number") {
							//	source = d3.select("[data-id='" + source + "']").datum();
							//}
							if (source) {
								console.log("[Tree] CenterNode called " + source.Id);
								x = -source.x0;
								y = -source.y0;
								var runZoomed = scale < 1;
								//addAdj = 0;
								//try {
								//	addAdjX = source.width;
								//	addAdjY = source.height;
								//} catch (e) {
								//	console.error(e);
								//}


								scale = Math.max(1, scale);
								x = (x) * scale + (svg.width()) / 2 + offset.x;
								y = (y) * scale + (svg.height()) / 3 + offset.y;

								if (animate) {
									$timeout(function () {
										d3.select(pz[0]).transition().duration(animate).ease("quad-in-out").attr("transform", "translate(" + x + "," + y + ")scale(" + scale + ")");
									}, runZoomed ? 100 : 100);
								} else {
									d3.select(pz[0]).attr("transform", "translate(" + x + "," + y + ")scale(" + scale + ")");
								}
								if (panzoom) {
									panzoom.scale(scale);
									panzoom.translate([x, y]);
									$timeout(function () {
										resolve();
									}, /*$scope.duration ||*/ 250);
									if (runZoomed)
										$scope.panzoom.zoomOptimization({
											translate: [x, y],
											scale
										}, false);
								} else {
									reject("No panzoom");
								}
							} else {
								reject("Element not found");
							}
						} catch (e) {
							// debugger;
							console.info("[Tree] Could not center:", e);
						}
					}, ms);
				});

			}

			$scope.showNode = function (source) {
				var id = getNodeId(source);
				console.log("[Tree] ShowNode called " + id);
				//http://bl.ocks.org/jjzieve/a743242f46321491a950
				function searchTree(obj, id, path) {
					if (obj.Id === id) { //if search is found return, add the object to the path and return it
						//path.push(obj);
						return path;
					}
					else if (obj.children || obj._children) { //if children are collapsed d3 object will have them instantiated as _children
						var children = (obj.children) ? obj.children : obj._children;
						for (var i = 0; i < children.length; i++) {
							path.push(obj);// we assume this path is the right one
							var found = searchTree(children[i], id, path);
							if (found) {// we were right, this should return the bubbled-up path from the first if statement
								return found;
							}
							else {//we were wrong, remove this parent from the path and continue iterating
								path.pop();
							}
						}
					}
					else {//not the right object, return false so it will continue to iterate in the loop
						return false;
					}
				}
				function openPaths(paths) {
					for (var i = 0; i < paths.length; i++) {
						if (paths[i].Id !== "1") {//i.e. not root
							paths[i].class = 'found';

							if (paths[i]._children) { //if children are hidden: open them, otherwise: don't do anything
								//paths[i].children = paths[i]._children;
								//paths[i]._children = null;
								$scope._expandNode(paths[i]);
								console.warn("[Tree] Untested!")
								$scope.updater(paths[i], 0);
							}
							//debouncedUpdate(paths[i],0);
						}
					}
				}


				var paths = searchTree($scope.root, id, []);
				if (typeof (paths) !== "undefined") {
					openPaths(paths);
				}
			}

			$scope.selectNode = function (id) {
				console.log("[Tree] SelectNode called " + id);
				$scope.showNode(id)
				$timeout(function () {
					d3.select(".selected").classed("selected", false).attr("filter", null);
					var all = d3.selectAll("[data-id='" + id + "']").classed("selected", true).attr("filter", "url(#glow)");
					setTimeout(function () {
						$scope.centerNode(id).then(function () {
							setTimeout(function () {
								all.attr("filter", null);
							}, 500);
						}, console.error);
					},/* $scope.duration ||*/ 250);

				}, 1);
			}

			$scope.swap = function (nodeId, parentId) {
				console.log("[Tree] Swap called on" + nodeId + "=>" + parentId);
				var draggingNode = null;
				var selectedNode = null;
				var nodes = $scope.tree.nodes($scope.root);
				for (var nid in nodes) {
					if (arrayHasOwnIndex(nodes, nid)) {
						var n = nodes[nid];
						if (n.Id == nodeId)
							draggingNode = n;
						if (n.Id == parentId)
							selectedNode = n;
					}
				}
				if (draggingNode == null)
					throw "Node not found";
				if (selectedNode == null)
					throw "Parent not found";

				var index = draggingNode.parent.children.indexOf(draggingNode);
				if (index > -1) {
					draggingNode.parent.children.splice(index, 1);
				}
				if (Array.isArray(selectedNode.children) || Array.isArray(selectedNode._children)) {
					if (Array.isArray(selectedNode.children)) {
						selectedNode.children.push(draggingNode);
					} else {
						selectedNode._children.push(draggingNode);
					}
				} else {
					selectedNode.children = [];
					selectedNode.children.push(draggingNode);
				}
			}

			$rootScope.$on("CenterNode", function (event, source, ms, animate, offset) {
				//setTimeout(function () {
				return $scope.centerNode(source, ms, animate, offset);
				//}, 1);
			});
			$rootScope.$on("CenterRootNode", function (event, ms, animate, offset) {
                return $scope.centerRootNode(ms, animate, offset);
			});
			$rootScope.$on("ShowNode", function (event, id) {
				$scope.showNode(id);
			});
			$rootScope.$on("SelectNode", function (event, id) {
				$scope.selectNode(id);
			});
			$rootScope.$on("SwapNode", function (event, id, parent) {
				$scope.swap(id, parent);
			});

			$rootScope.$on("ExpandNode", function (event, id) {
				$scope.expand(id);
			});
			$rootScope.$on("CollapseNode", function (event, id) {
				$scope.collapse(id);
			});
			$rootScope.$on("ExpandAllNodes", function (event) {
				$scope.expandAll();
			});
			$rootScope.$on("CollapseAllNodes", function (event, n) {
				$scope.collapseAll(n);
			});
			$rootScope.$on("SetLevel", function (event, n) {
				$scope.setLevel(n);
			});
			$rootScope.$on("ToggleNode", function (event, id) {
				$scope.toggle(id);
			});
			$rootScope.$on("RefreshTree", function (event, id) {
				$scope.updater($scope.root);
			});
			$rootScope.$on("SetDefaultOffset", function (event, offset) {
				$scope.defaultOffset = offset;
			});


		}],
		link: function (scope, element, attr, ctrl, transclude) {

			scope.lastZoom = 1;

			function debounce(func, wait, immediate) {
				var timeout;
				return function () {
					var context = this, args = arguments;
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

			var debouncedUpdate = debounce(function () {
				scope.updater(scope.root, 0);
			}, 500, false);


			var fallback = false;
			if (msieversion()) {
				var v = msieversion();
				if (v >= 11) {
					fallback = true; //Edge
				} else {
					fallback = true; //IE
				}
			}

			var svg = element.closest("svg");
			////BEGIN PAN ZOOM 

			var viewerWidth = 1000;
			var viewerHeight = 1000;

			svg.attr("height", "100%");
			svg.attr("width", "100%");
			//svg.attr("viewBox", "0 0 " + viewerWidth + " " + viewerHeight);
			var pzZoom = element.find(".pz-zoom");
			var pzZoomRect = element.find(".pz-zoom rect");
			var pzPan = pzZoom.find(".pz-pan");
			var canZoom = true;

			function zoomOptimization(event, callUpdate) {
				event = event || d3.event;
				var curScale = event.scale;
				var zoomHider = .65;
				try {
					var zoomHider = event.scale < 1 / $window.devicePixelRatio; //.65
				} catch (e) {

				}
				if (!$window.preventOffscreenHiding && (!$window.preventPanOffscreenHiding || curScale != scope.lastZoom)) {
					try {
						var offscreen = function (el) {
							var rect = el.getBoundingClientRect();
							return {
								offscreen: ((rect.x + rect.width) < 0 || (rect.y + rect.height) < 0 || (rect.x > window.innerWidth || rect.y > window.innerHeight)),
								rect: rect
							};
						};
						try {
							if (curScale != scope.lastZoom) {
								console.log("[Tree] Zoom:" + curScale);
							}

							$("body").toggleClass("zoomed-out", zoomHider);
							$("svg g.node").each(function () {
								var node = this;

								if (!$window.preventPanOffscreenHiding) {
									var foundPan = $(node).find("foreignObject");
									if (foundPan.length) {
										var calc = offscreen(node);
										if (calc.offscreen) {
											if (foundPan[0].style.display != "none") {
												foundPan[0].style.display = "none";
												node.style.oldWidth = node.style.width;
												node.style.oldHeight = node.style.height;
												node.style.width = calc.rect.width / curScale + "px";
												node.style.height = calc.rect.height / curScale + "px";
											}
										} else {
											if (foundPan[0].style.display != "") {
												foundPan[0].style.display = "";
												debugger;
												node.style.width = node.style.oldWidth;
												node.style.height = node.style.oldHeight;
											}
										}
									}
								}

								if (curScale != scope.lastZoom) {
									var containers = $(node).find(".zoom-hide-container");
									containers.each(function () {
										var foundZoomContainer = this;
										var calc = offscreen(foundZoomContainer);

										if (zoomHider) {
											if (!foundZoomContainer.style.isZoomHidden) {
												foundZoomContainer.style.oldPosition = this.style.position;
												foundZoomContainer.style.oldWidth = this.style.width;
												foundZoomContainer.style.oldHeight = this.style.height;
												foundZoomContainer.style.width = calc.rect.width /*/ curScale*/ + "px";
												foundZoomContainer.style.height = (calc.rect.height+6.4) /*/ curScale */ + "px";
												foundZoomContainer.style.position = "relative";
												foundZoomContainer.style.isZoomHidden = true;
											}
										} else {
											if (foundZoomContainer.style.isZoomHidden) {
												this.style.width = this.style.oldWidth;
												this.style.height = this.style.oldHeight;
												this.style.position = this.style.oldPosition;
												foundZoomContainer.style.isZoomHidden = false;
											}
										}
										$(foundZoomContainer).find(".zoom-hide").each(function () {
											var foundZoom = this;
											if (zoomHider) {
												if (foundZoom.style.display != "none") {
													foundZoom.style.display = "none";
												}
											} else {
												if (foundZoom.style.display != "") {
													foundZoom.style.display = "";
												}
											}
										});
									});
								}
							});
						} catch (e) {
							console.info("[Tree] Pan-zoom speedup error", e);
						}
						if (callUpdate != false) {
							debouncedUpdate();
						}
					} catch (e) {
						console.error(e);
					}
				}
				scope.lastZoom = event.scale;
			};

			function zoomed(event) {
				event = event || d3.event;

				if (!event) {
					console.log("[Tree] Zoomed called without event");
					return;
				}

				if (canZoom) {
					pzPan.attr("transform", "translate(" + event.translate + ")scale(" + event.scale + ")");
					zoomOptimization(event);
				}
			}

			scope.panzoom = d3.behavior.zoom().scaleExtent([.001, 10]).on("zoom", zoomed);
			//debugger;


			pzZoomRect.on("dblclick.zoom", null);

			scope.panzoom.zoomed = zoomed;
			scope.panzoom.zoomOptimization = zoomOptimization;

			d3.select(pzZoom[0]).call(scope.panzoom);
			////END PAN ZOOM

			var minHeight = 20;
			var minWidth = 20;

			var i = 0,
				duration = scope.duration;

			if (fallback) {
				duration = 0;
			}

			scope.root = scope.root || {};

			var vSeparation = 40;
			var hSeparation = 25;//25;
			var height = scope.graph.height;
			var width = scope.graph.width;

			var baseWidth = -100;// 200;//250;
			var baseHeight = 50;

			var panBoundary = 1;
			var tree = d3.layout.compactTree().sort(function (a, b) {
				if (b && a) {
					var diff = a.order - b.order;
					if (diff == 0)
						return a.Id - b.Id;
					return diff;
				}
				return 0;
			}).nodeSize([baseWidth, baseHeight]);
			scope.tree = tree;

			var drawLine = function (points) {
				var builder = "";
				if (points.length > 0) {
					builder += "M";
					builder += points[0].x + "," + points[0].y + "L";

					for (var i = 1; i < points.length; i++) {
						builder += points[i].x + "," + points[i].y + " ";
					}
				}
				return builder.trim();
			}

			var diagonal = function (d) {
				var sx = d.source.x, sy = d.source.y;
				var tx = d.target.x, ty = d.target.y;
				var my = ty - vSeparation / 4;
				if (d.target.isLeaf) {
					var tw = d.target.width;
					var lx;
					if (d.target.side == "left")
						lx = tx - tw / 2 - hSeparation / 2;
					else
						lx = tx + tw / 2 + hSeparation / 2;

					var tyy = ty + Math.min(30, d.target.height / 2);
					var points = [];
					if (!d.source.isLeaf) {
						points.push({ x: sx, y: sy });
						points.push({ x: sx, y: my });
						points.push({ x: lx, y: my });
					} else {
						var ax = sx;// - d.source.width / 2;
						var ay = (sy - d.source.height) + Math.min(30, d.source.height / 2);//d.source.height / 2;
						points.push({ x: ax, y: ay });
						points.push({ x: lx, y: ay });
					}
					points.push({ x: lx, y: tyy });
					points.push({ x: tx, y: tyy });

					return drawLine(points);
				} else {
					var points = [
						{ x: sx, y: sy },
						{ x: sx, y: my },
						{ x: tx, y: my },
						{ x: tx, y: ty },
					];
					return drawLine(points);
				}
			};

			var treeElement = element.find(".tt-tree");
			var self = d3.select(treeElement[0]);
			var selfSvg = d3.select(treeElement[0]);

			var expandSource = scope.root;



			function adjWidth(d) {
				if ($window.OverrideNodeWidth) {
					//debugger;
					return $window.OverrideNodeWidth;
				}
				return d.width;
			}


			scope.updater = function (source, n) {
				if (typeof (n) === "undefined")
					n = 2;

				//if (n == 0)
				//	console.trace();

				//Compress Tree
				tree.compactify(scope.compact);
				// Compute the new tree layout.
				var nodes = tree.nodes(scope.root),
					links = tree.links(nodes);
				// Normalize for fixed-depth.
				var maxHeightRow = {};
				maxHeightRow[-1] = 0;
				var maxWidth = baseWidth;
				var rowWidth = {};
				var maxDepth = 0;
				nodes.forEach(function (d) {
					maxHeightRow[d.depth] = Math.max((d.height || minHeight), maxHeightRow[d.depth] || 0);
					maxDepth = Math.max(maxDepth, d.depth);
					rowWidth[d.depth] = rowWidth[d.depth] || [];
					rowWidth[d.depth].push({
						ox: d.x,
						w: adjWidth(d)
					});
					maxWidth = Math.max(maxWidth, adjWidth(d));
				});
				for (var i = 1; i <= maxDepth; i++) {
					maxHeightRow[i] = maxHeightRow[i] + maxHeightRow[i - 1];
					rowWidth[i].sort(function (a, b) {
						return a.ox - b.ox;
					});
				}
				nodes.forEach(function (d) {
					d.y = d.depth * vSeparation + maxHeightRow[d.depth - 1];
					var shift = 0;
					for (var i = 0; i < rowWidth[d.depth].length; i++) {
						var ww = (rowWidth[d.depth][i].w - baseWidth + hSeparation);
						// shift+=ww;
						if (rowWidth[d.depth][i].ox == d.x) {
							break;
						}
						shift += ww;
					}
					d.ddx = shift;
				});

				// Update the nodes�
				var node = self.selectAll("g.node")
					.data(nodes, function (d) {
						return (d.Id);//|| (d.Id = ++i));
					});

				// Enter any new nodes at the parent's previous position.
				var nodeEnter = node.enter().append("g")
					.call(scope.dragListener || function () { })
					.call(dragListener)
					.attr("class", "node")
					.style("opacity", -10)
					.attr("transform", function (d) {
						var es = expandSource;
						if (!es || !es.x0 || !es.y0)
							es = source;
						return "translate(" + (es.x0) + "," + es.y0 + ")";
					}).classed("root-node", function (d) {
						return d.Id == scope.root.Id;
					}).classed("no-edit", function (d) {
						return d.Editable == false;
					}).classed("is-me", function (d) {
						return d.Me == true;
					}).on("click", expandCollapse).on("keydown", function (d) {
						if (d3.event.keyCode == 13 || d3.event.keyCode == 32)
							expandCollapse(d);
					});
				if (scope.ttEnter) {
					scope.ttEnter(nodeEnter);

				} else {
					nodeEnter.append("rect").attr("width", 20).attr("height", 20).attr("x", -10).attr("y", 0);
				}

				var ghost = nodeEnter.append("g").attr('class', 'ghost acc-fallback-ignore');
				ghost.append("rect").on("mouseover", overCircle).on("mouseout", outCircle);
				ghost.append("text").text("").on("mouseover", overCircle).on("mouseout", outCircle);


				// Transition nodes to their new position.

				d3.selectAll(".acc-fallback-ignore").classed("acc-fallback-hidden", true);
				console.info("[Tree] Re-execute " + n);
				//if (n == 0) {
				//	console.trace();
				//}
				var nodeUpdate = node
					.attr("data-id", function (d) { return d.Id })
					.attr("data-height", function (d) {
						var maxH = 0; var maxW = 0;
						var maxDepth = 2;
						var dive = function (parent, depth) {
							if ($(parent).css("display") == "none")
								return;
							if ($(parent).is("md-autocomplete"))
								return;

							var bb = { width: 0, height: 0 };
							if (fallback && !$(parent).hasClass("acc-fallback-ignore")) {
								bb = $(parent)[0].getBoundingClientRect();
							}

							//if ($(parent).is(".acc-contents")) {
							//	//console.log($(parent), $(parent).outerHeight());
							//	//debugger;
							//}
							if (!$(parent).is("foreignObject")) {

								var oh = Math.max($(parent).outerHeight(), bb.height / scope.panzoom.scale());
								var ow = Math.max($(parent).outerWidth(), bb.width / scope.panzoom.scale());

								var last = false;
								if (maxH < oh && maxW < ow)
									last = true;

								if (ow > 0)
									maxH = Math.max(maxH, oh);
								if (oh > 0)
									maxW = Math.max(maxW, ow);
							}
							if (depth >= 0 || fallback) {
								$(parent).children().each(function () {
									dive(this, depth - 1);
								});
							}

						}

						dive(this, maxDepth);
						d.height = maxH;
						d.width = maxW;

						return d.height;
					})
					.attr("data-width", function (d) {
						return adjWidth(d);
					})
					.classed("collapsed", function (d) {
						return d._children && d._children.length;
					})
					.style("opacity", 1)
					.attr("transform", function (d) {
						var adj = 0;
						return "translate(" + (d.x - adjWidth(d) / 2 + adj) + "," + d.y + ")";
					});

				d3.selectAll(".acc-fallback-ignore").classed("acc-fallback-hidden", false);
				var padding = 18;

				nodeUpdate.select(".ghost rect")
					.attr("width", function (d) { return adjWidth(d) + padding; })
					.attr("height", function (d) { return d.height + padding; })
					.attr("rx", 15).attr("ry", 15)
					.attr("transform", function (d) { return "translate(" + (-padding / 2) + "," + (-padding / 2) + ")"; });
				//.attr("transform", function (d) { return "translate(" + (adjWidth(d) / 2 - .25) + "," + (d.height + 15.5) + ")"; });

				if (scope.ttUpdate) {
					scope.ttUpdate(node);
				}

				// Transition exiting nodes to the parent's new position.
				var nodeExit = node.exit();

				var afterExit = nodeExit.attr("transform", function (d) { return "translate(" + source.x + "," + source.y + ")"; })
					.style("opacity", -10);

				if (scope.ttExit)
					scope.ttExit(afterExit);
				afterExit.remove();

				// Update the links�
				var link = self.selectAll("path.link")
					.data(links, function (d) { return d.target.Id; });

				// Enter any new links at the parent's previous position.
				link.enter().insert("path", "g")
					.attr("class", "link")
					.style("opacity", 0)
					.classed("root-node-link", function (d) {
						if (d.source && scope.root)
							return d.source.Id == scope.root.Id;
						return false;
					});

				// Transition links to their new position.
				link.attr("d", function (d) {
					var s = d.source;
					var t = d.target;
					var o = {
						source: { x: s.x, y: s.y + (s.height || minHeight), isLeaf: (s._compact && s._compact.isLeaf), width: s.width, height: s.height, side: (s._compact && s._compact.side) },
						target: { x: t.x, y: t.y, isLeaf: (t._compact && t._compact.isLeaf), width: t.width, height: t.height, side: (t._compact && t._compact.side) }
					};
					return diagonal(o);
				}).style("opacity", 1);

				// Transition exiting nodes to the parent's new position.
				link.exit()
					.attr("d", function (d) {
						var o = { x: source.x, y: source.y };
						return diagonal({ source: o, target: o });
					})
					.style("opacity", 0)
					.remove();

				// Stash the old positions for transition.
				nodes.forEach(function (d) {
					d.x0 = d.x;
					d.y0 = d.y;
				});

				var maxWidth = baseWidth;
				nodes.forEach(function (d) {
					maxWidth = Math.max(maxWidth, adjWidth(d));
				});

				tree.nodeSize([maxWidth + hSeparation, baseHeight]);

				if (n > 0) {
					$timeout(function () {
						scope.updater(scope.root, n - 1);

						if (n - 1 == 0 && !scope.initialCenter) {
							console.log("[Tree] Centering first time");
							scope.initialCenter = true;
							$timeout(function () {
                                scope.centerRootNode();
                                if ($('g[tree]').length > 0) {
                                    $('g[tree]').show();
                                }
                                
							}, $window.treeDuration || 0);
						}
					}, 50);
				}
			}

			var nestWatch = function (node) {
				if (node) {
					var children = null;
					if (node.children) {
						var flatChilds = (node.children);
						var dict = {};
						for (var i in flatChilds) {
							if (arrayHasOwnIndex(flatChilds, i)) {
								dict[flatChilds[i].Key] = nestWatch(flatChilds[i]);
							}
						}
						children = dict;
					}
					var _children = null;
					if (node._children) {
						var flatChilds = (node._children);
						var dict = {};
						for (var i in flatChilds) {
							if (arrayHasOwnIndex(flatChilds, i)) {
								dict[flatChilds[i].Key] = nestWatch(flatChilds[i]);
							}
						}
						_children = dict;
					}
					var nr = {};
					if (scope.ttWatch)
						nr = scope.ttWatch(node);
					nr.children = children;
					nr._children = _children;
					nr.order = node.order;
					if (node._children || !node.children)
						nr.collapsed = true;
					else
						nr.collapsed = false;
					return nr;

				} else {
					return undefined;
				}
			};

			var newValUndefined = false;
			scope.$watch(function () {
				if (scope.graph) {
					var r = null;
					var cn = null;
					var sn = null;
					var en = null;
					if (scope.graph.data) {
						r = scope.graph.data.Root;
						cn = scope.graph.data.CenterNode;
						en = scope.graph.data.ExpandNode;
						sn = scope.graph.data.ShowNode;
					}
					var n = nestWatch(r);
					return {
						compact: scope.compact,
						nest: n,
						center: cn,
						expand: en,
						show: sn
					};
				}
				return undefined;
			}, function (newVal, oldVal) {
				console.log("[Tree] Update called");
				if (typeof (newVal) === "undefined") {
					newValUndefined = true;
				} else if (newValUndefined) {
					$timeout(function () { scope.updater(scope.root, 1); }, 100);
				}

				if (scope.graph && scope.graph.data && scope.graph.data.Root) {
					scope.root = scope.graph.data.Root;
					scope.root.x = svg.width() / 2;
					scope.root.y = 0;
					scope.root.x0 = svg.width() / 2;
					scope.root.y0 = 0;

					//$timeout(function () {
					//console.log(JSON.stringify(newVal,null,2));
					//}, 100);

					if (newVal && (!oldVal || newVal.center !== oldVal.center))
						scope.centerNode(newVal.center, 200);

					if (newVal && newVal.expand && (!oldVal || newVal.expand !== oldVal.expand)) {
						scope.expand(newVal.expand, 200);
					} else {
						if (newVal && newVal.expand) {
							scope._expandNode(scope.getNode(newVal.expand));
						}
					}

					if (newVal && newVal.show && (!oldVal || newVal.show !== oldVal.show))
						scope.showNode(newVal.show);

					scope.updater(scope.root);
					//console.log("Timeout for AccountabilityChartRenderComplete");
					$timeout(function () {
						try {
							console.log("Sending AccountabilityChartRenderComplete");
							parent.console.log("Sending(p) AccountabilityChartRenderComplete");
							$('body').trigger('AccountabilityChartRenderComplete');
							parent.$('body').trigger('AccountabilityChartRenderComplete');
							// debugger;
						} catch (e) {
							console.error("could not send complete event.")
						}

					}, 1);
					//Clear out Expander Node
					try {
						if (scope.graph.data.ExpandNode) {
							scope.graph.data.ExpandNode = null;
						}
					} catch (e) {
						console.error("[Tree] Error ExpandNode:", e)
					}
					//Clear out Center Node
					try {
						if (scope.graph.data.CenterNode) {
							scope.graph.data.CenterNode = null;
						}
					} catch (e) {
						console.error("[Tree] Error CenterNode:", e)
					}
					//Clear out Show Node
					try {
						if (scope.graph.data.ShowNode) {
							scope.graph.data.ShowNode = null;
						}
					} catch (e) {
						console.error("[Tree] Error ShowNode:", e)
					}

				}
			}, true);
			// Toggle children on click.
			function expandCollapse(d) {
				//scope.lastZoom = undefined;
				expandSource = d;
				if (d.children) {
					if (typeof (scope.ttCollapse) === "function") {
						try {
							scope.ttCollapse(d);
						} catch (e) {
							console.info("[Tree] Error in collapse function.", e);
							return;
						}
					}
					scope._collapseNode(d);
					//debugger;
					//d._children = d.children;
					//d.children = null;
				} else {
					if (typeof (scope.ttExpand) === "function") {
						try {
							scope.ttExpand(d);
						} catch (e) {
							console.info("[Tree] Error in expand function.", e);
							return;
						}
					}
					scope._expandNode(d);
					//debugger;
					//d.children = d._children;
					//d._children = null;
				}
				scope.updater(d);
				scope.centerNode(d);
			}

			var svgGroup = self;
			var root = scope.root;
			var selectedNode = null;
			var panTimer = true;
			var dragStarted = false;
			var domNode = null;
			var panSpeed = 200;
			var panBoundary = 20; // Within 20px from edges will pan when dragging.
			var draggingNode = null;

			var updateTempConnector = function () {
				var data = [];
				if (draggingNode !== null && selectedNode !== null) {
					// have to flip the source coordinates since we did this for the existing connectors on the original tree
					data = [{
						source: {
							x: selectedNode.x0,
							y: selectedNode.y0 + selectedNode.height
						},
						target: {
							x: draggingNode.x0,
							y: draggingNode.y0 + 50
						}
					}];
				}
				var link = svgGroup.selectAll(".templink").data(data);

				link.enter().append("path")
					.attr("class", "templink")
					.attr("d", d3.svg.diagonal())
					.attr('pointer-events', 'none');

				link.attr("d", d3.svg.diagonal());

				link.exit().remove();
			};
			function pan(domNode, direction) {
				var speed = panSpeed;
				var pz = element.find('.pz-pan');
				var svgGroup = d3.select(pz[0]);
				var zoomListener = pz.scope().panzoom;
				if (panTimer) {
					clearTimeout(panTimer);
					console.log("clear2");
					translateCoords = d3.transform(svgGroup.attr("transform"));
					if (direction == 'left' || direction == 'right') {
						translateX = direction == 'left' ? translateCoords.translate[0] + speed : translateCoords.translate[0] - speed;
						translateY = translateCoords.translate[1];
					} else if (direction == 'up' || direction == 'down') {
						translateX = translateCoords.translate[0];
						translateY = direction == 'up' ? translateCoords.translate[1] + speed : translateCoords.translate[1] - speed;
					}
					scaleX = translateCoords.scale[0];
					scaleY = translateCoords.scale[1];
					scale = zoomListener.scale();
					svgGroup//.transition()
						.attr("transform", "translate(" + translateX + "," + translateY + ")scale(" + scale + ")");
					d3.select(domNode).select('g.node').attr("transform", "translate(" + translateX + "," + translateY + ")");
					zoomListener.scale(zoomListener.scale());
					zoomListener.translate([translateX, translateY]);
					panTimer = setTimeout(function () {
						pan(domNode,/* speed*10, */direction);
						console.log("shift " + direction);
					}, 50);
				}
			}

			var overCircle = function (d) {
				selectedNode = d;
				updateTempConnector();
			};
			var outCircle = function (d) {
				selectedNode = null;
				updateTempConnector();
			};


			function initiateDrag(d, domNode) {
				expandSource = d;
				console.log("init drag");
				draggingNode = d;
				d3.select(domNode).select('.ghost').attr('pointer-events', 'none');
				d3.selectAll('.ghost').attr('class', 'ghost showing acc-fallback-ignore');
				d3.select(domNode).classed('activeDrag', true).attr("filter", "url(#glow)");


				svgGroup.selectAll("g.node").sort(function (a, b) { // select the parent and sort the path's
					if (a.Id != draggingNode.Id) return -1; // a is not the hovered element, send "a" to the back
					else return 1; // a is the hovered element, bring "a" to the front
				});
				// if nodes has children, remove the links and nodes
				if (nodes.length > 1) {
					// remove link paths
					links = tree.links(nodes);
					nodePaths = svgGroup.selectAll("path.link")
						.data(links, function (d) {
							return d.target.Id;
						}).remove();
					// remove child nodes
					nodesExit = svgGroup.selectAll("g.node")
						.data(nodes, function (d) {
							return d.Id;
						}).filter(function (d, i) {
							if (d.Id == draggingNode.Id) {
								return false;
							}
							return true;
						}).remove();
				}

				// remove parent link
				parentLink = tree.links(tree.nodes(draggingNode.parent));
				svgGroup.selectAll('path.link').filter(function (d, i) {
					if (d.target.Id == draggingNode.Id) {
						return true;
					}
					return false;
				}).remove();

				dragStarted = null;
			};
			// Define the drag listeners for drag/drop behaviour of nodes.

			var ox, oy;
			var skipDrag = false;
			dragListener = d3.behavior.drag()
				.on("dragstart", function (d) {
					//debugger;
					skipDrag = false;
					if (d == scope.root)
						skipDrag = true;

					if (skipDrag)
						return;
					if (typeof (scope.ttDragStart) === "function") {
						try {
							scope.ttDragStart(d);
						} catch (e) {
							skipDrag = true;
							return;
						}
					}
					if (skipDrag)
						return;
					selfSvg.classed("is-dragging", true);

					dragStarted = true;
					nodes = tree.nodes(d);
					ox = adjWidth(d) / 2;
					oy = -50;

					console.log(ox, oy);
					d3.event.sourceEvent.stopPropagation();
					// it's important that we suppress the mouseover event on the node being dragged. Otherwise it will absorb the mouseover event and the underlying node will not detect it d3.select(this).attr('pointer-events', 'none');
				})
				.on("drag", function (d) {
					if (skipDrag)
						return;
					if (dragStarted) {
						domNode = this;
						initiateDrag(d, domNode);
					}
					var pz = element.find('.pz-pan');

					// get coords of mouseEvent relative to svg container to allow for panning
					relCoords = d3.mouse(svg.get(0));
					// console.log(relCoords);

					if (relCoords[0] < panBoundary) {
						panTimer = true;
						pan(this, 'left');
					} else if (relCoords[0] > (svg.width() - panBoundary)) {
						panTimer = true;
						pan(this, 'right');
					} else if (relCoords[1] < panBoundary) {
						panTimer = true;
						pan(this, 'up');
					} else if (relCoords[1] > (svg.height() - panBoundary)) {
						panTimer = true;
						pan(this, 'down');
					} else {
						try {
							clearTimeout(panTimer);
							// console.log("clear1");
						} catch (e) {
						}
					}

					d.x0 += d3.event.dx;
					d.y0 += d3.event.dy;
					var node = d3.select(this);
					node.attr("transform", "translate(" + (d.x0 - ox) + "," + (d.y0 - oy) + ")");
					updateTempConnector();
				})
				.on("dragend", function (d) {
					selfSvg.classed("is-dragging", false);
					panTimer = null;
					if (skipDrag) {
						return;
					}
					domNode = this;
					var dnid = null;
					if (draggingNode)
						dnid = draggingNode.Id;

					var dat = {
						oldParentId: null,
						newParentId: null,
						id: dnid,
						swap: false
					};
					if (selectedNode) {
						dat.swap = true;
						// now remove the element from the parent, and insert it into the new elements children
						dat.oldParentId = draggingNode.parent.Id;
						scope.swap(draggingNode.Id, selectedNode.Id);
						dat.newParentId = selectedNode.Id;
						// Make sure that the node being added to is expanded so user can see added node is correctly moved
						scope.expand(selectedNode);
						endDrag();
					} else {
						endDrag();
					}
					if (typeof (scope.ttDragEnd) === "function") {
						scope.ttDragEnd(d, dat);
					}
				});

			function endDrag() {
				selectedNode = null;
				d3.selectAll('.ghost').attr('class', 'ghost acc-fallback-ignore');
				d3.select(domNode).classed('activeDrag', false).attr("filter", null);
				// now restore the mouseover event or we won't be able to drag a 2nd time
				d3.select(domNode).select('.ghost').attr('pointer-events', '');
				updateTempConnector();
				if (draggingNode !== null) {
					$timeout(function () {
						scope.updater(scope.root);
						var dn = draggingNode;
						draggingNode = null;
					}, $window.treeDuration || 100);
				}
			}
		}
	}
}]);
