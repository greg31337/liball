using System;

namespace diameter;

internal class NodeComposition
{
    internal DiameterNodeConfiguration Configuration;
    internal IDiameterTransportFactory TransportFactory;
    internal IDiameterClock Clock;
    internal IDiameterIdentifierSource IdentifierSource;
    internal IDiameterSessionStore SessionStore;
    internal IAccountingRecordStore AccountingRecordStore;
    internal ICreditControlStore CreditControlStore;
    internal IDuplicateRequestStore DuplicateRequestStore;
    internal IDiameterMessageCodec MessageCodec;
    internal IDiameterDictionary Dictionary;
    internal IDiameterValidator Validator;
    internal IDiameterPeerManager PeerManager;
    internal IDiameterRouter Router;
    internal IDiameterRequestClient RequestClient;
    internal IDiameterSessionManager SessionManager;
    internal IDiameterAccountingClient AccountingClient;
    internal IDiameterAccountingApplication? AccountingApplication;
    internal IDiameterCreditControlClient? CreditControlClient;
    internal IDiameterCreditControlApplication? CreditControlApplication;
    internal IDiameterApplication? CreditControlModule;
    internal IDiameterApplicationHost ApplicationHost;
    internal IDiameterOverloadManager? OverloadManager;
    internal StatisticsCollector Statistics;
    internal HealthCollector Health;
    internal EventPublisher Events;

    internal NodeComposition(
        DiameterNodeConfiguration configuration,
        IDiameterTransportFactory transportFactory,
        IDiameterClock clock,
        IDiameterIdentifierSource identifierSource,
        IDiameterSessionStore sessionStore,
        IAccountingRecordStore accountingRecordStore,
        ICreditControlStore creditControlStore,
        IDuplicateRequestStore duplicateRequestStore)
    {
        Configuration = configuration;
        TransportFactory = transportFactory;
        Clock = clock;
        IdentifierSource = identifierSource;
        SessionStore = sessionStore;
        AccountingRecordStore = accountingRecordStore;
        CreditControlStore = creditControlStore;
        DuplicateRequestStore = duplicateRequestStore;
        MessageCodec = new DiameterMessageCodec();
        Dictionary = new DiameterDictionary();
        Validator = new DiameterValidator();
        PeerManager = new DiameterPeerManager(
            configuration,
            clock,
            identifierSource);
        Router = new DiameterRouter(
            configuration,
            PeerManager,
            clock);
        RequestClient = new DiameterRequestClient(
            configuration,
            Router,
            PeerManager,
            MessageCodec,
            identifierSource,
            clock);
        SessionManager = new DiameterSessionManager(
            configuration.OriginHost,
            sessionStore,
            configuration.SessionEventHandler,
            clock,
            identifierSource);

        DiameterBaseAvpFactory avpFactory =
            new DiameterBaseAvpFactory();
        DiameterBaseCommandFactory commandFactory =
            new DiameterBaseCommandFactory(identifierSource);
        AccountingApplication = null;
        if (configuration.AccountingHandler == null)
        {
            AccountingClient = new DiameterAccountingClient(
                configuration.OriginHost,
                configuration.OriginRealm,
                avpFactory,
                RequestClient,
                accountingRecordStore,
                clock);
        }
        else
        {
            DiameterAccountingApplication accounting =
                new DiameterAccountingApplication(
                    configuration.OriginHost,
                    configuration.OriginRealm,
                    avpFactory,
                    commandFactory,
                    RequestClient,
                    duplicateRequestStore,
                    configuration.AccountingHandler,
                    accountingRecordStore,
                    clock);
            AccountingClient = accounting;
            AccountingApplication = accounting;
        }

        CreditControlClient = null;
        CreditControlApplication = null;
        CreditControlModule = null;
        if (configuration.CreditControlRequestHandler != null)
        {
            DiameterCreditControlApplication creditControl =
                new DiameterCreditControlApplication(
                    configuration.OriginHost,
                    configuration.OriginRealm,
                    identifierSource,
                    RequestClient,
                    creditControlStore,
                    configuration.CreditControlRequestHandler,
                    configuration.CreditControlEventHandler,
                    clock);
            CreditControlClient = creditControl;
            CreditControlApplication = creditControl;
            CreditControlModule = creditControl;
        }

        DiameterApplicationHost applicationHost =
            new DiameterApplicationHost(
                Dictionary,
                Validator,
                clock,
                configuration.Limits,
                configuration.OriginHost,
                configuration.OriginRealm);
        ApplicationHost = applicationHost;
        OverloadManager = null;
        if (configuration.OverloadSource != null)
        {
            OverloadManager = new DiameterOverloadManager(
                configuration,
                configuration.OverloadSource,
                clock);
            applicationHost.ConfigureDiameterOverloadAdmission(
                OverloadManager);
        }

        Statistics = new StatisticsCollector(clock);
        Health = new HealthCollector(clock, 128);
        Events = new EventPublisher(
            clock,
            configuration.EventSink,
            configuration.RedactionPolicy);
    }
}

