using System;
using System.Collections.Generic;

namespace diameter;

/// <summary>Configures one static, default, local, or agent route.</summary>
public class DiameterRouteConfiguration
{
    /// <summary>The stable route identifier.</summary>
    public string RouteIdentifier = string.Empty;

    /// <summary>The destination realm matched by the route.</summary>
    public DiameterIdentity DestinationRealm = new DiameterIdentity();

    /// <summary>The destination host matched by the route, or <c>null</c>.</summary>
    public DiameterIdentity? DestinationHost;

    /// <summary>The application identifier matched by the route.</summary>
    public uint ApplicationId;

    /// <summary>Matches every application instead of one application identifier.</summary>
    public bool AnyApplication;

    /// <summary>The peer selected by a relay or proxy route, or <c>null</c>.</summary>
    public DiameterIdentity? PeerIdentity;

    /// <summary>The routing action performed by the route.</summary>
    public DiameterRouteAction Action;

    /// <summary>The agent role performed by the route.</summary>
    public DiameterAgentRole AgentRole;

    /// <summary>The lower-valued configured route priority.</summary>
    public int Priority;

    /// <summary>The positive weighted-selection value among equal priorities.</summary>
    public int Weight = 1;

    /// <summary>The target application for a translation route.</summary>
    public uint TargetApplicationId;

    /// <summary>The redirect targets returned by a redirect route.</summary>
    public List<DiameterUri> RedirectHosts = new List<DiameterUri>();

    /// <summary>The alternate realms returned by a realm-based redirect route.</summary>
    public List<DiameterIdentity> RedirectRealms =
        new List<DiameterIdentity>();

    /// <summary>The cache scope advertised by a redirect route.</summary>
    public DiameterRedirectHostUsage RedirectUsage;

    /// <summary>The advertised redirect cache lifetime in seconds.</summary>
    public uint RedirectMaximumCacheTimeSeconds;

    /// <summary>Restricts realm redirection to the first request of a session.</summary>
    public bool RedirectInitialSessionRequestsOnly;

    /// <summary>Allows realm redirection of explicitly host-addressed requests.</summary>
    public bool RedirectRequestsWithDestinationHost;

    /// <summary>Indicates that the route is considered after specific routes.</summary>
    public bool DefaultRoute;

    /// <summary>Requires a transport-secured candidate peer.</summary>
    public bool RequireSecurePeer;

    /// <summary>Indicates that the route is eligible for selection.</summary>
    public bool Enabled = true;

    internal void ValidateRouteConfiguration()
    {
        if (string.IsNullOrWhiteSpace(RouteIdentifier) ||
            ValueCodecs.FindInvalidUtf16Index(RouteIdentifier) >= 0)
        {
            throw new ArgumentException(
                "A valid route identifier is required.");
        }

        if (DestinationRealm == null || DestinationRealm.Value == null ||
            RedirectHosts == null || RedirectRealms == null)
        {
            throw new ArgumentException(
                "Route realm and redirect collections are required.");
        }

        bool hasRealm = DestinationRealm.Value != null &&
            DestinationRealm.Value.Length > 0;
        if (hasRealm &&
            ValueCodecs.NormalizeDiameterRealm(DestinationRealm).Status !=
                DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "The route destination realm is invalid.");
        }

