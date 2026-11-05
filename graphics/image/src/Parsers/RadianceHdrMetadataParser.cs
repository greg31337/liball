using System.Collections.Generic;
using System.Text;

namespace image;

internal class RadianceHeaderVariable
{
    internal string Name;
    internal string Value;
    internal long Offset;
    internal long Length;

    internal RadianceHeaderVariable()
    {
        Name = "";
        Value = "";
        Offset = -1;
        Length = 0;
    }
}

internal class RadianceLine
{
    internal string Text;
    internal long Offset;
    internal long Length;
    internal bool Complete;

    internal RadianceLine()
    {
        Text = "";
        Offset = -1;
        Length = 0;
        Complete = false;
    }
}

internal class RadianceHeader
{
    internal string Signature;
    internal string Format;
    internal int FormatCount;
    internal int Width;
    internal int Height;
    internal string FirstAxis;
    internal string SecondAxis;
    internal string ResolutionText;
    internal ImageOrientation Orientation;
    internal long ResolutionOffset;
    internal long DataOffset;
    internal double Exposure;
    internal int ExposureCount;
    internal double PixelAspect;
    internal int PixelAspectCount;
    internal double ColorCorrectionRed;
    internal double ColorCorrectionGreen;
    internal double ColorCorrectionBlue;
    internal int ColorCorrectionCount;
    internal double Gamma;
    internal int GammaCount;
    internal string Software;
    internal string View;
    internal List<RadianceHeaderVariable> Variables;
    internal List<string> Comments;
    internal List<string> Commands;

    internal RadianceHeader()
    {
        Signature = "";
        Format = "";
        FormatCount = 0;
        Width = 0;
        Height = 0;
        FirstAxis = "";
        SecondAxis = "";
        ResolutionText = "";
        Orientation = ImageOrientation.Unknown;
        ResolutionOffset = -1;
        DataOffset = -1;
        Exposure = 1;
        ExposureCount = 0;
        PixelAspect = 1;
        PixelAspectCount = 0;
        ColorCorrectionRed = 1;
        ColorCorrectionGreen = 1;
        ColorCorrectionBlue = 1;
        ColorCorrectionCount = 0;
        Gamma = 0;
        GammaCount = 0;
        Software = "";
        View = "";
        Variables = new List<RadianceHeaderVariable>();
        Comments = new List<string>();
        Commands = new List<string>();
    }
}

