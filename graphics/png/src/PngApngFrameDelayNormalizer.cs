namespace png;

internal class PngApngFrameDelayNormalizer
{
    internal const int DefaultDelayDenominator = 100;

    internal static PngFrameControl Normalize(PngFrameControl control)
    {
        ArgumentNullException.ThrowIfNull(control);

        PngFrameControl normalized = new PngFrameControl();
        normalized.SequenceNumber = control.SequenceNumber;
        normalized.Width = control.Width;
        normalized.Height = control.Height;
        normalized.XOffset = control.XOffset;
        normalized.YOffset = control.YOffset;
        normalized.DelayNumerator = control.DelayNumerator;
        normalized.DelayDenominator = (ushort)NormalizeDenominator(control.DelayDenominator);
        normalized.DisposeOperation = control.DisposeOperation;
        normalized.BlendOperation = control.BlendOperation;
        return normalized;
    }

    internal static PngFrame Normalize(PngFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ValidateFrameDelay(frame);

        PngFrame normalized = new PngFrame();
        normalized.Width = frame.Width;
        normalized.Height = frame.Height;
        normalized.XOffset = frame.XOffset;
        normalized.YOffset = frame.YOffset;
        normalized.DelayNumerator = frame.DelayNumerator;
        normalized.DelayDenominator = NormalizeDenominator(frame.DelayDenominator);
        normalized.BlendOperation = frame.BlendOperation;
        normalized.DisposeOperation = frame.DisposeOperation;
        normalized.Pixels = frame.Pixels;
        normalized.UsesDefaultImage = frame.UsesDefaultImage;
        return normalized;
    }

    internal static int CalculateDelayMilliseconds(PngFrameControl control)
    {
        ArgumentNullException.ThrowIfNull(control);

        return CalculateDelayMilliseconds(control.DelayNumerator, control.DelayDenominator);
    }

    internal static int CalculateDelayMilliseconds(PngFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ValidateFrameDelay(frame);

        return CalculateDelayMilliseconds(frame.DelayNumerator, frame.DelayDenominator);
    }

    internal static int CalculateDelayMilliseconds(int numerator, int denominator)
    {
        ValidateDelayValue(numerator, "DelayNumerator");
        ValidateDelayValue(denominator, "DelayDenominator");

        int normalizedDenominator = NormalizeDenominator(denominator);
        long milliseconds = (long)numerator * 1000 / normalizedDenominator;
        if (milliseconds > int.MaxValue)
        {
            throw new OverflowException("APNG frame delay does not fit in Int32 milliseconds.");
        }

        return (int)milliseconds;
    }

    internal static int NormalizeDenominator(int denominator)
    {
        ValidateDelayValue(denominator, "DelayDenominator");
        if (denominator == 0)
        {
            return DefaultDelayDenominator;
        }

        return denominator;
    }

    internal static void ValidateFrameDelay(PngFrame frame)
    {
        ValidateDelayValue(frame.DelayNumerator, "DelayNumerator");
        ValidateDelayValue(frame.DelayDenominator, "DelayDenominator");
    }

    internal static void ValidateDelayValue(int value, string name)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }
}
