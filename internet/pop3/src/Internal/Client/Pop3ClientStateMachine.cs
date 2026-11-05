namespace pop3;

internal class Pop3ClientStateMachine
{
    object SyncRoot = new object();
    Pop3SessionState State;
    bool IsConnected;
    bool IsSecure;
    bool IsAuthenticated;
    string? AuthenticatedIdentity;
    string? Greeting;
    string? GreetingTimestamp;
    Pop3Response? LastResponse;
    Pop3CapabilitySet Capabilities = new Pop3CapabilitySet();
    Pop3MailboxStatus? MailboxStatus;
    bool IsUtf8Enabled;
    bool IsUtf8UserEnabled;
    string? SelectedLanguage;
    Pop3MessageInfo[] Messages = new Pop3MessageInfo[0];
    Pop3UniqueId[] UniqueIds = new Pop3UniqueId[0];

    internal Pop3SessionState GetCurrentState()
    {
        lock (SyncRoot)
        {
            return State;
        }
    }

    internal void RequireDisconnected(string operation)
    {
        lock (SyncRoot)
        {
            ValidateOperation(operation);
            if (State != Pop3SessionState.Disconnected)
            {
                ThrowState(operation);
            }
        }
    }

    internal void RequireAuthorization(string operation)
    {
        lock (SyncRoot)
        {
            ValidateOperation(operation);
            if (State != Pop3SessionState.Authorization)
            {
                ThrowState(operation);
            }
        }
    }

    internal void RequireTransaction(string operation)
    {
        lock (SyncRoot)
        {
            ValidateOperation(operation);
            if (State != Pop3SessionState.Transaction)
            {
                ThrowState(operation);
            }
        }
    }

    internal void RequireActive(string operation)
    {
        lock (SyncRoot)
        {
            ValidateOperation(operation);
            if (State != Pop3SessionState.Authorization && State != Pop3SessionState.Transaction)
            {
                ThrowState(operation);
            }
        }
    }

    internal void RequireTlsUpgrade()
    {
        lock (SyncRoot)
        {
            if (State != Pop3SessionState.Authorization || IsSecure || IsUtf8Enabled)
            {
                ThrowState(Pop3Commands.Stls);
            }
        }
    }

    internal void EnterAuthorization(Pop3Response greeting, bool isSecure)
    {
        lock (SyncRoot)
        {
            if (State != Pop3SessionState.Disconnected)
            {
                ThrowState("Connect");
            }
            if (greeting == null)
            {
                throw new System.ArgumentNullException("greeting");
            }
            if (greeting.Kind != Pop3ResponseKind.Positive)
            {
                throw new System.IO.InvalidDataException("A client session requires a positive POP3 greeting.");
            }

            Pop3Response copiedGreeting = CopyResponse(greeting);
            State = Pop3SessionState.Authorization;
            IsConnected = true;
            IsSecure = isSecure;
            IsAuthenticated = false;
            AuthenticatedIdentity = null;
            Greeting = copiedGreeting.Text;
            GreetingTimestamp = copiedGreeting.GreetingTimestamp;
            LastResponse = copiedGreeting;
            Capabilities = new Pop3CapabilitySet();
            MailboxStatus = null;
            IsUtf8Enabled = false;
            IsUtf8UserEnabled = false;
            SelectedLanguage = null;
            Messages = new Pop3MessageInfo[0];
            UniqueIds = new Pop3UniqueId[0];
        }
    }

    internal void MarkSecure()
    {
        lock (SyncRoot)
        {
            if (State != Pop3SessionState.Authorization)
            {
                ThrowState(Pop3Commands.Stls);
            }
            IsSecure = true;
        }
    }

    internal void EnterTransaction(string? authenticatedIdentity)
    {
        lock (SyncRoot)
        {
            if (State != Pop3SessionState.Authorization)
            {
                ThrowState("Authenticate");
            }
            State = Pop3SessionState.Transaction;
            IsAuthenticated = true;
            AuthenticatedIdentity = authenticatedIdentity;
            MailboxStatus = null;
            Messages = new Pop3MessageInfo[0];
            UniqueIds = new Pop3UniqueId[0];
        }
    }

