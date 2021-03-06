﻿using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Todo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {

	public class ITodoHookUpdates {
		public ITodoHookUpdates(string updateSource) {
			UpdateSource = updateSource;
		}

		public bool MessageChanged { get; set; }
		public bool DueDateChanged { get; set; }
		public bool CompletionChanged { get; set; }
		public bool AccountableUserChanged { get; set; }
		public long PreviousAccountableUser { get; set; }
		public string UpdateSource { get; set; }
	}

	public interface ITodoHook : IHook {
		Task CreateTodo(ISession s, UserOrganizationModel caller, TodoModel todo);
		Task UpdateTodo(ISession s, UserOrganizationModel caller, TodoModel todo, ITodoHookUpdates updates);
	}
}
