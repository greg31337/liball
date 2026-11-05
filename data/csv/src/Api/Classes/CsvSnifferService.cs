namespace csv;

/// <summary>
/// Provides instance-based CSV syntax and schema detection.
/// </summary>
public class CsvSnifferService : ICsvSniffer
{
    /// <summary>Initializes a CSV detection service.</summary>
    public CsvSnifferService()
    {
    }

    /// <summary>Detects a dialect from sampled text.</summary>
    /// <param name="sample">The sampled CSV text.</param>
    /// <param name="options">Detection options.</param>
    /// <returns>The detected dialect.</returns>
    public CsvDialect Sniff(string? sample, CsvSnifferOptions? options)
    {
        return CsvSniffer.Sniff(sample, options);
    }

    /// <summary>Determines whether sampled text appears to contain a header.</summary>
    /// <param name="sample">The sampled CSV text.</param>
    /// <param name="dialect">The parsing dialect.</param>
    /// <param name="options">Detection options.</param>
    /// <returns><c>true</c> when a header is detected; otherwise <c>false</c>.</returns>
    public bool DetectHeader(string? sample, CsvDialect dialect, CsvSnifferOptions? options)
    {
        return CsvSniffer.DetectHeader(sample, dialect, options);
    }

    /// <summary>Infers column types from a sampled table.</summary>
    /// <param name="sample">The sampled table.</param>
    /// <param name="options">Inference options.</param>
    /// <returns>The inferred schema.</returns>
    public CsvSchema DetectTypes(CsvTable sample, CsvInferenceOptions? options)
    {
        return CsvSniffer.DetectTypes(sample, options);
    }
}
