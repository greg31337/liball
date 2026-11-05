using languagecodes;

namespace languagecodes.Tests;

public class GeneratedDataTests
{
    [Test]
    public void GeneratedMetadataMatchesRuntimeTablesAndPinnedVersions()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GeneratedDataMetadata.IsoEdition, Is.EqualTo("ISO 639:2023"));
            Assert.That(
                GeneratedDataMetadata.DataVersion,
                Is.EqualTo("iso6393-2026-07-15+iana-2026-08-08"));
            Assert.That(GeneratedDataMetadata.ManifestVersion, Is.EqualTo(1));
            Assert.That(GeneratedDataMetadata.GeneratorSchemaVersion, Is.EqualTo(1));
            Assert.That(GeneratedDataMetadata.RetrievalDate, Is.EqualTo("2026-08-27"));
            Assert.That(GeneratedDataMetadata.Iso639Set2UpstreamDate, Is.EqualTo("2024-07-19"));
            Assert.That(GeneratedDataMetadata.Iso639Set3CoreUpstreamDate, Is.EqualTo("2026-07-15"));
            Assert.That(GeneratedDataMetadata.Iso639Set5UpstreamDate, Is.EqualTo("2024-07-19"));
            Assert.That(GeneratedDataMetadata.IanaRegistryUpstreamDate, Is.EqualTo("2026-08-08"));
            Assert.That(GeneratedLanguageEntries.Rows.Length, Is.EqualTo(GeneratedDataMetadata.ActiveEntityCount));
            Assert.That(
                GeneratedLanguageEntries.Rows.Count(row => row.Alpha2Code != null),
                Is.EqualTo(GeneratedDataMetadata.Set1RecordCount));
            Assert.That(
                GeneratedLanguageEntries.Rows.Count(row => row.Set2TerminologyCode != null),
                Is.EqualTo(GeneratedDataMetadata.Set2RecordCount));
            Assert.That(
                GeneratedLanguageEntries.Rows.Count(row => row.Set3Code != null),
                Is.EqualTo(GeneratedDataMetadata.Set3RecordCount));
            Assert.That(
                GeneratedLanguageEntries.Rows.Count(row => row.Set5Code != null),
                Is.EqualTo(GeneratedDataMetadata.Set5RecordCount));
            Assert.That(GeneratedRetirements.Rows.Length, Is.EqualTo(GeneratedDataMetadata.RetiredCodeCount));
            Assert.That(GeneratedLanguageNames.Rows.Length, Is.EqualTo(GeneratedDataMetadata.NameCount));
            Assert.That(GeneratedAliases.Rows.Length, Is.EqualTo(GeneratedDataMetadata.AliasCount));
            Assert.That(GeneratedRelationships.Rows.Length, Is.EqualTo(GeneratedDataMetadata.RelationshipCount));
            Assert.That(GeneratedIanaRegistry.Rows.Length, Is.EqualTo(GeneratedDataMetadata.IanaRecordCount));
            Assert.That(
                GeneratedIanaRegistry.Rows.Count(row => row.RecordType == LanguageSubtagType.Language),
                Is.EqualTo(GeneratedDataMetadata.LanguageSubtagCount));
            Assert.That(
                GeneratedIanaRegistry.Rows.Count(row => row.RecordType == LanguageSubtagType.Extlang),
                Is.EqualTo(GeneratedDataMetadata.ExtlangSubtagCount));
            Assert.That(
                GeneratedIanaRegistry.Rows.Count(row => row.RecordType == LanguageSubtagType.Script),
                Is.EqualTo(GeneratedDataMetadata.ScriptSubtagCount));
            Assert.That(
                GeneratedIanaRegistry.Rows.Count(row => row.RecordType == LanguageSubtagType.Region),
                Is.EqualTo(GeneratedDataMetadata.RegionSubtagCount));
            Assert.That(
                GeneratedIanaRegistry.Rows.Count(row => row.RecordType == LanguageSubtagType.Variant),
                Is.EqualTo(GeneratedDataMetadata.VariantSubtagCount));
            Assert.That(
                GeneratedIanaRegistry.Rows.Count(
                    row => row.RecordType != LanguageSubtagType.Grandfathered &&
                        row.RecordType != LanguageSubtagType.Redundant),
                Is.EqualTo(GeneratedDataMetadata.RegisteredSubtagCount));
            Assert.That(
                GeneratedIanaRegistry.Rows.Count(row => row.RecordType == LanguageSubtagType.Grandfathered),
                Is.EqualTo(GeneratedDataMetadata.GrandfatheredTagCount));
            Assert.That(
                GeneratedIanaRegistry.Rows.Count(row => row.RecordType == LanguageSubtagType.Redundant),
                Is.EqualTo(GeneratedDataMetadata.RedundantTagCount));
        });
    }

    [Test]
    public void CodeIndexSeedsAreOrdinallySortedAndReferenceMatchingEntities()
    {
        string? previous = null;
        foreach (CodeIndexSeedRow seed in GeneratedLanguageEntries.CodeIndexSeeds)
        {
            Assert.That(seed.EntityIndex, Is.InRange(0, GeneratedLanguageEntries.Rows.Length - 1));
            LanguageRow entity = GeneratedLanguageEntries.Rows[seed.EntityIndex];
            string? expected = null;
            Iso639Set expectedSet = Iso639Set.None;
            switch (seed.Form)
            {
                case LanguageCodeForm.Alpha2:
                    expected = entity.Alpha2Code;
                    expectedSet = Iso639Set.Set1;
                    break;
                case LanguageCodeForm.Set2Terminology:
                    expected = entity.Set2TerminologyCode;
                    expectedSet = Iso639Set.Set2;
                    break;
                case LanguageCodeForm.Set2Bibliographic:
                    expected = entity.Set2BibliographicCode;
                    expectedSet = Iso639Set.Set2;
                    break;
                case LanguageCodeForm.Set3:
                    expected = entity.Set3Code;
                    expectedSet = Iso639Set.Set3;
                    break;
                case LanguageCodeForm.Set5:
                    expected = entity.Set5Code;
                    expectedSet = Iso639Set.Set5;
                    break;
            }

            Assert.That(seed.Value, Is.EqualTo(expected));
            Assert.That(seed.Set, Is.EqualTo(expectedSet));
            if (previous != null)
            {
                Assert.That(string.CompareOrdinal(previous, seed.Value), Is.LessThanOrEqualTo(0));
            }

            previous = seed.Value;
        }
    }

    [Test]
    public void NameAndIanaSeedsAreOrdinallySortedAndReferenceMatchingRows()
    {
        Assert.That(
            GeneratedLanguageNames.NameIndexSeeds.Length,
            Is.EqualTo(GeneratedLanguageNames.Rows.Length));
        string? previousName = null;
        foreach (NameIndexSeedRow seed in GeneratedLanguageNames.NameIndexSeeds)
        {
            Assert.That(seed.NameIndex, Is.InRange(0, GeneratedLanguageNames.Rows.Length - 1));
            Assert.That(seed.Value, Is.EqualTo(GeneratedLanguageNames.Rows[seed.NameIndex].Text));
            if (previousName != null)
            {
                Assert.That(string.CompareOrdinal(previousName, seed.Value), Is.LessThanOrEqualTo(0));
            }

            previousName = seed.Value;
        }

        Assert.That(GeneratedIanaRegistry.IndexSeeds.Length, Is.EqualTo(GeneratedIanaRegistry.Rows.Length));
        string? previousSubtag = null;
        foreach (IanaIndexSeedRow seed in GeneratedIanaRegistry.IndexSeeds)
        {
            Assert.That(seed.RecordIndex, Is.InRange(0, GeneratedIanaRegistry.Rows.Length - 1));
            IanaSubtagRow record = GeneratedIanaRegistry.Rows[seed.RecordIndex];
            Assert.That(seed.Value, Is.EqualTo(record.Value));
            Assert.That(seed.RecordType, Is.EqualTo(record.RecordType));
            if (previousSubtag != null)
            {
                Assert.That(string.CompareOrdinal(previousSubtag, seed.Value), Is.LessThanOrEqualTo(0));
            }

            previousSubtag = seed.Value;
        }
    }

    [Test]
    public void GeneratedRowsPreserveRepresentativePinnedData()
    {
        LanguageRow? english = Array.Find(
            GeneratedLanguageEntries.Rows,
            row => row.Set3Code == "eng");
        Assert.That(english, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(english?.Alpha2Code, Is.EqualTo("en"));
            Assert.That(english?.Set2TerminologyCode, Is.EqualTo("eng"));
            Assert.That(english?.Set2BibliographicCode, Is.EqualTo("eng"));
        });

        NameRow? ianaEnglishName = Array.Find(
            GeneratedLanguageNames.Rows,
            row => row.EntityKey == "set3:eng" && row.Text == "English" &&
                row.Kind == LanguageNameKind.IanaDescription);
        Assert.That(ianaEnglishName?.Source, Is.EqualTo(ReconciledSource.Iana));

        AliasRow? frenchBibliographic = Array.Find(
            GeneratedAliases.Rows,
            row => row.Value == "fre" && row.Kind == LanguageAliasKind.Set2Bibliographic);
        Assert.That(frenchBibliographic?.PreferredValue, Is.EqualTo("fra"));
        Assert.That(frenchBibliographic?.Source, Is.EqualTo(ReconciledSource.Iso639Set2));

        RetirementRow? westernFrisian = Array.Find(
            GeneratedRetirements.Rows,
            row => row.RetiredCode == "fri");
        Assert.That(westernFrisian?.ReplacementCodes, Is.EqualTo(new[] { "fry" }));

        RelationshipRow? arabicMember = Array.Find(
            GeneratedRelationships.Rows,
            row => row.ParentCode == "ara" && row.ChildCode == "aao");
        Assert.That(arabicMember?.Kind, Is.EqualTo(LanguageRelationshipKind.MacrolanguageMember));

        IanaSubtagRow? resian = Array.Find(
            GeneratedIanaRegistry.Rows,
            row => row.RecordType == LanguageSubtagType.Variant && row.Value == "1994");
        Assert.That(resian?.Prefixes, Has.Length.EqualTo(5));

        IanaSubtagRow? nicot = Array.Find(
            GeneratedIanaRegistry.Rows,
            row => row.RecordType == LanguageSubtagType.Variant && row.Value == "1606nict");
        Assert.That(nicot?.Comments[0], Does.Contain("francoyse\", 1606, but also"));

        IanaSubtagRow? oxfordEnglish = Array.Find(
            GeneratedIanaRegistry.Rows,
            row => row.RecordType == LanguageSubtagType.Grandfathered && row.Value == "en-GB-oed");
        Assert.That(oxfordEnglish?.PreferredValue, Is.EqualTo("en-GB-oxendict"));
    }
}
