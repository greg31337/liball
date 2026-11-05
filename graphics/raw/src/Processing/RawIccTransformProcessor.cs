using System;

namespace raw;

internal class RawIccTransformDefinition
{
    internal RawIccLut? Lut;
    internal RawIccMultiProcess? MultiProcess;
    internal double[]? Matrix;

    internal RawIccTransformDefinition(RawIccLut? lut, RawIccMultiProcess? multiProcess, double[]? matrix)
    {
        Lut = lut;
        MultiProcess = multiProcess;
        Matrix = matrix;
    }
}

internal class RawIccCurveElement
{
    internal RawIccToneCurve Curve;
    internal int NextOffset;

    internal RawIccCurveElement(RawIccToneCurve curve, int nextOffset)
    {
        Curve = curve;
        NextOffset = nextOffset;
    }
}

internal class RawIccMultiProcess
{
    internal bool ToPcs;
    internal RawIccToneCurve[]? ACurves;
    internal RawIccToneCurve[]? BCurves;
    internal RawIccToneCurve[]? MCurves;
    internal RawIccLut? Clut;
    internal double[]? Matrix;
    internal double[]? Offset;

    internal RawIccMultiProcess(bool toPcs, RawIccToneCurve[]? aCurves, RawIccToneCurve[]? bCurves, RawIccToneCurve[]? mCurves, RawIccLut? clut, double[]? matrix, double[]? offset)
    {
        ToPcs = toPcs;
        ACurves = aCurves;
        BCurves = bCurves;
        MCurves = mCurves;
        Clut = clut;
        Matrix = matrix;
        Offset = offset;
    }

    internal double[] Transform(double first, double second, double third)
    {
        double[] values = new double[] { first, second, third };
        if (ToPcs)
        {
            values = Curves(ACurves, values);
            if (Clut != null)
            {
                values = Clut.Tetrahedral(values[0], values[1], values[2]);
            }
            values = Curves(MCurves, values);
            values = MatrixAndOffset(values);
            values = Curves(BCurves, values);
        }
        else
        {
            values = Curves(BCurves, values);
            values = MatrixAndOffset(values);
            values = Curves(MCurves, values);
            if (Clut != null)
            {
                values = Clut.Tetrahedral(values[0], values[1], values[2]);
            }
            values = Curves(ACurves, values);
        }
        return values;
    }

    internal double[] Curves(RawIccToneCurve[]? curves, double[] values)
    {
        if (curves == null)
        {
            return values;
        }
        return new double[]
        {
            RawIccTransformProcessor.EvaluateCurve(curves[0], values[0]),
            RawIccTransformProcessor.EvaluateCurve(curves[1], values[1]),
            RawIccTransformProcessor.EvaluateCurve(curves[2], values[2])
        };
    }

    internal double[] MatrixAndOffset(double[] values)
    {
        if (Matrix == null)
        {
            return values;
        }
        double[] transformed = RawMatrixMath.MultiplyVector(Matrix, values);
        transformed[0] += (Offset ?? throw new System.InvalidOperationException("A required value is unavailable."))[0];
        transformed[1] += Offset[1];
        transformed[2] += Offset[2];
        return transformed;
    }
}

internal class RawIccLut
{
    internal byte[] Bytes;
    internal int PrecisionBytes;
    internal int GridPoints;
    internal int InputEntries;
    internal int OutputEntries;
    internal int InputOffset;
    internal int ClutOffset;
    internal int OutputOffset;
    internal double[] Matrix;

    internal RawIccLut(byte[] bytes, int precisionBytes, int gridPoints, int inputEntries, int outputEntries, int inputOffset, int clutOffset, int outputOffset, double[] matrix)
    {
        Bytes = bytes;
        PrecisionBytes = precisionBytes;
        GridPoints = gridPoints;
        InputEntries = inputEntries;
        OutputEntries = outputEntries;
        InputOffset = inputOffset;
        ClutOffset = clutOffset;
        OutputOffset = outputOffset;
        Matrix = matrix;
    }

    internal double[] Transform(double first, double second, double third, bool applyMatrix)
    {
        double[] values = new double[3];
        values[0] = Table(InputOffset, InputEntries, 0, first);
        values[1] = Table(InputOffset, InputEntries, 1, second);
        values[2] = Table(InputOffset, InputEntries, 2, third);
        if (applyMatrix)
        {
            values = RawMatrixMath.MultiplyVector(Matrix, values);
        }
        double[] clut = Tetrahedral(values[0], values[1], values[2]);
        clut[0] = Table(OutputOffset, OutputEntries, 0, clut[0]);
        clut[1] = Table(OutputOffset, OutputEntries, 1, clut[1]);
        clut[2] = Table(OutputOffset, OutputEntries, 2, clut[2]);
        return clut;
    }

