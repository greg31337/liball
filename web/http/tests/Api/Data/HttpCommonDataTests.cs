using System.Text;

namespace http.Tests;

public class HttpCommonDataTests
{
    [Test]
    public void HeaderModelRetainsOrderDuplicatesAndRawValues()
    {
        HttpHeaderField first = new HttpHeaderField();
        HttpHeaderField second = new HttpHeaderField();
        HttpHeaders headers = new HttpHeaders();
        byte[] rawValue = [0x61, 0x80, 0xff];

        first.Name = "X-Original";
        first.Value = rawValue;
        first.IsSensitive = true;
        first.NeverIndex = true;
        second.Name = "x-original";
        second.Value = [0x62];
        headers.Fields.Add(first);
        headers.Fields.Add(second);

        Assert.Multiple(() =>
        {
            Assert.That(headers.Fields, Is.EqualTo(new[] { first, second }));
            Assert.That(headers.Fields[0].Name, Is.EqualTo("X-Original"));
            Assert.That(headers.Fields[0].Value, Is.SameAs(rawValue));
            Assert.That(headers.Fields[0].Value, Is.EqualTo(new byte[] { 0x61, 0x80, 0xff }));
            Assert.That(headers.Fields[0].IsSensitive, Is.True);
            Assert.That(headers.Fields[0].NeverIndex, Is.True);
        });
    }

    [Test]
    public void RequestAndResponseRetainControlDataAndOwnedMessageParts()
    {
        HttpRequest request = new HttpRequest();
        HttpResponse response = new HttpResponse();
        HttpHeaderField trailer = new HttpHeaderField();

        request.ProtocolVersion = HttpProtocolVersion.Http2;
        request.Method = "CONNECT";
        request.Target = "example.com:443";
        request.TargetForm = HttpRequestTargetForm.Authority;
        request.Scheme = "https";
        request.Authority = "example.com:443";
        request.Path = "/chat";
        request.HasQuery = true;
        request.Query = "";
        request.ConnectProtocol = "websocket";
        request.Content = [1, 2, 3];
        request.IsComplete = true;
        request.Headers.Fields.Add(new HttpHeaderField());

        trailer.Name = "Digest";
        response.ProtocolVersion = HttpProtocolVersion.Http11;
        response.StatusCode = 200;
        response.HasReasonPhrase = true;
        response.ReasonPhrase = [0x4f, 0x4b, 0x80];
        response.Content = [4, 5];
        response.Trailers.Fields.Add(trailer);
        response.IsInformational = false;
        response.IsComplete = true;

        Assert.Multiple(() =>
        {
            Assert.That(request.Method, Is.EqualTo("CONNECT"));
            Assert.That(request.TargetForm, Is.EqualTo(HttpRequestTargetForm.Authority));
            Assert.That(request.Authority, Is.EqualTo("example.com:443"));
            Assert.That(request.HasQuery, Is.True);
            Assert.That(request.Query, Is.Empty);
            Assert.That(request.ConnectProtocol, Is.EqualTo("websocket"));
            Assert.That(request.Headers.Fields, Has.Count.EqualTo(1));
            Assert.That(request.Content, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(request.IsComplete, Is.True);
            Assert.That(response.StatusCode, Is.EqualTo(200));
            Assert.That(response.HasReasonPhrase, Is.True);
            Assert.That(response.ReasonPhrase, Is.EqualTo(new byte[] { 0x4f, 0x4b, 0x80 }));
            Assert.That(response.Trailers.Fields.Single(), Is.SameAs(trailer));
            Assert.That(response.IsComplete, Is.True);
        });
    }

    [Test]
    public void RepresentationRetainsMetadataPresenceAndContent()
    {
        HttpRepresentation representation = new HttpRepresentation();
        HttpMediaType mediaType = new HttpMediaType();
        HttpEntityTag entityTag = new HttpEntityTag();
        HttpContentRange contentRange = new HttpContentRange();
        DateTimeOffset modified = new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero);

        mediaType.Type = "text";
        mediaType.Subtype = "plain";
        entityTag.OpaqueTag = Encoding.ASCII.GetBytes("version-1");
        contentRange.Unit = "bytes";
        contentRange.HasStart = true;
        contentRange.Start = 0;
        contentRange.HasEnd = true;
        contentRange.End = 4;
        contentRange.HasCompleteLength = true;
        contentRange.CompleteLength = 10;
        representation.MediaType = mediaType;
        representation.Charset = "utf-8";
        representation.ContentCodings.Add("gzip");
        representation.Languages.Add("en-US");
        representation.HasContentLength = true;
        representation.ContentLength = 5;
        representation.ContentLocation = "/document";
        representation.EntityTag = entityTag;
        representation.HasLastModified = true;
        representation.LastModified = modified;
        representation.AcceptedRangeUnits.Add("bytes");
        representation.ContentRange = contentRange;
        representation.Content = [1, 2, 3, 4, 5];

        Assert.Multiple(() =>
        {
            Assert.That(representation.MediaType, Is.SameAs(mediaType));
            Assert.That(representation.ContentCodings, Is.EqualTo(new[] { "gzip" }));
            Assert.That(representation.Languages, Is.EqualTo(new[] { "en-US" }));
            Assert.That(representation.HasContentLength, Is.True);
            Assert.That(representation.ContentLength, Is.EqualTo(5));
            Assert.That(representation.EntityTag, Is.SameAs(entityTag));
            Assert.That(representation.HasLastModified, Is.True);
            Assert.That(representation.LastModified, Is.EqualTo(modified));
            Assert.That(representation.ContentRange, Is.SameAs(contentRange));
            Assert.That(representation.Content, Has.Length.EqualTo(5));
        });
    }

