namespace pop3;

internal class Pop3ClientEngine
{
    Pop3ClientStateMachine StateMachine = new Pop3ClientStateMachine();
    System.Net.Sockets.TcpClient? TcpClient;
    System.IO.Stream? TransportStream;
    Pop3WireReader? WireReader;
    Pop3WireWriter? WireWriter;
    Pop3ClientOptions? ConnectionOptions;
    Pop3CapabilityCache CapabilityCache = new Pop3CapabilityCache();
    bool DisposeTransportStream;

    internal void ConnectHost(
        Pop3ClientOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireDisconnected("ConnectHost");
        ValidateHostOptions(options);
        cancellationToken.ThrowIfCancellationRequested();

        System.Net.Sockets.TcpClient candidate = new System.Net.Sockets.TcpClient();
        ConnectTcpClient(candidate, options, cancellationToken);

        System.Net.Sockets.NetworkStream networkStream = candidate.GetStream();
        ConfigureStreamTimeouts(networkStream, options);
        System.IO.Stream stream = networkStream;
        bool isSecure = false;
        if (options.SecurityMode == Pop3SecurityMode.ImplicitTls)
        {
            stream = SecureTransport(
                networkStream,
                candidate,
                false,
                options,
                cancellationToken);
            ConfigureStreamTimeouts(stream, options);
            isSecure = true;
        }
        Pop3WireReader reader = new Pop3WireReader(stream, options.BufferSize);
        Pop3WireWriter writer = new Pop3WireWriter(stream, options.BufferSize);
        Pop3Response greeting = ReadGreeting(
            stream,
            candidate,
            true,
            reader,
            options.ReadTimeoutMilliseconds,
            cancellationToken);
        ValidateGreeting(greeting, stream, candidate, true, "ConnectHost");

        TcpClient = candidate;
        TransportStream = stream;
        WireReader = reader;
        WireWriter = writer;
        ConnectionOptions = options;
        DisposeTransportStream = true;
        StateMachine.EnterAuthorization(greeting, isSecure);
        InitializeCapabilities(options, cancellationToken);
    }

    internal void ConnectStream(
        System.IO.Stream stream,
        Pop3ClientOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireDisconnected("ConnectStream");
        ValidateStreamOptions(stream, options);
        cancellationToken.ThrowIfCancellationRequested();

        ConfigureStreamTimeouts(stream, options);
        System.IO.Stream connectedStream = stream;
        bool isSecure = false;
        if (options.SecurityMode == Pop3SecurityMode.ImplicitTls)
        {
            connectedStream = SecureTransport(
                stream,
                null,
                options.LeaveStreamOpen,
                options,
                cancellationToken);
            ConfigureStreamTimeouts(connectedStream, options);
            isSecure = true;
        }
        bool disposeConnectedStream = !options.LeaveStreamOpen ||
            (isSecure && !object.ReferenceEquals(connectedStream, stream));
        Pop3WireReader reader = new Pop3WireReader(connectedStream, options.BufferSize);
        Pop3WireWriter writer = new Pop3WireWriter(connectedStream, options.BufferSize);
        Pop3Response greeting = ReadGreeting(
            connectedStream,
            null,
            disposeConnectedStream,
            reader,
            options.ReadTimeoutMilliseconds,
            cancellationToken);
        ValidateGreeting(
            greeting,
            connectedStream,
            null,
            disposeConnectedStream,
            "ConnectStream");

        TcpClient = null;
        TransportStream = connectedStream;
        WireReader = reader;
        WireWriter = writer;
        ConnectionOptions = options;
        DisposeTransportStream = disposeConnectedStream;
        StateMachine.EnterAuthorization(greeting, isSecure);
        InitializeCapabilities(options, cancellationToken);
    }

    internal Pop3CapabilitySet RefreshCapabilities(
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireActive(Pop3Commands.Capa);
        cancellationToken.ThrowIfCancellationRequested();
        Pop3ClientOptions options = GetConnectionOptions(Pop3Commands.Capa);
        return RefreshCapabilitiesBarrier(options, cancellationToken);
    }

    internal Pop3MailboxStatus GetMailboxStatus(
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireTransaction(Pop3Commands.Stat);
        cancellationToken.ThrowIfCancellationRequested();
        Pop3ClientOptions options = GetConnectionOptions(Pop3Commands.Stat);
        Pop3Command command = new Pop3Command();
        command.Keyword = Pop3Commands.Stat;
        Pop3Response response = ExchangeSingleLineCommand(
            command,
            GetWireReader(),
            GetWireWriter(),
            options.ReadTimeoutMilliseconds,
            Pop3Commands.Stat,
            cancellationToken);
        StateMachine.TrackResponse(response);
        if (response.Kind == Pop3ResponseKind.Negative)
        {
            throw new Pop3CommandException(
                "The POP3 server rejected the STAT command.",
                command,
                response);
        }
        if (response.Kind != Pop3ResponseKind.Positive)
        {
            Pop3SessionState state = StateMachine.GetCurrentState();
            CloseConnection();
            throw new Pop3ProtocolException(
                "The POP3 server returned an invalid STAT response.",
                state,
                "STAT response",
                null);
        }

        Pop3MailboxStatus status = ParseMailboxStatusResponse(response);
        StateMachine.SetMailboxStatus(status);
        return status;
    }

    internal Pop3MessageInfo GetMessageInfo(
        int messageNumber,
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireTransaction(Pop3Commands.List);
        ValidateMessageNumber(messageNumber);
        cancellationToken.ThrowIfCancellationRequested();
        Pop3ClientOptions options = GetConnectionOptions(Pop3Commands.List);
        Pop3Command command = CreateMessageCommand(Pop3Commands.List, messageNumber);
        Pop3Response response = ExchangeSingleLineCommand(
            command,
            GetWireReader(),
            GetWireWriter(),
            options.ReadTimeoutMilliseconds,
            Pop3Commands.List,
            cancellationToken);
        StateMachine.TrackResponse(response);
        ThrowListCommandFailure(command, response);

        bool isUtf8Representation = StateMachine.GetSnapshot().IsUtf8Enabled;
        Pop3MessageInfo message = ParseListEntryResponse(
            response,
            messageNumber,
            isUtf8Representation);
        StateMachine.SetMessage(message);
        return message;
    }

    internal Pop3MessageInfo[] GetAllMessageInfo(
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireTransaction(Pop3Commands.List);
        cancellationToken.ThrowIfCancellationRequested();
        Pop3ClientOptions options = GetConnectionOptions(Pop3Commands.List);
        Pop3ClientSnapshot snapshot = StateMachine.GetSnapshot();
        Pop3Command command = new Pop3Command();
        command.Keyword = Pop3Commands.List;
        command.ExpectsMultilineResponse = true;
        Pop3WireReader reader = GetWireReader();
        Pop3Response response = ExchangeSingleLineCommand(
            command,
            reader,
            GetWireWriter(),
            options.ReadTimeoutMilliseconds,
            Pop3Commands.List,
            cancellationToken);
        if (response.Kind == Pop3ResponseKind.Positive)
        {
            response.IsMultiline = true;
        }
        StateMachine.TrackResponse(response);
        ThrowListCommandFailure(command, response);

        Pop3MessageInfo[] messages = ReadListEntries(
            reader,
            snapshot.MailboxStatus,
            snapshot.IsUtf8Enabled,
            options.ReadTimeoutMilliseconds,
            cancellationToken);
        StateMachine.SetMessages(messages);
        if (snapshot.MailboxStatus == null)
        {
            StateMachine.SetMailboxStatus(CreateMailboxStatus(messages));
        }
        return messages;
    }

    internal Pop3UniqueId GetUniqueId(
        int messageNumber,
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireTransaction(Pop3Commands.Uidl);
        ValidateMessageNumber(messageNumber);
        cancellationToken.ThrowIfCancellationRequested();
        EnsureUidlCanBeAttempted();
        Pop3ClientOptions options = GetConnectionOptions(Pop3Commands.Uidl);
        Pop3ClientSnapshot snapshot = StateMachine.GetSnapshot();
        Pop3Command command = CreateMessageCommand(Pop3Commands.Uidl, messageNumber);
        Pop3Response response = ExchangeSingleLineCommand(
            command,
            GetWireReader(),
            GetWireWriter(),
            options.ReadTimeoutMilliseconds,
            Pop3Commands.Uidl,
            cancellationToken);
        StateMachine.TrackResponse(response);
        ThrowUidlCommandFailure(command, response);

        Pop3UniqueId uniqueId = ParseUniqueIdResponse(
            response,
            messageNumber,
            snapshot.UniqueIds);
        StateMachine.SetUniqueId(uniqueId);
        return uniqueId;
    }

    internal Pop3UniqueId[] GetAllUniqueIds(
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireTransaction(Pop3Commands.Uidl);
        cancellationToken.ThrowIfCancellationRequested();
        EnsureUidlCanBeAttempted();
        Pop3ClientOptions options = GetConnectionOptions(Pop3Commands.Uidl);
        Pop3ClientSnapshot snapshot = StateMachine.GetSnapshot();
        Pop3Command command = new Pop3Command();
        command.Keyword = Pop3Commands.Uidl;
        command.ExpectsMultilineResponse = true;
        Pop3WireReader reader = GetWireReader();
        Pop3Response response = ExchangeSingleLineCommand(
            command,
            reader,
            GetWireWriter(),
            options.ReadTimeoutMilliseconds,
            Pop3Commands.Uidl,
            cancellationToken);
        if (response.Kind == Pop3ResponseKind.Positive)
        {
            response.IsMultiline = true;
        }
        StateMachine.TrackResponse(response);
        ThrowUidlCommandFailure(command, response);

        Pop3UniqueId[] uniqueIds = ReadUniqueIds(
            reader,
            snapshot,
            options.ReadTimeoutMilliseconds,
            cancellationToken);
        StateMachine.SetUniqueIds(uniqueIds);
        return uniqueIds;
    }

    internal long RetrieveMessageToStream(
        int messageNumber,
        System.IO.Stream destination,
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireTransaction(Pop3Commands.Retr);
        ValidateMessageNumber(messageNumber);
        ValidateRetrieveDestination(destination);
        cancellationToken.ThrowIfCancellationRequested();
        Pop3ClientOptions options = GetConnectionOptions(Pop3Commands.Retr);
        ValidateRetrieveOptions(options, false);
        Pop3WireReader reader = GetWireReader();
        Pop3Response response = BeginRetrieve(
            messageNumber,
            reader,
            GetWireWriter(),
            options,
            cancellationToken);
        return CopyMessageData(
            reader,
            destination,
            messageNumber,
            response.AdvisoryOctetCount,
            Pop3TransferKind.Retrieve,
            Pop3Commands.Retr,
            options,
            cancellationToken);
    }

    internal byte[] RetrieveMessageToMemory(
        int messageNumber,
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireTransaction(Pop3Commands.Retr);
        ValidateMessageNumber(messageNumber);
        cancellationToken.ThrowIfCancellationRequested();
        Pop3ClientOptions options = GetConnectionOptions(Pop3Commands.Retr);
        ValidateRetrieveOptions(options, true);
        Pop3WireReader reader = GetWireReader();
        Pop3Response response = BeginRetrieve(
            messageNumber,
            reader,
            GetWireWriter(),
            options,
            cancellationToken);
        return ReadMessageData(
            reader,
            messageNumber,
            response.AdvisoryOctetCount,
            Pop3TransferKind.Retrieve,
            Pop3Commands.Retr,
            options,
            cancellationToken);
    }

    internal long[] RetrieveMessagesToStreams(
        int[] messageNumbers,
        System.IO.Stream[] destinations,
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireTransaction(Pop3Commands.Retr);
        ValidateRetrieveStreamBatch(messageNumbers, destinations);
        cancellationToken.ThrowIfCancellationRequested();
        if (messageNumbers.Length == 0)
        {
            return new long[0];
        }
        Pop3ClientOptions options = GetConnectionOptions(Pop3Commands.Retr);
        ValidateRetrieveOptions(options, false);
        if (ShouldUsePipelining(options))
        {
            Pop3PipelineRequest[] requests = CreateRetrievePipelineRequests(
                messageNumbers,
                destinations,
                options);
            Pop3PipelineResult[] results = ExecuteClientPipeline(
                requests,
                options,
                Pop3Commands.Retr,
                cancellationToken);
            CompleteRepresentationPipelineResponses(results);
            return GetPipelineOctetCounts(results);
        }

        long[] counts = new long[messageNumbers.Length];
        int index = 0;
        while (index < messageNumbers.Length)
        {
            counts[index] = RetrieveMessageToStream(
                messageNumbers[index],
                destinations[index],
                cancellationToken);
            index++;
        }
        return counts;
    }

    internal byte[][] RetrieveMessagesToMemory(
        int[] messageNumbers,
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireTransaction(Pop3Commands.Retr);
        ValidateMessageNumberBatch(messageNumbers);
        cancellationToken.ThrowIfCancellationRequested();
        if (messageNumbers.Length == 0)
        {
            return new byte[0][];
        }
        Pop3ClientOptions options = GetConnectionOptions(Pop3Commands.Retr);
        ValidateRetrieveOptions(options, true);
        if (ShouldUsePipelining(options))
        {
            Pop3PipelineRequest[] requests = CreateRetrievePipelineRequests(
                messageNumbers,
                null,
                options);
            Pop3PipelineResult[] results = ExecuteClientPipeline(
                requests,
                options,
                Pop3Commands.Retr,
                cancellationToken);
            CompleteRepresentationPipelineResponses(results);
            return GetPipelineData(results);
        }

        byte[][] messages = new byte[messageNumbers.Length][];
        int index = 0;
        while (index < messageNumbers.Length)
        {
            messages[index] = RetrieveMessageToMemory(
                messageNumbers[index],
                cancellationToken);
            index++;
        }
        return messages;
    }

    internal long RetrieveTopToStream(
        int messageNumber,
        int bodyLineCount,
        System.IO.Stream destination,
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireTransaction(Pop3Commands.Top);
        ValidateMessageNumber(messageNumber);
        ValidateBodyLineCount(bodyLineCount);
        ValidateRetrieveDestination(destination);
        cancellationToken.ThrowIfCancellationRequested();
        EnsureTopCanBeAttempted();
        Pop3ClientOptions options = GetConnectionOptions(Pop3Commands.Top);
        ValidateRetrieveOptions(options, false);
        Pop3WireReader reader = GetWireReader();
        BeginTop(
            messageNumber,
            bodyLineCount,
            reader,
            GetWireWriter(),
            options,
            cancellationToken);
        return CopyMessageData(
            reader,
            destination,
            messageNumber,
            -1,
            Pop3TransferKind.Top,
            Pop3Commands.Top,
            options,
            cancellationToken);
    }

    internal byte[] RetrieveTopToMemory(
        int messageNumber,
        int bodyLineCount,
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireTransaction(Pop3Commands.Top);
        ValidateMessageNumber(messageNumber);
        ValidateBodyLineCount(bodyLineCount);
        cancellationToken.ThrowIfCancellationRequested();
        EnsureTopCanBeAttempted();
        Pop3ClientOptions options = GetConnectionOptions(Pop3Commands.Top);
        ValidateRetrieveOptions(options, true);
        Pop3WireReader reader = GetWireReader();
        BeginTop(
            messageNumber,
            bodyLineCount,
            reader,
            GetWireWriter(),
            options,
            cancellationToken);
        return ReadMessageData(
            reader,
            messageNumber,
            -1,
            Pop3TransferKind.Top,
            Pop3Commands.Top,
            options,
            cancellationToken);
    }

    internal long[] RetrieveTopBatchToStreams(
        int[] messageNumbers,
        int[] bodyLineCounts,
        System.IO.Stream[] destinations,
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireTransaction(Pop3Commands.Top);
        ValidateTopStreamBatch(messageNumbers, bodyLineCounts, destinations);
        cancellationToken.ThrowIfCancellationRequested();
        if (messageNumbers.Length == 0)
        {
            return new long[0];
        }
        EnsureTopCanBeAttempted();
        Pop3ClientOptions options = GetConnectionOptions(Pop3Commands.Top);
        ValidateRetrieveOptions(options, false);
        if (ShouldUsePipelining(options))
        {
            Pop3PipelineRequest[] requests = CreateTopPipelineRequests(
                messageNumbers,
                bodyLineCounts,
                destinations,
                options);
            Pop3PipelineResult[] results = ExecuteClientPipeline(
                requests,
                options,
                Pop3Commands.Top,
                cancellationToken);
            CompleteRepresentationPipelineResponses(results);
            return GetPipelineOctetCounts(results);
        }

        long[] counts = new long[messageNumbers.Length];
        int index = 0;
        while (index < messageNumbers.Length)
        {
            counts[index] = RetrieveTopToStream(
                messageNumbers[index],
                bodyLineCounts[index],
                destinations[index],
                cancellationToken);
            index++;
        }
        return counts;
    }

