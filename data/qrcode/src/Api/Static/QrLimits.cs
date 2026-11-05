namespace qrcode;

/// <summary>Defines standardized QR bounds and default resource-safety limits.</summary>
public static class QrLimits
{
    /// <summary>The first QR Code Model 2 version.</summary>
    public const int Model2MinimumVersion = 1;

    /// <summary>The last QR Code Model 2 version.</summary>
    public const int Model2MaximumVersion = 40;

    /// <summary>The width and height of a Model 2 version 1 symbol in modules.</summary>
    public const int Model2MinimumDimension = 21;

    /// <summary>The width and height of a Model 2 version 40 symbol in modules.</summary>
    public const int Model2MaximumDimension = 177;

    /// <summary>The standard Model 2 quiet-zone width in modules.</summary>
    public const int Model2QuietZoneModules = 4;

    /// <summary>The lowest Model 2 mask identifier.</summary>
    public const int Model2MinimumMask = 0;

    /// <summary>The highest Model 2 mask identifier.</summary>
    public const int Model2MaximumMask = 7;

    /// <summary>The first Micro QR version number.</summary>
    public const int MicroMinimumVersion = 1;

    /// <summary>The last Micro QR version number.</summary>
    public const int MicroMaximumVersion = 4;

    /// <summary>The width and height of an M1 symbol in modules.</summary>
    public const int MicroMinimumDimension = 11;

    /// <summary>The width and height of an M4 symbol in modules.</summary>
    public const int MicroMaximumDimension = 17;

    /// <summary>The standard Micro QR quiet-zone width in modules.</summary>
    public const int MicroQuietZoneModules = 2;

    /// <summary>The lowest Micro QR mask identifier.</summary>
    public const int MicroMinimumMask = 0;

    /// <summary>The highest Micro QR mask identifier.</summary>
    public const int MicroMaximumMask = 3;

    /// <summary>The number of standardized rectangular Micro QR sizes.</summary>
    public const int RectangularMicroSizeCount = 32;

    /// <summary>The lowest standardized rectangular Micro QR height in modules.</summary>
    public const int RectangularMicroMinimumHeight = 7;

    /// <summary>The highest standardized rectangular Micro QR height in modules.</summary>
    public const int RectangularMicroMaximumHeight = 17;

    /// <summary>The lowest standardized rectangular Micro QR width in modules.</summary>
    public const int RectangularMicroMinimumWidth = 27;

    /// <summary>The highest standardized rectangular Micro QR width in modules.</summary>
    public const int RectangularMicroMaximumWidth = 139;

    /// <summary>The standard rectangular Micro QR quiet-zone width in modules.</summary>
    public const int RectangularMicroQuietZoneModules = 2;

    /// <summary>The fixed mask identifier used by rectangular Micro QR.</summary>
    public const int RectangularMicroMask = 1;

    /// <summary>The first legacy QR Code Model 1 version.</summary>
    public const int LegacyModel1MinimumVersion = 1;

    /// <summary>The last legacy QR Code Model 1 version.</summary>
    public const int LegacyModel1MaximumVersion = 14;

    /// <summary>The width and height of a legacy Model 1 version 1 symbol in modules.</summary>
    public const int LegacyModel1MinimumDimension = 21;

    /// <summary>The width and height of a legacy Model 1 version 14 symbol in modules.</summary>
    public const int LegacyModel1MaximumDimension = 73;

    /// <summary>The standard legacy Model 1 quiet-zone width in modules.</summary>
    public const int LegacyModel1QuietZoneModules = 4;

    /// <summary>The minimum number of symbols in a structured-append sequence.</summary>
    public const int StructuredAppendMinimumSymbols = 2;

    /// <summary>The maximum number of symbols in a structured-append sequence.</summary>
    public const int StructuredAppendMaximumSymbols = 16;

    /// <summary>The highest zero-based structured-append symbol index.</summary>
    public const int StructuredAppendMaximumIndex = 15;

    /// <summary>The highest structured-append parity value.</summary>
    public const int StructuredAppendMaximumParity = 255;

    /// <summary>The lowest QR ECI assignment value.</summary>
    public const int EciMinimumAssignment = 0;

    /// <summary>The largest ECI assignment encoded by the one-byte QR designator form.</summary>
    public const int EciOneByteMaximumAssignment = 127;

    /// <summary>The largest ECI assignment encoded by the two-byte QR designator form.</summary>
    public const int EciTwoByteMaximumAssignment = 16383;

    /// <summary>The largest value representable by a QR ECI designator.</summary>
    public const int EciMaximumQrAssignment = 999999;

    /// <summary>The highest currently allocatable ECI protocol assignment.</summary>
    public const int EciMaximumProtocolAssignment = 811799;

    /// <summary>The default maximum accepted input byte count.</summary>
    public const int DefaultMaximumInputBytes = 1048576;

    /// <summary>The default maximum number of payload and control segments.</summary>
    public const int DefaultMaximumSegments = 1024;

    /// <summary>The default maximum number of bounded decode recovery attempts.</summary>
    public const int DefaultMaximumRecoveryAttempts = 64;

    /// <summary>The default maximum allocation size for one decode operation.</summary>
    public const int DefaultMaximumAllocationBytes = 16777216;

    /// <summary>The default maximum number of retained detection candidates.</summary>
    public const int DefaultMaximumDetectionCandidates = 256;

    /// <summary>The default maximum accepted image dimension in pixels.</summary>
    public const int DefaultMaximumImageDimension = 16384;

    /// <summary>The default maximum accepted image pixel count.</summary>
    public const int DefaultMaximumImagePixels = 67108864;

    /// <summary>The default maximum number of retained quality samples.</summary>
    public const int DefaultMaximumQualitySamples = 1048576;
}
