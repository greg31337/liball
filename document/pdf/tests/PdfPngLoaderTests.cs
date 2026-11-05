using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfPngLoaderTests
{
    [TestCase(0, 1)]
    [TestCase(0, 2)]
    [TestCase(0, 4)]
    [TestCase(0, 8)]
    [TestCase(0, 16)]
    [TestCase(2, 8)]
    [TestCase(2, 16)]
    [TestCase(3, 1)]
    [TestCase(3, 2)]
    [TestCase(3, 4)]
    [TestCase(3, 8)]
    [TestCase(4, 8)]
    [TestCase(4, 16)]
    [TestCase(6, 8)]
    [TestCase(6, 16)]
    public void Load_AcceptsEveryBaselineColorTypeAndBitDepth(int colorType, int bitDepth)
    {
        int[] samples = TestPng.CreateSamples(5, 3, colorType, bitDepth);
        byte[] palette = colorType == 3 ? TestPng.CreatePalette(bitDepth) : new byte[0];
        byte[] data = TestPng.Create(5, 3, colorType, bitDepth, samples, false, 0, palette, new byte[0], -1, -1);

        PdfPngData png = new PdfPngLoader().Load(data);

        Assert.Multiple(() =>
        {
            Assert.That(png.Width, Is.EqualTo(5));
            Assert.That(png.Height, Is.EqualTo(3));
            Assert.That(png.BitDepth, Is.EqualTo(bitDepth));
            Assert.That((int)png.ColorType, Is.EqualTo(colorType));
            Assert.That(png.ColorSpace, Is.EqualTo(colorType == 0 || colorType == 4 ? PdfPngColorSpace.Grayscale : PdfPngColorSpace.Rgb));
            Assert.That(png.HasAlpha, Is.EqualTo(colorType == 4 || colorType == 6));
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void Load_ReconstructsEveryPngFilter(int filter)
    {
        int[] samples = TestPng.CreateSamples(4, 3, 2, 8);
        byte[] data = TestPng.Create(4, 3, 2, 8, samples, false, filter, new byte[0], new byte[0], -1, -1);

        PdfPngData png = new PdfPngLoader().Load(data);
        byte[] decoded = TestPng.Decompress(png.CompressedPixelData);

        Assert.That(decoded, Is.EqualTo(TestPng.SamplesToBytes(samples, 8)));
    }

    [Test]
    public void Load_ReconstructsAdam7TruecolorAndAlphaSamples()
    {
        int[] samples = TestPng.CreateSamples(9, 8, 6, 8);
        byte[] data = TestPng.Create(9, 8, 6, 8, samples, true, 4, new byte[0], new byte[0], -1, -1);

        PdfPngData png = new PdfPngLoader().Load(data);
        byte[] colors = TestPng.Decompress(png.CompressedPixelData);
        byte[] alpha = TestPng.Decompress(png.CompressedAlphaData);

        Assert.That(png.IsInterlaced, Is.True);
        Assert.That(colors, Is.EqualTo(TestPng.SelectChannels(samples, 4, new int[] { 0, 1, 2 }, 8)));
        Assert.That(alpha, Is.EqualTo(TestPng.SelectChannels(samples, 4, new int[] { 3 }, 8)));
    }

    [Test]
    public void Load_ReconstructsAdam7PackedGrayscaleSamples()
    {
        int[] samples = TestPng.CreateSamples(9, 8, 0, 2);
        byte[] interlacedData = TestPng.Create(9, 8, 0, 2, samples, true, 3, new byte[0], new byte[0], -1, -1);
        byte[] sequentialData = TestPng.Create(9, 8, 0, 2, samples, false, 3, new byte[0], new byte[0], -1, -1);

        PdfPngData interlaced = new PdfPngLoader().Load(interlacedData);
        PdfPngData sequential = new PdfPngLoader().Load(sequentialData);

        Assert.That(interlaced.CompressedPixelData, Is.EqualTo(sequential.CompressedPixelData));
    }

    [Test]
    public void Load_ExpandsPaletteAndTransparencyIntoRgbAndSoftMask()
    {
        int[] samples = new int[] { 0, 1, 2, 1 };
        byte[] palette = new byte[] { 255, 0, 0, 0, 255, 0, 0, 0, 255 };
        byte[] transparency = new byte[] { 255, 128, 0 };
        byte[] data = TestPng.Create(4, 1, 3, 2, samples, false, 1, palette, transparency, -1, -1);

        PdfPngData png = new PdfPngLoader().Load(data);

        Assert.That(TestPng.Decompress(png.CompressedPixelData), Is.EqualTo(new byte[] { 255, 0, 0, 0, 255, 0, 0, 0, 255, 0, 255, 0 }));
        Assert.That(TestPng.Decompress(png.CompressedAlphaData), Is.EqualTo(new byte[] { 255, 128, 0, 128 }));
        Assert.That(png.HasAlpha, Is.True);
        Assert.That(png.OutputBitsPerComponent, Is.EqualTo(8));
    }

    [Test]
    public void Load_ConvertsTransparencyKeysIntoSoftMasks()
    {
        int[] grayscaleSamples = new int[] { 0, 2, 3, 2 };
        byte[] grayscaleTransparency = new byte[] { 0, 2 };
        PdfPngData grayscale = new PdfPngLoader().Load(TestPng.Create(4, 1, 0, 2, grayscaleSamples, false, 0, new byte[0], grayscaleTransparency, -1, -1));

        int[] rgbSamples = new int[] { 1, 2, 3, 9, 8, 7 };
        byte[] rgbTransparency = new byte[] { 0, 1, 0, 2, 0, 3 };
        PdfPngData rgb = new PdfPngLoader().Load(TestPng.Create(2, 1, 2, 8, rgbSamples, false, 0, new byte[0], rgbTransparency, -1, -1));

        Assert.That(TestPng.Decompress(grayscale.CompressedAlphaData), Is.EqualTo(new byte[] { 255, 0, 255, 0 }));
        Assert.That(TestPng.Decompress(rgb.CompressedAlphaData), Is.EqualTo(new byte[] { 0, 255 }));
    }

    [Test]
    public void LoadFromArraySpanStreamAndFile_PreservesDpiAndInputOwnership()
    {
        int[] samples = TestPng.CreateSamples(2, 2, 0, 8);
        byte[] data = TestPng.Create(2, 2, 0, 8, samples, false, 2, new byte[0], new byte[0], 11811, 5906);
        IPdfPngLoader loader = new PdfPngLoader();
        PdfPngData fromArray = loader.Load(data);
        PdfPngData fromSpan = loader.LoadFromSpan(data.AsSpan());

        byte[] prefixed = new byte[data.Length + 3];
        Array.Copy(data, 0, prefixed, 3, data.Length);
        using MemoryStream stream = new MemoryStream(prefixed);
        stream.Position = 3;
        PdfPngData fromStream = loader.LoadFromStream(stream);

        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "png-loader-test-" + Guid.NewGuid().ToString("N") + ".png");
        File.WriteAllBytes(path, data);
        try
        {
            PdfPngData fromFile = loader.LoadFromFile(path);
            data[0] = 0;

            Assert.That(fromArray.EncodedData[0], Is.EqualTo(137));
            Assert.That(fromArray.HasDpi, Is.True);
            Assert.That(fromArray.HorizontalDpi, Is.EqualTo(299.9994).Within(0.001));
            Assert.That(fromArray.VerticalDpi, Is.EqualTo(150.0124).Within(0.001));
            Assert.That(fromSpan.Width, Is.EqualTo(2));
            Assert.That(fromStream.Height, Is.EqualTo(2));
            Assert.That(fromFile.BitDepth, Is.EqualTo(8));
            Assert.That(stream.CanRead, Is.True);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Load_ProducesDeterministicFlateDataIndependentOfSourceFilters()
    {
        int[] samples = TestPng.CreateSamples(6, 4, 2, 8);
        byte[] unfiltered = TestPng.Create(6, 4, 2, 8, samples, false, 0, new byte[0], new byte[0], -1, -1);
        byte[] paeth = TestPng.Create(6, 4, 2, 8, samples, false, 4, new byte[0], new byte[0], -1, -1);

        PdfPngData first = new PdfPngLoader().Load(unfiltered);
        PdfPngData second = new PdfPngLoader().Load(paeth);

        Assert.That(second.CompressedPixelData, Is.EqualTo(first.CompressedPixelData));
    }

    [Test]
    public void Document_PngInputsReuseCacheAndWriterEmitsFlateSoftMaskObjects()
    {
        int[] samples = new int[] { 255, 0, 0, 255, 0, 0, 255, 64 };
        byte[] data = TestPng.Create(2, 1, 6, 8, samples, false, 0, new byte[0], new byte[0], -1, -1);
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "png-document-test-" + Guid.NewGuid().ToString("N") + ".png");
        File.WriteAllBytes(path, data);
        try
        {
            IPdfDocument documentApi = new PdfDocument();
            PdfImage first = documentApi.AddPng(data);
            PdfImage second = documentApi.AddPngFromSpan(data.AsSpan());
            using MemoryStream stream = new MemoryStream(data);
            PdfImage third = documentApi.AddPngFromStream(stream);
            PdfImage fourth = documentApi.AddPngFromFile(path);
            PdfDocument document = (PdfDocument)documentApi;
            document.AddPage(100, 100);
            data[0] = 0;

            byte[] firstPdf = document.ToBytes();
            byte[] secondPdf = document.ToBytes();
            string pdfText = Encoding.ASCII.GetString(firstPdf);

            Assert.That(second, Is.SameAs(first));
            Assert.That(third, Is.SameAs(first));
            Assert.That(fourth, Is.SameAs(first));
            Assert.That(document.Images.Count, Is.EqualTo(1));
            Assert.That(first.EncodedData[0], Is.EqualTo(137));
            Assert.That(pdfText, Does.Contain("/ColorSpace /DeviceRGB /BitsPerComponent 8"));
            Assert.That(pdfText, Does.Contain("/SMask "));
            Assert.That(pdfText, Does.Contain("/ColorSpace /DeviceGray /BitsPerComponent 8"));
            Assert.That(pdfText, Does.Contain("/Filter /FlateDecode"));
            Assert.That(TestPng.IndexOf(firstPdf, first.CompressedData), Is.GreaterThanOrEqualTo(0));
            Assert.That(TestPng.IndexOf(firstPdf, first.SoftMaskData), Is.GreaterThanOrEqualTo(0));
            Assert.That(secondPdf, Is.EqualTo(firstPdf));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Load_RejectsInvalidSignatureCrcFormatPaletteAnimationAndTrailingData()
    {
        int[] samples = TestPng.CreateSamples(1, 1, 2, 8);
        byte[] valid = TestPng.Create(1, 1, 2, 8, samples, false, 0, new byte[0], new byte[0], -1, -1);
        byte[] invalidSignature = (byte[])valid.Clone();
        invalidSignature[0] = 0;
        byte[] invalidCrc = (byte[])valid.Clone();
        invalidCrc[29] = (byte)(invalidCrc[29] ^ 1);
        byte[] invalidFormat = TestPng.Create(1, 1, 2, 1, new int[] { 0, 0, 0 }, false, 0, new byte[0], new byte[0], -1, -1);
        byte[] missingPalette = TestPng.Create(1, 1, 3, 1, new int[] { 0 }, false, 0, new byte[0], new byte[0], -1, -1);
        byte[] animated = TestPng.InsertBeforeImageData(valid, "acTL", new byte[] { 0, 0, 0, 1, 0, 0, 0, 0 });
        byte[] trailing = new byte[valid.Length + 1];
        Array.Copy(valid, trailing, valid.Length);
        byte[] truncated = new byte[valid.Length - 1];
        Array.Copy(valid, truncated, truncated.Length);

        PdfPngLoader loader = new PdfPngLoader();
        Assert.Throws<InvalidDataException>(() => loader.Load(invalidSignature));
        Assert.Throws<InvalidDataException>(() => loader.Load(invalidCrc));
        Assert.Throws<InvalidDataException>(() => loader.Load(invalidFormat));
        Assert.Throws<InvalidDataException>(() => loader.Load(missingPalette));
        Assert.Throws<InvalidDataException>(() => loader.Load(animated));
        Assert.Throws<InvalidDataException>(() => loader.Load(trailing));
        Assert.Throws<InvalidDataException>(() => loader.Load(truncated));
    }

    [Test]
    public void Loader_ValidatesNullAndUnreadableInputs()
    {
        PdfPngLoader loader = new PdfPngLoader();
        MemoryStream unreadable = new MemoryStream();
        unreadable.Dispose();

        Assert.Throws<ArgumentNullException>(() => loader.Load(null!));
        Assert.Throws<ArgumentNullException>(() => loader.LoadFromStream(null!));
        Assert.Throws<ArgumentException>(() => loader.LoadFromStream(unreadable));
        Assert.Throws<ArgumentNullException>(() => loader.LoadFromFile(null!));
    }
}

internal static class TestPng
{
    internal static byte[] Create(int width, int height, int colorType, int bitDepth, int[] samples, bool interlaced, int filter, byte[] palette, byte[] transparency, int pixelsPerMeterX, int pixelsPerMeterY)
    {
        List<byte> png = new List<byte>();
        png.AddRange(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        List<byte> header = new List<byte>();
        AddUInt32(header, width);
        AddUInt32(header, height);
        header.Add((byte)bitDepth);
        header.Add((byte)colorType);
        header.Add(0);
        header.Add(0);
        header.Add((byte)(interlaced ? 1 : 0));
        AddChunk(png, "IHDR", header.ToArray());

        if (palette.Length > 0)
        {
            AddChunk(png, "PLTE", palette);
        }

        if (transparency.Length > 0)
        {
            AddChunk(png, "tRNS", transparency);
        }

        if (pixelsPerMeterX >= 0)
        {
            List<byte> physical = new List<byte>();
            AddUInt32(physical, pixelsPerMeterX);
            AddUInt32(physical, pixelsPerMeterY);
            physical.Add(1);
            AddChunk(png, "pHYs", physical.ToArray());
        }

        int channels = GetChannelCount(colorType);
        byte[] scanData = BuildScanData(width, height, channels, bitDepth, samples, interlaced, filter);
        byte[] compressed = Compress(scanData);
        int split = compressed.Length / 2;
        if (split == 0)
        {
            split = compressed.Length;
        }

        byte[] first = new byte[split];
        Array.Copy(compressed, first, split);
        AddChunk(png, "IDAT", first);
        if (split < compressed.Length)
        {
            byte[] second = new byte[compressed.Length - split];
            Array.Copy(compressed, split, second, 0, second.Length);
            AddChunk(png, "IDAT", second);
        }

        AddChunk(png, "IEND", new byte[0]);
        return png.ToArray();
    }

    internal static int[] CreateSamples(int width, int height, int colorType, int bitDepth)
    {
        int channels = GetChannelCount(colorType);
        int maximum = bitDepth == 16 ? 65535 : (1 << bitDepth) - 1;
        int paletteEntries = colorType == 3 ? Math.Min(maximum + 1, 4) : maximum + 1;
        int[] samples = new int[width * height * channels];
        for (int pixel = 0; pixel < width * height; pixel++)
        {
            for (int channel = 0; channel < channels; channel++)
            {
                int value = (pixel * 37 + channel * 71 + 1) % paletteEntries;
                if ((colorType == 4 && channel == 1) || (colorType == 6 && channel == 3))
                {
                    value = pixel % 2 == 0 ? maximum : maximum / 3;
                }

                samples[pixel * channels + channel] = value;
            }
        }

        return samples;
    }

    internal static byte[] CreatePalette(int bitDepth)
    {
        int entries = Math.Min(1 << bitDepth, 4);
        byte[] palette = new byte[entries * 3];
        for (int index = 0; index < entries; index++)
        {
            palette[index * 3] = (byte)(index * 53);
            palette[index * 3 + 1] = (byte)(255 - index * 31);
            palette[index * 3 + 2] = (byte)(index * 17);
        }

        return palette;
    }

    internal static int GetChannelCount(int colorType)
    {
        if (colorType == 0 || colorType == 3)
        {
            return 1;
        }

        if (colorType == 2)
        {
            return 3;
        }

        if (colorType == 4)
        {
            return 2;
        }

        return 4;
    }

    internal static byte[] BuildScanData(int width, int height, int channels, int bitDepth, int[] samples, bool interlaced, int filter)
    {
        List<byte> output = new List<byte>();
        if (interlaced)
        {
            int[] startX = new int[] { 0, 4, 0, 2, 0, 1, 0 };
            int[] startY = new int[] { 0, 0, 4, 0, 2, 0, 1 };
            int[] stepX = new int[] { 8, 8, 4, 4, 2, 2, 1 };
            int[] stepY = new int[] { 8, 8, 8, 4, 4, 2, 2 };
            for (int pass = 0; pass < 7; pass++)
            {
                AddPass(output, width, height, channels, bitDepth, samples, startX[pass], startY[pass], stepX[pass], stepY[pass], filter);
            }
        }
        else
        {
            AddPass(output, width, height, channels, bitDepth, samples, 0, 0, 1, 1, filter);
        }

        return output.ToArray();
    }

    internal static void AddPass(List<byte> output, int width, int height, int channels, int bitDepth, int[] samples, int startX, int startY, int stepX, int stepY, int filter)
    {
        int passWidth = GetPassSize(width, startX, stepX);
        int passHeight = GetPassSize(height, startY, stepY);
        if (passWidth == 0 || passHeight == 0)
        {
            return;
        }

        int rowBytes = (passWidth * channels * bitDepth + 7) / 8;
        int bytesPerPixel = Math.Max(1, (channels * bitDepth + 7) / 8);
        byte[] previous = new byte[rowBytes];
        for (int passY = 0; passY < passHeight; passY++)
        {
            byte[] row = new byte[rowBytes];
            int imageY = startY + passY * stepY;
            for (int passX = 0; passX < passWidth; passX++)
            {
                int imageX = startX + passX * stepX;
                for (int channel = 0; channel < channels; channel++)
                {
                    int source = (imageY * width + imageX) * channels + channel;
                    WriteSample(row, passX * channels + channel, bitDepth, samples[source]);
                }
            }

            output.Add((byte)filter);
            for (int index = 0; index < row.Length; index++)
            {
                int left = index >= bytesPerPixel ? row[index - bytesPerPixel] : 0;
                int above = previous[index];
                int upperLeft = index >= bytesPerPixel ? previous[index - bytesPerPixel] : 0;
                int predictor = 0;
                if (filter == 1)
                {
                    predictor = left;
                }
                else if (filter == 2)
                {
                    predictor = above;
                }
                else if (filter == 3)
                {
                    predictor = (left + above) / 2;
                }
                else if (filter == 4)
                {
                    predictor = Paeth(left, above, upperLeft);
                }

                output.Add((byte)((row[index] - predictor) & 255));
            }

            previous = row;
        }
    }

    internal static int GetPassSize(int total, int start, int step)
    {
        return total <= start ? 0 : (total - start + step - 1) / step;
    }

    internal static void WriteSample(byte[] row, int sampleIndex, int bitDepth, int value)
    {
        if (bitDepth == 8)
        {
            row[sampleIndex] = (byte)value;
        }
        else if (bitDepth == 16)
        {
            row[sampleIndex * 2] = (byte)(value >> 8);
            row[sampleIndex * 2 + 1] = (byte)value;
        }
        else
        {
            int bitOffset = sampleIndex * bitDepth;
            int shift = 8 - bitDepth - bitOffset % 8;
            row[bitOffset / 8] = (byte)(row[bitOffset / 8] | value << shift);
        }
    }

    internal static int Paeth(int left, int above, int upperLeft)
    {
        int estimate = left + above - upperLeft;
        int leftDistance = Math.Abs(estimate - left);
        int aboveDistance = Math.Abs(estimate - above);
        int upperLeftDistance = Math.Abs(estimate - upperLeft);
        if (leftDistance <= aboveDistance && leftDistance <= upperLeftDistance)
        {
            return left;
        }

        return aboveDistance <= upperLeftDistance ? above : upperLeft;
    }

    internal static byte[] SelectChannels(int[] samples, int sourceChannels, int[] selectedChannels, int bitDepth)
    {
        int pixels = samples.Length / sourceChannels;
        List<int> selected = new List<int>();
        for (int pixel = 0; pixel < pixels; pixel++)
        {
            for (int index = 0; index < selectedChannels.Length; index++)
            {
                selected.Add(samples[pixel * sourceChannels + selectedChannels[index]]);
            }
        }

        return SamplesToBytes(selected.ToArray(), bitDepth);
    }

    internal static byte[] SamplesToBytes(int[] samples, int bitDepth)
    {
        if (bitDepth == 8)
        {
            byte[] bytes = new byte[samples.Length];
            for (int index = 0; index < samples.Length; index++)
            {
                bytes[index] = (byte)samples[index];
            }

            return bytes;
        }

        byte[] wideBytes = new byte[samples.Length * 2];
        for (int index = 0; index < samples.Length; index++)
        {
            wideBytes[index * 2] = (byte)(samples[index] >> 8);
            wideBytes[index * 2 + 1] = (byte)samples[index];
        }

        return wideBytes;
    }

    internal static byte[] Compress(byte[] data)
    {
        using MemoryStream output = new MemoryStream();
        using (ZLibStream compressed = new ZLibStream(output, CompressionLevel.Optimal, true))
        {
            compressed.Write(data);
        }

        return output.ToArray();
    }

    internal static byte[] Decompress(byte[] data)
    {
        using MemoryStream input = new MemoryStream(data);
        using ZLibStream decompressed = new ZLibStream(input, CompressionMode.Decompress);
        using MemoryStream output = new MemoryStream();
        decompressed.CopyTo(output);
        return output.ToArray();
    }

    internal static void AddChunk(List<byte> png, string type, byte[] data)
    {
        AddUInt32(png, data.Length);
        byte[] typeBytes = Encoding.ASCII.GetBytes(type);
        int crcStart = png.Count;
        png.AddRange(typeBytes);
        png.AddRange(data);
        uint crc = CalculateCrc(png, crcStart, typeBytes.Length + data.Length);
        AddUInt32(png, crc);
    }

    internal static void AddUInt32(List<byte> output, long value)
    {
        output.Add((byte)(value >> 24));
        output.Add((byte)(value >> 16));
        output.Add((byte)(value >> 8));
        output.Add((byte)value);
    }

    internal static uint CalculateCrc(List<byte> data, int offset, int length)
    {
        uint crc = 4294967295U;
        for (int index = 0; index < length; index++)
        {
            crc = crc ^ data[offset + index];
            for (int bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1U) != 0 ? 3988292384U ^ crc >> 1 : crc >> 1;
            }
        }

        return crc ^ 4294967295U;
    }

    internal static byte[] InsertBeforeImageData(byte[] png, string type, byte[] data)
    {
        int insertAt = FindChunk(png, "IDAT");
        List<byte> chunk = new List<byte>();
        AddChunk(chunk, type, data);
        byte[] result = new byte[png.Length + chunk.Count];
        Array.Copy(png, 0, result, 0, insertAt);
        chunk.CopyTo(result, insertAt);
        Array.Copy(png, insertAt, result, insertAt + chunk.Count, png.Length - insertAt);
        return result;
    }

    internal static int FindChunk(byte[] png, string type)
    {
        byte[] expected = Encoding.ASCII.GetBytes(type);
        int offset = 8;
        while (offset < png.Length)
        {
            bool matches = true;
            for (int index = 0; index < 4; index++)
            {
                if (png[offset + 4 + index] != expected[index])
                {
                    matches = false;
                }
            }

            if (matches)
            {
                return offset;
            }

            int length = png[offset] << 24 | png[offset + 1] << 16 | png[offset + 2] << 8 | png[offset + 3];
            offset = offset + 12 + length;
        }

        return -1;
    }

    internal static int IndexOf(byte[] data, byte[] expected)
    {
        for (int index = 0; index <= data.Length - expected.Length; index++)
        {
            bool matches = true;
            for (int expectedIndex = 0; expectedIndex < expected.Length; expectedIndex++)
            {
                if (data[index + expectedIndex] != expected[expectedIndex])
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                return index;
            }
        }

        return -1;
    }
}
