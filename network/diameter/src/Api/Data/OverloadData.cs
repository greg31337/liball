using System;
using System.Collections.Generic;

namespace diameter;

/// <summary>Configures Diameter overload control and load conveyance.</summary>
public class DiameterOverloadConfiguration
{
    /// <summary>Enables overload feature negotiation, reporting, and reaction.</summary>
    public bool Enabled;

    /// <summary>The overload algorithms supported by this node.</summary>
    public List<DiameterOverloadAlgorithm> SupportedAlgorithms =
        new List<DiameterOverloadAlgorithm>
        {
            DiameterOverloadAlgorithm.Loss
        };

    /// <summary>The algorithm selected for locally generated reports.</summary>
    public DiameterOverloadAlgorithm ReportingAlgorithm =
        DiameterOverloadAlgorithm.Loss;

    /// <summary>Enables RFC 8581 peer overload reports.</summary>
    public bool EnablePeerReports;

    /// <summary>Enables RFC 8583 load reports.</summary>
    public bool EnableLoadReports;

    /// <summary>The default OC-Validity-Duration value in seconds.</summary>
    public uint DefaultReportValiditySeconds = 30U;

    /// <summary>The maximum accepted overload report validity in seconds.</summary>
    public uint MaximumReportValiditySeconds = 3600U;

    /// <summary>The maximum number of retained remote overload reports.</summary>
    public int MaximumOverloadReports = 4096;

    /// <summary>The maximum number of retained remote load reports.</summary>
    public int MaximumLoadReports = 4096;

    /// <summary>The interval for expiring retained reports in milliseconds.</summary>
    public int ReportSweepMilliseconds = 1000;

    /// <summary>The normal interval between local load reports in milliseconds.</summary>
    public int LoadReportIntervalMilliseconds = 30000;

    /// <summary>The RFC 8582 rate-control burst tolerance in milliseconds.</summary>
    public int RateToleranceMilliseconds = 1000;

    internal void ValidateOverloadConfiguration()
    {
        if (SupportedAlgorithms == null)
        {
            throw new ArgumentException(
                "Supported overload algorithm storage is required.");
        }

        if (SupportedAlgorithms.Count == 0 ||
            DefaultReportValiditySeconds == 0U ||
            MaximumReportValiditySeconds < DefaultReportValiditySeconds ||
            MaximumReportValiditySeconds > 86400U ||
            MaximumOverloadReports <= 0 || MaximumLoadReports <= 0 ||
            ReportSweepMilliseconds <= 0 ||
            LoadReportIntervalMilliseconds <= 0 ||
            RateToleranceMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                "Diameter overload configuration value");
        }

        bool lossSupported = false;
        bool reportingSupported = false;
        int index = 0;
        while (index < SupportedAlgorithms.Count)
        {
            DiameterOverloadAlgorithm algorithm =
                SupportedAlgorithms[index];
            if (algorithm != DiameterOverloadAlgorithm.Loss &&
                algorithm != DiameterOverloadAlgorithm.Rate)
            {
                throw new ArgumentOutOfRangeException(
                    "SupportedAlgorithms");
            }

            if (algorithm == DiameterOverloadAlgorithm.Loss)
            {
                lossSupported = true;
            }

            if (algorithm == ReportingAlgorithm)
            {
                reportingSupported = true;
            }

            int comparisonIndex = index + 1;
            while (comparisonIndex < SupportedAlgorithms.Count)
            {
                if (SupportedAlgorithms[comparisonIndex] == algorithm)
                {
                    throw new ArgumentException(
                        "Supported overload algorithms must be distinct.");
                }

                comparisonIndex = comparisonIndex + 1;
            }

            index = index + 1;
        }

        if (!lossSupported || !reportingSupported)
        {
            throw new ArgumentException(
                "Loss support and the reporting algorithm are required.");
        }
    }
}

/// <summary>Contains one RFC 7683, RFC 8581, or RFC 8582 overload report.</summary>
public class DiameterOverloadReport
{
    /// <summary>The host, realm, or peer report type.</summary>
    public DiameterOverloadReportType ReportType;

    /// <summary>The selected loss or rate abatement algorithm.</summary>
    public DiameterOverloadAlgorithm Algorithm =
        DiameterOverloadAlgorithm.Loss;

    /// <summary>The source identity carried by SourceID, or <c>null</c>.</summary>
    public DiameterIdentity? SourceId;

    /// <summary>The overloaded host for a host report, or <c>null</c>.</summary>
    public DiameterIdentity? Host;

    /// <summary>The overloaded realm for a realm report, or <c>null</c>.</summary>
    public DiameterIdentity? Realm;

    /// <summary>The application identifier to which the report applies.</summary>
    public uint ApplicationId;

    /// <summary>The nonvolatile OC-Sequence-Number.</summary>
    public ulong SequenceNumber;

    /// <summary>The OC-Validity-Duration in seconds, including zero to end overload.</summary>
    public uint ValidityDurationSeconds = 30U;

    /// <summary>The requested loss reduction percentage.</summary>
    public uint ReductionPercentage;

