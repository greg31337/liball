using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class RootResultTests
{
    [Test]
    public void UnitRootResult_CreateSuccessStoresUnitAndSuccessState()
    {
        UnitDefinition unit = new StandardUnits().Meter();

        UnitRootResult result = UnitRootResult.CreateSuccess(unit);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.IsFailure(), Is.False);
        Assert.That(result.GetUnit().IsEquivalentTo(unit), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.None));
        Assert.That(result.GetMessage(), Is.EqualTo(""));
    }

    [Test]
    public void UnitRootResult_CreateFailureStoresErrorAndGuardsUnitAccess()
    {
        UnitRootResult result = UnitRootResult.CreateFailure(UnitErrorCode.ArithmeticInvalidOperation, null);

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.ArithmeticInvalidOperation));
        Assert.That(result.GetMessage(), Is.EqualTo(""));
        Assert.That(result._unit, Is.Null);
        Assert.Throws<System.InvalidOperationException>(() => result.GetUnit());
    }

    [Test]
    public void UnitRootResult_RejectsInvalidSuccessAndFailureStates()
    {
        UnitDefinition unit = new StandardUnits().Meter();

        Assert.Throws<System.ArgumentNullException>(() => UnitRootResult.CreateSuccess(null!));
        Assert.Throws<System.ArgumentException>(() => new UnitRootResult(true, unit, UnitErrorCode.ArithmeticInvalidOperation, ""));
        Assert.Throws<System.ArgumentException>(() => new UnitRootResult(false, unit, UnitErrorCode.None, ""));
        Assert.Throws<System.ArgumentException>(() => new UnitRootResult(false, unit, UnitErrorCode.ArithmeticInvalidOperation, ""));
        Assert.Throws<System.ArgumentException>(() => new UnitRootResult(false, null, UnitErrorCode.None, ""));
    }

    [Test]
    public void DimensionRootResult_CreateSuccessStoresDimensionAndSuccessState()
    {
        DimensionVector dimension = new StandardDimensions().Length();

        DimensionRootResult result = DimensionRootResult.CreateSuccess(dimension);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.IsFailure(), Is.False);
        Assert.That(result.GetDimension().EqualsDimension(dimension), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.None));
        Assert.That(result.GetMessage(), Is.EqualTo(""));
    }

    [Test]
    public void DimensionRootResult_CreateFailureStoresErrorAndGuardsDimensionAccess()
    {
        DimensionRootResult result = DimensionRootResult.CreateFailure(UnitErrorCode.ArithmeticInvalidOperation, null);

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.ArithmeticInvalidOperation));
        Assert.That(result.GetMessage(), Is.EqualTo(""));
        Assert.That(result._dimension, Is.Null);
        Assert.Throws<System.InvalidOperationException>(() => result.GetDimension());
    }

    [Test]
    public void DimensionRootResult_RejectsInvalidSuccessAndFailureStates()
    {
        DimensionVector dimension = new StandardDimensions().Length();

        Assert.Throws<System.ArgumentNullException>(() => DimensionRootResult.CreateSuccess(null!));
        Assert.Throws<System.ArgumentException>(() => new DimensionRootResult(true, dimension, UnitErrorCode.ArithmeticInvalidOperation, ""));
        Assert.Throws<System.ArgumentException>(() => new DimensionRootResult(false, dimension, UnitErrorCode.None, ""));
        Assert.Throws<System.ArgumentException>(() => new DimensionRootResult(false, dimension, UnitErrorCode.ArithmeticInvalidOperation, ""));
        Assert.Throws<System.ArgumentException>(() => new DimensionRootResult(false, null, UnitErrorCode.None, ""));
    }
}
