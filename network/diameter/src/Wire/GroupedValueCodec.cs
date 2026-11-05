using System;
using System.Collections.Generic;

namespace diameter;

internal class GroupedAvpType
{
    internal uint Code;
    internal uint VendorId;
}

internal class GroupedCodecContext
{
    internal DiameterLimitConfiguration Limits = new DiameterLimitConfiguration();
    internal List<GroupedAvpType> GroupedTypes = new List<GroupedAvpType>();
    internal DiameterCodecStatus Status;
    internal int RequiredCount;
    internal int RootConsumedCount;
    internal int ErrorOffset;
    internal string ErrorCode = string.Empty;
    internal string ErrorMessage = string.Empty;
}

internal static class GroupedValueCodec
{
    internal static DiameterValueResult MeasureGroupedValue(
        List<DiameterAvp>? avps,
        List<GroupedAvpType>? groupedTypes,
        DiameterLimitConfiguration? limits)
    {
        DiameterValueResult inputFailure = ValidateGroupedInputs(avps, limits);
        if (inputFailure.Status != DiameterCodecStatus.Success || avps == null || limits == null)
        {
            return inputFailure;
        }

        GroupedCodecContext context = CreateGroupedContext(groupedTypes, limits);
        int requiredCount = MeasureGroupedItems(avps, context, 1, 0);
        if (requiredCount < 0)
        {
            return CreateGroupedFailure(context);
        }

        DiameterValueResult result = new DiameterValueResult();
        result.Status = DiameterCodecStatus.Success;
        result.RequiredCount = requiredCount;
        return result;
    }

    internal static DiameterValueResult EncodeGroupedValue(
        Span<byte> destination,
        List<DiameterAvp>? avps,
        List<GroupedAvpType>? groupedTypes,
        DiameterLimitConfiguration? limits)
    {
        DiameterValueResult measurement = MeasureGroupedValue(avps, groupedTypes, limits);
        if (measurement.Status != DiameterCodecStatus.Success || avps == null)
        {
            return measurement;
        }

        if (destination.Length < measurement.RequiredCount)
        {
            return ValueCodecs.CreateValueFailure(
                DiameterCodecStatus.DestinationTooSmall,
                measurement.RequiredCount,
                0,
                "destination-too-small",
                "The destination cannot hold the encoded Grouped value.");
        }

        int writtenCount = 0;
        int index = 0;
        while (index < avps.Count)
        {
            DiameterEncodeResult item = AvpEncoder.EncodeAvp(
                destination.Slice(writtenCount),
                avps[index]);
            if (item.Status != DiameterCodecStatus.Success)
            {
                return CopyEncodeFailure(item, writtenCount);
            }

            writtenCount = writtenCount + item.WrittenCount;
            index = index + 1;
        }

        DiameterValueResult result = new DiameterValueResult();
        result.Status = DiameterCodecStatus.Success;
        result.RequiredCount = measurement.RequiredCount;
        result.WrittenCount = writtenCount;
        return result;
    }

    internal static DiameterValueResult DecodeGroupedValue(
        ReadOnlySpan<byte> source,
        List<GroupedAvpType>? groupedTypes,
        DiameterLimitConfiguration? limits)
    {
        DiameterValueResult inputFailure = ValidateGroupedInputs(
            new List<DiameterAvp>(),
            limits);
        if (inputFailure.Status != DiameterCodecStatus.Success || limits == null)
        {
            return inputFailure;
        }

        GroupedCodecContext context = CreateGroupedContext(groupedTypes, limits);
        context.RequiredCount = source.Length;
        List<DiameterAvp> avps = new List<DiameterAvp>();
        bool valid = ValidateGroupedBytes(source, context, 1, 0, avps, true);
        if (!valid)
        {
            DiameterValueResult failure = CreateGroupedFailure(context);
            int retainedIndex = 0;
            while (retainedIndex < avps.Count)
            {
                failure.Avps.Add(avps[retainedIndex]);
                retainedIndex = retainedIndex + 1;
            }

            return failure;
        }

        DiameterValueResult result = new DiameterValueResult();
        result.Status = DiameterCodecStatus.Success;
        result.RequiredCount = source.Length;
        result.ConsumedCount = source.Length;
        int index = 0;
        while (index < avps.Count)
        {
            result.Avps.Add(avps[index]);
            index = index + 1;
        }

        return result;
    }

