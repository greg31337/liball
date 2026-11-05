using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads custom properties data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxCustomPropertiesReader : IXlsxCustomPropertiesReader
{
    /// <summary>
    /// Initializes a new custom properties reader with its default state.
    /// </summary>
    public XlsxCustomPropertiesReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The custom properties part produced by the operation.</returns>
    public XlsxCustomPropertiesPart ReadString(string xml)
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
    /// <returns>The custom properties part produced by the operation.</returns>
    public XlsxCustomPropertiesPart ReadBytes(byte[] bytes)
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
    /// <returns>The custom properties part produced by the operation.</returns>
    public XlsxCustomPropertiesPart ReadStream(Stream stream)
    {
        XlsxCustomPropertiesPart part = new XlsxCustomPropertiesPart();
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

                if (reader.Depth == 1 && reader.NamespaceURI == XlsxNamespaces.CustomProperties && reader.LocalName == "property")
                {
                    ReadProperty(part, reader);
                    continue;
                }

                part.UnknownXml.Add(reader.ReadOuterXml());
            }
        }

        if (!sawRoot)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "customProperties.missingRoot", "Custom properties XML must contain a Properties root element."));
        }

        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static XlsxCustomPropertiesPart CreateMissingRootResult()
    {
        XlsxCustomPropertiesPart part = new XlsxCustomPropertiesPart();
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "customProperties.missingRoot", "Custom properties XML must contain a Properties root element."));
        return part;
    }

    internal static void ReadRootElement(XlsxCustomPropertiesPart part, XmlReader reader)
    {
        if (reader.LocalName != "Properties" || reader.NamespaceURI != XlsxNamespaces.CustomProperties)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "customProperties.invalidRoot", "Custom properties XML root must be a Properties element in the custom properties namespace.");
            diagnostic.SetLocation(XlsxPackagePaths.CustomProperties, reader.LocalName, "");
            part.AddDiagnostic(diagnostic);
        }

        XlsxCorePropertiesReader.ReadUnknownRootAttributes(reader, part.UnknownRootAttributes);
    }

    internal static void ReadProperty(XlsxCustomPropertiesPart part, XmlReader reader)
    {
        XlsxCustomProperty property = new XlsxCustomProperty();
        property.FormatId = reader.GetAttribute("fmtid");
        property.Name = reader.GetAttribute("name");
        property.LinkTarget = reader.GetAttribute("linkTarget");
        ReadUnknownPropertyAttributes(reader, property.UnknownAttributes);
        string? pidText = reader.GetAttribute("pid");
        if (pidText != null && pidText.Length > 0)
        {
            XlsxXmlValueParseResult pidResult = XlsxXmlValueParser.ParseInteger(pidText);
            if (pidResult.IsValid)
            {
                property.PropertyId = pidResult.IntValue;
            }
            else
            {
                AddValueDiagnostics(part, pidResult, "property", "pid");
            }
        }

        if (reader.IsEmptyElement)
        {
            part.Properties.Add(property);
            reader.Read();
            return;
        }

        int propertyDepth = reader.Depth;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == propertyDepth)
            {
                reader.Read();
                part.Properties.Add(property);
                return;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                reader.Read();
                continue;
            }

            if (reader.Depth == propertyDepth + 1 && reader.NamespaceURI == XlsxNamespaces.VariantTypes)
            {
                ReadVariantValue(part, property, reader);
                continue;
            }

            property.UnknownXml.Add(reader.ReadOuterXml());
        }

        part.Properties.Add(property);
    }

    internal static void ReadVariantValue(XlsxCustomPropertiesPart part, XlsxCustomProperty property, XmlReader reader)
    {
        string localName = reader.LocalName;
        property.ValueType = localName;
        if (!IsKnownVariantValue(localName))
        {
            property.RawValueXml = reader.ReadOuterXml();
            property.HasRawValueXml = true;
            return;
        }

        string value = reader.ReadElementContentAsString();
        if (localName == "lpwstr" || localName == "lpstr" || localName == "bstr")
        {
            property.TextValue = value;
            property.HasTextValue = true;
        }
        else if (localName == "i4" || localName == "int")
        {
            XlsxXmlValueParseResult parseResult = XlsxXmlValueParser.ParseInteger(value);
            if (parseResult.IsValid)
            {
                property.IntValue = parseResult.IntValue;
                property.HasIntValue = true;
            }
            else
            {
                AddValueDiagnostics(part, parseResult, localName, "");
            }
        }
        else if (localName == "r8")
        {
            XlsxXmlValueParseResult parseResult = XlsxXmlValueParser.ParseDouble(value);
            if (parseResult.IsValid)
            {
                property.DoubleValue = parseResult.DoubleValue;
                property.HasDoubleValue = true;
            }
            else
            {
                AddValueDiagnostics(part, parseResult, localName, "");
            }
        }
        else if (localName == "bool")
        {
            XlsxXmlValueParseResult parseResult = XlsxXmlValueParser.ParseBoolean(value);
            if (parseResult.IsValid)
            {
                property.BoolValue = parseResult.BooleanValue;
                property.HasBoolValue = true;
            }
            else
            {
                AddValueDiagnostics(part, parseResult, localName, "");
            }
        }
        else if (localName == "filetime" || localName == "date")
        {
            XlsxXmlValueParseResult parseResult = XlsxXmlValueParser.ParseDateTime(value);
            if (parseResult.IsValid)
            {
                property.DateTimeValue = parseResult.DateTimeValue;
                property.HasDateTimeValue = true;
            }
            else
            {
                AddValueDiagnostics(part, parseResult, localName, "");
            }
        }
    }

    internal static void AddValueDiagnostics(XlsxCustomPropertiesPart part, XlsxXmlValueParseResult parseResult, string elementName, string attributeName)
    {
        foreach (XlsxDiagnostic diagnostic in parseResult.Validation.Diagnostics)
        {
            diagnostic.SetLocation(XlsxPackagePaths.CustomProperties, elementName, attributeName);
            part.AddDiagnostic(diagnostic);
        }
    }

    internal static bool IsKnownVariantValue(string localName)
    {
        if (localName == "lpwstr" || localName == "lpstr" || localName == "bstr")
        {
            return true;
        }

        if (localName == "i4" || localName == "int" || localName == "r8" || localName == "bool" || localName == "filetime" || localName == "date")
        {
            return true;
        }

        return false;
    }

    internal static void ReadUnknownPropertyAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool known = reader.NamespaceURI == "" && (reader.LocalName == "fmtid" || reader.LocalName == "pid" || reader.LocalName == "name" || reader.LocalName == "linkTarget");
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!known && !namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }

        reader.MoveToElement();
    }
}
