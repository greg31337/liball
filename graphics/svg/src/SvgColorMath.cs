namespace svg;

/// <summary>Converts, interpolates, clips, and formats supported color values.</summary>
internal static class SvgColorMath
{
    /// <summary>Determines whether a color-space value is supported.</summary>
    /// <param name="space">The color-space value.</param>
    /// <returns><c>true</c> for a supported color space.</returns>
    public static bool IsKnownSpace(SvgColorSpace space)
    {
        return space == SvgColorSpace.Srgb || space == SvgColorSpace.SrgbLinear || space == SvgColorSpace.Hwb || space == SvgColorSpace.Lab || space == SvgColorSpace.Lch || space == SvgColorSpace.Oklab || space == SvgColorSpace.Oklch;
    }

    /// <summary>Converts a color to a supported destination space.</summary>
    /// <param name="color">The source color.</param>
    /// <param name="targetSpace">The destination space.</param>
    /// <returns>The converted color or a stable failure.</returns>
    public static SvgColorOperationResult Convert(SvgColor color, SvgColorSpace targetSpace)
    {
        SvgColorOperationResult? validation = Validate(color, targetSpace);
        if (validation != null) return validation;
        if (color.IsCurrentColor) return Failure(SvgColorOperationError.CurrentColorUnresolved);
        if (color.Space == targetSpace) return Success(Copy(color));
        double[] linear = ToLinearSrgb(color);
        if (!AreFinite(linear)) return Failure(SvgColorOperationError.NonFiniteResult);
        double[] channels = FromLinearSrgb(linear, targetSpace);
        if (!AreFinite(channels)) return Failure(SvgColorOperationError.NonFiniteResult);
        SvgColor converted = new SvgColor(targetSpace, channels[0], channels[1], channels[2], color.Alpha);
        converted.AlphaMissing = color.AlphaMissing;
        return Success(converted);
    }

