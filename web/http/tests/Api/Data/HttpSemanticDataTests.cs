using System.Text;

namespace http.Tests;

public class HttpSemanticDataTests
{
    [Test]
    public void StructuredFieldDataRetainsItemsInnerListsParametersAndDictionaryState()
    {
        StructuredFieldMember bareItem = new StructuredFieldMember();
        StructuredFieldParameter parameter = new StructuredFieldParameter();
        StructuredFieldMember innerItem = new StructuredFieldMember();
        StructuredFieldMember innerList = new StructuredFieldMember();
        StructuredFieldValue dictionary = new StructuredFieldValue();

        bareItem.ValueKind = HttpFieldValueKind.Decimal;
        bareItem.DecimalThousandths = -1250;
        bareItem.DictionaryKey = "ratio";
        bareItem.HasExplicitDictionaryValue = true;
        parameter.Name = "required";
        parameter.BareItem = new StructuredFieldMember();
        parameter.BareItem.ValueKind = HttpFieldValueKind.Boolean;
        parameter.BareItem.BooleanValue = true;
        bareItem.Parameters.Add(parameter);
        innerItem.ValueKind = HttpFieldValueKind.ByteSequence;
        innerItem.ByteSequenceValue = [0x00, 0x80, 0xff];
        innerList.IsInnerList = true;
        innerList.InnerList.Add(innerItem);
        dictionary.IsDictionary = true;
        dictionary.Members.Add(bareItem);
        dictionary.Members.Add(innerList);

        Assert.Multiple(() =>
        {
            Assert.That(dictionary.IsDictionary, Is.True);
            Assert.That(dictionary.Members, Is.EqualTo(new[] { bareItem, innerList }));
            Assert.That(bareItem.DecimalThousandths, Is.EqualTo(-1250));
            Assert.That(bareItem.DictionaryKey, Is.EqualTo("ratio"));
            Assert.That(bareItem.HasExplicitDictionaryValue, Is.True);
            Assert.That(bareItem.Parameters.Single(), Is.SameAs(parameter));
            Assert.That(parameter.BareItem!.BooleanValue, Is.True);
            Assert.That(innerList.IsInnerList, Is.True);
            Assert.That(innerList.InnerList.Single().ByteSequenceValue,
                Is.EqualTo(new byte[] { 0x00, 0x80, 0xff }));
        });
    }

    [Test]
    public void NegotiationAndPreconditionDataRetainScoresAndPresence()
    {
        HttpRepresentation representation = new HttpRepresentation();
        HttpNegotiationCandidate candidate = new HttpNegotiationCandidate();
        HttpNegotiationResult negotiation = new HttpNegotiationResult();
        HttpPreconditionContext context = new HttpPreconditionContext();
        HttpPreconditionDecision decision = new HttpPreconditionDecision();
        DateTimeOffset modified = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

        candidate.Representation = representation;
        candidate.ServerPreference = 20;
        candidate.SourceOrder = 2;
        negotiation.Outcome = HttpNegotiationOutcome.Selected;
        negotiation.Representation = representation;
        negotiation.Candidate = candidate;
        negotiation.HasMediaMatch = true;
        negotiation.MediaQuality = 900;
        negotiation.MediaSpecificity = 3;
        negotiation.HasEncodingMatch = true;
        negotiation.EncodingQuality = 500;
        negotiation.HasOverallQuality = true;
        negotiation.OverallQuality = 450000;
        negotiation.Reason = "highest combined quality";
        context.Method = "GET";
        context.IsCacheRecipient = true;
        context.RepresentationExists = true;
        context.EntityTag = new HttpEntityTag();
        context.HasLastModified = true;
        context.LastModified = modified;
        decision.Outcome = HttpPreconditionOutcome.NotModified;
        decision.Context = context;
        decision.DecidingCondition = "If-None-Match";
        decision.HasStatusCode = true;
        decision.StatusCode = 304;

        Assert.Multiple(() =>
        {
            Assert.That(negotiation.Candidate, Is.SameAs(candidate));
            Assert.That(negotiation.Representation, Is.SameAs(representation));
            Assert.That(candidate.ServerPreference, Is.EqualTo(20));
            Assert.That(negotiation.MediaQuality, Is.EqualTo(900));
            Assert.That(negotiation.EncodingQuality, Is.EqualTo(500));
            Assert.That(negotiation.OverallQuality, Is.EqualTo(450000));
            Assert.That(decision.Context, Is.SameAs(context));
            Assert.That(context.LastModified, Is.EqualTo(modified));
            Assert.That(decision.DecidingCondition, Is.EqualTo("If-None-Match"));
            Assert.That(decision.HasStatusCode, Is.True);
            Assert.That(decision.StatusCode, Is.EqualTo(304));
        });
    }

