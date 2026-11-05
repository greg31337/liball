using raw;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace raw.Tests;

public class MutationPropertyTests
{
    const int MutationCount = 96;
    const string Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

    [Test]
    public void TiffSingleBitMutationsEitherParseToAValidatableGraphOrReturnABoundedDiagnostic()
    {
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(RawByteOrder.LittleEndian, false);
        SyntheticTiffIfd root = builder.AddIfd();
        SyntheticTiffIfd child = builder.AddIfd();
        builder.AddPointer(root, 330, TiffFieldType.Ifd, child);
        builder.AddField(root, 0x9000, TiffFieldType.Undefined, 17, Enumerable.Range(0, 17).Select(value => (byte)value).ToArray());
        builder.AddField(child, 0x9001, TiffFieldType.Undefined, 9, Enumerable.Repeat((byte)0xa5, 9).ToArray());
        byte[] source = builder.Build();
        Random random = new Random(112001);
        int accepted = 0;
        int rejected = 0;

        for (int iteration = 0; iteration < MutationCount; iteration++)
        {
            byte[] mutated = MutateOneBit(source, random);
            if (TryTiff(mutated))
            {
                accepted++;
            }
            else
            {
                rejected++;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.GreaterThan(0));
            Assert.That(rejected, Is.GreaterThan(0));
            Assert.That(accepted + rejected, Is.EqualTo(MutationCount));
        });
    }

    [Test]
    public void JpegSingleBitMutationsEitherDecodeWithBoundedPlanesOrReturnABoundedDiagnostic()
    {
        BaselineJpegPlane plane = new BaselineJpegPlane();
        plane.Identifier = 1;
        plane.Width = 8;
        plane.Height = 8;
        plane.HorizontalSampling = 1;
        plane.VerticalSampling = 1;
        plane.Samples = Enumerable.Range(0, 64).Select(value => (byte)(value * 3)).ToArray();
        BaselineJpegImage image = new BaselineJpegImage();
        image.Width = 8;
        image.Height = 8;
        image.Precision = 8;
        image.Planes.Add(plane);
        byte[] source = BaselineJpegEncoder.Encode(image, new BaselineJpegEncodeParameters(91, 1, 1, 0), new RawLimits(), CancellationToken.None);
        Random random = new Random(112002);
        int accepted = 0;
        int rejected = 0;

        for (int iteration = 0; iteration < MutationCount; iteration++)
        {
            byte[] mutated = MutateOneBit(source, random);
            if (TryJpeg(mutated))
            {
                accepted++;
            }
            else
            {
                rejected++;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.GreaterThan(0));
            Assert.That(rejected, Is.GreaterThan(0));
            Assert.That(accepted + rejected, Is.EqualTo(MutationCount));
        });
    }

    [Test]
    public void HuffmanCountMutationsAreEitherCanonicalOrPreciselyOversubscribed()
    {
        byte[] complete = new byte[16];
        complete[0] = 1;
        complete[1] = 1;
        complete[2] = 2;
        Random random = new Random(112003);
        int accepted = 0;
        int rejected = 0;

        for (int iteration = 0; iteration < MutationCount; iteration++)
        {
            byte[] counts = complete.ToArray();
            int lengthIndex = random.Next(counts.Length);
            if ((iteration & 1) == 0)
            {
                counts[lengthIndex]++;
            }
            else
            {
                int populated = random.Next(3);
                if (counts[populated] != 0)
                {
                    counts[populated]--;
                }
            }
            int symbolCount = counts.Sum(value => value);
            byte[] symbols = Enumerable.Range(0, symbolCount).Select(value => (byte)value).ToArray();
            try
            {
                HuffmanTable table = new HuffmanTable(counts, symbols, 8);
                RoundTripHuffman(table, symbols);
                accepted++;
            }
            catch (RawFormatException exception)
            {
                Assert.That(exception.Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
                rejected++;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.GreaterThan(0));
            Assert.That(rejected, Is.GreaterThan(0));
            Assert.That(accepted + rejected, Is.EqualTo(MutationCount));
        });
    }

    [TestCase(RawBitOrder.MostSignificantBitFirst)]
    [TestCase(RawBitOrder.LeastSignificantBitFirst)]
    public void ArbitraryPackedBitPatternsReachAStableCanonicalRoundTrip(RawBitOrder order)
    {
        Random random = new Random(112004 + (int)order);
        int accepted = 0;
        int rejected = 0;
        for (int iteration = 0; iteration < MutationCount; iteration++)
        {
            int count = random.Next(1, 65);
            int bitDepth = random.Next(1, 33);
            byte[] bytes = new byte[BitPacking.GetPackedByteCount(count, bitDepth)];
            if ((iteration & 1) == 0)
            {
                uint mask = bitDepth == 32 ? uint.MaxValue : (1U << bitDepth) - 1;
                uint[] values = new uint[count];
                for (int index = 0; index < values.Length; index++)
                {
                    values[index] = ((uint)random.Next(ushort.MaxValue + 1) << 16 | (uint)random.Next(ushort.MaxValue + 1)) & mask;
                }
                BitPacking.PackUnsigned(values, bitDepth, order, bytes);
            }
            else
            {
                random.NextBytes(bytes);
            }
            uint[] first = new uint[count];
            uint[] second = new uint[count];
            byte[] canonical = new byte[bytes.Length];

            try
            {
                BitPacking.UnpackUnsigned(bytes, count, bitDepth, order, first);
                BitPacking.PackUnsigned(first, bitDepth, order, canonical);
                BitPacking.UnpackUnsigned(canonical, count, bitDepth, order, second);
                Assert.That(second, Is.EqualTo(first), "iteration " + iteration);
                accepted++;
            }
            catch (RawFormatException exception)
            {
                Assert.That(exception.Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
                rejected++;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.GreaterThan(0));
            Assert.That(rejected, Is.GreaterThan(0));
            Assert.That(accepted + rejected, Is.EqualTo(MutationCount));
        });
    }

    [Test]
    public void ProfileJsonMutationsEitherRoundTripCanonicallyOrReturnASchemaDiagnostic()
    {
        byte[] source = RawProfileSchemaCodec.Write(RawBuiltInProfileCatalogue.CreateDocument(), new RawLimits());
        Random random = new Random(112005);
        int accepted = 0;
        int rejected = 0;

        for (int iteration = 0; iteration < MutationCount; iteration++)
        {
            byte[] mutated = MutateOneBit(source, random);
            if (TryProfile(mutated))
            {
                accepted++;
            }
            else
            {
                rejected++;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.GreaterThan(0));
            Assert.That(rejected, Is.GreaterThan(0));
            Assert.That(accepted + rejected, Is.EqualTo(MutationCount));
        });
    }

    [TestCase(RawXmpEncoding.Utf8)]
    [TestCase(RawXmpEncoding.Utf16BigEndian)]
    [TestCase(RawXmpEncoding.Utf16LittleEndian)]
    [TestCase(RawXmpEncoding.Utf32BigEndian)]
    [TestCase(RawXmpEncoding.Utf32LittleEndian)]
    public void XmpMutationsEitherProduceAStablePacketOrReturnAnXmlDiagnostic(RawXmpEncoding encoding)
    {
        string xml = "<x:xmpmeta xmlns:x='adobe:ns:meta/'><rdf:RDF xmlns:rdf='" + Rdf + "'><rdf:Description rdf:about='' xmlns:t='urn:test' t:value='42'><t:name>camera</t:name></rdf:Description></rdf:RDF></x:xmpmeta>";
        byte[] source = EncodeXmp(xml, encoding);
        byte[] invalid = InvalidEncodedXmp(source, encoding);
        RawFormatException? invalidException = Assert.Throws<RawFormatException>(() => RawXmpPacket.Parse(invalid, new RawLimits()));
        Assert.That((invalidException ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));

        Random random = new Random(112006 + (int)encoding);
        int accepted = 0;
        int rejected = 0;

        for (int iteration = 0; iteration < MutationCount; iteration++)
        {
            byte[] mutated = MutateOneBit(source, random);
            if (TryXmp(mutated))
            {
                accepted++;
            }
            else
            {
                rejected++;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.GreaterThan(0));
            Assert.That(rejected, Is.GreaterThan(0));
            Assert.That(accepted + rejected, Is.EqualTo(MutationCount));
        });
    }

    [Test]
    public void RandomPositiveCr2SlicePartitionsRoundTripAndCoverageMutationsFail()
    {
        Random random = new Random(112007);
        for (int iteration = 0; iteration < MutationCount; iteration++)
        {
            int width = random.Next(1, 65);
            int height = random.Next(1, 9);
            int[] slices = Partition(width, random);
            ushort[] expected = new ushort[width * height];
            for (int index = 0; index < expected.Length; index++)
            {
                expected[index] = (ushort)random.Next(ushort.MaxValue + 1);
            }
            ushort[] encoded = Slice(expected, width, height, slices);

            ushort[] actual = Cr2LosslessJpegDecoder.Unslice(encoded, width, height, slices, new RawLimits(), CancellationToken.None);
            Assert.That(actual, Is.EqualTo(expected), "iteration " + iteration);

            int[] mutated = slices.ToArray();
            mutated[random.Next(mutated.Length)]++;
            RawFormatException? exception = Assert.Throws<RawFormatException>(() => Cr2LosslessJpegDecoder.ValidateSlices(mutated, width));
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
        }
    }

    [TestCase(12)]
    [TestCase(14)]
    public void RandomNikonLosslessStreamsRoundTripExactStoredSamples(int precision)
    {
        Random random = new Random(112008 + precision);
        int maximum = (1 << precision) - 1;
        for (int iteration = 0; iteration < 48; iteration++)
        {
            int width = random.Next(2, 17);
            int height = random.Next(2, 9);
            int stride = width + random.Next(0, 4);
            ushort[] samples = new ushort[stride * height];
            for (int index = 0; index < samples.Length; index++)
            {
                samples[index] = (ushort)random.Next(maximum + 1);
            }
            NikonLosslessEncoding encoded = NikonCompressionCodec.EncodeLossless(samples, width, height, stride, precision, RawByteOrder.LittleEndian, new RawLimits(), CancellationToken.None);
            NikonCompressionInfo info = NikonCompressionCodec.ParseInfo(encoded.CompressionInfo, RawByteOrder.LittleEndian, precision, height);
            ushort[] actual = new ushort[width * height];
            MsbBitReader reader = new MsbBitReader(new BoundedInput(encoded.EntropyBytes, 0, encoded.EntropyBytes.Length, new RawLimits()));
            NikonCompressionCodec.DecodeSamples(reader, actual, width, height, info, new RawDecodeOptions());
            reader.AlignToByteWithExpectedPadding(0);
            reader.RequireEnd();

            ushort[] expected = CompactRows(samples, width, height, stride);
            Assert.That(actual, Is.EqualTo(expected), "iteration " + iteration);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RandomTiffLayoutsSerializeDeterministicallyAndReparse(bool bigTiff)
    {
        Random random = new Random(112009 + (bigTiff ? 1 : 0));
        for (int iteration = 0; iteration < 48; iteration++)
        {
            RawByteOrder order = (iteration & 1) == 0 ? RawByteOrder.LittleEndian : RawByteOrder.BigEndian;
            TiffLayoutRequest request = RandomLayout(random, bigTiff, order);
            TiffLayoutPlan first = TiffLayoutPlanner.Plan(request, new RawLimits(), CancellationToken.None);
            TiffLayoutPlan second = TiffLayoutPlanner.Plan(request, new RawLimits(), CancellationToken.None);
            byte[] firstBytes = TiffWriter.WriteToArray(first, new RawLimits(), CancellationToken.None);
            byte[] secondBytes = TiffWriter.WriteToArray(second, new RawLimits(), CancellationToken.None);
            TiffIfdGraph graph = TiffParser.Parse(new BoundedInput(firstBytes, 0, firstBytes.Length, new RawLimits()));
            RawValidationReport report = TiffValidator.Validate(graph, true);

            Assert.Multiple(() =>
            {
                Assert.That(firstBytes, Is.EqualTo(secondBytes), "iteration " + iteration);
                Assert.That(first.PredictedSize, Is.EqualTo(firstBytes.Length), "iteration " + iteration);
                Assert.That(report.IsValid(), Is.True, "iteration " + iteration);
            });
        }
    }

    static byte[] MutateOneBit(byte[] source, Random random)
    {
        byte[] mutated = source.ToArray();
        int bit = random.Next(mutated.Length * 8);
        mutated[bit / 8] ^= (byte)(1 << (bit & 7));
        return mutated;
    }

    static bool TryTiff(byte[] bytes)
    {
        try
        {
            TiffIfdGraph graph = TiffParser.Parse(new BoundedInput(bytes, 0, bytes.Length, new RawLimits()));
            RawValidationReport report = TiffValidator.Validate(graph, true);
            Assert.That(report.Messages, Is.Not.Null);
            return true;
        }
        catch (RawFormatException exception)
        {
            Assert.That(exception.Code, Is.Not.EqualTo(RawDiagnosticCode.None));
            return false;
        }
        catch (RawLimitException exception)
        {
            Assert.That(exception.Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            return false;
        }
    }

    static bool TryJpeg(byte[] bytes)
    {
        try
        {
            BaselineJpegImage image = BaselineJpegCodec.Decode(bytes, new RawLimits(), CancellationToken.None);
            Assert.That(image.Width, Is.GreaterThan(0));
            Assert.That(image.Height, Is.GreaterThan(0));
            Assert.That(image.Planes.All(value => (value.Samples ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length == value.Width * value.Height), Is.True);
            return true;
        }
        catch (RawFormatException exception)
        {
            Assert.That(exception.Code, Is.Not.EqualTo(RawDiagnosticCode.None));
            return false;
        }
        catch (RawLimitException exception)
        {
            Assert.That(exception.Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            return false;
        }
        catch (RawUnsupportedException exception)
        {
            Assert.That(exception.Code, Is.Not.EqualTo(RawDiagnosticCode.None));
            return false;
        }
    }

    static void RoundTripHuffman(HuffmanTable table, byte[] symbols)
    {
        int bitCount = symbols.Sum(value => table.GetEncodeCodeLength(value));
        byte[] bytes = new byte[(bitCount + 7) / 8];
        MsbBitWriter writer = new MsbBitWriter(bytes);
        foreach (byte symbol in symbols)
        {
            table.EncodeSymbol(writer, symbol);
        }
        writer.Finish(0);
        MsbBitReader reader = new MsbBitReader(new BoundedInput(bytes, 0, bytes.Length, new RawLimits()));
        foreach (byte symbol in symbols)
        {
            Assert.That(table.DecodeSymbol(reader), Is.EqualTo(symbol));
        }
    }

    static bool TryProfile(byte[] json)
    {
        try
        {
            RawProfileSchemaDocument document = RawProfileSchemaCodec.Read(json, new RawLimits());
            byte[] canonical = RawProfileSchemaCodec.Write(document, new RawLimits());
            RawProfileSchemaDocument reparsed = RawProfileSchemaCodec.Read(canonical, new RawLimits());
            Assert.That(reparsed.Validate(new RawLimits()).IsValid(), Is.True);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (RawFormatException exception)
        {
            Assert.That(exception.Code, Is.Not.EqualTo(RawDiagnosticCode.None));
            return false;
        }
        catch (RawLimitException exception)
        {
            Assert.That(exception.Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            return false;
        }
        catch (RawUnsupportedException exception)
        {
            Assert.That(exception.Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
            return false;
        }
    }

    static bool TryXmp(byte[] bytes)
    {
        try
        {
            RawXmpPacket packet = RawXmpPacket.Parse(bytes, new RawLimits());
            Assert.That(packet.RawBytes, Is.EqualTo(bytes));
            Assert.That(packet.XmlText, Is.Not.Empty);
            return true;
        }
        catch (XmlException)
        {
            return false;
        }
        catch (RawFormatException exception)
        {
            Assert.That(exception.Code, Is.Not.EqualTo(RawDiagnosticCode.None));
            return false;
        }
        catch (RawLimitException exception)
        {
            Assert.That(exception.Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            return false;
        }
    }

    static int[] Partition(int width, Random random)
    {
        List<int> slices = new List<int>();
        int remaining = width;
        while (remaining != 0)
        {
            int next = random.Next(1, remaining + 1);
            slices.Add(next);
            remaining -= next;
        }
        return slices.ToArray();
    }

    static ushort[] Slice(ushort[] source, int width, int height, int[] slices)
    {
        ushort[] result = new ushort[source.Length];
        int destination = 0;
        int sliceX = 0;
        foreach (int sliceWidth in slices)
        {
            for (int row = 0; row < height; row++)
            {
                Array.Copy(source, row * width + sliceX, result, destination, sliceWidth);
                destination += sliceWidth;
            }
            sliceX += sliceWidth;
        }
        return result;
    }

    static ushort[] CompactRows(ushort[] source, int width, int height, int stride)
    {
        ushort[] result = new ushort[width * height];
        for (int row = 0; row < height; row++)
        {
            Array.Copy(source, row * stride, result, row * width, width);
        }
        return result;
    }

    static byte[] EncodeXmp(string xml, RawXmpEncoding encoding)
    {
        Encoding textEncoding;
        switch (encoding)
        {
            case RawXmpEncoding.Utf8:
                textEncoding = new UTF8Encoding(true, true);
                break;
            case RawXmpEncoding.Utf16BigEndian:
                textEncoding = new UnicodeEncoding(true, true, true);
                break;
            case RawXmpEncoding.Utf16LittleEndian:
                textEncoding = new UnicodeEncoding(false, true, true);
                break;
            case RawXmpEncoding.Utf32BigEndian:
                textEncoding = new UTF32Encoding(true, true, true);
                break;
            default:
                textEncoding = new UTF32Encoding(false, true, true);
                break;
        }
        byte[] preamble = textEncoding.GetPreamble();
        byte[] text = textEncoding.GetBytes(xml);
        return preamble.Concat(text).ToArray();
    }

    static byte[] InvalidEncodedXmp(byte[] source, RawXmpEncoding encoding)
    {
        byte[] invalid = source.ToArray();
        if (encoding == RawXmpEncoding.Utf8)
        {
            invalid[3] = 0xff;
        }
        else if (encoding == RawXmpEncoding.Utf16BigEndian)
        {
            invalid[2] = 0xd8;
            invalid[3] = 0;
        }
        else if (encoding == RawXmpEncoding.Utf16LittleEndian)
        {
            invalid[2] = 0;
            invalid[3] = 0xd8;
        }
        else if (encoding == RawXmpEncoding.Utf32BigEndian)
        {
            invalid[4] = 0;
            invalid[5] = 0x11;
            invalid[6] = 0;
            invalid[7] = 0;
        }
        else
        {
            invalid[4] = 0;
            invalid[5] = 0;
            invalid[6] = 0x11;
            invalid[7] = 0;
        }
        return invalid;
    }

    static TiffLayoutRequest RandomLayout(Random random, bool bigTiff, RawByteOrder order)
    {
        int ifdCount = random.Next(1, 5);
        TiffLayoutIfdInput[] ifds = new TiffLayoutIfdInput[ifdCount];
        for (int ifdIndex = 0; ifdIndex < ifdCount; ifdIndex++)
        {
            int entryCount = random.Next(0, 9);
            TiffLayoutEntryInput[] entries = new TiffLayoutEntryInput[entryCount];
            for (int entryIndex = 0; entryIndex < entryCount; entryIndex++)
            {
                int length = random.Next(1, 25);
                byte[] value = new byte[length];
                random.NextBytes(value);
                entries[entryIndex] = new TiffLayoutEntryInput((ushort)(0x8000 + entryIndex), TiffFieldType.Undefined, (ulong)length, value, null);
            }
            string path = "IFD" + ifdIndex;
            string? next = ifdIndex + 1 < ifdCount ? "IFD" + (ifdIndex + 1) : null;
            ifds[ifdIndex] = new TiffLayoutIfdInput(path, entries, next);
        }
        return new TiffLayoutRequest(bigTiff, order, "IFD0", ifds, Array.Empty<TiffLayoutBlockInput>());
    }
}
