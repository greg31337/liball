namespace email;
/// <summary>
/// Specifies the supported traversal values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailTraversalMode
{
    /// <summary>
    /// Represents the depth first choice or state.
    /// </summary>
    DepthFirst = 0,
    /// <summary>
    /// Represents the breadth first choice or state.
    /// </summary>
    BreadthFirst = 1
}
