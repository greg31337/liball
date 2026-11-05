using System.Collections.Generic;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PackedSampleRoundTripTests
{
    [TestCaseSource(nameof(CreatePackedSampleCases))]
    public void PackAndUnpack_RoundTripsAcrossByteBoundaries(PngBitDepth bitDepth, byte[] samples, byte[] expectedPacked)
    {
        byte[] packed = PngPackedSampleWriter.Pack(samples, 0, samples.Length, bitDepth);
        byte[] unpacked = PngPackedSampleUnpacker.Unpack(packed, 0, samples.Length, bitDepth);

        Assert.That(packed, Is.EqualTo(expectedPacked));
        Assert.That(unpacked, Is.EqualTo(samples));
    }

    [TestCaseSource(nameof(CreatePaddingCases))]
    public void Unpack_IgnoresPaddingBitsInFinalByte(PngBitDepth bitDepth, byte[] packed, int sampleCount, byte[] expectedSamples)
    {
        byte[] samples = PngPackedSampleUnpacker.Unpack(packed, 0, sampleCount, bitDepth);

        Assert.That(samples, Is.EqualTo(expectedSamples));
    }

    [Test]
    public void PackInto_ClearsAllRequiredBytesAndPreservesNeighborBytes()
    {
        byte[] destination = new byte[] { 0xAA, 0xFF, 0xFF, 0xAA };
        byte[] samples = new byte[] { 1, 0, 1, 1, 0, 0, 1, 0, 1, 1 };

        PngPackedSampleWriter.PackInto(destination, 1, samples, 0, samples.Length, PngBitDepth.One);

        Assert.That(destination, Is.EqualTo(new byte[] { 0xAA, 0b10110010, 0b11000000, 0xAA }));
    }

    [TestCaseSource(nameof(CreateLayoutCases))]
    public void PackRowAndUnpackRow_RoundTripsPackedLayouts(PngColorType colorType, PngBitDepth bitDepth, byte[] samples)
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(samples.Length, 1, colorType, bitDepth);

        byte[] packed = PngPackedSampleWriter.PackRow(samples, layout);
        byte[] unpacked = PngPackedSampleUnpacker.UnpackRow(packed, layout);

        Assert.That(unpacked, Is.EqualTo(samples));
    }

    static IEnumerable<TestCaseData> CreatePackedSampleCases()
    {
        yield return new TestCaseData(
            PngBitDepth.One,
            new byte[] { 1, 0, 1, 1, 0, 0, 1, 0, 1, 1 },
            new byte[] { 0b10110010, 0b11000000 }).SetName("1-bit crosses byte boundary");
        yield return new TestCaseData(
            PngBitDepth.Two,
            new byte[] { 0, 1, 2, 3, 3, 2, 1 },
            new byte[] { 0b00011011, 0b11100100 }).SetName("2-bit crosses byte boundary");
        yield return new TestCaseData(
            PngBitDepth.Four,
            new byte[] { 1, 15, 10, 0, 7 },
            new byte[] { 0x1F, 0xA0, 0x70 }).SetName("4-bit crosses byte boundary");
    }

    static IEnumerable<TestCaseData> CreatePaddingCases()
    {
        yield return new TestCaseData(
            PngBitDepth.One,
            new byte[] { 0b10111111 },
            3,
            new byte[] { 1, 0, 1 }).SetName("1-bit ignores 5 padding bits");
        yield return new TestCaseData(
            PngBitDepth.Two,
            new byte[] { 0b00011011, 0b11011111 },
            5,
            new byte[] { 0, 1, 2, 3, 3 }).SetName("2-bit ignores 6 padding bits");
        yield return new TestCaseData(
            PngBitDepth.Four,
            new byte[] { 0x1F, 0xAF },
            3,
            new byte[] { 1, 15, 10 }).SetName("4-bit ignores 4 padding bits");
    }

    static IEnumerable<TestCaseData> CreateLayoutCases()
    {
        yield return new TestCaseData(
            PngColorType.Grayscale,
            PngBitDepth.One,
            new byte[] { 1, 0, 1, 0, 1, 0, 1, 1, 1 }).SetName("grayscale 1-bit layout");
        yield return new TestCaseData(
            PngColorType.Grayscale,
            PngBitDepth.Two,
            new byte[] { 0, 1, 2, 3, 0, 2 }).SetName("grayscale 2-bit layout");
        yield return new TestCaseData(
            PngColorType.IndexedColor,
            PngBitDepth.Four,
            new byte[] { 0, 15, 1, 14, 2 }).SetName("indexed 4-bit layout");
    }
}
