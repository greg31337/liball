namespace pop3;

internal class Pop3AuthorizationExchange
{
    string? PendingUsername;
    IPop3SaslServerSession? SuccessfulSaslSession;

    internal Pop3Response AcceptUser(
        Pop3Command command,
        Pop3ServerOptions options,
        Pop3ServerStateMachine stateMachine,
        System.Threading.CancellationToken cancellationToken)
    {
        ValidateCommand(command, Pop3Commands.User);
        ValidateInputs(options, stateMachine);
        cancellationToken.ThrowIfCancellationRequested();
        PendingUsername = null;
        if (!CanUseUserPassword(options, stateMachine))
        {
            return CreateFailureResponse(
                Pop3AuthenticationStatus.Rejected,
                false,
                options.EnableResponseCodes);
        }

        PendingUsername = command.Arguments[0];
        Pop3Response response = new Pop3Response();
        response.Kind = Pop3ResponseKind.Positive;
        response.Text = "Send password.";
        return response;
    }

    internal Pop3AuthenticationResult AuthenticatePassword(
        Pop3Command command,
        Pop3ServerOptions options,
        Pop3ServerStateMachine stateMachine,
        System.Threading.CancellationToken cancellationToken)
    {
        ValidateCommand(command, Pop3Commands.Pass);
        ValidateInputs(options, stateMachine);
        cancellationToken.ThrowIfCancellationRequested();
        string? username = PendingUsername;
        PendingUsername = null;
        if (username == null || !CanUseUserPassword(options, stateMachine))
        {
            return CreateFailureResult(Pop3AuthenticationStatus.Rejected);
        }

        IPop3AuthenticationProvider? configuredProvider =
            options.AuthenticationProvider;
        if (configuredProvider == null)
        {
            return CreateFailureResult(Pop3AuthenticationStatus.Rejected);
        }
        IPop3AuthenticationProvider provider = configuredProvider;
        Pop3ServerConnection context = stateMachine.GetConnectionContext();
        Pop3AuthenticationResult delay = provider.CheckLoginDelay(
            username,
            context,
            cancellationToken);
        ValidateResult(delay, false);
        if (delay.Status != Pop3AuthenticationStatus.Success)
        {
            return CopyResult(delay);
        }

        Pop3Credentials credentials = new Pop3Credentials();
        credentials.Username = username;
        credentials.Password = command.Arguments[0];
        Pop3AuthenticationResult result = provider.AuthenticateUserPassword(
            credentials,
            context,
            cancellationToken);
        ValidateResult(result, true);
        return CopyResult(result);
    }

    internal Pop3AuthenticationResult AuthenticateApop(
        Pop3Command command,
        Pop3ServerOptions options,
        Pop3ServerStateMachine stateMachine,
        System.Threading.CancellationToken cancellationToken)
    {
        ValidateApopCommand(command);
        ValidateInputs(options, stateMachine);
        cancellationToken.ThrowIfCancellationRequested();
        PendingUsername = null;
        if (!CanUseApop(options, stateMachine) ||
            !IsValidApopDigest(command.Arguments[1]))
        {
            return CreateFailureResult(Pop3AuthenticationStatus.Rejected);
        }

        IPop3AuthenticationProvider? configuredProvider =
            options.AuthenticationProvider;
        string? timestamp = stateMachine.GetGreeting().GreetingTimestamp;
        if (configuredProvider == null || timestamp == null)
        {
            return CreateFailureResult(Pop3AuthenticationStatus.Rejected);
        }

        IPop3AuthenticationProvider provider = configuredProvider;
        Pop3ServerConnection context = stateMachine.GetConnectionContext();
        string username = command.Arguments[0];
        Pop3AuthenticationResult delay = provider.CheckLoginDelay(
            username,
            context,
            cancellationToken);
        ValidateResult(delay, false);
        if (delay.Status != Pop3AuthenticationStatus.Success)
        {
            return CopyResult(delay);
        }

        Pop3ApopRequest request = new Pop3ApopRequest();
        request.Username = username;
        request.Timestamp = timestamp;
        request.Digest = command.Arguments[1];
        Pop3AuthenticationResult result = provider.AuthenticateApop(
            request,
            context,
            cancellationToken);
        ValidateResult(result, true);
        return CopyResult(result);
    }

