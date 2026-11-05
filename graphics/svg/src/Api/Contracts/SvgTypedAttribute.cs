namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgTypedAttributeAccessor"/>.</summary>
public interface ISvgTypedAttributeAccessor
{
    /// <summary>Creates an accessor from explicit bounded parser options.</summary>
    /// <param name="options">The element catalog and parser limits.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an option or required option member is invalid.</exception>
    /// <returns>A new typed attribute accessor.</returns>
    SvgTypedAttributeAccessor Create(SvgTypedAttributeOptions options);
    /// <summary>Gets an unnamespaced attribute value, parsing it only on a cache miss.</summary>
    /// <param name="node">The tagged SVG element.</param>
    /// <param name="localName">The case-sensitive local name.</param>
    /// <returns>A source-preserving tagged typed value.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when the node is not an element or the name is empty.</exception>
    SvgTypedAttributeValue Get(SvgNode node, string localName);
    /// <summary>Gets an attribute by expanded XML name, parsing it only on a cache miss.</summary>
    /// <param name="node">The tagged SVG element.</param>
    /// <param name="namespaceUri">The resolved namespace URI.</param>
    /// <param name="localName">The case-sensitive local name.</param>
    /// <returns>A source-preserving tagged typed value.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when the node is not an element or the name is empty.</exception>
    SvgTypedAttributeValue GetNamespaced(SvgNode node, string namespaceUri, string localName);
    /// <summary>Clears every cached result while retaining cumulative statistics.</summary>
    void Clear();
    /// <summary>Returns a snapshot of cache and parsing activity.</summary>
    /// <returns>The current statistics snapshot.</returns>
    SvgTypedAttributeStatistics GetStatistics();
}
