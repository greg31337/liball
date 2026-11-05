namespace sip.Tests;

public class SipRegistrarEventTests
{
    const string Instance =
        "<urn:uuid:00000000-0000-1000-8000-000000000018>";

    [Test]
    public void GeneratesVersionedRfc3680EventsForBindingLifecycle()
    {
        SipRegistrar registrar = new SipRegistrar();
        registrar.ExpireRegistrarBindings(1000);
        SipRegistrarOptions options = Options();
        string contact = Contact(120, "token-one");

        registrar.ProcessRegistration(Register("lifecycle-call", 1,
            contact), options);
        SipRegistrationEvent registered = registrar.PollRegistrarEvent();
        registrar.ProcessRegistration(Register("lifecycle-call", 2,
            Contact(180, "token-one")), options);
        SipRegistrationEvent refreshed = registrar.PollRegistrarEvent();
        registrar.ProcessRegistration(Register("lifecycle-call", 3,
            Contact(60, "token-one")), options);
        SipRegistrationEvent shortened = registrar.PollRegistrarEvent();
        registrar.ProcessRegistration(Register("lifecycle-call", 3,
            Contact(60, "token-one")), options);
        SipRegistrationEvent noRetransmissionEvent =
            registrar.PollRegistrarEvent();
        registrar.ProcessRegistration(Register("lifecycle-call", 4,
            Contact(0, "token-one")), options);
        SipRegistrationEvent unregistered = registrar.PollRegistrarEvent();

        Assert.Multiple(() =>
        {
            Assert.That(registered.Version, Is.EqualTo(0));
            Assert.That(registered.DocumentState, Is.EqualTo("full"));
            Assert.That(registered.RegistrationState, Is.EqualTo("active"));
            Assert.That(registered.ContactState, Is.EqualTo("active"));
            Assert.That(registered.Reason, Is.EqualTo("registered"));
            Assert.That(registered.State,
                Is.EqualTo(SipRegistrationState.Registered));
            Assert.That(registered.Binding!.Contact!.PushResourceId,
                Is.EqualTo("token-one"));
            Assert.That(registered.PushChanged, Is.True);
            Assert.That(refreshed.Version, Is.EqualTo(1));
            Assert.That(refreshed.DocumentState, Is.EqualTo("partial"));
            Assert.That(refreshed.Reason, Is.EqualTo("refreshed"));
            Assert.That(refreshed.PushChanged, Is.False);
            Assert.That(shortened.Version, Is.EqualTo(2));
            Assert.That(shortened.Reason, Is.EqualTo("shortened"));
            Assert.That(noRetransmissionEvent.Version, Is.EqualTo(-1));
            Assert.That(unregistered.Version, Is.EqualTo(3));
            Assert.That(unregistered.Reason, Is.EqualTo("unregistered"));
            Assert.That(unregistered.RegistrationState,
                Is.EqualTo("terminated"));
            Assert.That(unregistered.ContactState,
                Is.EqualTo("terminated"));
            Assert.That(unregistered.State,
                Is.EqualTo(SipRegistrationState.Terminated));
            Assert.That(unregistered.Snapshot!.Bindings, Is.Empty);
        });
    }

    [Test]
    public void ExpiryRemovesBindingsAndGeneratesExpiredEventsAtDeadline()
    {
        SipLocationService location = new SipLocationService();
        SipRegistrar registrar = new SipRegistrar();
        registrar.ConfigureRegistrarLocationService(location);
        registrar.ExpireRegistrarBindings(5000);
        registrar.ProcessRegistration(Register("expiry-call", 1,
            Contact(60, null)), Options());
        registrar.PollRegistrarEvent();

        registrar.ExpireRegistrarBindings(64999);
        SipRegistrationEvent early = registrar.PollRegistrarEvent();
        registrar.ExpireRegistrarBindings(65000);
        SipRegistrationEvent expired = registrar.PollRegistrarEvent();
        SipLocationResult remaining = location.QueryLocation(
            Query("sip:alice@example.com"));

        Assert.Multiple(() =>
        {
            Assert.That(early.Version, Is.EqualTo(-1));
            Assert.That(expired.Version, Is.EqualTo(1));
            Assert.That(expired.Reason, Is.EqualTo("expired"));
            Assert.That(expired.ContactState, Is.EqualTo("terminated"));
            Assert.That(expired.RegistrationState, Is.EqualTo("terminated"));
            Assert.That(expired.Binding!.CallId, Is.EqualTo("expiry-call"));
            Assert.That(expired.Snapshot!.ExpiresAtMilliseconds,
                Is.EqualTo(-1));
            Assert.That(remaining.Bindings, Is.Empty);
        });
    }