        if (DestinationHost != null &&
            ValueCodecs.NormalizeDiameterIdentity(DestinationHost).Status !=
                DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "The route destination host is invalid.");
        }

        if (!DefaultRoute && !hasRealm && DestinationHost == null)
        {
            throw new ArgumentException(
                "A non-default route requires a destination realm or host.");
        }

        if (DefaultRoute && (hasRealm || DestinationHost != null))
        {
            throw new ArgumentException(
                "A default route cannot constrain the destination realm or host.");
        }

        int actionValue = (int)Action;
        int roleValue = (int)AgentRole;
        if (actionValue <= (int)DiameterRouteAction.NoRoute ||
            actionValue > (int)DiameterRouteAction.Reject ||
            roleValue < (int)DiameterAgentRole.None ||
            roleValue > (int)DiameterAgentRole.Translation)
        {
            throw new ArgumentOutOfRangeException("Route action or role");
        }

        if ((Action == DiameterRouteAction.Relay &&
            AgentRole != DiameterAgentRole.Relay) ||
            (Action == DiameterRouteAction.Proxy &&
            AgentRole != DiameterAgentRole.Proxy) ||
            (Action == DiameterRouteAction.Redirect &&
            AgentRole != DiameterAgentRole.Redirect) ||
            (Action == DiameterRouteAction.Translate &&
            AgentRole != DiameterAgentRole.Translation) ||
            ((Action == DiameterRouteAction.Local ||
            Action == DiameterRouteAction.Reject) &&
            AgentRole != DiameterAgentRole.None))
        {
            throw new ArgumentException(
                "The route action and agent role do not agree.");
        }

        if ((Action == DiameterRouteAction.Relay ||
            Action == DiameterRouteAction.Proxy) &&
            (PeerIdentity == null ||
            ValueCodecs.NormalizeDiameterIdentity(PeerIdentity).Status !=
                DiameterCodecStatus.Success))
        {
            throw new ArgumentException(
                "Relay and proxy routes require a valid peer identity.");
        }

        if ((Action == DiameterRouteAction.Translate &&
            TargetApplicationId == ApplicationId) ||
            (Action != DiameterRouteAction.Translate &&
            TargetApplicationId != 0U))
        {
            throw new ArgumentException(
                "Translation routes require a distinct target application.");
        }

        if (Action == DiameterRouteAction.Redirect &&
            RedirectHosts.Count == 0 && RedirectRealms.Count == 0)
        {
            throw new ArgumentException(
                "A redirect route requires at least one redirect target.");
        }

        if ((RedirectHosts.Count > 0 && RedirectRealms.Count > 0) ||
            (Action != DiameterRouteAction.Redirect &&
            RedirectRealms.Count > 0) ||
            (RedirectInitialSessionRequestsOnly &&
            RedirectRealms.Count == 0) ||
            (RedirectRequestsWithDestinationHost &&
            RedirectRealms.Count == 0))
        {
            throw new ArgumentException(
                "Host and realm redirect configurations cannot be combined.");
        }

        int redirectUsage = (int)RedirectUsage;
        if (redirectUsage < (int)DiameterRedirectHostUsage.DoNotCache ||
            redirectUsage > (int)DiameterRedirectHostUsage.AllUser ||
            (Action != DiameterRouteAction.Redirect &&
            (RedirectUsage != DiameterRedirectHostUsage.DoNotCache ||
            RedirectMaximumCacheTimeSeconds != 0U)) ||
            (Action == DiameterRouteAction.Redirect &&
            RedirectUsage == DiameterRedirectHostUsage.DoNotCache &&
            RedirectMaximumCacheTimeSeconds != 0U) ||
            (Action == DiameterRouteAction.Redirect &&
            RedirectUsage != DiameterRedirectHostUsage.DoNotCache &&
            RedirectMaximumCacheTimeSeconds == 0U))
        {
            throw new ArgumentException(
                "Redirect cache usage and lifetime are inconsistent.");
        }

        int redirectIndex = 0;
        while (redirectIndex < RedirectHosts.Count)
        {
            DiameterValueResult normalizedRedirect =
                ValueCodecs.NormalizeDiameterUri(
                    RedirectHosts[redirectIndex]);
            if (RedirectHosts[redirectIndex] == null ||
                normalizedRedirect.Status != DiameterCodecStatus.Success ||
                normalizedRedirect.Uri == null ||
                normalizedRedirect.Uri.Protocol != "diameter")
            {
                throw new ArgumentException(
                    "Redirect routes require valid redirect hosts.");
            }

            redirectIndex = redirectIndex + 1;
        }

        redirectIndex = 0;
        while (redirectIndex < RedirectRealms.Count)
        {
            DiameterValueResult normalizedRealm =
                ValueCodecs.NormalizeDiameterRealm(
                    RedirectRealms[redirectIndex]);
            if (normalizedRealm.Status != DiameterCodecStatus.Success)
            {
                throw new ArgumentException(
                    "Realm redirect routes require valid alternate realms.");
            }

            int comparisonIndex = redirectIndex + 1;
            while (comparisonIndex < RedirectRealms.Count)
            {
                if (ValueCodecs.NormalizeDiameterRealm(
                    RedirectRealms[comparisonIndex]).Text ==
                    normalizedRealm.Text)
                {
                    throw new ArgumentException(
                        "Realm redirect routes require distinct alternate realms.");
                }

                comparisonIndex = comparisonIndex + 1;
            }

            redirectIndex = redirectIndex + 1;
        }

        if (Priority < 0 || Weight <= 0)
        {
            throw new ArgumentOutOfRangeException(
                "Route priority or weight");
        }
    }
}

