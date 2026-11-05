using System;

namespace sdp.Tests.Api;

public class SdpEnumTests
{
    [TestCase(typeof(SdpParseMode))]
    [TestCase(typeof(SdpWriteMode))]
    [TestCase(typeof(SdpLineEnding))]
    [TestCase(typeof(SdpValidationProfile))]
    [TestCase(typeof(SdpValidationSeverity))]
    [TestCase(typeof(SdpScope))]
    [TestCase(typeof(SdpAttributeKind))]
    [TestCase(typeof(SdpMediaKind))]
    [TestCase(typeof(SdpAddressKind))]
    [TestCase(typeof(SdpDirection))]
    [TestCase(typeof(SdpIceCandidateType))]
    [TestCase(typeof(SdpIceTransportProtocol))]
    [TestCase(typeof(SdpIceTcpType))]
    [TestCase(typeof(SdpDtlsSetupRole))]
    [TestCase(typeof(SdpMuxCategory))]
    [TestCase(typeof(SdpGroupSemantic))]
    [TestCase(typeof(SdpCodecMatchMode))]
    [TestCase(typeof(SdpNegotiationRole))]
    [TestCase(typeof(SdpDifferenceKind))]
    [TestCase(typeof(SdpRejectionReason))]
    [TestCase(typeof(SdpClockSourceKind))]
    public void DefaultValue_IsUnknown(Type enumType)
    {
        Assert.That(Enum.GetName(enumType, 0), Is.EqualTo("Unknown"));
    }

    [Test]
    public void ParseModes_ExposeStrictInteroperableAndLosslessChoices()
    {
        Assert.That(SdpParseMode.Strict, Is.Not.EqualTo(SdpParseMode.Interoperable));
        Assert.That(SdpParseMode.Interoperable, Is.Not.EqualTo(SdpParseMode.Lossless));
    }

    [Test]
    public void AttributeKinds_PreservePropertyAndExplicitValueDistinction()
    {
        Assert.That(SdpAttributeKind.Property, Is.Not.EqualTo(SdpAttributeKind.Value));
    }

    [Test]
    public void IceEnums_RepresentAllStandardCandidateForms()
    {
        SdpIceCandidateType[] candidateTypes = Enum.GetValues<SdpIceCandidateType>();
        SdpIceTransportProtocol[] transports = Enum.GetValues<SdpIceTransportProtocol>();
        SdpIceTcpType[] tcpTypes = Enum.GetValues<SdpIceTcpType>();

        Assert.That(candidateTypes, Does.Contain(SdpIceCandidateType.Host));
        Assert.That(candidateTypes, Does.Contain(SdpIceCandidateType.ServerReflexive));
        Assert.That(candidateTypes, Does.Contain(SdpIceCandidateType.PeerReflexive));
        Assert.That(candidateTypes, Does.Contain(SdpIceCandidateType.Relayed));
        Assert.That(transports, Does.Contain(SdpIceTransportProtocol.Udp));
        Assert.That(transports, Does.Contain(SdpIceTransportProtocol.Tcp));
        Assert.That(tcpTypes, Does.Contain(SdpIceTcpType.Active));
        Assert.That(tcpTypes, Does.Contain(SdpIceTcpType.Passive));
        Assert.That(tcpTypes, Does.Contain(SdpIceTcpType.SimultaneousOpen));
    }

    [Test]
    public void MuxCategories_RepresentEveryRfc8859Category()
    {
        SdpMuxCategory[] categories = Enum.GetValues<SdpMuxCategory>();

        Assert.That(categories, Has.Length.EqualTo(8));
        Assert.That(categories, Does.Contain(SdpMuxCategory.Normal));
        Assert.That(categories, Does.Contain(SdpMuxCategory.Caution));
        Assert.That(categories, Does.Contain(SdpMuxCategory.Identical));
        Assert.That(categories, Does.Contain(SdpMuxCategory.Sum));
        Assert.That(categories, Does.Contain(SdpMuxCategory.Transport));
        Assert.That(categories, Does.Contain(SdpMuxCategory.IdenticalPerPayloadType));
        Assert.That(categories, Does.Contain(SdpMuxCategory.Special));
    }

    [Test]
    public void ClockSourceKinds_RepresentEveryRfc7273SourceForm()
    {
        SdpClockSourceKind[] sources = Enum.GetValues<SdpClockSourceKind>();

        Assert.That(sources, Has.Length.EqualTo(8));
        Assert.That(sources, Does.Contain(SdpClockSourceKind.Ntp));
        Assert.That(sources, Does.Contain(SdpClockSourceKind.Ptp));
        Assert.That(sources, Does.Contain(SdpClockSourceKind.Gps));
        Assert.That(sources, Does.Contain(SdpClockSourceKind.Galileo));
        Assert.That(sources, Does.Contain(SdpClockSourceKind.Glonass));
        Assert.That(sources, Does.Contain(SdpClockSourceKind.Local));
        Assert.That(sources, Does.Contain(SdpClockSourceKind.Private));
    }
}
