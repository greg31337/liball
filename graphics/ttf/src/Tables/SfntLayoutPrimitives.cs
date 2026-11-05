namespace ttf;

internal class SfntDeviceTable
{
    int startSize;
    int endSize;
    int deltaFormat;
    int[] packedValues;

    internal SfntDeviceTable(
        int startSize,
        int endSize,
        int deltaFormat,
        int[] packedValues)
    {
        this.startSize = startSize;
        this.endSize = endSize;
        this.deltaFormat = deltaFormat;
        this.packedValues = packedValues;
    }

    internal int GetStartSize()
    {
        return startSize;
    }

    internal int GetEndSize()
    {
        return endSize;
    }

    internal int GetDeltaFormat()
    {
        return deltaFormat;
    }

    internal int GetPackedValueCount()
    {
        return packedValues.Length;
    }

    internal int GetPackedValue(int index)
    {
        return packedValues[index];
    }

    internal int GetDelta(int pixelsPerEm)
    {
        if (pixelsPerEm < startSize || pixelsPerEm > endSize)
        {
            return 0;
        }

        int bitCount = 1 << deltaFormat;
        int valuesPerWord = 16 / bitCount;
        int valueIndex = pixelsPerEm - startSize;
        int word = packedValues[valueIndex / valuesPerWord];
        int position = valueIndex % valuesPerWord;
        int shift = 16 - bitCount * (position + 1);
        int mask = (1 << bitCount) - 1;
        int encoded = word >> shift & mask;
        int signBit = 1 << (bitCount - 1);
        return (encoded & signBit) == 0 ?
            encoded : encoded - (1 << bitCount);
    }
}

internal class SfntDeviceOrVariationIndex
{
    SfntDeviceTable? device;
    SfntVariationIndex? variationIndex;

    internal SfntDeviceOrVariationIndex(
        SfntDeviceTable? device,
        SfntVariationIndex? variationIndex)
    {
        if ((device == null) == (variationIndex == null))
        {
            throw new System.ArgumentException(
                "Exactly one device or variation index is required.");
        }

        this.device = device;
        this.variationIndex = variationIndex;
    }

    internal static SfntDeviceOrVariationIndex Create(SfntDeviceTable device)
    {
        if (device == null)
        {
            throw new System.ArgumentNullException("device");
        }

        return new SfntDeviceOrVariationIndex(device, null);
    }

    internal static SfntDeviceOrVariationIndex Create(
        SfntVariationIndex variationIndex)
    {
        if (variationIndex == null)
        {
            throw new System.ArgumentNullException("variationIndex");
        }

        return new SfntDeviceOrVariationIndex(null, variationIndex);
    }

    internal bool IsDevice()
    {
        return device != null;
    }

    internal bool IsVariationIndex()
    {
        return variationIndex != null;
    }

    internal SfntDeviceTable? GetDevice()
    {
        return device;
    }

    internal SfntVariationIndex? GetVariationIndex()
    {
        return variationIndex;
    }
}

internal class SfntLayoutAnchor
{
    int format;
    int xCoordinate;
    int yCoordinate;
    int anchorPoint;
    int xAdjustmentOffset;
    int yAdjustmentOffset;
    int xAdjustmentTableOffset;
    int yAdjustmentTableOffset;
    SfntDeviceOrVariationIndex? xAdjustment;
    SfntDeviceOrVariationIndex? yAdjustment;

    internal SfntLayoutAnchor(
        int format,
        int xCoordinate,
        int yCoordinate,
        int anchorPoint,
        int xAdjustmentOffset,
        int yAdjustmentOffset,
        int xAdjustmentTableOffset,
        int yAdjustmentTableOffset,
        SfntDeviceOrVariationIndex? xAdjustment,
        SfntDeviceOrVariationIndex? yAdjustment)
    {
        this.format = format;
        this.xCoordinate = xCoordinate;
        this.yCoordinate = yCoordinate;
        this.anchorPoint = anchorPoint;
        this.xAdjustmentOffset = xAdjustmentOffset;
        this.yAdjustmentOffset = yAdjustmentOffset;
        this.xAdjustmentTableOffset = xAdjustmentTableOffset;
        this.yAdjustmentTableOffset = yAdjustmentTableOffset;
        this.xAdjustment = xAdjustment;
        this.yAdjustment = yAdjustment;
    }

