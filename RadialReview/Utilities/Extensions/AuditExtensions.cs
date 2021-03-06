﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate.Envers;
using NHibernate.Envers.Exceptions;
using log4net;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Envers.Query;

namespace RadialReview.Utilities.Extensions {

	public static class AuditExtensions {
		public static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public class Revision<T> {
			public long RevisionId { get; set; }
			public DateTime Date { get; set; }
			public T Object { get; set; }
		}

		public class RevisionDiff<T> {
			public Revision<T> Before { get; set; }
			public Revision<T> After { get; set; }
		}

		public static RevisionDiff<T> FindNearestDiff<T>(this IAuditReader self, object id, Func<T, T, bool> oldNew) {
			var revisions = self.GetRevisions(typeof(T), id).OrderByDescending(x => x).ToList();

			if (!revisions.Any())
				return null;

			var after = self.Find<T>(id, revisions[0]);

			if (revisions.Count == 1)
				return null;

			for (var i = 1; i < revisions.Count; i++) {
				var before = self.Find<T>(id, revisions[i]);
				if (oldNew(before, after))
					return new RevisionDiff<T>() {
						After = new Revision<T> {
							Date = self.GetRevisionDate(revisions[i - 1]),
							Object = after,
							RevisionId = revisions[i - 1]
						},
						Before = new Revision<T> {
							Date = self.GetRevisionDate(revisions[i]),
							Object = before,
							RevisionId = revisions[i]
						},
					};
				after = before;
			}
			return null;
		}

		public static void _GetRevisionsBetween<T>(this IAuditReader self, object id, DateTime start, DateTime end) {
			//var r = DefaultRevisionEntity;
			var revisions = self.CreateQuery()
				.ForRevisionsOfEntity(typeof(T), false, true)
				.Add(AuditEntity.Id().Eq(id))
				.Add(AuditEntity.RevisionProperty("RevisionDate").Ge(start))
                .Add(AuditEntity.RevisionProperty("RevisionDate").Lt(end))
                .GetResultList();
			int a = 0;
		}

		//[Obsolete("Does not work",true)]
		public static IEnumerable<Revision<T>> GetRevisionsBetween<T>(this IAuditReader self, ISession session, object id, DateTime start, DateTime end) where T : class {
			if (start > end)
				throw new ArgumentOutOfRangeException("start", "Start must come before end.");
			
			start = start.AddSeconds(-1);
			end = end.AddSeconds(1);

			var revisionModels = self.CreateQuery()
				.ForHistoryOf<T, DefaultRevisionEntity>(true)
				.Add(AuditEntity.Id().Eq(id))				
				.Results();
		

			var revisions = revisionModels.Select(x=>x.RevisionEntity).ToList();
			var revisionIds = revisions.Where(x => start <= x.RevisionDate && x.RevisionDate <= end).OrderBy(x => x.RevisionDate).ToList();

			//     ----|--> ------> --->|
			//----x----|---x-------x----|---x------

			//Still need to add the one before the start.
			var startId = start;
			if (revisionIds.Any())
				startId = revisionIds.First().RevisionDate;
			var additional = revisions.Where(x => x.RevisionDate < startId).ToList();
			if (additional.Any()) {
				revisionIds.Add(additional.ArgMax(x=>x.RevisionDate));
			}
			if (!revisionIds.Any())
				return new List<Revision<T>>();
			var low = revisionIds.Min(x=>x.RevisionDate);
			var high = revisionIds.Max(x => x.RevisionDate);

			revisionModels = revisionModels.Where(x => low <= x.RevisionEntity.RevisionDate && x.RevisionEntity.RevisionDate <= high).ToList();

			return revisionModels.Select(x => new Revision<T>() {
				Date = x.RevisionEntity.RevisionDate,
				RevisionId = x.RevisionEntity.Id,
				Object = x.Entity
			});
		}


		[Obsolete("Too slow",true)]
		public static IEnumerable<Revision<T>> GetRevisionsBetween_old<T>(this IAuditReader self, ISession session, object id, DateTime start, DateTime end) where T : class {
			if (start > end)
				throw new ArgumentOutOfRangeException("start", "Start must come before end.");
			long s;
			long e;

			start = start.AddSeconds(-1);
			end = end.AddSeconds(1);

			try {
				s = self.GetRevisionNumberForDate(start);
			} catch (RevisionDoesNotExistException) {
				s = 1;
			}
			try {
				e = self.GetRevisionNumberForDate(end);
			} catch (RevisionDoesNotExistException) {
				e = 1;
			}
			if (s > e)
				throw new ArgumentOutOfRangeException("start", "Start must come before end.");

			var revisions = self.GetRevisions(typeof(T), id).ToList();
			var revisionIds = revisions.Where(x => s <= x && x <= e).OrderBy(x => x).ToList();

			//     ----|--> ------> --->|
			//----x----|---x-------x----|---x------

			//Still need to add the one before the start.
			var startId = s;
			if (revisionIds.Any())
				startId = revisionIds.First();
			var additional = revisions.Where(x => x < startId).ToList();
			if (additional.Any()) {
				revisionIds.Add(additional.Max());
			}
			if (!revisionIds.Any())
				return new List<Revision<T>>();
			var low = revisionIds.Min();
			var high = revisionIds.Max();

			var revisionModels = self.CreateQuery()
				.ForHistoryOf<T, DefaultRevisionEntity>(true)
				.Add(AuditEntity.Id().Eq(id))
				.Add(AuditEntity.RevisionNumber().Ge(low))
				.Add(AuditEntity.RevisionNumber().Le(high))				
				.Results();
			
			return revisionModels.Select(x => new Revision<T>() {
				Date = x.RevisionEntity.RevisionDate,
				RevisionId = x.RevisionEntity.Id,
				Object = x.Entity
			});
		}
	}
}