namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="InChIParser"/>.
/// </summary>
public interface IInChIParser
{
    /// <summary>
    /// Parses the supported text syntax and returns its object model.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The resulting in ch irecord.</returns>
    InChIRecord Parse(string? text);
}

