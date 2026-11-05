using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class CreditControlQuotaTimingTests
{
    [Test]
    public void TariffAndValidityMetadataRoundTripsInGroupedUnits()
    {
        DateTimeOffset now = new DateTimeOffset(
            2026, 8, 29, 21, 0, 0, TimeSpan.Zero);
        CreditControlCommandParser parser =
            new CreditControlCommandParser(
                new TestDiameterClock(now, 0));
        DiameterServiceUnit granted = CreateTimeUnit(60U);
        granted.TariffChangeTime = now.AddMinutes(5);
        granted.TariffChangeTimePresent = true;
        DiameterAvp grantedAvp = CreditControlCommandCodec
            .CreateCreditControlServiceUnitAvp(
                DiameterAvpCodes.GrantedServiceUnit,
                granted);
        DiameterServiceUnit parsedGranted = parser
            .ReadCreditControlServiceUnitAvp(grantedAvp);
        DiameterServiceUnit used = CreateTimeUnit(10U);
        used.TariffChangeUsage =
            DiameterTariffChangeUsage.UnitAfterTariffChange;
        used.TariffChangeUsagePresent = true;
        DiameterServiceUnit parsedUsed = parser
            .ReadCreditControlServiceUnitAvp(
                CreditControlCommandCodec
                    .CreateCreditControlServiceUnitAvp(
                        DiameterAvpCodes.UsedServiceUnit,
                        used));
        DiameterMultipleServiceCredit credit = CreateCredit();
        credit.GrantedUnit = granted;
        credit.TariffChangeUsage =
            DiameterTariffChangeUsage.UnitBeforeTariffChange;
        credit.TariffChangeUsagePresent = true;
        credit.ValidityTimeMilliseconds = 15000;
        credit.ValidityTimePresent = true;
        DiameterMultipleServiceCredit parsedCredit = parser
            .ReadCreditControlMultipleServiceAvp(
                CreditControlCommandCodec
                    .CreateCreditControlMultipleServiceAvp(credit));
        DiameterMultipleServiceCredit fractionalValidity = CreateCredit();
        fractionalValidity.ValidityTimeMilliseconds = 15001;
        fractionalValidity.ValidityTimePresent = true;

        Assert.Multiple(() =>
        {
            Assert.That(parsedGranted.TariffChangeTimePresent, Is.True);
            Assert.That(parsedGranted.TariffChangeTime,
                Is.EqualTo(now.AddMinutes(5)));
            Assert.That(parsedUsed.TariffChangeUsagePresent, Is.True);
            Assert.That(parsedUsed.TariffChangeUsage,
                Is.EqualTo(
                    DiameterTariffChangeUsage.UnitAfterTariffChange));
            Assert.That(parsedCredit.ValidityTimeMilliseconds,
                Is.EqualTo(15000));
            Assert.That(parsedCredit.TariffChangeUsage,
                Is.EqualTo(
                    DiameterTariffChangeUsage.UnitBeforeTariffChange));
            Assert.That(parsedCredit.GrantedUnit!.TariffChangeTime,
                Is.EqualTo(now.AddMinutes(5)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreditControlCommandCodec
                    .CreateCreditControlMultipleServiceAvp(
                        fractionalValidity));
        });
    }

    [Test]
    public void QuotaPolicyDetectsThresholdAndExhaustionPerGroup()
    {
        DiameterCreditControlSession session = CreateSession();
        session.GrantedServiceUnit = CreateTimeUnit(100U);
        session.UsedServiceUnit = CreateTimeUnit(89U);
        session.QuotaThreshold = CreateTimeUnit(10U);
        DiameterMultipleServiceCredit independent = CreateCredit();
        independent.GrantedUnit = CreateOctetUnit(1000UL);
        independent.QuotaThreshold = CreateOctetUnit(100UL);
        independent.UsedUnits.Add(CreateOctetUnit(900UL));
        session.MultipleServiceCredits.Add(independent);

        CreditControlQuotaState.ApplyCreditControlUsagePolicy(session);
        bool threshold = session.QuotaThresholdReached;
        bool exhausted = session.QuotaExhausted;
        independent.UsedUnits[0].TotalOctets = 1000UL;
        CreditControlQuotaState.ApplyCreditControlUsagePolicy(session);

        Assert.Multiple(() =>
        {
            Assert.That(threshold, Is.True);
            Assert.That(exhausted, Is.False);
            Assert.That(session.QuotaThresholdReached, Is.True);
            Assert.That(session.QuotaExhausted, Is.True);
        });
    }

    [Test]
    public void ClientSchedulesEarliestIntermediateInterrogationOnce()
    {
        DateTimeOffset now = new DateTimeOffset(
            2026, 8, 29, 22, 0, 0, TimeSpan.Zero);
        TestDiameterClock clock = new TestDiameterClock(now, 0);
        TestCreditControlStore store = new TestCreditControlStore();
        CreditControlClientStateMachine machine =
            new CreditControlClientStateMachine(store, clock);
        DiameterCreditControlRequest request = CreateRequest(
            DiameterCreditControlRequestType.Initial);
        request.MultipleServicesIndicator =
            DiameterMultipleServicesIndicator.Supported;
        request.MultipleServicesIndicatorPresent = true;
        request.MultipleServiceCredits.Add(CreateCredit());
        machine.BeginCreditControlClientSession(request, 701);
        DiameterCreditControlAnswer answer = CreateAnswer(
            DiameterCreditControlRequestType.Initial,
            0U);
        answer.ValidityTimeMilliseconds = 10000;
        answer.ValidityTimePresent = true;
        answer.GrantedServiceUnit = CreateTimeUnit(100U);
        answer.GrantedServiceUnit.TariffChangeTime = now.AddSeconds(7);
        answer.GrantedServiceUnit.TariffChangeTimePresent = true;
        DiameterMultipleServiceCredit group = CreateCredit();
        group.GrantedUnit = CreateTimeUnit(50U);
        group.ValidityTimeMilliseconds = 5000;
        group.ValidityTimePresent = true;
        answer.MultipleServiceCredits.Add(group);

        DiameterCreditControlResult opened =
            machine.ProcessCreditControlClientAnswer(701, answer);
        clock.UtcTime = now.AddSeconds(5);
        List<DiameterCreditControlResult> due =
            machine.EvaluateCreditControlClientInterrogations();
        List<DiameterCreditControlResult> repeated =
            machine.EvaluateCreditControlClientInterrogations();

        Assert.Multiple(() =>
        {
            Assert.That(opened.Session!.ValidityDeadline,
                Is.EqualTo(now.AddSeconds(10)));
            Assert.That(opened.Session.TariffChangeTime,
                Is.EqualTo(now.AddSeconds(7)));
            Assert.That(opened.Session.NextInterrogationTime,
                Is.EqualTo(now.AddSeconds(5)));
            Assert.That(due, Has.Count.EqualTo(1));
            Assert.That(due[0].Code,
                Is.EqualTo("credit-control-interrogation-due"));
            Assert.That(due[0].ContinueService, Is.True);
            Assert.That(repeated, Is.Empty);
        });
    }

    [Test]
    public void OptedInTerminationRequiresFinalUsageReport()
    {
        DateTimeOffset now = new DateTimeOffset(
            2026, 8, 29, 23, 0, 0, TimeSpan.Zero);
        TestDiameterClock clock = new TestDiameterClock(now, 0);
        TestCreditControlStore store = new TestCreditControlStore();
        CreditControlClientStateMachine machine =
            new CreditControlClientStateMachine(store, clock);
        DiameterCreditControlRequest initial = CreateRequest(
            DiameterCreditControlRequestType.Initial);
        initial.RequireFinalUsageReport = true;
        machine.BeginCreditControlClientSession(initial, 801);
        DiameterCreditControlAnswer answer = CreateAnswer(
            DiameterCreditControlRequestType.Initial,
            0U);
        answer.GrantedServiceUnit = CreateTimeUnit(100U);
        machine.ProcessCreditControlClientAnswer(801, answer);
        DiameterCreditControlRequest termination = CreateRequest(
            DiameterCreditControlRequestType.Termination);

        Assert.Throws<DiameterCreditControlException>(() =>
            machine.TerminateCreditControlClientSession(
                termination,
                802));
        termination.UsedServiceUnits.Add(CreateTimeUnit(75U));
        DiameterCreditControlResult pending =
            machine.TerminateCreditControlClientSession(
                termination,
                802);

        Assert.Multiple(() =>
        {
            Assert.That(pending.State,
                Is.EqualTo(
                    DiameterCreditControlState.PendingTermination));
            Assert.That(pending.Session!.UsedServiceUnit!.Time,
                Is.EqualTo(75U));
            Assert.That(pending.Session.FinalUsageReportRequired,
                Is.False);
        });
    }

    [Test]
    public void FinalUsageReportCoversEveryGrantedServiceGroup()
    {
        DiameterCreditControlSession session = CreateSession();
        session.FinalUsageReportRequired = true;
        DiameterMultipleServiceCredit granted = CreateCredit();
        granted.GrantedUnit = CreateTimeUnit(100U);
        session.MultipleServiceCredits.Add(granted);
        DiameterCreditControlRequest termination = CreateRequest(
            DiameterCreditControlRequestType.Termination);
        DiameterMultipleServiceCredit reported = CreateCredit();
        termination.MultipleServiceCredits.Add(reported);

        Assert.Throws<DiameterCreditControlException>(() =>
            CreditControlQuotaState.ValidateCreditControlFinalUsageReport(
                session,
                termination));
        reported.UsedUnits.Add(CreateTimeUnit(75U));

        Assert.DoesNotThrow(() =>
            CreditControlQuotaState.ValidateCreditControlFinalUsageReport(
                session,
                termination));
    }

    private static DiameterCreditControlRequest CreateRequest(
        DiameterCreditControlRequestType requestType)
    {
        DiameterCreditControlRequest result =
            new DiameterCreditControlRequest();
        result.SessionId = "client.example;quota;1";
        result.DestinationRealm.Value = "remote.example";
        result.ServiceContextId = "example.quota";
        result.RequestType = requestType;
        if (requestType == DiameterCreditControlRequestType.Termination)
        {
            result.TerminationCause = DiameterTerminationCause.Logout;
            result.TerminationCausePresent = true;
        }

        return result;
    }

    private static DiameterCreditControlAnswer CreateAnswer(
        DiameterCreditControlRequestType requestType,
        uint requestNumber)
    {
        DiameterCreditControlAnswer result =
            new DiameterCreditControlAnswer();
        result.SessionId = "client.example;quota;1";
        result.RequestType = requestType;
        result.RequestNumber = requestNumber;
        result.ResultCode = DiameterResultCodes.Success;
        return result;
    }

    private static DiameterCreditControlSession CreateSession()
    {
        DiameterCreditControlSession result =
            new DiameterCreditControlSession();
        result.SessionId = "client.example;quota;policy";
        result.Role = DiameterSessionRole.Client;
        result.State = DiameterCreditControlState.Open;
        result.ServiceContextId = "example.quota";
        result.DestinationRealm = new DiameterIdentity();
        result.DestinationRealm.Value = "remote.example";
        result.CreatedAt = new DateTimeOffset(
            2026, 8, 29, 21, 0, 0, TimeSpan.Zero);
        result.UpdatedAt = result.CreatedAt;
        return result;
    }

    private static DiameterMultipleServiceCredit CreateCredit()
    {
        DiameterMultipleServiceCredit result =
            new DiameterMultipleServiceCredit();
        result.ServiceIdentifiers.Add(10U);
        result.RatingGroup = 100U;
        result.RatingGroupPresent = true;
        return result;
    }

    private static DiameterServiceUnit CreateTimeUnit(uint time)
    {
        DiameterServiceUnit result = new DiameterServiceUnit();
        result.Time = time;
        result.TimePresent = true;
        return result;
    }

    private static DiameterServiceUnit CreateOctetUnit(ulong octets)
    {
        DiameterServiceUnit result = new DiameterServiceUnit();
        result.TotalOctets = octets;
        result.TotalOctetsPresent = true;
        return result;
    }
}
