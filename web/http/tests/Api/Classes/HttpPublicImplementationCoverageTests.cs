using System.Text;

namespace http.Tests;

public class HttpPublicImplementationCoverageTests
{
    [Test]
    public void HpackPublicOperationsRejectInvalidArgumentsWithoutChangingState()
    {
        HpackDecoder decoder = new HpackDecoder();
        HpackResult decoderBefore = decoder.InspectDecoderTable();
        HpackResult nullDecodeInput = decoder.DecodeHeaderBlock(
            null!, new HpackOptions());
        HpackResult nullDecodeOptions = decoder.DecodeHeaderBlock([], null!);
        HpackOptions negativeDecodeSize = new HpackOptions();
        negativeDecodeSize.MaximumDynamicTableSize = -1;
        HpackResult invalidDecodeSize = decoder.DecodeHeaderBlock(
            [], negativeDecodeSize);
        HpackResult invalidDecoderUpdate = decoder.UpdateDecoderTableLimit(-1);
        HpackResult decoderAfter = decoder.InspectDecoderTable();

        HpackEncoder encoder = new HpackEncoder();
        HpackResult encoderBefore = encoder.InspectEncoderTable();
        HpackResult nullEncodeHeaders = encoder.EncodeHeaderBlock(
            null!, new HpackOptions());
        HpackResult nullEncodeOptions = encoder.EncodeHeaderBlock(
            new HttpHeaders(), null!);
        HpackOptions negativeActiveSize = new HpackOptions();
        negativeActiveSize.DynamicTableSize = -1;
        HpackResult invalidActiveSize = encoder.EncodeHeaderBlock(
            new HttpHeaders(), negativeActiveSize);
        HpackOptions negativeMaximumSize = new HpackOptions();
        negativeMaximumSize.MaximumDynamicTableSize = -1;
        HpackResult invalidMaximumSize = encoder.EncodeHeaderBlock(
            new HttpHeaders(), negativeMaximumSize);
        HpackResult invalidEncoderUpdate = encoder.UpdateEncoderTableLimit(-1);
        HpackResult encoderAfter = encoder.InspectEncoderTable();

        Assert.Multiple(() =>
        {
            AssertHpackRejected(
                nullDecodeInput, "invalid-hpack-decode-arguments");
            AssertHpackRejected(
                nullDecodeOptions, "invalid-hpack-decode-arguments");
            AssertHpackRejected(
                invalidDecodeSize, "invalid-hpack-dynamic-table-size");
            AssertHpackRejected(
                invalidDecoderUpdate, "invalid-hpack-dynamic-table-size");
            Assert.That(decoderAfter.DynamicTableSize,
                Is.EqualTo(decoderBefore.DynamicTableSize));
            Assert.That(decoderAfter.DynamicTableLimit,
                Is.EqualTo(decoderBefore.DynamicTableLimit));

            AssertHpackRejected(
                nullEncodeHeaders, "invalid-hpack-encode-arguments");
            AssertHpackRejected(
                nullEncodeOptions, "invalid-hpack-encode-arguments");
            AssertHpackRejected(
                invalidActiveSize, "invalid-hpack-encode-limits");
            AssertHpackRejected(
                invalidMaximumSize, "invalid-hpack-encode-limits");
            AssertHpackRejected(
                invalidEncoderUpdate, "invalid-hpack-dynamic-table-size");
            Assert.That(encoderAfter.DynamicTableSize,
                Is.EqualTo(encoderBefore.DynamicTableSize));
            Assert.That(encoderAfter.DynamicTableLimit,
                Is.EqualTo(encoderBefore.DynamicTableLimit));
        });
    }

