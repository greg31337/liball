using System.Text;

namespace gltf.Tests.Internal.Resources;

[TestFixture]
public class Base64CodecTests
{
    [TestCase("", "")]
    [TestCase("f", "Zg==")]
    [TestCase("fo", "Zm8=")]
    [TestCase("foo", "Zm9v")]
    [TestCase("foob", "Zm9vYg==")]
    [TestCase("fooba", "Zm9vYmE=")]
    [TestCase("foobar", "Zm9vYmFy")]
    public void EncodeAndDecodeBase64Bytes_MatchRfc4648Vectors(string plain, string encoded)
    {
        Base64Codec codec = new Base64Codec();
        byte[] plainBytes = Encoding.ASCII.GetBytes(plain);

        string actualEncoded = codec.EncodeBase64Bytes(plainBytes);
        byte[] actualDecoded = codec.DecodeBase64Bytes(
            Encoding.ASCII.GetBytes(encoded),
            int.MaxValue,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(actualEncoded, Is.EqualTo(encoded));
            Assert.That(actualDecoded, Is.EqualTo(plainBytes));
        });
    }

    [Test]
    public void EncodeAndDecodeBase64Bytes_RoundTripEveryByteValueWithOwnedOutput()
    {
        byte[] bytes = new byte[256];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)index;
            index++;
        }

        Base64Codec codec = new Base64Codec();
        string encoded = codec.EncodeBase64Bytes(bytes);
        byte[] decoded = codec.DecodeBase64Bytes(
            Encoding.ASCII.GetBytes(encoded),
            bytes.Length,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Is.EqualTo(bytes));
            Assert.That(decoded, Is.Not.SameAs(bytes));
            Assert.That(encoded, Does.Not.Contain("\r"));
            Assert.That(encoded, Does.Not.Contain("\n"));
            Assert.That(encoded.Length % 4, Is.Zero);
        });
    }

    [Test]
    public void DecodeBase64Bytes_UsesTheStandardAlphabet()
    {
        Base64Codec codec = new Base64Codec();

        Assert.Multiple(() =>
        {
            Assert.That(
                codec.DecodeBase64Bytes(Encoding.ASCII.GetBytes("+/8="), 2, false),
                Is.EqualTo(new byte[] { 251, 255 }));
            Assert.That(
                () => codec.DecodeBase64Bytes(Encoding.ASCII.GetBytes("-_8="), 2, false),
                Throws.TypeOf<GltfResourceException>()
                    .With.Message.EqualTo("Base64 input contains an invalid character at byte 0."));
        });
    }

    [Test]
    public void DecodeBase64Bytes_AppliesTheExplicitWhitespacePolicy()
    {
        byte[] wrapped = Encoding.ASCII.GetBytes(" Z\tm9v\r\n");
        Base64Codec codec = new Base64Codec();

        Assert.Multiple(() =>
        {
            Assert.That(
                codec.DecodeBase64Bytes(wrapped, 3, true),
                Is.EqualTo(Encoding.ASCII.GetBytes("foo")));
            Assert.That(
                () => codec.DecodeBase64Bytes(wrapped, 3, false),
                Throws.TypeOf<GltfResourceException>()
                    .With.Message.EqualTo("Base64 input contains whitespace at byte 0."));
            Assert.That(
                () => codec.DecodeBase64Bytes(Encoding.ASCII.GetBytes("Zm\v9v"), 3, true),
                Throws.TypeOf<GltfResourceException>()
                    .With.Message.EqualTo("Base64 input contains an invalid character at byte 2."));
        });
    }

    [TestCase("Zg", "Base64 input length is not a multiple of four.")]
    [TestCase("Zg=", "Base64 input length is not a multiple of four.")]
    [TestCase("Zg===", "Base64 input length is not a multiple of four.")]
    [TestCase("=AAA", "Base64 padding must appear only at the end.")]
    [TestCase("AA=A", "Base64 padding must appear only at the end.")]
    [TestCase("AA==AAAA", "Base64 padding must appear only at the end.")]
    [TestCase("AAAA====", "Base64 padding must appear only at the end.")]
    public void DecodeBase64Bytes_RejectsInvalidLengthAndPadding(string encoded, string message)
    {
        Assert.That(
            () => new Base64Codec().DecodeBase64Bytes(
                Encoding.ASCII.GetBytes(encoded),
                int.MaxValue,
                false),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(message));
    }

    [TestCase("Zh==")]
    [TestCase("Zm9=")]
    public void DecodeBase64Bytes_RejectsNonCanonicalUnusedBits(string encoded)
    {
        Assert.That(
            () => new Base64Codec().DecodeBase64Bytes(
                Encoding.ASCII.GetBytes(encoded),
                int.MaxValue,
                false),
            Throws.TypeOf<GltfResourceException>()
                .With.Message.EqualTo("Base64 input has non-zero unused bits before padding."));
    }

    [Test]
    public void DecodeBase64Bytes_EnforcesTheDecodedSizeBeforeReturningBytes()
    {
        Base64Codec codec = new Base64Codec();
        byte[] encoded = Encoding.ASCII.GetBytes("Zm9v");

        Assert.Multiple(() =>
        {
            Assert.That(
                () => codec.DecodeBase64Bytes(encoded, 2, false),
                Throws.TypeOf<GltfResourceException>()
                    .With.Message.EqualTo(
                        "Base64 decoded length 3 exceeds the configured maximum 2."));
            Assert.That(codec.DecodeBase64Bytes(encoded, 3, false),
                Is.EqualTo(Encoding.ASCII.GetBytes("foo")));
            Assert.That(
                () => codec.DecodeBase64Bytes(encoded, -1, false),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName").EqualTo("maximumDecodedByteLength"));
        });
    }
}
