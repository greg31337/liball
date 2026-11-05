namespace pop3;

internal class Pop3ServerEngine
{
    Pop3ServerStateMachine StateMachine = new Pop3ServerStateMachine();
    Pop3ServerOptions? Options;
    Pop3WireReader? WireReader;
    Pop3WireWriter? WireWriter;
    Pop3CommandDispatcher? CommandDispatcher;
    Pop3CapabilityBuilder CapabilityBuilder = new Pop3CapabilityBuilder();
    Pop3ExtensionDispatcher ExtensionDispatcher =
        new Pop3ExtensionDispatcher();
    Pop3LanguageSession LanguageSession = new Pop3LanguageSession();
    Pop3AuthorizationExchange AuthorizationExchange =
        new Pop3AuthorizationExchange();
    Pop3MailboxTransaction MailboxTransaction =
        new Pop3MailboxTransaction();
    Pop3ServerMessageTransfer MessageTransfer =
        new Pop3ServerMessageTransfer();

    internal Pop3Response StartSession(
        Pop3ServerConnection connection,
        Pop3ServerOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        ValidateStartupInputs(connection, options);
        ExtensionDispatcher.ConfigureExtensions(options.Extensions);
        cancellationToken.ThrowIfCancellationRequested();
        AuthorizationExchange.ResetExchange();
        long startedAt = System.Environment.TickCount64;
        Pop3Response greeting = StateMachine.OpenConnection(
            connection,
            options,
            startedAt);
        System.IO.Stream stream = StateMachine.GetTransportStream();
        if (options.SecurityMode == Pop3SecurityMode.ImplicitTls &&
            !StateMachine.GetIsSecure())
        {
            stream = SecureTransport(
                stream,
                options,
                cancellationToken);
            StateMachine.ReplaceTransportStream(stream, true);
        }

        Pop3WireReader reader = new Pop3WireReader(stream, options.BufferSize);
        Pop3WireWriter writer = new Pop3WireWriter(stream, options.BufferSize);
        Options = options;
        WireReader = reader;
        WireWriter = writer;
        CommandDispatcher = new Pop3CommandDispatcher(
            reader,
            writer,
            StateMachine,
            options.EnablePipelining,
            options.EnableUtf8 && options.EnableUtf8User);
        CommandDispatcher.SetExtensionDispatcher(ExtensionDispatcher);
        CommandDispatcher.SetProtocolLogger(options.Logger);
        WriteGreeting(writer, greeting, options, cancellationToken);
        StateMachine.RecordActivity(System.Environment.TickCount64);
        return greeting;
    }

    internal Pop3ServerStateMachine GetStateMachine()
    {
        return StateMachine;
    }

    internal Pop3WireReader GetWireReader()
    {
        if (WireReader == null)
        {
            throw new Pop3StateException(
                "A POP3 server wire reader is not available.",
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
                "A POP3 server wire writer is not available.",
                "GetWireWriter",
                StateMachine.GetCurrentState());
        }
        return WireWriter;
    }

    internal Pop3ServerOptions GetOptions()
    {
        if (Options == null)
        {
            throw new Pop3StateException(
                "POP3 server session options are not available.",
                "GetOptions",
                StateMachine.GetCurrentState());
        }
        return Options;
    }

