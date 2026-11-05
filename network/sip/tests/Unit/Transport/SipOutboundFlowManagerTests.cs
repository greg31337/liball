namespace sip.Tests;

public class SipOutboundFlowManagerTests
{
    const string Instance =
        "<urn:uuid:00000000-0000-1000-8000-000000000001>";

    [Test]
    public void RegistersSelectsFailsAndReplacesPrimaryAndBackupFlows()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(1000);
        SipOutboundFlowManager manager = Manager(clock, 4);
        SipFlow primary = manager.RegisterFlow(Contact(1), Connection(
            "connection-1", SipTransportKind.Tcp, 5101),
            new SipRouteValue[] { Route("path-one.example", null) },
            120, 1000000, false);
        SipFlow backup = manager.RegisterFlow(Contact(2), Connection(
            "connection-2", SipTransportKind.Tcp, 5102),
            new SipRouteValue[] { Route("path-two.example", null) },
            120, 1000000, false);

        SipFlow? initiallySelected = manager.SelectFlow(Instance);
        SipFlow? failed = manager.MarkFlowFailed(primary.Id);
        SipFlow? alternate = manager.SelectAlternateFlow(Instance, primary.Id);
        SipFlow replacement = manager.RegisterFlow(Contact(1), Connection(
            "connection-3", SipTransportKind.Tcp, 5103),
            new SipRouteValue[] { Route("replacement.example", null) },
            90, 1000000, true);
        SipFlow? selectedReplacement = manager.SelectFlow(Instance);

