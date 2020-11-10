using Hangfire;
using NHibernate;
using NHibernate.Criterion;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Exceptions;
using RadialReview.Hangfire;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Enums;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Payments;
using RadialReview.Models.Periods;
using RadialReview.Models.Prereview;
using RadialReview.Models.Tasks;
using RadialReview.Models.Todo;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
	public class TaskAccessor : BaseAccessor {


		[Queue(HangfireQueues.Immediate.DAILY_TASKS)]
		[AutomaticRetry(Attempts = 0)]
		public static async Task<bool> GenerateRockPeriods_Hangfire(DateTime now) {
			var any = false;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var orgs = s.QueryOver<OrganizationModel>().Where(x => x.DeleteTime == null).List().ToList();

					var tomorrow = DateTime.UtcNow.Date.AddDays(7);
					foreach (var o in orgs) {
						var o1 = o;
						var period = s.QueryOver<PeriodModel>().Where(x => x.OrganizationId == o1.Id && x.DeleteTime == null && x.StartTime <= tomorrow && tomorrow < x.EndTime).List().ToList();

						if (!period.Any()) {

							var startOfYear = (int)o.Settings.StartOfYearMonth;

							if (startOfYear == 0) {
								startOfYear = 1;
							}

							var start = new DateTime(tomorrow.Year - 2, startOfYear, 1);

							//var curM = (int)o.Settings.StartOfYearMonth;
							//var curY = tomorrow.Year;
							//var last = 
							var quarter = 0;
							var prev = start;
							while (true) {
								start = start.AddMonths(3);
								quarter += 1;
								var tick = start.AddDateOffset(o.Settings.StartOfYearOffset);
								if (tick > tomorrow) {
									break;
								}
								prev = start;
							}

							var p = new PeriodModel() {
								StartTime = prev.AddDateOffset(o.Settings.StartOfYearOffset),
								EndTime = start.AddDateOffset(o.Settings.StartOfYearOffset).AddDays(-1),
								Name = prev.AddDateOffset(o.Settings.StartOfYearOffset).Year + " Q" + (((quarter + 3) % 4) + 1),// +3 same as -1
								Organization = o,
								OrganizationId = o.Id,
							};

							s.Save(p);
							any = true;
						}
					}
					//await EventUtil.GenerateAllDailyEvents(s, DateTime.UtcNow);
					tx.Commit();
					s.Flush();
				}
			}
			return any;
		}

		public static async Task CleanupSyncs_Hangfire() {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					try {
						var syncTable = "Sync";
						s.CreateSQLQuery("delete from " + syncTable + " where CreateTime < \"" + DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd") + "\"").ExecuteUpdate();
						DeleteOldSyncLocks(s);
						tx.Commit();
						s.Flush();

					} catch (Exception e) {
						log.Error(e);
					}
				}
			}
		}

		public static void DeleteOldSyncLocks(ISession s) {
			var syncTable = "SyncLock";
			s.CreateSQLQuery("delete from " + syncTable + " where LastClientUpdateTimeMs < \"" + DateTime.UtcNow.AddDays(-1).ToJavascriptMilliseconds() + "\"").ExecuteUpdate();
		}



		[Queue(HangfireQueues.Immediate.EXECUTE_TASKS)]
		[AutomaticRetry(Attempts = 0)]
		public static async Task CheckCardExpirations_Hangfire() {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var date = DateTime.UtcNow.Date;
					if (date == new DateTime(date.Year, date.Month, 1) || date == new DateTime(date.Year, date.Month, 15) || date == new DateTime(date.Year, date.Month, 21)) {
						var expireMonth = date.AddMonths(1);
						var tokens = s.QueryOver<PaymentSpringsToken>()
								.Where(x => x.Active && x.DeleteTime == null && x.TokenType == PaymentSpringTokenType.CreditCard && x.MonthExpire == expireMonth.Month && x.YearExpire == expireMonth.Year)
								.List().ToList();

						var tt = tokens.GroupBy(x => x.OrganizationId).Select(x => x.OrderByDescending(y => y.CreateTime).First());
						foreach (var t in tt) {
							await HooksRegistry.Each<IPaymentHook>((ses, x) => x.CardExpiresSoon(ses, t));
						}
					}

					tx.Commit();
					s.Flush();
				}
			}
		}


		[Queue(HangfireQueues.Immediate.EXECUTE_TASKS)]
		[AutomaticRetry(Attempts = 0)]
		public static async Task<object> EnqueueTasks(DateTime now) {
			var nowV = now;
			var tasks = GetTasksToExecute(nowV, false);

			foreach (var t in tasks) {
				try {
					//Must be started here, otherwise it can be queued up several times
					//Start the task ONLY
					var taskId = t.Id;
					using (var s = HibernateSession.GetCurrentSession()) {
						using (var tx = s.BeginTransaction()) {
							var task = s.Get<ScheduledTask>(taskId);
							if (task.Executed != null) {
								continue;//Already executed
							}
							if (task.Started != null) {
								continue; //Already started
							}
							task.Started = nowV;
							tx.Commit();
							s.Flush();
						}
					}
					Scheduler.Enqueue(() => ExecuteTask(t.Id, nowV, false));
				} catch (Exception e) {
					log.Error("ExecuteTask - Task execution exception.", e);
				}
			}

			return new {
				Number = tasks.Count
			};
		}


		[Obsolete("Used only in testing")]  
		public static async Task<ExecutionResult> EnqueueTask_Test(ScheduledTask t, DateTime now) {
			//MarkStarted(tasks, now);
			//return await _ExecuteTasks(tasks, now, d_ExecuteTaskFunc_Test);
			var output = new List<ExecutionResult>();
			//foreach (var t in tasks) {
			//var t = task;
			try {
				//Must be started here, otherwise it can be queued up several times
				//Start the task ONLY
				var taskId = t.Id;
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						var task = s.Get<ScheduledTask>(taskId);
						if (task.Executed != null) {
							return null;//Already executed
						}
						if (task.Started != null) {
							return null; //Already started
						}
						task.Started = now;
						tx.Commit();
						s.Flush();
					}
				}
				return await ExecuteTask(t.Id, now, true);
			} catch (Exception e) {
				log.Error("ExecuteTask - Task execution exception.", e);
				throw;
			}
			//}
			//return output;
		}

		[Queue(HangfireQueues.Immediate.EXECUTE_TASK)]
		[AutomaticRetry(Attempts = 0)]
		public static async Task<ExecutionResult> ExecuteTask(long taskId, DateTime now, bool test) {
			var start = DateTime.UtcNow;
			string taskUrl = null;
			ExecutionStatus status = ExecutionStatus.Unstarted;
			List<Exception> errors = new List<Exception>();
			bool errorEmailSent = false;
			var emailsToSend = new List<Mail>();
			var createdTasks = new List<ScheduledTask>();
			dynamic response = null;

			#region Execute Task
			try {
				//Execute the task and update the task ONLY
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						//TaskResult results = null;
						var task = s.Get<ScheduledTask>(taskId);
						taskUrl = task.Url;
						if (task.Executed != null) {
							errors.Add(new Exception("Task already executed"));
							return new ExecutionResult(taskId, status, taskUrl, response, start, DateTime.UtcNow, createdTasks, errors, errorEmailSent);
						}

						//Error Handling
						if (task.Started == null) {
							errors.Add(new Exception("Task was not started"));
							return new ExecutionResult(taskId, status, taskUrl, response, start, DateTime.UtcNow, createdTasks, errors, errorEmailSent);
						}
						if ((int)(task.Started.Value.ToJsMs() / 1000) != (int)(now.ToJsMs() / 1000)) {
							//Somehow it was started in another task.. this one is not ours.			
							errors.Add(new Exception("Task already started"));
							return new ExecutionResult(taskId, status, taskUrl, response, start, DateTime.UtcNow, createdTasks, errors, errorEmailSent);
						}

						//Execute function
						ExecuteTaskFunc func;
						if (test) {
							func = d_ExecuteTaskFunc_Test;
						} else {
							func = d_ExecuteTaskFunc;
						}
						status |= ExecutionStatus.Started;
						//Heavy Lifting...
						var results = await ExecuteTask_Internal(task, now, func);

						response = results.Response;
						errorEmailSent = results.ExceptionEmailSent;
						errors.AddRange(results.Errors);
						emailsToSend.AddRange(results.SendEmails);
						createdTasks.AddRange(results.CreateTasks);

						s.Update(task);
						tx.Commit();
						s.Flush();

						if (results.Executed) {
							status |= ExecutionStatus.Executed;
						} else {
							errors.Add(new Exception("Task failed to execute"));
							return new ExecutionResult(taskId, status, taskUrl, response, start, DateTime.UtcNow, createdTasks, errors, errorEmailSent);
						}
					}
				}
			} catch (Exception e) {
				log.Error("ExecuteTask - Task execution exception.", e);
				errors.Add(e);
			}
			#endregion
			#region Unmark started
			try {
				//Update started ONLY
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						var task = s.Get<ScheduledTask>(taskId);
						task.Started = null;
						s.Update(task);
						tx.Commit();
						s.Flush();
					}
				}
				status |= ExecutionStatus.Unmarked;
			} catch (Exception e) {
				log.Error("ExecuteTask - Task Remove Started exception.", e);
				errors.Add(e);
			}
			#endregion
			#region Save new tasks
			try {
				//Add newly created tasks ONLY
				var allFailed = false;
				if (createdTasks.Any()) {
					allFailed = true;
					using (var s = HibernateSession.GetCurrentSession()) {
						using (var tx = s.BeginTransaction()) {

							var existing = s.QueryOver<ScheduledTask>()
								.WhereRestrictionOn(x => x.CreatedFromTaskId)
								.IsIn(createdTasks.Select(x => x.CreatedFromTaskId).ToArray())
								.List().GroupBy(x => x.CreatedFromTaskId)
								.ToDefaultDictionary(x => x.Key, x => x.ToList(), x => new List<ScheduledTask>());

							foreach (var c in createdTasks) {
								var found = existing[c.CreatedFromTaskId];

								if (!found.Any(x => x.Url == c.Url && (int)(x.Fire.ToJsMs() / 1000) == (int)(c.Fire.ToJsMs() / 1000))) {
									s.Save(c);
									allFailed = false;
								} else {
									log.Error("ExecuteTask - Prevented duplicate task from being added");
								}
							}
							tx.Commit();
							s.Flush();
						}
					}
				}
				if (!allFailed) {
					status |= ExecutionStatus.TasksCreated;
				}
			} catch (Exception e) {
				log.Error("ExecuteTask - Task Creation exception.", e);
				errors.Add(e);
			}
			#endregion
			#region Send emails			
			try {
				if (emailsToSend.Any()) {
					log.Info("ExecuteTasks - Sending (" + emailsToSend.Count + ") emails " + DateTime.UtcNow);
					await Emailer.SendEmails(emailsToSend);
					log.Info("ExecuteTasks - Done sending emails " + DateTime.UtcNow);
				}
				status |= ExecutionStatus.EmailsSent;
			} catch (Exception e) {
				log.Error("ExecuteTasks - Task execution exception. Email failed (2).", e);
				errors.Add(e);
			}

			#endregion
			var result = new ExecutionResult(taskId, status, taskUrl, response, start, DateTime.UtcNow, createdTasks, errors, errorEmailSent);//Somehow it was started in another task.. this one is not ours.
			if (!result.Status.HasFlag(ExecutionStatus.Executed) && result.HasError) {
				throw result.ToException();
			}
			return result;

		}


		/*
		protected static async Task<List<ExecutionResult>> _ExecuteTasks(List<ScheduledTask> tasks, DateTime now, ExecuteTaskFunc executeTaskFunc) {
			var toCreate = new List<ScheduledTask>();
			var emails = new List<Mail>();
			var res = new List<ExecutionResult>();
			log.Info("ExecuteTasks - Starting Execute Tasks (" + tasks.Count + ") " + DateTime.UtcNow);
			try {
				//MarkStarted(tasks, now);
				var groupedTasks = tasks
					.Select((task, i) => new { task, i })
					.GroupBy(x => (int)(x.i / 4))
					.ToList();

				var results = new List<TaskResult>();
				foreach (var group in groupedTasks) {
					var groupResults = await Task.WhenAll(
						group.Select(a => {
							var task = a.task;
							try {
								return ExecuteTask_Internal(task, now, executeTaskFunc);
							} catch (Exception e) {
								log.Error("ExecuteTasks - Task execution exception.", e);
								return null;
							}
						})
					);
					results.AddRange(groupResults);
				}
				toCreate = results.Where(x => x != null).SelectMany(x => x.CreateTasks).ToList();
				emails = results.Where(x => x != null).SelectMany(x => x.SendEmails).ToList();
				res = results.Where(x => x != null).Select(x => {
					x.Result.NewTasks = x.CreateTasks;
					return x.Result;
				}).ToList();
			} finally {
				UnmarkStarted(tasks);
			}
			log.Info("ExecuteTasks - Ending Execute Task " + DateTime.UtcNow);

			//Sessions Must be separated.
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					foreach (var c in toCreate)
						s.Save(c);
					foreach (var task in tasks)
						s.Update(task);
					tx.Commit();
					s.Flush();
				}
			}

			log.Info("ExecuteTasks - UpdateScorecard " + DateTime.UtcNow);
			UpdateScorecard(now);
			log.Info("ExecuteTasks - Sending (" + emails.Count + ") emails " + DateTime.UtcNow);
			try {
				await Emailer.SendEmails(emails);
			} catch (Exception e) {
				log.Error("ExecuteTasks - Task execution exception. Email failed (2).", e);
			}
			log.Info("ExecuteTasks - Done sending emails " + DateTime.UtcNow);
			return res;
		}*/

		protected delegate Task<dynamic> ExecuteTaskFunc(string server, ScheduledTask task, DateTime now);
		protected static async Task<TaskResult> ExecuteTask_Internal(ScheduledTask task, DateTime now, ExecuteTaskFunc execute) {
			var o = new TaskResult();
			var newTasks = o.CreateTasks;
			//var sr = o.Result;
			//sr.TaskId = task.Id;

			if (task != null) {
				//sr.StartTime = DateTime.UtcNow;
				try {
					if (task.Url != null) {
						try {
							o.Response = await execute(Config.BaseUrl(null), task, now);
						} catch (WebException webEx) {
							var response = webEx.Response as HttpWebResponse;
							if (response != null && response.StatusCode == HttpStatusCode.NotImplemented) {
								//Fallthrough Exception...
								log.Info("Task Fallthrough [OK] (taskId:" + task.Id + ") (url:" + task.Url + ")");
							} else {
								throw webEx;
							}
						}
					}
					//sr.EndTime = DateTime.UtcNow;
					//sr.DurationMs = (sr.EndTime.Value - sr.StartTime.Value).TotalMilliseconds;
					log.Debug("Scheduled task was executed. " + task.Id);
					task.Executed = DateTime.UtcNow;
					o.Executed = true;
					if (task.NextSchedule != null) {
						var nt = new ScheduledTask() {
							FirstFire = (task.FirstFire ?? task.Fire).AddTimespan(task.NextSchedule.Value),
							Fire = (task.FirstFire ?? task.Fire).AddTimespan(task.NextSchedule.Value),
							NextSchedule = task.NextSchedule,
							Url = task.Url,
							TaskName = task.TaskName,
							MaxException = task.MaxException,
							OriginalTaskId = task.OriginalTaskId,
							CreatedFromTaskId = task.Id,
							EmailOnException = task.EmailOnException,
						};
						while (nt.Fire < DateTime.UtcNow) {
							nt.Fire = nt.Fire.AddTimespan(task.NextSchedule.Value);
							if (nt.FirstFire != null) {
								nt.FirstFire = nt.FirstFire.Value.AddTimespan(task.NextSchedule.Value);
							}
						}

						newTasks.Add(nt);
					}
				} catch (Exception e) {
					o.Errors.Add(e);
					//if (sr.EndTime == null) {
					//	sr.EndTime = DateTime.UtcNow;
					//	sr.DurationMs = (sr.EndTime.Value - sr.StartTime.Value).TotalMilliseconds;
					//}
					//if (e != null) {
					//	sr.ErrorType = "" + e.GetType();
					//	sr.ErrorMessage = e.Message;
					//}
					//sr.Url = task.Url;
					//sr.HasError = true;
					log.Error("Scheduled task error. " + task.Id, e);

					//Send an email
					if (task != null && task.EmailOnException) {
						try {
							var builder = new StringBuilder();
							builder.AppendLine("TaskId:" + task.Id + "<br/>");
							if (e != null) {
								builder.AppendLine("ExceptionType:" + e.GetType() + "<br/>");
								builder.AppendLine("Exception:" + e.Message + "<br/>");
								builder.AppendLine("ExceptionCount:" + task.ExceptionCount + " out of " + task.MaxException + "<br/>");
								builder.AppendLine("Url:" + task.Url + "<br/>");
								builder.AppendLine("StackTrace:<br/>" + (e.StackTrace ?? "").Replace("\n", "\n<br/>") + "<br/>");
							} else {
								builder.AppendLine("Exception was null <br/>");
							}
							var mail = Mail.To(EmailTypes.CustomerSupport, ProductStrings.EngineeringEmail)
								.SubjectPlainText("Task failed to execute. Action Required.")
								.BodyPlainText(builder.ToString());

							o.SendEmails.Add(mail);
							o.ExceptionEmailSent = true;
						} catch (Exception ee) {
							o.Errors.Add(ee);
							log.Error("Task execution exception. Email failed (1).", ee);
						}
					}
					task.ExceptionCount++;
					if (task.MaxException != null && task.ExceptionCount >= task.MaxException) {
						if (task.NextSchedule != null) {
							newTasks.Add(new ScheduledTask() {
								FirstFire = (task.FirstFire ?? task.Fire).Add(task.NextSchedule.Value),
								Fire = (task.FirstFire ?? task.Fire).Add(task.NextSchedule.Value),
								NextSchedule = task.NextSchedule,
								Url = task.Url,
								MaxException = task.MaxException,
								TaskName = task.TaskName,
								OriginalTaskId = task.OriginalTaskId,
								CreatedFromTaskId = task.Id,
							});
							task.Executed = DateTime.MaxValue;
						}
					}
					task.Fire = DateTime.UtcNow + TimeSpan.FromMinutes(5 + Math.Pow(2, task.ExceptionCount + 1));
				}
			}
			return o;
		}
		public static async Task<dynamic> d_ExecuteTaskFunc(string server, ScheduledTask task, DateTime _unused) {
			using (var webClient = new WebClient()) {
				var url = "";
				if (server != null) {
					url = (server.TrimEnd('/')) + "/";
				}
				url = url + task.Url.TrimStart('/');
				if (url.Contains("?")) {
					url += "&taskId=" + task.Id;
				} else {
					url += "?taskId=" + task.Id;
				}

				var str = await webClient.DownloadStringTaskAsync(new Uri(url, UriKind.Absolute));
				return str;
			}
		}
		protected static async Task<dynamic> d_ExecuteTaskFunc_Test(string _unused, ScheduledTask task, DateTime now) {
			var t = task;
			var url = t.Url;
			string[] parts = url.Split(new char[] { '?', '&' });
			var query = parts.ToDictionary(x => x.Split('=')[0].ToLower(), x => x.Split('=').ToList().Skip(1).LastOrDefault());
			var path = parts[0];
			var pathParts = path.Split('/');

			//BaseController b=null;
			if (path.StartsWith("/Scheduler/ChargeAccount/")) {
				//var sc = new SchedulerController();
				//var re = await sc.ChargeAccount(pathParts.Last().ToLong(), task.Id/*,executeTime:now.ToJavascriptMilliseconds()*/);
				return await PaymentAccessor.EnqueueChargeOrganizationFromTask(pathParts.Last().ToLong(), task.Id, true, executeTime: now);
			} else if (url == "https://example.com") {
				using (var webClient = new WebClient()) {
					return await webClient.DownloadStringTaskAsync(new Uri(url, UriKind.Absolute));
				}
			} else {
				throw new Exception("Unhandled URL: " + url);
			}

			//if (b != null) {
			//	if (b.Response.StatusCode != 200)
			//		throw new Exception("Status code was " + b.Response.StatusCode);
			//}

		}


		public static List<ScheduledTask> GetTasksToExecute(DateTime now, bool markStarted) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction(IsolationLevel.Serializable)) {
					//var all = s.QueryOver<ScheduledTask>().List().ToList();
					var current = s.QueryOver<ScheduledTask>().Where(x => x.Executed == null && x.Started == null && now >= x.Fire && x.DeleteTime == null && x.ExceptionCount <= 11 && (x.MaxException == null || x.ExceptionCount < x.MaxException)).List()
						.Where(x => x.ExceptionCount < (x.MaxException ?? 12))
						.ToList();

					if (markStarted) {
						var d = DateTime.UtcNow;
						foreach (var c in current) {
							c.Started = d;
						}
						tx.Commit();
						s.Flush();
					}

					return current;
				}
			}
		}


		public static long AddTask(AbstractUpdate update, ScheduledTask task) {
			update.Save(task);
			task.OriginalTaskId = task.Id;
			update.Update(task);
			return task.Id;
		}

		public static long AddTask(ScheduledTask task) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var output = AddTask(s.ToUpdateProvider(), task);
					tx.Commit();
					s.Flush();
					return output;
				}
			}
		}

		public static void UnmarkStarted(List<ScheduledTask> tasks) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction(IsolationLevel.Serializable)) {
					foreach (var t in tasks) {
						t.Started = null;
						s.Update(t);
					}
					tx.Commit();
					s.Flush();
				}
			}
		}
		[Obsolete("Do not use", true)]
		public static void MarkStarted(List<ScheduledTask> tasks, DateTime? date) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) { ////HEY ... IsolationLevel.Serializable ??
					foreach (var t in tasks) {
						t.Started = date;
						s.Update(t);
					}
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static int GetUnstartedTaskCountForUser(ISession s, long forUserId, DateTime now) {
			return new Cache().GetOrGenerate(CacheKeys.UNSTARTED_TASKS, ctx => {
				var profileImage = 0;
				try {
					profileImage = String.IsNullOrEmpty(s.Get<UserOrganizationModel>(forUserId).User.ImageGuid) ? 1 : 0;
				} catch {
				}
				var reviewCount = s.QueryOver<ReviewModel>().Where(x => x.ReviewerUserId == forUserId && x.DueDate > now && !x.Complete && x.DeleteTime == null).Select(Projections.RowCount()).FutureValue<int>();
				var prereviewCount = s.QueryOver<PrereviewModel>().Where(x => x.ManagerId == forUserId && x.PrereviewDue > now && !x.Started && x.DeleteTime == null).Select(Projections.RowCount()).FutureValue<int>();
				var nowPlus = now.Add(TimeSpan.FromDays(1));
				var todoCount = s.QueryOver<TodoModel>().Where(x => x.AccountableUserId == forUserId && x.DueDate < nowPlus && x.CompleteTime == null && x.DeleteTime == null).Select(Projections.RowCount()).FutureValue<int>();
				//var scorecardCount = s.QueryOver<ScoreModel>().Where(x => x.AccountableUserId == forUserId && x.DateDue < nowPlus && x.DateEntered == null).Select(Projections.RowCount()).FutureValue<int>();
				var total = reviewCount.Value + prereviewCount.Value /*+ scorecardCount.Value */+ profileImage + todoCount.Value;
				return total;
			});
		}

		public static List<TaskModel> GetTasksForUser(UserOrganizationModel caller, long forUserId, DateTime now) {
			var tasks = new List<TaskModel>();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);

					//Reviews
					var reviews = ReviewAccessor
						.GetReviewsForUser(s, perms, caller, forUserId, 0, int.MaxValue, now)
						.ToListAlive()
						.GroupBy(x => x.ForReviewContainerId);

					var reviewTasks = reviews.Select(x => new TaskModel() {
						Id = x.First().ForReviewContainerId,
						Type = TaskType.Review,
						Completion = CompletionModel.FromList(x.Select(y => y.GetCompletion())),
						DueDate = x.Max(y => y.DueDate),
						Name = x.First().Name
					});
					tasks.AddRange(reviewTasks);

					//Prereviews
					var prereviews = PrereviewAccessor.GetPrereviewsForUser(s.ToQueryProvider(true), perms, forUserId, now)
						.Where(x => x.Executed == null).ToListAlive();
					var reviewContainers = new Dictionary<long, String>();
					var prereviewCount = new Dictionary<long, int>();
					foreach (var p in prereviews) {
						reviewContainers[p.ReviewContainerId] = ReviewAccessor.GetReviewContainer(s.ToQueryProvider(true), perms, p.ReviewContainerId).ReviewName;
						prereviewCount[p.Id] = s.QueryOver<PrereviewMatchModel>()
							.Where(x => x.PrereviewId == p.Id && x.DeleteTime == null)
							.RowCount();
					}
					var prereviewTasks = prereviews.Select(x => new TaskModel() {
						Id = x.Id,
						Type = TaskType.Prereview,
						Count = prereviewCount[x.Id],
						DueDate = x.PrereviewDue,
						Name = reviewContainers[x.ReviewContainerId]
					});
					tasks.AddRange(prereviewTasks);
					var todos = TodoAccessor.GetTodosForUser(caller, caller.Id).Where(x =>
						//x.DeleteTime == null &&
						(x.CompleteTime == null && x.DueDate < DateTime.UtcNow.AddDays(7)) //|| 
																						   //(x.DueDate > DateTime.UtcNow.AddDays(-1) && x.DueDate< DateTime.UtcNow.AddDays(1))
					).ToList();

					var todoTasks = todos.Select(x => new TaskModel() {
						Id = x.Id,
						Type = TaskType.Todo,
						DueDate = x.DueDate ?? DateTime.UtcNow.AddDays(7),
						Name = x.Name,
					});
					tasks.AddRange(todoTasks);

					//Scorecard
					/*var scores = s.QueryOver<ScoreModel>()
						.Where(x => x.AccountableUserId == forUserId && x.DateDue < now.AddDays(1) && x.DateEntered == null)
						.List().ToList();

					var scoreTasks = scores.GroupBy(x => x.DateDue.Date).Select(x => new TaskModel()
					{
						Count = x.Count(),
						DueDate = x.First().DateDue,
						Name = "Enter Scorecard Metrics",
						Type = TaskType.Scorecard
					});
					tasks.AddRange(scoreTasks);*/

					try {
						if (String.IsNullOrEmpty(s.Get<UserOrganizationModel>(forUserId).User.ImageGuid)) {
							tasks.Add(new TaskModel() {
								Type = TaskType.Profile,
								Name = "Update Profile (Picture)",
								DueDate = DateTime.MaxValue,
							});
						}
					} catch {

					}


					/*

					  .Where(x => x.Executed == null).ToListAlive();

					foreach (var p in prereviews)
					{
						reviewContainers[p.ReviewContainerId] = ReviewAccessor.GetReviewContainer(s.ToQueryProvider(true), perms, p.ReviewContainerId).ReviewName;
						prereviewCount[p.Id] = s.QueryOver<PrereviewMatchModel>()
							.Where(x => x.PrereviewId == p.Id && x.DeleteTime == null)
							.RowCount();
					}
					var prereviewTasks = prereviews.Select(x => new TaskModel()
					{
						Id = x.Id,
						Type = TaskType.Prereview,
						Count = prereviewCount[x.Id],
						DueDate = x.PrereviewDue,
						Name = reviewContainers[x.ReviewContainerId]
					});*/

				}
			}
			return tasks;
		}

		public static void UpdateScorecard(DateTime now) {
			//using (var s = HibernateSession.GetCurrentSession()) {
			//	using (var tx = s.BeginTransaction()) {
			//		var measurables = s.QueryOver<MeasurableModel>().Where(x => x.DeleteTime == null && x.NextGeneration <= now).List().ToList();

			//		//var weekLookup = new Dictionary<long, DayOfWeek>();

			//		//Next Thursday
			//		foreach (var m in measurables) {

			//			//var startOfWeek =weekLookup.GetOrAddDefault(m.OrganizationId, x => m.Organization.Settings.WeekStart);

			//			var nextDue = m.NextGeneration.StartOfWeek(DayOfWeek.Sunday).AddDays(7).AddDays((int)m.DueDate).Add(m.DueTime);

			//			var score = new ScoreModel() {
			//				AccountableUserId = m.AccountableUserId,
			//				DateDue = nextDue,
			//				MeasurableId = m.Id,
			//				Measurable = m,
			//				OrganizationId = m.OrganizationId,
			//				ForWeek = nextDue.StartOfWeek(DayOfWeek.Sunday),
			//				OriginalGoal = m.Goal,
			//				OriginalGoalDirection = m.GoalDirection
			//			};
			//			s.Save(score);
			//			m.NextGeneration = nextDue;
			//			s.Update(m);
			//		}
			//		tx.Commit();
			//		s.Flush();
			//	}
			//}
		}

		public static void EnsureTaskIsExecuting(ISession s, long taskId) {
			var task = s.Get<ScheduledTask>(taskId);
			if (task.Executed != null) {
				throw new PermissionsException("Task was already executed.");
			}

			if (task.DeleteTime != null) {
				throw new PermissionsException("Task was deleted.");
			}

			if (task.OriginalTaskId == 0) {
				throw new PermissionsException("ScheduledTask OriginalTaskId was 0.");
			}

			if (task.Started == null) {
				throw new PermissionsException("Task was not started.");
			}
		}


		[Queue(HangfireQueues.Immediate.CLOSE_MEETING)]
		[AutomaticRetry(Attempts = 0)]
		public static async Task<bool> CloseMeeting(long recurrenceId) {
			await L10Accessor.ConcludeMeeting(UserOrganizationModel.ADMIN, recurrenceId, new List<Tuple<long, decimal?>>(), ConcludeSendEmail.None, false, false, null);
			return true;
		}
	}
}