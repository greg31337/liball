using System.Text.Json;
using fake;

namespace fake.Tests;

[TestFixture]
public class LocaleResourceStoreTests
{
    [Test]
    public void ProductAssemblyDiscoveryLoadsInvariantAndRegionalResourcesButIgnoresStandards()
    {
        LocaleResourceStore store = new LocaleResourceStore(
            LocaleResourceSource.FromAssembly(typeof(Fake).Assembly),
            1000000,
            10000);
        LocaleFieldValue[] values = store.Values("1");

        Assert.That(
            values
                .Where(value => value.Locale == LocaleFieldValue.InvariantLocale)
                .Select(value => value.Field),
            Is.EqualTo(new string[]
            {
                "address.catalog",
                "internet.domain.documentation",
                "internet.ipv4.documentation-prefixes",
                "internet.ipv6.documentation-prefix",
                "phone.nanpa.fictional-subscriber-range",
                "date.month.wide",
                "date.month.abbreviated",
                "date.weekday.wide",
                "date.weekday.abbreviated",
                "date.order",
                "education.catalog",
                "person.names",
                "person.identity",
                "person.work",
                "person.profile",
                "phone.catalog",
                "text.words"
            }));
        Assert.That(
            values
                .Where(value => value.Locale != LocaleFieldValue.InvariantLocale)
                .Select(value => value.Locale)
                .Distinct(StringComparer.Ordinal)
                .ToArray()
                .Length,
            Is.EqualTo(38));
        Assert.That(
            values.Count(value => value.Locale != LocaleFieldValue.InvariantLocale),
            Is.EqualTo(266));
        Assert.That(store.OpenCount(), Is.EqualTo(45));
    }

    [Test]
    public void DiscoveryAndStreamOpeningAreLazyAndIgnoreUnrelatedResources()
    {
        LocaleResourceStore store = Store(
            new string[]
            {
                "fake.Data.standards.language-tags.json",
                "fake.Data.locales.fr_CA.text.json",
                "other.Data.locales.fr_CA.json"
            },
            new string[]
            {
                null,
                Pack("fr-CA", "v1", "[\"fr\",\"invariant\"]", "reviewed", "text.word", "\"bonjour\""),
                null
            });

        Assert.That(store.Loaded(), Is.False);
        Assert.That(store.OpenCount(), Is.Zero);

        LocaleFieldValue[] values = store.Values("v1");

        Assert.That(store.Loaded(), Is.True);
        Assert.That(store.OpenCount(), Is.EqualTo(1));
        Assert.That(values, Has.Length.EqualTo(1));
        Assert.That(values[0].Locale, Is.EqualTo("fr-CA"));
        Assert.That(values[0].Field, Is.EqualTo("text.word"));
        Assert.That(((JsonElement)values[0].Value).GetString(), Is.EqualTo("bonjour"));
    }

    [Test]
    public void SuccessfulLoadsAreCachedAndReturnedValuesAreDefensiveCopies()
    {
        LocaleResourceStore store = Store(
            new string[] { "fake.Data.locales.en_US.text.json" },
            new string[] { Pack("en-US", "v1", "[\"en\"]", "reviewed", "text.word", "\"hello\"") });

        LocaleFieldValue[] first = store.Values("v1");
        first[0].Locale = "fr";
        first[0].Field = "changed";
        first[0].Provenance = "changed";
        LocaleFieldValue[] second = store.Values("v1");

        Assert.That(store.OpenCount(), Is.EqualTo(1));
        Assert.That(second, Is.Not.SameAs(first));
        Assert.That(second[0], Is.Not.SameAs(first[0]));
        Assert.That(second[0].Locale, Is.EqualTo("en-US"));
        Assert.That(second[0].Field, Is.EqualTo("text.word"));
        Assert.That(second[0].Provenance, Does.Contain("Project authors"));
    }

