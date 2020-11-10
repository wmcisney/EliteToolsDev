angular.module('angular-content-editable', []);
angular.module('angular-content-editable')
	.directive('contentEditable', ['$log', '$sce', '$parse', '$window', 'contentEditable', function ($log, $sce, $parse, $window, contentEditable) {
		var directive = {
			restrict: 'A',
			require: 'ngModel',
			scope: { editCallback: '&?', changeCallback: '&?',disabled:'=?' },
			link: _link
		}
		return directive;
		function _link(scope, elem, attrs, ngModel) {
			// return if ng model not specified
			if (scope.disabled) {
				return;
			}

			if (!ngModel) {
				$log.warn('Error: ngModel is required in elem: ', elem);
				return;
			}
			var noEscape = true;
			var originalElement = elem[0];
			// get default usage options
			var options = angular.copy(contentEditable);
			// update options with attributes
			angular.forEach(options, function (val, key) {
				if (key in attrs) {
					options[key] = $parse(attrs[key])(scope);
				}
			});
			// add editable class
			attrs.$addClass(options.editableClass);
			// render always with model value
            ngModel.$render = function () {
                var textToDisplay = ngModel.$modelValue || elem.html(); 
                elem.html(options.renderHtml ? textToDisplay : convertLineBreak(textToDisplay));
            }

            function unEscapeHtml(htmlString) {
                return $('<div />')
                    .html(htmlString.replace(/<br>/gi, '\n'))
                    .text();
            }

            function convertLineBreak(text) {
                return text.replace(/\n/gi, '<br />');
            }

            // handle click on element
			function onClick(e) {
				e.preventDefault();
				attrs.$set('contenteditable', 'true');
				attrs.$addClass('active');
				originalElement.focus();
			}
			// check some option extra
			// conditions during focus
			function onFocus(e) {
				scope.$apply(function () {
					// turn on the flag
					noEscape = true;
					// select all on focus
					//debugger;
					if (options.focusSelect) {
						var range = $window.document.createRange();
						var selection = $window.getSelection();
						range.selectNodeContents(originalElement);
						selection.removeAllRanges();
						selection.addRange(range);
					}
					// if render-html is enabled convert
					// all text content to plaintext
					// in order to modify html tags
					if (options.renderHtml) {
						originalElement.textContent = elem.html();
					}
				});
			}
			function onBlur(e) {
				scope.$apply(function () {
					// the text
					var html;
					// remove active class when editing is over
					attrs.$removeClass('active');
					// disable editability
					attrs.$set('contenteditable', 'false');
					// if text needs to be rendered as html
					if (options.renderHtml && noEscape) {
						// get plain text html (with html tags)
						// replace all blank spaces
						html = originalElement.textContent.replace(/\u00a0/g, " ");
						// update elem html value
						elem.html(html);
					} else {
						// get element content replacing html tag
						//html = elem.text().replace(/&nbsp;/g, ' ');
                        html = unEscapeHtml(elem.html());
                    }
					// if element value is different from model value
					if (html !== ngModel.$modelValue) {
						/**
						* This method should be called
						* when a controller wants to
						* change the view value
						*/
						ngModel.$setViewValue(html)
						// if user passed a variable
						// and is a function
						if (scope.editCallback && angular.isFunction(scope.editCallback)) {
							// run the callback with arguments: current text and element
							return scope.editCallback({
								text: html,
								elem: elem
							});
						}
					}
				});
			}
			function debounce(func, delay) {
				var inDebounce;
				return function () {
					var context = this;
					var args = arguments;
					clearTimeout(inDebounce);
					inDebounce = setTimeout(function () { func.apply(context, args); }, delay);
				};
			}

			function onKeyDown(e) {

				// on tab key blur and
				// TODO: focus to next
				if (e.which == 9) {
					originalElement.blur();
					return;
				}
				// on esc key roll back value and blur
				if (e.which == 27) {
					ngModel.$rollbackViewValue();
					noEscape = false;
					return originalElement.blur();
				}


				// if single line or ctrl key is
				// pressed trigger the blur event
				if (e.which == 13 && (options.singleLine || e.ctrlKey)) {
					return originalElement.blur();
				}



            }

            function onPaste(e) {
                // cancel paste
                e.preventDefault();
                // get text representation of clipboard
                // Get pasted data via clipboard API
                let clipboardData = (e.originalEvent || e).clipboardData || window.clipboardData;
                // strip formatting from pasted data
                let pastedData = clipboardData.getData('text/plain');

                window.document.execCommand('insertText', false, pastedData);
            }

			/**
			* On click turn the element
			* to editable and focus it
			*/
			elem.on('click', onClick);
			/**
			* On element focus
			*/
			elem.on('focus', onFocus);
			/**
			* On element blur turn off
			* editable mode, if HTML, render
			* update model value and run callback
			* if specified
			*/
			elem.on('blur', onBlur);
			/**
			* Bind the keydown event for many functions
			*/
            elem.on('keydown', onKeyDown);
            /**
            * On element paste
            */
            elem.on('paste', onPaste);


			elem.on('keydown', debounce(function () {
				var html = elem.html().replace(/&nbsp;/g, ' ');
				console.info(html);
				scope.$apply(function () {
					// the text
					var html;
					// if text needs to be rendered as html
					if (options.renderHtml && noEscape) {
						// get plain text html (with html tags)
						// replace all blank spaces
						html = originalElement.textContent.replace(/\u00a0/g, " ");
						// update elem html value
						elem.html(html);
					} else {
						// get element content replacing html tag
						html = elem.text().replace(/&nbsp;/g, ' ');
					}
					// if element value is different from model value
					if (html != ngModel.$modelValue) {
						/**
						* This method should be called
						* when a controller wants to
						* change the view value
						*/
						ngModel.$setViewValue(html)
						// if user passed a variable
						// and is a function
						if (scope.changeCallback && angular.isFunction(scope.changeCallback)) {
							// run the callback with arguments: current text and element
							return scope.changeCallback({
								text: html,
								elem: elem
							});
						}
					}
				});
			}, 400));

			/**
			* On element destroy, remove all event
			* listeners related to the directive
			* (helps to prevent memory leaks)
			*/
			scope.$on('$destroy', function () {
				elem.off('click', onClick);
				elem.off('focus', onFocus);
				elem.off('blur', onBlur);
				elem.off('keydown', onKeyDown);
				elem.off('paste', onKeyDown);
			});
		}
	}])
angular.module('angular-content-editable')
	/**
	 * Provider to setup the default
	 * module options for the directive
	 */
	.provider('contentEditable', function () {
		var defaults = {
			editableClass: 'editable',
			keyBindings: true, // default true for key shortcuts
			singleLine: false,
			focusSelect: false, // default on focus select all text inside
			renderHtml: false,
			editCallback: false
		}
		this.configure = function (options) {
			return angular.extend(defaults, options);
		}
		this.$get = function () {
			return defaults;
		}
	});