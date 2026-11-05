using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace pptx;

/// <summary>
/// Provides public operations for PptxFill values.
/// </summary>
public class PptxFillOperations : IPptxFillOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxFillOperations()
    {
    }

    /// <summary>
    /// Creates a new fill using the supplied values.
    /// </summary>
    /// <returns>The resulting fill value.</returns>
    public PptxFill CreateNone()
    {
        return PptxFill.CreateNone();
    }

    /// <summary>
    /// Creates a new fill using the supplied values.
    /// </summary>
    /// <param name="color">The color value to use.</param>
    /// <returns>The resulting fill value.</returns>
    public PptxFill CreateSolid(PptxColor color)
    {
        return PptxFill.CreateSolid(color);
    }

    /// <summary>
    /// Creates a new fill using the supplied values.
    /// </summary>
    /// <param name="rgb">The rgb value to use.</param>
    /// <returns>The resulting fill value.</returns>
    public PptxFill CreateSolidRgb(string rgb)
    {
        return PptxFill.CreateSolidRgb(rgb);
    }

    /// <summary>
    /// Creates a new fill using the supplied values.
    /// </summary>
    /// <param name="startColor">The start color value to use.</param>
    /// <param name="endColor">The end color value to use.</param>
    /// <param name="angleDegrees">The angle degrees value to use.</param>
    /// <returns>The resulting fill value.</returns>
    public PptxFill CreateGradient(PptxColor startColor, PptxColor endColor, double angleDegrees)
    {
        return PptxFill.CreateGradient(startColor, endColor, angleDegrees);
    }

    /// <summary>
    /// Creates one positioned gradient stop.
    /// </summary>
    /// <param name="position">The stop position from zero through 100,000.</param>
    /// <param name="color">The stop color.</param>
    /// <returns>The resulting gradient stop.</returns>
    public PptxGradientStop CreateGradientStop(int position, PptxColor color)
    {
        return PptxGradientStop.Create(position, color);
    }

    /// <summary>
    /// Creates a gradient from an ordered list of two or more stops.
    /// </summary>
    /// <param name="stops">The ordered gradient stops.</param>
    /// <param name="angleDegrees">The gradient angle in degrees.</param>
    /// <returns>The resulting fill value.</returns>
    public PptxFill CreateMultiStopGradient(List<PptxGradientStop> stops, double angleDegrees)
    {
        return PptxFill.CreateMultiStopGradient(stops, angleDegrees);
    }

    /// <summary>
    /// Creates a new fill using the supplied values.
    /// </summary>
    /// <param name="relationshipId">The relationship identifier used in the owning XML part.</param>
    /// <returns>The resulting fill value.</returns>
    public PptxFill CreatePicture(string relationshipId)
    {
        return PptxFill.CreatePicture(relationshipId);
    }

    /// <summary>
    /// Creates a new fill using the supplied values.
    /// </summary>
    /// <param name="patternType">The pattern type value to use.</param>
    /// <param name="foreground">The foreground value to use.</param>
    /// <param name="background">The background value to use.</param>
    /// <returns>The resulting fill value.</returns>
    public PptxFill CreatePattern(string patternType, PptxColor foreground, PptxColor background)
    {
        return PptxFill.CreatePattern(patternType, foreground, background);
    }

    /// <summary>
    /// Creates a new fill using the supplied values.
    /// </summary>
    /// <returns>The resulting fill value.</returns>
    public PptxFill CreateGroup()
    {
        return PptxFill.CreateGroup();
    }

    /// <summary>
    /// Sets transparency on a supported fill.
    /// </summary>
    /// <param name="target">The fill to update.</param>
    /// <param name="transparency">Transparency from zero through 100,000.</param>
    public void SetTransparency(PptxFill target, int transparency)
    {
        target.SetTransparency(transparency);
    }

    /// <summary>
    /// Sets the angle and shape-rotation behavior of a gradient fill.
    /// </summary>
    /// <param name="target">The fill to update.</param>
    /// <param name="angleDegrees">The gradient angle in degrees.</param>
    /// <param name="rotateWithShape">Whether the fill rotates with its shape.</param>
    public void SetGradientRotation(PptxFill target, double angleDegrees, bool rotateWithShape)
    {
        target.SetGradientRotation(angleDegrees, rotateWithShape);
    }

    /// <summary>
    /// Sets the source crop rectangle of a picture fill.
    /// </summary>
    /// <param name="target">The fill to update.</param>
    /// <param name="crop">The source crop rectangle.</param>
    public void SetPictureCrop(PptxFill target, PptxImageCrop crop)
    {
        target.SetPictureCrop(crop);
    }

    /// <summary>
    /// Selects stretch mode and sets its inset rectangle.
    /// </summary>
    /// <param name="target">The fill to update.</param>
    /// <param name="stretch">The stretch settings.</param>
    public void SetPictureStretch(PptxFill target, PptxFillStretch stretch)
    {
        target.SetPictureStretch(stretch);
    }

    /// <summary>
    /// Selects tile mode and sets its placement and scaling.
    /// </summary>
    /// <param name="target">The fill to update.</param>
    /// <param name="tile">The tile settings.</param>
    public void SetPictureTile(PptxFill target, PptxFillTile tile)
    {
        target.SetPictureTile(tile);
    }

    /// <summary>
    /// Sets whether a picture fill rotates with its owning shape.
    /// </summary>
    /// <param name="target">The fill to update.</param>
    /// <param name="rotateWithShape">Whether the fill rotates with its shape.</param>
    public void SetPictureRotation(PptxFill target, bool rotateWithShape)
    {
        target.SetPictureRotation(rotateWithShape);
    }

    /// <summary>
    /// Reads a DrawingML fill element.
    /// </summary>
    /// <param name="xml">The fill element XML.</param>
    /// <returns>The parsed fill.</returns>
    public PptxFill ParseXml(string xml)
    {
        XmlDocument document = PptxXml.LoadString(xml);
        XmlElement? element = document.DocumentElement;
        if (element == null || element.NamespaceURI != PptxNamespaces.Drawing || !PptxFill.IsFillElement(element.LocalName))
        {
            throw new System.ArgumentException("The XML root must be a supported DrawingML fill element.", "xml");
        }

        return PptxFill.FromElement(element);
    }

    /// <summary>
    /// Writes a DrawingML fill element.
    /// </summary>
    /// <param name="fill">The fill to write.</param>
    /// <returns>The serialized fill XML.</returns>
    public string ToXml(PptxFill fill)
    {
        if (fill == null)
        {
            throw new System.ArgumentNullException("fill");
        }

        XmlDocument document = new XmlDocument();
        document.AppendChild(fill.ToElement(document));
        return document.OuterXml;
    }
}
