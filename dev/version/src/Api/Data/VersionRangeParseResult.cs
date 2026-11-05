namespace version;

/// <summary>
/// Represents the outcome of a version range parse operation.
/// </summary>
/// <remarks>
/// Check the success state before requesting the produced value. Failure results retain diagnostics that explain why processing stopped.
/// </remarks>
public class VersionRangeParseResult
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
    /// Stores the <c>syntax</c> value.
    /// </summary>
    public RangeSyntax syntax;
    /// <summary>
    /// Stores the <c>range</c> value.
    /// </summary>
    public VersionRange? range;
    /// <summary>
    /// Stores the <c>diagnostics</c> value.
    /// </summary>
    public VersionDiagnostic[] diagnostics;

    /// <summary>
    /// Initializes a new Version Range Parse Result instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="successValue">Whether to enable success value.</param>
    /// <param name="sourceTextValue">The source text value to process.</param>
    /// <param name="syntaxValue">The syntax value used by the operation.</param>
    /// <param name="rangeValue">The range value used by the operation.</param>
    /// <param name="diagnosticsValue">The diagnostics value to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public VersionRangeParseResult(
        bool successValue,
        string sourceTextValue,
        RangeSyntax syntaxValue,
        VersionRange? rangeValue,
        VersionDiagnostic[] diagnosticsValue)
    {
        System.ArgumentNullException.ThrowIfNull(sourceTextValue);
        System.ArgumentNullException.ThrowIfNull(diagnosticsValue);

        if (!successValue && rangeValue != null)
        {
            throw new System.ArgumentException("A failed range parse result cannot contain a range.");
        }

        success = successValue;
        sourceText = sourceTextValue;
        syntax = syntaxValue;
        range = rangeValue;
        diagnostics = CopyDiagnostics(diagnosticsValue);
    }

    /// <summary>
    /// Creates a success.
    /// </summary>
    /// <param name="sourceText">The source text to process.</param>
    /// <param name="syntax">The syntax used by the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    internal static VersionRangeParseResult CreateSuccess(string sourceText, RangeSyntax syntax)
    {
        return new VersionRangeParseResult(true, sourceText, syntax, null, new VersionDiagnostic[0]);
    }

    /// <summary>
    /// Creates a success.
    /// </summary>
    /// <param name="sourceText">The source text to process.</param>
    /// <param name="syntax">The syntax used by the operation.</param>
    /// <param name="parsedRange">The parsed range used by the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal static VersionRangeParseResult CreateSuccess(string sourceText, RangeSyntax syntax, VersionRange parsedRange)
    {
        System.ArgumentNullException.ThrowIfNull(parsedRange);

        return new VersionRangeParseResult(true, sourceText, syntax, parsedRange, new VersionDiagnostic[0]);
    }

    /// <summary>
    /// Creates a failure.
    /// </summary>
    /// <param name="sourceText">The source text to process.</param>
    /// <param name="syntax">The syntax used by the operation.</param>
    /// <param name="diagnostic">The diagnostic used by the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal static VersionRangeParseResult CreateFailure(string sourceText, RangeSyntax syntax, VersionDiagnostic diagnostic)
    {
        System.ArgumentNullException.ThrowIfNull(diagnostic);

        VersionDiagnostic[] diagnostics = new VersionDiagnostic[1];
        diagnostics[0] = diagnostic;
        return new VersionRangeParseResult(false, sourceText, syntax, null, diagnostics);
    }

    /// <summary>
    /// Creates a failure.
    /// </summary>
    /// <param name="sourceText">The source text to process.</param>
    /// <param name="syntax">The syntax used by the operation.</param>
    /// <param name="diagnostics">The diagnostics to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    internal static VersionRangeParseResult CreateFailure(string sourceText, RangeSyntax syntax, VersionDiagnostic[] diagnostics)
    {
        return new VersionRangeParseResult(false, sourceText, syntax, null, diagnostics);
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
    internal static VersionRangeParseResult TryParse(string text, VersionRangeParseOptions options)
    {
        return VersionOptionDispatcher.ParseRange(text, options);
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
    /// Gets a range syntax.
    /// </summary>
    /// <returns>The resulting range syntax.</returns>
    internal RangeSyntax GetRangeSyntax()
    {
        return syntax;
    }

    /// <summary>
    /// Reports whether this value has range.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasRange()
    {
        return range != null;
    }

    /// <summary>
    /// Gets a range.
    /// </summary>
    /// <returns>The resulting version range.</returns>
    /// <exception cref="System.InvalidOperationException">The requested value is unavailable because the operation did not complete successfully.</exception>
    internal VersionRange GetRange()
    {
        if (range == null)
        {
            throw new System.InvalidOperationException("Parse result does not contain a range.");
        }

        return range;
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
