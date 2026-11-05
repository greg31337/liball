using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads data validation data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxDataValidationReader : IXlsxDataValidationReader
{
    /// <summary>
    /// Initializes a new data validation reader with its default state.
    /// </summary>
    public XlsxDataValidationReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The data validations produced by the operation.</returns>
    public XlsxDataValidations ReadString(string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult();
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        XlsxDataValidations validations = ReadBytes(bytes);
        validations.RawXml = xml;
        return validations;
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The data validations produced by the operation.</returns>
    public XlsxDataValidations ReadBytes(byte[] bytes)
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
    /// <returns>The data validations produced by the operation.</returns>
    public XlsxDataValidations ReadStream(Stream stream)
    {
        XlsxDataValidations validations = new XlsxDataValidations();
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
                    if (reader.LocalName == "dataValidations" && IsSpreadsheetNamespace(reader.NamespaceURI))
                    {
                        ReadDataValidationsRoot(validations, reader);
                        continue;
                    }

                    if (reader.LocalName == "dataValidation" && IsSpreadsheetNamespace(reader.NamespaceURI))
                    {
                        validations.NamespaceUri = reader.NamespaceURI;
                        validations.Items.Add(ReadDataValidation(validations, reader));
                        validations.Count = 1;
                        validations.HasCount = true;
                        continue;
                    }

                    validations.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "dataValidations.invalidRoot", "Data validation XML root must be a dataValidations or dataValidation element."));
                    reader.ReadOuterXml();
                    continue;
                }

                validations.UnknownXml.Add(reader.ReadOuterXml());
            }
        }

        if (!sawRoot)
        {
            validations.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "dataValidations.missingRoot", "Data validation XML must contain a dataValidations or dataValidation root element."));
        }

        return validations;
    }

    internal static XlsxDataValidations CreateMissingRootResult()
    {
        XlsxDataValidations validations = new XlsxDataValidations();
        validations.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "dataValidations.missingRoot", "Data validation XML must contain a dataValidations or dataValidation root element."));
        return validations;
    }

    internal static void ReadDataValidationsRoot(XlsxDataValidations validations, XmlReader reader)
    {
        validations.NamespaceUri = reader.NamespaceURI;
        SetOptionalUInt(validations, reader.GetAttribute("count"), "dataValidations", "count", 1);
        SetOptionalBool(validations, reader.GetAttribute("disablePrompts"), "dataValidations", "disablePrompts", 1);
        SetOptionalUInt(validations, reader.GetAttribute("xWindow"), "dataValidations", "xWindow", 2);
        SetOptionalUInt(validations, reader.GetAttribute("yWindow"), "dataValidations", "yWindow", 3);
        ReadUnknownAttributes(reader, validations.UnknownAttributes, "dataValidations");
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

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsSpreadsheetNamespace(reader.NamespaceURI))
            {
                if (reader.LocalName == "dataValidation")
                {
                    validations.Items.Add(ReadDataValidation(validations, reader));
                    continue;
                }

                if (reader.LocalName == "extLst")
                {
                    validations.ExtensionListRawXml = reader.ReadOuterXml();
                    continue;
                }
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                validations.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }
    }

    internal static XlsxDataValidation ReadDataValidation(XlsxDataValidations validations, XmlReader reader)
    {
        XlsxDataValidation validation = new XlsxDataValidation();
        validation.Type = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("type"));
        validation.ErrorStyle = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("errorStyle"));
        validation.ImeMode = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("imeMode"));
        validation.Operator = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("operator"));
        validation.Sqref = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("sqref"));
        validation.PromptTitle = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("promptTitle"));
        validation.Prompt = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("prompt"));
        validation.ErrorTitle = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("errorTitle"));
        validation.Error = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("error"));
        SetOptionalBool(validations, reader.GetAttribute("allowBlank"), "dataValidation", "allowBlank", validation, 1);
        SetOptionalBool(validations, reader.GetAttribute("showDropDown"), "dataValidation", "showDropDown", validation, 2);
        SetOptionalBool(validations, reader.GetAttribute("showInputMessage"), "dataValidation", "showInputMessage", validation, 3);
        SetOptionalBool(validations, reader.GetAttribute("showErrorMessage"), "dataValidation", "showErrorMessage", validation, 4);
        ValidateSqref(validations, validation.Sqref, "dataValidation", "sqref");
        ReadUnknownAttributes(reader, validation.UnknownAttributes, "dataValidation");
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return validation;
        }

        int depth = reader.Depth;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return validation;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsSpreadsheetNamespace(reader.NamespaceURI))
            {
                if (reader.LocalName == "formula1")
                {
                    validation.Formula1RawXml = reader.ReadOuterXml();
                    validation.Formula1 = ReadFormulaText(validation.Formula1RawXml);
                    continue;
                }

                if (reader.LocalName == "formula2")
                {
                    validation.Formula2RawXml = reader.ReadOuterXml();
                    validation.Formula2 = ReadFormulaText(validation.Formula2RawXml);
                    continue;
                }
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                validation.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }

        return validation;
    }

    internal static string ReadFormulaText(string rawXml)
    {
        if (rawXml == null || rawXml.Length == 0)
        {
            return "";
        }

        using (StringReader stringReader = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        return reader.ReadElementContentAsString();
                    }

                    reader.Read();
                }
            }
        }

        return "";
    }

    internal static void SetOptionalUInt(XlsxDataValidations validations, string? text, string elementName, string attributeName, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalUInt(validations, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        if (target == 1)
        {
            validations.Count = result.UnsignedIntValue;
            validations.HasCount = true;
        }
        else if (target == 2)
        {
            validations.XWindow = result.UnsignedIntValue;
            validations.HasXWindow = true;
        }
        else if (target == 3)
        {
            validations.YWindow = result.UnsignedIntValue;
            validations.HasYWindow = true;
        }
    }

    internal static void SetOptionalBool(XlsxDataValidations validations, string? text, string elementName, string attributeName, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalBool(validations, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        validations.DisablePrompts = result.BooleanValue;
        validations.HasDisablePrompts = true;
    }

    internal static void SetOptionalBool(XlsxDataValidations validations, string? text, string elementName, string attributeName, XlsxDataValidation validation, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalBool(validations, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        if (target == 1)
        {
            validation.AllowBlank = result.BooleanValue;
            validation.HasAllowBlank = true;
        }
        else if (target == 2)
        {
            validation.ShowDropDown = result.BooleanValue;
            validation.HasShowDropDown = true;
        }
        else if (target == 3)
        {
            validation.ShowInputMessage = result.BooleanValue;
            validation.HasShowInputMessage = true;
        }
        else if (target == 4)
        {
            validation.ShowErrorMessage = result.BooleanValue;
            validation.HasShowErrorMessage = true;
        }
    }

    internal static XlsxXmlValueParseResult ParseOptionalUInt(XlsxDataValidations validations, string? text, string elementName, string attributeName)
    {
        XlsxXmlValueParseResult result = new XlsxXmlValueParseResult();
        if (text == null || text.Length == 0)
        {
            return result;
        }

        result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        AddValueDiagnostics(validations, result, elementName, attributeName);
        return result;
    }

    internal static XlsxXmlValueParseResult ParseOptionalBool(XlsxDataValidations validations, string? text, string elementName, string attributeName)
    {
        XlsxXmlValueParseResult result = new XlsxXmlValueParseResult();
        if (text == null || text.Length == 0)
        {
            return result;
        }

        result = XlsxXmlValueParser.ParseBoolean(text);
        AddValueDiagnostics(validations, result, elementName, attributeName);
        return result;
    }

    internal static void AddValueDiagnostics(XlsxDataValidations validations, XlsxXmlValueParseResult valueResult, string elementName, string attributeName)
    {
        foreach (XlsxDiagnostic diagnostic in valueResult.Validation.Diagnostics)
        {
            diagnostic.SetLocation("", elementName, attributeName);
            validations.AddDiagnostic(diagnostic);
        }
    }

    internal static void ValidateSqref(XlsxDataValidations validations, string sqref, string elementName, string attributeName)
    {
        if (sqref == null || sqref.Length == 0)
        {
            return;
        }

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
                validations.AddDiagnostic(diagnostic);
            }
        }
    }

    internal static void ReadUnknownAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes, string elementName)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            bool known = reader.NamespaceURI == "" && IsKnownAttribute(elementName, reader.LocalName);
            if (!known && !namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }

        reader.MoveToElement();
    }

    internal static bool IsKnownAttribute(string elementName, string localName)
    {
        if (elementName == "dataValidations")
        {
            return localName == "count" || localName == "disablePrompts" || localName == "xWindow" || localName == "yWindow";
        }

        if (elementName == "dataValidation")
        {
            return localName == "type" || localName == "errorStyle" || localName == "imeMode" || localName == "operator" || localName == "allowBlank" || localName == "showDropDown" || localName == "showInputMessage" || localName == "showErrorMessage" || localName == "sqref" || localName == "promptTitle" || localName == "prompt" || localName == "errorTitle" || localName == "error";
        }

        return false;
    }

    internal static bool IsSpreadsheetNamespace(string namespaceUri)
    {
        return namespaceUri == XlsxNamespaces.Spreadsheet || namespaceUri == XlsxNamespaces.StrictSpreadsheet;
    }
}
