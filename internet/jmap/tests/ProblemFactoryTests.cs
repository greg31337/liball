namespace jmap.Tests;

public class ProblemFactoryTests
{
    [TestCase(JmapProblemKind.UnknownCapability, JmapErrorCodes.UnknownCapabilityProblemType, "Unknown Capability")]
    [TestCase(JmapProblemKind.NotJson, JmapErrorCodes.NotJsonProblemType, "Not JSON")]
    [TestCase(JmapProblemKind.NotRequest, JmapErrorCodes.NotRequestProblemType, "Not a JMAP Request")]
    public void StandardRequestProblemsProduceCanonicalHttpResponses(JmapProblemKind kind, string type, string title)
    {
        ProblemFactory factory = new ProblemFactory();

        JmapEndpointResponse response = factory.CreateProblemResponse(kind, "Request rejected.", null);
        JmapProblem problem = new JmapJsonCodec().DecodeProblemBytes(response.BodyBytes!);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(400));
            Assert.That(response.ContentType, Is.EqualTo(JmapMediaTypes.ProblemJson));
            Assert.That(response.BodyStream, Is.Null);
            Assert.That(response.HasContentLength, Is.True);
            Assert.That(response.ContentLength, Is.EqualTo(response.BodyBytes!.Length));
            Assert.That(problem.Type, Is.EqualTo(type));
            Assert.That(problem.Kind, Is.EqualTo(kind));
            Assert.That(problem.Title, Is.EqualTo(title));
            Assert.That(problem.Status, Is.EqualTo(response.StatusCode));
            Assert.That(problem.HasStatus, Is.True);
            Assert.That(problem.Detail, Is.EqualTo("Request rejected."));
            Assert.That(problem.Limit, Is.Null);
        }
    }

    [Test]
    public void LimitProblemIncludesTheAppliedLimitName()
    {
        ProblemFactory factory = new ProblemFactory();

        JmapEndpointResponse response = factory.CreateProblemResponse(
            JmapProblemKind.Limit,
            "The request contains too many calls.",
            JmapProperties.MaxCallsInRequest);
        JmapProblem problem = new JmapJsonCodec().DecodeProblemBytes(response.BodyBytes!);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(problem.Type, Is.EqualTo(JmapErrorCodes.LimitProblemType));
            Assert.That(problem.Kind, Is.EqualTo(JmapProblemKind.Limit));
            Assert.That(problem.Status, Is.EqualTo(400));
            Assert.That(problem.Limit, Is.EqualTo(JmapProperties.MaxCallsInRequest));
        }
    }

    [Test]
    public void ValidationFailureMapsDirectlyToAProblemResponse()
    {
        RequestValidationResult validation = new RequestValidationResult();
        validation.ProblemKind = JmapProblemKind.UnknownCapability;
        validation.Detail = "Unsupported capability.";

        JmapEndpointResponse response = new ProblemFactory().CreateValidationResponse(validation);
        JmapProblem problem = new JmapJsonCodec().DecodeProblemBytes(response.BodyBytes!);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(problem.Kind, Is.EqualTo(JmapProblemKind.UnknownCapability));
            Assert.That(problem.Detail, Is.EqualTo(validation.Detail));
            Assert.That(response.StatusCode, Is.EqualTo(400));
        }
    }

    [Test]
    public void FactoryRejectsMissingOrMisplacedLimitDataAndNonFailures()
    {
        ProblemFactory factory = new ProblemFactory();
        RequestValidationResult valid = new RequestValidationResult();
        valid.IsValid = true;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                () => factory.CreateProblemResponse(JmapProblemKind.Limit, "Limited.", null),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => factory.CreateProblemResponse(JmapProblemKind.NotRequest, "Invalid.", JmapProperties.MaxSizeRequest),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => factory.CreateProblemResponse(JmapProblemKind.Unknown, "Unknown.", null),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => factory.CreateValidationResponse(valid),
                Throws.TypeOf<ArgumentException>());
        }
    }
}
