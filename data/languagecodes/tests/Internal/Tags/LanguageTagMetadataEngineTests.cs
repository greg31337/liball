using languagecodes;

namespace languagecodes.Tests;

public class LanguageTagMetadataEngineTests
{
    [Test]
    public void ReturnsCompleteLanguageMetadataWithCanonicalRegistryCasing()
    {
        LanguageSubtagMetadata? metadata =
            LanguageLookupEngine.GetLanguageSubtagMetadata(
                LanguageSubtagType.Language,
                "IW");

        Assert.Multiple(() =>
        {
            Assert.That(metadata, Is.Not.Null);
            Assert.That(metadata?.Type, Is.EqualTo(LanguageSubtagType.Language));
            Assert.That(metadata?.Value, Is.EqualTo("iw"));
            Assert.That(metadata?.Descriptions, Is.EqualTo(new[] { "Hebrew" }));
            Assert.That(metadata?.AddedDate, Is.EqualTo("2005-10-16"));
            Assert.That(metadata?.DeprecatedDate, Is.EqualTo("1989-01-01"));
            Assert.That(metadata?.PreferredValue, Is.EqualTo("he"));
            Assert.That(metadata?.SuppressScript, Is.EqualTo("Hebr"));
            Assert.That(metadata?.Prefixes, Is.Empty);
        });
    }

    [Test]
    public void ReturnsExtlangPrefixAndMacrolanguageMetadata()
    {
        LanguageSubtagMetadata? metadata =
            LanguageLookupEngine.GetLanguageSubtagMetadata(
                LanguageSubtagType.Extlang,
                "AAO");

        Assert.Multiple(() =>
        {
            Assert.That(metadata?.Type, Is.EqualTo(LanguageSubtagType.Extlang));
            Assert.That(metadata?.Value, Is.EqualTo("aao"));
            Assert.That(metadata?.Descriptions, Is.EqualTo(
                new[] { "Algerian Saharan Arabic" }));
            Assert.That(metadata?.PreferredValue, Is.EqualTo("aao"));
            Assert.That(metadata?.Prefixes, Is.EqualTo(new[] { "ar" }));
            Assert.That(metadata?.Macrolanguage, Is.EqualTo("ar"));
        });
    }

    [Test]
    public void ScriptsRegionsAndVariantsRemainIanaMetadataRecords()
    {
        LanguageSubtagMetadata? script =
            LanguageLookupEngine.GetLanguageSubtagMetadata(
                LanguageSubtagType.Script,
                "latn");
        LanguageSubtagMetadata? region =
            LanguageLookupEngine.GetLanguageSubtagMetadata(
                LanguageSubtagType.Region,
                "bu");
        LanguageSubtagMetadata? variant =
            LanguageLookupEngine.GetLanguageSubtagMetadata(
                LanguageSubtagType.Variant,
                "1994");
        LanguageLookupResult scriptAsLanguage =
            LanguageLookupEngine.LookupPreferredCode(
                "Latn",
                new LanguageLookupOptions
                {
                    NormalizeCase = true
                });

        Assert.Multiple(() =>
        {
            Assert.That(script?.Type, Is.EqualTo(LanguageSubtagType.Script));
            Assert.That(script?.Value, Is.EqualTo("Latn"));
            Assert.That(script?.Descriptions, Is.EqualTo(new[] { "Latin" }));
            Assert.That(region?.Type, Is.EqualTo(LanguageSubtagType.Region));
            Assert.That(region?.Value, Is.EqualTo("BU"));
            Assert.That(region?.PreferredValue, Is.EqualTo("MM"));
            Assert.That(variant?.Type, Is.EqualTo(LanguageSubtagType.Variant));
            Assert.That(variant?.Prefixes, Does.Contain("sl-rozaj"));
            Assert.That(variant?.Comments, Is.Not.Empty);
            Assert.That(scriptAsLanguage.Status, Is.EqualTo(
                LanguageLookupStatus.NotFound));
            Assert.That(scriptAsLanguage.Entry, Is.Null);
        });
    }

    [Test]
    public void ReturnsGrandfatheredAndRedundantCompleteTagMetadata()
    {
        LanguageSubtagMetadata? grandfathered =
            LanguageLookupEngine.GetLanguageSubtagMetadata(
                LanguageSubtagType.Grandfathered,
                "EN-gb-OED");
        LanguageSubtagMetadata? redundant =
            LanguageLookupEngine.GetLanguageSubtagMetadata(
                LanguageSubtagType.Redundant,
                "SGN-us");

        Assert.Multiple(() =>
        {
            Assert.That(grandfathered?.Value, Is.EqualTo("en-GB-oed"));
            Assert.That(grandfathered?.DeprecatedDate, Is.EqualTo("2015-04-17"));
            Assert.That(grandfathered?.PreferredValue, Is.EqualTo("en-GB-oxendict"));
            Assert.That(redundant?.Value, Is.EqualTo("sgn-US"));
            Assert.That(redundant?.PreferredValue, Is.EqualTo("ase"));
        });
    }

