using System;
using System.Globalization;

namespace fake;

internal static class ColorGenerator
{
    const int DefaultPaletteCount = 5;
    const int HexLength = 7;
    const int MaximumCssLength = 16;

    internal static string Name(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateData(context);
        Guard.OutputSize(
            MaximumLength(ColorData.Names),
            context.CapturedOptions.MaximumOutputSize,
            "colorName");
        return ColorData.Names[random.IntFromMaximumExclusive(ColorData.Names.Length)];
    }

    internal static RgbColor Rgb(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.OutputSize(
            3,
            context.CapturedOptions.MaximumOutputSize,
            "rgb");
        return RandomRgbUnchecked(random);
    }

    internal static RgbColor Rgb(
        GenerationContext context,
        int red,
        int green,
        int blue)
    {
        ValidateChannel(red, "red");
        ValidateChannel(green, "green");
        ValidateChannel(blue, "blue");
        Guard.OutputSize(
            3,
            context.CapturedOptions.MaximumOutputSize,
            "rgb");
        return RgbUnchecked(red, green, blue);
    }

    internal static RgbColor Rgb(
        GenerationContext context,
        string hex)
    {
        Guard.NotNull(hex, "hex");
        Guard.Count(
            hex.Length,
            context.CapturedOptions.MaximumCount,
            "hex");
        UnicodeText.Validate(hex, "hex");
        int start = 0;
        if (hex.Length == 7 && hex[0] == '#')
        {
            start = 1;
        }
        else if (hex.Length != 6)
        {
            throw new ArgumentException(
                "hex must contain exactly six hexadecimal digits with an optional leading number sign.",
                "hex");
        }
        int red = HexByte(hex, start, "hex");
        int green = HexByte(hex, start + 2, "hex");
        int blue = HexByte(hex, start + 4, "hex");
        Guard.OutputSize(
            3,
            context.CapturedOptions.MaximumOutputSize,
            "rgb");
        return RgbUnchecked(red, green, blue);
    }

    internal static string Hex(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.OutputSize(
            HexLength,
            context.CapturedOptions.MaximumOutputSize,
            "hex");
        return HexUnchecked(RandomRgbUnchecked(random));
    }

    internal static string Hex(
        GenerationContext context,
        RgbColor value)
    {
        ValidateRgb(value);
        Guard.OutputSize(
            HexLength,
            context.CapturedOptions.MaximumOutputSize,
            "hex");
        return HexUnchecked(value);
    }

    internal static HslColor Hsl(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.OutputSize(
            3,
            context.CapturedOptions.MaximumOutputSize,
            "hsl");
        return HslUnchecked(RandomRgbUnchecked(random));
    }

    internal static HslColor Hsl(
        GenerationContext context,
        RgbColor value)
    {
        ValidateRgb(value);
        Guard.OutputSize(
            3,
            context.CapturedOptions.MaximumOutputSize,
            "hsl");
        return HslUnchecked(value);
    }

    internal static HwbColor Hwb(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.OutputSize(
            3,
            context.CapturedOptions.MaximumOutputSize,
            "hwb");
        return HwbUnchecked(RandomRgbUnchecked(random));
    }

    internal static HwbColor Hwb(
        GenerationContext context,
        RgbColor value)
    {
        ValidateRgb(value);
        Guard.OutputSize(
            3,
            context.CapturedOptions.MaximumOutputSize,
            "hwb");
        return HwbUnchecked(value);
    }

    internal static CmykColor Cmyk(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.OutputSize(
            4,
            context.CapturedOptions.MaximumOutputSize,
            "cmyk");
        return CmykUnchecked(RandomRgbUnchecked(random));
    }

    internal static CmykColor Cmyk(
        GenerationContext context,
        RgbColor value)
    {
        ValidateRgb(value);
        Guard.OutputSize(
            4,
            context.CapturedOptions.MaximumOutputSize,
            "cmyk");
        return CmykUnchecked(value);
    }

