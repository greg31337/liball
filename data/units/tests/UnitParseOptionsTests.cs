using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class UnitParseOptionsTests
{
    [Test]
    public void CreateDefault_HasRelaxedWhitespaceAndNoContext()
    {
        UnitParseOptions options = UnitParseOptions.CreateDefault();

        Assert.That(options.IsStrict(), Is.False);
        Assert.That(options.HasQuantityKind(), Is.False);
        Assert.That(options.HasUnitSystem(), Is.False);
        Assert.Throws<System.InvalidOperationException>(() => options.GetQuantityKind());
        Assert.Throws<System.InvalidOperationException>(() => options.GetUnitSystem());
        Assert.That(options, Is.AssignableTo<IUnitParseOptions>());
    }

    [Test]
    public void ConstructorAndWithMethods_PreserveIndependentContext()
    {
        QuantityKindDefinition length = LengthKind();
        UnitSystemDefinition system = MetricSystem();
        UnitParseOptions options = new UnitParseOptions(false, length, system);

        UnitParseOptions strict = options.WithStrict(true);
        UnitParseOptions withoutKind = strict.WithoutQuantityKind();
        UnitParseOptions withoutSystem = strict.WithoutUnitSystem();

        Assert.That(options.GetQuantityKind(), Is.SameAs(length));
        Assert.That(options.GetUnitSystem(), Is.SameAs(system));
        Assert.That(strict.IsStrict(), Is.True);
        Assert.That(strict.GetQuantityKind(), Is.SameAs(length));
        Assert.That(strict.GetUnitSystem(), Is.SameAs(system));
        Assert.That(withoutKind.HasQuantityKind(), Is.False);
        Assert.That(withoutKind.GetUnitSystem(), Is.SameAs(system));
        Assert.That(withoutSystem.GetQuantityKind(), Is.SameAs(length));
        Assert.That(withoutSystem.HasUnitSystem(), Is.False);
    }

    [Test]
    public void WithContextMethods_RejectMissingValues()
    {
        UnitParseOptions options = UnitParseOptions.CreateDefault();

        Assert.Throws<System.ArgumentNullException>(() => options.WithQuantityKind(null!));
        Assert.Throws<System.ArgumentNullException>(() => options.WithUnitSystem(null!));
    }

    static QuantityKindDefinition LengthKind()
    {
        return new QuantityKindDefinition("length", "Length", DimensionVector.Length(), "meter", new string[0], new string[] { "foot" });
    }

    static UnitSystemDefinition MetricSystem()
    {
        return new UnitSystemDefinition("metric", "Metric", new string[] { "meter" }, new string[] { "length" }, new string[] { "meter" });
    }
}
