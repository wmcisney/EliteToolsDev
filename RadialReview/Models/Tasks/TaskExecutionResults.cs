using Newtonsoft.Json;
using RadialReview.Models.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Tasks {
	public class TaskResult {
		public List<ScheduledTask> CreateTasks { get; set; }
		//public ExecutionResult Result { get; set; }
		public List<Mail> SendEmails { get; set; }
		public dynamic Response { get; set; }
		public bool Executed { get; set; }
		public List<Exception> Errors { get; set; }
		public bool ExceptionEmailSent = false;

		public TaskResult() {
			CreateTasks = new List<ScheduledTask>();
			SendEmails = new List<Mail>();
			Errors = new List<Exception>();
			//Result = new ExecutionResult();
		}
	}

	[Flags]
	public enum ExecutionStatus {
		Unstarted=0,
		Started=1,
		Executed=2,
		Unmarked=4,
		TasksCreated=8,
		EmailsSent=16

	}

	public class ExecutionResult {

		public ExecutionResult(
			long taskId, ExecutionStatus status,
			string url, dynamic response, DateTime startTime, DateTime endTime, List<ScheduledTask> newTasks,
			List<Exception> errors, bool errorEmailSent)
		{
			TaskId = taskId;
			Executed = status.HasFlag(ExecutionStatus.Executed);

			if (errors.Any()) {
				HasError = true;
			}

			Errors = errors;

			StartTime = startTime;
			EndTime = endTime;
			DurationMs = (endTime - startTime).TotalMilliseconds;
			ErrorEmailSent = errorEmailSent;
			Url = url;
			Response = response;
			Status = status;
			NewTasks = newTasks ?? new List<ScheduledTask>();
		}

		private static string ComputeMessage(List<Exception> errors) {		
			if (errors.Count == 1) {
				return errors.First().Message;
			} else if (errors.Count>1){
				return "Several errors were detected during execution";
			}
			return "Success";
		}

		public long TaskId { get; set; }
		public bool Executed { get; set; }
		public bool HasError { get; set; }

		public List<Exception> Errors { get; set; }
		
		public bool ErrorEmailSent { get; set; }

		//public string ErrorType { get; set; }
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public double DurationMs { get; set; }
		public string Url { get; set; }

		public ExecutionStatus Status { get; set; }

		public dynamic Response { get; set; }

		public List<ScheduledTask> NewTasks { get; set; }

		public Exception ToException() {
			var message = ComputeMessage(Errors);
			var json = JsonConvert.SerializeObject(new {
				Message = message,
				ExecutionResult = this
			},Formatting.Indented);
			return new Exception(json);
		}

	}
}