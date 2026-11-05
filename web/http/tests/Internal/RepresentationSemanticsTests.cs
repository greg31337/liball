using System.Text;

namespace http.Tests;

public class RepresentationSemanticsTests
{
    [Test]
    public void ExtractionParsesCompleteRepresentationMetadataAndOwnsContent()
    {
        HttpResponse response = new HttpResponse();
        AddField(response.Headers, HttpHeaderNames.ContentType,
            " Text/Plain; Charset=\"utf-8\"; format=flowed ");
        AddField(response.Headers, HttpHeaderNames.ContentEncoding, "gzip");
        AddField(response.Headers, HttpHeaderNames.ContentEncoding, "br");
        AddField(response.Headers, HttpHeaderNames.ContentLanguage, "en-US, fr");
        AddField(response.Headers, HttpHeaderNames.ContentLength, " 12 ");
        AddField(response.Headers, HttpHeaderNames.ContentLocation, "/documents/1?q=yes");
        AddField(response.Headers, HttpHeaderNames.ETag, " W/\"version-1\" ");
        AddField(response.Headers, HttpHeaderNames.LastModified,
            "Sun, 06 Nov 1994 08:49:37 GMT");
        AddField(response.Headers, HttpHeaderNames.AcceptRanges, "bytes, custom-unit");
        response.Content = new byte[] { 1, 2, 3 };

        HttpRepresentation representation = RepresentationSemantics.ExtractFromResponse(response);

        Assert.Multiple(() =>
        {
            Assert.That(representation.MediaType, Is.Not.Null);
            Assert.That(representation.MediaType!.Type, Is.EqualTo("Text"));
            Assert.That(representation.MediaType.Subtype, Is.EqualTo("Plain"));
            Assert.That(representation.MediaType.Parameters, Has.Count.EqualTo(2));
            Assert.That(representation.Charset, Is.EqualTo("utf-8"));
            Assert.That(representation.ContentCodings, Is.EqualTo(new[] { "gzip", "br" }));
            Assert.That(representation.Languages, Is.EqualTo(new[] { "en-US", "fr" }));
            Assert.That(representation.HasContentLength, Is.True);
            Assert.That(representation.ContentLength, Is.EqualTo(12));
            Assert.That(representation.ContentLocation, Is.EqualTo("/documents/1?q=yes"));
            Assert.That(representation.EntityTag!.IsWeak, Is.True);
            Assert.That(representation.EntityTag.OpaqueTag,
                Is.EqualTo("version-1"u8.ToArray()));
            Assert.That(representation.HasLastModified, Is.True);
            Assert.That(representation.LastModified,
                Is.EqualTo(new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero)));
            Assert.That(representation.AcceptedRangeUnits,
                Is.EqualTo(new[] { "bytes", "custom-unit" }));
            Assert.That(representation.Content, Is.EqualTo(response.Content));
            Assert.That(representation.Content, Is.Not.SameAs(response.Content));
        });
    }

    [Test]
    public void ApplicationReplacesManagedFieldsAndPreservesOtherFields()
    {
        HttpResponse response = new HttpResponse();
        AddField(response.Headers, HttpHeaderNames.ContentType, "application/old");
        AddField(response.Headers, HttpHeaderNames.ContentEncoding, "compress");
        AddField(response.Headers, HttpHeaderNames.ContentLanguage, "de");
        AddField(response.Headers, HttpHeaderNames.ContentLength, "99");
        AddField(response.Headers, HttpHeaderNames.ContentLocation, "/old");
        AddField(response.Headers, HttpHeaderNames.ETag, "\"old\"");
        AddField(response.Headers, HttpHeaderNames.LastModified,
            "Sun, 06 Nov 1994 08:49:37 GMT");
        AddField(response.Headers, HttpHeaderNames.AcceptRanges, "none");
        AddField(response.Headers, "X-Preserved", "yes");

        HttpRepresentation representation = CompleteRepresentation();
        RepresentationSemantics.ApplyToResponse(representation, response);

        Assert.Multiple(() =>
        {
            Assert.That(Values(response.Headers, HttpHeaderNames.ContentType),
                Is.EqualTo(new[] { "text/plain; format=flowed; charset=iso-8859-1" }));
            Assert.That(Values(response.Headers, HttpHeaderNames.ContentEncoding),
                Is.EqualTo(new[] { "gzip, br" }));
            Assert.That(Values(response.Headers, HttpHeaderNames.ContentLanguage),
                Is.EqualTo(new[] { "en-US, fr" }));
            Assert.That(Values(response.Headers, HttpHeaderNames.ContentLength),
                Is.EqualTo(new[] { "3" }));
            Assert.That(Values(response.Headers, HttpHeaderNames.ContentLocation),
                Is.EqualTo(new[] { "/current" }));
            Assert.That(Values(response.Headers, HttpHeaderNames.ETag),
                Is.EqualTo(new[] { "\"current\"" }));
            Assert.That(Values(response.Headers, HttpHeaderNames.LastModified),
                Is.EqualTo(new[] { "Sun, 06 Nov 1994 08:49:37 GMT" }));
            Assert.That(Values(response.Headers, HttpHeaderNames.AcceptRanges),
                Is.EqualTo(new[] { "bytes, custom-unit" }));
            Assert.That(Values(response.Headers, "X-Preserved"), Is.EqualTo(new[] { "yes" }));
            Assert.That(response.Content, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(response.Content, Is.Not.SameAs(representation.Content));
        });
    }

    [Test]
    public void ApplicationRemovesAbsentRepresentationFields()
    {
        HttpResponse response = new HttpResponse();
        string[] managed =
        {
            HttpHeaderNames.ContentType,
            HttpHeaderNames.ContentEncoding,
            HttpHeaderNames.ContentLanguage,
            HttpHeaderNames.ContentLength,
            HttpHeaderNames.ContentLocation,
            HttpHeaderNames.ETag,
            HttpHeaderNames.LastModified,
            HttpHeaderNames.AcceptRanges
        };
        foreach (string name in managed)
        {
            AddField(response.Headers, name, name == HttpHeaderNames.LastModified ?
                "Sun, 06 Nov 1994 08:49:37 GMT" : "old");
        }

        RepresentationSemantics.ApplyToResponse(new HttpRepresentation(), response);

        Assert.Multiple(() =>
        {
            foreach (string name in managed)
            {
                Assert.That(Values(response.Headers, name), Is.Empty, name);
            }

            Assert.That(response.Content, Is.Empty);
        });
    }

    [Test]
    public void AcceptRangesNoneExtractsAsNoAdvertisedUnits()
    {
        HttpResponse response = new HttpResponse();
        AddField(response.Headers, HttpHeaderNames.AcceptRanges, "none");

        HttpRepresentation representation = RepresentationSemantics.ExtractFromResponse(response);

        Assert.That(representation.AcceptedRangeUnits, Is.Empty);
    }

    [Test]
    public void AllowedTrailerMetadataParticipatesInExtractionAndIsNormalizedOnApplication()
    {
        HttpResponse response = new HttpResponse();
        AddField(response.Trailers, HttpHeaderNames.ETag, "\"trailer-tag\"");
        AddField(response.Trailers, HttpHeaderNames.ContentLanguage, "en-GB");

        HttpRepresentation extracted = RepresentationSemantics.ExtractFromResponse(response);
        HttpRepresentation replacement = new HttpRepresentation();
        RepresentationSemantics.ApplyToResponse(replacement, response);

        Assert.Multiple(() =>
        {
            Assert.That(extracted.EntityTag!.OpaqueTag,
                Is.EqualTo("trailer-tag"u8.ToArray()));
            Assert.That(extracted.Languages, Is.EqualTo(new[] { "en-GB" }));
            Assert.That(Values(response.Trailers, HttpHeaderNames.ETag), Is.Empty);
            Assert.That(Values(response.Trailers, HttpHeaderNames.ContentLanguage), Is.Empty);
        });
    }

    [Test]
    public void ExtractionRejectsDuplicateSingletonAndMalformedFields()
    {
        HttpResponse duplicate = ResponseWith(HttpHeaderNames.ContentType, "text/plain");
        AddField(duplicate.Headers, HttpHeaderNames.ContentType, "text/html");
        HttpResponse coding = ResponseWith(HttpHeaderNames.ContentEncoding, "bad coding");
        HttpResponse language = ResponseWith(HttpHeaderNames.ContentLanguage, "123");
        HttpResponse length = ResponseWith(HttpHeaderNames.ContentLength, "-1");
        HttpResponse location = ResponseWith(HttpHeaderNames.ContentLocation, "/item#fragment");
        HttpResponse tag = ResponseWith(HttpHeaderNames.ETag, "*");
        HttpResponse modified = ResponseWith(HttpHeaderNames.LastModified, "not-a-date");
        HttpResponse ranges = ResponseWith(HttpHeaderNames.AcceptRanges, "none, bytes");

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpFieldException>(() =>
                RepresentationSemantics.ExtractFromResponse(duplicate));
            Assert.Throws<HttpFieldException>(() =>
                RepresentationSemantics.ExtractFromResponse(coding));
            Assert.Throws<HttpFieldException>(() =>
                RepresentationSemantics.ExtractFromResponse(language));
            Assert.Throws<HttpFieldException>(() =>
                RepresentationSemantics.ExtractFromResponse(length));
            Assert.Throws<HttpFieldException>(() =>
                RepresentationSemantics.ExtractFromResponse(location));
            Assert.Throws<HttpFieldException>(() =>
                RepresentationSemantics.ExtractFromResponse(tag));
            Assert.Throws<HttpFieldException>(() =>
                RepresentationSemantics.ExtractFromResponse(modified));
            Assert.Throws<HttpFieldException>(() =>
                RepresentationSemantics.ExtractFromResponse(ranges));
        });
    }

    [Test]
    public void InvalidApplicationDoesNotPartiallyModifyTheResponse()
    {
        HttpResponse response = ResponseWith(HttpHeaderNames.ContentType, "application/original");
        HttpRepresentation representation = CompleteRepresentation();
        representation.Languages.Add("bad_language");

        Assert.Throws<HttpFieldException>(() =>
            RepresentationSemantics.ApplyToResponse(representation, response));
        Assert.That(Values(response.Headers, HttpHeaderNames.ContentType),
            Is.EqualTo(new[] { "application/original" }));
    }

    static HttpRepresentation CompleteRepresentation()
    {
        HttpRepresentation representation = new HttpRepresentation();
        HttpMediaType mediaType = new HttpMediaType();
        mediaType.Type = "text";
        mediaType.Subtype = "plain";
        HttpParameter charset = new HttpParameter();
        charset.Name = "charset";
        charset.HasValue = true;
        charset.Value = "utf-8"u8.ToArray();
        mediaType.Parameters.Add(charset);
        HttpParameter format = new HttpParameter();
        format.Name = "format";
        format.HasValue = true;
        format.Value = "flowed"u8.ToArray();
        mediaType.Parameters.Add(format);
        representation.MediaType = mediaType;
        representation.Charset = "iso-8859-1";
        representation.ContentCodings.Add("gzip");
        representation.ContentCodings.Add("br");
        representation.Languages.Add("en-US");
        representation.Languages.Add("fr");
        representation.HasContentLength = true;
        representation.ContentLength = 3;
        representation.ContentLocation = "/current";
        HttpEntityTag tag = new HttpEntityTag();
        tag.OpaqueTag = "current"u8.ToArray();
        representation.EntityTag = tag;
        representation.HasLastModified = true;
        representation.LastModified = new DateTimeOffset(
            1994,
            11,
            6,
            8,
            49,
            37,
            TimeSpan.Zero);
        representation.AcceptedRangeUnits.Add("bytes");
        representation.AcceptedRangeUnits.Add("custom-unit");
        representation.Content = new byte[] { 1, 2, 3 };
        return representation;
    }

    static HttpResponse ResponseWith(string name, string value)
    {
        HttpResponse response = new HttpResponse();
        AddField(response.Headers, name, value);
        return response;
    }

    static void AddField(HttpHeaders headers, string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        headers.Fields.Add(field);
    }

    static string[] Values(HttpHeaders headers, string name)
    {
        return headers.Fields
            .Where(field => string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase))
            .Select(field => Encoding.ASCII.GetString(field.Value))
            .ToArray();
    }
}
