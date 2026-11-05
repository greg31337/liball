using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class BmffBrandDetectorTests
{
    [TestCase("avif")]
    [TestCase("avis")]
    [TestCase("avio")]
    [TestCase("MA1A")]
    [TestCase("MA1B")]
    public void DetectBytes_RecognizesAvifBrands(string brand)
    {
        ImageFormatInfo info = new ImageFormatDetector().DetectBytes(CreateFtyp(brand, 0, new string[0], false));

        Assert.That(info.Kind, Is.EqualTo(ImageFormatKind.Avif));
        Assert.That(info.Name, Is.EqualTo("AVIF"));
        Assert.That(info.ContainerBrand, Is.EqualTo(brand));
    }

    [TestCase("heic")]
    [TestCase("heix")]
    [TestCase("hevc")]
    [TestCase("hevx")]
    [TestCase("heim")]
    [TestCase("heis")]
    [TestCase("hevm")]
    [TestCase("hevs")]
    [TestCase("MiHB")]
    public void DetectBytes_RecognizesHevcImageBrands(string brand)
    {
        ImageFormatInfo info = new ImageFormatDetector().DetectBytes(CreateFtyp(brand, 0, new string[0], false));

        Assert.That(info.Kind, Is.EqualTo(ImageFormatKind.Heic));
        Assert.That(info.Name, Is.EqualTo("HEIC"));
    }

    [TestCase("mif1")]
    [TestCase("mif2")]
    [TestCase("msf1")]
    [TestCase("avci")]
    [TestCase("evmi")]
    [TestCase("jpeg")]
    [TestCase("j2ki")]
    [TestCase("jxsi")]
    [TestCase("vvic")]
    [TestCase("miaf")]
    public void DetectBytes_RecognizesRelatedHeifBrands(string brand)
    {
        ImageFormatInfo info = new ImageFormatDetector().DetectBytes(CreateFtyp(brand, 0, new string[0], false));

        Assert.That(info.Kind, Is.EqualTo(ImageFormatKind.Heif));
        Assert.That(info.Name, Is.EqualTo("HEIF"));
    }

    [Test]
    public void DetectBytes_UsesCompatibleBrandToRefineGenericOrUnknownMajorBrand()
    {
        ImageFormatInfo avif = new ImageFormatDetector().DetectBytes(CreateFtyp("mif1", 0, new string[] { "mif1", "avif", "MA1B" }, false));
        ImageFormatInfo heic = new ImageFormatDetector().DetectBytes(CreateFtyp("isom", 0, new string[] { "mif1", "heix" }, false));
        ImageFormatInfo cr3 = new ImageFormatDetector().DetectBytes(CreateFtyp("isom", 0, new string[] { "crx " }, false));

        Assert.That(avif.Kind, Is.EqualTo(ImageFormatKind.Avif));
        Assert.That(avif.ContainerBrand, Is.EqualTo("mif1"));
        Assert.That(avif.Confidence, Is.EqualTo(90));
        Assert.That(heic.Kind, Is.EqualTo(ImageFormatKind.Heic));
        Assert.That(heic.ContainerBrand, Is.EqualTo("isom"));
        Assert.That(cr3.Kind, Is.EqualTo(ImageFormatKind.Cr3));
    }

    [Test]
    public void DetectBytes_ReadsLargeSizeFileTypeBox()
    {
        byte[] bytes = CreateFtyp("mif1", 16909060, new string[] { "avif" }, true);

        BmffBrandInfo brands = BmffBrandDetector.ReadBytes(bytes);
        ImageFormatInfo format = new ImageFormatDetector().DetectBytes(bytes);

        Assert.That(brands.Valid, Is.True);
        Assert.That(brands.MajorBrand, Is.EqualTo("mif1"));
        Assert.That(brands.MinorVersion, Is.EqualTo(16909060));
        Assert.That(brands.CompatibleBrands, Is.EqualTo(new string[] { "avif" }));
        Assert.That(format.Kind, Is.EqualTo(ImageFormatKind.Avif));
    }

    [Test]
    public void Parse_AddsFileTypeTagsAndRefinesDocumentFormat()
    {
        byte[] bytes = CreateFtyp("mif1", 16909060, new string[] { "mif1", "avif", "MA1B" }, false);
        MemoryStream stream = new MemoryStream(bytes);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(stream);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Avif));
        Assert.That(document.Format.ContainerBrand, Is.EqualTo("mif1"));
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmff.ftyp.MajorBrand").Value.Text, Is.EqualTo("mif1"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmff.ftyp.MinorVersion").Value.UnsignedIntegerValue, Is.EqualTo(16909060));
        MetadataTag compatible = RequiredMetadataQuery.FindFirst(document, "Bmff.ftyp.CompatibleBrands");
        Assert.That(compatible.Value.Kind, Is.EqualTo(MetadataValueKind.StringList));
        Assert.That(compatible.Value.TextValues, Is.EqualTo(new string[] { "mif1", "avif", "MA1B" }));
        Assert.That(compatible.Offset, Is.EqualTo(16));
        Assert.That(compatible.Length, Is.EqualTo(12));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmff.ftyp.DetectedFormat").Value.Text, Is.EqualTo("AVIF"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmff.ftyp.ClassifiedByCompatibleBrand").Value.BooleanValue, Is.True);
        Assert.That(stream.Position, Is.EqualTo(0));
    }

    [Test]
    public void Parse_PreservesCompatibleBrandOrderAndDuplicates()
    {
        byte[] bytes = CreateFtyp("mif1", 0, new string[] { "mif1", "heic", "mif1" }, false);
        ImageMetadataDocument document = ParseDirect(bytes);

        MetadataTag compatible = RequiredMetadataQuery.FindFirst(document, "Bmff.ftyp.CompatibleBrands");
        Assert.That(compatible.Value.TextValues, Is.EqualTo(new string[] { "mif1", "heic", "mif1" }));
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Heic));
    }

    [Test]
    public void Parse_ReportsTruncatedFieldsAndCompatibleBrand()
    {
        byte[] shortFields = CreateBoxWithPayload("ftyp", new byte[] { 109, 105, 102, 49, 0, 0, 0 });
        ImageMetadataDocument fieldsDocument = ParseDirect(shortFields);
        AssertDiagnostic(fieldsDocument, "bmff.ftyp.fields.truncated", MetadataSeverity.Truncated);

        byte[] incompleteCompatible = CreateBoxWithPayload(
            "ftyp",
            new byte[] { 109, 105, 102, 49, 0, 0, 0, 0, 97, 118, 105 });
        ImageMetadataDocument compatibleDocument = ParseDirect(incompleteCompatible);
        AssertDiagnostic(compatibleDocument, "bmff.ftyp.compatible-brand.truncated", MetadataSeverity.Truncated);
        Assert.That(RequiredMetadataQuery.FindFirst(compatibleDocument, "Bmff.ftyp.MajorBrand").Value.Text, Is.EqualTo("mif1"));
    }

    [Test]
    public void Parse_ReportsNonPrintableBrandButRetainsIt()
    {
        byte[] bytes = CreateFtyp("mif1", 0, new string[] { "A\u0001CD" }, false);

        ImageMetadataDocument document = ParseDirect(bytes);

        AssertDiagnostic(document, "bmff.ftyp.brand.invalid", MetadataSeverity.RecoverableError);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmff.ftyp.CompatibleBrands").Value.TextValues[0], Is.EqualTo("A\u0001CD"));
    }

    [Test]
    public void Parse_EnforcesCompatibleBrandCountLimit()
    {
        byte[] bytes = CreateFtyp("mif1", 0, new string[] { "mif1", "avif", "MA1B" }, false);
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxTagCount = 1;

        ImageMetadataDocument document = ParseDirect(bytes, options);

        AssertDiagnostic(document, "bmff.ftyp.brand.count.limit", MetadataSeverity.LimitExceeded);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmff.ftyp.CompatibleBrands").Value.TextValues, Is.EqualTo(new string[] { "mif1" }));
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Heif));
    }

    [Test]
    public void DetectBytes_RejectsUndersizedFileTypeBox()
    {
        byte[] bytes = CreateBoxWithPayload("ftyp", new byte[] { 109, 105, 102, 49, 0, 0, 0 });

        ImageFormatInfo info = new ImageFormatDetector().DetectBytes(bytes);

        Assert.That(info.Kind, Is.EqualTo(ImageFormatKind.Unknown));
        Assert.That(info.DetectedByMagic, Is.False);
    }

    public static ImageMetadataDocument ParseDirect(byte[] bytes)
    {
        return ParseDirect(bytes, new ImageMetadataReaderOptions());
    }

    public static ImageMetadataDocument ParseDirect(byte[] bytes, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return BmffMetadataParser.Parse(reader, new ImageFormatInfo(), options);
    }

    public static byte[] CreateFtyp(string majorBrand, uint minorVersion, string[] compatibleBrands, bool largeSize)
    {
        List<byte> payload = new List<byte>();
        AddAscii(payload, majorBrand);
        AddUnsigned32(payload, minorVersion);
        foreach (string brand in compatibleBrands)
        {
            AddAscii(payload, brand);
        }

        List<byte> bytes = new List<byte>();
        if (largeSize)
        {
            AddUnsigned32(bytes, 1);
            AddAscii(bytes, "ftyp");
            AddUnsigned64(bytes, (ulong)(16 + payload.Count));
        }
        else
        {
            AddUnsigned32(bytes, (uint)(8 + payload.Count));
            AddAscii(bytes, "ftyp");
        }

        AddBytes(bytes, payload.ToArray());
        return bytes.ToArray();
    }

    public static byte[] CreateBoxWithPayload(string type, byte[] payload)
    {
        List<byte> bytes = new List<byte>();
        AddUnsigned32(bytes, (uint)(8 + payload.Length));
        AddAscii(bytes, type);
        AddBytes(bytes, payload);
        return bytes.ToArray();
    }

    public static void AddUnsigned32(List<byte> bytes, uint value)
    {
        bytes.Add((byte)(value >> 24));
        bytes.Add((byte)(value >> 16));
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }

    public static void AddUnsigned64(List<byte> bytes, ulong value)
    {
        int shift = 56;
        while (shift >= 0)
        {
            bytes.Add((byte)(value >> shift));
            shift = shift - 8;
        }
    }

    public static void AddAscii(List<byte> bytes, string value)
    {
        foreach (char item in value)
        {
            bytes.Add((byte)item);
        }
    }

    public static void AddBytes(List<byte> bytes, byte[] values)
    {
        foreach (byte value in values)
        {
            bytes.Add(value);
        }
    }

    public static void AssertDiagnostic(ImageMetadataDocument document, string code, MetadataSeverity severity)
    {
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == code && diagnostic.Severity == severity));
    }
}
