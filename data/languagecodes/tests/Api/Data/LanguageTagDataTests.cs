using languagecodes;

namespace languagecodes.Tests;

public class LanguageTagDataTests
{
    [Test]
    public void LanguageTagRepresentsEveryRfc5646Component()
    {
        LanguageTag tag = new LanguageTag
        {
            Value = "zh-cmn-Hans-CN-rozaj-u-ca-gregory-t-en-x-example-private",
            NormalizedValue = "zh-cmn-Hans-CN-rozaj-u-ca-gregory-t-en-x-example-private",
            Language = "zh",
            Extlangs = new[] { "cmn" },
            Script = "Hans",
            Region = "CN",
            Variants = new[] { "rozaj" },
            Extensions = new[] { "u-ca-gregory", "t-en" },
            PrivateUse = new[] { "example", "private" },
            IsPrivateUseOnly = false,
            IsGrandfathered = false,
            IsRedundant = false
        };

        Assert.Multiple(() =>
        {
            Assert.That(tag.Value, Is.EqualTo("zh-cmn-Hans-CN-rozaj-u-ca-gregory-t-en-x-example-private"));
            Assert.That(tag.NormalizedValue, Is.EqualTo(tag.Value));
            Assert.That(tag.Language, Is.EqualTo("zh"));
            Assert.That(tag.Extlangs, Is.EqualTo(new[] { "cmn" }));
            Assert.That(tag.Script, Is.EqualTo("Hans"));
            Assert.That(tag.Region, Is.EqualTo("CN"));
            Assert.That(tag.Variants, Is.EqualTo(new[] { "rozaj" }));
            Assert.That(tag.Extensions, Is.EqualTo(new[] { "u-ca-gregory", "t-en" }));
            Assert.That(tag.PrivateUse, Is.EqualTo(new[] { "example", "private" }));
            Assert.That(tag.IsPrivateUseOnly, Is.False);
            Assert.That(tag.IsGrandfathered, Is.False);
            Assert.That(tag.IsRedundant, Is.False);
        });
    }

