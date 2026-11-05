using icc;

namespace icc.Tests;

public class BaseTagCodecTests
{
    [Test]
    public void IntegerArrayTypesDecodeTheirBigEndianElementWidths()
    {
        byte[] uint8Payload = CreatePayload(IccTypeSignatures.UInt8Array, 3);
        uint8Payload[8] = 0;
        uint8Payload[9] = 127;
        uint8Payload[10] = 255;
        byte[] uint16Payload = CreatePayload(IccTypeSignatures.UInt16Array, 6);
        WriteUInt16(uint16Payload, 8, 0);
        WriteUInt16(uint16Payload, 10, 0x1234);
        WriteUInt16(uint16Payload, 12, ushort.MaxValue);
        byte[] uint32Payload = CreatePayload(IccTypeSignatures.UInt32Array, 8);
        WriteUInt32(uint32Payload, 8, 0x89abcdef);
        WriteUInt32(uint32Payload, 12, uint.MaxValue);
        byte[] uint64Payload = CreatePayload(IccTypeSignatures.UInt64Array, 16);
        WriteUInt64(uint64Payload, 8, 0x7fffffffffffffff);
        WriteUInt64(uint64Payload, 16, ulong.MaxValue);

        TagDecodeState uint8State = DecodePayload(uint8Payload, IccVersionKind.Version4, true);
        TagDecodeState uint16State = DecodePayload(uint16Payload, IccVersionKind.Version4, true);
        TagDecodeState uint32State = DecodePayload(uint32Payload, IccVersionKind.Version4, true);
        TagDecodeState uint64State = DecodePayload(uint64Payload, IccVersionKind.Version4, true);

        Assert.Multiple(() =>
        {
            Assert.That(uint8State.Data.IntegerValues, Is.EqualTo(new long[] { 0, 127, 255 }));
            Assert.That(uint16State.Data.IntegerValues, Is.EqualTo(new long[] { 0, 0x1234, 0xffff }));
            Assert.That(uint32State.Data.IntegerValues, Is.EqualTo(new long[] { 0x89abcdef, uint.MaxValue }));
            Assert.That(uint64State.Data.IntegerValues, Is.EqualTo(new long[] { long.MaxValue, -1 }));
            Assert.That(uint8State.Data.Kind, Is.EqualTo(IccTagDataKind.IntegerArray));
            Assert.That(uint64State.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(uint64State.Data.RawData.Bytes, Is.EqualTo(uint64Payload));
        });
    }

    [Test]
    public void FixedPointArrayTypesDecodeSignedAndUnsignedValues()
    {
        byte[] signedPayload = CreatePayload(IccTypeSignatures.S15Fixed16Array, 8);
        WriteUInt32(signedPayload, 8, 0xfffe8000);
        WriteUInt32(signedPayload, 12, 0x00014000);
        byte[] unsignedPayload = CreatePayload(IccTypeSignatures.U16Fixed16Array, 8);
        WriteUInt32(unsignedPayload, 8, 0x00018000);
        WriteUInt32(unsignedPayload, 12, 0xffff0000);

        TagDecodeState signedState = DecodePayload(signedPayload, IccVersionKind.Version2, false);
        TagDecodeState unsignedState = DecodePayload(unsignedPayload, IccVersionKind.Version5, false);

        Assert.Multiple(() =>
        {
            Assert.That(signedState.Data.Kind, Is.EqualTo(IccTagDataKind.FixedPointArray));
            Assert.That(signedState.Data.FixedPointValues, Is.EqualTo(new double[] { -1.5, 1.25 }));
            Assert.That(unsignedState.Data.FixedPointValues, Is.EqualTo(new double[] { 1.5, 65535.0 }));
            Assert.That(signedState.Data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.S15Fixed16Array));
            Assert.That(signedState.Data.RawData.Bytes, Is.Empty);
        });
    }

    [Test]
    public void XyzTypeDecodesEveryCompleteXyzNumber()
    {
        byte[] payload = CreatePayload(IccTypeSignatures.Xyz, 24);
        WriteUInt32(payload, 8, 0x00008000);
        WriteUInt32(payload, 12, 0x00010000);
        WriteUInt32(payload, 16, 0xffffc000);
        WriteUInt32(payload, 20, 0x00020000);
        WriteUInt32(payload, 24, 0x00030000);
        WriteUInt32(payload, 28, 0x00040000);

        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version4, true);

