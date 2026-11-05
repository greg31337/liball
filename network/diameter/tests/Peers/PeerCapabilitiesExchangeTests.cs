using System.Net;

namespace diameter.Tests;

public class PeerCapabilitiesExchangeTests
{
    [Test]
    public void FullCerAndCeaMatchStableWireVectorsAndRoundTrip()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PeerCapabilitiesExchange exchange = CreateExchange(clock);
        DiameterPeerCapabilities capabilities = CreateCapabilities();
        DiameterMessage request = exchange.CreateCapabilitiesExchangeRequest(
            capabilities);
        DiameterMessage answer = exchange.CreateCapabilitiesExchangeAnswer(
            request,
            capabilities);
        DiameterMessageCodec codec = new DiameterMessageCodec();
        DiameterEncodeResult requestEncoding = codec.EncodeMessageToArray(request);
        DiameterEncodeResult answerEncoding = codec.EncodeMessageToArray(answer);
        DiameterDecodeResult requestDecoding = codec.DecodeMessage(
            requestEncoding.Data,
            false,
            new DiameterLimitConfiguration());
        DiameterDecodeResult answerDecoding = codec.DecodeMessage(
            answerEncoding.Data,
            false,
            new DiameterLimitConfiguration());
        PeerCapabilitiesExchangeResult parsedRequest =
            exchange.ParseCapabilitiesExchangeRequest(requestDecoding.Message);
        PeerCapabilitiesExchangeResult parsedAnswer =
            exchange.ProcessCapabilitiesExchangeAnswer(
                answerDecoding.Message,
                request,
                capabilities);

