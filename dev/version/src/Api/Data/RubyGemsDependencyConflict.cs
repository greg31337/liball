namespace version;

/// <summary>
/// Represents a rubygems dependency conflict.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class RubyGemsDependencyConflict
{
    /// <summary>
    /// Stores the <c>name</c> value.
    /// </summary>
    public string name;
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
    /// Initializes a new RubyGems Dependency Conflict instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="nameValue">The name value used by the operation.</param>
    /// <param name="depthValue">The depth value used by the operation.</param>
    /// <param name="diagnosticCodeValue">The diagnostic code value used by the operation.</param>
    /// <param name="messageValue">The message value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    public RubyGemsDependencyConflict(string nameValue, int depthValue, string diagnosticCodeValue, string messageValue)
    {
        RubyGemsDependency.ValidateName(nameValue, "RubyGems dependency conflict name cannot be empty or contain invalid characters.");
        System.ArgumentNullException.ThrowIfNull(diagnosticCodeValue);
        System.ArgumentNullException.ThrowIfNull(messageValue);

        if (depthValue < 0)
        {
            throw new System.ArgumentOutOfRangeException("depthValue");
        }

        name = nameValue;
        depth = depthValue;
        diagnosticCode = diagnosticCodeValue;
        message = messageValue;
    }

    /// <summary>
    /// Creates a conflict using the standard unsatisfied-constraints diagnostic code.
    /// </summary>
    /// <param name="name">The dependency name used by the operation.</param>
    /// <param name="depth">The dependency depth used by the operation.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <returns>The resulting rubygems dependency conflict.</returns>
    internal static RubyGemsDependencyConflict Create(string name, int depth, string message)
    {
        return new RubyGemsDependencyConflict(
            name,
            depth,
            RubyGemsResolverDiagnosticCodes.UnsatisfiedConstraints,
            message);
    }

    /// <summary>
    /// Gets a name.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetName()
    {
        return name;
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
