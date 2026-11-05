using System.Collections.Generic;

namespace png;

internal class PngApngFrameRectangleValidator
{
    internal static bool IsValid(PngFrameControl frameControl, PngIhdr ihdr)
    {
        return Validate(frameControl, ihdr).IsValid;
    }

    internal static bool IsValidDefaultImageFrame(PngFrameControl frameControl, PngIhdr ihdr)
    {
        return ValidateDefaultImageFrame(frameControl, ihdr).IsValid;
    }

    internal static PngValidationResult Validate(PngFrameControl frameControl, PngIhdr ihdr)
    {
        ArgumentNullException.ThrowIfNull(frameControl);
        ArgumentNullException.ThrowIfNull(ihdr);

        List<PngError> errors = new List<PngError>();
        if (!ValidateCanvas(errors, ihdr))
        {
            return CreateResult(errors);
        }

        ValidateFrame(errors, frameControl, ihdr);
        return CreateResult(errors);
    }

    internal static PngValidationResult ValidateDefaultImageFrame(PngFrameControl frameControl, PngIhdr ihdr)
    {
        ArgumentNullException.ThrowIfNull(frameControl);
        ArgumentNullException.ThrowIfNull(ihdr);

        List<PngError> errors = new List<PngError>();
        if (!ValidateCanvas(errors, ihdr))
        {
            return CreateResult(errors);
        }

        ValidateFrame(errors, frameControl, ihdr);
        ValidateDefaultFrame(errors, frameControl, ihdr);
        return CreateResult(errors);
    }

    internal static bool ValidateCanvas(List<PngError> errors, PngIhdr ihdr)
    {
        if (ihdr.Width <= 0 || ihdr.Height <= 0)
        {
            AddError(errors, "PNG_APNG_CANVAS_DIMENSION_INVALID", "APNG frame validation requires a positive canvas size.");
            return false;
        }

        return true;
    }

    internal static void ValidateFrame(List<PngError> errors, PngFrameControl frameControl, PngIhdr ihdr)
    {
        if (frameControl.Width == 0)
        {
            AddError(errors, "PNG_APNG_FRAME_WIDTH_INVALID", "APNG frame width must be greater than 0.");
        }

        if (frameControl.Height == 0)
        {
            AddError(errors, "PNG_APNG_FRAME_HEIGHT_INVALID", "APNG frame height must be greater than 0.");
        }

        if (frameControl.Width == 0 || frameControl.Height == 0)
        {
            return;
        }

        uint canvasWidth = (uint)ihdr.Width;
        uint canvasHeight = (uint)ihdr.Height;
        if (frameControl.XOffset > canvasWidth || frameControl.Width > canvasWidth - frameControl.XOffset)
        {
            AddError(errors, "PNG_APNG_FRAME_RECTANGLE_OUT_OF_BOUNDS", "APNG frame rectangle must fit within the canvas width.");
        }

        if (frameControl.YOffset > canvasHeight || frameControl.Height > canvasHeight - frameControl.YOffset)
        {
            AddError(errors, "PNG_APNG_FRAME_RECTANGLE_OUT_OF_BOUNDS", "APNG frame rectangle must fit within the canvas height.");
        }
    }

    internal static void ValidateDefaultFrame(List<PngError> errors, PngFrameControl frameControl, PngIhdr ihdr)
    {
        if (frameControl.XOffset != 0
            || frameControl.YOffset != 0
            || frameControl.Width != (uint)ihdr.Width
            || frameControl.Height != (uint)ihdr.Height)
        {
            AddError(errors, "PNG_APNG_DEFAULT_IMAGE_RECTANGLE_INVALID", "APNG default image frame must cover the full canvas at offset 0,0.");
        }
    }

    internal static void AddError(List<PngError> errors, string code, string message)
    {
        PngError error = new PngError();
        error.Code = code;
        error.Message = message;
        error.ChunkType = PngConstants.ChunkFrameControl;
        error.Offset = 0;
        error.IsFatal = true;
        errors.Add(error);
    }

    internal static PngValidationResult CreateResult(List<PngError> errors)
    {
        PngValidationResult result = new PngValidationResult();
        result.IsValid = errors.Count == 0;
        result.Errors = errors.ToArray();
        return result;
    }
}
