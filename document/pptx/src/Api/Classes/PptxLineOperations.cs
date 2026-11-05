using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace pptx;

/// <summary>
/// Provides public operations for PptxLine values.
/// </summary>
public class PptxLineOperations : IPptxLineOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxLineOperations()
    {
    }

    /// <summary>
    /// Creates a new line using the supplied values.
    /// </summary>
    /// <returns>The resulting line value.</returns>
    public PptxLine CreateNone()
    {
        return PptxLine.CreateNone();
    }

    /// <summary>
    /// Creates a new line using the supplied values.
    /// </summary>
    /// <param name="color">The color value to use.</param>
    /// <param name="width">The width in English Metric Units (EMUs), unless the method states another unit.</param>
    /// <returns>The resulting line value.</returns>
    public PptxLine CreateSolid(PptxColor color, long width)
    {
        return PptxLine.CreateSolid(color, width);
    }

    /// <summary>
    /// Creates a new line using the supplied values.
    /// </summary>
    /// <param name="rgb">The rgb value to use.</param>
    /// <param name="width">The width in English Metric Units (EMUs), unless the method states another unit.</param>
    /// <returns>The resulting line value.</returns>
    public PptxLine CreateSolidRgb(string rgb, long width)
    {
        return PptxLine.CreateSolidRgb(rgb, width);
    }

    /// <summary>
    /// Creates one custom dash and space pair.
    /// </summary>
    /// <param name="dashLength">The dash length in thousandths of one percent.</param>
    /// <param name="spaceLength">The space length in thousandths of one percent.</param>
    /// <returns>The custom dash pair.</returns>
    public PptxLineDash CreateCustomDash(int dashLength, int spaceLength)
    {
        return PptxLineDash.Create(dashLength, spaceLength);
    }

    /// <summary>
    /// Sets line cap, compound, and alignment values.
    /// </summary>
    /// <param name="target">The line to update.</param>
    /// <param name="cap">The DrawingML cap value.</param>
    /// <param name="compound">The DrawingML compound value.</param>
    /// <param name="alignment">The DrawingML alignment value.</param>
    public void SetAppearance(PptxLine target, string cap, string compound, string alignment)
    {
        target.SetAppearance(cap, compound, alignment);
    }

    /// <summary>
    /// Selects a preset dash pattern.
    /// </summary>
    /// <param name="target">The line to update.</param>
    /// <param name="dash">The DrawingML preset dash value.</param>
    public void SetPresetDash(PptxLine target, string dash)
    {
        target.SetPresetDash(dash);
    }

    /// <summary>
    /// Selects a custom dash pattern.
    /// </summary>
    /// <param name="target">The line to update.</param>
    /// <param name="dashes">The dash and space pairs.</param>
    public void SetCustomDash(PptxLine target, List<PptxLineDash> dashes)
    {
        target.SetCustomDash(dashes);
    }

    /// <summary>
    /// Sets the line join and optional miter limit.
    /// </summary>
    /// <param name="target">The line to update.</param>
    /// <param name="join">The join value.</param>
    /// <param name="miterLimit">The miter limit in thousandths of one percent.</param>
    public void SetJoin(PptxLine target, string join, int miterLimit)
    {
        target.SetJoin(join, miterLimit);
    }

    /// <summary>
    /// Sets head and tail arrow type, width, and length values.
    /// </summary>
    /// <param name="target">The line to update.</param>
    /// <param name="headType">The head arrow type.</param>
    /// <param name="headWidth">The head arrow width.</param>
    /// <param name="headLength">The head arrow length.</param>
    /// <param name="tailType">The tail arrow type.</param>
    /// <param name="tailWidth">The tail arrow width.</param>
    /// <param name="tailLength">The tail arrow length.</param>
    public void SetArrows(PptxLine target, string headType, string headWidth, string headLength, string tailType, string tailWidth, string tailLength)
    {
        target.SetArrows(headType, headWidth, headLength, tailType, tailWidth, tailLength);
    }

    /// <summary>
    /// Sets line transparency.
    /// </summary>
    /// <param name="target">The line to update.</param>
    /// <param name="transparency">Transparency from zero through 100,000.</param>
    public void SetTransparency(PptxLine target, int transparency)
    {
        target.SetTransparency(transparency);
    }

    /// <summary>
    /// Reads a DrawingML line element.
    /// </summary>
    /// <param name="xml">The line XML.</param>
    /// <returns>The parsed line.</returns>
    public PptxLine ParseXml(string xml)
    {
        XmlDocument document = PptxXml.LoadString(xml);
        XmlElement? element = document.DocumentElement;
        if (element == null || element.NamespaceURI != PptxNamespaces.Drawing || element.LocalName != "ln")
        {
            throw new System.ArgumentException("The XML root must be a DrawingML line element.", "xml");
        }

        return PptxLine.FromElement(element);
    }

    /// <summary>
    /// Writes a DrawingML line element.
    /// </summary>
    /// <param name="line">The line to write.</param>
    /// <returns>The serialized line XML.</returns>
    public string ToXml(PptxLine line)
    {
        if (line == null)
        {
            throw new System.ArgumentNullException("line");
        }

        XmlDocument document = new XmlDocument();
        document.AppendChild(line.ToElement(document));
        return document.OuterXml;
    }
}
