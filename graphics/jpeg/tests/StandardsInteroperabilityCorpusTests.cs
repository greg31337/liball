using System.Collections.Generic;

namespace jpeg.Tests;

internal class StandardsInteroperabilityCorpusTests
{
    [TestCase(0xC0, 8, global::jpeg.JpegProcess.BaselineDct, 128)]
    [TestCase(0xC1, 12, global::jpeg.JpegProcess.ExtendedSequentialDct, 2048)]
    [TestCase(0xC2, 8, global::jpeg.JpegProcess.ProgressiveDct, 128)]
    [TestCase(0xC3, 8, global::jpeg.JpegProcess.LosslessSequential, 128)]
    public void StandardsDerivedCorpus_DecodesKnownNeutralSamples(
        int frameMarker,
        int precision,
        global::jpeg.JpegProcess process,
        int expectedSample)
    {
        byte[] data = IndependentJpegCorpus.CreateNeutralGrayscale(frameMarker, precision);
        global::jpeg.JpegInfo info = new global::jpeg.JpegIdentifierService().Identify(data);
        global::jpeg.JpegImage image = new global::jpeg.JpegReader().Read(data);

        Assert.Multiple(
            delegate
            {
                Assert.That(info.Process, Is.EqualTo(process));
                Assert.That(info.Precision, Is.EqualTo(precision));
                Assert.That(info.Width, Is.EqualTo(8));
                Assert.That(info.Height, Is.EqualTo(8));
                Assert.That(info.ScanCount, Is.EqualTo(frameMarker == 0xC2 ? 2 : 1));
                Assert.That(image.Precision, Is.EqualTo(precision));
                Assert.That(image.Width, Is.EqualTo(8));
                Assert.That(image.Height, Is.EqualTo(8));
            });

        if (precision == 8)
        {
            Assert.That(image.Pixels, Has.Length.EqualTo(64));
            Assert.That(image.Pixels, Is.All.EqualTo(expectedSample));
        }
        else
        {
            Assert.That(image.PlanarBuffer.Components, Has.Count.EqualTo(1));
            Assert.That(image.PlanarBuffer.Components[0].IntSamples, Is.All.EqualTo(expectedSample));
        }
    }

    [TestCase(global::jpeg.JpegProcess.BaselineDct, 8, global::jpeg.JpegEntropyCoding.Huffman, 0xC0, 1)]
    [TestCase(global::jpeg.JpegProcess.ProgressiveDct, 8, global::jpeg.JpegEntropyCoding.Huffman, 0xC2, 2)]
    [TestCase(global::jpeg.JpegProcess.ExtendedSequentialDct, 12, global::jpeg.JpegEntropyCoding.Huffman, 0xC1, 1)]
    [TestCase(global::jpeg.JpegProcess.LosslessSequential, 12, global::jpeg.JpegEntropyCoding.Huffman, 0xC3, 1)]
    [TestCase(global::jpeg.JpegProcess.LosslessSequentialArithmetic, 8, global::jpeg.JpegEntropyCoding.Arithmetic, 0xCB, 1)]
    public void EncoderOutput_PassesIndependentMarkerStructureValidation(
        global::jpeg.JpegProcess process,
        int precision,
        global::jpeg.JpegEntropyCoding entropyCoding,
        int expectedFrameMarker,
        int expectedScanCount)
    {
        global::jpeg.JpegImage source = CreateSource(precision);
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Process = process;
        options.SamplePrecision = precision;
        options.EntropyCoding = entropyCoding;

        byte[] data = new global::jpeg.JpegWriter().WriteWithOptions(source, options);
        IndependentJpegStructure structure = IndependentJpegCorpus.ValidateStructure(data);

        Assert.Multiple(
            delegate
            {
                Assert.That(structure.FrameMarker, Is.EqualTo(expectedFrameMarker));
                Assert.That(structure.Precision, Is.EqualTo(precision));
                Assert.That(structure.Width, Is.EqualTo(8));
                Assert.That(structure.Height, Is.EqualTo(8));
                Assert.That(structure.ScanCount, Is.EqualTo(expectedScanCount));
                Assert.That(structure.SawEndOfImage, Is.True);
                Assert.That(structure.EntropyByteCount, Is.GreaterThan(0));
                Assert.That(structure.DefineHuffmanTableCount, Is.EqualTo(entropyCoding == global::jpeg.JpegEntropyCoding.Huffman ? 1 : 0));
                Assert.That(structure.DefineArithmeticCodingCount, Is.EqualTo(0));
            });
    }

