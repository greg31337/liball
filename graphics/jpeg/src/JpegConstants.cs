namespace jpeg;

internal class JpegConstants
{
    internal const int MarkerPrefix = 0xFF;
    internal const int MarkerStuffedByte = 0x00;
    internal const int MarkerPrefixShift = 8;
    internal const int MarkerSegmentLengthBytes = 2;
    internal const int MinimumMarkerSegmentLength = 2;
    internal const int MaximumMarkerSegmentLength = 65535;
    internal const int MaximumMarkerPayloadLength = 65533;

    internal const int MarkerTemporary = 0xFF01;
    internal const int MarkerStartOfFrame0 = 0xFFC0;
    internal const int MarkerStartOfFrame1 = 0xFFC1;
    internal const int MarkerStartOfFrame2 = 0xFFC2;
    internal const int MarkerStartOfFrame3 = 0xFFC3;
    internal const int MarkerDefineHuffmanTable = 0xFFC4;
    internal const int MarkerStartOfFrame5 = 0xFFC5;
    internal const int MarkerStartOfFrame6 = 0xFFC6;
    internal const int MarkerStartOfFrame7 = 0xFFC7;
    internal const int MarkerJpegExtension = 0xFFC8;
    internal const int MarkerStartOfFrame9 = 0xFFC9;
    internal const int MarkerStartOfFrame10 = 0xFFCA;
    internal const int MarkerStartOfFrame11 = 0xFFCB;
    internal const int MarkerDefineArithmeticConditioning = 0xFFCC;
    internal const int MarkerStartOfFrame13 = 0xFFCD;
    internal const int MarkerStartOfFrame14 = 0xFFCE;
    internal const int MarkerStartOfFrame15 = 0xFFCF;
    internal const int MarkerRestart0 = 0xFFD0;
    internal const int MarkerRestart1 = 0xFFD1;
    internal const int MarkerRestart2 = 0xFFD2;
    internal const int MarkerRestart3 = 0xFFD3;
    internal const int MarkerRestart4 = 0xFFD4;
    internal const int MarkerRestart5 = 0xFFD5;
    internal const int MarkerRestart6 = 0xFFD6;
    internal const int MarkerRestart7 = 0xFFD7;
    internal const int MarkerStartOfImage = 0xFFD8;
    internal const int MarkerEndOfImage = 0xFFD9;
    internal const int MarkerStartOfScan = 0xFFDA;
    internal const int MarkerDefineQuantizationTable = 0xFFDB;
    internal const int MarkerDefineNumberOfLines = 0xFFDC;
    internal const int MarkerDefineRestartInterval = 0xFFDD;
    internal const int MarkerDefineHierarchicalProgression = 0xFFDE;
    internal const int MarkerExpandReferenceComponents = 0xFFDF;
    internal const int MarkerApp0 = 0xFFE0;
    internal const int MarkerApp1 = 0xFFE1;
    internal const int MarkerApp2 = 0xFFE2;
    internal const int MarkerApp3 = 0xFFE3;
    internal const int MarkerApp4 = 0xFFE4;
    internal const int MarkerApp5 = 0xFFE5;
    internal const int MarkerApp6 = 0xFFE6;
    internal const int MarkerApp7 = 0xFFE7;
    internal const int MarkerApp8 = 0xFFE8;
    internal const int MarkerApp9 = 0xFFE9;
    internal const int MarkerApp10 = 0xFFEA;
    internal const int MarkerApp11 = 0xFFEB;
    internal const int MarkerApp12 = 0xFFEC;
    internal const int MarkerApp13 = 0xFFED;
    internal const int MarkerApp14 = 0xFFEE;
    internal const int MarkerApp15 = 0xFFEF;
    internal const int MarkerJpegLs = 0xFFF7;
    internal const int MarkerComment = 0xFFFE;

    internal const int JfxxExtensionJpegThumbnail = 0x10;
    internal const int JfxxExtensionIndexedThumbnail = 0x11;
    internal const int JfxxExtensionRgbThumbnail = 0x13;
    internal const int AdobeTransformUnknown = 0;
    internal const int AdobeTransformYCbCr = 1;
    internal const int AdobeTransformYCCK = 2;
    internal const int IccProfileIdentifierLength = 12;
    internal const int IccProfileChunkHeaderLength = 14;
    internal const int MaximumIccProfileChunkDataLength = MaximumMarkerPayloadLength - IccProfileChunkHeaderLength;
    internal const int MaximumIccProfileChunkCount = 255;
    internal const int ExifIdentifierLength = 6;
    internal const int XmpIdentifierLength = 29;
    internal const int ExtendedXmpIdentifierLength = 35;
    internal const int ExtendedXmpChunkHeaderLength = ExtendedXmpIdentifierLength + 40;
    internal const int MaximumExtendedXmpChunkDataLength = MaximumMarkerPayloadLength - ExtendedXmpChunkHeaderLength;
    internal const int MpfIdentifierLength = 4;
    internal const int PhotoshopIdentifierLength = 14;
    internal const int PhotoshopIptcResourceId = 0x0404;

    internal const int BlockWidth = 8;
    internal const int BlockHeight = 8;
    internal const int BlockSampleCount = 64;
    internal static readonly int[] ZigZagOrder = new int[]
    {
        0, 1, 8, 16, 9, 2, 3, 10,
        17, 24, 32, 25, 18, 11, 4, 5,
        12, 19, 26, 33, 40, 48, 41, 34,
        27, 20, 13, 6, 7, 14, 21, 28,
        35, 42, 49, 56, 57, 50, 43, 36,
        29, 22, 15, 23, 30, 37, 44, 51,
        58, 59, 52, 45, 38, 31, 39, 46,
        53, 60, 61, 54, 47, 55, 62, 63
    };

    internal const int DefaultQuality = 75;
    internal const int DefaultMaxWidth = 65535;
    internal const int DefaultMaxHeight = 65535;
    internal const int DefaultMaxComponents = 255;
    internal const int DefaultMaxScans = 1024;
    internal const int DefaultMaxMarkerPayloadBytes = MaximumMarkerPayloadLength;
    internal const int DefaultMaxRetainedMarkers = 4096;
    internal const int DefaultMaxTables = 256;
    internal const long DefaultMaxMetadataBytes = 64L * 1024L * 1024L;
    internal const long DefaultMaxEntropyBytes = 512L * 1024L * 1024L;
    internal const long DefaultMaxOutputPixels = 268435456L;
    internal const long DefaultMaxOutputBytes = 1073741824L;
    internal const long DefaultMaxComponentBufferBytes = 1073741824L;
    internal const long DefaultMaxProgressiveStateBytes = 1073741824L;
    internal const long DefaultMaxCallbackOutputBytes = 1073741824L;
}
