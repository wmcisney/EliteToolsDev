using log4net;
using NHibernate.Cfg;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Utilities.NHibernate {

	public class Alteration {
		public Alteration(string tABLE_NAME, string cOLUMN_NAME, string rEFERENCED_TABLE_NAME, string rEFERENCED_COLUMN_NAME, string incorrectForeignKey, string correctedForeignKey) {
			if (string.IsNullOrWhiteSpace(tABLE_NAME)) {
				throw new ArgumentException("message", nameof(tABLE_NAME));
			}

			if (string.IsNullOrWhiteSpace(cOLUMN_NAME)) {
				throw new ArgumentException("message", nameof(cOLUMN_NAME));
			}

			if (string.IsNullOrWhiteSpace(rEFERENCED_TABLE_NAME)) {
				throw new ArgumentException("message", nameof(rEFERENCED_TABLE_NAME));
			}

			if (string.IsNullOrWhiteSpace(rEFERENCED_COLUMN_NAME)) {
				throw new ArgumentException("message", nameof(rEFERENCED_COLUMN_NAME));
			}

			if (string.IsNullOrWhiteSpace(incorrectForeignKey)) {
				throw new ArgumentException("message", nameof(incorrectForeignKey));
			}

			if (string.IsNullOrWhiteSpace(correctedForeignKey)) {
				throw new ArgumentException("message", nameof(correctedForeignKey));
			}

			TABLE_NAME = tABLE_NAME;
			COLUMN_NAME = cOLUMN_NAME;
			REFERENCED_TABLE_NAME = rEFERENCED_TABLE_NAME;
			REFERENCED_COLUMN_NAME = rEFERENCED_COLUMN_NAME;
			IncorrectForeignKey = incorrectForeignKey;
			CorrectedForeignKey = correctedForeignKey;
		}

		public string TABLE_NAME { get; set; }
		public string COLUMN_NAME { get; set; }
		public string REFERENCED_TABLE_NAME { get; set; }
		public string REFERENCED_COLUMN_NAME { get; set; }

		public string IncorrectForeignKey { get; set; }
		public string CorrectedForeignKey { get; set; }

	}

	public class AuditForeignKeyInterceptor {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


		public class AuditTableAlteration {
			public string TableName { get; set; }
			public string IncorrectForeignKey { get; set; }
			public string CorrectedForeignKey { get; set; }
		}

		public static void Intercept(Configuration config, bool execute) {

			var alterationLookup = AuditForeignKeyInterceptorData.Alterations.ToDefaultDictionary(x => x.IncorrectForeignKey, x => x, x => null);
			var seen = new HashSet<string>();
			var i = 0;
			var dups = 0;
			foreach (var map in config.ClassMappings) {
				foreach (var key in map.IdentityTable.ForeignKeyIterator) {
					var found = alterationLookup[key.Name];
					if (found != null) {
						if (found.TABLE_NAME == key.Table.Name &&
							found.IncorrectForeignKey == key.Name &&
							found.COLUMN_NAME == key.Columns.FirstOrDefault().NotNull(x => x.Name) &&
							((
								key.ReferencedEntityName == "NHibernate.Envers.DefaultRevisionEntity" &&
								found.REFERENCED_TABLE_NAME == "REVINFO" &&
								found.REFERENCED_COLUMN_NAME == "REV"
							  ) || (
								found.REFERENCED_TABLE_NAME == key.ReferencedTable.NotNull(x => x.Name) &&
								found.REFERENCED_COLUMN_NAME == key.ReferencedColumns.FirstOrDefault().NotNull(x => x.Name)
							))
						) {
							var keyName = key.Name;
							var foundName = found.CorrectedForeignKey;
							i++;
							if (seen.Contains(key.Name)) {
								dups += 1;
							}
							seen.Add(key.Name);
							if (execute) {
								key.Name = found.CorrectedForeignKey;
							}
						}
					}
				}
			}
			if (execute) {
				log.Info("NHiberation Mapping Alterations Applied: " + i + "/" + AuditForeignKeyInterceptorData.Alterations.Count + " Duplicates:" + dups);
			} else {
				log.Info("NHiberation Mapping Alterations Found (Not Applied): " + i + "/" + AuditForeignKeyInterceptorData.Alterations.Count + " Duplicates:" + dups);
			}
		}
	}
}