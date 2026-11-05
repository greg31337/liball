using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads core properties data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxCorePropertiesReader : IXlsxCorePropertiesReader
{
    /// <summary>
    /// Initializes a new core properties reader with its default state.
    /// </summary>
    public XlsxCorePropertiesReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The core properties part produced by the operation.</returns>
    public XlsxCorePropertiesPart ReadString(string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult();
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        return ReadBytes(bytes);
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The core properties part produced by the operation.</returns>
    public XlsxCorePropertiesPart ReadBytes(byte[] bytes)
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
    /// <returns>The core properties part produced by the operation.</returns>
    public XlsxCorePropertiesPart ReadStream(Stream stream)
    {
        XlsxCorePropertiesPart part = new XlsxCorePropertiesPart();
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

                if (reader.Depth == 1 && IsKnownCorePropertyElement(reader))
                {
                    ReadCorePropertyElement(part, reader);
                    continue;
                }

                part.UnknownXml.Add(reader.ReadOuterXml());
            }
        }

        if (!sawRoot)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "coreProperties.missingRoot", "Core properties XML must contain a coreProperties root element."));
        }

        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static XlsxCorePropertiesPart CreateMissingRootResult()
    {
        XlsxCorePropertiesPart part = new XlsxCorePropertiesPart();
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "coreProperties.missingRoot", "Core properties XML must contain a coreProperties root element."));
        return part;
    }

    internal static void ReadRootElement(XlsxCorePropertiesPart part, XmlReader reader)
    {
        if (reader.LocalName != "coreProperties" || reader.NamespaceURI != XlsxNamespaces.CoreProperties)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "coreProperties.invalidRoot", "Core properties XML root must be a coreProperties element in the package core properties namespace.");
            diagnostic.SetLocation(XlsxPackagePaths.CoreProperties, reader.LocalName, "");
            part.AddDiagnostic(diagnostic);
        }

        ReadUnknownRootAttributes(reader, part.UnknownRootAttributes);
    }

    internal static void ReadCorePropertyElement(XlsxCorePropertiesPart part, XmlReader reader)
    {
        if (reader.NamespaceURI == XlsxNamespaces.DublinCore)
        {
            ReadDublinCoreElement(part, reader);
            return;
        }

        if (reader.NamespaceURI == XlsxNamespaces.CoreProperties)
        {
            ReadPackageCoreElement(part, reader);
            return;
        }

        if (reader.NamespaceURI == XlsxNamespaces.DublinCoreTerms)
        {
            ReadDublinCoreTermsElement(part, reader);
            return;
        }

        part.UnknownXml.Add(reader.ReadOuterXml());
    }

    internal static void ReadDublinCoreElement(XlsxCorePropertiesPart part, XmlReader reader)
    {
        string localName = reader.LocalName;
        string value = reader.ReadElementContentAsString();
        if (localName == "title")
        {
            part.Title = value;
        }
        else if (localName == "subject")
        {
            part.Subject = value;
        }
        else if (localName == "creator")
        {
            part.Creator = value;
        }
        else if (localName == "description")
        {
            part.Description = value;
        }
        else if (localName == "language")
        {
            part.Language = value;
        }
        else if (localName == "identifier")
        {
            part.Identifier = value;
        }
    }

    internal static void ReadPackageCoreElement(XlsxCorePropertiesPart part, XmlReader reader)
    {
        string localName = reader.LocalName;
        string value = reader.ReadElementContentAsString();
        if (localName == "keywords")
        {
            part.Keywords = value;
        }
        else if (localName == "lastModifiedBy")
        {
            part.LastModifiedBy = value;
        }
        else if (localName == "revision")
        {
            part.Revision = value;
        }
        else if (localName == "category")
        {
            part.Category = value;
        }
        else if (localName == "contentStatus")
        {
            part.ContentStatus = value;
        }
        else if (localName == "version")
        {
            part.Version = value;
        }
    }

    internal static void ReadDublinCoreTermsElement(XlsxCorePropertiesPart part, XmlReader reader)
    {
        string localName = reader.LocalName;
        string value = reader.ReadElementContentAsString();
        XlsxXmlValueParseResult dateResult = XlsxXmlValueParser.ParseDateTime(value);
        if (!dateResult.IsValid)
        {
            foreach (XlsxDiagnostic diagnostic in dateResult.Validation.Diagnostics)
            {
                diagnostic.SetLocation(XlsxPackagePaths.CoreProperties, localName, "");
                part.AddDiagnostic(diagnostic);
            }

            return;
        }

        if (localName == "created")
        {
            part.Created = dateResult.DateTimeValue;
            part.HasCreated = true;
        }
        else if (localName == "modified")
        {
            part.Modified = dateResult.DateTimeValue;
            part.HasModified = true;
        }
    }

    internal static bool IsKnownCorePropertyElement(XmlReader reader)
    {
        if (reader.NamespaceURI == XlsxNamespaces.DublinCore)
        {
            if (reader.LocalName == "title" || reader.LocalName == "subject" || reader.LocalName == "creator" || reader.LocalName == "description" || reader.LocalName == "language" || reader.LocalName == "identifier")
            {
                return true;
            }
        }

        if (reader.NamespaceURI == XlsxNamespaces.CoreProperties)
        {
            if (reader.LocalName == "keywords" || reader.LocalName == "lastModifiedBy" || reader.LocalName == "revision" || reader.LocalName == "category" || reader.LocalName == "contentStatus" || reader.LocalName == "version")
            {
                return true;
            }
        }

        if (reader.NamespaceURI == XlsxNamespaces.DublinCoreTerms)
        {
            if (reader.LocalName == "created" || reader.LocalName == "modified")
            {
                return true;
            }
        }

        return false;
    }

    internal static void ReadUnknownRootAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }

        reader.MoveToElement();
    }
}
