namespace svg;

internal class SvgBlendColor
{
    internal double Red;
    internal double Green;
    internal double Blue;
    internal double Alpha;

    internal SvgBlendColor(double red, double green, double blue, double alpha)
    {
        Red = red;
        Green = green;
        Blue = blue;
        Alpha = alpha;
    }
}

internal static class SvgBlendModeMath
{
    internal static void Composite(byte[] source, int sourceIndex, SvgPixelColorSpace sourceSpace, byte[] destination, int destinationIndex, SvgPixelColorSpace destinationSpace, SvgCompositeOperator operation, double opacity, SvgBlendMode mode, SvgBlendColorSpace selection, bool isolated)
    {
        SvgPixelColorSpace working = Working(selection, destinationSpace);
        SvgBlendColor sourceColor = Read(source, sourceIndex, sourceSpace, working, opacity);
        SvgBlendColor backdrop = Read(destination, destinationIndex, destinationSpace, working, 1);
        SvgBlendColor output;
        SvgBlendMode effective = isolated ? SvgBlendMode.Normal : mode;
        if (operation == SvgCompositeOperator.SourceOver && effective != SvgBlendMode.Normal) output = Blend(sourceColor, backdrop, effective);
        else output = PorterDuff(sourceColor, backdrop, operation);
        Write(destination, destinationIndex, output, working, destinationSpace);
    }

    internal static bool ValidMode(SvgBlendMode mode)
    {
        return (int)mode >= 0 && (int)mode <= 17;
    }

    internal static bool ValidSpace(SvgBlendColorSpace space)
    {
        return space == SvgBlendColorSpace.Auto || space == SvgBlendColorSpace.Srgb || space == SvgBlendColorSpace.SrgbLinear;
    }

    internal static SvgBlendMode Parse(string text)
    {
        if (text == "multiply") return SvgBlendMode.Multiply;
        if (text == "screen") return SvgBlendMode.Screen;
        if (text == "overlay") return SvgBlendMode.Overlay;
        if (text == "darken") return SvgBlendMode.Darken;
        if (text == "lighten") return SvgBlendMode.Lighten;
        if (text == "color-dodge") return SvgBlendMode.ColorDodge;
        if (text == "color-burn") return SvgBlendMode.ColorBurn;
        if (text == "hard-light") return SvgBlendMode.HardLight;
        if (text == "soft-light") return SvgBlendMode.SoftLight;
        if (text == "difference") return SvgBlendMode.Difference;
        if (text == "exclusion") return SvgBlendMode.Exclusion;
        if (text == "hue") return SvgBlendMode.Hue;
        if (text == "saturation") return SvgBlendMode.Saturation;
        if (text == "color") return SvgBlendMode.Color;
        if (text == "luminosity") return SvgBlendMode.Luminosity;
        if (text == "plus-darker") return SvgBlendMode.PlusDarker;
        if (text == "plus-lighter") return SvgBlendMode.PlusLighter;
        return SvgBlendMode.Normal;
    }

    static SvgBlendColor PorterDuff(SvgBlendColor source, SvgBlendColor backdrop, SvgCompositeOperator operation)
    {
        double sourceFactor = SourceFactor(operation, source.Alpha, backdrop.Alpha);
        double backdropFactor = BackdropFactor(operation, source.Alpha, backdrop.Alpha);
        return new SvgBlendColor(Clamp(source.Red * sourceFactor + backdrop.Red * backdropFactor), Clamp(source.Green * sourceFactor + backdrop.Green * backdropFactor), Clamp(source.Blue * sourceFactor + backdrop.Blue * backdropFactor), Clamp(source.Alpha * sourceFactor + backdrop.Alpha * backdropFactor));
    }

