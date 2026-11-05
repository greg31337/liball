namespace icc;

internal static class DescriptionFormatter
{
    internal static string DescribeVersionKind(IccVersionKind value)
    {
        switch (value)
        {
            case IccVersionKind.Version2:
                return "ICC.1 v2";
            case IccVersionKind.Version4:
                return "ICC.1 v4";
            case IccVersionKind.Version5:
                return "ICC.2 v5";
            case IccVersionKind.Unknown:
                return "Unknown ICC version";
            default:
                return $"Unknown ICC version ({(int)value})";
        }
    }

    internal static string DescribeProfileClassKind(IccProfileClass value)
    {
        switch (value)
        {
            case IccProfileClass.Input:
                return "Input device profile";
            case IccProfileClass.Display:
                return "Display device profile";
            case IccProfileClass.Output:
                return "Output device profile";
            case IccProfileClass.DeviceLink:
                return "DeviceLink profile";
            case IccProfileClass.ColorSpace:
                return "Colour-space conversion profile";
            case IccProfileClass.Abstract:
                return "Abstract profile";
            case IccProfileClass.NamedColor:
                return "Named-colour profile";
            case IccProfileClass.ColorEncodingSpace:
                return "ICC.2 colour-encoding-space profile";
            case IccProfileClass.MultiplexIdentification:
                return "ICC.2 multiplex-identification profile";
            case IccProfileClass.MultiplexLink:
                return "ICC.2 multiplex-link profile";
            case IccProfileClass.MultiplexVisualization:
                return "ICC.2 multiplex-visualization profile";
            case IccProfileClass.Unknown:
                return "Unknown profile class";
            default:
                return $"Unknown profile class ({(int)value})";
        }
    }

    internal static string DescribeColorSpaceKind(IccColorSpaceKind value)
    {
        switch (value)
        {
            case IccColorSpaceKind.Xyz:
                return "CIE XYZ";
            case IccColorSpaceKind.Lab:
                return "CIELAB";
            case IccColorSpaceKind.Luv:
                return "CIELUV";
            case IccColorSpaceKind.YCbCr:
                return "YCbCr";
            case IccColorSpaceKind.Yxy:
                return "CIE Yxy";
            case IccColorSpaceKind.Rgb:
                return "RGB";
            case IccColorSpaceKind.Gray:
                return "Grayscale";
            case IccColorSpaceKind.Hsv:
                return "HSV";
            case IccColorSpaceKind.Hls:
                return "HLS";
            case IccColorSpaceKind.Cmyk:
                return "CMYK";
            case IccColorSpaceKind.Cmy:
                return "CMY";
            case IccColorSpaceKind.NamedColor:
                return "Named colour";
            case IccColorSpaceKind.DeviceChannels:
                return "Device channels";
            case IccColorSpaceKind.ExtendedDevice:
                return "ICC.2 extended device channels";
            case IccColorSpaceKind.Spectral:
                return "Spectral";
            case IccColorSpaceKind.BiSpectral:
                return "Bi-spectral";
            case IccColorSpaceKind.Multiplex:
                return "Multiplex channels";
            case IccColorSpaceKind.Unknown:
                return "Unknown colour space";
            default:
                return $"Unknown colour space ({(int)value})";
        }
    }

    internal static string DescribePcsKind(IccPcsKind value)
    {
        switch (value)
        {
            case IccPcsKind.Xyz:
                return "CIE XYZ PCS";
            case IccPcsKind.Lab:
                return "CIELAB PCS";
            case IccPcsKind.Spectral:
                return "Spectral PCS";
            case IccPcsKind.BiSpectral:
                return "Bi-spectral PCS";
            case IccPcsKind.Material:
                return "Material Connection Space";
            case IccPcsKind.None:
                return "No PCS";
            default:
                return $"Unknown PCS ({(int)value})";
        }
    }

    internal static string DescribeRenderingIntentKind(IccRenderingIntent value)
    {
        switch (value)
        {
            case IccRenderingIntent.Perceptual:
                return "Perceptual";
            case IccRenderingIntent.MediaRelativeColorimetric:
                return "Media-relative colorimetric";
            case IccRenderingIntent.Saturation:
                return "Saturation";
            case IccRenderingIntent.IccAbsoluteColorimetric:
                return "ICC-absolute colorimetric";
            default:
                return $"Unknown rendering intent ({(int)value})";
        }
    }

    internal static string DescribeProfileVersion(IccVersion version)
    {
        if (version == null)
        {
            return "Unknown ICC version";
        }

        string generation = DescribeVersionKind(version.Kind);
        if (version.Major < 0 || version.Minor < 0 || version.BugFix < 0)
        {
            return generation;
        }

        return $"{generation} ({version.Major}.{version.Minor}.{version.BugFix})";
    }

