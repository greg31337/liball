using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class StandardRoundTripTests
{
    [Test]
    public void StandardUnits_ParseFormattedSymbolsAndCanonicalText()
    {
        UnitRegistry registry = new StandardRegistry().Create();
        UnitParser parser = new UnitParser();
        UnitFormatter formatter = new UnitFormatter();
        UnitDefinition[] units = registry.GetUnits();
        int index = 0;

        while (index < units.Length)
        {
            UnitDefinition unit = units[index];
            UnitDefinition symbolRoundTrip = parser.ParseOrThrow(formatter.FormatWithOptions(unit, UnitFormatOptions.CreateSymbol()), registry);
            UnitDefinition canonicalRoundTrip = parser.ParseOrThrow(formatter.FormatWithOptions(unit, UnitFormatOptions.CreateCanonical()), registry);

            Assert.That(symbolRoundTrip.IsEquivalentTo(unit), Is.True);
            Assert.That(canonicalRoundTrip.GetId(), Is.EqualTo(unit.GetId()));
            Assert.That(canonicalRoundTrip.IsEquivalentTo(unit), Is.True);

            index = index + 1;
        }
    }

    [Test]
    public void StandardUnits_FormatNamesAndParseDecomposedDimensions()
    {
        UnitRegistry registry = new StandardRegistry().Create();
        UnitParser parser = new UnitParser();
        UnitFormatter formatter = new UnitFormatter();
        UnitDefinition[] units = registry.GetUnits();
        int index = 0;

        while (index < units.Length)
        {
            UnitDefinition unit = units[index];
            string nameText = formatter.FormatWithOptions(unit, UnitFormatOptions.CreateName());
            string decomposedText = formatter.FormatWithOptions(unit, UnitFormatOptions.CreateDecomposed());
            UnitDefinition decomposed = parser.ParseOrThrow(decomposedText, registry);

            Assert.That(string.IsNullOrWhiteSpace(nameText), Is.False);
            Assert.That(decomposed.GetDimension().EqualsDimension(unit.GetDimension()), Is.True);

            index = index + 1;
        }
    }

    [Test]
    public void StandardQuantities_FormatAndParseBackFromDefaultText()
    {
        UnitRegistry registry = new StandardRegistry().Create();
        QuantityFormatter formatter = new QuantityFormatter();
        QuantityParser parser = QuantityParser.CreateDefault();
        UnitDefinition[] units = registry.GetUnits();
        int index = 0;

        while (index < units.Length)
        {
            UnitDefinition unit = units[index];
            QuantityValue quantity = new QuantityValue(1.25d, unit);
            string text = formatter.Format(quantity);
            QuantityValue roundTrip = parser.ParseOrThrow(text, registry);

            Assert.That(roundTrip.GetValue(), Is.EqualTo(1.25d));
            Assert.That(roundTrip.GetUnit().IsEquivalentTo(unit), Is.True);

            index = index + 1;
        }
    }

    [Test]
    public void StandardRegistry_RoundTripsRepresentativePrefixedAndCompoundUnits()
    {
        UnitRegistry registry = new StandardRegistry().Create();
        QuantityParser quantityParser = QuantityParser.CreateDefault();
        QuantityFormatter quantityFormatter = new QuantityFormatter();
        UnitFormatter unitFormatter = new UnitFormatter();

        QuantityValue speed = quantityParser.ParseOrThrow("1.25 km/s", registry);
        string formattedSpeed = quantityFormatter.Format(speed);
        QuantityValue speedRoundTrip = quantityParser.ParseOrThrow(formattedSpeed, registry);

        Assert.That(formattedSpeed, Is.EqualTo("1.25 km/s"));
        Assert.That(speedRoundTrip.GetValue(), Is.EqualTo(1.25d));
        Assert.That(speedRoundTrip.GetUnit().IsEquivalentTo(speed.GetUnit()), Is.True);
        Assert.That(unitFormatter.FormatWithOptions(speed.GetUnit(), UnitFormatOptions.CreateCanonical()), Is.EqualTo("(kilometer/second)"));
    }
}