    [Test]
    public void ConsistentFragmentsForOneLocaleAreCombined()
    {
        LocaleResourceStore store = Store(
            new string[]
            {
                "fake.Data.locales.en_US.address.json",
                "fake.Data.locales.en_US.text.json"
            },
            new string[]
            {
                Pack("en-US", "v1", "[\"en\"]", "reviewed", "address.line", "{\"separator\":\" \",\"lines\":2}"),
                Pack("en-US", "v1", "[\"en\"]", "reviewed", "text.word", "\"hello\"")
            });

        LocaleFieldValue[] values = store.Values("v1");

        Assert.That(values.Select(value => value.Field), Is.EqualTo(new string[] { "address.line", "text.word" }));
        Assert.That(store.OpenCount(), Is.EqualTo(2));
    }

    [Test]
    public void DataVersionsAreCachedSeparatelyAndSelectedExactly()
    {
        LocaleResourceStore store = Store(
            new string[]
            {
                "fake.Data.locales.en_US.v1.json",
                "fake.Data.locales.en_US.v2.json"
            },
            new string[]
            {
                Pack("en-US", "v1", "[\"en\"]", "reviewed", "text.word", "\"one\""),
                Pack("en-US", "v2", "[\"en\"]", "reviewed", "text.word", "\"two\"")
            });

        JsonElement one = (JsonElement)store.Values("v1")[0].Value;
        JsonElement two = (JsonElement)store.Values("v2")[0].Value;

        Assert.That(one.GetString(), Is.EqualTo("one"));
        Assert.That(two.GetString(), Is.EqualTo("two"));
        Assert.That(store.OpenCount(), Is.EqualTo(2));
        Assert.That(
            Assert.Throws<FakeException>(() => store.Values("missing")).Message,
            Is.EqualTo("Locale data version 'missing' is not embedded. Available versions: v1, v2."));
    }

    [Test]
    public void ProviderNoArgumentApisUseSelectedEmbeddedDataVersion()
    {
        LocaleResourceStore store = Store(
            new string[]
            {
                "fake.Data.locales.fr_CA.date.json",
                "fake.Data.locales.fr.text.json",
                "fake.Data.invariant.formats.json"
            },
            new string[]
            {
                Pack("fr-CA", "v1", "[\"fr\",\"invariant\"]", "reviewed", "date.order", "\"dmy\""),
                Pack("fr", "v1", "[\"invariant\"]", "reviewed", "text.word", "\"mot\""),
                Pack("invariant", "v1", "[]", "reviewed", "identifier.format", "\"id\"")
            });
        LocaleProvider locale = Provider(store, "fr-CA", "en", "v1");

        JsonElement resolved = (JsonElement)locale.Resolve("text.word");
        FallbackTrace trace = locale.FallbackTrace("identifier.format");
        LocaleCoverage coverage = locale.Coverage("fr-CA");

        Assert.Multiple(() =>
        {
            Assert.That(locale.Supported(), Is.EqualTo(new string[] { "fr", "fr-CA" }));
            Assert.That(locale.IsSupported("fr-CA"), Is.True);
            Assert.That(locale.Metadata("fr-CA").Fields, Is.EqualTo(new string[] { "date.order" }));
            Assert.That(resolved.GetString(), Is.EqualTo("mot"));
            Assert.That(trace.SourceLocale, Is.EqualTo("invariant"));
            Assert.That(((JsonElement)trace.Value).GetString(), Is.EqualTo("id"));
            Assert.That(coverage.NativeFields, Is.EqualTo(new string[] { "date.order" }));
            Assert.That(coverage.FallbackFields, Is.EqualTo(new string[] { "identifier.format", "text.word" }));
        });
    }

