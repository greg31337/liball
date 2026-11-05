using System.IO.Compression;
using System.Text;
using icc;

namespace icc.Tests;

public class Icc2TextAndArrayTagCodecTests
{
    [Test]
    public void FloatingPointArrayTypesRoundTripFiniteValuesAtTheirDeclaredPrecision()
    {
        byte[] halfPayload = Icc2ArrayTagCodec.SerializeFloatingPointArray(IccTypeSignatures.Float16Array, new double[] { 1.5, -2.0, 0.0 });
        byte[] singlePayload = Icc2ArrayTagCodec.SerializeFloatingPointArray(IccTypeSignatures.Float32Array, new double[] { 1.25, -8.5 });
        byte[] doublePayload = Icc2ArrayTagCodec.SerializeFloatingPointArray(IccTypeSignatures.Float64Array, new double[] { double.Epsilon, -0.0, 123456.75 });

        TagDecodeState half = DecodePayload(halfPayload, IccVersionKind.Version5, true);
        TagDecodeState single = DecodePayload(singlePayload, IccVersionKind.Version5, false);
        TagDecodeState twice = DecodePayload(doublePayload, IccVersionKind.Version5, false);

        Assert.Multiple(() =>
        {
            Assert.That(half.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(half.Data.Kind, Is.EqualTo(IccTagDataKind.FloatingPointArray));
            Assert.That(half.Data.FloatingPointValues, Is.EqualTo(new double[] { 1.5, -2.0, 0.0 }));
            Assert.That(half.Data.RawData.Bytes, Is.EqualTo(halfPayload));
            Assert.That(single.Data.FloatingPointValues, Is.EqualTo(new double[] { 1.25, -8.5 }));
            Assert.That(twice.Data.FloatingPointValues[0], Is.EqualTo(double.Epsilon));
            Assert.That(BitConverter.DoubleToInt64Bits(twice.Data.FloatingPointValues[1]), Is.EqualTo(BitConverter.DoubleToInt64Bits(-0.0)));
            Assert.That(twice.Data.FloatingPointValues[2], Is.EqualTo(123456.75));
        });
    }

    [Test]
    public void FloatingPointArraysRejectPartialAndNonFiniteElements()
    {
        byte[] partial = CreatePayload(IccTypeSignatures.Float32Array, new byte[] { 0, 0, 0 });
        byte[] infinity = CreatePayload(IccTypeSignatures.Float64Array, new byte[] { 0x7f, 0xf0, 0, 0, 0, 0, 0, 0 });
        byte[] subnormal = CreatePayload(IccTypeSignatures.Float32Array, new byte[] { 0, 0, 0, 1 });
        byte[] reserved = Icc2ArrayTagCodec.SerializeFloatingPointArray(IccTypeSignatures.Float16Array, new double[] { 1.0 });
        reserved[7] = 1;

        TagDecodeState partialState = DecodePayload(partial, IccVersionKind.Version5, true);
        TagDecodeState infinityState = DecodePayload(infinity, IccVersionKind.Version5, true);
        TagDecodeState subnormalState = DecodePayload(subnormal, IccVersionKind.Version5, true);
        TagDecodeState reservedState = DecodePayload(reserved, IccVersionKind.Version5, true);

        Assert.Multiple(() =>
        {
            Assert.That(partialState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(partialState.Result.Message, Does.Contain("not divisible"));
            Assert.That(infinityState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(infinityState.Result.Message, Does.Contain("infinity").And.Contain("NaN"));
            Assert.That(subnormalState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(subnormalState.Result.Message, Does.Contain("subnormal"));
            Assert.That(subnormalState.Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(reservedState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.Throws<IccProfileWriteException>(() => Icc2ArrayTagCodec.SerializeFloatingPointArray(IccTypeSignatures.Float32Array, new double[] { double.NaN }));
        });
    }

    [TestCase(IccTypeSignatures.ZippedUtf8, "bounded text")]
    [TestCase(IccTypeSignatures.ZippedXml, "<bounded>XML</bounded>")]
    public void CompressedTextAndXmlAcceptExactExpansionLimitAndRejectOneByteLess(uint typeSignature, string text)
    {
        byte[] content = Encoding.UTF8.GetBytes(text);
        byte[] payload = CreatePayload(typeSignature, CompressWithFramework(content));
        TagDecodeState exact = CreateDecodeState(payload, IccVersionKind.Version5, true);
        exact.MaximumDecompressedBytes = content.Length;
        TagCodecDispatcher.DecodeTagPayload(exact);
        TagDecodeState tooSmall = CreateDecodeState(payload, IccVersionKind.Version5, true);
        tooSmall.MaximumDecompressedBytes = content.Length - 1;
        TagCodecDispatcher.DecodeTagPayload(tooSmall);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(exact.Data.Text!.Text, Is.EqualTo(text));
            Assert.That(tooSmall.Result.Status, Is.EqualTo(IccOperationStatus.ResourceLimitExceeded));
            Assert.That(tooSmall.Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(tooSmall.Data.RawData.Bytes, Is.EqualTo(payload));
        });
    }

    [Test]
    public void Utf8AndBigEndianUtf16TypesRoundTripUnicodeWithoutTerminators()
    {
        IccTextData utf8 = new IccTextData { TypeSignature = IccTypeSignatures.Utf8, Text = "Colour \U0001f600" };
        IccTextData utf16 = new IccTextData { TypeSignature = IccTypeSignatures.Utf16, Text = "D50 \U0001d11e" };

        byte[] utf8Payload = Icc2TextTagCodec.SerializeIcc2Text(utf8);
        byte[] utf16Payload = Icc2TextTagCodec.SerializeIcc2Text(utf16);
        TagDecodeState utf8State = DecodePayload(utf8Payload, IccVersionKind.Version5, true);
        TagDecodeState utf16State = DecodePayload(utf16Payload, IccVersionKind.Version5, true);

        Assert.Multiple(() =>
        {
            Assert.That(utf8State.Data.Text!.Text, Is.EqualTo(utf8.Text));
            Assert.That(utf8State.Data.Text.Bytes, Is.EqualTo(Encoding.UTF8.GetBytes(utf8.Text)));
            Assert.That(utf8State.Data.Text.IsCompressed, Is.False);
            Assert.That(utf16State.Data.Text!.Text, Is.EqualTo(utf16.Text));
            Assert.That(utf16Payload.Length, Is.EqualTo(8 + utf16.Text.Length * 2));
            Assert.That(utf16State.Result.Status, Is.EqualTo(IccOperationStatus.Success));
        });
    }

    [Test]
    public void UnicodeTextTypesRejectInvalidByteAndSurrogateSequences()
    {
        byte[] invalidUtf8 = CreatePayload(IccTypeSignatures.Utf8, new byte[] { 0xe0, 0x80, 0x80 });
        byte[] invalidUtf16 = CreatePayload(IccTypeSignatures.Utf16, new byte[] { 0xd8, 0x00 });
        byte[] littleEndianBom = CreatePayload(IccTypeSignatures.Utf16, new byte[] { 0xff, 0xfe, 0x41, 0x00 });

        TagDecodeState utf8State = DecodePayload(invalidUtf8, IccVersionKind.Version5, true);
        TagDecodeState utf16State = DecodePayload(invalidUtf16, IccVersionKind.Version5, true);
        TagDecodeState bomState = DecodePayload(littleEndianBom, IccVersionKind.Version5, true);

        Assert.Multiple(() =>
        {
            Assert.That(utf8State.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(utf8State.Result.Message, Does.Contain("invalid UTF-8"));
            Assert.That(utf16State.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(utf16State.Result.Message, Does.Contain("surrogate"));
            Assert.That(bomState.Result.Message, Does.Contain("little-endian"));
        });
    }

    [Test]
    public void ZippedUtf8DecodesStandardDynamicZlibStreamsAndCanonicalStoredStreams()
    {
        string text = string.Concat(Enumerable.Range(0, 300).Select(index => $"sample-{index}-\U0001f600;"));
        byte[] frameworkPayload = CreatePayload(IccTypeSignatures.ZippedUtf8, CompressWithFramework(Encoding.UTF8.GetBytes(text)));
        IccTextData canonical = new IccTextData { TypeSignature = IccTypeSignatures.ZippedUtf8, Text = text };
        byte[] canonicalPayload = Icc2TextTagCodec.SerializeIcc2Text(canonical);

        TagDecodeState frameworkState = DecodePayload(frameworkPayload, IccVersionKind.Version5, true);
        TagDecodeState canonicalState = DecodePayload(canonicalPayload, IccVersionKind.Version5, false);

        Assert.Multiple(() =>
        {
            Assert.That(frameworkState.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(frameworkState.Data.Text!.Text, Is.EqualTo(text));
            Assert.That(frameworkState.Data.Text.IsCompressed, Is.True);
            Assert.That(canonicalState.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(canonicalState.Data.Text!.Text, Is.EqualTo(text));
            Assert.That(canonicalPayload[8], Is.EqualTo(0x78));
            Assert.That(canonicalPayload[9], Is.EqualTo(0x01));
        });
    }

    [Test]
    public void ZippedTextEnforcesChecksumsAndConfiguredExpansionLimit()
    {
        byte[] payload = CreatePayload(IccTypeSignatures.ZippedUtf8, CompressWithFramework(Encoding.UTF8.GetBytes(new string('A', 200))));
        byte[] corrupt = (byte[])payload.Clone();
        corrupt[corrupt.Length - 1] ^= 1;

        TagDecodeState corruptState = DecodePayload(corrupt, IccVersionKind.Version5, true);
        byte[] invalidUtf8Payload = CreatePayload(IccTypeSignatures.ZippedUtf8, CompressWithFramework(new byte[] { 0xc0, 0x80 }));
        TagDecodeState invalidUtf8State = DecodePayload(invalidUtf8Payload, IccVersionKind.Version5, true);
        TagDecodeState malformedState = DecodePayload(CreatePayload(IccTypeSignatures.ZippedUtf8, new byte[] { 0, 0, 0, 0, 0, 0 }), IccVersionKind.Version5, true);
        TagDecodeState limitedState = CreateDecodeState(payload, IccVersionKind.Version5, true);
        limitedState.MaximumDecompressedBytes = 32;
        TagCodecDispatcher.DecodeTagPayload(limitedState);
        IccReadOptions options = new IccReadOptions();
        options.ResourceLimits.MaximumDecompressedBytes = 32;
        IccProfile profile = new IccProfileReader().ReadProfileBytes(CreateProfileWithPayload(payload), options);
        IccTagData profileData = TagDecoder.GetDecodedData(profile.Tags[0]);

        Assert.Multiple(() =>
        {
            Assert.That(corruptState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(corruptState.Result.Message, Does.Contain("Adler-32"));
            Assert.That(invalidUtf8State.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(invalidUtf8State.Result.Message, Does.Contain("invalid UTF-8"));
            Assert.That(malformedState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(malformedState.Result.Message, Does.Contain("zlib stream header"));
            Assert.That(limitedState.Result.Status, Is.EqualTo(IccOperationStatus.ResourceLimitExceeded));
            Assert.That(limitedState.Result.Message, Does.Contain("32 bytes"));
            Assert.That(limitedState.Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(limitedState.Data.RawData.Bytes, Is.EqualTo(payload));
            Assert.That(profile.Tags[0].DecodeResult.Status, Is.EqualTo(IccOperationStatus.ResourceLimitExceeded));
            Assert.That(profileData.Kind, Is.EqualTo(IccTagDataKind.Raw));
        });
    }

    [Test]
    public void ZippedXmlSupportsBothSignaturesAndValidatesDeclaredUnicodeEncoding()
    {
        string utf8Xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><sample>Colour \U0001f600</sample>";
        byte[] lowerPayload = Icc2TextTagCodec.SerializeIcc2Text(new IccTextData { TypeSignature = IccTypeSignatures.ZippedXml, Text = utf8Xml });
        byte[] littleEndianXml = AddLittleEndianBom(Encoding.Unicode.GetBytes("<?xml version=\"1.0\" encoding=\"UTF-16\"?><sample>ok</sample>"));
        byte[] upperPayload = CreatePayload(IccTypeSignatures.ZippedXmlUppercase, CompressWithFramework(littleEndianXml));
        byte[] mismatchBytes = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"UTF-16\"?><sample/>");
        byte[] mismatchPayload = CreatePayload(IccTypeSignatures.ZippedXml, CompressWithFramework(mismatchBytes));

        TagDecodeState lower = DecodePayload(lowerPayload, IccVersionKind.Version5, true);
        TagDecodeState upper = DecodePayload(upperPayload, IccVersionKind.Version5, true);
        TagDecodeState mismatch = DecodePayload(mismatchPayload, IccVersionKind.Version5, true);

        Assert.Multiple(() =>
        {
            Assert.That(lower.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(lower.Data.Text!.IsXml, Is.True);
            Assert.That(lower.Data.Text.Text, Is.EqualTo(utf8Xml));
            Assert.That(upper.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(upper.Data.Text!.Text, Does.Contain("<sample>ok</sample>"));
            Assert.That(mismatch.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(mismatch.Result.Message, Does.Contain("encoding declaration"));
            Assert.Throws<IccProfileWriteException>(() => Icc2TextTagCodec.SerializeIcc2Text(new IccTextData { TypeSignature = IccTypeSignatures.ZippedXml, Text = "<?xml version=\"1.0\" encoding=\"UTF-16\"?><sample/>" }));
        });
    }

    [Test]
    public void SparseMatrixArrayRoundTripsCompactMatricesAndClonesWithoutAliases()
    {
        IccSparseMatrixArray source = new IccSparseMatrixArray
        {
            EquivalentOutputChannels = 6,
            Encoding = 4,
            Matrices = new IccSparseMatrix[]
            {
                CreateSparseMatrix(new int[] { 0, 2 }, new int[] { 0, 2, 1 }, new double[] { 0.5, 1.5, -0.25 }),
                CreateSparseMatrix(new int[] { 0, 1 }, new int[] { 1, 0 }, new double[] { 2.0, 3.0 })
            }
        };

        byte[] payload = Icc2ArrayTagCodec.SerializeSparseMatrixArray(source);
        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version5, true);
        IccTagData clone = ProfileClone.CloneTagDataStandalone(state.Data);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.SparseMatrixArray));
            Assert.That(state.Data.SparseMatrixArray!.EquivalentOutputChannels, Is.EqualTo(6));
            Assert.That(state.Data.SparseMatrixArray.Matrices, Has.Length.EqualTo(2));
            Assert.That(state.Data.SparseMatrixArray.Matrices[0].Values, Is.EqualTo(new double[] { 0.5, 1.5, -0.25 }));
            Assert.That(state.Data.SparseMatrixArray.Matrices[1].ColumnIndices, Is.EqualTo(new int[] { 1, 0 }));
            Assert.That(clone.SparseMatrixArray, Is.Not.SameAs(state.Data.SparseMatrixArray));
            Assert.That(clone.SparseMatrixArray!.Matrices[0].Values, Is.Not.SameAs(state.Data.SparseMatrixArray.Matrices[0].Values));
        });
    }

    [Test]
    public void SparseMatrixArrayRejectsDimensionMismatchNonCompactPaddingAndTrailingData()
    {
        IccSparseMatrixArray source = new IccSparseMatrixArray
        {
            EquivalentOutputChannels = 6,
            Encoding = 1,
            Matrices = new IccSparseMatrix[] { CreateUInt8SparseMatrix() }
        };
        byte[] valid = Icc2ArrayTagCodec.SerializeSparseMatrixArray(source);
        byte[] badChannels = (byte[])valid.Clone();
        WriteUInt16(badChannels, 8, 5);
        byte[] badPadding = (byte[])valid.Clone();
        badPadding[35] = 1;
        byte[] trailing = new byte[valid.Length + 1];
        Array.Copy(valid, trailing, valid.Length);

        TagDecodeState channelsState = DecodePayload(badChannels, IccVersionKind.Version5, true);
        TagDecodeState paddingState = DecodePayload(badPadding, IccVersionKind.Version5, true);
        TagDecodeState trailingState = DecodePayload(trailing, IccVersionKind.Version5, true);

        Assert.Multiple(() =>
        {
            Assert.That(channelsState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(channelsState.Result.Message, Does.Contain("dense output channels"));
            Assert.That(paddingState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(paddingState.Result.Message, Does.Contain("compact padding"));
            Assert.That(trailingState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(trailingState.Result.Message, Does.Contain("unconsumed"));
        });
    }

    [Test]
    public void Icc2OnlyTypesFallBackLosslesslyForEarlierProfileGenerations()
    {
        byte[] payload = Icc2TextTagCodec.SerializeIcc2Text(new IccTextData { TypeSignature = IccTypeSignatures.Utf8, Text = "ICC.2" });

        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version4, true);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(state.Data.RawData.Bytes, Is.EqualTo(payload));
        });
    }

    private static IccSparseMatrix CreateSparseMatrix(int[] rowStarts, int[] columns, double[] values)
    {
        return new IccSparseMatrix
        {
            Rows = 2,
            Columns = 3,
            Encoding = 4,
            RowStartOffsets = rowStarts,
            ColumnIndices = columns,
            Values = values
        };
    }

    private static IccSparseMatrix CreateUInt8SparseMatrix()
    {
        return new IccSparseMatrix
        {
            Rows = 2,
            Columns = 3,
            Encoding = 1,
            RowStartOffsets = new int[] { 0, 2 },
            ColumnIndices = new int[] { 0, 2, 1 },
            Values = new double[] { 0.25, 0.5, 0.75 }
        };
    }

    private static TagDecodeState DecodePayload(byte[] payload, IccVersionKind version, bool preserveOriginalBytes)
    {
        TagDecodeState state = CreateDecodeState(payload, version, preserveOriginalBytes);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static TagDecodeState CreateDecodeState(byte[] payload, IccVersionKind version, bool preserveOriginalBytes)
    {
        return new TagDecodeState(new TagDecodeSource(payload), 0, payload.Length, 500, version, preserveOriginalBytes);
    }

    private static byte[] CreatePayload(uint typeSignature, byte[] body)
    {
        byte[] payload = new byte[8 + body.Length];
        WriteUInt32(payload, 0, typeSignature);
        Array.Copy(body, 0, payload, 8, body.Length);
        return payload;
    }

    private static byte[] CreateProfileWithPayload(byte[] payload)
    {
        int payloadEnd = 144 + payload.Length;
        int declaredSize = (payloadEnd + 3) & -4;
        byte[] profile = new byte[declaredSize];
        WriteUInt32(profile, 0, (uint)declaredSize);
        WriteUInt32(profile, 8, 0x05000000);
        WriteUInt32(profile, 12, IccProfileClassSignatures.Display);
        WriteUInt32(profile, 16, IccColorSpaceSignatures.Rgb);
        WriteUInt32(profile, 20, IccColorSpaceSignatures.Xyz);
        WriteUInt32(profile, 36, IccConstants.ProfileFileSignature);
        WriteUInt32(profile, 128, 1);
        WriteUInt32(profile, 132, IccTagSignatures.Copyright);
        WriteUInt32(profile, 136, 144);
        WriteUInt32(profile, 140, (uint)payload.Length);
        Array.Copy(payload, 0, profile, 144, payload.Length);
        return profile;
    }

    private static byte[] CompressWithFramework(byte[] source)
    {
        using MemoryStream destination = new MemoryStream();
        using (ZLibStream compression = new ZLibStream(destination, CompressionLevel.SmallestSize, true))
        {
            compression.Write(source);
        }

        return destination.ToArray();
    }

    private static byte[] AddLittleEndianBom(byte[] source)
    {
        byte[] result = new byte[source.Length + 2];
        result[0] = 0xff;
        result[1] = 0xfe;
        Array.Copy(source, 0, result, 2, source.Length);
        return result;
    }

    private static void WriteUInt16(byte[] destination, int offset, int value)
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
}
