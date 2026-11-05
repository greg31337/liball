using System.Buffers.Binary;

namespace avif.Tests;

public class RandomizedParserTests
{
    const int BitFieldSeed = 1597463007;
    const int VariableCodeSeed = 324508639;
    const int BoxSeed = 610839776;
    const int ObuSeed = 270544960;
    const int MalformedSeed = 1437226410;

    [Test]
    public void RandomizedBitFieldsRoundTripAcrossArbitraryBoundaries()
    {
        Random random = new Random(BitFieldSeed);
        int scenario = 0;
        while (scenario < 256)
        {
            int byteLength = random.Next(1, 65);
            int bitLimit = random.Next(1, byteLength * 8 + 1);
            byte[] buffer = new byte[byteLength];
            random.NextBytes(buffer);
            BitWriter writer = new BitWriter(byteLength, bitLimit);
            List<int> widths = new List<int>();
            List<ulong> values = new List<ulong>();
            int remaining = bitLimit;
            while (remaining > 0)
            {
                int width = random.Next(1, Math.Min(64, remaining) + 1);
                ulong value = MaskToWidth(NextUInt64(random), width);
                AvifResultCode code = writer.WriteBits(buffer, value, width);
                Assert.That(
                    code,
                    Is.EqualTo(AvifResultCode.Success),
                    $"seed={BitFieldSeed} scenario={scenario} width={width}");
                widths.Add(width);
                values.Add(value);
                remaining -= width;
            }

            BitReader reader = new BitReader(buffer.Length, bitLimit);
            int field = 0;
            while (field < widths.Count)
            {
                long position = reader.BitPosition;
                ulong expected = ReadReferenceBits(
                    buffer,
                    position,
                    widths[field]);
                ulong actual = reader.ReadBits(buffer, widths[field]);
                Assert.Multiple(() =>
                {
                    Assert.That(
                        actual,
                        Is.EqualTo(values[field]),
                        $"seed={BitFieldSeed} scenario={scenario} field={field}");
                    Assert.That(actual, Is.EqualTo(expected));
                    Assert.That(reader.Code, Is.EqualTo(AvifResultCode.Success));
                });
                field++;
            }

            Assert.That(reader.BitPosition, Is.EqualTo(bitLimit));
            Assert.That(reader.RemainingBits(), Is.Zero);
            scenario++;
        }
    }

    [Test]
    public void RandomizedVariableBitCodesRoundTripForFullValueDomain()
    {
        Random random = new Random(VariableCodeSeed);
        int scenario = 0;
        while (scenario < 512)
        {
            uint uvlcValue = scenario == 0
                ? uint.MaxValue : NextUInt32(random);
            byte[] uvlcBuffer = new byte[8];
            BitWriter uvlcWriter = BitWriter.Create(uvlcBuffer.Length);
            AvifResultCode uvlcWrite = uvlcWriter.WriteUvlc(
                uvlcBuffer,
                uvlcValue);
            BitReader uvlcReader = new BitReader(
                uvlcBuffer.Length,
                uvlcWriter.BitPosition);
            uint decodedUvlc = uvlcReader.ReadUvlc(uvlcBuffer);

            uint valueCount = NextUInt32(random) % 1000000 + 1;
            uint nsValue = NextUInt32(random) % valueCount;
            byte[] nsBuffer = new byte[4];
            BitWriter nsWriter = BitWriter.Create(nsBuffer.Length);
            AvifResultCode nsWrite = nsWriter.WriteNs(
                nsBuffer,
                nsValue,
                valueCount);
            BitReader nsReader = new BitReader(
                nsBuffer.Length,
                nsWriter.BitPosition);
            uint decodedNs = nsReader.ReadNs(nsBuffer, valueCount);

            Assert.Multiple(() =>
            {
                Assert.That(
                    uvlcWrite,
                    Is.EqualTo(AvifResultCode.Success),
                    $"seed={VariableCodeSeed} scenario={scenario}");
                Assert.That(decodedUvlc, Is.EqualTo(uvlcValue));
                Assert.That(uvlcReader.Code, Is.EqualTo(AvifResultCode.Success));
                Assert.That(uvlcReader.RemainingBits(), Is.Zero);
                Assert.That(nsWrite, Is.EqualTo(AvifResultCode.Success));
                Assert.That(decodedNs, Is.EqualTo(nsValue));
                Assert.That(nsReader.Code, Is.EqualTo(AvifResultCode.Success));
                Assert.That(nsReader.RemainingBits(), Is.Zero);
            });
            scenario++;
        }
    }

