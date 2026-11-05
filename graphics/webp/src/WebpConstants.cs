namespace webp;

internal static class WebpConstants
{
    internal const string MimeType = "image/webp";
    internal const string FileExtension = ".webp";

    internal const string FourCcRiffString = "RIFF";
    internal const string FourCcWebpString = "WEBP";
    internal const string FourCcVp8String = "VP8 ";
    internal const string FourCcVp8LString = "VP8L";
    internal const string FourCcVp8XString = "VP8X";
    internal const string FourCcAlphaString = "ALPH";
    internal const string FourCcAnimString = "ANIM";
    internal const string FourCcAnimationFrameString = "ANMF";
    internal const string FourCcIccProfileString = "ICCP";
    internal const string FourCcExifString = "EXIF";
    internal const string FourCcXmpString = "XMP ";

    internal const uint FourCcRiff = 0x46464952U;
    internal const uint FourCcWebp = 0x50424557U;
    internal const uint FourCcVp8 = 0x20385056U;
    internal const uint FourCcVp8L = 0x4C385056U;
    internal const uint FourCcVp8X = 0x58385056U;
    internal const uint FourCcAlpha = 0x48504C41U;
    internal const uint FourCcAnim = 0x4D494E41U;
    internal const uint FourCcAnimationFrame = 0x464D4E41U;
    internal const uint FourCcIccProfile = 0x50434349U;
    internal const uint FourCcExif = 0x46495845U;
    internal const uint FourCcXmp = 0x20504D58U;

    internal const int RiffHeaderSize = 12;
    internal const int ChunkHeaderSize = 8;
    internal const int FourCcByteCount = 4;
    internal const long MaxRiffSizeField = 4294967286L;
    internal const long MaxRiffFileSize = 4294967294L;

    internal const int Vp8XChunkPayloadSize = 10;
    internal const int Vp8XCanvasDimensionFieldBytes = 3;
    internal const int Vp8XMaxCanvasDimension = 16777216;
    internal const uint Vp8XMaxCanvasPixels = 4294967295U;
    internal const byte Vp8XFeatureIccProfile = 0x20;
    internal const byte Vp8XFeatureAlpha = 0x10;
    internal const byte Vp8XFeatureExif = 0x08;
    internal const byte Vp8XFeatureXmp = 0x04;
    internal const byte Vp8XFeatureAnimation = 0x02;
    internal const byte Vp8XFeatureKnownMask = 0x3E;
    internal const byte Vp8XFeatureReservedMask = 0xC1;

    internal const int Vp8LossyMaxDimension = 16383;
    internal const int Vp8LossyFrameTagSize = 3;
    internal const int Vp8LossyKeyFrameHeaderSize = 10;
    internal const byte Vp8LossyKeyFrameSignature0 = 0x9D;
    internal const byte Vp8LossyKeyFrameSignature1 = 0x01;
    internal const byte Vp8LossyKeyFrameSignature2 = 0x2A;

    internal const int Vp8LMaxDimension = 16384;
    internal const int Vp8LHeaderSize = 5;
    internal const byte Vp8LSignature = 0x2F;
    internal const int Vp8LVersion = 0;
    internal const int Vp8LMaxColorCacheBits = 11;
    internal const int Vp8LMinColorCacheBits = 1;

    internal const byte AlphaCompressionMask = 0x03;
    internal const byte AlphaFilterMask = 0x0C;
    internal const byte AlphaPreprocessingMask = 0x30;
    internal const byte AlphaReservedMask = 0xC0;
    internal const int AlphaFilterShift = 2;
    internal const int AlphaPreprocessingShift = 4;
    internal const byte AlphaCompressionNone = 0;
    internal const byte AlphaCompressionLossless = 1;
    internal const byte AlphaFilterNone = 0;
    internal const byte AlphaFilterHorizontal = 1;
    internal const byte AlphaFilterVertical = 2;
    internal const byte AlphaFilterGradient = 3;

    internal const int AnimChunkPayloadSize = 6;
    internal const int AnimFrameHeaderSize = 16;
    internal const byte AnimFrameDisposeBackgroundFlag = 0x01;
    internal const byte AnimFrameNoBlendFlag = 0x02;
    internal const byte AnimFrameReservedMask = 0xFC;
    internal const ushort AnimInfiniteLoopCount = 0;
    internal const ushort AnimDefaultLoopCount = 1;
    internal const uint AnimDefaultBackgroundColorBgra = 0;

    internal const int DefaultQuality = 75;
    internal const int DefaultMethod = 4;
    internal const int DefaultFilterStrength = 60;
    internal const int DefaultFilterSharpness = 0;
    internal const bool DefaultAutoFilter = false;
    internal const int DefaultSpatialNoiseShaping = 50;
    internal const int DefaultTargetSize = 0;
    internal const double DefaultTargetPsnr = 0.0;
    internal const int DefaultPassCount = 1;
    internal const int DefaultTargetPassCount = 6;
    internal const int DefaultNearLosslessQuality = 100;
    internal const int DefaultAlphaQuality = 100;
    internal const WebpLossyPreset DefaultLossyPreset = WebpLossyPreset.Default;
    internal const WebpLossyPreprocessing DefaultLossyPreprocessing = WebpLossyPreprocessing.None;
    internal const WebpEncodeMode DefaultEncodeMode = WebpEncodeMode.Automatic;
    internal const WebpAlphaCompressionMethod DefaultAlphaCompression = WebpAlphaCompressionMethod.Lossless;
    internal const WebpAlphaFilterMode DefaultAlphaFilter = WebpAlphaFilterMode.Fast;
    internal const WebpLossyAlphaMode DefaultLossyAlphaMode = WebpLossyAlphaMode.Preserve;
    internal const uint DefaultAlphaBackgroundColorBgra = 0xFF000000U;
    internal const bool DefaultDeterministic = false;
    internal const WebpMetadataCopyPolicy DefaultMetadataCopyPolicy = WebpMetadataCopyPolicy.All;
    internal const WebpUnknownChunkPolicy DefaultUnknownChunkPolicy = WebpUnknownChunkPolicy.Preserve;

    internal const int DefaultMaxChunkCount = 65536;
    internal const int DefaultMaxFrameCount = 65536;
    internal const int DefaultMaxRecursionDepth = 64;
    internal const long DefaultMaxCanvasPixels = 268435456L;
    internal const long DefaultMaxTotalPixels = 268435456L;
    internal const long DefaultMaxMetadataBytes = 67108864L;
    internal const long DefaultMaxUnknownChunkBytes = 67108864L;
    internal const long DefaultMaxAggregateAllocationBytes = 1073741824L;
}
