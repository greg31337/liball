using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads extended properties data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxExtendedPropertiesReader : IXlsxExtendedPropertiesReader
{
    /// <summary>
    /// Initializes a new extended properties reader with its default state.
    /// </summary>
    public XlsxExtendedPropertiesReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The extended properties part produced by the operation.</returns>
    public XlsxExtendedPropertiesPart ReadString(string xml)
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
    /// <returns>The extended properties part produced by the operation.</returns>
    public XlsxExtendedPropertiesPart ReadBytes(byte[] bytes)
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
    /// <returns>The extended properties part produced by the operation.</returns>
    public XlsxExtendedPropertiesPart ReadStream(Stream stream)
    {
        XlsxExtendedPropertiesPart part = new XlsxExtendedPropertiesPart();
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

                if (reader.Depth == 1 && reader.NamespaceURI == XlsxNamespaces.ExtendedProperties && IsKnownExtendedPropertyElement(reader.LocalName))
                {
                    ReadExtendedPropertyElement(part, reader);
                    continue;
                }

                part.UnknownXml.Add(reader.ReadOuterXml());
            }
        }

        if (!sawRoot)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "extendedProperties.missingRoot", "Extended properties XML must contain a Properties root element."));
        }

        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static XlsxExtendedPropertiesPart CreateMissingRootResult()
    {
        XlsxExtendedPropertiesPart part = new XlsxExtendedPropertiesPart();
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "extendedProperties.missingRoot", "Extended properties XML must contain a Properties root element."));
        return part;
    }

    internal static void ReadRootElement(XlsxExtendedPropertiesPart part, XmlReader reader)
    {
        if (reader.LocalName != "Properties" || reader.NamespaceURI != XlsxNamespaces.ExtendedProperties)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "extendedProperties.invalidRoot", "Extended properties XML root must be a Properties element in the extended properties namespace.");
            diagnostic.SetLocation(XlsxPackagePaths.ExtendedProperties, reader.LocalName, "");
            part.AddDiagnostic(diagnostic);
        }

        XlsxCorePropertiesReader.ReadUnknownRootAttributes(reader, part.UnknownRootAttributes);
    }

    internal static void ReadExtendedPropertyElement(XlsxExtendedPropertiesPart part, XmlReader reader)
    {
        string localName = reader.LocalName;
        if (localName == "HeadingPairs")
        {
            part.HeadingPairsRawXml = reader.ReadOuterXml();
            part.HasHeadingPairs = true;
            return;
        }

        if (localName == "TitlesOfParts")
        {
            part.TitlesOfPartsRawXml = reader.ReadOuterXml();
            part.HasTitlesOfParts = true;
            return;
        }

        string value = reader.ReadElementContentAsString();
        if (localName == "Template")
        {
            part.Template = value;
        }
        else if (localName == "Manager")
        {
            part.Manager = value;
        }
        else if (localName == "Company")
        {
            part.Company = value;
        }
        else if (localName == "PresentationFormat")
        {
            part.PresentationFormat = value;
        }
        else if (localName == "Application")
        {
            part.Application = value;
        }
        else if (localName == "AppVersion")
        {
            part.AppVersion = value;
        }
        else if (localName == "DocSecurity")
        {
            SetIntegerProperty(part, localName, value, 1);
        }
        else if (IsUnsignedIntegerProperty(localName))
        {
            SetUnsignedIntegerProperty(part, localName, value);
        }
        else if (IsBooleanProperty(localName))
        {
            SetBooleanProperty(part, localName, value);
        }
    }

    internal static void SetIntegerProperty(XlsxExtendedPropertiesPart part, string localName, string value, int target)
    {
        XlsxXmlValueParseResult parseResult = XlsxXmlValueParser.ParseInteger(value);
        if (!parseResult.IsValid)
        {
            AddValueDiagnostics(part, parseResult, localName);
            return;
        }

        if (target == 1)
        {
            part.DocSecurity = parseResult.IntValue;
            part.HasDocSecurity = true;
        }
    }

    internal static void SetUnsignedIntegerProperty(XlsxExtendedPropertiesPart part, string localName, string value)
    {
        XlsxXmlValueParseResult parseResult = XlsxXmlValueParser.ParseUnsignedInteger(value);
        if (!parseResult.IsValid)
        {
            AddValueDiagnostics(part, parseResult, localName);
            return;
        }

        uint number = parseResult.UnsignedIntValue;
        if (localName == "Pages")
        {
            part.Pages = number;
            part.HasPages = true;
        }
        else if (localName == "Words")
        {
            part.Words = number;
            part.HasWords = true;
        }
        else if (localName == "Characters")
        {
            part.Characters = number;
            part.HasCharacters = true;
        }
        else if (localName == "Lines")
        {
            part.Lines = number;
            part.HasLines = true;
        }
        else if (localName == "Paragraphs")
        {
            part.Paragraphs = number;
            part.HasParagraphs = true;
        }
        else if (localName == "Slides")
        {
            part.Slides = number;
            part.HasSlides = true;
        }
        else if (localName == "Notes")
        {
            part.Notes = number;
            part.HasNotes = true;
        }
        else if (localName == "TotalTime")
        {
            part.TotalTime = number;
            part.HasTotalTime = true;
        }
        else if (localName == "HiddenSlides")
        {
            part.HiddenSlides = number;
            part.HasHiddenSlides = true;
        }
        else if (localName == "MMClips")
        {
            part.MMClips = number;
            part.HasMMClips = true;
        }
        else if (localName == "CharactersWithSpaces")
        {
            part.CharactersWithSpaces = number;
            part.HasCharactersWithSpaces = true;
        }
    }

    internal static void SetBooleanProperty(XlsxExtendedPropertiesPart part, string localName, string value)
    {
        XlsxXmlValueParseResult parseResult = XlsxXmlValueParser.ParseBoolean(value);
        if (!parseResult.IsValid)
        {
            AddValueDiagnostics(part, parseResult, localName);
            return;
        }

        if (localName == "ScaleCrop")
        {
            part.ScaleCrop = parseResult.BooleanValue;
            part.HasScaleCrop = true;
        }
        else if (localName == "LinksUpToDate")
        {
            part.LinksUpToDate = parseResult.BooleanValue;
            part.HasLinksUpToDate = true;
        }
        else if (localName == "SharedDoc")
        {
            part.SharedDoc = parseResult.BooleanValue;
            part.HasSharedDoc = true;
        }
        else if (localName == "HyperlinksChanged")
        {
            part.HyperlinksChanged = parseResult.BooleanValue;
            part.HasHyperlinksChanged = true;
        }
    }

    internal static void AddValueDiagnostics(XlsxExtendedPropertiesPart part, XlsxXmlValueParseResult parseResult, string localName)
    {
        foreach (XlsxDiagnostic diagnostic in parseResult.Validation.Diagnostics)
        {
            diagnostic.SetLocation(XlsxPackagePaths.ExtendedProperties, localName, "");
            part.AddDiagnostic(diagnostic);
        }
    }

    internal static bool IsKnownExtendedPropertyElement(string localName)
    {
        if (localName == "Template" || localName == "Manager" || localName == "Company" || localName == "PresentationFormat" || localName == "Application" || localName == "AppVersion")
        {
            return true;
        }

        if (localName == "DocSecurity" || IsUnsignedIntegerProperty(localName) || IsBooleanProperty(localName))
        {
            return true;
        }

        if (localName == "HeadingPairs" || localName == "TitlesOfParts")
        {
            return true;
        }

        return false;
    }

    internal static bool IsUnsignedIntegerProperty(string localName)
    {
        if (localName == "Pages" || localName == "Words" || localName == "Characters" || localName == "Lines" || localName == "Paragraphs" || localName == "Slides")
        {
            return true;
        }

        if (localName == "Notes" || localName == "TotalTime" || localName == "HiddenSlides" || localName == "MMClips" || localName == "CharactersWithSpaces")
        {
            return true;
        }

        return false;
    }

    internal static bool IsBooleanProperty(string localName)
    {
        if (localName == "ScaleCrop" || localName == "LinksUpToDate" || localName == "SharedDoc" || localName == "HyperlinksChanged")
        {
            return true;
        }

        return false;
    }
}
