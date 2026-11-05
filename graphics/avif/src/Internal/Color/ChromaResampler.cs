using System;

namespace avif;

internal static class ChromaResampler
{
    internal static AvifResultCode ResampleImageChroma(
        AvifImage source,
        AvifImage destination,
        AvifResamplingFilter filter)
    {
        AvifResultCode validation = ValidateImages(
            source, destination, filter);
        if (validation != AvifResultCode.Success)
        {
            return validation;
        }
        AvifResultCode uResult = ResamplePlane(
            source.GetPlane(AvifPlaneKind.U),
            destination.GetPlane(AvifPlaneKind.U),
            source.GetWidth(),
            source.GetHeight(),
            source.GetChromaFormat(),
            source.GetChromaSamplePosition(),
            destination.GetChromaFormat(),
            destination.GetChromaSamplePosition(),
            filter);
        if (uResult != AvifResultCode.Success)
        {
            return uResult;
        }
        return ResamplePlane(
            source.GetPlane(AvifPlaneKind.V),
            destination.GetPlane(AvifPlaneKind.V),
            source.GetWidth(),
            source.GetHeight(),
            source.GetChromaFormat(),
            source.GetChromaSamplePosition(),
            destination.GetChromaFormat(),
            destination.GetChromaSamplePosition(),
            filter);
    }

    internal static AvifResultCode ResamplePlane(
        AvifPlane? source,
        AvifPlane? destination,
        int lumaWidth,
        int lumaHeight,
        AvifChromaFormat sourceFormat,
        AvifChromaSamplePosition sourcePosition,
        AvifChromaFormat destinationFormat,
        AvifChromaSamplePosition destinationPosition,
        AvifResamplingFilter filter)
    {
        AvifResultCode validation = ValidatePlanes(
            source,
            destination,
            lumaWidth,
            lumaHeight,
            sourceFormat,
            sourcePosition,
            destinationFormat,
            destinationPosition,
            filter);
        if (validation != AvifResultCode.Success)
        {
            return validation;
        }
        if (sourceFormat == destinationFormat
            && sourcePosition == destinationPosition)
        {
            return (destination ?? throw new InvalidOperationException("Invalid internal state.")).CopyFrom(source);
        }

        if (filter == AvifResamplingFilter.Nearest)
        {
            return ResampleNearest(
                source,
                destination,
                sourceFormat,
                sourcePosition,
                destinationFormat,
                destinationPosition);
        }
        return ResampleFiltered(
            source,
            destination,
            sourceFormat,
            sourcePosition,
            destinationFormat,
            destinationPosition,
            filter);
    }

