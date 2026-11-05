namespace sip.Tests;

public class SipRegistrationClientStateTests
{
    [Test]
    public void SuccessMatchesContactsAndSchedulesJitteredAutomaticRefresh()
    {
        SipRegistrationClient client = new SipRegistrationClient();
        client.AdvanceRegistrationTime(1000);
        SipRegistrationRequest command = Request();
        command.Options.ExpiresSeconds = 120;
        command.Options.RefreshBeforeSeconds = 20;
        command.Options.RefreshJitterSeconds = 10;
        SipRegistrationSnapshot pending = client.RegisterContact(command);
        SipMessage register = client.PollRegistrationEvent().Message!;
        SipMessage response = Response(register, 200,
            "Contact: <sip:someone@other.example.com>;expires=50\r\n"
            + "Contact: <sip:alice@device.example.com>;expires=100\r\n");

        SipRegistrationSnapshot active =
            client.ReceiveRegistrationResponse(response);
        SipRegistrationEvent success = client.PollRegistrationEvent();
        client.AdvanceRegistrationTime(active.RefreshAtMilliseconds);
        SipRegistrationEvent refresh = client.PollRegistrationEvent();

        Assert.Multiple(() =>
        {
            Assert.That(active.State, Is.EqualTo(SipRegistrationState.Registered));
            Assert.That(active.Bindings, Has.Length.EqualTo(1));
            Assert.That(active.Bindings[0].Contact!.Address!.Uri!.User,
                Is.EqualTo("alice"));
            Assert.That(active.ExpiresAtMilliseconds, Is.EqualTo(101000));
            Assert.That(active.RefreshAtMilliseconds,
                Is.InRange(71000, 81000));
            Assert.That(success.State, Is.EqualTo(SipRegistrationState.Registered));
            Assert.That(success.Message!.StatusCode, Is.EqualTo(200));
            Assert.That(refresh.State, Is.EqualTo(SipRegistrationState.Refreshing));
            Assert.That(refresh.Message!.CSeq!.SequenceNumber, Is.EqualTo(2));
            Assert.That(Header(refresh.Message, SipHeaderNames.CallId),
                Is.EqualTo(pending.CallId));
        });
    }

    [TestCase(401, "WWW-Authenticate", "Authorization", SipCredentialScope.Origin)]
    [TestCase(407, "Proxy-Authenticate", "Proxy-Authorization", SipCredentialScope.Proxy)]
    public void ChallengeCreatesAssociatedBoundedAuthenticationRetry(
        int status, string challengeName, string authorizationName,
        SipCredentialScope expectedScope)
    {
        RecordingRegistrationAuthenticationClient authentication =
            new RecordingRegistrationAuthenticationClient();
        SipRegistrationClient client = new SipRegistrationClient();
        client.ConfigureRegistrationAuthenticationClient(authentication);
        SipRegistrationRequest command = Request();
        command.AuthenticationContextId = "registration-auth";
        command.Options.MaxAuthenticationAttempts = 1;
        client.RegisterContact(command);
        SipMessage first = client.PollRegistrationEvent().Message!;
        string challenge = challengeName
            + ": Digest realm=\"example.com\", nonce=\"one\", "
            + "algorithm=SHA-256, qop=\"auth\"\r\n";

        SipRegistrationSnapshot retrying = client.ReceiveRegistrationResponse(
            Response(first, status, challenge));
        SipRegistrationEvent retry = client.PollRegistrationEvent();
        SipRegistrationSnapshot limited = client.ReceiveRegistrationResponse(
            Response(retry.Message!, status, challenge));
        SipRegistrationEvent failure = client.PollRegistrationEvent();

        Assert.Multiple(() =>
        {
            Assert.That(retrying.State, Is.EqualTo(SipRegistrationState.Registering));
            Assert.That(retrying.AuthenticationAttemptCount, Is.EqualTo(1));
            Assert.That(retry.Message!.CSeq!.SequenceNumber, Is.EqualTo(2));
            Assert.That(Header(retry.Message, authorizationName), Is.EqualTo("test-value"));
            Assert.That(authentication.Scope, Is.EqualTo(expectedScope));
            Assert.That(authentication.Attempt, Is.EqualTo(0));
            Assert.That(authentication.CorrelationId,
                Is.EqualTo("registration-auth"));
            Assert.That(limited.State, Is.EqualTo(SipRegistrationState.Failed));
            Assert.That(limited.ErrorMessage, Does.Contain("limit"));
            Assert.That(failure.Message!.StatusCode, Is.EqualTo(status));
        });
    }

