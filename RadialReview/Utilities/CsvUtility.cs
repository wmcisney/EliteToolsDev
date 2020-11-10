using RadialReview.Utilities.DataTypes;
using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Web;
using System.Text.RegularExpressions;
using System.Text;
using System.Data;

namespace RadialReview.Utilities {

	public class DataTableHelper {
		private string[,] Data { get; set; }

		public string Name { get; private set; }

		protected DataTableHelper(string name, string[,] data) {
			Data = data;
			Name = name;
		}
		public DataTableHelper(DataTable table) {
			Name = table.TableName;

			var rows = table.Rows.Count;
			var columns = 0;

			for (var r = 0; r < table.Rows.Count; r++) {
				var row = table.Rows[r];
				columns = Math.Max(columns, row.ItemArray.Length);
			}

			Data = new string[rows, columns];
			for (var r = 0; r < table.Rows.Count; r++) {
				var row = table.Rows[r];
				var c = 0;
				foreach (var cell in row.ItemArray) {
					try {
						Data[r, c] = cell.NotNull(x => "" + x);
					} catch (Exception) {
					}
					c++;
				}
			}
		}

		public DataTableHelper GetRow(int row) {
			if (row >= 0 && row < Data.GetLength(0)) {
				var rr = new string[1, Data.GetLength(1)];
				for (var c = 0; c < Data.GetLength(1); c++) {
					rr[0, c] = Data[row, c];
				}
				return new DataTableHelper(Name, rr);
			} else {
				throw new NotImplementedException("Row out of range");
			}
		}

		public DataTableHelper ExcludeRows(Func<DataTableHelper, bool> whereRow) {
			var res = this;
			for (var i = Data.GetLength(0) - 1; i >= 0; i--) {
				if (whereRow(this.GetRow(i))) {
					res = res.ExcludeRow(i);
				}
			}
			return res;
		}

		public DataTableHelper ExcludeRow(int row) {
			if (row >= 0 && row < Data.GetLength(0)) {
				var newData = new string[Data.GetLength(0) - 1, Data.GetLength(1)];
				var i = 0;
				for (var r = 0; r < Data.GetLength(0) - 1; r++) {
					if (r == row) {
						i += 1;
					}
					for (var c = 0; c < Data.GetLength(1); c++) {
						newData[r, c] = Data[i, c];
					}
					i++;
				}
				return new DataTableHelper(Name, newData);
			} else {
				throw new NotImplementedException("Row out of range");
			}
		}

		public int[] GetColumnStartingUnder(RowCol cell) {
			if (cell == null)
				return null;
			if (!InRange(cell.Row + 1, cell.Col))
				return null;
			var x1 = cell.Col;
			var y1 = cell.Row + 1;
			var x2 = cell.Col;
			var y2 = Data.GetLength(0) - 1;
			var count = (x2 - x1 + 1) * (y2 - y1 + 1);
			return new int[] { x1, y1, x2, y2, count };
		}
		public int[] GetRowStartingRight(RowCol cell) {
			if (cell == null)
				return null;
			if (!InRange(cell.Row, cell.Col + 1))
				return null;
			var x1 = cell.Col + 1;
			var y1 = cell.Row;
			var x2 = Data.GetLength(1) - 1;
			var y2 = cell.Row;

			var count = (x2 - x1 + 1) * (y2 - y1 + 1);
			return new int[] { x1, y1, x2, y2, count };
		}

		public class RowCol {
			public RowCol(int row, int col) {
				Row = row;
				Col = col;
			}

			public int Row { get; set; }
			public int Col { get; set; }
		}

		public RowCol Find(string search, RowCol after = null) {
			var r = after.NotNull(x => x.Row);
			var c = after.NotNull(x => x.Col);

			for (; r < Data.GetLength(0); r++) {
				for (; c < Data.GetLength(1); c++) {
					if (Data[r, c].Contains(search)) {
						return new RowCol(r, c);
					}
				}
				c = 0;
			}
			return null;
		}

		public string ToCsv() {
			var sb = new StringBuilder();
			for (var r = 0; r < Data.GetLength(0); r++) {
				for (var c = 0; c < Data.GetLength(1); c++) {
					sb.Append(Data[r, c]).Append(",");
				}
				sb.AppendLine();
			}
			return sb.ToString();
		}

		public string Get(int row, int col) {
			if (InRange(row, col)) {
				return Data[row, col] ?? "";
			}
			return "";
		}

		private bool InRange(int row, int col) {
			return 0 <= row && 0 <= col && row < Data.GetLength(0) && col < Data.GetLength(1);
		}
	}


