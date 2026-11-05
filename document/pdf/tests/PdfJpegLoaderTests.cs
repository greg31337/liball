using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfJpegLoaderTests
{
    [Test]
    public void Load_ParsesFrameComponentsJfifDpiAndOwnsEncodedData()
    {
        byte[] data = TestJpeg.Create(3, 2, 3, 1, 300, 150, false);

        PdfJpegData jpeg = new PdfJpegLoader().Load(data);
        data[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(jpeg.Width, Is.EqualTo(3));
            Assert.That(jpeg.Height, Is.EqualTo(2));
            Assert.That(jpeg.ComponentCount, Is.EqualTo(3));
            Assert.That(jpeg.BitsPerComponent, Is.EqualTo(8));
            Assert.That(jpeg.ColorSpace, Is.EqualTo(PdfJpegColorSpace.Rgb));
            Assert.That(jpeg.HasDpi, Is.True);
            Assert.That(jpeg.HorizontalDpi, Is.EqualTo(300));
            Assert.That(jpeg.VerticalDpi, Is.EqualTo(150));
            Assert.That(jpeg.HasInvertedCmykSamples, Is.False);
            Assert.That(jpeg.EncodedData[0], Is.EqualTo(255));
        });
    }

    [Test]
    public void LoadFromSpanStreamAndFile_ReadCompleteInputsWithoutOwningStream()
    {
        byte[] data = TestJpeg.Create(7, 5, 1, 0, 1, 1, false);
        IPdfJpegLoader loader = new PdfJpegLoader();
        PdfJpegData fromSpan = loader.LoadFromSpan(data.AsSpan());

        byte[] prefixed = new byte[data.Length + 2];
        Array.Copy(data, 0, prefixed, 2, data.Length);
        using MemoryStream stream = new MemoryStream(prefixed);
        stream.Position = 2;
        PdfJpegData fromStream = loader.LoadFromStream(stream);

        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "jpeg-loader-test-" + Guid.NewGuid().ToString("N") + ".jpg");
        File.WriteAllBytes(path, data);
        try
        {
            PdfJpegData fromFile = loader.LoadFromFile(path);

            Assert.That(fromSpan.Width, Is.EqualTo(7));
            Assert.That(fromStream.Height, Is.EqualTo(5));
            Assert.That(fromFile.ColorSpace, Is.EqualTo(PdfJpegColorSpace.Grayscale));
            Assert.That(stream.CanRead, Is.True);
            Assert.That(stream.Position, Is.EqualTo(prefixed.Length));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Load_ConvertsCentimetreDensityAndHandlesAdobeCmyk()
    {
        PdfJpegData grayscale = new PdfJpegLoader().Load(TestJpeg.Create(4, 6, 1, 2, 100, 50, false));
        PdfJpegData cmyk = new PdfJpegLoader().Load(TestJpeg.Create(8, 9, 4, -1, 0, 0, true));

        Assert.Multiple(() =>
        {
            Assert.That(grayscale.ColorSpace, Is.EqualTo(PdfJpegColorSpace.Grayscale));
            Assert.That(grayscale.HorizontalDpi, Is.EqualTo(254).Within(0.0001));
            Assert.That(grayscale.VerticalDpi, Is.EqualTo(127).Within(0.0001));
            Assert.That(cmyk.ColorSpace, Is.EqualTo(PdfJpegColorSpace.Cmyk));
            Assert.That(cmyk.ComponentCount, Is.EqualTo(4));
            Assert.That(cmyk.HasDpi, Is.False);
            Assert.That(cmyk.HasInvertedCmykSamples, Is.True);
        });
    }

    [Test]
    public void Load_UsesExifResolutionWhenJfifPhysicalDensityIsAbsent()
    {
        byte[] jpegData = TestJpeg.Create(10, 11, 3, -1, 0, 0, false);
        byte[] withExif = TestJpeg.InsertAfterStart(jpegData, 225, TestJpeg.CreateExifResolution(240, 120, 2));

        PdfJpegData jpeg = new PdfJpegLoader().Load(withExif);

        Assert.That(jpeg.HasDpi, Is.True);
        Assert.That(jpeg.HorizontalDpi, Is.EqualTo(240));
        Assert.That(jpeg.VerticalDpi, Is.EqualTo(120));
    }

    [Test]
    public void Load_RejectsMalformedMarkersFramesAndScans()
    {
        PdfJpegLoader loader = new PdfJpegLoader();
        byte[] missingEnd = TestJpeg.Create(1, 1, 3, -1, 0, 0, false);
        Array.Resize(ref missingEnd, missingEnd.Length - 2);
        byte[] badLength = TestJpeg.Create(1, 1, 3, 1, 72, 72, false);
        badLength[4] = 255;
        badLength[5] = 255;
        byte[] unsupportedFrame = TestJpeg.Create(1, 1, 3, -1, 0, 0, false);
        unsupportedFrame[TestJpeg.FindMarker(unsupportedFrame, 192) + 1] = 195;
        byte[] unsupportedPrecision = TestJpeg.Create(1, 1, 3, -1, 0, 0, false);
        unsupportedPrecision[TestJpeg.FindMarker(unsupportedPrecision, 192) + 4] = 12;
        byte[] unsupportedComponents = TestJpeg.Create(1, 1, 2, -1, 0, 0, false);
        byte[] unknownScanComponent = TestJpeg.Create(1, 1, 3, -1, 0, 0, false);
        int scanMarker = TestJpeg.FindMarker(unknownScanComponent, 218);
        unknownScanComponent[scanMarker + 5] = 9;

        Assert.Throws<InvalidDataException>(() => loader.Load(new byte[] { 0, 1, 2, 3 }));
        Assert.Throws<InvalidDataException>(() => loader.Load(missingEnd));
        Assert.Throws<InvalidDataException>(() => loader.Load(badLength));
        Assert.Throws<InvalidDataException>(() => loader.Load(unsupportedFrame));
        Assert.Throws<InvalidDataException>(() => loader.Load(unsupportedPrecision));
        Assert.Throws<InvalidDataException>(() => loader.Load(unsupportedComponents));
        Assert.Throws<InvalidDataException>(() => loader.Load(unknownScanComponent));
    }

    [Test]
    public void Document_JpegInputsReuseCachedEncodedResourceAndCopyCallerData()
    {
        byte[] data = TestJpeg.Create(2, 3, 3, 1, 96, 96, false);
        IPdfDocument document = new PdfDocument();
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "jpeg-document-test-" + Guid.NewGuid().ToString("N") + ".jpg");
        File.WriteAllBytes(path, data);

        try
        {
            PdfImage first = document.AddJpeg(data);
            PdfImage second = document.AddJpegFromSpan(data.AsSpan());
            using MemoryStream stream = new MemoryStream(data);
            PdfImage third = document.AddJpegFromStream(stream);
            PdfImage fourth = document.AddJpegFromFile(path);
            data[0] = 0;

            Assert.That(second, Is.SameAs(first));
            Assert.That(third, Is.SameAs(first));
            Assert.That(fourth, Is.SameAs(first));
            Assert.That(first.EncodedData[0], Is.EqualTo(255));
            Assert.That(((PdfDocument)document).Images.Count, Is.EqualTo(1));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Save_WritesOriginalJpegBytesWithDctColorAndDecodeMetadata()
    {
        byte[] data = TestJpeg.Create(12, 13, 4, -1, 0, 0, true);
        PdfDocument document = new PdfDocument();
        document.AddPage(100, 100);
        document.AddJpeg(data);

        byte[] pdfBytes = document.ToBytes();
        string pdfText = Encoding.ASCII.GetString(pdfBytes);

        Assert.That(pdfText, Does.Contain("/Width 12 /Height 13"));
        Assert.That(pdfText, Does.Contain("/ColorSpace /DeviceCMYK /BitsPerComponent 8"));
        Assert.That(pdfText, Does.Contain("/Decode [1 0 1 0 1 0 1 0]"));
        Assert.That(pdfText, Does.Contain("/Filter /DCTDecode"));
        Assert.That(TestJpeg.IndexOf(pdfBytes, data), Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void Loader_ValidatesNullAndUnreadableInputs()
    {
        PdfJpegLoader loader = new PdfJpegLoader();
        using MemoryStream writable = new MemoryStream();
        writable.Dispose();

        Assert.Throws<ArgumentNullException>(() => loader.Load(null!));
        Assert.Throws<ArgumentNullException>(() => loader.LoadFromStream(null!));
        Assert.Throws<ArgumentException>(() => loader.LoadFromStream(writable));
        Assert.Throws<ArgumentNullException>(() => loader.LoadFromFile(null!));
    }
}

internal static class TestJpeg
{
    internal static byte[] Create(int width, int height, int components, int densityUnits, int horizontalDensity, int verticalDensity, bool adobe)
    {
        List<byte> data = new List<byte>();
        data.Add(255);
        data.Add(216);

        if (densityUnits >= 0)
        {
            List<byte> jfif = new List<byte>();
            jfif.AddRange(Encoding.ASCII.GetBytes("JFIF\0"));
            jfif.Add(1);
            jfif.Add(2);
            jfif.Add((byte)densityUnits);
            AddUInt16(jfif, horizontalDensity);
            AddUInt16(jfif, verticalDensity);
            jfif.Add(0);
            jfif.Add(0);
            AddSegment(data, 224, jfif.ToArray());
        }

        if (adobe)
        {
            List<byte> adobeData = new List<byte>();
            adobeData.AddRange(Encoding.ASCII.GetBytes("Adobe"));
            adobeData.Add(0);
            adobeData.Add(100);
            adobeData.Add(0);
            adobeData.Add(0);
            adobeData.Add(0);
            adobeData.Add(0);
            adobeData.Add((byte)(components == 4 ? 2 : components == 3 ? 1 : 0));
            AddSegment(data, 238, adobeData.ToArray());
        }

        List<byte> frame = new List<byte>();
        frame.Add(8);
        AddUInt16(frame, height);
        AddUInt16(frame, width);
        frame.Add((byte)components);
        for (int component = 1; component <= components; component++)
        {
            frame.Add((byte)component);
            frame.Add(17);
            frame.Add(0);
        }

        AddSegment(data, 192, frame.ToArray());

        List<byte> scan = new List<byte>();
        scan.Add((byte)components);
        for (int component = 1; component <= components; component++)
        {
            scan.Add((byte)component);
            scan.Add(0);
        }

        scan.Add(0);
        scan.Add(63);
        scan.Add(0);
        AddSegment(data, 218, scan.ToArray());
        data.Add(17);
        data.Add(255);
        data.Add(0);
        data.Add(34);
        data.Add(255);
        data.Add(208);
        data.Add(51);
        data.Add(255);
        data.Add(217);
        return data.ToArray();
    }

    internal static byte[] CreateExifResolution(int horizontalDpi, int verticalDpi, int unit)
    {
        List<byte> data = new List<byte>();
        data.AddRange(Encoding.ASCII.GetBytes("Exif\0\0"));
        data.Add(73);
        data.Add(73);
        AddLittleUInt16(data, 42);
        AddLittleUInt32(data, 8);
        AddLittleUInt16(data, 3);
        AddExifRationalEntry(data, 282, 46);
        AddExifRationalEntry(data, 283, 54);
        AddLittleUInt16(data, 296);
        AddLittleUInt16(data, 3);
        AddLittleUInt32(data, 1);
        AddLittleUInt16(data, unit);
        AddLittleUInt16(data, 0);
        AddLittleUInt32(data, horizontalDpi);
        AddLittleUInt32(data, 1);
        AddLittleUInt32(data, verticalDpi);
        AddLittleUInt32(data, 1);
        return data.ToArray();
    }

    internal static void AddExifRationalEntry(List<byte> data, int tag, int valueOffset)
    {
        AddLittleUInt16(data, tag);
        AddLittleUInt16(data, 5);
        AddLittleUInt32(data, 1);
        AddLittleUInt32(data, valueOffset);
    }

    internal static byte[] InsertAfterStart(byte[] jpegData, int marker, byte[] payload)
    {
        List<byte> segment = new List<byte>();
        AddSegment(segment, marker, payload);
        byte[] result = new byte[jpegData.Length + segment.Count];
        Array.Copy(jpegData, 0, result, 0, 2);
        segment.CopyTo(result, 2);
        Array.Copy(jpegData, 2, result, 2 + segment.Count, jpegData.Length - 2);
        return result;
    }

    internal static void AddSegment(List<byte> data, int marker, byte[] payload)
    {
        data.Add(255);
        data.Add((byte)marker);
        AddUInt16(data, payload.Length + 2);
        data.AddRange(payload);
    }

    internal static void AddUInt16(List<byte> data, int value)
    {
        data.Add((byte)(value >> 8));
        data.Add((byte)value);
    }

    internal static void AddLittleUInt16(List<byte> data, int value)
    {
        data.Add((byte)value);
        data.Add((byte)(value >> 8));
    }

    internal static void AddLittleUInt32(List<byte> data, int value)
    {
        data.Add((byte)value);
        data.Add((byte)(value >> 8));
        data.Add((byte)(value >> 16));
        data.Add((byte)(value >> 24));
    }

    internal static int FindMarker(byte[] data, int marker)
    {
        for (int index = 0; index < data.Length - 1; index++)
        {
            if (data[index] == 255 && data[index + 1] == marker)
            {
                return index;
            }
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