    internal void EnterUpdate()
    {
        lock (SyncRoot)
        {
            if (State != Pop3SessionState.Transaction)
            {
                ThrowState(Pop3Commands.Quit);
            }
            State = Pop3SessionState.Update;
        }
    }

    internal void TrackResponse(Pop3Response response)
    {
        if (response == null)
        {
            throw new System.ArgumentNullException("response");
        }

        Pop3Response copy = CopyResponse(response);
        lock (SyncRoot)
        {
            LastResponse = copy;
        }
    }

    internal string? GetGreetingTimestamp()
    {
        lock (SyncRoot)
        {
            return GreetingTimestamp;
        }
    }

    internal void SetCapabilities(Pop3CapabilitySet capabilities)
    {
        if (capabilities == null)
        {
            throw new System.ArgumentNullException("capabilities");
        }

        Pop3CapabilitySet copy = CopyCapabilities(capabilities);
        lock (SyncRoot)
        {
            EnsureActive(Pop3Commands.Capa);
            Capabilities = copy;
        }
    }

    internal void ClearCapabilities()
    {
        lock (SyncRoot)
        {
            Capabilities = new Pop3CapabilitySet();
        }
    }

    internal void SetMailboxStatus(Pop3MailboxStatus status)
    {
        if (status == null)
        {
            throw new System.ArgumentNullException("status");
        }
        if (status.MessageCount < 0)
        {
            throw new System.IO.InvalidDataException("A POP3 mailbox count cannot be negative.");
        }
        if (status.TotalOctets < 0)
        {
            throw new System.IO.InvalidDataException("A POP3 mailbox size cannot be negative.");
        }

        Pop3MailboxStatus copy = CopyMailboxStatus(status);
        lock (SyncRoot)
        {
            EnsureTransaction(Pop3Commands.Stat);
            MailboxStatus = copy;
        }
    }

    internal void SetMessages(Pop3MessageInfo[] messages)
    {
        if (messages == null)
        {
            throw new System.ArgumentNullException("messages");
        }

        Pop3MessageInfo[] copy = CopyMessages(messages);
        lock (SyncRoot)
        {
            EnsureTransaction(Pop3Commands.List);
            Messages = copy;
        }
    }

    internal void SetMessage(Pop3MessageInfo message)
    {
        if (message == null)
        {
            throw new System.ArgumentNullException("message");
        }
        if (message.MessageNumber < 1)
        {
            throw new System.IO.InvalidDataException("A POP3 message number must be positive.");
        }
        if (message.OctetCount < 0)
        {
            throw new System.IO.InvalidDataException("A POP3 message size cannot be negative.");
        }

        Pop3MessageInfo copy = CopyMessage(message);
        lock (SyncRoot)
        {
            EnsureTransaction(Pop3Commands.List);
            int existingIndex = FindMessageIndex(copy.MessageNumber);
            if (existingIndex >= 0)
            {
                Messages[existingIndex] = copy;
                return;
            }

            Pop3MessageInfo[] expanded = new Pop3MessageInfo[Messages.Length + 1];
            int readIndex = 0;
            int writeIndex = 0;
            while (readIndex < Messages.Length &&
                Messages[readIndex].MessageNumber < copy.MessageNumber)
            {
                expanded[writeIndex] = Messages[readIndex];
                readIndex++;
                writeIndex++;
            }
            expanded[writeIndex] = copy;
            writeIndex++;
            while (readIndex < Messages.Length)
            {
                expanded[writeIndex] = Messages[readIndex];
                readIndex++;
                writeIndex++;
            }
            Messages = expanded;
        }
    }

