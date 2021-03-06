﻿using RadialReview.AliveExtensions;
using RadialReview.Exceptions;
using RadialReview.Models.Application;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI.WebControls.Expressions;
using static RadialReview.EnumerableExtensions;

namespace RadialReview {
	public class AliveEnumerable<TSource> : IEnumerable<TSource> {
		public IEnumerable<TSource> Wrapped { get; set; }

		public AliveEnumerable(IEnumerable<TSource> source) {
			Wrapped = source;
		}

		public IEnumerator<TSource> GetEnumerator() {
			return Wrapped.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return Wrapped.GetEnumerator();
		}
	}


	public static class EnumerableExtensions {

		public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items, int maxItems) {
			return items.Select((item, inx) => new { item, inx })
						.GroupBy(x => x.inx / maxItems)
						.Select(g => g.Select(x => x.item));
		}

		public static IEnumerable<T> Duplicates<T>(this IEnumerable<T> items) {
			return items.GroupBy(x => x)
					  .Where(g => g.Count() > 1)
					  .Select(y => y.Key);
		}

		public static double Variance<T>(this IEnumerable<T> items, Func<T, double> selector) {
			var values = items.Select(selector);
			double avg = values.Average();
			return values.Average(v => (v - avg) * (v - avg));
		}


		public static double? Median<TColl, TValue>(this IEnumerable<TColl> source, Func<TColl, TValue> selector) {
			return source.Select(selector).Median();
		}

		public static double? Median<T>(this IEnumerable<T> source) {
			if (Nullable.GetUnderlyingType(typeof(T)) != null) {
				source = source.Where(x => x != null);
			}

			var count = source.Count();
			if (count == 0) {
				return null;
			}

			source = source.OrderBy(n => n);

			var midpoint = count / 2;
			if (count % 2 == 0) {
				return (Convert.ToDouble(source.ElementAt(midpoint - 1)) + Convert.ToDouble(source.ElementAt(midpoint))) / 2.0;
			} else {
				return Convert.ToDouble(source.ElementAt(midpoint));
			}
		}

		public static DefaultDictionary<TKey, TVal> ToDefaultDictionary<TSource, TKey, TVal>(this IEnumerable<TSource> items, Func<TSource, TKey> keySelector, Func<TSource, TVal> valueSelector, Func<TKey, TVal> defaultVal = null) {
			defaultVal = defaultVal ?? (x => default(TVal));
			var dict = new DefaultDictionary<TKey, TVal>(defaultVal);
			foreach (var i in items) {
				dict[keySelector(i)] = valueSelector(i);
			}
			return dict;
		}



		public static ILazyCollection ToLazyCollection<T>(this IEnumerable<T> self) {
			return new LazyCollection<T>(self);
		}

		public static bool AllSame<T, TProp>(this IEnumerable<T> self, Func<T, TProp> selector, out TProp property) {
			var hash = new HashSet<TProp>(self.Select(selector));
			var output = hash.Count == 1;
			if (output) {
				property = hash.First();
			} else {
				property = default(TProp);
			}

			return output;
		}

		public static TProp EnsureAllSame<T, TProp>(this IEnumerable<T> self, Func<T, TProp> selector) {
			TProp o;
			if (AllSame(self, selector, out o)) {
				return o;
			}
			throw new PermissionsException("All entries must be the same.");
		}


		public static bool AllSame<T, TProp>(this IEnumerable<T> self, Func<T, TProp> selector) {
			TProp dud;
			return AllSame(self, selector, out dud);
		}

		public static List<SelectListItem> ToSelectList<T>(this IEnumerable<T> self, Func<T, object> textSelector, Func<T, object> idSelector, object selected = default(object), bool anyNull = false) {
			return self.Select((x, i) => {
				var id = idSelector(x);
				var text = textSelector(x);
				var isSelected = id.Equals(selected);
				if (!anyNull && (selected == null || selected.Equals(default(object)))) {
					isSelected = i == 0;
				}

				return new SelectListItem() { Selected = isSelected, Text = text.NotNull(y => y.ToString()), Value = id.NotNull(y => y.ToString()) };
			}).ToList();
		}

		public static List<SelectListItem> ToSelectList(this IEnumerable<NameId> self, long? selected = null) {
			var foundSelected = self.FirstOrDefault(x => x.Id == selected);
			return self.ToSelectList(x => x.Name, x => x.Id, foundSelected);
		}

		public static List<T> AsList<T>(this T first, params T[] after) {
			var output = new List<T> { first };
			output.AddRange(after);
			return output;
		}

