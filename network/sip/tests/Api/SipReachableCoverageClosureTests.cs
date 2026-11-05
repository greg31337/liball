namespace sip.Tests;

public class SipReachableCoverageClosureTests
{
    [Test]
    public void StoresAndRegistriesExerciseNonFirstReplacementAndRemoval()
    {
        SipMemoryPublicationStore store = new SipMemoryPublicationStore();
        SipPublication first = Publication("first", "one");
        SipPublication second = Publication("second", "two");
        SipPublication third = Publication("third", "three");
        store.StorePublication(first);
        store.StorePublication(second);
        store.StorePublication(third);
        store.StorePublication(Publication("second", "replacement"));
        bool removedSecond = store.RemovePublication("second");
        bool removedMissing = store.RemovePublication("missing");

        CoverageExtension firstExtension = new CoverageExtension("FIRST");
        CoverageExtension middleExtension = new CoverageExtension("MIDDLE");
        CoverageExtension lastExtension = new CoverageExtension("LAST");
        SipExtensionRegistry extensions = new SipExtensionRegistry();
        extensions.RegisterExtension(firstExtension);
        extensions.RegisterExtension(middleExtension);
        extensions.RegisterExtension(lastExtension);
        bool removedMiddle = extensions.RemoveExtension(middleExtension);
        bool retainedMiddle = extensions.SupportsMethod("MIDDLE");

        SipPublicationServer publicationServer = new SipPublicationServer();
        publicationServer.ConfigurePublicationEventPackageRegistry(
            new SipEventPackageRegistry());
        SipSubscriptionClient subscriptionClient = new SipSubscriptionClient();
        subscriptionClient.ConfigureSubscriptionAuthenticationClient(
            new CoverageAuthenticationClient());

        Assert.Multiple(() =>
        {
            Assert.That(removedSecond, Is.True);
            Assert.That(removedMissing, Is.False);
            Assert.That(store.GetPublications().Select(value =>
                value.PublicationId), Is.EqualTo(
                    new string?[] { "first", "third" }));
            Assert.That(removedMiddle, Is.True);
            Assert.That(retainedMiddle, Is.False);
            Assert.That(extensions.SupportsMethod("FIRST"), Is.True);
            Assert.That(extensions.SupportsMethod("LAST"), Is.True);
            Assert.That(publicationServer.GetPublications(), Is.Empty);
            Assert.That(subscriptionClient.PollSubscriptionEvent().State,
                Is.EqualTo(SipSubscriptionState.None));
        });
    }

    [Test]
    public void InitialCancellationTerminatesEitherBackToBackLeg()
    {
        SipInviteRequest leftCancelled = IncomingInvite("left-one", "left-1");
        leftCancelled.Cancellation = Cancellation("cancel-left");
        SipBackToBackEvent leftEvent = new SipBackToBackSession()
            .CreateBackToBackSession(leftCancelled,
                OutgoingInvite("right-one", "right-1"));

        SipInviteRequest rightCancelled = OutgoingInvite(
            "right-two", "right-2");
        rightCancelled.Cancellation = Cancellation("cancel-right");
        SipBackToBackEvent rightEvent = new SipBackToBackSession()
            .CreateBackToBackSession(
                IncomingInvite("left-two", "left-2"), rightCancelled);

        Assert.Multiple(() =>
        {
            Assert.That(leftEvent.State,
                Is.EqualTo(SipBackToBackState.Terminating));
            Assert.That(leftEvent.Cancellation!.Id,
                Is.EqualTo("cancel-left"));
            Assert.That(rightEvent.State,
                Is.EqualTo(SipBackToBackState.Terminating));
            Assert.That(rightEvent.Cancellation!.Id,
                Is.EqualTo("cancel-right"));
        });
    }

