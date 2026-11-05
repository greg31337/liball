using System.Text;

namespace gltf.Tests.Internal.Resources;

[TestFixture]
public class DataUriCodecTests
{
    [Test]
    public void ParseDataUri_AppliesTheOmittedMediaTypeAndCharsetDefaults()
    {
        DataUriData result = new DataUriCodec().ParseDataUri("data:,A%20brief%20note");

        Assert.Multiple(() =>
        {
            Assert.That(result.SourceUri, Is.EqualTo("data:,A%20brief%20note"));
            Assert.That(result.HasDeclaredMimeType, Is.False);
            Assert.That(result.DeclaredMimeType, Is.Null);
            Assert.That(result.EffectiveMimeType, Is.EqualTo("text/plain"));
            Assert.That(result.HasDeclaredCharset, Is.False);
            Assert.That(result.EffectiveCharset, Is.EqualTo("US-ASCII"));
            Assert.That(result.Parameters, Is.Empty);
            Assert.That(result.Base64, Is.False);
            Assert.That(result.Bytes, Is.EqualTo(Encoding.ASCII.GetBytes("A brief note")));
        });
    }

    [Test]
    public void ParseDataUri_UsesTheTextPlainShorthandWithAnExplicitCharset()
    {
        DataUriData result = new DataUriCodec().ParseDataUri(
            "data:;charset=UTF-8,hello%20world");

        Assert.Multiple(() =>
        {
            Assert.That(result.HasDeclaredMimeType, Is.False);
            Assert.That(result.DeclaredMimeType, Is.Null);
            Assert.That(result.EffectiveMimeType, Is.EqualTo("text/plain"));
            Assert.That(result.HasDeclaredCharset, Is.True);
            Assert.That(result.EffectiveCharset, Is.EqualTo("UTF-8"));
            Assert.That(result.Parameters, Has.Count.EqualTo(1));
            Assert.That(result.Parameters[0].Name, Is.EqualTo("charset"));
            Assert.That(result.Parameters[0].Value, Is.EqualTo("UTF-8"));
        });
    }

    [Test]
    public void ParseDataUri_RetainsDeclaredMediaTypeAndOrderedDecodedParameters()
    {
        DataUriData result = new DataUriCodec().ParseDataUri(
            "DATA:APPLICATION/VND.Example+JSON;CHARSET=utf-8;name=a%3Bb,%7B%22x%22:1%7D");

        Assert.Multiple(() =>
        {
            Assert.That(result.HasDeclaredMimeType, Is.True);
            Assert.That(result.DeclaredMimeType, Is.EqualTo("application/vnd.example+json"));
            Assert.That(result.EffectiveMimeType, Is.EqualTo("application/vnd.example+json"));
            Assert.That(result.HasDeclaredCharset, Is.True);
            Assert.That(result.EffectiveCharset, Is.EqualTo("utf-8"));
            Assert.That(result.Parameters.Select(parameter => parameter.Name),
                Is.EqualTo(new[] { "charset", "name" }));
            Assert.That(result.Parameters.Select(parameter => parameter.Value),
                Is.EqualTo(new[] { "utf-8", "a;b" }));
            Assert.That(Encoding.UTF8.GetString(result.Bytes), Is.EqualTo("{\"x\":1}"));
        });
    }

    [TestCase("data:application/octet-stream;base64,AAECA/8=", new byte[] { 0, 1, 2, 3, 255 })]
    [TestCase("data:text/plain;BASE64,SGVsbG8=", new byte[] { 72, 101, 108, 108, 111 })]
    [TestCase("data:text/plain;base64,SGVsbG8%3D", new byte[] { 72, 101, 108, 108, 111 })]
    [TestCase("data:;base64,", new byte[0])]
    public void ParseDataUri_RecognizesAndDecodesTheBase64Marker(string uri, byte[] expected)
    {
        DataUriData result = new DataUriCodec().ParseDataUri(uri);

        Assert.Multiple(() =>
        {
            Assert.That(result.Base64, Is.True);
            Assert.That(result.Bytes, Is.EqualTo(expected));
        });
    }

    [Test]
    public void ParseDataUri_TreatsBase64WithAValueAsAnOrdinaryParameter()
    {
        DataUriData result = new DataUriCodec().ParseDataUri(
            "data:text/plain;base64=value,plain");

        Assert.Multiple(() =>
        {
            Assert.That(result.Base64, Is.False);
            Assert.That(result.Parameters, Has.Count.EqualTo(1));
            Assert.That(result.Parameters[0].Name, Is.EqualTo("base64"));
            Assert.That(result.Parameters[0].Value, Is.EqualTo("value"));
            Assert.That(result.Bytes, Is.EqualTo(Encoding.ASCII.GetBytes("plain")));
        });
    }

