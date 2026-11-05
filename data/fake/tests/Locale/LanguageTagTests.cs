using fake;

namespace fake.Tests;

[TestFixture]
public class LanguageTagTests
{
    [TestCase("EN-latn-us", "en-Latn-US")]
    [TestCase("sr-cYRL-rs", "sr-Cyrl-RS")]
    [TestCase("es-419", "es-419")]
    [TestCase("zxx-zsym", "zxx-Zsym")]
    public void NormalizeTagAppliesConventionalCasing(string input, string expected)
    {
        Assert.That(Fake.Create().Locale().NormalizeTag(input), Is.EqualTo(expected));
    }

    [Test]
    public void ParseTagReturnsCanonicalComponents()
    {
        LanguageTag tag = Fake.Create().Locale().ParseTag("SR-cyrl-rs-1994-U-ca-gregory-X-Test");

        Assert.Multiple(() =>
        {
            Assert.That(tag.Original, Is.EqualTo("SR-cyrl-rs-1994-U-ca-gregory-X-Test"));
            Assert.That(tag.Normalized, Is.EqualTo("sr-Cyrl-RS-1994-u-ca-gregory-x-test"));
            Assert.That(tag.Language, Is.EqualTo("sr"));
            Assert.That(tag.Extlangs, Is.Empty);
            Assert.That(tag.Script, Is.EqualTo("Cyrl"));
            Assert.That(tag.Region, Is.EqualTo("RS"));
            Assert.That(tag.Variants, Is.EqualTo(new string[] { "1994" }));
            Assert.That(tag.Extensions, Is.EqualTo(new string[] { "u-ca-gregory" }));
            Assert.That(tag.PrivateUse, Is.EqualTo(new string[] { "test" }));
            Assert.That(tag.Grandfathered, Is.False);
            Assert.That(tag.Aliased, Is.False);
        });
    }

    [Test]
    public void ExtensionsAreSortedAndPrivateUseRemainsLast()
    {
        string normalized = Fake.Create().Locale().NormalizeTag("en-B-CCC-BBB-a-DDD-X-US");

        Assert.That(normalized, Is.EqualTo("en-a-ddd-b-ccc-bbb-x-us"));
    }

    [TestCase("iw-IL", "he-IL")]
    [TestCase("in-ID", "id-ID")]
    [TestCase("en-BU", "en-MM")]
    [TestCase("ja-Latn-hepburn-heploc", "ja-Latn-hepburn-alalc97")]
    [TestCase("bh", "bih")]
    public void RegistryPreferredValuesCanonicalizeDeprecatedSubtags(string input, string expected)
    {
        LanguageTag tag = Fake.Create().Locale().ParseTag(input);

        Assert.That(tag.Normalized, Is.EqualTo(expected));
        Assert.That(tag.Aliased, Is.True);
    }

    [TestCase("i-klingon", "tlh")]
    [TestCase("en-GB-oed", "en-GB-oxendict")]
    [TestCase("sgn-BR", "bzs")]
    [TestCase("zh-cmn-Hans", "cmn-Hans")]
    public void WholeTagAliasesUseGrandfatheredAndRedundantPreferredValues(string input, string expected)
    {
        Assert.That(Fake.Create().Locale().NormalizeTag(input), Is.EqualTo(expected));
    }

    [Test]
    public void GrandfatheredTagWithoutPreferredValueRemainsAtomic()
    {
        LanguageTag tag = Fake.Create().Locale().ParseTag("I-DEFAULT");

        Assert.Multiple(() =>
        {
            Assert.That(tag.Normalized, Is.EqualTo("i-default"));
            Assert.That(tag.Language, Is.Null);
            Assert.That(tag.Grandfathered, Is.True);
            Assert.That(tag.Aliased, Is.False);
        });
    }

    [Test]
    public void ExtendedLanguageCanonicalizationRemovesPermittedPrefix()
    {
        LanguageTag tag = Fake.Create().Locale().ParseTag("zh-yue-Hant-HK");

        Assert.Multiple(() =>
        {
            Assert.That(tag.Normalized, Is.EqualTo("yue-Hant-HK"));
            Assert.That(tag.Language, Is.EqualTo("yue"));
            Assert.That(tag.Extlangs, Is.EqualTo(new string[] { "yue" }));
            Assert.That(tag.Aliased, Is.True);
        });
    }