    [Test]
    public void HpackPendingLimitBranchesRetainSynchronizedState()
    {
        HpackEncoder encoder = new HpackEncoder();
        encoder.UpdateEncoderTableLimit(3000);
        encoder.UpdateEncoderTableLimit(3500);
        encoder.UpdateEncoderTableLimit(2000);
        HpackOptions clamped = new HpackOptions();
        clamped.DynamicTableSize = 2500;
        clamped.MaximumDynamicTableSize = 1500;
        HpackResult encoded = encoder.EncodeHeaderBlock(
            Headers(Field("x-limit", "value")), clamped);

        HpackDecoder decoder = new HpackDecoder();
        decoder.UpdateDecoderTableLimit(1500);
        HpackResult malformed = decoder.DecodeHeaderBlock(
            [0x80], new HpackOptions());
        HpackResult decoded = decoder.DecodeHeaderBlock(
            encoded.Bytes, new HpackOptions());

        HpackDecoder limitedDecoder = new HpackDecoder();
        limitedDecoder.UpdateDecoderTableLimit(0);
        HpackOptions oneHeader = new HpackOptions();
        oneHeader.MaximumHeaderCount = 1;
        HpackResult resourceLimited = limitedDecoder.DecodeHeaderBlock(
            [0x20, 0x82, 0x84], oneHeader);
        HpackResult afterResourceLimit = limitedDecoder.DecodeHeaderBlock(
            [0x82], new HpackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(encoded.DynamicTableLimit, Is.EqualTo(1500));
            Assert.That(malformed.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(malformed.IsCompressionError, Is.True);
            Assert.That(decoded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Text(decoded.Headers.Fields.Single().Value),
                Is.EqualTo("value"));
            Assert.That(resourceLimited.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(resourceLimited.IsCompressionError, Is.False);
            Assert.That(resourceLimited.Consumed, Is.EqualTo(3));
            Assert.That(afterResourceLimit.Status,
                Is.EqualTo(HttpOperationStatus.Done));
        });
    }

    [Test]
    public void ContentCodingPublicOperationsCoverUnknownIdentityAndInvalidStacks()
    {
        IHttpContentCoding coding = new HttpContentCoding();
        byte[] content = Encoding.ASCII.GetBytes("unchanged");
        HttpContentCodingResult unknown = coding.EncodeContent(
            content, "example-coding", content.Length);
        HttpContentCodingResult encodedIdentity = coding.EncodeCodingStack(
            content,
            new List<string> { HttpContentCodings.Identity },
            content.Length);
        HttpContentCodingResult decodedIdentity = coding.DecodeCodingStack(
            content,
            new List<string> { HttpContentCodings.Identity },
            content.Length);
        HttpContentCodingException nullCoding =
            Assert.Throws<HttpContentCodingException>(() =>
                coding.EncodeContent(content, null!, content.Length))!;
        HttpContentCodingException nullEncodeStack =
            Assert.Throws<HttpContentCodingException>(() =>
                coding.EncodeCodingStack(content, null!, content.Length))!;
        HttpContentCodingException nullDecodeStack =
            Assert.Throws<HttpContentCodingException>(() =>
                coding.DecodeCodingStack(content, null!, content.Length))!;

        Assert.Multiple(() =>
        {
            Assert.That(unknown.Outcome,
                Is.EqualTo(HttpContentCodingOutcome.Unknown));
            Assert.That(unknown.Content, Is.EqualTo(content));
            Assert.That(unknown.RemainingCodings,
                Is.EqualTo(new[] { "example-coding" }));
            Assert.That(encodedIdentity.Outcome,
                Is.EqualTo(HttpContentCodingOutcome.Identity));
            Assert.That(decodedIdentity.Outcome,
                Is.EqualTo(HttpContentCodingOutcome.Identity));
            Assert.That(nullCoding.DiagnosticCode,
                Is.EqualTo("invalid-content-coding"));
            Assert.That(nullCoding.Coding, Is.Empty);
            Assert.That(nullEncodeStack.DiagnosticCode,
                Is.EqualTo("invalid-content-coding-stack"));
            Assert.That(nullDecodeStack.DiagnosticCode,
                Is.EqualTo("invalid-content-coding-stack"));
        });
    }

    [Test]
    public void FieldCodecPublicOperationsRejectMissingAndInconsistentModels()
    {
        HttpFieldValueCodec codec = new HttpFieldValueCodec();
        HttpFieldException missingLanguages =
            Assert.Throws<HttpFieldException>(() =>
                codec.SerializeLanguageRanges(null!))!;
        HttpFieldException nullLanguage =
            Assert.Throws<HttpFieldException>(() =>
                codec.SerializeLanguageRanges(
                    new List<HttpLanguageRange> { null! }))!;
        HttpLanguageRange missingRange = new HttpLanguageRange();
        missingRange.Range = null!;
        HttpFieldException missingRangeValue =
            Assert.Throws<HttpFieldException>(() =>
                codec.SerializeLanguageRanges(
                    new List<HttpLanguageRange> { missingRange }))!;
        HttpLanguageRange falseWildcard = new HttpLanguageRange();
        falseWildcard.Range = "*";
        falseWildcard.IsWildcard = false;
        HttpFieldException wildcardMismatch =
            Assert.Throws<HttpFieldException>(() =>
                codec.SerializeLanguageRanges(
                    new List<HttpLanguageRange> { falseWildcard }))!;
        HttpParseException missingPriority =
            Assert.Throws<HttpParseException>(() =>
                codec.ParsePriority(null!))!;

        HttpFieldValueCodec negativeLimit = new HttpFieldValueCodec();
        negativeLimit.limits.MaximumFieldValueSize = -1;
        HttpLimitExceededException invalidPriorityLimit =
            Assert.Throws<HttpLimitExceededException>(() =>
                negativeLimit.ParsePriority([]))!;

        HttpFieldValueCodec memberLimit = new HttpFieldValueCodec();
        memberLimit.limits.MaximumListMemberCount = 0;
        HttpWeightedValue weighted = new HttpWeightedValue();
        weighted.Value = "gzip";
        HttpLimitExceededException tooManyMembers =
            Assert.Throws<HttpLimitExceededException>(() =>
                memberLimit.SerializeWeightedValues(
                    new List<HttpWeightedValue> { weighted }))!;

        Assert.Multiple(() =>
        {
            Assert.That(missingLanguages.DiagnosticCode,
                Is.EqualTo("invalid-language-ranges"));
            Assert.That(nullLanguage.DiagnosticCode,
                Is.EqualTo("invalid-language-ranges"));
            Assert.That(missingRangeValue.DiagnosticCode,
                Is.EqualTo("invalid-language-ranges"));
            Assert.That(wildcardMismatch.DiagnosticCode,
                Is.EqualTo("invalid-language-ranges"));
            Assert.That(missingPriority.DiagnosticCode,
                Is.EqualTo("invalid-priority"));
            Assert.That(invalidPriorityLimit.LimitName,
                Is.EqualTo("maximum-field-value-size"));
            Assert.That(tooManyMembers.LimitName,
                Is.EqualTo("maximum-list-member-count"));
        });
    }

    [Test]
    public void FrameCodecPublicOperationsRejectMissingInputsAndInvalidLimits()
    {
        Http2FrameCodec codec = new Http2FrameCodec();
        Http2FrameResult missingInput = codec.ParseHttp2Frame(null!);
        Http2FrameResult missingFrame = codec.SerializeHttp2Frame(null!);

        codec.configuredMaximumFrameSize = -1;
        Http2FrameResult invalidConfigured = codec.ParseHttp2Frame([]);
        codec.configuredMaximumFrameSize = Http2Constants.DefaultMaximumFrameSize;
        codec.peerMaximumFrameSize = -1;
        Http2FrameResult invalidPeer = codec.ParseHttp2Frame([]);

        Assert.Multiple(() =>
        {
            AssertFrameRejected(missingInput, "invalid-http2-frame-input");
            AssertFrameRejected(missingFrame, "invalid-http2-frame");
            AssertFrameRejected(
                invalidConfigured, "invalid-http2-frame-size-limit");
            AssertFrameRejected(invalidPeer, "invalid-http2-frame-size-limit");
        });
    }

    [Test]
    public void EveryFrameKindRejectsANegativeOutboundLimit()
    {
        Http2FrameCodec codec = new Http2FrameCodec();
        codec.peerMaximumFrameSize = -1;
        codec.directionalMaximumFrameSizes = true;

        Assert.Multiple(() =>
        {
            foreach (Http2Frame frame in RepresentativeFrames())
            {
                AssertFrameRejected(
                    codec.SerializeHttp2Frame(frame),
                    "invalid-http2-frame-size-limit");
            }
        });
    }

    [Test]
    public void FrameKindsRejectMissingTypedPayloads()
    {
        Http2Frame data = DataFrame();
        data.Data = null!;
        Http2Frame headers = HeadersFrame();
        headers.HeaderBlockFragment = null!;
        Http2Frame settings = SettingsFrame();
        settings.Settings = null;
        Http2Frame push = PushPromiseFrame();
        push.HeaderBlockFragment = null!;
        Http2Frame ping = PingFrame();
        ping.OpaqueData = null!;
        Http2Frame goAway = GoAwayFrame();
        goAway.DebugData = null!;
        Http2Frame priorityUpdate = PriorityUpdateFrame();
        priorityUpdate.PriorityFieldValue = null!;
        Http2Frame origin = OriginFrame();
        origin.Origins = null!;
        Http2Frame unknown = UnknownFrame();
        unknown.Payload = null!;
        IHttp2FrameCodec codec = new Http2FrameCodec();

        Assert.Multiple(() =>
        {
            AssertFrameRejected(
                codec.SerializeHttp2Frame(data), "invalid-http2-data-frame");
            AssertFrameRejected(
                codec.SerializeHttp2Frame(headers),
                "invalid-http2-header-block-frame");
            AssertFrameRejected(
                codec.SerializeHttp2Frame(settings),
                "invalid-http2-settings-frame");
            AssertFrameRejected(
                codec.SerializeHttp2Frame(push),
                "invalid-http2-push-promise-frame");
            AssertFrameRejected(
                codec.SerializeHttp2Frame(ping), "invalid-http2-ping-data");
            AssertFrameRejected(
                codec.SerializeHttp2Frame(goAway),
                "invalid-http2-goaway-debug-data");
            AssertFrameRejected(
                codec.SerializeHttp2Frame(priorityUpdate),
                "invalid-http2-priority-update-field-value");
            AssertFrameRejected(
                codec.SerializeHttp2Frame(origin),
                "invalid-http2-origin-list");
            AssertFrameRejected(
                codec.SerializeHttp2Frame(unknown),
                "invalid-http2-unknown-frame-payload");
        });
    }

    [Test]
    public void FrameCodecCoversDirectionalLimitsAndIndependentHeaderMetadata()
    {
        Http2FrameCodec directional = new Http2FrameCodec();
        directional.peerMaximumFrameSize = 20000;
        directional.configuredMaximumFrameSize = 10000;
        Http2FrameResult bounded = directional.SerializeHttp2Frame(DataFrame());

        Http2Frame exclusive = HeadersFrame();
        exclusive.ExclusiveDependency = true;
        Http2Frame dependency = HeadersFrame();
        dependency.StreamDependency = 3;
        Http2Frame weight = HeadersFrame();
        weight.Weight = 16;
        Http2Frame nullOrigin = OriginFrame();
        nullOrigin.Origins.Clear();
        nullOrigin.Origins.Add(null!);
        IHttp2FrameCodec codec = new Http2FrameCodec();

        Assert.Multiple(() =>
        {
            Assert.That(bounded.Status, Is.EqualTo(HttpOperationStatus.Done));
            AssertFrameRejected(
                codec.SerializeHttp2Frame(exclusive),
                "invalid-http2-headers-priority");
            AssertFrameRejected(
                codec.SerializeHttp2Frame(dependency),
                "invalid-http2-headers-priority");
            AssertFrameRejected(
                codec.SerializeHttp2Frame(weight),
                "invalid-http2-headers-priority");
            AssertFrameRejected(
                codec.SerializeHttp2Frame(nullOrigin),
                "invalid-http2-origin-value");
        });
    }

    [Test]
    public void DefensiveHelpersReturnStableDiagnosticsForInvalidState()
    {
        Http2FrameResult missingData = Http2FrameCodec.BuildDataFrame(
            null!, Http2Constants.DefaultMaximumFrameSize);
        Http2FrameResult wrongDataKind = Http2FrameCodec.BuildDataFrame(
            HeadersFrame(), Http2Constants.DefaultMaximumFrameSize);
        HttpFieldValueCodec fields = new HttpFieldValueCodec();
        HttpLimitExceededException missingLimitInput =
            Assert.Throws<HttpLimitExceededException>(() =>
                fields.ValidateParseResult(
                    HttpOperationStatus.Rejected,
                    true,
                    0,
                    null!,
                    "invalid-test-value",
                    "maximum-list-member-count",
                    0))!;

        Assert.Multiple(() =>
        {
            AssertFrameRejected(missingData, "invalid-http2-frame");
            Assert.That(wrongDataKind.Status,
                Is.EqualTo(HttpOperationStatus.Unsupported));
            Assert.That(wrongDataKind.ErrorCode,
                Is.EqualTo("http2-frame-serialization-not-implemented"));
            Assert.That(missingLimitInput.LimitName,
                Is.EqualTo("maximum-list-member-count"));
        });
    }

    static List<Http2Frame> RepresentativeFrames()
    {
        return new List<Http2Frame>
        {
            DataFrame(),
            HeadersFrame(),
            PriorityFrame(),
            ResetStreamFrame(),
            SettingsFrame(),
            PushPromiseFrame(),
            PingFrame(),
            GoAwayFrame(),
            WindowUpdateFrame(),
            ContinuationFrame(),
            OriginFrame(),
            PriorityUpdateFrame(),
            UnknownFrame()
        };
    }

    static Http2Frame DataFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Data;
        frame.StreamId = 1;
        frame.Data = [1];
        return frame;
    }

