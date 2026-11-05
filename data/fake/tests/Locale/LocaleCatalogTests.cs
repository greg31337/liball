using fake;

namespace fake.Tests;

[TestFixture]
public class LocaleCatalogTests
{
    [Test]
    public void BundledCatalogReportsWaveOneAndTwoNativeFieldsAndInvariantFallbacks()
    {
        LocaleProvider locale = Fake.Create().Locale();

        Assert.That(locale.Supported(), Is.EqualTo(new string[]
        {
            "ar-EG", "ar-SA", "cs-CZ", "da-DK", "de-AT", "de-CH", "de-DE",
            "el-GR", "en-AU", "en-CA", "en-GB", "en-US", "es-ES", "es-MX",
            "fa-IR", "fi-FI", "fr-CA", "fr-FR", "he-IL", "hi-IN", "id-ID",
            "it-IT", "ja-JP", "ko-KR", "nb-NO", "nl-NL", "pl-PL", "pt-BR",
            "pt-PT", "ru-RU", "sv-SE", "th-TH", "tr-TR", "uk-UA", "vi-VN",
            "zh-CN", "zh-HK", "zh-TW"
        }));
        Assert.That(locale.IsSupported("en-US"), Is.True);
        Assert.That(locale.Metadata("en-US").Fields, Is.EqualTo(new string[]
        {
            "date.month.abbreviated",
            "date.month.wide",
            "date.order",
            "date.weekday.abbreviated",
            "date.weekday.wide",
            "locale.profile",
            "locale.review"
        }));
        Assert.That(locale.Coverage("en-US").NativeFields, Is.EqualTo(new string[]
        {
            "date.month.abbreviated",
            "date.month.wide",
            "date.order",
            "date.weekday.abbreviated",
            "date.weekday.wide",
            "locale.profile",
            "locale.review"
        }));
        Assert.That(
            locale.Coverage("en-US").FallbackFields,
            Is.EqualTo(new string[]
            {
                "address.catalog",
                "education.catalog",
                "internet.domain.documentation",
                "internet.ipv4.documentation-prefixes",
                "internet.ipv6.documentation-prefix",
                "person.identity",
                "person.names",
                "person.profile",
                "person.work",
                "phone.catalog",
                "phone.nanpa.fictional-subscriber-range",
                "text.words"
            }));
        Assert.That(
            locale.Coverage("en-US").FallbackSources,
            Is.EqualTo(new string[]
            {
                "invariant", "invariant", "invariant", "invariant", "invariant", "invariant", "invariant",
                "invariant", "invariant", "invariant", "invariant", "invariant"
            }));
    }

    [Test]
    public void SupportedReturnsCanonicalNativeTagsInOrdinalOrder()
    {
        LocaleProvider locale = Fake.Create().Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("fr-CA", "name", "value"),
            Value("EN-us", "date", "value"),
            Value("iw-IL", "direction", "value"),
            Value("en-US", "address", "value"),
            Value("invariant", "identifier", "value")
        };

