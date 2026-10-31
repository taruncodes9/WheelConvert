using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using SkiaSharp;
using UglyToad.PdfPig;

namespace WheelConvert.Core.Services;

/// <summary>
/// Handles PDF and DOCX conversions:
///  - Image -> PDF, Image -> DOCX
///  - PDF -> JPG, PDF -> PNG, PDF -> DOCX, PDF -> TXT
///  - DOCX -> PDF, DOCX -> TXT
/// </summary>
public static class DocumentProcessor
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace Rels = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace ContentTypes = "http://schemas.openxmlformats.org/package/2006/content-types";

    // ==========================================
    // 1. IMAGE -> PDF / DOCX
    // ==========================================

    /// <summary>Convert an image file directly into a clean PDF document.</summary>
    public static void ImageToPdf(string imagePath, string outputPdfPath)
    {
        using var bitmap = SKBitmap.Decode(imagePath);
        if (bitmap is null) throw new InvalidOperationException($"Cannot decode image: {imagePath}");

        Directory.CreateDirectory(Path.GetDirectoryName(outputPdfPath) ?? "");
        using var fileStream = File.Create(outputPdfPath);
        using var document = SKDocument.CreatePdf(fileStream);

        // Standard page size or image dimensions in points (72 DPI)
        float pageWidth = bitmap.Width;
        float pageHeight = bitmap.Height;

        using var canvas = document.BeginPage(pageWidth, pageHeight);
        canvas.DrawBitmap(bitmap, 0, 0);
        document.EndPage();
        document.Close();
    }

    /// <summary>Convert an image file into a DOCX document embedding the image.</summary>
    public static void ImageToDocx(string imagePath, string outputDocxPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputDocxPath) ?? "");
        if (File.Exists(outputDocxPath)) File.Delete(outputDocxPath);

        using var archive = ZipFile.Open(outputDocxPath, ZipArchiveMode.Create);
        var ext = Path.GetExtension(imagePath).TrimStart('.').ToLowerInvariant();
        if (ext is "jpg" or "jpeg") ext = "jpeg"; else ext = "png";

        // [Content_Types].xml
        var ctEntry = archive.CreateEntry("[Content_Types].xml");
        using (var sw = new StreamWriter(ctEntry.Open(), Encoding.UTF8))
        {
            sw.Write($@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
  <Default Extension=""xml"" ContentType=""application/xml""/>
  <Default Extension=""{ext}"" ContentType=""image/{ext}""/>
  <Override PartName=""/word/document.xml"" ContentType=""application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml""/>
</Types>");
        }

        // _rels/.rels
        var relsEntry = archive.CreateEntry("_rels/.rels");
        using (var sw = new StreamWriter(relsEntry.Open(), Encoding.UTF8))
        {
            sw.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""word/document.xml""/>
</Relationships>");
        }

        // word/_rels/document.xml.rels
        var docRelsEntry = archive.CreateEntry("word/_rels/document.xml.rels");
        using (var sw = new StreamWriter(docRelsEntry.Open(), Encoding.UTF8))
        {
            sw.Write($@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rIdImage1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/image"" Target=""media/image1.{ext}""/>
</Relationships>");
        }

        // word/media/image1.{ext}
        var imgEntry = archive.CreateEntry($"word/media/image1.{ext}");
        using (var inStream = File.OpenRead(imagePath))
        using (var outStream = imgEntry.Open())
        {
            inStream.CopyTo(outStream);
        }

        // Dimensions in EMUs (1 px at 96 DPI ≈ 9525 EMUs)
        var (w, h) = ImageProcessor.GetDimensions(imagePath);
        long emuW = Math.Clamp((long)w * 9525L, 952500L, 5715000L); // max ~6 inches
        long emuH = (w > 0) ? (long)(emuW * ((double)h / w)) : 3810000L;

        // word/document.xml
        var docEntry = archive.CreateEntry("word/document.xml");
        using (var sw = new StreamWriter(docEntry.Open(), Encoding.UTF8))
        {
            sw.Write($@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<w:document xmlns:w=""http://schemas.openxmlformats.org/wordprocessingml/2006/main""
            xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships""
            xmlns:wp=""http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing""
            xmlns:a=""http://schemas.openxmlformats.org/drawingml/2006/main""
            xmlns:pic=""http://schemas.openxmlformats.org/drawingml/2006/picture"">
  <w:body>
    <w:p>
      <w:r>
        <w:drawing>
          <wp:inline distT=""0"" distB=""0"" distL=""0"" distR=""0"">
            <wp:extent cx=""{emuW}"" cy=""{emuH}""/>
            <wp:docPr id=""1"" name=""Picture 1""/>
            <a:graphic>
              <a:graphicData uri=""http://schemas.openxmlformats.org/drawingml/2006/picture"">
                <pic:pic>
                  <pic:nvPicPr>
                    <pic:cNvPr id=""0"" name=""Picture 1""/>
                    <pic:cNvPicPr/>
                  </pic:nvPicPr>
                  <pic:blipFill>
                    <a:blip r:embed=""rIdImage1""/>
                    <a:stretch><a:fillRect/></a:stretch>
                  </pic:blipFill>
                  <pic:spPr>
                    <a:xfrm><a:off x=""0"" y=""0""/><a:ext cx=""{emuW}"" cy=""{emuH}""/></a:xfrm>
                    <a:prstGeom prst=""rect""><a:avLst/></a:prstGeom>
                  </pic:spPr>
                </pic:pic>
              </a:graphicData>
            </a:graphic>
          </wp:inline>
        </w:drawing>
      </w:r>
    </w:p>
  </w:body>
</w:document>");
        }
    }

    // ==========================================
    // 2. PDF CONVERSIONS (PDF -> JPG/PNG, DOCX, TXT)
    // ==========================================

    /// <summary>Render PDF page(s) to JPG or PNG using Windows.Data.Pdf.</summary>
    public static void PdfToImages(string pdfPath, string outputBaseImagePath, string targetFormat)
    {
        var task = Task.Run(async () =>
        {
            var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(Path.GetFullPath(pdfPath));
            var pdfDoc = await Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(file);

            if (pdfDoc.PageCount == 0)
                throw new InvalidOperationException("PDF contains no pages.");

            var ext = targetFormat.ToLowerInvariant().TrimStart('.');
            if (ext is "jpeg") ext = "jpg";
            var skFormat = ext == "png" ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg;

            var dir = Path.GetDirectoryName(outputBaseImagePath) ?? "";
            var nameWithoutExt = Path.GetFileNameWithoutExtension(outputBaseImagePath);

            // If 1 page, save as outputBaseImagePath. If multiple pages, save as name_1.ext, name_2.ext
            for (uint i = 0; i < pdfDoc.PageCount; i++)
            {
                using var page = pdfDoc.GetPage(i);
                using var memStream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                await page.RenderToStreamAsync(memStream);

                using var netStream = memStream.AsStreamForRead();
                using var bitmap = SKBitmap.Decode(netStream);
                if (bitmap is null) continue;

                var outPath = (pdfDoc.PageCount == 1)
                    ? outputBaseImagePath
                    : Path.Combine(dir, $"{nameWithoutExt}_page{i + 1}.{ext}");

                Directory.CreateDirectory(Path.GetDirectoryName(outPath) ?? "");
                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(skFormat, 90);
                using var outFile = File.OpenWrite(outPath);
                data.SaveTo(outFile);
            }
        });
        task.GetAwaiter().GetResult();
    }

    /// <summary>Extract all text from a PDF file.</summary>
    public static string PdfToText(string pdfPath)
    {
        using var pdf = UglyToad.PdfPig.PdfDocument.Open(pdfPath);
        var sb = new StringBuilder();
        foreach (var page in pdf.GetPages())
        {
            var text = page.Text;
            if (!string.IsNullOrWhiteSpace(text))
                sb.AppendLine(text);
        }
        return sb.ToString();
    }

    /// <summary>Convert a PDF file to a TXT file.</summary>
    public static void PdfToTxtFile(string pdfPath, string outputTxtPath)
    {
        var text = PdfToText(pdfPath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputTxtPath) ?? "");
        File.WriteAllText(outputTxtPath, text, Encoding.UTF8);
    }

    /// <summary>Convert a PDF file to a DOCX document by extracting its text paragraphs.</summary>
    public static void PdfToDocx(string pdfPath, string outputDocxPath)
    {
        var text = PdfToText(pdfPath);
        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        TextLinesToDocx(lines, outputDocxPath);
    }

    // ==========================================
    // 3. DOCX CONVERSIONS (DOCX -> PDF, TXT)
    // ==========================================

    /// <summary>Extract all paragraphs and text from a DOCX file.</summary>
    public static List<string> DocxToTextLines(string docxPath)
    {
        var lines = new List<string>();
        using var archive = ZipFile.OpenRead(docxPath);
        var entry = archive.GetEntry("word/document.xml");
        if (entry is null) return lines;

        using var stream = entry.Open();
        var xdoc = XDocument.Load(stream);

        // Find all paragraphs <w:p>
        foreach (var p in xdoc.Descendants(W + "p"))
        {
            var sb = new StringBuilder();
            foreach (var t in p.Descendants(W + "t"))
            {
                sb.Append(t.Value);
            }
            lines.Add(sb.ToString());
        }
        return lines;
    }

    /// <summary>Convert DOCX to a TXT file.</summary>
    public static void DocxToTxtFile(string docxPath, string outputTxtPath)
    {
        var lines = DocxToTextLines(docxPath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputTxtPath) ?? "");
        File.WriteAllLines(outputTxtPath, lines, Encoding.UTF8);
    }

    /// <summary>Convert DOCX to a multi-page PDF document.</summary>
    public static void DocxToPdf(string docxPath, string outputPdfPath)
    {
        var lines = DocxToTextLines(docxPath);
        TextLinesToPdf(lines, outputPdfPath);
    }

    // ==========================================
    // 4. SHARED HELPERS: TEXT -> DOCX & TEXT -> PDF
    // ==========================================

    /// <summary>Pack lines of text into a valid Microsoft Word (.docx) file.</summary>
    public static void TextLinesToDocx(IEnumerable<string> lines, string outputDocxPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputDocxPath) ?? "");
        if (File.Exists(outputDocxPath)) File.Delete(outputDocxPath);

        using var archive = ZipFile.Open(outputDocxPath, ZipArchiveMode.Create);

        // [Content_Types].xml
        var ctEntry = archive.CreateEntry("[Content_Types].xml");
        using (var sw = new StreamWriter(ctEntry.Open(), Encoding.UTF8))
        {
            sw.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
  <Default Extension=""xml"" ContentType=""application/xml""/>
  <Override PartName=""/word/document.xml"" ContentType=""application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml""/>
</Types>");
        }

        // _rels/.rels
        var relsEntry = archive.CreateEntry("_rels/.rels");
        using (var sw = new StreamWriter(relsEntry.Open(), Encoding.UTF8))
        {
            sw.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""word/document.xml""/>
</Relationships>");
        }

        // word/document.xml
        var docEntry = archive.CreateEntry("word/document.xml");
        using (var sw = new StreamWriter(docEntry.Open(), Encoding.UTF8))
        {
            sw.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<w:document xmlns:w=""http://schemas.openxmlformats.org/wordprocessingml/2006/main"">
  <w:body>");
            foreach (var line in lines)
            {
                var clean = System.Security.SecurityElement.Escape(line);
                sw.Write($@"<w:p><w:r><w:t>{clean}</w:t></w:r></w:p>");
            }
            sw.Write(@"</w:body></w:document>");
        }
    }

    /// <summary>Render lines of text to a clean paginated PDF document with auto-wrapping.</summary>
    public static void TextLinesToPdf(IEnumerable<string> lines, string outputPdfPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPdfPath) ?? "");
        using var stream = File.Create(outputPdfPath);
        using var document = SKDocument.CreatePdf(stream);

        const float pageWidth = 595.28f;  // A4 standard points (72 DPI)
        const float pageHeight = 841.89f;
        const float margin = 50f;
        const float fontSize = 11f;
        const float lineHeight = 16f;

        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = fontSize,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial")
        };

        float y = margin + lineHeight;
        SKCanvas? canvas = document.BeginPage(pageWidth, pageHeight);

        float maxTextWidth = pageWidth - (margin * 2);

        foreach (var rawLine in lines)
        {
            var line = rawLine ?? "";
            // Simple word wrapping
            var words = line.Split(' ');
            var currentLine = new StringBuilder();

            foreach (var word in words)
            {
                var test = currentLine.Length == 0 ? word : currentLine + " " + word;
                if (paint.MeasureText(test) <= maxTextWidth)
                {
                    if (currentLine.Length > 0) currentLine.Append(' ');
                    currentLine.Append(word);
                }
                else
                {
                    if (y + lineHeight > pageHeight - margin)
                    {
                        document.EndPage();
                        canvas = document.BeginPage(pageWidth, pageHeight);
                        y = margin + lineHeight;
                    }
                    canvas.DrawText(currentLine.ToString(), margin, y, paint);
                    y += lineHeight;
                    currentLine.Clear();
                    currentLine.Append(word);
                }
            }

            if (currentLine.Length > 0 || line.Length == 0)
            {
                if (y + lineHeight > pageHeight - margin)
                {
                    document.EndPage();
                    canvas = document.BeginPage(pageWidth, pageHeight);
                    y = margin + lineHeight;
                }
                canvas.DrawText(currentLine.ToString(), margin, y, paint);
                y += lineHeight;
            }
        }

        document.EndPage();
        document.Close();
    }
}

