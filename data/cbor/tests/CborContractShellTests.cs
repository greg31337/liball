using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborContractShellTests
{
    [Test]
    public void EveryOperationShellCanBeConstructedThroughItsContract()
    {
        CborDecodeOptions decodeOptions = new CborDecodeOptions();
        CborEncodeOptions encodeOptions = new CborEncodeOptions();
        MemoryStream input = new MemoryStream();
        MemoryStream output = new MemoryStream();

        ICborDecoder decoder = new CborDecoder();
        ICborEncoder encoder = new CborEncoder();
        ICborTokenReader tokenReader = new CborTokenReader(decodeOptions);
        ICborTokenWriter tokenWriter = new CborTokenWriter(encodeOptions);
        ICborStreamReader streamReader = new CborStreamReader(input, decodeOptions);
        ICborStreamWriter streamWriter = new CborStreamWriter(output, encodeOptions);
        ICborValidator validator = new CborValidator();
        ICborDiagnosticConverter diagnostic = new CborDiagnosticConverter();
        ICborJsonConverter json = new CborJsonConverter();
        ICborObjectMapper mapper = new CborObjectMapper();
        ICborTagRegistry registry = new CborTagRegistry();

        Assert.Multiple(() =>
        {
            Assert.That(decoder, Is.Not.Null);
            Assert.That(encoder, Is.Not.Null);
            Assert.That(tokenReader, Is.Not.Null);
            Assert.That(tokenWriter, Is.Not.Null);
            Assert.That(streamReader, Is.Not.Null);
            Assert.That(streamWriter, Is.Not.Null);
            Assert.That(validator, Is.Not.Null);
            Assert.That(diagnostic, Is.Not.Null);
            Assert.That(json, Is.Not.Null);
            Assert.That(mapper, Is.Not.Null);
            Assert.That(registry, Is.Not.Null);
        });
    }

    [Test]
    public void BufferAndStreamShellOperationsFailPredictably()
    {
        CborDecodeOptions decodeOptions = new CborDecodeOptions();
        CborEncodeOptions encodeOptions = new CborEncodeOptions();
        ICborTokenReader tokenReader = new CborTokenReader(decodeOptions);
        ICborTokenWriter tokenWriter = new CborTokenWriter(encodeOptions);
        ICborStreamReader streamReader = new CborStreamReader(new MemoryStream(), decodeOptions);
        ICborStreamWriter streamWriter = new CborStreamWriter(new MemoryStream(), encodeOptions);
        CborToken token = new CborToken
        {
            Kind = CborTokenKind.UnsignedInteger,
            IntegerValue = System.Numerics.BigInteger.Zero
        };
        CborValue value = new CborValue();
        CborWriteResult tokenWrite = tokenWriter.WriteToken(token, new byte[1]);
        CborWriteResult tokenCompletion = tokenWriter.CompleteWriter([]);

        Assert.Multiple(() =>
        {
            Assert.That(tokenWrite.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(tokenWrite.Written, Is.EqualTo(1));
            Assert.That(tokenCompletion.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.DoesNotThrow(() => tokenWriter.ResetWriter());
            Assert.That(streamReader.ReadNext().Status, Is.EqualTo(CborReadStatus.EndOfData));
            Assert.That(streamWriter.WriteNext(value).Status, Is.EqualTo(CborWriteStatus.Error));
            Assert.DoesNotThrow(() => streamWriter.FlushOutput());
        });

        Assert.That(tokenReader.ReadToken([]).Status, Is.EqualTo(CborReadStatus.NeedMoreData));
        Assert.That(tokenReader.SkipValue([]).Status, Is.EqualTo(CborReadStatus.NeedMoreData));
        Assert.DoesNotThrow(() => tokenReader.ResetReader());
    }

    [Test]
    public void HighLevelShellOperationsFailPredictably()
    {
        CborDecodeOptions decodeOptions = new CborDecodeOptions();
        CborEncodeOptions encodeOptions = new CborEncodeOptions();
        CborDiagnosticOptions diagnosticOptions = new CborDiagnosticOptions();
        CborJsonOptions jsonOptions = new CborJsonOptions();
        CborMappingOptions mappingOptions = new CborMappingOptions();
        ICborDecoder decoder = new CborDecoder();
        ICborEncoder encoder = new CborEncoder();
        ICborValidator validator = new CborValidator();
        ICborDiagnosticConverter diagnostic = new CborDiagnosticConverter();
        ICborJsonConverter json = new CborJsonConverter();
        ICborObjectMapper mapper = new CborObjectMapper();
        ICborTagRegistry registry = new CborTagRegistry();
        CborValue value = new CborValue();
        CborTagRegistration registration = new CborTagRegistration();
        CborReadResult emptyDecode = decoder.Decode([], decodeOptions);
        CborReadResult emptyFirst = decoder.DecodeFirst([], decodeOptions);

        Assert.Multiple(() =>
        {
            Assert.That(emptyDecode.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(emptyDecode.ErrorCode, Is.EqualTo("CBOR_UNEXPECTED_END_OF_DATA"));
            Assert.That(emptyFirst.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(emptyFirst.ErrorCode, Is.EqualTo("CBOR_UNEXPECTED_END_OF_DATA"));
            Assert.That(
                Assert.Throws<CborEncodeException>(() => encoder.Encode(value, encodeOptions))?.ErrorCode,
                Is.EqualTo("CBOR_VALUE_KIND_INVALID"));
            Assert.That(
                encoder.EncodeInto(value, [], encodeOptions).ErrorCode,
                Is.EqualTo("CBOR_VALUE_KIND_INVALID"));
            Assert.That(
                Assert.Throws<CborEncodeException>(() => encoder.Measure(value, encodeOptions))?.ErrorCode,
                Is.EqualTo("CBOR_VALUE_KIND_INVALID"));
            Assert.That(
                validator.ValidateEncoding([], CborValidationLevel.Basic).IsValid,
                Is.False
            );
            Assert.That(
                validator.ValidateValue(value, CborValidationLevel.Basic).Issues[0].Code,
                Is.EqualTo("CBOR_VALUE_KIND_INVALID"));
            Assert.That(
                Assert.Throws<CborDiagnosticException>(() =>
                    diagnostic.FormatDiagnostic(value, diagnosticOptions))?.ErrorCode,
                Is.EqualTo("CBOR_VALUE_KIND_INVALID"));
            Assert.That(
                diagnostic.ParseDiagnostic("0", diagnosticOptions).Status,
                Is.EqualTo(CborReadStatus.Done));
            Assert.That(
                Assert.Throws<CborJsonException>(() =>
                    json.ConvertToJson(value, jsonOptions))?.ErrorCode,
                Is.EqualTo("CBOR_VALUE_KIND_INVALID"));
            Assert.That(
                json.ConvertFromJson("null", jsonOptions).Status,
                Is.EqualTo(CborReadStatus.Done));
            Assert.That(
                mapper.MapToValue("value", typeof(string), mappingOptions).TextStringValue,
                Is.EqualTo("value"));
            Assert.That(
                Assert.Throws<CborMappingException>(() =>
                    mapper.MapFromValue(value, typeof(string), mappingOptions))?.ErrorCode,
                Is.EqualTo("CBOR_MAPPING_TEXT_REQUIRED"));
            Assert.DoesNotThrow(() => registry.AddTag(registration));
            Assert.That(registry.FindTag(0), Is.Not.Null);
        });
    }
}
