using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class QuantityFormatterTests
{
    [Test]
    public void Format_UsesDefaultGeneralInvariantSymbolOutput()
    {
        QuantityFormatter formatter = new QuantityFormatter();
        QuantityValue quantity = new QuantityValue(12.5d, new StandardUnits().Meter());

        string text = formatter.Format(quantity);

        Assert.That(text, Is.EqualTo("12.5 m"));
    }

    [Test]
    public void Format_UsesConfiguredNumericFormatAndCulture()
    {
        QuantityFormatter formatter = new QuantityFormatter();
        QuantityValue quantity = new QuantityValue(12.5d, new StandardUnits().Meter());
        QuantityFormatOptions options = QuantityFormatOptions.CreateDefault()
            .WithNumericFormat("F2")
            .WithCulture(System.Globalization.CultureInfo.GetCultureInfo("de-DE"));

        string text = formatter.FormatWithOptions(quantity, options);

        Assert.That(text, Is.EqualTo("12,50 m"));
    }

    [Test]
    public void Format_UsesConfiguredCultureWithNameStyle()
    {
        QuantityFormatter formatter = new QuantityFormatter();
        QuantityValue quantity = new QuantityValue(1.5d, new StandardUnits().Meter());
        QuantityFormatOptions options = QuantityFormatOptions.CreateDefault()
            .WithNumericFormat("F2")
            .WithCulture(System.Globalization.CultureInfo.GetCultureInfo("fr-FR"))
            .WithUnitFormatOptions(UnitFormatOptions.CreateName());

        string text = formatter.FormatWithOptions(quantity, options);

        Assert.That(text, Is.EqualTo("1,50 meters"));
    }

    [Test]
    public void Format_UsesConfiguredUnitFormat()
    {
        QuantityFormatter formatter = new QuantityFormatter();
        QuantityValue quantity = new QuantityValue(3.0d, new StandardUnits().Meter().Divide(new StandardUnits().Second()));

        string canonical = formatter.FormatWithOptions(quantity, QuantityFormatOptions.CreateDefault().WithUnitFormatOptions(UnitFormatOptions.CreateCanonical()));
        string decomposed = formatter.FormatWithOptions(quantity, QuantityFormatOptions.CreateDefault().WithUnitFormatOptions(UnitFormatOptions.CreateDecomposed()));

        Assert.That(canonical, Is.EqualTo("3 (meter/second)"));
        Assert.That(decomposed, Is.EqualTo("3 m/s"));
    }

    [Test]
    public void Format_UsesCanonicalOutputForStandardAndPrefixedUnits()
    {
        QuantityFormatter formatter = new QuantityFormatter();
        UnitRegistry registry = CreateRegistry();
        UnitDefinition kilometer = new UnitParser().ParseOrThrow("km", registry);
        QuantityFormatOptions options = QuantityFormatOptions.CreateDefault().WithUnitFormatOptions(UnitFormatOptions.CreateCanonical());

        string newtons = formatter.FormatWithOptions(new QuantityValue(4.0d, new StandardUnits().Newton()), options);
        string kilometers = formatter.FormatWithOptions(new QuantityValue(4.0d, kilometer), options);

        Assert.That(newtons, Is.EqualTo("4 newton"));
        Assert.That(kilometers, Is.EqualTo("4 kilometer"));
    }

    [Test]
    public void Format_UsesSingularAndPluralNamesForNameStyle()
    {
        QuantityFormatter formatter = new QuantityFormatter();
        QuantityFormatOptions options = QuantityFormatOptions.CreateDefault().WithUnitFormatOptions(UnitFormatOptions.CreateName());

        string singular = formatter.FormatWithOptions(new QuantityValue(1.0d, new StandardUnits().Meter()), options);
        string negativeSingular = formatter.FormatWithOptions(new QuantityValue(-1.0d, new StandardUnits().Meter()), options);
        string plural = formatter.FormatWithOptions(new QuantityValue(2.0d, new StandardUnits().Meter()), options);

        Assert.That(singular, Is.EqualTo("1 meter"));
        Assert.That(negativeSingular, Is.EqualTo("-1 meter"));
        Assert.That(plural, Is.EqualTo("2 meters"));
    }

    [Test]
    public void Format_SelectsPluralNamesByValueBeforeNumericFormatting()
    {
        QuantityFormatter formatter = new QuantityFormatter();
        QuantityFormatOptions options = QuantityFormatOptions.CreateDefault()
            .WithNumericFormat("F2")
            .WithUnitFormatOptions(UnitFormatOptions.CreateName());

        string roundedSingular = formatter.FormatWithOptions(new QuantityValue(1.0d, new StandardUnits().Meter()), options);
        string roundedPlural = formatter.FormatWithOptions(new QuantityValue(1.004d, new StandardUnits().Meter()), options);

        Assert.That(roundedSingular, Is.EqualTo("1.00 meter"));
        Assert.That(roundedPlural, Is.EqualTo("1.00 meters"));
    }

    [Test]
    public void Format_RejectsMissingInputs()
    {
        QuantityFormatter formatter = new QuantityFormatter();

        Assert.Throws<System.ArgumentNullException>(() => formatter.Format(null!));
        Assert.Throws<System.ArgumentNullException>(() => formatter.FormatWithOptions(new QuantityValue(1.0d, new StandardUnits().Meter()), null!));
    }

    static UnitRegistry CreateRegistry()
    {
        UnitRegistry registry = new UnitRegistry();

        new StandardUnits().RegisterSiBaseUnits(registry);
        new StandardUnits().RegisterSiDerivedUnits(registry);
        new StandardUnits().RegisterMetricUnits(registry);
        new StandardUnits().RegisterImperialAndUsCustomaryUnits(registry);
        new StandardUnits().RegisterDataUnits(registry);
        new StandardPrefixes().Register(registry);
        return registry;
    }
}
