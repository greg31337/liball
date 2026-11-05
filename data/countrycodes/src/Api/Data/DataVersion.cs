namespace countrycodes;

/// <summary>
/// Holds the immutable-by-detachment version metadata for the compiled dataset.
/// </summary>
public class DataVersion
{
    /// <summary>
    /// The public dataset schema version.
    /// </summary>
    public string SchemaVersion;

    /// <summary>
    /// The semantic version of the compiled dataset.
    /// </summary>
    public string DatasetVersion;

    /// <summary>
    /// The canonical effective date of the compiled data.
    /// </summary>
    public string EffectiveDate;

    /// <summary>
    /// The canonical generation date of the compiled data.
    /// </summary>
    public string GeneratedDate;

    /// <summary>
    /// The data generator version.
    /// </summary>
    public string GeneratorVersion;

    /// <summary>
    /// The pinned source identifiers and revisions.
    /// </summary>
    public string[] SourceRevisions;

    /// <summary>
    /// The aggregate checksum of the generated data files.
    /// </summary>
    public string ContentChecksum;

    /// <summary>
    /// Creates a detached data-version snapshot.
    /// </summary>
    /// <param name="schemaVersion">The dataset schema version.</param>
    /// <param name="datasetVersion">The semantic dataset version.</param>
    /// <param name="effectiveDate">The canonical effective date.</param>
    /// <param name="generatedDate">The canonical generation date.</param>
    /// <param name="generatorVersion">The generator version.</param>
    /// <param name="sourceRevisions">The pinned source identifiers and revisions.</param>
    /// <param name="contentChecksum">The aggregate content checksum.</param>
    public DataVersion(
        string schemaVersion,
        string datasetVersion,
        string effectiveDate,
        string generatedDate,
        string generatorVersion,
        string[] sourceRevisions,
        string contentChecksum)
    {
        SchemaVersion = schemaVersion;
        DatasetVersion = datasetVersion;
        EffectiveDate = effectiveDate;
        GeneratedDate = generatedDate;
        GeneratorVersion = generatorVersion;
        SourceRevisions = (string[])sourceRevisions.Clone();
        ContentChecksum = contentChecksum;
    }
}
