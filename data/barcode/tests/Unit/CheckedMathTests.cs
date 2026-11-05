namespace barcode.Tests;

public class CheckedMathTests
{
    [TestCase(0, 0, 0)]
    [TestCase(int.MaxValue, 0, int.MaxValue)]
    [TestCase(int.MinValue, 0, int.MinValue)]
    [TestCase(int.MaxValue, -1, int.MaxValue - 1)]
    [TestCase(int.MinValue, 1, int.MinValue + 1)]
    public void AddReturnsExactInRangeResult(int left, int right, int expected)
    {
        Assert.That(CheckedMath.Add(left, right), Is.EqualTo(expected));
    }

    [Test]
    public void AddRejectsPositiveAndNegativeOverflow()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => CheckedMath.Add(int.MaxValue, 1),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Integer addition exceeds the supported range."));
            Assert.That(
                () => CheckedMath.Add(int.MinValue, -1),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Integer addition exceeds the supported range."));
        });
    }

    [TestCase(0, 0, 0)]
    [TestCase(int.MaxValue, 0, int.MaxValue)]
    [TestCase(int.MinValue, 0, int.MinValue)]
    [TestCase(int.MaxValue, 1, int.MaxValue - 1)]
    [TestCase(int.MinValue, -1, int.MinValue + 1)]
    public void SubtractReturnsExactInRangeResult(int left, int right, int expected)
    {
        Assert.That(CheckedMath.Subtract(left, right), Is.EqualTo(expected));
    }

    [Test]
    public void SubtractRejectsPositiveAndNegativeOverflow()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => CheckedMath.Subtract(int.MaxValue, -1),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Integer subtraction exceeds the supported range."));
            Assert.That(
                () => CheckedMath.Subtract(int.MinValue, 1),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Integer subtraction exceeds the supported range."));
        });
    }

    [TestCase(0, int.MinValue, 0)]
    [TestCase(int.MaxValue, 1, int.MaxValue)]
    [TestCase(int.MinValue, 1, int.MinValue)]
    [TestCase(46340, 46340, 2147395600)]
    [TestCase(-46340, 46340, -2147395600)]
    public void MultiplyReturnsExactInRangeResult(int left, int right, int expected)
    {
        Assert.That(CheckedMath.Multiply(left, right), Is.EqualTo(expected));
    }

    [Test]
    public void MultiplyRejectsPositiveAndNegativeOverflow()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => CheckedMath.Multiply(46341, 46341),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Integer multiplication exceeds the supported range."));
            Assert.That(
                () => CheckedMath.Multiply(int.MinValue, -1),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Integer multiplication exceeds the supported range."));
        });
    }

    [TestCase(1)]
    [TestCase(int.MaxValue)]
    public void ValidateDimensionAcceptsPositiveValues(int value)
    {
        Assert.That(CheckedMath.ValidateDimension(value), Is.EqualTo(value));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(int.MinValue)]
    public void ValidateDimensionRejectsNonPositiveValues(int value)
    {
        Assert.That(
            () => CheckedMath.ValidateDimension(value),
            Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Dimension must be greater than zero."));
    }

    [TestCase(0, 0)]
    [TestCase(0, int.MaxValue)]
    [TestCase(100, 100)]
    [TestCase(int.MaxValue, int.MaxValue)]
    public void ValidateCapacityAcceptsInclusiveBounds(int capacity, int maximum)
    {
        Assert.That(CheckedMath.ValidateCapacity(capacity, maximum), Is.EqualTo(capacity));
    }

    [Test]
    public void ValidateCapacityRejectsInvalidBounds()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => CheckedMath.ValidateCapacity(-1, 10),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Capacity must not be negative."));
            Assert.That(
                () => CheckedMath.ValidateCapacity(0, -1),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Maximum capacity must not be negative."));
            Assert.That(
                () => CheckedMath.ValidateCapacity(11, 10),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Capacity exceeds the configured maximum."));
        });
    }

    [TestCase(0, 1)]
    [TestCase(4, 5)]
    [TestCase(int.MaxValue - 1, int.MaxValue)]
    public void ValidateIndexAcceptsFirstAndLastAvailableIndex(int index, int length)
    {
        Assert.That(CheckedMath.ValidateIndex(index, length), Is.EqualTo(index));
    }

    [Test]
    public void ValidateIndexRejectsInvalidLengthAndIndices()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => CheckedMath.ValidateIndex(0, -1),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Length must not be negative."));
            Assert.That(
                () => CheckedMath.ValidateIndex(-1, 1),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Index is outside the available range."));
            Assert.That(
                () => CheckedMath.ValidateIndex(1, 1),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Index is outside the available range."));
            Assert.That(
                () => CheckedMath.ValidateIndex(0, 0),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Index is outside the available range."));
        });
    }

    [TestCase(0, 0, 0, 0)]
    [TestCase(0, 10, 10, 10)]
    [TestCase(10, 0, 10, 10)]
    [TestCase(3, 4, 10, 7)]
    [TestCase(int.MaxValue, 0, int.MaxValue, int.MaxValue)]
    public void ValidateRangeAcceptsContainedAndEmptyRanges(int offset, int count, int length, int expectedEnd)
    {
        Assert.That(CheckedMath.ValidateRange(offset, count, length), Is.EqualTo(expectedEnd));
    }

    [Test]
    public void ValidateRangeRejectsInvalidComponentsAndOverflowingRanges()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => CheckedMath.ValidateRange(0, 0, -1),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Length must not be negative."));
            Assert.That(
                () => CheckedMath.ValidateRange(-1, 0, 1),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Offset must not be negative."));
            Assert.That(
                () => CheckedMath.ValidateRange(0, -1, 1),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Count must not be negative."));
            Assert.That(
                () => CheckedMath.ValidateRange(2, 0, 1),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Range is outside the available length."));
            Assert.That(
                () => CheckedMath.ValidateRange(1, int.MaxValue, int.MaxValue),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Range is outside the available length."));
        });
    }

    [TestCase(1, 1, 1, 1, 1)]
    [TestCase(100, 50, 4, 20000, 20000)]
    [TestCase(46340, 46340, 1, int.MaxValue, 2147395600)]
    public void CalculateAllocationLengthReturnsBoundedByteLength(int width, int height, int bytesPerElement, int maximum, int expected)
    {
        Assert.That(CheckedMath.CalculateAllocationLength(width, height, bytesPerElement, maximum), Is.EqualTo(expected));
    }

    [Test]
    public void CalculateAllocationLengthRejectsInvalidOverflowingAndExcessiveRequests()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => CheckedMath.CalculateAllocationLength(0, 1, 1, 1),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Dimension must be greater than zero."));
            Assert.That(
                () => CheckedMath.CalculateAllocationLength(1, 0, 1, 1),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Dimension must be greater than zero."));
            Assert.That(
                () => CheckedMath.CalculateAllocationLength(1, 1, 0, 1),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Dimension must be greater than zero."));
            Assert.That(
                () => CheckedMath.CalculateAllocationLength(46341, 46341, 1, int.MaxValue),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Integer multiplication exceeds the supported range."));
            Assert.That(
                () => CheckedMath.CalculateAllocationLength(100, 100, 4, 39999),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Capacity exceeds the configured maximum."));
            Assert.That(
                () => CheckedMath.CalculateAllocationLength(1, 1, 1, -1),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Maximum capacity must not be negative."));
        });
    }
}
