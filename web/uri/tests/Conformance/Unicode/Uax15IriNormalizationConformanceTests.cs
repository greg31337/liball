using System.Globalization;
using System.Text;

namespace uri.Tests;

public class Uax15IriNormalizationConformanceTests
{
    private static readonly NormalizationRow[] CorpusRows = LoadCorpusRows();

    [Test]
    public void ComponentNfcTransformationSatisfiesEveryCorpusInvariant()
    {
        foreach (NormalizationRow row in CorpusRows)
        {
            AssertComponentNfc(row.Source, row.Nfc, row.Line);
            AssertComponentNfc(row.Nfc, row.Nfc, row.Line);
            AssertComponentNfc(row.Nfd, row.Nfc, row.Line);
            AssertComponentNfc(row.Nfkc, row.Nfkc, row.Line);
            AssertComponentNfc(row.Nfkd, row.Nfkc, row.Line);
        }
    }

    [Test]
    public void NormalizeNfcModeSatisfiesEveryCorpusInvariantApplicableToIris()
    {
        IIriNormalizer normalizer = new IriNormalizer();
        IriConversionOptions options = new IriConversionOptions();
        options.NormalizationMode = UnicodeNormalizationMode.NormalizeNfc;

        foreach (NormalizationRow row in CorpusRows)
        {
            AssertNormalized(normalizer, options, row.Source, row.Nfc, row.Line);
            AssertNormalized(normalizer, options, row.Nfc, row.Nfc, row.Line);
            AssertNormalized(normalizer, options, row.Nfd, row.Nfc, row.Line);
            AssertNormalized(normalizer, options, row.Nfkc, row.Nfkc, row.Line);
            AssertNormalized(normalizer, options, row.Nfkd, row.Nfkc, row.Line);
        }
    }

    [Test]
    public void PreserveModeLeavesEveryCanonicalCorpusColumnUnchanged()
    {
        IIriNormalizer normalizer = new IriNormalizer();
        IriConversionOptions options = new IriConversionOptions();
        options.NormalizationMode = UnicodeNormalizationMode.Preserve;

        foreach (NormalizationRow row in CorpusRows)
        {
            AssertPreserved(normalizer, options, row.Source, row.Line);
            AssertPreserved(normalizer, options, row.Nfc, row.Line);
            AssertPreserved(normalizer, options, row.Nfd, row.Line);
        }
    }

    [Test]
    public void DiagnoseModeMatchesCorpusNfcStatusWithoutChangingText()
    {
        IIriNormalizer normalizer = new IriNormalizer();
        IriConversionOptions options = new IriConversionOptions();
        options.NormalizationMode = UnicodeNormalizationMode.DiagnoseNfc;

        foreach (NormalizationRow row in CorpusRows)
        {
            AssertDiagnosis(
                normalizer,
                options,
                row.Source,
                row.Source == row.Nfc,
                row.Line);
            AssertDiagnosis(normalizer, options, row.Nfc, true, row.Line);
            AssertDiagnosis(
                normalizer,
                options,
                row.Nfd,
                row.Nfd == row.Nfc,
                row.Line);
        }
    }

    private static void AssertNormalized(
        IIriNormalizer normalizer,
        IriConversionOptions options,
        string inputValue,
        string expectedValue,
        int line)
    {
        if (!IsLegalPathText(inputValue))
        {
            return;
        }

        string input = BuildIri(inputValue);
        string message = "NormalizationTest.txt line " +
            line.ToString(CultureInfo.InvariantCulture);
        if (!IsLegalPathText(expectedValue))
        {
            IriConversionException exception = Assert.Throws<IriConversionException>(() =>
                normalizer.NormalizeIri(input, options), message)!;
            Assert.That(
                exception.Diagnostic.Domain,
                Is.EqualTo(DiagnosticDomain.Iri),
                message);
            Assert.That(
                exception.Diagnostic.Code,
                Is.EqualTo(UriDiagnosticCode.InvalidPath),
                message);
            return;
        }

        string expected = BuildIri(expectedValue);
        string actual = normalizer.NormalizeIri(input, options);
        Assert.That(
            actual,
            Is.EqualTo(expected),
            message);
    }

