using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifImageRectangleValidatorTests
{
    [Test]
    public void Validate_AcceptsRectangleInsideLogicalScreen()
    {
        GifImageRectangleValidator validator = new GifImageRectangleValidator();
        GifLogicalScreen screen = new GifLogicalScreen(100, 50);
        GifFrame frame = new GifFrame(10, 5, 20, 30);

        GifImageRectangleValidationResult result = validator.ValidateAtFrameIndex(frame, screen, 2);

        Assert.That(result.Success, Is.True);
        Assert.That(validator.IsInsideLogicalScreen(frame, screen), Is.True);
    }

    [Test]
    public void Validate_AcceptsRectangleExactlyCoveringLogicalScreen()
    {
        GifImageRectangleValidator validator = new GifImageRectangleValidator();
        GifLogicalScreen screen = new GifLogicalScreen(100, 50);
        GifFrame frame = new GifFrame(0, 0, 100, 50);

        GifImageRectangleValidationResult result = validator.Validate(frame, screen);

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void Validate_RejectsMissingFrameOrLogicalScreen()
    {
        GifImageRectangleValidator validator = new GifImageRectangleValidator();

        GifImageRectangleValidationResult missingFrame = validator.ValidateAtFrameIndex(null!, new GifLogicalScreen(1, 1), 3);
        GifImageRectangleValidationResult missingScreen = validator.ValidateAtFrameIndex(new GifFrame(0, 0, 1, 1), null!, 4);

        Assert.That(missingFrame.Success, Is.False);
        Assert.That(missingFrame.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(missingFrame.Error.Position.FrameIndex, Is.EqualTo(3));
        Assert.That(missingScreen.Success, Is.False);
        Assert.That(missingScreen.Error.Position.FrameIndex, Is.EqualTo(4));
    }

    [Test]
    public void Validate_RejectsInvalidLogicalScreenDimensions()
    {
        GifImageRectangleValidator validator = new GifImageRectangleValidator();

        GifImageRectangleValidationResult zeroWidth = validator.Validate(new GifFrame(0, 0, 1, 1), new GifLogicalScreen(0, 1));
        GifImageRectangleValidationResult zeroHeight = validator.Validate(new GifFrame(0, 0, 1, 1), new GifLogicalScreen(1, 0));

        Assert.That(zeroWidth.Success, Is.False);
        Assert.That(zeroWidth.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(zeroHeight.Success, Is.False);
    }

    [Test]
    public void Validate_RejectsNegativeOffsets()
    {
        GifImageRectangleValidator validator = new GifImageRectangleValidator();
        GifLogicalScreen screen = new GifLogicalScreen(10, 10);

        Assert.That(validator.Validate(new GifFrame(-1, 0, 1, 1), screen).Success, Is.False);
        Assert.That(validator.Validate(new GifFrame(0, -1, 1, 1), screen).Success, Is.False);
    }

    [Test]
    public void Validate_RejectsNonPositiveFrameDimensions()
    {
        GifImageRectangleValidator validator = new GifImageRectangleValidator();
        GifLogicalScreen screen = new GifLogicalScreen(10, 10);

        Assert.That(validator.Validate(new GifFrame(0, 0, 0, 1), screen).Success, Is.False);
        Assert.That(validator.Validate(new GifFrame(0, 0, 1, 0), screen).Success, Is.False);
    }

    [Test]
    public void Validate_RejectsRectangleBeyondLogicalScreenBounds()
    {
        GifImageRectangleValidator validator = new GifImageRectangleValidator();
        GifLogicalScreen screen = new GifLogicalScreen(10, 10);

        GifImageRectangleValidationResult beyondWidth = validator.ValidateAtFrameIndex(new GifFrame(9, 0, 2, 1), screen, 5);
        GifImageRectangleValidationResult beyondHeight = validator.ValidateAtFrameIndex(new GifFrame(0, 9, 1, 2), screen, 6);

        Assert.That(beyondWidth.Success, Is.False);
        Assert.That(beyondWidth.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(beyondWidth.Error.Position.FrameIndex, Is.EqualTo(5));
        Assert.That(beyondHeight.Success, Is.False);
        Assert.That(beyondHeight.Error.Position.FrameIndex, Is.EqualTo(6));
    }

    [Test]
    public void Validate_UsesWideArithmeticForRightAndBottomEdges()
    {
        GifImageRectangleValidator validator = new GifImageRectangleValidator();
        GifLogicalScreen screen = new GifLogicalScreen(10, 10);
        GifFrame frame = new GifFrame(2147483640, 0, 100, 1);

        GifImageRectangleValidationResult result = validator.Validate(frame, screen);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
    }
}
