using System;
using System.Collections.Generic;

namespace diameter;

internal class RouteSessionAffinityStore
{
    internal Dictionary<string, string> RoutesBySession =
        new Dictionary<string, string>(StringComparer.Ordinal);
    internal List<string> SessionOrder = new List<string>();
    internal int Capacity;

    internal RouteSessionAffinityStore(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException("capacity");
        }

        Capacity = capacity;
    }

    internal string ReadAffinityRoute(string sessionId)
    {
        if (sessionId.Length == 0 ||
            !RoutesBySession.ContainsKey(sessionId))
        {
            return string.Empty;
        }

        return RoutesBySession[sessionId];
    }

    internal void StoreAffinityRoute(
        string sessionId,
        string routeIdentifier)
    {
        if (sessionId.Length == 0)
        {
            return;
        }

        if (RoutesBySession.ContainsKey(sessionId))
        {
            RoutesBySession[sessionId] = routeIdentifier;
            return;
        }

        while (RoutesBySession.Count >= Capacity)
        {
            string oldest = SessionOrder[0];
            SessionOrder.RemoveAt(0);
            RoutesBySession.Remove(oldest);
        }

        RoutesBySession[sessionId] = routeIdentifier;
        SessionOrder.Add(sessionId);
    }

    internal void RemoveAffinityRoute(string sessionId)
    {
        if (sessionId.Length > 0 &&
            RoutesBySession.Remove(sessionId))
        {
            SessionOrder.Remove(sessionId);
        }
    }

    internal void RemoveRouteAffinities(string routeIdentifier)
    {
        int index = SessionOrder.Count - 1;
        while (index >= 0)
        {
            string sessionId = SessionOrder[index];
            if (string.Equals(
                RoutesBySession[sessionId],
                routeIdentifier,
                StringComparison.OrdinalIgnoreCase))
            {
                RoutesBySession.Remove(sessionId);
                SessionOrder.RemoveAt(index);
            }

            index = index - 1;
        }
    }
}
