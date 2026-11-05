using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class QuantityParseOptionsTests
{
    [Test]
    public void CreateDefault_UsesInvariantCultureNonStrictAndNoQuantityKind()
    {
        QuantityParseOptions options = QuantityParseOptions.CreateDefault();

        Assert.That(options.GetCulture(), Is.SameAs(System.Globalization.CultureInfo.InvariantCulture));
        Assert.That(options.IsStrict(), Is.False);
        Assert.That(options.HasQuantityKind(), Is.False);
        Assert.That(options.HasUnitSystem(), Is.False);
        Assert.Throws<System.InvalidOperationException>(() => options.GetQuantityKind());
        Assert.Throws<System.InvalidOperationException>(() => options.GetUnitSystem());
    }

    [Test]
    public void Constructor_StoresCultureStrictnessAndQuantityKind()
    {
        System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
        QuantityKindDefinition length = CreateLengthKind();

        QuantityParseOptions options = new QuantityParseOptions(culture, true, length, null);

        Assert.That(options.GetCulture(), Is.SameAs(culture));
        Assert.That(options.IsStrict(), Is.True);
        Assert.That(options.HasQuantityKind(), Is.True);
        Assert.That(options.GetQuantityKind(), Is.SameAs(length));
    }

    [Test]
    public void WithCulture_ReturnsCopyWithDifferentCulture()
    {
        QuantityParseOptions options = new QuantityParseOptions(System.Globalization.CultureInfo.InvariantCulture, true, CreateLengthKind(), null);
        System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.GetCultureInfo("de-DE");

        QuantityParseOptions changed = options.WithCulture(culture);

        Assert.That(changed.GetCulture(), Is.SameAs(culture));
        Assert.That(changed.IsStrict(), Is.True);
        Assert.That(changed.GetQuantityKind(), Is.SameAs(options.GetQuantityKind()));
        Assert.That(options.GetCulture(), Is.SameAs(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Test]
    public void WithStrict_ReturnsCopyWithDifferentStrictness()
    {
        QuantityKindDefinition length = CreateLengthKind();
        QuantityParseOptions options = new QuantityParseOptions(System.Globalization.CultureInfo.InvariantCulture, false, length, null);

        QuantityParseOptions changed = options.WithStrict(true);

        Assert.That(changed.GetCulture(), Is.SameAs(System.Globalization.CultureInfo.InvariantCulture));
        Assert.That(changed.IsStrict(), Is.True);
        Assert.That(changed.GetQuantityKind(), Is.SameAs(length));
        Assert.That(options.IsStrict(), Is.False);
    }

    [Test]
    public void WithQuantityKindAndWithoutQuantityKind_ControlNarrowing()
    {
        QuantityParseOptions options = QuantityParseOptions.CreateDefault();
        QuantityKindDefinition length = CreateLengthKind();

        QuantityParseOptions narrowed = options.WithQuantityKind(length);
        QuantityParseOptions cleared = narrowed.WithoutQuantityKind();

        Assert.That(narrowed.HasQuantityKind(), Is.True);
        Assert.That(narrowed.GetQuantityKind(), Is.SameAs(length));
        Assert.That(cleared.HasQuantityKind(), Is.False);
        Assert.That(cleared.GetCulture(), Is.SameAs(narrowed.GetCulture()));
        Assert.That(cleared.IsStrict(), Is.EqualTo(narrowed.IsStrict()));
        Assert.Throws<System.InvalidOperationException>(() => cleared.GetQuantityKind());
    }

    [Test]
    public void WithUnitSystemAndWithoutUnitSystem_ControlNarrowing()
    {
        QuantityParseOptions options = QuantityParseOptions.CreateDefault().WithQuantityKind(CreateLengthKind());
        UnitSystemDefinition system = new UnitSystemDefinition("metric", "Metric", new string[] { "meter" }, new string[] { "length" }, new string[] { "meter" });

        QuantityParseOptions narrowed = options.WithUnitSystem(system);
        QuantityParseOptions cleared = narrowed.WithoutUnitSystem();

        Assert.That(narrowed.HasUnitSystem(), Is.True);
        Assert.That(narrowed.GetUnitSystem(), Is.SameAs(system));
        Assert.That(narrowed.GetQuantityKind(), Is.SameAs(options.GetQuantityKind()));
        Assert.That(cleared.HasUnitSystem(), Is.False);
        Assert.That(cleared.GetQuantityKind(), Is.SameAs(options.GetQuantityKind()));
        Assert.Throws<System.InvalidOperationException>(() => cleared.GetUnitSystem());
    }

    [Test]
    public void Constructor_RejectsMissingCulture()
    {
        Assert.Throws<System.ArgumentNullException>(() => new QuantityParseOptions(null!, false, null, null));
    }

    [Test]
    public void WithMethods_RejectMissingRequiredValues()
    {
        QuantityParseOptions options = QuantityParseOptions.CreateDefault();

        Assert.Throws<System.ArgumentNullException>(() => options.WithCulture(null!));
        Assert.Throws<System.ArgumentNullException>(() => options.WithQuantityKind(null!));
        Assert.Throws<System.ArgumentNullException>(() => options.WithUnitSystem(null!));
    }

    static QuantityKindDefinition CreateLengthKind()
    {
        return new QuantityKindDefinition(
            "length",
            "length",
            new StandardDimensions().Length(),
            "meter",
            new string[0],
            new string[] { "meter", "foot" });
    }
}