/// <summary>Contains the normalized context used to route one request.</summary>
public class DiameterRouteRequest
{
    /// <summary>The request message to route.</summary>
    public DiameterMessage Message = new DiameterMessage();

    /// <summary>The peer from which the request arrived, or <c>null</c>.</summary>
    public DiameterIdentity? SourcePeer;

    /// <summary>The Origin-Host from the request, or <c>null</c>.</summary>
    public DiameterIdentity? OriginHost;

    /// <summary>The Destination-Realm from the request, or <c>null</c>.</summary>
    public DiameterIdentity? DestinationRealm;

    /// <summary>The Destination-Host from the request, or <c>null</c>.</summary>
    public DiameterIdentity? DestinationHost;

    /// <summary>The application identifier to route.</summary>
    public uint ApplicationId;

    /// <summary>The Session-Id used for affinity, or an empty string.</summary>
    public string SessionId = string.Empty;

    /// <summary>The User-Name used for user-realm routing, or an empty string.</summary>
    public string UserName = string.Empty;

    /// <summary>The peers excluded by path, loop, or failover processing.</summary>
    public List<DiameterIdentity> ExcludedPeers =
        new List<DiameterIdentity>();

    /// <summary>The routing priority carried by the request.</summary>
    public DiameterRoutingPriority RoutingPriority =
        DiameterRoutingPriority.Priority10;

    /// <summary>Indicates that the request explicitly carried routing priority.</summary>
    public bool RoutingPriorityPresent;

    /// <summary>Allows redirect routes for the request.</summary>
    public bool AllowRedirect = true;

    /// <summary>Allows translation routes for the request.</summary>
    public bool AllowTranslation = true;

    /// <summary>Indicates that the request begins a Diameter session.</summary>
    public bool InitialSessionRequest = true;

    /// <summary>The time at which the request entered routing.</summary>
    public DateTimeOffset ReceivedAt;

    internal void ValidateRouteRequest()
    {
        if (Message == null || Message.Avps == null ||
            ExcludedPeers == null)
        {
            throw new ArgumentException(
                "A route request requires message and exclusion storage.");
        }

        int flagValue = (int)Message.Flags;
        if (Message.Version != DiameterProtocolConstants.Version ||
            Message.CommandCode == 0U ||
            Message.CommandCode > DiameterProtocolConstants.MaximumCommandCode ||
            Message.ApplicationId != ApplicationId ||
            flagValue < 0 || flagValue > 255 ||
            (flagValue & DiameterProtocolConstants.MessageReservedFlagMask) != 0 ||
            (Message.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None ||
            (Message.Flags & DiameterMessageFlags.Error) !=
                DiameterMessageFlags.None)
        {
            throw new ArgumentException(
                "The routed message must be a valid Diameter request.");
        }

        if ((SourcePeer != null &&
            ValueCodecs.NormalizeDiameterIdentity(SourcePeer).Status !=
                DiameterCodecStatus.Success) ||
            (OriginHost != null &&
            ValueCodecs.NormalizeDiameterIdentity(OriginHost).Status !=
                DiameterCodecStatus.Success) ||
            (DestinationRealm != null &&
            ValueCodecs.NormalizeDiameterRealm(DestinationRealm).Status !=
                DiameterCodecStatus.Success) ||
            (DestinationHost != null &&
            ValueCodecs.NormalizeDiameterIdentity(DestinationHost).Status !=
                DiameterCodecStatus.Success))
        {
            throw new ArgumentException(
                "The routing identities are invalid.");
        }

        if (SessionId == null || UserName == null ||
            ValueCodecs.FindInvalidUtf16Index(SessionId) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(UserName) >= 0)
        {
            throw new ArgumentException(
                "The routing session or user text is invalid.");
        }

        int priorityValue = (int)RoutingPriority;
        if (priorityValue < (int)DiameterRoutingPriority.Priority0 ||
            priorityValue > (int)DiameterRoutingPriority.Priority15)
        {
            throw new ArgumentOutOfRangeException("RoutingPriority");
        }

        int avpIndex = 0;
        while (avpIndex < Message.Avps.Count)
        {
            DiameterAvp avp = Message.Avps[avpIndex];
            if (avp == null || avp.Data == null)
            {
                throw new ArgumentException(
                    "The routed message must own its AVPs and payloads.");
            }

            avpIndex = avpIndex + 1;
        }

        int index = 0;
        while (index < ExcludedPeers.Count)
        {
            DiameterIdentity peer = ExcludedPeers[index];
            DiameterValueResult normalized =
                ValueCodecs.NormalizeDiameterIdentity(peer);
            if (normalized.Status != DiameterCodecStatus.Success)
            {
                throw new ArgumentException(
                    "Excluded peers must contain valid identities.");
            }

            int comparisonIndex = index + 1;
            while (comparisonIndex < ExcludedPeers.Count)
            {
                DiameterValueResult comparison =
                    ValueCodecs.NormalizeDiameterIdentity(
                        ExcludedPeers[comparisonIndex]);
                if (comparison.Status != DiameterCodecStatus.Success ||
                    normalized.Text == comparison.Text)
                {
                    throw new ArgumentException(
                        "Excluded peers must be valid and distinct.");
                }

                comparisonIndex = comparisonIndex + 1;
            }

            index = index + 1;
        }
    }
}

/// <summary>Contains one route candidate and its policy evaluation.</summary>
public class DiameterRouteCandidate
{
    /// <summary>The route configuration that produced the candidate.</summary>
    public DiameterRouteConfiguration Configuration =
        new DiameterRouteConfiguration();