    [Test]
    public void ReportsFlowAndPushStatusChangesWithoutReplacingBindingMetadata()
    {
        SipRegistrar registrar = new SipRegistrar();
        SipRegistrationSnapshot registered = registrar.ProcessRegistration(
            Register("status-call", 7, Contact(120, "original-token")),
            Options());
        registrar.PollRegistrarEvent();
        SipRegistrationBinding status = registered.Bindings[0];
        status.Flow = new SipFlow();
        status.Flow.Id = "flow-one";
        status.Flow.InstanceId = Instance;
        status.Flow.RegistrationId = 1;
        status.Flow.Failed = true;
        status.CallId = "caller-cannot-replace-this";
        status.CSeq = 99;

        SipRegistrationSnapshot flowSnapshot =
            registrar.UpdateRegistrarBindingStatus(status);
        SipRegistrationEvent flowEvent = registrar.PollRegistrarEvent();
        SipRegistrationSnapshot pushSnapshot =
            registrar.ProcessRegistration(Register("status-call", 8,
                Contact(120, "replacement-token")), Options());
        SipRegistrationEvent pushEvent = registrar.PollRegistrarEvent();

        Assert.Multiple(() =>
        {
            Assert.That(flowEvent.Version, Is.EqualTo(1));
            Assert.That(flowEvent.Reason, Is.EqualTo("refreshed"));
            Assert.That(flowEvent.FlowChanged, Is.True);
            Assert.That(flowEvent.PushChanged, Is.False);
            Assert.That(flowEvent.Binding!.Flow!.Failed, Is.True);
            Assert.That(flowSnapshot.Bindings[0].CallId,
                Is.EqualTo("status-call"));
            Assert.That(flowSnapshot.Bindings[0].CSeq, Is.EqualTo(7));
            Assert.That(pushEvent.Version, Is.EqualTo(2));
            Assert.That(pushEvent.FlowChanged, Is.False);
            Assert.That(pushEvent.PushChanged, Is.True);
            Assert.That(pushEvent.Binding!.Contact!.PushResourceId,
                Is.EqualTo("replacement-token"));
            Assert.That(pushSnapshot.Bindings[0].CallId,
                Is.EqualTo("status-call"));
            Assert.That(pushSnapshot.Bindings[0].CSeq, Is.EqualTo(8));
            Assert.That(pushSnapshot.Bindings[0].ExpiresAtMilliseconds,
                Is.EqualTo(120000));
            Assert.That(pushSnapshot.Bindings[0].Flow!.Failed, Is.True);
        });
    }

    [Test]
    public void PushRefreshUpdatesTheExistingContactBinding()
    {
        SipRegistrar registrar = new SipRegistrar();
        registrar.ProcessRegistration(Register("push-call", 1,
            PushContact("first-token")), Options());
        registrar.PollRegistrarEvent();

        SipRegistrationSnapshot refreshed = registrar.ProcessRegistration(
            Register("push-call", 2, PushContact("second-token")),
            Options());
        SipRegistrationEvent registrationEvent =
            registrar.PollRegistrarEvent();

        Assert.Multiple(() =>
        {
            Assert.That(refreshed.Bindings, Has.Length.EqualTo(1));
            Assert.That(refreshed.Bindings[0].Contact!.PushResourceId,
                Is.EqualTo("second-token"));
            Assert.That(registrationEvent.Reason, Is.EqualTo("refreshed"));
            Assert.That(registrationEvent.PushChanged, Is.True);
            Assert.That(registrationEvent.Binding!.CSeq, Is.EqualTo(2));
        });
    }

    static SipRegistrarOptions Options()
    {
        SipRegistrarOptions options = new SipRegistrarOptions();
        options.RequireAuthentication = false;
        options.DefaultExpiresSeconds = 120;
        options.MinExpiresSeconds = 60;
        options.MaxExpiresSeconds = 600;
        options.MaxBindingCount = 10;
        options.Domains = new string[] { "example.com" };
        return options;
    }

    static string Contact(int expiresSeconds, string? pushResourceId)
    {
        string push = pushResourceId == null ? ""
            : ";pn-provider=apns;pn-prid=" + pushResourceId;
        return "Contact: <sip:alice@device.example.com;ob" + push + ">"
            + ";expires=" + expiresSeconds.ToString()
            + ";reg-id=1;+sip.instance=\"" + Instance + "\"\r\n";
    }

    static string PushContact(string pushResourceId)
    {
        return "Contact: <sip:alice@device.example.com"
            + ";pn-provider=apns;pn-prid=" + pushResourceId + ">"
            + ";expires=120\r\n";
    }

    static SipMessage Register(string callId, int cseq, string contact)
    {
        string text = "REGISTER sip:example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/TCP client.example.com;branch=z9hG4bK-event\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: <sip:alice@example.com>\r\n"
            + "Call-ID: " + callId + "\r\n"
            + "CSeq: " + cseq.ToString() + " REGISTER\r\n"
            + "Supported: outbound\r\n"
            + contact
            + "Content-Length: 0\r\n\r\n";
        SipParseResult parsed = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Message!;
    }

    static SipLocationQuery Query(string addressOfRecord)
    {
        SipParseResult parsed = new SipUriCodec().ParseUri(
            System.Text.Encoding.ASCII.GetBytes(addressOfRecord),
            new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        SipLocationQuery query = new SipLocationQuery();
        query.AddressOfRecord = parsed.Uri;
        query.NowMilliseconds = -1;
        return query;
    }
}
