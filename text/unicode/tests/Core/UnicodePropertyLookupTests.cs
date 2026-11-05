using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePropertyLookupTests
{
    [TestCase(0x0020, UnicodeGeneralCategory.SpaceSeparator)]
    [TestCase(0x0030, UnicodeGeneralCategory.DecimalNumber)]
    [TestCase(0x0039, UnicodeGeneralCategory.DecimalNumber)]
    [TestCase(0x0041, UnicodeGeneralCategory.UppercaseLetter)]
    [TestCase(0x005A, UnicodeGeneralCategory.UppercaseLetter)]
    [TestCase(0x0061, UnicodeGeneralCategory.LowercaseLetter)]
    [TestCase(0x007A, UnicodeGeneralCategory.LowercaseLetter)]
    [TestCase(0x0301, UnicodeGeneralCategory.NonspacingMark)]
    [TestCase(0x1F600, UnicodeGeneralCategory.OtherSymbol)]
    [TestCase(0x10FFFF, UnicodeGeneralCategory.OtherNotAssigned)]
    public void GetGeneralCategory_MatchesRepresentativeFixture(int codePoint, UnicodeGeneralCategory expected)
    {
        Assert.That(new UnicodeProperties().GetGeneralCategory(codePoint), Is.EqualTo(expected));
    }

    [Test]
    public void GetGeneralCategory_ReturnsRepresentativeCategories()
    {
        Assert.That(new UnicodeProperties().GetGeneralCategory(0x0041), Is.EqualTo(UnicodeGeneralCategory.UppercaseLetter));
        Assert.That(new UnicodeProperties().GetGeneralCategory(0x0061), Is.EqualTo(UnicodeGeneralCategory.LowercaseLetter));
        Assert.That(new UnicodeProperties().GetGeneralCategory(0x0030), Is.EqualTo(UnicodeGeneralCategory.DecimalNumber));
        Assert.That(new UnicodeProperties().GetGeneralCategory(0x0020), Is.EqualTo(UnicodeGeneralCategory.SpaceSeparator));
        Assert.That(new UnicodeProperties().GetGeneralCategory(0x0301), Is.EqualTo(UnicodeGeneralCategory.NonspacingMark));
        Assert.That(new UnicodeProperties().GetGeneralCategory(0x1F600), Is.EqualTo(UnicodeGeneralCategory.OtherSymbol));
        Assert.That(new UnicodeProperties().GetGeneralCategory(0x10FFFF), Is.EqualTo(UnicodeGeneralCategory.OtherNotAssigned));
    }

    [Test]
    public void GetCombiningClass_ReturnsCanonicalCombiningClass()
    {
        Assert.That(new UnicodeProperties().GetCombiningClass(0x0041), Is.EqualTo(0));
        Assert.That(new UnicodeProperties().GetCombiningClass(0x0300), Is.EqualTo(230));
        Assert.That(new UnicodeProperties().GetCombiningClass(0x0301), Is.EqualTo(230));
        Assert.That(new UnicodeProperties().GetCombiningClass(0x0308), Is.EqualTo(230));
        Assert.That(new UnicodeProperties().GetCombiningClass(0x0327), Is.EqualTo(202));
        Assert.That(new UnicodeProperties().GetCombiningClass(0x0315), Is.EqualTo(232));
        Assert.That(new UnicodeProperties().GetCombiningClass(0x093C), Is.EqualTo(7));
    }

    [TestCase(0x0009, UnicodeBidiClass.SegmentSeparator)]
    [TestCase(0x000D, UnicodeBidiClass.ParagraphSeparator)]
    [TestCase(0x0020, UnicodeBidiClass.WhiteSpace)]
    [TestCase(0x0028, UnicodeBidiClass.OtherNeutral)]
    [TestCase(0x0029, UnicodeBidiClass.OtherNeutral)]
    [TestCase(0x0030, UnicodeBidiClass.EuropeanNumber)]
    [TestCase(0x0039, UnicodeBidiClass.EuropeanNumber)]
    [TestCase(0x0041, UnicodeBidiClass.LeftToRight)]
    [TestCase(0x007A, UnicodeBidiClass.LeftToRight)]
    [TestCase(0x0301, UnicodeBidiClass.NonspacingMark)]
    [TestCase(0x0600, UnicodeBidiClass.ArabicNumber)]
    [TestCase(0x06FF, UnicodeBidiClass.ArabicLetter)]
    [TestCase(0x0590, UnicodeBidiClass.RightToLeft)]
    [TestCase(0x10FFFF, UnicodeBidiClass.BoundaryNeutral)]
    public void GetBidiClass_MatchesRepresentativeFixture(int codePoint, UnicodeBidiClass expected)
    {
        Assert.That(new UnicodeProperties().GetBidiClass(codePoint), Is.EqualTo(expected));
    }

    [Test]
    public void GetBidiClass_ReturnsRepresentativeBidiClasses()
    {
        Assert.That(new UnicodeProperties().GetBidiClass(0x0041), Is.EqualTo(UnicodeBidiClass.LeftToRight));
        Assert.That(new UnicodeProperties().GetBidiClass(0x0030), Is.EqualTo(UnicodeBidiClass.EuropeanNumber));
        Assert.That(new UnicodeProperties().GetBidiClass(0x0020), Is.EqualTo(UnicodeBidiClass.WhiteSpace));
        Assert.That(new UnicodeProperties().GetBidiClass(0x0301), Is.EqualTo(UnicodeBidiClass.NonspacingMark));
        Assert.That(new UnicodeProperties().GetBidiClass(0x0628), Is.EqualTo(UnicodeBidiClass.ArabicLetter));
    }

    [TestCase(0x0030, UnicodeScript.Common)]
    [TestCase(0x0039, UnicodeScript.Common)]
    [TestCase(0x0041, UnicodeScript.Latin)]
    [TestCase(0x005A, UnicodeScript.Latin)]
    [TestCase(0x0061, UnicodeScript.Latin)]
    [TestCase(0x007A, UnicodeScript.Latin)]
    [TestCase(0x0370, UnicodeScript.Greek)]
    [TestCase(0x03FF, UnicodeScript.Greek)]
    [TestCase(0x0600, UnicodeScript.Arabic)]
    [TestCase(0x06FF, UnicodeScript.Arabic)]
    [TestCase(0x4E00, UnicodeScript.Han)]
    [TestCase(0x9FFF, UnicodeScript.Han)]
    [TestCase(0xAC00, UnicodeScript.Hangul)]
    [TestCase(0xD7A3, UnicodeScript.Hangul)]
    [TestCase(0x10FFFF, UnicodeScript.Unknown)]
    public void GetScript_MatchesRepresentativeFixture(int codePoint, UnicodeScript expected)
    {
        Assert.That(new UnicodeProperties().GetScript(codePoint), Is.EqualTo(expected));
    }

    [Test]
    public void GetScriptAndScriptExtensions_ReturnRepresentativeScripts()
    {
        Assert.That(new UnicodeProperties().GetScript(0x0041), Is.EqualTo(UnicodeScript.Latin));
        Assert.That(new UnicodeProperties().GetScript(0x0391), Is.EqualTo(UnicodeScript.Greek));
        Assert.That(new UnicodeProperties().GetScript(0x0628), Is.EqualTo(UnicodeScript.Arabic));
        Assert.That(new UnicodeProperties().GetScript(0x4E00), Is.EqualTo(UnicodeScript.Han));
        Assert.That(new UnicodeProperties().GetScript(0xAC00), Is.EqualTo(UnicodeScript.Hangul));
        Assert.That(new UnicodeProperties().GetScript(0xE0000), Is.EqualTo(UnicodeScript.Unknown));
        Assert.That(new UnicodeProperties().GetScriptExtensions(0x0041), Is.EqualTo(new UnicodeScript[] { UnicodeScript.Latin }));
    }

    [Test]
    public void GetScriptExtensions_ReturnsProductionScriptExtensionSet()
    {
        Assert.That(
            new UnicodeProperties().GetScriptExtensions(0x30FC),
            Is.EqualTo(new UnicodeScript[] { UnicodeScript.Hiragana, UnicodeScript.Katakana }));
    }

    [TestCase(0x0000, UnicodeBlock.BasicLatin)]
    [TestCase(0x007F, UnicodeBlock.BasicLatin)]
    [TestCase(0x0080, UnicodeBlock.Latin1Supplement)]
    [TestCase(0x00FF, UnicodeBlock.Latin1Supplement)]
    [TestCase(0x0300, UnicodeBlock.CombiningDiacriticalMarks)]
    [TestCase(0x036F, UnicodeBlock.CombiningDiacriticalMarks)]
    [TestCase(0x0370, UnicodeBlock.GreekAndCoptic)]
    [TestCase(0x03FF, UnicodeBlock.GreekAndCoptic)]
    [TestCase(0x0400, UnicodeBlock.Cyrillic)]
    [TestCase(0x04FF, UnicodeBlock.Cyrillic)]
    [TestCase(0x0590, UnicodeBlock.Hebrew)]
    [TestCase(0x05FF, UnicodeBlock.Hebrew)]
    [TestCase(0x0600, UnicodeBlock.Arabic)]
    [TestCase(0x06FF, UnicodeBlock.Arabic)]
    [TestCase(0x4E00, UnicodeBlock.CjkUnifiedIdeographs)]
    [TestCase(0x9FFF, UnicodeBlock.CjkUnifiedIdeographs)]
    [TestCase(0xAC00, UnicodeBlock.HangulSyllables)]
    [TestCase(0xD7A3, UnicodeBlock.HangulSyllables)]
    [TestCase(0xE000, UnicodeBlock.PrivateUseArea)]
    [TestCase(0xF8FF, UnicodeBlock.PrivateUseArea)]
    [TestCase(0xFFF0, UnicodeBlock.Specials)]
    [TestCase(0xFFFF, UnicodeBlock.Specials)]
    [TestCase(0x10000, UnicodeBlock.NoBlock)]
    public void GetBlock_MatchesRepresentativeFixture(int codePoint, UnicodeBlock expected)
    {
        Assert.That(new UnicodeProperties().GetBlock(codePoint), Is.EqualTo(expected));
    }

    [Test]
    public void GetBlock_ReturnsRepresentativeBlocks()
    {
        Assert.That(new UnicodeProperties().GetBlock(0x0041), Is.EqualTo(UnicodeBlock.BasicLatin));
        Assert.That(new UnicodeProperties().GetBlock(0x00A0), Is.EqualTo(UnicodeBlock.Latin1Supplement));
        Assert.That(new UnicodeProperties().GetBlock(0x0301), Is.EqualTo(UnicodeBlock.CombiningDiacriticalMarks));
        Assert.That(new UnicodeProperties().GetBlock(0x0391), Is.EqualTo(UnicodeBlock.GreekAndCoptic));
        Assert.That(new UnicodeProperties().GetBlock(0x4E00), Is.EqualTo(UnicodeBlock.CjkUnifiedIdeographs));
        Assert.That(new UnicodeProperties().GetBlock(0xE0000), Is.EqualTo(UnicodeBlock.NoBlock));
    }

    [TestCase(0x0041, UnicodeEastAsianWidth.Narrow)]
    [TestCase(0x005A, UnicodeEastAsianWidth.Narrow)]
    [TestCase(0x0061, UnicodeEastAsianWidth.Narrow)]
    [TestCase(0x007A, UnicodeEastAsianWidth.Narrow)]
    [TestCase(0x0370, UnicodeEastAsianWidth.Neutral)]
    [TestCase(0x03FF, UnicodeEastAsianWidth.Neutral)]
    [TestCase(0x4E00, UnicodeEastAsianWidth.Wide)]
    [TestCase(0x9FFF, UnicodeEastAsianWidth.Wide)]
    [TestCase(0x1F600, UnicodeEastAsianWidth.Wide)]
    [TestCase(0x10FFFF, UnicodeEastAsianWidth.Neutral)]
    public void GetEastAsianWidth_MatchesRepresentativeFixture(int codePoint, UnicodeEastAsianWidth expected)
    {
        Assert.That(new UnicodeProperties().GetEastAsianWidth(codePoint), Is.EqualTo(expected));
    }

    [Test]
    public void GetEastAsianWidth_ReturnsRepresentativeWidths()
    {
        Assert.That(new UnicodeProperties().GetEastAsianWidth(0x0041), Is.EqualTo(UnicodeEastAsianWidth.Narrow));
        Assert.That(new UnicodeProperties().GetEastAsianWidth(0x0391), Is.EqualTo(UnicodeEastAsianWidth.Ambiguous));
        Assert.That(new UnicodeProperties().GetEastAsianWidth(0x4E00), Is.EqualTo(UnicodeEastAsianWidth.Wide));
        Assert.That(new UnicodeProperties().GetEastAsianWidth(0x1F600), Is.EqualTo(UnicodeEastAsianWidth.Wide));
        Assert.That(new UnicodeProperties().GetEastAsianWidth(0xE0000), Is.EqualTo(UnicodeEastAsianWidth.Neutral));
    }

    [Test]
    public void GetNumericValue_ReturnsNumericValuesOrNegativeOne()
    {
        Assert.That(new UnicodeProperties().GetNumericValue(0x0030), Is.EqualTo(0.0));
        Assert.That(new UnicodeProperties().GetNumericValue(0x0031), Is.EqualTo(1.0));
        Assert.That(new UnicodeProperties().GetNumericValue(0x00BD), Is.EqualTo(0.5));
        Assert.That(new UnicodeProperties().GetNumericValue(0x0041), Is.EqualTo(-1.0));
    }

    [TestCase(0x0009, UnicodeBinaryProperty.WhiteSpace, true)]
    [TestCase(0x000D, UnicodeBinaryProperty.WhiteSpace, true)]
    [TestCase(0x0020, UnicodeBinaryProperty.WhiteSpace, true)]
    [TestCase(0x0021, UnicodeBinaryProperty.WhiteSpace, false)]
    [TestCase(0x00AD, UnicodeBinaryProperty.DefaultIgnorableCodePoint, true)]
    [TestCase(0x0041, UnicodeBinaryProperty.Uppercase, true)]
    [TestCase(0x005A, UnicodeBinaryProperty.Uppercase, true)]
    [TestCase(0x0061, UnicodeBinaryProperty.Uppercase, false)]
    [TestCase(0x0061, UnicodeBinaryProperty.Lowercase, true)]
    [TestCase(0x007A, UnicodeBinaryProperty.Lowercase, true)]
    [TestCase(0x0041, UnicodeBinaryProperty.Alphabetic, true)]
    [TestCase(0x0061, UnicodeBinaryProperty.Alphabetic, true)]
    [TestCase(0x0030, UnicodeBinaryProperty.Alphabetic, false)]
    [TestCase(0x1F600, UnicodeBinaryProperty.Emoji, true)]
    [TestCase(0x1F600, UnicodeBinaryProperty.EmojiPresentation, true)]
    [TestCase(0x1F600, UnicodeBinaryProperty.ExtendedPictographic, true)]
    [TestCase(0x1F601, UnicodeBinaryProperty.Emoji, true)]
    public void HasBinaryProperty_MatchesRepresentativeFixture(int codePoint, UnicodeBinaryProperty property, bool expected)
    {
        Assert.That(new UnicodeProperties().HasBinaryProperty(codePoint, property), Is.EqualTo(expected));
    }

    [Test]
    public void HasBinaryProperty_ReturnsRepresentativeBinaryProperties()
    {
        Assert.That(new UnicodeProperties().HasBinaryProperty(0x0020, UnicodeBinaryProperty.WhiteSpace), Is.True);
        Assert.That(new UnicodeProperties().HasBinaryProperty(0x0041, UnicodeBinaryProperty.Uppercase), Is.True);
        Assert.That(new UnicodeProperties().HasBinaryProperty(0x0041, UnicodeBinaryProperty.Alphabetic), Is.True);
        Assert.That(new UnicodeProperties().HasBinaryProperty(0x0061, UnicodeBinaryProperty.Lowercase), Is.True);
        Assert.That(new UnicodeProperties().HasBinaryProperty(0x00AD, UnicodeBinaryProperty.DefaultIgnorableCodePoint), Is.True);
        Assert.That(new UnicodeProperties().HasBinaryProperty(0x1F600, UnicodeBinaryProperty.Emoji), Is.True);
        Assert.That(new UnicodeProperties().HasBinaryProperty(0x1F600, UnicodeBinaryProperty.ExtendedPictographic), Is.True);
        Assert.That(new UnicodeProperties().HasBinaryProperty(0x0041, UnicodeBinaryProperty.WhiteSpace), Is.False);
    }
}
