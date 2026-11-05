using fake;

namespace fake.Tests;

[TestFixture]
public class FakeTests
{
    [Test]
    public void DefaultFactoryCapturesDefaultOptions()
    {
        Fake value = Fake.Create();

        FakeOptions options = value.OptionsSnapshot();

        Assert.That(options.Locale, Is.EqualTo("en-US"));
        Assert.That(options.Seed, Is.Zero);
        Assert.That(value.Context.ReferenceTime(), Is.EqualTo(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)));
    }

    [TestCase(int.MinValue)]
    [TestCase(0)]
    [TestCase(int.MaxValue)]
    public void IntegerSeedFactoryCapturesSeed(int seed)
    {
        Fake value = Fake.Create(seed);

        Assert.That(value.OptionsSnapshot().Seed, Is.EqualTo(seed));
    }

    [Test]
    public void LongSeedFactoryCapturesFullSeed()
    {
        Fake value = Fake.Create(long.MaxValue);

        Assert.That(value.OptionsSnapshot().Seed, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void LocaleFactoryCapturesLocale()
    {
        Fake value = Fake.Create("fr-CA");

        Assert.That(value.OptionsSnapshot().Locale, Is.EqualTo("fr-CA"));
    }

    [Test]
    public void LocaleAndSeedFactoryCapturesBoth()
    {
        Fake value = Fake.Create("de-AT", 1234567890123L);
        FakeOptions options = value.OptionsSnapshot();

        Assert.That(options.Locale, Is.EqualTo("de-AT"));
        Assert.That(options.Seed, Is.EqualTo(1234567890123L));
    }

    [Test]
    public void ReferenceClockIsCapturedAndStable()
    {
        FakeOptions options = new FakeOptions();
        options.ReferenceTime = new DateTimeOffset(2040, 2, 29, 23, 59, 58, TimeSpan.FromHours(5));
        Fake value = Fake.Create(options);

        options.ReferenceTime = DateTimeOffset.MinValue;

        DateTimeOffset firstReading = value.Context.ReferenceTime();

        Assert.That(firstReading, Is.EqualTo(new DateTimeOffset(2040, 2, 29, 23, 59, 58, TimeSpan.FromHours(5))));
        Assert.That(value.Context.ReferenceTime(), Is.EqualTo(firstReading));
    }

    [Test]
    public void GenerationContextOwnsAnIndependentOptionsSnapshot()
    {
        FakeOptions options = FakeOptions.Create("es-MX", 42);
        Fake value = Fake.Create(options);

        value.CapturedOptions.Locale = "ja-JP";
        FakeOptions contextOptions = value.Context.OptionsSnapshot();
        contextOptions.Locale = "tr-TR";

        Assert.That(value.Context.OptionsSnapshot().Locale, Is.EqualTo("es-MX"));
    }

    [Test]
    public void EveryProviderAccessorReturnsItsCachedInstance()
    {
        Fake value = Fake.Create();

        Assert.Multiple(() =>
        {
            AssertCached(value.Random(), () => value.Random());
            AssertCached(value.Primitive(), () => value.Primitive());
            AssertCached(value.Text(), () => value.Text());
            AssertCached(value.Checksum(), () => value.Checksum());
            AssertCached(value.Identifier(), () => value.Identifier());
            AssertCached(value.Date(), () => value.Date());
            AssertCached(value.Locale(), () => value.Locale());
            AssertCached(value.Person(), () => value.Person());
            AssertCached(value.Address(), () => value.Address());
            AssertCached(value.Internet(), () => value.Internet());
            AssertCached(value.Phone(), () => value.Phone());
            AssertCached(value.Finance(), () => value.Finance());
            AssertCached(value.Company(), () => value.Company());
            AssertCached(value.Commerce(), () => value.Commerce());
            AssertCached(value.System(), () => value.System());
            AssertCached(value.Database(), () => value.Database());
            AssertCached(value.Git(), () => value.Git());
            AssertCached(value.Color(), () => value.Color());
            AssertCached(value.Vehicle(), () => value.Vehicle());
            AssertCached(value.Airline(), () => value.Airline());
            AssertCached(value.Book(), () => value.Book());
            AssertCached(value.Media(), () => value.Media());
            AssertCached(value.Food(), () => value.Food());
            AssertCached(value.Animal(), () => value.Animal());
            AssertCached(value.Science(), () => value.Science());
            AssertCached(value.Health(), () => value.Health());
            AssertCached(value.Education(), () => value.Education());
            AssertCached(value.Culture(), () => value.Culture());
            AssertCached(value.Profile(), () => value.Profile());
            AssertCached(value.Schema(), () => value.Schema());
            AssertCached(value.Edge(), () => value.Edge());
            AssertCached(value.Export(), () => value.Export());
        });
    }

    [Test]
    public void ProvidersShareTheRootGenerationContext()
    {
        Fake value = Fake.Create();

        Assert.Multiple(() =>
        {
            Assert.That(value.Random().Context, Is.SameAs(value.Context));
            Assert.That(value.Date().Context, Is.SameAs(value.Context));
            Assert.That(value.Locale().Context, Is.SameAs(value.Context));
            Assert.That(value.Person().Context, Is.SameAs(value.Context));
            Assert.That(value.Profile().Context, Is.SameAs(value.Context));
            Assert.That(value.Export().Context, Is.SameAs(value.Context));
        });
    }

    [Test]
    public void SeparateRootsOwnSeparateProvidersAndContexts()
    {
        Fake first = Fake.Create(42);
        Fake second = Fake.Create(42);

        Assert.That(first.Context, Is.Not.SameAs(second.Context));
        Assert.That(first.Random(), Is.Not.SameAs(second.Random()));
        Assert.That(first.Person(), Is.Not.SameAs(second.Person()));
    }

    static void AssertCached(object first, Func<object> getSecond)
    {
        Assert.That(getSecond(), Is.SameAs(first));
    }
}
