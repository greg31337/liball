namespace image;

internal class ImageFormatMapping
{
    internal static ImageFormatInfo CreateInfo(ImageFormatKind kind)
    {
        ImageFormatInfo info = new ImageFormatInfo();
        info.Kind = kind;
        Apply(info);
        return info;
    }

    internal static void Apply(ImageFormatInfo info)
    {
        if (info == null)
        {
            return;
        }

        switch (info.Kind)
        {
            case ImageFormatKind.Jpeg:
                Set(info, "JPEG", "image/jpeg", ".jpg");
                break;
            case ImageFormatKind.Tiff:
                Set(info, "TIFF", "image/tiff", ".tif");
                break;
            case ImageFormatKind.BigTiff:
                Set(info, "BigTIFF", "image/tiff", ".btf");
                break;
            case ImageFormatKind.Png:
                Set(info, "PNG", "image/png", ".png");
                break;
            case ImageFormatKind.Gif:
                Set(info, "GIF", "image/gif", ".gif");
                break;
            case ImageFormatKind.WebP:
                Set(info, "WebP", "image/webp", ".webp");
                break;
            case ImageFormatKind.Bmp:
                Set(info, "BMP", "image/bmp", ".bmp");
                break;
            case ImageFormatKind.Ico:
                Set(info, "ICO", "image/x-icon", ".ico");
                break;
            case ImageFormatKind.Cur:
                Set(info, "CUR", "image/x-icon", ".cur");
                break;
            case ImageFormatKind.Svg:
                Set(info, "SVG", "image/svg+xml", ".svg");
                break;
            case ImageFormatKind.Svgz:
                Set(info, "SVGZ", "image/svg+xml", ".svgz");
                break;
            case ImageFormatKind.Heif:
                Set(info, "HEIF", "image/heif", ".heif");
                break;
            case ImageFormatKind.Heic:
                Set(info, "HEIC", "image/heic", ".heic");
                break;
            case ImageFormatKind.Avif:
                Set(info, "AVIF", "image/avif", ".avif");
                break;
            case ImageFormatKind.Cr3:
                Set(info, "CR3", "image/x-canon-cr3", ".cr3");
                break;
            case ImageFormatKind.Jpeg2000:
                Set(info, "JPEG 2000", "image/jp2", ".jp2");
                break;
            case ImageFormatKind.JpegXl:
                Set(info, "JPEG XL", "image/jxl", ".jxl");
                break;
            case ImageFormatKind.Psd:
                Set(info, "PSD", "image/vnd.adobe.photoshop", ".psd");
                break;
            case ImageFormatKind.Psb:
                Set(info, "PSB", "image/vnd.adobe.photoshop", ".psb");
                break;
            case ImageFormatKind.Dng:
                Set(info, "DNG", "image/x-adobe-dng", ".dng");
                break;
            case ImageFormatKind.Nef:
                Set(info, "NEF", "image/x-nikon-nef", ".nef");
                break;
            case ImageFormatKind.Arw:
                Set(info, "ARW", "image/x-sony-arw", ".arw");
                break;
            case ImageFormatKind.Orf:
                Set(info, "ORF", "image/x-olympus-orf", ".orf");
                break;
            case ImageFormatKind.Pef:
                Set(info, "PEF", "image/x-pentax-pef", ".pef");
                break;
            case ImageFormatKind.Rw2:
                Set(info, "RW2", "image/x-panasonic-rw2", ".rw2");
                break;
            case ImageFormatKind.Srw:
                Set(info, "SRW", "image/x-samsung-srw", ".srw");
                break;
            case ImageFormatKind.ThreeFr:
                Set(info, "3FR", "image/x-hasselblad-3fr", ".3fr");
                break;
            case ImageFormatKind.Kdc:
                Set(info, "KDC", "image/x-kodak-kdc", ".kdc");
                break;
            case ImageFormatKind.Mrw:
                Set(info, "MRW", "image/x-minolta-mrw", ".mrw");
                break;
            case ImageFormatKind.Rwl:
                Set(info, "RWL", "image/x-leica-rwl", ".rwl");
                break;
            case ImageFormatKind.Gpr:
                Set(info, "GPR", "image/x-gopro-gpr", ".gpr");
                break;
            case ImageFormatKind.Cr2:
                Set(info, "CR2", "image/x-canon-cr2", ".cr2");
                break;
            case ImageFormatKind.Crw:
                Set(info, "CRW", "image/x-canon-crw", ".crw");
                break;
            case ImageFormatKind.Raf:
                Set(info, "RAF", "image/x-fuji-raf", ".raf");
                break;
            case ImageFormatKind.Netpbm:
                Set(info, "Netpbm", "image/x-portable-anymap", ".pnm");
                break;
            case ImageFormatKind.Pcx:
                Set(info, "PCX", "image/x-pcx", ".pcx");
                break;
            case ImageFormatKind.Tga:
                Set(info, "TGA", "image/x-tga", ".tga");
                break;
            case ImageFormatKind.Dds:
                Set(info, "DDS", "image/vnd.ms-dds", ".dds");
                break;
            case ImageFormatKind.Qoi:
                Set(info, "QOI", "image/qoi", ".qoi");
                break;
            case ImageFormatKind.RadianceHdr:
                Set(info, "Radiance HDR", "image/vnd.radiance", ".hdr");
                break;
            case ImageFormatKind.Eps:
                Set(info, "EPS", "application/postscript", ".eps");
                break;
            case ImageFormatKind.Xmp:
                Set(info, "XMP", "application/rdf+xml", ".xmp");
                break;
            default:
                Set(info, "Unknown", "application/octet-stream", "");
                break;
        }
    }

