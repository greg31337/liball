using System.Reflection;

namespace http.Tests;

public class HttpContractDispatchTests
{
    [Test]
    public void HeaderAndFieldCodecContractsDispatchTypedOperations()
    {
        IHttpHeaderService headers = DispatchProxy.Create<IHttpHeaderService, RecordingProxy>();
        RecordingProxy headerProxy = (RecordingProxy)(object)headers;
        IHttpFieldValueCodec fields = DispatchProxy.Create<IHttpFieldValueCodec, RecordingProxy>();
        RecordingProxy fieldProxy = (RecordingProxy)(object)fields;
        HttpHeaders source = new HttpHeaders();
        HttpMediaType mediaType = new HttpMediaType();

        headerProxy.ReturnValue = true;
        bool valid = headers.ValidateFieldValue([0x61, 0x80]);
        fieldProxy.ReturnValue = mediaType;
        HttpMediaType parsed = fields.ParseMediaType([0x74, 0x65, 0x78, 0x74, 0x2f, 0x70, 0x6c, 0x61, 0x69, 0x6e]);

        Assert.Multiple(() =>
        {
            Assert.That(valid, Is.True);
            Assert.That(headerProxy.LastMethod, Is.EqualTo("ValidateFieldValue"));
            Assert.That(headerProxy.LastArguments, Has.Length.EqualTo(1));
            Assert.That(parsed, Is.SameAs(mediaType));
            Assert.That(fieldProxy.LastMethod, Is.EqualTo("ParseMediaType"));
        });

        headerProxy.ReturnValue = new List<HttpHeaderField>();
        Assert.That(headers.FindFields(source, "Example"), Is.Empty);
    }

    [Test]
    public void SemanticContractsDispatchDedicatedResultObjects()
    {
        IStructuredFieldCodec structured = DispatchProxy.Create<IStructuredFieldCodec, RecordingProxy>();
        IHttpSemantics semantics = DispatchProxy.Create<IHttpSemantics, RecordingProxy>();
        IHttpContentNegotiator negotiation = DispatchProxy.Create<IHttpContentNegotiator, RecordingProxy>();
        IHttpPreconditionEvaluator preconditions = DispatchProxy.Create<IHttpPreconditionEvaluator, RecordingProxy>();
        IHttpRangeService ranges = DispatchProxy.Create<IHttpRangeService, RecordingProxy>();
        RecordingProxy structuredProxy = (RecordingProxy)(object)structured;
        RecordingProxy semanticsProxy = (RecordingProxy)(object)semantics;
        RecordingProxy negotiationProxy = (RecordingProxy)(object)negotiation;
        RecordingProxy preconditionProxy = (RecordingProxy)(object)preconditions;
        RecordingProxy rangeProxy = (RecordingProxy)(object)ranges;
        StructuredFieldValue structuredValue = new StructuredFieldValue();
        HttpMethodMetadata method = new HttpMethodMetadata();
        HttpNegotiationResult selection = new HttpNegotiationResult();
        HttpPreconditionDecision precondition = new HttpPreconditionDecision();
        HttpRangeDecision range = new HttpRangeDecision();

        structuredProxy.ReturnValue = structuredValue;
        semanticsProxy.ReturnValue = method;
        negotiationProxy.ReturnValue = selection;
        preconditionProxy.ReturnValue = precondition;
        rangeProxy.ReturnValue = range;

        Assert.Multiple(() =>
        {
            Assert.That(structured.ParseStructuredDictionary([]), Is.SameAs(structuredValue));
            Assert.That(semantics.DescribeMethod("GET"), Is.SameAs(method));
            Assert.That(negotiation.SelectRepresentation(
                new HttpRequest(), new List<HttpNegotiationCandidate>()), Is.SameAs(selection));
            Assert.That(preconditions.EvaluateIfRange(
                new HttpRequest(), new HttpPreconditionContext()), Is.SameAs(precondition));
            Assert.That(ranges.NormalizeByteRanges(new HttpRangeRequest(), 100), Is.SameAs(range));
        });
    }

    [Test]
    public void CacheCodingAndParserContractsDispatchOperationResults()
    {
        IHttpCachePolicy cache = DispatchProxy.Create<IHttpCachePolicy, RecordingProxy>();
        IHttpContentCoding coding = DispatchProxy.Create<IHttpContentCoding, RecordingProxy>();
        IHttp1Parser parser = DispatchProxy.Create<IHttp1Parser, RecordingProxy>();
        RecordingProxy cacheProxy = (RecordingProxy)(object)cache;
        RecordingProxy codingProxy = (RecordingProxy)(object)coding;
        RecordingProxy parserProxy = (RecordingProxy)(object)parser;
        HttpCacheDecision cacheResult = new HttpCacheDecision();
        HttpContentCodingResult codingResult = new HttpContentCodingResult();
        Http1ParseResult parseResult = new Http1ParseResult();

        cacheProxy.ReturnValue = cacheResult;
        codingProxy.ReturnValue = codingResult;
        parserProxy.ReturnValue = parseResult;

        Assert.Multiple(() =>
        {
            Assert.That(cache.EvaluateReuse(
                new HttpCacheRequestContext(), new HttpCacheResponseContext()), Is.SameAs(cacheResult));
            Assert.That(coding.DecodeCodingStack(
                [], new List<string> { "gzip", "br" }, 4096), Is.SameAs(codingResult));
            Assert.That(parser.CompleteHttp1Input(), Is.SameAs(parseResult));
        });
    }

    [Test]
    public void CompressionAndHttp2ContractsDispatchStatefulResults()
    {
        IHpackEncoder encoder = DispatchProxy.Create<IHpackEncoder, RecordingProxy>();
        IHpackDecoder decoder = DispatchProxy.Create<IHpackDecoder, RecordingProxy>();
        IHttp2MessageCodec messages = DispatchProxy.Create<IHttp2MessageCodec, RecordingProxy>();
        IHttp2Connection connection = DispatchProxy.Create<IHttp2Connection, RecordingProxy>();
        RecordingProxy encoderProxy = (RecordingProxy)(object)encoder;
        RecordingProxy decoderProxy = (RecordingProxy)(object)decoder;
        RecordingProxy messageProxy = (RecordingProxy)(object)messages;
        RecordingProxy connectionProxy = (RecordingProxy)(object)connection;
        HpackResult hpackResult = new HpackResult();
        Http2MessageResult messageResult = new Http2MessageResult();
        Http2ConnectionResult connectionResult = new Http2ConnectionResult();

        encoderProxy.ReturnValue = hpackResult;
        decoderProxy.ReturnValue = hpackResult;
        messageProxy.ReturnValue = messageResult;
        connectionProxy.ReturnValue = connectionResult;

        Assert.Multiple(() =>
        {
            Assert.That(encoder.UpdateEncoderTableLimit(1024), Is.SameAs(hpackResult));
            Assert.That(decoder.DecodeHeaderBlock([0x82], new HpackOptions()), Is.SameAs(hpackResult));
            Assert.That(messages.DecodeHttp2Trailers(1, new HttpHeaders(), true), Is.SameAs(messageResult));
            Assert.That(connection.ReleaseHttp2Capacity(1, 1024), Is.SameAs(connectionResult));
        });
    }

    public class RecordingProxy : DispatchProxy
    {
        public object? ReturnValue;
        public string? LastMethod;
        public object?[]? LastArguments;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            LastMethod = targetMethod?.Name;
            LastArguments = args;
            return ReturnValue;
        }
    }
}