    [Test]
    public void LanguageTagRepresentsPrivateUseOnlyAndGrandfatheredForms()
    {
        LanguageTag privateUse = new LanguageTag
        {
            Value = "x-company-internal",
            NormalizedValue = "x-company-internal",
            PrivateUse = new[] { "company", "internal" },
            IsPrivateUseOnly = true
        };
        LanguageTag grandfathered = new LanguageTag
        {
            Value = "en-GB-oed",
            NormalizedValue = "en-GB-oed",
            IsGrandfathered = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(privateUse.Language, Is.Null);
            Assert.That(privateUse.PrivateUse, Is.EqualTo(new[] { "company", "internal" }));
            Assert.That(privateUse.IsPrivateUseOnly, Is.True);
            Assert.That(grandfathered.Value, Is.EqualTo("en-GB-oed"));
            Assert.That(grandfathered.IsGrandfathered, Is.True);
        });
    }

    [Test]
    public void SubtagMetadataPreservesCompleteRegistryRecord()
    {
        LanguageSubtagMetadata metadata = new LanguageSubtagMetadata
        {
            Type = LanguageSubtagType.Language,
            Value = "iw",
            Descriptions = new[] { "Hebrew" },
            AddedDate = "2005-10-16",
            DeprecatedDate = "1989-01-01",
            PreferredValue = "he",
            Prefixes = new string[0],
            SuppressScript = "Hebr",
            Macrolanguage = null,
            Scope = null,
            Comments = new[] { "Deprecated ISO identifier." }
        };

        Assert.Multiple(() =>
        {
            Assert.That(metadata.Type, Is.EqualTo(LanguageSubtagType.Language));
            Assert.That(metadata.Value, Is.EqualTo("iw"));
            Assert.That(metadata.Descriptions, Is.EqualTo(new[] { "Hebrew" }));
            Assert.That(metadata.AddedDate, Is.EqualTo("2005-10-16"));
            Assert.That(metadata.DeprecatedDate, Is.EqualTo("1989-01-01"));
            Assert.That(metadata.PreferredValue, Is.EqualTo("he"));
            Assert.That(metadata.Prefixes, Is.Empty);
            Assert.That(metadata.SuppressScript, Is.EqualTo("Hebr"));
            Assert.That(metadata.Macrolanguage, Is.Null);
            Assert.That(metadata.Scope, Is.Null);
            Assert.That(metadata.Comments, Is.EqualTo(new[] { "Deprecated ISO identifier." }));
        });
    }

    [Test]
    public void SubtagMetadataPreservesRepeatedDescriptionsAndPrefixes()
    {
        LanguageSubtagMetadata metadata = new LanguageSubtagMetadata
        {
            Type = LanguageSubtagType.Variant,
            Value = "1994",
            Descriptions = new[] { "Standardized Resian orthography", "Alternative description" },
            AddedDate = "2007-07-28",
            Prefixes = new[] { "sl-rozaj", "sl-rozaj-biske", "sl-rozaj-njiva" },
            Comments = new[] { "Retains source ordering." }
        };

        Assert.Multiple(() =>
        {
            Assert.That(metadata.Descriptions, Has.Length.EqualTo(2));
            Assert.That(metadata.Prefixes, Has.Length.EqualTo(3));
            Assert.That(metadata.Prefixes[0], Is.EqualTo("sl-rozaj"));
            Assert.That(metadata.Comments, Is.EqualTo(new[] { "Retains source ordering." }));
        });
    }

    [Test]
    public void TagMatchResultRepresentsMatchedAndNoMatchOutcomes()
    {
        LanguageTag parsed = new LanguageTag
        {
            Value = "de-CH",
            NormalizedValue = "de-CH",
            Language = "de",
            Region = "CH"
        };
        LanguageTagMatchResult matched = new LanguageTagMatchResult
        {
            Status = LanguageTagMatchStatus.Matched,
            MatchedTag = "de-CH",
            MatchedIndex = 2,
            NormalizedRange = "de-CH",
            ParsedTag = parsed
        };
        LanguageTagMatchResult noMatch = new LanguageTagMatchResult
        {
            Status = LanguageTagMatchStatus.NoMatch,
            NormalizedRange = "fr-CA"
        };

        Assert.Multiple(() =>
        {
            Assert.That(matched.Status, Is.EqualTo(LanguageTagMatchStatus.Matched));
            Assert.That(matched.MatchedTag, Is.EqualTo("de-CH"));
            Assert.That(matched.MatchedIndex, Is.EqualTo(2));
            Assert.That(matched.NormalizedRange, Is.EqualTo("de-CH"));
            Assert.That(matched.ParsedTag, Is.SameAs(parsed));
            Assert.That(noMatch.Status, Is.EqualTo(LanguageTagMatchStatus.NoMatch));
            Assert.That(noMatch.MatchedTag, Is.Null);
            Assert.That(noMatch.MatchedIndex, Is.EqualTo(-1));
            Assert.That(noMatch.ParsedTag, Is.Null);
        });
    }

    [Test]
    public void TagAndMetadataInstancesDoNotShareArrays()
    {
        LanguageTag firstTag = new LanguageTag();
        LanguageTag secondTag = new LanguageTag();
        LanguageSubtagMetadata firstMetadata = new LanguageSubtagMetadata();
        LanguageSubtagMetadata secondMetadata = new LanguageSubtagMetadata();

        Assert.Multiple(() =>
        {
            Assert.That(firstTag.Extlangs, Is.Not.SameAs(secondTag.Extlangs));
            Assert.That(firstTag.Variants, Is.Not.SameAs(secondTag.Variants));
            Assert.That(firstTag.Extensions, Is.Not.SameAs(secondTag.Extensions));
            Assert.That(firstTag.PrivateUse, Is.Not.SameAs(secondTag.PrivateUse));
            Assert.That(firstMetadata.Descriptions, Is.Not.SameAs(secondMetadata.Descriptions));
            Assert.That(firstMetadata.Prefixes, Is.Not.SameAs(secondMetadata.Prefixes));
            Assert.That(firstMetadata.Comments, Is.Not.SameAs(secondMetadata.Comments));
        });
    }

    [Test]
    public void TagMatchOperationRowKeepsTagMetadataAndResultTogether()
    {
        LanguageTag tag = new LanguageTag { Value = "en" };
        LanguageSubtagMetadata metadata = new LanguageSubtagMetadata { Value = "en" };
        LanguageTagMatchResult result = new LanguageTagMatchResult
        {
            Status = LanguageTagMatchStatus.Matched,
            MatchedTag = "en",
            MatchedIndex = 0,
            ParsedTag = tag
        };
        TagMatchOperationRow operation = new TagMatchOperationRow(tag, metadata, result);

        Assert.Multiple(() =>
        {
            Assert.That(operation.Tag, Is.SameAs(tag));
            Assert.That(operation.Metadata, Is.SameAs(metadata));
            Assert.That(operation.Result, Is.SameAs(result));
        });
    }
}