        Assert.Multiple(() =>
        {
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.XyzArray));
            Assert.That(state.Data.XyzValues, Has.Length.EqualTo(2));
            Assert.That(state.Data.XyzValues[0].X, Is.EqualTo(0.5));
            Assert.That(state.Data.XyzValues[0].Y, Is.EqualTo(1.0));
            Assert.That(state.Data.XyzValues[0].Z, Is.EqualTo(-0.25));
            Assert.That(state.Data.XyzValues[1].X, Is.EqualTo(2.0));
            Assert.That(state.Data.XyzValues[1].Y, Is.EqualTo(3.0));
            Assert.That(state.Data.XyzValues[1].Z, Is.EqualTo(4.0));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
        });
    }

    [Test]
    public void SignatureAndDateTimeTypesRequireAndDecodeTheirExactLayouts()
    {
        byte[] signaturePayload = CreatePayload(IccTypeSignatures.Signature, 4);
        WriteUInt32(signaturePayload, 8, 0x41424320);
        byte[] dateTimePayload = CreatePayload(IccTypeSignatures.DateTime, 12);
        WriteUInt16(dateTimePayload, 8, 2026);
        WriteUInt16(dateTimePayload, 10, 9);
        WriteUInt16(dateTimePayload, 12, 11);
        WriteUInt16(dateTimePayload, 14, 23);
        WriteUInt16(dateTimePayload, 16, 58);
        WriteUInt16(dateTimePayload, 18, 59);

        TagDecodeState signatureState = DecodePayload(signaturePayload, IccVersionKind.Version4, true);
        TagDecodeState dateTimeState = DecodePayload(dateTimePayload, IccVersionKind.Version4, true);

        Assert.Multiple(() =>
        {
            Assert.That(signatureState.Data.Kind, Is.EqualTo(IccTagDataKind.Signature));
            Assert.That(signatureState.Data.Signature, Is.EqualTo(0x41424320));
            Assert.That(dateTimeState.Data.Kind, Is.EqualTo(IccTagDataKind.DateTime));
            Assert.That(dateTimeState.Data.DateTime, Is.Not.Null);
            Assert.That(dateTimeState.Data.DateTime!.Year, Is.EqualTo(2026));
            Assert.That(dateTimeState.Data.DateTime.Month, Is.EqualTo(9));
            Assert.That(dateTimeState.Data.DateTime.Day, Is.EqualTo(11));
            Assert.That(dateTimeState.Data.DateTime.Hour, Is.EqualTo(23));
            Assert.That(dateTimeState.Data.DateTime.Minute, Is.EqualTo(58));
            Assert.That(dateTimeState.Data.DateTime.Second, Is.EqualTo(59));
        });
    }

    [Test]
    public void DataTypeDistinguishesNullTerminatedAsciiFromTransparentBinaryBytes()
    {
        byte[] asciiPayload = CreatePayload(IccTypeSignatures.Data, 7);
        WriteUInt32(asciiPayload, 8, 0);
        asciiPayload[12] = (byte)'H';
        asciiPayload[13] = (byte)'i';
        asciiPayload[14] = 0;
        byte[] binaryPayload = CreatePayload(IccTypeSignatures.Data, 7);
        WriteUInt32(binaryPayload, 8, 1);
        binaryPayload[12] = 0;
        binaryPayload[13] = 0x80;
        binaryPayload[14] = 0xff;

        TagDecodeState asciiState = DecodePayload(asciiPayload, IccVersionKind.Version4, true);
        TagDecodeState binaryState = DecodePayload(binaryPayload, IccVersionKind.Version4, true);

        Assert.Multiple(() =>
        {
            Assert.That(asciiState.Data.Kind, Is.EqualTo(IccTagDataKind.Text));
            Assert.That(asciiState.Data.Text, Is.Not.Null);
            Assert.That(asciiState.Data.Text!.TypeSignature, Is.EqualTo(IccTypeSignatures.Data));
            Assert.That(asciiState.Data.Text.Text, Is.EqualTo("Hi"));
            Assert.That(asciiState.Data.Text.Bytes, Is.EqualTo(new byte[] { 72, 105, 0 }));
            Assert.That(binaryState.Data.Kind, Is.EqualTo(IccTagDataKind.Binary));
            Assert.That(binaryState.Data.BinaryData, Is.EqualTo(new byte[] { 0, 0x80, 0xff }));
            Assert.That(binaryState.Result.Status, Is.EqualTo(IccOperationStatus.Success));
        });
    }

    [TestCase(IccVersionKind.Version2)]
    [TestCase(IccVersionKind.Version4)]
    [TestCase(IccVersionKind.Version5)]
    public void TextTypeDecodesNullTerminatedSevenBitAsciiForEachProfileGeneration(IccVersionKind version)
    {
        byte[] payload = CreatePayload(IccTypeSignatures.Text, 4);
        payload[8] = (byte)'I';
        payload[9] = (byte)'C';
        payload[10] = (byte)'C';
        payload[11] = 0;

        TagDecodeState state = DecodePayload(payload, version, true);

        Assert.Multiple(() =>
        {
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.Text));
            Assert.That(state.Data.Text, Is.Not.Null);
            Assert.That(state.Data.Text!.Text, Is.EqualTo("ICC"));
            Assert.That(state.Data.Text.TypeSignature, Is.EqualTo(IccTypeSignatures.Text));
            Assert.That(state.Data.Text.Bytes, Is.EqualTo(new byte[] { 73, 67, 67, 0 }));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Result.Message, Does.Contain(version == IccVersionKind.Version2 ? "version 2" : version == IccVersionKind.Version4 ? "version 4" : "version 5"));
        });
    }

    [Test]
    public void TextTypeIgnoresOnlyZeroBytesAfterFirstNullTerminator()
    {
        byte[] zeroPadded = CreatePayload(IccTypeSignatures.Text, 8);
        zeroPadded[8] = (byte)'I';
        zeroPadded[9] = (byte)'C';
        zeroPadded[10] = (byte)'C';
        zeroPadded[11] = (byte)'2';
        zeroPadded[12] = (byte)'2';
        byte[] nonzeroPadded = zeroPadded.ToArray();
        nonzeroPadded[15] = 1;

        TagDecodeState accepted = DecodePayload(zeroPadded,
            IccVersionKind.Version2, true);
        TagDecodeState rejected = DecodePayload(nonzeroPadded,
            IccVersionKind.Version2, true);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Result.Status,
                Is.EqualTo(IccOperationStatus.Success));
            Assert.That(accepted.Data.Text!.Text, Is.EqualTo("ICC22"));
            Assert.That(accepted.Data.Text.Bytes,
                Is.EqualTo(new byte[] { 73, 67, 67, 50, 50, 0, 0, 0 }));
            Assert.That(rejected.Result.Status,
                Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(rejected.Result.Message,
                Does.Contain("nonzero byte").And.Contain("terminator"));
        });
    }

    [Test]
    public void DataTypeUsesUtf8OnlyForIccVersion5()
    {
        byte[] payload = CreatePayload(IccTypeSignatures.Data, 7);
        WriteUInt32(payload, 8, 0);
        payload[12] = 0xc3;
        payload[13] = 0xa9;
        payload[14] = 0;

        TagDecodeState version4State = DecodePayload(payload, IccVersionKind.Version4, true);
        TagDecodeState version5State = DecodePayload(payload, IccVersionKind.Version5, true);

        Assert.Multiple(() =>
        {
            Assert.That(version4State.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(version4State.Result.Message, Does.Contain("7-bit ASCII"));
            Assert.That(version5State.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(version5State.Data.Text, Is.Not.Null);
            Assert.That(version5State.Data.Text!.Text, Is.EqualTo("é"));
            Assert.That(version5State.Data.Text.Bytes, Is.EqualTo(new byte[] { 0xc3, 0xa9, 0 }));
        });
    }

    [TestCase(new byte[] { 0xc0, 0x80, 0 })]
    [TestCase(new byte[] { 0xed, 0xa0, 0x80, 0 })]
    [TestCase(new byte[] { 0xf4, 0x90, 0x80, 0x80, 0 })]
    [TestCase(new byte[] { 0xe2, 0x82, 0 })]
    public void Version5DataRejectsMalformedUtf8(byte[] encodedText)
    {
        byte[] payload = CreatePayload(IccTypeSignatures.Data, 4 + encodedText.Length);
        WriteUInt32(payload, 8, 0);
        System.Array.Copy(encodedText, 0, payload, 12, encodedText.Length);

        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version5, true);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(state.Result.Message, Does.Contain("invalid UTF-8"));
            Assert.That(state.Data.RawData.Bytes, Is.EqualTo(payload));
        });
    }

    [TestCase(IccTypeSignatures.UInt16Array, 3)]
    [TestCase(IccTypeSignatures.UInt32Array, 6)]
    [TestCase(IccTypeSignatures.UInt64Array, 12)]
    [TestCase(IccTypeSignatures.S15Fixed16Array, 6)]
    [TestCase(IccTypeSignatures.U16Fixed16Array, 6)]
    [TestCase(IccTypeSignatures.Xyz, 16)]
    public void ArrayTypesRejectPartialTrailingElements(uint typeSignature, int bodyLength)
    {
        byte[] payload = CreatePayload(typeSignature, bodyLength);

        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version4, true);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(state.Result.Message, Does.Contain("not divisible"));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(state.Data.RawData.Bytes, Is.EqualTo(payload));
        });
    }

    [Test]
    public void FixedLayoutAndReservedFieldsAreValidatedWithoutReadingPastPayloads()
    {
        byte[] shortSignature = CreatePayload(IccTypeSignatures.Signature, 3);
        byte[] longDateTime = CreatePayload(IccTypeSignatures.DateTime, 13);
        byte[] reserved = CreatePayload(IccTypeSignatures.UInt8Array, 1);
        reserved[4] = 1;

        TagDecodeState signatureState = DecodePayload(shortSignature, IccVersionKind.Version4, true);
        TagDecodeState dateTimeState = DecodePayload(longDateTime, IccVersionKind.Version4, true);
        TagDecodeState reservedState = DecodePayload(reserved, IccVersionKind.Version4, true);

        Assert.Multiple(() =>
        {
            Assert.That(signatureState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(signatureState.Result.Message, Does.Contain("exactly 12 bytes"));
            Assert.That(dateTimeState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(dateTimeState.Result.Message, Does.Contain("exactly 20 bytes"));
            Assert.That(reservedState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(reservedState.Result.Message, Does.Contain("reserved bytes"));
        });
    }

    [Test]
    public void DateTimeAndAsciiPayloadsRejectInvalidValuesAndTerminators()
    {
        byte[] invalidDate = CreatePayload(IccTypeSignatures.DateTime, 12);
        WriteUInt16(invalidDate, 8, 2026);
        WriteUInt16(invalidDate, 10, 13);
        WriteUInt16(invalidDate, 12, 1);
        byte[] missingTerminator = CreatePayload(IccTypeSignatures.Text, 2);
        missingTerminator[8] = (byte)'N';
        missingTerminator[9] = (byte)'O';
        byte[] nonAscii = CreatePayload(IccTypeSignatures.Text, 2);
        nonAscii[8] = 0x80;
        nonAscii[9] = 0;
        byte[] unknownDataFlag = CreatePayload(IccTypeSignatures.Data, 4);
        WriteUInt32(unknownDataFlag, 8, 2);

        TagDecodeState dateState = DecodePayload(invalidDate, IccVersionKind.Version4, true);
        TagDecodeState terminatorState = DecodePayload(missingTerminator, IccVersionKind.Version4, true);
        TagDecodeState asciiState = DecodePayload(nonAscii, IccVersionKind.Version4, true);
        TagDecodeState flagState = DecodePayload(unknownDataFlag, IccVersionKind.Version4, true);

        Assert.Multiple(() =>
        {
            Assert.That(dateState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(dateState.Result.Message, Does.Contain("dateTimeNumber ranges"));
            Assert.That(terminatorState.Result.Message, Does.Contain("NULL byte"));
            Assert.That(asciiState.Result.Message, Does.Contain("7-bit ASCII"));
            Assert.That(flagState.Result.Message, Does.Contain("reserved").And.Contain("2"));
        });
    }

    private static TagDecodeState DecodePayload(byte[] payload, IccVersionKind version, bool preserveOriginalBytes)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length, 250, version, preserveOriginalBytes);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static byte[] CreatePayload(uint typeSignature, int bodyLength)
    {
        byte[] payload = new byte[8 + bodyLength];
        WriteUInt32(payload, 0, typeSignature);
        return payload;
    }

    private static void WriteUInt16(byte[] destination, int offset, ushort value)
    {
        destination[offset] = (byte)(value >> 8);
        destination[offset + 1] = (byte)value;
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }

    private static void WriteUInt64(byte[] destination, int offset, ulong value)
    {
        WriteUInt32(destination, offset, (uint)(value >> 32));
        WriteUInt32(destination, offset + 4, (uint)value);
    }
}
