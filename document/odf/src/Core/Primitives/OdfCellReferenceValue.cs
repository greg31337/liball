namespace odf;

internal class OdfQuotedSheetValue
{
    internal string LexicalValue = "";
    internal string SheetName = "";
    internal bool IsAbsolute;
    internal bool IsValid;
}

internal class OdfCellAddressValue
{
    internal string LexicalValue = "";
    internal string Source = "";
    internal string SheetName = "";
    internal string Column = "";
    internal global::System.Numerics.BigInteger ColumnIndex;
    internal global::System.Numerics.BigInteger Row;
    internal bool HasSource;
    internal bool HasSheet;
    internal bool HasColumn;
    internal bool HasRow;
    internal bool SheetAbsolute;
    internal bool ColumnAbsolute;
    internal bool RowAbsolute;
    internal bool IsValid;
}

internal class OdfCellRangeValue
{
    internal string LexicalValue = "";
    internal OdfCellAddressValue Start = new OdfCellAddressValue();
    internal OdfCellAddressValue End = new OdfCellAddressValue();
    internal bool HasEnd;
    internal bool IsBracketed;
    internal bool IsValid;
}

internal class OdfCellRangeListValue
{
    internal string LexicalValue = "";
    internal OdfCellRangeValue[] Ranges = [];
    internal bool IsValid;
}
