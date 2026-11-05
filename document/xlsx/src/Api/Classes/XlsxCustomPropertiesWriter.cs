using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes custom properties data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxCustomPropertiesWriter : IXlsxCustomPropertiesWriter
{
    /// <summary>
    /// Initializes a new custom properties writer with its default state.
    /// </summary>
    public XlsxCustomPropertiesWriter()
    {
    }

    /// <summary>
    /// Identifies the default format id OOXML value.
    /// </summary>
    internal const string DefaultFormatId = XlsxCustomPropertyDefaults.DefaultFormatId;
    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxCustomPropertiesWriteResult WriteString(XlsxCustomPropertiesPart part)
    {
        XlsxCustomPropertiesWriteResult result = WriteBytes(part);
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
    public XlsxCustomPropertiesWriteResult WriteBytes(XlsxCustomPropertiesPart part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxCustomPropertiesWriteResult result = WriteStream(stream, part);
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
    public XlsxCustomPropertiesWriteResult WriteStream(Stream stream, XlsxCustomPropertiesPart part)
    {
        XlsxCustomPropertiesWriteResult result = ValidateCustomPropertiesPart(part);
        if (!result.IsValid)
        {
            return result;
        }

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("", "Properties", XlsxNamespaces.CustomProperties);
            writer.WriteAttributeString("xmlns", "vt", "http://www.w3.org/2000/xmlns/", XlsxNamespaces.VariantTypes);
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.UnknownRootAttributes);
            foreach (XlsxCustomProperty property in part.Properties)
            {
                WriteProperty(writer, property);
            }

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

    internal static XlsxCustomPropertiesWriteResult ValidateCustomPropertiesPart(XlsxCustomPropertiesPart part)
    {
        XlsxCustomPropertiesWriteResult result = new XlsxCustomPropertiesWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "customProperties.partNull", "Custom properties part must not be null."));
            return result;
        }

        Dictionary<string, bool> names = new Dictionary<string, bool>(System.StringComparer.OrdinalIgnoreCase);
        Dictionary<string, bool> propertyIds = new Dictionary<string, bool>();
        foreach (XlsxCustomProperty property in part.Properties)
        {
            ValidateProperty(result, property, names, propertyIds);
        }

        return result;
    }

    internal static void ValidateProperty(XlsxCustomPropertiesWriteResult result, XlsxCustomProperty property, Dictionary<string, bool> names, Dictionary<string, bool> propertyIds)
    {
        if (property == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "customProperties.propertyNull", "Custom property entry must not be null."));
            return;
        }

        if (property.Name == null || property.Name.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "customProperties.nameMissing", "Custom property must have a name.");
            diagnostic.SetLocation(XlsxPackagePaths.CustomProperties, "property", "name");
            result.AddDiagnostic(diagnostic);
        }
        else if (names.ContainsKey(property.Name))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "customProperties.nameDuplicate", "Custom property names must be unique.");
            diagnostic.SetLocation(XlsxPackagePaths.CustomProperties, "property", "name");
            result.AddDiagnostic(diagnostic);
        }
        else
        {
            names.Add(property.Name, true);
        }

        if (property.PropertyId <= 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "customProperties.pidMissing", "Custom property must have a positive pid.");
            diagnostic.SetLocation(XlsxPackagePaths.CustomProperties, "property", "pid");
            result.AddDiagnostic(diagnostic);
        }
        else
        {
            string pid = property.PropertyId.ToString(CultureInfo.InvariantCulture);
            if (propertyIds.ContainsKey(pid))
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "customProperties.pidDuplicate", "Custom property pids must be unique.");
                diagnostic.SetLocation(XlsxPackagePaths.CustomProperties, "property", "pid");
                result.AddDiagnostic(diagnostic);
            }
            else
            {
                propertyIds.Add(pid, true);
            }
        }

        if (!HasWritableValue(property))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "customProperties.valueMissing", "Custom property must have a writable value.");
            diagnostic.SetLocation(XlsxPackagePaths.CustomProperties, "property", "");
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static bool HasWritableValue(XlsxCustomProperty property)
    {
        if (property.HasRawValueXml || property.HasTextValue || property.HasIntValue || property.HasDoubleValue || property.HasBoolValue || property.HasDateTimeValue)
        {
            return true;
        }

        return false;
    }

    internal static void WriteProperty(XmlWriter writer, XlsxCustomProperty property)
    {
        writer.WriteStartElement("", "property", XlsxNamespaces.CustomProperties);
        string? formatId = property.FormatId;
        if (formatId == null || formatId.Length == 0)
        {
            formatId = DefaultFormatId;
        }

        writer.WriteAttributeString("fmtid", formatId);
        writer.WriteAttributeString("pid", property.PropertyId.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("name", property.Name);
        if (property.LinkTarget != null && property.LinkTarget.Length > 0)
        {
            writer.WriteAttributeString("linkTarget", property.LinkTarget);
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, property.UnknownAttributes);
        WritePropertyValue(writer, property);
        foreach (string unknownXml in property.UnknownXml)
        {
            if (unknownXml != null && unknownXml.Length > 0)
            {
                writer.WriteRaw(unknownXml);
            }
        }

        writer.WriteEndElement();
    }

    internal static void WritePropertyValue(XmlWriter writer, XlsxCustomProperty property)
    {
        if (property.HasRawValueXml)
        {
            writer.WriteRaw(property.RawValueXml);
            return;
        }

        if (property.HasTextValue)
        {
            string valueType = property.ValueType;
            if (valueType == null || valueType.Length == 0)
            {
                valueType = "lpwstr";
            }

            WriteVariantText(writer, valueType, property.TextValue);
        }
        else if (property.HasIntValue)
        {
            string valueType = property.ValueType;
            if (valueType == null || valueType.Length == 0)
            {
                valueType = "i4";
            }

            WriteVariantText(writer, valueType, property.IntValue.ToString(CultureInfo.InvariantCulture));
        }
        else if (property.HasDoubleValue)
        {
            string valueType = property.ValueType;
            if (valueType == null || valueType.Length == 0)
            {
                valueType = "r8";
            }

            WriteVariantText(writer, valueType, XlsxXmlValueWriter.WriteDouble(property.DoubleValue));
        }
        else if (property.HasBoolValue)
        {
            WriteVariantText(writer, "bool", XlsxXmlValueWriter.WriteBoolean(property.BoolValue));
        }
        else if (property.HasDateTimeValue)
        {
            string valueType = property.ValueType;
            if (valueType == null || valueType.Length == 0)
            {
                valueType = "filetime";
            }

            WriteVariantText(writer, valueType, XlsxXmlValueWriter.WriteDateTime(property.DateTimeValue));
        }
    }

    internal static void WriteVariantText(XmlWriter writer, string localName, string value)
    {
        writer.WriteStartElement("vt", localName, XlsxNamespaces.VariantTypes);
        if (value != null && value.Length > 0)
        {
            writer.WriteString(value);
        }

        writer.WriteEndElement();
    }
}
