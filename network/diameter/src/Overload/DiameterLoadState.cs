using System;
using System.Collections.Generic;

namespace diameter;

internal class DiameterLoadCacheEntry
{
    internal DiameterLoadReport Report = new DiameterLoadReport();
    internal long ExpiresMonotonicMilliseconds;
}

internal class DiameterLoadReportUpdate
{
    internal bool Accepted;
    internal bool Updated;
    internal bool Active;
    internal DiameterLoadReport Report = new DiameterLoadReport();
    internal string Code = string.Empty;
    internal string Message = string.Empty;
}

internal class DiameterLoadCandidateScore
{
    internal DiameterRouteCandidate Candidate = new DiameterRouteCandidate();
    internal DiameterLoadReport? LoadReport;
    internal long SelectionWeight;
    internal bool LoadApplied;
    internal string Code = string.Empty;
}

internal class DiameterLoadScoringResult
{
    internal List<DiameterLoadCandidateScore> Scores =
        new List<DiameterLoadCandidateScore>();
    internal bool LoadApplied;
    internal long TotalSelectionWeight;
    internal string Code = string.Empty;
}

internal class DiameterLoadState
{
    internal Dictionary<string, DiameterLoadCacheEntry> Entries =
        new Dictionary<string, DiameterLoadCacheEntry>(
            StringComparer.OrdinalIgnoreCase);
    internal List<string> EntryOrder = new List<string>();
    internal object StateLock = new object();
    internal DiameterOverloadConfiguration Configuration;
    internal IDiameterClock Clock;

    internal DiameterLoadState(
        DiameterOverloadConfiguration configuration,
        IDiameterClock clock)
    {
        if (configuration == null || clock == null)
        {
            throw new ArgumentException(
                "Load state requires configuration and time.");
        }

        configuration.ValidateOverloadConfiguration();
        Configuration =
            DiameterOverloadState.CloneDiameterOverloadConfiguration(
                configuration);
        Clock = clock;
    }

    internal DiameterLoadReportUpdate ProcessDiameterLoadReport(
        DiameterLoadReport? report,
        DiameterIdentity? sourcePeer,
        bool serverSelection,
        DateTimeOffset receivedAt)
    {
        if (report == null || sourcePeer == null ||
            receivedAt == default(DateTimeOffset))
        {
            throw new ArgumentException(
                "A load report, source peer, and reception time are required.");
        }

        report.ValidateLoadReport();
        if (ValueCodecs.NormalizeDiameterIdentity(sourcePeer).Status !=
            DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "The load report source peer is invalid.");
        }

        DiameterLoadReport owned = CloneDiameterLoadReport(report);
        owned.SourceId = DiameterLoadProcessor.NormalizeDiameterLoadIdentity(
            owned.SourceId);
        owned.CreatedAt = receivedAt;
        owned.ExpiresAt = receivedAt.AddMilliseconds(
            Configuration.LoadReportIntervalMilliseconds);
        owned.Cached = true;
        DiameterLoadReportUpdate result =
            new DiameterLoadReportUpdate();
        result.Accepted = true;
        result.Report = CloneDiameterLoadReport(owned);
        if (!Configuration.EnableLoadReports)
        {
            result.Code = "load-cache-disabled";
            result.Message =
                "The load report was ignored because load processing is disabled.";
            return result;
        }

        if (report.Source == DiameterLoadSource.Host && !serverSelection)
        {
            result.Code = "load-host-report-ignored";
            result.Message =
                "The host report was ignored because this node does not select servers.";
            return result;
        }

        if (report.Source == DiameterLoadSource.Peer &&
            !ValueCodecs.AreDiameterIdentitiesEqual(
                report.SourceId,
                sourcePeer))
        {
            result.Code = "load-peer-source-ignored";
            result.Message =
                "The peer report was ignored because SourceID did not identify the adjacent peer.";
            return result;
        }

