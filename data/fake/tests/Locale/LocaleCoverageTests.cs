using fake;

namespace fake.Tests;

[TestFixture]
public class LocaleCoverageTests
{
    [Test]
    public void ReportGroupsExactWinningFieldsByProviderAndSourceKind()
    {
        LocaleProvider locale = Fake.Create(Options("fr-CA", "en-US", StrictnessMode.AllowFallback)).Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("fr-CA", "person.first", "Aline", "fr-ca-v1", "native names"),
            Value("fr-CA", "date.order", "dmy", "fr-ca-v1", "native dates"),
            Value("fr", "person.first", "parent shadow", "fr-v1", "parent names"),
            Value("fr", "person.last", "Martin", "fr-v1", "parent names"),
            Value("en-US", "address.city", "Example City", "en-us-v1", "configured addresses"),
            Value("invariant", "internet.domain", "example.com", "inv-v1", "standards")
        };

        LocaleCoverage report = locale.CoverageFromLocaleAndValues("fr-CA", values);

        Assert.Multiple(() =>
        {
            Assert.That(report.NativeFields, Is.EqualTo(new string[] { "date.order", "person.first" }));
            Assert.That(report.FallbackFields, Is.EqualTo(new string[] { "address.city", "internet.domain", "person.last" }));
            Assert.That(report.NativeProviders, Is.EqualTo(new string[] { "date", "person" }));
            Assert.That(report.FallbackOnlyProviders, Is.EqualTo(new string[] { "address", "internet" }));
            Assert.That(report.ProviderCount, Is.EqualTo(4));
            Assert.That(report.NativeProviderCount, Is.EqualTo(2));
            Assert.That(report.FallbackOnlyProviderCount, Is.EqualTo(2));
            Assert.That(report.Fields.Select(field => field.Field), Is.EqualTo(new string[]
            {
                "address.city",
                "date.order",
                "internet.domain",
                "person.first",
                "person.last"
            }));
            Assert.That(report.Providers.Select(provider => provider.Provider), Is.EqualTo(new string[]
            {
                "address",
                "date",
                "internet",
                "person"
            }));
        });

        LocaleFieldCoverage address = report.Fields.Single(field => field.Field == "address.city");
        LocaleFieldCoverage firstName = report.Fields.Single(field => field.Field == "person.first");
        LocaleProviderCoverage person = report.Providers.Single(provider => provider.Provider == "person");

        Assert.Multiple(() =>
        {
            Assert.That(address.Provider, Is.EqualTo("address"));
            Assert.That(address.SourceLocale, Is.EqualTo("en-US"));
            Assert.That(address.Native, Is.False);
            Assert.That(address.DataVersion, Is.EqualTo("en-us-v1"));
            Assert.That(address.Provenance, Is.EqualTo("configured addresses"));
            Assert.That(firstName.SourceLocale, Is.EqualTo("fr-CA"));
            Assert.That(firstName.Native, Is.True);
            Assert.That(firstName.DataVersion, Is.EqualTo("fr-ca-v1"));
            Assert.That(firstName.Provenance, Is.EqualTo("native names"));
            Assert.That(person.HasNativeCoverage, Is.True);
            Assert.That(person.HasFallbackCoverage, Is.True);
            Assert.That(person.NativeFields, Is.EqualTo(new string[] { "person.first" }));
            Assert.That(person.FallbackFields, Is.EqualTo(new string[] { "person.last" }));
            Assert.That(person.FallbackSources, Is.EqualTo(new string[] { "fr" }));
            Assert.That(person.NativeFieldCount, Is.EqualTo(1));
            Assert.That(person.FallbackFieldCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void BundledWaveOneProvidersSeparateNativeAndFallbackCoverage()
    {
        LocaleProvider locale = Fake.Create().Locale();

        LocaleCoverage report = locale.Coverage("en-US");

        Assert.Multiple(() =>
        {
            Assert.That(locale.IsSupported("en-US"), Is.True);
            Assert.That(report.NativeFields, Has.Length.EqualTo(7));
            Assert.That(report.NativeProviders, Is.EqualTo(new string[] { "date", "locale" }));
            Assert.That(report.NativeProviderCount, Is.EqualTo(2));
            Assert.That(report.FallbackOnlyProviders, Is.EqualTo(new string[] { "address", "education", "internet", "person", "phone", "text" }));
            Assert.That(report.FallbackOnlyProviderCount, Is.EqualTo(6));
            Assert.That(report.Fields.Where(field => field.Native), Has.All.Matches<LocaleFieldCoverage>(field => field.SourceLocale == "en-US"));
            Assert.That(report.Fields.Where(field => !field.Native), Has.All.Matches<LocaleFieldCoverage>(field => field.SourceLocale == "invariant"));
        });
    }

    [Test]
    public void StrictReportContainsOnlyExactNativeProviderCoverage()
    {
        LocaleProvider locale = Fake.Create(Options("fr-CA", "en", StrictnessMode.Strict)).Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("fr-CA", "address.city", "Ville", "fr-ca-v1", "native address"),
            Value("fr", "address.postal-code", "A1A 1A1", "fr-v1", "parent address"),
            Value("invariant", "internet.domain", "example.com", "inv-v1", "standards")
        };

        LocaleCoverage report = locale.CoverageFromLocaleAndValues("fr-CA", values);

        Assert.Multiple(() =>
        {
            Assert.That(report.NativeProviders, Is.EqualTo(new string[] { "address" }));
            Assert.That(report.FallbackOnlyProviders, Is.Empty);
            Assert.That(report.NativeProviderCount, Is.EqualTo(1));
            Assert.That(report.FallbackOnlyProviderCount, Is.Zero);
            Assert.That(report.Fields, Has.Length.EqualTo(1));
            Assert.That(report.Fields[0].Field, Is.EqualTo("address.city"));
            Assert.That(report.Fields[0].Native, Is.True);
            Assert.That(report.Providers[0].HasNativeCoverage, Is.True);
            Assert.That(report.Providers[0].HasFallbackCoverage, Is.False);
            Assert.That(report.Providers[0].FallbackFields, Is.Empty);
        });
    }

    [Test]
    public void ProviderNameUsesTheWholeFieldWhenNoSeparatorExists()
    {
        LocaleProvider locale = Fake.Create().Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("en-US", "word", "hello", "one", "native word")
        };

        LocaleCoverage report = locale.CoverageFromLocaleAndValues("en-US", values);

        Assert.That(report.NativeProviders, Is.EqualTo(new string[] { "word" }));
        Assert.That(report.Fields[0].Provider, Is.EqualTo("word"));
        Assert.That(report.Providers[0].Provider, Is.EqualTo("word"));
    }

    static FakeOptions Options(string locale, string fallbackLocale, StrictnessMode strictness)
    {
        FakeOptions options = FakeOptions.Create(locale, fallbackLocale, 0L);
        options.StrictnessMode = strictness;
        options.DataVersion = "selected";
        return options;
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
}
