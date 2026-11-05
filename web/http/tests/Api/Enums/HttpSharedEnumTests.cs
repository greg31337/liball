namespace http.Tests;

public class HttpSharedEnumTests
{
    [Test]
    public void EveryPublicEnumHasAStableValueSuite()
    {
        Type[] expected =
        [
            typeof(HpackIndexingMode),
            typeof(Http1BodyFraming),
            typeof(Http1ConnectionMode),
            typeof(Http1ParseEventKind),
            typeof(Http2ActionKind),
            typeof(Http2EndpointRole),
            typeof(Http2ErrorCode),
            typeof(Http2ErrorScope),
            typeof(Http2EventKind),
            typeof(Http2FrameType),
            typeof(Http2OriginOutcome),
            typeof(Http2StreamState),
            typeof(HttpCacheOutcome),
            typeof(HttpContentCodingOutcome),
            typeof(HttpFieldValueKind),
            typeof(HttpMessageKind),
            typeof(HttpNegotiationOutcome),
            typeof(HttpOperationStatus),
            typeof(HttpPreconditionOutcome),
            typeof(HttpProtocolVersion),
            typeof(HttpRangeOutcome),
            typeof(HttpRequestTargetForm),
            typeof(HttpStatusClass)
        ];
        Type[] actual = typeof(HttpProtocolVersion).Assembly.GetTypes()
            .Where(type => type.IsPublic && type.IsEnum)
            .ToArray();

        Assert.That(actual, Is.EquivalentTo(expected));
    }

    [Test]
    public void ProtocolVersionsHaveStableValues()
    {
        Assert.That((int)HttpProtocolVersion.Http10, Is.EqualTo(0));
        Assert.That((int)HttpProtocolVersion.Http11, Is.EqualTo(1));
        Assert.That((int)HttpProtocolVersion.Http2, Is.EqualTo(2));
    }

    [Test]
    public void MessageKindsHaveStableValues()
    {
        Assert.That((int)HttpMessageKind.Request, Is.EqualTo(0));
        Assert.That((int)HttpMessageKind.Response, Is.EqualTo(1));
        Assert.That((int)HttpMessageKind.InformationalResponse, Is.EqualTo(2));
        Assert.That((int)HttpMessageKind.Trailers, Is.EqualTo(3));
    }

    [Test]
    public void OperationStatusesHaveStableValues()
    {
        Assert.That((int)HttpOperationStatus.Done, Is.EqualTo(0));
        Assert.That((int)HttpOperationStatus.NeedInput, Is.EqualTo(1));
        Assert.That((int)HttpOperationStatus.NeedOutput, Is.EqualTo(2));
        Assert.That((int)HttpOperationStatus.Rejected, Is.EqualTo(3));
        Assert.That((int)HttpOperationStatus.Unsupported, Is.EqualTo(4));
        Assert.That((int)HttpOperationStatus.Terminal, Is.EqualTo(5));
    }

    [Test]
    public void RequestTargetFormsHaveStableValues()
    {
        Assert.That((int)HttpRequestTargetForm.Origin, Is.EqualTo(0));
        Assert.That((int)HttpRequestTargetForm.Absolute, Is.EqualTo(1));
        Assert.That((int)HttpRequestTargetForm.Authority, Is.EqualTo(2));
        Assert.That((int)HttpRequestTargetForm.Asterisk, Is.EqualTo(3));
    }

    [Test]
    public void StatusClassesHaveStableValues()
    {
        Assert.That((int)HttpStatusClass.Invalid, Is.EqualTo(0));
        Assert.That((int)HttpStatusClass.Informational, Is.EqualTo(1));
        Assert.That((int)HttpStatusClass.Successful, Is.EqualTo(2));
        Assert.That((int)HttpStatusClass.Redirection, Is.EqualTo(3));
        Assert.That((int)HttpStatusClass.ClientError, Is.EqualTo(4));
        Assert.That((int)HttpStatusClass.ServerError, Is.EqualTo(5));
        Assert.That((int)HttpStatusClass.Extension, Is.EqualTo(6));
    }

