namespace version;

/// <summary>
/// Represents a maven dependency conflict.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class MavenDependencyConflict
{
    /// <summary>
    /// Stores the <c>coordinate</c> value.
    /// </summary>
    public MavenArtifactCoordinate coordinate;
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
    /// Initializes a new Maven Dependency Conflict instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="coordinateValue">The coordinate value used by the operation.</param>
    /// <param name="depthValue">The depth value used by the operation.</param>
    /// <param name="diagnosticCodeValue">The diagnostic code value used by the operation.</param>
    /// <param name="messageValue">The message value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    public MavenDependencyConflict(MavenArtifactCoordinate coordinateValue, int depthValue, string diagnosticCodeValue, string messageValue)
    {
        System.ArgumentNullException.ThrowIfNull(coordinateValue);
        System.ArgumentNullException.ThrowIfNull(diagnosticCodeValue);
        System.ArgumentNullException.ThrowIfNull(messageValue);

        if (depthValue < 0)
        {
            throw new System.ArgumentOutOfRangeException("depthValue");
        }

        coordinate = coordinateValue;
        depth = depthValue;
        diagnosticCode = diagnosticCodeValue;
        message = messageValue;
    }

    /// <summary>
    /// Creates a conflict using the standard conflicting-ranges diagnostic code.
    /// </summary>
    /// <param name="coordinate">The artifact coordinate used by the operation.</param>
    /// <param name="depth">The dependency depth used by the operation.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <returns>The resulting maven dependency conflict.</returns>
    internal static MavenDependencyConflict Create(MavenArtifactCoordinate coordinate, int depth, string message)
    {
        return new MavenDependencyConflict(
            coordinate,
            depth,
            MavenResolverDiagnosticCodes.ConflictingRanges,
            message);
    }

    /// <summary>
    /// Gets a coordinate.
    /// </summary>
    /// <returns>The resulting maven artifact coordinate.</returns>
    internal MavenArtifactCoordinate GetCoordinate()
    {
        return coordinate;
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
