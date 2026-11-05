using System;
using System.Collections.Generic;

namespace htmlgui;

internal class FixedVirtualRange
{
    internal UiRangeRequest Request = new UiRangeRequest();
    internal UiOrientation Orientation;
    internal int VisibleStart;
    internal int VisibleCount;
    internal double ViewportOffset;
    internal double ViewportExtent;
    internal double BeforeExtent;
    internal double AfterExtent;
    internal double TotalExtent;
}

internal class FixedVirtualAnchor
{
    internal int Index;
    internal string? Key;
    internal double ViewportOffset;
}

internal class VariableVirtualRange
{
    internal UiRangeRequest Request = new UiRangeRequest();
    internal UiOrientation Orientation;
    internal int VisibleStart;
    internal int VisibleCount;
    internal bool HasTotalExtent;
    internal double ViewportOffset;
    internal double ViewportExtent;
    internal double BeforeExtent;
    internal double AfterExtent;
    internal double TotalExtent;
}

internal class VariableVirtualAnchor
{
    internal int Index;
    internal string Key = string.Empty;
    internal double ViewportOffset;
}

internal class VariableMeasurementUpdate
{
    internal bool Accepted;
    internal bool IsResize;
    internal double PreviousExtent;
    internal double Extent;
    internal double CorrectionDelta;
    internal bool HasTotalExtent;
    internal double TotalExtent;
    internal UiMeasuredItem? Measurement;
}

internal class VirtualizationCore
{
    internal const string StartAlignment = "start";
    internal const string CenterAlignment = "center";
    internal const string EndAlignment = "end";
    internal const string NearestAlignment = "nearest";

    internal List<UiMeasuredItem> VariableMeasurements;

    internal VirtualizationCore()
    {
        VariableMeasurements = new List<UiMeasuredItem>();
    }

    internal FixedVirtualRange CalculateFixedViewportRange(UiVirtualization? virtualization, double scrollX, double scrollY, double viewportWidth, double viewportHeight)
    {
        UiVirtualization options = ValidateFixedVirtualization(virtualization);
        ValidateViewportMetrics(scrollX, scrollY, viewportWidth, viewportHeight);
        double viewportExtent = SelectViewportExtent(options.Orientation, viewportWidth, viewportHeight);
        double requestedOffset = SelectViewportOffset(options.Orientation, scrollX, scrollY);
        double totalExtent = CalculateFixedTotalExtent(options.TotalCount, options.ItemExtent);
        double viewportOffset = ClampFixedOffset(requestedOffset, totalExtent, viewportExtent);
        FixedVirtualRange result = new FixedVirtualRange();
        result.Orientation = options.Orientation;
        result.ViewportOffset = viewportOffset;
        result.ViewportExtent = viewportExtent;
        result.TotalExtent = totalExtent;
        if (options.TotalCount == 0 || viewportExtent == 0)
        {
            return result;
        }

        int visibleStart = CalculateFixedStartIndex(viewportOffset, options.ItemExtent, options.TotalCount);
        int visibleEnd = CalculateFixedEndIndex(viewportOffset, viewportExtent, options.ItemExtent, options.TotalCount);
        int rangeStart = visibleStart - options.Overscan;
        if (rangeStart < 0)
        {
            rangeStart = 0;
        }

        long requestedEnd = (long)visibleEnd + options.Overscan;
        int rangeEnd = requestedEnd > options.TotalCount ? options.TotalCount : (int)requestedEnd;
        result.VisibleStart = visibleStart;
        result.VisibleCount = visibleEnd - visibleStart;
        result.Request.Start = rangeStart;
        result.Request.Count = rangeEnd - rangeStart;
        result.BeforeExtent = rangeStart * options.ItemExtent;
        result.AfterExtent = (options.TotalCount - rangeEnd) * options.ItemExtent;
        return result;
    }

