using System;
using System.Collections.Generic;

namespace image;

internal class BmffBrandDetector
{
    internal const int FileTypeFieldsSize = 8;
    internal const int BrandSize = 4;

    internal static BmffBrandInfo ReadBytes(byte[] bytes)
    {
        if (bytes == null)
        {
            return new BmffBrandInfo();
        }

        return ReadSpan(bytes);
    }

    internal static BmffBrandInfo ReadSpan(ReadOnlySpan<byte> bytes)
    {
        BmffBrandInfo info = new BmffBrandInfo();
        if (bytes.Length < BmffBoxTraverser.BasicHeaderSize + FileTypeFieldsSize)
        {
            return info;
        }

        if (ImageFormatDetectorCore.ReadAscii(bytes, 4, 4) != "ftyp")
        {
            return info;
        }

        uint size32 = ReadUnsigned32(bytes, 0);
        int dataOffset = BmffBoxTraverser.BasicHeaderSize;
        ulong declaredSize = size32;
        if (size32 == 1)
        {
            dataOffset = dataOffset + BmffBoxTraverser.LargeSizeFieldSize;
            if (bytes.Length < dataOffset + FileTypeFieldsSize)
            {
                return info;
            }

            declaredSize = ReadUnsigned64(bytes, 8);
        }
        else if (size32 == 0)
        {
            declaredSize = (ulong)bytes.Length;
        }

        if (declaredSize < (ulong)(dataOffset + FileTypeFieldsSize))
        {
            return info;
        }

        ulong visibleSize = declaredSize;
        if (visibleSize > (ulong)bytes.Length)
        {
            visibleSize = (ulong)bytes.Length;
        }

        info.MajorBrand = ImageFormatDetectorCore.ReadAscii(bytes, dataOffset, BrandSize);
        info.MinorVersion = ReadUnsigned32(bytes, dataOffset + BrandSize);
        int index = dataOffset + FileTypeFieldsSize;
        while ((ulong)(index + BrandSize) <= visibleSize)
        {
            info.CompatibleBrands.Add(ImageFormatDetectorCore.ReadAscii(bytes, index, BrandSize));
            index = index + BrandSize;
        }

        info.Valid = true;
        Classify(info);
        return info;
    }

    internal static uint ReadUnsigned32(ReadOnlySpan<byte> bytes, int offset)
    {
        if (offset < 0 || offset > bytes.Length - 4)
        {
            return 0;
        }

        return ((uint)bytes[offset] << 24)
            | ((uint)bytes[offset + 1] << 16)
            | ((uint)bytes[offset + 2] << 8)
            | bytes[offset + 3];
    }

    internal static ulong ReadUnsigned64(ReadOnlySpan<byte> bytes, int offset)
    {
        if (offset < 0 || offset > bytes.Length - 8)
        {
            return 0;
        }

        return ((ulong)bytes[offset] << 56)
            | ((ulong)bytes[offset + 1] << 48)
            | ((ulong)bytes[offset + 2] << 40)
            | ((ulong)bytes[offset + 3] << 32)
            | ((ulong)bytes[offset + 4] << 24)
            | ((ulong)bytes[offset + 5] << 16)
            | ((ulong)bytes[offset + 6] << 8)
            | bytes[offset + 7];
    }

