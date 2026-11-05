using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxLineOperations"/>.
/// </summary>
public interface IPptxLineOperations
{
    /// <summary>
    /// Creates a new line using the supplied values.
    /// </summary>
    /// <returns>The resulting line value.</returns>
    PptxLine CreateNone();
    /// <summary>
    /// Creates a new line using the supplied values.
    /// </summary>
    /// <param name="color">The color value to use.</param>
    /// <param name="width">The width in English Metric Units (EMUs), unless the method states another unit.</param>
    /// <returns>The resulting line value.</returns>
    PptxLine CreateSolid(PptxColor color, long width);
    /// <summary>
    /// Creates a new line using the supplied values.
    /// </summary>
    /// <param name="rgb">The rgb value to use.</param>
    /// <param name="width">The width in English Metric Units (EMUs), unless the method states another unit.</param>
    /// <returns>The resulting line value.</returns>
    PptxLine CreateSolidRgb(string rgb, long width);
    /// <summary>
    /// Creates one custom dash and space pair.
    /// </summary>
    /// <param name="dashLength">The dash length in thousandths of one percent.</param>
    /// <param name="spaceLength">The space length in thousandths of one percent.</param>
    /// <returns>The custom dash pair.</returns>
    PptxLineDash CreateCustomDash(int dashLength, int spaceLength);
    /// <summary>
    /// Sets line cap, compound, and alignment values.
    /// </summary>
    /// <param name="target">The line to update.</param>
    /// <param name="cap">The DrawingML cap value.</param>
    /// <param name="compound">The DrawingML compound value.</param>
    /// <param name="alignment">The DrawingML alignment value.</param>
    void SetAppearance(PptxLine target, string cap, string compound, string alignment);
    /// <summary>
    /// Selects a preset dash pattern.
    /// </summary>
    /// <param name="target">The line to update.</param>
    /// <param name="dash">The DrawingML preset dash value.</param>
    void SetPresetDash(PptxLine target, string dash);
    /// <summary>
    /// Selects a custom dash pattern.
    /// </summary>
    /// <param name="target">The line to update.</param>
    /// <param name="dashes">The dash and space pairs.</param>
    void SetCustomDash(PptxLine target, List<PptxLineDash> dashes);
    /// <summary>
    /// Sets the line join and optional miter limit.
    /// </summary>
    /// <param name="target">The line to update.</param>
    /// <param name="join">The join value.</param>
    /// <param name="miterLimit">The miter limit in thousandths of one percent.</param>
    void SetJoin(PptxLine target, string join, int miterLimit);
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
    void SetArrows(PptxLine target, string headType, string headWidth, string headLength, string tailType, string tailWidth, string tailLength);
    /// <summary>
    /// Sets line transparency.
    /// </summary>
    /// <param name="target">The line to update.</param>
    /// <param name="transparency">Transparency from zero through 100,000.</param>
    void SetTransparency(PptxLine target, int transparency);
    /// <summary>
    /// Reads a DrawingML line element.
    /// </summary>
    /// <param name="xml">The line XML.</param>
    /// <returns>The parsed line.</returns>
    PptxLine ParseXml(string xml);
    /// <summary>
    /// Writes a DrawingML line element.
    /// </summary>
    /// <param name="line">The line to write.</param>
    /// <returns>The serialized line XML.</returns>
    string ToXml(PptxLine line);
}
