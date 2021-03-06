﻿angular.module('imageTemplates', []).directive("profileImage", function () {
    return {
        restrict: "E",
        scope: {
            user: "="
        },
        link: function (scope, element, attrs) {
            var hash = 0, i, chr, len;
            if (scope.user && scope.user.Name) {
                var str = scope.user.Name;
                if (str.length != 0) {
                    for (i = 0, len = str.length; i < len; i++) {
                        chr = str.charCodeAt(i);
                        hash = ((hash << 5) - hash) + chr;
                        hash |= 0; // Convert to 32bit integer
                    }
                }
                hash = hash % 360;
                scope.user.colorCode = hash;

                if (typeof (scope.user.Initials) === "undefined") {
                    scope.user.Initials = getInitials(scope.user.Name).toUpperCase();
                }
            }
        },
        template: "<span class='picture-container' title='{{user.Name}}'>" +
        "<span ng-if='user.ImageUrl!=\"/i/userplaceholder\" && user.ImageUrl!=null && user.ImageUrl!=\"\"' class='picture' style='background: url({{user.ImageUrl}}) no-repeat center center; background-color:hsla({{::user.colorCode}}, 36%, 49%, 1);color:hsla({{::user.colorCode}}, 36%, 72%, 1)'></span>" +
        "<span ng-if='user.ImageUrl==\"/i/userplaceholder\"' class='picture keep-background' ng-style='getElementStyle(user.colorCode)'><span class='initials'>{{user.Initials}}</span></span>" +
			"<span ng-if='user.ImageUrl==null || user.ImageUrl==\"\"' class='picture keep-background' style='color:#ccc'><span style='position: absolute;width:100%;height:100%;text-align: center;margin-top: 50%;line-height: 0;left: 0;'>?</span></span>" +
        "</span>",
        controller: ["$scope",function ($scope) {
			$scope.getElementStyle = function (colorCode) {
				var name = $scope.user.Name;
				hash = 0;
				if (name.length != 0) {
					for (var i = 0; i < name.length; i++) {
						{
							var chr = name.charCodeAt(i);
							hash = ((hash << 5) - hash) + chr;
							hash |= 0; // Convert to 32bit integer
						}
					}
					//console.log(name + ": " + hash + " = " + Math.abs(hash) % 360);
					hash = Math.abs(hash) % 360;
				}
				colorCode = hash;
                return {
                    'background-color': 'hsla(' + colorCode + ', 36%, 49%, 1)', 'color': 'hsla(' + colorCode + ', 36%, 72%, 1)'
                };
            };
        }]
    };
});
