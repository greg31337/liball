using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class PrefixDefinitionTests
{
    [Test]
    public void Constructor_StoresPrefixMetadataAndExactFactor()
    {
        RationalNumber factor = new RationalNumber(1000, 1);

        PrefixDefinition prefix = new PrefixDefinition("kilo", "kilo", "k", factor);

        Assert.That(prefix.GetId(), Is.EqualTo("kilo"));
        Assert.That(prefix.GetName(), Is.EqualTo("kilo"));
        Assert.That(prefix.GetSymbol(), Is.EqualTo("k"));
        Assert.That(prefix.GetFactor().EqualsValue(factor), Is.True);
    }

    [Test]
    public void Constructor_RejectsMissingRequiredText()
    {
        RationalNumber factor = new RationalNumber(1000, 1);

        Assert.Throws<System.ArgumentException>(() => new PrefixDefinition("", "kilo", "k", factor));
        Assert.Throws<System.ArgumentException>(() => new PrefixDefinition("kilo", " ", "k", factor));
        Assert.Throws<System.ArgumentException>(() => new PrefixDefinition("kilo", "kilo", "", factor));
    }

    [Test]
    public void Constructor_RejectsMissingFactor()
    {
        Assert.Throws<System.ArgumentNullException>(() => new PrefixDefinition("kilo", "kilo", "k", null!));
    }

    [Test]
    public void Constructor_RejectsNonPositiveFactor()
    {
        Assert.Throws<System.ArgumentException>(() => new PrefixDefinition("zero", "zero", "z", new RationalNumber(0, 1)));
        Assert.Throws<System.ArgumentException>(() => new PrefixDefinition("negative", "negative", "n", new RationalNumber(-1, 1)));
    }

    [Test]
    public void Constructor_AcceptsArbitraryRangePositiveFactorWithoutLosingPrecision()
    {
        System.Numerics.BigInteger denominator = System.Numerics.BigInteger.Pow(new System.Numerics.BigInteger(10), 60);
        RationalNumber factor = new RationalNumber(new System.Numerics.BigInteger(1), denominator);

        PrefixDefinition prefix = new PrefixDefinition("tiny", "tiny", "x", factor);

        Assert.That(prefix.GetFactor(), Is.SameAs(factor));
        Assert.That(prefix.GetFactor().GetNumeratorText(), Is.EqualTo("1"));
        Assert.That(prefix.GetFactor().GetDenominatorText(), Is.EqualTo("1000000000000000000000000000000000000000000000000000000000000"));
    }
}
