using System;

namespace diameter.Tests;

public class ConfigurationDataTests
{
    [Test]
    public void ConfigurationDefaultsExposeBoundedRuntimeSettings()
    {
        DiameterNodeConfiguration node = new DiameterNodeConfiguration();
        DiameterLimitConfiguration limits = node.Limits;
        DiameterTimerConfiguration timers = node.Timers;
        DiameterTransportConfiguration transport = node.Transport;
        DiameterSecurityConfiguration security = node.Security;
        DiameterRealmRoutingConfiguration realmRouting = node.RealmRouting;

        Assert.Multiple(() =>
        {
            Assert.That(node.OriginHost.Value, Is.Empty);
            Assert.That(node.OriginRealm.Value, Is.Empty);
            Assert.That(node.ProductName, Is.Empty);
            Assert.That(node.VendorId, Is.Zero);
            Assert.That(node.FirmwareRevision, Is.Zero);
            Assert.That(node.Roles, Is.EqualTo(DiameterNodeRole.None));
            Assert.That(node.DefaultRoutingPriority,
                Is.EqualTo(DiameterRoutingPriority.Priority10));
            Assert.That(node.Applications, Is.Empty);
            Assert.That(node.TranslationHandlers, Is.Empty);
            Assert.That(node.Routes, Is.Empty);
            Assert.That(node.RoutePolicies, Is.Empty);
            Assert.That(node.RoutePathPolicy, Is.Null);
            Assert.That(node.RedirectHostPolicy, Is.Null);
            Assert.That(node.RealmRedirectPolicy, Is.Null);

            Assert.That(limits.MaximumMessageSize, Is.EqualTo(DiameterProtocolConstants.MaximumMessageLength));
            Assert.That(limits.MaximumAvpCount, Is.EqualTo(4096));
            Assert.That(limits.MaximumGroupedDepth, Is.EqualTo(16));
            Assert.That(limits.MaximumGroupedAvpCount, Is.EqualTo(1024));
            Assert.That(limits.MaximumGroupedBytes, Is.EqualTo(1048576));
            Assert.That(limits.MaximumCodecIssueCount, Is.EqualTo(128));
            Assert.That(limits.MaximumConnectionCount, Is.EqualTo(1024));
            Assert.That(limits.MaximumPendingRequests, Is.EqualTo(4096));
            Assert.That(limits.IngressQueueCapacity, Is.EqualTo(1024));
            Assert.That(limits.EgressQueueCapacity, Is.EqualTo(1024));
            Assert.That(limits.ApplicationQueueCapacity, Is.EqualTo(1024));

            Assert.That(timers.ConnectionRetryMilliseconds, Is.EqualTo(30000));
            Assert.That(timers.ConnectionRetryJitterPercent, Is.EqualTo(10));
            Assert.That(timers.WatchdogIntervalMilliseconds, Is.EqualTo(30000));
            Assert.That(timers.WatchdogJitterMilliseconds, Is.EqualTo(2000));
            Assert.That(timers.RequestTimeoutMilliseconds, Is.EqualTo(30000));
            Assert.That(timers.ConnectTimeoutMilliseconds, Is.EqualTo(10000));
            Assert.That(timers.DrainTimeoutMilliseconds, Is.EqualTo(30000));
            Assert.That(timers.DuplicateCacheLifetimeMilliseconds, Is.EqualTo(240000));
            Assert.That(timers.RedirectCacheMaximumLifetimeMilliseconds, Is.EqualTo(86400000));
            Assert.That(timers.SessionSweepMilliseconds, Is.EqualTo(1000));

            Assert.That(transport.Kind, Is.EqualTo(DiameterTransportKind.Tcp));
            Assert.That(transport.Host, Is.Empty);
            Assert.That(transport.Port, Is.EqualTo(DiameterProtocolConstants.Port));
            Assert.That(transport.EnableIpv4, Is.True);
            Assert.That(transport.EnableIpv6, Is.True);
            Assert.That(transport.EnableListener, Is.False);
            Assert.That(transport.EnableActiveConnections, Is.True);
            Assert.That(transport.NoDelay, Is.True);
            Assert.That(transport.KeepAlive, Is.True);
            Assert.That(transport.ReceiveBufferSize, Is.EqualTo(65536));
            Assert.That(transport.SendBufferSize, Is.EqualTo(65536));
            Assert.That(transport.ListenBacklog, Is.EqualTo(128));
            Assert.That(transport.SctpStreamCount, Is.EqualTo(1));

            Assert.That(security.Mode, Is.EqualTo(DiameterSecurityMode.None));
            Assert.That(security.LocalCertificatePath, Is.Empty);
            Assert.That(security.LocalCertificatePassword, Is.Empty);
            Assert.That(security.TrustAnchorPath, Is.Empty);
            Assert.That(security.ExpectedPeerName, Is.Empty);
            Assert.That(security.ExpectedPeerIdentity.Value, Is.Empty);
            Assert.That(security.ExpectedPeerRealm.Value, Is.Empty);
            Assert.That(security.PeerAdmissionPolicy, Is.Null);
            Assert.That(security.CertificatePeerPolicy, Is.Null);
            Assert.That(security.RequirePeerCertificate, Is.False);
            Assert.That(security.CheckCertificateRevocation, Is.False);
            Assert.That(security.AllowUnsecuredTransport, Is.False);
            Assert.That(realmRouting.LocalRealms, Is.Empty);
            Assert.That(realmRouting.HomeRealms, Is.Empty);
            Assert.That(realmRouting.EnableUserNameRouting, Is.True);
            Assert.That(realmRouting.OriginRealmIsHome, Is.True);
            Assert.That(realmRouting.RejectMalformedUserNames, Is.True);
        });
    }

