using System.Buffers.Binary;
using System.Text;
using ttf;

namespace ttf.Tests;

internal class ContainerDetectorTests
{
    [TestCase(new byte[] { 0x00, 0x01, 0x00, 0x00 }, ContainerSignatureKind.SfntVersion1)]
    [TestCase(new byte[] { 0x4F, 0x54, 0x54, 0x4F }, ContainerSignatureKind.SfntOpenType)]
    [TestCase(new byte[] { 0x74, 0x72, 0x75, 0x65 }, ContainerSignatureKind.SfntAppleTrueType)]
    [TestCase(new byte[] { 0x74, 0x79, 0x70, 0x31 }, ContainerSignatureKind.SfntType1)]
    [TestCase(new byte[] { 0x74, 0x74, 0x63, 0x66 }, ContainerSignatureKind.Collection)]
    [TestCase(new byte[] { 0x77, 0x4F, 0x46, 0x46 }, ContainerSignatureKind.Woff1)]
    [TestCase(new byte[] { 0x77, 0x4F, 0x46, 0x32 }, ContainerSignatureKind.Woff2)]
    public void DetectsEverySupportedSignature(byte[] signature, ContainerSignatureKind expected)
    {
        Assert.That(ContainerDetector.Detect(signature), Is.EqualTo(expected));
    }

    [Test]
    public void DetectionUsesContentAndIgnoresTrailingBytes()
    {
        byte[] data = new byte[] { 0x77, 0x4F, 0x46, 0x32, 1, 2, 3, 4 };

        Assert.That(ContainerDetector.Detect(data), Is.EqualTo(ContainerSignatureKind.Woff2));
    }