    static SvgBlendColor Blend(SvgBlendColor source, SvgBlendColor backdrop, SvgBlendMode mode)
    {
        if (mode == SvgBlendMode.PlusLighter) return new SvgBlendColor(Clamp(source.Red + backdrop.Red), Clamp(source.Green + backdrop.Green), Clamp(source.Blue + backdrop.Blue), Clamp(source.Alpha + backdrop.Alpha));
        if (mode == SvgBlendMode.PlusDarker)
        {
            return new SvgBlendColor(Clamp(source.Red + backdrop.Red - 1), Clamp(source.Green + backdrop.Green - 1), Clamp(source.Blue + backdrop.Blue - 1), Clamp(source.Alpha + backdrop.Alpha - 1));
        }
        double sourceRed = Straight(source.Red, source.Alpha);
        double sourceGreen = Straight(source.Green, source.Alpha);
        double sourceBlue = Straight(source.Blue, source.Alpha);
        double backdropRed = Straight(backdrop.Red, backdrop.Alpha);
        double backdropGreen = Straight(backdrop.Green, backdrop.Alpha);
        double backdropBlue = Straight(backdrop.Blue, backdrop.Alpha);
        double[]? blended = NonSeparable(mode, backdropRed, backdropGreen, backdropBlue, sourceRed, sourceGreen, sourceBlue);
        if (blended == null)
        {
            blended = new double[3];
            blended[0] = Separable(mode, backdropRed, sourceRed);
            blended[1] = Separable(mode, backdropGreen, sourceGreen);
            blended[2] = Separable(mode, backdropBlue, sourceBlue);
        }
        double overlap = source.Alpha * backdrop.Alpha;
        double sourceOnly = source.Alpha * (1 - backdrop.Alpha);
        double backdropOnly = backdrop.Alpha * (1 - source.Alpha);
        double alpha = source.Alpha + backdrop.Alpha - overlap;
        double red = sourceOnly * sourceRed + overlap * blended[0] + backdropOnly * backdropRed;
        double green = sourceOnly * sourceGreen + overlap * blended[1] + backdropOnly * backdropGreen;
        double blue = sourceOnly * sourceBlue + overlap * blended[2] + backdropOnly * backdropBlue;
        return new SvgBlendColor(Clamp(red), Clamp(green), Clamp(blue), Clamp(alpha));
    }

    static double Separable(SvgBlendMode mode, double backdrop, double source)
    {
        if (mode == SvgBlendMode.Multiply) return backdrop * source;
        if (mode == SvgBlendMode.Screen) return backdrop + source - backdrop * source;
        if (mode == SvgBlendMode.Overlay) return backdrop <= 0.5 ? 2 * backdrop * source : 1 - 2 * (1 - backdrop) * (1 - source);
        if (mode == SvgBlendMode.Darken) return System.Math.Min(backdrop, source);
        if (mode == SvgBlendMode.Lighten) return System.Math.Max(backdrop, source);
        if (mode == SvgBlendMode.ColorDodge) return source >= 1 ? 1 : System.Math.Min(1, backdrop / (1 - source));
        if (mode == SvgBlendMode.ColorBurn) return source <= 0 ? 0 : 1 - System.Math.Min(1, (1 - backdrop) / source);
        if (mode == SvgBlendMode.HardLight) return source <= 0.5 ? 2 * backdrop * source : 1 - 2 * (1 - backdrop) * (1 - source);
        if (mode == SvgBlendMode.SoftLight)
        {
            if (source <= 0.5) return backdrop - (1 - 2 * source) * backdrop * (1 - backdrop);
            double curve = backdrop <= 0.25 ? ((16 * backdrop - 12) * backdrop + 4) * backdrop : System.Math.Sqrt(backdrop);
            return backdrop + (2 * source - 1) * (curve - backdrop);
        }
        if (mode == SvgBlendMode.Difference) return System.Math.Abs(backdrop - source);
        if (mode == SvgBlendMode.Exclusion) return backdrop + source - 2 * backdrop * source;
        return source;
    }