    internal double Table(int offset, int entries, int channel, double value)
    {
        double bounded = RawIccTransformProcessor.Clamp(value);
        double position = bounded * (entries - 1);
        int lower = (int)System.Math.Floor(position);
        if (lower >= entries - 1)
        {
            return Element(offset, channel * entries + entries - 1);
        }
        double fraction = position - lower;
        double first = Element(offset, channel * entries + lower);
        double second = Element(offset, channel * entries + lower + 1);
        return first + (second - first) * fraction;
    }

    internal double[] Tetrahedral(double first, double second, double third)
    {
        double firstPosition = RawIccTransformProcessor.Clamp(first) * (GridPoints - 1);
        double secondPosition = RawIccTransformProcessor.Clamp(second) * (GridPoints - 1);
        double thirdPosition = RawIccTransformProcessor.Clamp(third) * (GridPoints - 1);
        int firstLower = (int)System.Math.Floor(firstPosition);
        int secondLower = (int)System.Math.Floor(secondPosition);
        int thirdLower = (int)System.Math.Floor(thirdPosition);
        if (firstLower >= GridPoints - 1)
        {
            firstLower = GridPoints - 2;
            firstPosition = GridPoints - 1;
        }
        if (secondLower >= GridPoints - 1)
        {
            secondLower = GridPoints - 2;
            secondPosition = GridPoints - 1;
        }
        if (thirdLower >= GridPoints - 1)
        {
            thirdLower = GridPoints - 2;
            thirdPosition = GridPoints - 1;
        }
        double firstFraction = firstPosition - firstLower;
        double secondFraction = secondPosition - secondLower;
        double thirdFraction = thirdPosition - thirdLower;
        double[] output = new double[3];
        int channel = 0;
        while (channel < 3)
        {
            double c000 = Clut(firstLower, secondLower, thirdLower, channel);
            double c100 = Clut(firstLower + 1, secondLower, thirdLower, channel);
            double c010 = Clut(firstLower, secondLower + 1, thirdLower, channel);
            double c001 = Clut(firstLower, secondLower, thirdLower + 1, channel);
            double c110 = Clut(firstLower + 1, secondLower + 1, thirdLower, channel);
            double c101 = Clut(firstLower + 1, secondLower, thirdLower + 1, channel);
            double c011 = Clut(firstLower, secondLower + 1, thirdLower + 1, channel);
            double c111 = Clut(firstLower + 1, secondLower + 1, thirdLower + 1, channel);
            double result;
            if (firstFraction >= secondFraction)
            {
                if (secondFraction >= thirdFraction)
                {
                    result = c000 + firstFraction * (c100 - c000) + secondFraction * (c110 - c100) + thirdFraction * (c111 - c110);
                }
                else if (firstFraction >= thirdFraction)
                {
                    result = c000 + firstFraction * (c100 - c000) + thirdFraction * (c101 - c100) + secondFraction * (c111 - c101);
                }
                else
                {
                    result = c000 + thirdFraction * (c001 - c000) + firstFraction * (c101 - c001) + secondFraction * (c111 - c101);
                }
            }
            else if (firstFraction >= thirdFraction)
            {
                result = c000 + secondFraction * (c010 - c000) + firstFraction * (c110 - c010) + thirdFraction * (c111 - c110);
            }
            else if (secondFraction >= thirdFraction)
            {
                result = c000 + secondFraction * (c010 - c000) + thirdFraction * (c011 - c010) + firstFraction * (c111 - c011);
            }
            else
            {
                result = c000 + thirdFraction * (c001 - c000) + secondFraction * (c011 - c001) + firstFraction * (c111 - c011);
            }
            output[channel] = result;
            channel++;
        }
        return output;
    }

    internal double Clut(int first, int second, int third, int channel)
    {
        int index = ((first * GridPoints + second) * GridPoints + third) * 3 + channel;
        return Element(ClutOffset, index);
    }

    internal double Element(int offset, int index)
    {
        int position = offset + index * PrecisionBytes;
        if (PrecisionBytes == 1)
        {
            return Bytes[position] / 255.0;
        }
        return RawIccTransformProcessor.ReadUInt16(Bytes, position) / 65535.0;
    }
}

internal static class RawIccTransformProcessor
{
    internal static RawRgbImage ConvertToXyz(RawRgbImage image, RawIccProfile profile, RawIccTransformOptions options)
    {
        RawRgbChannelIndices? channels = Validate(image, profile, options, true);
        RawIccTransformDefinition transform = SelectTransform(profile, options.RenderingIntent, true);
        RequireCurves(profile, transform, false);
        return Convert(image, profile, options, transform, channels, true);
    }

