namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgNodeNames"/>.</summary>
public interface ISvgNodeNames
{
    /// <summary>Returns the recognized element kind for an expanded XML name.</summary>
    /// <param name="namespaceUri">The resolved namespace URI.</param>
    /// <param name="localName">The case-sensitive local name.</param>
    /// <returns>The recognized SVG kind, or <c>Unknown</c>.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    SvgElementKind GetElementKind(string namespaceUri, string localName);
    /// <summary>Tests whether a recognized element belongs to the SVG 2 table.</summary>
    /// <param name="kind">The element kind.</param>
    /// <returns><c>true</c> for SVG 2 element kinds.</returns>
    bool IsSvg2Element(SvgElementKind kind);
    /// <summary>Tests whether a recognized element is preserved for SVG 1.1 compatibility.</summary>
    /// <param name="kind">The element kind.</param>
    /// <returns><c>true</c> for legacy SVG 1.1 element kinds.</returns>
    bool IsSvg11LegacyElement(SvgElementKind kind);
    /// <summary>Returns the recognized attribute category for an expanded XML name.</summary>
    /// <param name="namespaceUri">The resolved namespace URI.</param>
    /// <param name="localName">The case-sensitive local name.</param>
    /// <returns>The recognized attribute category, or <c>Unknown</c>.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    SvgAttributeKind GetAttributeKind(string namespaceUri, string localName);
}
