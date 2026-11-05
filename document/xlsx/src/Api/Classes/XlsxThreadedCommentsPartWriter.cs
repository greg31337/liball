using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes threaded comments part data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxThreadedCommentsPartWriter : IXlsxThreadedCommentsPartWriter
{
    /// <summary>
    /// Initializes a new threaded comments part writer with its default state.
    /// </summary>
    public XlsxThreadedCommentsPartWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxThreadedCommentsPartWriteResult WriteString(XlsxThreadedCommentsPart part)
    {
        XlsxThreadedCommentsPartWriteResult result = WriteBytes(part);
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
    public XlsxThreadedCommentsPartWriteResult WriteBytes(XlsxThreadedCommentsPart? part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxThreadedCommentsPartWriteResult result = WriteStream(stream, part);
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
    public XlsxThreadedCommentsPartWriteResult WriteStream(Stream stream, XlsxThreadedCommentsPart? part)
    {
        XlsxThreadedCommentsPartWriteResult result = ValidateThreadedCommentsPart(part);
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
            writer.WriteStartElement("", "ThreadedComments", part.NamespaceUri);
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.UnknownRootAttributes);
            foreach (XlsxThreadedComment comment in part.Comments)
            {
                WriteComment(writer, part, comment);
            }

            WriteUnknownXml(writer, part.UnknownXml);
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return result;
    }

    internal static XlsxThreadedCommentsPartWriteResult ValidateThreadedCommentsPart(XlsxThreadedCommentsPart? part)
    {
        XlsxThreadedCommentsPartWriteResult result = new XlsxThreadedCommentsPartWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "threadedCommentsWriter.partNull", "Threaded comments part must not be null."));
            return result;
        }

        if (part.RawXml != null && part.RawXml.Length > 0)
        {
            return result;
        }

        if (part.NamespaceUri == null || part.NamespaceUri.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "threadedCommentsWriter.namespaceMissing", "Threaded comments part must have a namespace URI."));
        }

        if (part.Comments == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "threadedCommentsWriter.commentsNull", "Threaded comments list must not be null."));
        }

        if (part.UnknownRootAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "threadedCommentsWriter.unknownRootAttributesNull", "Threaded comments unknown root attributes list must not be null."));
        }

        if (part.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "threadedCommentsWriter.unknownXmlNull", "Threaded comments unknown XML list must not be null."));
        }

        if (part.Comments != null)
        {
            int index = 0;
            foreach (XlsxThreadedComment comment in part.Comments)
            {
                ValidateComment(result, part, comment, index);
                index = index + 1;
            }
        }

        return result;
    }

    internal static void ValidateComment(XlsxThreadedCommentsPartWriteResult result, XlsxThreadedCommentsPart part, XlsxThreadedComment comment, int index)
    {
        if (comment == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "threadedCommentsWriter.commentNull", "Threaded comments list must not contain null entries."));
            return;
        }

        if (comment.RawXml != null && comment.RawXml.Length > 0)
        {
            return;
        }

        if (comment.Reference == null || comment.Reference.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "threadedCommentsWriter.referenceMissing", "Threaded comment must have a ref attribute.");
            diagnostic.SetLocation(part.PartPath, "threadedComment", index.ToString(CultureInfo.InvariantCulture));
            result.AddDiagnostic(diagnostic);
        }
        else
        {
            XlsxA1CellReferenceResult reference = XlsxA1CellReferenceConverter.Parse(comment.Reference);
            foreach (XlsxDiagnostic diagnostic in reference.Validation.Diagnostics)
            {
                diagnostic.SetLocation(part.PartPath, "threadedComment", "ref");
                result.AddDiagnostic(diagnostic);
            }
        }

        if (comment.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "threadedCommentsWriter.unknownAttributesNull", "Threaded comment unknown attributes list must not be null."));
        }

        if (comment.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "threadedCommentsWriter.commentUnknownXmlNull", "Threaded comment unknown XML list must not be null."));
        }
    }

    internal static void WriteComment(XmlWriter writer, XlsxThreadedCommentsPart part, XlsxThreadedComment comment)
    {
        if (comment.RawXml != null && comment.RawXml.Length > 0)
        {
            writer.WriteRaw(comment.RawXml);
            return;
        }

        writer.WriteStartElement("", "threadedComment", part.NamespaceUri);
        WriteOptionalAttribute(writer, "ref", comment.Reference);
        WriteOptionalAttribute(writer, "id", comment.Id);
        WriteOptionalAttribute(writer, "personId", comment.PersonId);
        WriteOptionalAttribute(writer, "parentId", comment.ParentId);
        WriteOptionalAttribute(writer, "dT", comment.DateTimeText);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, comment.UnknownAttributes);
        if (comment.TextRawXml != null && comment.TextRawXml.Length > 0)
        {
            writer.WriteRaw(comment.TextRawXml);
        }
        else
        {
            writer.WriteStartElement("", "text", part.NamespaceUri);
            writer.WriteString(NormalizeText(comment.Text));
            writer.WriteEndElement();
        }

        WriteUnknownXml(writer, comment.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteOptionalAttribute(XmlWriter writer, string name, string value)
    {
        if (value != null && value.Length > 0)
        {
            writer.WriteAttributeString(name, value);
        }
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
