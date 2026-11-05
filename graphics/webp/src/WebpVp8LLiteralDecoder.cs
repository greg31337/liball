using System;

namespace webp;

internal static class WebpVp8LLiteralDecoder
{
    internal static WebpVp8LLiteralDecodeResult Decode(WebpVp8LBitReader reader, WebpVp8LPrefixCodeGroup group)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ValidateGroup(group);

        WebpVp8LLiteralDecodeResult result = new WebpVp8LLiteralDecodeResult();
        int green = group.Green.DecodeSymbol(reader);
        result.GreenSymbol = green;
        if (green >= 256)
        {
            AddDiagnostic(result, "VP8L green symbol is not a literal ARGB value");
            Finish(result);
            return result;
        }

        int red = group.Red.DecodeSymbol(reader);
        int blue = group.Blue.DecodeSymbol(reader);
        int alpha = group.Alpha.DecodeSymbol(reader);
        ValidateChannel(result, red, "red");
        ValidateChannel(result, blue, "blue");
        ValidateChannel(result, alpha, "alpha");
        if (!result.HasErrors())
        {
            result.IsLiteral = true;
            result.Color.Green = (byte)green;
            result.Color.Red = (byte)red;
            result.Color.Blue = (byte)blue;
            result.Color.Alpha = (byte)alpha;
        }

        Finish(result);
        return result;
    }

    internal static void ValidateGroup(WebpVp8LPrefixCodeGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(group.Green);
        ArgumentNullException.ThrowIfNull(group.Red);
        ArgumentNullException.ThrowIfNull(group.Blue);
        ArgumentNullException.ThrowIfNull(group.Alpha);
    }

    internal static void ValidateChannel(WebpVp8LLiteralDecodeResult result, int value, string channel)
    {
        if (value < 0 || value > 255)
        {
            AddDiagnostic(result, "VP8L " + channel + " literal symbol exceeds byte range");
        }
    }

    internal static void AddDiagnostic(WebpVp8LLiteralDecodeResult result, string message)
    {
        result.Diagnostics.Add(new WebpDiagnostic(WebpDiagnosticSeverity.Error, -1, WebpConstants.FourCcVp8LString, message));
    }

    internal static void Finish(WebpVp8LLiteralDecodeResult result)
    {
        result.IsValid = !result.HasErrors();
    }
}
