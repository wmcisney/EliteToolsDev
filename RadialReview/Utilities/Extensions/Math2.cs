using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
	public static class Math2
	{

		public static T ArgMax<T, U>(this IEnumerable<T> list, Func<T, U> selector) where U : IComparable {
			if (!list.Any())
				return default(T);

			var argMax = list.First();
			var max = selector(argMax);

			foreach (var i in list) {
				var cur = selector(i);
				if (cur.CompareTo(max) > 0) {
					max = cur;
					argMax = i;
				}
			}
			return argMax;
		}

		public static T ArgMin<T, U>(this IEnumerable<T> list, Func<T, U> selector) where U : IComparable {
			if (!list.Any())
				return default(T);

			var argMin = list.First();
			var min = selector(argMin);

			foreach (var i in list) {
				var cur = selector(i);
				if (cur.CompareTo(min) < 0) {
					min = cur;
					argMin = i;
				}
			}
			return argMin;
		}


		public static DateTime Min(DateTime d1, params DateTime[] dates)
		{
			var min = dates.Select(d => d.Ticks).Concat(new[]{d1.Ticks}).Min();
			return new DateTime(min);
		}
		public static DateTime Max(DateTime d1, params DateTime[] dates)
		{
			var max = dates.Select(d => d.Ticks).Concat(new[]{d1.Ticks}).Max();
			return new DateTime(max);
		}


		public static double Coerce(double val, double low, double high)
		{
			return Math.Max(Math.Min(high, val), low);
		}

		public static DateTime? Min(IEnumerable<DateTime> dates) {
			if (dates.Any()) {
				return Min(DateTime.MaxValue, dates.ToArray());
			}
			return null;
		}
		public static DateTime? Max(IEnumerable<DateTime> dates) {
			if (dates.Any()) {
				return Max(DateTime.MinValue, dates.ToArray());
			}
			return null;
		}


		public static TimeSpan Min(TimeSpan d1, params TimeSpan[] dates) {
			var min = dates.Select(d => d.Ticks).Concat(new[] { d1.Ticks }).Min();
			return new TimeSpan(min);
		}
		public static TimeSpan Max(TimeSpan d1, params TimeSpan[] dates) {
			var max = dates.Select(d => d.Ticks).Concat(new[] { d1.Ticks }).Max();
			return new TimeSpan(max);
		}

	}

}