    static AvifResultCode ResampleNearest(
        AvifPlane? source,
        AvifPlane? destination,
        AvifChromaFormat sourceFormat,
        AvifChromaSamplePosition sourcePosition,
        AvifChromaFormat destinationFormat,
        AvifChromaSamplePosition destinationPosition)
    {
        AvifPlane? input = source;
        AvifPlane? scratch = null;
        if (((source ?? throw new InvalidOperationException("Invalid internal state.")).GetInternalBuffer() ?? throw new InvalidOperationException("Invalid internal state.")).Buffer
            == ((destination ?? throw new InvalidOperationException("Invalid internal state.")).GetInternalBuffer() ?? throw new InvalidOperationException("Invalid internal state.")).Buffer)
        {
            scratch = source.CreateCopy();
            if (scratch.GetCode() != AvifResultCode.Success)
            {
                return scratch.GetCode();
            }
            input = scratch;
        }

        double sourceStepX = HasSubsamplingX(sourceFormat) ? 2 : 1;
        double sourceStepY = HasSubsamplingY(sourceFormat) ? 2 : 1;
        double destinationStepX = HasSubsamplingX(destinationFormat) ? 2 : 1;
        double destinationStepY = HasSubsamplingY(destinationFormat) ? 2 : 1;
        double sourceOffsetX = GetOffsetX(sourceFormat, sourcePosition);
        double sourceOffsetY = GetOffsetY(sourceFormat, sourcePosition);
        double destinationOffsetX = GetOffsetX(
            destinationFormat, destinationPosition);
        double destinationOffsetY = GetOffsetY(
            destinationFormat, destinationPosition);
        int maximum = (1 << destination.GetBitDepth()) - 1;

        int y = 0;
        while (y < destination.GetHeight())
        {
            double sourceY = (y * destinationStepY + destinationOffsetY
                - sourceOffsetY) / sourceStepY;
            int nearestY = ClampIndex(
                (int)System.Math.Floor(sourceY + 0.5), (input ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight());
            Span<byte> sourceRow = input.GetRowSpan(nearestY);
            Span<byte> destinationRow = destination.GetRowSpan(y);
            int x = 0;
            while (x < destination.GetWidth())
            {
                double sourceX = (x * destinationStepX + destinationOffsetX
                    - sourceOffsetX) / sourceStepX;
                int nearestX = ClampIndex(
                    (int)System.Math.Floor(sourceX + 0.5), input.GetWidth());
                int value = ReadSample(
                    sourceRow, nearestX, input.GetBitDepth());
                if (value > maximum)
                {
                    value = maximum;
                }
                WriteSample(
                    destinationRow,
                    x,
                    destination.GetBitDepth(),
                    value);
                x++;
            }
            y++;
        }

        if (scratch != null)
        {
            scratch.Close();
        }
        return AvifResultCode.Success;
    }

    static AvifResultCode ResampleFiltered(
        AvifPlane? source,
        AvifPlane? destination,
        AvifChromaFormat sourceFormat,
        AvifChromaSamplePosition sourcePosition,
        AvifChromaFormat destinationFormat,
        AvifChromaSamplePosition destinationPosition,
        AvifResamplingFilter filter)
    {
        long intermediateCount = (long)(destination ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth()
            * (source ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight();
        if (intermediateCount > System.Array.MaxLength)
        {
            return AvifResultCode.LimitExceeded;
        }
        long intermediateBytes = intermediateCount * 8;
        if (intermediateBytes > AvifSecurityLimits.DefaultMaxTemporaryBytes)
        {
            return AvifResultCode.LimitExceeded;
        }
        double[] horizontal = new double[(int)intermediateCount];

        double sourceStepX = HasSubsamplingX(sourceFormat) ? 2 : 1;
        double sourceStepY = HasSubsamplingY(sourceFormat) ? 2 : 1;
        double destinationStepX = HasSubsamplingX(destinationFormat) ? 2 : 1;
        double destinationStepY = HasSubsamplingY(destinationFormat) ? 2 : 1;
        double sourceOffsetX = GetOffsetX(sourceFormat, sourcePosition);
        double sourceOffsetY = GetOffsetY(sourceFormat, sourcePosition);
        double destinationOffsetX = GetOffsetX(
            destinationFormat, destinationPosition);
        double destinationOffsetY = GetOffsetY(
            destinationFormat, destinationPosition);
        double horizontalScale = System.Math.Max(
            1, destinationStepX / sourceStepX);
        double verticalScale = System.Math.Max(
            1, destinationStepY / sourceStepY);

        int sourceY = 0;
        while (sourceY < source.GetHeight())
        {
            Span<byte> sourceRow = source.GetRowSpan(sourceY);
            int destinationX = 0;
            while (destinationX < destination.GetWidth())
            {
                double center = (destinationX * destinationStepX
                    + destinationOffsetX - sourceOffsetX) / sourceStepX;
                horizontal[sourceY * destination.GetWidth() + destinationX]
                    = FilterHorizontal(
                        sourceRow,
                        source.GetWidth(),
                        source.GetBitDepth(),
                        center,
                        horizontalScale,
                        filter);
                destinationX++;
            }
            sourceY++;
        }

        int maximum = (1 << destination.GetBitDepth()) - 1;
        int destinationY = 0;
        while (destinationY < destination.GetHeight())
        {
            Span<byte> destinationRow = destination.GetRowSpan(destinationY);
            double center = (destinationY * destinationStepY
                + destinationOffsetY - sourceOffsetY) / sourceStepY;
            int destinationX = 0;
            while (destinationX < destination.GetWidth())
            {
                double value = FilterVertical(
                    horizontal,
                    destination.GetWidth(),
                    source.GetHeight(),
                    destinationX,
                    center,
                    verticalScale,
                    filter);
                int rounded = (int)System.Math.Floor(value + 0.5);
                if (rounded < 0)
                {
                    rounded = 0;
                }
                else if (rounded > maximum)
                {
                    rounded = maximum;
                }
                WriteSample(
                    destinationRow,
                    destinationX,
                    destination.GetBitDepth(),
                    rounded);
                destinationX++;
            }
            destinationY++;
        }
        return AvifResultCode.Success;
    }

    static double FilterHorizontal(
        Span<byte> row,
        int width,
        int bitDepth,
        double center,
        double scale,
        AvifResamplingFilter filter)
    {
        double radius = GetRadius(filter) * scale;
        int start = (int)System.Math.Floor(center - radius);
        int end = (int)System.Math.Ceiling(center + radius);
        double sum = 0;
        double weightSum = 0;
        int index = start;
        while (index <= end)
        {
            double weight = GetWeight((center - index) / scale, filter);
            if (weight != 0)
            {
                int sourceIndex = ClampIndex(index, width);
                sum += weight * ReadSample(row, sourceIndex, bitDepth);
                weightSum += weight;
            }
            index++;
        }
        return weightSum == 0 ? 0 : sum / weightSum;
    }

    static double FilterVertical(
        double[] horizontal,
        int width,
        int height,
        int x,
        double center,
        double scale,
        AvifResamplingFilter filter)
    {
        double radius = GetRadius(filter) * scale;
        int start = (int)System.Math.Floor(center - radius);
        int end = (int)System.Math.Ceiling(center + radius);
        double sum = 0;
        double weightSum = 0;
        int index = start;
        while (index <= end)
        {
            double weight = GetWeight((center - index) / scale, filter);
            if (weight != 0)
            {
                int sourceIndex = ClampIndex(index, height);
                sum += weight * horizontal[sourceIndex * width + x];
                weightSum += weight;
            }
            index++;
        }
        return weightSum == 0 ? 0 : sum / weightSum;
    }

    static double GetRadius(AvifResamplingFilter filter)
    {
        return filter == AvifResamplingFilter.Bilinear
            ? 1
            : filter == AvifResamplingFilter.Bicubic ? 2 : 3;
    }

    static double GetWeight(
        double distance,
        AvifResamplingFilter filter)
    {
        double absolute = System.Math.Abs(distance);
        if (filter == AvifResamplingFilter.Bilinear)
        {
            return absolute < 1 ? 1 - absolute : 0;
        }
        if (filter == AvifResamplingFilter.Bicubic)
        {
            if (absolute < 1)
            {
                return 1.5 * absolute * absolute * absolute
                    - 2.5 * absolute * absolute + 1;
            }
            if (absolute < 2)
            {
                return -0.5 * absolute * absolute * absolute
                    + 2.5 * absolute * absolute
                    - 4 * absolute + 2;
            }
            return 0;
        }
        if (absolute == 0)
        {
            return 1;
        }
        if (absolute >= 3)
        {
            return 0;
        }
        double piDistance = System.Math.PI * distance;
        return System.Math.Sin(piDistance) / piDistance
            * System.Math.Sin(piDistance / 3) / (piDistance / 3);
    }

    static AvifResultCode ValidateImages(
        AvifImage source,
        AvifImage destination,
        AvifResamplingFilter filter)
    {
        if (source == null || destination == null)
        {
            return AvifResultCode.InvalidArgument;
        }
        AvifResultCode destinationState = destination.GetCode();
        if (destinationState != AvifResultCode.Success)
        {
            return destinationState;
        }
        AvifResultCode sourceState = source.GetCode();
        if (sourceState != AvifResultCode.Success)
        {
            return sourceState;
        }
        if (source.GetWidth() != destination.GetWidth()
            || source.GetHeight() != destination.GetHeight()
            || source.GetBitDepth() != destination.GetBitDepth()
            || source.GetRange() != destination.GetRange()
            || source.GetChromaFormat() == AvifChromaFormat.Monochrome
            || destination.GetChromaFormat() == AvifChromaFormat.Monochrome
            || !IsValidFilter(filter))
        {
            return AvifResultCode.InvalidArgument;
        }
        return AvifResultCode.Success;
    }

    static AvifResultCode ValidatePlanes(
        AvifPlane? source,
        AvifPlane? destination,
        int lumaWidth,
        int lumaHeight,
        AvifChromaFormat sourceFormat,
        AvifChromaSamplePosition sourcePosition,
        AvifChromaFormat destinationFormat,
        AvifChromaSamplePosition destinationPosition,
        AvifResamplingFilter filter)
    {
        if (source == null || destination == null
            || lumaWidth <= 0 || lumaHeight <= 0)
        {
            return AvifResultCode.InvalidArgument;
        }
        AvifResultCode destinationState = destination.GetCode();
        if (destinationState != AvifResultCode.Success)
        {
            return destinationState;
        }
        AvifResultCode sourceState = source.GetCode();
        if (sourceState != AvifResultCode.Success)
        {
            return sourceState;
        }
        if (!IsChroma(source.GetKind())
            || source.GetKind() != destination.GetKind()
            || source.GetBitDepth() != destination.GetBitDepth()
            || !IsValidFormat(sourceFormat)
            || !IsValidFormat(destinationFormat)
            || sourceFormat == AvifChromaFormat.Monochrome
            || destinationFormat == AvifChromaFormat.Monochrome
            || !IsValidPosition(sourcePosition)
            || !IsValidPosition(destinationPosition)
            || !IsValidFilter(filter)
            || source.GetWidth() != GetPlaneWidth(lumaWidth, sourceFormat)
            || source.GetHeight() != GetPlaneHeight(lumaHeight, sourceFormat)
            || destination.GetWidth()
                != GetPlaneWidth(lumaWidth, destinationFormat)
            || destination.GetHeight()
                != GetPlaneHeight(lumaHeight, destinationFormat))
        {
            return AvifResultCode.InvalidArgument;
        }
        return AvifResultCode.Success;
    }

    static int GetPlaneWidth(int lumaWidth, AvifChromaFormat format)
    {
        return HasSubsamplingX(format)
            ? (lumaWidth >> 1) + (lumaWidth & 1)
            : lumaWidth;
    }

    static int GetPlaneHeight(int lumaHeight, AvifChromaFormat format)
    {
        return HasSubsamplingY(format)
            ? (lumaHeight >> 1) + (lumaHeight & 1)
            : lumaHeight;
    }

    static double GetOffsetX(
        AvifChromaFormat format,
        AvifChromaSamplePosition position)
    {
        return 0;
    }

    static double GetOffsetY(
        AvifChromaFormat format,
        AvifChromaSamplePosition position)
    {
        if (!HasSubsamplingY(format)
            || position == AvifChromaSamplePosition.Colocated)
        {
            return 0;
        }
        return 0.5;
    }

    static bool HasSubsamplingX(AvifChromaFormat format)
    {
        return format == AvifChromaFormat.Yuv420
            || format == AvifChromaFormat.Yuv422;
    }

    static bool HasSubsamplingY(AvifChromaFormat format)
    {
        return format == AvifChromaFormat.Yuv420;
    }

    static bool IsChroma(AvifPlaneKind kind)
    {
        return kind == AvifPlaneKind.U || kind == AvifPlaneKind.V;
    }

    static bool IsValidFormat(AvifChromaFormat format)
    {
        int value = (int)format;
        return value >= (int)AvifChromaFormat.Monochrome
            && value <= (int)AvifChromaFormat.Yuv444;
    }

    static bool IsValidPosition(AvifChromaSamplePosition position)
    {
        int value = (int)position;
        return value >= (int)AvifChromaSamplePosition.Unknown
            && value <= (int)AvifChromaSamplePosition.Colocated;
    }

    static bool IsValidFilter(AvifResamplingFilter filter)
    {
        int value = (int)filter;
        return value >= (int)AvifResamplingFilter.Nearest
            && value <= (int)AvifResamplingFilter.Lanczos3;
    }

    static int ClampIndex(int value, int length)
    {
        if (value < 0)
        {
            return 0;
        }
        if (value >= length)
        {
            return length - 1;
        }
        return value;
    }

    static int ReadSample(Span<byte> row, int x, int bitDepth)
    {
        if (bitDepth == 8)
        {
            return row[x];
        }
        int offset = x * 2;
        return row[offset] | row[offset + 1] << 8;
    }

    static void WriteSample(
        Span<byte> row,
        int x,
        int bitDepth,
        int value)
    {
        if (bitDepth == 8)
        {
            row[x] = IntegerConversion.ToByte(value);
            return;
        }
        int offset = x * 2;
        row[offset] = IntegerConversion.ToByte(value);
        row[offset + 1] = IntegerConversion.ToByte(value >> 8);
    }
}
