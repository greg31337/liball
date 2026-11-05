namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontTableInfo"/>.</summary>
public interface IFontTableInfo
{
    /// <summary>Gets the big-endian table tag.</summary>
    /// <returns>The table tag.</returns>
    int GetTag();

    /// <summary>Gets the directory checksum.</summary>
    /// <returns>The stored checksum.</returns>
    uint GetChecksum();

    /// <summary>Gets the table offset in normalized SFNT data.</summary>
    /// <returns>The absolute normalized-data offset.</returns>
    long GetOffset();

    /// <summary>Gets the bounded table length.</summary>
    /// <returns>The length in bytes.</returns>
    int GetLength();

    /// <summary>Gets the table classification.</summary>
    /// <returns>The classification.</returns>
    FontTableKind GetKind();

    /// <summary>Reports whether a parser-defined version can be read.</summary>
    /// <returns>True when major and minor version values are available.</returns>
    bool HasVersion();

    /// <summary>Gets the parser-defined major version.</summary>
    /// <returns>The major version.</returns>
    /// <exception cref="System.InvalidOperationException">No version is available.</exception>
    int GetMajorVersion();

    /// <summary>Gets the parser-defined minor version.</summary>
    /// <returns>The minor version.</returns>
    /// <exception cref="System.InvalidOperationException">No version is available.</exception>
    int GetMinorVersion();

    /// <summary>Gets semantic support for the table and its version.</summary>
    /// <returns>The semantic support state.</returns>
    FontTableSupportState GetSupportState();

    /// <summary>Gets the table checksum validation result.</summary>
    /// <returns>The checksum validation state.</returns>
    FontChecksumValidationState GetChecksumValidationState();

    /// <summary>Gets the table offset and padding alignment result.</summary>
    /// <returns>The alignment validation state.</returns>
    FontTableAlignmentState GetAlignmentState();

    /// <summary>Reports whether raw bytes were retained.</summary>
    /// <returns>True when raw bytes can be copied.</returns>
    bool HasRawData();

    /// <summary>Copies the complete bounded table bytes.</summary>
    /// <returns>A new byte array.</returns>
    /// <exception cref="System.InvalidOperationException">Raw preservation was disabled.</exception>
    byte[] CopyData();
}
