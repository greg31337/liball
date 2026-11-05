using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class UnitParseResultTests
{
    [Test]
    public void CreateSuccess_ReturnsParsedUnitWithNoError()
    {
        UnitDefinition meter = CreateMeter();

        UnitParseResult result = UnitParseResult.CreateSuccess(meter);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.IsFailure(), Is.False);
        Assert.That(result.GetUnit(), Is.SameAs(meter));
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.None));
        Assert.That(result.GetMessage(), Is.EqualTo(""));
        Assert.That(result.GetPosition(), Is.EqualTo(-1));
        Assert.That(result.GetCandidates(), Has.Length.EqualTo(0));
        Assert.That(result, Is.AssignableTo<IUnitParseResult>());
    }

    [Test]
    public void CreateFailure_ReturnsErrorDetailsAndCandidates()
    {
        UnitDefinition meter = CreateMeter();
        UnitDefinition minute = CreateMinute();

        UnitParseResult result = UnitParseResult.CreateFailure(
            UnitErrorCode.ParseAmbiguousUnit,
            "Unit expression was ambiguous.",
            4,
            new UnitDefinition[] { meter, minute });

        UnitDefinition[] candidates = result.GetCandidates();

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseAmbiguousUnit));
        Assert.That(result.GetMessage(), Is.EqualTo("Unit expression was ambiguous."));
        Assert.That(result.GetPosition(), Is.EqualTo(4));
        Assert.That(result._unit, Is.Null);
        Assert.Throws<System.InvalidOperationException>(() => result.GetUnit());
        Assert.That(candidates, Has.Length.EqualTo(2));
        Assert.That(candidates[0], Is.SameAs(meter));
        Assert.That(candidates[1], Is.SameAs(minute));
    }

    [Test]
    public void Constructor_NormalizesNullFailureMessageToEmptyText()
    {
        UnitParseResult result = new UnitParseResult(false, null, UnitErrorCode.ParseUnknownUnit, null, 0, new UnitDefinition[0]);

        Assert.That(result.GetMessage(), Is.EqualTo(""));
    }

    [Test]
    public void Constructor_CopiesCandidateArrays()
    {
        UnitDefinition meter = CreateMeter();
        UnitDefinition minute = CreateMinute();
        UnitDefinition[] candidates = new UnitDefinition[] { meter };

        UnitParseResult result = UnitParseResult.CreateFailure(UnitErrorCode.ParseAmbiguousUnit, "Ambiguous.", 0, candidates);
        candidates[0] = minute;
        UnitDefinition[] returnedCandidates = result.GetCandidates();
        returnedCandidates[0] = minute;

        Assert.That(result.GetCandidates()[0], Is.SameAs(meter));
    }

    [Test]
    public void Constructor_RejectsInvalidSuccessState()
    {
        UnitDefinition meter = CreateMeter();

        Assert.Throws<System.ArgumentNullException>(() => new UnitParseResult(true, null!, UnitErrorCode.None, "", -1, new UnitDefinition[0]));
        Assert.Throws<System.ArgumentException>(() => new UnitParseResult(true, meter, UnitErrorCode.ParseInvalidInput, "", -1, new UnitDefinition[0]));
        Assert.Throws<System.ArgumentException>(() => new UnitParseResult(true, meter, UnitErrorCode.None, "", 0, new UnitDefinition[0]));
    }

    [Test]
    public void Constructor_RejectsInvalidFailureState()
    {
        UnitDefinition meter = CreateMeter();

        Assert.Throws<System.ArgumentException>(() => new UnitParseResult(false, meter, UnitErrorCode.None, "Missing error code.", 0, new UnitDefinition[0]));
        Assert.Throws<System.ArgumentException>(() => new UnitParseResult(false, null, UnitErrorCode.ParseInvalidInput, "Missing position.", -1, new UnitDefinition[0]));
    }

    [Test]
    public void Constructor_RejectsInvalidCandidates()
    {
        UnitDefinition meter = CreateMeter();

        Assert.Throws<System.ArgumentNullException>(() => UnitParseResult.CreateFailure(UnitErrorCode.ParseUnknownUnit, "Missing.", 0, null!));
        Assert.Throws<System.ArgumentNullException>(() => UnitParseResult.CreateFailure(UnitErrorCode.ParseAmbiguousUnit, "Ambiguous.", 0, new UnitDefinition[] { meter, null! }));
    }

    static UnitDefinition CreateMeter()
    {
        return new UnitDefinition(
            "meter",
            "meter",
            "meters",
            "m",
            new string[0],
            DimensionVector.Length(),
            new RationalNumber(1, 1),
            new RationalNumber(0, 1),
            true,
            UnitConversionKind.Scale);
    }

    static UnitDefinition CreateMinute()
    {
        return new UnitDefinition(
            "minute",
            "minute",
            "minutes",
            "min",
            new string[0],
            DimensionVector.Time(),
            new RationalNumber(60, 1),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);
    }
}
