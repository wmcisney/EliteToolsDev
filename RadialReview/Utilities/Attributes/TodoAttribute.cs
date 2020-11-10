using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview {
	[AttributeUsage(AttributeTargets.Method)]
	public class TodoAttribute : Attribute {
		public string message;
		public string[] notes;

		public TodoAttribute(string message, params string[] toTest) {
			this.message = message;
			this.notes = toTest ?? new string[0];
		}
	}
}