using System;
using System.Linq;

namespace diameter.Tests;

public class CreditControlFinalUnitTests
{
    [Test]
    public void LegacyRedirectAndQosRestrictionRoundTripInAnswer()
    {
        CreditControlFinalUnitFixture fixture = CreateFixture();
        DiameterCreditControlRequest request = CreateRequest(
            DiameterCreditControlRequestType.Initial);
        DiameterMessage requestMessage = fixture.Factory
            .CreateCreditControlRequest(request);
        DiameterCreditControlAnswer answer = CreateAnswer(request);
        answer.FinalUnitInstruction = CreateLegacyRedirectInstruction();
        answer.QosFinalUnitInstruction = CreateQosRestrictionInstruction();

        DiameterMessage message = fixture.Factory
            .CreateCreditControlAnswer(requestMessage, answer);
        DiameterValidationResult validation = new DiameterValidator()
            .ValidateMessage(
                message,
                new DiameterDictionary().ReadDictionarySnapshot(),
                new DiameterLimitConfiguration());
        DiameterCreditControlAnswer parsed = fixture.Parser
            .ParseCreditControlAnswer(message, request);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Status,
                Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(parsed.FinalUnitInstruction, Is.Not.Null);
            Assert.That(parsed.FinalUnitInstruction!.Action,
                Is.EqualTo(DiameterFinalUnitAction.Redirect));
            Assert.That(parsed.FinalUnitInstruction.RedirectAddressType,
                Is.EqualTo(DiameterRedirectAddressType.Url));
            Assert.That(parsed.FinalUnitInstruction.RedirectAddress,
                Is.EqualTo("https://legacy.example/final"));
            Assert.That(parsed.FinalUnitInstruction.RedirectExtension,
                Is.False);
            Assert.That(parsed.QosFinalUnitInstruction, Is.Not.Null);
            Assert.That(parsed.QosFinalUnitInstruction!.Action,
                Is.EqualTo(DiameterFinalUnitAction.RestrictAccess));
            Assert.That(parsed.QosFinalUnitInstruction.Qos, Is.True);
            Assert.That(
                parsed.QosFinalUnitInstruction.RestrictionFilterRules,
                Is.EqualTo(new string[] {
                    "tag in ip from any to assigned DSCP 46"
                }));
            Assert.That(parsed.QosFinalUnitInstruction.FilterIdentifiers,
                Is.EqualTo(new string[] { "subscriber-restricted" }));
        });
    }

    [Test]
    public void LegacyRestrictionFiltersRoundTrip()
    {
        CreditControlFinalUnitFixture fixture = CreateFixture();
        DiameterFinalUnitInstruction instruction =
            new DiameterFinalUnitInstruction();
        instruction.Action = DiameterFinalUnitAction.RestrictAccess;
        instruction.RestrictionFilterRules.Add(
            "permit out ip from any to assigned");
        instruction.FilterIdentifiers.Add("legacy-restricted");

        DiameterAvp encoded = CreditControlCommandCodec
            .CreateCreditControlFinalUnitAvp(instruction);
        DiameterFinalUnitInstruction parsed = fixture.Parser
            .ReadCreditControlFinalUnitAvp(encoded, false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Code,
                Is.EqualTo(DiameterAvpCodes.FinalUnitIndication));
            Assert.That(parsed.Action,
                Is.EqualTo(DiameterFinalUnitAction.RestrictAccess));
            Assert.That(parsed.RestrictionFilterRules,
                Is.EqualTo(new string[] {
                    "permit out ip from any to assigned"
                }));
            Assert.That(parsed.FilterIdentifiers,
                Is.EqualTo(new string[] { "legacy-restricted" }));
            Assert.That(parsed.Qos, Is.False);
        });
    }

    [TestCase("192.0.2.25", DiameterAvpCodes.RedirectAddressIpAddress)]
    [TestCase(
        "https://portal.example/final",
        DiameterAvpCodes.RedirectAddressUrl)]
    [TestCase(
        "sip:assistance@example.net",
        DiameterAvpCodes.RedirectAddressSipUri)]
    public void ExtensionRedirectFormsRoundTrip(
        string address,
        uint expectedAddressCode)
    {
        CreditControlFinalUnitFixture fixture = CreateFixture();
        DiameterFinalUnitInstruction instruction =
            new DiameterFinalUnitInstruction();
        instruction.Action = DiameterFinalUnitAction.Redirect;
        instruction.RedirectAddress = address;
        instruction.RedirectExtension = true;
        instruction.Qos = true;

        DiameterAvp encoded = CreditControlCommandCodec
            .CreateCreditControlFinalUnitAvp(instruction);
        DiameterValueResult outer = AvpDecoder
            .DecodeAvpSequence(encoded.Data);
        DiameterAvp redirect = outer.Avps.Single(avp =>
            avp.Code == DiameterAvpCodes.RedirectServerExtension);
        DiameterValueResult redirectChildren = AvpDecoder
            .DecodeAvpSequence(redirect.Data);
        DiameterFinalUnitInstruction parsed = fixture.Parser
            .ReadCreditControlFinalUnitAvp(encoded, true);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Code,
                Is.EqualTo(DiameterAvpCodes.QosFinalUnitIndication));
            Assert.That(redirectChildren.Avps, Has.Count.EqualTo(1));
            Assert.That(redirectChildren.Avps[0].Code,
                Is.EqualTo(expectedAddressCode));
            Assert.That(parsed.Action,
                Is.EqualTo(DiameterFinalUnitAction.Redirect));
            Assert.That(parsed.RedirectAddress, Is.EqualTo(address));
            Assert.That(parsed.RedirectExtension, Is.True);
            Assert.That(parsed.Qos, Is.True);
        });
    }

    [Test]
    public void TerminateActionRequestsGracefulTerminationAfterExhaustion()
    {
        TestDiameterClock clock = CreateClock();
        TestCreditControlStore store = new TestCreditControlStore();
        CreditControlClientStateMachine machine =
            new CreditControlClientStateMachine(store, clock);
        DiameterCreditControlRequest initial = CreateRequest(
            DiameterCreditControlRequestType.Initial);
        machine.BeginCreditControlClientSession(initial, 501);
        DiameterCreditControlAnswer answer = CreateAnswer(initial);
        answer.GrantedServiceUnit = CreateTimeUnit(10U);
        answer.FinalUnitInstruction = new DiameterFinalUnitInstruction();
        answer.FinalUnitInstruction.Action =
            DiameterFinalUnitAction.Terminate;

        DiameterCreditControlResult granted = machine
            .ProcessCreditControlClientAnswer(501, answer);
        DiameterCreditControlRequest update = CreateRequest(
            DiameterCreditControlRequestType.Update);
        update.UsedServiceUnits.Add(CreateTimeUnit(10U));
        DiameterCreditControlResult exhausted = machine
            .UpdateCreditControlClientSession(update, 502);

        Assert.Multiple(() =>
        {
            Assert.That(granted.FinalUnit, Is.True);
            Assert.That(granted.Disposition,
                Is.EqualTo(DiameterCreditControlDisposition.FinalUnit));
            Assert.That(granted.ContinueService, Is.True);
            Assert.That(granted.TerminateService, Is.False);
            Assert.That(exhausted.QuotaExhausted, Is.True);
            Assert.That(exhausted.AppliedFinalUnitInstruction, Is.Not.Null);
            Assert.That(exhausted.AppliedFinalUnitInstruction!.Action,
                Is.EqualTo(DiameterFinalUnitAction.Terminate));
            Assert.That(exhausted.GracefulTerminationRequired, Is.True);
            Assert.That(exhausted.ContinueService, Is.False);
            Assert.That(exhausted.TerminateService, Is.True);
            Assert.That(exhausted.State,
                Is.EqualTo(DiameterCreditControlState.PendingUpdate));
        });
    }

    [Test]
    public void QosRestrictionKeepsServiceAvailableAfterGroupExhaustion()
    {
        DiameterCreditControlSession session =
            new DiameterCreditControlSession();
        session.SessionId = "client.example;final-unit;group";
        session.State = DiameterCreditControlState.Open;
        DiameterMultipleServiceCredit credit =
            new DiameterMultipleServiceCredit();
        credit.ServiceIdentifiers.Add(7U);
        credit.GrantedUnit = CreateTimeUnit(5U);
        credit.UsedUnits.Add(CreateTimeUnit(5U));
        credit.QosFinalUnitInstruction =
            CreateQosRestrictionInstruction();
        session.MultipleServiceCredits.Add(credit);
        DiameterCreditControlResult result =
            new DiameterCreditControlResult();

        CreditControlQuotaState.ApplyCreditControlUsagePolicy(session);
        CreditControlFinalUnitState
            .ApplyCreditControlFinalUnitUsagePolicy(session);
        CreditControlFinalUnitState.PopulateCreditControlFinalUnitResult(
            result,
            session);

        Assert.Multiple(() =>
        {
            Assert.That(result.FinalUnit, Is.True);
            Assert.That(result.AppliedFinalUnitInstruction, Is.Not.Null);
            Assert.That(result.AppliedFinalUnitInstruction!.Action,
                Is.EqualTo(DiameterFinalUnitAction.RestrictAccess));
            Assert.That(result.AppliedFinalUnitInstruction.Qos, Is.True);
            Assert.That(result.ContinueService, Is.True);
            Assert.That(result.TerminateService, Is.False);
            Assert.That(result.GracefulTerminationRequired, Is.False);
        });
    }

    private static CreditControlFinalUnitFixture CreateFixture()
    {
        CreditControlFinalUnitFixture result =
            new CreditControlFinalUnitFixture();
        DiameterIdentity host = new DiameterIdentity();
        host.Value = "client.example";
        DiameterIdentity realm = new DiameterIdentity();
        realm.Value = "example";
        result.Factory = new CreditControlCommandFactory(
            new CreditControlCommandIdentifierSource(),
            host,
            realm);
        result.Parser = new CreditControlCommandParser(CreateClock());
        return result;
    }

    private static TestDiameterClock CreateClock()
    {
        return new TestDiameterClock(
            new DateTimeOffset(
                2026,
                8,
                29,
                20,
                0,
                0,
                TimeSpan.Zero),
            0);
    }

    private static DiameterCreditControlRequest CreateRequest(
        DiameterCreditControlRequestType type)
    {
        DiameterCreditControlRequest result =
            new DiameterCreditControlRequest();
        result.SessionId = "client.example;final-unit;1";
        result.DestinationRealm.Value = "remote.example";
        result.ServiceContextId = "example.final-unit";
        result.RequestType = type;
        return result;
    }

    private static DiameterCreditControlAnswer CreateAnswer(
        DiameterCreditControlRequest request)
    {
        DiameterCreditControlAnswer result =
            new DiameterCreditControlAnswer();
        result.SessionId = request.SessionId;
        result.RequestType = request.RequestType;
        result.RequestNumber = request.RequestNumber;
        result.ResultCode = DiameterResultCodes.Success;
        return result;
    }

    private static DiameterFinalUnitInstruction
        CreateLegacyRedirectInstruction()
    {
        DiameterFinalUnitInstruction result =
            new DiameterFinalUnitInstruction();
        result.Action = DiameterFinalUnitAction.Redirect;
        result.RedirectAddressType = DiameterRedirectAddressType.Url;
        result.RedirectAddressTypePresent = true;
        result.RedirectAddress = "https://legacy.example/final";
        return result;
    }

    private static DiameterFinalUnitInstruction
        CreateQosRestrictionInstruction()
    {
        DiameterFinalUnitInstruction result =
            new DiameterFinalUnitInstruction();
        result.Action = DiameterFinalUnitAction.RestrictAccess;
        result.Qos = true;
        result.RestrictionFilterRules.Add(
            "tag in ip from any to assigned DSCP 46");
        result.FilterIdentifiers.Add("subscriber-restricted");
        return result;
    }

    private static DiameterServiceUnit CreateTimeUnit(uint time)
    {
        DiameterServiceUnit result = new DiameterServiceUnit();
        result.Time = time;
        result.TimePresent = true;
        return result;
    }
}

internal class CreditControlFinalUnitFixture
{
    internal CreditControlCommandFactory Factory = null!;
    internal CreditControlCommandParser Parser = null!;
}