    [Test]
    public void StandardsDerivedCorpus_RejectsTruncationAndIllegalBaselinePrecision()
    {
        byte[] truncated = IndependentJpegCorpus.CreateNeutralGrayscale(0xC0, 8);
        System.Array.Resize(ref truncated, truncated.Length - 2);
        byte[] badPrecision = IndependentJpegCorpus.CreateNeutralGrayscale(0xC0, 12);

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                new global::jpeg.JpegReader().Read(truncated);
            });
        Assert.Throws<global::jpeg.JpegUnsupportedException>(
            delegate
            {
                new global::jpeg.JpegReader().Read(badPrecision);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                IndependentJpegCorpus.ValidateStructure(new byte[] { 0xFF, 0xD8, 0xFF, 0xDB, 0x00, 0x01, 0xFF, 0xD9 });
            });
    }

    public static global::jpeg.JpegImage CreateSource(int precision)
    {
        if (precision == 8)
        {
            return JpegWriteApiTests.CreateGrayscaleImage(8, 8, 128);
        }

        int[] samples = new int[64];
        int index = 0;
        while (index < samples.Length)
        {
            samples[index] = 2048;
            index = index + 1;
        }

        return new global::jpeg.JpegImageFactory().CreatePackedImageFromIntSamples(
            samples,
            8,
            8,
            1,
            8,
            precision,
            global::jpeg.JpegColorSpace.Grayscale);
    }
}

internal class IndependentJpegStructure
{
    public int FrameMarker;
    public int Precision;
    public int Width;
    public int Height;
    public int ScanCount;
    public int EntropyByteCount;
    public int DefineHuffmanTableCount;
    public int DefineArithmeticCodingCount;
    public bool SawEndOfImage;
}

internal static class IndependentJpegCorpus
{
    public static byte[] CreateNeutralGrayscale(int frameMarker, int precision)
    {
        List<byte> data = new List<byte>();
        AddMarker(data, 0xD8);
        if (frameMarker != 0xC3)
        {
            AddSegment(data, 0xDB, CreateUnitQuantizationTable());
        }

        AddSegment(data, frameMarker, CreateFramePayload(precision));
        AddSegment(data, 0xC4, CreateMinimalHuffmanTables(frameMarker != 0xC3));

        if (frameMarker == 0xC2)
        {
            AddSegment(data, 0xDA, CreateScanPayload(0, 0));
            data.Add(0x7F);
            AddSegment(data, 0xDA, CreateScanPayload(1, 63));
            data.Add(0x7F);
        }
        else if (frameMarker == 0xC3)
        {
            AddSegment(data, 0xDA, CreateScanPayload(1, 0));
            int sampleByte = 0;
            while (sampleByte < 8)
            {
                data.Add(0x00);
                sampleByte = sampleByte + 1;
            }
        }
        else
        {
            AddSegment(data, 0xDA, CreateScanPayload(0, 63));
            data.Add(0x3F);
        }

        AddMarker(data, 0xD9);
        return data.ToArray();
    }

    public static IndependentJpegStructure ValidateStructure(byte[] data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException(nameof(data));
        }
        if (data.Length < 4 || data[0] != 0xFF || data[1] != 0xD8)
        {
            throw global::jpeg.JpegInvalidDataException.Create("Independent validation requires an SOI marker.");
        }