internal static class ConfigurationValidator
{
    internal static NodeComposition ComposeDiameterNode(
        DiameterNodeConfiguration? configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentException(
                "A Diameter node configuration is required.");
        }

        configuration.ValidateConfiguration();
        if ((configuration.Transport.Kind ==
            DiameterTransportKind.External ||
            configuration.Security.Mode == DiameterSecurityMode.Dtls) &&
            configuration.TransportFactory == null)
        {
            throw new ArgumentException(
                "External and DTLS transports require a caller-supplied factory.");
        }

        if (configuration.Overload.Enabled &&
            configuration.OverloadSource == null)
        {
            throw new ArgumentException(
                "Enabled overload reporting requires a local overload source.");
        }

        DiameterNodeConfiguration owned =
            CloneDiameterNodeConfiguration(configuration);
        IDiameterClock clock = owned.Clock == null ?
            new DiameterSystemClock() : owned.Clock;
        IDiameterIdentifierSource identifierSource =
            owned.IdentifierSource == null ?
            new DiameterIdentifierSource(clock) : owned.IdentifierSource;
        IDiameterTransportFactory transportFactory =
            ResolveDiameterTransportFactory(owned);
        IDiameterSessionStore sessionStore = owned.SessionStore == null ?
            new DiameterInMemorySessionStore() : owned.SessionStore;
        IAccountingRecordStore accountingStore =
            owned.AccountingRecordStore == null ?
            new DiameterInMemoryAccountingRecordStore() :
            owned.AccountingRecordStore;
        ICreditControlStore creditControlStore =
            owned.CreditControlStore == null ?
            new DiameterInMemoryCreditControlStore() :
            owned.CreditControlStore;
        IDuplicateRequestStore duplicateStore =
            owned.DuplicateRequestStore == null ?
            new DuplicateRequestStore(
                clock,
                owned.Limits.MaximumPendingRequests,
                owned.Timers.DuplicateCacheLifetimeMilliseconds) :
            owned.DuplicateRequestStore;

