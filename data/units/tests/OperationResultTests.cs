using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class OperationResultTests
{
    [Test]
    public void CreateSuccess_ReturnsSuccessWithNoError()
    {
        OperationResult result = OperationResult.CreateSuccess();

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.IsFailure(), Is.False);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.None));
        Assert.That(result.GetMessage(), Is.EqualTo(""));
    }

    [Test]
    public void CreateFailure_ReturnsFailureWithErrorDetails()
    {
        OperationResult result = OperationResult.CreateFailure(
            UnitErrorCode.ParseInvalidInput,
            "Invalid unit expression.");

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseInvalidInput));
        Assert.That(result.GetMessage(), Is.EqualTo("Invalid unit expression."));
    }

    [Test]
    public void Constructor_NormalizesNullMessageToEmptyText()
    {
        OperationResult result = new OperationResult(false, UnitErrorCode.LookupNotFound, null);

        Assert.That(result.GetMessage(), Is.EqualTo(""));
    }

    [Test]
    public void Constructor_RejectsSuccessWithFailureCode()
    {
        Assert.Throws<System.ArgumentException>(
            () => new OperationResult(true, UnitErrorCode.RegistryFrozen, "Frozen."));
    }

    [Test]
    public void Constructor_RejectsFailureWithSuccessCode()
    {
        Assert.Throws<System.ArgumentException>(
            () => new OperationResult(false, UnitErrorCode.None, "Missing error code."));
    }
}