    [TestCase(ContainerSignatureKind.SfntVersion1, FontContainerKind.Sfnt)]
    [TestCase(ContainerSignatureKind.SfntOpenType, FontContainerKind.Sfnt)]
    [TestCase(ContainerSignatureKind.SfntAppleTrueType, FontContainerKind.Sfnt)]
    [TestCase(ContainerSignatureKind.SfntType1, FontContainerKind.Sfnt)]
    [TestCase(ContainerSignatureKind.Collection, FontContainerKind.Collection)]
    [TestCase(ContainerSignatureKind.Woff1, FontContainerKind.Woff1)]
    [TestCase(ContainerSignatureKind.Woff2, FontContainerKind.Woff2)]
    public void MapsSupportedSignaturesToPublicContainerKind(
        ContainerSignatureKind signature,
        FontContainerKind expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(ContainerDetector.IsSupported(signature), Is.True);
            Assert.That(ContainerDetector.GetContainerKind(signature), Is.EqualTo(expected));
        });
    }

    [TestCase(new byte[] { 0x80, 0x01 })]
    [TestCase(new byte[] { 0x80, 0x02, 0, 0, 0, 0 })]
    public void DetectsUnsupportedType1PfbSegments(byte[] data)
    {
        AssertUnsupported(data, ContainerSignatureKind.UnsupportedType1Pfb);
    }

    [TestCase("%!PS-AdobeFont-1.0: Tiny 1.0")]
    [TestCase("%!FontType1-1.1: Tiny")]
    public void DetectsUnsupportedType1PfaHeaders(string text)
    {
        AssertUnsupported(Encoding.ASCII.GetBytes(text), ContainerSignatureKind.UnsupportedType1Pfa);
    }

    [Test]
    public void DetectsUnsupportedPcfBitmapSignature()
    {
        AssertUnsupported(
            new byte[] { 0x01, 0x66, 0x63, 0x70 },
            ContainerSignatureKind.UnsupportedBitmapPcf);
    }

    [Test]
    public void DetectsUnsupportedBdfBitmapHeader()
    {
        AssertUnsupported(
            Encoding.ASCII.GetBytes("STARTFONT 2.1\n"),
            ContainerSignatureKind.UnsupportedBitmapBdf);
    }

    [TestCase(0x00010000U)]
    [TestCase(0x00020001U)]
    [TestCase(0x00020002U)]
    public void DetectsDefinedEmbeddedOpenTypeVersions(uint version)
    {
        byte[] data = CreateEotHeader(version, 100, 20, 0x504C);

        AssertUnsupported(data, ContainerSignatureKind.UnsupportedEmbeddedOpenType);
    }

    [Test]
    public void EmbeddedOpenTypeDetectionAcceptsTruncatedBodyAfterCompleteHeader()
    {
        byte[] data = CreateEotHeader(0x00020001U, 1000, 900, 0x504C);

        AssertUnsupported(data, ContainerSignatureKind.UnsupportedEmbeddedOpenType);
    }

    [Test]
    public void EmbeddedOpenTypeRequiresCompletePlausibleHeader()
    {
        byte[] valid = CreateEotHeader(0x00010000U, 100, 20, 0x504C);

        Assert.Multiple(() =>
        {
            Assert.That(ContainerDetector.Detect(valid.AsSpan(0, 35)), Is.EqualTo(ContainerSignatureKind.Unknown));
            Assert.That(
                ContainerDetector.Detect(CreateEotHeader(0x00030000U, 100, 20, 0x504C)),
                Is.EqualTo(ContainerSignatureKind.Unknown));
            Assert.That(
                ContainerDetector.Detect(CreateEotHeader(0x00010000U, 35, 20, 0x504C)),
                Is.EqualTo(ContainerSignatureKind.Unknown));
            Assert.That(
                ContainerDetector.Detect(CreateEotHeader(0x00010000U, 100, 101, 0x504C)),
                Is.EqualTo(ContainerSignatureKind.Unknown));
            Assert.That(
                ContainerDetector.Detect(CreateEotHeader(0x00010000U, 100, 20, 0x0000)),
                Is.EqualTo(ContainerSignatureKind.Unknown));
        });
    }

    [TestCase(new byte[] { })]
    [TestCase(new byte[] { 0x00 })]
    [TestCase(new byte[] { 0x00, 0x01 })]
    [TestCase(new byte[] { 0x00, 0x01, 0x00 })]
    [TestCase(new byte[] { 0x77, 0x4F, 0x46 })]
    public void PartialSupportedSignaturesRemainUnknown(byte[] data)
    {
        Assert.That(ContainerDetector.Detect(data), Is.EqualTo(ContainerSignatureKind.Unknown));
    }

    [Test]
    public void SignaturesAreCaseSensitive()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                ContainerDetector.Detect(Encoding.ASCII.GetBytes("otto")),
                Is.EqualTo(ContainerSignatureKind.Unknown));
            Assert.That(
                ContainerDetector.Detect(Encoding.ASCII.GetBytes("woff")),
                Is.EqualTo(ContainerSignatureKind.Unknown));
            Assert.That(
                ContainerDetector.Detect(Encoding.ASCII.GetBytes("TTCF")),
                Is.EqualTo(ContainerSignatureKind.Unknown));
        });
    }

    [Test]
    public void UnknownAndUnsupportedSignaturesDoNotMapToSupportedContainer()
    {
        ContainerSignatureKind[] kinds = new ContainerSignatureKind[]
        {
            ContainerSignatureKind.Unknown,
            ContainerSignatureKind.UnsupportedType1Pfb,
            ContainerSignatureKind.UnsupportedType1Pfa,
            ContainerSignatureKind.UnsupportedEmbeddedOpenType,
            ContainerSignatureKind.UnsupportedBitmapPcf,
            ContainerSignatureKind.UnsupportedBitmapBdf
        };

        Assert.Multiple(() =>
        {
            foreach (ContainerSignatureKind kind in kinds)
            {
                Assert.That(ContainerDetector.IsSupported(kind), Is.False);
                Assert.That(ContainerDetector.GetContainerKind(kind), Is.EqualTo(FontContainerKind.Unknown));
            }
        });
    }

    [Test]
    public void ArbitraryBinaryDataRemainsUnknown()
    {
        Assert.That(
            ContainerDetector.Detect(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 1, 2, 3, 4 }),
            Is.EqualTo(ContainerSignatureKind.Unknown));
    }

    private static byte[] CreateEotHeader(
        uint version,
        uint eotSize,
        uint fontDataSize,
        ushort magic)
    {
        byte[] data = new byte[36];
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0, 4), eotSize);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4, 4), fontDataSize);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8, 4), version);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(34, 2), magic);
        return data;
    }

    private static void AssertUnsupported(byte[] data, ContainerSignatureKind expected)
    {
        ContainerSignatureKind actual = ContainerDetector.Detect(data);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(ContainerDetector.IsSupported(actual), Is.False);
            Assert.That(ContainerDetector.GetContainerKind(actual), Is.EqualTo(FontContainerKind.Unknown));
        });
    }
}