internal class RadianceHdrMetadataParser
{
    internal const string RadianceSignature = "#?RADIANCE";
    internal const string RgbeSignature = "#?RGBE";
    internal const string RgbeFormat = "32-bit_rle_rgbe";
    internal const string XyzeFormat = "32-bit_rle_xyze";

    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = BasicMetadataParser.CopyFormat(format);
        if (reader == null)
        {
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "radiance.reader.null", "Radiance HDR parser received a null reader.");
            return document;
        }

        document.ByteLength = reader.Length;
        if (MaxSegmentBytes(options) < 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "radiance.header.limit.negative", "Radiance header segment limit was negative.", "Radiance Header", 0, MaxSegmentBytes(options));
            return Finish(document, reader);
        }
        if (!reader.SeekAbsolute(0))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "radiance.header.seek", "Radiance header could not be reached.", "Radiance Header", 0, 0);
            return Finish(document, reader);
        }

        RadianceHeader header = new RadianceHeader();
        RadianceLine signature = ReadLine(document, reader, options);
        if (!signature.Complete)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "radiance.signature.truncated", "Radiance signature line was truncated.", "Radiance Header", 0, signature.Length);
            return Finish(document, reader);
        }
        header.Signature = signature.Text;
        if (header.Signature != RadianceSignature && header.Signature != RgbeSignature)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "radiance.signature.invalid", "Radiance HDR signature was invalid.", "Radiance Header", 0, signature.Length);
            return Finish(document, reader);
        }

        bool variablesComplete = ParseInformationHeader(document, reader, options, header);
        if (!variablesComplete)
        {
            return Finish(document, reader);
        }
        bool resolutionValid = ParseResolution(document, reader, options, header);
        AddHeaderDirectory(document, header);
        CaptureHeader(document, reader, options, header.DataOffset);
        ValidateFormat(document, header);
        ValidateRaster(document, reader, header);
        if (resolutionValid)
        {
            AddFrame(document, header);
        }
        ImageMetadataNormalizerCore.BuildSummary(document);
        return Finish(document, reader);
    }

    internal static bool ParseInformationHeader(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, RadianceHeader header)
    {
        int tagCount = 0;
        while (true)
        {
            RadianceLine line = ReadLine(document, reader, options);
            if (!line.Complete)
            {
                if (!HasDiagnostic(document, "radiance.header.limit"))
                {
                    MetadataDiagnostic.AddTruncated(document.Diagnostics, "radiance.header.terminator.missing", "Radiance information header ended before its empty terminator line.", "Radiance Header", line.Offset, line.Length);
                }
                return false;
            }
            if (line.Text.Length == 0)
            {
                return true;
            }
            if (tagCount >= MaxTagCount(options))
            {
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "radiance.header.tag.limit", "Radiance information header exceeded the configured tag limit.", "Radiance Header", line.Offset, line.Length);
                return false;
            }
            tagCount = tagCount + 1;
            ParseInformationLine(document, header, line);
        }
    }

    internal static void ParseInformationLine(ImageMetadataDocument document, RadianceHeader header, RadianceLine line)
    {
        if (line.Text[0] == '#')
        {
            header.Comments.Add(line.Text.Substring(1).Trim());
            return;
        }
        if (line.Text[0] == '!')
        {
            header.Commands.Add(line.Text.Substring(1).Trim());
            return;
        }
        int separator = line.Text.IndexOf('=');
        if (separator <= 0)
        {
            header.Commands.Add(line.Text.Trim());
            return;
        }
        string name = line.Text.Substring(0, separator).Trim();
        string value = line.Text.Substring(separator + 1).Trim();
        RadianceHeaderVariable variable = new RadianceHeaderVariable();
        variable.Name = name;
        variable.Value = value;
        variable.Offset = line.Offset;
        variable.Length = line.Length;
        header.Variables.Add(variable);
        ApplyKnownVariable(document, header, variable);
    }

    internal static void ApplyKnownVariable(ImageMetadataDocument document, RadianceHeader header, RadianceHeaderVariable variable)
    {
        if (variable.Name == "FORMAT")
        {
            header.FormatCount = header.FormatCount + 1;
            if (header.FormatCount > 1)
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "radiance.format.duplicate", "Radiance information header contained more than one FORMAT assignment.", "Radiance Header", variable.Offset, variable.Length);
            }
            header.Format = variable.Value;
            return;
        }
        if (variable.Name == "EXPOSURE")
        {
            double value = NetpbmMetadataParser.ParseDecimal(variable.Value);
            if (value <= 0)
            {
                AddVariableDiagnostic(document, "radiance.exposure.invalid", "Radiance EXPOSURE value was not a positive number.", variable);
                return;
            }
            header.Exposure = header.Exposure * value;
            header.ExposureCount = header.ExposureCount + 1;
            return;
        }
        if (variable.Name == "PIXASPECT")
        {
            double value = NetpbmMetadataParser.ParseDecimal(variable.Value);
            if (value <= 0)
            {
                AddVariableDiagnostic(document, "radiance.pixel-aspect.invalid", "Radiance PIXASPECT value was not a positive number.", variable);
                return;
            }
            header.PixelAspect = header.PixelAspect * value;
            header.PixelAspectCount = header.PixelAspectCount + 1;
            return;
        }
        if (variable.Name == "COLORCORR")
        {
            double[] values = ParseNumberList(variable.Value, 3);
            if (values.Length != 3 || values[0] <= 0 || values[1] <= 0 || values[2] <= 0)
            {
                AddVariableDiagnostic(document, "radiance.color-correction.invalid", "Radiance COLORCORR value did not contain three positive numbers.", variable);
                return;
            }
            header.ColorCorrectionRed = header.ColorCorrectionRed * values[0];
            header.ColorCorrectionGreen = header.ColorCorrectionGreen * values[1];
            header.ColorCorrectionBlue = header.ColorCorrectionBlue * values[2];
            header.ColorCorrectionCount = header.ColorCorrectionCount + 1;
            return;
        }
        if (variable.Name == "GAMMA")
        {
            double value = NetpbmMetadataParser.ParseDecimal(variable.Value);
            if (value <= 0)
            {
                AddVariableDiagnostic(document, "radiance.gamma.invalid", "Radiance GAMMA value was not a positive number.", variable);
                return;
            }
            header.Gamma = value;
            header.GammaCount = header.GammaCount + 1;
            return;
        }
        if (variable.Name == "SOFTWARE")
        {
            header.Software = variable.Value;
            return;
        }
        if (variable.Name == "VIEW")
        {
            if (header.View.Length > 0)
            {
                header.View = header.View + " " + variable.Value;
            }
            else
            {
                header.View = variable.Value;
            }
            return;
        }
        if (variable.Name == "PRIMARIES")
        {
            double[] values = ParseNumberList(variable.Value, 8);
            if (values.Length != 8)
            {
                AddVariableDiagnostic(document, "radiance.primaries.invalid", "Radiance PRIMARIES value did not contain eight numbers.", variable);
            }
        }
    }

    internal static bool ParseResolution(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, RadianceHeader header)
    {
        RadianceLine line = ReadLine(document, reader, options);
        if (!line.Complete)
        {
            if (!HasDiagnostic(document, "radiance.header.limit"))
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "radiance.resolution.missing", "Radiance resolution string was missing or truncated.", "Radiance Resolution", line.Offset, line.Length);
            }
            header.DataOffset = reader.Position;
            return false;
        }
        header.ResolutionOffset = line.Offset;
        header.ResolutionText = line.Text.Trim();
        header.DataOffset = reader.Position;
        string[] parts = header.ResolutionText.Split(new char[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4 || !IsAxis(parts[0]) || !IsAxis(parts[2]) || parts[0][1] == parts[2][1])
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "radiance.resolution.invalid", "Radiance resolution string did not contain two distinct signed axes and sizes.", "Radiance Resolution", line.Offset, line.Length);
            return false;
        }
        int firstSize = NetpbmMetadataParser.ParsePositiveInteger(parts[1]);
        int secondSize = NetpbmMetadataParser.ParsePositiveInteger(parts[3]);
        if (firstSize <= 0 || secondSize <= 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "radiance.resolution.size.invalid", "Radiance resolution dimensions were invalid.", "Radiance Resolution", line.Offset, line.Length);
            return false;
        }
        header.FirstAxis = parts[0];
        header.SecondAxis = parts[2];
        if (parts[0][1] == 'X')
        {
            header.Width = firstSize;
            header.Height = secondSize;
        }
        else
        {
            header.Height = firstSize;
            header.Width = secondSize;
        }
        header.Orientation = Orientation(parts[0], parts[2]);
        return true;
    }

    internal static void ValidateFormat(ImageMetadataDocument document, RadianceHeader header)
    {
        if (header.FormatCount == 0)
        {
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "radiance.format.missing", "Radiance information header did not contain a FORMAT assignment.");
            return;
        }
        if (header.Format != RgbeFormat && header.Format != XyzeFormat)
        {
            MetadataDiagnostic.AddUnsupported(document.Diagnostics, "radiance.format.unsupported", "Radiance FORMAT was not a supported RGBE or XYZE picture encoding.", "Radiance Header");
        }
    }

    internal static void ValidateRaster(ImageMetadataDocument document, BoundedStreamReader reader, RadianceHeader header)
    {
        if (header.DataOffset >= 0 && header.DataOffset >= reader.AccessibleLength())
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "radiance.raster.empty", "Radiance picture contained no scanline records.", "Radiance Raster", header.DataOffset, 0);
        }
    }

    internal static void AddHeaderDirectory(ImageMetadataDocument document, RadianceHeader header)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Format, "Header", "Radiance HDR Header");
        directory.Path = "Radiance/Header";
        directory.Offset = 0;
        directory.Length = header.DataOffset;
        AddTag(directory, "Signature", "Signature", MetadataValue.FromString(header.Signature), 0, header.Signature.Length, header.Signature);
        foreach (RadianceHeaderVariable variable in header.Variables)
        {
            string key = VariableKey(variable.Name);
            AddTag(directory, key, VariableName(variable.Name), MetadataValue.FromString(variable.Value), variable.Offset, variable.Length, variable.Value);
        }
        if (header.ExposureCount > 0)
        {
            AddTag(directory, "EffectiveExposure", "Effective Exposure", MetadataValue.FromDouble(header.Exposure), 0, header.DataOffset, "cumulative multiplier");
        }
        if (header.PixelAspectCount > 0)
        {
            AddTag(directory, "EffectivePixelAspect", "Effective Pixel Aspect Ratio", MetadataValue.FromDouble(header.PixelAspect), 0, header.DataOffset, "pixel height / width");
        }
        if (header.ColorCorrectionCount > 0)
        {
            AddTag(directory, "EffectiveColorCorrectionRed", "Effective Red Color Correction", MetadataValue.FromDouble(header.ColorCorrectionRed), 0, header.DataOffset, "cumulative multiplier");
            AddTag(directory, "EffectiveColorCorrectionGreen", "Effective Green Color Correction", MetadataValue.FromDouble(header.ColorCorrectionGreen), 0, header.DataOffset, "cumulative multiplier");
            AddTag(directory, "EffectiveColorCorrectionBlue", "Effective Blue Color Correction", MetadataValue.FromDouble(header.ColorCorrectionBlue), 0, header.DataOffset, "cumulative multiplier");
        }
        if (header.GammaCount > 0)
        {
            AddTag(directory, "EffectiveGamma", "Effective Gamma", MetadataValue.FromDouble(header.Gamma), 0, header.DataOffset, "last assignment");
        }
        if (header.View.Length > 0)
        {
            AddTag(directory, "EffectiveView", "Effective View", MetadataValue.FromString(header.View), 0, header.DataOffset, header.View);
        }
        AddListTag(directory, "Comments", "Comments", header.Comments, 0, header.DataOffset);
        AddListTag(directory, "Commands", "Commands", header.Commands, 0, header.DataOffset);
        if (header.ResolutionOffset >= 0)
        {
            long resolutionLength = header.DataOffset - header.ResolutionOffset;
            AddTag(directory, "Resolution", "Resolution String", MetadataValue.FromString(header.ResolutionText), header.ResolutionOffset, resolutionLength, header.ResolutionText);
            AddTag(directory, "Width", "Width", MetadataValue.FromInteger(header.Width), header.ResolutionOffset, resolutionLength, "pixels");
            AddTag(directory, "Height", "Height", MetadataValue.FromInteger(header.Height), header.ResolutionOffset, resolutionLength, "pixels");
            AddTag(directory, "FirstAxis", "First Scan Axis", MetadataValue.FromString(header.FirstAxis), header.ResolutionOffset, resolutionLength, AxisName(header.FirstAxis));
            AddTag(directory, "SecondAxis", "Second Scan Axis", MetadataValue.FromString(header.SecondAxis), header.ResolutionOffset, resolutionLength, AxisName(header.SecondAxis));
            AddTag(directory, "Orientation", "Orientation", MetadataValue.FromInteger((int)header.Orientation), header.ResolutionOffset, resolutionLength, header.Orientation.ToString());
            AddTag(directory, "DataOffset", "Scanline Data Offset", MetadataValue.FromInteger(header.DataOffset), header.ResolutionOffset, resolutionLength, "bytes");
        }
        AddPrimariesTags(directory, header);
    }

    internal static void AddPrimariesTags(MetadataDirectory directory, RadianceHeader header)
    {
        int index = header.Variables.Count - 1;
        while (index >= 0)
        {
            RadianceHeaderVariable variable = header.Variables[index];
            if (variable.Name == "PRIMARIES")
            {
                double[] values = ParseNumberList(variable.Value, 8);
                if (values.Length == 8)
                {
                    string[] names = new string[] { "RedX", "RedY", "GreenX", "GreenY", "BlueX", "BlueY", "WhiteX", "WhiteY" };
                    int valueIndex = 0;
                    while (valueIndex < values.Length)
                    {
                        AddTag(directory, "Primaries." + names[valueIndex], "Primary " + names[valueIndex], MetadataValue.FromDouble(values[valueIndex]), variable.Offset, variable.Length, "CIE chromaticity");
                        valueIndex = valueIndex + 1;
                    }
                }
                return;
            }
            index = index - 1;
        }
    }

    internal static void AddFrame(ImageMetadataDocument document, RadianceHeader header)
    {
        ImageMetadataFrame frame = document.AddFrameWithDimensions(header.Width, header.Height);
        frame.BitDepth = 8;
        frame.ChannelCount = 3;
        frame.HasAlpha = false;
        frame.Compression = header.Format == RgbeFormat || header.Format == XyzeFormat ? ImageCompressionKind.Rle : ImageCompressionKind.Unknown;
        frame.ColorSpace = header.Format == XyzeFormat ? ImageColorSpaceKind.Xyz : ImageColorSpaceKind.Rgb;
        frame.Orientation = header.Orientation;
    }

    internal static ImageOrientation Orientation(string first, string second)
    {
        string axes = first + " " + second;
        if (axes == "-Y +X") return ImageOrientation.Normal;
        if (axes == "-Y -X") return ImageOrientation.MirrorHorizontal;
        if (axes == "+Y -X") return ImageOrientation.Rotate180;
        if (axes == "+Y +X") return ImageOrientation.MirrorVertical;
        if (axes == "+X +Y") return ImageOrientation.Rotate90;
        if (axes == "-X +Y") return ImageOrientation.MirrorHorizontalRotate90;
        if (axes == "-X -Y") return ImageOrientation.Rotate270;
        if (axes == "+X -Y") return ImageOrientation.MirrorHorizontalRotate270;
        return ImageOrientation.Unknown;
    }

    internal static string AxisName(string axis)
    {
        if (axis == "+X") return "X increasing";
        if (axis == "-X") return "X decreasing";
        if (axis == "+Y") return "Y increasing";
        if (axis == "-Y") return "Y decreasing";
        return "Unknown";
    }

    internal static bool IsAxis(string text)
    {
        return text == "+X" || text == "-X" || text == "+Y" || text == "-Y";
    }

    internal static double[] ParseNumberList(string text, int expected)
    {
        if (text == null)
        {
            return new double[0];
        }
        string[] parts = text.Split(new char[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != expected)
        {
            return new double[0];
        }
        double[] values = new double[expected];
        int index = 0;
        while (index < expected)
        {
            double value = NetpbmMetadataParser.ParseDecimal(parts[index]);
            if (value == 0 && !IsZeroText(parts[index]))
            {
                return new double[0];
            }
            values[index] = value;
            index = index + 1;
        }
        return values;
    }

    internal static bool IsZeroText(string text)
    {
        if (text == null || text.Length == 0)
        {
            return false;
        }
        int index = 0;
        if (text[index] == '+' || text[index] == '-') index = index + 1;
        bool digit = false;
        while (index < text.Length)
        {
            char value = text[index];
            if (value == '0')
            {
                digit = true;
            }
            else if (value != '.' && value != 'e' && value != 'E' && value != '+' && value != '-')
            {
                return false;
            }
            index = index + 1;
        }
        return digit;
    }

    internal static RadianceLine ReadLine(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options)
    {
        RadianceLine line = new RadianceLine();
        line.Offset = reader.Position;
        StringBuilder text = new StringBuilder();
        while (reader.Position < reader.AccessibleLength())
        {
            if (reader.Position >= MaxSegmentBytes(options))
            {
                if (!HasDiagnostic(document, "radiance.header.limit"))
                {
                    MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "radiance.header.limit", "Radiance header exceeded the configured segment limit.", "Radiance Header", 0, reader.Position);
                }
                line.Text = text.ToString();
                line.Length = reader.Position - line.Offset;
                return line;
            }
            int value = reader.ReadByte();
            if (value == 10)
            {
                line.Text = text.ToString();
                line.Length = reader.Position - line.Offset;
                line.Complete = true;
                return line;
            }
            if (value == 13)
            {
                if (reader.Position < reader.AccessibleLength())
                {
                    long position = reader.Position;
                    int next = reader.ReadByte();
                    if (next != 10) reader.SeekAbsolute(position);
                }
                line.Text = text.ToString();
                line.Length = reader.Position - line.Offset;
                line.Complete = true;
                return line;
            }
            if (value > 127)
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "radiance.header.non-ascii", "Radiance header contained a non-ASCII byte.", "Radiance Header", reader.Position - 1, 1);
                line.Text = text.ToString();
                line.Length = reader.Position - line.Offset;
                return line;
            }
            text.Append((char)value);
        }
        line.Text = text.ToString();
        line.Length = reader.Position - line.Offset;
        return line;
    }

    internal static string VariableKey(string name)
    {
        if (name == "FORMAT") return "Format";
        if (name == "EXPOSURE") return "Exposure";
        if (name == "COLORCORR") return "ColorCorrection";
        if (name == "PIXASPECT") return "PixelAspect";
        if (name == "SOFTWARE") return "Software";
        if (name == "VIEW") return "View";
        if (name == "PRIMARIES") return "Primaries";
        if (name == "GAMMA") return "Gamma";
        return "Variable." + SanitizeName(name);
    }

    internal static string VariableName(string name)
    {
        if (name == "FORMAT") return "Format";
        if (name == "EXPOSURE") return "Exposure";
        if (name == "COLORCORR") return "Color Correction";
        if (name == "PIXASPECT") return "Pixel Aspect Ratio";
        if (name == "SOFTWARE") return "Software";
        if (name == "VIEW") return "View";
        if (name == "PRIMARIES") return "Color Primaries";
        if (name == "GAMMA") return "Gamma";
        return name;
    }

    internal static string SanitizeName(string name)
    {
        StringBuilder value = new StringBuilder();
        int index = 0;
        while (index < name.Length)
        {
            char character = name[index];
            if ((character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z') || (character >= '0' && character <= '9') || character == '_' || character == '-')
            {
                value.Append(character);
            }
            else
            {
                value.Append('_');
            }
            index = index + 1;
        }
        return value.ToString();
    }

    internal static MetadataTag AddTag(MetadataDirectory directory, string keyName, string name, MetadataValue value, long offset, long length, string display)
    {
        MetadataTag tag = directory.AddTagWithValue("Radiance." + keyName, name, value);
        tag.Offset = offset;
        tag.Length = length;
        tag.DisplayValue = display;
        tag.Confidence = 100;
        return tag;
    }

    internal static void AddListTag(MetadataDirectory directory, string key, string name, List<string> values, long offset, long length)
    {
        if (values.Count == 0)
        {
            return;
        }
        MetadataValue value = new MetadataValue();
        value.Kind = MetadataValueKind.StringList;
        foreach (string item in values)
        {
            value.AddTextValue(item);
        }
        AddTag(directory, key, name, value, offset, length, values.Count.ToString() + " entries");
    }

    internal static void CaptureHeader(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, long length)
    {
        if (!CaptureRawBlocks(options) || length < 0)
        {
            return;
        }
        MetadataRawBlock block = document.AddRawBlockWithRange(MetadataFamily.Format, "Header", "Radiance HDR Header", 0, length);
        if (!CaptureRawBlockData(options))
        {
            return;
        }
        if (length > MaxRawBlockBytes(options) || length > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "radiance.raw.block.limit", "Radiance header exceeded the configured raw block limit.", "Header", 0, length);
            return;
        }
        block.Data = ReadRange(reader, 0, (int)length);
    }

    internal static byte[] ReadRange(BoundedStreamReader reader, long offset, int length)
    {
        if (reader == null || length <= 0 || offset < 0 || offset > reader.AccessibleLength() || length > reader.AccessibleLength() - offset)
        {
            return new byte[0];
        }
        if (!reader.SeekAbsolute(offset))
        {
            return new byte[0];
        }
        return reader.ReadBytes(length);
    }

    internal static void AddVariableDiagnostic(ImageMetadataDocument document, string code, string message, RadianceHeaderVariable variable)
    {
        MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, code, message, "Radiance Header", variable.Offset, variable.Length);
    }

    internal static bool HasDiagnostic(ImageMetadataDocument document, string code)
    {
        foreach (MetadataDiagnostic diagnostic in document.Diagnostics)
        {
            if (diagnostic.Code == code) return true;
        }
        return false;
    }

    internal static long MaxSegmentBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxSegmentBytes : options.MaxSegmentBytes;
    }

    internal static long MaxRawBlockBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxRawBlockBytes : options.MaxRawBlockBytes;
    }

    internal static int MaxTagCount(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxTagCount : options.MaxTagCount;
    }

    internal static bool CaptureRawBlocks(ImageMetadataReaderOptions options)
    {
        return options == null || options.CaptureRawBlocks;
    }

    internal static bool CaptureRawBlockData(ImageMetadataReaderOptions options)
    {
        return options != null && options.CaptureRawBlockData;
    }

    internal static ImageMetadataDocument Finish(ImageMetadataDocument document, BoundedStreamReader reader)
    {
        if (reader != null && reader.Diagnostics != null)
        {
            foreach (MetadataDiagnostic diagnostic in reader.Diagnostics)
            {
                document.AddDiagnostic(diagnostic);
            }
            reader.SeekAbsolute(0);
        }
        return document;
    }
}