    [Test]
    public void RedirectPolicyChoosesHighestQualityAndEnforcesLimit()
    {
        SipRegistrationClient client = new SipRegistrationClient();
        SipRegistrationRequest command = Request();
        command.Options.MaxRedirects = 1;
        client.RegisterContact(command);
        SipMessage first = client.PollRegistrationEvent().Message!;

        SipRegistrationSnapshot redirected = client.ReceiveRegistrationResponse(
            Response(first, SipStatusCodes.MovedTemporarily,
                "Contact: <sip:low.example.net>;q=0.2\r\n"
                + "Contact: <sip:preferred.example.net>;q=0.9\r\n"));
        SipRegistrationEvent retry = client.PollRegistrationEvent();
        SipRegistrationSnapshot limited = client.ReceiveRegistrationResponse(
            Response(retry.Message!, SipStatusCodes.MovedTemporarily,
                "Contact: <sip:third.example.net>\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(redirected.RedirectCount, Is.EqualTo(1));
            Assert.That(retry.Message!.RequestUri!.Scheme,
                Is.EqualTo(SipUriScheme.Sip));
            Assert.That(retry.Message.RequestUri.Host,
                Is.EqualTo("preferred.example.net"));
            Assert.That(retry.Message.CSeq!.SequenceNumber, Is.EqualTo(2));
            Assert.That(limited.State, Is.EqualTo(SipRegistrationState.Failed));
            Assert.That(limited.ErrorMessage, Does.Contain("policy"));
        });

        SipRegistrationClient refusing = new SipRegistrationClient();
        SipRegistrationRequest refusedCommand = Request();
        refusedCommand.Options.FollowRedirects = false;
        refusing.RegisterContact(refusedCommand);
        SipMessage refusedRequest = refusing.PollRegistrationEvent().Message!;
        SipRegistrationSnapshot refused = refusing.ReceiveRegistrationResponse(
            Response(refusedRequest, SipStatusCodes.MovedPermanently,
                "Contact: <sip:new.example.net>\r\n"));
        Assert.That(refused.State, Is.EqualTo(SipRegistrationState.Failed));
    }

    [Test]
    public void IntervalTooBriefRaisesExpiryAndRejectsUnacceptableMinimum()
    {
        SipRegistrationClient client = new SipRegistrationClient();
        SipRegistrationRequest command = Request();
        command.Contacts[0].ExpiresSeconds = 90;
        command.Options.ExpiresSeconds = 120;
        command.Options.MaxExpiresSeconds = 600;
        client.RegisterContact(command);
        SipMessage first = client.PollRegistrationEvent().Message!;

        SipRegistrationSnapshot retrying = client.ReceiveRegistrationResponse(
            Response(first, SipStatusCodes.IntervalTooBrief,
                "Min-Expires: 300\r\n"));
        SipRegistrationEvent retry = client.PollRegistrationEvent();

        Assert.Multiple(() =>
        {
            Assert.That(retrying.State, Is.EqualTo(SipRegistrationState.Registering));
            Assert.That(retry.Message!.CSeq!.SequenceNumber, Is.EqualTo(2));
            Assert.That(Header(retry.Message, SipHeaderNames.Expires),
                Is.EqualTo("300"));
            Assert.That(retry.Message.ContactValues[0].ExpiresSeconds,
                Is.EqualTo(300));
        });

        SipRegistrationSnapshot failed = client.ReceiveRegistrationResponse(
            Response(retry.Message!, SipStatusCodes.IntervalTooBrief,
                "Min-Expires: 900\r\n"));
        Assert.That(failed.State, Is.EqualTo(SipRegistrationState.Failed));
    }

