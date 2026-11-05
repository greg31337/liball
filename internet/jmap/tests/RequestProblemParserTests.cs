using System.Text;
using System.Text.Json;

namespace jmap.Tests;

public class RequestProblemParserTests
{
    [Test]
    public void ProblemResponseIsClassifiedAndPreservesRawAndExtensionData()
    {
        byte[] body = Encoding.UTF8.GetBytes(
            "{\"type\":\"urn:ietf:params:jmap:error:limit\",\"status\":400," +
            "\"detail\":\"Too large.\",\"limit\":\"maxSizeRequest\",\"vendorMaximum\":17}");

        JmapRequestException exception = new RequestProblemParser().ParseProblemResponse(
            400,
            " Application/Problem+Json ; charset=utf-8",
            body);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.Message, Is.EqualTo("Too large."));
            Assert.That(exception.HttpStatus, Is.EqualTo(400));
            Assert.That(exception.ErrorToken, Is.EqualTo(JmapErrorCodes.LimitProblemType));
            Assert.That(exception.Problem, Is.Not.Null);
            Assert.That(exception.Problem!.Kind, Is.EqualTo(JmapProblemKind.Limit));
            Assert.That(exception.Problem.Limit, Is.EqualTo(JmapProperties.MaxSizeRequest));
            Assert.That(exception.StructuredData.GetProperty("vendorMaximum").GetInt32(), Is.EqualTo(17));
            Assert.That(exception.ResponseBody, Is.SameAs(body));
            Assert.That(exception.IsRetryable, Is.False);
        }
    }

    [Test]
    public void ActualHttpStatusControlsTheExceptionAndRetryClassification()
    {
        byte[] body = Encoding.UTF8.GetBytes(
            "{\"type\":\"urn:ietf:params:jmap:error:notRequest\",\"title\":\"Bad request\",\"status\":400}");

        JmapRequestException exception = new RequestProblemParser().ParseProblemResponse(
            503,
            JmapMediaTypes.ProblemJson,
            body);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.Message, Is.EqualTo("Bad request"));
            Assert.That(exception.HttpStatus, Is.EqualTo(503));
            Assert.That(exception.Problem!.Status, Is.EqualTo(400));
            Assert.That(exception.IsRetryable, Is.True);
        }
    }

    [Test]
    public void NonProblemMediaOrMissingBodyProducesAnOpaqueRequestException()
    {
        RequestProblemParser parser = new RequestProblemParser();
        byte[] body = Encoding.UTF8.GetBytes("not problem json");

        JmapRequestException wrongMedia = parser.ParseProblemResponse(400, JmapMediaTypes.Json, body);
        JmapRequestException missingBody = parser.ParseProblemResponse(429, JmapMediaTypes.ProblemJson, null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(wrongMedia.Problem, Is.Null);
            Assert.That(wrongMedia.ErrorToken, Is.Null);
            Assert.That(wrongMedia.StructuredData.ValueKind, Is.EqualTo(JsonValueKind.Undefined));
            Assert.That(wrongMedia.ResponseBody, Is.SameAs(body));
            Assert.That(wrongMedia.IsRetryable, Is.False);
            Assert.That(missingBody.Problem, Is.Null);
            Assert.That(missingBody.ResponseBody, Is.Null);
            Assert.That(missingBody.IsRetryable, Is.True);
        }
    }

    [Test]
    public void UnknownProblemTypeIsPreservedWithoutLosingItsBody()
    {
        byte[] body = Encoding.UTF8.GetBytes(
            "{\"type\":\"https://example.test/problems/vendor\",\"status\":409,\"vendorCode\":\"V7\"}");

        JmapRequestException exception = new RequestProblemParser().ParseProblemResponse(
            409,
            JmapMediaTypes.ProblemJson,
            body);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.ErrorToken, Is.EqualTo("https://example.test/problems/vendor"));
            Assert.That(exception.Problem!.Kind, Is.EqualTo(JmapProblemKind.Unknown));
            Assert.That(exception.Problem.UnknownProperties["vendorCode"].GetString(), Is.EqualTo("V7"));
            Assert.That(exception.StructuredData.GetProperty("vendorCode").GetString(), Is.EqualTo("V7"));
        }
    }

    [Test]
    public void MalformedStandardProblemMetadataIsRejected()
    {
        RequestProblemParser parser = new RequestProblemParser();
        byte[] missingLimit = Encoding.UTF8.GetBytes(
            "{\"type\":\"urn:ietf:params:jmap:error:limit\",\"status\":400}");
        byte[] invalidStatus = Encoding.UTF8.GetBytes(
            "{\"type\":\"urn:ietf:params:jmap:error:notJSON\",\"status\":99}");

        JmapProtocolException limitError = Assert.Throws<JmapProtocolException>(
            () => parser.ParseProblemResponse(400, JmapMediaTypes.ProblemJson, missingLimit))!;
        JmapProtocolException statusError = Assert.Throws<JmapProtocolException>(
            () => parser.ParseProblemResponse(400, JmapMediaTypes.ProblemJson, invalidStatus))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(limitError.Path, Is.EqualTo("limit"));
            Assert.That(statusError.Path, Is.EqualTo("status"));
        }
    }
}
