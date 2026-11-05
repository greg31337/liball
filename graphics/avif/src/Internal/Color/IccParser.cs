using System.Collections.Generic;

namespace avif;

internal static class IccParser
{
    internal static IccParseResult Parse(
        byte[] data,
        AvifSecurityLimits? limits)
    {
        IccParseResult result = new IccParseResult(
            AvifResultCode.InvalidMetadata);
        if (data == null || limits == null)
        {
            result.Code = AvifResultCode.InvalidArgument;
            return result;
        }
        if (data.LongLength > limits.MaxIccBytes
            || data.LongLength > limits.MaxMetadataBytes)
        {
            result.Code = AvifResultCode.LimitExceeded;
            return result;
        }
        if (data.Length < 132)
        {
            return result;
        }

        uint declaredSize = ReadUInt32(data, 0);
        int versionMajor = data[8];
        int versionMinor = data[9] >> 4;
        string deviceClass = ReadSignature(data, 12);
        string colorSpace = ReadSignature(data, 16);
        string pcs = ReadSignature(data, 20);
        uint intent = ReadUInt32(data, 64);
        if (declaredSize != data.Length
            || ReadSignature(data, 36) != "acsp"
            || (versionMajor != 2 && versionMajor != 4)
            || intent > 3
            || (pcs != "XYZ " && pcs != "Lab "))
        {
            return result;
        }
        if (!IsValidDeviceClass(deviceClass)
            || GetChannelCount(colorSpace) == 0)
        {
            return result;
        }

        uint tagCountValue = ReadUInt32(data, 128);
        if (tagCountValue > int.MaxValue)
        {
            return result;
        }
        int tagCount = (int)tagCountValue;
        long tableEndValue = 132L + (long)tagCount * 12;
        if (tableEndValue > data.Length)
        {
            return result;
        }
        long temporaryEstimate = (long)data.Length * 8
            + (long)tagCount * 64;
        if (temporaryEstimate > limits.MaxTemporaryBytes)
        {
            result.Code = AvifResultCode.LimitExceeded;
            return result;
        }
        int tableEnd = (int)tableEndValue;
        List<IccTag> tags = new List<IccTag>();
        int index = 0;
        while (index < tagCount)
        {
            int entry = 132 + index * 12;
            string signature = ReadSignature(data, entry);
            uint offsetValue = ReadUInt32(data, entry + 4);
            uint sizeValue = ReadUInt32(data, entry + 8);
            if (offsetValue > int.MaxValue || sizeValue > int.MaxValue)
            {
                return result;
            }
            int offset = (int)offsetValue;
            int size = (int)sizeValue;
            if ((offset & 3) != 0 || offset < tableEnd || size < 8
                || offset > data.Length || size > data.Length - offset)
            {
                return result;
            }
            if (FindTag(tags, signature) != null)
            {
                return result;
            }
            string type = ReadSignature(data, offset);
            if (ReadUInt32(data, offset + 4) != 0)
            {
                return result;
            }
            tags.Add(new IccTag(signature, offset, size, type));
            index++;
        }
        if (!ValidateOverlaps(tags))
        {
            return result;
        }

        IccParsedProfile profile = new IccParsedProfile();
        profile.Data = (byte[])data.Clone();
        profile.VersionMajor = versionMajor;
        profile.VersionMinor = versionMinor;
        profile.DeviceClass = deviceClass;
        profile.ColorSpace = colorSpace;
        profile.Pcs = pcs;
        profile.Intent = (AvifRenderingIntent)intent;
        profile.Tags = tags;
        AvifResultCode semanticCode = ParseTransformTags(profile);
        if (semanticCode != AvifResultCode.Success)
        {
            result.Code = semanticCode;
            return result;
        }
        result.Code = AvifResultCode.Success;
        result.Profile = profile;
        return result;
    }

