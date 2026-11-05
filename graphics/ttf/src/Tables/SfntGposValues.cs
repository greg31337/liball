namespace ttf;

internal class SfntGposValueRecord
{
    int valueFormat;
    int xPlacement;
    int yPlacement;
    int xAdvance;
    int yAdvance;
    int xPlacementAdjustmentOffset;
    int yPlacementAdjustmentOffset;
    int xAdvanceAdjustmentOffset;
    int yAdvanceAdjustmentOffset;
    int xPlacementAdjustmentTableOffset;
    int yPlacementAdjustmentTableOffset;
    int xAdvanceAdjustmentTableOffset;
    int yAdvanceAdjustmentTableOffset;
    SfntDeviceOrVariationIndex? xPlacementAdjustment;
    SfntDeviceOrVariationIndex? yPlacementAdjustment;
    SfntDeviceOrVariationIndex? xAdvanceAdjustment;
    SfntDeviceOrVariationIndex? yAdvanceAdjustment;

    internal SfntGposValueRecord(
        int valueFormat,
        int xPlacement,
        int yPlacement,
        int xAdvance,
        int yAdvance,
        int xPlacementAdjustmentOffset,
        int yPlacementAdjustmentOffset,
        int xAdvanceAdjustmentOffset,
        int yAdvanceAdjustmentOffset,
        int xPlacementAdjustmentTableOffset,
        int yPlacementAdjustmentTableOffset,
        int xAdvanceAdjustmentTableOffset,
        int yAdvanceAdjustmentTableOffset,
        SfntDeviceOrVariationIndex? xPlacementAdjustment,
        SfntDeviceOrVariationIndex? yPlacementAdjustment,
        SfntDeviceOrVariationIndex? xAdvanceAdjustment,
        SfntDeviceOrVariationIndex? yAdvanceAdjustment)
    {
        this.valueFormat = valueFormat;
        this.xPlacement = xPlacement;
        this.yPlacement = yPlacement;
        this.xAdvance = xAdvance;
        this.yAdvance = yAdvance;
        this.xPlacementAdjustmentOffset = xPlacementAdjustmentOffset;
        this.yPlacementAdjustmentOffset = yPlacementAdjustmentOffset;
        this.xAdvanceAdjustmentOffset = xAdvanceAdjustmentOffset;
        this.yAdvanceAdjustmentOffset = yAdvanceAdjustmentOffset;
        this.xPlacementAdjustmentTableOffset =
            xPlacementAdjustmentTableOffset;
        this.yPlacementAdjustmentTableOffset =
            yPlacementAdjustmentTableOffset;
        this.xAdvanceAdjustmentTableOffset = xAdvanceAdjustmentTableOffset;
        this.yAdvanceAdjustmentTableOffset = yAdvanceAdjustmentTableOffset;
        this.xPlacementAdjustment = xPlacementAdjustment;
        this.yPlacementAdjustment = yPlacementAdjustment;
        this.xAdvanceAdjustment = xAdvanceAdjustment;
        this.yAdvanceAdjustment = yAdvanceAdjustment;
    }

    internal int GetValueFormat()
    {
        return valueFormat;
    }

    internal int GetXPlacement()
    {
        return xPlacement;
    }

    internal int GetYPlacement()
    {
        return yPlacement;
    }

    internal int GetXAdvance()
    {
        return xAdvance;
    }

    internal int GetYAdvance()
    {
        return yAdvance;
    }

    internal int GetXPlacementAdjustmentOffset()
    {
        return xPlacementAdjustmentOffset;
    }

    internal int GetYPlacementAdjustmentOffset()
    {
        return yPlacementAdjustmentOffset;
    }

    internal int GetXAdvanceAdjustmentOffset()
    {
        return xAdvanceAdjustmentOffset;
    }

    internal int GetYAdvanceAdjustmentOffset()
    {
        return yAdvanceAdjustmentOffset;
    }

    internal int GetXPlacementAdjustmentTableOffset()
    {
        return xPlacementAdjustmentTableOffset;
    }

    internal int GetYPlacementAdjustmentTableOffset()
    {
        return yPlacementAdjustmentTableOffset;
    }

    internal int GetXAdvanceAdjustmentTableOffset()
    {
        return xAdvanceAdjustmentTableOffset;
    }

    internal int GetYAdvanceAdjustmentTableOffset()
    {
        return yAdvanceAdjustmentTableOffset;
    }

    internal SfntDeviceOrVariationIndex? GetXPlacementAdjustment()
    {
        return xPlacementAdjustment;
    }

    internal SfntDeviceOrVariationIndex? GetYPlacementAdjustment()
    {
        return yPlacementAdjustment;
    }