    internal static DiameterValueResult ValidateGroupedInputs(
        List<DiameterAvp>? avps,
        DiameterLimitConfiguration? limits)
    {
        if (avps == null)
        {
            return ValueCodecs.CreateValueFailure(
                DiameterCodecStatus.Invalid,
                0,
                0,
                "missing-grouped-avps",
                "A list of AVPs is required for a Grouped value.");
        }

        if (limits == null || limits.MaximumGroupedDepth <= 0 ||
            limits.MaximumGroupedAvpCount <= 0 || limits.MaximumGroupedBytes <= 0)
        {
            return ValueCodecs.CreateValueFailure(
                DiameterCodecStatus.Invalid,
                0,
                0,
                "invalid-grouped-limits",
                "Positive Grouped depth, AVP-count, and byte limits are required.");
        }

        DiameterValueResult result = new DiameterValueResult();
        result.Status = DiameterCodecStatus.Success;
        return result;
    }

    internal static GroupedCodecContext CreateGroupedContext(
        List<GroupedAvpType>? groupedTypes,
        DiameterLimitConfiguration limits)
    {
        GroupedCodecContext context = new GroupedCodecContext();
        context.Limits = limits;
        context.Status = DiameterCodecStatus.Success;
        if (groupedTypes != null)
        {
            context.GroupedTypes = groupedTypes;
        }

        return context;
    }

    internal static int MeasureGroupedItems(
        List<DiameterAvp> avps,
        GroupedCodecContext context,
        int depth,
        int baseOffset)
    {
        if (avps.Count > context.Limits.MaximumGroupedAvpCount)
        {
            SetGroupedFailure(
                context,
                DiameterCodecStatus.LimitExceeded,
                0,
                0,
                baseOffset,
                "grouped-avp-count-limit",
                "The Grouped value contains more AVPs than the configured limit.");
            return -1;
        }

        int requiredCount = 0;
        int index = 0;
        while (index < avps.Count)
        {
            DiameterAvp avp = avps[index];
            DiameterEncodeResult measurement = AvpEncoder.MeasureAvp(avp);
            if (measurement.Status != DiameterCodecStatus.Success)
            {
                CopyMeasurementFailure(context, measurement, baseOffset + requiredCount);
                return -1;
            }

            if (requiredCount > context.Limits.MaximumGroupedBytes - measurement.RequiredCount)
            {
                int attemptedCount = AddGroupedCounts(requiredCount, measurement.RequiredCount);
                SetGroupedFailure(
                    context,
                    DiameterCodecStatus.LimitExceeded,
                    attemptedCount,
                    0,
                    baseOffset + requiredCount,
                    "grouped-byte-limit",
                    "The Grouped value exceeds the configured byte limit.");
                return -1;
            }

            if (IsGroupedAvp(avp, context.GroupedTypes))
            {
                int headerLength = ReadAvpHeaderLength(avp);
                context.RequiredCount = AddGroupedCounts(
                    requiredCount,
                    measurement.RequiredCount);
                if (depth >= context.Limits.MaximumGroupedDepth)
                {
                    SetGroupedFailure(
                        context,
                        DiameterCodecStatus.LimitExceeded,
                        requiredCount + measurement.RequiredCount,
                        0,
                        baseOffset + requiredCount + headerLength,
                        "grouped-depth-limit",
                        "The Grouped value exceeds the configured nesting depth.");
                    return -1;
                }

                bool nestedValid = ValidateGroupedBytes(
                    avp.Data,
                    context,
                    depth + 1,
                    baseOffset + requiredCount + headerLength,
                    null,
                    false);
                if (!nestedValid)
                {
                    return -1;
                }
            }

            requiredCount = requiredCount + measurement.RequiredCount;
            index = index + 1;
        }

        return requiredCount;
    }

    internal static bool ValidateGroupedBytes(
        ReadOnlySpan<byte> source,
        GroupedCodecContext context,
        int depth,
        int baseOffset,
        List<DiameterAvp>? retainedAvps,
        bool root)
    {
        if (source.Length > context.Limits.MaximumGroupedBytes)
        {
            int rootConsumedCount = context.RootConsumedCount;
            if (root)
            {
                rootConsumedCount = 0;
            }

            SetGroupedFailure(
                context,
                DiameterCodecStatus.LimitExceeded,
                source.Length,
                rootConsumedCount,
                baseOffset,
                "grouped-byte-limit",
                "The Grouped value exceeds the configured byte limit.");
            return false;
        }

        int consumedCount = 0;
        int itemCount = 0;
        while (consumedCount < source.Length)
        {
            if (itemCount >= context.Limits.MaximumGroupedAvpCount)
            {
                SetGroupedFailure(
                    context,
                    DiameterCodecStatus.LimitExceeded,
                    source.Length,
                    context.RootConsumedCount,
                    baseOffset + consumedCount,
                    "grouped-avp-count-limit",
                    "The Grouped value contains more AVPs than the configured limit.");
                return false;
            }

            DiameterAvpDecodeResult item = AvpDecoder.DecodeAvp(source.Slice(consumedCount));
            if (item.Status != DiameterCodecStatus.Success || item.Avp == null)
            {
                CopyDecodeFailure(context, item, baseOffset + consumedCount, root, consumedCount);
                return false;
            }

            DiameterAvp avp = item.Avp;
            if (IsGroupedAvp(avp, context.GroupedTypes))
            {
                int headerLength = ReadAvpHeaderLength(avp);
                if (depth >= context.Limits.MaximumGroupedDepth)
                {
                    SetGroupedFailure(
                        context,
                        DiameterCodecStatus.LimitExceeded,
                        source.Length,
                        context.RootConsumedCount,
                        baseOffset + consumedCount + headerLength,
                        "grouped-depth-limit",
                        "The Grouped value exceeds the configured nesting depth.");
                    return false;
                }

                if (root)
                {
                    context.RootConsumedCount = consumedCount;
                }

                bool nestedValid = ValidateGroupedBytes(
                    avp.Data,
                    context,
                    depth + 1,
                    baseOffset + consumedCount + headerLength,
                    null,
                    false);
                if (!nestedValid)
                {
                    return false;
                }
            }

            if (retainedAvps != null)
            {
                retainedAvps.Add(avp);
            }

            consumedCount = consumedCount + item.ConsumedCount;
            itemCount = itemCount + 1;
            if (root)
            {
                context.RootConsumedCount = consumedCount;
            }
        }

        return true;
    }

