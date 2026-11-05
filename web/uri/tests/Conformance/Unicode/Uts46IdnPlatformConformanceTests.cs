using System.Globalization;
using System.Text;

namespace uri.Tests;

public class Uts46IdnPlatformConformanceTests
{
    private static readonly IdnaRow[] CorpusRows = LoadCorpusRows();

    [Test]
    public void ApplicableNontransitionalToAsciiVectorsMatchPlatformMode()
    {
        int applicableCount = 0;
        OperationLimits limits = new OperationLimits();
        foreach (IdnaRow row in CorpusRows)
        {
            if (!row.AsciiValid)
            {
                continue;
            }

            UriTextResult ascii = IdnHostCore.ConvertHostToAscii(
                row.Source.AsSpan(),
                IriIdnMode.Platform,
                limits);
            if (!ascii.Success)
            {
                continue;
            }

            applicableCount = applicableCount + 1;
            string message = CreateLineMessage(row.Line);
            Assert.That(
                ascii.Text,
                Is.EqualTo(row.Ascii).IgnoreCase,
                message);

            UriTextResult unicode = IdnHostCore.ConvertHostToUnicode(
                ascii.Text.AsSpan(),
                IriIdnMode.Platform,
                limits);
            Assert.That(unicode.Success, Is.True, message);
            UriTextResult repeated = IdnHostCore.ConvertHostToAscii(
                unicode.Text.AsSpan(),
                IriIdnMode.Platform,
                limits);
            Assert.That(repeated.Success, Is.True, message);
            Assert.That(
                repeated.Text,
                Is.EqualTo(ascii.Text).IgnoreCase,
                message);
        }

        Assert.That(applicableCount, Is.GreaterThan(300));
    }

    [Test]
    public void ApplicableAsciiToUnicodeVectorsMatchPlatformMode()
    {
        int applicableCount = 0;
        OperationLimits limits = new OperationLimits();
        foreach (IdnaRow row in CorpusRows)
        {
            if (!row.UnicodeValid || !IsAscii(row.Source))
            {
                continue;
            }

            UriTextResult unicode = IdnHostCore.ConvertHostToUnicode(
                row.Source.AsSpan(),
                IriIdnMode.Platform,
                limits);
            if (!unicode.Success)
            {
                continue;
            }

            applicableCount = applicableCount + 1;
            string message = CreateLineMessage(row.Line);
            Assert.That(
                unicode.Text,
                Is.EqualTo(row.Unicode).IgnoreCase,
                message);
        }

        Assert.That(applicableCount, Is.GreaterThan(100));
    }

    [TestCase("例え。テスト", "xn--r8jz45g.xn--zckzah")]
    [TestCase("例え．テスト", "xn--r8jz45g.xn--zckzah")]
    [TestCase("例え｡テスト", "xn--r8jz45g.xn--zckzah")]
    [TestCase("例え.テスト.", "xn--r8jz45g.xn--zckzah.")]
    public void Uts46SeparatorMappingsPreserveLabelsAndRootState(
        string host,
        string expected)
    {
        IIriConverter converter = new IriConverter();
        IriConversionOptions options = new IriConversionOptions();
        options.IdnMode = IriIdnMode.Platform;

        string ascii = converter.ConvertHostToAscii(host, options);
        string unicode = converter.ConvertHostToUnicode(ascii, options);

        Assert.Multiple(() =>
        {
            Assert.That(ascii, Is.EqualTo(expected));
            Assert.That(unicode.Replace('。', '.').Replace('．', '.').Replace('｡', '.'),
                Is.EqualTo(host.Replace('。', '.').Replace('．', '.').Replace('｡', '.')));
        });
    }

