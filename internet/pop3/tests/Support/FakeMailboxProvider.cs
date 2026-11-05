namespace pop3.Tests;

internal sealed class FakeMailboxProvider : IPop3MailboxProvider
{
    internal Pop3MailboxOpenResult Result;
    internal Func<Pop3MailboxOpenRequest, CancellationToken, Pop3MailboxOpenResult>? OpenHandler = null;
    internal readonly List<Pop3MailboxOpenRequest> Requests = new List<Pop3MailboxOpenRequest>();
    internal int OpenCallCount;
    internal CancellationToken LastCancellationToken;

    internal FakeMailboxProvider()
    {
        FakeMailboxSession session = new FakeMailboxSession();
        Result = new Pop3MailboxOpenResult();
        Result.Status = Pop3AuthenticationStatus.Success;
        Result.Session = session;
    }

    internal FakeMailboxProvider(IPop3MailboxSession session)
    {
        Result = new Pop3MailboxOpenResult();
        Result.Status = Pop3AuthenticationStatus.Success;
        Result.Session = session;
    }

    public Pop3MailboxOpenResult OpenMailbox(
        Pop3MailboxOpenRequest request,
        CancellationToken cancellationToken)
    {
        OpenCallCount++;
        Requests.Add(request);
        LastCancellationToken = cancellationToken;
        if (OpenHandler != null)
        {
            return OpenHandler(request, cancellationToken);
        }
        return Result;
    }
}

internal sealed class FakeMailboxSession : IPop3MailboxSession
{
    internal Pop3MailboxStatus Status = new Pop3MailboxStatus();
    internal Pop3MessageInfo[] Messages = Array.Empty<Pop3MessageInfo>();
    internal Pop3UniqueId[] UniqueIds = Array.Empty<Pop3UniqueId>();
    internal Pop3MailboxStatus ResetStatus = new Pop3MailboxStatus();
    internal readonly Dictionary<int, byte[]> MessageData = new Dictionary<int, byte[]>();
    internal readonly Dictionary<int, byte[]> TopData = new Dictionary<int, byte[]>();
    internal Action<int, bool, Stream, CancellationToken>? WriteMessageHandler = null;
    internal Action<int, int, bool, Stream, CancellationToken>? WriteTopHandler = null;
    internal Action<int>? MarkDeletedHandler = null;
    internal Func<Pop3MailboxStatus>? ResetHandler = null;
    internal Action<CancellationToken>? CommitHandler = null;
    internal Action? RollbackHandler = null;
    internal Action? ReleaseHandler = null;
    internal readonly List<string> Calls = new List<string>();
    internal readonly List<int> MarkedMessageNumbers = new List<int>();
    internal int GetStatusCallCount;
    internal int GetMessagesCallCount;
    internal int GetUniqueIdsCallCount;
    internal int WriteMessageCallCount;
    internal int WriteTopCallCount;
    internal int ResetCallCount;
    internal int CommitCallCount;
    internal int RollbackCallCount;
    internal int ReleaseCallCount;
    internal int LastMessageNumber;
    internal int LastTopBodyLineCount;
    internal bool LastUseUtf8Representation;
    internal Stream? LastDestination;
    internal CancellationToken LastCancellationToken;
    internal bool IsCommitted;
    internal bool IsRolledBack;
    internal bool IsReleased;

    public Pop3MailboxStatus GetStatus()
    {
        GetStatusCallCount++;
        Calls.Add("get-status");
        return Status;
    }

    public Pop3MessageInfo[] GetMessages()
    {
        GetMessagesCallCount++;
        Calls.Add("get-messages");
        return Messages;
    }

    public Pop3UniqueId[] GetUniqueIds()
    {
        GetUniqueIdsCallCount++;
        Calls.Add("get-unique-ids");
        return UniqueIds;
    }

    public void WriteMessage(
        int messageNumber,
        bool useUtf8Representation,
        Stream destination,
        CancellationToken cancellationToken)
    {
        WriteMessageCallCount++;
        Calls.Add("write-message:" + messageNumber.ToString());
        RecordTransfer(messageNumber, useUtf8Representation, destination, cancellationToken);
        if (WriteMessageHandler != null)
        {
            WriteMessageHandler(messageNumber, useUtf8Representation, destination, cancellationToken);
            return;
        }
        if (MessageData.TryGetValue(messageNumber, out byte[]? data))
        {
            destination.Write(data, 0, data.Length);
        }
    }

    public void WriteTop(
        int messageNumber,
        int bodyLineCount,
        bool useUtf8Representation,
        Stream destination,
        CancellationToken cancellationToken)
    {
        WriteTopCallCount++;
        Calls.Add("write-top:" + messageNumber.ToString());
        LastTopBodyLineCount = bodyLineCount;
        RecordTransfer(messageNumber, useUtf8Representation, destination, cancellationToken);
        if (WriteTopHandler != null)
        {
            WriteTopHandler(
                messageNumber,
                bodyLineCount,
                useUtf8Representation,
                destination,
                cancellationToken);
            return;
        }
        if (TopData.TryGetValue(messageNumber, out byte[]? data))
        {
            destination.Write(data, 0, data.Length);
        }
    }

    public void MarkDeleted(int messageNumber)
    {
        Calls.Add("mark-deleted:" + messageNumber.ToString());
        MarkedMessageNumbers.Add(messageNumber);
        if (MarkDeletedHandler != null)
        {
            MarkDeletedHandler(messageNumber);
        }
    }

    public Pop3MailboxStatus ResetDeletions()
    {
        ResetCallCount++;
        Calls.Add("reset");
        MarkedMessageNumbers.Clear();
        if (ResetHandler != null)
        {
            return ResetHandler();
        }
        return ResetStatus;
    }

    public void CommitUpdate(CancellationToken cancellationToken)
    {
        CommitCallCount++;
        Calls.Add("commit");
        LastCancellationToken = cancellationToken;
        if (CommitHandler != null)
        {
            CommitHandler(cancellationToken);
        }
        IsCommitted = true;
    }

    public void RollbackChanges()
    {
        RollbackCallCount++;
        Calls.Add("rollback");
        if (RollbackHandler != null)
        {
            RollbackHandler();
        }
        IsRolledBack = true;
    }

    public void ReleaseSession()
    {
        ReleaseCallCount++;
        Calls.Add("release");
        if (ReleaseHandler != null)
        {
            ReleaseHandler();
        }
        IsReleased = true;
    }

    void RecordTransfer(
        int messageNumber,
        bool useUtf8Representation,
        Stream destination,
        CancellationToken cancellationToken)
    {
        LastMessageNumber = messageNumber;
        LastUseUtf8Representation = useUtf8Representation;
        LastDestination = destination;
        LastCancellationToken = cancellationToken;
    }
}
