using System.Text.Json;

namespace jmap.Tests;

public class MethodRegistryTests
{
    [Test]
    public void StandardRegistryResolvesEveryCoreAndMailMethodWithRequiredCapabilities()
    {
        MethodRegistry registry = new MethodRegistry(new JmapServerServices());
        Dictionary<string, List<string>> expected = StandardMethods();
        Dictionary<string, List<string>> actual = registry.GetMethodCapabilities();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(registry.GetSupportedCapabilities(), Is.EqualTo(new[]
            {
                JmapCapabilities.Core,
                JmapCapabilities.Mail,
                JmapCapabilities.Submission,
                JmapCapabilities.VacationResponse
            }));
            Assert.That(actual.Keys, Is.EquivalentTo(expected.Keys));
            Assert.That(actual, Has.Count.EqualTo(30));
            Assert.That(registry.FindMethod("email/get"), Is.Null);
            Assert.That(registry.FindMethod("Vendor/missing"), Is.Null);
        }

        foreach (KeyValuePair<string, List<string>> item in expected)
        {
            RegisteredJmapMethod? method = registry.FindMethod(item.Key);
            Assert.That(method, Is.Not.Null, item.Key);
            Assert.That(method!.Name, Is.EqualTo(item.Key), item.Key);
            Assert.That(method.RequiredCapabilities, Is.EqualTo(item.Value), item.Key);
            Assert.That(method.VendorHandler, Is.Null, item.Key);
            Assert.That(actual[item.Key], Is.EqualTo(item.Value), item.Key);
        }

