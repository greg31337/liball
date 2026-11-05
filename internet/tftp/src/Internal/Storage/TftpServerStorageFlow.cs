using System.IO;
using System.Threading;

namespace tftp;

internal class TftpServerStorageFlow
{
    internal ITftpServerStorage Storage;

    internal TftpServerStorageFlow(ITftpServerStorage storage)
    {
        if (storage == null)
        {
            throw new TftpStorageException(
                "The server storage provider cannot be null.",
                "CreateStorageFlow",
                null);
        }

        Storage = storage;
    }

    internal TftpStorageOpenResult OpenRead(
        TftpServerRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateStorageRequest(
            request,
            TftpTransferDirection.Read,
            "OpenRead");
        TftpStorageOpenResult result = Storage.OpenRead(
            request,
            cancellationToken);
        return ValidateOpenResult(result, false, "OpenRead");
    }

    internal TftpStorageOpenResult OpenPendingWrite(
        TftpServerRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateStorageRequest(
            request,
            TftpTransferDirection.Write,
            "OpenPendingWrite");
        TftpStorageOpenResult result = Storage.OpenPendingWrite(
            request,
            cancellationToken);
        return ValidateOpenResult(
            result,
            true,
            "OpenPendingWrite");
    }

    internal TftpStorageCommitResult CommitWrite(
        object completionToken,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateCompletionToken(completionToken, "CommitWrite");
        TftpStorageCommitResult result = Storage.CommitWrite(
            completionToken,
            cancellationToken);
        return ValidateCommitResult(result, "CommitWrite");
    }

    internal TftpStorageCommitResult AbortWrite(
        object completionToken,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateCompletionToken(completionToken, "AbortWrite");
        TftpStorageCommitResult result = Storage.AbortWrite(
            completionToken,
            cancellationToken);
        return ValidateCommitResult(result, "AbortWrite");
    }

    internal void ValidateStorageRequest(
        TftpServerRequest request,
        TftpTransferDirection expectedDirection,
        string operation)
    {
        if (request == null)
        {
            throw new TftpStorageException(
                "The storage request cannot be null.",
                operation,
                null);
        }

        if (request.Direction != expectedDirection)
        {
            throw new TftpStorageException(
                "The storage request direction does not match the operation.",
                operation,
                null);
        }
    }

    internal void ValidateCompletionToken(
        object completionToken,
        string operation)
    {
        if (completionToken == null)
        {
            throw new TftpStorageException(
                "The pending-write completion token cannot be null.",
                operation,
                null);
        }
    }

    internal TftpStorageOpenResult ValidateOpenResult(
        TftpStorageOpenResult result,
        bool requiresCompletionToken,
        string operation)
    {
        if (result == null)
        {
            throw new TftpStorageException(
                "The storage provider returned no open result.",
                operation,
                null);
        }

        if (!result.Success)
        {
            return CreateOpenFailure(result);
        }

        Stream? content = result.Content;
        if (content == null)
        {
            throw new TftpStorageException(
                "A successful storage open result requires a content stream.",
                operation,
                null);
        }

        if (requiresCompletionToken)
        {
            if (!content.CanWrite)
            {
                throw new TftpStorageException(
                    "A pending-write storage result requires a writable stream.",
                    operation,
                    null);
            }

            if (result.CompletionToken == null)
            {
                throw new TftpStorageException(
                    "A pending-write storage result requires a completion token.",
                    operation,
                    null);
            }
        }
        else if (!content.CanRead)
        {
            throw new TftpStorageException(
                "A read storage result requires a readable stream.",
                operation,
                null);
        }

        if (result.HasKnownSize && result.KnownSize < 0)
        {
            throw new TftpStorageException(
                "An exact storage content size cannot be negative.",
                operation,
                null);
        }

        TftpStorageOpenResult snapshot = new TftpStorageOpenResult();
        snapshot.Success = true;
        snapshot.Content = content;
        snapshot.OwnsContent = result.OwnsContent;
        snapshot.HasKnownSize = result.HasKnownSize;
        if (result.HasKnownSize)
        {
            snapshot.KnownSize = result.KnownSize;
        }

        if (requiresCompletionToken)
        {
            snapshot.CompletionToken = result.CompletionToken;
        }

        return snapshot;
    }

    internal TftpStorageOpenResult CreateOpenFailure(
        TftpStorageOpenResult result)
    {
        TftpStorageOpenResult failure = new TftpStorageOpenResult();
        failure.ErrorCode = SelectProtocolError(result.ErrorCode);
        if (result.ErrorMessage != null)
        {
            failure.ErrorMessage = result.ErrorMessage;
        }

        return failure;
    }

    internal TftpStorageCommitResult ValidateCommitResult(
        TftpStorageCommitResult result,
        string operation)
    {
        if (result == null)
        {
            throw new TftpStorageException(
                "The storage provider returned no completion result.",
                operation,
                null);
        }

        TftpStorageCommitResult snapshot = new TftpStorageCommitResult();
        snapshot.Success = result.Success;
        if (!result.Success)
        {
            snapshot.ErrorCode = SelectProtocolError(result.ErrorCode);
            if (result.ErrorMessage != null)
            {
                snapshot.ErrorMessage = result.ErrorMessage;
            }
        }

        return snapshot;
    }

    internal TftpErrorCode SelectProtocolError(TftpErrorCode errorCode)
    {
        int numericCode = (int)errorCode;
        if (numericCode < (int)TftpErrorCode.Undefined ||
            numericCode > (int)TftpErrorCode.OptionNegotiationFailed)
        {
            return TftpErrorCode.Undefined;
        }

        return errorCode;
    }
}
