using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class ParserEdgeBehaviorTests
{
    [Test]
    public void QuantityParser_ParsesCompactUnitBeginningWithExponentLetterThroughBothEntryPoints()
    {
        IQuantityParser parser = QuantityParser.CreateDefault();
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition each = Unit("each", "each", false);
        registry.AddUnit(each);

        QuantityParseResult result = parser.Parse("2each", registry);
        QuantityValue throwing = parser.ParseOrThrow("2each", registry);

        Assert.That(result.IsSuccess(), Is.True, result.GetMessage());
        Assert.That(result.GetQuantity().GetValue(), Is.EqualTo(2.0d));
        Assert.That(result.GetQuantity().GetUnit(), Is.SameAs(each));
        Assert.That(throwing.GetValue(), Is.EqualTo(2.0d));
        Assert.That(throwing.GetUnit(), Is.SameAs(each));
    }

    [Test]
    public void UnitParser_PrefixesOnlyEligibleCandidateFromAmbiguousBaseText()
    {
        IUnitParser parser = new UnitParser();
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition prefixable = Unit("prefixable", "u", true);
        UnitDefinition fixedUnit = Unit("fixed", "u", false);
        registry.AddPrefix(new PrefixDefinition("kilo", "kilo", "k", new RationalNumber(1000, 1)));
        registry.AddUnit(prefixable);
        registry.AddUnit(fixedUnit);

        UnitParseResult result = parser.Parse("ku", registry);
        UnitDefinition throwing = parser.ParseOrThrow("ku", registry);

        Assert.That(result.IsSuccess(), Is.True, result.GetMessage());
        Assert.That(result.GetUnit().GetId(), Is.EqualTo("kilo_prefixable"));
        Assert.That(result.GetUnit().GetScaleToSystemUnit().EqualsValue(new RationalNumber(1000, 1)), Is.True);
        Assert.That(throwing.GetId(), Is.EqualTo("kilo_prefixable"));
    }

    [Test]
    public void Parsers_RejectNonAsciiDigitsWithoutEscapingNonThrowingEntryPoints()
    {
        IUnitParser unitParser = new UnitParser();
        IQuantityParser quantityParser = QuantityParser.CreateDefault();
        UnitRegistry registry = new UnitRegistry();
        registry.AddUnit(Unit("meter", "m", true));

        UnitParseResult unitResult = unitParser.Parse("m^\u0662", registry);
        QuantityParseResult quantityResult = quantityParser.Parse("\u0661 m", registry);

        Assert.That(unitResult.IsFailure(), Is.True);
        Assert.That(unitResult.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseInvalidInput));
        Assert.That(unitResult.GetPosition(), Is.EqualTo(2));
        Assert.That(quantityResult.IsFailure(), Is.True);
        Assert.That(quantityResult.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseInvalidInput));
        Assert.That(quantityResult.GetPosition(), Is.EqualTo(0));
        Assert.Throws<System.ArgumentException>(() => unitParser.ParseOrThrow("m^\u0662", registry));
        Assert.Throws<System.ArgumentException>(() => quantityParser.ParseOrThrow("\u0661 m", registry));
    }

    static UnitDefinition Unit(string id, string symbol, bool prefixable)
    {
        return new UnitDefinition(
            id,
            id,
            id,
            symbol,
            new string[0],
            DimensionVector.Length(),
            new RationalNumber(1, 1),
            new RationalNumber(0, 1),
            prefixable,
            UnitConversionKind.Scale);
    }
}
