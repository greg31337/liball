using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Writes parsed Excel formulas back to text while preserving supported operators, references, literals, and function calls.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxFormulaWriter : IXlsxFormulaWriter
{
    /// <summary>
    /// Initializes a new formula writer with its default state.
    /// </summary>
    public XlsxFormulaWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "formula">Excel formula text without a leading equals sign.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxFormulaWriteResult WriteString(string formula)
    {
        XlsxFormulaParseResult parseResult = new XlsxFormulaParser().Parse(formula);
        return WriteParseResult(parseResult);
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "parseResult">The parse result value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxFormulaWriteResult WriteParseResult(XlsxFormulaParseResult parseResult)
    {
        XlsxFormulaWriteResult result = new XlsxFormulaWriteResult();
        if (parseResult == null)
        {
            result.AddError("formulaWriter.parseResultNull", "Formula parse result must not be null.");
            return result;
        }

        CopyParseDiagnostics(result, parseResult);
        if (parseResult.Formula != null && parseResult.Formula.Length > 0)
        {
            result.Text = parseResult.Formula;
            return result;
        }

        if (!parseResult.HasRoot || parseResult.Root == null)
        {
            result.AddError("formulaWriter.rootMissing", "Formula parse result must contain a root node or original formula text.");
            return result;
        }

        result.Text = WriteNodeText(parseResult.Root);
        return result;
    }

    internal XlsxFormulaWriteResult WriteNode(XlsxFormulaNode node)
    {
        XlsxFormulaWriteResult result = new XlsxFormulaWriteResult();
        if (node == null)
        {
            result.AddError("formulaWriter.nodeNull", "Formula node must not be null.");
            return result;
        }

        result.Text = WriteNodeText(node);
        if (result.Text.Length == 0 && node.Kind != XlsxFormulaNodeKind.Literal)
        {
            result.AddError("formulaWriter.nodeUnsupported", "Formula node could not be written.");
        }

        return result;
    }

    internal static string WriteNodeText(XlsxFormulaNode node)
    {
        if (node == null)
        {
            return "";
        }

        if (node.Text != null && node.Text.Length > 0)
        {
            return node.Text;
        }

        if (node.Kind == XlsxFormulaNodeKind.Literal || node.Kind == XlsxFormulaNodeKind.Name || node.Kind == XlsxFormulaNodeKind.Reference || node.Kind == XlsxFormulaNodeKind.ExternalReference || node.Kind == XlsxFormulaNodeKind.StructuredReference)
        {
            return XlsxCellValueConverter.NormalizeText(node.Text);
        }

        if (node.Kind == XlsxFormulaNodeKind.UnaryOperation)
        {
            return WriteUnaryNodeText(node);
        }

        if (node.Kind == XlsxFormulaNodeKind.BinaryOperation || node.Kind == XlsxFormulaNodeKind.Range)
        {
            return WriteBinaryNodeText(node);
        }

        if (node.Kind == XlsxFormulaNodeKind.FunctionCall)
        {
            return WriteFunctionNodeText(node);
        }

        if (node.Kind == XlsxFormulaNodeKind.ArrayConstant)
        {
            return WriteArrayNodeText(node);
        }

        return XlsxCellValueConverter.NormalizeText(node.Text);
    }

    internal static string WriteUnaryNodeText(XlsxFormulaNode node)
    {
        string operand = "";
        if (node.Children.Count > 0)
        {
            operand = WriteNodeText(node.Children[0]);
        }

        if (node.Operator == "%")
        {
            return operand + "%";
        }

        return XlsxCellValueConverter.NormalizeText(node.Operator) + operand;
    }

    internal static string WriteBinaryNodeText(XlsxFormulaNode node)
    {
        string left = "";
        string right = "";
        if (node.Children.Count > 0)
        {
            left = WriteNodeText(node.Children[0]);
        }

        if (node.Children.Count > 1)
        {
            right = WriteNodeText(node.Children[1]);
        }

        return left + XlsxCellValueConverter.NormalizeText(node.Operator) + right;
    }

    internal static string WriteFunctionNodeText(XlsxFormulaNode node)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(XlsxCellValueConverter.NormalizeText(node.Operator));
        builder.Append("(");
        int index = 0;
        while (index < node.Children.Count)
        {
            if (index > 0)
            {
                builder.Append(",");
            }

            builder.Append(WriteNodeText(node.Children[index]));
            index = index + 1;
        }

        builder.Append(")");
        return builder.ToString();
    }

    internal static string WriteArrayNodeText(XlsxFormulaNode node)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("{");
        int index = 0;
        while (index < node.Children.Count)
        {
            if (index > 0)
            {
                builder.Append(",");
            }

            builder.Append(WriteNodeText(node.Children[index]));
            index = index + 1;
        }

        builder.Append("}");
        return builder.ToString();
    }

    internal static void CopyParseDiagnostics(XlsxFormulaWriteResult result, XlsxFormulaParseResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}