    [Test]
    public void RandomizedIsoBoxesRoundTripHeadersPayloadsAndRanges()
    {
        Random random = new Random(BoxSeed);
        int scenario = 0;
        while (scenario < 96)
        {
            IsoBoxWriter writer = new IsoBoxWriter(0, 1048576);
            List<ExpectedBox> expected = new List<ExpectedBox>();
            int boxCount = random.Next(1, 17);
            int boxIndex = 0;
            while (boxIndex < boxCount)
            {
                string type = CreateOpaqueType(random);
                byte[] payload = new byte[random.Next(0, 65)];
                random.NextBytes(payload);
                byte[] userType = System.Array.Empty<byte>();
                int headerKind = random.Next(0, 4);
                AvifResultCode begin;
                if (headerKind == 0)
                {
                    begin = writer.BeginBox(type);
                }
                else if (headerKind == 1)
                {
                    begin = writer.BeginLargeBox(type);
                }
                else
                {
                    userType = new byte[16];
                    random.NextBytes(userType);
                    begin = headerKind == 2
                        ? writer.BeginUuidBox(userType)
                        : writer.BeginLargeUuidBox(userType);
                    type = "uuid";
                }
                Assert.That(begin, Is.EqualTo(AvifResultCode.Success));
                Assert.That(writer.Write(payload), Is.EqualTo(AvifResultCode.Success));
                Assert.That(writer.EndBox(), Is.EqualTo(AvifResultCode.Success));
                expected.Add(new ExpectedBox(type, payload, userType, headerKind));
                boxIndex++;
            }

            IsoBoxWriteResult written = writer.Finish();
            IsoBoxParseResult parsed = IsoBoxReader.Parse(
                written.Data,
                new AvifSecurityLimits(),
                true);
            Assert.That(
                parsed.Code,
                Is.EqualTo(AvifResultCode.Success),
                $"seed={BoxSeed} scenario={scenario}");
            Assert.That(parsed.Boxes.Count, Is.EqualTo(expected.Count));
            long expectedOffset = 0;
            boxIndex = 0;
            while (boxIndex < expected.Count)
            {
                IsoBox box = parsed.Boxes[boxIndex];
                ExpectedBox expectedBox = expected[boxIndex];
                Assert.Multiple(() =>
                {
                    Assert.That(box.Type, Is.EqualTo(expectedBox.Type));
                    Assert.That(box.Offset, Is.EqualTo(expectedOffset));
                    Assert.That(box.EndOffset, Is.EqualTo(box.Offset + box.Size));
                    Assert.That(box.PayloadOffset,
                        Is.EqualTo(box.Offset + box.HeaderSize));
                    Assert.That(box.PayloadSize, Is.EqualTo(expectedBox.Payload.Length));
                    Assert.That(box.UsesLargeSize,
                        Is.EqualTo(expectedBox.HeaderKind == 1
                            || expectedBox.HeaderKind == 3));
                    Assert.That(box.IsUuid,
                        Is.EqualTo(expectedBox.HeaderKind >= 2));
                    Assert.That(box.UserType, Is.EqualTo(expectedBox.UserType));
                    Assert.That(written.Data.AsSpan(
                        (int)box.PayloadOffset,
                        (int)box.PayloadSize).ToArray(),
                        Is.EqualTo(expectedBox.Payload));
                    Assert.That(box.RawData,
                        Is.EqualTo(written.Data.AsSpan(
                            (int)box.Offset,
                            (int)box.Size).ToArray()));
                });
                expectedOffset = box.EndOffset;
                boxIndex++;
            }
            Assert.That(expectedOffset, Is.EqualTo(written.Data.Length));
            scenario++;
        }
    }

