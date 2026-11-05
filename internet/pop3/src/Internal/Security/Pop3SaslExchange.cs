namespace pop3;

internal class Pop3SaslExchangeResult
{
    internal Pop3SaslStepStatus Status;
    internal string? AuthenticatedIdentity;
    internal Pop3Response? Response;
}

internal class Pop3SaslServerExchangeResult
{
    internal Pop3SaslStepStatus Status;
    internal Pop3AuthenticationResult AuthenticationResult =
        new Pop3AuthenticationResult();
    internal IPop3SaslServerSession? Session;
}

internal class Pop3SaslExchange
{
    Pop3Base64 Base64 = new Pop3Base64();
    Pop3SecretRedactor? Redactor;

    internal void SetProtocolLogger(IPop3ProtocolLogger? logger)
    {
        Redactor = new Pop3SecretRedactor(logger);
    }

    internal Pop3SaslExchangeResult AuthenticateClient(
        Pop3WireReader reader,
        Pop3WireWriter writer,
        IPop3SaslClient saslClient,
        int maximumSaslLineOctets,
        System.Threading.CancellationToken cancellationToken)
    {
        ValidateClientArguments(reader, writer, saslClient, maximumSaslLineOctets);
        cancellationToken.ThrowIfCancellationRequested();
        string mechanism = saslClient.GetMechanism();
        return AuthenticateSelectedClient(
            reader,
            writer,
            saslClient,
            mechanism,
            maximumSaslLineOctets,
            cancellationToken);
    }

