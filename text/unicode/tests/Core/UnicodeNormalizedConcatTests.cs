using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeNormalizedConcatTests
{
    [Test]
    public void Append_NormalizesAcrossCanonicalCompositionBoundary()
    {
        Assert.That(new UnicodeNormalization().Append("U\u0308", "\u0300", UnicodeNormalizationForm.Nfc), Is.EqualTo("\u01DB"));
        Assert.That(new UnicodeNormalization().Append("\u1100", "\u1161\u11A8", UnicodeNormalizationForm.Nfc), Is.EqualTo("\uAC01"));
    }

    [Test]
    public void Append_NormalizesAcrossCanonicalOrderingBoundary()
    {
        Assert.That(new UnicodeNormalization().Append("U\u0308", "\u0327", UnicodeNormalizationForm.Nfd), Is.EqualTo("U\u0327\u0308"));
    }

    [Test]
    public void Append_UsesCompatibilityForms()
    {
        Assert.That(new UnicodeNormalization().Append("\u00B2", "\u00BD", UnicodeNormalizationForm.Nfkd), Is.EqualTo("2\u0031\u2044\u0032"));
        Assert.That(new UnicodeNormalization().Append("\u00B2", "U\u0308\u0300", UnicodeNormalizationForm.Nfkc), Is.EqualTo("2\u01DB"));
    }

    [Test]
    public void Concat_NormalizesAllParts()
    {
        Assert.That(
            new UnicodeNormalization().Concat(new string[] { "\u00B2", "U\u0308", "\u0300", "\u1100\u1161\u11A8" }, UnicodeNormalizationForm.Nfkc),
            Is.EqualTo("2\u01DB\uAC01"));
        Assert.That(
            new UnicodeNormalization().Concat(new string[] { "U\u0308", "\u0327" }, UnicodeNormalizationForm.Nfd),
            Is.EqualTo("U\u0327\u0308"));
    }

    [Test]
    public void Concat_HandlesEmptyParts()
    {
        Assert.That(new UnicodeNormalization().Concat(new string[] { }, UnicodeNormalizationForm.Nfc), Is.EqualTo(""));
        Assert.That(new UnicodeNormalization().Concat(new string[] { "A", "", "B" }, UnicodeNormalizationForm.Nfc), Is.EqualTo("AB"));
    }

    [Test]
    public void AppendAndConcat_RejectNullInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new UnicodeNormalization().Append(null!, "A", UnicodeNormalizationForm.Nfc));
        Assert.Throws<ArgumentNullException>(() => new UnicodeNormalization().Append("A", null!, UnicodeNormalizationForm.Nfc));
        Assert.Throws<ArgumentNullException>(() => new UnicodeNormalization().Concat(null!, UnicodeNormalizationForm.Nfc));
        Assert.Throws<ArgumentNullException>(() => new UnicodeNormalization().Concat(new string[] { "A", null! }, UnicodeNormalizationForm.Nfc));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodeNormalization().Append("A", "B", (UnicodeNormalizationForm)99));
    }
}
