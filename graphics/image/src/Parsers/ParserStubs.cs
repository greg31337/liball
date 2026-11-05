namespace image;

internal class BasicMetadataParser
{
    internal static ImageMetadataDocument CreateDocument(BoundedStreamReader? reader, ImageFormatInfo format, string parserName)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = CopyFormat(format);

        if (reader != null)
        {
            document.ByteLength = reader.Length;
            if (reader.Diagnostics != null)
            {
                foreach (MetadataDiagnostic diagnostic in reader.Diagnostics)
                {
                    document.Diagnostics.Add(diagnostic);
                }
            }
        }

        if (format != null && format.Diagnostics != null)
        {
            foreach (MetadataDiagnostic diagnostic in format.Diagnostics)
            {
                document.Diagnostics.Add(diagnostic);
            }
        }

        MetadataDiagnostic.AddUnsupported(document.Diagnostics, "parser.not.implemented", parserName + " has not implemented metadata extraction yet.", parserName);
        return document;
    }

    internal static ImageFormatInfo CopyFormat(ImageFormatInfo? format)
    {
        ImageFormatInfo copy = new ImageFormatInfo();

        if (format == null)
        {
            return copy;
        }

        copy.Kind = format.Kind;
        copy.Name = format.Name;
        copy.MimeType = format.MimeType;
        copy.PreferredExtension = format.PreferredExtension;
        copy.ContainerBrand = format.ContainerBrand;
        copy.Confidence = format.Confidence;
        copy.DetectedByMagic = format.DetectedByMagic;
        if (format.Diagnostics != null)
        {
            foreach (MetadataDiagnostic diagnostic in format.Diagnostics)
            {
                copy.Diagnostics.Add(diagnostic);
            }
        }
        ImageFormatMapping.Apply(copy);

        if (format.Name.Length > 0)
        {
            copy.Name = format.Name;
        }

        return copy;
    }
}

internal class UnknownMetadataParser
{
    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        return BasicMetadataParser.CreateDocument(reader, format, "UnknownMetadataParser");
    }
}

internal class JpegMetadataParser
{
    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions? options)
    {
        if (options == null)
        {
            options = new ImageMetadataReaderOptions();
        }

        return JpegFrameExtractor.Parse(reader, format, options);
    }
}

internal class TiffMetadataParser
{
    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        return TiffHeaderParser.Parse(reader, format, options);
    }
}

internal class PngMetadataParser
{
    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions? options)
    {
        if (options == null)
        {
            options = new ImageMetadataReaderOptions();
        }

        return PngChunkTraverser.Parse(reader, format, options);
    }
}

internal class GifMetadataParser
{
    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        return GifBlockParser.Parse(reader, format, options);
    }
}

internal class WebPMetadataParser
{
    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        return RiffChunkTraverser.Parse(reader, format, options);
    }
}

internal class BmpMetadataParser
{
    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        return BmpHeaderParser.Parse(reader, format, options);
    }
}

internal class BmffMetadataParser
{
    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        return BmffBoxTraverser.Parse(reader, format, options);
    }
}

internal class Jpeg2000MetadataParser
{
    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        return Jpeg2000BoxTraverser.Parse(reader, format, options);
    }
}

internal class JpegXlMetadataParser
{
    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        return JpegXlBoxTraverser.Parse(reader, format, options);
    }
}

internal class PsdMetadataParser
{
    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        return PsdDocumentParser.Parse(reader, format, options);
    }
}

internal class RawMetadataParser
{
    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        return RawMetadataDispatcher.Parse(reader, format, options);
    }
}

internal class XmpSidecarMetadataParser
{
    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        return XmpSidecarReader.Parse(reader, format, options);
    }
}
