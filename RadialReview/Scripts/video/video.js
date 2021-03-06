﻿$(function () {

    $('body').on("click", '.videoconference-container .start-internal-video', function (e) {
        this.disabled = true;
        //debugger;
        connection.leave();
        connection.session.oneway = false;
        //connection.session.audio = true;
        //connection.session.video = true;
        //connection.session.broadcast = true;
        connection.openOrJoin();
        setTimeout(function () {
            if (!localConnected) {
                connection.open();
            }
        }, 6000);
        //connection.unhold("both");
        //connection.addStream({
        //    audio: true,
        //    video: true
        //});
        //connection.redial();
        jQuery.dequeue($("#main-window"), "fx");
    });

    // ................FileSharing/TextChat Code.............
    $('body').on('click', '.videoconference-container .video-container', function () {
        $(".video-overlay").addClass("fade1");
        var self = this;
        setTimeout(function () {
            console.log("FIVE");
            $(".video-overlay video").attr("src", $(self).find("video").attr("src"));
            $(".video-overlay video")[0].muted = $(self).find("video")[0].muted;
            $(".video-overlay").removeClass("hidden1");
            setTimeout(function () {
                $(".video-overlay").removeClass("fade1");
            }, 100);
        }, 150);
    });
    $('.video-overlay').click(function () {
        $(".video-overlay").addClass("hidden1");
        $(".video-overlay video").attr("src", "");
    });
    $('body').on('click', '.videoconference-container .start-internal-video:not(.disabled)', function () {
        $(".start-internal-video").addClass("disabled");
        $(".start-internal-video").html("Starting...");
        $(".start-video:not(.start-internal-video)").remove();
        //TODO Start video
    });

    $("body").on("click", ".share-file", function (e) {
        var fileSelector = new FileSelector();
        fileSelector.selectSingleFile(function (file) {
            connection.send(file);
        });
    });

    $("body").on("keyup", ".input-text-chat", function (e) {
        if (e.keyCode != 13) return;
        // removing trailing/leading whitespace
        this.value = this.value.replace(/^\s+|\s+$/g, '');
        if (!this.value.length) return;
        connection.send(this.value);
        appendDIV(this.value);
        this.value = '';
    });

    function selectStream(type, callback) {
        for (var i = 0; i < connection.attachStreams.length; i++) {
            var cur = connection.attachStreams[i];
            var e = connection.streamEvents[cur.streamid]
            if (e.type == type) {
                callback(cur);
            }
        }
    }
    var videoMuted = false;
    $('body').on('click', '.videoconference-container .sendVideo', function (e) {
		/*if (_mediaStream.getVideoTracks().length == 1) {
            var old = _mediaStream.getVideoTracks()[0].enabled;
            var newState = !old;
            _mediaStream.getVideoTracks()[0].enabled = newState;
            $(".videoconference-container .sendVideo").removeClass("fontastic-icon-eye-slash-close");
            $(".videoconference-container .sendVideo").removeClass("fontastic-icon-eye-2");
            if (newState) {
                $(".videoconference-container .sendVideo").addClass("fontastic-icon-eye-2");
            } else {
                $(".videoconference-container .sendVideo").addClass("fontastic-icon-eye-slash-close");
            }
        }*/
        var isMute = $(".videoconference-container .sendVideo").hasClass("fontastic-icon-eye-slash-close");

        if (isMute) { // UNMUTE
            selectStream('local', function (cur) {
                cur.getVideoTracks()[0].enabled = true;
            });
            $(".videoconference-container .sendVideo").removeClass("fontastic-icon-eye-slash-close");
            $(".videoconference-container .sendVideo").addClass("fontastic-icon-eye-2");
        } else {
            selectStream('local', function (cur) {
                cur.getVideoTracks()[0].enabled = false;
            });
            $(".videoconference-container .sendVideo").removeClass("fontastic-icon-eye-2");
            $(".videoconference-container .sendVideo").addClass("fontastic-icon-eye-slash-close");
        }


    });
    $('body').on('click', '.videoconference-container .sendAudio', function (e) {
        var isMute = $(".videoconference-container .sendAudio").hasClass("fontastic-icon-mic-no");

        if (isMute) { // UNMUTE
            selectStream('local', function (cur) {
                cur.getAudioTracks()[0].enabled = true;
            });
            $(".videoconference-container .sendAudio").removeClass("fontastic-icon-mic-no");
            $(".videoconference-container .sendAudio").addClass("fontastic-icon-mic");
            $(".videoconference-container .mine").removeClass("no-mic");
        } else {
            selectStream('local', function (cur) {
                cur.getAudioTracks()[0].enabled = false;
            });
            $(".videoconference-container .sendAudio").removeClass("fontastic-icon-mic");
            $(".videoconference-container .sendAudio").addClass("fontastic-icon-mic-no");
            $(".videoconference-container .mine").addClass("no-mic");
        }
    });


});