    [Test]
    public void FieldValueKindsHaveStableValues()
    {
        Assert.That((int)HttpFieldValueKind.Token, Is.EqualTo(0));
        Assert.That((int)HttpFieldValueKind.String, Is.EqualTo(1));
        Assert.That((int)HttpFieldValueKind.ByteSequence, Is.EqualTo(2));
        Assert.That((int)HttpFieldValueKind.Integer, Is.EqualTo(3));
        Assert.That((int)HttpFieldValueKind.Decimal, Is.EqualTo(4));
        Assert.That((int)HttpFieldValueKind.Boolean, Is.EqualTo(5));
        Assert.That((int)HttpFieldValueKind.Date, Is.EqualTo(6));
        Assert.That((int)HttpFieldValueKind.DisplayString, Is.EqualTo(7));
    }

    [Test]
    public void NegotiationOutcomesHaveStableValues()
    {
        Assert.That((int)HttpNegotiationOutcome.Selected, Is.EqualTo(0));
        Assert.That((int)HttpNegotiationOutcome.NoAcceptableRepresentation, Is.EqualTo(1));
        Assert.That((int)HttpNegotiationOutcome.MalformedPreference, Is.EqualTo(2));
        Assert.That((int)HttpNegotiationOutcome.AbsentDimension, Is.EqualTo(3));
    }

    [Test]
    public void PreconditionOutcomesHaveStableValues()
    {
        Assert.That((int)HttpPreconditionOutcome.Proceed, Is.EqualTo(0));
        Assert.That((int)HttpPreconditionOutcome.NotModified, Is.EqualTo(1));
        Assert.That((int)HttpPreconditionOutcome.PreconditionFailed, Is.EqualTo(2));
        Assert.That((int)HttpPreconditionOutcome.IgnoreRange, Is.EqualTo(3));
        Assert.That((int)HttpPreconditionOutcome.Rejected, Is.EqualTo(4));
    }

    [Test]
    public void RangeOutcomesHaveStableValues()
    {
        Assert.That((int)HttpRangeOutcome.Ignored, Is.EqualTo(0));
        Assert.That((int)HttpRangeOutcome.Satisfiable, Is.EqualTo(1));
        Assert.That((int)HttpRangeOutcome.Unsatisfiable, Is.EqualTo(2));
        Assert.That((int)HttpRangeOutcome.Malformed, Is.EqualTo(3));
        Assert.That((int)HttpRangeOutcome.UnsupportedUnit, Is.EqualTo(4));
    }

    [Test]
    public void CacheOutcomesHaveStableValues()
    {
        Assert.That((int)HttpCacheOutcome.Store, Is.EqualTo(0));
        Assert.That((int)HttpCacheOutcome.DoNotStore, Is.EqualTo(1));
        Assert.That((int)HttpCacheOutcome.ServeFresh, Is.EqualTo(2));
        Assert.That((int)HttpCacheOutcome.ServeStale, Is.EqualTo(3));
        Assert.That((int)HttpCacheOutcome.Validate, Is.EqualTo(4));
        Assert.That((int)HttpCacheOutcome.OnlyIfCachedMiss, Is.EqualTo(5));
        Assert.That((int)HttpCacheOutcome.Invalidate, Is.EqualTo(6));
        Assert.That((int)HttpCacheOutcome.Unusable, Is.EqualTo(7));
    }

    [Test]
    public void ContentCodingOutcomesHaveStableValues()
    {
        Assert.That((int)HttpContentCodingOutcome.Encoded, Is.EqualTo(0));
        Assert.That((int)HttpContentCodingOutcome.Decoded, Is.EqualTo(1));
        Assert.That((int)HttpContentCodingOutcome.Identity, Is.EqualTo(2));
        Assert.That((int)HttpContentCodingOutcome.Unknown, Is.EqualTo(3));
        Assert.That((int)HttpContentCodingOutcome.Malformed, Is.EqualTo(4));
        Assert.That((int)HttpContentCodingOutcome.LimitExceeded, Is.EqualTo(5));
    }
}
