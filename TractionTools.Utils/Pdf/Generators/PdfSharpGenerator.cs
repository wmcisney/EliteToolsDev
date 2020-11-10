//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using MigraDoc.DocumentObjectModel;
//using MigraDoc.Rendering;
//using PdfSharp.Pdf.IO;
//using TractionTools.Utils.Pdf.Generators;

//namespace TractionTools.Utils.Pdf.Generators {
//	public class PdfSharpGenerator : IPdfGenerator {

//		public async Task<Stream> Generate(string htmlSource,  bool closeStream, PdfPageSettings settings) {
//			throw new NotImplementedException();
//		}

//		//public Stream MergePdf(IEnumerable<Stream> pdfStreams) {
			
//		//}

//		public IPdfGenerator AddHeader(string text, bool isLeft = true) {
//			throw new NotImplementedException();
//		}

//		public IPdfGenerator AddFooter(string text, bool isLeft = true) {
//			throw new NotImplementedException();
//		}

//		public static Stream DocumentToStream(object pdfDocument) {
//			var pdfStream = new MemoryStream();

//			// save to Stream 
//			if (pdfDocument is PdfSharp.Pdf.PdfDocument pdfDoc) {
//				pdfDoc.Save(pdfStream, false);
//			} else {
//				// Migra Doc, use pdfRenderer
//				var renderer = new PdfDocumentRenderer(true);

//				if (pdfDocument is PdfDocumentRenderer docRenderer) {
//					// override current renderer
//					renderer = docRenderer;
//				}

//				/// type of document to convert
//				else if (pdfDocument is Document migraDoc) {
//					renderer.Document = migraDoc;

//					// Render
//					renderer.RenderDocument();

//					// unknown
//				} else {
//					throw new ArgumentException();
//				}

//				renderer.Save(pdfStream, false);
//			}


//			return pdfStream;

//		}
//	}
//}