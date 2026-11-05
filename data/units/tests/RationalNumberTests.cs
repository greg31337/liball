using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class RationalNumberTests
{
    [Test]
    public void Constructor_ReducesFractionToLowestTerms()
    {
        RationalNumber value = new RationalNumber(42, 56);

        Assert.That(value.GetNumerator(), Is.EqualTo(3));
        Assert.That(value.GetDenominator(), Is.EqualTo(4));
    }

    [Test]
    public void Constructor_NormalizesNegativeDenominatorToNumerator()
    {
        RationalNumber value = new RationalNumber(5, -8);

        Assert.That(value.GetNumerator(), Is.EqualTo(-5));
        Assert.That(value.GetDenominator(), Is.EqualTo(8));
    }

    [Test]
    public void Constructor_NormalizesDoubleNegativeToPositiveValue()
    {
        RationalNumber value = new RationalNumber(-9, -12);

        Assert.That(value.GetNumerator(), Is.EqualTo(3));
        Assert.That(value.GetDenominator(), Is.EqualTo(4));
    }

    [Test]
    public void Constructor_NormalizesZeroToSingleDenominator()
    {
        RationalNumber value = new RationalNumber(0, -19);

        Assert.That(value.GetNumerator(), Is.EqualTo(0));
        Assert.That(value.GetDenominator(), Is.EqualTo(1));
    }

    [Test]
    public void Constructor_RejectsZeroDenominator()
    {
        Assert.Throws<System.ArgumentException>(() => new RationalNumber(1, 0));
    }

    [Test]
    public void Constructor_AcceptsAndNormalizesBigIntegersOutsideLongRange()
    {
        System.Numerics.BigInteger largeOdd = System.Numerics.BigInteger.Pow(new System.Numerics.BigInteger(10), 40) + new System.Numerics.BigInteger(1);

        RationalNumber value = new RationalNumber(largeOdd * new System.Numerics.BigInteger(6), new System.Numerics.BigInteger(8));

        Assert.That(value.GetNumeratorAsBigInteger(), Is.EqualTo(largeOdd * new System.Numerics.BigInteger(3)));
        Assert.That(value.GetDenominatorAsBigInteger(), Is.EqualTo(new System.Numerics.BigInteger(4)));
        Assert.Throws<System.OverflowException>(() => value.GetNumerator());
    }

    [Test]
    public void Constructor_RepresentsQuettaAndQuectoFactorsExactly()
    {
        System.Numerics.BigInteger tenToThirty = System.Numerics.BigInteger.Pow(new System.Numerics.BigInteger(10), 30);

        RationalNumber quetta = new RationalNumber(tenToThirty, new System.Numerics.BigInteger(1));
        RationalNumber quecto = new RationalNumber(new System.Numerics.BigInteger(1), tenToThirty);

        Assert.That(quetta.GetNumeratorText(), Is.EqualTo("1000000000000000000000000000000"));
        Assert.That(quetta.GetDenominatorText(), Is.EqualTo("1"));
        Assert.That(quecto.GetNumeratorText(), Is.EqualTo("1"));
        Assert.That(quecto.GetDenominatorText(), Is.EqualTo("1000000000000000000000000000000"));
        Assert.That(new StandardPrefixes().CreateCatalog().FindById("quetta")!.GetFactor().EqualsValue(quetta), Is.True);
        Assert.That(new StandardPrefixes().CreateCatalog().FindById("quecto")!.GetFactor().EqualsValue(quecto), Is.True);
    }

    [Test]
    public void Constructor_ProvidesExactPublicScaleAndOffsetRegistrationPath()
    {
        System.Numerics.BigInteger tenToThirty = System.Numerics.BigInteger.Pow(new System.Numerics.BigInteger(10), 30);
        RationalNumber scale = new RationalNumber(tenToThirty, new System.Numerics.BigInteger(1));
        RationalNumber offset = new RationalNumber(new System.Numerics.BigInteger(1), tenToThirty);
        UnitDefinition unit = new UnitDefinition(
            "arbitrary_range_unit",
            "arbitrary range unit",
            "arbitrary range units",
            "aru",
            new string[0],
            new StandardDimensions().Length(),
            scale,
            offset,
            false,
            UnitConversionKind.Affine);
        UnitRegistry registry = new UnitRegistry();

        OperationResult result = registry.AddUnit(unit);
        UnitDefinition registered = registry.FindUnitById("arbitrary_range_unit").GetUnit();

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(registered.GetScaleToSystemUnit().EqualsValue(scale), Is.True);
        Assert.That(registered.GetOffsetToSystemUnit().EqualsValue(offset), Is.True);
    }

    [Test]
    public void FromBigIntegers_PreservesValuesOutsideLongRange()
    {
        System.Numerics.BigInteger numerator = System.Numerics.BigInteger.Pow(new System.Numerics.BigInteger(10), 30);

        RationalNumber value = RationalNumber.FromBigIntegers(numerator, new System.Numerics.BigInteger(3));

        Assert.That(value.GetNumeratorText(), Is.EqualTo("1000000000000000000000000000000"));
        Assert.That(value.GetDenominatorText(), Is.EqualTo("3"));
        Assert.Throws<System.OverflowException>(() => value.GetNumerator());
    }

    [Test]
    public void FromBigIntegers_RejectsZeroDenominator()
    {
        Assert.Throws<System.ArgumentException>(() => RationalNumber.FromBigIntegers(new System.Numerics.BigInteger(1), new System.Numerics.BigInteger(0)));
    }

    [Test]
    public void EqualsValue_ComparesNormalizedValues()
    {
        RationalNumber left = new RationalNumber(6, 8);
        RationalNumber right = new RationalNumber(3, 4);
        RationalNumber different = new RationalNumber(4, 5);

        Assert.That(left.EqualsValue(right), Is.True);
        Assert.That(left.EqualsValue(different), Is.False);
        Assert.That(left.EqualsValue(null), Is.False);
    }

    [Test]
    public void ToDouble_ConvertsNormalizedFractionToDecimalValue()
    {
        RationalNumber value = new RationalNumber(1, 8);

        Assert.That(value.ToDouble(), Is.EqualTo(0.125d));
    }

    [Test]
    public void SignPredicates_ClassifyPositiveNegativeAndZeroBoundaries()
    {
        RationalNumber positive = new RationalNumber(1, 2);
        RationalNumber negative = new RationalNumber(-1, 2);
        RationalNumber zero = new RationalNumber(0, long.MinValue);

        Assert.That(positive.IsPositive(), Is.True);
        Assert.That(positive.IsNegative(), Is.False);
        Assert.That(positive.IsZero(), Is.False);
        Assert.That(negative.IsPositive(), Is.False);
        Assert.That(negative.IsNegative(), Is.True);
        Assert.That(negative.IsZero(), Is.False);
        Assert.That(zero.IsPositive(), Is.False);
        Assert.That(zero.IsNegative(), Is.False);
        Assert.That(zero.IsZero(), Is.True);
    }

    [Test]
    public void Add_ReturnsReducedSum()
    {
        RationalNumber left = new RationalNumber(1, 6);
        RationalNumber right = new RationalNumber(1, 3);

        RationalNumber result = left.Add(right);

        Assert.That(result.GetNumerator(), Is.EqualTo(1));
        Assert.That(result.GetDenominator(), Is.EqualTo(2));
    }

    [Test]
    public void Subtract_ReturnsReducedDifference()
    {
        RationalNumber left = new RationalNumber(3, 4);
        RationalNumber right = new RationalNumber(5, 6);

        RationalNumber result = left.Subtract(right);

        Assert.That(result.GetNumerator(), Is.EqualTo(-1));
        Assert.That(result.GetDenominator(), Is.EqualTo(12));
    }

    [Test]
    public void Multiply_ReturnsReducedProduct()
    {
        RationalNumber left = new RationalNumber(-2, 3);
        RationalNumber right = new RationalNumber(9, 10);

        RationalNumber result = left.Multiply(right);

        Assert.That(result.GetNumerator(), Is.EqualTo(-3));
        Assert.That(result.GetDenominator(), Is.EqualTo(5));
    }

    [Test]
    public void Divide_ReturnsReducedQuotient()
    {
        RationalNumber left = new RationalNumber(2, 3);
        RationalNumber right = new RationalNumber(4, 9);

        RationalNumber result = left.Divide(right);

        Assert.That(result.GetNumerator(), Is.EqualTo(3));
        Assert.That(result.GetDenominator(), Is.EqualTo(2));
    }

    [Test]
    public void Divide_RejectsZeroRationalValue()
    {
        RationalNumber left = new RationalNumber(2, 3);
        RationalNumber right = new RationalNumber(0, 5);

        Assert.Throws<System.ArgumentException>(() => left.Divide(right));
    }

    [Test]
    public void CompareTo_ReturnsOrderingForNormalizedValues()
    {
        RationalNumber oneHalf = new RationalNumber(1, 2);
        RationalNumber twoQuarters = new RationalNumber(2, 4);
        RationalNumber twoThirds = new RationalNumber(2, 3);
        RationalNumber negativeOne = new RationalNumber(-1, 1);

        Assert.That(oneHalf.CompareTo(twoQuarters), Is.EqualTo(0));
        Assert.That(oneHalf.CompareTo(twoThirds), Is.EqualTo(-1));
        Assert.That(twoThirds.CompareTo(oneHalf), Is.EqualTo(1));
        Assert.That(negativeOne.CompareTo(oneHalf), Is.EqualTo(-1));
    }

    [Test]
    public void ArithmeticMethods_RejectNullOperands()
    {
        RationalNumber value = new RationalNumber(1, 2);

        Assert.Throws<System.ArgumentNullException>(() => value.Add(null!));
        Assert.Throws<System.ArgumentNullException>(() => value.Subtract(null!));
        Assert.Throws<System.ArgumentNullException>(() => value.Multiply(null!));
        Assert.Throws<System.ArgumentNullException>(() => value.Divide(null!));
        Assert.Throws<System.ArgumentNullException>(() => value.CompareTo(null!));
    }

    [Test]
    public void ArithmeticMethods_ReturnNewValuesWithoutChangingOperands()
    {
        RationalNumber left = new RationalNumber(2, 3);
        RationalNumber right = new RationalNumber(5, 7);

        RationalNumber sum = left.Add(right);
        RationalNumber difference = left.Subtract(right);
        RationalNumber product = left.Multiply(right);
        RationalNumber quotient = left.Divide(right);

        Assert.That(sum, Is.Not.SameAs(left));
        Assert.That(difference, Is.Not.SameAs(left));
        Assert.That(product, Is.Not.SameAs(left));
        Assert.That(quotient, Is.Not.SameAs(left));
        Assert.That(left.EqualsValue(new RationalNumber(2, 3)), Is.True);
        Assert.That(right.EqualsValue(new RationalNumber(5, 7)), Is.True);
    }

    [Test]
    public void Multiply_PreservesExactValueWhenProductExceedsLongRange()
    {
        RationalNumber left = new RationalNumber(long.MaxValue, 1);
        RationalNumber right = new RationalNumber(2, 1);

        RationalNumber result = left.Multiply(right);

        Assert.That(result.GetNumeratorText(), Is.EqualTo("18446744073709551614"));
        Assert.That(result.GetDenominatorText(), Is.EqualTo("1"));
        Assert.Throws<System.OverflowException>(() => result.GetNumerator());
    }

    [Test]
    public void CompareTo_DoesNotOverflowWhenCrossProductsExceedLongRange()
    {
        RationalNumber left = new RationalNumber(long.MaxValue, 2);
        RationalNumber right = new RationalNumber(long.MaxValue - 1, 2);

        Assert.That(left.CompareTo(right), Is.EqualTo(1));
        Assert.That(right.CompareTo(left), Is.EqualTo(-1));
    }

    [Test]
    public void Int64Accessors_RejectArbitraryRangeValuesThatDoNotFit()
    {
        System.Numerics.BigInteger outsideRange = new System.Numerics.BigInteger(long.MaxValue) + new System.Numerics.BigInteger(1);
        RationalNumber largeNumerator = new RationalNumber(outsideRange, new System.Numerics.BigInteger(1));
        RationalNumber largeDenominator = new RationalNumber(new System.Numerics.BigInteger(1), outsideRange);

        Assert.Throws<System.OverflowException>(() => largeNumerator.GetNumerator());
        Assert.Throws<System.OverflowException>(() => largeDenominator.GetDenominator());
        Assert.That(largeNumerator.GetDenominator(), Is.EqualTo(1));
        Assert.That(largeDenominator.GetNumerator(), Is.EqualTo(1));
    }
}
