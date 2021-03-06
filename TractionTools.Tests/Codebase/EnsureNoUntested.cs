﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview;
using RadialReview.Accessors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TractionTools.Tests.Codebase {
	[TestClass]
	public class EnsureNoUntested {

		[TestMethod]
		[TestCategory("Codebase")]
		public void _ALERT_NoUntested() {
			Assembly assembly = typeof(ApplicationAccessor).Assembly;
			var methods = assembly.GetTypes().SelectMany(t => t.GetMethods()).Where(m => m.GetCustomAttributes(typeof(Untested), false).Length > 0).ToArray();

			foreach (var m in methods) {
				Console.WriteLine(m.ReflectedType.FullName + " => " + m.Name);
				var attr = (Untested)m.GetCustomAttributes(typeof(Untested), false).First();
				Console.WriteLine("\t- " + attr.message);
				foreach (var a in attr.notes) {
					Console.WriteLine("\t\t+- " + a);
				}
			}

			if (methods.Any())
				Assert.Inconclusive("Methods remain untested (" + methods.Count() + "). See console.");
		}
		[TestMethod]
		[TestCategory("Codebase")]
		public void _ALERT_NoTodos() {
			Assembly assembly = typeof(ApplicationAccessor).Assembly;
			var methods = assembly.GetTypes().SelectMany(t => t.GetMethods()).Where(m => m.GetCustomAttributes(typeof(TodoAttribute), false).Length > 0).ToArray();

			foreach (var m in methods) {
				Console.WriteLine(m.ReflectedType.FullName + " => " + m.Name);
				var attr = (TodoAttribute)m.GetCustomAttributes(typeof(TodoAttribute), false).First();
				Console.WriteLine("\t- " + attr.message);
				foreach (var a in attr.notes) {
					Console.WriteLine("\t\t+- " + a);
				}
			}

			if (methods.Any())
				Assert.Fail("DO NOT PUBLISH. Critical to-dos remain undone. (" + methods.Count() + "). See console.");
		}
	}
}
