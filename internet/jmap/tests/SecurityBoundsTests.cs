using System.Text;
using System.Text.Json;

namespace jmap.Tests;

public class SecurityBoundsTests
{
    [Test]
    public void ServerJsonOptionsRejectUnallocatableSizesAndExcessiveDepth()
    {
        JmapServerOptions excessiveSize = new JmapServerOptions
        {
            MaxRequestBytes = (long)int.MaxValue + 1L
        };
        JmapServerOptions excessiveDepth = new JmapServerOptions
        {
            MaxJsonDepth = SecurityLimits.MaximumJsonDepth + 1
        };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => new RequestLimitGate(excessiveSize),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => new RequestLimitGate(excessiveDepth),
                Throws.TypeOf<ArgumentException>());
        }
    }

    [Test]
    public void JsonPointersAndPatchMapsHaveExplicitComplexityBounds()
    {
        JsonPointer pointers = new JsonPointer();
        string longPointer = "/" + new string('x',
            SecurityLimits.MaximumJsonPointerCharacters);
        StringBuilder manyTokens = new StringBuilder();
        int tokenIndex = 0;
        while (tokenIndex <= SecurityLimits.MaximumJsonPointerTokens)
        {
            manyTokens.Append("/x");
            tokenIndex = tokenIndex + 1;
        }

        JmapPatchObject patch = new JmapPatchObject();
        int patchIndex = 0;
        while (patchIndex <= SecurityLimits.MaximumPatchPaths)
        {
            patch.Values.Add("field" + patchIndex,
                JsonSerializer.SerializeToElement(patchIndex));
            patchIndex = patchIndex + 1;
        }

        PatchValidator validator = new PatchValidator();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(pointers.IsPointer(longPointer), Is.False);
            Assert.That(pointers.IsPointer(manyTokens.ToString()), Is.False);
            Assert.That(validator.ValidatePatch(Json("{}"), patch),
                Is.EqualTo(new[] { "" }));
        }
    }

    [Test]
    public void InvocationResultReferenceCountIsBoundedBeforeResolution()
    {
        StringBuilder arguments = new StringBuilder("{");
        int index = 0;
        while (index <= SecurityLimits.MaximumResultReferences)
        {
            if (index != 0)
            {
                arguments.Append(',');
            }

            arguments.Append("\"#value");
            arguments.Append(index);
            arguments.Append("\":{\"resultOf\":\"previous\",\"name\":\"Core/echo\",\"path\":\"/x\"}");
            index = index + 1;
        }

        arguments.Append('}');
        JmapInvocation invocation = new JmapInvocation
        {
            Name = JmapMethods.CoreEcho,
            CallId = "call-1",
            Arguments = Json(arguments.ToString())
        };
        RequestValidator validator = new RequestValidator(
            new JmapServerOptions(),
            new List<string> { JmapCapabilities.Core },
            new Dictionary<string, List<string>>());
        string? result = validator.ValidateInvocation(
            invocation, "methodCalls/0", new HashSet<string>());

        Assert.That(result, Is.EqualTo("methodCalls/0/1"));
    }

    [Test]
    public void StandardAndEmailFiltersRejectExcessiveBreadth()
    {
        JmapFilterNode standard = new JmapFilterNode
        {
            HasOperator = true,
            Operator = JmapFilterOperator.Or,
            Conditions = new List<JmapFilterNode>()
        };
        EmailFilter email = new EmailFilter
        {
            HasOperator = true,
            Operator = JmapFilterOperator.Or,
            Conditions = new List<EmailFilter>()
        };
        EmailSubmissionFilter submission = new EmailSubmissionFilter
        {
            HasOperator = true,
            Operator = JmapFilterOperator.Or,
            Conditions = new List<EmailSubmissionFilter>()
        };
        int index = 0;
        while (index < SecurityLimits.MaximumFilterNodes)
        {
            standard.Conditions.Add(new JmapFilterNode());
            email.Conditions.Add(new EmailFilter());
            submission.Conditions.Add(new EmailSubmissionFilter());
            index = index + 1;
        }

        StandardQueryProcessor standardProcessor = new StandardQueryProcessor(
            100, 64, new List<string>(), new List<string>(), new List<string>(),
            new List<string>(), new List<string>());
        JmapMethodError? standardError = standardProcessor.ValidateFilterTree(standard, 1);
        JmapMethodError? emailError = new EmailFilterEvaluator().ValidateFilter(email, 64);
        JmapMethodError? submissionError = new EmailSubmissionMethod()
            .ValidateSubmissionFilter(submission, 1, 64);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(standardError, Is.Not.Null);
            Assert.That(standardError!.Kind, Is.EqualTo(JmapMethodErrorKind.UnsupportedFilter));
            Assert.That(emailError, Is.Not.Null);
            Assert.That(emailError!.Kind, Is.EqualTo(JmapMethodErrorKind.UnsupportedFilter));
            Assert.That(submissionError, Is.Not.Null);
            Assert.That(submissionError!.Kind,
                Is.EqualTo(JmapMethodErrorKind.UnsupportedFilter));
        }
    }

    [Test]
    public void UriTemplatesVariablesAndSessionEndpointsHaveExpansionBounds()
    {
        UriTemplateExpander expander = new UriTemplateExpander();
        string largeVariable = new string('é',
            SecurityLimits.MaximumUriVariableBytes / 2 + 1);
        string largeTemplate = "https://example.test/" +
            new string('x', SecurityLimits.MaximumUriTemplateCharacters) +
            "/{accountId}";
        JmapSession session = new JmapSession
        {
            ApiUrl = largeTemplate,
            DownloadUrl = "d",
            UploadUrl = "u",
            EventSourceUrl = "e",
            Username = "user",
            State = "state"
        };
        JmapCapability core = new JmapCapability
        {
            Core = new JmapCoreCapabilities()
        };
        session.Capabilities.Add(JmapCapabilities.Core, core);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => expander.ExpandUploadUrl(
                "https://example.test/{accountId}", largeVariable),
                Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => expander.ExpandUploadUrl(largeTemplate, "a"),
                Throws.TypeOf<JmapProtocolException>());
            Assert.That(new SessionValidator().ValidateSession(session),
                Does.Contain("apiUrl"));
        }
    }

    [Test]
    public void EventSourceCapacityTypesAndPublishedStateAreBounded()
    {
        EventSourceHub hub = new EventSourceHub(
            new JmapServerOptions(), null, 2, 4);
        hub.MaximumSubscribers = 1;
        hub.Subscribe(new EventSourceOptions(), CancellationToken.None);

        List<string> types = new List<string>();
        int index = 0;
        while (index <= SecurityLimits.MaximumEventSourceTypes)
        {
            types.Add("Type" + index);
            index = index + 1;
        }

        List<string> invalidTypes = new List<string>();
        new PushValidator().ValidateTypes(types, invalidTypes);
        JmapStatePublication publication = Publication(
            new string('s', SecurityLimits.MaximumEventSourceStateBytes + 1));
        JmapStatePublication aggregate = Publication("state");
        aggregate.StateChange.Changed["account-1"].Clear();
        index = 0;
        while (index < 257)
        {
            aggregate.StateChange.Changed["account-1"].Add(
                "Type" + index,
                new string('s', SecurityLimits.MaximumEventSourceStateBytes));
            index = index + 1;
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => hub.Subscribe(
                new EventSourceOptions(), CancellationToken.None),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(invalidTypes, Is.EqualTo(new[] { "types" }));
            Assert.That(() => hub.Publish(publication),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => hub.Publish(aggregate),
                Throws.TypeOf<ArgumentException>());
        }
    }

    [Test]
    public void PushUrlsAndUtf8FileNamesAreBoundedBeforeUse()
    {
        PushValidator push = new PushValidator();
        string longUrl = "https://push.example.test/" +
            new string('x', SecurityLimits.MaximumPushUrlCharacters);
        BlobEndpointProcessor blobs = new BlobEndpointProcessor(
            new JmapServerOptions(), new JmapServerServices());
        string maximumName = new string('é',
            SecurityLimits.MaximumFileNameBytes / 2);
        string excessiveName = maximumName + "é";

        using (Assert.EnterMultipleScope())
        {
            Assert.That(push.IsHttpsUrl(longUrl), Is.False);
            Assert.That(push.IsDefaultPushDestinationAllowed(
                "https://127.0.0.1/callback"), Is.False);
            Assert.That(push.IsDefaultPushDestinationAllowed(
                "https://[::1]/callback"), Is.False);
            Assert.That(push.IsDefaultPushDestinationAllowed(
                "https://localhost/callback"), Is.False);
            Assert.That(push.IsDefaultPushDestinationAllowed(
                "https://push.example.test/callback"), Is.True);
            Assert.That(blobs.IsSafeFileName(maximumName), Is.True);
            Assert.That(blobs.IsSafeFileName(excessiveName), Is.False);
        }
    }

    [Test]
    public async Task EndpointFailuresDoNotExposeBackendExceptionDetails()
    {
        JmapServer server = new JmapServer(
            new JmapServerOptions(), new JmapServerServices());
        const string secret = "database-password=do-not-return";

        JmapEndpointResponse response = await server.Endpoints.RunEndpoint(
            () => throw new InvalidOperationException(secret),
            CancellationToken.None);
        string body = Encoding.UTF8.GetString(response.BodyBytes!);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(500));
            Assert.That(body, Does.Not.Contain(secret));
            Assert.That(body, Does.Contain("endpoint could not complete"));
        }
    }

    private static JmapStatePublication Publication(string state)
    {
        JmapStatePublication publication = new JmapStatePublication
        {
            EventId = "event-1"
        };
        publication.StateChange.Changed.Add("account-1",
            new Dictionary<string, string>
            {
                [JmapDataTypes.Email] = state
            });
        return publication;
    }

    private static JsonElement Json(string value)
    {
        using JsonDocument document = JsonDocument.Parse(value);
        return document.RootElement.Clone();
    }
}
