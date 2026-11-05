namespace gif;

internal class GifImageRectangleValidationResult
{
    internal bool Success;
    internal GifErrorDiagnostic Error;

    internal GifImageRectangleValidationResult()
    {
        Success = false;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifImageRectangleValidator
{
    internal GifImageRectangleValidationResult Validate(GifFrame frame, GifLogicalScreen logicalScreen)
    {
        return ValidateAtFrameIndex(frame, logicalScreen, -1);
    }

    internal GifImageRectangleValidationResult ValidateAtFrameIndex(GifFrame frame, GifLogicalScreen logicalScreen, int frameIndex)
    {
        GifImageRectangleValidationResult result = new GifImageRectangleValidationResult();

        if (frame == null)
        {
            result.Error = CreateError("Image frame is missing.", frameIndex);
            return result;
        }

        if (logicalScreen == null)
        {
            result.Error = CreateError("Logical screen is missing.", frameIndex);
            return result;
        }

        if (logicalScreen.Width <= 0 || logicalScreen.Height <= 0)
        {
            result.Error = CreateError("Logical screen dimensions must be positive.", frameIndex);
            return result;
        }

        if (frame.Left < 0 || frame.Top < 0)
        {
            result.Error = CreateError("Image rectangle offset must be nonnegative.", frameIndex);
            return result;
        }

        if (frame.Width <= 0 || frame.Height <= 0)
        {
            result.Error = CreateError("Image rectangle dimensions must be positive.", frameIndex);
            return result;
        }

        long right = (long)frame.Left + (long)frame.Width;
        long bottom = (long)frame.Top + (long)frame.Height;

        if (right > logicalScreen.Width)
        {
            result.Error = CreateError("Image rectangle extends beyond logical screen width.", frameIndex);
            return result;
        }

        if (bottom > logicalScreen.Height)
        {
            result.Error = CreateError("Image rectangle extends beyond logical screen height.", frameIndex);
            return result;
        }

        result.Success = true;
        return result;
    }

    internal bool IsInsideLogicalScreen(GifFrame frame, GifLogicalScreen logicalScreen)
    {
        GifImageRectangleValidationResult result = Validate(frame, logicalScreen);
        return result.Success;
    }

    internal GifErrorDiagnostic CreateError(string message, int frameIndex)
    {
        return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, message, new GifDiagnosticPosition(-1, frameIndex, "ImageDescriptor"));
    }
}
