namespace sdp.Tests.Api;

public class SdpExceptionTests
{
    [Test]
    public void Exceptions_PreserveStableCodeAndInnerExceptionWithFixedSafeMessages()
    {
        string sensitiveValue = "inline:super-secret-key-material";
        InvalidOperationException inner = new InvalidOperationException("Underlying operation failed.");
        SdpParseException parse = new SdpParseException(sensitiveValue, inner);
        SdpValidationException validation = new SdpValidationException(sensitiveValue, inner);
        SdpGenerationException generation = new SdpGenerationException(sensitiveValue, inner);
        SdpNegotiationException negotiation = new SdpNegotiationException(sensitiveValue, inner);

        Assert.Multiple(() =>
        {
            Assert.That(parse.Code, Is.EqualTo(sensitiveValue));
            Assert.That(parse.Message, Is.EqualTo("SDP parsing failed."));
            Assert.That(parse.Message, Does.Not.Contain(sensitiveValue));
            Assert.That(parse.InnerException, Is.SameAs(inner));
            Assert.That(validation.Code, Is.EqualTo(sensitiveValue));
            Assert.That(validation.Message, Is.EqualTo("SDP validation failed."));
            Assert.That(validation.Message, Does.Not.Contain(sensitiveValue));
            Assert.That(validation.InnerException, Is.SameAs(inner));
            Assert.That(generation.Code, Is.EqualTo(sensitiveValue));
            Assert.That(generation.Message, Is.EqualTo("SDP generation failed."));
            Assert.That(generation.Message, Does.Not.Contain(sensitiveValue));
            Assert.That(generation.InnerException, Is.SameAs(inner));
            Assert.That(negotiation.Code, Is.EqualTo(sensitiveValue));
            Assert.That(negotiation.Message, Is.EqualTo("SDP negotiation failed."));
            Assert.That(negotiation.Message, Does.Not.Contain(sensitiveValue));
            Assert.That(negotiation.InnerException, Is.SameAs(inner));
        });
    }

    [Test]
    public void Exceptions_AcceptMissingInnerExceptionWithoutChangingMessages()
    {
        SdpParseException parse = new SdpParseException(SdpDiagnosticCodes.ParseMalformedLine, null);
        SdpValidationException validation = new SdpValidationException(SdpDiagnosticCodes.ValidationMedia, null);
        SdpGenerationException generation = new SdpGenerationException(SdpDiagnosticCodes.WriteInvalidDescription, null);
        SdpNegotiationException negotiation = new SdpNegotiationException(SdpDiagnosticCodes.NegotiationInvalidOffer, null);

        Assert.Multiple(() =>
        {
            Assert.That(parse.InnerException, Is.Null);
            Assert.That(parse.Message, Is.EqualTo("SDP parsing failed."));
            Assert.That(validation.InnerException, Is.Null);
            Assert.That(validation.Message, Is.EqualTo("SDP validation failed."));
            Assert.That(generation.InnerException, Is.Null);
            Assert.That(generation.Message, Is.EqualTo("SDP generation failed."));
            Assert.That(negotiation.InnerException, Is.Null);
            Assert.That(negotiation.Message, Is.EqualTo("SDP negotiation failed."));
        });
    }
}