    static double[]? NonSeparable(SvgBlendMode mode, double backdropRed, double backdropGreen, double backdropBlue, double sourceRed, double sourceGreen, double sourceBlue)
    {
        double[] backdrop = Color(backdropRed, backdropGreen, backdropBlue);
        double[] source = Color(sourceRed, sourceGreen, sourceBlue);
        if (mode == SvgBlendMode.Hue) return SetLum(SetSat(source, Saturation(backdrop)), Luminosity(backdrop));
        if (mode == SvgBlendMode.Saturation) return SetLum(SetSat(backdrop, Saturation(source)), Luminosity(backdrop));
        if (mode == SvgBlendMode.Color) return SetLum(source, Luminosity(backdrop));
        if (mode == SvgBlendMode.Luminosity) return SetLum(backdrop, Luminosity(source));
        return null;
    }

    static double[] SetLum(double[] color, double luminosity)
    {
        double difference = luminosity - Luminosity(color);
        double[] adjusted = Color(color[0] + difference, color[1] + difference, color[2] + difference);
        return ClipColor(adjusted);
    }

    static double[] ClipColor(double[] color)
    {
        double luminosity = Luminosity(color);
        double minimum = System.Math.Min(color[0], System.Math.Min(color[1], color[2]));
        double maximum = System.Math.Max(color[0], System.Math.Max(color[1], color[2]));
        if (minimum < 0)
        {
            double denominator = luminosity - minimum;
            if (denominator != 0)
            {
                color[0] = luminosity + (color[0] - luminosity) * luminosity / denominator;
                color[1] = luminosity + (color[1] - luminosity) * luminosity / denominator;
                color[2] = luminosity + (color[2] - luminosity) * luminosity / denominator;
            }
        }
        maximum = System.Math.Max(color[0], System.Math.Max(color[1], color[2]));
        if (maximum > 1)
        {
            double denominator = maximum - luminosity;
            if (denominator != 0)
            {
                color[0] = luminosity + (color[0] - luminosity) * (1 - luminosity) / denominator;
                color[1] = luminosity + (color[1] - luminosity) * (1 - luminosity) / denominator;
                color[2] = luminosity + (color[2] - luminosity) * (1 - luminosity) / denominator;
            }
        }
        color[0] = Clamp(color[0]);
        color[1] = Clamp(color[1]);
        color[2] = Clamp(color[2]);
        return color;
    }

    static double[] SetSat(double[] color, double saturation)
    {
        int minimum = 0;
        int maximum = 0;
        int index = 1;
        while (index < 3)
        {
            if (color[index] < color[minimum]) minimum = index;
            if (color[index] > color[maximum]) maximum = index;
            index = index + 1;
        }
        if (minimum == maximum) return Color(0, 0, 0);
        int middle = 3 - minimum - maximum;
        double[] result = Color(color[0], color[1], color[2]);
        if (color[maximum] > color[minimum])
        {
            result[middle] = (color[middle] - color[minimum]) * saturation / (color[maximum] - color[minimum]);
            result[maximum] = saturation;
        }
        else
        {
            result[middle] = 0;
            result[maximum] = 0;
        }
        result[minimum] = 0;
        return result;
    }

    static double Saturation(double[] color)
    {
        return System.Math.Max(color[0], System.Math.Max(color[1], color[2])) - System.Math.Min(color[0], System.Math.Min(color[1], color[2]));
    }

    static double Luminosity(double[] color)
    {
        return 0.3 * color[0] + 0.59 * color[1] + 0.11 * color[2];
    }

    static double[] Color(double red, double green, double blue)
    {
        double[] result = new double[3];
        result[0] = red;
        result[1] = green;
        result[2] = blue;
        return result;
    }

