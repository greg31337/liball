using System.Net;

namespace diameter.Tests;

public class PeerCapabilitiesAnswerTests
{
    [Test]
    public void SuccessfulCeaProducesCorrelatedOwnedCapabilitySnapshot()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PeerCapabilitiesExchange localExchange = CreateExchange(clock);
        PeerCapabilitiesExchange remoteExchange = CreateExchange(clock);
        DiameterPeerCapabilities local = CreateCapabilities("local.example");
        AddApplications(local, 4U, 3U, 10U, false);
        local.AuthApplicationIds.Add(5U);
        local.AcctApplicationIds.Add(6U);
        DiameterPeerCapabilities remote = CreateCapabilities("remote.example");
        AddApplications(remote, 4U, 3U, 10U, false);
        remote.AuthApplicationIds.Add(8U);
        remote.VendorSpecificApplications.Add(CreateVendorApplication(11U, true));
        DiameterMessage request = localExchange.CreateCapabilitiesExchangeRequest(local);

        DiameterMessage answer = remoteExchange.CreateCapabilitiesExchangeAnswer(
            request,
            remote);
        clock.AdvanceDiscoveryTime(1);
        PeerCapabilitiesExchangeResult processed =
            localExchange.ProcessCapabilitiesExchangeAnswer(answer, request, local);
        FindAvp(answer, DiameterAvpCodes.OriginHost).Data[0] = (byte)'x';
        remote.OriginHost.Value = "mutated.example";
        local.AuthApplicationIds[0] = 999U;

