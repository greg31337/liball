namespace uspto;

/// <summary>
/// Reports archive detection, listing, validation, or extraction failure.
/// </summary>
public class ArchiveException : global::System.Exception
{
    /// <summary>The detected or selected compression format.</summary>
    public CompressionFormat Format;

    /// <summary>The local archive path, when applicable.</summary>
    public string? ArchivePath;

    /// <summary>The archive entry associated with the failure, when applicable.</summary>
    public string? EntryName;

    /// <summary>Initializes an archive exception and its safe entry context.</summary>
    /// <param name="message">The archive failure message.</param>
    /// <param name="format">The detected or selected compression format.</param>
    /// <param name="archivePath">The local archive path.</param>
    /// <param name="entryName">The archive entry associated with the failure.</param>
    /// <param name="innerException">The lower-level cause, or <c>null</c>.</param>
    public ArchiveException(string message, CompressionFormat format, string? archivePath, string? entryName, global::System.Exception? innerException)
        : base(message, innerException)
    {
        Format = format;
        ArchivePath = archivePath;
        EntryName = entryName;
    }
}
