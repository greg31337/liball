namespace tiff;

/// <summary>Contains normalized image layout and pixel-format information derived from TIFF tags.</summary>
public class TiffImageInfo
{
    /// <summary>Identifies the zero-based image directory in the file.</summary>
    public int ImageIndex;
    /// <summary>Contains the image or region width in pixels.</summary>
    public ulong Width;
    /// <summary>Contains the image or region height in rows.</summary>
    public ulong Length;
    /// <summary>Contains the samples per pixel.</summary>
    public int SamplesPerPixel;
    /// <summary>Contains the bits per sample.</summary>
    public int[] BitsPerSample;
    /// <summary>Contains the compression.</summary>
    public TiffCompression Compression;
    /// <summary>Contains the fill order.</summary>
    public int FillOrder;
    /// <summary>Contains the photometric.</summary>
    public TiffPhotometric Photometric;
    /// <summary>Contains the planar configuration.</summary>
    public TiffPlanarConfiguration PlanarConfiguration;
    /// <summary>Contains the sample formats.</summary>
    public TiffSampleFormat[] SampleFormats;
    /// <summary>Contains the extra samples.</summary>
    public TiffExtraSample[] ExtraSamples;
    /// <summary>Contains the color map.</summary>
    public ushort[] ColorMap;
    /// <summary>Contains the orientation.</summary>
    public TiffOrientation Orientation;
    /// <summary>Indicates whether tiled applies.</summary>
    public bool IsTiled;
    /// <summary>Contains the rows per strip.</summary>
    public ulong RowsPerStrip;
    /// <summary>Contains the tile width.</summary>
    public ulong TileWidth;
    /// <summary>Contains the tile length.</summary>
    public ulong TileLength;
    /// <summary>Contains the predictor.</summary>
    public int Predictor;
    /// <summary>Contains the T4 options.</summary>
    public int T4Options;
    /// <summary>Contains the T6 options.</summary>
    public int T6Options;
    /// <summary>Contains the Y cb cr coefficients.</summary>
    public double[] YCbCrCoefficients;
    /// <summary>Contains the Y cb cr sub sampling horizontal.</summary>
    public int YCbCrSubSamplingHorizontal;
    /// <summary>Contains the Y cb cr sub sampling vertical.</summary>
    public int YCbCrSubSamplingVertical;
    /// <summary>Contains the reference black white.</summary>
    public double[] ReferenceBlackWhite;
    /// <summary>Contains the image depth in samples along the Z axis.</summary>
    public ulong ImageDepth;
    /// <summary>Contains the tile depth in samples along the Z axis.</summary>
    public ulong TileDepth;
    /// <summary>Contains the NewSubfileType bit field.</summary>
    public ulong NewSubfileType;
    /// <summary>Contains the legacy SubfileType value.</summary>
    public int SubfileType;
    /// <summary>Indicates whether this image is reduced resolution.</summary>
    public bool IsReducedResolution;
    /// <summary>Indicates whether this image is one page of a multi-page document.</summary>
    public bool IsPage;
    /// <summary>Indicates whether this image is a transparency mask.</summary>
    public bool IsTransparencyMask;
    /// <summary>Indicates whether this image is a depth map.</summary>
    public bool IsDepthMap;
    /// <summary>Indicates whether this image contains enhanced image data.</summary>
    public bool IsEnhancedImage;
    /// <summary>Contains the horizontal resolution.</summary>
    public double XResolution;
    /// <summary>Contains the vertical resolution.</summary>
    public double YResolution;
    /// <summary>Specifies the unit used by resolution and position values.</summary>
    public TiffResolutionUnit ResolutionUnit;
    /// <summary>Contains the horizontal image position.</summary>
    public double XPosition;
    /// <summary>Contains the vertical image position.</summary>
    public double YPosition;
    /// <summary>Contains normalized baseline minimum sample values.</summary>
    public double[] MinSampleValues;
    /// <summary>Contains normalized baseline maximum sample values.</summary>
    public double[] MaxSampleValues;
    /// <summary>Contains explicitly stored signed or floating minimum sample values.</summary>
    public double[] SMinSampleValues;
    /// <summary>Contains explicitly stored signed or floating maximum sample values.</summary>
    public double[] SMaxSampleValues;
    /// <summary>Contains the image white-point chromaticities.</summary>
    public double[] WhitePoint;
    /// <summary>Contains the primary RGB chromaticities.</summary>
    public double[] PrimaryChromaticities;
    /// <summary>Contains the ink-set identifier.</summary>
    public int InkSet;
    /// <summary>Contains individual ink names.</summary>
    public string[] InkNames;
    /// <summary>Contains the number of inks.</summary>
    public int NumberOfInks;
    /// <summary>Contains normalized dot-range endpoints.</summary>
    public ushort[] DotRange;
    /// <summary>Contains the target printer description.</summary>
    public string TargetPrinter;
    /// <summary>Contains the transfer-function lookup tables.</summary>
    public ushort[] TransferFunction;
    /// <summary>Contains the transfer-range values.</summary>
    public ushort[] TransferRange;
    /// <summary>Contains the encoded JPEG tables stream.</summary>
    public byte[] JpegTables;
    /// <summary>Contains the CFA repeat-pattern row and column counts.</summary>
    public int[] CFARepeatPatternDimensions;
    /// <summary>Contains the CFA color pattern.</summary>
    public byte[] CFAPattern;
    /// <summary>Contains the thresholding mode.</summary>
    public int Thresholding;
    /// <summary>Contains the dithering or halftoning cell width.</summary>
    public int CellWidth;
    /// <summary>Contains the dithering or halftoning cell length.</summary>
    public int CellLength;
    /// <summary>Contains the gray-response precision unit.</summary>
    public int GrayResponseUnit;
    /// <summary>Contains the gray-response curve.</summary>
    public ushort[] GrayResponseCurve;
    /// <summary>Contains the color-response precision unit.</summary>
    public int ColorResponseUnit;
    /// <summary>Contains the highlight and shadow halftone hints.</summary>
    public int[] HalftoneHints;
    /// <summary>Contains the page number and total page count.</summary>
    public int[] PageNumber;
    /// <summary>Contains the YCbCr positioning mode.</summary>
    public int YCbCrPositioning;
    /// <summary>Contains the document name.</summary>
    public string DocumentName;
    /// <summary>Contains the image description.</summary>
    public string ImageDescription;
    /// <summary>Contains the scanner or camera manufacturer.</summary>
    public string Make;
    /// <summary>Contains the scanner or camera model.</summary>
    public string Model;
    /// <summary>Contains the page name.</summary>
    public string PageName;
    /// <summary>Contains the creating software name.</summary>
    public string Software;
    /// <summary>Contains the creation date and time text.</summary>
    public string DateTime;
    /// <summary>Contains the artist name.</summary>
    public string Artist;
    /// <summary>Contains the host-computer name.</summary>
    public string HostComputer;
    /// <summary>Contains copyright text.</summary>
    public string Copyright;