    [Test]
    public void TransientFailureRetriesAtDeadlineWhilePermanentFailureDoesNot()
    {
        SipRegistrationClient transientClient = new SipRegistrationClient();
        SipRegistrationRequest transientCommand = Request();
        transientClient.RegisterContact(transientCommand);
        SipMessage first = transientClient.PollRegistrationEvent().Message!;
        SipRegistrationSnapshot transient =
            transientClient.ReceiveRegistrationResponse(
                Response(first, SipStatusCodes.ServiceUnavailable,
                    "Retry-After: 7\r\n"));
        transientClient.PollRegistrationEvent();
        transientClient.AdvanceRegistrationTime(6999);
        SipRegistrationEvent early = transientClient.PollRegistrationEvent();
        transientClient.AdvanceRegistrationTime(7000);
        SipRegistrationEvent retried = transientClient.PollRegistrationEvent();

        SipRegistrationClient permanentClient = new SipRegistrationClient();
        permanentClient.RegisterContact(Request());
        SipMessage permanentRequest =
            permanentClient.PollRegistrationEvent().Message!;
        SipRegistrationSnapshot permanent =
            permanentClient.ReceiveRegistrationResponse(
                Response(permanentRequest, SipStatusCodes.Forbidden, ""));
        permanentClient.PollRegistrationEvent();
        permanentClient.AdvanceRegistrationTime(100000);

        Assert.Multiple(() =>
        {
            Assert.That(transient.State, Is.EqualTo(SipRegistrationState.Failed));
            Assert.That(transient.RetryAtMilliseconds, Is.EqualTo(7000));
            Assert.That(early.Message, Is.Null);
            Assert.That(retried.State, Is.EqualTo(SipRegistrationState.Registering));
            Assert.That(retried.Message!.CSeq!.SequenceNumber, Is.EqualTo(2));
            Assert.That(permanent.State, Is.EqualTo(SipRegistrationState.Failed));
            Assert.That(permanent.RetryAtMilliseconds, Is.EqualTo(-1));
            Assert.That(permanentClient.PollRegistrationEvent().Message, Is.Null);
        });
    }

    [Test]
    public void SuccessWithoutMatchingContactFailsClosed()
    {
        SipRegistrationClient client = new SipRegistrationClient();
        client.RegisterContact(Request());
        SipMessage register = client.PollRegistrationEvent().Message!;

        SipRegistrationSnapshot failed = client.ReceiveRegistrationResponse(
            Response(register, 200,
                "Contact: <sip:mallory@device.example.com>;expires=120\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(failed.State, Is.EqualTo(SipRegistrationState.Failed));
            Assert.That(failed.Bindings, Is.Empty);
            Assert.That(failed.ErrorMessage, Does.Contain("matching Contact"));
        });
    }

