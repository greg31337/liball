namespace sip.Tests;

public class SipRegistrationClientCoverageTests
{
    [Test]
    public void TransientUnregistrationFailureRetriesTheUnregistration()
    {
        SipRegistrationClient client = RegisteredClient();
        SipRegistrationRequest remove = Request(false);

        client.UnregisterContact(remove);
        SipMessage first = client.PollRegistrationEvent().Message!;
        SipRegistrationSnapshot delayed =
            client.ReceiveRegistrationResponse(Response(first,
                SipStatusCodes.ServiceUnavailable,
                "Retry-After: 2\r\n"));
        client.PollRegistrationEvent();
        client.AdvanceRegistrationTime(1999);
        SipRegistrationEvent early = client.PollRegistrationEvent();
        client.AdvanceRegistrationTime(2000);
        SipRegistrationEvent retry = client.PollRegistrationEvent();
        SipRegistrationSnapshot completed =
            client.ReceiveRegistrationResponse(Response(retry.Message!,
                SipStatusCodes.Ok, ""));

        Assert.Multiple(() =>
        {
            Assert.That(Header(first, SipHeaderNames.Expires),
                Is.EqualTo("0"));
            Assert.That(delayed.State,
                Is.EqualTo(SipRegistrationState.Registered));
            Assert.That(delayed.Bindings, Has.Length.EqualTo(1));
            Assert.That(delayed.RetryAtMilliseconds, Is.EqualTo(2000));
            Assert.That(early.Message, Is.Null);
            Assert.That(retry.State,
                Is.EqualTo(SipRegistrationState.Unregistering));
            Assert.That(Header(retry.Message!, SipHeaderNames.Expires),
                Is.EqualTo("0"));
            Assert.That(retry.Message!.ContactValues[0].ExpiresSeconds,
                Is.EqualTo(0));
            Assert.That(completed.State,
                Is.EqualTo(SipRegistrationState.Terminated));
            Assert.That(completed.Bindings, Is.Empty);
        });
    }

