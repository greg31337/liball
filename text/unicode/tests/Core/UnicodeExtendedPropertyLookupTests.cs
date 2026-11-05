using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeExtendedPropertyLookupTests
{
    [Test]
    public void DecompositionProperties_ReturnDirectCanonicalCompatibilityAndHangulMappings()
    {
        UnicodeProperties properties = new UnicodeProperties();

        Assert.That(properties.GetDecompositionType(0x0041), Is.EqualTo(UnicodeDecompositionType.None));
        Assert.That(properties.GetDecompositionMapping(0x0041), Is.Empty);
        Assert.That(properties.GetDecompositionType(0x00C0), Is.EqualTo(UnicodeDecompositionType.Canonical));
        Assert.That(properties.GetDecompositionMapping(0x00C0), Is.EqualTo(new int[] { 0x0041, 0x0300 }));
        Assert.That(properties.GetDecompositionType(0x00A0), Is.EqualTo(UnicodeDecompositionType.NoBreak));
        Assert.That(properties.GetDecompositionMapping(0x00A0), Is.EqualTo(new int[] { 0x0020 }));
        Assert.That(properties.GetDecompositionType(0xFB01), Is.EqualTo(UnicodeDecompositionType.Compat));
        Assert.That(properties.GetDecompositionMapping(0xFB01), Is.EqualTo(new int[] { 0x0066, 0x0069 }));
        Assert.That(properties.GetDecompositionType(0xAC00), Is.EqualTo(UnicodeDecompositionType.Canonical));
        Assert.That(properties.GetDecompositionMapping(0xAC00), Is.EqualTo(new int[] { 0x1100, 0x1161 }));
        Assert.That(properties.GetDecompositionMapping(0xAC01), Is.EqualTo(new int[] { 0x1100, 0x1161, 0x11A8 }));

        int[] first = properties.GetDecompositionMapping(0x00C0);
        first[0] = 0;
        Assert.That(properties.GetDecompositionMapping(0x00C0), Is.EqualTo(new int[] { 0x0041, 0x0300 }));
    }

    [Test]
    public void NumericProperties_ReturnTypeAndExactRationalValue()
    {
        UnicodeProperties properties = new UnicodeProperties();

        AssertNumeric(properties, 0x0039, UnicodeNumericType.Decimal, 9, 1);
        AssertNumeric(properties, 0x00B2, UnicodeNumericType.Digit, 2, 1);
        AssertNumeric(properties, 0x00BD, UnicodeNumericType.Numeric, 1, 2);
        AssertNumeric(properties, 0x0F33, UnicodeNumericType.Numeric, -1, 2);

        UnicodeRationalValue missing = properties.GetNumericValueExact(0x0041);
        Assert.That(properties.GetNumericType(0x0041), Is.EqualTo(UnicodeNumericType.None));
        Assert.That(missing.HasValue, Is.False);
        Assert.That(missing.Numerator, Is.Zero);
        Assert.That(missing.Denominator, Is.EqualTo(1));
    }

    [Test]
    public void BidiProperties_ReturnMirroringAndPairedBracketData()
    {
        UnicodeProperties properties = new UnicodeProperties();

        Assert.That(properties.IsBidiMirrored(0x0028), Is.True);
        Assert.That(properties.GetBidiMirror(0x0028), Is.EqualTo(0x0029));
        Assert.That(properties.GetBidiPairedBracket(0x0028), Is.EqualTo(0x0029));
        Assert.That(properties.GetBidiPairedBracketType(0x0028), Is.EqualTo(UnicodeBidiPairedBracketType.Open));
        Assert.That(properties.GetBidiPairedBracketType(0x0029), Is.EqualTo(UnicodeBidiPairedBracketType.Close));

        Assert.That(properties.IsBidiMirrored(0x2211), Is.True);
        Assert.That(properties.GetBidiMirror(0x2211), Is.EqualTo(0x2211));
        Assert.That(properties.GetBidiPairedBracketType(0x2211), Is.EqualTo(UnicodeBidiPairedBracketType.None));

        Assert.That(properties.IsBidiMirrored(0x0041), Is.False);
        Assert.That(properties.GetBidiMirror(0x0041), Is.EqualTo(0x0041));
        Assert.That(properties.GetBidiPairedBracket(0x0041), Is.EqualTo(0x0041));
    }

    [Test]
    public void JoiningProperties_ReturnExplicitAndDerivedDefaults()
    {
        UnicodeProperties properties = new UnicodeProperties();

        Assert.That(properties.GetJoiningType(0x0600), Is.EqualTo(UnicodeJoiningType.NonJoining));
        Assert.That(properties.GetJoiningType(0x0627), Is.EqualTo(UnicodeJoiningType.RightJoining));
        Assert.That(properties.GetJoiningGroup(0x0627), Is.EqualTo(UnicodeJoiningGroup.Alef));
        Assert.That(properties.GetJoiningType(0x0640), Is.EqualTo(UnicodeJoiningType.JoinCausing));
        Assert.That(properties.GetJoiningType(0x064B), Is.EqualTo(UnicodeJoiningType.Transparent));
        Assert.That(properties.GetJoiningType(0x0710), Is.EqualTo(UnicodeJoiningType.RightJoining));
        Assert.That(properties.GetJoiningGroup(0x0710), Is.EqualTo(UnicodeJoiningGroup.Alaph));
        Assert.That(properties.GetJoiningType(0xA872), Is.EqualTo(UnicodeJoiningType.LeftJoining));
        Assert.That(properties.GetJoiningType(0x10AC0), Is.EqualTo(UnicodeJoiningType.DualJoining));
        Assert.That(properties.GetJoiningGroup(0x10AC0), Is.EqualTo(UnicodeJoiningGroup.ManichaeanAleph));
        Assert.That(properties.GetJoiningGroup(0x0041), Is.EqualTo(UnicodeJoiningGroup.NoJoiningGroup));
    }

    [Test]
    public void AgeProperty_ReturnsAssignmentVersionOrUnassigned()
    {
        UnicodeProperties properties = new UnicodeProperties();

        Assert.That(properties.GetAge(0x0041), Is.EqualTo(UnicodeAge.Version1_1));
        Assert.That(properties.GetAge(0x20AC), Is.EqualTo(UnicodeAge.Version2_1));
        Assert.That(properties.GetAge(0x1F600), Is.EqualTo(UnicodeAge.Version6_1));
        Assert.That(properties.GetAge(0x088F), Is.EqualTo(UnicodeAge.Version17_0));
        Assert.That(properties.GetAge(0x0378), Is.EqualTo(UnicodeAge.Unassigned));
    }

    [Test]
    public void IndicAndHangulProperties_ReturnEveryPropertyValue()
    {
        UnicodeProperties properties = new UnicodeProperties();

        Assert.That(properties.GetIndicConjunctBreak(0x0041), Is.EqualTo(UnicodeIndicConjunctBreak.None));
        Assert.That(properties.GetIndicConjunctBreak(0x094D), Is.EqualTo(UnicodeIndicConjunctBreak.Linker));
        Assert.That(properties.GetIndicConjunctBreak(0x0915), Is.EqualTo(UnicodeIndicConjunctBreak.Consonant));
        Assert.That(properties.GetIndicConjunctBreak(0x093C), Is.EqualTo(UnicodeIndicConjunctBreak.Extend));

        Assert.That(properties.GetHangulSyllableType(0x0041), Is.EqualTo(UnicodeHangulSyllableType.NotApplicable));
        Assert.That(properties.GetHangulSyllableType(0x1100), Is.EqualTo(UnicodeHangulSyllableType.L));
        Assert.That(properties.GetHangulSyllableType(0x1161), Is.EqualTo(UnicodeHangulSyllableType.V));
        Assert.That(properties.GetHangulSyllableType(0x11A8), Is.EqualTo(UnicodeHangulSyllableType.T));
        Assert.That(properties.GetHangulSyllableType(0xAC00), Is.EqualTo(UnicodeHangulSyllableType.Lv));
        Assert.That(properties.GetHangulSyllableType(0xAC01), Is.EqualTo(UnicodeHangulSyllableType.Lvt));
    }

    [Test]
    public void BreakProperties_AreAvailableThroughThePropertyContract()
    {
        IUnicodeProperties properties = new UnicodeProperties();

        Assert.That(properties.GetGraphemeBreakClass(0x0300), Is.EqualTo(UnicodeGraphemeBreakClass.Extend));
        Assert.That(properties.GetWordBreakClass(0x0020), Is.EqualTo(UnicodeWordBreakClass.WSegSpace));
        Assert.That(properties.GetSentenceBreakClass(0x0021), Is.EqualTo(UnicodeSentenceBreakClass.STerm));
        Assert.That(properties.GetLineBreakClass(0x0021), Is.EqualTo(UnicodeLineBreakClass.Exclamation));
        Assert.That(properties.GetLineBreakClass(0x0029), Is.EqualTo(UnicodeLineBreakClass.ClosingParenthesis));
        Assert.That(properties.GetLineBreakClass(0x05D0), Is.EqualTo(UnicodeLineBreakClass.HebrewLetter));
        Assert.That(properties.GetLineBreakClass(0x1100), Is.EqualTo(UnicodeLineBreakClass.HangulLeadingJamo));
        Assert.That(properties.GetLineBreakClass(0x2060), Is.EqualTo(UnicodeLineBreakClass.WordJoiner));
        Assert.That(properties.GetLineBreakClass(0xAC00), Is.EqualTo(UnicodeLineBreakClass.HangulLvSyllable));
        Assert.That(properties.GetLineBreakClass(0xAC01), Is.EqualTo(UnicodeLineBreakClass.HangulLvtSyllable));
        Assert.That(properties.GetLineBreakClass(0x1F1E6), Is.EqualTo(UnicodeLineBreakClass.RegionalIndicator));
        Assert.That(properties.GetLineBreakClass(0x1F3FB), Is.EqualTo(UnicodeLineBreakClass.EmojiModifier));
    }

    [Test]
    public void LineBreakNameMapping_CoversEveryUnicode17PropertyValue()
    {
        string[] names = new string[]
        {
            "XX", "AI", "AK", "AL", "AP", "AS", "B2", "BA", "BB", "BK",
            "CB", "CJ", "CL", "CM", "CP", "CR", "EB", "EM", "EX", "GL",
            "H2", "H3", "HH", "HL", "HY", "ID", "IN", "IS", "JL", "JT",
            "JV", "LF", "NL", "NS", "NU", "OP", "PO", "PR", "QU", "RI",
            "SA", "SG", "SP", "SY", "VF", "VI", "WJ", "ZW", "ZWJ"
        };
        UnicodeLineBreakClass[] expected = new UnicodeLineBreakClass[]
        {
            UnicodeLineBreakClass.Unknown,
            UnicodeLineBreakClass.Ambiguous,
            UnicodeLineBreakClass.Aksara,
            UnicodeLineBreakClass.Alphabetic,
            UnicodeLineBreakClass.AksaraPrebase,
            UnicodeLineBreakClass.AksaraStart,
            UnicodeLineBreakClass.BreakBoth,
            UnicodeLineBreakClass.BreakAfter,
            UnicodeLineBreakClass.BreakBefore,
            UnicodeLineBreakClass.MandatoryBreak,
            UnicodeLineBreakClass.ContingentBreak,
            UnicodeLineBreakClass.ConditionalJapaneseStarter,
            UnicodeLineBreakClass.ClosingPunctuation,
            UnicodeLineBreakClass.CombiningMark,
            UnicodeLineBreakClass.ClosingParenthesis,
            UnicodeLineBreakClass.CarriageReturn,
            UnicodeLineBreakClass.EmojiBase,
            UnicodeLineBreakClass.EmojiModifier,
            UnicodeLineBreakClass.Exclamation,
            UnicodeLineBreakClass.Glue,
            UnicodeLineBreakClass.HangulLvSyllable,
            UnicodeLineBreakClass.HangulLvtSyllable,
            UnicodeLineBreakClass.UnambiguousHyphen,
            UnicodeLineBreakClass.HebrewLetter,
            UnicodeLineBreakClass.Hyphen,
            UnicodeLineBreakClass.Ideographic,
            UnicodeLineBreakClass.Inseparable,
            UnicodeLineBreakClass.InfixNumeric,
            UnicodeLineBreakClass.HangulLeadingJamo,
            UnicodeLineBreakClass.HangulTrailingJamo,
            UnicodeLineBreakClass.HangulVowelJamo,
            UnicodeLineBreakClass.LineFeed,
            UnicodeLineBreakClass.NextLine,
            UnicodeLineBreakClass.Nonstarter,
            UnicodeLineBreakClass.Numeric,
            UnicodeLineBreakClass.OpeningPunctuation,
            UnicodeLineBreakClass.PostfixNumeric,
            UnicodeLineBreakClass.PrefixNumeric,
            UnicodeLineBreakClass.Quotation,
            UnicodeLineBreakClass.RegionalIndicator,
            UnicodeLineBreakClass.ComplexContext,
            UnicodeLineBreakClass.Surrogate,
            UnicodeLineBreakClass.Space,
            UnicodeLineBreakClass.BreakSymbols,
            UnicodeLineBreakClass.ViramaFinal,
            UnicodeLineBreakClass.Virama,
            UnicodeLineBreakClass.WordJoiner,
            UnicodeLineBreakClass.ZeroWidthSpace,
            UnicodeLineBreakClass.ZeroWidthJoiner
        };

        Assert.That(names, Has.Length.EqualTo(expected.Length));
        for (int index = 0; index < names.Length; index++)
        {
            Assert.That(
                UnicodeSegments.LineBreakClassFromName(names[index]),
                Is.EqualTo(expected[index]),
                names[index]);
        }
    }

    [Test]
    public void BinaryProperties_CoverPlannedUnicodeAndEmojiSets()
    {
        UnicodeProperties properties = new UnicodeProperties();

        Assert.That(properties.HasBinaryProperty(0x1F3FB, UnicodeBinaryProperty.EmojiComponent), Is.True);
        Assert.That(properties.HasBinaryProperty(0x1F3FB, UnicodeBinaryProperty.EmojiModifier), Is.True);
        Assert.That(properties.HasBinaryProperty(0x261D, UnicodeBinaryProperty.EmojiModifierBase), Is.True);
        Assert.That(properties.HasBinaryProperty(0xFE0F, UnicodeBinaryProperty.VariationSelector), Is.True);
        Assert.That(properties.HasBinaryProperty(0xFDD0, UnicodeBinaryProperty.NoncharacterCodePoint), Is.True);
        Assert.That(properties.HasBinaryProperty(0x0020, UnicodeBinaryProperty.PatternWhiteSpace), Is.True);
        Assert.That(properties.HasBinaryProperty(0x0021, UnicodeBinaryProperty.PatternSyntax), Is.True);
        Assert.That(properties.HasBinaryProperty(0xE000, UnicodeBinaryProperty.PrivateUse), Is.True);
        Assert.That(properties.HasBinaryProperty(0x0000, UnicodeBinaryProperty.Control), Is.True);
        Assert.That(properties.HasBinaryProperty(0x0041, UnicodeBinaryProperty.EmojiModifier), Is.False);
        Assert.That(properties.HasBinaryProperty(0x0041, UnicodeBinaryProperty.VariationSelector), Is.False);
        Assert.That(properties.HasBinaryProperty(0x0041, UnicodeBinaryProperty.PrivateUse), Is.False);
        Assert.That(properties.HasBinaryProperty(0x0041, UnicodeBinaryProperty.Control), Is.False);
    }

    static void AssertNumeric(
        UnicodeProperties properties,
        int codePoint,
        UnicodeNumericType numericType,
        long numerator,
        long denominator)
    {
        UnicodeRationalValue value = properties.GetNumericValueExact(codePoint);
        Assert.That(properties.GetNumericType(codePoint), Is.EqualTo(numericType));
        Assert.That(value.HasValue, Is.True);
        Assert.That(value.Numerator, Is.EqualTo(numerator));
        Assert.That(value.Denominator, Is.EqualTo(denominator));
    }
}
