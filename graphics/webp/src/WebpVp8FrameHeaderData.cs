using System;

namespace webp;

internal class WebpVp8SegmentationHeader
{
    internal bool Enabled;
    internal bool UpdateMap;
    internal bool UpdateData;
    internal bool AbsoluteSegmentFeatureMode;
    internal int[] QuantizerDeltas;
    internal int[] LoopFilterDeltas;
    internal byte[] SegmentTreeProbabilities;

    internal WebpVp8SegmentationHeader()
    {
        Enabled = false;
        UpdateMap = false;
        UpdateData = false;
        AbsoluteSegmentFeatureMode = false;
        QuantizerDeltas = new int[4];
        LoopFilterDeltas = new int[4];
        SegmentTreeProbabilities = new byte[] { 255, 255, 255 };
    }
}

internal class WebpVp8LoopFilterHeader
{
    internal bool UseSimpleFilter;
    internal int Level;
    internal int Sharpness;
    internal bool DeltaEnabled;
    internal bool DeltaUpdate;
    internal bool[] ReferenceDeltaUpdates;
    internal int[] ReferenceDeltas;
    internal bool[] ModeDeltaUpdates;
    internal int[] ModeDeltas;

    internal WebpVp8LoopFilterHeader()
    {
        UseSimpleFilter = false;
        Level = 0;
        Sharpness = 0;
        DeltaEnabled = false;
        DeltaUpdate = false;
        ReferenceDeltaUpdates = new bool[4];
        ReferenceDeltas = new int[4];
        ModeDeltaUpdates = new bool[4];
        ModeDeltas = new int[4];
    }
}

internal class WebpVp8QuantizerHeader
{
    internal int BaseQIndex;
    internal bool YDcDeltaPresent;
    internal int YDcDelta;
    internal bool Y2DcDeltaPresent;
    internal int Y2DcDelta;
    internal bool Y2AcDeltaPresent;
    internal int Y2AcDelta;
    internal bool UvDcDeltaPresent;
    internal int UvDcDelta;
    internal bool UvAcDeltaPresent;
    internal int UvAcDelta;
    internal bool DeltaUpdate;

    internal WebpVp8QuantizerHeader()
    {
        BaseQIndex = 0;
        YDcDeltaPresent = false;
        YDcDelta = 0;
        Y2DcDeltaPresent = false;
        Y2DcDelta = 0;
        Y2AcDeltaPresent = false;
        Y2AcDelta = 0;
        UvDcDeltaPresent = false;
        UvDcDelta = 0;
        UvAcDeltaPresent = false;
        UvAcDelta = 0;
        DeltaUpdate = false;
    }
}

internal class WebpVp8TokenPartitionHeader
{
    internal int Log2PartitionCount;
    internal int PartitionCount;
    internal int PartitionSizeHeaderBytes;
    internal int TokenDataOffset;
    internal int[] PartitionOffsets;
    internal int[] PartitionSizes;

    internal WebpVp8TokenPartitionHeader()
    {
        Log2PartitionCount = 0;
        PartitionCount = 1;
        PartitionSizeHeaderBytes = 0;
        TokenDataOffset = 0;
        PartitionOffsets = Array.Empty<int>();
        PartitionSizes = Array.Empty<int>();
    }
}

internal class WebpVp8FrameHeaderData
{
    internal int ColorSpace;
    internal int PixelClampingType;
    internal WebpVp8SegmentationHeader Segmentation;
    internal WebpVp8LoopFilterHeader LoopFilter;
    internal WebpVp8TokenPartitionHeader TokenPartitions;
    internal WebpVp8QuantizerHeader Quantizer;

    internal WebpVp8FrameHeaderData()
    {
        ColorSpace = 0;
        PixelClampingType = 0;
        Segmentation = new WebpVp8SegmentationHeader();
        LoopFilter = new WebpVp8LoopFilterHeader();
        TokenPartitions = new WebpVp8TokenPartitionHeader();
        Quantizer = new WebpVp8QuantizerHeader();
    }
}
