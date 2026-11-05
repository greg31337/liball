namespace image;

internal interface IImageMetadataParser
{
    bool Supports(ImageFormatKind kind);
    ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options);
    string GetName();
}

internal class MetadataParser : IImageMetadataParser
{
    string Name;
    ImageFormatKind[] SupportedFormats;
    System.Func<BoundedStreamReader?, ImageFormatInfo, ImageMetadataReaderOptions, ImageMetadataDocument> ParseFunction;

    internal MetadataParser(
        string name,
        ImageFormatKind[] supportedFormats,
        System.Func<BoundedStreamReader?, ImageFormatInfo, ImageMetadataReaderOptions, ImageMetadataDocument> parseFunction)
    {
        Name = name;
        SupportedFormats = supportedFormats;
        ParseFunction = parseFunction;
    }

    public bool Supports(ImageFormatKind kind)
    {
        foreach (ImageFormatKind supportedFormat in SupportedFormats)
        {
            if (supportedFormat == kind)
            {
                return true;
            }
        }

        return false;
    }

    public ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        return ParseFunction(reader, format, options);
    }

    public string GetName()
    {
        return Name;
    }
}

internal class ParserDispatch
{
    static readonly IImageMetadataParser UnknownParser = new MetadataParser(
        "UnknownMetadataParser",
        new ImageFormatKind[0],
        (reader, format, options) => UnknownMetadataParser.Parse(reader, format, options));

    static readonly IImageMetadataParser[] Parsers =
    [
        new MetadataParser("JpegMetadataParser", [ImageFormatKind.Jpeg], (reader, format, options) => JpegMetadataParser.Parse(reader, format, options)),
        new MetadataParser("TiffMetadataParser", [ImageFormatKind.Tiff, ImageFormatKind.BigTiff], (reader, format, options) => TiffMetadataParser.Parse(reader, format, options)),
        new MetadataParser("PngMetadataParser", [ImageFormatKind.Png], (reader, format, options) => PngMetadataParser.Parse(reader, format, options)),
        new MetadataParser("GifMetadataParser", [ImageFormatKind.Gif], (reader, format, options) => GifMetadataParser.Parse(reader, format, options)),
        new MetadataParser("WebPMetadataParser", [ImageFormatKind.WebP], (reader, format, options) => WebPMetadataParser.Parse(reader, format, options)),
        new MetadataParser("BmpMetadataParser", [ImageFormatKind.Bmp], (reader, format, options) => BmpMetadataParser.Parse(reader, format, options)),
        new MetadataParser("IcoMetadataParser", [ImageFormatKind.Ico, ImageFormatKind.Cur], (reader, format, options) => IcoMetadataParser.Parse(reader, format, options)),
        new MetadataParser("SvgMetadataParser", [ImageFormatKind.Svg, ImageFormatKind.Svgz], (reader, format, options) => SvgMetadataParser.Parse(reader, format, options)),
        new MetadataParser("BmffMetadataParser", [ImageFormatKind.Heif, ImageFormatKind.Heic, ImageFormatKind.Avif], (reader, format, options) => BmffMetadataParser.Parse(reader, format, options)),
        new MetadataParser("Jpeg2000MetadataParser", [ImageFormatKind.Jpeg2000], (reader, format, options) => Jpeg2000MetadataParser.Parse(reader, format, options)),
        new MetadataParser("JpegXlMetadataParser", [ImageFormatKind.JpegXl], (reader, format, options) => JpegXlMetadataParser.Parse(reader, format, options)),
        new MetadataParser("PsdMetadataParser", [ImageFormatKind.Psd, ImageFormatKind.Psb], (reader, format, options) => PsdMetadataParser.Parse(reader, format, options)),
        new MetadataParser(
            "RawMetadataParser",
            [
                ImageFormatKind.Dng,
                ImageFormatKind.Nef,
                ImageFormatKind.Arw,
                ImageFormatKind.Orf,
                ImageFormatKind.Pef,
                ImageFormatKind.Rw2,
                ImageFormatKind.Srw,
                ImageFormatKind.ThreeFr,
                ImageFormatKind.Kdc,
                ImageFormatKind.Mrw,
                ImageFormatKind.Rwl,
                ImageFormatKind.Gpr,
                ImageFormatKind.Cr2,
                ImageFormatKind.Cr3,
                ImageFormatKind.Crw,
                ImageFormatKind.Raf
            ],
            (reader, format, options) => RawMetadataParser.Parse(reader, format, options)),
        new MetadataParser("NetpbmMetadataParser", [ImageFormatKind.Netpbm], (reader, format, options) => NetpbmMetadataParser.Parse(reader, format, options)),
        new MetadataParser("PcxMetadataParser", [ImageFormatKind.Pcx], (reader, format, options) => PcxMetadataParser.Parse(reader, format, options)),
        new MetadataParser("TgaMetadataParser", [ImageFormatKind.Tga], (reader, format, options) => TgaMetadataParser.Parse(reader, format, options)),
        new MetadataParser("DdsMetadataParser", [ImageFormatKind.Dds], (reader, format, options) => DdsMetadataParser.Parse(reader, format, options)),
        new MetadataParser("QoiMetadataParser", [ImageFormatKind.Qoi], (reader, format, options) => QoiMetadataParser.Parse(reader, format, options)),
        new MetadataParser("RadianceHdrMetadataParser", [ImageFormatKind.RadianceHdr], (reader, format, options) => RadianceHdrMetadataParser.Parse(reader, format, options)),
        new MetadataParser("EpsMetadataParser", [ImageFormatKind.Eps], (reader, format, options) => EpsMetadataParser.Parse(reader, format, options)),
        new MetadataParser("XmpSidecarMetadataParser", [ImageFormatKind.Xmp], (reader, format, options) => XmpSidecarMetadataParser.Parse(reader, format, options))
    ];

    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo? format, ImageMetadataReaderOptions? options)
    {
        ImageFormatInfo? actualFormat = format;
        if (actualFormat == null)
        {
            actualFormat = new ImageFormatInfo();
        }

        ImageMetadataReaderOptions? actualOptions = options;
        if (actualOptions == null)
        {
            actualOptions = new ImageMetadataReaderOptions();
        }

        IImageMetadataParser parser = GetParser(actualFormat.Kind);
        return ParseWithParser(reader, actualFormat, actualOptions, parser);
    }

    internal static ImageMetadataDocument ParseWithParser(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options, IImageMetadataParser? parser)
    {
        IImageMetadataParser? actualParser = parser;
        if (actualParser == null)
        {
            actualParser = UnknownParser;
        }

        ImageMetadataDocument document = actualParser.Parse(reader, format, options);
        document.RefreshDiagnosticStatus();
        return document;
    }

    internal static IImageMetadataParser GetParser(ImageFormatKind kind)
    {
        foreach (IImageMetadataParser parser in Parsers)
        {
            if (parser.Supports(kind))
            {
                return parser;
            }
        }

        return UnknownParser;
    }

    internal static string GetParserName(ImageFormatKind kind)
    {
        return GetParser(kind).GetName();
    }
}
