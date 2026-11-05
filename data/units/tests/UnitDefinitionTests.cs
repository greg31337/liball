using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class UnitDefinitionTests
{
    [Test]
    public void Constructor_StoresUnitMetadata()
    {
        DimensionVector dimension = DimensionVector.Length();
        RationalNumber scale = new RationalNumber(1, 1);
        RationalNumber offset = new RationalNumber(0, 1);

        UnitDefinition unit = new UnitDefinition(
            "meter",
            "meter",
            "meters",
            "m",
            new string[] { "metre" },
            dimension,
            scale,
            offset,
            true,
            UnitConversionKind.Scale);

        Assert.That(unit.GetId(), Is.EqualTo("meter"));
        Assert.That(unit.GetName(), Is.EqualTo("meter"));
        Assert.That(unit.GetPluralName(), Is.EqualTo("meters"));
        Assert.That(unit.GetSymbol(), Is.EqualTo("m"));
        Assert.That(unit.GetAliases(), Is.EqualTo(new string[] { "metre" }));
        Assert.That(unit.GetDimension().EqualsDimension(dimension), Is.True);
        Assert.That(unit.GetScaleToSystemUnit().EqualsValue(scale), Is.True);
        Assert.That(unit.GetOffsetToSystemUnit().EqualsValue(offset), Is.True);
        Assert.That(unit.IsPrefixable(), Is.True);
        Assert.That(unit.GetConversionKind(), Is.EqualTo(UnitConversionKind.Scale));
    }

    [Test]
    public void Constructor_StoresAffineOffsetMetadata()
    {
        RationalNumber scale = new RationalNumber(1, 1);
        RationalNumber offset = new RationalNumber(27315, 100);

        UnitDefinition unit = new UnitDefinition(
            "degree_celsius",
            "degree Celsius",
            "degrees Celsius",
            "degC",
            new string[] { "celsius" },
            DimensionVector.ThermodynamicTemperature(),
            scale,
            offset,
            false,
            UnitConversionKind.Affine);

        Assert.That(unit.GetOffsetToSystemUnit().EqualsValue(new RationalNumber(27315, 100)), Is.True);
        Assert.That(unit.IsPrefixable(), Is.False);
        Assert.That(unit.GetConversionKind(), Is.EqualTo(UnitConversionKind.Affine));
    }

    [Test]
    public void Constructor_CopiesAliasesDefensively()
    {
        string[] aliases = new string[] { "metre", "metres" };
        UnitDefinition unit = CreateMeter(aliases);

        aliases[0] = "changed";
        string[] returnedAliases = unit.GetAliases();
        returnedAliases[1] = "changed";

        Assert.That(unit.GetAliases(), Is.EqualTo(new string[] { "metre", "metres" }));
    }

    [Test]
    public void Constructor_AllowsNullAliasesAsEmptyAliasList()
    {
        UnitDefinition unit = CreateMeter(null);

        Assert.That(unit.GetAliases(), Is.EqualTo(new string[0]));
    }

    [Test]
    public void Constructor_RejectsMissingRequiredMetadata()
    {
        string[] aliases = new string[0];

        Assert.Throws<System.ArgumentException>(() => new UnitDefinition("", "meter", "meters", "m", aliases, DimensionVector.Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitConversionKind.Scale));
        Assert.Throws<System.ArgumentException>(() => new UnitDefinition("meter", " ", "meters", "m", aliases, DimensionVector.Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitConversionKind.Scale));
        Assert.Throws<System.ArgumentException>(() => new UnitDefinition("meter", "meter", "", "m", aliases, DimensionVector.Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitConversionKind.Scale));
        Assert.Throws<System.ArgumentException>(() => new UnitDefinition("meter", "meter", "meters", "", aliases, DimensionVector.Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitConversionKind.Scale));
        Assert.Throws<System.ArgumentException>(() => new UnitDefinition("meter", "meter", "meters", "m", new string[] { "" }, DimensionVector.Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitConversionKind.Scale));
    }

    [Test]
    public void Constructor_RejectsMissingRequiredObjects()
    {
        string[] aliases = new string[0];

        Assert.Throws<System.ArgumentNullException>(() => new UnitDefinition("meter", "meter", "meters", "m", aliases, null!, new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitConversionKind.Scale));
        Assert.Throws<System.ArgumentNullException>(() => new UnitDefinition("meter", "meter", "meters", "m", aliases, DimensionVector.Length(), null!, new RationalNumber(0, 1), true, UnitConversionKind.Scale));
        Assert.Throws<System.ArgumentNullException>(() => new UnitDefinition("meter", "meter", "meters", "m", aliases, DimensionVector.Length(), new RationalNumber(1, 1), null!, true, UnitConversionKind.Scale));
    }

    [Test]
    public void Constructor_RejectsZeroScale()
    {
        Assert.Throws<System.ArgumentException>(
            () => new UnitDefinition(
                "bad",
                "bad",
                "bad",
                "bad",
                new string[0],
                DimensionVector.Length(),
                new RationalNumber(0, 1),
                new RationalNumber(0, 1),
                false,
                UnitConversionKind.Unsupported));
    }

    [Test]
    public void Constructor_RejectsUnknownConversionKind()
    {
        Assert.Throws<System.ArgumentException>(
            () => CreateUnit(
                "bad_kind",
                "bad kind",
                "bad kinds",
                "bk",
                DimensionVector.Length(),
                new RationalNumber(1, 1),
                new RationalNumber(0, 1),
                false,
                (UnitConversionKind)999));
    }

    [Test]
    public void IsCompatibleWith_ReturnsTrueForSameDimension()
    {
        UnitDefinition meter = CreateMeter(new string[0]);
        UnitDefinition foot = CreateUnit(
            "foot",
            "foot",
            "feet",
            "ft",
            DimensionVector.Length(),
            new RationalNumber(381, 1250),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);

        Assert.That(meter.IsCompatibleWith(foot), Is.True);
        Assert.That(foot.IsCompatibleWith(meter), Is.True);
    }

    [Test]
    public void IsCompatibleWith_ReturnsFalseForDifferentDimensionOrNull()
    {
        UnitDefinition meter = CreateMeter(new string[0]);
        UnitDefinition second = CreateUnit(
            "second",
            "second",
            "seconds",
            "s",
            DimensionVector.Time(),
            new RationalNumber(1, 1),
            new RationalNumber(0, 1),
            true,
            UnitConversionKind.Scale);

        Assert.That(meter.IsCompatibleWith(second), Is.False);
        Assert.That(meter.IsCompatibleWith(null), Is.False);
    }

    [Test]
    public void IsEquivalentTo_ReturnsTrueForSameConversionBehavior()
    {
        UnitDefinition meter = CreateMeter(new string[] { "metre" });
        UnitDefinition alternateMeter = CreateUnit(
            "metre",
            "metre",
            "metres",
            "m",
            DimensionVector.Length(),
            new RationalNumber(1, 1),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);

        Assert.That(meter.IsEquivalentTo(alternateMeter), Is.True);
        Assert.That(alternateMeter.IsEquivalentTo(meter), Is.True);
    }

    [Test]
    public void IsEquivalentTo_ReturnsFalseWhenScaleOffsetKindDimensionOrNullDiffer()
    {
        UnitDefinition meter = CreateMeter(new string[0]);
        UnitDefinition foot = CreateUnit("foot", "foot", "feet", "ft", DimensionVector.Length(), new RationalNumber(381, 1250), new RationalNumber(0, 1), false, UnitConversionKind.Scale);
        UnitDefinition shiftedMeter = CreateUnit("shifted_meter", "shifted meter", "shifted meters", "sm", DimensionVector.Length(), new RationalNumber(1, 1), new RationalNumber(1, 1), false, UnitConversionKind.Affine);
        UnitDefinition specialMeter = CreateUnit("special_meter", "special meter", "special meters", "spm", DimensionVector.Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), false, UnitConversionKind.Special);
        UnitDefinition second = CreateUnit("second", "second", "seconds", "s", DimensionVector.Time(), new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitConversionKind.Scale);

        Assert.That(meter.IsEquivalentTo(foot), Is.False);
        Assert.That(meter.IsEquivalentTo(shiftedMeter), Is.False);
        Assert.That(meter.IsEquivalentTo(specialMeter), Is.False);
        Assert.That(meter.IsEquivalentTo(second), Is.False);
        Assert.That(meter.IsEquivalentTo(null), Is.False);
    }

    [Test]
    public void Multiply_CreatesCompoundScaleOnlyUnit()
    {
        UnitDefinition meter = CreateMeter(new string[0]);
        UnitDefinition second = CreateSecond();

        UnitDefinition result = meter.Multiply(second);

        Assert.That(result.GetId(), Is.EqualTo("(meter*second)"));
        Assert.That(result.GetName(), Is.EqualTo("meter second"));
        Assert.That(result.GetSymbol(), Is.EqualTo("m*s"));
        Assert.That(result.GetDimension().EqualsDimension(new DimensionVector(1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0)), Is.True);
        Assert.That(result.GetScaleToSystemUnit().EqualsValue(new RationalNumber(1, 1)), Is.True);
        Assert.That(result.GetOffsetToSystemUnit().EqualsValue(new RationalNumber(0, 1)), Is.True);
        Assert.That(result.IsPrefixable(), Is.False);
        Assert.That(result.GetConversionKind(), Is.EqualTo(UnitConversionKind.Scale));
    }

    [Test]
    public void Divide_CreatesCompoundScaleOnlyUnit()
    {
        UnitDefinition meter = CreateMeter(new string[0]);
        UnitDefinition second = CreateSecond();

        UnitDefinition result = meter.Divide(second);

        Assert.That(result.GetId(), Is.EqualTo("(meter/second)"));
        Assert.That(result.GetName(), Is.EqualTo("meter per second"));
        Assert.That(result.GetSymbol(), Is.EqualTo("m/s"));
        Assert.That(result.GetDimension().EqualsDimension(new DimensionVector(1, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0)), Is.True);
        Assert.That(result.GetScaleToSystemUnit().EqualsValue(new RationalNumber(1, 1)), Is.True);
    }

    [Test]
    public void Inverse_CreatesReciprocalScaleOnlyUnit()
    {
        UnitDefinition second = CreateSecond();

        UnitDefinition result = second.Inverse();

        Assert.That(result.GetId(), Is.EqualTo("(1/second)"));
        Assert.That(result.GetName(), Is.EqualTo("inverse second"));
        Assert.That(result.GetSymbol(), Is.EqualTo("1/s"));
        Assert.That(result.GetDimension().EqualsDimension(new DimensionVector(0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0)), Is.True);
        Assert.That(result.GetScaleToSystemUnit().EqualsValue(new RationalNumber(1, 1)), Is.True);
    }

    [Test]
    public void Pow_CreatesPoweredScaleOnlyUnit()
    {
        UnitDefinition foot = CreateFoot();

        UnitDefinition result = foot.Pow(2);

        Assert.That(result.GetId(), Is.EqualTo("(foot^2)"));
        Assert.That(result.GetSymbol(), Is.EqualTo("ft^2"));
        Assert.That(result.GetDimension().EqualsDimension(new DimensionVector(2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)), Is.True);
        Assert.That(result.GetScaleToSystemUnit().EqualsValue(new RationalNumber(145161, 1562500)), Is.True);
    }

    [Test]
    public void Pow_WithZeroPowerCreatesDimensionlessUnit()
    {
        UnitDefinition foot = CreateFoot();

        UnitDefinition result = foot.Pow(0);

        Assert.That(result.GetDimension().IsDimensionless(), Is.True);
        Assert.That(result.GetScaleToSystemUnit().EqualsValue(new RationalNumber(1, 1)), Is.True);
    }

    [Test]
    public void Pow_WithNegativePowerCreatesExactReciprocalAndRejectsMinimumInteger()
    {
        UnitDefinition foot = CreateFoot();

        UnitDefinition inverseSquare = foot.Pow(-2);

        Assert.That(inverseSquare.GetDimension().EqualsDimension(DimensionVector.Length().Pow(-2)), Is.True);
        Assert.That(inverseSquare.GetScaleToSystemUnit().EqualsValue(new RationalNumber(1562500, 145161)), Is.True);
        Assert.Throws<System.OverflowException>(() => foot.Pow(int.MinValue));
    }

    [Test]
    public void Root_ReturnsUnitWhenDimensionAndScaleHaveExactRoot()
    {
        UnitDefinition squareFoot = CreateFoot().Pow(2);

        UnitRootResult result = squareFoot.Root(2);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.IsFailure(), Is.False);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.None));
        Assert.That(result.GetUnit().GetDimension().EqualsDimension(DimensionVector.Length()), Is.True);
        Assert.That(result.GetUnit().GetScaleToSystemUnit().EqualsValue(new RationalNumber(381, 1250)), Is.True);
    }

    [Test]
    public void Root_ReturnsFailureWhenDimensionCannotBeRooted()
    {
        UnitRootResult result = CreateMeter(new string[0]).Root(2);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.ArithmeticInvalidOperation));
        Assert.Throws<System.InvalidOperationException>(() => result.GetUnit());
    }

    [Test]
    public void Root_ReturnsFailureWhenScaleDoesNotHaveExactRationalRoot()
    {
        UnitDefinition scaledArea = CreateUnit(
            "scaled_area",
            "scaled area",
            "scaled areas",
            "sa",
            DimensionVector.Length().Pow(2),
            new RationalNumber(2, 1),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);

        UnitRootResult result = scaledArea.Root(2);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetMessage(), Does.Contain("exact rational root"));
    }

    [Test]
    public void Root_HandlesIdentityNonPositiveAndVeryLargeBoundaries()
    {
        UnitDefinition foot = CreateFoot();
        UnitDefinition sixtyThirdPower = CreateUnit(
            "large_power",
            "large power",
            "large powers",
            "lp",
            DimensionVector.Length().Pow(63),
            new RationalNumber(System.Numerics.BigInteger.Pow(new System.Numerics.BigInteger(2), 63), new System.Numerics.BigInteger(1)),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);

        UnitRootResult identity = foot.Root(1);
        UnitRootResult sixtyThirdRoot = sixtyThirdPower.Root(63);
        UnitRootResult zero = foot.Root(0);
        UnitRootResult negative = foot.Root(-1);

        Assert.That(identity.IsSuccess(), Is.True);
        Assert.That(identity.GetUnit().IsEquivalentTo(foot), Is.True);
        Assert.That(sixtyThirdRoot.IsSuccess(), Is.True, sixtyThirdRoot.GetMessage());
        Assert.That(sixtyThirdRoot.GetUnit().GetDimension().EqualsDimension(DimensionVector.Length()), Is.True);
        Assert.That(sixtyThirdRoot.GetUnit().GetScaleToSystemUnit().EqualsValue(new RationalNumber(2, 1)), Is.True);
        Assert.That(zero.IsFailure(), Is.True);
        Assert.That(negative.IsFailure(), Is.True);
        Assert.That(zero.GetErrorCode(), Is.EqualTo(UnitErrorCode.ArithmeticInvalidOperation));
        Assert.That(negative.GetErrorCode(), Is.EqualTo(UnitErrorCode.ArithmeticInvalidOperation));
    }

    [Test]
    public void Root_ReturnsExactOddRootForNegativeScaleAndRejectsEvenRoot()
    {
        UnitDefinition negativeCube = CreateUnit(
            "negative_cube",
            "negative cube",
            "negative cubes",
            "nc",
            DimensionVector.Length().Pow(3),
            new RationalNumber(-8, 27),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);
        UnitDefinition negativeSquare = CreateUnit(
            "negative_square",
            "negative square",
            "negative squares",
            "ns",
            DimensionVector.Length().Pow(2),
            new RationalNumber(-4, 9),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);

        UnitRootResult odd = negativeCube.Root(3);
        UnitRootResult even = negativeSquare.Root(2);

        Assert.That(odd.IsSuccess(), Is.True, odd.GetMessage());
        Assert.That(odd.GetUnit().GetScaleToSystemUnit().EqualsValue(new RationalNumber(-2, 3)), Is.True);
        Assert.That(odd.GetUnit().GetDimension().EqualsDimension(DimensionVector.Length()), Is.True);
        Assert.That(even.IsFailure(), Is.True);
        Assert.That(even.GetMessage(), Does.Contain("exact rational root"));
    }

    [Test]
    public void CompoundOperations_RejectNullAndNonScaleOnlyUnits()
    {
        UnitDefinition meter = CreateMeter(new string[0]);
        UnitDefinition celsius = CreateCelsius();

        Assert.Throws<System.ArgumentNullException>(() => meter.Multiply(null!));
        Assert.Throws<System.ArgumentNullException>(() => meter.Divide(null!));
        Assert.Throws<System.InvalidOperationException>(() => celsius.Multiply(meter));
        Assert.Throws<System.InvalidOperationException>(() => meter.Divide(celsius));
        Assert.Throws<System.InvalidOperationException>(() => celsius.Inverse());
        Assert.Throws<System.InvalidOperationException>(() => celsius.Pow(2));
        Assert.That(celsius.Root(2).IsFailure(), Is.True);
    }

    static UnitDefinition CreateMeter(string[]? aliases)
    {
        return new UnitDefinition(
            "meter",
            "meter",
            "meters",
            "m",
            aliases,
            DimensionVector.Length(),
            new RationalNumber(1, 1),
            new RationalNumber(0, 1),
            true,
            UnitConversionKind.Scale);
    }

    static UnitDefinition CreateFoot()
    {
        return CreateUnit(
            "foot",
            "foot",
            "feet",
            "ft",
            DimensionVector.Length(),
            new RationalNumber(381, 1250),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);
    }

    static UnitDefinition CreateSecond()
    {
        return CreateUnit(
            "second",
            "second",
            "seconds",
            "s",
            DimensionVector.Time(),
            new RationalNumber(1, 1),
            new RationalNumber(0, 1),
            true,
            UnitConversionKind.Scale);
    }

    static UnitDefinition CreateCelsius()
    {
        return CreateUnit(
            "degree_celsius",
            "degree Celsius",
            "degrees Celsius",
            "degC",
            DimensionVector.ThermodynamicTemperature(),
            new RationalNumber(1, 1),
            new RationalNumber(27315, 100),
            false,
            UnitConversionKind.Affine);
    }

    static UnitDefinition CreateUnit(string id, string name, string pluralName, string symbol, DimensionVector dimension, RationalNumber scale, RationalNumber offset, bool prefixable, UnitConversionKind conversionKind)
    {
        return new UnitDefinition(
            id,
            name,
            pluralName,
            symbol,
            new string[0],
            dimension,
            scale,
            offset,
            prefixable,
            conversionKind);
    }
}
