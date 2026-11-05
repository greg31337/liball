using rtp;

namespace rtp.Tests.Api;

public class RtpHeaderExtensionRegistryTests
{
    [Test]
    public void RegistryStoresAndReturnsIndependentMappingCopies()
    {
        IRtpHeaderExtensionRegistry registry = new RtpHeaderExtensionRegistry();
        RtpHeaderExtensionMapping mapping = CreateOneByteMapping(
            3,
            "urn:ietf:params:rtp-hdrext:ssrc-audio-level");
        mapping.Attributes = "vad=on";

        registry.SetHeaderExtensionMapping(mapping);
        mapping.Identifier = 4;
        mapping.Uri = "changed";
        mapping.SupportsOneByte = false;
        mapping.Attributes = "changed";
        RtpHeaderExtensionMapping? byIdentifier =
            registry.FindHeaderExtensionMapping(3);
        RtpHeaderExtensionMapping? byUri = registry.FindHeaderExtensionMappingByUri(
            "urn:ietf:params:rtp-hdrext:ssrc-audio-level");
        if (byIdentifier != null)
        {
            byIdentifier.Uri = "mutated-copy";
            byIdentifier.Attributes = "mutated-copy";
        }

        RtpHeaderExtensionMapping? freshCopy =
            registry.FindHeaderExtensionMapping(3);

        Assert.Multiple(() =>
        {
            Assert.That(byIdentifier, Is.Not.Null);
            Assert.That(byUri?.Identifier, Is.EqualTo(3));
            Assert.That(byUri?.SupportsOneByte, Is.True);
            Assert.That(byUri?.SupportsTwoByte, Is.True);
            Assert.That(byUri?.PreferredFormat, Is.EqualTo(RtpHeaderExtensionFormat.OneByte));
            Assert.That(freshCopy?.Uri,
                Is.EqualTo("urn:ietf:params:rtp-hdrext:ssrc-audio-level"));
            Assert.That(freshCopy?.Attributes, Is.EqualTo("vad=on"));
            Assert.That(registry.FindHeaderExtensionMappingByUri(
                "URN:IETF:PARAMS:RTP-HDREXT:SSRC-AUDIO-LEVEL"), Is.Null);
        });
    }

    [Test]
    public void RegistryReturnsIdentifierOrderedIndependentSnapshots()
    {
        IRtpHeaderExtensionRegistry registry = new RtpHeaderExtensionRegistry();
        registry.SetHeaderExtensionMapping(CreateTwoByteMapping(200, "urn:test:wide"));
        registry.SetHeaderExtensionMapping(CreateOneByteMapping(7, "urn:test:seven"));
        registry.SetHeaderExtensionMapping(CreateOneByteMapping(2, "urn:test:two"));

        RtpHeaderExtensionMapping[] snapshot = registry.GetHeaderExtensionMappings();
        snapshot[0].Identifier = 99;
        snapshot[1].Uri = "changed";
        RtpHeaderExtensionMapping[] freshSnapshot = registry.GetHeaderExtensionMappings();

        Assert.Multiple(() =>
        {
            Assert.That(freshSnapshot.Length, Is.EqualTo(3));
            Assert.That(freshSnapshot[0].Identifier, Is.EqualTo(2));
            Assert.That(freshSnapshot[1].Identifier, Is.EqualTo(7));
            Assert.That(freshSnapshot[2].Identifier, Is.EqualTo(200));
            Assert.That(freshSnapshot[1].Uri, Is.EqualTo("urn:test:seven"));
            Assert.That(freshSnapshot, Is.Not.SameAs(snapshot));
            Assert.That(freshSnapshot[0], Is.Not.SameAs(snapshot[0]));
        });
    }

    [Test]
    public void RegistryReplacesByIdentifierAndRemovesTheOldUriLookup()
    {
        IRtpHeaderExtensionRegistry registry = new RtpHeaderExtensionRegistry();
        registry.SetHeaderExtensionMapping(CreateOneByteMapping(4, "urn:test:old"));
        RtpHeaderExtensionMapping replacement =
            CreateTwoByteMapping(4, "urn:test:new");
        replacement.Attributes = "updated";

        registry.SetHeaderExtensionMapping(replacement);

        RtpHeaderExtensionMapping? stored = registry.FindHeaderExtensionMapping(4);
        Assert.Multiple(() =>
        {
            Assert.That(registry.GetHeaderExtensionMappings().Length, Is.EqualTo(1));
            Assert.That(registry.FindHeaderExtensionMappingByUri("urn:test:old"), Is.Null);
            Assert.That(registry.FindHeaderExtensionMappingByUri("urn:test:new"), Is.Not.Null);
            Assert.That(stored?.SupportsOneByte, Is.False);
            Assert.That(stored?.SupportsTwoByte, Is.True);
            Assert.That(stored?.PreferredFormat, Is.EqualTo(RtpHeaderExtensionFormat.TwoByte));
            Assert.That(stored?.Attributes, Is.EqualTo("updated"));
        });
    }