    /// <summary>Creates a new instance of the TIFF Image Info class.</summary>
    public TiffImageInfo()
    {
        ImageIndex = TiffDiagnostic.NoImageIndex;
        Width = 0;
        Length = 0;
        SamplesPerPixel = 1;
        BitsPerSample = new int[] { 1 };
        Compression = TiffCompression.None;
        FillOrder = 1;
        Photometric = TiffPhotometric.WhiteIsZero;
        PlanarConfiguration = TiffPlanarConfiguration.Contiguous;
        SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger };
        ExtraSamples = new TiffExtraSample[0];
        ColorMap = new ushort[0];
        Orientation = TiffOrientation.TopLeft;
        IsTiled = false;
        RowsPerStrip = uint.MaxValue;
        TileWidth = 0;
        TileLength = 0;
        Predictor = 1;
        T4Options = 0;
        T6Options = 0;
        YCbCrCoefficients = new double[] { 0.299d, 0.587d, 0.114d };
        YCbCrSubSamplingHorizontal = 2;
        YCbCrSubSamplingVertical = 2;
        ReferenceBlackWhite = new double[] { 0d, 255d, 128d, 255d, 128d, 255d };
        ImageDepth = 1;
        TileDepth = 1;
        NewSubfileType = 0;
        SubfileType = 0;
        IsReducedResolution = false;
        IsPage = false;
        IsTransparencyMask = false;
        IsDepthMap = false;
        IsEnhancedImage = false;
        XResolution = 0d;
        YResolution = 0d;
        ResolutionUnit = TiffResolutionUnit.Inch;
        XPosition = 0d;
        YPosition = 0d;
        MinSampleValues = new double[] { 0d };
        MaxSampleValues = new double[] { 1d };
        SMinSampleValues = new double[0];
        SMaxSampleValues = new double[0];
        WhitePoint = new double[0];
        PrimaryChromaticities = new double[0];
        InkSet = 1;
        InkNames = new string[0];
        NumberOfInks = 4;
        DotRange = new ushort[0];
        TargetPrinter = "";
        TransferFunction = new ushort[0];
        TransferRange = new ushort[0];
        JpegTables = new byte[0];
        CFARepeatPatternDimensions = new int[0];
        CFAPattern = new byte[0];
        Thresholding = 1;
        CellWidth = 0;
        CellLength = 0;
        GrayResponseUnit = 2;
        GrayResponseCurve = new ushort[0];
        ColorResponseUnit = 0;
        HalftoneHints = new int[0];
        PageNumber = new int[0];
        YCbCrPositioning = 1;
        DocumentName = "";
        ImageDescription = "";
        Make = "";
        Model = "";
        PageName = "";
        Software = "";
        DateTime = "";
        Artist = "";
        HostComputer = "";
        Copyright = "";
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF image info.</returns>
    internal TiffImageInfo Copy()
    {
        TiffImageInfo copy = new TiffImageInfo();
        copy.ImageIndex = ImageIndex;
        copy.Width = Width;
        copy.Length = Length;
        copy.SamplesPerPixel = SamplesPerPixel;
        copy.BitsPerSample = CopyIntArray(BitsPerSample);
        copy.Compression = Compression;
        copy.FillOrder = FillOrder;
        copy.Photometric = Photometric;
        copy.PlanarConfiguration = PlanarConfiguration;
        copy.SampleFormats = CopySampleFormats(SampleFormats);
        copy.ExtraSamples = CopyExtraSamples(ExtraSamples);
        copy.ColorMap = CopyUShortArray(ColorMap);
        copy.Orientation = Orientation;
        copy.IsTiled = IsTiled;
        copy.RowsPerStrip = RowsPerStrip;
        copy.TileWidth = TileWidth;
        copy.TileLength = TileLength;
        copy.Predictor = Predictor;
        copy.T4Options = T4Options;
        copy.T6Options = T6Options;
        copy.YCbCrCoefficients = CopyDoubleArray(YCbCrCoefficients);
        copy.YCbCrSubSamplingHorizontal = YCbCrSubSamplingHorizontal;
        copy.YCbCrSubSamplingVertical = YCbCrSubSamplingVertical;
        copy.ReferenceBlackWhite = CopyDoubleArray(ReferenceBlackWhite);
        copy.ImageDepth = ImageDepth;
        copy.TileDepth = TileDepth;
        copy.NewSubfileType = NewSubfileType;
        copy.SubfileType = SubfileType;
        copy.IsReducedResolution = IsReducedResolution;
        copy.IsPage = IsPage;
        copy.IsTransparencyMask = IsTransparencyMask;
        copy.IsDepthMap = IsDepthMap;
        copy.IsEnhancedImage = IsEnhancedImage;
        copy.XResolution = XResolution;
        copy.YResolution = YResolution;
        copy.ResolutionUnit = ResolutionUnit;
        copy.XPosition = XPosition;
        copy.YPosition = YPosition;
        copy.MinSampleValues = CopyDoubleArray(MinSampleValues);
        copy.MaxSampleValues = CopyDoubleArray(MaxSampleValues);
        copy.SMinSampleValues = CopyDoubleArray(SMinSampleValues);
        copy.SMaxSampleValues = CopyDoubleArray(SMaxSampleValues);
        copy.WhitePoint = CopyDoubleArray(WhitePoint);
        copy.PrimaryChromaticities = CopyDoubleArray(PrimaryChromaticities);
        copy.InkSet = InkSet;
        copy.InkNames = CopyStringArray(InkNames);
        copy.NumberOfInks = NumberOfInks;
        copy.DotRange = CopyUShortArray(DotRange);
        copy.TargetPrinter = TargetPrinter;
        copy.TransferFunction = CopyUShortArray(TransferFunction);
        copy.TransferRange = CopyUShortArray(TransferRange);
        copy.JpegTables = CopyByteArray(JpegTables);
        copy.CFARepeatPatternDimensions = CopyIntArray(CFARepeatPatternDimensions);
        copy.CFAPattern = CopyByteArray(CFAPattern);
        copy.Thresholding = Thresholding;
        copy.CellWidth = CellWidth;
        copy.CellLength = CellLength;
        copy.GrayResponseUnit = GrayResponseUnit;
        copy.GrayResponseCurve = CopyUShortArray(GrayResponseCurve);
        copy.ColorResponseUnit = ColorResponseUnit;
        copy.HalftoneHints = CopyIntArray(HalftoneHints);
        copy.PageNumber = CopyIntArray(PageNumber);
        copy.YCbCrPositioning = YCbCrPositioning;
        copy.DocumentName = DocumentName;
        copy.ImageDescription = ImageDescription;
        copy.Make = Make;
        copy.Model = Model;
        copy.PageName = PageName;
        copy.Software = Software;
        copy.DateTime = DateTime;
        copy.Artist = Artist;
        copy.HostComputer = HostComputer;
        copy.Copyright = Copyright;
        return copy;
    }

