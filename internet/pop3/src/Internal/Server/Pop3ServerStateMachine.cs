namespace pop3;

internal class Pop3ServerStateMachine
{
    object SyncRoot = new object();
    Pop3SessionState State;
    System.IO.Stream? RootStream;
    System.IO.Stream? TransportStream;
    bool LeaveOpen;
    bool Secure;
    bool Authenticated;
    bool Utf8Enabled;
    string? LocalEndPoint;
    string? RemoteEndPoint;
    string? SelectedLanguage;
    Pop3Response? Greeting;
    int InactivityTimeoutMilliseconds;
    long LastActivityMilliseconds;
    System.Threading.CancellationTokenSource? InactivitySource;

    internal Pop3Response OpenConnection(
        Pop3ServerConnection connection,
        Pop3ServerOptions options,
        long activityMilliseconds)
    {
        lock (SyncRoot)
        {
            if (State != Pop3SessionState.Disconnected)
            {
                throw new Pop3StateException(
                    "A POP3 server connection can only be opened from the disconnected state.",
                    "OpenConnection",
                    State);
            }
            ValidateConnection(connection);
            ValidateOptions(options);
            ValidateActivityTimestamp(activityMilliseconds);
            Pop3Response greeting = CreateGreeting(options);

            RootStream = connection.Stream;
            TransportStream = connection.Stream;
            LeaveOpen = connection.LeaveOpen;
            Secure = connection.IsSecure;
            LocalEndPoint = connection.LocalEndPoint;
            RemoteEndPoint = connection.RemoteEndPoint;
            Greeting = CopyResponse(greeting);
            InactivityTimeoutMilliseconds = options.InactivityTimeoutMilliseconds;
            LastActivityMilliseconds = activityMilliseconds;
            if (InactivityTimeoutMilliseconds > 0)
            {
                InactivitySource =
                    new System.Threading.CancellationTokenSource(
                        InactivityTimeoutMilliseconds);
            }
            State = Pop3SessionState.Authorization;
            return CopyResponse(greeting);
        }
    }

    internal Pop3SessionState GetCurrentState()
    {
        lock (SyncRoot)
        {
            return State;
        }
    }

    internal bool GetIsSecure()
    {
        lock (SyncRoot)
        {
            return Secure;
        }
    }

    internal bool GetIsAuthenticated()
    {
        lock (SyncRoot)
        {
            return Authenticated;
        }
    }

    internal bool GetIsUtf8Enabled()
    {
        lock (SyncRoot)
        {
            return Utf8Enabled;
        }
    }

    internal string? GetSelectedLanguage()
    {
        lock (SyncRoot)
        {
            return SelectedLanguage;
        }
    }

    internal Pop3Response GetGreeting()
    {
        lock (SyncRoot)
        {
            EnsureOpen("GetGreeting");
            if (Greeting == null)
            {
                throw new System.InvalidOperationException(
                    "A POP3 server greeting is not available.");
            }
            return CopyResponse(Greeting);
        }
    }

    internal Pop3ServerConnection GetConnectionContext()
    {
        lock (SyncRoot)
        {
            Pop3ServerConnection context = new Pop3ServerConnection();
            context.Stream = TransportStream;
            context.IsSecure = Secure;
            context.LocalEndPoint = LocalEndPoint;
            context.RemoteEndPoint = RemoteEndPoint;
            context.LeaveOpen = LeaveOpen;
            return context;
        }
    }

    internal System.IO.Stream GetTransportStream()
    {
        lock (SyncRoot)
        {
            EnsureOpen("GetTransportStream");
            if (TransportStream == null)
            {
                throw new Pop3StateException(
                    "A POP3 server transport is not attached.",
                    "GetTransportStream",
                    State);
            }
            return TransportStream;
        }
    }

