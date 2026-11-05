using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes extended properties data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxExtendedPropertiesWriter : IXlsxExtendedPropertiesWriter
{
    /// <summary>
    /// Initializes a new extended properties writer with its default state.
    /// </summary>
    public XlsxExtendedPropertiesWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxExtendedPropertiesWriteResult WriteString(XlsxExtendedPropertiesPart part)
    {
        XlsxExtendedPropertiesWriteResult result = WriteBytes(part);
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
    public XlsxExtendedPropertiesWriteResult WriteBytes(XlsxExtendedPropertiesPart part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxExtendedPropertiesWriteResult result = WriteStream(stream, part);
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
    public XlsxExtendedPropertiesWriteResult WriteStream(Stream stream, XlsxExtendedPropertiesPart part)
    {
        XlsxExtendedPropertiesWriteResult result = ValidateExtendedPropertiesPart(part);
        if (!result.IsValid)
        {
            return result;
        }

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("", "Properties", XlsxNamespaces.ExtendedProperties);
            writer.WriteAttributeString("xmlns", "vt", "http://www.w3.org/2000/xmlns/", XlsxNamespaces.VariantTypes);
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.UnknownRootAttributes);
            XlsxCorePropertiesWriter.WriteTextElement(writer, "", XlsxNamespaces.ExtendedProperties, "Template", part.Template);
            XlsxCorePropertiesWriter.WriteTextElement(writer, "", XlsxNamespaces.ExtendedProperties, "Manager", part.Manager);
            XlsxCorePropertiesWriter.WriteTextElement(writer, "", XlsxNamespaces.ExtendedProperties, "Company", part.Company);
            WriteUnsignedElement(writer, "Pages", part.Pages, part.HasPages);
            WriteUnsignedElement(writer, "Words", part.Words, part.HasWords);
            WriteUnsignedElement(writer, "Characters", part.Characters, part.HasCharacters);
            XlsxCorePropertiesWriter.WriteTextElement(writer, "", XlsxNamespaces.ExtendedProperties, "PresentationFormat", part.PresentationFormat);
            WriteUnsignedElement(writer, "Lines", part.Lines, part.HasLines);
            WriteUnsignedElement(writer, "Paragraphs", part.Paragraphs, part.HasParagraphs);
            WriteUnsignedElement(writer, "Slides", part.Slides, part.HasSlides);
            WriteUnsignedElement(writer, "Notes", part.Notes, part.HasNotes);
            WriteUnsignedElement(writer, "TotalTime", part.TotalTime, part.HasTotalTime);
            WriteUnsignedElement(writer, "HiddenSlides", part.HiddenSlides, part.HasHiddenSlides);
            WriteUnsignedElement(writer, "MMClips", part.MMClips, part.HasMMClips);
            WriteBooleanElement(writer, "ScaleCrop", part.ScaleCrop, part.HasScaleCrop);
            WriteRawElement(writer, part.HeadingPairsRawXml, part.HasHeadingPairs);
            WriteRawElement(writer, part.TitlesOfPartsRawXml, part.HasTitlesOfParts);
            WriteBooleanElement(writer, "LinksUpToDate", part.LinksUpToDate, part.HasLinksUpToDate);
            WriteUnsignedElement(writer, "CharactersWithSpaces", part.CharactersWithSpaces, part.HasCharactersWithSpaces);
            WriteBooleanElement(writer, "SharedDoc", part.SharedDoc, part.HasSharedDoc);
            WriteBooleanElement(writer, "HyperlinksChanged", part.HyperlinksChanged, part.HasHyperlinksChanged);
            XlsxCorePropertiesWriter.WriteTextElement(writer, "", XlsxNamespaces.ExtendedProperties, "Application", part.Application);
            XlsxCorePropertiesWriter.WriteTextElement(writer, "", XlsxNamespaces.ExtendedProperties, "AppVersion", part.AppVersion);
            WriteIntegerElement(writer, "DocSecurity", part.DocSecurity, part.HasDocSecurity);
            foreach (string unknownXml in part.UnknownXml)
            {
                if (unknownXml != null && unknownXml.Length > 0)
                {
                    writer.WriteRaw(unknownXml);
                }
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return result;
    }

    internal static XlsxExtendedPropertiesWriteResult ValidateExtendedPropertiesPart(XlsxExtendedPropertiesPart part)
    {
        XlsxExtendedPropertiesWriteResult result = new XlsxExtendedPropertiesWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "extendedProperties.partNull", "Extended properties part must not be null."));
            return result;
        }

        return result;
    }

    internal static void WriteUnsignedElement(XmlWriter writer, string localName, uint value, bool hasValue)
    {
        if (!hasValue)
        {
            return;
        }

        writer.WriteStartElement("", localName, XlsxNamespaces.ExtendedProperties);
        writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
        writer.WriteEndElement();
    }

    internal static void WriteIntegerElement(XmlWriter writer, string localName, int value, bool hasValue)
    {
        if (!hasValue)
        {
            return;
        }

        writer.WriteStartElement("", localName, XlsxNamespaces.ExtendedProperties);
        writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
        writer.WriteEndElement();
    }

    internal static void WriteBooleanElement(XmlWriter writer, string localName, bool value, bool hasValue)
    {
        if (!hasValue)
        {
            return;
        }

        writer.WriteStartElement("", localName, XlsxNamespaces.ExtendedProperties);
        writer.WriteString(XlsxXmlValueWriter.WriteBoolean(value));
        writer.WriteEndElement();
    }

    internal static void WriteRawElement(XmlWriter writer, string rawXml, bool hasValue)
    {
        if (!hasValue || rawXml == null || rawXml.Length == 0)
        {
            return;
        }

        writer.WriteRaw(rawXml);
    }
}
