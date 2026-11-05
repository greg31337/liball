using languagecodes;

namespace languagecodes.Tests;

public class LanguageDataInfoTests
{
    [Test]
    public void PublishedEditionVersionsAndSourceDatesMatchBundledSnapshot()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LanguageDataInfo.IsoEdition, Is.EqualTo("ISO 639:2023"));
            Assert.That(LanguageDataInfo.DataVersion, Is.EqualTo("iso6393-2026-07-15+iana-2026-08-08"));
            Assert.That(LanguageDataInfo.ManifestVersion, Is.EqualTo(1));
            Assert.That(LanguageDataInfo.GeneratorSchemaVersion, Is.EqualTo(1));
            Assert.That(LanguageDataInfo.RetrievalDate, Is.EqualTo("2026-08-27"));
            Assert.That(LanguageDataInfo.Iso639Set2UpstreamDate, Is.EqualTo("2024-07-19"));
            Assert.That(LanguageDataInfo.Iso639Set3CoreUpstreamDate, Is.EqualTo("2026-07-15"));
            Assert.That(LanguageDataInfo.Iso639Set3NamesUpstreamDate, Is.EqualTo("2026-07-15"));
            Assert.That(LanguageDataInfo.Iso639Set3MacrolanguagesUpstreamDate, Is.EqualTo("2026-07-15"));
            Assert.That(LanguageDataInfo.Iso639Set3RetirementsUpstreamDate, Is.EqualTo("2026-07-15"));
            Assert.That(LanguageDataInfo.Iso639Set5UpstreamDate, Is.EqualTo("2024-07-19"));
            Assert.That(LanguageDataInfo.Iso639Set5HierarchyUpstreamDate, Is.EqualTo("2024-07-19"));
            Assert.That(LanguageDataInfo.IanaRegistryUpstreamDate, Is.EqualTo("2026-08-08"));
            Assert.That(LanguageDataInfo.Rfc4647Version, Is.EqualTo("2006-09"));
            Assert.That(LanguageDataInfo.Rfc5646Version, Is.EqualTo("2009-09"));
        });
    }

    [Test]
    public void PublishedMetadataMatchesGeneratedMetadata()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LanguageDataInfo.IsoEdition, Is.EqualTo(GeneratedDataMetadata.IsoEdition));
            Assert.That(LanguageDataInfo.DataVersion, Is.EqualTo(GeneratedDataMetadata.DataVersion));
            Assert.That(LanguageDataInfo.ManifestVersion, Is.EqualTo(GeneratedDataMetadata.ManifestVersion));
            Assert.That(
                LanguageDataInfo.GeneratorSchemaVersion,
                Is.EqualTo(GeneratedDataMetadata.GeneratorSchemaVersion));
            Assert.That(LanguageDataInfo.RetrievalDate, Is.EqualTo(GeneratedDataMetadata.RetrievalDate));
            Assert.That(
                LanguageDataInfo.Iso639Set2UpstreamDate,
                Is.EqualTo(GeneratedDataMetadata.Iso639Set2UpstreamDate));
            Assert.That(
                LanguageDataInfo.Iso639Set3CoreUpstreamDate,
                Is.EqualTo(GeneratedDataMetadata.Iso639Set3CoreUpstreamDate));
            Assert.That(
                LanguageDataInfo.Iso639Set3NamesUpstreamDate,
                Is.EqualTo(GeneratedDataMetadata.Iso639Set3NamesUpstreamDate));
            Assert.That(
                LanguageDataInfo.Iso639Set3MacrolanguagesUpstreamDate,
                Is.EqualTo(GeneratedDataMetadata.Iso639Set3MacrolanguagesUpstreamDate));
            Assert.That(
                LanguageDataInfo.Iso639Set3RetirementsUpstreamDate,
                Is.EqualTo(GeneratedDataMetadata.Iso639Set3RetirementsUpstreamDate));
            Assert.That(
                LanguageDataInfo.Iso639Set5UpstreamDate,
                Is.EqualTo(GeneratedDataMetadata.Iso639Set5UpstreamDate));
            Assert.That(
                LanguageDataInfo.Iso639Set5HierarchyUpstreamDate,
                Is.EqualTo(GeneratedDataMetadata.Iso639Set5HierarchyUpstreamDate));
            Assert.That(
                LanguageDataInfo.IanaRegistryUpstreamDate,
                Is.EqualTo(GeneratedDataMetadata.IanaRegistryUpstreamDate));
            Assert.That(LanguageDataInfo.Rfc4647Version, Is.EqualTo(GeneratedDataMetadata.Rfc4647Version));
            Assert.That(LanguageDataInfo.Rfc5646Version, Is.EqualTo(GeneratedDataMetadata.Rfc5646Version));
        });
    }

    [Test]
    public void PublishedCountsMatchBundledSnapshot()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LanguageDataInfo.ActiveEntityCount, Is.EqualTo(8043));
            Assert.That(LanguageDataInfo.Set1RecordCount, Is.EqualTo(184));
            Assert.That(LanguageDataInfo.Set2RecordCount, Is.EqualTo(486));
            Assert.That(LanguageDataInfo.Set3RecordCount, Is.EqualTo(7927));
            Assert.That(LanguageDataInfo.Set5RecordCount, Is.EqualTo(115));
            Assert.That(LanguageDataInfo.RetiredCodeCount, Is.EqualTo(388));
            Assert.That(LanguageDataInfo.NameCount, Is.EqualTo(34628));
            Assert.That(LanguageDataInfo.AliasCount, Is.EqualTo(2659));
            Assert.That(LanguageDataInfo.RelationshipCount, Is.EqualTo(548));
            Assert.That(LanguageDataInfo.IanaRecordCount, Is.EqualTo(9296));
            Assert.That(LanguageDataInfo.LanguageSubtagCount, Is.EqualTo(8276));
            Assert.That(LanguageDataInfo.ExtlangSubtagCount, Is.EqualTo(258));
            Assert.That(LanguageDataInfo.ScriptSubtagCount, Is.EqualTo(225));
            Assert.That(LanguageDataInfo.RegionSubtagCount, Is.EqualTo(305));
            Assert.That(LanguageDataInfo.VariantSubtagCount, Is.EqualTo(139));
            Assert.That(LanguageDataInfo.GrandfatheredTagCount, Is.EqualTo(26));
            Assert.That(LanguageDataInfo.RedundantTagCount, Is.EqualTo(67));
            Assert.That(LanguageDataInfo.RegisteredSubtagCount, Is.EqualTo(9203));
        });
    }

    [Test]
    public void PublishedIsoAndAuxiliaryCountsMatchGeneratedRuntimeTables()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GeneratedLanguageEntries.Rows.Length, Is.EqualTo(LanguageDataInfo.ActiveEntityCount));
            Assert.That(
                GeneratedLanguageEntries.Rows.Count(row => row.Alpha2Code != null),
                Is.EqualTo(LanguageDataInfo.Set1RecordCount));
            Assert.That(
                GeneratedLanguageEntries.Rows.Count(row => row.Set2TerminologyCode != null),
                Is.EqualTo(LanguageDataInfo.Set2RecordCount));
            Assert.That(
                GeneratedLanguageEntries.Rows.Count(row => row.Set3Code != null),
                Is.EqualTo(LanguageDataInfo.Set3RecordCount));
            Assert.That(
                GeneratedLanguageEntries.Rows.Count(row => row.Set5Code != null),
                Is.EqualTo(LanguageDataInfo.Set5RecordCount));
            Assert.That(GeneratedRetirements.Rows.Length, Is.EqualTo(LanguageDataInfo.RetiredCodeCount));
            Assert.That(GeneratedLanguageNames.Rows.Length, Is.EqualTo(LanguageDataInfo.NameCount));
            Assert.That(GeneratedAliases.Rows.Length, Is.EqualTo(LanguageDataInfo.AliasCount));
            Assert.That(GeneratedRelationships.Rows.Length, Is.EqualTo(LanguageDataInfo.RelationshipCount));
        });
    }

    [Test]
    public void PublishedIanaCountsMatchEveryGeneratedRegistryCategory()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GeneratedIanaRegistry.Rows.Length, Is.EqualTo(LanguageDataInfo.IanaRecordCount));
            Assert.That(
                CountIanaRecords(LanguageSubtagType.Language),
                Is.EqualTo(LanguageDataInfo.LanguageSubtagCount));
            Assert.That(
                CountIanaRecords(LanguageSubtagType.Extlang),
                Is.EqualTo(LanguageDataInfo.ExtlangSubtagCount));
            Assert.That(
                CountIanaRecords(LanguageSubtagType.Script),
                Is.EqualTo(LanguageDataInfo.ScriptSubtagCount));
            Assert.That(
                CountIanaRecords(LanguageSubtagType.Region),
                Is.EqualTo(LanguageDataInfo.RegionSubtagCount));
            Assert.That(
                CountIanaRecords(LanguageSubtagType.Variant),
                Is.EqualTo(LanguageDataInfo.VariantSubtagCount));
            Assert.That(
                CountIanaRecords(LanguageSubtagType.Grandfathered),
                Is.EqualTo(LanguageDataInfo.GrandfatheredTagCount));
            Assert.That(
                CountIanaRecords(LanguageSubtagType.Redundant),
                Is.EqualTo(LanguageDataInfo.RedundantTagCount));
        });
    }

    [Test]
    public void PublishedIanaTotalsPartitionTheGeneratedRegistry()
    {
        int generatedRegisteredSubtagCount = GeneratedIanaRegistry.Rows.Count(
            row => row.RecordType == LanguageSubtagType.Language ||
                row.RecordType == LanguageSubtagType.Extlang ||
                row.RecordType == LanguageSubtagType.Script ||
                row.RecordType == LanguageSubtagType.Region ||
                row.RecordType == LanguageSubtagType.Variant);

        Assert.Multiple(() =>
        {
            Assert.That(generatedRegisteredSubtagCount, Is.EqualTo(LanguageDataInfo.RegisteredSubtagCount));
            Assert.That(
                LanguageDataInfo.RegisteredSubtagCount,
                Is.EqualTo(
                    LanguageDataInfo.LanguageSubtagCount +
                        LanguageDataInfo.ExtlangSubtagCount +
                        LanguageDataInfo.ScriptSubtagCount +
                        LanguageDataInfo.RegionSubtagCount +
                        LanguageDataInfo.VariantSubtagCount));
            Assert.That(
                GeneratedIanaRegistry.Rows.Length,
                Is.EqualTo(
                    LanguageDataInfo.RegisteredSubtagCount +
                        LanguageDataInfo.GrandfatheredTagCount +
                        LanguageDataInfo.RedundantTagCount));
        });
    }

    private static int CountIanaRecords(LanguageSubtagType recordType)
    {
        return GeneratedIanaRegistry.Rows.Count(row => row.RecordType == recordType);
    }
}