    [Test]
    public void MethodAndStatusMetadataRetainSemanticCharacteristics()
    {
        HttpMethodMetadata method = new HttpMethodMetadata();
        HttpStatusMetadata status = new HttpStatusMetadata();

        method.Method = "GET";
        method.IsRegistered = true;
        method.IsCore = true;
        method.IsSafe = true;
        method.IsIdempotent = true;
        method.IsCacheable = true;
        method.HasDefinedRequestContentSemantics = false;
        status.StatusCode = 503;
        status.StatusClass = HttpStatusClass.ServerError;
        status.IsRegistered = true;
        status.ReasonPhrase = "Service Unavailable";
        status.AllowsContent = true;
        status.IsCacheableByDefault = false;
        status.IsRedirect = false;
        status.RedirectsPostToGet = false;
        status.IsRetryable = true;

        Assert.Multiple(() =>
        {
            Assert.That(method.Method, Is.EqualTo("GET"));
            Assert.That(method.IsRegistered, Is.True);
            Assert.That(method.IsCore, Is.True);
            Assert.That(method.IsSafe, Is.True);
            Assert.That(method.IsIdempotent, Is.True);
            Assert.That(method.IsCacheable, Is.True);
            Assert.That(method.HasDefinedRequestContentSemantics, Is.False);
            Assert.That(status.StatusCode, Is.EqualTo(503));
            Assert.That(status.StatusClass, Is.EqualTo(HttpStatusClass.ServerError));
            Assert.That(status.ReasonPhrase, Is.EqualTo("Service Unavailable"));
            Assert.That(status.AllowsContent, Is.True);
            Assert.That(status.IsRetryable, Is.True);
        });
    }

    [Test]
    public void SelectionValueTypesRetainParametersQualityAndSpecificity()
    {
        HttpParameter parameter = new HttpParameter();
        HttpMediaType media = new HttpMediaType();
        HttpWeightedValue weighted = new HttpWeightedValue();
        HttpLanguageRange language = new HttpLanguageRange();
        HttpEntityTag entityTag = new HttpEntityTag();

        parameter.Name = "level";
        parameter.Value = Encoding.ASCII.GetBytes("1");
        parameter.IsQuoted = true;
        media.Type = "text";
        media.Subtype = "*";
        media.IsSubtypeWildcard = true;
        media.Parameters.Add(parameter);
        weighted.Value = "gzip";
        weighted.Parameters.Add(parameter);
        weighted.Quality = 500;
        weighted.SourceOrder = 2;
        weighted.Specificity = 3;
        language.Range = "en-US";
        language.Quality = 750;
        language.SourceOrder = 1;
        language.Specificity = 2;
        entityTag.OpaqueTag = [0x76, 0x31, 0x80];
        entityTag.IsWeak = true;

        Assert.Multiple(() =>
        {
            Assert.That(media.Parameters.Single(), Is.SameAs(parameter));
            Assert.That(media.IsSubtypeWildcard, Is.True);
            Assert.That(weighted.Quality, Is.EqualTo(500));
            Assert.That(weighted.SourceOrder, Is.EqualTo(2));
            Assert.That(weighted.Specificity, Is.EqualTo(3));
            Assert.That(language.Range, Is.EqualTo("en-US"));
            Assert.That(language.Quality, Is.EqualTo(750));
            Assert.That(language.Specificity, Is.EqualTo(2));
            Assert.That(entityTag.OpaqueTag, Is.EqualTo(new byte[] { 0x76, 0x31, 0x80 }));
            Assert.That(entityTag.IsWeak, Is.True);
        });
    }

