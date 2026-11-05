namespace sip.Tests;

public class SipOfferAnswerSessionTests
{
    [Test]
    public void LocalSdpOfferCompletesWithRemoteAnswerAndAcceptsFinalDuplicate()
    {
        SipOfferAnswerSession session = new SipOfferAnswerSession();
        SipOfferAnswer initial = session.GetOfferAnswer();
        SipOfferAnswer offer = LocalOffer("v=0\r\no=alice 1 1 IN IP4 192.0.2.1\r\n");

        SipOfferAnswer pending = session.ApplyLocalOffer(offer);
        offer.LocalOffer[0] = 0;
        SipOfferAnswer answer = RemoteAnswer(
            "v=0\r\no=bob 2 2 IN IP4 192.0.2.2\r\n", MediaType("application", "sdp"));
        SipOfferAnswer completed = session.ApplyOfferAnswer(answer);
        SipOfferAnswer duplicate = session.ApplyOfferAnswer(answer);
        completed.NegotiatedBody[0] = 0;
        SipOfferAnswer current = session.GetOfferAnswer();

        Assert.Multiple(() =>
        {
            Assert.That(initial.State, Is.EqualTo(SipOfferAnswerState.Stable));
            Assert.That(pending.State,
                Is.EqualTo(SipOfferAnswerState.LocalOfferPending));
            Assert.That(Text(pending.LocalOffer), Does.StartWith("v=0"));
            Assert.That(pending.NegotiatedBody, Is.Empty);
            Assert.That(completed.State, Is.EqualTo(SipOfferAnswerState.Completed));
            Assert.That(Text(completed.RemoteAnswer), Does.Contain("o=bob"));
            Assert.That(Text(duplicate.NegotiatedBody), Does.Contain("o=bob"));
            Assert.That(Text(current.NegotiatedBody), Does.Contain("o=bob"));
            Assert.That(current.NegotiatedContentType!.Type,
                Is.EqualTo("application"));
            Assert.That(current.NegotiatedContentType.Subtype, Is.EqualTo("sdp"));
        });
    }

    [Test]
    public void RemoteOpaqueOfferCompletesWithDetachedLocalAnswer()
    {
        SipOfferAnswerSession session = new SipOfferAnswerSession();
        SipOfferAnswer offer = RemoteOffer("remote-proposal",
            MediaType("application", "vendor-session"));

        SipOfferAnswer pending = session.ApplyRemoteOffer(offer);
        SipOfferAnswer answer = LocalAnswer("local-selection", null);
        SipOfferAnswer completed = session.ApplyOfferAnswer(answer);
        answer.LocalAnswer[0] = 0;
        SipOfferAnswer current = session.GetOfferAnswer();

        Assert.Multiple(() =>
        {
            Assert.That(pending.State,
                Is.EqualTo(SipOfferAnswerState.RemoteOfferPending));
            Assert.That(Text(pending.RemoteOffer), Is.EqualTo("remote-proposal"));
            Assert.That(completed.State, Is.EqualTo(SipOfferAnswerState.Completed));
            Assert.That(Text(completed.LocalAnswer), Is.EqualTo("local-selection"));
            Assert.That(Text(current.NegotiatedBody), Is.EqualTo("local-selection"));
            Assert.That(current.ContentType!.Subtype,
                Is.EqualTo("vendor-session"));
        });
    }

    [Test]
    public void OneOfferInvariantRejectsGlareButAllowsOfferRetransmission()
    {
        SipOfferAnswerSession session = new SipOfferAnswerSession();
        SipOfferAnswer local = LocalOffer("local-offer");
        session.ApplyLocalOffer(local);

        SipOfferAnswer repeated = session.ApplyLocalOffer(LocalOffer("local-offer"));
        Assert.Throws<InvalidOperationException>(() =>
            session.ApplyLocalOffer(LocalOffer("different-offer")));
        Assert.Throws<InvalidOperationException>(() =>
            session.ApplyRemoteOffer(RemoteOffer("glare-offer", SdpType())));
        SipOfferAnswer current = session.GetOfferAnswer();

        Assert.Multiple(() =>
        {
            Assert.That(repeated.State,
                Is.EqualTo(SipOfferAnswerState.LocalOfferPending));
            Assert.That(current.State,
                Is.EqualTo(SipOfferAnswerState.LocalOfferPending));
            Assert.That(Text(current.LocalOffer), Is.EqualTo("local-offer"));
            Assert.That(current.RemoteOffer, Is.Empty);
        });
    }

