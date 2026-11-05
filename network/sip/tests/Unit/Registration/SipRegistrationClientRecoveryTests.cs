namespace sip.Tests;

public class SipRegistrationClientRecoveryTests
{
    const string Instance =
        "<urn:uuid:00000000-0000-1000-8000-000000000123>";

    [Test]
    public void WildcardUnregisterRemovesEveryBinding()
    {
        SipRegistrationClient client = new SipRegistrationClient();
        SipRegistrationRequest command = RequestWithContacts(2);
        client.RegisterContact(command);
        SipMessage register = client.PollRegistrationEvent().Message!;
        client.ReceiveRegistrationResponse(Success(register));
        client.PollRegistrationEvent();

        SipRegistrationRequest remove = BasicRequest();
        remove.UnregisterAll = true;
        remove.Contacts = Array.Empty<SipContactValue>();
        SipRegistrationSnapshot pending = client.UnregisterContact(remove);
        SipMessage unregister = client.PollRegistrationEvent().Message!;
        SipRegistrationSnapshot completed = client.ReceiveRegistrationResponse(
            Response(unregister, 200, ""));

        Assert.Multiple(() =>
        {
            Assert.That(pending.State,
                Is.EqualTo(SipRegistrationState.Unregistering));
            Assert.That(unregister.ContactValues, Has.Length.EqualTo(1));
            Assert.That(unregister.ContactValues[0].Wildcard, Is.True);
            Assert.That(Header(unregister, SipHeaderNames.Contact), Is.EqualTo("*"));
            Assert.That(Header(unregister, SipHeaderNames.Expires), Is.EqualTo("0"));
            Assert.That(completed.State,
                Is.EqualTo(SipRegistrationState.Terminated));
            Assert.That(completed.Bindings, Is.Empty);
        });
    }

    [Test]
    public void PerContactUnregisterKeepsUnselectedBindingActive()
    {
        SipRegistrationClient client = new SipRegistrationClient();
        SipRegistrationRequest command = RequestWithContacts(2);
        client.RegisterContact(command);
        SipMessage register = client.PollRegistrationEvent().Message!;
        SipRegistrationSnapshot active =
            client.ReceiveRegistrationResponse(Success(register));
        client.PollRegistrationEvent();

        SipRegistrationRequest remove = BasicRequest();
        remove.Contacts = new SipContactValue[]
        {
            CopyContact(command.Contacts[0])
        };
        client.UnregisterContact(remove);
        SipMessage unregister = client.PollRegistrationEvent().Message!;
        SipRegistrationSnapshot remaining = client.ReceiveRegistrationResponse(
            Response(unregister, 200, ""));

        Assert.Multiple(() =>
        {
            Assert.That(active.Bindings, Has.Length.EqualTo(2));
            Assert.That(unregister.ContactValues, Has.Length.EqualTo(1));
            Assert.That(unregister.ContactValues[0].ExpiresSeconds, Is.EqualTo(0));
            Assert.That(remaining.State,
                Is.EqualTo(SipRegistrationState.Registered));
            Assert.That(remaining.Bindings, Has.Length.EqualTo(1));
            Assert.That(remaining.Bindings[0].Contact!.Address!.Uri!.Host,
                Is.EqualTo("device-two.example.com"));
            Assert.That(remaining.RefreshAtMilliseconds, Is.GreaterThanOrEqualTo(0));
        });
    }

    [Test]
    public void ContactGenerationAndSuccessRoundTripPushAndGruuMetadata()
    {
        SipRegistrationClient client = new SipRegistrationClient();
        SipRegistrationRequest command = RequestWithContacts(1);
        SipContactValue contact = command.Contacts[0];
        contact.InstanceId = Instance;
        contact.RegistrationId = 7;
        contact.Outbound = true;
        contact.PushProvider = "apns";
        contact.PushParameter = "com.example.app";
        contact.PushResourceId = "device-token";
        contact.PushRegistrationReference = "registration-reference";
        client.RegisterContact(command);
        SipMessage register = client.PollRegistrationEvent().Message!;
        string contactText = Header(register, SipHeaderNames.Contact)!;
        string responseContact = contactText
            + ";pub-gruu=\"sip:alice@example.com;gr=public\""
            + ";temp-gruu=\"sip:temporary@example.com;gr=temp\"";

        SipRegistrationSnapshot active = client.ReceiveRegistrationResponse(
            Response(register, 200,
                "Contact: " + responseContact + "\r\nExpires: 120\r\n"));
        SipContactValue captured = active.Bindings[0].Contact!;

        Assert.Multiple(() =>
        {
            Assert.That(register.ContactValues[0].InstanceId, Is.EqualTo(Instance));
            Assert.That(register.ContactValues[0].RegistrationId, Is.EqualTo(7));
            Assert.That(register.ContactValues[0].Outbound, Is.True);
            Assert.That(register.ContactValues[0].PushProvider, Is.EqualTo("apns"));
            Assert.That(register.ContactValues[0].PushParameter,
                Is.EqualTo("com.example.app"));
            Assert.That(register.ContactValues[0].PushResourceId,
                Is.EqualTo("device-token"));
            Assert.That(register.ContactValues[0].PushRegistrationReference,
                Is.EqualTo("registration-reference"));
            Assert.That(captured.PublicGruu,
                Is.EqualTo("sip:alice@example.com;gr=public"));
            Assert.That(captured.TemporaryGruu,
                Is.EqualTo("sip:temporary@example.com;gr=temp"));
        });
    }

