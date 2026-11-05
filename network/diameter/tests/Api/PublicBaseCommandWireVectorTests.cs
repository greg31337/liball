using System;
using System.Net;

namespace diameter.Tests;

public class PublicBaseCommandWireVectorTests
{
    [Test]
    public void EveryBaseCommandFactoryMatchesItsWireVectorAndDictionary()
    {
        AssertCapabilitiesExchangeVectors();
        AssertDeviceWatchdogVectors();
        AssertDisconnectPeerVectors();
        AssertReAuthVectors();
        AssertAbortSessionVectors();
        AssertSessionTerminationVectors();
        AssertAccountingVectors();
    }

    [Test]
    public void EveryErrorAnswerPathMatchesItsWireVectorAndDictionary()
    {
        IDiameterValidator validator = new DiameterValidator();
        DiameterIdentity originHost = CreateIdentity("n.io");
        DiameterIdentity originRealm = CreateIdentity("r.io");

        DiameterMessage detailedRequest = CreateErrorSourceRequest();
        detailedRequest.Avps.Add(CreateProxyInfo());
        DiameterErrorAnswerRequest detailedError = CreateStandardError(
            detailedRequest,
            DiameterResultCodes.UnableToDeliver);
        detailedError.ErrorMessage = "bad";
        detailedError.ErrorReportingHost = CreateIdentity("u.io");
        detailedError.FailedAvp = CreateFailedAvp();
        DiameterMessage detailedAnswer = validator.BuildResultErrorAnswer(
            detailedError,
            originHost,
            originRealm);
        AssertWireVectorAndDictionaryValid(
            detailedAnswer,
            "0100009460000102000000000102030405060708" +
            "000001074000000e6e2e696f3b310000" +
            "000001084000000c6e2e696f" +
            "000001284000000c722e696f" +
            "0000010c4000000c00000bba" +
            "000001190000000b62616400" +
            "000001260000000c752e696f" +
            "0000011740000014000001024000000c00000004" +
            "0000011c40000020000001184000000c702e696f" +
            "0000002140000009aa000000",
            "protocol Result-Code error");

        DiameterErrorAnswerRequest transientError = CreateStandardError(
            CreateErrorSourceRequest(),
            DiameterResultCodes.AuthenticationRejected);
        DiameterMessage transientAnswer = validator.BuildResultErrorAnswer(
            transientError,
            originHost,
            originRealm);
        AssertWireVectorAndDictionaryValid(
            transientAnswer,
            "0100004840000102000000000102030405060708" +
            "000001074000000e6e2e696f3b310000" +
            "000001084000000c6e2e696f" +
            "000001284000000c722e696f" +
            "0000010c4000000c00000fa1",
            "transient Result-Code error");

        DiameterErrorAnswerRequest permanentError = CreateStandardError(
            CreateErrorSourceRequest(),
            DiameterResultCodes.UnableToComply);
        DiameterMessage permanentAnswer = validator.BuildResultErrorAnswer(
            permanentError,
            originHost,
            originRealm);
        AssertWireVectorAndDictionaryValid(
            permanentAnswer,
            "0100004840000102000000000102030405060708" +
            "000001074000000e6e2e696f3b310000" +
            "000001084000000c6e2e696f" +
            "000001284000000c722e696f" +
            "0000010c4000000c00001394",
            "permanent Result-Code error");

        DiameterMessage validationAnswer = validator.BuildValidationErrorAnswer(
            permanentError,
            originHost,
            originRealm);
        AssertWireVectorAndDictionaryValid(
            validationAnswer,
            "0100004840000102000000000102030405060708" +
            "000001074000000e6e2e696f3b310000" +
            "000001084000000c6e2e696f" +
            "000001284000000c722e696f" +
            "0000010c4000000c00001394",
            "validation error");

        AssertExperimentalErrorVector(
            validator,
            originHost,
            originRealm,
            DiameterResultCodes.CommandUnsupported,
            "0100005c60000102000000000102030405060708" +
            "000001074000000e6e2e696f3b310000" +
            "000001084000000c6e2e696f" +
            "000001284000000c722e696f" +
            "00000129400000200000010a4000000c00000001" +
            "0000012a4000000c00000bb9",
            "protocol Experimental-Result error");
        AssertExperimentalErrorVector(
            validator,
            originHost,
            originRealm,
            DiameterResultCodes.AvpUnsupported,
            "0100005c40000102000000000102030405060708" +
            "000001074000000e6e2e696f3b310000" +
            "000001084000000c6e2e696f" +
            "000001284000000c722e696f" +
            "00000129400000200000010a4000000c00000001" +
            "0000012a4000000c00001389",
            "permanent Experimental-Result error");
    }

