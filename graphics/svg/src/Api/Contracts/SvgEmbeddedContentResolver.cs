namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgEmbeddedContentResolver"/>.</summary>
public interface ISvgEmbeddedContentResolver
{
    /// <summary>Resolves one supported embedded-content element using secure defaults.</summary>
    /// <param name="node">The supported embedded-content element.</param>
    /// <returns>The resolved geometry and host record.</returns>
    SvgEmbeddedContentResult ResolveWithNode(SvgNode node);
    /// <summary>Resolves one supported embedded-content element with explicit host and geometry context.</summary>
    /// <param name="node">The supported embedded-content element.</param>
    /// <param name="options">The computed style, intrinsic data, host availability, and bounds.</param>
    /// <returns>The resolved geometry and host record.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when the node is not a supported embedded-content element.</exception>
    SvgEmbeddedContentResult ResolveWithNodeAndOptions(SvgNode node, SvgEmbeddedContentResolveOptions options);
}
