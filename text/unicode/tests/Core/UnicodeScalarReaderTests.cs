using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeScalarReaderTests
{
    [Test]
    public void ReadAll_ReturnsScalarsWithUtf16Offsets()
    {
        string text = "A" + char.ConvertFromUtf32(0x1F600) + "B";

        UnicodeScalarRecord[] records = new UnicodeScalarReader().ReadAll(text);

        Assert.That(records, Has.Length.EqualTo(3));
        AssertRecord(records[0], 0x0041, 0, 1, false);
        AssertRecord(records[1], 0x1F600, 1, 2, false);
        AssertRecord(records[2], 0x0042, 3, 1, false);
    }

    [Test]
    public void ReadAll_ReturnsReplacementRecordsForUnpairedSurrogates()
    {
        string text = "\uD800X\uDC00";

        UnicodeScalarRecord[] records = new UnicodeScalarReader().ReadAll(text);

        Assert.That(records, Has.Length.EqualTo(3));
        AssertRecord(records[0], UnicodeScalar.ReplacementCharacter, 0, 1, true);
        AssertRecord(records[1], 0x0058, 1, 1, false);
        AssertRecord(records[2], UnicodeScalar.ReplacementCharacter, 2, 1, true);
    }

    [Test]
    public void ReadAll_ReturnsReplacementWhenHighSurrogateIsFollowedByNonLowCodeUnit()
    {
        string text = "\uD800A";

        UnicodeScalarRecord[] records = new UnicodeScalarReader().ReadAll(text);

        Assert.That(records, Has.Length.EqualTo(2));
        AssertRecord(records[0], UnicodeScalar.ReplacementCharacter, 0, 1, true);
        AssertRecord(records[1], 0x0041, 1, 1, false);
    }

    [Test]
    public void ReadAllWithPolicy_UsesRequestedMalformedUtf16Behavior()
    {
        UnicodeScalarReader reader = new UnicodeScalarReader();
        string text = "\uD800A\uDC00";

        UnicodeScalarRecord[] records = reader.ReadAllWithPolicy(text, UnicodeMalformedInputPolicy.Replace);

        Assert.That(records, Has.Length.EqualTo(3));
        AssertRecord(records[0], UnicodeScalar.ReplacementCharacter, 0, 1, true);
        AssertRecord(records[1], 0x0041, 1, 1, false);
        AssertRecord(records[2], UnicodeScalar.ReplacementCharacter, 2, 1, true);
        Assert.Throws<System.Text.EncoderFallbackException>(() =>
            reader.ReadAllWithPolicy(text, UnicodeMalformedInputPolicy.Strict));
    }

    [Test]
    public void SpanEntryPoints_ReturnOffsetsAndReportRequiredCapacityWithoutPartialWrites()
    {
        UnicodeScalarReader reader = new UnicodeScalarReader();
        string text = "A" + char.ConvertFromUtf32(0x1F600) + "B";
        UnicodeScalarRecord[] tooSmall = new UnicodeScalarRecord[2];

        UnicodeBufferResult insufficient = reader.ReadIntoBuffer(
            text,
            tooSmall,
            UnicodeMalformedInputPolicy.Strict);

        Assert.That(insufficient.Success, Is.False);
        Assert.That(insufficient.RequiredLength, Is.EqualTo(3));
        Assert.That(insufficient.WrittenLength, Is.EqualTo(0));
        Assert.That(tooSmall, Is.All.Null);

        UnicodeScalarRecord[] destination = new UnicodeScalarRecord[3];
        UnicodeBufferResult complete = reader.ReadIntoBuffer(
            text,
            destination,
            UnicodeMalformedInputPolicy.Strict);

        Assert.That(complete.Success, Is.True);
        Assert.That(complete.RequiredLength, Is.EqualTo(3));
        Assert.That(complete.WrittenLength, Is.EqualTo(3));
        AssertRecord(destination[0], 0x0041, 0, 1, false);
        AssertRecord(destination[1], 0x1F600, 1, 2, false);
        AssertRecord(destination[2], 0x0042, 3, 1, false);

        UnicodeScalarRecord[] spanRecords = reader.ReadAllSpan(
            text.AsSpan(),
            UnicodeMalformedInputPolicy.Strict);
        Assert.That(spanRecords, Has.Length.EqualTo(3));
        AssertRecord(spanRecords[1], 0x1F600, 1, 2, false);
    }

    [Test]
    public void PolicyEntryPoints_RejectUnknownPolicy()
    {
        UnicodeMalformedInputPolicy policy = (UnicodeMalformedInputPolicy)99;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new UnicodeScalarReader().ReadAllWithPolicy("A", policy));
    }

    [Test]
    public void ReadAll_ReturnsEmptyArrayForEmptyString()
    {
        UnicodeScalarRecord[] records = new UnicodeScalarReader().ReadAll("");

        Assert.That(records, Is.Empty);
    }

    [Test]
    public void ReadAll_ThrowsForNullText()
    {
        Assert.Throws<ArgumentNullException>(() => new UnicodeScalarReader().ReadAll(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodeScalarReader().ReadAllWithPolicy(
            null!,
            UnicodeMalformedInputPolicy.Replace));
    }

    [Test]
    public void Validate_ReturnsValidResultForWellFormedUtf16()
    {
        string text = "A" + char.ConvertFromUtf32(0x1F600) + "B";

        UnicodeValidationResult result = new UnicodeScalarReader().Validate(text);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ScalarCount, Is.EqualTo(3));
        Assert.That(result.ReplacementCount, Is.EqualTo(0));
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Scalars, Has.Length.EqualTo(3));
        AssertRecord(result.Scalars[1], 0x1F600, 1, 2, false);
    }

    [Test]
    public void Validate_ReturnsDiagnosticsForUnpairedSurrogates()
    {
        string text = "\uD800X\uDC00\uD83D";

        UnicodeValidationResult result = new UnicodeScalarReader().Validate(text);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ScalarCount, Is.EqualTo(4));
        Assert.That(result.ReplacementCount, Is.EqualTo(3));
        Assert.That(result.Diagnostics, Has.Length.EqualTo(3));
        AssertDiagnostic(result.Diagnostics[0], 0, 0xD800, "UnpairedHighSurrogate");
        AssertDiagnostic(result.Diagnostics[1], 2, 0xDC00, "UnpairedLowSurrogate");
        AssertDiagnostic(result.Diagnostics[2], 3, 0xD83D, "UnpairedHighSurrogate");
    }

    [Test]
    public void ReplaceInvalid_PreservesValidTextAndReplacesInvalidCodeUnits()
    {
        string validText = "A" + char.ConvertFromUtf32(0x1F600) + "B";
        string invalidText = "\uD800X\uDC00\uD83D";

        Assert.That(new UnicodeScalarReader().ReplaceInvalid(validText), Is.EqualTo(validText));
        Assert.That(new UnicodeScalarReader().ReplaceInvalid(invalidText), Is.EqualTo("\uFFFDX\uFFFD\uFFFD"));
    }

    [Test]
    public void ValidateAndReplaceInvalid_ThrowForNullText()
    {
        Assert.Throws<ArgumentNullException>(() => new UnicodeScalarReader().Validate(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodeScalarReader().ReplaceInvalid(null!));
    }

    static void AssertRecord(UnicodeScalarRecord record, int codePoint, int utf16Index, int utf16Length, bool isReplacement)
    {
        Assert.That(record.CodePoint, Is.EqualTo(codePoint));
        Assert.That(record.Utf16Index, Is.EqualTo(utf16Index));
        Assert.That(record.Utf16Length, Is.EqualTo(utf16Length));
        Assert.That(record.IsReplacement, Is.EqualTo(isReplacement));
    }

    static void AssertDiagnostic(UnicodeUtf16Diagnostic diagnostic, int utf16Index, int codeUnit, string rule)
    {
        Assert.That(diagnostic.Utf16Index, Is.EqualTo(utf16Index));
        Assert.That(diagnostic.Utf16Length, Is.EqualTo(1));
        Assert.That(diagnostic.CodeUnit, Is.EqualTo(codeUnit));
        Assert.That(diagnostic.Rule, Is.EqualTo(rule));
        Assert.That(diagnostic.Message, Is.Not.Empty);
    }
}