    internal static LabColor Lab(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.OutputSize(
            3,
            context.CapturedOptions.MaximumOutputSize,
            "lab");
        return LabUnchecked(RandomRgbUnchecked(random));
    }

    internal static LabColor Lab(
        GenerationContext context,
        RgbColor value)
    {
        ValidateRgb(value);
        Guard.OutputSize(
            3,
            context.CapturedOptions.MaximumOutputSize,
            "lab");
        return LabUnchecked(value);
    }

    internal static LchColor Lch(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.OutputSize(
            3,
            context.CapturedOptions.MaximumOutputSize,
            "lch");
        return LchUnchecked(LabUnchecked(RandomRgbUnchecked(random)));
    }

    internal static LchColor Lch(
        GenerationContext context,
        RgbColor value)
    {
        ValidateRgb(value);
        Guard.OutputSize(
            3,
            context.CapturedOptions.MaximumOutputSize,
            "lch");
        return LchUnchecked(LabUnchecked(value));
    }

    internal static string Css(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.OutputSize(
            MaximumCssLength,
            context.CapturedOptions.MaximumOutputSize,
            "cssColor");
        return CssUnchecked(RandomRgbUnchecked(random));
    }

    internal static string Css(
        GenerationContext context,
        RgbColor value)
    {
        ValidateRgb(value);
        Guard.OutputSize(
            MaximumCssLength,
            context.CapturedOptions.MaximumOutputSize,
            "cssColor");
        return CssUnchecked(value);
    }

    internal static ColorRecord Record(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateData(context);
        Guard.OutputSize(
            MaximumRecordSize(),
            context.CapturedOptions.MaximumOutputSize,
            "colorRecord");
        return RecordUnchecked(random.IntFromMaximumExclusive(ColorData.Names.Length));
    }

    internal static ColorRecord[] Palette(
        GenerationContext context,
        FakeRandom random)
    {
        return Palette(context, random, DefaultPaletteCount);
    }

    internal static ColorRecord[] Palette(
        GenerationContext context,
        FakeRandom random,
        int count)
    {
        Guard.NonNegative(count, "count");
        Guard.Count(
            count,
            context.CapturedOptions.MaximumCount,
            "count");
        ValidateData(context);
        if (count > ColorData.Names.Length)
        {
            throw new ArgumentOutOfRangeException(
                "count",
                count,
                "count cannot exceed the reviewed named-color count.");
        }
        Guard.OutputSize(
            (long)count * MaximumRecordSize(),
            context.CapturedOptions.MaximumOutputSize,
            "palette");
        int[] indices = new int[ColorData.Names.Length];
        int index = 0;
        while (index < indices.Length)
        {
            indices[index] = index;
            index = index + 1;
        }
        ColorRecord[] result = new ColorRecord[count];
        index = 0;
        while (index < count)
        {
            int selected = random.IntFromMinimumAndMaximumAndMaximumInclusive(index, indices.Length - 1, true);
            int temporary = indices[index];
            indices[index] = indices[selected];
            indices[selected] = temporary;
            result[index] = RecordUnchecked(indices[index]);
            index = index + 1;
        }
        return result;
    }

    static RgbColor RandomRgbUnchecked(FakeRandom random)
    {
        return RgbUnchecked(
            random.IntFromMaximumExclusive(256),
            random.IntFromMaximumExclusive(256),
            random.IntFromMaximumExclusive(256));
    }

    static RgbColor RgbUnchecked(int red, int green, int blue)
    {
        RgbColor result = new RgbColor();
        result.Red = red;
        result.Green = green;
        result.Blue = blue;
        return result;
    }

    static string HexUnchecked(RgbColor value)
    {
        return "#" + value.Red.ToString("X2", CultureInfo.InvariantCulture) +
            value.Green.ToString("X2", CultureInfo.InvariantCulture) +
            value.Blue.ToString("X2", CultureInfo.InvariantCulture);
    }

