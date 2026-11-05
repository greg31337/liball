using fake;

namespace fake.Tests;

[TestFixture]
public class FakeOptionsTests
{
    [Test]
    public void DefaultConstructorUsesSafeDeterministicDefaults()
    {
        FakeOptions options = new FakeOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Locale, Is.EqualTo("en-US"));
            Assert.That(options.FallbackLocale, Is.EqualTo("en"));
            Assert.That(options.Seed, Is.Zero);
            Assert.That(options.ReferenceTime, Is.EqualTo(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(options.RandomMode, Is.EqualTo(RandomMode.Deterministic));
            Assert.That(options.SafetyMode, Is.EqualTo(SafetyMode.Reserved));
            Assert.That(options.GenerationMode, Is.EqualTo(GenerationMode.Typical));
            Assert.That(options.StrictnessMode, Is.EqualTo(StrictnessMode.AllowFallback));
            Assert.That(options.NormalizationMode, Is.EqualTo(NormalizationMode.None));
            Assert.That(options.MaximumCount, Is.EqualTo(FakeLimits.DefaultMaximumCount));
            Assert.That(options.MaximumOutputSize, Is.EqualTo(FakeLimits.DefaultMaximumOutputSize));
            Assert.That(options.UniquenessRetryLimit, Is.EqualTo(100));
            Assert.That(options.DataVersion, Is.EqualTo("1"));
        });
        Assert.DoesNotThrow(options.Validate);
    }

    [Test]
    public void SeedFactoryOverridesOnlySeed()
    {
        FakeOptions options = FakeOptions.Create(long.MinValue);

        Assert.That(options.Seed, Is.EqualTo(long.MinValue));
        Assert.That(options.Locale, Is.EqualTo("en-US"));
    }

    [Test]
    public void LocaleFactoryOverridesOnlyLocale()
    {
        FakeOptions options = FakeOptions.Create("fr-CA");

        Assert.That(options.Locale, Is.EqualTo("fr-CA"));
        Assert.That(options.Seed, Is.Zero);
    }

    [Test]
    public void LocaleAndSeedFactorySetsBothValues()
    {
        FakeOptions options = FakeOptions.Create("de-DE", 42);

        Assert.That(options.Locale, Is.EqualTo("de-DE"));
        Assert.That(options.Seed, Is.EqualTo(42));
    }

    [Test]
    public void LocaleFallbackAndSeedFactorySetsAllValues()
    {
        FakeOptions options = FakeOptions.Create("es-MX", "es", -42);

        Assert.That(options.Locale, Is.EqualTo("es-MX"));
        Assert.That(options.FallbackLocale, Is.EqualTo("es"));
        Assert.That(options.Seed, Is.EqualTo(-42));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void LocaleFactoriesRejectInvalidLocale(string? locale)
    {
        Assert.Catch<ArgumentException>(() => FakeOptions.Create(locale));
        Assert.Catch<ArgumentException>(() => FakeOptions.Create(locale, 1));
        Assert.Catch<ArgumentException>(() => FakeOptions.Create(locale, "en", 1));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void ExplicitFallbackFactoryRejectsInvalidFallback(string? fallbackLocale)
    {
        Assert.Catch<ArgumentException>(() => FakeOptions.Create("en-US", fallbackLocale, 1));
    }

    [Test]
    public void CopyPreservesEveryField()
    {
        FakeOptions original = CreateNonDefaultOptions();

        FakeOptions copy = original.Copy();

        Assert.That(copy, Is.Not.SameAs(original));
        AssertOptionsEqual(copy, original);
    }

    [Test]
    public void CopyIsIndependentFromLaterFieldChanges()
    {
        FakeOptions original = CreateNonDefaultOptions();
        FakeOptions copy = original.Copy();

        original.Locale = "ja-JP";
        original.MaximumCount = 1;

        Assert.That(copy.Locale, Is.EqualTo("fr-CA"));
        Assert.That(copy.MaximumCount, Is.EqualTo(321));
    }

    [Test]
    public void CopyFactoryRejectsNull()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => FakeOptions.Create((FakeOptions)null));

        Assert.That(exception.ParamName, Is.EqualTo("options"));
    }

    [TestCase("Locale")]
    [TestCase("FallbackLocale")]
    [TestCase("DataVersion")]
    public void ValidateRejectsNullRequiredStrings(string field)
    {
        FakeOptions options = new FakeOptions();
        SetStringField(options, field, null);

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(options.Validate);

        Assert.That(exception.ParamName, Is.EqualTo(field));
    }

    [TestCase("Locale")]
    [TestCase("FallbackLocale")]
    [TestCase("DataVersion")]
    public void ValidateRejectsBlankRequiredStrings(string field)
    {
        FakeOptions options = new FakeOptions();
        SetStringField(options, field, " ");

        ArgumentException exception = Assert.Throws<ArgumentException>(options.Validate);

        Assert.That(exception.ParamName, Is.EqualTo(field));
    }

    [TestCase("MaximumCount")]
    [TestCase("MaximumOutputSize")]
    [TestCase("UniquenessRetryLimit")]
    public void ValidateRejectsNonPositiveLimits(string field)
    {
        FakeOptions options = new FakeOptions();
        SetLimitField(options, field, 0);

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(options.Validate);

        Assert.That(exception.ParamName, Is.EqualTo(field));
    }

    [Test]
    public void ValidateRejectsUnsupportedRandomMode()
    {
        FakeOptions options = new FakeOptions();
        options.RandomMode = (RandomMode)99;

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(options.Validate);

        Assert.That(exception.ParamName, Is.EqualTo("RandomMode"));
        Assert.That(exception.Message, Does.StartWith("RandomMode is not supported."));
    }

    [Test]
    public void ValidateRejectsUnsupportedSafetyMode()
    {
        FakeOptions options = new FakeOptions();
        options.SafetyMode = (SafetyMode)99;

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(options.Validate);

        Assert.That(exception.ParamName, Is.EqualTo("SafetyMode"));
    }

    [Test]
    public void ValidateRejectsUnsupportedGenerationMode()
    {
        FakeOptions options = new FakeOptions();
        options.GenerationMode = (GenerationMode)99;

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(options.Validate);

        Assert.That(exception.ParamName, Is.EqualTo("GenerationMode"));
    }

    [Test]
    public void ValidateRejectsUnsupportedStrictnessMode()
    {
        FakeOptions options = new FakeOptions();
        options.StrictnessMode = (StrictnessMode)99;

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(options.Validate);

        Assert.That(exception.ParamName, Is.EqualTo("StrictnessMode"));
    }

    [Test]
    public void ValidateRejectsUnsupportedNormalizationMode()
    {
        FakeOptions options = new FakeOptions();
        options.NormalizationMode = (NormalizationMode)99;

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(options.Validate);

        Assert.That(exception.ParamName, Is.EqualTo("NormalizationMode"));
    }

    [Test]
    public void FakeCapturesIndependentValidatedOptions()
    {
        FakeOptions options = CreateNonDefaultOptions();
        Fake value = Fake.Create(options);

        options.Locale = "ja-JP";
        options.MaximumCount = 1;
        FakeOptions firstSnapshot = value.OptionsSnapshot();
        firstSnapshot.Locale = "tr-TR";
        FakeOptions secondSnapshot = value.OptionsSnapshot();

        Assert.That(secondSnapshot.Locale, Is.EqualTo("fr-CA"));
        Assert.That(secondSnapshot.MaximumCount, Is.EqualTo(321));
    }

    [Test]
    public void FakeRejectsNullOptions()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Fake.Create((FakeOptions)null));

        Assert.That(exception.ParamName, Is.EqualTo("options"));
    }

    [Test]
    public void FakeRejectsInvalidOptionsBeforeCapture()
    {
        FakeOptions options = new FakeOptions();
        options.MaximumOutputSize = 0;

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Fake.Create(options));

        Assert.That(exception.ParamName, Is.EqualTo("MaximumOutputSize"));
    }

    static FakeOptions CreateNonDefaultOptions()
    {
        FakeOptions options = FakeOptions.Create("fr-CA", "fr", 987654321);
        options.ReferenceTime = new DateTimeOffset(2030, 6, 7, 8, 9, 10, TimeSpan.FromHours(-4));
        options.RandomMode = RandomMode.Cryptographic;
        options.SafetyMode = SafetyMode.SyntheticPlausible;
        options.GenerationMode = GenerationMode.ValidUnusual;
        options.StrictnessMode = StrictnessMode.Strict;
        options.NormalizationMode = NormalizationMode.Nfc;
        options.MaximumCount = 321;
        options.MaximumOutputSize = 6543;
        options.UniquenessRetryLimit = 17;
        options.DataVersion = "2026.08";
        return options;
    }

    static void AssertOptionsEqual(FakeOptions actual, FakeOptions expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Locale, Is.EqualTo(expected.Locale));
            Assert.That(actual.FallbackLocale, Is.EqualTo(expected.FallbackLocale));
            Assert.That(actual.Seed, Is.EqualTo(expected.Seed));
            Assert.That(actual.ReferenceTime, Is.EqualTo(expected.ReferenceTime));
            Assert.That(actual.RandomMode, Is.EqualTo(expected.RandomMode));
            Assert.That(actual.SafetyMode, Is.EqualTo(expected.SafetyMode));
            Assert.That(actual.GenerationMode, Is.EqualTo(expected.GenerationMode));
            Assert.That(actual.StrictnessMode, Is.EqualTo(expected.StrictnessMode));
            Assert.That(actual.NormalizationMode, Is.EqualTo(expected.NormalizationMode));
            Assert.That(actual.MaximumCount, Is.EqualTo(expected.MaximumCount));
            Assert.That(actual.MaximumOutputSize, Is.EqualTo(expected.MaximumOutputSize));
            Assert.That(actual.UniquenessRetryLimit, Is.EqualTo(expected.UniquenessRetryLimit));
            Assert.That(actual.DataVersion, Is.EqualTo(expected.DataVersion));
        });
    }

    static void SetStringField(FakeOptions options, string field, string value)
    {
        if (field == "Locale")
        {
            options.Locale = value;
        }
        else if (field == "FallbackLocale")
        {
            options.FallbackLocale = value;
        }
        else
        {
            options.DataVersion = value;
        }
    }

    static void SetLimitField(FakeOptions options, string field, int value)
    {
        if (field == "MaximumCount")
        {
            options.MaximumCount = value;
        }
        else if (field == "MaximumOutputSize")
        {
            options.MaximumOutputSize = value;
        }
        else
        {
            options.UniquenessRetryLimit = value;
        }
    }
}
