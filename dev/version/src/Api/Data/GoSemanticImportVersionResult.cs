namespace version;

/// <summary>
/// Represents the outcome of a go semantic import version operation.
/// </summary>
/// <remarks>
/// Check the success state before requesting the produced value. Failure results retain diagnostics that explain why processing stopped.
/// </remarks>
public class GoSemanticImportVersionResult
{
    /// <summary>
    /// Stores the <c>success</c> value.
    /// </summary>
    public bool success;
    /// <summary>
    /// Stores the <c>modulePath</c> value.
    /// </summary>
    public GoModulePath? modulePath;
    /// <summary>
    /// Stores the <c>version</c> value.
    /// </summary>
    public UniversalVersion? version;
    /// <summary>
    /// Stores the <c>diagnostics</c> value.
    /// </summary>
    public VersionDiagnostic[] diagnostics;

    /// <summary>
    /// Initializes a new Go Semantic Import Version Result instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="modulePathValue">The parsed module path, or null on failure.</param>
    /// <param name="versionValue">The parsed version, or null on failure.</param>
    /// <param name="diagnosticValues">The diagnostic values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public GoSemanticImportVersionResult(
        GoModulePath? modulePathValue,
        UniversalVersion? versionValue,
        VersionDiagnostic[] diagnosticValues)
    {
        System.ArgumentNullException.ThrowIfNull(diagnosticValues);

        if ((modulePathValue == null) != (versionValue == null))
        {
            throw new System.ArgumentException("A semantic import result must contain both a module path and version, or neither.");
        }

        success = modulePathValue != null;
        modulePath = modulePathValue;
        version = versionValue;
        diagnostics = CopyDiagnostics(diagnosticValues);
    }

    /// <summary>
    /// Creates a success.
    /// </summary>
    /// <param name="parsedModulePath">The parsed module path used by the operation.</param>
    /// <param name="parsedVersion">The parsed version used by the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    internal static GoSemanticImportVersionResult CreateSuccess(GoModulePath parsedModulePath, UniversalVersion parsedVersion)
    {
        return new GoSemanticImportVersionResult(parsedModulePath, parsedVersion, new VersionDiagnostic[0]);
    }

    /// <summary>
    /// Creates a failure.
    /// </summary>
    /// <param name="diagnostic">The diagnostic used by the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal static GoSemanticImportVersionResult CreateFailure(VersionDiagnostic diagnostic)
    {
        System.ArgumentNullException.ThrowIfNull(diagnostic);

        VersionDiagnostic[] diagnostics = new VersionDiagnostic[1];
        diagnostics[0] = diagnostic;
        return new GoSemanticImportVersionResult(null, null, diagnostics);
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
    /// Reports whether this value has module path.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasModulePath()
    {
        return modulePath != null;
    }

    /// <summary>
    /// Gets a module path.
    /// </summary>
    /// <returns>The resulting go module path.</returns>
    /// <exception cref="System.InvalidOperationException">The requested value is unavailable because the operation did not complete successfully.</exception>
    internal GoModulePath GetModulePath()
    {
        if (modulePath == null)
        {
            throw new System.InvalidOperationException("Go semantic import version result does not contain a module path.");
        }

        return modulePath;
    }

    /// <summary>
    /// Reports whether this value has version.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasVersion()
    {
        return version != null;
    }

    /// <summary>
    /// Gets a version.
    /// </summary>
    /// <returns>The resulting universal version.</returns>
    /// <exception cref="System.InvalidOperationException">The requested value is unavailable because the operation did not complete successfully.</exception>
    internal UniversalVersion GetVersion()
    {
        if (version == null)
        {
            throw new System.InvalidOperationException("Go semantic import version result does not contain a version.");
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
