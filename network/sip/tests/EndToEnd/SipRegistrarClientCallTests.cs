namespace sip.Tests;

public class SipRegistrarClientCallTests
{
    const string Instance =
        "<urn:uuid:00000000-0000-1000-8000-000000001163>";

    [TestCase(SipDigestAlgorithm.Md5)]
    [TestCase(SipDigestAlgorithm.Sha256)]
    [TestCase(SipDigestAlgorithm.Sha512256)]
    public void AuthenticatedOutboundRegistrationRoundTripsFeatures(
        SipDigestAlgorithm algorithm)
    {
        using SipInProcessHarness harness = new SipInProcessHarness();
        EndToEndCredentialProvider provider = CredentialProvider();
        SipRegistrationClient client = new SipRegistrationClient();
        client.ConfigureRegistrationAuthenticationClient(
            new SipAuthenticationClient(provider));
        SipRegistrar registrar = AuthenticatedRegistrar(provider);
        SipRegistrationRequest request = OutboundRequest(1);

        client.RegisterContact(request);
        SipRegistrationEvent started = client.PollRegistrationEvent();
        SipMessage initial = DeliverRequest(harness, started.Message!,
            "register-initial", true);
        SipRegistrationSnapshot challenge = registrar.ProcessRegistration(
            initial, RegistrarOptions(true));
        KeepChallenge(challenge.Response!, algorithm);
        SipMessage receivedChallenge = DeliverResponse(harness,
            challenge.Response!, "register-challenge");

        SipRegistrationSnapshot retrying =
            client.ReceiveRegistrationResponse(receivedChallenge);
        SipRegistrationEvent retry = client.PollRegistrationEvent();
        SipMessage authorized = DeliverRequest(harness, retry.Message!,
            "register-authorized", true);
        SipDigestAuthorization authorization = Authorization(authorized);
        SipRegistrationSnapshot registered = registrar.ProcessRegistration(
            authorized, RegistrarOptions(true));
        AppendHeader(registered.Response!, SipHeaderNames.ServiceRoute,
            "<sip:service.example.com;lr>");
        SipMessage success = DeliverResponse(harness, registered.Response!,
            "register-success");
        SipRegistrationSnapshot active =
            client.ReceiveRegistrationResponse(success);
        SipRegistrationEvent clientEvent = client.PollRegistrationEvent();
        SipRegistrationEvent registrarEvent = registrar.PollRegistrarEvent();

        Assert.Multiple(() =>
        {
            Assert.That(started.CorrelationId, Is.EqualTo("registration-e2e"));
            Assert.That(retrying.AuthenticationAttemptCount, Is.EqualTo(1));
            Assert.That(authorization.Algorithm, Is.EqualTo(algorithm));
            Assert.That(active.State, Is.EqualTo(SipRegistrationState.Registered));
            Assert.That(active.ServiceRoute, Has.Length.EqualTo(1));
            Assert.That(active.ServiceRoute[0].Address!.Uri!.Host,
                Is.EqualTo("service.example.com"));
            Assert.That(active.ActiveFlow!.RegistrationId, Is.EqualTo(1));
            Assert.That(active.Bindings[0].Contact!.Outbound, Is.True);
            Assert.That(active.Bindings[0].Contact!.PublicGruu,
                Does.StartWith("sip:alice@example.com;gr="));
            Assert.That(active.Bindings[0].Contact!.TemporaryGruu,
                Does.Contain(";gr"));
            Assert.That(active.Bindings[0].Contact!.PushProvider,
                Is.EqualTo("apns"));
            Assert.That(active.Bindings[0].Contact!.PushParameter,
                Is.EqualTo("com.example.sip"));
            Assert.That(active.Bindings[0].Contact!.PushResourceId,
                Is.EqualTo("device-token-1"));
            Assert.That(active.Bindings[0].Contact!.PushRegistrationReference,
                Is.EqualTo("push-reference-1"));
            Assert.That(registered.Bindings[0].Path, Has.Length.EqualTo(1));
            Assert.That(registered.Bindings[0].Path[0].Address!.Uri!.Host,
                Is.EqualTo("edge.example.com"));
            Assert.That(clientEvent.State,
                Is.EqualTo(SipRegistrationState.Registered));
            Assert.That(clientEvent.CorrelationId,
                Is.EqualTo("registration-e2e"));
            Assert.That(registrarEvent.ContactState, Is.EqualTo("active"));
            Assert.That(registrarEvent.RegistrationState, Is.EqualTo("active"));
            Assert.That(registrarEvent.DocumentState, Is.EqualTo("full"));
            Assert.That(registrarEvent.PushChanged, Is.True);
        });
    }