    internal static int[] CopyIntArray(int[] values)
    {
        if (values == null)
        {
            return new int[0];
        }

        int[] copy = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }

    internal static TiffSampleFormat[] CopySampleFormats(TiffSampleFormat[] values)
    {
        if (values == null)
        {
            return new TiffSampleFormat[0];
        }

        TiffSampleFormat[] copy = new TiffSampleFormat[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }

    internal static TiffExtraSample[] CopyExtraSamples(TiffExtraSample[] values)
    {
        if (values == null)
        {
            return new TiffExtraSample[0];
        }

        TiffExtraSample[] copy = new TiffExtraSample[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }

    internal static ushort[] CopyUShortArray(ushort[] values)
    {
        if (values == null)
        {
            return new ushort[0];
        }

        ushort[] copy = new ushort[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }

    internal static double[] CopyDoubleArray(double[] values)
    {
        if (values == null)
        {
            return new double[0];
        }

        double[] copy = new double[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }

    internal static byte[] CopyByteArray(byte[] values)
    {
        if (values == null)
        {
            return new byte[0];
        }

        byte[] copy = new byte[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }

    internal static string[] CopyStringArray(string[] values)
    {
        if (values == null)
        {
            return new string[0];
        }

        string[] copy = new string[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                copy[index] = "";
            }
            else
            {
                copy[index] = values[index];
            }

            index = index + 1;
        }

        return copy;
    }
}
