using raw;

namespace raw.Tests;

public class BaselineJpegTests
{
    [Test]
    public void GrayscaleBlocksApplyQuantizationAndInverseDct()
    {
        byte[] quantization = Enumerable.Repeat((byte)1, 64).ToArray();
        quantization[0] = 2;
        Block block = new Block(0, 80, 8);
        byte[] jpeg = BuildInterleaved(8, 8, new byte[] { 0x11 }, quantization, new Block[] { block }, 0);

        BaselineJpegImage image = BaselineJpegCodec.Decode(jpeg, new RawLimits(), CancellationToken.None);
        BaselineJpegPlane plane = image.Planes.Single();

        Assert.Multiple(() =>
        {
            Assert.That(image.Width, Is.EqualTo(8));
            Assert.That(image.Height, Is.EqualTo(8));
            Assert.That(image.Precision, Is.EqualTo(8));
            Assert.That(((plane.Samples ?? throw new System.InvalidOperationException("A required value is unavailable.")).Take(8) ?? throw new System.InvalidOperationException("A required value is unavailable.")), Is.EqualTo(new byte[] { 149, 149, 149, 148, 148, 147, 147, 147 }));
            Assert.That(plane.Samples.Chunk(8).All(row => row.SequenceEqual(plane.Samples.Take(8))), Is.True);
        });
    }