    internal static BmffBrandInfo ReadBox(ImageMetadataDocument document, BoundedStreamReader reader, BmffBox box, ImageMetadataReaderOptions options)
    {
        BmffBrandInfo info = new BmffBrandInfo();
        if (document == null || reader == null || box == null || box.Type != "ftyp")
        {
            return info;
        }

        if (box.DataSize < FileTypeFieldsSize || !reader.SeekAbsolute(box.DataOffset) || !reader.CanRead(FileTypeFieldsSize))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "bmff.ftyp.fields.truncated", "ISO BMFF file type fields were truncated.", "ftyp", box.DataOffset, FileTypeFieldsSize);
            return info;
        }

        byte[] majorBytes = reader.ReadBytes(BrandSize);
        info.MajorBrand = MetadataStringReader.DecodeLatin1(majorBytes);
        info.MinorVersion = BmffBoxTraverser.ReadUnsigned32(reader.ReadBytes(BrandSize));
        ValidateBrand(document, info.MajorBrand, majorBytes, box.DataOffset);

        ulong compatibleBytes = box.DataSize - FileTypeFieldsSize;
        ulong compatibleCount = compatibleBytes / BrandSize;
        ulong availableCompatibleCount = compatibleCount;
        ulong maximumBrands = ToUnsignedCount(GetMaxBrandCount(options));
        if (compatibleCount > maximumBrands)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "bmff.ftyp.brand.count.limit", "ISO BMFF compatible brand count exceeded the configured tag count limit.", "ftyp", box.DataOffset + FileTypeFieldsSize, BmffBoxTraverser.ToDiagnosticLength(compatibleBytes));
            compatibleCount = maximumBrands;
        }

        ulong index = 0;
        while (index < compatibleCount)
        {
            byte[] brandBytes = reader.ReadBytes(BrandSize);
            string brand = MetadataStringReader.DecodeLatin1(brandBytes);
            info.CompatibleBrands.Add(brand);
            ValidateBrand(document, brand, brandBytes, box.DataOffset + FileTypeFieldsSize + (long)(index * BrandSize));
            index = index + 1;
        }

        ulong remainder = compatibleBytes % BrandSize;
        if (remainder != 0)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "bmff.ftyp.compatible-brand.truncated", "ISO BMFF compatible brand field ended with an incomplete FourCC.", "ftyp", box.DataOffset + FileTypeFieldsSize + (long)(availableCompatibleCount * BrandSize), (long)remainder);
        }

        info.Valid = true;
        Classify(info);
        return info;
    }

    internal static void Extract(ImageMetadataDocument document, BoundedStreamReader reader, List<BmffBox> boxes, ImageMetadataReaderOptions options)
    {
        if (document == null || reader == null || boxes == null)
        {
            return;
        }

        foreach (BmffBox box in boxes)
        {
            if (box != null && box.Type == "ftyp" && BoxPayloadAvailable(box, reader))
            {
                box.BrandInfo = ReadBox(document, reader, box, options);
                if (box.BrandInfo.Valid)
                {
                    ApplyFormat(document.Format, box.BrandInfo);
                }

                return;
            }
        }
    }

    internal static bool BoxPayloadAvailable(BmffBox box, BoundedStreamReader reader)
    {
        if (box == null || reader == null || box.DataLimited || box.DataOffset < 0)
        {
            return false;
        }

        long accessible = reader.AccessibleLength();
        if (box.DataOffset > accessible)
        {
            return false;
        }

        return box.DataSize <= (ulong)(accessible - box.DataOffset);
    }

    internal static void ApplyFormat(ImageFormatInfo format, BmffBrandInfo info)
    {
        if (format == null || info == null || !info.Valid)
        {
            return;
        }

        format.ContainerBrand = info.MajorBrand;
        if (info.Kind == ImageFormatKind.Unknown)
        {
            return;
        }

        format.Kind = info.Kind;
        ImageFormatMapping.Apply(format);
        format.Name = info.Name;
        format.Confidence = info.Confidence;
        format.DetectedByMagic = true;
    }

    internal static void Classify(BmffBrandInfo info)
    {
        if (info == null)
        {
            return;
        }

        ImageFormatKind majorKind = ClassifyBrand(info.MajorBrand);
        if (majorKind != ImageFormatKind.Unknown && majorKind != ImageFormatKind.Heif)
        {
            SetClassification(info, majorKind, false);
            return;
        }

        foreach (string brand in info.CompatibleBrands)
        {
            ImageFormatKind compatibleKind = ClassifyBrand(brand);
            if (compatibleKind != ImageFormatKind.Unknown && compatibleKind != ImageFormatKind.Heif)
            {
                SetClassification(info, compatibleKind, true);
                return;
            }
        }

        if (majorKind == ImageFormatKind.Heif)
        {
            SetClassification(info, ImageFormatKind.Heif, false);
            return;
        }

        foreach (string brand in info.CompatibleBrands)
        {
            if (ClassifyBrand(brand) == ImageFormatKind.Heif)
            {
                SetClassification(info, ImageFormatKind.Heif, true);
                return;
            }
        }
    }

    internal static ImageFormatKind ClassifyBrand(string brand)
    {
        if (IsCr3Brand(brand))
        {
            return ImageFormatKind.Cr3;
        }

        if (IsAvifBrand(brand))
        {
            return ImageFormatKind.Avif;
        }

        if (IsHevcImageBrand(brand))
        {
            return ImageFormatKind.Heic;
        }

        if (IsJpeg2000Brand(brand))
        {
            return ImageFormatKind.Jpeg2000;
        }

        if (IsHeifBrand(brand))
        {
            return ImageFormatKind.Heif;
        }

        return ImageFormatKind.Unknown;
    }

    internal static bool IsCr3Brand(string brand)
    {
        return brand == "crx ";
    }

    internal static bool IsAvifBrand(string brand)
    {
        return brand == "avif"
            || brand == "avis"
            || brand == "avio"
            || brand == "MA1A"
            || brand == "MA1B";
    }

    internal static bool IsHevcImageBrand(string brand)
    {
        return brand == "heic"
            || brand == "heix"
            || brand == "hevc"
            || brand == "hevx"
            || brand == "heim"
            || brand == "heis"
            || brand == "hevm"
            || brand == "hevs"
            || brand == "MiHA"
            || brand == "MiHB"
            || brand == "MiHE";
    }

    internal static bool IsJpeg2000Brand(string brand)
    {
        return brand == "jp2 " || brand == "jpx " || brand == "jpm ";
    }

    internal static bool IsHeifBrand(string brand)
    {
        return brand == "mif1"
            || brand == "mif2"
            || brand == "msf1"
            || brand == "heif"
            || brand == "1pic"
            || brand == "avci"
            || brand == "avcs"
            || brand == "evbi"
            || brand == "evbs"
            || brand == "evmi"
            || brand == "evms"
            || brand == "jpeg"
            || brand == "jpgs"
            || brand == "j2ki"
            || brand == "j2ks"
            || brand == "j2is"
            || brand == "jxsi"
            || brand == "jxss"
            || brand == "vvic"
            || brand == "vvis"
            || brand == "pred"
            || brand == "miaf"
            || brand == "MiAB"
            || brand == "MiAC"
            || brand == "MiAn"
            || brand == "MiBu"
            || brand == "MiCm"
            || brand == "MiPr";
    }

    internal static void SetClassification(BmffBrandInfo info, ImageFormatKind kind, bool compatible)
    {
        info.Kind = kind;
        info.ClassifiedByCompatibleBrand = compatible;
        if (compatible)
        {
            info.Confidence = 90;
        }
        else if (kind == ImageFormatKind.Heif)
        {
            info.Confidence = 95;
        }
        else
        {
            info.Confidence = 100;
        }

        ImageFormatInfo format = ImageFormatMapping.CreateInfo(kind);
        info.Name = format.Name;
    }

    internal static void ValidateBrand(ImageMetadataDocument document, string brand, byte[] bytes, long offset)
    {
        if (!BmffBoxTraverser.IsValidFourCc(bytes))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "bmff.ftyp.brand.invalid", "ISO BMFF brand contained a non-printable byte.", brand, offset, BrandSize);
        }
    }

    internal static byte[] CopyBytes(byte[] bytes, int offset, int count)
    {
        byte[] result = new byte[count];
        int index = 0;
        while (index < count)
        {
            result[index] = bytes[offset + index];
            index = index + 1;
        }

        return result;
    }

    internal static int GetMaxBrandCount(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxTagCount : options.MaxTagCount;
    }

    internal static ulong ToUnsignedCount(int value)
    {
        if (value < 0)
        {
            return 0;
        }

        return (ulong)value;
    }
}