    static AvifResultCode ParseTransformTags(IccParsedProfile profile)
    {
        IccTag? whiteTag = FindTag(profile.Tags, "wtpt");
        if (whiteTag != null)
        {
            profile.MediaWhite = ParseXyz(profile.Data, whiteTag);
            if (profile.MediaWhite == null)
            {
                return AvifResultCode.InvalidMetadata;
            }
        }
        IccTag? adaptationTag = FindTag(profile.Tags, "chad");
        if (adaptationTag != null)
        {
            profile.ChromaticAdaptation = ParseS15Array(
                profile.Data, adaptationTag, 9);
            if (profile.ChromaticAdaptation == null)
            {
                return AvifResultCode.InvalidMetadata;
            }
        }

        string[] toPcs = new string[] { "A2B0", "A2B1", "A2B2" };
        string[] fromPcs = new string[] { "B2A0", "B2A1", "B2A2" };
        int intent = 0;
        while (intent < 3)
        {
            IccTag? tag = FindTag(profile.Tags, toPcs[intent]);
            if (tag != null)
            {
                profile.AToB[intent] = ParsePipeline(
                    profile.Data, tag);
                if (profile.AToB[intent] == null)
                {
                    return AvifResultCode.InvalidMetadata;
                }
            }
            tag = FindTag(profile.Tags, fromPcs[intent]);
            if (tag != null)
            {
                profile.BToA[intent] = ParsePipeline(
                    profile.Data, tag);
                if (profile.BToA[intent] == null)
                {
                    return AvifResultCode.InvalidMetadata;
                }
            }
            intent++;
        }
        return ParseMatrixCurves(profile);
    }

    static AvifResultCode ParseMatrixCurves(IccParsedProfile profile)
    {
        if (profile.ColorSpace == "RGB ")
        {
            string[] xyzNames = new string[] { "rXYZ", "gXYZ", "bXYZ" };
            string[] curveNames = new string[] { "rTRC", "gTRC", "bTRC" };
            double[] matrix = new double[9];
            IccCurve[] curves = new IccCurve[3];
            int index = 0;
            int present = 0;
            while (index < 3)
            {
                IccTag? xyzTag = FindTag(profile.Tags, xyzNames[index]);
                IccTag? curveTag = FindTag(profile.Tags, curveNames[index]);
                if (xyzTag != null || curveTag != null)
                {
                    present++;
                }
                if (xyzTag != null && curveTag != null)
                {
                    double[]? xyz = ParseXyz(profile.Data, xyzTag);
                    IccCurveParseResult curve = ParseCurve(
                        profile.Data, curveTag.Offset, curveTag.Size);
                    IccCurve? parsedCurve = curve.Curve;
                    if (xyz == null
                        || curve.Code != AvifResultCode.Success
                        || parsedCurve == null)
                    {
                        return AvifResultCode.InvalidMetadata;
                    }
                    matrix[index] = xyz[0];
                    matrix[3 + index] = xyz[1];
                    matrix[6 + index] = xyz[2];
                    curves[index] = parsedCurve;
                }
                index++;
            }
            if (present != 0)
            {
                index = 0;
                while (index < 3)
                {
                    if (curves[index] == null)
                    {
                        return AvifResultCode.InvalidMetadata;
                    }
                    index++;
                }
                profile.Matrix = matrix;
                profile.MatrixCurves = curves;
            }
        }
        else if (profile.ColorSpace == "GRAY")
        {
            IccTag? gray = FindTag(profile.Tags, "kTRC");
            if (gray != null)
            {
                IccCurveParseResult curve = ParseCurve(
                    profile.Data, gray.Offset, gray.Size);
                IccCurve? parsedCurve = curve.Curve;
                if (curve.Code != AvifResultCode.Success
                    || parsedCurve == null)
                {
                    return curve.Code;
                }
                profile.MatrixCurves = new IccCurve[] { parsedCurve };
            }
        }
        return AvifResultCode.Success;
    }

    static IccPipeline? ParsePipeline(byte[] data, IccTag tag)
    {
        if (tag.Type == "mft1" || tag.Type == "mft2")
        {
            return ParseLegacyLut(data, tag);
        }
        if (tag.Type == "mAB " || tag.Type == "mBA ")
        {
            return ParseMultiProcessLut(data, tag);
        }
        return null;
    }

