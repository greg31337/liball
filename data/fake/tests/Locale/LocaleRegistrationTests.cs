using System.Text;
using System.Text.Json;
using fake;

namespace fake.Tests;

[TestFixture]
public class LocaleRegistrationTests
{
    [Test]
    public void DictionaryRegistrationCanonicalizesLocaleAndUpdatesCatalogApis()
    {
        Fake fake = FakeFor("he-IL", "en", "v1");
        Dictionary<string, object> fields = new Dictionary<string, object>
        {
            ["text.direction"] = "rtl",
            ["person.name"] = "name"
        };

        fake.Locale().RegisterFromLocaleAndFields("iw-il", fields);

        Assert.Multiple(() =>
        {
            Assert.That(fake.Locale().Supported(), Is.EqualTo(new string[] { "he-IL" }));
            Assert.That(fake.Locale().IsSupported("iw-IL"), Is.True);
            Assert.That(fake.Locale().Resolve("text.direction"), Is.EqualTo("rtl"));
            Assert.That(fake.Locale().Metadata("he-IL").Fields, Is.EqualTo(new string[] { "person.name", "text.direction" }));
            Assert.That(fake.Locale().Metadata("he-IL").DataVersion, Is.EqualTo("v1"));
            Assert.That(fake.Locale().Metadata("he-IL").Provenance, Is.EqualTo(new string[] { LocaleFieldValue.CallerProvenance }));
        });
    }

    [Test]
    public void CallerOnlyDataVersionDoesNotRequireAnEmbeddedVersion()
    {
        Fake fake = FakeFor("en", "en", "caller-v1");
        fake.Locale().RegisterFromLocaleAndFields(
            "en",
            new Dictionary<string, object> { ["text.word"] = "runtime only" });

        Assert.That(fake.Locale().Resolve("text.word"), Is.EqualTo("runtime only"));
        Assert.That(fake.Locale().Metadata("en").DataVersion, Is.EqualTo("caller-v1"));
    }

    [Test]
    public void DictionaryIsCopiedAndValuesAreCapturedByReference()
    {
        Fake fake = FakeFor("en", "en", "v1");
        object marker = new object();
        Dictionary<string, object> fields = new Dictionary<string, object>
        {
            ["test.value"] = marker
        };

        fake.Locale().RegisterFromLocaleAndFields("en", fields);
        fields["test.value"] = new object();
        fields.Clear();

        Assert.That(fake.Locale().Resolve("test.value"), Is.SameAs(marker));
    }

    [Test]
    public void ExplicitDictionaryMetadataAppearsInTraceAndCoverage()
    {
        Fake fake = FakeFor("fr-CA", "en", "v1");
        fake.Locale().RegisterFromLocaleAndDataVersionAndProvenanceAndFields(
            "fr",
            "v1",
            "reviewed caller dictionary",
            new Dictionary<string, object> { ["text.word"] = "mot" });

        FallbackTrace trace = fake.Locale().FallbackTrace("text.word");
        LocaleCoverage coverage = fake.Locale().Coverage("fr-CA");

        Assert.Multiple(() =>
        {
            Assert.That(trace.SourceLocale, Is.EqualTo("fr"));
            Assert.That(trace.UsedFallback, Is.True);
            Assert.That(trace.DataVersion, Is.EqualTo("v1"));
            Assert.That(trace.Provenance, Is.EqualTo("reviewed caller dictionary"));
            Assert.That(coverage.NativeFields, Is.Empty);
            Assert.That(coverage.FallbackFields, Is.EqualTo(new string[] { "text.word" }));
            Assert.That(coverage.FallbackSources, Is.EqualTo(new string[] { "fr" }));
        });
    }