        Assert.Multiple(() =>
        {
            Assert.That(requestEncoding.Status,
                Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(Convert.ToHexString(requestEncoding.Data),
                Is.EqualTo(
                    "01000128800001010000000000000065000000C9" +
                    "00000108400000146E6F64652E6578616D706C6500" +
                    "0001284000000F6578616D706C6500" +
                    "000001014000000E0001C000020A0000" +
                    "000001014000001A000220010DB80000000000000000000000100000" +
                    "0000010A4000000C000028AF" +
                    "0000010D000000156469616D657465722D6E6F6465000000" +
                    "000001164000000C0000012C" +
                    "000001094000000C000028AF" +
                    "000001094000000C0000159F" +
                    "000001024000000C00000004" +
                    "0000012B4000000C00000000" +
                    "0000012B4000000C00000001" +
                    "000001034000000C00000003" +
                    "00000104400000200000010A4000000C000028AF" +
                    "000001024000000C01000000" +
                    "00000104400000200000010A4000000C000028AF" +
                    "000001034000000C01000001" +
                    "0000010B0000000C00000007"));
            Assert.That(answerEncoding.Status,
                Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(Convert.ToHexString(answerEncoding.Data),
                Is.EqualTo(
                    "01000134000001010000000000000065000000C9" +
                    "0000010C4000000C000007D1" +
                    "00000108400000146E6F64652E6578616D706C6500" +
                    "0001284000000F6578616D706C6500" +
                    "000001014000000E0001C000020A0000" +
                    "000001014000001A000220010DB80000000000000000000000100000" +
                    "0000010A4000000C000028AF" +
                    "0000010D000000156469616D657465722D6E6F6465000000" +
                    "000001164000000C0000012C" +
                    "000001094000000C000028AF" +
                    "000001094000000C0000159F" +
                    "000001024000000C00000004" +
                    "0000012B4000000C00000000" +
                    "0000012B4000000C00000001" +
                    "000001034000000C00000003" +
                    "00000104400000200000010A4000000C000028AF" +
                    "000001024000000C01000000" +
                    "00000104400000200000010A4000000C000028AF" +
                    "000001034000000C01000001" +
                    "0000010B0000000C00000007"));
            Assert.That(requestDecoding.Status,
                Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(answerDecoding.Status,
                Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(parsedRequest.Accepted, Is.True);
            Assert.That(parsedRequest.Capabilities.VendorSpecificApplications,
                Has.Count.EqualTo(2));
            Assert.That(parsedAnswer.Accepted, Is.True);
        });
    }

    [Test]
    public void CreatesAndParsesCerWithEveryCapabilityForm()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        BaseCommandTestIdentifierSource identifiers =
            new BaseCommandTestIdentifierSource();
        PeerCapabilitiesExchange exchange = new PeerCapabilitiesExchange(
            identifiers,
            clock,
            new DiameterLimitConfiguration());
        DiameterPeerCapabilities local = CreateCapabilities();

        DiameterMessage request = exchange.CreateCapabilitiesExchangeRequest(local);
        DiameterAvp extension = new DiameterAvp();
        extension.Code = 9999U;
        extension.Data = new byte[] { 1, 2, 3 };
        request.Avps.Add(extension);
        local.OriginHost.Value = "mutated.example";
        local.HostAddresses[0] = IPAddress.Parse("203.0.113.1");
        local.VendorSpecificApplications[0].Id = 999U;
        PeerCapabilitiesExchangeResult parsed =
            exchange.ParseCapabilitiesExchangeRequest(request);
        FindAvp(request, DiameterAvpCodes.OriginHost).Data[0] = (byte)'x';

        Assert.Multiple(() =>
        {
            Assert.That(request.Version, Is.EqualTo(DiameterProtocolConstants.Version));
            Assert.That(request.Flags, Is.EqualTo(DiameterMessageFlags.Request));
            Assert.That(request.ApplicationId, Is.EqualTo(DiameterApplicationIds.Common));
            Assert.That(request.CommandCode,
                Is.EqualTo(DiameterCommandCodes.CapabilitiesExchange));
            Assert.That(request.HopByHopIdentifier, Is.EqualTo(101U));
            Assert.That(request.EndToEndIdentifier, Is.EqualTo(201U));
            Assert.That(CountAvps(request, DiameterAvpCodes.HostIpAddress), Is.EqualTo(2));
            Assert.That(CountAvps(request, DiameterAvpCodes.SupportedVendorId),
                Is.EqualTo(2));
            Assert.That(CountAvps(request, DiameterAvpCodes.InbandSecurityId),
                Is.EqualTo(2));
            Assert.That(CountAvps(request, DiameterAvpCodes.VendorSpecificApplicationId),
                Is.EqualTo(2));
            Assert.That(parsed.Validation.Status, Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(parsed.Capabilities.OriginHost.Value, Is.EqualTo("node.example"));
            Assert.That(parsed.Capabilities.OriginRealm.Value, Is.EqualTo("example"));
            Assert.That(parsed.Capabilities.HostAddresses,
                Is.EqualTo(new IPAddress[]
                {
                    IPAddress.Parse("192.0.2.10"),
                    IPAddress.Parse("2001:db8::10")
                }));
            Assert.That(parsed.Capabilities.VendorId, Is.EqualTo(10415U));
            Assert.That(parsed.Capabilities.ProductName, Is.EqualTo("diameter-node"));
            Assert.That(parsed.Capabilities.FirmwareRevision, Is.EqualTo(7U));
            Assert.That(parsed.Capabilities.OriginStateId, Is.EqualTo(300U));
            Assert.That(parsed.Capabilities.SupportedVendorIds,
                Is.EqualTo(new uint[] { 10415U, 5535U }));
            Assert.That(parsed.Capabilities.AuthApplicationIds,
                Is.EqualTo(new uint[] { 4U }));
            Assert.That(parsed.Capabilities.AcctApplicationIds,
                Is.EqualTo(new uint[] { 3U }));
            Assert.That(parsed.Capabilities.InbandSecurityIds,
                Is.EqualTo(new uint[] { 0U, 1U }));
            Assert.That(parsed.Capabilities.VendorSpecificApplications[0].Id,
                Is.EqualTo(16777216U));
            Assert.That(parsed.Capabilities.VendorSpecificApplications[0].VendorId,
                Is.EqualTo(10415U));
            Assert.That(parsed.Capabilities.VendorSpecificApplications[0].IsAccounting,
                Is.False);
            Assert.That(parsed.Capabilities.VendorSpecificApplications[1].Id,
                Is.EqualTo(16777217U));
            Assert.That(parsed.Capabilities.VendorSpecificApplications[1].IsAccounting,
                Is.True);
            Assert.That(parsed.Capabilities.CommonApplications, Is.Empty);
            Assert.That(parsed.Capabilities.UpdatedAt, Is.EqualTo(clock.UtcTime));
        });
    }

    [Test]
    public void RejectsInvalidCerHeadersAndRequiredAvpValues()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PeerCapabilitiesExchange exchange = CreateExchange(clock);

        DiameterMessage wrongVersion = CreateCer(exchange);
        wrongVersion.Version = 2;
        DiameterMessage wrongFlags = CreateCer(exchange);
        wrongFlags.Flags = wrongFlags.Flags | DiameterMessageFlags.Proxyable;
        DiameterMessage missingOrigin = CreateCer(exchange);
        missingOrigin.Avps.Remove(FindAvp(missingOrigin, DiameterAvpCodes.OriginHost));
        DiameterMessage invalidIdentity = CreateCer(exchange);
        FindAvp(invalidIdentity, DiameterAvpCodes.OriginHost).Data =
            new byte[] { 255 };
        DiameterMessage invalidAddress = CreateCer(exchange);
        FindAvp(invalidAddress, DiameterAvpCodes.HostIpAddress).Data =
            new byte[] { 0, 1, 127 };
        DiameterMessage invalidVendor = CreateCer(exchange);
        FindAvp(invalidVendor, DiameterAvpCodes.VendorId).Data =
            new byte[] { 0, 0, 1 };

        PeerCapabilitiesExchangeResult absent =
            exchange.ParseCapabilitiesExchangeRequest(null);
        PeerCapabilitiesExchangeResult version =
            exchange.ParseCapabilitiesExchangeRequest(wrongVersion);
        PeerCapabilitiesExchangeResult flags =
            exchange.ParseCapabilitiesExchangeRequest(wrongFlags);
        PeerCapabilitiesExchangeResult origin =
            exchange.ParseCapabilitiesExchangeRequest(missingOrigin);
        PeerCapabilitiesExchangeResult identity =
            exchange.ParseCapabilitiesExchangeRequest(invalidIdentity);
        PeerCapabilitiesExchangeResult address =
            exchange.ParseCapabilitiesExchangeRequest(invalidAddress);
        PeerCapabilitiesExchangeResult vendor =
            exchange.ParseCapabilitiesExchangeRequest(invalidVendor);

        Assert.Multiple(() =>
        {
            AssertFailure(absent, "message-required");
            AssertFailure(version, "unsupported-diameter-version");
            Assert.That(version.Validation.Issues[0].ResultCode,
                Is.EqualTo(DiameterResultCodes.UnsupportedVersion));
            AssertFailure(flags, "prohibited-header-flags-present");
            AssertFailure(origin, "missing-required-avp");
            Assert.That(origin.Validation.Issues[0].AvpCode,
                Is.EqualTo(DiameterAvpCodes.OriginHost));
            AssertFailure(identity, "invalid-derived-avp-value");
            AssertFailure(address, "invalid-derived-avp-value");
            AssertFailure(vendor, "invalid-avp-size");
        });
    }

    [Test]
    public void RejectsSemanticallyInvalidProductAndVendorApplicationValues()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PeerCapabilitiesExchange exchange = CreateExchange(clock);
        DiameterMessage emptyProduct = CreateCer(exchange);
        FindAvp(emptyProduct, DiameterAvpCodes.ProductName).Data = new byte[0];
        DiameterMessage ambiguousApplication = CreateCer(exchange);
        ReplaceFirstAvp(
            ambiguousApplication,
            DiameterAvpCodes.VendorSpecificApplicationId,
            CreateAmbiguousVendorApplication());

        PeerCapabilitiesExchangeResult product =
            exchange.ParseCapabilitiesExchangeRequest(emptyProduct);
        PeerCapabilitiesExchangeResult application =
            exchange.ParseCapabilitiesExchangeRequest(ambiguousApplication);

        Assert.Multiple(() =>
        {
            AssertFailure(product, "invalid-product-name");
            Assert.That(product.Validation.ErrorAnswer!.ResultCode,
                Is.EqualTo(DiameterResultCodes.InvalidAvpValue));
            Assert.That(product.Validation.ErrorAnswer.FailedAvp, Is.Not.Null);
            AssertFailure(application, "invalid-vendor-specific-application");
            Assert.That(application.Validation.Issues[0].AvpCode,
                Is.EqualTo(DiameterAvpCodes.VendorSpecificApplicationId));
            Assert.That(application.Validation.ErrorAnswer!.FailedAvp, Is.Not.Null);
        });
    }

    [TestCase(DiameterAvpCodes.VendorId)]
    [TestCase(DiameterAvpCodes.OriginStateId)]
    [TestCase(DiameterAvpCodes.SupportedVendorId)]
    [TestCase(DiameterAvpCodes.AuthApplicationId)]
    [TestCase(DiameterAvpCodes.AcctApplicationId)]
    [TestCase(DiameterAvpCodes.InbandSecurityId)]
    [TestCase(DiameterAvpCodes.FirmwareRevision)]
    public void RejectsMalformedUnsignedCapabilityValues(uint avpCode)
    {
        PeerCapabilitiesExchange exchange = CreateExchange(
            new FakeDiscoveryClock());
        DiameterMessage request = CreateCer(exchange);
        FindAvp(request, avpCode).Data = new byte[] { 0, 0, 1 };

        PeerCapabilitiesExchangeResult result =
            exchange.ParseCapabilitiesExchangeRequest(request);

        AssertFailure(result, "invalid-avp-size");
        Assert.That(result.Validation.Issues[0].AvpCode, Is.EqualTo(avpCode));
    }

    [Test]
    public void RejectsInvalidRealmAndUnsupportedSecurityIdentifiers()
    {
        PeerCapabilitiesExchange exchange = CreateExchange(
            new FakeDiscoveryClock());
        DiameterMessage invalidRealm = CreateCer(exchange);
        FindAvp(invalidRealm, DiameterAvpCodes.OriginRealm).Data =
            new byte[] { 255 };
        DiameterMessage invalidSecurity = CreateCer(exchange);
        NetworkByteOrder.WriteUInt32(
            FindAvp(invalidSecurity, DiameterAvpCodes.InbandSecurityId).Data,
            2U);
        DiameterPeerCapabilities invalidLocal = CreateCapabilities();
        invalidLocal.InbandSecurityIds.Add(2U);

        PeerCapabilitiesExchangeResult realm =
            exchange.ParseCapabilitiesExchangeRequest(invalidRealm);
        PeerCapabilitiesExchangeResult security =
            exchange.ParseCapabilitiesExchangeRequest(invalidSecurity);

        Assert.Multiple(() =>
        {
            AssertFailure(realm, "invalid-derived-avp-value");
            AssertFailure(security, "invalid-inband-security-id");
            Assert.That(security.Validation.ErrorAnswer!.FailedAvp, Is.Not.Null);
            Assert.Throws<ArgumentException>(() =>
                exchange.CreateCapabilitiesExchangeRequest(invalidLocal));
        });
    }

    [Test]
    public void EnforcesConfiguredLimitsAndRejectsInvalidLocalCapabilities()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        DiameterLimitConfiguration limits = new DiameterLimitConfiguration();
        limits.MaximumAvpCount = 8;
        limits.MaximumGroupedAvpCount = 8;
        PeerCapabilitiesExchange limited = new PeerCapabilitiesExchange(
            new BaseCommandTestIdentifierSource(),
            clock,
            limits);
        DiameterMessage request = limited.CreateCapabilitiesExchangeRequest(
            CreateCapabilities());
        PeerCapabilitiesExchangeResult result =
            limited.ParseCapabilitiesExchangeRequest(request);
        DiameterPeerCapabilities invalidApplication = CreateCapabilities();
        invalidApplication.VendorSpecificApplications[0].VendorId = 0U;
        DiameterPeerCapabilities invalidCollections = CreateCapabilities();
        invalidCollections.InbandSecurityIds = null!;

        Assert.Multiple(() =>
        {
            AssertFailure(result, "message-avp-count-limit");
            Assert.That(() => limited.CreateCapabilitiesExchangeRequest(
                invalidApplication), Throws.ArgumentException);
            Assert.That(() => limited.CreateCapabilitiesExchangeRequest(
                invalidCollections), Throws.ArgumentException);
        });
    }

