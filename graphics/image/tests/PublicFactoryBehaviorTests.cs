using System;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class PublicFactoryBehaviorTests
{
    [Test]
    public void ObjectFactoryOperations_PreserveInputsAndInitializeRemainingFields()
    {
        IImageMetadataFactory factory = new ImageMetadataFactory();
        GpsCoordinate gps = factory.CreateGpsCoordinate(34, 30, 0, "S", 118, 15, 0, "W", 12.5, "1");
        ImageFormatInfo format = factory.CreateFormatInfo(ImageFormatKind.Png, "PNG", "image/png", "png");
        MetadataDiagnostic diagnostic = factory.CreateDiagnostic(MetadataSeverity.Warning, "warning.code", "Warning text");
        MetadataDirectory directory = factory.CreateDirectory(MetadataFamily.Exif, "IFD0", "Image File Directory");
        MetadataValue tagValue = factory.CreateStringValue("Canon");
        MetadataTag tag = factory.CreateTag("Exif.IFD0.Make", "Make", MetadataFamily.Exif, tagValue);

        Assert.That(gps.IsValid, Is.True);
        Assert.That(gps.Latitude, Is.EqualTo(-34.5));
        Assert.That(gps.Longitude, Is.EqualTo(-118.25));
        Assert.That(gps.Altitude, Is.EqualTo(-12.5));
        Assert.That(gps.LatitudeReference, Is.EqualTo("S"));
        Assert.That(gps.LongitudeReference, Is.EqualTo("W"));
        Assert.That(gps.AltitudeReference, Is.EqualTo("1"));
        Assert.That(format.Kind, Is.EqualTo(ImageFormatKind.Png));
        Assert.That(format.Name, Is.EqualTo("PNG"));
        Assert.That(format.MimeType, Is.EqualTo("image/png"));
        Assert.That(format.PreferredExtension, Is.EqualTo("png"));
        Assert.That(format.ContainerBrand, Is.Empty);
        Assert.That(format.Confidence, Is.Zero);
        Assert.That(format.DetectedByMagic, Is.False);
        Assert.That(format.Diagnostics, Is.Empty);
        Assert.That(diagnostic.Severity, Is.EqualTo(MetadataSeverity.Warning));
        Assert.That(diagnostic.Code, Is.EqualTo("warning.code"));
        Assert.That(diagnostic.Message, Is.EqualTo("Warning text"));
        Assert.That(diagnostic.BlockType, Is.Empty);
        Assert.That(diagnostic.Offset, Is.EqualTo(-1));
        Assert.That(diagnostic.Length, Is.Zero);
        Assert.That(directory.Family, Is.EqualTo(MetadataFamily.Exif));
        Assert.That(directory.Group, Is.EqualTo("IFD0"));
        Assert.That(directory.Name, Is.EqualTo("Image File Directory"));
        Assert.That(directory.Tags, Is.Empty);
        Assert.That(tag.Key, Is.EqualTo("Exif.IFD0.Make"));
        Assert.That(tag.Name, Is.EqualTo("Make"));
        Assert.That(tag.Family, Is.EqualTo(MetadataFamily.Exif));
        Assert.That(tag.ValueType, Is.EqualTo(MetadataValueKind.String));
        Assert.That(tag.Value, Is.SameAs(tagValue));
    }

    [Test]
    public void ScalarValueFactoryOperations_PreserveKindsAndPayloads()
    {
        IImageMetadataFactory factory = new ImageMetadataFactory();
        MetadataDirectory directory = new MetadataDirectory();
        MetadataValue integer = factory.CreateIntegerValue(-7);
        MetadataValue unsigned = factory.CreateUnsignedIntegerValue(8);
        MetadataValue rational = factory.CreateRationalValue(1, 3);
        MetadataValue signedRational = factory.CreateSignedRationalValue(-2, 5);
        MetadataValue floating = factory.CreateDoubleValue(1.25);
        MetadataValue text = factory.CreateStringValue("text");
        MetadataValue date = factory.CreateDateTextValue("2026:08:25 12:34:56");
        MetadataValue xml = factory.CreateXmlTextValue("<x />");
        MetadataValue directoryReference = factory.CreateDirectoryValue(directory);
        MetadataValue boolean = factory.CreateBooleanValue(true);

        Assert.That(integer.Kind, Is.EqualTo(MetadataValueKind.Integer));
        Assert.That(integer.IntegerValue, Is.EqualTo(-7));
        Assert.That(unsigned.Kind, Is.EqualTo(MetadataValueKind.UnsignedInteger));
        Assert.That(unsigned.UnsignedIntegerValue, Is.EqualTo(8));
        Assert.That(rational.Kind, Is.EqualTo(MetadataValueKind.Rational));
        Assert.That(rational.Numerator, Is.EqualTo(1));
        Assert.That(rational.Denominator, Is.EqualTo(3));
        Assert.That(signedRational.Kind, Is.EqualTo(MetadataValueKind.SignedRational));
        Assert.That(signedRational.Numerator, Is.EqualTo(-2));
        Assert.That(signedRational.Denominator, Is.EqualTo(5));
        Assert.That(floating.Kind, Is.EqualTo(MetadataValueKind.Double));
        Assert.That(floating.DoubleValue, Is.EqualTo(1.25));
        Assert.That(text.Kind, Is.EqualTo(MetadataValueKind.String));
        Assert.That(text.Text, Is.EqualTo("text"));
        Assert.That(date.Kind, Is.EqualTo(MetadataValueKind.DateText));
        Assert.That(date.Text, Is.EqualTo("2026:08:25 12:34:56"));
        Assert.That(xml.Kind, Is.EqualTo(MetadataValueKind.XmlText));
        Assert.That(xml.Text, Is.EqualTo("<x />"));
        Assert.That(directoryReference.Kind, Is.EqualTo(MetadataValueKind.DirectoryReference));
        Assert.That(directoryReference.DirectoryValue, Is.SameAs(directory));
        Assert.That(boolean.Kind, Is.EqualTo(MetadataValueKind.Boolean));
        Assert.That(boolean.BooleanValue, Is.True);
    }

    [Test]
    public void ArrayValueFactoryOperations_CopyEveryInputAndSupportEmptySpans()
    {
        IImageMetadataFactory factory = new ImageMetadataFactory();
        long[] integers = new long[] { -1, 2 };
        ulong[] unsignedIntegers = new ulong[] { 3, 4 };
        long[] numerators = new long[] { 1, -2 };
        long[] denominators = new long[] { 3, 5 };
        double[] doubles = new double[] { 1.25, -2.5 };
        string[] strings = new string[] { "one", "two" };
        byte[] bytes = new byte[] { 6, 7 };

        MetadataValue integerArray = factory.CreateIntegerArrayValue(integers);
        MetadataValue unsignedArray = factory.CreateUnsignedIntegerArrayValue(unsignedIntegers);
        MetadataValue rationalArray = factory.CreateRationalArrayValue(numerators, denominators);
        MetadataValue signedRationalArray = factory.CreateSignedRationalArrayValue(numerators, denominators);
        MetadataValue doubleArray = factory.CreateDoubleArrayValue(doubles);
        MetadataValue stringList = factory.CreateStringListValue(strings);
        MetadataValue byteArray = factory.CreateBytesValue(bytes);

        integers[0] = 99;
        unsignedIntegers[0] = 99;
        numerators[0] = 99;
        denominators[0] = 99;
        doubles[0] = 99;
        strings[0] = "changed";
        bytes[0] = 99;

        Assert.That(integerArray.Kind, Is.EqualTo(MetadataValueKind.IntegerArray));
        Assert.That(integerArray.IntegerValues, Is.EqualTo(new long[] { -1, 2 }));
        Assert.That(unsignedArray.Kind, Is.EqualTo(MetadataValueKind.UnsignedIntegerArray));
        Assert.That(unsignedArray.UnsignedIntegerValues, Is.EqualTo(new ulong[] { 3, 4 }));
        Assert.That(rationalArray.Kind, Is.EqualTo(MetadataValueKind.RationalArray));
        Assert.That(rationalArray.Numerators, Is.EqualTo(new long[] { 1, -2 }));
        Assert.That(rationalArray.Denominators, Is.EqualTo(new long[] { 3, 5 }));
        Assert.That(signedRationalArray.Kind, Is.EqualTo(MetadataValueKind.SignedRationalArray));
        Assert.That(signedRationalArray.Numerators, Is.EqualTo(new long[] { 1, -2 }));
        Assert.That(signedRationalArray.Denominators, Is.EqualTo(new long[] { 3, 5 }));
        Assert.That(doubleArray.Kind, Is.EqualTo(MetadataValueKind.DoubleArray));
        Assert.That(doubleArray.DoubleValues, Is.EqualTo(new double[] { 1.25, -2.5 }));
        Assert.That(stringList.Kind, Is.EqualTo(MetadataValueKind.StringList));
        Assert.That(stringList.TextValues, Is.EqualTo(new string[] { "one", "two" }));
        Assert.That(byteArray.Kind, Is.EqualTo(MetadataValueKind.ByteArray));
        Assert.That(byteArray.Bytes, Is.EqualTo(new byte[] { 6, 7 }));
        Assert.That(factory.CreateIntegerArrayValue(ReadOnlySpan<long>.Empty).IntegerValues, Is.Empty);
        Assert.That(factory.CreateUnsignedIntegerArrayValue(ReadOnlySpan<ulong>.Empty).UnsignedIntegerValues, Is.Empty);
        Assert.That(factory.CreateRationalArrayValue(ReadOnlySpan<long>.Empty, ReadOnlySpan<long>.Empty).Numerators, Is.Empty);
        Assert.That(factory.CreateSignedRationalArrayValue(ReadOnlySpan<long>.Empty, ReadOnlySpan<long>.Empty).Denominators, Is.Empty);
        Assert.That(factory.CreateDoubleArrayValue(ReadOnlySpan<double>.Empty).DoubleValues, Is.Empty);
        Assert.That(factory.CreateStringListValue(ReadOnlySpan<string>.Empty).TextValues, Is.Empty);
        Assert.That(factory.CreateBytesValue(ReadOnlySpan<byte>.Empty).Bytes, Is.Empty);
    }

    [Test]
    public void RationalArrayFactoryOperations_RejectMismatchedPairCounts()
    {
        IImageMetadataFactory factory = new ImageMetadataFactory();

        Assert.Throws<ArgumentException>(() => factory.CreateRationalArrayValue(new long[] { 1 }, Array.Empty<long>()));
        Assert.Throws<ArgumentException>(() => factory.CreateSignedRationalArrayValue(Array.Empty<long>(), new long[] { 1 }));
    }

    [Test]
    public void ReaderOptionFactoryPresets_HaveExactIndependentValues()
    {
        IImageMetadataFactory factory = new ImageMetadataFactory();
        ImageMetadataReaderOptions defaults = factory.CreateDefaultReaderOptions();
        ImageMetadataReaderOptions secondDefaults = factory.CreateDefaultReaderOptions();
        ImageMetadataReaderOptions conservative = factory.CreateConservativeReaderOptions();
        ImageMetadataReaderOptions metadataOnly = factory.CreateMetadataOnlyReaderOptions();

        AssertDefaultOptions(defaults);
        AssertDefaultOptions(secondDefaults);
        AssertDefaultOptions(metadataOnly);
        Assert.That(secondDefaults, Is.Not.SameAs(defaults));
        Assert.That(metadataOnly, Is.Not.SameAs(defaults));
        Assert.That(conservative.MaxFileBytes, Is.EqualTo(134217728));
        Assert.That(conservative.MaxSegmentBytes, Is.EqualTo(8388608));
        Assert.That(conservative.MaxRawBlockBytes, Is.EqualTo(1048576));
        Assert.That(conservative.MaxProfileBytes, Is.EqualTo(4194304));
        Assert.That(conservative.MaxXmlBytes, Is.EqualTo(2097152));
        Assert.That(conservative.MaxDecompressedBytes, Is.EqualTo(4194304));
        Assert.That(conservative.MaxIfdCount, Is.EqualTo(512));
        Assert.That(conservative.MaxNestingDepth, Is.EqualTo(16));
        Assert.That(conservative.MaxTagCount, Is.EqualTo(8192));
        Assert.That(conservative.MaxFrameCount, Is.EqualTo(1024));
        Assert.That(conservative.CaptureRawBlocks, Is.True);
        Assert.That(conservative.CaptureRawBlockData, Is.False);
        Assert.That(conservative.ParseXmp, Is.True);
        Assert.That(conservative.ParseIcc, Is.True);
        Assert.That(conservative.PreserveMakerNotes, Is.True);
        Assert.That(conservative.DecodeMakerNotes, Is.False);
        Assert.That(conservative.DiagnosticsVerbosity, Is.EqualTo(MetadataDiagnosticsVerbosity.Normal));
    }

    public static void AssertDefaultOptions(ImageMetadataReaderOptions options)
    {
        Assert.That(options.MaxFileBytes, Is.EqualTo(536870912));
        Assert.That(options.MaxSegmentBytes, Is.EqualTo(67108864));
        Assert.That(options.MaxRawBlockBytes, Is.EqualTo(16777216));
        Assert.That(options.MaxProfileBytes, Is.EqualTo(33554432));
        Assert.That(options.MaxXmlBytes, Is.EqualTo(16777216));
        Assert.That(options.MaxDecompressedBytes, Is.EqualTo(33554432));
        Assert.That(options.MaxIfdCount, Is.EqualTo(4096));
        Assert.That(options.MaxNestingDepth, Is.EqualTo(64));
        Assert.That(options.MaxTagCount, Is.EqualTo(65536));
        Assert.That(options.MaxFrameCount, Is.EqualTo(10000));
        Assert.That(options.CaptureRawBlocks, Is.True);
        Assert.That(options.CaptureRawBlockData, Is.False);
        Assert.That(options.ParseXmp, Is.True);
        Assert.That(options.ParseIcc, Is.True);
        Assert.That(options.PreserveMakerNotes, Is.True);
        Assert.That(options.DecodeMakerNotes, Is.False);
        Assert.That(options.DiagnosticsVerbosity, Is.EqualTo(MetadataDiagnosticsVerbosity.Normal));
    }
}
