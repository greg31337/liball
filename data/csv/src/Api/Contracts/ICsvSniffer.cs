namespace csv;

/// <summary>
/// Detects likely CSV syntax, headers, and column types.
/// </summary>
public interface ICsvSniffer
{
    /// <summary>Detects a dialect from sampled text.</summary>
    /// <param name="sample">The sampled CSV text.</param>
    /// <param name="options">Detection options.</param>
    /// <returns>The detected dialect.</returns>
    CsvDialect Sniff(string? sample, CsvSnifferOptions? options);

    /// <summary>Determines whether sampled text appears to contain a header.</summary>
    /// <param name="sample">The sampled CSV text.</param>
    /// <param name="dialect">The parsing dialect.</param>
    /// <param name="options">Detection options.</param>
    /// <returns><c>true</c> when a header is detected; otherwise <c>false</c>.</returns>
    bool DetectHeader(string? sample, CsvDialect dialect, CsvSnifferOptions? options);

    /// <summary>Infers column types from a sampled table.</summary>
    /// <param name="sample">The sampled table.</param>
    /// <param name="options">Inference options.</param>
    /// <returns>The inferred schema.</returns>
    CsvSchema DetectTypes(CsvTable sample, CsvInferenceOptions? options);
}