function closeVideoBar() {
    $(".footer-bar-container.shifted").find(".clicker").click();
}

function createZoom() {
    closeVideoBar();
    showModal("Add Meeting Room", "/Integrations/AddZoomModal", "/Integrations/addzoomroom?recurId=" + window.recurrenceId + "&userId=" + window.UserId, null, null,
        function (d) { });
    //{
    //	title: "Add a Meeting Room",
    //	fields: [
    //		{ type: "h3", text: "Click <a class='a-link' target='_blank' href='https://www.zoom.us/meeting/schedule'>Here</a> to sign into Zoom. Schedule a recurring meeting with these settings:" },
    //		{ type: "img", src: "https://s3.amazonaws.com/Radial/Instructional/ZoomSetup/ZoomSetup1.png", classes: "img-thumbnail zoom-thumbnail"  },
    //		{ type: "h3", text: "<br/>After clicking 'Schedule', find and copy your Meeting ID" },
    //		{ type: "img", src: "https://s3.amazonaws.com/Radial/Instructional/ZoomSetup/ZoomSetup2.png", classes: "img-thumbnail zoom-thumbnail" },
    //		{ type: "h3", text: "&nbsp;" },
    //		{ name: "zoomMeetingId", text: "Meeting ID" },
    //		{ name: "name", text: "Name",placeholder:"(optional)" },
    //	],
    //	push: "/integrations/addzoomroom?recurId=" + window.recurrenceId + "&userId=" + window.UserId,
    //	success: function () {
    //	}
    //})
}

function joinVideoConference(url, type, friendlyName, providerId) {
   console.log("new joinVideoConference");
    if (typeof (url) === "object") {
        var o = url;
        url = o.Url;
        type = type || o.VideoConferenceType;
        friendlyName = friendlyName || o.FriendlyName;
        providerId = providerId || o.Id;
    }
    $(window).off(".videoprovider");
    if (type == "Zoom") {
        $.ajax("/l10/SetJoinedVideo?recur=" + window.recurrenceId + "&provider=" + providerId);
		
		if (url.indexOf('http') !== -1 ){
			url = url.substr(url.indexOf('http'));
		}
		
        var newWindow = window.open(url, "s", "width= 1, height= 1, left=0, top=10000, resizable=yes, toolbar=no, location=no, directories=no, status=no, menubar=no, scrollbars=no, resizable=no, copyhistory=no");
		newWindow.blur();
		window.focus();
		setTimeout(function(){window.focus();},100);
		setTimeout(function () {
			newWindow.close();
		}, 5000);
		$(window).off(".videoprovider");
    } else if (type == "TractionTools") {
        $(".videoconference-container .start-internal-video").trigger("click");
    } else {
        showAlert("Could not start video.");
        return;
    }

    var selector = $(".videoconference-container .selected-video .start-conference.btn-success").prop("disabled", true).text("Starting...");
    setTimeout(function () {
        closeVideoBar();
    }, 1500);
    setTimeout(function () {
        var selector = $(".videoconference-container .selected-video .start-conference.btn-success").prop("disabled", false).text("Rejoin Conference");
    }, 2000);
}

var videoProviderList = {};

function getVideoProviderLink(vcProvider) {
    return "<a class='clickable' onclick='selectVideoProvider(" + vcProvider.Id + ");'>" + vcProvider.FriendlyName + "   </a><span  onclick='removeProvider(" + vcProvider.Id + ",\"" + vcProvider.FriendlyName + "\")' class='glyphicon glyphicon-trash clickable gray'></span>";
}