    [Test]
    public void RangeDataRetainsPolicyNormalizedRangesAndMultipartOutput()
    {
        HttpRangeRequest request = new HttpRangeRequest();
        HttpByteRange byteRange = new HttpByteRange();
        HttpContentRange contentRange = new HttpContentRange();
        HttpMultipartRange multipart = new HttpMultipartRange();
        HttpRangeDecision decision = new HttpRangeDecision();

        request.Unit = "bytes";
        request.CoalesceOverlaps = false;
        request.CoalesceAdjacent = false;
        request.Limits.MaximumRangeCount = 4;
        byteRange.HasStart = true;
        byteRange.Start = 0;
        byteRange.HasEnd = true;
        byteRange.End = 9;
        request.Ranges.Add(byteRange);
        contentRange.Unit = "bytes";
        contentRange.HasStart = true;
        contentRange.Start = 0;
        contentRange.HasEnd = true;
        contentRange.End = 9;
        contentRange.HasCompleteLength = true;
        contentRange.CompleteLength = 100;
        multipart.Boundary = "boundary-1";
        multipart.ContentType = "multipart/byteranges; boundary=boundary-1";
        multipart.EncodedLength = 3;
        multipart.EncodedBody = [1, 2, 3];
        multipart.Parts.Add(new HttpRepresentation());
        decision.Outcome = HttpRangeOutcome.Satisfiable;
        decision.Request = request;
        decision.Ranges.Add(byteRange);
        decision.ContentRanges.Add(contentRange);
        decision.Multipart = multipart;

        Assert.Multiple(() =>
        {
            Assert.That(decision.Request, Is.SameAs(request));
            Assert.That(request.Ranges.Single(), Is.SameAs(byteRange));
            Assert.That(request.Limits.MaximumRangeCount, Is.EqualTo(4));
            Assert.That(request.CoalesceOverlaps, Is.False);
            Assert.That(decision.ContentRanges.Single(), Is.SameAs(contentRange));
            Assert.That(decision.Multipart, Is.SameAs(multipart));
            Assert.That(multipart.Parts, Has.Count.EqualTo(1));
            Assert.That(multipart.EncodedLength, Is.EqualTo(3));
            Assert.That(multipart.EncodedBody, Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void CacheDataRetainsDirectivesContextsCalculationsAndValidationResults()
    {
        HttpCacheControl requestControl = new HttpCacheControl();
        HttpCacheControl responseControl = new HttpCacheControl();
        HttpCacheRequestContext requestContext = new HttpCacheRequestContext();
        HttpCacheResponseContext responseContext = new HttpCacheResponseContext();
        HttpCacheValidationResult validation = new HttpCacheValidationResult();
        HttpCacheDecision decision = new HttpCacheDecision();
        HttpParameter extension = new HttpParameter();
        HttpResponse response = new HttpResponse();
        DateTimeOffset now = new DateTimeOffset(2026, 2, 3, 4, 5, 6, TimeSpan.Zero);

        requestControl.NoCache = true;
        requestControl.HasMaxAge = true;
        requestControl.MaxAge = 60;
        requestControl.HasMaxStale = true;
        requestControl.HasMaxStaleValue = true;
        requestControl.MaxStale = 30;
        extension.Name = "example";
        extension.HasValue = true;
        extension.Value = Encoding.ASCII.GetBytes("value");
        requestControl.Extensions.Add(extension);
        responseControl.Private = true;
        responseControl.PrivateFields.Add("Authorization");
        responseControl.HasSharedMaxAge = true;
        responseControl.SharedMaxAge = 120;
        responseControl.HasStaleWhileRevalidate = true;
        responseControl.StaleWhileRevalidate = 15;
        requestContext.Request = new HttpRequest();
        requestContext.CacheControl = requestControl;
        requestContext.RequestTime = now;
        requestContext.IsShared = true;
        requestContext.CollapsedRequestCount = 3;
        responseContext.Response = response;
        responseContext.MatchingResponseContexts.Add(new HttpCacheResponseContext());
        responseContext.CacheControl = responseControl;
        responseContext.RequestTime = now.AddSeconds(-2);
        responseContext.ResponseTime = now.AddSeconds(-1);
        responseContext.StoredTime = now;
        responseContext.CurrentTime = now.AddSeconds(10);
        responseContext.HasAge = true;
        responseContext.Age = 5;
        responseContext.ValidationResult = validation;
        validation.Response = response;
        validation.AffectedResponses.Add(response);
        validation.WasMerged = true;
        decision.Outcome = HttpCacheOutcome.ServeFresh;
        decision.RequestContext = requestContext;
        decision.ResponseContext = responseContext;
        decision.Response = response;
        decision.HasCurrentAge = true;
        decision.CurrentAge = 15;
        decision.HasFreshnessLifetime = true;
        decision.FreshnessLifetime = 120;
        decision.PrimaryKey = "GET https://example.test/";
        decision.ValidationResult = validation;
        decision.PreconditionDecision = new HttpPreconditionDecision();
        decision.RangeDecision = new HttpRangeDecision();
        decision.CollapsedRequestCount = 3;
        decision.WasCollapsed = true;
        decision.WasRevalidated = true;
        decision.InvalidationTargets.Add("https://example.test/item");

        Assert.Multiple(() =>
        {
            Assert.That(requestControl.NoCache, Is.True);
            Assert.That(requestControl.MaxAge, Is.EqualTo(60));
            Assert.That(requestControl.HasMaxStaleValue, Is.True);
            Assert.That(requestControl.Extensions.Single(), Is.SameAs(extension));
            Assert.That(responseControl.PrivateFields, Is.EqualTo(new[] { "Authorization" }));
            Assert.That(responseControl.SharedMaxAge, Is.EqualTo(120));
            Assert.That(requestContext.RequestTime, Is.EqualTo(now));
            Assert.That(requestContext.CollapsedRequestCount, Is.EqualTo(3));
            Assert.That(responseContext.CurrentTime, Is.EqualTo(now.AddSeconds(10)));
            Assert.That(responseContext.MatchingResponseContexts, Has.Count.EqualTo(1));
            Assert.That(decision.RequestContext, Is.SameAs(requestContext));
            Assert.That(decision.ResponseContext, Is.SameAs(responseContext));
            Assert.That(decision.CurrentAge, Is.EqualTo(15));
            Assert.That(decision.FreshnessLifetime, Is.EqualTo(120));
            Assert.That(decision.ValidationResult, Is.SameAs(validation));
            Assert.That(responseContext.ValidationResult, Is.SameAs(validation));
            Assert.That(decision.PreconditionDecision, Is.Not.Null);
            Assert.That(decision.RangeDecision, Is.Not.Null);
            Assert.That(decision.CollapsedRequestCount, Is.EqualTo(3));
            Assert.That(decision.WasCollapsed, Is.True);
            Assert.That(decision.WasRevalidated, Is.True);
            Assert.That(decision.InvalidationTargets,
                Is.EqualTo(new[] { "https://example.test/item" }));
            Assert.That(validation.AffectedResponses.Single(), Is.SameAs(response));
        });
    }

    [Test]
    public void ContentCodingResultRetainsTransformedContentAndCodingProgress()
    {
        HttpContentCodingResult result = new HttpContentCodingResult();

        result.Outcome = HttpContentCodingOutcome.Unknown;
        result.Content = [1, 2, 3];
        result.AppliedCodings.Add("gzip");
        result.RemainingCodings.Add("example");
        result.Reason = "unsupported coding";

        Assert.Multiple(() =>
        {
            Assert.That(result.Content, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(result.AppliedCodings, Is.EqualTo(new[] { "gzip" }));
            Assert.That(result.RemainingCodings, Is.EqualTo(new[] { "example" }));
            Assert.That(result.Reason, Is.EqualTo("unsupported coding"));
        });
    }

    [Test]
    public void SharedDefaultsAreBoundedAndPresenceFlagsStartAbsent()
    {
        HttpLimits limits = new HttpLimits();
        HttpCacheControl cacheControl = new HttpCacheControl();
        HttpCacheRequestContext cacheRequest = new HttpCacheRequestContext();
        HttpNegotiationResult negotiation = new HttpNegotiationResult();
        HttpPreconditionContext precondition = new HttpPreconditionContext();
        HttpRangeRequest rangeRequest = new HttpRangeRequest();

        Assert.Multiple(() =>
        {
            Assert.That(limits.MaximumStartLineSize, Is.EqualTo(8192));
            Assert.That(limits.MaximumFieldLineSize, Is.EqualTo(8192));
            Assert.That(limits.MaximumFieldSectionSize, Is.EqualTo(65536));
            Assert.That(limits.MaximumFieldCount, Is.EqualTo(100));
            Assert.That(limits.MaximumFieldValueSize, Is.EqualTo(8192));
            Assert.That(limits.MaximumTrailerSectionSize, Is.EqualTo(16384));
            Assert.That(limits.MaximumTrailerCount, Is.EqualTo(100));
            Assert.That(limits.MaximumContentSize, Is.EqualTo(16777216));
            Assert.That(limits.MaximumChunkSize, Is.EqualTo(8388608));
            Assert.That(limits.MaximumCompressedBlockSize, Is.EqualTo(65536));
            Assert.That(limits.MaximumHeaderListSize, Is.EqualTo(65536));
            Assert.That(limits.MaximumStringSize, Is.EqualTo(16384));
            Assert.That(limits.MaximumFrameSize, Is.EqualTo(Http2Constants.DefaultMaximumFrameSize));
            Assert.That(limits.MaximumRangeCount, Is.EqualTo(16));
            Assert.That(limits.MaximumParameterCount, Is.EqualTo(64));
            Assert.That(limits.MaximumListMemberCount, Is.EqualTo(256));
            Assert.That(limits.MaximumBufferedSize, Is.EqualTo(65536));
            Assert.That(cacheControl.HasMaxAge, Is.False);
            Assert.That(cacheControl.HasMaxStale, Is.False);
            Assert.That(cacheControl.HasStaleIfError, Is.False);
            Assert.That(cacheRequest.IsConnected, Is.True);
            Assert.That(cacheRequest.AllowHeuristicFreshness, Is.True);
            Assert.That(cacheRequest.HeuristicFraction, Is.EqualTo(100));
            Assert.That(negotiation.HasOverallQuality, Is.False);
            Assert.That(precondition.HasLastModified, Is.False);
            Assert.That(precondition.HasDate, Is.False);
            Assert.That(rangeRequest.CoalesceOverlaps, Is.True);
            Assert.That(rangeRequest.CoalesceAdjacent, Is.True);
        });
    }
}
