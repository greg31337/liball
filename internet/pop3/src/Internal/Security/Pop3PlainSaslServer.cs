namespace pop3;

internal class Pop3PlainSaslServerFactory : IPop3SaslServerFactory
{
    IPop3AuthenticationProvider Provider;
    System.Threading.CancellationToken CancellationToken;

    internal Pop3PlainSaslServerFactory(
        IPop3AuthenticationProvider provider,
        System.Threading.CancellationToken cancellationToken)
    {
        if (provider == null)
        {
            throw new System.ArgumentNullException("provider");
        }
        Provider = provider;
        CancellationToken = cancellationToken;
    }

    public string GetMechanism()
    {
        return "PLAIN";
    }

    public IPop3SaslServerSession CreateSession(
        Pop3ServerConnection connection)
    {
        return new Pop3PlainSaslServer(
            Provider,
            connection,
            CancellationToken);
    }
}

internal class Pop3PlainSaslServer : IPop3SaslServerSession
{
    IPop3AuthenticationProvider Provider;
    Pop3ServerConnection Connection;
    System.Threading.CancellationToken CancellationToken;
    Pop3AuthenticationResult AuthenticationResult;
    bool Started;
    bool WaitingForResponse;
    bool Completed;

    internal Pop3PlainSaslServer(
        IPop3AuthenticationProvider provider,
        Pop3ServerConnection connection,
        System.Threading.CancellationToken cancellationToken)
    {
        if (provider == null)
        {
            throw new System.ArgumentNullException("provider");
        }
        if (connection == null)
        {
            throw new System.ArgumentNullException("connection");
        }
        Provider = provider;
        Connection = connection;
        CancellationToken = cancellationToken;
        AuthenticationResult = CreateRejectedResult();
    }

    public Pop3SaslStep BeginAuthentication(
        byte[] initialResponse,
        bool hasInitialResponse)
    {
        if (initialResponse == null)
        {
            throw new System.ArgumentNullException("initialResponse");
        }
        if (Started)
        {
            throw new System.InvalidOperationException(
                "A SASL PLAIN server exchange can only be started once.");
        }
        if (!hasInitialResponse && initialResponse.Length != 0)
        {
            throw new System.ArgumentException(
                "An absent SASL PLAIN initial response must be empty.",
                "initialResponse");
        }
        CancellationToken.ThrowIfCancellationRequested();
        Started = true;
        if (!hasInitialResponse)
        {
            WaitingForResponse = true;
            Pop3SaslStep challenge = new Pop3SaslStep();
            challenge.Status = Pop3SaslStepStatus.Continue;
            challenge.HasData = true;
            return challenge;
        }
        return AuthenticateResponse(initialResponse);
    }

    public Pop3SaslStep ContinueAuthentication(byte[] response)
    {
        if (response == null)
        {
            throw new System.ArgumentNullException("response");
        }
        if (!Started || !WaitingForResponse || Completed)
        {
            throw new System.InvalidOperationException(
                "A SASL PLAIN response is not expected.");
        }
        WaitingForResponse = false;
        return AuthenticateResponse(response);
    }

    public void CancelAuthentication()
    {
        WaitingForResponse = false;
        Completed = true;
        AuthenticationResult = CreateRejectedResult();
    }

    public Pop3AuthenticationResult GetAuthenticationResult()
    {
        if (!Completed)
        {
            throw new System.InvalidOperationException(
                "The SASL PLAIN authentication result is not available yet.");
        }
        return CopyResult(AuthenticationResult);
    }

    public System.IO.Stream ApplySecurityLayer(System.IO.Stream stream)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }
        return stream;
    }

    Pop3SaslStep AuthenticateResponse(byte[] response)
    {
        Pop3Credentials? credentials = DecodeCredentials(response);
        System.Array.Clear(response, 0, response.Length);
        CancellationToken.ThrowIfCancellationRequested();
        if (credentials == null)
        {
            AuthenticationResult = CreateRejectedResult();
            Completed = true;
            return CreateCompletionStep(Pop3SaslStepStatus.Rejected);
        }

        Pop3AuthenticationResult delay = Provider.CheckLoginDelay(
            credentials.Username,
            Connection,
            CancellationToken);
        ValidateProviderResult(delay, false);
        if (delay.Status != Pop3AuthenticationStatus.Success)
        {
            AuthenticationResult = CopyResult(delay);
            Completed = true;
            return CreateCompletionStep(Pop3SaslStepStatus.Rejected);
        }

        Pop3AuthenticationResult result = Provider.AuthenticateSaslPlain(
            credentials,
            Connection,
            CancellationToken);
        ValidateProviderResult(result, true);
        AuthenticationResult = CopyResult(result);
        Completed = true;
        if (result.Status == Pop3AuthenticationStatus.Success)
        {
            return CreateCompletionStep(Pop3SaslStepStatus.Success);
        }
        return CreateCompletionStep(Pop3SaslStepStatus.Rejected);
    }

    Pop3Credentials? DecodeCredentials(byte[] response)
    {
        int firstSeparator = -1;
        int secondSeparator = -1;
        int index = 0;
        while (index < response.Length)
        {
            if (response[index] == 0)
            {
                if (firstSeparator < 0)
                {
                    firstSeparator = index;
                }
                else if (secondSeparator < 0)
                {
                    secondSeparator = index;
                }
                else
                {
                    return null;
                }
            }
            index++;
        }
        if (firstSeparator < 0 ||
            secondSeparator <= firstSeparator + 1 ||
            secondSeparator >= response.Length - 1)
        {
            return null;
        }

        Pop3Utf8Validator validator = new Pop3Utf8Validator();
        if (!validator.IsValidUtf8(
                new System.ReadOnlySpan<byte>(
                    response,
                    0,
                    firstSeparator)) ||
            !validator.IsValidUtf8(
                new System.ReadOnlySpan<byte>(
                    response,
                    firstSeparator + 1,
                    secondSeparator - firstSeparator - 1)) ||
            !validator.IsValidUtf8(
                new System.ReadOnlySpan<byte>(
                    response,
                    secondSeparator + 1,
                    response.Length - secondSeparator - 1)))
        {
            return null;
        }

        System.Text.UTF8Encoding encoding =
            new System.Text.UTF8Encoding(false, true);
        Pop3Credentials credentials = new Pop3Credentials();
        if (firstSeparator > 0)
        {
            credentials.AuthorizationIdentity = encoding.GetString(
                response,
                0,
                firstSeparator);
        }
        credentials.Username = encoding.GetString(
            response,
            firstSeparator + 1,
            secondSeparator - firstSeparator - 1);
        credentials.Password = encoding.GetString(
            response,
            secondSeparator + 1,
            response.Length - secondSeparator - 1);
        return credentials;
    }

    Pop3SaslStep CreateCompletionStep(Pop3SaslStepStatus status)
    {
        Pop3SaslStep step = new Pop3SaslStep();
        step.Status = status;
        return step;
    }

    Pop3AuthenticationResult CreateRejectedResult()
    {
        Pop3AuthenticationResult result = new Pop3AuthenticationResult();
        result.Status = Pop3AuthenticationStatus.Rejected;
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

    void ValidateProviderResult(
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
}
