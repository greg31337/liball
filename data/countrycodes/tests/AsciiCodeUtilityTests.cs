using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class AsciiCodeUtilityTests
{
    [Test]
    public void CharacterClassificationAndUpperCasingAreAsciiOnly()
    {
        Assert.That(AsciiCodeUtility.IsAsciiLetter('A'), Is.True);
        Assert.That(AsciiCodeUtility.IsAsciiLetter('z'), Is.True);
        Assert.That(AsciiCodeUtility.IsAsciiLetter('@'), Is.False);
        Assert.That(AsciiCodeUtility.IsAsciiLetter('é'), Is.False);
        Assert.That(AsciiCodeUtility.IsAsciiDigit('0'), Is.True);
        Assert.That(AsciiCodeUtility.IsAsciiDigit('9'), Is.True);
        Assert.That(AsciiCodeUtility.IsAsciiDigit('/'), Is.False);
        Assert.That(AsciiCodeUtility.IsAsciiDigit('９'), Is.False);
        Assert.That(AsciiCodeUtility.IsAsciiAlphanumeric('q'), Is.True);
        Assert.That(AsciiCodeUtility.IsAsciiAlphanumeric('7'), Is.True);
        Assert.That(AsciiCodeUtility.IsAsciiAlphanumeric('-'), Is.False);
        Assert.That(AsciiCodeUtility.ToUpperAscii('a'), Is.EqualTo('A'));
        Assert.That(AsciiCodeUtility.ToUpperAscii('Z'), Is.EqualTo('Z'));
        Assert.That(AsciiCodeUtility.ToUpperAscii('é'), Is.EqualTo('é'));
    }

    [Test]
    public void AlphaNormalizationRequiresExactAsciiLengths()
    {
        char[] alpha2 = { '?', '?' };
        char[] alpha3 = { '?', '?', '?' };
        char[] alpha4 = { '?', '?', '?', '?' };

        Assert.That(AsciiCodeUtility.IsAlpha2("uS"), Is.True);
        Assert.That(AsciiCodeUtility.IsAlpha2("USA"), Is.False);
        Assert.That(AsciiCodeUtility.IsAlpha3("gBr"), Is.True);
        Assert.That(AsciiCodeUtility.IsAlpha3("GB1"), Is.False);
        Assert.That(AsciiCodeUtility.IsAlpha4("aNhH"), Is.True);
        Assert.That(AsciiCodeUtility.IsAlpha4("AÑHH"), Is.False);
        Assert.That(AsciiCodeUtility.TryNormalizeAlpha2("uS", alpha2), Is.True);
        Assert.That(AsciiCodeUtility.TryNormalizeAlpha3("gBr", alpha3), Is.True);
        Assert.That(AsciiCodeUtility.TryNormalizeAlpha4("aNhH", alpha4), Is.True);
        Assert.That(new string(alpha2), Is.EqualTo("US"));
        Assert.That(new string(alpha3), Is.EqualTo("GBR"));
        Assert.That(new string(alpha4), Is.EqualTo("ANHH"));

        char[] unchanged = { '?', '?' };
        Assert.That(AsciiCodeUtility.TryNormalizeAlpha2("U1", unchanged), Is.False);
        Assert.That(unchanged, Is.EqualTo(new[] { '?', '?' }));
        Assert.That(AsciiCodeUtility.TryNormalizeAlpha3("USA", new char[2]), Is.False);
    }

    [Test]
    public void NumericParsingAndFormattingPreserveThreeDigitIdentity()
    {
        Assert.That(AsciiCodeUtility.IsNumeric3("000"), Is.True);
        Assert.That(AsciiCodeUtility.IsNumeric3("004"), Is.True);
        Assert.That(AsciiCodeUtility.IsNumeric3("999"), Is.True);
        Assert.That(AsciiCodeUtility.IsNumeric3("04"), Is.False);
        Assert.That(AsciiCodeUtility.IsNumeric3("0A4"), Is.False);
        Assert.That(AsciiCodeUtility.ParseNumeric3Value("000"), Is.Zero);
        Assert.That(AsciiCodeUtility.ParseNumeric3Value("004"), Is.EqualTo(4));
        Assert.That(AsciiCodeUtility.ParseNumeric3Value("999"), Is.EqualTo(999));
        Assert.That(AsciiCodeUtility.ParseNumeric3Value("99"), Is.EqualTo(-1));

        char[] numeric = { '?', '?', '?' };
        Assert.That(AsciiCodeUtility.TryFormatNumeric3(4, numeric), Is.True);
        Assert.That(new string(numeric), Is.EqualTo("004"));
        Assert.That(AsciiCodeUtility.TryFormatNumeric3(999, numeric), Is.True);
        Assert.That(new string(numeric), Is.EqualTo("999"));

        char[] copied = { '?', '?', '?' };
        Assert.That(AsciiCodeUtility.TryNormalizeNumeric3("020", copied), Is.True);
        Assert.That(new string(copied), Is.EqualTo("020"));

        char[] unchanged = { '?', '?', '?' };
        Assert.That(AsciiCodeUtility.TryFormatNumeric3(-1, unchanged), Is.False);
        Assert.That(AsciiCodeUtility.TryFormatNumeric3(1000, unchanged), Is.False);
        Assert.That(unchanged, Is.EqualTo(new[] { '?', '?', '?' }));
        Assert.That(AsciiCodeUtility.TryFormatNumeric3(20, new char[2]), Is.False);
    }

    [Test]
    public void SubdivisionNormalizationCanonicalizesCaseAndPermittedSeparators()
    {
        char[] canonical = new char[6];
        char[] permissive = new char[5];

        Assert.That(AsciiCodeUtility.IsSubdivisionSeparator('-', false), Is.True);
        Assert.That(AsciiCodeUtility.IsSubdivisionSeparator('_', false), Is.False);
        Assert.That(AsciiCodeUtility.IsSubdivisionSeparator('_', true), Is.True);
        Assert.That(AsciiCodeUtility.IsSubdivisionCode("gb-eng", false), Is.True);
        Assert.That(AsciiCodeUtility.IsSubdivisionCode("us_ca", false), Is.False);
        Assert.That(AsciiCodeUtility.IsSubdivisionCode("us_ca", true), Is.True);
        Assert.That(AsciiCodeUtility.TryNormalizeSubdivisionCode("gb-eng", canonical, false), Is.True);
        Assert.That(AsciiCodeUtility.TryNormalizeSubdivisionCode("us_ca", permissive, true), Is.True);
        Assert.That(new string(canonical), Is.EqualTo("GB-ENG"));
        Assert.That(new string(permissive), Is.EqualTo("US-CA"));
    }

    [Test]
    public void SubdivisionValidationRejectsInvalidShapeWithoutWriting()
    {
        Assert.That(AsciiCodeUtility.IsSubdivisionCode("US-A", false), Is.True);
        Assert.That(AsciiCodeUtility.IsSubdivisionCode("US-A12", false), Is.True);
        Assert.That(AsciiCodeUtility.IsSubdivisionCode("US-", false), Is.False);
        Assert.That(AsciiCodeUtility.IsSubdivisionCode("US-A123", false), Is.False);
        Assert.That(AsciiCodeUtility.IsSubdivisionCode("U-A", false), Is.False);
        Assert.That(AsciiCodeUtility.IsSubdivisionCode("US-A!", false), Is.False);
        Assert.That(AsciiCodeUtility.IsSubdivisionCode("ÜS-A", false), Is.False);

        char[] unchanged = { '?', '?', '?', '?', '?' };
        Assert.That(
            AsciiCodeUtility.TryNormalizeSubdivisionCode("US_A!", unchanged, true),
            Is.False);
        Assert.That(unchanged, Is.EqualTo(new[] { '?', '?', '?', '?', '?' }));
        Assert.That(
            AsciiCodeUtility.TryNormalizeSubdivisionCode("US-CA", new char[4], false),
            Is.False);
    }

    [Test]
    public void OrdinalComparisonUsesCodeUnitsAndOptionalAsciiCaseFolding()
    {
        Assert.That(AsciiCodeUtility.EqualsOrdinal("US", "US"), Is.True);
        Assert.That(AsciiCodeUtility.EqualsOrdinal("US", "us"), Is.False);
        Assert.That(AsciiCodeUtility.EqualsOrdinalIgnoreAsciiCase("US", "us"), Is.True);
        Assert.That(AsciiCodeUtility.EqualsOrdinalIgnoreAsciiCase("É", "é"), Is.False);
        Assert.That(AsciiCodeUtility.CompareOrdinal("AA", "AB"), Is.LessThan(0));
        Assert.That(AsciiCodeUtility.CompareOrdinal("AB", "AA"), Is.GreaterThan(0));
        Assert.That(AsciiCodeUtility.CompareOrdinal("AA", "AA"), Is.Zero);
        Assert.That(AsciiCodeUtility.CompareOrdinal("AA", "AAA"), Is.LessThan(0));
        Assert.That(AsciiCodeUtility.CompareOrdinalIgnoreAsciiCase("gb", "GB"), Is.Zero);
        Assert.That(AsciiCodeUtility.CompareOrdinalIgnoreAsciiCase("gb", "US"), Is.LessThan(0));
    }
}