    [Test]
    public void CompleteValidConfigurationsPassEagerValidation()
    {
        DiameterNodeConfiguration unsecured = CreateValidConfiguration();

        DiameterNodeConfiguration tls = CreateValidConfiguration();
        tls.Roles = DiameterNodeRole.Server;
        tls.Transport.EnableListener = true;
        tls.Security.Mode = DiameterSecurityMode.Tls;
        tls.Security.AllowUnsecuredTransport = false;
        tls.Security.LocalCertificatePath = "local.pfx";
        tls.Security.LocalCertificatePassword = "password";
        tls.Security.TrustAnchorPath = "roots.pem";
        tls.Security.ExpectedPeerName = "peer.example";
        tls.Security.RequirePeerCertificate = true;
        tls.Security.CheckCertificateRevocation = true;

        DiameterNodeConfiguration dtls = CreateValidConfiguration();
        dtls.Transport.Kind = DiameterTransportKind.Sctp;
        dtls.Transport.SctpStreamCount = 4;
        dtls.Security.Mode = DiameterSecurityMode.Dtls;
        dtls.Security.AllowUnsecuredTransport = false;
        dtls.Security.LocalCertificatePath = "local.pfx";
        dtls.Security.RequirePeerCertificate = true;

        DiameterNodeConfiguration external = CreateValidConfiguration();
        external.Transport.Kind = DiameterTransportKind.External;
        external.Transport.Port = 0;
        external.Security.Mode = DiameterSecurityMode.External;
        external.Security.AllowUnsecuredTransport = false;

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(unsecured.ValidateConfiguration);
            Assert.DoesNotThrow(tls.ValidateConfiguration);
            Assert.DoesNotThrow(dtls.ValidateConfiguration);
            Assert.DoesNotThrow(external.ValidateConfiguration);
        });
    }

    [Test]
    public void TranslationRegistrationsRequireAdvertisedDistinctPairs()
    {
        DiameterNodeConfiguration valid = CreateValidConfiguration();
        AddConfigurationApplication(valid, 4U);
        AddConfigurationApplication(valid, 9U);
        valid.TranslationHandlers.Add(CreateTranslationRegistration(
            4U,
            9U));
        DiameterNodeConfiguration duplicate = CreateValidConfiguration();
        AddConfigurationApplication(duplicate, 4U);
        AddConfigurationApplication(duplicate, 9U);
        duplicate.TranslationHandlers.Add(CreateTranslationRegistration(
            4U,
            9U));
        duplicate.TranslationHandlers.Add(CreateTranslationRegistration(
            4U,
            9U));
        DiameterNodeConfiguration unadvertised = CreateValidConfiguration();
        AddConfigurationApplication(unadvertised, 4U);
        unadvertised.TranslationHandlers.Add(CreateTranslationRegistration(
            4U,
            9U));
        DiameterNodeConfiguration sameApplication =
            CreateValidConfiguration();
        AddConfigurationApplication(sameApplication, 4U);
        sameApplication.TranslationHandlers.Add(CreateTranslationRegistration(
            4U,
            4U));
        DiameterNodeConfiguration missingHandler =
            CreateValidConfiguration();
        AddConfigurationApplication(missingHandler, 4U);
        AddConfigurationApplication(missingHandler, 9U);
        DiameterTranslationHandlerConfiguration missing =
            new DiameterTranslationHandlerConfiguration();
        missing.SourceApplicationId = 4U;
        missing.TargetApplicationId = 9U;
        missingHandler.TranslationHandlers.Add(missing);

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(valid.ValidateConfiguration);
            Assert.Throws<ArgumentException>(duplicate.ValidateConfiguration);
            Assert.Throws<ArgumentException>(unadvertised.ValidateConfiguration);
            Assert.Throws<ArgumentException>(
                sameApplication.ValidateConfiguration);
            Assert.Throws<ArgumentException>(
                missingHandler.ValidateConfiguration);
        });
    }

    [Test]
    public void LimitValidationRejectsInvalidBoundariesAndRelationships()
    {
        DiameterLimitConfiguration tooSmall = new DiameterLimitConfiguration();
        tooSmall.MaximumMessageSize = DiameterProtocolConstants.MinimumMessageLength - 1;

        DiameterLimitConfiguration tooLarge = new DiameterLimitConfiguration();
        tooLarge.MaximumMessageSize = DiameterProtocolConstants.MaximumMessageLength + 4;

        DiameterLimitConfiguration misaligned = new DiameterLimitConfiguration();
        misaligned.MaximumMessageSize = DiameterProtocolConstants.MinimumMessageLength + 1;

        DiameterLimitConfiguration nonPositive = new DiameterLimitConfiguration();
        nonPositive.MaximumPendingRequests = 0;

        DiameterLimitConfiguration groupedBytes = new DiameterLimitConfiguration();
        groupedBytes.MaximumMessageSize = 1024;
        groupedBytes.MaximumGroupedBytes = 1028;

        DiameterLimitConfiguration groupedCount = new DiameterLimitConfiguration();
        groupedCount.MaximumAvpCount = 10;
        groupedCount.MaximumGroupedAvpCount = 11;

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(new DiameterLimitConfiguration().ValidateConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(tooSmall.ValidateConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(tooLarge.ValidateConfiguration);
            Assert.Throws<ArgumentException>(misaligned.ValidateConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(nonPositive.ValidateConfiguration);
            Assert.Throws<ArgumentException>(groupedBytes.ValidateConfiguration);
            Assert.Throws<ArgumentException>(groupedCount.ValidateConfiguration);
        });
    }

    [Test]
    public void TimerValidationRejectsNonPositiveAndUnsafeRelationships()
    {
        DiameterTimerConfiguration nonPositive = new DiameterTimerConfiguration();
        nonPositive.WatchdogIntervalMilliseconds = 0;

        DiameterTimerConfiguration tooShortWatchdog =
            new DiameterTimerConfiguration();
        tooShortWatchdog.WatchdogIntervalMilliseconds = 5999;

        DiameterTimerConfiguration shortDuplicateLifetime = new DiameterTimerConfiguration();
        shortDuplicateLifetime.RequestTimeoutMilliseconds = 60000;
        shortDuplicateLifetime.DuplicateCacheLifetimeMilliseconds = 59999;

        DiameterTimerConfiguration boundary = new DiameterTimerConfiguration();
        boundary.RequestTimeoutMilliseconds = 60000;
        boundary.DuplicateCacheLifetimeMilliseconds = 60000;

        DiameterTimerConfiguration negativeJitter = new DiameterTimerConfiguration();
        negativeJitter.ConnectionRetryJitterPercent = -1;

        DiameterTimerConfiguration excessiveJitter = new DiameterTimerConfiguration();
        excessiveJitter.ConnectionRetryJitterPercent = 101;

        DiameterTimerConfiguration negativeWatchdogJitter =
            new DiameterTimerConfiguration();
        negativeWatchdogJitter.WatchdogJitterMilliseconds = -1;

        DiameterTimerConfiguration excessiveWatchdogJitter =
            new DiameterTimerConfiguration();
        excessiveWatchdogJitter.WatchdogJitterMilliseconds = 60001;

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(new DiameterTimerConfiguration().ValidateConfiguration);
            Assert.DoesNotThrow(boundary.ValidateConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(nonPositive.ValidateConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(
                tooShortWatchdog.ValidateConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(negativeJitter.ValidateConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(excessiveJitter.ValidateConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(
                negativeWatchdogJitter.ValidateConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(
                excessiveWatchdogJitter.ValidateConfiguration);
            Assert.Throws<ArgumentException>(shortDuplicateLifetime.ValidateConfiguration);
        });
    }

    [Test]
    public void TransportValidationRejectsInvalidEndpointsAndSizes()
    {
        DiameterTransportConfiguration invalidPort = new DiameterTransportConfiguration();
        invalidPort.Port = 0;

        DiameterTransportConfiguration noAddressFamily = new DiameterTransportConfiguration();
        noAddressFamily.EnableIpv4 = false;
        noAddressFamily.EnableIpv6 = false;

        DiameterTransportConfiguration smallBuffer = new DiameterTransportConfiguration();
        smallBuffer.ReceiveBufferSize = DiameterProtocolConstants.MessageHeaderSize - 1;

        DiameterTransportConfiguration invalidStreamCount = new DiameterTransportConfiguration();
        invalidStreamCount.SctpStreamCount = 2;

        DiameterTransportConfiguration sctp = new DiameterTransportConfiguration();
        sctp.Kind = DiameterTransportKind.Sctp;
        sctp.SctpStreamCount = 8;

        DiameterTransportConfiguration excessiveSctpStreams =
            new DiameterTransportConfiguration();
        excessiveSctpStreams.Kind = DiameterTransportKind.Sctp;
        excessiveSctpStreams.SctpStreamCount = ushort.MaxValue + 1;

        DiameterTransportConfiguration external = new DiameterTransportConfiguration();
        external.Kind = DiameterTransportKind.External;
        external.Port = 0;
        external.EnableIpv4 = false;
        external.EnableIpv6 = false;

        DiameterTransportConfiguration invalidKind = new DiameterTransportConfiguration();
        invalidKind.Kind = (DiameterTransportKind)99;

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(new DiameterTransportConfiguration().ValidateConfiguration);
            Assert.DoesNotThrow(sctp.ValidateConfiguration);
            Assert.DoesNotThrow(external.ValidateConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(invalidPort.ValidateConfiguration);
            Assert.Throws<ArgumentException>(noAddressFamily.ValidateConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(smallBuffer.ValidateConfiguration);
            Assert.Throws<ArgumentException>(invalidStreamCount.ValidateConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(
                excessiveSctpStreams.ValidateConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(invalidKind.ValidateConfiguration);
        });
    }

    [Test]
    public void SecurityValidationEnforcesMutualAuthenticationAndExplicitInsecureUse()
    {
        DiameterSecurityConfiguration unsecured = new DiameterSecurityConfiguration();
        unsecured.AllowUnsecuredTransport = true;

        DiameterSecurityConfiguration defaultSecurity = new DiameterSecurityConfiguration();

        DiameterSecurityConfiguration missingCertificate = new DiameterSecurityConfiguration();
        missingCertificate.Mode = DiameterSecurityMode.Tls;
        missingCertificate.RequirePeerCertificate = true;

        DiameterSecurityConfiguration missingPeerCertificate = new DiameterSecurityConfiguration();
        missingPeerCertificate.Mode = DiameterSecurityMode.Tls;
        missingPeerCertificate.LocalCertificatePath = "local.pfx";

        DiameterSecurityConfiguration passwordWithoutCertificate = new DiameterSecurityConfiguration();
        passwordWithoutCertificate.AllowUnsecuredTransport = true;
        passwordWithoutCertificate.LocalCertificatePassword = "password";

        DiameterSecurityConfiguration certificateWithIpsec = new DiameterSecurityConfiguration();
        certificateWithIpsec.Mode = DiameterSecurityMode.Ipsec;
        certificateWithIpsec.LocalCertificatePath = "local.pfx";

        DiameterSecurityConfiguration validTls = new DiameterSecurityConfiguration();
        validTls.Mode = DiameterSecurityMode.Tls;
        validTls.LocalCertificatePath = "local.pfx";
        validTls.RequirePeerCertificate = true;

        DiameterSecurityConfiguration invalidMode = new DiameterSecurityConfiguration();
        invalidMode.Mode = (DiameterSecurityMode)99;

        DiameterSecurityConfiguration invalidPeerIdentity =
            new DiameterSecurityConfiguration();
        invalidPeerIdentity.AllowUnsecuredTransport = true;
        invalidPeerIdentity.ExpectedPeerIdentity.Value = "bad_name.example";

        DiameterSecurityConfiguration invalidPeerRealm =
            new DiameterSecurityConfiguration();
        invalidPeerRealm.AllowUnsecuredTransport = true;
        invalidPeerRealm.ExpectedPeerRealm.Value = "bad realm";

        DiameterSecurityConfiguration certificatePolicyWithoutTls =
            new DiameterSecurityConfiguration();
        certificatePolicyWithoutTls.AllowUnsecuredTransport = true;
        certificatePolicyWithoutTls.CertificatePeerPolicy =
            new RecordingCertificatePeerPolicy();

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(unsecured.ValidateConfiguration);
            Assert.DoesNotThrow(validTls.ValidateConfiguration);
            Assert.Throws<ArgumentException>(defaultSecurity.ValidateConfiguration);
            Assert.Throws<ArgumentException>(missingCertificate.ValidateConfiguration);
            Assert.Throws<ArgumentException>(missingPeerCertificate.ValidateConfiguration);
            Assert.Throws<ArgumentException>(passwordWithoutCertificate.ValidateConfiguration);
            Assert.Throws<ArgumentException>(certificateWithIpsec.ValidateConfiguration);
            Assert.Throws<ArgumentException>(invalidPeerIdentity.ValidateConfiguration);
            Assert.Throws<ArgumentException>(invalidPeerRealm.ValidateConfiguration);
            Assert.Throws<ArgumentException>(
                certificatePolicyWithoutTls.ValidateConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(invalidMode.ValidateConfiguration);
        });
    }

    [Test]
    public void PeerAdmissionDataDefaultsAreIndependentAndDenyByDefault()
    {
        DiameterPeerAdmissionRequest first = new DiameterPeerAdmissionRequest();
        DiameterPeerAdmissionRequest second = new DiameterPeerAdmissionRequest();
        DiameterPeerAdmissionResult decision = new DiameterPeerAdmissionResult();
        first.PeerIdentity.Value = "peer.example";
        first.PeerRealm.Value = "example";
        first.Connection.ConnectionId = "connection";

        Assert.Multiple(() =>
        {
            Assert.That(first.PeerIdentity, Is.Not.SameAs(second.PeerIdentity));
            Assert.That(first.PeerRealm, Is.Not.SameAs(second.PeerRealm));
            Assert.That(first.Connection, Is.Not.SameAs(second.Connection));
            Assert.That(second.PeerIdentity.Value, Is.Empty);
            Assert.That(second.PeerRealm.Value, Is.Empty);
            Assert.That(second.Connection.ConnectionId, Is.Empty);
            Assert.That(decision.Admitted, Is.False);
            Assert.That(decision.Code, Is.Empty);
            Assert.That(decision.Message, Is.Empty);
        });
    }

    [Test]
    public void NodeValidationRejectsRoleApplicationAndSecurityConflicts()
    {
        DiameterNodeConfiguration missingIdentity = CreateValidConfiguration();
        missingIdentity.OriginHost.Value = "";

        DiameterNodeConfiguration inactiveClient = CreateValidConfiguration();
        inactiveClient.Transport.EnableActiveConnections = false;

        DiameterNodeConfiguration serverWithoutListener = CreateValidConfiguration();
        serverWithoutListener.Roles = DiameterNodeRole.Server;

        DiameterNodeConfiguration tlsOverSctp = CreateValidConfiguration();
        tlsOverSctp.Transport.Kind = DiameterTransportKind.Sctp;
        tlsOverSctp.Security.Mode = DiameterSecurityMode.Tls;
        tlsOverSctp.Security.AllowUnsecuredTransport = false;
        tlsOverSctp.Security.LocalCertificatePath = "local.pfx";
        tlsOverSctp.Security.RequirePeerCertificate = true;

        DiameterNodeConfiguration duplicateApplications = CreateValidConfiguration();
        DiameterApplicationId first = new DiameterApplicationId();
        first.Id = DiameterApplicationIds.CreditControl;
        DiameterApplicationId second = new DiameterApplicationId();
        second.Id = DiameterApplicationIds.CreditControl;
        duplicateApplications.Applications.Add(first);
        duplicateApplications.Applications.Add(second);

        DiameterNodeConfiguration invalidRoles = CreateValidConfiguration();
        invalidRoles.Roles = (DiameterNodeRole)64;

        DiameterNodeConfiguration invalidRoutingPriority =
            CreateValidConfiguration();
        invalidRoutingPriority.DefaultRoutingPriority =
            (DiameterRoutingPriority)16;

        DiameterNodeConfiguration duplicateRoutes =
            CreateValidConfiguration();
        duplicateRoutes.Routes.Add(CreateConfigurationRoute("route"));
        duplicateRoutes.Routes.Add(CreateConfigurationRoute("ROUTE"));

        DiameterNodeConfiguration nullPolicy = CreateValidConfiguration();
        nullPolicy.RoutePolicies.Add(null!);

        DiameterNodeConfiguration missingRealmRouting = CreateValidConfiguration();
        missingRealmRouting.RealmRouting = null!;

        DiameterNodeConfiguration invalidLocalRealm = CreateValidConfiguration();
        DiameterIdentity invalidRealm = new DiameterIdentity();
        invalidRealm.Value = "invalid realm";
        invalidLocalRealm.RealmRouting.LocalRealms.Add(invalidRealm);

        DiameterNodeConfiguration duplicateHomeRealm = CreateValidConfiguration();
        DiameterIdentity homeRealm = new DiameterIdentity();
        homeRealm.Value = "home.example";
        DiameterIdentity duplicateHome = new DiameterIdentity();
        duplicateHome.Value = "HOME.EXAMPLE";
        duplicateHomeRealm.RealmRouting.HomeRealms.Add(homeRealm);
        duplicateHomeRealm.RealmRouting.HomeRealms.Add(duplicateHome);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(missingIdentity.ValidateConfiguration);
            Assert.Throws<ArgumentException>(inactiveClient.ValidateConfiguration);
            Assert.Throws<ArgumentException>(serverWithoutListener.ValidateConfiguration);
            Assert.Throws<ArgumentException>(tlsOverSctp.ValidateConfiguration);
            Assert.Throws<ArgumentException>(duplicateApplications.ValidateConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(invalidRoles.ValidateConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidRoutingPriority.ValidateConfiguration);
            Assert.Throws<ArgumentException>(
                duplicateRoutes.ValidateConfiguration);
            Assert.Throws<ArgumentException>(
                nullPolicy.ValidateConfiguration);
            Assert.Throws<ArgumentException>(missingRealmRouting.ValidateConfiguration);
            Assert.Throws<ArgumentException>(invalidLocalRealm.ValidateConfiguration);
            Assert.Throws<ArgumentException>(duplicateHomeRealm.ValidateConfiguration);
        });
    }

    [Test]
    public void ConfigurationStorageIsIndependentAndCanBeAttachedToEvents()
    {
        DiameterNodeConfiguration first = CreateValidConfiguration();
        DiameterNodeConfiguration second = CreateValidConfiguration();
        first.Limits.MaximumPendingRequests = 7;
        first.Applications.Add(new DiameterApplicationId());
        DiameterIdentity localRealm = new DiameterIdentity();
        localRealm.Value = "transit.example";
        first.RealmRouting.LocalRealms.Add(localRealm);

        DiameterOperationalEvent eventData = new DiameterOperationalEvent();
        eventData.Configuration = first;

        Assert.Multiple(() =>
        {
            Assert.That(second.Limits.MaximumPendingRequests, Is.EqualTo(4096));
            Assert.That(second.Applications, Is.Empty);
            Assert.That(first.Limits, Is.Not.SameAs(second.Limits));
            Assert.That(first.Timers, Is.Not.SameAs(second.Timers));
            Assert.That(first.Transport, Is.Not.SameAs(second.Transport));
            Assert.That(first.Security, Is.Not.SameAs(second.Security));
            Assert.That(first.RealmRouting, Is.Not.SameAs(second.RealmRouting));
            Assert.That(second.RealmRouting.LocalRealms, Is.Empty);
            Assert.That(first.Applications, Is.Not.SameAs(second.Applications));
            Assert.That(eventData.Configuration, Is.SameAs(first));
        });
    }

    [Test]
    public void NodeConfigurationRejectsEveryMissingCollectionAndEntry()
    {
        AssertEveryMutationIsRejected(
            CreateValidConfiguration,
            value => value.ValidateConfiguration(),
            value => value.OriginHost = null!,
            value => value.OriginRealm = null!,
            value => value.OriginRealm.Value = " ",
            value => value.ProductName = " ",
            value => value.Roles = DiameterNodeRole.None,
            value => value.DefaultRoutingPriority =
                (DiameterRoutingPriority)(-1),
            value => value.Applications = null!,
            value => value.TranslationHandlers = null!,
            value => value.Routes = null!,
            value => value.RoutePolicies = null!,
            value => value.Applications.Add(null!),
            value =>
            {
                AddConfigurationApplication(value, 4U);
                value.Applications.Add(null!);
            },
            value => value.TranslationHandlers.Add(null!),
            value =>
            {
                AddConfigurationApplication(value, 4U);
                AddConfigurationApplication(value, 9U);
                value.TranslationHandlers.Add(
                    CreateTranslationRegistration(4U, 9U));
                value.TranslationHandlers.Add(null!);
            },
            value => value.Routes.Add(null!),
            value =>
            {
                value.Routes.Add(CreateConfigurationRoute("route"));
                value.Routes.Add(null!);
            });

        DiameterNodeConfiguration missingSource =
            CreateValidConfiguration();
        AddConfigurationApplication(missingSource, 9U);
        missingSource.TranslationHandlers.Add(
            CreateTranslationRegistration(4U, 9U));
        DiameterNodeConfiguration missingTarget =
            CreateValidConfiguration();
        AddConfigurationApplication(missingTarget, 4U);
        missingTarget.TranslationHandlers.Add(
            CreateTranslationRegistration(4U, 9U));
        DiameterNodeConfiguration dtlsOverTcp =
            CreateValidConfiguration();
        dtlsOverTcp.Security.Mode = DiameterSecurityMode.Dtls;
        dtlsOverTcp.Security.AllowUnsecuredTransport = false;
        dtlsOverTcp.Security.LocalCertificatePath = "local.pfx";
        dtlsOverTcp.Security.RequirePeerCertificate = true;
        DiameterNodeConfiguration externalOverTcp =
            CreateValidConfiguration();
        externalOverTcp.Security.Mode = DiameterSecurityMode.External;
        externalOverTcp.Security.AllowUnsecuredTransport = false;
        DiameterNodeConfiguration distinctTranslations =
            CreateValidConfiguration();
        AddConfigurationApplication(distinctTranslations, 4U);
        AddConfigurationApplication(distinctTranslations, 9U);
        AddConfigurationApplication(distinctTranslations, 10U);
        distinctTranslations.TranslationHandlers.Add(
            CreateTranslationRegistration(4U, 9U));
        distinctTranslations.TranslationHandlers.Add(
            CreateTranslationRegistration(9U, 10U));

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(
                missingSource.ValidateConfiguration);
            Assert.Throws<ArgumentException>(
                missingTarget.ValidateConfiguration);
            Assert.Throws<ArgumentException>(
                dtlsOverTcp.ValidateConfiguration);
            Assert.Throws<ArgumentException>(
                externalOverTcp.ValidateConfiguration);
            Assert.DoesNotThrow(
                distinctTranslations.ValidateConfiguration);
        });
    }

    [Test]
    public void RealmRoutingRejectsEveryMissingOrInvalidRealmEntry()
    {
        DiameterIdentity origin = new DiameterIdentity();
        origin.Value = "example";
        Func<DiameterRealmRoutingConfiguration> factory =
            () => new DiameterRealmRoutingConfiguration();

        AssertEveryMutationIsRejected(
            factory,
            value => value.ValidateRealmRoutingConfiguration(origin),
            value => value.LocalRealms = null!,
            value => value.HomeRealms = null!);

        DiameterRealmRoutingConfiguration missingOrigin = factory();
        DiameterRealmRoutingConfiguration invalidOrigin = factory();
        DiameterIdentity malformed = new DiameterIdentity();
        malformed.Value = "bad realm";
        DiameterRealmRoutingConfiguration invalidComparison = factory();
        DiameterIdentity valid = new DiameterIdentity();
        valid.Value = "valid.example";
        invalidComparison.LocalRealms.Add(valid);
        invalidComparison.LocalRealms.Add(malformed);
        DiameterRealmRoutingConfiguration distinctRealms = factory();
        DiameterIdentity firstRealm = new DiameterIdentity();
        firstRealm.Value = "first.example";
        DiameterIdentity secondRealm = new DiameterIdentity();
        secondRealm.Value = "second.example";
        distinctRealms.LocalRealms.Add(firstRealm);
        distinctRealms.LocalRealms.Add(secondRealm);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                missingOrigin.ValidateRealmRoutingConfiguration(null!));
            Assert.Throws<ArgumentException>(() =>
                invalidOrigin.ValidateRealmRoutingConfiguration(malformed));
            Assert.Throws<ArgumentException>(() =>
                invalidComparison.ValidateRealmRoutingConfiguration(origin));
            Assert.DoesNotThrow(() =>
                distinctRealms.ValidateRealmRoutingConfiguration(origin));
        });
    }

    [Test]
    public void TransportAndSecurityRejectRemainingBoundaryCombinations()
    {
        DiameterTransportConfiguration externalNegative =
            new DiameterTransportConfiguration();
        externalNegative.Kind = DiameterTransportKind.External;
        externalNegative.Port = -1;
        DiameterTransportConfiguration externalExcessive =
            new DiameterTransportConfiguration();
        externalExcessive.Kind = DiameterTransportKind.External;
        externalExcessive.Port = 65536;

        DiameterSecurityConfiguration nullIdentity =
            CreateUnsecuredSecurity();
        nullIdentity.ExpectedPeerIdentity = null!;
        DiameterSecurityConfiguration nullRealm =
            CreateUnsecuredSecurity();
        nullRealm.ExpectedPeerRealm = null!;
        DiameterSecurityConfiguration unsecuredTls =
            new DiameterSecurityConfiguration();
        unsecuredTls.Mode = DiameterSecurityMode.Tls;
        unsecuredTls.LocalCertificatePath = "local.pfx";
        unsecuredTls.RequirePeerCertificate = true;
        unsecuredTls.AllowUnsecuredTransport = true;
        DiameterSecurityConfiguration unsecuredIpsec =
            new DiameterSecurityConfiguration();
        unsecuredIpsec.Mode = DiameterSecurityMode.Ipsec;
        unsecuredIpsec.AllowUnsecuredTransport = true;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                externalNegative.ValidateConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(
                externalExcessive.ValidateConfiguration);
            Assert.Throws<ArgumentException>(
                nullIdentity.ValidateConfiguration);
            Assert.Throws<ArgumentException>(
                nullRealm.ValidateConfiguration);
            Assert.Throws<ArgumentException>(
                unsecuredTls.ValidateConfiguration);
            Assert.Throws<ArgumentException>(
                unsecuredIpsec.ValidateConfiguration);
        });
    }

    private static DiameterNodeConfiguration CreateValidConfiguration()
    {
        DiameterNodeConfiguration configuration = new DiameterNodeConfiguration();
        configuration.OriginHost.Value = "node.example";
        configuration.OriginRealm.Value = "example";
        configuration.ProductName = "diameter.Tests";
        configuration.Roles = DiameterNodeRole.Client;
        configuration.Security.AllowUnsecuredTransport = true;
        return configuration;
    }

    private static DiameterSecurityConfiguration CreateUnsecuredSecurity()
    {
        DiameterSecurityConfiguration result =
            new DiameterSecurityConfiguration();
        result.AllowUnsecuredTransport = true;
        return result;
    }

    private static void AssertEveryMutationIsRejected<T>(
        Func<T> factory,
        Action<T> validate,
        params Action<T>[] mutations)
    {
        int index = 0;
        while (index < mutations.Length)
        {
            T value = factory();
            mutations[index](value);
            Assert.That(
                new TestDelegate(() => validate(value)),
                Throws.InstanceOf<ArgumentException>(),
                $"Mutation {index} was accepted.");
            index = index + 1;
        }
    }

    private static void AddConfigurationApplication(
        DiameterNodeConfiguration configuration,
        uint applicationId)
    {
        DiameterApplicationId application = new DiameterApplicationId();
        application.Id = applicationId;
        configuration.Applications.Add(application);
    }

    private static DiameterTranslationHandlerConfiguration
        CreateTranslationRegistration(
            uint sourceApplicationId,
            uint targetApplicationId)
    {
        DiameterTranslationHandlerConfiguration result =
            new DiameterTranslationHandlerConfiguration();
        result.SourceApplicationId = sourceApplicationId;
        result.TargetApplicationId = targetApplicationId;
        result.Handler = new TranslationContractProbe();
        return result;
    }

    private static DiameterRouteConfiguration CreateConfigurationRoute(
        string identifier)
    {
        DiameterRouteConfiguration result =
            new DiameterRouteConfiguration();
        result.RouteIdentifier = identifier;
        result.DestinationRealm.Value = "example";
        result.ApplicationId = 4U;
        result.Action = DiameterRouteAction.Local;
        result.AgentRole = DiameterAgentRole.None;
        return result;
    }
}
