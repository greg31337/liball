using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class CheckedDimensionsTests
{
    [Test]
    public void DimensionBoundsAcceptExactConfiguredLimits()
    {
        Assert.DoesNotThrow(() => CheckedDimensions.ValidateDimensionBounds(
            16384,
            4096,
            16384,
            16384,
            67108864));
        Assert.That(
            CheckedDimensions.CalculateArea(16384, 4096, 67108864),
            Is.EqualTo(67108864));
    }

    [Test]
    public void DimensionBoundsRejectInvalidAndOverLimitValues()
    {
        QrInputException invalid = Assert.Throws<QrInputException>(() =>
            CheckedDimensions.ValidateDimensionBounds(0, 10, 100, 100, 10000))!;
        QrResourceLimitException widthLimit = Assert.Throws<QrResourceLimitException>(() =>
            CheckedDimensions.ValidateDimensionBounds(101, 10, 100, 100, 10000))!;
        QrResourceLimitException areaLimit = Assert.Throws<QrResourceLimitException>(() =>
            CheckedDimensions.ValidateDimensionBounds(100, 100, 100, 100, 9999))!;

        Assert.Multiple(() =>
        {
            Assert.That(invalid.InputName, Is.EqualTo("width"));
            Assert.That(widthLimit.LimitName, Is.EqualTo("MaximumWidth"));
            Assert.That(widthLimit.RequestedValue, Is.EqualTo(101));
            Assert.That(widthLimit.MaximumValue, Is.EqualTo(100));
            Assert.That(areaLimit.LimitName, Is.EqualTo("MaximumArea"));
            Assert.That(areaLimit.RequestedValue, Is.EqualTo(10000));
            Assert.That(areaLimit.MaximumValue, Is.EqualTo(9999));
        });
    }

    [Test]
    public void MinimumStrideRejectsIntegerOverflowBeforeReturningAValue()
    {
        Assert.That(CheckedDimensions.CalculateMinimumStride(17, 4), Is.EqualTo(68));

        QrResourceLimitException exception = Assert.Throws<QrResourceLimitException>(() =>
            CheckedDimensions.CalculateMinimumStride(1073741824, 2))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.LimitName, Is.EqualTo("MinimumStride"));
            Assert.That(exception.RequestedValue, Is.EqualTo(int.MaxValue));
            Assert.That(exception.MaximumValue, Is.EqualTo(int.MaxValue));
        });
    }

    [Test]
    public void RequiredBufferLengthAccountsForPaddingBeforeButNotAfterFinalRow()
    {
        int requiredLength = CheckedDimensions.CalculateRequiredBufferLength(3, 8, 6);

        Assert.That(requiredLength, Is.EqualTo(22));
        Assert.DoesNotThrow(() => CheckedDimensions.ValidateBufferLength(requiredLength, 22));

        QrInputException strideException = Assert.Throws<QrInputException>(() =>
            CheckedDimensions.CalculateRequiredBufferLength(3, 5, 6))!;
        QrInputException bufferException = Assert.Throws<QrInputException>(() =>
            CheckedDimensions.ValidateBufferLength(requiredLength, 21))!;

        Assert.Multiple(() =>
        {
            Assert.That(strideException.InputName, Is.EqualTo("stride"));
            Assert.That(bufferException.InputName, Is.EqualTo("actualLength"));
        });
    }

    [Test]
    public void ElementOffsetValidatesLayoutAndCoordinatesBeforeBufferAccess()
    {
        int offset = CheckedDimensions.CalculateElementOffset(
            1,
            2,
            3,
            2,
            16,
            4,
            28);

        Assert.That(offset, Is.EqualTo(24));

        QrInputException rowException = Assert.Throws<QrInputException>(() =>
            CheckedDimensions.CalculateElementOffset(2, 0, 3, 2, 16, 4, 28))!;
        QrInputException columnException = Assert.Throws<QrInputException>(() =>
            CheckedDimensions.CalculateElementOffset(0, 3, 3, 2, 16, 4, 28))!;
        QrInputException bufferException = Assert.Throws<QrInputException>(() =>
            CheckedDimensions.CalculateElementOffset(1, 2, 3, 2, 16, 4, 27))!;

        Assert.Multiple(() =>
        {
            Assert.That(rowException.InputName, Is.EqualTo("row"));
            Assert.That(columnException.InputName, Is.EqualTo("column"));
            Assert.That(bufferException.InputName, Is.EqualTo("actualLength"));
        });
    }

    [Test]
    public void AllocationArithmeticAllowsZeroAndRejectsConfiguredLimitExcess()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CheckedDimensions.CalculateAllocationBytes(0, 4, 1024), Is.EqualTo(0));
            Assert.That(CheckedDimensions.CalculateAllocationBytes(256, 4, 1024), Is.EqualTo(1024));
        });

        QrResourceLimitException exception = Assert.Throws<QrResourceLimitException>(() =>
            CheckedDimensions.CalculateAllocationBytes(257, 4, 1024))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.LimitName, Is.EqualTo("MaximumAllocationBytes"));
            Assert.That(exception.RequestedValue, Is.EqualTo(1028));
            Assert.That(exception.MaximumValue, Is.EqualTo(1024));
        });
    }

    [Test]
    public void GeneralProductAndSumRejectOverflowAndConfiguredLimits()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CheckedDimensions.CalculateProduct(1000, 2000, 2000000, "Product"), Is.EqualTo(2000000));
            Assert.That(CheckedDimensions.CalculateSum(1000, 2000, 3000, "Sum"), Is.EqualTo(3000));
        });

        QrResourceLimitException product = Assert.Throws<QrResourceLimitException>(() =>
            CheckedDimensions.CalculateProduct(int.MaxValue, 2, int.MaxValue, "Product"))!;
        QrResourceLimitException sum = Assert.Throws<QrResourceLimitException>(() =>
            CheckedDimensions.CalculateSum(int.MaxValue, 1, int.MaxValue, "Sum"))!;

        Assert.Multiple(() =>
        {
            Assert.That(product.LimitName, Is.EqualTo("Product"));
            Assert.That(product.RequestedValue, Is.EqualTo(int.MaxValue));
            Assert.That(sum.LimitName, Is.EqualTo("Sum"));
            Assert.That(sum.RequestedValue, Is.EqualTo(int.MaxValue));
        });
    }

    [Test]
    public void CapacityArithmeticCombinesHeadersPayloadCodewordsAndRemainderBits()
    {
        int required = CheckedDimensions.CalculateRequiredCapacityBits(4, 8, 12, 100);
        int available = CheckedDimensions.CalculateCodewordCapacityBits(26, 7);

        Assert.Multiple(() =>
        {
            Assert.That(required, Is.EqualTo(124));
            Assert.That(available, Is.EqualTo(215));
        });

        QrInputException remainder = Assert.Throws<QrInputException>(() =>
            CheckedDimensions.CalculateCodewordCapacityBits(26, 8))!;
        QrResourceLimitException overflow = Assert.Throws<QrResourceLimitException>(() =>
            CheckedDimensions.CalculateCodewordCapacityBits(int.MaxValue / 8 + 1, 0))!;

        Assert.Multiple(() =>
        {
            Assert.That(remainder.InputName, Is.EqualTo("remainderBitCount"));
            Assert.That(overflow.LimitName, Is.EqualTo("CapacityBitCount"));
        });
    }
}