    [Test]
    public void LearnedServiceRouteIsUsedForRefreshAndUnregister()
    {
        using SipInProcessHarness harness = new SipInProcessHarness();
        SipRegistrationClient client = new SipRegistrationClient();
        SipRegistrar registrar = new SipRegistrar();
        SipRegistrarOptions options = RegistrarOptions(false);
        SipRegistrationRequest request = BasicRequest(1);

        client.RegisterContact(request);
        SipMessage initial = DeliverRequest(harness,
            client.PollRegistrationEvent().Message!, "lifecycle-register", true);
        SipRegistrationSnapshot registered = registrar.ProcessRegistration(
            initial, options);
        Assert.That(registered.Response!.StatusCode,
            Is.EqualTo(SipStatusCodes.Ok), registered.ErrorMessage);
        AppendHeader(registered.Response!, SipHeaderNames.ServiceRoute,
            "<sip:service.example.com;lr>");
        SipRegistrationSnapshot active = client.ReceiveRegistrationResponse(
            DeliverResponse(harness, registered.Response!,
                "lifecycle-registered"));
        client.PollRegistrationEvent();
        SipRegistrationEvent registeredEvent = registrar.PollRegistrarEvent();

        client.AdvanceRegistrationTime(active.RefreshAtMilliseconds);
        SipRegistrationEvent refreshEvent = client.PollRegistrationEvent();
        SipMessage refresh = DeliverRequest(harness, refreshEvent.Message!,
            "lifecycle-refresh", true);
        SipRegistrationSnapshot refreshed = registrar.ProcessRegistration(
            refresh, options);
        SipRegistrationSnapshot refreshedClient =
            client.ReceiveRegistrationResponse(DeliverResponse(harness,
                refreshed.Response!, "lifecycle-refreshed"));
        client.PollRegistrationEvent();
        SipRegistrationEvent registrarRefresh = registrar.PollRegistrarEvent();

        SipRegistrationRequest remove = BasicRequest(1);
        remove.ServiceRoute = refreshedClient.ServiceRoute;
        client.UnregisterContact(remove);
        SipRegistrationEvent removing = client.PollRegistrationEvent();
        SipMessage unregister = DeliverRequest(harness, removing.Message!,
            "lifecycle-unregister", true);
        SipRegistrationSnapshot removed = registrar.ProcessRegistration(
            unregister, options);
        SipRegistrationSnapshot terminated =
            client.ReceiveRegistrationResponse(DeliverResponse(harness,
                removed.Response!, "lifecycle-unregistered"));
        SipRegistrationEvent clientTerminated = client.PollRegistrationEvent();
        SipRegistrationEvent registrarTerminated = registrar.PollRegistrarEvent();

        Assert.Multiple(() =>
        {
            Assert.That(refresh.RouteValues, Has.Length.EqualTo(1));
            Assert.That(refresh.RouteValues[0].Address!.Uri!.Host,
                Is.EqualTo("service.example.com"));
            Assert.That(refreshEvent.State,
                Is.EqualTo(SipRegistrationState.Refreshing));
            Assert.That(refreshedClient.State,
                Is.EqualTo(SipRegistrationState.Registered));
            Assert.That(registrarRefresh.ContactState, Is.EqualTo("active"));
            Assert.That(registrarRefresh.Version,
                Is.GreaterThan(registeredEvent.Version));
            Assert.That(unregister.RouteValues, Has.Length.EqualTo(1));
            Assert.That(unregister.ContactValues[0].ExpiresSeconds,
                Is.EqualTo(0));
            Assert.That(removed.Bindings, Is.Empty);
            Assert.That(terminated.State,
                Is.EqualTo(SipRegistrationState.Terminated));
            Assert.That(clientTerminated.State,
                Is.EqualTo(SipRegistrationState.Terminated));
            Assert.That(registrarTerminated.ContactState,
                Is.EqualTo("terminated"));
            Assert.That(registrarTerminated.RegistrationState,
                Is.EqualTo("terminated"));
        });
    }

