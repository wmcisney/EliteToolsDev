using Hangfire;
using log4net;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Hangfire;
using SlackAPI;
using System;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Integrations {
	public class Slack {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		private static SlackTaskClient client;


		private static bool lck = false;

		public static void SendNotification(string message, string details = null) {
			string instance;
			string env = "" + Config.GetAwsEnv();
			try {
				instance = Config.IsLocal() ? "local" : Amazon.Util.EC2InstanceMetadata.InstanceId.ToString();
			} catch (Exception) {
				instance = "err";
			}

			var channel = Config.GetSlackNotificationsChannel();
			if (channel == null) {
				return;
			}

			if (!string.IsNullOrWhiteSpace(details)) {
				details = "\n```" + details + "```";
			} else {
				details = "";
			}

			var m = String.Format(
				"*{0,10} {1,15} {2,13}* - _{3}_{4}",
				$"[{env}]",
				$"[{instance}]",
				DateTime.Now.ToString("HH:mm:ss.ffff"),
				message,
				details
			);
			SendMessage(m, channel);
		}

		private static void SendMessage(string message, string channel = null) {
			try {
				log.Info($"SlackNotification [{channel}]: " + message);
				Scheduler.Enqueue(() => SendMessage_Hangfire(message, channel));
			} catch (Exception e) {
				log.Error("Slack notification error",e);
			}
		}

		[Queue(HangfireQueues.Immediate.SEND_SLACK_MESSAGE)]
		[AutomaticRetry(Attempts = 0)]
		public static async Task<bool> SendMessage_Hangfire(string message, string channel) {
			message = message ?? "";
			channel = channel ?? "tt-notifications";
			var i = 0;
			while (lck) {
				if (i > 1000) {
					throw new Exception("Slack locked. Aborting after 10 seconds.");
				}
				i += 1;
				await Task.Delay(10);
			}
			try {
				lck = true;
				if (client == null) {
					client = new SlackTaskClient(Config.GetSlackAuthToken());
				}
				if (client.Channels == null) {
					await client.ConnectAsync();
				}
				Conversation c = client.Groups.Find(x => x.name.Equals(channel));
				if (c == null) {
					c = client.Channels.Find(x => x.name.Equals(channel));
				}
				if (c == null) {
					var user = client.Users.Find(x => x.name == channel);
					if (user != null) {
						var userId = user.id;
						c = client.DirectMessages.Find(x => x.user.Equals(userId));
					}
				}

				if (c == null) {
					return false;
				}

				var mr = await client.PostMessageAsync(c.id, message);
				if (mr.ok) {
					log.Info($"Slack Post (@{channel}): {message}");
					return true;
				} else {
					log.Info($"Slack Post Failed (@{channel}): {message}");
					return false;
				}
			} catch (Exception e) {
				log.Error($"Slack message failed to send (@{channel}): {message}", e);
				return false;
			} finally {
				lck = false;
			}
		}
	}
}