    /// <summary>Indicates that OC-Reduction-Percentage is present.</summary>
    public bool ReductionPercentagePresent = true;

    /// <summary>The requested maximum rate in requests per second.</summary>
    public uint MaximumRate;

    /// <summary>Indicates that OC-Maximum-Rate is present.</summary>
    public bool MaximumRatePresent;

    /// <summary>Owned extension AVPs carried by the OC-OLR value.</summary>
    public List<DiameterAvp> ExtensionAvps = new List<DiameterAvp>();

    /// <summary>The time at which this report snapshot was created or received.</summary>
    public DateTimeOffset CreatedAt;

    /// <summary>The absolute expiry calculated for a retained report.</summary>
    public DateTimeOffset ExpiresAt;

    /// <summary>Indicates that the report is retained in reacting-node state.</summary>
    public bool Cached;

    internal void ValidateOverloadReport()
    {
        int reportType = (int)ReportType;
        if (reportType < (int)DiameterOverloadReportType.Host ||
            reportType > (int)DiameterOverloadReportType.Peer ||
            (Algorithm != DiameterOverloadAlgorithm.Loss &&
            Algorithm != DiameterOverloadAlgorithm.Rate) ||
            ReductionPercentagePresent && ReductionPercentage > 100U)
        {
            throw new ArgumentOutOfRangeException(
                "Diameter overload report value");
        }

        if (ReportType == DiameterOverloadReportType.Host &&
            (Host == null || Realm != null ||
            ValueCodecs.NormalizeDiameterIdentity(Host).Status !=
                DiameterCodecStatus.Success) ||
            ReportType == DiameterOverloadReportType.Realm &&
            (Realm == null || Host != null ||
            ValueCodecs.NormalizeDiameterRealm(Realm).Status !=
                DiameterCodecStatus.Success) ||
            ReportType == DiameterOverloadReportType.Peer &&
            (Host != null || Realm != null || SourceId == null ||
            ValueCodecs.NormalizeDiameterIdentity(SourceId).Status !=
                DiameterCodecStatus.Success) ||
            SourceId != null &&
            ValueCodecs.NormalizeDiameterIdentity(SourceId).Status !=
                DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "The overload report target identities are inconsistent.");
        }

        if (Algorithm == DiameterOverloadAlgorithm.Loss &&
            (!ReductionPercentagePresent || MaximumRatePresent) ||
            Algorithm == DiameterOverloadAlgorithm.Rate &&
            !MaximumRatePresent)
        {
            throw new ArgumentException(
                "The overload algorithm requires matching abatement data.");
        }

        if (Cached &&
            (CreatedAt == default(DateTimeOffset) ||
            ExpiresAt == default(DateTimeOffset) || ExpiresAt < CreatedAt))
        {
            throw new ArgumentException(
                "A cached overload report requires a valid lifetime.");
        }

        ValidateOverloadExtensionAvps();
    }

    internal void ValidateOverloadExtensionAvps()
    {
        if (ExtensionAvps == null)
        {
            throw new ArgumentException(
                "Overload report extension storage is required.");
        }

        int index = 0;
        while (index < ExtensionAvps.Count)
        {
            DiameterAvp avp = ExtensionAvps[index];
            if (avp == null || avp.Data == null)
            {
                throw new ArgumentException(
                    "Overload report extensions must own payload data.");
            }

            index = index + 1;
        }
    }
}

/// <summary>Contains one RFC 8583 host or peer load report.</summary>
public class DiameterLoadReport
{
    /// <summary>Identifies whether the report describes a host or peer.</summary>
    public DiameterLoadSource Source;

    /// <summary>The Diameter identity carried by SourceID.</summary>
    public DiameterIdentity SourceId = new DiameterIdentity();

    /// <summary>The relative available capacity from zero through 65535.</summary>
    public ulong LoadValue;

    /// <summary>Owned extension AVPs carried by the Load value.</summary>
    public List<DiameterAvp> ExtensionAvps = new List<DiameterAvp>();

    /// <summary>The time at which this report snapshot was created or received.</summary>
    public DateTimeOffset CreatedAt;

    /// <summary>The absolute expiry calculated for a retained report.</summary>
    public DateTimeOffset ExpiresAt;

    /// <summary>Indicates that the report is retained for route selection.</summary>
    public bool Cached;

    internal void ValidateLoadReport()
    {
        int source = (int)Source;
        if (source < (int)DiameterLoadSource.Host ||
            source > (int)DiameterLoadSource.Peer || LoadValue > 65535UL)
        {
            throw new ArgumentOutOfRangeException(
                "Diameter load report value");
        }

        if (SourceId == null ||
            ValueCodecs.NormalizeDiameterIdentity(SourceId).Status !=
                DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "A load report requires a valid SourceID.");
        }

        if (Cached &&
            (CreatedAt == default(DateTimeOffset) ||
            ExpiresAt == default(DateTimeOffset) || ExpiresAt < CreatedAt))
        {
            throw new ArgumentException(
                "A cached load report requires a valid lifetime.");
        }

        if (ExtensionAvps == null)
        {
            throw new ArgumentException(
                "Load report extension storage is required.");
        }