    [Test]
    public void CommonYcbcrSamplingModesProduceNativeComponentPlanes()
    {
        foreach (byte sampling in new byte[] { 0x11, 0x21, 0x22 })
        {
            int horizontal = sampling >> 4;
            int vertical = sampling & 15;
            int mcuColumns = DivideRoundUp(17, horizontal * 8);
            int mcuRows = DivideRoundUp(17, vertical * 8);
            List<Block> blocks = new List<Block>();
            int lumaIndex = 0;
            for (int mcuY = 0; mcuY < mcuRows; mcuY++)
            {
                for (int mcuX = 0; mcuX < mcuColumns; mcuX++)
                {
                    for (int component = 0; component < 3; component++)
                    {
                        int componentHorizontal = component == 0 ? horizontal : 1;
                        int componentVertical = component == 0 ? vertical : 1;
                        for (int y = 0; y < componentVertical; y++)
                        {
                            for (int x = 0; x < componentHorizontal; x++)
                            {
                                int dc = component == 0 ? (10 + lumaIndex++) * 8 : component == 1 ? -80 : 160;
                                blocks.Add(new Block(component, dc, 0));
                            }
                        }
                    }
                }
            }
            byte[] jpeg = BuildInterleaved(17, 17, new byte[] { sampling, 0x11, 0x11 }, Enumerable.Repeat((byte)1, 64).ToArray(), blocks.ToArray(), 0);

            BaselineJpegImage image = BaselineJpegCodec.Decode(jpeg, new RawLimits(), CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(image.Planes.Select(plane => plane.Identifier), Is.EqualTo(new byte[] { 1, 2, 3 }), $"sampling {sampling:x2}");
                Assert.That(image.GetPlane(1).Width, Is.EqualTo(17), $"sampling {sampling:x2}");
                Assert.That(image.GetPlane(1).Height, Is.EqualTo(17), $"sampling {sampling:x2}");
                Assert.That(image.GetPlane(2).Width, Is.EqualTo(DivideRoundUp(17, horizontal)), $"sampling {sampling:x2}");
                Assert.That(image.GetPlane(2).Height, Is.EqualTo(DivideRoundUp(17, vertical)), $"sampling {sampling:x2}");
                Assert.That(image.GetPlane(3).Width, Is.EqualTo(DivideRoundUp(17, horizontal)), $"sampling {sampling:x2}");
                Assert.That(image.GetPlane(3).Height, Is.EqualTo(DivideRoundUp(17, vertical)), $"sampling {sampling:x2}");
                Assert.That((image.GetPlane(1).Samples ?? throw new System.InvalidOperationException("A required value is unavailable."))[0], Is.EqualTo(138), $"sampling {sampling:x2}");
                Assert.That((image.GetPlane(1).Samples ?? throw new System.InvalidOperationException("A required value is unavailable."))[8], Is.EqualTo(139), $"sampling {sampling:x2}");
                Assert.That(image.GetPlane(2).Samples, Is.All.EqualTo(118), $"sampling {sampling:x2}");
                Assert.That(image.GetPlane(3).Samples, Is.All.EqualTo(148), $"sampling {sampling:x2}");
            });
        }
    }

    [Test]
    public void SeparateSequentialScansRetainComponentOrder()
    {
        byte[] tables = Join(QuantizationMarker(Enumerable.Repeat((byte)1, 64).ToArray()), HuffmanMarker());
        byte[] frame = Frame(8, 8, new byte[] { 0x11, 0x11, 0x11 });
        byte[] jpeg = Join(
            new byte[] { 0xff, 0xd8 }, tables, frame,
            Scan(new byte[] { 1 }, 0, 63, 0), EncodeEntropy(new Block[] { new Block(0, 96, 0) }, 1),
            Scan(new byte[] { 2 }, 0, 63, 0), EncodeEntropy(new Block[] { new Block(0, -64, 0) }, 1),
            Scan(new byte[] { 3 }, 0, 63, 0), EncodeEntropy(new Block[] { new Block(0, 192, 0) }, 1),
            new byte[] { 0xff, 0xd9 });

        BaselineJpegImage image = BaselineJpegCodec.Decode(jpeg, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(image.Planes.Select(plane => plane.Identifier), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(image.GetPlane(1).Samples, Is.All.EqualTo(140));
            Assert.That(image.GetPlane(2).Samples, Is.All.EqualTo(120));
            Assert.That(image.GetPlane(3).Samples, Is.All.EqualTo(152));
        });
    }

    [Test]
    public void RestartIntervalsResetDcPredictionAtMcuBoundaries()
    {
        byte[] tables = Join(QuantizationMarker(Enumerable.Repeat((byte)1, 64).ToArray()), HuffmanMarker());
        byte[] entropy = Join(
            EncodeEntropy(new Block[] { new Block(0, 80, 0) }, 1), new byte[] { 0xff, 0xd0 },
            EncodeEntropy(new Block[] { new Block(0, 160, 0) }, 1), new byte[] { 0xff, 0xd1 },
            EncodeEntropy(new Block[] { new Block(0, -80, 0) }, 1));
        byte[] jpeg = Join(
            new byte[] { 0xff, 0xd8 }, tables, Frame(24, 8, new byte[] { 0x11 }), Segment(0xdd, new byte[] { 0, 1 }),
            Scan(new byte[] { 1 }, 0, 63, 0), entropy, new byte[] { 0xff, 0xd9 });

        BaselineJpegImage image = BaselineJpegCodec.Decode(jpeg, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(((image.Planes.Single().Samples ?? throw new System.InvalidOperationException("A required value is unavailable.")).Take(8) ?? throw new System.InvalidOperationException("A required value is unavailable.")), Is.All.EqualTo(138));
            Assert.That(((image.Planes.Single().Samples ?? throw new System.InvalidOperationException("A required value is unavailable.")).Skip(8).Take(8) ?? throw new System.InvalidOperationException("A required value is unavailable.")), Is.All.EqualTo(148));
            Assert.That(((image.Planes.Single().Samples ?? throw new System.InvalidOperationException("A required value is unavailable.")).Skip(16).Take(8) ?? throw new System.InvalidOperationException("A required value is unavailable.")), Is.All.EqualTo(118));
        });
    }

    [Test]
    public void StandardLibJpegGrayscaleStreamMatchesReferenceSamples()
    {
        byte[] jpeg = Convert.FromBase64String("/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAYEBQYFBAYGBQYHBwYIChAKCgkJChQODwwQFxQYGBcUFhYaHSUfGhsjHBYWICwgIyYnKSopGR8tMC0oMCUoKSj/wAALCAAIAAgBAREA/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/9oACAEBAAA/AE/4Z6/6c/8Ax2v/2Q==");
        byte[] expected = new byte[]
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            35, 35, 35, 35, 35, 35, 35, 35,
            74, 74, 74, 74, 74, 74, 74, 74,
            108, 108, 108, 108, 108, 108, 108, 108,
            146, 146, 146, 146, 146, 146, 146, 146,
            181, 181, 181, 181, 181, 181, 181, 181,
            219, 219, 219, 219, 219, 219, 219, 219,
            255, 255, 255, 255, 255, 255, 255, 255
        };

        BaselineJpegImage image = BaselineJpegCodec.Decode(jpeg, new RawLimits(), CancellationToken.None);

        Assert.That(((image.Planes.Single().Samples ?? throw new System.InvalidOperationException("A required value is unavailable.")).Zip(expected, (actual, reference) => Math.Abs(actual - reference)) ?? throw new System.InvalidOperationException("A required value is unavailable.")), Is.All.LessThanOrEqualTo(1));
    }

    [Test]
    public void InvalidProcessSamplingTablesAndScanParametersAreRejected()
    {
        byte[] valid = BuildInterleaved(8, 8, new byte[] { 0x11 }, Enumerable.Repeat((byte)1, 64).ToArray(), new Block[] { new Block(0, 0, 0) }, 0);
        byte[] lossless = (byte[])valid.Clone();
        lossless[FindMarker(lossless, 0xc0) + 1] = 0xc3;
        byte[] uncommonSampling = BuildInterleaved(8, 8, new byte[] { 0x12, 0x11, 0x11 }, Enumerable.Repeat((byte)1, 64).ToArray(), new Block[] { new Block(0, 0, 0), new Block(0, 0, 0), new Block(1, 0, 0), new Block(2, 0, 0) }, 0);
        byte[] missingQuantization = RemoveMarker(valid, 0xdb);
        byte[] undefinedAc = (byte[])valid.Clone();
        int scanOffset = FindMarker(undefinedAc, 0xda);
        undefinedAc[scanOffset + 6] = 1;
        byte[] badSequential = (byte[])valid.Clone();
        badSequential[scanOffset + 6 + 1] = 1;

        Assert.Multiple(() =>
        {
            Assert.Throws<RawUnsupportedException>(() => BaselineJpegCodec.Decode(lossless, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawUnsupportedException>(() => BaselineJpegCodec.Decode(uncommonSampling, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => BaselineJpegCodec.Decode(missingQuantization, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => BaselineJpegCodec.Decode(undefinedAc, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => BaselineJpegCodec.Decode(badSequential, new RawLimits(), CancellationToken.None));
        });
    }

    [Test]
    public void DecodeHonorsOutputLimitsCancellationAndRequiredDocument()
    {
        byte[] jpeg = BuildInterleaved(8, 8, new byte[] { 0x11 }, Enumerable.Repeat((byte)1, 64).ToArray(), new Block[] { new Block(0, 0, 0) }, 0);
        RawLimits limits = new RawLimits();
        limits.MaxDecodedBytes = 63;
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.Throws<RawLimitException>(() => BaselineJpegCodec.Decode(jpeg, limits, CancellationToken.None));
            Assert.Throws<OperationCanceledException>(() => BaselineJpegCodec.Decode(jpeg, new RawLimits(), cancellation.Token));
            Assert.Throws<RawFormatException>(() => BaselineJpegCodec.Decode((JpegDocument?)null, new RawLimits(), CancellationToken.None));
        });
    }

    private class Block
    {
        internal int Component;
        internal int Dc;
        internal int FirstAc;

        internal Block(int component, int dc, int firstAc)
        {
            Component = component;
            Dc = dc;
            FirstAc = firstAc;
        }
    }

    private static byte[] BuildInterleaved(int width, int height, byte[] sampling, byte[] quantization, Block[] blocks, int restartInterval)
    {
        byte[] identifiers = Enumerable.Range(1, sampling.Length).Select(value => (byte)value).ToArray();
        List<byte[]> parts = new List<byte[]>();
        parts.Add(new byte[] { 0xff, 0xd8 });
        parts.Add(QuantizationMarker(quantization));
        parts.Add(HuffmanMarker());
        parts.Add(Frame(width, height, sampling));
        if (restartInterval != 0)
        {
            parts.Add(Segment(0xdd, new byte[] { (byte)(restartInterval >> 8), (byte)restartInterval }));
        }
        parts.Add(Scan(identifiers, 0, 63, 0));
        parts.Add(EncodeEntropy(blocks, sampling.Length));
        parts.Add(new byte[] { 0xff, 0xd9 });
        return Join(parts.ToArray());
    }

    private static byte[] Frame(int width, int height, byte[] sampling)
    {
        List<byte> payload = new List<byte>
        {
            8, (byte)(height >> 8), (byte)height, (byte)(width >> 8), (byte)width, (byte)sampling.Length
        };
        for (int index = 0; index < sampling.Length; index++)
        {
            payload.Add((byte)(index + 1));
            payload.Add(sampling[index]);
            payload.Add(0);
        }
        return Segment(0xc0, payload.ToArray());
    }

    private static byte[] Scan(byte[] identifiers, int spectralStart, int spectralEnd, int approximation)
    {
        List<byte> payload = new List<byte> { (byte)identifiers.Length };
        foreach (byte identifier in identifiers)
        {
            payload.Add(identifier);
            payload.Add(0);
        }
        payload.Add((byte)spectralStart);
        payload.Add((byte)spectralEnd);
        payload.Add((byte)approximation);
        return Segment(0xda, payload.ToArray());
    }

    private static byte[] QuantizationMarker(byte[] values)
    {
        return Segment(0xdb, Join(new byte[] { 0 }, values));
    }

    private static byte[] HuffmanMarker()
    {
        byte[] counts = new byte[16];
        counts[3] = 12;
        byte[] dcSymbols = Enumerable.Range(0, 12).Select(value => (byte)value).ToArray();
        byte[] acSymbols = Join(new byte[] { 0 }, Enumerable.Range(1, 10).Select(value => (byte)value).ToArray(), new byte[] { 0xf0 });
        return Segment(0xc4, Join(new byte[] { 0 }, counts, dcSymbols, new byte[] { 0x10 }, counts, acSymbols));
    }

    private static byte[] EncodeEntropy(Block[] blocks, int componentCount)
    {
        int[] predictions = new int[componentCount];
        List<int> bits = new List<int>();
        foreach (Block block in blocks)
        {
            int difference = block.Dc - predictions[block.Component];
            predictions[block.Component] = block.Dc;
            EncodeValue(bits, difference, 4);
            if (block.FirstAc != 0)
            {
                EncodeValue(bits, block.FirstAc, 4);
            }
            AppendBits(bits, 0, 4);
        }
        while (bits.Count % 8 != 0)
        {
            bits.Add(1);
        }
        List<byte> bytes = new List<byte>();
        for (int offset = 0; offset < bits.Count; offset += 8)
        {
            byte value = 0;
            for (int bit = 0; bit < 8; bit++)
            {
                value = (byte)((value << 1) | bits[offset + bit]);
            }
            bytes.Add(value);
            if (value == 0xff)
            {
                bytes.Add(0);
            }
        }
        return bytes.ToArray();
    }

    private static void EncodeValue(List<int> bits, int value, int huffmanWidth)
    {
        int magnitude = Math.Abs(value);
        int category = 0;
        while (magnitude != 0)
        {
            category++;
            magnitude >>= 1;
        }
        AppendBits(bits, category, huffmanWidth);
        if (category != 0)
        {
            int amplitude = value < 0 ? value + (1 << category) - 1 : value;
            AppendBits(bits, amplitude, category);
        }
    }

    private static void AppendBits(List<int> bits, int value, int count)
    {
        for (int bit = count - 1; bit >= 0; bit--)
        {
            bits.Add((value >> bit) & 1);
        }
    }

    private static int FindMarker(byte[] jpeg, byte marker)
    {
        for (int index = 0; index < jpeg.Length - 1; index++)
        {
            if (jpeg[index] == 0xff && jpeg[index + 1] == marker)
            {
                return index;
            }
        }
        throw new InvalidOperationException();
    }

    private static byte[] RemoveMarker(byte[] jpeg, byte marker)
    {
        int offset = FindMarker(jpeg, marker);
        int length = jpeg[offset + 2] * 256 + jpeg[offset + 3] + 2;
        return jpeg[..offset].Concat(jpeg[(offset + length)..]).ToArray();
    }

    private static int DivideRoundUp(int value, int divisor)
    {
        return (value + divisor - 1) / divisor;
    }

    private static byte[] Segment(byte marker, byte[] payload)
    {
        int length = payload.Length + 2;
        return Join(new byte[] { 0xff, marker, (byte)(length >> 8), (byte)length }, payload);
    }

    private static byte[] Join(params byte[][] arrays)
    {
        int length = arrays.Sum(array => array.Length);
        byte[] result = new byte[length];
        int offset = 0;
        foreach (byte[] array in arrays)
        {
            array.CopyTo(result, offset);
            offset += array.Length;
        }
        return result;
    }
}