    internal Pop3SaslExchangeResult AuthenticateSelectedClient(
        Pop3WireReader reader,
        Pop3WireWriter writer,
        IPop3SaslClient saslClient,
        string mechanism,
        int maximumSaslLineOctets,
        System.Threading.CancellationToken cancellationToken)
    {
        ValidateArguments(reader, writer, saslClient, mechanism, maximumSaslLineOctets);
        cancellationToken.ThrowIfCancellationRequested();

        Pop3SaslStep? suppliedInitialStep = saslClient.BeginAuthentication();
        string? initialFailure = GetStepFailure(suppliedInitialStep, true);
        if (initialFailure != null || suppliedInitialStep == null)
        {
            throw new System.IO.InvalidDataException(initialFailure);
        }
        Pop3SaslStep initialStep = suppliedInitialStep;
        if (initialStep.Status == Pop3SaslStepStatus.Cancelled ||
            initialStep.Status == Pop3SaslStepStatus.Rejected)
        {
            saslClient.CancelAuthentication();
            return CreateLocalResult(initialStep);
        }

        string? initialToken = null;
        bool deferredInitialResponse = false;
        if (initialStep.HasData)
        {
            initialToken = Base64.EncodeInitialResponse(initialStep.Data);
            deferredInitialResponse = !FitsInitialResponse(mechanism, initialToken);
        }

        Pop3Command command = new Pop3Command();
        command.Keyword = Pop3Commands.Auth;
        command.ContainsSensitiveData = initialToken != null && !deferredInitialResponse;
        if (initialToken == null || deferredInitialResponse)
        {
            command.Arguments = new string[] { mechanism };
        }
        else
        {
            command.Arguments = new string[] { mechanism, initialToken };
        }
        writer.WriteCommand(command, Pop3Defaults.MaximumCommandLineOctets, cancellationToken);
        LogCommand(command);

        Pop3Response response = reader.ReadResponse(maximumSaslLineOctets, cancellationToken);
        LogResponse(response);
        if (deferredInitialResponse)
        {
            if (response.Kind == Pop3ResponseKind.Negative)
            {
                saslClient.CancelAuthentication();
                return CreatePeerResult(response);
            }
            if (response.Kind == Pop3ResponseKind.Positive)
            {
                saslClient.CancelAuthentication();
                throw new System.IO.InvalidDataException(
                    "A POP3 server reported SASL success before receiving the deferred initial response.");
            }
            if (response.Kind != Pop3ResponseKind.SaslContinuation)
            {
                saslClient.CancelAuthentication();
                throw new System.IO.InvalidDataException(
                    "A POP3 server returned an invalid response before the deferred SASL initial response.");
            }
            if (response.Text.Length != 0)
            {
                FailActiveExchange(reader, writer, saslClient, maximumSaslLineOctets, cancellationToken);
                throw new System.IO.InvalidDataException(
                    "A POP3 server did not issue the required empty challenge for a deferred SASL initial response.");
            }

            string deferredPayload = Base64.Encode(initialStep.Data);
            if (!PayloadFits(deferredPayload, maximumSaslLineOctets))
            {
                FailActiveExchange(reader, writer, saslClient, maximumSaslLineOctets, cancellationToken);
                throw new System.IO.InvalidDataException(
                    "A deferred POP3 SASL initial response exceeds its configured line limit.");
            }
            WritePayload(writer, deferredPayload, maximumSaslLineOctets, cancellationToken);
            response = reader.ReadResponse(maximumSaslLineOctets, cancellationToken);
            LogResponse(response);
        }

        Pop3SaslStep currentStep = initialStep;
        bool complete = false;
        Pop3SaslExchangeResult result = new Pop3SaslExchangeResult();
        while (!complete)
        {
            if (response.Kind == Pop3ResponseKind.Negative)
            {
                saslClient.CancelAuthentication();
                result = CreatePeerResult(response);
                complete = true;
            }
            else if (response.Kind == Pop3ResponseKind.Positive)
            {
                if (currentStep.Status != Pop3SaslStepStatus.Success)
                {
                    saslClient.CancelAuthentication();
                    throw new System.IO.InvalidDataException(
                        "A POP3 server reported SASL success before the client mechanism completed.");
                }
                result.Status = Pop3SaslStepStatus.Success;
                result.AuthenticatedIdentity = currentStep.AuthenticatedIdentity;
                result.Response = response;
                complete = true;
            }
            else if (response.Kind == Pop3ResponseKind.SaslContinuation)
            {
                if (currentStep.Status == Pop3SaslStepStatus.Success)
                {
                    FailActiveExchange(reader, writer, saslClient, maximumSaslLineOctets, cancellationToken);
                    throw new System.IO.InvalidDataException(
                        "POP3 cannot carry additional SASL data after the client mechanism completes.");
                }
                if (!Base64.IsValid(response.Text))
                {
                    FailActiveExchange(reader, writer, saslClient, maximumSaslLineOctets, cancellationToken);
                    throw new System.IO.InvalidDataException("A POP3 server sent a malformed SASL challenge.");
                }

                byte[] challenge = Base64.Decode(response.Text);
                Pop3SaslStep? suppliedNextStep = saslClient.ContinueAuthentication(challenge);
                string? nextFailure = GetStepFailure(suppliedNextStep, false);
                if (nextFailure != null || suppliedNextStep == null)
                {
                    FailActiveExchange(reader, writer, saslClient, maximumSaslLineOctets, cancellationToken);
                    throw new System.IO.InvalidDataException(nextFailure);
                }
                Pop3SaslStep nextStep = suppliedNextStep;
                if (nextStep.Status == Pop3SaslStepStatus.Cancelled ||
                    nextStep.Status == Pop3SaslStepStatus.Rejected)
                {
                    Pop3Response cancellationResponse = FailActiveExchange(
                        reader,
                        writer,
                        saslClient,
                        maximumSaslLineOctets,
                        cancellationToken);
                    result.Status = nextStep.Status;
                    result.Response = cancellationResponse;
                    complete = true;
                }
                else
                {
                    string payload = Base64.Encode(nextStep.Data);
                    if (!PayloadFits(payload, maximumSaslLineOctets))
                    {
                        FailActiveExchange(reader, writer, saslClient, maximumSaslLineOctets, cancellationToken);
                        throw new System.IO.InvalidDataException(
                            "A POP3 SASL response exceeds its configured line limit.");
                    }
                    WritePayload(
                        writer,
                        payload,
                        maximumSaslLineOctets,
                        cancellationToken);
                    currentStep = nextStep;
                    response = reader.ReadResponse(maximumSaslLineOctets, cancellationToken);
                    LogResponse(response);
                }
            }
            else
            {
                saslClient.CancelAuthentication();
                throw new System.IO.InvalidDataException("A POP3 server returned an invalid SASL response status.");
            }
        }
        return result;
    }

