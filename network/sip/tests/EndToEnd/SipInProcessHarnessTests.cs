namespace sip.Tests;

public class SipInProcessHarnessTests
{
    [TestCase(SipTransportKind.Udp)]
    [TestCase(SipTransportKind.Tcp)]
    [TestCase(SipTransportKind.Tls)]
    [TestCase(SipTransportKind.WebSocket)]
    [TestCase(SipTransportKind.SecureWebSocket)]
    [TestCase(SipTransportKind.Custom)]
    public void ExchangesParsedRequestsOverEveryImplementedTransport(
        SipTransportKind transport)
    {
        using SipInProcessHarness harness = new SipInProcessHarness();

        SipSendResult sent = harness.SendFromUac(
            Request(1), transport, "transport-" + transport);
        harness.DeliverPackets();
        List<SipStackEvent> received =
            SipInProcessHarness.Drain(harness.Uas);

        Assert.Multiple(() =>
        {
            Assert.That(sent.Success, Is.True, sent.ErrorMessage);
            Assert.That(received.Any(value => value.Kind
                == SipStackEventKind.Transport
                && value.TransportEvent!.Transport == transport), Is.True);
            Assert.That(received.Any(value => value.Kind
                == SipStackEventKind.IncomingRequest
                && value.Message!.Method == SipMethod.Options
                && value.CorrelationId == "transport-" + transport), Is.True);
            Assert.That(received.Any(value => value.Kind
                == SipStackEventKind.Transaction
                && value.CorrelationId == "transport-" + transport), Is.True);
        });
    }

    [Test]
    public void AppliesLossDuplicationReorderingAndFragmentationDeterministically()
    {
        using SipInProcessHarness harness = new SipInProcessHarness();
        harness.Network.DropNextPacketCount = 1;
        harness.SendFromUac(Request(1), SipTransportKind.Udp, "lost");
        harness.DeliverPackets();
        List<SipStackEvent> lost = SipInProcessHarness.Drain(harness.Uas);

        harness.Network.DuplicateNextPacketCount = 1;
        harness.SendFromUac(Request(2), SipTransportKind.Udp, "duplicated");
        harness.DeliverPackets();
        List<SipStackEvent> duplicated =
            SipInProcessHarness.Drain(harness.Uas);

        harness.Network.ReorderNextPair = true;
        harness.SendFromUac(Request(3), SipTransportKind.Tcp, "third");
        harness.SendFromUac(Request(4), SipTransportKind.Tcp, "fourth");
        harness.DeliverPackets();
        List<SipStackEvent> reordered =
            SipInProcessHarness.Drain(harness.Uas);

        harness.Network.FragmentBytes = 7;
        harness.SendFromUac(Request(5), SipTransportKind.WebSocket,
            "fragmented");
        harness.DeliverPackets();
        List<SipStackEvent> fragmented =
            SipInProcessHarness.Drain(harness.Uas);

        Assert.Multiple(() =>
        {
            Assert.That(lost.Any(value => value.Kind
                == SipStackEventKind.IncomingRequest), Is.False);
            Assert.That(duplicated.Count(value => value.Kind
                == SipStackEventKind.IncomingRequest), Is.EqualTo(2));
            Assert.That(reordered.Where(value => value.Kind
                == SipStackEventKind.IncomingRequest).Select(value =>
                    value.Message!.CSeq!.SequenceNumber),
                Is.EqualTo(new int[] { 4, 3 }));
            Assert.That(harness.Network.LastFragmentCount,
                Is.GreaterThan(1));
            Assert.That(fragmented.Any(value => value.Kind
                == SipStackEventKind.IncomingRequest
                && value.CorrelationId == "fragmented"), Is.True);
        });
    }

    [Test]
    public void SharesDeterministicClockScriptedDnsAndCertificateFixtures()
    {
        using SipInProcessHarness harness = new SipInProcessHarness();
        harness.Network.DropNextPacketCount = 1;
        harness.SendFromUac(Request(10), SipTransportKind.Udp, "timer");
        SipInProcessHarness.Drain(harness.Uac);
        harness.AdvanceMilliseconds(10);
        List<SipStackEvent> timerEvents =
            SipInProcessHarness.Drain(harness.Uac);

        using SipHarnessDnsScript dns = new SipHarnessDnsScript();
        dns.AddAddress("uas.test", "192.0.2.88");
        SipResolutionRequest resolutionRequest = new SipResolutionRequest();
        resolutionRequest.Uri = ParseUri(
            "sip:service@uas.test:5090;transport=tcp");
        resolutionRequest.CorrelationId = "scripted-dns";
        SipResolutionResult resolution = harness.Uac.GetEndpointDnsResolver()
            .ResolveTarget(resolutionRequest, dns.CreateOptions());

        using System.Security.Cryptography.X509Certificates.X509Certificate2
            serverCertificate =
                System.Security.Cryptography.X509Certificates.X509Certificate2
                    .CreateFromPem(
                        System.Text.Encoding.ASCII.GetString(
                            harness.ServerCertificate.CertificateBytes),
                        System.Text.Encoding.ASCII.GetString(
                            harness.ServerCertificate.PrivateKeyBytes));

        Assert.Multiple(() =>
        {
            Assert.That(timerEvents.Any(value => value.Kind
                == SipStackEventKind.Transaction
                && value.TransactionEvent!.Kind
                    == SipTransactionEventKind.TimerFired), Is.True);
            Assert.That(harness.Clock.GetMilliseconds(), Is.EqualTo(1010));
            Assert.That(resolution.Status,
                Is.EqualTo(SipResolutionStatus.Success),
                resolution.ErrorMessage);
            Assert.That(resolution.Candidates[0].Host,
                Is.EqualTo("192.0.2.88"));
            Assert.That(resolution.Candidates[0].Port, Is.EqualTo(5090));
            Assert.That(resolution.Candidates[0].Transport,
                Is.EqualTo(SipTransportKind.Tcp));
            Assert.That(resolution.CorrelationId, Is.EqualTo("scripted-dns"));
            Assert.That(dns.ResponseCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(serverCertificate.HasPrivateKey, Is.True);
            Assert.That(harness.ServerCertificate.Identity.Domains,
                Is.EqualTo(new string[] { "server.test" }));
            Assert.That(harness.ClientCertificate.Identity.Domains,
                Is.EqualTo(new string[] { "client.test" }));
        });
    }

    static SipMessage Request(int sequence)
    {
        string text = "OPTIONS sip:service@uas.test SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP uac.test;branch=z9hG4bK-harness-"
            + sequence + "\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: UAC <sip:uac@uac.test>;tag=uac-" + sequence + "\r\n"
            + "To: UAS <sip:uas@uas.test>\r\n"
            + "Call-ID: harness-" + sequence + "\r\n"
            + "CSeq: " + sequence + " OPTIONS\r\n"
            + "Content-Length: 0\r\n\r\n";
        SipParseResult parsed = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Message!;
    }

    static SipUri ParseUri(string text)
    {
        SipParseResult parsed = new SipUriCodec().ParseUri(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Uri!;
    }
}
