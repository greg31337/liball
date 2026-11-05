using System;
using System.Collections.Generic;
using System.Text;

namespace diameter.Tests;

public class PublicResultCodeTests
{
    [Test]
    public void ClassifierCoversEveryBuiltInFailureResultCode()
    {
        IDiameterValidator validator = new DiameterValidator();
        uint[] protocolCodes = new uint[]
        {
            DiameterResultCodes.CommandUnsupported,
            DiameterResultCodes.UnableToDeliver,
            DiameterResultCodes.RealmNotServed,
            DiameterResultCodes.TooBusy,
            DiameterResultCodes.LoopDetected,
            DiameterResultCodes.RedirectIndication,
            DiameterResultCodes.ApplicationUnsupported,
            DiameterResultCodes.InvalidHeaderBits,
            DiameterResultCodes.InvalidAvpBits,
            DiameterResultCodes.UnknownPeer,
            DiameterResultCodes.RealmRedirectIndication
        };
        uint[] transientCodes = new uint[]
        {
            DiameterResultCodes.AuthenticationRejected,
            DiameterResultCodes.OutOfSpace,
            DiameterResultCodes.ElectionLost,
            DiameterResultCodes.EndUserServiceDenied,
            DiameterResultCodes.CreditControlNotApplicable,
            DiameterResultCodes.CreditLimitReached
        };
        uint[] permanentCodes = new uint[]
        {
            DiameterResultCodes.AvpUnsupported,
            DiameterResultCodes.UnknownSessionId,
            DiameterResultCodes.AuthorizationRejected,
            DiameterResultCodes.InvalidAvpValue,
            DiameterResultCodes.MissingAvp,
            DiameterResultCodes.ResourcesExceeded,
            DiameterResultCodes.ContradictingAvps,
            DiameterResultCodes.AvpNotAllowed,
            DiameterResultCodes.AvpOccursTooManyTimes,
            DiameterResultCodes.NoCommonApplication,
            DiameterResultCodes.UnsupportedVersion,
            DiameterResultCodes.UnableToComply,
            DiameterResultCodes.InvalidBitInHeader,
            DiameterResultCodes.InvalidAvpLength,
            DiameterResultCodes.InvalidMessageLength,
            DiameterResultCodes.InvalidAvpBitCombination,
            DiameterResultCodes.NoCommonSecurity,
            DiameterResultCodes.UserUnknown,
            DiameterResultCodes.RatingFailed
        };

        AssertClass(validator, protocolCodes, DiameterResultClass.ProtocolError);
        AssertClass(validator, transientCodes, DiameterResultClass.TransientFailure);
        AssertClass(validator, permanentCodes, DiameterResultClass.PermanentFailure);
        Assert.Multiple(() =>
        {
            Assert.That(validator.ClassifyResultCode(DiameterResultCodes.MultiRoundAuth),
                Is.EqualTo(DiameterResultClass.Informational));
            Assert.That(validator.ClassifyResultCode(DiameterResultCodes.Success),
                Is.EqualTo(DiameterResultClass.Success));
            Assert.That(validator.ClassifyResultCode(DiameterResultCodes.LimitedSuccess),
                Is.EqualTo(DiameterResultClass.Success));
            Assert.That(validator.ClassifyResultCode(0U),
                Is.EqualTo(DiameterResultClass.PermanentFailure));
            Assert.That(validator.ClassifyResultCode(999U),
                Is.EqualTo(DiameterResultClass.PermanentFailure));
            Assert.That(validator.ClassifyResultCode(6000U),
                Is.EqualTo(DiameterResultClass.PermanentFailure));
            Assert.That(validator.ClassifyResultCode(uint.MaxValue),
                Is.EqualTo(DiameterResultClass.PermanentFailure));
        });
    }

