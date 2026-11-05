using fake;

namespace fake.Tests;

[TestFixture]
public class LocaleResolverTests
{
    [Test]
    public void ExactLocaleFieldWinsOverEveryFallbackTier()
    {
        FakeOptions options = Options("fr-CA", "en-US", StrictnessMode.AllowFallback);
        LocaleProvider locale = Fake.Create(options).Locale();
        object exact = new object();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("fr-CA", "person.name", exact),
            Value("fr", "person.name", new object()),
            Value("en-US", "person.name", new object()),
            Value("invariant", "person.name", new object())
        };

        Assert.That(locale.ResolveFromFieldAndValues("person.name", values), Is.SameAs(exact));
    }

    [Test]
    public void RegionFallsBackToLanguageParent()
    {
        LocaleProvider locale = Fake.Create(Options("fr-CA", "en-US", StrictnessMode.AllowFallback)).Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("fr", "text.word", "parent"),
            Value("en-US", "text.word", "configured"),
            Value("invariant", "text.word", "invariant")
        };

        Assert.That(locale.ResolveFromFieldAndValues("text.word", values), Is.EqualTo("parent"));
    }

    [Test]
    public void ScriptParentPrecedesLanguageParent()
    {
        LocaleProvider locale = Fake.Create(Options("zh-Hant-HK", "en", StrictnessMode.AllowFallback)).Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("zh-Hant", "text.word", "script parent"),
            Value("zh", "text.word", "language parent")
        };

        Assert.That(locale.ResolveFromFieldAndValues("text.word", values), Is.EqualTo("script parent"));
    }

    [Test]
    public void LookupParentsRemoveVariantsExtensionsAndPrivateUseCorrectly()
    {
        LocaleProvider locale = Fake.Create(Options("de-DE", "en", StrictnessMode.AllowFallback)).Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("de-DE", "variant", "without variant"),
            Value("en-u-ca", "extension", "shorter extension"),
            Value("en-US", "private", "without private use")
        };

        Assert.That(locale.ResolveFromLocaleAndFieldAndValues("de-DE-1901", "variant", values), Is.EqualTo("without variant"));
        Assert.That(locale.ResolveFromLocaleAndFieldAndValues("en-u-ca-gregory", "extension", values), Is.EqualTo("shorter extension"));
        Assert.That(locale.ResolveFromLocaleAndFieldAndValues("en-US-x-company", "private", values), Is.EqualTo("without private use"));
    }

    [Test]
    public void RequestedParentsPrecedeConfiguredFallback()
    {
        LocaleProvider locale = Fake.Create(Options("fr-CA", "en-US", StrictnessMode.AllowFallback)).Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("fr", "date.order", "requested parent"),
            Value("en-US", "date.order", "configured exact")
        };

        Assert.That(locale.ResolveFromFieldAndValues("date.order", values), Is.EqualTo("requested parent"));
    }

    [Test]
    public void ConfiguredFallbackUsesItsOwnParents()
    {
        LocaleProvider locale = Fake.Create(Options("fr-CA", "en-US", StrictnessMode.AllowFallback)).Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("en", "date.order", "configured parent"),
            Value("invariant", "date.order", "invariant")
        };

        Assert.That(locale.ResolveFromFieldAndValues("date.order", values), Is.EqualTo("configured parent"));
    }

    [Test]
    public void InvariantIsTheFinalFallback()
    {
        LocaleProvider locale = Fake.Create(Options("fr-CA", "en-US", StrictnessMode.AllowFallback)).Locale();
        object invariant = new object();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value(LocaleFieldValue.InvariantLocale, "identifier.format", invariant)
        };

        Assert.That(locale.ResolveFromFieldAndValues("identifier.format", values), Is.SameAs(invariant));
    }

    [Test]
    public void EveryFieldResolvesThroughItsOwnFallbackChain()
    {
        LocaleProvider locale = Fake.Create(Options("fr-CA", "en-US", StrictnessMode.AllowFallback)).Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("fr-CA", "date.order", "exact date"),
            Value("fr", "person.name", "parent name"),
            Value("en-US", "address.line", "configured address"),
            Value("invariant", "identifier.format", "invariant identifier")
        };

        Assert.Multiple(() =>
        {
            Assert.That(locale.ResolveFromFieldAndValues("date.order", values), Is.EqualTo("exact date"));
            Assert.That(locale.ResolveFromFieldAndValues("person.name", values), Is.EqualTo("parent name"));
            Assert.That(locale.ResolveFromFieldAndValues("address.line", values), Is.EqualTo("configured address"));
            Assert.That(locale.ResolveFromFieldAndValues("identifier.format", values), Is.EqualTo("invariant identifier"));
        });
    }

    [Test]
    public void StrictModeAcceptsOnlyAnExactNormalizedLocaleField()
    {
        LocaleProvider locale = Fake.Create(Options("fr-CA", "en-US", StrictnessMode.Strict)).Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("fr-CA", "exact", "exact value"),
            Value("fr", "parent", "parent value"),
            Value("en-US", "configured", "configured value"),
            Value("invariant", "invariant", "invariant value")
        };

        Assert.That(locale.ResolveFromFieldAndValues("exact", values), Is.EqualTo("exact value"));
        Assert.That(
            Assert.Throws<FakeException>(() => locale.ResolveFromFieldAndValues("parent", values)).Message,
            Is.EqualTo("Locale field 'parent' is unavailable for exact locale 'fr-CA' because strict fallback is enabled."));
        Assert.Throws<FakeException>(() => locale.ResolveFromFieldAndValues("configured", values));
        Assert.Throws<FakeException>(() => locale.ResolveFromFieldAndValues("invariant", values));
    }

    [Test]
    public void ExplicitLocaleStillUsesConfiguredFallbackPolicy()
    {
        LocaleProvider locale = Fake.Create(Options("de-DE", "en-US", StrictnessMode.AllowFallback)).Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("es", "text.word", "explicit parent"),
            Value("en-US", "text.word", "configured")
        };

        Assert.That(locale.ResolveFromLocaleAndFieldAndValues("es-MX", "text.word", values), Is.EqualTo("explicit parent"));
    }

    [Test]
    public void MissingFieldDiagnosticListsDeterministicFallbackOrder()
    {
        LocaleProvider locale = Fake.Create(Options("fr-CA", "en-US", StrictnessMode.AllowFallback)).Locale();

        FakeException exception = Assert.Throws<FakeException>(() => locale.ResolveFromFieldAndValues("missing", Array.Empty<LocaleFieldValue>()));

        Assert.That(
            exception.Message,
            Is.EqualTo("Locale field 'missing' is unavailable for locale 'fr-CA' after trying fr-CA -> fr -> en-US -> en -> invariant."));
    }

    [Test]
    public void LocaleAliasesAreCanonicalizedForMatching()
    {
        LocaleProvider locale = Fake.Create(Options("iw-IL", "en", StrictnessMode.AllowFallback)).Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("he-il", "text.direction", "right-to-left")
        };

        Assert.That(locale.ResolveFromFieldAndValues("text.direction", values), Is.EqualTo("right-to-left"));
    }

    [Test]
    public void DuplicateCanonicalLocaleFieldsAreRejected()
    {
        LocaleProvider locale = Fake.Create().Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("iw-IL", "text.direction", "first"),
            Value("he-IL", "text.direction", "second")
        };

        ArgumentException exception = Assert.Throws<ArgumentException>(() => locale.ResolveFromFieldAndValues("text.direction", values));

        Assert.That(exception.ParamName, Is.EqualTo("values"));
        Assert.That(exception.Message, Does.Contain("normalized locale 'he-IL'"));
    }

    [Test]
    public void LocaleFieldValueConstructorRejectsInvalidArguments()
    {
        Assert.That(Assert.Throws<ArgumentNullException>(() => new LocaleFieldValue(null, "field", "value")).ParamName, Is.EqualTo("locale"));
        Assert.That(Assert.Throws<ArgumentException>(() => new LocaleFieldValue(" ", "field", "value")).ParamName, Is.EqualTo("locale"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => new LocaleFieldValue("en", null, "value")).ParamName, Is.EqualTo("field"));
        Assert.That(Assert.Throws<ArgumentException>(() => new LocaleFieldValue("en", " ", "value")).ParamName, Is.EqualTo("field"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => new LocaleFieldValue("en", "field", null)).ParamName, Is.EqualTo("value"));
    }

    [Test]
    public void ResolveRejectsInvalidAndMutatedInputs()
    {
        LocaleProvider locale = Fake.Create().Locale();
        LocaleFieldValue mutated = Value("en", "field", "value");
        mutated.Locale = "abcd";

        Assert.That(Assert.Throws<ArgumentNullException>(() => locale.ResolveFromFieldAndValues(null, Array.Empty<LocaleFieldValue>())).ParamName, Is.EqualTo("field"));
        Assert.That(Assert.Throws<ArgumentException>(() => locale.ResolveFromFieldAndValues(" ", Array.Empty<LocaleFieldValue>())).ParamName, Is.EqualTo("field"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => locale.ResolveFromFieldAndValues("field", null)).ParamName, Is.EqualTo("values"));
        Assert.That(Assert.Throws<ArgumentException>(() => locale.ResolveFromFieldAndValues("field", new LocaleFieldValue[] { null })).ParamName, Is.EqualTo("values"));
        Assert.That(Assert.Throws<ArgumentException>(() => locale.ResolveFromFieldAndValues("field", new LocaleFieldValue[] { mutated })).ParamName, Is.EqualTo("values"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => locale.ResolveFromLocaleAndFieldAndValues(null, "field", Array.Empty<LocaleFieldValue>())).ParamName, Is.EqualTo("locale"));
    }

    [Test]
    public void ResolveHonorsConfiguredCountLimit()
    {
        FakeOptions options = Options("en-US", "en", StrictnessMode.AllowFallback);
        options.MaximumCount = 1;
        LocaleProvider locale = Fake.Create(options).Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("en-US", "field", "first"),
            Value("en", "field", "second")
        };

        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => locale.ResolveFromFieldAndValues("field", values)).ParamName, Is.EqualTo("values"));
    }

    [Test]
    public void ResolutionDoesNotConsumeRandomEntropy()
    {
        Fake fake = Fake.Create(701);
        FakeState before = fake.Snapshot();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("en", "field", "value")
        };

        Assert.That(fake.Locale().ResolveFromFieldAndValues("field", values), Is.EqualTo("value"));
        AssertStateEqual(fake.Snapshot(), before);
    }

    [Test]
    public void ConcurrentResolutionIsDeterministic()
    {
        LocaleProvider locale = Fake.Create(Options("zh-Hant-HK", "en", StrictnessMode.AllowFallback)).Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("zh-Hant", "field", "value")
        };
        object[] results = new object[1000];

        Parallel.For(0, results.Length, index => results[index] = locale.ResolveFromFieldAndValues("field", values));

        Assert.That(results, Has.All.EqualTo("value"));
    }

    static FakeOptions Options(string locale, string fallbackLocale, StrictnessMode strictness)
    {
        FakeOptions options = FakeOptions.Create(locale, fallbackLocale, 0L);
        options.StrictnessMode = strictness;
        return options;
    }

    static LocaleFieldValue Value(string locale, string field, object value)
    {
        return new LocaleFieldValue(locale, field, value);
    }

    static void AssertStateEqual(FakeState actual, FakeState expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Algorithm, Is.EqualTo(expected.Algorithm));
            Assert.That(actual.InitialSeed, Is.EqualTo(expected.InitialSeed));
            Assert.That(actual.State0, Is.EqualTo(expected.State0));
            Assert.That(actual.State1, Is.EqualTo(expected.State1));
            Assert.That(actual.State2, Is.EqualTo(expected.State2));
            Assert.That(actual.State3, Is.EqualTo(expected.State3));
        });
    }
}
