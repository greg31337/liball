using System.Collections.Generic;

namespace png;

internal class PngRowFilterValidator
{
    internal static bool IsValidFilterType(int filterType)
    {
        return filterType >= PngConstants.FilterTypeNone && filterType <= PngConstants.FilterTypePaeth;
    }

    internal static PngValidationResult ValidateRow(byte[] filteredRow)
    {
        return ValidateRow(filteredRow, 0);
    }

    internal static PngValidationResult ValidateRow(byte[] filteredRow, long offset)
    {
        ArgumentNullException.ThrowIfNull(filteredRow);

        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        List<PngError> errors = new List<PngError>();
        if (filteredRow.Length == 0)
        {
            AddError(errors, "PNG_SCANLINE_ROW_TOO_SHORT", "PNG scanline row must contain a filter byte.", PngConstants.ChunkIDAT, offset);
        }
        else
        {
            ValidateFilterType(errors, filteredRow[0], offset);
        }

        return CreateResult(errors);
    }

    internal static PngValidationResult Validate(byte[] filteredScanlines, PngIhdr ihdr)
    {
        return Validate(filteredScanlines, ihdr, new PngSecurityLimits());
    }

    internal static PngValidationResult Validate(byte[] filteredScanlines, PngIhdr ihdr, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(filteredScanlines);
        ArgumentNullException.ThrowIfNull(ihdr);
        ArgumentNullException.ThrowIfNull(limits);

        PngValidationResult result = PngScanlineByteCountValidator.ValidateAfterInflation(filteredScanlines, ihdr, limits);
        if (!result.IsValid)
        {
            return result;
        }

        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(ihdr);
        List<PngError> errors = new List<PngError>();

        int rowIndex = 0;
        while (rowIndex < ihdr.Height)
        {
            long rowOffset = rowIndex * layout.FilteredRowBytes;
            ValidateFilterType(errors, filteredScanlines[(int)rowOffset], rowOffset);
            rowIndex += 1;
        }

        return FinishResult(result, errors);
    }

    internal static void ValidateFilterType(List<PngError> errors, int filterType, long offset)
    {
        if (!IsValidFilterType(filterType))
        {
            AddError(errors, "PNG_SCANLINE_FILTER_TYPE_INVALID", "PNG scanline filter type must be 0 through 4.", PngConstants.ChunkIDAT, offset);
        }
    }

    internal static PngValidationResult CreateResult(List<PngError> errors)
    {
        PngValidationResult result = new PngValidationResult();
        return FinishResult(result, errors);
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