    /// <summary>The candidate peer identity, or <c>null</c>.</summary>
    public DiameterIdentity? PeerIdentity;

    /// <summary>The owned candidate peer snapshot, or <c>null</c>.</summary>
    public DiameterPeerSnapshot? Peer;

    /// <summary>The action proposed by the candidate.</summary>
    public DiameterRouteAction Action;

    /// <summary>The agent role proposed by the candidate.</summary>
    public DiameterAgentRole AgentRole;

    /// <summary>The source that supplied the candidate.</summary>
    public DiameterRouteSource Source;

    /// <summary>The configured route priority.</summary>
    public int Priority;

    /// <summary>The configured weighted-selection value.</summary>
    public int Weight = 1;

    /// <summary>The cumulative route-policy score.</summary>
    public int PolicyScore;

    /// <summary>The stable source order used for deterministic ties.</summary>
    public int Order;

    /// <summary>Indicates that the candidate peer is available.</summary>
    public bool Available;

    /// <summary>Indicates that the peer supports the routed application.</summary>
    public bool ApplicationSupported;

    /// <summary>Indicates that the candidate does not create a route loop.</summary>
    public bool LoopFree;

    /// <summary>Indicates that the candidate meets transport security policy.</summary>
    public bool Secure;

    /// <summary>Indicates that route policy accepted the candidate.</summary>
    public bool PolicyAccepted;

    /// <summary>Stops later policy evaluation when this candidate is selected or rejected.</summary>
    public bool PolicyTerminal;

    /// <summary>The stable machine-readable candidate outcome code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable candidate outcome description.</summary>
    public string Message = string.Empty;
}

/// <summary>Contains the complete routing outcome for one request.</summary>
public class DiameterRouteDecision
{
    /// <summary>The owned request routing context.</summary>
    public DiameterRouteRequest Request = new DiameterRouteRequest();

    /// <summary>The selected action.</summary>
    public DiameterRouteAction Action;

    /// <summary>The selected agent role.</summary>
    public DiameterAgentRole AgentRole;

    /// <summary>The selected candidate, or <c>null</c>.</summary>
    public DiameterRouteCandidate? SelectedCandidate;

    /// <summary>The evaluated candidates in deterministic order.</summary>
    public List<DiameterRouteCandidate> Candidates =
        new List<DiameterRouteCandidate>();

    /// <summary>The redirect cache entries selected or created by routing.</summary>
    public List<DiameterRedirectEntry> RedirectEntries =
        new List<DiameterRedirectEntry>();

    /// <summary>The translation outcome, or <c>null</c>.</summary>
    public DiameterTranslationResult? TranslationResult;

    /// <summary>The owned message to deliver or forward, or <c>null</c>.</summary>
    public DiameterMessage? RoutedMessage;

