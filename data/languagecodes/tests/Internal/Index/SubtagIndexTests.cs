using languagecodes;

namespace languagecodes.Tests;

public class SubtagIndexTests
{
    [Test]
    public void AssignmentLookupIsCaseInsensitiveAndTypeSpecific()
    {
        IanaSubtagRow[] aaoRecords = SubtagIndex.FindRecordsByValue("AAO");

        Assert.Multiple(() =>
        {
            Assert.That(SubtagIndex.IsAssigned(LanguageSubtagType.Language, "AAO"), Is.True);
            Assert.That(SubtagIndex.IsAssigned(LanguageSubtagType.Extlang, "AAO"), Is.True);
            Assert.That(SubtagIndex.IsAssigned(LanguageSubtagType.Script, "latn"), Is.True);
            Assert.That(SubtagIndex.IsAssigned(LanguageSubtagType.Region, "us"), Is.True);
            Assert.That(SubtagIndex.IsAssigned(LanguageSubtagType.Variant, "1994"), Is.True);
            Assert.That(SubtagIndex.IsAssigned(LanguageSubtagType.Region, "not-a-region"), Is.False);
            Assert.That(aaoRecords.Select(record => record.RecordType), Does.Contain(LanguageSubtagType.Language));
            Assert.That(aaoRecords.Select(record => record.RecordType), Does.Contain(LanguageSubtagType.Extlang));
            Assert.That(
                SubtagIndex.FindRecord(LanguageSubtagType.Script, "latn")?.Value,
                Is.EqualTo("Latn"));
            Assert.That(
                SubtagIndex.FindRecord(LanguageSubtagType.Region, "us")?.Value,
                Is.EqualTo("US"));
        });
    }

    [Test]
    public void PrefixIndexSupportsExtlangAndVariantConstraints()
    {
        IanaSubtagRow[] resianRecords = SubtagIndex.FindRecordsForPrefix("SL-ROZAJ");
        IanaSubtagRow[] arabicRecords = SubtagIndex.FindRecordsForPrefix("AR");

        Assert.Multiple(() =>
        {
            Assert.That(
                resianRecords.Any(
                    record => record.RecordType == LanguageSubtagType.Variant &&
                        record.Value == "1994"),
                Is.True);
            Assert.That(
                arabicRecords.Any(
                    record => record.RecordType == LanguageSubtagType.Extlang &&
                        record.Value == "aao"),
                Is.True);
            Assert.That(
                SubtagIndex.AllowsPrefix(LanguageSubtagType.Variant, "1994", "sl-ROZAJ"),
                Is.True);
            Assert.That(
                SubtagIndex.AllowsPrefix(LanguageSubtagType.Extlang, "AAO", "AR"),
                Is.True);
            Assert.That(
                SubtagIndex.AllowsPrefix(LanguageSubtagType.Variant, "1994", "sl"),
                Is.False);
        });
    }