    [Test]
    public void PrimaryFlowFailureSelectsBackupAndCreatesRefresh()
    {
        SipRegistrationClient client = new SipRegistrationClient();
        SipRegistrationRequest command = OutboundRequest();
        client.RegisterContact(command);
        SipMessage register = client.PollRegistrationEvent().Message!;
        SipRegistrationSnapshot active =
            client.ReceiveRegistrationResponse(Success(register));
        client.PollRegistrationEvent();

        SipTransportEvent failure = new SipTransportEvent();
        failure.Kind = SipTransportEventKind.FlowFailed;
        failure.Flow = active.ActiveFlow;
        failure.ErrorMessage = "primary disconnected";
        SipRegistrationSnapshot failedOver =
            client.ReportRegistrationTransportFailure(failure);
        SipRegistrationEvent refresh = client.PollRegistrationEvent();

        Assert.Multiple(() =>
        {
            Assert.That(active.Flows, Has.Length.EqualTo(2));
            Assert.That(active.ActiveFlow!.RegistrationId, Is.EqualTo(1));
            Assert.That(failedOver.State,
                Is.EqualTo(SipRegistrationState.Refreshing));
            Assert.That(failedOver.ActiveFlow!.RegistrationId, Is.EqualTo(2));
            Assert.That(failedOver.Flows.Single(value =>
                value.RegistrationId == 1).Failed, Is.True);
            Assert.That(refresh.Message!.CSeq!.SequenceNumber,
                Is.EqualTo(active.CSeq + 1));
            Assert.That(refresh.Reason, Does.Contain("backup"));
        });
    }

    [Test]
    public void AllFlowFailureSignalsRecoveryAndReplacementRestartsRegistration()
    {
        SipRegistrationClient client = new SipRegistrationClient();
        SipRegistrationRequest command = OutboundRequest();
        client.RegisterContact(command);
        SipMessage register = client.PollRegistrationEvent().Message!;
        SipRegistrationSnapshot active =
            client.ReceiveRegistrationResponse(Success(register));
        client.PollRegistrationEvent();

        SipTransportEvent firstFailure = new SipTransportEvent();
        firstFailure.Kind = SipTransportEventKind.FlowFailed;
        firstFailure.Flow = active.ActiveFlow;
        SipRegistrationSnapshot backup =
            client.ReportRegistrationTransportFailure(firstFailure);
        client.PollRegistrationEvent();
        SipTransportEvent secondFailure = new SipTransportEvent();
        secondFailure.Kind = SipTransportEventKind.Disconnected;
        secondFailure.Flow = backup.ActiveFlow;
        SipRegistrationSnapshot failed =
            client.ReportRegistrationTransportFailure(secondFailure);
        SipRegistrationEvent failureEvent = client.PollRegistrationEvent();
        client.AdvanceRegistrationTime(failed.RetryAtMilliseconds);
        SipRegistrationEvent due = client.PollRegistrationEvent();

        SipFlow replacement = Flow(1, "connection-recovered", true);
        SipRegistrationSnapshot recovering =
            client.RecoverRegistrationFlow(replacement);
        SipRegistrationEvent retry = client.PollRegistrationEvent();

        Assert.Multiple(() =>
        {
            Assert.That(failed.State, Is.EqualTo(SipRegistrationState.Failed));
            Assert.That(failed.ActiveFlow, Is.Null);
            Assert.That(failed.RetryAtMilliseconds, Is.GreaterThan(0));
            Assert.That(failureEvent.Message, Is.Null);
            Assert.That(due.Message, Is.Null);
            Assert.That(due.Reason, Does.Contain("recovery is due"));
            Assert.That(recovering.State,
                Is.EqualTo(SipRegistrationState.Refreshing));
            Assert.That(recovering.ActiveFlow!.Connection!.Id,
                Is.EqualTo("connection-recovered"));
            Assert.That(retry.Message, Is.Not.Null);
            Assert.That(retry.Reason, Does.Contain("recovered"));
        });
    }