    internal static string DescribeColorSpaceWithChannels(uint signature, IccColorSpaceKind kind)
    {
        int count = ColorSpaceChannelCount.Decode(signature);
        uint prefix = signature & 0xffff0000;
        IccColorSpaceKind resolvedKind = kind;
        if (resolvedKind == IccColorSpaceKind.Unknown)
        {
            resolvedKind = HeaderCodec.DecodeColorSpace(signature);
        }
        if (resolvedKind == IccColorSpaceKind.Unknown && (prefix == IccColorSpaceSignatures.ReflectanceSpectralPrefix
            || prefix == IccColorSpaceSignatures.TransmissionSpectralPrefix
            || prefix == IccColorSpaceSignatures.RadiantSpectralPrefix
            || prefix == IccColorSpaceSignatures.SparseMatrixSpectralPrefix))
        {
            resolvedKind = IccColorSpaceKind.Spectral;
        }
        else if (resolvedKind == IccColorSpaceKind.Unknown && prefix == IccColorSpaceSignatures.BiSpectralPrefix)
        {
            resolvedKind = IccColorSpaceKind.BiSpectral;
        }
        else if (resolvedKind == IccColorSpaceKind.Unknown && prefix == IccColorSpaceSignatures.MultiplexPrefix)
        {
            resolvedKind = IccColorSpaceKind.Multiplex;
        }

        if (count == 0 && prefix == IccColorSpaceSignatures.MultiplexPrefix)
        {
            count = (int)(signature & 0x0000ffff);
        }

        string name = DescribeColorSpaceKind(resolvedKind);
        string signatureText = SignatureDatabase.FormatDescription("FourCC", signature);
        if (count == 0)
        {
            return $"{name}; {signatureText}";
        }

        string noun = count == 1 ? "channel" : "channels";
        string labels = DescribeChannelLabels(signature, count);
        return $"{name}; {count} {noun} ({labels}); {signatureText}";
    }

    internal static string DescribeChannelLabels(uint signature, int count)
    {
        switch (signature)
        {
            case IccColorSpaceSignatures.Xyz:
                return "X, Y, Z";
            case IccColorSpaceSignatures.Lab:
                return "L*, a*, b*";
            case IccColorSpaceSignatures.Luv:
                return "L*, u*, v*";
            case IccColorSpaceSignatures.YCbCr:
                return "Y, Cb, Cr";
            case IccColorSpaceSignatures.Yxy:
                return "Y, x, y";
            case IccColorSpaceSignatures.Rgb:
                return "Red, Green, Blue";
            case IccColorSpaceSignatures.Gray:
                return "Gray";
            case IccColorSpaceSignatures.Hsv:
                return "Hue, Saturation, Value";
            case IccColorSpaceSignatures.Hls:
                return "Hue, Lightness, Saturation";
            case IccColorSpaceSignatures.Cmyk:
                return "Cyan, Magenta, Yellow, Black";
            case IccColorSpaceSignatures.Cmy:
                return "Cyan, Magenta, Yellow";
            default:
                break;
        }

        if (count == 1)
        {
            return "Channel 1";
        }

        return $"Channels 1-{count}";
    }

    internal static string SelectLocalizedText(IccLocalizedString[] values)
    {
        IccLocalizedString? selected = LocalizedUnicodeTagCodec.FindLocalizedString(values, "en", "US");
        if (selected == null)
        {
            return string.Empty;
        }

        if (selected.Text == null)
        {
            throw new IccProfileValidationException("A localized string text value cannot be null.");
        }

        return selected.Text;
    }

