namespace languagecodes;

/// <summary>Publishes scalar version, provenance, and record-count metadata for the bundled data snapshot.</summary>
public static class LanguageDataInfo
{
    /// <summary>The ISO edition represented by the bundled data.</summary>
    public const string IsoEdition = GeneratedDataMetadata.IsoEdition;

    /// <summary>The composite version of the bundled ISO and IANA snapshots.</summary>
    public const string DataVersion = GeneratedDataMetadata.DataVersion;

    /// <summary>The version of the pinned source manifest format.</summary>
    public const int ManifestVersion = GeneratedDataMetadata.ManifestVersion;

    /// <summary>The runtime-table generator schema version.</summary>
    public const int GeneratorSchemaVersion = GeneratedDataMetadata.GeneratorSchemaVersion;

    /// <summary>The date on which the pinned sources were retrieved, in ISO <c>yyyy-MM-dd</c> form.</summary>
    public const string RetrievalDate = GeneratedDataMetadata.RetrievalDate;

    /// <summary>The upstream date for the ISO 639 Set 1 and Set 2 table.</summary>
    public const string Iso639Set2UpstreamDate = GeneratedDataMetadata.Iso639Set2UpstreamDate;

    /// <summary>The upstream date for the ISO 639 Set 3 core table.</summary>
    public const string Iso639Set3CoreUpstreamDate = GeneratedDataMetadata.Iso639Set3CoreUpstreamDate;

    /// <summary>The upstream date for the ISO 639 Set 3 name index.</summary>
    public const string Iso639Set3NamesUpstreamDate = GeneratedDataMetadata.Iso639Set3NamesUpstreamDate;

    /// <summary>The upstream date for the ISO 639 Set 3 macrolanguage table.</summary>
    public const string Iso639Set3MacrolanguagesUpstreamDate = GeneratedDataMetadata.Iso639Set3MacrolanguagesUpstreamDate;

    /// <summary>The upstream date for the ISO 639 Set 3 retirements table.</summary>
    public const string Iso639Set3RetirementsUpstreamDate = GeneratedDataMetadata.Iso639Set3RetirementsUpstreamDate;

    /// <summary>The upstream date for the ISO 639 Set 5 code table.</summary>
    public const string Iso639Set5UpstreamDate = GeneratedDataMetadata.Iso639Set5UpstreamDate;

    /// <summary>The upstream date for the ISO 639 Set 5 hierarchy.</summary>
    public const string Iso639Set5HierarchyUpstreamDate = GeneratedDataMetadata.Iso639Set5HierarchyUpstreamDate;

    /// <summary>The file date of the bundled IANA Language Subtag Registry.</summary>
    public const string IanaRegistryUpstreamDate = GeneratedDataMetadata.IanaRegistryUpstreamDate;

    /// <summary>The published version date of RFC 4647.</summary>
    public const string Rfc4647Version = GeneratedDataMetadata.Rfc4647Version;

    /// <summary>The published version date of RFC 5646.</summary>
    public const string Rfc5646Version = GeneratedDataMetadata.Rfc5646Version;

    /// <summary>The number of active reconciled ISO entities.</summary>
    public const int ActiveEntityCount = GeneratedDataMetadata.ActiveEntityCount;

    /// <summary>The number of ISO 639 Set 1 records.</summary>
    public const int Set1RecordCount = GeneratedDataMetadata.Set1RecordCount;

    /// <summary>The number of ISO 639 Set 2 records.</summary>
    public const int Set2RecordCount = GeneratedDataMetadata.Set2RecordCount;

    /// <summary>The number of ISO 639 Set 3 records.</summary>
    public const int Set3RecordCount = GeneratedDataMetadata.Set3RecordCount;

    /// <summary>The number of ISO 639 Set 5 records.</summary>
    public const int Set5RecordCount = GeneratedDataMetadata.Set5RecordCount;

    /// <summary>The number of retained ISO 639 Set 3 retirement records.</summary>
    public const int RetiredCodeCount = GeneratedDataMetadata.RetiredCodeCount;

    /// <summary>The number of generated language-name rows.</summary>
    public const int NameCount = GeneratedDataMetadata.NameCount;

    /// <summary>The number of generated language-alias rows.</summary>
    public const int AliasCount = GeneratedDataMetadata.AliasCount;

    /// <summary>The number of generated relationship rows.</summary>
    public const int RelationshipCount = GeneratedDataMetadata.RelationshipCount;

    /// <summary>The total number of generated IANA registry records.</summary>
    public const int IanaRecordCount = GeneratedDataMetadata.IanaRecordCount;

    /// <summary>The number of IANA language-subtag records.</summary>
    public const int LanguageSubtagCount = GeneratedDataMetadata.LanguageSubtagCount;

    /// <summary>The number of IANA extended-language-subtag records.</summary>
    public const int ExtlangSubtagCount = GeneratedDataMetadata.ExtlangSubtagCount;

    /// <summary>The number of IANA script-subtag records.</summary>
    public const int ScriptSubtagCount = GeneratedDataMetadata.ScriptSubtagCount;

    /// <summary>The number of IANA region-subtag records.</summary>
    public const int RegionSubtagCount = GeneratedDataMetadata.RegionSubtagCount;

    /// <summary>The number of IANA variant-subtag records.</summary>
    public const int VariantSubtagCount = GeneratedDataMetadata.VariantSubtagCount;

    /// <summary>The number of IANA grandfathered-tag records.</summary>
    public const int GrandfatheredTagCount = GeneratedDataMetadata.GrandfatheredTagCount;

    /// <summary>The number of IANA redundant-tag records.</summary>
    public const int RedundantTagCount = GeneratedDataMetadata.RedundantTagCount;

    /// <summary>The number of IANA registered subtag records excluding complete tags.</summary>
    public const int RegisteredSubtagCount = GeneratedDataMetadata.RegisteredSubtagCount;
}