    static IccPipeline? ParseLegacyLut(byte[] data, IccTag tag)
    {
        int start = tag.Offset;
        int end = start + tag.Size;
        if (tag.Size < 48)
        {
            return null;
        }
        int inputChannels = data[start + 8];
        int outputChannels = data[start + 9];
        int grid = data[start + 10];
        if (inputChannels < 1 || inputChannels > 15
            || outputChannels < 1 || outputChannels > 15
            || grid < 2)
        {
            return null;
        }
        double[] matrix = new double[12];
        int matrixIndex = 0;
        while (matrixIndex < 9)
        {
            matrix[matrixIndex] = ReadS15Fixed16(
                data, start + 12 + matrixIndex * 4);
            matrixIndex++;
        }
        int inputEntries = tag.Type == "mft1"
            ? 256 : ReadUInt16(data, start + 48);
        int outputEntries = tag.Type == "mft1"
            ? 256 : ReadUInt16(data, start + 50);
        int bytes = tag.Type == "mft1" ? 1 : 2;
        int position = start + (tag.Type == "mft1" ? 48 : 52);
        if (inputEntries < 2 || outputEntries < 2)
        {
            return null;
        }
        long inputTableBytes = (long)inputChannels * inputEntries * bytes;
        long clutEntries = Power(grid, inputChannels);
        long clutBytes = clutEntries * outputChannels * bytes;
        long outputTableBytes = (long)outputChannels * outputEntries * bytes;
        long required = (long)position + inputTableBytes
            + clutBytes + outputTableBytes;
        if (clutEntries <= 0 || required > end)
        {
            return null;
        }

        IccPipeline pipeline = new IccPipeline(IccPipelineKind.Lut);
        pipeline.InputChannels = inputChannels;
        pipeline.OutputChannels = outputChannels;
        pipeline.Matrix = inputChannels == 3 ? matrix : null;
        pipeline.InputCurves = ReadTableCurves(
            data, position, inputChannels, inputEntries, bytes);
        position += (int)inputTableBytes;
        int[] grids = new int[inputChannels];
        int gridIndex = 0;
        while (gridIndex < grids.Length)
        {
            grids[gridIndex] = grid;
            gridIndex++;
        }
        pipeline.Clut = ReadClut(
            data,
            position,
            inputChannels,
            outputChannels,
            grids,
            (int)clutEntries,
            bytes);
        position += (int)clutBytes;
        pipeline.OutputCurves = ReadTableCurves(
            data, position, outputChannels, outputEntries, bytes);
        return pipeline;
    }

