using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace tftp;

/// <summary>
/// Provides root-confined directory storage for a TFTP server.
/// </summary>
public class TftpDirectoryStorage : ITftpServerStorage
{
    internal TftpDirectoryStorageOptions Options;
    internal TftpPathValidator PathValidator;
    internal Dictionary<object, TftpPendingWrite> PendingWrites;

    /// <summary>
    /// Initializes directory storage with access and path-safety settings.
    /// </summary>
    /// <param name="options">The directory storage settings.</param>
    public TftpDirectoryStorage(TftpDirectoryStorageOptions options)
    {
        if (options == null)
        {
            throw new TftpStorageException(
                "The directory storage options cannot be null.",
                "CreateDirectoryStorage",
                null);
        }

        TftpDirectoryStorageOptions optionsSnapshot =
            new TftpDirectoryStorageOptions();
        optionsSnapshot.RootPath = options.RootPath;
        optionsSnapshot.AllowRead = options.AllowRead;
        optionsSnapshot.AllowWrite = options.AllowWrite;
        optionsSnapshot.AllowCreate = options.AllowCreate;
        optionsSnapshot.AllowOverwrite = options.AllowOverwrite;
        optionsSnapshot.AllowSubdirectories = options.AllowSubdirectories;
        optionsSnapshot.RejectSymbolicLinks = options.RejectSymbolicLinks;
        TftpPathValidator pathValidator = new TftpPathValidator(
            optionsSnapshot);
        if (!Directory.Exists(pathValidator.RootPath))
        {
            throw new TftpStorageException(
                "The directory storage root must be an existing directory.",
                "CreateDirectoryStorage",
                null);
        }

        optionsSnapshot.RootPath = pathValidator.RootPath;
        Options = optionsSnapshot;
        PathValidator = pathValidator;
        PendingWrites = new Dictionary<object, TftpPendingWrite>();
    }

    /// <summary>
    /// Opens root-confined content for a read request.
    /// </summary>
    /// <param name="request">The accepted read request.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The storage open result.</returns>
    public TftpStorageOpenResult OpenRead(
        TftpServerRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request == null)
        {
            return CreateReadFailure(
                TftpErrorCode.AccessViolation,
                "The storage read request cannot be null.");
        }

        if (request.Direction != TftpTransferDirection.Read)
        {
            return CreateReadFailure(
                TftpErrorCode.AccessViolation,
                "The storage request is not a read request.");
        }

        if (!Options.AllowRead)
        {
            return CreateReadFailure(
                TftpErrorCode.AccessViolation,
                "Reading from this storage provider is not permitted.");
        }

        TftpPathValidationResult pathResult =
            PathValidator.ValidatePath(request.Filename);
        if (!pathResult.Success)
        {
            return CreateReadFailure(
                pathResult.ErrorCode,
                pathResult.ErrorMessage);
        }

        if (!File.Exists(pathResult.FullPath))
        {
            return CreateReadFailure(
                TftpErrorCode.FileNotFound,
                "The requested file was not found.");
        }

        if (request.Mode != TftpTransferMode.Octet &&
            request.Mode != TftpTransferMode.Netascii)
        {
            return CreateReadFailure(
                TftpErrorCode.IllegalOperation,
                "The requested transfer mode is not supported.");
        }

        FileStream content = new FileStream(
            pathResult.FullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            4096,
            FileOptions.SequentialScan);
        TftpStorageCancellation.ThrowIfReadCancellationRequested(
            cancellationToken,
            content);

        TftpStorageOpenResult result = new TftpStorageOpenResult();
        result.Success = true;
        result.Content = content;
        result.OwnsContent = true;
        if (request.Mode == TftpTransferMode.Octet)
        {
            result.HasKnownSize = true;
            result.KnownSize = content.Length - content.Position;
        }
        else if (RequestsTransferSize(request.RequestedOptions))
        {
            result.KnownSize = CountNetasciiSize(
                content,
                cancellationToken);
            result.HasKnownSize = true;
        }

