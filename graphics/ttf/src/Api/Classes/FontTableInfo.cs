namespace ttf;

/// <summary>Describes one bounded table and provides optional raw-byte access.</summary>
public class FontTableInfo : IFontTableInfo
{
    SfntTableData table;
    SfntTableCache tableCache;
    FontTableKind kind;

    internal FontTableInfo(SfntTableData table, SfntTableCache tableCache)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (tableCache == null)
        {
            throw new System.ArgumentNullException("tableCache");
        }

        this.table = table;
        this.tableCache = tableCache;
        SfntTableClassification classification = table.GetClassification();
        if (classification == SfntTableClassification.Known) { kind = FontTableKind.Known; }
        else if (classification == SfntTableClassification.Unsupported) { kind = FontTableKind.Unsupported; }
        else { kind = FontTableKind.Unknown; }
    }

    /// <summary>Gets the big-endian table tag.</summary>
    /// <returns>The table tag.</returns>
    public int GetTag() { return table.GetTag(); }

    /// <summary>Gets the directory checksum.</summary>
    /// <returns>The stored checksum.</returns>
    public uint GetChecksum() { return table.GetChecksum(); }

    /// <summary>Gets the table offset in normalized SFNT data.</summary>
    /// <returns>The absolute normalized-data offset.</returns>
    public long GetOffset() { return table.GetOffset(); }

    /// <summary>Gets the bounded table length.</summary>
    /// <returns>The length in bytes.</returns>
    public int GetLength() { return table.GetLength(); }

    /// <summary>Gets the table classification.</summary>
    /// <returns>The classification.</returns>
    public FontTableKind GetKind() { return kind; }

    /// <summary>Reports whether a parser-defined version can be read.</summary>
    /// <returns>True when major and minor version values are available.</returns>
    public bool HasVersion() { return SfntTableInspection.HasVersion(table); }

    /// <summary>Gets the parser-defined major version.</summary>
    /// <returns>The major version.</returns>
    /// <exception cref="System.InvalidOperationException">No version is available.</exception>
    public int GetMajorVersion()
    {
        return SfntTableInspection.GetVersion(table).GetMajorVersion();
    }

    /// <summary>Gets the parser-defined minor version.</summary>
    /// <returns>The minor version.</returns>
    /// <exception cref="System.InvalidOperationException">No version is available.</exception>
    public int GetMinorVersion()
    {
        return SfntTableInspection.GetVersion(table).GetMinorVersion();
    }

    /// <summary>Gets semantic support for the table and its version.</summary>
    /// <returns>The semantic support state.</returns>
    public FontTableSupportState GetSupportState()
    {
        return SfntTableInspection.GetSupportState(table, tableCache.GetMode());
    }

    /// <summary>Gets the table checksum validation result.</summary>
    /// <returns>The checksum validation state.</returns>
    public FontChecksumValidationState GetChecksumValidationState()
    {
        return table.GetChecksumValidationState();
    }

    /// <summary>Gets the table offset and padding alignment result.</summary>
    /// <returns>The alignment validation state.</returns>
    public FontTableAlignmentState GetAlignmentState()
    {
        return table.GetAlignmentState();
    }

    /// <summary>Reports whether raw bytes were retained.</summary>
    /// <returns>True when raw bytes can be copied.</returns>
    public bool HasRawData() { return table.HasRawData(); }

    /// <summary>Copies the complete bounded table bytes.</summary>
    /// <returns>A new byte array.</returns>
    /// <exception cref="System.InvalidOperationException">Raw preservation was disabled.</exception>
    public byte[] CopyData() { return table.CopyRawData(); }
}
