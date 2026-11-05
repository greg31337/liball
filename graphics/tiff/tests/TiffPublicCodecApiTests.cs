using System;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffPublicCodecApiTests
{
    [TestCase(TiffCompression.None)]
    [TestCase(TiffCompression.PackBits)]
    [TestCase(TiffCompression.Deflate)]
    [TestCase(TiffCompression.AdobeDeflate)]
    [TestCase(TiffCompression.Lzw)]
    public void PublicCodecService_RoundTripsEveryByteOrientedBuiltIn(TiffCompression compression)
    {
        byte[] source = new byte[] { 10, 10, 10, 20, 21, 22, 22, 22, 30, 31, 32, 33 };
        TiffCodecContext context = CreateByteContext(compression, (ulong)source.Length);

        TiffCodecResult encoded = TiffApi.Codec.Encode(context, source);
        TiffCodecResult decoded = TiffApi.Codec.Decode(context, encoded.Bytes);
        TiffCompressionCapabilityResult capability = TiffApi.Codec.GetCompressionCapability(compression);

        Assert.That(encoded.Success, Is.True, compression.ToString());
        Assert.That(decoded.Success, Is.True, compression.ToString());
        Assert.That(decoded.Bytes, Is.EqualTo(source), compression.ToString());
        Assert.That(capability.Success, Is.True, compression.ToString());
        Assert.That(capability.CanEncode, Is.True, compression.ToString());
        Assert.That(capability.CanDecode, Is.True, compression.ToString());
        Assert.That(capability.EncodeSource, Is.EqualTo(TiffCodecCapabilitySource.BuiltIn), compression.ToString());
        Assert.That(capability.DecodeSource, Is.EqualTo(TiffCodecCapabilitySource.BuiltIn), compression.ToString());
        Assert.That(TiffApi.Codec.IsCompressionSupported(compression), Is.True, compression.ToString());
    }

    [Test]
    public void PublicCodecService_RoundTripsEveryBilevelBuiltInAgainstKnownTiffVectors()
    {
        TiffCompression[] compressions = new TiffCompression[]
        {
            TiffCompression.CcittRle,
            TiffCompression.CcittGroup3,
            TiffCompression.CcittGroup4
        };
        byte[][] encodedVectors = new byte[][]
        {
            new byte[] { 142, 0 },
            new byte[] { 0, 24, 224 },
            new byte[] { 49, 192, 4, 0, 64 }
        };
        byte[] decodedVector = new byte[] { 24 };
        int index = 0;
        while (index < compressions.Length)
        {
            TiffCodecContext context = CreateBilevelContext(compressions[index], 8, 1, 1);

            TiffCodecResult decoded = TiffApi.Codec.Decode(context, encodedVectors[index]);
            TiffCodecResult encoded = TiffApi.Codec.Encode(context, decodedVector);

            Assert.That(decoded.Success, Is.True, compressions[index].ToString());
            Assert.That(decoded.Bytes, Is.EqualTo(decodedVector), compressions[index].ToString());
            Assert.That(encoded.Success, Is.True, compressions[index].ToString());
            Assert.That(encoded.Bytes, Is.EqualTo(encodedVectors[index]), compressions[index].ToString());
            Assert.That(TiffApi.Codec.CanEncodeCompression(compressions[index]), Is.True);
            Assert.That(TiffApi.Codec.CanDecodeCompression(compressions[index]), Is.True);
            index = index + 1;
        }
    }

    [Test]
    public void PublicCodecService_DecodesKnownPackBitsZlibAndLzwSequences()
    {
        TiffCodecResult packBits = TiffApi.Codec.Decode(
            CreateByteContext(TiffCompression.PackBits, 5),
            new byte[] { 1, 65, 66, 254, 67 });
        TiffCodecResult zlib = TiffApi.Codec.Decode(
            CreateByteContext(TiffCompression.Deflate, 6),
            new byte[] { 120, 156, 99, 100, 98, 102, 97, 101, 3, 0, 0, 62, 0, 22 });
        TiffCodecResult lzw = TiffApi.Codec.Decode(
            CreateByteContext(TiffCompression.Lzw, 3),
            new byte[] { 128, 16, 72, 68, 56, 8 });
        TiffCodecResult packBitsEncoded = TiffApi.Codec.Encode(
            CreateByteContext(TiffCompression.PackBits, 6),
            new byte[] { 10, 11, 7, 7, 7, 12 });
        TiffCodecResult lzwEncoded = TiffApi.Codec.Encode(
            CreateByteContext(TiffCompression.Lzw, 3),
            new byte[] { 65, 66, 67 });

        Assert.That(packBits.Success, Is.True);
        Assert.That(packBits.Bytes, Is.EqualTo(new byte[] { 65, 66, 67, 67, 67 }));
        Assert.That(zlib.Success, Is.True);
        Assert.That(zlib.Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
        Assert.That(lzw.Success, Is.True);
        Assert.That(lzw.Bytes, Is.EqualTo(new byte[] { 65, 66, 67 }));
        Assert.That(packBitsEncoded.Success, Is.True);
        Assert.That(packBitsEncoded.Bytes, Is.EqualTo(new byte[] { 1, 10, 11, 254, 7, 0, 12 }));
        Assert.That(lzwEncoded.Success, Is.True);
        Assert.That(lzwEncoded.Bytes, Is.EqualTo(new byte[] { 128, 16, 72, 68, 56, 8 }));
    }

    [TestCase(TiffCompression.Lzw, TiffByteOrder.LittleEndian)]
    [TestCase(TiffCompression.Lzw, TiffByteOrder.BigEndian)]
    [TestCase(TiffCompression.Deflate, TiffByteOrder.LittleEndian)]
    [TestCase(TiffCompression.Deflate, TiffByteOrder.BigEndian)]
    public void HorizontalIntegerPredictor_RoundTripsSixteenBitRowsThroughPublicPipeline(TiffCompression compression, TiffByteOrder byteOrder)
    {
        byte[] source = new byte[]
        {
            2, 1, 4, 3, 6, 5,
            8, 7, 10, 9, 12, 11
        };
        TiffCodecContext context = CreateByteContext(compression, (ulong)source.Length);
        context.Info.Width = 3;
        context.Info.Length = 2;
        context.Info.RowsPerStrip = 2;
        context.Info.BitsPerSample = new int[] { 16 };
        context.Info.Predictor = 2;
        context.ByteOrder = byteOrder;

        TiffCodecResult encoded = TiffApi.Codec.Encode(context, source);
        TiffCodecResult decoded = TiffApi.Codec.Decode(context, encoded.Bytes);

        Assert.That(encoded.Success, Is.True);
        Assert.That(encoded.Bytes, Is.Not.EqualTo(source));
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(source));
    }

    [TestCase(TiffByteOrder.LittleEndian)]
    [TestCase(TiffByteOrder.BigEndian)]
    public void FloatingPointPredictor_RoundTripsThirtyTwoBitSamplesThroughPublicPipeline(TiffByteOrder byteOrder)
    {
        byte[] source = new byte[]
        {
            0, 0, 128, 63,
            0, 0, 0, 64,
            0, 0, 128, 64
        };
        TiffCodecContext context = CreateByteContext(TiffCompression.Deflate, (ulong)source.Length);
        context.Info.Width = 3;
        context.Info.Length = 1;
        context.Info.RowsPerStrip = 1;
        context.Info.BitsPerSample = new int[] { 32 };
        context.Info.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.IeeeFloatingPoint };
        context.Info.Predictor = 3;
        context.ByteOrder = byteOrder;

        TiffCodecResult encoded = TiffApi.Codec.Encode(context, source);
        TiffCodecResult decoded = TiffApi.Codec.Decode(context, encoded.Bytes);

        Assert.That(encoded.Success, Is.True);
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(source));
    }

    [Test]
    public void FillOrderAndCcittOptions_AreAppliedByPublicCodecPipeline()
    {
        TiffCodecContext packBitsContext = CreateByteContext(TiffCompression.PackBits, 1);
        packBitsContext.Info.FillOrder = 2;
        TiffCodecResult packBitsEncoded = TiffApi.Codec.Encode(packBitsContext, new byte[] { 18 });
        TiffCodecResult packBitsDecoded = TiffApi.Codec.Decode(packBitsContext, packBitsEncoded.Bytes);

        TiffCodecContext rleContext = CreateBilevelContext(TiffCompression.CcittRle, 8, 1, 1);
        rleContext.Info.FillOrder = 2;
        TiffCodecResult rleEncoded = TiffApi.Codec.Encode(rleContext, new byte[] { 24 });
        TiffCodecResult rleDecoded = TiffApi.Codec.Decode(rleContext, rleEncoded.Bytes);

        TiffCodecContext group3Context = CreateBilevelContext(TiffCompression.CcittGroup3, 8, 1, 1);
        group3Context.Info.T4Options = 4;
        TiffCodecResult group3Encoded = TiffApi.Codec.Encode(group3Context, new byte[] { 24 });
        TiffCodecResult group3Decoded = TiffApi.Codec.Decode(group3Context, group3Encoded.Bytes);

        TiffCodecContext invalidGroup4Context = CreateBilevelContext(TiffCompression.CcittGroup4, 8, 1, 1);
        invalidGroup4Context.Info.T6Options = 1;
        TiffCodecResult invalidGroup4 = TiffApi.Codec.Decode(invalidGroup4Context, new byte[] { 128, 8, 0, 128 });

        Assert.That(packBitsEncoded.Bytes, Is.EqualTo(new byte[] { 0, 72 }));
        Assert.That(packBitsDecoded.Bytes, Is.EqualTo(new byte[] { 18 }));
        Assert.That(rleEncoded.Bytes, Is.EqualTo(new byte[] { 113, 0 }));
        Assert.That(rleDecoded.Bytes, Is.EqualTo(new byte[] { 24 }));
        Assert.That(group3Encoded.Bytes, Is.EqualTo(new byte[] { 0, 1, 142, 0 }));
        Assert.That(group3Decoded.Bytes, Is.EqualTo(new byte[] { 24 }));
        Assert.That(invalidGroup4.Success, Is.False);
        Assert.That(invalidGroup4.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void PublicCodecService_ReportsMalformedStreamsForEveryBuiltInDecoder()
    {
        TiffCodecContext[] contexts = new TiffCodecContext[]
        {
            CreateByteContext(TiffCompression.None, 2),
            CreateByteContext(TiffCompression.PackBits, 4),
            CreateByteContext(TiffCompression.Deflate, 6),
            CreateByteContext(TiffCompression.AdobeDeflate, 6),
            CreateByteContext(TiffCompression.Lzw, 1),
            CreateBilevelContext(TiffCompression.CcittRle, 4, 1, 1),
            CreateBilevelContext(TiffCompression.CcittGroup3, 8, 1, 1),
            CreateBilevelContext(TiffCompression.CcittGroup4, 8, 1, 1)
        };
        byte[][] malformed = new byte[][]
        {
            new byte[] { 1 },
            new byte[] { 1, 65 },
            new byte[] { 120, 156, 99 },
            new byte[] { 120, 156, 99 },
            new byte[] { 0 },
            new byte[] { 0 },
            new byte[] { 0 },
            new byte[] { 0 }
        };

        int index = 0;
        while (index < contexts.Length)
        {
            TiffCodecResult result = TiffApi.Codec.Decode(contexts[index], malformed[index]);

            Assert.That(result.Success, Is.False, contexts[index].Info.Compression.ToString());
            Assert.That(result.Diagnostics.HasErrors(), Is.True, contexts[index].Info.Compression.ToString());
            index = index + 1;
        }
    }

    [Test]
    public void PublicCodecService_EnforcesDecodedSizeExpansionRatioAndAllocationLimits()
    {
        TiffCodecContext decodedLimitContext = CreateByteContext(TiffCompression.None, 6);
        decodedLimitContext.Limits.MaximumDecodedBytes = 5;
        TiffCodecResult decodedLimit = TiffApi.Codec.Decode(decodedLimitContext, new byte[] { 1, 2, 3, 4, 5, 6 });

        TiffCodecContext ratioContext = CreateByteContext(TiffCompression.PackBits, 4);
        ratioContext.Limits.MaximumCodecExpansionRatio = 1;
        TiffCodecResult ratioLimit = TiffApi.Codec.Decode(ratioContext, new byte[] { 253, 6 });

        TiffCodecContext allocationContext = CreateByteContext(TiffCompression.Deflate, 16);
        allocationContext.Limits.MaximumAllocationBytes = 28;
        TiffCodecResult allocationLimit = TiffApi.Codec.Encode(allocationContext, new byte[16]);

        Assert.That(decodedLimit.Success, Is.False);
        Assert.That(decodedLimit.Diagnostics.ContainsCode(TiffErrorCode.CodecExpansionLimitExceeded), Is.True);
        Assert.That(ratioLimit.Success, Is.False);
        Assert.That(ratioLimit.Diagnostics.ContainsCode(TiffErrorCode.CodecExpansionLimitExceeded), Is.True);
        Assert.That(allocationLimit.Success, Is.False);
        Assert.That(allocationLimit.Diagnostics.ContainsCode(TiffErrorCode.AllocationLimitExceeded), Is.True);
    }

    [Test]
    public void UnsupportedCompression_RemainsAvailableThroughPublicRawPassThrough()
    {
        byte[] rawBytes = new byte[] { 255, 216, 1, 2, 255, 217 };
        TiffCodecContext context = CreateByteContext(TiffCompression.Jpeg, (ulong)rawBytes.Length);
        TiffCodecResult unsupportedDecode = TiffApi.Codec.Decode(context, rawBytes);
        TiffCodecResult unsupportedEncode = TiffApi.Codec.Encode(context, rawBytes);
        TiffCompressionCapabilityResult capability = TiffApi.Codec.GetCompressionCapability(TiffCompression.Jpeg);

        TiffWriteOptions writeOptions = new TiffWriteOptions();
        writeOptions.ValidateBeforeWrite = false;
        TiffWriterImageConfiguration configuration = new TiffWriterImageConfiguration();
        configuration.Width = 3;
        configuration.Length = 1;
        configuration.Compression = TiffCompression.Jpeg;
        TiffWriter writer = TiffApi.Factory.CreateWriter(writeOptions);
        int imageIndex = writer.AddConfiguredImage(configuration);
        TiffRawStripWriteResult write = writer.WriteRawStrip(imageIndex, 0, rawBytes);
        TiffDiagnostics flush = writer.Flush();
        TiffImage image = TiffApi.Factory.OpenReadBytes(writer.GetFileBytes(), new TiffReadOptions()).GetImage(0);
        TiffRawStripReadResult raw = image.ReadRawStrip(0);
        TiffRawStripReadResult decoded = image.ReadEncodedStrip(0);

        Assert.That(capability.CanDecode, Is.False);
        Assert.That(capability.CanEncode, Is.False);
        Assert.That(capability.CanPassThroughRaw, Is.True);
        Assert.That(TiffApi.Codec.CanPassThroughRawCompression(TiffCompression.Jpeg), Is.True);
        Assert.That(unsupportedDecode.Success, Is.False);
        Assert.That(unsupportedDecode.Bytes, Is.EqualTo(rawBytes));
        Assert.That(unsupportedDecode.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedCompression), Is.True);
        Assert.That(unsupportedEncode.Success, Is.False);
        Assert.That(unsupportedEncode.Bytes, Is.EqualTo(rawBytes));
        Assert.That(write.Success, Is.True);
        Assert.That(flush.HasErrors(), Is.False);
        Assert.That(raw.Success, Is.True);
        Assert.That(raw.Bytes, Is.EqualTo(rawBytes));
        Assert.That(decoded.Success, Is.False);
        Assert.That(decoded.Bytes, Is.EqualTo(rawBytes));
        Assert.That(decoded.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedCompression), Is.True);
    }

    [Test]
    public void ApplicationCodec_UsesPublicResultModelForEncodeDecodeAndCapabilities()
    {
        TiffCompression compression = (TiffCompression)65000;
        TiffCodecContext context = CreateByteContext(compression, 4);
        XorCodec codec = new XorCodec(compression);
        byte[] source = new byte[] { 1, 2, 3, 4 };

        TiffCodecResult encoded = TiffApi.Codec.EncodeWithCompressionCodec(context, source, codec);
        TiffCodecResult decoded = TiffApi.Codec.DecodeWithCompressionCodec(context, encoded.Bytes, codec);
        TiffCompressionCapabilityResult capability = TiffApi.Codec.GetCompressionCapabilityWithCodec(compression, codec);

        Assert.That(encoded.Success, Is.True);
        Assert.That(encoded.Bytes, Is.EqualTo(new byte[] { 254, 253, 252, 251 }));
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(source));
        Assert.That(codec.EncodeCallCount, Is.EqualTo(1));
        Assert.That(codec.DecodeCallCount, Is.EqualTo(1));
        Assert.That(capability.CanEncode, Is.True);
        Assert.That(capability.CanDecode, Is.True);
        Assert.That(capability.CanPassThroughRaw, Is.False);
        Assert.That(capability.EncodeSource, Is.EqualTo(TiffCodecCapabilitySource.ApplicationProvided));
        Assert.That(capability.DecodeSource, Is.EqualTo(TiffCodecCapabilitySource.ApplicationProvided));
        Assert.That(TiffApi.Codec.IsCompressionSupportedWithCodec(compression, codec), Is.True);
    }

    static TiffCodecContext CreateByteContext(TiffCompression compression, ulong expectedDecodedByteCount)
    {
        TiffCodecContext context = new TiffCodecContext();
        context.Info.Width = expectedDecodedByteCount;
        context.Info.Length = 1;
        context.Info.RowsPerStrip = 1;
        context.Info.SamplesPerPixel = 1;
        context.Info.BitsPerSample = new int[] { 8 };
        context.Info.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger };
        context.Info.Compression = compression;
        context.ExpectedDecodedByteCount = expectedDecodedByteCount;
        return context;
    }

    static TiffCodecContext CreateBilevelContext(TiffCompression compression, ulong width, ulong length, ulong expectedDecodedByteCount)
    {
        TiffCodecContext context = new TiffCodecContext();
        context.Info.Width = width;
        context.Info.Length = length;
        context.Info.RowsPerStrip = length;
        context.Info.SamplesPerPixel = 1;
        context.Info.BitsPerSample = new int[] { 1 };
        context.Info.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger };
        context.Info.Photometric = TiffPhotometric.WhiteIsZero;
        context.Info.Compression = compression;
        context.ExpectedDecodedByteCount = expectedDecodedByteCount;
        return context;
    }

    public class XorCodec : ITiffCompressionCodec
    {
        public TiffCompression Compression;
        public int EncodeCallCount;
        public int DecodeCallCount;

        public XorCodec(TiffCompression compression)
        {
            Compression = compression;
            EncodeCallCount = 0;
            DecodeCallCount = 0;
        }

        public bool CanDecode(TiffCompression compression)
        {
            return compression == Compression;
        }

        public bool CanEncode(TiffCompression compression)
        {
            return compression == Compression;
        }

        public TiffCodecResult Decode(TiffCodecContext context, ReadOnlySpan<byte> source)
        {
            DecodeCallCount = DecodeCallCount + 1;
            return CreateSuccessResult(Transform(source));
        }

        public TiffCodecResult Encode(TiffCodecContext context, ReadOnlySpan<byte> source)
        {
            EncodeCallCount = EncodeCallCount + 1;
            return CreateSuccessResult(Transform(source));
        }

        public byte[] Transform(ReadOnlySpan<byte> source)
        {
            byte[] result = source.ToArray();
            int index = 0;
            while (index < result.Length)
            {
                result[index] = (byte)(result[index] ^ 255);
                index = index + 1;
            }

            return result;
        }

        public TiffCodecResult CreateSuccessResult(byte[] bytes)
        {
            TiffCodecResult result = new TiffCodecResult();
            result.Success = true;
            result.Bytes = bytes;
            result.Diagnostics = new TiffDiagnostics();
            return result;
        }
    }
}