        int index = 0;
        while (index < ExtensionAvps.Count)
        {
            DiameterAvp avp = ExtensionAvps[index];
            if (avp == null || avp.Data == null)
            {
                throw new ArgumentException(
                    "Load report extensions must own payload data.");
            }

            index = index + 1;
        }
    }
}

/// <summary>Contains one request being evaluated for overload admission.</summary>
public class DiameterAdmissionRequest
{
    /// <summary>The owned request and routing context being evaluated.</summary>
    public DiameterRouteRequest Request = new DiameterRouteRequest();

    /// <summary>The current route candidate, or <c>null</c> before selection.</summary>
    public DiameterRouteCandidate? Candidate;

    /// <summary>The overload scope being evaluated.</summary>
    public DiameterOverloadScope Scope;

    /// <summary>Allows an abated request to use another route.</summary>
    public bool AllowDiversion = true;

    /// <summary>Allows an abated request to be rejected locally.</summary>
    public bool AllowThrottling = true;

    /// <summary>Identifies a conservative probe after overload expiry.</summary>
    public bool Probe;

    /// <summary>The time at which overload admission is evaluated.</summary>
    public DateTimeOffset EvaluatedAt;

    /// <summary>Caller-defined text returned with the decision.</summary>
    public string CorrelationText = string.Empty;

    internal void ValidateAdmissionRequest()
    {
        if (Request == null || CorrelationText == null ||
            ValueCodecs.FindInvalidUtf16Index(CorrelationText) >= 0)
        {
            throw new ArgumentException(
                "Overload admission requires request and correlation data.");
        }

        Request.ValidateRouteRequest();
        int scope = (int)Scope;
        if (scope < (int)DiameterOverloadScope.Host ||
            scope > (int)DiameterOverloadScope.Application)
        {
            throw new ArgumentOutOfRangeException("Scope");
        }

        if (!AllowDiversion && !AllowThrottling)
        {
            throw new ArgumentException(
                "Admission must permit diversion or throttling.");
        }
    }
}

/// <summary>Contains the overload admission outcome for one request.</summary>
public class DiameterAdmissionDecision
{
    /// <summary>The owned admission request represented by this decision.</summary>
    public DiameterAdmissionRequest Request = new DiameterAdmissionRequest();

    /// <summary>The selected admission, throttle, or diversion action.</summary>
    public DiameterAdmissionAction Action;

    /// <summary>The overload reports that matched the request.</summary>
    public List<DiameterOverloadReport> Reports =
        new List<DiameterOverloadReport>();

    /// <summary>The load report considered during selection, or <c>null</c>.</summary>
    public DiameterLoadReport? LoadReport;

    /// <summary>The alternate route selected for diversion, or <c>null</c>.</summary>
    public DiameterRouteCandidate? DiversionCandidate;

    /// <summary>Indicates that normal request processing may continue.</summary>
    public bool Admitted = true;

    /// <summary>Indicates that an overload report affected the decision.</summary>
    public bool OverloadApplied;

    /// <summary>The Result-Code used for local throttling, or zero.</summary>
    public uint ResultCode;

    /// <summary>The nonnegative suggested retry delay in milliseconds.</summary>
    public int RetryAfterMilliseconds;

    /// <summary>The stable machine-readable decision code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable decision description.</summary>
    public string Message = string.Empty;

    internal void ValidateAdmissionDecision()
    {
        if (Request == null || Reports == null || Code == null ||
            Message == null || ValueCodecs.FindInvalidUtf16Index(Code) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(Message) >= 0)
        {
            throw new ArgumentException(
                "An admission decision requires owned outcome data.");
        }

        Request.ValidateAdmissionRequest();
        int action = (int)Action;
        if (action < (int)DiameterAdmissionAction.Admit ||
            action > (int)DiameterAdmissionAction.Divert ||
            RetryAfterMilliseconds < 0 || ResultCode != 0U &&
            (ResultCode < 1000U || ResultCode > 5999U))
        {
            throw new ArgumentOutOfRangeException(
                "Diameter admission decision value");
        }

        if (Action == DiameterAdmissionAction.Admit && !Admitted ||
            Action != DiameterAdmissionAction.Admit && Admitted ||
            Action == DiameterAdmissionAction.Admit &&
            (DiversionCandidate != null || ResultCode != 0U) ||
            Action == DiameterAdmissionAction.Throttle &&
            (DiversionCandidate != null || ResultCode == 0U) ||
            Action == DiameterAdmissionAction.Divert &&
            (DiversionCandidate == null || ResultCode != 0U))
        {
            throw new ArgumentException(
                "The admission action and outcome fields are inconsistent.");
        }

        int index = 0;
        while (index < Reports.Count)
        {
            DiameterOverloadReport report = Reports[index];
            if (report == null)
            {
                throw new ArgumentException(
                    "Admission reports cannot contain null entries.");
            }

            report.ValidateOverloadReport();
            index = index + 1;
        }

        if (LoadReport != null)
        {
            LoadReport.ValidateLoadReport();
        }
    }
}