    private static PeerCapabilitiesExchange CreateExchange(FakeDiscoveryClock clock)
    {
        return new PeerCapabilitiesExchange(
            new BaseCommandTestIdentifierSource(),
            clock,
            new DiameterLimitConfiguration());
    }

    private static DiameterMessage CreateCer(PeerCapabilitiesExchange exchange)
    {
        return exchange.CreateCapabilitiesExchangeRequest(CreateCapabilities());
    }

    private static DiameterPeerCapabilities CreateCapabilities()
    {
        DiameterPeerCapabilities result = new DiameterPeerCapabilities();
        result.OriginHost.Value = "NODE.Example.";
        result.OriginRealm.Value = "EXAMPLE.";
        result.HostAddresses.Add(IPAddress.Parse("192.0.2.10"));
        result.HostAddresses.Add(IPAddress.Parse("2001:db8::10"));
        result.VendorId = 10415U;
        result.ProductName = "diameter-node";
        result.FirmwareRevision = 7U;
        result.SupportedVendorIds.Add(10415U);
        result.SupportedVendorIds.Add(5535U);
        result.AuthApplicationIds.Add(4U);
        result.AcctApplicationIds.Add(3U);
        result.InbandSecurityIds.Add(0U);
        result.InbandSecurityIds.Add(1U);
        DiameterApplicationId authentication = new DiameterApplicationId();
        authentication.Id = 16777216U;
        authentication.VendorId = 10415U;
        result.VendorSpecificApplications.Add(authentication);
        DiameterApplicationId accounting = new DiameterApplicationId();
        accounting.Id = 16777217U;
        accounting.VendorId = 10415U;
        accounting.IsAccounting = true;
        result.VendorSpecificApplications.Add(accounting);
        return result;
    }

