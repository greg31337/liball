using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML threaded comments part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxThreadedCommentsPart
{
    /// <summary>
    /// Indicates whether the operation completed without error-level diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains diagnostics produced while the object was read, changed, validated, or written.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Stores the normalized absolute package path, beginning with <c>/</c>.
    /// </summary>
    public string PartPath;
    /// <summary>
    /// Stores the namespace uri value used by this model.
    /// </summary>
    public string NamespaceUri;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Contains comments.
    /// </summary>
    public List<XlsxThreadedComment> Comments;
    /// <summary>
    /// Contains unknown root attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownRootAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new threaded comments part with its default state.
    /// </summary>
    public XlsxThreadedCommentsPart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = "/xl/threadedComments/threadedComment1.xml";
        NamespaceUri = "http://schemas.microsoft.com/office/spreadsheetml/2018/threadedcomments";
        RawXml = "";
        Comments = new List<XlsxThreadedComment>();
        UnknownRootAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    /// <summary>
    /// Returns comment from the current model.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <returns>The threaded comment produced by the operation.</returns>
    internal XlsxThreadedComment? GetComment(string reference)
    {
        foreach (XlsxThreadedComment comment in Comments)
        {
            if (comment != null && comment.Reference == reference)
            {
                return comment;
            }
        }

        return null;
    }
}