    [Test]
    public void DuplicateResourceNamesAreRejectedBeforeAnyStreamIsOpened()
    {
        string name = "fake.Data.locales.en.text.json";
        LocaleResourceStore store = Store(
            new string[] { name, name },
            new string[]
            {
                Pack("en", "v1", "[]", "reviewed", "text.one", "1"),
                Pack("en", "v1", "[]", "reviewed", "text.two", "2")
            });

        Assert.That(
            Assert.Throws<FakeException>(() => store.Values("v1")).Message,
            Is.EqualTo("Embedded locale resource name 'fake.Data.locales.en.text.json' is duplicated."));
        Assert.That(store.OpenCount(), Is.Zero);
        Assert.That(store.Loaded(), Is.False);
    }

    [Test]
    public void DuplicateFieldsNameBothResourcesDeterministically()
    {
        LocaleResourceStore store = Store(
            new string[]
            {
                "fake.Data.locales.en.z.json",
                "fake.Data.locales.en.a.json"
            },
            new string[]
            {
                Pack("en", "v1", "[]", "reviewed", "text.word", "\"z\""),
                Pack("en", "v1", "[]", "reviewed", "text.word", "\"a\"")
            });

        FakeException exception = Assert.Throws<FakeException>(() => store.Values("v1"));

        Assert.That(
            exception.Message,
            Is.EqualTo("Embedded locale resources 'fake.Data.locales.en.a.json' and 'fake.Data.locales.en.z.json' both define field 'text.word' for locale 'en' in data version 'v1'."));
        Assert.That(store.Loaded(), Is.False);
    }

    [Test]
    public void InconsistentFragmentMetadataNamesBothResources()
    {
        LocaleResourceStore store = Store(
            new string[]
            {
                "fake.Data.locales.en.first.json",
                "fake.Data.locales.en.second.json"
            },
            new string[]
            {
                Pack("en", "v1", "[\"invariant\"]", "reviewed", "text.one", "1"),
                Pack("en", "v1", "[]", "reviewed", "text.two", "2")
            });

        FakeException exception = Assert.Throws<FakeException>(() => store.Values("v1"));

        Assert.That(exception.Message, Does.Contain("fake.Data.locales.en.first.json"));
        Assert.That(exception.Message, Does.Contain("fake.Data.locales.en.second.json"));
        Assert.That(exception.Message, Does.Contain("inconsistent fallback or review metadata"));
    }

    [Test]
    public void MissingAndMalformedResourcesHaveSpecificDiagnosticsAndAreNotCached()
    {
        string missingName = "fake.Data.locales.en.missing.json";
        LocaleResourceStore missing = Store(new string[] { missingName }, new string[] { null });
        string malformedName = "fake.Data.locales.en.malformed.json";
        LocaleResourceStore malformed = Store(new string[] { malformedName }, new string[] { "{" });

        Assert.That(
            Assert.Throws<FakeException>(() => missing.Values("v1")).Message,
            Is.EqualTo("Embedded locale resource 'fake.Data.locales.en.missing.json' could not be opened."));
        Assert.That(missing.Loaded(), Is.False);
        Assert.That(
            Assert.Throws<FakeException>(() => malformed.Values("v1")).Message,
            Does.StartWith("Malformed locale pack resource 'fake.Data.locales.en.malformed.json' at character 1:"));
        Assert.That(malformed.Loaded(), Is.False);

        Assert.Throws<FakeException>(() => malformed.Values("v1"));
        Assert.That(malformed.OpenCount(), Is.EqualTo(2));
    }

    [Test]
    public void SchemaDiagnosticsRetainTheExactJsonPath()
    {
        string name = "fake.Data.locales.en.invalid.json";
        LocaleResourceStore store = Store(
            new string[] { name },
            new string[] { Pack("EN", "v1", "[]", "reviewed", "text.word", "\"hello\"") });

        FakeException exception = Assert.Throws<FakeException>(() => store.Values("v1"));

        Assert.That(exception.Message, Does.Contain("Locale pack schema violation at '$.locale'"));
        Assert.That(exception.Message, Does.Contain("must use canonical tag 'en'"));
    }

