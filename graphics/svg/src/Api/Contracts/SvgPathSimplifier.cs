namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgPathSimplifier"/>.</summary>
public interface ISvgPathSimplifier
{
    /// <summary>Simplifies and rounds a path with deterministic defaults.</summary>
    /// <param name="path">The valid mutable path.</param>
    /// <returns>The bounded lossy result or a stable failure.</returns>
    SvgPathSimplifyResult SimplifyWithPath(SvgPathData path);
    /// <summary>Simplifies and rounds a path within explicit error and security limits.</summary>
    /// <param name="path">The valid mutable path.</param><param name="options">The tolerance, precision, and limits.</param>
    /// <returns>The bounded lossy result or a stable failure.</returns>
    SvgPathSimplifyResult SimplifyWithPathAndOptions(SvgPathData path, SvgPathSimplifyOptions options);
}