    [Test]
    public void GeneralErrorBuilderConstructsEveryBuiltInFailureClass()
    {
        IDiameterValidator validator = new DiameterValidator();
        DiameterIdentity originHost = CreateIdentity("NODE.Example.");
        DiameterIdentity originRealm = CreateIdentity("EXAMPLE.");
        uint[] codes = CreateFailureCodes();
        int index = 0;
        while (index < codes.Length)
        {
            DiameterMessage request = CreateWatchdogRequest((uint)index + 1U);
            DiameterErrorAnswerRequest error = new DiameterErrorAnswerRequest();
            error.Request = request;
            error.ResultCode = codes[index];
            error.ErrorMessage = "failure";

            DiameterMessage answer = validator.BuildResultErrorAnswer(
                error,
                originHost,
                originRealm);
            DiameterResultClass resultClass = validator.ClassifyResultCode(codes[index]);
            DiameterMessageFlags expectedFlags = DiameterMessageFlags.None;
            if (resultClass == DiameterResultClass.ProtocolError)
            {
                expectedFlags = DiameterMessageFlags.Error;
            }

            Assert.Multiple(() =>
            {
                Assert.That(answer.Flags, Is.EqualTo(expectedFlags));
                Assert.That(answer.CommandCode, Is.EqualTo(request.CommandCode));
                Assert.That(answer.ApplicationId, Is.EqualTo(request.ApplicationId));
                Assert.That(answer.HopByHopIdentifier,
                    Is.EqualTo(request.HopByHopIdentifier));
                Assert.That(answer.EndToEndIdentifier,
                    Is.EqualTo(request.EndToEndIdentifier));
                Assert.That(ReadUnsigned32(answer, DiameterAvpCodes.ResultCode),
                    Is.EqualTo(codes[index]));
                Assert.That(Encoding.UTF8.GetString(
                    FindAvp(answer, DiameterAvpCodes.OriginHost).Data),
                    Is.EqualTo("node.example"));
                Assert.That(Encoding.UTF8.GetString(
                    FindAvp(answer, DiameterAvpCodes.OriginRealm).Data),
                    Is.EqualTo("example"));
                Assert.That(Encoding.UTF8.GetString(
                    FindAvp(answer, DiameterAvpCodes.ErrorMessage).Data),
                    Is.EqualTo("failure"));
                Assert.That(new DiameterMessageCodec().EncodeMessageToArray(answer).Status,
                    Is.EqualTo(DiameterCodecStatus.Success));
            });

            if (resultClass != DiameterResultClass.ProtocolError)
            {
                DiameterValidationResult validation = validator.ValidateMessage(
                    answer,
                    new DiameterDictionary().ReadDictionarySnapshot(),
                    new DiameterLimitConfiguration());
                Assert.That(validation.Status, Is.EqualTo(DiameterValidationStatus.Valid));
            }

            index = index + 1;
        }
    }

    [Test]
    public void GeneralErrorBuilderRejectsNonFailuresInvalidTextAndMismatchedDirection()
    {
        IDiameterValidator validator = new DiameterValidator();
        DiameterIdentity valid = CreateIdentity("node.example");
        DiameterErrorAnswerRequest success = new DiameterErrorAnswerRequest();
        success.Request = CreateWatchdogRequest(1U);
        success.ResultCode = DiameterResultCodes.Success;
        DiameterErrorAnswerRequest informational = new DiameterErrorAnswerRequest();
        informational.Request = CreateWatchdogRequest(2U);
        informational.ResultCode = DiameterResultCodes.MultiRoundAuth;
        DiameterErrorAnswerRequest unknownClass = new DiameterErrorAnswerRequest();
        unknownClass.Request = CreateWatchdogRequest(3U);
        unknownClass.ResultCode = 6001U;
        DiameterErrorAnswerRequest invalidText = new DiameterErrorAnswerRequest();
        invalidText.Request = CreateWatchdogRequest(4U);
        invalidText.ResultCode = DiameterResultCodes.UnableToComply;
        invalidText.ErrorMessage = "invalid\0text";
        DiameterErrorAnswerRequest answerSource = new DiameterErrorAnswerRequest();
        answerSource.Request = new DiameterMessage();
        answerSource.ResultCode = DiameterResultCodes.UnableToComply;

        Assert.Multiple(() =>
        {
            Assert.Throws<DiameterValidationException>(() =>
                validator.BuildResultErrorAnswer(success, valid, valid));
            Assert.Throws<DiameterValidationException>(() =>
                validator.BuildResultErrorAnswer(informational, valid, valid));
            Assert.Throws<DiameterValidationException>(() =>
                validator.BuildResultErrorAnswer(unknownClass, valid, valid));
            Assert.Throws<DiameterValidationException>(() =>
                validator.BuildResultErrorAnswer(invalidText, valid, valid));
            Assert.Throws<DiameterValidationException>(() =>
                validator.BuildResultErrorAnswer(answerSource, valid, valid));
        });
    }