    /// <summary>The Result-Code used when routing rejects the request.</summary>
    public uint ResultCode;

    /// <summary>Indicates that a usable routing outcome was selected.</summary>
    public bool Routed;

    /// <summary>Indicates that route policy rejected the request.</summary>
    public bool Rejected;

    /// <summary>The stable machine-readable routing outcome code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable routing outcome description.</summary>
    public string Message = string.Empty;
}

/// <summary>Contains an owned request and traversed path for route authorization.</summary>
public class DiameterRoutePathAuthorizationRequest
{
    /// <summary>The owned request being considered for local delivery.</summary>
    public DiameterRouteRequest Request = new DiameterRouteRequest();

    /// <summary>The local Diameter host performing path authorization.</summary>
    public DiameterIdentity LocalHost = new DiameterIdentity();

    /// <summary>The ordered peer identities received in Route-Record AVPs.</summary>
    public List<DiameterIdentity> TraversedPeers =
        new List<DiameterIdentity>();
}

/// <summary>Contains the result of authorizing a traversed Diameter route.</summary>
public class DiameterRoutePathAuthorizationResult
{
    /// <summary>Indicates that the traversed path is authorized.</summary>
    public bool Authorized;

    /// <summary>The stable machine-readable authorization code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable authorization description.</summary>
    public string Message = string.Empty;
}

/// <summary>Contains an owned operational snapshot of one configured route.</summary>
public class DiameterRouteSnapshot
{
    /// <summary>The owned route configuration.</summary>
    public DiameterRouteConfiguration Configuration =
        new DiameterRouteConfiguration();

    /// <summary>The currently resolved peer identity, or <c>null</c>.</summary>
    public DiameterIdentity? PeerIdentity;

    /// <summary>The source currently supplying the route.</summary>
    public DiameterRouteSource Source;

    /// <summary>Indicates that the route can currently be selected.</summary>
    public bool Available;

    /// <summary>Indicates that peer capabilities match the route application.</summary>
    public bool ApplicationSupported;

    /// <summary>Indicates that the route satisfies current health policy.</summary>
    public bool Healthy;

    /// <summary>The number of times the route was selected.</summary>
    public long SelectionCount;

    /// <summary>The number of route delivery failures.</summary>
    public long FailureCount;

    /// <summary>The time at which the route was added.</summary>
    public DateTimeOffset CreatedAt;

    /// <summary>The time at which the route state was updated.</summary>
    public DateTimeOffset UpdatedAt;

    /// <summary>The most recent route selection time.</summary>
    public DateTimeOffset LastSelectedAt;

    /// <summary>The stable machine-readable route state code.</summary>
    public string Code = string.Empty;
}

/// <summary>Contains one scoped redirect-cache entry.</summary>
public class DiameterRedirectEntry
{
    /// <summary>The stable redirect cache key.</summary>
    public string RedirectKey = string.Empty;

    /// <summary>The RFC redirect cache scope.</summary>
    public DiameterRedirectHostUsage Usage;

    /// <summary>The ordered redirect targets.</summary>
    public List<DiameterUri> Hosts = new List<DiameterUri>();

    /// <summary>The destination realm represented by the key, or <c>null</c>.</summary>
    public DiameterIdentity? DestinationRealm;

    /// <summary>The destination host represented by the key, or <c>null</c>.</summary>
    public DiameterIdentity? DestinationHost;

    /// <summary>The application identifier represented by the key.</summary>
    public uint ApplicationId;

    /// <summary>The Session-Id represented by the key, or an empty string.</summary>
    public string SessionId = string.Empty;

    /// <summary>The User-Name represented by the key, or an empty string.</summary>
    public string UserName = string.Empty;

    /// <summary>The peer that supplied the redirect.</summary>
    public DiameterIdentity RedirectingPeer = new DiameterIdentity();

    /// <summary>The maximum cache lifetime supplied by the redirect answer.</summary>
    public uint MaximumCacheTimeSeconds;

    /// <summary>The time at which the redirect was created.</summary>
    public DateTimeOffset CreatedAt;

    /// <summary>The absolute redirect expiry.</summary>
    public DateTimeOffset ExpiresAt;

    /// <summary>Indicates that the redirect source used a secured transport.</summary>
    public bool Secure;

    /// <summary>Indicates that every redirect target passed authorization.</summary>
    public bool Authorized;

