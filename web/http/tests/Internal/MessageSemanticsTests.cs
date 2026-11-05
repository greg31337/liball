using System.Text;

namespace http.Tests;

public class MessageSemanticsTests
{
    [Test]
    public void HeadResponsesNeverAllowContent()
    {
        HttpRequest request = Request(HttpMethods.Head);

        Assert.Multiple(() =>
        {
            Assert.That(MessageSemantics.AllowsResponseContent(request, Response(200)), Is.False);
            Assert.That(MessageSemantics.AllowsResponseContent(request, Response(404)), Is.False);
            Assert.That(MessageSemantics.AllowsResponseContent(request, Response(699)), Is.False);
        });
    }

    [Test]
    public void SuccessfulConnectResponsesBecomeContentlessTunnels()
    {
        HttpRequest request = Request(HttpMethods.Connect);

        Assert.Multiple(() =>
        {
            Assert.That(MessageSemantics.AllowsResponseContent(request, Response(200)), Is.False);
            Assert.That(MessageSemantics.AllowsResponseContent(request, Response(299)), Is.False);
            Assert.That(MessageSemantics.AllowsResponseContent(request, Response(407)), Is.True);
            Assert.That(MessageSemantics.AllowsResponseContent(request, Response(500)), Is.True);
        });
    }

    [Test]
    public void StatusRulesForbidContentForInformational204205And304Responses()
    {
        HttpRequest request = Request(HttpMethods.Get);

        Assert.Multiple(() =>
        {
            Assert.That(MessageSemantics.AllowsResponseContent(request, Informational(100)), Is.False);
            Assert.That(MessageSemantics.AllowsResponseContent(request, Informational(150)), Is.False);
            Assert.That(MessageSemantics.AllowsResponseContent(request, Response(204)), Is.False);
            Assert.That(MessageSemantics.AllowsResponseContent(request, Response(205)), Is.False);
            Assert.That(MessageSemantics.AllowsResponseContent(request, Response(304)), Is.False);
            Assert.That(MessageSemantics.AllowsResponseContent(request, Response(200)), Is.True);
            Assert.That(MessageSemantics.AllowsResponseContent(request, Response(206)), Is.True);
        });
    }

    [Test]
    public void InformationalFlagMustMatchTheStatusClass()
    {
        HttpResponse missingFlag = Response(103);
        HttpResponse extraFlag = Response(200);
        extraFlag.IsInformational = true;
        HttpResponse informational = Informational(103);

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpProtocolException>(() =>
                MessageSemantics.IsInformationalResponse(missingFlag));
            Assert.Throws<HttpProtocolException>(() =>
                MessageSemantics.IsInformationalResponse(extraFlag));
            Assert.That(MessageSemantics.IsInformationalResponse(informational), Is.True);
            Assert.That(MessageSemantics.IsInformationalResponse(Response(200)), Is.False);
        });
    }

    [Test]
    public void InformationalAndContentlessResponsesRejectContentAndTrailers()
    {
        HttpRequest request = Request(HttpMethods.Get);
        HttpResponse informationalContent = Informational(103);
        informationalContent.Content = [1];
        HttpResponse noContentTrailer = Response(204);
        AddTrailer(noContentTrailer.Trailers, "Digest", "value");
        HttpResponse headContent = Response(200);
        headContent.Content = [1];

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpProtocolException>(() =>
                MessageSemantics.IsResponseComplete(request, informationalContent));
            Assert.Throws<HttpProtocolException>(() =>
                MessageSemantics.IsResponseComplete(request, noContentTrailer));
            Assert.Throws<HttpProtocolException>(() =>
                MessageSemantics.IsResponseComplete(Request(HttpMethods.Head), headContent));
        });
    }

    [Test]
    public void FullyFramedPartialResponseIsACompleteMessage()
    {
        HttpResponse response = Response(206);
        response.Content = [2, 3, 4];
        response.IsComplete = true;

        Assert.Multiple(() =>
        {
            Assert.That(response.Content, Has.Length.EqualTo(3));
            Assert.That(MessageSemantics.IsResponseComplete(Request(HttpMethods.Get), response),
                Is.True);
        });
    }

    [Test]
    public void CompletenessFlagDistinguishesIncompleteAndCompleteMessages()
    {
        HttpRequest request = Request(HttpMethods.Post);
        request.Content = [1, 2];
        HttpResponse response = Response(200);
        response.Content = [3, 4];

        Assert.Multiple(() =>
        {
            Assert.That(MessageSemantics.IsRequestComplete(request), Is.False);
            Assert.That(MessageSemantics.IsResponseComplete(request, response), Is.False);
        });

        request.IsComplete = true;
        response.IsComplete = true;

        Assert.Multiple(() =>
        {
            Assert.That(MessageSemantics.IsRequestComplete(request), Is.True);
            Assert.That(MessageSemantics.IsResponseComplete(request, response), Is.True);
        });
    }

    [Test]
    public void ValidTrailersArePartOfACompleteMessage()
    {
        HttpRequest request = Request(HttpMethods.Post);
        request.IsComplete = true;
        AddTrailer(request.Trailers, "Digest", "request-digest");
        HttpResponse response = Response(200);
        response.IsComplete = true;
        AddTrailer(response.Trailers, "X-Checksum", "response-checksum");

        Assert.Multiple(() =>
        {
            Assert.That(MessageSemantics.IsRequestComplete(request), Is.True);
            Assert.That(MessageSemantics.IsResponseComplete(request, response), Is.True);
        });
    }

    [Test]
    public void ParsedTrailersRemainIncompleteUntilTerminatedAndRejectForbiddenFields()
    {
        HttpRequest request = Request(HttpMethods.Post);
        AddTrailer(request.Trailers, "Digest", "value");
        HttpResponse response = Response(200);
        AddTrailer(response.Trailers, "Digest", "value");
        HttpResponse forbidden = Response(200);
        forbidden.IsComplete = true;
        AddTrailer(forbidden.Trailers, HttpHeaderNames.ContentLength, "1");

        Assert.Multiple(() =>
        {
            Assert.That(MessageSemantics.IsRequestComplete(request), Is.False);
            Assert.That(MessageSemantics.IsResponseComplete(request, response), Is.False);
            Assert.Throws<HttpProtocolException>(() =>
                MessageSemantics.IsResponseComplete(Request(HttpMethods.Get), forbidden));
        });
    }

    [Test]
    public void ConnectAndTraceRequestsRejectContentAndTrailers()
    {
        HttpRequest connect = Request(HttpMethods.Connect);
        connect.Content = [1];
        connect.IsComplete = true;
        HttpRequest trace = Request(HttpMethods.Trace);
        trace.IsComplete = true;
        AddTrailer(trace.Trailers, "Digest", "value");

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpProtocolException>(() =>
                MessageSemantics.IsRequestComplete(connect));
            Assert.Throws<HttpProtocolException>(() =>
                MessageSemantics.IsRequestComplete(trace));
        });
    }

    static HttpRequest Request(string method)
    {
        HttpRequest request = new HttpRequest();
        request.Method = method;
        return request;
    }

    static HttpResponse Response(int statusCode)
    {
        HttpResponse response = new HttpResponse();
        response.StatusCode = statusCode;
        return response;
    }

    static HttpResponse Informational(int statusCode)
    {
        HttpResponse response = Response(statusCode);
        response.IsInformational = true;
        response.IsComplete = true;
        return response;
    }

    static void AddTrailer(HttpHeaders trailers, string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        trailers.Fields.Add(field);
    }
}
