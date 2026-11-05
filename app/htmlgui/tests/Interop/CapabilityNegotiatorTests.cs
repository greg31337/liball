using htmlgui;

namespace htmlgui.Tests;

public class CapabilityNegotiatorTests
{
    [Test]
    public void NegotiatesDeterministicIntersectionAndFallsBackUnsupportedFeatures()
    {
        CapabilityNegotiator negotiator = new CapabilityNegotiator();
        UiTransportCapabilities local = Capabilities(
            new UiCapability { Name = "zeta", IsSupported = true, Version = "1" },
            new UiCapability { Name = "alpha", IsSupported = true },
            new UiCapability { Name = "menu", IsSupported = true, Version = "2" },
            new UiCapability { Name = "disabled", IsSupported = false });
        local.SupportsBinary = true;
        local.SupportsChunking = true;
        UiTransportCapabilities remote = Capabilities(
            new UiCapability { Name = "remote-only", IsSupported = true },
            new UiCapability { Name = "alpha", IsSupported = true, Version = "1" },
            new UiCapability { Name = "menu", IsSupported = true, Version = "3" },
            new UiCapability { Name = "zeta", IsSupported = false, Version = "1" },
            new UiCapability { Name = "disabled", IsSupported = true });
        remote.SupportsBinary = true;
        remote.SupportsChunking = false;

        CapabilityNegotiationResult result = negotiator.NegotiateBridgeCapabilities(local, remote);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Capabilities.Version, Is.EqualTo(BridgeProtocolVersions.Current));
            Assert.That(result.Capabilities.State, Is.EqualTo(UiTransportState.Ready));
            Assert.That(result.Capabilities.SupportsBinary, Is.True);
            Assert.That(result.Capabilities.SupportsChunking, Is.False);
            Assert.That(result.Capabilities.Capabilities.Select(item => item.Name), Is.EqualTo(new[] { "alpha", "disabled", "menu", "zeta" }));
            Assert.That(result.Capabilities.Capabilities[0].IsSupported, Is.True);
            Assert.That(result.Capabilities.Capabilities[0].Version, Is.EqualTo("1"));
            Assert.That(result.Capabilities.Capabilities[1].IsSupported, Is.False);
            Assert.That(result.Capabilities.Capabilities[2].IsSupported, Is.False);
            Assert.That(result.Capabilities.Capabilities[3].IsSupported, Is.False);
        });
    }

    [Test]
    public void RejectsInvalidAndDuplicateCapabilityDeclarations()
    {
        CapabilityNegotiator negotiator = new CapabilityNegotiator();
        UiTransportCapabilities duplicate = Capabilities(
            new UiCapability { Name = "same", IsSupported = true },
            new UiCapability { Name = "same", IsSupported = false });
        UiTransportCapabilities unsupportedVersion = Capabilities();
        unsupportedVersion.Version = BridgeProtocolVersions.Current + 1;
        UiTransportCapabilities invalidName = Capabilities(new UiCapability { Name = " ", IsSupported = true });

        Assert.Multiple(() =>
        {
            Assert.That(negotiator.ValidateBridgeCapabilities(null).Code, Is.EqualTo("bridge.null-capabilities"));
            Assert.That(negotiator.ValidateBridgeCapabilities(duplicate).Code, Is.EqualTo("bridge.duplicate-capability"));
            Assert.That(negotiator.ValidateBridgeCapabilities(unsupportedVersion).Code, Is.EqualTo("bridge.unsupported-capability-version"));
            Assert.That(negotiator.ValidateBridgeCapabilities(invalidName).Code, Is.EqualTo("bridge.invalid-capability"));
        });
    }

    [Test]
    public void HandshakePayloadRoundTripsAndRejectsUnknownMembers()
    {
        CapabilityNegotiator negotiator = new CapabilityNegotiator();
        UiTransportCapabilities capabilities = Capabilities(
            new UiCapability { Name = "binary", IsSupported = true, Version = "1" },
            new UiCapability { Name = "menu", IsSupported = false });
        capabilities.SupportsBinary = true;
        UiValue payload = negotiator.CreateBridgeHandshakePayload("nonce-1", capabilities);

        BridgeHandshakePayloadResult parsed = negotiator.ParseBridgeHandshakePayload(payload);
        payload.ObjectValue = payload.ObjectValue!.Append(new UiValueEntry { Key = "unexpected", Value = new UiValue() }).ToArray();
        BridgeHandshakePayloadResult unknown = negotiator.ParseBridgeHandshakePayload(payload);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Nonce, Is.EqualTo("nonce-1"));
            Assert.That(parsed.Capabilities.SupportsBinary, Is.True);
            Assert.That(parsed.Capabilities.Capabilities.Select(item => item.Name), Is.EqualTo(new[] { "binary", "menu" }));
            Assert.That(parsed.Capabilities.Capabilities[0].Version, Is.EqualTo("1"));
            Assert.That(parsed.Capabilities.Capabilities[1].Version, Is.Null);
            Assert.That(unknown.Code, Is.EqualTo("bridge.unknown-handshake-member"));
        });
    }

    private static UiTransportCapabilities Capabilities(params UiCapability[] capabilities)
    {
        return new UiTransportCapabilities
        {
            State = UiTransportState.Starting,
            Version = BridgeProtocolVersions.Current,
            Capabilities = capabilities
        };
    }
}
