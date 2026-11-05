using System.Globalization;
using htmlgui;

namespace htmlgui.Tests;

[TestFixture]
public class InvariantFormatterTests
{
    [Test]
    public void FormatsPrimitiveValuesCanonicallyAcrossCultures()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");

            Assert.That(InvariantFormatter.FormatBoolean(true), Is.EqualTo("true"));
            Assert.That(InvariantFormatter.FormatBoolean(false), Is.EqualTo("false"));
            Assert.That(InvariantFormatter.FormatInt32(-1234), Is.EqualTo("-1234"));
            Assert.That(InvariantFormatter.FormatInt64(1234567890123L), Is.EqualTo("1234567890123"));
            Assert.That(InvariantFormatter.FormatDouble(12.5), Is.EqualTo("12.5"));
            Assert.That(InvariantFormatter.FormatDouble(-0.0), Is.EqualTo("0"));
            Assert.That(InvariantFormatter.FormatDecimal(12.5000m), Is.EqualTo("12.5"));
            Assert.That(InvariantFormatter.FormatDecimal(-0.000m), Is.EqualTo("0"));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Test]
    public void RejectsNonFiniteDoubleValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => InvariantFormatter.FormatDouble(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => InvariantFormatter.FormatDouble(double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => InvariantFormatter.FormatDouble(double.NegativeInfinity));
    }

    [Test]
    public void NormalizesEveryLineEndingToLineFeed()
    {
        Assert.That(InvariantFormatter.NormalizeLineEndings("a\r\nb\rc\nd"), Is.EqualTo("a\nb\nc\nd"));
        Assert.That(InvariantFormatter.NormalizeLineEndings(""), Is.Empty);
        Assert.Throws<ArgumentNullException>(() => InvariantFormatter.NormalizeLineEndings(null));
    }
}
