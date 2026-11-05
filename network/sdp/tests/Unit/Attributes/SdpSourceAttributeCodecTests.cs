namespace sdp.Tests.Unit.Attributes;

public class SdpSourceAttributeCodecTests
{
    [TestCase("4294967295 cname:user@example.test", 4294967295u, "cname", true, "user@example.test", "4294967295 cname:user@example.test")]
    [TestCase(" 0   x-muted ", 0u, "x-muted", false, "", "0 x-muted")]
    [TestCase("00042\tx-note:opaque value", 42u, "x-note", true, "opaque value", "42 x-note:opaque value")]
    public void SourceAttribute_DecodesUnsignedIdentifiersAndNestedAttributes(string value, uint sourceId, string name, bool hasValue, string nestedValue, string canonicalValue)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "SSRC", Value = value, RawText = "SSRC:" + value };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpSourceAttribute projection = decoded.SynchronizationSourceAttribute ?? new SdpSourceAttribute();
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(projection);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Recognized, Is.True);
            Assert.That(decoded.SourceAttribute, Is.SameAs(source));
            Assert.That(projection.SourceAttribute, Is.SameAs(source));
            Assert.That(projection.SynchronizationSource, Is.EqualTo(sourceId));
            Assert.That(projection.Name, Is.EqualTo(name));
            Assert.That(projection.HasValue, Is.EqualTo(hasValue));
            Assert.That(projection.Value, Is.EqualTo(nestedValue));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.SynchronizationSourceAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("ssrc:" + canonicalValue));
        });
    }

    [Test]
    public void SourceLevelMsid_ProvidesBothSourceAndStreamProjections()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = "ssrc",
            Value = "2122260223 msid:stream-1 track-1",
            RawText = "ssrc:2122260223 msid:stream-1 track-1"
        };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.MediaStreamId!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.SynchronizationSourceAttribute?.Name, Is.EqualTo("msid"));
            Assert.That(decoded.SynchronizationSourceAttribute?.Value, Is.EqualTo("stream-1 track-1"));
            Assert.That(decoded.MediaStreamId?.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.MediaStreamId?.StreamId, Is.EqualTo("stream-1"));
            Assert.That(decoded.MediaStreamId?.HasTrackId, Is.True);
            Assert.That(decoded.MediaStreamId?.TrackId, Is.EqualTo("track-1"));
            Assert.That(decoded.MediaStreamId?.SourceLevel, Is.True);
            Assert.That(decoded.MediaStreamId?.SynchronizationSource, Is.EqualTo(2122260223u));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("ssrc:2122260223 msid:stream-1 track-1"));
        });
    }

    [TestCase(SdpAttributeKind.Property, "", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "42", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "-1 cname:user", "SDP_PARSE_INVALID_NUMBER")]
    [TestCase(SdpAttributeKind.Value, "4294967296 cname:user", "SDP_PARSE_NUMBER_OVERFLOW")]
    [TestCase(SdpAttributeKind.Value, "42 bad/name:value", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "42 cname", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "42 cname:", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "42 msid:stream one extra", "SDP_VALIDATION_ATTRIBUTE")]
    public void InvalidSourceAttribute_FailsWithoutDiscardingOpaqueInput(SdpAttributeKind kind, string value, string code)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = kind, Name = "ssrc", Value = value, RawText = "ssrc:" + value };
        source.Location.LineNumber = 19;

        SdpAttributeDecodeResult result = codec.DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.SynchronizationSourceAttribute, Is.Null);
            Assert.That(result.TypedAttribute, Is.Null);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(code));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(19));
        });
    }

    [Test]
    public void SourceGroup_PreservesOrderDuplicatesExtensionsAndUnsignedBounds()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = "SSRC-GROUP",
            Value = " X-DUP 4294967295 0 4294967295 ",
            RawText = "SSRC-GROUP: X-DUP 4294967295 0 4294967295 "
        };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpSourceGroupAttribute projection = decoded.SourceGroupAttribute ?? new SdpSourceGroupAttribute();
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(projection);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(projection.SourceAttribute, Is.SameAs(source));
            Assert.That(projection.Semantic, Is.EqualTo("X-DUP"));
            Assert.That(projection.SynchronizationSources, Is.EqualTo(new uint[] { uint.MaxValue, 0u, uint.MaxValue }));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.SourceGroupAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("ssrc-group:X-DUP 4294967295 0 4294967295"));
        });
    }

    [TestCase("FID", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase("bad:semantic 1", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase("FID -1", "SDP_PARSE_INVALID_NUMBER")]
    [TestCase("FID 4294967296", "SDP_PARSE_NUMBER_OVERFLOW")]
    public void InvalidSourceGroup_ReturnsStructuredFailure(string value, string code)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "ssrc-group", Value = value };

        SdpAttributeDecodeResult result = codec.DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.SourceGroupAttribute, Is.Null);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(code));
        });
    }

    [TestCase("stream-1", "stream-1", false, "", "msid:stream-1")]
    [TestCase(" stream-1   track-1 ", "stream-1", true, "track-1", "msid:stream-1 track-1")]
    [TestCase("- track-1", "-", true, "track-1", "msid:- track-1")]
    public void MediaLevelMsid_DecodesOptionalTrackAndEncodesCanonically(string value, string streamId, bool hasTrackId, string trackId, string rawText)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "MSID", Value = value, RawText = "MSID:" + value };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpMediaStreamId projection = decoded.MediaStreamId ?? new SdpMediaStreamId();
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(projection);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(projection.SourceAttribute, Is.SameAs(source));
            Assert.That(projection.StreamId, Is.EqualTo(streamId));
            Assert.That(projection.HasTrackId, Is.EqualTo(hasTrackId));
            Assert.That(projection.TrackId, Is.EqualTo(trackId));
            Assert.That(projection.SourceLevel, Is.False);
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo(rawText));
        });
    }

    [Test]
    public void MsidSemantic_DecodesWildcardAndOrderedStreamLists()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute wildcardSource = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "MSID-SEMANTIC", Value = " WMS   * " };
        SdpAttribute listSource = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "msid-semantic", Value = "X-SEM stream-1 stream-2" };

        SdpAttributeDecodeResult wildcard = codec.DecodeAttribute(wildcardSource);
        SdpAttributeDecodeResult list = codec.DecodeAttribute(listSource);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(list.MediaStreamSemanticAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(wildcard.Success, Is.True);
            Assert.That(wildcard.MediaStreamSemanticAttribute?.Semantic, Is.EqualTo("WMS"));
            Assert.That(wildcard.MediaStreamSemanticAttribute?.StreamIds, Is.EqualTo(new[] { "*" }));
            Assert.That(list.Success, Is.True);
            Assert.That(list.MediaStreamSemanticAttribute?.SourceAttribute, Is.SameAs(listSource));
            Assert.That(list.MediaStreamSemanticAttribute?.Semantic, Is.EqualTo("X-SEM"));
            Assert.That(list.MediaStreamSemanticAttribute?.StreamIds, Is.EqualTo(new[] { "stream-1", "stream-2" }));
            Assert.That(list.TypedAttribute, Is.SameAs(list.MediaStreamSemanticAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("msid-semantic:X-SEM stream-1 stream-2"));
        });
    }

    [TestCase("msid", "")]
    [TestCase("msid", "one two three")]
    [TestCase("msid", "bad:value")]
    [TestCase("msid-semantic", "WMS")]
    [TestCase("msid-semantic", "WMS * stream")]
    [TestCase("msid-semantic", "bad:semantic stream")]
    public void InvalidMsidForms_ReturnStructuredFailures(string name, string value)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value };

        SdpAttributeDecodeResult result = codec.DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.TypedAttribute, Is.Null);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationAttribute));
        });
    }

    [Test]
    public void MsidIdentifierLength_AcceptsSixtyFourAndRejectsSixtyFiveCharacters()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        string maximum = new string('a', 64);
        string tooLong = new string('b', 65);
        SdpAttribute validSource = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "msid", Value = maximum + " " + maximum };
        SdpAttribute invalidSource = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "msid", Value = tooLong };

        SdpAttributeDecodeResult valid = codec.DecodeAttribute(validSource);
        SdpAttributeDecodeResult invalid = codec.DecodeAttribute(invalidSource);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True);
            Assert.That(valid.MediaStreamId?.StreamId, Has.Length.EqualTo(64));
            Assert.That(valid.MediaStreamId?.TrackId, Has.Length.EqualTo(64));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.SourceAttribute, Is.SameAs(invalidSource));
            Assert.That(invalid.MediaStreamId, Is.Null);
        });
    }

    [Test]
    public void CallerConstructedSourceProjections_RejectInconsistentState()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpSourceAttribute hiddenValue = new SdpSourceAttribute { SynchronizationSource = 1u, Name = "x-note", Value = "value" };
        SdpSourceGroupAttribute emptyGroup = new SdpSourceGroupAttribute { Semantic = "FID" };
        SdpMediaStreamId hiddenTrack = new SdpMediaStreamId { StreamId = "stream", TrackId = "track" };
        SdpMediaStreamSemanticAttribute mixedWildcard = new SdpMediaStreamSemanticAttribute { Semantic = "WMS" };
        mixedWildcard.StreamIds.Add("*");
        mixedWildcard.StreamIds.Add("stream");

        SdpAttributeEncodeResult sourceResult = codec.EncodeAttribute(hiddenValue);
        SdpAttributeEncodeResult groupResult = codec.EncodeAttribute(emptyGroup);
        SdpAttributeEncodeResult streamResult = codec.EncodeAttribute(hiddenTrack);
        SdpAttributeEncodeResult semanticResult = codec.EncodeAttribute(mixedWildcard);

        Assert.Multiple(() =>
        {
            Assert.That(sourceResult.Success, Is.False);
            Assert.That(sourceResult.Attribute, Is.Null);
            Assert.That(groupResult.Success, Is.False);
            Assert.That(groupResult.Attribute, Is.Null);
            Assert.That(streamResult.Success, Is.False);
            Assert.That(streamResult.Attribute, Is.Null);
            Assert.That(semanticResult.Success, Is.False);
            Assert.That(semanticResult.Attribute, Is.Null);
        });
    }
}
