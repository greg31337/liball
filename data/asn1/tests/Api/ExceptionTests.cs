using asn1;

namespace asn1.Tests;

public class ExceptionTests
{
    [Test]
    public void EveryExceptionPreservesTheSharedErrorPayload()
    {
        AsnDiagnostic diagnostic = new AsnDiagnostic
        {
            Severity = AsnDiagnosticSeverity.Error,
            Code = "ASN0001",
            Message = "Primary diagnostic."
        };
        AsnDiagnostic[] diagnostics = [diagnostic];

        AsnCompilationException compilation = new AsnCompilationException("Compilation failed.", "ASNC001", diagnostics);
        AsnParseException parse = new AsnParseException("Parsing failed.", "ASNP001", diagnostics);
        AsnSchemaException schema = new AsnSchemaException("Schema failed.", "ASNS001", diagnostics);
        AsnConstraintException constraint = new AsnConstraintException("Constraint failed.", "ASNK001", diagnostics);
        AsnEncodingException encoding = new AsnEncodingException("Encoding failed.", "ASNE001", diagnostics);
        AsnDecodingException decoding = new AsnDecodingException("Decoding failed.", "ASND001", diagnostics);
        AsnCanonicalityException canonicality = new AsnCanonicalityException("Canonicality failed.", "ASNCAN001", diagnostics);
        AsnResourceLimitException resource = new AsnResourceLimitException("Limit exceeded.", "ASNR001", diagnostics);
        AsnEcnException ecn = new AsnEcnException("ECN failed.", "ASNECN001", diagnostics);
        AsnXsdMappingException xsd = new AsnXsdMappingException("XSD mapping failed.", "ASNXSD001", diagnostics);
        AsnCodeGenerationException generation = new AsnCodeGenerationException("Generation failed.", "ASNGEN001", diagnostics);

        AssertPayload(compilation.Message, compilation.Code, compilation.Diagnostics, "Compilation failed.", "ASNC001", diagnostics);
        AssertPayload(parse.Message, parse.Code, parse.Diagnostics, "Parsing failed.", "ASNP001", diagnostics);
        AssertPayload(schema.Message, schema.Code, schema.Diagnostics, "Schema failed.", "ASNS001", diagnostics);
        AssertPayload(constraint.Message, constraint.Code, constraint.Diagnostics, "Constraint failed.", "ASNK001", diagnostics);
        AssertPayload(encoding.Message, encoding.Code, encoding.Diagnostics, "Encoding failed.", "ASNE001", diagnostics);
        AssertPayload(decoding.Message, decoding.Code, decoding.Diagnostics, "Decoding failed.", "ASND001", diagnostics);
        AssertPayload(canonicality.Message, canonicality.Code, canonicality.Diagnostics, "Canonicality failed.", "ASNCAN001", diagnostics);
        AssertPayload(resource.Message, resource.Code, resource.Diagnostics, "Limit exceeded.", "ASNR001", diagnostics);
        AssertPayload(ecn.Message, ecn.Code, ecn.Diagnostics, "ECN failed.", "ASNECN001", diagnostics);
        AssertPayload(xsd.Message, xsd.Code, xsd.Diagnostics, "XSD mapping failed.", "ASNXSD001", diagnostics);
        AssertPayload(generation.Message, generation.Code, generation.Diagnostics, "Generation failed.", "ASNGEN001", diagnostics);
    }

    [Test]
    public void EveryExceptionNormalizesNullPayloadParts()
    {
        AsnCompilationException compilation = new AsnCompilationException(null, null, null);
        AsnParseException parse = new AsnParseException(null, null, null);
        AsnSchemaException schema = new AsnSchemaException(null, null, null);
        AsnConstraintException constraint = new AsnConstraintException(null, null, null);
        AsnEncodingException encoding = new AsnEncodingException(null, null, null);
        AsnDecodingException decoding = new AsnDecodingException(null, null, null);
        AsnCanonicalityException canonicality = new AsnCanonicalityException(null, null, null);
        AsnResourceLimitException resource = new AsnResourceLimitException(null, null, null);
        AsnEcnException ecn = new AsnEcnException(null, null, null);
        AsnXsdMappingException xsd = new AsnXsdMappingException(null, null, null);
        AsnCodeGenerationException generation = new AsnCodeGenerationException(null, null, null);

        AssertEmptyPayload(compilation.Message, compilation.Code, compilation.Diagnostics);
        AssertEmptyPayload(parse.Message, parse.Code, parse.Diagnostics);
        AssertEmptyPayload(schema.Message, schema.Code, schema.Diagnostics);
        AssertEmptyPayload(constraint.Message, constraint.Code, constraint.Diagnostics);
        AssertEmptyPayload(encoding.Message, encoding.Code, encoding.Diagnostics);
        AssertEmptyPayload(decoding.Message, decoding.Code, decoding.Diagnostics);
        AssertEmptyPayload(canonicality.Message, canonicality.Code, canonicality.Diagnostics);
        AssertEmptyPayload(resource.Message, resource.Code, resource.Diagnostics);
        AssertEmptyPayload(ecn.Message, ecn.Code, ecn.Diagnostics);
        AssertEmptyPayload(xsd.Message, xsd.Code, xsd.Diagnostics);
        AssertEmptyPayload(generation.Message, generation.Code, generation.Diagnostics);
    }

    [Test]
    public void ExceptionsUseNormalThrowingBehavior()
    {
        AsnEncodingException exception = new AsnEncodingException("Encoding failed.", "ASNE001", []);

        Assert.That(() => throw exception, Throws.TypeOf<AsnEncodingException>());
    }

    private static void AssertPayload(
        string actualMessage,
        string actualCode,
        AsnDiagnostic[] actualDiagnostics,
        string expectedMessage,
        string expectedCode,
        AsnDiagnostic[] expectedDiagnostics)
    {
        Assert.That(actualMessage, Is.EqualTo(expectedMessage));
        Assert.That(actualCode, Is.EqualTo(expectedCode));
        Assert.That(actualDiagnostics, Is.SameAs(expectedDiagnostics));
    }

    private static void AssertEmptyPayload(string message, string code, AsnDiagnostic[] diagnostics)
    {
        Assert.That(message, Is.Empty);
        Assert.That(code, Is.Empty);
        Assert.That(diagnostics, Is.Empty);
    }
}