    internal int GetFormat()
    {
        return format;
    }

    internal int GetXCoordinate()
    {
        return xCoordinate;
    }

    internal int GetYCoordinate()
    {
        return yCoordinate;
    }

    internal bool HasAnchorPoint()
    {
        return format == 2;
    }

    internal int GetAnchorPoint()
    {
        return anchorPoint;
    }

    internal int GetXAdjustmentOffset()
    {
        return xAdjustmentOffset;
    }

    internal int GetYAdjustmentOffset()
    {
        return yAdjustmentOffset;
    }

    internal int GetXAdjustmentTableOffset()
    {
        return xAdjustmentTableOffset;
    }

    internal int GetYAdjustmentTableOffset()
    {
        return yAdjustmentTableOffset;
    }

    internal SfntDeviceOrVariationIndex? GetXAdjustment()
    {
        return xAdjustment;
    }

    internal SfntDeviceOrVariationIndex? GetYAdjustment()
    {
        return yAdjustment;
    }
}

internal class SfntMarkRecord
{
    int markClass;
    int anchorOffset;
    int anchorTableOffset;
    SfntLayoutAnchor anchor;

    internal SfntMarkRecord(
        int markClass,
        int anchorOffset,
        int anchorTableOffset,
        SfntLayoutAnchor anchor)
    {
        this.markClass = markClass;
        this.anchorOffset = anchorOffset;
        this.anchorTableOffset = anchorTableOffset;
        this.anchor = anchor;
    }

    internal int GetMarkClass()
    {
        return markClass;
    }

    internal int GetAnchorOffset()
    {
        return anchorOffset;
    }

    internal int GetAnchorTableOffset()
    {
        return anchorTableOffset;
    }

    internal SfntLayoutAnchor GetAnchor()
    {
        return anchor;
    }
}

internal class SfntMarkArray
{
    SfntMarkRecord[] records;

    internal SfntMarkArray(SfntMarkRecord[] records)
    {
        this.records = records;
    }

    internal int GetMarkCount()
    {
        return records.Length;
    }

    internal SfntMarkRecord GetMark(int index)
    {
        return records[index];
    }
}

internal class SfntSequenceLookupRecord
{
    int sequenceIndex;
    int lookupListIndex;

    internal SfntSequenceLookupRecord(
        int sequenceIndex,
        int lookupListIndex)
    {
        this.sequenceIndex = sequenceIndex;
        this.lookupListIndex = lookupListIndex;
    }

    internal int GetSequenceIndex()
    {
        return sequenceIndex;
    }

    internal int GetLookupListIndex()
    {
        return lookupListIndex;
    }
}

internal class SfntSequenceLookupRecords
{
    SfntSequenceLookupRecord[] records;

    internal SfntSequenceLookupRecords(
        SfntSequenceLookupRecord[] records)
    {
        this.records = records;
    }

    internal int GetRecordCount()
    {
        return records.Length;
    }

    internal SfntSequenceLookupRecord GetRecord(int index)
    {
        return records[index];
    }
}

internal class SfntExtensionLookup
{
    int lookupType;
    int extensionOffset;
    int extensionTableOffset;

    internal SfntExtensionLookup(
        int lookupType,
        int extensionOffset,
        int extensionTableOffset)
    {
        this.lookupType = lookupType;
        this.extensionOffset = extensionOffset;
        this.extensionTableOffset = extensionTableOffset;
    }

    internal int GetLookupType()
    {
        return lookupType;
    }

    internal int GetExtensionOffset()
    {
        return extensionOffset;
    }

    internal int GetExtensionTableOffset()
    {
        return extensionTableOffset;
    }
}