    static IccPipeline? ParseMultiProcessLut(byte[] data, IccTag tag)
    {
        int start = tag.Offset;
        if (tag.Size < 32)
        {
            return null;
        }
        int inputChannels = data[start + 8];
        int outputChannels = data[start + 9];
        if (inputChannels < 1 || inputChannels > 15
            || outputChannels < 1 || outputChannels > 15)
        {
            return null;
        }
        int bOffset = GetRelativeOffset(data, start, tag.Size, 12);
        int matrixOffset = GetRelativeOffset(data, start, tag.Size, 16);
        int middleOffset = GetRelativeOffset(data, start, tag.Size, 20);
        int clutOffset = GetRelativeOffset(data, start, tag.Size, 24);
        int aOffset = GetRelativeOffset(data, start, tag.Size, 28);
        if (bOffset < 0 || matrixOffset < 0 || middleOffset < 0
            || clutOffset < 0 || aOffset < 0 || bOffset == 0)
        {
            return null;
        }
        if (!OffsetsAreDistinct(
                bOffset, matrixOffset, middleOffset, clutOffset, aOffset)
            || (matrixOffset == 0) != (middleOffset == 0)
            || (clutOffset == 0) != (aOffset == 0))
        {
            return null;
        }

        bool aToB = tag.Type == "mAB ";
        IccPipeline pipeline = new IccPipeline(
            aToB ? IccPipelineKind.AToB : IccPipelineKind.BToA);
        pipeline.InputChannels = inputChannels;
        pipeline.OutputChannels = outputChannels;
        int inputCurveOffset = aToB ? aOffset : bOffset;
        int outputCurveOffset = aToB ? bOffset : aOffset;
        if (inputCurveOffset != 0)
        {
            pipeline.InputCurves = ParseCurveSet(
                data,
                start + inputCurveOffset,
                start + tag.Size,
                inputChannels);
            if (pipeline.InputCurves == null)
            {
                return null;
            }
        }
        if (outputCurveOffset != 0)
        {
            pipeline.OutputCurves = ParseCurveSet(
                data,
                start + outputCurveOffset,
                start + tag.Size,
                outputChannels);
            if (pipeline.OutputCurves == null)
            {
                return null;
            }
        }
        if (middleOffset != 0)
        {
            int middleCount = aToB ? outputChannels : inputChannels;
            pipeline.MiddleCurves = ParseCurveSet(
                data,
                start + middleOffset,
                start + tag.Size,
                middleCount);
            if (pipeline.MiddleCurves == null)
            {
                return null;
            }
        }
        if (matrixOffset != 0)
        {
            if (matrixOffset > tag.Size - 48)
            {
                return null;
            }
            pipeline.Matrix = new double[12];
            int index = 0;
            while (index < 12)
            {
                pipeline.Matrix[index] = ReadS15Fixed16(
                    data, start + matrixOffset + index * 4);
                index++;
            }
        }
        if (clutOffset != 0)
        {
            pipeline.Clut = ParseMultiProcessClut(
                data,
                start + clutOffset,
                start + tag.Size,
                inputChannels,
                outputChannels);
            if (pipeline.Clut == null)
            {
                return null;
            }
        }
        return pipeline;
    }

    static IccClut? ParseMultiProcessClut(
        byte[] data,
        int offset,
        int end,
        int inputChannels,
        int outputChannels)
    {
        if (offset > end - 20)
        {
            return null;
        }
        int[] grids = new int[inputChannels];
        long entryCount = 1;
        int index = 0;
        while (index < inputChannels)
        {
            grids[index] = data[offset + index];
            if (grids[index] < 2
                || entryCount > int.MaxValue / grids[index])
            {
                return null;
            }
            entryCount *= grids[index];
            index++;
        }
        int precision = data[offset + 16];
        if ((precision != 1 && precision != 2)
            || data[offset + 17] != 0
            || data[offset + 18] != 0
            || data[offset + 19] != 0)
        {
            return null;
        }
        long byteCount = entryCount * outputChannels * precision;
        if (byteCount > end - offset - 20)
        {
            return null;
        }
        return ReadClut(
            data,
            offset + 20,
            inputChannels,
            outputChannels,
            grids,
            (int)entryCount,
            precision);
    }

