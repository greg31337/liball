using System;

namespace diameter;

internal static class CreditControlFailoverState
{
    internal static void InitializeCreditControlServerAffinity(
        DiameterCreditControlSession session,
        DiameterCreditControlRequest request)
    {
        session.PrimaryServer = CreateCreditControlServer(request);
        session.AlternateServers = CreditControlDataCloner
            .CloneCreditControlServers(request.AlternateServers);
        session.AttemptedServers.Clear();
        DiameterCreditControlServer? primary = session.PrimaryServer;
        if (primary != null)
        {
            session.AttemptedServers.Add(
                ReadCreditControlServerKey(primary));
        }
    }

    internal static void ApplyCreditControlServerAffinity(
        DiameterCreditControlSession session,
        DiameterCreditControlRequest request)
    {
        request.DestinationHost = CreditControlDataCloner
            .CloneOptionalCreditControlIdentity(session.DestinationHost);
        request.PeerIdentity = CreditControlDataCloner
            .CloneOptionalCreditControlIdentity(session.PeerIdentity);
        request.RouteIdentifier = session.RouteIdentifier;
        request.AlternateServers = CreditControlDataCloner
            .CloneCreditControlServers(session.AlternateServers);
    }

    internal static void ResetCreditControlAttemptedServers(
        DiameterCreditControlSession session,
        DiameterCreditControlRequest request)
    {
        session.AttemptedServers.Clear();
        DiameterCreditControlServer? current =
            CreateCreditControlServer(request);
        if (current != null)
        {
            session.AttemptedServers.Add(
                ReadCreditControlServerKey(current));
        }
    }

    internal static bool TrySelectAlternateCreditControlServer(
        DiameterCreditControlSession session,
        DiameterCreditControlRequest request,
        DateTimeOffset now)
    {
        if (!request.AllowFailover ||
            session.PendingRequestType ==
                DiameterCreditControlRequestType.Termination ||
            session.PendingRequestType !=
                DiameterCreditControlRequestType.Initial &&
            !session.SessionFailoverSupported)
        {
            return false;
        }

        int index = 0;
        while (index < session.AlternateServers.Count)
        {
            DiameterCreditControlServer candidate =
                session.AlternateServers[index];
            string key = ReadCreditControlServerKey(candidate);
            if (!ContainsCreditControlServerKey(
                session.AttemptedServers,
                key))
            {
                session.AttemptedServers.Add(key);
                ApplyCreditControlServer(session, request, candidate);
                session.LastRequest = CreditControlDataCloner
                    .CloneCreditControlRequest(request);
                session.AttemptCount = session.AttemptCount + 1;
                session.TransactionDeadline = now.AddMilliseconds(
                    request.TimeoutMilliseconds);
                session.TransactionDeadlinePresent = true;
                session.FailoverActive = true;
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool TryFailbackCreditControlServer(
        DiameterCreditControlSession session)
    {
        DiameterCreditControlServer? primary = session.PrimaryServer;
        if (!session.FailoverActive || primary == null)
        {
            return false;
        }

        session.DestinationHost = CreditControlDataCloner
            .CloneOptionalCreditControlIdentity(primary.DestinationHost);
        session.PeerIdentity = CreditControlDataCloner
            .CloneOptionalCreditControlIdentity(primary.PeerIdentity);
        session.RouteIdentifier = primary.RouteIdentifier;
        session.FailoverActive = false;
        session.AttemptedServers.Clear();
        session.AttemptedServers.Add(
            ReadCreditControlServerKey(primary));
        return true;
    }

    internal static DiameterCreditControlServer?
        CreateCreditControlServer(DiameterCreditControlRequest request)
    {
        if (request.DestinationHost == null &&
            request.PeerIdentity == null &&
            request.RouteIdentifier.Length == 0)
        {
            return null;
        }

        DiameterCreditControlServer result =
            new DiameterCreditControlServer();
        result.DestinationHost = CreditControlDataCloner
            .CloneOptionalCreditControlIdentity(request.DestinationHost);
        result.PeerIdentity = CreditControlDataCloner
            .CloneOptionalCreditControlIdentity(request.PeerIdentity);
        result.RouteIdentifier = request.RouteIdentifier;
        return result;
    }

    internal static void ApplyCreditControlServer(
        DiameterCreditControlSession session,
        DiameterCreditControlRequest request,
        DiameterCreditControlServer server)
    {
        request.DestinationHost = CreditControlDataCloner
            .CloneOptionalCreditControlIdentity(server.DestinationHost);
        request.PeerIdentity = CreditControlDataCloner
            .CloneOptionalCreditControlIdentity(server.PeerIdentity);
        request.RouteIdentifier = server.RouteIdentifier;
        session.DestinationHost = CreditControlDataCloner
            .CloneOptionalCreditControlIdentity(server.DestinationHost);
        session.PeerIdentity = CreditControlDataCloner
            .CloneOptionalCreditControlIdentity(server.PeerIdentity);
        session.RouteIdentifier = server.RouteIdentifier;
    }

    internal static string ReadCreditControlServerKey(
        DiameterCreditControlServer server)
    {
        string host = server.DestinationHost == null ?
            string.Empty : server.DestinationHost.Value;
        string peer = server.PeerIdentity == null ?
            string.Empty : server.PeerIdentity.Value;
        return host + "|" + peer + "|" + server.RouteIdentifier;
    }

    internal static bool ContainsCreditControlServerKey(
        System.Collections.Generic.List<string> keys,
        string key)
    {
        int index = 0;
        while (index < keys.Count)
        {
            if (keys[index] == key)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
