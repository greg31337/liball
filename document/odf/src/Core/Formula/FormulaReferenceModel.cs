namespace odf;

internal static class FormulaReferenceModel
{
    internal static void BindReferences(OdfFormulaNode node, string source)
    {
        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            BindReferences(node.Children[childIndex], source);
            childIndex = childIndex + 1;
        }

        if (node.Kind == OdfFormulaNodeKind.Reference
            || (node.Kind == OdfFormulaNodeKind.Range
                && node.Children.Length == 0)
            || (node.Kind == OdfFormulaNodeKind.ExternalReference
                && node.Text.Length > 0
                && node.Text[0] == '['))
        {
            node.Reference = CreateAddressReference(node);
        }
        else if (node.Kind == OdfFormulaNodeKind.Name
            || node.Kind == OdfFormulaNodeKind.ExternalReference)
        {
            node.Reference = CreateNameReference(node);
        }
        else if (node.Kind == OdfFormulaNodeKind.Range
            || node.Kind == OdfFormulaNodeKind.Intersection
            || node.Kind == OdfFormulaNodeKind.ReferenceConcatenation)
        {
            node.Reference = CreateOperatorReference(node, source);
        }
    }

    static OdfFormulaReference CreateAddressReference(OdfFormulaNode node)
    {
        OdfFormulaReference result = new OdfFormulaReference();
        result.Kind = node.Kind;
        result.LexicalValue = node.Text;
        if (node.Text == "[#REF!]")
        {
            result.IsReferenceError = true;
            return result;
        }

        OdfCellRangeValue range = OdfCellAddressParser.ParseFormulaReference(
            node.Text);
        if (!range.IsValid)
        {
            return result;
        }
        result.Start = CreateEndpoint(range.Start, "");
        result.Source = range.Start.Source;
        result.HasSource = range.Start.HasSource;
        result.HasEnd = range.HasEnd;
        if (range.HasEnd)
        {
            result.End = CreateEndpoint(
                range.End,
                result.Start.EffectiveSheetName);
            result.IsThreeDimensional = range.Start.HasSheet
                && range.End.HasSheet
                && !global::System.String.Equals(
                    range.Start.SheetName,
                    range.End.SheetName,
                    global::System.StringComparison.Ordinal);
        }
        return result;
    }

    static OdfFormulaReferenceEndpoint CreateEndpoint(
        OdfCellAddressValue value,
        string inheritedSheet)
    {
        OdfFormulaReferenceEndpoint result =
            new OdfFormulaReferenceEndpoint();
        result.SheetName = value.SheetName;
        result.EffectiveSheetName = value.HasSheet
            ? value.SheetName
            : inheritedSheet;
        result.ColumnName = value.Column;
        result.ColumnNumber = value.ColumnIndex;
        result.RowNumber = value.Row;
        result.HasSheet = value.HasSheet;
        result.HasColumn = value.HasColumn;
        result.HasRow = value.HasRow;
        result.SheetAbsolute = value.SheetAbsolute;
        result.ColumnAbsolute = value.ColumnAbsolute;
        result.RowAbsolute = value.RowAbsolute;
        return result;
    }

    static OdfFormulaReference CreateNameReference(OdfFormulaNode node)
    {
        OdfFormulaReference result = new OdfFormulaReference();
        result.Kind = node.Kind;
        result.LexicalValue = node.Text;
        string remaining = node.Text;
        if (remaining.Length > 0 && remaining[0] == '\'')
        {
            int quoteEnd = FindClosingQuote(remaining);
            if (quoteEnd >= 0 && quoteEnd + 1 < remaining.Length)
            {
                char separator = remaining[quoteEnd + 1];
                string qualifier = DecodeQuotedName(
                    remaining.Substring(0, quoteEnd + 1));
                if (separator == '#')
                {
                    result.Source = qualifier;
                    result.HasSource = true;
                    result.Kind = OdfFormulaNodeKind.ExternalReference;
                    remaining = remaining.Substring(quoteEnd + 2);
                }
                else if (separator == '.')
                {
                    result.NameSheet = qualifier;
                    remaining = remaining.Substring(quoteEnd + 2);
                }
            }
            else if (quoteEnd == remaining.Length - 1)
            {
                result.Name = DecodeQuotedName(remaining);
                result.IsLabel = true;
                return result;
            }
        }
        if (remaining.Length >= 2
            && remaining[0] == '$'
            && remaining[1] == '$')
        {
            result.IsExplicitName = true;
            remaining = remaining.Substring(2);
        }
        result.Name = DecodeQuotedName(remaining);
        return result;
    }

    static OdfFormulaReference CreateOperatorReference(
        OdfFormulaNode node,
        string source)
    {
        OdfFormulaReference result = new OdfFormulaReference();
        result.Kind = node.Kind;
        if (node.Start >= 0
            && node.Length >= 0
            && node.Start + node.Length <= source.Length)
        {
            result.LexicalValue = source.Substring(node.Start, node.Length);
        }
        if (node.Children.Length == 2)
        {
            OdfFormulaReference? left = node.Children[0].Reference;
            OdfFormulaReference? right = node.Children[1].Reference;
            if (left != null && right != null)
            {
                result.Items = [left, right];
            }
        }
        return result;
    }

    static int FindClosingQuote(string value)
    {
        int index = 1;
        while (index < value.Length)
        {
            if (value[index] == '\'')
            {
                if (index + 1 < value.Length && value[index + 1] == '\'')
                {
                    index = index + 2;
                }
                else
                {
                    return index;
                }
            }
            else
            {
                index = index + 1;
            }
        }
        return -1;
    }

    static string DecodeQuotedName(string value)
    {
        if (value.Length < 2
            || value[0] != '\''
            || value[value.Length - 1] != '\'')
        {
            return value;
        }
        global::System.Text.StringBuilder result =
            new global::System.Text.StringBuilder();
        int index = 1;
        while (index < value.Length - 1)
        {
            if (value[index] == '\''
                && index + 1 < value.Length - 1
                && value[index + 1] == '\'')
            {
                result.Append('\'');
                index = index + 2;
            }
            else
            {
                result.Append(value[index]);
                index = index + 1;
            }
        }
        return result.ToString();
    }
}
