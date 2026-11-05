using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeFcdFccTests
{
    [Test]
    public void LeadingAndTrailingCanonicalCombiningClass_UseCanonicalDecomposition()
    {
        Assert.That(UnicodeNormalization.GetLeadingCanonicalCombiningClass(0x01DB), Is.EqualTo(0));
        Assert.That(UnicodeNormalization.GetTrailingCanonicalCombiningClass(0x01DB), Is.EqualTo(230));
        Assert.That(UnicodeNormalization.GetLeadingCanonicalCombiningClass(0x0327), Is.EqualTo(202));
        Assert.That(UnicodeNormalization.GetTrailingCanonicalCombiningClass(0x0327), Is.EqualTo(202));
    }

    [Test]
    public void IsFcd_ReturnsTrueForCanonicallyOrderedText()
    {
        Assert.That(new UnicodeNormalization().IsFcd("A\u0327\u0301\u0315"), Is.True);
        Assert.That(new UnicodeNormalization().IsFcd("\u01DB"), Is.True);
        Assert.That(new UnicodeNormalization().IsFcd("\uAC01"), Is.True);
    }

    [Test]
    public void IsFcd_ReturnsFalseForDescendingAdjacentCanonicalClasses()
    {
        Assert.That(new UnicodeNormalization().IsFcd("A\u0315\u0327"), Is.False);
        Assert.That(new UnicodeNormalization().IsFcd("\u0301\u0327"), Is.False);
    }

    [Test]
    public void ToFcd_ProducesCanonicallyDecomposedOrderedText()
    {
        Assert.That(new UnicodeNormalization().ToFcd("\u01DB\u0327"), Is.EqualTo("U\u0327\u0308\u0300"));
        Assert.That(new UnicodeNormalization().IsFcd(new UnicodeNormalization().ToFcd("A\u0315\u0327")), Is.True);
        Assert.That(new UnicodeNormalization().ToFcd("\u01DB"), Is.EqualTo("\u01DB"));
    }

    [Test]
    public void ToFcc_ProducesCanonicallyComposedFcdText()
    {
        Assert.That(new UnicodeNormalization().ToFcc("U\u0308\u0300"), Is.EqualTo("\u01DB"));
        Assert.That(new UnicodeNormalization().ToFcc("\u1100\u1161\u11A8"), Is.EqualTo("\uAC01"));
    }

    [Test]
    public void ToFcc_PreventsDiscontiguousCompositionAndAlwaysProducesFcd()
    {
        UnicodeNormalization normalization = new UnicodeNormalization();
        string source = "A\u0327\u030A";

        Assert.That(normalization.ToNfc(source), Is.EqualTo("\u00C5\u0327"));
        Assert.That(normalization.IsFcd(normalization.ToNfc(source)), Is.False);
        Assert.That(normalization.ToFcc(source), Is.EqualTo(source));
        Assert.That(normalization.IsFcd(normalization.ToFcc(source)), Is.True);
        Assert.That(normalization.IsFcc(source), Is.True);
        Assert.That(normalization.IsFcc("\u00C5\u0327"), Is.False);
    }

    [Test]
    public void ToFcc_AllowsSuccessiveContiguousComposition()
    {
        UnicodeNormalization normalization = new UnicodeNormalization();

        Assert.That(normalization.ToFcc("U\u0308\u0300"), Is.EqualTo("\u01DB"));
        Assert.That(normalization.ToFcc(normalization.ToFcc("U\u0308\u0300")), Is.EqualTo("\u01DB"));
    }

    [Test]
    public void IsFcc_ComparesAgainstFccForm()
    {
        Assert.That(new UnicodeNormalization().IsFcc("\u01DB"), Is.True);
        Assert.That(new UnicodeNormalization().IsFcc("U\u0308\u0300"), Is.False);
    }

    [Test]
    public void FcdAndFccApis_RejectNullText()
    {
        Assert.Throws<ArgumentNullException>(() => new UnicodeNormalization().IsFcd(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodeNormalization().ToFcd(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodeNormalization().IsFcc(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodeNormalization().ToFcc(null!));
    }
}
