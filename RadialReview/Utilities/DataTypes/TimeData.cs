﻿using RadialReview.Models.Application;
using RadialReview.Models.Scorecard;
using System;

namespace RadialReview.Utilities.DataTypes {
	public class TimeData : TimeSettings, ITimeData {
		public DayOfWeek WeekStart { get; set; }
		public ScorecardPeriod Period { get; set; }
		public int TimezoneOffset { get; set; }
		public YearStart YearStart { get; set; }
		public DateTime Now { get; set; }
		public bool Descending { get; set; }
		public string DateFormat { get; set; }

		public DateTime ConvertFromServerTime(DateTime serverTime) {
			return ConvertFromServerTime(serverTime, TimezoneOffset);
		}

		public DateTime ConvertToServerTime(DateTime localTime) {
			return ConvertToServerTime(localTime, TimezoneOffset);
		}

		public ITimeData GetTimeSettings() {
			return this;
		}

		public TimeData() {
			DateFormat = "MM-dd-yyyy";
		}


		public static DateTime ConvertToServerTime(DateTime localTime, int timezoneOffset) {
			try {
				return localTime.AddMinutes(-timezoneOffset);
			} catch (ArgumentOutOfRangeException) {
				if (-timezoneOffset > 0) {
					return DateTime.MaxValue;
				}
				return DateTime.MinValue;
			}
		}
		public static DateTime ConvertFromServerTime(DateTime serverTime, int timezoneOffset) {
			try {
				return serverTime.AddMinutes(timezoneOffset);
			} catch (ArgumentOutOfRangeException) {
				if (timezoneOffset > 0) {
					return DateTime.MaxValue;
				}
				return DateTime.MinValue;
			}
		}

		public static int GetTimezoneOffset(string timeZoneId) {
			var zone = timeZoneId ?? "Central Standard Time";
			var ts = TimeZoneInfo.FindSystemTimeZoneById(zone);
			return (int)ts.GetUtcOffset(DateTime.UtcNow).TotalMinutes;
		}
	}

	public interface TimeSettings {
		ITimeData GetTimeSettings();
	}

	public interface ITimeData : TimeSettings {
		DayOfWeek WeekStart { get; set; }
		ScorecardPeriod Period { get; }
		int TimezoneOffset { get; }
		YearStart YearStart { get; }
		DateTime Now { get; }
		bool Descending { get; set; }
		string DateFormat { get; }
		DateTime ConvertFromServerTime(DateTime serverTime);
		DateTime ConvertToServerTime(DateTime localTime);
	}
}
