/*
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///  settings ={                                                                                                                ///
///      container:<element_id>,		                                                                                        ///
///      id: (optional, default: guid),                                                                                         ///
///      data:[{},{},...],                                                                                                      ///
///		 title:(optional, default: ""),																							///
///		 nodataText:(optional, default: "No data available."),																	///
///																																///
///		 clickAdd:<url||function,function(row,settings)||object>(optional, default: null),										///
///		 clickEdit:<url||bool||function,function(row,settings)>(optional, default: null									///
///				true = autogenerate popup from cells																			///
///		 clickRemove:<url||function,function(row,settings)>(optional, default: null),											///
///		 clickReorder:<url||function,function(row,oldIndex,newIndex,settings)>(optional, default: null),						///
///			{0} = row.Id																										///
///			{1} = oldIndex																										///
///			{2} = newIndex																										///
///		 postAdd:<url>																											///
///		 postEdit:<url>																											///
///																																///
///		 cellId:<function(cell,settings)>(optional, default: cell=>cell.Id),													///
///																																///
///      panel:{	(optional)																									///
///			id:(optional, default: "panel-{id}"),																				///
///			classes:(optional, default: "panel panel-primary"),																	///
///			element:(optional, default: "<div/>"),																				///
///			header:{																											///
///				id:(optional, defaults: null),																					///
///				classes:(optional, default: "panel-heading"),																	///
///				element:(optional, default: "<div/>"),																			///
///				title:{																											///
///					id:(optional, defaults: null),																				///
///					classes:(optional, default: "panel-title"),																	///
///					element:(optional, default: "<h2/>"),																		///
///				}																												///
///			},																													///
///			nodata:{																											///
///				id:(optional, defaults: "panel-nodata-{id}"),																	///
///				classes:(optional, default: "panel-body"),																		///
///				element:(optional, default: "<div/>"),																			///
///			},																													///
///		 },																														///
///      addButton:(optional)<false,{																							///
///			id:(optional, default: "add-{id}"),																					///
///			element:(optional, default: "<div/>"),																				///
///			classes:(optional, default: "btn btn-primary btn-invert"),															///
///			text:(optional, default: "New {title}"),																			///
///		 }>,																													///
///      table:{	(optional)																									///
///			id:(optional, default: "table-{id}"),																				///
///			element:(optional, default: "<table/>"),																			///
///			classes:(optional, default: "table table-hover"),																	///
///			rows:{ (optional)																									///
///				id:<function(row,settings)>(optional, defaults: "row row-{id}-{row.Id}"),										///
///				classes:(optional, default: ""),																				///
///				element:(optional, default: "<tr/>"),																			///
///			},																													///
///			cells:{ (optional)																									///
///				id:<function(row,settings)>(optional, default: null),															///
///				classes:<function(row,settings)>(optional, default: ""),														///
///				element:(optional, default: "<td/>"),																			///
///			}																													///
///			editText:(optional, default: "Edit"),																				///
///			removeText:(optional, default: "Delete"),																			///
///		 },																														///
///      cells: [{                                                                                                              ///
///          id:(optional, overrides above),																					///
///          classes:(optional, overrides above),																				///
///          contents:<string,function(row,i)>(optional),																		///
///          edit:<bool,function(settings)>(optional, false),																	///
///          remove:<bool,function(settings)>(optional, false),																	///
///          reorder:<bool,function(settings)>(optional, false),																///
///          rowNum:<bool,function(settings)>(optional, false),																	///
///          text/name:<bool,function(settings)>(optional, null),																///
///          hideColumn:<bool,function(settings)>(optional, false),																///
///          input:<bool,obj> (optional, true) For autogenerated modals. false=not included										///
///			 {																													///
///					name:<bool,function(row,settings)>(optional, cell.name),													///
///					value: <function(row,i)>(optional, cell.contents),															///
///					text: <function(row,settings)>(optional, cell.name),														///
///					type: <function(row,settings)>(optional, 'text'),															///
///					classes: <function(row,settings)>(optional, null),															///
///			 }																													///
///			 hideIfEmpty: (optional, default:false)																				///
///		 },...,																													///
///			...function(row,i) ,...																								///
///      },...],																												///
///      nodata: <string,element>,																								///
///  }                                                                                                                          ///
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
*/