    private static void AssertCapabilitiesExchangeVectors()
    {
        IDiameterBaseCommandFactory factory = CreateFactory();
        DiameterBaseCommandOptions options = CreateOptions();
        DiameterMessage request = factory.CreateCapabilitiesExchangeRequest(options);
        AssertWireVectorAndDictionaryValid(
            request,
            "0100006c80000101000000000102030405060708" +
            "000001084000000c6e2e696f000001284000000c722e696f" +
            "000001014000000e0001c00002010000" +
            "0000010a4000000c000000010000010d0000000c74657374" +
            "000001164000000c0a0b0c0d0000010b0000000c00000002",
            "CER");

        options.Request = request;
        DiameterMessage answer = factory.CreateCapabilitiesExchangeAnswer(options);
        AssertWireVectorAndDictionaryValid(
            answer,
            "0100007800000101000000000102030405060708" +
            "0000010c4000000c000007d1" +
            "000001084000000c6e2e696f000001284000000c722e696f" +
            "000001014000000e0001c00002010000" +
            "0000010a4000000c000000010000010d0000000c74657374" +
            "000001164000000c0a0b0c0d0000010b0000000c00000002",
            "CEA");
    }

    private static void AssertDeviceWatchdogVectors()
    {
        IDiameterBaseCommandFactory factory = CreateFactory();
        DiameterBaseCommandOptions options = CreateOptions();
        DiameterMessage request = factory.CreateDeviceWatchdogRequest(options);
        AssertWireVectorAndDictionaryValid(
            request,
            "0100003880000118000000000102030405060708" +
            "000001084000000c6e2e696f000001284000000c722e696f" +
            "000001164000000c0a0b0c0d",
            "DWR");

        options.Request = request;
        DiameterMessage answer = factory.CreateDeviceWatchdogAnswer(options);
        AssertWireVectorAndDictionaryValid(
            answer,
            "0100004400000118000000000102030405060708" +
            "0000010c4000000c000007d1" +
            "000001084000000c6e2e696f000001284000000c722e696f" +
            "000001164000000c0a0b0c0d",
            "DWA");
    }

    private static void AssertDisconnectPeerVectors()
    {
        IDiameterBaseCommandFactory factory = CreateFactory();
        DiameterBaseCommandOptions options = CreateOptions();
        DiameterMessage request = factory.CreateDisconnectPeerRequest(options);
        AssertWireVectorAndDictionaryValid(
            request,
            "010000388000011a000000000102030405060708" +
            "000001084000000c6e2e696f000001284000000c722e696f" +
            "000001114000000c00000001",
            "DPR");

        options.Request = request;
        DiameterMessage answer = factory.CreateDisconnectPeerAnswer(options);
        AssertWireVectorAndDictionaryValid(
            answer,
            "010000380000011a000000000102030405060708" +
            "0000010c4000000c000007d1" +
            "000001084000000c6e2e696f000001284000000c722e696f",
            "DPA");
    }

    private static void AssertReAuthVectors()
    {
        IDiameterBaseCommandFactory factory = CreateFactory();
        DiameterBaseCommandOptions options = CreateOptions();
        DiameterMessage request = factory.CreateReAuthRequest(options);
        AssertWireVectorAndDictionaryValid(
            request,
            "01000078c0000102000000000102030405060708" +
            "000001074000000e6e2e696f3b310000" +
            "000001084000000c6e2e696f000001284000000c722e696f" +
            "0000011b4000000c722e696f000001254000000c642e696f" +
            "000001024000000c000000040000011d4000000c00000001" +
            "000001164000000c0a0b0c0d",
            "RAR");

        options.Request = request;
        DiameterMessage answer = factory.CreateReAuthAnswer(options);
        AssertSessionAnswerVector(answer, "0100005440000102", "RAA");
    }