    private static DiameterAvp CreateAmbiguousVendorApplication()
    {
        DiameterBaseAvpFactory factory = new DiameterBaseAvpFactory();
        List<DiameterAvp> children = new List<DiameterAvp>();
        children.Add(factory.CreateUnsigned32Avp(DiameterAvpCodes.VendorId, 10415U));
        children.Add(factory.CreateAuthApplicationIdAvp(4U));
        children.Add(factory.CreateAcctApplicationIdAvp(3U));
        return factory.CreateGroupedAvp(
            DiameterAvpCodes.VendorSpecificApplicationId,
            children);
    }

    private static void ReplaceFirstAvp(
        DiameterMessage message,
        uint code,
        DiameterAvp replacement)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (message.Avps[index].Code == code &&
                message.Avps[index].VendorId == DiameterVendorIds.Ietf)
            {
                message.Avps[index] = replacement;
                return;
            }

            index = index + 1;
        }

        throw new AssertionException($"AVP {code} was not found.");
    }

    private static DiameterAvp FindAvp(DiameterMessage message, uint code)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.Code == code && avp.VendorId == DiameterVendorIds.Ietf)
            {
                return avp;
            }

            index = index + 1;
        }

        throw new AssertionException($"AVP {code} was not found.");
    }

    private static int CountAvps(DiameterMessage message, uint code)
    {
        int count = 0;
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.Code == code && avp.VendorId == DiameterVendorIds.Ietf)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    private static void AssertFailure(
        PeerCapabilitiesExchangeResult result,
        string code)
    {
        Assert.That(result.Validation.Status, Is.EqualTo(DiameterValidationStatus.Invalid));
        Assert.That(result.Validation.Issues, Is.Not.Empty);
        Assert.That(result.Validation.Issues[0].Code, Is.EqualTo(code));
    }
}
