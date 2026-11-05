using languagecodes;

namespace languagecodes.Tests;

public class LanguageTagParserTests
{
    [Test]
    public void ParsesEveryOrderedLangtagComponentAndNormalizesStructuralCasing()
    {
        const string input =
            "ZH-cmn-yue-gan-hANS-419-rozaj-1994-U-ca-GREGORY-T-en-X-Example-PRIVATE";

        LanguageTag tag = LanguageTagParser.ParseTag(input);

        Assert.Multiple(() =>
        {
            Assert.That(tag.Value, Is.EqualTo(input));
            Assert.That(tag.NormalizedValue, Is.EqualTo(
                "zh-cmn-yue-gan-Hans-419-rozaj-1994-u-ca-gregory-t-en-x-example-private"));
            Assert.That(tag.Language, Is.EqualTo("zh"));
            Assert.That(tag.Extlangs, Is.EqualTo(new[] { "cmn", "yue", "gan" }));
            Assert.That(tag.Script, Is.EqualTo("Hans"));
            Assert.That(tag.Region, Is.EqualTo("419"));
            Assert.That(tag.Variants, Is.EqualTo(new[] { "rozaj", "1994" }));
            Assert.That(tag.Extensions, Is.EqualTo(
                new[] { "u-ca-gregory", "t-en" }));
            Assert.That(tag.PrivateUse, Is.EqualTo(new[] { "example", "private" }));
            Assert.That(tag.IsPrivateUseOnly, Is.False);
            Assert.That(tag.IsGrandfathered, Is.False);
            Assert.That(tag.IsRedundant, Is.False);
        });
    }

    [TestCase("DE", "de")]
    [TestCase("abc", "abc")]
    [TestCase("ABCD", "abcd")]
    [TestCase("ABCDEFGH", "abcdefgh")]
    public void SupportsEveryPrimaryLanguageLength(string input, string expectedLanguage)
    {
        LanguageTag tag = LanguageTagParser.ParseTag(input);

        Assert.Multiple(() =>
        {
            Assert.That(tag.Language, Is.EqualTo(expectedLanguage));
            Assert.That(tag.NormalizedValue, Is.EqualTo(expectedLanguage));
        });
    }

    [Test]
    public void ParsesAlphabeticAndNumericRegionsAndDigitLedExtensions()
    {
        LanguageTag alphabetic = LanguageTagParser.ParseTag("en-lATN-us-0-ABC");
        LanguageTag numeric = LanguageTagParser.ParseTag("es-419");

        Assert.Multiple(() =>
        {
            Assert.That(alphabetic.Script, Is.EqualTo("Latn"));
            Assert.That(alphabetic.Region, Is.EqualTo("US"));
            Assert.That(alphabetic.Extensions, Is.EqualTo(new[] { "0-abc" }));
            Assert.That(alphabetic.NormalizedValue, Is.EqualTo("en-Latn-US-0-abc"));
            Assert.That(numeric.Region, Is.EqualTo("419"));
            Assert.That(numeric.NormalizedValue, Is.EqualTo("es-419"));
        });
    }

    [Test]
    public void ParsesPrivateUseOnlyTagsWithoutInventingALanguage()
    {
        LanguageTag tag = LanguageTagParser.ParseTag("X-Company-Internal-42");

        Assert.Multiple(() =>
        {
            Assert.That(tag.Value, Is.EqualTo("X-Company-Internal-42"));
            Assert.That(tag.NormalizedValue, Is.EqualTo("x-company-internal-42"));
            Assert.That(tag.Language, Is.Null);
            Assert.That(tag.PrivateUse, Is.EqualTo(
                new[] { "company", "internal", "42" }));
            Assert.That(tag.IsPrivateUseOnly, Is.True);
            Assert.That(tag.IsGrandfathered, Is.False);
        });
    }

    [TestCase("EN-gb-OED", "en-GB-oed")]
    [TestCase("I-KLINGON", "i-klingon")]
    [TestCase("ZH-min-NAN", "zh-min-nan")]
    public void GrandfatheredTagsRemainAtomicRegisteredValues(
        string input,
        string expectedNormalized)
    {
        LanguageTag tag = LanguageTagParser.ParseTag(input);

        Assert.Multiple(() =>
        {
            Assert.That(tag.Value, Is.EqualTo(input));
            Assert.That(tag.NormalizedValue, Is.EqualTo(expectedNormalized));
            Assert.That(tag.Language, Is.Null);
            Assert.That(tag.Extlangs, Is.Empty);
            Assert.That(tag.Variants, Is.Empty);
            Assert.That(tag.IsGrandfathered, Is.True);
            Assert.That(tag.IsRedundant, Is.False);
        });
    }

    [Test]
    public void RedundantRegistryTagsRetainOrdinaryParsedComponents()
    {
        LanguageTag tag = LanguageTagParser.ParseTag("SGN-us");

        Assert.Multiple(() =>
        {
            Assert.That(tag.NormalizedValue, Is.EqualTo("sgn-US"));
            Assert.That(tag.Language, Is.EqualTo("sgn"));
            Assert.That(tag.Region, Is.EqualTo("US"));
            Assert.That(tag.IsGrandfathered, Is.False);
            Assert.That(tag.IsRedundant, Is.True);
        });
    }

    [Test]
    public void EveryRegisteredCompleteTagParsesInItsDeclaredCategory()
    {
        foreach (string value in SubtagIndex.Data.GrandfatheredTagIndexesByValue.Keys)
        {
            LanguageTag tag = LanguageTagParser.ParseTag(value);
            if (!tag.IsGrandfathered)
            {
                Assert.Fail($"Grandfathered tag was not recognized: {value}");
            }
        }

        foreach (string value in SubtagIndex.Data.RedundantTagIndexesByValue.Keys)
        {
            LanguageTag tag = LanguageTagParser.ParseTag(value);
            if (!tag.IsRedundant)
            {
                Assert.Fail($"Redundant tag was not recognized: {value}");
            }
        }
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("x")]
    [TestCase("en-")]
    [TestCase("-en")]
    [TestCase("en--US")]
    [TestCase("e")]
    [TestCase("abcdefghi")]
    [TestCase("en_Latn_US")]
    [TestCase("en-12")]
    [TestCase("en-u")]
    [TestCase("en-x")]
    [TestCase("en-éé")]
    [TestCase("en-US-Latn")]
    [TestCase("en-cmn-yue-gan-abc")]
    public void RejectsValuesOutsideTheLangtagGrammar(string? input)
    {
        Assert.Throws<InvalidLanguageTagException>(() =>
            LanguageTagParser.ParseTag(input));
    }

    [Test]
    public void ParsedTagsAreRecursiveDefensiveSnapshots()
    {
        const string input = "zh-cmn-Hans-CN-rozaj-u-ca-gregory-x-private";
        LanguageTag first = LanguageTagParser.ParseTag(input);
        LanguageTag second = LanguageTagParser.ParseTag(input);

        first.Extlangs[0] = "changed";
        first.Variants[0] = "changed";
        first.Extensions[0] = "changed";
        first.PrivateUse[0] = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Extlangs, Is.Not.SameAs(second.Extlangs));
            Assert.That(second.Extlangs, Is.EqualTo(new[] { "cmn" }));
            Assert.That(second.Variants, Is.EqualTo(new[] { "rozaj" }));
            Assert.That(second.Extensions, Is.EqualTo(new[] { "u-ca-gregory" }));
            Assert.That(second.PrivateUse, Is.EqualTo(new[] { "private" }));
            Assert.That(LanguageTagParser.ParseTag(input).NormalizedValue, Is.EqualTo(input));
        });
    }
}
