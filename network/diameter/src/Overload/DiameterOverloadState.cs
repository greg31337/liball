using System;
using System.Collections.Generic;
using System.Globalization;

namespace diameter;

internal interface IOverloadRandom
{
    int ReadOverloadRandomPercentage();
}

internal class SystemOverloadRandom : IOverloadRandom
{
    internal SystemOverloadRandom()
    {
    }

    public int ReadOverloadRandomPercentage()
    {
        return Random.Shared.Next(1, 101);
    }
}

internal class DiameterOverloadCacheEntry
{
    internal DiameterOverloadReport Report = new DiameterOverloadReport();
    internal long ExpiresMonotonicMilliseconds;
}

internal class DiameterOverloadReportUpdate
{
    internal bool Accepted;
    internal bool Updated;
    internal bool Active;
    internal DiameterOverloadReport Report = new DiameterOverloadReport();
    internal string Code = string.Empty;
    internal string Message = string.Empty;
}

internal class DiameterOverloadState
{
    internal Dictionary<string, DiameterOverloadCacheEntry> Entries =
        new Dictionary<string, DiameterOverloadCacheEntry>(
            StringComparer.OrdinalIgnoreCase);
    internal Dictionary<string, DiameterRateBucketState> RateBuckets =
        new Dictionary<string, DiameterRateBucketState>(
            StringComparer.OrdinalIgnoreCase);
    internal List<string> EntryOrder = new List<string>();
    internal object StateLock = new object();
    internal DiameterOverloadConfiguration Configuration;
    internal IDiameterClock Clock;
    internal IOverloadRandom RandomSource;

    internal DiameterOverloadState(
        DiameterOverloadConfiguration configuration,
        IDiameterClock clock,
        IOverloadRandom randomSource)
    {
        if (configuration == null || clock == null || randomSource == null)
        {
            throw new ArgumentException(
                "Overload state requires configuration, time, and randomness.");
        }

        configuration.ValidateOverloadConfiguration();
        Configuration = CloneDiameterOverloadConfiguration(configuration);
        Clock = clock;
        RandomSource = randomSource;
    }

    internal DiameterOverloadReportUpdate ProcessDiameterOverloadReport(
        DiameterOverloadReport? report,
        DateTimeOffset receivedAt)
    {
        if (report == null)
        {
            throw new ArgumentException("An overload report is required.");
        }

        if (receivedAt == default(DateTimeOffset))
        {
            throw new ArgumentException(
                "An overload report requires its reception time.");
        }

        report.ValidateOverloadReport();
        if (report.ReportType == DiameterOverloadReportType.Peer)
        {
            throw new ArgumentException(
                "Peer overload reports require peer-report processing.");
        }

        if (!SupportsDiameterOverloadAlgorithm(report.Algorithm))
        {
            throw new ArgumentException(
                "The overload report selected an unsupported algorithm.");
        }

        return StoreDiameterOverloadReport(report, receivedAt);
    }

    internal DiameterOverloadReportUpdate ProcessDiameterPeerOverloadReport(
        DiameterOverloadReport? report,
        DiameterIdentity? sourcePeer,
        DateTimeOffset receivedAt)
    {
        if (report == null || sourcePeer == null)
        {
            throw new ArgumentException(
                "A peer overload report and source peer are required.");
        }

        if (receivedAt == default(DateTimeOffset))
        {
            throw new ArgumentException(
                "A peer overload report requires its reception time.");
        }

        report.ValidateOverloadReport();
        if (!Configuration.EnablePeerReports ||
            report.ReportType != DiameterOverloadReportType.Peer ||
            ValueCodecs.NormalizeDiameterIdentity(sourcePeer).Status !=
                DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "Peer overload reporting is not valid for this state.");
        }

        if (report.SourceId == null ||
            !ValueCodecs.AreDiameterIdentitiesEqual(
                report.SourceId,
                sourcePeer))
        {
            DiameterOverloadReportUpdate ignored =
                new DiameterOverloadReportUpdate();
            ignored.Accepted = true;
            ignored.Report = CloneDiameterOverloadReport(report);
            ignored.Code = "peer-overload-source-ignored";
            ignored.Message =
                "The peer report was ignored because SourceID did not identify the adjacent peer.";
            return ignored;
        }

