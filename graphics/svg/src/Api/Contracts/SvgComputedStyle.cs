namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgComputedStyleConverter"/>.</summary>
public interface ISvgComputedStyleConverter
{
    /// <summary>Converts one element cascade using default options.</summary>
    /// <param name="node">The source SVG element.</param>
    /// <param name="cascade">The completed cascade result.</param>
    /// <returns>The typed computed style.</returns>
    SvgComputedStyle ConvertWithNodeAndCascade(SvgNode node, SvgCascadeResult cascade);
    /// <summary>Converts one element cascade using explicit metadata and limits.</summary>
    /// <param name="node">The source SVG element.</param>
    /// <param name="cascade">The completed cascade result.</param>
    /// <param name="options">The metadata, parent style, and limit options.</param>
    /// <returns>The typed computed style and diagnostics.</returns>
    SvgComputedStyle ConvertWithNodeAndCascadeAndOptions(SvgNode? node, SvgCascadeResult cascade, SvgComputedStyleOptions options);
}
