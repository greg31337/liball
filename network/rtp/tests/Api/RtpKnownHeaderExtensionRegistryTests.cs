using rtp;

namespace rtp.Tests.Api;

public class RtpKnownHeaderExtensionRegistryTests
{
    [Test]
    public void RegistryPreservesMappingsForEveryKnownExtensionUri()
    {
        IRtpHeaderExtensionRegistry registry = new RtpHeaderExtensionRegistry();
        string[] uris = new string[]
        {
            RtpHeaderExtensionUris.Ntp64,
            RtpHeaderExtensionUris.SsrcAudioLevel,
            RtpHeaderExtensionUris.CsrcAudioLevel,
            RtpHeaderExtensionUris.MediaIdentifier,
            RtpHeaderExtensionUris.RtpStreamIdentifier,
            RtpHeaderExtensionUris.RepairedRtpStreamIdentifier
        };
        int uriIndex = 0;
        while (uriIndex < uris.Length)
        {
            RtpHeaderExtensionMapping mapping = CreateMapping(
                uriIndex + 1,
                uris[uriIndex]);
            if (string.Equals(
                mapping.Uri,
                RtpHeaderExtensionUris.SsrcAudioLevel,
                StringComparison.Ordinal))
            {
                mapping.Attributes = "vad=off";
            }

            registry.SetHeaderExtensionMapping(mapping);
            uriIndex = uriIndex + 1;
        }

        RtpHeaderExtensionMapping[] snapshot =
            registry.GetHeaderExtensionMappings();

        Assert.That(snapshot, Has.Length.EqualTo(uris.Length));
        uriIndex = 0;
        while (uriIndex < uris.Length)
        {
            int identifier = uriIndex + 1;
            RtpHeaderExtensionMapping? byIdentifier =
                registry.FindHeaderExtensionMapping(identifier);
            RtpHeaderExtensionMapping? byUri =
                registry.FindHeaderExtensionMappingByUri(uris[uriIndex]);

            Assert.Multiple(() =>
            {
                Assert.That(snapshot[uriIndex].Identifier, Is.EqualTo(identifier));
                Assert.That(snapshot[uriIndex].Uri, Is.EqualTo(uris[uriIndex]));
                Assert.That(byIdentifier?.Uri, Is.EqualTo(uris[uriIndex]));
                Assert.That(byUri?.Identifier, Is.EqualTo(identifier));
                Assert.That(byUri?.SupportsOneByte, Is.True);
                Assert.That(byUri?.SupportsTwoByte, Is.True);
                Assert.That(byUri?.PreferredFormat,
                    Is.EqualTo(RtpHeaderExtensionFormat.OneByte));
            });
            uriIndex = uriIndex + 1;
        }

        Assert.That(registry.FindHeaderExtensionMappingByUri(
            RtpHeaderExtensionUris.SsrcAudioLevel)?.Attributes,
            Is.EqualTo("vad=off"));
    }

    [Test]
    public void RegistryPreservesUnknownUrisAndTheirSignalingAttributes()
    {
        IRtpHeaderExtensionRegistry registry = new RtpHeaderExtensionRegistry();
        RtpHeaderExtensionMapping mapping = new RtpHeaderExtensionMapping();
        mapping.Identifier = 200;
        mapping.Uri = "urn:example:future-extension";
        mapping.SupportsTwoByte = true;
        mapping.PreferredFormat = RtpHeaderExtensionFormat.TwoByte;
        mapping.Attributes = "future=value";

        registry.SetHeaderExtensionMapping(mapping);
        mapping.Uri = "changed";
        mapping.Attributes = "changed";
        RtpHeaderExtensionMapping? stored =
            registry.FindHeaderExtensionMapping(200);

        Assert.Multiple(() =>
        {
            Assert.That(stored?.Uri, Is.EqualTo("urn:example:future-extension"));
            Assert.That(stored?.Attributes, Is.EqualTo("future=value"));
            Assert.That(stored?.SupportsOneByte, Is.False);
            Assert.That(stored?.SupportsTwoByte, Is.True);
            Assert.That(stored?.PreferredFormat,
                Is.EqualTo(RtpHeaderExtensionFormat.TwoByte));
            Assert.That(registry.FindHeaderExtensionMappingByUri(
                "urn:example:future-extension")?.Identifier,
                Is.EqualTo(200));
        });
    }

    static RtpHeaderExtensionMapping CreateMapping(int identifier, string uri)
    {
        RtpHeaderExtensionMapping mapping = new RtpHeaderExtensionMapping();
        mapping.Identifier = identifier;
        mapping.Uri = uri;
        mapping.SupportsOneByte = true;
        mapping.SupportsTwoByte = true;
        mapping.PreferredFormat = RtpHeaderExtensionFormat.OneByte;
        return mapping;
    }
}