    internal void MarkMessageDeleted(int messageNumber)
    {
        if (messageNumber < 1)
        {
            throw new System.ArgumentOutOfRangeException("messageNumber");
        }

        lock (SyncRoot)
        {
            EnsureTransaction(Pop3Commands.Dele);
            int messageIndex = FindMessageIndex(messageNumber);
            if (messageIndex < 0)
            {
                MailboxStatus = null;
                return;
            }
            if (Messages[messageIndex].IsDeleted)
            {
                throw new System.InvalidOperationException(
                    "The POP3 message is already marked for deletion.");
            }

            Messages[messageIndex].IsDeleted = true;
            if (MailboxStatus == null)
            {
                return;
            }
            if (MailboxStatus.MessageCount < 1 ||
                MailboxStatus.TotalOctets < Messages[messageIndex].OctetCount)
            {
                MailboxStatus = null;
                return;
            }
            MailboxStatus.MessageCount--;
            MailboxStatus.TotalOctets -= Messages[messageIndex].OctetCount;
        }
    }

    internal void ResetDeletionMarks()
    {
        lock (SyncRoot)
        {
            EnsureTransaction(Pop3Commands.Rset);
            int index = 0;
            while (index < Messages.Length)
            {
                Messages[index].IsDeleted = false;
                index++;
            }
            MailboxStatus = null;
        }
    }

    internal void SetUniqueIds(Pop3UniqueId[] uniqueIds)
    {
        if (uniqueIds == null)
        {
            throw new System.ArgumentNullException("uniqueIds");
        }

        Pop3UniqueId[] copy = CopyUniqueIds(uniqueIds);
        lock (SyncRoot)
        {
            EnsureTransaction(Pop3Commands.Uidl);
            UniqueIds = copy;
        }
    }

    internal void SetUniqueId(Pop3UniqueId uniqueId)
    {
        if (uniqueId == null)
        {
            throw new System.ArgumentNullException("uniqueId");
        }
        if (uniqueId.MessageNumber < 1)
        {
            throw new System.IO.InvalidDataException("A POP3 unique-id message number must be positive.");
        }
        if (uniqueId.Value == null)
        {
            throw new System.IO.InvalidDataException("A POP3 unique-id value cannot be null.");
        }

        Pop3UniqueId copy = CopyUniqueId(uniqueId);
        lock (SyncRoot)
        {
            EnsureTransaction(Pop3Commands.Uidl);
            int existingIndex = FindUniqueIdIndex(copy.MessageNumber);
            if (existingIndex >= 0)
            {
                UniqueIds[existingIndex] = copy;
                return;
            }

            Pop3UniqueId[] expanded = new Pop3UniqueId[UniqueIds.Length + 1];
            int readIndex = 0;
            int writeIndex = 0;
            while (readIndex < UniqueIds.Length &&
                UniqueIds[readIndex].MessageNumber < copy.MessageNumber)
            {
                expanded[writeIndex] = UniqueIds[readIndex];
                readIndex++;
                writeIndex++;
            }
            expanded[writeIndex] = copy;
            writeIndex++;
            while (readIndex < UniqueIds.Length)
            {
                expanded[writeIndex] = UniqueIds[readIndex];
                readIndex++;
                writeIndex++;
            }
            UniqueIds = expanded;
        }
    }

    internal void EnableUtf8(bool isUtf8UserEnabled)
    {
        lock (SyncRoot)
        {
            if (State != Pop3SessionState.Authorization)
            {
                ThrowState(Pop3Commands.Utf8);
            }
            IsUtf8Enabled = true;
            IsUtf8UserEnabled = isUtf8UserEnabled;
        }
    }

    internal void SelectLanguage(string? languageTag)
    {
        lock (SyncRoot)
        {
            EnsureActive(Pop3Commands.Lang);
            SelectedLanguage = languageTag;
        }
    }

    internal void InvalidateAfterFailure()
    {
        lock (SyncRoot)
        {
            CloseState();
        }
    }

    internal void CloseSession()
    {
        lock (SyncRoot)
        {
            CloseState();
        }
    }

