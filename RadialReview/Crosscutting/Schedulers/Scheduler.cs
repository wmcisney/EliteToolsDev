﻿using Hangfire;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Schedulers {

	/// <summary>
	/// Mock with using(Scheduler.Mock()) 
	/// Copied from BackgroundJob
	/// </summary>
	public class Scheduler {

		public static IMockScheduler Mock() {
			return Mock(new MockScheduler(false));
		}
		public static IMockScheduler MockAndExecute() {
			return Mock(new MockScheduler(true));
		}
		public static IMockScheduler Mock(IMockScheduler mockScheduler) {
			MockWith(mockScheduler);
			return mockScheduler;
		}
		public static IBackgroundJobClient Advanced() {
			return GetSingleton();
		}

		//
		// Summary:
		//     Creates a new fire-and-forget job based on a given method call expression.
		//
		// Parameters:
		//   methodCall:
		//     Method call expression that will be marshalled to a server.
		//
		// Returns:
		//     Unique identifier of a background job.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     methodCall is null.
		public static string Enqueue([InstantHandle][NotNull] Expression<Func<Task>> methodCall) {
			return GetSingleton().Enqueue(methodCall);
		}
		//
		// Summary:
		//     Creates a new fire-and-forget job based on a given method call expression.
		//
		// Parameters:
		//   methodCall:
		//     Method call expression that will be marshalled to a server.
		//
		// Returns:
		//     Unique identifier of a background job.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     methodCall is null.
		public static string Enqueue([InstantHandle][NotNull] Expression<Action> methodCall) {
			return GetSingleton().Enqueue(methodCall);
		}
		//
		// Summary:
		//     Creates a new fire-and-forget job based on a given method call expression.
		//
		// Parameters:
		//   methodCall:
		//     Method call expression that will be marshalled to a server.
		//
		// Returns:
		//     Unique identifier of a background job.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     methodCall is null.
		public static string Enqueue<T>([InstantHandle][NotNull] Expression<Func<T, Task>> methodCall) {
			return GetSingleton().Enqueue(methodCall);
		}
		//
		// Summary:
		//     Creates a new fire-and-forget job based on a given method call expression.
		//
		// Parameters:
		//   methodCall:
		//     Method call expression that will be marshalled to a server.
		//
		// Returns:
		//     Unique identifier of a background job.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     methodCall is null.
		public static string Enqueue<T>([InstantHandle][NotNull] Expression<Action<T>> methodCall) {
			return GetSingleton().Enqueue(methodCall);
		}

		/// <summary>
		/// Creates a new background job based on a specified method
		/// call expression and schedules it to be enqueued after a given delay.
		/// </summary>
		/// 
		/// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
		/// <param name="delay">Delay, after which the job will be enqueued.</param>
		/// <returns>Unique identifier of the created job.</returns>
		public static string Schedule([NotNull, InstantHandle] Expression<Action> methodCall, TimeSpan delay) {
			return GetSingleton().Schedule(methodCall, delay);
		}

		/// <summary>
		/// Creates a new background job based on a specified method
		/// call expression and schedules it to be enqueued after a given delay.
		/// </summary>
		/// 
		/// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
		/// <param name="delay">Delay, after which the job will be enqueued.</param>
		/// <returns>Unique identifier of the created job.</returns>
		public static string Schedule([NotNull, InstantHandle] Expression<Func<Task>> methodCall, TimeSpan delay) {
			return GetSingleton().Schedule(methodCall, delay);
		}

		/// <summary>
		/// Creates a new background job based on a specified method call expression
		/// and schedules it to be enqueued at the given moment of time.
		/// </summary>
		/// 
		/// <param name="methodCall">Method call expression that will be marshalled to the Server.</param>
		/// <param name="enqueueAt">The moment of time at which the job will be enqueued.</param>
		/// <returns>Unique identifier of a created job.</returns>
		public static string Schedule([NotNull, InstantHandle] Expression<Action> methodCall, DateTimeOffset enqueueAt) {
			return GetSingleton().Schedule(methodCall, enqueueAt);
		}

		/// <summary>
		/// Creates a new background job based on a specified method call expression
		/// and schedules it to be enqueued at the given moment of time.
		/// </summary>
		/// 
		/// <param name="methodCall">Method call expression that will be marshalled to the Server.</param>
		/// <param name="enqueueAt">The moment of time at which the job will be enqueued.</param>
		/// <returns>Unique identifier of a created job.</returns>
		public static string Schedule(
			[NotNull, InstantHandle] Expression<Func<Task>> methodCall, DateTimeOffset enqueueAt) {
			return GetSingleton().Schedule(methodCall, enqueueAt);
		}

		/// <summary>
		/// Creates a new background job based on a specified instance method
		/// call expression and schedules it to be enqueued after a given delay.
		/// </summary>
		/// 
		/// <typeparam name="T">Type whose method will be invoked during job processing.</typeparam>
		/// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
		/// <param name="delay">Delay, after which the job will be enqueued.</param>
		/// <returns>Unique identifier of the created job.</returns>
		public static string Schedule<T>([NotNull, InstantHandle] Expression<Action<T>> methodCall, TimeSpan delay) {
			return GetSingleton().Schedule(methodCall, delay);
		}

		/// <summary>
		/// Creates a new background job based on a specified instance method
		/// call expression and schedules it to be enqueued after a given delay.
		/// </summary>
		/// 
		/// <typeparam name="T">Type whose method will be invoked during job processing.</typeparam>
		/// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
		/// <param name="delay">Delay, after which the job will be enqueued.</param>
		/// <returns>Unique identifier of the created job.</returns>
		public static string Schedule<T>([NotNull, InstantHandle] Expression<Func<T, Task>> methodCall, TimeSpan delay) {
			return GetSingleton().Schedule(methodCall, delay);
		}

		/// <summary>
		/// Creates a new background job based on a specified method call expression
		/// and schedules it to be enqueued at the given moment of time.
		/// </summary>
		/// 
		/// <typeparam name="T">The type whose method will be invoked during the job processing.</typeparam>
		/// <param name="methodCall">Method call expression that will be marshalled to the Server.</param>
		/// <param name="enqueueAt">The moment of time at which the job will be enqueued.</param>
		/// <returns>Unique identifier of a created job.</returns>
		public static string Schedule<T>([NotNull, InstantHandle] Expression<Action<T>> methodCall, DateTimeOffset enqueueAt) {
			return GetSingleton().Schedule(methodCall, enqueueAt);
		}

		/// <summary>
		/// Creates a new background job based on a specified method call expression
		/// and schedules it to be enqueued at the given moment of time.
		/// </summary>
		/// 
		/// <typeparam name="T">The type whose method will be invoked during the job processing.</typeparam>
		/// <param name="methodCall">Method call expression that will be marshalled to the Server.</param>
		/// <param name="enqueueAt">The moment of time at which the job will be enqueued.</param>
		/// <returns>Unique identifier of a created job.</returns>
		public static string Schedule<T>([NotNull, InstantHandle] Expression<Func<T, Task>> methodCall, DateTimeOffset enqueueAt) {
			return GetSingleton().Schedule(methodCall, enqueueAt);
		}




		private static IBackgroundJobClient SingletonClient { get; set; }
		private static IBackgroundJobClient PrevSingletonClient { get; set; }
		private static IBackgroundJobClient GetSingleton() {
			if (SingletonClient == null) {
				SingletonClient = new BackgroundJobClient();
			}

			return SingletonClient;
		}

		protected static void MockWith(IBackgroundJobClient overrideWith) {
			if (PrevSingletonClient != null) {
				throw new Exception("Scheduler already mocked");
			}

			PrevSingletonClient = SingletonClient;
			SingletonClient = overrideWith;

		}
		protected static void Unmock() {
			SingletonClient = PrevSingletonClient;
			PrevSingletonClient = null;
		}

		private class MockScheduler : IMockScheduler {
			private bool Disposed { get; set; }
			private int CurrentId = -1;
			private bool ShouldExecute { get; set; }
			public Dictionary<string, Job> JobsLookup = new Dictionary<string, Job>();

			public MockScheduler(bool shouldExecute) {
				ShouldExecute = shouldExecute;
			}

			private void CheckDisposed() {
				if (Disposed) {
					throw new Exception("Cannot access disposed Scheduler.");
				}
			}

			public void Dispose() {
				if (ShouldExecute) {
					foreach (var job in JobsLookup) {
						var activator = JobActivator.Current;
						job.Value.Perform(activator, new JobCancellationToken(false));
					}
				}


				CheckDisposed();
				Disposed = true;
				Scheduler.Unmock();
			}

			public string Create([NotNull] Job job, [NotNull] IState state) {
				CheckDisposed();
				CurrentId += 1;
				var key = "" + CurrentId;
				JobsLookup.Add(key, job);
				return key;
			}

			public bool ChangeState([NotNull] string jobId, [NotNull] IState state, [CanBeNull] string expectedState) {
				CheckDisposed();
				throw new NotImplementedException();
			}

			public Job GetJob(string jobName) {
				CheckDisposed();
				return JobsLookup.GetOrDefault(jobName, null);
			}

			public void RemoveJob(string jobName) {
				CheckDisposed();
				JobsLookup.Remove(jobName);
			}

			public T Perform<T>(string jobName) {
				CheckDisposed();
				var job = GetJob(jobName);
				if (job == null) {
					throw new Exception("Job does not exist");
				}

				var performer = new BackgroundJobPerformer();
				var backgroundJob = new BackgroundJob(jobName, job, DateTime.UtcNow);
				var performContext = new PerformContext(new MockStorageConnection(), backgroundJob, new JobCancellationToken(false));
				try {
					var result = performer.Perform(performContext);
					return (T)result;
				} catch (JobPerformanceException e) {
					throw e.InnerException;
				}
			}

			public class MockStorageConnection : IStorageConnection {
				public string GetJobParameter(string id, string name) {
					return null;
				}
				#region not implemented
				public IDisposable AcquireDistributedLock(string resource, TimeSpan timeout) {
					throw new NotImplementedException();
				}
				public void AnnounceServer(string serverId, ServerContext context) {
					throw new NotImplementedException();
				}
				public string CreateExpiredJob(Job job, IDictionary<string, string> parameters, DateTime createdAt, TimeSpan expireIn) {
					throw new NotImplementedException();
				}
				public IWriteOnlyTransaction CreateWriteTransaction() {
					throw new NotImplementedException();
				}
				public void Dispose() {
					throw new NotImplementedException();
				}
				public IFetchedJob FetchNextJob(string[] queues, CancellationToken cancellationToken) {
					throw new NotImplementedException();
				}
				public Dictionary<string, string> GetAllEntriesFromHash([NotNull] string key) {
					throw new NotImplementedException();
				}
				public HashSet<string> GetAllItemsFromSet([NotNull] string key) {
					throw new NotImplementedException();
				}
				public string GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore) {
					throw new NotImplementedException();
				}
				public JobData GetJobData([NotNull] string jobId) {
					throw new NotImplementedException();
				}
				public StateData GetStateData([NotNull] string jobId) {
					throw new NotImplementedException();
				}
				public void Heartbeat(string serverId) {
					throw new NotImplementedException();
				}
				public void RemoveServer(string serverId) {
					throw new NotImplementedException();
				}
				public int RemoveTimedOutServers(TimeSpan timeOut) {
					throw new NotImplementedException();
				}
				public void SetJobParameter(string id, string name, string value) {
					throw new NotImplementedException();
				}
				public void SetRangeInHash([NotNull] string key, [NotNull] IEnumerable<KeyValuePair<string, string>> keyValuePairs) {
					throw new NotImplementedException();
				}
				#endregion
			}

		}
	}

	public interface IMockScheduler : IBackgroundJobClient, IDisposable {
		Job GetJob(string jobName);
		T Perform<T>(string jobName);
	}
}