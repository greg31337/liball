using System.Collections.Generic;
using System.Text;

namespace diameter.Tests;

public class PublicValidatorTests
{
    [Test]
    public void ValidatorAcceptsACompleteBuiltInWatchdogRequest()
    {
        IDiameterDictionary dictionary = new DiameterDictionary();
        IDiameterValidator validator = new DiameterValidator();
        DiameterMessage message = CreateWatchdogRequest(true);

        DiameterValidationResult result = validator.ValidateMessage(
            message,
            dictionary.ReadDictionarySnapshot(),
            new DiameterLimitConfiguration());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.ErrorAnswer, Is.Null);
        });
    }

    [Test]
    public void ValidatorProducesStructuredMissingAvpAndProtocolCorrectAnswer()
    {
        IDiameterDictionary dictionary = new DiameterDictionary();
        IDiameterValidator validator = new DiameterValidator();
        DiameterMessage request = CreateWatchdogRequest(false);
        DiameterValidationResult validation = validator.ValidateMessage(
            request,
            dictionary.ReadDictionarySnapshot(),
            new DiameterLimitConfiguration());
        request.Flags = request.Flags | DiameterMessageFlags.Proxyable |
            DiameterMessageFlags.PotentiallyRetransmitted;
        request.Avps.Add(CreateAvp(
            DiameterAvpCodes.SessionId,
            DiameterAvpFlags.Mandatory,
            Encoding.UTF8.GetBytes("session.example;1")));
        DiameterAvp proxy = CreateAvp(
            DiameterAvpCodes.ProxyInfo,
            DiameterAvpFlags.Mandatory,
            CreateProxyInfoData());
        request.Avps.Add(proxy);
        DiameterIdentity host = new DiameterIdentity();
        host.Value = "NODE.EXAMPLE";
        DiameterIdentity realm = new DiameterIdentity();
        realm.Value = "EXAMPLE";
        DiameterMessage answer = validator.BuildValidationErrorAnswer(
            validation.ErrorAnswer,
            host,
            realm);
        DiameterAvp resultCode = FindAvp(answer, DiameterAvpCodes.ResultCode);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Status, Is.EqualTo(DiameterValidationStatus.Invalid));
            Assert.That(validation.Issues[0].Code, Is.EqualTo("missing-required-avp"));
            Assert.That(validation.ErrorAnswer!.ResultCode,
                Is.EqualTo(DiameterResultCodes.MissingAvp));
            Assert.That(answer.Version, Is.EqualTo(request.Version));
            Assert.That(answer.Flags, Is.EqualTo(DiameterMessageFlags.Proxyable));
            Assert.That(answer.CommandCode, Is.EqualTo(request.CommandCode));
            Assert.That(answer.ApplicationId, Is.EqualTo(request.ApplicationId));
            Assert.That(answer.HopByHopIdentifier, Is.EqualTo(request.HopByHopIdentifier));
            Assert.That(answer.EndToEndIdentifier, Is.EqualTo(request.EndToEndIdentifier));
            Assert.That(answer.Avps[0].Code, Is.EqualTo(DiameterAvpCodes.SessionId));
            Assert.That(new DiameterAvpCodec().DecodeUnsigned32(resultCode.Data).Unsigned32,
                Is.EqualTo(DiameterResultCodes.MissingAvp));
            Assert.That(Encoding.UTF8.GetString(
                FindAvp(answer, DiameterAvpCodes.OriginHost).Data), Is.EqualTo("node.example"));
            Assert.That(Encoding.UTF8.GetString(
                FindAvp(answer, DiameterAvpCodes.OriginRealm).Data), Is.EqualTo("example"));
            Assert.That(FindAvp(answer, DiameterAvpCodes.FailedAvp), Is.Not.Null);
            Assert.That(answer.Avps[^1].Code, Is.EqualTo(DiameterAvpCodes.ProxyInfo));
            Assert.That(answer.Avps[^1], Is.Not.SameAs(proxy));
        });
    }

    [Test]
    public void ProtocolFailureSetsErrorBitAndGroupedValidationUsesDictionaryRules()
    {
        IDiameterDictionary dictionary = new DiameterDictionary();
        IDiameterValidator validator = new DiameterValidator();
        DiameterMessage source = CreateWatchdogRequest(true);
        source.Flags = source.Flags | DiameterMessageFlags.Proxyable;
        DiameterErrorAnswerRequest error = new DiameterErrorAnswerRequest();
        error.Request = source;
        error.ResultCode = DiameterResultCodes.CommandUnsupported;
        DiameterIdentity host = new DiameterIdentity();
        host.Value = "node.example";
        DiameterIdentity realm = new DiameterIdentity();
        realm.Value = "example";
        DiameterMessage answer = validator.BuildValidationErrorAnswer(error, host, realm);
        DiameterAvp incompleteProxy = CreateAvp(
            DiameterAvpCodes.ProxyInfo,
            DiameterAvpFlags.Mandatory,
            CreateSingleGroupedAvpData(CreateAvp(
                DiameterAvpCodes.ProxyHost,
                DiameterAvpFlags.Mandatory,
                Encoding.UTF8.GetBytes("proxy.example"))));

        DiameterValidationResult grouped = validator.ValidateGroupedAvp(
            incompleteProxy,
            dictionary.ReadDictionarySnapshot(),
            new DiameterLimitConfiguration());

        Assert.Multiple(() =>
        {
            Assert.That(answer.Flags,
                Is.EqualTo(DiameterMessageFlags.Proxyable | DiameterMessageFlags.Error));
            Assert.That(grouped.Status, Is.EqualTo(DiameterValidationStatus.Invalid));
            Assert.That(grouped.Issues[0].Code, Is.EqualTo("missing-required-grouped-avp"));
            Assert.That(grouped.Issues[0].AvpCode, Is.EqualTo(DiameterAvpCodes.ProxyState));
        });
    }

    [Test]
    public void ErrorAnswerBuilderRejectsMissingInputAnswerMessagesAndInvalidIdentities()
    {
        IDiameterValidator validator = new DiameterValidator();
        DiameterIdentity valid = new DiameterIdentity();
        valid.Value = "node.example";
        DiameterIdentity invalid = new DiameterIdentity();
        invalid.Value = "bad identity";
        DiameterErrorAnswerRequest answerInput = new DiameterErrorAnswerRequest();
        answerInput.Request = new DiameterMessage();
        answerInput.ResultCode = DiameterResultCodes.UnableToComply;
        DiameterErrorAnswerRequest invalidIdentityInput = new DiameterErrorAnswerRequest();
        invalidIdentityInput.Request = CreateWatchdogRequest(true);
        invalidIdentityInput.ResultCode = DiameterResultCodes.UnableToComply;

        Assert.Multiple(() =>
        {
            Assert.That(() => validator.BuildValidationErrorAnswer(null, valid, valid),
                Throws.TypeOf<DiameterValidationException>());
            Assert.That(() => validator.BuildValidationErrorAnswer(answerInput, valid, valid),
                Throws.TypeOf<DiameterValidationException>());
            Assert.That(() => validator.BuildValidationErrorAnswer(
                invalidIdentityInput,
                invalid,
                valid), Throws.TypeOf<DiameterValidationException>());
        });
    }

    private static DiameterMessage CreateWatchdogRequest(bool includeRealm)
    {
        DiameterMessage message = new DiameterMessage();
        message.Flags = DiameterMessageFlags.Request;
        message.CommandCode = DiameterCommandCodes.DeviceWatchdog;
        message.ApplicationId = DiameterApplicationIds.Common;
        message.HopByHopIdentifier = 0x01020304U;
        message.EndToEndIdentifier = 0x05060708U;
        message.Avps.Add(CreateAvp(
            DiameterAvpCodes.OriginHost,
            DiameterAvpFlags.Mandatory,
            Encoding.UTF8.GetBytes("peer.example")));
        if (includeRealm)
        {
            message.Avps.Add(CreateAvp(
                DiameterAvpCodes.OriginRealm,
                DiameterAvpFlags.Mandatory,
                Encoding.UTF8.GetBytes("example")));
        }

        return message;
    }

    private static byte[] CreateProxyInfoData()
    {
        DiameterAvp proxyHost = CreateAvp(
            DiameterAvpCodes.ProxyHost,
            DiameterAvpFlags.Mandatory,
            Encoding.UTF8.GetBytes("proxy.example"));
        DiameterAvp proxyState = CreateAvp(
            DiameterAvpCodes.ProxyState,
            DiameterAvpFlags.Mandatory,
            new byte[] { 1, 2, 3 });
        List<DiameterAvp> avps = new List<DiameterAvp>();
        avps.Add(proxyHost);
        avps.Add(proxyState);
        return EncodeGroupedAvps(avps);
    }

    private static byte[] CreateSingleGroupedAvpData(DiameterAvp avp)
    {
        List<DiameterAvp> avps = new List<DiameterAvp>();
        avps.Add(avp);
        return EncodeGroupedAvps(avps);
    }

    private static byte[] EncodeGroupedAvps(List<DiameterAvp> avps)
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        DiameterLimitConfiguration limits = new DiameterLimitConfiguration();
        DiameterValueResult measurement = codec.MeasureGrouped(avps, null, limits);
        byte[] data = new byte[measurement.RequiredCount];
        DiameterValueResult encoded = codec.EncodeGrouped(data, avps, null, limits);
        Assert.That(encoded.Status, Is.EqualTo(DiameterCodecStatus.Success));
        return data;
    }

    private static DiameterAvp CreateAvp(
        uint code,
        DiameterAvpFlags flags,
        byte[] data)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = flags;
        avp.Data = data;
        return avp;
    }

    private static DiameterAvp FindAvp(DiameterMessage message, uint code)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (message.Avps[index].Code == code)
            {
                return message.Avps[index];
            }

            index = index + 1;
        }

        Assert.Fail("Expected AVP " + code + ".");
        return new DiameterAvp();
    }
}