    static Http2Frame HeadersFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Headers;
        frame.StreamId = 1;
        frame.HeaderBlockFragment = [0x82];
        return frame;
    }

    static Http2Frame ContinuationFrame()
    {
        Http2Frame frame = HeadersFrame();
        frame.FrameType = Http2FrameType.Continuation;
        return frame;
    }

    static Http2Frame PriorityFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Priority;
        frame.StreamId = 3;
        frame.StreamDependency = 1;
        frame.Weight = 16;
        return frame;
    }

    static Http2Frame ResetStreamFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.RstStream;
        frame.StreamId = 1;
        frame.HasErrorCode = true;
        frame.ErrorCode = Http2ErrorCode.Cancel;
        frame.ErrorCodeValue = (uint)Http2ErrorCode.Cancel;
        frame.IsKnownErrorCode = true;
        return frame;
    }

    static Http2Frame SettingsFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Settings;
        frame.Settings = new Http2Settings();
        return frame;
    }

    static Http2Frame PushPromiseFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.PushPromise;
        frame.StreamId = 1;
        frame.HasPromisedStreamId = true;
        frame.PromisedStreamId = 2;
        frame.HeaderBlockFragment = [0x82];
        return frame;
    }

    static Http2Frame PingFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Ping;
        frame.OpaqueData = Encoding.ASCII.GetBytes("12345678");
        return frame;
    }

    static Http2Frame GoAwayFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.GoAway;
        frame.HasLastStreamId = true;
        frame.LastStreamId = 0;
        frame.HasErrorCode = true;
        frame.ErrorCode = Http2ErrorCode.NoError;
        frame.IsKnownErrorCode = true;
        return frame;
    }

    static Http2Frame WindowUpdateFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.WindowUpdate;
        frame.HasWindowIncrement = true;
        frame.WindowIncrement = 1;
        return frame;
    }

    static Http2Frame OriginFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Origin;
        frame.Origins.Add("https://example.test");
        return frame;
    }

    static Http2Frame PriorityUpdateFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.PriorityUpdate;
        frame.HasPrioritizedElementId = true;
        frame.PrioritizedElementId = 1;
        frame.PriorityFieldValue = Encoding.ASCII.GetBytes("u=3");
        return frame;
    }

    static Http2Frame UnknownFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Unknown;
        frame.TypeCode = 0xfe;
        frame.Payload = [1];
        return frame;
    }

    static HttpHeaders Headers(params HttpHeaderField[] fields)
    {
        HttpHeaders headers = new HttpHeaders();
        headers.Fields.AddRange(fields);
        return headers;
    }

    static HttpHeaderField Field(string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        return field;
    }

    static void AssertHpackRejected(HpackResult result, string errorCode)
    {
        Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
        Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
    }

    static void AssertFrameRejected(Http2FrameResult result, string errorCode)
    {
        Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
        Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
    }

    static string Text(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }
}
