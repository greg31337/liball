using System;

namespace odf;

internal class TableSpanValue
{
    internal long Value = 1;
    internal bool IsPresent;
    internal bool IsValid = true;
}

internal static class TableCellStructureModel
{
    internal static void LoadCellStructures(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for table-structure materialization.");
        }
        int tableIndex = 0;
        while (tableIndex < document.Tables.Length)
        {
            OdfTable table = document.Tables[tableIndex];
            int rowIndex = 0;
            while (rowIndex < table.Rows.Length)
            {
                OdfRow row = table.Rows[rowIndex];
                int cellIndex = 0;
                while (cellIndex < row.Cells.Length)
                {
                    InitializeCellStructure(row.Cells[cellIndex]);
                    cellIndex = cellIndex + 1;
                }
                rowIndex = rowIndex + 1;
            }
            tableIndex = tableIndex + 1;
        }
    }

    internal static void SynchronizeCellStructures(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for table-structure synchronization.");
        }
        int tableIndex = 0;
        while (tableIndex < document.Tables.Length)
        {
            OdfTable table = document.Tables[tableIndex];
            int rowIndex = 0;
            while (rowIndex < table.Rows.Length)
            {
                OdfRow row = table.Rows[rowIndex];
                int cellIndex = 0;
                while (cellIndex < row.Cells.Length)
                {
                    SynchronizeCellStructure(row.Cells[cellIndex]);
                    cellIndex = cellIndex + 1;
                }
                rowIndex = rowIndex + 1;
            }
            tableIndex = tableIndex + 1;
        }
    }

    internal static void InitializeCellStructure(OdfCell cell)
    {
        OdfNode? node = cell.Node;
        if (node == null)
        {
            RememberStructure(cell);
            return;
        }
        TableSpanValue columns = ReadSpan(
            node,
            "number-columns-spanned");
        TableSpanValue rows = ReadSpan(
            node,
            "number-rows-spanned");
        TableSpanValue matrixColumns = ReadSpan(
            node,
            "number-matrix-columns-spanned");
        TableSpanValue matrixRows = ReadSpan(
            node,
            "number-matrix-rows-spanned");
        cell.ColumnSpan = columns.Value;
        cell.RowSpan = rows.Value;
        cell.MatrixColumnSpan = matrixColumns.Value;
        cell.MatrixRowSpan = matrixRows.Value;
        cell.IsMergeOrigin = columns.IsPresent || rows.IsPresent;
        cell.IsMatrixFormula = matrixColumns.IsPresent
            || matrixRows.IsPresent;
        string formula = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Table,
            "formula");
        cell.HasValidStructure = columns.IsValid
            && rows.IsValid
            && matrixColumns.IsValid
            && matrixRows.IsValid
            && !(cell.IsCovered && cell.IsMergeOrigin)
            && !(cell.IsCovered && cell.IsMatrixFormula)
            && !(cell.IsMergeOrigin && cell.IsMatrixFormula)
            && (!cell.IsMatrixFormula
                || matrixColumns.IsPresent
                    && matrixRows.IsPresent
                    && formula.Length > 0)
            && !(cell.IsCovered && cell.NestedTables.Length > 0);
        RememberStructure(cell);
    }

    static void SynchronizeCellStructure(OdfCell cell)
    {
        OdfNode? node = cell.Node;
        if (node == null)
        {
            return;
        }
        bool merge = cell.IsMergeOrigin
            || cell.ColumnSpan != 1
            || cell.RowSpan != 1;
        bool matrix = cell.IsMatrixFormula
            || cell.MatrixColumnSpan != 1
            || cell.MatrixRowSpan != 1;
        if (cell.ColumnSpan <= 0
            || cell.RowSpan <= 0
            || cell.MatrixColumnSpan <= 0
            || cell.MatrixRowSpan <= 0
            || cell.IsCovered && (merge || matrix)
            || merge && matrix
            || cell.IsCovered && cell.NestedTables.Length > 0
            || matrix && cell.Formula.Length == 0)
        {
            throw new OdfEditException(
                "The requested cell span, formula, or nested-table structure is invalid.");
        }
        if (cell.IsCovered != cell.OriginalIsCovered)
        {
            SetCellElementKind(node, cell.IsCovered);
        }
        WriteSpan(
            node,
            "number-columns-spanned",
            cell.ColumnSpan,
            merge);
        WriteSpan(
            node,
            "number-rows-spanned",
            cell.RowSpan,
            merge);
        WriteSpan(
            node,
            "number-matrix-columns-spanned",
            cell.MatrixColumnSpan,
            matrix);
        WriteSpan(
            node,
            "number-matrix-rows-spanned",
            cell.MatrixRowSpan,
            matrix);
        cell.IsMergeOrigin = merge;
        cell.IsMatrixFormula = matrix;
        cell.HasValidStructure = true;
        RememberStructure(cell);
    }

    static TableSpanValue ReadSpan(OdfNode node, string localName)
    {
        TableSpanValue result = new TableSpanValue();
        OdfAttribute? attribute = FindTableAttribute(node, localName);
        if (attribute == null)
        {
            return result;
        }
        result.IsPresent = true;
        OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(
            attribute.Value);
        result.IsValid = parsed.IsValid && parsed.IntegerValue > 0;
        if (result.IsValid)
        {
            result.Value = parsed.IntegerValue;
        }
        return result;
    }

    static OdfAttribute? FindTableAttribute(
        OdfNode node,
        string localName)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == OdfNamespaces.Table
                && attribute.Name.LocalName == localName)
            {
                return attribute;
            }
            index = index + 1;
        }
        return null;
    }

    static void WriteSpan(
        OdfNode node,
        string localName,
        long value,
        bool present)
    {
        if (present)
        {
            StyleDocumentModel.WriteAttribute(
                node,
                OdfNamespaces.Table,
                localName,
                OdfNamespaces.TablePrefix,
                value.ToString());
        }
        else
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                OdfNamespaces.Table,
                localName);
        }
    }

    static void SetCellElementKind(OdfNode node, bool covered)
    {
        if (node.Name == null)
        {
            throw new OdfEditException(
                "A table cell requires an element name.");
        }
        node.Name.NamespaceName = OdfNamespaces.Table;
        node.Name.Prefix = OdfNamespaces.TablePrefix;
        node.Name.LocalName = covered
            ? "covered-table-cell"
            : "table-cell";
    }

    static void RememberStructure(OdfCell cell)
    {
        cell.OriginalIsCovered = cell.IsCovered;
        cell.OriginalColumnSpan = cell.ColumnSpan;
        cell.OriginalRowSpan = cell.RowSpan;
        cell.OriginalMatrixColumnSpan = cell.MatrixColumnSpan;
        cell.OriginalMatrixRowSpan = cell.MatrixRowSpan;
    }
}