    [Test]
    public void ConflictingUriUpdateFailsWithoutChangingRegistryState()
    {
        IRtpHeaderExtensionRegistry registry = new RtpHeaderExtensionRegistry();
        registry.SetHeaderExtensionMapping(CreateOneByteMapping(1, "urn:test:first"));
        registry.SetHeaderExtensionMapping(CreateOneByteMapping(2, "urn:test:second"));
        RtpHeaderExtensionMapping conflict =
            CreateOneByteMapping(2, "urn:test:first");

        Assert.Throws<RtpConfigurationException>(() =>
            registry.SetHeaderExtensionMapping(conflict));

        Assert.Multiple(() =>
        {
            Assert.That(registry.GetHeaderExtensionMappings().Length, Is.EqualTo(2));
            Assert.That(registry.FindHeaderExtensionMapping(1)?.Uri,
                Is.EqualTo("urn:test:first"));
            Assert.That(registry.FindHeaderExtensionMapping(2)?.Uri,
                Is.EqualTo("urn:test:second"));
        });
    }

    [Test]
    public void RegistryValidatesOperationsAndReportsRemovalOutcomes()
    {
        IRtpHeaderExtensionRegistry registry = new RtpHeaderExtensionRegistry();
        RtpHeaderExtensionMapping noCapabilities = new RtpHeaderExtensionMapping();
        noCapabilities.Identifier = 1;
        noCapabilities.Uri = "urn:test:none";
        noCapabilities.PreferredFormat = RtpHeaderExtensionFormat.OneByte;
        registry.SetHeaderExtensionMapping(CreateTwoByteMapping(200, "urn:test:remove"));

        bool removed = registry.RemoveHeaderExtensionMapping(200);
        bool absent = registry.RemoveHeaderExtensionMapping(200);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(absent, Is.False);
            Assert.That(registry.GetHeaderExtensionMappings(), Is.Empty);
            Assert.Throws<RtpConfigurationException>(() =>
                registry.SetHeaderExtensionMapping(noCapabilities));
            Assert.Throws<RtpConfigurationException>(() =>
                registry.SetHeaderExtensionMapping(null!));
            Assert.Throws<RtpConfigurationException>(() =>
                registry.FindHeaderExtensionMapping(0));
            Assert.Throws<RtpConfigurationException>(() =>
                registry.RemoveHeaderExtensionMapping(256));
            Assert.Throws<RtpConfigurationException>(() =>
                registry.FindHeaderExtensionMappingByUri(""));
        });
    }

    [Test]
    public void MissingLookupsAndRemovalFromMultipleMappingsPreserveTheRemainder()
    {
        IRtpHeaderExtensionRegistry registry = new RtpHeaderExtensionRegistry();
        registry.SetHeaderExtensionMapping(CreateOneByteMapping(1, "urn:test:first"));
        registry.SetHeaderExtensionMapping(CreateOneByteMapping(2, "urn:test:second"));
        registry.SetHeaderExtensionMapping(CreateTwoByteMapping(3, "urn:test:third"));

        RtpHeaderExtensionMapping? missingIdentifier =
            registry.FindHeaderExtensionMapping(255);
        RtpHeaderExtensionMapping? missingUri =
            registry.FindHeaderExtensionMappingByUri("urn:test:missing");
        bool removed = registry.RemoveHeaderExtensionMapping(2);
        RtpHeaderExtensionMapping[] remaining =
            registry.GetHeaderExtensionMappings();

        Assert.Multiple(() =>
        {
            Assert.That(missingIdentifier, Is.Null);
            Assert.That(missingUri, Is.Null);
            Assert.That(removed, Is.True);
            Assert.That(remaining.Select(mapping => mapping.Identifier),
                Is.EqualTo(new[] { 1, 3 }));
        });
    }

    static RtpHeaderExtensionMapping CreateOneByteMapping(int identifier, string uri)
    {
        RtpHeaderExtensionMapping mapping = new RtpHeaderExtensionMapping();
        mapping.Identifier = identifier;
        mapping.Uri = uri;
        mapping.SupportsOneByte = true;
        mapping.SupportsTwoByte = true;
        mapping.PreferredFormat = RtpHeaderExtensionFormat.OneByte;
        return mapping;
    }

    static RtpHeaderExtensionMapping CreateTwoByteMapping(int identifier, string uri)
    {
        RtpHeaderExtensionMapping mapping = new RtpHeaderExtensionMapping();
        mapping.Identifier = identifier;
        mapping.Uri = uri;
        mapping.SupportsTwoByte = true;
        mapping.PreferredFormat = RtpHeaderExtensionFormat.TwoByte;
        return mapping;
    }
}
