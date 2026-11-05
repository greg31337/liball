namespace ttf;

/// <summary>Defines inspection of the legacy hdmx table.</summary>
public interface IFontHorizontalDeviceMetrics
{
    /// <summary>Gets the table version.</summary>
    /// <returns>The version.</returns>
    int GetVersion();

    /// <summary>Gets the aligned device-record size.</summary>
    /// <returns>The record size in bytes.</returns>
    int GetDeviceRecordSize();

    /// <summary>Gets the number of device records.</summary>
    /// <returns>The record count.</returns>
    int GetRecordCount();

    /// <summary>Gets a record's pixels-per-em size.</summary>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <returns>The pixel size.</returns>
    int GetPixelSize(int recordIndex);

    /// <summary>Gets a record's maximum glyph width.</summary>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <returns>The maximum pixel width.</returns>
    int GetMaximumWidth(int recordIndex);

    /// <summary>Gets one glyph width from a device record.</summary>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <returns>The pixel width.</returns>
    int GetWidth(int recordIndex, int glyphId);

    /// <summary>Finds a device record by pixels per em.</summary>
    /// <param name="pixelSize">The pixel size from zero through 255.</param>
    /// <returns>The record index, or negative one when absent.</returns>
    int FindRecordIndex(int pixelSize);
}
