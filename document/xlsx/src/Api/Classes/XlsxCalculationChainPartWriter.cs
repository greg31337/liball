using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes calculation chain part data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxCalculationChainPartWriter : IXlsxCalculationChainPartWriter
{
    /// <summary>
    /// Initializes a new calculation chain part writer with its default state.
    /// </summary>
    public XlsxCalculationChainPartWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxCalculationChainPartWriteResult WriteString(XlsxCalculationChainPart part)
    {
        XlsxCalculationChainPartWriteResult result = WriteBytes(part);
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
    public XlsxCalculationChainPartWriteResult WriteBytes(XlsxCalculationChainPart? part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxCalculationChainPartWriteResult result = WriteStream(stream, part);
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
    public XlsxCalculationChainPartWriteResult WriteStream(Stream stream, XlsxCalculationChainPart? part)
    {
        XlsxCalculationChainPartWriteResult result = ValidatePart(part);
        if (!result.IsValid || part == null)
        {
            return result;
        }

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("", "calcChain", XlsxNamespaces.Spreadsheet);
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.UnknownRootAttributes);
            foreach (XlsxCalculationChainCell cell in part.Cells)
            {
                WriteCell(writer, cell);
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

    internal static void WriteCell(XmlWriter writer, XlsxCalculationChainCell cell)
    {
        writer.WriteStartElement("", "c", XlsxNamespaces.Spreadsheet);
        writer.WriteAttributeString("r", cell.Reference);
        if (cell.HasSheetId)
        {
            writer.WriteAttributeString("i", XlsxXmlValueWriter.WriteUnsignedInteger(cell.SheetId));
        }

        if (cell.HasInChildChain)
        {
            writer.WriteAttributeString("s", XlsxXmlValueWriter.WriteBoolean(cell.InChildChain));
        }

        if (cell.HasNewDependencyLevel)
        {
            writer.WriteAttributeString("l", XlsxXmlValueWriter.WriteBoolean(cell.NewDependencyLevel));
        }

        if (cell.HasNewThread)
        {
            writer.WriteAttributeString("t", XlsxXmlValueWriter.WriteBoolean(cell.NewThread));
        }

        if (cell.HasArray)
        {
            writer.WriteAttributeString("a", XlsxXmlValueWriter.WriteBoolean(cell.Array));
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, cell.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static XlsxCalculationChainPartWriteResult ValidatePart(XlsxCalculationChainPart? part)
    {
        XlsxCalculationChainPartWriteResult result = new XlsxCalculationChainPartWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "calculationChain.partNull", "Calculation chain part must not be null."));
            return result;
        }

        if (part.UnknownRootAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "calculationChain.unknownRootAttributesNull", "Calculation chain unknown root attributes list must not be null."));
        }

        if (part.Cells == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "calculationChain.cellsNull", "Calculation chain cells list must not be null."));
            return result;
        }

        if (part.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "calculationChain.unknownXmlNull", "Calculation chain unknown XML list must not be null."));
        }

        int index = 0;
        foreach (XlsxCalculationChainCell cell in part.Cells)
        {
            ValidateCell(result, cell, index);
            index = index + 1;
        }

        return result;
    }

    internal static void ValidateCell(XlsxCalculationChainPartWriteResult result, XlsxCalculationChainCell cell, int index)
    {
        if (cell == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "calculationChain.cellNull", "Calculation chain cells must not contain null entries."));
            return;
        }

        if (cell.Reference == null || cell.Reference.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "calculationChain.cellReferenceMissing", "Calculation chain cell must have an r attribute."));
        }
        else
        {
            XlsxA1CellReferenceResult reference = XlsxA1CellReferenceConverter.Parse(cell.Reference);
            if (!reference.IsValid)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "calculationChain.cellReferenceInvalid", "Calculation chain cell r attribute must be a valid A1 cell reference."));
                foreach (XlsxDiagnostic diagnostic in reference.Validation.Diagnostics)
                {
                    result.AddDiagnostic(diagnostic);
                }
            }
        }

        if (cell.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "calculationChain.cellUnknownAttributesNull", "Calculation chain cell unknown attributes list must not be null."));
        }
    }
}