    private static uint[] CreateFailureCodes()
    {
        return new uint[]
        {
            DiameterResultCodes.CommandUnsupported,
            DiameterResultCodes.UnableToDeliver,
            DiameterResultCodes.RealmNotServed,
            DiameterResultCodes.TooBusy,
            DiameterResultCodes.LoopDetected,
            DiameterResultCodes.RedirectIndication,
            DiameterResultCodes.ApplicationUnsupported,
            DiameterResultCodes.InvalidHeaderBits,
            DiameterResultCodes.InvalidAvpBits,
            DiameterResultCodes.UnknownPeer,
            DiameterResultCodes.RealmRedirectIndication,
            DiameterResultCodes.AuthenticationRejected,
            DiameterResultCodes.OutOfSpace,
            DiameterResultCodes.ElectionLost,
            DiameterResultCodes.EndUserServiceDenied,
            DiameterResultCodes.CreditControlNotApplicable,
            DiameterResultCodes.CreditLimitReached,
            DiameterResultCodes.AvpUnsupported,
            DiameterResultCodes.UnknownSessionId,
            DiameterResultCodes.AuthorizationRejected,
            DiameterResultCodes.InvalidAvpValue,
            DiameterResultCodes.MissingAvp,
            DiameterResultCodes.ResourcesExceeded,
            DiameterResultCodes.ContradictingAvps,
            DiameterResultCodes.AvpNotAllowed,
            DiameterResultCodes.AvpOccursTooManyTimes,
            DiameterResultCodes.NoCommonApplication,
            DiameterResultCodes.UnsupportedVersion,
            DiameterResultCodes.UnableToComply,
            DiameterResultCodes.InvalidBitInHeader,
            DiameterResultCodes.InvalidAvpLength,
            DiameterResultCodes.InvalidMessageLength,
            DiameterResultCodes.InvalidAvpBitCombination,
            DiameterResultCodes.NoCommonSecurity,
            DiameterResultCodes.UserUnknown,
            DiameterResultCodes.RatingFailed
        };
    }

    private static void AssertClass(
        IDiameterValidator validator,
        uint[] codes,
        DiameterResultClass expected)
    {
        int index = 0;
        while (index < codes.Length)
        {
            Assert.That(validator.ClassifyResultCode(codes[index]), Is.EqualTo(expected));
            index = index + 1;
        }
    }

    private static DiameterMessage CreateWatchdogRequest(uint identifier)
    {
        DiameterMessage request = new DiameterMessage();
        request.Flags = DiameterMessageFlags.Request;
        request.CommandCode = DiameterCommandCodes.DeviceWatchdog;
        request.ApplicationId = DiameterApplicationIds.Common;
        request.HopByHopIdentifier = identifier;
        request.EndToEndIdentifier = identifier + 100U;
        return request;
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity identity = new DiameterIdentity();
        identity.Value = value;
        return identity;
    }

    private static DiameterAvp FindAvp(DiameterMessage message, uint code)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.Code == code && avp.VendorId == 0U)
            {
                return avp;
            }

            index = index + 1;
        }

        throw new AssertionException($"AVP {code} was not found.");
    }

    private static uint ReadUnsigned32(DiameterMessage message, uint code)
    {
        DiameterValueResult decoded = new DiameterAvpCodec().DecodeUnsigned32(
            FindAvp(message, code).Data);
        Assert.That(decoded.Status, Is.EqualTo(DiameterCodecStatus.Success));
        return decoded.Unsigned32;
    }
}
