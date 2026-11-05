using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace diameter.Tests;

public class PublicBaseCommandFactoryTests
{
    [Test]
    public void FactoryCreatesEveryDictionaryValidBaseRequest()
    {
        BaseCommandTestIdentifierSource identifiers = new BaseCommandTestIdentifierSource();
        IDiameterBaseCommandFactory factory = new DiameterBaseCommandFactory(identifiers);
        DiameterBaseCommandOptions options = CreateOptions();

        DiameterMessage cer = factory.CreateCapabilitiesExchangeRequest(options);
        DiameterMessage dwr = factory.CreateDeviceWatchdogRequest(options);
        DiameterMessage dpr = factory.CreateDisconnectPeerRequest(options);
        DiameterMessage rar = factory.CreateReAuthRequest(options);
        DiameterMessage asr = factory.CreateAbortSessionRequest(options);
        DiameterMessage str = factory.CreateSessionTerminationRequest(options);
        DiameterMessage acr = factory.CreateAccountingRequest(options);

        List<DiameterMessage> requests = new List<DiameterMessage>
        {
            cer, dwr, dpr, rar, asr, str, acr
        };
        int index = 0;
        while (index < requests.Count)
        {
            AssertValidAndEncodable(requests[index]);
            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            AssertRequestHeader(cer, DiameterCommandCodes.CapabilitiesExchange, 0U, false);
            AssertRequestHeader(dwr, DiameterCommandCodes.DeviceWatchdog, 0U, false);
            AssertRequestHeader(dpr, DiameterCommandCodes.DisconnectPeer, 0U, false);
            AssertRequestHeader(rar, DiameterCommandCodes.ReAuth, 0U, true);
            AssertRequestHeader(asr, DiameterCommandCodes.AbortSession, 0U, true);
            AssertRequestHeader(str, DiameterCommandCodes.SessionTermination, 0U, true);
            AssertRequestHeader(acr, DiameterCommandCodes.Accounting, 3U, true);
            Assert.That(cer.HopByHopIdentifier, Is.EqualTo(101U));
            Assert.That(acr.HopByHopIdentifier, Is.EqualTo(107U));
            Assert.That(cer.EndToEndIdentifier, Is.EqualTo(201U));
            Assert.That(acr.EndToEndIdentifier, Is.EqualTo(207U));
            Assert.That(cer.Avps.Count, Is.EqualTo(10));
            Assert.That(Encoding.UTF8.GetString(
                FindAvp(cer, DiameterAvpCodes.OriginHost).Data), Is.EqualTo("node.example"));
            Assert.That(ReadUnsigned32(cer, DiameterAvpCodes.VendorId), Is.EqualTo(10415U));
            Assert.That(ReadUnsigned32(dpr, DiameterAvpCodes.DisconnectCause), Is.EqualTo(1U));
            Assert.That(rar.Avps[0].Code, Is.EqualTo(DiameterAvpCodes.SessionId));
            Assert.That(ReadUnsigned32(rar, DiameterAvpCodes.ReAuthRequestType), Is.EqualTo(1U));
            Assert.That(ReadUnsigned32(str, DiameterAvpCodes.TerminationCause), Is.EqualTo(4U));
            Assert.That(ReadUnsigned32(acr, DiameterAvpCodes.AccountingRecordType), Is.EqualTo(3U));
            Assert.That(ReadUnsigned32(acr, DiameterAvpCodes.AccountingRecordNumber), Is.EqualTo(9U));
        });
    }

    [Test]
    public void FactoryCreatesEveryDictionaryValidBaseAnswerAndCopiesCorrelationValues()
    {
        BaseCommandTestIdentifierSource identifiers = new BaseCommandTestIdentifierSource();
        IDiameterBaseCommandFactory factory = new DiameterBaseCommandFactory(identifiers);
        DiameterBaseCommandOptions options = CreateOptions();

        DiameterMessage cer = factory.CreateCapabilitiesExchangeRequest(options);
        DiameterMessage dwr = factory.CreateDeviceWatchdogRequest(options);
        DiameterMessage dpr = factory.CreateDisconnectPeerRequest(options);
        DiameterMessage rar = factory.CreateReAuthRequest(options);
        DiameterMessage asr = factory.CreateAbortSessionRequest(options);
        DiameterMessage str = factory.CreateSessionTerminationRequest(options);
        DiameterMessage acr = factory.CreateAccountingRequest(options);

        options.Request = cer;
        DiameterMessage cea = factory.CreateCapabilitiesExchangeAnswer(options);
        options.Request = dwr;
        DiameterMessage dwa = factory.CreateDeviceWatchdogAnswer(options);
        options.Request = dpr;
        DiameterMessage dpa = factory.CreateDisconnectPeerAnswer(options);
        options.Request = rar;
        DiameterMessage raa = factory.CreateReAuthAnswer(options);
        options.Request = asr;
        DiameterMessage asa = factory.CreateAbortSessionAnswer(options);
        options.Request = str;
        DiameterMessage sta = factory.CreateSessionTerminationAnswer(options);
        options.Request = acr;
        DiameterMessage aca = factory.CreateAccountingAnswer(options);

        List<DiameterMessage> answers = new List<DiameterMessage>
        {
            cea, dwa, dpa, raa, asa, sta, aca
        };
        int index = 0;
        while (index < answers.Count)
        {
            AssertValidAndEncodable(answers[index]);
            Assert.That(ReadUnsigned32(answers[index], DiameterAvpCodes.ResultCode),
                Is.EqualTo(DiameterResultCodes.Success));
            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            AssertAnswerCorrelation(cer, cea, false);
            AssertAnswerCorrelation(dwr, dwa, false);
            AssertAnswerCorrelation(dpr, dpa, false);
            AssertAnswerCorrelation(rar, raa, true);
            AssertAnswerCorrelation(asr, asa, true);
            AssertAnswerCorrelation(str, sta, true);
            AssertAnswerCorrelation(acr, aca, true);
            Assert.That(raa.Avps[0].Code, Is.EqualTo(DiameterAvpCodes.SessionId));
            Assert.That(raa.Avps[0], Is.Not.SameAs(rar.Avps[0]));
            Assert.That(raa.Avps[0].Data, Is.Not.SameAs(rar.Avps[0].Data));
            Assert.That(ReadUnsigned32(aca, DiameterAvpCodes.AccountingRecordType),
                Is.EqualTo(3U));
            Assert.That(ReadUnsigned32(aca, DiameterAvpCodes.AccountingRecordNumber),
                Is.EqualTo(9U));
        });
    }

    [Test]
    public void FactoryRejectsInvalidRequiredInputsAndMismatchedAnswerRequests()
    {
        BaseCommandTestIdentifierSource identifiers = new BaseCommandTestIdentifierSource();
        IDiameterBaseCommandFactory factory = new DiameterBaseCommandFactory(identifiers);
        DiameterBaseCommandOptions invalidOrigin = CreateOptions();
        invalidOrigin.OriginHost.Value = "bad_name.example";
        DiameterBaseCommandOptions missingAddress = CreateOptions();
        missingAddress.HostAddresses.Clear();
        DiameterBaseCommandOptions missingDestination = CreateOptions();
        missingDestination.DestinationHost = null;
        DiameterBaseCommandOptions invalidDisconnect = CreateOptions();
        invalidDisconnect.DisconnectCause = (DiameterDisconnectCause)99;
        DiameterBaseCommandOptions invalidTermination = CreateOptions();
        invalidTermination.TerminationCause = (DiameterTerminationCause)0;
        DiameterBaseCommandOptions invalidAccounting = CreateOptions();
        invalidAccounting.AccountingRecordType = (DiameterAccountingRecordType)0;
        DiameterBaseCommandOptions missingRequest = CreateOptions();
        DiameterBaseCommandOptions wrongRequest = CreateOptions();
        wrongRequest.Request = factory.CreateDeviceWatchdogRequest(CreateOptions());

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => new DiameterBaseCommandFactory(null!));
            Assert.Throws<ArgumentException>(() => factory.CreateDeviceWatchdogRequest(null));
            Assert.Throws<ArgumentException>(() =>
                factory.CreateDeviceWatchdogRequest(invalidOrigin));
            Assert.Throws<ArgumentException>(() =>
                factory.CreateCapabilitiesExchangeRequest(missingAddress));
            Assert.Throws<ArgumentException>(() =>
                factory.CreateReAuthRequest(missingDestination));
            Assert.Throws<ArgumentException>(() =>
                factory.CreateDisconnectPeerRequest(invalidDisconnect));
            Assert.Throws<ArgumentException>(() =>
                factory.CreateSessionTerminationRequest(invalidTermination));
            Assert.Throws<ArgumentException>(() =>
                factory.CreateAccountingRequest(invalidAccounting));
            Assert.Throws<ArgumentException>(() =>
                factory.CreateDeviceWatchdogAnswer(missingRequest));
            Assert.Throws<ArgumentException>(() =>
                factory.CreateAccountingAnswer(wrongRequest));
        });
    }

    [Test]
    public void FactoryRejectsEveryCapabilityAndReAuthBoundary()
    {
        BaseCommandTestIdentifierSource identifiers =
            new BaseCommandTestIdentifierSource();
        IDiameterBaseCommandFactory factory =
            new DiameterBaseCommandFactory(identifiers);

        DiameterBaseCommandOptions lowDisconnect = CreateOptions();
        lowDisconnect.DisconnectCause = (DiameterDisconnectCause)(-1);
        DiameterBaseCommandOptions lowReAuth = CreateOptions();
        lowReAuth.ReAuthRequestType = (DiameterReAuthRequestType)(-1);
        DiameterBaseCommandOptions highReAuth = CreateOptions();
        highReAuth.ReAuthRequestType = (DiameterReAuthRequestType)2;
        DiameterBaseCommandOptions invalidReAuthApplication =
            CreateOptions();
        invalidReAuthApplication.CommandApplicationId = 9U;

        List<DiameterBaseCommandOptions> baseSelectors =
            new List<DiameterBaseCommandOptions>();
        DiameterBaseCommandOptions subSession = CreateOptions();
        subSession.CreditControlSubSessionIdPresent = true;
        baseSelectors.Add(subSession);
        DiameterBaseCommandOptions pool = CreateOptions();
        pool.GsuPoolIdentifierPresent = true;
        baseSelectors.Add(pool);
        DiameterBaseCommandOptions service = CreateOptions();
        service.ServiceIdentifierPresent = true;
        baseSelectors.Add(service);
        DiameterBaseCommandOptions rating = CreateOptions();
        rating.RatingGroupPresent = true;
        baseSelectors.Add(rating);

        DiameterBaseCommandOptions wrongCreditAuth = CreateOptions();
        wrongCreditAuth.CommandApplicationId =
            DiameterApplicationIds.CreditControl;
        wrongCreditAuth.AuthApplicationId = 9U;

        List<DiameterBaseCommandOptions> capabilities =
            new List<DiameterBaseCommandOptions>();
        DiameterBaseCommandOptions nullAddresses = CreateOptions();
        nullAddresses.HostAddresses = null!;
        capabilities.Add(nullAddresses);
        DiameterBaseCommandOptions nullVendors = CreateOptions();
        nullVendors.SupportedVendorIds = null!;
        capabilities.Add(nullVendors);
        DiameterBaseCommandOptions nullAuth = CreateOptions();
        nullAuth.AuthApplicationIds = null!;
        capabilities.Add(nullAuth);
        DiameterBaseCommandOptions nullAccounting = CreateOptions();
        nullAccounting.AcctApplicationIds = null!;
        capabilities.Add(nullAccounting);
        DiameterBaseCommandOptions nullVendorApplications = CreateOptions();
        nullVendorApplications.VendorSpecificApplications = null!;
        capabilities.Add(nullVendorApplications);
        DiameterBaseCommandOptions nullSecurity = CreateOptions();
        nullSecurity.InbandSecurityIds = null!;
        capabilities.Add(nullSecurity);
        DiameterBaseCommandOptions nullAddress = CreateOptions();
        nullAddress.HostAddresses[0] = null!;
        capabilities.Add(nullAddress);
        DiameterBaseCommandOptions nullVendorApplication = CreateOptions();
        nullVendorApplication.VendorSpecificApplications.Add(null!);
        capabilities.Add(nullVendorApplication);
        DiameterBaseCommandOptions zeroVendorApplication = CreateOptions();
        zeroVendorApplication.VendorSpecificApplications.Add(
            new DiameterApplicationId());
        capabilities.Add(zeroVendorApplication);
        DiameterBaseCommandOptions nullProduct = CreateOptions();
        nullProduct.ProductName = null!;
        capabilities.Add(nullProduct);
        DiameterBaseCommandOptions invalidProduct = CreateOptions();
        invalidProduct.ProductName = "bad\ud800product";
        capabilities.Add(invalidProduct);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                factory.CreateDisconnectPeerRequest(lowDisconnect));
            Assert.Throws<ArgumentException>(() =>
                factory.CreateReAuthRequest(lowReAuth));
            Assert.Throws<ArgumentException>(() =>
                factory.CreateReAuthRequest(highReAuth));
            Assert.Throws<ArgumentException>(() =>
                factory.CreateReAuthRequest(invalidReAuthApplication));
            int selectorIndex = 0;
            while (selectorIndex < baseSelectors.Count)
            {
                DiameterBaseCommandOptions value =
                    baseSelectors[selectorIndex];
                Assert.Throws<ArgumentException>(() =>
                    factory.CreateReAuthRequest(value));
                selectorIndex = selectorIndex + 1;
            }

            Assert.Throws<ArgumentException>(() =>
                factory.CreateReAuthRequest(wrongCreditAuth));
            int capabilityIndex = 0;
            while (capabilityIndex < capabilities.Count)
            {
                DiameterBaseCommandOptions value =
                    capabilities[capabilityIndex];
                Assert.Throws<ArgumentException>(() =>
                    factory.CreateCapabilitiesExchangeRequest(value));
                capabilityIndex = capabilityIndex + 1;
            }
        });
    }

    [Test]
    public void ReAuthAnswerRejectsEveryMismatchedSourceShape()
    {
        BaseCommandTestIdentifierSource identifiers =
            new BaseCommandTestIdentifierSource();
        IDiameterBaseCommandFactory factory =
            new DiameterBaseCommandFactory(identifiers);
        DiameterMessage validRequest =
            factory.CreateReAuthRequest(CreateOptions());

        List<DiameterBaseCommandOptions> invalid =
            new List<DiameterBaseCommandOptions>();
        DiameterBaseCommandOptions nullAvps = CreateOptions();
        nullAvps.Request = CloneMessage(validRequest);
        nullAvps.Request.Avps = null!;
        invalid.Add(nullAvps);
        DiameterBaseCommandOptions answer = CreateOptions();
        answer.Request = CloneMessage(validRequest);
        answer.Request.Flags = DiameterMessageFlags.None;
        invalid.Add(answer);
        DiameterBaseCommandOptions wrongCommand = CreateOptions();
        wrongCommand.Request = CloneMessage(validRequest);
        wrongCommand.Request.CommandCode = DiameterCommandCodes.AbortSession;
        invalid.Add(wrongCommand);
        DiameterBaseCommandOptions wrongApplication = CreateOptions();
        wrongApplication.Request = CloneMessage(validRequest);
        wrongApplication.Request.ApplicationId = 9U;
        invalid.Add(wrongApplication);
        DiameterBaseCommandOptions missingSession = CreateOptions();
        missingSession.Request = CloneMessage(validRequest);
        missingSession.Request.Avps.Clear();
        invalid.Add(missingSession);
        DiameterBaseCommandOptions zeroResult = CreateOptions();
        zeroResult.ResultCode = 0U;

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < invalid.Count)
            {
                DiameterBaseCommandOptions value = invalid[index];
                Assert.Throws<ArgumentException>(() =>
                    factory.CreateReAuthAnswer(value));
                index = index + 1;
            }

            Assert.Throws<ArgumentException>(() =>
                factory.CreateDeviceWatchdogRequest(zeroResult));
        });
    }

    private static DiameterBaseCommandOptions CreateOptions()
    {
        DiameterBaseCommandOptions options = new DiameterBaseCommandOptions();
        options.OriginHost.Value = "NODE.Example.";
        options.OriginRealm.Value = "EXAMPLE.";
        options.HostAddresses.Add(IPAddress.Parse("192.0.2.10"));
        options.VendorId = 10415U;
        options.ProductName = "diameter-tests";
        options.FirmwareRevision = 7U;
        options.SupportedVendorIds.Add(10415U);
        options.AuthApplicationIds.Add(4U);
        options.AcctApplicationIds.Add(3U);
        options.DestinationHost = CreateIdentity("server.example");
        options.DestinationRealm = CreateIdentity("example");
        options.SessionId = "node.example;3913056000;1";
        options.AuthApplicationId = 4U;
        options.DisconnectCause = DiameterDisconnectCause.Busy;
        options.ReAuthRequestType = DiameterReAuthRequestType.AuthorizeAuthenticate;
        options.TerminationCause = DiameterTerminationCause.Administrative;
        options.AccountingRecordType = DiameterAccountingRecordType.Interim;
        options.AccountingRecordNumber = 9U;
        return options;
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity identity = new DiameterIdentity();
        identity.Value = value;
        return identity;
    }

    private static DiameterMessage CloneMessage(DiameterMessage source)
    {
        DiameterMessage result = new DiameterMessage();
        result.Version = source.Version;
        result.Flags = source.Flags;
        result.CommandCode = source.CommandCode;
        result.ApplicationId = source.ApplicationId;
        result.HopByHopIdentifier = source.HopByHopIdentifier;
        result.EndToEndIdentifier = source.EndToEndIdentifier;
        int index = 0;
        while (index < source.Avps.Count)
        {
            DiameterAvp sourceAvp = source.Avps[index];
            DiameterAvp avp = new DiameterAvp();
            avp.Code = sourceAvp.Code;
            avp.Flags = sourceAvp.Flags;
            avp.VendorId = sourceAvp.VendorId;
            avp.Data = (byte[])sourceAvp.Data.Clone();
            result.Avps.Add(avp);
            index = index + 1;
        }

        return result;
    }

    private static void AssertValidAndEncodable(DiameterMessage message)
    {
        IDiameterDictionary dictionary = new DiameterDictionary();
        IDiameterValidator validator = new DiameterValidator();
        DiameterValidationResult validation = validator.ValidateMessage(
            message,
            dictionary.ReadDictionarySnapshot(),
            new DiameterLimitConfiguration());
        DiameterEncodeResult encoding = new DiameterMessageCodec().EncodeMessageToArray(message);

        Assert.That(validation.Status, Is.EqualTo(DiameterValidationStatus.Valid),
            string.Join(" | ", validation.Issues.ConvertAll(issue => issue.Code)));
        Assert.That(encoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
        Assert.That(encoding.Data.Length, Is.GreaterThan(DiameterProtocolConstants.MessageHeaderSize));
    }

    private static void AssertRequestHeader(
        DiameterMessage message,
        uint commandCode,
        uint applicationId,
        bool proxyable)
    {
        DiameterMessageFlags expected = DiameterMessageFlags.Request;
        if (proxyable)
        {
            expected = expected | DiameterMessageFlags.Proxyable;
        }

        Assert.That(message.Flags, Is.EqualTo(expected));
        Assert.That(message.CommandCode, Is.EqualTo(commandCode));
        Assert.That(message.ApplicationId, Is.EqualTo(applicationId));
    }

    private static void AssertAnswerCorrelation(
        DiameterMessage request,
        DiameterMessage answer,
        bool proxyable)
    {
        DiameterMessageFlags expected = DiameterMessageFlags.None;
        if (proxyable)
        {
            expected = DiameterMessageFlags.Proxyable;
        }

        Assert.That(answer.Flags, Is.EqualTo(expected));
        Assert.That(answer.CommandCode, Is.EqualTo(request.CommandCode));
        Assert.That(answer.ApplicationId, Is.EqualTo(request.ApplicationId));
        Assert.That(answer.HopByHopIdentifier, Is.EqualTo(request.HopByHopIdentifier));
        Assert.That(answer.EndToEndIdentifier, Is.EqualTo(request.EndToEndIdentifier));
    }

    private static DiameterAvp FindAvp(DiameterMessage message, uint code)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (message.Avps[index].Code == code && message.Avps[index].VendorId == 0U)
            {
                return message.Avps[index];
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

public class BaseCommandTestIdentifierSource : IDiameterIdentifierSource
{
    public uint HopByHopIdentifier = 100U;
    public uint EndToEndIdentifier = 200U;
    public uint OriginStateIdentifier = 300U;

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
        return OriginStateIdentifier;
    }
}
