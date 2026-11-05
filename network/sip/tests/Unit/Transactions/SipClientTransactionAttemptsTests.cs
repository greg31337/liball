namespace sip.Tests;

public class SipClientTransactionAttemptsTests
{
    [Test]
    public void StartUsesFirstCandidateAndCreatesDetachedTransactionVia()
    {
        SipMessage request = Request("z9hG4bK-original");
        SipServerCandidate first = Candidate("192.0.2.10", 5060, SipTransportKind.Udp, false);
        SipClientTransactionAttempts attempts = Attempts(request,
            new SipServerCandidate[] { first });

        request.RequestUri!.Host = "changed.example.com";
        first.Host = "192.0.2.99";
        SipClientTransactionAttemptResult result = attempts.StartAttempt();
        SipMessage sent = result.SendRequest!.Message!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Created, Is.True);
            Assert.That(result.Retried, Is.False);
            Assert.That(result.CandidateIndex, Is.EqualTo(0));
            Assert.That(result.Branch, Is.EqualTo("z9hG4bK01010101010101010101010101010101"));
            Assert.That(result.Candidate!.Host, Is.EqualTo("192.0.2.10"));
            Assert.That(result.SendRequest.RemoteAddress!.Host, Is.EqualTo("192.0.2.10"));
            Assert.That(result.SendRequest.RemoteAddress.Port, Is.EqualTo(5060));
            Assert.That(result.SendRequest.Transport, Is.EqualTo(SipTransportKind.Udp));
            Assert.That(result.SendRequest.LocalAddress!.Host, Is.EqualTo("client.example.com"));
            Assert.That(sent.RequestUri!.Host, Is.EqualTo("service.example.com"));
            Assert.That(sent.ViaValues[0].Branch, Is.EqualTo(result.Branch));
            Assert.That(sent.ViaValues[0].Transport, Is.EqualTo("UDP"));
            Assert.That(sent.ViaValues[0].HasRPort, Is.True);
            Assert.That(sent.ViaValues[1].Branch, Is.EqualTo("z9hG4bK-lower"));
            Assert.That(request.ViaValues[0].Branch, Is.EqualTo("z9hG4bK-original"));
        });
    }

    [Test]
    public void FailureBeforeResponseUsesNextTargetAndFreshBranch()
    {
        SipClientTransactionAttempts attempts = Attempts(Request("z9hG4bK-original"),
            new SipServerCandidate[]
            {
                Candidate("192.0.2.10", 5060, SipTransportKind.Udp, false),
                Candidate("2001:db8::20", 5070, SipTransportKind.Tcp, false)
            });
        SipClientTransactionAttemptResult first = attempts.StartAttempt();
        attempts.CaptureSendAffinity(SendResult("connection-one", "flow-one",
            SipTransportKind.Udp));

        SipClientTransactionAttemptResult retry =
            attempts.RetryAfterTransportFailure("first target failed");
        SipWriteResult serialized = new SipMessageCodec().SerializeMessage(
            retry.SendRequest!.Message!, new SipWriteOptions());
        SipParseResult parsed = new SipMessageCodec().ParseDatagramMessage(
            serialized.Bytes, new SipParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(retry.Created, Is.True);
            Assert.That(retry.Retried, Is.True);
            Assert.That(retry.CandidateIndex, Is.EqualTo(1));
            Assert.That(retry.Branch, Is.EqualTo("z9hG4bK02020202020202020202020202020202"));
            Assert.That(retry.Branch, Is.Not.EqualTo(first.Branch));
            Assert.That(retry.SendRequest.RemoteAddress!.Host, Is.EqualTo("2001:db8::20"));
            Assert.That(retry.SendRequest.RemoteAddress.Port, Is.EqualTo(5070));
            Assert.That(retry.SendRequest.Transport, Is.EqualTo(SipTransportKind.Tcp));
            Assert.That(retry.SendRequest.ConnectionId, Is.Null);
            Assert.That(retry.SendRequest.FlowId, Is.Null);
            Assert.That(retry.SendRequest.Message!.ViaValues[0].Transport, Is.EqualTo("TCP"));
            Assert.That(retry.SendRequest.Message.ViaValues[1].Branch,
                Is.EqualTo("z9hG4bK-lower"));
            Assert.That(serialized.Success, Is.True, serialized.ErrorMessage);
            Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete), parsed.ErrorMessage);
            Assert.That(parsed.Message!.ViaValues[0].Branch, Is.EqualTo(retry.Branch));
            Assert.That(parsed.Message.ViaValues, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void RetransmissionKeepsTargetBranchConnectionAndFlowAffinity()
    {
        SipClientTransactionAttempts attempts = Attempts(Request("z9hG4bK-original"),
            new SipServerCandidate[]
            {
                Candidate("192.0.2.20", 5070, SipTransportKind.Tcp, false)
            });
        SipClientTransactionAttemptResult started = attempts.StartAttempt();
        bool captured = attempts.CaptureSendAffinity(
            SendResult("connection-one", "flow-one", SipTransportKind.Tcp));

        SipSendRequest retransmission =
            attempts.CreateAffineSend(started.SendRequest!.Message);
        started.SendRequest.Message!.RequestUri!.Host = "mutated.example.com";

        Assert.Multiple(() =>
        {
            Assert.That(captured, Is.True);
            Assert.That(retransmission.ConnectionId, Is.EqualTo("connection-one"));
            Assert.That(retransmission.FlowId, Is.EqualTo("flow-one"));
            Assert.That(retransmission.Transport, Is.EqualTo(SipTransportKind.Tcp));
            Assert.That(retransmission.RemoteAddress!.Host, Is.EqualTo("192.0.2.20"));
            Assert.That(retransmission.RemoteAddress.Port, Is.EqualTo(5070));
            Assert.That(retransmission.Message!.ViaValues[0].Branch,
                Is.EqualTo(started.Branch));
            Assert.That(retransmission.Message.RequestUri!.Host,
                Is.EqualTo("service.example.com"));
        });
    }

    [Test]
    public void RejectedAffinityUpdatePreservesKnownGoodConnectionAndFlow()
    {
        SipClientTransactionAttempts attempts = Attempts(Request("z9hG4bK-original"),
            new SipServerCandidate[]
            {
                Candidate("192.0.2.20", 5070, SipTransportKind.Tcp, false)
            });
        SipClientTransactionAttemptResult started = attempts.StartAttempt();
        bool captured = attempts.CaptureSendAffinity(
            SendResult("connection-good", "flow-good", SipTransportKind.Tcp));
        bool rejected = attempts.CaptureSendAffinity(
            SendResult("connection-wrong", "flow-wrong", SipTransportKind.Udp));

        SipSendRequest retransmission = attempts.CreateAffineSend(
            started.SendRequest!.Message);

        Assert.Multiple(() =>
        {
            Assert.That(captured, Is.True);
            Assert.That(rejected, Is.False);
            Assert.That(retransmission.ConnectionId, Is.EqualTo("connection-good"));
            Assert.That(retransmission.FlowId, Is.EqualTo("flow-good"));
            Assert.That(retransmission.Transport, Is.EqualTo(SipTransportKind.Tcp));
            Assert.That(retransmission.RemoteAddress!.Host, Is.EqualTo("192.0.2.20"));
        });
    }

    [Test]
    public void MatchingResponseEstablishesTransactionAndSuppressesFailover()
    {
        SipClientTransactionAttempts attempts = Attempts(Request("z9hG4bK-original"),
            new SipServerCandidate[]
            {
                Candidate("192.0.2.20", 5070, SipTransportKind.Tcp, false),
                Candidate("192.0.2.30", 5080, SipTransportKind.Tcp, false)
            });
        SipClientTransactionAttemptResult started = attempts.StartAttempt();
        SipTransportMessage response = Response(started.Branch!, SipTransportKind.Tcp);
        response.Connection = Connection("response-connection", SipTransportKind.Tcp);
        response.Flow = Flow("response-flow", response.Connection);

        bool observed = attempts.ObserveResponse(response);
        SipSendRequest affine = attempts.CreateAffineSend(started.SendRequest!.Message);
        SipClientTransactionAttemptResult failed =
            attempts.RetryAfterTransportFailure("connection closed");

        Assert.Multiple(() =>
        {
            Assert.That(observed, Is.True);
            Assert.That(attempts.HasEstablishedTransaction(), Is.True);
            Assert.That(affine.ConnectionId, Is.EqualTo("response-connection"));
            Assert.That(affine.FlowId, Is.EqualTo("response-flow"));
            Assert.That(failed.Suppressed, Is.True);
            Assert.That(failed.Created, Is.False);
            Assert.That(failed.Exhausted, Is.False);
            Assert.That(failed.SendRequest, Is.Null);
            Assert.That(failed.CandidateIndex, Is.EqualTo(0));
            Assert.That(attempts.GetCandidateIndex(), Is.EqualTo(0));
        });
    }

    [Test]
    public void UnmatchedOrWrongTransportResponseDoesNotSuppressSafeRetry()
    {
        SipClientTransactionAttempts attempts = Attempts(Request("z9hG4bK-original"),
            new SipServerCandidate[]
            {
                Candidate("192.0.2.10", 5060, SipTransportKind.Udp, false),
                Candidate("192.0.2.20", 5070, SipTransportKind.Tcp, false)
            });
        SipClientTransactionAttemptResult started = attempts.StartAttempt();

        bool wrongBranch = attempts.ObserveResponse(
            Response("z9hG4bK-unrelated", SipTransportKind.Udp));
        bool wrongTransport = attempts.ObserveResponse(
            Response(started.Branch!, SipTransportKind.Tcp));
        SipClientTransactionAttemptResult retry =
            attempts.RetryAfterTransportFailure("datagram send failed");

        Assert.Multiple(() =>
        {
            Assert.That(wrongBranch, Is.False);
            Assert.That(wrongTransport, Is.False);
            Assert.That(attempts.HasEstablishedTransaction(), Is.False);
            Assert.That(retry.Created, Is.True);
            Assert.That(retry.CandidateIndex, Is.EqualTo(1));
        });
    }

    [Test]
    public void LastTargetFailureReportsExhaustionWithoutChangingAttempt()
    {
        SipClientTransactionAttempts attempts = Attempts(Request("z9hG4bK-original"),
            new SipServerCandidate[]
            {
                Candidate("192.0.2.10", 5060, SipTransportKind.Udp, false)
            });
        SipClientTransactionAttemptResult started = attempts.StartAttempt();

        SipClientTransactionAttemptResult exhausted =
            attempts.RetryAfterTransportFailure("no route");

        Assert.Multiple(() =>
        {
            Assert.That(exhausted.Exhausted, Is.True);
            Assert.That(exhausted.Suppressed, Is.False);
            Assert.That(exhausted.Created, Is.False);
            Assert.That(exhausted.SendRequest, Is.Null);
            Assert.That(exhausted.Branch, Is.EqualTo(started.Branch));
            Assert.That(exhausted.ErrorMessage, Is.EqualTo("no route"));
            Assert.That(attempts.GetCandidateIndex(), Is.EqualTo(0));
        });
    }

    [Test]
    public void RejectsInvalidSetupAndOutOfSequenceOperations()
    {
        SipMessage request = Request("z9hG4bK-original");
        SipServerCandidate invalid = Candidate("192.0.2.10", 0,
            SipTransportKind.Udp, false);
        SipClientTransactionAttempts attempts = Attempts(request,
            new SipServerCandidate[]
            {
                Candidate("192.0.2.10", 5060, SipTransportKind.Udp, false)
            });

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => Attempts(request,
                new SipServerCandidate[] { invalid }));
            Assert.Throws<InvalidOperationException>(() =>
                attempts.RetryAfterTransportFailure("failed"));
            Assert.Throws<InvalidOperationException>(() =>
                attempts.CaptureSendAffinity(new SipSendResult()));
            attempts.StartAttempt();
            Assert.Throws<InvalidOperationException>(() => attempts.StartAttempt());
            Assert.Throws<ArgumentException>(() =>
                attempts.RetryAfterTransportFailure(""));
            Assert.Throws<ArgumentException>(() =>
                attempts.CreateAffineSend(Request("z9hG4bK-other")));
        });
    }

    static SipClientTransactionAttempts Attempts(SipMessage request,
        SipServerCandidate[] candidates)
    {
        SipEndpointAddress sentBy = new SipEndpointAddress();
        sentBy.Host = "client.example.com";
        sentBy.Port = 5060;
        return new SipClientTransactionAttempts(request, candidates, sentBy,
            true, new AttemptRandomSource());
    }

    static SipServerCandidate Candidate(string host, int port,
        SipTransportKind transport, bool secure)
    {
        SipServerCandidate candidate = new SipServerCandidate();
        candidate.Host = host;
        candidate.Port = port;
        candidate.Transport = transport;
        candidate.Secure = secure;
        candidate.RecordKind = SipDnsRecordKind.A;
        return candidate;
    }

    static SipSendResult SendResult(string connectionId, string flowId,
        SipTransportKind transport)
    {
        SipSendResult result = new SipSendResult();
        result.Success = true;
        result.Connection = Connection(connectionId, transport);
        result.Flow = Flow(flowId, result.Connection);
        return result;
    }

    static SipConnectionInfo Connection(string id, SipTransportKind transport)
    {
        SipConnectionInfo connection = new SipConnectionInfo();
        connection.Id = id;
        connection.State = SipConnectionState.Connected;
        connection.Transport = transport;
        return connection;
    }

    static SipFlow Flow(string id, SipConnectionInfo connection)
    {
        SipFlow flow = new SipFlow();
        flow.Id = id;
        flow.Connection = connection;
        return flow;
    }

    static SipTransportMessage Response(string branch, SipTransportKind transport)
    {
        string transportToken = transport == SipTransportKind.Tcp ? "TCP" : "UDP";
        SipTransportMessage envelope = new SipTransportMessage();
        envelope.Transport = transport;
        envelope.Message = Parse("SIP/2.0 180 Ringing\r\n"
            + "Via: SIP/2.0/" + transportToken
            + " client.example.com:5060;branch=" + branch + ";rport\r\n"
            + "From: <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: <sip:service@example.com>;tag=to-tag\r\n"
            + "Call-ID: attempts@example.com\r\n"
            + "CSeq: 1 OPTIONS\r\n"
            + "Content-Length: 0\r\n\r\n");
        return envelope;
    }

    static SipMessage Request(string branch)
    {
        return Parse("OPTIONS sip:service@service.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=" + branch + "\r\n"
            + "Via: SIP/2.0/TCP lower.example.com:5070;branch=z9hG4bK-lower\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: <sip:service@example.com>\r\n"
            + "Call-ID: attempts@example.com\r\n"
            + "CSeq: 1 OPTIONS\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
        return result.Message!;
    }
}

class AttemptRandomSource : ISipRandomSource
{
    byte next = 1;

    public void FillBytes(byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = next;
            index = index + 1;
        }
        next = (byte)(next + 1);
    }
}
