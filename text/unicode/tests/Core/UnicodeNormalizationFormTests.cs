using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeNormalizationFormTests
{
    [Test]
    public void Normalize_Nfd_DecomposesAndOrdersCanonically()
    {
        Assert.That(
            new UnicodeNormalization().Normalize("\u01DB\u0327", UnicodeNormalizationForm.Nfd),
            Is.EqualTo("U\u0327\u0308\u0300"));
        Assert.That(
            new UnicodeNormalization().ToNfd("\uAC01"),
            Is.EqualTo("\u1100\u1161\u11A8"));
    }

    [Test]
    public void Normalize_Nfkd_DecomposesCompatibilityMappingsAndOrdersCanonically()
    {
        Assert.That(
            new UnicodeNormalization().Normalize("\u00B2\u0315\u0327", UnicodeNormalizationForm.Nfkd),
            Is.EqualTo("2\u0327\u0315"));
        Assert.That(
            new UnicodeNormalization().ToNfkd("\u00BD"),
            Is.EqualTo("\u0031\u2044\u0032"));
    }

    [Test]
    public void Normalize_Nfc_DecomposesOrdersAndComposesCanonically()
    {
        Assert.That(
            new UnicodeNormalization().Normalize("U\u0308\u0300", UnicodeNormalizationForm.Nfc),
            Is.EqualTo("\u01DB"));
        Assert.That(
            new UnicodeNormalization().ToNfc("\u1100\u1161\u11A8"),
            Is.EqualTo("\uAC01"));
    }

    [Test]
    public void Normalize_Nfkc_DecomposesCompatibilityMappingsThenComposesCanonically()
    {
        Assert.That(
            new UnicodeNormalization().Normalize("\u00B2", UnicodeNormalizationForm.Nfkc),
            Is.EqualTo("2"));
        Assert.That(
            new UnicodeNormalization().ToNfkc("\u00BD"),
            Is.EqualTo("\u0031\u2044\u0032"));
        Assert.That(
            new UnicodeNormalization().ToNfkc("U\u0308\u0300"),
            Is.EqualTo("\u01DB"));
    }

    [Test]
    public void Normalize_LeavesAlreadyNormalizedRepresentativeTextStable()
    {
        Assert.That(new UnicodeNormalization().ToNfc("\u01DB"), Is.EqualTo("\u01DB"));
        Assert.That(new UnicodeNormalization().ToNfd("U\u0327\u0308\u0300"), Is.EqualTo("U\u0327\u0308\u0300"));
    }

    [Test]
    public void Normalize_RejectsNullTextAndUnknownForms()
    {
        Assert.Throws<ArgumentNullException>(() => new UnicodeNormalization().Normalize(null!, UnicodeNormalizationForm.Nfc));
        Assert.Throws<ArgumentNullException>(() => new UnicodeNormalization().ToNfc(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodeNormalization().Normalize("A", (UnicodeNormalizationForm)99));
    }
}
