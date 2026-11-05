using languagecodes;

namespace languagecodes.Tests;

public sealed class LanguageTagParserCoverageTests
{
    [TestCase("de", "de")]
    [TestCase("fr", "fr")]
    [TestCase("ja", "ja")]
    [TestCase("i-enochian", "i-enochian")]
    [TestCase("zh-Hant", "zh-Hant")]
    [TestCase("zh-Hans", "zh-Hans")]
    [TestCase("sr-Cyrl", "sr-Cyrl")]
    [TestCase("sr-Latn", "sr-Latn")]
    [TestCase("zh-cmn-Hans-CN", "zh-cmn-Hans-CN")]
    [TestCase("cmn-Hans-CN", "cmn-Hans-CN")]
    [TestCase("zh-yue-HK", "zh-yue-HK")]
    [TestCase("yue-HK", "yue-HK")]
    [TestCase("zh-Hans-CN", "zh-Hans-CN")]
    [TestCase("sr-Latn-RS", "sr-Latn-RS")]
    [TestCase("sl-rozaj", "sl-rozaj")]
    [TestCase("sl-rozaj-biske", "sl-rozaj-biske")]
    [TestCase("sl-nedis", "sl-nedis")]
    [TestCase("de-CH-1901", "de-CH-1901")]
    [TestCase("sl-IT-nedis", "sl-IT-nedis")]
    [TestCase("hy-Latn-IT-arevela", "hy-Latn-IT-arevela")]
    [TestCase("de-DE", "de-DE")]
    [TestCase("en-US", "en-US")]
    [TestCase("es-419", "es-419")]
    [TestCase("de-CH-x-phonebk", "de-CH-x-phonebk")]
    [TestCase("az-Arab-x-AZE-derbend", "az-Arab-x-aze-derbend")]
    [TestCase("x-whatever", "x-whatever")]
    [TestCase("qaa-Qaaa-QM-x-southern", "qaa-Qaaa-QM-x-southern")]
    [TestCase("de-Qaaa", "de-Qaaa")]
    [TestCase("sr-Latn-QM", "sr-Latn-QM")]
    [TestCase("sr-Qaaa-RS", "sr-Qaaa-RS")]
    [TestCase("en-US-u-islamcal", "en-US-u-islamcal")]
    [TestCase("zh-CN-a-myext-x-private", "zh-CN-a-myext-x-private")]
    [TestCase("en-a-myext-b-another", "en-a-myext-b-another")]
    public void Rfc5646AppendixAValidExamplesParse(
        string input,
        string expectedNormalized)
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageTag tag = service.ParseTag(input);