    [Test]
    public void EventsAndSnapshotQueuesRemainIndependentAndDetached()
    {
        SipRegistrationClient client = new SipRegistrationClient();
        SipRegistrationSnapshot returned =
            client.RegisterContact(RequestWithContacts(1));
        SipRegistrationEvent registrationEvent = client.PollRegistrationEvent();
        SipRegistrationSnapshot queued = client.PollRegistrationSnapshot();
        returned.AddressOfRecord!.Host = "changed.invalid";
        queued.AddressOfRecord!.Host = "also-changed.invalid";
        SipRegistrationSnapshot current = client.GetRegistrationSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(registrationEvent.Snapshot, Is.Not.SameAs(queued));
            Assert.That(current.AddressOfRecord!.Host, Is.EqualTo("example.com"));
            Assert.That(client.PollRegistrationSnapshot().State,
                Is.EqualTo(SipRegistrationState.None));
        });
    }

    static SipRegistrationRequest OutboundRequest()
    {
        SipRegistrationRequest request = RequestWithContacts(2);
        request.Options.UseOutbound = true;
        request.Contacts[0].InstanceId = Instance;
        request.Contacts[0].RegistrationId = 1;
        request.Contacts[0].Outbound = true;
        request.Contacts[1].InstanceId = Instance;
        request.Contacts[1].RegistrationId = 2;
        request.Contacts[1].Outbound = true;
        request.Flows = new SipFlow[]
        {
            Flow(1, "connection-primary", true),
            Flow(2, "connection-backup", false)
        };
        return request;
    }

    static SipFlow Flow(int registrationId, string connectionId, bool primary)
    {
        SipFlow flow = new SipFlow();
        flow.InstanceId = Instance;
        flow.RegistrationId = registrationId;
        flow.Connection = Connection(connectionId, 5000 + registrationId);
        flow.Primary = primary;
        return flow;
    }

    static SipConnectionInfo Connection(string id, int port)
    {
        SipConnectionInfo connection = new SipConnectionInfo();
        connection.Id = id;
        connection.State = SipConnectionState.Connected;
        connection.Transport = SipTransportKind.Tcp;
        connection.LocalAddress = Endpoint("127.0.0.1", port);
        connection.RemoteAddress = Endpoint("192.0.2.10", 5060);
        return connection;
    }

    static SipEndpointAddress Endpoint(string host, int port)
    {
        SipEndpointAddress endpoint = new SipEndpointAddress();
        endpoint.Host = host;
        endpoint.Port = port;
        endpoint.Transport = SipTransportKind.Tcp;
        return endpoint;
    }

    static SipRegistrationRequest RequestWithContacts(int count)
    {
        SipRegistrationRequest request = BasicRequest();
        SipContactValue[] contacts = new SipContactValue[count];
        int index = 0;
        while (index < count)
        {
            SipContactValue contact = new SipContactValue();
            SipNameAddress address = new SipNameAddress();
            address.Uri = Uri(index == 0
                ? "sip:alice@device-one.example.com"
                : "sip:alice@device-two.example.com");
            contact.Address = address;
            contacts[index] = contact;
            index = index + 1;
        }
        request.Contacts = contacts;
        return request;
    }

    static SipRegistrationRequest BasicRequest()
    {
        SipRegistrationRequest request = new SipRegistrationRequest();
        request.AddressOfRecord = Uri("sip:alice@example.com");
        request.Target = Uri("sip:registrar.example.com");
        request.Options.ExpiresSeconds = 120;
        request.Options.RefreshBeforeSeconds = 20;
        request.Options.RefreshJitterSeconds = 0;
        return request;
    }

    static SipContactValue CopyContact(SipContactValue source)
    {
        SipContactValue copy = new SipContactValue();
        copy.Address = new SipNameAddress();
        copy.Address.Uri = Uri("sip:alice@" + source.Address!.Uri!.Host);
        copy.InstanceId = source.InstanceId;
        copy.RegistrationId = source.RegistrationId;
        return copy;
    }

    static SipMessage Success(SipMessage request)
    {
        return Response(request, 200,
            "Contact: " + Header(request, SipHeaderNames.Contact)
                + "\r\nExpires: 120\r\n");
    }

    static SipMessage Response(SipMessage request, int status, string extraHeaders)
    {
        string text = "SIP/2.0 " + status + " Result\r\n"
            + "Via: " + Header(request, SipHeaderNames.Via) + "\r\n"
            + "From: " + Header(request, SipHeaderNames.From) + "\r\n"
            + "To: " + Header(request, SipHeaderNames.To) + "\r\n"
            + "Call-ID: " + Header(request, SipHeaderNames.CallId) + "\r\n"
            + "CSeq: " + Header(request, SipHeaderNames.CSeq) + "\r\n"
            + extraHeaders + "Content-Length: 0\r\n\r\n";
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
