/*
    1. Upload File          (uploadFileUrl)
    2. Select Data          (uploadSelectionUrl)
    3. Confirm Selection    (confirmSelectionUrl)
*/

function UploadSteps(args) {
	//=================================================
	//Add these to args (Required)
	this.uploadFileUrl = null;
	this.uploadFileData = {};
	this.uploadSelectionUrl = null;
	this.uploadSelectionData = {};
	this.confirmSelectionUrl = null;
	this.confirmSelectionData = {};
	this.windowSelector = "#window";
	this.notificationSelector = "#notifications";
	this.submissionCallback = false; // May alternatively specify a callback instead of redirecting.

	//=================================================
	//Add these to args (Optional)
	this.defaultData = {};
	this.currentlySelectedClass = "ui-selected";
	this.currentlySelectingClass = "ui-selecting";
	this.previouslySelectedClass = "wasSelected";
	this.confirmationText = "Does everything look correct?";
	this.afterUpload = function (resultObject) { }
	//=================================================

	this.currentSelectionStep = 0;
	this.selectionSteps = [];

	var validationFunc = null;
	var errors = [];

	var that = this;
	var instructionAlert = function () { return that.notificationSelector + " .instruction"; }
	var errorAlert = function () { return that.notificationSelector + " .errors"; }
	var nextButton = function () { return that.notificationSelector + " .nextButton"; }
	var backButton = function () { return that.notificationSelector + " .backButton"; }
	var skipButton = function () { return that.notificationSelector + " .skipButton"; }
	var tableSelector = function () { return that.windowSelector + " .selector-table"; }

	var getFormData = function ($form) {
		var unindexed_array = $form.serializeArray();
		var indexed_array = {};

		$.map(unindexed_array, function (n, i) {
			if (n['name'] in indexed_array && indexed_array[n['name']] == "true") {
				//skip. checkboxes dont work correctly
			} else {
				indexed_array[n['name']] = n['value'];
			}
		});

		return indexed_array;
	}

	this.showLoading = function () {
		$(this.windowSelector).html("<center><div class='loader centered alignCenter'><div class='component '><div>Loading</div><img src='/Content/select2-spinner.gif' /></div></div></center>");
	}

	this.stackRect = [[]];

	this.initFileSelect = function () {
		var data = $.extend({}, this.defaultData, this.uploadFileData);


		var dataStr = "";
		for (var d in data) {
			if (arrayHasOwnIndex(data, d)) {
				dataStr += "<input type=hidden name='" + d + "' value='" + data[d] + "'/>";
			}
		}
		//var submit = $("<input class='btn btn-primary'/>");
		// 
		$(".instructions").show();
		$(that.notificationSelector).hide();

		var uploadForm = $("<form action='javascript:;' enctype='multipart/form-data' method='post' accept-charset='utf-8'>   " +
			"    <div class='row'>                                                                 " +
			"        <div class='col-xs-2'>                                                        " +
			"            <label for='file' class='pull-right1'>Filename:</label>                   " +
			"        </div>                                                                        " +
			"        <div class='col-xs-3'>                                                        " +
			"            <input class='blend' type='file' name='File' id='File' />                 " +
			"        </div>                                                                        " +
			"        <div class='col-xs-2 submit'>                                                 " +
			"            <input class='btn btn-primary' type='submit'/>                            " +
			"        </div>                                                                        " +
			"    </div>                                                                            " +
			dataStr +
			"</form>                                                                               ");

		$(uploadForm).submit(function (e) {
			that.showLoading();
			e.preventDefault();
			console.warn("Using FormData will not work on IE9");
			var formData = new FormData($(this)[0]);
			console.log("upload_url: " + that.uploadFileUrl);
			$.ajax({
				url: that.uploadFileUrl,
				data: formData,
				type: "POST",
				async: false,
				cache: false,
				contentType: false,
				processData: false,
				enctype: 'multipart/form-data',
				success: function (d) {
					that.onUploadSuccess(d);
				},
				error: function (d, e) {
					showAlert(e);
					that.initFileSelect();
					console.error(d);
					sendErrorReport();
					//$(this.windowSelector).html(uploadForm);
				}
			});
			return false;
		});
		$(this.windowSelector).html(uploadForm);
	}

	function sleep(ms) {
		return new Promise(resolve => setTimeout(resolve, ms));
	}

	this.onUploadSuccess = async function (d) {
		if (this.uploadMultiple == true) {

			for (var i = 0; i < d.Object.length; i++) {
				var prevUploader = window.lastRunUploader;
				$("body").append("<script src=\"/Scripts/Upload/" + d.Object[i].Script + "\"/>");
				while (prevUploader == window.lastRunUploader) {
					await sleep(10);
				}
				var curUploader = window.lastRunUploader;
				d.Object[i].Uploader = curUploader;
				var guess = d.Object[i].StepGuess;

				debugger;
				if (guess && guess.length) {
					curUploader.initialSelection = curUploader.initialSelection || [];
					for (var j = 0; j < guess.length; j++) {
						curUploader.initialSelection[j] = guess[j];
					}
				}
				debugger;

				curUploader.onUploadSuccess(d.Object[i].Result);
				var loop = true;
				curUploader.submissionCallback = function () {
					loop = false;
				}

				while (loop) {
					await sleep(1000);
				}
			}

			$(".window").html("Upload Complete");
			if (typeof (this.submissionCallback) === "function") {
				this.submissionCallback();
			}

		} else {
			var html = processSuccess(d, this.uploadSelectionData);
			if (this.afterUpload)
				this.afterUpload(d);
			$(this.windowSelector).html(html);
			this.initSelection();
			$(".instructions").hide();
			try {
				console.log("path: " + $(html).find("#file_name").val());
			} catch (e) {
				console.error(e);
			}
		}
	}

	function highlightSelection(x1, y1, x2, y2) {
		debugger;
		var table = $(that.windowSelector).find("table");
		var str = [];
		var minx = Math.min(x1, x2);
		var miny = Math.min(y1, y2);
		var maxx = Math.max(x1, x2);
		var maxy = Math.max(y1, y2);

		for (var i = minx; i <= maxx; i++) {
			for (var j = miny; j <= maxy; j++) {
				str.push("td[data-row=" + j + "][data-col=" + i + "]");
			}
		}

		$("." + that.currentlySelectedClass).removeClass(that.currentlySelectedClass);
		$(str.join(","), table).addClass(that.currentlySelectedClass);
	}


	this.initSelection = function () {
		var button = $("<button disabled class='nextButton btn btn-primary pull-right btn-sm' style='margin-top:-4px;'>Next</button>");
		$(button).click(function () {
			that.nextSelectionStep();
		});
		var btnSkip = $("<button class='skipButton btn btn-default pull-right btn-sm' style='margin-top:-4px;'>Skip</button>");
		$(btnSkip).click(function () {
			that.skipSelectionStep();
		});
		var btnBack = $("<button class='backButton btn btn-default pull-right btn-sm' style='margin-top:-4px;'>Back</button>");
		$(btnBack).click(function () {
			that.backSelectionStep();
		});
		$(this.notificationSelector).html("<div class='alert alert-info next-container'><span class='instruction'>Please wait...</span></div><div class=' errors alert alert-danger'></div>")
		$(this.notificationSelector).find(".next-container").append(button);
		$(this.notificationSelector).find(".next-container").append(btnBack);
		$(this.notificationSelector).find(".next-container").append(btnSkip);
		$(this.notificationSelector + " .errors").hide();
		$(skipButton()).hide();
		this.currentSelectionStep = 0;
		var start = null, end;
		$(nextButton()).show();
		$(this.notificationSelector).show();

		var that = this;
		var table = $(this.windowSelector).find("table");
		$(table).find("tr").each(function (i) {
			$(this).find("td").each(function (j) {
				$(this).data("row", i);
				$(this).data("col", j);
				$(this).attr("data-row", i);
				$(this).attr("data-col", j);
			});
		});

		$("td", table).mousedown(function () {
			$("." + that.currentlySelectedClass).removeClass(that.currentlySelectedClass);
			start = [$(this).data("col"), $(this).data("row")];
			mouseOver.apply(this);
		});

		function mouseOver() {
			var cur = [$(this).data("col"), $(this).data("row")];

			if (start != null) {
				end = cur;
				var minx = Math.min(cur[0], start[0]);
				var miny = Math.min(cur[1], start[1]);
				var maxx = Math.max(cur[0], start[0]);
				var maxy = Math.max(cur[1], start[1]);
				var str = [];
				for (var i = minx; i <= maxx; i++) {
					for (var j = miny; j <= maxy; j++) {
						str.push("td[data-row=" + j + "][data-col=" + i + "]");
					}
				}
				try {
					console.log("selection " + that.selectionSteps[that.currentSelectionStep - 1].message + ": " + minx + "," + miny + "," + maxx + "," + maxy);
				} catch (e) {
					console.error(e);
				}
				$("." + that.currentlySelectingClass).removeClass(that.currentlySelectingClass);
				$(str.join(","), table).addClass(that.currentlySelectingClass);
			}
		}

		$("td", table).mouseenter(function () {
			mouseOver.apply(this);
		});

		$(document).mouseup(function () {
			if (start != null && end != null) {
				start = null; end = null;
				$("." + that.currentlySelectingClass).addClass(that.currentlySelectedClass).removeClass(that.currentlySelectingClass);
				$(nextButton()).attr("disabled", true);

				if (validationFunc != null) {
					var rect = getRect();
					that.stackRect[that.stackRect.length - 1] = rect;
					if (validationFunc(rect)) {
						$(errorAlert()).html("").hide();
						$(nextButton()).attr("disabled", null);
					} else {
						$(nextButton()).attr("disabled", true);
						$(errorAlert()).html(errors.join("<br/>")).show();
						//$("#table").selectable("refresh");
						errors = [];

					}
				}
			} else if (start != null && end == null) {
				$(nextButton()).attr("disabled", true);
				start = null; end = null;
			} else {
				start = null; end = null;
				// $(".ui-selected").removeClass("ui-selected");
			}
		});

		this.nextSelectionStep();
	}

	this.initConfirmSelection = function (html) {
		var form = $("<form action='javascript:;' enctype='multipart/form-data' method='post' accept-charset='utf-8'>   " +
			"<input class='btn btn-success finalSubmit' type='submit' value='Submit'/>" +
			html +
			"</form>");
		$(that.windowSelector).html(form);
		$(that.notificationSelector).hide();

		$(form).submit(function (e) {
			//debugger;
			e.preventDefault();
			var d = getFormData($(this));
			that.showLoading();
			that.submitConfirmSelection(d);
		});

	}

	this.skipSelectionStep = function () {
		$(".ui-selected").removeClass("ui-selected");
		//debugger;
		that.stackRect[that.stackRect.length - 1] = [];
		this.nextSelectionStep();
	};

	//this.backSelectionStep = function () {
	//    this.currentSelectionStep -= 2;
	//    this.nextSelectionStep();
	//};

	this.nextSelectionStep = function () {
		$("." + this.currentlySelectedClass).addClass(this.previouslySelectedClass).removeClass(this.currentlySelectedClass);
		if (this.currentSelectionStep == 0)
			$(backButton()).hide();
		else
			$(backButton()).show();


		if (this.currentSelectionStep >= this.selectionSteps.length) {
			$(instructionAlert()).css("color", "rgba(0, 0, 0, 0.73)").html("Please wait...");
			$(nextButton()).attr("disabled", "true");
			validationFunc = null;
			this.submitSelection();
			//$(this.windowSelector).html("<center>Please Wait</center>");
			this.showLoading();
			return;
		}
		if (this.selectionSteps[this.currentSelectionStep].skipable) {
			$(skipButton()).show();
		} else {
			$(skipButton()).hide();
		}

		$(nextButton()).attr("disabled", "true");
		$(errorAlert()).hide();
		errors = [];
		$(instructionAlert()).css("color", "#005ed7").html(this.selectionSteps[this.currentSelectionStep].message).show();
		//$("#table").selectable("refresh");
		$(tableSelector()).css("display", null);
		validationFunc = this.selectionSteps[this.currentSelectionStep].func;

		debugger;

		var initialSelection = [];

		//has initial selection
		try {
			if (this.initialSelection && this.initialSelection[this.currentSelectionStep] != []) {
				initialSelection = this.initialSelection[this.currentSelectionStep];

				highlightSelection(initialSelection[0], initialSelection[1], initialSelection[2], initialSelection[3], initialSelection[4]);
				validationFunc(initialSelection);
				$(nextButton()).attr("disabled", null);
			}
		} catch (e) {
			console.error(e);
		}

		that.stackRect.push(initialSelection);



		this.currentSelectionStep += 1;
	}

	this.backSelectionStep = function () {
		this.currentSelectionStep = Math.max(0, this.currentSelectionStep - 2);
		var selection = that.stackRect.pop();
		deselectRect(selection, "ui-selected");
		var selection = that.stackRect.pop();
		deselectRect(selection, "wasSelected");

		this.nextSelectionStep();
	}

	var processSuccess = function (d, extendData) {
		if (typeof (d.Error) !== "undefined") {
			if (showJsonAlert(d)) {
				if (!d || typeof (d.Html) === "undefined")
					throw "Expects obj.Html from '" + that.uploadSelectionUrl + "'";
				if (typeof (d.Data) !== "undefined")
					$.extend(true, extendData, d.Data);

				return d.Html;
			}
		} else {
			//$(d).find("input[type='hidden'")
			return d;
		}
	}

	this.submitSelection = function () {
		var data = $.extend({}, this.defaultData, this.uploadSelectionData);
		//var data = {
		//    userRect: userRect,
		//    dateRect: dateRect,
		//    measurableRect: measurableRect,
		//    goalRect: goalRect,
		//    RecurrenceId: $("[name='RecurrenceId']").val(),
		//    Path: $("[name='Path']").val(),
		//    UseAWS: $("[name='UseAWS']").val()
		//};
		$.ajax({
			url: that.uploadSelectionUrl,
			method: "post",
			data: JSON.stringify(data),
			traditional: true,
			//dataType: "json",
			contentType: 'application/json; charset=utf-8',
			success: function (d) {
				var html = processSuccess(d, that.confirmSelectionData);
				that.initConfirmSelection(html);
			},
			error: function (d, e) {
				showHtmlErrorAlert(d, "Error uploading file.");
				that.initFileSelect();
				console.error(d);
				sendErrorReport();
			}
		});
	}

	this.submitConfirmSelection = function (formData) {
		var data = $.extend({}, this.defaultData, this.confirmSelectionData);
		$.extend(true, data, formData);
		$.ajax({
			url: that.confirmSelectionUrl,
			method: "post",
			data: data,
			//dataType: "json",  
			traditional: true,
			//dataType: 'json',
			//contentType: 'application/json; charset=utf-8',
			success: function (d) {
				var hasCallback = typeof (that.submissionCallback) == "function";
				if (hasCallback) {
					try {
						window.HaltRedirect();
					} catch (e) {
						debugger;
						console.error(e);
					}
				}

				if (!showJsonAlert(d)) {
					$(that.windowSelector).html("<center>An error occurred. " + d.Message + " </center>");
					setTimeout(that.initFileSelect, 2000);
				} else {
					if (hasCallback) {
						that.submissionCallback();
					}
				}



			},
			error: function (e) {
				showAlert("An error occurred.");
				that.initFileSelect();
				console.error(e);
				sendErrorReport();
			}
		});
	}

	this.addSelectionStep = function (instructions, validate, skipable) {
		this.selectionSteps.push({ message: instructions, func: validate, skipable: skipable });
	}

	this.clearSelectionSteps = function () {
		//debugger;
		this.selectionSteps = [];
	}

	var deselectRect = function (rect, toremove) {
		for (var i = rect[0]; i <= rect[2]; i++) {
			for (var j = rect[1]; j <= rect[3]; j++) {
				$("[data-row=" + j + "][data-col=" + i + "]").removeClass(toremove);
			}
		}
		//var y = $(this).data("row");
		//var x = $(this).data("col");
	}

	var getRect = function () {
		var minx = 10000000;
		var miny = 10000000;
		var maxx = 0;
		var maxy = 0;
		var count = 0;
		$("." + that.currentlySelectingClass + ", ." + that.currentlySelectedClass).each(function () {
			var y = $(this/*must be this*/).data("row");
			var x = $(this/*must be this*/).data("col");
			miny = Math.min(miny, y);
			minx = Math.min(minx, x);
			maxy = Math.max(maxy, y);
			maxx = Math.max(maxx, x);
			count += 1;
		});
		if (count == 0)
			return null;
		return [minx, miny, maxx, maxy, count];
	}

	this.addSelectionData = function (data, value) {
		if (typeof (value) !== "undefined") {
			var o = {};
			o[data] = value;
			this.addSelectionData(o);
		} else {
			this.uploadSelectionData = $.extend(true, this.uploadSelectionData, data);
		}
	}

	this.verify = {
		atLeastOneCell: function (rect) {
			if (rect != null)
				return true;
			errors.push("You must select at least one cell.");
			return false;
		},
		eitherColumnOrRow: function (rect) {
			if (rect[0] == rect[2] || rect[1] == rect[3])
				return true;
			errors.push("You must select either a row or column.");
			return false;
		},
		similarSelection: function (rect1, rect2) {
			if ((rect1[0] == rect1[2] && rect1[1] == rect2[1] && rect1[3] == rect2[3]) || (rect1[1] == rect1[3] && rect1[0] == rect2[0] && rect1[2] == rect2[2]))
				return true;
			errors.push("Invalid selection. Cells must match the dimensions of the previous selection.");
			return false;
		},
		eitherAboveOrBelow: function (rect1, rect2) {
			if (rect1[1] > rect2[1] || rect1[3] < rect2[3])
				return true;
			errors.push("Invalid selection. Cells must be above or below the other selection.");
			return false;
		},
		rightOf: function (rect1, rect2) {
			if (rect1[2] < rect2[0])
				return true;
			errors.push("Invalid selection. Cells must be to the right the other selection.");
			return false;
		}
	};


	$.extend(this, args);
	this.initFileSelect();
	//debugger;
	window.lastRunUploader = this;
	return this;
}

$("body").on("keypress", function (e) {
	if (e.keyCode == 13) {
		if ($(".errors").css("display") != "none") {
			showAlert("Form has errors.");
			return;
		}

		var found = $(".finalSubmit");
		if (found.length > 0) {
			found.click();
		} else {
			found = $(".nextButton:not([disabled])");
			if (found.length > 0) {
				//debugger;
				found.click();
			} else {
				$(".skipButton").click();
			}
		}
	}
});