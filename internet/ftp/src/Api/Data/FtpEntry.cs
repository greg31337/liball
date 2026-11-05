namespace ftp;

/// <summary>
/// Represents one FTP file-system or directory-listing entry.
/// </summary>
public class FtpEntry
{
    /// <summary>
    /// The entry name exactly as supplied by the server or provider.
    /// </summary>
    public string Name;

    /// <summary>
    /// The optional normalized or fully qualified path.
    /// </summary>
    public string? FullPath;

    /// <summary>
    /// The recognized entry type.
    /// </summary>
    public FtpEntryType Type;

    /// <summary>
    /// All normalized and unknown machine-readable facts.
    /// </summary>
    public FtpFact[] Facts;

    /// <summary>
    /// Indicates whether <see cref="Size"/> is available.
    /// </summary>
    public bool HasSize;

    /// <summary>
    /// The file size in octets when <see cref="HasSize"/> is true.
    /// </summary>
    public long Size;

    /// <summary>
    /// The modification timestamp.
    /// </summary>
    public FtpTimestamp Modified;

    /// <summary>
    /// The creation timestamp.
    /// </summary>
    public FtpTimestamp Created;

    /// <summary>
    /// The normalized MLSx permissions.
    /// </summary>
    public FtpPermission[] Permissions;

    /// <summary>
    /// Indicates whether <see cref="UnixMode"/> is available.
    /// </summary>
    public bool HasUnixMode;

    /// <summary>
    /// The Unix-style permission bits when <see cref="HasUnixMode"/> is true.
    /// </summary>
    public int UnixMode;

    /// <summary>
    /// The optional owner name.
    /// </summary>
    public string? Owner;

    /// <summary>
    /// The optional group name.
    /// </summary>
    public string? Group;

    /// <summary>
    /// The optional language tag for the entry name.
    /// </summary>
    public string? Language;

    /// <summary>
    /// The optional media type.
    /// </summary>
    public string? MediaType;

    /// <summary>
    /// The optional server-defined stable unique identifier.
    /// </summary>
    public string? Unique;

    /// <summary>
    /// The optional character-set name.
    /// </summary>
    public string? Charset;

    /// <summary>
    /// The optional symbolic-link target.
    /// </summary>
    public string? LinkTarget;

    /// <summary>
    /// Indicates that the entry is hidden according to the listing source.
    /// </summary>
    public bool Hidden;

    /// <summary>
    /// The original listing line retained for diagnostics.
    /// </summary>
    public string RawLine;

    /// <summary>
    /// Creates an unknown entry with unavailable optional metadata.
    /// </summary>
    public FtpEntry()
    {
        Name = "";
        FullPath = null;
        Type = FtpEntryType.Unknown;
        Facts = new FtpFact[0];
        HasSize = false;
        Size = 0;
        Modified = new FtpTimestamp();
        Created = new FtpTimestamp();
        Permissions = new FtpPermission[0];
        HasUnixMode = false;
        UnixMode = 0;
        Owner = null;
        Group = null;
        Language = null;
        MediaType = null;
        Unique = null;
        Charset = null;
        LinkTarget = null;
        Hidden = false;
        RawLine = "";
    }
}

