namespace image;

internal class RawMetadataDispatcher
{
    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo? format, ImageMetadataReaderOptions options)
    {
        ImageFormatInfo actualFormat = format == null ? new ImageFormatInfo() : format;
        RawContainerKind container = Classify(actualFormat.Kind);
        switch (container)
        {
            case RawContainerKind.Tiff:
                return ParseTiff(reader, actualFormat, options);
            case RawContainerKind.Bmff:
                return BmffMetadataParser.Parse(reader, actualFormat, options);
            case RawContainerKind.Ciff:
                return CiffMetadataParser.Parse(reader, actualFormat, options);
            case RawContainerKind.Raf:
                return RafMetadataParser.Parse(reader, actualFormat, options);
            default:
                return Unsupported(reader, actualFormat);
        }
    }

    internal static RawContainerKind Classify(ImageFormatKind kind)
    {
        switch (kind)
        {
            case ImageFormatKind.Dng:
            case ImageFormatKind.Nef:
            case ImageFormatKind.Arw:
            case ImageFormatKind.Orf:
            case ImageFormatKind.Pef:
            case ImageFormatKind.Rw2:
            case ImageFormatKind.Srw:
            case ImageFormatKind.ThreeFr:
            case ImageFormatKind.Kdc:
            case ImageFormatKind.Mrw:
            case ImageFormatKind.Rwl:
            case ImageFormatKind.Gpr:
            case ImageFormatKind.Cr2:
                return RawContainerKind.Tiff;
            case ImageFormatKind.Cr3:
                return RawContainerKind.Bmff;
            case ImageFormatKind.Crw:
                return RawContainerKind.Ciff;
            case ImageFormatKind.Raf:
                return RawContainerKind.Raf;
            default:
                return RawContainerKind.Unknown;
        }
    }

    internal static ImageMetadataDocument ParseTiff(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        if (format != null && format.Kind == ImageFormatKind.Cr2)
        {
            return Cr2MetadataParser.Parse(reader, format, options);
        }

        if (format != null)
        {
            return RawTiffProfileParser.Parse(reader, format, options);
        }

        ImageFormatInfo rawFormat = BasicMetadataParser.CopyFormat(format);
        ImageMetadataDocument document = TiffMetadataParser.Parse(reader, rawFormat, options);
        document.Format = rawFormat;
        return document;
    }

    internal static ImageMetadataDocument Unsupported(BoundedStreamReader? reader, ImageFormatInfo format)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = BasicMetadataParser.CopyFormat(format);
        if (reader != null)
        {
            document.ByteLength = reader.Length;
            CopyReaderDiagnostics(document, reader);
        }

        MetadataDiagnostic.AddUnsupported(document.Diagnostics, "raw.container.unsupported", "RAW format did not map to a supported container family.", "RawMetadataDispatcher");
        return document;
    }

    internal static void CopyReaderDiagnostics(ImageMetadataDocument document, BoundedStreamReader reader)
    {
        if (document == null || reader == null || reader.Diagnostics == null)
        {
            return;
        }

        foreach (MetadataDiagnostic diagnostic in reader.Diagnostics)
        {
            document.Diagnostics.Add(diagnostic);
        }
    }
}
