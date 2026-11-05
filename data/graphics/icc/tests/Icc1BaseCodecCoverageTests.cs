using icc;

namespace icc.Tests;

public class Icc1BaseCodecCoverageTests
{
    [Test]
    public void ScalarNumberBoundariesHaveIndependentGoldenBytes()
    {
        byte[] bytes = new byte[46];
        BigEndianWriter writer = new BigEndianWriter(bytes, 0);
        IccNumberCodec.WriteS15Fixed16(writer,
            IccNumberCodec.S15Fixed16Minimum);
        IccNumberCodec.WriteS15Fixed16(writer, -1.0 / 65536.0);
        IccNumberCodec.WriteS15Fixed16(writer, 0.0);
        IccNumberCodec.WriteS15Fixed16(writer, 1.0 / 65536.0);
        IccNumberCodec.WriteS15Fixed16(writer,
            IccNumberCodec.S15Fixed16Maximum);
        IccNumberCodec.WriteU16Fixed16(writer, 0.0);
        IccNumberCodec.WriteU16Fixed16(writer, 1.0 / 65536.0);
        IccNumberCodec.WriteU16Fixed16(writer,
            IccNumberCodec.U16Fixed16Maximum);
        IccNumberCodec.WriteU1Fixed15(writer, 0.0);
        IccNumberCodec.WriteU1Fixed15(writer, 1.0 / 32768.0);
        IccNumberCodec.WriteU1Fixed15(writer, 1.0);
        IccNumberCodec.WriteU1Fixed15(writer,
            IccNumberCodec.U1Fixed15Maximum);
        IccNumberCodec.WriteU8Fixed8(writer, 0.0);
        IccNumberCodec.WriteU8Fixed8(writer, 1.0 / 256.0);
        IccNumberCodec.WriteU8Fixed8(writer,
            IccNumberCodec.U8Fixed8Maximum);

        byte[] golden = new byte[]
        {
            0x80, 0x00, 0x00, 0x00,
            0xff, 0xff, 0xff, 0xff,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x01,
            0x7f, 0xff, 0xff, 0xff,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x01,
            0xff, 0xff, 0xff, 0xff,
            0x00, 0x00,
            0x00, 0x01,
            0x80, 0x00,
            0xff, 0xff,
            0x00, 0x00,
            0x00, 0x01,
            0xff, 0xff
        };
        BigEndianReader reader = new BigEndianReader(bytes, 0);

        Assert.Multiple(() =>
        {
            Assert.That(bytes, Is.EqualTo(golden));
            Assert.That(IccNumberCodec.ReadS15Fixed16(reader),
                Is.EqualTo(IccNumberCodec.S15Fixed16Minimum));
            Assert.That(IccNumberCodec.ReadS15Fixed16(reader),
                Is.EqualTo(-1.0 / 65536.0));
            Assert.That(IccNumberCodec.ReadS15Fixed16(reader), Is.Zero);
            Assert.That(IccNumberCodec.ReadS15Fixed16(reader),
                Is.EqualTo(1.0 / 65536.0));
            Assert.That(IccNumberCodec.ReadS15Fixed16(reader),
                Is.EqualTo(IccNumberCodec.S15Fixed16Maximum));
            Assert.That(IccNumberCodec.ReadU16Fixed16(reader), Is.Zero);
            Assert.That(IccNumberCodec.ReadU16Fixed16(reader),
                Is.EqualTo(1.0 / 65536.0));
            Assert.That(IccNumberCodec.ReadU16Fixed16(reader),
                Is.EqualTo(IccNumberCodec.U16Fixed16Maximum));
            Assert.That(IccNumberCodec.ReadU1Fixed15(reader), Is.Zero);
            Assert.That(IccNumberCodec.ReadU1Fixed15(reader),
                Is.EqualTo(1.0 / 32768.0));
            Assert.That(IccNumberCodec.ReadU1Fixed15(reader), Is.EqualTo(1.0));
            Assert.That(IccNumberCodec.ReadU1Fixed15(reader),
                Is.EqualTo(IccNumberCodec.U1Fixed15Maximum));
            Assert.That(IccNumberCodec.ReadU8Fixed8(reader), Is.Zero);
            Assert.That(IccNumberCodec.ReadU8Fixed8(reader),
                Is.EqualTo(1.0 / 256.0));
            Assert.That(IccNumberCodec.ReadU8Fixed8(reader),
                Is.EqualTo(IccNumberCodec.U8Fixed8Maximum));
            Assert.That(reader.GetRemainingCount(), Is.Zero);
        });
    }