        Assert.Multiple(() =>
        {
            Assert.That(answer.Flags, Is.EqualTo(DiameterMessageFlags.None));
            Assert.That(answer.CommandCode,
                Is.EqualTo(DiameterCommandCodes.CapabilitiesExchange));
            Assert.That(answer.ApplicationId, Is.EqualTo(DiameterApplicationIds.Common));
            Assert.That(answer.HopByHopIdentifier,
                Is.EqualTo(request.HopByHopIdentifier));
            Assert.That(answer.EndToEndIdentifier,
                Is.EqualTo(request.EndToEndIdentifier));
            Assert.That(ReadUnsigned32(answer, DiameterAvpCodes.ResultCode),
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(processed.Validation.Status,
                Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(processed.Accepted, Is.True);
            Assert.That(processed.ResultCode, Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(processed.Capabilities.OriginHost.Value,
                Is.EqualTo("remote.example"));
            Assert.That(processed.Capabilities.UpdatedAt, Is.EqualTo(clock.UtcTime));
            Assert.That(processed.Capabilities.CommonApplications, Has.Count.EqualTo(3));
            AssertApplication(
                processed.Capabilities.CommonApplications[0],
                4U,
                0U,
                false);
            AssertApplication(
                processed.Capabilities.CommonApplications[1],
                3U,
                0U,
                true);
            AssertApplication(
                processed.Capabilities.CommonApplications[2],
                10U,
                10415U,
                false);
        });
    }

    [Test]
    public void NoCommonApplicationCreatesAndProcessesFailureCea()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PeerCapabilitiesExchange localExchange = CreateExchange(clock);
        PeerCapabilitiesExchange remoteExchange = CreateExchange(clock);
        DiameterPeerCapabilities local = CreateCapabilities("local.example");
        local.AuthApplicationIds.Add(4U);
        DiameterPeerCapabilities remote = CreateCapabilities("remote.example");
        remote.AcctApplicationIds.Add(3U);
        DiameterMessage request = localExchange.CreateCapabilitiesExchangeRequest(local);

        DiameterMessage answer = remoteExchange.CreateCapabilitiesExchangeAnswer(
            request,
            remote);
        PeerCapabilitiesExchangeResult processed =
            localExchange.ProcessCapabilitiesExchangeAnswer(answer, request, local);

        Assert.Multiple(() =>
        {
            Assert.That(ReadUnsigned32(answer, DiameterAvpCodes.ResultCode),
                Is.EqualTo(DiameterResultCodes.NoCommonApplication));
            Assert.That(processed.Validation.Status,
                Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(processed.Accepted, Is.False);
            Assert.That(processed.ResultCode,
                Is.EqualTo(DiameterResultCodes.NoCommonApplication));
            Assert.That(processed.Capabilities.CommonApplications, Is.Empty);
        });
    }

    [Test]
    public void CommonApplicationCalculationIsExactOrderedAndDuplicateFree()
    {
        PeerCapabilitiesExchange exchange = CreateExchange(new FakeDiscoveryClock());
        DiameterPeerCapabilities local = CreateCapabilities("local.example");
        local.AuthApplicationIds.Add(4U);
        local.AuthApplicationIds.Add(4U);
        local.AuthApplicationIds.Add(10U);
        local.AcctApplicationIds.Add(3U);
        local.AcctApplicationIds.Add(3U);
        local.VendorSpecificApplications.Add(CreateVendorApplication(12U, false));
        local.VendorSpecificApplications.Add(CreateVendorApplication(12U, false));
        local.VendorSpecificApplications.Add(CreateVendorApplication(11U, true));
        DiameterPeerCapabilities remote = CreateCapabilities("remote.example");
        remote.AuthApplicationIds.Add(4U);
        remote.AcctApplicationIds.Add(3U);
        remote.VendorSpecificApplications.Add(CreateVendorApplication(10U, false));
        DiameterApplicationId otherVendor = CreateVendorApplication(12U, false);
        otherVendor.VendorId = 5535U;
        remote.VendorSpecificApplications.Add(otherVendor);
        remote.VendorSpecificApplications.Add(CreateVendorApplication(11U, false));

        List<DiameterApplicationId> common = exchange.ReadCommonPeerApplications(
            local,
            remote);
        local.VendorSpecificApplications[0].Id = 999U;

        Assert.Multiple(() =>
        {
            Assert.That(common, Has.Count.EqualTo(4));
            AssertApplication(common[0], 4U, 0U, false);
            AssertApplication(common[1], 10U, 0U, false);
            AssertApplication(common[2], 3U, 0U, true);
            AssertApplication(common[3], 12U, 10415U, false);
        });
    }

    [Test]
    public void RelayAdvertisementMakesAllPeerApplicationsCommon()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PeerCapabilitiesExchange localExchange = CreateExchange(clock);
        PeerCapabilitiesExchange relayExchange = CreateExchange(clock);
        DiameterPeerCapabilities local = CreateCapabilities("local.example");
        local.AuthApplicationIds.Add(4U);
        local.AcctApplicationIds.Add(3U);
        DiameterPeerCapabilities relay = CreateCapabilities("relay.example");
        relay.AuthApplicationIds.Add(DiameterApplicationIds.Relay);
        DiameterMessage request = localExchange.CreateCapabilitiesExchangeRequest(local);

        DiameterMessage answer = relayExchange.CreateCapabilitiesExchangeAnswer(
            request,
            relay);
        PeerCapabilitiesExchangeResult processed =
            localExchange.ProcessCapabilitiesExchangeAnswer(answer, request, local);

        Assert.Multiple(() =>
        {
            Assert.That(ReadUnsigned32(answer, DiameterAvpCodes.ResultCode),
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(processed.Accepted, Is.True);
            Assert.That(processed.Capabilities.CommonApplications, Has.Count.EqualTo(2));
            AssertApplication(
                processed.Capabilities.CommonApplications[0],
                4U,
                0U,
                false);
            AssertApplication(
                processed.Capabilities.CommonApplications[1],
                3U,
                0U,
                true);
        });
    }

    [Test]
    public void ApplicationIntersectionMatchesAcrossEveryAdvertisementForm()
    {
        PeerCapabilitiesExchange exchange = CreateExchange(
            new FakeDiscoveryClock());
        DiameterPeerCapabilities local = CreateCapabilities("local.example");
        local.AuthApplicationIds.Add(4U);
        local.AcctApplicationIds.Add(3U);
        local.VendorSpecificApplications.Add(
            CreateVendorApplication(5U, false));
        DiameterPeerCapabilities remote = CreateCapabilities("remote.example");
        DiameterApplicationId remoteAuthentication =
            CreateVendorApplication(4U, false);
        remoteAuthentication.VendorId = 5535U;
        remote.VendorSpecificApplications.Add(remoteAuthentication);
        DiameterApplicationId remoteAccounting =
            CreateVendorApplication(3U, true);
        remoteAccounting.VendorId = 5535U;
        remote.VendorSpecificApplications.Add(remoteAccounting);
        remote.AuthApplicationIds.Add(5U);

        List<DiameterApplicationId> common = exchange.ReadCommonPeerApplications(
            local,
            remote);

        Assert.Multiple(() =>
        {
            Assert.That(common, Has.Count.EqualTo(3));
            AssertApplication(common[0], 4U, 0U, false);
            AssertApplication(common[1], 3U, 0U, true);
            AssertApplication(common[2], 5U, 10415U, false);
        });
    }

    [Test]
    public void SecurityNegotiationHandlesOverlapDefaultAndRejection()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PeerCapabilitiesExchange localExchange = CreateExchange(clock);
        PeerCapabilitiesExchange remoteExchange = CreateExchange(clock);
        DiameterPeerCapabilities local = CreateCapabilities("local.example");
        local.AuthApplicationIds.Add(4U);
        local.InbandSecurityIds.Add(1U);
        DiameterPeerCapabilities remote = CreateCapabilities("remote.example");
        remote.AuthApplicationIds.Add(4U);
        remote.InbandSecurityIds.Clear();
        remote.InbandSecurityIds.Add(1U);
        DiameterMessage overlapRequest =
            localExchange.CreateCapabilitiesExchangeRequest(local);
        DiameterMessage overlapAnswer =
            remoteExchange.CreateCapabilitiesExchangeAnswer(
                overlapRequest,
                remote);
        PeerCapabilitiesExchangeResult overlap =
            localExchange.ProcessCapabilitiesExchangeAnswer(
                overlapAnswer,
                overlapRequest,
                local);

        local.InbandSecurityIds.Clear();
        remote.InbandSecurityIds.Clear();
        remote.InbandSecurityIds.Add(0U);
        DiameterMessage defaultRequest =
            localExchange.CreateCapabilitiesExchangeRequest(local);
        DiameterMessage defaultAnswer =
            remoteExchange.CreateCapabilitiesExchangeAnswer(
                defaultRequest,
                remote);
        PeerCapabilitiesExchangeResult defaultSecurity =
            localExchange.ProcessCapabilitiesExchangeAnswer(
                defaultAnswer,
                defaultRequest,
                local);

        local.InbandSecurityIds.Add(0U);
        remote.InbandSecurityIds.Clear();
        remote.InbandSecurityIds.Add(1U);
        DiameterMessage rejectedRequest =
            localExchange.CreateCapabilitiesExchangeRequest(local);
        DiameterMessage rejectedAnswer =
            remoteExchange.CreateCapabilitiesExchangeAnswer(
                rejectedRequest,
                remote);
        uint rejectedAnswerResult = ReadUnsigned32(
            rejectedAnswer,
            DiameterAvpCodes.ResultCode);
        NetworkByteOrder.WriteUInt32(
            FindAvp(rejectedAnswer, DiameterAvpCodes.ResultCode).Data,
            DiameterResultCodes.Success);
        PeerCapabilitiesExchangeResult rejected =
            localExchange.ProcessCapabilitiesExchangeAnswer(
                rejectedAnswer,
                rejectedRequest,
                local);

        Assert.Multiple(() =>
        {
            Assert.That(ReadUnsigned32(
                overlapAnswer,
                DiameterAvpCodes.ResultCode),
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(overlap.Accepted, Is.True);
            Assert.That(ReadUnsigned32(
                defaultAnswer,
                DiameterAvpCodes.ResultCode),
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(defaultSecurity.Accepted, Is.True);
            Assert.That(rejectedAnswerResult,
                Is.EqualTo(DiameterResultCodes.NoCommonSecurity));
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.ResultCode,
                Is.EqualTo(DiameterResultCodes.NoCommonSecurity));
        });
    }

    [Test]
    public void RejectsMalformedAndUncorrelatedCeaAndPreservesRemoteFailure()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PeerCapabilitiesExchange localExchange = CreateExchange(clock);
        PeerCapabilitiesExchange remoteExchange = CreateExchange(clock);
        DiameterPeerCapabilities local = CreateCapabilities("local.example");
        local.AuthApplicationIds.Add(4U);
        DiameterPeerCapabilities remote = CreateCapabilities("remote.example");
        remote.AuthApplicationIds.Add(4U);
        DiameterMessage request = localExchange.CreateCapabilitiesExchangeRequest(local);

        DiameterMessage uncorrelated = remoteExchange.CreateCapabilitiesExchangeAnswer(
            request,
            remote);
        uncorrelated.HopByHopIdentifier = uncorrelated.HopByHopIdentifier + 1U;
        DiameterMessage malformed = remoteExchange.CreateCapabilitiesExchangeAnswer(
            request,
            remote);
        malformed.Flags = DiameterMessageFlags.Request;
        DiameterMessage failed = remoteExchange.CreateCapabilitiesExchangeAnswer(
            request,
            remote);
        NetworkByteOrder.WriteUInt32(
            FindAvp(failed, DiameterAvpCodes.ResultCode).Data,
            DiameterResultCodes.UnableToComply);

        PeerCapabilitiesExchangeResult correlation =
            localExchange.ProcessCapabilitiesExchangeAnswer(
                uncorrelated,
                request,
                local);
        PeerCapabilitiesExchangeResult grammar =
            localExchange.ProcessCapabilitiesExchangeAnswer(
                malformed,
                request,
                local);
        PeerCapabilitiesExchangeResult remoteFailure =
            localExchange.ProcessCapabilitiesExchangeAnswer(failed, request, local);

        Assert.Multiple(() =>
        {
            AssertFailure(correlation, "capabilities-answer-correlation-mismatch");
            Assert.That(correlation.Validation.ErrorAnswer, Is.Null);
            AssertFailure(grammar, "capabilities-answer-header-mismatch");
            Assert.That(grammar.Validation.ErrorAnswer, Is.Null);
            Assert.That(remoteFailure.Validation.Status,
                Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(remoteFailure.Accepted, Is.False);
            Assert.That(remoteFailure.ResultCode,
                Is.EqualTo(DiameterResultCodes.UnableToComply));
            Assert.That(remoteFailure.Capabilities.CommonApplications,
                Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void AnswerCreationRejectsAnInvalidCer()
    {
        PeerCapabilitiesExchange exchange = CreateExchange(new FakeDiscoveryClock());
        DiameterPeerCapabilities local = CreateCapabilities("local.example");
        local.AuthApplicationIds.Add(4U);
        DiameterMessage request = exchange.CreateCapabilitiesExchangeRequest(local);
        request.Avps.Remove(FindAvp(request, DiameterAvpCodes.OriginRealm));

        Assert.That(() => exchange.CreateCapabilitiesExchangeAnswer(request, local),
            Throws.TypeOf<DiameterPeerException>());
    }

    private static PeerCapabilitiesExchange CreateExchange(FakeDiscoveryClock clock)
    {
        return new PeerCapabilitiesExchange(
            new BaseCommandTestIdentifierSource(),
            clock,
            new DiameterLimitConfiguration());
    }

    private static DiameterPeerCapabilities CreateCapabilities(string identity)
    {
        DiameterPeerCapabilities result = new DiameterPeerCapabilities();
        result.OriginHost.Value = identity;
        result.OriginRealm.Value = "example";
        result.HostAddresses.Add(IPAddress.Parse("192.0.2.10"));
        result.VendorId = 10415U;
        result.ProductName = "diameter-node";
        result.FirmwareRevision = 7U;
        result.SupportedVendorIds.Add(10415U);
        result.InbandSecurityIds.Add(0U);
        return result;
    }

    private static void AddApplications(
        DiameterPeerCapabilities capabilities,
        uint authentication,
        uint accounting,
        uint vendorSpecific,
        bool vendorAccounting)
    {
        capabilities.AuthApplicationIds.Add(authentication);
        capabilities.AcctApplicationIds.Add(accounting);
        capabilities.VendorSpecificApplications.Add(
            CreateVendorApplication(vendorSpecific, vendorAccounting));
    }

    private static DiameterApplicationId CreateVendorApplication(
        uint applicationId,
        bool accounting)
    {
        DiameterApplicationId result = new DiameterApplicationId();
        result.Id = applicationId;
        result.VendorId = 10415U;
        result.IsAccounting = accounting;
        return result;
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

    private static uint ReadUnsigned32(DiameterMessage message, uint code)
    {
        return NetworkByteOrder.ReadUInt32(FindAvp(message, code).Data);
    }

    private static void AssertApplication(
        DiameterApplicationId application,
        uint applicationId,
        uint vendorId,
        bool accounting)
    {
        Assert.That(application.Id, Is.EqualTo(applicationId));
        Assert.That(application.VendorId, Is.EqualTo(vendorId));
        Assert.That(application.IsAccounting, Is.EqualTo(accounting));
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
