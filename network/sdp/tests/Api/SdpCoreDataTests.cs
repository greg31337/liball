namespace sdp.Tests.Api;

public class SdpCoreDataTests
{
    [Test]
    public void SessionDescription_InitializesIndependentOrderedCollections()
    {
        SdpSessionDescription first = new SdpSessionDescription();
        SdpSessionDescription second = new SdpSessionDescription();

        first.Emails.Add("alice@example.test");
        first.Phones.Add("+1 555 0100");
        first.Bandwidths.Add(new SdpBandwidth());
        first.Timings.Add(new SdpTiming());
        first.TimeZoneAdjustments.Add(new SdpTimeZoneAdjustment());
        first.Attributes.Add(new SdpAttribute());
        first.MediaDescriptions.Add(new SdpMediaDescription());
        first.RawLines.Add(new SdpRawLine());

        Assert.Multiple(() =>
        {
            Assert.That(first.HasProtocolVersion, Is.False);
            Assert.That(first.ProtocolVersion, Is.Zero);
            Assert.That(first.Origin, Is.Null);
            Assert.That(first.HasSessionName, Is.False);
            Assert.That(first.SessionName, Is.Empty);
            Assert.That(first.Information, Is.Null);
            Assert.That(first.Uri, Is.Null);
            Assert.That(first.Connection, Is.Null);
            Assert.That(first.EncryptionKey, Is.Null);
            Assert.That(first.LineEnding, Is.EqualTo(SdpLineEnding.Unknown));
            Assert.That(second.Emails, Is.Empty);
            Assert.That(second.Phones, Is.Empty);
            Assert.That(second.Bandwidths, Is.Empty);
            Assert.That(second.Timings, Is.Empty);
            Assert.That(second.TimeZoneAdjustments, Is.Empty);
            Assert.That(second.Attributes, Is.Empty);
            Assert.That(second.MediaDescriptions, Is.Empty);
            Assert.That(second.RawLines, Is.Empty);
            Assert.That(first.Emails, Is.Not.SameAs(second.Emails));
            Assert.That(first.MediaDescriptions, Is.Not.SameAs(second.MediaDescriptions));
            Assert.That(first.RawLines, Is.Not.SameAs(second.RawLines));
        });
    }

    [Test]
    public void MediaDescription_InitializesIndependentOrderedCollections()
    {
        SdpMediaDescription first = new SdpMediaDescription();
        SdpMediaDescription second = new SdpMediaDescription();

        first.Formats.Add("111");
        first.Connections.Add(new SdpConnectionData());
        first.Bandwidths.Add(new SdpBandwidth());
        first.Attributes.Add(new SdpAttribute());
        first.RawLines.Add(new SdpRawLine());

        Assert.Multiple(() =>
        {
            Assert.That(first.MediaType, Is.Empty);
            Assert.That(first.MediaKind, Is.EqualTo(SdpMediaKind.Unknown));
            Assert.That(first.Port, Is.Zero);
            Assert.That(first.HasPortCount, Is.False);
            Assert.That(first.PortCount, Is.EqualTo(1));
            Assert.That(first.Protocol, Is.Empty);
            Assert.That(first.Information, Is.Null);
            Assert.That(first.EncryptionKey, Is.Null);
            Assert.That(second.Formats, Is.Empty);
            Assert.That(second.Connections, Is.Empty);
            Assert.That(second.Bandwidths, Is.Empty);
            Assert.That(second.Attributes, Is.Empty);
            Assert.That(second.RawLines, Is.Empty);
            Assert.That(first.Formats, Is.Not.SameAs(second.Formats));
            Assert.That(first.Connections, Is.Not.SameAs(second.Connections));
            Assert.That(first.Attributes, Is.Not.SameAs(second.Attributes));
            Assert.That(first.Location, Is.Not.SameAs(second.Location));
        });
    }