    internal void ReplaceTransportStream(
        System.IO.Stream stream,
        bool isSecure)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }
        if (!stream.CanRead || !stream.CanWrite)
        {
            throw new System.ArgumentException(
                "A replacement POP3 server transport must be readable and writable.",
                "stream");
        }

        lock (SyncRoot)
        {
            EnsureOpen("ReplaceTransportStream");
            TransportStream = stream;
            Secure = isSecure;
        }
    }

    internal void EnterTransaction()
    {
        lock (SyncRoot)
        {
            if (State != Pop3SessionState.Authorization)
            {
                throw new Pop3StateException(
                    "A POP3 server transaction can only begin after authorization.",
                    "EnterTransaction",
                    State);
            }
            Authenticated = true;
            State = Pop3SessionState.Transaction;
        }
    }

    internal void EnterUpdate()
    {
        lock (SyncRoot)
        {
            if (State != Pop3SessionState.Transaction)
            {
                throw new Pop3StateException(
                    "A POP3 server update can only begin from a transaction.",
                    "EnterUpdate",
                    State);
            }
            State = Pop3SessionState.Update;
        }
    }

    internal void EnableUtf8Mode()
    {
        lock (SyncRoot)
        {
            if (State != Pop3SessionState.Authorization)
            {
                throw new Pop3StateException(
                    "POP3 UTF8 mode can only be enabled during authorization.",
                    "EnableUtf8Mode",
                    State);
            }
            Utf8Enabled = true;
        }
    }

    internal void SelectLanguage(string? language)
    {
        lock (SyncRoot)
        {
            EnsureOpen("SelectLanguage");
            SelectedLanguage = language;
        }
    }

    internal Pop3ExtensionContext CreateExtensionContext(
        Pop3CapabilitySet capabilities)
    {
        if (capabilities == null)
        {
            throw new System.ArgumentNullException("capabilities");
        }
        lock (SyncRoot)
        {
            EnsureOpen("CreateExtensionContext");
            Pop3ExtensionContext context = new Pop3ExtensionContext();
            context.State = State;
            context.Capabilities = capabilities;
            context.IsSecure = Secure;
            context.IsAuthenticated = Authenticated;
            context.IsUtf8Enabled = Utf8Enabled;
            context.SelectedLanguage = SelectedLanguage;
            return context;
        }
    }

    internal void RecordActivity(long activityMilliseconds)
    {
        lock (SyncRoot)
        {
            EnsureOpen("RecordActivity");
            ValidateActivityTimestamp(activityMilliseconds);
            if (activityMilliseconds < LastActivityMilliseconds)
            {
                throw new System.ArgumentOutOfRangeException(
                    "activityMilliseconds");
            }
            LastActivityMilliseconds = activityMilliseconds;
            if (InactivitySource != null)
            {
                InactivitySource.CancelAfter(InactivityTimeoutMilliseconds);
            }
        }
    }

    internal System.Threading.CancellationToken GetInactivityCancellationToken()
    {
        lock (SyncRoot)
        {
            EnsureOpen("GetInactivityCancellationToken");
            if (InactivitySource == null)
            {
                return System.Threading.CancellationToken.None;
            }
            return InactivitySource.Token;
        }
    }

    internal bool HasInactivityExpired(long activityMilliseconds)
    {
        lock (SyncRoot)
        {
            EnsureOpen("HasInactivityExpired");
            ValidateActivityTimestamp(activityMilliseconds);
            if (activityMilliseconds < LastActivityMilliseconds)
            {
                throw new System.ArgumentOutOfRangeException(
                    "activityMilliseconds");
            }
            if (InactivityTimeoutMilliseconds == 0)
            {
                return false;
            }
            return activityMilliseconds - LastActivityMilliseconds >=
                InactivityTimeoutMilliseconds;
        }
    }

    internal bool ExpireInactivity(long activityMilliseconds)
    {
        lock (SyncRoot)
        {
            EnsureOpen("ExpireInactivity");
            ValidateActivityTimestamp(activityMilliseconds);
            if (activityMilliseconds < LastActivityMilliseconds)
            {
                throw new System.ArgumentOutOfRangeException(
                    "activityMilliseconds");
            }
            if (InactivityTimeoutMilliseconds == 0 ||
                activityMilliseconds - LastActivityMilliseconds <
                    InactivityTimeoutMilliseconds)
            {
                return false;
            }
            if (InactivitySource != null)
            {
                InactivitySource.Cancel();
            }
            return true;
        }
    }

    internal long GetRemainingInactivityMilliseconds(long activityMilliseconds)
    {
        lock (SyncRoot)
        {
            EnsureOpen("GetRemainingInactivityMilliseconds");
            ValidateActivityTimestamp(activityMilliseconds);
            if (activityMilliseconds < LastActivityMilliseconds)
            {
                throw new System.ArgumentOutOfRangeException(
                    "activityMilliseconds");
            }
            if (InactivityTimeoutMilliseconds == 0)
            {
                return -1;
            }

            long elapsed = activityMilliseconds - LastActivityMilliseconds;
            if (elapsed >= InactivityTimeoutMilliseconds)
            {
                return 0;
            }
            return InactivityTimeoutMilliseconds - elapsed;
        }
    }

    internal void CloseConnection()
    {
        System.IO.Stream? transportStream = null;
        System.IO.Stream? rootStream = null;
        System.Threading.CancellationTokenSource? inactivitySource = null;
        bool disposeTransportStream = false;
        bool disposeRootStream = false;
        lock (SyncRoot)
        {
            if (State == Pop3SessionState.Closed)
            {
                return;
            }

            transportStream = TransportStream;
            rootStream = RootStream;
            disposeTransportStream = transportStream != null &&
                rootStream != null &&
                !object.ReferenceEquals(transportStream, rootStream);
            disposeRootStream = rootStream != null && !LeaveOpen;
            RootStream = null;
            TransportStream = null;
            LeaveOpen = false;
            Secure = false;
            Authenticated = false;
            Utf8Enabled = false;
            SelectedLanguage = null;
            Greeting = null;
            InactivityTimeoutMilliseconds = 0;
            LastActivityMilliseconds = 0;
            inactivitySource = InactivitySource;
            InactivitySource = null;
            State = Pop3SessionState.Closed;
        }
        if (inactivitySource != null)
        {
            inactivitySource.Dispose();
        }
        if (disposeTransportStream && transportStream != null)
        {
            transportStream.Dispose();
        }
        if (disposeRootStream && rootStream != null)
        {
            rootStream.Dispose();
        }
    }

    void ValidateConnection(Pop3ServerConnection connection)
    {
        if (connection == null)
        {
            throw new System.ArgumentNullException("connection");
        }
        if (connection.Stream == null)
        {
            throw new System.ArgumentException(
                "An accepted POP3 connection requires a transport stream.",
                "connection");
        }
        if (!connection.Stream.CanRead || !connection.Stream.CanWrite)
        {
            throw new System.ArgumentException(
                "An accepted POP3 transport stream must be readable and writable.",
                "connection");
        }
    }

    void ValidateOptions(Pop3ServerOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        if (options.InactivityTimeoutMilliseconds < 0)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }
        if (options.InactivityTimeoutMilliseconds > 0 &&
            options.InactivityTimeoutMilliseconds <
                Pop3Defaults.MinimumServerInactivityTimeoutMilliseconds)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }
        if (options.GreetingText == null)
        {
            throw new System.ArgumentException(
                "A POP3 server greeting cannot be null.",
                "options");
        }
    }

    void ValidateActivityTimestamp(long activityMilliseconds)
    {
        if (activityMilliseconds < 0)
        {
            throw new System.ArgumentOutOfRangeException(
                "activityMilliseconds");
        }
    }

    Pop3Response CreateGreeting(Pop3ServerOptions options)
    {
        Pop3Response greeting = new Pop3Response();
        greeting.Kind = Pop3ResponseKind.Positive;
        greeting.Text = options.GreetingText;
        if (options.EnableApop)
        {
            greeting.GreetingTimestamp = CreateApopTimestamp();
        }
        new Pop3ResponseParser().FormatResponse(
            greeting,
            Pop3Defaults.MaximumResponseLineOctets);
        return greeting;
    }

    string CreateApopTimestamp()
    {
        byte[] nonce = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        string nonceText = System.Convert.ToHexString(nonce).ToLowerInvariant();
        System.Array.Clear(nonce, 0, nonce.Length);
        long timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return "<" +
            timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture) +
            "." + nonceText + "@localhost>";
    }

    Pop3Response CopyResponse(Pop3Response source)
    {
        Pop3Response copy = new Pop3Response();
        copy.Kind = source.Kind;
        copy.Text = source.Text;
        copy.ResponseCodeLevels = new string[source.ResponseCodeLevels.Length];
        int index = 0;
        while (index < source.ResponseCodeLevels.Length)
        {
            copy.ResponseCodeLevels[index] = source.ResponseCodeLevels[index];
            index++;
        }
        copy.GreetingTimestamp = source.GreetingTimestamp;
        copy.IsMultiline = source.IsMultiline;
        copy.AdvisoryOctetCount = source.AdvisoryOctetCount;
        return copy;
    }

    void EnsureOpen(string operation)
    {
        if (State == Pop3SessionState.Disconnected ||
            State == Pop3SessionState.Closed)
        {
            throw new Pop3StateException(
                "The POP3 server connection is not active.",
                operation,
                State);
        }
    }
}