    [Test]
    public void ProvisionalAndSuccessfulRefreshResponsesAdvanceStates()
    {
        SipRegistrationClient client = new SipRegistrationClient();
        client.AdvanceRegistrationTime(1000);
        SipRegistrationRequest command = Request(false);
        client.RegisterContact(command);
        SipMessage initial = client.PollRegistrationEvent().Message!;

        SipRegistrationSnapshot provisional =
            client.ReceiveRegistrationResponse(Response(initial,
                SipStatusCodes.Ringing, ""));
        SipRegistrationEvent provisionalEvent =
            client.PollRegistrationEvent();
        SipRegistrationSnapshot registered =
            client.ReceiveRegistrationResponse(Success(initial, 90));
        client.PollRegistrationEvent();

        client.RefreshRegistration(command);
        SipMessage refresh = client.PollRegistrationEvent().Message!;
        SipRegistrationSnapshot refreshed =
            client.ReceiveRegistrationResponse(Success(refresh, 60));

        Assert.Multiple(() =>
        {
            Assert.That(provisional.State,
                Is.EqualTo(SipRegistrationState.Registering));
            Assert.That(provisional.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.Ringing));
            Assert.That(provisionalEvent.Message!.StatusCode,
                Is.EqualTo(SipStatusCodes.Ringing));
            Assert.That(registered.State,
                Is.EqualTo(SipRegistrationState.Registered));
            Assert.That(refresh.CSeq!.SequenceNumber,
                Is.EqualTo(initial.CSeq!.SequenceNumber + 1));
            Assert.That(refreshed.State,
                Is.EqualTo(SipRegistrationState.Registered));
            Assert.That(refreshed.ExpiresAtMilliseconds,
                Is.EqualTo(61000));
            Assert.That(refreshed.Bindings[0].ExpiresAtMilliseconds,
                Is.EqualTo(61000));
        });
    }

    [Test]
    public void RedirectErrorsRejectMissingTargetsAndSipsDowngrades()
    {
        SipRegistrationClient missingClient =
            new SipRegistrationClient();
        missingClient.RegisterContact(Request(false));
        SipMessage missingRequest =
            missingClient.PollRegistrationEvent().Message!;
        SipRegistrationSnapshot missing =
            missingClient.ReceiveRegistrationResponse(
                Response(missingRequest,
                    SipStatusCodes.MovedTemporarily, ""));

        SipRegistrationClient secureClient =
            new SipRegistrationClient();
        secureClient.RegisterContact(Request(true));
        SipMessage secureRequest =
            secureClient.PollRegistrationEvent().Message!;
        SipRegistrationSnapshot downgrade =
            secureClient.ReceiveRegistrationResponse(
                Response(secureRequest,
                    SipStatusCodes.MovedTemporarily,
                    "Contact: <sip:registrar.example.net>\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(missing.State,
                Is.EqualTo(SipRegistrationState.Failed));
            Assert.That(missing.ErrorMessage,
                Does.Contain("no usable Contact"));
            Assert.That(downgrade.State,
                Is.EqualTo(SipRegistrationState.Failed));
            Assert.That(downgrade.ErrorMessage,
                Does.Contain("cannot downgrade"));
        });
    }

    [Test]
    public void AuthenticationFailuresAreTerminalWithoutRetryMessages()
    {
        SipRegistrationClient missingClient =
            new SipRegistrationClient();
        missingClient.RegisterContact(Request(false));
        SipMessage missingRequest =
            missingClient.PollRegistrationEvent().Message!;
        SipRegistrationSnapshot missing =
            missingClient.ReceiveRegistrationResponse(
                Challenge(missingRequest));
        SipRegistrationEvent missingFailure =
            missingClient.PollRegistrationEvent();

        SipRegistrationClient rejectingClient =
            new SipRegistrationClient();
        rejectingClient.ConfigureRegistrationAuthenticationClient(
            new RejectingAuthenticationClient());
        rejectingClient.RegisterContact(Request(false));
        SipMessage rejectingRequest =
            rejectingClient.PollRegistrationEvent().Message!;
        SipRegistrationSnapshot rejected =
            rejectingClient.ReceiveRegistrationResponse(
                Challenge(rejectingRequest));

        Assert.Multiple(() =>
        {
            Assert.That(missing.State,
                Is.EqualTo(SipRegistrationState.Failed));
            Assert.That(missing.ErrorMessage,
                Does.Contain("authentication client"));
            Assert.That(missingFailure.State,
                Is.EqualTo(SipRegistrationState.Failed));
            Assert.That(missingFailure.Message!.StatusCode,
                Is.EqualTo(SipStatusCodes.Unauthorized));
            Assert.That(missingClient.PollRegistrationEvent().Message,
                Is.Null);
            Assert.That(rejected.State,
                Is.EqualTo(SipRegistrationState.Failed));
            Assert.That(rejected.ErrorMessage,
                Is.EqualTo("credential unavailable"));
            Assert.That(rejected.AuthenticationAttemptCount, Is.Zero);
        });
    }

    [Test]
    public void MismatchedResponseDoesNotConsumeTheOutstandingRequest()
    {
        SipRegistrationClient client = new SipRegistrationClient();
        client.RegisterContact(Request(false));
        SipMessage register = client.PollRegistrationEvent().Message!;
        SipMessage mismatch = Success(register, 120);
        SetHeader(mismatch, SipHeaderNames.CallId, "other@example.com");

        Assert.Throws<ArgumentException>(() =>
            client.ReceiveRegistrationResponse(mismatch));
        SipRegistrationSnapshot pending =
            client.GetRegistrationSnapshot();
        SipRegistrationSnapshot accepted =
            client.ReceiveRegistrationResponse(Success(register, 120));

        Assert.Multiple(() =>
        {
            Assert.That(pending.State,
                Is.EqualTo(SipRegistrationState.Registering));
            Assert.That(pending.CSeq,
                Is.EqualTo(register.CSeq!.SequenceNumber));
            Assert.That(accepted.State,
                Is.EqualTo(SipRegistrationState.Registered));
        });
    }

    [Test]
    public void IntervalFailureAndShutdownValidationAreDeterministic()
    {
        SipRegistrationClient intervalClient =
            new SipRegistrationClient();
        intervalClient.RegisterContact(Request(false));
        SipMessage register =
            intervalClient.PollRegistrationEvent().Message!;
        SipRegistrationSnapshot interval =
            intervalClient.ReceiveRegistrationResponse(Response(register,
                SipStatusCodes.IntervalTooBrief, ""));

        SipRegistrationClient shutdownClient =
            new SipRegistrationClient();
        shutdownClient.RegisterContact(Request(false));
        shutdownClient.PollRegistrationEvent();
        Assert.Throws<ArgumentException>(() =>
            shutdownClient.ShutdownRegistration(
                new SipCancellation()));
        SipCancellation stop = new SipCancellation();
        stop.Requested = true;
        stop.Reason = "done";
        shutdownClient.ShutdownRegistration(stop);
        shutdownClient.PollRegistrationEvent();
        shutdownClient.ShutdownRegistration(stop);

        Assert.Multiple(() =>
        {
            Assert.That(interval.State,
                Is.EqualTo(SipRegistrationState.Failed));
            Assert.That(interval.ErrorMessage,
                Does.Contain("Min-Expires"));
            Assert.That(shutdownClient.GetRegistrationSnapshot().State,
                Is.EqualTo(SipRegistrationState.Terminated));
            Assert.That(shutdownClient.PollRegistrationEvent().State,
                Is.EqualTo(SipRegistrationState.None));
        });
    }

    static SipRegistrationClient RegisteredClient()
    {
        SipRegistrationClient client = new SipRegistrationClient();
        client.RegisterContact(Request(false));
        SipMessage register = client.PollRegistrationEvent().Message!;
        client.ReceiveRegistrationResponse(Success(register, 120));
        client.PollRegistrationEvent();
        return client;
    }

    static SipRegistrationRequest Request(bool secure)
    {
        string scheme = secure ? "sips" : "sip";
        SipContactValue contact = new SipContactValue();
        SipNameAddress address = new SipNameAddress();
        address.Uri = Uri(scheme
            + ":alice@device.example.com;transport="
            + (secure ? "tls" : "tcp"));
        contact.Address = address;
        SipRegistrationRequest request = new SipRegistrationRequest();
        request.AddressOfRecord = Uri(
            scheme + ":alice@example.com");
        request.Target = Uri(
            scheme + ":registrar.example.com");
        request.Contacts = [contact];
        request.Options.ExpiresSeconds = 120;
        request.Options.RefreshBeforeSeconds = 20;
        request.Options.RefreshJitterSeconds = 0;
        request.Options.RetryDelaySeconds = 5;
        return request;
    }

    static SipMessage Success(SipMessage request, int expires)
    {
        return Response(request, SipStatusCodes.Ok,
            "Contact: " + Header(request, SipHeaderNames.Contact)
                + ";expires=" + expires + "\r\n");
    }

    static SipMessage Challenge(SipMessage request)
    {
        return Response(request, SipStatusCodes.Unauthorized,
            "WWW-Authenticate: Digest realm=\"example.com\", "
                + "nonce=\"one\", algorithm=SHA-256, qop=\"auth\"\r\n");
    }

    static SipMessage Response(
        SipMessage request, int status, string extraHeaders)
    {
        string text = "SIP/2.0 " + status + " Result\r\n"
            + "Via: " + Header(request, SipHeaderNames.Via) + "\r\n"
            + "From: " + Header(request, SipHeaderNames.From) + "\r\n"
            + "To: " + Header(request, SipHeaderNames.To) + "\r\n"
            + "Call-ID: " + Header(request, SipHeaderNames.CallId) + "\r\n"
            + "CSeq: " + Header(request, SipHeaderNames.CSeq) + "\r\n"
            + extraHeaders + "Content-Length: 0\r\n\r\n";
        SipParseResult parsed = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text),
            new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Message!;
    }

    static SipUri Uri(string text)
    {
        SipParseResult parsed = new SipUriCodec().ParseUri(
            System.Text.Encoding.ASCII.GetBytes(text),
            new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Uri!;
    }

    static string? Header(SipMessage message, string name)
    {
        SipHeader? header = message.Headers.FirstOrDefault(value =>
            string.Equals(value.Name, name,
                StringComparison.OrdinalIgnoreCase));
        return header == null ? null : header.Value;
    }

    static void SetHeader(
        SipMessage message, string name, string value)
    {
        SipHeader header = message.Headers.First(candidate =>
            string.Equals(candidate.Name, name,
                StringComparison.OrdinalIgnoreCase));
        header.Value = value;
    }

    class RejectingAuthenticationClient : ISipAuthenticationClient
    {
        public SipAuthenticationResult AuthorizeRequest(
            SipAuthenticationRequest request)
        {
            SipAuthenticationResult result =
                new SipAuthenticationResult();
            result.Status = SipAuthenticationStatus.Failed;
            result.ErrorMessage = "credential unavailable";
            return result;
        }

        public SipAuthenticationResult VerifyAuthenticationInfo(
            SipAuthenticationRequest request)
        {
            return new SipAuthenticationResult();
        }
    }
}
