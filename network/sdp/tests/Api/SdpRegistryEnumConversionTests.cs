namespace sdp.Tests.Api;

public class SdpRegistryEnumConversionTests
{
    [Test]
    public void RegistryMetadata_MapsEveryMuxTokenAndUnknownValue()
    {
        ISdpRegistry registry = new SdpRegistry();

        SdpAttributeRegistration normal = registry.GetAttributeRegistration(SdpAttributeNames.BundleOnly);
        SdpAttributeRegistration caution = registry.GetAttributeRegistration(SdpAttributeNames.Control);
        SdpAttributeRegistration identical = registry.GetAttributeRegistration(SdpAttributeNames.ExtMapAllowMixed);
        SdpRegisteredToken sum = registry.GetBandwidthTypeRegistration(SdpBandwidthTypes.ApplicationSpecific);
        SdpAttributeRegistration transport = registry.GetAttributeRegistration(SdpAttributeNames.Candidate);
        SdpAttributeRegistration perPayload = registry.GetAttributeRegistration(SdpAttributeNames.FormatParameters);
        SdpAttributeRegistration special = registry.GetAttributeRegistration(SdpAttributeNames.ExtMap);
        SdpAttributeRegistration unknown = registry.GetAttributeRegistration("x-future-mux-category");

        Assert.Multiple(() =>
        {
            Assert.That(normal.MuxCategory, Is.EqualTo(SdpMuxCategory.Normal));
            Assert.That(normal.MuxCategoryToken, Is.EqualTo("NORMAL"));
            Assert.That(caution.MuxCategory, Is.EqualTo(SdpMuxCategory.Caution));
            Assert.That(caution.MuxCategoryToken, Is.EqualTo("CAUTION"));
            Assert.That(identical.MuxCategory, Is.EqualTo(SdpMuxCategory.Identical));
            Assert.That(identical.MuxCategoryToken, Is.EqualTo("IDENTICAL"));
            Assert.That(sum.MuxCategory, Is.EqualTo(SdpMuxCategory.Sum));
            Assert.That(sum.MuxCategoryToken, Is.EqualTo("SUM"));
            Assert.That(transport.MuxCategory, Is.EqualTo(SdpMuxCategory.Transport));
            Assert.That(transport.MuxCategoryToken, Is.EqualTo("TRANSPORT"));
            Assert.That(perPayload.MuxCategory, Is.EqualTo(SdpMuxCategory.IdenticalPerPayloadType));
            Assert.That(perPayload.MuxCategoryToken, Is.EqualTo("IDENTICAL-PER-PT"));
            Assert.That(special.MuxCategory, Is.EqualTo(SdpMuxCategory.Special));
            Assert.That(special.MuxCategoryToken, Is.EqualTo("SPECIAL"));
            Assert.That(unknown.MuxCategory, Is.EqualTo(SdpMuxCategory.Unknown));
            Assert.That(unknown.MuxCategoryToken, Is.Empty);
        });
    }

    [Test]
    public void RegistryMetadata_MapsEveryUsageScopeTokenAndUnknownValue()
    {
        ISdpRegistry registry = new SdpRegistry();

        SdpAttributeRegistration session = registry.GetAttributeRegistration(SdpAttributeNames.Category);
        SdpAttributeRegistration media = registry.GetAttributeRegistration(SdpAttributeNames.Candidate);
        SdpAttributeRegistration source = registry.GetAttributeRegistration("cname");
        SdpAttributeRegistration combined = registry.GetAttributeRegistration(SdpAttributeNames.FormatParameters);
        SdpAttributeRegistration unknown = registry.GetAttributeRegistration("x-future-scope");

        Assert.Multiple(() =>
        {
            Assert.That(session.Scopes, Is.EqualTo(new[] { SdpScope.Session }));
            Assert.That(media.Scopes, Is.EqualTo(new[] { SdpScope.Media }));
            Assert.That(source.Scopes, Is.EqualTo(new[] { SdpScope.Source }));
            Assert.That(combined.Scopes, Is.EqualTo(new[] { SdpScope.Media, SdpScope.Source }));
            Assert.That(unknown.Scopes, Is.Empty);
        });
    }
}