    [Test]
    public void BackToBackSessionClosesReachableValidationAndQueueBranches()
    {
        SipBackToBackSession session = new SipBackToBackSession();
        SipInviteRequest left = IncomingInvite("left-validation", "left-validation");
        SipInviteRequest right = OutgoingInvite("right-validation", "right-validation");
        session.CreateBackToBackSession(left, right);

        SipCancellation ignored = new SipCancellation();
        ignored.Requested = false;
        session.TerminateBackToBackSession(ignored);
        Assert.That(session.GetBackToBackSession().State,
            Is.EqualTo(SipBackToBackState.Establishing));

        Assert.Multiple(() =>
        {
            Assert.That(() => session.CreateBackToBackSession(left, right),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(() => session.TerminateBackToBackSession(null!),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => session.AssociateBackToBackAuthenticationContext(
                    SipBackToBackLeg.None, "invalid"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => session.ApplyBackToBackMapping(null!),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => session.MapBackToBackMessage(new SipMessage()),
                Throws.TypeOf<ArgumentException>());
        });

        SipBackToBackMapping ending = new SipBackToBackMapping();
        ending.SourceLeg = SipBackToBackLeg.Left;
        ending.SourceMessage = LegRequest(
            "OPTIONS", "left-validation", 2, "source-options");
        ending.MappedMessage = LegRequest(
            "BYE", "right-validation", 2, "mapped-bye");
        SipBackToBackEvent mapped = session.ApplyBackToBackMapping(ending);
        Assert.That(mapped.Mapped, Is.True);

        SipBackToBackSession queued = new SipBackToBackSession();
        queued.CreateBackToBackSession(
            IncomingInvite("left-queue", "left-queue"),
            OutgoingInvite("right-queue", "right-queue"));
        SipMessage queueMessage = LegRequest(
            "OPTIONS", "left-queue", 2, "queue-options");
        int index = 0;
        while (index <= SipDefaults.MaxQueueCount)
        {
            queued.MapBackToBackMessage(queueMessage);
            index = index + 1;
        }
        Assert.That(queued.PollBackToBackEvent().State,
            Is.EqualTo(SipBackToBackState.Establishing));
    }

    [Test]
    public void RepeatedIncomingSuccessRequiresEquivalentStatusTagAndBody()
    {
        SipInviteSession session = new SipInviteSession();
        SipMessage invite = IncomingInvite("replay-call", "replay").Message!;
        session.ReceiveInviteMessage(invite);
        DrainInvite(session);
        SipInviteRequest first = new SipInviteRequest();
        first.Message = InviteResponse(invite, 200, "OK", "answer", "uas-tag");
        SipInviteEvent accepted = session.StartInvite(first);
        DrainInvite(session);

        SipInviteRequest duplicate = new SipInviteRequest();
        duplicate.Message = InviteResponse(
            invite, 200, "OK", "answer", "uas-tag");
        SipInviteEvent replayed = session.StartInvite(duplicate);
        SipInviteRequest changed = new SipInviteRequest();
        changed.Message = InviteResponse(
            invite, 200, "OK", "changed", "uas-tag");

        Assert.Multiple(() =>
        {
            Assert.That(accepted.State,
                Is.EqualTo(SipInviteSessionState.Confirmed));
            Assert.That(replayed.State,
                Is.EqualTo(SipInviteSessionState.Confirmed));
            Assert.That(replayed.Reason, Does.Contain("retransmitted"));
            Assert.That(() => session.StartInvite(changed),
                Throws.TypeOf<InvalidOperationException>());
        });
    }

    [Test]
    public void OfferAnswerClosesRetransmissionGlareAndRequiredOfferBranches()
    {
        SipOfferAnswerSession remotePending = new SipOfferAnswerSession();
        SipOfferAnswer remote = CoverageRemoteOffer("remote-offer");
        remotePending.ApplyRemoteOffer(remote);
        SipOfferAnswer retransmitted = remotePending.ApplyRemoteOffer(
            CoverageRemoteOffer("remote-offer"));

        Assert.Multiple(() =>
        {
            Assert.That(retransmitted.State,
                Is.EqualTo(SipOfferAnswerState.RemoteOfferPending));
            Assert.That(() => remotePending.ApplyRemoteOffer(
                    CoverageRemoteOffer("Remote-offer")),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(() => remotePending.ApplyLocalOffer(
                    CoverageLocalOffer("glare")),
                Throws.TypeOf<InvalidOperationException>());
        });

        SipOfferAnswerSession localRequired = new SipOfferAnswerSession();
        localRequired.ApplyRemoteOffer(new SipOfferAnswer());
        SipOfferAnswerSession remoteRequired = new SipOfferAnswerSession();
        remoteRequired.ApplyLocalOffer(new SipOfferAnswer());
        Assert.Multiple(() =>
        {
            Assert.That(() => localRequired.ApplyLocalOffer(
                    new SipOfferAnswer()), Throws.TypeOf<ArgumentException>());
            Assert.That(() => remoteRequired.ApplyRemoteOffer(
                    new SipOfferAnswer()), Throws.TypeOf<ArgumentException>());
            Assert.That(() => new SipOfferAnswerSession().ApplyOfferAnswer(
                    new SipOfferAnswer()),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(() => new SipOfferAnswerSession().RollbackOfferAnswer(
                    new SipOfferAnswer()),
                Throws.TypeOf<InvalidOperationException>());
        });

        SipOfferAnswerSession completed = new SipOfferAnswerSession();
        completed.ApplyRemoteOffer(CoverageRemoteOffer("offer"));
        SipOfferAnswer localAnswer = CoverageLocalAnswer("answer");
        completed.ApplyOfferAnswer(localAnswer);
        SipOfferAnswer duplicate = completed.ApplyOfferAnswer(localAnswer);
        SipOfferAnswer changedAnswer = CoverageLocalAnswer("Answer");

        SipMediaType invalidType = CoverageSdpType();
        SipUriParameter invalidParameter = new SipUriParameter();
        invalidParameter.Name = "mode";
        invalidParameter.HasValue = false;
        invalidType.Parameters = new SipUriParameter[] { invalidParameter };
        SipOfferAnswer invalidMedia = CoverageLocalOffer("offer");
        invalidMedia.ContentType = invalidType;

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.State,
                Is.EqualTo(SipOfferAnswerState.Completed));
            Assert.That(() => completed.ApplyOfferAnswer(changedAnswer),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(() => new SipOfferAnswerSession().ApplyLocalOffer(
                    invalidMedia), Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void PendingRemoteTargetCommitsFromRequestUriOrLastRoute()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = CreateDialog(layer, "target-call");
        layer.UpdateDialog(dialog.Id!, RemoteUpdate(
            "target-call", 11, "moved-one.example.com"));
        layer.UpdateDialog(dialog.Id!, LocalOptions(
            "target-call", 11, "moved-one.example.com", null));
        SipDialogSnapshot requestUriCommit = layer.FindDialog(dialog.Id!);

        layer.UpdateDialog(dialog.Id!, RemoteUpdate(
            "target-call", 12, "moved-two.example.com"));
        layer.UpdateDialog(dialog.Id!, LocalOptions(
            "target-call", 12, "proxy.example.com",
            "moved-two.example.com"));
        SipDialogSnapshot routeCommit = layer.FindDialog(dialog.Id!);

        layer.UpdateDialog(dialog.Id!, RemoteUpdate(
            "target-call", 13, "not-used.example.com"));
        layer.UpdateDialog(dialog.Id!, LocalOptions(
            "target-call", 13, "other.example.com", null));
        SipDialogSnapshot notCommitted = layer.FindDialog(dialog.Id!);

        Assert.Multiple(() =>
        {
            Assert.That(requestUriCommit.RemoteTarget!.Host,
                Is.EqualTo("moved-one.example.com"));
            Assert.That(routeCommit.RemoteTarget!.Host,
                Is.EqualTo("moved-two.example.com"));
            Assert.That(notCommitted.RemoteTarget!.Host,
                Is.EqualTo("moved-two.example.com"));
        });
    }

    [Test]
    public void EndpointBridgesDialogEventsAndManagerMarksFlowEvents()
    {
        SipEndpoint endpoint = new SipEndpoint();
        endpoint.StartEndpoint(new SipStackOptions());
        endpoint.PollStackEvent();
        SipDialogLayer dialogLayer =
            (SipDialogLayer)endpoint.GetEndpointDialogLayer();
        dialogLayer.CreateDialog(DialogInvite("endpoint-dialog"),
            DialogResponse("endpoint-dialog"), new SipDialogOptions());
        SipStackEvent dialogEvent = endpoint.PollStackEvent();
        Stop(endpoint);

        ManagedTestTransport transport = new ManagedTestTransport(
            SipTransportKind.Custom, false);
        SipConnectionInfo connection = Connection("flow-connection");
        SipFlow flow = new SipFlow();
        flow.Id = "flow-event";
        flow.Connection = connection;
        transport.ResultConnection = connection;
        transport.ResultFlow = flow;
        SipTransportManager manager = new SipTransportManager();
        manager.RegisterTransport(transport);
        SipSendResult established = manager.SendManaged(
            ManagedRequest("establish"));
        DrainManaged(manager);
        SipTransportEvent failedEvent = new SipTransportEvent();
        failedEvent.Kind = SipTransportEventKind.FlowFailed;
        failedEvent.Transport = SipTransportKind.Custom;
        failedEvent.Flow = flow;
        transport.QueueEvent(failedEvent);
        SipTransportEvent observed = manager.PollManagedTransportEvent();
        SipSendRequest retry = new SipSendRequest();
        retry.Bytes = new byte[] { 1 };
        retry.FlowId = "flow-event";
        SipSendResult rejected = manager.SendManaged(retry);

        Assert.Multiple(() =>
        {
            Assert.That(dialogEvent.Kind, Is.EqualTo(SipStackEventKind.Dialog));
            Assert.That(dialogEvent.DialogEvent, Is.Not.Null);
            Assert.That(dialogEvent.DialogEvent!.Kind, Is.EqualTo("created"));
            Assert.That(established.Success, Is.True,
                established.ErrorMessage);
            Assert.That(observed.Kind, Is.EqualTo(SipTransportEventKind.FlowFailed));
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.ErrorMessage, Does.Contain("flow has failed"));
        });
    }

    [Test]
    public void FormatterFailuresAlwaysSupplyTheProtocolExceptionReason()
    {
        SipMessageCodec codec = new SipMessageCodec();
        SipWriteResult nullMessage = codec.SerializeMessage(
            null!, new SipWriteOptions());
        SipMessage invalid = new SipMessage();
        SipWriteResult invalidStatus = codec.SerializeMessage(
            invalid, new SipWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(nullMessage.Success, Is.False);
            Assert.That(nullMessage.ErrorMessage, Is.Not.Null.And.Not.Empty);
            Assert.That(invalidStatus.Success, Is.False);
            Assert.That(invalidStatus.ErrorMessage, Is.Not.Null.And.Not.Empty);
            Assert.That(() => codec.MeasureMessage(
                    invalid, new SipWriteOptions()),
                Throws.TypeOf<SipProtocolException>().With.Message
                    .EqualTo(invalidStatus.ErrorMessage));
        });
    }

    [Test]
    public void BuiltInSecureListenersCompleteTlsAndWebSocketHandshakes()
    {
        SipHarnessCertificateFixture serverIdentity =
            new SipHarnessCertificateFixture("server.test");
        SipHarnessCertificateFixture clientIdentity =
            new SipHarnessCertificateFixture("client.test");
        TlsSipTransport tlsClient = new TlsSipTransport();
        TlsSipTransport tlsServer = new TlsSipTransport();
        WebSocketSipTransport webSocketClient =
            new WebSocketSipTransport();
        WebSocketSipTransport webSocketServer =
            new WebSocketSipTransport();
        try
        {
            byte[] serverPkcs12 = CreatePkcs12(serverIdentity);
            byte[] clientPkcs12 = CreatePkcs12(clientIdentity);
            SipEndpointAddress tlsEndpoint = BindTls(tlsServer,
                serverPkcs12, clientIdentity, null);
            BindTls(tlsClient, clientPkcs12, serverIdentity, "server.test");
            tlsClient.PollTransportEvent();
            tlsServer.PollTransportEvent();
            System.Threading.Tasks.Task<SipConnectionInfo> tlsConnecting =
                System.Threading.Tasks.Task.Run(() =>
                    tlsClient.ConnectTransport(tlsEndpoint));
            PollUntilComplete(tlsServer, tlsConnecting);
            SipConnectionInfo tlsConnection =
                tlsConnecting.GetAwaiter().GetResult();
            SipTransportEvent tlsAccepted = PollUntil(
                tlsClient, SipTransportEventKind.Connected);
            SendSecureRequest(tlsClient, tlsConnection.Id, "tls");
            PollUntil(tlsClient, SipTransportEventKind.MessageSent);
            SipTransportEvent tlsReceived = PollUntil(
                tlsServer, SipTransportEventKind.MessageReceived);

            SipEndpointAddress webSocketEndpoint = BindSecureWebSocket(
                webSocketServer, serverPkcs12, clientIdentity, null);
            BindSecureWebSocket(webSocketClient, clientPkcs12,
                serverIdentity, "server.test");
            webSocketClient.PollTransportEvent();
            webSocketServer.PollTransportEvent();
            System.Threading.Tasks.Task<SipConnectionInfo> webSocketConnecting =
                System.Threading.Tasks.Task.Run(() =>
                    webSocketClient.ConnectTransport(webSocketEndpoint));
            PollUntilComplete(webSocketServer, webSocketConnecting);
            SipConnectionInfo webSocketConnection =
                webSocketConnecting.GetAwaiter().GetResult();
            SipTransportEvent webSocketAccepted = PollUntil(
                webSocketClient, SipTransportEventKind.Connected);
            SendSecureRequest(webSocketClient, webSocketConnection.Id,
                "websocket");
            PollUntil(webSocketClient, SipTransportEventKind.MessageSent);
            SipTransportEvent webSocketReceived = PollUntil(
                webSocketServer, SipTransportEventKind.MessageReceived);

            Assert.Multiple(() =>
            {
                Assert.That(tlsConnection.Secure, Is.True);
                Assert.That(tlsAccepted.Connection!.Secure, Is.True);
                Assert.That(tlsReceived.Message!.Message!.Method,
                    Is.EqualTo(SipMethod.Options));
                Assert.That(webSocketConnection.Transport,
                    Is.EqualTo(SipTransportKind.SecureWebSocket));
                Assert.That(webSocketAccepted.Connection!.Secure, Is.True);
                Assert.That(webSocketReceived.Message!.Message!.Method,
                    Is.EqualTo(SipMethod.Options));
            });
        }
        catch (System.Security.Cryptography.CryptographicException error)
        {
            if (!System.OperatingSystem.IsMacOS())
            {
                throw;
            }
            Assert.Ignore("The macOS test sandbox could not create its "
                + "temporary certificate keychain: " + error.Message);
        }
        finally
        {
            tlsClient.CloseTransport();
            tlsServer.CloseTransport();
            webSocketClient.CloseTransport();
            webSocketServer.CloseTransport();
        }
    }

    static SipPublication Publication(string id, string body)
    {
        SipPublication value = new SipPublication();
        value.PublicationId = id;
        value.Body = System.Text.Encoding.ASCII.GetBytes(body);
        return value;
    }

    static SipCancellation Cancellation(string id)
    {
        SipCancellation value = new SipCancellation();
        value.Requested = true;
        value.Id = id;
        value.Reason = "coverage";
        return value;
    }

    static SipInviteRequest IncomingInvite(string callId, string branch)
    {
        SipInviteRequest value = new SipInviteRequest();
        value.Message = Parse("INVITE sip:service@example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP caller.example.com;branch=z9hG4bK-"
            + branch + "\r\nMax-Forwards: 70\r\n"
            + "From: Caller <sip:caller@example.com>;tag=caller-tag\r\n"
            + "To: Service <sip:service@example.com>\r\n"
            + "Call-ID: " + callId + "\r\nCSeq: 1 INVITE\r\n"
            + "Contact: <sip:caller@caller.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
        return value;
    }

    static SipInviteRequest OutgoingInvite(string callId, string branch)
    {
        SipInviteRequest value = new SipInviteRequest();
        value.Message = Parse("INVITE sip:callee@example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP relay.example.com;branch=z9hG4bK-"
            + branch + "\r\nMax-Forwards: 70\r\n"
            + "From: Relay <sip:relay@example.com>;tag=relay-tag\r\n"
            + "To: Callee <sip:callee@example.com>\r\n"
            + "Call-ID: " + callId + "\r\nCSeq: 1 INVITE\r\n"
            + "Contact: <sip:relay@relay.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
        return value;
    }

    static SipMessage InviteResponse(SipMessage invite, int status,
        string reason, string body, string tag)
    {
        SipHeader? callIdHeader = invite.Headers.FirstOrDefault(value =>
            string.Equals(value.Name, SipHeaderNames.CallId,
                StringComparison.OrdinalIgnoreCase));
        string callId = callIdHeader == null || callIdHeader.Value == null
            ? "missing" : callIdHeader.Value;
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(body);
        return Parse("SIP/2.0 " + status + " " + reason + "\r\n"
            + "Via: SIP/2.0/UDP caller.example.com;branch="
            + invite.ViaValues[0].Branch + "\r\n"
            + "From: Caller <sip:caller@example.com>;tag=caller-tag\r\n"
            + "To: Service <sip:service@example.com>;tag=" + tag + "\r\n"
            + "Call-ID: " + callId + "\r\nCSeq: 1 INVITE\r\n"
            + "Contact: <sip:service@service.example.com>\r\n"
            + (bytes.Length == 0 ? "" : "Content-Type: application/sdp\r\n")
            + "Content-Length: " + bytes.Length + "\r\n\r\n" + body);
    }

    static SipMessage LegRequest(string method, string callId, int cseq,
        string branch)
    {
        return Parse(method + " sip:service@example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP relay.example.com;branch=z9hG4bK-"
            + branch + "\r\nMax-Forwards: 70\r\n"
            + "From: Caller <sip:caller@example.com>;tag=caller-tag\r\n"
            + "To: Service <sip:service@example.com>;tag=service-tag\r\n"
            + "Call-ID: " + callId + "\r\nCSeq: " + cseq + " "
            + method + "\r\nContent-Length: 0\r\n\r\n");
    }

    static SipOfferAnswer CoverageLocalOffer(string text)
    {
        SipOfferAnswer value = new SipOfferAnswer();
        value.LocalOffer = System.Text.Encoding.UTF8.GetBytes(text);
        value.ContentType = CoverageSdpType();
        return value;
    }

    static SipOfferAnswer CoverageRemoteOffer(string text)
    {
        SipOfferAnswer value = new SipOfferAnswer();
        value.RemoteOffer = System.Text.Encoding.UTF8.GetBytes(text);
        value.ContentType = CoverageSdpType();
        return value;
    }

    static SipOfferAnswer CoverageLocalAnswer(string text)
    {
        SipOfferAnswer value = new SipOfferAnswer();
        value.LocalAnswer = System.Text.Encoding.UTF8.GetBytes(text);
        value.ContentType = CoverageSdpType();
        return value;
    }

    static SipMediaType CoverageSdpType()
    {
        SipMediaType value = new SipMediaType();
        value.Type = "application";
        value.Subtype = "sdp";
        return value;
    }

    static SipDialogSnapshot CreateDialog(SipDialogLayer layer, string callId)
    {
        return layer.CreateDialog(DialogInvite(callId), DialogResponse(callId),
            new SipDialogOptions());
    }

    static SipMessage DialogInvite(string callId)
    {
        return Parse("INVITE sip:bob@callee.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-dialog\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@example.com>;tag=local-tag\r\n"
            + "To: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: " + callId + "\r\nCSeq: 10 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage DialogResponse(string callId)
    {
        return Parse("SIP/2.0 200 OK\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-dialog\r\n"
            + "From: Alice <sip:alice@example.com>;tag=local-tag\r\n"
            + "To: Bob <sip:bob@example.com>;tag=remote-tag\r\n"
            + "Call-ID: " + callId + "\r\nCSeq: 10 INVITE\r\n"
            + "Contact: <sip:bob@server-old.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage RemoteUpdate(string callId, int cseq, string contact)
    {
        return Parse("UPDATE sip:alice@client.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP server.example.com;branch=z9hG4bK-remote-"
            + cseq + "\r\nMax-Forwards: 70\r\n"
            + "From: Bob <sip:bob@example.com>;tag=remote-tag\r\n"
            + "To: Alice <sip:alice@example.com>;tag=local-tag\r\n"
            + "Call-ID: " + callId + "\r\nCSeq: " + cseq
            + " UPDATE\r\nContact: <sip:bob@" + contact + ">\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage LocalOptions(string callId, int cseq,
        string requestHost, string? routeHost)
    {
        return Parse("OPTIONS sip:bob@" + requestHost + " SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-local-"
            + cseq + "\r\nMax-Forwards: 70\r\n"
            + "From: Alice <sip:alice@example.com>;tag=local-tag\r\n"
            + "To: Bob <sip:bob@example.com>;tag=remote-tag\r\n"
            + "Call-ID: " + callId + "\r\nCSeq: " + cseq
            + " OPTIONS\r\n" + (routeHost == null ? ""
                : "Route: <sip:bob@" + routeHost + ">\r\n")
            + "Content-Length: 0\r\n\r\n");
    }

    static SipConnectionInfo Connection(string id)
    {
        SipEndpointAddress address = new SipEndpointAddress();
        address.Host = "192.0.2.40";
        address.Port = 5060;
        address.Transport = SipTransportKind.Custom;
        SipConnectionInfo value = new SipConnectionInfo();
        value.Id = id;
        value.State = SipConnectionState.Connected;
        value.Transport = SipTransportKind.Custom;
        value.LocalAddress = address;
        value.RemoteAddress = address;
        return value;
    }

    static SipEndpointAddress BindTls(TlsSipTransport transport,
        byte[] localCertificate,
        SipHarnessCertificateFixture trusted, string? serverName)
    {
        SipTransportOptions options = SecureTransportOptions(
            SipTransportKind.Tls);
        SipTlsOptions tls = TlsOptions(
            localCertificate, trusted, serverName);
        return transport.BindTransport(
            options, tls, new SipWebSocketOptions());
    }

    static SipEndpointAddress BindSecureWebSocket(
        WebSocketSipTransport transport,
        byte[] localCertificate,
        SipHarnessCertificateFixture trusted, string? serverName)
    {
        SipTransportOptions options = SecureTransportOptions(
            SipTransportKind.SecureWebSocket);
        SipWebSocketOptions webSocket = new SipWebSocketOptions();
        webSocket.Path = "/sip";
        webSocket.AllowBinaryFrames = true;
        webSocket.MaxMessageBytes = 8192;
        return transport.BindTransport(
            options, TlsOptions(localCertificate, trusted, serverName),
            webSocket);
    }

    static SipTransportOptions SecureTransportOptions(
        SipTransportKind kind)
    {
        SipEndpointAddress local = new SipEndpointAddress();
        local.Host = "127.0.0.1";
        local.Port = 0;
        local.Transport = kind;
        local.Secure = true;
        SipTransportOptions options = new SipTransportOptions();
        options.Kind = kind;
        options.LocalAddress = local;
        options.MaxQueueCount = 16;
        options.IdleTimeoutMilliseconds = 60000;
        return options;
    }

    static SipTlsOptions TlsOptions(
        byte[] localCertificate,
        SipHarnessCertificateFixture trusted, string? serverName)
    {
        SipTlsOptions value = new SipTlsOptions();
        value.ServerName = serverName;
        value.CertificateBytes = localCertificate;
        value.PrivateKeyBytes = new byte[0];
        value.TrustedRoots = new byte[][]
        {
            trusted.Identity.CertificateBytes
        };
        value.ValidatePeer = true;
        return value;
    }

    static byte[] CreatePkcs12(SipHarnessCertificateFixture source)
    {
        string directory = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "sip-coverage-"
                + System.Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(directory);
        string certificate = System.IO.Path.Combine(directory, "identity.pem");
        string privateKey = System.IO.Path.Combine(directory, "identity.key");
        string output = System.IO.Path.Combine(directory, "identity.p12");
        try
        {
            System.IO.File.WriteAllBytes(certificate, source.CertificateBytes);
            System.IO.File.WriteAllBytes(privateKey, source.PrivateKeyBytes);
            System.Diagnostics.ProcessStartInfo start =
                new System.Diagnostics.ProcessStartInfo();
            start.FileName = "openssl";
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            start.ArgumentList.Add("pkcs12");
            start.ArgumentList.Add("-export");
            start.ArgumentList.Add("-out");
            start.ArgumentList.Add(output);
            start.ArgumentList.Add("-inkey");
            start.ArgumentList.Add(privateKey);
            start.ArgumentList.Add("-in");
            start.ArgumentList.Add(certificate);
            start.ArgumentList.Add("-passout");
            start.ArgumentList.Add("pass:");
            using System.Diagnostics.Process? process =
                System.Diagnostics.Process.Start(start);
            if (process == null || !process.WaitForExit(10000)
                || process.ExitCode != 0)
            {
                string error = process == null
                    ? "could not start"
                    : process.StandardError.ReadToEnd();
                throw new System.InvalidOperationException(
                    "OpenSSL PKCS#12 generation failed: " + error);
            }
            return System.IO.File.ReadAllBytes(output);
        }
        finally
        {
            System.IO.Directory.Delete(directory, true);
        }
    }

    static void SendSecureRequest(
        ISipTransport transport, string? connectionId, string correlationId)
    {
        SipSendRequest request = new SipSendRequest();
        request.Bytes = System.Text.Encoding.ASCII.GetBytes(
            "OPTIONS sips:service@server.test SIP/2.0\r\n"
                + "Content-Length: 0\r\n\r\n");
        request.ConnectionId = connectionId;
        request.CorrelationId = correlationId;
        SipSendResult result = transport.SendTransport(request);
        Assert.That(result.Success, Is.True, result.ErrorMessage);
    }

    static void PollUntilComplete(
        ISipTransport server, System.Threading.Tasks.Task task)
    {
        long deadline = System.Environment.TickCount64 + 5000;
        while (!task.IsCompleted && System.Environment.TickCount64 < deadline)
        {
            server.PollTransportEvent();
            System.Threading.Thread.Sleep(2);
        }
        Assert.That(task.IsCompleted, Is.True,
            "The secure connection operation timed out.");
    }

    static SipTransportEvent PollUntil(
        ISipTransport transport, SipTransportEventKind kind)
    {
        long deadline = System.Environment.TickCount64 + 5000;
        SipTransportEvent value = transport.PollTransportEvent();
        while (value.Kind != kind
            && System.Environment.TickCount64 < deadline)
        {
            System.Threading.Thread.Sleep(2);
            value = transport.PollTransportEvent();
        }
        Assert.That(value.Kind, Is.EqualTo(kind), value.ErrorMessage);
        return value;
    }

    static SipSendRequest ManagedRequest(string correlationId)
    {
        SipEndpointAddress remote = new SipEndpointAddress();
        remote.Host = "192.0.2.41";
        remote.Port = 5060;
        remote.Transport = SipTransportKind.Custom;
        SipSendRequest value = new SipSendRequest();
        value.Bytes = new byte[] { 1 };
        value.Transport = SipTransportKind.Custom;
        value.RemoteAddress = remote;
        value.CorrelationId = correlationId;
        return value;
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete),
            result.ErrorMessage);
        return result.Message!;
    }

    static void DrainInvite(SipInviteSession session)
    {
        while (session.PollInviteEvent().State != SipInviteSessionState.None)
        {
        }
    }

    static void DrainManaged(SipTransportManager manager)
    {
        while (manager.PollManagedTransportEvent().Kind
            != SipTransportEventKind.None)
        {
        }
    }

    static void Stop(SipEndpoint endpoint)
    {
        SipCancellation cancellation = new SipCancellation();
        cancellation.Requested = true;
        endpoint.StopEndpoint(cancellation);
    }
}

class CoverageExtension : ISipExtension
{
    string method;

    public CoverageExtension(string methodValue)
    {
        method = methodValue;
    }

    public SipCapabilitySet GetExtensionCapabilities()
    {
        SipCapabilitySet value = new SipCapabilitySet();
        value.Methods = new string[] { method };
        return value;
    }

    public SipExtensionResult ProcessExtensionRequest(SipMessage message)
    {
        return new SipExtensionResult();
    }
}

class CoverageAuthenticationClient : ISipAuthenticationClient
{
    public SipAuthenticationResult AuthorizeRequest(
        SipAuthenticationRequest request)
    {
        return new SipAuthenticationResult();
    }

    public SipAuthenticationResult VerifyAuthenticationInfo(
        SipAuthenticationRequest request)
    {
        return new SipAuthenticationResult();
    }
}
