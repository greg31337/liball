namespace sip.Tests;

public class SipOutboundServiceTests
{
    const string Instance =
        "<urn:uuid:f81d4fae-7dec-11d0-a765-00a0c91e6bf6>";

    [Test]
    public void CreatesOutboundGruuAndPushRegistrationContact()
    {
        SipOutboundContactRequest request = ContactRequest();
        request.PushRefreshWithoutNotification = true;
        SipOutboundService service = new SipOutboundService();

        SipOutboundResult result =
            service.CreateRegistrationContact(request);
        request.ContactUri!.Host = "changed.invalid";

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Contact!.Address!.Uri!.Host,
                Is.EqualTo("device.example.com"));
            Assert.That(result.Contact.Outbound, Is.True);
            Assert.That(result.Contact.InstanceId, Is.EqualTo(Instance));
            Assert.That(result.Contact.RegistrationId, Is.EqualTo(2));
            Assert.That(result.Contact.PushProvider, Is.EqualTo("apns"));
            Assert.That(result.Contact.PushParameter,
                Is.EqualTo("TEAM.example.voip"));
            Assert.That(result.Contact.PushResourceId,
                Is.EqualTo("device-token"));
            Assert.That(Parameter(result.Contact.Address.Parameters,
                "+sip.pnsreg"), Is.Not.Null);
            Assert.That(result.SupportedOptionTags, Is.EqualTo(
                new string[] { "path", "outbound", "gruu" }));
        });
    }

    [Test]
    public void GeneratedContactIntegratesWithRegistrationClientOptions()
    {
        SipOutboundResult generated = new SipOutboundService()
            .CreateRegistrationContact(ContactRequest());
        SipRegistrationRequest request = new SipRegistrationRequest();
        request.AddressOfRecord = Uri("sip:alice@example.com");
        request.Target = Uri("sip:registrar.example.com");
        request.Contacts = new SipContactValue[] { generated.Contact! };
        request.Options.UseOutbound = true;
        request.Options.UseGruu = true;
        SipFlow flow = new SipFlow();
        flow.InstanceId = Instance;
        flow.RegistrationId = 2;
        flow.Connection = Connection("outbound-2");
        request.Flows = new SipFlow[] { flow };

        SipRegistrationClient client = new SipRegistrationClient();
        client.RegisterContact(request);
        SipMessage message = client.PollRegistrationEvent().Message!;

        Assert.Multiple(() =>
        {
            Assert.That(Header(message, SipHeaderNames.Supported),
                Is.EqualTo("path, outbound, gruu"));
            Assert.That(message.ContactValues[0].Outbound, Is.True);
            Assert.That(message.ContactValues[0].PushProvider,
                Is.EqualTo("apns"));
            Assert.That(message.ContactValues[0].PushResourceId,
                Is.EqualTo("device-token"));
        });
    }

    [Test]
    public void RejectsInconsistentOutboundGruuAndPushInputs()
    {
        SipOutboundService service = new SipOutboundService();
        SipOutboundContactRequest missingInstance = ContactRequest();
        missingInstance.InstanceId = null;
        SipOutboundContactRequest orphanResource = ContactRequest();
        orphanResource.PushProvider = null;
        SipOutboundContactRequest parameterWithoutResource = ContactRequest();
        parameterWithoutResource.PushResourceId = null;

        SipOutboundResult first =
            service.CreateRegistrationContact(missingInstance);
        SipOutboundResult second =
            service.CreateRegistrationContact(orphanResource);
        SipOutboundResult third =
            service.CreateRegistrationContact(parameterWithoutResource);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.False);
            Assert.That(first.StatusCode, Is.EqualTo(SipStatusCodes.BadRequest));
            Assert.That(second.Success, Is.False);
            Assert.That(third.Success, Is.False);
        });
    }

    [Test]
    public void DialogContactRemovesPrivateRegistrationParameters()
    {
        SipContactValue registration = Contact(
            "<sip:alice@device.example.com;transport=tcp;ob;"
            + "pn-provider=apns;pn-param=topic;pn-prid=token;pn-purr=opaque>"
            + ";expires=600;q=0.8;reg-id=2;+sip.instance=\"" + Instance + "\""
            + ";pub-gruu=\"sip:alice@example.com;gr=public\""
            + ";temp-gruu=\"sip:temp@example.com;gr\";+sip.pnsreg");

        SipOutboundResult result = new SipOutboundService()
            .CreateDialogContact(registration);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Contact!.InstanceId, Is.Null);
            Assert.That(result.Contact.RegistrationId, Is.EqualTo(-1));
            Assert.That(result.Contact.Outbound, Is.False);
            Assert.That(result.Contact.PushProvider, Is.Null);
            Assert.That(result.Contact.PushResourceId, Is.Null);
            Assert.That(result.Contact.PushRegistrationReference,
                Is.EqualTo("opaque"));
            Assert.That(Parameter(result.Contact.Address!.Uri!.Parameters,
                "transport"), Is.Not.Null);
            Assert.That(Parameter(result.Contact.Address.Uri.Parameters,
                "pn-prid"), Is.Null);
            Assert.That(Parameter(result.Contact.Address.Parameters,
                "+sip.instance"), Is.Null);
            Assert.That(registration.PushResourceId, Is.EqualTo("token"));
        });
    }

    [Test]
    public void SelectsPreferredGruuWithSecureFallbackRules()
    {
        SipContactValue contact = Contact(
            "<sips:alice@device.example.com>"
            + ";pub-gruu=\"sips:alice@example.com;gr=public\""
            + ";temp-gruu=\"sip:temporary@example.com;gr\"");
        SipOutboundService service = new SipOutboundService();

        SipOutboundResult temporary =
            service.SelectGruuTarget(contact, true, false);
        SipOutboundResult publicGruu =
            service.SelectGruuTarget(contact, false, true);
        SipOutboundResult insecure =
            service.SelectGruuTarget(contact, true, true);
        SipOutboundResult fallback = service.SelectGruuTarget(
            Contact("<sip:alice@plain.example.com>"), true, false);

        Assert.Multiple(() =>
        {
            Assert.That(temporary.Success, Is.True);
            Assert.That(temporary.GruuSelected, Is.True);
            Assert.That(temporary.TemporaryGruuSelected, Is.True);
            Assert.That(temporary.Uri!.User, Is.EqualTo("temporary"));
            Assert.That(publicGruu.Uri!.Scheme, Is.EqualTo(SipUriScheme.Sips));
            Assert.That(publicGruu.TemporaryGruuSelected, Is.False);
            Assert.That(insecure.Success, Is.False);
            Assert.That(insecure.StatusCode,
                Is.EqualTo(SipStatusCodes.UnsupportedUriScheme));
            Assert.That(fallback.GruuSelected, Is.False);
            Assert.That(fallback.Uri!.Host, Is.EqualTo("plain.example.com"));
        });
    }

    [Test]
    public void RoutesAddressedGruuOverActiveFlowAndPath()
    {
        SipRegistrationBinding binding = Binding(true);
        binding.Contact!.TemporaryGruu = "sip:opaque@example.com;gr";
        binding.Path = Routes("<sip:flow-token@edge.example.com;lr;ob>");
        binding.Flow = Flow(false);
        SipOutboundRouteRequest request = new SipOutboundRouteRequest();
        request.Bindings = new SipRegistrationBinding[] { binding };
        request.Destination = Uri("sip:opaque@example.com;gr");
        request.NowMilliseconds = 1000;

        SipOutboundResult result = new SipOutboundService()
            .SelectBindingRoute(request, null!);
        result.Target!.Uri!.Host = "changed.invalid";
        result.Target.RouteSet[0].Address!.Uri!.Host = "changed.invalid";

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Binding!.Contact!.Address!.Uri!.Host,
                Is.EqualTo("device.example.com"));
            Assert.That(result.Target.Flow!.Id, Is.EqualTo("flow-2"));
            Assert.That(result.Target.ExpiresSeconds, Is.EqualTo(99));
            Assert.That(binding.Contact.Address!.Uri!.Host,
                Is.EqualTo("device.example.com"));
            Assert.That(binding.Path[0].Address!.Uri!.Host,
                Is.EqualTo("edge.example.com"));
        });
    }

    [Test]
    public void DormantOutboundBindingUsesDetachedPushPolicy()
    {
        SipRegistrationBinding binding = Binding(true);
        binding.Flow = Flow(true);
        SipOutboundRouteRequest request = new SipOutboundRouteRequest();
        request.Bindings = new SipRegistrationBinding[] { binding };
        request.Destination = Uri("sip:alice@example.com");
        request.NowMilliseconds = 1000;
        request.AllowPush = true;
        request.CorrelationId = "push-1";
        RecordingPushPolicy policy = new RecordingPushPolicy();
        policy.Decision.Accepted = true;
        policy.Decision.CorrelationId = "provider-1";

        SipOutboundService service = new SipOutboundService();
        SipOutboundResult result =
            service.SelectBindingRoute(request, policy);
        SipOutboundResult unsupported =
            service.SelectBindingRoute(request, null!);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.PushRequested, Is.True);
            Assert.That(result.Target, Is.Null);
            Assert.That(result.PushResult!.CorrelationId,
                Is.EqualTo("provider-1"));
            Assert.That(policy.Request!.Provider, Is.EqualTo("apns"));
            Assert.That(policy.Request.ResourceId,
                Is.EqualTo("device-token"));
            Assert.That(policy.Request.CorrelationId, Is.EqualTo("push-1"));
            Assert.That(binding.Contact!.Address!.Uri!.Host,
                Is.EqualTo("device.example.com"));
            Assert.That(result.Binding!.Contact!.Address!.Uri!.Host,
                Is.EqualTo("device.example.com"));
            Assert.That(unsupported.StatusCode, Is.EqualTo(
                SipStatusCodes.PushNotificationServiceNotSupported));
        });
    }

    [Test]
    public void RoutingUsesQualityOrderAndSkipsExpiredOrInsecureBindings()
    {
        SipRegistrationBinding expired = Binding(false);
        expired.Contact!.Address!.Uri!.Host = "expired.example.com";
        expired.QualityThousandths = 1000;
        expired.ExpiresAtMilliseconds = 500;
        SipRegistrationBinding insecure = Binding(false);
        insecure.Contact!.Address!.Uri!.Host = "insecure.example.com";
        insecure.QualityThousandths = 900;
        SipRegistrationBinding secure = Binding(false);
        secure.Contact!.Address!.Uri = Uri("sips:alice@secure.example.com");
        secure.QualityThousandths = 700;
        SipOutboundRouteRequest request = new SipOutboundRouteRequest();
        request.Bindings = new SipRegistrationBinding[] {
            expired, insecure, secure };
        request.NowMilliseconds = 1000;
        request.RequireSecure = true;

        SipOutboundResult result = new SipOutboundService()
            .SelectBindingRoute(request, null!);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Target!.Uri!.Scheme,
                Is.EqualTo(SipUriScheme.Sips));
            Assert.That(result.Target.Uri.Host,
                Is.EqualTo("secure.example.com"));
        });
    }

    [Test]
    public void InspectsMatchingPushCapabilityAndRegistrationReference()
    {
        SipContactValue requested = Contact(
            "<sip:alice@device.example.com;pn-provider=apns;"
            + "pn-param=TEAM.example.voip;pn-prid=device-token>"
            + ";reg-id=2;+sip.instance=\"" + Instance + "\"");
        SipMessage accepted = RegisterResponse(
            "Feature-Caps: *;+sip.pns=\"apns\";+sip.pnsreg=\"120\"\r\n"
            + "Contact: <sip:alice@device.example.com;pn-provider=apns;"
            + "pn-param=TEAM.example.voip;pn-prid=device-token;pn-purr=reference>"
            + ";reg-id=2;+sip.instance=\"" + Instance + "\"\r\n");
        SipMessage unsupported = RegisterResponse(
            "Feature-Caps: *;+sip.pns=\"fcm\"\r\n"
            + "Contact: <sip:alice@device.example.com;pn-provider=apns;"
            + "pn-prid=device-token>;reg-id=2;+sip.instance=\""
            + Instance + "\"\r\n");
        SipOutboundService service = new SipOutboundService();

        SipOutboundResult result =
            service.InspectPushRegistration(accepted, requested);
        SipOutboundResult missing =
            service.InspectPushRegistration(unsupported, requested);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.PushSupported, Is.True);
            Assert.That(result.Contact!.PushRegistrationReference,
                Is.EqualTo("reference"));
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.StatusCode, Is.EqualTo(
                SipStatusCodes.PushNotificationServiceNotSupported));
        });
    }

    [Test]
    public void UpdatesOnlyValidDialogTargetRefreshes()
    {
        SipDialogSnapshot dialog = new SipDialogSnapshot();
        dialog.RemoteTarget = Uri("sip:bob@old.example.com");
        SipMessage update = Request("UPDATE",
            "Contact: <sip:bob@new.example.com>\r\n");
        SipMessage bye = Request("BYE",
            "Contact: <sip:bob@ignored.example.com>\r\n");
        SipDialogSnapshot secure = new SipDialogSnapshot();
        secure.Secure = true;
        secure.RemoteTarget = Uri("sips:bob@secure.example.com");
        SipOutboundService service = new SipOutboundService();

        SipOutboundResult updated =
            service.UpdateDialogTarget(dialog, update);
        SipOutboundResult ignored = service.UpdateDialogTarget(dialog, bye);
        SipOutboundResult rejected = service.UpdateDialogTarget(secure, update);

        Assert.Multiple(() =>
        {
            Assert.That(updated.Success, Is.True, updated.ErrorMessage);
            Assert.That(updated.TargetUpdated, Is.True);
            Assert.That(updated.Dialog!.RemoteTarget!.Host,
                Is.EqualTo("new.example.com"));
            Assert.That(ignored.Success, Is.True);
            Assert.That(ignored.TargetUpdated, Is.False);
            Assert.That(ignored.Dialog!.RemoteTarget!.Host,
                Is.EqualTo("old.example.com"));
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.StatusCode,
                Is.EqualTo(SipStatusCodes.UnsupportedUriScheme));
            Assert.That(dialog.RemoteTarget!.Host,
                Is.EqualTo("old.example.com"));
        });
    }

    [Test]
    public void FailedOutboundFlowFallsBackToImmediatelyRoutableBinding()
    {
        SipRegistrationBinding dormant = Binding(true);
        dormant.QualityThousandths = 1000;
        dormant.Flow = Flow(true);
        SipRegistrationBinding direct = Binding(false);
        direct.QualityThousandths = 200;
        direct.Contact!.Address!.Uri!.Host = "fallback.example.com";
        SipOutboundRouteRequest request = new SipOutboundRouteRequest();
        request.Bindings = new SipRegistrationBinding[] { dormant, direct };
        request.Destination = Uri("sip:alice@example.com");
        request.NowMilliseconds = 1000;
        request.AllowPush = true;

        SipOutboundResult result = new SipOutboundService()
            .SelectBindingRoute(request, null!);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Target!.Uri!.Host,
                Is.EqualTo("fallback.example.com"));
            Assert.That(result.Target.Flow, Is.Null);
            Assert.That(result.Binding!.QualityThousandths, Is.EqualTo(200));
        });
    }

    [Test]
    public void PushPolicyRejectionPreservesStatusAndDoesNotCreateRoute()
    {
        SipRegistrationBinding dormant = Binding(true);
        dormant.Flow = Flow(true);
        SipOutboundRouteRequest request = new SipOutboundRouteRequest();
        request.Bindings = new SipRegistrationBinding[] { dormant };
        request.Destination = Uri("sip:alice@example.com");
        request.NowMilliseconds = 1000;
        request.AllowPush = true;
        RecordingPushPolicy policy = new RecordingPushPolicy();
        policy.Decision.StatusCode = SipStatusCodes.TemporarilyUnavailable;
        policy.Decision.Reason = "push provider unavailable";

        SipOutboundResult result = new SipOutboundService()
            .SelectBindingRoute(request, policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.PushRequested, Is.False);
            Assert.That(result.Target, Is.Null);
            Assert.That(result.StatusCode,
                Is.EqualTo(SipStatusCodes.TemporarilyUnavailable));
            Assert.That(result.ErrorMessage,
                Is.EqualTo("push provider unavailable"));
            Assert.That(result.PushResult, Is.Not.Null);
        });
    }

    static SipOutboundContactRequest ContactRequest()
    {
        SipOutboundContactRequest request = new SipOutboundContactRequest();
        request.ContactUri = Uri(
            "sip:alice@device.example.com;transport=tcp;vendor=kept");
        request.InstanceId = Instance;
        request.RegistrationId = 2;
        request.ExpiresSeconds = 600;
        request.QualityThousandths = 800;
        request.UseOutbound = true;
        request.RequestGruu = true;
        request.PushProvider = "apns";
        request.PushParameter = "TEAM.example.voip";
        request.PushResourceId = "device-token";
        return request;
    }

    static SipRegistrationBinding Binding(bool outbound)
    {
        SipRegistrationBinding binding = new SipRegistrationBinding();
        binding.AddressOfRecord = Uri("sip:alice@example.com");
        binding.Contact = Contact(outbound
            ? "<sip:alice@device.example.com;ob;pn-provider=apns;"
                + "pn-param=TEAM.example.voip;pn-prid=device-token>"
                + ";reg-id=2;+sip.instance=\"" + Instance + "\""
            : "<sip:alice@device.example.com>");
        binding.InstanceId = outbound ? Instance : null;
        binding.RegistrationId = outbound ? 2 : -1;
        binding.QualityThousandths = 800;
        binding.ExpiresAtMilliseconds = 100000;
        return binding;
    }

    static SipFlow Flow(bool failed)
    {
        SipFlow flow = new SipFlow();
        flow.Id = "flow-2";
        flow.InstanceId = Instance;
        flow.RegistrationId = 2;
        flow.Connection = Connection("flow-connection");
        flow.Failed = failed;
        flow.ExpiresAtMilliseconds = 100000;
        return flow;
    }

    static SipConnectionInfo Connection(string id)
    {
        SipConnectionInfo connection = new SipConnectionInfo();
        connection.Id = id;
        connection.State = SipConnectionState.Connected;
        connection.Transport = SipTransportKind.Tcp;
        connection.LocalAddress = Endpoint("192.0.2.10", 5090);
        connection.RemoteAddress = Endpoint("198.51.100.10", 5060);
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

    static SipContactValue Contact(string value)
    {
        SipHeaderParseResult parsed = new SipHeaderCodec().ParseHeader(
            System.Text.Encoding.UTF8.GetBytes("Contact: " + value + "\r\n"),
            new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.ContactValues[0];
    }

    static SipRouteValue[] Routes(string value)
    {
        SipHeaderParseResult parsed = new SipHeaderCodec().ParseHeader(
            System.Text.Encoding.UTF8.GetBytes("Route: " + value + "\r\n"),
            new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.RouteValues;
    }

    static SipUri Uri(string text)
    {
        SipParseResult parsed = new SipUriCodec().ParseUri(
            System.Text.Encoding.UTF8.GetBytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Uri!;
    }

    static SipMessage RegisterResponse(string headers)
    {
        return Message("SIP/2.0 200 OK\r\n"
            + "Via: SIP/2.0/TCP client.example.com;branch=z9hG4bK-push\r\n"
            + "From: <sip:alice@example.com>;tag=from\r\n"
            + "To: <sip:alice@example.com>;tag=to\r\n"
            + "Call-ID: push@example.com\r\n"
            + "CSeq: 1 REGISTER\r\n"
            + headers
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Request(string method, string headers)
    {
        return Message(method + " sip:bob@example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/TCP client.example.com;branch=z9hG4bK-target\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <sip:alice@example.com>;tag=from\r\n"
            + "To: <sip:bob@example.com>;tag=to\r\n"
            + "Call-ID: target@example.com\r\n"
            + "CSeq: 2 " + method + "\r\n"
            + headers
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Message(string text)
    {
        SipParseResult parsed = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.UTF8.GetBytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Message!;
    }

    static string? Header(SipMessage message, string name)
    {
        return message.Headers.FirstOrDefault(header => string.Equals(
            header.Name, name, System.StringComparison.OrdinalIgnoreCase))
            ?.Value;
    }

    static SipUriParameter? Parameter(
        SipUriParameter[] parameters, string name)
    {
        return parameters.FirstOrDefault(parameter => string.Equals(
            parameter.Name, name,
            System.StringComparison.OrdinalIgnoreCase));
    }

    sealed class RecordingPushPolicy : ISipPushNotificationPolicy
    {
        public SipPushNotificationRequest? Request;
        public SipPushNotificationResult Decision =
            new SipPushNotificationResult();

        public SipPushNotificationResult RequestPushNotification(
            SipPushNotificationRequest request)
        {
            Request = request;
            request.Binding!.Contact!.Address!.Uri!.Host = "changed.invalid";
            return Decision;
        }
    }
}
