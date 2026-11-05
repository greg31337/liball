namespace sip.Tests;

public class NetworkAndAuthenticationModelTests
{
    [Test]
    public void NetworkModelsExposeSafeDefaultsAndCompose()
    {
        SipEndpointAddress endpoint = new SipEndpointAddress();
        SipDnsOptions dns = new SipDnsOptions();
        SipResolutionRequest resolutionRequest = new SipResolutionRequest();
        SipServerCandidate candidate = new SipServerCandidate();
        SipResolutionResult resolutionResult = new SipResolutionResult();
        resolutionResult.Candidates = [candidate];

        SipTransportOptions transportOptions = new SipTransportOptions();
        SipTlsOptions tls = new SipTlsOptions();
        SipWebSocketOptions webSocket = new SipWebSocketOptions();
        SipConnectionInfo connection = new SipConnectionInfo();
        SipFlow flow = new SipFlow();
        flow.Connection = connection;

        SipMessage message = new SipMessage();
        SipParseResult parseResult = new SipParseResult();
        SipTransportMessage transportMessage = new SipTransportMessage();
        transportMessage.Message = message;
        transportMessage.LocalAddress = endpoint;
        transportMessage.RemoteAddress = endpoint;
        transportMessage.Connection = connection;
        transportMessage.Flow = flow;
        transportMessage.ParseResult = parseResult;

        SipTransportEvent transportEvent = new SipTransportEvent();
        transportEvent.Message = transportMessage;
        transportEvent.Connection = connection;
        transportEvent.Flow = flow;

        SipSendRequest sendRequest = new SipSendRequest();
        sendRequest.Message = message;
        sendRequest.RemoteAddress = endpoint;
        SipSendResult sendResult = new SipSendResult();
        sendResult.Connection = connection;
        sendResult.Flow = flow;

        Assert.Multiple(() =>
        {
            Assert.That(endpoint.Port, Is.EqualTo(-1));
            Assert.That(endpoint.Transport, Is.EqualTo(SipTransportKind.Unknown));
            Assert.That(dns.NameServers, Is.Empty);
            Assert.That(dns.TimeoutMilliseconds, Is.EqualTo(2000));
            Assert.That(dns.MaxAttempts, Is.EqualTo(3));
            Assert.That(dns.MaxRecordCount, Is.EqualTo(SipDefaults.MaxDnsRecordCount));
            Assert.That(dns.MaxCnameDepth, Is.EqualTo(SipDefaults.MaxDnsCnameDepth));
            Assert.That(dns.EnableCache, Is.True);
            Assert.That(resolutionRequest.Transport, Is.EqualTo(SipTransportKind.Unknown));
            Assert.That(candidate.Port, Is.EqualTo(-1));
            Assert.That(candidate.ExpiresAtMilliseconds, Is.EqualTo(-1));
            Assert.That(resolutionResult.Status, Is.EqualTo(SipResolutionStatus.None));
            Assert.That(resolutionResult.Candidates, Is.EqualTo(new SipServerCandidate[] { candidate }));
            Assert.That(transportOptions.MaxDatagramBytes, Is.EqualTo(65507));
            Assert.That(transportOptions.MaxQueueCount, Is.EqualTo(SipDefaults.MaxQueueCount));
            Assert.That(transportOptions.ReuseConnections, Is.True);
            Assert.That(tls.CertificateBytes, Is.Empty);
            Assert.That(tls.PrivateKeyBytes, Is.Empty);
            Assert.That(tls.TrustedRoots, Is.Empty);
            Assert.That(tls.ValidatePeer, Is.True);
            Assert.That(webSocket.Path, Is.EqualTo("/"));
            Assert.That(webSocket.Subprotocol, Is.EqualTo(SipTransportNames.WebSocketSubprotocol));
            Assert.That(webSocket.AllowBinaryFrames, Is.True);
            Assert.That(webSocket.MaxMessageBytes, Is.EqualTo(SipDefaults.MaxMessageBytes));
            Assert.That(connection.State, Is.EqualTo(SipConnectionState.Closed));
            Assert.That(flow.RegistrationId, Is.EqualTo(-1));
            Assert.That(flow.ExpiresAtMilliseconds, Is.EqualTo(-1));
            Assert.That(flow.Connection, Is.SameAs(connection));
            Assert.That(transportMessage.Message, Is.SameAs(message));
            Assert.That(transportMessage.Bytes, Is.Empty);
            Assert.That(transportMessage.ParseResult, Is.SameAs(parseResult));
            Assert.That(transportMessage.TimestampMilliseconds, Is.EqualTo(-1));
            Assert.That(transportEvent.Kind, Is.EqualTo(SipTransportEventKind.None));
            Assert.That(transportEvent.Message, Is.SameAs(transportMessage));
            Assert.That(sendRequest.Message, Is.SameAs(message));
            Assert.That(sendRequest.Bytes, Is.Empty);
            Assert.That(sendRequest.DeadlineMilliseconds, Is.EqualTo(-1));
            Assert.That(sendResult.Success, Is.False);
            Assert.That(sendResult.Connection, Is.SameAs(connection));
        });
    }