    internal Pop3Command ReadNextCommand(
        System.Threading.CancellationToken cancellationToken)
    {
        if (CommandDispatcher == null)
        {
            throw new Pop3StateException(
                "A POP3 server command dispatcher is not available.",
                "ReadNextCommand",
                StateMachine.GetCurrentState());
        }
        Pop3CommandDispatcher dispatcher = CommandDispatcher;
        Pop3ServerOptions options = GetOptions();
        System.Threading.CancellationToken inactivityToken =
            StateMachine.GetInactivityCancellationToken();
        using (System.Threading.CancellationTokenSource timeoutSource =
            new System.Threading.CancellationTokenSource(
                options.ReadTimeoutMilliseconds))
        using (System.Threading.CancellationTokenSource linkedSource =
            System.Threading.CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token,
                inactivityToken))
        {
            System.Threading.Tasks.Task<Pop3Command> readTask =
                System.Threading.Tasks.Task.Run(() =>
                    dispatcher.ReadDispatchableCommand(
                        linkedSource.Token));
            System.Threading.Tasks.Task<Pop3Command> completionTask =
                readTask.ContinueWith(
                    task => CompleteCommandRead(
                        task,
                        cancellationToken,
                        timeoutSource.Token,
                        inactivityToken),
                    System.Threading.CancellationToken.None,
                    System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                    System.Threading.Tasks.TaskScheduler.Default);
            return completionTask.GetAwaiter().GetResult();
        }
    }

    internal void WriteNextResponse(
        Pop3Response response,
        System.Threading.CancellationToken cancellationToken)
    {
        if (CommandDispatcher == null)
        {
            throw new Pop3StateException(
                "A POP3 server command dispatcher is not available.",
                "WriteNextResponse",
                StateMachine.GetCurrentState());
        }
        CommandDispatcher.WriteDispatchedResponse(response, cancellationToken);
    }

    internal Pop3CapabilitySet WriteCapabilitiesResponse(
        System.Threading.CancellationToken cancellationToken)
    {
        using (Pop3ServerFailureGuard guard =
            new Pop3ServerFailureGuard(this))
        {
            GetCommandDispatcher().RequirePendingCommand(Pop3Commands.Capa);
            Pop3CapabilitySet capabilities =
                CapabilityBuilder.BuildCapabilities(
                    GetOptions(),
                    StateMachine);
            Pop3Response response = new Pop3Response();
            response.Kind = Pop3ResponseKind.Positive;
            response.Text = "Capability list follows";
            response.IsMultiline = true;
            WriteNextResponse(response, cancellationToken);
            GetWireWriter().WriteServerCapabilities(
                capabilities.Entries,
                StateMachine.GetCurrentState(),
                Pop3Defaults.MaximumResponseLineOctets,
                cancellationToken);
            GetCommandDispatcher().CompleteMultilineResponse(
                cancellationToken);
            guard.CompleteOperation();
            return capabilities;
        }
    }

    internal Pop3CapabilitySet? UpgradeTransportToTls(
        Pop3Command command,
        System.Threading.CancellationToken cancellationToken)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }
        if (command.Keyword == null || command.Arguments == null ||
            !string.Equals(
                command.Keyword,
                Pop3Commands.Stls,
                System.StringComparison.OrdinalIgnoreCase) ||
            command.Arguments.Length != 0)
        {
            throw new System.ArgumentException(
                "A TLS upgrade requires a parsed STLS command.",
                "command");
        }
        Pop3CommandDispatcher dispatcher = GetCommandDispatcher();
        dispatcher.RequirePendingCommand(Pop3Commands.Stls);
        cancellationToken.ThrowIfCancellationRequested();
        Pop3ServerOptions options = GetOptions();
        if (!CanUpgradeTransportToTls(options))
        {
            Pop3Response rejection = new Pop3Response();
            rejection.Kind = Pop3ResponseKind.Negative;
            rejection.Text = "TLS upgrade is not available.";
            dispatcher.WriteDispatchedResponse(rejection, cancellationToken);
            return null;
        }

        ValidateTlsConfiguration(options);
        Pop3Response acceptance = new Pop3Response();
        acceptance.Kind = Pop3ResponseKind.Positive;
        acceptance.Text = "Begin TLS negotiation";
        dispatcher.WriteDispatchedResponse(acceptance, cancellationToken);
        AuthorizationExchange.ResetExchange();
        GetWireReader().DiscardBufferedInput();

        System.IO.Stream secureStream = SecureTransport(
            StateMachine.GetTransportStream(),
            options,
            cancellationToken);
        StateMachine.ReplaceTransportStream(secureStream, true);
        InitializeWireComponents(secureStream, options.BufferSize);
        StateMachine.RecordActivity(System.Environment.TickCount64);
        return CapabilityBuilder.BuildCapabilities(options, StateMachine);
    }

    internal Pop3Response HandleUserCommand(
        Pop3Command command,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3CommandDispatcher dispatcher = GetCommandDispatcher();
        dispatcher.RequirePendingCommand(Pop3Commands.User);
        Pop3Response response = AuthorizationExchange.AcceptUser(
            command,
            GetOptions(),
            StateMachine,
            cancellationToken);
        dispatcher.WriteDispatchedResponse(response, cancellationToken);
        return response;
    }

    internal Pop3AuthenticationResult HandlePasswordCommand(
        Pop3Command command,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3CommandDispatcher dispatcher = GetCommandDispatcher();
        dispatcher.RequirePendingCommand(Pop3Commands.Pass);
        bool classifyCredentials = AuthorizationExchange.HasPendingUser();
        Pop3ServerOptions options = GetOptions();
        Pop3AuthenticationResult result =
            AuthorizationExchange.AuthenticatePassword(
                command,
                options,
                StateMachine,
                cancellationToken);
        if (result.Status != Pop3AuthenticationStatus.Success)
        {
            Pop3Response response =
                AuthorizationExchange.CreateAuthenticationFailureResponse(
                    result,
                    classifyCredentials,
                    options.EnableResponseCodes);
            dispatcher.WriteDispatchedResponse(response, cancellationToken);
        }
        return result;
    }

    internal Pop3AuthenticationResult HandleApopCommand(
        Pop3Command command,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3CommandDispatcher dispatcher = GetCommandDispatcher();
        dispatcher.RequirePendingCommand(Pop3Commands.Apop);
        Pop3ServerOptions options = GetOptions();
        Pop3AuthenticationResult result = AuthorizationExchange.AuthenticateApop(
            command,
            options,
            StateMachine,
            cancellationToken);
        if (result.Status != Pop3AuthenticationStatus.Success)
        {
            Pop3Response response =
                AuthorizationExchange.CreateAuthenticationFailureResponse(
                    result,
                    true,
                    options.EnableResponseCodes);
            dispatcher.WriteDispatchedResponse(response, cancellationToken);
        }
        return result;
    }

    internal Pop3AuthenticationResult HandleAuthCommand(
        Pop3Command command,
        System.Threading.CancellationToken cancellationToken)
    {
        using (Pop3ServerFailureGuard guard =
            new Pop3ServerFailureGuard(this))
        {
            Pop3CommandDispatcher dispatcher = GetCommandDispatcher();
            dispatcher.RequirePendingCommand(Pop3Commands.Auth);
            Pop3ServerOptions options = GetOptions();
            Pop3AuthenticationResult result =
                AuthorizationExchange.AuthenticateSasl(
                    command,
                    options,
                    StateMachine,
                    GetWireReader(),
                    GetWireWriter(),
                    cancellationToken);
            if (result.Status != Pop3AuthenticationStatus.Success)
            {
                Pop3Response response =
                    AuthorizationExchange.CreateAuthenticationFailureResponse(
                        result,
                        true,
                        options.EnableResponseCodes);
                dispatcher.WriteDispatchedResponse(response, cancellationToken);
            }
            guard.CompleteOperation();
            return result;
        }
    }

    internal Pop3Response HandleUtf8Command(
        Pop3Command command,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3CommandDispatcher dispatcher = GetCommandDispatcher();
        dispatcher.RequirePendingCommand(Pop3Commands.Utf8);
        ValidateUtf8Command(command);
        cancellationToken.ThrowIfCancellationRequested();
        Pop3Response response = new Pop3Response();
        if (!GetOptions().EnableUtf8)
        {
            response.Kind = Pop3ResponseKind.Negative;
            response.Text = "UTF8 mode is not available.";
        }
        else
        {
            StateMachine.EnableUtf8Mode();
            response.Kind = Pop3ResponseKind.Positive;
            response.Text = "UTF8 mode enabled.";
        }
        dispatcher.WriteDispatchedResponse(response, cancellationToken);
        return response;
    }

    internal Pop3Language[] HandleLangCommand(
        Pop3Command command,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3CommandDispatcher dispatcher = GetCommandDispatcher();
        dispatcher.RequirePendingCommand(Pop3Commands.Lang);
        ValidateLangCommand(command);
        cancellationToken.ThrowIfCancellationRequested();
        Pop3ServerOptions options = GetOptions();
        IPop3LanguageProvider? provider = options.LanguageProvider;
        if (provider == null)
        {
            WriteLangResponse(
                Pop3ResponseKind.Negative,
                "lang-unavailable",
                "Language support is not available.",
                null,
                cancellationToken);
            return new Pop3Language[0];
        }

        if (command.Arguments.Length == 0)
        {
            Pop3Language[] languages =
                LanguageSession.GetValidatedLanguages(options);
            Pop3Response response = new Pop3Response();
            response.Kind = Pop3ResponseKind.Positive;
            response.Text = LanguageSession.ResolveResponseText(
                options,
                StateMachine,
                "lang-list-success",
                "Language listing follows.");
            response.IsMultiline = true;
            dispatcher.WriteDispatchedResponse(response, cancellationToken);
            GetWireWriter().WriteServerLanguageEntries(
                languages,
                Pop3Defaults.MaximumResponseLineOctets,
                cancellationToken);
            dispatcher.CompleteMultilineResponse(cancellationToken);
            return languages;
        }

        string language = command.Arguments[0];
        if (language == "*")
        {
            string? defaultLanguage = options.DefaultLanguage;
            if (defaultLanguage != null &&
                (!new Pop3LanguageParser().IsValidLanguageTag(defaultLanguage) ||
                    !provider.SupportsLanguage(defaultLanguage)))
            {
                WriteLangResponse(
                    Pop3ResponseKind.Negative,
                    "lang-reset-unavailable",
                    "The server default language is not available.",
                    null,
                    cancellationToken);
                return new Pop3Language[0];
            }
            StateMachine.SelectLanguage(null);
            WriteLangResponse(
                Pop3ResponseKind.Positive,
                "lang-reset-success",
                "Server default language selected.",
                defaultLanguage,
                cancellationToken);
            return new Pop3Language[0];
        }

        if (!new Pop3LanguageParser().IsValidLanguageTag(language) ||
            !provider.SupportsLanguage(language))
        {
            WriteLangResponse(
                Pop3ResponseKind.Negative,
                "lang-language-unavailable",
                "Requested language is not available.",
                null,
                cancellationToken);
            return new Pop3Language[0];
        }

        StateMachine.SelectLanguage(language);
        WriteLangResponse(
            Pop3ResponseKind.Positive,
            "lang-select-success",
            "Language selected.",
            language,
            cancellationToken);
        return new Pop3Language[0];
    }

    internal Pop3ExtensionResult HandleExtensionCommand(
        Pop3Command command,
        System.Threading.CancellationToken cancellationToken)
    {
        using (Pop3ServerFailureGuard guard =
            new Pop3ServerFailureGuard(this))
        {
            if (command == null)
            {
                throw new System.ArgumentNullException("command");
            }
            Pop3CommandDispatcher dispatcher = GetCommandDispatcher();
            dispatcher.RequirePendingCommand(command.Keyword);
            cancellationToken.ThrowIfCancellationRequested();
            Pop3ServerOptions options = GetOptions();
            Pop3Command extensionCommand = CopyExtensionCommand(command);
            int commandOctets = new Pop3CommandParser().FormatCommand(
                extensionCommand).Length + 2;
            Pop3CapabilitySet capabilities =
                CapabilityBuilder.BuildCapabilities(options, StateMachine);
            Pop3ExtensionContext context =
                StateMachine.CreateExtensionContext(capabilities);
            Pop3ExtensionResult result =
                ExtensionDispatcher.ExecuteRegisteredCommand(
                    context,
                    extensionCommand,
                    cancellationToken);
            if (!options.EnableResponseCodes)
            {
                result.Response.ResponseCodeLevels = new string[0];
            }
            Pop3Command loggedCommand = CopyExtensionCommand(command);
            loggedCommand.ContainsSensitiveData =
                extensionCommand.ContainsSensitiveData;
            Pop3SecretRedactor redactor =
                new Pop3SecretRedactor(options.Logger);
            redactor.LogCommand(
                Pop3LogDirection.ClientToServer,
                loggedCommand,
                commandOctets,
                StateMachine.GetCurrentState());
            dispatcher.WriteDispatchedResponse(
                result.Response,
                cancellationToken);
            if (result.MultilineData != null)
            {
                long dataOctets = GetWireWriter().WriteServerExtensionData(
                    result.MultilineData,
                    options.MaximumMessageLineOctets,
                    options.TransferObserver,
                    cancellationToken);
                redactor.LogMultilineData(
                    Pop3LogDirection.ServerToClient,
                    Pop3TransferKind.ExtensionData,
                    0,
                    dataOctets,
                    StateMachine.GetCurrentState());
                dispatcher.CompleteMultilineResponse(cancellationToken);
            }
            if (result.CloseSession)
            {
                CloseSession();
            }
            guard.CompleteOperation();
            return result;
        }
    }

    internal void WriteAuthenticationSuccessResponse(
        Pop3Response response,
        System.Threading.CancellationToken cancellationToken)
    {
        using (Pop3ServerFailureGuard guard =
            new Pop3ServerFailureGuard(this))
        {
            if (response == null)
            {
                throw new System.ArgumentNullException("response");
            }
            if (response.Kind != Pop3ResponseKind.Positive ||
                response.IsMultiline)
            {
                throw new System.ArgumentException(
                    "Authentication completion requires a single-line positive response.",
                    "response");
            }
            cancellationToken.ThrowIfCancellationRequested();
            bool activateSaslLayer =
                AuthorizationExchange.HasSuccessfulSaslAuthentication();
            System.IO.Stream innerStream = StateMachine.GetTransportStream();
            Pop3WireReader innerReader = GetWireReader();
            Pop3WireWriter innerWriter = GetWireWriter();
            Pop3CommandDispatcher innerDispatcher = GetCommandDispatcher();
            WriteNextResponse(response, cancellationToken);
            if (!activateSaslLayer)
            {
                guard.CompleteOperation();
                return;
            }

            WireReader = null;
            WireWriter = null;
            CommandDispatcher = null;
            System.IO.Stream securityStream =
                AuthorizationExchange.ApplySuccessfulSaslSecurityLayer(
                    innerStream,
                    cancellationToken);
            if (object.ReferenceEquals(securityStream, innerStream))
            {
                WireReader = innerReader;
                WireWriter = innerWriter;
                CommandDispatcher = innerDispatcher;
                guard.CompleteOperation();
                return;
            }

            innerReader.DiscardBufferedInput();
            StateMachine.ReplaceTransportStream(securityStream, true);
            InitializeWireComponents(securityStream, GetOptions().BufferSize);
            StateMachine.RecordActivity(System.Environment.TickCount64);
            guard.CompleteOperation();
        }
    }

    internal Pop3MailboxOpenResult CompleteAuthentication(
        Pop3AuthenticationResult authentication,
        System.Threading.CancellationToken cancellationToken)
    {
        if (authentication == null)
        {
            throw new System.ArgumentNullException("authentication");
        }
        if (authentication.Status != Pop3AuthenticationStatus.Success)
        {
            throw new System.ArgumentException(
                "Only successful POP3 authentication can acquire a mailbox.",
                "authentication");
        }
        cancellationToken.ThrowIfCancellationRequested();
        Pop3ServerOptions options = GetOptions();
        Pop3MailboxOpenResult result = MailboxTransaction.OpenMailbox(
            authentication,
            options.MailboxProvider,
            StateMachine.GetConnectionContext(),
            cancellationToken);
        if (result.Status != Pop3AuthenticationStatus.Success)
        {
            AuthorizationExchange.RejectSuccessfulAuthentication();
            Pop3AuthenticationResult failure =
                CreateMailboxAuthenticationFailure(result);
            Pop3Response response =
                AuthorizationExchange.CreateAuthenticationFailureResponse(
                    failure,
                    true,
                    options.EnableResponseCodes);
            WriteNextResponse(response, cancellationToken);
            return result;
        }

        StateMachine.EnterTransaction();
        Pop3Response success = new Pop3Response();
        success.Kind = Pop3ResponseKind.Positive;
        success.Text = "Authentication successful.";
        WriteAuthenticationSuccessResponse(success, cancellationToken);
        AuthorizationExchange.ResetExchange();
        return result;
    }

    internal bool HasOpenMailbox()
    {
        return MailboxTransaction.HasSession();
    }

    internal void MarkMailboxMessageDeleted(int messageNumber)
    {
        MailboxTransaction.MarkDeleted(messageNumber);
    }

    internal Pop3MailboxStatus HandleStatCommand(
        Pop3Command command,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3CommandDispatcher dispatcher = GetCommandDispatcher();
        dispatcher.RequirePendingCommand(Pop3Commands.Stat);
        ValidateStatCommand(command);
        ThrowIfCancellationRequestedAndAbort(cancellationToken);
        Pop3MailboxStatus status = MailboxTransaction.GetStatus();
        ValidateStatStatus(status);
        string responseText = status.MessageCount.ToString(
            System.Globalization.CultureInfo.InvariantCulture) +
            " " +
            status.TotalOctets.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        string localizedText = LanguageSession.ResolveResponseText(
            GetOptions(),
            StateMachine,
            "stat-success",
            "Maildrop status.");
        if (localizedText.Length > 0)
        {
            responseText += " " + localizedText;
        }

        Pop3Response response = new Pop3Response();
        response.Kind = Pop3ResponseKind.Positive;
        response.Text = responseText;
        dispatcher.WriteDispatchedResponse(response, cancellationToken);
        return status;
    }

    internal Pop3MessageInfo[] HandleListCommand(
        Pop3Command command,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3CommandDispatcher dispatcher = GetCommandDispatcher();
        dispatcher.RequirePendingCommand(Pop3Commands.List);
        ValidateListCommand(command);
        ThrowIfCancellationRequestedAndAbort(cancellationToken);
        Pop3MessageInfo[] messages = MailboxTransaction.GetMessages();
        if (command.Arguments.Length == 1)
        {
            int messageNumber = ParseListMessageNumber(command.Arguments[0]);
            if (messageNumber > messages.Length ||
                messages[messageNumber - 1].IsDeleted)
            {
                WriteListFailure(
                    "list-message-unavailable",
                    "No such message.",
                    false,
                    cancellationToken);
                return new Pop3MessageInfo[0];
            }
            Pop3MessageInfo message = messages[messageNumber - 1];
            if (RequiresUtf8ForList(message))
            {
                WriteListFailure(
                    "list-utf8-required",
                    "Enable UTF8 to access this message.",
                    true,
                    cancellationToken);
                return new Pop3MessageInfo[0];
            }

            string localizedText = LanguageSession.ResolveResponseText(
                GetOptions(),
                StateMachine,
                "list-one-success",
                "Message scan listing.");
            string responseText = FormatListEntry(message);
            if (localizedText.Length > 0)
            {
                responseText += " " + localizedText;
            }
            Pop3Response response = new Pop3Response();
            response.Kind = Pop3ResponseKind.Positive;
            response.Text = responseText;
            dispatcher.WriteDispatchedResponse(response, cancellationToken);
            return new Pop3MessageInfo[] { message };
        }

        Pop3MessageInfo[] visibleMessages = CreateVisibleList(messages);
        int index = 0;
        while (index < visibleMessages.Length)
        {
            if (RequiresUtf8ForList(visibleMessages[index]))
            {
                WriteListFailure(
                    "list-utf8-required",
                    "Enable UTF8 to access this maildrop.",
                    true,
                    cancellationToken);
                return new Pop3MessageInfo[0];
            }
            index++;
        }

        string listingText = LanguageSession.ResolveResponseText(
            GetOptions(),
            StateMachine,
            "list-all-success",
            "Scan listing follows.");
        Pop3Response listingResponse = new Pop3Response();
        listingResponse.Kind = Pop3ResponseKind.Positive;
        listingResponse.Text = listingText;
        listingResponse.IsMultiline = true;
        dispatcher.WriteDispatchedResponse(
            listingResponse,
            cancellationToken);
        GetWireWriter().WriteServerListEntries(
            visibleMessages,
            Pop3Defaults.MaximumResponseLineOctets,
            cancellationToken);
        dispatcher.CompleteMultilineResponse(cancellationToken);
        return visibleMessages;
    }

    internal Pop3UniqueId[] HandleUidlCommand(
        Pop3Command command,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3CommandDispatcher dispatcher = GetCommandDispatcher();
        dispatcher.RequirePendingCommand(Pop3Commands.Uidl);
        ValidateUidlCommand(command);
        ThrowIfCancellationRequestedAndAbort(cancellationToken);
        Pop3ServerOptions options = GetOptions();
        if (!options.EnableUidl)
        {
            WriteUidlFailure(
                "uidl-unavailable",
                "UIDL is not available.",
                cancellationToken);
            return new Pop3UniqueId[0];
        }

        Pop3MessageInfo[] messages = MailboxTransaction.GetMessages();
        Pop3UniqueId[] uniqueIds = MailboxTransaction.GetUniqueIds();
        if (command.Arguments.Length == 1)
        {
            int messageNumber = ParseUidlMessageNumber(command.Arguments[0]);
            if (messageNumber > messages.Length ||
                messages[messageNumber - 1].IsDeleted)
            {
                WriteUidlFailure(
                    "uidl-message-unavailable",
                    "No such message.",
                    cancellationToken);
                return new Pop3UniqueId[0];
            }

            Pop3UniqueId uniqueId = uniqueIds[messageNumber - 1];
            string localizedText = LanguageSession.ResolveResponseText(
                options,
                StateMachine,
                "uidl-one-success",
                "Unique-id listing.");
            string responseText = FormatUniqueIdEntry(uniqueId);
            if (localizedText.Length > 0)
            {
                responseText += " " + localizedText;
            }
            Pop3Response response = new Pop3Response();
            response.Kind = Pop3ResponseKind.Positive;
            response.Text = responseText;
            dispatcher.WriteDispatchedResponse(response, cancellationToken);
            return new Pop3UniqueId[] { uniqueId };
        }

        Pop3UniqueId[] visibleUniqueIds = CreateVisibleUniqueIds(
            messages,
            uniqueIds);
        string listingText = LanguageSession.ResolveResponseText(
            options,
            StateMachine,
            "uidl-all-success",
            "Unique-id listing follows.");
        Pop3Response listingResponse = new Pop3Response();
        listingResponse.Kind = Pop3ResponseKind.Positive;
        listingResponse.Text = listingText;
        listingResponse.IsMultiline = true;
        dispatcher.WriteDispatchedResponse(
            listingResponse,
            cancellationToken);
        GetWireWriter().WriteServerUniqueIdEntries(
            visibleUniqueIds,
            Pop3Defaults.MaximumResponseLineOctets,
            cancellationToken);
        dispatcher.CompleteMultilineResponse(cancellationToken);
        return visibleUniqueIds;
    }

    internal long HandleRetrCommand(
        Pop3Command command,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3CommandDispatcher dispatcher = GetCommandDispatcher();
        dispatcher.RequirePendingCommand(Pop3Commands.Retr);
        ValidateRetrCommand(command);
        ThrowIfCancellationRequestedAndAbort(cancellationToken);
        int messageNumber = ParseRetrMessageNumber(command.Arguments[0]);
        Pop3MessageInfo[] messages = MailboxTransaction.GetMessages();
        if (messageNumber > messages.Length ||
            messages[messageNumber - 1].IsDeleted)
        {
            WriteRetrFailure(
                "retr-message-unavailable",
                "No such message.",
                false,
                cancellationToken);
            return -1;
        }

        Pop3MessageInfo message = messages[messageNumber - 1];
        if (message.IsUtf8Representation &&
            !StateMachine.GetIsUtf8Enabled())
        {
            WriteRetrFailure(
                "retr-utf8-required",
                "Enable UTF8 to retrieve this message.",
                true,
                cancellationToken);
            return -1;
        }

        Pop3ServerOptions options = GetOptions();
        string localizedText = LanguageSession.ResolveResponseText(
            options,
            StateMachine,
            "retr-success",
            "Message follows.");
        string responseText = message.OctetCount.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        if (localizedText.Length > 0)
        {
            responseText += " " + localizedText;
        }
        Pop3Response response = new Pop3Response();
        response.Kind = Pop3ResponseKind.Positive;
        response.Text = responseText;
        response.IsMultiline = true;
        response.AdvisoryOctetCount = message.OctetCount;
        dispatcher.WriteDispatchedResponse(response, cancellationToken);
        using (Pop3ServerFailureGuard guard =
            new Pop3ServerFailureGuard(this))
        {
            long transmittedOctets = MessageTransfer.WriteMessage(
                MailboxTransaction,
                GetWireWriter(),
                options,
                messageNumber,
                message.IsUtf8Representation,
                message.OctetCount,
                cancellationToken);
            dispatcher.CompleteMultilineResponse(cancellationToken);
            guard.CompleteOperation();
            return transmittedOctets;
        }
    }

    internal long HandleTopCommand(
        Pop3Command command,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3CommandDispatcher dispatcher = GetCommandDispatcher();
        dispatcher.RequirePendingCommand(Pop3Commands.Top);
        ValidateTopCommand(command);
        ThrowIfCancellationRequestedAndAbort(cancellationToken);
        Pop3ServerOptions options = GetOptions();
        if (!options.EnableTop)
        {
            WriteTopFailure(
                "top-unavailable",
                "TOP is not available.",
                false,
                cancellationToken);
            return -1;
        }

        int messageNumber = ParseTopMessageNumber(command.Arguments[0]);
        int bodyLineCount = ParseTopBodyLineCount(command.Arguments[1]);
        Pop3MessageInfo[] messages = MailboxTransaction.GetMessages();
        if (messageNumber > messages.Length ||
            messages[messageNumber - 1].IsDeleted)
        {
            WriteTopFailure(
                "top-message-unavailable",
                "No such message.",
                false,
                cancellationToken);
            return -1;
        }

        Pop3MessageInfo message = messages[messageNumber - 1];
        if (message.IsUtf8Representation &&
            !StateMachine.GetIsUtf8Enabled())
        {
            WriteTopFailure(
                "top-utf8-required",
                "Enable UTF8 to access this message.",
                true,
                cancellationToken);
            return -1;
        }

        string responseText = LanguageSession.ResolveResponseText(
            options,
            StateMachine,
            "top-success",
            "Top of message follows.");
        Pop3Response response = new Pop3Response();
        response.Kind = Pop3ResponseKind.Positive;
        response.Text = responseText;
        response.IsMultiline = true;
        dispatcher.WriteDispatchedResponse(response, cancellationToken);
        using (Pop3ServerFailureGuard guard =
            new Pop3ServerFailureGuard(this))
        {
            long transmittedOctets = MessageTransfer.WriteTop(
                MailboxTransaction,
                GetWireWriter(),
                options,
                messageNumber,
                bodyLineCount,
                message.IsUtf8Representation,
                cancellationToken);
            dispatcher.CompleteMultilineResponse(cancellationToken);
            guard.CompleteOperation();
            return transmittedOctets;
        }
    }

    internal bool HandleDeleCommand(
        Pop3Command command,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3CommandDispatcher dispatcher = GetCommandDispatcher();
        dispatcher.RequirePendingCommand(Pop3Commands.Dele);
        ValidateDeleCommand(command);
        ThrowIfCancellationRequestedAndAbort(cancellationToken);
        int messageNumber = ParseDeleMessageNumber(command.Arguments[0]);
        Pop3MessageInfo[] messages = MailboxTransaction.GetMessages();
        if (messageNumber > messages.Length)
        {
            WriteDeleFailure(
                "dele-message-unavailable",
                "No such message.",
                cancellationToken);
            return false;
        }
        if (messages[messageNumber - 1].IsDeleted)
        {
            WriteDeleFailure(
                "dele-already-deleted",
                "Message is already marked for deletion.",
                cancellationToken);
            return false;
        }

        using (Pop3ServerFailureGuard guard =
            new Pop3ServerFailureGuard(this))
        {
            MailboxTransaction.MarkDeleted(messageNumber);
            Pop3Response response = new Pop3Response();
            response.Kind = Pop3ResponseKind.Positive;
            response.Text = LanguageSession.ResolveResponseText(
                GetOptions(),
                StateMachine,
                "dele-success",
                "Message marked for deletion.");
            dispatcher.WriteDispatchedResponse(response, cancellationToken);
            guard.CompleteOperation();
            return true;
        }
    }

    internal Pop3Response HandleNoopCommand(
        Pop3Command command,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3CommandDispatcher dispatcher = GetCommandDispatcher();
        dispatcher.RequirePendingCommand(Pop3Commands.Noop);
        ValidateNoopCommand(command);
        ThrowIfCancellationRequestedAndAbort(cancellationToken);
        Pop3Response response = new Pop3Response();
        response.Kind = Pop3ResponseKind.Positive;
        response.Text = LanguageSession.ResolveResponseText(
            GetOptions(),
            StateMachine,
            "noop-success",
            "No operation.");
        dispatcher.WriteDispatchedResponse(response, cancellationToken);
        return response;
    }

    internal Pop3MailboxStatus HandleRsetCommand(
        Pop3Command command,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3CommandDispatcher dispatcher = GetCommandDispatcher();
        dispatcher.RequirePendingCommand(Pop3Commands.Rset);
        ValidateRsetCommand(command);
        ThrowIfCancellationRequestedAndAbort(cancellationToken);
        using (Pop3ServerFailureGuard guard =
            new Pop3ServerFailureGuard(this))
        {
            Pop3MailboxStatus status = MailboxTransaction.ResetDeletions();
            string responseText = status.MessageCount.ToString(
                System.Globalization.CultureInfo.InvariantCulture) +
                " " +
                status.TotalOctets.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
            string localizedText = LanguageSession.ResolveResponseText(
                GetOptions(),
                StateMachine,
                "rset-success",
                "Maildrop restored.");
            if (localizedText.Length > 0)
            {
                responseText += " " + localizedText;
            }
            Pop3Response response = new Pop3Response();
            response.Kind = Pop3ResponseKind.Positive;
            response.Text = responseText;
            dispatcher.WriteDispatchedResponse(response, cancellationToken);
            guard.CompleteOperation();
            return status;
        }
    }

    internal bool HandleQuitCommand(
        Pop3Command command,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3CommandDispatcher dispatcher = GetCommandDispatcher();
        dispatcher.RequirePendingCommand(Pop3Commands.Quit);
        ValidateQuitCommand(command);
        cancellationToken.ThrowIfCancellationRequested();
        Pop3SessionState state = StateMachine.GetCurrentState();
        if (state == Pop3SessionState.Authorization)
        {
            WriteQuitResponseAndClose(
                dispatcher,
                Pop3ResponseKind.Positive,
                "quit-authorization-success",
                "Server signing off.");
            return true;
        }
        if (state != Pop3SessionState.Transaction)
        {
            throw new Pop3StateException(
                "QUIT requires an authorization or transaction session.",
                Pop3Commands.Quit,
                state);
        }

        StateMachine.EnterUpdate();
        bool committed = MailboxTransaction.CommitAndRelease(
            cancellationToken);
        if (committed)
        {
            WriteQuitResponseAndClose(
                dispatcher,
                Pop3ResponseKind.Positive,
                "quit-update-success",
                "Update committed; server signing off.");
        }
        else
        {
            WriteQuitResponseAndClose(
                dispatcher,
                Pop3ResponseKind.Negative,
                "quit-update-failed",
                "Unable to commit update; server signing off.");
        }
        return committed;
    }

    internal void CloseSession()
    {
        MailboxTransaction.RollbackAndRelease();
        Options = null;
        WireReader = null;
        WireWriter = null;
        CommandDispatcher = null;
        AuthorizationExchange.AbortExchange();
        StateMachine.CloseConnection();
    }

    internal void AbortSession()
    {
        CloseSession();
    }

    Pop3CommandDispatcher GetCommandDispatcher()
    {
        if (CommandDispatcher == null)
        {
            throw new Pop3StateException(
                "A POP3 server command dispatcher is not available.",
                "GetCommandDispatcher",
                StateMachine.GetCurrentState());
        }
        return CommandDispatcher;
    }

    Pop3Command CopyExtensionCommand(Pop3Command source)
    {
        if (source.Keyword == null || source.Arguments == null)
        {
            throw new System.IO.InvalidDataException(
                "A POP3 extension command contains invalid command data.");
        }
        Pop3Command copy = new Pop3Command();
        copy.Keyword = source.Keyword;
        copy.Arguments = new string[source.Arguments.Length];
        System.Array.Copy(
            source.Arguments,
            copy.Arguments,
            source.Arguments.Length);
        copy.ContainsSensitiveData = source.ContainsSensitiveData;
        copy.ExpectsMultilineResponse = source.ExpectsMultilineResponse;
        return copy;
    }

    Pop3Command CompleteCommandRead(
        System.Threading.Tasks.Task<Pop3Command> task,
        System.Threading.CancellationToken cancellationToken,
        System.Threading.CancellationToken timeoutToken,
        System.Threading.CancellationToken inactivityToken)
    {
        if (task.IsCompletedSuccessfully)
        {
            return task.Result;
        }

        System.Exception? failure = GetTaskFailure(task);
        if (cancellationToken.IsCancellationRequested)
        {
            AbortSession();
            cancellationToken.ThrowIfCancellationRequested();
        }
        if (inactivityToken.IsCancellationRequested)
        {
            Pop3SessionState state = StateMachine.GetCurrentState();
            AbortSession();
            throw new Pop3ConnectionException(
                "The POP3 server inactivity period expired.",
                "Inactivity autologout",
                state,
                false,
                true,
                failure);
        }
        if (timeoutToken.IsCancellationRequested)
        {
            Pop3SessionState state = StateMachine.GetCurrentState();
            AbortSession();
            throw new Pop3ConnectionException(
                "Reading a POP3 client command timed out.",
                "Command read",
                state,
                false,
                true,
                failure);
        }
        if (failure != null)
        {
            Pop3StateException? stateFailure =
                failure as Pop3StateException;
            if (stateFailure != null)
            {
                throw stateFailure;
            }
            AbortSession();
            throw failure;
        }
        AbortSession();
        throw new System.IO.InvalidDataException(
            "Reading a POP3 client command failed.");
    }

    void ThrowIfCancellationRequestedAndAbort(
        System.Threading.CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            AbortSession();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    Pop3AuthenticationResult CreateMailboxAuthenticationFailure(
        Pop3MailboxOpenResult result)
    {
        Pop3AuthenticationResult failure = new Pop3AuthenticationResult();
        failure.Status = result.Status;
        failure.RetryAfterSeconds = result.RetryAfterSeconds;
        failure.FailureText = result.FailureText;
        return failure;
    }

    void ValidateUtf8Command(Pop3Command command)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }
        if (command.Keyword == null || command.Arguments == null ||
            !string.Equals(
                command.Keyword,
                Pop3Commands.Utf8,
                System.StringComparison.OrdinalIgnoreCase) ||
            command.Arguments.Length != 0)
        {
            throw new System.ArgumentException(
                "A UTF8 operation requires a parsed UTF8 command.",
                "command");
        }
    }

    void ValidateLangCommand(Pop3Command command)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }
        if (command.Keyword == null || command.Arguments == null ||
            !string.Equals(
                command.Keyword,
                Pop3Commands.Lang,
                System.StringComparison.OrdinalIgnoreCase) ||
            command.Arguments.Length > 1)
        {
            throw new System.ArgumentException(
                "A LANG operation requires a parsed LANG command.",
                "command");
        }
    }

    void WriteLangResponse(
        Pop3ResponseKind kind,
        string responseTextKey,
        string fallbackText,
        string? selectedLanguage,
        System.Threading.CancellationToken cancellationToken)
    {
        string responseText = LanguageSession.ResolveResponseText(
            GetOptions(),
            StateMachine,
            responseTextKey,
            fallbackText);
        if (selectedLanguage != null)
        {
            responseText = selectedLanguage + " " + responseText;
        }
        Pop3Response response = new Pop3Response();
        response.Kind = kind;
        response.Text = responseText;
        GetCommandDispatcher().WriteDispatchedResponse(
            response,
            cancellationToken);
    }

    void ValidateStatCommand(Pop3Command command)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }
        if (command.Keyword == null || command.Arguments == null ||
            !string.Equals(
                command.Keyword,
                Pop3Commands.Stat,
                System.StringComparison.OrdinalIgnoreCase) ||
            command.Arguments.Length != 0)
        {
            throw new System.ArgumentException(
                "A STAT operation requires a parsed STAT command.",
                "command");
        }
    }

    void ValidateStatStatus(Pop3MailboxStatus status)
    {
        if (status == null)
        {
            throw new System.IO.InvalidDataException(
                "A POP3 mailbox transaction returned a null status.");
        }
        if (status.MessageCount < 0 || status.TotalOctets < 0 ||
            status.MessageCount == 0 && status.TotalOctets != 0)
        {
            throw new System.IO.InvalidDataException(
                "A POP3 mailbox transaction returned an invalid status.");
        }
    }

    void ValidateListCommand(Pop3Command command)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }
        if (command.Keyword == null || command.Arguments == null ||
            !string.Equals(
                command.Keyword,
                Pop3Commands.List,
                System.StringComparison.OrdinalIgnoreCase) ||
            command.Arguments.Length > 1)
        {
            throw new System.ArgumentException(
                "A LIST operation requires a parsed LIST command.",
                "command");
        }
        if (command.Arguments.Length == 1)
        {
            ParseListMessageNumber(command.Arguments[0]);
        }
    }

    int ParseListMessageNumber(string text)
    {
        if (text == null || text.Length == 0)
        {
            throw new System.ArgumentException(
                "A LIST message number must be a positive decimal integer.",
                "command");
        }
        int value = 0;
        int index = 0;
        while (index < text.Length)
        {
            int digit = text[index] - '0';
            if (digit < 0 || digit > 9 ||
                value > (int.MaxValue - digit) / 10)
            {
                throw new System.ArgumentException(
                    "A LIST message number must be a positive decimal integer.",
                    "command");
            }
            value = value * 10 + digit;
            index++;
        }
        if (value < 1)
        {
            throw new System.ArgumentException(
                "A LIST message number must be a positive decimal integer.",
                "command");
        }
        return value;
    }

    bool RequiresUtf8ForList(Pop3MessageInfo message)
    {
        return message.IsUtf8Representation &&
            !StateMachine.GetIsUtf8Enabled();
    }

    Pop3MessageInfo[] CreateVisibleList(Pop3MessageInfo[] messages)
    {
        int visibleCount = 0;
        int index = 0;
        while (index < messages.Length)
        {
            if (!messages[index].IsDeleted)
            {
                visibleCount++;
            }
            index++;
        }
        Pop3MessageInfo[] visible = new Pop3MessageInfo[visibleCount];
        index = 0;
        int writeIndex = 0;
        while (index < messages.Length)
        {
            if (!messages[index].IsDeleted)
            {
                visible[writeIndex] = messages[index];
                writeIndex++;
            }
            index++;
        }
        return visible;
    }

    string FormatListEntry(Pop3MessageInfo message)
    {
        return message.MessageNumber.ToString(
            System.Globalization.CultureInfo.InvariantCulture) +
            " " +
            message.OctetCount.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
    }

    void WriteListFailure(
        string responseTextKey,
        string fallbackText,
        bool utf8Failure,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3ServerOptions options = GetOptions();
        Pop3Response response = new Pop3Response();
        response.Kind = Pop3ResponseKind.Negative;
        response.Text = LanguageSession.ResolveResponseText(
            options,
            StateMachine,
            responseTextKey,
            fallbackText);
        if (utf8Failure && options.EnableResponseCodes)
        {
            response.ResponseCodeLevels = new string[]
            {
                Pop3ResponseCodes.Utf8
            };
        }
        WriteNextResponse(response, cancellationToken);
    }

    void ValidateUidlCommand(Pop3Command command)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }
        if (command.Keyword == null || command.Arguments == null ||
            !string.Equals(
                command.Keyword,
                Pop3Commands.Uidl,
                System.StringComparison.OrdinalIgnoreCase) ||
            command.Arguments.Length > 1)
        {
            throw new System.ArgumentException(
                "A UIDL operation requires a parsed UIDL command.",
                "command");
        }
        if (command.Arguments.Length == 1)
        {
            ParseUidlMessageNumber(command.Arguments[0]);
        }
    }

    int ParseUidlMessageNumber(string text)
    {
        if (text == null || text.Length == 0)
        {
            throw new System.ArgumentException(
                "A UIDL message number must be a positive decimal integer.",
                "command");
        }
        int value = 0;
        int index = 0;
        while (index < text.Length)
        {
            int digit = text[index] - '0';
            if (digit < 0 || digit > 9 ||
                value > (int.MaxValue - digit) / 10)
            {
                throw new System.ArgumentException(
                    "A UIDL message number must be a positive decimal integer.",
                    "command");
            }
            value = value * 10 + digit;
            index++;
        }
        if (value < 1)
        {
            throw new System.ArgumentException(
                "A UIDL message number must be a positive decimal integer.",
                "command");
        }
        return value;
    }

    Pop3UniqueId[] CreateVisibleUniqueIds(
        Pop3MessageInfo[] messages,
        Pop3UniqueId[] uniqueIds)
    {
        if (messages.Length != uniqueIds.Length)
        {
            throw new System.IO.InvalidDataException(
                "The POP3 transaction UIDL snapshot is inconsistent.");
        }
        int visibleCount = 0;
        int index = 0;
        while (index < messages.Length)
        {
            if (!messages[index].IsDeleted)
            {
                visibleCount++;
            }
            index++;
        }
        Pop3UniqueId[] visible = new Pop3UniqueId[visibleCount];
        index = 0;
        int writeIndex = 0;
        while (index < messages.Length)
        {
            if (!messages[index].IsDeleted)
            {
                visible[writeIndex] = uniqueIds[index];
                writeIndex++;
            }
            index++;
        }
        return visible;
    }

    string FormatUniqueIdEntry(Pop3UniqueId uniqueId)
    {
        return uniqueId.MessageNumber.ToString(
            System.Globalization.CultureInfo.InvariantCulture) +
            " " + uniqueId.Value;
    }

    void WriteUidlFailure(
        string responseTextKey,
        string fallbackText,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3Response response = new Pop3Response();
        response.Kind = Pop3ResponseKind.Negative;
        response.Text = LanguageSession.ResolveResponseText(
            GetOptions(),
            StateMachine,
            responseTextKey,
            fallbackText);
        WriteNextResponse(response, cancellationToken);
    }

    void ValidateRetrCommand(Pop3Command command)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }
        if (command.Keyword == null || command.Arguments == null ||
            !string.Equals(
                command.Keyword,
                Pop3Commands.Retr,
                System.StringComparison.OrdinalIgnoreCase) ||
            command.Arguments.Length != 1)
        {
            throw new System.ArgumentException(
                "A RETR operation requires a parsed RETR command.",
                "command");
        }
        ParseRetrMessageNumber(command.Arguments[0]);
    }

    int ParseRetrMessageNumber(string text)
    {
        if (text == null || text.Length == 0)
        {
            throw new System.ArgumentException(
                "A RETR message number must be a positive decimal integer.",
                "command");
        }
        int value = 0;
        int index = 0;
        while (index < text.Length)
        {
            int digit = text[index] - '0';
            if (digit < 0 || digit > 9 ||
                value > (int.MaxValue - digit) / 10)
            {
                throw new System.ArgumentException(
                    "A RETR message number must be a positive decimal integer.",
                    "command");
            }
            value = value * 10 + digit;
            index++;
        }
        if (value < 1)
        {
            throw new System.ArgumentException(
                "A RETR message number must be a positive decimal integer.",
                "command");
        }
        return value;
    }

    void WriteRetrFailure(
        string responseTextKey,
        string fallbackText,
        bool utf8Failure,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3ServerOptions options = GetOptions();
        Pop3Response response = new Pop3Response();
        response.Kind = Pop3ResponseKind.Negative;
        response.Text = LanguageSession.ResolveResponseText(
            options,
            StateMachine,
            responseTextKey,
            fallbackText);
        if (utf8Failure && options.EnableResponseCodes)
        {
            response.ResponseCodeLevels = new string[]
            {
                Pop3ResponseCodes.Utf8
            };
        }
        WriteNextResponse(response, cancellationToken);
    }

    void ValidateTopCommand(Pop3Command command)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }
        if (command.Keyword == null || command.Arguments == null ||
            !string.Equals(
                command.Keyword,
                Pop3Commands.Top,
                System.StringComparison.OrdinalIgnoreCase) ||
            command.Arguments.Length != 2)
        {
            throw new System.ArgumentException(
                "A TOP operation requires a parsed TOP command.",
                "command");
        }
        ParseTopMessageNumber(command.Arguments[0]);
        ParseTopBodyLineCount(command.Arguments[1]);
    }

    int ParseTopMessageNumber(string text)
    {
        if (text == null || text.Length == 0)
        {
            throw new System.ArgumentException(
                "A TOP message number must be a positive decimal integer.",
                "command");
        }
        int value = 0;
        int index = 0;
        while (index < text.Length)
        {
            int digit = text[index] - '0';
            if (digit < 0 || digit > 9 ||
                value > (int.MaxValue - digit) / 10)
            {
                throw new System.ArgumentException(
                    "A TOP message number must be a positive decimal integer.",
                    "command");
            }
            value = value * 10 + digit;
            index++;
        }
        if (value < 1)
        {
            throw new System.ArgumentException(
                "A TOP message number must be a positive decimal integer.",
                "command");
        }
        return value;
    }

    int ParseTopBodyLineCount(string text)
    {
        if (text == null || text.Length == 0)
        {
            throw new System.ArgumentException(
                "A TOP body-line count must be a nonnegative decimal integer.",
                "command");
        }
        int value = 0;
        int index = 0;
        while (index < text.Length)
        {
            int digit = text[index] - '0';
            if (digit < 0 || digit > 9 ||
                value > (int.MaxValue - digit) / 10)
            {
                throw new System.ArgumentException(
                    "A TOP body-line count must be a nonnegative decimal integer.",
                    "command");
            }
            value = value * 10 + digit;
            index++;
        }
        return value;
    }

    void WriteTopFailure(
        string responseTextKey,
        string fallbackText,
        bool utf8Failure,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3ServerOptions options = GetOptions();
        Pop3Response response = new Pop3Response();
        response.Kind = Pop3ResponseKind.Negative;
        response.Text = LanguageSession.ResolveResponseText(
            options,
            StateMachine,
            responseTextKey,
            fallbackText);
        if (utf8Failure && options.EnableResponseCodes)
        {
            response.ResponseCodeLevels = new string[]
            {
                Pop3ResponseCodes.Utf8
            };
        }
        WriteNextResponse(response, cancellationToken);
    }

    void ValidateDeleCommand(Pop3Command command)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }
        if (command.Keyword == null || command.Arguments == null ||
            !string.Equals(
                command.Keyword,
                Pop3Commands.Dele,
                System.StringComparison.OrdinalIgnoreCase) ||
            command.Arguments.Length != 1)
        {
            throw new System.ArgumentException(
                "A DELE operation requires a parsed DELE command.",
                "command");
        }
        ParseDeleMessageNumber(command.Arguments[0]);
    }

    int ParseDeleMessageNumber(string text)
    {
        if (text == null || text.Length == 0)
        {
            throw new System.ArgumentException(
                "A DELE message number must be a positive decimal integer.",
                "command");
        }
        int value = 0;
        int index = 0;
        while (index < text.Length)
        {
            int digit = text[index] - '0';
            if (digit < 0 || digit > 9 ||
                value > (int.MaxValue - digit) / 10)
            {
                throw new System.ArgumentException(
                    "A DELE message number must be a positive decimal integer.",
                    "command");
            }
            value = value * 10 + digit;
            index++;
        }
        if (value < 1)
        {
            throw new System.ArgumentException(
                "A DELE message number must be a positive decimal integer.",
                "command");
        }
        return value;
    }

    void WriteDeleFailure(
        string responseTextKey,
        string fallbackText,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3Response response = new Pop3Response();
        response.Kind = Pop3ResponseKind.Negative;
        response.Text = LanguageSession.ResolveResponseText(
            GetOptions(),
            StateMachine,
            responseTextKey,
            fallbackText);
        WriteNextResponse(response, cancellationToken);
    }

    void ValidateNoopCommand(Pop3Command command)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }
        if (command.Keyword == null || command.Arguments == null ||
            !string.Equals(
                command.Keyword,
                Pop3Commands.Noop,
                System.StringComparison.OrdinalIgnoreCase) ||
            command.Arguments.Length != 0)
        {
            throw new System.ArgumentException(
                "A NOOP operation requires a parsed NOOP command.",
                "command");
        }
    }

    void ValidateRsetCommand(Pop3Command command)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }
        if (command.Keyword == null || command.Arguments == null ||
            !string.Equals(
                command.Keyword,
                Pop3Commands.Rset,
                System.StringComparison.OrdinalIgnoreCase) ||
            command.Arguments.Length != 0)
        {
            throw new System.ArgumentException(
                "An RSET operation requires a parsed RSET command.",
                "command");
        }
    }

    void ValidateQuitCommand(Pop3Command command)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }
        if (command.Keyword == null || command.Arguments == null ||
            !string.Equals(
                command.Keyword,
                Pop3Commands.Quit,
                System.StringComparison.OrdinalIgnoreCase) ||
            command.Arguments.Length != 0)
        {
            throw new System.ArgumentException(
                "A QUIT operation requires a parsed QUIT command.",
                "command");
        }
    }

    void WriteQuitResponseAndClose(
        Pop3CommandDispatcher dispatcher,
        Pop3ResponseKind kind,
        string responseTextKey,
        string fallbackText)
    {
        System.Threading.Tasks.Task responseTask =
            System.Threading.Tasks.Task.Run(() =>
                WriteQuitResponse(
                    dispatcher,
                    kind,
                    responseTextKey,
                    fallbackText));
        System.Threading.Tasks.Task closeTask = responseTask.ContinueWith(
            task => CompleteQuitResponse(task),
            System.Threading.CancellationToken.None,
            System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
            System.Threading.Tasks.TaskScheduler.Default);
        closeTask.GetAwaiter().GetResult();
    }

    void WriteQuitResponse(
        Pop3CommandDispatcher dispatcher,
        Pop3ResponseKind kind,
        string responseTextKey,
        string fallbackText)
    {
        Pop3Response response = new Pop3Response();
        response.Kind = kind;
        response.Text = LanguageSession.ResolveResponseText(
            GetOptions(),
            StateMachine,
            responseTextKey,
            fallbackText);
        dispatcher.WriteDispatchedResponse(
            response,
            System.Threading.CancellationToken.None);
    }

    void CompleteQuitResponse(System.Threading.Tasks.Task responseTask)
    {
        CloseSession();
        responseTask.GetAwaiter().GetResult();
    }

    bool CanUpgradeTransportToTls(Pop3ServerOptions options)
    {
        return StateMachine.GetCurrentState() == Pop3SessionState.Authorization &&
            !StateMachine.GetIsSecure() &&
            !StateMachine.GetIsAuthenticated() &&
            !StateMachine.GetIsUtf8Enabled() &&
            !AuthorizationExchange.HasPendingUser() &&
            !AuthorizationExchange.HasSuccessfulSaslAuthentication() &&
            (options.SecurityMode == Pop3SecurityMode.StlsIfAvailable ||
                options.SecurityMode == Pop3SecurityMode.StlsRequired);
    }

    void InitializeWireComponents(System.IO.Stream stream, int bufferSize)
    {
        Pop3ServerOptions options = GetOptions();
        Pop3WireReader reader = new Pop3WireReader(stream, bufferSize);
        Pop3WireWriter writer = new Pop3WireWriter(stream, bufferSize);
        WireReader = reader;
        WireWriter = writer;
        CommandDispatcher = new Pop3CommandDispatcher(
            reader,
            writer,
            StateMachine,
            options.EnablePipelining,
            options.EnableUtf8 && options.EnableUtf8User);
        CommandDispatcher.SetExtensionDispatcher(ExtensionDispatcher);
        CommandDispatcher.SetProtocolLogger(options.Logger);
    }

    void ValidateStartupInputs(
        Pop3ServerConnection connection,
        Pop3ServerOptions options)
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
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        if (options.ReadTimeoutMilliseconds <= 0 ||
            options.WriteTimeoutMilliseconds <= 0 ||
            options.AuthenticationTimeoutMilliseconds <= 0 ||
            options.BufferSize <= 0 ||
            options.MaximumMessageLineOctets < 3 ||
            options.MaximumSaslLineOctets < 4)
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
        if (!connection.IsSecure &&
            options.SecurityMode != Pop3SecurityMode.Plaintext)
        {
            ValidateTlsConfiguration(options);
        }
    }

    void ValidateTlsConfiguration(Pop3ServerOptions options)
    {
        if (options.Tls == null)
        {
            throw new System.ArgumentException(
                "POP3 server TLS options cannot be null.",
                "options");
        }
        System.Security.Authentication.SslProtocols modernProtocols =
            System.Security.Authentication.SslProtocols.Tls12 |
            System.Security.Authentication.SslProtocols.Tls13;
        if ((options.Tls.EnabledProtocols & modernProtocols) == 0 ||
            (options.Tls.EnabledProtocols & ~modernProtocols) != 0)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }
        if (options.TlsProvider == null &&
            (options.Tls.ServerCertificate == null ||
                !options.Tls.ServerCertificate.HasPrivateKey))
        {
            throw new System.ArgumentException(
                "The default POP3 server TLS provider requires a certificate with a private key.",
                "options");
        }
    }

    System.IO.Stream SecureTransport(
        System.IO.Stream stream,
        Pop3ServerOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3TlsServerOptions tlsOptions = CopyTlsOptions(options);
        IPop3ServerTlsProvider? configuredProvider = options.TlsProvider;
        IPop3ServerTlsProvider provider;
        if (configuredProvider == null)
        {
            provider = new Pop3DefaultServerTlsProvider();
        }
        else
        {
            provider = configuredProvider;
        }

        using (System.Threading.CancellationTokenSource timeoutSource =
            new System.Threading.CancellationTokenSource(
                options.AuthenticationTimeoutMilliseconds))
        using (System.Threading.CancellationTokenSource linkedSource =
            System.Threading.CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token))
        {
            System.Threading.Tasks.Task<System.IO.Stream> secureTask =
                System.Threading.Tasks.Task.Run(
                    () => provider.SecureServerStream(
                        stream,
                        tlsOptions,
                        linkedSource.Token));
            System.Threading.Tasks.Task<System.IO.Stream> completionTask =
                secureTask.ContinueWith(
                    task => CompleteSecureTransport(
                        task,
                        stream,
                        options.SecurityMode,
                        cancellationToken,
                        timeoutSource.Token),
                    System.Threading.CancellationToken.None,
                    System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                    System.Threading.Tasks.TaskScheduler.Default);
            return completionTask.GetAwaiter().GetResult();
        }
    }

    Pop3TlsServerOptions CopyTlsOptions(Pop3ServerOptions options)
    {
        Pop3TlsServerOptions copy = new Pop3TlsServerOptions();
        copy.ServerCertificate = options.Tls.ServerCertificate;
        copy.EnabledProtocols = options.Tls.EnabledProtocols;
        copy.RequireClientCertificate = options.Tls.RequireClientCertificate;
        copy.CheckCertificateRevocation = options.Tls.CheckCertificateRevocation;
        copy.LeaveInnerStreamOpen = true;
        return copy;
    }

    System.IO.Stream CompleteSecureTransport(
        System.Threading.Tasks.Task<System.IO.Stream> task,
        System.IO.Stream innerStream,
        Pop3SecurityMode securityMode,
        System.Threading.CancellationToken cancellationToken,
        System.Threading.CancellationToken timeoutToken)
    {
        if (task.IsCompletedSuccessfully)
        {
            System.IO.Stream? securedStream = task.Result;
            if (securedStream != null &&
                securedStream.CanRead &&
                securedStream.CanWrite)
            {
                return securedStream;
            }

            if (securedStream != null &&
                !object.ReferenceEquals(securedStream, innerStream))
            {
                securedStream.Dispose();
            }
            CloseSession();
            throw new Pop3TlsException(
                "The POP3 TLS provider returned an invalid secured stream.",
                securityMode,
                "SecureServerStream",
                null);
        }

        System.Exception? failure = GetTaskFailure(task);
        CloseSession();
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        if (timeoutToken.IsCancellationRequested)
        {
            throw new Pop3TlsException(
                "The POP3 server TLS handshake timed out.",
                securityMode,
                "SecureServerStream",
                failure);
        }
        throw new Pop3TlsException(
            "The POP3 server TLS handshake failed.",
            securityMode,
            "SecureServerStream",
            failure);
    }

    void WriteGreeting(
        Pop3WireWriter writer,
        Pop3Response greeting,
        Pop3ServerOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        using (System.Threading.CancellationTokenSource timeoutSource =
            new System.Threading.CancellationTokenSource(
                options.WriteTimeoutMilliseconds))
        using (System.Threading.CancellationTokenSource linkedSource =
            System.Threading.CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token))
        {
            System.Threading.Tasks.Task writeTask = System.Threading.Tasks.Task.Run(
                () => writer.WriteResponse(
                    greeting,
                    Pop3Defaults.MaximumResponseLineOctets,
                    linkedSource.Token));
            System.Threading.Tasks.Task completionTask = writeTask.ContinueWith(
                task => CompleteGreetingWrite(
                    task,
                    cancellationToken,
                    timeoutSource.Token),
                System.Threading.CancellationToken.None,
                System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                System.Threading.Tasks.TaskScheduler.Default);
            completionTask.GetAwaiter().GetResult();
        }
    }

    void CompleteGreetingWrite(
        System.Threading.Tasks.Task task,
        System.Threading.CancellationToken cancellationToken,
        System.Threading.CancellationToken timeoutToken)
    {
        if (task.IsCompletedSuccessfully)
        {
            return;
        }

        System.Exception? failure = GetTaskFailure(task);
        Pop3SessionState state = StateMachine.GetCurrentState();
        CloseSession();
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        if (timeoutToken.IsCancellationRequested)
        {
            throw new Pop3ConnectionException(
                "Writing the POP3 server greeting timed out.",
                "Greeting",
                state,
                false,
                true,
                failure);
        }
        throw new Pop3ConnectionException(
            "Writing the POP3 server greeting failed.",
            "Greeting",
            state,
            false,
            false,
            failure);
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
