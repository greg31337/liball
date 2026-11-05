using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ftp;

/// <summary>
/// Provides FTP file-system operations beneath one existing local directory.
/// </summary>
public class LocalFtpFileSystem : IFtpFileSystem
{
    internal string RootPath;
    internal string RootPrefix;
    internal StringComparison PathComparison;

    /// <summary>
    /// Creates a local provider rooted beneath an existing directory.
    /// </summary>
    /// <param name="rootPath">The existing directory exposed as the FTP root.</param>
    /// <exception cref="System.ArgumentNullException">The root path is null.</exception>
    /// <exception cref="FtpConfigurationException">The root path is empty, invalid, or not an existing directory.</exception>
    public LocalFtpFileSystem(string rootPath)
    {
        if (rootPath == null)
        {
            throw new ArgumentNullException("rootPath");
        }
        if (string.IsNullOrWhiteSpace(rootPath) ||
            FtpConfigurationValidator.HasInvalidText(rootPath))
        {
            throw new FtpConfigurationException(
                "A local FTP root directory is required.", null);
        }

        FtpFaultIsolationResult resolution =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return Path.GetFullPath(rootPath);
            });
        string? resolvedPath = resolution.Value as string;
        if (!resolution.Succeeded || resolvedPath == null ||
            !Directory.Exists(resolvedPath))
        {
            throw new FtpConfigurationException(
                "The local FTP root must be an existing directory.",
                resolution.Failure);
        }

        FtpFaultIsolationResult canonicalization =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return CanonicalizeRootPath(resolvedPath);
            });
        string? canonicalRoot = canonicalization.Value as string;
        if (!canonicalization.Succeeded || canonicalRoot == null ||
            !Directory.Exists(canonicalRoot))
        {
            throw new FtpConfigurationException(
                "The local FTP root could not be canonicalized.",
                canonicalization.Failure);
        }

        RootPath = Path.TrimEndingDirectorySeparator(canonicalRoot);
        RootPrefix = RootPath;
        if (RootPrefix.Length > 0 &&
            RootPrefix[RootPrefix.Length - 1] != Path.DirectorySeparatorChar)
        {
            RootPrefix += Path.DirectorySeparatorChar;
        }
        PathComparison = OperatingSystem.IsWindows() ?
                StringComparison.OrdinalIgnoreCase :
                StringComparison.Ordinal;
    }

    /// <summary>
    /// Resolves local entry metadata.
    /// </summary>
    /// <param name="request">The principal and normalized virtual path.</param>
    /// <returns>The exception-less resolution result.</returns>
    public FtpFileSystemResult ResolveEntry(FtpFileSystemRequest request)
    {
        return RunFileSystemOperation(() =>
        {
            return ResolveEntryCore(request);
        }, "The local FTP entry could not be resolved.");
    }

    /// <summary>
    /// Enumerates a local directory.
    /// </summary>
    /// <param name="request">The principal and normalized virtual directory path.</param>
    /// <returns>The exception-less bounded enumeration result.</returns>
    public FtpFileSystemResult EnumerateDirectory(
        FtpFileSystemRequest request)
    {
        return RunFileSystemOperation(() =>
        {
            return EnumerateDirectoryCore(request);
        }, "The local FTP directory could not be enumerated.");
    }

    /// <summary>
    /// Opens a local file for reading.
    /// </summary>
    /// <param name="request">The path and optional validated offset.</param>
    /// <returns>The exception-less stream-open result.</returns>
    public FtpFileOpenResult OpenFileRead(FtpFileSystemRequest request)
    {
        return RunFileOpenOperation(() =>
        {
            return OpenFileReadCore(request);
        }, "The local FTP file could not be opened for reading.");
    }

    /// <summary>
    /// Opens a local file using the requested write mode.
    /// </summary>
    /// <param name="request">The path, mode, and optional validated offset.</param>
    /// <returns>The exception-less stream-open result.</returns>
    public FtpFileOpenResult OpenFileWrite(FtpFileSystemRequest request)
    {
        return RunFileOpenOperation(() =>
        {
            return OpenFileWriteCore(request);
        }, "The local FTP file could not be opened for writing.");
    }

    /// <summary>
    /// Creates one local directory.
    /// </summary>
    /// <param name="request">The normalized virtual directory path.</param>
    /// <returns>The exception-less creation result.</returns>
    public FtpFileSystemResult CreateDirectoryEntry(
        FtpFileSystemRequest request)
    {
        return RunFileSystemOperation(() =>
        {
            return CreateDirectoryEntryCore(request);
        }, "The local FTP directory could not be created.");
    }

    /// <summary>
    /// Deletes one local file.
    /// </summary>
    /// <param name="request">The normalized virtual file path.</param>
    /// <returns>The exception-less deletion result.</returns>
    public FtpFileSystemResult DeleteFileEntry(
        FtpFileSystemRequest request)
    {
        return RunFileSystemOperation(() =>
        {
            return DeleteFileEntryCore(request);
        }, "The local FTP file could not be deleted.");
    }

    /// <summary>
    /// Deletes one empty local directory.
    /// </summary>
    /// <param name="request">The normalized virtual directory path.</param>
    /// <returns>The exception-less deletion result.</returns>
    public FtpFileSystemResult DeleteDirectoryEntry(
        FtpFileSystemRequest request)
    {
        return RunFileSystemOperation(() =>
        {
            return DeleteDirectoryEntryCore(request);
        }, "The local FTP directory could not be deleted.");
    }

    /// <summary>
    /// Renames or moves one local entry.
    /// </summary>
    /// <param name="request">The normalized source and destination paths.</param>
    /// <returns>The exception-less rename result.</returns>
    public FtpFileSystemResult RenameEntry(FtpFileSystemRequest request)
    {
        return RunFileSystemOperation(() =>
        {
            return RenameEntryCore(request);
        }, "The local FTP entry could not be renamed.");
    }

    /// <summary>
    /// Creates an empty file with a provider-selected unique name.
    /// </summary>
    /// <param name="request">The normalized directory and optional prefix.</param>
    /// <returns>The exception-less unique-entry result.</returns>
    public FtpFileSystemResult CreateUniqueEntry(
        FtpFileSystemRequest request)
    {
        return RunFileSystemOperation(() =>
        {
            return CreateUniqueEntryCore(request);
        }, "A unique local FTP entry could not be created.");
    }

    /// <summary>
    /// Sets the requested local file length.
    /// </summary>
    /// <param name="request">The normalized path and requested length.</param>
    /// <returns>The exception-less allocation result.</returns>
    public FtpFileSystemResult AllocateEntry(FtpFileSystemRequest request)
    {
        return RunFileSystemOperation(() =>
        {
            return AllocateEntryCore(request);
        }, "The local FTP file could not be allocated.");
    }

    /// <summary>
    /// Changes a local entry's modification timestamp.
    /// </summary>
    /// <param name="request">The normalized path and requested UTC timestamp.</param>
    /// <returns>The exception-less metadata-mutation result.</returns>
    public FtpFileSystemResult SetModificationTime(
        FtpFileSystemRequest request)
    {
        return RunFileSystemOperation(() =>
        {
            return SetModificationTimeCore(request);
        }, "The local FTP modification time could not be changed.");
    }

    /// <summary>
    /// Changes a local entry's Unix-style permission bits.
    /// </summary>
    /// <param name="request">The normalized path and requested Unix mode.</param>
    /// <returns>The exception-less metadata-mutation result.</returns>
    public FtpFileSystemResult SetPermissions(
        FtpFileSystemRequest request)
    {
        return RunFileSystemOperation(() =>
        {
            return SetPermissionsCore(request);
        }, "The local FTP permissions could not be changed.");
    }

    /// <summary>
    /// Queries the capacity of the local volume containing an entry.
    /// </summary>
    /// <param name="request">The normalized virtual path.</param>
    /// <returns>The exception-less capacity result.</returns>
    public FtpFileSystemResult QuerySpace(FtpFileSystemRequest request)
    {
        return RunFileSystemOperation(() =>
        {
            return QuerySpaceCore(request);
        }, "Local FTP capacity information is unavailable.");
    }

    /// <summary>
    /// Selects an existing local directory as a structure root.
    /// </summary>
    /// <param name="request">The requested structure and normalized mount path.</param>
    /// <returns>The exception-less mount result.</returns>
    public FtpFileSystemResult MountStructure(
        FtpFileSystemRequest request)
    {
        return RunFileSystemOperation(() =>
        {
            return MountStructureCore(request);
        }, "The local FTP structure could not be mounted.");
    }

    internal FtpFileSystemResult ResolveEntryCore(
        FtpFileSystemRequest request)
    {
        string localPath = ResolveLocalPath(request);
        if (!EntryExists(localPath))
        {
            return CreateFileSystemFailure(
                "The requested local FTP entry does not exist.");
        }
        FtpFileSystemResult result = CreateFileSystemSuccess();
        result.Entry = CreateEntry(localPath, request.Path);
        result.Path = request.Path;
        return result;
    }

    internal FtpFileSystemResult EnumerateDirectoryCore(
        FtpFileSystemRequest request)
    {
        string localPath = ResolveLocalPath(request);
        if (!Directory.Exists(localPath))
        {
            return CreateFileSystemFailure(
                "The requested local FTP directory does not exist.");
        }
        string[] localEntries = Directory.GetFileSystemEntries(localPath);
        if (localEntries.Length > FtpDefaults.MaximumListingEntryCount)
        {
            return CreateFileSystemFailure(
                "The local FTP directory exceeds the listing-entry limit.");
        }
        Array.Sort(localEntries, StringComparer.Ordinal);
        FtpEntry[] entries = new FtpEntry[localEntries.Length];
        int index = 0;
        while (index < localEntries.Length)
        {
            string name = Path.GetFileName(localEntries[index]);
            string virtualPath = CombineVirtualPath(request.Path, name);
            entries[index] = CreateEntry(localEntries[index], virtualPath);
            index++;
        }
        FtpFileSystemResult result = CreateFileSystemSuccess();
        result.Entries = entries;
        result.Path = request.Path;
        result.Entry = CreateEntry(localPath, request.Path);
        return result;
    }

    internal FtpFileOpenResult OpenFileReadCore(
        FtpFileSystemRequest request)
    {
        if (request == null || request.OpenMode != FtpFileOpenMode.Read ||
            !IsKnownStructure(request.Structure))
        {
            return CreateFileOpenFailure(
                "A read request with read mode and a recognized structure is required.");
        }
        if (request.HasOffset && request.Offset < 0)
        {
            return CreateFileOpenFailure(
                "A local FTP read offset cannot be negative.");
        }
        string localPath = ResolveLocalPath(request);
        if (!File.Exists(localPath))
        {
            return CreateFileOpenFailure(
                "The requested local FTP file does not exist.");
        }
        FileStream stream = new FileStream(
            localPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (request.HasOffset)
        {
            if (request.Offset > stream.Length)
            {
                stream.Dispose();
                return CreateFileOpenFailure(
                    "The local FTP read offset exceeds the file length.");
            }
            stream.Seek(request.Offset, SeekOrigin.Begin);
        }
        FtpFileOpenResult result = CreateFileOpenSuccess();
        result.Stream = stream;
        result.Entry = CreateEntry(localPath, request.Path);
        result.HasLength = true;
        result.Length = stream.Length;
        return result;
    }

    internal FtpFileOpenResult OpenFileWriteCore(
        FtpFileSystemRequest request)
    {
        if (request == null || !IsKnownWriteMode(request.OpenMode) ||
            !IsKnownStructure(request.Structure))
        {
            return CreateFileOpenFailure(
                "A request with a write mode and a recognized structure is required.");
        }
        if (request.HasOffset && request.Offset < 0)
        {
            return CreateFileOpenFailure(
                "A local FTP write offset cannot be negative.");
        }
        if (request.OpenMode == FtpFileOpenMode.Resume &&
            !request.HasOffset)
        {
            return CreateFileOpenFailure(
                "Resume mode requires a local FTP write offset.");
        }
        if (request.OpenMode != FtpFileOpenMode.Resume && request.HasOffset)
        {
            return CreateFileOpenFailure(
                "Only resume mode accepts a local FTP write offset.");
        }

        string localPath = ResolveLocalPath(request);
        bool existed = File.Exists(localPath);
        if (Directory.Exists(localPath))
        {
            return CreateFileOpenFailure(
                "The requested local FTP path is a directory.");
        }
        if (!ParentDirectoryExists(localPath))
        {
            return CreateFileOpenFailure(
                "The parent local FTP directory does not exist.");
        }
        if (request.OpenMode == FtpFileOpenMode.Resume &&
            !File.Exists(localPath))
        {
            return CreateFileOpenFailure(
                "The requested local FTP file does not exist.");
        }
        FileMode mode = ResolveWriteMode(request.OpenMode);
        FileStream stream = new FileStream(
            localPath, mode, FileAccess.Write, FileShare.None);
        if (!existed)
        {
            ApplyRequestedUnixMode(request, localPath);
        }
        if (request.OpenMode == FtpFileOpenMode.Resume)
        {
            if (request.Offset > stream.Length)
            {
                stream.Dispose();
                return CreateFileOpenFailure(
                    "The local FTP write offset exceeds the file length.");
            }
            stream.Seek(request.Offset, SeekOrigin.Begin);
        }
        FtpFileOpenResult result = CreateFileOpenSuccess();
        result.Stream = stream;
        result.Entry = CreateEntry(localPath, request.Path);
        result.HasLength = true;
        result.Length = stream.Length;
        return result;
    }

    internal FtpFileSystemResult CreateDirectoryEntryCore(
        FtpFileSystemRequest request)
    {
        string localPath = ResolveLocalPath(request);
        if (EntryExists(localPath))
        {
            return CreateFileSystemFailure(
                "The requested local FTP entry already exists.");
        }
        if (!request.Recursive && !ParentDirectoryExists(localPath))
        {
            return CreateFileSystemFailure(
                "The parent local FTP directory does not exist.");
        }
        Directory.CreateDirectory(localPath);
        ApplyRequestedUnixMode(request, localPath);
        FtpFileSystemResult result = CreateFileSystemSuccess();
        result.Entry = CreateEntry(localPath, request.Path);
        result.Path = request.Path;
        return result;
    }

    internal FtpFileSystemResult DeleteFileEntryCore(
        FtpFileSystemRequest request)
    {
        string localPath = ResolveLocalPath(request);
        if (!File.Exists(localPath))
        {
            return CreateFileSystemFailure(
                "The requested local FTP file does not exist.");
        }
        File.Delete(localPath);
        FtpFileSystemResult result = CreateFileSystemSuccess();
        result.Path = request.Path;
        return result;
    }

    internal FtpFileSystemResult DeleteDirectoryEntryCore(
        FtpFileSystemRequest request)
    {
        string localPath = ResolveLocalPath(request);
        if (string.Equals(localPath, RootPath, PathComparison))
        {
            return CreateFileSystemFailure(
                "The local FTP root directory cannot be deleted.");
        }
        if (!Directory.Exists(localPath))
        {
            return CreateFileSystemFailure(
                "The requested local FTP directory does not exist.");
        }
        Directory.Delete(localPath, request.Recursive);
        FtpFileSystemResult result = CreateFileSystemSuccess();
        result.Path = request.Path;
        return result;
    }

    internal FtpFileSystemResult RenameEntryCore(
        FtpFileSystemRequest request)
    {
        string sourcePath = ResolveLocalPath(request);
        string destinationPath = ResolveDestinationPath(request);
        if (string.Equals(sourcePath, RootPath, PathComparison))
        {
            return CreateFileSystemFailure(
                "The local FTP root directory cannot be renamed.");
        }
        if (!EntryExists(sourcePath))
        {
            return CreateFileSystemFailure(
                "The requested local FTP source entry does not exist.");
        }
        if (EntryExists(destinationPath))
        {
            return CreateFileSystemFailure(
                "The requested local FTP destination already exists.");
        }
        if (!ParentDirectoryExists(destinationPath))
        {
            return CreateFileSystemFailure(
                "The destination local FTP directory does not exist.");
        }
        string sourcePrefix = sourcePath + Path.DirectorySeparatorChar;
        if (Directory.Exists(sourcePath) &&
            destinationPath.StartsWith(sourcePrefix, PathComparison))
        {
            return CreateFileSystemFailure(
                "A local FTP directory cannot be moved beneath itself.");
        }

        if (Directory.Exists(sourcePath))
        {
            Directory.Move(sourcePath, destinationPath);
        }
        else
        {
            File.Move(sourcePath, destinationPath);
        }
        FtpFileSystemResult result = CreateFileSystemSuccess();
        string destinationVirtualPath = request.DestinationPath == null ?
            "" : request.DestinationPath;
        result.Entry = CreateEntry(
            destinationPath, destinationVirtualPath);
        result.Path = destinationVirtualPath;
        return result;
    }

    internal FtpFileSystemResult CreateUniqueEntryCore(
        FtpFileSystemRequest request)
    {
        string directoryPath = ResolveLocalPath(request);
        if (!Directory.Exists(directoryPath))
        {
            return CreateFileSystemFailure(
                "The requested local FTP directory does not exist.");
        }
        string prefix = ValidateUniquePrefix(request.UniquePrefix);
        int attempt = 0;
        while (attempt < 128)
        {
            byte[] randomBytes = RandomNumberGenerator.GetBytes(16);
            string name = prefix +
                Convert.ToHexString(randomBytes).ToLowerInvariant();
            string localPath = Path.Combine(directoryPath, name);
            FtpFaultIsolationResult creation =
                FtpFaultIsolationRunner.RunOperation(() =>
                {
                    FileStream stream = new FileStream(
                        localPath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None);
                    stream.Dispose();
                    return localPath;
                });
            if (creation.Succeeded)
            {
                string virtualPath = CombineVirtualPath(request.Path, name);
                ApplyRequestedUnixMode(request, localPath);
                FtpFileSystemResult result = CreateFileSystemSuccess();
                result.Path = virtualPath;
                result.Entry = CreateEntry(localPath, virtualPath);
                return result;
            }
            attempt++;
        }
        return CreateFileSystemFailure(
            "A unique local FTP entry name could not be selected.");
    }

    internal FtpFileSystemResult AllocateEntryCore(
        FtpFileSystemRequest request)
    {
        if (request == null || !request.HasLength || request.Length < 0)
        {
            return CreateFileSystemFailure(
                "A non-negative local FTP allocation length is required.");
        }
        string localPath = ResolveLocalPath(request);
        if (Directory.Exists(localPath))
        {
            return CreateFileSystemFailure(
                "The requested local FTP path is a directory.");
        }
        if (!ParentDirectoryExists(localPath))
        {
            return CreateFileSystemFailure(
                "The parent local FTP directory does not exist.");
        }
        FileStream stream = new FileStream(
            localPath,
            FileMode.OpenOrCreate,
            FileAccess.Write,
            FileShare.None);
        if (stream.Length < request.Length)
        {
            stream.SetLength(request.Length);
        }
        long allocatedBytes = stream.Length;
        stream.Dispose();
        FtpFileSystemResult result = CreateFileSystemSuccess();
        result.Path = request.Path;
        result.Entry = CreateEntry(localPath, request.Path);
        result.HasAllocatedBytes = true;
        result.AllocatedBytes = allocatedBytes;
        return result;
    }

    internal FtpFileSystemResult SetModificationTimeCore(
        FtpFileSystemRequest request)
    {
        if (request == null || request.ModificationTime == null ||
            !request.ModificationTime.HasValue ||
            request.ModificationTime.ValueUtc.Kind != DateTimeKind.Utc)
        {
            return CreateFileSystemFailure(
                "A valid UTC local FTP modification time is required.");
        }
        string localPath = ResolveLocalPath(request);
        if (!EntryExists(localPath))
        {
            return CreateFileSystemFailure(
                "The requested local FTP entry does not exist.");
        }
        if (Directory.Exists(localPath))
        {
            Directory.SetLastWriteTimeUtc(
                localPath, request.ModificationTime.ValueUtc);
        }
        else
        {
            File.SetLastWriteTimeUtc(
                localPath, request.ModificationTime.ValueUtc);
        }
        FtpFileSystemResult result = CreateFileSystemSuccess();
        result.Entry = CreateEntry(localPath, request.Path);
        result.Path = request.Path;
        return result;
    }

    internal FtpFileSystemResult SetPermissionsCore(
        FtpFileSystemRequest request)
    {
        if (request == null || !request.HasUnixMode ||
            request.UnixMode < 0 || request.UnixMode > 4095)
        {
            return CreateFileSystemFailure(
                "A Unix mode between 0000 and 07777 is required.");
        }
        if (OperatingSystem.IsWindows())
        {
            return CreateFileSystemFailure(
                "Unix-style permissions are unavailable on this platform.");
        }
        string localPath = ResolveLocalPath(request);
        if (!EntryExists(localPath))
        {
            return CreateFileSystemFailure(
                "The requested local FTP entry does not exist.");
        }
        File.SetUnixFileMode(localPath, (UnixFileMode)request.UnixMode);
        FtpFileSystemResult result = CreateFileSystemSuccess();
        result.Entry = CreateEntry(localPath, request.Path);
        result.Path = request.Path;
        return result;
    }

    internal void ApplyRequestedUnixMode(
        FtpFileSystemRequest request,
        string localPath)
    {
        if (!request.HasUnixMode)
        {
            return;
        }
        if (request.UnixMode < 0 || request.UnixMode > 4095)
        {
            throw new FtpProtocolException(
                "A Unix mode between 0000 and 07777 is required.", null);
        }
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(localPath, (UnixFileMode)request.UnixMode);
        }
    }

    internal FtpFileSystemResult QuerySpaceCore(
        FtpFileSystemRequest request)
    {
        string localPath = ResolveLocalPath(request);
        string existingPath = FindExistingAncestor(localPath);
        string? volumeRoot = Path.GetPathRoot(existingPath);
        if (string.IsNullOrEmpty(volumeRoot))
        {
            return CreateFileSystemFailure(
                "The local FTP volume could not be identified.");
        }
        DriveInfo drive = new DriveInfo(volumeRoot);
        long totalBytes = drive.TotalSize;
        long availableBytes = drive.AvailableFreeSpace;
        long usedBytes = totalBytes - availableBytes;
        FtpSpaceInfo space = new FtpSpaceInfo();
        FtpResourceLimits limits = request.Principal.ResourceLimits;
        if (limits.HasStorageQuotaBytes)
        {
            string quotaRoot = ResolveQuotaRoot(request);
            usedBytes = CalculateStoredByteCount(quotaRoot);
            totalBytes = limits.StorageQuotaBytes;
            long quotaAvailable = usedBytes >= totalBytes ?
                0 : totalBytes - usedBytes;
            if (quotaAvailable < availableBytes)
            {
                availableBytes = quotaAvailable;
            }
            space.QuotaName = request.Principal.Name;
        }
        space.HasTotalBytes = true;
        space.TotalBytes = totalBytes;
        space.HasAvailableBytes = true;
        space.AvailableBytes = availableBytes;
        space.HasUsedBytes = true;
        space.UsedBytes = usedBytes;
        FtpFileSystemResult result = CreateFileSystemSuccess();
        result.Path = request.Path;
        result.Space = space;
        return result;
    }

    internal string ResolveQuotaRoot(FtpFileSystemRequest request)
    {
        if (request.Principal.HomeDirectory == null)
        {
            return RootPath;
        }
        FtpFileSystemRequest homeRequest = new FtpFileSystemRequest();
        homeRequest.Principal = request.Principal;
        homeRequest.Path = request.Principal.HomeDirectory;
        return ResolveLocalPath(homeRequest);
    }

    internal long CalculateStoredByteCount(string quotaRoot)
    {
        if (!Directory.Exists(quotaRoot))
        {
            return 0;
        }
        Stack<string> pending = new Stack<string>();
        pending.Push(quotaRoot);
        long byteCount = 0;
        while (pending.Count != 0)
        {
            string directory = pending.Pop();
            string[] entries = Directory.GetFileSystemEntries(directory);
            int index = 0;
            while (index < entries.Length)
            {
                string entry = entries[index];
                FileAttributes attributes = File.GetAttributes(entry);
                bool reparsePoint =
                    (attributes & FileAttributes.ReparsePoint) != 0;
                bool directoryEntry =
                    (attributes & FileAttributes.Directory) != 0;
                if (!reparsePoint && directoryEntry)
                {
                    pending.Push(entry);
                }
                else if (!reparsePoint)
                {
                    long length = new FileInfo(entry).Length;
                    if (byteCount > long.MaxValue - length)
                    {
                        throw new IOException(
                            "The local FTP storage usage exceeds Int64 capacity.");
                    }
                    byteCount += length;
                }
                index++;
            }
        }
        return byteCount;
    }

    internal FtpFileSystemResult MountStructureCore(
        FtpFileSystemRequest request)
    {
        if (request == null || !IsKnownStructure(request.Structure))
        {
            return CreateFileSystemFailure(
                "A recognized FTP file structure is required.");
        }
        string localPath = ResolveLocalPath(request);
        if (!Directory.Exists(localPath))
        {
            return CreateFileSystemFailure(
                "The requested local FTP mount directory does not exist.");
        }
        FtpFileSystemResult result = CreateFileSystemSuccess();
        result.Path = request.Path;
        result.Entry = CreateEntry(localPath, request.Path);
        return result;
    }

    internal FtpFileSystemResult RunFileSystemOperation(
        Func<FtpFileSystemResult> operation,
        string failureMessage)
    {
        FtpFaultIsolationResult invocation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return operation();
            });
        FtpFileSystemResult? result =
            invocation.Value as FtpFileSystemResult;
        if (!invocation.Succeeded || result == null)
        {
            return CreateFileSystemFailure(failureMessage);
        }
        return result;
    }

    internal FtpFileOpenResult RunFileOpenOperation(
        Func<FtpFileOpenResult> operation,
        string failureMessage)
    {
        FtpFaultIsolationResult invocation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return operation();
            });
        FtpFileOpenResult? result = invocation.Value as FtpFileOpenResult;
        if (!invocation.Succeeded || result == null)
        {
            return CreateFileOpenFailure(failureMessage);
        }
        return result;
    }

    internal string ResolveLocalPath(FtpFileSystemRequest request)
    {
        ValidateVirtualPath(request);
        string relativePath = request.Path.Length == 1 ?
            "" : request.Path.Substring(1);
        if (Path.DirectorySeparatorChar != '/')
        {
            relativePath = relativePath.Replace(
                '/', Path.DirectorySeparatorChar);
        }
        string localPath = Path.GetFullPath(
            Path.Combine(RootPath, relativePath));
        RequireContainedPath(localPath);
        RequireNoReparseTraversal(localPath);
        return localPath;
    }

    internal string ResolveDestinationPath(FtpFileSystemRequest request)
    {
        if (request == null ||
            string.IsNullOrEmpty(request.DestinationPath))
        {
            throw new FtpProtocolException(
                "A normalized FTP destination path is required.", null);
        }
        FtpFileSystemRequest destination = new FtpFileSystemRequest();
        destination.Path = request.DestinationPath;
        return ResolveLocalPath(destination);
    }

    internal void RequireContainedPath(string localPath)
    {
        if (!string.Equals(localPath, RootPath, PathComparison) &&
            !localPath.StartsWith(RootPrefix, PathComparison))
        {
            throw new FtpProtocolException(
                "The local FTP path is outside the configured root.", null);
        }
    }

    internal void ValidateVirtualPath(FtpFileSystemRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Path) ||
            request.Path[0] != '/' ||
            FtpConfigurationValidator.HasInvalidText(request.Path) ||
            request.Path.IndexOf('\\') >= 0 ||
            request.Path.IndexOf(':') >= 0 ||
            (request.Path.Length > 1 && request.Path[1] == '/') ||
            (request.Path.Length > 1 &&
             request.Path[request.Path.Length - 1] == '/'))
        {
            throw new FtpProtocolException(
                "A canonical absolute FTP path is required.", null);
        }
        if (request.Path == "/")
        {
            return;
        }

        string[] segments = request.Path.Split('/');
        int index = 1;
        while (index < segments.Length)
        {
            string segment = segments[index];
            if (segment.Length == 0 || segment == "." || segment == "..")
            {
                throw new FtpProtocolException(
                    "The FTP path contains a non-canonical segment.", null);
            }
            index++;
        }
    }

    internal void RequireNoReparseTraversal(string localPath)
    {
        if (string.Equals(localPath, RootPath, PathComparison))
        {
            return;
        }
        string relativePath = localPath.Substring(RootPrefix.Length);
        string[] segments = relativePath.Split(Path.DirectorySeparatorChar);
        string candidate = RootPath;
        int index = 0;
        while (index < segments.Length)
        {
            candidate = Path.Combine(candidate, segments[index]);
            if (!PathEntryExists(candidate))
            {
                return;
            }
            FileAttributes attributes = File.GetAttributes(candidate);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new FtpProtocolException(
                    "The local FTP path cannot traverse a symbolic link or reparse point.",
                    null);
            }
            index++;
        }
    }

    internal static string CanonicalizeRootPath(string rootPath)
    {
        string? volumeRoot = Path.GetPathRoot(rootPath);
        if (string.IsNullOrEmpty(volumeRoot))
        {
            throw new FtpConfigurationException(
                "The local FTP root must be fully qualified.", null);
        }
        string relativePath = rootPath.Substring(volumeRoot.Length);
        string[] segments = relativePath.Split(
            Path.DirectorySeparatorChar,
            StringSplitOptions.RemoveEmptyEntries);
        string candidate = volumeRoot;
        int index = 0;
        while (index < segments.Length)
        {
            candidate = Path.Combine(candidate, segments[index]);
            FileAttributes attributes = File.GetAttributes(candidate);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                FileSystemInfo link =
                    (attributes & FileAttributes.Directory) != 0 ?
                        new DirectoryInfo(candidate) :
                        new FileInfo(candidate);
                FileSystemInfo? target = link.ResolveLinkTarget(true);
                if (target == null)
                {
                    throw new FtpConfigurationException(
                        "The local FTP root contains an unresolved symbolic link.",
                        null);
                }
                candidate = CanonicalizeRootPath(
                    Path.GetFullPath(target.FullName));
            }
            index++;
        }
        return candidate;
    }

    internal static bool PathEntryExists(string localPath)
    {
        if (EntryExists(localPath))
        {
            return true;
        }
        FileSystemInfo info = new FileInfo(localPath);
        return info.LinkTarget != null;
    }

    internal FtpEntry CreateEntry(
        string localPath,
        string virtualPath)
    {
        FileAttributes attributes = File.GetAttributes(localPath);
        bool directory =
            (attributes & FileAttributes.Directory) != 0;
        bool symbolicLink =
            (attributes & FileAttributes.ReparsePoint) != 0;
        FtpEntry entry = new FtpEntry();
        entry.Name = virtualPath == "/" ?
            "/" : Path.GetFileName(localPath);
        entry.FullPath = virtualPath;
        entry.Type = symbolicLink ? FtpEntryType.SymbolicLink :
            directory ? FtpEntryType.Directory : FtpEntryType.File;
        if (!directory && !symbolicLink)
        {
            FileInfo file = new FileInfo(localPath);
            entry.HasSize = true;
            entry.Size = file.Length;
        }
        if (!symbolicLink)
        {
            DateTime modifiedUtc = File.GetLastWriteTimeUtc(localPath);
            if (IsAvailableTimestamp(modifiedUtc))
            {
                entry.Modified = CreateLocalTimestamp(modifiedUtc);
            }
            DateTime createdUtc = File.GetCreationTimeUtc(localPath);
            entry.Unique = CreateLocalUnique(localPath, createdUtc);
            if (IsAvailableTimestamp(createdUtc))
            {
                entry.Created = CreateLocalTimestamp(createdUtc);
            }
        }
        ApplyLocalMode(entry, localPath, attributes);
        entry.Permissions = CreateLocalPermissions(
            entry, localPath, attributes);
        entry.MediaType = ResolveMediaType(entry, localPath);
        entry.Hidden =
            (attributes & FileAttributes.Hidden) != 0 ||
            (entry.Name.Length > 1 && entry.Name[0] == '.');
        entry.Facts = CreateLocalFacts(entry);
        entry.RawLine = FtpMlsxCodec.FormatEntry(entry);
        return entry;
    }

    internal string CreateLocalUnique(
        string localPath,
        DateTime createdUtc)
    {
        string identity = localPath + "\0" +
            createdUtc.Ticks.ToString(CultureInfo.InvariantCulture);
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        return Convert.ToHexString(digest).ToLowerInvariant();
    }

    internal void ApplyLocalMode(
        FtpEntry entry,
        string localPath,
        FileAttributes attributes)
    {
        if (entry.Type == FtpEntryType.SymbolicLink ||
            OperatingSystem.IsWindows())
        {
            return;
        }
        UnixFileMode mode = File.GetUnixFileMode(localPath);
        entry.HasUnixMode = true;
        entry.UnixMode = (int)mode;
    }

    internal FtpPermission[] CreateLocalPermissions(
        FtpEntry entry,
        string localPath,
        FileAttributes attributes)
    {
        List<FtpPermission> permissions = new List<FtpPermission>();
        if (entry.Type == FtpEntryType.SymbolicLink)
        {
            return permissions.ToArray();
        }

        bool read;
        bool write;
        bool execute;
        if (entry.HasUnixMode)
        {
            UnixFileMode mode = (UnixFileMode)entry.UnixMode;
            read = HasAnyReadMode(mode);
            write = HasAnyWriteMode(mode);
            execute = HasAnyExecuteMode(mode);
        }
        else
        {
            read = true;
            write = (attributes & FileAttributes.ReadOnly) == 0;
            execute = entry.Type == FtpEntryType.Directory;
        }

        if (entry.Type == FtpEntryType.Directory)
        {
            if (write && execute)
            {
                permissions.Add(FtpPermission.CreateFile);
                permissions.Add(FtpPermission.DeleteFile);
                permissions.Add(FtpPermission.Rename);
                permissions.Add(FtpPermission.CreateDirectory);
                permissions.Add(FtpPermission.DeleteDirectory);
            }
            if (execute)
            {
                permissions.Add(FtpPermission.EnterDirectory);
            }
            if (read)
            {
                permissions.Add(FtpPermission.List);
            }
            return SortPermissions(permissions);
        }

        if (write)
        {
            permissions.Add(FtpPermission.Append);
            permissions.Add(FtpPermission.Write);
        }
        if (CanModifyParentDirectory(localPath))
        {
            permissions.Add(FtpPermission.DeleteFile);
            permissions.Add(FtpPermission.Rename);
        }
        if (read)
        {
            permissions.Add(FtpPermission.Read);
        }
        return SortPermissions(permissions);
    }

    internal bool CanModifyParentDirectory(string localPath)
    {
        string? parentPath = Path.GetDirectoryName(localPath);
        if (string.IsNullOrEmpty(parentPath))
        {
            return false;
        }
        FileAttributes attributes = File.GetAttributes(parentPath);
        if ((attributes & FileAttributes.ReadOnly) != 0)
        {
            return false;
        }
        if (OperatingSystem.IsWindows())
        {
            return true;
        }
        UnixFileMode mode = File.GetUnixFileMode(parentPath);
        return HasAnyWriteMode(mode) && HasAnyExecuteMode(mode);
    }

    internal FtpFact[] CreateLocalFacts(FtpEntry entry)
    {
        List<FtpFact> facts = new List<FtpFact>();
        facts.Add(CreateLocalFact(
            FtpFactNames.Type, ResolveFactType(entry.Type)));
        if (entry.HasSize)
        {
            facts.Add(CreateLocalFact(
                FtpFactNames.Size,
                entry.Size.ToString(CultureInfo.InvariantCulture)));
        }
        if (entry.Modified.HasValue)
        {
            facts.Add(CreateLocalFact(
                FtpFactNames.Modify,
                FtpTimestampCodec.FormatTimestampValue(entry.Modified)));
        }
        if (entry.Created.HasValue)
        {
            facts.Add(CreateLocalFact(
                FtpFactNames.Create,
                FtpTimestampCodec.FormatTimestampValue(entry.Created)));
        }
        if (entry.Unique != null)
        {
            facts.Add(CreateLocalFact(FtpFactNames.Unique, entry.Unique));
        }
        if (entry.Permissions.Length > 0)
        {
            facts.Add(CreateLocalFact(
                FtpFactNames.Perm,
                CreatePermissionFactValue(entry.Permissions)));
        }
        if (entry.Language != null)
        {
            facts.Add(CreateLocalFact(FtpFactNames.Lang, entry.Language));
        }
        if (entry.MediaType != null)
        {
            facts.Add(CreateLocalFact(
                FtpFactNames.MediaType, entry.MediaType));
        }
        if (entry.Charset != null)
        {
            facts.Add(CreateLocalFact(
                FtpFactNames.Charset, entry.Charset));
        }
        return facts.ToArray();
    }

    internal static FtpFact CreateLocalFact(string name, string value)
    {
        FtpFact fact = new FtpFact();
        fact.Name = name;
        fact.Value = value;
        fact.RawText = name + "=" + value;
        fact.Standard = true;
        return fact;
    }

    internal static FtpTimestamp CreateLocalTimestamp(DateTime valueUtc)
    {
        DateTime normalizedUtc = valueUtc.Kind == DateTimeKind.Utc ?
            valueUtc : valueUtc.ToUniversalTime();
        FtpTimestamp timestamp = new FtpTimestamp();
        timestamp.HasValue = true;
        timestamp.ValueUtc = normalizedUtc;
        long fractionalTicks = normalizedUtc.Ticks %
            TimeSpan.TicksPerSecond;
        if (fractionalTicks != 0)
        {
            string fraction = fractionalTicks.ToString(
                "D7", CultureInfo.InvariantCulture);
            int length = fraction.Length;
            while (length > 0 && fraction[length - 1] == '0')
            {
                length--;
            }
            timestamp.FractionalSeconds = fraction.Substring(0, length);
        }
        timestamp.RawValue =
            FtpTimestampCodec.FormatTimestampValue(timestamp);
        return timestamp;
    }

    internal static bool IsAvailableTimestamp(DateTime valueUtc)
    {
        return valueUtc.Kind == DateTimeKind.Utc &&
            valueUtc.Year >= 1000 && valueUtc.Year <= 9999;
    }

    internal static bool HasAnyReadMode(UnixFileMode mode)
    {
        UnixFileMode readModes = UnixFileMode.UserRead |
            UnixFileMode.GroupRead | UnixFileMode.OtherRead;
        return (mode & readModes) != 0;
    }

    internal static bool HasAnyWriteMode(UnixFileMode mode)
    {
        UnixFileMode writeModes = UnixFileMode.UserWrite |
            UnixFileMode.GroupWrite | UnixFileMode.OtherWrite;
        return (mode & writeModes) != 0;
    }

    internal static bool HasAnyExecuteMode(UnixFileMode mode)
    {
        UnixFileMode executeModes = UnixFileMode.UserExecute |
            UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
        return (mode & executeModes) != 0;
    }

    internal static FtpPermission[] SortPermissions(
        List<FtpPermission> permissions)
    {
        FtpPermission[] values = permissions.ToArray();
        Array.Sort(values);
        return values;
    }

    internal static string ResolveFactType(FtpEntryType type)
    {
        switch (type)
        {
            case FtpEntryType.File:
                return "file";
            case FtpEntryType.Directory:
                return "dir";
            case FtpEntryType.CurrentDirectory:
                return "cdir";
            case FtpEntryType.ParentDirectory:
                return "pdir";
            case FtpEntryType.SymbolicLink:
                return OperatingSystem.IsWindows() ?
                    "OS.windows=reparse" : "OS.unix=slink";
            default:
                return "OS.local=unknown";
        }
    }

    internal static string CreatePermissionFactValue(
        FtpPermission[] permissions)
    {
        StringBuilder value = new StringBuilder();
        int index = 0;
        while (index < permissions.Length)
        {
            switch (permissions[index])
            {
                case FtpPermission.Append: value.Append('a'); break;
                case FtpPermission.CreateFile: value.Append('c'); break;
                case FtpPermission.DeleteFile: value.Append('d'); break;
                case FtpPermission.EnterDirectory: value.Append('e'); break;
                case FtpPermission.Rename: value.Append('f'); break;
                case FtpPermission.List: value.Append('l'); break;
                case FtpPermission.CreateDirectory: value.Append('m'); break;
                case FtpPermission.DeleteDirectory: value.Append('p'); break;
                case FtpPermission.Read: value.Append('r'); break;
                case FtpPermission.Write: value.Append('w'); break;
                default: break;
            }
            index++;
        }
        return value.ToString();
    }

    internal static string? ResolveMediaType(
        FtpEntry entry,
        string localPath)
    {
        if (entry.Type != FtpEntryType.File)
        {
            return null;
        }
        string extension = Path.GetExtension(localPath).ToLowerInvariant();
        switch (extension)
        {
            case ".txt":
            case ".log":
                return "text/plain";
            case ".csv":
                return "text/csv";
            case ".html":
            case ".htm":
                return "text/html";
            case ".css":
                return "text/css";
            case ".js":
                return "text/javascript";
            case ".json":
                return "application/json";
            case ".xml":
                return "application/xml";
            case ".pdf":
                return "application/pdf";
            case ".zip":
                return "application/zip";
            case ".gz":
                return "application/gzip";
            case ".tar":
                return "application/x-tar";
            case ".png":
                return "image/png";
            case ".jpg":
            case ".jpeg":
                return "image/jpeg";
            case ".gif":
                return "image/gif";
            case ".svg":
                return "image/svg+xml";
            case ".mp3":
                return "audio/mpeg";
            case ".mp4":
                return "video/mp4";
            default:
                return null;
        }
    }

    internal string FindExistingAncestor(string localPath)
    {
        string candidate = localPath;
        while (!EntryExists(candidate))
        {
            string? parent = Path.GetDirectoryName(candidate);
            if (string.IsNullOrEmpty(parent))
            {
                return RootPath;
            }
            candidate = parent;
        }
        return candidate;
    }

    internal string ValidateUniquePrefix(string? prefix)
    {
        if (prefix == null)
        {
            return "ftp-";
        }
        if (prefix.Length == 0 || prefix == "." || prefix == ".." ||
            prefix.IndexOf('/') >= 0 || prefix.IndexOf('\\') >= 0 ||
            FtpConfigurationValidator.HasInvalidText(prefix))
        {
            throw new FtpProtocolException(
                "The local FTP unique-name prefix is invalid.", null);
        }
        char[] invalidCharacters = Path.GetInvalidFileNameChars();
        int prefixIndex = 0;
        while (prefixIndex < prefix.Length)
        {
            int invalidIndex = 0;
            while (invalidIndex < invalidCharacters.Length)
            {
                if (prefix[prefixIndex] == invalidCharacters[invalidIndex])
                {
                    throw new FtpProtocolException(
                        "The local FTP unique-name prefix is invalid.", null);
                }
                invalidIndex++;
            }
            prefixIndex++;
        }
        return prefix;
    }

    internal static bool ParentDirectoryExists(string localPath)
    {
        string? parentPath = Path.GetDirectoryName(localPath);
        return !string.IsNullOrEmpty(parentPath) &&
            Directory.Exists(parentPath);
    }

    internal static bool IsKnownWriteMode(FtpFileOpenMode openMode)
    {
        return openMode == FtpFileOpenMode.CreateNew ||
            openMode == FtpFileOpenMode.Overwrite ||
            openMode == FtpFileOpenMode.Append ||
            openMode == FtpFileOpenMode.Resume;
    }

    internal static FileMode ResolveWriteMode(FtpFileOpenMode openMode)
    {
        switch (openMode)
        {
            case FtpFileOpenMode.CreateNew:
                return FileMode.CreateNew;
            case FtpFileOpenMode.Overwrite:
                return FileMode.Create;
            case FtpFileOpenMode.Append:
                return FileMode.Append;
            case FtpFileOpenMode.Resume:
                return FileMode.Open;
            default:
                throw new FtpProtocolException(
                    "The local FTP write mode is invalid.", null);
        }
    }

    internal static bool IsKnownStructure(FtpFileStructure structure)
    {
        return structure == FtpFileStructure.File ||
            structure == FtpFileStructure.Record ||
            structure == FtpFileStructure.Page;
    }

    internal static bool EntryExists(string localPath)
    {
        return File.Exists(localPath) || Directory.Exists(localPath);
    }

    internal static string CombineVirtualPath(
        string directoryPath,
        string name)
    {
        return directoryPath == "/" ?
            "/" + name : directoryPath + "/" + name;
    }

    internal static FtpFileSystemResult CreateFileSystemSuccess()
    {
        FtpFileSystemResult result = new FtpFileSystemResult();
        result.Succeeded = true;
        return result;
    }

    internal static FtpFileSystemResult CreateFileSystemFailure(
        string message)
    {
        FtpFileSystemResult result = new FtpFileSystemResult();
        result.FailureMessage = message;
        return result;
    }

    internal static FtpFileOpenResult CreateFileOpenSuccess()
    {
        FtpFileOpenResult result = new FtpFileOpenResult();
        result.Succeeded = true;
        result.LeaveStreamOpen = false;
        return result;
    }

    internal static FtpFileOpenResult CreateFileOpenFailure(
        string message)
    {
        FtpFileOpenResult result = new FtpFileOpenResult();
        result.FailureMessage = message;
        return result;
    }
}
