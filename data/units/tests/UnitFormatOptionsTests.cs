using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class UnitFormatOptionsTests
{
    [Test]
    public void CreateDefault_UsesSymbolStyle()
    {
        UnitFormatOptions options = UnitFormatOptions.CreateDefault();

        Assert.That(options.GetStyle(), Is.EqualTo(UnitFormatStyle.Symbol));
        Assert.That(options.IsSymbol(), Is.True);
        Assert.That(options.IsName(), Is.False);
        Assert.That(options.IsCanonical(), Is.False);
        Assert.That(options.IsDecomposed(), Is.False);
    }

    [Test]
    public void Factories_CreateEachSupportedStyle()
    {
        UnitFormatOptions symbol = UnitFormatOptions.CreateSymbol();
        UnitFormatOptions name = UnitFormatOptions.CreateName();
        UnitFormatOptions canonical = UnitFormatOptions.CreateCanonical();
        UnitFormatOptions decomposed = UnitFormatOptions.CreateDecomposed();

        Assert.That(symbol.GetStyle(), Is.EqualTo(UnitFormatStyle.Symbol));
        Assert.That(name.GetStyle(), Is.EqualTo(UnitFormatStyle.Name));
        Assert.That(canonical.GetStyle(), Is.EqualTo(UnitFormatStyle.Canonical));
        Assert.That(decomposed.GetStyle(), Is.EqualTo(UnitFormatStyle.Decomposed));
        Assert.That(name.IsName(), Is.True);
        Assert.That(canonical.IsCanonical(), Is.True);
        Assert.That(decomposed.IsDecomposed(), Is.True);
    }

    [Test]
    public void Constructor_StoresSupportedStyle()
    {
        UnitFormatOptions options = new UnitFormatOptions(UnitFormatStyle.Name);

        Assert.That(options.GetStyle(), Is.EqualTo(UnitFormatStyle.Name));
        Assert.That(options.IsName(), Is.True);
    }

    [Test]
    public void WithStyle_ReturnsCopyWithDifferentStyle()
    {
        UnitFormatOptions options = UnitFormatOptions.CreateSymbol();

        UnitFormatOptions changed = options.WithStyle(UnitFormatStyle.Canonical);

        Assert.That(changed.GetStyle(), Is.EqualTo(UnitFormatStyle.Canonical));
        Assert.That(changed.IsCanonical(), Is.True);
        Assert.That(options.GetStyle(), Is.EqualTo(UnitFormatStyle.Symbol));
    }

    [Test]
    public void Constructor_RejectsUnsupportedStyle()
    {
        Assert.Throws<System.ArgumentException>(() => new UnitFormatOptions((UnitFormatStyle)0));
        Assert.Throws<System.ArgumentException>(() => new UnitFormatOptions((UnitFormatStyle)99));
    }

    [Test]
    public void WithStyle_RejectsUnsupportedStyle()
    {
        UnitFormatOptions options = UnitFormatOptions.CreateDefault();

        Assert.Throws<System.ArgumentException>(() => options.WithStyle((UnitFormatStyle)99));
    }
}
