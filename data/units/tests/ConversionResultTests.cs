using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class ConversionResultTests
{
    [Test]
    public void CreateSuccess_ReturnsConvertedValueAndUnit()
    {
        UnitDefinition foot = CreateFoot();

        ConversionResult result = ConversionResult.CreateSuccess(3.280839895d, foot);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.IsFailure(), Is.False);
        Assert.That(result.GetValue(), Is.EqualTo(3.280839895d));
        Assert.That(result.GetUnit(), Is.SameAs(foot));
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.None));
        Assert.That(result.GetMessage(), Is.EqualTo(""));
    }

    [Test]
    public void CreateFailure_ReturnsErrorDetailsAndGuardsValueAccess()
    {
        ConversionResult result = ConversionResult.CreateFailure(
            UnitErrorCode.ConversionIncompatibleDimensions,
            "Units have incompatible dimensions.");

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionIncompatibleDimensions));
        Assert.That(result.GetMessage(), Is.EqualTo("Units have incompatible dimensions."));
        Assert.That(result._unit, Is.Null);
        Assert.Throws<System.InvalidOperationException>(() => result.GetValue());
        Assert.Throws<System.InvalidOperationException>(() => result.GetUnit());
    }

    [Test]
    public void Constructor_NormalizesNullFailureMessageToEmptyText()
    {
        ConversionResult result = new ConversionResult(false, 0.0d, null, UnitErrorCode.ConversionUnsupported, null);

        Assert.That(result.GetMessage(), Is.EqualTo(""));
    }

    [Test]
    public void Constructor_RejectsInvalidSuccessState()
    {
        UnitDefinition foot = CreateFoot();

        Assert.Throws<System.ArgumentNullException>(() => new ConversionResult(true, 1.0d, null!, UnitErrorCode.None, ""));
        Assert.Throws<System.ArgumentException>(() => new ConversionResult(true, 1.0d, foot, UnitErrorCode.ConversionUnsupported, ""));
    }

    [Test]
    public void Constructor_RejectsInvalidFailureState()
    {
        UnitDefinition foot = CreateFoot();

        Assert.Throws<System.ArgumentException>(() => new ConversionResult(false, 0.0d, foot, UnitErrorCode.None, "Missing error code."));
        Assert.Throws<System.ArgumentException>(() => new ConversionResult(false, 0.0d, foot, UnitErrorCode.ConversionUnsupported, "Unexpected unit."));
        Assert.Throws<System.ArgumentException>(() => new ConversionResult(false, 0.0d, null, UnitErrorCode.None, "Missing error code."));
        Assert.Throws<System.ArgumentException>(() => ConversionResult.CreateFailure(UnitErrorCode.None, "Missing error code."));
    }

    static UnitDefinition CreateFoot()
    {
        return new UnitDefinition(
            "foot",
            "foot",
            "feet",
            "ft",
            new string[0],
            DimensionVector.Length(),
            new RationalNumber(381, 1250),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);
    }
}