    [Test]
    public void UnexpectedOfferAndAnswerPlacementsAreRejectedAtomically()
    {
        SipOfferAnswerSession localSession = new SipOfferAnswerSession();
        SipOfferAnswer mixedLocalOffer = LocalOffer("local-offer");
        mixedLocalOffer.RemoteOffer = Bytes("unexpected-remote-offer");
        Assert.Throws<ArgumentException>(() =>
            localSession.ApplyLocalOffer(mixedLocalOffer));
        SipOfferAnswer localPending = localSession.ApplyLocalOffer(
            LocalOffer("local-offer"));
        SipOfferAnswer mixedRemoteAnswer = RemoteAnswer("remote-answer", SdpType());
        mixedRemoteAnswer.LocalAnswer = Bytes("unexpected-local-answer");
        Assert.Throws<ArgumentException>(() =>
            localSession.ApplyOfferAnswer(mixedRemoteAnswer));

        SipOfferAnswerSession remoteSession = new SipOfferAnswerSession();
        SipOfferAnswer mixedRemoteOffer = RemoteOffer("remote-offer", SdpType());
        mixedRemoteOffer.LocalOffer = Bytes("unexpected-local-offer");
        Assert.Throws<ArgumentException>(() =>
            remoteSession.ApplyRemoteOffer(mixedRemoteOffer));
        SipOfferAnswer remotePending = remoteSession.ApplyRemoteOffer(
            RemoteOffer("remote-offer", SdpType()));
        SipOfferAnswer mixedLocalAnswer = LocalAnswer("local-answer", SdpType());
        mixedLocalAnswer.RemoteAnswer = Bytes("unexpected-remote-answer");
        Assert.Throws<ArgumentException>(() =>
            remoteSession.ApplyOfferAnswer(mixedLocalAnswer));

        Assert.Multiple(() =>
        {
            Assert.That(localPending.State,
                Is.EqualTo(SipOfferAnswerState.LocalOfferPending));
            Assert.That(localSession.GetOfferAnswer().State,
                Is.EqualTo(SipOfferAnswerState.LocalOfferPending));
            Assert.That(localSession.GetOfferAnswer().RemoteAnswer, Is.Empty);
            Assert.That(remotePending.State,
                Is.EqualTo(SipOfferAnswerState.RemoteOfferPending));
            Assert.That(remoteSession.GetOfferAnswer().State,
                Is.EqualTo(SipOfferAnswerState.RemoteOfferPending));
            Assert.That(remoteSession.GetOfferAnswer().LocalAnswer, Is.Empty);
        });
    }

    [Test]
    public void LocalOfferlessInviteAcceptsRemoteOfferThenLocalAckAnswer()
    {
        SipOfferAnswerSession session = new SipOfferAnswerSession();
        SipOfferAnswer offerless = new SipOfferAnswer();

        SipOfferAnswer awaitingOffer = session.ApplyLocalOffer(offerless);
        SipOfferAnswer pending = session.ApplyRemoteOffer(
            RemoteOffer("offer-in-success", SdpType()));
        SipOfferAnswer completed = session.ApplyOfferAnswer(
            LocalAnswer("answer-in-ack", SdpType()));

        Assert.Multiple(() =>
        {
            Assert.That(awaitingOffer.State,
                Is.EqualTo(SipOfferAnswerState.RemoteOfferRequired));
            Assert.That(pending.State,
                Is.EqualTo(SipOfferAnswerState.RemoteOfferPending));
            Assert.That(Text(pending.RemoteOffer), Is.EqualTo("offer-in-success"));
            Assert.That(completed.State, Is.EqualTo(SipOfferAnswerState.Completed));
            Assert.That(Text(completed.NegotiatedBody), Is.EqualTo("answer-in-ack"));
        });
    }