    [Test]
    public void CompositeNumberBoundariesHaveIndependentGoldenBytes()
    {
        byte[] bytes = new byte[40];
        BigEndianWriter writer = new BigEndianWriter(bytes, 0);
        IccXyzNumber xyz = new IccXyzNumber();
        xyz.X = IccNumberCodec.S15Fixed16Minimum;
        xyz.Y = 0.0;
        xyz.Z = IccNumberCodec.S15Fixed16Maximum;
        IccDateTime date = new IccDateTime();
        date.Year = 0;
        date.Month = ushort.MaxValue;
        date.Day = 0;
        date.Hour = ushort.MaxValue;
        date.Minute = 0;
        date.Second = ushort.MaxValue;
        IccPositionNumber position = new IccPositionNumber(uint.MaxValue, 0);
        IccResponse16Number response = new IccResponse16Number(
            ushort.MaxValue, ushort.MaxValue,
            IccNumberCodec.S15Fixed16Maximum);

        IccNumberCodec.WriteXyzNumber(writer, xyz);
        IccNumberCodec.WriteDateTimeNumber(writer, date);
        IccNumberCodec.WritePositionNumber(writer, position);
        IccNumberCodec.WriteResponse16Number(writer, response);

        byte[] golden = new byte[]
        {
            0x80, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x7f, 0xff, 0xff, 0xff,
            0x00, 0x00, 0xff, 0xff, 0x00, 0x00,
            0xff, 0xff, 0x00, 0x00, 0xff, 0xff,
            0xff, 0xff, 0xff, 0xff,
            0x00, 0x00, 0x00, 0x00,
            0xff, 0xff, 0xff, 0xff,
            0x7f, 0xff, 0xff, 0xff
        };
        BigEndianReader reader = new BigEndianReader(bytes, 0);
        IccXyzNumber decodedXyz = IccNumberCodec.ReadXyzNumber(reader);
        IccDateTime decodedDate = IccNumberCodec.ReadDateTimeNumber(reader);
        IccPositionNumber decodedPosition =
            IccNumberCodec.ReadPositionNumber(reader);
        IccResponse16Number decodedResponse =
            IccNumberCodec.ReadResponse16Number(reader);

        Assert.Multiple(() =>
        {
            Assert.That(bytes, Is.EqualTo(golden));
            Assert.That(decodedXyz.X,
                Is.EqualTo(IccNumberCodec.S15Fixed16Minimum));
            Assert.That(decodedXyz.Y, Is.Zero);
            Assert.That(decodedXyz.Z,
                Is.EqualTo(IccNumberCodec.S15Fixed16Maximum));
            Assert.That(decodedDate.Year, Is.Zero);
            Assert.That(decodedDate.Month, Is.EqualTo(ushort.MaxValue));
            Assert.That(decodedDate.Hour, Is.EqualTo(ushort.MaxValue));
            Assert.That(decodedDate.Second, Is.EqualTo(ushort.MaxValue));
            Assert.That(decodedPosition.Offset, Is.EqualTo(uint.MaxValue));
            Assert.That(decodedPosition.Size, Is.Zero);
            Assert.That(decodedResponse.DeviceCode,
                Is.EqualTo(ushort.MaxValue));
            Assert.That(decodedResponse.Reserved,
                Is.EqualTo(ushort.MaxValue));
            Assert.That(decodedResponse.Measurement,
                Is.EqualTo(IccNumberCodec.S15Fixed16Maximum));
            Assert.That(reader.GetRemainingCount(), Is.Zero);
        });
    }