    internal Pop3AuthenticationResult AuthenticateSasl(
        Pop3Command command,
        Pop3ServerOptions options,
        Pop3ServerStateMachine stateMachine,
        Pop3WireReader reader,
        Pop3WireWriter writer,
        System.Threading.CancellationToken cancellationToken)
    {
        ValidateAuthCommand(command);
        ValidateInputs(options, stateMachine);
        cancellationToken.ThrowIfCancellationRequested();
        if (SuccessfulSaslSession != null)
        {
            return CreateFailureResult(Pop3AuthenticationStatus.Rejected);
        }
        PendingUsername = null;
        if (stateMachine.GetCurrentState() != Pop3SessionState.Authorization ||
            stateMachine.GetIsAuthenticated())
        {
            return CreateFailureResult(Pop3AuthenticationStatus.Rejected);
        }
        if (options.SaslFactories == null)
        {
            throw new System.ArgumentException(
                "POP3 SASL server factories cannot be null.",
                "options");
        }

        IPop3SaslServerFactory[] factories = CreateSaslFactories(
            options,
            stateMachine,
            cancellationToken);

        Pop3SaslExchange saslExchange = new Pop3SaslExchange();
        saslExchange.SetProtocolLogger(options.Logger);
        Pop3SaslServerExchangeResult exchange =
            saslExchange.AuthenticateServer(
                reader,
                writer,
                command,
                factories,
                stateMachine.GetConnectionContext(),
                options.MaximumSaslLineOctets,
                cancellationToken);
        bool succeeded = exchange.Status == Pop3SaslStepStatus.Success;
        ValidateResult(exchange.AuthenticationResult, succeeded);
        if (succeeded &&
            exchange.AuthenticationResult.Status !=
                Pop3AuthenticationStatus.Success)
        {
            throw new System.IO.InvalidDataException(
                "A successful POP3 SASL exchange returned a failed authentication result.");
        }
        if (!succeeded &&
            exchange.AuthenticationResult.Status ==
                Pop3AuthenticationStatus.Success)
        {
            throw new System.IO.InvalidDataException(
                "A failed POP3 SASL exchange returned a successful authentication result.");
        }
        if (succeeded)
        {
            if (exchange.Session == null)
            {
                throw new System.IO.InvalidDataException(
                    "A successful POP3 SASL exchange did not retain its session.");
            }
            SuccessfulSaslSession = exchange.Session;
        }
        return CopyResult(exchange.AuthenticationResult);
    }

    internal Pop3Response CreateAuthenticationFailureResponse(
        Pop3AuthenticationResult result,
        bool classifyCredentials,
        bool enableResponseCodes)
    {
        if (result == null)
        {
            throw new System.ArgumentNullException("result");
        }
        ValidateResult(result, false);
        if (result.Status == Pop3AuthenticationStatus.Success)
        {
            throw new System.ArgumentException(
                "A successful authentication result cannot produce a failure response.",
                "result");
        }
        return CreateFailureResponse(
            result.Status,
            classifyCredentials,
            enableResponseCodes);
    }

    internal bool HasPendingUser()
    {
        return PendingUsername != null;
    }

    internal bool HasSuccessfulSaslAuthentication()
    {
        return SuccessfulSaslSession != null;
    }

