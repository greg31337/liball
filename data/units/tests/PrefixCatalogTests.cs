using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class PrefixCatalogTests
{
    [Test]
    public void CreateSi_ReturnsAllPrefixesFromQuettaThroughQuecto()
    {
        PrefixCatalog catalog = PrefixCatalog.CreateSi();
        PrefixDefinition[] prefixes = catalog.GetPrefixes();

        Assert.That(catalog.GetCount(), Is.EqualTo(24));
        Assert.That(prefixes[0].GetId(), Is.EqualTo("quetta"));
        Assert.That(prefixes[0].GetSymbol(), Is.EqualTo("Q"));
        Assert.That(prefixes[23].GetId(), Is.EqualTo("quecto"));
        Assert.That(prefixes[23].GetSymbol(), Is.EqualTo("q"));
    }

    [Test]
    public void CreateSi_StoresExactDecimalFactorsForLargeAndSmallPrefixes()
    {
        PrefixCatalog catalog = PrefixCatalog.CreateSi();

        AssertFactor(catalog.FindById("quetta")!, "1000000000000000000000000000000", "1");
        AssertFactor(catalog.FindById("ronna")!, "1000000000000000000000000000", "1");
        AssertFactor(catalog.FindById("kilo")!, "1000", "1");
        AssertFactor(catalog.FindById("micro")!, "1", "1000000");
        AssertFactor(catalog.FindById("ronto")!, "1", "1000000000000000000000000000");
        AssertFactor(catalog.FindById("quecto")!, "1", "1000000000000000000000000000000");
    }

    [Test]
    public void FindMethods_LookUpPrefixesByIdNameAndSymbol()
    {
        PrefixCatalog catalog = PrefixCatalog.CreateSi();

        Assert.That(catalog.FindById("mega")!.GetSymbol(), Is.EqualTo("M"));
        Assert.That(catalog.FindByName("centi")!.GetSymbol(), Is.EqualTo("c"));
        Assert.That(catalog.FindBySymbol("da")!.GetId(), Is.EqualTo("deca"));
        Assert.That(catalog.FindBySymbol("\u03bc")!.GetId(), Is.EqualTo("micro"));
    }

    [Test]
    public void FindMethods_ReturnNullForUnknownPrefix()
    {
        PrefixCatalog catalog = PrefixCatalog.CreateSi();

        Assert.That(catalog.FindById("unknown"), Is.Null);
        Assert.That(catalog.FindByName("unknown"), Is.Null);
        Assert.That(catalog.FindBySymbol("?"), Is.Null);
        Assert.That(catalog.FindById(null), Is.Null);
    }

    [Test]
    public void GetPrefixes_ReturnsDefensiveCopy()
    {
        PrefixCatalog catalog = PrefixCatalog.CreateSi();
        PrefixDefinition[] prefixes = catalog.GetPrefixes();

        prefixes[0] = new PrefixDefinition("changed", "changed", "x", new RationalNumber(1, 1));

        Assert.That(catalog.GetPrefixes()[0].GetId(), Is.EqualTo("quetta"));
    }

    [Test]
    public void Constructor_RejectsNullEntriesAndDuplicateLookups()
    {
        Assert.Throws<System.ArgumentNullException>(() => new PrefixCatalog(null!));
        Assert.Throws<System.ArgumentNullException>(() => new PrefixCatalog(new PrefixDefinition[] { null! }));
        Assert.Throws<System.ArgumentException>(
            () => new PrefixCatalog(
                new PrefixDefinition[]
                {
                    new PrefixDefinition("kilo", "kilo", "k", new RationalNumber(1000, 1)),
                    new PrefixDefinition("kilo", "other", "x", new RationalNumber(1000, 1))
                }));
        Assert.Throws<System.ArgumentException>(
            () => new PrefixCatalog(
                new PrefixDefinition[]
                {
                    new PrefixDefinition("kilo", "kilo", "k", new RationalNumber(1000, 1)),
                    new PrefixDefinition("other", "kilo", "x", new RationalNumber(1000, 1))
                }));
        Assert.Throws<System.ArgumentException>(
            () => new PrefixCatalog(
                new PrefixDefinition[]
                {
                    new PrefixDefinition("kilo", "kilo", "k", new RationalNumber(1000, 1)),
                    new PrefixDefinition("other", "other", "k", new RationalNumber(1000, 1))
                }));
    }

    static void AssertFactor(PrefixDefinition prefix, string numerator, string denominator)
    {
        Assert.That(prefix.GetFactor().GetNumeratorText(), Is.EqualTo(numerator));
        Assert.That(prefix.GetFactor().GetDenominatorText(), Is.EqualTo(denominator));
    }
}
