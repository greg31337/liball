using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class QuantityConstructionResultTests
{
    [Test]
    public void Constructor_RejectsInconsistentResultStates()
    {
        QuantityValue quantity = new QuantityValue(1.0d, CreateMeter());

        Assert.Throws<System.ArgumentNullException>(() => new QuantityConstructionResult(true, null!, UnitErrorCode.None, "", new UnitDefinition[0]));
        Assert.Throws<System.ArgumentException>(() => new QuantityConstructionResult(true, quantity, UnitErrorCode.LookupNotFound, "", new UnitDefinition[0]));
        Assert.Throws<System.ArgumentException>(() => new QuantityConstructionResult(false, null, UnitErrorCode.None, "", new UnitDefinition[0]));
        Assert.Throws<System.ArgumentNullException>(() => new QuantityConstructionResult(false, null, UnitErrorCode.LookupNotFound, "", null!));
    }

    [Test]
    public void Failure_NormalizesNullMessageAndCopiesInputCandidates()
    {
        UnitDefinition meter = CreateMeter();
        UnitDefinition second = CreateSecond();
        UnitDefinition[] candidates = new UnitDefinition[] { meter };

        QuantityConstructionResult result = QuantityConstructionResult.CreateFailure(UnitErrorCode.LookupAmbiguous, null, candidates);
        candidates[0] = second;

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetMessage(), Is.Empty);
        Assert.That(result.GetCandidates(), Is.EqualTo(new UnitDefinition[] { meter }));
    }

    static UnitDefinition CreateMeter()
    {
        return new UnitDefinition("meter", "meter", "meters", "m", new string[0], DimensionVector.Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitConversionKind.Scale);
    }

    static UnitDefinition CreateSecond()
    {
        return new UnitDefinition("second", "second", "seconds", "s", new string[0], DimensionVector.Time(), new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitConversionKind.Scale);
    }
}
