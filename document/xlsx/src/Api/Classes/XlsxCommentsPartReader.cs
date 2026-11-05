using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads comments part data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxCommentsPartReader : IXlsxCommentsPartReader
{
    /// <summary>
    /// Initializes a new comments part reader with its default state.
    /// </summary>
    public XlsxCommentsPartReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The comments part produced by the operation.</returns>
    public XlsxCommentsPart ReadString(string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult();
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        XlsxCommentsPart part = ReadBytes(bytes);
        part.RawXml = xml;
        return part;
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The comments part produced by the operation.</returns>
    public XlsxCommentsPart ReadBytes(byte[] bytes)
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
    /// <returns>The comments part produced by the operation.</returns>
    public XlsxCommentsPart ReadStream(Stream stream)
    {
        XlsxCommentsPart part = new XlsxCommentsPart();
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

                if (reader.Depth == 1 && IsCommentsNamespace(part, reader.NamespaceURI))
                {
                    if (reader.LocalName == "authors")
                    {
                        ReadAuthors(part, reader);
                        continue;
                    }

                    if (reader.LocalName == "commentList")
                    {
                        ReadCommentList(part, reader);
                        continue;
                    }
                }

                part.UnknownXml.Add(reader.ReadOuterXml());
            }
        }

        if (!sawRoot)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "comments.missingRoot", "Comments XML must contain a comments root element."));
        }

        ApplyAuthorNames(part);
        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static XlsxCommentsPart CreateMissingRootResult()
    {
        XlsxCommentsPart part = new XlsxCommentsPart();
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "comments.missingRoot", "Comments XML must contain a comments root element."));
        return part;
    }

    internal static void ReadRootElement(XlsxCommentsPart part, XmlReader reader)
    {
        if (reader.LocalName != "comments" || !IsCommentsNamespace(part, reader.NamespaceURI))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "comments.invalidRoot", "Comments XML root must be a comments element in a SpreadsheetML namespace.");
            diagnostic.SetLocation(part.PartPath, reader.LocalName, "");
            part.AddDiagnostic(diagnostic);
        }

        part.NamespaceUri = reader.NamespaceURI;
        XlsxCorePropertiesReader.ReadUnknownRootAttributes(reader, part.UnknownRootAttributes);
    }

    internal static void ReadAuthors(XlsxCommentsPart part, XmlReader reader)
    {
        int depth = reader.Depth;
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && reader.LocalName == "author" && IsCommentsNamespace(part, reader.NamespaceURI))
            {
                part.Authors.Add(reader.ReadElementContentAsString());
                continue;
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                part.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }
    }

    internal static void ReadCommentList(XlsxCommentsPart part, XmlReader reader)
    {
        int depth = reader.Depth;
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && reader.LocalName == "comment" && IsCommentsNamespace(part, reader.NamespaceURI))
            {
                part.Comments.Add(ReadComment(part, reader));
                continue;
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                part.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }
    }

    internal static XlsxComment ReadComment(XlsxCommentsPart part, XmlReader reader)
    {
        XlsxComment comment = new XlsxComment();
        comment.Reference = reader.GetAttribute("ref");
        SetCommentReferenceDiagnostics(part, comment.Reference);
        SetOptionalUInt(part, reader.GetAttribute("authorId"), "comment", "authorId", comment, 1);
        SetOptionalUInt(part, reader.GetAttribute("shapeId"), "comment", "shapeId", comment, 2);
        ReadUnknownCommentAttributes(reader, comment.UnknownAttributes);
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return comment;
        }

        int depth = reader.Depth;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return comment;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && reader.LocalName == "text" && IsCommentsNamespace(part, reader.NamespaceURI))
            {
                comment.TextRawXml = reader.ReadOuterXml();
                comment.Text = ReadCommentPlainText(comment.TextRawXml, part.NamespaceUri);
                continue;
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                comment.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }

        return comment;
    }

    internal static string ReadCommentPlainText(string xml, string namespaceUri)
    {
        StringBuilder builder = new StringBuilder();
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        using (MemoryStream stream = new MemoryStream(bytes))
        {
            using (XmlReader reader = XmlReader.Create(stream, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "t" && (reader.NamespaceURI == namespaceUri || reader.NamespaceURI == XlsxNamespaces.Spreadsheet || reader.NamespaceURI == XlsxNamespaces.StrictSpreadsheet))
                    {
                        builder.Append(reader.ReadElementContentAsString());
                        continue;
                    }

                    reader.Read();
                }
            }
        }

        return builder.ToString();
    }

    internal static void ApplyAuthorNames(XlsxCommentsPart part)
    {
        foreach (XlsxComment comment in part.Comments)
        {
            if (comment != null && comment.HasAuthorId)
            {
                if (comment.AuthorId < part.Authors.Count)
                {
                    comment.Author = part.Authors[(int)comment.AuthorId];
                }
                else
                {
                    XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "comments.authorIdRange", "Comment authorId is outside the authors list.");
                    diagnostic.SetLocation(part.PartPath, "comment", "authorId");
                    part.AddDiagnostic(diagnostic);
                }
            }
        }
    }

    internal static void SetCommentReferenceDiagnostics(XlsxCommentsPart part, string? reference)
    {
        if (reference == null || reference.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "comments.referenceMissing", "Comment must have a ref attribute.");
            diagnostic.SetLocation(part.PartPath, "comment", "ref");
            part.AddDiagnostic(diagnostic);
            return;
        }

        XlsxA1CellReferenceResult result = XlsxA1CellReferenceConverter.Parse(reference);
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            diagnostic.SetLocation(part.PartPath, "comment", "ref");
            part.AddDiagnostic(diagnostic);
        }
    }

    internal static void SetOptionalUInt(XlsxCommentsPart part, string? text, string elementName, string attributeName, XlsxComment comment, int target)
    {
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            diagnostic.SetLocation(part.PartPath, elementName, attributeName);
            part.AddDiagnostic(diagnostic);
        }

        if (!result.IsValid)
        {
            return;
        }

        if (target == 1)
        {
            comment.AuthorId = result.UnsignedIntValue;
            comment.HasAuthorId = true;
        }
        else if (target == 2)
        {
            comment.ShapeId = result.UnsignedIntValue;
            comment.HasShapeId = true;
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
            bool known = reader.NamespaceURI == "" && (reader.LocalName == "ref" || reader.LocalName == "authorId" || reader.LocalName == "shapeId");
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!known && !namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }

        reader.MoveToElement();
    }

    internal static bool IsCommentsNamespace(XlsxCommentsPart part, string namespaceUri)
    {
        return namespaceUri == part.NamespaceUri || namespaceUri == XlsxNamespaces.Spreadsheet || namespaceUri == XlsxNamespaces.StrictSpreadsheet;
    }
}
