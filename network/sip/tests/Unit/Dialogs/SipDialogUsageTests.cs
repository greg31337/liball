namespace sip.Tests;

public class SipDialogUsageTests
{
    [Test]
    public void InitialInviteUsageIsVisibleAndStableWhenEarlyDialogConfirms()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipMessage invite = Invite();
        SipDialogSnapshot early = layer.CreateDialog(invite, Response(180),
            new SipDialogOptions());
        SipDialogEvent created = layer.PollDialogEvent();

        SipDialogSnapshot confirmed = layer.CreateDialog(invite, Response(200),
            new SipDialogOptions());
        SipDialogEvent changed = layer.PollDialogEvent();

        Assert.Multiple(() =>
        {
            Assert.That(early.UsageIds, Has.Length.EqualTo(1));
            Assert.That(early.UsageIds[0], Does.StartWith("usage-"));
            Assert.That(created.UsageId, Is.EqualTo(early.UsageIds[0]));
            Assert.That(created.Snapshot!.UsageIds,
                Is.EqualTo(early.UsageIds));
            Assert.That(confirmed.UsageIds, Is.EqualTo(early.UsageIds));
            Assert.That(changed.UsageId, Is.EqualTo(early.UsageIds[0]));
            Assert.That(confirmed.State, Is.EqualTo(SipDialogState.Confirmed));
        });
    }

    [Test]
    public void AdditionalUsagesReceiveDistinctIdentifiersAndRespectCapacity()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogOptions options = new SipDialogOptions();
        options.MaxUsageCount = 3;
        SipDialogSnapshot dialog = layer.CreateDialog(
            Invite(), Response(200), options);
        layer.PollDialogEvent();

        string second = layer.CreateUsage(dialog.Id!);
        SipDialogEvent secondEvent = layer.PollDialogEvent();
        string third = layer.CreateUsage(dialog.Id!);
        SipDialogEvent thirdEvent = layer.PollDialogEvent();
        SipDialogException full = Assert.Throws<SipDialogException>(() =>
            layer.CreateUsage(dialog.Id!))!;
        SipDialogSnapshot current = layer.FindDialog(dialog.Id!);

        Assert.Multiple(() =>
        {
            Assert.That(second, Does.StartWith("usage-"));
            Assert.That(third, Does.StartWith("usage-"));
            Assert.That(third, Is.Not.EqualTo(second));
            Assert.That(secondEvent.Kind, Is.EqualTo("usage-created"));
            Assert.That(secondEvent.UsageId, Is.EqualTo(second));
            Assert.That(thirdEvent.UsageId, Is.EqualTo(third));
            Assert.That(current.UsageIds,
                Is.EqualTo(new string[] { dialog.UsageIds[0], second, third }));
            Assert.That(full.Message, Does.Contain("limit"));
            Assert.That(layer.PollDialogEvent().Kind, Is.Null);
        });
    }

    [Test]
    public void TerminatingOneUsageKeepsDialogUntilLastUsageEnds()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = layer.CreateDialog(
            Invite(), Response(200), new SipDialogOptions());
        string inviteUsage = dialog.UsageIds[0];
        string subscriptionUsage = layer.CreateUsage(dialog.Id!);
        layer.PollDialogEvent();
        layer.PollDialogEvent();
        SipCancellation cancellation = new SipCancellation();
        cancellation.Reason = "Subscription replaced.";

        layer.TerminateUsage(dialog.Id!, subscriptionUsage, cancellation);
        SipDialogEvent usageEnded = layer.PollDialogEvent();
        SipDialogSnapshot remaining = layer.FindDialog(dialog.Id!);
        layer.TerminateUsage(dialog.Id!, inviteUsage, new SipCancellation());
        SipDialogEvent dialogEnded = layer.PollDialogEvent();

        Assert.Multiple(() =>
        {
            Assert.That(usageEnded.Kind, Is.EqualTo("usage-terminated"));
            Assert.That(usageEnded.UsageId, Is.EqualTo(subscriptionUsage));
            Assert.That(usageEnded.Reason, Is.EqualTo("Subscription replaced."));
            Assert.That(remaining.State, Is.EqualTo(SipDialogState.Confirmed));
            Assert.That(remaining.UsageIds,
                Is.EqualTo(new string[] { inviteUsage }));
            Assert.That(dialogEnded.Kind, Is.EqualTo("terminated"));
            Assert.That(dialogEnded.UsageId, Is.EqualTo(inviteUsage));
            Assert.That(dialogEnded.State, Is.EqualTo(SipDialogState.Terminated));
            Assert.That(dialogEnded.Snapshot!.UsageIds, Is.Empty);
            Assert.Throws<SipDialogException>(() => layer.FindDialog(dialog.Id!));
        });
    }

    [Test]
    public void ByeTerminatesOnlyInviteUsageWhenSiblingUsageRemains()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = layer.CreateDialog(
            Invite(), Response(200), new SipDialogOptions());
        string inviteUsage = dialog.UsageIds[0];
        string siblingUsage = layer.CreateUsage(dialog.Id!);
        layer.PollDialogEvent();
        layer.PollDialogEvent();

        SipDialogSnapshot afterBye = layer.UpdateDialog(
            dialog.Id!, RemoteBye(11));
        SipDialogEvent ended = layer.PollDialogEvent();

        Assert.Multiple(() =>
        {
            Assert.That(afterBye.State, Is.EqualTo(SipDialogState.Confirmed));
            Assert.That(afterBye.UsageIds,
                Is.EqualTo(new string[] { siblingUsage }));
            Assert.That(ended.Kind, Is.EqualTo("usage-terminated"));
            Assert.That(ended.UsageId, Is.EqualTo(inviteUsage));
            Assert.That(ended.Message!.Method, Is.EqualTo(SipMethod.Bye));
            Assert.That(layer.FindDialog(dialog.Id!).UsageIds,
                Is.EqualTo(new string[] { siblingUsage }));
        });
    }

    [Test]
    public void InitialInviteFailureLeavesDialogAliveForSiblingUsage()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot early = layer.CreateDialog(
            Invite(), Response(180), new SipDialogOptions());
        string inviteUsage = early.UsageIds[0];
        string siblingUsage = layer.CreateUsage(early.Id!);
        layer.PollDialogEvent();
        layer.PollDialogEvent();

        SipDialogSnapshot failed = layer.UpdateDialog(early.Id!, Response(486));
        SipDialogEvent ended = layer.PollDialogEvent();

        Assert.Multiple(() =>
        {
            Assert.That(failed.State, Is.EqualTo(SipDialogState.Early));
            Assert.That(failed.UsageIds,
                Is.EqualTo(new string[] { siblingUsage }));
            Assert.That(ended.Kind, Is.EqualTo("usage-terminated"));
            Assert.That(ended.UsageId, Is.EqualTo(inviteUsage));
            Assert.That(layer.FindDialog(early.Id!).UsageIds,
                Is.EqualTo(new string[] { siblingUsage }));
        });
    }

    [Test]
    public void InvalidOrUnknownUsageTerminationDoesNotChangeDialog()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = layer.CreateDialog(
            Invite(), Response(200), new SipDialogOptions());
        layer.PollDialogEvent();

        Assert.Throws<ArgumentException>(() => layer.TerminateUsage(
            dialog.Id!, "not a token", new SipCancellation()));
        Assert.Throws<SipDialogException>(() => layer.TerminateUsage(
            dialog.Id!, "usage-missing", new SipCancellation()));
        SipDialogSnapshot unchanged = layer.FindDialog(dialog.Id!);

        Assert.Multiple(() =>
        {
            Assert.That(unchanged.State, Is.EqualTo(SipDialogState.Confirmed));
            Assert.That(unchanged.UsageIds, Is.EqualTo(dialog.UsageIds));
            Assert.That(layer.PollDialogEvent().Kind, Is.Null);
        });
    }

    [Test]
    public void ExplicitDialogTerminationEndsEveryUsage()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = layer.CreateDialog(
            Invite(), Response(200), new SipDialogOptions());
        layer.CreateUsage(dialog.Id!);
        layer.PollDialogEvent();
        layer.PollDialogEvent();

        layer.TerminateDialog(dialog.Id!, new SipCancellation());
        SipDialogEvent ended = layer.PollDialogEvent();

        Assert.Multiple(() =>
        {
            Assert.That(ended.Kind, Is.EqualTo("terminated"));
            Assert.That(ended.UsageId, Is.Null);
            Assert.That(ended.Snapshot!.UsageIds, Is.Empty);
            Assert.That(ended.State, Is.EqualTo(SipDialogState.Terminated));
            Assert.Throws<SipDialogException>(() => layer.FindDialog(dialog.Id!));
        });
    }

    static SipMessage Invite()
    {
        return Parse("INVITE sip:bob@callee.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-usage\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>\r\n"
            + "Call-ID: usage-call@example.com\r\nCSeq: 10 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Response(int status)
    {
        string contact = status < 300
            ? "Contact: <sip:bob@server.example.com>\r\n" : "";
        return Parse("SIP/2.0 " + status.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " Dialog\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-usage\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>;tag=remote-tag\r\n"
            + "Call-ID: usage-call@example.com\r\nCSeq: 10 INVITE\r\n"
            + contact + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage RemoteBye(int cseq)
    {
        return Parse("BYE sip:alice@client.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP server.example.com:5060;branch=z9hG4bK-bye\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Bob <sip:bob@bob.example.com>;tag=remote-tag\r\n"
            + "To: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "Call-ID: usage-call@example.com\r\nCSeq: "
            + cseq.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " BYE\r\nContent-Length: 0\r\n\r\n");
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