/**
 * @typedef {Object} dataTableData
 * @property {Object} anyProperty
 */
/**
 * @typedef {String} Url - The Endpoint
 * 
 */
/**
 * @callback dataTableRowSettingsFunc
 * @property {Object} row - Row data
 * @property {Object} settings - Table settings
 */
/**
 * @typedef {Object} dataTableAddButtonSettings 
 * @property {string} [element] - Element type. Default <div/>
 * @property {string} [classes] - Button classes. Default "btn btn-primary btn-invert"
 * @property {string} [text] - Text to display. Default "New {title}"
 * @property {string} [id] - Button Id
 */

/**
 * @callback dataTableCellContents
 * @property {Object} row - Row data
 * @property {Object} i - Row number
 */
/**
 * @callback dataTableCellSettings
 * @property {Object} settings - table settings
 */
/**
 * 
 * @typedef {Object} dataTableCellFunction
 * @property {dataTableCellContents} [contents] - Html to display
 * @property {string} [classes] - Add a class to the cell
 * @property {boolean|dataTableCellSettings} [edit] - Is this an edit button cell?
 * @property {boolean|dataTableCellSettings} [remove] - Is this a remove button cell?
 * @property {boolean|dataTableCellSettings} [reorder] - Is this a reorder cell?
 * @property {boolean|dataTableCellSettings} [rowNum] - Is this a rowNumber cell?
 * @property {boolean|dataTableCellSettings} [text] - ?
 * @property {boolean|dataTableCellSettings} [name] - ?
 * @property {boolean|dataTableCellSettings} [hideColumn] - hides the column
 * @property {boolean|Object} [input] - for autogenerated modals, False to exclude. See datatable.js for documentation
 * @property {boolean} [hideIfEmpty] - hides the column if they are all empty 
 * @property {string} [id] - override the id
 */
/**
 * @callback dataTableCellFunctionSetting
 * @property {dataTableData} row - row data
 * @property {number} i - row rumber
 * @returns dataTableCellFunction
 */


/**
 * 
 * @typedef {Object} dataTableSettings
 * @property {string} [container] - Element selector to inject the datatable into, by default inject into the #main
 * @property {dataTableData[]} data - The data for the table
 * @property {dataTableCellFunction[]|dataTableCellFunctionSetting[]} [cells] - How to display the cells in the table
 * 
 * @property {string} [id] - The id of the table, by default a guid
 * @property {string} [title=""] - The title of the table, by default ""
 * @property {string} [nodataText=""] - Text to display when no data is present, by default "No data available."
 * @property {(Url|dataTableRowSettingsFunc|modalSettings)} [clickAdd] - Action to perform when clicking Add. Default none
 * @property {(Url|boolean|dataTableRowSettingsFunc|modalSettings)} [clickEdit] - Action to perform when clicking Edit. Default none.
 * @property {(Url|dataTableRowSettingsFunc|modalSettings)} [clickRemove] - Action to perform when clicking Remove. Default none.
 * @property {(Url|dataTableRowSettingsFunc|modalSettings)} [clickReorder] - Action to perform when reordering. Default none.
 * @property {(Url)} [postAdd] - Url to post an Add to.
 * @property {(Url)} [postEdit] - Url to post an Edit to.
 * @property {dataTableRowSettingsFunc} [cellId] - Function that selects the Id for the cell. Default x=>x.Id
 * @property {(boolean|dataTableAddButtonSettings)} [addButton] - Adjust the Panel display settings. See DataTable.js for documentation
 * 
 * @property {Object} [panel] - Adjust the Panel display settings. See DataTable.js for documentation
 * @property {Object} [table] - Adjust the Table display settings. See DataTable.js for documentation
 * @property {Object} [panel] - Adjust the Panel. See DataTable.js for documentation
 */
/**
 * @param {dataTableSettings} settings
 */
