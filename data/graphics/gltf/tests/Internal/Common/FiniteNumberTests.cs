namespace gltf.Tests.Internal.Common;

[TestFixture]
public class FiniteNumberTests
{
    [Test]
    public void FinitePredicates_DistinguishFiniteAndNonFiniteValues()
    {
        Assert.That(FiniteNumber.IsFiniteSingle(float.MaxValue), Is.True);
        Assert.That(FiniteNumber.IsFiniteSingle(float.NaN), Is.False);
        Assert.That(FiniteNumber.IsFiniteSingle(float.PositiveInfinity), Is.False);
        Assert.That(FiniteNumber.IsFiniteDouble(double.MinValue), Is.True);
        Assert.That(FiniteNumber.IsFiniteDouble(double.NaN), Is.False);
        Assert.That(FiniteNumber.IsFiniteDouble(double.NegativeInfinity), Is.False);
    }

    [Test]
    public void FiniteRequirements_ReturnFiniteValues()
    {
        Assert.That(FiniteNumber.RequireFiniteSingle(1.25F, "value"), Is.EqualTo(1.25F));
        Assert.That(FiniteNumber.RequireFiniteDouble(-2.5, "value"), Is.EqualTo(-2.5));
    }

    [Test]
    public void FiniteRequirements_RejectNonFiniteValuesWithParameterName()
    {
        ArgumentOutOfRangeException? singleException = Assert.Throws<ArgumentOutOfRangeException>(
            () => FiniteNumber.RequireFiniteSingle(float.NaN, "singleValue"));
        ArgumentOutOfRangeException? doubleException = Assert.Throws<ArgumentOutOfRangeException>(
            () => FiniteNumber.RequireFiniteDouble(double.PositiveInfinity, "doubleValue"));

        Assert.That(singleException!.ParamName, Is.EqualTo("singleValue"));
        Assert.That(doubleException!.ParamName, Is.EqualTo("doubleValue"));
    }
}
