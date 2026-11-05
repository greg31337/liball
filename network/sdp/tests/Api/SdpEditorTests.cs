namespace sdp.Tests.Api;

public class SdpEditorTests
{
    [Test]
    public void CloneDescription_CopiesCompleteParsedGraphWithoutSharedMutableState()
    {
        string input =
            "v=0\r\n" +
            "o=- 1 2 IN IP4 192.0.2.1\r\n" +
            "s=Clone source\r\n" +
            "i=Session information\r\n" +
            "u=https://example.test/session\r\n" +
            "e=person@example.test\r\n" +
            "p=+1 555 0100\r\n" +
            "c=IN IP4 192.0.2.1\r\n" +
            "b=AS:128\r\n" +
            "t=0 0\r\n" +
            "r=7d 1h 0\r\n" +
            "z=2882844526 -1h\r\n" +
            "k=prompt\r\n" +
            "a=tool:clone-test\r\n" +
            "m=audio 49170 RTP/AVP 0\r\n" +
            "i=Media information\r\n" +
            "c=IN IP4 192.0.2.2\r\n" +
            "b=TIAS:64000\r\n" +
            "k=prompt\r\n" +
            "a=sendrecv\r\n";
        SdpParseResult parsed = new SdpParser().ParseText(input, new SdpParseOptions
        {
            Mode = SdpParseMode.Lossless
        });
        SdpSessionDescription source = parsed.Description!;
        ISdpEditor editor = new SdpEditor();

        SdpSessionDescription clone = editor.CloneDescription(source);

        Assert.Multiple(() =>
        {
            Assert.That(clone, Is.Not.SameAs(source));
            Assert.That(clone.Emails, Is.Not.SameAs(source.Emails));
            Assert.That(clone.Phones, Is.Not.SameAs(source.Phones));
            Assert.That(clone.Bandwidths, Is.Not.SameAs(source.Bandwidths));
            Assert.That(clone.Timings, Is.Not.SameAs(source.Timings));
            Assert.That(clone.TimeZoneAdjustments, Is.Not.SameAs(source.TimeZoneAdjustments));
            Assert.That(clone.Attributes, Is.Not.SameAs(source.Attributes));
            Assert.That(clone.MediaDescriptions, Is.Not.SameAs(source.MediaDescriptions));
            Assert.That(clone.RawLines, Is.Not.SameAs(source.RawLines));
            Assert.That(clone.Origin, Is.Not.SameAs(source.Origin));
            Assert.That(clone.Origin!.Location, Is.Not.SameAs(source.Origin!.Location));
            Assert.That(clone.Connection, Is.Not.SameAs(source.Connection));
            Assert.That(clone.Connection!.Location, Is.Not.SameAs(source.Connection!.Location));
            Assert.That(clone.Bandwidths[0], Is.Not.SameAs(source.Bandwidths[0]));
            Assert.That(clone.Bandwidths[0].Location, Is.Not.SameAs(source.Bandwidths[0].Location));
            Assert.That(clone.Timings[0], Is.Not.SameAs(source.Timings[0]));
            Assert.That(clone.Timings[0].RepeatTimes[0], Is.Not.SameAs(source.Timings[0].RepeatTimes[0]));
            Assert.That(clone.Timings[0].RepeatTimes[0].RepeatInterval, Is.Not.SameAs(source.Timings[0].RepeatTimes[0].RepeatInterval));
            Assert.That(clone.Timings[0].RepeatTimes[0].Offsets[0], Is.Not.SameAs(source.Timings[0].RepeatTimes[0].Offsets[0]));
            Assert.That(clone.TimeZoneAdjustments[0], Is.Not.SameAs(source.TimeZoneAdjustments[0]));
            Assert.That(clone.TimeZoneAdjustments[0].Offset, Is.Not.SameAs(source.TimeZoneAdjustments[0].Offset));
            Assert.That(clone.EncryptionKey, Is.Not.SameAs(source.EncryptionKey));
            Assert.That(clone.Attributes[0], Is.Not.SameAs(source.Attributes[0]));
            Assert.That(clone.MediaDescriptions[0], Is.Not.SameAs(source.MediaDescriptions[0]));
            Assert.That(clone.MediaDescriptions[0].Formats, Is.Not.SameAs(source.MediaDescriptions[0].Formats));
            Assert.That(clone.MediaDescriptions[0].Connections, Is.Not.SameAs(source.MediaDescriptions[0].Connections));
            Assert.That(clone.MediaDescriptions[0].Bandwidths, Is.Not.SameAs(source.MediaDescriptions[0].Bandwidths));
            Assert.That(clone.MediaDescriptions[0].Attributes, Is.Not.SameAs(source.MediaDescriptions[0].Attributes));
            Assert.That(clone.MediaDescriptions[0].RawLines, Is.Not.SameAs(source.MediaDescriptions[0].RawLines));
            Assert.That(clone.MediaDescriptions[0].Connections[0], Is.Not.SameAs(source.MediaDescriptions[0].Connections[0]));
            Assert.That(clone.MediaDescriptions[0].Bandwidths[0], Is.Not.SameAs(source.MediaDescriptions[0].Bandwidths[0]));
            Assert.That(clone.MediaDescriptions[0].EncryptionKey, Is.Not.SameAs(source.MediaDescriptions[0].EncryptionKey));
            Assert.That(clone.MediaDescriptions[0].Attributes[0], Is.Not.SameAs(source.MediaDescriptions[0].Attributes[0]));
            Assert.That(clone.RawLines[0], Is.Not.SameAs(source.RawLines[0]));
            Assert.That(clone.RawLines[0].Location, Is.Not.SameAs(source.RawLines[0].Location));
            Assert.That(clone.MediaDescriptions[0].RawLines[0], Is.Not.SameAs(source.MediaDescriptions[0].RawLines[0]));
            Assert.That(clone.MediaDescriptions[0].RawLines[0].Location, Is.Not.SameAs(source.MediaDescriptions[0].RawLines[0].Location));
            Assert.That(new SdpWriter().WriteText(clone, new SdpWriteOptions { Mode = SdpWriteMode.Lossless }).Text, Is.EqualTo(input));
        });

        clone.Origin!.SessionVersion = "9";
        clone.Connection!.Address = "203.0.113.1";
        clone.Timings[0].RepeatTimes[0].RepeatInterval.Value = 8;
        clone.TimeZoneAdjustments[0].Offset.Value = -2;
        clone.EncryptionKey!.Method = "extension";
        clone.Attributes[0].Value = "changed";
        clone.MediaDescriptions[0].Formats.Add("96");
        clone.MediaDescriptions[0].Connections[0].Address = "203.0.113.2";
        clone.RawLines[0].Text = "v=9";

        Assert.Multiple(() =>
        {
            Assert.That(source.Origin!.SessionVersion, Is.EqualTo("2"));
            Assert.That(source.Connection!.Address, Is.EqualTo("192.0.2.1"));
            Assert.That(source.Timings[0].RepeatTimes[0].RepeatInterval.Value, Is.EqualTo(7));
            Assert.That(source.TimeZoneAdjustments[0].Offset.Value, Is.EqualTo(-1));
            Assert.That(source.EncryptionKey!.Method, Is.EqualTo("prompt"));
            Assert.That(source.Attributes[0].Value, Is.EqualTo("clone-test"));
            Assert.That(source.MediaDescriptions[0].Formats, Is.EqualTo(new[] { "0" }));
            Assert.That(source.MediaDescriptions[0].Connections[0].Address, Is.EqualTo("192.0.2.2"));
            Assert.That(source.RawLines[0].Text, Is.EqualTo("v=0"));
        });
    }

