using System.IO;

namespace tftp;

internal class TftpPendingWrite
{
    internal object Token;
    internal string RequestedPath;
    internal string DestinationPath;
    internal string TemporaryPath;
    internal FileStream Stream;
    internal bool AllowCreate;
    internal bool AllowOverwrite;
    internal bool HasAnnouncedSize;
    internal long AnnouncedSize;
    internal TftpTransferMode Mode;
    internal bool Completed = false;

    internal TftpPendingWrite(
        string requestedPath,
        string destinationPath,
        string temporaryPath,
        bool allowCreate,
        bool allowOverwrite,
        bool hasAnnouncedSize,
        long announcedSize,
        TftpTransferMode mode)
    {
        Token = new object();
        RequestedPath = requestedPath;
        DestinationPath = destinationPath;
        TemporaryPath = temporaryPath;
        AllowCreate = allowCreate;
        AllowOverwrite = allowOverwrite;
        HasAnnouncedSize = hasAnnouncedSize;
        AnnouncedSize = announcedSize;
        Mode = mode;
        Stream = new FileStream(
            temporaryPath,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            4096,
            FileOptions.SequentialScan);
    }

    internal TftpStorageCommitResult PublishWrite(bool overwriteExisting)
    {
        if (Completed)
        {
            return CreatePendingFailure(
                TftpErrorCode.AccessViolation,
                "The pending write has already been completed.");
        }

        ClosePendingStream();
        if (HasAnnouncedSize &&
            CalculateEncodedContentSize() != AnnouncedSize)
        {
            if (File.Exists(TemporaryPath))
            {
                File.Delete(TemporaryPath);
            }

            Completed = true;
            return CreatePendingFailure(
                TftpErrorCode.IllegalOperation,
                "The pending write size does not match the announced transfer size.");
        }

        File.Move(
            TemporaryPath,
            DestinationPath,
            overwriteExisting);
        Completed = true;
        TftpStorageCommitResult result = new TftpStorageCommitResult();
        result.Success = true;
        return result;
    }

    internal TftpStorageCommitResult AbandonWrite()
    {
        if (Completed)
        {
            return CreatePendingFailure(
                TftpErrorCode.AccessViolation,
                "The pending write has already been completed.");
        }

        ClosePendingStream();
        if (File.Exists(TemporaryPath))
        {
            File.Delete(TemporaryPath);
        }

        Completed = true;
        TftpStorageCommitResult result = new TftpStorageCommitResult();
        result.Success = true;
        return result;
    }

    internal void ClosePendingStream()
    {
        if (Stream.CanWrite)
        {
            Stream.Flush(true);
        }

        Stream.Dispose();
    }

    internal long CalculateEncodedContentSize()
    {
        if (Mode == TftpTransferMode.Octet)
        {
            return new FileInfo(TemporaryPath).Length;
        }

        using (FileStream content = new FileStream(
            TemporaryPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            4096,
            FileOptions.SequentialScan))
        {
            byte[] buffer = new byte[4096];
            long encodedSize = 0;
            while (true)
            {
                int readLength = content.Read(buffer, 0, buffer.Length);
                if (readLength == 0)
                {
                    return encodedSize;
                }

                int index = 0;
                while (index < readLength)
                {
                    long increment = 1;
                    if (buffer[index] == 10 || buffer[index] == 13)
                    {
                        increment = 2;
                    }

                    if (encodedSize > long.MaxValue - increment)
                    {
                        throw new TftpStorageException(
                            "The pending netascii size exceeds the supported range.",
                            "CommitWrite",
                            null);
                    }

                    encodedSize = encodedSize + increment;
                    index = index + 1;
                }
            }
        }
    }

    internal TftpStorageCommitResult CreatePendingFailure(
        TftpErrorCode errorCode,
        string message)
    {
        TftpStorageCommitResult result = new TftpStorageCommitResult();
        result.ErrorCode = errorCode;
        result.ErrorMessage = message;
        return result;
    }
}