	public class CsvUtility {
		public static List<List<String>> Load(Stream stream, bool trim = true) {
			stream.Seek(0, SeekOrigin.Begin);
			TextReader tr = new StreamReader(stream);
			//var csv = new CsvReader(tr);
			var csvData = new List<List<string>>();
			var lowest = 0;
			int fieldCount;
			using (var csv = new LumenWorks.Framework.IO.Csv.CsvReader(tr, false)) {
				fieldCount = csv.FieldCount;
				while (csv.ReadNextRecord()) {
					var row = new List<String>();
					for (int i = 0; i < fieldCount; i++) {
						row.Add(csv[i]);
						if (!string.IsNullOrWhiteSpace(csv[i]))
							lowest = Math.Max(lowest, i);
					}
					csvData.Add(row);
				}
			}
			if (trim) {
				for (var i = csvData.Count - 1; i >= 0; i--) {
					if (csvData[i].All(x => string.IsNullOrWhiteSpace(x)))
						csvData.RemoveAt(i);
					else
						break;
				}
				if (lowest < fieldCount) {
					for (var i = 0; i < csvData.Count; i++) {
						var row = csvData[i].Where((x, k) => k <= lowest).ToList();
						csvData[i] = row;
					}
				}
			}

			return csvData;
		}

		public static SLDocument ToXls(bool showRowNames, params Csv[] file) {
			return ToXls(showRowNames, file.ToList());
		}


		public static SLDocument ToXls(bool showRowNames, List<Csv> files) {
			SLDocument sl = new SLDocument();
			var first = true;
			var existingNames = new DefaultDictionary<string, int>(x => 0);
			string firstName = null;
			foreach (var csv in files) {
				var sheetname = csv.Title ?? "Sheet ";
				var dup = existingNames[sheetname] + 1;
				existingNames[sheetname] = dup;
				if (dup > 1) {
					sheetname += dup;
				}

				//Naming
				if (first) {
					firstName = sheetname;
					sl.RenameWorksheet(SLDocument.DefaultFirstSheetName, sheetname);
				} else {
					sl.AddWorksheet(sheetname);
				}


				//Copy data
				var columns = csv.GetColumnsCopy();
				var rows = csv.GetRowsCopy();

				var xDiff = 0;
				if (showRowNames) {
					xDiff = 1;
				}
				for (var j = 0; j < columns.Count; j++) {
					SetCell(sl, columns[j], 0, j + xDiff);
				}
				for (var i = 0; i < rows.Count; i++) {
					if (showRowNames) {
						SetCell(sl, rows[i], i + 1, 0);
					}
					for (var j = 0; j < columns.Count; j++) {
						try {
							var cell = csv.Get(i, j);
							SetCell(sl, cell, i + 1, j + xDiff);
						} catch (ArgumentOutOfRangeException e) {
							//noop
						}
					}
				}

				first = false;
			}
			if (!first && firstName != null) {
				sl.SelectWorksheet(firstName);
			}

			return sl;
		}

		private static Regex LongTester = new Regex(@"^\d+$");

		/// <summary>
		/// Takes zero indexed rows and columns, automatically converts cell to long if possible
		/// </summary>
		/// <param name="document"></param>
		/// <param name="cell"></param>
		/// <param name="row"></param>
		/// <param name="col"></param>
		private static void SetCell(SLDocument document, string cell, int row, int col) {

			if (cell == null) {
				return;
			}
			try {
				if (LongTester.IsMatch(cell)) {
					document.SetCellValue(row + 1, col + 1, cell.ToLong());
					return;
				}
			} catch (Exception) {
				//Eat it.
			}

			//default is to use string.
			document.SetCellValue(row + 1, col + 1, cell);
			return;
		}



		public static DataTableHelper FromTable(DataTable table) {
			return new DataTableHelper(table);
		}

		public static string FromTableToCsvString(DataTable table) {
			var c = 0;
			var sb = new StringBuilder();
			for (var r = 0; r < table.Rows.Count; r++) {
				var row = table.Rows[r];
				foreach (var cell in row.ItemArray) {
					try {
						sb.Append(Csv.CsvQuote(cell.NotNull(x => "" + x) ?? ""));
					} catch (Exception) {
					}
					sb.Append(",");
					c++;
				}
				sb.AppendLine();
			}
			return sb.ToString();


		}
		public static string FromXlsToCsvString(SLDocument document, string worksheet) {
			var current = document.GetCurrentWorksheetName();
			document.SelectWorksheet(worksheet);
			var rows = document.GetCells();

			var r = 0;
			var c = 0;
			var sb = new StringBuilder();
			foreach (var row in rows) {
				foreach (var cell in row.Value) {
					try {
						sb.Append(Csv.CsvQuote(cell.Value.CellText ?? cell.Value.NumericValue.NotNull(x => x.ToString()) ?? ""));
					} catch (Exception) {
					}
					sb.Append(",");
					c++;
				}
				sb.AppendLine();
				r++;
			}
			document.SelectWorksheet(current);
			return sb.ToString();
		}
	}
}