        IndependentJpegStructure structure = new IndependentJpegStructure();
        int offset = 2;
        bool inEntropyData = false;
        while (offset < data.Length)
        {
            if (inEntropyData)
            {
                if (data[offset] != 0xFF)
                {
                    structure.EntropyByteCount = structure.EntropyByteCount + 1;
                    offset = offset + 1;
                    continue;
                }

                if (offset + 1 >= data.Length)
                {
                    throw global::jpeg.JpegInvalidDataException.Create("Truncated entropy marker.");
                }

                int entropyCode = data[offset + 1];
                if (entropyCode == 0x00)
                {
                    structure.EntropyByteCount = structure.EntropyByteCount + 1;
                    offset = offset + 2;
                    continue;
                }
                if (entropyCode >= 0xD0 && entropyCode <= 0xD7)
                {
                    offset = offset + 2;
                    continue;
                }

                inEntropyData = false;
            }

            if (offset + 1 >= data.Length || data[offset] != 0xFF)
            {
                throw global::jpeg.JpegInvalidDataException.Create("Expected a JPEG marker.");
            }

            int marker = data[offset + 1];
            offset = offset + 2;
            if (marker == 0xD9)
            {
                structure.SawEndOfImage = true;
                if (offset != data.Length)
                {
                    throw global::jpeg.JpegInvalidDataException.Create("Data follows EOI.");
                }
                break;
            }
            if (marker == 0xD8 || marker == 0x01 || marker >= 0xD0 && marker <= 0xD7)
            {
                continue;
            }
            if (offset + 1 >= data.Length)
            {
                throw global::jpeg.JpegInvalidDataException.Create("Truncated marker length.");
            }

            int length = (data[offset] << 8) | data[offset + 1];
            if (length < 2 || offset + length > data.Length)
            {
                throw global::jpeg.JpegInvalidDataException.Create("Invalid marker length.");
            }

            int payload = offset + 2;
            if (IsFrameMarker(marker))
            {
                if (length < 8)
                {
                    throw global::jpeg.JpegInvalidDataException.Create("Invalid frame header.");
                }
                structure.FrameMarker = marker;
                structure.Precision = data[payload];
                structure.Height = (data[payload + 1] << 8) | data[payload + 2];
                structure.Width = (data[payload + 3] << 8) | data[payload + 4];
            }
            else if (marker == 0xC4)
            {
                ValidateHuffmanPayload(data, payload, length - 2);
                structure.DefineHuffmanTableCount = structure.DefineHuffmanTableCount + 1;
            }
            else if (marker == 0xCC)
            {
                structure.DefineArithmeticCodingCount = structure.DefineArithmeticCodingCount + 1;
            }
            else if (marker == 0xDA)
            {
                if (length < 6)
                {
                    throw global::jpeg.JpegInvalidDataException.Create("Invalid scan header.");
                }
                structure.ScanCount = structure.ScanCount + 1;
                inEntropyData = true;
            }

            offset = offset + length;
        }

        if (!structure.SawEndOfImage)
        {
            throw global::jpeg.JpegInvalidDataException.Create("Missing EOI marker.");
        }
        return structure;
    }

    public static void ValidateHuffmanPayload(byte[] data, int offset, int length)
    {
        int end = offset + length;
        while (offset < end)
        {
            if (offset + 17 > end)
            {
                throw global::jpeg.JpegInvalidDataException.Create("Truncated Huffman table.");
            }
            offset = offset + 1;
            int symbolCount = 0;
            int countIndex = 0;
            while (countIndex < 16)
            {
                symbolCount = symbolCount + data[offset + countIndex];
                countIndex = countIndex + 1;
            }
            offset = offset + 16 + symbolCount;
            if (offset > end)
            {
                throw global::jpeg.JpegInvalidDataException.Create("Truncated Huffman symbols.");
            }
        }
    }

    public static bool IsFrameMarker(int marker)
    {
        return marker >= 0xC0 && marker <= 0xCF && marker != 0xC4 && marker != 0xC8 && marker != 0xCC;
    }

    public static byte[] CreateUnitQuantizationTable()
    {
        byte[] payload = new byte[65];
        int index = 1;
        while (index < payload.Length)
        {
            payload[index] = 1;
            index = index + 1;
        }
        return payload;
    }

    public static byte[] CreateFramePayload(int precision)
    {
        return new byte[]
        {
            (byte)precision,
            0x00, 0x08,
            0x00, 0x08,
            0x01,
            0x01, 0x11, 0x00
        };
    }

    public static byte[] CreateMinimalHuffmanTables(bool includeAcTable)
    {
        List<byte> payload = new List<byte>();
        AddSingleSymbolHuffmanTable(payload, 0x00, 0x00);
        if (includeAcTable)
        {
            AddSingleSymbolHuffmanTable(payload, 0x10, 0x00);
        }
        return payload.ToArray();
    }

    public static void AddSingleSymbolHuffmanTable(List<byte> payload, int tableInformation, int symbol)
    {
        payload.Add((byte)tableInformation);
        payload.Add(0x01);
        int index = 1;
        while (index < 16)
        {
            payload.Add(0x00);
            index = index + 1;
        }
        payload.Add((byte)symbol);
    }

    public static byte[] CreateScanPayload(int spectralStart, int spectralEnd)
    {
        return new byte[]
        {
            0x01,
            0x01, 0x00,
            (byte)spectralStart,
            (byte)spectralEnd,
            0x00
        };
    }

    public static void AddSegment(List<byte> data, int marker, byte[] payload)
    {
        AddMarker(data, marker);
        int length = payload.Length + 2;
        data.Add((byte)(length >> 8));
        data.Add((byte)length);
        data.AddRange(payload);
    }

    public static void AddMarker(List<byte> data, int marker)
    {
        data.Add(0xFF);
        data.Add((byte)marker);
    }
}