    [Test]
    public void RangeAuthenticationViaAndPriorityRetainExtensionData()
    {
        HttpByteRange range = new HttpByteRange();
        HttpAuthenticationValue authentication = new HttpAuthenticationValue();
        HttpViaEntry via = new HttpViaEntry();
        HttpPriority priority = new HttpPriority();
        StructuredFieldMember extension = new StructuredFieldMember();

        range.HasStart = true;
        range.Start = 10;
        range.HasEnd = true;
        range.End = 19;
        range.IsNormalized = true;
        authentication.Scheme = "Bearer";
        authentication.Token68 = Encoding.ASCII.GetBytes("abc==");
        via.ReceivedProtocol = "HTTP/1.1";
        via.ReceivedBy = "proxy.example";
        via.HasPort = true;
        via.Port = 8080;
        via.Comment = Encoding.ASCII.GetBytes("edge");
        priority.HasUrgency = true;
        priority.Urgency = 1;
        priority.HasIncremental = true;
        priority.Incremental = true;
        extension.ValueKind = HttpFieldValueKind.String;
        priority.UnknownParameters.Add(extension);

        Assert.Multiple(() =>
        {
            Assert.That(range.HasStart, Is.True);
            Assert.That(range.Start, Is.EqualTo(10));
            Assert.That(range.End, Is.EqualTo(19));
            Assert.That(range.IsNormalized, Is.True);
            Assert.That(authentication.Scheme, Is.EqualTo("Bearer"));
            Assert.That(authentication.Token68, Is.EqualTo(Encoding.ASCII.GetBytes("abc==")));
            Assert.That(via.ReceivedProtocol, Is.EqualTo("HTTP/1.1"));
            Assert.That(via.HasPort, Is.True);
            Assert.That(via.Port, Is.EqualTo(8080));
            Assert.That(via.Comment, Is.EqualTo(Encoding.ASCII.GetBytes("edge")));
            Assert.That(priority.HasUrgency, Is.True);
            Assert.That(priority.Urgency, Is.EqualTo(1));
            Assert.That(priority.Incremental, Is.True);
            Assert.That(priority.UnknownParameters.Single(), Is.SameAs(extension));
        });
    }

    [Test]
    public void CommonDataDefaultsRepresentAbsentValuesAndProtocolDefaults()
    {
        HttpRequest request = new HttpRequest();
        HttpResponse response = new HttpResponse();
        HttpRepresentation representation = new HttpRepresentation();
        HttpWeightedValue weighted = new HttpWeightedValue();
        HttpLanguageRange language = new HttpLanguageRange();
        HttpPriority priority = new HttpPriority();
        HttpContentRange contentRange = new HttpContentRange();

        Assert.Multiple(() =>
        {
            Assert.That(request.Headers.Fields, Is.Empty);
            Assert.That(request.Content, Is.Empty);
            Assert.That(request.Trailers.Fields, Is.Empty);
            Assert.That(request.HasQuery, Is.False);
            Assert.That(response.HasReasonPhrase, Is.False);
            Assert.That(response.ReasonPhrase, Is.Empty);
            Assert.That(response.Headers.Fields, Is.Empty);
            Assert.That(response.Trailers.Fields, Is.Empty);
            Assert.That(representation.HasContentLength, Is.False);
            Assert.That(representation.HasLastModified, Is.False);
            Assert.That(representation.ContentCodings, Is.Empty);
            Assert.That(weighted.Quality, Is.EqualTo(1000));
            Assert.That(language.Quality, Is.EqualTo(1000));
            Assert.That(priority.HasUrgency, Is.False);
            Assert.That(priority.Urgency, Is.EqualTo(3));
            Assert.That(priority.HasIncremental, Is.False);
            Assert.That(priority.Incremental, Is.False);
            Assert.That(contentRange.HasCompleteLength, Is.False);
            Assert.That(contentRange.IsUnsatisfied, Is.False);
        });
    }
}
