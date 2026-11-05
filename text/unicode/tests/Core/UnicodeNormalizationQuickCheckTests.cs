using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeNormalizationQuickCheckTests
{
    [Test]
    public void QuickCheck_ReturnsYesForRepresentativeNormalizedText()
    {
        Assert.That(new UnicodeNormalization().QuickCheck("ABC", UnicodeNormalizationForm.Nfc), Is.EqualTo(UnicodeQuickCheckResult.Yes));
        Assert.That(new UnicodeNormalization().QuickCheck("U\u0327\u0308\u0300", UnicodeNormalizationForm.Nfd), Is.EqualTo(UnicodeQuickCheckResult.Yes));
    }

    [Test]
    public void QuickCheck_ReturnsNoForKnownNoValuesOrUnorderedText()
    {
        Assert.That(new UnicodeNormalization().QuickCheck("\u00C0", UnicodeNormalizationForm.Nfd), Is.EqualTo(UnicodeQuickCheckResult.No));
        Assert.That(new UnicodeNormalization().QuickCheck("\u00B2", UnicodeNormalizationForm.Nfkd), Is.EqualTo(UnicodeQuickCheckResult.No));
        Assert.That(new UnicodeNormalization().QuickCheck("a\u0315\u0327", UnicodeNormalizationForm.Nfd), Is.EqualTo(UnicodeQuickCheckResult.No));
    }

    [Test]
    public void QuickCheck_ReturnsMaybeForKnownMaybeValuesThatAreOtherwiseStable()
    {
        Assert.That(new UnicodeNormalization().QuickCheck("\u0300", UnicodeNormalizationForm.Nfc), Is.EqualTo(UnicodeQuickCheckResult.Maybe));
        Assert.That(new UnicodeNormalization().QuickCheck("a\u0300", UnicodeNormalizationForm.Nfc), Is.EqualTo(UnicodeQuickCheckResult.Maybe));
        Assert.That(new UnicodeNormalization().IsNormalized("\u0300", UnicodeNormalizationForm.Nfc), Is.True);
        Assert.That(new UnicodeNormalization().IsNormalized("a\u0300", UnicodeNormalizationForm.Nfc), Is.False);
    }

    [Test]
    public void IsNormalized_ComparesAgainstRequestedNormalizationForm()
    {
        Assert.That(new UnicodeNormalization().IsNormalized("\u01DB", UnicodeNormalizationForm.Nfc), Is.True);
        Assert.That(new UnicodeNormalization().IsNormalized("U\u0308\u0300", UnicodeNormalizationForm.Nfc), Is.False);
        Assert.That(new UnicodeNormalization().IsNormalized("U\u0308\u0300", UnicodeNormalizationForm.Nfd), Is.True);
    }

    [Test]
    public void SpanQuickCheckYes_ReturnsLongestDefiniteYesPrefix()
    {
        Assert.That(new UnicodeNormalization().SpanQuickCheckYes("ABC", UnicodeNormalizationForm.Nfc), Is.EqualTo(3));
        Assert.That(new UnicodeNormalization().SpanQuickCheckYes("ABC\u00C0", UnicodeNormalizationForm.Nfd), Is.EqualTo(3));
        Assert.That(new UnicodeNormalization().SpanQuickCheckYes("a\u0300", UnicodeNormalizationForm.Nfc), Is.EqualTo(1));
        Assert.That(new UnicodeNormalization().SpanQuickCheckYes("\u00C0", UnicodeNormalizationForm.Nfd), Is.EqualTo(0));
        Assert.That(new UnicodeNormalization().SpanQuickCheckYes("\U0001F600A\u0300", UnicodeNormalizationForm.Nfc), Is.EqualTo(3));
        Assert.That(new UnicodeNormalization().SpanQuickCheckYes("A\u0315\u0327", UnicodeNormalizationForm.Nfd), Is.EqualTo(2));
    }

    [Test]
    public void GetQuickCheckValue_ReturnsTableValueOrImplicitYes()
    {
        Assert.That(UnicodeNormalization.GetQuickCheckValue(0x0340, "NFC"), Is.EqualTo("N"));
        Assert.That(UnicodeNormalization.GetQuickCheckValue(0x0300, "NFC"), Is.EqualTo("M"));
        Assert.That(UnicodeNormalization.GetQuickCheckValue(0x0041, "NFC"), Is.EqualTo("Y"));
    }

    [Test]
    public void QuickCheckApis_RejectNullAndUnknownForms()
    {
        Assert.Throws<ArgumentNullException>(() => new UnicodeNormalization().QuickCheck(null!, UnicodeNormalizationForm.Nfc));
        Assert.Throws<ArgumentNullException>(() => new UnicodeNormalization().IsNormalized(null!, UnicodeNormalizationForm.Nfc));
        Assert.Throws<ArgumentNullException>(() => new UnicodeNormalization().SpanQuickCheckYes(null!, UnicodeNormalizationForm.Nfc));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodeNormalization().QuickCheck("A", (UnicodeNormalizationForm)99));
    }
}