    [Test]
    public void RegistrationExpiryTerminatesClientAndRegistrarEvents()
    {
        using SipInProcessHarness harness = new SipInProcessHarness();
        SipRegistrationClient client = new SipRegistrationClient();
        SipRegistrar registrar = new SipRegistrar();
        SipRegistrationRequest request = BasicRequest(1);
        request.Options.ExpiresSeconds = 60;
        request.Options.RefreshBeforeSeconds = 10;

        client.RegisterContact(request);
        SipMessage initial = DeliverRequest(harness,
            client.PollRegistrationEvent().Message!, "expiry-register", false);
        SipRegistrationSnapshot registered = registrar.ProcessRegistration(
            initial, RegistrarOptions(false));
        SipRegistrationSnapshot active = client.ReceiveRegistrationResponse(
            DeliverResponse(harness, registered.Response!, "expiry-success"));
        client.PollRegistrationEvent();
        registrar.PollRegistrarEvent();

        client.AdvanceRegistrationTime(active.ExpiresAtMilliseconds);
        registrar.ExpireRegistrarBindings(
            registered.Bindings[0].ExpiresAtMilliseconds);
        SipRegistrationEvent clientExpired = client.PollRegistrationEvent();
        SipRegistrationEvent registrarExpired = registrar.PollRegistrarEvent();

        Assert.Multiple(() =>
        {
            Assert.That(clientExpired.State,
                Is.EqualTo(SipRegistrationState.Terminated));
            Assert.That(clientExpired.Reason, Does.Contain("expired"));
            Assert.That(clientExpired.Snapshot!.Bindings, Is.Empty);
            Assert.That(registrarExpired.State,
                Is.EqualTo(SipRegistrationState.Terminated));
            Assert.That(registrarExpired.ContactState,
                Is.EqualTo("terminated"));
            Assert.That(registrarExpired.RegistrationState,
                Is.EqualTo("terminated"));
            Assert.That(registrarExpired.Reason, Is.EqualTo("expired"));
        });
    }

    [Test]
    public void PrimaryOutboundFlowFailureRefreshesOverBackupFlow()
    {
        using SipInProcessHarness harness = new SipInProcessHarness();
        SipRegistrationClient client = new SipRegistrationClient();
        SipRegistrar registrar = new SipRegistrar();
        SipRegistrarOptions options = RegistrarOptions(false);
        SipRegistrationRequest request = OutboundRequest(2);

        client.RegisterContact(request);
        SipMessage initial = DeliverRequest(harness,
            client.PollRegistrationEvent().Message!, "failover-register", true);
        initial.ContactValues[1].ExpiresSeconds = 0;
        SipRegistrationSnapshot registered = registrar.ProcessRegistration(
            initial, options);
        Assert.That(registered.Response!.StatusCode,
            Is.EqualTo(SipStatusCodes.Ok), registered.ErrorMessage);
        AppendHeader(registered.Response!, SipHeaderNames.ServiceRoute,
            "<sip:service.example.com;lr>");
        SipRegistrationSnapshot active = client.ReceiveRegistrationResponse(
            DeliverResponse(harness, registered.Response!, "failover-success"));
        Assert.That(active.State, Is.EqualTo(SipRegistrationState.Registered),
            active.ErrorMessage);
        client.PollRegistrationEvent();
        registrar.PollRegistrarEvent();
        registrar.PollRegistrarEvent();

        SipTransportEvent failure = new SipTransportEvent();
        failure.Kind = SipTransportEventKind.FlowFailed;
        failure.Flow = active.ActiveFlow;
        failure.ErrorMessage = "primary connection failed";
        SipRegistrationSnapshot failingOver =
            client.ReportRegistrationTransportFailure(failure);
        SipRegistrationEvent failoverEvent = client.PollRegistrationEvent();
        SipMessage refresh = DeliverRequest(harness, failoverEvent.Message!,
            "failover-refresh", true);
        refresh.ContactValues[1].ExpiresSeconds = 0;
        SipRegistrationSnapshot refreshed = registrar.ProcessRegistration(
            refresh, options);
        SipRegistrationSnapshot recovered = client.ReceiveRegistrationResponse(
            DeliverResponse(harness, refreshed.Response!, "failover-recovered"));
        SipRegistrationEvent recoveredEvent = client.PollRegistrationEvent();

        Assert.Multiple(() =>
        {
            Assert.That(failingOver.State,
                Is.EqualTo(SipRegistrationState.Refreshing));
            Assert.That(failingOver.ActiveFlow!.RegistrationId, Is.EqualTo(2));
            Assert.That(failingOver.Flows.Single(value =>
                value.RegistrationId == 1).Failed, Is.True);
            Assert.That(failoverEvent.Reason, Does.Contain("backup"));
            Assert.That(refresh.RouteValues[0].Address!.Uri!.Host,
                Is.EqualTo("service.example.com"));
            Assert.That(recovered.State,
                Is.EqualTo(SipRegistrationState.Registered));
            Assert.That(recovered.ActiveFlow!.RegistrationId, Is.EqualTo(2));
            Assert.That(recoveredEvent.State,
                Is.EqualTo(SipRegistrationState.Registered));
        });
    }

