namespace pop3;

internal class Pop3SecretRedactor
{
    IPop3ProtocolLogger? Logger;
    object SyncRoot = new object();

    internal Pop3SecretRedactor(IPop3ProtocolLogger? logger)
    {
        Logger = logger;
    }

    internal void LogCommand(
        Pop3LogDirection direction,
        Pop3Command command,
        long byteCount,
        Pop3SessionState sessionState)
    {
        ValidateWireEntry(direction, byteCount, sessionState);
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }
        if (Logger == null)
        {
            return;
        }

        Pop3LogEntry entry = CreateEntry(direction, byteCount, sessionState);
        entry.Text = FormatCommand(command);
        entry.IsRedacted = IsSensitiveCommand(command);
        Dispatch(entry);
    }

    internal void LogResponse(
        Pop3LogDirection direction,
        Pop3Response response,
        long byteCount,
        Pop3SessionState sessionState)
    {
        ValidateWireEntry(direction, byteCount, sessionState);
        if (response == null)
        {
            throw new System.ArgumentNullException("response");
        }
        if (Logger == null)
        {
            return;
        }

        Pop3LogEntry entry = CreateEntry(direction, byteCount, sessionState);
        if (response.Kind == Pop3ResponseKind.SaslContinuation)
        {
            entry.Text = "+ [redacted]";
            entry.IsRedacted = true;
        }
        else
        {
            Pop3ResponseParser parser = new Pop3ResponseParser();
            byte[] line = parser.FormatResponse(response, int.MaxValue);
            entry.Text = System.Text.Encoding.UTF8.GetString(line);
        }
        Dispatch(entry);
    }

    internal void LogSaslPayload(
        Pop3LogDirection direction,
        long byteCount,
        Pop3SessionState sessionState)
    {
        ValidateWireEntry(direction, byteCount, sessionState);
        if (Logger == null)
        {
            return;
        }

        Pop3LogEntry entry = CreateEntry(direction, byteCount, sessionState);
        entry.Text = "SASL [redacted]";
        entry.IsRedacted = true;
        Dispatch(entry);
    }

    internal void LogMultilineData(
        Pop3LogDirection direction,
        Pop3TransferKind kind,
        int messageNumber,
        long byteCount,
        Pop3SessionState sessionState)
    {
        ValidateWireEntry(direction, byteCount, sessionState);
        ValidateTransfer(kind, messageNumber);
        if (Logger == null)
        {
            return;
        }

        Pop3LogEntry entry = CreateEntry(direction, byteCount, sessionState);
        entry.Text = FormatTransfer(kind, messageNumber);
        entry.IsRedacted = kind == Pop3TransferKind.Retrieve || kind == Pop3TransferKind.Top;
        Dispatch(entry);
    }

    internal void LogState(string text, Pop3SessionState sessionState)
    {
        ValidateState(sessionState);
        if (text == null)
        {
            throw new System.ArgumentNullException("text");
        }
        if (Logger == null)
        {
            return;
        }

        Pop3LogEntry entry = CreateEntry(Pop3LogDirection.LocalState, 0, sessionState);
        entry.Text = text;
        Dispatch(entry);
    }

    Pop3LogEntry CreateEntry(
        Pop3LogDirection direction,
        long byteCount,
        Pop3SessionState sessionState)
    {
        Pop3LogEntry entry = new Pop3LogEntry();
        entry.Direction = direction;
        entry.ByteCount = byteCount;
        entry.SessionState = sessionState;
        return entry;
    }

    string FormatCommand(Pop3Command command)
    {
        ValidateCommand(command);
        if (KeywordEquals(command.Keyword, Pop3Commands.Pass) ||
            KeywordEquals(command.Keyword, Pop3Commands.Apop))
        {
            return command.Keyword + " [redacted]";
        }
        if (KeywordEquals(command.Keyword, Pop3Commands.Auth))
        {
            if (command.Arguments.Length == 0)
            {
                return command.Keyword;
            }
            if (command.Arguments.Length == 1)
            {
                return command.Keyword + " " + command.Arguments[0];
            }
            return command.Keyword + " " + command.Arguments[0] + " [redacted]";
        }
        if (command.ContainsSensitiveData)
        {
            return command.Keyword + " [redacted]";
        }

        string text = command.Keyword;
        int index = 0;
        while (index < command.Arguments.Length)
        {
            text = text + " " + command.Arguments[index];
            index++;
        }
        return text;
    }

    bool IsSensitiveCommand(Pop3Command command)
    {
        if (KeywordEquals(command.Keyword, Pop3Commands.Pass) ||
            KeywordEquals(command.Keyword, Pop3Commands.Apop))
        {
            return true;
        }
        if (KeywordEquals(command.Keyword, Pop3Commands.Auth))
        {
            return command.Arguments.Length > 1;
        }
        return command.ContainsSensitiveData;
    }

    void ValidateCommand(Pop3Command command)
    {
        if (command.Keyword == null)
        {
            throw new System.IO.InvalidDataException("A logged POP3 command keyword cannot be null.");
        }
        if (command.Arguments == null)
        {
            throw new System.IO.InvalidDataException("Logged POP3 command arguments cannot be null.");
        }

        int index = 0;
        while (index < command.Arguments.Length)
        {
            if (command.Arguments[index] == null)
            {
                throw new System.IO.InvalidDataException("A logged POP3 command argument cannot be null.");
            }
            index++;
        }
    }

    string FormatTransfer(Pop3TransferKind kind, int messageNumber)
    {
        string text;
        switch (kind)
        {
            case Pop3TransferKind.List:
                text = "LIST data";
                break;
            case Pop3TransferKind.Uidl:
                text = "UIDL data";
                break;
            case Pop3TransferKind.Retrieve:
                text = "RETR message body [redacted]";
                break;
            case Pop3TransferKind.Top:
                text = "TOP message data [redacted]";
                break;
            default:
                text = "extension data";
                break;
        }

        if (messageNumber > 0)
        {
            return text + " for message " + messageNumber;
        }
        return text;
    }

    bool KeywordEquals(string left, string right)
    {
        return string.Equals(left, right, System.StringComparison.OrdinalIgnoreCase);
    }

    void ValidateWireEntry(
        Pop3LogDirection direction,
        long byteCount,
        Pop3SessionState sessionState)
    {
        if (direction != Pop3LogDirection.ClientToServer &&
            direction != Pop3LogDirection.ServerToClient)
        {
            throw new System.ArgumentOutOfRangeException("direction");
        }
        if (byteCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("byteCount");
        }
        ValidateState(sessionState);
    }

    void ValidateTransfer(Pop3TransferKind kind, int messageNumber)
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
    }

    void ValidateState(Pop3SessionState sessionState)
    {
        if (sessionState != Pop3SessionState.Disconnected &&
            sessionState != Pop3SessionState.Authorization &&
            sessionState != Pop3SessionState.Transaction &&
            sessionState != Pop3SessionState.Update &&
            sessionState != Pop3SessionState.Closed)
        {
            throw new System.ArgumentOutOfRangeException("sessionState");
        }
    }

    void Dispatch(Pop3LogEntry entry)
    {
        lock (SyncRoot)
        {
            if (Logger != null)
            {
                Logger.Log(entry);
            }
        }
    }
}