    [Test]
    public void StreamRegistrationLeavesStreamOpenAndLoadsJsonValues()
    {
        Fake fake = FakeFor("fr-CA", "en", "v1");
        MemoryStream stream = Stream(Pack(
            "fr-CA",
            "v1",
            "[\"fr\",\"invariant\"]",
            "text.word",
            "\"bonjour\""));

        fake.Locale().RegisterFromStreamAndResourceName(stream, "caller-fr-CA.json");
        JsonElement value = (JsonElement)fake.Locale().Resolve("text.word");
        FallbackTrace trace = fake.Locale().FallbackTrace("text.word");

        Assert.That(stream.CanRead, Is.True);
        Assert.That(stream.Position, Is.EqualTo(stream.Length));
        Assert.That(value.GetString(), Is.EqualTo("bonjour"));
        Assert.That(trace.Provenance, Does.Contain("Project authors"));
        Assert.That(trace.Provenance, Does.Contain("license project"));
    }

    [Test]
    public void RuntimeFieldOverridesEmbeddedFieldAtTheSameLocaleTier()
    {
        LocaleResourceStore embedded = Embedded(
            new string[] { "fake.Data.locales.en.text.json" },
            new string[] { Pack("en", "v1", "[]", "text.word", "\"embedded\"") });
        Fake fake = FakeFor("en", "en", "v1", embedded);

        Assert.That(((JsonElement)fake.Locale().Resolve("text.word")).GetString(), Is.EqualTo("embedded"));

        fake.Locale().RegisterFromLocaleAndFields("en", new Dictionary<string, object> { ["text.word"] = "runtime" });

        Assert.That(fake.Locale().Resolve("text.word"), Is.EqualTo("runtime"));
    }

    [Test]
    public void LocaleFallbackOrderPrecedesSourceKindPrecedence()
    {
        LocaleResourceStore embedded = Embedded(
            new string[] { "fake.Data.locales.en_US.text.json" },
            new string[] { Pack("en-US", "v1", "[\"en\"]", "text.word", "\"embedded exact\"") });
        Fake fake = FakeFor("en-US", "en", "v1", embedded);
        fake.Locale().RegisterFromLocaleAndFields("en", new Dictionary<string, object> { ["text.word"] = "runtime parent" });

        Assert.That(((JsonElement)fake.Locale().Resolve("text.word")).GetString(), Is.EqualTo("embedded exact"));
    }

    [Test]
    public void LaterSuccessfulRegistrationWinsAcrossInputForms()
    {
        Fake fake = FakeFor("en", "en", "v1");
        fake.Locale().RegisterFromLocaleAndFields("en", new Dictionary<string, object> { ["text.word"] = "dictionary one" });
        fake.Locale().RegisterFromStreamAndResourceName(
            Stream(Pack("en", "v1", "[]", "text.word", "\"stream two\"")),
            "stream-two.json");

        Assert.That(((JsonElement)fake.Locale().Resolve("text.word")).GetString(), Is.EqualTo("stream two"));

        fake.Locale().RegisterFromLocaleAndFields("en", new Dictionary<string, object> { ["text.word"] = "dictionary three" });

        Assert.That(fake.Locale().Resolve("text.word"), Is.EqualTo("dictionary three"));
        Assert.That(fake.Context.LocaleRegistrations.RegistrationCount(), Is.EqualTo(1));
    }

    [Test]
    public void FailedStreamRegistrationIsAtomicAndUsesCallerResourceDiagnostic()
    {
        Fake fake = FakeFor("en", "en", "v1");
        fake.Locale().RegisterFromLocaleAndFields("en", new Dictionary<string, object> { ["text.word"] = "existing" });

        FakeException exception = Assert.Throws<FakeException>(() => fake.Locale().RegisterFromStreamAndResourceName(
            Stream("{"),
            "broken-caller-pack.json"));

        Assert.That(exception.Message, Does.StartWith("Malformed locale pack resource 'broken-caller-pack.json'"));
        Assert.That(fake.Locale().Resolve("text.word"), Is.EqualTo("existing"));
        Assert.That(fake.Context.LocaleRegistrations.RegistrationCount(), Is.EqualTo(1));
    }