    static SipRegistrar AuthenticatedRegistrar(
        EndToEndCredentialProvider provider)
    {
        SipRegistrar registrar = new SipRegistrar();
        SipNonceOptions nonce = new SipNonceOptions();
        nonce.Realm = "example.com";
        nonce.CurrentKey = Enumerable.Range(1, 32)
            .Select(value => (byte)value).ToArray();
        registrar.ConfigureRegistrarAuthenticationServer(
            new SipAuthenticationServer(provider), nonce);
        return registrar;
    }

    static EndToEndCredentialProvider CredentialProvider()
    {
        EndToEndCredentialProvider provider =
            new EndToEndCredentialProvider();
        provider.Credential.UserName = "alice";
        provider.Credential.Secret =
            System.Text.Encoding.UTF8.GetBytes("correct horse battery staple");
        provider.Credential.Realm = "example.com";
        provider.Credential.Scope = SipCredentialScope.Origin;
        return provider;
    }

    static SipRegistrarOptions RegistrarOptions(bool authentication)
    {
        SipRegistrarOptions options = new SipRegistrarOptions();
        options.RequireAuthentication = authentication;
        options.Domains = new string[] { "example.com" };
        options.DefaultExpiresSeconds = 120;
        options.MinExpiresSeconds = 60;
        options.MaxExpiresSeconds = 600;
        return options;
    }

    static SipRegistrationRequest BasicRequest(int contactCount)
    {
        SipRegistrationRequest request = new SipRegistrationRequest();
        request.AddressOfRecord = Uri("sip:alice@example.com");
        request.Target = Uri("sip:example.com");
        request.CorrelationId = "registration-e2e";
        request.Options.ExpiresSeconds = 120;
        request.Options.RefreshBeforeSeconds = 20;
        request.Options.RefreshJitterSeconds = 0;
        request.Contacts = new SipContactValue[contactCount];
        int index = 0;
        while (index < contactCount)
        {
            SipContactValue contact = new SipContactValue();
            SipNameAddress address = new SipNameAddress();
            address.Uri = Uri("sip:alice@device-" + (index + 1).ToString()
                + ".example.com");
            contact.Address = address;
            request.Contacts[index] = contact;
            index++;
        }
        return request;
    }

    static SipRegistrationRequest OutboundRequest(int contactCount)
    {
        SipRegistrationRequest request = BasicRequest(contactCount);
        request.Options.UseOutbound = true;
        request.Options.UseGruu = true;
        request.Flows = new SipFlow[contactCount];
        int index = 0;
        while (index < contactCount)
        {
            int registrationId = index + 1;
            SipContactValue contact = request.Contacts[index];
            contact.InstanceId = Instance;
            contact.RegistrationId = registrationId;
            contact.Outbound = true;
            contact.PushProvider = "apns";
            contact.PushParameter = "com.example.sip";
            contact.PushResourceId = "device-token-" + registrationId.ToString();
            contact.PushRegistrationReference =
                "push-reference-" + registrationId.ToString();
            request.Flows[index] = Flow(registrationId, index == 0);
            index++;
        }
        return request;
    }

