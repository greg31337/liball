namespace ttf;

/// <summary>Represents one axis coordinate in a format 4 <c>STAT</c> value.</summary>
public class FontStatAxisValueRecord : IFontStatAxisValueRecord
{
    SfntStatAxisValueRecord record;
    SfntStatTable table;

    internal FontStatAxisValueRecord(
        SfntStatAxisValueRecord record,
        SfntStatTable table)
    {
        this.record = record;
        this.table = table;
    }

    /// <summary>Gets the design-axis index.</summary>
    /// <returns>The zero-based axis index.</returns>
    public int GetAxisIndex() { return record.GetAxisIndex(); }

    /// <summary>Gets the design-axis tag.</summary>
    /// <returns>The big-endian axis tag.</returns>
    public int GetAxisTag()
    {
        return table.GetAxis(record.GetAxisIndex()).GetTag();
    }

    /// <summary>Gets the design-axis value.</summary>
    /// <returns>The value.</returns>
    public double GetValue() { return record.GetValue(); }
}