    [Test]
    public void WrongStreamDataVersionIsRejectedAtomically()
    {
        Fake fake = FakeFor("en", "en", "v1");

        FakeException exception = Assert.Throws<FakeException>(() => fake.Locale().RegisterFromStreamAndResourceName(
            Stream(Pack("en", "v2", "[]", "text.word", "\"wrong\"")),
            "wrong-version.json"));

        Assert.That(
            exception.Message,
            Is.EqualTo("Caller locale resource 'wrong-version.json' uses data version 'v2', but this Fake instance selected 'v1'."));
        Assert.That(fake.Context.LocaleRegistrations.RegistrationCount(), Is.Zero);
    }

    [Test]
    public void DictionaryValidationAndCountLimitsAreAtomic()
    {
        FakeOptions options = Options("en", "en", "v1");
        options.MaximumCount = 2;
        Fake fake = Fake.Create(options);
        fake.Locale().RegisterFromLocaleAndFields("en", new Dictionary<string, object> { ["one"] = 1 });

        Assert.That(
            Assert.Throws<ArgumentException>(() => fake.Locale().RegisterFromLocaleAndFields("en", new Dictionary<string, object>())).ParamName,
            Is.EqualTo("fields"));
        Assert.That(
            Assert.Throws<ArgumentNullException>(() => fake.Locale().RegisterFromLocaleAndFields("en", new Dictionary<string, object> { ["bad"] = null })).ParamName,
            Is.EqualTo("fields"));
        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => fake.Locale().RegisterFromLocaleAndFields(
                "en",
                new Dictionary<string, object> { ["two"] = 2, ["three"] = 3, ["four"] = 4 })).ParamName,
            Is.EqualTo("fields"));
        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => fake.Locale().RegisterFromLocaleAndFields(
                "en",
                new Dictionary<string, object> { ["two"] = 2, ["three"] = 3 })).ParamName,
            Is.EqualTo("registeredLocaleFields"));
        Assert.That(fake.Context.LocaleRegistrations.RegistrationCount(), Is.EqualTo(1));
        Assert.That(fake.Locale().Resolve("one"), Is.EqualTo(1));
        Assert.Throws<FakeException>(() => fake.Locale().Resolve("two"));
    }

    [Test]
    public void RegistrationArgumentsAndReadableStreamAreValidated()
    {
        Fake fake = FakeFor("en", "en", "v1");
        MemoryStream closed = Stream("{}");
        closed.Dispose();

        Assert.That(Assert.Throws<ArgumentNullException>(() => fake.Locale().Register((Stream)null)).ParamName, Is.EqualTo("stream"));
        Assert.That(Assert.Throws<ArgumentException>(() => fake.Locale().Register(closed)).ParamName, Is.EqualTo("stream"));
        Assert.That(Assert.Throws<ArgumentException>(() => fake.Locale().RegisterFromStreamAndResourceName(Stream("{}"), " ")).ParamName, Is.EqualTo("resourceName"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => fake.Locale().RegisterFromLocaleAndFields("en", (Dictionary<string, object>)null)).ParamName, Is.EqualTo("fields"));
        Assert.That(Assert.Throws<ArgumentException>(() => fake.Locale().RegisterFromLocaleAndFields("not_a_tag", new Dictionary<string, object> { ["field"] = 1 })).ParamName, Is.EqualTo("locale"));
        Assert.That(Assert.Throws<ArgumentException>(() => fake.Locale().RegisterFromLocaleAndDataVersionAndProvenanceAndFields("en", "v2", "source", new Dictionary<string, object> { ["field"] = 1 })).ParamName, Is.EqualTo("dataVersion"));
    }

    [Test]
    public void StrictModeStillRejectsRegisteredFallbackFields()
    {
        FakeOptions options = Options("fr-CA", "en", "v1");
        options.StrictnessMode = StrictnessMode.Strict;
        Fake fake = Fake.Create(options);
        fake.Locale().RegisterFromLocaleAndFields("fr", new Dictionary<string, object> { ["text.word"] = "mot" });

        Assert.Throws<FakeException>(() => fake.Locale().Resolve("text.word"));
    }

    [Test]
    public void RegistrationsAreInstanceScopedAndForksStartWithoutThem()
    {
        Fake first = FakeFor("en", "en", "v1");
        Fake second = FakeFor("en", "en", "v1");
        first.Locale().RegisterFromLocaleAndFields("en", new Dictionary<string, object> { ["text.word"] = "first only" });
        Fake fork = first.Fork("child");

        Assert.That(first.Locale().Resolve("text.word"), Is.EqualTo("first only"));
        Assert.Throws<FakeException>(() => second.Locale().Resolve("text.word"));
        Assert.Throws<FakeException>(() => fork.Locale().Resolve("text.word"));
    }

    [Test]
    public void RegistrationDoesNotConsumeEntropyAndSurvivesRandomReset()
    {
        Fake fake = Fake.Create(501);
        FakeState before = fake.Snapshot();
        fake.Locale().RegisterFromLocaleAndFields("en-US", new Dictionary<string, object> { ["text.word"] = "word" });

        AssertStateEqual(fake.Snapshot(), before);
        fake.Reset();
        Assert.That(fake.Locale().Resolve("text.word"), Is.EqualTo("word"));
    }

    [Test]
    public void ConcurrentDistinctFieldRegistrationsAreNotLost()
    {
        Fake fake = FakeFor("en", "en", "v1");

        Parallel.For(0, 500, index =>
        {
            Dictionary<string, object> fields = new Dictionary<string, object>
            {
                ["field." + index] = index
            };
            fake.Locale().RegisterFromLocaleAndFields("en", fields);
        });

        Assert.That(fake.Context.LocaleRegistrations.RegistrationCount(), Is.EqualTo(500));
        Assert.That(fake.Locale().Metadata("en").Fields, Has.Length.EqualTo(500));
        Assert.That(fake.Locale().Resolve("field.123"), Is.EqualTo(123));
    }

    static Fake FakeFor(string locale, string fallbackLocale, string dataVersion)
    {
        return Fake.Create(Options(locale, fallbackLocale, dataVersion));
    }

    static Fake FakeFor(
        string locale,
        string fallbackLocale,
        string dataVersion,
        LocaleResourceStore embedded)
    {
        Fake fake = FakeFor(locale, fallbackLocale, dataVersion);
        fake.Context.LocaleResources = embedded;
        return fake;
    }

    static FakeOptions Options(string locale, string fallbackLocale, string dataVersion)
    {
        FakeOptions options = FakeOptions.Create(locale, fallbackLocale, 0L);
        options.DataVersion = dataVersion;
        return options;
    }

    static LocaleResourceStore Embedded(string[] names, string[] contents)
    {
        return new LocaleResourceStore(LocaleResourceSource.FromContent(names, contents), 100000, 1000);
    }

    static MemoryStream Stream(string value)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(value));
    }

    static string Pack(
        string locale,
        string dataVersion,
        string fallbacks,
        string field,
        string value)
    {
        return "{"
            + "\"schemaVersion\":1,"
            + "\"dataVersion\":\"" + dataVersion + "\","
            + "\"locale\":\"" + locale + "\","
            + "\"fallbacks\":" + fallbacks + ","
            + "\"reviewStatus\":\"reviewed\","
            + "\"provenance\":[{"
            + "\"id\":\"project\","
            + "\"source\":\"Project authors\","
            + "\"sourceVersion\":\"2026.08\","
            + "\"license\":\"project\","
            + "\"reviewer\":\"reviewer\"}],"
            + "\"fields\":[{"
            + "\"name\":\"" + field + "\","
            + "\"provenance\":\"project\","
            + "\"value\":" + value + "}]}";
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
