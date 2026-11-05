namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgAnimationTargetResolver"/>.</summary>
public interface ISvgAnimationTargetResolver
{
    /// <summary>Resolves one animation using secure default catalogs and bounds.</summary>
    /// <param name="source">The animation element.</param>
    /// <returns>The resolved animation target and property.</returns>
    SvgAnimation ResolveWithSource(SvgNode source);
    /// <summary>Resolves one animation using explicit catalogs, reference policy, and bounds.</summary>
    /// <param name="source">The animation element.</param>
    /// <param name="options">The target-resolution options.</param>
    /// <returns>The resolved animation target and property.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    SvgAnimation ResolveWithSourceAndOptions(SvgNode source, SvgAnimationTargetOptions options);
}