    private static void AssertPreserved(
        IIriNormalizer normalizer,
        IriConversionOptions options,
        string value,
        int line)
    {
        if (!IsLegalPathText(value))
        {
            return;
        }

        string input = BuildIri(value);
        string actual = normalizer.NormalizeIri(input, options);

        Assert.That(
            actual,
            Is.EqualTo(input),
            "NormalizationTest.txt line " + line.ToString(CultureInfo.InvariantCulture));
    }

    private static void AssertDiagnosis(
        IIriNormalizer normalizer,
        IriConversionOptions options,
        string value,
        bool normalized,
        int line)
    {
        if (!IsLegalPathText(value))
        {
            return;
        }

        UriDiagnostic? diagnostic = normalizer.DiagnoseIriNormalization(
            BuildIri(value),
            options);
        string message = "NormalizationTest.txt line " +
            line.ToString(CultureInfo.InvariantCulture);
        if (normalized)
        {
            Assert.That(diagnostic, Is.Null, message);
            return;
        }

        Assert.That(diagnostic, Is.Not.Null, message);
        Assert.Multiple(() =>
        {
            Assert.That(
                diagnostic!.Domain,
                Is.EqualTo(DiagnosticDomain.Iri),
                message);
            Assert.That(
                diagnostic.Code,
                Is.EqualTo(UriDiagnosticCode.NormalizationRequired),
                message);
            Assert.That(
                diagnostic.Component,
                Is.EqualTo(UriComponent.Path),
                message);
        });
    }

    private static bool IsLegalPathText(string value)
    {
        UriDiagnostic? diagnostic = IriComponentScanner.ValidatePath(
            value.AsSpan(),
            0,
            false);
        return diagnostic == null;
    }

    private static void AssertComponentNfc(
        string input,
        string expected,
        int line)
    {
        TextOutput output = new TextOutput(expected.Length + 16);
        IriNormalizerCore.AppendNfcComponent(input, output);
        TextOutputResult status = output.GetStatus();
        string message = "NormalizationTest.txt line " +
            line.ToString(CultureInfo.InvariantCulture);

        Assert.Multiple(() =>
        {
            Assert.That(status.Success, Is.True, message);
            Assert.That(output.GetText(), Is.EqualTo(expected), message);
        });
    }

    private static string BuildIri(string value)
    {
        return "x:/" + value + "/end";
    }

    private static NormalizationRow[] LoadCorpusRows()
    {
        string path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "specs",
            "unicode",
            "NormalizationTest.txt"));
        List<NormalizationRow> rows = new List<NormalizationRow>();
        int lineNumber = 0;
        foreach (string rawLine in File.ReadLines(path))
        {
            lineNumber = lineNumber + 1;
            string line = RemoveComment(rawLine).Trim();
            if (line.Length == 0 || line[0] == '@')
            {
                continue;
            }

            string[] columns = line.Split(';');
            if (columns.Length < 5)
            {
                throw new InvalidOperationException(
                    "Invalid normalization corpus row at line " +
                    lineNumber.ToString(CultureInfo.InvariantCulture));
            }

            rows.Add(new NormalizationRow(
                lineNumber,
                DecodeCodePoints(columns[0]),
                DecodeCodePoints(columns[1]),
                DecodeCodePoints(columns[2]),
                DecodeCodePoints(columns[3]),
                DecodeCodePoints(columns[4])));
        }

        if (rows.Count == 0)
        {
            throw new InvalidOperationException("The normalization corpus is empty.");
        }

        return rows.ToArray();
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

    private static string DecodeCodePoints(string column)
    {
        string[] values = column.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries);
        StringBuilder text = new StringBuilder();
        foreach (string value in values)
        {
            int scalarValue = int.Parse(
                value,
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture);
            text.Append(char.ConvertFromUtf32(scalarValue));
        }

        return text.ToString();
    }

    private class NormalizationRow
    {
        internal int Line;
        internal string Source;
        internal string Nfc;
        internal string Nfd;
        internal string Nfkc;
        internal string Nfkd;

        internal NormalizationRow(
            int line,
            string source,
            string nfc,
            string nfd,
            string nfkc,
            string nfkd)
        {
            Line = line;
            Source = source;
            Nfc = nfc;
            Nfd = nfd;
            Nfkc = nfkc;
            Nfkd = nfkd;
        }
    }
}
