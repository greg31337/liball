using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes comments part data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxCommentsPartWriter : IXlsxCommentsPartWriter
{
    /// <summary>
    /// Initializes a new comments part writer with its default state.
    /// </summary>
    public XlsxCommentsPartWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxCommentsPartWriteResult WriteString(XlsxCommentsPart part)
    {
        XlsxCommentsPartWriteResult result = WriteBytes(part);
        if (result.IsValid)
        {
            result.Xml = Encoding.UTF8.GetString(result.Bytes);
        }

        return result;
    }

    /// <summary>
    /// Validates the input model and returns its serialized UTF-8 bytes.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxCommentsPartWriteResult WriteBytes(XlsxCommentsPart? part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxCommentsPartWriteResult result = WriteStream(stream, part);
            if (result.IsValid)
            {
                result.Bytes = stream.ToArray();
                result.Xml = Encoding.UTF8.GetString(result.Bytes);
            }

            return result;
        }
    }

    /// <summary>
    /// Validates the input model and writes its serialized representation to the supplied stream.
    /// </summary>
    /// <param name = "stream">The writable output stream. The caller retains ownership unless a returned streaming session is explicitly closed.</param>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxCommentsPartWriteResult WriteStream(Stream stream, XlsxCommentsPart? part)
    {
        XlsxCommentsPartWriteResult result = ValidateCommentsPart(part);
        if (!result.IsValid || part == null)
        {
            return result;
        }

        if (part.RawXml != null && part.RawXml.Length > 0)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(part.RawXml);
            stream.Write(bytes, 0, bytes.Length);
            return result;
        }

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("", "comments", part.NamespaceUri);
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.UnknownRootAttributes);
            WriteAuthors(writer, part);
            WriteCommentList(writer, part);
            WriteUnknownXml(writer, part.UnknownXml);
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return result;
    }

    internal static XlsxCommentsPartWriteResult ValidateCommentsPart(XlsxCommentsPart? part)
    {
        XlsxCommentsPartWriteResult result = new XlsxCommentsPartWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "commentsWriter.partNull", "Comments part must not be null."));
            return result;
        }

        if (part.RawXml != null && part.RawXml.Length > 0)
        {
            return result;
        }

        if (part.NamespaceUri == null || part.NamespaceUri.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "commentsWriter.namespaceMissing", "Comments part must have a namespace URI."));
        }

        if (part.Authors == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "commentsWriter.authorsNull", "Comments authors list must not be null."));
        }

        if (part.Comments == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "commentsWriter.commentsNull", "Comments list must not be null."));
        }

        if (part.UnknownRootAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "commentsWriter.unknownRootAttributesNull", "Comments unknown root attributes list must not be null."));
        }

        if (part.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "commentsWriter.unknownXmlNull", "Comments unknown XML list must not be null."));
        }

        if (part.Authors != null && part.Comments != null)
        {
            ValidateComments(result, part);
        }

        return result;
    }

    internal static void ValidateComments(XlsxCommentsPartWriteResult result, XlsxCommentsPart part)
    {
        int index = 0;
        foreach (XlsxComment comment in part.Comments)
        {
            if (comment == null)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "commentsWriter.commentNull", "Comments list must not contain null entries."));
            }
            else
            {
                ValidateComment(result, part, comment, index);
            }

            index = index + 1;
        }
    }

    internal static void ValidateComment(XlsxCommentsPartWriteResult result, XlsxCommentsPart part, XlsxComment comment, int index)
    {
        if (comment.Reference == null || comment.Reference.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "commentsWriter.referenceMissing", "Comment must have a ref attribute.");
            diagnostic.SetLocation(part.PartPath, "comment", index.ToString(CultureInfo.InvariantCulture));
            result.AddDiagnostic(diagnostic);
        }
        else
        {
            XlsxA1CellReferenceResult reference = XlsxA1CellReferenceConverter.Parse(comment.Reference);
            foreach (XlsxDiagnostic diagnostic in reference.Validation.Diagnostics)
            {
                diagnostic.SetLocation(part.PartPath, "comment", "ref");
                result.AddDiagnostic(diagnostic);
            }
        }

        if (comment.HasAuthorId && comment.AuthorId >= part.Authors.Count)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "commentsWriter.authorIdRange", "Comment authorId is outside the authors list.");
            diagnostic.SetLocation(part.PartPath, "comment", "authorId");
            result.AddDiagnostic(diagnostic);
        }

        if (comment.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "commentsWriter.unknownAttributesNull", "Comment unknown attributes list must not be null."));
        }

        if (comment.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "commentsWriter.commentUnknownXmlNull", "Comment unknown XML list must not be null."));
        }
    }

    internal static void WriteAuthors(XmlWriter writer, XlsxCommentsPart part)
    {
        writer.WriteStartElement("", "authors", part.NamespaceUri);
        foreach (string author in part.Authors)
        {
            writer.WriteStartElement("", "author", part.NamespaceUri);
            writer.WriteString(NormalizeText(author));
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    internal static void WriteCommentList(XmlWriter writer, XlsxCommentsPart part)
    {
        writer.WriteStartElement("", "commentList", part.NamespaceUri);
        foreach (XlsxComment comment in part.Comments)
        {
            WriteComment(writer, part, comment);
        }

        writer.WriteEndElement();
    }

    internal static void WriteComment(XmlWriter writer, XlsxCommentsPart part, XlsxComment comment)
    {
        writer.WriteStartElement("", "comment", part.NamespaceUri);
        writer.WriteAttributeString("ref", comment.Reference);
        if (comment.HasAuthorId)
        {
            writer.WriteAttributeString("authorId", XlsxXmlValueWriter.WriteUnsignedInteger(comment.AuthorId));
        }

        if (comment.HasShapeId)
        {
            writer.WriteAttributeString("shapeId", XlsxXmlValueWriter.WriteUnsignedInteger(comment.ShapeId));
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, comment.UnknownAttributes);
        if (comment.TextRawXml != null && comment.TextRawXml.Length > 0)
        {
            writer.WriteRaw(comment.TextRawXml);
        }
        else
        {
            writer.WriteStartElement("", "text", part.NamespaceUri);
            writer.WriteStartElement("", "t", part.NamespaceUri);
            writer.WriteString(NormalizeText(comment.Text));
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        WriteUnknownXml(writer, comment.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteUnknownXml(XmlWriter writer, List<string> unknownXml)
    {
        if (unknownXml == null)
        {
            return;
        }

        foreach (string item in unknownXml)
        {
            if (item != null && item.Length > 0)
            {
                writer.WriteRaw(item);
            }
        }
    }

    internal static string NormalizeText(string text)
    {
        if (text == null)
        {
            return "";
        }

        return text;
    }
}