    internal Pop3SaslServerExchangeResult AuthenticateServer(
        Pop3WireReader reader,
        Pop3WireWriter writer,
        Pop3Command command,
        IPop3SaslServerFactory[] factories,
        Pop3ServerConnection connection,
        int maximumSaslLineOctets,
        System.Threading.CancellationToken cancellationToken)
    {
        ValidateServerArguments(
            reader,
            writer,
            command,
            factories,
            connection,
            maximumSaslLineOctets);
        cancellationToken.ThrowIfCancellationRequested();
        string mechanism = command.Arguments[0].ToUpperInvariant();
        ValidateMechanism(mechanism);
        IPop3SaslServerFactory? selectedFactory = FindServerFactory(
            factories,
            mechanism);
        if (selectedFactory == null)
        {
            return CreateServerFailureResult(Pop3SaslStepStatus.Rejected);
        }

        bool hasInitialResponse = command.Arguments.Length == 2;
        byte[] initialResponse = new byte[0];
        if (hasInitialResponse)
        {
            string initialToken = command.Arguments[1];
            if (Base64.IsCancellationMarker(initialToken) ||
                initialToken != Pop3Base64.EmptyInitialResponseMarker &&
                !Base64.IsValid(initialToken))
            {
                return CreateServerFailureResult(Pop3SaslStepStatus.Rejected);
            }
            initialResponse = Base64.DecodeInitialResponse(initialToken);
        }

        IPop3SaslServerSession? suppliedSession =
            selectedFactory.CreateSession(connection);
        if (suppliedSession == null)
        {
            throw new System.IO.InvalidDataException(
                "A POP3 SASL server factory returned a null session.");
        }
        IPop3SaslServerSession session = suppliedSession;
        Pop3SaslStep? suppliedStep = session.BeginAuthentication(
            initialResponse,
            hasInitialResponse);
        Pop3SaslStep step = RequireServerStep(session, suppliedStep);
        return ContinueServerExchange(
            reader,
            writer,
            session,
            step,
            maximumSaslLineOctets,
            cancellationToken);
    }

    Pop3SaslServerExchangeResult ContinueServerExchange(
        Pop3WireReader reader,
        Pop3WireWriter writer,
        IPop3SaslServerSession session,
        Pop3SaslStep initialStep,
        int maximumSaslLineOctets,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3SaslStep step = initialStep;
        while (step.Status == Pop3SaslStepStatus.Continue)
        {
            WriteServerChallenge(
                writer,
                session,
                step.Data,
                maximumSaslLineOctets,
                cancellationToken);
            byte[]? line = reader.LineReader.ReadDataLine(
                maximumSaslLineOctets,
                cancellationToken);
            if (line == null)
            {
                session.CancelAuthentication();
                throw new System.IO.EndOfStreamException(
                    "The POP3 stream ended during a SASL exchange.");
            }
            LogServerPayload(line.Length + 2);
            string token = System.Text.Encoding.ASCII.GetString(line);
            if (Base64.IsCancellationMarker(token))
            {
                session.CancelAuthentication();
                return CreateServerFailureResult(
                    Pop3SaslStepStatus.Cancelled);
            }
            if (!Base64.IsValid(token))
            {
                session.CancelAuthentication();
                return CreateServerFailureResult(
                    Pop3SaslStepStatus.Rejected);
            }

            byte[] response = Base64.Decode(token);
            Pop3SaslStep? suppliedStep =
                session.ContinueAuthentication(response);
            step = RequireServerStep(session, suppliedStep);
        }
        return CompleteServerExchange(session, step);
    }

