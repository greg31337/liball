namespace png;

internal class PngConstants
{
    internal const int SignatureLength = 8;
    internal const byte SignatureByte0 = 137;
    internal const byte SignatureByte1 = 80;
    internal const byte SignatureByte2 = 78;
    internal const byte SignatureByte3 = 71;
    internal const byte SignatureByte4 = 13;
    internal const byte SignatureByte5 = 10;
    internal const byte SignatureByte6 = 26;
    internal const byte SignatureByte7 = 10;

    internal const string ChunkIHDR = "IHDR";
    internal const string ChunkPLTE = "PLTE";
    internal const string ChunkIDAT = "IDAT";
    internal const string ChunkIEND = "IEND";

    internal const string ChunkTransparency = "tRNS";
    internal const string ChunkChromaticities = "cHRM";
    internal const string ChunkGamma = "gAMA";
    internal const string ChunkIccProfile = "iCCP";
    internal const string ChunkSignificantBits = "sBIT";
    internal const string ChunkStandardRgb = "sRGB";
    internal const string ChunkCodingIndependentCodePoints = "cICP";
    internal const string ChunkMasteringDisplayColorVolume = "mDCV";
    internal const string ChunkContentLightLevelInformation = "cLLI";
    internal const string ChunkText = "tEXt";
    internal const string ChunkCompressedText = "zTXt";
    internal const string ChunkInternationalText = "iTXt";
    internal const string ChunkBackgroundColor = "bKGD";
    internal const string ChunkPaletteHistogram = "hIST";
    internal const string ChunkPhysicalPixels = "pHYs";
    internal const string ChunkSuggestedPalette = "sPLT";
    internal const string ChunkExif = "eXIf";
    internal const string ChunkTime = "tIME";
    internal const string ChunkAnimationControl = "acTL";
    internal const string ChunkFrameControl = "fcTL";
    internal const string ChunkFrameData = "fdAT";

    internal const int ColorTypeGrayscale = 0;
    internal const int ColorTypeTruecolor = 2;
    internal const int ColorTypeIndexedColor = 3;
    internal const int ColorTypeGrayscaleAlpha = 4;
    internal const int ColorTypeTruecolorAlpha = 6;

    internal const int BitDepth1 = 1;
    internal const int BitDepth2 = 2;
    internal const int BitDepth4 = 4;
    internal const int BitDepth8 = 8;
    internal const int BitDepth16 = 16;

    internal const int CompressionMethodDeflate = 0;
    internal const int FilterMethodAdaptive = 0;
    internal const int InterlaceMethodNone = 0;
    internal const int InterlaceMethodAdam7 = 1;

    internal const int FilterTypeNone = 0;
    internal const int FilterTypeSub = 1;
    internal const int FilterTypeUp = 2;
    internal const int FilterTypeAverage = 3;
    internal const int FilterTypePaeth = 4;

    internal const int ApngBlendOperationSource = 0;
    internal const int ApngBlendOperationOver = 1;
    internal const int ApngDisposeOperationNone = 0;
    internal const int ApngDisposeOperationBackground = 1;
    internal const int ApngDisposeOperationPrevious = 2;
}