    static HslColor HslUnchecked(RgbColor value)
    {
        double red = value.Red / 255.0;
        double green = value.Green / 255.0;
        double blue = value.Blue / 255.0;
        double maximum = Maximum(red, green, blue);
        double minimum = Minimum(red, green, blue);
        double delta = maximum - minimum;
        double hue = Hue(red, green, blue, maximum, delta);
        double lightness = (maximum + minimum) / 2.0;
        double saturation = 0.0;
        if (delta != 0.0)
        {
            saturation = delta / (1.0 - Math.Abs(2.0 * lightness - 1.0));
        }
        HslColor result = new HslColor();
        result.Hue = Round(hue);
        result.Saturation = Round(saturation * 100.0);
        result.Lightness = Round(lightness * 100.0);
        return result;
    }

    static HwbColor HwbUnchecked(RgbColor value)
    {
        double red = value.Red / 255.0;
        double green = value.Green / 255.0;
        double blue = value.Blue / 255.0;
        double maximum = Maximum(red, green, blue);
        double minimum = Minimum(red, green, blue);
        HwbColor result = new HwbColor();
        result.Hue = Round(Hue(
            red,
            green,
            blue,
            maximum,
            maximum - minimum));
        result.Whiteness = Round(minimum * 100.0);
        result.Blackness = Round((1.0 - maximum) * 100.0);
        return result;
    }

    static CmykColor CmykUnchecked(RgbColor value)
    {
        double red = value.Red / 255.0;
        double green = value.Green / 255.0;
        double blue = value.Blue / 255.0;
        double key = 1.0 - Maximum(red, green, blue);
        CmykColor result = new CmykColor();
        if (key == 1.0)
        {
            result.Cyan = 0.0;
            result.Magenta = 0.0;
            result.Yellow = 0.0;
            result.Key = 100.0;
            return result;
        }
        result.Cyan = Round((1.0 - red - key) / (1.0 - key) * 100.0);
        result.Magenta = Round((1.0 - green - key) / (1.0 - key) * 100.0);
        result.Yellow = Round((1.0 - blue - key) / (1.0 - key) * 100.0);
        result.Key = Round(key * 100.0);
        return result;
    }

    static LabColor LabUnchecked(RgbColor value)
    {
        double red = LinearChannel(value.Red / 255.0);
        double green = LinearChannel(value.Green / 255.0);
        double blue = LinearChannel(value.Blue / 255.0);
        double x = (red * 0.4124564 + green * 0.3575761 +
            blue * 0.1804375) / 0.95047;
        double y = red * 0.2126729 + green * 0.7151522 +
            blue * 0.0721750;
        double z = (red * 0.0193339 + green * 0.1191920 +
            blue * 0.9503041) / 1.08883;
        double transformedX = LabComponent(x);
        double transformedY = LabComponent(y);
        double transformedZ = LabComponent(z);
        LabColor result = new LabColor();
        result.Lightness = Round(116.0 * transformedY - 16.0);
        result.A = Round(500.0 * (transformedX - transformedY));
        result.B = Round(200.0 * (transformedY - transformedZ));
        return result;
    }

    static LchColor LchUnchecked(LabColor value)
    {
        double hue = Math.Atan2(value.B, value.A) * 180.0 / Math.PI;
        if (hue < 0.0)
        {
            hue = hue + 360.0;
        }
        LchColor result = new LchColor();
        result.Lightness = value.Lightness;
        result.Chroma = Round(Math.Sqrt(value.A * value.A + value.B * value.B));
        result.Hue = Round(hue);
        return result;
    }

    static string CssUnchecked(RgbColor value)
    {
        return "rgb(" + value.Red.ToString(CultureInfo.InvariantCulture) +
            " " + value.Green.ToString(CultureInfo.InvariantCulture) +
            " " + value.Blue.ToString(CultureInfo.InvariantCulture) + ")";
    }