    internal static IccCurveParseResult ParseCurve(
        byte[] data,
        int offset,
        int available)
    {
        IccCurveParseResult result = new IccCurveParseResult(
            AvifResultCode.InvalidMetadata);
        if (data == null || offset < 0 || available < 12
            || offset > data.Length - available)
        {
            return result;
        }
        string type = ReadSignature(data, offset);
        if (ReadUInt32(data, offset + 4) != 0)
        {
            return result;
        }
        if (type == "curv")
        {
            uint countValue = ReadUInt32(data, offset + 8);
            if (countValue > int.MaxValue)
            {
                return result;
            }
            int count = (int)countValue;
            long size = 12L + count * 2L;
            if (size > available)
            {
                return result;
            }
            result.Size = (int)size;
            if (count == 0)
            {
                result.Curve = new IccCurve(-1, System.Array.Empty<double>());
            }
            else if (count == 1)
            {
                double gamma = ReadUInt16(data, offset + 12) / 256.0;
                if (gamma <= 0)
                {
                    return result;
                }
                result.Curve = new IccCurve(-2, new double[] { gamma });
            }
            else
            {
                double[] values = new double[count];
                int index = 0;
                while (index < count)
                {
                    values[index] = ReadUInt16(
                        data, offset + 12 + index * 2) / 65535.0;
                    if (index != 0 && values[index] < values[index - 1])
                    {
                        return result;
                    }
                    index++;
                }
                result.Curve = new IccCurve(-2, values);
            }
            result.Code = AvifResultCode.Success;
            return result;
        }
        if (type != "para" || available < 16)
        {
            return result;
        }
        int function = ReadUInt16(data, offset + 8);
        if (ReadUInt16(data, offset + 10) != 0
            || function < 0 || function > 4)
        {
            return result;
        }
        int[] parameterCounts = new int[] { 1, 3, 4, 5, 7 };
        int parameterCount = parameterCounts[function];
        int parameterSize = 12 + parameterCount * 4;
        if (parameterSize > available)
        {
            return result;
        }
        double[] parameters = new double[parameterCount];
        int parameter = 0;
        while (parameter < parameterCount)
        {
            parameters[parameter] = ReadS15Fixed16(
                data, offset + 12 + parameter * 4);
            parameter++;
        }
        if (parameters[0] <= 0
            || (function >= 1 && parameters[1] == 0))
        {
            return result;
        }
        result.Code = AvifResultCode.Success;
        result.Curve = new IccCurve(function, parameters);
        result.Size = parameterSize;
        return result;
    }

    static IccCurve[]? ParseCurveSet(
        byte[] data,
        int offset,
        int end,
        int count)
    {
        IccCurve[] curves = new IccCurve[count];
        int index = 0;
        int position = offset;
        while (index < count)
        {
            IccCurveParseResult curve = ParseCurve(
                data, position, end - position);
            IccCurve? parsedCurve = curve.Curve;
            if (curve.Code != AvifResultCode.Success || parsedCurve == null)
            {
                return null;
            }
            curves[index] = parsedCurve;
            position += AlignFour(curve.Size);
            if (position > end)
            {
                return null;
            }
            index++;
        }
        return curves;
    }

    static IccCurve[] ReadTableCurves(
        byte[] data,
        int offset,
        int channelCount,
        int entryCount,
        int bytes)
    {
        IccCurve[] curves = new IccCurve[channelCount];
        int channel = 0;
        while (channel < channelCount)
        {
            double[] values = new double[entryCount];
            int entry = 0;
            while (entry < entryCount)
            {
                int position = offset
                    + (channel * entryCount + entry) * bytes;
                values[entry] = bytes == 1
                    ? data[position] / 255.0
                    : ReadUInt16(data, position) / 65535.0;
                entry++;
            }
            curves[channel] = new IccCurve(-2, values);
            channel++;
        }
        return curves;
    }

    static IccClut ReadClut(
        byte[] data,
        int offset,
        int inputChannels,
        int outputChannels,
        int[] grids,
        int entryCount,
        int bytes)
    {
        double[] values = new double[entryCount * outputChannels];
        int index = 0;
        while (index < values.Length)
        {
            int position = offset + index * bytes;
            values[index] = bytes == 1
                ? data[position] / 255.0
                : ReadUInt16(data, position) / 65535.0;
            index++;
        }
        return new IccClut(
            inputChannels, outputChannels, grids, values);
    }

    static double[]? ParseXyz(byte[] data, IccTag tag)
    {
        if (tag.Type != "XYZ " || tag.Size < 20)
        {
            return null;
        }
        return new double[]
        {
            ReadS15Fixed16(data, tag.Offset + 8),
            ReadS15Fixed16(data, tag.Offset + 12),
            ReadS15Fixed16(data, tag.Offset + 16)
        };
    }

    static double[]? ParseS15Array(
        byte[] data,
        IccTag tag,
        int count)
    {
        if (tag.Type != "sf32" || tag.Size < 8 + count * 4)
        {
            return null;
        }
        double[] values = new double[count];
        int index = 0;
        while (index < count)
        {
            values[index] = ReadS15Fixed16(
                data, tag.Offset + 8 + index * 4);
            index++;
        }
        return values;
    }