    /// <summary>Interpolates two colors using premultiplied alpha.</summary>
    /// <param name="first">The starting color.</param>
    /// <param name="second">The ending color.</param>
    /// <param name="amount">The interpolation amount from zero through one.</param>
    /// <param name="space">The interpolation space.</param>
    /// <returns>The interpolated color or a stable failure.</returns>
    public static SvgColorOperationResult Interpolate(SvgColor first, SvgColor second, double amount, SvgColorSpace space)
    {
        SvgColorOperationResult? firstValidation = Validate(first, space);
        if (firstValidation != null) return firstValidation;
        SvgColorOperationResult? secondValidation = Validate(second, space);
        if (secondValidation != null) return secondValidation;
        if (!SvgFiniteMath.IsFinite(amount) || amount < 0 || amount > 1) return Failure(SvgColorOperationError.InvalidArgument);
        if (first.IsCurrentColor || second.IsCurrentColor) return Failure(SvgColorOperationError.CurrentColorUnresolved);
        SvgColorOperationResult firstResult = Convert(first, space);
        SvgColorOperationResult secondResult = Convert(second, space);
        if (!firstResult.Success) return firstResult;
        if (!secondResult.Success) return secondResult;
        SvgColor? a = firstResult.Color;
        SvgColor? b = secondResult.Color;
        ApplyMissingFixup(a, b);
        double alpha = Lerp((a ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Alpha, (b ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Alpha, amount);
        bool alphaMissing = a.AlphaMissing && b.AlphaMissing;
        double firstPremultiplyAlpha = alphaMissing ? 1 : a.Alpha;
        double secondPremultiplyAlpha = alphaMissing ? 1 : b.Alpha;
        double premultipliedAlpha = alphaMissing ? 1 : alpha;
        bool polar = space == SvgColorSpace.Lch || space == SvgColorSpace.Oklch;
        bool hwb = space == SvgColorSpace.Hwb;
        double firstChannel = hwb ? InterpolateHue(a.Red, b.Red, amount) : InterpolatePremultiplied(a.Red, b.Red, firstPremultiplyAlpha, secondPremultiplyAlpha, premultipliedAlpha, amount);
        double secondChannel = InterpolatePremultiplied(a.Green, b.Green, firstPremultiplyAlpha, secondPremultiplyAlpha, premultipliedAlpha, amount);
        double thirdChannel;
        if (polar)
        {
            thirdChannel = InterpolateHue(a.Blue, b.Blue, amount);
        }
        else
        {
            thirdChannel = InterpolatePremultiplied(a.Blue, b.Blue, firstPremultiplyAlpha, secondPremultiplyAlpha, premultipliedAlpha, amount);
        }
        if (!SvgFiniteMath.IsFinite(firstChannel) || !SvgFiniteMath.IsFinite(secondChannel) || !SvgFiniteMath.IsFinite(thirdChannel) || !SvgFiniteMath.IsFinite(alpha)) return Failure(SvgColorOperationError.NonFiniteResult);
        SvgColor result = new SvgColor(space, firstChannel, secondChannel, thirdChannel, alpha);
        result.FirstMissing = a.FirstMissing && b.FirstMissing;
        result.SecondMissing = a.SecondMissing && b.SecondMissing;
        result.ThirdMissing = a.ThirdMissing && b.ThirdMissing;
        result.AlphaMissing = alphaMissing;
        return Success(result);
    }

    /// <summary>Determines whether a color is in the sRGB gamut.</summary>
    /// <param name="color">The color to test.</param>
    /// <returns><c>true</c> when converted channels are from zero through one.</returns>
    public static bool IsInSrgbGamut(SvgColor color)
    {
        SvgColorOperationResult converted = Convert(color, SvgColorSpace.Srgb);
        if (!converted.Success) return false;
        SvgColor? value = converted.Color;
        double tolerance = 0.0000001;
        return (value ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Red >= -tolerance && value.Red <= 1 + tolerance && value.Green >= -tolerance && value.Green <= 1 + tolerance && value.Blue >= -tolerance && value.Blue <= 1 + tolerance;
    }

    /// <summary>Converts a color to sRGB and clips its channels.</summary>
    /// <param name="color">The color to clip.</param>
    /// <returns>The clipped color or a stable failure.</returns>
    public static SvgColorOperationResult ClipToSrgbGamut(SvgColor color)
    {
        SvgColorOperationResult converted = Convert(color, SvgColorSpace.Srgb);
        if (!converted.Success) return converted;
        SvgColor? value = converted.Color;
        SvgColor clipped = new SvgColor(SvgColorSpace.Srgb, Clamp((value ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Red), Clamp(value.Green), Clamp(value.Blue), value.Alpha);
        clipped.AlphaMissing = value.AlphaMissing;
        return Success(clipped);
    }

    /// <summary>Formats a color in canonical functional notation.</summary>
    /// <param name="color">The color to format.</param>
    /// <returns>The canonical text, or an empty string for invalid state.</returns>
    public static string Format(SvgColor? color)
    {
        SvgColorOperationResult? validation = Validate(color, color == null ? SvgColorSpace.Srgb : color.Space);
        if (validation != null) return "";
        if ((color ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).IsCurrentColor) return "currentColor";
        string name;
        if (color.Space == SvgColorSpace.Srgb) name = "color(srgb";
        else if (color.Space == SvgColorSpace.SrgbLinear) name = "color(srgb-linear";
        else if (color.Space == SvgColorSpace.Hwb) name = "hwb(";
        else if (color.Space == SvgColorSpace.Lab) name = "lab(";
        else if (color.Space == SvgColorSpace.Lch) name = "lch(";
        else if (color.Space == SvgColorSpace.Oklab) name = "oklab(";
        else name = "oklch(";
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append(name);
        if (color.Space == SvgColorSpace.Srgb || color.Space == SvgColorSpace.SrgbLinear) builder.Append(' ');
        AppendComponent(builder, color.Red, color.FirstMissing);
        builder.Append(' ');
        AppendComponent(builder, color.Green, color.SecondMissing);
        if (color.Space == SvgColorSpace.Hwb && !color.SecondMissing) builder.Append('%');
        builder.Append(' ');
        AppendComponent(builder, color.Blue, color.ThirdMissing);
        if (color.Space == SvgColorSpace.Hwb && !color.ThirdMissing) builder.Append('%');
        if (color.Alpha != 1 || color.AlphaMissing)
        {
            builder.Append(" / ");
            AppendComponent(builder, color.Alpha, color.AlphaMissing);
        }
        builder.Append(')');
        return builder.ToString();
    }

    static SvgColorOperationResult? Validate(SvgColor? color, SvgColorSpace space)
    {
        if (color == null || !IsKnownSpace(space) || !color.IsValid || !IsKnownSpace(color.Space) || !SvgFiniteMath.IsFinite(color.Red) || !SvgFiniteMath.IsFinite(color.Green) || !SvgFiniteMath.IsFinite(color.Blue) || !SvgFiniteMath.IsFinite(color.Alpha) || color.Alpha < 0 || color.Alpha > 1)
        {
            return Failure(SvgColorOperationError.InvalidArgument);
        }
        return null;
    }

    static SvgColor Copy(SvgColor color)
    {
        SvgColor copy = new SvgColor(color.Space, color.Red, color.Green, color.Blue, color.Alpha);
        copy.IsCurrentColor = color.IsCurrentColor;
        copy.FirstMissing = color.FirstMissing;
        copy.SecondMissing = color.SecondMissing;
        copy.ThirdMissing = color.ThirdMissing;
        copy.AlphaMissing = color.AlphaMissing;
        copy.Source = color.Source;
        return copy;
    }

    static double[] ToLinearSrgb(SvgColor color)
    {
        if (color.Space == SvgColorSpace.Srgb)
        {
            return new double[] { Decode(color.Red), Decode(color.Green), Decode(color.Blue) };
        }
        if (color.Space == SvgColorSpace.SrgbLinear)
        {
            return new double[] { color.Red, color.Green, color.Blue };
        }
        if (color.Space == SvgColorSpace.Hwb)
        {
            double white = color.Green / 100;
            double black = color.Blue / 100;
            double sum = white + black;
            double red;
            double green;
            double blue;
            if (sum >= 1)
            {
                red = white / sum;
                green = red;
                blue = red;
            }
            else
            {
                double hue = color.Red / 30;
                double factor = 1 - sum;
                red = HwbChannel(0, hue) * factor + white;
                green = HwbChannel(8, hue) * factor + white;
                blue = HwbChannel(4, hue) * factor + white;
            }
            return new double[] { Decode(red), Decode(green), Decode(blue) };
        }
        if (color.Space == SvgColorSpace.Oklab)
        {
            return OklabToLinear(new double[] { color.Red, color.Green, color.Blue });
        }
        if (color.Space == SvgColorSpace.Oklch)
        {
            return OklabToLinear(PolarToRectangular(new double[] { color.Red, color.Green, color.Blue }));
        }
        double[] lab = color.Space == SvgColorSpace.Lab ? new double[] { color.Red, color.Green, color.Blue } : PolarToRectangular(new double[] { color.Red, color.Green, color.Blue });
        double[] xyzD50 = LabToXyz(lab);
        double[] xyzD65 = Multiply(xyzD50, 0.955473421488075, -0.02309845494876471, 0.06325924320057072, -0.0283697093338637, 1.0099953980813041, 0.021041441191917323, 0.012314014864481998, -0.020507649298898964, 1.330365926242124);
        return XyzToLinear(xyzD65);
    }

    static double[] FromLinearSrgb(double[] linear, SvgColorSpace space)
    {
        if (space == SvgColorSpace.Srgb)
        {
            return new double[] { Encode(linear[0]), Encode(linear[1]), Encode(linear[2]) };
        }
        if (space == SvgColorSpace.SrgbLinear)
        {
            return new double[] { linear[0], linear[1], linear[2] };
        }
        if (space == SvgColorSpace.Hwb) return LinearToHwb(linear);
        if (space == SvgColorSpace.Oklab) return LinearToOklab(linear);
        if (space == SvgColorSpace.Oklch) return RectangularToPolar(LinearToOklab(linear));
        double[] xyzD65 = LinearToXyz(linear);
        double[] xyzD50 = Multiply(xyzD65, 1.0479297925449969, 0.022946870601609652, -0.05019226628920524, 0.02962780877005599, 0.9904344267538799, -0.017073799063418826, -0.009243040646204504, 0.015055191490298152, 0.7518742814281371);
        double[] lab = XyzToLab(xyzD50);
        return space == SvgColorSpace.Lab ? lab : RectangularToPolar(lab);
    }

    static double[] LinearToXyz(double[] rgb)
    {
        return Multiply(rgb, 506752.0 / 1228815, 87881.0 / 245763, 12673.0 / 70218, 87098.0 / 409605, 175762.0 / 245763, 12673.0 / 175545, 7918.0 / 409605, 87881.0 / 737289, 1001167.0 / 1053270);
    }

    static double[] XyzToLinear(double[] xyz)
    {
        return Multiply(xyz, 12831.0 / 3959, -329.0 / 214, -1974.0 / 3959, -851781.0 / 878810, 1648619.0 / 878810, 36519.0 / 878810, 705.0 / 12673, -2585.0 / 12673, 705.0 / 667);
    }

    static double[] XyzToLab(double[] xyz)
    {
        double epsilon = 216.0 / 24389;
        double kappa = 24389.0 / 27;
        double fx = LabRoot(xyz[0] / (0.3457 / 0.3585), epsilon, kappa);
        double fy = LabRoot(xyz[1], epsilon, kappa);
        double fz = LabRoot(xyz[2] / ((1 - 0.3457 - 0.3585) / 0.3585), epsilon, kappa);
        return new double[] { 116 * fy - 16, 500 * (fx - fy), 200 * (fy - fz) };
    }

    static double[] LabToXyz(double[] lab)
    {
        double epsilon = 216.0 / 24389;
        double kappa = 24389.0 / 27;
        double fy = (lab[0] + 16) / 116;
        double fx = lab[1] / 500 + fy;
        double fz = fy - lab[2] / 200;
        double x = LabCube(fx, epsilon, kappa);
        double y = lab[0] > kappa * epsilon ? fy * fy * fy : lab[0] / kappa;
        double z = LabCube(fz, epsilon, kappa);
        return new double[] { x * (0.3457 / 0.3585), y, z * ((1 - 0.3457 - 0.3585) / 0.3585) };
    }

    static double LabRoot(double value, double epsilon, double kappa)
    {
        return value > epsilon ? System.Math.Cbrt(value) : (kappa * value + 16) / 116;
    }

    static double LabCube(double value, double epsilon, double kappa)
    {
        double cube = value * value * value;
        return cube > epsilon ? cube : (116 * value - 16) / kappa;
    }

    static double[] LinearToOklab(double[] rgb)
    {
        double[] xyz = LinearToXyz(rgb);
        double[] lms = Multiply(xyz, 0.8190224379967030, 0.3619062600528904, -0.1288737815209879, 0.0329836539323885, 0.9292868615863434, 0.0361446663506424, 0.0481771893596242, 0.2642395317527308, 0.6335478284694309);
        lms[0] = System.Math.Cbrt(lms[0]);
        lms[1] = System.Math.Cbrt(lms[1]);
        lms[2] = System.Math.Cbrt(lms[2]);
        return Multiply(lms, 0.2104542683093140, 0.7936177747023054, -0.0040720430116193, 1.9779985324311684, -2.4285922420485799, 0.4505937096174110, 0.0259040424655478, 0.7827717124575296, -0.8086757549230774);
    }

    static double[] LinearToHwb(double[] linear)
    {
        double red = Encode(linear[0]);
        double green = Encode(linear[1]);
        double blue = Encode(linear[2]);
        double maximum = System.Math.Max(red, System.Math.Max(green, blue));
        double minimum = System.Math.Min(red, System.Math.Min(green, blue));
        double hue = 0;
        double difference = maximum - minimum;
        if (difference != 0)
        {
            if (maximum == red) hue = 60 * ((green - blue) / difference % 6);
            else if (maximum == green) hue = 60 * ((blue - red) / difference + 2);
            else hue = 60 * ((red - green) / difference + 4);
        }
        return new double[] { NormalizeHue(hue), minimum * 100, (1 - maximum) * 100 };
    }

    static double[] OklabToLinear(double[] lab)
    {
        double[] lms = Multiply(lab, 1, 0.3963377773761749, 0.2158037573099136, 1, -0.1055613458156586, -0.0638541728258133, 1, -0.0894841775298119, -1.2914855480194092);
        lms[0] = lms[0] * lms[0] * lms[0];
        lms[1] = lms[1] * lms[1] * lms[1];
        lms[2] = lms[2] * lms[2] * lms[2];
        double[] xyz = Multiply(lms, 1.2268798758459243, -0.5578149944602171, 0.2813910456659647, -0.0405757452148008, 1.1122868032803170, -0.0717110580655164, -0.0763729366746601, -0.4214933324022432, 1.5869240198367816);
        return XyzToLinear(xyz);
    }

    static double[] RectangularToPolar(double[] rectangular)
    {
        double chroma = System.Math.Sqrt(rectangular[1] * rectangular[1] + rectangular[2] * rectangular[2]);
        double hue = NormalizeHue(System.Math.Atan2(rectangular[2], rectangular[1]) * 180 / System.Math.PI);
        if (chroma <= 0.000000000001) hue = 0;
        return new double[] { rectangular[0], chroma, hue };
    }

    static double[] PolarToRectangular(double[] polar)
    {
        double radians = polar[2] * System.Math.PI / 180;
        return new double[] { polar[0], polar[1] * System.Math.Cos(radians), polar[1] * System.Math.Sin(radians) };
    }

    static double[] Multiply(double[] value, double m00, double m01, double m02, double m10, double m11, double m12, double m20, double m21, double m22)
    {
        return new double[] { m00 * value[0] + m01 * value[1] + m02 * value[2], m10 * value[0] + m11 * value[1] + m12 * value[2], m20 * value[0] + m21 * value[1] + m22 * value[2] };
    }

    static double Decode(double value)
    {
        double sign = value < 0 ? -1 : 1;
        double absolute = System.Math.Abs(value);
        if (absolute <= 0.04045) return value / 12.92;
        return sign * System.Math.Pow((absolute + 0.055) / 1.055, 2.4);
    }

    static double Encode(double value)
    {
        double sign = value < 0 ? -1 : 1;
        double absolute = System.Math.Abs(value);
        if (absolute <= 0.0031308) return value * 12.92;
        return sign * (1.055 * System.Math.Pow(absolute, 1 / 2.4) - 0.055);
    }

    static double HwbChannel(double offset, double hue)
    {
        double k = (offset + hue) % 12;
        if (k < 0) k = k + 12;
        double middle = System.Math.Min(k - 3, 9 - k);
        middle = System.Math.Min(middle, 1);
        middle = System.Math.Max(-1, middle);
        return 0.5 - 0.5 * middle;
    }

    static void ApplyMissingFixup(SvgColor? first, SvgColor? second)
    {
        bool firstMissing = (first ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FirstMissing;
        bool secondMissing = first.SecondMissing;
        bool thirdMissing = first.ThirdMissing;
        bool alphaMissing = first.AlphaMissing;
        if (first.FirstMissing && !(second ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FirstMissing) first.Red = second.Red;
        if ((second ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FirstMissing && !firstMissing) second.Red = first.Red;
        if (first.SecondMissing && !second.SecondMissing) first.Green = second.Green;
        if (second.SecondMissing && !secondMissing) second.Green = first.Green;
        if (first.ThirdMissing && !second.ThirdMissing) first.Blue = second.Blue;
        if (second.ThirdMissing && !thirdMissing) second.Blue = first.Blue;
        if (first.AlphaMissing && !second.AlphaMissing) first.Alpha = second.Alpha;
        if (second.AlphaMissing && !alphaMissing) second.Alpha = first.Alpha;
    }

    static double InterpolatePremultiplied(double first, double second, double firstAlpha, double secondAlpha, double alpha, double amount)
    {
        double value = Lerp(first * firstAlpha, second * secondAlpha, amount);
        if (alpha == 0) return 0;
        return value / alpha;
    }

    static double Lerp(double first, double second, double amount)
    {
        return first + (second - first) * amount;
    }

    static double InterpolateHue(double first, double second, double amount)
    {
        double delta = second - first;
        if (delta > 180) delta = delta - 360;
        if (delta < -180) delta = delta + 360;
        return NormalizeHue(first + delta * amount);
    }

    static double NormalizeHue(double hue)
    {
        double result = hue % 360;
        if (result < 0) result = result + 360;
        return SvgFiniteMath.NormalizeZero(result);
    }

    static double Clamp(double value)
    {
        if (value < 0) return 0;
        if (value > 1) return 1;
        return SvgFiniteMath.NormalizeZero(value);
    }

    static bool AreFinite(double[] values)
    {
        return SvgFiniteMath.IsFinite(values[0]) && SvgFiniteMath.IsFinite(values[1]) && SvgFiniteMath.IsFinite(values[2]);
    }

    static void AppendComponent(System.Text.StringBuilder builder, double value, bool missing)
    {
        if (missing) builder.Append("none");
        else builder.Append(SvgNumberFormatter.FormatShortest(value).Text);
    }

    static SvgColorOperationResult Success(SvgColor color)
    {
        return new SvgColorOperationResult(true, SvgColorOperationError.None, color);
    }

    static SvgColorOperationResult Failure(SvgColorOperationError error)
    {
        return new SvgColorOperationResult(false, error, null);
    }
}