    internal UiScrollTarget CalculateFixedScrollTarget(UiVirtualization? virtualization, UiScrollTarget? target, double scrollX, double scrollY, double viewportWidth, double viewportHeight)
    {
        UiVirtualization options = ValidateFixedVirtualization(virtualization);
        ValidateViewportMetrics(scrollX, scrollY, viewportWidth, viewportHeight);
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (!target.HasIndex || target.Index < 0 || target.Index >= options.TotalCount)
        {
            throw new ArgumentOutOfRangeException("target", "The fixed scroll target index is outside the logical item range.");
        }

        ValidateFixedAlignment(target.Alignment);
        double viewportExtent = SelectViewportExtent(options.Orientation, viewportWidth, viewportHeight);
        double requestedOffset = SelectViewportOffset(options.Orientation, scrollX, scrollY);
        double totalExtent = CalculateFixedTotalExtent(options.TotalCount, options.ItemExtent);
        double currentOffset = ClampFixedOffset(requestedOffset, totalExtent, viewportExtent);
        double itemStart = target.Index * options.ItemExtent;
        double itemEnd = itemStart + options.ItemExtent;
        double destination = currentOffset;
        switch (target.Alignment)
        {
            case StartAlignment:
                destination = itemStart;
                break;
            case CenterAlignment:
                destination = itemStart - ((viewportExtent - options.ItemExtent) / 2.0);
                break;
            case EndAlignment:
                destination = itemEnd - viewportExtent;
                break;
            case NearestAlignment:
                destination = CalculateNearestFixedOffset(itemStart, itemEnd, currentOffset, viewportExtent);
                break;
        }

        return new UiScrollTarget
        {
            HasIndex = true,
            Index = target.Index,
            Key = target.Key,
            Alignment = target.Alignment,
            Offset = ClampFixedOffset(destination, totalExtent, viewportExtent)
        };
    }

    internal FixedVirtualAnchor CaptureFixedAnchor(UiVirtualization? virtualization, int index, string? key, double scrollX, double scrollY, double viewportWidth, double viewportHeight)
    {
        UiVirtualization options = ValidateFixedVirtualization(virtualization);
        ValidateViewportMetrics(scrollX, scrollY, viewportWidth, viewportHeight);
        if (index < 0 || index >= options.TotalCount)
        {
            throw new ArgumentOutOfRangeException("index", index, "The fixed anchor index is outside the logical item range.");
        }

        double viewportExtent = SelectViewportExtent(options.Orientation, viewportWidth, viewportHeight);
        double requestedOffset = SelectViewportOffset(options.Orientation, scrollX, scrollY);
        double totalExtent = CalculateFixedTotalExtent(options.TotalCount, options.ItemExtent);
        double currentOffset = ClampFixedOffset(requestedOffset, totalExtent, viewportExtent);
        return new FixedVirtualAnchor
        {
            Index = index,
            Key = string.IsNullOrWhiteSpace(key) ? null : key,
            ViewportOffset = (index * options.ItemExtent) - currentOffset
        };
    }

    internal UiScrollTarget RestoreFixedAnchor(UiVirtualization? virtualization, FixedVirtualAnchor? anchor, int index, double viewportWidth, double viewportHeight)
    {
        UiVirtualization options = ValidateFixedVirtualization(virtualization);
        ValidateViewportSize(viewportWidth, viewportHeight);
        if (anchor == null)
        {
            throw new ArgumentNullException("anchor");
        }

        if (index < 0 || index >= options.TotalCount)
        {
            throw new ArgumentOutOfRangeException("index", index, "The restored fixed anchor index is outside the logical item range.");
        }

        if (!double.IsFinite(anchor.ViewportOffset))
        {
            throw new ArgumentOutOfRangeException("anchor", "The fixed anchor viewport offset must be finite.");
        }

        double viewportExtent = SelectViewportExtent(options.Orientation, viewportWidth, viewportHeight);
        double totalExtent = CalculateFixedTotalExtent(options.TotalCount, options.ItemExtent);
        double destination = (index * options.ItemExtent) - anchor.ViewportOffset;
        return new UiScrollTarget
        {
            HasIndex = true,
            Index = index,
            Key = anchor.Key,
            Alignment = NearestAlignment,
            Offset = ClampFixedOffset(destination, totalExtent, viewportExtent)
        };
    }

