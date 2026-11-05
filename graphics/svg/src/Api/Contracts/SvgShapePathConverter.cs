namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgShapePathConverter"/>.</summary>
public interface ISvgShapePathConverter
{
    /// <summary>Converts a resolved basic shape without an additional command restriction.</summary>
    /// <param name="shape">The resolved line, rectangle, circle, ellipse, polyline, or polygon.</param>
    /// <returns>The generated path or a stable conversion failure.</returns>
    SvgShapePathResult ConvertWithShape(SvgShapeResult shape);
    /// <summary>Converts a resolved basic shape within a positive command limit.</summary>
    /// <param name="shape">The resolved line, rectangle, circle, ellipse, polyline, or polygon.</param>
    /// <param name="maximumCommands">The positive maximum generated command count.</param>
    /// <returns>The generated path or a stable conversion failure.</returns>
    SvgShapePathResult ConvertWithShapeAndMaximumCommands(SvgShapeResult? shape, int maximumCommands);
}