    internal byte[][] RetrieveTopBatchToMemory(
        int[] messageNumbers,
        int[] bodyLineCounts,
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireTransaction(Pop3Commands.Top);
        ValidateTopBatch(messageNumbers, bodyLineCounts);
        cancellationToken.ThrowIfCancellationRequested();
        if (messageNumbers.Length == 0)
        {
            return new byte[0][];
        }
        EnsureTopCanBeAttempted();
        Pop3ClientOptions options = GetConnectionOptions(Pop3Commands.Top);
        ValidateRetrieveOptions(options, true);
        if (ShouldUsePipelining(options))
        {
            Pop3PipelineRequest[] requests = CreateTopPipelineRequests(
                messageNumbers,
                bodyLineCounts,
                null,
                options);
            Pop3PipelineResult[] results = ExecuteClientPipeline(
                requests,
                options,
                Pop3Commands.Top,
                cancellationToken);
            CompleteRepresentationPipelineResponses(results);
            return GetPipelineData(results);
        }

        byte[][] messages = new byte[messageNumbers.Length][];
        int index = 0;
        while (index < messageNumbers.Length)
        {
            messages[index] = RetrieveTopToMemory(
                messageNumbers[index],
                bodyLineCounts[index],
                cancellationToken);
            index++;
        }
        return messages;
    }

    internal void DeleteMessage(
        int messageNumber,
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireTransaction(Pop3Commands.Dele);
        ValidateMessageNumber(messageNumber);
        int[] messageNumbers = new int[] { messageNumber };
        ValidateKnownDeletionTargets(messageNumbers, StateMachine.GetSnapshot());
        cancellationToken.ThrowIfCancellationRequested();
        Pop3ClientOptions options = GetConnectionOptions(Pop3Commands.Dele);
        DeleteMessageCore(
            messageNumber,
            GetWireReader(),
            GetWireWriter(),
            options,
            cancellationToken);
    }

    internal void DeleteMessages(
        int[] messageNumbers,
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireTransaction(Pop3Commands.Dele);
        ValidateDeletionBatch(messageNumbers);
        ValidateKnownDeletionTargets(messageNumbers, StateMachine.GetSnapshot());
        cancellationToken.ThrowIfCancellationRequested();
        if (messageNumbers.Length == 0)
        {
            return;
        }

        Pop3ClientOptions options = GetConnectionOptions(Pop3Commands.Dele);
        if (ShouldUsePipelining(options))
        {
            Pop3PipelineRequest[] requests = CreateDeletePipelineRequests(
                messageNumbers);
            Pop3PipelineResult[] results = ExecuteClientPipeline(
                requests,
                options,
                Pop3Commands.Dele,
                cancellationToken);
            CompleteDeletePipelineResponses(messageNumbers, results);
            return;
        }

        Pop3WireReader reader = GetWireReader();
        Pop3WireWriter writer = GetWireWriter();
        int index = 0;
        while (index < messageNumbers.Length)
        {
            DeleteMessageCore(
                messageNumbers[index],
                reader,
                writer,
                options,
                cancellationToken);
            index++;
        }
    }

    internal void DeleteAllMessages(
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireTransaction(Pop3Commands.Dele);
        cancellationToken.ThrowIfCancellationRequested();
        Pop3MessageInfo[] messages = GetAllMessageInfo(cancellationToken);
        int[] messageNumbers = new int[messages.Length];
        int index = 0;
        while (index < messages.Length)
        {
            messageNumbers[index] = messages[index].MessageNumber;
            index++;
        }
        DeleteMessages(messageNumbers, cancellationToken);
    }

    internal Pop3MailboxStatus ResetDeletions(
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireTransaction(Pop3Commands.Rset);
        cancellationToken.ThrowIfCancellationRequested();
        Pop3ClientOptions options = GetConnectionOptions(Pop3Commands.Rset);
        Pop3Command command = new Pop3Command();
        command.Keyword = Pop3Commands.Rset;
        Pop3Response response = ExchangeSingleLineCommand(
            command,
            GetWireReader(),
            GetWireWriter(),
            options.ReadTimeoutMilliseconds,
            Pop3Commands.Rset,
            cancellationToken);
        StateMachine.TrackResponse(response);
        if (response.Kind == Pop3ResponseKind.Negative)
        {
            throw new Pop3CommandException(
                "The POP3 server rejected the RSET command.",
                command,
                response);
        }
        if (response.Kind != Pop3ResponseKind.Positive)
        {
            Pop3SessionState state = StateMachine.GetCurrentState();
            CloseConnection();
            throw new Pop3ProtocolException(
                "The POP3 server returned an invalid RSET response.",
                state,
                "RSET response",
                null);
        }

        StateMachine.ResetDeletionMarks();
        return GetMailboxStatus(cancellationToken);
    }

    internal void NoOperation(
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireTransaction(Pop3Commands.Noop);
        cancellationToken.ThrowIfCancellationRequested();
        Pop3ClientOptions options = GetConnectionOptions(Pop3Commands.Noop);
        Pop3Command command = new Pop3Command();
        command.Keyword = Pop3Commands.Noop;
        Pop3Response response = ExchangeSingleLineCommand(
            command,
            GetWireReader(),
            GetWireWriter(),
            options.ReadTimeoutMilliseconds,
            Pop3Commands.Noop,
            cancellationToken);
        StateMachine.TrackResponse(response);
        if (response.Kind == Pop3ResponseKind.Negative)
        {
            throw new Pop3CommandException(
                "The POP3 server rejected the NOOP command.",
                command,
                response);
        }
        if (response.Kind != Pop3ResponseKind.Positive)
        {
            Pop3SessionState state = StateMachine.GetCurrentState();
            CloseConnection();
            throw new Pop3ProtocolException(
                "The POP3 server returned an invalid NOOP response.",
                state,
                "NOOP response",
                null);
        }
    }

    internal void EnableUtf8(
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireAuthorization(Pop3Commands.Utf8);
        cancellationToken.ThrowIfCancellationRequested();
        Pop3ClientSnapshot snapshot = StateMachine.GetSnapshot();
        if (!snapshot.Capabilities.SupportsUtf8)
        {
            throw new System.InvalidOperationException(
                "The POP3 server does not advertise UTF8.");
        }

        Pop3ClientOptions options = GetConnectionOptions(Pop3Commands.Utf8);
        Pop3Command command = new Pop3Command();
        command.Keyword = Pop3Commands.Utf8;
        Pop3Response response = ExchangeSingleLineCommand(
            command,
            GetWireReader(),
            GetWireWriter(),
            options.ReadTimeoutMilliseconds,
            Pop3Commands.Utf8,
            cancellationToken);
        StateMachine.TrackResponse(response);
        if (response.Kind == Pop3ResponseKind.Negative)
        {
            throw new Pop3CommandException(
                "The POP3 server rejected the UTF8 command.",
                command,
                response);
        }
        if (response.Kind != Pop3ResponseKind.Positive)
        {
            Pop3SessionState state = StateMachine.GetCurrentState();
            CloseConnection();
            throw new Pop3ProtocolException(
                "The POP3 server returned an invalid UTF8 response.",
                state,
                "UTF8 response",
                null);
        }
        StateMachine.EnableUtf8(snapshot.Capabilities.SupportsUtf8User);
    }

    internal Pop3Language[] GetLanguages(
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireActive(Pop3Commands.Lang);
        cancellationToken.ThrowIfCancellationRequested();
        EnsureLangCanBeAttempted();
        Pop3ClientOptions options = GetConnectionOptions(Pop3Commands.Lang);
        Pop3Command command = new Pop3Command();
        command.Keyword = Pop3Commands.Lang;
        command.ExpectsMultilineResponse = true;
        Pop3WireReader reader = GetWireReader();
        Pop3Response response = ExchangeSingleLineCommand(
            command,
            reader,
            GetWireWriter(),
            options.ReadTimeoutMilliseconds,
            Pop3Commands.Lang,
            cancellationToken);
        if (response.Kind == Pop3ResponseKind.Positive)
        {
            response.IsMultiline = true;
        }
        StateMachine.TrackResponse(response);
        ThrowLanguageCommandFailure(command, response);
        return ReadLanguageEntries(
            reader,
            options.ReadTimeoutMilliseconds,
            cancellationToken);
    }

    internal void SelectLanguage(
        string languageTag,
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireActive(Pop3Commands.Lang);
        ValidateLanguageTagArgument(languageTag);
        cancellationToken.ThrowIfCancellationRequested();
        EnsureLangCanBeAttempted();
        ExecuteLanguageSelection(languageTag, languageTag, cancellationToken);
    }

    internal void ResetLanguage(
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireActive(Pop3Commands.Lang);
        cancellationToken.ThrowIfCancellationRequested();
        EnsureLangCanBeAttempted();
        ExecuteLanguageSelection("*", null, cancellationToken);
    }

    internal Pop3Response ExecuteSingleLineExtension(
        Pop3Command command,
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireActive("Extension");
        Pop3Command extensionCommand = PrepareExtensionCommand(command, false);
        cancellationToken.ThrowIfCancellationRequested();
        Pop3ClientOptions options = GetConnectionOptions(extensionCommand.Keyword);
        Pop3Response response = ExchangeExtensionCommand(
            extensionCommand,
            options,
            cancellationToken);
        StateMachine.TrackResponse(response);
        ValidateExtensionResponse(response, extensionCommand.Keyword);
        return response;
    }

    internal Pop3Response ExecuteMultilineExtension(
        Pop3Command command,
        System.IO.Stream destination,
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireActive("Extension");
        Pop3Command extensionCommand = PrepareExtensionCommand(command, true);
        ValidateExtensionDestination(destination);
        Pop3ClientOptions options = GetConnectionOptions(extensionCommand.Keyword);
        ValidateExtensionOptions(options);
        cancellationToken.ThrowIfCancellationRequested();
        Pop3Response response = ExchangeExtensionCommand(
            extensionCommand,
            options,
            cancellationToken);
        if (response.Kind == Pop3ResponseKind.Positive)
        {
            response.IsMultiline = true;
        }
        StateMachine.TrackResponse(response);
        ValidateExtensionResponse(response, extensionCommand.Keyword);
        if (response.Kind == Pop3ResponseKind.Positive)
        {
            CopyExtensionData(
                GetWireReader(),
                destination,
                extensionCommand.Keyword,
                options,
                cancellationToken);
        }
        return response;
    }

    internal void QuitAndCommit(
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireActive(Pop3Commands.Quit);
        cancellationToken.ThrowIfCancellationRequested();
        Pop3ClientOptions options = GetConnectionOptions(Pop3Commands.Quit);
        Pop3SessionState state = StateMachine.GetCurrentState();
        Pop3Command command = new Pop3Command();
        command.Keyword = Pop3Commands.Quit;
        Pop3Response response = ExchangeSingleLineCommand(
            command,
            GetWireReader(),
            GetWireWriter(),
            options.ReadTimeoutMilliseconds,
            Pop3Commands.Quit,
            System.Threading.CancellationToken.None);
        StateMachine.TrackResponse(response);
        if (response.Kind == Pop3ResponseKind.Positive)
        {
            if (state == Pop3SessionState.Transaction)
            {
                StateMachine.EnterUpdate();
            }
            CloseConnection();
            return;
        }

        CloseConnection();
        if (response.Kind == Pop3ResponseKind.Negative)
        {
            throw new Pop3CommandException(
                "The POP3 server rejected the QUIT command.",
                command,
                response);
        }
        throw new Pop3ProtocolException(
            "The POP3 server returned an invalid QUIT response.",
            state,
            "QUIT response",
            null);
    }

    internal void Abort()
    {
        CloseConnection();
    }