    internal SfntDeviceOrVariationIndex? GetXAdvanceAdjustment()
    {
        return xAdvanceAdjustment;
    }

    internal SfntDeviceOrVariationIndex? GetYAdvanceAdjustment()
    {
        return yAdvanceAdjustment;
    }

    internal void ValidateVariationIndexes(
        SfntItemVariationStore? variationStore)
    {
        ValidateVariationIndex(xPlacementAdjustment, variationStore);
        ValidateVariationIndex(yPlacementAdjustment, variationStore);
        ValidateVariationIndex(xAdvanceAdjustment, variationStore);
        ValidateVariationIndex(yAdvanceAdjustment, variationStore);
    }

    static void ValidateVariationIndex(
        SfntDeviceOrVariationIndex? adjustment,
        SfntItemVariationStore? variationStore)
    {
        if (adjustment == null || !adjustment.IsVariationIndex())
        {
            return;
        }

        if (variationStore == null)
        {
            throw FontFormatException.Create(
                "A GPOS VariationIndex requires a GDEF ItemVariationStore.");
        }

        SfntVariationIndex? variationIndex = adjustment.GetVariationIndex();
        System.ArgumentNullException.ThrowIfNull(variationIndex);
        variationStore.ValidateDeltaSetIndex(
            variationIndex.GetDeltaSetIndex());
    }
}

internal class SfntGposAdjustment
{
    double xPlacement;
    double yPlacement;
    double xAdvance;
    double yAdvance;

    internal SfntGposAdjustment(
        double xPlacement,
        double yPlacement,
        double xAdvance,
        double yAdvance)
    {
        this.xPlacement = xPlacement;
        this.yPlacement = yPlacement;
        this.xAdvance = xAdvance;
        this.yAdvance = yAdvance;
    }

    internal double GetXPlacement()
    {
        return xPlacement;
    }

    internal double GetYPlacement()
    {
        return yPlacement;
    }

    internal double GetXAdvance()
    {
        return xAdvance;
    }

    internal double GetYAdvance()
    {
        return yAdvance;
    }
}

internal class SfntGposAnchorPosition
{
    double xCoordinate;
    double yCoordinate;
    bool hasAnchorPoint;
    int anchorPoint;

    internal SfntGposAnchorPosition(
        double xCoordinate,
        double yCoordinate,
        bool hasAnchorPoint,
        int anchorPoint)
    {
        this.xCoordinate = xCoordinate;
        this.yCoordinate = yCoordinate;
        this.hasAnchorPoint = hasAnchorPoint;
        this.anchorPoint = anchorPoint;
    }

    internal double GetXCoordinate()
    {
        return xCoordinate;
    }

    internal double GetYCoordinate()
    {
        return yCoordinate;
    }

    internal bool HasAnchorPoint()
    {
        return hasAnchorPoint;
    }

    internal int GetAnchorPoint()
    {
        return anchorPoint;
    }
}

internal class SfntGposAdjustmentContext
{
    int unitsPerEm;
    int horizontalPixelsPerEm;
    int verticalPixelsPerEm;
    SfntVariationLocation? variationLocation;

    internal SfntGposAdjustmentContext(
        int unitsPerEm,
        int horizontalPixelsPerEm,
        int verticalPixelsPerEm,
        SfntVariationLocation? variationLocation)
    {
        if (unitsPerEm <= 0)
        {
            throw new System.ArgumentOutOfRangeException("unitsPerEm");
        }

        if (horizontalPixelsPerEm < 0 || horizontalPixelsPerEm > 65535)
        {
            throw new System.ArgumentOutOfRangeException(
                "horizontalPixelsPerEm");
        }

        if (verticalPixelsPerEm < 0 || verticalPixelsPerEm > 65535)
        {
            throw new System.ArgumentOutOfRangeException(
                "verticalPixelsPerEm");
        }

        this.unitsPerEm = unitsPerEm;
        this.horizontalPixelsPerEm = horizontalPixelsPerEm;
        this.verticalPixelsPerEm = verticalPixelsPerEm;
        this.variationLocation = variationLocation;
    }

    internal SfntGposAdjustment ResolveValueRecord(
        SfntGposValueRecord valueRecord)
    {
        if (valueRecord == null)
        {
            throw new System.ArgumentNullException("valueRecord");
        }

        return new SfntGposAdjustment(
            valueRecord.GetXPlacement() + CalculateAdjustment(
                valueRecord.GetXPlacementAdjustment(),
                true),
            valueRecord.GetYPlacement() + CalculateAdjustment(
                valueRecord.GetYPlacementAdjustment(),
                false),
            valueRecord.GetXAdvance() + CalculateAdjustment(
                valueRecord.GetXAdvanceAdjustment(),
                true),
            valueRecord.GetYAdvance() + CalculateAdjustment(
                valueRecord.GetYAdvanceAdjustment(),
                false));
    }