    [Test]
    public void RemoteOfferlessInviteAcceptsLocalOfferThenRemoteAckAnswer()
    {
        SipOfferAnswerSession session = new SipOfferAnswerSession();
        SipOfferAnswer offerless = new SipOfferAnswer();

        SipOfferAnswer awaitingOffer = session.ApplyRemoteOffer(offerless);
        SipOfferAnswer pending = session.ApplyLocalOffer(
            LocalOffer("offer-in-success"));
        SipOfferAnswer completed = session.ApplyOfferAnswer(
            RemoteAnswer("answer-in-ack", SdpType()));

        Assert.Multiple(() =>
        {
            Assert.That(awaitingOffer.State,
                Is.EqualTo(SipOfferAnswerState.LocalOfferRequired));
            Assert.That(pending.State,
                Is.EqualTo(SipOfferAnswerState.LocalOfferPending));
            Assert.That(Text(pending.LocalOffer), Is.EqualTo("offer-in-success"));
            Assert.That(completed.State, Is.EqualTo(SipOfferAnswerState.Completed));
            Assert.That(Text(completed.NegotiatedBody), Is.EqualTo("answer-in-ack"));
        });
    }

    [Test]
    public void FailedUpdateRollsBackToPreviouslyNegotiatedBody()
    {
        SipOfferAnswerSession session = new SipOfferAnswerSession();
        session.ApplyLocalOffer(LocalOffer("initial-offer"));
        session.ApplyOfferAnswer(RemoteAnswer("initial-answer", SdpType()));

        SipOfferAnswer pending = session.ApplyLocalOffer(LocalOffer("update-offer"));
        SipOfferAnswer failed = session.RollbackOfferAnswer(pending);
        SipOfferAnswer next = session.ApplyRemoteOffer(
            RemoteOffer("later-reinvite", SdpType()));

        Assert.Multiple(() =>
        {
            Assert.That(Text(pending.NegotiatedBody), Is.EqualTo("initial-answer"));
            Assert.That(failed.State, Is.EqualTo(SipOfferAnswerState.Failed));
            Assert.That(Text(failed.LocalOffer), Is.EqualTo("update-offer"));
            Assert.That(Text(failed.NegotiatedBody), Is.EqualTo("initial-answer"));
            Assert.That(next.State,
                Is.EqualTo(SipOfferAnswerState.RemoteOfferPending));
            Assert.That(Text(next.NegotiatedBody), Is.EqualTo("initial-answer"));
        });
    }

