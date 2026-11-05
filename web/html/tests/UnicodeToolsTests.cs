
namespace UniversalHtml.Tests;

[TestFixture]
public class UnicodeToolsTests
{
    [Test]
    public void ScalarValidationAndReplacementAreExhaustive()
    {
        for (int value = -1; value <= 0x110000; value++)
        {
            bool expected = value >= 0 && value <= 0x10FFFF && !(value >= 0xD800 && value <= 0xDFFF);
            Assert.That(UnicodeTools.IsScalarValue(value), Is.EqualTo(expected), $"U+{value:X}");
            Assert.That(UnicodeTools.GetScalarOrReplacement(value), Is.EqualTo(expected ? value : 0xFFFD), $"replace U+{value:X}");
            Assert.That(UnicodeTools.GetHtmlScalarOrReplacement(value), Is.EqualTo(expected && value != 0 ? value : 0xFFFD), $"HTML replace U+{value:X}");
        }
    }

    [Test]
    public void SurrogateClassificationIsExhaustiveAcrossUtf16CodeUnits()
    {
        for (int code = 0; code <= char.MaxValue; code++)
        {
            char value = (char)code;
            Assert.That(UnicodeTools.IsHighSurrogate(value), Is.EqualTo(code >= 0xD800 && code <= 0xDBFF), $"high U+{code:X4}");
            Assert.That(UnicodeTools.IsLowSurrogate(value), Is.EqualTo(code >= 0xDC00 && code <= 0xDFFF), $"low U+{code:X4}");
            Assert.That(UnicodeTools.IsSurrogateCodePoint(code), Is.EqualTo(code >= 0xD800 && code <= 0xDFFF), $"code point U+{code:X4}");
        }
    }

    [TestCase(0x10000)]
    [TestCase(0x1F600)]
    [TestCase(0x10FFFF)]
    public void SupplementaryScalarsRoundTripThroughSurrogates(int scalar)
    {
        char high = UnicodeTools.GetHighSurrogate(scalar);
        char low = UnicodeTools.GetLowSurrogate(scalar);

        Assert.That(UnicodeTools.CombineSurrogates(high, low), Is.EqualTo(scalar));
        Assert.That(UnicodeTools.GetUtf16Length(scalar), Is.EqualTo(2));
        Assert.That(UnicodeTools.ScalarToString(scalar), Is.EqualTo(new string([high, low])));
    }

    [Test]
    public void SurrogateHelpersRejectInvalidInputs()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodeTools.CombineSurrogates('a', '\uDC00'));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodeTools.CombineSurrogates('\uD800', 'a'));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodeTools.GetHighSurrogate(0xFFFF));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodeTools.GetLowSurrogate(0x110000));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodeTools.GetUtf16Length(0xD800));
        Assert.That(UnicodeTools.ScalarToString(-1), Is.EqualTo("\uFFFD"));
    }

    [Test]
    public void NoncharacterAndControlClassificationAreExhaustive()
    {
        for (int value = 0; value <= 0x10FFFF; value++)
        {
            bool scalar = !(value >= 0xD800 && value <= 0xDFFF);
            bool noncharacter = scalar && (value >= 0xFDD0 && value <= 0xFDEF || value >= 0xFFFE && (value & 0xFFFF) >= 0xFFFE);
            bool control = value <= 0x1F || value >= 0x7F && value <= 0x9F;
            Assert.That(UnicodeTools.IsNoncharacter(value), Is.EqualTo(noncharacter), $"noncharacter U+{value:X}");
            Assert.That(UnicodeTools.IsControl(value), Is.EqualTo(control), $"control U+{value:X}");
        }
    }

    [Test]
    public void UnicodeWhitespaceClassificationIsExhaustiveAcrossScalarRange()
    {
        HashSet<int> expected =
        [
            0x0009, 0x000A, 0x000B, 0x000C, 0x000D, 0x0020, 0x0085, 0x00A0,
            0x1680, 0x2000, 0x2001, 0x2002, 0x2003, 0x2004, 0x2005, 0x2006,
            0x2007, 0x2008, 0x2009, 0x200A, 0x2028, 0x2029, 0x202F, 0x205F,
            0x3000
        ];

        for (int value = 0; value <= 0x10FFFF; value++)
        {
            Assert.That(UnicodeTools.IsUnicodeWhitespace(value), Is.EqualTo(expected.Contains(value)), $"U+{value:X}");
        }
    }
}