    internal SfntGposAnchorPosition ResolveAnchor(
        SfntLayoutAnchor? anchor)
    {
        if (anchor == null)
        {
            throw new System.ArgumentNullException("anchor");
        }

        return new SfntGposAnchorPosition(
            anchor.GetXCoordinate() + CalculateAdjustment(
                anchor.GetXAdjustment(),
                true),
            anchor.GetYCoordinate() + CalculateAdjustment(
                anchor.GetYAdjustment(),
                false),
            anchor.HasAnchorPoint(),
            anchor.GetAnchorPoint());
    }

    internal double ResolveAdjustment(
        SfntDeviceOrVariationIndex adjustment,
        bool horizontal)
    {
        return CalculateAdjustment(adjustment, horizontal);
    }

    double CalculateAdjustment(
        SfntDeviceOrVariationIndex? adjustment,
        bool horizontal)
    {
        if (adjustment == null)
        {
            return 0;
        }

        if (adjustment.IsVariationIndex())
        {
            if (variationLocation == null)
            {
                throw new System.InvalidOperationException(
                    "A GPOS VariationIndex requires a variation location.");
            }

            SfntVariationIndex? variationIndex = adjustment.GetVariationIndex();
            System.ArgumentNullException.ThrowIfNull(variationIndex);
            return variationIndex.CalculateDelta(
                variationLocation);
        }

        int pixelsPerEm = horizontal ?
            horizontalPixelsPerEm : verticalPixelsPerEm;

        if (pixelsPerEm == 0)
        {
            return 0;
        }

        SfntDeviceTable? device = adjustment.GetDevice();
        System.ArgumentNullException.ThrowIfNull(device);
        return (double)device.GetDelta(pixelsPerEm) *
            unitsPerEm / pixelsPerEm;
    }
}

internal static class SfntGposAdjustmentValidator
{
    internal static void ValidateAnchorVariationIndexes(
        SfntLayoutAnchor anchor,
        SfntItemVariationStore? variationStore)
    {
        if (anchor == null)
        {
            throw new System.ArgumentNullException("anchor");
        }

        ValidateAdjustment(anchor.GetXAdjustment(), variationStore);
        ValidateAdjustment(anchor.GetYAdjustment(), variationStore);
    }

    static void ValidateAdjustment(
        SfntDeviceOrVariationIndex? adjustment,
        SfntItemVariationStore? variationStore)
    {
        if (adjustment == null || !adjustment.IsVariationIndex())
        {
            return;
        }

        if (variationStore == null)
        {
            throw FontFormatException.Create(
                "A GPOS anchor VariationIndex requires a GDEF ItemVariationStore.");
        }

        SfntVariationIndex? variationIndex = adjustment.GetVariationIndex();
        System.ArgumentNullException.ThrowIfNull(variationIndex);
        variationStore.ValidateDeltaSetIndex(
            variationIndex.GetDeltaSetIndex());
    }
}

internal class SfntGposValueRecordParserContext
{
    FontDataSlice tableData;
    FontLimits limits;

    internal SfntGposValueRecordParserContext(
        FontDataSlice tableData,
        FontLimits limits)
    {
        this.tableData = tableData;
        this.limits = limits;
    }