    Pop3SaslServerExchangeResult CompleteServerExchange(
        IPop3SaslServerSession session,
        Pop3SaslStep step)
    {
        if (step.Status == Pop3SaslStepStatus.Cancelled)
        {
            session.CancelAuthentication();
            return CreateServerFailureResult(Pop3SaslStepStatus.Cancelled);
        }

        Pop3AuthenticationResult? suppliedResult =
            session.GetAuthenticationResult();
        if (suppliedResult == null)
        {
            session.CancelAuthentication();
            throw new System.IO.InvalidDataException(
                "A POP3 SASL server session returned a null authentication result.");
        }
        Pop3SaslServerExchangeResult result =
            new Pop3SaslServerExchangeResult();
        result.Status = step.Status;
        result.AuthenticationResult = suppliedResult;
        if (step.Status == Pop3SaslStepStatus.Success)
        {
            result.Session = session;
        }
        else
        {
            session.CancelAuthentication();
        }
        return result;
    }

    Pop3SaslStep RequireServerStep(
        IPop3SaslServerSession session,
        Pop3SaslStep? suppliedStep)
    {
        string? failure = GetServerStepFailure(suppliedStep);
        if (failure != null || suppliedStep == null)
        {
            session.CancelAuthentication();
            throw new System.IO.InvalidDataException(failure);
        }
        return suppliedStep;
    }

    string? GetServerStepFailure(Pop3SaslStep? step)
    {
        if (step == null)
        {
            return "A POP3 SASL server session returned a null exchange step.";
        }
        if (step.Data == null)
        {
            return "A POP3 SASL server session returned null exchange data.";
        }
        if (step.Status != Pop3SaslStepStatus.Continue &&
            step.Status != Pop3SaslStepStatus.Success &&
            step.Status != Pop3SaslStepStatus.Rejected &&
            step.Status != Pop3SaslStepStatus.Cancelled)
        {
            return "A POP3 SASL server session returned an invalid exchange status.";
        }
        if (step.Status == Pop3SaslStepStatus.Continue && !step.HasData)
        {
            return "A continuing POP3 SASL server step must provide a challenge.";
        }
        if (step.Status != Pop3SaslStepStatus.Continue &&
            (step.HasData || step.Data.Length != 0))
        {
            return "A completed POP3 SASL server step cannot provide additional data.";
        }
        if (!step.HasData && step.Data.Length != 0)
        {
            return "A POP3 SASL server session returned unmarked exchange data.";
        }
        return null;
    }

    void WriteServerChallenge(
        Pop3WireWriter writer,
        IPop3SaslServerSession session,
        byte[] challenge,
        int maximumSaslLineOctets,
        System.Threading.CancellationToken cancellationToken)
    {
        string encoded = Base64.Encode(challenge);
        if (encoded.Length > maximumSaslLineOctets - 4)
        {
            session.CancelAuthentication();
            throw new System.IO.InvalidDataException(
                "A POP3 SASL challenge exceeds its configured line limit.");
        }
        Pop3Response response = new Pop3Response();
        response.Kind = Pop3ResponseKind.SaslContinuation;
        response.Text = encoded;
        writer.WriteResponse(
            response,
            maximumSaslLineOctets,
            cancellationToken);
        LogResponse(response);
    }

    void LogServerPayload(int byteCount)
    {
        if (Redactor == null)
        {
            return;
        }
        Redactor.LogSaslPayload(
            Pop3LogDirection.ClientToServer,
            byteCount,
            Pop3SessionState.Authorization);
    }

