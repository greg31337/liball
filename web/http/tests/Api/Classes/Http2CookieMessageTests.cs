using System.Text;

namespace http.Tests;

public class Http2CookieMessageTests
{
    [Test]
    public void RequestCookieLinesCombineAtTheirFirstPositionInWireOrder()
    {
        HttpHeaderField first = Field("cookie", "a=1");
        HttpHeaderField second = Field("cookie", "b=2");
        HttpHeaderField third = Field("cookie", "c=3");
        second.IsSensitive = true;
        third.NeverIndex = true;
        Http2MessageResult result = new Http2MessageCodec()
            .DecodeHttp2RequestHeaders(
                1,
                Headers(
                    Field(":method", "GET"),
                    Field(":scheme", "https"),
                    Field(":authority", "example.test"),
                    Field(":path", "/"),
                    Field("accept", "text/plain"),
                    first,
                    Field("x-middle", "value"),
                    second,
                    third,
                    Field("x-last", "value")),
                true,
                false);
        HttpHeaders headers = result.Request!.Headers;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(headers.Fields.Select(item => item.Name),
                Is.EqualTo(new[]
                {
                    "accept", "cookie", "x-middle", "x-last"
                }));
            Assert.That(Text(headers.Fields[1]),
                Is.EqualTo("a=1; b=2; c=3"));
            Assert.That(headers.Fields[1].IsSensitive, Is.True);
            Assert.That(headers.Fields[1].NeverIndex, Is.True);
        });

        first.Value[0] = (byte)'X';
        second.Value[0] = (byte)'Y';
        Assert.That(Text(headers.Fields[1]),
            Is.EqualTo("a=1; b=2; c=3"));
    }

    [Test]
    public void ResponseMappingAlsoNormalizesDecompressedCookieLines()
    {
        Http2MessageResult result = new Http2MessageCodec()
            .DecodeHttp2ResponseHeaders(
                3,
                Headers(
                    Field(":status", "200"),
                    Field("cookie", "one"),
                    Field("cookie", "two")),
                true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Response!.Headers.Fields,
                Has.Count.EqualTo(1));
            Assert.That(Text(result.Response.Headers.Fields[0]),
                Is.EqualTo("one; two"));
        });
    }

    [Test]
    public void ASingleCookieFieldRemainsOneOwnedField()
    {
        HttpHeaderField source = Field("cookie", "a=1; b=2");
        Http2MessageResult result = new Http2MessageCodec()
            .DecodeHttp2RequestHeaders(
                5,
                Headers(
                    Field(":method", "GET"),
                    Field(":scheme", "https"),
                    Field(":authority", "example.test"),
                    Field(":path", "/"),
                    source),
                true,
                false);
        HttpHeaderField mapped = result.Request!.Headers.Fields[0];

        source.Value[0] = (byte)'X';

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Request.Headers.Fields, Has.Count.EqualTo(1));
            Assert.That(Text(mapped), Is.EqualTo("a=1; b=2"));
        });
    }

    [Test]
    public void EncodingPreparationCanSplitAtSemicolonsAndTrimBoundaryWhitespace()
    {
        HttpHeaderField cookie = Field(
            "Cookie", "first=one ;\t second=two;third=three");
        cookie.IsSensitive = true;
        cookie.NeverIndex = true;
        HttpHeaders source = Headers(
            Field("Accept", "text/plain"),
            cookie,
            Field("X-Last", "value"));

        HttpHeaders split = Http2CookieSemantics.CreateEncodingCookieFields(
            source, true);

        Assert.Multiple(() =>
        {
            Assert.That(split.Fields.Select(item => item.Name),
                Is.EqualTo(new[]
                {
                    "Accept", "Cookie", "Cookie", "Cookie", "X-Last"
                }));
            Assert.That(split.Fields.Select(Text),
                Is.EqualTo(new[]
                {
                    "text/plain", "first=one", "second=two",
                    "third=three", "value"
                }));
            Assert.That(split.Fields[1].IsSensitive, Is.True);
            Assert.That(split.Fields[2].IsSensitive, Is.True);
            Assert.That(split.Fields[3].NeverIndex, Is.True);
        });

        cookie.Value[0] = (byte)'X';
        Assert.That(Text(split.Fields[1]), Is.EqualTo("first=one"));
    }

    [Test]
    public void EncodingPreparationCanPreserveCookieFieldsWithoutSplitting()
    {
        HttpHeaders source = Headers(Field("Cookie", "a=1; b=2"));

        HttpHeaders preserved =
            Http2CookieSemantics.CreateEncodingCookieFields(source, false);

        source.Fields[0].Value[0] = (byte)'X';
        Assert.Multiple(() =>
        {
            Assert.That(preserved.Fields, Has.Count.EqualTo(1));
            Assert.That(preserved.Fields[0].Name, Is.EqualTo("Cookie"));
            Assert.That(Text(preserved.Fields[0]), Is.EqualTo("a=1; b=2"));
        });
    }

    static string Text(HttpHeaderField field)
    {
        return Encoding.ASCII.GetString(field.Value);
    }

    static HttpHeaders Headers(params HttpHeaderField[] fields)
    {
        HttpHeaders headers = new HttpHeaders();
        headers.Fields.AddRange(fields);
        return headers;
    }

    static HttpHeaderField Field(string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        return field;
    }
}
