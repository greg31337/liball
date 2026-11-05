using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class CreditControlServiceUnitTests
{
    [Test]
    public void RequestedAndUsedUnitsRoundTripEveryBasicUnitKind()
    {
        CreditControlUnitTestFixture fixture = CreateUnitFixture();
        DiameterCreditControlRequest request = CreateUnitRequest();
        request.RequestedServiceUnit = CreateCompleteServiceUnit(900UL);
        request.UsedServiceUnits.Add(CreateCompleteServiceUnit(100UL));
        request.UsedServiceUnits.Add(CreateCompleteServiceUnit(200UL));

        DiameterMessage encoded = fixture.Factory
            .CreateCreditControlRequest(request);
        DiameterValidationResult validation = new DiameterValidator()
            .ValidateMessage(
                encoded,
                new DiameterDictionary().ReadDictionarySnapshot(),
                new DiameterLimitConfiguration());
        DiameterCreditControlRequest parsed = fixture.Parser
            .ParseCreditControlRequest(encoded);
        request.RequestedServiceUnit.Money!.ValueDigits = 9999L;
        request.UsedServiceUnits[0].TotalOctets = 9999UL;

        Assert.Multiple(() =>
        {
            AssertServiceUnit(parsed.RequestedServiceUnit!, 900UL);
            Assert.That(validation.Status,
                Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(parsed.UsedServiceUnits, Has.Count.EqualTo(2));
            AssertServiceUnit(parsed.UsedServiceUnits[0], 100UL);
            AssertServiceUnit(parsed.UsedServiceUnits[1], 200UL);
            Assert.That(CountUnitAvps(
                encoded,
                DiameterAvpCodes.RequestedServiceUnit),
                Is.EqualTo(1));
            Assert.That(CountUnitAvps(
                encoded,
                DiameterAvpCodes.UsedServiceUnit),
                Is.EqualTo(2));
        });
    }

    [Test]
    public void GrantedUnitsRoundTripInCreditControlAnswer()
    {
        CreditControlUnitTestFixture fixture = CreateUnitFixture();
        DiameterCreditControlRequest request = CreateUnitRequest();
        DiameterMessage requestMessage = fixture.Factory
            .CreateCreditControlRequest(request);
        DiameterCreditControlAnswer answer =
            new DiameterCreditControlAnswer();
        answer.SessionId = request.SessionId;
        answer.RequestType = request.RequestType;
        answer.RequestNumber = request.RequestNumber;
        answer.ResultCode = DiameterResultCodes.Success;
        answer.GrantedServiceUnit = CreateCompleteServiceUnit(
            ulong.MaxValue);
        answer.GrantedServiceUnit.Time = uint.MaxValue;
        answer.GrantedServiceUnit.Money!.ValueDigits = long.MinValue;

        DiameterMessage encoded = fixture.Factory
            .CreateCreditControlAnswer(requestMessage, answer);
        DiameterValidationResult validation = new DiameterValidator()
            .ValidateMessage(
                encoded,
                new DiameterDictionary().ReadDictionarySnapshot(),
                new DiameterLimitConfiguration());
        DiameterCreditControlAnswer parsed = fixture.Parser
            .ParseCreditControlAnswer(encoded, request);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.GrantedServiceUnit, Is.Not.Null);
            Assert.That(validation.Status,
                Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(parsed.GrantedServiceUnit!.Time,
                Is.EqualTo(uint.MaxValue));
            Assert.That(parsed.GrantedServiceUnit.TotalOctets,
                Is.EqualTo(ulong.MaxValue));
            Assert.That(parsed.GrantedServiceUnit.InputOctets,
                Is.EqualTo(ulong.MaxValue - 1UL));
            Assert.That(parsed.GrantedServiceUnit.OutputOctets,
                Is.EqualTo(ulong.MaxValue - 2UL));
            Assert.That(parsed.GrantedServiceUnit.ServiceSpecificUnits,
                Is.EqualTo(ulong.MaxValue - 3UL));
            Assert.That(parsed.GrantedServiceUnit.Money!.ValueDigits,
                Is.EqualTo(long.MinValue));
            Assert.That(parsed.GrantedServiceUnit.Money.Exponent,
                Is.EqualTo(-2));
            Assert.That(parsed.GrantedServiceUnit.Money.CurrencyCode,
                Is.EqualTo(840U));
        });
    }

    [Test]
    public void DuplicateServiceUnitChildrenAreRejected()
    {
        CreditControlUnitTestFixture fixture = CreateUnitFixture();
        DiameterCreditControlRequest request = CreateUnitRequest();
        DiameterMessage encoded = fixture.Factory
            .CreateCreditControlRequest(request);
        List<DiameterAvp> duplicateChildren = new List<DiameterAvp>();
        duplicateChildren.Add(CreditControlCommandCodec
            .CreateCreditControlUnsigned32ValueAvp(
                DiameterAvpCodes.CcTime,
                1U));
        duplicateChildren.Add(CreditControlCommandCodec
            .CreateCreditControlUnsigned32ValueAvp(
                DiameterAvpCodes.CcTime,
                2U));
        encoded.Avps.Add(CreditControlCommandCodec
            .CreateCreditControlGroupedAvp(
                DiameterAvpCodes.RequestedServiceUnit,
                duplicateChildren));

        Assert.Throws<DiameterCreditControlException>(() =>
            fixture.Parser.ParseCreditControlRequest(encoded));
    }

    [Test]
    public void ServiceUnitArithmeticAccumulatesExactly()
    {
        DiameterServiceUnit first = CreateCompleteServiceUnit(100UL);
        first.Time = 10U;
        first.Money!.ValueDigits = 125L;
        DiameterServiceUnit second = CreateCompleteServiceUnit(200UL);
        second.Time = 20U;
        second.Money!.ValueDigits = 75L;

        DiameterServiceUnit total = CreditControlUnitArithmetic
            .AddCreditControlServiceUnits(first, second);

        Assert.Multiple(() =>
        {
            Assert.That(total.Time, Is.EqualTo(30U));
            Assert.That(total.TotalOctets, Is.EqualTo(300UL));
            Assert.That(total.InputOctets, Is.EqualTo(298UL));
            Assert.That(total.OutputOctets, Is.EqualTo(296UL));
            Assert.That(total.ServiceSpecificUnits, Is.EqualTo(294UL));
            Assert.That(total.Money!.ValueDigits, Is.EqualTo(200L));
            Assert.That(first.TotalOctets, Is.EqualTo(100UL));
            Assert.That(first.Money.ValueDigits, Is.EqualTo(125L));
        });
    }

    [Test]
    public void ServiceUnitArithmeticRejectsEveryNumericOverflow()
    {
        DiameterServiceUnit maximumTime = new DiameterServiceUnit();
        maximumTime.Time = uint.MaxValue;
        maximumTime.TimePresent = true;
        DiameterServiceUnit oneTime = new DiameterServiceUnit();
        oneTime.Time = 1U;
        oneTime.TimePresent = true;
        DiameterServiceUnit maximumOctets = CreateUnsignedMaximumUnit();
        DiameterServiceUnit oneOctet = CreateUnsignedOneUnit();
        DiameterServiceUnit maximumMoney = new DiameterServiceUnit();
        maximumMoney.Money = CreateMoney(long.MaxValue);
        DiameterServiceUnit oneMoney = new DiameterServiceUnit();
        oneMoney.Money = CreateMoney(1L);

        Assert.Multiple(() =>
        {
            Assert.Throws<DiameterCreditControlException>(() =>
                CreditControlUnitArithmetic.AddCreditControlServiceUnits(
                    maximumTime,
                    oneTime));
            Assert.Throws<DiameterCreditControlException>(() =>
                CreditControlUnitArithmetic.AddCreditControlServiceUnits(
                    maximumOctets,
                    oneOctet));
            Assert.Throws<DiameterCreditControlException>(() =>
                CreditControlUnitArithmetic.AddCreditControlServiceUnits(
                    maximumMoney,
                    oneMoney));
        });
    }

    [Test]
    public void PartialGrantIsDerivedAcrossUnsignedAndMoneyUnits()
    {
        DiameterServiceUnit requested = CreateCompleteServiceUnit(1000UL);
        requested.Time = 60U;
        requested.Money!.ValueDigits = 500L;
        DiameterServiceUnit partialTime = CreateCompleteServiceUnit(1000UL);
        partialTime.Time = 59U;
        partialTime.Money!.ValueDigits = 500L;
        DiameterServiceUnit partialMoney = CreateCompleteServiceUnit(1000UL);
        partialMoney.Time = 60U;
        partialMoney.Money!.ValueDigits = 499L;
        DiameterServiceUnit complete = CreateCompleteServiceUnit(1000UL);
        complete.Time = 60U;
        complete.Money!.ValueDigits = 500L;

        Assert.Multiple(() =>
        {
            Assert.That(CreditControlUnitArithmetic
                .ReadCreditControlGrantDisposition(
                    requested,
                    partialTime,
                    DiameterCreditControlDisposition.Granted),
                Is.EqualTo(
                    DiameterCreditControlDisposition.PartiallyGranted));
            Assert.That(CreditControlUnitArithmetic
                .ReadCreditControlGrantDisposition(
                    requested,
                    partialMoney,
                    DiameterCreditControlDisposition.Granted),
                Is.EqualTo(
                    DiameterCreditControlDisposition.PartiallyGranted));
            Assert.That(CreditControlUnitArithmetic
                .ReadCreditControlGrantDisposition(
                    requested,
                    complete,
                    DiameterCreditControlDisposition.Granted),
                Is.EqualTo(DiameterCreditControlDisposition.Granted));
        });
    }

    [Test]
    public void ClientSessionPublishesDerivedPartialGrant()
    {
        TestDiameterClock clock = new TestDiameterClock(
            new DateTimeOffset(
                2026,
                8,
                29,
                18,
                0,
                0,
                TimeSpan.Zero),
            0);
        TestCreditControlStore store = new TestCreditControlStore();
        CreditControlClientStateMachine machine =
            new CreditControlClientStateMachine(store, clock);
        DiameterCreditControlRequest request = CreateUnitRequest();
        request.RequestedServiceUnit = new DiameterServiceUnit();
        request.RequestedServiceUnit.Time = 60U;
        request.RequestedServiceUnit.TimePresent = true;
        machine.BeginCreditControlClientSession(request, 501);
        DiameterCreditControlAnswer answer =
            new DiameterCreditControlAnswer();
        answer.SessionId = request.SessionId;
        answer.RequestType = request.RequestType;
        answer.RequestNumber = 0U;
        answer.ResultCode = DiameterResultCodes.Success;
        answer.GrantedServiceUnit = new DiameterServiceUnit();
        answer.GrantedServiceUnit.Time = 30U;
        answer.GrantedServiceUnit.TimePresent = true;

        DiameterCreditControlResult result =
            machine.ProcessCreditControlClientAnswer(501, answer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Successful, Is.True);
            Assert.That(result.ContinueService, Is.True);
            Assert.That(result.Disposition,
                Is.EqualTo(
                    DiameterCreditControlDisposition.PartiallyGranted));
            Assert.That(result.Answer!.Disposition,
                Is.EqualTo(
                    DiameterCreditControlDisposition.PartiallyGranted));
            Assert.That(result.Session!.Disposition,
                Is.EqualTo(
                    DiameterCreditControlDisposition.PartiallyGranted));
        });
    }

    [Test]
    public void ServerSessionAccumulatesUsedUnitsAcrossUpdates()
    {
        TestDiameterClock clock = new TestDiameterClock(
            new DateTimeOffset(
                2026,
                8,
                29,
                19,
                0,
                0,
                TimeSpan.Zero),
            0);
        TestCreditControlStore store = new TestCreditControlStore();
        TestCreditControlRequestHandler handler =
            new TestCreditControlRequestHandler();
        CreditControlServerStateMachine machine =
            new CreditControlServerStateMachine(store, handler, clock);
        DiameterCreditControlRequest initial = CreateUnitRequest();
        initial.SessionId = "server.example;usage;1";
        machine.ProcessCreditControlServerRequest(initial);
        DiameterCreditControlRequest firstUpdate = CreateUnitRequest();
        firstUpdate.SessionId = initial.SessionId;
        firstUpdate.RequestType = DiameterCreditControlRequestType.Update;
        firstUpdate.RequestNumber = 1U;
        DiameterServiceUnit firstUsed = new DiameterServiceUnit();
        firstUsed.Time = 10U;
        firstUsed.TimePresent = true;
        firstUsed.TotalOctets = 100UL;
        firstUsed.TotalOctetsPresent = true;
        firstUpdate.UsedServiceUnits.Add(firstUsed);
        machine.ProcessCreditControlServerRequest(firstUpdate);
        DiameterCreditControlRequest secondUpdate = CreateUnitRequest();
        secondUpdate.SessionId = initial.SessionId;
        secondUpdate.RequestType = DiameterCreditControlRequestType.Update;
        secondUpdate.RequestNumber = 2U;
        DiameterServiceUnit secondUsed = new DiameterServiceUnit();
        secondUsed.Time = 20U;
        secondUsed.TimePresent = true;
        secondUsed.TotalOctets = 200UL;
        secondUsed.TotalOctetsPresent = true;
        secondUpdate.UsedServiceUnits.Add(secondUsed);
        machine.ProcessCreditControlServerRequest(secondUpdate);

        DiameterCreditControlSession? session =
            machine.FindCreditControlServerSession(initial.SessionId);

        Assert.Multiple(() =>
        {
            Assert.That(session, Is.Not.Null);
            Assert.That(session!.UsedServiceUnit, Is.Not.Null);
            Assert.That(session.UsedServiceUnit!.Time, Is.EqualTo(30U));
            Assert.That(session.UsedServiceUnit.TotalOctets,
                Is.EqualTo(300UL));
            Assert.That(session.NextRequestNumber, Is.EqualTo(3U));
            Assert.That(handler.Requests, Has.Count.EqualTo(3));
        });
    }

    private static CreditControlUnitTestFixture CreateUnitFixture()
    {
        CreditControlUnitTestFixture result =
            new CreditControlUnitTestFixture();
        TestDiameterClock clock = new TestDiameterClock(
            new DateTimeOffset(
                2026,
                8,
                29,
                17,
                0,
                0,
                TimeSpan.Zero),
            0);
        DiameterIdentity host = new DiameterIdentity();
        host.Value = "client.example";
        DiameterIdentity realm = new DiameterIdentity();
        realm.Value = "example";
        result.Factory = new CreditControlCommandFactory(
            new CreditControlCommandIdentifierSource(),
            host,
            realm);
        result.Parser = new CreditControlCommandParser(clock);
        return result;
    }

    private static DiameterCreditControlRequest CreateUnitRequest()
    {
        DiameterCreditControlRequest result =
            new DiameterCreditControlRequest();
        result.SessionId = "client.example;units;1";
        result.DestinationRealm.Value = "remote.example";
        result.ServiceContextId = "example.units";
        result.RequestType = DiameterCreditControlRequestType.Initial;
        return result;
    }

    private static DiameterServiceUnit CreateCompleteServiceUnit(ulong value)
    {
        DiameterServiceUnit result = new DiameterServiceUnit();
        result.Time = 30U;
        result.TimePresent = true;
        result.Money = CreateMoney(12345L);
        result.TotalOctets = value;
        result.TotalOctetsPresent = true;
        result.InputOctets = value - 1UL;
        result.InputOctetsPresent = true;
        result.OutputOctets = value - 2UL;
        result.OutputOctetsPresent = true;
        result.ServiceSpecificUnits = value - 3UL;
        result.ServiceSpecificUnitsPresent = true;
        return result;
    }

    private static DiameterCostInformation CreateMoney(long valueDigits)
    {
        DiameterCostInformation result =
            new DiameterCostInformation();
        result.ValueDigits = valueDigits;
        result.Exponent = -2;
        result.ExponentPresent = true;
        result.CurrencyCode = 840U;
        result.CurrencyCodePresent = true;
        return result;
    }

    private static DiameterServiceUnit CreateUnsignedMaximumUnit()
    {
        DiameterServiceUnit result = new DiameterServiceUnit();
        result.TotalOctets = ulong.MaxValue;
        result.TotalOctetsPresent = true;
        result.InputOctets = ulong.MaxValue;
        result.InputOctetsPresent = true;
        result.OutputOctets = ulong.MaxValue;
        result.OutputOctetsPresent = true;
        result.ServiceSpecificUnits = ulong.MaxValue;
        result.ServiceSpecificUnitsPresent = true;
        return result;
    }

    private static DiameterServiceUnit CreateUnsignedOneUnit()
    {
        DiameterServiceUnit result = new DiameterServiceUnit();
        result.TotalOctets = 1UL;
        result.TotalOctetsPresent = true;
        result.InputOctets = 1UL;
        result.InputOctetsPresent = true;
        result.OutputOctets = 1UL;
        result.OutputOctetsPresent = true;
        result.ServiceSpecificUnits = 1UL;
        result.ServiceSpecificUnitsPresent = true;
        return result;
    }

    private static int CountUnitAvps(
        DiameterMessage message,
        uint code)
    {
        int count = 0;
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (message.Avps[index].VendorId == DiameterVendorIds.Ietf &&
                message.Avps[index].Code == code)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    private static void AssertServiceUnit(
        DiameterServiceUnit unit,
        ulong value)
    {
        Assert.Multiple(() =>
        {
            Assert.That(unit.Time, Is.EqualTo(30U));
            Assert.That(unit.TimePresent, Is.True);
            Assert.That(unit.Money!.ValueDigits, Is.EqualTo(12345L));
            Assert.That(unit.Money.Exponent, Is.EqualTo(-2));
            Assert.That(unit.Money.CurrencyCode, Is.EqualTo(840U));
            Assert.That(unit.TotalOctets, Is.EqualTo(value));
            Assert.That(unit.InputOctets, Is.EqualTo(value - 1UL));
            Assert.That(unit.OutputOctets, Is.EqualTo(value - 2UL));
            Assert.That(unit.ServiceSpecificUnits,
                Is.EqualTo(value - 3UL));
        });
    }
}

internal class CreditControlUnitTestFixture
{
    internal CreditControlCommandFactory Factory = null!;
    internal CreditControlCommandParser Parser = null!;
}
