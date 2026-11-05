using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffLzwCodecTests
{
    [Test]
    public void Decode_DecodesLiteralCodes()
    {
        TiffCodecContext context = CreateContext(3, new TiffLimits());
        byte[] source = new byte[] { 128, 16, 72, 68, 56, 8 };

        TiffCodecResult result = TiffLzwCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 65, 66, 67 }));
    }

    [Test]
    public void Decode_HandlesSpecialNextCodeCase()
    {
        TiffCodecContext context = CreateContext(7, new TiffLimits());
        byte[] source = new byte[] { 128, 24, 76, 80, 40, 36, 4 };

        TiffCodecResult result = TiffLzwCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 97, 98, 97, 98, 97, 98, 97 }));
    }

    [Test]
    public void Decode_DispatchesLzwThroughCodec()
    {
        TiffCodecContext context = CreateContext(3, new TiffLimits());
        byte[] source = new byte[] { 128, 16, 96, 80, 16 };

        TiffCodecResult result = TiffCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 65, 65, 65 }));
    }

    [Test]
    public void Decode_HandlesCodeSizeGrowthToTenBits()
    {
        byte[] expectedBytes = CreateByteSequence(255);
        byte[] source = CreateWidthTransitionStream();
        TiffCodecContext context = CreateContext(255, new TiffLimits());

        TiffCodecResult result = TiffLzwCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(expectedBytes));
    }

    [Test]
    public void Decode_ReportsMissingInitialClearCode()
    {
        TiffCodecContext context = CreateContext(1, new TiffLimits());
        byte[] source = new byte[] { 32, 192, 64 };

        TiffCodecResult result = TiffLzwCodec.Decode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void Decode_ReportsInvalidDictionaryCode()
    {
        TiffCodecContext context = CreateContext(1, new TiffLimits());
        byte[] source = new byte[] { 128, 75, 32, 32 };

        TiffCodecResult result = TiffLzwCodec.Decode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void Decode_ReportsOutputBeyondExpectedDecodedByteCount()
    {
        TiffCodecContext context = CreateContext(2, new TiffLimits());
        byte[] source = new byte[] { 128, 16, 72, 68, 56, 8 };

        TiffCodecResult result = TiffLzwCodec.Decode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void Decode_ReportsMissingEndOfInformationCode()
    {
        TiffCodecContext context = CreateContext(1, new TiffLimits());
        byte[] source = new byte[] { 128, 16, 64 };

        TiffCodecResult result = TiffLzwCodec.Decode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.TruncatedStream), Is.True);
    }

    [Test]
    public void Decode_EnforcesDecodedByteLimit()
    {
        TiffLimits limits = new TiffLimits();
        limits.MaximumDecodedBytes = 2;
        TiffCodecContext context = CreateContext(3, limits);
        byte[] source = new byte[] { 128, 16, 72, 68, 56, 8 };

        TiffCodecResult result = TiffLzwCodec.Decode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.CodecExpansionLimitExceeded), Is.True);
    }

    [Test]
    public void Encode_EncodesLiteralCodes()
    {
        TiffCodecContext context = CreateContext(3, new TiffLimits());

        TiffCodecResult result = TiffLzwCodec.Encode(context, new byte[] { 65, 66, 67 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 128, 16, 72, 68, 56, 8 }));
    }

    [Test]
    public void Encode_EncodesSpecialNextCodeCase()
    {
        TiffCodecContext context = CreateContext(7, new TiffLimits());
        byte[] source = new byte[] { 97, 98, 97, 98, 97, 98, 97 };

        TiffCodecResult result = TiffLzwCodec.Encode(context, source);
        TiffCodecResult decoded = TiffLzwCodec.Decode(context, result.Bytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 128, 24, 76, 80, 40, 36, 4 }));
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(source));
    }

    [Test]
    public void Encode_DispatchesLzwThroughCodec()
    {
        TiffCodecContext context = CreateContext(3, new TiffLimits());

        TiffCodecResult result = TiffCodec.Encode(context, new byte[] { 65, 65, 65 });
        TiffCodecResult decoded = TiffCodec.Decode(context, result.Bytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 128, 16, 96, 80, 16 }));
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(new byte[] { 65, 65, 65 }));
    }

    [Test]
    public void Encode_HandlesCodeSizeGrowthToTenBits()
    {
        byte[] source = CreateByteSequence(600);
        TiffCodecContext context = CreateContext(600, new TiffLimits());

        TiffCodecResult result = TiffLzwCodec.Encode(context, source);
        TiffCodecResult decoded = TiffLzwCodec.Decode(context, result.Bytes);

        Assert.That(result.Success, Is.True);
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(source));
    }

    [Test]
    public void Encode_EncodesEmptyInputAsClearAndEndOfInformation()
    {
        TiffCodecContext context = CreateContext(0, new TiffLimits());

        TiffCodecResult result = TiffLzwCodec.Encode(context, new byte[0]);
        TiffCodecResult decoded = TiffLzwCodec.Decode(context, result.Bytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 128, 64, 64 }));
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(new byte[0]));
    }

    [Test]
    public void Encode_RejectsSourceByteCountMismatch()
    {
        TiffCodecContext context = CreateContext(4, new TiffLimits());

        TiffCodecResult result = TiffLzwCodec.Encode(context, new byte[] { 1, 2, 3 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void Encode_EnforcesAllocationLimit()
    {
        TiffLimits limits = new TiffLimits();
        limits.MaximumAllocationBytes = 16;
        TiffCodecContext context = CreateContext(16, limits);
        byte[] source = new byte[16];

        TiffCodecResult result = TiffLzwCodec.Encode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.AllocationLimitExceeded), Is.True);
    }

    static TiffCodecContext CreateContext(ulong expectedDecodedByteCount, TiffLimits limits)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Compression = TiffCompression.Lzw;
        return TiffCodecContext.Create(info, false, 0, expectedDecodedByteCount, limits);
    }

    static byte[] CreateByteSequence(int length)
    {
        byte[] bytes = new byte[length];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)index;
            index = index + 1;
        }

        return bytes;
    }

    static byte[] CreateWidthTransitionStream()
    {
        List<int> codes = new List<int>();
        List<int> codeSizes = new List<int>();
        int codeSize = 9;
        int nextCode = 258;
        bool hasPreviousCode = false;

        codes.Add(256);
        codeSizes.Add(codeSize);

        int value = 0;
        while (value < 255)
        {
            codes.Add(value);
            codeSizes.Add(codeSize);
            if (hasPreviousCode)
            {
                nextCode = nextCode + 1;
                if (nextCode == 1 << codeSize)
                {
                    codeSize = codeSize + 1;
                }
            }

            hasPreviousCode = true;
            value = value + 1;
        }

        codes.Add(257);
        codeSizes.Add(codeSize);
        return PackCodes(codes, codeSizes);
    }

    static byte[] PackCodes(List<int> codes, List<int> codeSizes)
    {
        List<byte> bytes = new List<byte>();
        int pendingByte = 0;
        int pendingBitCount = 0;
        int codeIndex = 0;
        while (codeIndex < codes.Count)
        {
            int code = codes[codeIndex];
            int codeSize = codeSizes[codeIndex];
            int bitIndex = codeSize - 1;
            while (bitIndex >= 0)
            {
                pendingByte = pendingByte * 2 + ((code >> bitIndex) & 1);
                pendingBitCount = pendingBitCount + 1;
                if (pendingBitCount == 8)
                {
                    bytes.Add((byte)pendingByte);
                    pendingByte = 0;
                    pendingBitCount = 0;
                }

                bitIndex = bitIndex - 1;
            }

            codeIndex = codeIndex + 1;
        }

        if (pendingBitCount > 0)
        {
            while (pendingBitCount < 8)
            {
                pendingByte = pendingByte * 2;
                pendingBitCount = pendingBitCount + 1;
            }

            bytes.Add((byte)pendingByte);
        }

        return bytes.ToArray();
    }
}
