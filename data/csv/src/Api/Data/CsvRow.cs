namespace csv;

/// <summary>
/// Stores one CSV record together with source location, header, raw text, and diagnostics.
/// </summary>
public class CsvRow
{
    /// <summary>
    /// The one-based logical record number.
    /// </summary>
    public long RowNumber;
    /// <summary>
    /// The one-based physical line, or zero when unavailable.
    /// </summary>
    public long Line;
    /// <summary>
    /// The one-based column, or zero when unavailable.
    /// </summary>
    public int Column;
    /// <summary>
    /// The zero-based source character offset.
    /// </summary>
    public long CharacterOffset;
    /// <summary>
    /// The zero-based source byte offset, or -1 when unavailable.
    /// </summary>
    public long ByteOffset;
    /// <summary>
    /// Field values in zero-based column order; null entries represent null fields.
    /// </summary>
    public string?[] Fields;
    /// <summary>
    /// Raw record text without its terminating line break.
    /// </summary>
    public string RawText;
    /// <summary>
    /// The header used for name lookup.
    /// </summary>
    public CsvHeader Header;
    /// <summary>
    /// Diagnostics associated with this object.
    /// </summary>
    public CsvDiagnosticList Diagnostics;

    /// <summary>
    /// Initializes a new CsvRow with library defaults.
    /// </summary>
    public CsvRow()
    {
        RowNumber = 0;
        Line = 0;
        Column = 0;
        CharacterOffset = 0;
        ByteOffset = -1;
        Fields = new string[0];
        RawText = "";
        Header = new CsvHeader();
        Diagnostics = CsvDiagnosticList.Create();
    }

    /// <summary>
    /// Creates an initialized instance from the supplied values.
    /// </summary>
    /// <param name="fields">Fields in column order; entries may be null.</param>
    /// <param name="rowNumber">The one-based logical row.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvRow Create(string?[] fields, long rowNumber)
    {
        CsvRow row = new CsvRow();
        row.RowNumber = rowNumber;
        row.Fields = CopyFields(fields);
        return row;
    }

    /// <summary>
    /// Returns a field by zero-based position.
    /// </summary>
    /// <param name="index">The zero-based position.</param>
    /// <returns>The stored value, which may be null, or empty text when <c>index</c> is outside the row.</returns>
    internal string? GetField(int index)
    {
        if (index < 0)
        {
            return "";
        }

        if (index >= Fields.Length)
        {
            return "";
        }

        string? value = Fields[index];
        return value;
    }

    /// <summary>
    /// Returns the current row field count.
    /// </summary>
    /// <returns>The length of <see cref="Fields"/>.</returns>
    internal int CountFields()
    {
        int count = Fields.Length;
        return count;
    }

    /// <summary>
    /// Checks whether the row contains a zero-based field position.
    /// </summary>
    /// <param name="index">The zero-based position.</param>
    /// <returns><c>true</c> when <c>index</c> is valid; otherwise <c>false</c>.</returns>
    internal bool HasField(int index)
    {
        if (index < 0)
        {
            return false;
        }

        if (index >= Fields.Length)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Returns a field by header name.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <returns>The stored value, which may be null, or empty text when the name is absent.</returns>
    internal string? GetFieldByName(string name)
    {
        int index = Header.IndexOf(name);
        string? value = GetField(index);
        return value;
    }

    /// <summary>
    /// Checks for a current-row field by header name.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <returns><c>true</c> when the header name resolves to a field; otherwise <c>false</c>.</returns>
    internal bool HasFieldByName(string name)
    {
        int index = Header.IndexOf(name);
        bool hasField = HasField(index);
        return hasField;
    }

    /// <summary>
    /// Replaces a field when the zero-based position is valid.
    /// </summary>
    /// <param name="index">The zero-based position.</param>
    /// <param name="value">The replacement field value, which may be null.</param>
    internal void SetField(int index, string? value)
    {
        if (index < 0)
        {
            return;
        }

        if (index >= Fields.Length)
        {
            return;
        }

        Fields[index] = value;
    }

    /// <summary>
    /// Replaces a field when the header name resolves to a valid position.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <param name="value">The replacement field value, which may be null.</param>
    internal void SetFieldByName(string name, string? value)
    {
        int index = Header.IndexOf(name);
        SetField(index, value);
    }

    /// <summary>
    /// Appends a field by replacing the field array with a one-element-larger copy.
    /// </summary>
    /// <param name="value">The field value to append, which may be null.</param>
    internal void AddField(string? value)
    {
        string?[] fields = new string?[Fields.Length + 1];
        int index = 0;
        while (index < Fields.Length)
        {
            fields[index] = Fields[index];
            index = index + 1;
        }

        fields[Fields.Length] = value;
        Fields = fields;
    }

    /// <summary>
    /// Creates an independent copy so later changes do not affect the original.
    /// </summary>
    /// <returns>An independent copy.</returns>
    internal CsvRow Clone()
    {
        CsvRow row = new CsvRow();
        row.RowNumber = RowNumber;
        row.Line = Line;
        row.Column = Column;
        row.CharacterOffset = CharacterOffset;
        row.ByteOffset = ByteOffset;
        row.Fields = CopyFields(Fields);
        row.RawText = RawText;
        row.Header = Header.Clone();
        row.Diagnostics = Diagnostics.Clone();
        return row;
    }

    internal static string?[] CopyFields(string?[] fields)
    {
        string?[] copy = new string?[fields.Length];
        int index = 0;
        while (index < fields.Length)
        {
            copy[index] = fields[index];
            index = index + 1;
        }

        return copy;
    }
}