    static SvgBlendColor Read(byte[] data, int index, SvgPixelColorSpace encoded, SvgPixelColorSpace working, double opacity)
    {
        double alpha = data[index + 3] / 255.0;
        double red = alpha == 0 ? 0 : data[index] / 255.0 / alpha;
        double green = alpha == 0 ? 0 : data[index + 1] / 255.0 / alpha;
        double blue = alpha == 0 ? 0 : data[index + 2] / 255.0 / alpha;
        red = Convert(red, encoded, working);
        green = Convert(green, encoded, working);
        blue = Convert(blue, encoded, working);
        alpha = Clamp(alpha * opacity);
        return new SvgBlendColor(red * alpha, green * alpha, blue * alpha, alpha);
    }

    static void Write(byte[] data, int index, SvgBlendColor color, SvgPixelColorSpace working, SvgPixelColorSpace encoded)
    {
        double red = color.Alpha == 0 ? 0 : color.Red / color.Alpha;
        double green = color.Alpha == 0 ? 0 : color.Green / color.Alpha;
        double blue = color.Alpha == 0 ? 0 : color.Blue / color.Alpha;
        red = Convert(red, working, encoded) * color.Alpha;
        green = Convert(green, working, encoded) * color.Alpha;
        blue = Convert(blue, working, encoded) * color.Alpha;
        data[index] = Byte(red);
        data[index + 1] = Byte(green);
        data[index + 2] = Byte(blue);
        data[index + 3] = Byte(color.Alpha);
    }

    static SvgPixelColorSpace Working(SvgBlendColorSpace selection, SvgPixelColorSpace destination)
    {
        if (selection == SvgBlendColorSpace.Srgb) return SvgPixelColorSpace.Srgb;
        if (selection == SvgBlendColorSpace.SrgbLinear) return SvgPixelColorSpace.SrgbLinear;
        return destination;
    }

    static double Convert(double value, SvgPixelColorSpace source, SvgPixelColorSpace destination)
    {
        value = Clamp(value);
        if (source == destination) return value;
        if (source == SvgPixelColorSpace.Srgb) return value <= 0.04045 ? value / 12.92 : System.Math.Pow((value + 0.055) / 1.055, 2.4);
        return value <= 0.0031308 ? value * 12.92 : 1.055 * System.Math.Pow(value, 1 / 2.4) - 0.055;
    }

    static double SourceFactor(SvgCompositeOperator operation, double sourceAlpha, double backdropAlpha)
    {
        if (operation == SvgCompositeOperator.Clear || operation == SvgCompositeOperator.Destination || operation == SvgCompositeOperator.DestinationIn || operation == SvgCompositeOperator.DestinationOut) return 0;
        if (operation == SvgCompositeOperator.SourceIn || operation == SvgCompositeOperator.SourceAtop) return backdropAlpha;
        if (operation == SvgCompositeOperator.DestinationOver || operation == SvgCompositeOperator.SourceOut || operation == SvgCompositeOperator.DestinationAtop || operation == SvgCompositeOperator.Xor) return 1 - backdropAlpha;
        return 1;
    }

    static double BackdropFactor(SvgCompositeOperator operation, double sourceAlpha, double backdropAlpha)
    {
        if (operation == SvgCompositeOperator.Clear || operation == SvgCompositeOperator.Copy || operation == SvgCompositeOperator.SourceIn || operation == SvgCompositeOperator.SourceOut) return 0;
        if (operation == SvgCompositeOperator.DestinationIn || operation == SvgCompositeOperator.DestinationAtop) return sourceAlpha;
        if (operation == SvgCompositeOperator.SourceOver || operation == SvgCompositeOperator.SourceAtop || operation == SvgCompositeOperator.DestinationOut || operation == SvgCompositeOperator.Xor) return 1 - sourceAlpha;
        return 1;
    }

    static double Straight(double premultiplied, double alpha)
    {
        return alpha == 0 ? 0 : Clamp(premultiplied / alpha);
    }

    static double Clamp(double value)
    {
        if (value <= 0) return 0;
        if (value >= 1) return 1;
        return value;
    }

    static byte Byte(double value)
    {
        value = Clamp(value);
        return (byte)System.Math.Round(value * 255, System.MidpointRounding.AwayFromZero);
    }
}