    internal static string GetName(ImageFormatKind kind)
    {
        ImageFormatInfo info = CreateInfo(kind);
        return info.Name;
    }

    internal static string GetMimeType(ImageFormatKind kind)
    {
        ImageFormatInfo info = CreateInfo(kind);
        return info.MimeType;
    }

    internal static string GetPreferredExtension(ImageFormatKind kind)
    {
        ImageFormatInfo info = CreateInfo(kind);
        return info.PreferredExtension;
    }

    internal static ImageFormatKind KindFromFileNameOrExtension(string? fileNameOrExtension)
    {
        if (fileNameOrExtension == null || fileNameOrExtension.Length == 0)
        {
            return ImageFormatKind.Unknown;
        }

        int separator = fileNameOrExtension.LastIndexOf('/');
        int alternateSeparator = fileNameOrExtension.LastIndexOf('\\');
        if (alternateSeparator > separator)
        {
            separator = alternateSeparator;
        }

        int dot = fileNameOrExtension.LastIndexOf('.');
        string extension;
        if (dot > separator)
        {
            extension = fileNameOrExtension.Substring(dot);
        }
        else if (separator < 0)
        {
            extension = "." + fileNameOrExtension;
        }
        else
        {
            return ImageFormatKind.Unknown;
        }

        return KindFromExtension(extension);
    }

