namespace svg.Tests;

public class SvgDataUrlTests
{
    [Test]
    public void OmittedMediaTypeUsesRfcDefaultAndPercentDecodesBytes()
    {
        SvgDataUrlResult result = new SvgDataUrl().ParseWithValue("data:,A%20brief%20note%2E%FF");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgDataUrlError.None));
            Assert.That(result.MediaType.Type, Is.EqualTo("text"));
            Assert.That(result.MediaType.Subtype, Is.EqualTo("plain"));
            Assert.That(result.MediaType.ToText(), Is.EqualTo("text/plain; charset=US-ASCII"));
            Assert.That(result.Charset, Is.EqualTo("US-ASCII"));
            Assert.That(result.IsBase64, Is.False);
            Assert.That(result.Bytes, Is.EqualTo(new byte[] { 65, 32, 98, 114, 105, 101, 102, 32, 110, 111, 116, 101, 46, 255 }));
            Assert.That(result.DataOffset, Is.EqualTo(6));
        });
    }

    [Test]
    public void ExplicitMediaTypeDecodesParametersAndUtf8Payload()
    {
        SvgDataUrlResult result = new SvgDataUrl().ParseWithValue("data:Text/Plain;Charset=UTF-8;name=a%20b,Hello%2C%20%E2%9C%93");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.MediaType.Type, Is.EqualTo("text"));
            Assert.That(result.MediaType.Subtype, Is.EqualTo("plain"));
            Assert.That(result.MediaType.Parameters, Has.Count.EqualTo(2));
            Assert.That(result.MediaType.Parameters[1].Name, Is.EqualTo("name"));
            Assert.That(result.MediaType.Parameters[1].Value, Is.EqualTo("a b"));
            Assert.That(result.Charset, Is.EqualTo("UTF-8"));
            Assert.That(System.Text.Encoding.UTF8.GetString(result.Bytes), Is.EqualTo("Hello, ✓"));
        });
    }

    [Test]
    public void Base64AcceptsAsciiWhitespaceAndPercentEscapedAlphabetBytes()
    {
        SvgDataUrlResult result = new SvgDataUrl().ParseWithValue("DATA:image/svg+xml;profile=tiny;base64,PHN2\r\n\t Zy8%2B");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.IsBase64, Is.True);
            Assert.That(result.MediaType.ToText(), Is.EqualTo("image/svg+xml; profile=tiny"));
            Assert.That(System.Text.Encoding.ASCII.GetString(result.Bytes), Is.EqualTo("<svg/>"));
        });
    }

    [Test]
    public void Base64MarkerIsDistinctFromAParameterAndSupportsMediaShorthand()
    {
        SvgDataUrlResult parameter = new SvgDataUrl().ParseWithValue("data:text/plain;base64=token,abc");
        SvgDataUrlResult shorthand = new SvgDataUrl().ParseWithValue("data:;charset=utf-8;base64,4pyT");
        SvgDataUrlResult defaultCharset = new SvgDataUrl().ParseWithValue("data:;profile=tiny,");

        Assert.Multiple(() =>
        {
            Assert.That(parameter.Success, Is.True);
            Assert.That(parameter.IsBase64, Is.False);
            Assert.That(parameter.MediaType.Parameters.Single().Name, Is.EqualTo("base64"));
            Assert.That(parameter.Bytes, Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("abc")));
            Assert.That(shorthand.Success, Is.True);
            Assert.That(shorthand.IsBase64, Is.True);
            Assert.That(shorthand.MediaType.Type, Is.EqualTo("text"));
            Assert.That(shorthand.Charset, Is.EqualTo("utf-8"));
            Assert.That(System.Text.Encoding.UTF8.GetString(shorthand.Bytes), Is.EqualTo("✓"));
            Assert.That(defaultCharset.Charset, Is.EqualTo("US-ASCII"));
            Assert.That(defaultCharset.MediaType.Parameters[1].Name, Is.EqualTo("profile"));
        });
    }

    [Test]
    public void MalformedSchemeMetadataPayloadAndBase64HaveStableFailures()
    {
        SvgDataUrlResult scheme = new SvgDataUrl().ParseWithValue("http:,abc");
        SvgDataUrlResult comma = new SvgDataUrl().ParseWithValue("data:text/plain");
        SvgDataUrlResult marker = new SvgDataUrl().ParseWithValue("data:text/plain;base64;name=x,QQ==");
        SvgDataUrlResult rawSeparator = new SvgDataUrl().ParseWithValue("data:text/plain;name=a=b,abc");
        SvgDataUrlResult media = new SvgDataUrl().ParseWithValue("data:text,abc");
        SvgDataUrlResult percent = new SvgDataUrl().ParseWithValue("data:,a%2G");
        SvgDataUrlResult whitespace = new SvgDataUrl().ParseWithValue("data:,a b");
        SvgDataUrlResult fragment = new SvgDataUrl().ParseWithValue("data:,abc#fragment");
        SvgDataUrlResult unsafeRaw = new SvgDataUrl().ParseWithValue("data:,[");
        SvgDataUrlResult escapedUnsafe = new SvgDataUrl().ParseWithValue("data:,%5B");
        SvgDataUrlResult length = new SvgDataUrl().ParseWithValue("data:;base64,AAA");
        SvgDataUrlResult padding = new SvgDataUrl().ParseWithValue("data:;base64,AB==");

        Assert.Multiple(() =>
        {
            Assert.That(scheme.Error, Is.EqualTo(SvgDataUrlError.InvalidScheme));
            Assert.That(comma.Error, Is.EqualTo(SvgDataUrlError.MissingComma));
            Assert.That(marker.Error, Is.EqualTo(SvgDataUrlError.InvalidMediaType));
            Assert.That(rawSeparator.Error, Is.EqualTo(SvgDataUrlError.InvalidMediaType));
            Assert.That(media.Error, Is.EqualTo(SvgDataUrlError.InvalidMediaType));
            Assert.That(percent.Error, Is.EqualTo(SvgDataUrlError.InvalidPercentEscape));
            Assert.That(whitespace.Error, Is.EqualTo(SvgDataUrlError.InvalidPayload));
            Assert.That(fragment.Error, Is.EqualTo(SvgDataUrlError.InvalidPayload));
            Assert.That(unsafeRaw.Error, Is.EqualTo(SvgDataUrlError.InvalidPayload));
            Assert.That(escapedUnsafe.Bytes, Is.EqualTo(new byte[] { 91 }));
            Assert.That(length.Error, Is.EqualTo(SvgDataUrlError.InvalidBase64));
            Assert.That(padding.Error, Is.EqualTo(SvgDataUrlError.InvalidBase64));
            Assert.That(percent.ErrorOffset, Is.EqualTo("data:,a".Length));
        });
    }

    [Test]
    public void DecodedLimitsApplyToBothFormsAndResourceOptions()
    {
        SvgDataUrlResult percentFailure = new SvgDataUrl().ParseWithValueAndMaxDecodedBytes("data:,abcd", 3);
        SvgDataUrlResult base64Failure = new SvgDataUrl().ParseWithValueAndMaxDecodedBytes("data:;base64,YWJjZA==", 3);
        SvgDataUrlResult exact = new SvgDataUrl().ParseWithValueAndMaxDecodedBytes("data:;base64,YWJjZA==", 4);
        SvgResourceLimits limits = new SvgResourceLimits();
        limits.MaxDataUrlBytes = 0;
        SvgDataUrlResult empty = new SvgDataUrl().ParseWithValueAndLimits("data:;base64,", limits);
        SvgDataUrlResult nonempty = new SvgDataUrl().ParseWithValueAndLimits("data:,x", limits);
        SvgDataUrlEncodeResult encodeLimited = new SvgDataUrl().EncodeWithBytesAndMediaTypeAndUseBase64AndLimits(new byte[] { 1 }, "", true, limits);

        Assert.Multiple(() =>
        {
            Assert.That(percentFailure.Error, Is.EqualTo(SvgDataUrlError.DecodedSizeLimitExceeded));
            Assert.That(base64Failure.Error, Is.EqualTo(SvgDataUrlError.DecodedSizeLimitExceeded));
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.Bytes, Has.Length.EqualTo(4));
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.Bytes, Is.Empty);
            Assert.That(nonempty.Error, Is.EqualTo(SvgDataUrlError.DecodedSizeLimitExceeded));
            Assert.That(encodeLimited.Error, Is.EqualTo(SvgDataUrlError.DecodedSizeLimitExceeded));
        });
    }

    [Test]
    public void PercentEncodingIsDeterministicAndRoundTripsUtf8Text()
    {
        SvgDataUrlEncodeResult encoded = new SvgDataUrl().EncodeText("A brief ✓", "text/plain; charset=utf-8", false);
        SvgDataUrlResult parsed = new SvgDataUrl().ParseWithValue(encoded.DataUrl);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.DataUrl, Is.EqualTo("data:text/plain;charset=utf-8,A%20brief%20%E2%9C%93"));
            Assert.That(encoded.IsBase64, Is.False);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Charset, Is.EqualTo("utf-8"));
            Assert.That(System.Text.Encoding.UTF8.GetString(parsed.Bytes), Is.EqualTo("A brief ✓"));
        });
    }

    [Test]
    public void Base64EncodingCanonicalizesMediaParametersAndRoundTripsBinary()
    {
        byte[] bytes = new byte[] { 0, 1, 2, 253, 254, 255 };
        SvgDataUrlEncodeResult encoded = new SvgDataUrl().EncodeWithBytesAndMediaTypeAndUseBase64(bytes, "application/octet-stream; name=\"binary blob\"", true);
        SvgDataUrlResult parsed = new SvgDataUrl().ParseWithValue(encoded.DataUrl);
        SvgDataUrlEncodeResult badMedia = new SvgDataUrl().EncodeWithBytesAndMediaTypeAndUseBase64(bytes, "application", true);
        SvgDataUrlEncodeResult tooLarge = new SvgDataUrl().EncodeWithBytesAndMediaTypeAndUseBase64AndMaxDecodedBytes(bytes, "", false, 5);
        SvgDataUrlEncodeResult escapedToken = new SvgDataUrl().EncodeWithBytesAndMediaTypeAndUseBase64(System.Array.Empty<byte>(), "application/x#fixture", false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.DataUrl, Is.EqualTo("data:application/octet-stream;name=binary%20blob;base64,AAEC/f7/"));
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Bytes, Is.EqualTo(bytes));
            Assert.That(parsed.MediaType.Parameters.Single().Value, Is.EqualTo("binary blob"));
            Assert.That(badMedia.Error, Is.EqualTo(SvgDataUrlError.InvalidMediaType));
            Assert.That(tooLarge.Error, Is.EqualTo(SvgDataUrlError.DecodedSizeLimitExceeded));
            Assert.That(escapedToken.Success, Is.True);
            Assert.That(escapedToken.DataUrl, Is.EqualTo("data:application/x%23fixture,"));
            Assert.That(new SvgDataUrl().ParseWithValue(escapedToken.DataUrl).MediaType.Subtype, Is.EqualTo("x#fixture"));
        });
    }
}
