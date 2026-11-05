namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgIntersections"/>.</summary>
public interface ISvgIntersections
{
    /// <summary>Finds ordered distinct intersections between two tagged segments.</summary>
    /// <param name="first">The first segment.</param><param name="second">The second segment.</param><param name="options">The tolerance and security limits.</param>
    /// <returns>The distinct intersections or a stable failure.</returns>
    SvgIntersectionResult Find(SvgIntersectionSegment first, SvgIntersectionSegment second, SvgIntersectionOptions options);
}
