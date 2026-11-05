using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class StandardPrefixesTests
{
    [Test]
    public void CreateCatalog_ReturnsSiPrefixCatalog()
    {
        PrefixCatalog catalog = new StandardPrefixes().CreateCatalog();

        Assert.That(catalog.GetCount(), Is.EqualTo(24));
        Assert.That(catalog.FindById("quetta")!.GetFactor().GetNumeratorText(), Is.EqualTo("1000000000000000000000000000000"));
        Assert.That(catalog.FindById("quecto")!.GetFactor().GetDenominatorText(), Is.EqualTo("1000000000000000000000000000000"));
    }

    [Test]
    public void CreateCatalog_MatchesEverySiPrefixDefinitionAndExactPowerOfTen()
    {
        PrefixDefinition[] prefixes = new StandardPrefixes().CreateCatalog().GetPrefixes();
        string[] ids = new string[]
        {
            "quetta", "ronna", "yotta", "zetta", "exa", "peta", "tera", "giga", "mega", "kilo", "hecto", "deca",
            "deci", "centi", "milli", "micro", "nano", "pico", "femto", "atto", "zepto", "yocto", "ronto", "quecto"
        };
        string[] symbols = new string[]
        {
            "Q", "R", "Y", "Z", "E", "P", "T", "G", "M", "k", "h", "da",
            "d", "c", "m", "\u03bc", "n", "p", "f", "a", "z", "y", "r", "q"
        };
        int[] exponents = new int[]
        {
            30, 27, 24, 21, 18, 15, 12, 9, 6, 3, 2, 1,
            -1, -2, -3, -6, -9, -12, -15, -18, -21, -24, -27, -30
        };

        Assert.That(prefixes.Length, Is.EqualTo(ids.Length));
        int index = 0;
        while (index < prefixes.Length)
        {
            PrefixDefinition prefix = prefixes[index];
            Assert.That(prefix.GetId(), Is.EqualTo(ids[index]), "prefix id at index " + index);
            Assert.That(prefix.GetName(), Is.EqualTo(ids[index]), "prefix name for " + ids[index]);
            Assert.That(prefix.GetSymbol(), Is.EqualTo(symbols[index]), "prefix symbol for " + ids[index]);
            Assert.That(prefix.GetFactor().EqualsValue(PowerOfTen(exponents[index])), Is.True, "prefix factor for " + ids[index]);
            index = index + 1;
        }
    }

    [Test]
    public void Register_AddsSiPrefixesToRegistry()
    {
        UnitRegistry registry = new UnitRegistry();

        OperationResult result = new StandardPrefixes().Register(registry);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(registry.GetPrefixes().Length, Is.EqualTo(24));
        Assert.That(registry.FindPrefixById("kilo")!.GetSymbol(), Is.EqualTo("k"));
        Assert.That(registry.FindPrefixByName("micro")!.GetSymbol(), Is.EqualTo("\u03bc"));
        Assert.That(registry.FindPrefixBySymbol("q")!.GetId(), Is.EqualTo("quecto"));
        Assert.That(registry.FindPrefixById("missing"), Is.Null);
    }

    [Test]
    public void Register_ReturnsDuplicateFailureWhenPrefixesAlreadyRegistered()
    {
        UnitRegistry registry = new UnitRegistry();
        new StandardPrefixes().Register(registry);

        OperationResult result = new StandardPrefixes().Register(registry);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryDuplicateId));
    }

    [Test]
    public void Register_ReturnsFrozenFailureWhenRegistryIsFrozen()
    {
        UnitRegistry registry = new UnitRegistry();
        registry.Freeze();

        OperationResult result = new StandardPrefixes().Register(registry);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryFrozen));
        Assert.That(registry.GetPrefixes(), Is.EqualTo(new PrefixDefinition[0]));
    }

    [Test]
    public void Register_RejectsMissingRegistry()
    {
        Assert.Throws<System.ArgumentNullException>(() => new StandardPrefixes().Register(null!));
    }

    static RationalNumber PowerOfTen(int exponent)
    {
        if (exponent >= 0)
        {
            return RationalNumber.FromBigIntegers(System.Numerics.BigInteger.Pow(new System.Numerics.BigInteger(10), exponent), new System.Numerics.BigInteger(1));
        }

        return RationalNumber.FromBigIntegers(new System.Numerics.BigInteger(1), System.Numerics.BigInteger.Pow(new System.Numerics.BigInteger(10), 0 - exponent));
    }
}