    static int GetRelativeOffset(
        byte[] data,
        int start,
        int size,
        int field)
    {
        uint value = ReadUInt32(data, start + field);
        if (value == 0)
        {
            return 0;
        }
        if (value > int.MaxValue || value < 32
            || (value & 3) != 0 || value > size - 8)
        {
            return -1;
        }
        return (int)value;
    }

    static bool ValidateOverlaps(List<IccTag> tags)
    {
        int first = 0;
        while (first < tags.Count)
        {
            int second = first + 1;
            while (second < tags.Count)
            {
                IccTag a = tags[first];
                IccTag b = tags[second];
                bool overlap = a.Offset < b.Offset + b.Size
                    && b.Offset < a.Offset + a.Size;
                bool identical = a.Offset == b.Offset && a.Size == b.Size;
                if (overlap && !identical)
                {
                    return false;
                }
                second++;
            }
            first++;
        }
        return true;
    }

    static bool OffsetsAreDistinct(
        int first,
        int second,
        int third,
        int fourth,
        int fifth)
    {
        int[] values = new int[]
        {
            first, second, third, fourth, fifth
        };
        int outer = 0;
        while (outer < values.Length)
        {
            if (values[outer] != 0)
            {
                int inner = outer + 1;
                while (inner < values.Length)
                {
                    if (values[outer] == values[inner])
                    {
                        return false;
                    }
                    inner++;
                }
            }
            outer++;
        }
        return true;
    }

    internal static int GetChannelCount(string signature)
    {
        if (signature == "GRAY")
        {
            return 1;
        }
        if (signature == "RGB " || signature == "XYZ "
            || signature == "Lab " || signature == "YCbr"
            || signature == "Yxy " || signature == "HSV "
            || signature == "HLS ")
        {
            return 3;
        }
        if (signature == "CMYK")
        {
            return 4;
        }
        if (signature.Length == 4 && signature[1] == 'C'
            && signature[2] == 'L' && signature[3] == 'R')
        {
            char digit = signature[0];
            if (digit >= '2' && digit <= '9')
            {
                return digit - '0';
            }
            if (digit >= 'A' && digit <= 'F')
            {
                return 10 + digit - 'A';
            }
        }
        return 0;
    }

    static bool IsValidDeviceClass(string value)
    {
        return value == "scnr" || value == "mntr"
            || value == "prtr" || value == "link"
            || value == "spac" || value == "abst"
            || value == "nmcl";
    }

    static IccTag? FindTag(List<IccTag> tags, string signature)
    {
        int index = 0;
        while (index < tags.Count)
        {
            if (tags[index].Signature == signature)
            {
                return tags[index];
            }
            index++;
        }
        return null;
    }

    static long Power(int value, int exponent)
    {
        long result = 1;
        int index = 0;
        while (index < exponent)
        {
            if (result > int.MaxValue / value)
            {
                return -1;
            }
            result *= value;
            index++;
        }
        return result;
    }

    static int AlignFour(int value)
    {
        return (value + 3) & -4;
    }

    internal static string ReadSignature(byte[] data, int offset)
    {
        if (offset < 0 || offset > data.Length - 4)
        {
            return string.Empty;
        }
        return new string(new char[]
        {
            (char)data[offset],
            (char)data[offset + 1],
            (char)data[offset + 2],
            (char)data[offset + 3]
        });
    }

    internal static ushort ReadUInt16(byte[] data, int offset)
    {
        return (ushort)(data[offset] << 8 | data[offset + 1]);
    }

    internal static uint ReadUInt32(byte[] data, int offset)
    {
        return (uint)data[offset] << 24
            | (uint)data[offset + 1] << 16
            | (uint)data[offset + 2] << 8
            | data[offset + 3];
    }

    internal static double ReadS15Fixed16(byte[] data, int offset)
    {
        return IntegerConversion.ToInt32(ReadUInt32(data, offset))
            / 65536.0;
    }
}
