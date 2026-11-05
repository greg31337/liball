namespace sdp.Tests.Unit.Writing;

public class SdpWriterCanonicalAttributeTests
{
    [TestCase("RTPMAP", "98 L16/16000/2", "rtpmap:98 L16/16000/2", false)]
    [TestCase("FMTP", " 111   minptime = 10 ; X-Vendor=opaque ; flag ", "fmtp:111 minptime=10;X-Vendor=opaque;flag", false)]
    [TestCase("RTCP", " 53020   IN IP4 126.16.64.4 ", "rtcp:53020 IN IP4 126.16.64.4", false)]
    [TestCase("RTCP-FB", " 96   ccm   fir ", "rtcp-fb:96 ccm fir", false)]
    [TestCase("RTCP-XR", "pkt-loss-rle=120   stat-summary=loss,jitt\tx-future=opaque", "rtcp-xr:pkt-loss-rle=120 stat-summary=loss,jitt x-future=opaque", false)]
    [TestCase("SSRC", "00042\tx-note:opaque value", "ssrc:42 x-note:opaque value", false)]
    [TestCase("SSRC-GROUP", " X-DUP 4294967295 0 4294967295 ", "ssrc-group:X-DUP 4294967295 0 4294967295", false)]
    [TestCase("MSID", " stream-1   track-1 ", "msid:stream-1 track-1", false)]
    [TestCase("MSID-SEMANTIC", " WMS   * ", "msid-semantic:WMS *", true)]
    [TestCase("SOURCE-FILTER", " incl IN IP4 232.3.4.5 192.0.2.10 ", "source-filter:incl IN IP4 232.3.4.5 192.0.2.10", true)]
    [TestCase("TS-REFCLK", " gps ", "ts-refclk:gps", true)]
    [TestCase("MEDIACLK", " direct=963214424   rate=1000/1001 ", "mediaclk:direct=963214424 rate=1000/1001", true)]
    [TestCase("IMAGEATTR", " 99 recv [x=330,y=250]   send [x=800,y=640] ", "imageattr:99 send [x=800,y=640] recv [x=330,y=250]", false)]
    [TestCase("GROUP", " X-REL   audio video ", "group:X-REL audio video", true)]
    [TestCase("MID", " audio-1 ", "mid:audio-1", false)]
    [TestCase("ICE-UFRAG", "8hhY", "ice-ufrag:8hhY", true)]
    [TestCase("ICE-PWD", "asd88fgpdd777uzjYhagZg", "ice-pwd:asd88fgpdd777uzjYhagZg", true)]
    [TestCase("ICE-OPTIONS", "  trickle\tXFuture  trickle rtp+ecn  ", "ice-options:trickle XFuture trickle rtp+ecn", true)]
    [TestCase("CANDIDATE", " 842163049\t1 X-QUIC 1677734910 peer.local 60769 typ x-nat generation 0 network-cost 999 generation 1 raddr 10.0.0.2 rport 8998 ", "candidate:842163049 1 X-QUIC 1677734910 peer.local 60769 typ x-nat raddr 10.0.0.2 rport 8998 generation 0 network-cost 999 generation 1", false)]
    [TestCase("REMOTE-CANDIDATES", " 2 192.0.2.3 45665\t 1 192.0.2.3   45664 ", "remote-candidates:2 192.0.2.3 45665 1 192.0.2.3 45664", false)]
    [TestCase("FINGERPRINT", " SHA-256  DE:AD:BE:EF ", "fingerprint:SHA-256 DE:AD:BE:EF", true)]
    [TestCase("SETUP", "\tPASSIVE ", "setup:passive", true)]
    [TestCase("CONNECTION", "\tNEW ", "connection:new", true)]
    [TestCase("TLS-ID", " abc3de65cddef001be82\t", "tls-id:abc3de65cddef001be82", false)]
    [TestCase("CRYPTO", "\t1  AES_CM_128_HMAC_SHA1_80 inline:key  UNENCRYPTED_SRTCP\t", "crypto:1 AES_CM_128_HMAC_SHA1_80 inline:key UNENCRYPTED_SRTCP", false)]
    [TestCase("EXTMAP", "\t2/sendonly  urn:example:x-extension  short opaque data\t", "extmap:2/sendonly urn:example:x-extension short opaque data", true)]
    [TestCase("RID", "f-1_send send pt=96,0,127;max-width=1280;vendor-mode=alpha beta=gamma", "rid:f-1_send send pt=96,0,127;max-width=1280;vendor-mode=alpha beta=gamma", false)]
    [TestCase("SIMULCAST", "send high;mid,~mid-vp9;~low recv remote-a,remote-b;~remote-low", "simulcast:send high;mid,~mid-vp9;~low recv remote-a,remote-b;~remote-low", false)]
    [TestCase("SCTP-PORT", "5000", "sctp-port:5000", false)]
    [TestCase("MAX-MESSAGE-SIZE", "65536", "max-message-size:65536", false)]
    [TestCase("SCTPMAP", "5000\twebrtc-datachannel\t16", "sctpmap:5000 webrtc-datachannel 16", false)]
    [TestCase("DCMAP", "00004 LABEL=\"foo%09bar\";SUBPROTOCOL=\"\";X-Future=opaque", "dcmap:4 label=\"foo%09bar\";subprotocol=\"\";X-Future=opaque", false)]
    [TestCase("DCSA", "00003 path:msrp://example.test/id;dc", "dcsa:3 path:msrp://example.test/id;dc", false)]
    public void Writer_CanonicalizesEachSpecializedTypedAttribute(string name, string value, string expectedAttribute, bool sessionScope)
    {
        SdpSessionDescription description = DescriptionWithAttribute(ValueAttribute(name, value), sessionScope);

        SdpWriteResult result = new SdpWriter().WriteText(description, new SdpWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Diagnostics(result));
            Assert.That(result.Text, Does.Contain("a=" + expectedAttribute + "\r\n"));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void Writer_CanonicalizesCommonAttributesAndRetainsGenericAttributes()
    {
        SdpSessionDescription description = MinimalDescription();
        description.Attributes.Add(new SdpAttribute
        {
            Kind = SdpAttributeKind.Property,
            Name = "SENDONLY",
            RawText = "SENDONLY"
        });
        description.Attributes.Add(ValueAttribute("X-Vendor-Attribute", "Opaque Extension Value"));

        SdpWriteResult retained = new SdpWriter().WriteText(description, new SdpWriteOptions());
        SdpWriteResult omitted = new SdpWriter().WriteText(description, new SdpWriteOptions { IncludeUnknownFields = false });

        Assert.Multiple(() =>
        {
            Assert.That(retained.Success, Is.True, Diagnostics(retained));
            Assert.That(retained.Text, Does.Contain("a=sendonly\r\n"));
            Assert.That(retained.Text, Does.Contain("a=X-Vendor-Attribute:Opaque Extension Value\r\n"));
            Assert.That(omitted.Success, Is.True, Diagnostics(omitted));
            Assert.That(omitted.Text, Does.Contain("a=sendonly\r\n"));
            Assert.That(omitted.Text, Does.Not.Contain("X-Vendor-Attribute"));
        });
    }

    [Test]
    public void Writer_AppliesCanonicalObsoleteAndUnknownFieldPolicies()
    {
        SdpSessionDescription description = MinimalDescription();
        description.EncryptionKey = new SdpEncryptionKey { Method = "prompt" };
        description.Attributes.Add(ValueAttribute("CAT", "conference"));
        description.Attributes.Add(ValueAttribute("KEYWDS", "one two"));
        description.Attributes.Add(ValueAttribute("X-Future", "opaque"));
        description.Attributes.Add(new SdpAttribute
        {
            Kind = SdpAttributeKind.Property,
            Name = "SENDRECV",
            RawText = "SENDRECV"
        });

        SdpWriteResult included = new SdpWriter().WriteText(description, new SdpWriteOptions());
        SdpWriteResult omitted = new SdpWriter().WriteText(description, new SdpWriteOptions
        {
            IncludeObsoleteFields = false,
            IncludeUnknownFields = false
        });

        Assert.Multiple(() =>
        {
            Assert.That(included.Success, Is.True, Diagnostics(included));
            Assert.That(included.Text, Does.Contain("k=prompt\r\n"));
            Assert.That(included.Text, Does.Contain("a=cat:conference\r\n"));
            Assert.That(included.Text, Does.Contain("a=keywds:one two\r\n"));
            Assert.That(included.Text, Does.Contain("a=X-Future:opaque\r\n"));
            Assert.That(omitted.Success, Is.True, Diagnostics(omitted));
            Assert.That(omitted.Text, Does.Not.Contain("k=prompt"));
            Assert.That(omitted.Text, Does.Not.Contain("a=cat:"));
            Assert.That(omitted.Text, Does.Not.Contain("a=keywds:"));
            Assert.That(omitted.Text, Does.Not.Contain("a=X-Future:"));
            Assert.That(omitted.Text, Does.Contain("a=sendrecv\r\n"));
        });
    }

    [Test]
    public void Writer_RejectsRecognizedAttributesThatCannotBeCanonicallyEncoded()
    {
        SdpAttribute attribute = ValueAttribute("candidate", "malformed candidate data");
        attribute.Location.LineNumber = 27;
        SdpSessionDescription description = DescriptionWithAttribute(attribute, false);

        SdpWriteResult result = new SdpWriter().WriteText(description, new SdpWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.WriteInvalidDescription));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(result.Diagnostics[0].AttributeName, Is.EqualTo("candidate"));
            Assert.That(result.Diagnostics[0].MediaIndex, Is.Zero);
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(27));
        });
    }

    static SdpSessionDescription DescriptionWithAttribute(SdpAttribute attribute, bool sessionScope)
    {
        SdpSessionDescription description = MinimalDescription();
        if (sessionScope)
        {
            description.Attributes.Add(attribute);
        }
        else
        {
            SdpMediaDescription media = new SdpMediaDescription
            {
                MediaType = SdpMediaTypes.Audio,
                MediaKind = SdpMediaKind.Audio,
                Port = 9,
                Protocol = SdpTransportProtocols.RtpSavpf
            };
            media.Formats.Add("0");
            media.Attributes.Add(attribute);
            description.MediaDescriptions.Add(media);
        }
        return description;
    }

    static SdpSessionDescription MinimalDescription()
    {
        return new SdpSessionFactory().CreateSession(
            new SdpOrigin
            {
                Username = "-",
                SessionId = "1",
                SessionVersion = "1",
                NetworkType = SdpNetworkTypes.Internet,
                AddressType = SdpAddressTypes.Ip4,
                UnicastAddress = "192.0.2.1",
                AddressKind = SdpAddressKind.Ipv4
            },
            "Canonical attributes",
            new SdpConnectionData
            {
                NetworkType = SdpNetworkTypes.Internet,
                AddressType = SdpAddressTypes.Ip4,
                Address = "192.0.2.1",
                AddressKind = SdpAddressKind.Ipv4
            },
            new SdpTiming { StartTime = "0", StopTime = "0" });
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = name,
            Value = value,
            RawText = name + ":" + value
        };
    }

    static string Diagnostics(SdpWriteResult result)
    {
        string text = string.Empty;
        int index = 0;
        while (index < result.Diagnostics.Count)
        {
            if (index > 0)
            {
                text = text + " | ";
            }
            text = text + result.Diagnostics[index].Code + ": " + result.Diagnostics[index].Detail;
            index = index + 1;
        }
        return text;
    }
}