var DataTable = function (settings) {

	//Helpers
	var resolve = function (strFunc, args) {
		try {
			if (typeof (strFunc) === "string")
				return strFunc;
			else if (typeof (strFunc) === "function")
				return strFunc.apply(settings, [].splice.call(arguments, 1));
			else if (typeof (strFunc) === "boolean")
				return strFunc;
		} catch (e) {
			console.warn("resolve error: " + e);
		}
		return null;
	}

	if (!settings.container) {
		console.warn("Container not set for data-table.");
		settings.container = "#main .body-content";
	}

	settings.id = settings.id || generateGuid();
	var id = settings.id;

	if (typeof (data) === "undefined" || data === null || data === false)
		data = [];

	settings.title = resolve(settings.title, settings) || "";
	settings.nodataText = resolve(settings.nodataText, settings) || "No data available.";

	//AddButton
	if (settings.addButton != false) {
		if (settings.addButton == true) {
			settings.addButton = {};
		}

		if (!settings.clickAdd) {
			console.warn("Cannot generate AddButton if clickAdd is null. To disable this warning set 'addButton: false'");
			settings.addButton = false;
		} else {
			settings.addButton = settings.addButton || {};
			settings.addButton.id = settings.addButton.id || "add-" + settings.id;
			settings.addButton.element = settings.addButton.element || $("<div/>");
			settings.addButton.classes = settings.addButton.classes || "btn btn-primary btn-invert";
			settings.addButton.text = settings.addButton.text || "New " + settings.title;
		}
	}


	//IdSelector
	settings.cellId = settings.cellId || function (cell) { return cell.Id; };

	//Panel
	settings.panel = settings.panel || {};
	settings.panel.id = settings.panel.id || "panel-" + settings.id;
	settings.panel.element = settings.panel.element || $("<div/>");
	settings.panel.classes = settings.panel.classes || "panel panel-primary";

	//Panel - Header
	if (settings.panel.header != false) {
		settings.panel.header = settings.panel.header || {};
		settings.panel.header.id = settings.panel.header.id || null;
		settings.panel.header.classes = settings.panel.header.classes || "panel-heading";
		settings.panel.header.element = settings.panel.header.element || $("<div/>");

		settings.panel.header.title = settings.panel.header.title || {};
		settings.panel.header.title.classes = settings.panel.header.title.classes || "panel-title";
		settings.panel.header.title.element = settings.panel.header.title.element || $("<h2/>");
	}

	//Panel - No Data
	settings.panel.nodata = settings.panel.nodata || {};
	settings.panel.nodata.id = settings.panel.nodata.id || function (_settings) { return "panel-nodata-" + _settings.id; };
	settings.panel.nodata.classes = settings.panel.nodata.classes || "panel-body gray";
	settings.panel.nodata.element = settings.panel.nodata.element || $("<div/>");

	//Table
	settings.table = settings.table || {};
	settings.table.id = settings.table.id || "table-" + settings.id;
	settings.table.element = settings.table.element || $("<table/>");
	settings.table.bodyElement = settings.table.bodyElement || $("<tbody/>");
	settings.table.classes = settings.table.classes || "table table-hover";

	//Table - Rows
	settings.table.rows = settings.table.rows || {};
	settings.table.rows.id = settings.table.rows.id || function (row, _settings) {
		if (row == "header") {
			return "row-header";
		}
		var rid = resolve(settings.cellId, row, settings);
		return "row-" + _settings.id + "-" + rid;
	};

	settings.table.rows.element = settings.table.rows.element || $("<tr/>");
	settings.table.rows.classes = settings.table.rows.classes || "row";

	//Table - Cells
	settings.table.cells = settings.table.cells || {};
	settings.table.cells.id = settings.table.cells.id || function (row, _settings) { return null; };
	settings.table.cells.element = settings.table.cells.element || $("<td/>");
	settings.table.cells.classes = settings.table.cells.classes || "";

	settings.table.editText = settings.table.editText || "Edit";
	settings.table.removeText = settings.table.removeText || "Delete";

	//Cell Selectors
	settings.cells = settings.cells || [];

	//Internal
	settings._ = settings._ || {};
	settings._.olddata = settings._.olddata || [];

	//Variables
	var container, addButton, panel, panelTitle, panelHeader, table, nodata;

	//Complex Updates
	if (typeof (settings.clickEdit) === "string") {
		settings._.onEditAction = settings.clickEdit;
		var postWasSet = false;
		if (settings.postEdit)
			postWasSet = true;

		settings._.onEditActionPost = settings.postEdit || settings.clickEdit;
		settings.clickEdit = function (row, settings) {
			var title = settings.clickEditTitle || function (settings) { return "Edit " + resolve(settings.title, settings); };
			var rid = resolve(settings.cellId, row, settings);
			var actionType = typeof (settings._.onEditAction);
			if (actionType == "string") {
				var pUrl = settings._.onEditActionPost.replace("{0}", postWasSet ? rid : "");
				if (!showModal) {
					console.warn("modal.js not available.");
				} else {
					showModal(resolve(title, settings), settings._.onEditAction.replace("{0}", rid), pUrl, null, null, function (d) {
						if (d.Error) {
							showAlert(d);
						} else {
							editRow(d.Object);
						}
					});
				}
			}
		};
	}
	if (typeof (settings.clickEdit) === "boolean" && settings.clickEdit == true) {

		//settings._.onEditAction = settings.clickEdit;
		var postWasSet = false;
		if (settings.postEdit)
			postWasSet = true;
		settings._.onEditActionPost = settings.postEdit || settings.clickEdit;
		settings.clickEdit = function (row, settings) {
			if (!showModal) {
				console.warn("modal.js not available.");
			} else {
				var title = settings.clickEditTitle || function (settings) { return "Edit " + resolve(settings.title, settings); };
				var rid = resolve(settings.cellId, row, settings);

				var postUrl = null;
				if (!settings.postEdit) {
					console.warn("postEdit not specified. Edit will not do anything.");
				} else {
					postUrl = settings.postEdit.replace("{0}", rid);
				}

				var fields = [];
				var modalSettings = {
					title: resolve(title, settings),
					pushUrl: postUrl,
					success: function (d) {
						if (d.Error) {
							showAlert(d);
						} else {
							editRow(d.Object);
						}
					},
				};
				for (var c in settings.cells) {
					if (arrayHasOwnIndex(settings.cells, c)) {
						var cell = settings.cells[c];
						if (cell.edit || cell.remove)
							continue;

						var cellInput = cell.input;
						if (cellInput != false) {
							if (!cellInput) {
								cellInput = {};
							}
							fields.push({
								name: resolve(cellInput.name || cell.name, row, settings),
								text: resolve(cellInput.text || cell.name, row, settings),
								type: resolve(cellInput.type || cell.type || "text", row, settings),
								value: resolve(cellInput.value || resolve(cell.value, row, settings) || cell.contents, row, rid),
								classes: resolve(cellInput.classes, row, settings),
								options: cellInput.options,
							});
						}
					}
				}
				modalSettings.fields = fields;

				showModal(modalSettings);
			}
		};

	}
	/*
	if (typeof (settings.clickEdit) === "object") {
		settings._.onEditOptions = settings.clickEdit;

		var oldSuccess = settings.clickEdit.success;
		settings._.onEditOptions.success = function (d) {
			if (oldSuccess) {
				oldSuccess(d);
			}
			editRow(d.Object);
		};

		settings.clickEdit = function (row,settingsX) {
			debugger
			showModal(settingsX._.onEditOptions);
		}
	}*/


	if (typeof (settings.clickAdd) === "string") {
		settings._.onAddUrl = settings.clickAdd;
		settings._.onAddUrlPost = settings.postAdd || settings.clickAdd;
		settings.clickAdd = function (settings) {
			var title = settings.clickAddTitle || function (settings) { return "Add " + resolve(settings.title, settings); }
			showModal(resolve(title, settings), settings._.onAddUrl, settings._.onAddUrlPost, null, null, function (d) {
				if (d.Error) {
					showAlert(d);
				} else {
					addRow(d.Object);
				}
			});
		};
	}
	if (typeof (settings.clickAdd) === "object") {
		settings._.onAddOptions = settings.clickAdd;

		var oldSuccess = settings.clickAdd.success;
		settings._.onAddOptions.success = function (d) {
			if (oldSuccess) {
				oldSuccess(d);
			}
			if (d.Error) {
				showAlert(d);
			} else {
				addRow(d.Object);
			}
		};

		settings.clickAdd = function (settings) {
			showModal(settings._.onAddOptions);
		}

	}
	if (typeof (settings.clickReorder) === "string") {
		settings._.clickReorderUrl = settings.clickReorder;
		settings.clickReorder = function (row, oldIndex, newIndex, settings) {
			var rid = resolve(settings.cellId, row, settings);

			$.ajax({
				url: settings._.clickReorderUrl.replace("{0}", rid).replace("{1}", oldIndex).replace("{2}", newIndex),
				error: function (e) {
					if (oldIndex > newIndex)
						oldIndex -= 1;
					var item = settings.data.splice(newIndex, 1);
					settings.data.splice(oldIndex, 0, item);
					update();
					refreshRowNum();
				}
			})
		};
	}
	if (typeof (settings.clickRemove) === "string") {
		settings._.onRemoveUrl = settings.clickRemove;
		settings.clickRemove = function (row, settings) {
			var title = settings.clickRemoveTitle || function (settings) { return "Are you sure you want to remove " + (resolve(settings.title, settings) || "").toLowerCase(); };
			var rid = resolve(settings.cellId, row, settings);
			showModal({
				icon: "warning",
				title: resolve(title, settings),
				success: function (d) {
					$.ajax({
						url: settings._.onRemoveUrl.replace("{0}", rid),
						success: function () {
							removeRow(row);
						}
					});
				}
			});
		};
	} else if (typeof (settings.clickRemove) === "function") {
		settings._.onRemoveFunc = settings.clickRemove;
		settings.clickRemove = function (row, settings) {

			var resultUrl = settings._.onRemoveFunc(row, settings);
			if (typeof (resultUrl) === "string") {
				var title = settings.clickRemoveTitle || function (settings) { return "Are you sure you want to remove " + (resolve(settings.title, settings) || "").toLowerCase(); };
				var rid = resolve(settings.cellId, row, settings);
				showModal({
					icon: "warning",
					title: resolve(title, settings),
					success: function (d) {
						$.ajax({
							url: resultUrl.replace("{0}", rid),
							success: function () {
								removeRow(row);
							}
						});
					}
				});
			}
		};
	}

	//Generator Functions
	var rowIndexShift = 0;
	var generateContainer = function () {
		container = $("<div/>");
		panel = $(settings.panel.element).clone();
		if (settings.panel.header != false) {
			panelHeader = $(settings.panel.header.element).clone();
			panelTitle = $(settings.panel.header.title.element).clone();
			panelHeader.append(panelTitle);
			panel.append(panelHeader);
		}
		table = $(settings.table.element).clone();
		var tableBody = $(settings.table.bodyElement).clone();
		table.append(tableBody);
		$(panel).append(table);
		var headers = [];
		var anyHeaders = false;
		for (var c in settings.cells) {
			if (arrayHasOwnIndex(settings.cells, c)) {
				if (resolve(settings.cells[c].hideColumn, null, settings) != true) {
					var cellName = resolve(settings.cells[c].text, settings) || resolve(settings.cells[c].name, settings);
					if (cellName != null) {
						anyHeaders = true;
					}
					headers.push(cellName);
				}
			}
		}

		if (anyHeaders) {
			settings._.generatedHeaders = [];

			var headerRow = $(settings.table.rows.element).clone();
			try {
				$(headerRow).attr("id", resolve(settings.table.rows.id, "header", settings));
			} catch (e) {
				console.warn("HeaderRow id failed to resolve");
			}
			try {
				$(headerRow).attr("class", resolve(settings.table.rows.classes, null, settings));
			} catch (e) {
				console.warn("HeaderRow class failed to resolve");
			}

			for (var c in headers) {
				if (arrayHasOwnIndex(headers, c)) {
					var headerCell = $(settings.table.cells.element).clone();
					if (headers[c] != null) {
						if (headerCell.is("td")) {
							headerCell = $("<th/>");
						}
						headerCell.text(headers[c]);
					}
					settings._.generatedHeaders.push(headerCell);
					$(headerRow).append(headerCell);
				}
			}
			//rowIndexShift -= 1;
			var head = $("<thead/>");
			head.append(headerRow);
			$(table).append(head);

		} else {
			console.warn("No headers. To add a header, supply a 'name' to the cell.");
		}


		nodata = $(settings.panel.nodata.element).clone();
		$(panel).append(nodata);

		if (settings.addButton != false) {
			var btnHolder = $("<div style='text-align: right;margin-bottom: 3px;'/>");
			addButton = $(settings.addButton.element).clone();
			btnHolder.append(addButton);
			container.append(btnHolder);
			$(addButton).on("click", function () { resolve(settings.clickAdd, settings); });
		}
		container.append(panel);

		var anyReorder = false;
		for (var c in settings.cells) {
			if (arrayHasOwnIndex(settings.cells, c)) {
				if (resolve(settings.cells[c].reorder, settings) == true) {
					if (!settings.clickReorder) {
						console.warn("Cannot use cell.reorder if clickReorder is not defined. To disable this warning remove reordable cells contents.");
						//settings.addButton = false;
						//delete (settings.cells[c]);
					} else {
						try {
							$.getScript("/Scripts/jquery/jquery.ui.sortable.js").done(function () {
								$("#" + settings.table.id + " tbody").xsortable({
									items: ">.row",
									handle: ".reorder-handle",
									start: function (e, ui) {
										$(this).attr('data-previndex', ui.item.index() + rowIndexShift);
									},
									update: function (e, ui) {
										var newIndex = ui.item.index() + rowIndexShift;
										var oldIndex = +$(this).attr('data-previndex');
										$(this).removeAttr('data-previndex');
										var row = settings.data[oldIndex];
										resolve(settings.clickReorder, row, oldIndex, newIndex, settings);
										refreshRowNum();
									}
								}).disableSelection();
							});
						} catch (e) {
							console.warn("xsortable not loaded.");
						}
					}
				}
			}
		}
	}
	var generateRow = function (rowData) {
		var row = $(settings.table.rows.element).clone();
		$(row).attr("id", resolve(settings.table.rows.id, rowData, settings));
		$(row).attr("class", resolve(settings.table.rows.classes, rowData, settings));
		row.append(generateRowCells(rowData));
		return row;
	};

	var generateRowCells = function (row) {
		var i = 0;
		var results = [];
		for (var s in settings.cells) {
			if (arrayHasOwnIndex(settings.cells, s)) {
				var cellSelector = settings.cells[s];

				if (resolve(cellSelector.hideColumn, row, settings) != true) {
					var q = generateCell(settings, row, cellSelector, i);
					results.push(q.dom);
					i++;
				}
			}
		}
		return results;
	};

	var generateCell = function (settings, row, cellSelector, i) {
		var cell = $(settings.table.cells.element).clone();

		var contents = null;
		var cellSelectorId = settings.table.cells.id;
		var cellSelectorClasses = settings.table.cells.classes;

		if (typeof (cellSelector) === "object") {
			cellSelectorId = cellSelector.id || cellSelectorId;
			cellSelectorClasses = cellSelector.classes || cellSelectorId;
			contents = cellSelector.contents;
		} else if (typeof (cellSelector) === "function") {
			contents = cellSelector;
		}

		cell.attr("id", resolve(cellSelectorId, row, settings));
		cell.attr("class", resolve(cellSelectorClasses, row, settings));

		//Is edit button?
		if (resolve(cellSelector.edit, row) == true) {
			cell.on("click", function () { resolve(settings.clickEdit, row, settings); });
			if (!contents)
				contents = settings.table.editText;
			cell.addClass("clickable");
		}

		//Is remove button?
		if (resolve(cellSelector.remove, row) == true) {
			cell.on("click", function () { resolve(settings.clickRemove, row, settings); });
			if (!contents)
				contents = settings.table.removeText;
			cell.addClass("clickable");
		}

		//Is row number?
		if (resolve(cellSelector.rowNum, settings) == true) {
			var oldContents = contents;
			contents = function (row, i, settings) {
				return "<span class='rowNum'>" + (i + 1) + ". </span>" + (resolve(oldContents, row, i, settings) || "");
			};
		}

		//Is draggable?
		if (resolve(cellSelector.reorder, settings) == true) {
			contents = function (row, i, settings) {
				return "<span class='reorder-handle icon fontastic-icon-three-bars icon-rotate gray' style='margin-left: -5px;margin-right: -5px;cursor:move;'></span>";
			};
		}

		var html = resolve(contents, row, i, settings);

		if (contents == null)
			console.warn("Contents null for " + i);
		if (typeof (html) === "undefined")
			console.warn("Cell was undefined for " + i + " (Did you forget to 'return'?)");

		cell.html(html);

		return { dom: cell, html: html };
	}


	//Update Function
	var idError = false;
	var getIds = function (data) {
		var res = [];
		for (var d in data) {
			if (arrayHasOwnIndex(data, d)) {
				var rid = resolve(settings.cellId, data[d], settings);
				//var  = data[d][selector];
				if (typeof (rid) === "undefined") {
					console.error("Id resolved to undefined for data[" + d + "].");
				}
				res.push(rid);
			}
		}
		return res;
	};
	var diffIds = function (a, b) {
		return a.filter(function (i) { return b.indexOf(i) < 0; });
	};
	var getRowById = function (data, id) {
		for (var r in data) {
			if (arrayHasOwnIndex(data, r)) {
				var rid = resolve(settings.cellId, data[r], settings);
				if (rid == id)
					return data[r];
			}
		}
		return null;
	};
	var insertAt = function (self, index, element) {
		var lastIndex = self.children().length;
		if (index < 0) {
			index = Math.max(0, lastIndex + 1 + index)
		}
		self.append(element)
		if (index < lastIndex) {
			self.children().eq(index).before(self.children().last())
		}
		return self;
	}

	var refreshColumns = function (settings) {
		settings.data = settings.data || [];
		settings.cells = settings.cells || [];

		for (var c in settings.cells) {
			if (arrayHasOwnIndex(settings.cells, c)) {
				var cell = settings.cells[c];

				//Hide empty columns
				if (cell.hideIfEmpty) {
					var allEmpty = true;
					var i = 0;
					for (var d in settings.data) {
						if (arrayHasOwnIndex(settings.data, d)) {
							var data = settings.data[d];
							var html = generateCell(settings, data, cell, i).html;
							if (!(html == null || typeof (html) === "undefined")) {
								allEmpty = false;
								break;
							}
							i++;
						}
					}
					if (allEmpty) {
						console.log("column empty!");
						settings._.generatedHeaders[c].hide();
					} else {
						settings._.generatedHeaders[c].show();
					}
				}
			}
		}
	}
	var refreshRowNum = function () {
		$(".rowNum").each(function (i, x) {
			$(this).html("" + (i + 1) + ". ");
		});
	};
	var updateRowsUI = function (settings) {
		settings.data = settings.data || [];

		var dataIds = getIds(settings.data);
		var oldIds = getIds(settings._.olddata);

		var added = diffIds(dataIds, oldIds);
		var removed = diffIds(oldIds, dataIds);
		var checkEdit = diffIds(dataIds, added);

		for (var a in added) {
			if (arrayHasOwnIndex(added, a)) {
				var row = getRowById(settings.data, added[a]);
				var tableId = resolve(settings.table.id, settings);
				var tableElement = $("#" + tableId+" tbody");
				insertAt(tableElement, dataIds.indexOf(added[a]), generateRow(row));
			}
		}

		for (var a in removed) {
			if (arrayHasOwnIndex(removed, a)) {
				var row = getRowById(settings._.olddata, removed[a])
				var rowId = settings.table.rows.id(row, settings);
				var rowElement = $("#" + rowId);
				rowElement.children().off();
				rowElement.off();
				rowElement.remove();
			}
		}

		for (var a in checkEdit) {
			if (arrayHasOwnIndex(checkEdit, a)) {
				var newRow = getRowById(settings.data, checkEdit[a]);
				var oldRow = getRowById(settings._.olddata, checkEdit[a]);
				if (JSON.stringify(newRow) != JSON.stringify(oldRow)) {
					console.log("edit row " + checkEdit[a]);
					var rowId = settings.table.rows.id(newRow, settings);
					var rowElement = $("#" + rowId);
					rowElement.children().off();
					rowElement.children().remove();
					rowElement.append(generateRowCells(newRow));
				}
			}
		}

		if (!settings.data || !settings.data.length) {
			$(table).hide();
			$(nodata).show();
		} else {
			$(table).show();
			$(nodata).hide();
		}
		refreshRowNum();
		refreshColumns(settings);

		settings._.olddata = JSON.parse(JSON.stringify(settings.data));
	}
	var updateProperties = function (settings) {
		$(panel).attr("id", resolve(settings.panel.id, settings));
		$(panel).attr("class", resolve(settings.panel.classes, settings));

		if (settings.panel.header != false) {
			$(panelHeader).attr("id", resolve(settings.panel.header.id, settings));
			$(panelHeader).attr("class", resolve(settings.panel.header.classes, settings));
			$(panelTitle).attr("id", resolve(settings.panel.header.title.id, settings));
			$(panelTitle).attr("class", resolve(settings.panel.header.title.classes, settings));
			var title = resolve(settings.title, settings);
			$(panelTitle).html(title);
			$(panelHeader).toggleClass("hidden", title == null || title == "" || typeof (title) === "undefined");

		}

		$(table).attr("id", resolve(settings.table.id, settings));
		$(table).attr("class", resolve(settings.table.classes, settings));

		$(nodata).attr("id", resolve(settings.panel.nodata.id, settings));
		$(nodata).attr("class", resolve(settings.panel.nodata.classes, settings));
		$(nodata).html(resolve(settings.nodataText, settings));

		if (settings.addButton != false) {
			$(addButton).attr("id", resolve(settings.addButton.id, settings));
			$(addButton).attr("class", resolve(settings.addButton.classes, settings));
			$(addButton).html(resolve(settings.addButton.text, settings));
		}
	};

	var addRow = function (row) {
		console.info("add row");
		if (row) {
			settings.data.push(row);
			update();
		} else {
			showAlert("Row could not be added.");
			console.warn("row was " + row);
		}
	};
	var editRow = function (row) {
		console.info("edit row");
		if (row) {
			try {
				var ids = getIds(settings.data);
				var rid = resolve(settings.cellId, row, settings);
				var index = ids.indexOf(rid);
				settings.data[index] = row;
			} catch (e) {
				console.error(e);
			}

			update();
		} else {
			showAlert("Row could not be edited.");
			console.warn("row was " + row);
		}
	};
	var removeRow = function (row, skipUpdate) {
		console.info("remove row");
		if (row) {
			var rid = resolve(settings.cellId, row, settings);
			for (var i = settings.data.length - 1; i >= 0; i--) {
				var did = resolve(settings.cellId, settings.data[i], settings);
				if (did == rid)
					settings.data.splice(i, 1);
			}
			update();
		} else {
			showAlert("Row could not be removed.");
			console.warn("row was " + row);
		}
	};

	var update = function () {
		updateProperties(settings);
		updateRowsUI(settings);
	};

	generateContainer();

	if (settings.container) {
		$(settings.container).append(container);
		update();
	} else {
		console.warn("container was not specified.")
	}

	return {
		update: update,
		settings: settings,

		setData: function (data) {
			settings.data = data;
			update();
		},
		addRow: addRow,
		//insertRow: insertRow,
		editRow: editRow,
		removeRow: removeRow,
		removeRowById: function (id) {
			var row = getRowById(settings.data, id);
			return removeRow(row);
		},

		container: container,

		//data: settings.data,
	};
}