    [Test]
    public void ParseDataUri_DecodesPercentPayloadAsArbitraryOwnedBytes()
    {
        DataUriCodec codec = new DataUriCodec();

        DataUriData first = codec.ParseDataUri("data:application/octet-stream,%00%7F%80%FF");
        DataUriData second = codec.ParseDataUri("data:application/octet-stream,%00%7F%80%FF");

        Assert.Multiple(() =>
        {
            Assert.That(first.Bytes, Is.EqualTo(new byte[] { 0, 127, 128, 255 }));
            Assert.That(second.Bytes, Is.EqualTo(first.Bytes));
            Assert.That(second.Bytes, Is.Not.SameAs(first.Bytes));
        });
    }

    [Test]
    public void ParseLimitedDataUri_AppliesLimitsAfterPercentDecodingAtTheExactBoundary()
    {
        DataUriCodec codec = new DataUriCodec();

        DataUriData percent = codec.ParseLimitedDataUri(
            "data:application/octet-stream,A%20%00B",
            4);
        DataUriData base64 = codec.ParseLimitedDataUri(
            "data:application/octet-stream;base64,Zg%3D%3D",
            1);

        Assert.Multiple(() =>
        {
            Assert.That(percent.Bytes, Is.EqualTo(new byte[] { 65, 32, 0, 66 }));
            Assert.That(base64.Bytes, Is.EqualTo(new byte[] { 102 }));
            Assert.That(
                () => codec.ParseLimitedDataUri(
                    "data:application/octet-stream,A%20%00B",
                    3),
                Throws.TypeOf<GltfResourceException>()
                    .With.Message.EqualTo(
                        "Data URI decoded length exceeds the configured maximum 3."));
            Assert.That(
                () => codec.ParseLimitedDataUri(
                    "data:application/octet-stream;base64,Zg%3D%3D",
                    0),
                Throws.TypeOf<GltfResourceException>()
                    .With.Message.EqualTo(
                        "Data URI decoded length exceeds the configured maximum 0."));
        });
    }

    [TestCase("http:text/plain,abc", "The URI does not use the data scheme.")]
    [TestCase("data:text/plain", "The data URI is missing its comma separator.")]
    [TestCase("data:text,abc", "Data URI media type 'text' is invalid.")]
    [TestCase("data:/plain,abc", "Data URI media type '/plain' is invalid.")]
    [TestCase("data:text/,abc", "Data URI media type 'text/' is invalid.")]
    [TestCase("data:text/plain/extra,abc", "Data URI media type 'text/plain/extra' is invalid.")]
    [TestCase("data:text/plain;;name=value,abc", "The data URI contains an empty metadata segment.")]
    [TestCase("data:text/plain;name,abc", "Data URI metadata segment 1 is not a valid parameter.")]
    [TestCase("data:text/plain;name=,abc", "Data URI metadata segment 1 is not a valid parameter.")]
    [TestCase(
        "data:text/plain;base64;charset=utf-8,",
        "The data URI base64 marker must follow all media type parameters.")]
    public void ParseDataUri_RejectsMalformedStructure(string uri, string message)
    {
        Assert.That(
            () => new DataUriCodec().ParseDataUri(uri),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(message));
    }

    [TestCase("data:,a b", "The data URI payload contains an unsafe character at character 1.")]
    [TestCase("data:,abc#fragment", "The data URI payload contains an unsafe character at character 3.")]
    [TestCase("data:,café", "The data URI payload contains an unsafe character at character 3.")]
    [TestCase("data:text plain,abc", "The data URI metadata contains an unsafe character at character 4.")]
    public void ParseDataUri_RejectsRawCharactersOutsideRfc2397UrlChar(
        string uri,
        string message)
    {
        Assert.That(
            () => new DataUriCodec().ParseDataUri(uri),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(message));
    }

    [TestCase("data:,%", "Malformed percent escape at character 0.")]
    [TestCase("data:,%2", "Malformed percent escape at character 0.")]
    [TestCase("data:,%GG", "Malformed percent escape at character 0.")]
    [TestCase("data:text%2,abc", "Malformed percent escape at character 4.")]
    public void ParseDataUri_RejectsMalformedPercentEscapes(string uri, string message)
    {
        Assert.That(
            () => new DataUriCodec().ParseDataUri(uri),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(message));
    }

    [TestCase(
        "data:application/octet-stream;base64,AAA",
        "Base64 input length is not a multiple of four.")]
    [TestCase(
        "data:application/octet-stream;base64,@@@@",
        "Base64 input contains an invalid character at byte 0.")]
    [TestCase(
        "data:application/octet-stream;base64,AA=A",
        "Base64 padding must appear only at the end.")]
    [TestCase(
        "data:application/octet-stream;base64,AA==AAAA",
        "Base64 padding must appear only at the end.")]
    [TestCase(
        "data:application/octet-stream;base64,Zh==",
        "Base64 input has non-zero unused bits before padding.")]
    public void ParseDataUri_RejectsMalformedBase64Payloads(string uri, string message)
    {
        Assert.That(
            () => new DataUriCodec().ParseDataUri(uri),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(message));
    }
}
