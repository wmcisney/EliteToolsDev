


$(window).on("wizard:changed-page", function (e, data) {


	updateCreateButton();

	setTimeout(function () {
		try {
			//var scope = angular.element("[vs-repeat]").scope();
			//scope.$emit("vs-repeat-resize");
			//scope.$digest();
		} catch (e) {
			console.error(e);
		}
		setCompletion(data.completion * 100);

	}, 1);

	setTimeout(function () {
		try {
			//var scope = angular.element("[vs-repeat]").scope();
			//scope.$emit("vs-repeat-resize");
			//scope.$digest();
		} catch (e) {
			console.error(e);
		}
	}, 1500);


	$(".backButton.disabled,.nextButton.disabled").removeClass("disabled");
});
$(window).on("wizard:first-page", function (e, data) {
	$(".backButton").addClass("disabled");
});
$(window).on("wizard:last-page", function (e, data) {
	$(".nextButton").addClass("disabled");
});

var lastLoc = -1000;
function updateCreateButton() {
	var btn = $(".create-row:visible");
	if (btn.length) {
		var container = btn.closest(".component.wizard-page");
		var windowHeight = $(window).height() - container.offset().top - btn.height() / 2 - 10;
		var loc = container.offset().top + container.height() - $(document).scrollTop() - parseFloat(container.css("margin-bottom")) + parseFloat(container.css("padding-bottom")) / 2;
		var x = loc;
		if (loc > windowHeight) {
			x = windowHeight;
		}

		var time = 80;
		if (Math.abs(loc - lastLoc) < 50) {
			time = 0;
		} else {
			console.log("[L10Wizard] Lengthen:" + Math.abs(loc - lastLoc));
		}


		btn.animate({ top: x, opacity: 1 }, time,"easeInOutCubic");
		lastLoc = loc;
	}
}
setInterval(updateCreateButton, 70);
$(window).on("scroll", updateCreateButton);
$(window).on("resize", updateCreateButton);
