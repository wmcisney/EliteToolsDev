using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenHtmlToPdf;
using PdfSharp.Pdf.IO;

namespace TractionTools.Utils.Pdf.Generators {
    public class OpenHtmlToPdfGenerator : IPdfGenerator {
        public byte[] Generate(string htmlSource, string baseUri) {
            return OpenHtmlToPdf.Pdf.From(htmlSource)
                //				.Portrait()
                //				.Content();
                .OfSize(PaperSize.Letter)
                .WithTitle("Title")
                .WithoutOutline()
                .WithMargins(1.25.Centimeters())
                .WithObjectSetting("web.enableIntelligentShrinking", "false")
                .Portrait()
                .Comressed()
                .Content();
        }

        public async Task<Stream> Generate(string htmlSource,bool includeFooters, PdfPageSettings settings) {
            throw new NotImplementedException();
        }

        public byte[] MergePdf(IEnumerable<byte[]> pdfs) {

            var pdfDocs = pdfs.Select(pdf => PdfReader.Open(new MemoryStream(pdf), PdfDocumentOpenMode.Import));

            using (PdfSharp.Pdf.PdfDocument outPdf = new PdfSharp.Pdf.PdfDocument()) {
                return pdfDocs.Aggregate(outPdf, AddPagesIntoDocument,
                    output => {
                        var stream = new MemoryStream();
                        output.Save(stream, false);

                        return stream.ToArray();
                    });

            }
        }

        public Stream MergePdf(IEnumerable<Stream> pdfStreams) {
            throw new NotImplementedException();
        }

        public IPdfGenerator AddHeader(string text, bool isLeft = true) {
            throw new NotImplementedException();
        }

        public IPdfGenerator AddFooter(string text, bool isLeft = true) {
            throw new NotImplementedException();
        }

        private PdfSharp.Pdf.PdfDocument AddPagesIntoDocument(PdfSharp.Pdf.PdfDocument outputDoc,
            PdfSharp.Pdf.PdfDocument doc) {
            foreach (PdfSharp.Pdf.PdfPage page in doc.Pages) {
                outputDoc.AddPage(page);
            }

            return outputDoc;
        }
    }
}