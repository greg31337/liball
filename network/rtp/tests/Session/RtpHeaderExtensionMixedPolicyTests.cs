using rtp;

namespace rtp.Tests.Session;

public class RtpHeaderExtensionMixedPolicyTests
{
    [Test]
    public void AutomaticSelectionUsesOneByteWhenAllMappingsPreferIt()
    {
        RtpHeaderExtension extension = CreateAutomaticExtension(
            CreateElement(1),
            CreateElement(2));
        RtpHeaderExtensionMapping[] mappings = new RtpHeaderExtensionMapping[]
        {
            CreateMapping(1, true, true, RtpHeaderExtensionFormat.OneByte),
            CreateMapping(2, true, true, RtpHeaderExtensionFormat.OneByte)
        };

        RtpHeaderExtension prepared = RtpOutboundPacketBuilder.PrepareMappedExtension(
            extension,
            mappings);

        Assert.Multiple(() =>
        {
            Assert.That(prepared.Format, Is.EqualTo(RtpHeaderExtensionFormat.OneByte));
            Assert.That(prepared.Profile, Is.EqualTo(RtpHeaderExtensionProfiles.OneByte));
        });
    }

    [Test]
    public void APreferredTwoByteMappingPromotesTheWholeCompatibleSet()
    {
        RtpHeaderExtension extension = CreateAutomaticExtension(
            CreateElement(1),
            CreateElement(2));
        RtpHeaderExtensionMapping[] mappings = new RtpHeaderExtensionMapping[]
        {
            CreateMapping(1, true, true, RtpHeaderExtensionFormat.OneByte),
            CreateMapping(2, true, true, RtpHeaderExtensionFormat.TwoByte)
        };

        RtpHeaderExtension prepared = RtpOutboundPacketBuilder.PrepareMappedExtension(
            extension,
            mappings);

        Assert.Multiple(() =>
        {
            Assert.That(prepared.Format, Is.EqualTo(RtpHeaderExtensionFormat.TwoByte));
            Assert.That(prepared.Profile, Is.EqualTo(RtpHeaderExtensionProfiles.TwoByte));
            Assert.That(prepared.Elements, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void OneByteOnlyMappingConstrainsADualFormatPreference()
    {
        RtpHeaderExtension extension = CreateAutomaticExtension(
            CreateElement(1),
            CreateElement(2));
        RtpHeaderExtensionMapping[] mappings = new RtpHeaderExtensionMapping[]
        {
            CreateMapping(1, true, false, RtpHeaderExtensionFormat.OneByte),
            CreateMapping(2, true, true, RtpHeaderExtensionFormat.TwoByte)
        };

        RtpHeaderExtension prepared = RtpOutboundPacketBuilder.PrepareMappedExtension(
            extension,
            mappings);

        Assert.That(prepared.Format, Is.EqualTo(RtpHeaderExtensionFormat.OneByte));
    }

    [Test]
    public void TwoByteOnlyMappingPromotesAnOtherwiseOneByteCompatibleSet()
    {
        RtpHeaderExtension extension = CreateAutomaticExtension(
            CreateElement(1),
            CreateElement(2));
        RtpHeaderExtensionMapping[] mappings = new RtpHeaderExtensionMapping[]
        {
            CreateMapping(1, false, true, RtpHeaderExtensionFormat.TwoByte),
            CreateMapping(2, true, true, RtpHeaderExtensionFormat.OneByte)
        };

        RtpHeaderExtension prepared = RtpOutboundPacketBuilder.PrepareMappedExtension(
            extension,
            mappings);

        Assert.That(prepared.Format, Is.EqualTo(RtpHeaderExtensionFormat.TwoByte));
    }

    [Test]
    public void IncompatibleOneByteOnlyAndTwoByteOnlyMappingsAreRejected()
    {
        RtpHeaderExtension extension = CreateAutomaticExtension(
            CreateElement(1),
            CreateElement(2));
        RtpHeaderExtensionMapping[] mappings = new RtpHeaderExtensionMapping[]
        {
            CreateMapping(1, true, false, RtpHeaderExtensionFormat.OneByte),
            CreateMapping(2, false, true, RtpHeaderExtensionFormat.TwoByte)
        };

        RtpSessionException exception = Assert.Throws<RtpSessionException>(() =>
            RtpOutboundPacketBuilder.PrepareMappedExtension(extension, mappings))!;

        Assert.That(exception.Message, Does.Contain("No negotiated format"));
    }

    [Test]
    public void AutomaticTwoByteRequirementsRejectOneByteOnlyMappings()
    {
        RtpHeaderExtensionElement zeroLength = CreateElement(1);
        zeroLength.Data = [];
        RtpHeaderExtension extension = CreateAutomaticExtension(zeroLength);
        RtpHeaderExtensionMapping[] mappings = new RtpHeaderExtensionMapping[]
        {
            CreateMapping(1, true, false, RtpHeaderExtensionFormat.OneByte)
        };

        RtpSessionException exception = Assert.Throws<RtpSessionException>(() =>
            RtpOutboundPacketBuilder.PrepareMappedExtension(extension, mappings))!;

        Assert.That(exception.Message, Does.Contain("No negotiated format"));
    }

    [Test]
    public void ExplicitFormatsRejectMappingsThatDoNotSupportThem()
    {
        RtpHeaderExtension oneByte = CreateAutomaticExtension(CreateElement(1));
        oneByte.Format = RtpHeaderExtensionFormat.OneByte;
        oneByte.Profile = RtpHeaderExtensionProfiles.OneByte;
        RtpHeaderExtension twoByte = CreateAutomaticExtension(CreateElement(2));
        twoByte.Format = RtpHeaderExtensionFormat.TwoByte;
        twoByte.Profile = RtpHeaderExtensionProfiles.TwoByte;
        RtpHeaderExtensionMapping[] oneByteOnly = new RtpHeaderExtensionMapping[]
        {
            CreateMapping(2, true, false, RtpHeaderExtensionFormat.OneByte)
        };
        RtpHeaderExtensionMapping[] twoByteOnly = new RtpHeaderExtensionMapping[]
        {
            CreateMapping(1, false, true, RtpHeaderExtensionFormat.TwoByte)
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpSessionException>(() =>
                RtpOutboundPacketBuilder.PrepareMappedExtension(oneByte, twoByteOnly));
            Assert.Throws<RtpSessionException>(() =>
                RtpOutboundPacketBuilder.PrepareMappedExtension(twoByte, oneByteOnly));
        });
    }

    [Test]
    public void TwoByteApplicationBitsSurviveMappedAutomaticSelection()
    {
        RtpHeaderExtension extension = CreateAutomaticExtension(CreateElement(1));
        extension.ApplicationBits = 15;
        RtpHeaderExtensionMapping[] mappings = new RtpHeaderExtensionMapping[]
        {
            CreateMapping(1, true, true, RtpHeaderExtensionFormat.OneByte)
        };

        RtpHeaderExtension prepared = RtpOutboundPacketBuilder.PrepareMappedExtension(
            extension,
            mappings);

        Assert.Multiple(() =>
        {
            Assert.That(prepared.Format, Is.EqualTo(RtpHeaderExtensionFormat.TwoByte));
            Assert.That(prepared.Profile, Is.EqualTo(0x100F));
            Assert.That(prepared.ApplicationBits, Is.EqualTo(15));
        });
    }

    static RtpHeaderExtension CreateAutomaticExtension(
        params RtpHeaderExtensionElement[] elements)
    {
        RtpHeaderExtension extension = new RtpHeaderExtension();
        extension.Elements = elements;
        return extension;
    }

    static RtpHeaderExtensionElement CreateElement(int identifier)
    {
        RtpHeaderExtensionElement element = new RtpHeaderExtensionElement();
        element.Identifier = identifier;
        element.Data = new byte[] { (byte)identifier };
        return element;
    }

    static RtpHeaderExtensionMapping CreateMapping(
        int identifier,
        bool supportsOneByte,
        bool supportsTwoByte,
        RtpHeaderExtensionFormat preferredFormat)
    {
        RtpHeaderExtensionMapping mapping = new RtpHeaderExtensionMapping();
        mapping.Identifier = identifier;
        mapping.Uri = $"urn:test:mixed-policy:{identifier}";
        mapping.SupportsOneByte = supportsOneByte;
        mapping.SupportsTwoByte = supportsTwoByte;
        mapping.PreferredFormat = preferredFormat;
        return mapping;
    }
}