    static ColorRecord RecordUnchecked(int index)
    {
        RgbColor rgb = RgbUnchecked(
            ColorData.Red[index],
            ColorData.Green[index],
            ColorData.Blue[index]);
        ColorRecord result = new ColorRecord();
        result.Name = ColorData.Names[index];
        result.Hex = HexUnchecked(rgb);
        result.Rgb = rgb;
        result.Hsl = HslUnchecked(rgb);
        result.Hwb = HwbUnchecked(rgb);
        result.Cmyk = CmykUnchecked(rgb);
        result.Lab = LabUnchecked(rgb);
        result.Lch = LchUnchecked(result.Lab);
        result.Css = CssUnchecked(rgb);
        result.IsSynthetic = true;
        return result;
    }

    static double Hue(
        double red,
        double green,
        double blue,
        double maximum,
        double delta)
    {
        if (delta == 0.0)
        {
            return 0.0;
        }
        double hue;
        if (maximum == red)
        {
            hue = 60.0 * ((green - blue) / delta);
        }
        else if (maximum == green)
        {
            hue = 60.0 * ((blue - red) / delta + 2.0);
        }
        else
        {
            hue = 60.0 * ((red - green) / delta + 4.0);
        }
        if (hue < 0.0)
        {
            hue = hue + 360.0;
        }
        return hue;
    }

    static double LinearChannel(double value)
    {
        if (value <= 0.04045)
        {
            return value / 12.92;
        }
        return Math.Pow((value + 0.055) / 1.055, 2.4);
    }

    static double LabComponent(double value)
    {
        if (value > 216.0 / 24389.0)
        {
            return Math.Cbrt(value);
        }
        return (24389.0 / 27.0 * value + 16.0) / 116.0;
    }

    static double Round(double value)
    {
        return Math.Round(value, 6, MidpointRounding.ToEven);
    }

    static double Maximum(double first, double second, double third)
    {
        double result = first;
        if (second > result)
        {
            result = second;
        }
        if (third > result)
        {
            result = third;
        }
        return result;
    }

    static double Minimum(double first, double second, double third)
    {
        double result = first;
        if (second < result)
        {
            result = second;
        }
        if (third < result)
        {
            result = third;
        }
        return result;
    }

    static void ValidateRgb(RgbColor value)
    {
        Guard.NotNull(value, "value");
        ValidateChannel(value.Red, "value");
        ValidateChannel(value.Green, "value");
        ValidateChannel(value.Blue, "value");
    }

    static void ValidateChannel(int value, string parameterName)
    {
        if (value < 0 || value > 255)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "RGB channels must be between 0 and 255, inclusive.");
        }
    }

    static int HexByte(string value, int index, string parameterName)
    {
        int high = HexDigit(value[index]);
        int low = HexDigit(value[index + 1]);
        if (high < 0 || low < 0)
        {
            throw new ArgumentException(
                "hex contains a character outside the hexadecimal alphabet.",
                parameterName);
        }
        return high * 16 + low;
    }

    static int HexDigit(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }
        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }
        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }
        return -1;
    }

    static void ValidateData(GenerationContext context)
    {
        if (ColorData.Names.Length != ColorData.Red.Length ||
            ColorData.Names.Length != ColorData.Green.Length ||
            ColorData.Names.Length != ColorData.Blue.Length)
        {
            throw FakeException.Create(
                "The reviewed named-color table has inconsistent column lengths.");
        }
        Guard.Count(
            ColorData.Names.Length,
            context.CapturedOptions.MaximumCount,
            "colorData");
        int index = 0;
        while (index < ColorData.Names.Length)
        {
            if (ColorData.Names[index] == null ||
                ColorData.Names[index].Length == 0)
            {
                throw FakeException.Create(
                    "The reviewed named-color table contains an empty name.");
            }
            ValidateChannel(ColorData.Red[index], "colorData");
            ValidateChannel(ColorData.Green[index], "colorData");
            ValidateChannel(ColorData.Blue[index], "colorData");
            index = index + 1;
        }
    }

    static int MaximumRecordSize()
    {
        return MaximumLength(ColorData.Names) + HexLength +
            MaximumCssLength + 20;
    }

    static int MaximumLength(string[] values)
    {
        int maximum = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].Length > maximum)
            {
                maximum = values[index].Length;
            }
            index = index + 1;
        }
        return maximum;
    }
}
