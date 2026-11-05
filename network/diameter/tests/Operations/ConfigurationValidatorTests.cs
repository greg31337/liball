namespace diameter.Tests;

public class ConfigurationValidatorTests
{
    [Test]
    public void DefaultCompositionBuildsCompleteInProcessDependencies()
    {
        DiameterNodeConfiguration configuration =
            CreateValidConfiguration();

        NodeComposition composition =
            ConfigurationValidator.ComposeDiameterNode(configuration);

        Assert.Multiple(() =>
        {
            Assert.That(composition.TransportFactory,
                Is.TypeOf<DiameterTcpTransportFactory>());
            Assert.That(composition.Clock,
                Is.TypeOf<DiameterSystemClock>());
            Assert.That(composition.IdentifierSource,
                Is.TypeOf<DiameterIdentifierSource>());
            Assert.That(composition.SessionStore,
                Is.TypeOf<DiameterInMemorySessionStore>());
            Assert.That(composition.AccountingRecordStore,
                Is.TypeOf<DiameterInMemoryAccountingRecordStore>());
            Assert.That(composition.CreditControlStore,
                Is.TypeOf<DiameterInMemoryCreditControlStore>());
            Assert.That(composition.DuplicateRequestStore,
                Is.TypeOf<DuplicateRequestStore>());
            Assert.That(composition.MessageCodec,
                Is.TypeOf<DiameterMessageCodec>());
            Assert.That(composition.Dictionary,
                Is.TypeOf<DiameterDictionary>());
            Assert.That(composition.Validator,
                Is.TypeOf<DiameterValidator>());
            Assert.That(composition.PeerManager,
                Is.TypeOf<DiameterPeerManager>());
            Assert.That(composition.Router,
                Is.TypeOf<DiameterRouter>());
            Assert.That(composition.RequestClient,
                Is.TypeOf<DiameterRequestClient>());
            Assert.That(composition.SessionManager,
                Is.TypeOf<DiameterSessionManager>());
            Assert.That(composition.AccountingClient,
                Is.TypeOf<DiameterAccountingClient>());
            Assert.That(composition.AccountingApplication, Is.Null);
            Assert.That(composition.CreditControlClient, Is.Null);
            Assert.That(composition.ApplicationHost,
                Is.TypeOf<DiameterApplicationHost>());
            Assert.That(composition.OverloadManager, Is.Null);
            Assert.That(composition.Statistics, Is.Not.Null);
            Assert.That(composition.Health, Is.Not.Null);
            Assert.That(composition.Events, Is.Not.Null);
        });
    }