    [TestCase(LanguageSubtagType.Language, "qaa", "qaa..qtz")]
    [TestCase(LanguageSubtagType.Language, "QTZ", "qaa..qtz")]
    [TestCase(LanguageSubtagType.Script, "qaaa", "Qaaa..Qabx")]
    [TestCase(LanguageSubtagType.Script, "QABX", "Qaaa..Qabx")]
    [TestCase(LanguageSubtagType.Region, "qm", "QM..QZ")]
    [TestCase(LanguageSubtagType.Region, "QZ", "QM..QZ")]
    [TestCase(LanguageSubtagType.Region, "xa", "XA..XZ")]
    [TestCase(LanguageSubtagType.Region, "XZ", "XA..XZ")]
    public void ResolvesPrivateUseMembersToTheirRegistryRangeRecords(
        LanguageSubtagType type,
        string value,
        string expectedRange)
    {
        LanguageSubtagMetadata? metadata =
            LanguageLookupEngine.GetLanguageSubtagMetadata(type, value);

        Assert.Multiple(() =>
        {
            Assert.That(metadata?.Type, Is.EqualTo(type));
            Assert.That(metadata?.Value, Is.EqualTo(expectedRange));
            Assert.That(metadata?.Descriptions, Is.EqualTo(new[] { "Private use" }));
        });
    }

    [Test]
    public void SupportsExactPrivateUseRecordsAndRangeRecordKeys()
    {
        LanguageSubtagMetadata? exactRegion =
            LanguageLookupEngine.GetLanguageSubtagMetadata(
                LanguageSubtagType.Region,
                "aa");
        LanguageSubtagMetadata? rangeKey =
            LanguageLookupEngine.GetLanguageSubtagMetadata(
                LanguageSubtagType.Language,
                "QAA..QTZ");

        Assert.Multiple(() =>
        {
            Assert.That(exactRegion?.Value, Is.EqualTo("AA"));
            Assert.That(exactRegion?.Descriptions, Is.EqualTo(new[] { "Private use" }));
            Assert.That(rangeKey?.Value, Is.EqualTo("qaa..qtz"));
            Assert.That(rangeKey?.Scope, Is.EqualTo("private-use"));
        });
    }

    [Test]
    public void ReturnsNullForMissingMalformedOrNonregistryRecordTypes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LanguageLookupEngine.GetLanguageSubtagMetadata(
                LanguageSubtagType.Language,
                null), Is.Null);
            Assert.That(LanguageLookupEngine.GetLanguageSubtagMetadata(
                LanguageSubtagType.Language,
                string.Empty), Is.Null);
            Assert.That(LanguageLookupEngine.GetLanguageSubtagMetadata(
                LanguageSubtagType.Language,
                "notknown"), Is.Null);
            Assert.That(LanguageLookupEngine.GetLanguageSubtagMetadata(
                LanguageSubtagType.Language,
                "Latn"), Is.Null);
            Assert.That(LanguageLookupEngine.GetLanguageSubtagMetadata(
                LanguageSubtagType.Language,
                "qa{"), Is.Null);
            Assert.That(LanguageLookupEngine.GetLanguageSubtagMetadata(
                LanguageSubtagType.Extension,
                "u"), Is.Null);
            Assert.That(LanguageLookupEngine.GetLanguageSubtagMetadata(
                LanguageSubtagType.PrivateUse,
                "x"), Is.Null);
        });
    }

    [Test]
    public void MetadataLookupsReturnRecursiveDefensiveSnapshots()
    {
        LanguageSubtagMetadata first =
            LanguageLookupEngine.GetLanguageSubtagMetadata(
                LanguageSubtagType.Variant,
                "1994")!;
        LanguageSubtagMetadata second =
            LanguageLookupEngine.GetLanguageSubtagMetadata(
                LanguageSubtagType.Variant,
                "1994")!;

        first.Descriptions[0] = "changed";
        first.Prefixes[0] = "changed";
        first.Comments[0] = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Descriptions, Is.Not.SameAs(second.Descriptions));
            Assert.That(first.Prefixes, Is.Not.SameAs(second.Prefixes));
            Assert.That(first.Comments, Is.Not.SameAs(second.Comments));
            Assert.That(second.Descriptions[0], Is.EqualTo(
                "Standardized Resian orthography"));
            Assert.That(second.Prefixes[0], Is.EqualTo("sl-rozaj"));
            Assert.That(second.Comments[0], Does.Contain("orthography"));
        });
    }
}