        string key = CreateDiameterLoadReportKey(owned);
        long monotonicNow = Clock.ReadMonotonicMilliseconds();
        DateTimeOffset utcNow = Clock.ReadUtcTime();
        lock (StateLock)
        {
            PurgeExpiredDiameterLoadEntries(utcNow, monotonicNow);
            if (!Entries.ContainsKey(key))
            {
                while (EntryOrder.Count >=
                    Configuration.MaximumLoadReports)
                {
                    RemoveDiameterLoadEntry(EntryOrder[0]);
                }

                EntryOrder.Add(key);
            }

            DiameterLoadCacheEntry entry = new DiameterLoadCacheEntry();
            entry.Report = CloneDiameterLoadReport(owned);
            entry.ExpiresMonotonicMilliseconds =
                AddDiameterLoadMonotonicDuration(
                    monotonicNow,
                    Configuration.LoadReportIntervalMilliseconds);
            Entries[key] = entry;
        }

        result.Updated = true;
        result.Active = true;
        result.Code = "load-report-cached";
        result.Message =
            "The load report updated route-selection state.";
        return result;
    }

    internal List<DiameterLoadReport> ReadActiveDiameterLoadReports()
    {
        DateTimeOffset utcNow = Clock.ReadUtcTime();
        long monotonicNow = Clock.ReadMonotonicMilliseconds();
        List<DiameterLoadReport> result =
            new List<DiameterLoadReport>();
        lock (StateLock)
        {
            PurgeExpiredDiameterLoadEntries(utcNow, monotonicNow);
            int index = 0;
            while (index < EntryOrder.Count)
            {
                string key = EntryOrder[index];
                if (Entries.ContainsKey(key))
                {
                    result.Add(CloneDiameterLoadReport(
                        Entries[key].Report));
                }

                index = index + 1;
            }
        }

        return result;
    }

    internal DiameterLoadScoringResult ScoreDiameterRouteCandidates(
        List<DiameterRouteCandidate>? candidates,
        bool serverSelection)
    {
        if (candidates == null || candidates.Count == 0)
        {
            throw new ArgumentException(
                "Load scoring requires route candidates.");
        }

        DateTimeOffset utcNow = Clock.ReadUtcTime();
        long monotonicNow = Clock.ReadMonotonicMilliseconds();
        DiameterLoadScoringResult result =
            new DiameterLoadScoringResult();
        bool complete = Configuration.EnableLoadReports;
        long loadTotal = 0L;
        lock (StateLock)
        {
            PurgeExpiredDiameterLoadEntries(utcNow, monotonicNow);
            int index = 0;
            while (index < candidates.Count)
            {
                DiameterRouteCandidate candidate = candidates[index];
                if (candidate == null || candidate.Weight <= 0)
                {
                    throw new ArgumentException(
                        "Load scoring requires valid positive-weight candidates.");
                }

                DiameterLoadCandidateScore score =
                    new DiameterLoadCandidateScore();
                score.Candidate =
                    RouteTableCloner.CloneRouteCandidate(candidate);
                score.SelectionWeight = candidate.Weight;
                DiameterLoadReport? report =
                    FindDiameterCandidateLoadReport(
                        candidate,
                        serverSelection);
                if (report == null)
                {
                    complete = false;
                    score.Code = "load-score-unavailable";
                }
                else
                {
                    score.LoadReport = CloneDiameterLoadReport(report);
                    score.SelectionWeight =
                        (long)candidate.Weight *
                        (long)report.LoadValue;
                    score.Code = "load-score-calculated";
                    if (long.MaxValue - loadTotal <
                        score.SelectionWeight)
                    {
                        throw new DiameterRoutingException(
                            "The load-adjusted route weight total is too large.");
                    }

                    loadTotal = loadTotal + score.SelectionWeight;
                }

                result.Scores.Add(score);
                index = index + 1;
            }
        }

        if (complete && loadTotal > 0L)
        {
            result.LoadApplied = true;
            result.TotalSelectionWeight = loadTotal;
            result.Code = "load-scores-applied";
            int index = 0;
            while (index < result.Scores.Count)
            {
                result.Scores[index].LoadApplied = true;
                index = index + 1;
            }

            return result;
        }

        result.Code = complete ?
            "load-scores-zero-fallback" :
            "load-scores-incomplete-fallback";
        int fallbackIndex = 0;
        while (fallbackIndex < result.Scores.Count)
        {
            DiameterLoadCandidateScore score =
                result.Scores[fallbackIndex];
            score.SelectionWeight = score.Candidate.Weight;
            score.LoadApplied = false;
            if (long.MaxValue - result.TotalSelectionWeight <
                score.SelectionWeight)
            {
                throw new DiameterRoutingException(
                    "The route weight total is too large.");
            }

            result.TotalSelectionWeight =
                result.TotalSelectionWeight + score.SelectionWeight;
            fallbackIndex = fallbackIndex + 1;
        }

        return result;
    }

    internal DiameterLoadReport? FindDiameterCandidateLoadReport(
        DiameterRouteCandidate candidate,
        bool serverSelection)
    {
        if (serverSelection)
        {
            DiameterIdentity? host =
                candidate.Configuration.DestinationHost;
            if (host == null)
            {
                host = candidate.PeerIdentity;
            }

            DiameterLoadReport? hostReport =
                FindDiameterLoadReport(
                    DiameterLoadSource.Host,
                    host);
            if (hostReport != null)
            {
                return hostReport;
            }
        }

        return FindDiameterLoadReport(
            DiameterLoadSource.Peer,
            candidate.PeerIdentity);
    }

    internal DiameterLoadReport? FindDiameterLoadReport(
        DiameterLoadSource source,
        DiameterIdentity? identity)
    {
        if (identity == null)
        {
            return null;
        }

        string key = CreateDiameterLoadReportIdentityKey(source, identity);
        if (!Entries.ContainsKey(key))
        {
            return null;
        }

        return Entries[key].Report;
    }

    internal void PurgeExpiredDiameterLoadReports()
    {
        DateTimeOffset utcNow = Clock.ReadUtcTime();
        long monotonicNow = Clock.ReadMonotonicMilliseconds();
        lock (StateLock)
        {
            PurgeExpiredDiameterLoadEntries(utcNow, monotonicNow);
        }
    }

    internal void PurgeExpiredDiameterLoadEntries(
        DateTimeOffset utcNow,
        long monotonicNow)
    {
        int index = 0;
        while (index < EntryOrder.Count)
        {
            string key = EntryOrder[index];
            if (!Entries.ContainsKey(key) ||
                Entries[key].Report.ExpiresAt <= utcNow ||
                Entries[key].ExpiresMonotonicMilliseconds <= monotonicNow)
            {
                Entries.Remove(key);
                EntryOrder.RemoveAt(index);
            }
            else
            {
                index = index + 1;
            }
        }
    }

    internal void RemoveDiameterLoadEntry(string key)
    {
        Entries.Remove(key);
        int index = 0;
        while (index < EntryOrder.Count)
        {
            if (string.Equals(
                EntryOrder[index],
                key,
                StringComparison.OrdinalIgnoreCase))
            {
                EntryOrder.RemoveAt(index);
                return;
            }

            index = index + 1;
        }
    }

    internal static string CreateDiameterLoadReportKey(
        DiameterLoadReport report)
    {
        return CreateDiameterLoadReportIdentityKey(
            report.Source,
            report.SourceId);
    }

    internal static string CreateDiameterLoadReportIdentityKey(
        DiameterLoadSource source,
        DiameterIdentity identity)
    {
        string prefix = source == DiameterLoadSource.Host ?
            "host" : "peer";
        return prefix + "|" +
            ValueCodecs.NormalizeDiameterIdentity(identity).Text;
    }

    internal static long AddDiameterLoadMonotonicDuration(
        long monotonicNow,
        int durationMilliseconds)
    {
        if (durationMilliseconds > 0 &&
            monotonicNow > long.MaxValue - durationMilliseconds)
        {
            return long.MaxValue;
        }

        return monotonicNow + durationMilliseconds;
    }

    internal static DiameterLoadReport CloneDiameterLoadReport(
        DiameterLoadReport source)
    {
        DiameterLoadReport result = new DiameterLoadReport();
        result.Source = source.Source;
        result.SourceId = DiameterLoadProcessor.NormalizeDiameterLoadIdentity(
            source.SourceId);
        result.LoadValue = source.LoadValue;
        int index = 0;
        while (index < source.ExtensionAvps.Count)
        {
            result.ExtensionAvps.Add(
                DiameterLoadProcessor.CloneDiameterLoadAvp(
                    source.ExtensionAvps[index]));
            index = index + 1;
        }

        result.CreatedAt = source.CreatedAt;
        result.ExpiresAt = source.ExpiresAt;
        result.Cached = source.Cached;
        return result;
    }
}
