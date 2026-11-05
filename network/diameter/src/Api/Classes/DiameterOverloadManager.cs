using System;
using System.Collections.Generic;

namespace diameter;

/// <summary>Publishes local load state and applies received overload and load reports.</summary>
public class DiameterOverloadManager : IDiameterOverloadManager
{
    internal DiameterOverloadConfiguration Configuration;
    internal IOverloadSource Source;
    internal IDiameterClock Clock;
    internal DiameterIdentity OriginHost = new DiameterIdentity();
    internal DiameterIdentity OriginRealm = new DiameterIdentity();
    internal bool ServerSelection;
    internal DiameterOverloadState OverloadState;
    internal DiameterLoadState LoadState;

    /// <summary>Initializes overload control for one configured Diameter node.</summary>
    /// <param name="configuration">The node identity, roles, and overload settings.</param>
    /// <param name="source">The provider of local overload and load state.</param>
    /// <param name="clock">The UTC and monotonic report clock.</param>
    /// <exception cref="System.ArgumentException">
    /// Thrown when configuration, identities, roles, or dependencies are invalid.
    /// </exception>
    public DiameterOverloadManager(
        DiameterNodeConfiguration? configuration,
        IOverloadSource? source,
        IDiameterClock? clock)
    {
        DiameterValueResult host = configuration == null ?
            new DiameterValueResult() :
            ValueCodecs.NormalizeDiameterIdentity(configuration.OriginHost);
        DiameterValueResult realm = configuration == null ?
            new DiameterValueResult() :
            ValueCodecs.NormalizeDiameterRealm(configuration.OriginRealm);
        int roles = configuration == null ?
            0 : (int)configuration.Roles;
        if (configuration == null || configuration.Overload == null ||
            source == null || clock == null ||
            host.Status != DiameterCodecStatus.Success ||
            realm.Status != DiameterCodecStatus.Success || roles == 0 ||
            (roles & -64) != 0)
        {
            throw new ArgumentException(
                "Valid node overload configuration and dependencies are required.");
        }

        configuration.Overload.ValidateOverloadConfiguration();
        Configuration =
            DiameterOverloadState.CloneDiameterOverloadConfiguration(
                configuration.Overload);
        Source = source;
        Clock = clock;
        OriginHost.Value = host.Text;
        OriginRealm.Value = realm.Text;
        int selectionRoles =
            (int)DiameterNodeRole.Client |
            (int)DiameterNodeRole.Relay |
            (int)DiameterNodeRole.Proxy |
            (int)DiameterNodeRole.Translation;
        ServerSelection = (roles & selectionRoles) != 0;
        OverloadState = new DiameterOverloadState(
            Configuration,
            clock,
            new SystemOverloadRandom());
        LoadState = new DiameterLoadState(Configuration, clock);
    }

    /// <summary>Builds and publishes a local overload report for a request scope.</summary>
    /// <param name="request">The request scope used to obtain local overload state.</param>
    /// <returns>The normalized owned local overload report.</returns>
    public DiameterOverloadReport PublishDiameterOverloadReport(
        DiameterAdmissionRequest? request)
    {
        if (request == null)
        {
            throw new ArgumentException(
                "An overload publication request is required.");
        }

        request.ValidateAdmissionRequest();
        DiameterOverloadReport? supplied =
            Source.ReadLocalDiameterOverloadReport(
                DiameterOverloadState.CloneDiameterAdmissionRequest(
                    request));
        if (supplied == null)
        {
            throw new DiameterValidationException(
                "The overload source returned no local report.");
        }

        supplied.ValidateOverloadReport();
        if (supplied.Algorithm != Configuration.ReportingAlgorithm ||
            supplied.ReportType == DiameterOverloadReportType.Peer &&
            !Configuration.EnablePeerReports)
        {
            throw new DiameterValidationException(
                "The local overload report conflicts with configured reporting behavior.");
        }

        ValidateLocalDiameterOverloadScope(supplied);
        DiameterOverloadReport result =
            DiameterOverloadState.CloneDiameterOverloadReport(supplied);
        DiameterOverloadState.NormalizeDiameterOverloadReportScope(result);
        if (result.ValidityDurationSeconds >
                Configuration.MaximumReportValiditySeconds ||
            result.ValidityDurationSeconds > 86400U)
        {
            result.ValidityDurationSeconds =
                Configuration.DefaultReportValiditySeconds;
        }

        result.CreatedAt = Clock.ReadUtcTime();
        result.ExpiresAt = result.CreatedAt.AddSeconds(
            result.ValidityDurationSeconds);
        result.Cached = false;
        result.ValidateOverloadReport();
        return result;
    }

