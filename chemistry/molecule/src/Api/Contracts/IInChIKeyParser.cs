namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="InChIKeyParser"/>.
/// </summary>
public interface IInChIKeyParser
{
    /// <summary>
    /// Parses the supported text syntax and returns its object model.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The resulting in ch ikey record.</returns>
    InChIKeyRecord Parse(string? text);

    /// <summary>
    /// Checks the supplied value and returns diagnostics without silently repairing it.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    bool Validate(string? text);
}