    [Test]
    public void NetworkMutableDefaultsAreOwnedPerInstance()
    {
        SipDnsOptions firstDns = new SipDnsOptions();
        SipDnsOptions secondDns = new SipDnsOptions();
        SipTlsOptions firstTls = new SipTlsOptions();
        SipTlsOptions secondTls = new SipTlsOptions();
        SipTransportMessage firstMessage = new SipTransportMessage();
        SipTransportMessage secondMessage = new SipTransportMessage();
        SipSendRequest firstRequest = new SipSendRequest();
        SipSendRequest secondRequest = new SipSendRequest();
        SipResolutionResult firstResult = new SipResolutionResult();
        SipResolutionResult secondResult = new SipResolutionResult();

        Assert.Multiple(() =>
        {
            Assert.That(firstDns.NameServers, Is.Not.SameAs(secondDns.NameServers));
            Assert.That(firstTls.CertificateBytes, Is.Not.SameAs(secondTls.CertificateBytes));
            Assert.That(firstTls.TrustedRoots, Is.Not.SameAs(secondTls.TrustedRoots));
            Assert.That(firstMessage.Bytes, Is.Not.SameAs(secondMessage.Bytes));
            Assert.That(firstRequest.Bytes, Is.Not.SameAs(secondRequest.Bytes));
            Assert.That(firstResult.Candidates, Is.Not.SameAs(secondResult.Candidates));
        });
    }

    [Test]
    public void AuthenticationModelsRetainRelationshipsAndSafeDefaults()
    {
        SipCredential credential = new SipCredential();
        credential.Scope = SipCredentialScope.Proxy;
        SipDigestChallenge challenge = new SipDigestChallenge();
        challenge.Algorithm = SipDigestAlgorithm.Sha256;
        challenge.Qualities = [SipDigestQuality.Auth, SipDigestQuality.AuthInt];
        SipDigestAuthorization authorization = new SipDigestAuthorization();
        authorization.Algorithm = SipDigestAlgorithm.Sha512256;
        authorization.Quality = SipDigestQuality.AuthInt;

        SipAuthenticationRequest request = new SipAuthenticationRequest();
        request.Challenges = [challenge];
        request.Credential = credential;
        request.Body = [1, 2];
        request.Scope = SipCredentialScope.Proxy;

        SipAuthenticationResult result = new SipAuthenticationResult();
        result.Status = SipAuthenticationStatus.Success;
        result.DigestAuthorization = authorization;
        SipNonceOptions nonce = new SipNonceOptions();
        SipBearerToken bearer = new SipBearerToken();
        bearer.Scopes = ["sip:register"];
        SipCertificateIdentity certificate = new SipCertificateIdentity();
        certificate.Domains = ["example.com"];
        SipAuthorizationDecision decision = new SipAuthorizationDecision();
        decision.Scopes = ["sip:register"];

        Assert.Multiple(() =>
        {
            Assert.That(credential.Secret, Is.Empty);
            Assert.That(credential.Scope, Is.EqualTo(SipCredentialScope.Proxy));
            Assert.That(challenge.Algorithm, Is.EqualTo(SipDigestAlgorithm.Sha256));
            Assert.That(challenge.Qualities, Is.EqualTo(new SipDigestQuality[] { SipDigestQuality.Auth, SipDigestQuality.AuthInt }));
            Assert.That(challenge.Parameters, Is.Empty);
            Assert.That(authorization.NonceCount, Is.EqualTo(-1));
            Assert.That(authorization.Algorithm, Is.EqualTo(SipDigestAlgorithm.Sha512256));
            Assert.That(authorization.Quality, Is.EqualTo(SipDigestQuality.AuthInt));
            Assert.That(authorization.Parameters, Is.Empty);
            Assert.That(request.Challenges, Is.EqualTo(new SipDigestChallenge[] { challenge }));
            Assert.That(request.Credential, Is.SameAs(credential));
            Assert.That(request.Body, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(result.Status, Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(result.DigestAuthorization, Is.SameAs(authorization));
            Assert.That(nonce.LifetimeSeconds, Is.EqualTo(300));
            Assert.That(nonce.ClockSkewSeconds, Is.EqualTo(30));
            Assert.That(nonce.CurrentKey, Is.Empty);
            Assert.That(nonce.PreviousKey, Is.Empty);
            Assert.That(bearer.Scopes, Is.EqualTo(new string[] { "sip:register" }));
            Assert.That(certificate.Domains, Is.EqualTo(new string[] { "example.com" }));
            Assert.That(certificate.CertificateBytes, Is.Empty);
            Assert.That(decision.Allowed, Is.False);
            Assert.That(decision.Scopes, Is.EqualTo(new string[] { "sip:register" }));
        });
    }
}
