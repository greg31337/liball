namespace ftp;

/// <summary>
/// Identifies an operation presented to an FTP authorization provider.
/// </summary>
public enum FtpAuthorizationOperation
{
    /// <summary>
    /// Authorizes directory listing.
    /// </summary>
    List = 0,
    /// <summary>
    /// Authorizes reading file content.
    /// </summary>
    Read = 1,
    /// <summary>
    /// Authorizes writing or replacing file content.
    /// </summary>
    Write = 2,
    /// <summary>
    /// Authorizes appending file content.
    /// </summary>
    Append = 3,
    /// <summary>
    /// Authorizes directory creation.
    /// </summary>
    CreateDirectory = 4,
    /// <summary>
    /// Authorizes file deletion.
    /// </summary>
    DeleteFile = 5,
    /// <summary>
    /// Authorizes directory deletion.
    /// </summary>
    DeleteDirectory = 6,
    /// <summary>
    /// Authorizes renaming or moving an entry.
    /// </summary>
    Rename = 7,
    /// <summary>
    /// Authorizes reading entry metadata.
    /// </summary>
    ReadMetadata = 8,
    /// <summary>
    /// Authorizes changing entry metadata.
    /// </summary>
    WriteMetadata = 9,
    /// <summary>
    /// Authorizes a SITE extension operation.
    /// </summary>
    Site = 10,
    /// <summary>
    /// Authorizes a server-to-server transfer.
    /// </summary>
    ServerToServer = 11
}