    IPop3SaslServerFactory? FindServerFactory(
        IPop3SaslServerFactory[] factories,
        string mechanism)
    {
        IPop3SaslServerFactory? selectedFactory = null;
        int index = 0;
        while (index < factories.Length)
        {
            IPop3SaslServerFactory factory = factories[index];
            string? suppliedMechanism = factory.GetMechanism();
            if (suppliedMechanism == null)
            {
                throw new System.IO.InvalidDataException(
                    "A POP3 SASL server factory returned a null mechanism.");
            }
            string candidate = suppliedMechanism.ToUpperInvariant();
            ValidateMechanism(candidate);
            if (string.Equals(
                candidate,
                mechanism,
                System.StringComparison.Ordinal) &&
                selectedFactory == null)
            {
                selectedFactory = factory;
            }
            index++;
        }
        return selectedFactory;
    }

    Pop3SaslServerExchangeResult CreateServerFailureResult(
        Pop3SaslStepStatus status)
    {
        Pop3AuthenticationResult authentication =
            new Pop3AuthenticationResult();
        authentication.Status = Pop3AuthenticationStatus.Rejected;
        Pop3SaslServerExchangeResult result =
            new Pop3SaslServerExchangeResult();
        result.Status = status;
        result.AuthenticationResult = authentication;
        return result;
    }