    internal static string DescribeLocalizedRecords(IccLocalizedString[] values, int maximumValues)
    {
        if (values == null)
        {
            throw new IccProfileValidationException("A localized-string collection cannot be null.");
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int limit = values.Length < maximumValues ? values.Length : maximumValues;
        int index = 0;
        while (index < limit)
        {
            IccLocalizedString value = values[index];
            if (value == null || value.Language == null || value.Country == null || value.Text == null)
            {
                throw new IccProfileValidationException($"Localized string record {index} cannot be null and must contain locale and text values.");
            }

            if (index != 0)
            {
                builder.Append(", ");
            }

            builder.Append(value.Language);
            builder.Append('-');
            builder.Append(value.Country);
            builder.Append("=\"");
            builder.Append(EscapeText(value.Text));
            builder.Append('"');
            index = index + 1;
        }

        if (limit < values.Length)
        {
            if (limit != 0)
            {
                builder.Append(", ");
            }

            builder.Append($"… {values.Length - limit} more");
        }

        return builder.ToString();
    }

    internal static string DescribeCicp(IccCicp value)
    {
        if (value == null)
        {
            throw new IccProfileValidationException("CICP metadata cannot be null when it is described.");
        }

        string primaries = value.UsesProfilePrimaries
            ? "profile RGB matrix primaries (code 2 amendment)"
            : DescribeCicpPrimaries(value.ColorPrimaries);
        string range = value.FullRange ? "full range" : "limited range";
        return $"primaries: {primaries}; transfer: {DescribeCicpTransfer(value.TransferCharacteristics)}; matrix: {DescribeCicpMatrix(value.MatrixCoefficients)}; {range}";
    }

    internal static string DescribeColor(IccColorDescription value)
    {
        if (value == null)
        {
            throw new IccProfileValidationException("A colour description cannot be null.");
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append('"');
        builder.Append(EscapeText(value.Name == null ? string.Empty : value.Name));
        builder.Append('"');
        if (value.Lab != null)
        {
            builder.Append($"; Lab {FormatTriple(value.Lab.L, value.Lab.A, value.Lab.B)}");
        }
        if (value.Xyz != null)
        {
            builder.Append($"; XYZ {FormatTriple(value.Xyz.X, value.Xyz.Y, value.Xyz.Z)}");
        }
        if (value.XyY != null)
        {
            builder.Append($"; xyY {FormatTriple(value.XyY.X, value.XyY.Y, value.XyY.Luminance)}");
        }
        if (value.Lch != null)
        {
            builder.Append($"; LCh {FormatTriple(value.Lch.L, value.Lch.C, value.Lch.HueDegrees)}");
        }
        if (value.DeviceColor != null)
        {
            builder.Append($"; device {FormatComponents(value.DeviceColor.Components)}");
        }

        return builder.ToString();
    }

    internal static string DescribeCicpPrimaries(int value)
    {
        switch (value)
        {
            case 1:
                return "ITU-R BT.709 (code 1)";
            case 2:
                return "unspecified (code 2)";
            case 4:
                return "ITU-R BT.470 System M (code 4)";
            case 5:
                return "ITU-R BT.470 System B/G (code 5)";
            case 6:
                return "SMPTE 170M (code 6)";
            case 7:
                return "SMPTE 240M (code 7)";
            case 8:
                return "generic film (code 8)";
            case 9:
                return "ITU-R BT.2020 (code 9)";
            case 10:
                return "SMPTE ST 428-1 (code 10)";
            case 11:
                return "SMPTE RP 431-2 (code 11)";
            case 12:
                return "SMPTE EG 432-1 (code 12)";
            case 22:
                return "EBU Tech 3213-E coordinates (code 22)";
            default:
                return $"unknown/reserved code {value}";
        }
    }

    internal static string DescribeCicpTransfer(int value)
    {
        switch (value)
        {
            case 1:
                return "ITU-R BT.709 (code 1)";
            case 2:
                return "unspecified (code 2)";
            case 4:
                return "gamma 2.2 (code 4)";
            case 5:
                return "gamma 2.8 (code 5)";
            case 6:
                return "ITU-R BT.601 (code 6)";
            case 7:
                return "SMPTE 240M (code 7)";
            case 8:
                return "linear (code 8)";
            case 9:
                return "logarithmic 100:1 (code 9)";
            case 10:
                return "logarithmic 316:1 (code 10)";
            case 11:
                return "IEC 61966-2-4 (code 11)";
            case 12:
                return "ITU-R BT.1361 extended gamut (code 12)";
            case 13:
                return "sRGB (code 13)";
            case 14:
                return "ITU-R BT.2020 10-bit (code 14)";
            case 15:
                return "ITU-R BT.2020 12-bit (code 15)";
            case 16:
                return "SMPTE ST 2084 PQ (code 16)";
            case 17:
                return "SMPTE ST 428-1 (code 17)";
            case 18:
                return "ITU-R BT.2100 HLG (code 18)";
            default:
                return $"unknown/reserved code {value}";
        }
    }

    internal static string DescribeCicpMatrix(int value)
    {
        switch (value)
        {
            case 0:
                return "identity/RGB (code 0)";
            case 1:
                return "ITU-R BT.709 (code 1)";
            case 2:
                return "unspecified (code 2)";
            case 4:
                return "FCC 73.682 (code 4)";
            case 5:
                return "ITU-R BT.470 System B/G (code 5)";
            case 6:
                return "ITU-R BT.601 (code 6)";
            case 7:
                return "SMPTE 240M (code 7)";
            case 8:
                return "YCgCo (code 8)";
            case 9:
                return "ITU-R BT.2020 non-constant luminance (code 9)";
            case 10:
                return "ITU-R BT.2020 constant luminance (code 10)";
            case 11:
                return "SMPTE ST 2085 YDzDx (code 11)";
            case 12:
                return "chromaticity-derived non-constant luminance (code 12)";
            case 13:
                return "chromaticity-derived constant luminance (code 13)";
            case 14:
                return "ICtCp (code 14)";
            default:
                return $"unknown/reserved code {value}";
        }
    }

    internal static string EscapeText(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\"", "\\\"");
    }

    internal static string FormatTriple(double first, double second, double third)
    {
        return $"({FormatNumber(first)}, {FormatNumber(second)}, {FormatNumber(third)})";
    }

    internal static string FormatComponents(double[] values)
    {
        if (values == null)
        {
            throw new IccProfileValidationException("Colour components cannot be null when they are described.");
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append('(');
        int index = 0;
        while (index < values.Length)
        {
            if (index != 0)
            {
                builder.Append(", ");
            }

            builder.Append(FormatNumber(values[index]));
            index = index + 1;
        }

        builder.Append(')');
        return builder.ToString();
    }

    internal static string FormatNumber(double value)
    {
        return value.ToString("G10", System.Globalization.CultureInfo.InvariantCulture);
    }
}
