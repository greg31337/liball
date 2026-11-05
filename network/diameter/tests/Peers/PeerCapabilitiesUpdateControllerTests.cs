using System.Net;

namespace diameter.Tests;

public class PeerCapabilitiesUpdateControllerTests
{
    [Test]
    public void CapabilityUpdateRequestAndAnswerMatchStableWireVectors()
    {
        CapabilitiesUpdateTestContext context =
            CreateCapabilitiesUpdateTestContext();
        DiameterMessage request =
            context.Controller.CreatePeerCapabilitiesUpdateRequestMessage(
                context.LocalCapabilities);
        DiameterMessage answer =
            context.Controller.CreatePeerCapabilitiesUpdateAnswerMessage(
                request,
                DiameterResultCodes.Success,
                context.LocalCapabilities);
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
            context.Controller.ParsePeerCapabilitiesUpdateRequest(
                requestDecoding.Message!);
        PeerCapabilitiesExchangeResult parsedAnswer =
            context.Controller.ParsePeerCapabilitiesUpdateAnswer(
                answerDecoding.Message!);

        Assert.Multiple(() =>
        {
            Assert.That(Convert.ToHexString(requestEncoding.Data),
                Is.EqualTo(
                    "01000098800001480000000A00000065000000C9" +
                    "00000108400000146E6F64652E6578616D706C6500" +
                    "0001284000000F6578616D706C6500" +
                    "000001014000000E0001C00002010000" +
                    "0000010A4000000C000028AF" +
                    "0000010D000000126C6F63616C2D6E6F64650000" +
                    "000001164000000C0000012C" +
                    "000001024000000C0000000A" +
                    "000001024000000C00000004" +
                    "0000010B0000000C00000001"));
            Assert.That(Convert.ToHexString(answerEncoding.Data),
                Is.EqualTo(
                    "01000044000001480000000A00000065000000C9" +
                    "00000108400000146E6F64652E6578616D706C6500" +
                    "0001284000000F6578616D706C6500" +
                    "0000010C4000000C000007D1"));
            Assert.That(requestDecoding.Status,
                Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(answerDecoding.Status,
                Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(parsedRequest.Accepted, Is.True);
            Assert.That(parsedAnswer.Accepted, Is.True);
            Assert.That(parsedAnswer.ResultCode,
                Is.EqualTo(DiameterResultCodes.Success));
        });
    }

    [Test]
    public void SupportedOpenPeerCreatesSingleRfc6737Request()
    {
        CapabilitiesUpdateTestContext context =
            CreateCapabilitiesUpdateTestContext();

        PeerCapabilitiesUpdateResult created =
            context.Controller.BeginPeerCapabilitiesUpdate(
                "peer.example",
                context.LocalCapabilities);
        PeerCapabilitiesUpdateResult repeated =
            context.Controller.BeginPeerCapabilitiesUpdate(
                "peer.example",
                context.LocalCapabilities);

        Assert.Multiple(() =>
        {
            Assert.That(created.Handled, Is.True);
            Assert.That(created.Accepted, Is.True);
            Assert.That(created.Code,
                Is.EqualTo("capabilities-update-request-created"));
            Assert.That(created.Message, Is.Not.Null);
            Assert.That(created.Message!.ApplicationId,
                Is.EqualTo(DiameterApplicationIds.CapabilitiesUpdate));
            Assert.That(created.Message.CommandCode,
                Is.EqualTo(DiameterCommandCodes.CapabilitiesUpdate));
            Assert.That(created.Message.Flags,
                Is.EqualTo(DiameterMessageFlags.Request));
            Assert.That(CountCapabilitiesUpdateTestAvps(
                created.Message,
                DiameterAvpCodes.HostIpAddress), Is.EqualTo(1));
            Assert.That(CountCapabilitiesUpdateTestAvps(
                created.Message,
                DiameterAvpCodes.InbandSecurityId), Is.Zero);
            Assert.That(CountCapabilitiesUpdateTestAvps(
                created.Message,
                DiameterAvpCodes.AuthApplicationId), Is.EqualTo(2));
            Assert.That(repeated.Handled, Is.False);
            Assert.That(repeated.Code,
                Is.EqualTo("capabilities-update-already-pending"));
        });
    }

    [Test]
    public void ValidRequestAtomicallyUpdatesPermittedCapabilities()
    {
        CapabilitiesUpdateTestContext context =
            CreateCapabilitiesUpdateTestContext();
        DiameterPeerCapabilities updated =
            CreateCapabilitiesUpdateTestPeerCapabilities();
        updated.HostAddresses.Clear();
        updated.HostAddresses.Add(IPAddress.Parse("2001:db8::20"));
        updated.VendorId = 5535;
        updated.SupportedVendorIds.Clear();
        updated.SupportedVendorIds.Add(5535);
        updated.ProductName = "updated-peer";
        updated.FirmwareRevision = 9;
        updated.AuthApplicationIds.Add(5);
        DiameterApplicationId remoteVendorApplication =
            new DiameterApplicationId();
        remoteVendorApplication.Id = 88;
        remoteVendorApplication.VendorId = 5535;
        updated.VendorSpecificApplications.Add(remoteVendorApplication);
        DiameterApplicationId localVendorApplication =
            new DiameterApplicationId();
        localVendorApplication.Id = 88;
        localVendorApplication.VendorId = 10415;
        context.LocalCapabilities.VendorSpecificApplications.Add(
            localVendorApplication);
        DiameterMessage request =
            context.Controller.CreatePeerCapabilitiesUpdateRequestMessage(
                updated);

        PeerCapabilitiesUpdateResult result =
            context.Controller.HandlePeerCapabilitiesUpdateRequest(
                "peer.example",
                request,
                context.LocalCapabilities);
        FindCapabilitiesUpdateTestAvp(
            request,
            DiameterAvpCodes.ProductName).Data[0] = (byte)'x';
        updated.HostAddresses[0] = IPAddress.Parse("203.0.113.20");

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.True);
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.ResultCode, Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(result.Code, Is.EqualTo("capabilities-update-accepted"));
            Assert.That(result.Message, Is.Not.Null);
            Assert.That(result.Message!.Flags,
                Is.EqualTo(DiameterMessageFlags.None));
            Assert.That(result.Message.HopByHopIdentifier,
                Is.EqualTo(request.HopByHopIdentifier));
            Assert.That(result.Message.EndToEndIdentifier,
                Is.EqualTo(request.EndToEndIdentifier));
            Assert.That(ReadCapabilitiesUpdateTestUnsigned32(
                result.Message,
                DiameterAvpCodes.ResultCode),
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(result.CapabilityChange,
                Is.EqualTo((DiameterCapabilityChange)31));
            Assert.That(result.Peer.Capabilities.HostAddresses,
                Is.EqualTo(new[] { IPAddress.Parse("2001:db8::20") }));
            Assert.That(result.Peer.Capabilities.VendorId, Is.EqualTo(5535U));
            Assert.That(result.Peer.Capabilities.SupportedVendorIds,
                Is.EqualTo(new uint[] { 5535 }));
            Assert.That(result.Peer.Capabilities.ProductName,
                Is.EqualTo("updated-peer"));
            Assert.That(result.Peer.Capabilities.FirmwareRevision,
                Is.EqualTo(9U));
            Assert.That(result.Peer.Capabilities.AuthApplicationIds,
                Is.EqualTo(new uint[]
                {
                    DiameterApplicationIds.CapabilitiesUpdate, 4, 5
                }));
            Assert.That(result.Peer.Capabilities.CommonApplications,
                Has.Count.EqualTo(3));
            Assert.That(result.Peer.Capabilities.CommonApplications[2].Id,
                Is.EqualTo(88U));
            Assert.That(result.Peer.Capabilities.CommonApplications[2].VendorId,
                Is.EqualTo(10415U));
            Assert.That(result.Peer.Capabilities.InbandSecurityIds,
                Is.EqualTo(new uint[] { 1 }));
            Assert.That(result.DisconnectRequired, Is.False);
            Assert.That(result.SecurityRestartRequired, Is.False);
        });
    }

