using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxFillOperations"/>.
/// </summary>
public interface IPptxFillOperations
{
    /// <summary>
    /// Creates a new fill using the supplied values.
    /// </summary>
    /// <returns>The resulting fill value.</returns>
    PptxFill CreateNone();
    /// <summary>
    /// Creates a new fill using the supplied values.
    /// </summary>
    /// <param name="color">The color value to use.</param>
    /// <returns>The resulting fill value.</returns>
    PptxFill CreateSolid(PptxColor color);
    /// <summary>
    /// Creates a new fill using the supplied values.
    /// </summary>
    /// <param name="rgb">The rgb value to use.</param>
    /// <returns>The resulting fill value.</returns>
    PptxFill CreateSolidRgb(string rgb);
    /// <summary>
    /// Creates a new fill using the supplied values.
    /// </summary>
    /// <param name="startColor">The start color value to use.</param>
    /// <param name="endColor">The end color value to use.</param>
    /// <param name="angleDegrees">The angle degrees value to use.</param>
    /// <returns>The resulting fill value.</returns>
    PptxFill CreateGradient(PptxColor startColor, PptxColor endColor, double angleDegrees);
    /// <summary>
    /// Creates one positioned gradient stop.
    /// </summary>
    /// <param name="position">The stop position from zero through 100,000.</param>
    /// <param name="color">The stop color.</param>
    /// <returns>The resulting gradient stop.</returns>
    PptxGradientStop CreateGradientStop(int position, PptxColor color);
    /// <summary>
    /// Creates a gradient from an ordered list of two or more stops.
    /// </summary>
    /// <param name="stops">The ordered gradient stops.</param>
    /// <param name="angleDegrees">The gradient angle in degrees.</param>
    /// <returns>The resulting fill value.</returns>
    PptxFill CreateMultiStopGradient(List<PptxGradientStop> stops, double angleDegrees);
    /// <summary>
    /// Creates a new fill using the supplied values.
    /// </summary>
    /// <param name="relationshipId">The relationship identifier used in the owning XML part.</param>
    /// <returns>The resulting fill value.</returns>
    PptxFill CreatePicture(string relationshipId);
    /// <summary>
    /// Creates a new fill using the supplied values.
    /// </summary>
    /// <param name="patternType">The pattern type value to use.</param>
    /// <param name="foreground">The foreground value to use.</param>
    /// <param name="background">The background value to use.</param>
    /// <returns>The resulting fill value.</returns>
    PptxFill CreatePattern(string patternType, PptxColor foreground, PptxColor background);
    /// <summary>
    /// Creates a new fill using the supplied values.
    /// </summary>
    /// <returns>The resulting fill value.</returns>
    PptxFill CreateGroup();
    /// <summary>
    /// Sets transparency on a supported fill.
    /// </summary>
    /// <param name="target">The fill to update.</param>
    /// <param name="transparency">Transparency from zero through 100,000.</param>
    void SetTransparency(PptxFill target, int transparency);
    /// <summary>
    /// Sets the angle and shape-rotation behavior of a gradient fill.
    /// </summary>
    /// <param name="target">The fill to update.</param>
    /// <param name="angleDegrees">The gradient angle in degrees.</param>
    /// <param name="rotateWithShape">Whether the fill rotates with its shape.</param>
    void SetGradientRotation(PptxFill target, double angleDegrees, bool rotateWithShape);
    /// <summary>
    /// Sets the source crop rectangle of a picture fill.
    /// </summary>
    /// <param name="target">The fill to update.</param>
    /// <param name="crop">The source crop rectangle.</param>
    void SetPictureCrop(PptxFill target, PptxImageCrop crop);
    /// <summary>
    /// Selects stretch mode and sets its inset rectangle.
    /// </summary>
    /// <param name="target">The fill to update.</param>
    /// <param name="stretch">The stretch settings.</param>
    void SetPictureStretch(PptxFill target, PptxFillStretch stretch);
    /// <summary>
    /// Selects tile mode and sets its placement and scaling.
    /// </summary>
    /// <param name="target">The fill to update.</param>
    /// <param name="tile">The tile settings.</param>
    void SetPictureTile(PptxFill target, PptxFillTile tile);
    /// <summary>
    /// Sets whether a picture fill rotates with its owning shape.
    /// </summary>
    /// <param name="target">The fill to update.</param>
    /// <param name="rotateWithShape">Whether the fill rotates with its shape.</param>
    void SetPictureRotation(PptxFill target, bool rotateWithShape);
    /// <summary>
    /// Reads a DrawingML fill element.
    /// </summary>
    /// <param name="xml">The fill element XML.</param>
    /// <returns>The parsed fill.</returns>
    PptxFill ParseXml(string xml);
    /// <summary>
    /// Writes a DrawingML fill element.
    /// </summary>
    /// <param name="fill">The fill to write.</param>
    /// <returns>The serialized fill XML.</returns>
    string ToXml(PptxFill fill);
}
