namespace pop3;

internal class Pop3MailboxTransaction
{
    IPop3MailboxSession? Session;
    Pop3MessageInfo[] Messages = new Pop3MessageInfo[0];
    Pop3UniqueId[] UniqueIds = new Pop3UniqueId[0];
    long TotalOctets;

    internal Pop3MailboxOpenResult OpenMailbox(
        Pop3AuthenticationResult authentication,
        IPop3MailboxProvider? provider,
        Pop3ServerConnection connection,
        System.Threading.CancellationToken cancellationToken)
    {
        ValidateInputs(authentication, connection);
        cancellationToken.ThrowIfCancellationRequested();
        if (Session != null)
        {
            throw new Pop3StateException(
                "A POP3 mailbox transaction is already active.",
                "OpenMailbox",
                Pop3SessionState.Transaction);
        }
        if (provider == null)
        {
            return CreateFailureResult(
                Pop3AuthenticationStatus.PermanentFailure);
        }

        Pop3MailboxOpenRequest request = new Pop3MailboxOpenRequest();
        request.AuthenticatedIdentity = authentication.AuthenticatedIdentity ??
            throw new System.InvalidOperationException(
                "A successful POP3 authentication identity is unavailable.");
        request.MaildropKey = authentication.MaildropKey ??
            throw new System.InvalidOperationException(
                "A successful POP3 authentication maildrop key is unavailable.");
        request.Connection = connection;
        Pop3MailboxOpenResult suppliedResult = provider.OpenMailbox(
            request,
            cancellationToken);
        ValidateResult(suppliedResult);
        Pop3MailboxOpenResult result = CopyResult(suppliedResult);
        if (result.Status == Pop3AuthenticationStatus.Success)
        {
            IPop3MailboxSession session = result.Session ??
                throw new System.InvalidOperationException(
                    "A successful POP3 mailbox session is unavailable.");
            InitializeSnapshot(session);
            Session = session;
            return result;
        }

        IPop3MailboxSession? failedSession = result.Session;
        result.Session = null;
        if (failedSession != null)
        {
            failedSession.ReleaseSession();
        }
        return result;
    }

    internal bool HasSession()
    {
        return Session != null;
    }

    internal Pop3MailboxStatus GetStatus()
    {
        EnsureSession("GetStatus");
        Pop3MailboxStatus status = new Pop3MailboxStatus();
        int index = 0;
        while (index < Messages.Length)
        {
            if (!Messages[index].IsDeleted)
            {
                status.MessageCount++;
                status.TotalOctets += Messages[index].OctetCount;
            }
            index++;
        }
        return status;
    }

    internal Pop3MessageInfo[] GetMessages()
    {
        EnsureSession("GetMessages");
        return CopyMessages(Messages);
    }

    internal Pop3UniqueId[] GetUniqueIds()
    {
        EnsureSession("GetUniqueIds");
        return CopyUniqueIds(UniqueIds);
    }

    internal void MarkDeleted(int messageNumber)
    {
        IPop3MailboxSession session = EnsureSession("MarkDeleted");
        if (messageNumber < 1 || messageNumber > Messages.Length)
        {
            throw new System.ArgumentOutOfRangeException("messageNumber");
        }
        int index = messageNumber - 1;
        if (Messages[index].IsDeleted)
        {
            throw new System.InvalidOperationException(
                "The POP3 message is already marked for deletion.");
        }

        session.MarkDeleted(messageNumber);
        Messages[index].IsDeleted = true;
    }

    internal Pop3MailboxStatus ResetDeletions()
    {
        IPop3MailboxSession session = EnsureSession("ResetDeletions");
        Pop3MailboxStatus restoredStatus = session.ResetDeletions();
        ValidateRestoredStatus(restoredStatus);
        int index = 0;
        while (index < Messages.Length)
        {
            Messages[index].IsDeleted = false;
            index++;
        }
        return CopyStatus(restoredStatus);
    }

