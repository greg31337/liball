using System;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffCompressionCapabilityTests
{
    [Test]
    public void GetCompressionCapability_ReportsEveryBuiltInCodecAsBidirectional()
    {
        ITiffCodecService service = new TiffCodecService();
        TiffCompression[] compressions = new TiffCompression[]
        {
            TiffCompression.None,
            TiffCompression.PackBits,
            TiffCompression.Deflate,
            TiffCompression.AdobeDeflate,
            TiffCompression.Lzw,
            TiffCompression.CcittRle,
            TiffCompression.CcittGroup3,
            TiffCompression.CcittGroup4
        };

        foreach (TiffCompression compression in compressions)
        {
            TiffCompressionCapabilityResult result = service.GetCompressionCapability(compression);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True, compression.ToString());
                Assert.That(result.Compression, Is.EqualTo(compression), compression.ToString());
                Assert.That(result.CanDecode, Is.True, compression.ToString());
                Assert.That(result.CanEncode, Is.True, compression.ToString());
                Assert.That(result.CanPassThroughRaw, Is.False, compression.ToString());
                Assert.That(result.DecodeSource, Is.EqualTo(TiffCodecCapabilitySource.BuiltIn), compression.ToString());
                Assert.That(result.EncodeSource, Is.EqualTo(TiffCodecCapabilitySource.BuiltIn), compression.ToString());
                Assert.That(result.Diagnostics.Count(), Is.EqualTo(0), compression.ToString());
            });
        }
    }

    [Test]
    public void GetCompressionCapability_ReportsLegacyAndJpegCodecsAsUnavailableRawPassThrough()
    {
        ITiffCodecService service = new TiffCodecService();
        TiffCompression[] compressions = new TiffCompression[]
        {
            TiffCompression.CcittRleW,
            TiffCompression.ThunderScan,
            TiffCompression.NeXt,
            TiffCompression.OldJpeg,
            TiffCompression.Jpeg
        };
        string[] names = new string[]
        {
            "CCITT word-aligned Modified Huffman",
            "ThunderScan",
            "NeXT 2-bit",
            "old-style JPEG",
            "JPEG"
        };

        int index = 0;
        while (index < compressions.Length)
        {
            TiffCompressionCapabilityResult result = service.GetCompressionCapability(compressions[index]);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.CanDecode, Is.False);
                Assert.That(result.CanEncode, Is.False);
                Assert.That(result.CanPassThroughRaw, Is.True);
                Assert.That(result.DecodeSource, Is.EqualTo(TiffCodecCapabilitySource.Unavailable));
                Assert.That(result.EncodeSource, Is.EqualTo(TiffCodecCapabilitySource.Unavailable));
                Assert.That(result.Diagnostics.WarningCount(), Is.EqualTo(2));
                Assert.That(result.Diagnostics.Get(0).Message, Does.Contain(names[index]));
                Assert.That(result.Diagnostics.Get(0).Message, Does.Contain("decoder"));
                Assert.That(result.Diagnostics.Get(1).Message, Does.Contain("encoder"));
            });

            index = index + 1;
        }
    }

    [Test]
    public void GetCompressionCapability_ReportsRegisteredAndPrivateExternalIdentifiers()
    {
        ITiffCodecService service = new TiffCodecService();
        TiffCompression[] compressions = new TiffCompression[]
        {
            TiffCompression.T85,
            TiffCompression.T43,
            TiffCompression.It8Ctpad,
            TiffCompression.It8Lw,
            TiffCompression.It8Mp,
            TiffCompression.It8Bl,
            TiffCompression.PixarFilm,
            TiffCompression.PixarLog,
            TiffCompression.Dcs,
            TiffCompression.Jbig,
            TiffCompression.Sgilog,
            TiffCompression.Sgilog24,
            TiffCompression.Jpeg2000,
            TiffCompression.Lzma,
            TiffCompression.Zstd,
            TiffCompression.WebP,
            TiffCompression.Jxl,
            (TiffCompression)65000
        };

        foreach (TiffCompression compression in compressions)
        {
            TiffCompressionCapabilityResult result = service.GetCompressionCapability(compression);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True, compression.ToString());
                Assert.That(result.CanDecode, Is.False, compression.ToString());
                Assert.That(result.CanEncode, Is.False, compression.ToString());
                Assert.That(result.CanPassThroughRaw, Is.True, compression.ToString());
                Assert.That(result.Diagnostics.WarningCount(), Is.EqualTo(2), compression.ToString());
                Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedCompression), Is.True, compression.ToString());
            });
        }

        TiffCompressionCapabilityResult privateResult = service.GetCompressionCapability((TiffCompression)65000);
        Assert.That(privateResult.Diagnostics.Get(0).Message, Does.Contain("external/private"));
    }

    [Test]
    public void GetCompressionCapabilityWithCodec_ReportsEachApplicationProvidedDirection()
    {
        ITiffCodecService service = new TiffCodecService();
        CapabilityCodec decodeOnly = new CapabilityCodec(TiffCompression.Jpeg, true, false);
        CapabilityCodec bidirectional = new CapabilityCodec(TiffCompression.Jpeg, true, true);
        CapabilityCodec builtInOverride = new CapabilityCodec(TiffCompression.None, true, true);

        TiffCompressionCapabilityResult partial = service.GetCompressionCapabilityWithCodec(TiffCompression.Jpeg, decodeOnly);
        TiffCompressionCapabilityResult full = service.GetCompressionCapabilityWithCodec(TiffCompression.Jpeg, bidirectional);
        TiffCompressionCapabilityResult overridden = service.GetCompressionCapabilityWithCodec(TiffCompression.None, builtInOverride);

        Assert.Multiple(() =>
        {
            Assert.That(partial.Success, Is.True);
            Assert.That(partial.CanDecode, Is.True);
            Assert.That(partial.CanEncode, Is.False);
            Assert.That(partial.CanPassThroughRaw, Is.True);
            Assert.That(partial.DecodeSource, Is.EqualTo(TiffCodecCapabilitySource.ApplicationProvided));
            Assert.That(partial.EncodeSource, Is.EqualTo(TiffCodecCapabilitySource.Unavailable));
            Assert.That(partial.Diagnostics.WarningCount(), Is.EqualTo(1));
            Assert.That(partial.Diagnostics.Get(0).Message, Does.Contain("encoder"));
            Assert.That(full.CanDecode, Is.True);
            Assert.That(full.CanEncode, Is.True);
            Assert.That(full.CanPassThroughRaw, Is.False);
            Assert.That(full.DecodeSource, Is.EqualTo(TiffCodecCapabilitySource.ApplicationProvided));
            Assert.That(full.EncodeSource, Is.EqualTo(TiffCodecCapabilitySource.ApplicationProvided));
            Assert.That(full.Diagnostics.Count(), Is.EqualTo(0));
            Assert.That(overridden.DecodeSource, Is.EqualTo(TiffCodecCapabilitySource.ApplicationProvided));
            Assert.That(overridden.EncodeSource, Is.EqualTo(TiffCodecCapabilitySource.ApplicationProvided));
        });
    }

    [Test]
    public void GetCompressionCapability_RejectsUnknownAndOutOfRangeIdentifiers()
    {
        ITiffCodecService service = new TiffCodecService();

        TiffCompressionCapabilityResult unknown = service.GetCompressionCapability(TiffCompression.Unknown);
        TiffCompressionCapabilityResult negative = service.GetCompressionCapability((TiffCompression)(-1));
        TiffCompressionCapabilityResult oversized = service.GetCompressionCapability((TiffCompression)65536);

        Assert.Multiple(() =>
        {
            Assert.That(unknown.Success, Is.False);
            Assert.That(unknown.CanPassThroughRaw, Is.False);
            Assert.That(unknown.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
            Assert.That(negative.Success, Is.False);
            Assert.That(negative.CanPassThroughRaw, Is.False);
            Assert.That(oversized.Success, Is.False);
            Assert.That(oversized.CanPassThroughRaw, Is.False);
            Assert.That(service.CanPassThroughRawCompression((TiffCompression)(-1)), Is.False);
            Assert.That(service.CanPassThroughRawCompression((TiffCompression)65536), Is.False);
        });
    }

    [Test]
    public void DecodeUnsupportedCompression_UsesNamedDirectionalDiagnostic()
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Compression = TiffCompression.CcittRleW;
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 1, new TiffLimits());

        TiffCodecResult result = TiffCodec.Decode(context, new byte[] { 1 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Get(0).Message, Does.Contain("CCITT word-aligned Modified Huffman"));
        Assert.That(result.Diagnostics.Get(0).Message, Does.Contain("decoder"));
    }

    class CapabilityCodec : ITiffCompressionCodec
    {
        public TiffCompression Compression;
        public bool DecodeSupported;
        public bool EncodeSupported;

        public CapabilityCodec(TiffCompression compression, bool decodeSupported, bool encodeSupported)
        {
            Compression = compression;
            DecodeSupported = decodeSupported;
            EncodeSupported = encodeSupported;
        }

        public bool CanDecode(TiffCompression compression)
        {
            return DecodeSupported && compression == Compression;
        }

        public bool CanEncode(TiffCompression compression)
        {
            return EncodeSupported && compression == Compression;
        }

        public TiffCodecResult Decode(TiffCodecContext context, ReadOnlySpan<byte> source)
        {
            return TiffCodecResult.Create(source.ToArray(), new TiffDiagnostics());
        }

        public TiffCodecResult Encode(TiffCodecContext context, ReadOnlySpan<byte> source)
        {
            return TiffCodecResult.Create(source.ToArray(), new TiffDiagnostics());
        }
    }
}