    [Test]
    public void TimingGraph_InitializesIndependentChildrenAndPreservesLosslessValues()
    {
        SdpTiming firstTiming = new SdpTiming();
        SdpTiming secondTiming = new SdpTiming();
        SdpRepeatTime firstRepeat = new SdpRepeatTime();
        SdpRepeatTime secondRepeat = new SdpRepeatTime();
        SdpTimeZoneAdjustment firstAdjustment = new SdpTimeZoneAdjustment();
        SdpTimeZoneAdjustment secondAdjustment = new SdpTimeZoneAdjustment();

        firstTiming.StartTime = "2873397496";
        firstTiming.StopTime = "2873404696";
        firstRepeat.RepeatInterval.Value = 7;
        firstRepeat.RepeatInterval.Unit = "d";
        firstRepeat.RepeatInterval.RawValue = "7d";
        firstRepeat.ActiveDuration.Value = 1;
        firstRepeat.ActiveDuration.Unit = "h";
        firstRepeat.ActiveDuration.RawValue = "1h";
        firstRepeat.Offsets.Add(new SdpTypedTime
        {
            Value = 25,
            Unit = "h",
            RawValue = "25h"
        });
        firstTiming.RepeatTimes.Add(firstRepeat);
        firstAdjustment.AdjustmentTime = "2882844526";
        firstAdjustment.Offset.Value = -1;
        firstAdjustment.Offset.Unit = "h";
        firstAdjustment.Offset.RawValue = "-1h";

        Assert.Multiple(() =>
        {
            Assert.That(firstTiming.StartTime, Is.EqualTo("2873397496"));
            Assert.That(firstTiming.StopTime, Is.EqualTo("2873404696"));
            Assert.That(firstTiming.RepeatTimes, Has.Count.EqualTo(1));
            Assert.That(firstRepeat.RepeatInterval.RawValue, Is.EqualTo("7d"));
            Assert.That(firstRepeat.ActiveDuration.RawValue, Is.EqualTo("1h"));
            Assert.That(firstRepeat.Offsets[0].RawValue, Is.EqualTo("25h"));
            Assert.That(firstAdjustment.AdjustmentTime, Is.EqualTo("2882844526"));
            Assert.That(firstAdjustment.Offset.Value, Is.EqualTo(-1));
            Assert.That(secondTiming.RepeatTimes, Is.Empty);
            Assert.That(firstTiming.RepeatTimes, Is.Not.SameAs(secondTiming.RepeatTimes));
            Assert.That(firstRepeat.Offsets, Is.Not.SameAs(secondRepeat.Offsets));
            Assert.That(firstRepeat.RepeatInterval, Is.Not.SameAs(secondRepeat.RepeatInterval));
            Assert.That(firstRepeat.ActiveDuration, Is.Not.SameAs(secondRepeat.ActiveDuration));
            Assert.That(firstAdjustment.Offset, Is.Not.SameAs(secondAdjustment.Offset));
        });
    }

    [Test]
    public void CoreLineModels_PreserveExtensionAndLosslessState()
    {
        SdpSourceLocation location = new SdpSourceLocation
        {
            CharacterOffset = 18,
            ByteOffset = 20,
            LineNumber = 2,
            ColumnNumber = 1
        };
        SdpRawLine rawLine = new SdpRawLine
        {
            FieldName = 'x',
            Text = "x=opaque",
            Value = "opaque",
            LineEnding = SdpLineEnding.Lf,
            IsMalformed = true,
            Location = location
        };
        SdpAttribute attribute = new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = "X-Custom",
            Value = string.Empty,
            RawText = "X-Custom:",
            Location = location
        };
        SdpOrigin origin = new SdpOrigin
        {
            Username = "user",
            SessionId = "18446744073709551615",
            SessionVersion = "18446744073709551614",
            NetworkType = "IN",
            AddressType = "IP6",
            UnicastAddress = "2001:db8::1",
            AddressKind = SdpAddressKind.Ipv6,
            RawText = "user 18446744073709551615 18446744073709551614 IN IP6 2001:db8::1",
            Location = location
        };
        SdpConnectionData connection = new SdpConnectionData
        {
            NetworkType = "IN",
            AddressType = "IP4",
            Address = "224.2.1.1",
            AddressKind = SdpAddressKind.Ipv4Multicast,
            HasTimeToLive = true,
            TimeToLive = 127,
            HasAddressCount = true,
            AddressCount = 3,
            RawAddress = "224.2.1.1/127/3"
        };
        SdpBandwidth bandwidth = new SdpBandwidth
        {
            Type = "X-CUSTOM",
            Value = 9000,
            RawValue = "09000",
            RawText = "X-CUSTOM:09000"
        };
        SdpEncryptionKey key = new SdpEncryptionKey
        {
            Method = "prompt",
            HasValue = true,
            Value = "secret",
            RawText = "prompt:secret"
        };

