namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgPatchPath"/>.</summary>
public interface ISvgPatchPath
{
    /// <summary>Returns the stable child-index path of a connected document node.</summary>
    /// <param name="node">The connected node.</param>
    /// <returns><c>/</c> for the document node or a slash-separated child-index path.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentException">Thrown when parent and child links are inconsistent.</exception>
    string Get(SvgNode node);
    /// <summary>Resolves a stable child-index path against a document.</summary>
    /// <param name="document">The document containing the path.</param>
    /// <param name="path">The slash-separated path.</param>
    /// <returns>The resolved node, or <c>null</c> for malformed or missing paths.</returns>
    SvgNode? Resolve(SvgDocument? document, string? path);
}
