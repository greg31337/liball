namespace sip.Tests;

public class SipBearerAuthenticationTests
{
    [Test]
    public void ParsesAndFormatsRfc8898ChallengeMetadata()
    {
        string text = "WWW-Authenticate: Bearer realm=\"sip.example\", "
            + "scope=\"sip:register sip:call\", "
            + "authz_server=\"https://auth.example/oauth\", "
            + "error=\"invalid_scope\", audience=\"sip service\"";
        SipHeaderCodec codec = new SipHeaderCodec();

        SipHeaderParseResult parsed = codec.ParseHeader(
            System.Text.Encoding.ASCII.GetBytes(text + "\r\n"),
            new SipParseOptions());
        SipWriteResult formatted = codec.FormatHeader(
            parsed.Header!, new SipWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(parsed.BearerToken, Is.Not.Null);
            Assert.That(parsed.BearerToken!.Realm, Is.EqualTo("sip.example"));
            Assert.That(parsed.BearerToken.AuthorizationServer,
                Is.EqualTo("https://auth.example/oauth"));
            Assert.That(parsed.BearerToken.Scopes,
                Is.EqualTo(new string[] { "sip:register", "sip:call" }));
            Assert.That(parsed.BearerToken.Error, Is.EqualTo("invalid_scope"));
            Assert.That(parsed.BearerToken.Parameters, Has.Length.EqualTo(1));
            Assert.That(parsed.BearerToken.Parameters[0].Name,
                Is.EqualTo("audience"));
            Assert.That(System.Text.Encoding.ASCII.GetString(formatted.Bytes),
                Is.EqualTo(text + "\r\n"));
        });
    }

    [TestCase("WWW-Authenticate: Bearer realm=\"sip.example\"")]
    [TestCase("WWW-Authenticate: Bearer authz_server=\"http://auth.example\"")]
    [TestCase("WWW-Authenticate: Bearer scope=\"one  two\", authz_server=\"https://auth.example\"")]
    [TestCase("WWW-Authenticate: Bearer authz_server=\"https://auth.example\", error=\"other\"")]
    public void RejectsMalformedRecognizedBearerChallenges(string text)
    {
        SipHeaderParseResult parsed = new SipHeaderCodec().ParseHeader(
            System.Text.Encoding.ASCII.GetBytes(text + "\r\n"),
            new SipParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(parsed.BearerToken, Is.Null);
        });
    }

    [Test]
    public void ClientAddsExternallyIssuedBearerTokenWithoutIssuingCredentials()
    {
        BearerCredentialProvider provider = new BearerCredentialProvider();
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipAuthenticationRequest request = CreateRequest(
            CreateMessage(""), SipCredentialScope.Origin);
        request.BearerToken = CreateBearerPolicy();
        request.BearerToken.Token = "abc.DEF-123_~/+==";
        request.BearerToken.Subject = "alice";

        SipAuthenticationResult result = client.AuthorizeRequest(request);
        SipHeader? header = FindHeader(result.Message!, SipHeaderNames.Authorization);
        SipHeaderParseResult parsed = new SipHeaderCodec().ParseHeader(
            System.Text.Encoding.ASCII.GetBytes(
                header!.Name + ": " + header.Value + "\r\n"),
            new SipParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(result.Identity, Is.EqualTo("alice"));
            Assert.That(header!.Value, Is.EqualTo("Bearer abc.DEF-123_~/+=="));
            Assert.That(parsed.BearerToken!.Token,
                Is.EqualTo("abc.DEF-123_~/+=="));
            Assert.That(provider.CallCount, Is.EqualTo(0));
            Assert.That(FindHeader(result.Message!,
                SipHeaderNames.ProxyAuthorization), Is.Null);
        });
    }

    [Test]
    public void ClientReportsMissingOrMalformedExternalTokenWithBoundedRetry()
    {
        SipAuthenticationClient client = new SipAuthenticationClient(
            new BearerCredentialProvider());
        SipAuthenticationRequest missing = CreateRequest(
            CreateMessage(""), SipCredentialScope.Origin);
        missing.BearerToken = CreateBearerPolicy();
        missing.Attempt = 2;
        SipAuthenticationRequest malformed = CreateRequest(
            CreateMessage(""), SipCredentialScope.Proxy);
        malformed.BearerToken = CreateBearerPolicy();
        malformed.BearerToken.Token = "abc=def";

        SipAuthenticationResult required = client.AuthorizeRequest(missing);
        SipAuthenticationResult invalid = client.AuthorizeRequest(malformed);

        Assert.Multiple(() =>
        {
            Assert.That(required.Status,
                Is.EqualTo(SipAuthenticationStatus.ChallengeRequired));
            Assert.That(required.RetryAllowed, Is.False);
            Assert.That(invalid.Status, Is.EqualTo(SipAuthenticationStatus.Failed));
            Assert.That(invalid.ErrorMessage, Does.Not.Contain("abc=def"));
        });
    }

    [Test]
    public void ServerCreatesBearerChallengeWithoutIssuingAToken()
    {
        SipAuthenticationServer server = new SipAuthenticationServer(
            new BearerCredentialProvider());
        SipAuthenticationRequest request = CreateRequest(
            CreateMessage(""), SipCredentialScope.Proxy);
        request.BearerToken = CreateBearerPolicy();

        SipAuthenticationResult result = server.CreateAuthenticationChallenge(
            request, new SipNonceOptions());
        SipHeader? header = FindHeader(
            result.Message!, SipHeaderNames.ProxyAuthenticate);
        SipHeaderParseResult parsed = new SipHeaderCodec().ParseHeader(
            System.Text.Encoding.ASCII.GetBytes(
                header!.Name + ": " + header.Value + "\r\n"),
            new SipParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(SipAuthenticationStatus.ChallengeRequired));
            Assert.That(result.Message!.StatusCode,
                Is.EqualTo(SipStatusCodes.ProxyAuthenticationRequired));
            Assert.That(parsed.BearerToken!.Token, Is.Null);
            Assert.That(parsed.BearerToken.Realm, Is.EqualTo("sip.example"));
            Assert.That(parsed.BearerToken.Scopes,
                Is.EqualTo(new string[] { "sip:register" }));
            Assert.That(parsed.BearerToken.AuthorizationServer,
                Is.EqualTo("https://auth.example/oauth"));
            Assert.That(result.BearerToken!.Token, Is.Null);
        });
    }

    [Test]
    public void ServerPassesRealmAndScopeToExternalValidator()
    {
        BearerTokenValidator validator = new BearerTokenValidator();
        validator.Status = SipAuthenticationStatus.Success;
        validator.Identity = "alice@example.com";
        validator.GrantedScopes = new string[]
            { "sip:register", "sip:advanced" };
        SipAuthenticationServer server = new SipAuthenticationServer(
            new BearerCredentialProvider());
        SipMessage message = CreateMessage("");
        SipAuthenticationRequest clientRequest = CreateRequest(
            message, SipCredentialScope.Origin);
        clientRequest.BearerToken = CreateBearerPolicy();
        clientRequest.BearerToken.Token = "encrypted.jwt.token";
        SipAuthenticationResult authorized = new SipAuthenticationClient(
            new BearerCredentialProvider()).AuthorizeRequest(clientRequest);
        SipAuthenticationRequest verification = CreateRequest(
            authorized.Message!, SipCredentialScope.Origin);
        verification.BearerToken = CreateBearerPolicy();

        SipAuthenticationResult result = server.VerifyBearerAuthentication(
            verification, validator);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(result.Identity, Is.EqualTo("alice@example.com"));
            Assert.That(validator.ReceivedToken, Is.EqualTo("encrypted.jwt.token"));
            Assert.That(validator.ReceivedRealm, Is.EqualTo("sip.example"));
            Assert.That(validator.ReceivedScopes,
                Is.EqualTo(new string[] { "sip:register" }));
            Assert.That(result.BearerToken!.Token, Is.Null);
            Assert.That(result.BearerToken.Subject,
                Is.EqualTo("alice@example.com"));
            Assert.That(result.BearerToken.Scopes,
                Is.EqualTo(new string[] { "sip:register", "sip:advanced" }));
            Assert.That(FindHeader(result.Message!,
                SipHeaderNames.Authorization), Is.Null);
            Assert.That(result.Message!.OriginalBytes, Is.Empty);
        });
    }

    [TestCase(SipCredentialScope.Origin, "invalid_token", 401, "WWW-Authenticate")]
    [TestCase(SipCredentialScope.Proxy, "invalid_scope", 407, "Proxy-Authenticate")]
    public void ValidatorFailureProducesScopedRetryChallenge(
        SipCredentialScope scope, string error, int statusCode, string headerName)
    {
        BearerTokenValidator validator = new BearerTokenValidator();
        validator.Status = SipAuthenticationStatus.InvalidCredentials;
        validator.Error = error;
        validator.RetryAllowed = true;
        validator.ErrorMessage = "External validation rejected the token.";
        SipAuthenticationServer server = new SipAuthenticationServer(
            new BearerCredentialProvider());
        SipAuthenticationRequest clientRequest = CreateRequest(
            CreateMessage(""), scope);
        clientRequest.BearerToken = CreateBearerPolicy();
        clientRequest.BearerToken.Token = "rejected.token";
        SipAuthenticationResult authorized = new SipAuthenticationClient(
            new BearerCredentialProvider()).AuthorizeRequest(clientRequest);
        SipAuthenticationRequest verification = CreateRequest(
            authorized.Message!, scope);
        verification.BearerToken = CreateBearerPolicy();

        SipAuthenticationResult result = server.VerifyBearerAuthentication(
            verification, validator);
        SipHeader? header = FindHeader(result.Message!, headerName);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(SipAuthenticationStatus.InvalidCredentials));
            Assert.That(result.RetryAllowed, Is.True);
            Assert.That(result.Message!.StatusCode, Is.EqualTo(statusCode));
            Assert.That(header!.Value, Does.Contain("error=\"" + error + "\""));
            Assert.That(result.BearerToken!.Error, Is.EqualTo(error));
            Assert.That(result.BearerToken.Token, Is.Null);
        });
    }

    [Test]
    public void MissingValidatorOrMalformedHeaderDoesNotExposeToken()
    {
        SipAuthenticationServer server = new SipAuthenticationServer(
            new BearerCredentialProvider());
        SipAuthenticationRequest missing = CreateRequest(
            CreateMessage(""), SipCredentialScope.Origin);
        missing.BearerToken = CreateBearerPolicy();
        SipAuthenticationRequest unsupported = CreateRequest(
            AddAuthorization(CreateMessage(""),
                SipHeaderNames.Authorization, "Bearer private.token"),
            SipCredentialScope.Origin);
        unsupported.BearerToken = CreateBearerPolicy();
        SipAuthenticationRequest malformed = CreateRequest(
            AddAuthorization(CreateMessage(""),
                SipHeaderNames.Authorization, "Bearer bad=token"),
            SipCredentialScope.Origin);
        malformed.BearerToken = CreateBearerPolicy();

        SipAuthenticationResult required = server.VerifyBearerAuthentication(
            missing, new BearerTokenValidator());
        SipAuthenticationResult noValidator =
            server.VerifyAuthentication(unsupported);
        SipAuthenticationResult invalid = server.VerifyBearerAuthentication(
            malformed, new BearerTokenValidator());

        Assert.Multiple(() =>
        {
            Assert.That(required.Status,
                Is.EqualTo(SipAuthenticationStatus.ChallengeRequired));
            Assert.That(required.Message!.StatusCode,
                Is.EqualTo(SipStatusCodes.Unauthorized));
            Assert.That(noValidator.Status,
                Is.EqualTo(SipAuthenticationStatus.Unsupported));
            Assert.That(noValidator.ErrorMessage, Does.Not.Contain("private.token"));
            Assert.That(invalid.Status,
                Is.EqualTo(SipAuthenticationStatus.InvalidCredentials));
            Assert.That(invalid.Message!.StatusCode,
                Is.EqualTo(SipStatusCodes.Unauthorized));
            Assert.That(invalid.BearerToken!.Error,
                Is.EqualTo("invalid_token"));
        });
    }

    [Test]
    public void ExternalValidatorErrorCannotEchoBearerSecretIntoResult()
    {
        BearerTokenValidator validator = new BearerTokenValidator();
        validator.Status = SipAuthenticationStatus.InvalidCredentials;
        validator.Error = "invalid_token";
        validator.ErrorMessage =
            "The token rejected.token must never reach logs.";
        SipAuthenticationServer server = new SipAuthenticationServer(
            new BearerCredentialProvider());
        SipAuthenticationRequest clientRequest = CreateRequest(
            CreateMessage(""), SipCredentialScope.Origin);
        clientRequest.BearerToken = CreateBearerPolicy();
        clientRequest.BearerToken.Token = "rejected.token";
        SipAuthenticationResult authorized = new SipAuthenticationClient(
            new BearerCredentialProvider()).AuthorizeRequest(clientRequest);
        SipAuthenticationRequest verification = CreateRequest(
            authorized.Message!, SipCredentialScope.Origin);
        verification.BearerToken = CreateBearerPolicy();

        SipAuthenticationResult result = server.VerifyBearerAuthentication(
            verification, validator);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(SipAuthenticationStatus.InvalidCredentials));
            Assert.That(result.ErrorMessage, Does.Not.Contain("rejected.token"));
            Assert.That(result.BearerToken!.Token, Is.Null);
            Assert.That(FindHeader(result.Message!,
                SipHeaderNames.WwwAuthenticate)!.Value,
                Does.Not.Contain("rejected.token"));
        });
    }

    [Test]
    public void ExternalValidatorCanEnforceBearerRealmAndScope()
    {
        BearerRealmScopeValidator validator = new BearerRealmScopeValidator();
        validator.ExpectedRealm = "sip.example";
        validator.ExpectedScope = "sip:register";
        SipAuthenticationServer server = new SipAuthenticationServer(
            new BearerCredentialProvider());
        SipAuthenticationRequest clientRequest = CreateRequest(
            CreateMessage(""), SipCredentialScope.Origin);
        clientRequest.BearerToken = CreateBearerPolicy();
        clientRequest.BearerToken.Token = "realm.scope.token";
        SipAuthenticationResult authorized = new SipAuthenticationClient(
            new BearerCredentialProvider()).AuthorizeRequest(clientRequest);
        SipAuthenticationRequest matching = CreateRequest(
            authorized.Message!, SipCredentialScope.Origin);
        matching.BearerToken = CreateBearerPolicy();
        SipAuthenticationRequest wrongRealm = CreateRequest(
            authorized.Message!, SipCredentialScope.Origin);
        wrongRealm.BearerToken = CreateBearerPolicy();
        wrongRealm.BearerToken.Realm = "other.example";
        SipAuthenticationRequest wrongScope = CreateRequest(
            authorized.Message!, SipCredentialScope.Origin);
        wrongScope.BearerToken = CreateBearerPolicy();
        wrongScope.BearerToken.Scopes = new string[] { "sip:invite" };

        SipAuthenticationResult accepted = server.VerifyBearerAuthentication(
            matching, validator);
        SipAuthenticationResult realmDenied = server.VerifyBearerAuthentication(
            wrongRealm, validator);
        SipAuthenticationResult scopeDenied = server.VerifyBearerAuthentication(
            wrongScope, validator);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Status,
                Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(accepted.Identity, Is.EqualTo("alice@example.com"));
            Assert.That(realmDenied.Status,
                Is.EqualTo(SipAuthenticationStatus.InvalidCredentials));
            Assert.That(realmDenied.BearerToken!.Error,
                Is.EqualTo("invalid_token"));
            Assert.That(scopeDenied.Status,
                Is.EqualTo(SipAuthenticationStatus.InvalidCredentials));
            Assert.That(scopeDenied.BearerToken!.Error,
                Is.EqualTo("invalid_scope"));
            Assert.That(validator.CallCount, Is.EqualTo(3));
        });
    }

    static SipBearerToken CreateBearerPolicy()
    {
        SipBearerToken token = new SipBearerToken();
        token.Realm = "sip.example";
        token.AuthorizationServer = "https://auth.example/oauth";
        token.Scopes = new string[] { "sip:register" };
        return token;
    }

    static SipAuthenticationRequest CreateRequest(
        SipMessage message, SipCredentialScope scope)
    {
        SipAuthenticationRequest request = new SipAuthenticationRequest();
        request.Message = message;
        request.Target = message.RequestUri;
        request.Body = message.Body == null
            ? new byte[0] : (byte[])message.Body.Clone();
        request.Scope = scope;
        request.CorrelationId = "bearer-test";
        return request;
    }

    static SipMessage CreateMessage(string body)
    {
        byte[] bodyBytes = System.Text.Encoding.ASCII.GetBytes(body);
        string text = "REGISTER sip:example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/TLS client.example.com;branch=z9hG4bKbearer\r\n"
            + "From: <sip:alice@example.com>;tag=bearer-from\r\n"
            + "To: <sip:alice@example.com>\r\n"
            + "Call-ID: bearer@example.com\r\n"
            + "CSeq: 1 REGISTER\r\n"
            + "Content-Length: " + bodyBytes.Length + "\r\n\r\n"
            + body;
        SipParseOptions options = new SipParseOptions();
        options.Strict = false;
        SipParseResult parsed = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), options);
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Message!;
    }

    static SipMessage AddAuthorization(
        SipMessage message, string name, string value)
    {
        SipHeader[] headers = new SipHeader[message.Headers.Length + 1];
        System.Array.Copy(message.Headers, headers, message.Headers.Length);
        SipHeader added = new SipHeader();
        added.Name = name;
        added.Value = value;
        headers[headers.Length - 1] = added;
        message.Headers = headers;
        return message;
    }

    static SipHeader? FindHeader(SipMessage message, string name)
    {
        int index = 0;
        while (index < message.Headers.Length)
        {
            if (string.Equals(message.Headers[index].Name, name,
                StringComparison.OrdinalIgnoreCase))
            {
                return message.Headers[index];
            }
            index = index + 1;
        }
        return null;
    }
}

