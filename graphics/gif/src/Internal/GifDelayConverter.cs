namespace gif;

internal class GifDelayConversionResult
{
    internal bool Success;
    internal int Value;
    internal GifErrorDiagnostic Error;

    internal GifDelayConversionResult()
    {
        Success = false;
        Value = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifDelayConverter
{
    internal GifDelayConversionResult ConvertHundredthsToMilliseconds(int delayHundredths)
    {
        GifDelayConversionResult result = new GifDelayConversionResult();
        if (delayHundredths < 0)
        {
            result.Error = CreateNegativeDelayError("GIF delay hundredths cannot be negative.");
            return result;
        }

        long delayMilliseconds = (long)delayHundredths * 10;
        if (delayMilliseconds > 2147483647)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Converting GIF delay hundredths would overflow the supported millisecond range.", new GifDiagnosticPosition(-1, -1, "DelayConversion"));
            return result;
        }

        if (delayHundredths > 65535)
        {
            result.Error = CreateRangeError("GIF delay hundredths exceed the 16-bit format range.");
            return result;
        }

        result.Value = (int)delayMilliseconds;
        result.Success = true;
        return result;
    }

    internal GifDelayConversionResult ConvertMillisecondsToHundredths(int delayMilliseconds, GifDelayRoundingMode roundingMode)
    {
        GifDelayConversionResult result = new GifDelayConversionResult();
        if (delayMilliseconds < 0)
        {
            result.Error = CreateNegativeDelayError("GIF delay milliseconds cannot be negative.");
            return result;
        }

        if (roundingMode != GifDelayRoundingMode.Floor && roundingMode != GifDelayRoundingMode.Nearest && roundingMode != GifDelayRoundingMode.Ceiling)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "GIF delay rounding mode is not supported.", new GifDiagnosticPosition(-1, -1, "DelayConversion"));
            return result;
        }

        long delayHundredths = delayMilliseconds / 10;
        int remainderMilliseconds = delayMilliseconds % 10;
        if (roundingMode == GifDelayRoundingMode.Nearest && remainderMilliseconds >= 5)
        {
            delayHundredths = delayHundredths + 1;
        }
        else if (roundingMode == GifDelayRoundingMode.Ceiling && remainderMilliseconds > 0)
        {
            delayHundredths = delayHundredths + 1;
        }

        if (delayHundredths > 65535)
        {
            result.Error = CreateRangeError("Rounded GIF delay exceeds the 16-bit format range.");
            return result;
        }

        result.Value = (int)delayHundredths;
        result.Success = true;
        return result;
    }

    internal GifErrorDiagnostic CreateNegativeDelayError(string message)
    {
        return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, message, new GifDiagnosticPosition(-1, -1, "DelayConversion"));
    }

    internal GifErrorDiagnostic CreateRangeError(string message)
    {
        return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, message, new GifDiagnosticPosition(-1, -1, "DelayConversion"));
    }
}