    internal SfntGposValueRecord Parse(
        int recordOffset,
        int valueFormat,
        int parentOffset,
        int minimumAdjustmentOffset)
    {
        int recordSize = SfntGposValueRecordParser.GetRecordSize(
            valueFormat);
        BigEndianReader reader = CreateReader(
            recordOffset,
            recordSize,
            "A GPOS ValueRecord is truncated.");
        int xPlacement = ReadValue(reader, valueFormat, 0x0001);
        int yPlacement = ReadValue(reader, valueFormat, 0x0002);
        int xAdvance = ReadValue(reader, valueFormat, 0x0004);
        int yAdvance = ReadValue(reader, valueFormat, 0x0008);
        int xPlacementOffset = ReadOffset(reader, valueFormat, 0x0010);
        int yPlacementOffset = ReadOffset(reader, valueFormat, 0x0020);
        int xAdvanceOffset = ReadOffset(reader, valueFormat, 0x0040);
        int yAdvanceOffset = ReadOffset(reader, valueFormat, 0x0080);
        int xPlacementTableOffset = ResolveAdjustmentOffset(
            parentOffset,
            xPlacementOffset,
            minimumAdjustmentOffset);
        int yPlacementTableOffset = ResolveAdjustmentOffset(
            parentOffset,
            yPlacementOffset,
            minimumAdjustmentOffset);
        int xAdvanceTableOffset = ResolveAdjustmentOffset(
            parentOffset,
            xAdvanceOffset,
            minimumAdjustmentOffset);
        int yAdvanceTableOffset = ResolveAdjustmentOffset(
            parentOffset,
            yAdvanceOffset,
            minimumAdjustmentOffset);
        return new SfntGposValueRecord(
            valueFormat,
            xPlacement,
            yPlacement,
            xAdvance,
            yAdvance,
            xPlacementOffset,
            yPlacementOffset,
            xAdvanceOffset,
            yAdvanceOffset,
            xPlacementTableOffset,
            yPlacementTableOffset,
            xAdvanceTableOffset,
            yAdvanceTableOffset,
            ParseAdjustment(xPlacementOffset, xPlacementTableOffset),
            ParseAdjustment(yPlacementOffset, yPlacementTableOffset),
            ParseAdjustment(xAdvanceOffset, xAdvanceTableOffset),
            ParseAdjustment(yAdvanceOffset, yAdvanceTableOffset));
    }

    static int ReadValue(
        BigEndianReader reader,
        int valueFormat,
        int flag)
    {
        return (valueFormat & flag) == 0 ? 0 : reader.ReadInt16();
    }

    static int ReadOffset(
        BigEndianReader reader,
        int valueFormat,
        int flag)
    {
        return (valueFormat & flag) == 0 ? 0 : reader.ReadUInt16();
    }

    int ResolveAdjustmentOffset(
        int parentOffset,
        int relativeOffset,
        int minimumRelativeOffset)
    {
        if (relativeOffset == 0)
        {
            return 0;
        }

        if (relativeOffset < minimumRelativeOffset)
        {
            throw FontFormatException.Create(
                "A GPOS ValueRecord adjustment offset is invalid.");
        }

        int absoluteOffset = CheckedFontMath.AddOffset(
            parentOffset,
            relativeOffset);

        if (absoluteOffset > tableData.GetLength() - 6)
        {
            throw FontFormatException.Create(
                "A GPOS ValueRecord adjustment offset is invalid.");
        }

        return absoluteOffset;
    }

    SfntDeviceOrVariationIndex? ParseAdjustment(
        int relativeOffset,
        int tableOffset)
    {
        return relativeOffset == 0 ? null :
            SfntLayoutPrimitiveParser.ParseDeviceOrVariationIndex(
                tableData,
                tableOffset,
                limits);
    }

    BigEndianReader CreateReader(
        int offset,
        int requiredLength,
        string message)
    {
        if (offset < 0 ||
            requiredLength < 0 ||
            offset > tableData.GetLength() - requiredLength)
        {
            throw FontFormatException.Create(message);
        }

        return new BigEndianReader(tableData.GetSlice(
            offset,
            tableData.GetLength() - offset));
    }
}

internal static class SfntGposValueRecordParser
{
    internal static int GetRecordSize(int valueFormat)
    {
        ValidateValueFormat(valueFormat);
        int size = 0;
        int flag = 1;

        while (flag <= 0x0080)
        {
            if ((valueFormat & flag) != 0)
            {
                size += 2;
            }

            flag <<= 1;
        }

        return size;
    }

    internal static SfntGposValueRecord Parse(
        FontDataSlice tableData,
        int recordOffset,
        int valueFormat,
        int parentOffset,
        int minimumAdjustmentOffset,
        FontLimits limits)
    {
        if (tableData == null)
        {
            throw new System.ArgumentNullException("tableData");
        }

        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        if (parentOffset < 0 || parentOffset > tableData.GetLength())
        {
            throw new System.ArgumentOutOfRangeException("parentOffset");
        }

        if (minimumAdjustmentOffset < 0)
        {
            throw new System.ArgumentOutOfRangeException(
                "minimumAdjustmentOffset");
        }

        ValidateValueFormat(valueFormat);
        return new SfntGposValueRecordParserContext(
            tableData,
            limits).Parse(
                recordOffset,
                valueFormat,
                parentOffset,
                minimumAdjustmentOffset);
    }

    static void ValidateValueFormat(int valueFormat)
    {
        if (valueFormat < 0 || valueFormat > 0xFFFF)
        {
            throw new System.ArgumentOutOfRangeException("valueFormat");
        }

        if ((valueFormat & 0xFF00) != 0)
        {
            throw FontFormatException.Create(
                "A GPOS ValueFormat has nonzero reserved bits.");
        }
    }
}
