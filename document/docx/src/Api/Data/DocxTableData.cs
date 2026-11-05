namespace docx;

/// <summary>
/// Stores the named values used to expand a template table row.
/// </summary>
public class DocxTableData
{
    /// <summary>
    /// The named values for each template row.
    /// </summary>
    public List<Dictionary<string, string>> Rows;

    /// <summary>
    /// Creates a table data instance.
    /// </summary>
    public DocxTableData()
    {
        this.Rows = new List<Dictionary<string, string>>();
    }

    internal static DocxTableData Create(List<Dictionary<string, string>> rows)
    {
        if (rows == null)
        {
            throw new ArgumentNullException("rows");
        }

        DocxTableData data = new DocxTableData();

        foreach (Dictionary<string, string> row in rows)
        {
            data.AddRowValues(row);
        }

        return data;
    }

    internal Dictionary<string, string> AddRow()
    {
        Dictionary<string, string> row = new Dictionary<string, string>();
        this.Rows.Add(row);
        return row;
    }

    internal void AddRowValues(Dictionary<string, string> values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        this.Rows.Add(CloneRow(values));
    }

    internal int RowCount()
    {
        return this.Rows.Count;
    }

    internal Dictionary<string, string> GetRow(int index)
    {
        return this.Rows[index];
    }

    internal static Dictionary<string, string> CloneRow(Dictionary<string, string> values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        Dictionary<string, string> row = new Dictionary<string, string>();

        foreach (KeyValuePair<string, string> value in values)
        {
            DocxTemplate.ValidatePlaceholderValue(value.Key, value.Value);
            row.Add(value.Key, value.Value);
        }

        return row;
    }
}
