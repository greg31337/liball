namespace odf;

/// <summary>
/// Describes an entry discovered in an OpenDocument package.
/// </summary>
public class OdfPackageEntry
{
    /// <summary>
    /// The entry path decoded from the ZIP central directory.
    /// </summary>
    public string Path = "";
    /// <summary>
    /// The normalized package-relative entry path when one can be derived safely.
    /// </summary>
    public string CanonicalPath = "";
    /// <summary>
    /// The package-entry compression method.
    /// </summary>
    public OdfCompressionMethod CompressionMethod;
    /// <summary>
    /// The numeric ZIP compression method stored in the package.
    /// </summary>
    public int CompressionMethodCode;
    /// <summary>
    /// The compressed entry size in bytes.
    /// </summary>
    public long CompressedSize;
    /// <summary>
    /// The expanded entry size in bytes.
    /// </summary>
    public long ExpandedSize;
    /// <summary>
    /// The CRC-32 value recorded by the ZIP central directory.
    /// </summary>
    public uint Crc32;
    /// <summary>
    /// The byte offset of the entry local header.
    /// </summary>
    public long LocalHeaderOffset;
    /// <summary>
    /// The extra-field length stored in the central-directory header.
    /// </summary>
    public int CentralExtraFieldLength;
    /// <summary>
    /// The extra-field length stored in the local header.
    /// </summary>
    public int LocalExtraFieldLength;
    /// <summary>
    /// Whether the ZIP general-purpose flag declares the entry name as UTF-8.
    /// </summary>
    public bool UsesUtf8Name;
    /// <summary>
    /// Whether the entry obtains size or offset information from ZIP64 metadata.
    /// </summary>
    public bool UsesZip64;
    /// <summary>
    /// Whether the local entry data is followed by a ZIP data descriptor.
    /// </summary>
    public bool UsesDataDescriptor;
    /// <summary>
    /// Whether the entry represents a directory.
    /// </summary>
    public bool IsDirectory;
    /// <summary>
    /// Whether this entry has the earliest local header in the package.
    /// </summary>
    public bool IsFirstEntry;
    /// <summary>
    /// Whether <see cref="Path"/> already has the canonical package path form.
    /// </summary>
    public bool IsCanonicalPath;
    /// <summary>
    /// Whether the entry is encrypted according to its ZIP flags.
    /// </summary>
    public bool IsEncrypted;
    /// <summary>
    /// Whether the expanded entry data matches the recorded CRC-32 value.
    /// </summary>
    public bool HasValidCrc;
}