    [Test]
    public void SecurityMechanismChangeRequiresRestartAndPreservesSnapshot()
    {
        CapabilitiesUpdateTestContext context =
            CreateCapabilitiesUpdateTestContext();
        DiameterPeerCapabilities updated =
            CreateCapabilitiesUpdateTestPeerCapabilities();
        updated.ProductName = "must-not-commit";
        DiameterMessage request =
            context.Controller.CreatePeerCapabilitiesUpdateRequestMessage(
                updated);
        request.Avps.Add(CreateCapabilitiesUpdateTestUnsigned32Avp(
            DiameterAvpCodes.InbandSecurityId,
            0));

        PeerCapabilitiesUpdateResult result =
            context.Controller.HandlePeerCapabilitiesUpdateRequest(
                "peer.example",
                request,
                context.LocalCapabilities);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.True);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.SecurityRestartRequired, Is.True);
            Assert.That(result.DisconnectRequired, Is.True);
            Assert.That(result.CapabilityChange,
                Is.EqualTo(DiameterCapabilityChange.SecurityMechanisms));
            Assert.That(result.ResultCode,
                Is.EqualTo(DiameterResultCodes.UnableToComply));
            Assert.That(result.Code,
                Is.EqualTo("capabilities-update-security-restart-required"));
            Assert.That(ReadCapabilitiesUpdateTestUnsigned32(
                result.Message!,
                DiameterAvpCodes.ResultCode),
                Is.EqualTo(DiameterResultCodes.UnableToComply));
            Assert.That(result.Peer.Capabilities.ProductName,
                Is.EqualTo("peer-node"));
            Assert.That(result.Peer.Capabilities.InbandSecurityIds,
                Is.EqualTo(new uint[] { 1 }));
        });
    }

    [Test]
    public void NoCommonApplicationReturnsFailureWithoutPartialUpdate()
    {
        CapabilitiesUpdateTestContext context =
            CreateCapabilitiesUpdateTestContext();
        DiameterPeerCapabilities updated =
            CreateCapabilitiesUpdateTestPeerCapabilities();
        updated.ProductName = "must-not-commit";
        updated.AuthApplicationIds.Clear();
        updated.AuthApplicationIds.Add(99);
        DiameterMessage request =
            context.Controller.CreatePeerCapabilitiesUpdateRequestMessage(
                updated);

        PeerCapabilitiesUpdateResult result =
            context.Controller.HandlePeerCapabilitiesUpdateRequest(
                "peer.example",
                request,
                context.LocalCapabilities);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.True);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.DisconnectRequired, Is.True);
            Assert.That(result.ResultCode,
                Is.EqualTo(DiameterResultCodes.NoCommonApplication));
            Assert.That(result.Code,
                Is.EqualTo("capabilities-update-no-common-application"));
            Assert.That(ReadCapabilitiesUpdateTestUnsigned32(
                result.Message!,
                DiameterAvpCodes.ResultCode),
                Is.EqualTo(DiameterResultCodes.NoCommonApplication));
            Assert.That(result.Peer.Capabilities.ProductName,
                Is.EqualTo("peer-node"));
            Assert.That(result.Peer.Capabilities.AuthApplicationIds,
                Is.EqualTo(new uint[]
                {
                    DiameterApplicationIds.CapabilitiesUpdate, 4
                }));
        });
    }

    [Test]
    public void InvalidRequestDoesNotMutatePeerOrProduceSuccessAnswer()
    {
        CapabilitiesUpdateTestContext context =
            CreateCapabilitiesUpdateTestContext();
        DiameterMessage request =
            context.Controller.CreatePeerCapabilitiesUpdateRequestMessage(
                CreateCapabilitiesUpdateTestPeerCapabilities());
        request.Avps.Remove(FindCapabilitiesUpdateTestAvp(
            request,
            DiameterAvpCodes.HostIpAddress));

        PeerCapabilitiesUpdateResult result =
            context.Controller.HandlePeerCapabilitiesUpdateRequest(
                "peer.example",
                request,
                context.LocalCapabilities);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.False);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Validation.Status,
                Is.EqualTo(DiameterValidationStatus.Invalid));
            Assert.That(result.Code,
                Is.EqualTo("invalid-capabilities-update-request"));
            Assert.That(result.Message, Is.Null);
            Assert.That(result.Peer.Capabilities.ProductName,
                Is.EqualTo("peer-node"));
        });
    }

    [Test]
    public void OnlyCorrelatedAnswerCompletesPendingUpdate()
    {
        CapabilitiesUpdateTestContext context =
            CreateCapabilitiesUpdateTestContext();
        PeerCapabilitiesUpdateResult started =
            context.Controller.BeginPeerCapabilitiesUpdate(
                "peer.example",
                context.LocalCapabilities);
        DiameterMessage uncorrelated =
            context.Controller.CreatePeerCapabilitiesUpdateAnswerMessage(
                started.Message!,
                DiameterResultCodes.Success,
                CreateCapabilitiesUpdateTestPeerCapabilities());
        uncorrelated.HopByHopIdentifier =
            uncorrelated.HopByHopIdentifier + 1;
        DiameterMessage correlated =
            context.Controller.CreatePeerCapabilitiesUpdateAnswerMessage(
                started.Message!,
                DiameterResultCodes.Success,
                CreateCapabilitiesUpdateTestPeerCapabilities());

        PeerCapabilitiesUpdateResult ignored =
            context.Controller.ProcessPeerCapabilitiesUpdateAnswer(
                "peer.example",
                uncorrelated);
        PeerCapabilitiesUpdateResult accepted =
            context.Controller.ProcessPeerCapabilitiesUpdateAnswer(
                "peer.example",
                correlated);

        Assert.Multiple(() =>
        {
            Assert.That(ignored.Handled, Is.False);
            Assert.That(ignored.Code,
                Is.EqualTo("capabilities-update-answer-not-correlated"));
            Assert.That(accepted.Handled, Is.True);
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(accepted.ResultCode,
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(accepted.Code,
                Is.EqualTo("capabilities-update-answer-accepted"));
        });
    }

    [Test]
    public void NoCommonApplicationAnswerRequiresConnectionClosure()
    {
        CapabilitiesUpdateTestContext context =
            CreateCapabilitiesUpdateTestContext();
        PeerCapabilitiesUpdateResult started =
            context.Controller.BeginPeerCapabilitiesUpdate(
                "peer.example",
                context.LocalCapabilities);
        DiameterMessage answer =
            context.Controller.CreatePeerCapabilitiesUpdateAnswerMessage(
                started.Message!,
                DiameterResultCodes.NoCommonApplication,
                CreateCapabilitiesUpdateTestPeerCapabilities());

        PeerCapabilitiesUpdateResult result =
            context.Controller.ProcessPeerCapabilitiesUpdateAnswer(
                "peer.example",
                answer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.True);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.DisconnectRequired, Is.True);
            Assert.That(result.ResultCode,
                Is.EqualTo(DiameterResultCodes.NoCommonApplication));
            Assert.That(result.Code,
                Is.EqualTo("capabilities-update-answer-no-common-application"));
        });
    }

    [Test]
    public void ClosedOrUnsupportedPeerCannotStartUpdate()
    {
        CapabilitiesUpdateTestContext closed =
            CreateCapabilitiesUpdateTestContext();
        closed.Table.TransitionPeerTableState(
            "peer.example",
            DiameterPeerState.Closed);
        CapabilitiesUpdateTestContext unsupported =
            CreateCapabilitiesUpdateTestContext();
        DiameterPeerCapabilities peer =
            CreateCapabilitiesUpdateTestPeerCapabilities();
        peer.AuthApplicationIds.Remove(
            DiameterApplicationIds.CapabilitiesUpdate);
        unsupported.Table.UpdatePeerTableCapabilities("peer.example", peer);

        PeerCapabilitiesUpdateResult closedResult =
            closed.Controller.BeginPeerCapabilitiesUpdate(
                "peer.example",
                closed.LocalCapabilities);
        PeerCapabilitiesUpdateResult unsupportedResult =
            unsupported.Controller.BeginPeerCapabilitiesUpdate(
                "peer.example",
                unsupported.LocalCapabilities);

        Assert.Multiple(() =>
        {
            Assert.That(closedResult.Handled, Is.False);
            Assert.That(closedResult.Code,
                Is.EqualTo("capabilities-update-peer-not-open"));
            Assert.That(unsupportedResult.Handled, Is.False);
            Assert.That(unsupportedResult.Code,
                Is.EqualTo("capabilities-update-not-supported"));
        });
    }

    private static CapabilitiesUpdateTestContext
        CreateCapabilitiesUpdateTestContext()
    {
        CapabilitiesUpdateTestContext result =
            new CapabilitiesUpdateTestContext();
        DiameterPeerConfiguration configuration =
            new DiameterPeerConfiguration();
        configuration.Identity.Value = "peer.example";
        configuration.Realm.Value = "example";
        DiameterEndpoint endpoint = new DiameterEndpoint();
        endpoint.Identity.Value = "peer.example";
        endpoint.Host = "peer.example";
        endpoint.Address = IPAddress.Parse("192.0.2.10");
        endpoint.Port = DiameterProtocolConstants.Port;
        endpoint.Transport = DiameterTransportKind.Tcp;
        configuration.Endpoints.Add(endpoint);
        result.Table = new PeerTable(
            result.Clock,
            new DiameterTimerConfiguration(),
            1);
        result.Table.AddOrUpdatePeerConfiguration(configuration);
        result.Table.UpdatePeerTableCapabilities(
            "peer.example",
            CreateCapabilitiesUpdateTestPeerCapabilities());
        DiameterConnectionMetadata connection = new DiameterConnectionMetadata();
        connection.ConnectionId = "capabilities-update-connection";
        connection.LocalEndpoint = new DiameterEndpoint();
        connection.LocalEndpoint.Address = IPAddress.Parse("192.0.2.1");
        connection.LocalEndpoint.Port = DiameterProtocolConstants.Port;
        connection.LocalEndpoint.Transport = DiameterTransportKind.Tcp;
        connection.RemoteEndpoint = endpoint;
        connection.Open = true;
        result.Table.AttachPeerTableConnection(
            "peer.example",
            endpoint,
            connection,
            DiameterConnectionDirection.Outbound);
        result.Table.TransitionPeerTableState(
            "peer.example",
            DiameterPeerState.InitiatorOpen);
        result.Controller = new PeerCapabilitiesUpdateController(
            result.Table,
            result.Identifiers,
            new DiameterLimitConfiguration());
        result.LocalCapabilities =
            CreateCapabilitiesUpdateTestLocalCapabilities();
        return result;
    }

    private static DiameterPeerCapabilities
        CreateCapabilitiesUpdateTestPeerCapabilities()
    {
        DiameterPeerCapabilities result = new DiameterPeerCapabilities();
        result.OriginHost.Value = "peer.example";
        result.OriginRealm.Value = "example";
        result.HostAddresses.Add(IPAddress.Parse("192.0.2.10"));
        result.VendorId = 10415;
        result.ProductName = "peer-node";
        result.FirmwareRevision = 7;
        result.SupportedVendorIds.Add(10415);
        result.AuthApplicationIds.Add(
            DiameterApplicationIds.CapabilitiesUpdate);
        result.AuthApplicationIds.Add(4);
        result.InbandSecurityIds.Add(1);
        return result;
    }

    private static DiameterPeerCapabilities
        CreateCapabilitiesUpdateTestLocalCapabilities()
    {
        DiameterPeerCapabilities result = new DiameterPeerCapabilities();
        result.OriginHost.Value = "node.example";
        result.OriginRealm.Value = "example";
        result.HostAddresses.Add(IPAddress.Parse("192.0.2.1"));
        result.VendorId = 10415;
        result.ProductName = "local-node";
        result.FirmwareRevision = 1;
        result.AuthApplicationIds.Add(
            DiameterApplicationIds.CapabilitiesUpdate);
        result.AuthApplicationIds.Add(4);
        result.InbandSecurityIds.Add(1);
        return result;
    }

    private static DiameterAvp CreateCapabilitiesUpdateTestUnsigned32Avp(
        uint code,
        uint value)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(result.Data, value);
        return result;
    }

    private static int CountCapabilitiesUpdateTestAvps(
        DiameterMessage message,
        uint code)
    {
        int result = 0;
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.Code == code && avp.VendorId == DiameterVendorIds.Ietf)
            {
                result = result + 1;
            }

            index = index + 1;
        }

        return result;
    }

    private static DiameterAvp FindCapabilitiesUpdateTestAvp(
        DiameterMessage message,
        uint code)
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

        throw new AssertionException("Expected capabilities-update AVP was not found.");
    }

    private static uint ReadCapabilitiesUpdateTestUnsigned32(
        DiameterMessage message,
        uint code)
    {
        return NetworkByteOrder.ReadUInt32(
            FindCapabilitiesUpdateTestAvp(message, code).Data);
    }
}

internal class CapabilitiesUpdateTestContext
{
    internal FakeDiscoveryClock Clock = new FakeDiscoveryClock();
    internal BaseCommandTestIdentifierSource Identifiers =
        new BaseCommandTestIdentifierSource();
    internal PeerTable Table = null!;
    internal PeerCapabilitiesUpdateController Controller = null!;
    internal DiameterPeerCapabilities LocalCapabilities =
        new DiameterPeerCapabilities();
}
