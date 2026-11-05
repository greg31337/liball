using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class QuantityArithmeticResultTests
{
    [Test]
    public void Success_ExposesQuantityAndNoError()
    {
        QuantityValue quantity = new QuantityValue(2.0d, new StandardUnits().Meter());

        QuantityArithmeticResult result = QuantityArithmeticResult.CreateSuccess(quantity);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.IsFailure(), Is.False);
        Assert.That(result.GetQuantity(), Is.SameAs(quantity));
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.None));
        Assert.That(result.GetMessage(), Is.Empty);
        Assert.That(result, Is.AssignableTo<IQuantityArithmeticResult>());
    }

    [Test]
    public void Failure_ExposesTypedErrorAndNoQuantity()
    {
        QuantityArithmeticResult result = QuantityArithmeticResult.CreateFailure(UnitErrorCode.ArithmeticIncompatibleDimensions, null);

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.ArithmeticIncompatibleDimensions));
        Assert.That(result.GetMessage(), Is.Empty);
        Assert.Throws<System.InvalidOperationException>(() => result.GetQuantity());
    }

    [Test]
    public void Constructor_RejectsInconsistentStates()
    {
        QuantityValue quantity = new QuantityValue(2.0d, new StandardUnits().Meter());

        Assert.Throws<System.ArgumentNullException>(() => new QuantityArithmeticResult(true, null!, UnitErrorCode.None, ""));
        Assert.Throws<System.ArgumentException>(() => new QuantityArithmeticResult(true, quantity, UnitErrorCode.ArithmeticInvalidOperation, ""));
        Assert.Throws<System.ArgumentException>(() => new QuantityArithmeticResult(false, null, UnitErrorCode.None, ""));
        Assert.Throws<System.ArgumentException>(() => new QuantityArithmeticResult(false, quantity, UnitErrorCode.ArithmeticInvalidOperation, ""));
    }
}
