using raw;

namespace raw.Tests;

public class CheckedMathTests
{
    [Test]
    public void AddAcceptsExactBoundaryValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CheckedMath.Add(0, 0), Is.EqualTo(0));
            Assert.That(CheckedMath.Add(long.MaxValue, 0), Is.EqualTo(long.MaxValue));
            Assert.That(CheckedMath.Add(long.MaxValue - 10, 10), Is.EqualTo(long.MaxValue));
        });
    }

    [Test]
    public void AddDistinguishesInvalidInputsFromOverflow()
    {
        RawFormatException? offset = Assert.Throws<RawFormatException>(() => CheckedMath.Add(-1, 1));
        RawFormatException? length = Assert.Throws<RawFormatException>(() => CheckedMath.Add(1, -1));
        RawFormatException? overflow = Assert.Throws<RawFormatException>(() => CheckedMath.Add(long.MaxValue, 1));

        Assert.Multiple(() =>
        {
            Assert.That((offset ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidOffset));
            Assert.That((length ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidLength));
            Assert.That((overflow ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ArithmeticOverflow));
            Assert.That(overflow.Offset, Is.EqualTo(long.MaxValue));
        });
    }

    [Test]
    public void MultiplyHandlesZeroExactProductsAndOverflow()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CheckedMath.Multiply(0, long.MaxValue), Is.EqualTo(0));
            Assert.That(CheckedMath.Multiply(long.MaxValue, 1), Is.EqualTo(long.MaxValue));
            Assert.That(CheckedMath.Multiply(1024, 4096), Is.EqualTo(4194304));
        });

        RawFormatException? negative = Assert.Throws<RawFormatException>(() => CheckedMath.Multiply(-1, 4));
        RawFormatException? overflow = Assert.Throws<RawFormatException>(() => CheckedMath.Multiply(long.MaxValue, 2));

        Assert.Multiple(() =>
        {
            Assert.That((negative ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidLength));
            Assert.That((overflow ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ArithmeticOverflow));
        });
    }

    [Test]
    public void RangeEndNeverEscapesItsBoundary()
    {
        Assert.That(CheckedMath.RangeEnd(80, 20, 100), Is.EqualTo(100));

        RawFormatException? outside = Assert.Throws<RawFormatException>(() => CheckedMath.RangeEnd(81, 20, 100));
        RawFormatException? overflow = Assert.Throws<RawFormatException>(() => CheckedMath.RangeEnd(long.MaxValue, 1, long.MaxValue));

        Assert.Multiple(() =>
        {
            Assert.That((outside ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidOffset));
            Assert.That(outside.Offset, Is.EqualTo(81));
            Assert.That((overflow ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ArithmeticOverflow));
        });
    }

    [Test]
    public void FileSizeHonorsTheConfiguredLimit()
    {
        RawLimits limits = new RawLimits();
        limits.MaxFileSizeBytes = 1024;

        Assert.That(CheckedMath.FileSize(1024, limits), Is.EqualTo(1024));

        RawFormatException? negative = Assert.Throws<RawFormatException>(() => CheckedMath.FileSize(-1, limits));
        RawLimitException? exceeded = Assert.Throws<RawLimitException>(() => CheckedMath.FileSize(1025, limits));

        Assert.Multiple(() =>
        {
            Assert.That((negative ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidLength));
            Assert.That((exceeded ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
        });
    }

    [Test]
    public void PixelCountChecksDimensionsBeforeReturningTheProduct()
    {
        RawLimits limits = new RawLimits();
        limits.MaxWidth = 20;
        limits.MaxHeight = 10;
        limits.MaxPixelCount = 150;

        Assert.That(CheckedMath.PixelCount(15, 10, limits), Is.EqualTo(150));

        RawFormatException? zero = Assert.Throws<RawFormatException>(() => CheckedMath.PixelCount(0, 10, limits));
        RawLimitException? dimension = Assert.Throws<RawLimitException>(() => CheckedMath.PixelCount(21, 1, limits));
        RawLimitException? pixels = Assert.Throws<RawLimitException>(() => CheckedMath.PixelCount(20, 10, limits));

        Assert.Multiple(() =>
        {
            Assert.That((zero ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidLength));
            Assert.That((dimension ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That((pixels ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
        });
    }

    [Test]
    public void DecodedAndAllocationSizesUseIndependentLimits()
    {
        RawLimits limits = new RawLimits();
        limits.MaxDecodedBytes = 200;
        limits.MaxAllocationBytes = 100;

        Assert.Multiple(() =>
        {
            Assert.That(CheckedMath.DecodedSize(20, 10, limits), Is.EqualTo(200));
            Assert.That(CheckedMath.AllocationSize(10, 10, limits), Is.EqualTo(100));
        });

        RawLimitException? decoded = Assert.Throws<RawLimitException>(() => CheckedMath.DecodedSize(201, 1, limits));
        RawLimitException? allocation = Assert.Throws<RawLimitException>(() => CheckedMath.AllocationSize(101, 1, limits));
        RawFormatException? overflow = Assert.Throws<RawFormatException>(() => CheckedMath.AllocationSize(long.MaxValue, 2, limits));

        Assert.Multiple(() =>
        {
            Assert.That((decoded ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That((allocation ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That((overflow ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ArithmeticOverflow));
        });
    }

    [Test]
    public void IntegerConversionsRejectUnrepresentableFileValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CheckedMath.ToInt32(int.MaxValue), Is.EqualTo(int.MaxValue));
            Assert.That(CheckedMath.ToInt64((ulong)long.MaxValue), Is.EqualTo(long.MaxValue));
        });

        RawFormatException? negative = Assert.Throws<RawFormatException>(() => CheckedMath.ToInt32(-1));
        RawFormatException? intOverflow = Assert.Throws<RawFormatException>(() => CheckedMath.ToInt32((long)int.MaxValue + 1));
        RawFormatException? longOverflow = Assert.Throws<RawFormatException>(() => CheckedMath.ToInt64((ulong)long.MaxValue + 1));

        Assert.Multiple(() =>
        {
            Assert.That((negative ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidLength));
            Assert.That((intOverflow ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ArithmeticOverflow));
            Assert.That((longOverflow ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ArithmeticOverflow));
        });
    }

    [Test]
    public void LimitAwareOperationsRejectMissingOrInvalidRelevantLimits()
    {
        RawLimitException? missing = Assert.Throws<RawLimitException>(() => CheckedMath.FileSize(1, null));
        RawLimits limits = new RawLimits();
        limits.MaxFileSizeBytes = 0;
        RawLimitException? invalid = Assert.Throws<RawLimitException>(() => CheckedMath.FileSize(1, limits));

        Assert.Multiple(() =>
        {
            Assert.That((missing ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((invalid ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
        });
    }
}
