namespace tiff;

/// <summary>Defines the registered TIFF tag identifiers recognized by the library.</summary>
public static class TiffTagId
{
    /// <summary>Identifies the TIFF New Subfile Type tag.</summary>
    public const int NewSubfileType = 254;
    /// <summary>Identifies the TIFF Subfile Type tag.</summary>
    public const int SubfileType = 255;
    /// <summary>Identifies the TIFF Image Width tag.</summary>
    public const int ImageWidth = 256;
    /// <summary>Identifies the TIFF Image Length tag.</summary>
    public const int ImageLength = 257;
    /// <summary>Identifies the TIFF Bits Per Sample tag.</summary>
    public const int BitsPerSample = 258;
    /// <summary>Identifies the TIFF Compression tag.</summary>
    public const int Compression = 259;
    /// <summary>Identifies the TIFF Photometric Interpretation tag.</summary>
    public const int PhotometricInterpretation = 262;
    /// <summary>Identifies the TIFF Thresholding tag.</summary>
    public const int Thresholding = 263;
    /// <summary>Identifies the TIFF Cell Width tag.</summary>
    public const int CellWidth = 264;
    /// <summary>Identifies the TIFF Cell Length tag.</summary>
    public const int CellLength = 265;
    /// <summary>Identifies the TIFF Fill Order tag.</summary>
    public const int FillOrder = 266;
    /// <summary>Identifies the TIFF Document Name tag.</summary>
    public const int DocumentName = 269;
    /// <summary>Identifies the TIFF Image Description tag.</summary>
    public const int ImageDescription = 270;
    /// <summary>Identifies the TIFF Make tag.</summary>
    public const int Make = 271;
    /// <summary>Identifies the TIFF Model tag.</summary>
    public const int Model = 272;
    /// <summary>Identifies the TIFF Strip Offsets tag.</summary>
    public const int StripOffsets = 273;
    /// <summary>Identifies the TIFF Orientation tag.</summary>
    public const int Orientation = 274;
    /// <summary>Identifies the TIFF Samples Per Pixel tag.</summary>
    public const int SamplesPerPixel = 277;
    /// <summary>Identifies the TIFF Rows Per Strip tag.</summary>
    public const int RowsPerStrip = 278;
    /// <summary>Identifies the TIFF Strip Byte Counts tag.</summary>
    public const int StripByteCounts = 279;
    /// <summary>Identifies the TIFF Min Sample Value tag.</summary>
    public const int MinSampleValue = 280;
    /// <summary>Identifies the TIFF Max Sample Value tag.</summary>
    public const int MaxSampleValue = 281;
    /// <summary>Identifies the TIFF X Resolution tag.</summary>
    public const int XResolution = 282;
    /// <summary>Identifies the TIFF Y Resolution tag.</summary>
    public const int YResolution = 283;
    /// <summary>Identifies the TIFF Planar Configuration tag.</summary>
    public const int PlanarConfiguration = 284;
    /// <summary>Identifies the TIFF Page Name tag.</summary>
    public const int PageName = 285;
    /// <summary>Identifies the TIFF X Position tag.</summary>
    public const int XPosition = 286;
    /// <summary>Identifies the TIFF Y Position tag.</summary>
    public const int YPosition = 287;
    /// <summary>Identifies the TIFF Free Offsets tag.</summary>
    public const int FreeOffsets = 288;
    /// <summary>Identifies the TIFF Free Byte Counts tag.</summary>
    public const int FreeByteCounts = 289;
    /// <summary>Identifies the TIFF Gray Response Unit tag.</summary>
    public const int GrayResponseUnit = 290;
    /// <summary>Identifies the TIFF Gray Response Curve tag.</summary>
    public const int GrayResponseCurve = 291;
    /// <summary>Identifies the TIFF T4 Options tag.</summary>
    public const int T4Options = 292;
    /// <summary>Identifies the TIFF Group 3 Options tag.</summary>
    public const int Group3Options = 292;
    /// <summary>Identifies the TIFF T6 Options tag.</summary>
    public const int T6Options = 293;
    /// <summary>Identifies the TIFF Group 4 Options tag.</summary>
    public const int Group4Options = 293;
    /// <summary>Identifies the TIFF Resolution Unit tag.</summary>
    public const int ResolutionUnit = 296;
    /// <summary>Identifies the TIFF Page Number tag.</summary>
    public const int PageNumber = 297;
    /// <summary>Identifies the TIFF Color Response Unit tag.</summary>
    public const int ColorResponseUnit = 300;
    /// <summary>Identifies the TIFF Transfer Function tag.</summary>
    public const int TransferFunction = 301;
    /// <summary>Identifies the legacy TIFF Color Response Curves tag.</summary>
    public const int ColorResponseCurves = 301;
    /// <summary>Identifies the TIFF Software tag.</summary>
    public const int Software = 305;
    /// <summary>Identifies the TIFF Date Time tag.</summary>
    public const int DateTime = 306;
    /// <summary>Identifies the TIFF Artist tag.</summary>
    public const int Artist = 315;
    /// <summary>Identifies the TIFF Host Computer tag.</summary>
    public const int HostComputer = 316;
    /// <summary>Identifies the TIFF Predictor tag.</summary>
    public const int Predictor = 317;
    /// <summary>Identifies the TIFF White Point tag.</summary>
    public const int WhitePoint = 318;
    /// <summary>Identifies the TIFF Primary Chromaticities tag.</summary>
    public const int PrimaryChromaticities = 319;
    /// <summary>Identifies the TIFF Color Map tag.</summary>
    public const int ColorMap = 320;
    /// <summary>Identifies the TIFF Halftone Hints tag.</summary>
    public const int HalftoneHints = 321;
    /// <summary>Identifies the TIFF Tile Width tag.</summary>
    public const int TileWidth = 322;
    /// <summary>Identifies the TIFF Tile Length tag.</summary>
    public const int TileLength = 323;
    /// <summary>Identifies the TIFF Tile Offsets tag.</summary>
    public const int TileOffsets = 324;
    /// <summary>Identifies the TIFF Tile Byte Counts tag.</summary>
    public const int TileByteCounts = 325;
    /// <summary>Identifies the TIFF Bad Fax Lines tag.</summary>
    public const int BadFaxLines = 326;
    /// <summary>Identifies the TIFF Clean Fax Data tag.</summary>
    public const int CleanFaxData = 327;
    /// <summary>Identifies the TIFF Consecutive Bad Fax Lines tag.</summary>
    public const int ConsecutiveBadFaxLines = 328;
    /// <summary>Identifies the TIFF SubIFDs tag.</summary>
    public const int SubIFDs = 330;
    /// <summary>Identifies the TIFF Ink Set tag.</summary>
    public const int InkSet = 332;
    /// <summary>Identifies the TIFF Ink Names tag.</summary>
    public const int InkNames = 333;
    /// <summary>Identifies the TIFF Number Of Inks tag.</summary>
    public const int NumberOfInks = 334;
    /// <summary>Identifies the TIFF Dot Range tag.</summary>
    public const int DotRange = 336;
    /// <summary>Identifies the TIFF Target Printer tag.</summary>
    public const int TargetPrinter = 337;
    /// <summary>Identifies the TIFF Extra Samples tag.</summary>
    public const int ExtraSamples = 338;
    /// <summary>Identifies the TIFF Sample Format tag.</summary>
    public const int SampleFormat = 339;
    /// <summary>Identifies the TIFF S Min Sample Value tag.</summary>
    public const int SMinSampleValue = 340;
    /// <summary>Identifies the TIFF S Max Sample Value tag.</summary>
    public const int SMaxSampleValue = 341;
    /// <summary>Identifies the TIFF Transfer Range tag.</summary>
    public const int TransferRange = 342;
    /// <summary>Identifies the TIFF JPEG Tables tag.</summary>
    public const int JPEGTables = 347;
    /// <summary>Identifies the legacy TIFF JPEG processing tag.</summary>
    public const int JPEGProc = 512;
    /// <summary>Identifies the legacy TIFF JPEG interchange format offset tag.</summary>
    public const int JPEGInterchangeFormat = 513;
    /// <summary>Identifies the legacy TIFF JPEG interchange format byte count tag.</summary>
    public const int JPEGInterchangeFormatLength = 514;
    /// <summary>Identifies the legacy TIFF JPEG restart interval tag.</summary>
    public const int JPEGRestartInterval = 515;
    /// <summary>Identifies the legacy TIFF JPEG lossless predictors tag.</summary>
    public const int JPEGLosslessPredictors = 517;
    /// <summary>Identifies the legacy TIFF JPEG point transforms tag.</summary>
    public const int JPEGPointTransforms = 518;
    /// <summary>Identifies the legacy TIFF JPEG quantization table offsets tag.</summary>
    public const int JPEGQTables = 519;
    /// <summary>Identifies the legacy TIFF JPEG DC table offsets tag.</summary>
    public const int JPEGDCTables = 520;
    /// <summary>Identifies the legacy TIFF JPEG AC table offsets tag.</summary>
    public const int JPEGACTables = 521;
    /// <summary>Identifies the TIFF Y Cb Cr Coefficients tag.</summary>
    public const int YCbCrCoefficients = 529;
    /// <summary>Identifies the TIFF Y Cb Cr Sub Sampling tag.</summary>
    public const int YCbCrSubSampling = 530;
    /// <summary>Identifies the TIFF Y Cb Cr Positioning tag.</summary>
    public const int YCbCrPositioning = 531;
    /// <summary>Identifies the TIFF Reference Black White tag.</summary>
    public const int ReferenceBlackWhite = 532;
    /// <summary>Identifies the TIFF XMP packet tag.</summary>
    public const int Xmp = 700;
    /// <summary>Identifies the TIFF image depth tag.</summary>
    public const int ImageDepth = 32997;
    /// <summary>Identifies the TIFF tile depth tag.</summary>
    public const int TileDepth = 32998;
    /// <summary>Identifies the TIFF CFA repeat-pattern dimensions tag.</summary>
    public const int CFARepeatPatternDim = 33421;
    /// <summary>Identifies the TIFF CFA pattern tag.</summary>
    public const int CFAPattern = 33422;
    /// <summary>Identifies the TIFF IPTC tag.</summary>
    public const int Iptc = 33723;
    /// <summary>Identifies the TIFF RichTIFF IPTC tag.</summary>
    public const int RichTiffIptc = 33723;
    /// <summary>Identifies the TIFF Photoshop tag.</summary>
    public const int Photoshop = 34377;
    /// <summary>Identifies the TIFF Cz Lsm Info tag.</summary>
    public const int CzLsmInfo = 34412;
    /// <summary>Identifies the TIFF ICC Profile tag.</summary>
    public const int IccProfile = 34675;
    /// <summary>Identifies the TIFF EXIF IFD tag.</summary>
    public const int ExifIfd = 34665;
    /// <summary>Identifies the TIFF GPS IFD tag.</summary>
    public const int GpsIfd = 34853;
    /// <summary>Identifies the TIFF fax receive parameters tag.</summary>
    public const int FaxReceiveParameters = 34908;
    /// <summary>Identifies the TIFF fax receive parameters tag.</summary>
    public const int FaxRecvParams = 34908;
    /// <summary>Identifies the TIFF fax subaddress tag.</summary>
    public const int FaxSubAddress = 34909;
    /// <summary>Identifies the TIFF fax receive time tag.</summary>
    public const int FaxReceiveTime = 34910;
    /// <summary>Identifies the TIFF fax receive time tag.</summary>
    public const int FaxRecvTime = 34910;
    /// <summary>Identifies the TIFF fax digital command signal tag.</summary>
    public const int FaxDcs = 34911;
    /// <summary>Identifies the TIFF Interoperability IFD tag.</summary>
    public const int InteroperabilityIfd = 40965;
    /// <summary>Identifies the TIFF Copyright tag.</summary>
    public const int Copyright = 33432;
    /// <summary>Identifies the TIFF Model Pixel Scale tag.</summary>
    public const int ModelPixelScale = 33550;
    /// <summary>Identifies the TIFF Model Tiepoint tag.</summary>
    public const int ModelTiepoint = 33922;
    /// <summary>Identifies the TIFF Model Transformation tag.</summary>
    public const int ModelTransformation = 34264;
    /// <summary>Identifies the TIFF geographic Key Directory tag.</summary>
    public const int GeoKeyDirectory = 34735;
    /// <summary>Identifies the TIFF geographic Double Params tag.</summary>
    public const int GeoDoubleParams = 34736;
    /// <summary>Identifies the TIFF geographic ASCII Params tag.</summary>
    public const int GeoAsciiParams = 34737;
    /// <summary>Identifies the TIFF GDAL Metadata tag.</summary>
    public const int GdalMetadata = 42112;
    /// <summary>Identifies the TIFF GDAL No Data tag.</summary>
    public const int GdalNoData = 42113;
    /// <summary>Identifies the TIFF Dng Version tag.</summary>
    public const int DngVersion = 50706;
    /// <summary>Identifies the TIFF Dng Backward Version tag.</summary>
    public const int DngBackwardVersion = 50707;
    /// <summary>Identifies the TIFF Unique Camera Model tag.</summary>
    public const int UniqueCameraModel = 50708;
    /// <summary>Identifies the DNG localized camera model tag.</summary>
    public const int LocalizedCameraModel = 50709;
    /// <summary>Identifies the DNG CFA plane color tag.</summary>
    public const int CFAPlaneColor = 50710;
    /// <summary>Identifies the DNG CFA layout tag.</summary>
    public const int CFALayout = 50711;
    /// <summary>Identifies the DNG linearization table tag.</summary>
    public const int LinearizationTable = 50712;
    /// <summary>Identifies the DNG black-level repeat dimensions tag.</summary>
    public const int BlackLevelRepeatDim = 50713;
    /// <summary>Identifies the DNG black level tag.</summary>
    public const int BlackLevel = 50714;
    /// <summary>Identifies the DNG horizontal black-level delta tag.</summary>
    public const int BlackLevelDeltaH = 50715;
    /// <summary>Identifies the DNG vertical black-level delta tag.</summary>
    public const int BlackLevelDeltaV = 50716;
    /// <summary>Identifies the DNG white level tag.</summary>
    public const int WhiteLevel = 50717;
    /// <summary>Identifies the DNG default scale tag.</summary>
    public const int DefaultScale = 50718;
    /// <summary>Identifies the DNG default crop origin tag.</summary>
    public const int DefaultCropOrigin = 50719;
    /// <summary>Identifies the DNG default crop size tag.</summary>
    public const int DefaultCropSize = 50720;
    /// <summary>Identifies the TIFF Color Matrix1 tag.</summary>
    public const int ColorMatrix1 = 50721;
    /// <summary>Identifies the TIFF Color Matrix2 tag.</summary>
    public const int ColorMatrix2 = 50722;
    /// <summary>Identifies the DNG camera calibration matrix 1 tag.</summary>
    public const int CameraCalibration1 = 50723;
    /// <summary>Identifies the DNG camera calibration matrix 2 tag.</summary>
    public const int CameraCalibration2 = 50724;
    /// <summary>Identifies the DNG reduction matrix 1 tag.</summary>
    public const int ReductionMatrix1 = 50725;
    /// <summary>Identifies the DNG reduction matrix 2 tag.</summary>
    public const int ReductionMatrix2 = 50726;
    /// <summary>Identifies the DNG analog balance tag.</summary>
    public const int AnalogBalance = 50727;
    /// <summary>Identifies the DNG as-shot neutral tag.</summary>
    public const int AsShotNeutral = 50728;
    /// <summary>Identifies the DNG as-shot white chromaticity tag.</summary>
    public const int AsShotWhiteXY = 50729;
    /// <summary>Identifies the DNG baseline exposure tag.</summary>
    public const int BaselineExposure = 50730;
    /// <summary>Identifies the DNG baseline noise tag.</summary>
    public const int BaselineNoise = 50731;
    /// <summary>Identifies the DNG baseline sharpness tag.</summary>
    public const int BaselineSharpness = 50732;
    /// <summary>Identifies the DNG Bayer green split tag.</summary>
    public const int BayerGreenSplit = 50733;
    /// <summary>Identifies the DNG linear response limit tag.</summary>
    public const int LinearResponseLimit = 50734;
    /// <summary>Identifies the DNG camera serial number tag.</summary>
    public const int CameraSerialNumber = 50735;
    /// <summary>Identifies the DNG lens information tag.</summary>
    public const int LensInfo = 50736;
    /// <summary>Identifies the DNG chroma blur radius tag.</summary>
    public const int ChromaBlurRadius = 50737;
    /// <summary>Identifies the DNG anti-alias strength tag.</summary>
    public const int AntiAliasStrength = 50738;
    /// <summary>Identifies the DNG shadow scale tag.</summary>
    public const int ShadowScale = 50739;
    /// <summary>Identifies the DNG private data tag.</summary>
    public const int DngPrivateData = 50740;
    /// <summary>Identifies the DNG maker-note safety tag.</summary>
    public const int MakerNoteSafety = 50741;
    /// <summary>Identifies the DNG calibration illuminant 1 tag.</summary>
    public const int CalibrationIlluminant1 = 50778;
    /// <summary>Identifies the DNG calibration illuminant 2 tag.</summary>
    public const int CalibrationIlluminant2 = 50779;
    /// <summary>Identifies the DNG best-quality scale tag.</summary>
    public const int BestQualityScale = 50780;
    /// <summary>Identifies the DNG raw-data unique identifier tag.</summary>
    public const int RawDataUniqueId = 50781;
    /// <summary>Identifies the DNG original raw filename tag.</summary>
    public const int OriginalRawFileName = 50827;
    /// <summary>Identifies the DNG original raw file data tag.</summary>
    public const int OriginalRawFileData = 50828;
    /// <summary>Identifies the DNG active area tag.</summary>
    public const int ActiveArea = 50829;
    /// <summary>Identifies the DNG masked areas tag.</summary>
    public const int MaskedAreas = 50830;
    /// <summary>Identifies the DNG as-shot ICC profile tag.</summary>
    public const int AsShotIccProfile = 50831;
    /// <summary>Identifies the DNG as-shot pre-profile matrix tag.</summary>
    public const int AsShotPreProfileMatrix = 50832;
    /// <summary>Identifies the DNG current ICC profile tag.</summary>
    public const int CurrentIccProfile = 50833;
    /// <summary>Identifies the DNG current pre-profile matrix tag.</summary>
    public const int CurrentPreProfileMatrix = 50834;
    /// <summary>Identifies the ImageJ private metadata byte-counts tag.</summary>
    public const int ImageJMetadataByteCounts = 50838;
    /// <summary>Identifies the ImageJ private metadata payload tag.</summary>
    public const int ImageJMetadata = 50839;
    /// <summary>Identifies the DNG depth format tag.</summary>
    public const int DepthFormat = 51177;
    /// <summary>Identifies the DNG near depth tag.</summary>
    public const int DepthNear = 51178;
    /// <summary>Identifies the DNG far depth tag.</summary>
    public const int DepthFar = 51179;
    /// <summary>Identifies the DNG depth units tag.</summary>
    public const int DepthUnits = 51180;
    /// <summary>Identifies the DNG depth measurement type tag.</summary>
    public const int DepthMeasureType = 51181;
    /// <summary>Identifies the DNG enhancement parameters tag.</summary>
    public const int EnhanceParams = 51182;
}
