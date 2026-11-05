namespace avif.Tests;

public class CheckedMathTests
{
    [Test]
    public void Int32OperationsAcceptRepresentableBoundaries()
    {
        CheckedIntegerResult sum = CheckedMath.AddInt32(int.MaxValue, 0);
        CheckedIntegerResult difference = CheckedMath.SubtractInt32(int.MinValue, 0);
        CheckedIntegerResult product = CheckedMath.MultiplyInt32(int.MinValue, 1);

        Assert.Multiple(() =>
        {
            Assert.That(sum.Success, Is.True);
            Assert.That(sum.Value, Is.EqualTo(int.MaxValue));
            Assert.That(difference.Success, Is.True);
            Assert.That(difference.Value, Is.EqualTo(int.MinValue));
            Assert.That(product.Success, Is.True);
            Assert.That(product.Value, Is.EqualTo(int.MinValue));
        });
    }

    [Test]
    public void Int32OperationsRejectOverflow()
    {
        CheckedIntegerResult sum = CheckedMath.AddInt32(int.MaxValue, 1);
        CheckedIntegerResult difference = CheckedMath.SubtractInt32(int.MinValue, 1);
        CheckedIntegerResult product = CheckedMath.MultiplyInt32(int.MinValue, -1);

        Assert.Multiple(() =>
        {
            Assert.That(sum.Success, Is.False);
            Assert.That(sum.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(difference.Success, Is.False);
            Assert.That(difference.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(product.Success, Is.False);
            Assert.That(product.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
        });
    }

    [Test]
    public void Int64OperationsAcceptRepresentableBoundaries()
    {
        CheckedIntegerResult sum = CheckedMath.AddInt64(long.MaxValue - 1, 1);
        CheckedIntegerResult difference = CheckedMath.SubtractInt64(long.MinValue + 1, 1);
        CheckedIntegerResult positiveProduct = CheckedMath.MultiplyInt64(long.MaxValue / 3, 3);
        CheckedIntegerResult negativeProduct = CheckedMath.MultiplyInt64(long.MinValue / 2, 2);

        Assert.Multiple(() =>
        {
            Assert.That(sum.Success, Is.True);
            Assert.That(sum.Value, Is.EqualTo(long.MaxValue));
            Assert.That(difference.Success, Is.True);
            Assert.That(difference.Value, Is.EqualTo(long.MinValue));
            Assert.That(positiveProduct.Success, Is.True);
            Assert.That(positiveProduct.Value, Is.EqualTo((long.MaxValue / 3) * 3));
            Assert.That(negativeProduct.Success, Is.True);
            Assert.That(negativeProduct.Value, Is.EqualTo(long.MinValue));
        });
    }

    [Test]
    public void Int64OperationsRejectEveryOverflowDirection()
    {
        CheckedIntegerResult positiveSum = CheckedMath.AddInt64(long.MaxValue, 1);
        CheckedIntegerResult negativeSum = CheckedMath.AddInt64(long.MinValue, -1);
        CheckedIntegerResult negativeDifference = CheckedMath.SubtractInt64(long.MinValue, 1);
        CheckedIntegerResult positiveDifference = CheckedMath.SubtractInt64(long.MaxValue, -1);
        CheckedIntegerResult positiveProduct = CheckedMath.MultiplyInt64(long.MaxValue, 2);
        CheckedIntegerResult negativeProduct = CheckedMath.MultiplyInt64(long.MinValue, 2);
        CheckedIntegerResult signProduct = CheckedMath.MultiplyInt64(long.MinValue, -1);

        Assert.Multiple(() =>
        {
            Assert.That(positiveSum.Success, Is.False);
            Assert.That(negativeSum.Success, Is.False);
            Assert.That(negativeDifference.Success, Is.False);
            Assert.That(positiveDifference.Success, Is.False);
            Assert.That(positiveProduct.Success, Is.False);
            Assert.That(negativeProduct.Success, Is.False);
            Assert.That(signProduct.Success, Is.False);
            Assert.That(signProduct.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
        });
    }

    [Test]
    public void ResolveRangeCombinesOffsetsAndAllowsEmptyRangeAtEnd()
    {
        CheckedRangeResult range = CheckedMath.ResolveRange(100, 20, 30, 200);
        CheckedRangeResult emptyAtEnd = CheckedMath.ResolveRange(200, 0, 0, 200);

        Assert.Multiple(() =>
        {
            Assert.That(range.Success, Is.True);
            Assert.That(range.Start, Is.EqualTo(120));
            Assert.That(range.Length, Is.EqualTo(30));
            Assert.That(range.End, Is.EqualTo(150));
            Assert.That(range.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(emptyAtEnd.Success, Is.True);
            Assert.That(emptyAtEnd.Start, Is.EqualTo(200));
            Assert.That(emptyAtEnd.End, Is.EqualTo(200));
        });
    }

    [Test]
    public void ResolveRangeDistinguishesInvalidOverflowAndTruncation()
    {
        CheckedRangeResult invalid = CheckedMath.ResolveRange(-1, 0, 0, 10);
        CheckedRangeResult overflow = CheckedMath.ResolveRange(long.MaxValue, 1, 0, long.MaxValue);
        CheckedRangeResult truncated = CheckedMath.ResolveRange(5, 5, 1, 10);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(overflow.Success, Is.False);
            Assert.That(overflow.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(truncated.Success, Is.False);
            Assert.That(truncated.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(truncated.Start, Is.EqualTo(10));
            Assert.That(truncated.End, Is.EqualTo(11));
        });
    }

    [Test]
    public void ComputeStrideAlignsAndEnforcesRepresentableLimit()
    {
        CheckedIntegerResult exact = CheckedMath.ComputeStride(5, 4, 2, 16, 48);
        CheckedIntegerResult overLimit = CheckedMath.ComputeStride(5, 4, 2, 16, 47);
        CheckedIntegerResult overflow = CheckedMath.ComputeStride(
            int.MaxValue,
            int.MaxValue,
            int.MaxValue,
            1,
            long.MaxValue);
        CheckedIntegerResult invalid = CheckedMath.ComputeStride(5, 4, 2, 0, 48);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.Value, Is.EqualTo(48));
            Assert.That(overLimit.Success, Is.False);
            Assert.That(overLimit.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(overflow.Success, Is.False);
            Assert.That(overflow.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Code, Is.EqualTo(AvifResultCode.InvalidArgument));
        });
    }

    [Test]
    public void PlaneAndPixelSizesEnforceExactLimits()
    {
        CheckedIntegerResult plane = CheckedMath.ComputePlaneSize(48, 10, 480);
        CheckedIntegerResult planeOverLimit = CheckedMath.ComputePlaneSize(48, 10, 479);
        CheckedIntegerResult pixels = CheckedMath.ComputePixelCount(16384, 16384, 268435456);
        CheckedIntegerResult pixelsOverLimit = CheckedMath.ComputePixelCount(16385, 16384, 268435456);

        Assert.Multiple(() =>
        {
            Assert.That(plane.Success, Is.True);
            Assert.That(plane.Value, Is.EqualTo(480));
            Assert.That(planeOverLimit.Success, Is.False);
            Assert.That(planeOverLimit.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(pixels.Success, Is.True);
            Assert.That(pixels.Value, Is.EqualTo(268435456));
            Assert.That(pixelsOverLimit.Success, Is.False);
            Assert.That(pixelsOverLimit.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
        });
    }

    [Test]
    public void DurationChecksNegativeOverflowAndConfiguredLimit()
    {
        CheckedIntegerResult exact = CheckedMath.ComputeDuration(60, 1000, 60000);
        CheckedIntegerResult invalid = CheckedMath.ComputeDuration(-1, 1000, 60000);
        CheckedIntegerResult overflow = CheckedMath.ComputeDuration(long.MaxValue, 2, long.MaxValue);
        CheckedIntegerResult overLimit = CheckedMath.ComputeDuration(61, 1000, 60000);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.Value, Is.EqualTo(60000));
            Assert.That(invalid.Code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(overflow.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(overLimit.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
        });
    }

    [Test]
    public void AllocationSizeAllowsEmptyAndRejectsOversizedArrays()
    {
        CheckedIntegerResult empty = CheckedMath.ComputeAllocationSize(0, 8, 1024);
        CheckedIntegerResult exact = CheckedMath.ComputeAllocationSize(256, 4, 1024);
        CheckedIntegerResult runtimeBoundary = CheckedMath.ComputeAllocationSize(
            System.Array.MaxLength,
            1,
            long.MaxValue);
        CheckedIntegerResult configuredLimit = CheckedMath.ComputeAllocationSize(257, 4, 1024);
        CheckedIntegerResult runtimeLimit = CheckedMath.ComputeAllocationSize(
            (long)System.Array.MaxLength + 1,
            1,
            long.MaxValue);
        CheckedIntegerResult overflow = CheckedMath.ComputeAllocationSize(long.MaxValue, 2, long.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.Value, Is.Zero);
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.Value, Is.EqualTo(1024));
            Assert.That(runtimeBoundary.Success, Is.True);
            Assert.That(runtimeBoundary.Value, Is.EqualTo(System.Array.MaxLength));
            Assert.That(configuredLimit.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(runtimeLimit.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(overflow.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
        });
    }
}