    [Test]
    public void ApplicationCanRejectRemoteReinviteWithoutChangingSession()
    {
        SipOfferAnswerSession session = new SipOfferAnswerSession();
        session.ApplyRemoteOffer(RemoteOffer("initial-offer", SdpType()));
        session.ApplyOfferAnswer(LocalAnswer("initial-answer", SdpType()));
        SipOfferAnswer pending = session.ApplyRemoteOffer(
            RemoteOffer("unacceptable-offer", SdpType()));

        SipOfferAnswer rejected = session.RejectOfferAnswer(pending);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.State, Is.EqualTo(SipOfferAnswerState.Failed));
            Assert.That(Text(rejected.RemoteOffer),
                Is.EqualTo("unacceptable-offer"));
            Assert.That(Text(rejected.NegotiatedBody),
                Is.EqualTo("initial-answer"));
            Assert.Throws<InvalidOperationException>(() =>
                session.RejectOfferAnswer(rejected));
        });
    }

    [Test]
    public void RollbackAndRejectionRequireTheMatchingOutstandingExchange()
    {
        SipOfferAnswerSession localSession = new SipOfferAnswerSession();
        SipOfferAnswer localPending = localSession.ApplyLocalOffer(
            LocalOffer("current-local-offer"));
        SipOfferAnswer staleLocal = LocalOffer("stale-local-offer");
        staleLocal.State = SipOfferAnswerState.LocalOfferPending;

        Assert.Throws<ArgumentException>(() =>
            localSession.RollbackOfferAnswer(staleLocal));
        SipOfferAnswer localStillPending = localSession.GetOfferAnswer();
        SipOfferAnswer rolledBack = localSession.RollbackOfferAnswer(localPending);

        SipOfferAnswerSession remoteSession = new SipOfferAnswerSession();
        SipOfferAnswer remotePending = remoteSession.ApplyRemoteOffer(
            RemoteOffer("current-remote-offer", SdpType()));
        SipOfferAnswer staleRemote = RemoteOffer("stale-remote-offer", SdpType());
        staleRemote.State = SipOfferAnswerState.RemoteOfferPending;

        Assert.Throws<ArgumentException>(() =>
            remoteSession.RejectOfferAnswer(staleRemote));
        SipOfferAnswer remoteStillPending = remoteSession.GetOfferAnswer();
        SipOfferAnswer rejected = remoteSession.RejectOfferAnswer(remotePending);

        Assert.Multiple(() =>
        {
            Assert.That(localStillPending.State,
                Is.EqualTo(SipOfferAnswerState.LocalOfferPending));
            Assert.That(Text(localStillPending.LocalOffer),
                Is.EqualTo("current-local-offer"));
            Assert.That(rolledBack.State, Is.EqualTo(SipOfferAnswerState.Failed));
            Assert.That(remoteStillPending.State,
                Is.EqualTo(SipOfferAnswerState.RemoteOfferPending));
            Assert.That(Text(remoteStillPending.RemoteOffer),
                Is.EqualTo("current-remote-offer"));
            Assert.That(rejected.State, Is.EqualTo(SipOfferAnswerState.Failed));
        });
    }

    [Test]
    public void InvalidBodiesAndAnswersLeaveOutstandingStateUnchanged()
    {
        SipOfferAnswerSession session = new SipOfferAnswerSession();
        SipOfferAnswer missingType = new SipOfferAnswer();
        missingType.LocalOffer = Bytes("offer");
        SipOfferAnswer typedOfferless = new SipOfferAnswer();
        typedOfferless.ContentType = SdpType();
        SipOfferAnswer oversized = LocalOffer("offer");
        oversized.LocalOffer = new byte[SipDefaults.MaxBodyBytes + 1];

        Assert.Throws<ArgumentException>(() => session.ApplyLocalOffer(missingType));
        Assert.Throws<ArgumentException>(() => session.ApplyLocalOffer(typedOfferless));
        Assert.Throws<ArgumentException>(() => session.ApplyLocalOffer(oversized));
        session.ApplyLocalOffer(LocalOffer("valid-offer"));
        Assert.Throws<ArgumentException>(() => session.ApplyOfferAnswer(
            RemoteAnswer("wrong-type", MediaType("text", "plain"))));
        Assert.Throws<ArgumentException>(() => session.ApplyOfferAnswer(
            RemoteAnswer("", SdpType())));
        Assert.Throws<InvalidOperationException>(() => session.ApplyRemoteOffer(
            RemoteOffer("glare", SdpType())));
        SipOfferAnswer pending = session.GetOfferAnswer();

        Assert.Multiple(() =>
        {
            Assert.That(pending.State,
                Is.EqualTo(SipOfferAnswerState.LocalOfferPending));
            Assert.That(Text(pending.LocalOffer), Is.EqualTo("valid-offer"));
            Assert.That(pending.RemoteAnswer, Is.Empty);
            Assert.That(pending.NegotiatedBody, Is.Empty);
        });
    }

    static SipOfferAnswer LocalOffer(string text)
    {
        SipOfferAnswer exchange = new SipOfferAnswer();
        exchange.LocalOffer = Bytes(text);
        exchange.ContentType = SdpType();
        return exchange;
    }

    static SipOfferAnswer RemoteOffer(string text, SipMediaType contentType)
    {
        SipOfferAnswer exchange = new SipOfferAnswer();
        exchange.RemoteOffer = Bytes(text);
        exchange.ContentType = contentType;
        return exchange;
    }

    static SipOfferAnswer LocalAnswer(string text, SipMediaType? contentType)
    {
        SipOfferAnswer exchange = new SipOfferAnswer();
        exchange.LocalAnswer = Bytes(text);
        exchange.ContentType = contentType;
        return exchange;
    }

    static SipOfferAnswer RemoteAnswer(string text, SipMediaType? contentType)
    {
        SipOfferAnswer exchange = new SipOfferAnswer();
        exchange.RemoteAnswer = Bytes(text);
        exchange.ContentType = contentType;
        return exchange;
    }

    static SipMediaType SdpType()
    {
        return MediaType("application", "sdp");
    }

    static SipMediaType MediaType(string type, string subtype)
    {
        SipMediaType value = new SipMediaType();
        value.Type = type;
        value.Subtype = subtype;
        return value;
    }

    static byte[] Bytes(string text)
    {
        return System.Text.Encoding.UTF8.GetBytes(text);
    }

    static string Text(byte[] value)
    {
        return System.Text.Encoding.UTF8.GetString(value);
    }
}
