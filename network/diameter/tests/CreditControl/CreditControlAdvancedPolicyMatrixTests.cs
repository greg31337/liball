using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class CreditControlAdvancedPolicyMatrixTests
{
    [Test]
    public void MultipleServiceDispositionUsesTheMostRestrictiveGroupResult()
    {
        DiameterCreditControlDisposition[] values =
            new DiameterCreditControlDisposition[]
            {
                DiameterCreditControlDisposition.Granted,
                DiameterCreditControlDisposition.PartiallyGranted,
                DiameterCreditControlDisposition.FinalUnit,
                DiameterCreditControlDisposition.Denied,
                DiameterCreditControlDisposition.Failed
            };

        int first = 0;
        while (first < values.Length)
        {
            int second = 0;
            while (second < values.Length)
            {
                DiameterCreditControlDisposition actual =
                    CreditControlMultipleServiceState
                        .SelectCreditControlMultipleServiceDisposition(
                            values[first],
                            values[second]);
                DiameterCreditControlDisposition expected =
                    SelectExpectedDisposition(
                        values[first],
                        values[second]);
                Assert.That(actual, Is.EqualTo(expected),
                    values[first] + "/" + values[second]);
                second = second + 1;
            }

            first = first + 1;
        }
    }

    [TestCase(DiameterCreditControlUnitType.Time)]
    [TestCase(DiameterCreditControlUnitType.Money)]
    [TestCase(DiameterCreditControlUnitType.TotalOctets)]
    [TestCase(DiameterCreditControlUnitType.InputOctets)]
    [TestCase(DiameterCreditControlUnitType.OutputOctets)]
    [TestCase(DiameterCreditControlUnitType.ServiceSpecificUnits)]
    public void EquivalentReferencesForEveryPoolUnitTypeCollapseToOneState(
        DiameterCreditControlUnitType unitType)
    {
        List<DiameterPoolReference> pools =
            new List<DiameterPoolReference>();
        DiameterPoolReference first = CreatePoolReference(
            77U,
            unitType,
            10L,
            -1);
        DiameterPoolReference equivalent = CreatePoolReference(
            77U,
            unitType,
            1L,
            0);

        CreditControlMultipleServiceState.AddCreditControlPoolState(
            pools,
            first);
        CreditControlMultipleServiceState.AddCreditControlPoolState(
            pools,
            equivalent);
        first.ValueDigits = 999L;

        Assert.Multiple(() =>
        {
            Assert.That(pools, Has.Count.EqualTo(1));
            Assert.That(pools[0].UnitType, Is.EqualTo(unitType));
            Assert.That(pools[0].ValueDigits, Is.EqualTo(10L));
            Assert.That(pools[0].Exponent, Is.EqualTo(-1));
        });
    }

    [Test]
    public void SamePoolIdentifierRetainsIndependentUnitTypes()
    {
        List<DiameterPoolReference> pools =
            new List<DiameterPoolReference>();
        DiameterCreditControlUnitType[] types =
            (DiameterCreditControlUnitType[])Enum.GetValues(
                typeof(DiameterCreditControlUnitType));

        int index = 0;
        while (index < types.Length)
        {
            CreditControlMultipleServiceState.AddCreditControlPoolState(
                pools,
                CreatePoolReference(88U, types[index], 1L, 0));
            index = index + 1;
        }

        Assert.That(pools, Has.Count.EqualTo(types.Length));
    }

    [TestCase(DiameterCreditControlUnitType.Time, 99UL, false)]
    [TestCase(DiameterCreditControlUnitType.Time, 100UL, true)]
    [TestCase(DiameterCreditControlUnitType.Money, 99UL, false)]
    [TestCase(DiameterCreditControlUnitType.Money, 100UL, true)]
    [TestCase(DiameterCreditControlUnitType.TotalOctets, 99UL, false)]
    [TestCase(DiameterCreditControlUnitType.TotalOctets, 100UL, true)]
    [TestCase(DiameterCreditControlUnitType.InputOctets, 99UL, false)]
    [TestCase(DiameterCreditControlUnitType.InputOctets, 100UL, true)]
    [TestCase(DiameterCreditControlUnitType.OutputOctets, 99UL, false)]
    [TestCase(DiameterCreditControlUnitType.OutputOctets, 100UL, true)]
    [TestCase(DiameterCreditControlUnitType.ServiceSpecificUnits, 99UL, false)]
    [TestCase(DiameterCreditControlUnitType.ServiceSpecificUnits, 100UL, true)]
    public void QuotaExhaustionCoversEveryGrantedAndUsedUnitType(
        DiameterCreditControlUnitType unitType,
        ulong usedValue,
        bool expected)
    {
        DiameterServiceUnit granted = CreateUnit(unitType, 100UL);
        DiameterServiceUnit used = CreateUnit(unitType, usedValue);

        bool exhausted = CreditControlQuotaState
            .IsCreditControlQuotaExhausted(granted, used);

        Assert.That(exhausted, Is.EqualTo(expected));
    }

    [Test]
    public void MoneyExhaustionUsesExactExponentAndCurrencyComparison()
    {
        DiameterServiceUnit granted = CreateMoneyUnit(100L, -2, 840U);
        DiameterServiceUnit equal = CreateMoneyUnit(1000L, -3, 840U);
        DiameterServiceUnit below = CreateMoneyUnit(999L, -3, 840U);
        DiameterServiceUnit otherCurrency =
            CreateMoneyUnit(100L, -2, 978U);

        Assert.Multiple(() =>
        {
            Assert.That(CreditControlQuotaState
                .IsCreditControlQuotaExhausted(granted, equal), Is.True);
            Assert.That(CreditControlQuotaState
                .IsCreditControlQuotaExhausted(granted, below), Is.False);
            Assert.That(CreditControlQuotaState
                .IsCreditControlQuotaExhausted(granted, otherCurrency),
                Is.False);
        });
    }

    [TestCase(DiameterCreditControlUnitType.Time)]
    [TestCase(DiameterCreditControlUnitType.TotalOctets)]
    [TestCase(DiameterCreditControlUnitType.InputOctets)]
    [TestCase(DiameterCreditControlUnitType.OutputOctets)]
    [TestCase(DiameterCreditControlUnitType.ServiceSpecificUnits)]
    public void ThresholdBoundaryCoversEveryMeasuredUnitType(
        DiameterCreditControlUnitType unitType)
    {
        DiameterServiceUnit granted = CreateUnit(unitType, 100UL);
        DiameterServiceUnit threshold = CreateUnit(unitType, 10UL);
        DiameterServiceUnit before = CreateUnit(unitType, 89UL);
        DiameterServiceUnit at = CreateUnit(unitType, 90UL);
        DiameterServiceUnit exhausted = CreateUnit(unitType, 100UL);

        Assert.Multiple(() =>
        {
            Assert.That(CreditControlQuotaState
                .IsCreditControlThresholdReached(
                    granted, before, threshold), Is.False);
            Assert.That(CreditControlQuotaState
                .IsCreditControlThresholdReached(
                    granted, at, threshold), Is.True);
            Assert.That(CreditControlQuotaState
                .IsCreditControlThresholdReached(
                    granted, exhausted, threshold), Is.True);
        });
    }

    [TestCase(DiameterTariffChangeUsage.UnitBeforeTariffChange)]
    [TestCase(DiameterTariffChangeUsage.UnitAfterTariffChange)]
    [TestCase(DiameterTariffChangeUsage.UnitIndeterminate)]
    public void EveryTariffChangeUsageRoundTripsForUsedAndMultipleServiceUnits(
        DiameterTariffChangeUsage usage)
    {
        CreditControlCommandParser parser = new CreditControlCommandParser(
            CreateClock());
        DiameterServiceUnit used = CreateUnit(
            DiameterCreditControlUnitType.Time,
            5UL);
        used.TariffChangeUsage = usage;
        used.TariffChangeUsagePresent = true;
        DiameterMultipleServiceCredit group = CreateGroup();
        group.TariffChangeUsage = usage;
        group.TariffChangeUsagePresent = true;

        DiameterServiceUnit parsedUsed = parser
            .ReadCreditControlServiceUnitAvp(
                CreditControlCommandCodec.CreateCreditControlServiceUnitAvp(
                    DiameterAvpCodes.UsedServiceUnit,
                    used));
        DiameterMultipleServiceCredit parsedGroup = parser
            .ReadCreditControlMultipleServiceAvp(
                CreditControlCommandCodec
                    .CreateCreditControlMultipleServiceAvp(group));

        Assert.Multiple(() =>
        {
            Assert.That(parsedUsed.TariffChangeUsagePresent, Is.True);
            Assert.That(parsedUsed.TariffChangeUsage, Is.EqualTo(usage));
            Assert.That(parsedGroup.TariffChangeUsagePresent, Is.True);
            Assert.That(parsedGroup.TariffChangeUsage, Is.EqualTo(usage));
        });
    }

    [Test]
    public void ValidityAndTariffDeadlinesAdvanceInChronologicalOrder()
    {
        DateTimeOffset now = CreateClock().UtcTime;
        DiameterCreditControlSession session = CreateSession(now);
        session.ValidityDeadline = now.AddSeconds(5);
        session.ValidityDeadlinePresent = true;
        session.TariffChangeTime = now.AddSeconds(9);
        session.TariffChangeTimePresent = true;
        DiameterMultipleServiceCredit group = CreateGroup();
        group.ValidityDeadline = now.AddSeconds(7);
        group.ValidityDeadlinePresent = true;
        session.MultipleServiceCredits.Add(group);

        CreditControlQuotaState.AdvanceCreditControlInterrogationSchedule(
            session,
            now);
        DateTimeOffset first = session.NextInterrogationTime;
        CreditControlQuotaState.AdvanceCreditControlInterrogationSchedule(
            session,
            now.AddSeconds(5));
        DateTimeOffset second = session.NextInterrogationTime;
        CreditControlQuotaState.AdvanceCreditControlInterrogationSchedule(
            session,
            now.AddSeconds(7));
        DateTimeOffset third = session.NextInterrogationTime;
        CreditControlQuotaState.AdvanceCreditControlInterrogationSchedule(
            session,
            now.AddSeconds(9));

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(now.AddSeconds(5)));
            Assert.That(second, Is.EqualTo(now.AddSeconds(7)));
            Assert.That(third, Is.EqualTo(now.AddSeconds(9)));
            Assert.That(session.ValidityDeadlinePresent, Is.False);
            Assert.That(group.ValidityDeadlinePresent, Is.False);
            Assert.That(session.TariffChangeTimePresent, Is.False);
            Assert.That(session.NextInterrogationTimePresent, Is.False);
        });
    }

    [TestCase(DiameterFinalUnitAction.Terminate, false, true, true)]
    [TestCase(DiameterFinalUnitAction.Redirect, true, false, false)]
    [TestCase(DiameterFinalUnitAction.RestrictAccess, true, false, false)]
    public void EveryFinalUnitActionIsAppliedAfterSingleServiceExhaustion(
        DiameterFinalUnitAction action,
        bool continueService,
        bool terminateService,
        bool graceful)
    {
        DiameterCreditControlSession session = CreateSession(
            CreateClock().UtcTime);
        session.GrantedServiceUnit = CreateUnit(
            DiameterCreditControlUnitType.Time,
            10UL);
        session.UsedServiceUnit = CreateUnit(
            DiameterCreditControlUnitType.Time,
            10UL);
        session.FinalUnitInstruction = CreateFinalInstruction(action, false);
        DiameterCreditControlResult result =
            new DiameterCreditControlResult();

        CreditControlQuotaState.ApplyCreditControlUsagePolicy(session);
        CreditControlFinalUnitState.ApplyCreditControlFinalUnitUsagePolicy(
            session);
        CreditControlFinalUnitState.PopulateCreditControlFinalUnitResult(
            result,
            session);

        Assert.Multiple(() =>
        {
            Assert.That(result.FinalUnit, Is.True);
            Assert.That(result.AppliedFinalUnitInstruction!.Action,
                Is.EqualTo(action));
            Assert.That(result.ContinueService, Is.EqualTo(continueService));
            Assert.That(result.TerminateService, Is.EqualTo(terminateService));
            Assert.That(result.GracefulTerminationRequired,
                Is.EqualTo(graceful));
        });
    }

    [TestCase(DiameterFinalUnitAction.Terminate, false, true, true)]
    [TestCase(DiameterFinalUnitAction.Redirect, true, false, false)]
    [TestCase(DiameterFinalUnitAction.RestrictAccess, true, false, false)]
    public void EveryQosFinalUnitActionIsAppliedAfterGroupExhaustion(
        DiameterFinalUnitAction action,
        bool continueService,
        bool terminateService,
        bool graceful)
    {
        DiameterCreditControlSession session = CreateSession(
            CreateClock().UtcTime);
        DiameterMultipleServiceCredit group = CreateGroup();
        group.GrantedUnit = CreateUnit(
            DiameterCreditControlUnitType.TotalOctets,
            10UL);
        group.UsedUnits.Add(CreateUnit(
            DiameterCreditControlUnitType.TotalOctets,
            10UL));
        group.QosFinalUnitInstruction = CreateFinalInstruction(action, true);
        session.MultipleServiceCredits.Add(group);
        DiameterCreditControlResult result =
            new DiameterCreditControlResult();

        CreditControlQuotaState.ApplyCreditControlUsagePolicy(session);
        CreditControlFinalUnitState.ApplyCreditControlFinalUnitUsagePolicy(
            session);
        CreditControlFinalUnitState.PopulateCreditControlFinalUnitResult(
            result,
            session);

        Assert.Multiple(() =>
        {
            Assert.That(result.AppliedFinalUnitInstruction!.Action,
                Is.EqualTo(action));
            Assert.That(result.AppliedFinalUnitInstruction.Qos, Is.True);
            Assert.That(result.ContinueService, Is.EqualTo(continueService));
            Assert.That(result.TerminateService, Is.EqualTo(terminateService));
            Assert.That(result.GracefulTerminationRequired,
                Is.EqualTo(graceful));
        });
    }

    [Test]
    public void TerminateFinalUnitTakesPrecedenceAcrossExhaustedGroups()
    {
        DiameterCreditControlSession session = CreateSession(
            CreateClock().UtcTime);
        session.GrantedServiceUnit = CreateUnit(
            DiameterCreditControlUnitType.Time,
            1UL);
        session.UsedServiceUnit = CreateUnit(
            DiameterCreditControlUnitType.Time,
            1UL);
        session.FinalUnitInstruction = CreateFinalInstruction(
            DiameterFinalUnitAction.Redirect,
            false);
        DiameterMultipleServiceCredit group = CreateGroup();
        group.GrantedUnit = CreateUnit(
            DiameterCreditControlUnitType.TotalOctets,
            1UL);
        group.UsedUnits.Add(CreateUnit(
            DiameterCreditControlUnitType.TotalOctets,
            1UL));
        group.FinalUnitInstruction = CreateFinalInstruction(
            DiameterFinalUnitAction.Terminate,
            false);
        session.MultipleServiceCredits.Add(group);

        CreditControlFinalUnitState.ApplyCreditControlFinalUnitUsagePolicy(
            session);

        Assert.That(session.AppliedFinalUnitInstruction!.Action,
            Is.EqualTo(DiameterFinalUnitAction.Terminate));
    }

    [TestCase("none")]
    [TestCase("sub-session")]
    [TestCase("pool")]
    [TestCase("service")]
    [TestCase("rating")]
    [TestCase("service-rating")]
    [TestCase("all")]
    public void EveryMatchingReauthorizationScopeIsAccepted(string scope)
    {
        DiameterCreditControlSession session = CreateReAuthSession();
        DiameterMessage request = CreateReAuthRequest(scope, false);

        Assert.DoesNotThrow(() =>
            CreditControlReauthorizationController
                .ValidateCreditControlReAuthScope(session, request));
    }

    [TestCase("sub-session")]
    [TestCase("pool")]
    [TestCase("service")]
    [TestCase("rating")]
    [TestCase("service-rating")]
    public void EveryUnknownReauthorizationSelectorIsRejected(string scope)
    {
        DiameterCreditControlSession session = CreateReAuthSession();
        DiameterMessage request = CreateReAuthRequest(scope, true);

        Assert.Throws<DiameterCreditControlException>(() =>
            CreditControlReauthorizationController
                .ValidateCreditControlReAuthScope(session, request));
    }

    [TestCase(DiameterAvpCodes.AuthApplicationId)]
    [TestCase(DiameterAvpCodes.CcSubSessionId)]
    [TestCase(DiameterAvpCodes.GsuPoolIdentifier)]
    [TestCase(DiameterAvpCodes.ServiceIdentifier)]
    [TestCase(DiameterAvpCodes.RatingGroup)]
    public void DuplicateReauthorizationSelectorsAreRejected(uint code)
    {
        DiameterCreditControlSession session = CreateReAuthSession();
        DiameterMessage request = CreateReAuthRequest("all", false);
        DiameterAvp duplicate = new DiameterAvp();
        duplicate.Code = code;
        duplicate.Flags = DiameterAvpFlags.Mandatory;
        duplicate.Data = new byte[code == DiameterAvpCodes.CcSubSessionId ? 8 : 4];
        request.Avps.Add(duplicate);

        Assert.Throws<DiameterCreditControlException>(() =>
            CreditControlReauthorizationController
                .ValidateCreditControlReAuthScope(session, request));
    }

    private static TestDiameterClock CreateClock()
    {
        return new TestDiameterClock(
            new DateTimeOffset(2026, 8, 29, 20, 0, 0, TimeSpan.Zero),
            0L);
    }

    private static DiameterCreditControlDisposition
        SelectExpectedDisposition(
            DiameterCreditControlDisposition first,
            DiameterCreditControlDisposition second)
    {
        if (first == DiameterCreditControlDisposition.Failed ||
            second == DiameterCreditControlDisposition.Failed)
        {
            return DiameterCreditControlDisposition.Failed;
        }

        if (first == DiameterCreditControlDisposition.Denied ||
            second == DiameterCreditControlDisposition.Denied)
        {
            return DiameterCreditControlDisposition.Denied;
        }

        if (first == DiameterCreditControlDisposition.FinalUnit ||
            second == DiameterCreditControlDisposition.FinalUnit)
        {
            return DiameterCreditControlDisposition.FinalUnit;
        }

        if (first == DiameterCreditControlDisposition.PartiallyGranted ||
            second == DiameterCreditControlDisposition.PartiallyGranted)
        {
            return DiameterCreditControlDisposition.PartiallyGranted;
        }

        return DiameterCreditControlDisposition.Granted;
    }

    private static DiameterServiceUnit CreateUnit(
        DiameterCreditControlUnitType unitType,
        ulong value)
    {
        if (unitType == DiameterCreditControlUnitType.Money)
        {
            return CreateMoneyUnit((long)value, 0, 840U);
        }

        DiameterServiceUnit result = new DiameterServiceUnit();
        if (unitType == DiameterCreditControlUnitType.Time)
        {
            result.Time = (uint)value;
            result.TimePresent = true;
        }
        else if (unitType == DiameterCreditControlUnitType.TotalOctets)
        {
            result.TotalOctets = value;
            result.TotalOctetsPresent = true;
        }
        else if (unitType == DiameterCreditControlUnitType.InputOctets)
        {
            result.InputOctets = value;
            result.InputOctetsPresent = true;
        }
        else if (unitType == DiameterCreditControlUnitType.OutputOctets)
        {
            result.OutputOctets = value;
            result.OutputOctetsPresent = true;
        }
        else
        {
            result.ServiceSpecificUnits = value;
            result.ServiceSpecificUnitsPresent = true;
        }

        return result;
    }

    private static DiameterServiceUnit CreateMoneyUnit(
        long digits,
        int exponent,
        uint currency)
    {
        DiameterCostInformation money = new DiameterCostInformation();
        money.ValueDigits = digits;
        money.Exponent = exponent;
        money.ExponentPresent = true;
        money.CurrencyCode = currency;
        money.CurrencyCodePresent = true;
        DiameterServiceUnit result = new DiameterServiceUnit();
        result.Money = money;
        return result;
    }

    private static DiameterPoolReference CreatePoolReference(
        uint identifier,
        DiameterCreditControlUnitType unitType,
        long digits,
        int exponent)
    {
        DiameterPoolReference result = new DiameterPoolReference();
        result.PoolIdentifier = identifier;
        result.UnitType = unitType;
        result.ValueDigits = digits;
        result.Exponent = exponent;
        result.ExponentPresent = true;
        return result;
    }

    private static DiameterMultipleServiceCredit CreateGroup()
    {
        DiameterMultipleServiceCredit result =
            new DiameterMultipleServiceCredit();
        result.ServiceIdentifiers.Add(10U);
        result.RatingGroup = 100U;
        result.RatingGroupPresent = true;
        return result;
    }

    private static DiameterCreditControlSession CreateSession(
        DateTimeOffset now)
    {
        DiameterCreditControlSession result =
            new DiameterCreditControlSession();
        result.SessionId = "client.example;advanced;1";
        result.Role = DiameterSessionRole.Client;
        result.State = DiameterCreditControlState.Open;
        result.ServiceContextId = "advanced.service";
        result.DestinationRealm = new DiameterIdentity();
        result.DestinationRealm.Value = "remote.example";
        result.CreatedAt = now;
        result.UpdatedAt = now;
        return result;
    }

    private static DiameterFinalUnitInstruction CreateFinalInstruction(
        DiameterFinalUnitAction action,
        bool qos)
    {
        DiameterFinalUnitInstruction result =
            new DiameterFinalUnitInstruction();
        result.Action = action;
        result.Qos = qos;
        if (action == DiameterFinalUnitAction.Redirect)
        {
            result.RedirectAddress = qos ?
                "https://qos.example/final" :
                "https://legacy.example/final";
            result.RedirectExtension = qos;
            if (!qos)
            {
                result.RedirectAddressType = DiameterRedirectAddressType.Url;
                result.RedirectAddressTypePresent = true;
            }
        }
        else if (action == DiameterFinalUnitAction.RestrictAccess)
        {
            result.FilterIdentifiers.Add("restricted-service");
        }

        return result;
    }

    private static DiameterCreditControlSession CreateReAuthSession()
    {
        DiameterCreditControlSession result = CreateSession(
            CreateClock().UtcTime);
        result.CreditControlSubSessionId = 9UL;
        result.CreditControlSubSessionIdPresent = true;
        DiameterMultipleServiceCredit group = CreateGroup();
        group.PoolReferences.Add(CreatePoolReference(
            77U,
            DiameterCreditControlUnitType.Time,
            1L,
            0));
        result.MultipleServiceCredits.Add(group);
        result.PoolReferences.Add(group.PoolReferences[0]);
        return result;
    }

    private static DiameterMessage CreateReAuthRequest(
        string scope,
        bool unknown)
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        result.CommandCode = DiameterCommandCodes.ReAuth;
        result.ApplicationId = DiameterApplicationIds.CreditControl;
        result.Avps.Add(CreateUnsigned32Avp(
            DiameterAvpCodes.AuthApplicationId,
            DiameterApplicationIds.CreditControl));
        if (scope == "sub-session" || scope == "all")
        {
            DiameterAvp subSession = new DiameterAvp();
            subSession.Code = DiameterAvpCodes.CcSubSessionId;
            subSession.Flags = DiameterAvpFlags.Mandatory;
            subSession.Data = new byte[8];
            NetworkByteOrder.WriteUInt64(
                subSession.Data,
                unknown ? 99UL : 9UL);
            result.Avps.Add(subSession);
        }

        if (scope == "pool" || scope == "all")
        {
            result.Avps.Add(CreateUnsigned32Avp(
                DiameterAvpCodes.GsuPoolIdentifier,
                unknown ? 99U : 77U));
        }

        if (scope == "service" || scope == "service-rating" ||
            scope == "all")
        {
            result.Avps.Add(CreateUnsigned32Avp(
                DiameterAvpCodes.ServiceIdentifier,
                unknown ? 99U : 10U));
        }

        if (scope == "rating" || scope == "service-rating" ||
            scope == "all")
        {
            result.Avps.Add(CreateUnsigned32Avp(
                DiameterAvpCodes.RatingGroup,
                unknown ? 999U : 100U));
        }

        return result;
    }

    private static DiameterAvp CreateUnsigned32Avp(uint code, uint value)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(result.Data, value);
        return result;
    }
}
