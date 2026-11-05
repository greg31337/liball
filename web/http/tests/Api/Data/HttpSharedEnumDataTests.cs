namespace http.Tests;

public class HttpSharedEnumDataTests
{
    [Test]
    public void RequestRetainsProtocolAndTargetForm()
    {
        HttpRequest request = new HttpRequest();

        request.ProtocolVersion = HttpProtocolVersion.Http2;
        request.TargetForm = HttpRequestTargetForm.Authority;

        Assert.That(request.ProtocolVersion, Is.EqualTo(HttpProtocolVersion.Http2));
        Assert.That(request.TargetForm, Is.EqualTo(HttpRequestTargetForm.Authority));
    }

    [Test]
    public void ParserTypesRetainMessageKindAndOperationStatus()
    {
        Http1ParseEvent parseEvent = new Http1ParseEvent();
        Http1ParseResult parseResult = new Http1ParseResult();

        parseEvent.MessageKind = HttpMessageKind.InformationalResponse;
        parseResult.Status = HttpOperationStatus.NeedInput;

        Assert.That(parseEvent.MessageKind, Is.EqualTo(HttpMessageKind.InformationalResponse));
        Assert.That(parseResult.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
    }

    [Test]
    public void MetadataTypesRetainTheirClassifications()
    {
        HttpStatusMetadata status = new HttpStatusMetadata();
        StructuredFieldMember member = new StructuredFieldMember();

        status.StatusClass = HttpStatusClass.Extension;
        member.ValueKind = HttpFieldValueKind.DisplayString;

        Assert.That(status.StatusClass, Is.EqualTo(HttpStatusClass.Extension));
        Assert.That(member.ValueKind, Is.EqualTo(HttpFieldValueKind.DisplayString));
    }

    [Test]
    public void DecisionTypesRetainTheirOutcomes()
    {
        HttpNegotiationResult negotiation = new HttpNegotiationResult();
        HttpPreconditionDecision precondition = new HttpPreconditionDecision();
        HttpRangeDecision range = new HttpRangeDecision();
        HttpCacheDecision cache = new HttpCacheDecision();
        HttpContentCodingResult coding = new HttpContentCodingResult();

        negotiation.Outcome = HttpNegotiationOutcome.NoAcceptableRepresentation;
        precondition.Outcome = HttpPreconditionOutcome.PreconditionFailed;
        range.Outcome = HttpRangeOutcome.Unsatisfiable;
        cache.Outcome = HttpCacheOutcome.Validate;
        coding.Outcome = HttpContentCodingOutcome.LimitExceeded;

        Assert.That(negotiation.Outcome, Is.EqualTo(HttpNegotiationOutcome.NoAcceptableRepresentation));
        Assert.That(precondition.Outcome, Is.EqualTo(HttpPreconditionOutcome.PreconditionFailed));
        Assert.That(range.Outcome, Is.EqualTo(HttpRangeOutcome.Unsatisfiable));
        Assert.That(cache.Outcome, Is.EqualTo(HttpCacheOutcome.Validate));
        Assert.That(coding.Outcome, Is.EqualTo(HttpContentCodingOutcome.LimitExceeded));
    }
}
