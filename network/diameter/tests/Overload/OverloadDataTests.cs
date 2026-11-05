using System;

namespace diameter.Tests;

public class OverloadDataTests
{
    [Test]
    public void DefaultsAreBoundedConservativeAndIndependentlyOwned()
    {
        DiameterNodeConfiguration firstNode =
            new DiameterNodeConfiguration();
        DiameterNodeConfiguration secondNode =
            new DiameterNodeConfiguration();
        DiameterOverloadReport firstReport =
            new DiameterOverloadReport();
        DiameterOverloadReport secondReport =
            new DiameterOverloadReport();
        DiameterLoadReport firstLoad = new DiameterLoadReport();
        DiameterLoadReport secondLoad = new DiameterLoadReport();
        DiameterAdmissionDecision firstDecision =
            new DiameterAdmissionDecision();
        DiameterAdmissionDecision secondDecision =
            new DiameterAdmissionDecision();
        firstNode.Overload.SupportedAlgorithms.Add(
            DiameterOverloadAlgorithm.Rate);
        firstReport.ExtensionAvps.Add(new DiameterAvp());
        firstLoad.SourceId.Value = "load.example";
        firstDecision.Reports.Add(firstReport);

        Assert.Multiple(() =>
        {
            Assert.That(firstNode.Overload.Enabled, Is.False);
            Assert.That(firstNode.Overload.ReportingAlgorithm,
                Is.EqualTo(DiameterOverloadAlgorithm.Loss));
            Assert.That(firstNode.Overload.SupportedAlgorithms,
                Is.EqualTo(new DiameterOverloadAlgorithm[]
                {
                    DiameterOverloadAlgorithm.Loss,
                    DiameterOverloadAlgorithm.Rate
                }));
            Assert.That(secondNode.Overload.SupportedAlgorithms,
                Is.EqualTo(new DiameterOverloadAlgorithm[]
                {
                    DiameterOverloadAlgorithm.Loss
                }));
            Assert.That(firstNode.Overload,
                Is.Not.SameAs(secondNode.Overload));
            Assert.That(firstNode.Overload.SupportedAlgorithms,
                Is.Not.SameAs(secondNode.Overload.SupportedAlgorithms));
            Assert.That(firstNode.Overload.DefaultReportValiditySeconds,
                Is.EqualTo(30U));
            Assert.That(firstNode.Overload.MaximumReportValiditySeconds,
                Is.EqualTo(3600U));
            Assert.That(firstNode.Overload.MaximumOverloadReports,
                Is.EqualTo(4096));
            Assert.That(firstNode.Overload.MaximumLoadReports,
                Is.EqualTo(4096));
            Assert.That(firstNode.Overload.ReportSweepMilliseconds,
                Is.EqualTo(1000));
            Assert.That(firstNode.Overload.LoadReportIntervalMilliseconds,
                Is.EqualTo(30000));
            Assert.That(firstNode.Overload.RateToleranceMilliseconds,
                Is.EqualTo(1000));
            Assert.That(firstReport.ExtensionAvps,
                Is.Not.SameAs(secondReport.ExtensionAvps));
            Assert.That(secondReport.ExtensionAvps, Is.Empty);
            Assert.That(firstLoad.SourceId,
                Is.Not.SameAs(secondLoad.SourceId));
            Assert.That(secondLoad.SourceId.Value, Is.Empty);
            Assert.That(firstDecision.Request,
                Is.Not.SameAs(secondDecision.Request));
            Assert.That(firstDecision.Reports,
                Is.Not.SameAs(secondDecision.Reports));
            Assert.That(secondDecision.Reports, Is.Empty);
            Assert.That(firstDecision.Action,
                Is.EqualTo(DiameterAdmissionAction.Admit));
            Assert.That(firstDecision.Admitted, Is.True);
        });
    }