    [Test]
    public void ExpiryAndShutdownTerminateAndRejectLateResponses()
    {
        SipRegistrationClient expiring = new SipRegistrationClient();
        SipRegistrationRequest expiringCommand = Request();
        expiringCommand.Options.ExpiresSeconds = 60;
        expiringCommand.Options.RefreshBeforeSeconds = 10;
        expiringCommand.Options.RefreshJitterSeconds = 0;
        expiring.RegisterContact(expiringCommand);
        SipMessage register = expiring.PollRegistrationEvent().Message!;
        SipRegistrationSnapshot active = expiring.ReceiveRegistrationResponse(
            Response(register, 200,
                "Contact: <sip:alice@device.example.com>;expires=60\r\n"));
        expiring.PollRegistrationEvent();
        expiring.AdvanceRegistrationTime(active.ExpiresAtMilliseconds);
        SipRegistrationEvent expired = expiring.PollRegistrationEvent();

        SipRegistrationClient stopping = new SipRegistrationClient();
        stopping.RegisterContact(Request());
        SipMessage outstanding = stopping.PollRegistrationEvent().Message!;
        SipCancellation cancellation = new SipCancellation();
        cancellation.Requested = true;
        cancellation.Reason = "application shutdown";
        stopping.ShutdownRegistration(cancellation);
        SipRegistrationEvent stopped = stopping.PollRegistrationEvent();

        Assert.Multiple(() =>
        {
            Assert.That(expired.State, Is.EqualTo(SipRegistrationState.Terminated));
            Assert.That(expired.Snapshot!.Bindings, Is.Empty);
            Assert.That(expired.Reason, Does.Contain("expired"));
            Assert.That(stopped.State, Is.EqualTo(SipRegistrationState.Terminated));
            Assert.That(stopped.Reason, Is.EqualTo("application shutdown"));
            Assert.Throws<InvalidOperationException>(() =>
                stopping.ReceiveRegistrationResponse(
                    Response(outstanding, 200,
                        "Contact: <sip:alice@device.example.com>\r\n")));
        });
    }

    static SipRegistrationRequest Request()
    {
        SipContactValue contact = new SipContactValue();
        SipNameAddress contactAddress = new SipNameAddress();
        contactAddress.Uri = Uri("sip:alice@device.example.com");
        contact.Address = contactAddress;
        SipRegistrationRequest request = new SipRegistrationRequest();
        request.AddressOfRecord = Uri("sip:alice@example.com");
        request.Target = Uri("sip:registrar.example.com");
        request.Contacts = new SipContactValue[] { contact };
        request.CorrelationId = "registration-operation";
        return request;
    }

    static SipMessage Response(SipMessage request, int status, string extraHeaders)
    {
        string text = "SIP/2.0 " + status + " Result\r\n"
            + "Via: " + Header(request, SipHeaderNames.Via) + "\r\n"
            + "From: " + Header(request, SipHeaderNames.From) + "\r\n"
            + "To: " + Header(request, SipHeaderNames.To) + "\r\n"
            + "Call-ID: " + Header(request, SipHeaderNames.CallId) + "\r\n"
            + "CSeq: " + Header(request, SipHeaderNames.CSeq) + "\r\n"
            + extraHeaders
            + "Content-Length: 0\r\n\r\n";
        SipParseResult parsed = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Message!;
    }

    static SipUri Uri(string text)
    {
        SipParseResult parsed = new SipUriCodec().ParseUri(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Uri!;
    }

    static string? Header(SipMessage message, string name)
    {
        return message.Headers.FirstOrDefault(value =>
            string.Equals(value.Name, name,
                StringComparison.OrdinalIgnoreCase))?.Value;
    }
}

public class RecordingRegistrationAuthenticationClient : ISipAuthenticationClient
{
    public SipCredentialScope Scope;
    public int Attempt = -1;
    public string? CorrelationId;

    public SipAuthenticationResult AuthorizeRequest(SipAuthenticationRequest request)
    {
        Scope = request.Scope;
        Attempt = request.Attempt;
        CorrelationId = request.CorrelationId;
        SipHeader header = new SipHeader();
        header.Name = request.Scope == SipCredentialScope.Proxy
            ? SipHeaderNames.ProxyAuthorization : SipHeaderNames.Authorization;
        header.Value = "test-value";
        request.Message!.Headers = request.Message.Headers
            .Concat(new SipHeader[] { header }).ToArray();
        SipAuthenticationResult result = new SipAuthenticationResult();
        result.Status = SipAuthenticationStatus.Success;
        result.RetryAllowed = true;
        result.Message = request.Message;
        result.CorrelationId = request.CorrelationId;
        return result;
    }

    public SipAuthenticationResult VerifyAuthenticationInfo(
        SipAuthenticationRequest request)
    {
        return new SipAuthenticationResult();
    }
}