    internal System.IO.Stream ApplySuccessfulSaslSecurityLayer(
        System.IO.Stream stream,
        System.Threading.CancellationToken cancellationToken)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }
        if (SuccessfulSaslSession == null)
        {
            throw new System.InvalidOperationException(
                "A successful POP3 SASL session is not available.");
        }
        cancellationToken.ThrowIfCancellationRequested();
        IPop3SaslServerSession session = SuccessfulSaslSession;
        System.IO.Stream? suppliedStream = session.ApplySecurityLayer(stream);
        if (suppliedStream == null ||
            !suppliedStream.CanRead ||
            !suppliedStream.CanWrite)
        {
            if (suppliedStream != null &&
                !object.ReferenceEquals(suppliedStream, stream))
            {
                suppliedStream.Dispose();
            }
            throw new System.IO.InvalidDataException(
                "The POP3 SASL server session returned an invalid security stream.");
        }
        SuccessfulSaslSession = null;
        return suppliedStream;
    }

    internal void RejectSuccessfulAuthentication()
    {
        if (SuccessfulSaslSession != null)
        {
            SuccessfulSaslSession.CancelAuthentication();
        }
        PendingUsername = null;
        SuccessfulSaslSession = null;
    }

    internal void ResetExchange()
    {
        PendingUsername = null;
        SuccessfulSaslSession = null;
    }

    internal bool AbortExchange()
    {
        IPop3SaslServerSession? session = SuccessfulSaslSession;
        PendingUsername = null;
        SuccessfulSaslSession = null;
        if (session == null)
        {
            return true;
        }

        System.Threading.Tasks.Task cancellationTask =
            System.Threading.Tasks.Task.Run(() =>
                session.CancelAuthentication());
        System.Threading.Tasks.Task<bool> completionTask =
            cancellationTask.ContinueWith(
                task => GetAbortCompletion(task),
                System.Threading.CancellationToken.None,
                System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                System.Threading.Tasks.TaskScheduler.Default);
        return completionTask.GetAwaiter().GetResult();
    }

    bool GetAbortCompletion(System.Threading.Tasks.Task cancellationTask)
    {
        System.AggregateException? failure = cancellationTask.Exception;
        return cancellationTask.IsCompletedSuccessfully && failure == null;
    }

    bool CanUseUserPassword(
        Pop3ServerOptions options,
        Pop3ServerStateMachine stateMachine)
    {
        return stateMachine.GetCurrentState() == Pop3SessionState.Authorization &&
            !stateMachine.GetIsAuthenticated() &&
            SuccessfulSaslSession == null &&
            options.EnableUserPassword &&
            options.AuthenticationProvider != null &&
            (stateMachine.GetIsSecure() ||
                options.AllowUnprotectedCredentials &&
                options.SecurityMode != Pop3SecurityMode.StlsRequired);
    }

    bool CanUseApop(
        Pop3ServerOptions options,
        Pop3ServerStateMachine stateMachine)
    {
        return stateMachine.GetCurrentState() == Pop3SessionState.Authorization &&
            !stateMachine.GetIsAuthenticated() &&
            SuccessfulSaslSession == null &&
            options.EnableApop &&
            options.AuthenticationProvider != null &&
            stateMachine.GetGreeting().GreetingTimestamp != null;
    }

    IPop3SaslServerFactory[] CreateSaslFactories(
        Pop3ServerOptions options,
        Pop3ServerStateMachine stateMachine,
        System.Threading.CancellationToken cancellationToken)
    {
        bool credentialsAllowed = stateMachine.GetIsSecure() ||
            options.AllowUnprotectedCredentials &&
            options.SecurityMode != Pop3SecurityMode.StlsRequired;
        System.Collections.Generic.List<IPop3SaslServerFactory> factories =
            new System.Collections.Generic.List<IPop3SaslServerFactory>();
        int index = 0;
        while (index < options.SaslFactories.Length)
        {
            IPop3SaslServerFactory factory = options.SaslFactories[index];
            if (factory == null)
            {
                throw new System.ArgumentException(
                    "A POP3 SASL server factory cannot be null.",
                    "options");
            }
            if (credentialsAllowed)
            {
                factories.Add(factory);
            }
            else
            {
                string? mechanism = factory.GetMechanism();
                if (mechanism == null)
                {
                    throw new System.IO.InvalidDataException(
                        "A POP3 SASL server factory returned a null mechanism.");
                }
                if (!string.Equals(
                        mechanism,
                        "PLAIN",
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    factories.Add(factory);
                }
            }
            index++;
        }
        if (credentialsAllowed && options.AuthenticationProvider != null)
        {
            factories.Add(new Pop3PlainSaslServerFactory(
                options.AuthenticationProvider,
                cancellationToken));
        }
        return factories.ToArray();
    }

    bool IsValidApopDigest(string digest)
    {
        if (digest == null || digest.Length != 32)
        {
            return false;
        }
        int index = 0;
        while (index < digest.Length)
        {
            char value = digest[index];
            if (!((value >= '0' && value <= '9') ||
                (value >= 'a' && value <= 'f') ||
                (value >= 'A' && value <= 'F')))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    Pop3Response CreateFailureResponse(
        Pop3AuthenticationStatus status,
        bool classifyCredentials,
        bool enableResponseCodes)
    {
        Pop3Response response = new Pop3Response();
        response.Kind = Pop3ResponseKind.Negative;
        response.Text = "Authentication failed.";
        if (!enableResponseCodes)
        {
            return response;
        }
        if (status == Pop3AuthenticationStatus.LoginDelay)
        {
            response.ResponseCodeLevels = new string[]
            {
                Pop3ResponseCodes.LoginDelay
            };
        }
        else if (status == Pop3AuthenticationStatus.InUse)
        {
            response.ResponseCodeLevels = new string[]
            {
                Pop3ResponseCodes.InUse
            };
        }
        else if (status == Pop3AuthenticationStatus.TemporaryFailure)
        {
            response.ResponseCodeLevels = new string[] { "SYS", "TEMP" };
        }
        else if (status == Pop3AuthenticationStatus.PermanentFailure)
        {
            response.ResponseCodeLevels = new string[] { "SYS", "PERM" };
        }
        else if (classifyCredentials)
        {
            response.ResponseCodeLevels = new string[]
            {
                Pop3ResponseCodes.Authentication
            };
        }
        return response;
    }

    Pop3AuthenticationResult CreateFailureResult(
        Pop3AuthenticationStatus status)
    {
        Pop3AuthenticationResult result = new Pop3AuthenticationResult();
        result.Status = status;
        return result;
    }

    Pop3AuthenticationResult CopyResult(Pop3AuthenticationResult source)
    {
        Pop3AuthenticationResult copy = new Pop3AuthenticationResult();
        copy.Status = source.Status;
        copy.AuthenticatedIdentity = source.AuthenticatedIdentity;
        copy.MaildropKey = source.MaildropKey;
        copy.RetryAfterSeconds = source.RetryAfterSeconds;
        copy.FailureText = source.FailureText;
        return copy;
    }

    void ValidateResult(
        Pop3AuthenticationResult result,
        bool requireSuccessfulIdentity)
    {
        if (result == null)
        {
            throw new System.IO.InvalidDataException(
                "The POP3 authentication provider returned a null result.");
        }
        if (result.Status != Pop3AuthenticationStatus.Success &&
            result.Status != Pop3AuthenticationStatus.Rejected &&
            result.Status != Pop3AuthenticationStatus.LoginDelay &&
            result.Status != Pop3AuthenticationStatus.InUse &&
            result.Status != Pop3AuthenticationStatus.TemporaryFailure &&
            result.Status != Pop3AuthenticationStatus.PermanentFailure)
        {
            throw new System.IO.InvalidDataException(
                "The POP3 authentication provider returned an invalid status.");
        }
        if (result.RetryAfterSeconds < -1)
        {
            throw new System.IO.InvalidDataException(
                "The POP3 authentication provider returned an invalid retry delay.");
        }
        if (requireSuccessfulIdentity &&
            result.Status == Pop3AuthenticationStatus.Success &&
            (result.AuthenticatedIdentity == null || result.MaildropKey == null))
        {
            throw new System.IO.InvalidDataException(
                "Successful POP3 authentication requires identity and maildrop values.");
        }
    }

    void ValidateCommand(Pop3Command command, string expectedKeyword)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }
        if (command.Keyword == null || command.Arguments == null ||
            !string.Equals(
                command.Keyword,
                expectedKeyword,
                System.StringComparison.OrdinalIgnoreCase) ||
            command.Arguments.Length != 1 ||
            command.Arguments[0] == null ||
            command.Arguments[0].Length == 0)
        {
            throw new System.ArgumentException(
                "The authentication exchange received an invalid command.",
                "command");
        }
    }

    void ValidateApopCommand(Pop3Command command)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }
        if (command.Keyword == null || command.Arguments == null ||
            !string.Equals(
                command.Keyword,
                Pop3Commands.Apop,
                System.StringComparison.OrdinalIgnoreCase) ||
            command.Arguments.Length != 2 ||
            command.Arguments[0] == null ||
            command.Arguments[0].Length == 0 ||
            command.Arguments[1] == null ||
            command.Arguments[1].Length == 0)
        {
            throw new System.ArgumentException(
                "The authentication exchange received an invalid APOP command.",
                "command");
        }
    }

    void ValidateAuthCommand(Pop3Command command)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }
        if (command.Keyword == null || command.Arguments == null ||
            !string.Equals(
                command.Keyword,
                Pop3Commands.Auth,
                System.StringComparison.OrdinalIgnoreCase) ||
            command.Arguments.Length < 1 ||
            command.Arguments.Length > 2 ||
            command.Arguments[0] == null ||
            command.Arguments[0].Length == 0 ||
            command.Arguments.Length == 2 &&
                (command.Arguments[1] == null ||
                    command.Arguments[1].Length == 0))
        {
            throw new System.ArgumentException(
                "The authentication exchange received an invalid AUTH command.",
                "command");
        }
    }

    void ValidateInputs(
        Pop3ServerOptions options,
        Pop3ServerStateMachine stateMachine)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        if (stateMachine == null)
        {
            throw new System.ArgumentNullException("stateMachine");
        }
    }
}
