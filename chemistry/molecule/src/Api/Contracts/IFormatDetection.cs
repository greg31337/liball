namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="FormatDetection"/>.
/// </summary>
public interface IFormatDetection
{
    /// <summary>
    /// Detects a supported molecule format from a caller hint, file name, or characteristic content.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="fileName">The file name.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    FormatDetectionResult Detect(string? text, string? fileName);

    /// <summary>
    /// Detects a supported molecule format using the supplied hint.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="hint">The hint.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    FormatDetectionResult DetectWithHint(string? text, string? fileName, FormatKind hint);
}

