using System;
using System.Collections.Generic;
using System.Text;

namespace TractionTools.Utils.Pdf.Generators {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using iText.Html2pdf;
    using iText.IO.Source;
    using iText.Kernel.Pdf;
    using iText.Kernel.Utils;
    using iText.Layout;

    namespace PdfGeneration.Engine.Generators {
        public class ITextGenerator : IPdfGenerator {
            public byte[] Generate(string htmlSource, string baseUri) {
                ConverterProperties properties = new ConverterProperties();
                properties.SetBaseUri(baseUri);

                var baos = new ByteArrayOutputStream();
                PdfWriter writer = new PdfWriter(baos);
                PdfDocument pdf = new PdfDocument(writer);
                Document document =
                    HtmlConverter.ConvertToDocument(htmlSource, pdf, properties);
                document.Close();

                return baos.ToArray();
            }

            public async Task<Stream> Generate(string htmlSource,bool includeFooter, PdfPageSettings settings) {
                throw new NotImplementedException();
            }

            public byte[] MergePdf(IEnumerable<byte[]> pdfs) {
                var output = new ByteArrayOutputStream();
                var pdfDocResult = new PdfDocument(new PdfWriter(output));
                var merger = new PdfMerger(pdfDocResult);

                foreach (var pdf in pdfs) {
                    var readerTemp = new PdfReader(new MemoryStream(pdf));
                    var pdfDoc = new PdfDocument(readerTemp);
                    merger.Merge(pdfDoc, 1, pdfDoc.GetNumberOfPages());
                }

                pdfDocResult.Close();

                return output.ToArray();
            }

            public Stream MergePdf(IEnumerable<Stream> pdfStreams) {
                var output = new ByteArrayOutputStream();
                var pdfDocResult = new PdfDocument(new PdfWriter(output));
                var merger = new PdfMerger(pdfDocResult);

                foreach (var pdf in pdfStreams) {
                    var readerTemp = new PdfReader(pdf);
                    var pdfDoc = new PdfDocument(readerTemp);
                    merger.Merge(pdfDoc, 1, pdfDoc.GetNumberOfPages());
                }

                pdfDocResult.Close();

                return output;
            }

            public IPdfGenerator AddHeader(string text, bool isLeft = true) {
                throw new NotImplementedException();
            }

            public IPdfGenerator AddFooter(string text, bool isLeft = true) {
                throw new NotImplementedException();
            }
        }
    }
}