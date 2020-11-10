using NHibernate;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities {

	public class ServerSettings {
		public static ServerSettings<T> Create<T>(Func<ISession, T> getter, TimeSpan? duration = null) {
			;
			return new ServerSettings<T>(getter, duration);
		}
	}

	public class ServerSettings<T> {
		public static readonly TimeSpan DefaultRefreshDuration = TimeSpan.FromMinutes(3);

		private TimeSpan Duration { get; set; }
		private DateTime? LastCheck { get; set; }
		private T Settings { get; set; }
		private Func<ISession, T> Getter { get; set; }
		private object lck { get; set; }

		public ServerSettings(Func<ISession, T> getter, TimeSpan? duration = null) {
			Duration = duration ?? DefaultRefreshDuration;
			Getter = getter;
			lck = new object();
		}

		public void Reset() {
			LastCheck = null;
		}

		public DateTime GetLastCheck() {
			return LastCheck ?? DateTime.MinValue;
		}

		[Obsolete("May need to call commit")]
		public T Get(ISession s) {
			if (LastCheck == null || LastCheck.Value.Add(Duration) < DateTime.UtcNow) {
				lock (lck) {
					if (LastCheck == null || LastCheck.Value.Add(Duration) < DateTime.UtcNow) {
						Settings = Getter(s);
						LastCheck = DateTime.UtcNow;
					}
				}
			}
			return Settings;
		}

		public T Get() {
			if (LastCheck == null || LastCheck.Value.Add(Duration) < DateTime.UtcNow) {
				lock (lck) {
					if (LastCheck == null || LastCheck.Value.Add(Duration) < DateTime.UtcNow) {
						using (var s = HibernateSession.GetCurrentSession()) {
							using (var tx = s.BeginTransaction()) {
								Settings = Getter(s);
								tx.Commit();
								s.Flush();
							}
						}
						LastCheck = DateTime.UtcNow;
					}
				}
			}
			return Settings;
		}

	}

}