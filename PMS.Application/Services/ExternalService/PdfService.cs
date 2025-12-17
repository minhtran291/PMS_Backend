using DinkToPdf;
using DinkToPdf.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.ExternalService
{
    public class PdfService(IConverter converter) : IPdfService
    {
        private readonly IConverter _converter = converter;

        public byte[] GeneratePdfFromHtml(string html)
        {
            var doc = new HtmlToPdfDocument
            {
                GlobalSettings =
                {
                    PaperSize = PaperKind.A4,
                    Orientation = Orientation.Portrait,
                    Margins = new MarginSettings { Top = 10, Bottom = 10 }
                },
                Objects =
                {
                    new ObjectSettings
                    {
                        HtmlContent = html,
                        WebSettings = { 
                            DefaultEncoding = "utf-8",
                            LoadImages = true,
                            PrintMediaType = true
                        }
                    }
                }
            };
            return _converter.Convert(doc);
        }
    }
}
