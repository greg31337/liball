namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifPixelAspectRatio"/>.</summary>
public interface IAvifPixelAspectRatio
{
    /// <summary>Returns the horizontal pixel spacing.</summary>
    /// <returns>The positive horizontal spacing.</returns>
    uint GetHorizontalSpacing();

    /// <summary>Returns the vertical pixel spacing.</summary>
    /// <returns>The positive vertical spacing.</returns>
    uint GetVerticalSpacing();

    /// <summary>Returns the horizontal-to-vertical spacing as a floating-point ratio.</summary>
    /// <returns>The pixel aspect ratio.</returns>
    double GetRatio();
}
