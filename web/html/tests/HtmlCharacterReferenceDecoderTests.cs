
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlCharacterReferenceDecoderTests
{
    [Test]
    public void NamedReferencesUseLongestCaseSensitiveMatches()
    {
        HtmlCharacterReferenceResult ampersand = Decode("amp;", HtmlCharacterReferenceContext.Data);
        AssertMatch(ampersand, 4, 0x26, -1);
        Assert.That(ampersand.GetReplacementString(), Is.EqualTo("&"));
        Assert.That(ampersand.GetDiagnosticCount(), Is.Zero);

        HtmlCharacterReferenceResult pair = Decode("NotEqualTilde;", HtmlCharacterReferenceContext.Data);
        AssertMatch(pair, 14, 0x2242, 0x0338);
        Assert.That(pair.GetReplacementString(), Is.EqualTo("≂̸"));

        HtmlCharacterReferenceResult legacy = Decode("notit;", HtmlCharacterReferenceContext.Data);
        AssertMatch(legacy, 3, 0x00AC, -1);
        Assert.That(legacy.GetDiagnosticCode(0), Is.EqualTo(HtmlDiagnosticCode.CharacterReferenceMissingSemicolon));
        Assert.That(legacy.GetDiagnosticCharacterLength(0), Is.EqualTo(4));

        Assert.That(Decode("AMP;", HtmlCharacterReferenceContext.Data).GetFirstCodePoint(), Is.EqualTo('&'));
        Assert.That(Decode("Amp;", HtmlCharacterReferenceContext.Data).GetStatus(),
            Is.EqualTo(HtmlCharacterReferenceStatus.NoMatch));
    }

    [Test]
    public void AttributeContextAppliesTheHistoricalAmbiguousAmpersandRule()
    {
        HtmlCharacterReferenceResult alphanumeric = Decode("notit;", HtmlCharacterReferenceContext.Attribute);
        HtmlCharacterReferenceResult equals = Decode("amp=value", HtmlCharacterReferenceContext.Attribute);
        Assert.That(alphanumeric.GetStatus(), Is.EqualTo(HtmlCharacterReferenceStatus.NoMatch));
        Assert.That(equals.GetStatus(), Is.EqualTo(HtmlCharacterReferenceStatus.NoMatch));
        Assert.That(alphanumeric.GetDiagnosticCount(), Is.Zero);
        Assert.That(equals.GetDiagnosticCount(), Is.Zero);

        HtmlCharacterReferenceResult punctuation = Decode("copy!", HtmlCharacterReferenceContext.Attribute);
        AssertMatch(punctuation, 4, 0x00A9, -1);
        Assert.That(punctuation.GetDiagnosticCode(0),
            Is.EqualTo(HtmlDiagnosticCode.CharacterReferenceMissingSemicolon));

        HtmlCharacterReferenceResult data = Decode("amp=value", HtmlCharacterReferenceContext.Data);
        AssertMatch(data, 3, '&', -1);

        HtmlCharacterReferenceResult allowed = HtmlCharacterReferenceDecoder.Decode(
            "\"amp;".AsSpan(), true, HtmlCharacterReferenceContext.Attribute, '"');
        Assert.That(allowed.GetStatus(), Is.EqualTo(HtmlCharacterReferenceStatus.NoMatch));
    }

    [Test]
    public void AmbiguousAmpersandsRemainLiteralAndDiagnoseOnlyUnknownSemicolonNames()
    {
        HtmlCharacterReferenceResult unknown = Decode("bogus;", HtmlCharacterReferenceContext.Data);
        Assert.That(unknown.GetStatus(), Is.EqualTo(HtmlCharacterReferenceStatus.NoMatch));
        Assert.That(unknown.GetConsumedCharacters(), Is.Zero);
        Assert.That(unknown.GetReplacementString(), Is.Empty);
        Assert.That(unknown.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(unknown.GetDiagnosticCode(0), Is.EqualTo(HtmlDiagnosticCode.CharacterReferenceInvalid));
        Assert.That(unknown.GetDiagnosticCharacterLength(0), Is.EqualTo(7));

        Assert.That(Decode("bogus!", HtmlCharacterReferenceContext.Data).GetDiagnosticCount(), Is.Zero);
        Assert.That(Decode("=value", HtmlCharacterReferenceContext.Data).GetStatus(),
            Is.EqualTo(HtmlCharacterReferenceStatus.NoMatch));
        Assert.That(Decode("&amp;", HtmlCharacterReferenceContext.Data).GetStatus(),
            Is.EqualTo(HtmlCharacterReferenceStatus.NoMatch));
    }

    [Test]
    public void NumericReferencesDecodeDecimalHexAndMissingSemicolons()
    {
        AssertMatch(Decode("#128169;", HtmlCharacterReferenceContext.Data), 8, 0x1F4A9, -1);
        AssertMatch(Decode("#x1F4A9;", HtmlCharacterReferenceContext.Data), 8, 0x1F4A9, -1);
        AssertMatch(Decode("#X41;", HtmlCharacterReferenceContext.Data), 5, 'A', -1);

        HtmlCharacterReferenceResult missing = Decode("#65x", HtmlCharacterReferenceContext.Data);
        AssertMatch(missing, 3, 'A', -1);
        Assert.That(missing.GetDiagnosticCode(0),
            Is.EqualTo(HtmlDiagnosticCode.CharacterReferenceMissingSemicolon));
        Assert.That(missing.GetDiagnosticCharacterLength(0), Is.EqualTo(4));

        HtmlCharacterReferenceResult decimalDigits = Decode("#;", HtmlCharacterReferenceContext.Data);
        HtmlCharacterReferenceResult hexadecimalDigits = Decode("#x;", HtmlCharacterReferenceContext.Data);
        Assert.That(decimalDigits.GetStatus(), Is.EqualTo(HtmlCharacterReferenceStatus.NoMatch));
        Assert.That(decimalDigits.GetDiagnosticCharacterLength(0), Is.EqualTo(3));
        Assert.That(hexadecimalDigits.GetStatus(), Is.EqualTo(HtmlCharacterReferenceStatus.NoMatch));
        Assert.That(hexadecimalDigits.GetDiagnosticCharacterLength(0), Is.EqualTo(4));
    }

    [Test]
    public void NumericReferencesApplyWindows1252ControlReplacements()
    {
        int[] replacements =
        [
            0x20AC, 0x81, 0x201A, 0x0192, 0x201E, 0x2026, 0x2020, 0x2021,
            0x02C6, 0x2030, 0x0160, 0x2039, 0x0152, 0x8D, 0x017D, 0x8F,
            0x90, 0x2018, 0x2019, 0x201C, 0x201D, 0x2022, 0x2013, 0x2014,
            0x02DC, 0x2122, 0x0161, 0x203A, 0x0153, 0x9D, 0x017E, 0x0178
        ];

        for (int value = 0x80; value <= 0x9F; value++)
        {
            HtmlCharacterReferenceResult result = Decode("#" + value + ";", HtmlCharacterReferenceContext.Data);
            Assert.That(result.GetFirstCodePoint(), Is.EqualTo(replacements[value - 0x80]), $"U+{value:X4}");
            Assert.That(result.GetDiagnosticCode(0), Is.EqualTo(HtmlDiagnosticCode.CharacterReferenceInvalid));
        }
    }

    [Test]
    public void InvalidNumericScalarsFollowHtmlRecovery()
    {
        HtmlCharacterReferenceResult zero = Decode("#0;", HtmlCharacterReferenceContext.Data);
        HtmlCharacterReferenceResult surrogate = Decode("#xD800;", HtmlCharacterReferenceContext.Data);
        HtmlCharacterReferenceResult outOfRange = Decode("#1114112;", HtmlCharacterReferenceContext.Data);
        HtmlCharacterReferenceResult huge = Decode("#999999999999999999999999;", HtmlCharacterReferenceContext.Data);
        AssertMatch(zero, 3, 0xFFFD, -1);
        AssertMatch(surrogate, 7, 0xFFFD, -1);
        AssertMatch(outOfRange, 9, 0xFFFD, -1);
        Assert.That(huge.GetFirstCodePoint(), Is.EqualTo(0xFFFD));

        HtmlCharacterReferenceResult noncharacter = Decode("#xFDD0;", HtmlCharacterReferenceContext.Data);
        Assert.That(noncharacter.GetFirstCodePoint(), Is.EqualTo(0xFDD0));
        Assert.That(noncharacter.GetDiagnosticCode(0), Is.EqualTo(HtmlDiagnosticCode.CharacterReferenceInvalid));

        HtmlCharacterReferenceResult carriageReturn = Decode("#13;", HtmlCharacterReferenceContext.Data);
        Assert.That(carriageReturn.GetFirstCodePoint(), Is.EqualTo(13));
        Assert.That(carriageReturn.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(Decode("#9;", HtmlCharacterReferenceContext.Data).GetDiagnosticCount(), Is.Zero);

        HtmlCharacterReferenceResult combined = Decode("#128", HtmlCharacterReferenceContext.Data);
        Assert.That(combined.GetDiagnosticCount(), Is.EqualTo(2));
        Assert.That(combined.GetDiagnosticCode(0),
            Is.EqualTo(HtmlDiagnosticCode.CharacterReferenceMissingSemicolon));
        Assert.That(combined.GetDiagnosticCode(1), Is.EqualTo(HtmlDiagnosticCode.CharacterReferenceInvalid));
    }

    [Test]
    public void UndecidableChunkPrefixesRequestMoreInput()
    {
        AssertNeedsEveryPrefix("CounterClockwiseContourIntegral;");
        AssertNeedsEveryPrefix("#x1F4A9;");
        AssertNeedsEveryPrefix("bogus;");

        Assert.That(HtmlCharacterReferenceDecoder.Decode("amp!".AsSpan(), false,
            HtmlCharacterReferenceContext.Data).GetStatus(), Is.EqualTo(HtmlCharacterReferenceStatus.Match));
        Assert.That(HtmlCharacterReferenceDecoder.Decode("bogus!".AsSpan(), false,
            HtmlCharacterReferenceContext.Data).GetStatus(), Is.EqualTo(HtmlCharacterReferenceStatus.NoMatch));
    }

    [Test]
    public void ResultDiagnosticsUseInputSourceSpans()
    {
        HtmlBudget budget = new HtmlBudget(HtmlLimits.CreateDefault(), CancellationToken.None);
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        HtmlInput input = HtmlInput.Create(budget, diagnostics);
        input.TryAppend("x\n&notit;");
        input.Complete();

        HtmlCharacterReferenceResult result = Decode("notit;", HtmlCharacterReferenceContext.Data);
        result.AddDiagnostics(diagnostics, input, 2);

        HtmlDiagnostic found = diagnostics.GetDiagnostics().Single();
        Assert.That(found.GetCode(), Is.EqualTo(HtmlDiagnosticCode.CharacterReferenceMissingSemicolon));
        Assert.That(found.GetSpan().GetStart().GetLine(), Is.EqualTo(2));
        Assert.That(found.GetSpan().GetStart().GetColumn(), Is.EqualTo(1));
        Assert.That(found.GetSpan().GetCharacterLength(), Is.EqualTo(4));
    }

    [Test]
    public void DecoderAndResultsValidateArguments()
    {
        Assert.Throws<ArgumentNullException>(() => HtmlCharacterReferenceDecoder.Decode(
            (string)null, true, HtmlCharacterReferenceContext.Data));
        Assert.Throws<ArgumentOutOfRangeException>(() => HtmlCharacterReferenceDecoder.Decode(
            "amp;".AsSpan(), true, (HtmlCharacterReferenceContext)99));

        HtmlCharacterReferenceResult result = Decode("amp;", HtmlCharacterReferenceContext.Data);
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetDiagnosticCode(0));
        Assert.Throws<ArgumentNullException>(() => result.AddDiagnostics(null, null, 0));
    }

    static HtmlCharacterReferenceResult Decode(string input, HtmlCharacterReferenceContext context)
    {
        return HtmlCharacterReferenceDecoder.Decode(input.AsSpan(), true, context);
    }

    static void AssertMatch(HtmlCharacterReferenceResult result, int consumed, int first, int second)
    {
        Assert.That(result.GetStatus(), Is.EqualTo(HtmlCharacterReferenceStatus.Match));
        Assert.That(result.IsMatch(), Is.True);
        Assert.That(result.GetConsumedCharacters(), Is.EqualTo(consumed));
        Assert.That(result.GetFirstCodePoint(), Is.EqualTo(first));
        Assert.That(result.GetSecondCodePoint(), Is.EqualTo(second));
        Assert.That(result.HasSecondCodePoint(), Is.EqualTo(second >= 0));
    }

    static void AssertNeedsEveryPrefix(string input)
    {
        for (int length = 0; length < input.Length; length++)
        {
            HtmlCharacterReferenceResult result = HtmlCharacterReferenceDecoder.Decode(
                input.AsSpan(0, length), false, HtmlCharacterReferenceContext.Data);
            Assert.That(result.GetStatus(), Is.EqualTo(HtmlCharacterReferenceStatus.NeedMoreInput),
                $"prefix '{input[..length]}'");
        }

        Assert.That(HtmlCharacterReferenceDecoder.Decode(input.AsSpan(), false,
            HtmlCharacterReferenceContext.Data).GetStatus(), Is.Not.EqualTo(HtmlCharacterReferenceStatus.NeedMoreInput));
    }
}
