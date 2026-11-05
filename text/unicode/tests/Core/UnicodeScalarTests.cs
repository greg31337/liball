using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeScalarTests
{
    [TestCase(0x000000)]
    [TestCase(0x000041)]
    [TestCase(0x00D7FF)]
    [TestCase(0x00E000)]
    [TestCase(0x00FFFD)]
    [TestCase(0x010000)]
    [TestCase(0x10FFFF)]
    public void IsValid_ReturnsTrueForUnicodeScalarValues(int codePoint)
    {
        Assert.That(new UnicodeScalar().IsValid(codePoint), Is.True);
        Assert.That(new UnicodeScalar().IsScalarValue(codePoint), Is.True);
    }

    [TestCase(-1)]
    [TestCase(0x00D800)]
    [TestCase(0x00DBFF)]
    [TestCase(0x00DC00)]
    [TestCase(0x00DFFF)]
    [TestCase(0x110000)]
    public void IsValid_ReturnsFalseForInvalidCodePoints(int codePoint)
    {
        Assert.That(new UnicodeScalar().IsValid(codePoint), Is.False);
        Assert.That(new UnicodeScalar().IsScalarValue(codePoint), Is.False);
    }

    [TestCase(0x000000)]
    [TestCase(0x00D7FF)]
    [TestCase(0x00E000)]
    [TestCase(0x00FFFF)]
    public void IsBmpScalar_ReturnsTrueForValidNonSurrogateBmpScalars(int codePoint)
    {
        Assert.That(new UnicodeScalar().IsBmpScalar(codePoint), Is.True);
        Assert.That(new UnicodeScalar().IsSupplementaryScalar(codePoint), Is.False);
    }

    [TestCase(0x010000)]
    [TestCase(0x10FFFF)]
    public void IsSupplementaryScalar_ReturnsTrueForSupplementaryScalars(int codePoint)
    {
        Assert.That(new UnicodeScalar().IsSupplementaryScalar(codePoint), Is.True);
        Assert.That(new UnicodeScalar().IsBmpScalar(codePoint), Is.False);
    }

    [TestCase(-1, false)]
    [TestCase(0x000000, true)]
    [TestCase(0x00FFFF, true)]
    [TestCase(0x010000, false)]
    public void IsValidCodeUnit_ValidatesUtf16CodeUnitRange(int codeUnit, bool expected)
    {
        Assert.That(new UnicodeScalar().IsValidCodeUnit(codeUnit), Is.EqualTo(expected));
    }

    [Test]
    public void SurrogateHelpers_ClassifyUtf16SurrogateCodeUnits()
    {
        Assert.That(new UnicodeScalar().IsSurrogate(0xD7FF), Is.False);
        Assert.That(new UnicodeScalar().IsSurrogate(0xD800), Is.True);
        Assert.That(new UnicodeScalar().IsSurrogate(0xDFFF), Is.True);
        Assert.That(new UnicodeScalar().IsSurrogate(0xE000), Is.False);

        Assert.That(new UnicodeScalar().IsHighSurrogate(0xD800), Is.True);
        Assert.That(new UnicodeScalar().IsHighSurrogate(0xDBFF), Is.True);
        Assert.That(new UnicodeScalar().IsHighSurrogate(0xDC00), Is.False);

        Assert.That(new UnicodeScalar().IsLowSurrogate(0xDBFF), Is.False);
        Assert.That(new UnicodeScalar().IsLowSurrogate(0xDC00), Is.True);
        Assert.That(new UnicodeScalar().IsLowSurrogate(0xDFFF), Is.True);
    }

    [Test]
    public void IsValidSurrogatePair_RequiresHighThenLowSurrogate()
    {
        Assert.That(new UnicodeScalar().IsValidSurrogatePair(0xD800, 0xDC00), Is.True);
        Assert.That(new UnicodeScalar().IsValidSurrogatePair(0xDBFF, 0xDFFF), Is.True);
        Assert.That(new UnicodeScalar().IsValidSurrogatePair(0xDC00, 0xD800), Is.False);
        Assert.That(new UnicodeScalar().IsValidSurrogatePair(0xD800, 0x0041), Is.False);
        Assert.That(new UnicodeScalar().IsValidSurrogatePair(0x0041, 0xDC00), Is.False);
    }

    [Test]
    public void ToUtf16String_ConvertsBmpAndSupplementaryScalars()
    {
        Assert.That(new UnicodeScalar().ToUtf16String(0x0041), Is.EqualTo("A"));
        Assert.That(new UnicodeScalar().ToUtf16String(0x20AC), Is.EqualTo("\u20AC"));
        Assert.That(new UnicodeScalar().ToUtf16String(0x10000), Is.EqualTo(char.ConvertFromUtf32(0x10000)));
        Assert.That(new UnicodeScalar().ToUtf16String(0x1F600), Is.EqualTo(char.ConvertFromUtf32(0x1F600)));
    }

    [TestCase(-1)]
    [TestCase(0xD800)]
    [TestCase(0xDFFF)]
    [TestCase(0x110000)]
    public void ToUtf16String_ThrowsForInvalidScalars(int codePoint)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodeScalar().ToUtf16String(codePoint));
    }
}