        Assert.Multiple(() =>
        {
            Assert.That(tag.Value, Is.EqualTo(input));
            Assert.That(tag.NormalizedValue, Is.EqualTo(expectedNormalized));
        });
    }

    [Test]
    public void EveryLangtagComponentAcceptsItsMinimumAndMaximumLengths()
    {
        LanguageCodeService service = new LanguageCodeService();
        const string input =
            "abcdefgh-Abcd-123-1abc-abcdefgh-a-ab-b-abcdefgh-x-a-abcdefgh";

        LanguageTag tag = service.ParseTag(input);
        LanguageTag extlangs = service.ParseTag("abc-def-ghi-jkl");

        Assert.Multiple(() =>
        {
            Assert.That(tag.Language, Is.EqualTo("abcdefgh"));
            Assert.That(tag.Script, Is.EqualTo("Abcd"));
            Assert.That(tag.Region, Is.EqualTo("123"));
            Assert.That(tag.Variants, Is.EqualTo(new[] { "1abc", "abcdefgh" }));
            Assert.That(tag.Extensions, Is.EqualTo(
                new[] { "a-ab", "b-abcdefgh" }));
            Assert.That(tag.PrivateUse, Is.EqualTo(new[] { "a", "abcdefgh" }));
            Assert.That(tag.NormalizedValue, Is.EqualTo(input));
            Assert.That(extlangs.Language, Is.EqualTo("abc"));
            Assert.That(extlangs.Extlangs, Is.EqualTo(new[] { "def", "ghi", "jkl" }));
        });
    }

    [Test]
    public void AlphabeticAndNumericRegionBoundariesNormalizeCasing()
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageTag alphabetic = service.ParseTag("EN-lATN-us");
        LanguageTag numeric = service.ParseTag("es-419");

        Assert.Multiple(() =>
        {
            Assert.That(alphabetic.Language, Is.EqualTo("en"));
            Assert.That(alphabetic.Script, Is.EqualTo("Latn"));
            Assert.That(alphabetic.Region, Is.EqualTo("US"));
            Assert.That(alphabetic.NormalizedValue, Is.EqualTo("en-Latn-US"));
            Assert.That(numeric.Region, Is.EqualTo("419"));
            Assert.That(numeric.NormalizedValue, Is.EqualTo("es-419"));
        });
    }

    [Test]
    public void GrandfatheredAndPrivateUseOnlyFormsRemainDistinct()
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageTag grandfathered = service.ParseTag("I-KLINGON");
        LanguageTag privateUse = service.ParseTag("X-A-ABCDEFGH");

        Assert.Multiple(() =>
        {
            Assert.That(grandfathered.NormalizedValue, Is.EqualTo("i-klingon"));
            Assert.That(grandfathered.Language, Is.Null);
            Assert.That(grandfathered.IsGrandfathered, Is.True);
            Assert.That(grandfathered.IsPrivateUseOnly, Is.False);
            Assert.That(privateUse.NormalizedValue, Is.EqualTo("x-a-abcdefgh"));
            Assert.That(privateUse.Language, Is.Null);
            Assert.That(privateUse.PrivateUse, Is.EqualTo(new[] { "a", "abcdefgh" }));
            Assert.That(privateUse.IsPrivateUseOnly, Is.True);
            Assert.That(privateUse.IsGrandfathered, Is.False);
        });
    }

    [Test]
    public void ExtensionSequencesPreserveOrderAndAllowDigitSingletons()
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageTag tag = service.ParseTag(
            "en-A-AB-abcdefgh-0-12-ABCDEFGH-b-another-X-private");

        Assert.Multiple(() =>
        {
            Assert.That(tag.Extensions, Is.EqualTo(new[]
            {
                "a-ab-abcdefgh",
                "0-12-abcdefgh",
                "b-another"
            }));
            Assert.That(tag.PrivateUse, Is.EqualTo(new[] { "private" }));
            Assert.That(tag.NormalizedValue, Is.EqualTo(
                "en-a-ab-abcdefgh-0-12-abcdefgh-b-another-x-private"));
        });
    }

    [Test]
    public void DuplicateVariantsAndSingletonsRemainParseableButInvalid()
    {
        LanguageCodeService service = new LanguageCodeService();
        const string input = "sl-rozaj-ROZAJ-u-ca-U-nu-latn";

        LanguageTag tag = service.ParseTag(input);
        LanguageValidationResult validation = service.ValidateTag(
            input,
            new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(tag.Variants, Is.EqualTo(new[] { "rozaj", "rozaj" }));
            Assert.That(tag.Extensions, Is.EqualTo(
                new[] { "u-ca", "u-nu-latn" }));
            Assert.That(validation.IsWellFormed, Is.True);
            Assert.That(validation.IsValid, Is.False);
            Assert.That(validation.Issues.Select(issue => issue.Error), Is.EqualTo(new[]
            {
                LanguageValidationError.DuplicateSubtag,
                LanguageValidationError.DuplicateSubtag
            }));
        });
    }

    [TestCase("a-DE")]
    [TestCase("abcdefghi")]
    [TestCase("abc-def-ghi-jkl-mno")]
    [TestCase("abcd-Abc")]
    [TestCase("abcd-12")]
    [TestCase("en-abcdefghi")]
    [TestCase("en-a")]
    [TestCase("en-a-a")]
    [TestCase("en-a-abcdefghi")]
    [TestCase("en-x")]
    [TestCase("en-x-abcdefghi")]
    public void RejectsComponentLengthsOutsideTheirAllowedBoundaries(string input)
    {
        LanguageCodeService service = new LanguageCodeService();

        Assert.Throws<InvalidLanguageTagException>(() => service.ParseTag(input));
    }

    [TestCase("de-419-DE")]
    [TestCase("en-US-Latn")]
    [TestCase("en-rozaj-US")]
    [TestCase("en-Latn-cmn")]
    [TestCase("-en")]
    [TestCase("en-")]
    [TestCase("en--US")]
    public void RejectsSubtagsInMalformedOrder(string input)
    {
        LanguageCodeService service = new LanguageCodeService();

        Assert.Throws<InvalidLanguageTagException>(() => service.ParseTag(input));
    }

    [TestCase("\u00e9n")]
    [TestCase("en-\u00e9\u00e9")]
    [TestCase("en\u2013US")]
    [TestCase("en-\ud83d\ude00")]
    public void RejectsNonAsciiInput(string input)
    {
        LanguageCodeService service = new LanguageCodeService();

        Assert.Throws<InvalidLanguageTagException>(() => service.ParseTag(input));
    }
}