    private static void AssertAbortSessionVectors()
    {
        IDiameterBaseCommandFactory factory = CreateFactory();
        DiameterBaseCommandOptions options = CreateOptions();
        DiameterMessage request = factory.CreateAbortSessionRequest(options);
        AssertWireVectorAndDictionaryValid(
            request,
            "0100006cc0000112000000000102030405060708" +
            "000001074000000e6e2e696f3b310000" +
            "000001084000000c6e2e696f000001284000000c722e696f" +
            "0000011b4000000c722e696f000001254000000c642e696f" +
            "000001024000000c00000004000001164000000c0a0b0c0d",
            "ASR");

        options.Request = request;
        DiameterMessage answer = factory.CreateAbortSessionAnswer(options);
        AssertSessionAnswerVector(answer, "0100005440000112", "ASA");
    }

    private static void AssertSessionTerminationVectors()
    {
        IDiameterBaseCommandFactory factory = CreateFactory();
        DiameterBaseCommandOptions options = CreateOptions();
        options.DestinationHost = null;
        DiameterMessage request = factory.CreateSessionTerminationRequest(options);
        AssertWireVectorAndDictionaryValid(
            request,
            "0100006cc0000113000000000102030405060708" +
            "000001074000000e6e2e696f3b310000" +
            "000001084000000c6e2e696f000001284000000c722e696f" +
            "0000011b4000000c722e696f000001024000000c00000004" +
            "000001274000000c00000004000001164000000c0a0b0c0d",
            "STR");

        options.Request = request;
        DiameterMessage answer = factory.CreateSessionTerminationAnswer(options);
        AssertSessionAnswerVector(answer, "0100005440000113", "STA");
    }

    private static void AssertAccountingVectors()
    {
        IDiameterBaseCommandFactory factory = CreateFactory();
        DiameterBaseCommandOptions options = CreateOptions();
        DiameterMessage request = factory.CreateAccountingRequest(options);
        AssertWireVectorAndDictionaryValid(
            request,
            "0100006cc000010f000000030102030405060708" +
            "000001074000000e6e2e696f3b310000" +
            "000001084000000c6e2e696f000001284000000c722e696f" +
            "0000011b4000000c722e696f000001e04000000c00000003" +
            "000001e54000000c00000009000001164000000c0a0b0c0d",
            "ACR");

        options.Request = request;
        DiameterMessage answer = factory.CreateAccountingAnswer(options);
        AssertWireVectorAndDictionaryValid(
            answer,
            "0100006c4000010f000000030102030405060708" +
            "000001074000000e6e2e696f3b310000" +
            "0000010c4000000c000007d1" +
            "000001084000000c6e2e696f000001284000000c722e696f" +
            "000001e04000000c00000003000001e54000000c00000009" +
            "000001164000000c0a0b0c0d",
            "ACA");
    }

    private static void AssertSessionAnswerVector(
        DiameterMessage answer,
        string header,
        string name)
    {
        AssertWireVectorAndDictionaryValid(
            answer,
            header + "000000000102030405060708" +
            "000001074000000e6e2e696f3b310000" +
            "0000010c4000000c000007d1" +
            "000001084000000c6e2e696f000001284000000c722e696f" +
            "000001164000000c0a0b0c0d",
            name);
    }

    private static void AssertExperimentalErrorVector(
        IDiameterValidator validator,
        DiameterIdentity originHost,
        DiameterIdentity originRealm,
        uint resultCode,
        string vector,
        string name)
    {
        DiameterErrorAnswerRequest error = new DiameterErrorAnswerRequest();
        error.Request = CreateErrorSourceRequest();
        error.ExperimentalResult = new DiameterExperimentalResult();
        error.ExperimentalResult.VendorId = 1U;
        error.ExperimentalResult.ResultCode = resultCode;
        DiameterMessage answer = validator.BuildResultErrorAnswer(
            error,
            originHost,
            originRealm);
        AssertWireVectorAndDictionaryValid(answer, vector, name);
    }

    private static IDiameterBaseCommandFactory CreateFactory()
    {
        return new DiameterBaseCommandFactory(new WireVectorIdentifierSource());
    }

