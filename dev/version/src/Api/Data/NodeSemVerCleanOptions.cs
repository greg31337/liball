namespace version;

/// <summary>
/// Configures node semver clean behavior.
/// </summary>
/// <remarks>
/// Configure behavior through the public fields before passing the value to an operation.
/// </remarks>
public class NodeSemVerCleanOptions
{
    /// <summary>
    /// Stores the <c>loose</c> value.
    /// </summary>
    public bool loose;

    /// <summary>
    /// Initializes a new Node SemVer Clean Options instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="looseValue">Whether to enable loose value.</param>
    public NodeSemVerCleanOptions(bool looseValue)
    {
        loose = looseValue;
    }

    /// <summary>
    /// Creates a strict.
    /// </summary>
    /// <returns>The resulting node semver clean options.</returns>
    internal static NodeSemVerCleanOptions CreateStrict()
    {
        return new NodeSemVerCleanOptions(false);
    }

    /// <summary>
    /// Creates a loose.
    /// </summary>
    /// <returns>The resulting node semver clean options.</returns>
    internal static NodeSemVerCleanOptions CreateLoose()
    {
        return new NodeSemVerCleanOptions(true);
    }

    /// <summary>
    /// Creates a with loose mode.
    /// </summary>
    /// <param name="looseValue">Whether to enable loose value.</param>
    /// <returns>The resulting node semver clean options.</returns>
    internal static NodeSemVerCleanOptions CreateWithLooseMode(bool looseValue)
    {
        return new NodeSemVerCleanOptions(looseValue);
    }

    /// <summary>
    /// Reports whether loose.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsLoose()
    {
        return loose;
    }

    /// <summary>
    /// Reports whether strict.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsStrict()
    {
        return !loose;
    }

    /// <summary>
    /// Updates the loose setting.
    /// </summary>
    /// <param name="looseValue">Whether to enable loose value.</param>
    internal void SetLoose(bool looseValue)
    {
        loose = looseValue;
    }
}
