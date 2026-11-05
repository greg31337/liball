namespace version;

/// <summary>
/// Represents the outcome of a node semver valid operation.
/// </summary>
/// <remarks>
/// Check the success state before requesting the produced value. Failure results retain diagnostics that explain why processing stopped.
/// </remarks>
public class NodeSemVerValidResult
{
    /// <summary>
    /// Stores the <c>valid</c> value.
    /// </summary>
    public bool valid;
    /// <summary>
    /// Stores the <c>sourceText</c> value.
    /// </summary>
    public string sourceText;
    /// <summary>
    /// Stores the <c>versionText</c> value.
    /// </summary>
    public string versionText;
    /// <summary>
    /// Stores the <c>diagnostics</c> value.
    /// </summary>
    public VersionDiagnostic[] diagnostics;

    /// <summary>
    /// Initializes a new Node SemVer Valid Result instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="validValue">Whether to enable valid value.</param>
    /// <param name="sourceTextValue">The source text value to process.</param>
    /// <param name="versionTextValue">The version text value to process.</param>
    /// <param name="diagnosticValues">The diagnostic values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public NodeSemVerValidResult(bool validValue, string sourceTextValue, string versionTextValue, VersionDiagnostic[] diagnosticValues)
    {
        System.ArgumentNullException.ThrowIfNull(sourceTextValue);
        System.ArgumentNullException.ThrowIfNull(versionTextValue);
        System.ArgumentNullException.ThrowIfNull(diagnosticValues);

        valid = validValue;
        sourceText = sourceTextValue;
        versionText = versionTextValue;
        diagnostics = CopyDiagnostics(diagnosticValues);

        if (valid && versionText.Length == 0)
        {
            throw new System.ArgumentException("Valid node-semver results require version text.");
        }

        if (valid && diagnostics.Length > 0)
        {
            throw new System.ArgumentException("Valid node-semver results cannot contain diagnostics.");
        }
    }

    /// <summary>
    /// Creates a valid.
    /// </summary>
    /// <param name="sourceText">The source text to process.</param>
    /// <param name="versionText">The version text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    internal static NodeSemVerValidResult CreateValid(string sourceText, string versionText)
    {
        return new NodeSemVerValidResult(true, sourceText, versionText, new VersionDiagnostic[0]);
    }

    /// <summary>
    /// Creates an invalid.
    /// </summary>
    /// <param name="sourceText">The source text to process.</param>
    /// <param name="diagnostics">The diagnostics to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    internal static NodeSemVerValidResult CreateInvalid(string sourceText, VersionDiagnostic[] diagnostics)
    {
        return new NodeSemVerValidResult(false, sourceText, "", diagnostics);
    }

    /// <summary>
    /// Creates an invalid for null.
    /// </summary>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    internal static NodeSemVerValidResult CreateInvalidForNull()
    {
        VersionDiagnostic[] diagnostics = new VersionDiagnostic[1];
        diagnostics[0] = NodeSemVerInputGuard.CreateVersionNullDiagnostic();
        return CreateInvalid("", diagnostics);
    }

    /// <summary>
    /// Reports whether the represented input is valid.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsValid()
    {
        return valid;
    }

    /// <summary>
    /// Reports whether invalid.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsInvalid()
    {
        return !valid;
    }

    /// <summary>
    /// Reports whether this value has version.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasVersion()
    {
        return valid;
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
    /// Gets a version text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.InvalidOperationException">The requested value is unavailable because the operation did not complete successfully.</exception>
    internal string GetVersionText()
    {
        if (!valid)
        {
            throw new System.InvalidOperationException("Valid result does not contain version text.");
        }

        return versionText;
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

    internal static VersionDiagnostic[] CopyDiagnostics(VersionDiagnostic[] diagnosticValues)
    {
        VersionDiagnostic[] copy = new VersionDiagnostic[diagnosticValues.Length];
        int index = 0;

        while (index < diagnosticValues.Length)
        {
            if (diagnosticValues[index] == null)
            {
                throw new System.ArgumentException("Diagnostics cannot contain null values.");
            }

            copy[index] = diagnosticValues[index];
            index = index + 1;
        }

        return copy;
    }
}
