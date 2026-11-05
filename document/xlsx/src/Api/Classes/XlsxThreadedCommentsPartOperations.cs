using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxThreadedCommentsPart"/> data.
/// </summary>
public class XlsxThreadedCommentsPartOperations : IXlsxThreadedCommentsPartOperations
{
    /// <summary>
    /// Initializes a new xlsx threaded comments part operations instance.
    /// </summary>
    public XlsxThreadedCommentsPartOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxThreadedCommentsPart"/>.GetComment through the GetComment operation.
    /// </summary>
    /// <param name="data">The xlsx threaded comments part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxThreadedComment? GetComment(XlsxThreadedCommentsPart data, string reference)
    {
        return data.GetComment(reference);
    }
}