    internal static bool IsGroupedAvp(DiameterAvp avp, List<GroupedAvpType> groupedTypes)
    {
        int index = 0;
        while (index < groupedTypes.Count)
        {
            GroupedAvpType type = groupedTypes[index];
            if (type != null && type.Code == avp.Code && type.VendorId == avp.VendorId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static int ReadAvpHeaderLength(DiameterAvp avp)
    {
        if ((avp.Flags & DiameterAvpFlags.VendorSpecific) != DiameterAvpFlags.None)
        {
            return DiameterProtocolConstants.VendorSpecificAvpHeaderSize;
        }

        return DiameterProtocolConstants.AvpHeaderSize;
    }

    internal static void CopyMeasurementFailure(
        GroupedCodecContext context,
        DiameterEncodeResult source,
        int baseOffset)
    {
        DiameterCodecIssue issue = source.Issues[0];
        SetGroupedFailure(
            context,
            source.Status,
            source.RequiredCount,
            0,
            baseOffset + issue.Offset,
            issue.Code,
            issue.Message);
    }

    internal static void CopyDecodeFailure(
        GroupedCodecContext context,
        DiameterAvpDecodeResult source,
        int baseOffset,
        bool root,
        int consumedCount)
    {
        DiameterCodecIssue issue = source.Issues[0];
        DiameterCodecStatus status = source.Status;
        string code = issue.Code;
        string message = issue.Message;
        int requiredCount = source.RequiredCount;
        int rootConsumedCount = context.RootConsumedCount;
        if (root)
        {
            requiredCount = AddGroupedCounts(consumedCount, source.RequiredCount);
            rootConsumedCount = consumedCount;
        }
        else
        {
            requiredCount = context.RequiredCount;
            if (status == DiameterCodecStatus.Incomplete)
            {
                status = DiameterCodecStatus.Invalid;
                code = "invalid-grouped-content";
                message = "A nested Grouped AVP contains a truncated inner AVP.";
            }
        }

        SetGroupedFailure(
            context,
            status,
            requiredCount,
            rootConsumedCount,
            baseOffset + issue.Offset,
            code,
            message);
    }

    internal static DiameterValueResult CopyEncodeFailure(
        DiameterEncodeResult source,
        int baseOffset)
    {
        DiameterCodecIssue sourceIssue = source.Issues[0];
        return ValueCodecs.CreateValueFailure(
            source.Status,
            AddGroupedCounts(source.RequiredCount, baseOffset),
            sourceIssue.Offset + baseOffset,
            sourceIssue.Code,
            sourceIssue.Message);
    }

    internal static int AddGroupedCounts(int first, int second)
    {
        if (first > int.MaxValue - second)
        {
            return int.MaxValue;
        }

        return first + second;
    }

    internal static void SetGroupedFailure(
        GroupedCodecContext context,
        DiameterCodecStatus status,
        int requiredCount,
        int consumedCount,
        int offset,
        string code,
        string message)
    {
        context.Status = status;
        context.RequiredCount = requiredCount;
        context.RootConsumedCount = consumedCount;
        context.ErrorOffset = offset;
        context.ErrorCode = code;
        context.ErrorMessage = message;
    }

    internal static DiameterValueResult CreateGroupedFailure(GroupedCodecContext context)
    {
        DiameterValueResult result = ValueCodecs.CreateValueFailure(
            context.Status,
            context.RequiredCount,
            context.ErrorOffset,
            context.ErrorCode,
            context.ErrorMessage);
        result.ConsumedCount = context.RootConsumedCount;
        return result;
    }
}
