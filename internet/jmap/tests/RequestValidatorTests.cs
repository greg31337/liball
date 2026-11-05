using System.Text.Json;

namespace jmap.Tests;

public class RequestValidatorTests
{
    [Test]
    public void ValidRequestAcceptsEarlierReferencesAndIgnoresUnknownTopLevelMembers()
    {
        RequestValidator validator = Validator(10, 1000L);
        JmapRequest request = RequestWithCapabilities(JmapCapabilities.Core, JmapCapabilities.Mail);
        request.MethodCalls.Add(Invocation(JmapMethods.EmailQuery, "c1", "{}"));
        request.MethodCalls.Add(Invocation(
            JmapMethods.EmailGet,
            "c2",
            "{\"#ids\":{\"resultOf\":\"c1\",\"name\":\"Email/query\",\"path\":\"/ids\"}}"));
        request.CreatedIds = new Dictionary<string, string> { { "draft", "M1" } };
        request.UnknownProperties.Add("future", Json("{\"enabled\":true}"));

        RequestValidationResult result = validator.ValidateRequest(request, 1000L);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.ProblemKind, Is.EqualTo(JmapProblemKind.Unknown));
            Assert.That(result.MethodErrors, Is.Empty);
            Assert.That(result.InvalidPaths, Is.Empty);
        }
    }

    [Test]
    public void UnsupportedCapabilityRejectsTheWholeRequest()
    {
        RequestValidator validator = Validator(10, 1000L);
        JmapRequest request = RequestWithCapabilities(JmapCapabilities.Core, "https://example.test/unsupported");

        RequestValidationResult result = validator.ValidateRequest(request, 10L);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ProblemKind, Is.EqualTo(JmapProblemKind.UnknownCapability));
            Assert.That(result.InvalidPaths, Is.EqualTo(new[] { "using/1" }));
            Assert.That(result.Detail, Does.Contain("https://example.test/unsupported"));
        }
    }

    [Test]
    public void DuplicateCapabilitiesAreHarmlessButMalformedCapabilitiesAreRejected()
    {
        RequestValidator validator = Validator(10, 1000L);
        JmapRequest duplicate = RequestWithCapabilities(JmapCapabilities.Core, JmapCapabilities.Core);
        JmapRequest malformed = RequestWithCapabilities("relative-capability");

        RequestValidationResult duplicateResult = validator.ValidateRequest(duplicate, 10L);
        RequestValidationResult malformedResult = validator.ValidateRequest(malformed, 10L);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(duplicateResult.IsValid, Is.True);
            Assert.That(duplicateResult.InvalidPaths, Is.Empty);
            Assert.That(malformedResult.ProblemKind, Is.EqualTo(JmapProblemKind.NotRequest));
            Assert.That(malformedResult.InvalidPaths, Is.EqualTo(new[] { "using/0" }));
        }
    }

    [Test]
    public void SizeAndCallLimitsAcceptTheBoundaryAndRejectOnlyExcess()
    {
        RequestValidator validator = Validator(2, 100L);
        JmapRequest request = RequestWithCapabilities(JmapCapabilities.Core);
        request.MethodCalls.Add(Invocation(JmapMethods.CoreEcho, "c1", "{}"));
        request.MethodCalls.Add(Invocation(JmapMethods.CoreEcho, "c2", "{}"));

        RequestValidationResult boundary = validator.ValidateRequest(request, 100L);
        RequestValidationResult tooLarge = validator.ValidateRequest(request, 101L);
        request.MethodCalls.Add(Invocation(JmapMethods.CoreEcho, "c3", "{}"));
        RequestValidationResult tooManyCalls = validator.ValidateRequest(request, 100L);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(boundary.IsValid, Is.True);
            Assert.That(tooLarge.ProblemKind, Is.EqualTo(JmapProblemKind.Limit));
            Assert.That(tooLarge.Limit, Is.EqualTo(JmapProperties.MaxSizeRequest));
            Assert.That(tooManyCalls.ProblemKind, Is.EqualTo(JmapProblemKind.Limit));
            Assert.That(tooManyCalls.Limit, Is.EqualTo(JmapProperties.MaxCallsInRequest));
        }
    }

    [Test]
    public void InvocationShapeAndCallIdentifiersAreValidatedBeforeExecution()
    {
        RequestValidator validator = Validator(10, 1000L);
        JmapRequest badName = RequestWithCapabilities(JmapCapabilities.Core);
        badName.MethodCalls.Add(Invocation("bad", "c1", "{}"));
        JmapRequest badArguments = RequestWithCapabilities(JmapCapabilities.Core);
        badArguments.MethodCalls.Add(Invocation(JmapMethods.CoreEcho, "c1", "[]"));
        JmapRequest emptyCallId = RequestWithCapabilities(JmapCapabilities.Core);
        emptyCallId.MethodCalls.Add(Invocation(JmapMethods.CoreEcho, "", "{}"));
        JmapRequest duplicateCallId = RequestWithCapabilities(JmapCapabilities.Core);
        duplicateCallId.MethodCalls.Add(Invocation(JmapMethods.CoreEcho, "same", "{}"));
        duplicateCallId.MethodCalls.Add(Invocation(JmapMethods.CoreEcho, "same", "{}"));

        RequestValidationResult badNameResult = validator.ValidateRequest(badName, 10L);
        RequestValidationResult badArgumentsResult = validator.ValidateRequest(badArguments, 10L);
        RequestValidationResult emptyCallIdResult = validator.ValidateRequest(emptyCallId, 10L);
        RequestValidationResult duplicateCallIdResult = validator.ValidateRequest(duplicateCallId, 10L);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(badNameResult.InvalidPaths, Is.EqualTo(new[] { "methodCalls/0/0" }));
            Assert.That(badArgumentsResult.InvalidPaths, Is.EqualTo(new[] { "methodCalls/0/1" }));
            Assert.That(emptyCallIdResult.InvalidPaths, Is.EqualTo(new[] { "methodCalls/0/2" }));
            Assert.That(duplicateCallIdResult.InvalidPaths, Is.EqualTo(new[] { "methodCalls/1/2" }));
            Assert.That(duplicateCallIdResult.ProblemKind, Is.EqualTo(JmapProblemKind.NotRequest));
        }
    }

    [Test]
    public void InvalidRequiredCollectionsAndCreatedIdMappingsAreRejected()
    {
        RequestValidator validator = Validator(10, 1000L);
        JmapRequest missingUsing = RequestWithCapabilities();
        missingUsing.Using = null!;
        JmapRequest missingCalls = RequestWithCapabilities();
        missingCalls.MethodCalls = null!;
        JmapRequest invalidCreatedId = RequestWithCapabilities();
        invalidCreatedId.CreatedIds = new Dictionary<string, string> { { "#draft", "M1" } };
        JmapRequest invalidObjectId = RequestWithCapabilities();
        invalidObjectId.CreatedIds = new Dictionary<string, string> { { "draft", "not/an/id" } };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.ValidateRequest(null, 10L).ProblemKind, Is.EqualTo(JmapProblemKind.NotRequest));
            Assert.That(validator.ValidateRequest(missingUsing, 10L).InvalidPaths, Is.EqualTo(new[] { "using" }));
            Assert.That(validator.ValidateRequest(missingCalls, 10L).InvalidPaths, Is.EqualTo(new[] { "methodCalls" }));
            Assert.That(validator.ValidateRequest(invalidCreatedId, 10L).InvalidPaths, Is.EqualTo(new[] { "createdIds/#draft" }));
            Assert.That(validator.ValidateRequest(invalidObjectId, 10L).InvalidPaths, Is.EqualTo(new[] { "createdIds/draft" }));
            Assert.That(validator.ValidateRequest(RequestWithCapabilities(), -1L).ProblemKind, Is.EqualTo(JmapProblemKind.NotRequest));
        }
    }

    [Test]
    public void MissingMethodCapabilityProducesAnIsolatedUnknownMethodError()
    {
        RequestValidator validator = Validator(10, 1000L);
        JmapRequest request = RequestWithCapabilities(JmapCapabilities.Core);
        request.MethodCalls.Add(Invocation(JmapMethods.EmailGet, "mail", "{}"));
        request.MethodCalls.Add(Invocation("Vendor/run", "vendor", "{}"));

        RequestValidationResult result = validator.ValidateRequest(request, 10L);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.MethodErrors.Keys, Is.EqualTo(new[] { 0 }));
            Assert.That(result.MethodErrors[0].Type, Is.EqualTo(JmapErrorCodes.UnknownMethod));
            Assert.That(result.MethodErrors[0].Kind, Is.EqualTo(JmapMethodErrorKind.UnknownMethod));
        }
    }

    [Test]
    public void ForwardMalformedAndConflictingReferencesFailOnlyTheirCalls()
    {
        RequestValidator validator = Validator(10, 1000L);
        JmapRequest request = RequestWithCapabilities(JmapCapabilities.Core, JmapCapabilities.Mail);
        request.MethodCalls.Add(Invocation(JmapMethods.EmailQuery, "c1", "{}"));
        request.MethodCalls.Add(Invocation(
            JmapMethods.EmailGet,
            "c2",
            "{\"#ids\":{\"resultOf\":\"c1\",\"name\":\"Email/query\",\"path\":\"/ids\"}}"));
        request.MethodCalls.Add(Invocation(
            JmapMethods.EmailGet,
            "c3",
            "{\"#ids\":{\"resultOf\":\"c4\",\"name\":\"Email/query\",\"path\":\"/ids\"}}"));
        request.MethodCalls.Add(Invocation(JmapMethods.CoreEcho, "c4", "{}"));
        request.MethodCalls.Add(Invocation(
            JmapMethods.EmailGet,
            "c5",
            "{\"ids\":[],\"#ids\":{\"resultOf\":\"c1\",\"name\":\"Email/query\",\"path\":\"/ids\"}}"));
        request.MethodCalls.Add(Invocation(JmapMethods.EmailGet, "c6", "{\"#ids\":null}"));

        RequestValidationResult result = validator.ValidateRequest(request, 500L);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.MethodErrors.Keys, Is.EqualTo(new[] { 2, 4, 5 }));
            Assert.That(result.MethodErrors[2].Kind, Is.EqualTo(JmapMethodErrorKind.InvalidResultReference));
            Assert.That(result.MethodErrors[4].Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(result.MethodErrors[5].Kind, Is.EqualTo(JmapMethodErrorKind.InvalidResultReference));
            Assert.That(result.MethodErrors.ContainsKey(1), Is.False);
        }
    }

    private static RequestValidator Validator(int maximumCalls, long maximumBytes)
    {
        JmapServerOptions options = new JmapServerOptions();
        options.MaxCallsInRequest = maximumCalls;
        options.MaxRequestBytes = maximumBytes;
        List<string> supported = new List<string>
        {
            JmapCapabilities.Core,
            JmapCapabilities.Mail
        };
        Dictionary<string, List<string>> methodCapabilities = new Dictionary<string, List<string>>
        {
            { JmapMethods.CoreEcho, new List<string> { JmapCapabilities.Core } },
            { JmapMethods.EmailQuery, new List<string> { JmapCapabilities.Core, JmapCapabilities.Mail } },
            { JmapMethods.EmailGet, new List<string> { JmapCapabilities.Core, JmapCapabilities.Mail } }
        };
        return new RequestValidator(options, supported, methodCapabilities);
    }

    private static JmapRequest RequestWithCapabilities(params string[] capabilities)
    {
        JmapRequest request = new JmapRequest();
        foreach (string capability in capabilities)
        {
            request.Using.Add(capability);
        }

        return request;
    }

    private static JmapInvocation Invocation(string name, string callId, string arguments)
    {
        JmapInvocation invocation = new JmapInvocation();
        invocation.Name = name;
        invocation.CallId = callId;
        invocation.Arguments = Json(arguments);
        return invocation;
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
