using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxCommentsPart"/> data.
/// </summary>
public class XlsxCommentsPartOperations : IXlsxCommentsPartOperations
{
    /// <summary>
    /// Initializes a new xlsx comments part operations instance.
    /// </summary>
    public XlsxCommentsPartOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxCommentsPart"/>.GetComment through the GetComment operation.
    /// </summary>
    /// <param name="data">The xlsx comments part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxComment? GetComment(XlsxCommentsPart data, string reference)
    {
        return data.GetComment(reference);
    }

    /// <summary>
    /// Invokes <see cref="XlsxCommentsPart"/>.ApplyLegacyDrawing through the ApplyLegacyDrawing operation.
    /// </summary>
    /// <param name="data">The xlsx comments part data to operate on.</param>
    /// <param name="drawingPart">The drawing part value used by the operation.</param>
    public void ApplyLegacyDrawing(XlsxCommentsPart data, XlsxLegacyCommentDrawingPart drawingPart)
    {
        data.ApplyLegacyDrawing(drawingPart);
    }
}