        return new NodeComposition(
            owned,
            transportFactory,
            clock,
            identifierSource,
            sessionStore,
            accountingStore,
            creditControlStore,
            duplicateStore);
    }

    internal static IDiameterTransportFactory ResolveDiameterTransportFactory(
        DiameterNodeConfiguration configuration)
    {
        if (configuration.TransportFactory != null)
        {
            return new DiameterExternalTransportFactory(
                configuration.TransportFactory);
        }

        if (configuration.Transport.Kind == DiameterTransportKind.Sctp)
        {
            return new DiameterSctpTransportFactory();
        }

        if (configuration.Security.Mode == DiameterSecurityMode.Tls)
        {
            return new DiameterTlsTransportFactory();
        }

        return new DiameterTcpTransportFactory();
    }

    internal static DiameterNodeConfiguration CloneDiameterNodeConfiguration(
        DiameterNodeConfiguration source)
    {
        DiameterNodeConfiguration result =
            new DiameterNodeConfiguration();
        result.OriginHost.Value = source.OriginHost.Value;
        result.OriginRealm.Value = source.OriginRealm.Value;
        result.ProductName = source.ProductName;
        result.VendorId = source.VendorId;
        result.FirmwareRevision = source.FirmwareRevision;
        result.Roles = source.Roles;
        result.DefaultRoutingPriority = source.DefaultRoutingPriority;

        result.Applications.Clear();
        int index = 0;
        while (index < source.Applications.Count)
        {
            DiameterApplicationId sourceApplication =
                source.Applications[index];
            DiameterApplicationId application =
                new DiameterApplicationId();
            application.Id = sourceApplication.Id;
            application.VendorId = sourceApplication.VendorId;
            application.IsAccounting = sourceApplication.IsAccounting;
            result.Applications.Add(application);
            index = index + 1;
        }

        result.TranslationHandlers.Clear();
        index = 0;
        while (index < source.TranslationHandlers.Count)
        {
            DiameterTranslationHandlerConfiguration sourceTranslation =
                source.TranslationHandlers[index];
            DiameterTranslationHandlerConfiguration translation =
                new DiameterTranslationHandlerConfiguration();
            translation.SourceApplicationId =
                sourceTranslation.SourceApplicationId;
            translation.TargetApplicationId =
                sourceTranslation.TargetApplicationId;
            translation.Handler = sourceTranslation.Handler;
            translation.Enabled = sourceTranslation.Enabled;
            result.TranslationHandlers.Add(translation);
            index = index + 1;
        }

        result.Routes.Clear();
        index = 0;
        while (index < source.Routes.Count)
        {
            result.Routes.Add(RouteTableCloner.CloneRouteConfiguration(
                source.Routes[index]));
            index = index + 1;
        }

        result.RoutePolicies.Clear();
        index = 0;
        while (index < source.RoutePolicies.Count)
        {
            result.RoutePolicies.Add(source.RoutePolicies[index]);
            index = index + 1;
        }

        result.Limits = CloneDiameterLimitConfiguration(source.Limits);
        result.Timers = CloneDiameterTimerConfiguration(source.Timers);
        result.Transport = CloneDiameterTransportConfiguration(
            source.Transport);
        result.Security = CloneDiameterSecurityConfiguration(
            source.Security);
        result.RealmRouting = CloneDiameterRealmRoutingConfiguration(
            source.RealmRouting);
        result.Overload =
            DiameterOverloadState.CloneDiameterOverloadConfiguration(
                source.Overload);
        result.RoutePathPolicy = source.RoutePathPolicy;
        result.RedirectHostPolicy = source.RedirectHostPolicy;
        result.RealmRedirectPolicy = source.RealmRedirectPolicy;
        result.TransportFactory = source.TransportFactory;
        result.Clock = source.Clock;
        result.IdentifierSource = source.IdentifierSource;
        result.SessionStore = source.SessionStore;
        result.AccountingRecordStore = source.AccountingRecordStore;
        result.CreditControlStore = source.CreditControlStore;
        result.DuplicateRequestStore = source.DuplicateRequestStore;
        result.SessionEventHandler = source.SessionEventHandler;
        result.AccountingHandler = source.AccountingHandler;
        result.CreditControlRequestHandler =
            source.CreditControlRequestHandler;
        result.CreditControlEventHandler =
            source.CreditControlEventHandler;
        result.OverloadSource = source.OverloadSource;
        result.EventSink = source.EventSink;
        result.RedactionPolicy = source.RedactionPolicy;
        return result;
    }

    internal static DiameterLimitConfiguration CloneDiameterLimitConfiguration(
        DiameterLimitConfiguration source)
    {
        DiameterLimitConfiguration result =
            new DiameterLimitConfiguration();
        result.MaximumMessageSize = source.MaximumMessageSize;
        result.MaximumAvpCount = source.MaximumAvpCount;
        result.MaximumGroupedDepth = source.MaximumGroupedDepth;
        result.MaximumGroupedAvpCount = source.MaximumGroupedAvpCount;
        result.MaximumGroupedBytes = source.MaximumGroupedBytes;
        result.MaximumCodecIssueCount = source.MaximumCodecIssueCount;
        result.MaximumConnectionCount = source.MaximumConnectionCount;
        result.MaximumPendingRequests = source.MaximumPendingRequests;
        result.IngressQueueCapacity = source.IngressQueueCapacity;
        result.EgressQueueCapacity = source.EgressQueueCapacity;
        result.ApplicationQueueCapacity = source.ApplicationQueueCapacity;
        return result;
    }

    internal static DiameterTimerConfiguration CloneDiameterTimerConfiguration(
        DiameterTimerConfiguration source)
    {
        DiameterTimerConfiguration result =
            new DiameterTimerConfiguration();
        result.ConnectionRetryMilliseconds =
            source.ConnectionRetryMilliseconds;
        result.ConnectionRetryJitterPercent =
            source.ConnectionRetryJitterPercent;
        result.WatchdogIntervalMilliseconds =
            source.WatchdogIntervalMilliseconds;
        result.WatchdogJitterMilliseconds =
            source.WatchdogJitterMilliseconds;
        result.RequestTimeoutMilliseconds =
            source.RequestTimeoutMilliseconds;
        result.ConnectTimeoutMilliseconds =
            source.ConnectTimeoutMilliseconds;
        result.DrainTimeoutMilliseconds =
            source.DrainTimeoutMilliseconds;
        result.DuplicateCacheLifetimeMilliseconds =
            source.DuplicateCacheLifetimeMilliseconds;
        result.RedirectCacheMaximumLifetimeMilliseconds =
            source.RedirectCacheMaximumLifetimeMilliseconds;
        result.SessionSweepMilliseconds = source.SessionSweepMilliseconds;
        return result;
    }

    internal static DiameterTransportConfiguration
        CloneDiameterTransportConfiguration(
            DiameterTransportConfiguration source)
    {
        DiameterTransportConfiguration result =
            new DiameterTransportConfiguration();
        result.Kind = source.Kind;
        result.Host = source.Host;
        result.Port = source.Port;
        result.EnableIpv4 = source.EnableIpv4;
        result.EnableIpv6 = source.EnableIpv6;
        result.EnableListener = source.EnableListener;
        result.EnableActiveConnections = source.EnableActiveConnections;
        result.NoDelay = source.NoDelay;
        result.KeepAlive = source.KeepAlive;
        result.ReceiveBufferSize = source.ReceiveBufferSize;
        result.SendBufferSize = source.SendBufferSize;
        result.ListenBacklog = source.ListenBacklog;
        result.SctpStreamCount = source.SctpStreamCount;
        return result;
    }

    internal static DiameterSecurityConfiguration
        CloneDiameterSecurityConfiguration(
            DiameterSecurityConfiguration source)
    {
        DiameterSecurityConfiguration result =
            new DiameterSecurityConfiguration();
        result.Mode = source.Mode;
        result.LocalCertificatePath = source.LocalCertificatePath;
        result.LocalCertificatePassword = source.LocalCertificatePassword;
        result.TrustAnchorPath = source.TrustAnchorPath;
        result.ExpectedPeerName = source.ExpectedPeerName;
        result.ExpectedPeerIdentity.Value =
            source.ExpectedPeerIdentity.Value;
        result.ExpectedPeerRealm.Value = source.ExpectedPeerRealm.Value;
        result.PeerAdmissionPolicy = source.PeerAdmissionPolicy;
        result.CertificatePeerPolicy = source.CertificatePeerPolicy;
        result.RequirePeerCertificate = source.RequirePeerCertificate;
        result.CheckCertificateRevocation =
            source.CheckCertificateRevocation;
        result.AllowUnsecuredTransport = source.AllowUnsecuredTransport;
        return result;
    }

    internal static DiameterRealmRoutingConfiguration
        CloneDiameterRealmRoutingConfiguration(
            DiameterRealmRoutingConfiguration source)
    {
        DiameterRealmRoutingConfiguration result =
            new DiameterRealmRoutingConfiguration();
        result.LocalRealms.Clear();
        int index = 0;
        while (index < source.LocalRealms.Count)
        {
            DiameterIdentity realm = new DiameterIdentity();
            realm.Value = source.LocalRealms[index].Value;
            result.LocalRealms.Add(realm);
            index = index + 1;
        }

        result.HomeRealms.Clear();
        index = 0;
        while (index < source.HomeRealms.Count)
        {
            DiameterIdentity realm = new DiameterIdentity();
            realm.Value = source.HomeRealms[index].Value;
            result.HomeRealms.Add(realm);
            index = index + 1;
        }

        result.EnableUserNameRouting = source.EnableUserNameRouting;
        result.OriginRealmIsHome = source.OriginRealmIsHome;
        result.RejectMalformedUserNames =
            source.RejectMalformedUserNames;
        return result;
    }
}
