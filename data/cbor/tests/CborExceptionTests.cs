using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborExceptionTests
{
    [Test]
    public void DecodeExceptionPreservesFailureMetadataWhenThrown()
    {
        CborDecodeException exception = new CborDecodeException(
            "CBOR_DECODE_TRUNCATED",
            "The encoded item is incomplete.",
            17,
            "$[2]");
        CborDecodeException? caught = Assert.Throws<CborDecodeException>(() => throw exception);

        Assert.Multiple(() =>
        {
            Assert.That(caught, Is.SameAs(exception));
            Assert.That(exception.ErrorCode, Is.EqualTo("CBOR_DECODE_TRUNCATED"));
            Assert.That(exception.Message, Is.EqualTo("The encoded item is incomplete."));
            Assert.That(exception.ErrorOffset, Is.EqualTo(17));
            Assert.That(exception.ErrorPath, Is.EqualTo("$[2]"));
        });
    }

    [Test]
    public void EncodeExceptionPreservesFailureMetadataWhenThrown()
    {
        CborEncodeException exception = new CborEncodeException(
            "CBOR_ENCODE_INVALID_VALUE",
            "The value is inconsistent.",
            9,
            "$.value");
        CborEncodeException? caught = Assert.Throws<CborEncodeException>(() => throw exception);

        Assert.Multiple(() =>
        {
            Assert.That(caught, Is.SameAs(exception));
            Assert.That(exception.ErrorCode, Is.EqualTo("CBOR_ENCODE_INVALID_VALUE"));
            Assert.That(exception.Message, Is.EqualTo("The value is inconsistent."));
            Assert.That(exception.ErrorOffset, Is.EqualTo(9));
            Assert.That(exception.ErrorPath, Is.EqualTo("$.value"));
        });
    }

    [Test]
    public void ValidationExceptionPreservesFailureMetadataWhenThrown()
    {
        CborValidationException exception = new CborValidationException(
            "CBOR_VALIDATION_BUDGET",
            "The validation budget was exhausted.",
            128,
            "$.items[4]");
        CborValidationException? caught = Assert.Throws<CborValidationException>(() => throw exception);

        Assert.Multiple(() =>
        {
            Assert.That(caught, Is.SameAs(exception));
            Assert.That(exception.ErrorCode, Is.EqualTo("CBOR_VALIDATION_BUDGET"));
            Assert.That(exception.Message, Is.EqualTo("The validation budget was exhausted."));
            Assert.That(exception.ErrorOffset, Is.EqualTo(128));
            Assert.That(exception.ErrorPath, Is.EqualTo("$.items[4]"));
        });
    }

    [Test]
    public void DiagnosticExceptionPreservesFailureMetadataWhenThrown()
    {
        CborDiagnosticException exception = new CborDiagnosticException(
            "CBOR_DIAGNOSTIC_ESCAPE",
            "The escape sequence is invalid.",
            6,
            null);
        CborDiagnosticException? caught = Assert.Throws<CborDiagnosticException>(() => throw exception);

        Assert.Multiple(() =>
        {
            Assert.That(caught, Is.SameAs(exception));
            Assert.That(exception.ErrorCode, Is.EqualTo("CBOR_DIAGNOSTIC_ESCAPE"));
            Assert.That(exception.Message, Is.EqualTo("The escape sequence is invalid."));
            Assert.That(exception.ErrorOffset, Is.EqualTo(6));
            Assert.That(exception.ErrorPath, Is.Null);
        });
    }

    [Test]
    public void MappingExceptionPreservesFailureMetadataWhenThrown()
    {
        CborMappingException exception = new CborMappingException(
            "CBOR_MAPPING_OVERFLOW",
            "The integer does not fit the target member.",
            -1,
            "$.Count");
        CborMappingException? caught = Assert.Throws<CborMappingException>(() => throw exception);

        Assert.Multiple(() =>
        {
            Assert.That(caught, Is.SameAs(exception));
            Assert.That(exception.ErrorCode, Is.EqualTo("CBOR_MAPPING_OVERFLOW"));
            Assert.That(exception.Message, Is.EqualTo("The integer does not fit the target member."));
            Assert.That(exception.ErrorOffset, Is.EqualTo(-1));
            Assert.That(exception.ErrorPath, Is.EqualTo("$.Count"));
        });
    }
}
