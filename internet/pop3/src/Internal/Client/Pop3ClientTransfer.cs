namespace pop3;

internal class Pop3ClientTransfer
{
    IPop3TransferObserver? Observer;
    object SyncRoot = new object();

    internal Pop3ClientTransfer(IPop3TransferObserver? observer)
    {
        Observer = observer;
    }

    internal void ReportProgress(
        Pop3TransferKind kind,
        int messageNumber,
        long bytesTransferred,
        long expectedBytes,
        bool isComplete)
    {
        ValidateMetadata(kind, messageNumber, expectedBytes);
        if (bytesTransferred < 0)
        {
            throw new System.ArgumentOutOfRangeException("bytesTransferred");
        }
        if (Observer == null)
        {
            return;
        }

        lock (SyncRoot)
        {
            Pop3TransferProgress progress = new Pop3TransferProgress();
            progress.Kind = kind;
            progress.MessageNumber = messageNumber;
            progress.BytesTransferred = bytesTransferred;
            progress.ExpectedBytes = expectedBytes;
            progress.IsComplete = isComplete;
            Observer.Report(progress);
        }
    }

    internal void ValidateMetadata(
        Pop3TransferKind kind,
        int messageNumber,
        long expectedBytes)
    {
        if (kind != Pop3TransferKind.List &&
            kind != Pop3TransferKind.Uidl &&
            kind != Pop3TransferKind.Retrieve &&
            kind != Pop3TransferKind.Top &&
            kind != Pop3TransferKind.ExtensionData)
        {
            throw new System.ArgumentOutOfRangeException("kind");
        }
        if (messageNumber < 0)
        {
            throw new System.ArgumentOutOfRangeException("messageNumber");
        }
        if ((kind == Pop3TransferKind.Retrieve || kind == Pop3TransferKind.Top) && messageNumber == 0)
        {
            throw new System.ArgumentOutOfRangeException("messageNumber");
        }
        if (expectedBytes < -1)
        {
            throw new System.ArgumentOutOfRangeException("expectedBytes");
        }
    }
}
