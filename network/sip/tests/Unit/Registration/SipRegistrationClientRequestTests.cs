namespace sip.Tests;

public class SipRegistrationClientRequestTests
{
    [Test]
    public void InitialRegisterContainsMandatoryHeadersAndGeneratedContact()
    {
        SipRegistrationClient client = new SipRegistrationClient();
        SipRegistrationRequest request = CreateRequest();
        request.CorrelationId = "register-one";

        SipRegistrationSnapshot snapshot = client.RegisterContact(request);
        SipRegistrationEvent registrationEvent = client.PollRegistrationEvent();
        SipMessage message = registrationEvent.Message!;

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.State, Is.EqualTo(SipRegistrationState.Registering));
            Assert.That(snapshot.CallId, Is.Not.Null.And.Not.Empty);
            Assert.That(snapshot.CSeq, Is.EqualTo(1));
            Assert.That(snapshot.CorrelationId, Is.EqualTo("register-one"));
            Assert.That(message.Kind, Is.EqualTo(SipMessageKind.Request));
            Assert.That(message.Method, Is.EqualTo(SipMethod.Register));
            Assert.That(message.MethodToken, Is.EqualTo(SipMethods.Register));
            Assert.That(message.RequestUri!.Host, Is.EqualTo("registrar.example.com"));
            Assert.That(message.ViaValues, Has.Length.EqualTo(1));
            Assert.That(message.ViaValues[0].Branch, Does.StartWith("z9hG4bK"));
            Assert.That(message.From!.Uri!.User, Is.EqualTo("alice"));
            Assert.That(Parameter(message.From.Parameters, "tag"), Is.Not.Null);
            Assert.That(message.To!.Uri!.User, Is.EqualTo("alice"));
            Assert.That(Parameter(message.To.Parameters, "tag"), Is.Null);
            Assert.That(Header(message, SipHeaderNames.CallId),
                Is.EqualTo(snapshot.CallId));
            Assert.That(message.CSeq!.SequenceNumber, Is.EqualTo(1));
            Assert.That(message.CSeq.Method, Is.EqualTo(SipMethod.Register));
            Assert.That(Header(message, SipHeaderNames.MaxForwards), Is.EqualTo("70"));
            Assert.That(Header(message, SipHeaderNames.Expires), Is.EqualTo("1200"));
            Assert.That(Header(message, SipHeaderNames.ContentLength), Is.EqualTo("0"));
            Assert.That(message.ContactValues, Has.Length.EqualTo(1));
            Assert.That(message.ContactValues[0].Address!.Uri!.Host,
                Is.EqualTo("device.example.com"));
            Assert.That(message.ContactValues[0].ExpiresSeconds, Is.EqualTo(900));
            Assert.That(message.ContactValues[0].QualityThousandths, Is.EqualTo(750));
            Assert.That(snapshot.Bindings[0].CallId, Is.EqualTo(snapshot.CallId));
            Assert.That(snapshot.Bindings[0].CSeq, Is.EqualTo(1));
            Assert.That(registrationEvent.CorrelationId, Is.EqualTo("register-one"));
            Assert.That(registrationEvent.Snapshot!.CallId, Is.EqualTo(snapshot.CallId));
            Assert.That(client.PollRegistrationEvent().Message, Is.Null);
        });
    }

    [Test]
    public void RefreshKeepsCallIdAndFromTagWhileAdvancingCSeqAndBranch()
    {
        SipRegistrationClient client = new SipRegistrationClient();
        SipRegistrationRequest request = CreateRequest();
        SipRegistrationSnapshot initial = client.RegisterContact(request);
        SipMessage first = client.PollRegistrationEvent().Message!;

        SipRegistrationSnapshot refreshed = client.RefreshRegistration(request);
        SipMessage second = client.PollRegistrationEvent().Message!;
        SipRegistrationSnapshot unregistering = client.UnregisterContact(request);
        SipMessage third = client.PollRegistrationEvent().Message!;

        Assert.Multiple(() =>
        {
            Assert.That(refreshed.State, Is.EqualTo(SipRegistrationState.Refreshing));
            Assert.That(unregistering.State,
                Is.EqualTo(SipRegistrationState.Unregistering));
            Assert.That(initial.CallId, Is.EqualTo(refreshed.CallId));
            Assert.That(refreshed.CallId, Is.EqualTo(unregistering.CallId));
            Assert.That(first.CSeq!.SequenceNumber, Is.EqualTo(1));
            Assert.That(second.CSeq!.SequenceNumber, Is.EqualTo(2));
            Assert.That(third.CSeq!.SequenceNumber, Is.EqualTo(3));
            Assert.That(Header(first, SipHeaderNames.CallId),
                Is.EqualTo(Header(second, SipHeaderNames.CallId)));
            Assert.That(Parameter(first.From!.Parameters, "tag")!.Value,
                Is.EqualTo(Parameter(second.From!.Parameters, "tag")!.Value));
            Assert.That(first.ViaValues[0].Branch,
                Is.Not.EqualTo(second.ViaValues[0].Branch));
            Assert.That(second.ViaValues[0].Branch,
                Is.Not.EqualTo(third.ViaValues[0].Branch));
            Assert.That(Header(third, SipHeaderNames.Expires), Is.EqualTo("0"));
        });
    }

    [Test]
    public void OutboundProxyPrecedesServiceRouteWithoutChangingRegistrarTarget()
    {
        SipRegistrationClient client = new SipRegistrationClient();
        SipRegistrationRequest request = CreateRequest();
        request.OutboundProxy = Uri("sip:edge.example.net;transport=tcp");
        request.ServiceRoute = new SipRouteValue[]
        {
            Route("sips:service-one.example.net;lr"),
            Route("sip:service-two.example.net;lr")
        };

        SipRegistrationSnapshot snapshot = client.RegisterContact(request);
        SipMessage message = client.PollRegistrationEvent().Message!;

        Assert.Multiple(() =>
        {
            Assert.That(message.RequestUri!.Host, Is.EqualTo("registrar.example.com"));
            Assert.That(message.RouteValues, Has.Length.EqualTo(3));
            Assert.That(message.RouteValues[0].Address!.Uri!.Host,
                Is.EqualTo("edge.example.net"));
            Assert.That(Parameter(
                message.RouteValues[0].Address!.Uri!.Parameters, "lr"), Is.Not.Null);
            Assert.That(message.RouteValues[1].Address!.Uri!.Host,
                Is.EqualTo("service-one.example.net"));
            Assert.That(message.RouteValues[2].Address!.Uri!.Host,
                Is.EqualTo("service-two.example.net"));
            Assert.That(snapshot.OutboundProxy!.Host, Is.EqualTo("edge.example.net"));
            Assert.That(snapshot.ServiceRoute, Has.Length.EqualTo(2));
            Assert.That(snapshot.Bindings[0].ServiceRoute, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void TargetDefaultsToAddressOfRecordDomain()
    {
        SipRegistrationClient client = new SipRegistrationClient();
        SipRegistrationRequest request = CreateRequest();
        request.Target = null;

        SipRegistrationSnapshot snapshot = client.RegisterContact(request);
        SipMessage message = client.PollRegistrationEvent().Message!;

        Assert.Multiple(() =>
        {
            Assert.That(message.RequestUri!.Scheme, Is.EqualTo(SipUriScheme.Sip));
            Assert.That(message.RequestUri.User, Is.Null);
            Assert.That(message.RequestUri.Host, Is.EqualTo("example.com"));
            Assert.That(message.RequestUri.Parameters, Is.Empty);
            Assert.That(snapshot.Target!.User, Is.Null);
            Assert.That(snapshot.Target.Host, Is.EqualTo("example.com"));
        });
    }

    [Test]
    public void AuthenticationAssociationSurvivesRefreshAndEventsAreDetached()
    {
        SipRegistrationClient client = new SipRegistrationClient();
        SipRegistrationRequest request = CreateRequest();
        request.AuthenticationContextId = "digest-context-one";
        SipRegistrationSnapshot initial = client.RegisterContact(request);
        initial.AddressOfRecord!.Host = "mutated.invalid";
        SipRegistrationEvent created = client.PollRegistrationEvent();

        client.AssociateRegistrationAuthenticationContext("digest-context-two");
        SipRegistrationEvent associated = client.PollRegistrationEvent();
        request.AuthenticationContextId = null;
        SipRegistrationSnapshot refreshed = client.RefreshRegistration(request);
        SipRegistrationEvent refreshEvent = client.PollRegistrationEvent();

        Assert.Multiple(() =>
        {
            Assert.That(created.Snapshot!.AddressOfRecord!.Host,
                Is.EqualTo("example.com"));
            Assert.That(created.Snapshot.AuthenticationContextId,
                Is.EqualTo("digest-context-one"));
            Assert.That(associated.Message, Is.Null);
            Assert.That(associated.Snapshot!.AuthenticationContextId,
                Is.EqualTo("digest-context-two"));
            Assert.That(refreshed.AuthenticationContextId,
                Is.EqualTo("digest-context-two"));
            Assert.That(refreshEvent.Snapshot!.AuthenticationContextId,
                Is.EqualTo("digest-context-two"));
        });
    }

    [Test]
    public void InvalidRequestsDoNotConsumeRegistrationIdentity()
    {
        SipRegistrationClient client = new SipRegistrationClient();
        SipRegistrationRequest invalid = CreateRequest();
        invalid.Contacts = Array.Empty<SipContactValue>();

        Assert.Throws<ArgumentException>(() => client.RegisterContact(invalid));
        Assert.That(client.PollRegistrationEvent().Message, Is.Null);

        SipRegistrationRequest valid = CreateRequest();
        SipRegistrationSnapshot snapshot = client.RegisterContact(valid);
        client.PollRegistrationEvent();
        SipRegistrationRequest unrelated = CreateRequest();
        unrelated.AddressOfRecord = Uri("sip:bob@example.com");

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.CSeq, Is.EqualTo(1));
            Assert.Throws<ArgumentException>(() =>
                client.RefreshRegistration(unrelated));
            Assert.Throws<InvalidOperationException>(() =>
                client.RegisterContact(valid));
            Assert.That(client.PollRegistrationEvent().Message, Is.Null);
        });
    }

    static SipRegistrationRequest CreateRequest()
    {
        SipContactValue contact = new SipContactValue();
        SipNameAddress address = new SipNameAddress();
        address.DisplayName = "Alice Device";
        address.Uri = Uri("sip:alice@device.example.com;transport=tcp");
        contact.Address = address;
        contact.ExpiresSeconds = 900;
        contact.QualityThousandths = 750;

        SipRegistrationOptions options = new SipRegistrationOptions();
        options.ExpiresSeconds = 1200;
        SipRegistrationRequest request = new SipRegistrationRequest();
        request.AddressOfRecord = Uri("sip:alice@example.com");
        request.Contacts = new SipContactValue[] { contact };
        request.Options = options;
        request.Target = Uri("sip:registrar.example.com");
        return request;
    }

    static SipRouteValue Route(string text)
    {
        SipRouteValue route = new SipRouteValue();
        SipNameAddress address = new SipNameAddress();
        address.Uri = Uri(text);
        route.Address = address;
        route.LooseRouting = Parameter(address.Uri.Parameters, "lr") != null;
        return route;
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
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            if (string.Equals(header.Name, name,
                StringComparison.OrdinalIgnoreCase))
            {
                return header.Value;
            }
            index = index + 1;
        }
        return null;
    }

    static SipUriParameter? Parameter(SipUriParameter[] parameters, string name)
    {
        int index = 0;
        while (index < parameters.Length)
        {
            SipUriParameter parameter = parameters[index];
            if (string.Equals(parameter.Name, name,
                StringComparison.OrdinalIgnoreCase))
            {
                return parameter;
            }
            index = index + 1;
        }
        return null;
    }
}
