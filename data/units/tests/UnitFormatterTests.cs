using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class UnitFormatterTests
{
    [Test]
    public void Format_UsesSymbolStyleByDefault()
    {
        UnitFormatter formatter = new UnitFormatter();

        string text = formatter.Format(new StandardUnits().Meter());

        Assert.That(text, Is.EqualTo("m"));
    }

    [Test]
    public void Format_UsesSelectedMetadataStyles()
    {
        UnitFormatter formatter = new UnitFormatter();
        UnitDefinition joule = new StandardUnits().Joule();
        UnitDefinition speed = new StandardUnits().Meter().Divide(new StandardUnits().Second());

        Assert.That(formatter.FormatWithOptions(joule, UnitFormatOptions.CreateSymbol()), Is.EqualTo("J"));
        Assert.That(formatter.FormatWithOptions(joule, UnitFormatOptions.CreateName()), Is.EqualTo("joule"));
        Assert.That(formatter.FormatWithOptions(joule, UnitFormatOptions.CreateCanonical()), Is.EqualTo("joule"));
        Assert.That(formatter.FormatWithOptions(speed, UnitFormatOptions.CreateSymbol()), Is.EqualTo("m/s"));
        Assert.That(formatter.FormatWithOptions(speed, UnitFormatOptions.CreateName()), Is.EqualTo("meter per second"));
        Assert.That(formatter.FormatWithOptions(speed, UnitFormatOptions.CreateCanonical()), Is.EqualTo("(meter/second)"));
    }

    [Test]
    public void Format_DecomposesDerivedUnitsInCanonicalDimensionOrder()
    {
        UnitFormatter formatter = new UnitFormatter();

        string text = formatter.FormatWithOptions(new StandardUnits().Newton(), UnitFormatOptions.CreateDecomposed());

        Assert.That(text, Is.EqualTo("m*kg/s^2"));
    }

    [Test]
    public void Format_DecomposedCompoundUnitsUseDeterministicOrdering()
    {
        UnitFormatter formatter = new UnitFormatter();
        UnitDefinition meter = new StandardUnits().Meter();
        UnitDefinition kilogram = new StandardUnits().Kilogram();
        UnitDefinition secondSquared = new StandardUnits().Second().Pow(2);
        UnitDefinition left = meter.Multiply(kilogram).Divide(secondSquared);
        UnitDefinition right = kilogram.Multiply(meter).Divide(secondSquared);

        string leftText = formatter.FormatWithOptions(left, UnitFormatOptions.CreateDecomposed());
        string rightText = formatter.FormatWithOptions(right, UnitFormatOptions.CreateDecomposed());

        Assert.That(left.GetSymbol(), Is.Not.EqualTo(right.GetSymbol()));
        Assert.That(leftText, Is.EqualTo("m*kg/s^2"));
        Assert.That(rightText, Is.EqualTo("m*kg/s^2"));
    }

    [Test]
    public void Format_DecomposedUnitsGroupMultipleDenominatorFactors()
    {
        UnitFormatter formatter = new UnitFormatter();
        UnitDefinition unit = new StandardUnits().Meter().Divide(new StandardUnits().Kilogram().Multiply(new StandardUnits().Second()));

        string text = formatter.FormatWithOptions(unit, UnitFormatOptions.CreateDecomposed());

        Assert.That(text, Is.EqualTo("m/(kg*s)"));
    }

    [Test]
    public void Format_DecomposedUnitsHandleInverseAndDimensionlessUnits()
    {
        UnitFormatter formatter = new UnitFormatter();
        UnitDefinition inverseSecond = new StandardUnits().Second().Inverse();
        UnitDefinition dimensionless = new StandardUnits().Meter().Divide(new StandardUnits().Meter());

        Assert.That(formatter.FormatWithOptions(inverseSecond, UnitFormatOptions.CreateDecomposed()), Is.EqualTo("1/s"));
        Assert.That(formatter.FormatWithOptions(dimensionless, UnitFormatOptions.CreateDecomposed()), Is.EqualTo("1"));
    }

    [Test]
    public void Format_DecomposedUnitsUseCanonicalOrderForAllDimensionSlots()
    {
        UnitFormatter formatter = new UnitFormatter();
        UnitDefinition numerator = new UnitDefinition("all_positive", "all positive", "all positive", "all+", new string[0], new DimensionVector(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1), new RationalNumber(1, 1), new RationalNumber(0, 1), false, UnitConversionKind.Scale);
        UnitDefinition denominator = new UnitDefinition("all_negative", "all negative", "all negative", "all-", new string[0], new DimensionVector(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1), new RationalNumber(1, 1), new RationalNumber(0, 1), false, UnitConversionKind.Scale);

        string numeratorText = formatter.FormatWithOptions(numerator, UnitFormatOptions.CreateDecomposed());
        string denominatorText = formatter.FormatWithOptions(denominator, UnitFormatOptions.CreateDecomposed());

        Assert.That(numeratorText, Is.EqualTo("m*kg*s*A*K*mol*cd*rad*sr*bit*currency*count"));
        Assert.That(denominatorText, Is.EqualTo("1/(m*kg*s*A*K*mol*cd*rad*sr*bit*currency*count)"));
    }

    [Test]
    public void Format_RejectsMissingInputs()
    {
        UnitFormatter formatter = new UnitFormatter();

        Assert.Throws<System.ArgumentNullException>(() => formatter.Format(null!));
        Assert.Throws<System.ArgumentNullException>(() => formatter.FormatWithOptions(new StandardUnits().Meter(), null!));
    }
}
