using System;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class PublicEnumValueTests
{
    [Test]
    public void EveryPublicEnumMember_HasItsStableNameAndIntegerValue()
    {
        AssertEnum(typeof(ImageColorSpaceKind), new string[] { "Unknown", "Gray", "Rgb", "Cmyk", "YCbCr", "Lab", "Indexed", "Hsv", "Hsl", "Xyz", "WideGamutRgb", "DisplayP3", "AdobeRgb", "ProPhotoRgb" }, Sequence(0, 13));
        AssertEnum(typeof(ImageCompressionKind), new string[] { "Unknown", "None", "Jpeg", "Deflate", "Lzw", "PackBits", "Rle", "WebP", "Jpeg2000", "JpegXl", "Bzip2", "Zstd", "Lzma" }, Sequence(0, 12));
        AssertEnum(typeof(ImageFormatKind), new string[] { "Unknown", "Jpeg", "Tiff", "BigTiff", "Png", "Gif", "WebP", "Bmp", "Ico", "Cur", "Svg", "Svgz", "Heif", "Heic", "Avif", "Cr3", "Jpeg2000", "JpegXl", "Psd", "Psb", "Dng", "Nef", "Arw", "Orf", "Pef", "Rw2", "Srw", "ThreeFr", "Kdc", "Mrw", "Rwl", "Gpr", "Cr2", "Crw", "Raf", "Netpbm", "Pcx", "Tga", "Dds", "Qoi", "RadianceHdr", "Eps", "Xmp" }, Sequence(0, 42));
        AssertEnum(typeof(ImageMetadataHashStatus), new string[] { "Unknown", "Success", "EmptyMetadata", "UnsupportedSeparation", "UnsupportedAlgorithm", "InvalidInput" }, Sequence(0, 5));
        AssertEnum(typeof(ImageMetadataRole), new string[] { "Unknown", "Primary", "Page", "Thumbnail", "Preview", "Auxiliary", "AuxiliaryAlpha", "AuxiliaryDepth" }, Sequence(0, 7));
        AssertEnum(typeof(ImageOrientation), new string[] { "Unknown", "Normal", "MirrorHorizontal", "Rotate180", "MirrorVertical", "MirrorHorizontalRotate270", "Rotate90", "MirrorHorizontalRotate90", "Rotate270" }, Sequence(0, 8));
        AssertEnum(typeof(MetadataDatePrecision), new string[] { "Unknown", "Year", "Month", "Day", "Hour", "Minute", "Second", "FractionalSecond" }, Sequence(0, 7));
        AssertEnum(typeof(MetadataDiagnosticsVerbosity), new string[] { "Quiet", "Normal", "Verbose" }, Sequence(0, 2));
        AssertEnum(typeof(MetadataFamily), new string[] { "Unknown", "File", "Format", "Image", "Container", "Jpeg", "Jfif", "Tiff", "Exif", "Gps", "Iptc", "Xmp", "Icc", "Photoshop", "Png", "Gif", "WebP", "Bmp", "Ico", "Svg", "Bmff", "Jpeg2000", "JpegXl", "Psd", "Raw", "MakerNote", "Thumbnail", "Diagnostic" }, Sequence(0, 27));
        AssertEnum(typeof(MetadataProfileKind), new string[] { "Unknown", "Exif", "Xmp", "Iptc", "Icc", "PhotoshopImageResource", "Jumbf", "C2pa", "MakerNote", "Thumbnail" }, Sequence(0, 9));
        AssertEnum(typeof(MetadataSeverity), new string[] { "Info", "Warning", "RecoverableError", "Unsupported", "Truncated", "LimitExceeded" }, Sequence(0, 5));
        AssertEnum(typeof(MetadataValueKind), new string[] { "Empty", "Integer", "UnsignedInteger", "Rational", "SignedRational", "Double", "String", "StringList", "ByteArray", "DateText", "XmlText", "DirectoryReference", "Boolean", "IntegerArray", "UnsignedIntegerArray", "RationalArray", "SignedRationalArray", "DoubleArray" }, Sequence(0, 17));
    }

    public static void AssertEnum(Type enumType, string[] expectedNames, int[] expectedValues)
    {
        string[] names = Enum.GetNames(enumType);
        Array values = Enum.GetValues(enumType);
        int[] integers = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            object? value = values.GetValue(index);
            integers[index] = Convert.ToInt32(value);
            index = index + 1;
        }

        Assert.That(names, Is.EqualTo(expectedNames), enumType.Name + " names");
        Assert.That(integers, Is.EqualTo(expectedValues), enumType.Name + " values");
    }

    public static int[] Sequence(int first, int last)
    {
        int[] values = new int[last - first + 1];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = first + index;
            index = index + 1;
        }

        return values;
    }
}
