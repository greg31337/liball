using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfInteroperabilityTests
{
    [Test]
    public void IndependentParserObservesPagesTextFormsMetadataAndTagging()
    {
        IndependentPdfReader reader = IndependentPdfReader.Open(CreateRepresentativeDocument().ToBytes());

        Assert.Multiple(() =>
        {
            Assert.That(reader.Version, Is.EqualTo("1.4"));
            Assert.That(reader.PageCount, Is.EqualTo(2));
            Assert.That(reader.ExtractText(), Does.Contain("Interoperability text"));
            Assert.That(reader.GetMetadataValue("Title"), Is.EqualTo("Interop Fixture"));
            Assert.That(reader.GetMetadataValue("Author"), Is.EqualTo("Ada Example"));
            Assert.That(reader.GetFormFieldValue("customer"), Is.EqualTo("Ada"));
            Assert.That(reader.CatalogBody, Does.Contain("/AcroForm "));
            Assert.That(reader.CatalogBody, Does.Contain("/StructTreeRoot "));
            Assert.That(reader.CatalogBody, Does.Contain("/MarkInfo << /Marked true >>"));
            Assert.That(reader.AllObjectText, Does.Contain("/S /Document"));
            Assert.That(reader.AllObjectText, Does.Contain("/S /Figure"));
            Assert.That(reader.AllObjectText, Does.Contain("/Alt (Two color sample)"));
            Assert.That(reader.AllObjectText, Does.Contain("/Type /OBJR"));
        });
    }

    [Test]
    public void IndependentRendererAndImageDecoderObservePaintAndRgbSamples()
    {
        IndependentPdfReader reader = IndependentPdfReader.Open(CreateRepresentativeDocument().ToBytes());
        IndependentPdfRaster raster = IndependentPdfRasterizer.RenderFirstPage(reader);
        IndependentPdfImage image = IndependentPdfImageDecoder.DecodeFirstImage(reader);

        Assert.Multiple(() =>
        {
            Assert.That(raster.Width, Is.EqualTo(120));
            Assert.That(raster.Height, Is.EqualTo(80));
            Assert.That(raster.GetRgb(2, 2), Is.EqualTo(new byte[] { 255, 255, 255 }));
            Assert.That(raster.GetRgb(15, 15), Is.EqualTo(new byte[] { 64, 128, 191 }));
            Assert.That(image.Width, Is.EqualTo(2));
            Assert.That(image.Height, Is.EqualTo(1));
            Assert.That(image.ColorSpace, Is.EqualTo("DeviceRGB"));
            Assert.That(image.Samples, Is.EqualTo(new byte[] { 255, 0, 0, 0, 255, 0 }));
        });
    }

    [Test]
    public void IndependentFontInspectorObservesEmbeddedProgramAndUnicodeMap()
    {
        IndependentPdfReader reader = IndependentPdfReader.Open(CreateRepresentativeDocument().ToBytes());
        IndependentPdfFont font = IndependentPdfFontInspector.InspectEmbeddedFont(reader);

        Assert.Multiple(() =>
        {
            Assert.That(font.BaseFont, Does.Match("^[A-Z]{6}\\+TestSans-Regular$"));
            Assert.That(font.IsEmbedded, Is.True);
            Assert.That(font.SfntVersion, Is.EqualTo(0x00010000U));
            Assert.That(font.TableCount, Is.GreaterThan(0));
            Assert.That(font.ToUnicodeCMap, Does.Contain("beginbfchar"));
            Assert.That(font.ToUnicodeCMap, Does.Contain("<0041>"));
            Assert.That(font.ToUnicodeCMap, Does.Contain("<0043>"));
        });
    }

    [Test]
    public void IndependentSecurityInspectionObservesEncryptionAndProtectedContent()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(100, 60);
        page.GetCanvas().DrawText("Secret interop text", 10, 30, 10);
        PdfPermissions permissions = new PdfPermissions();
        permissions.DisallowCopying();
        document.UseStandardEncryptionWithPermissions("reader", "owner", permissions);

        IndependentPdfReader reader = IndependentPdfReader.Open(document.ToBytes());

        Assert.Multiple(() =>
        {
            Assert.That(reader.HasEncryption, Is.True);
            Assert.That(reader.EncryptionBody, Does.Contain("/Filter /Standard"));
            Assert.That(reader.EncryptionBody, Does.Contain("/V 1 /R 2 /Length 40"));
            Assert.That(reader.EncryptionBody, Does.Contain("/P "));
            Assert.That(reader.AllObjectText, Does.Not.Contain("Secret interop text"));
            Assert.That(reader.PageCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void CommonPdfToolsOpenRepresentativeOutputWhenInstalled()
    {
        byte[] bytes = CreateRepresentativeDocument().ToBytes();
        string[] tools = IndependentPdfToolRunner.CheckWithInstalledTools(bytes, 2);

        if (tools.Length == 0)
        {
            Assert.Pass("No supported external PDF conformance tool is installed on this host.");
        }

        Assert.That(tools, Is.Not.Empty);
    }

    [Test]
    public void IndependentParserRejectsDamagedCrossReferenceOffsets()
    {
        byte[] valid = CreateRepresentativeDocument().ToBytes();
        string text = Encoding.Latin1.GetString(valid);
        Regex xrefEntry = new Regex(@"^\d{10} 00000 n $", RegexOptions.Multiline);
        string damaged = xrefEntry.Replace(text, "0999999999 00000 n ", 1);

        Assert.Throws<InvalidDataException>(() => IndependentPdfReader.Open(Encoding.Latin1.GetBytes(damaged)));
    }

    static PdfDocument CreateRepresentativeDocument()
    {
        PdfDocument document = new PdfDocument();
        PdfMetadata metadata = new PdfMetadata();
        metadata.SetTitle("Interop Fixture");
        metadata.SetAuthor("Ada Example");
        metadata.SetProducer("pdf interoperability tests");
        document.SetMetadata(metadata);
        document.SetLanguage("en-US");
        document.SetDisplayDocumentTitle(true);
        document.EnableTaggedPdf();

        PdfPage first = document.AddPage(120, 80);
        PdfAccessibilityNode root = document.AddAccessibilityNode(first, "Document");
        PdfAccessibilityNode paragraph = document.AddStructureChild(root, "P", string.Empty);
        PdfAccessibilityNode figure = document.AddStructureChild(root, "Figure", string.Empty);
        PdfAccessibilityNode form = document.AddStructureChild(root, "Form", string.Empty);

        first.GetCanvas().SetFillColorValue(PdfColor.Rgb(0.25, 0.5, 0.75));
        first.GetCanvas().FillRectangle(10, 10, 20, 15);
        first.GetCanvas().BeginStructureContent(paragraph);
        first.GetCanvas().DrawText("Interoperability text", 10, 55, 10);
        first.GetCanvas().EndMarkedContent();

        PdfImage image = document.AddImage(2, 1, new byte[] { 255, 0, 0, 0, 255, 0 });
        document.SetStructureAlternateText(figure, "Two color sample");
        first.GetCanvas().BeginStructureContent(figure);
        first.GetCanvas().DrawImage(image, 70, 10, 20, 10);
        first.GetCanvas().EndMarkedContent();

        PdfTrueTypeFont font = document.RegisterTrueTypeFont(TestSfntFont.Create(0x00010000U, 0, false));
        first.GetCanvas().DrawTextWithTrueTypeFont("AC", 10, 40, 10, font);

        PdfAcroFormField field = document.AddTextField("customer", first, 10, 28, 50, 10);
        field.SetValue("Ada");
        field.SetAccessibilityName("Customer name");
        document.TagFormField(form, field);

        document.AddPage(120, 80);
        return document;
    }
}

internal class IndependentPdfReader
{
    internal byte[] Bytes = new byte[0];
    internal string Text = string.Empty;
    internal string Version = string.Empty;
    internal Dictionary<int, int> Offsets = new Dictionary<int, int>();
    internal Dictionary<int, string> Objects = new Dictionary<int, string>();
    internal string Trailer = string.Empty;
    internal int RootObjectId;

    internal string AllObjectText
    {
        get { return string.Join("\n", Objects.OrderBy(item => item.Key).Select(item => item.Value)); }
    }

    internal string CatalogBody
    {
        get { return Objects[RootObjectId]; }
    }

    internal int PageCount
    {
        get { return Objects.Values.Count(body => body.Contains("/Type /Page ", StringComparison.Ordinal)); }
    }

    internal bool HasEncryption
    {
        get { return Regex.IsMatch(Trailer, @"/Encrypt \d+ 0 R"); }
    }

    internal string EncryptionBody
    {
        get
        {
            Match match = Regex.Match(Trailer, @"/Encrypt (\d+) 0 R");
            return match.Success ? Objects[int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)] : string.Empty;
        }
    }

    internal static IndependentPdfReader Open(byte[] bytes)
    {
        IndependentPdfReader reader = new IndependentPdfReader();
        reader.Bytes = bytes;
        reader.Text = Encoding.Latin1.GetString(bytes);
        reader.Parse();
        return reader;
    }

    internal void Parse()
    {
        Match header = Regex.Match(Text, @"\A%PDF-(\d+\.\d+)\n");
        if (!header.Success || !Text.EndsWith("%%EOF\n", StringComparison.Ordinal))
        {
            throw new InvalidDataException("The PDF header or end marker is invalid.");
        }

        Version = header.Groups[1].Value;
        Match start = Regex.Match(Text, @"startxref\n(\d+)\n%%EOF\n\z");
        if (!start.Success)
        {
            throw new InvalidDataException("The startxref entry is missing.");
        }

        int xrefOffset = int.Parse(start.Groups[1].Value, CultureInfo.InvariantCulture);
        if (xrefOffset < 0 || xrefOffset >= Text.Length || !Text.AsSpan(xrefOffset).StartsWith("xref\n", StringComparison.Ordinal))
        {
            throw new InvalidDataException("The cross-reference offset is invalid.");
        }

        int trailerIndex = Text.IndexOf("trailer\n", xrefOffset, StringComparison.Ordinal);
        if (trailerIndex < 0)
        {
            throw new InvalidDataException("The trailer is missing.");
        }

        ParseXref(Text.Substring(xrefOffset, trailerIndex - xrefOffset));
        int startXrefIndex = Text.IndexOf("startxref\n", trailerIndex, StringComparison.Ordinal);
        Trailer = Text.Substring(trailerIndex, startXrefIndex - trailerIndex);
        Match root = Regex.Match(Trailer, @"/Root (\d+) 0 R");
        if (!root.Success)
        {
            throw new InvalidDataException("The document catalog reference is missing.");
        }

        RootObjectId = int.Parse(root.Groups[1].Value, CultureInfo.InvariantCulture);
        ParseObjects(xrefOffset);
        ValidateReferences();
    }

    internal void ParseXref(string section)
    {
        string[] lines = section.Split('\n');
        if (lines.Length < 3 || lines[0] != "xref")
        {
            throw new InvalidDataException("The cross-reference table is malformed.");
        }

        Match subsection = Regex.Match(lines[1], @"^0 (\d+)$");
        if (!subsection.Success)
        {
            throw new InvalidDataException("The cross-reference subsection is unsupported.");
        }

        int count = int.Parse(subsection.Groups[1].Value, CultureInfo.InvariantCulture);
        if (lines.Length < count + 2)
        {
            throw new InvalidDataException("The cross-reference table is truncated.");
        }

        for (int objectId = 1; objectId < count; objectId++)
        {
            Match entry = Regex.Match(lines[objectId + 2], @"^(\d{10}) 00000 n ");
            if (!entry.Success)
            {
                throw new InvalidDataException("A cross-reference entry is malformed.");
            }

            Offsets[objectId] = int.Parse(entry.Groups[1].Value, CultureInfo.InvariantCulture);
        }
    }

    internal void ParseObjects(int xrefOffset)
    {
        foreach (KeyValuePair<int, int> entry in Offsets)
        {
            string prefix = entry.Key.ToString(CultureInfo.InvariantCulture) + " 0 obj\n";
            if (entry.Value < 0 || entry.Value >= xrefOffset || !Text.AsSpan(entry.Value).StartsWith(prefix, StringComparison.Ordinal))
            {
                throw new InvalidDataException("A cross-reference entry does not point to its object.");
            }

            int bodyStart = entry.Value + prefix.Length;
            int bodyEnd = Text.IndexOf("\nendobj", bodyStart, StringComparison.Ordinal);
            if (bodyEnd < 0 || bodyEnd > xrefOffset)
            {
                throw new InvalidDataException("An indirect object is not terminated.");
            }

            Objects[entry.Key] = Text.Substring(bodyStart, bodyEnd - bodyStart);
        }
    }

    internal void ValidateReferences()
    {
        if (!Objects.ContainsKey(RootObjectId) || !CatalogBody.Contains("/Type /Catalog", StringComparison.Ordinal))
        {
            throw new InvalidDataException("The root object is not a catalog.");
        }

        foreach (string body in Objects.Values)
        {
            foreach (Match reference in Regex.Matches(body, @"(?<!\d)(\d+) 0 R"))
            {
                int objectId = int.Parse(reference.Groups[1].Value, CultureInfo.InvariantCulture);
                if (!Objects.ContainsKey(objectId))
                {
                    throw new InvalidDataException("An indirect reference points to a missing object.");
                }
            }
        }
    }

    internal byte[] GetStreamBytes(int objectId)
    {
        string body = Objects[objectId];
        Match lengthMatch = Regex.Match(body, @"/Length\s+(\d+)(?!\s+0\s+R)");
        if (!lengthMatch.Success)
        {
            throw new InvalidDataException("The stream length is missing.");
        }

        int length = int.Parse(lengthMatch.Groups[1].Value, CultureInfo.InvariantCulture);
        int objectOffset = Offsets[objectId];
        int streamMarker = Text.IndexOf("stream\n", objectOffset, StringComparison.Ordinal);
        int objectEnd = Text.IndexOf("\nendobj", objectOffset, StringComparison.Ordinal);
        if (streamMarker < 0 || streamMarker > objectEnd || streamMarker + 7 + length > Bytes.Length)
        {
            throw new InvalidDataException("The stream bytes are truncated.");
        }

        byte[] result = new byte[length];
        Array.Copy(Bytes, streamMarker + 7, result, 0, length);
        return result;
    }

    internal string ExtractText()
    {
        StringBuilder result = new StringBuilder();
        foreach (string page in PageBodies())
        {
            Match contentReference = Regex.Match(page, @"/Contents (\d+) 0 R");
            if (!contentReference.Success)
            {
                continue;
            }

            int objectId = int.Parse(contentReference.Groups[1].Value, CultureInfo.InvariantCulture);
            string content = Encoding.Latin1.GetString(GetStreamBytes(objectId));
            foreach (Match textMatch in Regex.Matches(content, @"\(((?:\\.|[^\\)])*)\)\s*Tj"))
            {
                if (result.Length > 0)
                {
                    result.Append(' ');
                }

                result.Append(DecodeLiteral(textMatch.Groups[1].Value));
            }
        }

        return result.ToString();
    }

    internal string GetMetadataValue(string key)
    {
        Match infoReference = Regex.Match(Trailer, @"/Info (\d+) 0 R");
        if (!infoReference.Success)
        {
            return string.Empty;
        }

        string info = Objects[int.Parse(infoReference.Groups[1].Value, CultureInfo.InvariantCulture)];
        Match value = Regex.Match(info, "/" + Regex.Escape(key) + @" \(((?:\\.|[^\\)])*)\)");
        return value.Success ? DecodeLiteral(value.Groups[1].Value) : string.Empty;
    }

    internal string GetFormFieldValue(string fieldName)
    {
        foreach (string body in Objects.Values)
        {
            if (!body.Contains("/Subtype /Widget", StringComparison.Ordinal))
            {
                continue;
            }

            Match name = Regex.Match(body, @"/T \(((?:\\.|[^\\)])*)\)");
            if (name.Success && DecodeLiteral(name.Groups[1].Value) == fieldName)
            {
                Match value = Regex.Match(body, @"/V \(((?:\\.|[^\\)])*)\)");
                return value.Success ? DecodeLiteral(value.Groups[1].Value) : string.Empty;
            }
        }

        return string.Empty;
    }

    internal List<string> PageBodies()
    {
        return Objects.Values.Where(body => body.Contains("/Type /Page ", StringComparison.Ordinal)).ToList();
    }

    internal int FindObject(string marker)
    {
        foreach (KeyValuePair<int, string> entry in Objects)
        {
            if (entry.Value.Contains(marker, StringComparison.Ordinal))
            {
                return entry.Key;
            }
        }

        throw new InvalidDataException("The requested object was not found: " + marker);
    }

    internal static string DecodeLiteral(string value)
    {
        return value
            .Replace("\\n", "\n", StringComparison.Ordinal)
            .Replace("\\r", "\r", StringComparison.Ordinal)
            .Replace("\\(", "(", StringComparison.Ordinal)
            .Replace("\\)", ")", StringComparison.Ordinal)
            .Replace("\\\\", "\\", StringComparison.Ordinal);
    }
}

internal class IndependentPdfRaster
{
    internal int Width;
    internal int Height;
    internal byte[] Pixels = new byte[0];

    internal byte[] GetRgb(int x, int y)
    {
        int index = ((Height - 1 - y) * Width + x) * 3;
        return new byte[] { Pixels[index], Pixels[index + 1], Pixels[index + 2] };
    }
}

internal static class IndependentPdfRasterizer
{
    internal static IndependentPdfRaster RenderFirstPage(IndependentPdfReader reader)
    {
        string page = reader.PageBodies()[0];
        Match mediaBox = Regex.Match(page, @"/MediaBox \[0 0 (\d+) (\d+)\]");
        Match contentReference = Regex.Match(page, @"/Contents (\d+) 0 R");
        if (!mediaBox.Success || !contentReference.Success)
        {
            throw new InvalidDataException("The page geometry or content reference is missing.");
        }

        IndependentPdfRaster raster = new IndependentPdfRaster();
        raster.Width = int.Parse(mediaBox.Groups[1].Value, CultureInfo.InvariantCulture);
        raster.Height = int.Parse(mediaBox.Groups[2].Value, CultureInfo.InvariantCulture);
        raster.Pixels = Enumerable.Repeat((byte)255, raster.Width * raster.Height * 3).ToArray();
        byte red = 0;
        byte green = 0;
        byte blue = 0;
        int contentId = int.Parse(contentReference.Groups[1].Value, CultureInfo.InvariantCulture);
        string content = Encoding.ASCII.GetString(reader.GetStreamBytes(contentId));
        foreach (string line in content.Split('\n'))
        {
            Match color = Regex.Match(line, @"^([0-9.]+) ([0-9.]+) ([0-9.]+) rg$");
            if (color.Success)
            {
                red = Component(color.Groups[1].Value);
                green = Component(color.Groups[2].Value);
                blue = Component(color.Groups[3].Value);
                continue;
            }

            Match rectangle = Regex.Match(line, @"^([0-9.]+) ([0-9.]+) ([0-9.]+) ([0-9.]+) re f$");
            if (rectangle.Success)
            {
                FillRectangle(
                    raster,
                    int.Parse(rectangle.Groups[1].Value, CultureInfo.InvariantCulture),
                    int.Parse(rectangle.Groups[2].Value, CultureInfo.InvariantCulture),
                    int.Parse(rectangle.Groups[3].Value, CultureInfo.InvariantCulture),
                    int.Parse(rectangle.Groups[4].Value, CultureInfo.InvariantCulture),
                    red,
                    green,
                    blue);
            }
        }

        return raster;
    }

    static byte Component(string value)
    {
        double component = double.Parse(value, CultureInfo.InvariantCulture);
        return (byte)Math.Round(component * 255, MidpointRounding.AwayFromZero);
    }

    static void FillRectangle(IndependentPdfRaster raster, int x, int y, int width, int height, byte red, byte green, byte blue)
    {
        for (int pixelY = y; pixelY < y + height; pixelY++)
        {
            for (int pixelX = x; pixelX < x + width; pixelX++)
            {
                int index = ((raster.Height - 1 - pixelY) * raster.Width + pixelX) * 3;
                raster.Pixels[index] = red;
                raster.Pixels[index + 1] = green;
                raster.Pixels[index + 2] = blue;
            }
        }
    }
}

internal class IndependentPdfImage
{
    internal int Width;
    internal int Height;
    internal string ColorSpace = string.Empty;
    internal byte[] Samples = new byte[0];
}

internal static class IndependentPdfImageDecoder
{
    internal static IndependentPdfImage DecodeFirstImage(IndependentPdfReader reader)
    {
        int objectId = reader.FindObject("/Subtype /Image");
        string body = reader.Objects[objectId];
        Match width = Regex.Match(body, @"/Width (\d+)");
        Match height = Regex.Match(body, @"/Height (\d+)");
        Match colorSpace = Regex.Match(body, @"/ColorSpace /(\w+)");
        if (!width.Success || !height.Success || !colorSpace.Success || !body.Contains("/Filter /ASCIIHexDecode", StringComparison.Ordinal))
        {
            throw new InvalidDataException("The representative image dictionary is invalid.");
        }

        string encoded = Encoding.ASCII.GetString(reader.GetStreamBytes(objectId));
        string hex = new string(encoded.Where(character => !char.IsWhiteSpace(character) && character != '>').ToArray());
        if (hex.Length % 2 != 0)
        {
            hex += "0";
        }

        IndependentPdfImage image = new IndependentPdfImage();
        image.Width = int.Parse(width.Groups[1].Value, CultureInfo.InvariantCulture);
        image.Height = int.Parse(height.Groups[1].Value, CultureInfo.InvariantCulture);
        image.ColorSpace = colorSpace.Groups[1].Value;
        image.Samples = Convert.FromHexString(hex);
        return image;
    }
}

internal class IndependentPdfFont
{
    internal string BaseFont = string.Empty;
    internal bool IsEmbedded;
    internal uint SfntVersion;
    internal int TableCount;
    internal string ToUnicodeCMap = string.Empty;
}

internal static class IndependentPdfFontInspector
{
    internal static IndependentPdfFont InspectEmbeddedFont(IndependentPdfReader reader)
    {
        int type0Id = reader.FindObject("/Subtype /Type0");
        string type0 = reader.Objects[type0Id];
        Match baseFont = Regex.Match(type0, @"/BaseFont /([^\s]+)");
        Match unicodeReference = Regex.Match(type0, @"/ToUnicode (\d+) 0 R");
        int descriptorId = reader.FindObject("/Type /FontDescriptor");
        Match fileReference = Regex.Match(reader.Objects[descriptorId], @"/FontFile2 (\d+) 0 R");
        if (!baseFont.Success || !unicodeReference.Success || !fileReference.Success)
        {
            throw new InvalidDataException("The embedded font object graph is incomplete.");
        }

        byte[] program = reader.GetStreamBytes(int.Parse(fileReference.Groups[1].Value, CultureInfo.InvariantCulture));
        if (program.Length < 6)
        {
            throw new InvalidDataException("The embedded font program is truncated.");
        }

        IndependentPdfFont font = new IndependentPdfFont();
        font.BaseFont = baseFont.Groups[1].Value;
        font.IsEmbedded = true;
        font.SfntVersion = ((uint)program[0] << 24) | ((uint)program[1] << 16) | ((uint)program[2] << 8) | program[3];
        font.TableCount = (program[4] << 8) | program[5];
        font.ToUnicodeCMap = Encoding.ASCII.GetString(reader.GetStreamBytes(int.Parse(unicodeReference.Groups[1].Value, CultureInfo.InvariantCulture)));
        return font;
    }
}

internal static class IndependentPdfToolRunner
{
    internal static string[] CheckWithInstalledTools(byte[] bytes, int expectedPages)
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "interoperability-tools");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "representative-" + Guid.NewGuid().ToString("N") + ".pdf");
        File.WriteAllBytes(path, bytes);
        List<string> tools = new List<string>();

        string qpdf = FindExecutable("qpdf");
        if (qpdf.Length > 0)
        {
            RunTextTool(qpdf, new string[] { "--check", path }, string.Empty);
            tools.Add("qpdf");
        }

        string pdfinfo = FindExecutable("pdfinfo");
        if (pdfinfo.Length > 0)
        {
            string output = RunTextTool(pdfinfo, new string[] { path }, "Pages:");
            Assert.That(output, Does.Contain("Pages:").And.Contain(expectedPages.ToString(CultureInfo.InvariantCulture)));
            tools.Add("pdfinfo");
        }

        string pdffonts = FindExecutable("pdffonts");
        if (pdffonts.Length > 0)
        {
            string output = RunTextTool(pdffonts, new string[] { path }, "emb");
            Assert.That(output, Does.Contain("yes"));
            tools.Add("pdffonts");
        }

        string cupsfilter = FindExecutable("cupsfilter");
        if (cupsfilter.Length > 0)
        {
            byte[] normalized = RunBinaryTool(cupsfilter, new string[] { "-m", "application/pdf", path });
            IndependentPdfReader normalizedReader = IndependentPdfReader.Open(normalized);
            Assert.That(normalizedReader.PageCount, Is.EqualTo(expectedPages));
            tools.Add("cupsfilter");
        }

        return tools.ToArray();
    }

    static string FindExecutable(string name)
    {
        string pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        List<string> directories = pathValue.Split(Path.PathSeparator).ToList();
        directories.Add("/usr/bin");
        directories.Add("/usr/sbin");
        directories.Add("/opt/homebrew/bin");
        foreach (string directory in directories)
        {
            string candidate = Path.Combine(directory, name);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    static string RunTextTool(string executable, string[] arguments, string expectedOutput)
    {
        ProcessStartInfo startInfo = StartInfo(executable, arguments);
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start " + executable + ".");
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(20000))
        {
            process.Kill(true);
            throw new TimeoutException(executable + " did not finish.");
        }

        Assert.That(process.ExitCode, Is.EqualTo(0), error);
        if (expectedOutput.Length > 0)
        {
            Assert.That(output, Does.Contain(expectedOutput));
        }

        return output;
    }

    static byte[] RunBinaryTool(string executable, string[] arguments)
    {
        ProcessStartInfo startInfo = StartInfo(executable, arguments);
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start " + executable + ".");
        Task<string> errorTask = process.StandardError.ReadToEndAsync();
        using MemoryStream output = new MemoryStream();
        process.StandardOutput.BaseStream.CopyTo(output);
        if (!process.WaitForExit(20000))
        {
            process.Kill(true);
            throw new TimeoutException(executable + " did not finish.");
        }

        string error = errorTask.GetAwaiter().GetResult();
        Assert.That(process.ExitCode, Is.EqualTo(0), error);
        return output.ToArray();
    }

    static ProcessStartInfo StartInfo(string executable, string[] arguments)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = executable;
        startInfo.UseShellExecute = false;
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }
}
