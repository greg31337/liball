using System.Text;
using System.Text.Json;

namespace jmap.Tests;

public class ExceptionTests
{
    [Test]
    public void ProtocolExceptionRetainsPathAndStructuredData()
    {
        JsonElement data = Json("{\"member\":\"methodCalls\",\"index\":2}");
        JmapProtocolException exception = new JmapProtocolException("Invalid invocation.", "/methodCalls/2", data);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.Message, Is.EqualTo("Invalid invocation."));
            Assert.That(exception.Path, Is.EqualTo("/methodCalls/2"));
            Assert.That(exception.StructuredData.GetProperty("member").GetString(), Is.EqualTo("methodCalls"));
            Assert.That(exception.StructuredData.GetProperty("index").GetInt32(), Is.EqualTo(2));
        }
    }

    [Test]
    public void ValidationExceptionRetainsEveryInvalidPropertyAndInputData()
    {
        List<string> invalidProperties = new List<string> { "mailboxIds", "keywords/$Draft" };
        JsonElement data = Json("{\"mailboxIds\":null,\"keywords/$Draft\":true}");
        JmapValidationException exception = new JmapValidationException("The email is invalid.", invalidProperties, data);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.Message, Is.EqualTo("The email is invalid."));
            Assert.That(exception.InvalidProperties, Is.SameAs(invalidProperties));
            Assert.That(exception.InvalidProperties, Is.EqualTo(new[] { "mailboxIds", "keywords/$Draft" }));
            Assert.That(exception.StructuredData.GetProperty("mailboxIds").ValueKind, Is.EqualTo(JsonValueKind.Null));
        }
    }

    [Test]
    public void TransportExceptionRetainsHttpResponseRetryAndUnderlyingFailure()
    {
        byte[] responseBody = Encoding.UTF8.GetBytes("temporarily unavailable");
        InvalidOperationException cause = new InvalidOperationException("connection closed");
        JmapTransportException exception = new JmapTransportException(
            "The API request failed.",
            "https://example.test/jmap/api",
            503,
            true,
            true,
            responseBody,
            cause);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.Message, Is.EqualTo("The API request failed."));
            Assert.That(exception.Endpoint, Is.EqualTo("https://example.test/jmap/api"));
            Assert.That(exception.HttpStatus, Is.EqualTo(503));
            Assert.That(exception.HasHttpStatus, Is.True);
            Assert.That(exception.IsRetryable, Is.True);
            Assert.That(exception.ResponseBody, Is.SameAs(responseBody));
            Assert.That(exception.InnerException, Is.SameAs(cause));
        }
    }

    [Test]
    public void RequestExceptionRetainsProblemRawResponseAndClassification()
    {
        JmapProblem problem = new JmapProblem();
        problem.Type = JmapErrorCodes.LimitProblemType;
        problem.Kind = JmapProblemKind.Limit;
        problem.Status = 400;
        problem.HasStatus = true;
        problem.Limit = JmapProperties.MaxSizeRequest;
        problem.UnknownProperties.Add("vendorLimit", Json("17"));
        JsonElement data = Json("{\"type\":\"urn:ietf:params:jmap:error:limit\",\"limit\":\"maxSizeRequest\",\"vendorLimit\":17}");
        byte[] responseBody = Encoding.UTF8.GetBytes(data.GetRawText());

        JmapRequestException exception = new JmapRequestException(
            "The request exceeded a server limit.",
            400,
            JmapErrorCodes.LimitProblemType,
            problem,
            data,
            responseBody,
            false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.HttpStatus, Is.EqualTo(400));
            Assert.That(exception.ErrorToken, Is.EqualTo(JmapErrorCodes.LimitProblemType));
            Assert.That(exception.Problem, Is.SameAs(problem));
            Assert.That(problem.UnknownProperties["vendorLimit"].GetInt32(), Is.EqualTo(17));
            Assert.That(exception.StructuredData.GetProperty("vendorLimit").GetInt32(), Is.EqualTo(17));
            Assert.That(exception.ResponseBody, Is.SameAs(responseBody));
            Assert.That(exception.IsRetryable, Is.False);
        }
    }

    [Test]
    public void MethodExceptionRetainsCallIdentityExactErrorAndClassification()
    {
        JmapMethodError error = new JmapMethodError();
        error.Type = "vendorTemporarilyUnavailable";
        error.Kind = JmapMethodErrorKind.Unknown;
        error.Description = "Try again later.";
        error.UnknownProperties.Add("retryAfter", Json("30"));
        JsonElement data = Json("{\"type\":\"vendorTemporarilyUnavailable\",\"retryAfter\":30}");

        JmapMethodException exception = new JmapMethodException(
            "The method failed.",
            JmapMethods.EmailGet,
            "call-7",
            "vendorTemporarilyUnavailable",
            error,
            data,
            true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.MethodName, Is.EqualTo(JmapMethods.EmailGet));
            Assert.That(exception.CallId, Is.EqualTo("call-7"));
            Assert.That(exception.ErrorToken, Is.EqualTo("vendorTemporarilyUnavailable"));
            Assert.That(exception.Error, Is.SameAs(error));
            Assert.That(error.UnknownProperties["retryAfter"].GetInt32(), Is.EqualTo(30));
            Assert.That(exception.StructuredData.GetProperty("retryAfter").GetInt32(), Is.EqualTo(30));
            Assert.That(exception.IsRetryable, Is.True);
        }
    }

    [Test]
    public void CapabilityExceptionRetainsCapabilityMethodAndAccountContext()
    {
        JmapCapabilityException exception = new JmapCapabilityException(
            "The account does not support mail.",
            JmapCapabilities.Mail,
            JmapMethods.EmailGet,
            "account-3");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.Message, Is.EqualTo("The account does not support mail."));
            Assert.That(exception.Capability, Is.EqualTo(JmapCapabilities.Mail));
            Assert.That(exception.MethodName, Is.EqualTo(JmapMethods.EmailGet));
            Assert.That(exception.AccountId, Is.EqualTo("account-3"));
        }
    }

    [Test]
    public void StateMismatchExceptionRetainsCallStatesAndStructuredError()
    {
        JmapMethodError error = new JmapMethodError();
        error.Type = JmapErrorCodes.StateMismatch;
        error.Kind = JmapMethodErrorKind.StateMismatch;
        JsonElement data = Json("{\"type\":\"stateMismatch\",\"newState\":\"state-9\"}");

        JmapStateMismatchException exception = new JmapStateMismatchException(
            "The account state changed.",
            JmapMethods.EmailSet,
            "call-9",
            "account-1",
            "state-8",
            "state-9",
            error,
            data,
            false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.MethodName, Is.EqualTo(JmapMethods.EmailSet));
            Assert.That(exception.CallId, Is.EqualTo("call-9"));
            Assert.That(exception.AccountId, Is.EqualTo("account-1"));
            Assert.That(exception.ExpectedState, Is.EqualTo("state-8"));
            Assert.That(exception.ActualState, Is.EqualTo("state-9"));
            Assert.That(exception.Error, Is.SameAs(error));
            Assert.That(exception.StructuredData.GetProperty("newState").GetString(), Is.EqualTo("state-9"));
            Assert.That(exception.IsRetryable, Is.False);
        }
    }

    private static JsonElement Json(string value)
    {
        using JsonDocument document = JsonDocument.Parse(value);
        return document.RootElement.Clone();
    }
}