    [Test]
    public void AttributeOperations_PreserveOrderAndReturnReplacedOrRemovedInstances()
    {
        SdpSessionDescription description = DescriptionWithMedia();
        ISdpEditor editor = new SdpEditor();
        SdpAttribute first = ValueAttribute("x-first", "1");
        SdpAttribute middle = ValueAttribute("x-middle", "2");
        SdpAttribute last = ValueAttribute("x-last", "3");
        editor.AddSessionAttribute(description, first);
        editor.AddSessionAttribute(description, last);
        editor.InsertSessionAttribute(description, 1, middle);
        SdpAttribute replacement = ValueAttribute("x-replacement", "4");
        SdpAttribute replaced = editor.ReplaceSessionAttribute(description, 1, replacement);
        SdpAttribute removed = editor.RemoveSessionAttributeAt(description, 0);
        editor.AddSessionAttribute(description, ValueAttribute("TOOL", "one"));
        editor.AddSessionAttribute(description, ValueAttribute("tool", "two"));

        int removedByName = editor.RemoveSessionAttributesByName(description, "Tool");

        SdpAttribute mediaFirst = ValueAttribute("x-media-first", "1");
        SdpAttribute mediaMiddle = ValueAttribute("x-media-middle", "2");
        SdpAttribute mediaLast = ValueAttribute("x-media-last", "3");
        editor.AddMediaAttribute(description, 0, mediaFirst);
        editor.AddMediaAttribute(description, 0, mediaLast);
        editor.InsertMediaAttribute(description, 0, 1, mediaMiddle);
        SdpAttribute mediaReplacement = ValueAttribute("x-media-replacement", "4");
        SdpAttribute mediaReplaced = editor.ReplaceMediaAttribute(description, 0, 1, mediaReplacement);
        SdpAttribute mediaRemoved = editor.RemoveMediaAttributeAt(description, 0, 0);
        editor.AddMediaAttribute(description, 0, ValueAttribute("PTIME", "10"));
        editor.AddMediaAttribute(description, 0, ValueAttribute("ptime", "20"));

        int mediaRemovedByName = editor.RemoveMediaAttributesByName(description, 0, "ptime");

        Assert.Multiple(() =>
        {
            Assert.That(replaced, Is.SameAs(middle));
            Assert.That(removed, Is.SameAs(first));
            Assert.That(removedByName, Is.EqualTo(2));
            Assert.That(description.Attributes, Is.EqualTo(new[] { replacement, last }));
            Assert.That(mediaReplaced, Is.SameAs(mediaMiddle));
            Assert.That(mediaRemoved, Is.SameAs(mediaFirst));
            Assert.That(mediaRemovedByName, Is.EqualTo(2));
            Assert.That(description.MediaDescriptions[0].Attributes, Is.EqualTo(new[] { mediaReplacement, mediaLast }));
        });
    }