        actual[JmapMethods.EmailGet].Clear();
        List<string> capabilities = registry.GetSupportedCapabilities();
        capabilities.Clear();
        Assert.That(registry.FindMethod(JmapMethods.EmailGet)!.RequiredCapabilities,
            Is.EqualTo(new[] { JmapCapabilities.Core, JmapCapabilities.Mail }));
        Assert.That(registry.HasCapability(JmapCapabilities.Core), Is.True);
    }

    [Test]
    public void RegistryTablesDriveRequestCapabilityValidation()
    {
        MethodRegistry registry = new MethodRegistry(new JmapServerServices());
        RequestValidator validator = new RequestValidator(
            new JmapServerOptions(),
            registry.GetSupportedCapabilities(),
            registry.GetMethodCapabilities());
        JmapRequest request = new JmapRequest();
        request.Using.Add(JmapCapabilities.Core);
        request.Using.Add(JmapCapabilities.Mail);
        request.MethodCalls.Add(Invocation(JmapMethods.EmailGet, "c0", "{}"));
        request.MethodCalls.Add(Invocation(JmapMethods.IdentityGet, "c1", "{}"));

        RequestValidationResult result = validator.ValidateRequest(request, 100L);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.MethodErrors.ContainsKey(0), Is.False);
            Assert.That(result.MethodErrors[1].Kind, Is.EqualTo(JmapMethodErrorKind.UnknownMethod));
            Assert.That(result.MethodErrors[1].Description, Does.Contain(JmapCapabilities.Submission));
        }
    }

    [Test]
    public async Task VendorRegistrationAddsCapabilitiesAndPassesOpaqueInvocationToItsHandler()
    {
        RecordingVendorHandler handler = new RecordingVendorHandler();
        JmapVendorMethodRegistration registration = new JmapVendorMethodRegistration();
        registration.MethodName = "Vendor/run";
        registration.RequiredCapabilities.Add("https://vendor.example/capability");
        registration.RequiredCapabilities.Add("https://vendor.example/capability");
        registration.RequiredCapabilities.Add(JmapCapabilities.Core);
        registration.Handler = handler;
        JmapServerServices services = new JmapServerServices();
        services.VendorCapabilities.Add("https://vendor.example/session-only");
        services.VendorMethods.Add(registration);
        MethodRegistry registry = new MethodRegistry(services);
        JmapRequestContext context = new JmapRequestContext { UserId = "user-1" };
        CancellationTokenSource cancellation = new CancellationTokenSource();
        JmapInvocation invocation = Invocation(
            "Vendor/run", "vendor-1",
            "{\"nested\":[1.2300e+4,{\"unknown\":true}],\"name\":\"value\"}");

        JmapInvocation response = await registry.ProcessVendorMethodAsync(
            invocation, context, cancellation.Token);
        RegisteredJmapMethod? resolved = registry.FindMethod("Vendor/run");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolved, Is.Not.Null);
            Assert.That(resolved!.RequiredCapabilities, Is.EqualTo(new[]
            {
                JmapCapabilities.Core,
                "https://vendor.example/capability"
            }));
            Assert.That(registry.HasCapability("https://vendor.example/session-only"), Is.True);
            Assert.That(registry.HasCapability("https://vendor.example/capability"), Is.True);
            Assert.That(handler.Invocation, Is.SameAs(invocation));
            Assert.That(handler.Context, Is.SameAs(context));
            Assert.That(handler.CancellationToken, Is.EqualTo(cancellation.Token));
            Assert.That(response.Name, Is.EqualTo("Vendor/result"));
            Assert.That(response.CallId, Is.EqualTo("vendor-1"));
            Assert.That(response.Arguments.GetRawText(), Is.EqualTo(invocation.Arguments.GetRawText()));
        }
    }

    [Test]
    public void InvalidOrConflictingVendorRegistrationsAreRejected()
    {
        JmapServerServices malformedCapability = new JmapServerServices();
        malformedCapability.VendorCapabilities.Add("not a uri");
        JmapServerServices standardCollision = ServicesWithMethod(
            JmapMethods.EmailGet, "https://vendor.example/capability", new RecordingVendorHandler());
        JmapServerServices missingHandler = ServicesWithMethod(
            "Vendor/run", "https://vendor.example/capability", null);
        JmapServerServices duplicate = ServicesWithMethod(
            "Vendor/run", "https://vendor.example/capability", new RecordingVendorHandler());
        duplicate.VendorMethods.Add(duplicate.VendorMethods[0]);

        Assert.That(() => new MethodRegistry(malformedCapability),
            Throws.TypeOf<JmapValidationException>());
        Assert.That(() => new MethodRegistry(standardCollision),
            Throws.TypeOf<JmapValidationException>());
        Assert.That(() => new MethodRegistry(missingHandler),
            Throws.TypeOf<JmapValidationException>());
        Assert.That(() => new MethodRegistry(duplicate),
            Throws.TypeOf<JmapValidationException>());
    }

    private static JmapServerServices ServicesWithMethod(
        string methodName,
        string capability,
        IJmapVendorMethodHandler? handler)
    {
        JmapVendorMethodRegistration registration = new JmapVendorMethodRegistration();
        registration.MethodName = methodName;
        registration.RequiredCapabilities.Add(capability);
        registration.Handler = handler;
        JmapServerServices services = new JmapServerServices();
        services.VendorMethods.Add(registration);
        return services;
    }

    private static Dictionary<string, List<string>> StandardMethods()
    {
        Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();
        Add(result, JmapMethods.CoreEcho, JmapCapabilities.Core);
        Add(result, JmapMethods.BlobCopy, JmapCapabilities.Core);
        Add(result, JmapMethods.PushSubscriptionGet, JmapCapabilities.Core);
        Add(result, JmapMethods.PushSubscriptionSet, JmapCapabilities.Core);
        Add(result, JmapMethods.MailboxGet, JmapCapabilities.Mail);
        Add(result, JmapMethods.MailboxChanges, JmapCapabilities.Mail);
        Add(result, JmapMethods.MailboxQuery, JmapCapabilities.Mail);
        Add(result, JmapMethods.MailboxQueryChanges, JmapCapabilities.Mail);
        Add(result, JmapMethods.MailboxSet, JmapCapabilities.Mail);
        Add(result, JmapMethods.ThreadGet, JmapCapabilities.Mail);
        Add(result, JmapMethods.ThreadChanges, JmapCapabilities.Mail);
        Add(result, JmapMethods.EmailGet, JmapCapabilities.Mail);
        Add(result, JmapMethods.EmailChanges, JmapCapabilities.Mail);
        Add(result, JmapMethods.EmailQuery, JmapCapabilities.Mail);
        Add(result, JmapMethods.EmailQueryChanges, JmapCapabilities.Mail);
        Add(result, JmapMethods.EmailSet, JmapCapabilities.Mail);
        Add(result, JmapMethods.EmailCopy, JmapCapabilities.Mail);
        Add(result, JmapMethods.EmailImport, JmapCapabilities.Mail);
        Add(result, JmapMethods.EmailParse, JmapCapabilities.Mail);
        Add(result, JmapMethods.SearchSnippetGet, JmapCapabilities.Mail);
        Add(result, JmapMethods.IdentityGet, JmapCapabilities.Submission);
        Add(result, JmapMethods.IdentityChanges, JmapCapabilities.Submission);
        Add(result, JmapMethods.IdentitySet, JmapCapabilities.Submission);
        Add(result, JmapMethods.EmailSubmissionGet, JmapCapabilities.Submission);
        Add(result, JmapMethods.EmailSubmissionChanges, JmapCapabilities.Submission);
        Add(result, JmapMethods.EmailSubmissionQuery, JmapCapabilities.Submission);
        Add(result, JmapMethods.EmailSubmissionQueryChanges, JmapCapabilities.Submission);
        Add(result, JmapMethods.EmailSubmissionSet, JmapCapabilities.Submission);
        Add(result, JmapMethods.VacationResponseGet, JmapCapabilities.VacationResponse);
        Add(result, JmapMethods.VacationResponseSet, JmapCapabilities.VacationResponse);
        return result;
    }

    private static void Add(
        Dictionary<string, List<string>> methods,
        string methodName,
        string capability)
    {
        List<string> required = new List<string> { JmapCapabilities.Core };
        if (capability != JmapCapabilities.Core)
        {
            required.Add(capability);
        }

        methods.Add(methodName, required);
    }

    private static JmapInvocation Invocation(string name, string callId, string arguments)
    {
        JmapInvocation invocation = new JmapInvocation();
        invocation.Name = name;
        invocation.CallId = callId;
        invocation.Arguments = Json(arguments);
        return invocation;
    }

    private static JsonElement Json(string value)
    {
        return JsonDocument.Parse(value).RootElement.Clone();
    }

    private class RecordingVendorHandler : IJmapVendorMethodHandler
    {
        public JmapInvocation? Invocation;
        public JmapRequestContext? Context;
        public CancellationToken CancellationToken;

        public Task<JmapInvocation> ProcessVendorMethodAsync(
            JmapInvocation invocation,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            Invocation = invocation;
            Context = context;
            CancellationToken = cancellationToken;
            JmapInvocation response = new JmapInvocation();
            response.Name = "Vendor/result";
            response.CallId = invocation.CallId;
            response.Arguments = invocation.Arguments.Clone();
            return Task.FromResult(response);
        }
    }
}
