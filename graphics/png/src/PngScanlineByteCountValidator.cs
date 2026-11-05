using System.Collections.Generic;

namespace png;

internal class PngScanlineByteCountValidator
{
    internal static long CalculateExpectedByteCount(PngIhdr ihdr)
    {
        ArgumentNullException.ThrowIfNull(ihdr);

        if (ihdr.InterlaceMethod != PngInterlaceMethod.None)
        {
            throw new ArgumentOutOfRangeException("InterlaceMethod");
        }

        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(ihdr);
        return layout.FilteredImageBytes;
    }

    internal static PngValidationResult ValidateBeforeInflation(PngIhdr ihdr)
    {
        return ValidateBeforeInflation(ihdr, new PngSecurityLimits());
    }

    internal static PngValidationResult ValidateBeforeInflation(PngIhdr ihdr, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(ihdr);
        ArgumentNullException.ThrowIfNull(limits);

        PngValidationResult result = CreateBaseResult(ihdr);
        List<PngError> errors = new List<PngError>();
        AddErrors(errors, PngIhdrValidator.Validate(ihdr, limits).Errors);
        if (errors.Count == 0)
        {
            ValidateExpectedByteCount(errors, ihdr, limits);
        }

        return FinishResult(result, errors);
    }

    internal static PngValidationResult ValidateAfterInflation(byte[] decompressed, PngIhdr ihdr)
    {
        return ValidateAfterInflation(decompressed, ihdr, new PngSecurityLimits());
    }

    internal static PngValidationResult ValidateAfterInflation(byte[] decompressed, PngIhdr ihdr, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(decompressed);
        ArgumentNullException.ThrowIfNull(ihdr);
        ArgumentNullException.ThrowIfNull(limits);

        PngValidationResult result = CreateBaseResult(ihdr);
        List<PngError> errors = new List<PngError>();
        AddErrors(errors, PngIhdrValidator.Validate(ihdr, limits).Errors);
        if (errors.Count == 0)
        {
            long expectedByteCount = ValidateExpectedByteCount(errors, ihdr, limits);
            if (errors.Count == 0 && decompressed.Length != expectedByteCount)
            {
                AddError(errors, "PNG_SCANLINE_DECOMPRESSED_BYTE_COUNT_MISMATCH", "Decompressed scanline byte count does not match IHDR dimensions and format.", PngConstants.ChunkIDAT, 0);
            }
        }

        return FinishResult(result, errors);
    }

    internal static long ValidateExpectedByteCount(List<PngError> errors, PngIhdr ihdr, PngSecurityLimits limits)
    {
        if (ihdr.InterlaceMethod != PngInterlaceMethod.None)
        {
            AddError(errors, "PNG_SCANLINE_INTERLACE_UNSUPPORTED", "Scanline byte count validation currently requires non-interlaced PNG data.", PngConstants.ChunkIDAT, 0);
            return 0;
        }

        long expectedByteCount = CalculateExpectedByteCount(ihdr);
        if (limits.MaxDecompressedBytes > 0 && expectedByteCount > limits.MaxDecompressedBytes)
        {
            AddError(errors, "PNG_SCANLINE_DECOMPRESSED_BYTE_LIMIT_EXCEEDED", "Expected decompressed scanline byte count exceeds the configured limit.", PngConstants.ChunkIDAT, 0);
        }

        return expectedByteCount;
    }

    internal static PngValidationResult CreateBaseResult(PngIhdr ihdr)
    {
        PngValidationResult result = new PngValidationResult();
        result.Info.Width = ihdr.Width;
        result.Info.Height = ihdr.Height;
        result.Info.BitDepth = ihdr.BitDepth;
        result.Info.ColorType = ihdr.ColorType;
        result.Info.CompressionMethod = ihdr.CompressionMethod;
        result.Info.FilterMethod = ihdr.FilterMethod;
        result.Info.InterlaceMethod = ihdr.InterlaceMethod;
        return result;
    }

    internal static void AddErrors(List<PngError> errors, PngError[] newErrors)
    {
        int index = 0;
        while (index < newErrors.Length)
        {
            errors.Add(newErrors[index]);
            index += 1;
        }
    }

    internal static PngValidationResult FinishResult(PngValidationResult result, List<PngError> errors)
    {
        result.Errors = errors.ToArray();
        result.IsValid = errors.Count == 0;
        return result;
    }

    internal static void AddError(List<PngError> errors, string code, string message, string chunkType, long offset)
    {
        PngError error = new PngError();
        error.Code = code;
        error.Message = message;
        error.ChunkType = chunkType;
        error.Offset = offset;
        error.IsFatal = true;
        errors.Add(error);
    }
}
