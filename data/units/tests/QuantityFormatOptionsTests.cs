using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class QuantityFormatOptionsTests
{
    [Test]
    public void CreateDefault_UsesGeneralInvariantSymbolFormatting()
    {
        QuantityFormatOptions options = QuantityFormatOptions.CreateDefault();

        Assert.That(options.GetNumericFormat(), Is.EqualTo("G"));
        Assert.That(options.GetCulture(), Is.SameAs(System.Globalization.CultureInfo.InvariantCulture));
        Assert.That(options.GetUnitFormatOptions().GetStyle(), Is.EqualTo(UnitFormatStyle.Symbol));
    }

    [Test]
    public void Constructor_StoresNumericFormatCultureAndUnitFormat()
    {
        System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
        UnitFormatOptions unitFormatOptions = UnitFormatOptions.CreateName();

        QuantityFormatOptions options = new QuantityFormatOptions("F2", culture, unitFormatOptions);

        Assert.That(options.GetNumericFormat(), Is.EqualTo("F2"));
        Assert.That(options.GetCulture(), Is.SameAs(culture));
        Assert.That(options.GetUnitFormatOptions(), Is.SameAs(unitFormatOptions));
    }

    [Test]
    public void WithNumericFormat_ReturnsCopyWithDifferentNumericFormat()
    {
        QuantityFormatOptions options = new QuantityFormatOptions("G", System.Globalization.CultureInfo.InvariantCulture, UnitFormatOptions.CreateName());

        QuantityFormatOptions changed = options.WithNumericFormat("F3");

        Assert.That(changed.GetNumericFormat(), Is.EqualTo("F3"));
        Assert.That(changed.GetCulture(), Is.SameAs(options.GetCulture()));
        Assert.That(changed.GetUnitFormatOptions(), Is.SameAs(options.GetUnitFormatOptions()));
        Assert.That(options.GetNumericFormat(), Is.EqualTo("G"));
    }

    [Test]
    public void WithCulture_ReturnsCopyWithDifferentCulture()
    {
        QuantityFormatOptions options = QuantityFormatOptions.CreateDefault();
        System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.GetCultureInfo("de-DE");

        QuantityFormatOptions changed = options.WithCulture(culture);

        Assert.That(changed.GetCulture(), Is.SameAs(culture));
        Assert.That(changed.GetNumericFormat(), Is.EqualTo(options.GetNumericFormat()));
        Assert.That(changed.GetUnitFormatOptions(), Is.SameAs(options.GetUnitFormatOptions()));
        Assert.That(options.GetCulture(), Is.SameAs(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Test]
    public void WithUnitFormatOptions_ReturnsCopyWithDifferentUnitFormat()
    {
        QuantityFormatOptions options = QuantityFormatOptions.CreateDefault();
        UnitFormatOptions unitFormatOptions = UnitFormatOptions.CreateCanonical();

        QuantityFormatOptions changed = options.WithUnitFormatOptions(unitFormatOptions);

        Assert.That(changed.GetUnitFormatOptions(), Is.SameAs(unitFormatOptions));
        Assert.That(changed.GetNumericFormat(), Is.EqualTo(options.GetNumericFormat()));
        Assert.That(changed.GetCulture(), Is.SameAs(options.GetCulture()));
        Assert.That(options.GetUnitFormatOptions().GetStyle(), Is.EqualTo(UnitFormatStyle.Symbol));
    }

    [Test]
    public void Constructor_RejectsMissingValues()
    {
        Assert.Throws<System.ArgumentException>(() => new QuantityFormatOptions("", System.Globalization.CultureInfo.InvariantCulture, UnitFormatOptions.CreateDefault()));
        Assert.Throws<System.ArgumentException>(() => new QuantityFormatOptions(" ", System.Globalization.CultureInfo.InvariantCulture, UnitFormatOptions.CreateDefault()));
        Assert.Throws<System.ArgumentNullException>(() => new QuantityFormatOptions("G", null!, UnitFormatOptions.CreateDefault()));
        Assert.Throws<System.ArgumentNullException>(() => new QuantityFormatOptions("G", System.Globalization.CultureInfo.InvariantCulture, null!));
    }

    [Test]
    public void WithMethods_RejectMissingRequiredValues()
    {
        QuantityFormatOptions options = QuantityFormatOptions.CreateDefault();

        Assert.Throws<System.ArgumentException>(() => options.WithNumericFormat(""));
        Assert.Throws<System.ArgumentException>(() => options.WithNumericFormat(" "));
        Assert.Throws<System.ArgumentNullException>(() => options.WithCulture(null!));
        Assert.Throws<System.ArgumentNullException>(() => options.WithUnitFormatOptions(null!));
    }
}
