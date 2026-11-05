namespace languagecodes;

internal static class LanguageDataTables
{
    internal static readonly LanguageRow[] LanguageRows =
        LanguageTableMaterializer.CopyLanguageRows(GeneratedLanguageEntries.Rows);
    internal static readonly NameRow[] NameRows =
        LanguageTableMaterializer.CopyNameRows(GeneratedLanguageNames.Rows);
    internal static readonly AliasRow[] AliasRows =
        LanguageTableMaterializer.CopyAliasRows(GeneratedAliases.Rows);
    internal static readonly RetirementRow[] RetirementRows =
        LanguageTableMaterializer.CopyRetirementRows(GeneratedRetirements.Rows);
    internal static readonly RelationshipRow[] RelationshipRows =
        LanguageTableMaterializer.CopyRelationshipRows(GeneratedRelationships.Rows);
    internal static readonly IanaSubtagRow[] IanaRows =
        LanguageTableMaterializer.CopyIanaRows(GeneratedIanaRegistry.Rows);
    internal static readonly CodeIndexSeedRow[] CodeIndexSeeds =
        LanguageTableMaterializer.CopyCodeIndexSeeds(GeneratedLanguageEntries.CodeIndexSeeds);
    internal static readonly NameIndexSeedRow[] NameIndexSeeds =
        LanguageTableMaterializer.CopyNameIndexSeeds(GeneratedLanguageNames.NameIndexSeeds);
    internal static readonly IanaIndexSeedRow[] IanaIndexSeeds =
        LanguageTableMaterializer.CopyIanaIndexSeeds(GeneratedIanaRegistry.IndexSeeds);
}

internal static class LanguageTableMaterializer
{
    internal static LanguageRow[] CopyLanguageRows(LanguageRow[] source)
    {
        LanguageRow[] destination = new LanguageRow[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            LanguageRow row = source[index];
            destination[index] = new LanguageRow(
                row.Key,
                row.Alpha2Code,
                row.Set2TerminologyCode,
                row.Set2BibliographicCode,
                row.Set3Code,
                row.Set5Code,
                row.Scope,
                row.LanguageType,
                row.Status,
                row.GroupKind,
                row.Comment);
        }

        return destination;
    }

    internal static NameRow[] CopyNameRows(NameRow[] source)
    {
        NameRow[] destination = new NameRow[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            NameRow row = source[index];
            destination[index] = new NameRow(
                row.EntityKey,
                row.Text,
                row.NameLanguage,
                row.Kind,
                row.Source,
                row.IsReferenceName);
        }

        return destination;
    }

    internal static AliasRow[] CopyAliasRows(AliasRow[] source)
    {
        AliasRow[] destination = new AliasRow[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            AliasRow row = source[index];
            destination[index] = new AliasRow(
                row.EntityKey,
                row.Value,
                row.Kind,
                row.PreferredValue,
                row.Source,
                row.SourceRecordIdentifier,
                row.IsDeprecated);
        }

        return destination;
    }

    internal static RetirementRow[] CopyRetirementRows(RetirementRow[] source)
    {
        RetirementRow[] destination = new RetirementRow[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            RetirementRow row = source[index];
            destination[index] = new RetirementRow(
                row.RetiredCode,
                row.FormerReferenceName,
                row.Reason,
                CopyStrings(row.ReplacementCodes),
                row.Remedy,
                row.EffectiveDate);
        }

        return destination;
    }

    internal static RelationshipRow[] CopyRelationshipRows(RelationshipRow[] source)
    {
        RelationshipRow[] destination = new RelationshipRow[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            RelationshipRow row = source[index];
            destination[index] = new RelationshipRow(
                row.ParentCode,
                row.ChildCode,
                row.Kind,
                row.Status,
                row.Source);
        }

        return destination;
    }

    internal static IanaSubtagRow[] CopyIanaRows(IanaSubtagRow[] source)
    {
        IanaSubtagRow[] destination = new IanaSubtagRow[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            IanaSubtagRow row = source[index];
            destination[index] = new IanaSubtagRow(
                row.RecordType,
                row.Value,
                CopyStrings(row.Descriptions),
                row.AddedDate,
                row.DeprecatedDate,
                row.PreferredValue,
                CopyStrings(row.Prefixes),
                row.SuppressScript,
                row.Macrolanguage,
                row.Scope,
                CopyStrings(row.Comments));
        }

        return destination;
    }

    internal static CodeIndexSeedRow[] CopyCodeIndexSeeds(CodeIndexSeedRow[] source)
    {
        CodeIndexSeedRow[] destination = new CodeIndexSeedRow[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            CodeIndexSeedRow row = source[index];
            destination[index] = new CodeIndexSeedRow(row.Value, row.EntityIndex, row.Form, row.Set);
        }

        return destination;
    }

    internal static NameIndexSeedRow[] CopyNameIndexSeeds(NameIndexSeedRow[] source)
    {
        NameIndexSeedRow[] destination = new NameIndexSeedRow[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            NameIndexSeedRow row = source[index];
            destination[index] = new NameIndexSeedRow(row.Value, row.NameIndex);
        }

        return destination;
    }

    internal static IanaIndexSeedRow[] CopyIanaIndexSeeds(IanaIndexSeedRow[] source)
    {
        IanaIndexSeedRow[] destination = new IanaIndexSeedRow[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            IanaIndexSeedRow row = source[index];
            destination[index] = new IanaIndexSeedRow(row.RecordType, row.Value, row.RecordIndex);
        }

        return destination;
    }

    internal static string[] CopyStrings(string[] source)
    {
        string[] destination = new string[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            destination[index] = source[index];
        }

        return destination;
    }
}