        return result;
    }

    /// <summary>
    /// Opens a root-confined pending destination for a write request.
    /// </summary>
    /// <param name="request">The accepted write request.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The storage open result.</returns>
    public TftpStorageOpenResult OpenPendingWrite(
        TftpServerRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request == null)
        {
            return CreateWriteFailure(
                TftpErrorCode.AccessViolation,
                "The storage write request cannot be null.");
        }

        if (request.Direction != TftpTransferDirection.Write)
        {
            return CreateWriteFailure(
                TftpErrorCode.AccessViolation,
                "The storage request is not a write request.");
        }

        if (!Options.AllowWrite)
        {
            return CreateWriteFailure(
                TftpErrorCode.AccessViolation,
                "Writing to this storage provider is not permitted.");
        }

        if (request.HasAnnouncedSize && request.AnnouncedSize < 0)
        {
            return CreateWriteFailure(
                TftpErrorCode.IllegalOperation,
                "The announced write size cannot be negative.");
        }

        if (request.Mode != TftpTransferMode.Octet &&
            request.Mode != TftpTransferMode.Netascii)
        {
            return CreateWriteFailure(
                TftpErrorCode.IllegalOperation,
                "The requested transfer mode is not supported.");
        }

        TftpPathValidationResult pathResult =
            PathValidator.ValidatePath(request.Filename);
        if (!pathResult.Success)
        {
            return CreateWriteFailure(
                pathResult.ErrorCode,
                pathResult.ErrorMessage);
        }

        string? destinationDirectory = Path.GetDirectoryName(
            pathResult.FullPath);
        if (!Directory.Exists(destinationDirectory))
        {
            return CreateWriteFailure(
                TftpErrorCode.AccessViolation,
                "The destination directory does not exist.");
        }

        bool destinationFileExists = File.Exists(pathResult.FullPath);
        bool destinationDirectoryExists = Directory.Exists(
            pathResult.FullPath);
        if (destinationDirectoryExists)
        {
            return CreateWriteFailure(
                TftpErrorCode.FileAlreadyExists,
                "The write destination is an existing directory.");
        }

        if (destinationFileExists && !Options.AllowOverwrite)
        {
            return CreateWriteFailure(
                TftpErrorCode.FileAlreadyExists,
                "The destination file already exists.");
        }

        if (!destinationFileExists && !Options.AllowCreate)
        {
            return CreateWriteFailure(
                TftpErrorCode.AccessViolation,
                "Creating new files is not permitted.");
        }

        string destinationName = Path.GetFileName(pathResult.FullPath);
        string temporaryName = "." + destinationName + "." +
            Guid.NewGuid().ToString("N") + ".tftp.tmp";
        string temporaryPath = Path.Combine(
            destinationDirectory,
            temporaryName);
        TftpPendingWrite pendingWrite = new TftpPendingWrite(
            request.Filename,
            pathResult.FullPath,
            temporaryPath,
            Options.AllowCreate,
            Options.AllowOverwrite,
            request.HasAnnouncedSize,
            request.AnnouncedSize,
            request.Mode);
        lock (PendingWrites)
        {
            PendingWrites.Add(pendingWrite.Token, pendingWrite);
        }

        TftpStorageCancellation.ThrowIfPendingWriteCancellationRequested(
            cancellationToken,
            pendingWrite,
            PendingWrites);

        TftpStorageOpenResult result = new TftpStorageOpenResult();
        result.Success = true;
        result.Content = pendingWrite.Stream;
        result.OwnsContent = true;
        result.HasKnownSize = request.HasAnnouncedSize;
        if (request.HasAnnouncedSize)
        {
            result.KnownSize = request.AnnouncedSize;
        }

        result.CompletionToken = pendingWrite.Token;
        return result;
    }

    /// <summary>
    /// Publishes a completed pending directory write.
    /// </summary>
    /// <param name="completionToken">The pending-write completion token.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The storage commit result.</returns>
    public TftpStorageCommitResult CommitWrite(
        object completionToken,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (completionToken == null)
        {
            return CreateCompletionFailure(
                TftpErrorCode.AccessViolation,
                "The pending-write completion token is invalid.");
        }

        lock (PendingWrites)
        {
            if (!PendingWrites.ContainsKey(completionToken))
            {
                return CreateCompletionFailure(
                    TftpErrorCode.AccessViolation,
                    "The pending-write completion token is invalid or stale.");
            }

            TftpPendingWrite pendingWrite = PendingWrites[completionToken];
            TftpPathValidationResult pathResult =
                PathValidator.ValidatePath(pendingWrite.RequestedPath);
            if (!pathResult.Success || !string.Equals(
                pathResult.FullPath,
                pendingWrite.DestinationPath,
                PathValidator.PathComparison))
            {
                pendingWrite.AbandonWrite();
                PendingWrites.Remove(completionToken);
                return CreateCompletionFailure(
                    TftpErrorCode.AccessViolation,
                    "The pending-write destination is no longer safe.");
            }

            bool destinationFileExists = File.Exists(
                pendingWrite.DestinationPath);
            bool destinationDirectoryExists = Directory.Exists(
                pendingWrite.DestinationPath);
            if (destinationDirectoryExists)
            {
                pendingWrite.AbandonWrite();
                PendingWrites.Remove(completionToken);
                return CreateCompletionFailure(
                    TftpErrorCode.FileAlreadyExists,
                    "The write destination became an existing directory.");
            }

            if (destinationFileExists && !pendingWrite.AllowOverwrite)
            {
                pendingWrite.AbandonWrite();
                PendingWrites.Remove(completionToken);
                return CreateCompletionFailure(
                    TftpErrorCode.FileAlreadyExists,
                    "The destination file was created before commit.");
            }

            if (!destinationFileExists && !pendingWrite.AllowCreate)
            {
                pendingWrite.AbandonWrite();
                PendingWrites.Remove(completionToken);
                return CreateCompletionFailure(
                    TftpErrorCode.AccessViolation,
                    "The destination file was removed before commit and creation is not permitted.");
            }

            TftpStorageCommitResult result = pendingWrite.PublishWrite(
                destinationFileExists && pendingWrite.AllowOverwrite);
            if (result.Success || pendingWrite.Completed)
            {
                PendingWrites.Remove(completionToken);
            }

            return result;
        }
    }

    /// <summary>
    /// Abandons a pending directory write.
    /// </summary>
    /// <param name="completionToken">The pending-write completion token.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The storage abort result.</returns>
    public TftpStorageCommitResult AbortWrite(
        object completionToken,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (completionToken == null)
        {
            return CreateCompletionFailure(
                TftpErrorCode.AccessViolation,
                "The pending-write completion token is invalid.");
        }

        lock (PendingWrites)
        {
            if (!PendingWrites.ContainsKey(completionToken))
            {
                return CreateCompletionFailure(
                    TftpErrorCode.AccessViolation,
                    "The pending-write completion token is invalid or stale.");
            }

            TftpPendingWrite pendingWrite = PendingWrites[completionToken];
            TftpStorageCommitResult result = pendingWrite.AbandonWrite();
            if (result.Success)
            {
                PendingWrites.Remove(completionToken);
            }

            return result;
        }
    }

    internal bool RequestsTransferSize(List<TftpOption> requestedOptions)
    {
        if (requestedOptions == null)
        {
            return false;
        }

        foreach (TftpOption option in requestedOptions)
        {
            if (option != null && string.Equals(
                option.Name,
                TftpProtocolValues.TransferSizeOption,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal long CountNetasciiSize(
        FileStream content,
        CancellationToken cancellationToken)
    {
        long initialPosition = content.Position;
        byte[] buffer = new byte[4096];
        long encodedSize = 0;
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                content.Position = initialPosition;
                content.Dispose();
            }

            cancellationToken.ThrowIfCancellationRequested();

            int readLength = content.Read(buffer, 0, buffer.Length);
            if (readLength == 0)
            {
                break;
            }

            int index = 0;
            while (index < readLength)
            {
                long increment = 1;
                if (buffer[index] == 10 || buffer[index] == 13)
                {
                    increment = 2;
                }

                encodedSize = encodedSize + increment;
                index = index + 1;
            }
        }

        content.Position = initialPosition;
        return encodedSize;
    }

    internal TftpStorageOpenResult CreateReadFailure(
        TftpErrorCode errorCode,
        string message)
    {
        TftpStorageOpenResult result = new TftpStorageOpenResult();
        result.ErrorCode = errorCode;
        result.ErrorMessage = message;
        return result;
    }

    internal TftpStorageOpenResult CreateWriteFailure(
        TftpErrorCode errorCode,
        string message)
    {
        TftpStorageOpenResult result = new TftpStorageOpenResult();
        result.ErrorCode = errorCode;
        result.ErrorMessage = message;
        return result;
    }

    internal TftpStorageCommitResult CreateCompletionFailure(
        TftpErrorCode errorCode,
        string message)
    {
        TftpStorageCommitResult result = new TftpStorageCommitResult();
        result.ErrorCode = errorCode;
        result.ErrorMessage = message;
        return result;
    }
}
