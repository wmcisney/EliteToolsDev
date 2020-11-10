
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//Create issues, todos, headlines, rocks
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

$("body").on("click", ".issuesModal:not(.disabled)", function () {
	var dat = getDataValues(this);
	var parm = $.param(dat);
	var m = dat.method;
	if (!m)
		m = "Modal";
	var title = dat.title || "Add an issue";
	showModal(title, "/Issues/" + m + "?" + parm, "/Issues/" + m);
});
$("body").on("click", ".todoModal:not(.disabled)", function () {
	var dat = getDataValues(this);
	var parm = $.param(dat);
	var m = dat.method;
	if (!m)
		m = "Modal";
	var title = dat.title || "Add a to-do";
	showModal(title, "/Todo/" + m + "?" + parm, "/Todo/" + m, null, function () {
		var found = $('#modalBody').find(".select-user");

		var name = $('#modalBody').find("[name='Message']");
		//var found = $('#modalBody').find(".select-user");
		if (name.length && (name.val() == null || name.val() == "")) {
			name.addClass("has-error");
			return "You must specify a to-do name.";
		}

		if (found.length && (found.val() == null || (Array.isArray(found.val()) && found.val().length == 0)))
			return "You must select at least one to-do owner.";

		return true;
	});
});
$("body").on("click", ".headlineModal:not(.disabled)", function () {
	var dat = getDataValues(this);
	var parm = $.param(dat);
	var m = dat.method;
	if (!m)
		m = "Modal";
	var title = dat.title || "Add a people headline";
	showModal(title, "/Headlines/" + m + "?" + parm, "/Headlines/" + m, null, function () {
		var found = $('#modalBody').find(".select-user");
		//if (found.length && found.val() == null)
		//	return "You must select at least one to-do owner.";
		return true;
	});
});

$("body").on("click", ".rockModal:not(.disabled)", function () {
	var dat = getDataValues(this);
	var parm = $.param(dat);
	var m = dat.method;
	var c = dat.controller;
	var post = dat.post;
	if (!m)
		m = "EditModal";
	if (!c)
		c = "Rocks";
	if (!post)
		post = m;
	var title = dat.title || "Add a rock";
	showModal(title, "/" + c + "/" + m + "?" + parm, "/" + c + "/" + post, null, function () {
		var name = $('#modalBody').find("[name='Name']");
		//var found = $('#modalBody').find(".select-user");
		if (name.length && (name.val() == null || name.val() == "")) {
			name.addClass("has-error");
			return "You must specify a rock name.";
		}
		name = $('#modalBody').find("[name='Title']");
		//var found = $('#modalBody').find(".select-user");
		if (name.length && (name.val() == null || name.val() == "")) {
			name.addClass("has-error");
			return "You must specify a rock name.";
		}
		return true;
	});
});

$("body").on("click", ".scorecardModal:not(.disabled)", function () {
	var dat = getDataValues(this);
	var parm = $.param(dat);
	var m = dat.method;
	var c = dat.controller;
	var pc = dat.postcontroller;
	var post = dat.post;
	if (!m)
		m = "CreateMeasurable";
	if (!c)
		c = "Scorecard";
	if (!post)
		post = m;
	if (!pc)
		pc = c;

	var title = dat.title || "Add a measurable";
	showModal(title, "/" + c + "/" + m + "?" + parm, "/" + pc + "/" + post, null, function () {
		var found = $('#modalBody').find("#Name");
		if (found.length && (found.val() == null || found.val() == "")) {
			return "You must specify a measurable name."; 
		}
		debugger;
		return true;
	});
});

$("body").on("click", ".milestoneModal:not(.disabled)", function () {
	var dat = getDataValues(this);
	var parm = $.param(dat);
	var m = dat.method;
	if (!m)
		m = "Modal";
	var title = dat.title || "Add a milestone";
	showModal(title, "/Milestone/" + m + "?" + parm, "/Milestone/" + m, null, function () {
		//var found = $('#modalBody').find(".select-user");
		//if (found.length && found.val() == null)
		//	return "You must select at least one to-do owner.";
		return true;
	});
});