using System;
using System.Collections.Generic;

namespace diameter;

/// <summary>Routes Diameter requests through configured peers, routes, and policies.</summary>
public class DiameterRouter : IDiameterRouter
{
    internal IDiameterPeerManager PeerManager;
    internal RouteTable Table;
    internal RouteSelector Selector;

    /// <summary>Initializes a router for one configured Diameter node and peer manager.</summary>
    /// <param name="configuration">The node, routes, and ordered policies.</param>
    /// <param name="peerManager">The source of current peer snapshots.</param>
    /// <param name="clock">The clock used for route and redirect state.</param>
    /// <exception cref="System.ArgumentException">
    /// Thrown when a dependency or configuration is invalid.
    /// </exception>
    public DiameterRouter(
        DiameterNodeConfiguration? configuration,
        IDiameterPeerManager? peerManager,
        IDiameterClock? clock)
    {
        if (configuration == null || peerManager == null || clock == null)
        {
            throw new ArgumentException(
                "A node configuration, peer manager, and clock are required.");
        }

        configuration.ValidateConfiguration();
        PeerManager = peerManager;
        Table = new RouteTable(clock, new SystemRouteRandom());
        int routeIndex = 0;
        while (routeIndex < configuration.Routes.Count)
        {
            Table.AddOrReplaceStaticRoute(
                configuration.Routes[routeIndex]);
            routeIndex = routeIndex + 1;
        }

        Selector = new RouteSelector(configuration, Table);
    }

    /// <summary>Selects one local, agent, redirect, or translation route.</summary>
    /// <param name="request">The normalized request routing context.</param>
    /// <returns>The complete owned routing decision.</returns>
    public DiameterRouteDecision SelectDiameterRoute(
        DiameterRouteRequest? request)
    {
        if (request == null)
        {
            throw new ArgumentException("A route request is required.");
        }

        List<DiameterPeerSnapshot> peers =
            PeerManager.ReadDiameterPeerSnapshots();
        return Selector.SelectRoute(request, peers);
    }

    /// <summary>Processes redirect routing information from an answer.</summary>
    /// <param name="request">The request that produced the redirect answer.</param>
    /// <param name="answer">The received redirect answer.</param>
    /// <param name="sourcePeer">The peer that supplied the answer.</param>
    /// <param name="receivedAt">The answer receive time.</param>
    /// <returns>The owned redirect entries accepted from the answer.</returns>
    public List<DiameterRedirectEntry> ProcessDiameterRedirectAnswer(
        DiameterRouteRequest? request,
        DiameterMessage? answer,
        DiameterIdentity? sourcePeer,
        DateTimeOffset receivedAt)
    {
        List<DiameterRedirectEntry> result =
            new List<DiameterRedirectEntry>();
        if (request == null || answer == null || sourcePeer == null)
        {
            return result;
        }

        List<DiameterPeerSnapshot> peers =
            PeerManager.ReadDiameterPeerSnapshots();
        bool sourceSecure = ReadRedirectSourceSecurity(
            sourcePeer,
            peers);
        if (ContainsRealmRedirectInformation(answer))
        {
            RealmRedirectProcessingResult realm =
                Selector.ProcessRealmRedirectAnswer(
                    request,
                    answer,
                    sourcePeer,
                    sourceSecure,
                    receivedAt,
                    peers);
            if (realm.Entry != null)
            {
                result.Add(RouteTableCloner.CloneRedirectEntry(
                    realm.Entry));
            }

            return result;
        }

        RedirectProcessingResult redirect = Selector.ProcessRedirectAnswer(
            request,
            answer,
            sourcePeer,
            sourceSecure,
            receivedAt);
        int index = 0;
        while (index < redirect.Entries.Count)
        {
            result.Add(RouteTableCloner.CloneRedirectEntry(
                redirect.Entries[index]));
            index = index + 1;
        }

        return result;
    }

    /// <summary>Reads owned snapshots of the current route table.</summary>
    /// <returns>The route snapshots in deterministic order.</returns>
    public List<DiameterRouteSnapshot> ReadDiameterRouteSnapshots()
    {
        return Table.ReadAllStaticRouteSnapshots();
    }

    internal bool ReadRedirectSourceSecurity(
        DiameterIdentity sourcePeer,
        List<DiameterPeerSnapshot> peers)
    {
        DiameterValueResult source =
            ValueCodecs.NormalizeDiameterIdentity(sourcePeer);
        if (source.Status != DiameterCodecStatus.Success)
        {
            return false;
        }

        int index = 0;
        while (index < peers.Count)
        {
            DiameterPeerSnapshot peer = peers[index];
            DiameterValueResult identity =
                ValueCodecs.NormalizeDiameterIdentity(
                    peer.Configuration.Identity);
            if (identity.Status == DiameterCodecStatus.Success &&
                identity.Text == source.Text)
            {
                if (peer.Connection != null &&
                    peer.Connection.Security != null &&
                    peer.Connection.Security.Mode !=
                        DiameterSecurityMode.None)
                {
                    return true;
                }

                return peer.CurrentEndpoint != null &&
                    peer.CurrentEndpoint.Security !=
                        DiameterSecurityMode.None;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsRealmRedirectInformation(
        DiameterMessage answer)
    {
        if (answer.Avps == null)
        {
            return false;
        }

        int index = 0;
        while (index < answer.Avps.Count)
        {
            DiameterAvp avp = answer.Avps[index];
            if (avp != null && avp.Data != null &&
                avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.RedirectRealm)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