class BearerCredentialProvider : ISipCredentialProvider
{
    internal int CallCount;

    public SipCredential GetCredential(SipAuthenticationRequest request)
    {
        CallCount = CallCount + 1;
        return new SipCredential();
    }
}

class BearerTokenValidator : ISipTokenValidator
{
    internal SipAuthenticationStatus Status;
    internal string? Identity;
    internal string? Error;
    internal string? ErrorMessage;
    internal bool RetryAllowed;
    internal string[] GrantedScopes = new string[0];
    internal string? ReceivedToken;
    internal string? ReceivedRealm;
    internal string[] ReceivedScopes = new string[0];

    public SipAuthenticationResult ValidateBearerToken(
        SipBearerToken token, SipAuthenticationRequest request)
    {
        ReceivedToken = token.Token;
        ReceivedRealm = token.Realm;
        ReceivedScopes = (string[])token.Scopes.Clone();
        token.Subject = Identity;
        token.Error = Error;
        if (GrantedScopes.Length > 0)
        {
            token.Scopes = (string[])GrantedScopes.Clone();
        }
        SipAuthenticationResult result = new SipAuthenticationResult();
        result.Status = Status;
        result.Identity = Identity;
        result.ErrorMessage = ErrorMessage;
        result.RetryAllowed = RetryAllowed;
        return result;
    }
}

class BearerRealmScopeValidator : ISipTokenValidator
{
    internal string? ExpectedRealm;
    internal string? ExpectedScope;
    internal int CallCount;

    public SipAuthenticationResult ValidateBearerToken(
        SipBearerToken token, SipAuthenticationRequest request)
    {
        CallCount = CallCount + 1;
        SipAuthenticationResult result = new SipAuthenticationResult();
        if (token.Realm != ExpectedRealm)
        {
            token.Error = "invalid_token";
            result.Status = SipAuthenticationStatus.InvalidCredentials;
            return result;
        }
        bool scopeFound = false;
        int index = 0;
        while (index < token.Scopes.Length)
        {
            if (token.Scopes[index] == ExpectedScope)
            {
                scopeFound = true;
            }
            index = index + 1;
        }
        if (!scopeFound)
        {
            token.Error = "invalid_scope";
            result.Status = SipAuthenticationStatus.InvalidCredentials;
            return result;
        }
        token.Subject = "alice@example.com";
        result.Status = SipAuthenticationStatus.Success;
        result.Identity = "alice@example.com";
        return result;
    }
}