    [Test]
    public void CallerSuppliedDependenciesAreWiredWithoutReplacement()
    {
        DiameterNodeConfiguration configuration =
            CreateValidConfiguration();
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        ManagerIdentifierSource identifiers =
            new ManagerIdentifierSource();
        ScriptedDiameterTransportFactory transport =
            new ScriptedDiameterTransportFactory();
        DiameterInMemorySessionStore sessionStore =
            new DiameterInMemorySessionStore();
        DiameterInMemoryAccountingRecordStore accountingStore =
            new DiameterInMemoryAccountingRecordStore();
        DiameterInMemoryCreditControlStore creditControlStore =
            new DiameterInMemoryCreditControlStore();
        DuplicateRequestStore duplicateStore =
            new DuplicateRequestStore(clock, 20, 30000);
        ManagerSessionEventHandler sessionHandler =
            new ManagerSessionEventHandler();
        RecordingAccountingHandler accountingHandler =
            new RecordingAccountingHandler();
        CreditControlApplicationRatingHandler creditHandler =
            new CreditControlApplicationRatingHandler();
        CreditControlApplicationEventHandler creditEvents =
            new CreditControlApplicationEventHandler();
        ManagerOverloadSource overload = new ManagerOverloadSource();
        RecordingEventSink eventSink = new RecordingEventSink();
        EventRedactionPolicyProbe redaction =
            new EventRedactionPolicyProbe();
        NodePeerAdmissionPolicy admission =
            new NodePeerAdmissionPolicy();
        configuration.TransportFactory = transport;
        configuration.Clock = clock;
        configuration.IdentifierSource = identifiers;
        configuration.SessionStore = sessionStore;
        configuration.AccountingRecordStore = accountingStore;
        configuration.CreditControlStore = creditControlStore;
        configuration.DuplicateRequestStore = duplicateStore;
        configuration.SessionEventHandler = sessionHandler;
        configuration.AccountingHandler = accountingHandler;
        configuration.CreditControlRequestHandler = creditHandler;
        configuration.CreditControlEventHandler = creditEvents;
        configuration.OverloadSource = overload;
        configuration.Overload.Enabled = true;
        configuration.EventSink = eventSink;
        configuration.RedactionPolicy = redaction;
        configuration.Security.PeerAdmissionPolicy = admission;

        NodeComposition composition =
            ConfigurationValidator.ComposeDiameterNode(configuration);

        DiameterExternalTransportFactory adapted =
            (DiameterExternalTransportFactory)composition.TransportFactory;
        Assert.Multiple(() =>
        {
            Assert.That(adapted.OwnedFactory, Is.SameAs(transport));
            Assert.That(composition.Clock, Is.SameAs(clock));
            Assert.That(composition.IdentifierSource,
                Is.SameAs(identifiers));
            Assert.That(composition.SessionStore,
                Is.SameAs(sessionStore));
            Assert.That(composition.AccountingRecordStore,
                Is.SameAs(accountingStore));
            Assert.That(composition.CreditControlStore,
                Is.SameAs(creditControlStore));
            Assert.That(composition.DuplicateRequestStore,
                Is.SameAs(duplicateStore));
            Assert.That(composition.Configuration.SessionEventHandler,
                Is.SameAs(sessionHandler));
            Assert.That(composition.Configuration.Security.PeerAdmissionPolicy,
                Is.SameAs(admission));
            Assert.That(composition.AccountingClient,
                Is.SameAs(composition.AccountingApplication));
            Assert.That(composition.AccountingApplication,
                Is.TypeOf<DiameterAccountingApplication>());
            Assert.That(composition.CreditControlClient,
                Is.SameAs(composition.CreditControlApplication));
            Assert.That(composition.CreditControlApplication,
                Is.TypeOf<DiameterCreditControlApplication>());
            Assert.That(composition.OverloadManager,
                Is.TypeOf<DiameterOverloadManager>());
            Assert.That(composition.Events.Sink, Is.SameAs(eventSink));
            Assert.That(composition.Events.RedactionPolicy,
                Is.SameAs(redaction));
        });
    }

    [Test]
    public void BuiltInTransportSelectionMatchesTransportAndSecurity()
    {
        DiameterNodeConfiguration tls = CreateValidConfiguration();
        tls.Security.Mode = DiameterSecurityMode.Tls;
        tls.Security.AllowUnsecuredTransport = false;
        tls.Security.LocalCertificatePath = "node.pfx";
        tls.Security.RequirePeerCertificate = true;

        DiameterNodeConfiguration sctp = CreateValidConfiguration();
        sctp.Transport.Kind = DiameterTransportKind.Sctp;
        sctp.Transport.SctpStreamCount = 4;

        NodeComposition tlsComposition =
            ConfigurationValidator.ComposeDiameterNode(tls);
        NodeComposition sctpComposition =
            ConfigurationValidator.ComposeDiameterNode(sctp);

        Assert.Multiple(() =>
        {
            Assert.That(tlsComposition.TransportFactory,
                Is.TypeOf<DiameterTlsTransportFactory>());
            Assert.That(sctpComposition.TransportFactory,
                Is.TypeOf<DiameterSctpTransportFactory>());
        });
    }

