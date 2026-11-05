namespace jmap.Tests;

public class InterfaceContractsTests
{
    [Test]
    public void RequestBuilderContractCoversCapabilitiesCallsReferencesCreatedIdsAndLifecycle()
    {
        string[] names = MethodNames(typeof(IJmapRequestBuilder));

        Assert.That(names, Is.EquivalentTo(new[]
        {
            "AddRequestCapability",
            "AddRawMethodCall",
            "AddTypedMethodCall",
            "CreateResultReference",
            "AddRequestCreatedId",
            "BuildJmapRequest",
            "ResetJmapRequestBuilder"
        }));
    }

    [Test]
    public void TransportContractCoversSessionApiBinaryAndEventSourceEndpoints()
    {
        string[] names = MethodNames(typeof(IJmapTransport));

        Assert.That(names, Is.EquivalentTo(new[]
        {
            "GetTransportSessionAsync",
            "SendTransportApiRequestAsync",
            "SendTransportUploadAsync",
            "SendTransportDownloadAsync",
            "OpenTransportEventSourceAsync"
        }));
    }

    [Test]
    public void ClientLifecycleContractCoversDiscoveryRefreshPrimaryAccountBatchRawAndEcho()
    {
        string[] names = MethodNames(typeof(IJmapClient));

        Assert.That(names, Is.EquivalentTo(new[]
        {
            "GetClientOptions",
            "GetSession",
            "DiscoverSessionAsync",
            "RefreshSessionAsync",
            "GetPrimaryAccountId",
            "ExecuteBatchAsync",
            "ExecuteRawCallAsync",
            "ExecuteEchoAsync",
            "FindCallResult"
        }));
    }

    [Test]
    public void CodecContractCoversByteMemoryAndStreamProtocolOperations()
    {
        string[] names = MethodNames(typeof(IJmapCodec));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(names, Does.Contain("DecodeSessionBytes"));
            Assert.That(names, Does.Contain("DecodeSessionMemory"));
            Assert.That(names, Does.Contain("DecodeSessionStreamAsync"));
            Assert.That(names, Does.Contain("EncodeSessionBytes"));
            Assert.That(names, Does.Contain("EncodeSessionStreamAsync"));
            Assert.That(names, Does.Contain("DecodeRequestMemory"));
            Assert.That(names, Does.Contain("DecodeRequestStreamAsync"));
            Assert.That(names, Does.Contain("EncodeRequestStreamAsync"));
            Assert.That(names, Does.Contain("DecodeResponseMemory"));
            Assert.That(names, Does.Contain("DecodeResponseStreamAsync"));
            Assert.That(names, Does.Contain("EncodeResponseStreamAsync"));
            Assert.That(names, Does.Contain("DecodeProblemMemory"));
            Assert.That(names, Does.Contain("DecodeProblemStreamAsync"));
            Assert.That(names, Does.Contain("EncodeProblemStreamAsync"));
            Assert.That(names, Does.Contain("DecodeStateChangeMemory"));
            Assert.That(names, Does.Contain("DecodeStateChangeStreamAsync"));
            Assert.That(names, Does.Contain("EncodeStateChangeStreamAsync"));
        }
    }

    [Test]
    public void EveryOperationInterfaceUsesUniqueMethodNames()
    {
        Type[] interfaces =
        {
            typeof(IJmapCodec),
            typeof(IJmapRequestBuilder),
            typeof(IJmapTransport),
            typeof(IJmapAuthorizationProvider),
            typeof(IJmapServiceLocator),
            typeof(IJmapClient),
            typeof(IBlobClient),
            typeof(IPushClient),
            typeof(IMailboxClient),
            typeof(IThreadClient),
            typeof(IEmailClient),
            typeof(ISearchSnippetClient),
            typeof(IIdentityClient),
            typeof(IEmailSubmissionClient),
            typeof(IVacationResponseClient),
            typeof(IJmapEventSourceReader),
            typeof(IJmapServer),
            typeof(IJmapSessionService),
            typeof(IJmapCoreService),
            typeof(IBlobService),
            typeof(IPushService),
            typeof(IMailboxService),
            typeof(IThreadService),
            typeof(IEmailService),
            typeof(ISearchSnippetService),
            typeof(IIdentityService),
            typeof(IEmailSubmissionService),
            typeof(IVacationResponseService),
            typeof(IJmapStatePublisher),
            typeof(IJmapClock),
            typeof(IJmapPushUrlPolicy),
            typeof(IJmapStringLocalizer)
        };

        foreach (Type contract in interfaces)
        {
            string[] names = MethodNames(contract);
            Assert.That(names.Distinct().Count(), Is.EqualTo(names.Length), contract.Name);
        }
    }

    [Test]
    public void CombinedClientContractsHaveNoMethodNameCollisions()
    {
        Type[] clientContracts =
        {
            typeof(IJmapClient),
            typeof(IBlobClient),
            typeof(IPushClient),
            typeof(IMailboxClient),
            typeof(IThreadClient),
            typeof(IEmailClient),
            typeof(ISearchSnippetClient),
            typeof(IIdentityClient),
            typeof(IEmailSubmissionClient),
            typeof(IVacationResponseClient)
        };
        string[] names = clientContracts
            .SelectMany(MethodNames)
            .ToArray();

        Assert.That(names.Distinct().Count(), Is.EqualTo(names.Length));
    }

    private static string[] MethodNames(Type contract)
    {
        return contract
            .GetMethods()
            .Select(method => method.Name)
            .ToArray();
    }
}