        Assert.Multiple(() =>
        {
            Assert.That(primary.Primary, Is.True);
            Assert.That(backup.Primary, Is.False);
            Assert.That(initiallySelected!.Id, Is.EqualTo(primary.Id));
            Assert.That(failed!.Failed, Is.True);
            Assert.That(alternate!.Id, Is.EqualTo(backup.Id));
            Assert.That(replacement.Id, Is.Not.EqualTo(primary.Id));
            Assert.That(replacement.Connection!.Id, Is.EqualTo("connection-3"));
            Assert.That(replacement.Path[0].Address!.Uri!.Host,
                Is.EqualTo("replacement.example"));
            Assert.That(selectedReplacement!.Id, Is.EqualTo(replacement.Id));
            Assert.That(manager.FindFlow(primary.Id), Is.Null);
            Assert.That(manager.CountFlows(), Is.EqualTo(2));
        });
    }

    [Test]
    public void CreatesAuthenticatedPathTokenBoundToTransportAssociation()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(0);
        SipOutboundFlowManager manager = Manager(clock, 2);
        SipFlow flow = manager.RegisterFlow(Contact(1), Connection(
            "connection-token", SipTransportKind.Tcp, 5200),
            new SipRouteValue[0], -1, 1000000, true);
        SipUri edge = Uri("edge.example");
        SipUriParameter transport = new SipUriParameter();
        transport.Name = "transport";
        transport.Value = "tcp";
        transport.HasValue = true;
        edge.Parameters = new SipUriParameter[] { transport };

        SipRouteValue path = manager.CreatePathRoute(flow.Id, edge);
        SipFlow? resolved = manager.ResolvePathFlow(path);
        string tamperedToken = flow.Id!.Substring(0, flow.Id.Length - 1)
            + (flow.Id[flow.Id.Length - 1] == 'A' ? "B" : "A");
        path.Address!.Uri!.User = tamperedToken;
        SipFlow? tampered = manager.ResolvePathFlow(path);
        edge.Parameters[0].Value = "tls";
        System.ArgumentException? incompatible =
            Assert.Throws<System.ArgumentException>(
                () => manager.CreatePathRoute(flow.Id, edge));

        Assert.Multiple(() =>
        {
            Assert.That(resolved!.Id, Is.EqualTo(flow.Id));
            Assert.That(resolved.Connection!.Id, Is.EqualTo("connection-token"));
            Assert.That(HasParameter(path.Address.Uri.Parameters, "lr"), Is.True);
            Assert.That(HasParameter(path.Address.Uri.Parameters, "ob"), Is.True);
            Assert.That(tampered, Is.Null);
            Assert.That(incompatible!.Message,
                Does.Contain("transport-compatible"));
        });
    }

    [Test]
    public void AppliesFlowTimerJitterPongDeadlineAndRecoveryBackoff()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(1000);
        SipOutboundFlowManager manager = Manager(clock, 2);
        SipFlow flow = manager.RegisterFlow(Contact(1), Connection(
            "connection-timer", SipTransportKind.Tcp, 5300),
            new SipRouteValue[0], 100, 1000000, true);
        clock.AdvanceMilliseconds(79999);
        SipFlow[] early = manager.StartDueKeepAlives();
        clock.AdvanceMilliseconds(1);
        SipFlow[] due = manager.StartDueKeepAlives();
        long deadline = due[0].KeepAliveResponseDeadlineMilliseconds;
        clock.AdvanceMilliseconds(9999);
        SipFlow[] notFailed = manager.ExpireKeepAliveTimeouts();
        bool acknowledged = manager.AcknowledgeKeepAlive(flow.Id);
        SipFlow? afterPong = manager.FindFlow(flow.Id);
        clock.AdvanceMilliseconds(80000);
        SipFlow[] secondDue = manager.StartDueKeepAlives();
        clock.AdvanceMilliseconds(10000);
        SipFlow[] failed = manager.ExpireKeepAliveTimeouts();
        long retryAt = failed[0].RetryAtMilliseconds;
        SipFlow[] beforeRetry = manager.GetRetryableFlows();
        clock.AdvanceMilliseconds(retryAt - clock.GetMilliseconds());
        SipFlow[] retryable = manager.GetRetryableFlows();
        SipFlow? repeatedFailure = manager.RecordRecoveryFailure(flow.Id);

        Assert.Multiple(() =>
        {
            Assert.That(flow.NextKeepAliveAtMilliseconds, Is.EqualTo(81000));
            Assert.That(early, Is.Empty);
            Assert.That(due, Has.Length.EqualTo(1));
            Assert.That(deadline, Is.EqualTo(91000));
            Assert.That(notFailed, Is.Empty);
            Assert.That(acknowledged, Is.True);
            Assert.That(afterPong!.NextKeepAliveAtMilliseconds,
                Is.EqualTo(170999));
            Assert.That(secondDue, Has.Length.EqualTo(1));
            Assert.That(failed, Has.Length.EqualTo(1));
            Assert.That(failed[0].Failed, Is.True);
            Assert.That(failed[0].ConsecutiveFailures, Is.EqualTo(1));
            Assert.That(retryAt, Is.EqualTo(210999));
            Assert.That(beforeRetry, Is.Empty);
            Assert.That(retryable.Select(value => value.Id),
                Does.Contain(flow.Id));
            Assert.That(repeatedFailure!.ConsecutiveFailures, Is.EqualTo(2));
            Assert.That(repeatedFailure.RetryAtMilliseconds,
                Is.EqualTo(270999));
        });
    }

    [Test]
    public void ReadsFlowTimerAndAppliesTransportFailureToAssociatedFlows()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(0);
        SipOutboundFlowManager manager = Manager(clock, 3);
        SipFlow first = manager.RegisterFlow(Contact(1), Connection(
            "shared", SipTransportKind.Tls, 5401), new SipRouteValue[0],
            -1, 1000000, true);
        SipContactValue otherContact = Contact(2);
        otherContact.InstanceId =
            "<urn:uuid:00000000-0000-1000-8000-000000000002>";
        manager.RegisterFlow(otherContact, Connection(
            "shared", SipTransportKind.Tls, 5401), new SipRouteValue[0],
            -1, 1000000, true);
        SipMessage response = new SipMessage();
        SipHeader timer = new SipHeader();
        timer.Name = "flow-timer";
        timer.Value = "75";
        response.Headers = new SipHeader[] { timer };
        int seconds = manager.ReadFlowTimer(response);
        SipFlow? updated = manager.UpdateFlowTimer(first.Id, seconds);
        SipTransportEvent disconnected = new SipTransportEvent();
        disconnected.Kind = SipTransportEventKind.Disconnected;
        disconnected.Connection = new SipConnectionInfo();
        disconnected.Connection.Id = "shared";
        SipFlow[] failed = manager.ApplyTransportEvent(disconnected);

        SipHeader duplicate = new SipHeader();
        duplicate.Name = SipHeaderNames.FlowTimer;
        duplicate.Value = "80";
        response.Headers = new SipHeader[] { timer, duplicate };
        SipProtocolException? invalid = Assert.Throws<SipProtocolException>(
            () => manager.ReadFlowTimer(response));

        Assert.Multiple(() =>
        {
            Assert.That(updated!.FlowTimerSeconds, Is.EqualTo(75));
            Assert.That(updated.NextKeepAliveAtMilliseconds,
                Is.EqualTo(60000));
            Assert.That(failed, Has.Length.EqualTo(2));
            Assert.That(failed.All(value => value.Failed), Is.True);
            Assert.That(invalid!.StatusCode, Is.EqualTo(400));
        });
    }

    [Test]
    public void DetachesSnapshotsExpiresFlowsAndRejectsInvalidIdentity()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(0);
        SipOutboundFlowManager manager = Manager(clock, 1);
        SipRouteValue sourcePath = Route("stored.example", null);
        SipFlow flow = manager.RegisterFlow(Contact(1), Connection(
            "connection-expiry", SipTransportKind.WebSocket, 5500),
            new SipRouteValue[] { sourcePath }, -1, 100, true);
        sourcePath.Address!.Uri!.Host = "changed.example";
        flow.Path[0].Address!.Uri!.Host = "also-changed.example";
        SipFlow? stored = manager.FindFlow(flow.Id);
        SipContactValue invalidContact = Contact(1);
        invalidContact.InstanceId = "not-a-uuid-urn";
        Assert.Throws<System.ArgumentException>(() => manager.RegisterFlow(
            invalidContact, Connection("invalid", SipTransportKind.Tcp, 5501),
            new SipRouteValue[0], -1, 1000, true));
        clock.AdvanceMilliseconds(100);
        SipFlow[] expired = manager.RemoveExpiredFlows();

        Assert.Multiple(() =>
        {
            Assert.That(stored!.Path[0].Address!.Uri!.Host,
                Is.EqualTo("stored.example"));
            Assert.That(stored.NextKeepAliveAtMilliseconds, Is.EqualTo(-1));
            Assert.That(expired.Select(value => value.Id), Does.Contain(flow.Id));
            Assert.That(manager.CountFlows(), Is.Zero);
        });
    }

    [Test]
    public void RegistrationReplacementPreservesCapacityAndValidatesConnectionSecurity()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(0);
        SipOutboundFlowManager manager = Manager(clock, 1);
        SipFlow original = manager.RegisterFlow(Contact(1), Connection(
            "connection-original", SipTransportKind.Tcp, 5601),
            new SipRouteValue[0], -1, 100000, true);
        SipFlow replacement = manager.RegisterFlow(Contact(1), Connection(
            "connection-replacement", SipTransportKind.Tcp, 5602),
            new SipRouteValue[0], -1, 100000, false);
        System.InvalidOperationException? capacity =
            Assert.Throws<System.InvalidOperationException>(() => manager.RegisterFlow(
                Contact(2), Connection("connection-capacity", SipTransportKind.Tcp, 5603),
                new SipRouteValue[0], -1, 100000, false));
        SipConnectionInfo secureTcp = Connection(
            "secure-tcp", SipTransportKind.Tcp, 5604);
        secureTcp.Secure = true;
        secureTcp.LocalAddress!.Secure = true;
        secureTcp.RemoteAddress!.Secure = true;
        SipConnectionInfo insecureTls = Connection(
            "insecure-tls", SipTransportKind.Tls, 5605);
        insecureTls.Secure = false;
        insecureTls.LocalAddress!.Secure = false;
        insecureTls.RemoteAddress!.Secure = false;

        Assert.Multiple(() =>
        {
            Assert.That(replacement.Id, Is.Not.EqualTo(original.Id));
            Assert.That(replacement.Connection!.Id,
                Is.EqualTo("connection-replacement"));
            Assert.That(manager.FindFlow(original.Id), Is.Null);
            Assert.That(manager.CountFlows(), Is.EqualTo(1));
            Assert.That(capacity!.Message, Does.Contain("registry is full"));
            Assert.That(() => manager.RegisterFlow(Contact(1), secureTcp,
                new SipRouteValue[0], -1, 100000, true),
                Throws.ArgumentException);
            Assert.That(() => manager.RegisterFlow(Contact(1), insecureTls,
                new SipRouteValue[0], -1, 100000, true),
                Throws.ArgumentException);
        });
    }

    [Test]
    public void SelectsActivePrimaryThenStableBackupWithinTheRequestedInstance()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(0);
        SipOutboundFlowManager manager = Manager(clock, 4);
        SipFlow firstBackup = manager.RegisterFlow(Contact(1), Connection(
            "connection-first", SipTransportKind.Tcp, 5701),
            new SipRouteValue[0], -1, 1000, false);
        SipFlow primary = manager.RegisterFlow(Contact(2), Connection(
            "connection-primary", SipTransportKind.Tcp, 5702),
            new SipRouteValue[0], -1, 100, true);
        SipFlow secondBackup = manager.RegisterFlow(Contact(3), Connection(
            "connection-second", SipTransportKind.Tcp, 5703),
            new SipRouteValue[0], -1, 1000, false);
        SipContactValue otherContact = Contact(1);
        otherContact.InstanceId =
            "<urn:uuid:00000000-0000-1000-8000-000000000002>";
        SipFlow other = manager.RegisterFlow(otherContact, Connection(
            "connection-other", SipTransportKind.Tcp, 5704),
            new SipRouteValue[0], -1, 1000, true);

        SipFlow? selectedPrimary = manager.SelectFlow(Instance);
        clock.AdvanceMilliseconds(100);
        SipFlow? selectedFirstBackup = manager.SelectFlow(Instance);
        manager.MarkFlowFailed(firstBackup.Id);
        SipFlow? selectedSecondBackup = manager.SelectFlow(Instance);
        SipFlow? selectedOther = manager.SelectFlow(other.InstanceId);
        SipFlow? noAlternate = manager.SelectAlternateFlow(
            Instance, secondBackup.Id);

        Assert.Multiple(() =>
        {
            Assert.That(selectedPrimary!.Id, Is.EqualTo(primary.Id));
            Assert.That(selectedFirstBackup!.Id, Is.EqualTo(firstBackup.Id));
            Assert.That(selectedSecondBackup!.Id, Is.EqualTo(secondBackup.Id));
            Assert.That(selectedOther!.Id, Is.EqualTo(other.Id));
            Assert.That(noAlternate, Is.Null);
        });
    }

    [Test]
    public void AppliesFlowTimerBoundariesAndAcknowledgesEveryFlowOnAConnection()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(0);
        SipOutboundFlowManager manager = Manager(clock, 3);
        SipFlow first = manager.RegisterFlow(Contact(1), Connection(
            "shared-keepalive", SipTransportKind.Tcp, 5801),
            new SipRouteValue[0], 10, 100000, true);
        SipFlow second = manager.RegisterFlow(Contact(2), Connection(
            "shared-keepalive", SipTransportKind.Tcp, 5801),
            new SipRouteValue[0], 10, 100000, false);
        SipFlow webSocket = manager.RegisterFlow(Contact(3), Connection(
            "websocket", SipTransportKind.WebSocket, 5802),
            new SipRouteValue[0], 10, 100000, false);
        clock.AdvanceMilliseconds(8000);
        SipFlow[] due = manager.StartDueKeepAlives();
        SipTransportEvent pong = new SipTransportEvent();
        pong.Kind = SipTransportEventKind.KeepAliveReceived;
        pong.Connection = Connection(
            "shared-keepalive", SipTransportKind.Tcp, 5801);
        SipFlow[] acknowledged = manager.ApplyTransportEvent(pong);
        SipMessage absent = new SipMessage();
        SipMessage zero = FlowTimerMessage("0");
        SipMessage overflow = FlowTimerMessage("2147483648");

        Assert.Multiple(() =>
        {
            Assert.That(first.NextKeepAliveAtMilliseconds, Is.EqualTo(8000));
            Assert.That(second.NextKeepAliveAtMilliseconds, Is.EqualTo(8000));
            Assert.That(webSocket.NextKeepAliveAtMilliseconds, Is.EqualTo(-1));
            Assert.That(due.Select(flow => flow.Id),
                Is.EquivalentTo(new string?[] { first.Id, second.Id }));
            Assert.That(acknowledged.Select(flow => flow.Id),
                Is.EquivalentTo(new string?[] { first.Id, second.Id }));
            Assert.That(acknowledged.All(flow =>
                flow.KeepAliveResponseDeadlineMilliseconds < 0), Is.True);
            Assert.That(manager.ReadFlowTimer(absent), Is.EqualTo(-1));
            Assert.That(manager.ReadFlowTimer(zero), Is.Zero);
            Assert.That(() => manager.ReadFlowTimer(overflow),
                Throws.TypeOf<SipProtocolException>());
        });
    }

    [Test]
    public void PathResolutionRequiresAuthenticatedOutboundRouteShape()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(0);
        SipOutboundFlowManager manager = Manager(clock, 2);
        SipFlow flow = manager.RegisterFlow(Contact(1), Connection(
            "connection-path", SipTransportKind.Tcp, 5901),
            new SipRouteValue[0], -1, 100000, true);
        SipUri edge = Uri("edge.example");
        SipUriParameter valuedLr = Parameter("lr", "yes", true);
        SipUriParameter valuedOb = Parameter("ob", "yes", true);
        SipUriParameter transport = Parameter("transport", "tcp", true);
        edge.Parameters = new SipUriParameter[]
        {
            valuedLr, valuedOb, transport
        };
        SipRouteValue path = manager.CreatePathRoute(flow.Id, edge);
        SipFlow? resolved = manager.ResolvePathFlow(path);

        path.LooseRouting = false;
        SipFlow? strict = manager.ResolvePathFlow(path);
        path.LooseRouting = true;
        SipUriParameter ob = path.Address!.Uri!.Parameters.Single(parameter =>
            parameter.Name!.Equals("ob", System.StringComparison.OrdinalIgnoreCase));
        ob.HasValue = true;
        ob.Value = "yes";
        SipFlow? valued = manager.ResolvePathFlow(path);
        ob.HasValue = false;
        ob.Value = null;
        SipUriParameter selectedTransport = path.Address.Uri.Parameters.Single(parameter =>
            parameter.Name!.Equals("transport", System.StringComparison.OrdinalIgnoreCase));
        selectedTransport.Value = "tls";
        SipFlow? wrongTransport = manager.ResolvePathFlow(path);
        SipUri secureEdge = Uri("secure-edge.example");
        secureEdge.Scheme = SipUriScheme.Sips;
        secureEdge.SchemeToken = "sips";

        Assert.Multiple(() =>
        {
            Assert.That(resolved!.Id, Is.EqualTo(flow.Id));
            Assert.That(path.Address.Uri.Parameters.Count(parameter =>
                parameter.Name!.Equals("lr", System.StringComparison.OrdinalIgnoreCase)),
                Is.EqualTo(1));
            Assert.That(path.Address.Uri.Parameters.Count(parameter =>
                parameter.Name!.Equals("ob", System.StringComparison.OrdinalIgnoreCase)),
                Is.EqualTo(1));
            Assert.That(strict, Is.Null);
            Assert.That(valued, Is.Null);
            Assert.That(wrongTransport, Is.Null);
            Assert.That(() => manager.CreatePathRoute(flow.Id, secureEdge),
                Throws.ArgumentException);
        });
    }

    [Test]
    public void FlowTokensAreBoundToManagerSecretAndConnectionAssociation()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(0);
        SipOutboundFlowManager firstManager = new SipOutboundFlowManager(
            clock, new ByteRandomSource(1), 2);
        SipOutboundFlowManager secondManager = new SipOutboundFlowManager(
            clock, new ByteRandomSource(2), 2);
        SipConnectionInfo firstConnection = Connection(
            "connection-token-scope", SipTransportKind.Tcp, 6001);
        SipFlow first = firstManager.RegisterFlow(Contact(1), firstConnection,
            new SipRouteValue[0], -1, 100000, true);
        SipFlow second = secondManager.RegisterFlow(Contact(1), firstConnection,
            new SipRouteValue[0], -1, 100000, true);
        SipRouteValue path = firstManager.CreatePathRoute(first.Id, Uri("edge.example"));

        SipFlow? correct = firstManager.ResolvePathFlow(path);
        SipFlow? wrongManager = secondManager.ResolvePathFlow(path);
        SipFlow? malformed = firstManager.FindFlow("not-a-flow-token");
        SipConnectionInfo changedEndpoint = Connection(
            "connection-token-scope", SipTransportKind.Tcp, 6002);
        SipFlow changed = firstManager.RegisterFlow(Contact(2), changedEndpoint,
            new SipRouteValue[0], -1, 100000, false);

        Assert.Multiple(() =>
        {
            Assert.That(correct!.Id, Is.EqualTo(first.Id));
            Assert.That(wrongManager, Is.Null);
            Assert.That(malformed, Is.Null);
            Assert.That(first.Id, Is.Not.EqualTo(second.Id));
            Assert.That(first.Id, Is.Not.EqualTo(changed.Id));
        });
    }

    [Test]
    public void RecoveryDistinguishesPartialAndAllFlowFailure()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(0);
        SipOutboundFlowManager manager = Manager(clock, 2);
        SipFlow primary = manager.RegisterFlow(Contact(1), Connection(
            "connection-recovery-primary", SipTransportKind.Tcp, 6101),
            new SipRouteValue[0], -1, 1000000, true);
        SipFlow backup = manager.RegisterFlow(Contact(2), Connection(
            "connection-recovery-backup", SipTransportKind.Tcp, 6102),
            new SipRouteValue[0], -1, 1000000, false);

        SipFlow? partialFailure = manager.MarkFlowFailed(primary.Id);
        SipFlow? allFailure = manager.MarkFlowFailed(backup.Id);
        clock.AdvanceMilliseconds(30000);
        SipFlow[] firstRetry = manager.GetRetryableFlows();
        SipFlow replacement = manager.RegisterFlow(Contact(2), Connection(
            "connection-recovered", SipTransportKind.Tcp, 6103),
            new SipRouteValue[0], -1, 1000000, false);
        SipFlow? selected = manager.SelectFlow(Instance);

        Assert.Multiple(() =>
        {
            Assert.That(partialFailure!.RetryAtMilliseconds, Is.EqualTo(90000));
            Assert.That(allFailure!.RetryAtMilliseconds, Is.EqualTo(30000));
            Assert.That(firstRetry.Select(flow => flow.Id),
                Is.EqualTo(new string?[] { backup.Id }));
            Assert.That(replacement.Failed, Is.False);
            Assert.That(replacement.ConsecutiveFailures, Is.Zero);
            Assert.That(replacement.Connection!.Id, Is.EqualTo("connection-recovered"));
            Assert.That(selected!.Id, Is.EqualTo(replacement.Id));
        });
    }

    static SipOutboundFlowManager Manager(SipManualMonotonicClock clock,
        int maximumFlows)
    {
        return new SipOutboundFlowManager(clock, new ZeroRandomSource(),
            maximumFlows);
    }

    static SipContactValue Contact(int registrationId)
    {
        SipContactValue result = new SipContactValue();
        result.InstanceId = Instance;
        result.RegistrationId = registrationId;
        result.Address = new SipNameAddress();
        result.Address.Uri = Uri("client.example");
        return result;
    }

    static SipConnectionInfo Connection(string id, SipTransportKind transport,
        int remotePort)
    {
        SipConnectionInfo result = new SipConnectionInfo();
        result.Id = id;
        result.State = SipConnectionState.Connected;
        result.Transport = transport;
        result.Secure = transport == SipTransportKind.Tls
            || transport == SipTransportKind.SecureWebSocket;
        result.LocalAddress = Endpoint("127.0.0.1", 5060, transport);
        result.RemoteAddress = Endpoint("192.0.2.1", remotePort, transport);
        return result;
    }

    static SipEndpointAddress Endpoint(string host, int port,
        SipTransportKind transport)
    {
        SipEndpointAddress result = new SipEndpointAddress();
        result.Host = host;
        result.Port = port;
        result.Transport = transport;
        result.Secure = transport == SipTransportKind.Tls
            || transport == SipTransportKind.SecureWebSocket;
        return result;
    }

    static SipRouteValue Route(string host, string? transport)
    {
        SipRouteValue result = new SipRouteValue();
        result.Address = new SipNameAddress();
        result.Address.Uri = Uri(host);
        if (transport != null)
        {
            SipUriParameter parameter = new SipUriParameter();
            parameter.Name = "transport";
            parameter.Value = transport;
            parameter.HasValue = true;
            result.Address.Uri.Parameters = new SipUriParameter[] { parameter };
        }
        result.LooseRouting = true;
        return result;
    }

    static SipUri Uri(string host)
    {
        SipUri result = new SipUri();
        result.Scheme = SipUriScheme.Sip;
        result.SchemeToken = "sip";
        result.Host = host;
        return result;
    }

    static SipUriParameter Parameter(string name, string? value, bool hasValue)
    {
        SipUriParameter result = new SipUriParameter();
        result.Name = name;
        result.Value = value;
        result.HasValue = hasValue;
        return result;
    }

    static SipMessage FlowTimerMessage(string value)
    {
        SipMessage result = new SipMessage();
        SipHeader header = new SipHeader();
        header.Name = SipHeaderNames.FlowTimer;
        header.Value = value;
        result.Headers = new SipHeader[] { header };
        return result;
    }

    static bool HasParameter(SipUriParameter[] parameters, string name)
    {
        return parameters.Any(parameter => parameter.Name != null
            && parameter.Name.Equals(name,
                System.StringComparison.OrdinalIgnoreCase));
    }
}

class ZeroRandomSource : ISipRandomSource
{
    public void FillBytes(byte[] bytes)
    {
        System.Array.Clear(bytes);
    }
}

class ByteRandomSource : ISipRandomSource
{
    byte value;

    public ByteRandomSource(byte supplied)
    {
        value = supplied;
    }

    public void FillBytes(byte[] bytes)
    {
        System.Array.Fill(bytes, value);
    }
}
