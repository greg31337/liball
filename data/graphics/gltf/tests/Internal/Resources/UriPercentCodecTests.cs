namespace gltf.Tests.Internal.Resources;

[TestFixture]
public class UriPercentCodecTests
{
    [TestCase("", "")]
    [TestCase("unreserved-AZaz09-._~", "unreserved-AZaz09-._~")]
    [TestCase("hello%20world", "hello world")]
    [TestCase("caf%C3%A9", "café")]
    [TestCase("%E3%82%A2", "ア")]
    [TestCase("%F0%9F%98%80", "😀")]
    [TestCase("%2F%3F%23%25", "/?#%")]
    [TestCase("%252F", "%2F")]
    [TestCase("raw-café", "raw-café")]
    public void DecodeText_DecodesPercentOctetsAsUtf8ExactlyOnce(string encoded, string expected)
    {
        Assert.That(new UriPercentCodec().DecodeText(encoded), Is.EqualTo(expected));
    }

    [TestCase("%", 0)]
    [TestCase("abc%", 3)]
    [TestCase("%2", 0)]
    [TestCase("a%2", 1)]
    [TestCase("%GG", 0)]
    [TestCase("a%2Xz", 1)]
    public void DecodeText_RejectsMalformedPercentEscapes(string value, int index)
    {
        Assert.That(
            () => new UriPercentCodec().DecodeText(value),
            Throws.TypeOf<GltfResourceException>()
                .With.Message.EqualTo($"Malformed percent escape at character {index}."));
    }

    [TestCase("%80", 0)]
    [TestCase("%C0%AF", 0)]
    [TestCase("%C2", 0)]
    [TestCase("%C2A", 1)]
    [TestCase("%E0%80%80", 0)]
    [TestCase("%ED%A0%80", 0)]
    [TestCase("%F0%80%80%80", 0)]
    [TestCase("%F4%90%80%80", 0)]
    [TestCase("%F5%80%80%80", 0)]
    public void DecodeText_RejectsInvalidUtf8(string value, int byteIndex)
    {
        Assert.That(
            () => new UriPercentCodec().DecodeText(value),
            Throws.TypeOf<GltfResourceException>()
                .With.Message.EqualTo(
                    $"Percent-decoded URI text is not valid UTF-8 at byte {byteIndex}."));
    }

    [TestCase("", "")]
    [TestCase("AZaz09-._~", "AZaz09-._~")]
    [TestCase("a b", "a%20b")]
    [TestCase("a/b?c#d", "a%2Fb%3Fc%23d")]
    [TestCase(":@!$&'()*+,;=", "%3A%40%21%24%26%27%28%29%2A%2B%2C%3B%3D")]
    [TestCase("%2F", "%252F")]
    [TestCase("café", "caf%C3%A9")]
    [TestCase("日本/😀", "%E6%97%A5%E6%9C%AC%2F%F0%9F%98%80")]
    public void EncodePathSegment_UsesUtf8AndEncodesReservedData(string value, string expected)
    {
        Assert.That(new UriPercentCodec().EncodePathSegment(value), Is.EqualTo(expected));
    }

    [Test]
    public void EncodeAndDecodePathSegment_RoundTripsUnicodeAndReservedText()
    {
        string value = "folder/100% 日本語 😀?#";
        UriPercentCodec codec = new UriPercentCodec();

        string encoded = codec.EncodePathSegment(value);

        Assert.Multiple(() =>
        {
            Assert.That(codec.DecodeText(encoded), Is.EqualTo(value));
            Assert.That(encoded, Does.Not.Contain("/"));
            Assert.That(encoded, Does.Not.Contain("?"));
            Assert.That(encoded, Does.Not.Contain("#"));
        });
    }

    [Test]
    public void EncodePathSegment_RejectsUnpairedUtf16Surrogates()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => new UriPercentCodec().EncodePathSegment("a\uD800b"),
                Throws.TypeOf<GltfResourceException>()
                    .With.Message.EqualTo(
                        "URI text contains an unpaired UTF-16 surrogate at character 1."));
            Assert.That(
                () => new UriPercentCodec().EncodePathSegment("\uDC00"),
                Throws.TypeOf<GltfResourceException>()
                    .With.Message.EqualTo(
                        "URI text contains an unpaired UTF-16 surrogate at character 0."));
        });
    }

    [TestCase(
        "Images/My File%2f%7etex.png?x=a%26b&y=c d#part%2ftwo",
        "Images/My%20File%2F~tex.png?x=a%26b&y=c%20d#part%2Ftwo")]
    [TestCase("a%252Fb/%41.bin", "a%252Fb/A.bin")]
    [TestCase("folder/café.bin", "folder/caf%C3%A9.bin")]
    [TestCase("folder/a:b@c!d.bin", "folder/a:b@c!d.bin")]
    [TestCase("?#", "?#")]
    public void NormalizeReference_ProducesDeterministicRelativeUriText(
        string value,
        string expected)
    {
        Assert.That(new UriReference().NormalizeReference(value), Is.EqualTo(expected));
    }

    [Test]
    public void NormalizeReference_PreservesReservedEscapesAndGenericDelimiters()
    {
        UriReference reference = new UriReference();

        Assert.Multiple(() =>
        {
            Assert.That(
                reference.NormalizeReference("HTTPS://example.com/a%2fb?q=a/b?c%23d#x/y?z%23q"),
                Is.EqualTo("https://example.com/a%2Fb?q=a/b?c%23d#x/y?z%23q"));
            Assert.That(
                reference.NormalizeReference("%2e/%2E%2e/file"),
                Is.EqualTo("%2E/%2E%2E/file"));
            Assert.That(reference.NormalizeReference("a%252Fb"), Is.EqualTo("a%252Fb"));
        });
    }

    [Test]
    public void UriReferenceParsingAndNormalization_RejectMalformedOrInvalidUtf8Escapes()
    {
        UriReference reference = new UriReference();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => reference.ParseReference("images/bad%2.png"),
                Throws.TypeOf<GltfResourceException>()
                    .With.Message.EqualTo("Malformed percent escape at character 10."));
            Assert.That(
                () => reference.NormalizeReference("images/%FF.bin"),
                Throws.TypeOf<GltfResourceException>()
                    .With.Message.EqualTo(
                        "Percent-decoded URI text is not valid UTF-8 at byte 7."));
        });
    }
}
