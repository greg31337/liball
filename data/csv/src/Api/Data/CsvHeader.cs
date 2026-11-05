namespace csv;

/// <summary>
/// Stores column names and applies configured lookup and normalization rules.
/// </summary>
public class CsvHeader
{
    /// <summary>
    /// Column names in zero-based field order.
    /// </summary>
    public string[] Names;
    /// <summary>
    /// Whether name lookup distinguishes letter case.
    /// </summary>
    public bool CaseSensitive;
    /// <summary>
    /// Whether names are trimmed and internal whitespace is collapsed.
    /// </summary>
    public bool NormalizeHeaderNames;
    /// <summary>
    /// The repeated-name policy.
    /// </summary>
    public CsvDuplicateHeaderMode DuplicateHeaderMode;
    /// <summary>
    /// The empty-header-name policy.
    /// </summary>
    public CsvMissingHeaderMode MissingHeaderMode;
    /// <summary>
    /// Diagnostics associated with this object.
    /// </summary>
    public CsvDiagnosticList Diagnostics;

    /// <summary>
    /// Initializes a new CsvHeader with library defaults.
    /// </summary>
    public CsvHeader()
    {
        Names = new string[0];
        CaseSensitive = true;
        NormalizeHeaderNames = false;
        DuplicateHeaderMode = CsvDuplicateHeaderMode.Error;
        MissingHeaderMode = CsvMissingHeaderMode.Generate;
        Diagnostics = CsvDiagnosticList.Create();
    }

    /// <summary>
    /// Builds a header from row fields and applies configured header policies.
    /// </summary>
    /// <param name="row">The row to process.</param>
    /// <param name="options">Header policy settings.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvHeader CreateFromRow(CsvRow row, CsvHeaderOptions options)
    {
        CsvHeader header = new CsvHeader();
        header.Names = new string[row.Fields.Length];
        header.CaseSensitive = options.CaseSensitive;
        header.NormalizeHeaderNames = options.NormalizeHeaderNames;
        header.DuplicateHeaderMode = options.DuplicateHeaderMode;
        header.MissingHeaderMode = options.MissingHeaderMode;

        int index = 0;
        while (index < row.Fields.Length)
        {
            string? name = row.GetField(index);
            if (name == null)
            {
                name = "";
            }

            if (header.NormalizeHeaderNames)
            {
                name = name.Trim();
            }

            if (name.Length == 0)
            {
                name = header.ResolveMissingNameForRow(options, index, row.RowNumber);
            }

            if (name.Length > 0 && header.FindIndexBefore(name, index) >= 0 && header.DuplicateHeaderMode == CsvDuplicateHeaderMode.Error)
            {
                CsvDiagnostic diagnostic = CsvDiagnostic.CreateHeaderError("DuplicateHeader", "Duplicate header name.", row.RowNumber, index, row.RowNumber, 0, name);
                header.Diagnostics.Add(diagnostic);
            }

            header.Names[index] = name;
            index = index + 1;
        }

        row.Header = header;
        return header;
    }

    /// <summary>
    /// Builds generated names for a header of the requested width.
    /// </summary>
    /// <param name="count">The requested item count.</param>
    /// <param name="options">Header policy settings.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvHeader CreateGenerated(int count, CsvHeaderOptions options)
    {
        CsvHeader header = new CsvHeader();
        header.Names = new string[count];
        header.CaseSensitive = options.CaseSensitive;
        header.NormalizeHeaderNames = options.NormalizeHeaderNames;
        header.DuplicateHeaderMode = options.DuplicateHeaderMode;
        header.MissingHeaderMode = options.MissingHeaderMode;

        int index = 0;
        while (index < count)
        {
            header.Names[index] = options.GeneratedHeaderPrefix + ((long)index + 1).ToString();
            index = index + 1;
        }

        return header;
    }

    /// <summary>
    /// Finds a column using case and duplicate-name rules.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <returns>The selected zero-based column position, or -1 when absent.</returns>
    internal int IndexOf(string name)
    {
        string target = Normalize(name);
        int found = -1;
        int index = 0;
        while (index < Names.Length)
        {
            string current = Normalize(Names[index]);
            if (current == target)
            {
                if (DuplicateHeaderMode == CsvDuplicateHeaderMode.LastWins)
                {
                    found = index;
                }
                else
                {
                    return index;
                }
            }

            index = index + 1;
        }

        return found;
    }

    /// <summary>
    /// Trims a name and collapses whitespace when normalization is enabled.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <returns>The normalized name, or the original name when normalization is disabled.</returns>
    internal string Normalize(string name)
    {
        string normalized = name;
        if (normalized == null)
        {
            normalized = "";
        }

        if (NormalizeHeaderNames)
        {
            normalized = normalized.Trim();
        }

        if (!CaseSensitive)
        {
            normalized = normalized.ToUpperInvariant();
        }

        return normalized;
    }

    /// <summary>
    /// Creates an independent copy so later changes do not affect the original.
    /// </summary>
    /// <returns>An independent copy.</returns>
    internal CsvHeader Clone()
    {
        CsvHeader header = new CsvHeader();
        header.Names = new string[Names.Length];
        int index = 0;
        while (index < Names.Length)
        {
            header.Names[index] = Names[index];
            index = index + 1;
        }
        header.CaseSensitive = CaseSensitive;
        header.NormalizeHeaderNames = NormalizeHeaderNames;
        header.DuplicateHeaderMode = DuplicateHeaderMode;
        header.MissingHeaderMode = MissingHeaderMode;
        header.Diagnostics = Diagnostics.Clone();
        return header;
    }

    internal string ResolveMissingName(CsvHeaderOptions options, int index)
    {
        string name = ResolveMissingNameForRow(options, index, 0);
        return name;
    }

    internal string ResolveMissingNameForRow(CsvHeaderOptions options, int index, long rowNumber)
    {
        if (options.MissingHeaderMode == CsvMissingHeaderMode.Generate)
        {
            string generated = options.GeneratedHeaderPrefix + ((long)index + 1).ToString();
            return generated;
        }

        if (options.MissingHeaderMode == CsvMissingHeaderMode.Error)
        {
            CsvDiagnostic diagnostic = CsvDiagnostic.CreateHeaderError("MissingHeader", "Header name is missing.", rowNumber, index, rowNumber, 0, "");
            Diagnostics.Add(diagnostic);
        }

        return "";
    }

    internal int FindIndexBefore(string name, int beforeIndex)
    {
        string target = Normalize(name);
        int index = 0;
        while (index < beforeIndex)
        {
            string current = Normalize(Names[index]);
            if (current == target)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }
}