    /// <summary>Indicates that the entry is eligible for caching.</summary>
    public bool Cached;

    /// <summary>The stable machine-readable redirect outcome code.</summary>
    public string Code = string.Empty;

    /// <summary>Indicates that the entry was derived from realm redirection.</summary>
    public bool RealmBased;

    /// <summary>The alternate realm selected by realm redirection, or <c>null</c>.</summary>
    public DiameterIdentity? RedirectRealm;
}

/// <summary>Contains one realm redirect authorization request.</summary>
public class DiameterRealmRedirectAuthorizationRequest
{
    /// <summary>The owned original request being redirected.</summary>
    public DiameterRouteRequest Request = new DiameterRouteRequest();

    /// <summary>The peer that supplied the realm redirect answer.</summary>
    public DiameterIdentity RedirectingPeer = new DiameterIdentity();

    /// <summary>The alternate realm being considered.</summary>
    public DiameterIdentity RedirectRealm = new DiameterIdentity();

    /// <summary>The discovered route candidate for the alternate realm.</summary>
    public DiameterRouteCandidate Candidate = new DiameterRouteCandidate();

    /// <summary>Indicates that the redirect answer arrived securely.</summary>
    public bool SourceSecure;
}

/// <summary>Contains a realm redirect authorization outcome.</summary>
public class DiameterRealmRedirectAuthorizationResult
{
    /// <summary>Indicates that the alternate realm route is authorized.</summary>
    public bool Authorized;

    /// <summary>The stable machine-readable authorization code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable authorization description.</summary>
    public string Message = string.Empty;
}

/// <summary>Contains one redirect target authorization request.</summary>
public class DiameterRedirectHostAuthorizationRequest
{
    /// <summary>The owned request that produced the redirect answer.</summary>
    public DiameterRouteRequest Request = new DiameterRouteRequest();

    /// <summary>The peer that supplied the redirect answer.</summary>
    public DiameterIdentity RedirectingPeer = new DiameterIdentity();

    /// <summary>The normalized redirect target being authorized.</summary>
    public DiameterUri RedirectHost = new DiameterUri();

    /// <summary>Indicates that the redirect answer arrived securely.</summary>
    public bool SourceSecure;
}

/// <summary>Contains the authorization outcome for one redirect target.</summary>
public class DiameterRedirectHostAuthorizationResult
{
    /// <summary>Indicates that the redirect target is authorized.</summary>
    public bool Authorized;

    /// <summary>The stable machine-readable authorization code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable authorization description.</summary>
    public string Message = string.Empty;
}

/// <summary>Contains the owned outcome and state of one message translation.</summary>
public class DiameterTranslationResult
{
    /// <summary>The translation disposition.</summary>
    public DiameterTranslationDisposition Disposition;

    /// <summary>The message supplied to the translation handler, or <c>null</c>.</summary>
    public DiameterMessage? SourceMessage;

    /// <summary>The translated message, or <c>null</c>.</summary>
    public DiameterMessage? TranslatedMessage;

    /// <summary>The source application identifier.</summary>
    public uint SourceApplicationId;

    /// <summary>The target application identifier.</summary>
    public uint TargetApplicationId;

    /// <summary>The owned state used to translate the corresponding answer.</summary>
    public byte[] TranslationState = new byte[0];

    /// <summary>The Result-Code produced when translation fails.</summary>
    public uint ResultCode;

    /// <summary>The stable machine-readable translation outcome code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable translation outcome description.</summary>
    public string Message = string.Empty;
}

/// <summary>Registers one application-pair translation handler.</summary>
public class DiameterTranslationHandlerConfiguration
{
    /// <summary>The application identifier accepted by the handler.</summary>
    public uint SourceApplicationId;

    /// <summary>The application identifier produced by the handler.</summary>
    public uint TargetApplicationId;

    /// <summary>The handler for the configured application pair, or <c>null</c>.</summary>
    public ITranslationHandler? Handler;

    /// <summary>Indicates that the handler is available for dispatch.</summary>
    public bool Enabled = true;

    internal void ValidateTranslationHandlerConfiguration()
    {
        if (SourceApplicationId == TargetApplicationId || Handler == null)
        {
            throw new ArgumentException(
                "A translation handler requires distinct applications and a handler.");
        }
    }
}
