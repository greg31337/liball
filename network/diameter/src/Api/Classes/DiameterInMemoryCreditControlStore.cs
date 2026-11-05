using System;
using System.Collections.Generic;

namespace diameter;

/// <summary>Stores credit-control sessions atomically in process memory.</summary>
public class DiameterInMemoryCreditControlStore : ICreditControlStore
{
    internal object StoreLock = new object();
    internal List<DiameterCreditControlSession> Sessions =
        new List<DiameterCreditControlSession>();

    /// <summary>Initializes an empty in-memory credit-control store.</summary>
    public DiameterInMemoryCreditControlStore()
    {
    }

    /// <summary>Loads one independently owned credit-control session.</summary>
    /// <param name="sessionId">The globally unique Session-Id.</param>
    /// <returns>An owned session snapshot, or <c>null</c> when absent.</returns>
    public DiameterCreditControlSession? LoadDiameterCreditControlSession(
        string? sessionId)
    {
        lock (StoreLock)
        {
            int index = FindCreditControlStoreSessionIndex(sessionId);
            if (index < 0)
            {
                return null;
            }

            return CreditControlDataCloner.CloneCreditControlSession(
                Sessions[index]);
        }
    }

    /// <summary>Atomically creates or replaces one credit-control session.</summary>
    /// <param name="session">The complete owned session state.</param>
    /// <param name="expectedVersion">Zero for creation or the current version for replacement.</param>
    /// <returns>The independently owned persisted session.</returns>
    /// <exception cref="ArgumentException">The session is absent or invalid.</exception>
    /// <exception cref="DiameterCreditControlException">The expected version does not match.</exception>
    public DiameterCreditControlSession SaveDiameterCreditControlSession(
        DiameterCreditControlSession? session,
        long expectedVersion)
    {
        if (session == null)
        {
            throw new ArgumentException(
                "A credit-control session is required.");
        }

        if (expectedVersion < 0)
        {
            throw new ArgumentOutOfRangeException("expectedVersion");
        }

        session.ValidateCreditControlSession();
        lock (StoreLock)
        {
            int index = FindCreditControlStoreSessionIndex(
                session.SessionId);
            if (index < 0)
            {
                if (expectedVersion != 0)
                {
                    throw new DiameterCreditControlException(
                        "The expected credit-control session does not exist.");
                }

                DiameterCreditControlSession created =
                    CreditControlDataCloner.CloneCreditControlSession(
                        session);
                created.Version = 1;
                Sessions.Add(created);
                return CreditControlDataCloner.CloneCreditControlSession(
                    created);
            }

            if (Sessions[index].Version != expectedVersion)
            {
                throw new DiameterCreditControlException(
                    "The credit-control session version changed.");
            }

            if (expectedVersion == long.MaxValue)
            {
                throw new DiameterCreditControlException(
                    "The credit-control session version is exhausted.");
            }

            DiameterCreditControlSession updated =
                CreditControlDataCloner.CloneCreditControlSession(session);
            updated.Version = expectedVersion + 1;
            Sessions[index] = updated;
            return CreditControlDataCloner.CloneCreditControlSession(
                updated);
        }
    }

    /// <summary>Atomically removes one version-matched session.</summary>
    /// <param name="sessionId">The globally unique Session-Id.</param>
    /// <param name="expectedVersion">The version that must still be current.</param>
    /// <returns><c>true</c> when the matching session was removed.</returns>
    public bool RemoveDiameterCreditControlSession(
        string? sessionId,
        long expectedVersion)
    {
        if (expectedVersion <= 0)
        {
            return false;
        }

        lock (StoreLock)
        {
            int index = FindCreditControlStoreSessionIndex(sessionId);
            if (index < 0 ||
                Sessions[index].Version != expectedVersion)
            {
                return false;
            }

            Sessions.RemoveAt(index);
            return true;
        }
    }

    /// <summary>Reads independent sessions in creation order.</summary>
    /// <returns>All retained credit-control sessions.</returns>
    public List<DiameterCreditControlSession>
        ReadStoredDiameterCreditControlSessions()
    {
        lock (StoreLock)
        {
            List<DiameterCreditControlSession> result =
                new List<DiameterCreditControlSession>();
            int index = 0;
            while (index < Sessions.Count)
            {
                result.Add(CreditControlDataCloner
                    .CloneCreditControlSession(Sessions[index]));
                index = index + 1;
            }

            return result;
        }
    }

    internal int FindCreditControlStoreSessionIndex(string? sessionId)
    {
        int index = 0;
        while (index < Sessions.Count)
        {
            if (Sessions[index].SessionId == sessionId)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }
}