    [Test]
    public void MediaOperations_PreserveObjectIdentityAndFinalOrdering()
    {
        SdpSessionDescription description = MinimalDescription();
        ISdpEditor editor = new SdpEditor();
        SdpMediaDescription audio = Media("audio", 4000);
        SdpMediaDescription application = Media("application", 5000);
        SdpMediaDescription video = Media("video", 6000);
        video.HasPortCount = true;
        video.PortCount = 2;
        video.Attributes.Add(ValueAttribute("mid", "video"));

        editor.AddMedia(description, audio);
        editor.AddMedia(description, video);
        editor.InsertMedia(description, 1, application);
        editor.MoveMedia(description, 2, 0);
        SdpMediaDescription removed = editor.RemoveMediaAt(description, 1);
        editor.RejectMedia(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.SameAs(audio));
            Assert.That(description.MediaDescriptions, Is.EqualTo(new[] { video, application }));
            Assert.That(description.MediaDescriptions[0], Is.SameAs(video));
            Assert.That(video.Port, Is.Zero);
            Assert.That(video.HasPortCount, Is.True);
            Assert.That(video.PortCount, Is.EqualTo(2));
            Assert.That(video.Formats, Is.EqualTo(new[] { "0" }));
            Assert.That(video.Attributes[0].Value, Is.EqualTo("video"));
        });

        editor.ReactivateMedia(description, 0, 7000);

        Assert.That(video.Port, Is.EqualTo(7000));
    }

    [Test]
    public void MoveMedia_UsesFinalIndexesInBothDirectionsAndPreservesReferencesForNoOp()
    {
        SdpSessionDescription description = MinimalDescription();
        SdpMediaDescription audio = Media("audio", 4000);
        SdpMediaDescription video = Media("video", 5000);
        SdpMediaDescription application = Media("application", 6000);
        description.MediaDescriptions.Add(audio);
        description.MediaDescriptions.Add(video);
        description.MediaDescriptions.Add(application);
        ISdpEditor editor = new SdpEditor();

        editor.MoveMedia(description, 0, 2);
        Assert.That(description.MediaDescriptions, Is.EqualTo(new[] { video, application, audio }));

        editor.MoveMedia(description, 2, 1);
        Assert.That(description.MediaDescriptions, Is.EqualTo(new[] { video, audio, application }));

        editor.MoveMedia(description, 1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(description.MediaDescriptions, Is.EqualTo(new[] { video, audio, application }));
            Assert.That(description.MediaDescriptions[0], Is.SameAs(video));
            Assert.That(description.MediaDescriptions[1], Is.SameAs(audio));
            Assert.That(description.MediaDescriptions[2], Is.SameAs(application));
        });
    }

    [Test]
    public void RemoveAttributesByName_PreservesInterleavedUnmatchedOrderAndIdentity()
    {
        SdpSessionDescription description = DescriptionWithMedia();
        SdpAttribute sessionFirst = ValueAttribute("x-first", "1");
        SdpAttribute sessionMatchOne = ValueAttribute("TOOL", "remove-one");
        SdpAttribute sessionMiddle = ValueAttribute("x-middle", "2");
        SdpAttribute sessionMatchTwo = ValueAttribute("tool", "remove-two");
        SdpAttribute sessionLast = ValueAttribute("x-last", "3");
        description.Attributes.AddRange(new[] { sessionFirst, sessionMatchOne, sessionMiddle, sessionMatchTwo, sessionLast });
        SdpAttribute mediaFirst = ValueAttribute("x-media-first", "1");
        SdpAttribute mediaMatchOne = ValueAttribute("PTIME", "10");
        SdpAttribute mediaMiddle = ValueAttribute("x-media-middle", "2");
        SdpAttribute mediaMatchTwo = ValueAttribute("ptime", "20");
        SdpAttribute mediaLast = ValueAttribute("x-media-last", "3");
        description.MediaDescriptions[0].Attributes.AddRange(new[] { mediaFirst, mediaMatchOne, mediaMiddle, mediaMatchTwo, mediaLast });
        ISdpEditor editor = new SdpEditor();

        int sessionRemoved = editor.RemoveSessionAttributesByName(description, "Tool");
        int mediaRemoved = editor.RemoveMediaAttributesByName(description, 0, "pTiMe");
        int absentRemoved = editor.RemoveSessionAttributesByName(description, "x-absent");

        Assert.Multiple(() =>
        {
            Assert.That(sessionRemoved, Is.EqualTo(2));
            Assert.That(mediaRemoved, Is.EqualTo(2));
            Assert.That(absentRemoved, Is.Zero);
            Assert.That(description.Attributes, Is.EqualTo(new[] { sessionFirst, sessionMiddle, sessionLast }));
            Assert.That(description.Attributes[0], Is.SameAs(sessionFirst));
            Assert.That(description.Attributes[1], Is.SameAs(sessionMiddle));
            Assert.That(description.Attributes[2], Is.SameAs(sessionLast));
            Assert.That(description.MediaDescriptions[0].Attributes, Is.EqualTo(new[] { mediaFirst, mediaMiddle, mediaLast }));
            Assert.That(description.MediaDescriptions[0].Attributes[0], Is.SameAs(mediaFirst));
            Assert.That(description.MediaDescriptions[0].Attributes[1], Is.SameAs(mediaMiddle));
            Assert.That(description.MediaDescriptions[0].Attributes[2], Is.SameAs(mediaLast));
        });
    }

    [Test]
    public void IncrementOriginVersion_IsAtomicForSuccessInvalidStateAndOverflow()
    {
        ISdpEditor editor = new SdpEditor();
        SdpSessionDescription description = MinimalDescription();
        description.Origin!.SessionVersion = "00041";

        editor.IncrementOriginVersion(description);

        Assert.That(description.Origin.SessionVersion, Is.EqualTo("42"));

        description.Origin.SessionVersion = ulong.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Assert.That(() => editor.IncrementOriginVersion(description), Throws.TypeOf<InvalidOperationException>());
        Assert.That(description.Origin.SessionVersion, Is.EqualTo("18446744073709551615"));

        description.Origin.SessionVersion = "invalid";
        Assert.That(() => editor.IncrementOriginVersion(description), Throws.TypeOf<InvalidOperationException>());
        Assert.That(description.Origin.SessionVersion, Is.EqualTo("invalid"));

        description.Origin = null;
        Assert.That(() => editor.IncrementOriginVersion(description), Throws.TypeOf<InvalidOperationException>());
        Assert.That(description.Origin, Is.Null);
    }

    [Test]
    public void Editor_RejectsInvalidArgumentsWithoutMutation()
    {
        ISdpEditor editor = new SdpEditor();
        SdpSessionDescription description = DescriptionWithMedia();
        SdpAttribute existing = ValueAttribute("x-existing", "value");
        description.Attributes.Add(existing);
        SdpMediaDescription existingMedia = description.MediaDescriptions[0];
        int originalPort = existingMedia.Port;

        Assert.Multiple(() =>
        {
            Assert.That(() => editor.CloneDescription(null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => editor.AddSessionAttribute(description, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("attribute"));
            Assert.That(() => editor.AddMediaAttribute(description, 1, ValueAttribute("x", "x")), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => editor.InsertSessionAttribute(description, 2, ValueAttribute("x", "x")), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("attributeIndex"));
            Assert.That(() => editor.InsertMediaAttribute(description, 0, 1, ValueAttribute("x", "x")), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("attributeIndex"));
            Assert.That(() => editor.ReplaceSessionAttribute(description, 1, ValueAttribute("x", "x")), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("attributeIndex"));
            Assert.That(() => editor.ReplaceMediaAttribute(description, 1, 0, ValueAttribute("x", "x")), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => editor.RemoveSessionAttributeAt(description, 1), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("attributeIndex"));
            Assert.That(() => editor.RemoveMediaAttributeAt(description, 0, 0), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("attributeIndex"));
            Assert.That(() => editor.RemoveSessionAttributesByName(description, "not a token"), Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("attributeName"));
            Assert.That(() => editor.RemoveMediaAttributesByName(description, 1, "x"), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => editor.AddMedia(description, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("media"));
            Assert.That(() => editor.InsertMedia(description, 2, Media("video", 5000)), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => editor.MoveMedia(description, 0, 1), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("destinationMediaIndex"));
            Assert.That(() => editor.RemoveMediaAt(description, 1), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => editor.RejectMedia(description, 1), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => editor.ReactivateMedia(description, 0, 7000), Throws.TypeOf<InvalidOperationException>());
        });

        editor.RejectMedia(description, 0);
        Assert.That(() => editor.ReactivateMedia(description, 0, 65535), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("port"));

        Assert.Multiple(() =>
        {
            Assert.That(description.Attributes, Is.EqualTo(new[] { existing }));
            Assert.That(description.MediaDescriptions, Is.EqualTo(new[] { existingMedia }));
            Assert.That(existingMedia.Port, Is.Zero);
            Assert.That(originalPort, Is.EqualTo(49170));
        });
    }

    [Test]
    public void NormalizeDescription_CanonicalizesModelStateAndIsIdempotent()
    {
        SdpSessionDescription description = MinimalDescription();
        description.Origin!.SessionId = "0001";
        description.Origin.SessionVersion = "0002";
        description.Origin.NetworkType = "in";
        description.Origin.AddressType = "ip4";
        description.Origin.AddressKind = SdpAddressKind.Unknown;
        description.Origin.RawText = "stale origin";
        description.Connection!.NetworkType = "in";
        description.Connection.AddressType = "ip4";
        description.Connection.AddressKind = SdpAddressKind.Unknown;
        description.Connection.RawAddress = "stale address";
        description.Connection.RawText = "stale connection";
        description.Bandwidths.Add(new SdpBandwidth
        {
            Type = "as",
            Value = 128,
            RawValue = "000128",
            RawText = "as:000128"
        });
        description.Timings[0].RepeatTimes.Add(new SdpRepeatTime
        {
            RepeatInterval = new SdpTypedTime { Value = 1, Unit = "d", RawValue = "1d" },
            ActiveDuration = new SdpTypedTime { Value = 1, Unit = "h", RawValue = "1h" },
            Offsets =
            {
                new SdpTypedTime { Value = 0, Unit = "s", RawValue = "0s" },
                new SdpTypedTime { Value = 1, Unit = "h", RawValue = "1h" }
            },
            RawText = "1d 1h 0s 1h"
        });
        description.TimeZoneAdjustments.Add(new SdpTimeZoneAdjustment
        {
            AdjustmentTime = "2882844526",
            Offset = new SdpTypedTime { Value = -1, Unit = "h", RawValue = "-1h" }
        });
        description.EncryptionKey = new SdpEncryptionKey { Method = "PROMPT", RawText = "PROMPT" };
        description.Attributes.Add(ValueAttribute("TOOL", "editor"));
        description.Attributes.Add(ValueAttribute("GROUP", "BUNDLE   video"));
        description.Attributes.Add(ValueAttribute("tool", "editor"));
        description.Attributes.Add(ValueAttribute("X-Zeta", "  opaque  value  "));
        description.RawLines.Add(new SdpRawLine
        {
            FieldName = 'v',
            Text = "v=0",
            Value = "0",
            LineEnding = SdpLineEnding.Lf
        });
        description.LineEnding = SdpLineEnding.Lf;
        SdpMediaDescription media = Media("VIDEO", 49170);
        media.MediaKind = SdpMediaKind.Audio;
        media.Protocol = "rtp/avp";
        media.Formats.Clear();
        media.Formats.Add("096");
        media.Formats.Add("96");
        media.Formats.Add("0");
        media.Formats.Add("0");
        media.Connections.Add(new SdpConnectionData
        {
            NetworkType = "in",
            AddressType = "ip4",
            Address = "192.0.2.2",
            AddressKind = SdpAddressKind.Unknown,
            RawAddress = "stale",
            RawText = "stale"
        });
        media.Bandwidths.Add(new SdpBandwidth { Type = "tias", Value = 64000, RawValue = "064000", RawText = "tias:064000" });
        media.EncryptionKey = new SdpEncryptionKey { Method = "PROMPT", RawText = "PROMPT" };
        media.Attributes.Add(ValueAttribute("SENDRECV", string.Empty));
        media.Attributes[0].Kind = SdpAttributeKind.Property;
        media.Attributes.Add(ValueAttribute("RTPMAP", "96 opus/48000/2"));
        media.Attributes.Add(ValueAttribute("sendrecv", string.Empty));
        media.Attributes[2].Kind = SdpAttributeKind.Property;
        media.RawLines.Add(new SdpRawLine
        {
            FieldName = 'm',
            Text = "m=VIDEO 49170 rtp/avp 096 96 0 0",
            Value = "VIDEO 49170 rtp/avp 096 96 0 0",
            LineEnding = SdpLineEnding.Lf
        });
        description.MediaDescriptions.Add(media);
        ISdpEditor editor = new SdpEditor();

        editor.NormalizeDescription(description);
        SdpWriteResult firstWrite = new SdpWriter().WriteText(description, new SdpWriteOptions());
        editor.NormalizeDescription(description);
        SdpWriteResult secondWrite = new SdpWriter().WriteText(description, new SdpWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(description.Origin.SessionId, Is.EqualTo("1"));
            Assert.That(description.Origin.SessionVersion, Is.EqualTo("2"));
            Assert.That(description.Origin.NetworkType, Is.EqualTo("IN"));
            Assert.That(description.Origin.AddressType, Is.EqualTo("IP4"));
            Assert.That(description.Origin.AddressKind, Is.EqualTo(SdpAddressKind.Ipv4));
            Assert.That(description.Origin.RawText, Is.Empty);
            Assert.That(description.Connection.NetworkType, Is.EqualTo("IN"));
            Assert.That(description.Connection.AddressType, Is.EqualTo("IP4"));
            Assert.That(description.Connection.AddressKind, Is.EqualTo(SdpAddressKind.Ipv4));
            Assert.That(description.Connection.RawText, Is.Empty);
            Assert.That(description.Bandwidths[0].Type, Is.EqualTo("AS"));
            Assert.That(description.Timings[0].RepeatTimes[0].RepeatInterval.Value, Is.EqualTo(86400));
            Assert.That(description.Timings[0].RepeatTimes[0].RepeatInterval.Unit, Is.Empty);
            Assert.That(description.Timings[0].RepeatTimes[0].ActiveDuration.Value, Is.EqualTo(3600));
            Assert.That(description.Timings[0].RepeatTimes[0].Offsets.Select(value => value.Value), Is.EqualTo(new long[] { 0, 3600 }));
            Assert.That(description.TimeZoneAdjustments[0].Offset.Value, Is.EqualTo(-3600));
            Assert.That(description.EncryptionKey.Method, Is.EqualTo("prompt"));
            Assert.That(description.Attributes.Select(attribute => attribute.Name), Is.EqualTo(new[] { "group", "tool", "X-Zeta" }));
            Assert.That(description.Attributes[0].Value, Is.EqualTo("BUNDLE video"));
            Assert.That(description.Attributes[2].Value, Is.EqualTo("  opaque  value  "));
            Assert.That(description.RawLines, Is.Empty);
            Assert.That(description.LineEnding, Is.EqualTo(SdpLineEnding.CrLf));
            Assert.That(media.MediaType, Is.EqualTo("video"));
            Assert.That(media.MediaKind, Is.EqualTo(SdpMediaKind.Video));
            Assert.That(media.Protocol, Is.EqualTo("RTP/AVP"));
            Assert.That(media.Formats, Is.EqualTo(new[] { "96", "0" }));
            Assert.That(media.Connections[0].NetworkType, Is.EqualTo("IN"));
            Assert.That(media.Connections[0].AddressKind, Is.EqualTo(SdpAddressKind.Ipv4));
            Assert.That(media.Bandwidths[0].Type, Is.EqualTo("TIAS"));
            Assert.That(media.EncryptionKey.Method, Is.EqualTo("prompt"));
            Assert.That(media.Attributes.Select(attribute => attribute.Name), Is.EqualTo(new[] { "rtpmap", "sendrecv" }));
            Assert.That(media.Attributes[0].Value, Is.EqualTo("96 opus/48000/2"));
            Assert.That(media.RawLines, Is.Empty);
            Assert.That(firstWrite.Success, Is.True, Diagnostics(firstWrite));
            Assert.That(secondWrite.Text, Is.EqualTo(firstWrite.Text));
            Assert.That(firstWrite.Text, Does.StartWith("v=0\r\no=- 1 2 IN IP4 192.0.2.1\r\ns=Editor test\r\nc=IN IP4 192.0.2.1\r\nb=AS:128\r\nt=0 0\r\n"));
        });
    }

    [Test]
    public void NormalizeDescription_FailsAtomicallyForUnrepresentableState()
    {
        SdpSessionDescription description = MinimalDescription();
        description.Origin!.NetworkType = "in";
        description.LineEnding = SdpLineEnding.Lf;
        description.RawLines.Add(new SdpRawLine
        {
            FieldName = 'v',
            Text = "v=0",
            Value = "0",
            LineEnding = SdpLineEnding.Lf
        });
        description.Timings[0].RepeatTimes.Add(new SdpRepeatTime
        {
            RepeatInterval = new SdpTypedTime { Value = long.MaxValue, Unit = "d" },
            ActiveDuration = new SdpTypedTime { Value = 1, Unit = "s" },
            Offsets = { new SdpTypedTime { Value = 0 } }
        });

        Assert.That(() => new SdpEditor().NormalizeDescription(description), Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("description"));

        Assert.Multiple(() =>
        {
            Assert.That(description.Origin.NetworkType, Is.EqualTo("in"));
            Assert.That(description.Timings[0].RepeatTimes[0].RepeatInterval.Value, Is.EqualTo(long.MaxValue));
            Assert.That(description.Timings[0].RepeatTimes[0].RepeatInterval.Unit, Is.EqualTo("d"));
            Assert.That(description.RawLines, Has.Count.EqualTo(1));
            Assert.That(description.LineEnding, Is.EqualTo(SdpLineEnding.Lf));
        });
    }

    [Test]
    public void Parser_PreservesNormalizableSpellingUntilNormalizationIsRequested()
    {
        string input =
            "v=0\n" +
            "o=- 1 1 IN IP4 192.0.2.1\n" +
            "s=Explicit normalization\n" +
            "c=IN IP4 192.0.2.1\n" +
            "t=0 0\n" +
            "r=1d 1h 0\n" +
            "a=TOOL:parser-value\n";
        SdpParseResult parsed = new SdpParser().ParseText(input, new SdpParseOptions
        {
            Mode = SdpParseMode.Lossless,
            AllowLfLineEndings = true
        });
        SdpSessionDescription description = parsed.Description!;

        Assert.Multiple(() =>
        {
            Assert.That(description.Attributes[0].Name, Is.EqualTo("TOOL"));
            Assert.That(description.Timings[0].RepeatTimes[0].RepeatInterval.Unit, Is.EqualTo("d"));
            Assert.That(description.RawLines, Is.Not.Empty);
            Assert.That(description.LineEnding, Is.EqualTo(SdpLineEnding.Lf));
        });

        new SdpEditor().NormalizeDescription(description);

        Assert.Multiple(() =>
        {
            Assert.That(description.Attributes[0].Name, Is.EqualTo("tool"));
            Assert.That(description.Timings[0].RepeatTimes[0].RepeatInterval.Value, Is.EqualTo(86400));
            Assert.That(description.Timings[0].RepeatTimes[0].RepeatInterval.Unit, Is.Empty);
            Assert.That(description.RawLines, Is.Empty);
            Assert.That(description.LineEnding, Is.EqualTo(SdpLineEnding.CrLf));
        });
    }

    static SdpSessionDescription DescriptionWithMedia()
    {
        SdpSessionDescription description = MinimalDescription();
        SdpMediaDescription media = Media("audio", 49170);
        media.HasPortCount = true;
        media.PortCount = 2;
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
            "Editor test",
            new SdpConnectionData
            {
                NetworkType = SdpNetworkTypes.Internet,
                AddressType = SdpAddressTypes.Ip4,
                Address = "192.0.2.1",
                AddressKind = SdpAddressKind.Ipv4
            },
            new SdpTiming { StartTime = "0", StopTime = "0" });
    }

    static SdpMediaDescription Media(string mediaType, int port)
    {
        return new SdpSessionFactory().CreateMedia(mediaType, port, SdpTransportProtocols.RtpAvp, new List<string> { "0" });
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
            text = text + result.Diagnostics[index].Code + ": " + result.Diagnostics[index].Detail + " | ";
            index = index + 1;
        }
        return text;
    }
}