    [Test]
    public void RandomizedLowOverheadObusRoundTripHeadersPayloadsAndRanges()
    {
        Random random = new Random(ObuSeed);
        Av1ObuType[] types =
        [
            Av1ObuType.SequenceHeader,
            Av1ObuType.FrameHeader,
            Av1ObuType.TileGroup,
            Av1ObuType.Metadata,
            Av1ObuType.Frame,
            Av1ObuType.RedundantFrameHeader,
            Av1ObuType.TileList,
            Av1ObuType.Padding
        ];
        int scenario = 0;
        while (scenario < 96)
        {
            List<byte> source = new List<byte>();
            List<ExpectedObu> expected = new List<ExpectedObu>();
            int obuCount = random.Next(1, 25);
            int obuIndex = 0;
            while (obuIndex < obuCount)
            {
                Av1ObuType type = types[random.Next(types.Length)];
                byte[] payload = new byte[random.Next(0, 129)];
                random.NextBytes(payload);
                bool extensionAllowed = type != Av1ObuType.SequenceHeader
                    && type != Av1ObuType.TileList;
                bool hasExtension = extensionAllowed && random.Next(2) == 1;
                int temporalId = hasExtension ? random.Next(0, 8) : 0;
                int spatialId = hasExtension ? random.Next(0, 4) : 0;
                Av1WriteResult written = Av1ObuWriter.WriteObu(
                    type,
                    payload,
                    hasExtension,
                    temporalId,
                    spatialId);
                Assert.That(written.Code, Is.EqualTo(AvifResultCode.Success));
                source.AddRange(written.Data);
                expected.Add(new ExpectedObu(
                    type,
                    payload,
                    hasExtension,
                    temporalId,
                    spatialId,
                    written.Data.Length));
                obuIndex++;
            }

            byte[] encoded = source.ToArray();
            Av1ObuParseResult parsed = Av1ObuParser.ParseLowOverhead(
                encoded,
                new AvifSecurityLimits(),
                0);
            Assert.That(
                parsed.Code,
                Is.EqualTo(AvifResultCode.Success),
                $"seed={ObuSeed} scenario={scenario}");
            Assert.That(parsed.Obus.Count, Is.EqualTo(expected.Count));
            int expectedOffset = 0;
            obuIndex = 0;
            while (obuIndex < expected.Count)
            {
                Av1Obu obu = parsed.Obus[obuIndex];
                ExpectedObu expectedObu = expected[obuIndex];
                Assert.Multiple(() =>
                {
                    Assert.That(obu.Header.Type, Is.EqualTo(expectedObu.Type));
                    Assert.That(obu.Header.HasExtension,
                        Is.EqualTo(expectedObu.HasExtension));
                    Assert.That(obu.Header.TemporalId,
                        Is.EqualTo(expectedObu.TemporalId));
                    Assert.That(obu.Header.SpatialId,
                        Is.EqualTo(expectedObu.SpatialId));
                    Assert.That(obu.Offset, Is.EqualTo(expectedOffset));
                    Assert.That(obu.TotalSize, Is.EqualTo(expectedObu.TotalSize));
                    Assert.That(obu.GetPayload(), Is.EqualTo(expectedObu.Payload));
                    Assert.That(obu.PayloadOffset + obu.PayloadSize,
                        Is.EqualTo(obu.Offset + obu.TotalSize));
                });
                expectedOffset += obu.TotalSize;
                obuIndex++;
            }
            Assert.That(expectedOffset, Is.EqualTo(encoded.Length));
            scenario++;
        }
    }

