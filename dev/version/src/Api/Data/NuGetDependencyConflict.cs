namespace version;

/// <summary>
/// Represents a nuget dependency conflict.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class NuGetDependencyConflict
{
    /// <summary>
    /// Stores the <c>packageId</c> value.
    /// </summary>
    public NuGetPackageId packageId;
    /// <summary>
    /// Stores the <c>depth</c> value.
    /// </summary>
    public int depth;
    /// <summary>
    /// Stores the <c>diagnosticCode</c> value.
    /// </summary>
    public string diagnosticCode;
    /// <summary>
    /// Stores the <c>message</c> value.
    /// </summary>
    public string message;

    /// <summary>
    /// Initializes a new NuGet Dependency Conflict instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="packageIdValue">The package id value used by the operation.</param>
    /// <param name="depthValue">The depth value used by the operation.</param>
    /// <param name="diagnosticCodeValue">The diagnostic code value used by the operation.</param>
    /// <param name="messageValue">The message value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    public NuGetDependencyConflict(NuGetPackageId packageIdValue, int depthValue, string diagnosticCodeValue, string messageValue)
    {
        System.ArgumentNullException.ThrowIfNull(packageIdValue);
        System.ArgumentNullException.ThrowIfNull(diagnosticCodeValue);
        System.ArgumentNullException.ThrowIfNull(messageValue);

        if (depthValue < 0)
        {
            throw new System.ArgumentOutOfRangeException("depthValue");
        }

        packageId = packageIdValue;
        depth = depthValue;
        diagnosticCode = diagnosticCodeValue;
        message = messageValue;
    }

    /// <summary>
    /// Creates a conflict using the standard unsatisfied-constraints diagnostic code.
    /// </summary>
    /// <param name="packageId">The package identifier used by the operation.</param>
    /// <param name="depth">The dependency depth used by the operation.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <returns>The resulting nuget dependency conflict.</returns>
    internal static NuGetDependencyConflict Create(NuGetPackageId packageId, int depth, string message)
    {
        return new NuGetDependencyConflict(
            packageId,
            depth,
            NuGetResolverDiagnosticCodes.UnsatisfiedConstraints,
            message);
    }

    /// <summary>
    /// Gets a package id.
    /// </summary>
    /// <returns>The resulting nuget package id.</returns>
    internal NuGetPackageId GetPackageId()
    {
        return packageId;
    }

    /// <summary>
    /// Gets a depth.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetDepth()
    {
        return depth;
    }

    /// <summary>
    /// Gets a diagnostic code.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetDiagnosticCode()
    {
        return diagnosticCode;
    }

    /// <summary>
    /// Gets a message.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetMessage()
    {
        return message;
    }
}
