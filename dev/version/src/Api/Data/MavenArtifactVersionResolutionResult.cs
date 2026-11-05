namespace version;

/// <summary>
/// Represents the outcome of a maven artifact version resolution operation.
/// </summary>
/// <remarks>
/// Check the success state before requesting the produced value. Failure results retain diagnostics that explain why processing stopped.
/// </remarks>
public class MavenArtifactVersionResolutionResult
{
    /// <summary>
    /// Stores the <c>success</c> value.
    /// </summary>
    public bool success;
    /// <summary>
    /// Stores the <c>identity</c> value.
    /// </summary>
    public MavenArtifactVersionIdentity? identity;
    /// <summary>
    /// Stores the <c>diagnostics</c> value.
    /// </summary>
    public VersionDiagnostic[] diagnostics;

    /// <summary>
    /// Initializes a new Maven Artifact Version Resolution Result instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="identityValue">The resolved identity, or null on failure.</param>
    /// <param name="diagnosticValues">The diagnostic values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public MavenArtifactVersionResolutionResult(
        MavenArtifactVersionIdentity? identityValue,
        VersionDiagnostic[] diagnosticValues)
    {
        System.ArgumentNullException.ThrowIfNull(diagnosticValues);

        success = identityValue != null;
        identity = identityValue;
        diagnostics = CopyDiagnostics(diagnosticValues);
    }

    /// <summary>
    /// Creates a success.
    /// </summary>
    /// <param name="resolvedIdentity">The resolved identity used by the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    internal static MavenArtifactVersionResolutionResult CreateSuccess(MavenArtifactVersionIdentity resolvedIdentity)
    {
        return new MavenArtifactVersionResolutionResult(resolvedIdentity, new VersionDiagnostic[0]);
    }

    /// <summary>
    /// Creates a failure.
    /// </summary>
    /// <param name="diagnostic">The diagnostic used by the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal static MavenArtifactVersionResolutionResult CreateFailure(VersionDiagnostic diagnostic)
    {
        System.ArgumentNullException.ThrowIfNull(diagnostic);

        VersionDiagnostic[] diagnostics = new VersionDiagnostic[1];
        diagnostics[0] = diagnostic;
        return new MavenArtifactVersionResolutionResult(null, diagnostics);
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
    /// Reports whether this value has identity.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasIdentity()
    {
        return identity != null;
    }

    /// <summary>
    /// Gets an identity.
    /// </summary>
    /// <returns>The resulting maven artifact version identity.</returns>
    /// <exception cref="System.InvalidOperationException">The requested value is unavailable because the operation did not complete successfully.</exception>
    internal MavenArtifactVersionIdentity GetIdentity()
    {
        if (identity == null)
        {
            throw new System.InvalidOperationException("Maven artifact version resolution result does not contain an identity.");
        }

        return identity;
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