function addVideoProvider(vcProvider) {
    var item = $("<li></li>");
    item.append(getVideoProviderLink(vcProvider));
    var dropDown = $(".videoconference-container .type-" + vcProvider.VideoConferenceType + " .dropdown-menu");
    if (dropDown.find(".before-add-new").length == 0)
        dropDown.append("<li role='separator' class='divider before-add-new'></li");
    dropDown.append(item);

    if (!(vcProvider.VideoConferenceType in videoProviderList)) {
        videoProviderList[vcProvider.VideoConferenceType] = [];
    }
    videoProviderList[vcProvider.VideoConferenceType].push(vcProvider);
}

function removeVideoProvider(vcProvider) {

    var item = $("<li></li>");
    item.append(getVideoProviderLink(vcProvider));
    var dropDown = $(".videoconference-container .type-" + vcProvider.VideoConferenceType + " .dropdown-menu");
    dropDown.find(item).remove();

    var element_vid = videoProviderList[vcProvider.VideoConferenceType].find(function (element, i) {
        return element.Id === vcProvider.Id;
    });


    var indexOfElement = videoProviderList[vcProvider.VideoConferenceType].indexOf(element_vid);
    if (indexOfElement > -1) {
        videoProviderList[vcProvider.VideoConferenceType].splice(indexOfElement, 1);
    }

    if ($('#selectedVideoId').val() == vcProvider.Id) {
        unsetSelectedVideoProvider();
    }

    var clickEvent = '';
    if (videoProviderList[vcProvider.VideoConferenceType].length > 0) {
        var getZoomLink = videoProviderList[vcProvider.VideoConferenceType][0];
        clickEvent = "selectVideoProvider(" + getZoomLink.Id + ")";
        $('.join-meeting-btn').attr('onClick', clickEvent);
    }
    else {
        clickEvent = "createZoom()";
        $('.join-meeting-btn').attr('onClick', clickEvent);
    }

    closeVideoBar();
}

function selectVideoProvider(providerId) {
    $('#modal').modal('hide');
	var connectionId = "";
	try{
		connectionId=RealTime.connectionId;
	}catch(e){
		console.error(e);
	}

    $.ajax({
        url: "/L10/SetVideoProvider?provider=" + providerId + "&recur=" + window.recurrenceId + "&connectionid=" + connectionId,
    });

}

function removeProvider(providerId, providerName) {

    var connectionId = "";
    try {
        connectionId = RealTime.connectionId;
    } catch (e) {
        console.error(e);
    }

    showModal({
        title: "Are you sure you want to remove " + providerName + ".",
        icon: "danger",
        success: function () {
            $.ajax({
                url: "/Integrations/RemoveZoomRoom?providerId=" + providerId + "&connectionid=" + connectionId,
            });
        }
    });
}

function clickVideoProvider(vcProvider) {
    setSelectedVideoProvider(vcProvider);
    joinVideoConference(vcProvider.Url, vcProvider.VideoConferenceType, vcProvider.FriendlyName, vcProvider.Id);
}

function setSelectedVideoProvider(vcProvider) {
    console.log("setSelectedVideoProvider");
    $(".videoconference-container .unselected-video").hide();
    var selector = $(".videoconference-container .selected-video");
    selector.html('<div class="start-conference start-video btn btn-xs btn-success"' +
        ' onclick="joinVideoConference(' +
        '\'' + vcProvider.Url + '\',' +
        '\'' + vcProvider.VideoConferenceType + '\',' +
        '\'' + vcProvider.FriendlyName + '\',' +
        vcProvider.Id +
        ')">Join Conference</div><input type="hidden" id="selectedVideoId" value="' + vcProvider.Id+'"/><div class="start-conference start-video btn btn-xs btn-default" onclick="unsetSelectedVideoProvider()">Change Video Provider</div>');
    selector.show();
    //joinVideoConference(vcProvider.Url, vcProvider.VideoConferenceType, vcProvider.FriendlyName, vcProvider.Id);
}
function unsetSelectedVideoProvider() {
    $(".videoconference-container .unselected-video").show();
    var selector = $(".videoconference-container .selected-video");
    selector.hide();

}