    internal VariableMeasurementUpdate ApplyVariableMeasurement(UiVirtualization? virtualization, UiMeasuredItem? measurement, int anchorIndex)
    {
        UiVirtualization options = ValidateVariableVirtualization(virtualization);
        if (measurement == null)
        {
            throw new ArgumentNullException("measurement");
        }

        if (string.IsNullOrWhiteSpace(measurement.Key))
        {
            throw new ArgumentException("Variable measurements require a stable item key.", "measurement");
        }

        if (measurement.Index < 0 || (options.HasTotalCount && measurement.Index >= options.TotalCount))
        {
            throw new ArgumentOutOfRangeException("measurement", "The variable measurement index is outside the logical item range.");
        }

        if (!double.IsFinite(measurement.Extent) || measurement.Extent <= 0 || measurement.Generation < 0)
        {
            throw new ArgumentOutOfRangeException("measurement", "Variable measurements require a positive finite extent and nonnegative generation.");
        }

        if (anchorIndex < 0 || (options.HasTotalCount && anchorIndex > options.TotalCount))
        {
            throw new ArgumentOutOfRangeException("anchorIndex", anchorIndex, "The variable anchor index is outside the logical item range.");
        }

        int keyIndex = FindVariableMeasurementKeyIndex(measurement.Key);
        int logicalIndex = FindVariableMeasurementLogicalIndex(measurement.Index);
        UiMeasuredItem? existing = keyIndex < 0 ? null : VariableMeasurements[keyIndex];
        UiMeasuredItem? displaced = logicalIndex < 0 ? null : VariableMeasurements[logicalIndex];
        int latestGeneration = existing == null ? -1 : existing.Generation;
        if (displaced != null && displaced.Generation > latestGeneration)
        {
            latestGeneration = displaced.Generation;
        }

        if (measurement.Generation < latestGeneration)
        {
            VariableMeasurementUpdate stale = CreateVariableMeasurementUpdate(options);
            stale.Measurement = existing == null ? null : CopyVariableMeasurement(existing);
            stale.Extent = existing == null ? options.ItemExtent : existing.Extent;
            stale.PreviousExtent = stale.Extent;
            return stale;
        }

        bool unchanged = existing != null
            && existing.Index == measurement.Index
            && existing.Generation == measurement.Generation
            && existing.Extent == measurement.Extent;
        if (unchanged)
        {
            VariableMeasurementUpdate repeated = CreateVariableMeasurementUpdate(options);
            repeated.Measurement = CopyVariableMeasurement(measurement);
            repeated.Extent = measurement.Extent;
            repeated.PreviousExtent = measurement.Extent;
            return repeated;
        }

        double previousExtent = existing == null ? options.ItemExtent : existing.Extent;
        bool isResize = existing != null && existing.Index == measurement.Index;
        if (keyIndex >= 0)
        {
            VariableMeasurements.RemoveAt(keyIndex);
        }

        int collisionIndex = FindVariableMeasurementLogicalIndex(measurement.Index);
        if (collisionIndex >= 0)
        {
            VariableMeasurements.RemoveAt(collisionIndex);
        }

        UiMeasuredItem accepted = CopyVariableMeasurement(measurement);
        VariableMeasurements.Add(accepted);
        SortVariableMeasurements();
        VariableMeasurementUpdate result = CreateVariableMeasurementUpdate(options);
        result.Accepted = true;
        result.IsResize = isResize;
        result.PreviousExtent = previousExtent;
        result.Extent = accepted.Extent;
        if (accepted.Index < anchorIndex)
        {
            result.CorrectionDelta = accepted.Extent - previousExtent;
        }

        result.Measurement = CopyVariableMeasurement(accepted);
        if (options.HasTotalCount)
        {
            result.TotalExtent = CalculateVariableTotalExtent(options.TotalCount, options.ItemExtent);
        }

        return result;
    }

    internal UiMeasuredItem[] ReadVariableMeasurements()
    {
        UiMeasuredItem[] result = new UiMeasuredItem[VariableMeasurements.Count];
        int index = 0;
        while (index < VariableMeasurements.Count)
        {
            result[index] = CopyVariableMeasurement(VariableMeasurements[index]);
            index++;
        }

        return result;
    }