        Assert.Multiple(() =>
        {
            Assert.That(rawLine.FieldName, Is.EqualTo('x'));
            Assert.That(rawLine.Text, Is.EqualTo("x=opaque"));
            Assert.That(rawLine.LineEnding, Is.EqualTo(SdpLineEnding.Lf));
            Assert.That(rawLine.IsMalformed, Is.True);
            Assert.That(rawLine.Location.ByteOffset, Is.EqualTo(20));
            Assert.That(attribute.Kind, Is.EqualTo(SdpAttributeKind.Value));
            Assert.That(attribute.Name, Is.EqualTo("X-Custom"));
            Assert.That(attribute.Value, Is.Empty);
            Assert.That(attribute.RawText, Is.EqualTo("X-Custom:"));
            Assert.That(origin.SessionId, Is.EqualTo("18446744073709551615"));
            Assert.That(origin.AddressKind, Is.EqualTo(SdpAddressKind.Ipv6));
            Assert.That(connection.RawAddress, Is.EqualTo("224.2.1.1/127/3"));
            Assert.That(connection.TimeToLive, Is.EqualTo(127));
            Assert.That(connection.AddressCount, Is.EqualTo(3));
            Assert.That(bandwidth.Type, Is.EqualTo("X-CUSTOM"));
            Assert.That(bandwidth.RawValue, Is.EqualTo("09000"));
            Assert.That(key.HasValue, Is.True);
            Assert.That(key.Value, Is.EqualTo("secret"));
        });
    }

    [Test]
    public void SessionGraph_PreservesOptionalFieldsAndSourceOrder()
    {
        SdpSessionDescription session = new SdpSessionDescription
        {
            HasProtocolVersion = true,
            ProtocolVersion = 0,
            Origin = new SdpOrigin { Username = "alice" },
            HasSessionName = true,
            SessionName = "Example",
            Information = string.Empty,
            Uri = "https://example.test/session",
            Connection = new SdpConnectionData { Address = "203.0.113.1" },
            EncryptionKey = new SdpEncryptionKey { Method = "prompt" },
            LineEnding = SdpLineEnding.CrLf
        };
        SdpMediaDescription audio = new SdpMediaDescription
        {
            MediaType = "audio",
            MediaKind = SdpMediaKind.Audio,
            Port = 49170,
            HasPortCount = true,
            PortCount = 2,
            Protocol = "RTP/AVP",
            Information = "Primary audio"
        };
        audio.Formats.Add("0");
        audio.Formats.Add("96");
        session.Emails.Add("first@example.test");
        session.Emails.Add("second@example.test");
        session.MediaDescriptions.Add(audio);

        Assert.Multiple(() =>
        {
            Assert.That(session.HasProtocolVersion, Is.True);
            Assert.That(session.Origin?.Username, Is.EqualTo("alice"));
            Assert.That(session.Information, Is.EqualTo(string.Empty));
            Assert.That(session.Connection?.Address, Is.EqualTo("203.0.113.1"));
            Assert.That(session.Emails, Is.EqualTo(new[] { "first@example.test", "second@example.test" }));
            Assert.That(session.MediaDescriptions, Has.Count.EqualTo(1));
            Assert.That(session.MediaDescriptions[0].Formats, Is.EqualTo(new[] { "0", "96" }));
            Assert.That(session.MediaDescriptions[0].PortCount, Is.EqualTo(2));
            Assert.That(session.EncryptionKey?.Method, Is.EqualTo("prompt"));
            Assert.That(session.LineEnding, Is.EqualTo(SdpLineEnding.CrLf));
        });
    }
}