    Pop3Command PrepareExtensionCommand(
        Pop3Command command,
        bool expectsMultilineResponse)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }

        Pop3Command copy = new Pop3Command();
        copy.Keyword = command.Keyword;
        copy.ContainsSensitiveData = command.ContainsSensitiveData;
        copy.ExpectsMultilineResponse = command.ExpectsMultilineResponse;
        if (command.Arguments == null)
        {
            throw new System.IO.InvalidDataException(
                "POP3 command arguments cannot be null.");
        }
        copy.Arguments = new string[command.Arguments.Length];
        int index = 0;
        while (index < command.Arguments.Length)
        {
            copy.Arguments[index] = command.Arguments[index];
            index++;
        }

        new Pop3CommandParser().FormatCommand(copy);
        copy.Keyword = copy.Keyword.ToUpperInvariant();
        if (IsStandardCommandKeyword(copy.Keyword))
        {
            throw new System.ArgumentException(
                "A standard POP3 command cannot be executed through the raw extension API.",
                "command");
        }
        if (copy.ExpectsMultilineResponse != expectsMultilineResponse)
        {
            throw new System.ArgumentException(
                "The POP3 extension response classification does not match the selected API.",
                "command");
        }
        return copy;
    }

    bool IsStandardCommandKeyword(string keyword)
    {
        return keyword == Pop3Commands.User ||
            keyword == Pop3Commands.Pass ||
            keyword == Pop3Commands.Apop ||
            keyword == Pop3Commands.Stat ||
            keyword == Pop3Commands.List ||
            keyword == Pop3Commands.Retr ||
            keyword == Pop3Commands.Dele ||
            keyword == Pop3Commands.Noop ||
            keyword == Pop3Commands.Rset ||
            keyword == Pop3Commands.Quit ||
            keyword == Pop3Commands.Top ||
            keyword == Pop3Commands.Uidl ||
            keyword == Pop3Commands.Capa ||
            keyword == Pop3Commands.Stls ||
            keyword == Pop3Commands.Auth ||
            keyword == Pop3Commands.Utf8 ||
            keyword == Pop3Commands.Lang;
    }

    void ValidateExtensionDestination(System.IO.Stream destination)
    {
        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }
        if (!destination.CanWrite)
        {
            throw new System.ArgumentException(
                "The POP3 extension destination stream must be writable.",
                "destination");
        }
    }

    void ValidateExtensionOptions(Pop3ClientOptions options)
    {
        if (options.MaximumMessageLineOctets < 3)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }
    }

    void ValidateExtensionResponse(Pop3Response response, string operation)
    {
        if (response.Kind == Pop3ResponseKind.Positive ||
            response.Kind == Pop3ResponseKind.Negative)
        {
            return;
        }

        Pop3SessionState state = StateMachine.GetCurrentState();
        CloseConnection();
        throw new Pop3ProtocolException(
            "The POP3 extension returned an invalid response status.",
            state,
            operation + " response",
            null);
    }

    Pop3Response ExchangeExtensionCommand(
        Pop3Command command,
        Pop3ClientOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3WireReader reader = GetWireReader();
        Pop3WireWriter writer = GetWireWriter();
        Pop3SessionState state = StateMachine.GetCurrentState();
        Pop3SecretRedactor redactor = new Pop3SecretRedactor(options.Logger);
        int commandOctets = new Pop3CommandParser().FormatCommand(command).Length + 2;
        using (System.Threading.CancellationTokenSource timeoutSource =
            new System.Threading.CancellationTokenSource(options.ReadTimeoutMilliseconds))
        using (System.Threading.CancellationTokenSource linkedSource =
            System.Threading.CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token))
        {
            System.Threading.Tasks.Task<Pop3Response> exchangeTask =
                System.Threading.Tasks.Task.Run(
                    () => PerformExtensionCommandExchange(
                        command,
                        reader,
                        writer,
                        redactor,
                        commandOctets,
                        state,
                        linkedSource.Token));
            System.Threading.Tasks.Task<Pop3Response> completionTask =
                exchangeTask.ContinueWith(
                    task => CompleteSingleLineExchange(
                        task,
                        command.Keyword,
                        cancellationToken,
                        timeoutSource.Token),
                    System.Threading.CancellationToken.None,
                    System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                    System.Threading.Tasks.TaskScheduler.Default);
            return completionTask.GetAwaiter().GetResult();
        }
    }

    Pop3Response PerformExtensionCommandExchange(
        Pop3Command command,
        Pop3WireReader reader,
        Pop3WireWriter writer,
        Pop3SecretRedactor redactor,
        int commandOctets,
        Pop3SessionState state,
        System.Threading.CancellationToken cancellationToken)
    {
        writer.WriteCommand(
            command,
            Pop3Defaults.MaximumCommandLineOctets,
            cancellationToken);
        redactor.LogCommand(
            Pop3LogDirection.ClientToServer,
            command,
            commandOctets,
            state);
        Pop3Response response = reader.ReadResponse(
            Pop3Defaults.MaximumResponseLineOctets,
            cancellationToken);
        redactor.LogResponse(
            Pop3LogDirection.ServerToClient,
            response,
            GetResponseWireOctetCount(response),
            state);
        return response;
    }

    long CopyExtensionData(
        Pop3WireReader reader,
        System.IO.Stream destination,
        string operation,
        Pop3ClientOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3SessionState state = StateMachine.GetCurrentState();
        Pop3SecretRedactor redactor = new Pop3SecretRedactor(options.Logger);
        using (System.Threading.CancellationTokenSource timeoutSource =
            new System.Threading.CancellationTokenSource(options.ReadTimeoutMilliseconds))
        using (System.Threading.CancellationTokenSource linkedSource =
            System.Threading.CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token))
        {
            System.Threading.Tasks.Task<long> copyTask =
                System.Threading.Tasks.Task.Run(
                    () => PerformExtensionDataCopy(
                        reader,
                        destination,
                        options,
                        redactor,
                        state,
                        linkedSource.Token));
            System.Threading.Tasks.Task<long> completionTask = copyTask.ContinueWith(
                task => CompleteMessageCopy(
                    task,
                    operation,
                    cancellationToken,
                    timeoutSource.Token),
                System.Threading.CancellationToken.None,
                System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                System.Threading.Tasks.TaskScheduler.Default);
            return completionTask.GetAwaiter().GetResult();
        }
    }

    long PerformExtensionDataCopy(
        Pop3WireReader reader,
        System.IO.Stream destination,
        Pop3ClientOptions options,
        Pop3SecretRedactor redactor,
        Pop3SessionState state,
        System.Threading.CancellationToken cancellationToken)
    {
        long octets = reader.MultilineReader.CopyMultilineData(
            destination,
            options.MaximumMessageLineOctets,
            options.TransferObserver,
            Pop3TransferKind.ExtensionData,
            0,
            -1,
            cancellationToken);
        redactor.LogMultilineData(
            Pop3LogDirection.ServerToClient,
            Pop3TransferKind.ExtensionData,
            0,
            octets,
            state);
        return octets;
    }

    bool ShouldUsePipelining(Pop3ClientOptions options)
    {
        Pop3ClientPipeline pipeline = new Pop3ClientPipeline(
            GetWireReader(),
            GetWireWriter());
        return pipeline.IsEnabled(
            options,
            StateMachine.GetSnapshot().Capabilities);
    }

    Pop3PipelineResult[] ExecuteClientPipeline(
        Pop3PipelineRequest[] requests,
        Pop3ClientOptions options,
        string operation,
        System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Pop3ClientPipeline pipeline = new Pop3ClientPipeline(
            GetWireReader(),
            GetWireWriter());
        using (System.Threading.CancellationTokenSource timeoutSource =
            new System.Threading.CancellationTokenSource(options.ReadTimeoutMilliseconds))
        using (System.Threading.CancellationTokenSource linkedSource =
            System.Threading.CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token))
        {
            System.Threading.Tasks.Task<Pop3PipelineResult[]> pipelineTask =
                System.Threading.Tasks.Task.Run(
                    () => pipeline.Execute(requests, linkedSource.Token));
            System.Threading.Tasks.Task<Pop3PipelineResult[]> completionTask =
                pipelineTask.ContinueWith(
                    task => CompleteClientPipeline(
                        task,
                        operation,
                        cancellationToken,
                        timeoutSource.Token),
                    System.Threading.CancellationToken.None,
                    System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                    System.Threading.Tasks.TaskScheduler.Default);
            return completionTask.GetAwaiter().GetResult();
        }
    }

    Pop3PipelineResult[] CompleteClientPipeline(
        System.Threading.Tasks.Task<Pop3PipelineResult[]> task,
        string operation,
        System.Threading.CancellationToken cancellationToken,
        System.Threading.CancellationToken timeoutToken)
    {
        if (task.IsCompletedSuccessfully)
        {
            return task.Result;
        }

        System.Exception? failure = GetTaskFailure(task);
        Pop3SessionState state = StateMachine.GetCurrentState();
        CloseConnection();
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        if (timeoutToken.IsCancellationRequested)
        {
            throw new Pop3ConnectionException(
                "The POP3 command pipeline timed out.",
                operation,
                state,
                false,
                true,
                failure);
        }
        System.IO.EndOfStreamException? endOfStream =
            failure as System.IO.EndOfStreamException;
        if (endOfStream != null)
        {
            throw new Pop3ConnectionException(
                "The POP3 connection ended before the command pipeline completed.",
                operation,
                state,
                true,
                false,
                endOfStream);
        }
        System.IO.InvalidDataException? invalidData =
            failure as System.IO.InvalidDataException;
        if (invalidData != null)
        {
            throw new Pop3ProtocolException(
                "The POP3 server returned malformed pipelined data.",
                state,
                operation + " pipeline",
                invalidData);
        }
        throw new Pop3ConnectionException(
            "The POP3 command pipeline failed.",
            operation,
            state,
            false,
            false,
            failure);
    }

    Pop3PipelineRequest[] CreateRetrievePipelineRequests(
        int[] messageNumbers,
        System.IO.Stream[]? destinations,
        Pop3ClientOptions options)
    {
        Pop3PipelineRequest[] requests =
            new Pop3PipelineRequest[messageNumbers.Length];
        int index = 0;
        while (index < messageNumbers.Length)
        {
            Pop3PipelineRequest request = new Pop3PipelineRequest();
            request.Command = CreateMessageCommand(
                Pop3Commands.Retr,
                messageNumbers[index]);
            request.Command.ExpectsMultilineResponse = true;
            if (destinations != null)
            {
                request.MultilineDestination = destinations[index];
            }
            request.MaximumLineOctets = options.MaximumMessageLineOctets;
            request.MemoryCeilingBytes = options.MessageMemoryCeilingBytes;
            request.TransferObserver = options.TransferObserver;
            request.TransferKind = Pop3TransferKind.Retrieve;
            request.MessageNumber = messageNumbers[index];
            requests[index] = request;
            index++;
        }
        return requests;
    }

    Pop3PipelineRequest[] CreateTopPipelineRequests(
        int[] messageNumbers,
        int[] bodyLineCounts,
        System.IO.Stream[]? destinations,
        Pop3ClientOptions options)
    {
        Pop3PipelineRequest[] requests =
            new Pop3PipelineRequest[messageNumbers.Length];
        int index = 0;
        while (index < messageNumbers.Length)
        {
            Pop3PipelineRequest request = new Pop3PipelineRequest();
            request.Command = CreateTopCommand(
                messageNumbers[index],
                bodyLineCounts[index]);
            if (destinations != null)
            {
                request.MultilineDestination = destinations[index];
            }
            request.MaximumLineOctets = options.MaximumMessageLineOctets;
            request.MemoryCeilingBytes = options.MessageMemoryCeilingBytes;
            request.TransferObserver = options.TransferObserver;
            request.TransferKind = Pop3TransferKind.Top;
            request.MessageNumber = messageNumbers[index];
            requests[index] = request;
            index++;
        }
        return requests;
    }

    Pop3PipelineRequest[] CreateDeletePipelineRequests(int[] messageNumbers)
    {
        Pop3PipelineRequest[] requests =
            new Pop3PipelineRequest[messageNumbers.Length];
        int index = 0;
        while (index < messageNumbers.Length)
        {
            Pop3PipelineRequest request = new Pop3PipelineRequest();
            request.Command = CreateMessageCommand(
                Pop3Commands.Dele,
                messageNumbers[index]);
            requests[index] = request;
            index++;
        }
        return requests;
    }

    void CompleteRepresentationPipelineResponses(
        Pop3PipelineResult[] results)
    {
        Pop3CommandException? firstFailure = null;
        int index = 0;
        while (index < results.Length)
        {
            Pop3PipelineResult result = results[index];
            StateMachine.TrackResponse(result.Response);
            if (result.Response.Kind == Pop3ResponseKind.Negative &&
                firstFailure == null)
            {
                string defaultMessage =
                    "The POP3 server rejected the RETR command.";
                if (result.Command.Keyword == Pop3Commands.Top)
                {
                    defaultMessage =
                        "The POP3 server rejected the TOP command.";
                }
                firstFailure = CreateRepresentationCommandException(
                    defaultMessage,
                    result.Command,
                    result.Response);
            }
            index++;
        }
        if (firstFailure != null)
        {
            throw firstFailure;
        }
    }

    void CompleteDeletePipelineResponses(
        int[] messageNumbers,
        Pop3PipelineResult[] results)
    {
        Pop3CommandException? firstFailure = null;
        int index = 0;
        while (index < results.Length)
        {
            Pop3PipelineResult result = results[index];
            StateMachine.TrackResponse(result.Response);
            if (result.Response.Kind == Pop3ResponseKind.Positive)
            {
                StateMachine.MarkMessageDeleted(messageNumbers[index]);
            }
            else if (firstFailure == null)
            {
                firstFailure = new Pop3CommandException(
                    "The POP3 server rejected the DELE command.",
                    result.Command,
                    result.Response);
            }
            index++;
        }
        if (firstFailure != null)
        {
            throw firstFailure;
        }
    }

    long[] GetPipelineOctetCounts(Pop3PipelineResult[] results)
    {
        long[] counts = new long[results.Length];
        int index = 0;
        while (index < results.Length)
        {
            counts[index] = results[index].MultilineOctets;
            index++;
        }
        return counts;
    }

    byte[][] GetPipelineData(Pop3PipelineResult[] results)
    {
        byte[][] data = new byte[results.Length][];
        int index = 0;
        while (index < results.Length)
        {
            data[index] = results[index].MultilineData;
            index++;
        }
        return data;
    }

    void EnsureLangCanBeAttempted()
    {
        if (!StateMachine.GetSnapshot().Capabilities.SupportsLang)
        {
            throw new System.InvalidOperationException(
                "The POP3 server does not advertise LANG.");
        }
    }

    void ValidateLanguageTagArgument(string languageTag)
    {
        if (languageTag == null)
        {
            throw new System.ArgumentNullException("languageTag");
        }
        Pop3LanguageParser parser = new Pop3LanguageParser();
        if (!parser.IsValidLanguageTag(languageTag))
        {
            throw new System.ArgumentException(
                "A selected POP3 language tag is invalid.",
                "languageTag");
        }
        if (languageTag.Length > Pop3Defaults.MaximumCommandLineOctets - 2 -
            Pop3Commands.Lang.Length - 1)
        {
            throw new System.ArgumentException(
                "The POP3 LANG command exceeds the command-line limit.",
                "languageTag");
        }
    }

    void ExecuteLanguageSelection(
        string commandArgument,
        string? selectedLanguage,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3ClientOptions options = GetConnectionOptions(Pop3Commands.Lang);
        Pop3Command command = new Pop3Command();
        command.Keyword = Pop3Commands.Lang;
        command.Arguments = new string[] { commandArgument };
        Pop3Response response = ExchangeSingleLineCommand(
            command,
            GetWireReader(),
            GetWireWriter(),
            options.ReadTimeoutMilliseconds,
            Pop3Commands.Lang,
            cancellationToken);
        StateMachine.TrackResponse(response);
        ThrowLanguageCommandFailure(command, response);
        StateMachine.SelectLanguage(selectedLanguage);
    }

    void ThrowLanguageCommandFailure(
        Pop3Command command,
        Pop3Response response)
    {
        if (response.Kind == Pop3ResponseKind.Negative)
        {
            throw new Pop3CommandException(
                "The POP3 server rejected the LANG command.",
                command,
                response);
        }
        if (response.Kind != Pop3ResponseKind.Positive)
        {
            Pop3SessionState state = StateMachine.GetCurrentState();
            CloseConnection();
            throw new Pop3ProtocolException(
                "The POP3 server returned an invalid LANG response.",
                state,
                "LANG response",
                null);
        }
    }

    Pop3Language[] ReadLanguageEntries(
        Pop3WireReader reader,
        int timeoutMilliseconds,
        System.Threading.CancellationToken cancellationToken)
    {
        using (System.Threading.CancellationTokenSource timeoutSource =
            new System.Threading.CancellationTokenSource(timeoutMilliseconds))
        using (System.Threading.CancellationTokenSource linkedSource =
            System.Threading.CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token))
        {
            System.Threading.Tasks.Task<Pop3Language[]> readTask =
                System.Threading.Tasks.Task.Run(
                    () => PerformLanguageEntriesRead(reader, linkedSource.Token));
            System.Threading.Tasks.Task<Pop3Language[]> completionTask = readTask.ContinueWith(
                task => CompleteLanguageEntriesRead(
                    task,
                    cancellationToken,
                    timeoutSource.Token),
                System.Threading.CancellationToken.None,
                System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                System.Threading.Tasks.TaskScheduler.Default);
            return completionTask.GetAwaiter().GetResult();
        }
    }

    Pop3Language[] PerformLanguageEntriesRead(
        Pop3WireReader reader,
        System.Threading.CancellationToken cancellationToken)
    {
        System.Collections.Generic.List<byte[]> lines =
            new System.Collections.Generic.List<byte[]>();
        bool complete = false;
        while (!complete)
        {
            byte[]? line = reader.LineReader.ReadResponseLine(cancellationToken);
            if (line == null)
            {
                throw new System.IO.EndOfStreamException(
                    "The POP3 stream ended before the LANG terminator.");
            }
            if (line.Length == 1 && line[0] == 46)
            {
                complete = true;
            }
            else
            {
                lines.Add(line);
            }
        }
        return new Pop3LanguageParser().ParseLanguages(lines.ToArray());
    }

    Pop3Language[] CompleteLanguageEntriesRead(
        System.Threading.Tasks.Task<Pop3Language[]> task,
        System.Threading.CancellationToken cancellationToken,
        System.Threading.CancellationToken timeoutToken)
    {
        if (task.IsCompletedSuccessfully)
        {
            return task.Result;
        }

        System.Exception? failure = GetTaskFailure(task);
        Pop3SessionState state = StateMachine.GetCurrentState();
        CloseConnection();
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        if (timeoutToken.IsCancellationRequested)
        {
            throw new Pop3ConnectionException(
                "Reading the POP3 LANG response timed out.",
                Pop3Commands.Lang,
                state,
                false,
                true,
                failure);
        }

        System.IO.EndOfStreamException? endOfStream =
            failure as System.IO.EndOfStreamException;
        if (endOfStream != null)
        {
            throw new Pop3ConnectionException(
                "The POP3 connection ended before the LANG response was complete.",
                Pop3Commands.Lang,
                state,
                true,
                false,
                endOfStream);
        }
        System.IO.InvalidDataException? invalidData =
            failure as System.IO.InvalidDataException;
        if (invalidData != null)
        {
            throw new Pop3ProtocolException(
                "The POP3 server returned malformed LANG data.",
                state,
                "LANG response",
                invalidData);
        }
        throw new Pop3ConnectionException(
            "Reading the POP3 LANG response failed.",
            Pop3Commands.Lang,
            state,
            false,
            false,
            failure);
    }

    internal Pop3CapabilitySet CompleteAuthentication(
        string? authenticatedIdentity,
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireAuthorization("Authenticate");
        if (cancellationToken.IsCancellationRequested)
        {
            CloseConnection();
            cancellationToken.ThrowIfCancellationRequested();
        }
        Pop3ClientOptions options = GetConnectionOptions("Authenticate");
        StateMachine.EnterTransaction(authenticatedIdentity);
        InvalidateCapabilityKnowledge();
        return RefreshCapabilitiesBarrier(options, cancellationToken);
    }

    internal Pop3AuthenticationResult AuthenticateUserPassword(
        Pop3Credentials credentials,
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireAuthorization("AuthenticateUserPassword");
        cancellationToken.ThrowIfCancellationRequested();
        Pop3ClientOptions options = GetConnectionOptions("AuthenticateUserPassword");
        if (options.Authentication == null)
        {
            throw new System.InvalidOperationException("POP3 authentication policy is not configured.");
        }
        return AuthenticateUserPasswordWithPolicy(
            credentials,
            options.Authentication.AllowUnprotectedCredentials,
            options,
            cancellationToken);
    }

    Pop3AuthenticationResult AuthenticateUserPasswordWithPolicy(
        Pop3Credentials credentials,
        bool allowUnprotectedCredentials,
        Pop3ClientOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireAuthorization("AuthenticateUserPassword");
        cancellationToken.ThrowIfCancellationRequested();
        ValidateUserPasswordCredentials(credentials);

        Pop3ClientSnapshot snapshot = StateMachine.GetSnapshot();
        if (!snapshot.IsSecure && !allowUnprotectedCredentials)
        {
            throw new System.InvalidOperationException(
                "USER and PASS credentials require a protected transport unless unprotected use is explicitly permitted.");
        }
        if (!CapabilityCache.CanAttemptUser())
        {
            throw new System.InvalidOperationException(
                "The POP3 server does not advertise USER and probing is not permitted.");
        }

        bool requiresUtf8 = ContainsNonAsciiCredential(credentials);
        if (requiresUtf8 && !snapshot.IsUtf8UserEnabled)
        {
            throw new System.InvalidOperationException(
                "UTF-8 username and password use requires successfully enabled UTF8 USER mode.");
        }
        ValidateCredentialLineLength(Pop3Commands.User, credentials.Username, requiresUtf8);
        ValidateCredentialLineLength(Pop3Commands.Pass, credentials.Password, requiresUtf8);

        Pop3WireReader reader = GetWireReader();
        Pop3WireWriter writer = GetWireWriter();
        Pop3Response userResponse = ExchangeCredentialCommand(
            Pop3Commands.User,
            credentials.Username,
            false,
            requiresUtf8,
            reader,
            writer,
            options,
            cancellationToken);
        StateMachine.TrackResponse(userResponse);
        if (userResponse.Kind == Pop3ResponseKind.Negative)
        {
            throw CreateAuthenticationException(
                Pop3AuthenticationMethod.UserPassword,
                userResponse);
        }
        if (userResponse.Kind != Pop3ResponseKind.Positive)
        {
            Pop3SessionState state = StateMachine.GetCurrentState();
            CloseConnection();
            throw new Pop3ProtocolException(
                "The POP3 server returned an invalid USER response.",
                state,
                "USER response",
                null);
        }

        Pop3Response passResponse = ExchangeCredentialCommand(
            Pop3Commands.Pass,
            credentials.Password,
            true,
            requiresUtf8,
            reader,
            writer,
            options,
            cancellationToken);
        StateMachine.TrackResponse(passResponse);
        if (passResponse.Kind == Pop3ResponseKind.Negative)
        {
            throw CreateAuthenticationException(
                Pop3AuthenticationMethod.UserPassword,
                passResponse);
        }
        if (passResponse.Kind != Pop3ResponseKind.Positive)
        {
            Pop3SessionState state = StateMachine.GetCurrentState();
            CloseConnection();
            throw new Pop3ProtocolException(
                "The POP3 server returned an invalid PASS response.",
                state,
                "PASS response",
                null);
        }

        CompleteAuthentication(credentials.Username, cancellationToken);
        Pop3AuthenticationResult result = new Pop3AuthenticationResult();
        result.Status = Pop3AuthenticationStatus.Success;
        result.AuthenticatedIdentity = credentials.Username;
        return result;
    }

    internal Pop3AuthenticationResult AuthenticateApop(
        Pop3Credentials credentials,
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireAuthorization("AuthenticateApop");
        cancellationToken.ThrowIfCancellationRequested();
        if (credentials == null)
        {
            throw new System.ArgumentNullException("credentials");
        }
        if (credentials.Username == null || credentials.Password == null)
        {
            throw new System.ArgumentException("APOP credentials cannot contain null values.", "credentials");
        }

        Pop3ClientOptions options = GetConnectionOptions("AuthenticateApop");
        if (options.Authentication == null)
        {
            throw new System.InvalidOperationException("POP3 authentication policy is not configured.");
        }

        string? timestamp = StateMachine.GetGreetingTimestamp();
        if (timestamp == null)
        {
            throw new System.InvalidOperationException(
                "The POP3 greeting does not contain an APOP challenge.");
        }

        Pop3ClientSnapshot snapshot = StateMachine.GetSnapshot();
        bool requiresUtf8 = ContainsNonAsciiCredential(credentials);
        if (requiresUtf8 && !snapshot.IsUtf8UserEnabled)
        {
            throw new System.InvalidOperationException(
                "UTF-8 username and password use requires successfully enabled UTF8 USER mode.");
        }

        Pop3ApopDigest digest = new Pop3ApopDigest();
        Pop3ApopRequest request = digest.CreateRequest(timestamp, credentials, requiresUtf8);
        ValidateApopLineLength(request.Username, requiresUtf8);

        Pop3WireReader reader = GetWireReader();
        Pop3WireWriter writer = GetWireWriter();
        Pop3Response response = ExchangeApopCommand(
            request,
            requiresUtf8,
            reader,
            writer,
            options,
            cancellationToken);
        StateMachine.TrackResponse(response);
        if (response.Kind == Pop3ResponseKind.Negative)
        {
            throw CreateAuthenticationException(
                Pop3AuthenticationMethod.Apop,
                response);
        }
        if (response.Kind != Pop3ResponseKind.Positive)
        {
            Pop3SessionState state = StateMachine.GetCurrentState();
            CloseConnection();
            throw new Pop3ProtocolException(
                "The POP3 server returned an invalid APOP response.",
                state,
                "APOP response",
                null);
        }

        CompleteAuthentication(credentials.Username, cancellationToken);
        Pop3AuthenticationResult result = new Pop3AuthenticationResult();
        result.Status = Pop3AuthenticationStatus.Success;
        result.AuthenticatedIdentity = credentials.Username;
        return result;
    }

    internal Pop3AuthenticationResult AuthenticateAutomatic(
        Pop3AuthenticationRequest request,
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireAuthorization("AuthenticateAutomatic");
        cancellationToken.ThrowIfCancellationRequested();
        ValidateAutomaticAuthenticationRequest(request);
        Pop3ClientOptions options = GetConnectionOptions("AuthenticateAutomatic");
        Pop3ClientSnapshot snapshot = StateMachine.GetSnapshot();

        IPop3SaslClient[] saslClients = GetAutomaticSaslClients(request);
        int clientIndex = 0;
        while (clientIndex < saslClients.Length)
        {
            IPop3SaslClient saslClient = saslClients[clientIndex];
            string mechanism = GetSaslMechanism(saslClient);
            if (IsSaslMechanismAdvertised(mechanism, snapshot.Capabilities) &&
                IsSaslMechanismPermitted(
                    mechanism,
                    snapshot.IsSecure,
                    request.AllowUnprotectedCredentials))
            {
                return AuthenticateSaslWithOptions(
                    saslClient,
                    mechanism,
                    options,
                    cancellationToken);
            }
            clientIndex++;
        }

        if (request.AllowApop && StateMachine.GetGreetingTimestamp() != null)
        {
            return AuthenticateApop(request.Credentials, cancellationToken);
        }

        if (CapabilityCache.CanAttemptUser() &&
            (snapshot.IsSecure || request.AllowUnprotectedCredentials))
        {
            return AuthenticateUserPasswordWithPolicy(
                request.Credentials,
                request.AllowUnprotectedCredentials,
                options,
                cancellationToken);
        }

        throw new System.InvalidOperationException(
            "No advertised authentication method is permitted by the current POP3 authentication policy.");
    }

    internal Pop3AuthenticationResult AuthenticateSasl(
        IPop3SaslClient saslClient,
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireAuthorization("AuthenticateSasl");
        cancellationToken.ThrowIfCancellationRequested();
        Pop3ClientOptions options = GetConnectionOptions("AuthenticateSasl");
        string mechanism = GetSaslMechanism(saslClient);
        return AuthenticateSaslWithOptions(
            saslClient,
            mechanism,
            options,
            cancellationToken);
    }

    Pop3AuthenticationResult AuthenticateSaslWithOptions(
        IPop3SaslClient saslClient,
        string mechanism,
        Pop3ClientOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireAuthorization("AuthenticateSasl");
        cancellationToken.ThrowIfCancellationRequested();
        Pop3WireReader reader = GetWireReader();
        Pop3WireWriter writer = GetWireWriter();
        Pop3SaslExchangeResult exchangeResult = ExchangeSaslAuthentication(
            saslClient,
            mechanism,
            reader,
            writer,
            options,
            cancellationToken);
        if (exchangeResult.Response != null)
        {
            StateMachine.TrackResponse(exchangeResult.Response);
        }
        if (exchangeResult.Status != Pop3SaslStepStatus.Success)
        {
            if (exchangeResult.Response != null)
            {
                throw CreateAuthenticationException(
                    Pop3AuthenticationMethod.Sasl,
                    exchangeResult.Response);
            }
            Pop3AuthenticationResult localResult = new Pop3AuthenticationResult();
            localResult.Status = Pop3AuthenticationStatus.Rejected;
            localResult.FailureText = "The selected SASL mechanism did not complete authentication.";
            return localResult;
        }

        ActivateSaslSecurityLayer(saslClient, options, cancellationToken);
        CompleteAuthentication(exchangeResult.AuthenticatedIdentity, cancellationToken);
        Pop3AuthenticationResult result = new Pop3AuthenticationResult();
        result.Status = Pop3AuthenticationStatus.Success;
        result.AuthenticatedIdentity = exchangeResult.AuthenticatedIdentity;
        return result;
    }

    internal void UpgradeToTls(System.Threading.CancellationToken cancellationToken)
    {
        Pop3ClientOptions options = GetConnectionOptions(Pop3Commands.Stls);
        UpgradeToTlsBarrier(options, false, cancellationToken);
    }

    void UpgradeToTlsBarrier(
        Pop3ClientOptions options,
        bool allowRejectedUpgrade,
        System.Threading.CancellationToken cancellationToken)
    {
        StateMachine.RequireTlsUpgrade();
        cancellationToken.ThrowIfCancellationRequested();

        System.IO.Stream plaintextStream = GetTransportStream();
        Pop3WireReader plaintextReader = GetWireReader();
        Pop3WireWriter plaintextWriter = GetWireWriter();
        bool leaveInnerStreamOpen = TcpClient == null && !DisposeTransportStream;
        ValidateTlsConfiguration(options);

        Pop3Command command = new Pop3Command();
        command.Keyword = Pop3Commands.Stls;
        Pop3Response response = ExchangeSingleLineCommand(
            command,
            plaintextReader,
            plaintextWriter,
            options.ReadTimeoutMilliseconds,
            Pop3Commands.Stls,
            cancellationToken);
        StateMachine.TrackResponse(response);
        if (response.Kind == Pop3ResponseKind.Negative)
        {
            if (allowRejectedUpgrade)
            {
                return;
            }
            if (options.SecurityMode == Pop3SecurityMode.StlsRequired)
            {
                CloseConnection();
            }
            throw new Pop3TlsException(
                "The POP3 server rejected the STLS command.",
                options.SecurityMode,
                Pop3Commands.Stls,
                null);
        }
        if (response.Kind != Pop3ResponseKind.Positive)
        {
            CloseConnection();
            throw new Pop3ProtocolException(
                "The POP3 server returned an invalid STLS response.",
                Pop3SessionState.Authorization,
                "STLS response",
                null);
        }

        InvalidateCapabilityKnowledge();
        WireReader = null;
        WireWriter = null;
        System.IO.Stream secureStream = SecureTransport(
            plaintextStream,
            TcpClient,
            leaveInnerStreamOpen,
            options,
            cancellationToken);
        bool disposeSecureStream = DisposeTransportStream ||
            !object.ReferenceEquals(secureStream, plaintextStream);
        ConfigureStreamTimeouts(secureStream, options);
        TransportStream = secureStream;
        WireReader = new Pop3WireReader(secureStream, options.BufferSize);
        WireWriter = new Pop3WireWriter(secureStream, options.BufferSize);
        DisposeTransportStream = disposeSecureStream;
        StateMachine.MarkSecure();
        RefreshCapabilitiesBarrier(options, cancellationToken);
    }

    void InitializeCapabilities(
        Pop3ClientOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3CapabilitySet capabilities = RefreshCapabilitiesBarrier(options, cancellationToken);
        if (options.SecurityMode != Pop3SecurityMode.StlsIfAvailable &&
            options.SecurityMode != Pop3SecurityMode.StlsRequired)
        {
            return;
        }

        if (capabilities.SupportsStls)
        {
            bool allowRejectedUpgrade = options.SecurityMode == Pop3SecurityMode.StlsIfAvailable;
            UpgradeToTlsBarrier(options, allowRejectedUpgrade, cancellationToken);
            return;
        }
        if (options.SecurityMode == Pop3SecurityMode.StlsRequired)
        {
            CloseConnection();
            throw new Pop3TlsException(
                "The POP3 server does not advertise the required STLS capability.",
                options.SecurityMode,
                Pop3Commands.Stls,
                null);
        }
    }

    internal Pop3ClientSnapshot GetSnapshot()
    {
        return StateMachine.GetSnapshot();
    }

    internal bool IsCapaAvailable()
    {
        return CapabilityCache.IsAvailable();
    }

    internal bool CanAttemptUserAuthentication()
    {
        return CapabilityCache.CanAttemptUser();
    }

    internal bool CanAttemptTop()
    {
        return CapabilityCache.CanAttemptTop();
    }

    internal bool CanAttemptUidl()
    {
        return CapabilityCache.CanAttemptUidl();
    }

    internal System.IO.Stream GetTransportStream()
    {
        if (TransportStream == null)
        {
            throw new Pop3StateException(
                "A POP3 transport is not attached.",
                "GetTransportStream",
                StateMachine.GetCurrentState());
        }
        return TransportStream;
    }

    internal Pop3WireReader GetWireReader()
    {
        if (WireReader == null)
        {
            throw new Pop3StateException(
                "A POP3 wire reader is not available.",
                "GetWireReader",
                StateMachine.GetCurrentState());
        }
        return WireReader;
    }

    internal Pop3WireWriter GetWireWriter()
    {
        if (WireWriter == null)
        {
            throw new Pop3StateException(
                "A POP3 wire writer is not available.",
                "GetWireWriter",
                StateMachine.GetCurrentState());
        }
        return WireWriter;
    }

    internal void CloseConnection()
    {
        System.Net.Sockets.TcpClient? client = TcpClient;
        System.IO.Stream? stream = TransportStream;
        bool disposeStream = DisposeTransportStream;
        TcpClient = null;
        TransportStream = null;
        WireReader = null;
        WireWriter = null;
        ConnectionOptions = null;
        CapabilityCache.Invalidate();
        DisposeTransportStream = false;
        if (stream != null && disposeStream)
        {
            stream.Dispose();
        }
        if (client != null)
        {
            client.Dispose();
        }
        StateMachine.CloseSession();
    }

    void ValidateHostOptions(Pop3ClientOptions options)
    {
        ValidateConnectionOptions(options);
        if (options.Host == null)
        {
            throw new System.ArgumentException("A POP3 host name is required.", "options");
        }
        if (string.IsNullOrWhiteSpace(options.Host))
        {
            throw new System.ArgumentException("A POP3 host name is required.", "options");
        }
        if (options.Port < 1 || options.Port > 65535)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }
    }

    void ValidateAutomaticAuthenticationRequest(Pop3AuthenticationRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }
        if (request.Method != Pop3AuthenticationMethod.Automatic)
        {
            throw new System.ArgumentException(
                "Automatic POP3 authentication requires the automatic method policy.",
                "request");
        }
        if (request.Credentials == null)
        {
            throw new System.ArgumentException(
                "Automatic POP3 authentication credentials cannot be null.",
                "request");
        }
        if (request.SaslClients == null)
        {
            throw new System.ArgumentException(
                "Automatic POP3 SASL clients cannot be null.",
                "request");
        }

        int index = 0;
        while (index < request.SaslClients.Length)
        {
            if (request.SaslClients[index] == null)
            {
                throw new System.ArgumentException(
                    "Automatic POP3 SASL clients cannot contain null entries.",
                    "request");
            }
            index++;
        }
    }

    IPop3SaslClient[] GetAutomaticSaslClients(Pop3AuthenticationRequest request)
    {
        if (request.SaslClients.Length > 0)
        {
            IPop3SaslClient[] clients = new IPop3SaslClient[request.SaslClients.Length];
            System.Array.Copy(request.SaslClients, clients, clients.Length);
            return clients;
        }
        if (request.SaslClient != null)
        {
            return new IPop3SaslClient[] { request.SaslClient };
        }
        return new IPop3SaslClient[0];
    }

    string GetSaslMechanism(IPop3SaslClient saslClient)
    {
        if (saslClient == null)
        {
            throw new System.ArgumentNullException("saslClient");
        }

        string mechanism = saslClient.GetMechanism();
        if (mechanism == null || mechanism.Length < 1 || mechanism.Length > 20)
        {
            throw new System.ArgumentException(
                "A POP3 SASL mechanism must contain between one and twenty characters.",
                "saslClient");
        }

        int index = 0;
        while (index < mechanism.Length)
        {
            char character = mechanism[index];
            if (!((character >= 'A' && character <= 'Z') ||
                (character >= '0' && character <= '9') ||
                character == '-' || character == '_'))
            {
                throw new System.ArgumentException(
                    "A POP3 SASL mechanism contains an invalid character.",
                    "saslClient");
            }
            index++;
        }
        return mechanism;
    }

    bool IsSaslMechanismAdvertised(string mechanism, Pop3CapabilitySet capabilities)
    {
        int index = 0;
        while (index < capabilities.SaslMechanisms.Length)
        {
            if (string.Equals(
                mechanism,
                capabilities.SaslMechanisms[index],
                System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    bool IsSaslMechanismPermitted(
        string mechanism,
        bool isSecure,
        bool allowUnprotectedCredentials)
    {
        if (isSecure || allowUnprotectedCredentials)
        {
            return true;
        }
        if (string.Equals(mechanism, "PLAIN", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mechanism, "LOGIN", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mechanism, "OAUTHBEARER", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mechanism, "XOAUTH2", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        return true;
    }

    void ValidateUserPasswordCredentials(Pop3Credentials credentials)
    {
        if (credentials == null)
        {
            throw new System.ArgumentNullException("credentials");
        }
        ValidateCredentialArgument(credentials.Username, "Username");
        ValidateCredentialArgument(credentials.Password, "Password");
    }

    void ValidateCredentialArgument(string value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentException("A POP3 " + fieldName + " value cannot be null.", "credentials");
        }
        if (value.Length == 0)
        {
            throw new System.ArgumentException("A POP3 " + fieldName + " value cannot be empty.", "credentials");
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (System.Char.IsControl(character) || System.Char.IsWhiteSpace(character))
            {
                throw new System.ArgumentException(
                    "A POP3 " + fieldName + " value cannot contain control or whitespace characters.",
                    "credentials");
            }
            if (character >= 55296 && character <= 56319)
            {
                if (index + 1 >= value.Length || value[index + 1] < 56320 || value[index + 1] > 57343)
                {
                    throw new System.ArgumentException(
                        "A POP3 " + fieldName + " value must contain valid Unicode.",
                        "credentials");
                }
                index += 2;
            }
            else if (character >= 56320 && character <= 57343)
            {
                throw new System.ArgumentException(
                    "A POP3 " + fieldName + " value must contain valid Unicode.",
                    "credentials");
            }
            else
            {
                index++;
            }
        }
    }

    bool ContainsNonAsciiCredential(Pop3Credentials credentials)
    {
        int index = 0;
        while (index < credentials.Username.Length)
        {
            if (credentials.Username[index] > 127)
            {
                return true;
            }
            index++;
        }

        index = 0;
        while (index < credentials.Password.Length)
        {
            if (credentials.Password[index] > 127)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    void ValidateCredentialLineLength(string keyword, string argument, bool useUtf8)
    {
        System.Text.Encoding encoding;
        if (useUtf8)
        {
            encoding = new System.Text.UTF8Encoding(false, true);
        }
        else
        {
            encoding = System.Text.Encoding.ASCII;
        }

        int contentOctets = keyword.Length + 1 + encoding.GetByteCount(argument);
        if (contentOctets > Pop3Defaults.MaximumCommandLineOctets - 2)
        {
            throw new System.ArgumentException(
                "A POP3 " + keyword + " credential exceeds the command-line limit.",
                "credentials");
        }
    }

    void ValidateApopLineLength(string username, bool useUtf8)
    {
        System.Text.Encoding encoding;
        if (useUtf8)
        {
            encoding = new System.Text.UTF8Encoding(false, true);
        }
        else
        {
            encoding = System.Text.Encoding.ASCII;
        }

        int contentOctets = Pop3Commands.Apop.Length + 1 +
            encoding.GetByteCount(username) + 1 + 32;
        if (contentOctets > Pop3Defaults.MaximumCommandLineOctets - 2)
        {
            throw new System.ArgumentException(
                "An APOP request exceeds the command-line limit.",
                "credentials");
        }
    }

    Pop3SaslExchangeResult ExchangeSaslAuthentication(
        IPop3SaslClient saslClient,
        string mechanism,
        Pop3WireReader reader,
        Pop3WireWriter writer,
        Pop3ClientOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        using (System.Threading.CancellationTokenSource timeoutSource =
            new System.Threading.CancellationTokenSource(options.AuthenticationTimeoutMilliseconds))
        using (System.Threading.CancellationTokenSource linkedSource =
            System.Threading.CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token))
        {
            System.Threading.Tasks.Task<Pop3SaslExchangeResult> exchangeTask =
                System.Threading.Tasks.Task.Run(
                    () => PerformSaslAuthentication(
                        saslClient,
                        mechanism,
                        reader,
                        writer,
                        options.MaximumSaslLineOctets,
                        options.Logger,
                        linkedSource.Token));
            System.Threading.Tasks.Task<Pop3SaslExchangeResult> completionTask =
                exchangeTask.ContinueWith(
                    task => CompleteSaslAuthentication(
                        task,
                        saslClient,
                        cancellationToken,
                        timeoutSource.Token),
                    System.Threading.CancellationToken.None,
                    System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                    System.Threading.Tasks.TaskScheduler.Default);
            return completionTask.GetAwaiter().GetResult();
        }
    }

    Pop3SaslExchangeResult PerformSaslAuthentication(
        IPop3SaslClient saslClient,
        string mechanism,
        Pop3WireReader reader,
        Pop3WireWriter writer,
        int maximumSaslLineOctets,
        IPop3ProtocolLogger? logger,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3SaslExchange exchange = new Pop3SaslExchange();
        exchange.SetProtocolLogger(logger);
        return exchange.AuthenticateSelectedClient(
            reader,
            writer,
            saslClient,
            mechanism,
            maximumSaslLineOctets,
            cancellationToken);
    }

    Pop3SaslExchangeResult CompleteSaslAuthentication(
        System.Threading.Tasks.Task<Pop3SaslExchangeResult> task,
        IPop3SaslClient saslClient,
        System.Threading.CancellationToken cancellationToken,
        System.Threading.CancellationToken timeoutToken)
    {
        if (task.IsCompletedSuccessfully)
        {
            return task.Result;
        }

        System.Exception? failure = GetTaskFailure(task);
        saslClient.CancelAuthentication();
        Pop3SessionState state = StateMachine.GetCurrentState();
        CloseConnection();
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        if (timeoutToken.IsCancellationRequested)
        {
            throw new Pop3ConnectionException(
                "The POP3 SASL authentication exchange timed out.",
                Pop3Commands.Auth,
                state,
                false,
                true,
                failure);
        }

        System.IO.EndOfStreamException? endOfStream = failure as System.IO.EndOfStreamException;
        if (endOfStream != null)
        {
            throw new Pop3ConnectionException(
                "The POP3 connection ended during SASL authentication.",
                Pop3Commands.Auth,
                state,
                true,
                false,
                endOfStream);
        }
        System.IO.InvalidDataException? invalidData = failure as System.IO.InvalidDataException;
        if (invalidData != null)
        {
            throw new Pop3ProtocolException(
                "The POP3 SASL authentication exchange was malformed.",
                state,
                Pop3Commands.Auth,
                invalidData);
        }
        throw new Pop3ConnectionException(
            "The POP3 SASL authentication exchange failed.",
            Pop3Commands.Auth,
            state,
            false,
            false,
            failure);
    }

    void ActivateSaslSecurityLayer(
        IPop3SaslClient saslClient,
        Pop3ClientOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        System.IO.Stream innerStream = GetTransportStream();
        Pop3WireReader innerReader = GetWireReader();
        Pop3WireWriter innerWriter = GetWireWriter();
        bool disposeInnerStream = DisposeTransportStream;
        InvalidateCapabilityKnowledge();
        WireReader = null;
        WireWriter = null;

        using (System.Threading.CancellationTokenSource timeoutSource =
            new System.Threading.CancellationTokenSource(options.AuthenticationTimeoutMilliseconds))
        {
            System.Threading.Tasks.Task<System.IO.Stream> securityTask =
                System.Threading.Tasks.Task.Run(
                    () => saslClient.ApplySecurityLayer(innerStream));
            System.Threading.Tasks.Task completionTask = securityTask.ContinueWith(
                task => CompleteSaslSecurityLayer(
                    task,
                    innerStream,
                    innerReader,
                    innerWriter,
                    disposeInnerStream,
                    options,
                    cancellationToken,
                    timeoutSource.Token),
                System.Threading.CancellationToken.None,
                System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                System.Threading.Tasks.TaskScheduler.Default);
            completionTask.GetAwaiter().GetResult();
        }
    }

    void CompleteSaslSecurityLayer(
        System.Threading.Tasks.Task<System.IO.Stream> task,
        System.IO.Stream innerStream,
        Pop3WireReader innerReader,
        Pop3WireWriter innerWriter,
        bool disposeInnerStream,
        Pop3ClientOptions options,
        System.Threading.CancellationToken cancellationToken,
        System.Threading.CancellationToken timeoutToken)
    {
        if (task.IsCompletedSuccessfully)
        {
            System.IO.Stream securityStream = task.Result;
            if (securityStream == null || !securityStream.CanRead || !securityStream.CanWrite)
            {
                if (securityStream != null && !object.ReferenceEquals(securityStream, innerStream))
                {
                    securityStream.Dispose();
                }
                CloseConnection();
                throw new Pop3ConnectionException(
                    "The POP3 SASL client returned an invalid security stream.",
                    "ApplySecurityLayer",
                    Pop3SessionState.Authorization,
                    false,
                    false,
                    null);
            }

            if (object.ReferenceEquals(securityStream, innerStream))
            {
                WireReader = innerReader;
                WireWriter = innerWriter;
                return;
            }

            innerReader.DiscardBufferedInput();
            ConfigureStreamTimeouts(securityStream, options);
            TransportStream = securityStream;
            WireReader = new Pop3WireReader(securityStream, options.BufferSize);
            WireReader.IsFirstResponse = false;
            WireWriter = new Pop3WireWriter(securityStream, options.BufferSize);
            DisposeTransportStream = disposeInnerStream ||
                !object.ReferenceEquals(securityStream, innerStream);
            StateMachine.MarkSecure();
            return;
        }

        System.Exception? failure = GetTaskFailure(task);
        CloseConnection();
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        if (timeoutToken.IsCancellationRequested)
        {
            throw new Pop3ConnectionException(
                "Applying the POP3 SASL security layer timed out.",
                "ApplySecurityLayer",
                Pop3SessionState.Authorization,
                false,
                true,
                failure);
        }
        throw new Pop3ConnectionException(
            "Applying the POP3 SASL security layer failed.",
            "ApplySecurityLayer",
            Pop3SessionState.Authorization,
            false,
            false,
            failure);
    }

    Pop3Response ExchangeApopCommand(
        Pop3ApopRequest request,
        bool useUtf8,
        Pop3WireReader reader,
        Pop3WireWriter writer,
        Pop3ClientOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        byte[] line = CreateApopLine(request, useUtf8);
        Pop3Command command = new Pop3Command();
        command.Keyword = Pop3Commands.Apop;
        command.Arguments = new string[0];
        command.ContainsSensitiveData = true;
        Pop3SecretRedactor redactor = new Pop3SecretRedactor(options.Logger);

        using (System.Threading.CancellationTokenSource timeoutSource =
            new System.Threading.CancellationTokenSource(options.AuthenticationTimeoutMilliseconds))
        using (System.Threading.CancellationTokenSource linkedSource =
            System.Threading.CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token))
        {
            System.Threading.Tasks.Task<Pop3Response> exchangeTask = System.Threading.Tasks.Task.Run(
                () => PerformCredentialExchange(
                    line,
                    command,
                    redactor,
                    reader,
                    writer,
                    linkedSource.Token));
            System.Threading.Tasks.Task<Pop3Response> completionTask = exchangeTask.ContinueWith(
                task => CompleteCredentialExchange(
                    task,
                    line,
                    Pop3Commands.Apop,
                    cancellationToken,
                    timeoutSource.Token),
                System.Threading.CancellationToken.None,
                System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                System.Threading.Tasks.TaskScheduler.Default);
            return completionTask.GetAwaiter().GetResult();
        }
    }

    byte[] CreateApopLine(Pop3ApopRequest request, bool useUtf8)
    {
        System.Text.Encoding usernameEncoding;
        if (useUtf8)
        {
            usernameEncoding = new System.Text.UTF8Encoding(false, true);
        }
        else
        {
            usernameEncoding = System.Text.Encoding.ASCII;
        }

        byte[] keywordBytes = System.Text.Encoding.ASCII.GetBytes(Pop3Commands.Apop);
        byte[] usernameBytes = usernameEncoding.GetBytes(request.Username);
        byte[] digestBytes = System.Text.Encoding.ASCII.GetBytes(request.Digest);
        byte[] line = new byte[
            keywordBytes.Length + usernameBytes.Length + digestBytes.Length + 2];
        System.Array.Copy(keywordBytes, 0, line, 0, keywordBytes.Length);
        line[keywordBytes.Length] = 32;
        System.Array.Copy(
            usernameBytes,
            0,
            line,
            keywordBytes.Length + 1,
            usernameBytes.Length);
        line[keywordBytes.Length + usernameBytes.Length + 1] = 32;
        System.Array.Copy(
            digestBytes,
            0,
            line,
            keywordBytes.Length + usernameBytes.Length + 2,
            digestBytes.Length);
        System.Array.Clear(usernameBytes, 0, usernameBytes.Length);
        System.Array.Clear(digestBytes, 0, digestBytes.Length);
        request.Timestamp = "";
        request.Digest = "";
        return line;
    }

    Pop3Response ExchangeCredentialCommand(
        string keyword,
        string argument,
        bool containsSensitiveData,
        bool useUtf8,
        Pop3WireReader reader,
        Pop3WireWriter writer,
        Pop3ClientOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        byte[] line = CreateCredentialLine(keyword, argument, useUtf8);
        Pop3Command command = new Pop3Command();
        command.Keyword = keyword;
        command.Arguments = new string[] { argument };
        command.ContainsSensitiveData = containsSensitiveData;
        Pop3SecretRedactor redactor = new Pop3SecretRedactor(options.Logger);

        using (System.Threading.CancellationTokenSource timeoutSource =
            new System.Threading.CancellationTokenSource(options.AuthenticationTimeoutMilliseconds))
        using (System.Threading.CancellationTokenSource linkedSource =
            System.Threading.CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token))
        {
            System.Threading.Tasks.Task<Pop3Response> exchangeTask = System.Threading.Tasks.Task.Run(
                () => PerformCredentialExchange(
                    line,
                    command,
                    redactor,
                    reader,
                    writer,
                    linkedSource.Token));
            System.Threading.Tasks.Task<Pop3Response> completionTask = exchangeTask.ContinueWith(
                task => CompleteCredentialExchange(
                    task,
                    line,
                    keyword,
                    cancellationToken,
                    timeoutSource.Token),
                System.Threading.CancellationToken.None,
                System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                System.Threading.Tasks.TaskScheduler.Default);
            return completionTask.GetAwaiter().GetResult();
        }
    }

    byte[] CreateCredentialLine(string keyword, string argument, bool useUtf8)
    {
        System.Text.Encoding encoding;
        if (useUtf8)
        {
            encoding = new System.Text.UTF8Encoding(false, true);
        }
        else
        {
            encoding = System.Text.Encoding.ASCII;
        }

        byte[] keywordBytes = System.Text.Encoding.ASCII.GetBytes(keyword);
        byte[] argumentBytes = encoding.GetBytes(argument);
        byte[] line = new byte[keywordBytes.Length + argumentBytes.Length + 1];
        System.Array.Copy(keywordBytes, 0, line, 0, keywordBytes.Length);
        line[keywordBytes.Length] = 32;
        System.Array.Copy(argumentBytes, 0, line, keywordBytes.Length + 1, argumentBytes.Length);
        if (keyword == Pop3Commands.Pass)
        {
            System.Array.Clear(argumentBytes, 0, argumentBytes.Length);
        }
        return line;
    }

    Pop3Response PerformCredentialExchange(
        byte[] line,
        Pop3Command command,
        Pop3SecretRedactor redactor,
        Pop3WireReader reader,
        Pop3WireWriter writer,
        System.Threading.CancellationToken cancellationToken)
    {
        writer.LineWriter.WriteDataLine(
            line,
            Pop3Defaults.MaximumCommandLineOctets,
            cancellationToken);
        redactor.LogCommand(
            Pop3LogDirection.ClientToServer,
            command,
            line.Length + 2,
            Pop3SessionState.Authorization);
        Pop3Response response = reader.ReadResponse(
            Pop3Defaults.MaximumResponseLineOctets,
            cancellationToken);
        redactor.LogResponse(
            Pop3LogDirection.ServerToClient,
            response,
            GetResponseWireOctetCount(response),
            Pop3SessionState.Authorization);
        return response;
    }

    Pop3Response CompleteCredentialExchange(
        System.Threading.Tasks.Task<Pop3Response> task,
        byte[] line,
        string operation,
        System.Threading.CancellationToken cancellationToken,
        System.Threading.CancellationToken timeoutToken)
    {
        System.Array.Clear(line, 0, line.Length);
        return CompleteSingleLineExchange(
            task,
            operation,
            cancellationToken,
            timeoutToken);
    }

    int GetResponseWireOctetCount(Pop3Response response)
    {
        Pop3ResponseParser parser = new Pop3ResponseParser();
        byte[] line = parser.FormatResponse(response, Pop3Defaults.MaximumResponseLineOctets);
        return line.Length + 2;
    }

    Pop3AuthenticationException CreateAuthenticationException(
        Pop3AuthenticationMethod method,
        Pop3Response response)
    {
        Pop3ResponseCodeParser parser = new Pop3ResponseCodeParser();
        Pop3AuthenticationResult result = new Pop3AuthenticationResult();
        result.Status = parser.ClassifyAuthenticationStatus(response.ResponseCodeLevels);
        result.FailureText = response.Text;
        if (result.Status == Pop3AuthenticationStatus.LoginDelay)
        {
            Pop3ClientSnapshot snapshot = StateMachine.GetSnapshot();
            if (snapshot.Capabilities.HasLoginDelay)
            {
                result.RetryAfterSeconds = snapshot.Capabilities.LoginDelaySeconds;
            }
        }
        return new Pop3AuthenticationException(
            "The POP3 server rejected the authentication attempt.",
            method,
            result,
            response);
    }

    void ValidateStreamOptions(System.IO.Stream stream, Pop3ClientOptions options)
    {
        ValidateConnectionOptions(options);
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }
        if (!stream.CanRead)
        {
            throw new System.ArgumentException("The connected POP3 stream must be readable.", "stream");
        }
        if (!stream.CanWrite)
        {
            throw new System.ArgumentException("The connected POP3 stream must be writable.", "stream");
        }
    }

    void ValidateConnectionOptions(Pop3ClientOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        if (options.ConnectTimeoutMilliseconds <= 0)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }
        if (options.ReadTimeoutMilliseconds <= 0)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }
        if (options.WriteTimeoutMilliseconds <= 0)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }
        if (options.AuthenticationTimeoutMilliseconds <= 0)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }
        if (options.BufferSize <= 0)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }
        if (options.SecurityMode != Pop3SecurityMode.Plaintext &&
            options.SecurityMode != Pop3SecurityMode.StlsIfAvailable &&
            options.SecurityMode != Pop3SecurityMode.StlsRequired &&
            options.SecurityMode != Pop3SecurityMode.ImplicitTls)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }
        if (options.SecurityMode == Pop3SecurityMode.StlsIfAvailable ||
            options.SecurityMode == Pop3SecurityMode.StlsRequired ||
            options.SecurityMode == Pop3SecurityMode.ImplicitTls)
        {
            ValidateTlsConfiguration(options);
        }
    }

    void ValidateTlsConfiguration(Pop3ClientOptions options)
    {
        if (options.Tls == null)
        {
            throw new System.ArgumentException("POP3 client TLS options cannot be null.", "options");
        }
        if (options.Tls.ClientCertificates == null)
        {
            throw new System.ArgumentException("POP3 client certificates cannot be null.", "options");
        }

        string? targetHost = options.Tls.TargetHost;
        if ((targetHost == null || string.IsNullOrWhiteSpace(targetHost)) &&
            (options.Host == null || string.IsNullOrWhiteSpace(options.Host)))
        {
            throw new System.ArgumentException("A target host is required for POP3 TLS.", "options");
        }
    }

    void ConfigureStreamTimeouts(System.IO.Stream stream, Pop3ClientOptions options)
    {
        if (stream.CanTimeout)
        {
            stream.ReadTimeout = options.ReadTimeoutMilliseconds;
            stream.WriteTimeout = options.WriteTimeoutMilliseconds;
        }
    }

    Pop3ClientOptions GetConnectionOptions(string operation)
    {
        if (ConnectionOptions == null)
        {
            throw new Pop3StateException(
                "POP3 connection options are not available.",
                operation,
                StateMachine.GetCurrentState());
        }
        return ConnectionOptions;
    }

    Pop3Response ExchangeSingleLineCommand(
        Pop3Command command,
        Pop3WireReader reader,
        Pop3WireWriter writer,
        int timeoutMilliseconds,
        string operation,
        System.Threading.CancellationToken cancellationToken)
    {
        using (System.Threading.CancellationTokenSource timeoutSource =
            new System.Threading.CancellationTokenSource(timeoutMilliseconds))
        using (System.Threading.CancellationTokenSource linkedSource =
            System.Threading.CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token))
        {
            System.Threading.Tasks.Task<Pop3Response> exchangeTask = System.Threading.Tasks.Task.Run(
                () => PerformSingleLineExchange(command, reader, writer, linkedSource.Token));
            System.Threading.Tasks.Task<Pop3Response> completionTask = exchangeTask.ContinueWith(
                task => CompleteSingleLineExchange(
                    task,
                    operation,
                    cancellationToken,
                    timeoutSource.Token),
                System.Threading.CancellationToken.None,
                System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                System.Threading.Tasks.TaskScheduler.Default);
            return completionTask.GetAwaiter().GetResult();
        }
    }

    Pop3Response PerformSingleLineExchange(
        Pop3Command command,
        Pop3WireReader reader,
        Pop3WireWriter writer,
        System.Threading.CancellationToken cancellationToken)
    {
        writer.WriteCommand(
            command,
            Pop3Defaults.MaximumCommandLineOctets,
            cancellationToken);
        return reader.ReadResponse(
            Pop3Defaults.MaximumResponseLineOctets,
            cancellationToken);
    }

    Pop3Response CompleteSingleLineExchange(
        System.Threading.Tasks.Task<Pop3Response> task,
        string operation,
        System.Threading.CancellationToken cancellationToken,
        System.Threading.CancellationToken timeoutToken)
    {
        if (task.IsCompletedSuccessfully)
        {
            return task.Result;
        }

        System.Exception? failure = GetTaskFailure(task);
        Pop3SessionState state = StateMachine.GetCurrentState();
        CloseConnection();
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        if (timeoutToken.IsCancellationRequested)
        {
            throw new Pop3ConnectionException(
                "The POP3 command exchange timed out.",
                operation,
                state,
                false,
                true,
                failure);
        }

        System.IO.EndOfStreamException? endOfStream = failure as System.IO.EndOfStreamException;
        if (endOfStream != null)
        {
            throw new Pop3ConnectionException(
                "The POP3 connection ended during a command exchange.",
                operation,
                state,
                true,
                false,
                endOfStream);
        }
        System.IO.InvalidDataException? invalidData = failure as System.IO.InvalidDataException;
        if (invalidData != null)
        {
            throw new Pop3ProtocolException(
                "The POP3 server returned a malformed command response.",
                state,
                operation,
                invalidData);
        }
        throw new Pop3ConnectionException(
            "The POP3 command exchange failed.",
            operation,
            state,
            false,
            false,
            failure);
    }

    Pop3MailboxStatus ParseMailboxStatusResponse(Pop3Response response)
    {
        System.Threading.Tasks.Task<Pop3MailboxStatus> parseTask =
            System.Threading.Tasks.Task.Run(() => PerformMailboxStatusParse(response));
        System.Threading.Tasks.Task<Pop3MailboxStatus> completionTask = parseTask.ContinueWith(
            task => CompleteMailboxStatusParse(task),
            System.Threading.CancellationToken.None,
            System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
            System.Threading.Tasks.TaskScheduler.Default);
        return completionTask.GetAwaiter().GetResult();
    }

    Pop3MailboxStatus PerformMailboxStatusParse(Pop3Response response)
    {
        string payload = GetResponsePayload(response);
        byte[] payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);
        Pop3MailboxStatus status = new Pop3ListParser().ParseMailboxStatus(payloadBytes);
        if (status.MessageCount == 0 && status.TotalOctets != 0)
        {
            throw new System.IO.InvalidDataException(
                "An empty POP3 maildrop cannot have a nonzero total size.");
        }
        return status;
    }

    string GetResponsePayload(Pop3Response response)
    {
        if (response.ResponseCodeLevels.Length == 0)
        {
            return response.Text;
        }

        int responseCodeEnd = response.Text.IndexOf(']');
        int payloadStart = responseCodeEnd + 1;
        if (payloadStart >= response.Text.Length || response.Text[payloadStart] != ' ')
        {
            throw new System.IO.InvalidDataException(
                "A POP3 response code must be followed by its command payload.");
        }
        payloadStart++;
        return response.Text.Substring(payloadStart);
    }

    Pop3MailboxStatus CompleteMailboxStatusParse(
        System.Threading.Tasks.Task<Pop3MailboxStatus> task)
    {
        if (task.IsCompletedSuccessfully)
        {
            return task.Result;
        }

        System.Exception? failure = GetTaskFailure(task);
        Pop3SessionState state = StateMachine.GetCurrentState();
        CloseConnection();
        throw new Pop3ProtocolException(
            "The POP3 server returned a malformed or contradictory STAT response.",
            state,
            "STAT response",
            failure);
    }

    void ValidateMessageNumber(int messageNumber)
    {
        if (messageNumber < 1)
        {
            throw new System.ArgumentOutOfRangeException("messageNumber");
        }
    }

    Pop3Command CreateMessageCommand(string keyword, int messageNumber)
    {
        Pop3Command command = new Pop3Command();
        command.Keyword = keyword;
        command.Arguments = new string[]
        {
            messageNumber.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
        return command;
    }

    void ThrowListCommandFailure(Pop3Command command, Pop3Response response)
    {
        if (response.Kind == Pop3ResponseKind.Negative)
        {
            throw CreateRepresentationCommandException(
                "The POP3 server rejected the LIST command.",
                command,
                response);
        }
        if (response.Kind != Pop3ResponseKind.Positive)
        {
            Pop3SessionState state = StateMachine.GetCurrentState();
            CloseConnection();
            throw new Pop3ProtocolException(
                "The POP3 server returned an invalid LIST response.",
                state,
                "LIST response",
                null);
        }
    }

    Pop3CommandException CreateRepresentationCommandException(
        string defaultMessage,
        Pop3Command command,
        Pop3Response response)
    {
        Pop3ResponseCodeParser parser = new Pop3ResponseCodeParser();
        string responseCode = parser.GetRegisteredResponseCode(
            response.ResponseCodeLevels);
        if (responseCode == Pop3ResponseCodes.Utf8)
        {
            return new Pop3CommandException(
                "The POP3 server requires UTF8 message mode. Reconnect, call EnableUtf8 before authentication, and retry the command.",
                command,
                response);
        }
        return new Pop3CommandException(defaultMessage, command, response);
    }

    Pop3MessageInfo ParseListEntryResponse(
        Pop3Response response,
        int expectedMessageNumber,
        bool isUtf8Representation)
    {
        System.Threading.Tasks.Task<Pop3MessageInfo> parseTask =
            System.Threading.Tasks.Task.Run(
                () => PerformListEntryParse(
                    response,
                    expectedMessageNumber,
                    isUtf8Representation));
        System.Threading.Tasks.Task<Pop3MessageInfo> completionTask = parseTask.ContinueWith(
            task => CompleteListEntryParse(task),
            System.Threading.CancellationToken.None,
            System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
            System.Threading.Tasks.TaskScheduler.Default);
        return completionTask.GetAwaiter().GetResult();
    }

    Pop3MessageInfo PerformListEntryParse(
        Pop3Response response,
        int expectedMessageNumber,
        bool isUtf8Representation)
    {
        string payload = GetResponsePayload(response);
        byte[] payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);
        Pop3MessageInfo message = new Pop3ListParser().ParseListEntry(payloadBytes);
        if (message.MessageNumber != expectedMessageNumber)
        {
            throw new System.IO.InvalidDataException(
                "The POP3 LIST response does not match the requested message number.");
        }
        message.IsUtf8Representation = isUtf8Representation;
        return message;
    }

    Pop3MessageInfo CompleteListEntryParse(
        System.Threading.Tasks.Task<Pop3MessageInfo> task)
    {
        if (task.IsCompletedSuccessfully)
        {
            return task.Result;
        }

        System.Exception? failure = GetTaskFailure(task);
        Pop3SessionState state = StateMachine.GetCurrentState();
        CloseConnection();
        throw new Pop3ProtocolException(
            "The POP3 server returned a malformed or contradictory LIST response.",
            state,
            "LIST response",
            failure);
    }

    Pop3MessageInfo[] ReadListEntries(
        Pop3WireReader reader,
        Pop3MailboxStatus? expectedStatus,
        bool isUtf8Representation,
        int timeoutMilliseconds,
        System.Threading.CancellationToken cancellationToken)
    {
        using (System.Threading.CancellationTokenSource timeoutSource =
            new System.Threading.CancellationTokenSource(timeoutMilliseconds))
        using (System.Threading.CancellationTokenSource linkedSource =
            System.Threading.CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token))
        {
            System.Threading.Tasks.Task<Pop3MessageInfo[]> readTask =
                System.Threading.Tasks.Task.Run(
                    () => PerformListEntriesRead(
                        reader,
                        expectedStatus,
                        isUtf8Representation,
                        linkedSource.Token));
            System.Threading.Tasks.Task<Pop3MessageInfo[]> completionTask = readTask.ContinueWith(
                task => CompleteListEntriesRead(
                    task,
                    cancellationToken,
                    timeoutSource.Token),
                System.Threading.CancellationToken.None,
                System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                System.Threading.Tasks.TaskScheduler.Default);
            return completionTask.GetAwaiter().GetResult();
        }
    }

    Pop3MessageInfo[] PerformListEntriesRead(
        Pop3WireReader reader,
        Pop3MailboxStatus? expectedStatus,
        bool isUtf8Representation,
        System.Threading.CancellationToken cancellationToken)
    {
        System.Collections.Generic.List<byte[]> lines =
            new System.Collections.Generic.List<byte[]>();
        bool complete = false;
        while (!complete)
        {
            byte[]? line = reader.LineReader.ReadResponseLine(cancellationToken);
            if (line == null)
            {
                throw new System.IO.EndOfStreamException(
                    "The POP3 stream ended before the LIST terminator.");
            }
            if (line.Length == 1 && line[0] == 46)
            {
                complete = true;
            }
            else
            {
                lines.Add(line);
            }
        }

        Pop3MessageInfo[] messages = new Pop3ListParser().ParseListEntries(
            lines.ToArray(),
            expectedStatus);
        int index = 0;
        while (index < messages.Length)
        {
            messages[index].IsUtf8Representation = isUtf8Representation;
            index++;
        }
        return messages;
    }

    Pop3MessageInfo[] CompleteListEntriesRead(
        System.Threading.Tasks.Task<Pop3MessageInfo[]> task,
        System.Threading.CancellationToken cancellationToken,
        System.Threading.CancellationToken timeoutToken)
    {
        if (task.IsCompletedSuccessfully)
        {
            return task.Result;
        }

        System.Exception? failure = GetTaskFailure(task);
        Pop3SessionState state = StateMachine.GetCurrentState();
        CloseConnection();
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        if (timeoutToken.IsCancellationRequested)
        {
            throw new Pop3ConnectionException(
                "Reading the POP3 LIST response timed out.",
                Pop3Commands.List,
                state,
                false,
                true,
                failure);
        }

        System.IO.EndOfStreamException? endOfStream = failure as System.IO.EndOfStreamException;
        if (endOfStream != null)
        {
            throw new Pop3ConnectionException(
                "The POP3 connection ended before the LIST response was complete.",
                Pop3Commands.List,
                state,
                true,
                false,
                endOfStream);
        }
        System.IO.InvalidDataException? invalidData = failure as System.IO.InvalidDataException;
        if (invalidData != null)
        {
            throw new Pop3ProtocolException(
                "The POP3 server returned malformed or contradictory LIST data.",
                state,
                "LIST response",
                invalidData);
        }
        throw new Pop3ConnectionException(
            "Reading the POP3 LIST response failed.",
            Pop3Commands.List,
            state,
            false,
            false,
            failure);
    }

    Pop3MailboxStatus CreateMailboxStatus(Pop3MessageInfo[] messages)
    {
        Pop3MailboxStatus status = new Pop3MailboxStatus();
        status.MessageCount = messages.Length;
        int index = 0;
        while (index < messages.Length)
        {
            status.TotalOctets += messages[index].OctetCount;
            index++;
        }
        return status;
    }

    void EnsureUidlCanBeAttempted()
    {
        if (!CapabilityCache.CanAttemptUidl())
        {
            throw new System.InvalidOperationException(
                "The POP3 server does not advertise UIDL and probing is not permitted.");
        }
    }

    void ThrowUidlCommandFailure(Pop3Command command, Pop3Response response)
    {
        if (response.Kind == Pop3ResponseKind.Negative)
        {
            throw new Pop3CommandException(
                "The POP3 server rejected the UIDL command.",
                command,
                response);
        }
        if (response.Kind != Pop3ResponseKind.Positive)
        {
            Pop3SessionState state = StateMachine.GetCurrentState();
            CloseConnection();
            throw new Pop3ProtocolException(
                "The POP3 server returned an invalid UIDL response.",
                state,
                "UIDL response",
                null);
        }
    }

    Pop3UniqueId ParseUniqueIdResponse(
        Pop3Response response,
        int expectedMessageNumber,
        Pop3UniqueId[] knownUniqueIds)
    {
        System.Threading.Tasks.Task<Pop3UniqueId> parseTask =
            System.Threading.Tasks.Task.Run(
                () => PerformUniqueIdParse(
                    response,
                    expectedMessageNumber,
                    knownUniqueIds));
        System.Threading.Tasks.Task<Pop3UniqueId> completionTask = parseTask.ContinueWith(
            task => CompleteUniqueIdParse(task),
            System.Threading.CancellationToken.None,
            System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
            System.Threading.Tasks.TaskScheduler.Default);
        return completionTask.GetAwaiter().GetResult();
    }

    Pop3UniqueId PerformUniqueIdParse(
        Pop3Response response,
        int expectedMessageNumber,
        Pop3UniqueId[] knownUniqueIds)
    {
        string payload = GetResponsePayload(response);
        byte[] payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);
        Pop3UniqueId uniqueId = new Pop3UidlParser().ParseSingleUniqueIdResponse(payloadBytes);
        if (uniqueId.MessageNumber != expectedMessageNumber)
        {
            throw new System.IO.InvalidDataException(
                "The POP3 UIDL response does not match the requested message number.");
        }
        ValidateStableUniqueId(uniqueId, knownUniqueIds);
        return uniqueId;
    }

    Pop3UniqueId CompleteUniqueIdParse(
        System.Threading.Tasks.Task<Pop3UniqueId> task)
    {
        if (task.IsCompletedSuccessfully)
        {
            return task.Result;
        }

        System.Exception? failure = GetTaskFailure(task);
        Pop3SessionState state = StateMachine.GetCurrentState();
        CloseConnection();
        throw new Pop3ProtocolException(
            "The POP3 server returned a malformed or contradictory UIDL response.",
            state,
            "UIDL response",
            failure);
    }

    Pop3UniqueId[] ReadUniqueIds(
        Pop3WireReader reader,
        Pop3ClientSnapshot snapshot,
        int timeoutMilliseconds,
        System.Threading.CancellationToken cancellationToken)
    {
        using (System.Threading.CancellationTokenSource timeoutSource =
            new System.Threading.CancellationTokenSource(timeoutMilliseconds))
        using (System.Threading.CancellationTokenSource linkedSource =
            System.Threading.CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token))
        {
            System.Threading.Tasks.Task<Pop3UniqueId[]> readTask =
                System.Threading.Tasks.Task.Run(
                    () => PerformUniqueIdsRead(reader, snapshot, linkedSource.Token));
            System.Threading.Tasks.Task<Pop3UniqueId[]> completionTask = readTask.ContinueWith(
                task => CompleteUniqueIdsRead(
                    task,
                    cancellationToken,
                    timeoutSource.Token),
                System.Threading.CancellationToken.None,
                System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                System.Threading.Tasks.TaskScheduler.Default);
            return completionTask.GetAwaiter().GetResult();
        }
    }

    Pop3UniqueId[] PerformUniqueIdsRead(
        Pop3WireReader reader,
        Pop3ClientSnapshot snapshot,
        System.Threading.CancellationToken cancellationToken)
    {
        System.Collections.Generic.List<byte[]> lines =
            new System.Collections.Generic.List<byte[]>();
        bool complete = false;
        while (!complete)
        {
            byte[]? line = reader.LineReader.ReadResponseLine(cancellationToken);
            if (line == null)
            {
                throw new System.IO.EndOfStreamException(
                    "The POP3 stream ended before the UIDL terminator.");
            }
            if (line.Length == 1 && line[0] == 46)
            {
                complete = true;
            }
            else
            {
                lines.Add(line);
            }
        }

        Pop3UniqueId[] uniqueIds = new Pop3UidlParser().ParseUniqueIds(
            lines.ToArray(),
            null);
        ValidateUniqueIdBatch(uniqueIds, snapshot);
        return uniqueIds;
    }

    void ValidateUniqueIdBatch(
        Pop3UniqueId[] uniqueIds,
        Pop3ClientSnapshot snapshot)
    {
        if (snapshot.MailboxStatus != null &&
            snapshot.MailboxStatus.MessageCount != uniqueIds.Length)
        {
            throw new System.IO.InvalidDataException(
                "The POP3 UIDL entry count does not match the known mailbox count.");
        }

        int index = 0;
        while (index < uniqueIds.Length)
        {
            ValidateStableUniqueId(uniqueIds[index], snapshot.UniqueIds);
            int previousIndex = 0;
            while (previousIndex < index)
            {
                if (string.Equals(
                    uniqueIds[previousIndex].Value,
                    uniqueIds[index].Value,
                    System.StringComparison.Ordinal))
                {
                    throw new System.IO.InvalidDataException(
                        "POP3 UIDL values must be unique within a maildrop.");
                }
                previousIndex++;
            }
            index++;
        }

        ValidateUniqueIdMessages(uniqueIds, snapshot);
    }

    void ValidateStableUniqueId(
        Pop3UniqueId uniqueId,
        Pop3UniqueId[] knownUniqueIds)
    {
        int index = 0;
        while (index < knownUniqueIds.Length)
        {
            Pop3UniqueId known = knownUniqueIds[index];
            if (known.MessageNumber == uniqueId.MessageNumber &&
                !string.Equals(known.Value, uniqueId.Value, System.StringComparison.Ordinal))
            {
                throw new System.IO.InvalidDataException(
                    "A POP3 message changed its unique-id during the transaction.");
            }
            if (known.MessageNumber != uniqueId.MessageNumber &&
                string.Equals(known.Value, uniqueId.Value, System.StringComparison.Ordinal))
            {
                throw new System.IO.InvalidDataException(
                    "Two POP3 messages cannot have the same unique-id.");
            }
            index++;
        }
    }

    void ValidateUniqueIdMessages(
        Pop3UniqueId[] uniqueIds,
        Pop3ClientSnapshot snapshot)
    {
        int visibleMessageCount = 0;
        int messageIndex = 0;
        while (messageIndex < snapshot.Messages.Length)
        {
            Pop3MessageInfo message = snapshot.Messages[messageIndex];
            if (!message.IsDeleted)
            {
                visibleMessageCount++;
                if (!ContainsUniqueIdNumber(uniqueIds, message.MessageNumber))
                {
                    throw new System.IO.InvalidDataException(
                        "The POP3 UIDL data omits a known visible message.");
                }
            }
            messageIndex++;
        }

        if (snapshot.MailboxStatus == null ||
            visibleMessageCount != snapshot.MailboxStatus.MessageCount)
        {
            return;
        }

        int uniqueIdIndex = 0;
        while (uniqueIdIndex < uniqueIds.Length)
        {
            if (!ContainsVisibleMessageNumber(
                snapshot.Messages,
                uniqueIds[uniqueIdIndex].MessageNumber))
            {
                throw new System.IO.InvalidDataException(
                    "The POP3 UIDL data contains an unknown message number.");
            }
            uniqueIdIndex++;
        }
    }

    bool ContainsUniqueIdNumber(Pop3UniqueId[] uniqueIds, int messageNumber)
    {
        int index = 0;
        while (index < uniqueIds.Length)
        {
            if (uniqueIds[index].MessageNumber == messageNumber)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    bool ContainsVisibleMessageNumber(Pop3MessageInfo[] messages, int messageNumber)
    {
        int index = 0;
        while (index < messages.Length)
        {
            if (!messages[index].IsDeleted &&
                messages[index].MessageNumber == messageNumber)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    Pop3UniqueId[] CompleteUniqueIdsRead(
        System.Threading.Tasks.Task<Pop3UniqueId[]> task,
        System.Threading.CancellationToken cancellationToken,
        System.Threading.CancellationToken timeoutToken)
    {
        if (task.IsCompletedSuccessfully)
        {
            return task.Result;
        }

        System.Exception? failure = GetTaskFailure(task);
        Pop3SessionState state = StateMachine.GetCurrentState();
        CloseConnection();
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        if (timeoutToken.IsCancellationRequested)
        {
            throw new Pop3ConnectionException(
                "Reading the POP3 UIDL response timed out.",
                Pop3Commands.Uidl,
                state,
                false,
                true,
                failure);
        }

        System.IO.EndOfStreamException? endOfStream = failure as System.IO.EndOfStreamException;
        if (endOfStream != null)
        {
            throw new Pop3ConnectionException(
                "The POP3 connection ended before the UIDL response was complete.",
                Pop3Commands.Uidl,
                state,
                true,
                false,
                endOfStream);
        }
        System.IO.InvalidDataException? invalidData = failure as System.IO.InvalidDataException;
        if (invalidData != null)
        {
            throw new Pop3ProtocolException(
                "The POP3 server returned malformed or contradictory UIDL data.",
                state,
                "UIDL response",
                invalidData);
        }
        throw new Pop3ConnectionException(
            "Reading the POP3 UIDL response failed.",
            Pop3Commands.Uidl,
            state,
            false,
            false,
            failure);
    }

    void ValidateRetrieveDestination(System.IO.Stream destination)
    {
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
    }

    void ValidateMessageNumberBatch(int[] messageNumbers)
    {
        if (messageNumbers == null)
        {
            throw new System.ArgumentNullException("messageNumbers");
        }
        int index = 0;
        while (index < messageNumbers.Length)
        {
            ValidateMessageNumber(messageNumbers[index]);
            index++;
        }
    }

    void ValidateRetrieveStreamBatch(
        int[] messageNumbers,
        System.IO.Stream[] destinations)
    {
        ValidateMessageNumberBatch(messageNumbers);
        ValidateBatchDestinations(destinations, messageNumbers.Length);
    }

    void ValidateTopBatch(
        int[] messageNumbers,
        int[] bodyLineCounts)
    {
        ValidateMessageNumberBatch(messageNumbers);
        if (bodyLineCounts == null)
        {
            throw new System.ArgumentNullException("bodyLineCounts");
        }
        if (bodyLineCounts.Length != messageNumbers.Length)
        {
            throw new System.ArgumentException(
                "POP3 TOP batch arrays must have the same length.",
                "bodyLineCounts");
        }
        int index = 0;
        while (index < bodyLineCounts.Length)
        {
            ValidateBodyLineCount(bodyLineCounts[index]);
            index++;
        }
    }

    void ValidateTopStreamBatch(
        int[] messageNumbers,
        int[] bodyLineCounts,
        System.IO.Stream[] destinations)
    {
        ValidateTopBatch(messageNumbers, bodyLineCounts);
        ValidateBatchDestinations(destinations, messageNumbers.Length);
    }

    void ValidateBatchDestinations(
        System.IO.Stream[] destinations,
        int expectedLength)
    {
        if (destinations == null)
        {
            throw new System.ArgumentNullException("destinations");
        }
        if (destinations.Length != expectedLength)
        {
            throw new System.ArgumentException(
                "A POP3 destination batch must match the message-number count.",
                "destinations");
        }
        int index = 0;
        while (index < destinations.Length)
        {
            if (destinations[index] == null)
            {
                throw new System.ArgumentException(
                    "A POP3 destination batch cannot contain null streams.",
                    "destinations");
            }
            if (!destinations[index].CanWrite)
            {
                throw new System.ArgumentException(
                    "Every POP3 destination batch stream must be writable.",
                    "destinations");
            }
            index++;
        }
    }

    void ValidateRetrieveOptions(Pop3ClientOptions options, bool requiresMemory)
    {
        if (options.MaximumMessageLineOctets < 3)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }
        if (requiresMemory && options.MessageMemoryCeilingBytes < 0)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }
    }

    Pop3Response BeginRetrieve(
        int messageNumber,
        Pop3WireReader reader,
        Pop3WireWriter writer,
        Pop3ClientOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3Command command = CreateMessageCommand(Pop3Commands.Retr, messageNumber);
        command.ExpectsMultilineResponse = true;
        Pop3Response response = ExchangeSingleLineCommand(
            command,
            reader,
            writer,
            options.ReadTimeoutMilliseconds,
            Pop3Commands.Retr,
            cancellationToken);
        if (response.Kind == Pop3ResponseKind.Positive)
        {
            response.IsMultiline = true;
            response.AdvisoryOctetCount = ParseAdvisoryOctetCount(response);
        }
        StateMachine.TrackResponse(response);
        if (response.Kind == Pop3ResponseKind.Negative)
        {
            throw CreateRepresentationCommandException(
                "The POP3 server rejected the RETR command.",
                command,
                response);
        }
        if (response.Kind != Pop3ResponseKind.Positive)
        {
            Pop3SessionState state = StateMachine.GetCurrentState();
            CloseConnection();
            throw new Pop3ProtocolException(
                "The POP3 server returned an invalid RETR response.",
                state,
                "RETR response",
                null);
        }
        return response;
    }

    long ParseAdvisoryOctetCount(Pop3Response response)
    {
        string text = response.Text;
        int start = 0;
        if (response.ResponseCodeLevels.Length > 0)
        {
            int responseCodeEnd = text.IndexOf(']');
            start = responseCodeEnd + 1;
            if (start >= text.Length || text[start] != ' ')
            {
                return -1;
            }
            start++;
        }

        int end = start;
        while (end < text.Length && text[end] >= '0' && text[end] <= '9')
        {
            end++;
        }
        if (end == start)
        {
            return -1;
        }
        if (end < text.Length && text[end] != ' ')
        {
            return -1;
        }

        byte[] digits = System.Text.Encoding.ASCII.GetBytes(
            text.Substring(start, end - start));
        Pop3NumberParseResult parsed = new Pop3NumberParser().ParseSignedDecimal(
            digits,
            0,
            long.MaxValue);
        if (!parsed.Success)
        {
            return -1;
        }
        return parsed.Value;
    }

    void ValidateBodyLineCount(int bodyLineCount)
    {
        if (bodyLineCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("bodyLineCount");
        }
    }

    void EnsureTopCanBeAttempted()
    {
        if (!CapabilityCache.CanAttemptTop())
        {
            throw new System.InvalidOperationException(
                "The POP3 server does not advertise TOP and probing is not permitted.");
        }
    }

    Pop3Response BeginTop(
        int messageNumber,
        int bodyLineCount,
        Pop3WireReader reader,
        Pop3WireWriter writer,
        Pop3ClientOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3Command command = CreateTopCommand(messageNumber, bodyLineCount);
        Pop3Response response = ExchangeSingleLineCommand(
            command,
            reader,
            writer,
            options.ReadTimeoutMilliseconds,
            Pop3Commands.Top,
            cancellationToken);
        if (response.Kind == Pop3ResponseKind.Positive)
        {
            response.IsMultiline = true;
        }
        StateMachine.TrackResponse(response);
        if (response.Kind == Pop3ResponseKind.Negative)
        {
            throw CreateRepresentationCommandException(
                "The POP3 server rejected the TOP command.",
                command,
                response);
        }
        if (response.Kind != Pop3ResponseKind.Positive)
        {
            Pop3SessionState state = StateMachine.GetCurrentState();
            CloseConnection();
            throw new Pop3ProtocolException(
                "The POP3 server returned an invalid TOP response.",
                state,
                "TOP response",
                null);
        }
        return response;
    }

    Pop3Command CreateTopCommand(int messageNumber, int bodyLineCount)
    {
        Pop3Command command = new Pop3Command();
        command.Keyword = Pop3Commands.Top;
        command.Arguments = new string[]
        {
            messageNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
            bodyLineCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
        command.ExpectsMultilineResponse = true;
        return command;
    }

    long CopyMessageData(
        Pop3WireReader reader,
        System.IO.Stream destination,
        int messageNumber,
        long expectedBytes,
        Pop3TransferKind kind,
        string operation,
        Pop3ClientOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        using (System.Threading.CancellationTokenSource timeoutSource =
            new System.Threading.CancellationTokenSource(options.ReadTimeoutMilliseconds))
        using (System.Threading.CancellationTokenSource linkedSource =
            System.Threading.CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token))
        {
            System.Threading.Tasks.Task<long> copyTask = System.Threading.Tasks.Task.Run(
                () => reader.MultilineReader.CopyMultilineData(
                    destination,
                    options.MaximumMessageLineOctets,
                    options.TransferObserver,
                    kind,
                    messageNumber,
                    expectedBytes,
                    linkedSource.Token));
            System.Threading.Tasks.Task<long> completionTask = copyTask.ContinueWith(
                task => CompleteMessageCopy(
                    task,
                    operation,
                    cancellationToken,
                    timeoutSource.Token),
                System.Threading.CancellationToken.None,
                System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                System.Threading.Tasks.TaskScheduler.Default);
            return completionTask.GetAwaiter().GetResult();
        }
    }

    long CompleteMessageCopy(
        System.Threading.Tasks.Task<long> task,
        string operation,
        System.Threading.CancellationToken cancellationToken,
        System.Threading.CancellationToken timeoutToken)
    {
        if (task.IsCompletedSuccessfully)
        {
            return task.Result;
        }

        System.Exception? failure = GetTaskFailure(task);
        ThrowMessageTransferFailure(
            failure,
            operation,
            cancellationToken,
            timeoutToken);
        return 0;
    }

    byte[] ReadMessageData(
        Pop3WireReader reader,
        int messageNumber,
        long expectedBytes,
        Pop3TransferKind kind,
        string operation,
        Pop3ClientOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        using (System.Threading.CancellationTokenSource timeoutSource =
            new System.Threading.CancellationTokenSource(options.ReadTimeoutMilliseconds))
        using (System.Threading.CancellationTokenSource linkedSource =
            System.Threading.CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token))
        {
            System.Threading.Tasks.Task<byte[]> readTask = System.Threading.Tasks.Task.Run(
                () => reader.MultilineReader.ReadMultilineData(
                    options.MaximumMessageLineOctets,
                    options.MessageMemoryCeilingBytes,
                    options.TransferObserver,
                    kind,
                    messageNumber,
                    expectedBytes,
                    linkedSource.Token));
            System.Threading.Tasks.Task<byte[]> completionTask = readTask.ContinueWith(
                task => CompleteMessageRead(
                    task,
                    operation,
                    cancellationToken,
                    timeoutSource.Token),
                System.Threading.CancellationToken.None,
                System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                System.Threading.Tasks.TaskScheduler.Default);
            return completionTask.GetAwaiter().GetResult();
        }
    }

    byte[] CompleteMessageRead(
        System.Threading.Tasks.Task<byte[]> task,
        string operation,
        System.Threading.CancellationToken cancellationToken,
        System.Threading.CancellationToken timeoutToken)
    {
        if (task.IsCompletedSuccessfully)
        {
            return task.Result;
        }

        System.Exception? failure = GetTaskFailure(task);
        ThrowMessageTransferFailure(
            failure,
            operation,
            cancellationToken,
            timeoutToken);
        return new byte[0];
    }

    void ThrowMessageTransferFailure(
        System.Exception? failure,
        string operation,
        System.Threading.CancellationToken cancellationToken,
        System.Threading.CancellationToken timeoutToken)
    {
        Pop3SessionState state = StateMachine.GetCurrentState();
        CloseConnection();
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        if (timeoutToken.IsCancellationRequested)
        {
            throw new Pop3ConnectionException(
                "Reading the POP3 message timed out.",
                operation,
                state,
                false,
                true,
                failure);
        }

        System.IO.EndOfStreamException? endOfStream = failure as System.IO.EndOfStreamException;
        if (endOfStream != null)
        {
            throw new Pop3ConnectionException(
                "The POP3 connection ended before the message was complete.",
                operation,
                state,
                true,
                false,
                endOfStream);
        }
        System.IO.InvalidDataException? invalidData = failure as System.IO.InvalidDataException;
        if (invalidData != null)
        {
            throw new Pop3ProtocolException(
                "The POP3 server returned invalid message data.",
                state,
                operation + " data",
                invalidData);
        }
        System.IO.IOException? ioFailure = failure as System.IO.IOException;
        if (ioFailure != null)
        {
            throw new Pop3ConnectionException(
                "The POP3 message transfer failed.",
                operation,
                state,
                false,
                false,
                ioFailure);
        }
        if (failure != null)
        {
            throw failure;
        }
        throw new Pop3ConnectionException(
            "The POP3 message transfer failed.",
            operation,
            state,
            false,
            false,
            null);
    }

    void ValidateDeletionBatch(int[] messageNumbers)
    {
        if (messageNumbers == null)
        {
            throw new System.ArgumentNullException("messageNumbers");
        }

        int index = 0;
        while (index < messageNumbers.Length)
        {
            ValidateMessageNumber(messageNumbers[index]);
            int previousIndex = 0;
            while (previousIndex < index)
            {
                if (messageNumbers[previousIndex] == messageNumbers[index])
                {
                    throw new System.ArgumentException(
                        "A POP3 deletion batch cannot contain duplicate message numbers.",
                        "messageNumbers");
                }
                previousIndex++;
            }
            index++;
        }
    }

    void ValidateKnownDeletionTargets(
        int[] messageNumbers,
        Pop3ClientSnapshot snapshot)
    {
        int visibleMessageCount = 0;
        int messageIndex = 0;
        while (messageIndex < snapshot.Messages.Length)
        {
            if (!snapshot.Messages[messageIndex].IsDeleted)
            {
                visibleMessageCount++;
            }
            messageIndex++;
        }
        bool completeSnapshot = snapshot.MailboxStatus != null &&
            visibleMessageCount == snapshot.MailboxStatus.MessageCount;

        int targetIndex = 0;
        while (targetIndex < messageNumbers.Length)
        {
            bool found = false;
            messageIndex = 0;
            while (messageIndex < snapshot.Messages.Length)
            {
                Pop3MessageInfo message = snapshot.Messages[messageIndex];
                if (message.MessageNumber == messageNumbers[targetIndex])
                {
                    found = true;
                    if (message.IsDeleted)
                    {
                        throw new System.InvalidOperationException(
                            "A POP3 message is already marked for deletion.");
                    }
                }
                messageIndex++;
            }
            if (!found && completeSnapshot)
            {
                throw new System.ArgumentException(
                    "A POP3 deletion target is not present in the known maildrop.",
                    "messageNumbers");
            }
            targetIndex++;
        }
    }

    void DeleteMessageCore(
        int messageNumber,
        Pop3WireReader reader,
        Pop3WireWriter writer,
        Pop3ClientOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3Command command = CreateMessageCommand(Pop3Commands.Dele, messageNumber);
        Pop3Response response = ExchangeSingleLineCommand(
            command,
            reader,
            writer,
            options.ReadTimeoutMilliseconds,
            Pop3Commands.Dele,
            cancellationToken);
        StateMachine.TrackResponse(response);
        if (response.Kind == Pop3ResponseKind.Negative)
        {
            throw new Pop3CommandException(
                "The POP3 server rejected the DELE command.",
                command,
                response);
        }
        if (response.Kind != Pop3ResponseKind.Positive)
        {
            Pop3SessionState state = StateMachine.GetCurrentState();
            CloseConnection();
            throw new Pop3ProtocolException(
                "The POP3 server returned an invalid DELE response.",
                state,
                "DELE response",
                null);
        }
        StateMachine.MarkMessageDeleted(messageNumber);
    }

    Pop3CapabilitySet RefreshCapabilitiesBarrier(
        Pop3ClientOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3WireReader reader = GetWireReader();
        Pop3WireWriter writer = GetWireWriter();
        Pop3SessionState state = StateMachine.GetCurrentState();
        using (System.Threading.CancellationTokenSource timeoutSource =
            new System.Threading.CancellationTokenSource(options.ReadTimeoutMilliseconds))
        using (System.Threading.CancellationTokenSource linkedSource =
            System.Threading.CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token))
        {
            System.Threading.Tasks.Task<Pop3CapabilityRefreshResult> refreshTask =
                System.Threading.Tasks.Task.Run(
                    () => PerformCapabilityRefresh(reader, writer, state, linkedSource.Token));
            System.Threading.Tasks.Task<Pop3CapabilitySet> completionTask = refreshTask.ContinueWith(
                task => CompleteCapabilityRefresh(
                    task,
                    options,
                    state,
                    cancellationToken,
                    timeoutSource.Token),
                System.Threading.CancellationToken.None,
                System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                System.Threading.Tasks.TaskScheduler.Default);
            return completionTask.GetAwaiter().GetResult();
        }
    }

    Pop3CapabilityRefreshResult PerformCapabilityRefresh(
        Pop3WireReader reader,
        Pop3WireWriter writer,
        Pop3SessionState state,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3Command command = new Pop3Command();
        command.Keyword = Pop3Commands.Capa;
        writer.WriteCommand(
            command,
            Pop3Defaults.MaximumCommandLineOctets,
            cancellationToken);
        Pop3Response response = reader.ReadResponse(
            Pop3Defaults.MaximumResponseLineOctets,
            cancellationToken);
        StateMachine.TrackResponse(response);
        if (response.Kind == Pop3ResponseKind.Negative)
        {
            Pop3CapabilityRefreshResult unavailable = new Pop3CapabilityRefreshResult();
            unavailable.IsAvailable = false;
            return unavailable;
        }
        if (response.Kind != Pop3ResponseKind.Positive)
        {
            throw new System.IO.InvalidDataException("The POP3 CAPA command returned an invalid response status.");
        }

        System.Collections.Generic.List<byte[]> lines =
            new System.Collections.Generic.List<byte[]>();
        bool complete = false;
        while (!complete)
        {
            byte[]? line = reader.LineReader.ReadCapabilityLine(cancellationToken);
            if (line == null)
            {
                throw new System.IO.EndOfStreamException("The POP3 stream ended before the CAPA terminator.");
            }
            if (line.Length == 1 && line[0] == 46)
            {
                complete = true;
            }
            else
            {
                lines.Add(line);
            }
        }

        Pop3CapabilityParser parser = new Pop3CapabilityParser();
        Pop3CapabilityRefreshResult available = new Pop3CapabilityRefreshResult();
        available.Capabilities = parser.ParseCapabilities(lines.ToArray(), state);
        available.IsAvailable = true;
        return available;
    }

    Pop3CapabilitySet CompleteCapabilityRefresh(
        System.Threading.Tasks.Task<Pop3CapabilityRefreshResult> task,
        Pop3ClientOptions options,
        Pop3SessionState state,
        System.Threading.CancellationToken cancellationToken,
        System.Threading.CancellationToken timeoutToken)
    {
        if (task.IsCompletedSuccessfully)
        {
            Pop3CapabilityRefreshResult result = task.Result;
            CapabilityCache.Record(result.Capabilities, result.IsAvailable, options);
            StateMachine.SetCapabilities(result.Capabilities);
            return result.Capabilities;
        }

        System.Exception? failure = GetTaskFailure(task);
        CloseConnection();
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        if (timeoutToken.IsCancellationRequested)
        {
            throw new Pop3ConnectionException(
                "Refreshing POP3 capabilities timed out.",
                Pop3Commands.Capa,
                state,
                false,
                true,
                failure);
        }
        System.IO.EndOfStreamException? endOfStream = failure as System.IO.EndOfStreamException;
        if (endOfStream != null)
        {
            throw new Pop3ConnectionException(
                "The POP3 connection ended during capability refresh.",
                Pop3Commands.Capa,
                state,
                true,
                false,
                endOfStream);
        }
        throw new Pop3ProtocolException(
            "The POP3 server returned malformed capabilities.",
            state,
            "CAPA",
            failure);
    }

    System.IO.Stream SecureTransport(
        System.IO.Stream stream,
        System.Net.Sockets.TcpClient? candidate,
        bool leaveStreamOpen,
        Pop3ClientOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3TlsClientOptions tlsOptions = CopyTlsOptions(options, leaveStreamOpen);
        IPop3ClientTlsProvider? configuredProvider = options.TlsProvider;
        IPop3ClientTlsProvider provider;
        if (configuredProvider == null)
        {
            provider = new Pop3DefaultClientTlsProvider();
        }
        else
        {
            provider = configuredProvider;
        }

        using (System.Threading.CancellationTokenSource timeoutSource =
            new System.Threading.CancellationTokenSource(options.AuthenticationTimeoutMilliseconds))
        using (System.Threading.CancellationTokenSource linkedSource =
            System.Threading.CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token))
        {
            System.Threading.Tasks.Task<System.IO.Stream> secureTask = System.Threading.Tasks.Task.Run(
                () => provider.SecureClientStream(stream, tlsOptions, linkedSource.Token));
            System.Threading.Tasks.Task<System.IO.Stream> completionTask = secureTask.ContinueWith(
                task => CompleteSecureTransport(
                    task,
                    stream,
                    candidate,
                    leaveStreamOpen,
                    options.SecurityMode,
                    cancellationToken,
                    timeoutSource.Token),
                System.Threading.CancellationToken.None,
                System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                System.Threading.Tasks.TaskScheduler.Default);
            return completionTask.GetAwaiter().GetResult();
        }
    }

    Pop3TlsClientOptions CopyTlsOptions(Pop3ClientOptions options, bool leaveStreamOpen)
    {
        if (options.Tls == null)
        {
            throw new System.ArgumentException("POP3 client TLS options cannot be null.", "options");
        }
        if (options.Tls.ClientCertificates == null)
        {
            throw new System.ArgumentException("POP3 client certificates cannot be null.", "options");
        }

        string? targetHost = options.Tls.TargetHost;
        if (targetHost == null || string.IsNullOrWhiteSpace(targetHost))
        {
            targetHost = options.Host;
        }
        if (targetHost == null || string.IsNullOrWhiteSpace(targetHost))
        {
            throw new System.ArgumentException("A target host is required for POP3 TLS.", "options");
        }

        Pop3TlsClientOptions copy = new Pop3TlsClientOptions();
        copy.TargetHost = targetHost;
        copy.EnabledProtocols = options.Tls.EnabledProtocols;
        copy.CheckCertificateRevocation = options.Tls.CheckCertificateRevocation;
        copy.ClientCertificates.AddRange(options.Tls.ClientCertificates);
        copy.LeaveInnerStreamOpen = leaveStreamOpen;
        return copy;
    }

    System.IO.Stream CompleteSecureTransport(
        System.Threading.Tasks.Task<System.IO.Stream> task,
        System.IO.Stream innerStream,
        System.Net.Sockets.TcpClient? candidate,
        bool leaveInnerStreamOpen,
        Pop3SecurityMode securityMode,
        System.Threading.CancellationToken cancellationToken,
        System.Threading.CancellationToken timeoutToken)
    {
        if (task.IsCompletedSuccessfully)
        {
            System.IO.Stream? securedStream = task.Result;
            if (securedStream != null && securedStream.CanRead && securedStream.CanWrite)
            {
                return securedStream;
            }

            CloseInvalidSecureStream(
                securedStream,
                innerStream,
                candidate,
                leaveInnerStreamOpen);
            ClearTransportAfterFailure();
            StateMachine.InvalidateAfterFailure();
            throw new Pop3TlsException(
                "The POP3 TLS provider returned an invalid secured stream.",
                securityMode,
                "SecureClientStream",
                null);
        }

        System.Exception? failure = GetTaskFailure(task);
        CloseCandidate(innerStream, candidate, !leaveInnerStreamOpen);
        ClearTransportAfterFailure();
        StateMachine.InvalidateAfterFailure();
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        if (timeoutToken.IsCancellationRequested)
        {
            throw new Pop3TlsException(
                "The POP3 TLS handshake timed out.",
                securityMode,
                "SecureClientStream",
                failure);
        }
        throw new Pop3TlsException(
            "The POP3 TLS handshake failed.",
            securityMode,
            "SecureClientStream",
            failure);
    }

    void CloseInvalidSecureStream(
        System.IO.Stream? securedStream,
        System.IO.Stream innerStream,
        System.Net.Sockets.TcpClient? candidate,
        bool leaveInnerStreamOpen)
    {
        if (securedStream != null && !object.ReferenceEquals(securedStream, innerStream))
        {
            securedStream.Dispose();
        }
        CloseCandidate(innerStream, candidate, !leaveInnerStreamOpen);
    }

    void ClearTransportAfterFailure()
    {
        TcpClient = null;
        TransportStream = null;
        WireReader = null;
        WireWriter = null;
        ConnectionOptions = null;
        CapabilityCache.Invalidate();
        DisposeTransportStream = false;
    }

    void InvalidateCapabilityKnowledge()
    {
        StateMachine.ClearCapabilities();
        CapabilityCache.Invalidate();
    }

    void ConnectTcpClient(
        System.Net.Sockets.TcpClient candidate,
        Pop3ClientOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        using (System.Threading.CancellationTokenSource timeoutSource =
            new System.Threading.CancellationTokenSource(options.ConnectTimeoutMilliseconds))
        using (System.Threading.CancellationTokenSource linkedSource =
            System.Threading.CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token))
        {
            System.Threading.Tasks.Task connectTask = candidate.ConnectAsync(
                options.Host,
                options.Port,
                linkedSource.Token).AsTask();
            System.Threading.Tasks.Task completionTask = connectTask.ContinueWith(
                task => CompleteConnect(
                    task,
                    candidate,
                    cancellationToken,
                    timeoutSource.Token),
                System.Threading.CancellationToken.None,
                System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                System.Threading.Tasks.TaskScheduler.Default);
            completionTask.GetAwaiter().GetResult();
        }
    }

    void CompleteConnect(
        System.Threading.Tasks.Task task,
        System.Net.Sockets.TcpClient candidate,
        System.Threading.CancellationToken cancellationToken,
        System.Threading.CancellationToken timeoutToken)
    {
        if (task.IsCompletedSuccessfully)
        {
            return;
        }

        System.Exception? failure = GetTaskFailure(task);
        candidate.Dispose();
        StateMachine.InvalidateAfterFailure();
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        if (timeoutToken.IsCancellationRequested)
        {
            throw new Pop3ConnectionException(
                "The POP3 host connection timed out.",
                "ConnectHost",
                Pop3SessionState.Disconnected,
                false,
                true,
                failure);
        }
        throw new Pop3ConnectionException(
            "The POP3 host connection failed.",
            "ConnectHost",
            Pop3SessionState.Disconnected,
            false,
            false,
            failure);
    }

    Pop3Response ReadGreeting(
        System.IO.Stream stream,
        System.Net.Sockets.TcpClient? candidate,
        bool disposeStream,
        Pop3WireReader reader,
        int readTimeoutMilliseconds,
        System.Threading.CancellationToken cancellationToken)
    {
        using (System.Threading.CancellationTokenSource timeoutSource =
            new System.Threading.CancellationTokenSource(readTimeoutMilliseconds))
        using (System.Threading.CancellationTokenSource linkedSource =
            System.Threading.CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token))
        {
            System.Threading.Tasks.Task<Pop3Response> readTask = System.Threading.Tasks.Task.Run(
                () => reader.ReadResponse(
                    Pop3Defaults.MaximumResponseLineOctets,
                    linkedSource.Token));
            System.Threading.Tasks.Task<Pop3Response> completionTask = readTask.ContinueWith(
                task => CompleteGreeting(
                    task,
                    stream,
                    candidate,
                    disposeStream,
                    cancellationToken,
                    timeoutSource.Token),
                System.Threading.CancellationToken.None,
                System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                System.Threading.Tasks.TaskScheduler.Default);
            return completionTask.GetAwaiter().GetResult();
        }
    }

    Pop3Response CompleteGreeting(
        System.Threading.Tasks.Task<Pop3Response> task,
        System.IO.Stream stream,
        System.Net.Sockets.TcpClient? candidate,
        bool disposeStream,
        System.Threading.CancellationToken cancellationToken,
        System.Threading.CancellationToken timeoutToken)
    {
        if (task.IsCompletedSuccessfully)
        {
            return task.Result;
        }

        System.Exception? failure = GetTaskFailure(task);
        CloseCandidate(stream, candidate, disposeStream);
        StateMachine.InvalidateAfterFailure();
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        if (timeoutToken.IsCancellationRequested)
        {
            throw new Pop3ConnectionException(
                "Reading the POP3 greeting timed out.",
                "ReadGreeting",
                Pop3SessionState.Disconnected,
                false,
                true,
                failure);
        }

        System.IO.EndOfStreamException? endOfStream = failure as System.IO.EndOfStreamException;
        if (endOfStream != null)
        {
            throw new Pop3ConnectionException(
                "The POP3 connection ended before a complete greeting was received.",
                "ReadGreeting",
                Pop3SessionState.Disconnected,
                true,
                false,
                endOfStream);
        }

        System.IO.InvalidDataException? invalidData = failure as System.IO.InvalidDataException;
        if (invalidData != null)
        {
            throw new Pop3ProtocolException(
                "The POP3 server sent a malformed greeting.",
                Pop3SessionState.Disconnected,
                "greeting",
                invalidData);
        }

        throw new Pop3ConnectionException(
            "Reading the POP3 greeting failed.",
            "ReadGreeting",
            Pop3SessionState.Disconnected,
            false,
            false,
            failure);
    }

    void ValidateGreeting(
        Pop3Response greeting,
        System.IO.Stream stream,
        System.Net.Sockets.TcpClient? candidate,
        bool disposeStream,
        string operation)
    {
        if (greeting.Kind == Pop3ResponseKind.Positive)
        {
            return;
        }

        StateMachine.TrackResponse(greeting);
        CloseCandidate(stream, candidate, disposeStream);
        StateMachine.InvalidateAfterFailure();
        if (greeting.Kind == Pop3ResponseKind.Negative)
        {
            throw new Pop3ConnectionException(
                "The POP3 server rejected the connection greeting.",
                operation,
                Pop3SessionState.Disconnected,
                false,
                false,
                null);
        }
        throw new Pop3ProtocolException(
            "The POP3 server sent an invalid connection greeting.",
            Pop3SessionState.Disconnected,
            "greeting status",
            null);
    }

    void CloseCandidate(
        System.IO.Stream stream,
        System.Net.Sockets.TcpClient? candidate,
        bool disposeStream)
    {
        if (disposeStream)
        {
            stream.Dispose();
        }
        if (candidate != null)
        {
            candidate.Dispose();
        }
    }

    System.Exception? GetTaskFailure(System.Threading.Tasks.Task task)
    {
        if (task.Exception == null)
        {
            return null;
        }
        return task.Exception.GetBaseException();
    }
}