    static SipFlow Flow(int registrationId, bool primary)
    {
        SipFlow flow = new SipFlow();
        flow.InstanceId = Instance;
        flow.RegistrationId = registrationId;
        flow.Primary = primary;
        flow.Connection = new SipConnectionInfo();
        flow.Connection.Id = "flow-connection-" + registrationId.ToString();
        flow.Connection.State = SipConnectionState.Connected;
        flow.Connection.Transport = SipTransportKind.Tcp;
        flow.Connection.LocalAddress = Endpoint(
            "127.0.0.1", 5100 + registrationId);
        flow.Connection.RemoteAddress = Endpoint("192.0.2.20", 5060);
        return flow;
    }

    static SipEndpointAddress Endpoint(string host, int port)
    {
        SipEndpointAddress endpoint = new SipEndpointAddress();
        endpoint.Host = host;
        endpoint.Port = port;
        endpoint.Transport = SipTransportKind.Tcp;
        return endpoint;
    }

    static SipMessage DeliverRequest(SipInProcessHarness harness,
        SipMessage message, string correlationId, bool addPath)
    {
        if (addPath)
        {
            AppendHeader(message, SipHeaderNames.Path,
                "<sip:edge.example.com;lr;ob>");
        }
        SipSendResult sent = harness.SendFromUac(
            message, SipTransportKind.Tcp, correlationId);
        Assert.That(sent.Success, Is.True, sent.ErrorMessage);
        SipInProcessHarness.Drain(harness.Uac);
        harness.DeliverPackets();
        List<SipStackEvent> received = SipInProcessHarness.Drain(harness.Uas);
        return received.First(value => value.Kind
            == SipStackEventKind.IncomingRequest
            && value.Message!.Method == SipMethod.Register).Message!;
    }

    static SipMessage DeliverResponse(SipInProcessHarness harness,
        SipMessage message, string correlationId)
    {
        SipSendResult sent = harness.SendFromUas(
            message, SipTransportKind.Tcp, correlationId);
        Assert.That(sent.Success, Is.True, sent.ErrorMessage);
        SipInProcessHarness.Drain(harness.Uas);
        harness.DeliverPackets();
        List<SipStackEvent> received = SipInProcessHarness.Drain(harness.Uac);
        return received.First(value => value.Kind
            == SipStackEventKind.Transaction
            && value.TransactionEvent!.Message != null)
            .TransactionEvent!.Message!;
    }

    static void KeepChallenge(SipMessage response,
        SipDigestAlgorithm algorithm)
    {
        response.Headers = response.Headers.Where(header =>
            !string.Equals(header.Name, SipHeaderNames.WwwAuthenticate,
                StringComparison.OrdinalIgnoreCase)
            || Challenge(header).Algorithm == algorithm).ToArray();
        Assert.That(response.Headers.Count(header => string.Equals(
            header.Name, SipHeaderNames.WwwAuthenticate,
            StringComparison.OrdinalIgnoreCase)), Is.EqualTo(1));
    }

    static SipDigestChallenge Challenge(SipHeader header)
    {
        SipHeaderParseResult parsed = new SipHeaderCodec().ParseHeader(
            System.Text.Encoding.ASCII.GetBytes(header.Name + ": "
                + header.Value + "\r\n"), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.DigestChallenge!;
    }

    static SipDigestAuthorization Authorization(SipMessage message)
    {
        SipHeader header = message.Headers.First(value => string.Equals(
            value.Name, SipHeaderNames.Authorization,
            StringComparison.OrdinalIgnoreCase));
        SipHeaderParseResult parsed = new SipHeaderCodec().ParseHeader(
            System.Text.Encoding.ASCII.GetBytes(header.Name + ": "
                + header.Value + "\r\n"), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.DigestAuthorization!;
    }

    static void AppendHeader(SipMessage message, string name, string value)
    {
        SipHeader header = new SipHeader();
        header.Name = name;
        header.Value = value;
        message.Headers = message.Headers.Concat(
            new SipHeader[] { header }).ToArray();
    }

    static SipUri Uri(string text)
    {
        SipParseResult parsed = new SipUriCodec().ParseUri(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Uri!;
    }
}

class EndToEndCredentialProvider : ISipCredentialProvider
{
    internal SipCredential Credential = new SipCredential();

    public SipCredential GetCredential(SipAuthenticationRequest request)
    {
        return Credential;
    }
}
