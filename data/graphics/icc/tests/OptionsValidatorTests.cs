using icc;

namespace icc.Tests;

public class OptionsValidatorTests
{
    [Test]
    public void DefaultReadAndWriteOptionsAreValid()
    {
        IIccOptionsValidator validator = new IccOptionsValidator();

        IccOperationResult read = validator.ValidateReadOptions(
            new IccReadOptions());
        IccOperationResult write = validator.ValidateWriteOptions(
            new IccWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(read.Message, Does.Contain("reading options"));
            Assert.That(read.Location.Offset, Is.EqualTo(-1));
            Assert.That(read.Location.Length, Is.Zero);
            Assert.That(write.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(write.Message, Does.Contain("writing options"));
            Assert.That(write.Location.Offset, Is.EqualTo(-1));
            Assert.That(write.Location.Length, Is.Zero);
        });
    }

    [Test]
    public void ReadValidationReportsEveryInvalidOptionCategory()
    {
        IIccOptionsValidator validator = new IccOptionsValidator();
        IccReadOptions missingLimits = new IccReadOptions();
        missingLimits.ResourceLimits = null!;
        IccReadOptions invalidReadMode = new IccReadOptions();
        invalidReadMode.Mode = (IccReadMode)99;
        IccReadOptions invalidValidationMode = new IccReadOptions();
        invalidValidationMode.ValidationMode = (IccValidationMode)99;
        IccReadOptions smallProfileLimit = new IccReadOptions();
        smallProfileLimit.ResourceLimits.MaximumProfileBytes = 131;
        IccReadOptions negativeCarrierLimit = new IccReadOptions();
        negativeCarrierLimit.ResourceLimits.MaximumCarrierBytes = -1;
        IccReadOptions negativeWorkLimit = new IccReadOptions();
        negativeWorkLimit.ResourceLimits.MaximumNestingDepth = -1;

        Assert.Multiple(() =>
        {
            AssertInvalid(validator.ValidateReadOptions(null!), "cannot be null");
            AssertInvalid(validator.ValidateReadOptions(missingLimits), "resource limits");
            AssertInvalid(validator.ValidateReadOptions(invalidReadMode), "read mode");
            AssertInvalid(validator.ValidateReadOptions(invalidValidationMode), "validation mode");
            AssertInvalid(validator.ValidateReadOptions(smallProfileLimit), "at least 132 bytes");
            AssertInvalid(validator.ValidateReadOptions(negativeCarrierLimit), "carrier size");
            AssertInvalid(validator.ValidateReadOptions(negativeWorkLimit), "cannot be negative");
        });
    }

    [Test]
    public void WriteValidationReportsEveryInvalidOptionCategory()
    {
        IIccOptionsValidator validator = new IccOptionsValidator();
        IccWriteOptions missingLimits = new IccWriteOptions();
        missingLimits.ResourceLimits = null!;
        IccWriteOptions invalidMode = new IccWriteOptions();
        invalidMode.Mode = (IccWriteMode)99;
        IccWriteOptions smallProfileLimit = new IccWriteOptions();
        smallProfileLimit.ResourceLimits.MaximumProfileBytes = 131;
        IccWriteOptions negativeCarrierLimit = new IccWriteOptions();
        negativeCarrierLimit.ResourceLimits.MaximumCarrierBytes = -1;
        IccWriteOptions negativeWorkLimit = new IccWriteOptions();
        negativeWorkLimit.ResourceLimits.MaximumCalculatorInstructions = -1;

        Assert.Multiple(() =>
        {
            AssertInvalid(validator.ValidateWriteOptions(null!), "cannot be null");
            AssertInvalid(validator.ValidateWriteOptions(missingLimits), "resource limits");
            AssertInvalid(validator.ValidateWriteOptions(invalidMode), "write mode");
            AssertInvalid(validator.ValidateWriteOptions(smallProfileLimit), "at least 132 bytes");
            AssertInvalid(validator.ValidateWriteOptions(negativeCarrierLimit), "carrier size");
            AssertInvalid(validator.ValidateWriteOptions(negativeWorkLimit), "cannot be negative");
        });
    }

    [TestCase("MaximumTagCount")]
    [TestCase("MaximumTagBytes")]
    [TestCase("MaximumTotalTagBytes")]
    [TestCase("MaximumNestingDepth")]
    [TestCase("MaximumDecompressedBytes")]
    [TestCase("MaximumCalculatorInstructions")]
    [TestCase("MaximumCalculatorStackDepth")]
    [TestCase("MaximumCalculatorStorageValues")]
    [TestCase("MaximumDiagnosticCount")]
    public void EveryWorkLimitRejectsNegativeValues(string limitName)
    {
        IIccOptionsValidator validator = new IccOptionsValidator();
        IccReadOptions readOptions = new IccReadOptions();
        IccWriteOptions writeOptions = new IccWriteOptions();
        SetNegativeLimit(readOptions.ResourceLimits, limitName);
        SetNegativeLimit(writeOptions.ResourceLimits, limitName);

        IccOperationResult read = validator.ValidateReadOptions(readOptions);
        IccOperationResult write = validator.ValidateWriteOptions(writeOptions);

        Assert.Multiple(() =>
        {
            AssertInvalid(read, "cannot be negative");
            AssertInvalid(write, "cannot be negative");
        });
    }

    private static void AssertInvalid(IccOperationResult result,
        string messagePart)
    {
        Assert.That(result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
        Assert.That(result.Message, Does.Contain(messagePart));
        Assert.That(result.Location.Offset, Is.EqualTo(-1));
        Assert.That(result.Location.Length, Is.Zero);
    }

    private static void SetNegativeLimit(IccResourceLimits limits,
        string limitName)
    {
        switch (limitName)
        {
            case "MaximumTagCount":
                limits.MaximumTagCount = -1;
                break;
            case "MaximumTagBytes":
                limits.MaximumTagBytes = -1;
                break;
            case "MaximumTotalTagBytes":
                limits.MaximumTotalTagBytes = -1;
                break;
            case "MaximumNestingDepth":
                limits.MaximumNestingDepth = -1;
                break;
            case "MaximumDecompressedBytes":
                limits.MaximumDecompressedBytes = -1;
                break;
            case "MaximumCalculatorInstructions":
                limits.MaximumCalculatorInstructions = -1;
                break;
            case "MaximumCalculatorStackDepth":
                limits.MaximumCalculatorStackDepth = -1;
                break;
            case "MaximumCalculatorStorageValues":
                limits.MaximumCalculatorStorageValues = -1;
                break;
            case "MaximumDiagnosticCount":
                limits.MaximumDiagnosticCount = -1;
                break;
        }
    }
}
