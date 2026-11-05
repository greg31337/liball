using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeCanonicalOrderingTests
{
    [Test]
    public void OrderCanonicalCodePoints_OrdersNonStartersByCombiningClass()
    {
        Assert.That(
            UnicodeNormalization.OrderCanonicalCodePoints(new int[] { 0x0061, 0x0315, 0x0327, 0x0301 }),
            Is.EqualTo(new int[] { 0x0061, 0x0327, 0x0301, 0x0315 }));
    }

    [Test]
    public void OrderCanonicalCodePoints_KeepsEqualCombiningClassesStable()
    {
        Assert.That(
            UnicodeNormalization.OrderCanonicalCodePoints(new int[] { 0x0061, 0x0301, 0x0300, 0x0308 }),
            Is.EqualTo(new int[] { 0x0061, 0x0301, 0x0300, 0x0308 }));
    }

    [Test]
    public void OrderCanonicalCodePoints_DoesNotReorderAcrossStarters()
    {
        Assert.That(
            UnicodeNormalization.OrderCanonicalCodePoints(new int[] { 0x0315, 0x0061, 0x0327 }),
            Is.EqualTo(new int[] { 0x0315, 0x0061, 0x0327 }));
    }

    [Test]
    public void OrderCanonicalCodePoints_OrdersLeadingNonStarterRun()
    {
        Assert.That(
            UnicodeNormalization.OrderCanonicalCodePoints(new int[] { 0x0315, 0x0327, 0x0301 }),
            Is.EqualTo(new int[] { 0x0327, 0x0301, 0x0315 }));
    }

    [Test]
    public void OrderCanonical_OrdersTextByCanonicalCombiningClass()
    {
        Assert.That(UnicodeNormalization.OrderCanonical("a\u0315\u0327\u0301"), Is.EqualTo("a\u0327\u0301\u0315"));
    }

    [Test]
    public void DecomposeAndOrderCanonical_DecomposesThenOrdersMarks()
    {
        Assert.That(UnicodeNormalization.DecomposeAndOrderCanonical("\u01DB\u0327"), Is.EqualTo("U\u0327\u0308\u0300"));
    }

    [Test]
    public void DecomposeAndOrderCompatibility_DecomposesCompatibilityMappingsThenOrdersMarks()
    {
        Assert.That(UnicodeNormalization.DecomposeAndOrderCompatibility("\u00B2\u0315\u0327"), Is.EqualTo("2\u0327\u0315"));
    }

    [Test]
    public void OrderCanonical_RejectsNullInputs()
    {
        Assert.Throws<ArgumentNullException>(() => UnicodeNormalization.OrderCanonical(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodeNormalization.OrderCanonicalCodePoints(null!));
    }
}
