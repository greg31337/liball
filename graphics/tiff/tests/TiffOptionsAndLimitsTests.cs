using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffOptionsAndLimitsTests
{
    [Test]
    public void DefaultLimits_UseDocumentedSecurityBudgets()
    {
        TiffLimits limits = new TiffLimits();

        Assert.That(limits.MaximumAllocationBytes, Is.EqualTo(268435456));
        Assert.That(limits.MaximumDecodedBytes, Is.EqualTo(1073741824));
        Assert.That(limits.MaximumCodecExpansionRatio, Is.EqualTo(1000000));
        Assert.That(limits.MaximumImageWidth, Is.EqualTo(1000000));
        Assert.That(limits.MaximumImageLength, Is.EqualTo(1000000));
        Assert.That(limits.MaximumImagePixels, Is.EqualTo(1000000000));
        Assert.That(limits.MaximumIfdCount, Is.EqualTo(65536));
        Assert.That(limits.MaximumTagCountPerIfd, Is.EqualTo(4096));
        Assert.That(limits.MaximumSubIfdDepth, Is.EqualTo(16));
    }

    [Test]
    public void Limits_ValidateAllocationAndDecodedByteCounts()
    {
        TiffLimits limits = new TiffLimits();
        limits.MaximumAllocationBytes = 10;
        limits.MaximumDecodedBytes = 20;
        limits.MaximumCodecExpansionRatio = 3;

        Assert.That(limits.AllowsAllocation(10), Is.True);
        Assert.That(limits.AllowsAllocation(11), Is.False);
        Assert.That(limits.AllowsAllocation(-1), Is.False);
        Assert.That(limits.AllowsDecodedBytes(20), Is.True);
        Assert.That(limits.AllowsDecodedBytes(21), Is.False);
        Assert.That(limits.AllowsDecodedBytes(-1), Is.False);
        Assert.That(limits.AllowsCodecExpansion(4, 12), Is.True);
        Assert.That(limits.AllowsCodecExpansion(4, 13), Is.False);
        Assert.That(limits.AllowsCodecExpansion(0, 0), Is.True);
        Assert.That(limits.AllowsCodecExpansion(0, 1), Is.False);
        Assert.That(limits.AllowsCodecExpansion(-1, 1), Is.False);
    }

    [Test]
    public void Limits_ValidateImageDimensionsWithoutOverflow()
    {
        TiffLimits limits = new TiffLimits();
        limits.MaximumImageWidth = 100;
        limits.MaximumImageLength = 200;
        limits.MaximumImagePixels = 1000;

        Assert.That(limits.AllowsImageDimensions(10, 100), Is.True);
        Assert.That(limits.AllowsImageDimensions(0, 100), Is.False);
        Assert.That(limits.AllowsImageDimensions(10, 0), Is.False);
        Assert.That(limits.AllowsImageDimensions(101, 1), Is.False);
        Assert.That(limits.AllowsImageDimensions(1, 201), Is.False);
        Assert.That(limits.AllowsImageDimensions(11, 100), Is.False);
        Assert.That(limits.AllowsImageDimensions(1000000000, 1000000000), Is.False);
    }

    [Test]
    public void Limits_ValidateIfdTagAndSubIfdCounts()
    {
        TiffLimits limits = new TiffLimits();
        limits.MaximumIfdCount = 2;
        limits.MaximumTagCountPerIfd = 3;
        limits.MaximumSubIfdDepth = 4;

        Assert.That(limits.AllowsIfdCount(2), Is.True);
        Assert.That(limits.AllowsIfdCount(3), Is.False);
        Assert.That(limits.AllowsIfdCount(-1), Is.False);
        Assert.That(limits.AllowsTagCountPerIfd(3), Is.True);
        Assert.That(limits.AllowsTagCountPerIfd(4), Is.False);
        Assert.That(limits.AllowsTagCountPerIfd(-1), Is.False);
        Assert.That(limits.AllowsSubIfdDepth(4), Is.True);
        Assert.That(limits.AllowsSubIfdDepth(5), Is.False);
        Assert.That(limits.AllowsSubIfdDepth(-1), Is.False);
    }

    [Test]
    public void ReadOptions_DefaultToStrictMetadataPreservingReadBehavior()
    {
        TiffReadOptions options = new TiffReadOptions();

        Assert.That(options.Strict, Is.True);
        Assert.That(options.LeaveOpen, Is.False);
        Assert.That(options.ReadImageData, Is.True);
        Assert.That(options.PreserveUnknownTags, Is.True);
        Assert.That(options.PreserveTrailingData, Is.True);
        Assert.That(options.UnsupportedFeaturePolicy, Is.EqualTo(TiffUnsupportedFeaturePolicy.PreserveRaw));
        Assert.That(options.Limits, Is.Not.Null);
    }

    [Test]
    public void WriteOptions_DefaultToClassicLittleEndianValidatedOutput()
    {
        TiffWriteOptions options = new TiffWriteOptions();

        Assert.That(options.Strict, Is.True);
        Assert.That(options.LeaveOpen, Is.False);
        Assert.That(options.ValidateBeforeWrite, Is.True);
        Assert.That(options.AutoUpgradeToBigTiff, Is.True);
        Assert.That(options.CloudOptimizedGeoTiffLayout, Is.False);
        Assert.That(options.ByteOrder, Is.EqualTo(TiffByteOrder.LittleEndian));
        Assert.That(options.FileKind, Is.EqualTo(TiffFileKind.Classic));
        Assert.That(options.UnsupportedFeaturePolicy, Is.EqualTo(TiffUnsupportedFeaturePolicy.Error));
        Assert.That(options.Limits, Is.Not.Null);
    }

    [Test]
    public void Options_AcceptCallerSuppliedLimits()
    {
        TiffLimits limits = new TiffLimits();
        limits.MaximumIfdCount = 7;

        TiffReadOptions readOptions = TiffReadOptions.Create(limits);
        TiffWriteOptions writeOptions = TiffWriteOptions.Create(limits);

        Assert.That(readOptions.Limits, Is.SameAs(limits));
        Assert.That(writeOptions.Limits, Is.SameAs(limits));
        Assert.That(readOptions.Limits.MaximumIfdCount, Is.EqualTo(7));
        Assert.That(writeOptions.Limits.MaximumIfdCount, Is.EqualTo(7));
    }

    [Test]
    public void OptionCopies_DeepCopyLimits()
    {
        TiffReadOptions readOptions = new TiffReadOptions();
        readOptions.Strict = false;
        readOptions.Limits.MaximumIfdCount = 8;

        TiffWriteOptions writeOptions = new TiffWriteOptions();
        writeOptions.ByteOrder = TiffByteOrder.BigEndian;
        writeOptions.CloudOptimizedGeoTiffLayout = true;
        writeOptions.Limits.MaximumTagCountPerIfd = 9;

        TiffReadOptions readCopy = readOptions.Copy();
        TiffWriteOptions writeCopy = writeOptions.Copy();

        readOptions.Limits.MaximumIfdCount = 80;
        writeOptions.Limits.MaximumTagCountPerIfd = 90;

        Assert.That(readCopy.Strict, Is.False);
        Assert.That(readCopy.Limits.MaximumIfdCount, Is.EqualTo(8));
        Assert.That(writeCopy.ByteOrder, Is.EqualTo(TiffByteOrder.BigEndian));
        Assert.That(writeCopy.CloudOptimizedGeoTiffLayout, Is.True);
        Assert.That(writeCopy.Limits.MaximumTagCountPerIfd, Is.EqualTo(9));
    }

    [Test]
    public void UnsupportedFeaturePolicyValues_AreStable()
    {
        Assert.That((int)TiffUnsupportedFeaturePolicy.Error, Is.EqualTo(0));
        Assert.That((int)TiffUnsupportedFeaturePolicy.PreserveRaw, Is.EqualTo(1));
        Assert.That((int)TiffUnsupportedFeaturePolicy.Skip, Is.EqualTo(2));
        Assert.That((int)TiffUnsupportedFeaturePolicy.Ignore, Is.EqualTo(3));
    }
}
