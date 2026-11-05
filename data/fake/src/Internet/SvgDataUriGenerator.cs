using System;
using System.Globalization;
using System.Text;

namespace fake;

internal static class SvgDataUriGenerator
{
    const int MaximumDimension = 4096;
    const string Prefix = "data:image/svg+xml;base64,";

    static readonly string[] BackgroundColors = new string[]
    {
        "#1D4ED8", "#0F766E", "#7E22CE", "#B45309",
        "#BE123C", "#0369A1", "#4338CA", "#047857"
    };

    static readonly string[] AccentColors = new string[]
    {
        "#BFDBFE", "#99F6E4", "#E9D5FF", "#FDE68A",
        "#FECDD3", "#BAE6FD", "#C7D2FE", "#A7F3D0"
    };

    internal static string Placeholder(
        FakeRandom random,
        int width,
        int height,
        int maximumOutputSize)
    {
        ValidateDimension(width, "width");
        ValidateDimension(height, "height");
        int colorIndex = random.IntFromMaximumExclusive(BackgroundColors.Length);
        string widthText = width.ToString(CultureInfo.InvariantCulture);
        string heightText = height.ToString(CultureInfo.InvariantCulture);
        string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='" + widthText +
            "' height='" + heightText +
            "' viewBox='0 0 " + widthText + " " + heightText +
            "'><title>Synthetic placeholder</title><rect width='100%' height='100%' fill='" +
            BackgroundColors[colorIndex] +
            "'/><path d='M0 0L" + widthText + " " + heightText + "M" + widthText +
            " 0L0 " + heightText + "' stroke='" + AccentColors[colorIndex] +
            "' stroke-width='2'/></svg>";
        return Encode(svg, maximumOutputSize);
    }

    internal static string Avatar(
        FakeRandom random,
        int size,
        int maximumOutputSize)
    {
        ValidateDimension(size, "size");
        int colorIndex = random.IntFromMaximumExclusive(BackgroundColors.Length);
        string sizeText = size.ToString(CultureInfo.InvariantCulture);
        string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='" + sizeText +
            "' height='" + sizeText +
            "' viewBox='0 0 100 100'><title>Synthetic avatar</title>" +
            "<rect width='100' height='100' rx='18' fill='" + BackgroundColors[colorIndex] +
            "'/><circle cx='50' cy='36' r='18' fill='" + AccentColors[colorIndex] +
            "'/><circle cx='50' cy='92' r='32' fill='" + AccentColors[colorIndex] +
            "'/></svg>";
        return Encode(svg, maximumOutputSize);
    }

    static string Encode(string svg, int maximumOutputSize)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(svg);
        string payload = BaseEncoding.Base64(
            new ReadOnlySpan<byte>(bytes),
            maximumOutputSize);
        string result = Prefix + payload;
        Guard.OutputSize(result.Length, maximumOutputSize, "dataUri");
        return result;
    }

    static void ValidateDimension(int value, string parameterName)
    {
        if (value < 1 || value > MaximumDimension)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                parameterName + " must be between 1 and 4096, inclusive.");
        }
    }
}