    [Test]
    public void CompleteConfigurationAndEveryReportShapeValidate()
    {
        DiameterOverloadConfiguration configuration =
            new DiameterOverloadConfiguration();
        configuration.Enabled = true;
        configuration.EnablePeerReports = true;
        configuration.EnableLoadReports = true;
        configuration.SupportedAlgorithms.Add(
            DiameterOverloadAlgorithm.Rate);
        configuration.ReportingAlgorithm =
            DiameterOverloadAlgorithm.Rate;
        DiameterOverloadReport host = CreateHostLossReport();
        DiameterOverloadReport realm = CreateRealmLossReport();
        DiameterOverloadReport peer = CreatePeerRateReport();
        peer.ValidityDurationSeconds = 0U;
        DiameterOverloadReport cached = CreateHostLossReport();
        cached.Cached = true;
        cached.CreatedAt = CreateTime();
        cached.ExpiresAt = CreateTime().AddSeconds(30);
        DiameterLoadReport empty = CreateLoadReport(
            DiameterLoadSource.Host,
            0UL);
        DiameterLoadReport available = CreateLoadReport(
            DiameterLoadSource.Peer,
            65535UL);
        available.Cached = true;
        available.CreatedAt = CreateTime();
        available.ExpiresAt = CreateTime().AddSeconds(30);

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(
                configuration.ValidateOverloadConfiguration);
            Assert.DoesNotThrow(host.ValidateOverloadReport);
            Assert.DoesNotThrow(realm.ValidateOverloadReport);
            Assert.DoesNotThrow(peer.ValidateOverloadReport);
            Assert.DoesNotThrow(cached.ValidateOverloadReport);
            Assert.DoesNotThrow(empty.ValidateLoadReport);
            Assert.DoesNotThrow(available.ValidateLoadReport);
        });
    }

    [Test]
    public void ConfigurationValidationRejectsInvalidAlgorithmsAndBounds()
    {
        DiameterOverloadConfiguration missingAlgorithms =
            new DiameterOverloadConfiguration();
        missingAlgorithms.SupportedAlgorithms = null!;
        DiameterOverloadConfiguration emptyAlgorithms =
            new DiameterOverloadConfiguration();
        emptyAlgorithms.SupportedAlgorithms.Clear();
        DiameterOverloadConfiguration missingLoss =
            new DiameterOverloadConfiguration();
        missingLoss.SupportedAlgorithms.Clear();
        missingLoss.SupportedAlgorithms.Add(
            DiameterOverloadAlgorithm.Rate);
        missingLoss.ReportingAlgorithm = DiameterOverloadAlgorithm.Rate;
        DiameterOverloadConfiguration duplicate =
            new DiameterOverloadConfiguration();
        duplicate.SupportedAlgorithms.Add(
            DiameterOverloadAlgorithm.Loss);
        DiameterOverloadConfiguration invalidAlgorithm =
            new DiameterOverloadConfiguration();
        invalidAlgorithm.SupportedAlgorithms[0] =
            (DiameterOverloadAlgorithm)2;
        DiameterOverloadConfiguration unsupportedReporting =
            new DiameterOverloadConfiguration();
        unsupportedReporting.ReportingAlgorithm =
            DiameterOverloadAlgorithm.Rate;
        DiameterOverloadConfiguration zeroValidity =
            new DiameterOverloadConfiguration();
        zeroValidity.DefaultReportValiditySeconds = 0U;
        DiameterOverloadConfiguration invertedValidity =
            new DiameterOverloadConfiguration();
        invertedValidity.DefaultReportValiditySeconds = 31U;
        invertedValidity.MaximumReportValiditySeconds = 30U;
        DiameterOverloadConfiguration zeroCapacity =
            new DiameterOverloadConfiguration();
        zeroCapacity.MaximumOverloadReports = 0;
        DiameterOverloadConfiguration invalidTimer =
            new DiameterOverloadConfiguration();
        invalidTimer.LoadReportIntervalMilliseconds = 0;
        DiameterOverloadConfiguration invalidTolerance =
            new DiameterOverloadConfiguration();
        invalidTolerance.RateToleranceMilliseconds = -1;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(
                missingAlgorithms.ValidateOverloadConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(
                emptyAlgorithms.ValidateOverloadConfiguration);
            Assert.Throws<ArgumentException>(
                missingLoss.ValidateOverloadConfiguration);
            Assert.Throws<ArgumentException>(
                duplicate.ValidateOverloadConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidAlgorithm.ValidateOverloadConfiguration);
            Assert.Throws<ArgumentException>(
                unsupportedReporting.ValidateOverloadConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(
                zeroValidity.ValidateOverloadConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(
                invertedValidity.ValidateOverloadConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(
                zeroCapacity.ValidateOverloadConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidTimer.ValidateOverloadConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidTolerance.ValidateOverloadConfiguration);
        });
    }

    [Test]
    public void OverloadReportValidationRejectsInvalidTargetsAndAbatementData()
    {
        DiameterOverloadReport invalidType = CreateHostLossReport();
        invalidType.ReportType = (DiameterOverloadReportType)3;
        DiameterOverloadReport noAlgorithm = CreateHostLossReport();
        noAlgorithm.Algorithm = DiameterOverloadAlgorithm.None;
        DiameterOverloadReport missingHost = CreateHostLossReport();
        missingHost.Host = null;
        DiameterOverloadReport mixedRealm = CreateRealmLossReport();
        mixedRealm.Host = CreateIdentity("host.example");
        DiameterOverloadReport missingPeerSource = CreatePeerRateReport();
        missingPeerSource.SourceId = null;
        DiameterOverloadReport invalidSource = CreateHostLossReport();
        invalidSource.SourceId = CreateIdentity("invalid identity");
        DiameterOverloadReport missingReduction = CreateHostLossReport();
        missingReduction.ReductionPercentagePresent = false;
        DiameterOverloadReport excessiveReduction = CreateHostLossReport();
        excessiveReduction.ReductionPercentage = 101U;
        DiameterOverloadReport mixedAlgorithms = CreateHostLossReport();
        mixedAlgorithms.MaximumRatePresent = true;
        DiameterOverloadReport missingRate = CreatePeerRateReport();
        missingRate.MaximumRatePresent = false;
        DiameterOverloadReport invalidLifetime = CreateHostLossReport();
        invalidLifetime.Cached = true;
        invalidLifetime.CreatedAt = CreateTime();
        invalidLifetime.ExpiresAt = CreateTime().AddTicks(-1L);
        DiameterOverloadReport missingExtensions = CreateHostLossReport();
        missingExtensions.ExtensionAvps = null!;
        DiameterOverloadReport nullExtension = CreateHostLossReport();
        nullExtension.ExtensionAvps.Add(null!);
        DiameterOverloadReport nullPayload = CreateHostLossReport();
        DiameterAvp invalidAvp = new DiameterAvp();
        invalidAvp.Data = null!;
        nullPayload.ExtensionAvps.Add(invalidAvp);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidType.ValidateOverloadReport);
            Assert.Throws<ArgumentOutOfRangeException>(
                noAlgorithm.ValidateOverloadReport);
            Assert.Throws<ArgumentException>(
                missingHost.ValidateOverloadReport);
            Assert.Throws<ArgumentException>(
                mixedRealm.ValidateOverloadReport);
            Assert.Throws<ArgumentException>(
                missingPeerSource.ValidateOverloadReport);
            Assert.Throws<ArgumentException>(
                invalidSource.ValidateOverloadReport);
            Assert.Throws<ArgumentException>(
                missingReduction.ValidateOverloadReport);
            Assert.Throws<ArgumentOutOfRangeException>(
                excessiveReduction.ValidateOverloadReport);
            Assert.Throws<ArgumentException>(
                mixedAlgorithms.ValidateOverloadReport);
            Assert.Throws<ArgumentException>(
                missingRate.ValidateOverloadReport);
            Assert.Throws<ArgumentException>(
                invalidLifetime.ValidateOverloadReport);
            Assert.Throws<ArgumentException>(
                missingExtensions.ValidateOverloadReport);
            Assert.Throws<ArgumentException>(
                nullExtension.ValidateOverloadReport);
            Assert.Throws<ArgumentException>(
                nullPayload.ValidateOverloadReport);
        });
    }

    [Test]
    public void LoadReportValidationEnforcesWireRangeSourceAndOwnership()
    {
        DiameterLoadReport invalidSource = CreateLoadReport(
            DiameterLoadSource.Host,
            1UL);
        invalidSource.Source = (DiameterLoadSource)2;
        DiameterLoadReport invalidIdentity = CreateLoadReport(
            DiameterLoadSource.Host,
            1UL);
        invalidIdentity.SourceId.Value = "invalid identity";
        DiameterLoadReport tooLarge = CreateLoadReport(
            DiameterLoadSource.Host,
            65536UL);
        DiameterLoadReport invalidLifetime = CreateLoadReport(
            DiameterLoadSource.Host,
            1UL);
        invalidLifetime.Cached = true;
        invalidLifetime.CreatedAt = CreateTime();
        invalidLifetime.ExpiresAt = default(DateTimeOffset);
        DiameterLoadReport missingExtensions = CreateLoadReport(
            DiameterLoadSource.Host,
            1UL);
        missingExtensions.ExtensionAvps = null!;
        DiameterLoadReport nullExtension = CreateLoadReport(
            DiameterLoadSource.Host,
            1UL);
        nullExtension.ExtensionAvps.Add(null!);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidSource.ValidateLoadReport);
            Assert.Throws<ArgumentException>(
                invalidIdentity.ValidateLoadReport);
            Assert.Throws<ArgumentOutOfRangeException>(
                tooLarge.ValidateLoadReport);
            Assert.Throws<ArgumentException>(
                invalidLifetime.ValidateLoadReport);
            Assert.Throws<ArgumentException>(
                missingExtensions.ValidateLoadReport);
            Assert.Throws<ArgumentException>(
                nullExtension.ValidateLoadReport);
        });
    }

    [Test]
    public void AdmissionRequestAndEveryDecisionActionValidate()
    {
        DiameterAdmissionRequest request = CreateAdmissionRequest();
        DiameterAdmissionDecision admit = CreateDecision(
            DiameterAdmissionAction.Admit);
        DiameterAdmissionDecision throttle = CreateDecision(
            DiameterAdmissionAction.Throttle);
        throttle.Admitted = false;
        throttle.OverloadApplied = true;
        throttle.ResultCode = DiameterResultCodes.TooBusy;
        throttle.RetryAfterMilliseconds = 1000;
        throttle.Reports.Add(CreateHostLossReport());
        DiameterAdmissionDecision divert = CreateDecision(
            DiameterAdmissionAction.Divert);
        divert.Admitted = false;
        divert.OverloadApplied = true;
        divert.DiversionCandidate = new DiameterRouteCandidate();
        divert.Reports.Add(CreatePeerRateReport());
        divert.LoadReport = CreateLoadReport(
            DiameterLoadSource.Peer,
            40000UL);

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(request.ValidateAdmissionRequest);
            Assert.DoesNotThrow(admit.ValidateAdmissionDecision);
            Assert.DoesNotThrow(throttle.ValidateAdmissionDecision);
            Assert.DoesNotThrow(divert.ValidateAdmissionDecision);
        });
    }

    [Test]
    public void AdmissionValidationRejectsInvalidRequestsAndDecisions()
    {
        DiameterAdmissionRequest missingRequest = CreateAdmissionRequest();
        missingRequest.Request = null!;
        DiameterAdmissionRequest invalidScope = CreateAdmissionRequest();
        invalidScope.Scope = (DiameterOverloadScope)4;
        DiameterAdmissionRequest noAbatement = CreateAdmissionRequest();
        noAbatement.AllowDiversion = false;
        noAbatement.AllowThrottling = false;
        DiameterAdmissionRequest invalidCorrelation = CreateAdmissionRequest();
        invalidCorrelation.CorrelationText = "bad\ud800";

        DiameterAdmissionDecision invalidAction = CreateDecision(
            DiameterAdmissionAction.Admit);
        invalidAction.Action = (DiameterAdmissionAction)3;
        DiameterAdmissionDecision negativeRetry = CreateDecision(
            DiameterAdmissionAction.Admit);
        negativeRetry.RetryAfterMilliseconds = -1;
        DiameterAdmissionDecision rejectedAdmit = CreateDecision(
            DiameterAdmissionAction.Admit);
        rejectedAdmit.Admitted = false;
        DiameterAdmissionDecision admittedThrottle = CreateDecision(
            DiameterAdmissionAction.Throttle);
        admittedThrottle.ResultCode = DiameterResultCodes.TooBusy;
        DiameterAdmissionDecision missingThrottleCode = CreateDecision(
            DiameterAdmissionAction.Throttle);
        missingThrottleCode.Admitted = false;
        DiameterAdmissionDecision missingDiversion = CreateDecision(
            DiameterAdmissionAction.Divert);
        missingDiversion.Admitted = false;
        DiameterAdmissionDecision diversionWithCode = CreateDecision(
            DiameterAdmissionAction.Divert);
        diversionWithCode.Admitted = false;
        diversionWithCode.DiversionCandidate = new DiameterRouteCandidate();
        diversionWithCode.ResultCode = DiameterResultCodes.TooBusy;
        DiameterAdmissionDecision nullReports = CreateDecision(
            DiameterAdmissionAction.Admit);
        nullReports.Reports = null!;
        DiameterAdmissionDecision nullReport = CreateDecision(
            DiameterAdmissionAction.Admit);
        nullReport.Reports.Add(null!);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(
                missingRequest.ValidateAdmissionRequest);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidScope.ValidateAdmissionRequest);
            Assert.Throws<ArgumentException>(
                noAbatement.ValidateAdmissionRequest);
            Assert.Throws<ArgumentException>(
                invalidCorrelation.ValidateAdmissionRequest);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidAction.ValidateAdmissionDecision);
            Assert.Throws<ArgumentOutOfRangeException>(
                negativeRetry.ValidateAdmissionDecision);
            Assert.Throws<ArgumentException>(
                rejectedAdmit.ValidateAdmissionDecision);
            Assert.Throws<ArgumentException>(
                admittedThrottle.ValidateAdmissionDecision);
            Assert.Throws<ArgumentException>(
                missingThrottleCode.ValidateAdmissionDecision);
            Assert.Throws<ArgumentException>(
                missingDiversion.ValidateAdmissionDecision);
            Assert.Throws<ArgumentException>(
                diversionWithCode.ValidateAdmissionDecision);
            Assert.Throws<ArgumentException>(
                nullReports.ValidateAdmissionDecision);
            Assert.Throws<ArgumentException>(
                nullReport.ValidateAdmissionDecision);
        });
    }

    [Test]
    public void NodeConfigurationValidatesAndOwnsOverloadSettings()
    {
        DiameterNodeConfiguration valid = CreateNodeConfiguration();
        valid.Overload.Enabled = true;
        DiameterNodeConfiguration invalid = CreateNodeConfiguration();
        invalid.Overload.MaximumLoadReports = 0;
        DiameterNodeConfiguration missing = CreateNodeConfiguration();
        missing.Overload = null!;
        DiameterNodeConfiguration independent = CreateNodeConfiguration();
        valid.Overload.SupportedAlgorithms.Add(
            DiameterOverloadAlgorithm.Rate);

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(valid.ValidateConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalid.ValidateConfiguration);
            Assert.Throws<ArgumentException>(missing.ValidateConfiguration);
            Assert.That(independent.Overload.SupportedAlgorithms,
                Is.EqualTo(new DiameterOverloadAlgorithm[]
                {
                    DiameterOverloadAlgorithm.Loss
                }));
        });
    }

    [Test]
    public void PublicOverloadDataRejectsRemainingBoundaryBranches()
    {
        DiameterOverloadReport missingCreated = CreateHostLossReport();
        missingCreated.Cached = true;
        missingCreated.ExpiresAt = CreateTime().AddSeconds(30);
        DiameterOverloadReport invertedLifetime = CreateHostLossReport();
        invertedLifetime.Cached = true;
        invertedLifetime.CreatedAt = CreateTime();
        invertedLifetime.ExpiresAt = CreateTime().AddSeconds(-1);
        DiameterLoadReport missingSource = CreateLoadReport(
            DiameterLoadSource.Host,
            1UL);
        missingSource.SourceId = null!;
        DiameterAdmissionRequest lowScope = CreateAdmissionRequest();
        lowScope.Scope = (DiameterOverloadScope)(-1);
        DiameterAdmissionDecision lowAction = CreateDecision(
            DiameterAdmissionAction.Admit);
        lowAction.Action = (DiameterAdmissionAction)(-1);
        DiameterAdmissionDecision lowResult = CreateDecision(
            DiameterAdmissionAction.Admit);
        lowResult.ResultCode = 999U;
        DiameterAdmissionDecision highResult = CreateDecision(
            DiameterAdmissionAction.Admit);
        highResult.ResultCode = 6000U;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(
                missingCreated.ValidateOverloadReport);
            Assert.Throws<ArgumentException>(
                invertedLifetime.ValidateOverloadReport);
            Assert.Throws<ArgumentException>(
                missingSource.ValidateLoadReport);
            Assert.Throws<ArgumentOutOfRangeException>(
                lowScope.ValidateAdmissionRequest);
            Assert.Throws<ArgumentOutOfRangeException>(
                lowAction.ValidateAdmissionDecision);
            Assert.Throws<ArgumentOutOfRangeException>(
                lowResult.ValidateAdmissionDecision);
            Assert.Throws<ArgumentOutOfRangeException>(
                highResult.ValidateAdmissionDecision);
        });
    }

    private static DiameterOverloadReport CreateHostLossReport()
    {
        DiameterOverloadReport result = new DiameterOverloadReport();
        result.ReportType = DiameterOverloadReportType.Host;
        result.Algorithm = DiameterOverloadAlgorithm.Loss;
        result.Host = CreateIdentity("server.example");
        result.ApplicationId = DiameterApplicationIds.CreditControl;
        result.SequenceNumber = 7UL;
        result.ValidityDurationSeconds = 30U;
        result.ReductionPercentage = 25U;
        result.ReductionPercentagePresent = true;
        return result;
    }

    private static DiameterOverloadReport CreateRealmLossReport()
    {
        DiameterOverloadReport result = CreateHostLossReport();
        result.ReportType = DiameterOverloadReportType.Realm;
        result.Host = null;
        result.Realm = CreateIdentity("example");
        return result;
    }

    private static DiameterOverloadReport CreatePeerRateReport()
    {
        DiameterOverloadReport result = new DiameterOverloadReport();
        result.ReportType = DiameterOverloadReportType.Peer;
        result.Algorithm = DiameterOverloadAlgorithm.Rate;
        result.SourceId = CreateIdentity("agent.example");
        result.ApplicationId = DiameterApplicationIds.CreditControl;
        result.SequenceNumber = ulong.MaxValue;
        result.ValidityDurationSeconds = 30U;
        result.ReductionPercentagePresent = false;
        result.MaximumRate = 500U;
        result.MaximumRatePresent = true;
        return result;
    }

    private static DiameterLoadReport CreateLoadReport(
        DiameterLoadSource source,
        ulong value)
    {
        DiameterLoadReport result = new DiameterLoadReport();
        result.Source = source;
        result.SourceId = CreateIdentity("load.example");
        result.LoadValue = value;
        return result;
    }

    private static DiameterAdmissionRequest CreateAdmissionRequest()
    {
        DiameterAdmissionRequest result = new DiameterAdmissionRequest();
        result.Request.Message.Version = DiameterProtocolConstants.Version;
        result.Request.Message.Flags = DiameterMessageFlags.Request;
        result.Request.Message.CommandCode =
            DiameterCommandCodes.CreditControl;
        result.Request.Message.ApplicationId =
            DiameterApplicationIds.CreditControl;
        result.Request.ApplicationId =
            DiameterApplicationIds.CreditControl;
        result.Request.DestinationRealm = CreateIdentity("example");
        result.Request.DestinationHost = CreateIdentity("server.example");
        result.Request.ReceivedAt = CreateTime();
        result.Scope = DiameterOverloadScope.Host;
        result.EvaluatedAt = CreateTime();
        result.CorrelationText = "admission-1";
        return result;
    }

    private static DiameterAdmissionDecision CreateDecision(
        DiameterAdmissionAction action)
    {
        DiameterAdmissionDecision result =
            new DiameterAdmissionDecision();
        result.Request = CreateAdmissionRequest();
        result.Action = action;
        result.Code = "overload-admission";
        result.Message = "The request was evaluated for overload.";
        return result;
    }

    private static DiameterNodeConfiguration CreateNodeConfiguration()
    {
        DiameterNodeConfiguration result =
            new DiameterNodeConfiguration();
        result.OriginHost.Value = "node.example";
        result.OriginRealm.Value = "example";
        result.ProductName = "diameter.Tests";
        result.Roles = DiameterNodeRole.Client;
        result.Security.AllowUnsecuredTransport = true;
        return result;
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }

    private static DateTimeOffset CreateTime()
    {
        return new DateTimeOffset(
            2026,
            8,
            30,
            2,
            0,
            0,
            TimeSpan.Zero);
    }
}