    internal static ImageFormatKind KindFromExtension(string extension)
    {
        string value = extension == null ? "" : extension.ToLowerInvariant();
        switch (value)
        {
            case ".jpg":
            case ".jpeg":
            case ".jpe":
            case ".jls":
                return ImageFormatKind.Jpeg;
            case ".tif":
            case ".tiff":
                return ImageFormatKind.Tiff;
            case ".btf":
            case ".bigtiff":
                return ImageFormatKind.BigTiff;
            case ".png":
                return ImageFormatKind.Png;
            case ".gif":
                return ImageFormatKind.Gif;
            case ".webp":
                return ImageFormatKind.WebP;
            case ".bmp":
            case ".dib":
                return ImageFormatKind.Bmp;
            case ".ico":
                return ImageFormatKind.Ico;
            case ".cur":
                return ImageFormatKind.Cur;
            case ".svg":
                return ImageFormatKind.Svg;
            case ".svgz":
                return ImageFormatKind.Svgz;
            case ".heif":
            case ".hif":
                return ImageFormatKind.Heif;
            case ".heic":
                return ImageFormatKind.Heic;
            case ".avif":
                return ImageFormatKind.Avif;
            case ".cr3":
                return ImageFormatKind.Cr3;
            case ".jp2":
            case ".j2k":
            case ".j2c":
            case ".jpc":
            case ".jpm":
            case ".jpx":
                return ImageFormatKind.Jpeg2000;
            case ".jxl":
                return ImageFormatKind.JpegXl;
            case ".psd":
                return ImageFormatKind.Psd;
            case ".psb":
                return ImageFormatKind.Psb;
            case ".dng":
                return ImageFormatKind.Dng;
            case ".nef":
                return ImageFormatKind.Nef;
            case ".arw":
                return ImageFormatKind.Arw;
            case ".orf":
                return ImageFormatKind.Orf;
            case ".pef":
                return ImageFormatKind.Pef;
            case ".rw2":
                return ImageFormatKind.Rw2;
            case ".srw":
                return ImageFormatKind.Srw;
            case ".3fr":
                return ImageFormatKind.ThreeFr;
            case ".kdc":
                return ImageFormatKind.Kdc;
            case ".mrw":
                return ImageFormatKind.Mrw;
            case ".rwl":
                return ImageFormatKind.Rwl;
            case ".gpr":
                return ImageFormatKind.Gpr;
            case ".cr2":
                return ImageFormatKind.Cr2;
            case ".crw":
                return ImageFormatKind.Crw;
            case ".raf":
                return ImageFormatKind.Raf;
            case ".pbm":
            case ".pgm":
            case ".ppm":
            case ".pam":
            case ".pfm":
            case ".pnm":
                return ImageFormatKind.Netpbm;
            case ".pcx":
                return ImageFormatKind.Pcx;
            case ".tga":
                return ImageFormatKind.Tga;
            case ".dds":
                return ImageFormatKind.Dds;
            case ".qoi":
                return ImageFormatKind.Qoi;
            case ".hdr":
            case ".rgbe":
                return ImageFormatKind.RadianceHdr;
            case ".eps":
            case ".epsf":
            case ".ps":
                return ImageFormatKind.Eps;
            case ".xmp":
                return ImageFormatKind.Xmp;
            default:
                return ImageFormatKind.Unknown;
        }
    }

    internal static bool IsTiffDerivedRaw(ImageFormatKind kind)
    {
        return kind == ImageFormatKind.Dng
            || kind == ImageFormatKind.Nef
            || kind == ImageFormatKind.Arw
            || kind == ImageFormatKind.Orf
            || kind == ImageFormatKind.Pef
            || kind == ImageFormatKind.Rw2
            || kind == ImageFormatKind.Srw
            || kind == ImageFormatKind.ThreeFr
            || kind == ImageFormatKind.Kdc
            || kind == ImageFormatKind.Rwl
            || kind == ImageFormatKind.Gpr
            || kind == ImageFormatKind.Cr2;
    }

    internal static bool IsRawProfile(ImageFormatKind kind)
    {
        return IsTiffDerivedRaw(kind) || kind == ImageFormatKind.Mrw;
    }

    internal static bool IsBmffImage(ImageFormatKind kind)
    {
        return kind == ImageFormatKind.Heif
            || kind == ImageFormatKind.Heic
            || kind == ImageFormatKind.Avif
            || kind == ImageFormatKind.Cr3
            || kind == ImageFormatKind.Jpeg2000;
    }

    internal static void Set(ImageFormatInfo info, string name, string mimeType, string preferredExtension)
    {
        info.Name = name;
        info.MimeType = mimeType;
        info.PreferredExtension = preferredExtension;
    }
}
