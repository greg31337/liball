using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads calculation chain part data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxCalculationChainPartReader : IXlsxCalculationChainPartReader
{
    /// <summary>
    /// Initializes a new calculation chain part reader with its default state.
    /// </summary>
    public XlsxCalculationChainPartReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The calculation chain part produced by the operation.</returns>
    public XlsxCalculationChainPart ReadString(string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult();
        }

        return ReadBytes(Encoding.UTF8.GetBytes(xml));
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The calculation chain part produced by the operation.</returns>
    public XlsxCalculationChainPart ReadBytes(byte[] bytes)
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
    /// <returns>The calculation chain part produced by the operation.</returns>
    public XlsxCalculationChainPart ReadStream(Stream stream)
    {
        XlsxCalculationChainPart part = new XlsxCalculationChainPart();
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
                    ReadRoot(part, reader);
                    if (reader.IsEmptyElement)
                    {
                        reader.Read();
                    }
                    else
                    {
                        reader.Read();
                    }

                    continue;
                }

                if (reader.Depth == 1 && IsCalculationChainNamespace(part, reader.NamespaceURI) && reader.LocalName == "c")
                {
                    ReadCell(part, reader);
                    continue;
                }

                part.UnknownXml.Add(reader.ReadOuterXml());
            }
        }

        if (!sawRoot)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "calculationChain.missingRoot", "Calculation chain XML must contain a calcChain root element."));
        }

        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static XlsxCalculationChainPart CreateMissingRootResult()
    {
        XlsxCalculationChainPart part = new XlsxCalculationChainPart();
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "calculationChain.missingRoot", "Calculation chain XML must contain a calcChain root element."));
        return part;
    }

    internal static void ReadRoot(XlsxCalculationChainPart part, XmlReader reader)
    {
        if (reader.LocalName != "calcChain" || !XlsxWorkbookPartReader.IsSpreadsheetNamespace(reader.NamespaceURI))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "calculationChain.invalidRoot", "Calculation chain root must be calcChain in a SpreadsheetML namespace.");
            diagnostic.SetLocation(part.PartPath, reader.LocalName, "");
            part.AddDiagnostic(diagnostic);
        }

        part.NamespaceUri = reader.NamespaceURI;
        ReadUnknownRootAttributes(reader, part.UnknownRootAttributes);
    }

    internal static void ReadCell(XlsxCalculationChainPart part, XmlReader reader)
    {
        XlsxCalculationChainCell cell = new XlsxCalculationChainCell();
        cell.Reference = reader.GetAttribute("r");
        ReadOptionalUnsignedAttribute(part, reader, "i", cell, 1);
        ReadOptionalBooleanAttribute(part, reader, "s", cell, 1);
        ReadOptionalBooleanAttribute(part, reader, "l", cell, 2);
        ReadOptionalBooleanAttribute(part, reader, "t", cell, 3);
        ReadOptionalBooleanAttribute(part, reader, "a", cell, 4);
        ReadUnknownCellAttributes(reader, cell.UnknownAttributes);
        if (cell.Reference == null || cell.Reference.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "calculationChain.cellReferenceMissing", "Calculation chain cell must have an r attribute.");
            diagnostic.SetLocation(part.PartPath, "c", "r");
            part.AddDiagnostic(diagnostic);
        }
        else
        {
            XlsxA1CellReferenceResult reference = XlsxA1CellReferenceConverter.Parse(cell.Reference);
            if (!reference.IsValid)
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "calculationChain.cellReferenceInvalid", "Calculation chain cell r attribute must be a valid A1 cell reference.");
                diagnostic.SetLocation(part.PartPath, "c", "r");
                part.AddDiagnostic(diagnostic);
                AddValueDiagnostics(part, reference.Validation);
            }
        }

        part.Cells.Add(cell);
        reader.ReadOuterXml();
    }

    internal static void ReadOptionalUnsignedAttribute(XlsxCalculationChainPart part, XmlReader reader, string attributeName, XlsxCalculationChainCell cell, int target)
    {
        string? text = reader.GetAttribute(attributeName);
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        if (!result.IsValid)
        {
            AddValueDiagnostics(part, result.Validation);
            return;
        }

        if (target == 1)
        {
            cell.SheetId = result.UnsignedIntValue;
            cell.HasSheetId = true;
        }
    }

    internal static void ReadOptionalBooleanAttribute(XlsxCalculationChainPart part, XmlReader reader, string attributeName, XlsxCalculationChainCell cell, int target)
    {
        string? text = reader.GetAttribute(attributeName);
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseBoolean(text);
        if (!result.IsValid)
        {
            AddValueDiagnostics(part, result.Validation);
            return;
        }

        if (target == 1)
        {
            cell.InChildChain = result.BooleanValue;
            cell.HasInChildChain = true;
        }
        else if (target == 2)
        {
            cell.NewDependencyLevel = result.BooleanValue;
            cell.HasNewDependencyLevel = true;
        }
        else if (target == 3)
        {
            cell.NewThread = result.BooleanValue;
            cell.HasNewThread = true;
        }
        else if (target == 4)
        {
            cell.Array = result.BooleanValue;
            cell.HasArray = true;
        }
    }

    internal static void AddValueDiagnostics(XlsxCalculationChainPart part, XlsxValidationResult validation)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            part.AddDiagnostic(diagnostic);
        }
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

    internal static void ReadUnknownCellAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool known = reader.NamespaceURI == "" && (reader.LocalName == "r" || reader.LocalName == "i" || reader.LocalName == "s" || reader.LocalName == "l" || reader.LocalName == "t" || reader.LocalName == "a");
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!known && !namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }

        reader.MoveToElement();
    }

    internal static bool IsCalculationChainNamespace(XlsxCalculationChainPart part, string namespaceUri)
    {
        if (namespaceUri == part.NamespaceUri)
        {
            return true;
        }

        return XlsxWorkbookPartReader.IsSpreadsheetNamespace(namespaceUri);
    }
}
