﻿Tours.textTodoIssue = {
	start: function () {
		// debugger;
		var a = new Anno({
			target: '#header-tab-l10',
			content: "Click the Level 10 tab.",
			className: 'anno-width-300',
			position: { left: "-15px" },
			arrowPosition: "top",
			buttons: []
		});
		Tours.appendParams(a, '#header-tab-l10', "textTodoIssue", "l10");
		a.show();
	},
	l10: function () {
		var any = "You'll have to create a Level 10 before you can setup Text-A-Todo and Text-An-Issue. Once created, your Level 10 meetings will show up here. ";
		if ($(".l10-row").length == 1)
			any = "Your Level 10 meetings show up here. You've already got one L10 meeting.";
		else if ($(".l10-row").length > 1)
			any = "Your Level 10 meetings show up here. You've already got a few L10 meetings.";

		var anno1 = {
			target: '#l10-meeting-list',
			content: any,
			className: 'anno-width-300',
			//position: { left: "190px", top: "50px" },
			arrowPosition: "left",
			position: "right",
			onShow: function (a) {
				a.showOverlay();
			}
		};
		//var anno11 = {
		//    target: '.alerts',
		//    content: "Let's add texting capabilities to one of your Level 10 meetings.",
		//    className: 'anno-center anno-width-300',
		//    //position: { left: "190px", top: "50px" },
		//    arrowPosition: "none",
		//    onShow: function (a) {
		//        a.showOverlay();
		//    }
		//};
		var anno11 = {
			target: '#alerts',
			className: "anno-center anno-width-300",
			arrowPosition: "none",
			content: "Let's attach a phone number to a meeting. Once we're set up, you'll be able to text your to-do or issue to the meeting.",
			noEmphasis: true,
			onShow: function (a) {
				//Tours.appendParams(a, '.l10-extra-options', "textTodoIssue", "wizard");
			}
			//buttons: []
		};

		var intercept = function (e) {
			e.preventDefault();
			debugger;
			location.href = $(this).attr('href') + "?tname=textTodoIssue&tmethod=wizard";
		};

		var a;
		var anno2 = {
			target: '.l10-extra-options',
			content: "Click this button to open extra settings.",
			className: 'anno-width-300',
			buttons: [],
			// position: { left: "-26px", top: "30px" },
			position: "left",
			// arrowPosition: "top",
			onShow: function (a) {
				a.showOverlay();

				$('a.l10-extra-options').on('click', intercept);

			},
			onHide: function(a){
				('a.l10-extra-options').off('click', intercept);
			}

		};
		//Tours.clickToAdvance(anno2);
		a = new Anno([anno1, anno11, anno2]);
		a.show();
		//Tours.appendParams(anno2, '#l10-create-meeting', "createL10", "wizard");
	},
	wizard: function () {
		var anno2 = {
			target: "#alerts",
			content: "This is the meeting manager.",
			className: 'anno-width-400 anno-center',
			//arrowPosition: "center-right",
			arrowPosition: "none",
			//position: "right",

		};


		var anno3 = {
			target: ".l10-texting-actions",
			content: "Click on texting actions.",
			className: 'anno-width-400',
			//arrowPosition: "center-right",
			//arrowPosition: "none",
			buttons: [],
			position: "right",
			//noOverlay: true
			//onShow: function (a) {
			//    a.hideOverlay();
			//},
			//onHide: function (a) {
			//    a.showOverlay();
			//},
			// buttons: [AnnoButton.BackButton,AnnoButton.NextButton]
		};
		//pages.push({
		//	target: '#wizard-extra-buttons',
		//	content: "You can also access your meeting's archive, timeline, V/TO™, and more.",
		//	className: 'anno-width-400',
		//	//position: { left: "-15px" },
		//	arrowPosition: "left",
		//	buttons: [AnnoButton.BackButton, AnnoButton.NextButton]
		//});


		Tours.clickToAdvance(anno3);

		var anno4 = {
			target: '#alerts',
			className: "anno-center anno-width-300",
			arrowPosition: "none",
			content: "Now lets pick a phone number, and choose whether this number adds an issue or to-do.",
			noEmphasis: true,
			buttons: [{
				text: 'Next',
				className: 'anno-btn',
				click: function (anno, evt) {
					var a = anno;
					waitUntilVisible("#modalOk", function () {
						setTimeout(function () {
							a.switchToChainNext();
						}, 500);
					});
					//$("#modal").on('shown.bs.modal.my-anno', function () {

					//    //anno5.show()
					//}).on('hide.bs.modal.my-anno', function () {
					//    anno5.hide();
					//    $("#modal").off('.my-anno');
					//});
				}
			}],
		};
		//Tours.clickToAdvance(anno4);

		var anno5 = {
			target: "#modalOk",
			className: "anno-width-300",
			position: "bottom-right",
			arrowPosition: "top-right",
			content: "When you've selected a phone number and action, click OK. ",
			buttons: [],
			//onShow: function () {
			//   
			//}
		};
		Tours.clickToAdvance(anno5);


		var anno6 = {
			target: "#alerts",
			className: "anno-width-300 anno-center ",
			content: "Use these instructions to register your phone.",
			arrowPosition: "none",
			//position: "bottom",
			buttons: [AnnoButton.NextButton],
			noGlow: true,
			//noEmphasis: true
		};
		//Tours.clickToAdvance(anno5);
		var anno7 = {
			target: "#alerts",
			className: "anno-center anno-width-300",
			title: "You're all set!",
			arrowPosition: "none",
			content: "Texting this number will add an issue, to-do, or headline to your meeting.",
			noEmphasis: true,
			noOverlay: true,

		};




		var a = new Anno([/*anno1, anno11,*/ anno2, anno3, anno4, anno5, anno6, anno7]);

		a.show();
	}
};