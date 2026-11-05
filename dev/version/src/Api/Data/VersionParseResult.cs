namespace version;

/// <summary>
/// Represents the outcome of a version parse operation.
/// </summary>
/// <remarks>
/// Check the success state before requesting the produced value. Failure results retain diagnostics that explain why processing stopped.
/// </remarks>
public class VersionParseResult
{
    /// <summary>
    /// Stores the <c>success</c> value.
    /// </summary>
    public bool success;
    /// <summary>
    /// Stores the <c>sourceText</c> value.
    /// </summary>
    public string sourceText;
    /// <summary>
    /// Stores the <c>dialect</c> value.
    /// </summary>
    public VersionDialect dialect;
    /// <summary>
    /// Stores the <c>versionPresent</c> value.
    /// </summary>
    public bool versionPresent;
    /// <summary>
    /// Stores the <c>version</c> value.
    /// </summary>
    public UniversalVersion? version;
    /// <summary>
    /// Stores the <c>diagnostics</c> value.
    /// </summary>
    public VersionDiagnostic[] diagnostics;

    /// <summary>
    /// Initializes a new Version Parse Result instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="successValue">Whether to enable success value.</param>
    /// <param name="sourceTextValue">The source text value to process.</param>
    /// <param name="dialectValue">The dialect value used by the operation.</param>
    /// <param name="diagnosticsValue">The diagnostics value to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public VersionParseResult(bool successValue, string sourceTextValue, VersionDialect dialectValue, VersionDiagnostic[] diagnosticsValue)
    {
        System.ArgumentNullException.ThrowIfNull(sourceTextValue);
        System.ArgumentNullException.ThrowIfNull(diagnosticsValue);

        success = successValue;
        sourceText = sourceTextValue;
        dialect = dialectValue;
        versionPresent = false;
        version = null;
        diagnostics = CopyDiagnostics(diagnosticsValue);
    }

    /// <summary>
    /// Creates a success.
    /// </summary>
    /// <param name="sourceText">The source text to process.</param>
    /// <param name="dialect">The dialect used by the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    internal static VersionParseResult CreateSuccess(string sourceText, VersionDialect dialect)
    {
        return new VersionParseResult(true, sourceText, dialect, new VersionDiagnostic[0]);
    }

    /// <summary>
    /// Creates a success.
    /// </summary>
    /// <param name="sourceText">The source text to process.</param>
    /// <param name="dialect">The dialect used by the operation.</param>
    /// <param name="parsedVersion">The parsed version used by the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal static VersionParseResult CreateSuccess(string sourceText, VersionDialect dialect, UniversalVersion parsedVersion)
    {
        System.ArgumentNullException.ThrowIfNull(parsedVersion);

        VersionParseResult result = new VersionParseResult(true, sourceText, dialect, new VersionDiagnostic[0]);
        result.versionPresent = true;
        result.version = parsedVersion;
        return result;
    }

    /// <summary>
    /// Creates a failure.
    /// </summary>
    /// <param name="sourceText">The source text to process.</param>
    /// <param name="dialect">The dialect used by the operation.</param>
    /// <param name="diagnostic">The diagnostic used by the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal static VersionParseResult CreateFailure(string sourceText, VersionDialect dialect, VersionDiagnostic diagnostic)
    {
        System.ArgumentNullException.ThrowIfNull(diagnostic);

        VersionDiagnostic[] diagnostics = new VersionDiagnostic[1];
        diagnostics[0] = diagnostic;
        return new VersionParseResult(false, sourceText, dialect, diagnostics);
    }

    /// <summary>
    /// Creates a failure.
    /// </summary>
    /// <param name="sourceText">The source text to process.</param>
    /// <param name="dialect">The dialect used by the operation.</param>
    /// <param name="diagnostics">The diagnostics to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    internal static VersionParseResult CreateFailure(string sourceText, VersionDialect dialect, VersionDiagnostic[] diagnostics)
    {
        return new VersionParseResult(false, sourceText, dialect, diagnostics);
    }

    /// <summary>
    /// Attempts to parse version text using the supplied options.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    internal static VersionParseResult TryParse(string text, VersionParseOptions options)
    {
        return VersionOptionDispatcher.Parse(text, options);
    }

    /// <summary>
    /// Reports whether the operation completed successfully.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsSuccess()
    {
        return success;
    }

    /// <summary>
    /// Reports whether the operation failed and produced diagnostics.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsFailure()
    {
        return !success;
    }

    /// <summary>
    /// Gets a source text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetSourceText()
    {
        return sourceText;
    }

    /// <summary>
    /// Gets a dialect.
    /// </summary>
    /// <returns>The resulting version dialect.</returns>
    internal VersionDialect GetDialect()
    {
        return dialect;
    }

    /// <summary>
    /// Reports whether this value has version.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasVersion()
    {
        return versionPresent;
    }

    /// <summary>
    /// Gets a version.
    /// </summary>
    /// <returns>The resulting universal version.</returns>
    /// <exception cref="System.InvalidOperationException">The requested value is unavailable because the operation did not complete successfully.</exception>
    internal UniversalVersion GetVersion()
    {
        if (!versionPresent || version == null)
        {
            throw new System.InvalidOperationException("Parse result does not contain a version.");
        }

        return version;
    }

    /// <summary>
    /// Gets a diagnostic count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetDiagnosticCount()
    {
        return diagnostics.Length;
    }

    /// <summary>
    /// Gets a diagnostic.
    /// </summary>
    /// <param name="diagnosticIndex">The zero-based diagnostic index.</param>
    /// <returns>The resulting version diagnostic.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal VersionDiagnostic GetDiagnostic(int diagnosticIndex)
    {
        if (diagnosticIndex < 0 || diagnosticIndex >= diagnostics.Length)
        {
            throw new System.ArgumentOutOfRangeException("diagnosticIndex");
        }

        return diagnostics[diagnosticIndex];
    }

    /// <summary>
    /// Gets a diagnostics.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal VersionDiagnostic[] GetDiagnostics()
    {
        return CopyDiagnostics(diagnostics);
    }

    internal static VersionDiagnostic[] CopyDiagnostics(VersionDiagnostic[] diagnosticsValue)
    {
        VersionDiagnostic[] copy = new VersionDiagnostic[diagnosticsValue.Length];
        int index = 0;

        while (index < diagnosticsValue.Length)
        {
            if (diagnosticsValue[index] == null)
            {
                throw new System.ArgumentException("Diagnostics cannot contain null values.");
            }

            copy[index] = diagnosticsValue[index];
            index = index + 1;
        }

        return copy;
    }
}
