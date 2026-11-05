namespace ttf;

/// <summary>Contains legacy hdmx horizontal device widths.</summary>
public class FontHorizontalDeviceMetrics : IFontHorizontalDeviceMetrics
{
    SfntHdmxTable table;

    internal FontHorizontalDeviceMetrics(SfntHdmxTable table)
    {
        this.table = table;
    }

    /// <summary>Gets the table version.</summary>
    /// <returns>The version.</returns>
    public int GetVersion() { return table.GetVersion(); }

    /// <summary>Gets the aligned device-record size.</summary>
    /// <returns>The record size in bytes.</returns>
    public int GetDeviceRecordSize() { return table.GetDeviceRecordSize(); }

    /// <summary>Gets the number of device records.</summary>
    /// <returns>The record count.</returns>
    public int GetRecordCount() { return table.GetRecordCount(); }

    /// <summary>Gets a record's pixels-per-em size.</summary>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <returns>The pixel size.</returns>
    public int GetPixelSize(int recordIndex)
    {
        return GetRecord(recordIndex).GetPixelSize();
    }

    /// <summary>Gets a record's maximum glyph width.</summary>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <returns>The maximum pixel width.</returns>
    public int GetMaximumWidth(int recordIndex)
    {
        return GetRecord(recordIndex).GetMaximumWidth();
    }

    /// <summary>Gets one glyph width from a device record.</summary>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <returns>The pixel width.</returns>
    public int GetWidth(int recordIndex, int glyphId)
    {
        SfntHdmxRecord record = GetRecord(recordIndex);

        if (glyphId < 0 || glyphId >= record.GetGlyphCount())
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }

        return record.GetWidth(glyphId);
    }

    /// <summary>Finds a device record by pixels per em.</summary>
    /// <param name="pixelSize">The pixel size from zero through 255.</param>
    /// <returns>The record index, or negative one when absent.</returns>
    public int FindRecordIndex(int pixelSize)
    {
        if (pixelSize < 0 || pixelSize > 0xFF)
        {
            throw new System.ArgumentOutOfRangeException("pixelSize");
        }

        int index = 0;

        while (index < table.GetRecordCount())
        {
            if (table.GetRecord(index).GetPixelSize() == pixelSize)
            {
                return index;
            }

            index += 1;
        }

        return -1;
    }

    SfntHdmxRecord GetRecord(int index)
    {
        if (index < 0 || index >= table.GetRecordCount())
        {
            throw new System.ArgumentOutOfRangeException("recordIndex");
        }

        return table.GetRecord(index);
    }
}