    void ValidateServerArguments(
        Pop3WireReader reader,
        Pop3WireWriter writer,
        Pop3Command command,
        IPop3SaslServerFactory[] factories,
        Pop3ServerConnection connection,
        int maximumSaslLineOctets)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }
        if (factories == null)
        {
            throw new System.ArgumentNullException("factories");
        }
        if (connection == null)
        {
            throw new System.ArgumentNullException("connection");
        }
        if (maximumSaslLineOctets < 4)
        {
            throw new System.ArgumentOutOfRangeException(
                "maximumSaslLineOctets");
        }
        int index = 0;
        while (index < factories.Length)
        {
            if (factories[index] == null)
            {
                throw new System.ArgumentException(
                    "A POP3 SASL server factory cannot be null.",
                    "factories");
            }
            index++;
        }
    }

    void ValidateArguments(
        Pop3WireReader reader,
        Pop3WireWriter writer,
        IPop3SaslClient saslClient,
        string mechanism,
        int maximumSaslLineOctets)
    {
        ValidateClientArguments(reader, writer, saslClient, maximumSaslLineOctets);
        ValidateMechanism(mechanism);
    }

    void ValidateClientArguments(
        Pop3WireReader reader,
        Pop3WireWriter writer,
        IPop3SaslClient saslClient,
        int maximumSaslLineOctets)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }
        if (saslClient == null)
        {
            throw new System.ArgumentNullException("saslClient");
        }
        if (maximumSaslLineOctets < 4)
        {
            throw new System.ArgumentOutOfRangeException("maximumSaslLineOctets");
        }
    }

    void ValidateMechanism(string mechanism)
    {
        if (mechanism == null)
        {
            throw new System.IO.InvalidDataException("A POP3 SASL mechanism cannot be null.");
        }
        if (mechanism.Length < 1 || mechanism.Length > 20)
        {
            throw new System.IO.InvalidDataException(
                "A POP3 SASL mechanism must contain between one and twenty characters.");
        }

        int index = 0;
        while (index < mechanism.Length)
        {
            char character = mechanism[index];
            if (!((character >= 'A' && character <= 'Z') ||
                (character >= '0' && character <= '9') ||
                character == '-' || character == '_'))
            {
                throw new System.IO.InvalidDataException(
                    "A POP3 SASL mechanism contains an invalid character.");
            }
            index++;
        }
    }

    string? GetStepFailure(Pop3SaslStep? step, bool isInitial)
    {
        if (step == null)
        {
            return "A POP3 SASL client returned a null exchange step.";
        }
        if (step.Data == null)
        {
            return "A POP3 SASL client returned null exchange data.";
        }
        if (!step.HasData && step.Data.Length != 0)
        {
            return "A POP3 SASL client returned unmarked exchange data.";
        }
        if (step.Status != Pop3SaslStepStatus.Continue &&
            step.Status != Pop3SaslStepStatus.Success &&
            step.Status != Pop3SaslStepStatus.Rejected &&
            step.Status != Pop3SaslStepStatus.Cancelled)
        {
            return "A POP3 SASL client returned an invalid exchange status.";
        }
        if (!isInitial &&
            (step.Status == Pop3SaslStepStatus.Continue || step.Status == Pop3SaslStepStatus.Success) &&
            !step.HasData)
        {
            return "A POP3 SASL client did not provide a challenge response.";
        }
        if (isInitial && step.Status == Pop3SaslStepStatus.Success && !step.HasData)
        {
            return "A completed POP3 SASL initial step must provide a response.";
        }
        return null;
    }

    bool FitsInitialResponse(string mechanism, string initialToken)
    {
        int contentOctets = Pop3Commands.Auth.Length + 1 + mechanism.Length + 1 + initialToken.Length;
        return contentOctets <= Pop3Defaults.MaximumCommandLineOctets - 2;
    }

    bool PayloadFits(string payload, int maximumSaslLineOctets)
    {
        return payload.Length <= maximumSaslLineOctets - 2;
    }

    void WritePayload(
        Pop3WireWriter writer,
        string payload,
        int maximumSaslLineOctets,
        System.Threading.CancellationToken cancellationToken)
    {
        byte[] line = System.Text.Encoding.ASCII.GetBytes(payload);
        writer.LineWriter.WriteDataLine(line, maximumSaslLineOctets, cancellationToken);
        if (Redactor != null)
        {
            Redactor.LogSaslPayload(
                Pop3LogDirection.ClientToServer,
                line.Length + 2,
                Pop3SessionState.Authorization);
        }
    }

    Pop3Response FailActiveExchange(
        Pop3WireReader reader,
        Pop3WireWriter writer,
        IPop3SaslClient saslClient,
        int maximumSaslLineOctets,
        System.Threading.CancellationToken cancellationToken)
    {
        WritePayload(
            writer,
            Pop3Base64.CancellationMarker,
            maximumSaslLineOctets,
            cancellationToken);
        Pop3Response response = reader.ReadResponse(maximumSaslLineOctets, cancellationToken);
        LogResponse(response);
        saslClient.CancelAuthentication();
        if (response.Kind != Pop3ResponseKind.Negative)
        {
            throw new System.IO.InvalidDataException("A POP3 server did not reject a cancelled SASL exchange.");
        }
        return response;
    }

    Pop3SaslExchangeResult CreatePeerResult(Pop3Response response)
    {
        Pop3SaslExchangeResult result = new Pop3SaslExchangeResult();
        result.Status = Pop3SaslStepStatus.Rejected;
        result.Response = response;
        return result;
    }

    Pop3SaslExchangeResult CreateLocalResult(Pop3SaslStep step)
    {
        Pop3SaslExchangeResult result = new Pop3SaslExchangeResult();
        result.Status = step.Status;
        result.AuthenticatedIdentity = step.AuthenticatedIdentity;
        return result;
    }

    void LogCommand(Pop3Command command)
    {
        if (Redactor == null)
        {
            return;
        }
        Pop3CommandParser parser = new Pop3CommandParser();
        byte[] line = parser.FormatCommand(command);
        Redactor.LogCommand(
            Pop3LogDirection.ClientToServer,
            command,
            line.Length + 2,
            Pop3SessionState.Authorization);
    }

    void LogResponse(Pop3Response response)
    {
        if (Redactor == null)
        {
            return;
        }
        Pop3ResponseParser parser = new Pop3ResponseParser();
        byte[] line = parser.FormatResponse(response, int.MaxValue);
        Redactor.LogResponse(
            Pop3LogDirection.ServerToClient,
            response,
            line.Length + 2,
            Pop3SessionState.Authorization);
    }
}