    [Test]
    public void PreferredValueIndexCoversSubtagsAndCompleteTags()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                SubtagIndex.FindPreferredValue(LanguageSubtagType.Language, "IW"),
                Is.EqualTo("he"));
            Assert.That(
                SubtagIndex.FindPreferredValue(LanguageSubtagType.Extlang, "AAO"),
                Is.EqualTo("aao"));
            Assert.That(
                SubtagIndex.FindPreferredValue(LanguageSubtagType.Grandfathered, "EN-gb-OED"),
                Is.EqualTo("en-GB-oxendict"));
            Assert.That(
                SubtagIndex.FindPreferredValue(LanguageSubtagType.Language, "en"),
                Is.Null);
        });
    }

    [Test]
    public void SuppressScriptIndexPreservesCanonicalScriptCasing()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SubtagIndex.FindSuppressScript("EN"), Is.EqualTo("Latn"));
            Assert.That(SubtagIndex.FindSuppressScript("IW"), Is.EqualTo("Hebr"));
            Assert.That(SubtagIndex.FindSuppressScript("aaa"), Is.Null);
        });
    }

    [Test]
    public void CompleteTagIndexesDistinguishGrandfatheredAndRedundantRecords()
    {
        IanaSubtagRow? grandfathered = SubtagIndex.FindGrandfatheredTag("EN-gb-OED");
        IanaSubtagRow? redundant = SubtagIndex.FindCompleteTag("ZH-CMN");

        Assert.Multiple(() =>
        {
            Assert.That(grandfathered?.RecordType, Is.EqualTo(LanguageSubtagType.Grandfathered));
            Assert.That(grandfathered?.Value, Is.EqualTo("en-GB-oed"));
            Assert.That(grandfathered?.PreferredValue, Is.EqualTo("en-GB-oxendict"));
            Assert.That(redundant?.RecordType, Is.EqualTo(LanguageSubtagType.Redundant));
            Assert.That(redundant?.PreferredValue, Is.EqualTo("cmn"));
            Assert.That(SubtagIndex.FindGrandfatheredTag("zh-cmn"), Is.Null);
            Assert.That(SubtagIndex.FindCompleteTag("not-registered"), Is.Null);
        });
    }

    [Test]
    public void CompleteTagTraversalKeysAreNormalizedAndOrdinallySorted()
    {
        string[] values = SubtagIndex.Data.SortedCompleteTagValues;

        Assert.Multiple(() =>
        {
            Assert.That(values, Does.Contain("en-gb-oed"));
            Assert.That(values, Does.Contain("zh-cmn"));
            Assert.That(values, Is.EqualTo(values.OrderBy(value => value, StringComparer.Ordinal)));
            Assert.That(SubtagIndexBuilder.NormalizeValue("EN-gb-OED"), Is.EqualTo("en-gb-oed"));
            Assert.That(
                SubtagIndex.Data.RecordIndexesByKey.Comparer,
                Is.SameAs(StringComparer.Ordinal));
            Assert.That(
                SubtagIndex.Data.RecordIndexesByPrefix.Comparer,
                Is.SameAs(StringComparer.Ordinal));
        });
    }

    [Test]
    public void IndexDoesNotRetainInputsAndLookupsReturnDefensiveRows()
    {
        IanaSubtagRow source = CreateRecord("testvar", new[] { "en" });
        IanaIndexSeedRow seed = new IanaIndexSeedRow(LanguageSubtagType.Variant, "testvar", 0);
        SubtagIndexData custom = SubtagIndexBuilder.BuildIndex(new[] { source }, new[] { seed });
        source.Value = "changed";
        source.Descriptions[0] = "changed";
        source.Prefixes[0] = "changed";

        IanaSubtagRow? first = SubtagIndex.FindRecord(LanguageSubtagType.Variant, "1994");
        IanaSubtagRow? second = SubtagIndex.FindRecord(LanguageSubtagType.Variant, "1994");
        first!.Descriptions[0] = "changed";
        first.Prefixes[0] = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(custom.Records[0].Value, Is.EqualTo("testvar"));
            Assert.That(custom.Records[0].Descriptions[0], Is.EqualTo("Description"));
            Assert.That(custom.Records[0].Prefixes[0], Is.EqualTo("en"));
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Descriptions, Is.Not.SameAs(second?.Descriptions));
            Assert.That(second?.Descriptions[0], Is.EqualTo("Standardized Resian orthography"));
            Assert.That(second?.Prefixes[0], Is.EqualTo("sl-rozaj"));
        });
    }

    [Test]
    public void InvalidMissingAndCollidingSeedsFailIndexConstruction()
    {
        IanaSubtagRow lower = CreateRecord("testvar", Array.Empty<string>());
        IanaSubtagRow upper = CreateRecord("TESTVAR", Array.Empty<string>());

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(
                () => SubtagIndexBuilder.BuildIndex(
                    new[] { lower },
                    new[] { new IanaIndexSeedRow(LanguageSubtagType.Variant, "wrong", 0) }));
            Assert.Throws<InvalidOperationException>(
                () => SubtagIndexBuilder.BuildIndex(new[] { lower }, Array.Empty<IanaIndexSeedRow>()));
            Assert.Throws<InvalidOperationException>(
                () => SubtagIndexBuilder.BuildIndex(
                    new[] { lower, upper },
                    new[]
                    {
                        new IanaIndexSeedRow(LanguageSubtagType.Variant, "testvar", 0),
                        new IanaIndexSeedRow(LanguageSubtagType.Variant, "TESTVAR", 1)
                    }));
        });
    }

    private static IanaSubtagRow CreateRecord(string value, string[] prefixes)
    {
        return new IanaSubtagRow(
            LanguageSubtagType.Variant,
            value,
            new[] { "Description" },
            "2000-01-01",
            null,
            null,
            prefixes,
            null,
            null,
            null,
            Array.Empty<string>());
    }
}
