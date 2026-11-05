namespace sip.Tests;

public class SipDialogForkingTests
{
    [Test]
    public void ForkedEarlyDialogsConfirmAndRefreshOnlyTheMatchingDialog()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipMessage invite = Invite(10);
        SipDialogSnapshot first = layer.CreateDialog(invite,
            Response(180, "first-tag", 10, SipMethods.Invite,
                "first-early.example.com",
                "Record-Route: <sip:first-early-route.example.com;lr>\r\n"),
            new SipDialogOptions());
        SipDialogSnapshot second = layer.CreateDialog(invite,
            Response(183, "second-tag", 10, SipMethods.Invite,
                "second-early.example.com",
                "Record-Route: <sip:second-early-route.example.com;lr>\r\n"),
            new SipDialogOptions());
        layer.CreateRequest(first.Id!, SipMethods.Options);

        SipDialogSnapshot confirmed = layer.UpdateDialog(first.Id!,
            Response(200, "first-tag", 10, SipMethods.Invite,
                "first-final.example.com",
                "Record-Route: <sip:first-final-route.example.com;lr>\r\n"));
        SipMessage ack = layer.CreateRequest(first.Id!, SipMethods.Ack);

        Assert.Multiple(() =>
        {
            Assert.That(confirmed.State, Is.EqualTo(SipDialogState.Confirmed));
            Assert.That(confirmed.LocalCSeq, Is.EqualTo(11));
            Assert.That(ack.CSeq!.SequenceNumber, Is.EqualTo(10));
            Assert.That(layer.FindDialog(first.Id!).LocalCSeq, Is.EqualTo(11));
            Assert.That(confirmed.RemoteTarget!.Host,
                Is.EqualTo("first-final.example.com"));
            Assert.That(RouteHosts(confirmed.RouteSet),
                Is.EqualTo(new string[] { "first-final-route.example.com" }));
            Assert.That(layer.FindDialog(second.Id!).State,
                Is.EqualTo(SipDialogState.Early));
            Assert.That(layer.FindDialog(second.Id!).RemoteTarget!.Host,
                Is.EqualTo("second-early.example.com"));
            Assert.That(RouteHosts(layer.FindDialog(second.Id!).RouteSet),
                Is.EqualTo(new string[] { "second-early-route.example.com" }));
        });
    }

    [Test]
    public void RepeatedFinalResponseReplacesEarlyStateWithoutDuplicatingUsage()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipMessage invite = Invite(10);
        SipDialogSnapshot early = layer.CreateDialog(invite,
            Response(183, "branch-tag", 10, SipMethods.Invite,
                "early.example.com",
                "Record-Route: <sip:early-route.example.com;lr>\r\n"),
            new SipDialogOptions());
        layer.PollDialogEvent();
        SipMessage finalResponse = Response(200, "branch-tag", 10,
            SipMethods.Invite, "final.example.com",
            "Record-Route: <sip:final-route.example.com;lr>\r\n");

        SipDialogSnapshot confirmed = layer.CreateDialog(
            invite, finalResponse, new SipDialogOptions());
        SipDialogEvent changed = layer.PollDialogEvent();
        SipDialogSnapshot repeated = layer.CreateDialog(
            invite, finalResponse, new SipDialogOptions());
        SipDialogEvent observed = layer.PollDialogEvent();

        Assert.Multiple(() =>
        {
            Assert.That(confirmed.State, Is.EqualTo(SipDialogState.Confirmed));
            Assert.That(confirmed.UsageIds, Is.EqualTo(early.UsageIds));
            Assert.That(repeated.UsageIds, Is.EqualTo(early.UsageIds));
            Assert.That(repeated.RemoteTarget!.Host, Is.EqualTo("final.example.com"));
            Assert.That(RouteHosts(repeated.RouteSet),
                Is.EqualTo(new string[] { "final-route.example.com" }));
            Assert.That(changed.Kind, Is.EqualTo("state-changed"));
            Assert.That(changed.UsageId, Is.EqualTo(early.UsageIds[0]));
            Assert.That(observed.Kind, Is.EqualTo("updated"));
            Assert.That(observed.UsageId, Is.EqualTo(early.UsageIds[0]));
            Assert.That(layer.PollDialogEvent().Kind, Is.Null);
        });
    }

    [Test]
    public void MultipleSuccessResponsesCreateDialogsWithIndependentAckAndByeCleanup()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipMessage invite = Invite(10);
        SipDialogSnapshot first = layer.CreateDialog(invite,
            Response(200, "first-tag", 10, SipMethods.Invite,
                "first.example.com", ""), new SipDialogOptions());
        SipDialogSnapshot second = layer.CreateDialog(invite,
            Response(200, "second-tag", 10, SipMethods.Invite,
                "second.example.com", ""), new SipDialogOptions());

        SipMessage firstAck = layer.CreateRequest(first.Id!, SipMethods.Ack);
        SipMessage secondAck = layer.CreateRequest(second.Id!, SipMethods.Ack);
        SipMessage firstBye = layer.CreateRequest(first.Id!, SipMethods.Bye);
        Assert.Throws<ArgumentException>(() => layer.UpdateDialog(first.Id!,
            Response(200, "first-tag", 99, SipMethods.Bye, "", "")));
        SipDialogSnapshot firstEnded = layer.UpdateDialog(first.Id!,
            Response(200, "first-tag", firstBye.CSeq!.SequenceNumber,
                SipMethods.Bye, "", ""));
        SipDialogSnapshot secondAfterAck = layer.UpdateDialog(second.Id!,
            RemoteRequest(SipMethods.Ack, "second-tag", "caller-tag", 10,
                "client.example.com"));

        Assert.Multiple(() =>
        {
            Assert.That(firstAck.RequestUri!.Host, Is.EqualTo("first.example.com"));
            Assert.That(secondAck.RequestUri!.Host, Is.EqualTo("second.example.com"));
            Assert.That(firstAck.CSeq!.SequenceNumber, Is.EqualTo(10));
            Assert.That(secondAck.CSeq!.SequenceNumber, Is.EqualTo(10));
            Assert.That(firstEnded.State, Is.EqualTo(SipDialogState.Terminated));
            Assert.Throws<SipDialogException>(() => layer.FindDialog(first.Id!));
            Assert.That(secondAfterAck.State, Is.EqualTo(SipDialogState.Confirmed));
            Assert.That(layer.FindDialog(second.Id!).LocalCSeq, Is.EqualTo(10));
        });

        SipDialogSnapshot secondEnded = layer.UpdateDialog(second.Id!,
            RemoteRequest(SipMethods.Bye, "second-tag", "caller-tag", 11,
                "client.example.com"));

        Assert.Multiple(() =>
        {
            Assert.That(secondEnded.State, Is.EqualTo(SipDialogState.Terminated));
            Assert.Throws<SipDialogException>(() => layer.FindDialog(second.Id!));
        });
    }

    [Test]
    public void ConfirmedForksRetainSiblingUsagesAfterIndependentLocalBye()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipMessage invite = Invite(10);
        SipDialogSnapshot first = layer.CreateDialog(invite,
            Response(200, "first-tag", 10, SipMethods.Invite,
                "first.example.com", ""), new SipDialogOptions());
        SipDialogSnapshot second = layer.CreateDialog(invite,
            Response(200, "second-tag", 10, SipMethods.Invite,
                "second.example.com", ""), new SipDialogOptions());
        string firstSibling = layer.CreateUsage(first.Id!);
        string secondSibling = layer.CreateUsage(second.Id!);
        layer.PollDialogEvent();
        layer.PollDialogEvent();
        layer.PollDialogEvent();
        layer.PollDialogEvent();

        SipMessage firstAck = layer.CreateRequest(first.Id!, SipMethods.Ack);
        SipMessage secondAck = layer.CreateRequest(second.Id!, SipMethods.Ack);
        SipMessage firstBye = layer.CreateRequest(first.Id!, SipMethods.Bye);
        SipMessage secondBye = layer.CreateRequest(second.Id!, SipMethods.Bye);
        layer.PollDialogEvent();
        layer.PollDialogEvent();
        layer.PollDialogEvent();
        layer.PollDialogEvent();
        SipDialogSnapshot firstAfterBye = layer.UpdateDialog(first.Id!,
            Response(200, "first-tag", firstBye.CSeq!.SequenceNumber,
                SipMethods.Bye, "", ""));
        SipDialogEvent firstEnded = layer.PollDialogEvent();
        SipDialogSnapshot secondAfterBye = layer.UpdateDialog(second.Id!,
            Response(200, "second-tag", secondBye.CSeq!.SequenceNumber,
                SipMethods.Bye, "", ""));
        SipDialogEvent secondEnded = layer.PollDialogEvent();

        layer.TerminateUsage(first.Id!, firstSibling, new SipCancellation());
        layer.PollDialogEvent();
        SipDialogSnapshot survivingFork = layer.FindDialog(second.Id!);
        layer.TerminateUsage(second.Id!, secondSibling, new SipCancellation());

        Assert.Multiple(() =>
        {
            Assert.That(firstAck.CSeq!.SequenceNumber, Is.EqualTo(10));
            Assert.That(secondAck.CSeq!.SequenceNumber, Is.EqualTo(10));
            Assert.That(firstBye.CSeq.SequenceNumber, Is.EqualTo(11));
            Assert.That(secondBye.CSeq.SequenceNumber, Is.EqualTo(11));
            Assert.That(firstAfterBye.State, Is.EqualTo(SipDialogState.Confirmed));
            Assert.That(firstAfterBye.UsageIds,
                Is.EqualTo(new string[] { firstSibling }));
            Assert.That(secondAfterBye.State, Is.EqualTo(SipDialogState.Confirmed));
            Assert.That(secondAfterBye.UsageIds,
                Is.EqualTo(new string[] { secondSibling }));
            Assert.That(firstEnded.Kind, Is.EqualTo("usage-terminated"));
            Assert.That(firstEnded.UsageId, Is.EqualTo(first.UsageIds[0]));
            Assert.That(secondEnded.Kind, Is.EqualTo("usage-terminated"));
            Assert.That(secondEnded.UsageId, Is.EqualTo(second.UsageIds[0]));
            Assert.Throws<SipDialogException>(() => layer.FindDialog(first.Id!));
            Assert.That(survivingFork.UsageIds,
                Is.EqualTo(new string[] { secondSibling }));
            Assert.Throws<SipDialogException>(() => layer.FindDialog(second.Id!));
        });
    }

    [Test]
    public void UnmatchedFinalInviteFailureTerminatesEveryAssociatedEarlyFork()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot first = layer.CreateDialog(Invite(10),
            Response(180, "first-tag", 10, SipMethods.Invite,
                "first.example.com", ""), new SipDialogOptions());
        SipDialogSnapshot second = layer.CreateDialog(Invite(10),
            Response(183, "second-tag", 10, SipMethods.Invite,
                "second.example.com", ""), new SipDialogOptions());
        SipDialogSnapshot survivor = layer.CreateDialog(Invite(20),
            Response(200, "survivor-tag", 20, SipMethods.Invite,
                "survivor.example.com", ""), new SipDialogOptions());
        layer.PollDialogEvent();
        layer.PollDialogEvent();
        layer.PollDialogEvent();

        SipDialogSnapshot ended = layer.UpdateDialog(
            Id("aggregate-final-tag"),
            Response(486, "aggregate-final-tag", 10, SipMethods.Invite, "", ""));
        SipDialogEvent firstEvent = layer.PollDialogEvent();
        SipDialogEvent secondEvent = layer.PollDialogEvent();

        Assert.Multiple(() =>
        {
            Assert.That(ended.State, Is.EqualTo(SipDialogState.Terminated));
            Assert.Throws<SipDialogException>(() => layer.FindDialog(first.Id!));
            Assert.Throws<SipDialogException>(() => layer.FindDialog(second.Id!));
            Assert.That(layer.FindDialog(survivor.Id!).State,
                Is.EqualTo(SipDialogState.Confirmed));
            Assert.That(new string?[]
            {
                firstEvent.DialogId!.RemoteTag, secondEvent.DialogId!.RemoteTag
            }, Is.EquivalentTo(new string?[] { "first-tag", "second-tag" }));
            Assert.That(firstEvent.State, Is.EqualTo(SipDialogState.Terminated));
            Assert.That(secondEvent.State, Is.EqualTo(SipDialogState.Terminated));
            Assert.That(layer.PollDialogEvent().Kind, Is.Null);
        });
    }

    [Test]
    public void ForkFailureRetainsEachDialogThatHasAnotherUsage()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipMessage invite = Invite(10);
        SipDialogSnapshot first = layer.CreateDialog(invite,
            Response(180, "first-tag", 10, SipMethods.Invite,
                "first.example.com", ""), new SipDialogOptions());
        SipDialogSnapshot second = layer.CreateDialog(invite,
            Response(183, "second-tag", 10, SipMethods.Invite,
                "second.example.com", ""), new SipDialogOptions());
        string firstSibling = layer.CreateUsage(first.Id!);
        string secondSibling = layer.CreateUsage(second.Id!);
        layer.PollDialogEvent();
        layer.PollDialogEvent();
        layer.PollDialogEvent();
        layer.PollDialogEvent();

        SipDialogSnapshot selected = layer.UpdateDialog(Id("final-tag"),
            Response(486, "final-tag", 10, SipMethods.Invite, "", ""));
        SipDialogEvent firstEnded = layer.PollDialogEvent();
        SipDialogEvent secondEnded = layer.PollDialogEvent();
        SipDialogSnapshot firstRetained = layer.FindDialog(first.Id!);
        SipDialogSnapshot secondRetained = layer.FindDialog(second.Id!);

        layer.TerminateUsage(first.Id!, firstSibling, new SipCancellation());
        layer.TerminateUsage(second.Id!, secondSibling, new SipCancellation());

        Assert.Multiple(() =>
        {
            Assert.That(selected.State, Is.EqualTo(SipDialogState.Early));
            Assert.That(firstRetained.State, Is.EqualTo(SipDialogState.Early));
            Assert.That(firstRetained.UsageIds,
                Is.EqualTo(new string[] { firstSibling }));
            Assert.That(secondRetained.State, Is.EqualTo(SipDialogState.Early));
            Assert.That(secondRetained.UsageIds,
                Is.EqualTo(new string[] { secondSibling }));
            Assert.That(firstEnded.Kind, Is.EqualTo("usage-terminated"));
            Assert.That(secondEnded.Kind, Is.EqualTo("usage-terminated"));
            Assert.That(new string?[] { firstEnded.UsageId, secondEnded.UsageId },
                Is.EquivalentTo(new string?[]
                {
                    first.UsageIds[0], second.UsageIds[0]
                }));
            Assert.Throws<SipDialogException>(() => layer.FindDialog(first.Id!));
            Assert.Throws<SipDialogException>(() => layer.FindDialog(second.Id!));
        });
    }

    [Test]
    public void UnmatchedInDialogRequestIsRejectedWithoutChangingOtherDialogs()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = layer.CreateDialog(Invite(10),
            Response(200, "known-tag", 10, SipMethods.Invite,
                "known.example.com", ""), new SipDialogOptions());
        layer.PollDialogEvent();

        SipDialogException unmatched = Assert.Throws<SipDialogException>(() =>
            layer.UpdateDialog(Id("missing-tag"),
                RemoteRequest(SipMethods.Bye, "missing-tag", "caller-tag", 11,
                    "client.example.com")))!;

        Assert.Multiple(() =>
        {
            Assert.That(unmatched.State, Is.EqualTo(SipDialogState.Terminated));
            Assert.That(unmatched.DialogId!.RemoteTag, Is.EqualTo("missing-tag"));
            Assert.That(layer.FindDialog(dialog.Id!).State,
                Is.EqualTo(SipDialogState.Confirmed));
            Assert.That(layer.PollDialogEvent().Kind, Is.Null);
        });
    }

    [Test]
    public void UasCannotSendByeOnEarlyDialog()
    {
        SipDialogOptions options = new SipDialogOptions();
        options.Role = SipDialogRole.Uas;
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = layer.CreateDialog(Invite(10),
            Response(180, "server-tag", 10, SipMethods.Invite,
                "server.example.com", ""), options);
        layer.PollDialogEvent();

        SipDialogException failure = Assert.Throws<SipDialogException>(() =>
            layer.CreateRequest(dialog.Id!, SipMethods.Bye))!;

        Assert.Multiple(() =>
        {
            Assert.That(failure.State, Is.EqualTo(SipDialogState.Early));
            Assert.That(layer.FindDialog(dialog.Id!).State,
                Is.EqualTo(SipDialogState.Early));
            Assert.That(layer.FindDialog(dialog.Id!).LocalCSeq, Is.EqualTo(-1));
            Assert.That(layer.PollDialogEvent().Kind, Is.Null);
        });
    }

    static SipDialogId Id(string remoteTag)
    {
        SipDialogId id = new SipDialogId();
        id.CallId = "fork-call@example.com";
        id.LocalTag = "caller-tag";
        id.RemoteTag = remoteTag;
        return id;
    }

    static SipMessage Invite(int cseq)
    {
        return Parse("INVITE sip:bob@callee.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-fork\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=caller-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>\r\n"
            + "Call-ID: fork-call@example.com\r\n"
            + "CSeq: " + cseq.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " INVITE\r\nContact: <sip:alice@client.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Response(int status, string remoteTag, int cseq,
        string method, string contactHost, string routes)
    {
        string contact = contactHost.Length == 0
            ? "" : "Contact: <sip:bob@" + contactHost + ">\r\n";
        return Parse("SIP/2.0 " + status.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " Dialog\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-fork\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=caller-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>;tag=" + remoteTag + "\r\n"
            + "Call-ID: fork-call@example.com\r\n"
            + "CSeq: " + cseq.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " " + method + "\r\n" + routes + contact
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage RemoteRequest(string method, string fromTag,
        string toTag, int cseq, string targetHost)
    {
        return Parse(method + " sip:alice@" + targetHost + " SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP remote.example.com:5060;branch=z9hG4bK-remote\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Bob <sip:bob@bob.example.com>;tag=" + fromTag + "\r\n"
            + "To: Alice <sip:alice@alice.example.com>;tag=" + toTag + "\r\n"
            + "Call-ID: fork-call@example.com\r\n"
            + "CSeq: " + cseq.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " " + method + "\r\nContent-Length: 0\r\n\r\n");
    }

    static string[] RouteHosts(SipRouteValue[] routes)
    {
        return routes.Select(route => route.Address!.Uri!.Host!).ToArray();
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
        return result.Message!;
    }
}