    [TestCase("àא", UriDiagnosticCode.InvalidBidiText)]
    [TestCase("שלום1٢", UriDiagnosticCode.InvalidBidiText)]
    [TestCase("a\u200Cb", UriDiagnosticCode.InvalidIdnLabel)]
    [TestCase("a\u200Db", UriDiagnosticCode.InvalidIdnLabel)]
    [TestCase("\u0308.example", UriDiagnosticCode.InvalidIdnLabel)]
    [TestCase("ab--cd.example", UriDiagnosticCode.InvalidIdnLabel)]
    [TestCase("-example.test", UriDiagnosticCode.InvalidIdnLabel)]
    [TestCase("example-.test", UriDiagnosticCode.InvalidIdnLabel)]
    [TestCase("emoji😀.test", UriDiagnosticCode.InvalidIdnLabel)]
    public void InvalidLabelContextHyphenAndBidiVectorsAreRejected(
        string host,
        UriDiagnosticCode code)
    {
        UriTextResult result = IdnHostCore.ConvertHostToAscii(
            host.AsSpan(),
            IriIdnMode.Platform,
            new OperationLimits());

        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Iri));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Host));
        });
    }

    [Test]
    public void PlatformModeEnforcesDnsLabelAndDomainLengths()
    {
        string longLabel = "1234567890ä1234567890123456789012345678901234567890123456";
        string maximumDomain = new string('a', 63) + "." +
            new string('b', 63) + "." +
            new string('c', 63) + "." +
            new string('d', 61);
        string excessiveDomain = maximumDomain + "e";

        UriTextResult label = IdnHostCore.ConvertHostToAscii(
            longLabel.AsSpan(),
            IriIdnMode.Platform,
            new OperationLimits());
        UriTextResult exact = IdnHostCore.ConvertHostToAscii(
            maximumDomain.AsSpan(),
            IriIdnMode.Platform,
            new OperationLimits());
        UriTextResult domain = IdnHostCore.ConvertHostToAscii(
            excessiveDomain.AsSpan(),
            IriIdnMode.Platform,
            new OperationLimits());

        Assert.Multiple(() =>
        {
            Assert.That(label.Success, Is.False);
            Assert.That(label.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidIdnLabel));
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.Text, Is.EqualTo(maximumDomain));
            Assert.That(domain.Success, Is.False);
            Assert.That(domain.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidIdnLabel));
        });
    }

    private static bool IsAscii(string value)
    {
        int offset = 0;
        while (offset < value.Length)
        {
            if (value[offset] > 127)
            {
                return false;
            }

            offset = offset + 1;
        }

        return true;
    }

    private static string CreateLineMessage(int line)
    {
        return "IdnaTestV2.txt line " +
            line.ToString(CultureInfo.InvariantCulture);
    }

    private static IdnaRow[] LoadCorpusRows()
    {
        string path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "specs",
            "unicode",
            "IdnaTestV2.txt"));
        List<IdnaRow> rows = new List<IdnaRow>();
        int lineNumber = 0;
        foreach (string rawLine in File.ReadLines(path))
        {
            lineNumber = lineNumber + 1;
            string line = RemoveComment(rawLine).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            string[] columns = line.Split(';');
            if (columns.Length < 7)
            {
                throw new InvalidOperationException(
                    "Invalid IDNA corpus row at line " +
                    lineNumber.ToString(CultureInfo.InvariantCulture));
            }

            string source = DecodeEscapes(columns[0].Trim());
            string unicode = GetInheritedText(columns[1], source);
            string unicodeStatus = columns[2].Trim();
            string ascii = GetInheritedText(columns[3], unicode);
            string asciiStatus = columns[4].Trim();
            if (asciiStatus.Length == 0)
            {
                asciiStatus = unicodeStatus;
            }

            rows.Add(new IdnaRow(
                lineNumber,
                source,
                unicode,
                ascii,
                IsSuccessStatus(unicodeStatus),
                IsSuccessStatus(asciiStatus)));
        }

        if (rows.Count == 0)
        {
            throw new InvalidOperationException("The IDNA corpus is empty.");
        }

        return rows.ToArray();
    }

    private static string GetInheritedText(string column, string inherited)
    {
        string value = column.Trim();
        if (value.Length == 0)
        {
            return inherited;
        }

        return DecodeEscapes(value);
    }

    private static bool IsSuccessStatus(string status)
    {
        return status.Length == 0 || status == "[]";
    }

    private static string RemoveComment(string line)
    {
        int offset = line.IndexOf('#');
        if (offset < 0)
        {
            return line;
        }

        return line.Substring(0, offset);
    }

    private static string DecodeEscapes(string value)
    {
        if (value == "\"\"")
        {
            return "";
        }

        StringBuilder output = new StringBuilder();
        int offset = 0;
        while (offset < value.Length)
        {
            if (value[offset] == '\\' &&
                offset + 5 < value.Length &&
                value[offset + 1] == 'u')
            {
                int codeUnit = int.Parse(
                    value.Substring(offset + 2, 4),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture);
                output.Append((char)codeUnit);
                offset = offset + 6;
                continue;
            }

            if (value[offset] == '\\' &&
                offset + 3 < value.Length &&
                value[offset + 1] == 'x' &&
                value[offset + 2] == '{')
            {
                int end = value.IndexOf('}', offset + 3);
                if (end < 0)
                {
                    throw new InvalidOperationException("Invalid IDNA corpus escape.");
                }

                int scalarValue = int.Parse(
                    value.Substring(offset + 3, end - offset - 3),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture);
                if (scalarValue >= 0xD800 && scalarValue <= 0xDFFF)
                {
                    output.Append((char)scalarValue);
                }
                else
                {
                    output.Append(char.ConvertFromUtf32(scalarValue));
                }

                offset = end + 1;
                continue;
            }

            output.Append(value[offset]);
            offset = offset + 1;
        }

        return output.ToString();
    }

    private class IdnaRow
    {
        internal int Line;
        internal string Source;
        internal string Unicode;
        internal string Ascii;
        internal bool UnicodeValid;
        internal bool AsciiValid;

        internal IdnaRow(
            int line,
            string source,
            string unicode,
            string ascii,
            bool unicodeValid,
            bool asciiValid)
        {
            Line = line;
            Source = source;
            Unicode = unicode;
            Ascii = ascii;
            UnicodeValid = unicodeValid;
            AsciiValid = asciiValid;
        }
    }
}