    [Test]
    public void SeededMalformedInputsFailDeterministicallyAndStayBounded()
    {
        Random random = new Random(MalformedSeed);
        int scenario = 0;
        while (scenario < 256)
        {
            byte[] boxSource = CreateMalformedBox(random, scenario % 5);
            IsoBoxParseResult firstBox = IsoBoxReader.Parse(
                boxSource,
                new AvifSecurityLimits(),
                false);
            IsoBoxParseResult secondBox = IsoBoxReader.Parse(
                boxSource,
                new AvifSecurityLimits(),
                false);

            byte[] obuSource = CreateMalformedObu(random, scenario % 6);
            Av1ObuParseResult firstObu = Av1ObuParser.ParseLowOverhead(
                obuSource,
                new AvifSecurityLimits(),
                0);
            Av1ObuParseResult secondObu = Av1ObuParser.ParseLowOverhead(
                obuSource,
                new AvifSecurityLimits(),
                0);

            byte[] annexSource = new byte[random.Next(1, 65)];
            random.NextBytes(annexSource);
            annexSource[0] = (byte)(annexSource.Length + random.Next(1, 63));
            Av1ObuParseResult firstAnnex = Av1ObuParser.ParseLengthDelimited(
                annexSource,
                new AvifSecurityLimits(),
                0);
            Av1ObuParseResult secondAnnex = Av1ObuParser.ParseLengthDelimited(
                annexSource,
                new AvifSecurityLimits(),
                0);

            Assert.Multiple(() =>
            {
                Assert.That(
                    firstBox.Code,
                    Is.Not.EqualTo(AvifResultCode.Success),
                    $"box seed={MalformedSeed} scenario={scenario}");
                Assert.That(secondBox.Code, Is.EqualTo(firstBox.Code));
                Assert.That(firstBox.Diagnostics.Diagnostics, Is.Not.Empty);
                Assert.That(firstBox.Resources.BoxDepth.Current, Is.Zero);
                Assert.That(firstObu.Code, Is.Not.EqualTo(AvifResultCode.Success),
                    $"obu seed={MalformedSeed} scenario={scenario}");
                Assert.That(secondObu.Code, Is.EqualTo(firstObu.Code));
                Assert.That(firstObu.Diagnostics.Diagnostics, Is.Not.Empty);
                Assert.That(firstObu.Obus, Is.Empty);
                Assert.That(firstAnnex.Code,
                    Is.EqualTo(AvifResultCode.TruncatedData),
                    $"annex seed={MalformedSeed} scenario={scenario}");
                Assert.That(secondAnnex.Code, Is.EqualTo(firstAnnex.Code));
                Assert.That(firstAnnex.Diagnostics.Diagnostics, Is.Not.Empty);
                Assert.That(firstAnnex.Obus, Is.Empty);
            });
            scenario++;
        }
    }

    [Test]
    public void SeededTruncatedBitReadsAreAtomicAndSticky()
    {
        Random random = new Random(MalformedSeed);
        int scenario = 0;
        while (scenario < 256)
        {
            byte[] source = new byte[random.Next(1, 9)];
            random.NextBytes(source);
            int bitLimit = random.Next(1, source.Length * 8 + 1);
            int prefixLength = random.Next(0, bitLimit);
            BitReader reader = new BitReader(source.Length, bitLimit);
            reader.ReadBits(source, prefixLength);
            long position = reader.BitPosition;
            int overrunWidth = (int)reader.RemainingBits() + 1;

            ulong value = reader.ReadBits(source, overrunWidth);
            int later = reader.ReadBit(source);

            Assert.Multiple(() =>
            {
                Assert.That(value, Is.Zero,
                    $"seed={MalformedSeed} scenario={scenario}");
                Assert.That(later, Is.Zero);
                Assert.That(reader.Code, Is.EqualTo(AvifResultCode.TruncatedData));
                Assert.That(reader.BitPosition, Is.EqualTo(position));
                Assert.That(reader.BitPosition, Is.InRange(0, bitLimit));
            });
            scenario++;
        }
    }

    static ulong NextUInt64(Random random)
    {
        byte[] bytes = new byte[8];
        random.NextBytes(bytes);
        return BinaryPrimitives.ReadUInt64LittleEndian(bytes);
    }

    static uint NextUInt32(Random random)
    {
        byte[] bytes = new byte[4];
        random.NextBytes(bytes);
        return BinaryPrimitives.ReadUInt32LittleEndian(bytes);
    }

    static ulong MaskToWidth(ulong value, int width)
    {
        return width == 64 ? value : value & ((1UL << width) - 1);
    }

