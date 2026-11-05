using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class CanonicalInterchangeGrammarTests
{
    [Test]
    public void CanonicalFormatter_EscapesCustomIdsAndRoundTripsCompoundExpressions()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition custom = CustomUnit("widget/2", "wu", new StandardDimensions().Length(), new RationalNumber(3, 1));
        UnitDefinition second = new StandardUnits().Second();
        registry.AddUnit(custom);
        registry.AddUnit(second);
        UnitDefinition compound = custom.Divide(second);
        UnitFormatter formatter = new UnitFormatter();

        string atomText = formatter.FormatWithOptions(custom, UnitFormatOptions.CreateCanonical());
        string compoundText = formatter.FormatWithOptions(compound, UnitFormatOptions.CreateCanonical());
        UnitDefinition parsed = new UnitParser().ParseOrThrowWithOptions(compoundText, registry, new UnitParseOptions(true, null, null));

        Assert.That(atomText, Is.EqualTo("[007700690064006700650074002F0032]"));
        Assert.That(compoundText, Is.EqualTo("([007700690064006700650074002F0032]/second)"));
        Assert.That(parsed.IsEquivalentTo(compound), Is.True);
    }

    [Test]
    public void CanonicalFormatter_RoundTripsDynamicallyPrefixedUnitsByStableIds()
    {
        UnitRegistry registry = new StandardRegistry().Create();
        UnitParser parser = new UnitParser();
        UnitDefinition millisecond = parser.ParseOrThrow("ms", registry);
        UnitDefinition rate = millisecond.Divide(new StandardUnits().Meter());
        UnitFormatter formatter = new UnitFormatter();

        string millisecondText = formatter.FormatWithOptions(millisecond, UnitFormatOptions.CreateCanonical());
        string rateText = formatter.FormatWithOptions(rate, UnitFormatOptions.CreateCanonical());
        UnitDefinition millisecondRoundTrip = parser.ParseOrThrowWithOptions(millisecondText, registry, new UnitParseOptions(true, null, null));
        UnitDefinition rateRoundTrip = parser.ParseOrThrowWithOptions(rateText, registry, new UnitParseOptions(true, null, null));

        Assert.That(millisecondText, Is.EqualTo("milli_second"));
        Assert.That(rateText, Is.EqualTo("(milli_second/meter)"));
        Assert.That(millisecondRoundTrip.IsEquivalentTo(millisecond), Is.True);
        Assert.That(rateRoundTrip.IsEquivalentTo(rate), Is.True);
    }

    [Test]
    public void CanonicalFormatter_RoundTripsExactUnitRoots()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition square = CustomUnit("four_square_meters", "4m2", new StandardDimensions().Length().Pow(2), new RationalNumber(4, 1));
        registry.AddUnit(square);
        UnitDefinition root = square.Root(2).GetUnit();

        string text = new UnitFormatter().FormatWithOptions(root, UnitFormatOptions.CreateCanonical());
        UnitParseResult result = new UnitParser().ParseWithOptions(text, registry, new UnitParseOptions(true, null, null));

        Assert.That(text, Is.EqualTo("(four_square_meters^1/2)"));
        Assert.That(result.IsSuccess(), Is.True, result.GetMessage());
        Assert.That(result.GetUnit().IsEquivalentTo(root), Is.True);
        Assert.That(result.GetUnit().GetScaleToSystemUnit().EqualsValue(new RationalNumber(2, 1)), Is.True);
    }

    [Test]
    public void CanonicalParser_RejectsMalformedEscapesAndInvalidRootsWithoutThrowing()
    {
        UnitRegistry registry = new StandardRegistry().Create();
        UnitParser parser = new UnitParser();

        UnitParseResult malformedEscape = parser.ParseWithOptions("[00XZ]", registry, new UnitParseOptions(true, null, null));
        UnitParseResult zeroRoot = parser.ParseWithOptions("(meter^1/0)", registry, new UnitParseOptions(true, null, null));
        UnitParseResult inexactRoot = parser.ParseWithOptions("(meter^1/2)", registry, new UnitParseOptions(true, null, null));

        Assert.That(malformedEscape.IsFailure(), Is.True);
        Assert.That(malformedEscape.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseInvalidInput));
        Assert.That(zeroRoot.IsFailure(), Is.True);
        Assert.That(zeroRoot.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseInvalidInput));
        Assert.That(inexactRoot.IsFailure(), Is.True);
        Assert.That(inexactRoot.GetErrorCode(), Is.EqualTo(UnitErrorCode.ArithmeticInvalidOperation));
    }

    [Test]
    public void CanonicalParser_PrefersStableUnitIdOverOtherLookupText()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition byId = CustomUnit("token", "x", new StandardDimensions().Length(), new RationalNumber(1, 1));
        UnitDefinition bySymbol = CustomUnit("other", "token", new StandardDimensions().Time(), new RationalNumber(1, 1));
        registry.AddUnit(byId);
        registry.AddUnit(bySymbol);

        UnitDefinition parsed = new UnitParser().ParseOrThrowWithOptions("token", registry, new UnitParseOptions(true, null, null));

        Assert.That(parsed, Is.SameAs(byId));
    }

    static UnitDefinition CustomUnit(string id, string symbol, DimensionVector dimension, RationalNumber scale)
    {
        return new UnitDefinition(
            id,
            id,
            id,
            symbol,
            new string[0],
            dimension,
            scale,
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);
    }
}
