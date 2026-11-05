namespace sip.Tests;

public class SipDialogManagementTests
{
    [Test]
    public void MessageLookupAndQueuesExposeDetachedCorrelatedSnapshots()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogOptions options = Options();
        options.CorrelationId = "dialog-operation";
        SipDialogSnapshot created = layer.CreateDialog(
            Invite("management-call@example.com"),
            Response(200, "management-call@example.com", "remote-tag"), options);

        SipDialogEvent createdEvent = layer.PollDialogEvent();
        SipDialogSnapshot queued = layer.PollDialogSnapshot();
        SipDialogSnapshot matched = layer.FindDialogByMessage(
            RemoteRequest(SipMethods.Options, "management-call@example.com",
                "remote-tag", 11));
        queued.RemoteTarget!.Host = "changed.example.com";
        SipDialogSnapshot live = layer.FindDialog(created.Id!);

        Assert.Multiple(() =>
        {
            Assert.That(created.CorrelationId, Is.EqualTo("dialog-operation"));
            Assert.That(createdEvent.CorrelationId,
                Is.EqualTo("dialog-operation"));
            Assert.That(createdEvent.Snapshot, Is.Not.SameAs(queued));
            Assert.That(queued.Id!.CallId, Is.EqualTo("management-call@example.com"));
            Assert.That(queued.CorrelationId, Is.EqualTo("dialog-operation"));
            Assert.That(matched.Id!.RemoteTag, Is.EqualTo("remote-tag"));
            Assert.That(live.RemoteTarget!.Host, Is.EqualTo("server.example.com"));
            Assert.That(layer.PollDialogEvent().Kind, Is.Null);
            Assert.That(layer.PollDialogSnapshot().State,
                Is.EqualTo(SipDialogState.None));
        });
    }

    [Test]
    public void TransactionAndAuthenticationAssociationsCanBeMutatedAndLookedUp()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = layer.CreateDialog(
            Invite("association-call@example.com"),
            Response(200, "association-call@example.com", "remote-tag"), Options());
        Drain(layer);

        layer.AssociateTransaction(dialog.Id!, "tx-one");
        SipDialogEvent transactionEvent = layer.PollDialogEvent();
        SipDialogSnapshot transactionSnapshot = layer.PollDialogSnapshot();
        layer.AssociateTransaction(dialog.Id!, "tx-one");
        layer.AssociateAuthenticationContext(dialog.Id!, "auth-one");
        layer.AssociateAuthenticationContext(dialog.Id!, "auth-two");
        SipDialogSnapshot byTransaction = layer.FindDialogByTransaction("tx-one");
        layer.DissociateTransaction(dialog.Id!, "tx-one");
        layer.ClearAuthenticationContext(dialog.Id!);
        SipDialogSnapshot cleared = layer.FindDialog(dialog.Id!);

        Assert.Multiple(() =>
        {
            Assert.That(transactionEvent.Kind,
                Is.EqualTo("transaction-associated"));
            Assert.That(transactionSnapshot.TransactionIds,
                Is.EqualTo(new string[] { "tx-one" }));
            Assert.That(byTransaction.AuthenticationContextId,
                Is.EqualTo("auth-two"));
            Assert.That(cleared.TransactionIds, Is.Empty);
            Assert.That(cleared.AuthenticationContextId, Is.Null);
            Assert.Throws<SipDialogException>(() =>
                layer.FindDialogByTransaction("tx-one"));
            Assert.That(DrainEvents(layer), Is.EqualTo(4));
            Assert.That(DrainSnapshots(layer), Is.EqualTo(4));
        });
    }

    [Test]
    public void TransactionAssociationLimitsAndOwnershipFailuresAreAtomic()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogOptions options = Options();
        options.MaxTransactionCount = 1;
        SipDialogSnapshot first = layer.CreateDialog(
            Invite("first-association@example.com"),
            Response(200, "first-association@example.com", "first-tag"), options);
        SipDialogSnapshot second = layer.CreateDialog(
            Invite("second-association@example.com"),
            Response(200, "second-association@example.com", "second-tag"), options);
        Drain(layer);

        layer.AssociateTransaction(first.Id!, "tx-owned");
        layer.AssociateTransaction(first.Id!, "tx-owned");
        SipDialogException full = Assert.Throws<SipDialogException>(() =>
            layer.AssociateTransaction(first.Id!, "tx-second"))!;
        SipDialogException owned = Assert.Throws<SipDialogException>(() =>
            layer.AssociateTransaction(second.Id!, "tx-owned"))!;
        Assert.Throws<ArgumentException>(() =>
            layer.AssociateTransaction(second.Id!, "not an id"));

        Assert.Multiple(() =>
        {
            Assert.That(full.Message, Does.Contain("limit"));
            Assert.That(owned.Message, Does.Contain("another dialog"));
            Assert.That(layer.FindDialog(first.Id!).TransactionIds,
                Is.EqualTo(new string[] { "tx-owned" }));
            Assert.That(layer.FindDialog(second.Id!).TransactionIds, Is.Empty);
            Assert.That(DrainEvents(layer), Is.EqualTo(1));
            Assert.That(DrainSnapshots(layer), Is.EqualTo(1));
        });
    }

    [Test]
    public void QueuePressureRejectsMutationsWithoutPartialStateChanges()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogOptions options = Options();
        options.MaxEventCount = 1;
        SipDialogSnapshot dialog = layer.CreateDialog(
            Invite("pressure-call@example.com"),
            Response(200, "pressure-call@example.com", "remote-tag"), options);

        Assert.Throws<SipDialogException>(() => layer.CreateUsage(dialog.Id!));
        layer.PollDialogEvent();
        Assert.Throws<SipDialogException>(() => layer.CreateUsage(dialog.Id!));
        layer.PollDialogSnapshot();
        string usageId = layer.CreateUsage(dialog.Id!);
        SipMessage update = RemoteRequest(SipMethods.Options,
            "pressure-call@example.com", "remote-tag", 11);
        Assert.Throws<SipDialogException>(() =>
            layer.UpdateDialog(dialog.Id!, update));
        SipDialogSnapshot unchanged = layer.FindDialog(dialog.Id!);

        Assert.Multiple(() =>
        {
            Assert.That(unchanged.UsageIds,
                Is.EqualTo(new string[] { dialog.UsageIds[0], usageId }));
            Assert.That(unchanged.RemoteCSeq, Is.EqualTo(-1));
        });

        Drain(layer);
        SipDialogSnapshot changed = layer.UpdateDialog(dialog.Id!, update);
        Assert.That(changed.RemoteCSeq, Is.EqualTo(11));
    }

    [Test]
    public void ForkFailurePreflightProtectsEveryAffectedDialog()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogOptions options = Options();
        options.MaxEventCount = 2;
        SipMessage invite = Invite("fork-pressure@example.com");
        SipDialogSnapshot first = layer.CreateDialog(invite,
            Response(180, "fork-pressure@example.com", "first-tag"), options);
        SipDialogSnapshot second = layer.CreateDialog(invite,
            Response(183, "fork-pressure@example.com", "second-tag"), options);
        Drain(layer);
        string siblingUsage = layer.CreateUsage(first.Id!);

        Assert.Throws<SipDialogException>(() => layer.UpdateDialog(first.Id!,
            Response(486, "fork-pressure@example.com", "first-tag")));
        SipDialogSnapshot unchangedFirst = layer.FindDialog(first.Id!);
        SipDialogSnapshot unchangedSecond = layer.FindDialog(second.Id!);
        Drain(layer);

        SipDialogSnapshot failed = layer.UpdateDialog(first.Id!,
            Response(486, "fork-pressure@example.com", "first-tag"));

        Assert.Multiple(() =>
        {
            Assert.That(unchangedFirst.UsageIds, Has.Length.EqualTo(2));
            Assert.That(unchangedSecond.UsageIds, Has.Length.EqualTo(1));
            Assert.That(failed.State, Is.EqualTo(SipDialogState.Early));
            Assert.That(failed.UsageIds,
                Is.EqualTo(new string[] { siblingUsage }));
            Assert.Throws<SipDialogException>(() => layer.FindDialog(second.Id!));
            Assert.That(DrainEvents(layer), Is.EqualTo(2));
            Assert.That(DrainSnapshots(layer), Is.EqualTo(2));
        });
    }

    [Test]
    public void LastUsageTerminationCleansAssociationsFromFinalSnapshot()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = layer.CreateDialog(
            Invite("cleanup-call@example.com"),
            Response(200, "cleanup-call@example.com", "remote-tag"), Options());
        string usageId = dialog.UsageIds[0];
        Drain(layer);
        layer.AssociateTransaction(dialog.Id!, "tx-cleanup");
        layer.AssociateAuthenticationContext(dialog.Id!, "auth-cleanup");
        Drain(layer);

        layer.TerminateUsage(dialog.Id!, usageId, new SipCancellation());
        SipDialogEvent terminated = layer.PollDialogEvent();
        SipDialogSnapshot final = layer.PollDialogSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(terminated.Kind, Is.EqualTo("terminated"));
            Assert.That(terminated.Snapshot!.TransactionIds, Is.Empty);
            Assert.That(terminated.Snapshot.AuthenticationContextId, Is.Null);
            Assert.That(final.State, Is.EqualTo(SipDialogState.Terminated));
            Assert.That(final.TransactionIds, Is.Empty);
            Assert.That(final.AuthenticationContextId, Is.Null);
            Assert.Throws<SipDialogException>(() =>
                layer.FindDialogByTransaction("tx-cleanup"));
            Assert.Throws<SipDialogException>(() => layer.FindDialog(dialog.Id!));
        });
    }

    [Test]
    public void InvalidManagementOptionsAndLookupsDoNotCreateState()
    {
        SipMessage invite = Invite("invalid-management@example.com");
        SipMessage response = Response(
            200, "invalid-management@example.com", "remote-tag");
        SipDialogOptions noTransactions = Options();
        noTransactions.MaxTransactionCount = 0;
        SipDialogOptions noEvents = Options();
        noEvents.MaxEventCount = 0;
        SipDialogOptions longCorrelation = Options();
        longCorrelation.CorrelationId = new string(
            'x', SipDefaults.MaxHeaderLineBytes + 1);
        SipDialogLayer layer = new SipDialogLayer();

        Assert.Throws<ArgumentException>(() =>
            layer.CreateDialog(invite, response, noTransactions));
        Assert.Throws<ArgumentException>(() =>
            layer.CreateDialog(invite, response, noEvents));
        Assert.Throws<ArgumentException>(() =>
            layer.CreateDialog(invite, response, longCorrelation));
        Assert.Throws<ArgumentException>(() =>
            layer.FindDialogByMessage(new SipMessage()));
        Assert.Throws<SipDialogException>(() => layer.FindDialogByMessage(
            RemoteRequest(SipMethods.Options, "missing@example.com",
                "remote-tag", 11)));

        Assert.Multiple(() =>
        {
            Assert.That(layer.PollDialogEvent().Kind, Is.Null);
            Assert.That(layer.PollDialogSnapshot().State,
                Is.EqualTo(SipDialogState.None));
        });
    }

    static SipDialogOptions Options()
    {
        SipDialogOptions options = new SipDialogOptions();
        options.MaxDialogCount = 16;
        options.MaxUsageCount = 8;
        options.MaxTransactionCount = 8;
        options.MaxEventCount = 64;
        return options;
    }

    static SipMessage Invite(string callId)
    {
        return Parse("INVITE sip:bob@callee.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-management\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>\r\nCall-ID: " + callId
            + "\r\nCSeq: 10 INVITE\r\nContact: <sip:alice@client.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Response(int status, string callId, string remoteTag)
    {
        string contact = status < 300
            ? "Contact: <sip:bob@server.example.com>\r\n" : "";
        return Parse("SIP/2.0 " + status.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " Dialog\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-management\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>;tag=" + remoteTag + "\r\n"
            + "Call-ID: " + callId + "\r\nCSeq: 10 INVITE\r\n"
            + contact + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage RemoteRequest(
        string method, string callId, string remoteTag, int cseq)
    {
        return Parse(method + " sip:alice@client.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP server.example.com:5060;branch=z9hG4bK-remote\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Bob <sip:bob@bob.example.com>;tag=" + remoteTag + "\r\n"
            + "To: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "Call-ID: " + callId + "\r\nCSeq: "
            + cseq.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " " + method + "\r\nContent-Length: 0\r\n\r\n");
    }

    static void Drain(SipDialogLayer layer)
    {
        DrainEvents(layer);
        DrainSnapshots(layer);
    }

    static int DrainEvents(SipDialogLayer layer)
    {
        int count = 0;
        while (layer.PollDialogEvent().Kind != null)
        {
            count = count + 1;
        }
        return count;
    }

    static int DrainSnapshots(SipDialogLayer layer)
    {
        int count = 0;
        while (layer.PollDialogSnapshot().State != SipDialogState.None)
        {
            count = count + 1;
        }
        return count;
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete),
            result.ErrorMessage);
        return result.Message!;
    }
}