        if (!SupportsDiameterOverloadAlgorithm(report.Algorithm))
        {
            throw new ArgumentException(
                "The peer overload report selected an unsupported algorithm.");
        }

        return StoreDiameterOverloadReport(report, receivedAt);
    }

    internal DiameterOverloadReportUpdate StoreDiameterOverloadReport(
        DiameterOverloadReport report,
        DateTimeOffset receivedAt)
    {
        DiameterOverloadReport owned =
            CloneDiameterOverloadReport(report);
        NormalizeDiameterOverloadReportScope(owned);
        uint validity = owned.ValidityDurationSeconds;
        if (validity > Configuration.MaximumReportValiditySeconds ||
            validity > 86400U)
        {
            validity = Configuration.DefaultReportValiditySeconds;
        }

        owned.ValidityDurationSeconds = validity;
        owned.CreatedAt = receivedAt;
        owned.ExpiresAt = receivedAt.AddSeconds(validity);
        owned.Cached = validity > 0U;
        string key = CreateDiameterOverloadReportKey(owned);
        DiameterOverloadReportUpdate result =
            new DiameterOverloadReportUpdate();
        result.Accepted = true;
        result.Report = CloneDiameterOverloadReport(owned);
        if (!Configuration.Enabled)
        {
            result.Code = "overload-cache-disabled";
            result.Message =
                "The overload report was ignored because overload control is disabled.";
            return result;
        }

        long monotonicNow = Clock.ReadMonotonicMilliseconds();
        DateTimeOffset utcNow = Clock.ReadUtcTime();
        lock (StateLock)
        {
            PurgeExpiredDiameterOverloadEntries(utcNow, monotonicNow);
            if (Entries.ContainsKey(key))
            {
                DiameterOverloadReport existing = Entries[key].Report;
                if (!IsNewerDiameterOverloadSequence(
                    owned.SequenceNumber,
                    existing.SequenceNumber))
                {
                    result.Report = CloneDiameterOverloadReport(existing);
                    result.Active = true;
                    result.Code = "overload-report-ignored";
                    result.Message =
                        "The overload report sequence was not newer than cached state.";
                    return result;
                }
            }

            result.Updated = true;
            if (validity == 0U)
            {
                RemoveDiameterOverloadEntry(key);
                result.Code = "overload-report-ended";
                result.Message =
                    "The overload report ended the matching overload condition.";
                return result;
            }

            if (!Entries.ContainsKey(key))
            {
                while (EntryOrder.Count >=
                    Configuration.MaximumOverloadReports)
                {
                    RemoveDiameterOverloadEntry(EntryOrder[0]);
                }

                EntryOrder.Add(key);
            }

            DiameterOverloadCacheEntry entry =
                new DiameterOverloadCacheEntry();
            entry.Report = CloneDiameterOverloadReport(owned);
            entry.ExpiresMonotonicMilliseconds =
                AddDiameterOverloadMonotonicDuration(
                    monotonicNow,
                    validity);
            Entries[key] = entry;
            if (owned.Algorithm == DiameterOverloadAlgorithm.Rate)
            {
                if (RateBuckets.ContainsKey(key))
                {
                    DiameterRateScheduler.UpdateDiameterRateBucket(
                        RateBuckets[key],
                        owned,
                        monotonicNow);
                }
                else
                {
                    RateBuckets[key] =
                        DiameterRateScheduler.CreateDiameterRateBucket(
                            owned,
                            monotonicNow);
                }
            }
            else
            {
                RateBuckets.Remove(key);
            }

            result.Active = true;
            result.Code = "overload-report-cached";
            result.Message =
                "The overload report created or updated cached overload state.";
            return result;
        }
    }

    internal List<DiameterOverloadReport> ReadActiveDiameterOverloadReports()
    {
        List<DiameterOverloadReport> result =
            new List<DiameterOverloadReport>();
        lock (StateLock)
        {
            PurgeExpiredDiameterOverloadEntries(
                Clock.ReadUtcTime(),
                Clock.ReadMonotonicMilliseconds());
            int index = 0;
            while (index < EntryOrder.Count)
            {
                string key = EntryOrder[index];
                if (Entries.ContainsKey(key))
                {
                    result.Add(CloneDiameterOverloadReport(
                        Entries[key].Report));
                }

                index = index + 1;
            }
        }

        return result;
    }

    internal DiameterAdmissionDecision EvaluateDiameterLossAdmission(
        DiameterAdmissionRequest? request)
    {
        if (request == null)
        {
            throw new ArgumentException("An overload admission request is required.");
        }

        request.ValidateAdmissionRequest();
        DiameterAdmissionRequest ownedRequest =
            CloneDiameterAdmissionRequest(request);
        DateTimeOffset evaluatedAt = request.EvaluatedAt;
        if (evaluatedAt == default(DateTimeOffset))
        {
            evaluatedAt = Clock.ReadUtcTime();
            ownedRequest.EvaluatedAt = evaluatedAt;
        }

        List<DiameterOverloadReport> matches =
            ReadMatchingDiameterOverloadReports(request, evaluatedAt);
        DiameterAdmissionDecision result =
            new DiameterAdmissionDecision();
        result.Request = ownedRequest;
        int copyIndex = 0;
        while (copyIndex < matches.Count)
        {
            result.Reports.Add(
                CloneDiameterOverloadReport(matches[copyIndex]));
            copyIndex = copyIndex + 1;
        }

        if (!Configuration.Enabled || matches.Count == 0)
        {
            result.Code = request.Probe ?
                "overload-probe-admitted" :
                "overload-not-applicable";
            result.Message = request.Probe ?
                "The conservative probe was admitted without active overload state." :
                "No active overload report matched the request.";
            return result;
        }

        bool abatement = false;
        int index = 0;
        while (index < matches.Count && !abatement)
        {
            DiameterOverloadReport match = matches[index];
            if (match.Algorithm == DiameterOverloadAlgorithm.Loss &&
                SelectDiameterLossAbatement(match.ReductionPercentage))
            {
                abatement = true;
            }

            index = index + 1;
        }

        if (!abatement)
        {
            result.Code = "overload-loss-admitted";
            result.Message =
                "The loss algorithm selected normal routing for the request.";
            return result;
        }

        result.Admitted = false;
        result.OverloadApplied = true;
        if (request.AllowDiversion)
        {
            result.Action = DiameterAdmissionAction.Divert;
            if (request.Candidate != null)
            {
                result.DiversionCandidate =
                    RouteTableCloner.CloneRouteCandidate(request.Candidate);
            }
            else
            {
                result.DiversionCandidate =
                    new DiameterRouteCandidate();
                result.DiversionCandidate.Code =
                    "overload-diversion-required";
                result.DiversionCandidate.Message =
                    "Routing must select an alternate candidate.";
            }

            result.Code = "overload-divert";
            result.Message =
                "The loss algorithm selected the request for diversion.";
            return result;
        }

        result.Action = DiameterAdmissionAction.Throttle;
        result.ResultCode = DiameterResultCodes.TooBusy;
        result.RetryAfterMilliseconds =
            ReadDiameterOverloadRetryMilliseconds(matches, evaluatedAt);
        result.Code = "overload-throttle";
        result.Message =
            "The loss algorithm selected the request for throttling.";
        return result;
    }

    internal DiameterAdmissionDecision EvaluateDiameterRateAdmission(
        DiameterAdmissionRequest? request)
    {
        if (request == null)
        {
            throw new ArgumentException("An overload admission request is required.");
        }

        request.ValidateAdmissionRequest();
        DateTimeOffset utcNow = Clock.ReadUtcTime();
        long monotonicNow = Clock.ReadMonotonicMilliseconds();
        DateTimeOffset evaluatedAt = request.EvaluatedAt;
        if (evaluatedAt == default(DateTimeOffset))
        {
            evaluatedAt = utcNow;
        }

        DiameterAdmissionDecision result =
            new DiameterAdmissionDecision();
        result.Request = CloneDiameterAdmissionRequest(request);
        result.Request.EvaluatedAt = evaluatedAt;
        List<DiameterOverloadReport> rateReports =
            new List<DiameterOverloadReport>();
        List<DiameterRateScheduleDecision> schedules =
            new List<DiameterRateScheduleDecision>();
        bool excess = false;
        int retryAfterMilliseconds = 0;
        lock (StateLock)
        {
            PurgeExpiredDiameterOverloadEntries(utcNow, monotonicNow);
            List<DiameterOverloadReport> matches =
                CollectMatchingDiameterOverloadReports(
                    request,
                    evaluatedAt);
            int index = 0;
            while (index < matches.Count)
            {
                DiameterOverloadReport report = matches[index];
                if (report.Algorithm == DiameterOverloadAlgorithm.Rate)
                {
                    string key = CreateDiameterOverloadReportKey(report);
                    if (!RateBuckets.ContainsKey(key))
                    {
                        RateBuckets[key] =
                            DiameterRateScheduler.CreateDiameterRateBucket(
                                report,
                                monotonicNow);
                    }

                    DiameterRateScheduleDecision schedule =
                        DiameterRateScheduler.EvaluateDiameterRateBucket(
                            RateBuckets[key],
                            Configuration.RateToleranceMilliseconds,
                            monotonicNow);
                    rateReports.Add(report);
                    schedules.Add(schedule);
                    if (schedule.Excess)
                    {
                        excess = true;
                        int scheduleRetry = schedule.RetryAfterMilliseconds;
                        if (report.MaximumRate == 0U)
                        {
                            scheduleRetry =
                                ReadDiameterOverloadReportRemainingMilliseconds(
                                    report,
                                    evaluatedAt);
                        }

                        if (scheduleRetry > retryAfterMilliseconds)
                        {
                            retryAfterMilliseconds = scheduleRetry;
                        }
                    }
                }

                index = index + 1;
            }

            if (!excess)
            {
                int commitIndex = 0;
                while (commitIndex < rateReports.Count)
                {
                    string key = CreateDiameterOverloadReportKey(
                        rateReports[commitIndex]);
                    DiameterRateScheduler.CommitDiameterRateBucket(
                        RateBuckets[key],
                        schedules[commitIndex]);
                    commitIndex = commitIndex + 1;
                }
            }
        }

        int copyIndex = 0;
        while (copyIndex < rateReports.Count)
        {
            result.Reports.Add(CloneDiameterOverloadReport(
                rateReports[copyIndex]));
            copyIndex = copyIndex + 1;
        }

        if (!Configuration.Enabled || rateReports.Count == 0)
        {
            result.Code = request.Probe ?
                "overload-rate-probe-admitted" :
                "overload-rate-not-applicable";
            result.Message = request.Probe ?
                "The conservative probe was admitted without active rate state." :
                "No active rate overload report matched the request.";
            return result;
        }

        if (!excess)
        {
            result.Code = "overload-rate-admitted";
            result.Message =
                "The request conforms to every matching maximum rate.";
            return result;
        }

        result.Admitted = false;
        result.OverloadApplied = true;
        if (request.AllowDiversion)
        {
            result.Action = DiameterAdmissionAction.Divert;
            if (request.Candidate != null)
            {
                result.DiversionCandidate =
                    RouteTableCloner.CloneRouteCandidate(request.Candidate);
            }
            else
            {
                result.DiversionCandidate =
                    new DiameterRouteCandidate();
                result.DiversionCandidate.Code =
                    "overload-rate-diversion-required";
                result.DiversionCandidate.Message =
                    "Routing must select an alternate rate-control candidate.";
            }

            result.Code = "overload-rate-excess-divert";
            result.Message =
                "The excess request was selected for diversion.";
            return result;
        }

        result.Action = DiameterAdmissionAction.Throttle;
        result.ResultCode = DiameterResultCodes.TooBusy;
        result.RetryAfterMilliseconds = retryAfterMilliseconds;
        result.Code = "overload-rate-excess-throttle";
        result.Message =
            "The excess request was selected for throttling.";
        return result;
    }

    internal List<DiameterOverloadReport> ReadMatchingDiameterOverloadReports(
        DiameterAdmissionRequest request,
        DateTimeOffset evaluatedAt)
    {
        DateTimeOffset utcNow = Clock.ReadUtcTime();
        long monotonicNow = Clock.ReadMonotonicMilliseconds();
        lock (StateLock)
        {
            PurgeExpiredDiameterOverloadEntries(
                utcNow,
                monotonicNow);
            return CollectMatchingDiameterOverloadReports(
                request,
                evaluatedAt);
        }
    }

    internal List<DiameterOverloadReport> CollectMatchingDiameterOverloadReports(
        DiameterAdmissionRequest request,
        DateTimeOffset evaluatedAt)
    {
        List<DiameterOverloadReport> result =
            new List<DiameterOverloadReport>();
        int pass = 0;
        while (pass < 2)
        {
            int index = 0;
            while (index < EntryOrder.Count)
            {
                string key = EntryOrder[index];
                if (Entries.ContainsKey(key))
                {
                    DiameterOverloadReport report = Entries[key].Report;
                    bool peer = report.ReportType ==
                        DiameterOverloadReportType.Peer;
                    if (report.ExpiresAt > evaluatedAt &&
                        peer == (pass == 1) &&
                        MatchesDiameterOverloadScope(report, request))
                    {
                        result.Add(CloneDiameterOverloadReport(report));
                    }
                }

                index = index + 1;
            }

            pass = pass + 1;
        }

        return result;
    }

    internal void PurgeExpiredDiameterOverloadReports()
    {
        lock (StateLock)
        {
            PurgeExpiredDiameterOverloadEntries(
                Clock.ReadUtcTime(),
                Clock.ReadMonotonicMilliseconds());
        }
    }

    internal void PurgeExpiredDiameterOverloadEntries(
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
                RateBuckets.Remove(key);
                EntryOrder.RemoveAt(index);
            }
            else
            {
                index = index + 1;
            }
        }
    }

    internal void RemoveDiameterOverloadEntry(string key)
    {
        Entries.Remove(key);
        RateBuckets.Remove(key);
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

    internal bool MatchesDiameterOverloadScope(
        DiameterOverloadReport report,
        DiameterAdmissionRequest request)
    {
        if (report.ApplicationId != request.Request.ApplicationId)
        {
            return false;
        }

        bool inspectHost = request.Scope == DiameterOverloadScope.Host ||
            request.Scope == DiameterOverloadScope.Application;
        bool inspectRealm = request.Scope == DiameterOverloadScope.Realm ||
            request.Scope == DiameterOverloadScope.Application;
        if (report.ReportType == DiameterOverloadReportType.Host &&
            inspectHost && report.Host != null)
        {
            DiameterIdentity? target =
                ReadDiameterOverloadTargetHost(request);
            return target != null &&
                ValueCodecs.AreDiameterIdentitiesEqual(report.Host, target);
        }

        if (report.ReportType == DiameterOverloadReportType.Realm &&
            inspectRealm && report.Realm != null &&
            request.Request.DestinationRealm != null)
        {
            return ValueCodecs.NormalizeDiameterRealm(report.Realm).Text ==
                ValueCodecs.NormalizeDiameterRealm(
                    request.Request.DestinationRealm).Text;
        }

        if (report.ReportType == DiameterOverloadReportType.Peer &&
            (request.Scope == DiameterOverloadScope.Peer ||
            request.Scope == DiameterOverloadScope.Application) &&
            report.SourceId != null)
        {
            DiameterIdentity? target =
                ReadDiameterOverloadTargetPeer(request);
            return target != null &&
                ValueCodecs.AreDiameterIdentitiesEqual(
                    report.SourceId,
                    target);
        }

        return false;
    }

    internal DiameterIdentity? ReadDiameterOverloadTargetHost(
        DiameterAdmissionRequest request)
    {
        if (request.Request.DestinationHost != null)
        {
            return request.Request.DestinationHost;
        }

        if (request.Candidate != null &&
            request.Candidate.Configuration.DestinationHost != null)
        {
            return request.Candidate.Configuration.DestinationHost;
        }

        if (request.Candidate != null)
        {
            return request.Candidate.PeerIdentity;
        }

        return null;
    }

    internal DiameterIdentity? ReadDiameterOverloadTargetPeer(
        DiameterAdmissionRequest request)
    {
        if (request.Candidate != null &&
            request.Candidate.PeerIdentity != null)
        {
            return request.Candidate.PeerIdentity;
        }

        if (request.Candidate != null)
        {
            return request.Candidate.Configuration.PeerIdentity;
        }

        return null;
    }

    internal bool SelectDiameterLossAbatement(uint reductionPercentage)
    {
        if (reductionPercentage == 0U)
        {
            return false;
        }

        if (reductionPercentage >= 100U)
        {
            return true;
        }

        int selection = RandomSource.ReadOverloadRandomPercentage();
        if (selection < 1 || selection > 100)
        {
            throw new DiameterValidationException(
                "The overload random source returned an invalid percentage.");
        }

        return selection <= (int)reductionPercentage;
    }

    internal bool SupportsDiameterOverloadAlgorithm(
        DiameterOverloadAlgorithm algorithm)
    {
        int index = 0;
        while (index < Configuration.SupportedAlgorithms.Count)
        {
            if (Configuration.SupportedAlgorithms[index] == algorithm)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal int ReadDiameterOverloadRetryMilliseconds(
        List<DiameterOverloadReport> reports,
        DateTimeOffset evaluatedAt)
    {
        double minimum = int.MaxValue;
        int index = 0;
        while (index < reports.Count)
        {
            double remaining =
                (reports[index].ExpiresAt - evaluatedAt).TotalMilliseconds;
            if (remaining > 0.0 && remaining < minimum)
            {
                minimum = remaining;
            }

            index = index + 1;
        }

        if (minimum == int.MaxValue)
        {
            return 0;
        }

        if (minimum > int.MaxValue)
        {
            return int.MaxValue;
        }

        int result = (int)minimum;
        if ((double)result < minimum)
        {
            result = result + 1;
        }

        return result;
    }

    internal static int ReadDiameterOverloadReportRemainingMilliseconds(
        DiameterOverloadReport report,
        DateTimeOffset evaluatedAt)
    {
        double remaining =
            (report.ExpiresAt - evaluatedAt).TotalMilliseconds;
        if (remaining <= 0.0)
        {
            return 0;
        }

        if (remaining > int.MaxValue)
        {
            return int.MaxValue;
        }

        int result = (int)remaining;
        if ((double)result < remaining)
        {
            result = result + 1;
        }

        return result;
    }

    internal static bool IsNewerDiameterOverloadSequence(
        ulong received,
        ulong existing)
    {
        if (received > existing)
        {
            return true;
        }

        ulong onePercent = ulong.MaxValue / 100UL;
        ulong highBoundary = ulong.MaxValue - onePercent;
        return existing >= highBoundary && received <= onePercent;
    }

    internal static long AddDiameterOverloadMonotonicDuration(
        long monotonicNow,
        uint validitySeconds)
    {
        long duration = (long)validitySeconds * 1000L;
        if (duration > 0L && monotonicNow > long.MaxValue - duration)
        {
            return long.MaxValue;
        }

        return monotonicNow + duration;
    }

    internal static string CreateDiameterOverloadReportKey(
        DiameterOverloadReport report)
    {
        string application = report.ApplicationId.ToString(
            CultureInfo.InvariantCulture);
        if (report.ReportType == DiameterOverloadReportType.Host &&
            report.Host != null)
        {
            return "host|" + application + "|" +
                ValueCodecs.NormalizeDiameterIdentity(report.Host).Text;
        }

        if (report.ReportType == DiameterOverloadReportType.Realm &&
            report.Realm != null)
        {
            return "realm|" + application + "|" +
                ValueCodecs.NormalizeDiameterRealm(report.Realm).Text;
        }

        if (report.ReportType == DiameterOverloadReportType.Peer &&
            report.SourceId != null)
        {
            return "peer|" + application + "|" +
                ValueCodecs.NormalizeDiameterIdentity(report.SourceId).Text;
        }

        throw new ArgumentException(
            "An overload report requires host, realm, or peer scope.");
    }

    internal static void NormalizeDiameterOverloadReportScope(
        DiameterOverloadReport report)
    {
        if (report.Host != null)
        {
            report.Host.Value =
                ValueCodecs.NormalizeDiameterIdentity(report.Host).Text;
        }

        if (report.Realm != null)
        {
            report.Realm.Value =
                ValueCodecs.NormalizeDiameterRealm(report.Realm).Text;
        }

        if (report.SourceId != null)
        {
            report.SourceId.Value =
                ValueCodecs.NormalizeDiameterIdentity(report.SourceId).Text;
        }
    }

    internal static DiameterOverloadConfiguration CloneDiameterOverloadConfiguration(
        DiameterOverloadConfiguration source)
    {
        DiameterOverloadConfiguration result =
            new DiameterOverloadConfiguration();
        result.Enabled = source.Enabled;
        result.SupportedAlgorithms.Clear();
        int index = 0;
        while (index < source.SupportedAlgorithms.Count)
        {
            result.SupportedAlgorithms.Add(source.SupportedAlgorithms[index]);
            index = index + 1;
        }

        result.ReportingAlgorithm = source.ReportingAlgorithm;
        result.EnablePeerReports = source.EnablePeerReports;
        result.EnableLoadReports = source.EnableLoadReports;
        result.DefaultReportValiditySeconds =
            source.DefaultReportValiditySeconds;
        result.MaximumReportValiditySeconds =
            source.MaximumReportValiditySeconds;
        result.MaximumOverloadReports = source.MaximumOverloadReports;
        result.MaximumLoadReports = source.MaximumLoadReports;
        result.ReportSweepMilliseconds = source.ReportSweepMilliseconds;
        result.LoadReportIntervalMilliseconds =
            source.LoadReportIntervalMilliseconds;
        result.RateToleranceMilliseconds =
            source.RateToleranceMilliseconds;
        return result;
    }

    internal static DiameterOverloadReport CloneDiameterOverloadReport(
        DiameterOverloadReport source)
    {
        DiameterOverloadReport result = new DiameterOverloadReport();
        result.ReportType = source.ReportType;
        result.Algorithm = source.Algorithm;
        result.SourceId = RouteTableCloner.CloneRouteIdentity(source.SourceId);
        result.Host = RouteTableCloner.CloneRouteIdentity(source.Host);
        result.Realm = RouteTableCloner.CloneRouteIdentity(source.Realm);
        result.ApplicationId = source.ApplicationId;
        result.SequenceNumber = source.SequenceNumber;
        result.ValidityDurationSeconds = source.ValidityDurationSeconds;
        result.ReductionPercentage = source.ReductionPercentage;
        result.ReductionPercentagePresent =
            source.ReductionPercentagePresent;
        result.MaximumRate = source.MaximumRate;
        result.MaximumRatePresent = source.MaximumRatePresent;
        int index = 0;
        while (index < source.ExtensionAvps.Count)
        {
            DiameterAvp sourceAvp = source.ExtensionAvps[index];
            DiameterAvp avp = new DiameterAvp();
            avp.Code = sourceAvp.Code;
            avp.Flags = sourceAvp.Flags;
            avp.VendorId = sourceAvp.VendorId;
            avp.Data = new byte[sourceAvp.Data.Length];
            Array.Copy(sourceAvp.Data, avp.Data, sourceAvp.Data.Length);
            result.ExtensionAvps.Add(avp);
            index = index + 1;
        }

        result.CreatedAt = source.CreatedAt;
        result.ExpiresAt = source.ExpiresAt;
        result.Cached = source.Cached;
        return result;
    }

    internal static DiameterAdmissionRequest CloneDiameterAdmissionRequest(
        DiameterAdmissionRequest source)
    {
        DiameterAdmissionRequest result =
            new DiameterAdmissionRequest();
        result.Request = RouteTableCloner.CloneRouteRequest(source.Request);
        if (source.Candidate != null)
        {
            result.Candidate =
                RouteTableCloner.CloneRouteCandidate(source.Candidate);
        }

        result.Scope = source.Scope;
        result.AllowDiversion = source.AllowDiversion;
        result.AllowThrottling = source.AllowThrottling;
        result.Probe = source.Probe;
        result.EvaluatedAt = source.EvaluatedAt;
        result.CorrelationText = source.CorrelationText;
        return result;
    }
}