        Assert.That(locale.SupportedFromValues(values), Is.EqualTo(new string[] { "en-US", "fr-CA", "he-IL" }));
    }

    [Test]
    public void IsSupportedRequiresNativeDataAndCanonicalizesAliases()
    {
        LocaleProvider locale = Fake.Create().Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("he-IL", "direction", "rtl"),
            Value("en", "word", "word")
        };

        Assert.That(locale.IsSupportedFromLocaleAndValues("iw-il", values), Is.True);
        Assert.That(locale.IsSupportedFromLocaleAndValues("en-US", values), Is.False);
        Assert.That(locale.IsSupportedFromLocaleAndValues("en", values), Is.True);
    }

    [Test]
    public void MetadataReportsCanonicalComponentsFieldsVersionAndProvenance()
    {
        LocaleProvider locale = Fake.Create().Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("sr-Cyrl-RS", "person.name", "name", "2026.08", "authored names"),
            Value("sr-Cyrl-RS", "date.order", "dmy", "2026.08", "Unicode CLDR"),
            Value("sr-Cyrl-RS", "text.direction", "ltr", "2026.08", "Unicode CLDR")
        };

        LocaleInfo info = locale.MetadataFromLocaleAndValues("SR-cyrl-rs", values);

        Assert.Multiple(() =>
        {
            Assert.That(info.Tag, Is.EqualTo("sr-Cyrl-RS"));
            Assert.That(info.Language, Is.EqualTo("sr"));
            Assert.That(info.Script, Is.EqualTo("Cyrl"));
            Assert.That(info.Territory, Is.EqualTo("RS"));
            Assert.That(info.Fields, Is.EqualTo(new string[] { "date.order", "person.name", "text.direction" }));
            Assert.That(info.DataVersion, Is.EqualTo("2026.08"));
            Assert.That(info.Provenance, Is.EqualTo(new string[] { "Unicode CLDR", "authored names" }));
        });
    }

    [Test]
    public void MetadataRejectsLocalesWithoutNativeFields()
    {
        LocaleProvider locale = Fake.Create().Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("fr", "word", "mot")
        };

        FakeException exception = Assert.Throws<FakeException>(() => locale.MetadataFromLocaleAndValues("fr-CA", values));

        Assert.That(exception.Message, Is.EqualTo("Locale 'fr-CA' has no native fields in the locale catalog."));
    }

    [Test]
    public void CoverageSeparatesNativeFieldsFromEveryFallbackTier()
    {
        LocaleProvider locale = Fake.Create(Options("fr-CA", "en-US", StrictnessMode.AllowFallback, "selected")).Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("fr-CA", "date.order", "ymd", "fr-v1", "native dates"),
            Value("fr", "person.name", "name", "fr-parent-v1", "parent names"),
            Value("en-US", "address.line", "address", "en-us-v1", "configured addresses"),
            Value("en", "text.word", "word", "en-v1", "configured words"),
            Value("invariant", "identifier.format", "id", "inv-v1", "standards")
        };

        LocaleCoverage coverage = locale.CoverageFromLocaleAndValues("fr-CA", values);

        Assert.Multiple(() =>
        {
            Assert.That(coverage.Locale, Is.EqualTo("fr-CA"));
            Assert.That(coverage.NativeFields, Is.EqualTo(new string[] { "date.order" }));
            Assert.That(coverage.FallbackFields, Is.EqualTo(new string[] { "address.line", "identifier.format", "person.name", "text.word" }));
            Assert.That(coverage.FallbackSources, Is.EqualTo(new string[] { "en-US", "invariant", "fr", "en" }));
            Assert.That(coverage.NativeFieldCount, Is.EqualTo(1));
            Assert.That(coverage.FallbackFieldCount, Is.EqualTo(4));
            Assert.That(coverage.DataVersions, Is.EqualTo(new string[] { "en-us-v1", "en-v1", "fr-parent-v1", "fr-v1", "inv-v1" }));
            Assert.That(coverage.Provenance, Is.EqualTo(new string[] { "configured addresses", "configured words", "native dates", "parent names", "standards" }));
        });
    }

    [Test]
    public void CoverageUsesTheWinningSourceForDuplicateFields()
    {
        LocaleProvider locale = Fake.Create(Options("fr-CA", "en", StrictnessMode.AllowFallback, "1")).Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("fr", "word", "parent"),
            Value("en", "word", "configured"),
            Value("invariant", "word", "invariant")
        };

        LocaleCoverage coverage = locale.CoverageFromLocaleAndValues("fr-CA", values);

        Assert.That(coverage.FallbackFields, Is.EqualTo(new string[] { "word" }));
        Assert.That(coverage.FallbackSources, Is.EqualTo(new string[] { "fr" }));
    }

    [Test]
    public void StrictCoverageReportsOnlyExactNativeFields()
    {
        LocaleProvider locale = Fake.Create(Options("fr-CA", "en", StrictnessMode.Strict, "1")).Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("fr-CA", "native", "value"),
            Value("fr", "parent", "value"),
            Value("invariant", "invariant", "value")
        };

        LocaleCoverage coverage = locale.CoverageFromLocaleAndValues("fr-CA", values);

        Assert.That(coverage.NativeFields, Is.EqualTo(new string[] { "native" }));
        Assert.That(coverage.FallbackFields, Is.Empty);
    }

    [Test]
    public void FallbackTraceReportsExactAndParentSources()
    {
        LocaleProvider locale = Fake.Create(Options("zh-Hant-HK", "en", StrictnessMode.AllowFallback, "1")).Locale();
        object exact = new object();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("zh-Hant-HK", "exact", exact, "zh-v2", "native exact"),
            Value("zh-Hant", "parent", "parent value", "zh-v1", "script parent")
        };

        FallbackTrace exactTrace = locale.FallbackTraceFromFieldAndValues("exact", values);
        FallbackTrace parentTrace = locale.FallbackTraceFromFieldAndValues("parent", values);

        Assert.Multiple(() =>
        {
            Assert.That(exactTrace.SourceLocale, Is.EqualTo("zh-Hant-HK"));
            Assert.That(exactTrace.AttemptedLocales, Is.EqualTo(new string[] { "zh-Hant-HK" }));
            Assert.That(exactTrace.UsedFallback, Is.False);
            Assert.That(exactTrace.Value, Is.SameAs(exact));
            Assert.That(exactTrace.DataVersion, Is.EqualTo("zh-v2"));
            Assert.That(exactTrace.Provenance, Is.EqualTo("native exact"));
            Assert.That(parentTrace.RequestedLocale, Is.EqualTo("zh-Hant-HK"));
            Assert.That(parentTrace.Field, Is.EqualTo("parent"));
            Assert.That(parentTrace.SourceLocale, Is.EqualTo("zh-Hant"));
            Assert.That(parentTrace.AttemptedLocales, Is.EqualTo(new string[] { "zh-Hant-HK", "zh-Hant" }));
            Assert.That(parentTrace.UsedFallback, Is.True);
        });
    }

    [Test]
    public void FallbackTraceReportsConfiguredAndInvariantSources()
    {
        LocaleProvider locale = Fake.Create(Options("fr-CA", "en-US", StrictnessMode.AllowFallback, "1")).Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("en-US", "configured", "configured value"),
            Value("invariant", "identifier", "invariant value")
        };

        FallbackTrace configured = locale.FallbackTraceFromFieldAndValues("configured", values);
        FallbackTrace invariant = locale.FallbackTraceFromFieldAndValues("identifier", values);

        Assert.That(configured.SourceLocale, Is.EqualTo("en-US"));
        Assert.That(configured.AttemptedLocales, Is.EqualTo(new string[] { "fr-CA", "fr", "en-US" }));
        Assert.That(invariant.SourceLocale, Is.EqualTo("invariant"));
        Assert.That(invariant.AttemptedLocales, Is.EqualTo(new string[] { "fr-CA", "fr", "en-US", "en", "invariant" }));
    }

    [Test]
    public void StrictTraceDoesNotInspectFallbackSources()
    {
        LocaleProvider locale = Fake.Create(Options("fr-CA", "en", StrictnessMode.Strict, "1")).Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("fr", "field", "parent")
        };

        FakeException exception = Assert.Throws<FakeException>(() => locale.FallbackTraceFromFieldAndValues("field", values));

        Assert.That(exception.Message, Is.EqualTo("Locale field 'field' is unavailable for exact locale 'fr-CA' because strict fallback is enabled."));
    }

    [Test]
    public void DefaultFieldVersionUsesTheCapturedOptionVersion()
    {
        FakeOptions options = Options("en-US", "en", StrictnessMode.AllowFallback, "catalog-2026.08");
        Fake fake = Fake.Create(options);
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("en-US", "field", "value")
        };
        options.DataVersion = "changed";

        Assert.That(fake.Locale().DataVersion(), Is.EqualTo("catalog-2026.08"));
        Assert.That(fake.Locale().MetadataFromLocaleAndValues("en-US", values).DataVersion, Is.EqualTo("catalog-2026.08"));
        Assert.That(fake.Locale().FallbackTraceFromFieldAndValues("field", values).DataVersion, Is.EqualTo("catalog-2026.08"));
    }

    [Test]
    public void CatalogRejectsInconsistentVersionsAndMutatedMetadata()
    {
        LocaleProvider locale = Fake.Create().Locale();
        LocaleFieldValue invalidProvenance = Value("en", "field", "value");
        invalidProvenance.Provenance = " ";
        LocaleFieldValue invalidVersion = Value("en", "field", "value");
        invalidVersion.DataVersion = " ";
        LocaleFieldValue[] inconsistent = new LocaleFieldValue[]
        {
            Value("en", "first", "value", "one", "source"),
            Value("en", "second", "value", "two", "source")
        };

        Assert.That(Assert.Throws<ArgumentException>(() => locale.SupportedFromValues(new LocaleFieldValue[] { invalidProvenance })).ParamName, Is.EqualTo("values"));
        Assert.That(Assert.Throws<ArgumentException>(() => locale.SupportedFromValues(new LocaleFieldValue[] { invalidVersion })).ParamName, Is.EqualTo("values"));
        Assert.That(Assert.Throws<ArgumentException>(() => locale.SupportedFromValues(inconsistent)).ParamName, Is.EqualTo("values"));
    }

    [Test]
    public void ExplicitMetadataConstructorValidatesVersionAndProvenance()
    {
        Assert.That(
            Assert.Throws<ArgumentNullException>(() => LocaleFieldValue.WithMetadata("en", "field", "value", null, "source")).ParamName,
            Is.EqualTo("dataVersion"));
        Assert.That(
            Assert.Throws<ArgumentException>(() => LocaleFieldValue.WithMetadata("en", "field", "value", " ", "source")).ParamName,
            Is.EqualTo("dataVersion"));
        Assert.That(
            Assert.Throws<ArgumentNullException>(() => LocaleFieldValue.WithMetadata("en", "field", "value", "one", null)).ParamName,
            Is.EqualTo("provenance"));
        Assert.That(
            Assert.Throws<ArgumentException>(() => LocaleFieldValue.WithMetadata("en", "field", "value", "one", " ")).ParamName,
            Is.EqualTo("provenance"));
    }

    [Test]
    public void CatalogInspectionAndTracingDoNotConsumeRandomEntropy()
    {
        Fake fake = Fake.Create(9001);
        FakeState before = fake.Snapshot();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("en-US", "field", "value")
        };

        Assert.That(fake.Locale().SupportedFromValues(values), Is.EqualTo(new string[] { "en-US" }));
        Assert.That(fake.Locale().CoverageFromLocaleAndValues("en-US", values).NativeFieldCount, Is.EqualTo(1));
        Assert.That(fake.Locale().FallbackTraceFromFieldAndValues("field", values).Value, Is.EqualTo("value"));
        AssertStateEqual(fake.Snapshot(), before);
    }

    static FakeOptions Options(
        string locale,
        string fallbackLocale,
        StrictnessMode strictness,
        string dataVersion)
    {
        FakeOptions options = FakeOptions.Create(locale, fallbackLocale, 0L);
        options.StrictnessMode = strictness;
        options.DataVersion = dataVersion;
        return options;
    }

    static LocaleFieldValue Value(string locale, string field, object value)
    {
        return new LocaleFieldValue(locale, field, value);
    }

    static LocaleFieldValue Value(
        string locale,
        string field,
        object value,
        string dataVersion,
        string provenance)
    {
        return LocaleFieldValue.WithMetadata(locale, field, value, dataVersion, provenance);
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