    [TestCase("qaa-Qaaa-QM", "qaa-Qaaa-QM")]
    [TestCase("qtz-Qabx-XZ", "qtz-Qabx-XZ")]
    [TestCase("x-Pig-Latin", "x-pig-latin")]
    [TestCase("en-x-US", "en-x-us")]
    public void PrivateUseRangesAndSequencesAreValid(string input, string expected)
    {
        Assert.That(Fake.Create().Locale().NormalizeTag(input), Is.EqualTo(expected));
    }

    [Test]
    public void PrivateUseOnlyTagHasNoPrimaryLanguage()
    {
        LanguageTag tag = Fake.Create().Locale().ParseTag("X-company-Test1");

        Assert.That(tag.Normalized, Is.EqualTo("x-company-test1"));
        Assert.That(tag.Language, Is.Null);
        Assert.That(tag.PrivateUse, Is.EqualTo(new string[] { "company", "test1" }));
    }

    [TestCase(null, typeof(ArgumentNullException))]
    [TestCase("", typeof(ArgumentException))]
    [TestCase(" ", typeof(ArgumentException))]
    public void NullAndBlankTagsAreRejected(string? input, Type exceptionType)
    {
        Exception exception = Assert.Throws(exceptionType, () => Fake.Create().Locale().NormalizeTag(input));

        Assert.That(((ArgumentException)exception).ParamName, Is.EqualTo("tag"));
    }

    [TestCase("en_US")]
    [TestCase("en--US")]
    [TestCase("-en")]
    [TestCase("en-")]
    [TestCase("é-US")]
    [TestCase("en-abcdefghi")]
    [TestCase("x")]
    [TestCase("en-x")]
    [TestCase("en-a")]
    [TestCase("en-a-foo-A-bar")]
    [TestCase("de-1901-1901")]
    [TestCase("en-US-Latn")]
    public void MalformedTagsAreRejected(string input)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Fake.Create().Locale().NormalizeTag(input));

        Assert.That(exception.ParamName, Is.EqualTo("tag"));
        Assert.That(exception.Message, Does.Contain("Language tag"));
    }

    [TestCase("abcd")]
    [TestCase("en-Abcd")]
    [TestCase("en-BC")]
    [TestCase("en-unknown")]
    [TestCase("en-zzz")]
    public void UnregisteredSubtagsAreRejected(string input)
    {
        Assert.That(
            Assert.Throws<ArgumentException>(() => Fake.Create().Locale().NormalizeTag(input)).ParamName,
            Is.EqualTo("tag"));
    }

    [TestCase("en-yue")]
    [TestCase("zh-yue-cmn")]
    public void InvalidExtendedLanguagePrefixesAndReservedPositionsAreRejected(string input)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Fake.Create().Locale().NormalizeTag(input));

        Assert.That(exception.ParamName, Is.EqualTo("tag"));
        Assert.That(exception.Message, Does.Contain("extended-language"));
    }

    [Test]
    public void FakeCapturesCanonicalAndValidatedLocaleOptions()
    {
        FakeOptions options = new FakeOptions();
        options.Locale = "IW-il";
        options.FallbackLocale = "IN-id";

        Fake fake = Fake.Create(options);

        Assert.That(fake.OptionsSnapshot().Locale, Is.EqualTo("he-IL"));
        Assert.That(fake.OptionsSnapshot().FallbackLocale, Is.EqualTo("id-ID"));
        options.Locale = "abcd";
        Assert.That(Assert.Throws<ArgumentException>(() => Fake.Create(options)).ParamName, Is.EqualTo("Locale"));
    }

    [Test]
    public void ConcurrentParsingUsesTheSameImmutableRegistryData()
    {
        string[] results = new string[1000];

        Parallel.For(0, results.Length, index =>
        {
            results[index] = Fake.Create().Locale().NormalizeTag(index % 2 == 0 ? "iw-IL" : "zh-yue-Hant-HK");
        });

        Assert.That(results.Where((value, index) => index % 2 == 0), Has.All.EqualTo("he-IL"));
        Assert.That(results.Where((value, index) => index % 2 == 1), Has.All.EqualTo("yue-Hant-HK"));
    }
}
