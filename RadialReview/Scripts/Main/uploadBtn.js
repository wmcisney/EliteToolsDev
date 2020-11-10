window.TT = window.TT || {};
window.TT.Html = window.TT.Html || {};

window.TT.Html.UploadButton = function (settings) {
	settings = settings || {};
	settings.url = settings.url || "/upload/uploadfile";
	settings.buttonClass = settings.buttonClass || "btn btn-default btn-xs";
	settings.success = settings.success;
	settings.error = settings.error;

	if (!settings.container) {
		console.error("UploadButton requires a container");
		return;
	}
	if ($(settings.container).length == 0) {
		console.error("Specified container ('"+settings.container+"') does not exist");
	}

	var guid = generateGuid();
	var form = $("<form id='form_" + guid + "'  style='display:none' enctype='multipart/form-data'/>")
	var input = $("<input type='file' id='input_" + guid + "' />");
	form.append(input);
	$("body").append(form);
	var uploadImage = "<img src='data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAC4jAAAuIwF4pT92AAAAB3RJTUUH4wQZFS4Kdar88AAAABl0RVh0Q29tbWVudABDcmVhdGVkIHdpdGggR0lNUFeBDhcAAABOSURBVDjLY/z//z8DJYCJgUJAWwPS09P/p6en/yfLAGSN+AxhIqSZkCGM+GIBpmnmzJmMQyQWCIU6NnkWfKbj8zteAwjFPVXDgHHoZyYAFwonIYW/02oAAAAASUVORK5CYII=' alt='' />";
	var btn = $("<span class='" + settings.buttonClass +"' id='upload_btn_" + guid + "'>"+uploadImage+" Upload</span>");
	btn.click(function () {
		if (!$(btn).is(".disabled")) {
			input.click();
		}
	});

	function sendUpload() {
		$(input).addClass("disabled");
		var formData = new FormData();
		formData.append("File", input[0].files[0]);
		formData.append("Name", "Name");
		$.ajax({
			url: settings.url,
			data: formData,
			type: "POST",
			async: false,
			cache: false,
			contentType: false,
			processData: false,
			enctype: 'multipart/form-data',
			success: function (d) {
				console.log(d);
				if (typeof(settings.success)==="function") {
					settings.success({
						url: d.Object
					});
				}
			},
			error: function (d, e) {
				showAlert(e);
				if (typeof (settings.error) === "function") {
					settings.error(d);
				}
			},
			complete: function () {
				$(input).val('');
				$(input).removeClass("disabled");
			}
		});
	}
	input.change(sendUpload);
	$(settings.container).append(btn);	
}