    private static DiameterBaseCommandOptions CreateOptions()
    {
        DiameterBaseCommandOptions options = new DiameterBaseCommandOptions();
        options.OriginHost = CreateIdentity("n.io");
        options.OriginRealm = CreateIdentity("r.io");
        options.HostAddresses.Add(IPAddress.Parse("192.0.2.1"));
        options.VendorId = 1U;
        options.ProductName = "test";
        options.FirmwareRevision = 2U;
        options.DestinationHost = CreateIdentity("d.io");
        options.DestinationRealm = CreateIdentity("r.io");
        options.SessionId = "n.io;1";
        options.AuthApplicationId = 4U;
        options.DisconnectCause = DiameterDisconnectCause.Busy;
        options.ReAuthRequestType = DiameterReAuthRequestType.AuthorizeAuthenticate;
        options.TerminationCause = DiameterTerminationCause.Administrative;
        options.AccountingRecordType = DiameterAccountingRecordType.Interim;
        options.AccountingRecordNumber = 9U;
        return options;
    }

    private static DiameterErrorAnswerRequest CreateStandardError(
        DiameterMessage request,
        uint resultCode)
    {
        DiameterErrorAnswerRequest error = new DiameterErrorAnswerRequest();
        error.Request = request;
        error.ResultCode = resultCode;
        return error;
    }

    private static DiameterMessage CreateErrorSourceRequest()
    {
        DiameterMessage request = new DiameterMessage();
        request.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable |
            DiameterMessageFlags.PotentiallyRetransmitted;
        request.CommandCode = DiameterCommandCodes.ReAuth;
        request.HopByHopIdentifier = 0x01020304U;
        request.EndToEndIdentifier = 0x05060708U;
        request.Avps.Add(new DiameterBaseAvpFactory().CreateSessionIdAvp("n.io;1"));
        return request;
    }

    private static DiameterAvp CreateProxyInfo()
    {
        DiameterProxyInformation information = new DiameterProxyInformation();
        information.ProxyHost = CreateIdentity("p.io");
        information.ProxyState = new byte[] { 0xaa };
        return new DiameterBaseAvpFactory().CreateProxyInfoAvp(information);
    }

    private static DiameterAvp CreateFailedAvp()
    {
        DiameterAvp failed = new DiameterAvp();
        failed.Code = DiameterAvpCodes.FailedAvp;
        failed.Flags = DiameterAvpFlags.Mandatory;
        failed.Data = Convert.FromHexString("000001024000000c00000004");
        return failed;
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity identity = new DiameterIdentity();
        identity.Value = value;
        return identity;
    }

    private static void AssertWireVectorAndDictionaryValid(
        DiameterMessage message,
        string expectedHex,
        string name)
    {
        DiameterEncodeResult encoded = new DiameterMessageCodec().EncodeMessageToArray(message);
        DiameterDictionarySnapshot snapshot = new DiameterDictionary().ReadDictionarySnapshot();
        DiameterValidationResult validation = new DiameterValidator().ValidateMessage(
            message,
            snapshot,
            new DiameterLimitConfiguration());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Status, Is.EqualTo(DiameterCodecStatus.Success), name);
            Assert.That(encoded.Data, Is.EqualTo(Convert.FromHexString(expectedHex)), name);
            Assert.That(validation.Status, Is.EqualTo(DiameterValidationStatus.Valid),
                name + ": " + string.Join(" | ", validation.Issues.ConvertAll(
                    issue => issue.Code)));
        });
    }
}

public class WireVectorIdentifierSource : IDiameterIdentifierSource
{
    public uint HopByHopIdentifier = 0x01020303U;
    public uint EndToEndIdentifier = 0x05060707U;

    public uint CreateHopByHopIdentifier()
    {
        HopByHopIdentifier = HopByHopIdentifier + 1U;
        return HopByHopIdentifier;
    }

    public uint CreateEndToEndIdentifier()
    {
        EndToEndIdentifier = EndToEndIdentifier + 1U;
        return EndToEndIdentifier;
    }

    public string CreateSessionIdentifier(DiameterIdentity originHost, string? correlationText)
    {
        throw new NotSupportedException();
    }

    public uint CreateOriginStateIdentifier()
    {
        return 0x0a0b0c0dU;
    }
}