    /// <summary>Processes one overload report received from a Diameter peer.</summary>
    /// <param name="report">The received overload report.</param>
    /// <param name="sourcePeer">The peer that supplied the report.</param>
    /// <param name="receivedAt">The report reception time.</param>
    /// <returns>The normalized owned report accepted by reacting-node state.</returns>
    public DiameterOverloadReport ProcessDiameterOverloadReport(
        DiameterOverloadReport? report,
        DiameterIdentity? sourcePeer,
        DateTimeOffset receivedAt)
    {
        if (report == null || sourcePeer == null ||
            ValueCodecs.NormalizeDiameterIdentity(sourcePeer).Status !=
                DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "A received overload report and valid source peer are required.");
        }

        DiameterOverloadReportUpdate update = report.ReportType ==
            DiameterOverloadReportType.Peer ?
            OverloadState.ProcessDiameterPeerOverloadReport(
                report,
                sourcePeer,
                receivedAt) :
            OverloadState.ProcessDiameterOverloadReport(
                report,
                receivedAt);
        return DiameterOverloadState.CloneDiameterOverloadReport(
            update.Report);
    }

    /// <summary>Builds and publishes a local host or peer load report.</summary>
    /// <param name="source">The local load source represented by the report.</param>
    /// <returns>The normalized owned local load report.</returns>
    public DiameterLoadReport PublishDiameterLoadReport(
        DiameterLoadSource source)
    {
        int sourceValue = (int)source;
        if (sourceValue < (int)DiameterLoadSource.Host ||
            sourceValue > (int)DiameterLoadSource.Peer)
        {
            throw new ArgumentOutOfRangeException("source");
        }

        DiameterLoadReport? supplied =
            Source.ReadLocalDiameterLoadReport(source);
        if (supplied == null)
        {
            throw new DiameterValidationException(
                "The overload source returned no local load report.");
        }

        supplied.ValidateLoadReport();
        if (supplied.Source != source ||
            !ValueCodecs.AreDiameterIdentitiesEqual(
                supplied.SourceId,
                OriginHost))
        {
            throw new DiameterValidationException(
                "The local load report conflicts with its requested source or node identity.");
        }

        DiameterLoadReport result =
            DiameterLoadState.CloneDiameterLoadReport(supplied);
        result.CreatedAt = Clock.ReadUtcTime();
        result.ExpiresAt = result.CreatedAt.AddMilliseconds(
            Configuration.LoadReportIntervalMilliseconds);
        result.Cached = false;
        result.ValidateLoadReport();
        return result;
    }

    /// <summary>Processes one load report received from a Diameter peer.</summary>
    /// <param name="report">The received host or peer load report.</param>
    /// <param name="sourcePeer">The peer that supplied the report.</param>
    /// <param name="receivedAt">The report reception time.</param>
    /// <returns>The normalized owned report accepted by route-selection state.</returns>
    public DiameterLoadReport ProcessDiameterLoadReport(
        DiameterLoadReport? report,
        DiameterIdentity? sourcePeer,
        DateTimeOffset receivedAt)
    {
        DiameterLoadReportUpdate update = LoadState.ProcessDiameterLoadReport(
            report,
            sourcePeer,
            ServerSelection,
            receivedAt);
        return DiameterLoadState.CloneDiameterLoadReport(update.Report);
    }

    /// <summary>Evaluates loss and rate overload admission for one routed request.</summary>
    /// <param name="request">The request and current route candidate.</param>
    /// <returns>The owned admission decision.</returns>
    public DiameterAdmissionDecision EvaluateDiameterAdmission(
        DiameterAdmissionRequest? request)
    {
        if (request == null)
        {
            throw new ArgumentException(
                "An overload admission request is required.");
        }

        request.ValidateAdmissionRequest();
        if (IsDiameterManagerWatchdog(request.Request.Message))
        {
            DiameterAdmissionDecision watchdog =
                new DiameterAdmissionDecision();
            watchdog.Request =
                DiameterOverloadState.CloneDiameterAdmissionRequest(
                    request);
            watchdog.Code = "overload-watchdog-bypass";
            watchdog.Message =
                "Diameter watchdog traffic bypasses overload admission.";
            return watchdog;
        }

        DiameterAdmissionDecision loss =
            OverloadState.EvaluateDiameterLossAdmission(request);
        if (!loss.Admitted)
        {
            AttachDiameterAdmissionLoadReport(loss);
            return loss;
        }

        DiameterAdmissionDecision rate =
            OverloadState.EvaluateDiameterRateAdmission(request);
        DiameterAdmissionDecision result = rate.Reports.Count > 0 ?
            rate : loss;
        AttachDiameterAdmissionLoadReport(result);
        return result;
    }

    /// <summary>Reads owned active overload report snapshots.</summary>
    /// <returns>The reports in deterministic cache order.</returns>
    public List<DiameterOverloadReport> ReadDiameterOverloadReports()
    {
        return OverloadState.ReadActiveDiameterOverloadReports();
    }

    /// <summary>Reads owned active load report snapshots.</summary>
    /// <returns>The reports in deterministic cache order.</returns>
    public List<DiameterLoadReport> ReadDiameterLoadReports()
    {
        return LoadState.ReadActiveDiameterLoadReports();
    }

    internal void ValidateLocalDiameterOverloadScope(
        DiameterOverloadReport report)
    {
        bool local = report.ReportType == DiameterOverloadReportType.Host &&
            report.Host != null &&
            ValueCodecs.AreDiameterIdentitiesEqual(report.Host, OriginHost) ||
            report.ReportType == DiameterOverloadReportType.Realm &&
            report.Realm != null &&
            ValueCodecs.NormalizeDiameterRealm(report.Realm).Text ==
                OriginRealm.Value ||
            report.ReportType == DiameterOverloadReportType.Peer &&
            report.SourceId != null &&
            ValueCodecs.AreDiameterIdentitiesEqual(
                report.SourceId,
                OriginHost);
        if (!local)
        {
            throw new DiameterValidationException(
                "The overload source returned a report outside the local node scope.");
        }
    }

    internal void AttachDiameterAdmissionLoadReport(
        DiameterAdmissionDecision decision)
    {
        if (decision.Request.Candidate == null)
        {
            return;
        }

        List<DiameterLoadReport> reports =
            LoadState.ReadActiveDiameterLoadReports();
        DiameterRouteCandidate candidate = decision.Request.Candidate;
        DiameterIdentity? host =
            candidate.Configuration.DestinationHost;
        if (host == null)
        {
            host = candidate.PeerIdentity;
        }

        int pass = ServerSelection ? 0 : 1;
        while (pass < 2)
        {
            int index = 0;
            while (index < reports.Count)
            {
                DiameterLoadReport report = reports[index];
                DiameterIdentity? target = pass == 0 ?
                    host : candidate.PeerIdentity;
                DiameterLoadSource source = pass == 0 ?
                    DiameterLoadSource.Host : DiameterLoadSource.Peer;
                if (target != null && report.Source == source &&
                    ValueCodecs.AreDiameterIdentitiesEqual(
                        report.SourceId,
                        target))
                {
                    decision.LoadReport =
                        DiameterLoadState.CloneDiameterLoadReport(report);
                    return;
                }

                index = index + 1;
            }

            pass = pass + 1;
        }
    }

    internal bool IsDiameterManagerWatchdog(DiameterMessage message)
    {
        return message.CommandCode == DiameterCommandCodes.DeviceWatchdog &&
            message.ApplicationId == DiameterApplicationIds.Common;
    }
}