    internal VariableVirtualRange CalculateVariableViewportRange(UiVirtualization? virtualization, double scrollX, double scrollY, double viewportWidth, double viewportHeight)
    {
        UiVirtualization options = ValidateVariableVirtualization(virtualization);
        ValidateViewportMetrics(scrollX, scrollY, viewportWidth, viewportHeight);
        double viewportExtent = SelectViewportExtent(options.Orientation, viewportWidth, viewportHeight);
        double requestedOffset = SelectViewportOffset(options.Orientation, scrollX, scrollY);
        double totalExtent = 0;
        double viewportOffset = requestedOffset < 0 ? 0 : requestedOffset;
        if (options.HasTotalCount)
        {
            totalExtent = CalculateVariableTotalExtent(options.TotalCount, options.ItemExtent);
            viewportOffset = ClampFixedOffset(requestedOffset, totalExtent, viewportExtent);
        }

        VariableVirtualRange result = new VariableVirtualRange();
        result.Orientation = options.Orientation;
        result.HasTotalExtent = options.HasTotalCount;
        result.ViewportOffset = viewportOffset;
        result.ViewportExtent = viewportExtent;
        result.TotalExtent = totalExtent;
        if ((options.HasTotalCount && options.TotalCount == 0) || viewportExtent == 0)
        {
            return result;
        }

        double viewportEnd = viewportOffset + viewportExtent;
        if (!double.IsFinite(viewportEnd))
        {
            throw new ArgumentOutOfRangeException("viewportExtent", "The variable viewport end is too large.");
        }

        int searchLimit = options.HasTotalCount
            ? options.TotalCount
            : CalculateUnknownVariableSearchLimit(viewportEnd, options.ItemExtent, options.Overscan);
        int visibleStart = FindVariableVisibleStart(viewportOffset, searchLimit, options.ItemExtent);
        int visibleEnd = FindVariableVisibleEnd(viewportEnd, searchLimit, options.ItemExtent);
        int rangeStart = visibleStart - options.Overscan;
        if (rangeStart < 0)
        {
            rangeStart = 0;
        }

        long requestedEnd = (long)visibleEnd + options.Overscan;
        int rangeEnd = requestedEnd > searchLimit ? searchLimit : (int)requestedEnd;
        result.VisibleStart = visibleStart;
        result.VisibleCount = visibleEnd - visibleStart;
        result.Request.Start = rangeStart;
        result.Request.Count = rangeEnd - rangeStart;
        result.BeforeExtent = CalculateVariableItemOffset(rangeStart, options.ItemExtent);
        if (options.HasTotalCount)
        {
            result.AfterExtent = totalExtent - CalculateVariableItemOffset(rangeEnd, options.ItemExtent);
        }

        return result;
    }

    internal VariableVirtualAnchor CaptureVariableAnchor(UiVirtualization? virtualization, int index, string? key, double scrollX, double scrollY, double viewportWidth, double viewportHeight)
    {
        UiVirtualization options = ValidateVariableVirtualization(virtualization);
        ValidateViewportMetrics(scrollX, scrollY, viewportWidth, viewportHeight);
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Variable anchors require a stable item key.", "key");
        }

        if (index < 0 || (options.HasTotalCount && index >= options.TotalCount))
        {
            throw new ArgumentOutOfRangeException("index", index, "The variable anchor index is outside the logical item range.");
        }

        double viewportExtent = SelectViewportExtent(options.Orientation, viewportWidth, viewportHeight);
        double requestedOffset = SelectViewportOffset(options.Orientation, scrollX, scrollY);
        double currentOffset = requestedOffset < 0 ? 0 : requestedOffset;
        if (options.HasTotalCount)
        {
            double totalExtent = CalculateVariableTotalExtent(options.TotalCount, options.ItemExtent);
            currentOffset = ClampFixedOffset(requestedOffset, totalExtent, viewportExtent);
        }