    static ulong ReadReferenceBits(byte[] source, long position, int width)
    {
        ulong value = 0;
        int index = 0;
        while (index < width)
        {
            long bitPosition = position + index;
            int byteIndex = (int)(bitPosition / 8);
            int shift = 7 - (int)(bitPosition % 8);
            value = value << 1 | (uint)(source[byteIndex] >> shift & 1);
            index++;
        }
        return value;
    }

    static string CreateOpaqueType(Random random)
    {
        char[] type = new char[4];
        type[0] = 'z';
        int index = 1;
        while (index < type.Length)
        {
            type[index] = (char)('a' + random.Next(0, 26));
            index++;
        }
        return new string(type);
    }

    static byte[] CreateMalformedBox(Random random, int kind)
    {
        if (kind == 4)
        {
            byte[] nested = new byte[16];
            BinaryPrimitives.WriteUInt32BigEndian(nested.AsSpan(0, 4), 16);
            WriteType(nested, 4, "moov");
            BinaryPrimitives.WriteUInt32BigEndian(nested.AsSpan(8, 4), 16);
            WriteType(nested, 12, "free");
            return nested;
        }

        int length = random.Next(8, 65);
        byte[] source = new byte[length];
        random.NextBytes(source);
        WriteType(source, 4, "free");
        if (kind == 0)
        {
            BinaryPrimitives.WriteUInt32BigEndian(
                source.AsSpan(0, 4),
                (uint)random.Next(2, 8));
        }
        else if (kind == 1)
        {
            BinaryPrimitives.WriteUInt32BigEndian(
                source.AsSpan(0, 4),
                (uint)(length + random.Next(1, 1024)));
        }
        else if (kind == 2)
        {
            BinaryPrimitives.WriteUInt32BigEndian(source.AsSpan(0, 4), 1);
            if (source.Length >= 16)
            {
                BinaryPrimitives.WriteUInt64BigEndian(
                    source.AsSpan(8, 8),
                    ulong.MaxValue);
            }
        }
        else
        {
            BinaryPrimitives.WriteUInt32BigEndian(source.AsSpan(0, 4), 8);
            System.Array.Resize(ref source, 8 + random.Next(1, 8));
        }
        return source;
    }

    static byte[] CreateMalformedObu(Random random, int kind)
    {
        if (kind == 0)
        {
            return new byte[] { 138, 0 };
        }
        if (kind == 1)
        {
            return new byte[] { 11, 0 };
        }
        if (kind == 2)
        {
            return new byte[] { 8 };
        }
        if (kind == 3)
        {
            byte[] unterminated = new byte[9];
            unterminated[0] = 10;
            unterminated.AsSpan(1).Fill(128);
            return unterminated;
        }
        if (kind == 4)
        {
            int actualLength = random.Next(0, 16);
            byte[] truncated = new byte[2 + actualLength];
            truncated[0] = 10;
            truncated[1] = (byte)(actualLength + random.Next(1, 64));
            random.NextBytes(truncated.AsSpan(2));
            return truncated;
        }
        return new byte[] { 30, 7, 0 };
    }

    static void WriteType(byte[] destination, int offset, string type)
    {
        int index = 0;
        while (index < 4)
        {
            destination[offset + index] = (byte)type[index];
            index++;
        }
    }

    sealed class ExpectedBox
    {
        internal string Type;
        internal byte[] Payload;
        internal byte[] UserType;
        internal int HeaderKind;

        internal ExpectedBox(
            string type,
            byte[] payload,
            byte[] userType,
            int headerKind)
        {
            Type = type;
            Payload = payload;
            UserType = userType;
            HeaderKind = headerKind;
        }
    }

    sealed class ExpectedObu
    {
        internal Av1ObuType Type;
        internal byte[] Payload;
        internal bool HasExtension;
        internal int TemporalId;
        internal int SpatialId;
        internal int TotalSize;

        internal ExpectedObu(
            Av1ObuType type,
            byte[] payload,
            bool hasExtension,
            int temporalId,
            int spatialId,
            int totalSize)
        {
            Type = type;
            Payload = payload;
            HasExtension = hasExtension;
            TemporalId = temporalId;
            SpatialId = spatialId;
            TotalSize = totalSize;
        }
    }
}
