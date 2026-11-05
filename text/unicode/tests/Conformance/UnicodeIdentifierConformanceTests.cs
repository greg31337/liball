using System.Text;
using NUnit.Framework;
using unicode.Tools.UnicodeDataGenerator;

namespace unicode.Tests;

[TestFixture]
public class UnicodeIdentifierConformanceTests
{
    [Test]
    public void Uax31IdentifierProfileSample_MatchesValidationAndPreparationProfiles()
    {
        IdentifierProfileSampleRow[] rows = ReadRows(ConformancePath("Uax31IdentifierProfile.sample.txt"));

        Assert.That(rows, Has.Length.GreaterThan(0));

        int index = 0;
        while (index < rows.Length)
        {
            AssertRow(rows[index]);
            index = index + 1;
        }
    }

    static void AssertRow(IdentifierProfileSampleRow row)
    {
        string text = ToText(row.CodePoints);
        UnicodeIdentifierProfile defaultProfile = new UnicodeProfiles().CreateDefaultIdentifier();
        UnicodeIdentifierProfile immutableProfile = new UnicodeProfiles().CreateImmutableIdentifier();
        UnicodeIdentifierProfile normalizedProfile = new UnicodeProfiles().CreateNormalizedIdentifier();
        UnicodeIdentifierProfile caseInsensitiveProfile = new UnicodeProfiles().CreateCaseInsensitiveIdentifier();

        Assert.That(new UnicodeIdentifiers().ValidateIdentifier(text, defaultProfile).IsValid, Is.EqualTo(row.DefaultValid), row.Comment + " default");
        Assert.That(new UnicodeIdentifiers().ValidateIdentifier(text, immutableProfile).IsValid, Is.EqualTo(row.ImmutableValid), row.Comment + " immutable");
        Assert.That(new UnicodeIdentifiers().ValidateIdentifier(text, normalizedProfile).IsValid, Is.EqualTo(row.NormalizedValid), row.Comment + " normalized");
        Assert.That(new UnicodeIdentifiers().ValidateIdentifier(text, caseInsensitiveProfile).IsValid, Is.EqualTo(row.CaseInsensitiveValid), row.Comment + " case-insensitive");
        Assert.That(new UnicodeIdentifiers().NormalizeIdentifier(text, normalizedProfile), Is.EqualTo(ToText(row.NormalizedCodePoints)), row.Comment + " normalized form");
        Assert.That(new UnicodeIdentifiers().GetIdentifierCompareKey(text, caseInsensitiveProfile), Is.EqualTo(ToText(row.CaseInsensitiveKeyCodePoints)), row.Comment + " case key");
    }

    static IdentifierProfileSampleRow[] ReadRows(string path)
    {
        List<IdentifierProfileSampleRow> rows = new List<IdentifierProfileSampleRow>();

        foreach (string rawLine in File.ReadLines(path))
        {
            string dataLine = UnicodeDataParser.StripComment(rawLine).Trim();
            if (dataLine.Length == 0)
            {
                continue;
            }

            string[] fields = UnicodeDataParser.SplitFields(dataLine);
            Assert.That(fields, Has.Length.EqualTo(7), rawLine);
            rows.Add(new IdentifierProfileSampleRow(
                UnicodeDataParser.ParseCodePointSequence(fields[0]),
                ParseBoolean(fields[1]),
                ParseBoolean(fields[2]),
                ParseBoolean(fields[3]),
                ParseBoolean(fields[4]),
                UnicodeDataParser.ParseCodePointSequence(fields[5]),
                UnicodeDataParser.ParseCodePointSequence(fields[6]),
                UnicodeDataParser.ExtractComment(rawLine)));
        }

        return rows.ToArray();
    }

    static bool ParseBoolean(string text)
    {
        if (text == "true")
        {
            return true;
        }

        if (text == "false")
        {
            return false;
        }

        Assert.Fail("Expected boolean literal but found " + text);
        return false;
    }

    static string ToText(int[] codePoints)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;

        while (index < codePoints.Length)
        {
            builder.Append(new UnicodeScalar().ToUtf16String(codePoints[index]));
            index = index + 1;
        }

        return builder.ToString();
    }

    static string ConformancePath(string fileName)
    {
        return Path.Combine(FindRepositoryRoot(), "tests", "Conformance", fileName);
    }

    static string FindRepositoryRoot()
    {
        string directory = TestContext.CurrentContext.TestDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "unicode.slnx")) && Directory.Exists(Path.Combine(directory, "src")))
            {
                return directory;
            }

            DirectoryInfo? parent = Directory.GetParent(directory);
            if (parent == null)
            {
                break;
            }

            directory = parent.FullName;
        }

        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
    }
}

public sealed class IdentifierProfileSampleRow
{
    public int[] CodePoints;
    public bool DefaultValid;
    public bool ImmutableValid;
    public bool NormalizedValid;
    public bool CaseInsensitiveValid;
    public int[] NormalizedCodePoints;
    public int[] CaseInsensitiveKeyCodePoints;
    public string Comment;

    public IdentifierProfileSampleRow(
        int[] codePoints,
        bool defaultValid,
        bool immutableValid,
        bool normalizedValid,
        bool caseInsensitiveValid,
        int[] normalizedCodePoints,
        int[] caseInsensitiveKeyCodePoints,
        string comment)
    {
        CodePoints = codePoints;
        DefaultValid = defaultValid;
        ImmutableValid = immutableValid;
        NormalizedValid = normalizedValid;
        CaseInsensitiveValid = caseInsensitiveValid;
        NormalizedCodePoints = normalizedCodePoints;
        CaseInsensitiveKeyCodePoints = caseInsensitiveKeyCodePoints;
        Comment = comment;
    }
}