    internal Pop3ClientSnapshot GetSnapshot()
    {
        lock (SyncRoot)
        {
            Pop3ClientSnapshot snapshot = new Pop3ClientSnapshot();
            snapshot.State = State;
            snapshot.IsConnected = IsConnected;
            snapshot.IsSecure = IsSecure;
            snapshot.IsAuthenticated = IsAuthenticated;
            snapshot.AuthenticatedIdentity = AuthenticatedIdentity;
            snapshot.Greeting = Greeting;
            snapshot.LastResponse = CopyOptionalResponse(LastResponse);
            snapshot.Capabilities = CopyCapabilities(Capabilities);
            snapshot.MailboxStatus = CopyOptionalMailboxStatus(MailboxStatus);
            snapshot.IsUtf8Enabled = IsUtf8Enabled;
            snapshot.IsUtf8UserEnabled = IsUtf8UserEnabled;
            snapshot.SelectedLanguage = SelectedLanguage;
            snapshot.Messages = CopyMessages(Messages);
            snapshot.UniqueIds = CopyUniqueIds(UniqueIds);
            return snapshot;
        }
    }

    void CloseState()
    {
        State = Pop3SessionState.Closed;
        IsConnected = false;
        IsSecure = false;
        IsAuthenticated = false;
        AuthenticatedIdentity = null;
        GreetingTimestamp = null;
        Capabilities = new Pop3CapabilitySet();
        MailboxStatus = null;
        IsUtf8Enabled = false;
        IsUtf8UserEnabled = false;
        SelectedLanguage = null;
        Messages = new Pop3MessageInfo[0];
        UniqueIds = new Pop3UniqueId[0];
    }

    void EnsureActive(string operation)
    {
        if (State != Pop3SessionState.Authorization && State != Pop3SessionState.Transaction)
        {
            ThrowState(operation);
        }
    }

    void EnsureTransaction(string operation)
    {
        if (State != Pop3SessionState.Transaction)
        {
            ThrowState(operation);
        }
    }

    void ValidateOperation(string operation)
    {
        if (operation == null)
        {
            throw new System.ArgumentNullException("operation");
        }
        if (operation.Length == 0)
        {
            throw new System.ArgumentException("A guarded POP3 operation name cannot be empty.", "operation");
        }
    }

    void ThrowState(string operation)
    {
        throw new Pop3StateException(
            "The requested POP3 operation is not legal in the current session state.",
            operation,
            State);
    }

    Pop3Response CopyResponse(Pop3Response source)
    {
        if (source.Text == null)
        {
            throw new System.IO.InvalidDataException("A POP3 response text cannot be null.");
        }
        if (source.ResponseCodeLevels == null)
        {
            throw new System.IO.InvalidDataException("POP3 response code levels cannot be null.");
        }

        Pop3Response copy = new Pop3Response();
        copy.Kind = source.Kind;
        copy.Text = source.Text;
        copy.ResponseCodeLevels = CopyStrings(source.ResponseCodeLevels, "response code level");
        copy.GreetingTimestamp = source.GreetingTimestamp;
        copy.IsMultiline = source.IsMultiline;
        copy.AdvisoryOctetCount = source.AdvisoryOctetCount;
        return copy;
    }

    Pop3Response? CopyOptionalResponse(Pop3Response? source)
    {
        if (source == null)
        {
            return null;
        }
        return CopyResponse(source);
    }

    Pop3CapabilitySet CopyCapabilities(Pop3CapabilitySet source)
    {
        if (source.Entries == null)
        {
            throw new System.IO.InvalidDataException("POP3 capability entries cannot be null.");
        }
        if (source.SaslMechanisms == null)
        {
            throw new System.IO.InvalidDataException("POP3 SASL mechanisms cannot be null.");
        }

        Pop3CapabilitySet copy = new Pop3CapabilitySet();
        copy.Entries = CopyCapabilityEntries(source.Entries);
        copy.SupportsTop = source.SupportsTop;
        copy.SupportsUser = source.SupportsUser;
        copy.SaslMechanisms = CopyStrings(source.SaslMechanisms, "SASL mechanism");
        copy.SupportsResponseCodes = source.SupportsResponseCodes;
        copy.HasLoginDelay = source.HasLoginDelay;
        copy.LoginDelaySeconds = source.LoginDelaySeconds;
        copy.LoginDelayPerUser = source.LoginDelayPerUser;
        copy.SupportsPipelining = source.SupportsPipelining;
        copy.HasExpire = source.HasExpire;
        copy.ExpireNever = source.ExpireNever;
        copy.ExpireDays = source.ExpireDays;
        copy.ExpirePerUser = source.ExpirePerUser;
        copy.SupportsUidl = source.SupportsUidl;
        copy.Implementation = source.Implementation;
        copy.SupportsStls = source.SupportsStls;
        copy.SupportsAuthenticationResponseCode = source.SupportsAuthenticationResponseCode;
        copy.SupportsUtf8 = source.SupportsUtf8;
        copy.SupportsUtf8User = source.SupportsUtf8User;
        copy.SupportsLang = source.SupportsLang;
        return copy;
    }

