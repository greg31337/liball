namespace countrycodes;

/// <summary>
/// Exposes immutable scalar metadata for the compiled country-code dataset.
/// </summary>
public static class CountryDataMetadata
{
    /// <summary>
    /// The semantic version of the compiled dataset.
    /// </summary>
    public const string DatasetVersion = GeneratedDataMetadata.DataVersion;

    /// <summary>
    /// The canonical effective date of the compiled dataset.
    /// </summary>
    public const string EffectiveDate = GeneratedDataMetadata.EffectiveDate;

    /// <summary>
    /// The version of the generator that emitted the compiled data.
    /// </summary>
    public const string GeneratorVersion = GeneratedDataMetadata.GeneratorVersion;

    /// <summary>
    /// The algorithm used by the aggregate content checksum.
    /// </summary>
    public const string ContentChecksumAlgorithm = GeneratedDataMetadata.ContentChecksumAlgorithm;

    /// <summary>
    /// The aggregate checksum of the generated dataset files.
    /// </summary>
    public const string ContentChecksum = GeneratedDataMetadata.AggregateContentChecksum;

    /// <summary>
    /// The pinned CLDR source revision.
    /// </summary>
    public static readonly string CldrSourceRevision =
        GeneratedDataMetadata.SourceRevisions[0].Revision;

    /// <summary>
    /// The pinned Debian ISO Codes source revision.
    /// </summary>
    public static readonly string DebianIsoCodesSourceRevision =
        GeneratedDataMetadata.SourceRevisions[1].Revision;

    /// <summary>
    /// The pinned IANA language-subtag registry revision.
    /// </summary>
    public static readonly string IanaLanguageSubtagRegistrySourceRevision =
        GeneratedDataMetadata.SourceRevisions[2].Revision;

    /// <summary>
    /// The pinned IETF BCP 47 reference revision.
    /// </summary>
    public static readonly string IetfBcp47SourceRevision =
        GeneratedDataMetadata.SourceRevisions[3].Revision;

    /// <summary>
    /// The pinned UN M49 source revision.
    /// </summary>
    public static readonly string UnM49SourceRevision =
        GeneratedDataMetadata.SourceRevisions[4].Revision;

    /// <summary>
    /// The pinned Unicode specification revision.
    /// </summary>
    public static readonly string UnicodeSpecificationsSourceRevision =
        GeneratedDataMetadata.SourceRevisions[5].Revision;
}