    [Test]
    public void ScalarNumberReadersRejectTruncationWithoutAdvancing()
    {
        BigEndianReader signedReader = new BigEndianReader(new byte[3], 10);
        BigEndianReader unsignedReader = new BigEndianReader(new byte[3], 20);
        BigEndianReader u1Reader = new BigEndianReader(new byte[1], 30);
        BigEndianReader u8Reader = new BigEndianReader(new byte[1], 40);

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileFormatException>(() =>
                IccNumberCodec.ReadS15Fixed16(signedReader));
            Assert.Throws<IccProfileFormatException>(() =>
                IccNumberCodec.ReadU16Fixed16(unsignedReader));
            Assert.Throws<IccProfileFormatException>(() =>
                IccNumberCodec.ReadU1Fixed15(u1Reader));
            Assert.Throws<IccProfileFormatException>(() =>
                IccNumberCodec.ReadU8Fixed8(u8Reader));
            Assert.That(signedReader.GetReadPosition(), Is.Zero);
            Assert.That(unsignedReader.GetReadPosition(), Is.Zero);
            Assert.That(u1Reader.GetReadPosition(), Is.Zero);
            Assert.That(u8Reader.GetReadPosition(), Is.Zero);
        });
    }

    [Test]
    public void IccOneNumbersRejectNullReadersWritersAndCompositeValues()
    {
        BigEndianWriter writer = new BigEndianWriter(new byte[12], 0);

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileFormatException>(() =>
                IccNumberCodec.ReadFloat32(null!));
            Assert.Throws<IccProfileFormatException>(() =>
                IccNumberCodec.ReadS15Fixed16(null!));
            Assert.Throws<IccProfileFormatException>(() =>
                IccNumberCodec.ReadU16Fixed16(null!));
            Assert.Throws<IccProfileFormatException>(() =>
                IccNumberCodec.ReadU1Fixed15(null!));
            Assert.Throws<IccProfileFormatException>(() =>
                IccNumberCodec.ReadU8Fixed8(null!));
            Assert.Throws<IccProfileFormatException>(() =>
                IccNumberCodec.ReadXyzNumber(null!));
            Assert.Throws<IccProfileFormatException>(() =>
                IccNumberCodec.ReadDateTimeNumber(null!));
            Assert.Throws<IccProfileFormatException>(() =>
                IccNumberCodec.ReadPositionNumber(null!));
            Assert.Throws<IccProfileFormatException>(() =>
                IccNumberCodec.ReadResponse16Number(null!));
            Assert.Throws<IccProfileWriteException>(() =>
                IccNumberCodec.WriteFloat32(null!, 0));
            Assert.Throws<IccProfileWriteException>(() =>
                IccNumberCodec.WriteS15Fixed16(null!, 0));
            Assert.Throws<IccProfileWriteException>(() =>
                IccNumberCodec.WriteU16Fixed16(null!, 0));
            Assert.Throws<IccProfileWriteException>(() =>
                IccNumberCodec.WriteU1Fixed15(null!, 0));
            Assert.Throws<IccProfileWriteException>(() =>
                IccNumberCodec.WriteU8Fixed8(null!, 0));
            Assert.Throws<IccProfileWriteException>(() =>
                IccNumberCodec.WriteXyzNumber(null!, new IccXyzNumber()));
            Assert.Throws<IccProfileWriteException>(() =>
                IccNumberCodec.WriteDateTimeNumber(null!, new IccDateTime()));
            Assert.Throws<IccProfileWriteException>(() =>
                IccNumberCodec.WritePositionNumber(null!,
                    new IccPositionNumber(0, 0)));
            Assert.Throws<IccProfileWriteException>(() =>
                IccNumberCodec.WriteResponse16Number(null!,
                    new IccResponse16Number(0, 0, 0)));
            Assert.Throws<IccProfileWriteException>(() =>
                IccNumberCodec.WriteXyzNumber(writer, null!));
            Assert.Throws<IccProfileWriteException>(() =>
                IccNumberCodec.WriteDateTimeNumber(writer, null!));
            Assert.Throws<IccProfileWriteException>(() =>
                IccNumberCodec.WritePositionNumber(writer, null!));
            Assert.Throws<IccProfileWriteException>(() =>
                IccNumberCodec.WriteResponse16Number(writer, null!));
            Assert.That(writer.GetWritePosition(), Is.Zero);
        });
    }

    [Test]
    public void EveryIccOneTypeHasTheExpectedGenerationSupport()
    {
        (uint Signature, bool Version2, bool Version4)[] cases =
            IccOneTypeSupportCases();

        foreach ((uint signature, bool version2, bool version4) in cases)
        {
            Assert.That(TagCodecDispatcher.IsTypeSupportedByGeneration(
                signature, IccVersionKind.Version2), Is.EqualTo(version2),
                $"v2 support for 0x{signature:X8}");
            Assert.That(TagCodecDispatcher.IsTypeSupportedByGeneration(
                signature, IccVersionKind.Version4), Is.EqualTo(version4),
                $"v4 support for 0x{signature:X8}");
        }

        Assert.That(cases, Has.Length.EqualTo(38));
    }

    [Test]
    public void DataTextSerializationUsesTheProfileGenerationEncoding()
    {
        IccTag asciiTag = TextDataTag("ASCII");
        IccTag unicodeTag = TextDataTag("é");
        IccWriteOptions options = new IccWriteOptions();
        IccProfileHeader version2 = Header(IccVersionKind.Version2);
        IccProfileHeader version4 = Header(IccVersionKind.Version4);
        IccProfileHeader version5 = Header(IccVersionKind.Version5);

        byte[] version2Bytes = ProfileTagSerializer.SerializeTypedTag(
            asciiTag, version2, options);
        byte[] version4Bytes = ProfileTagSerializer.SerializeTypedTag(
            asciiTag, version4, options);
        byte[] version5Bytes = ProfileTagSerializer.SerializeTypedTag(
            unicodeTag, version5, options);
        TagDecodeState version2State = Decode(version2Bytes,
            IccVersionKind.Version2);
        TagDecodeState version4State = Decode(version4Bytes,
            IccVersionKind.Version4);
        TagDecodeState version5State = Decode(version5Bytes,
            IccVersionKind.Version5);

        Assert.Multiple(() =>
        {
            Assert.That(version2Bytes, Is.EqualTo(new byte[]
            {
                0x64, 0x61, 0x74, 0x61, 0, 0, 0, 0,
                0, 0, 0, 0, 0x41, 0x53, 0x43, 0x49, 0x49, 0
            }));
            Assert.That(version4Bytes, Is.EqualTo(version2Bytes));
            Assert.That(version5Bytes, Is.EqualTo(new byte[]
            {
                0x64, 0x61, 0x74, 0x61, 0, 0, 0, 0,
                0, 0, 0, 0, 0xc3, 0xa9, 0
            }));
            Assert.That(version2State.Result.Status,
                Is.EqualTo(IccOperationStatus.Success));
            Assert.That(version4State.Result.Status,
                Is.EqualTo(IccOperationStatus.Success));
            Assert.That(version5State.Result.Status,
                Is.EqualTo(IccOperationStatus.Success));
            Assert.That(version2State.Data.Text!.Text, Is.EqualTo("ASCII"));
            Assert.That(version4State.Data.Text!.Text, Is.EqualTo("ASCII"));
            Assert.That(version5State.Data.Text!.Text, Is.EqualTo("é"));
            Assert.Throws<IccProfileWriteException>(() =>
                ProfileTagSerializer.SerializeTypedTag(unicodeTag, version2,
                    options));
            Assert.Throws<IccProfileWriteException>(() =>
                ProfileTagSerializer.SerializeTypedTag(unicodeTag, version4,
                    options));
        });
    }

    [TestCaseSource(nameof(TruncatedPayloadCases))]
    public void FixedAndCountedBaseTagPayloadsRejectTruncation(
        string name, byte[] golden, IccVersionKind version)
    {
        byte[] truncated = golden.Take(golden.Length - 1).ToArray();

        TagDecodeState state = Decode(truncated, version);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status,
                Is.EqualTo(IccOperationStatus.InvalidData), name);
            Assert.That(state.Data.RawData.Bytes, Is.EqualTo(truncated), name);
        });
    }

    [TestCaseSource(nameof(GoldenPayloadCases))]
    public void BaseTagCodecsDecodeAndReencodeIndependentGoldenBytes(
        string name, byte[] golden, IccVersionKind version,
        IccTagDataKind expectedKind)
    {
        TagDecodeState state = Decode(golden, version);

        Assert.That(state.Result.Status, Is.EqualTo(
            IccOperationStatus.Success), name);
        Assert.That(state.Data.Kind, Is.EqualTo(expectedKind), name);
        Assert.That(SerializeDecoded(state), Is.EqualTo(golden), name);
    }

    [TestCaseSource(nameof(GoldenPayloadCases))]
    public void EveryBaseTagCodecRejectsEachNonzeroReservedByte(
        string name, byte[] golden, IccVersionKind version,
        IccTagDataKind expectedKind)
    {
        int reservedIndex = 4;
        while (reservedIndex <= 7)
        {
            byte[] malformed = golden.ToArray();
            malformed[reservedIndex] = (byte)(reservedIndex + 1);

            TagDecodeState state = Decode(malformed, version);

            Assert.That(state.Result.Status,
                Is.EqualTo(IccOperationStatus.InvalidData),
                $"{name}, reserved byte {reservedIndex}");
            Assert.That(state.Result.Message, Does.Contain("reserved"),
                $"{name}, reserved byte {reservedIndex}");
            reservedIndex++;
        }
    }

    [TestCaseSource(nameof(MalformedCountAndOffsetCases))]
    public void UnsignedCountsAndOffsetsAreRejectedBeforeAllocation(
        string name, byte[] malformed, IccVersionKind version,
        string messageFragment)
    {
        TagDecodeState state = Decode(malformed, version);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status,
                Is.EqualTo(IccOperationStatus.InvalidData), name);
            Assert.That(state.Result.Message, Does.Contain(messageFragment),
                name);
            Assert.That(state.Data.RawData.Bytes, Is.EqualTo(malformed), name);
        });
    }

    private static IEnumerable<TestCaseData> GoldenPayloadCases()
    {
        foreach ((string name, byte[] payload, IccVersionKind version,
            IccTagDataKind kind) in CreateGoldenPayloadCases())
        {
            yield return new TestCaseData(name, payload, version, kind)
                .SetName($"{{m}}({name})");
        }
    }

    private static IEnumerable<TestCaseData> TruncatedPayloadCases()
    {
        foreach ((string name, byte[] payload, IccVersionKind version,
            IccTagDataKind kind) in CreateGoldenPayloadCases())
        {
            if (name != "uint8" && name != "binary data")
            {
                yield return new TestCaseData(name, payload, version)
                    .SetName($"{{m}}({name})");
            }
        }
    }

    private static IEnumerable<TestCaseData> MalformedCountAndOffsetCases()
    {
        byte[] curve = Payload(IccTypeSignatures.Curve, 12);
        IndependentBigEndian.WriteUInt32(curve, 8, uint.MaxValue);
        yield return MalformedCase("curve count", curve,
            IccVersionKind.Version4, "4294967295");

        byte[] mlucCount = Payload(
            IccTypeSignatures.MultiLocalizedUnicode, 16);
        IndependentBigEndian.WriteUInt32(mlucCount, 8, uint.MaxValue);
        IndependentBigEndian.WriteUInt32(mlucCount, 12, 12);
        yield return MalformedCase("mluc count", mlucCount,
            IccVersionKind.Version4, "4294967295");

        byte[] mlucOffset = Payload(
            IccTypeSignatures.MultiLocalizedUnicode, 30);
        IndependentBigEndian.WriteUInt32(mlucOffset, 8, 1);
        IndependentBigEndian.WriteUInt32(mlucOffset, 12, 12);
        IndependentBigEndian.WriteUInt16(mlucOffset, 16, 0x656e);
        IndependentBigEndian.WriteUInt16(mlucOffset, 18, 0x5553);
        IndependentBigEndian.WriteUInt32(mlucOffset, 20, 2);
        IndependentBigEndian.WriteUInt32(mlucOffset, 24, uint.MaxValue);
        yield return MalformedCase("mluc offset", mlucOffset,
            IccVersionKind.Version4, "managed offset");

        byte[] dictionary = Payload(IccTypeSignatures.Dictionary, 16);
        IndependentBigEndian.WriteUInt32(dictionary, 8, uint.MaxValue);
        IndependentBigEndian.WriteUInt32(dictionary, 12, 16);
        yield return MalformedCase("dictionary count", dictionary,
            IccVersionKind.Version4, "4294967295");

        byte[] sequence = Payload(
            IccTypeSignatures.ProfileSequenceDescription, 12);
        IndependentBigEndian.WriteUInt32(sequence, 8, uint.MaxValue);
        yield return MalformedCase("profile sequence count", sequence,
            IccVersionKind.Version4, "4294967295");

        byte[] identifier = Payload(
            IccTypeSignatures.ProfileSequenceIdentifier, 20);
        IndependentBigEndian.WriteUInt32(identifier, 8, 1);
        IndependentBigEndian.WriteUInt32(identifier, 12, uint.MaxValue);
        IndependentBigEndian.WriteUInt32(identifier, 16, 32);
        yield return MalformedCase("profile identifier offset", identifier,
            IccVersionKind.Version4, "managed offset");

        byte[] response = Payload(
            IccTypeSignatures.ResponseCurveSet16, 16);
        IndependentBigEndian.WriteUInt16(response, 8, 1);
        IndependentBigEndian.WriteUInt16(response, 10, 1);
        IndependentBigEndian.WriteUInt32(response, 12, uint.MaxValue);
        yield return MalformedCase("response curve offset", response,
            IccVersionKind.Version4, "managed range");
    }

    private static TestCaseData MalformedCase(string name, byte[] payload,
        IccVersionKind version, string messageFragment)
    {
        return new TestCaseData(name, payload, version, messageFragment)
            .SetName($"{{m}}({name})");
    }

    private static (string Name, byte[] Payload, IccVersionKind Version,
        IccTagDataKind Kind)[] CreateGoldenPayloadCases()
    {
        return new[]
        {
            Case("uint8", IntegerPayload(IccTypeSignatures.UInt8Array,
                1, 0, ulong.MaxValue), IccTagDataKind.IntegerArray),
            Case("uint16", IntegerPayload(IccTypeSignatures.UInt16Array,
                2, 0, ushort.MaxValue), IccTagDataKind.IntegerArray),
            Case("uint32", IntegerPayload(IccTypeSignatures.UInt32Array,
                4, 0, uint.MaxValue), IccTagDataKind.IntegerArray),
            Case("uint64", IntegerPayload(IccTypeSignatures.UInt64Array,
                8, 0, ulong.MaxValue), IccTagDataKind.IntegerArray),
            Case("s15Fixed16", FixedPayload(
                IccTypeSignatures.S15Fixed16Array, 0x80000000,
                0x7fffffff), IccTagDataKind.FixedPointArray),
            Case("u16Fixed16", FixedPayload(
                IccTypeSignatures.U16Fixed16Array, 0, uint.MaxValue),
                IccTagDataKind.FixedPointArray),
            Case("XYZ", XyzPayload(), IccTagDataKind.XyzArray),
            Case("signature", SignaturePayload(), IccTagDataKind.Signature),
            Case("dateTime", DateTimePayload(), IccTagDataKind.DateTime),
            Case("binary data", DataPayload(true), IccTagDataKind.Binary),
            Case("ASCII data", DataPayload(false), IccTagDataKind.Text),
            Case("text", TextPayload(), IccTagDataKind.Text),
            Case("mluc", EmptyCountPayload(
                IccTypeSignatures.MultiLocalizedUnicode, 16, 12),
                IccTagDataKind.LocalizedText),
            Case("dictionary", EmptyCountPayload(
                IccTypeSignatures.Dictionary, 16, 16),
                IccTagDataKind.Dictionary),
            Case("curve", CurvePayload(), IccTagDataKind.Curve),
            Case("parametric curve", ParametricCurvePayload(),
                IccTagDataKind.Curve),
            Case("chromaticity", ChromaticityPayload(),
                IccTagDataKind.Chromaticity),
            Case("measurement", MeasurementPayload(),
                IccTagDataKind.Measurement),
            Case("viewing conditions", ViewingConditionsPayload(),
                IccTagDataKind.ViewingConditions),
            Case("colorant order", ColorantOrderPayload(),
                IccTagDataKind.IntegerArray),
            Case("colorant table", ColorantTablePayload(),
                IccTagDataKind.Colorants),
            Case("profile sequence", EmptyCountPayload(
                IccTypeSignatures.ProfileSequenceDescription, 12, 0),
                IccTagDataKind.ProfileSequence),
            Case("profile identifiers", EmptyCountPayload(
                IccTypeSignatures.ProfileSequenceIdentifier, 12, 0),
                IccTagDataKind.ProfileSequence),
            Case("response curves", ResponseCurvePayload(),
                IccTagDataKind.ResponseCurves)
        };
    }

    private static (string, byte[], IccVersionKind, IccTagDataKind) Case(
        string name, byte[] payload, IccTagDataKind kind)
    {
        return (name, payload, IccVersionKind.Version4, kind);
    }

    private static byte[] IntegerPayload(uint signature, int width,
        ulong first, ulong second)
    {
        byte[] payload = Payload(signature, 8 + (width * 2));
        WriteWidth(payload, 8, width, first);
        WriteWidth(payload, 8 + width, width, second);
        return payload;
    }

    private static byte[] FixedPayload(uint signature, uint first,
        uint second)
    {
        byte[] payload = Payload(signature, 16);
        IndependentBigEndian.WriteUInt32(payload, 8, first);
        IndependentBigEndian.WriteUInt32(payload, 12, second);
        return payload;
    }

    private static byte[] XyzPayload()
    {
        byte[] payload = Payload(IccTypeSignatures.Xyz, 20);
        IndependentBigEndian.WriteUInt32(payload, 8, 0x80000000);
        IndependentBigEndian.WriteUInt32(payload, 12, 0);
        IndependentBigEndian.WriteUInt32(payload, 16, 0x7fffffff);
        return payload;
    }

    private static byte[] SignaturePayload()
    {
        byte[] payload = Payload(IccTypeSignatures.Signature, 12);
        IndependentBigEndian.WriteUInt32(payload, 8, uint.MaxValue);
        return payload;
    }

    private static byte[] DateTimePayload()
    {
        byte[] payload = Payload(IccTypeSignatures.DateTime, 20);
        IndependentBigEndian.WriteUInt16(payload, 8, 1);
        IndependentBigEndian.WriteUInt16(payload, 10, 1);
        IndependentBigEndian.WriteUInt16(payload, 12, 1);
        return payload;
    }

    private static byte[] DataPayload(bool binary)
    {
        byte[] payload = Payload(IccTypeSignatures.Data, 14);
        IndependentBigEndian.WriteUInt32(payload, 8, binary ? 1u : 0u);
        payload[12] = binary ? (byte)0xff : (byte)'A';
        payload[13] = 0;
        return payload;
    }

    private static byte[] TextPayload()
    {
        byte[] payload = Payload(IccTypeSignatures.Text, 10);
        payload[8] = (byte)'A';
        return payload;
    }

    private static byte[] EmptyCountPayload(uint signature, int size,
        uint recordSize)
    {
        byte[] payload = Payload(signature, size);
        if (size >= 16)
        {
            IndependentBigEndian.WriteUInt32(payload, 12, recordSize);
        }

        return payload;
    }

    private static byte[] CurvePayload()
    {
        byte[] payload = Payload(IccTypeSignatures.Curve, 16);
        IndependentBigEndian.WriteUInt32(payload, 8, 2);
        IndependentBigEndian.WriteUInt16(payload, 14, ushort.MaxValue);
        return payload;
    }

    private static byte[] ParametricCurvePayload()
    {
        byte[] payload = Payload(IccTypeSignatures.ParametricCurve, 16);
        IndependentBigEndian.WriteUInt32(payload, 12, 0x80000000);
        return payload;
    }

    private static byte[] ChromaticityPayload()
    {
        byte[] payload = Payload(IccTypeSignatures.Chromaticity, 20);
        IndependentBigEndian.WriteUInt16(payload, 8, 1);
        IndependentBigEndian.WriteUInt32(payload, 16, 0x00010000);
        return payload;
    }

    private static byte[] MeasurementPayload()
    {
        byte[] payload = Payload(IccTypeSignatures.Measurement, 36);
        IndependentBigEndian.WriteUInt32(payload, 8, 2);
        IndependentBigEndian.WriteUInt32(payload, 12, 0x80000000);
        IndependentBigEndian.WriteUInt32(payload, 16, 0);
        IndependentBigEndian.WriteUInt32(payload, 20, 0x7fffffff);
        IndependentBigEndian.WriteUInt32(payload, 24, 2);
        IndependentBigEndian.WriteUInt32(payload, 28, 0x00010000);
        IndependentBigEndian.WriteUInt32(payload, 32, 8);
        return payload;
    }

    private static byte[] ViewingConditionsPayload()
    {
        byte[] payload = Payload(IccTypeSignatures.ViewingConditions, 36);
        IndependentBigEndian.WriteUInt32(payload, 8, 0x80000000);
        IndependentBigEndian.WriteUInt32(payload, 12, 0);
        IndependentBigEndian.WriteUInt32(payload, 16, 0x7fffffff);
        IndependentBigEndian.WriteUInt32(payload, 20, 0x7fffffff);
        IndependentBigEndian.WriteUInt32(payload, 24, 0);
        IndependentBigEndian.WriteUInt32(payload, 28, 0x80000000);
        IndependentBigEndian.WriteUInt32(payload, 32, 8);
        return payload;
    }

    private static byte[] ColorantOrderPayload()
    {
        byte[] payload = Payload(IccTypeSignatures.ColorantOrder, 27);
        IndependentBigEndian.WriteUInt32(payload, 8, 15);
        int index = 0;
        while (index < 15)
        {
            payload[12 + index] = (byte)(14 - index);
            index++;
        }

        return payload;
    }

    private static byte[] ColorantTablePayload()
    {
        byte[] payload = Payload(IccTypeSignatures.ColorantTable, 50);
        IndependentBigEndian.WriteUInt32(payload, 8, 1);
        payload[12] = (byte)'K';
        IndependentBigEndian.WriteUInt16(payload, 44, 0);
        IndependentBigEndian.WriteUInt16(payload, 46, 0x8000);
        IndependentBigEndian.WriteUInt16(payload, 48, ushort.MaxValue);
        return payload;
    }

    private static byte[] ResponseCurvePayload()
    {
        byte[] payload = Payload(IccTypeSignatures.ResponseCurveSet16, 12);
        IndependentBigEndian.WriteUInt16(payload, 8, 1);
        return payload;
    }

    private static byte[] Payload(uint signature, int size)
    {
        byte[] payload = new byte[size];
        IndependentBigEndian.WriteUInt32(payload, 0, signature);
        return payload;
    }

    private static void WriteWidth(byte[] destination, int offset, int width,
        ulong value)
    {
        if (width == 1)
        {
            destination[offset] = (byte)value;
        }
        else if (width == 2)
        {
            IndependentBigEndian.WriteUInt16(destination, offset,
                (ushort)value);
        }
        else if (width == 4)
        {
            IndependentBigEndian.WriteUInt32(destination, offset,
                (uint)value);
        }
        else
        {
            IndependentBigEndian.WriteUInt64(destination, offset, value);
        }
    }

    private static TagDecodeState Decode(byte[] payload,
        IccVersionKind version)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length,
            500, version, true);
        state.ProfileClass = IccProfileClass.Display;
        state.DataColorSpace = IccColorSpaceKind.Gray;
        state.DataColorSpaceSignature = IccColorSpaceSignatures.Gray;
        state.ProfileConnectionSpace = IccPcsKind.Xyz;
        state.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static byte[] SerializeDecoded(TagDecodeState state)
    {
        uint signature = state.Data.RawData.TypeSignature;
        switch (signature)
        {
            case IccTypeSignatures.UInt8Array:
            case IccTypeSignatures.UInt16Array:
            case IccTypeSignatures.UInt32Array:
            case IccTypeSignatures.UInt64Array:
                return NestedTagDataSerializer.SerializeIntegerArray(
                    state.Data.IntegerValues, signature);
            case IccTypeSignatures.S15Fixed16Array:
            case IccTypeSignatures.U16Fixed16Array:
                return NestedTagDataSerializer.SerializeFixedPointArray(
                    state.Data.FixedPointValues, signature);
            case IccTypeSignatures.Xyz:
                return NestedTagDataSerializer.SerializeXyzArray(
                    state.Data.XyzValues);
            case IccTypeSignatures.Signature:
                return NestedTagDataSerializer.SerializeSignatureValue(
                    state.Data.Signature);
            case IccTypeSignatures.DateTime:
                return NestedTagDataSerializer.SerializeDateTimeValue(
                    state.Data.DateTime);
            case IccTypeSignatures.Data:
                if (state.Data.Kind == IccTagDataKind.Binary)
                {
                    return NestedTagDataSerializer.SerializeBinaryData(
                        state.Data.BinaryData, signature);
                }

                return NestedTagDataSerializer.SerializeTextData(
                    state.Data.Text, signature);
            case IccTypeSignatures.Text:
                return NestedTagDataSerializer.SerializeTextData(
                    state.Data.Text, signature);
            case IccTypeSignatures.MultiLocalizedUnicode:
                return LocalizedUnicodeTagCodec.SerializeMultiLocalizedUnicode(
                    state.Data.LocalizedStrings);
            case IccTypeSignatures.Dictionary:
                return DictionaryTagCodec.SerializeDictionary(
                    state.Data.Metadata);
            case IccTypeSignatures.Curve:
                return CurveTagCodec.SerializeCurve(state.Data.Curve!);
            case IccTypeSignatures.ParametricCurve:
                return CurveTagCodec.SerializeParametricCurve(
                    state.Data.Curve!.Parametric!);
            case IccTypeSignatures.Chromaticity:
                return ChromaticityTagCodec.SerializeChromaticity(
                    state.Data.Chromaticity!, IccVersionKind.Version4);
            case IccTypeSignatures.Measurement:
                return MeasurementTagCodec.SerializeMeasurement(
                    state.Data.Measurement!);
            case IccTypeSignatures.ViewingConditions:
                return ViewingConditionsTagCodec.SerializeViewingConditions(
                    state.Data.ViewingConditions!);
            case IccTypeSignatures.ColorantOrder:
                return ColorantOrderTagCodec.SerializeColorantOrder(
                    state.Data.IntegerValues);
            case IccTypeSignatures.ColorantTable:
                return ColorantTableTagCodec.SerializeColorantTable(
                    state.Data.Colorants!, IccVersionKind.Version4,
                    IccPcsKind.Xyz, false);
            case IccTypeSignatures.ProfileSequenceDescription:
                return ProfileSequenceTagCodec.SerializeProfileSequenceDescription(
                    state.Data.ProfileSequence, IccVersionKind.Version4);
            case IccTypeSignatures.ProfileSequenceIdentifier:
                return ProfileSequenceTagCodec.SerializeProfileSequenceIdentifier(
                    state.Data.ProfileSequenceIdentifiers);
            case IccTypeSignatures.ResponseCurveSet16:
                return ResponseCurveTagCodec.SerializeResponseCurveSet16(
                    state.Data.ResponseCurves, IccColorSpaceSignatures.Gray);
            default:
                throw new InvalidOperationException(
                    $"No golden serializer for 0x{signature:X8}.");
        }
    }

    private static (uint Signature, bool Version2, bool Version4)[]
        IccOneTypeSupportCases()
    {
        return new[]
        {
            Support(IccTypeSignatures.Chromaticity, true, true),
            Support(IccTypeSignatures.Cicp, false, true),
            Support(IccTypeSignatures.ColorantOrder, true, true),
            Support(IccTypeSignatures.ColorantTable, true, true),
            Support(IccTypeSignatures.Curve, true, true),
            Support(IccTypeSignatures.Data, true, true),
            Support(IccTypeSignatures.DateTime, true, true),
            Support(IccTypeSignatures.Dictionary, true, true),
            Support(IccTypeSignatures.Lut8, true, true),
            Support(IccTypeSignatures.Lut16, true, true),
            Support(IccTypeSignatures.LutAToB, false, true),
            Support(IccTypeSignatures.LutBToA, false, true),
            Support(IccTypeSignatures.Measurement, true, true),
            Support(IccTypeSignatures.MultiLocalizedUnicode, false, true),
            Support(IccTypeSignatures.MultiProcessElements, false, true),
            Support(IccTypeSignatures.NamedColor2, true, true),
            Support(IccTypeSignatures.ParametricCurve, false, true),
            Support(IccTypeSignatures.ProfileSequenceDescription, true, true),
            Support(IccTypeSignatures.ProfileSequenceIdentifier, false, true),
            Support(IccTypeSignatures.ResponseCurveSet16, true, true),
            Support(IccTypeSignatures.S15Fixed16Array, true, true),
            Support(IccTypeSignatures.Signature, true, true),
            Support(IccTypeSignatures.Text, true, true),
            Support(IccTypeSignatures.U16Fixed16Array, true, true),
            Support(IccTypeSignatures.UInt8Array, true, true),
            Support(IccTypeSignatures.UInt16Array, true, true),
            Support(IccTypeSignatures.UInt32Array, true, true),
            Support(IccTypeSignatures.UInt64Array, true, true),
            Support(IccTypeSignatures.ViewingConditions, true, true),
            Support(IccTypeSignatures.Xyz, true, true),
            Support(IccTypeSignatures.TextDescription, true, false),
            Support(IccTypeSignatures.CrdInfo, true, false),
            Support(IccTypeSignatures.NamedColor, true, false),
            Support(IccTypeSignatures.Screening, true, false),
            Support(IccTypeSignatures.UcrBg, true, false),
            Support(IccTypeSignatures.DeviceSettings, true, false),
            Support(IccTypeSignatures.EmbeddedProfile, true, true),
            Support(IccTypeSignatures.AdaptiveGainCurve, false, true)
        };
    }

    private static (uint, bool, bool) Support(uint signature, bool version2,
        bool version4)
    {
        return (signature, version2, version4);
    }

    private static IccTag TextDataTag(string value)
    {
        IccTextData text = new IccTextData();
        text.TypeSignature = IccTypeSignatures.Data;
        text.Text = value;
        IccTag tag = new IccTag();
        tag.Data.Kind = IccTagDataKind.Text;
        tag.Data.Text = text;
        return tag;
    }

    private static IccProfileHeader Header(IccVersionKind version)
    {
        IccProfileHeader header = new IccProfileHeader();
        header.Version.Kind = version;
        return header;
    }
}
