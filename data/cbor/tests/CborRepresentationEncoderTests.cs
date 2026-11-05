using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborRepresentationEncoderTests
{
    [Test]
    public void PreservedNonPreferredHeadsAndFloatBitsRoundTripExactly()
    {
        byte[] source = Convert.FromHexString(
            "9A00000004"
            + "1B0000000000000000"
            + "79000161"
            + "DA00000001F8FF"
            + "FA3FC00000");
        CborValue value = DecodeWithRepresentation(source);
        CborEncoder encoder = new CborEncoder();
        CborEncodeOptions options = PreserveOptions();

        byte[] encoded = encoder.Encode(value, options);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(source));
            Assert.That(encoder.Measure(value, options), Is.EqualTo(source.Length));
            Assert.That(value.Representation?.HeadArgumentWidth, Is.EqualTo(4));
            Assert.That(value.ArrayItems?[0].Representation?.HeadArgumentWidth, Is.EqualTo(8));
            Assert.That(value.ArrayItems?[1].Representation?.HeadArgumentWidth, Is.EqualTo(2));
            Assert.That(value.ArrayItems?[2].Representation?.HeadArgumentWidth, Is.EqualTo(4));
            Assert.That(value.ArrayItems?[3].Representation?.FloatBits, Is.EqualTo(0x3fc00000));
        });
    }

    [Test]
    public void PreservedIndefiniteFormsAndStringChunksRoundTripExactly()
    {
        byte[] source = Convert.FromHexString(
            "9F"
            + "5F41AA4042BBCCFF"
            + "7F61616062C2A2FF"
            + "BF019F02FFFF"
            + "FF");
        CborValue value = DecodeWithRepresentation(source);
        CborValue bytes = value.ArrayItems?[0] ?? new CborValue();
        CborValue text = value.ArrayItems?[1] ?? new CborValue();
        CborEncoder encoder = new CborEncoder();
        CborEncodeOptions options = PreserveOptions();

        byte[] encoded = encoder.Encode(value, options);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(source));
            Assert.That(encoder.Measure(value, options), Is.EqualTo(source.Length));
            Assert.That(bytes.Representation?.ChunkByteLengths, Is.EqualTo(new[] { 1, 0, 2 }));
            Assert.That(text.Representation?.ChunkByteLengths, Is.EqualTo(new[] { 1, 0, 2 }));
            Assert.That(value.LengthKind, Is.EqualTo(CborLengthKind.Indefinite));
            Assert.That(value.ArrayItems?[2].LengthKind, Is.EqualTo(CborLengthKind.Indefinite));
        });
    }

    [Test]
    public void RepresentationPreservationIsOptIn()
    {
        CborValue value = DecodeWithRepresentation(
            Convert.FromHexString("1B0000000000000000"));
        CborEncoder encoder = new CborEncoder();

        byte[] ordinary = encoder.Encode(value, new CborEncodeOptions());
        byte[] preserved = encoder.Encode(value, PreserveOptions());

        Assert.Multiple(() =>
        {
            Assert.That(ordinary, Is.EqualTo(new byte[] { 0x00 }));
            Assert.That(
                preserved,
                Is.EqualTo(Convert.FromHexString("1B0000000000000000")));
        });
    }

    [Test]
    public void AStaleHeadWidthFailsWithoutWriting()
    {
        CborValue value = DecodeWithRepresentation(Convert.FromHexString("1818"));
        value.IntegerValue = new BigInteger(256);

        AssertPreservationFailure(
            value,
            "CBOR_REPRESENTATION_HEAD_WIDTH_INVALID");
    }

    [Test]
    public void AStaleLengthFormFailsWithoutWriting()
    {
        CborValue value = DecodeWithRepresentation(
            Convert.FromHexString("9F01FF"));
        value.LengthKind = CborLengthKind.Definite;

        AssertPreservationFailure(
            value,
            "CBOR_REPRESENTATION_LENGTH_KIND_MISMATCH");
    }

    [Test]
    public void StaleByteChunkLengthsFailWithoutWriting()
    {
        CborValue value = DecodeWithRepresentation(
            Convert.FromHexString("5F41AA42BBCCFF"));
        value.ByteStringValue = [0xaa, 0xbb, 0xcc, 0xdd];

        AssertPreservationFailure(
            value,
            "CBOR_REPRESENTATION_CHUNK_LENGTH_MISMATCH");
    }

    [Test]
    public void TextChunkBoundariesCannotSplitAMutatedUtf8Scalar()
    {
        CborValue value = DecodeWithRepresentation(
            Convert.FromHexString("7F616162C2A2FF"));
        value.TextStringValue = "¢a";

        AssertPreservationFailure(
            value,
            "CBOR_REPRESENTATION_TEXT_CHUNK_INVALID");
    }

    [Test]
    public void StaleFloatBitsFailWithoutWriting()
    {
        CborValue value = DecodeWithRepresentation(
            Convert.FromHexString("FA3FC00000"));
        if (value.FloatValue != null)
        {
            value.FloatValue.Value = 2.0;
        }

        AssertPreservationFailure(
            value,
            "CBOR_REPRESENTATION_FLOAT_MISMATCH");
    }

    [Test]
    public void IndefiniteSourceRangeMetadataWithoutChunksIsRejected()
    {
        CborReadResult decoded = new CborDecoder().Decode(
            Convert.FromHexString("5F41AAFF"),
            new CborDecodeOptions
            {
                RetainSourceRanges = true
            });
        CborValue value = decoded.Value ?? new CborValue();

        AssertPreservationFailure(
            value,
            "CBOR_REPRESENTATION_CHUNKS_MISSING");
    }

    private static CborValue DecodeWithRepresentation(byte[] source)
    {
        CborReadResult decoded = new CborDecoder().Decode(
            source,
            new CborDecodeOptions
            {
                TagPolicy = CborTagPolicy.Preserve,
                RetainRepresentation = true,
                RetainSourceRanges = true,
                DuplicateKeyPolicy = CborDuplicateKeyPolicy.Preserve
            });
        Assert.That(decoded.Status, Is.EqualTo(CborReadStatus.Done), decoded.ErrorMessage);
        Assert.That(decoded.Value, Is.Not.Null);
        return decoded.Value ?? new CborValue();
    }

    private static CborEncodeOptions PreserveOptions()
    {
        return new CborEncodeOptions
        {
            PreserveRepresentation = true
        };
    }

    private static void AssertPreservationFailure(
        CborValue value,
        string expectedCode)
    {
        CborEncoder encoder = new CborEncoder();
        CborEncodeOptions options = PreserveOptions();
        byte[] destination = Enumerable.Repeat((byte)0xa5, 64).ToArray();
        byte[] original = destination.ToArray();

        CborWriteResult result = encoder.EncodeInto(value, destination, options);
        CborEncodeException? exception = Assert.Throws<CborEncodeException>(
            () => encoder.Measure(value, options));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborWriteStatus.Error));
            Assert.That(result.ErrorCode, Is.EqualTo(expectedCode));
            Assert.That(result.Written, Is.Zero);
            Assert.That(destination, Is.EqualTo(original));
            Assert.That(exception?.ErrorCode, Is.EqualTo(expectedCode));
        });
    }
}
