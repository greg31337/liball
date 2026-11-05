namespace ftp;

/// <summary>
/// Defines virtual FTP file-system operations.
/// </summary>
public interface IFtpFileSystem
{
    /// <summary>
    /// Resolves entry metadata.
    /// </summary>
    /// <param name="request">The principal and normalized path.</param>
    /// <returns>The resolved entry result.</returns>
    FtpFileSystemResult ResolveEntry(FtpFileSystemRequest request);

    /// <summary>
    /// Enumerates a directory.
    /// </summary>
    /// <param name="request">The principal and normalized directory path.</param>
    /// <returns>The bounded entry result.</returns>
    FtpFileSystemResult EnumerateDirectory(FtpFileSystemRequest request);

    /// <summary>
    /// Opens a file for reading.
    /// </summary>
    /// <param name="request">The principal, path, structure, and optional offset.</param>
    /// <returns>The stream-open result.</returns>
    FtpFileOpenResult OpenFileRead(FtpFileSystemRequest request);

    /// <summary>
    /// Opens a file for writing.
    /// </summary>
    /// <param name="request">The principal, path, mode, structure, and optional offset.</param>
    /// <returns>The stream-open result.</returns>
    FtpFileOpenResult OpenFileWrite(FtpFileSystemRequest request);

    /// <summary>
    /// Creates a directory entry.
    /// </summary>
    /// <param name="request">The principal and normalized path.</param>
    /// <returns>The creation result.</returns>
    FtpFileSystemResult CreateDirectoryEntry(FtpFileSystemRequest request);

    /// <summary>
    /// Deletes a file entry.
    /// </summary>
    /// <param name="request">The principal and normalized path.</param>
    /// <returns>The deletion result.</returns>
    FtpFileSystemResult DeleteFileEntry(FtpFileSystemRequest request);

    /// <summary>
    /// Deletes an empty directory entry.
    /// </summary>
    /// <param name="request">The principal and normalized path.</param>
    /// <returns>The deletion result.</returns>
    FtpFileSystemResult DeleteDirectoryEntry(FtpFileSystemRequest request);

    /// <summary>
    /// Renames or moves an entry.
    /// </summary>
    /// <param name="request">The principal and normalized source and destination paths.</param>
    /// <returns>The rename result.</returns>
    FtpFileSystemResult RenameEntry(FtpFileSystemRequest request);

    /// <summary>
    /// Creates a provider-selected unique entry.
    /// </summary>
    /// <param name="request">The principal, normalized directory, and optional prefix.</param>
    /// <returns>The unique path and entry result.</returns>
    FtpFileSystemResult CreateUniqueEntry(FtpFileSystemRequest request);

    /// <summary>
    /// Allocates storage for an entry.
    /// </summary>
    /// <param name="request">The principal, path, and requested length.</param>
    /// <returns>The allocation result.</returns>
    FtpFileSystemResult AllocateEntry(FtpFileSystemRequest request);

    /// <summary>
    /// Changes an entry's modification timestamp.
    /// </summary>
    /// <param name="request">The principal, normalized path, and requested timestamp.</param>
    /// <returns>The metadata-mutation result with the stored entry metadata.</returns>
    FtpFileSystemResult SetModificationTime(FtpFileSystemRequest request);

    /// <summary>
    /// Changes an entry's Unix-style permission bits.
    /// </summary>
    /// <param name="request">The principal, normalized path, and requested Unix mode.</param>
    /// <returns>The metadata-mutation result with the stored entry metadata.</returns>
    FtpFileSystemResult SetPermissions(FtpFileSystemRequest request);

    /// <summary>
    /// Queries capacity and quota information.
    /// </summary>
    /// <param name="request">The principal and normalized path.</param>
    /// <returns>The capacity result.</returns>
    FtpFileSystemResult QuerySpace(FtpFileSystemRequest request);

    /// <summary>
    /// Mounts or selects an FTP structure root.
    /// </summary>
    /// <param name="request">The principal, structure, and normalized mount path.</param>
    /// <returns>The mount result.</returns>
    FtpFileSystemResult MountStructure(FtpFileSystemRequest request);
}
