using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class CreditControlMultipleServiceTests
{
    [Test]
    public void MultipleServiceRequestRoundTripsIndependentGroups()
    {
        CreditControlMultipleTestFixture fixture = CreateMultipleFixture();
        DiameterCreditControlRequest request = CreateMultipleRequest();
        DiameterMultipleServiceCredit timeCredit =
            CreateMultipleCredit(10U, 100U);
        timeCredit.RequestedUnit = CreateTimeUnit(60U);
        timeCredit.UsedUnits.Add(CreateTimeUnit(15U));
        DiameterMultipleServiceCredit octetCredit =
            CreateMultipleCredit(20U, 200U);
        octetCredit.RequestedUnit = CreateOctetUnit(1000UL);
        octetCredit.UsedUnits.Add(CreateOctetUnit(250UL));
        request.MultipleServiceCredits.Add(timeCredit);
        request.MultipleServiceCredits.Add(octetCredit);

        DiameterMessage encoded = fixture.Factory
            .CreateCreditControlRequest(request);
        DiameterValidationResult validation = new DiameterValidator()
            .ValidateMessage(
                encoded,
                new DiameterDictionary().ReadDictionarySnapshot(),
                new DiameterLimitConfiguration());
        DiameterCreditControlRequest parsed = fixture.Parser
            .ParseCreditControlRequest(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Status,
                Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(parsed.MultipleServicesIndicatorPresent, Is.True);
            Assert.That(parsed.MultipleServicesIndicator,
                Is.EqualTo(DiameterMultipleServicesIndicator.Supported));
            Assert.That(parsed.MultipleServiceCredits,
                Has.Count.EqualTo(2));
            Assert.That(parsed.MultipleServiceCredits[0]
                .ServiceIdentifiers[0], Is.EqualTo(10U));
            Assert.That(parsed.MultipleServiceCredits[0].RatingGroup,
                Is.EqualTo(100U));
            Assert.That(parsed.MultipleServiceCredits[0]
                .RequestedUnit!.Time, Is.EqualTo(60U));
            Assert.That(parsed.MultipleServiceCredits[0]
                .UsedUnits[0].Time, Is.EqualTo(15U));
            Assert.That(parsed.MultipleServiceCredits[1]
                .RequestedUnit!.TotalOctets, Is.EqualTo(1000UL));
            Assert.That(parsed.MultipleServiceCredits[1]
                .UsedUnits[0].TotalOctets, Is.EqualTo(250UL));
        });
    }

    [Test]
    public void MultipleServiceAnswerRoundTripsPoolReferences()
    {
        CreditControlMultipleTestFixture fixture = CreateMultipleFixture();
        DiameterCreditControlRequest request = CreateMultipleRequest();
        DiameterMessage requestMessage = fixture.Factory
            .CreateCreditControlRequest(request);
        DiameterCreditControlAnswer answer =
            new DiameterCreditControlAnswer();
        answer.SessionId = request.SessionId;
        answer.RequestType = request.RequestType;
        answer.RequestNumber = request.RequestNumber;
        answer.ResultCode = DiameterResultCodes.Success;
        DiameterMultipleServiceCredit credit =
            CreateMultipleCredit(10U, 100U);
        credit.GrantedUnit = CreateTimeUnit(30U);
        credit.ResultCode = DiameterResultCodes.Success;
        credit.ResultCodePresent = true;
        credit.PoolReferences.Add(CreatePoolReference(
            77U,
            DiameterCreditControlUnitType.Time,
            25L,
            -1));
        answer.MultipleServiceCredits.Add(credit);

        DiameterMessage encoded = fixture.Factory
            .CreateCreditControlAnswer(requestMessage, answer);
        DiameterValidationResult validation = new DiameterValidator()
            .ValidateMessage(
                encoded,
                new DiameterDictionary().ReadDictionarySnapshot(),
                new DiameterLimitConfiguration());
        DiameterCreditControlAnswer parsed = fixture.Parser
            .ParseCreditControlAnswer(encoded, request);
        DiameterPoolReference pool = parsed.MultipleServiceCredits[0]
            .PoolReferences[0];

        Assert.Multiple(() =>
        {
            Assert.That(validation.Status,
                Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(parsed.MultipleServiceCredits,
                Has.Count.EqualTo(1));
            Assert.That(parsed.MultipleServiceCredits[0]
                .GrantedUnit!.Time, Is.EqualTo(30U));
            Assert.That(parsed.MultipleServiceCredits[0].ResultCode,
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(pool.PoolIdentifier, Is.EqualTo(77U));
            Assert.That(pool.UnitType,
                Is.EqualTo(DiameterCreditControlUnitType.Time));
            Assert.That(pool.ValueDigits, Is.EqualTo(25L));
            Assert.That(pool.Exponent, Is.EqualTo(-1));
            Assert.That(pool.ExponentPresent, Is.True);
        });
    }

    [Test]
    public void ClientRetainsIndependentQuotaAndPooledState()
    {
        DateTimeOffset now =
            new DateTimeOffset(2026, 8, 29, 20, 0, 0, TimeSpan.Zero);
        TestDiameterClock clock = new TestDiameterClock(now, 0);
        TestCreditControlStore store = new TestCreditControlStore();
        CreditControlClientStateMachine machine =
            new CreditControlClientStateMachine(store, clock);
        DiameterCreditControlRequest initial = CreateMultipleRequest();
        DiameterMultipleServiceCredit timeRequest =
            CreateMultipleCredit(10U, 100U);
        timeRequest.RequestedUnit = CreateTimeUnit(60U);
        DiameterMultipleServiceCredit octetRequest =
            CreateMultipleCredit(20U, 200U);
        octetRequest.RequestedUnit = CreateOctetUnit(1000UL);
        initial.MultipleServiceCredits.Add(timeRequest);
        initial.MultipleServiceCredits.Add(octetRequest);
        machine.BeginCreditControlClientSession(initial, 601);
        DiameterCreditControlAnswer initialAnswer =
            CreateMultipleAnswer(initial, 0U);
        initialAnswer.MultipleServiceCredits.Add(
            CreateGrantedTimeCredit(10U, 100U, 60U));
        initialAnswer.MultipleServiceCredits.Add(
            CreateGrantedOctetCredit(20U, 200U, 1000UL));
        machine.ProcessCreditControlClientAnswer(601, initialAnswer);

        DiameterCreditControlRequest update = CreateMultipleRequest();
        update.RequestType = DiameterCreditControlRequestType.Update;
        update.MultipleServicesIndicatorPresent = false;
        DiameterMultipleServiceCredit timeUsage =
            CreateMultipleCredit(10U, 100U);
        timeUsage.RequestedUnit = CreateTimeUnit(60U);
        timeUsage.UsedUnits.Add(CreateTimeUnit(10U));
        DiameterMultipleServiceCredit octetUsage =
            CreateMultipleCredit(20U, 200U);
        octetUsage.RequestedUnit = CreateOctetUnit(1000UL);
        octetUsage.UsedUnits.Add(CreateOctetUnit(250UL));
        update.MultipleServiceCredits.Add(timeUsage);
        update.MultipleServiceCredits.Add(octetUsage);
        DiameterCreditControlResult pending =
            machine.UpdateCreditControlClientSession(update, 602);
        DiameterCreditControlAnswer updateAnswer =
            CreateMultipleAnswer(update, 1U);
        updateAnswer.MultipleServiceCredits.Add(
            CreateGrantedTimeCredit(10U, 100U, 30U));
        DiameterCreditControlResult completed =
            machine.ProcessCreditControlClientAnswer(602, updateAnswer);
        DiameterMultipleServiceCredit timeState =
            completed.Session!.MultipleServiceCredits[0];
        DiameterMultipleServiceCredit octetState =
            completed.Session.MultipleServiceCredits[1];

        Assert.Multiple(() =>
        {
            Assert.That(pending.Session!.MultipleServicesIndicatorPresent,
                Is.True);
            Assert.That(pending.Session.MultipleServicesIndicator,
                Is.EqualTo(DiameterMultipleServicesIndicator.Supported));
            Assert.That(timeState.UsedUnits[0].Time, Is.EqualTo(10U));
            Assert.That(timeState.GrantedUnit!.Time, Is.EqualTo(30U));
            Assert.That(timeState.Disposition,
                Is.EqualTo(
                    DiameterCreditControlDisposition.PartiallyGranted));
            Assert.That(octetState.UsedUnits[0].TotalOctets,
                Is.EqualTo(250UL));
            Assert.That(octetState.GrantedUnit!.TotalOctets,
                Is.EqualTo(1000UL));
            Assert.That(completed.Session.PoolReferences,
                Has.Count.EqualTo(1));
            Assert.That(completed.Disposition,
                Is.EqualTo(
                    DiameterCreditControlDisposition.PartiallyGranted));
        });
    }

    [Test]
    public void DuplicateMultipleServiceKeysAreRejectedBeforeEncoding()
    {
        CreditControlMultipleTestFixture fixture = CreateMultipleFixture();
        DiameterCreditControlRequest request = CreateMultipleRequest();
        request.MultipleServiceCredits.Add(
            CreateMultipleCredit(10U, 100U));
        request.MultipleServiceCredits.Add(
            CreateMultipleCredit(10U, 100U));

        Assert.Throws<DiameterCreditControlException>(() =>
            fixture.Factory.CreateCreditControlRequest(request));
    }

    [Test]
    public void ConflictingPoolConversionsAreRejectedDuringStateUpdate()
    {
        DiameterCreditControlSession session =
            new DiameterCreditControlSession();
        DiameterMultipleServiceCredit first =
            CreateMultipleCredit(10U, 100U);
        first.PoolReferences.Add(CreatePoolReference(
            77U,
            DiameterCreditControlUnitType.Time,
            1L,
            0));
        DiameterMultipleServiceCredit second =
            CreateMultipleCredit(20U, 200U);
        second.PoolReferences.Add(CreatePoolReference(
            77U,
            DiameterCreditControlUnitType.Time,
            2L,
            0));
        List<DiameterMultipleServiceCredit> answers =
            new List<DiameterMultipleServiceCredit>();
        answers.Add(first);
        answers.Add(second);

        Assert.Throws<DiameterCreditControlException>(() =>
            CreditControlMultipleServiceState
                .ApplyCreditControlMultipleServiceAnswer(
                    session,
                    answers,
                    DiameterCreditControlDisposition.Granted));
    }

    private static CreditControlMultipleTestFixture CreateMultipleFixture()
    {
        CreditControlMultipleTestFixture result =
            new CreditControlMultipleTestFixture();
        DiameterIdentity host = new DiameterIdentity();
        host.Value = "client.example";
        DiameterIdentity realm = new DiameterIdentity();
        realm.Value = "example";
        result.Factory = new CreditControlCommandFactory(
            new CreditControlCommandIdentifierSource(),
            host,
            realm);
        result.Parser = new CreditControlCommandParser(
            new TestDiameterClock(
                new DateTimeOffset(
                    2026,
                    8,
                    29,
                    20,
                    0,
                    0,
                    TimeSpan.Zero),
                0));
        return result;
    }

    private static DiameterCreditControlRequest CreateMultipleRequest()
    {
        DiameterCreditControlRequest result =
            new DiameterCreditControlRequest();
        result.SessionId = "client.example;multiple;1";
        result.DestinationRealm.Value = "remote.example";
        result.ServiceContextId = "example.multiple";
        result.RequestType = DiameterCreditControlRequestType.Initial;
        result.MultipleServicesIndicator =
            DiameterMultipleServicesIndicator.Supported;
        result.MultipleServicesIndicatorPresent = true;
        return result;
    }

    private static DiameterCreditControlAnswer CreateMultipleAnswer(
        DiameterCreditControlRequest request,
        uint requestNumber)
    {
        DiameterCreditControlAnswer result =
            new DiameterCreditControlAnswer();
        result.SessionId = request.SessionId;
        result.RequestType = request.RequestType;
        result.RequestNumber = requestNumber;
        result.ResultCode = DiameterResultCodes.Success;
        return result;
    }

    private static DiameterMultipleServiceCredit CreateMultipleCredit(
        uint serviceIdentifier,
        uint ratingGroup)
    {
        DiameterMultipleServiceCredit result =
            new DiameterMultipleServiceCredit();
        result.ServiceIdentifiers.Add(serviceIdentifier);
        result.RatingGroup = ratingGroup;
        result.RatingGroupPresent = true;
        return result;
    }

    private static DiameterMultipleServiceCredit CreateGrantedTimeCredit(
        uint serviceIdentifier,
        uint ratingGroup,
        uint time)
    {
        DiameterMultipleServiceCredit result =
            CreateMultipleCredit(serviceIdentifier, ratingGroup);
        result.GrantedUnit = CreateTimeUnit(time);
        result.PoolReferences.Add(CreatePoolReference(
            77U,
            DiameterCreditControlUnitType.Time,
            1L,
            0));
        return result;
    }

    private static DiameterMultipleServiceCredit CreateGrantedOctetCredit(
        uint serviceIdentifier,
        uint ratingGroup,
        ulong octets)
    {
        DiameterMultipleServiceCredit result =
            CreateMultipleCredit(serviceIdentifier, ratingGroup);
        result.GrantedUnit = CreateOctetUnit(octets);
        result.PoolReferences.Add(CreatePoolReference(
            77U,
            DiameterCreditControlUnitType.Time,
            1L,
            0));
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

    private static DiameterPoolReference CreatePoolReference(
        uint identifier,
        DiameterCreditControlUnitType unitType,
        long valueDigits,
        int exponent)
    {
        DiameterPoolReference result = new DiameterPoolReference();
        result.PoolIdentifier = identifier;
        result.UnitType = unitType;
        result.ValueDigits = valueDigits;
        result.Exponent = exponent;
        result.ExponentPresent = true;
        return result;
    }
}

internal class CreditControlMultipleTestFixture
{
    internal CreditControlCommandFactory Factory = null!;
    internal CreditControlCommandParser Parser = null!;
}
