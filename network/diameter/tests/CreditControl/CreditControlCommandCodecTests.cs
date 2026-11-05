using System;
using System.Linq;
using System.Text;

namespace diameter.Tests;

public class CreditControlCommandCodecTests
{
    [TestCase(DiameterCreditControlRequestType.Initial, 0U)]
    [TestCase(DiameterCreditControlRequestType.Update, 1U)]
    [TestCase(DiameterCreditControlRequestType.Termination, 2U)]
    [TestCase(DiameterCreditControlRequestType.Event, 0U)]
    public void EveryRequestTypeRoundTripsThroughDictionaryValidCcr(
        DiameterCreditControlRequestType requestType,
        uint requestNumber)
    {
        CreditControlCommandTestFixture fixture = CreateFixture();
        DiameterCreditControlRequest request = CreateRequest(
            requestType,
            requestNumber);

        DiameterMessage message = fixture.Factory
            .CreateCreditControlRequest(request);
        DiameterValidationResult validation = new DiameterValidator()
            .ValidateMessage(
                message,
                fixture.Dictionary.ReadDictionarySnapshot(),
                new DiameterLimitConfiguration());
        DiameterCreditControlRequest parsed = fixture.Parser
            .ParseCreditControlRequest(message);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Status,
                Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(message.CommandCode,
                Is.EqualTo(DiameterCommandCodes.CreditControl));
            Assert.That(message.ApplicationId,
                Is.EqualTo(DiameterApplicationIds.CreditControl));
            Assert.That(message.Flags,
                Is.EqualTo(
                    DiameterMessageFlags.Request |
                    DiameterMessageFlags.Proxyable));
            Assert.That(message.HopByHopIdentifier, Is.EqualTo(101U));
            Assert.That(message.EndToEndIdentifier, Is.EqualTo(201U));
            Assert.That(parsed.SessionId, Is.EqualTo(request.SessionId));
            Assert.That(parsed.ServiceContextId,
                Is.EqualTo(request.ServiceContextId));
            Assert.That(parsed.RequestType, Is.EqualTo(requestType));
            Assert.That(parsed.RequestNumber, Is.EqualTo(requestNumber));
            Assert.That(parsed.DestinationRealm.Value,
                Is.EqualTo("remote.example"));
            Assert.That(parsed.DestinationHost!.Value,
                Is.EqualTo("credit.remote.example"));
            Assert.That(parsed.UserName,
                Is.EqualTo("alice@remote.example"));
            Assert.That(parsed.CreditControlSubSessionId,
                Is.EqualTo(9UL));
            Assert.That(parsed.AccountingMultiSessionId,
                Is.EqualTo("multi-session-1"));
            Assert.That(parsed.OriginStateIdentifier,
                Is.EqualTo(77U));
            Assert.That(parsed.EventTimestamp,
                Is.EqualTo(request.EventTimestamp));
            Assert.That(parsed.ServiceIdentifiers,
                Is.EqualTo(new uint[] { 10U }));
            Assert.That(parsed.CorrelationId,
                Is.EqualTo(new byte[] { 4, 5, 6 }));
            Assert.That(parsed.ServiceAvps, Has.Count.EqualTo(1));
            Assert.That(parsed.ServiceAvps[0].VendorId,
                Is.EqualTo(9999U));
        });

        if (requestType == DiameterCreditControlRequestType.Event)
        {
            Assert.That(parsed.RequestedAction,
                Is.EqualTo(DiameterRequestedAction.DirectDebiting));
        }

        if (requestType == DiameterCreditControlRequestType.Termination)
        {
            Assert.That(parsed.TerminationCause,
                Is.EqualTo(DiameterTerminationCause.Logout));
        }
    }

    [Test]
    public void CorrelatedAnswerRoundTripsAndOwnsRawMessage()
    {
        CreditControlCommandTestFixture fixture = CreateFixture();
        DiameterCreditControlRequest request = CreateRequest(
            DiameterCreditControlRequestType.Update,
            3U);
        DiameterMessage requestMessage = fixture.Factory
            .CreateCreditControlRequest(request);
        DiameterCreditControlAnswer answer = CreateAnswer(request);

        DiameterMessage message = fixture.Factory
            .CreateCreditControlAnswer(requestMessage, answer);
        DiameterValidationResult validation = new DiameterValidator()
            .ValidateMessage(
                message,
                fixture.Dictionary.ReadDictionarySnapshot(),
                new DiameterLimitConfiguration());
        DiameterCreditControlAnswer parsed = fixture.Parser
            .ParseCreditControlAnswer(message, request);
        message.Avps[0].Data[0] = (byte)'X';

        Assert.Multiple(() =>
        {
            Assert.That(validation.Status,
                Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(message.Flags,
                Is.EqualTo(DiameterMessageFlags.Proxyable));
            Assert.That(message.HopByHopIdentifier,
                Is.EqualTo(requestMessage.HopByHopIdentifier));
            Assert.That(message.EndToEndIdentifier,
                Is.EqualTo(requestMessage.EndToEndIdentifier));
            Assert.That(parsed.SessionId, Is.EqualTo(request.SessionId));
            Assert.That(parsed.RequestType,
                Is.EqualTo(DiameterCreditControlRequestType.Update));
            Assert.That(parsed.RequestNumber, Is.EqualTo(3U));
            Assert.That(parsed.ResultCode,
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(parsed.UserName,
                Is.EqualTo("alice@remote.example"));
            Assert.That(parsed.SessionFailoverSupported, Is.True);
            Assert.That(parsed.CreditControlSubSessionId,
                Is.EqualTo(9UL));
            Assert.That(parsed.CheckBalanceResult,
                Is.EqualTo(DiameterCheckBalanceResult.EnoughCredit));
            Assert.That(parsed.FailureHandling,
                Is.EqualTo(
                    DiameterCreditControlFailureHandling.RetryAndTerminate));
            Assert.That(parsed.DirectDebitFailureHandling,
                Is.EqualTo(
                    DiameterDirectDebitFailureHandling.Continue));
            Assert.That(parsed.ValidityTimeMilliseconds,
                Is.EqualTo(5000));
            Assert.That(parsed.RedirectHosts, Has.Count.EqualTo(1));
            Assert.That(parsed.RedirectHosts[0].Host.Value,
                Is.EqualTo("redirect.credit.example"));
            Assert.That(parsed.RedirectHostUsagePresent, Is.True);
            Assert.That(parsed.RedirectHostUsage,
                Is.EqualTo(DiameterRedirectHostUsage.AllSession));
            Assert.That(parsed.RedirectMaxCacheTimePresent, Is.True);
            Assert.That(parsed.RedirectMaxCacheTimeMilliseconds,
                Is.EqualTo(30000));
            Assert.That(parsed.ServiceAvps, Has.Count.EqualTo(1));
            Assert.That(parsed.FailedAvps, Has.Count.EqualTo(1));
            Assert.That(parsed.FailedAvps[0].Code,
                Is.EqualTo(DiameterAvpCodes.FailedAvp));
            Assert.That(parsed.Message, Is.Not.Null);
            Assert.That(parsed.Message!.Avps[0].Data[0],
                Is.EqualTo((byte)'c'));
        });
    }

    [Test]
    public void RequestParserRejectsWrongHeadersMissingDuplicatesAndBadNumbers()
    {
        CreditControlCommandTestFixture fixture = CreateFixture();
        DiameterCreditControlRequest initial = CreateRequest(
            DiameterCreditControlRequestType.Initial,
            0U);
        DiameterMessage valid = fixture.Factory
            .CreateCreditControlRequest(initial);
        DiameterMessage wrongApplication = CloneMessage(valid);
        wrongApplication.ApplicationId = 3U;
        DiameterMessage missingSession = CloneMessage(valid);
        missingSession.Avps.RemoveAt(0);
        DiameterMessage duplicateType = CloneMessage(valid);
        duplicateType.Avps.Add(CloneAvp(FindAvp(
            valid,
            DiameterAvpCodes.CcRequestType)));
        DiameterMessage badAuthApplication = CloneMessage(valid);
        NetworkByteOrder.WriteUInt32(
            FindAvp(
                badAuthApplication,
                DiameterAvpCodes.AuthApplicationId).Data,
            3U);
        DiameterMessage updateNumberZero = CloneMessage(valid);
        NetworkByteOrder.WriteUInt32(
            FindAvp(
                updateNumberZero,
                DiameterAvpCodes.CcRequestType).Data,
            (uint)DiameterCreditControlRequestType.Update);
        DiameterCreditControlRequest invalidFactoryRequest =
            CreateRequest(
                DiameterCreditControlRequestType.Update,
                0U);

        Assert.Multiple(() =>
        {
            Assert.Throws<DiameterCreditControlException>(() =>
                fixture.Parser.ParseCreditControlRequest(
                    wrongApplication));
            Assert.Throws<DiameterCreditControlException>(() =>
                fixture.Parser.ParseCreditControlRequest(
                    missingSession));
            Assert.Throws<DiameterCreditControlException>(() =>
                fixture.Parser.ParseCreditControlRequest(
                    duplicateType));
            Assert.Throws<DiameterCreditControlException>(() =>
                fixture.Parser.ParseCreditControlRequest(
                    badAuthApplication));
            Assert.Throws<DiameterCreditControlException>(() =>
                fixture.Parser.ParseCreditControlRequest(
                    updateNumberZero));
            Assert.Throws<DiameterCreditControlException>(() =>
                fixture.Factory.CreateCreditControlRequest(
                    invalidFactoryRequest));
        });
    }

    [Test]
    public void AnswerFactoryAndParserRejectCorrelationMismatch()
    {
        CreditControlCommandTestFixture fixture = CreateFixture();
        DiameterCreditControlRequest request = CreateRequest(
            DiameterCreditControlRequestType.Initial,
            0U);
        DiameterMessage requestMessage = fixture.Factory
            .CreateCreditControlRequest(request);
        DiameterCreditControlAnswer answer = CreateAnswer(request);
        DiameterCreditControlAnswer wrongFactoryAnswer =
            CreateAnswer(request);
        wrongFactoryAnswer.RequestNumber = 1U;
        DiameterMessage answerMessage = fixture.Factory
            .CreateCreditControlAnswer(requestMessage, answer);
        DiameterMessage wrongSession = CloneMessage(answerMessage);
        FindAvp(wrongSession, DiameterAvpCodes.SessionId).Data =
            Encoding.UTF8.GetBytes("client.example;credit;other");
        DiameterMessage duplicateNumber = CloneMessage(answerMessage);
        duplicateNumber.Avps.Add(CloneAvp(FindAvp(
            answerMessage,
            DiameterAvpCodes.CcRequestNumber)));
        DiameterMessage duplicateRedirectUsage =
            CloneMessage(answerMessage);
        duplicateRedirectUsage.Avps.Add(CloneAvp(FindAvp(
            answerMessage,
            DiameterAvpCodes.RedirectHostUsage)));
        DiameterMessage invalidRedirectHost =
            CloneMessage(answerMessage);
        FindAvp(
            invalidRedirectHost,
            DiameterAvpCodes.RedirectHost).Data =
                Encoding.UTF8.GetBytes("http://redirect.credit.example");
        DiameterMessage oversizedRedirectCache =
            CloneMessage(answerMessage);
        NetworkByteOrder.WriteUInt32(
            FindAvp(
                oversizedRedirectCache,
                DiameterAvpCodes.RedirectMaxCacheTime).Data,
            uint.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.Throws<DiameterCreditControlException>(() =>
                fixture.Factory.CreateCreditControlAnswer(
                    requestMessage,
                    wrongFactoryAnswer));
            Assert.Throws<DiameterCreditControlException>(() =>
                fixture.Parser.ParseCreditControlAnswer(
                    wrongSession,
                    request));
            Assert.Throws<DiameterCreditControlException>(() =>
                fixture.Parser.ParseCreditControlAnswer(
                    duplicateNumber,
                    request));
            Assert.Throws<DiameterCreditControlException>(() =>
                fixture.Parser.ParseCreditControlAnswer(
                    duplicateRedirectUsage,
                    request));
            Assert.Throws<DiameterCreditControlException>(() =>
                fixture.Parser.ParseCreditControlAnswer(
                    invalidRedirectHost,
                    request));
            Assert.Throws<DiameterCreditControlException>(() =>
                fixture.Parser.ParseCreditControlAnswer(
                    oversizedRedirectCache,
                    request));
        });
    }

    private static CreditControlCommandTestFixture CreateFixture()
    {
        CreditControlCommandTestFixture result =
            new CreditControlCommandTestFixture();
        result.Clock = new TestDiameterClock(
            new DateTimeOffset(
                2026,
                8,
                29,
                12,
                0,
                0,
                TimeSpan.Zero),
            1000L);
        result.Factory = new CreditControlCommandFactory(
            new CreditControlCommandIdentifierSource(),
            CreateIdentity("client.example"),
            CreateIdentity("example"));
        result.Parser = new CreditControlCommandParser(result.Clock);
        result.Dictionary = new DiameterDictionary();
        return result;
    }

    private static DiameterCreditControlRequest CreateRequest(
        DiameterCreditControlRequestType requestType,
        uint requestNumber)
    {
        DiameterCreditControlRequest result =
            new DiameterCreditControlRequest();
        result.SessionId = "client.example;credit;1";
        result.DestinationRealm = CreateIdentity("remote.example");
        result.DestinationHost =
            CreateIdentity("credit.remote.example");
        result.ServiceContextId = "example.service";
        result.UserName = "alice@remote.example";
        result.RequestType = requestType;
        result.RequestNumber = requestNumber;
        result.CreditControlSubSessionId = 9UL;
        result.CreditControlSubSessionIdPresent = true;
        result.AccountingMultiSessionId = "multi-session-1";
        result.EventTimestamp = new DateTimeOffset(
            2026,
            8,
            29,
            12,
            0,
            0,
            TimeSpan.Zero);
        result.EventTimestampPresent = true;
        result.ServiceIdentifiers.Add(10U);
        result.CorrelationId = new byte[] { 4, 5, 6 };
        DiameterAvp extension = new DiameterAvp();
        extension.Code = 900U;
        extension.VendorId = 9999U;
        extension.Flags = DiameterAvpFlags.VendorSpecific;
        extension.Data = new byte[] { 7, 8 };
        result.ServiceAvps.Add(extension);
        if (requestType == DiameterCreditControlRequestType.Event)
        {
            result.RequestedActionPresent = true;
            result.RequestedAction =
                DiameterRequestedAction.DirectDebiting;
        }

        if (requestType == DiameterCreditControlRequestType.Termination)
        {
            result.TerminationCausePresent = true;
            result.TerminationCause = DiameterTerminationCause.Logout;
        }

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
        result.UserName = request.UserName;
        result.SessionFailoverPresent = true;
        result.SessionFailoverSupported = true;
        result.CreditControlSubSessionId = 9UL;
        result.CreditControlSubSessionIdPresent = true;
        result.AccountingMultiSessionId = "multi-session-1";
        result.EventTimestamp = request.EventTimestamp;
        result.EventTimestampPresent = true;
        result.CheckBalanceResultPresent = true;
        result.CheckBalanceResult =
            DiameterCheckBalanceResult.EnoughCredit;
        result.FailureHandlingPresent = true;
        result.FailureHandling =
            DiameterCreditControlFailureHandling.RetryAndTerminate;
        result.DirectDebitFailureHandlingPresent = true;
        result.DirectDebitFailureHandling =
            DiameterDirectDebitFailureHandling.Continue;
        result.ValidityTimePresent = true;
        result.ValidityTimeMilliseconds = 5000;
        DiameterUri redirect = new DiameterUri();
        redirect.Host.Value = "redirect.credit.example";
        redirect.Transport = "tcp";
        result.RedirectHosts.Add(redirect);
        result.RedirectHostUsagePresent = true;
        result.RedirectHostUsage =
            DiameterRedirectHostUsage.AllSession;
        result.RedirectMaxCacheTimePresent = true;
        result.RedirectMaxCacheTimeMilliseconds = 30000;
        List<DiameterAvp> failedChildren = new List<DiameterAvp>();
        DiameterAvp failedChild = new DiameterAvp();
        failedChild.Code = DiameterAvpCodes.CcTime;
        failedChild.Flags = DiameterAvpFlags.Mandatory;
        failedChild.Data = new byte[4];
        failedChildren.Add(failedChild);
        result.FailedAvps.Add(CreditControlCommandCodec
            .CreateCreditControlGroupedAvp(
                DiameterAvpCodes.FailedAvp,
                failedChildren));
        DiameterAvp extension = new DiameterAvp();
        extension.Code = 901U;
        extension.VendorId = 9999U;
        extension.Flags = DiameterAvpFlags.VendorSpecific;
        extension.Data = new byte[] { 9, 10 };
        result.ServiceAvps.Add(extension);
        return result;
    }

    private static DiameterAvp FindAvp(
        DiameterMessage message,
        uint code)
    {
        return message.Avps.Single(avp =>
            avp.Code == code &&
            avp.VendorId == DiameterVendorIds.Ietf);
    }

    private static DiameterMessage CloneMessage(DiameterMessage source)
    {
        return CreditControlCommandCodec.CloneCreditControlMessage(source);
    }

    private static DiameterAvp CloneAvp(DiameterAvp source)
    {
        return CreditControlCommandCodec.CloneCreditControlAvp(source);
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }
}

internal class CreditControlCommandTestFixture
{
    internal TestDiameterClock Clock = null!;
    internal CreditControlCommandFactory Factory = null!;
    internal CreditControlCommandParser Parser = null!;
    internal DiameterDictionary Dictionary = null!;
}

internal class CreditControlCommandIdentifierSource :
    IDiameterIdentifierSource
{
    public uint CreateHopByHopIdentifier()
    {
        return 101U;
    }

    public uint CreateEndToEndIdentifier()
    {
        return 201U;
    }

    public string CreateSessionIdentifier(
        DiameterIdentity originHost,
        string? correlationText)
    {
        return originHost.Value + ";credit;1";
    }

    public uint CreateOriginStateIdentifier()
    {
        return 77U;
    }
}
