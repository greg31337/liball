using System.Globalization;

namespace sdp.Tests.Api;

public class SdpWriterTests
{
    [Test]
    public void Writer_GeneratesMinimalCanonicalCrLfDeterministically()
    {
        SdpSessionDescription description = MinimalDescription();
        ISdpWriter writer = new SdpWriter();
        string expected =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 192.0.2.1\r\n" +
            "s=Minimal\r\n" +
            "c=IN IP4 192.0.2.1\r\n" +
            "t=0 0\r\n";

        SdpWriteResult first = writer.WriteText(description, new SdpWriteOptions());
        SdpWriteResult second = writer.WriteText(description, new SdpWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Text, Is.EqualTo(expected));
            Assert.That(first.CharactersWritten, Is.EqualTo(expected.Length));
            Assert.That(first.Diagnostics, Is.Empty);
            Assert.That(second.Success, Is.True);
            Assert.That(second.Text, Is.EqualTo(first.Text));
            Assert.That(second.CharactersWritten, Is.EqualTo(first.CharactersWritten));
            Assert.That(second.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void Writer_GeneratesCompleteCanonicalTextInRfcFieldOrder()
    {
        SdpSessionDescription description = FullDescription();
        ISdpWriter writer = new SdpWriter();
        SdpWriteOptions options = new SdpWriteOptions();
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        SdpWriteResult result;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            result = writer.WriteText(description, options);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }

        string expected =
            "v=0\r\n" +
            "o=- 1234567890 42 IN IP4 192.0.2.1\r\n" +
            "s=Canonical session\r\n" +
            "i=Session information\r\n" +
            "u=https://example.test/session\r\n" +
            "e=first@example.test\r\n" +
            "e=second@example.test\r\n" +
            "p=+1 555 0100\r\n" +
            "c=IN IP4 233.252.0.1/127/3\r\n" +
            "b=AS:128000\r\n" +
            "t=0 0\r\n" +
            "r=7d 1h 0 25h\r\n" +
            "z=2882844526 -1h\r\n" +
            "k=prompt\r\n" +
            "a=tool:sdp-tests\r\n" +
            "a=x-session:extension\r\n" +
            "m=audio 49170/2 RTP/AVP 0 96\r\n" +
            "i=Media information\r\n" +
            "c=IN IP4 192.0.2.10\r\n" +
            "b=TIAS:64000\r\n" +
            "k=prompt\r\n" +
            "a=ptime:20\r\n" +
            "a=x-media:extension\r\n";

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo(expected));
            Assert.That(result.Text, Does.EndWith("\r\n"));
            Assert.That(result.Utf8Bytes, Is.Empty);
            Assert.That(result.CharactersWritten, Is.EqualTo(expected.Length));
            Assert.That(result.BytesWritten, Is.Zero);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.AppliedOptions, Is.Not.SameAs(options));
            Assert.That(result.AppliedOptions.Mode, Is.EqualTo(SdpWriteMode.Canonical));
            Assert.That(result.AppliedOptions.LineEnding, Is.EqualTo(SdpLineEnding.CrLf));
        });

        SdpParseResult reparsed = new SdpParser().ParseText(result.Text, new SdpParseOptions());
        Assert.That(reparsed.Success, Is.True);
    }

    [Test]
    public void Writer_HonorsExplicitLineEndingAndFinalLinePolicy()
    {
        SdpSessionDescription description = MinimalDescription();
        SdpWriteOptions options = new SdpWriteOptions
        {
            LineEnding = SdpLineEnding.Lf,
            IncludeFinalLineEnding = false
        };

        SdpWriteResult result = new SdpWriter().WriteText(description, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo(
                "v=0\n" +
                "o=- 1 1 IN IP4 192.0.2.1\n" +
                "s=Minimal\n" +
                "c=IN IP4 192.0.2.1\n" +
                "t=0 0"));
            Assert.That(result.Text, Does.Not.EndWith("\n"));
            Assert.That(result.CharactersWritten, Is.EqualTo(result.Text.Length));
        });
    }

    [Test]
    public void Writer_ReturnsDiagnosticsWithoutPartialTextForUnsupportedOrInvalidWrites()
    {
        ISdpWriter writer = new SdpWriter();
        SdpSessionDescription valid = MinimalDescription();
        SdpWriteResult lossless = writer.WriteText(valid, new SdpWriteOptions { Mode = SdpWriteMode.Lossless });
        SdpWriteResult invalid = writer.WriteText(new SdpSessionDescription(), new SdpWriteOptions());
        SdpWriteResult limited = writer.WriteText(valid, new SdpWriteOptions { MaximumOutputCharacters = 5 });

        Assert.Multiple(() =>
        {
            Assert.That(lossless.Success, Is.False);
            Assert.That(lossless.Text, Is.Empty);
            Assert.That(lossless.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.WriteLosslessDataUnavailable));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Text, Is.Empty);
            Assert.That(invalid.Diagnostics, Is.Not.Empty);
            Assert.That(invalid.Diagnostics, Has.All.Matches<SdpDiagnostic>(diagnostic => diagnostic.Code == SdpDiagnosticCodes.WriteInvalidDescription || diagnostic.Code == SdpDiagnosticCodes.DiagnosticLimitReached));
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.Text, Is.Empty);
            Assert.That(limited.CharactersWritten, Is.Zero);
            Assert.That(limited.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.WriteOutputCharacterLimitExceeded));
        });
    }

    [Test]
    public void Writer_RejectsNullArgumentsAndInvalidOptions()
    {
        ISdpWriter writer = new SdpWriter();
        SdpSessionDescription description = MinimalDescription();

        Assert.Multiple(() =>
        {
            Assert.That(() => writer.WriteText(null!, new SdpWriteOptions()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => writer.WriteText(description, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("options"));
            Assert.That(() => writer.WriteText(description, new SdpWriteOptions { Mode = SdpWriteMode.Unknown }), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("options"));
            Assert.That(() => writer.WriteText(description, new SdpWriteOptions { LineEnding = SdpLineEnding.None }), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("options"));
        });
    }

    static SdpSessionDescription FullDescription()
    {
        SdpSessionDescription description = MinimalDescription();
        description.Origin!.SessionId = "1234567890";
        description.Origin.SessionVersion = "42";
        description.SessionName = "Canonical session";
        description.Information = "Session information";
        description.Uri = "https://example.test/session";
        description.Emails.Add("first@example.test");
        description.Emails.Add("second@example.test");
        description.Phones.Add("+1 555 0100");
        description.Connection = new SdpConnectionData
        {
            NetworkType = SdpNetworkTypes.Internet,
            AddressType = SdpAddressTypes.Ip4,
            Address = "233.252.0.1",
            AddressKind = SdpAddressKind.Ipv4Multicast,
            HasTimeToLive = true,
            TimeToLive = 127,
            HasAddressCount = true,
            AddressCount = 3
        };
        description.Bandwidths.Add(new SdpBandwidth { Type = SdpBandwidthTypes.ApplicationSpecific, Value = 128000 });
        description.Timings[0].RepeatTimes.Add(new SdpRepeatTime
        {
            RepeatInterval = new SdpTypedTime { Value = 7, Unit = "d" },
            ActiveDuration = new SdpTypedTime { Value = 1, Unit = "h" },
            Offsets =
            {
                new SdpTypedTime { Value = 0 },
                new SdpTypedTime { Value = 25, Unit = "h" }
            }
        });
        description.TimeZoneAdjustments.Add(new SdpTimeZoneAdjustment
        {
            AdjustmentTime = "2882844526",
            Offset = new SdpTypedTime { Value = -1, Unit = "h" }
        });
        description.EncryptionKey = new SdpEncryptionKey { Method = "prompt" };
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Tool, "sdp-tests"));
        description.Attributes.Add(ValueAttribute("x-session", "extension"));
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = SdpMediaTypes.Audio,
            MediaKind = SdpMediaKind.Audio,
            Port = 49170,
            HasPortCount = true,
            PortCount = 2,
            Protocol = SdpTransportProtocols.RtpAvp,
            Information = "Media information",
            EncryptionKey = new SdpEncryptionKey { Method = "prompt" }
        };
        media.Formats.Add("0");
        media.Formats.Add("96");
        media.Connections.Add(new SdpConnectionData
        {
            NetworkType = SdpNetworkTypes.Internet,
            AddressType = SdpAddressTypes.Ip4,
            Address = "192.0.2.10",
            AddressKind = SdpAddressKind.Ipv4
        });
        media.Bandwidths.Add(new SdpBandwidth { Type = SdpBandwidthTypes.TransportIndependentApplicationSpecific, Value = 64000 });
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.PacketTime, "20"));
        media.Attributes.Add(ValueAttribute("x-media", "extension"));
        description.MediaDescriptions.Add(media);
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
            "Minimal",
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
}
