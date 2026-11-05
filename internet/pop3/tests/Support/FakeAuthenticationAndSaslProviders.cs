namespace pop3.Tests;

internal sealed class FakeAuthenticationProvider : IPop3AuthenticationProvider
{
    internal Pop3AuthenticationResult LoginDelayResult = SuccessfulResult();
    internal Pop3AuthenticationResult UserPasswordResult = SuccessfulResult();
    internal Pop3AuthenticationResult ApopResult = SuccessfulResult();
    internal Pop3AuthenticationResult SaslPlainResult = SuccessfulResult();
    internal Func<string, Pop3ServerConnection, CancellationToken, Pop3AuthenticationResult>? LoginDelayHandler = null;
    internal Func<Pop3Credentials, Pop3ServerConnection, CancellationToken, Pop3AuthenticationResult>? UserPasswordHandler = null;
    internal Func<Pop3ApopRequest, Pop3ServerConnection, CancellationToken, Pop3AuthenticationResult>? ApopHandler = null;
    internal Func<Pop3Credentials, Pop3ServerConnection, CancellationToken, Pop3AuthenticationResult>? SaslPlainHandler = null;
    internal readonly List<string> Calls = new List<string>();
    internal int LoginDelayCallCount;
    internal int UserPasswordCallCount;
    internal int ApopCallCount;
    internal int SaslPlainCallCount;
    internal string? LastLoginDelayUsername;
    internal Pop3Credentials? LastUserPasswordCredentials;
    internal Pop3ApopRequest? LastApopRequest;
    internal Pop3Credentials? LastSaslPlainCredentials;
    internal Pop3ServerConnection? LastConnection;
    internal CancellationToken LastCancellationToken;

    public Pop3AuthenticationResult CheckLoginDelay(
        string username,
        Pop3ServerConnection connection,
        CancellationToken cancellationToken)
    {
        LoginDelayCallCount++;
        Calls.Add("login-delay");
        LastLoginDelayUsername = username;
        RecordCommon(connection, cancellationToken);
        if (LoginDelayHandler != null)
        {
            return LoginDelayHandler(username, connection, cancellationToken);
        }
        return LoginDelayResult;
    }

    public Pop3AuthenticationResult AuthenticateUserPassword(
        Pop3Credentials credentials,
        Pop3ServerConnection connection,
        CancellationToken cancellationToken)
    {
        UserPasswordCallCount++;
        Calls.Add("user-password");
        LastUserPasswordCredentials = credentials;
        RecordCommon(connection, cancellationToken);
        if (UserPasswordHandler != null)
        {
            return UserPasswordHandler(credentials, connection, cancellationToken);
        }
        return UserPasswordResult;
    }

    public Pop3AuthenticationResult AuthenticateApop(
        Pop3ApopRequest request,
        Pop3ServerConnection connection,
        CancellationToken cancellationToken)
    {
        ApopCallCount++;
        Calls.Add("apop");
        LastApopRequest = request;
        RecordCommon(connection, cancellationToken);
        if (ApopHandler != null)
        {
            return ApopHandler(request, connection, cancellationToken);
        }
        return ApopResult;
    }

    public Pop3AuthenticationResult AuthenticateSaslPlain(
        Pop3Credentials credentials,
        Pop3ServerConnection connection,
        CancellationToken cancellationToken)
    {
        SaslPlainCallCount++;
        Calls.Add("sasl-plain");
        LastSaslPlainCredentials = credentials;
        RecordCommon(connection, cancellationToken);
        if (SaslPlainHandler != null)
        {
            return SaslPlainHandler(credentials, connection, cancellationToken);
        }
        return SaslPlainResult;
    }

    void RecordCommon(
        Pop3ServerConnection connection,
        CancellationToken cancellationToken)
    {
        LastConnection = connection;
        LastCancellationToken = cancellationToken;
    }

    static Pop3AuthenticationResult SuccessfulResult()
    {
        Pop3AuthenticationResult result = new Pop3AuthenticationResult();
        result.Status = Pop3AuthenticationStatus.Success;
        result.AuthenticatedIdentity = "test-user";
        result.MaildropKey = "test-maildrop";
        return result;
    }
}

internal sealed class FakeSaslClient : IPop3SaslClient
{
    internal string Mechanism = "TEST";
    internal Pop3SaslStep BeginStep = SuccessfulStep();
    internal readonly Queue<Pop3SaslStep> ContinueSteps = new Queue<Pop3SaslStep>();
    internal Func<Stream, Stream>? SecurityLayerHandler = null;
    internal Stream? SecurityLayerStream = null;
    internal readonly List<byte[]> Challenges = new List<byte[]>();
    internal readonly List<string> Calls = new List<string>();
    internal int MechanismCallCount;
    internal int BeginCallCount;
    internal int ContinueCallCount;
    internal int CancelCallCount;
    internal int ApplySecurityLayerCallCount;
    internal Stream? SuppliedSecurityLayerStream;
    internal Stream? ReturnedSecurityLayerStream;

    public string GetMechanism()
    {
        MechanismCallCount++;
        Calls.Add("mechanism");
        return Mechanism;
    }

    public Pop3SaslStep BeginAuthentication()
    {
        BeginCallCount++;
        Calls.Add("begin");
        return BeginStep;
    }

