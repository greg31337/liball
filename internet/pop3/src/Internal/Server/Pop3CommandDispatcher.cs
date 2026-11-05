namespace pop3;

internal class Pop3CommandDispatcher
{
    Pop3WireReader Reader;
    Pop3WireWriter Writer;
    Pop3ServerStateMachine StateMachine;
    Pop3CommandParser Parser = new Pop3CommandParser();
    bool PipeliningEnabled;
    bool Utf8UserEnabled;
    Pop3ExtensionDispatcher? ExtensionDispatcher;
    Pop3SecretRedactor Redactor = new Pop3SecretRedactor(null);
    System.Collections.Generic.List<Pop3ServerPendingResponse>
        PendingResponses =
            new System.Collections.Generic.List<Pop3ServerPendingResponse>();

    internal Pop3CommandDispatcher(
        Pop3WireReader reader,
        Pop3WireWriter writer,
        Pop3ServerStateMachine stateMachine,
        bool pipeliningEnabled,
        bool utf8UserEnabled)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }
        if (stateMachine == null)
        {
            throw new System.ArgumentNullException("stateMachine");
        }
        Reader = reader;
        Writer = writer;
        StateMachine = stateMachine;
        PipeliningEnabled = pipeliningEnabled;
        Utf8UserEnabled = utf8UserEnabled;
    }

    internal void SetExtensionDispatcher(
        Pop3ExtensionDispatcher extensionDispatcher)
    {
        if (extensionDispatcher == null)
        {
            throw new System.ArgumentNullException("extensionDispatcher");
        }
        ExtensionDispatcher = extensionDispatcher;
    }

    internal void SetProtocolLogger(IPop3ProtocolLogger? logger)
    {
        Redactor = new Pop3SecretRedactor(logger);
    }

    internal Pop3Command ReadDispatchableCommand(
        System.Threading.CancellationToken cancellationToken)
    {
        EnsureCommandIntakeAllowed();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Pop3ServerCommandLine line = Reader.ReadServerCommandLine(
                cancellationToken);
            if (line.EndOfStream)
            {
                throw new System.IO.EndOfStreamException(
                    "The POP3 client closed the stream before sending another command.");
            }

            StateMachine.RecordActivity(System.Environment.TickCount64);
            string? syntaxError = ValidateLineSyntax(line);
            if (syntaxError != null)
            {
                QueueOrWriteErrorResponse(syntaxError, cancellationToken);
                continue;
            }

            Pop3Command command = Parser.ParseServerCommand(
                line.Content,
                Utf8UserEnabled);
            if (!IsKnownCommand(command.Keyword))
            {
                QueueOrWriteErrorResponse(
                    "Unknown command.",
                    cancellationToken);
                continue;
            }
            if (!HasValidArguments(command))
            {
                QueueOrWriteErrorResponse(
                    "Invalid command arguments.",
                    cancellationToken);
                continue;
            }
            if (!IsCommandAllowed(
                command.Keyword,
                StateMachine.GetCurrentState()))
            {
                QueueOrWriteErrorResponse(
                    "Command is not valid in the current state.",
                    cancellationToken);
                continue;
            }
            if (CannotJoinPipeline(command.Keyword))
            {
                QueueOrWriteErrorResponse(
                    "The command cannot be used in a pipeline.",
                    cancellationToken);
                continue;
            }
            Pop3ServerPendingResponse pending =
                new Pop3ServerPendingResponse();
            pending.Keyword = command.Keyword;
            pending.IntakeBarrier = IsPipelineBarrier(command);
            LogCommand(command, line.Content.Length + 2);
            PendingResponses.Add(pending);
            return command;
        }
    }

    internal void RequirePendingCommand(string keyword)
    {
        if (keyword == null)
        {
            throw new System.ArgumentNullException("keyword");
        }
        if (PendingResponses.Count == 0 ||
            PendingResponses[0].ResponseStarted ||
            !string.Equals(
                PendingResponses[0].Keyword,
                keyword,
                System.StringComparison.OrdinalIgnoreCase))
        {
            throw new Pop3StateException(
                "The expected POP3 server command is not awaiting a response.",
                keyword,
                StateMachine.GetCurrentState());
        }
        PendingResponses[0].HandlingStarted = true;
    }

    internal void WriteDispatchedResponse(
        Pop3Response response,
        System.Threading.CancellationToken cancellationToken)
    {
        if (response == null)
        {
            throw new System.ArgumentNullException("response");
        }
        if (PendingResponses.Count == 0 ||
            PendingResponses[0].Keyword == null ||
            PendingResponses[0].ResponseStarted)
        {
            throw new Pop3StateException(
                "A POP3 server response requires a preceding dispatched command.",
                "WriteDispatchedResponse",
                StateMachine.GetCurrentState());
        }
        Writer.WriteResponse(
            response,
            Pop3Defaults.MaximumResponseLineOctets,
            cancellationToken);
        LogResponse(response);
        if (response.IsMultiline)
        {
            PendingResponses[0].ResponseStarted = true;
            return;
        }
        PendingResponses.RemoveAt(0);
        WriteDeferredErrorResponses(cancellationToken);
    }

    internal void CompleteMultilineResponse(
        System.Threading.CancellationToken cancellationToken)
    {
        if (PendingResponses.Count == 0 ||
            !PendingResponses[0].ResponseStarted)
        {
            throw new Pop3StateException(
                "A POP3 multiline response is not awaiting completion.",
                "CompleteMultilineResponse",
                StateMachine.GetCurrentState());
        }
        PendingResponses.RemoveAt(0);
        WriteDeferredErrorResponses(cancellationToken);
    }

    internal bool IsCommandAllowed(
        string keyword,
        Pop3SessionState state)
    {
        if (keyword == null)
        {
            throw new System.ArgumentNullException("keyword");
        }
        string canonicalKeyword = keyword.ToUpperInvariant();
        if (state == Pop3SessionState.Authorization)
        {
            if (canonicalKeyword == Pop3Commands.User ||
                canonicalKeyword == Pop3Commands.Pass ||
                canonicalKeyword == Pop3Commands.Apop ||
                canonicalKeyword == Pop3Commands.Auth ||
                canonicalKeyword == Pop3Commands.Quit ||
                canonicalKeyword == Pop3Commands.Capa ||
                canonicalKeyword == Pop3Commands.Stls ||
                canonicalKeyword == Pop3Commands.Utf8 ||
                canonicalKeyword == Pop3Commands.Lang)
            {
                return true;
            }
        }
        if (state == Pop3SessionState.Transaction)
        {
            if (canonicalKeyword == Pop3Commands.Stat ||
                canonicalKeyword == Pop3Commands.List ||
                canonicalKeyword == Pop3Commands.Retr ||
                canonicalKeyword == Pop3Commands.Dele ||
                canonicalKeyword == Pop3Commands.Noop ||
                canonicalKeyword == Pop3Commands.Rset ||
                canonicalKeyword == Pop3Commands.Quit ||
                canonicalKeyword == Pop3Commands.Top ||
                canonicalKeyword == Pop3Commands.Uidl ||
                canonicalKeyword == Pop3Commands.Capa ||
                canonicalKeyword == Pop3Commands.Lang)
            {
                return true;
            }
        }
        if (ExtensionDispatcher != null)
        {
            return ExtensionDispatcher.AllowsCommand(canonicalKeyword, state);
        }
        return false;
    }

    string? ValidateLineSyntax(Pop3ServerCommandLine line)
    {
        if (!line.IsValid)
        {
            return line.ErrorText;
        }
        if (line.Content.Length == 0)
        {
            return "A command cannot be empty.";
        }

        int keywordLength = 0;
        while (keywordLength < line.Content.Length &&
            line.Content[keywordLength] != 32)
        {
            if (!IsVisibleAscii(line.Content[keywordLength]))
            {
                return "The command contains invalid syntax.";
            }
            keywordLength++;
        }
        if (keywordLength < 3 || keywordLength > 4)
        {
            return "The command keyword must contain three or four characters.";
        }

        int index = keywordLength;
        while (index < line.Content.Length)
        {
            if (line.Content[index] != 32)
            {
                return "Command tokens must be separated by one space.";
            }
            index++;
            if (index == line.Content.Length || line.Content[index] == 32)
            {
                return "Command arguments cannot be empty.";
            }
            while (index < line.Content.Length && line.Content[index] != 32)
            {
                if (line.Content[index] < 33 || line.Content[index] == 127)
                {
                    return "The command contains invalid syntax.";
                }
                index++;
            }
        }
        if (!Parser.HasValidServerEncoding(
            line.Content,
            keywordLength,
            Utf8UserEnabled))
        {
            return "The command contains invalid UTF-8 syntax.";
        }
        return null;
    }

    bool IsVisibleAscii(byte value)
    {
        return value >= 33 && value <= 126;
    }

    void EnsureCommandIntakeAllowed()
    {
        if (PendingResponses.Count == 0)
        {
            return;
        }
        if (!PipeliningEnabled)
        {
            throw new Pop3StateException(
                "The preceding POP3 server command must receive its response before another command is dispatched.",
                "ReadDispatchableCommand",
                StateMachine.GetCurrentState());
        }
        if (PendingResponses[0].HandlingStarted ||
            PendingResponses[0].ResponseStarted)
        {
            throw new Pop3StateException(
                "A POP3 command response must finish before more commands are dispatched.",
                "ReadDispatchableCommand",
                StateMachine.GetCurrentState());
        }

        int index = 0;
        while (index < PendingResponses.Count)
        {
            if (PendingResponses[index].IntakeBarrier)
            {
                throw new Pop3StateException(
                    "A POP3 pipeline barrier must finish before more commands are dispatched.",
                    "ReadDispatchableCommand",
                    StateMachine.GetCurrentState());
            }
            index++;
        }
    }

    bool IsPipelineBarrier(Pop3Command command)
    {
        string keyword = command.Keyword;
        return keyword == Pop3Commands.Stls ||
            keyword == Pop3Commands.Auth ||
            keyword == Pop3Commands.Pass ||
            keyword == Pop3Commands.Apop ||
            keyword == Pop3Commands.Utf8 ||
            keyword == Pop3Commands.Quit ||
            keyword == Pop3Commands.Lang && command.Arguments.Length == 1 ||
            ExtensionDispatcher != null &&
                ExtensionDispatcher.HandlesCommand(keyword);
    }

    bool CannotJoinPipeline(string keyword)
    {
        if (PendingResponses.Count == 0)
        {
            return false;
        }
        return keyword == Pop3Commands.Stls ||
            keyword == Pop3Commands.Auth ||
            keyword == Pop3Commands.Utf8 ||
            ExtensionDispatcher != null &&
                ExtensionDispatcher.HandlesCommand(keyword);
    }

    bool IsKnownCommand(string keyword)
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
            keyword == Pop3Commands.Lang ||
            ExtensionDispatcher != null &&
                ExtensionDispatcher.HandlesCommand(keyword);
    }

    bool HasValidArguments(Pop3Command command)
    {
        int count = command.Arguments.Length;
        string keyword = command.Keyword;
        if (keyword == Pop3Commands.User || keyword == Pop3Commands.Pass)
        {
            return count == 1;
        }
        if (keyword == Pop3Commands.Apop)
        {
            return count == 2;
        }
        if (keyword == Pop3Commands.List || keyword == Pop3Commands.Uidl)
        {
            return count == 0 || count == 1 &&
                IsDecimalArgument(command.Arguments[0], 1);
        }
        if (keyword == Pop3Commands.Retr || keyword == Pop3Commands.Dele)
        {
            return count == 1 && IsDecimalArgument(command.Arguments[0], 1);
        }
        if (keyword == Pop3Commands.Top)
        {
            return count == 2 &&
                IsDecimalArgument(command.Arguments[0], 1) &&
                IsDecimalArgument(command.Arguments[1], 0);
        }
        if (keyword == Pop3Commands.Auth)
        {
            return count >= 1 && count <= 2;
        }
        if (keyword == Pop3Commands.Lang)
        {
            return count <= 1;
        }
        if (ExtensionDispatcher != null &&
            ExtensionDispatcher.HandlesCommand(keyword))
        {
            return true;
        }
        return count == 0;
    }

    bool IsDecimalArgument(string argument, int minimum)
    {
        if (argument.Length == 0)
        {
            return false;
        }
        int index = 0;
        while (index < argument.Length)
        {
            char character = argument[index];
            if (character < '0' || character > '9')
            {
                return false;
            }
            index++;
        }
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(argument);
        return new Pop3NumberParser().ParseSignedDecimal(
            bytes,
            minimum,
            int.MaxValue).Success;
    }

    void WriteErrorResponse(
        string text,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3Response response = new Pop3Response();
        response.Kind = Pop3ResponseKind.Negative;
        response.Text = text;
        Writer.WriteResponse(
            response,
            Pop3Defaults.MaximumResponseLineOctets,
            cancellationToken);
        LogResponse(response);
    }

    void LogCommand(Pop3Command command, int byteCount)
    {
        if (ExtensionDispatcher != null &&
            ExtensionDispatcher.HandlesCommand(command.Keyword))
        {
            return;
        }
        Redactor.LogCommand(
            Pop3LogDirection.ClientToServer,
            command,
            byteCount,
            StateMachine.GetCurrentState());
    }

    void LogResponse(Pop3Response response)
    {
        byte[] line = new Pop3ResponseParser().FormatResponse(
            response,
            Pop3Defaults.MaximumResponseLineOctets);
        Redactor.LogResponse(
            Pop3LogDirection.ServerToClient,
            response,
            line.Length + 2,
            StateMachine.GetCurrentState());
    }

    void QueueOrWriteErrorResponse(
        string text,
        System.Threading.CancellationToken cancellationToken)
    {
        if (PendingResponses.Count == 0)
        {
            WriteErrorResponse(text, cancellationToken);
            return;
        }
        Pop3ServerPendingResponse pending =
            new Pop3ServerPendingResponse();
        pending.ErrorText = text;
        PendingResponses.Add(pending);
    }

    void WriteDeferredErrorResponses(
        System.Threading.CancellationToken cancellationToken)
    {
        while (PendingResponses.Count > 0 &&
            PendingResponses[0].Keyword == null)
        {
            string? errorText = PendingResponses[0].ErrorText;
            if (errorText == null)
            {
                throw new System.InvalidOperationException(
                    "A deferred POP3 error response has no text.");
            }
            WriteErrorResponse(errorText, cancellationToken);
            PendingResponses.RemoveAt(0);
        }
    }
}
