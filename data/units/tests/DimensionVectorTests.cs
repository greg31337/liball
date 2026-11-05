using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class DimensionVectorTests
{
    [Test]
    public void Constructor_StoresEverySupportedBaseDimensionExponent()
    {
        DimensionVector vector = new DimensionVector(1, -2, 3, -4, 5, -6, 7, -8, 9, -10, 11, -12);

        Assert.That(vector.GetLengthExponent(), Is.EqualTo(1));
        Assert.That(vector.GetMassExponent(), Is.EqualTo(-2));
        Assert.That(vector.GetTimeExponent(), Is.EqualTo(3));
        Assert.That(vector.GetCurrentExponent(), Is.EqualTo(-4));
        Assert.That(vector.GetTemperatureExponent(), Is.EqualTo(5));
        Assert.That(vector.GetAmountExponent(), Is.EqualTo(-6));
        Assert.That(vector.GetLuminousIntensityExponent(), Is.EqualTo(7));
        Assert.That(vector.GetAngleExponent(), Is.EqualTo(-8));
        Assert.That(vector.GetSolidAngleExponent(), Is.EqualTo(9));
        Assert.That(vector.GetInformationExponent(), Is.EqualTo(-10));
        Assert.That(vector.GetCurrencyExponent(), Is.EqualTo(11));
        Assert.That(vector.GetCountExponent(), Is.EqualTo(-12));
    }

    [Test]
    public void SlotConstants_DefineStableBaseDimensionOrder()
    {
        Assert.That(DimensionVector.GetSlotCount(), Is.EqualTo(12));
        Assert.That(DimensionSlots.Length, Is.EqualTo(0));
        Assert.That(DimensionSlots.Mass, Is.EqualTo(1));
        Assert.That(DimensionSlots.Time, Is.EqualTo(2));
        Assert.That(DimensionSlots.ElectricCurrent, Is.EqualTo(3));
        Assert.That(DimensionSlots.ThermodynamicTemperature, Is.EqualTo(4));
        Assert.That(DimensionSlots.AmountOfSubstance, Is.EqualTo(5));
        Assert.That(DimensionSlots.LuminousIntensity, Is.EqualTo(6));
        Assert.That(DimensionSlots.PlaneAngle, Is.EqualTo(7));
        Assert.That(DimensionSlots.SolidAngle, Is.EqualTo(8));
        Assert.That(DimensionSlots.Information, Is.EqualTo(9));
        Assert.That(DimensionSlots.Currency, Is.EqualTo(10));
        Assert.That(DimensionSlots.Count, Is.EqualTo(11));
    }

    [Test]
    public void GetExponent_ReturnsValueByStableSlot()
    {
        DimensionVector vector = new DimensionVector(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);

        Assert.That(vector.GetExponent(DimensionSlots.Length), Is.EqualTo(1));
        Assert.That(vector.GetExponent(DimensionSlots.Mass), Is.EqualTo(2));
        Assert.That(vector.GetExponent(DimensionSlots.Time), Is.EqualTo(3));
        Assert.That(vector.GetExponent(DimensionSlots.ElectricCurrent), Is.EqualTo(4));
        Assert.That(vector.GetExponent(DimensionSlots.ThermodynamicTemperature), Is.EqualTo(5));
        Assert.That(vector.GetExponent(DimensionSlots.AmountOfSubstance), Is.EqualTo(6));
        Assert.That(vector.GetExponent(DimensionSlots.LuminousIntensity), Is.EqualTo(7));
        Assert.That(vector.GetExponent(DimensionSlots.PlaneAngle), Is.EqualTo(8));
        Assert.That(vector.GetExponent(DimensionSlots.SolidAngle), Is.EqualTo(9));
        Assert.That(vector.GetExponent(DimensionSlots.Information), Is.EqualTo(10));
        Assert.That(vector.GetExponent(DimensionSlots.Currency), Is.EqualTo(11));
        Assert.That(vector.GetExponent(DimensionSlots.Count), Is.EqualTo(12));
    }

    [Test]
    public void GetExponent_RejectsUnknownSlot()
    {
        DimensionVector vector = new DimensionVector(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        Assert.Throws<System.ArgumentException>(() => vector.GetExponent(12));
        Assert.Throws<System.ArgumentException>(() => vector.GetExponent(-1));
    }

    [Test]
    public void DimensionlessFactory_ReturnsAllZeroExponents()
    {
        DimensionVector vector = DimensionVector.Dimensionless();

        AssertAllZero(vector);
    }

    [Test]
    public void BaseDimensionFactories_SetOnlyTheirNamedExponent()
    {
        AssertSingleBaseDimension(DimensionVector.Length(), DimensionSlots.Length);
        AssertSingleBaseDimension(DimensionVector.Mass(), DimensionSlots.Mass);
        AssertSingleBaseDimension(DimensionVector.Time(), DimensionSlots.Time);
        AssertSingleBaseDimension(DimensionVector.ElectricCurrent(), DimensionSlots.ElectricCurrent);
        AssertSingleBaseDimension(DimensionVector.ThermodynamicTemperature(), DimensionSlots.ThermodynamicTemperature);
        AssertSingleBaseDimension(DimensionVector.AmountOfSubstance(), DimensionSlots.AmountOfSubstance);
        AssertSingleBaseDimension(DimensionVector.LuminousIntensity(), DimensionSlots.LuminousIntensity);
        AssertSingleBaseDimension(DimensionVector.PlaneAngle(), DimensionSlots.PlaneAngle);
        AssertSingleBaseDimension(DimensionVector.SolidAngle(), DimensionSlots.SolidAngle);
        AssertSingleBaseDimension(DimensionVector.Information(), DimensionSlots.Information);
        AssertSingleBaseDimension(DimensionVector.Currency(), DimensionSlots.Currency);
        AssertSingleBaseDimension(DimensionVector.Count(), DimensionSlots.Count);
    }

    [Test]
    public void EqualsDimension_ComparesAllExponentSlots()
    {
        DimensionVector left = new DimensionVector(1, 0, -2, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        DimensionVector same = new DimensionVector(1, 0, -2, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        DimensionVector different = new DimensionVector(1, 1, -2, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        Assert.That(left.EqualsDimension(same), Is.True);
        Assert.That(left.EqualsDimension(different), Is.False);
        Assert.That(left.EqualsDimension(null), Is.False);
    }

    [Test]
    public void IsDimensionless_ReturnsTrueOnlyWhenAllExponentsAreZero()
    {
        Assert.That(DimensionVector.Dimensionless().IsDimensionless(), Is.True);
        Assert.That(DimensionVector.Length().IsDimensionless(), Is.False);
    }

    [Test]
    public void Multiply_AddsEachExponentSlot()
    {
        DimensionVector left = new DimensionVector(1, 2, -3, 4, -5, 6, -7, 8, -9, 10, -11, 12);
        DimensionVector right = new DimensionVector(-2, 3, 4, -5, 6, -7, 8, -9, 10, -11, 12, -13);

        DimensionVector result = left.Multiply(right);

        AssertVector(result, -1, 5, 1, -1, 1, -1, 1, -1, 1, -1, 1, -1);
    }

    [Test]
    public void Divide_SubtractsEachExponentSlot()
    {
        DimensionVector left = new DimensionVector(1, 2, -3, 4, -5, 6, -7, 8, -9, 10, -11, 12);
        DimensionVector right = new DimensionVector(-2, 3, 4, -5, 6, -7, 8, -9, 10, -11, 12, -13);

        DimensionVector result = left.Divide(right);

        AssertVector(result, 3, -1, -7, 9, -11, 13, -15, 17, -19, 21, -23, 25);
    }

    [Test]
    public void Pow_MultipliesEachExponentSlot()
    {
        DimensionVector vector = new DimensionVector(1, -2, 3, -4, 5, -6, 7, -8, 9, -10, 11, -12);

        DimensionVector result = vector.Pow(3);

        AssertVector(result, 3, -6, 9, -12, 15, -18, 21, -24, 27, -30, 33, -36);
    }

    [Test]
    public void Pow_HandlesZeroAndNegativePowersWithoutChangingOriginal()
    {
        DimensionVector vector = new DimensionVector(1, -2, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        DimensionVector zero = vector.Pow(0);
        DimensionVector inverse = vector.Pow(-1);

        Assert.That(zero.IsDimensionless(), Is.True);
        AssertVector(inverse, -1, 2, -3, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        AssertVector(vector, 1, -2, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    [Test]
    public void AlgebraMethods_ThrowWhenAnExponentOverflows()
    {
        DimensionVector maximumLength = new DimensionVector(int.MaxValue, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        DimensionVector minimumLength = new DimensionVector(int.MinValue, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        Assert.Throws<System.OverflowException>(() => maximumLength.Multiply(DimensionVector.Length()));
        Assert.Throws<System.OverflowException>(() => minimumLength.Divide(DimensionVector.Length()));
        Assert.Throws<System.OverflowException>(() => maximumLength.Pow(2));
    }

    [Test]
    public void Root_ReturnsDividedExponentsWhenAllSlotsDivideEvenly()
    {
        DimensionVector vector = new DimensionVector(2, -4, 6, -8, 10, -12, 14, -16, 18, -20, 22, -24);

        DimensionRootResult result = vector.Root(2);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.IsFailure(), Is.False);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.None));
        AssertVector(result.GetDimension(), 1, -2, 3, -4, 5, -6, 7, -8, 9, -10, 11, -12);
    }

    [Test]
    public void Root_ReturnsFailureWhenAnyExponentDoesNotDivideEvenly()
    {
        DimensionVector vector = new DimensionVector(2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        DimensionRootResult result = vector.Root(2);

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.ArithmeticInvalidOperation));
        Assert.That(result.GetMessage(), Does.Contain("divide evenly"));
        Assert.Throws<System.InvalidOperationException>(() => result.GetDimension());
    }

    [Test]
    public void Root_ReturnsFailureForNonPositiveRoot()
    {
        DimensionVector vector = DimensionVector.Length();

        DimensionRootResult result = vector.Root(0);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.ArithmeticInvalidOperation));
        Assert.That(result.GetMessage(), Does.Contain("greater than zero"));

        DimensionRootResult negative = vector.Root(-1);
        Assert.That(negative.IsFailure(), Is.True);
        Assert.That(negative.GetErrorCode(), Is.EqualTo(UnitErrorCode.ArithmeticInvalidOperation));
    }

    [Test]
    public void Root_HandlesIdentityAndDimensionlessBoundaryRoots()
    {
        DimensionVector vector = new DimensionVector(1, -2, 3, -4, 5, -6, 7, -8, 9, -10, 11, -12);

        DimensionRootResult identity = vector.Root(1);
        DimensionRootResult dimensionless = DimensionVector.Dimensionless().Root(int.MaxValue);

        Assert.That(identity.IsSuccess(), Is.True);
        Assert.That(identity.GetDimension().EqualsDimension(vector), Is.True);
        Assert.That(identity.GetDimension(), Is.Not.SameAs(vector));
        Assert.That(dimensionless.IsSuccess(), Is.True);
        Assert.That(dimensionless.GetDimension().IsDimensionless(), Is.True);
    }

    [Test]
    public void ToKey_UsesCanonicalSlotOrder()
    {
        DimensionVector vector = new DimensionVector(1, -2, 3, -4, 5, -6, 7, -8, 9, -10, 11, -12);

        Assert.That(vector.ToKey(), Is.EqualTo("1,-2,3,-4,5,-6,7,-8,9,-10,11,-12"));
    }

    [Test]
    public void AlgebraMethods_RejectNullOperands()
    {
        DimensionVector vector = DimensionVector.Length();

        Assert.Throws<System.ArgumentNullException>(() => vector.Multiply(null!));
        Assert.Throws<System.ArgumentNullException>(() => vector.Divide(null!));
    }

    static void AssertAllZero(DimensionVector vector)
    {
        int slot = 0;

        while (slot < DimensionVector.GetSlotCount())
        {
            Assert.That(vector.GetExponent(slot), Is.EqualTo(0));
            slot = slot + 1;
        }
    }

    static void AssertSingleBaseDimension(DimensionVector vector, int expectedSlot)
    {
        int slot = 0;

        while (slot < DimensionVector.GetSlotCount())
        {
            if (slot == expectedSlot)
            {
                Assert.That(vector.GetExponent(slot), Is.EqualTo(1));
            }
            else
            {
                Assert.That(vector.GetExponent(slot), Is.EqualTo(0));
            }

            slot = slot + 1;
        }
    }

    static void AssertVector(DimensionVector vector, int length, int mass, int time, int current, int temperature, int amount, int luminousIntensity, int angle, int solidAngle, int information, int currency, int count)
    {
        Assert.That(vector.GetLengthExponent(), Is.EqualTo(length));
        Assert.That(vector.GetMassExponent(), Is.EqualTo(mass));
        Assert.That(vector.GetTimeExponent(), Is.EqualTo(time));
        Assert.That(vector.GetCurrentExponent(), Is.EqualTo(current));
        Assert.That(vector.GetTemperatureExponent(), Is.EqualTo(temperature));
        Assert.That(vector.GetAmountExponent(), Is.EqualTo(amount));
        Assert.That(vector.GetLuminousIntensityExponent(), Is.EqualTo(luminousIntensity));
        Assert.That(vector.GetAngleExponent(), Is.EqualTo(angle));
        Assert.That(vector.GetSolidAngleExponent(), Is.EqualTo(solidAngle));
        Assert.That(vector.GetInformationExponent(), Is.EqualTo(information));
        Assert.That(vector.GetCurrencyExponent(), Is.EqualTo(currency));
        Assert.That(vector.GetCountExponent(), Is.EqualTo(count));
    }
}