		public static IEnumerable<TResult> SelectAlive<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, TResult> selector) where TSource : IDeletable {
			return source.Alive().Select(selector);
		}
		public static IEnumerable<TResult> SelectAlive<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) where TSource : IDeletable {
			return source.Alive().Select(selector);
		}

		public static IEnumerable<T> Alive<T>(this IEnumerable<T> source) {
			if (source == null) {
				return null;
			}

			/*if (source is AliveEnumerable<T>)
                return source;*/
			return source.Where(x => x.Alive());
			//return new AliveEnumerable<T>(source.Where(x => x.Alive()));
		}

		public static List<TSource> ToListAlive<TSource>(this IEnumerable<TSource> source) where TSource : IDeletable {
			return source.Alive().ToList();
		}

		public static IEnumerable<TSource> UnionBy<TSource, TProp>(this IEnumerable<TSource> first, Func<TSource, TProp> keySelector, params IEnumerable<TSource>[] remaining) {
			var enumerables = first;
			foreach (var r in remaining) {
				first = first.Concat(r);
			}
			return first.GroupBy(keySelector).Select(x => x.First());
		}
		public static IEnumerable<TSource> Except<TSource, TProp>(this IEnumerable<TSource> first, IEnumerable<TSource> except, Func<TSource, TProp> keySelector) {
			/*var enumerables = first;
			foreach (var r in remaining)
			{
				first = first.Concat(r);
			}
			return first.GroupBy(keySelector).Select(x => x.First());*/
			var other = except.Select(y => keySelector);

			return first.Where(x => {
				var cur = keySelector(x);
				return other.Any(y => y.Equals(cur));
			});
		}

		public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source) {
			var rng = new Random();
			return source.Shuffle(rng);
		}

		public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rng) {
			if (source == null) {
				throw new ArgumentNullException("source");
			}

			if (rng == null) {
				throw new ArgumentNullException("rng");
			}

			return source.ShuffleIterator(rng);
		}

		private static IEnumerable<T> ShuffleIterator<T>(
			this IEnumerable<T> source, Random rng) {
			var buffer = source.ToList();

			for (int i = 0; i < buffer.Count; i++) {
				int j = rng.Next(i, buffer.Count);
				yield return buffer[j];

				buffer[j] = buffer[i];
			}
		}

		public static IEnumerable<T> Distinct<T, TProp>(this IEnumerable<T> source, Func<T, TProp> distinction) {
			return source.GroupBy(distinction).Select(x => x.First());
		}

		public static bool ContainsAny<T>(this IEnumerable<T> source, IEnumerable<T> other) {
			return source.Any(other.Contains);
		}

		public static bool ContainsAll<T>(this IEnumerable<T> source, IEnumerable<T> other) {
			return source.ContainsAll(other, x => x);
		}

		public static bool ContainsAll<T, TProp>(this IEnumerable<T> source, IEnumerable<T> other, Func<T, TProp> selector) {
			var sourceMod = source.Select(selector).ToList();

			return other.All(i => sourceMod.Contains(selector(i)));
		}

		public static void EnsureContainsAll<T>(this IEnumerable<T> source, IEnumerable<T> other) {
			source.EnsureContainsAll(other, x => x);
		}

		public static void EnsureContainsAll<T, TProp>(this IEnumerable<T> source, IEnumerable<T> other, Func<T, TProp> selector) {
			if (!source.ContainsAll(other, selector)) {
				throw new PermissionsException("Item is required in source.");
			}
		}

		public static IEnumerable<T> FilterRangeRestricted<T>(this IEnumerable<T> source, DateRange range, DateRangeType rangeType = DateRangeType.Any) where T : IDeletable {
			if (range == null) {
				return source.Where(x => x.DeleteTime == null);
			}

			return source.FilterRange(range, x => DateTime.MinValue, x => x.DeleteTime, rangeType);
		}

		public static IEnumerable<T> FilterRange<T>(this IEnumerable<T> source, DateRange range, DateRangeType rangeType = DateRangeType.Any) where T : IHistorical {
			if (range == null) {
				return source.Where(x => x.DeleteTime == null);
			}

			return source.FilterRange(range, x => x.CreateTime, x => x.DeleteTime, rangeType);
		}

		private static IEnumerable<T> FilterRange<T>(this IEnumerable<T> source, DateRange range, Func<T, DateTime> createTime, Func<T, DateTime?> deleteTime, DateRangeType rangeType = DateRangeType.Any) {
			if (range == null) {
				return source;
			}

			switch (rangeType) {
				case DateRangeType.Any:
					return source.Where(x => {
						var del = deleteTime(x);
						return createTime(x) <= range.EndTime && (del == null || del.Value >= range.StartTime);
					});
				case DateRangeType.All:
					return source.Where(x => {
						var del = deleteTime(x);
						return createTime(x) <= range.StartTime && (del == null || del.Value >= range.EndTime);
					});
				default:
					throw new ArgumentOutOfRangeException("rangeType");
			}
		}

		public static IEnumerable<OUT> SelectNoException<IN, OUT>(this IEnumerable<IN> self, Func<IN, OUT> lambda) {
			return self.SelectMany(x => {
				try {
					return lambda(x).AsList();
				} catch (Exception) {
					//Eat it..
				}
				return new List<OUT>();
			});
		}


		/*public static Boolean Contains<T>(this IEnumerable<T> enumerable, Func<T, Boolean> contains)
        {
            return enumerable.FirstOrDefault(contains) != null;
        }*/

	}
}