    internal static RawRgbImage ConvertFromXyz(RawRgbImage image, RawIccProfile profile, RawIccTransformOptions options)
    {
        Validate(image, profile, options, false);
        RawIccTransformDefinition transform = SelectTransform(profile, options.RenderingIntent, false);
        RequireCurves(profile, transform, true);
        return Convert(image, profile, options, transform, null, false);
    }

    internal static RawRgbImage Convert(RawRgbImage image, RawIccProfile profile, RawIccTransformOptions options, RawIccTransformDefinition transform, RawRgbChannelIndices? channels, bool toXyz)
    {
        int channelCount = image.ChannelCount;
        long pixelCount = CheckedMath.PixelCount(image.Width, image.Height, options.Limits);
        long sampleCountLong = CheckedMath.Multiply(pixelCount, channelCount);
        CheckedMath.DecodedSize(sampleCountLong, 4, options.Limits);
        CheckedMath.AllocationSize(sampleCountLong, 4, options.Limits);
        float[] output = new float[CheckedMath.ToInt32(sampleCountLong)];
        int alpha = channelCount == 4 ? (toXyz ? RawCameraColorProcessor.FindAlpha(image) : 3) : -1;
        int y = 0;
        while (y < image.Height)
        {
            options.CheckCancellation();
            int x = 0;
            while (x < image.Width)
            {
                int source = y * image.RowStrideSamples + x * channelCount;
                int destination = (y * image.Width + x) * channelCount;
                double first = image.FloatingSamples[source + (toXyz ? (channels ?? throw new System.InvalidOperationException("A required value is unavailable.")).Red : 0)];
                double second = image.FloatingSamples[source + (toXyz ? (channels ?? throw new System.InvalidOperationException("A required value is unavailable.")).Green : 1)];
                double third = image.FloatingSamples[source + (toXyz ? (channels ?? throw new System.InvalidOperationException("A required value is unavailable.")).Blue : 2)];
                if (!RawModelValidation.IsFinite(first) || !RawModelValidation.IsFinite(second) || !RawModelValidation.IsFinite(third))
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "ICC conversion requires finite floating-point color samples.", -1);
                }
                double[] converted = toXyz ? ToXyz(first, second, third, profile, options.RenderingIntent, transform) : FromXyz(first, second, third, profile, options.RenderingIntent, transform);
                Store(output, destination, converted[0]);
                Store(output, destination + 1, converted[1]);
                Store(output, destination + 2, converted[2]);
                if (channelCount == 4)
                {
                    Store(output, destination + 3, image.FloatingSamples[source + alpha]);
                }
                x++;
            }
            y++;
        }
        RawChannelDescription[] descriptions = toXyz ? RawOutputColorProcessor.XyzChannels(channelCount) : RawOutputColorProcessor.RgbChannels(channelCount);
        RawColorSpace colorSpace = toXyz ? RawColorSpace.XyzD50 : RawColorSpace.IccRgb;
        RawTransferFunction transfer = toXyz ? RawTransferFunction.Linear : RawTransferFunction.Icc;
        return new RawRgbImage(image.Width, image.Height, channelCount, image.Width * channelCount, null, null, output, descriptions, colorSpace, transfer, image.Orientation, image.OrientationApplied, true, image.Diagnostics);
    }

    internal static double[] ToXyz(double first, double second, double third, RawIccProfile profile, RawIccRenderingIntent intent, RawIccTransformDefinition transform)
    {
        double[] xyz;
        if (transform.Lut != null)
        {
            xyz = transform.Lut.Transform(first, second, third, false);
            double scale = PcsScale(transform.Lut);
            xyz[0] *= scale;
            xyz[1] *= scale;
            xyz[2] *= scale;
        }
        else if (transform.MultiProcess != null)
        {
            xyz = transform.MultiProcess.Transform(first, second, third);
            double scale = 65535.0 / 32768.0;
            xyz[0] *= scale;
            xyz[1] *= scale;
            xyz[2] *= scale;
        }
        else
        {
            double[] linear = new double[]
            {
                EvaluateCurve(profile.RedTrc, first),
                EvaluateCurve(profile.GreenTrc, second),
                EvaluateCurve(profile.BlueTrc, third)
            };
            xyz = RawMatrixMath.MultiplyVector(transform.Matrix, linear);
        }
        ApplyAbsolute(profile, xyz, intent, true);
        return xyz;
    }

    internal static double[] FromXyz(double first, double second, double third, RawIccProfile profile, RawIccRenderingIntent intent, RawIccTransformDefinition transform)
    {
        double[] xyz = new double[] { first, second, third };
        ApplyAbsolute(profile, xyz, intent, false);
        if (transform.Lut != null)
        {
            double scale = PcsScale(transform.Lut);
            return transform.Lut.Transform(xyz[0] / scale, xyz[1] / scale, xyz[2] / scale, true);
        }
        if (transform.MultiProcess != null)
        {
            double scale = 65535.0 / 32768.0;
            return transform.MultiProcess.Transform(xyz[0] / scale, xyz[1] / scale, xyz[2] / scale);
        }
        double[] linear = RawMatrixMath.MultiplyVector(transform.Matrix, xyz);
        return new double[]
        {
            InvertCurve(profile.RedTrc, linear[0]),
            InvertCurve(profile.GreenTrc, linear[1]),
            InvertCurve(profile.BlueTrc, linear[2])
        };
    }

    internal static RawRgbChannelIndices? Validate(RawRgbImage image, RawIccProfile profile, RawIccTransformOptions options, bool toXyz)
    {
        if (image == null || profile == null || options == null || !options.Validate().IsValid() || !profile.Validate().IsValid() || !image.Validate(options == null ? null : options.Limits).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A valid floating image, ICC profile, and transform options are required.", -1);
        }
        if (profile.Header == null || profile.Header.DataColorSpace != "RGB " || profile.Header.ProfileConnectionSpace != "XYZ ")
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "Managed ICC transforms currently require RGB profile data and an XYZ profile connection space.", profile.Offset);
        }
        if (image.FloatingSamples == null || image.FloatingSamples.Length == 0 || image.ChannelCount != 3 && image.ChannelCount != 4)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "ICC conversion requires floating RGB, RGBA, XYZ, or XYZA samples.", -1);
        }
        RawRgbChannelIndices? channels = null;
        if (toXyz)
        {
            channels = RawWhiteBalanceProcessor.FindChannels(image);
            if (image.ColorSpace != RawColorSpace.IccRgb || image.TransferFunction != RawTransferFunction.Icc || channels.Red < 0 || channels.Green < 0 || channels.Blue < 0 || image.ChannelCount == 4 && RawCameraColorProcessor.FindAlpha(image) < 0)
            {
                throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "An input-profile transform requires ICC-encoded RGB or RGBA samples.", -1);
            }
        }
        else if (image.ColorSpace != RawColorSpace.XyzD50 || image.TransferFunction != RawTransferFunction.Linear || !RawOutputColorProcessor.XyzOrder(image))
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "An output-profile transform requires linear D50 XYZ or XYZA samples.", -1);
        }
        (options ?? throw new System.InvalidOperationException("A required value is unavailable.")).CheckCancellation();
        return channels;
    }

    internal static RawIccTransformDefinition SelectTransform(RawIccProfile profile, RawIccRenderingIntent intent, bool toXyz)
    {
        int intentIndex = intent == RawIccRenderingIntent.IccAbsoluteColorimetric ? 1 : (int)intent;
        string prefix = toXyz ? "A2B" : "B2A";
        RawIccTag? tag = profile.GetTag(prefix + intentIndex.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (tag != null)
        {
            return ParseTransformTag(tag, toXyz);
        }
        if (profile.HasMatrixTrc())
        {
            double[]? matrix = toXyz ? profile.Matrix : RawMatrixMath.Invert(profile.Matrix);
            return new RawIccTransformDefinition(null, null, matrix);
        }
        if (intentIndex != 0)
        {
            tag = profile.GetTag(prefix + "0");
            if (tag != null)
            {
                return ParseTransformTag(tag, toXyz);
            }
        }
        throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "The ICC profile has no supported transform for the selected direction and rendering intent.", profile.Offset);
    }

    internal static RawIccTransformDefinition ParseTransformTag(RawIccTag tag, bool toXyz)
    {
        if (tag.TypeSignature == "mft1" || tag.TypeSignature == "mft2")
        {
            return new RawIccTransformDefinition(ParseLut(tag), null, null);
        }
        if (tag.TypeSignature == "mAB " && toXyz || tag.TypeSignature == "mBA " && !toXyz)
        {
            return new RawIccTransformDefinition(null, ParseMultiProcess(tag, toXyz), null);
        }
        if (tag.TypeSignature == "mAB " || tag.TypeSignature == "mBA ")
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "The ICC LUT tag direction does not match its AToB or BToA element type.", tag.Offset);
        }
        throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "The ICC transform tag does not use a reviewed managed LUT element type.", tag.Offset);
    }

    internal static RawIccLut ParseLut(RawIccTag tag)
    {
        if (tag == null || tag.RawBytes == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "An ICC LUT tag is missing its preserved bytes.", -1);
        }
        byte[] bytes = tag.RawBytes;
        int precision;
        int inputEntries;
        int outputEntries;
        int inputOffset;
        if (tag.TypeSignature == "mft1")
        {
            precision = 1;
            inputEntries = 256;
            outputEntries = 256;
            inputOffset = 48;
        }
        else if (tag.TypeSignature == "mft2")
        {
            if (bytes.Length < 52)
            {
                throw InvalidLut("An ICC lut16Type header is truncated.", tag);
            }
            precision = 2;
            inputEntries = ReadUInt16(bytes, 48);
            outputEntries = ReadUInt16(bytes, 50);
            inputOffset = 52;
            if (inputEntries < 2 || inputEntries > 4096 || outputEntries < 2 || outputEntries > 4096)
            {
                throw InvalidLut("ICC lut16Type tables must contain between two and 4096 entries.", tag);
            }
        }
        else
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "Managed ICC LUT transforms support reviewed lut8Type and lut16Type elements.", tag.Offset);
        }
        if (bytes.Length < inputOffset || bytes[8] != 3 || bytes[9] != 3 || bytes[10] < 2 || bytes[11] != 0)
        {
            throw InvalidLut("An ICC LUT transform requires three input channels, three output channels, at least two grid points, and zero reserved bytes.", tag);
        }
        int gridPoints = bytes[10];
        long inputBytes = CheckedMath.Multiply(CheckedMath.Multiply(3, inputEntries), precision);
        long gridValues = CheckedMath.Multiply(CheckedMath.Multiply(CheckedMath.Multiply(gridPoints, gridPoints), gridPoints), 3);
        long clutBytes = CheckedMath.Multiply(gridValues, precision);
        long outputBytes = CheckedMath.Multiply(CheckedMath.Multiply(3, outputEntries), precision);
        long clutOffsetLong = CheckedMath.Add(inputOffset, inputBytes);
        long outputOffsetLong = CheckedMath.Add(clutOffsetLong, clutBytes);
        long expected = CheckedMath.Add(outputOffsetLong, outputBytes);
        if (expected != bytes.Length)
        {
            throw InvalidLut("The ICC LUT byte length does not match its tables and three-dimensional grid.", tag);
        }
        double[] matrix = new double[9];
        int index = 0;
        while (index < matrix.Length)
        {
            matrix[index] = ReadFixed(bytes, 12 + index * 4);
            index++;
        }
        RawMatrixMath.RequireMatrix(matrix, "The ICC LUT matrix");
        return new RawIccLut(bytes, precision, gridPoints, inputEntries, outputEntries, inputOffset, CheckedMath.ToInt32(clutOffsetLong), CheckedMath.ToInt32(outputOffsetLong), matrix);
    }

    internal static RawIccMultiProcess ParseMultiProcess(RawIccTag tag, bool toXyz)
    {
        byte[] bytes = tag.RawBytes;
        if (bytes == null || bytes.Length < 32 || bytes[8] != 3 || bytes[9] != 3 || bytes[10] != 0 || bytes[11] != 0)
        {
            throw InvalidLut("An ICC v4 AToB or BToA transform requires a complete three-channel header.", tag);
        }
        int bOffset = ElementOffset(bytes, 12, tag);
        int matrixOffset = ElementOffset(bytes, 16, tag);
        int mOffset = ElementOffset(bytes, 20, tag);
        int clutOffset = ElementOffset(bytes, 24, tag);
        int aOffset = ElementOffset(bytes, 28, tag);
        RawIccToneCurve[]? aCurves = ParseCurveSet(bytes, aOffset, tag);
        RawIccToneCurve[]? bCurves = ParseCurveSet(bytes, bOffset, tag);
        RawIccToneCurve[]? mCurves = ParseCurveSet(bytes, mOffset, tag);
        RawIccLut? clut = ParseMultiProcessClut(bytes, clutOffset, tag);
        double[]? matrix = null;
        double[]? offset = null;
        if (matrixOffset != 0)
        {
            if (matrixOffset > bytes.Length - 48)
            {
                throw InvalidLut("An ICC v4 LUT matrix is truncated.", tag);
            }
            matrix = new double[9];
            offset = new double[3];
            int index = 0;
            while (index < 9)
            {
                matrix[index] = ReadFixed(bytes, matrixOffset + index * 4);
                index++;
            }
            offset[0] = ReadFixed(bytes, matrixOffset + 36);
            offset[1] = ReadFixed(bytes, matrixOffset + 40);
            offset[2] = ReadFixed(bytes, matrixOffset + 44);
            RawMatrixMath.RequireMatrix(matrix, "The ICC v4 LUT matrix");
            RawMatrixMath.RequireVector(offset, "The ICC v4 LUT matrix offset");
        }
        if (aCurves == null && bCurves == null && mCurves == null && clut == null && matrix == null)
        {
            throw InvalidLut("An ICC v4 LUT transform must contain at least one processing element.", tag);
        }
        return new RawIccMultiProcess(toXyz, aCurves, bCurves, mCurves, clut, matrix, offset);
    }

    internal static int ElementOffset(byte[] bytes, int fieldOffset, RawIccTag tag)
    {
        uint unsignedOffset = ReadUInt32(bytes, fieldOffset);
        if (unsignedOffset == 0)
        {
            return 0;
        }
        if (unsignedOffset > int.MaxValue || unsignedOffset < 32 || (unsignedOffset & 3) != 0 || unsignedOffset >= bytes.Length)
        {
            throw InvalidLut("An ICC v4 LUT processing-element offset is invalid.", tag);
        }
        return (int)unsignedOffset;
    }

    internal static RawIccToneCurve[]? ParseCurveSet(byte[] bytes, int offset, RawIccTag tag)
    {
        if (offset == 0)
        {
            return null;
        }
        RawIccToneCurve[] curves = new RawIccToneCurve[3];
        int position = offset;
        int index = 0;
        while (index < curves.Length)
        {
            RawIccCurveElement element = ParseCurveElement(bytes, position, tag);
            curves[index] = element.Curve;
            position = element.NextOffset;
            index++;
        }
        return curves;
    }

    internal static RawIccCurveElement ParseCurveElement(byte[] bytes, int offset, RawIccTag parent)
    {
        if (offset < 0 || offset > bytes.Length - 12)
        {
            throw InvalidLut("An embedded ICC v4 curve is truncated.", parent);
        }
        string signature = Signature(bytes, offset);
        if (bytes[offset + 4] != 0 || bytes[offset + 5] != 0 || bytes[offset + 6] != 0 || bytes[offset + 7] != 0)
        {
            throw InvalidLut("An embedded ICC v4 curve has nonzero reserved bytes.", parent);
        }
        long length;
        if (signature == "curv")
        {
            uint count = ReadUInt32(bytes, offset + 8);
            length = CheckedMath.Add(12, CheckedMath.Multiply(count, 2));
        }
        else if (signature == "para")
        {
            int functionType = ReadUInt16(bytes, offset + 8);
            if (bytes[offset + 10] != 0 || bytes[offset + 11] != 0 || functionType < 0 || functionType > 4)
            {
                throw InvalidLut("An embedded ICC v4 parametric curve header is invalid.", parent);
            }
            length = 12 + IccCodec.ParametricCount(functionType) * 4;
        }
        else
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "ICC v4 LUT curves must use curveType or parametricCurveType.", parent.Offset + offset);
        }
        if (length > bytes.Length - offset || length > int.MaxValue)
        {
            throw InvalidLut("An embedded ICC v4 curve exceeds its LUT element.", parent);
        }
        int signedLength = (int)length;
        byte[] curveBytes = bytes.AsSpan(offset, signedLength).ToArray();
        RawIccTag curveTag = new RawIccTag("curv", parent.Offset + (uint)offset, (uint)signedLength, signature, curveBytes);
        RawIccToneCurve curve = signature == "curv" ? IccCodec.ParseCurveType(curveTag, 0) : IccCodec.ParseParametricCurve(curveTag, 0);
        RequireCurve(curve, false);
        int next = (int)((length + offset + 3) & ~3L);
        if (next > bytes.Length)
        {
            throw InvalidLut("An embedded ICC v4 curve alignment exceeds its LUT element.", parent);
        }
        int padding = offset + signedLength;
        while (padding < next)
        {
            if (bytes[padding] != 0)
            {
                throw InvalidLut("Embedded ICC v4 curve alignment bytes must be zero.", parent);
            }
            padding++;
        }
        return new RawIccCurveElement(curve, next);
    }

    internal static RawIccLut? ParseMultiProcessClut(byte[] bytes, int offset, RawIccTag tag)
    {
        if (offset == 0)
        {
            return null;
        }
        if (offset > bytes.Length - 20 || bytes[offset] < 2 || bytes[offset] != bytes[offset + 1] || bytes[offset] != bytes[offset + 2])
        {
            throw InvalidLut("A managed ICC v4 CLUT requires three equal grid dimensions of at least two points.", tag);
        }
        int unused = 3;
        while (unused < 16)
        {
            if (bytes[offset + unused] != 0)
            {
                throw InvalidLut("Unused ICC v4 CLUT grid dimensions must be zero.", tag);
            }
            unused++;
        }
        int precision = bytes[offset + 16];
        if ((precision != 1 && precision != 2) || bytes[offset + 17] != 0 || bytes[offset + 18] != 0 || bytes[offset + 19] != 0)
        {
            throw InvalidLut("An ICC v4 CLUT requires one- or two-byte values and zero reserved bytes.", tag);
        }
        int grid = bytes[offset];
        long values = CheckedMath.Multiply(CheckedMath.Multiply(CheckedMath.Multiply(grid, grid), grid), 3);
        long end = CheckedMath.Add(offset + 20, CheckedMath.Multiply(values, precision));
        if (end > bytes.Length)
        {
            throw InvalidLut("An ICC v4 CLUT is truncated.", tag);
        }
        return new RawIccLut(bytes, precision, grid, 0, 0, 0, offset + 20, 0, RawMatrixMath.Identity());
    }

    internal static string Signature(byte[] bytes, int offset)
    {
        return new string(new char[] { (char)bytes[offset], (char)bytes[offset + 1], (char)bytes[offset + 2], (char)bytes[offset + 3] });
    }

    internal static void RequireCurves(RawIccProfile profile, RawIccTransformDefinition transform, bool inverse)
    {
        if (transform.Lut != null || transform.MultiProcess != null)
        {
            return;
        }
        RequireCurve(profile.RedTrc, inverse);
        RequireCurve(profile.GreenTrc, inverse);
        RequireCurve(profile.BlueTrc, inverse);
    }

    internal static void RequireCurve(RawIccToneCurve? curve, bool inverse)
    {
        if (curve == null || curve.Kind < RawIccCurveKind.Identity || curve.Kind > RawIccCurveKind.Parametric)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "An ICC matrix transform requires three supported tone reproduction curves.", -1);
        }
        if (curve.Kind == RawIccCurveKind.Gamma && (!RawModelValidation.IsFinite(curve.Gamma) || curve.Gamma <= 0.0))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "An ICC gamma curve requires a finite positive exponent.", -1);
        }
        if (curve.Kind == RawIccCurveKind.Sampled)
        {
            if (curve.Samples == null || curve.Samples.Length < 2)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "An ICC sampled curve requires at least two entries.", -1);
            }
            int sample = 1;
            while (sample < curve.Samples.Length)
            {
                if (curve.Samples[sample] < curve.Samples[sample - 1])
                {
                    throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "Managed ICC sampled curves must be monotonic.", -1);
                }
                sample++;
            }
        }
        if (curve.Kind == RawIccCurveKind.Parametric)
        {
            int expected = IccCodec.ParametricCount(curve.FunctionType);
            if (expected == 0 || curve.Parameters == null || curve.Parameters.Length != expected || !RawBlackLevel.AllFinite(curve.Parameters) || curve.Parameters[0] <= 0.0 || curve.FunctionType > 0 && curve.Parameters[1] == 0.0)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "An ICC parametric curve contains invalid function parameters.", -1);
            }
        }
        double previous = EvaluateCurve(curve, 0.0);
        int index = 1;
        while (index <= 256)
        {
            double current = EvaluateCurve(curve, index / 256.0);
            if (current + 0.000000000001 < previous)
            {
                throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "Managed ICC tone curves must be monotonic.", -1);
            }
            previous = current;
            index++;
        }
        if (inverse && previous <= EvaluateCurve(curve, 0.0) + 0.000000000001)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "A managed ICC output curve must have a nonzero range.", -1);
        }
    }

    internal static double EvaluateCurve(RawIccToneCurve? curve, double value)
    {
        double input = Clamp(value);
        double result;
        if ((curve ?? throw new System.InvalidOperationException("A required value is unavailable.")).Kind == RawIccCurveKind.Identity)
        {
            result = input;
        }
        else if (curve.Kind == RawIccCurveKind.Gamma)
        {
            result = System.Math.Pow(input, curve.Gamma);
        }
        else if (curve.Kind == RawIccCurveKind.Sampled)
        {
            double position = input * (curve.Samples.Length - 1);
            int lower = (int)System.Math.Floor(position);
            if (lower >= curve.Samples.Length - 1)
            {
                result = curve.Samples[curve.Samples.Length - 1] / 65535.0;
            }
            else
            {
                double fraction = position - lower;
                double first = curve.Samples[lower] / 65535.0;
                double second = curve.Samples[lower + 1] / 65535.0;
                result = first + (second - first) * fraction;
            }
        }
        else
        {
            result = EvaluateParametric(curve, input);
        }
        if (!RawModelValidation.IsFinite(result))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "An ICC tone curve produced a non-finite value.", -1);
        }
        return result;
    }

    internal static double EvaluateParametric(RawIccToneCurve curve, double value)
    {
        double[] p = curve.Parameters;
        double g = p[0];
        if (curve.FunctionType == 0)
        {
            return System.Math.Pow(value, g);
        }
        double a = p[1];
        double b = p[2];
        if (curve.FunctionType == 1)
        {
            return value >= -b / a ? System.Math.Pow(a * value + b, g) : 0.0;
        }
        double c = p[3];
        if (curve.FunctionType == 2)
        {
            return value >= -b / a ? System.Math.Pow(a * value + b, g) + c : c;
        }
        double d = p[4];
        if (curve.FunctionType == 3)
        {
            return value >= d ? System.Math.Pow(a * value + b, g) : c * value;
        }
        double e = p[5];
        double f = p[6];
        return value >= d ? System.Math.Pow(a * value + b, g) + e : c * value + f;
    }

    internal static double InvertCurve(RawIccToneCurve? curve, double value)
    {
        double lowValue = EvaluateCurve(curve, 0.0);
        double highValue = EvaluateCurve(curve, 1.0);
        if (value <= lowValue)
        {
            return 0.0;
        }
        if (value >= highValue)
        {
            return 1.0;
        }
        double low = 0.0;
        double high = 1.0;
        int iteration = 0;
        while (iteration < 48)
        {
            double middle = (low + high) * 0.5;
            if (EvaluateCurve(curve, middle) < value)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
            iteration++;
        }
        return (low + high) * 0.5;
    }

    internal static void ApplyAbsolute(RawIccProfile profile, double[] xyz, RawIccRenderingIntent intent, bool toXyz)
    {
        if (intent != RawIccRenderingIntent.IccAbsoluteColorimetric)
        {
            return;
        }
        if (profile.WhitePoint == null || profile.WhitePoint.Length != 3 || (profile.Header ?? throw new System.InvalidOperationException("A required value is unavailable.")).PcsIlluminant == null || profile.Header.PcsIlluminant.Length != 3)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "ICC absolute colorimetric conversion requires media and PCS white points.", profile.Offset);
        }
        int index = 0;
        while (index < 3)
        {
            double media = profile.WhitePoint[index];
            double pcs = profile.Header.PcsIlluminant[index];
            if (!RawModelValidation.IsFinite(media) || !RawModelValidation.IsFinite(pcs) || media <= 0.0 || pcs <= 0.0)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "ICC absolute colorimetric white points must contain positive finite values.", profile.Offset);
            }
            xyz[index] *= toXyz ? media / pcs : pcs / media;
            index++;
        }
    }

    internal static double PcsScale(RawIccLut lut)
    {
        return lut.PrecisionBytes == 1 ? 255.0 / 128.0 : 65535.0 / 32768.0;
    }

    internal static double Clamp(double value)
    {
        if (!RawModelValidation.IsFinite(value))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "An ICC transform value is not finite.", -1);
        }
        if (value < 0.0)
        {
            return 0.0;
        }
        if (value > 1.0)
        {
            return 1.0;
        }
        return value;
    }

    internal static ushort ReadUInt16(byte[] bytes, int offset)
    {
        return (ushort)((bytes[offset] << 8) | bytes[offset + 1]);
    }

    internal static uint ReadUInt32(byte[] bytes, int offset)
    {
        return ((uint)bytes[offset] << 24) | ((uint)bytes[offset + 1] << 16) | ((uint)bytes[offset + 2] << 8) | bytes[offset + 3];
    }

    internal static double ReadFixed(byte[] bytes, int offset)
    {
        return BitUtility.ToInt32Bits(ReadUInt32(bytes, offset)) / 65536.0;
    }

    internal static RawFormatException InvalidLut(string message, RawIccTag tag)
    {
        return new RawFormatException(RawDiagnosticCode.InvalidMetadata, message, tag == null ? -1 : tag.Offset);
    }

    internal static void Store(float[] output, int index, double value)
    {
        if (!RawModelValidation.IsFinite(value) || value < -float.MaxValue || value > float.MaxValue)
        {
            throw new RawFormatException(RawDiagnosticCode.ArithmeticOverflow, "An ICC output sample exceeds the floating-point range.", -1);
        }
        output[index] = (float)value;
    }
}
