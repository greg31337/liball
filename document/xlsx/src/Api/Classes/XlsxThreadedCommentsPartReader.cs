using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads threaded comments part data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxThreadedCommentsPartReader : IXlsxThreadedCommentsPartReader
{
    /// <summary>
    /// Initializes a new threaded comments part reader with its default state.
    /// </summary>
    public XlsxThreadedCommentsPartReader()
    {
    }

    /// <summary>
    /// Identifies the default namespace OOXML value.
    /// </summary>
    internal const string DefaultNamespace = "http://schemas.microsoft.com/office/spreadsheetml/2018/threadedcomments";
    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The threaded comments part produced by the operation.</returns>
    public XlsxThreadedCommentsPart ReadString(string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult();
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        XlsxThreadedCommentsPart part = ReadBytes(bytes);
        part.RawXml = xml;
        return part;
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The threaded comments part produced by the operation.</returns>
    public XlsxThreadedCommentsPart ReadBytes(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return CreateMissingRootResult();
        }

        using (MemoryStream stream = new MemoryStream(bytes))
        {
            return ReadStream(stream);
        }
    }

    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <returns>The threaded comments part produced by the operation.</returns>
    public XlsxThreadedCommentsPart ReadStream(Stream stream)
    {
        XlsxThreadedCommentsPart part = new XlsxThreadedCommentsPart();
        bool sawRoot = false;
        using (XmlReader reader = XmlReader.Create(stream, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (!reader.EOF)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    reader.Read();
                    continue;
                }

                if (!sawRoot)
                {
                    sawRoot = true;
                    ReadRootElement(part, reader);
                    reader.Read();
                    continue;
                }

                if (reader.Depth == 1 && reader.LocalName == "threadedComment" && IsThreadedCommentsNamespace(part, reader.NamespaceURI))
                {
                    string rawXml = reader.ReadOuterXml();
                    part.Comments.Add(ReadComment(part, rawXml));
                    continue;
                }

                if (reader.Depth > 0)
                {
                    part.UnknownXml.Add(reader.ReadOuterXml());
                    continue;
                }

                reader.Read();
            }
        }

        if (!sawRoot)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "threadedComments.missingRoot", "Threaded comments XML must contain a ThreadedComments root element."));
        }

        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static XlsxThreadedCommentsPart CreateMissingRootResult()
    {
        XlsxThreadedCommentsPart part = new XlsxThreadedCommentsPart();
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "threadedComments.missingRoot", "Threaded comments XML must contain a ThreadedComments root element."));
        return part;
    }

    internal static void ReadRootElement(XlsxThreadedCommentsPart part, XmlReader reader)
    {
        if (reader.LocalName != "ThreadedComments" || !IsThreadedCommentsNamespace(part, reader.NamespaceURI))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "threadedComments.invalidRoot", "Threaded comments XML root must be a ThreadedComments element in a threaded-comments namespace.");
            diagnostic.SetLocation(part.PartPath, reader.LocalName, "");
            part.AddDiagnostic(diagnostic);
        }

        if (reader.NamespaceURI != null && reader.NamespaceURI.Length > 0)
        {
            part.NamespaceUri = reader.NamespaceURI;
        }

        XlsxCorePropertiesReader.ReadUnknownRootAttributes(reader, part.UnknownRootAttributes);
    }

    internal static XlsxThreadedComment ReadComment(XlsxThreadedCommentsPart part, string rawXml)
    {
        XlsxThreadedComment comment = new XlsxThreadedComment();
        comment.RawXml = rawXml;
        byte[] bytes = Encoding.UTF8.GetBytes(rawXml);
        using (MemoryStream stream = new MemoryStream(bytes))
        {
            using (XmlReader reader = XmlReader.Create(stream, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType != XmlNodeType.Element)
                    {
                        reader.Read();
                        continue;
                    }

                    if (reader.LocalName == "threadedComment" && IsThreadedCommentsNamespace(part, reader.NamespaceURI))
                    {
                        ReadCommentElement(part, comment, reader);
                        continue;
                    }

                    reader.Read();
                }
            }
        }

        ValidateCommentReference(part, comment.Reference, "threadedComment");
        return comment;
    }

    internal static void ReadCommentElement(XlsxThreadedCommentsPart part, XlsxThreadedComment comment, XmlReader reader)
    {
        comment.Id = NormalizeAttribute(reader.GetAttribute("id"));
        comment.Reference = NormalizeAttribute(reader.GetAttribute("ref"));
        comment.PersonId = NormalizeAttribute(reader.GetAttribute("personId"));
        comment.ParentId = NormalizeAttribute(reader.GetAttribute("parentId"));
        comment.DateTimeText = NormalizeAttribute(reader.GetAttribute("dT"));
        ReadUnknownCommentAttributes(reader, comment.UnknownAttributes);
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        int depth = reader.Depth;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && reader.LocalName == "text" && IsThreadedCommentsNamespace(part, reader.NamespaceURI))
            {
                comment.TextRawXml = reader.ReadOuterXml();
                comment.Text = ReadThreadedCommentPlainText(comment.TextRawXml);
                continue;
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                comment.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }
    }

    internal static string ReadThreadedCommentPlainText(string xml)
    {
        StringBuilder builder = new StringBuilder();
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        using (MemoryStream stream = new MemoryStream(bytes))
        {
            using (XmlReader reader = XmlReader.Create(stream, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "t")
                    {
                        builder.Append(reader.ReadElementContentAsString());
                        continue;
                    }

                    if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA)
                    {
                        builder.Append(reader.Value);
                    }

                    reader.Read();
                }
            }
        }

        return builder.ToString();
    }

    internal static void ValidateCommentReference(XlsxThreadedCommentsPart part, string reference, string elementName)
    {
        if (reference == null || reference.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "threadedComments.referenceMissing", "Threaded comment must have a ref attribute.");
            diagnostic.SetLocation(part.PartPath, elementName, "ref");
            part.AddDiagnostic(diagnostic);
            return;
        }

        XlsxA1CellReferenceResult result = XlsxA1CellReferenceConverter.Parse(reference);
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            diagnostic.SetLocation(part.PartPath, elementName, "ref");
            part.AddDiagnostic(diagnostic);
        }
    }

    internal static void ReadUnknownCommentAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool known = reader.NamespaceURI == "" && (reader.LocalName == "id" || reader.LocalName == "ref" || reader.LocalName == "personId" || reader.LocalName == "parentId" || reader.LocalName == "dT");
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!known && !namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }

        reader.MoveToElement();
    }

    internal static bool IsThreadedCommentsNamespace(XlsxThreadedCommentsPart part, string namespaceUri)
    {
        return namespaceUri == part.NamespaceUri || namespaceUri == DefaultNamespace;
    }

    internal static string NormalizeAttribute(string? text)
    {
        if (text == null)
        {
            return "";
        }

        return text;
    }
}
