using fake;

namespace fake.Tests;

[TestFixture]
public class FinanceCurrencyTests
{
    [Test]
    public void CurrencyCatalogExposesDatedSourceMetadata()
    {
        CurrencyCatalogMetadata metadata = Fake.Create(9801).Finance().CurrencyMetadata();

        Assert.Multiple(() =>
        {
            Assert.That(metadata.Source, Is.EqualTo("SIX ISO 4217 List One"));
            Assert.That(metadata.SourceUrl, Does.StartWith("https://www.six-group.com/"));
            Assert.That(metadata.SourceVersion, Is.EqualTo("published 2026-01-01"));
            Assert.That(metadata.PublishedOn, Is.EqualTo("2026-01-01"));
            Assert.That(metadata.RetrievedOn, Is.EqualTo("2026-08-14"));
            Assert.That(metadata.RecordCount, Is.EqualTo(178));
        });
    }

    [Test]
    public void CurrencyLookupPreservesNumericCodesAndMinorUnitExceptions()
    {
        FinanceProvider finance = Fake.Create(9802).Finance();
        CurrencyRecord lek = finance.CurrencyFromCode("all");
        CurrencyRecord yen = finance.CurrencyFromCode("JPY");
        CurrencyRecord dinar = finance.CurrencyFromCode("KWD");
        CurrencyRecord unit = finance.CurrencyFromCode("CLF");
        CurrencyRecord gold = finance.CurrencyFromCode("XAU");

        Assert.Multiple(() =>
        {
            Assert.That(lek.NumericCode, Is.EqualTo("008"));
            Assert.That(lek.Name, Is.EqualTo("Lek"));
            Assert.That(lek.MinorUnits, Is.EqualTo(2));
            Assert.That(lek.HasMinorUnits, Is.True);
            Assert.That(yen.MinorUnits, Is.EqualTo(0));
            Assert.That(yen.HasMinorUnits, Is.True);
            Assert.That(dinar.MinorUnits, Is.EqualTo(3));
            Assert.That(unit.MinorUnits, Is.EqualTo(4));
            Assert.That(gold.HasMinorUnits, Is.False);
            Assert.That(gold.RegistryVersion, Is.EqualTo("published 2026-01-01"));
        });
    }

    [Test]
    public void NumericLookupReturnsTheCorrelatedCurrencyRecord()
    {
        FinanceProvider finance = Fake.Create(9803).Finance();

        Assert.Multiple(() =>
        {
            AssertEquivalent(finance.CurrencyFromCode("ALL"), finance.CurrencyByNumericCode("008"));
            AssertEquivalent(finance.CurrencyFromCode("USD"), finance.CurrencyByNumericCode("840"));
            AssertEquivalent(finance.CurrencyFromCode("XXX"), finance.CurrencyByNumericCode("999"));
        });
    }

    [Test]
    public void DeduplicatedCurrenciesPreserveEveryEntityMapping()
    {
        FinanceProvider finance = Fake.Create(9804).Finance();
        CurrencyRecord euro = finance.CurrencyFromCode("EUR");
        CurrencyRecord rand = finance.CurrencyFromCode("ZAR");
        CurrencyRecord[] all = finance.Currencies();

        Assert.Multiple(() =>
        {
            Assert.That(euro.Entities, Does.Contain("FRANCE"));
            Assert.That(euro.Entities, Does.Contain("GERMANY"));
            Assert.That(euro.Entities, Does.Contain("ÅLAND ISLANDS"));
            Assert.That(euro.Entities.Length, Is.GreaterThan(20));
            Assert.That(rand.Entities,
                Is.EqualTo(new string[] { "LESOTHO", "NAMIBIA", "SOUTH AFRICA" }));
            Assert.That(all.Select(value => value.Code), Is.Ordered);
            Assert.That(all.Select(value => value.Code).Distinct().Count(), Is.EqualTo(178));
            Assert.That(all.Select(value => value.NumericCode).Distinct().Count(), Is.EqualTo(178));
        });
    }

    [Test]
    public void CurrencySelectionIsDeterministicAndScalarAliasesStayCorrelated()
    {
        const long seed = 9805;
        CurrencyRecord first = Fake.Create(seed).Finance().Currency();
        CurrencyRecord second = Fake.Create(seed).Finance().Currency();
        string code = Fake.Create(seed).Finance().CurrencyCode();
        string name = Fake.Create(seed).Finance().CurrencyName();

        Assert.Multiple(() =>
        {
            AssertEquivalent(first, second);
            Assert.That(code, Is.EqualTo(first.Code));
            Assert.That(name, Is.EqualTo(first.Name));
        });
    }

    [Test]
    public void CurrencyResultsAndNestedEntityArraysAreIsolated()
    {
        FinanceProvider finance = Fake.Create(9806).Finance();
        CurrencyRecord first = finance.CurrencyFromCode("EUR");
        CurrencyRecord second = finance.CurrencyFromCode("EUR");
        CurrencyRecord[] catalog = finance.Currencies();
        int euroIndex = Array.FindIndex(catalog, value => value.Code == "EUR");

        first.Name = "changed";
        first.Entities[0] = "changed";
        catalog[euroIndex].Entities[0] = "changed again";

        Assert.Multiple(() =>
        {
            Assert.That(second.Name, Is.EqualTo("Euro"));
            Assert.That(second.Entities[0], Is.Not.EqualTo("changed"));
            Assert.That(finance.CurrencyFromCode("EUR").Entities[0], Is.Not.EqualTo("changed again"));
        });
    }

    [Test]
    public void CurrencyApisRejectInvalidLookupsAndHonorConfiguredLimits()
    {
        FinanceProvider finance = Fake.Create(9807).Finance();
        FakeOptions countOptions = FakeOptions.Create(9807);
        countOptions.MaximumCount = 177;
        FakeOptions outputOptions = FakeOptions.Create(9807);
        outputOptions.MaximumOutputSize = 2;

        Assert.That(() => finance.CurrencyFromCode(null), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => finance.CurrencyFromCode(" "), Throws.TypeOf<ArgumentException>());
        Assert.That(() => finance.CurrencyFromCode("US"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => finance.CurrencyFromCode("ZZZ"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => finance.CurrencyByNumericCode(null), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => finance.CurrencyByNumericCode("08"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => finance.CurrencyByNumericCode("000"), Throws.TypeOf<ArgumentException>());
        Assert.That(
            () => Fake.Create(countOptions).Finance().Currencies(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(
            () => Fake.Create(outputOptions).Finance().CurrencyCode(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(
            () => Fake.Create(outputOptions).Finance().CurrencyFromCode("USD"),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(
            () => Fake.Create(outputOptions).Finance().CurrencyMetadata(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    static void AssertEquivalent(CurrencyRecord actual, CurrencyRecord expected)
    {
        Assert.That(actual.Code, Is.EqualTo(expected.Code));
        Assert.That(actual.NumericCode, Is.EqualTo(expected.NumericCode));
        Assert.That(actual.MinorUnits, Is.EqualTo(expected.MinorUnits));
        Assert.That(actual.HasMinorUnits, Is.EqualTo(expected.HasMinorUnits));
        Assert.That(actual.Name, Is.EqualTo(expected.Name));
        Assert.That(actual.Entities, Is.EqualTo(expected.Entities));
        Assert.That(actual.RegistryVersion, Is.EqualTo(expected.RegistryVersion));
    }
}