    [Test]
    public void ExternalAndDtlsTransportsRequireCallerFactory()
    {
        DiameterNodeConfiguration external = CreateValidConfiguration();
        external.Transport.Kind = DiameterTransportKind.External;
        external.Transport.Port = 0;
        external.Security.Mode = DiameterSecurityMode.External;
        external.Security.AllowUnsecuredTransport = false;

        DiameterNodeConfiguration dtls = CreateValidConfiguration();
        dtls.Transport.Kind = DiameterTransportKind.Sctp;
        dtls.Transport.SctpStreamCount = 4;
        dtls.Security.Mode = DiameterSecurityMode.Dtls;
        dtls.Security.AllowUnsecuredTransport = false;
        dtls.Security.LocalCertificatePath = "node.pfx";
        dtls.Security.RequirePeerCertificate = true;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                ConfigurationValidator.ComposeDiameterNode(external));
            Assert.Throws<ArgumentException>(() =>
                ConfigurationValidator.ComposeDiameterNode(dtls));
        });

        external.TransportFactory =
            new ScriptedDiameterTransportFactory();
        dtls.TransportFactory =
            new ScriptedDiameterTransportFactory();
        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() =>
                ConfigurationValidator.ComposeDiameterNode(external));
            Assert.DoesNotThrow(() =>
                ConfigurationValidator.ComposeDiameterNode(dtls));
        });
    }

    [Test]
    public void CompositionRejectsInvalidConfigurationAndDependencies()
    {
        DiameterNodeConfiguration invalid = CreateValidConfiguration();
        invalid.Transport.EnableActiveConnections = false;
        DiameterNodeConfiguration missingOverload =
            CreateValidConfiguration();
        missingOverload.Overload.Enabled = true;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                ConfigurationValidator.ComposeDiameterNode(null));
            Assert.Throws<ArgumentException>(() =>
                ConfigurationValidator.ComposeDiameterNode(invalid));
            Assert.Throws<ArgumentException>(() =>
                ConfigurationValidator.ComposeDiameterNode(
                    missingOverload));
        });
    }

    [Test]
    public void CompositionOwnsMutableConfigurationData()
    {
        DiameterNodeConfiguration configuration =
            CreateValidConfiguration();
        DiameterApplicationId application = new DiameterApplicationId();
        application.Id = 4U;
        configuration.Applications.Add(application);
        DiameterIdentity realm = new DiameterIdentity();
        realm.Value = "transit.example";
        configuration.RealmRouting.LocalRealms.Add(realm);

        NodeComposition composition =
            ConfigurationValidator.ComposeDiameterNode(configuration);
        configuration.OriginHost.Value = "changed.example";
        configuration.Applications[0].Id = 99U;
        configuration.Limits.MaximumPendingRequests = 1;
        configuration.RealmRouting.LocalRealms[0].Value =
            "changed.example";

        Assert.Multiple(() =>
        {
            Assert.That(composition.Configuration,
                Is.Not.SameAs(configuration));
            Assert.That(composition.Configuration.OriginHost.Value,
                Is.EqualTo("node.example"));
            Assert.That(composition.Configuration.Applications[0].Id,
                Is.EqualTo(4U));
            Assert.That(composition.Configuration.Limits.MaximumPendingRequests,
                Is.EqualTo(4096));
            Assert.That(composition.Configuration.RealmRouting.LocalRealms[0].Value,
                Is.EqualTo("transit.example"));
        });
    }

    [Test]
    public void DependencyFieldsDefaultToOptional()
    {
        DiameterNodeConfiguration configuration =
            new DiameterNodeConfiguration();

        Assert.Multiple(() =>
        {
            Assert.That(configuration.TransportFactory, Is.Null);
            Assert.That(configuration.Clock, Is.Null);
            Assert.That(configuration.IdentifierSource, Is.Null);
            Assert.That(configuration.SessionStore, Is.Null);
            Assert.That(configuration.AccountingRecordStore, Is.Null);
            Assert.That(configuration.CreditControlStore, Is.Null);
            Assert.That(configuration.DuplicateRequestStore, Is.Null);
            Assert.That(configuration.SessionEventHandler, Is.Null);
            Assert.That(configuration.AccountingHandler, Is.Null);
            Assert.That(configuration.CreditControlRequestHandler, Is.Null);
            Assert.That(configuration.CreditControlEventHandler, Is.Null);
            Assert.That(configuration.OverloadSource, Is.Null);
            Assert.That(configuration.EventSink, Is.Null);
            Assert.That(configuration.RedactionPolicy, Is.Null);
        });
    }

    private static DiameterNodeConfiguration CreateValidConfiguration()
    {
        DiameterNodeConfiguration configuration =
            new DiameterNodeConfiguration();
        configuration.OriginHost.Value = "node.example";
        configuration.OriginRealm.Value = "example";
        configuration.ProductName = "diameter.Tests";
        configuration.Roles = DiameterNodeRole.Client;
        configuration.Security.AllowUnsecuredTransport = true;
        return configuration;
    }
}

internal class NodePeerAdmissionPolicy : IPeerAdmissionPolicy
{
    public DiameterPeerAdmissionResult EvaluatePeerAdmission(
        DiameterPeerAdmissionRequest request)
    {
        DiameterPeerAdmissionResult result =
            new DiameterPeerAdmissionResult();
        result.Admitted = true;
        return result;
    }
}
