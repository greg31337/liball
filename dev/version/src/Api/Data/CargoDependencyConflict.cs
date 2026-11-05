namespace version;

/// <summary>
/// Represents a cargo dependency conflict.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class CargoDependencyConflict
{
    /// <summary>
    /// Stores the <c>packageName</c> value.
    /// </summary>
    public CargoPackageName packageName;
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
    /// Initializes a new Cargo Dependency Conflict instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="packageNameValue">The package name value used by the operation.</param>
    /// <param name="depthValue">The depth value used by the operation.</param>
    /// <param name="diagnosticCodeValue">The diagnostic code value used by the operation.</param>
    /// <param name="messageValue">The message value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    public CargoDependencyConflict(CargoPackageName packageNameValue, int depthValue, string diagnosticCodeValue, string messageValue)
    {
        System.ArgumentNullException.ThrowIfNull(packageNameValue);
        System.ArgumentNullException.ThrowIfNull(diagnosticCodeValue);
        System.ArgumentNullException.ThrowIfNull(messageValue);

        if (depthValue < 0)
        {
            throw new System.ArgumentOutOfRangeException("depthValue");
        }

        packageName = packageNameValue;
        depth = depthValue;
        diagnosticCode = diagnosticCodeValue;
        message = messageValue;
    }

    /// <summary>
    /// Creates a conflict using the standard unsatisfied-constraints diagnostic code.
    /// </summary>
    /// <param name="packageName">The package name used by the operation.</param>
    /// <param name="depth">The dependency depth used by the operation.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <returns>The resulting cargo dependency conflict.</returns>
    internal static CargoDependencyConflict Create(CargoPackageName packageName, int depth, string message)
    {
        return new CargoDependencyConflict(
            packageName,
            depth,
            CargoResolverDiagnosticCodes.UnsatisfiedConstraints,
            message);
    }

    /// <summary>
    /// Gets a package name.
    /// </summary>
    /// <returns>The resulting cargo package name.</returns>
    internal CargoPackageName GetPackageName()
    {
        return packageName;
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
