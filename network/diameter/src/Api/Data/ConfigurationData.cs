using System;
using System.Collections.Generic;

namespace diameter;

/// <summary>
/// Contains the top-level configuration of a Diameter node.
/// </summary>
public class DiameterNodeConfiguration
{
    /// <summary>The node's Origin-Host identity.</summary>
    public DiameterIdentity OriginHost = new DiameterIdentity();

    /// <summary>The node's Origin-Realm identity.</summary>
    public DiameterIdentity OriginRealm = new DiameterIdentity();

    /// <summary>The product name advertised during capabilities exchange.</summary>
    public string ProductName = string.Empty;

    /// <summary>The vendor identifier advertised by the node.</summary>
    public uint VendorId;

    /// <summary>The firmware revision advertised by the node.</summary>
    public uint FirmwareRevision;

    /// <summary>The independently enabled node roles.</summary>
    public DiameterNodeRole Roles;

    /// <summary>The RFC 7944 priority applied when a transaction has no DRMP AVP.</summary>
    public DiameterRoutingPriority DefaultRoutingPriority =
        DiameterRoutingPriority.Priority10;

    /// <summary>The applications advertised by the node.</summary>
    public List<DiameterApplicationId> Applications = new List<DiameterApplicationId>();

    /// <summary>The application-pair translation handlers.</summary>
    public List<DiameterTranslationHandlerConfiguration> TranslationHandlers =
        new List<DiameterTranslationHandlerConfiguration>();

    /// <summary>The ordered static routes owned by the node.</summary>
    public List<DiameterRouteConfiguration> Routes =
        new List<DiameterRouteConfiguration>();

    /// <summary>The ordered policies applied to each route candidate.</summary>
    public List<IRoutePolicy> RoutePolicies = new List<IRoutePolicy>();

    /// <summary>The resource and codec limits.</summary>
    public DiameterLimitConfiguration Limits = new DiameterLimitConfiguration();

    /// <summary>The node timer settings.</summary>
    public DiameterTimerConfiguration Timers = new DiameterTimerConfiguration();

    /// <summary>The transport settings.</summary>
    public DiameterTransportConfiguration Transport = new DiameterTransportConfiguration();

    /// <summary>The transport security settings.</summary>
    public DiameterSecurityConfiguration Security = new DiameterSecurityConfiguration();

    /// <summary>The local, home, and User-Name realm-routing policy.</summary>
    public DiameterRealmRoutingConfiguration RealmRouting =
        new DiameterRealmRoutingConfiguration();

    /// <summary>The overload-control and load-conveyance settings.</summary>
    public DiameterOverloadConfiguration Overload =
        new DiameterOverloadConfiguration();

    /// <summary>The optional policy that authorizes Route-Record paths.</summary>
    public IRoutePathPolicy? RoutePathPolicy;

    /// <summary>The optional policy that authorizes redirect targets.</summary>
    public IRedirectHostPolicy? RedirectHostPolicy;

    /// <summary>The optional policy that authorizes realm redirect routes.</summary>
    public IRealmRedirectPolicy? RealmRedirectPolicy;

    /// <summary>The optional caller-supplied transport factory.</summary>
    public IDiameterTransportFactory? TransportFactory;

    /// <summary>The optional caller-supplied UTC and monotonic clock.</summary>
    public IDiameterClock? Clock;

    /// <summary>The optional caller-supplied protocol identifier source.</summary>
    public IDiameterIdentifierSource? IdentifierSource;

    /// <summary>The optional caller-supplied authorization-session store.</summary>
    public IDiameterSessionStore? SessionStore;

    /// <summary>The optional caller-supplied accounting record store.</summary>
    public IAccountingRecordStore? AccountingRecordStore;

    /// <summary>The optional caller-supplied credit-control store.</summary>
    public ICreditControlStore? CreditControlStore;

    /// <summary>The optional caller-supplied duplicate-request store.</summary>
    public IDuplicateRequestStore? DuplicateRequestStore;

    /// <summary>The optional authorization-session event handler.</summary>
    public ISessionEventHandler? SessionEventHandler;

    /// <summary>The optional inbound accounting request handler.</summary>
    public IDiameterAccountingHandler? AccountingHandler;

    /// <summary>The optional inbound credit-control request handler.</summary>
    public ICreditControlRequestHandler? CreditControlRequestHandler;