        return new VariableVirtualAnchor
        {
            Index = index,
            Key = key,
            ViewportOffset = CalculateVariableItemOffset(index, options.ItemExtent) - currentOffset
        };
    }

    internal UiScrollTarget RestoreVariableAnchor(UiVirtualization? virtualization, VariableVirtualAnchor? anchor, double viewportWidth, double viewportHeight)
    {
        UiVirtualization options = ValidateVariableVirtualization(virtualization);
        ValidateViewportSize(viewportWidth, viewportHeight);
        if (anchor == null)
        {
            throw new ArgumentNullException("anchor");
        }

        if (string.IsNullOrWhiteSpace(anchor.Key) || !double.IsFinite(anchor.ViewportOffset))
        {
            throw new ArgumentException("The variable anchor is invalid.", "anchor");
        }

        int restoredIndex = anchor.Index;
        int measurementIndex = FindVariableMeasurementKeyIndex(anchor.Key);
        if (measurementIndex >= 0)
        {
            restoredIndex = VariableMeasurements[measurementIndex].Index;
        }

        if (restoredIndex < 0 || (options.HasTotalCount && restoredIndex >= options.TotalCount))
        {
            throw new ArgumentOutOfRangeException("anchor", "The variable anchor index is outside the logical item range.");
        }

        double viewportExtent = SelectViewportExtent(options.Orientation, viewportWidth, viewportHeight);
        double destination = CalculateVariableItemOffset(restoredIndex, options.ItemExtent) - anchor.ViewportOffset;
        if (options.HasTotalCount)
        {
            double totalExtent = CalculateVariableTotalExtent(options.TotalCount, options.ItemExtent);
            destination = ClampFixedOffset(destination, totalExtent, viewportExtent);
        }
        else if (destination < 0)
        {
            destination = 0;
        }

        return new UiScrollTarget
        {
            HasIndex = true,
            Index = restoredIndex,
            Key = anchor.Key,
            Alignment = NearestAlignment,
            Offset = destination
        };
    }

    internal UiScrollTarget CalculateVariableScrollTarget(UiVirtualization? virtualization, UiScrollTarget? target, double scrollX, double scrollY, double viewportWidth, double viewportHeight)
    {
        UiVirtualization options = ValidateVariableVirtualization(virtualization);
        ValidateViewportMetrics(scrollX, scrollY, viewportWidth, viewportHeight);
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (!target.HasIndex || target.Index < 0 || (options.HasTotalCount && target.Index >= options.TotalCount))
        {
            throw new ArgumentOutOfRangeException("target", "The variable scroll target index is outside the logical item range.");
        }

        ValidateFixedAlignment(target.Alignment);
        double viewportExtent = SelectViewportExtent(options.Orientation, viewportWidth, viewportHeight);
        double requestedOffset = SelectViewportOffset(options.Orientation, scrollX, scrollY);
        double currentOffset = requestedOffset < 0 ? 0 : requestedOffset;
        if (options.HasTotalCount)
        {
            currentOffset = ClampFixedOffset(requestedOffset, CalculateVariableTotalExtent(options.TotalCount, options.ItemExtent), viewportExtent);
        }

        double itemStart = CalculateVariableItemOffset(target.Index, options.ItemExtent);
        double itemExtent = ReadVariableItemExtent(target.Index, options.ItemExtent);
        double itemEnd = itemStart + itemExtent;
        double destination = currentOffset;
        switch (target.Alignment)
        {
            case StartAlignment:
                destination = itemStart;
                break;
            case CenterAlignment:
                destination = itemStart - ((viewportExtent - itemExtent) / 2.0);
                break;
            case EndAlignment:
                destination = itemEnd - viewportExtent;
                break;
            case NearestAlignment:
                destination = CalculateNearestFixedOffset(itemStart, itemEnd, currentOffset, viewportExtent);
                break;
        }

        if (options.HasTotalCount)
        {
            destination = ClampFixedOffset(destination, CalculateVariableTotalExtent(options.TotalCount, options.ItemExtent), viewportExtent);
        }
        else if (destination < 0)
        {
            destination = 0;
        }

        return new UiScrollTarget
        {
            HasIndex = true,
            Index = target.Index,
            Key = target.Key,
            Alignment = target.Alignment,
            Offset = destination
        };
    }

    internal UiVirtualization ValidateVariableVirtualization(UiVirtualization? virtualization)
    {
        if (virtualization == null)
        {
            throw new ArgumentNullException("virtualization");
        }

        if (virtualization.Orientation != UiOrientation.Horizontal && virtualization.Orientation != UiOrientation.Vertical)
        {
            throw new ArgumentOutOfRangeException("virtualization", "The virtualization orientation is invalid.");
        }

        if (!double.IsFinite(virtualization.ItemExtent) || virtualization.ItemExtent <= 0)
        {
            throw new ArgumentOutOfRangeException("virtualization", "Variable virtualization requires a positive finite estimated item extent.");
        }

        if (virtualization.Overscan < 0 || (virtualization.HasTotalCount && virtualization.TotalCount < 0))
        {
            throw new ArgumentOutOfRangeException("virtualization", "Variable virtualization requires nonnegative overscan and total count values.");
        }

        CalculateFixedTotalExtent(int.MaxValue, virtualization.ItemExtent);
        return virtualization;
    }

    internal VariableMeasurementUpdate CreateVariableMeasurementUpdate(UiVirtualization options)
    {
        return new VariableMeasurementUpdate
        {
            HasTotalExtent = options.HasTotalCount,
            TotalExtent = options.HasTotalCount ? CalculateVariableTotalExtent(options.TotalCount, options.ItemExtent) : 0
        };
    }

    internal UiMeasuredItem CopyVariableMeasurement(UiMeasuredItem source)
    {
        return new UiMeasuredItem
        {
            Key = source.Key,
            Index = source.Index,
            Extent = source.Extent,
            Generation = source.Generation
        };
    }

    internal int FindVariableMeasurementKeyIndex(string key)
    {
        int index = 0;
        while (index < VariableMeasurements.Count)
        {
            if (string.Equals(VariableMeasurements[index].Key, key, StringComparison.Ordinal))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal int FindVariableMeasurementLogicalIndex(int logicalIndex)
    {
        int index = 0;
        while (index < VariableMeasurements.Count)
        {
            if (VariableMeasurements[index].Index == logicalIndex)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal void SortVariableMeasurements()
    {
        int index = 1;
        while (index < VariableMeasurements.Count)
        {
            UiMeasuredItem candidate = VariableMeasurements[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 && CompareVariableMeasurements(VariableMeasurements[insertionIndex], candidate) > 0)
            {
                VariableMeasurements[insertionIndex + 1] = VariableMeasurements[insertionIndex];
                insertionIndex--;
            }

            VariableMeasurements[insertionIndex + 1] = candidate;
            index++;
        }
    }

    internal int CompareVariableMeasurements(UiMeasuredItem left, UiMeasuredItem right)
    {
        int comparison = left.Index.CompareTo(right.Index);
        if (comparison != 0)
        {
            return comparison;
        }

        return string.CompareOrdinal(left.Key, right.Key);
    }

    internal double CalculateVariableItemOffset(int index, double estimatedExtent)
    {
        double offset = index * estimatedExtent;
        int measurementIndex = 0;
        while (measurementIndex < VariableMeasurements.Count)
        {
            UiMeasuredItem measurement = VariableMeasurements[measurementIndex];
            if (measurement.Index >= index)
            {
                break;
            }

            offset += measurement.Extent - estimatedExtent;
            measurementIndex++;
        }

        if (!double.IsFinite(offset))
        {
            throw new ArgumentOutOfRangeException("virtualization", "The variable virtualization offset is too large.");
        }

        return offset;
    }

    internal double ReadVariableItemExtent(int index, double estimatedExtent)
    {
        int measurementIndex = FindVariableMeasurementLogicalIndex(index);
        return measurementIndex < 0 ? estimatedExtent : VariableMeasurements[measurementIndex].Extent;
    }

    internal double CalculateVariableTotalExtent(int totalCount, double estimatedExtent)
    {
        return CalculateVariableItemOffset(totalCount, estimatedExtent);
    }

    internal int CalculateUnknownVariableSearchLimit(double viewportEnd, double estimatedExtent, int overscan)
    {
        double approximate = Math.Ceiling(viewportEnd / estimatedExtent);
        if (approximate >= int.MaxValue)
        {
            return int.MaxValue;
        }

        long limit = (long)approximate + VariableMeasurements.Count + overscan + 2L;
        return limit > int.MaxValue ? int.MaxValue : (int)limit;
    }

    internal int FindVariableVisibleStart(double viewportOffset, int searchLimit, double estimatedExtent)
    {
        int low = 0;
        int high = searchLimit;
        while (low < high)
        {
            int middle = low + ((high - low) / 2);
            double itemEnd = CalculateVariableItemOffset(middle, estimatedExtent) + ReadVariableItemExtent(middle, estimatedExtent);
            if (itemEnd <= viewportOffset)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    internal int FindVariableVisibleEnd(double viewportEnd, int searchLimit, double estimatedExtent)
    {
        int low = 0;
        int high = searchLimit;
        while (low < high)
        {
            int middle = low + ((high - low) / 2);
            double itemStart = CalculateVariableItemOffset(middle, estimatedExtent);
            if (itemStart < viewportEnd)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    internal UiVirtualization ValidateFixedVirtualization(UiVirtualization? virtualization)
    {
        if (virtualization == null)
        {
            throw new ArgumentNullException("virtualization");
        }

        if (virtualization.Orientation != UiOrientation.Horizontal && virtualization.Orientation != UiOrientation.Vertical)
        {
            throw new ArgumentOutOfRangeException("virtualization", "The virtualization orientation is invalid.");
        }

        if (!virtualization.HasItemExtent || !double.IsFinite(virtualization.ItemExtent) || virtualization.ItemExtent <= 0)
        {
            throw new ArgumentOutOfRangeException("virtualization", "Fixed virtualization requires a positive finite item extent.");
        }

        if (!virtualization.HasTotalCount || virtualization.TotalCount < 0)
        {
            throw new ArgumentOutOfRangeException("virtualization", "Fixed virtualization requires a nonnegative total count.");
        }

        if (virtualization.Overscan < 0)
        {
            throw new ArgumentOutOfRangeException("virtualization", "The virtualization overscan cannot be negative.");
        }

        CalculateFixedTotalExtent(virtualization.TotalCount, virtualization.ItemExtent);
        return virtualization;
    }

    internal void ValidateViewportMetrics(double scrollX, double scrollY, double viewportWidth, double viewportHeight)
    {
        if (!double.IsFinite(scrollX) || !double.IsFinite(scrollY))
        {
            throw new ArgumentOutOfRangeException("scrollOffset", "Viewport scroll offsets must be finite.");
        }

        ValidateViewportSize(viewportWidth, viewportHeight);
    }

    internal void ValidateViewportSize(double viewportWidth, double viewportHeight)
    {
        if (!double.IsFinite(viewportWidth) || viewportWidth < 0 || !double.IsFinite(viewportHeight) || viewportHeight < 0)
        {
            throw new ArgumentOutOfRangeException("viewportExtent", "Viewport extents must be finite and nonnegative.");
        }
    }

    internal double SelectViewportOffset(UiOrientation orientation, double scrollX, double scrollY)
    {
        return orientation == UiOrientation.Horizontal ? scrollX : scrollY;
    }

    internal double SelectViewportExtent(UiOrientation orientation, double viewportWidth, double viewportHeight)
    {
        return orientation == UiOrientation.Horizontal ? viewportWidth : viewportHeight;
    }

    internal double CalculateFixedTotalExtent(int totalCount, double itemExtent)
    {
        double totalExtent = totalCount * itemExtent;
        if (!double.IsFinite(totalExtent))
        {
            throw new ArgumentOutOfRangeException("virtualization", "The fixed virtualization total extent is too large.");
        }

        return totalExtent;
    }

    internal double ClampFixedOffset(double offset, double totalExtent, double viewportExtent)
    {
        double maximum = totalExtent - viewportExtent;
        if (maximum < 0)
        {
            maximum = 0;
        }

        if (offset < 0)
        {
            return 0;
        }

        if (offset > maximum)
        {
            return maximum;
        }

        return offset;
    }

    internal int CalculateFixedStartIndex(double viewportOffset, double itemExtent, int totalCount)
    {
        double index = Math.Floor(viewportOffset / itemExtent);
        if (index >= totalCount)
        {
            return totalCount - 1;
        }

        return (int)index;
    }

    internal int CalculateFixedEndIndex(double viewportOffset, double viewportExtent, double itemExtent, int totalCount)
    {
        double index = Math.Ceiling((viewportOffset + viewportExtent) / itemExtent);
        if (index > totalCount)
        {
            return totalCount;
        }

        return (int)index;
    }

    internal double CalculateNearestFixedOffset(double itemStart, double itemEnd, double viewportOffset, double viewportExtent)
    {
        double viewportEnd = viewportOffset + viewportExtent;
        if (itemStart < viewportOffset && itemEnd > viewportEnd)
        {
            return viewportOffset;
        }

        if (itemStart < viewportOffset)
        {
            return itemStart;
        }

        if (itemEnd > viewportEnd)
        {
            return itemEnd - viewportExtent;
        }

        return viewportOffset;
    }

    internal void ValidateFixedAlignment(string? alignment)
    {
        if (alignment != StartAlignment && alignment != CenterAlignment && alignment != EndAlignment && alignment != NearestAlignment)
        {
            throw new ArgumentException("The fixed scroll target alignment is invalid.", "target");
        }
    }
}