    internal void WriteMessage(
        int messageNumber,
        bool useUtf8Representation,
        System.IO.Stream destination,
        System.Threading.CancellationToken cancellationToken)
    {
        IPop3MailboxSession session = EnsureSession("WriteMessage");
        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }
        if (!destination.CanWrite)
        {
            throw new System.ArgumentException(
                "A POP3 message destination must be writable.",
                "destination");
        }
        if (messageNumber < 1 || messageNumber > Messages.Length)
        {
            throw new System.ArgumentOutOfRangeException("messageNumber");
        }
        if (Messages[messageNumber - 1].IsDeleted)
        {
            throw new System.InvalidOperationException(
                "A deleted POP3 message cannot be written.");
        }
        cancellationToken.ThrowIfCancellationRequested();
        session.WriteMessage(
            messageNumber,
            useUtf8Representation,
            destination,
            cancellationToken);
    }

    internal void WriteTop(
        int messageNumber,
        int bodyLineCount,
        bool useUtf8Representation,
        System.IO.Stream destination,
        System.Threading.CancellationToken cancellationToken)
    {
        IPop3MailboxSession session = EnsureSession("WriteTop");
        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }
        if (!destination.CanWrite)
        {
            throw new System.ArgumentException(
                "A POP3 TOP destination must be writable.",
                "destination");
        }
        if (messageNumber < 1 || messageNumber > Messages.Length)
        {
            throw new System.ArgumentOutOfRangeException("messageNumber");
        }
        if (bodyLineCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("bodyLineCount");
        }
        if (Messages[messageNumber - 1].IsDeleted)
        {
            throw new System.InvalidOperationException(
                "A deleted POP3 message cannot be written by TOP.");
        }
        cancellationToken.ThrowIfCancellationRequested();
        session.WriteTop(
            messageNumber,
            bodyLineCount,
            useUtf8Representation,
            destination,
            cancellationToken);
    }

    internal bool CommitAndRelease(
        System.Threading.CancellationToken cancellationToken)
    {
        IPop3MailboxSession session = EnsureSession("CommitAndRelease");
        cancellationToken.ThrowIfCancellationRequested();
        System.Threading.Tasks.Task commitTask =
            System.Threading.Tasks.Task.Run(() =>
                session.CommitUpdate(cancellationToken));
        System.Threading.Tasks.Task rollbackTask = commitTask.ContinueWith(
            task => RollbackFailedCommit(task, session),
            System.Threading.CancellationToken.None,
            System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
            System.Threading.Tasks.TaskScheduler.Default);
        System.Threading.Tasks.Task releaseTask = rollbackTask.ContinueWith(
            task => session.ReleaseSession(),
            System.Threading.CancellationToken.None,
            System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
            System.Threading.Tasks.TaskScheduler.Default);
        System.Threading.Tasks.Task<bool> completionTask =
            releaseTask.ContinueWith(
                task => GetUpdateCompletion(
                    commitTask,
                    rollbackTask,
                    releaseTask),
                System.Threading.CancellationToken.None,
                System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                System.Threading.Tasks.TaskScheduler.Default);
        bool committed = completionTask.GetAwaiter().GetResult();
        ClearSession();
        return committed;
    }

    internal bool RollbackAndRelease()
    {
        IPop3MailboxSession? session = Session;
        if (session == null)
        {
            return true;
        }

        System.Threading.Tasks.Task rollbackTask =
            System.Threading.Tasks.Task.Run(() =>
                session.RollbackChanges());
        System.Threading.Tasks.Task releaseTask = rollbackTask.ContinueWith(
            task => session.ReleaseSession(),
            System.Threading.CancellationToken.None,
            System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
            System.Threading.Tasks.TaskScheduler.Default);
        System.Threading.Tasks.Task<bool> completionTask =
            releaseTask.ContinueWith(
                task => GetAbortCompletion(
                    rollbackTask,
                    releaseTask),
                System.Threading.CancellationToken.None,
                System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                System.Threading.Tasks.TaskScheduler.Default);
        bool completed = completionTask.GetAwaiter().GetResult();
        ClearSession();
        return completed;
    }

    void RollbackFailedCommit(
        System.Threading.Tasks.Task commitTask,
        IPop3MailboxSession session)
    {
        if (!commitTask.IsCompletedSuccessfully)
        {
            session.RollbackChanges();
        }
    }

    bool GetUpdateCompletion(
        System.Threading.Tasks.Task commitTask,
        System.Threading.Tasks.Task rollbackTask,
        System.Threading.Tasks.Task releaseTask)
    {
        System.AggregateException? commitFailure = commitTask.Exception;
        System.AggregateException? rollbackFailure = rollbackTask.Exception;
        System.AggregateException? releaseFailure = releaseTask.Exception;
        return commitTask.IsCompletedSuccessfully &&
            rollbackTask.IsCompletedSuccessfully &&
            releaseTask.IsCompletedSuccessfully &&
            commitFailure == null &&
            rollbackFailure == null &&
            releaseFailure == null;
    }

    bool GetAbortCompletion(
        System.Threading.Tasks.Task rollbackTask,
        System.Threading.Tasks.Task releaseTask)
    {
        System.AggregateException? rollbackFailure = rollbackTask.Exception;
        System.AggregateException? releaseFailure = releaseTask.Exception;
        return rollbackTask.IsCompletedSuccessfully &&
            releaseTask.IsCompletedSuccessfully &&
            rollbackFailure == null &&
            releaseFailure == null;
    }

    void ClearSession()
    {
        Session = null;
        Messages = new Pop3MessageInfo[0];
        UniqueIds = new Pop3UniqueId[0];
        TotalOctets = 0;
    }

    void InitializeSnapshot(IPop3MailboxSession session)
    {
        Pop3MailboxStatus? suppliedStatus = session.GetStatus();
        Pop3MessageInfo[]? suppliedMessages = session.GetMessages();
        Pop3UniqueId[]? suppliedUniqueIds = session.GetUniqueIds();
        string? validationError = FindSnapshotError(
            suppliedStatus,
            suppliedMessages,
            suppliedUniqueIds);
        if (validationError != null)
        {
            session.ReleaseSession();
            throw new System.IO.InvalidDataException(validationError);
        }

        Pop3MessageInfo[] messages = suppliedMessages ??
            throw new System.InvalidOperationException(
                "Validated POP3 message metadata is unavailable.");
        Pop3UniqueId[] uniqueIds = suppliedUniqueIds ??
            throw new System.InvalidOperationException(
                "Validated POP3 unique identifiers are unavailable.");
        Messages = CopyMessages(messages);
        UniqueIds = CopyUniqueIds(uniqueIds);
        TotalOctets = CalculateTotalOctets(Messages);
    }

    string? FindSnapshotError(
        Pop3MailboxStatus? status,
        Pop3MessageInfo[]? messages,
        Pop3UniqueId[]? uniqueIds)
    {
        if (status == null)
        {
            return "The POP3 mailbox provider returned a null status.";
        }
        if (status.MessageCount < 0 || status.TotalOctets < 0)
        {
            return "The POP3 mailbox provider returned a negative status value.";
        }
        if (messages == null)
        {
            return "The POP3 mailbox provider returned null message metadata.";
        }
        if (status.MessageCount != messages.Length)
        {
            return "The POP3 mailbox status does not match the message count.";
        }

        long totalOctets = 0;
        int messageIndex = 0;
        while (messageIndex < messages.Length)
        {
            Pop3MessageInfo message = messages[messageIndex];
            if (message == null)
            {
                return "The POP3 mailbox contains null message metadata.";
            }
            if (message.MessageNumber != messageIndex + 1)
            {
                return "POP3 mailbox message numbers must be consecutive and one-based.";
            }
            if (message.OctetCount < 0)
            {
                return "A POP3 mailbox message size cannot be negative.";
            }
            if (message.IsDeleted)
            {
                return "A new POP3 mailbox snapshot cannot contain deletion marks.";
            }
            if (totalOctets > long.MaxValue - message.OctetCount)
            {
                return "The POP3 mailbox octet total exceeds Int64.";
            }
            totalOctets += message.OctetCount;
            messageIndex++;
        }
        if (status.TotalOctets != totalOctets)
        {
            return "The POP3 mailbox status does not match the message sizes.";
        }
        if (uniqueIds == null)
        {
            return "The POP3 mailbox provider returned null unique identifiers.";
        }
        if (uniqueIds.Length != messages.Length)
        {
            return "The POP3 unique-id count does not match the message count.";
        }

        int uniqueIdIndex = 0;
        while (uniqueIdIndex < uniqueIds.Length)
        {
            Pop3UniqueId uniqueId = uniqueIds[uniqueIdIndex];
            if (uniqueId == null)
            {
                return "The POP3 mailbox contains a null unique identifier.";
            }
            if (uniqueId.MessageNumber != uniqueIdIndex + 1)
            {
                return "POP3 unique-id message numbers must be consecutive and one-based.";
            }
            if (!IsValidUniqueId(uniqueId.Value))
            {
                return "A POP3 unique-id must contain one to 70 visible ASCII characters.";
            }
            int previousIndex = 0;
            while (previousIndex < uniqueIdIndex)
            {
                if (string.Equals(
                    uniqueIds[previousIndex].Value,
                    uniqueId.Value,
                    System.StringComparison.Ordinal))
                {
                    return "POP3 unique identifiers must be unique within a maildrop.";
                }
                previousIndex++;
            }
            uniqueIdIndex++;
        }
        return null;
    }

    bool IsValidUniqueId(string value)
    {
        if (value == null || value.Length < 1 || value.Length > 70)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < 33 || value[index] > 126)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    void ValidateRestoredStatus(Pop3MailboxStatus status)
    {
        if (status == null)
        {
            throw new System.IO.InvalidDataException(
                "The POP3 mailbox provider returned a null reset status.");
        }
        if (status.MessageCount != Messages.Length ||
            status.TotalOctets != TotalOctets)
        {
            throw new System.IO.InvalidDataException(
                "The POP3 mailbox reset status does not match the transaction snapshot.");
        }
    }

    IPop3MailboxSession EnsureSession(string operation)
    {
        if (Session == null)
        {
            throw new Pop3StateException(
                "A POP3 mailbox transaction is not active.",
                operation,
                Pop3SessionState.Authorization);
        }
        return Session;
    }

    Pop3MailboxStatus CopyStatus(Pop3MailboxStatus source)
    {
        Pop3MailboxStatus copy = new Pop3MailboxStatus();
        copy.MessageCount = source.MessageCount;
        copy.TotalOctets = source.TotalOctets;
        return copy;
    }

    Pop3MessageInfo[] CopyMessages(Pop3MessageInfo[] source)
    {
        Pop3MessageInfo[] copy = new Pop3MessageInfo[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            Pop3MessageInfo message = new Pop3MessageInfo();
            message.MessageNumber = source[index].MessageNumber;
            message.OctetCount = source[index].OctetCount;
            message.IsDeleted = source[index].IsDeleted;
            message.IsUtf8Representation = source[index].IsUtf8Representation;
            copy[index] = message;
            index++;
        }
        return copy;
    }

    Pop3UniqueId[] CopyUniqueIds(Pop3UniqueId[] source)
    {
        Pop3UniqueId[] copy = new Pop3UniqueId[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            Pop3UniqueId uniqueId = new Pop3UniqueId();
            uniqueId.MessageNumber = source[index].MessageNumber;
            uniqueId.Value = source[index].Value;
            copy[index] = uniqueId;
            index++;
        }
        return copy;
    }

    long CalculateTotalOctets(Pop3MessageInfo[] messages)
    {
        long totalOctets = 0;
        int index = 0;
        while (index < messages.Length)
        {
            totalOctets += messages[index].OctetCount;
            index++;
        }
        return totalOctets;
    }

    void ValidateInputs(
        Pop3AuthenticationResult authentication,
        Pop3ServerConnection connection)
    {
        if (authentication == null)
        {
            throw new System.ArgumentNullException("authentication");
        }
        if (connection == null)
        {
            throw new System.ArgumentNullException("connection");
        }
        if (authentication.Status != Pop3AuthenticationStatus.Success ||
            authentication.AuthenticatedIdentity == null ||
            authentication.MaildropKey == null)
        {
            throw new System.ArgumentException(
                "A successful POP3 authentication result is required.",
                "authentication");
        }
    }

    void ValidateResult(Pop3MailboxOpenResult result)
    {
        if (result == null)
        {
            throw new System.IO.InvalidDataException(
                "The POP3 mailbox provider returned a null result.");
        }
        if (result.Status != Pop3AuthenticationStatus.Success &&
            result.Status != Pop3AuthenticationStatus.LoginDelay &&
            result.Status != Pop3AuthenticationStatus.InUse &&
            result.Status != Pop3AuthenticationStatus.TemporaryFailure &&
            result.Status != Pop3AuthenticationStatus.PermanentFailure)
        {
            throw new System.IO.InvalidDataException(
                "The POP3 mailbox provider returned an invalid status.");
        }
        if (result.RetryAfterSeconds < -1)
        {
            throw new System.IO.InvalidDataException(
                "The POP3 mailbox provider returned an invalid retry delay.");
        }
        if (result.Status == Pop3AuthenticationStatus.Success &&
            result.Session == null)
        {
            throw new System.IO.InvalidDataException(
                "A successful POP3 mailbox result requires a session.");
        }
    }

    Pop3MailboxOpenResult CopyResult(Pop3MailboxOpenResult source)
    {
        Pop3MailboxOpenResult copy = new Pop3MailboxOpenResult();
        copy.Status = source.Status;
        copy.Session = source.Session;
        copy.RetryAfterSeconds = source.RetryAfterSeconds;
        copy.FailureText = source.FailureText;
        return copy;
    }

    Pop3MailboxOpenResult CreateFailureResult(
        Pop3AuthenticationStatus status)
    {
        Pop3MailboxOpenResult result = new Pop3MailboxOpenResult();
        result.Status = status;
        return result;
    }
}
