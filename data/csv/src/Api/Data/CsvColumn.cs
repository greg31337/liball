namespace csv;

/// <summary>
/// Describes one CSV column, including its position, inferred type, sample statistics, format, and diagnostics.
/// </summary>
public class CsvColumn
{
    /// <summary>
    /// The column name.
    /// </summary>
    public string Name;
    /// <summary>
    /// The zero-based column position.
    /// </summary>
    public int Index;
    /// <summary>
    /// The inferred or declared value kind.
    /// </summary>
    public CsvTypeKind TypeKind;
    /// <summary>
    /// Whether values may be null.
    /// </summary>
    public bool IsNullable;
    /// <summary>
    /// An optional format associated with the column.
    /// </summary>
    public string FormatHint;
    /// <summary>
    /// The number of sampled rows examined for this column.
    /// </summary>
    public int SampledCount;
    /// <summary>
    /// The number of sampled rows without this column position.
    /// </summary>
    public int MissingCount;
    /// <summary>
    /// The number of sampled fields containing null or a configured null token.
    /// </summary>
    public int NullCount;
    /// <summary>
    /// The number of sampled fields containing empty text.
    /// </summary>
    public int EmptyCount;
    /// <summary>
    /// The number of sampled fields containing non-null, non-empty text.
    /// </summary>
    public int NonNullCount;
    /// <summary>
    /// Diagnostics associated with this object.
    /// </summary>
    public CsvDiagnosticList Diagnostics;

    /// <summary>
    /// Initializes a new CsvColumn with library defaults.
    /// </summary>
    public CsvColumn()
    {
        Name = "";
        Index = 0;
        TypeKind = CsvTypeKind.Unknown;
        IsNullable = true;
        FormatHint = "";
        SampledCount = 0;
        MissingCount = 0;
        NullCount = 0;
        EmptyCount = 0;
        NonNullCount = 0;
        Diagnostics = CsvDiagnosticList.Create();
    }

    /// <summary>
    /// Creates an initialized instance from the supplied values.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <param name="index">The zero-based position.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvColumn Create(string name, int index)
    {
        CsvColumn column = new CsvColumn();
        column.Name = name;
        column.Index = index;
        return column;
    }

    /// <summary>
    /// Creates an independent copy so later changes do not affect the original.
    /// </summary>
    /// <returns>An independent copy.</returns>
    internal CsvColumn Clone()
    {
        CsvColumn column = new CsvColumn();
        column.Name = Name;
        column.Index = Index;
        column.TypeKind = TypeKind;
        column.IsNullable = IsNullable;
        column.FormatHint = FormatHint;
        column.SampledCount = SampledCount;
        column.MissingCount = MissingCount;
        column.NullCount = NullCount;
        column.EmptyCount = EmptyCount;
        column.NonNullCount = NonNullCount;
        column.Diagnostics = Diagnostics.Clone();
        return column;
    }
}
