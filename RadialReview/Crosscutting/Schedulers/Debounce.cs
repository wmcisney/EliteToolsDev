
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Text;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Models.Angular.Base;
using System.Collections;
using Newtonsoft.Json;

namespace RadialReview.Crosscutting.Schedulers {
	public sealed class DebounceAttribute : JobFilterAttribute, IElectStateFilter, IClientFilter, IServerFilter {
		static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(5);

		readonly int _seconds;
		TimeSpan _delay => TimeSpan.FromSeconds(_seconds);

		/// <summary>
		/// A fingerprint format to apply to each job. The job arguments will be passed to
		/// this via String.Format to generate a unique name. If left null, the name
		/// will be automatically generated from the job class, method, and parameters.
		/// The format should resolve to something 88 chars or less or will be automatically
		/// truncated.
		/// </summary>
		public string FingerPrintFormat { get; set; }

		/// <summary>
		/// Debounce the background task, preventing it from being called until the end
		/// of the lockout period.
		/// </summary>
		/// <param name="seconds">The length of the lockout period in seconds.</param>
		/// <param name="resourceFormat"></param>
		public DebounceAttribute(int seconds, string resourceFormat) {
			_seconds = seconds;
			FingerPrintFormat = resourceFormat;
		}

		public void OnCreating(CreatingContext context) {
			// If the job is created in anything other than an Enqueued state, check the
			// debounce lock state. This allows short scheduled versions to pass through.
			if (!(context.InitialState is EnqueuedState)) {
				return;
			}

			using (context.Connection.AcquireDistributedLock(GetFingerPrintLockKey(context.Job), LockTimeout)) {
				var timestamp = GetTimestamp(context.Connection, context.Job);

				if (timestamp.HasValue && DateTimeOffset.UtcNow <= timestamp.Value.Add(_delay)) {
					// Actual fingerprint found and still valid, cancel the creation of a new job.
					context.Canceled = true;

				}

				// Set the timestamp - this will add the lock key, or update
				// and extend the lock.
				context.Connection.SetRangeInHash(GetFingerPrintKey(context.Job), new Dictionary<string, string>
				{
					{
						"Timestamp", DateTimeOffset.UtcNow.ToString("o")
					}
				});
			}
		}

		/// <summary>
		/// Hangfire filter event called when the action is transitioning from
		/// one state to another.
		/// </summary>
		/// <param name="context"></param>
		public void OnStateElection(ElectStateContext context) {
			if (context.CandidateState is DeletedState) {
				// If we're transitioning to deleted, also ensure the fingerprint is removed.
				RemoveFingerPrint(context.Connection, context.BackgroundJob.Job);
			} else if (!(context.CandidateState is EnqueuedState)) {
				// If not tranitioning to an Enqueued state, skip the rest.
				return;
			}

			// Fetch the origin timestamp.
			// Check if we're still in the lockout period - if so, reschedule
			// for the end of the lockout.
			var timestamp = GetTimestamp(context.Connection, context.BackgroundJob.Job);

			if (timestamp.HasValue) {
				// If within the lockout period, reschedule out to the expiration.
				if (DateTimeOffset.UtcNow <= timestamp.Value.Add(_delay)) {
					context.CandidateState = new ScheduledState(timestamp.Value.Add(_delay).DateTime) { Reason = $"Delayed {_seconds} seconds by the debounce filter." };
				}
			}
		}

		/// <summary>
		/// Hangfire filter event called once the action has completed.
		/// </summary>
		/// <param name="filterContext"></param>
		public void OnPerformed(PerformedContext filterContext) {
			RemoveFingerPrint(filterContext.Connection, filterContext.BackgroundJob.Job);
		}

		/// <summary>
		/// Fetch the debounce starting timestamp.
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="job"></param>
		/// <returns></returns>
		DateTimeOffset? GetTimestamp(IStorageConnection connection, Job job) {
			var fingerprint = connection.GetAllEntriesFromHash(GetFingerPrintKey(job));

			if (fingerprint != null
				&& fingerprint.ContainsKey("Timestamp")
				&& DateTimeOffset.TryParse(fingerprint["Timestamp"], null, DateTimeStyles.RoundtripKind, out DateTimeOffset timestamp)) {
				return timestamp;
			}

			return null;
		}

		/// <summary>
		/// Remove the fingerprint for the job.
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="job"></param>
		void RemoveFingerPrint(IStorageConnection connection, Job job) {
			using (connection.AcquireDistributedLock(GetFingerPrintLockKey(job), LockTimeout))
			using (var transaction = connection.CreateWriteTransaction()) {
				transaction.RemoveHash(GetFingerPrintKey(job));
				transaction.Commit();
			}
		}

		/// <summary>
		/// Build the fingerprint for the given job. The format is:
		/// {class}.{method}.{params}
		/// </summary>
		/// <param name="job"></param>
		/// <returns></returns>
		string GetFingerPrint(Job job) {
			//if (FingerPrintFormat != null) {
			//	return String.Format(FingerPrintFormat, job.Args).Substring(0, 87);
			//}

			// Cannot fingerprint anon funcs.
			if (job.Type == null || job.Method == null) {
				return string.Empty;
			}

			//TODO: Need to make this scalable so not to limit the condition to angular score
			//var args = new List<string>();
			//foreach (var obj in job.Args.ToList()) {
			//	if (obj is IEnumerable) {
			//		var subarr = new List<string>();
			//		foreach (var i in (IEnumerable)obj) {
			//			subarr.Add(i.ToString());
			//		}
			//		args.Add("{{[" + string.Join(",",subarr) + "]}}");
			//	} else {
			//		args.Add("{{" + obj.ToString() + "}}");

			//	}

			//}

			//var parameters = "[[" + string.Join(",", args) + "]]";
			//if (string.Equals(FingerPrintFormat, "AngularScore", StringComparison.OrdinalIgnoreCase)) {
			//	foreach (var obj in job.Args.ToList()) {
			//		var scores = (List<long>)obj;
			//		var ids = scores.Select(s => s).ToArray();
			//		parameters = string.Join(".", ids);
			//	}
			//} else {
			//	parameters = Guid.NewGuid().ToString();
			//}
			var parameters = JsonConvert.SerializeObject(job.Args);
			var bytes = Encoding.UTF8.GetBytes(parameters);
			var sha1 = System.Security.Cryptography.SHA1.Create();
			var hash = sha1.ComputeHash(bytes);
			var hashStr = Convert.ToBase64String(hash);
			var res = String.Format(FingerPrintFormat, hashStr);
			res.Substring(0, Math.Min(87, res.Length));
			return res;


			// Return the unique key. Truncate it as the Hangfire database
			// column is only 100 wide.
			//var fingerPrintFormat = String.Join(".", job.Type.Name, job.Method.Name, parameters);
		;
			//return fingerPrintFormat;
		}

		private string GetFingerPrintLockKey(Job job) {
			return String.Format("{0}:lock", GetFingerPrintKey(job));
		}

		private string GetFingerPrintKey(Job job) {
			return String.Format("fingerprint:{0}", GetFingerPrint(job));
		}

		void IClientFilter.OnCreated(CreatedContext filterContext) { }
		void IServerFilter.OnPerforming(PerformingContext filterContext) { }
	}
}