    [TestCase("true")]
    [TestCase("false")]
    [TestCase("-12.5e+2")]
    [TestCase("\"\\uD83D\\uDE00\"")]
    [TestCase("[{\"escaped\":\"\\b\\f\\n\\r\\t\\/\"}]")]
    public void ResourceSyntaxPreflightAcceptsValidJsonValueForms(string value)
    {
        LocaleResourceStore store = Store(
            new string[] { "fake.Data.locales.en.valid.json" },
            new string[] { Pack("en", "v1", "[]", "reviewed", "test.value", value) });

        Assert.That(store.Values("v1"), Has.Length.EqualTo(1));
    }

    [TestCase("01")]
    [TestCase("\"\\q\"")]
    [TestCase("{\"x\":1,}")]
    [TestCase("[1,]")]
    [TestCase("\"\\uD800\"")]
    [TestCase("true false")]
    public void ResourceSyntaxPreflightRejectsMalformedJsonValueForms(string value)
    {
        string name = "fake.Data.locales.en.malformed-value.json";
        LocaleResourceStore store = Store(
            new string[] { name },
            new string[] { Pack("en", "v1", "[]", "reviewed", "test.value", value) });

        Assert.That(
            Assert.Throws<FakeException>(() => store.Values("v1")).Message,
            Does.StartWith("Malformed locale pack resource '" + name + "' at character"));
    }

    [Test]
    public void ResourceSizeAndManifestCountLimitsAreEnforced()
    {
        string json = Pack("en", "v1", "[]", "reviewed", "text.word", "\"hello\"");
        LocaleResourceStore tooLarge = new LocaleResourceStore(
            LocaleResourceSource.FromContent(new string[] { "fake.Data.locales.en.text.json" },
                new string[] { json }),
            json.Length - 1,
            10);
        LocaleResourceStore tooMany = new LocaleResourceStore(
            LocaleResourceSource.FromContent(new string[] { "one", "two" },
                new string[] { "{}", "{}" }),
            100,
            1);

        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => tooLarge.Values("v1")).ParamName,
            Is.EqualTo("json"));
        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => tooMany.Values("v1")).ParamName,
            Is.EqualTo("embeddedResourceNames"));
    }

    [Test]
    public void ConcurrentFirstAccessLoadsEveryResourceExactlyOnce()
    {
        LocaleResourceStore store = Store(
            new string[]
            {
                "fake.Data.locales.en.one.json",
                "fake.Data.locales.en.two.json"
            },
            new string[]
            {
                Pack("en", "v1", "[]", "reviewed", "text.one", "1"),
                Pack("en", "v1", "[]", "reviewed", "text.two", "2")
            });
        int[] lengths = new int[1000];

        Parallel.For(0, lengths.Length, index => lengths[index] = store.Values("v1").Length);

        Assert.That(lengths, Has.All.EqualTo(2));
        Assert.That(store.OpenCount(), Is.EqualTo(2));
        Assert.That(store.Loaded(), Is.True);
    }

    static LocaleResourceStore Store(string[] names, string[] contents)
    {
        return new LocaleResourceStore(LocaleResourceSource.FromContent(names, contents), 100000, 1000);
    }

    static LocaleProvider Provider(
        LocaleResourceStore store,
        string locale,
        string fallbackLocale,
        string dataVersion)
    {
        FakeOptions options = FakeOptions.Create(locale, fallbackLocale, 0L);
        options.DataVersion = dataVersion;
        GenerationContext context = new GenerationContext(options);
        context.LocaleResources = store;
        return new LocaleProvider(context);
    }

    static string Pack(
        string locale,
        string dataVersion,
        string fallbacks,
        string reviewStatus,
        string field,
        string value)
    {
        return "{"
            + "\"schemaVersion\":1,"
            + "\"dataVersion\":\"" + dataVersion + "\","
            + "\"locale\":\"" + locale + "\","
            + "\"fallbacks\":" + fallbacks + ","
            + "\"reviewStatus\":\"" + reviewStatus + "\","
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
}
