namespace ftp.Tests;

public class FtpEndpointCodecTests
{
    [Test]
    public void PORT_codec_round_trips_IPv4_octets_and_unsigned_port_bytes()
    {
        FtpEndpoint parsed = FtpEndpointCodec.ParsePortArgument("192,000,2,10,255,255");
        FtpEndpoint endpoint = CreateEndpoint("203.0.113.7", 50000, FtpAddressFamily.IPv4);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Host, Is.EqualTo("192.0.2.10"));
            Assert.That(parsed.Port, Is.EqualTo(65535));
            Assert.That(parsed.AddressFamily, Is.EqualTo(FtpAddressFamily.IPv4));
            Assert.That(FtpEndpointCodec.FormatPortArgument(endpoint),
                Is.EqualTo("203,0,113,7,195,80"));
        });
    }

    [Test]
    public void PORT_codec_rejects_wrong_counts_ranges_zero_port_and_non_IPv4_endpoints()
    {
        FtpEndpoint ipv6 = CreateEndpoint("2001:db8::1", 1024, FtpAddressFamily.IPv6);
        FtpEndpoint dns = CreateEndpoint("example.test", 1024, FtpAddressFamily.IPv4);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.ParsePortArgument("192,0,2,1,4"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.ParsePortArgument("192,0,2,1,4,1,0"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.ParsePortArgument("192,0,2,256,4,1"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.ParsePortArgument("192,0,2,1,0,0"));
            Assert.Throws<FtpProtocolException>(() => FtpEndpointCodec.FormatPortArgument(ipv6));
            Assert.Throws<FtpProtocolException>(() => FtpEndpointCodec.FormatPortArgument(dns));
        });
    }

    [Test]
    public void EPRT_codec_accepts_IPv4_IPv6_and_arbitrary_nonconflicting_delimiters()
    {
        FtpEndpoint ipv4 = FtpEndpointCodec.ParseExtendedPortArgument(
            "!1!192.0.2.44!49152!");
        FtpEndpoint ipv6 = FtpEndpointCodec.ParseExtendedPortArgument(
            "^2^2001:db8::8:800:200c:417a^65535^");

        Assert.Multiple(() =>
        {
            Assert.That(ipv4.Host, Is.EqualTo("192.0.2.44"));
            Assert.That(ipv4.Port, Is.EqualTo(49152));
            Assert.That(ipv4.AddressFamily, Is.EqualTo(FtpAddressFamily.IPv4));
            Assert.That(ipv6.Host, Is.EqualTo("2001:db8::8:800:200c:417a"));
            Assert.That(ipv6.Port, Is.EqualTo(65535));
            Assert.That(ipv6.AddressFamily, Is.EqualTo(FtpAddressFamily.IPv6));
            Assert.That(FtpEndpointCodec.FormatExtendedPortArgument(ipv4, '#'),
                Is.EqualTo("#1#192.0.2.44#49152#"));
            Assert.That(FtpEndpointCodec.FormatExtendedPortDefault(ipv6),
                Is.EqualTo("|2|2001:db8::8:800:200c:417a|65535|"));
        });
    }

    [Test]
    public void EPRT_codec_rejects_invalid_framing_protocol_addresses_ports_and_delimiters()
    {
        FtpEndpoint ipv6 = CreateEndpoint("2001:db8::1", 1234, FtpAddressFamily.IPv6);
        FtpEndpoint scoped = CreateEndpoint("fe80::1%3", 1234, FtpAddressFamily.IPv6);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.ParseExtendedPortArgument(" 1 192.0.2.1 21 "));
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.ParseExtendedPortArgument("|1|192.0.2.1|21"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.ParseExtendedPortArgument("|3|192.0.2.1|21|"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.ParseExtendedPortArgument("|1|example.test|21|"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.ParseExtendedPortArgument("|1|192.0.2.999|21|"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.ParseExtendedPortArgument("|2|2001:::1|21|"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.ParseExtendedPortArgument("|2|2001:db8::1|0|"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.FormatExtendedPortArgument(ipv6, ':'));
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.FormatExtendedPortArgument(ipv6, '1'));
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.FormatExtendedPortDefault(scoped));
        });
    }

    [Test]
    public void EPSV_argument_codec_distinguishes_control_protocol_explicit_protocol_and_ALL()
    {
        FtpExtendedPassiveRequest control = FtpEndpointCodec.ParseExtendedPassiveArgument(null);
        FtpExtendedPassiveRequest ipv4 = FtpEndpointCodec.ParseExtendedPassiveArgument("01");
        FtpExtendedPassiveRequest ipv6 = FtpEndpointCodec.ParseExtendedPassiveArgument("2");
        FtpExtendedPassiveRequest unsupported = FtpEndpointCodec.ParseExtendedPassiveArgument("300");
        FtpExtendedPassiveRequest all = FtpEndpointCodec.ParseExtendedPassiveArgument("all");

        Assert.Multiple(() =>
        {
            Assert.That(control.All, Is.False);
            Assert.That(control.HasProtocol, Is.False);
            Assert.That(control.AddressFamily, Is.EqualTo(FtpAddressFamily.Unspecified));
            Assert.That(FtpEndpointCodec.FormatExtendedPassiveArgument(control), Is.Null);
            Assert.That(ipv4.Protocol, Is.EqualTo(1));
            Assert.That(ipv4.AddressFamily, Is.EqualTo(FtpAddressFamily.IPv4));
            Assert.That(FtpEndpointCodec.FormatExtendedPassiveArgument(ipv4), Is.EqualTo("1"));
            Assert.That(ipv6.AddressFamily, Is.EqualTo(FtpAddressFamily.IPv6));
            Assert.That(unsupported.Protocol, Is.EqualTo(300));
            Assert.That(unsupported.Supported, Is.False);
            Assert.That(unsupported.AddressFamily, Is.EqualTo(FtpAddressFamily.Unspecified));
            Assert.That(FtpEndpointCodec.FormatExtendedPassiveArgument(unsupported), Is.EqualTo("300"));
            Assert.That(all.All, Is.True);
            Assert.That(FtpEndpointCodec.FormatExtendedPassiveArgument(all), Is.EqualTo("ALL"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.ParseExtendedPassiveArgument(""));
        });
    }

    [Test]
    public void Extended_request_parsers_retain_unknown_protocols_for_522_handling()
    {
        FtpExtendedPortRequest eprt = FtpEndpointCodec.ParseExtendedPortRequest(
            "|300|future-address|1025|");
        FtpExtendedPassiveRequest epsv = FtpEndpointCodec.ParseExtendedPassiveArgument("300");

        Assert.Multiple(() =>
        {
            Assert.That(eprt.Protocol, Is.EqualTo(300));
            Assert.That(eprt.Supported, Is.False);
            Assert.That(eprt.Endpoint.Host, Is.EqualTo("future-address"));
            Assert.That(eprt.Endpoint.Port, Is.EqualTo(1025));
            Assert.That(eprt.Endpoint.AddressFamily, Is.EqualTo(FtpAddressFamily.Unspecified));
            Assert.That(epsv.Protocol, Is.EqualTo(300));
            Assert.That(epsv.Supported, Is.False);
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.ParseExtendedPortArgument("|300|future-address|1025|"));
        });
    }

    [Test]
    public void PASV_reply_codec_generates_and_parses_the_legacy_IPv4_form()
    {
        FtpEndpoint endpoint = CreateEndpoint("198.51.100.9", 50000, FtpAddressFamily.IPv4);
        FtpReply reply = FtpEndpointCodec.CreatePassiveReply(endpoint);
        FtpPassiveReply parsed = FtpEndpointCodec.ParsePassiveReply(reply);

        Assert.Multiple(() =>
        {
            Assert.That(reply.Code, Is.EqualTo(227));
            Assert.That(reply.Text, Is.EqualTo(
                "Entering Passive Mode (198,51,100,9,195,80)"));
            Assert.That(reply.Classification.Succeeded, Is.True);
            Assert.That(parsed.Reply, Is.SameAs(reply));
            Assert.That(parsed.Endpoint.Host, Is.EqualTo("198.51.100.9"));
            Assert.That(parsed.Endpoint.Port, Is.EqualTo(50000));
            Assert.That(parsed.Extended, Is.False);
        });
    }

    [TestCase('!', "Entering Extended Passive Mode (!!!6446!)")]
    [TestCase('(', "Entering Extended Passive Mode ((((6446()")]
    [TestCase(')', "Entering Extended Passive Mode ()))6446))")]
    [TestCase('~', "Entering Extended Passive Mode (~~~6446~)")]
    public void EPSV_reply_codec_supports_every_kind_of_printable_nonconflicting_delimiter(
        char delimiter, string expectedText)
    {
        FtpReply reply = FtpEndpointCodec.CreateExtendedPassiveReply(6446, delimiter);
        FtpPassiveReply parsed = FtpEndpointCodec.ParseExtendedPassiveReply(reply);

        Assert.Multiple(() =>
        {
            Assert.That(reply.Code, Is.EqualTo(229));
            Assert.That(reply.Text, Is.EqualTo(expectedText));
            Assert.That(parsed.Endpoint.Host, Is.Empty);
            Assert.That(parsed.Endpoint.Port, Is.EqualTo(6446));
            Assert.That(parsed.Endpoint.AddressFamily, Is.EqualTo(FtpAddressFamily.Unspecified));
            Assert.That(parsed.Extended, Is.True);
        });
    }

    [Test]
    public void EPSV_reply_codec_rejects_invalid_framing_and_ports()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.ParseExtendedPassiveReply(CreateReply(229, "EPSV (||123|)")));
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.ParseExtendedPassiveReply(CreateReply(229, "EPSV (|||0|)")));
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.ParseExtendedPassiveReply(CreateReply(229, "EPSV (|||65536|)")));
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.CreateExtendedPassiveReply(21, '2'));
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.CreateExtendedPassiveReply(21, ' '));
        });
    }

    [Test]
    public void Network_protocol_reply_codec_preserves_supported_family_numbers()
    {
        FtpReply reply = FtpEndpointCodec.CreateNetworkProtocolReply(new[] { 1, 2, 300 });
        FtpNetworkProtocolReply parsed = FtpEndpointCodec.ParseNetworkProtocolReply(reply);

        Assert.Multiple(() =>
        {
            Assert.That(reply.Code, Is.EqualTo(522));
            Assert.That(reply.Text, Is.EqualTo(
                "Network protocol not supported, use (1,2,300)"));
            Assert.That(reply.Classification.PermanentFailure, Is.True);
            Assert.That(parsed.SupportedProtocols, Is.EqualTo(new[] { 1, 2, 300 }));
            Assert.That(parsed.Reply, Is.SameAs(reply));
        });
    }

    [Test]
    public void Network_protocol_reply_codec_rejects_malformed_or_empty_lists()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.ParseNetworkProtocolReply(CreateReply(522, "Unsupported ()")));
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.ParseNetworkProtocolReply(CreateReply(522, "Unsupported (1, 2)")));
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.ParseNetworkProtocolReply(CreateReply(522, "Unsupported (0)")));
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.ParseNetworkProtocolReply(CreateReply(522, "Unsupported (1) suffix")));
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.ParseNetworkProtocolReply(CreateReply(522, "Unsupported) use (1)")));
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.CreateNetworkProtocolReply(new int[0]));
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.CreateNetworkProtocolReply(new[] { 0, 1 }));
        });
    }

    [Test]
    public void Command_parser_applies_endpoint_grammars_to_PORT_EPRT_and_EPSV()
    {
        FtpCommand port = FtpCommandParser.ParseDecodedCommand("PORT 192,0,2,1,4,1");
        FtpCommand eprt = FtpCommandParser.ParseDecodedCommand("EPRT |2|2001:db8::1|1025|");
        FtpCommand epsv = FtpCommandParser.ParseDecodedCommand("EPSV ALL");

        Assert.Multiple(() =>
        {
            Assert.That(port.Kind, Is.EqualTo(FtpCommandKind.Port));
            Assert.That(eprt.Kind, Is.EqualTo(FtpCommandKind.Eprt));
            Assert.That(epsv.Kind, Is.EqualTo(FtpCommandKind.Epsv));
            Assert.Throws<FtpProtocolException>(() =>
                FtpCommandParser.ParseDecodedCommand("PORT 192,0,2,1,0,0"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpCommandParser.ParseDecodedCommand("EPRT |1|example.test|21|"));
            Assert.That(FtpCommandParser.ParseDecodedCommand("EPSV 3").Argument, Is.EqualTo("3"));
            Assert.That(FtpCommandParser.ParseDecodedCommand(
                "EPRT |300|future-address|21|").Argument,
                Is.EqualTo("|300|future-address|21|"));
        });
    }

    private static FtpEndpoint CreateEndpoint(
        string host, int port, FtpAddressFamily addressFamily)
    {
        FtpEndpoint endpoint = new FtpEndpoint();
        endpoint.Host = host;
        endpoint.Port = port;
        endpoint.AddressFamily = addressFamily;
        return endpoint;
    }

    private static FtpReply CreateReply(int code, string text)
    {
        FtpReply reply = new FtpReply();
        reply.Code = code;
        reply.Text = text;
        FtpReplyClassifier.ApplyClassification(reply);
        return reply;
    }
}