    Pop3Capability[] CopyCapabilityEntries(Pop3Capability[] source)
    {
        Pop3Capability[] copy = new Pop3Capability[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            Pop3Capability item = source[index];
            if (item == null)
            {
                throw new System.IO.InvalidDataException("A POP3 capability entry cannot be null.");
            }
            if (item.Name == null)
            {
                throw new System.IO.InvalidDataException("A POP3 capability name cannot be null.");
            }
            if (item.Parameters == null)
            {
                throw new System.IO.InvalidDataException("POP3 capability parameters cannot be null.");
            }

            Pop3Capability itemCopy = new Pop3Capability();
            itemCopy.Name = item.Name;
            itemCopy.Parameters = CopyStrings(item.Parameters, "capability parameter");
            copy[index] = itemCopy;
            index++;
        }
        return copy;
    }

    Pop3MailboxStatus CopyMailboxStatus(Pop3MailboxStatus source)
    {
        Pop3MailboxStatus copy = new Pop3MailboxStatus();
        copy.MessageCount = source.MessageCount;
        copy.TotalOctets = source.TotalOctets;
        return copy;
    }

    Pop3MailboxStatus? CopyOptionalMailboxStatus(Pop3MailboxStatus? source)
    {
        if (source == null)
        {
            return null;
        }
        return CopyMailboxStatus(source);
    }

    Pop3MessageInfo[] CopyMessages(Pop3MessageInfo[] source)
    {
        Pop3MessageInfo[] copy = new Pop3MessageInfo[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            Pop3MessageInfo item = source[index];
            if (item == null)
            {
                throw new System.IO.InvalidDataException("POP3 message metadata cannot contain a null entry.");
            }
            copy[index] = CopyMessage(item);
            index++;
        }
        return copy;
    }

    Pop3MessageInfo CopyMessage(Pop3MessageInfo source)
    {
        Pop3MessageInfo copy = new Pop3MessageInfo();
        copy.MessageNumber = source.MessageNumber;
        copy.OctetCount = source.OctetCount;
        copy.IsDeleted = source.IsDeleted;
        copy.IsUtf8Representation = source.IsUtf8Representation;
        return copy;
    }

    int FindMessageIndex(int messageNumber)
    {
        int index = 0;
        while (index < Messages.Length)
        {
            if (Messages[index].MessageNumber == messageNumber)
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    Pop3UniqueId[] CopyUniqueIds(Pop3UniqueId[] source)
    {
        Pop3UniqueId[] copy = new Pop3UniqueId[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            Pop3UniqueId item = source[index];
            if (item == null)
            {
                throw new System.IO.InvalidDataException("POP3 unique identifiers cannot contain a null entry.");
            }
            if (item.Value == null)
            {
                throw new System.IO.InvalidDataException("A POP3 unique identifier value cannot be null.");
            }
            copy[index] = CopyUniqueId(item);
            index++;
        }
        return copy;
    }

    Pop3UniqueId CopyUniqueId(Pop3UniqueId source)
    {
        Pop3UniqueId copy = new Pop3UniqueId();
        copy.MessageNumber = source.MessageNumber;
        copy.Value = source.Value;
        return copy;
    }

    int FindUniqueIdIndex(int messageNumber)
    {
        int index = 0;
        while (index < UniqueIds.Length)
        {
            if (UniqueIds[index].MessageNumber == messageNumber)
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    string[] CopyStrings(string[] source, string description)
    {
        string[] copy = new string[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            if (source[index] == null)
            {
                throw new System.IO.InvalidDataException("A POP3 " + description + " cannot be null.");
            }
            copy[index] = source[index];
            index++;
        }
        return copy;
    }
}
