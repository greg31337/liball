using languagecodes;

namespace languagecodes.Tests;

public class LanguageDataTablesTests
{
    [Test]
    public void MaterializedTablesContainEveryGeneratedRowAndSeed()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LanguageDataTables.LanguageRows.Length, Is.EqualTo(GeneratedLanguageEntries.Rows.Length));
            Assert.That(LanguageDataTables.NameRows.Length, Is.EqualTo(GeneratedLanguageNames.Rows.Length));
            Assert.That(LanguageDataTables.AliasRows.Length, Is.EqualTo(GeneratedAliases.Rows.Length));
            Assert.That(LanguageDataTables.RetirementRows.Length, Is.EqualTo(GeneratedRetirements.Rows.Length));
            Assert.That(
                LanguageDataTables.RelationshipRows.Length,
                Is.EqualTo(GeneratedRelationships.Rows.Length));
            Assert.That(LanguageDataTables.IanaRows.Length, Is.EqualTo(GeneratedIanaRegistry.Rows.Length));
            Assert.That(
                LanguageDataTables.CodeIndexSeeds.Length,
                Is.EqualTo(GeneratedLanguageEntries.CodeIndexSeeds.Length));
            Assert.That(
                LanguageDataTables.NameIndexSeeds.Length,
                Is.EqualTo(GeneratedLanguageNames.NameIndexSeeds.Length));
            Assert.That(
                LanguageDataTables.IanaIndexSeeds.Length,
                Is.EqualTo(GeneratedIanaRegistry.IndexSeeds.Length));
        });
    }

    [Test]
    public void MaterializedTablesDoNotRetainGeneratedArraysOrRows()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LanguageDataTables.LanguageRows, Is.Not.SameAs(GeneratedLanguageEntries.Rows));
            Assert.That(LanguageDataTables.LanguageRows[0], Is.Not.SameAs(GeneratedLanguageEntries.Rows[0]));
            Assert.That(LanguageDataTables.NameRows, Is.Not.SameAs(GeneratedLanguageNames.Rows));
            Assert.That(LanguageDataTables.NameRows[0], Is.Not.SameAs(GeneratedLanguageNames.Rows[0]));
            Assert.That(LanguageDataTables.AliasRows, Is.Not.SameAs(GeneratedAliases.Rows));
            Assert.That(LanguageDataTables.AliasRows[0], Is.Not.SameAs(GeneratedAliases.Rows[0]));
            Assert.That(LanguageDataTables.RetirementRows, Is.Not.SameAs(GeneratedRetirements.Rows));
            Assert.That(LanguageDataTables.RetirementRows[0], Is.Not.SameAs(GeneratedRetirements.Rows[0]));
            Assert.That(LanguageDataTables.RelationshipRows, Is.Not.SameAs(GeneratedRelationships.Rows));
            Assert.That(
                LanguageDataTables.RelationshipRows[0],
                Is.Not.SameAs(GeneratedRelationships.Rows[0]));
            Assert.That(LanguageDataTables.IanaRows, Is.Not.SameAs(GeneratedIanaRegistry.Rows));
            Assert.That(LanguageDataTables.IanaRows[0], Is.Not.SameAs(GeneratedIanaRegistry.Rows[0]));
        });
    }

    [Test]
    public void MaterializationDeepCopiesNestedMutableArrays()
    {
        RetirementRow retirementSource = new RetirementRow(
            "old",
            "Former name",
            LanguageRetirementReason.Split,
            new[] { "aaa", "bbb" },
            "Split",
            "2026-01-01");
        IanaSubtagRow ianaSource = new IanaSubtagRow(
            LanguageSubtagType.Variant,
            "test",
            new[] { "Description" },
            "2026-01-01",
            null,
            null,
            new[] { "en" },
            null,
            null,
            null,
            new[] { "Comment" });

        RetirementRow[] retirements = LanguageTableMaterializer.CopyRetirementRows(new[] { retirementSource });
        IanaSubtagRow[] ianaRows = LanguageTableMaterializer.CopyIanaRows(new[] { ianaSource });

        retirementSource.ReplacementCodes[0] = "changed";
        ianaSource.Descriptions[0] = "Changed description";
        ianaSource.Prefixes[0] = "fr";
        ianaSource.Comments[0] = "Changed comment";

        Assert.Multiple(() =>
        {
            Assert.That(retirements[0], Is.Not.SameAs(retirementSource));
            Assert.That(retirements[0].ReplacementCodes, Is.EqualTo(new[] { "aaa", "bbb" }));
            Assert.That(retirements[0].ReplacementCodes, Is.Not.SameAs(retirementSource.ReplacementCodes));
            Assert.That(ianaRows[0], Is.Not.SameAs(ianaSource));
            Assert.That(ianaRows[0].Descriptions, Is.EqualTo(new[] { "Description" }));
            Assert.That(ianaRows[0].Prefixes, Is.EqualTo(new[] { "en" }));
            Assert.That(ianaRows[0].Comments, Is.EqualTo(new[] { "Comment" }));
            Assert.That(ianaRows[0].Descriptions, Is.Not.SameAs(ianaSource.Descriptions));
            Assert.That(ianaRows[0].Prefixes, Is.Not.SameAs(ianaSource.Prefixes));
            Assert.That(ianaRows[0].Comments, Is.Not.SameAs(ianaSource.Comments));
        });
    }

    [Test]
    public void MaterializedRowsPreserveRepresentativeValues()
    {
        LanguageRow? english = Array.Find(LanguageDataTables.LanguageRows, row => row.Set3Code == "eng");
        RetirementRow? westernFrisian = Array.Find(
            LanguageDataTables.RetirementRows,
            row => row.RetiredCode == "fri");
        IanaSubtagRow? oxfordEnglish = Array.Find(
            LanguageDataTables.IanaRows,
            row => row.Value == "en-GB-oed");

        Assert.Multiple(() =>
        {
            Assert.That(english?.Alpha2Code, Is.EqualTo("en"));
            Assert.That(english?.Status, Is.EqualTo(LanguageCodeStatus.Active));
            Assert.That(westernFrisian?.ReplacementCodes, Is.EqualTo(new[] { "fry" }));
            Assert.That(oxfordEnglish?.RecordType, Is.EqualTo(LanguageSubtagType.Grandfathered));
            Assert.That(oxfordEnglish?.PreferredValue, Is.EqualTo("en-GB-oxendict"));
        });
    }

    [Test]
    public void SeedMaterializationCopiesContainersAndRows()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                LanguageDataTables.CodeIndexSeeds,
                Is.Not.SameAs(GeneratedLanguageEntries.CodeIndexSeeds));
            Assert.That(
                LanguageDataTables.CodeIndexSeeds[0],
                Is.Not.SameAs(GeneratedLanguageEntries.CodeIndexSeeds[0]));
            Assert.That(
                LanguageDataTables.NameIndexSeeds,
                Is.Not.SameAs(GeneratedLanguageNames.NameIndexSeeds));
            Assert.That(
                LanguageDataTables.NameIndexSeeds[0],
                Is.Not.SameAs(GeneratedLanguageNames.NameIndexSeeds[0]));
            Assert.That(
                LanguageDataTables.IanaIndexSeeds,
                Is.Not.SameAs(GeneratedIanaRegistry.IndexSeeds));
            Assert.That(
                LanguageDataTables.IanaIndexSeeds[0],
                Is.Not.SameAs(GeneratedIanaRegistry.IndexSeeds[0]));
        });
    }
}