    /// <summary>The optional credit-control lifecycle event handler.</summary>
    public ICreditControlEventHandler? CreditControlEventHandler;

    /// <summary>The optional local overload and load source.</summary>
    public IOverloadSource? OverloadSource;

    /// <summary>The optional structured operational event sink.</summary>
    public IDiameterEventSink? EventSink;

    /// <summary>The optional operational event redaction policy.</summary>
    public IRedactionPolicy? RedactionPolicy;

    internal void ValidateConfiguration()
    {
        if (OriginHost == null || string.IsNullOrWhiteSpace(OriginHost.Value))
        {
            throw new ArgumentException("OriginHost must contain a Diameter identity.");
        }

        if (OriginRealm == null || string.IsNullOrWhiteSpace(OriginRealm.Value))
        {
            throw new ArgumentException("OriginRealm must contain a Diameter identity.");
        }

        if (string.IsNullOrWhiteSpace(ProductName))
        {
            throw new ArgumentException("ProductName must not be empty.");
        }

        int roleValue = (int)Roles;
        if (roleValue == 0 || (roleValue & -64) != 0)
        {
            throw new ArgumentOutOfRangeException("Roles");
        }

        int routingPriorityValue = (int)DefaultRoutingPriority;
        if (routingPriorityValue < (int)DiameterRoutingPriority.Priority0 ||
            routingPriorityValue > (int)DiameterRoutingPriority.Priority15)
        {
            throw new ArgumentOutOfRangeException("DefaultRoutingPriority");
        }

        if (Applications == null || TranslationHandlers == null ||
            Routes == null || RoutePolicies == null)
        {
            throw new ArgumentException(
                "Applications, routes, and policies must not be null.");
        }

        if (Limits == null || Timers == null || Transport == null ||
            Security == null || RealmRouting == null || Overload == null)
        {
            throw new ArgumentException("Nested configuration objects must not be null.");
        }

        Limits.ValidateConfiguration();
        Timers.ValidateConfiguration();
        Transport.ValidateConfiguration();
        Security.ValidateConfiguration();
        RealmRouting.ValidateRealmRoutingConfiguration(OriginRealm);
        Overload.ValidateOverloadConfiguration();

        if ((Roles & DiameterNodeRole.Client) != DiameterNodeRole.None && !Transport.EnableActiveConnections)
        {
            throw new ArgumentException("The client role requires active connections to be enabled.");
        }

        if ((Roles & DiameterNodeRole.Server) != DiameterNodeRole.None && !Transport.EnableListener)
        {
            throw new ArgumentException("The server role requires a listener to be enabled.");
        }

        if (Security.Mode == DiameterSecurityMode.Tls && Transport.Kind != DiameterTransportKind.Tcp)
        {
            throw new ArgumentException("TLS security requires the TCP transport.");
        }

        if (Security.Mode == DiameterSecurityMode.Dtls && Transport.Kind != DiameterTransportKind.Sctp)
        {
            throw new ArgumentException("DTLS security requires the SCTP transport.");
        }

        if (Security.Mode == DiameterSecurityMode.External && Transport.Kind != DiameterTransportKind.External)
        {
            throw new ArgumentException("External security requires an external transport.");
        }

        int applicationIndex = 0;
        while (applicationIndex < Applications.Count)
        {
            DiameterApplicationId application = Applications[applicationIndex];
            if (application == null)
            {
                throw new ArgumentException("Applications must not contain null entries.");
            }

            int comparisonIndex = applicationIndex + 1;
            while (comparisonIndex < Applications.Count)
            {
                DiameterApplicationId comparison = Applications[comparisonIndex];
                if (comparison == null)
                {
                    throw new ArgumentException("Applications must not contain null entries.");
                }

                if (application.Id == comparison.Id &&
                    application.VendorId == comparison.VendorId &&
                    application.IsAccounting == comparison.IsAccounting)
                {
                    throw new ArgumentException("Applications must not contain duplicate identifiers.");
                }

                comparisonIndex = comparisonIndex + 1;
            }

            applicationIndex = applicationIndex + 1;
        }

        int translationIndex = 0;
        while (translationIndex < TranslationHandlers.Count)
        {
            DiameterTranslationHandlerConfiguration translation =
                TranslationHandlers[translationIndex];
            if (translation == null)
            {
                throw new ArgumentException(
                    "Translation handler registrations cannot be null.");
            }

            translation.ValidateTranslationHandlerConfiguration();
            if (!ContainsConfiguredTranslationApplication(
                translation.SourceApplicationId) ||
                !ContainsConfiguredTranslationApplication(
                translation.TargetApplicationId))
            {
                throw new ArgumentException(
                    "Translation handler applications must be advertised.");
            }
            int comparisonIndex = translationIndex + 1;
            while (comparisonIndex < TranslationHandlers.Count)
            {
                DiameterTranslationHandlerConfiguration comparison =
                    TranslationHandlers[comparisonIndex];
                if (comparison == null ||
                    (translation.SourceApplicationId ==
                    comparison.SourceApplicationId &&
                    translation.TargetApplicationId ==
                    comparison.TargetApplicationId))
                {
                    throw new ArgumentException(
                        "Translation handler application pairs must be distinct.");
                }

                comparisonIndex = comparisonIndex + 1;
            }

            translationIndex = translationIndex + 1;
        }

        int routeIndex = 0;
        while (routeIndex < Routes.Count)
        {
            DiameterRouteConfiguration route = Routes[routeIndex];
            if (route == null)
            {
                throw new ArgumentException(
                    "Route registrations cannot be null.");
            }

            route.ValidateRouteConfiguration();
            int comparisonIndex = routeIndex + 1;
            while (comparisonIndex < Routes.Count)
            {
                DiameterRouteConfiguration comparison =
                    Routes[comparisonIndex];
                if (comparison == null || string.Equals(
                    route.RouteIdentifier,
                    comparison.RouteIdentifier,
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(
                        "Route identifiers must be distinct.");
                }

                comparisonIndex = comparisonIndex + 1;
            }

            routeIndex = routeIndex + 1;
        }

        int policyIndex = 0;
        while (policyIndex < RoutePolicies.Count)
        {
            if (RoutePolicies[policyIndex] == null)
            {
                throw new ArgumentException(
                    "Route policy registrations cannot be null.");
            }

            policyIndex = policyIndex + 1;
        }
    }

    internal bool ContainsConfiguredTranslationApplication(
        uint applicationId)
    {
        int index = 0;
        while (index < Applications.Count)
        {
            if (Applications[index].Id == applicationId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}

/// <summary>Configures local, home, and RFC 5729 User-Name realm routing.</summary>
public class DiameterRealmRoutingConfiguration
{
    /// <summary>Additional transit realms processed locally by this node.</summary>
    public List<DiameterIdentity> LocalRealms = new List<DiameterIdentity>();

    /// <summary>Additional realms served as home realms by local applications.</summary>
    public List<DiameterIdentity> HomeRealms = new List<DiameterIdentity>();

    /// <summary>Enables realm derivation and Decorated NAI processing.</summary>
    public bool EnableUserNameRouting = true;

    /// <summary>Treats the node Origin-Realm as a locally served home realm.</summary>
    public bool OriginRealmIsHome = true;

    /// <summary>Rejects malformed or inconsistent routing User-Name values.</summary>
    public bool RejectMalformedUserNames = true;

    internal void ValidateRealmRoutingConfiguration(DiameterIdentity originRealm)
    {
        if (LocalRealms == null || HomeRealms == null || originRealm == null ||
            ValueCodecs.NormalizeDiameterRealm(originRealm).Status !=
                DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "Realm routing requires valid realm collections and Origin-Realm.");
        }

        ValidateRealmRoutingList(LocalRealms, "LocalRealms");
        ValidateRealmRoutingList(HomeRealms, "HomeRealms");
    }

    internal void ValidateRealmRoutingList(
        List<DiameterIdentity> realms,
        string fieldName)
    {
        int index = 0;
        while (index < realms.Count)
        {
            DiameterIdentity realm = realms[index];
            DiameterValueResult normalized =
                ValueCodecs.NormalizeDiameterRealm(realm);
            if (normalized.Status != DiameterCodecStatus.Success)
            {
                throw new ArgumentException(
                    fieldName + " must contain valid Diameter realms.");
            }

            int comparisonIndex = index + 1;
            while (comparisonIndex < realms.Count)
            {
                DiameterValueResult comparison =
                    ValueCodecs.NormalizeDiameterRealm(
                        realms[comparisonIndex]);
                if (comparison.Status != DiameterCodecStatus.Success ||
                    normalized.Text == comparison.Text)
                {
                    throw new ArgumentException(
                        fieldName + " must contain valid distinct realms.");
                }

                comparisonIndex = comparisonIndex + 1;
            }

            index = index + 1;
        }
    }
}

/// <summary>
/// Contains bounded resource and codec limits for a Diameter node.
/// </summary>
public class DiameterLimitConfiguration
{
    /// <summary>The maximum accepted Diameter message size in octets.</summary>
    public int MaximumMessageSize = DiameterProtocolConstants.MaximumMessageLength;

    /// <summary>The maximum number of AVPs in one message.</summary>
    public int MaximumAvpCount = 4096;

    /// <summary>The maximum nesting depth of Grouped AVPs.</summary>
    public int MaximumGroupedDepth = 16;

    /// <summary>The maximum AVP count within one Grouped value.</summary>
    public int MaximumGroupedAvpCount = 1024;

    /// <summary>The maximum Grouped value size in octets.</summary>
    public int MaximumGroupedBytes = 1048576;

    /// <summary>The maximum number of codec issues retained per operation.</summary>
    public int MaximumCodecIssueCount = 128;

    /// <summary>The maximum number of simultaneous peer connections.</summary>
    public int MaximumConnectionCount = 1024;

    /// <summary>The maximum number of pending outbound requests.</summary>
    public int MaximumPendingRequests = 4096;

    /// <summary>The inbound message queue capacity.</summary>
    public int IngressQueueCapacity = 1024;

    /// <summary>The outbound message queue capacity.</summary>
    public int EgressQueueCapacity = 1024;

    /// <summary>The per-application work queue capacity.</summary>
    public int ApplicationQueueCapacity = 1024;

    internal void ValidateConfiguration()
    {
        if (MaximumMessageSize < DiameterProtocolConstants.MinimumMessageLength ||
            MaximumMessageSize > DiameterProtocolConstants.MaximumMessageLength)
        {
            throw new ArgumentOutOfRangeException("MaximumMessageSize");
        }

        if (MaximumMessageSize % DiameterProtocolConstants.Alignment != 0)
        {
            throw new ArgumentException("MaximumMessageSize must be four-octet aligned.");
        }

        if (MaximumAvpCount <= 0 || MaximumGroupedDepth <= 0 || MaximumGroupedAvpCount <= 0 ||
            MaximumGroupedBytes <= 0 || MaximumCodecIssueCount <= 0 || MaximumConnectionCount <= 0 ||
            MaximumPendingRequests <= 0 || IngressQueueCapacity <= 0 || EgressQueueCapacity <= 0 ||
            ApplicationQueueCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException("Limit values");
        }

        if (MaximumGroupedBytes > MaximumMessageSize)
        {
            throw new ArgumentException("MaximumGroupedBytes must not exceed MaximumMessageSize.");
        }

        if (MaximumGroupedAvpCount > MaximumAvpCount)
        {
            throw new ArgumentException("MaximumGroupedAvpCount must not exceed MaximumAvpCount.");
        }
    }
}

/// <summary>
/// Contains timer intervals for Diameter connection and request processing.
/// </summary>
public class DiameterTimerConfiguration
{
    /// <summary>The peer connection retry interval in milliseconds.</summary>
    public int ConnectionRetryMilliseconds = 30000;

    /// <summary>The maximum percentage randomly subtracted from or added to each retry interval.</summary>
    public int ConnectionRetryJitterPercent = 10;

    /// <summary>The idle watchdog interval in milliseconds.</summary>
    public int WatchdogIntervalMilliseconds = 30000;

    /// <summary>The maximum random offset applied to each watchdog interval.</summary>
    public int WatchdogJitterMilliseconds = 2000;

    /// <summary>The outbound request timeout in milliseconds.</summary>
    public int RequestTimeoutMilliseconds = 30000;

    /// <summary>The transport connection timeout in milliseconds.</summary>
    public int ConnectTimeoutMilliseconds = 10000;

    /// <summary>The graceful drain timeout in milliseconds.</summary>
    public int DrainTimeoutMilliseconds = 30000;

    /// <summary>The duplicate-request cache lifetime in milliseconds.</summary>
    public int DuplicateCacheLifetimeMilliseconds = 240000;

    /// <summary>The maximum redirect cache lifetime in milliseconds.</summary>
    public int RedirectCacheMaximumLifetimeMilliseconds = 86400000;

    /// <summary>The session expiry scan interval in milliseconds.</summary>
    public int SessionSweepMilliseconds = 1000;

    internal void ValidateConfiguration()
    {
        if (ConnectionRetryMilliseconds <= 0 || ConnectionRetryJitterPercent < 0 ||
            ConnectionRetryJitterPercent > 100 || WatchdogIntervalMilliseconds < 6000 ||
            WatchdogJitterMilliseconds < 0 || WatchdogJitterMilliseconds > 60000 ||
            RequestTimeoutMilliseconds <= 0 || ConnectTimeoutMilliseconds <= 0 ||
            DrainTimeoutMilliseconds <= 0 || DuplicateCacheLifetimeMilliseconds <= 0 ||
            RedirectCacheMaximumLifetimeMilliseconds <= 0 || SessionSweepMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException("Timer values");
        }

        if (DuplicateCacheLifetimeMilliseconds < RequestTimeoutMilliseconds)
        {
            throw new ArgumentException("Duplicate cache lifetime must cover the request timeout.");
        }
    }
}

/// <summary>
/// Contains transport listener and connection settings.
/// </summary>
public class DiameterTransportConfiguration
{
    /// <summary>The transport implementation kind.</summary>
    public DiameterTransportKind Kind;

    /// <summary>The local host or address text, or an empty string for any local address.</summary>
    public string Host = string.Empty;

    /// <summary>The local or remote transport port.</summary>
    public int Port = DiameterProtocolConstants.Port;

    /// <summary>Enables IPv4 endpoints.</summary>
    public bool EnableIpv4 = true;

    /// <summary>Enables IPv6 endpoints.</summary>
    public bool EnableIpv6 = true;

    /// <summary>Enables passive listener creation.</summary>
    public bool EnableListener;

    /// <summary>Enables active outbound connections.</summary>
    public bool EnableActiveConnections = true;

    /// <summary>Enables TCP no-delay behavior.</summary>
    public bool NoDelay = true;

    /// <summary>Enables transport keepalive behavior.</summary>
    public bool KeepAlive = true;

    /// <summary>The socket receive buffer size in octets.</summary>
    public int ReceiveBufferSize = 65536;

    /// <summary>The socket send buffer size in octets.</summary>
    public int SendBufferSize = 65536;

    /// <summary>The listener accept backlog.</summary>
    public int ListenBacklog = 128;

    /// <summary>The requested SCTP stream count.</summary>
    public int SctpStreamCount = 1;

    internal void ValidateConfiguration()
    {
        if (Kind != DiameterTransportKind.Tcp && Kind != DiameterTransportKind.Sctp &&
            Kind != DiameterTransportKind.External)
        {
            throw new ArgumentOutOfRangeException("Kind");
        }

        if (Kind == DiameterTransportKind.External)
        {
            if (Port < 0 || Port > 65535)
            {
                throw new ArgumentOutOfRangeException("Port");
            }
        }
        else if (Port <= 0 || Port > 65535)
        {
            throw new ArgumentOutOfRangeException("Port");
        }

        if (Kind != DiameterTransportKind.External && !EnableIpv4 && !EnableIpv6)
        {
            throw new ArgumentException("At least one IP address family must be enabled.");
        }

        if (ReceiveBufferSize < DiameterProtocolConstants.MessageHeaderSize ||
            SendBufferSize < DiameterProtocolConstants.MessageHeaderSize || ListenBacklog <= 0 ||
            SctpStreamCount <= 0)
        {
            throw new ArgumentOutOfRangeException("Transport size values");
        }

        if (Kind != DiameterTransportKind.Sctp && SctpStreamCount != 1)
        {
            throw new ArgumentException("SctpStreamCount applies only to SCTP transports.");
        }

        if (Kind == DiameterTransportKind.Sctp && SctpStreamCount > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException("SctpStreamCount");
        }
    }
}

/// <summary>
/// Contains transport-security and certificate validation settings.
/// </summary>
public class DiameterSecurityConfiguration
{
    /// <summary>The transport security mode.</summary>
    public DiameterSecurityMode Mode;

    /// <summary>The path to the local certificate and private key bundle.</summary>
    public string LocalCertificatePath = string.Empty;

    /// <summary>The password for the local certificate bundle.</summary>
    public string LocalCertificatePassword = string.Empty;

    /// <summary>The path to an optional custom trust-anchor bundle.</summary>
    public string TrustAnchorPath = string.Empty;

    /// <summary>The expected peer DNS name when one is known globally.</summary>
    public string ExpectedPeerName = string.Empty;

    /// <summary>The expected Diameter host identity, or an empty value to use the endpoint.</summary>
    public DiameterIdentity ExpectedPeerIdentity = new DiameterIdentity();

    /// <summary>The expected Diameter realm, or an empty value to derive it from the host identity.</summary>
    public DiameterIdentity ExpectedPeerRealm = new DiameterIdentity();

    /// <summary>The optional authenticated peer admission hook.</summary>
    public IPeerAdmissionPolicy? PeerAdmissionPolicy;

    /// <summary>The optional certificate-to-Diameter-identity authorization hook.</summary>
    public ICertificatePeerPolicy? CertificatePeerPolicy;

    /// <summary>Requires the remote peer to present a certificate.</summary>
    public bool RequirePeerCertificate;

    /// <summary>Enables certificate revocation checks.</summary>
    public bool CheckCertificateRevocation;

    /// <summary>Explicitly permits an unsecured transport.</summary>
    public bool AllowUnsecuredTransport;

    internal void ValidateConfiguration()
    {
        if (Mode != DiameterSecurityMode.None && Mode != DiameterSecurityMode.Tls &&
            Mode != DiameterSecurityMode.Dtls && Mode != DiameterSecurityMode.Ipsec &&
            Mode != DiameterSecurityMode.External)
        {
            throw new ArgumentOutOfRangeException("Mode");
        }

        bool usesCertificates = Mode == DiameterSecurityMode.Tls || Mode == DiameterSecurityMode.Dtls;

        if (ExpectedPeerIdentity == null || ExpectedPeerRealm == null)
        {
            throw new ArgumentException("Expected peer identity objects must not be null.");
        }

        if ((!string.IsNullOrWhiteSpace(ExpectedPeerIdentity.Value) &&
            ValueCodecs.NormalizeDiameterIdentity(ExpectedPeerIdentity).Status !=
                DiameterCodecStatus.Success) ||
            (!string.IsNullOrWhiteSpace(ExpectedPeerRealm.Value) &&
            ValueCodecs.NormalizeDiameterRealm(ExpectedPeerRealm).Status !=
                DiameterCodecStatus.Success))
        {
            throw new ArgumentException("Expected peer identity values are invalid.");
        }

        if (!string.IsNullOrWhiteSpace(LocalCertificatePassword) &&
            string.IsNullOrWhiteSpace(LocalCertificatePath))
        {
            throw new ArgumentException("A certificate password requires a local certificate path.");
        }

        if (usesCertificates)
        {
            if (string.IsNullOrWhiteSpace(LocalCertificatePath))
            {
                throw new ArgumentException("TLS and DTLS require a local certificate.");
            }

            if (!RequirePeerCertificate)
            {
                throw new ArgumentException("TLS and DTLS require mutual certificate authentication.");
            }

            if (AllowUnsecuredTransport)
            {
                throw new ArgumentException("Secure modes cannot permit an unsecured transport.");
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(LocalCertificatePath) ||
                !string.IsNullOrWhiteSpace(LocalCertificatePassword) ||
                !string.IsNullOrWhiteSpace(TrustAnchorPath) ||
                !string.IsNullOrWhiteSpace(ExpectedPeerName) || RequirePeerCertificate ||
                CheckCertificateRevocation || CertificatePeerPolicy != null)
            {
                throw new ArgumentException("Certificate settings require TLS or DTLS.");
            }

            if (Mode == DiameterSecurityMode.None && !AllowUnsecuredTransport)
            {
                throw new ArgumentException("Unsecured transport use must be explicitly enabled.");
            }

            if (Mode != DiameterSecurityMode.None && AllowUnsecuredTransport)
            {
                throw new ArgumentException("Secured transport modes cannot permit unsecured use.");
            }
        }
    }
}
