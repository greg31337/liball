namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="ColorPaintStop"/>.</summary>
public interface IColorPaintStop
{
    /// <summary>Gets the normalized stop offset.</summary>
    /// <returns>The stop offset.</returns>
    double GetOffset();

    /// <summary>Gets the resolved color paint for this stop.</summary>
    /// <returns>The palette or foreground color paint.</returns>
    ColorPaint GetColor();
}