    public Pop3SaslStep ContinueAuthentication(byte[] challenge)
    {
        ContinueCallCount++;
        Calls.Add("continue");
        Challenges.Add(challenge.ToArray());
        if (ContinueSteps.Count == 0)
        {
            throw new InvalidOperationException("No SASL client continuation step was configured.");
        }
        return ContinueSteps.Dequeue();
    }

    public void CancelAuthentication()
    {
        CancelCallCount++;
        Calls.Add("cancel");
    }

    public Stream ApplySecurityLayer(Stream stream)
    {
        ApplySecurityLayerCallCount++;
        Calls.Add("apply-security-layer");
        SuppliedSecurityLayerStream = stream;
        if (SecurityLayerHandler != null)
        {
            ReturnedSecurityLayerStream = SecurityLayerHandler(stream);
        }
        else
        {
            ReturnedSecurityLayerStream = SecurityLayerStream ?? stream;
        }
        return ReturnedSecurityLayerStream;
    }

    static Pop3SaslStep SuccessfulStep()
    {
        Pop3SaslStep step = new Pop3SaslStep();
        step.Status = Pop3SaslStepStatus.Success;
        return step;
    }
}

internal sealed class FakeSaslServerFactory : IPop3SaslServerFactory
{
    internal string Mechanism = "TEST";
    internal IPop3SaslServerSession Session;
    internal Func<Pop3ServerConnection, IPop3SaslServerSession>? SessionFactory = null;
    internal readonly List<Pop3ServerConnection> Connections = new List<Pop3ServerConnection>();
    internal int MechanismCallCount;
    internal int CreateSessionCallCount;

    internal FakeSaslServerFactory()
    {
        Session = new FakeSaslServerSession();
    }

    internal FakeSaslServerFactory(IPop3SaslServerSession session)
    {
        Session = session;
    }

    public string GetMechanism()
    {
        MechanismCallCount++;
        return Mechanism;
    }

    public IPop3SaslServerSession CreateSession(Pop3ServerConnection connection)
    {
        CreateSessionCallCount++;
        Connections.Add(connection);
        if (SessionFactory != null)
        {
            return SessionFactory(connection);
        }
        return Session;
    }
}

internal sealed class FakeSaslServerSession : IPop3SaslServerSession
{
    internal Pop3SaslStep BeginStep = SuccessfulStep();
    internal readonly Queue<Pop3SaslStep> ContinueSteps = new Queue<Pop3SaslStep>();
    internal Pop3AuthenticationResult AuthenticationResult = SuccessfulResult();
    internal Func<Stream, Stream>? SecurityLayerHandler = null;
    internal Stream? SecurityLayerStream = null;
    internal readonly List<byte[]> Responses = new List<byte[]>();
    internal readonly List<string> Calls = new List<string>();
    internal int BeginCallCount;
    internal int ContinueCallCount;
    internal int CancelCallCount;
    internal int AuthenticationResultCallCount;
    internal int ApplySecurityLayerCallCount;
    internal byte[] LastInitialResponse = Array.Empty<byte>();
    internal bool LastHasInitialResponse;
    internal Stream? SuppliedSecurityLayerStream;
    internal Stream? ReturnedSecurityLayerStream;

    public Pop3SaslStep BeginAuthentication(
        byte[] initialResponse,
        bool hasInitialResponse)
    {
        BeginCallCount++;
        Calls.Add("begin");
        LastInitialResponse = initialResponse.ToArray();
        LastHasInitialResponse = hasInitialResponse;
        return BeginStep;
    }

    public Pop3SaslStep ContinueAuthentication(byte[] response)
    {
        ContinueCallCount++;
        Calls.Add("continue");
        Responses.Add(response.ToArray());
        if (ContinueSteps.Count == 0)
        {
            throw new InvalidOperationException("No SASL server continuation step was configured.");
        }
        return ContinueSteps.Dequeue();
    }

    public void CancelAuthentication()
    {
        CancelCallCount++;
        Calls.Add("cancel");
    }

    public Pop3AuthenticationResult GetAuthenticationResult()
    {
        AuthenticationResultCallCount++;
        Calls.Add("authentication-result");
        return AuthenticationResult;
    }

    public Stream ApplySecurityLayer(Stream stream)
    {
        ApplySecurityLayerCallCount++;
        Calls.Add("apply-security-layer");
        SuppliedSecurityLayerStream = stream;
        if (SecurityLayerHandler != null)
        {
            ReturnedSecurityLayerStream = SecurityLayerHandler(stream);
        }
        else
        {
            ReturnedSecurityLayerStream = SecurityLayerStream ?? stream;
        }
        return ReturnedSecurityLayerStream;
    }

    static Pop3SaslStep SuccessfulStep()
    {
        Pop3SaslStep step = new Pop3SaslStep();
        step.Status = Pop3SaslStepStatus.Success;
        return step;
    }

    static Pop3AuthenticationResult SuccessfulResult()
    {
        Pop3AuthenticationResult result = new Pop3AuthenticationResult();
        result.Status = Pop3AuthenticationStatus.Success;
        result.AuthenticatedIdentity = "test-user";
        result.MaildropKey = "test-maildrop";
        return result;
    }
}
