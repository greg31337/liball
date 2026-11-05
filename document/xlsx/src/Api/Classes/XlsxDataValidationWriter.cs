using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes data validation data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxDataValidationWriter : IXlsxDataValidationWriter
{
    /// <summary>
    /// Initializes a new data validation writer with its default state.
    /// </summary>
    public XlsxDataValidationWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "validations">The validations value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxDataValidationWriteResult WriteString(XlsxDataValidations validations)
    {
        XlsxDataValidationWriteResult result = WriteBytes(validations);
        if (result.IsValid)
        {
            result.Xml = Encoding.UTF8.GetString(result.Bytes);
        }

        return result;
    }

    /// <summary>
    /// Validates the input model and returns its serialized UTF-8 bytes.
    /// </summary>
    /// <param name = "validations">The validations value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxDataValidationWriteResult WriteBytes(XlsxDataValidations validations)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxDataValidationWriteResult result = WriteStream(stream, validations);
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
    /// <param name = "validations">The validations value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxDataValidationWriteResult WriteStream(Stream stream, XlsxDataValidations validations)
    {
        XlsxDataValidationWriteResult result = ValidateDataValidations(validations);
        if (!result.IsValid)
        {
            return result;
        }

        if (validations.RawXml != null && validations.RawXml.Length > 0)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(validations.RawXml);
            stream.Write(bytes, 0, bytes.Length);
            return result;
        }

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartDocument();
            WriteDataValidationsElement(writer, validations);
            writer.WriteEndDocument();
        }

        return result;
    }

    internal XlsxDataValidationWriteResult WriteDataValidationString(XlsxDataValidation validation)
    {
        XlsxDataValidationWriteResult result = WriteDataValidationBytes(validation);
        if (result.IsValid)
        {
            result.Xml = Encoding.UTF8.GetString(result.Bytes);
        }

        return result;
    }

    internal XlsxDataValidationWriteResult WriteDataValidationBytes(XlsxDataValidation validation)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxDataValidationWriteResult result = WriteDataValidationStream(stream, validation);
            if (result.IsValid)
            {
                result.Bytes = stream.ToArray();
                result.Xml = Encoding.UTF8.GetString(result.Bytes);
            }

            return result;
        }
    }

    internal XlsxDataValidationWriteResult WriteDataValidationStream(Stream stream, XlsxDataValidation validation)
    {
        XlsxDataValidationWriteResult result = new XlsxDataValidationWriteResult();
        ValidateDataValidation(result, validation, 0);
        if (!result.IsValid)
        {
            return result;
        }

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartDocument();
            WriteDataValidationElement(writer, XlsxNamespaces.Spreadsheet, validation);
            writer.WriteEndDocument();
        }

        return result;
    }

    internal static XlsxDataValidationWriteResult ValidateDataValidations(XlsxDataValidations validations)
    {
        XlsxDataValidationWriteResult result = new XlsxDataValidationWriteResult();
        if (validations == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "dataValidationWriter.validationsNull", "Data validations must not be null."));
            return result;
        }

        if (validations.RawXml != null && validations.RawXml.Length > 0)
        {
            return result;
        }

        if (validations.NamespaceUri == null || validations.NamespaceUri.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "dataValidationWriter.namespaceMissing", "Data validations must have a namespace URI."));
        }
        else if (!XlsxDataValidationReader.IsSpreadsheetNamespace(validations.NamespaceUri))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "dataValidationWriter.namespaceInvalid", "Data validations namespace URI must be a SpreadsheetML namespace."));
        }

        if (validations.Items == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "dataValidationWriter.itemsNull", "Data validation item list must not be null."));
        }
        else
        {
            if (validations.HasCount && validations.Count != (uint)validations.Items.Count)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "dataValidationWriter.countMismatch", "Data validations count attribute must match the number of dataValidation elements."));
            }

            int index = 0;
            foreach (XlsxDataValidation validation in validations.Items)
            {
                ValidateDataValidation(result, validation, index);
                index = index + 1;
            }
        }

        if (validations.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "dataValidationWriter.unknownAttributesNull", "Data validations unknown attribute list must not be null."));
        }

        if (validations.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "dataValidationWriter.unknownXmlNull", "Data validations unknown XML list must not be null."));
        }

        return result;
    }

    internal static void ValidateDataValidation(XlsxDataValidationWriteResult result, XlsxDataValidation validation, int index)
    {
        if (validation == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "dataValidationWriter.itemNull", "Data validation item list must not contain null entries.");
            diagnostic.SetLocation("", "dataValidation", index.ToString(CultureInfo.InvariantCulture));
            result.AddDiagnostic(diagnostic);
            return;
        }

        if (validation.Sqref == null || validation.Sqref.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "dataValidationWriter.sqrefMissing", "Data validation must have a sqref attribute.");
            diagnostic.SetLocation("", "dataValidation", index.ToString(CultureInfo.InvariantCulture));
            result.AddDiagnostic(diagnostic);
        }
        else
        {
            ValidateSqref(result, validation.Sqref, "dataValidation", "sqref");
        }

        if (validation.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "dataValidationWriter.itemUnknownAttributesNull", "Data validation unknown attribute list must not be null."));
        }

        if (validation.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "dataValidationWriter.itemUnknownXmlNull", "Data validation unknown XML list must not be null."));
        }
    }

    internal static void ValidateSqref(XlsxDataValidationWriteResult result, string sqref, string elementName, string attributeName)
    {
        string[] references = sqref.Split(' ');
        foreach (string reference in references)
        {
            string trimmed = reference.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(trimmed);
            foreach (XlsxDiagnostic diagnostic in range.Validation.Diagnostics)
            {
                diagnostic.SetLocation("", elementName, attributeName);
                result.AddDiagnostic(diagnostic);
            }
        }
    }

    internal static void WriteDataValidationsElement(XmlWriter writer, XlsxDataValidations validations)
    {
        if (validations.RawXml != null && validations.RawXml.Length > 0)
        {
            writer.WriteRaw(validations.RawXml);
            return;
        }

        writer.WriteStartElement("", "dataValidations", validations.NamespaceUri);
        uint count = 0;
        if (validations.Items != null)
        {
            count = (uint)validations.Items.Count;
        }

        if (validations.HasCount)
        {
            count = validations.Count;
        }

        writer.WriteAttributeString("count", XlsxXmlValueWriter.WriteUnsignedInteger(count));
        WriteOptionalBoolAttribute(writer, "disablePrompts", validations.DisablePrompts, validations.HasDisablePrompts);
        WriteOptionalUIntAttribute(writer, "xWindow", validations.XWindow, validations.HasXWindow);
        WriteOptionalUIntAttribute(writer, "yWindow", validations.YWindow, validations.HasYWindow);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, validations.UnknownAttributes);
        if (validations.Items == null)
        {
            writer.WriteEndElement();
            return;
        }

        foreach (XlsxDataValidation validation in validations.Items)
        {
            WriteDataValidationElement(writer, validations.NamespaceUri, validation);
        }

        WriteRawXml(writer, validations.ExtensionListRawXml);
        WriteUnknownXml(writer, validations.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteDataValidationElement(XmlWriter writer, string namespaceUri, XlsxDataValidation validation)
    {
        writer.WriteStartElement("", "dataValidation", namespaceUri);
        WriteOptionalStringAttribute(writer, "type", validation.Type);
        WriteOptionalStringAttribute(writer, "errorStyle", validation.ErrorStyle);
        WriteOptionalStringAttribute(writer, "imeMode", validation.ImeMode);
        WriteOptionalStringAttribute(writer, "operator", validation.Operator);
        WriteOptionalBoolAttribute(writer, "allowBlank", validation.AllowBlank, validation.HasAllowBlank);
        WriteOptionalBoolAttribute(writer, "showDropDown", validation.ShowDropDown, validation.HasShowDropDown);
        WriteOptionalBoolAttribute(writer, "showInputMessage", validation.ShowInputMessage, validation.HasShowInputMessage);
        WriteOptionalBoolAttribute(writer, "showErrorMessage", validation.ShowErrorMessage, validation.HasShowErrorMessage);
        WriteOptionalStringAttribute(writer, "sqref", validation.Sqref);
        WriteOptionalStringAttribute(writer, "promptTitle", validation.PromptTitle);
        WriteOptionalStringAttribute(writer, "prompt", validation.Prompt);
        WriteOptionalStringAttribute(writer, "errorTitle", validation.ErrorTitle);
        WriteOptionalStringAttribute(writer, "error", validation.Error);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, validation.UnknownAttributes);
        WriteFormulaElement(writer, namespaceUri, "formula1", validation.Formula1RawXml, validation.Formula1);
        WriteFormulaElement(writer, namespaceUri, "formula2", validation.Formula2RawXml, validation.Formula2);
        WriteUnknownXml(writer, validation.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteFormulaElement(XmlWriter writer, string namespaceUri, string elementName, string rawXml, string formula)
    {
        if (rawXml != null && rawXml.Length > 0)
        {
            writer.WriteRaw(rawXml);
            return;
        }

        if (formula == null || formula.Length == 0)
        {
            return;
        }

        writer.WriteStartElement("", elementName, namespaceUri);
        writer.WriteString(formula);
        writer.WriteEndElement();
    }

    internal static void WriteOptionalStringAttribute(XmlWriter writer, string name, string value)
    {
        if (value != null && value.Length > 0)
        {
            writer.WriteAttributeString(name, value);
        }
    }

    internal static void WriteOptionalUIntAttribute(XmlWriter writer, string name, uint value, bool hasValue)
    {
        if (hasValue)
        {
            writer.WriteAttributeString(name, XlsxXmlValueWriter.WriteUnsignedInteger(value));
        }
    }

    internal static void WriteOptionalBoolAttribute(XmlWriter writer, string name, bool value, bool hasValue)
    {
        if (hasValue)
        {
            writer.WriteAttributeString(name, XlsxXmlValueWriter.WriteBoolean(value));
        }
    }

    internal static void WriteRawXml(XmlWriter writer, string rawXml)
    {
        if (rawXml != null && rawXml.Length > 0)
        {
            writer.WriteRaw(rawXml);
        }
    }

    internal static void WriteUnknownXml(XmlWriter writer, List<string> unknownXml)
    {
        foreach (string item in unknownXml)
        {
            WriteRawXml(writer, item);
        }
    }
}
