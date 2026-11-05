using System;

namespace diameter;

internal static class StreamFramer
{
    internal static DiameterFramingResult FrameStreamData(
        ReadOnlySpan<byte> retained,
        ReadOnlySpan<byte> input,
        bool endOfStream,
        int maximumFrameSize)
    {
        if (maximumFrameSize < DiameterProtocolConstants.MinimumMessageLength ||
            maximumFrameSize > DiameterProtocolConstants.MaximumMessageLength ||
            maximumFrameSize % DiameterProtocolConstants.Alignment != 0)
        {
            return CreateFramingFailure(
                DiameterCodecStatus.Invalid,
                DiameterProtocolConstants.MessageHeaderSize,
                0,
                "invalid-frame-limit",
                "The maximum Diameter frame size is invalid.",
                endOfStream);
        }

        if (retained.Length > int.MaxValue - input.Length)
        {
            return CreateFramingFailure(
                DiameterCodecStatus.LimitExceeded,
                int.MaxValue,
                0,
                "framing-buffer-limit",
                "The retained and new framing input exceeds the supported buffer size.",
                endOfStream);
        }

        byte[] combined = new byte[retained.Length + input.Length];
        int copyIndex = 0;
        while (copyIndex < retained.Length)
        {
            combined[copyIndex] = retained[copyIndex];
            copyIndex = copyIndex + 1;
        }

        int inputIndex = 0;
        while (inputIndex < input.Length)
        {
            combined[retained.Length + inputIndex] = input[inputIndex];
            inputIndex = inputIndex + 1;
        }

        DiameterFramingResult result = new DiameterFramingResult();
        result.Status = DiameterCodecStatus.Success;
        result.EndOfStream = endOfStream;
        int consumedCount = 0;
        while (consumedCount < combined.Length)
        {
            int availableCount = combined.Length - consumedCount;
            if (availableCount < DiameterProtocolConstants.MessageHeaderSize)
            {
                result.Status = DiameterCodecStatus.Incomplete;
                result.RequiredCount = DiameterProtocolConstants.MessageHeaderSize;
                result.ConsumedCount = consumedCount;
                result.Remainder = CopyFrameBytes(combined, consumedCount, availableCount);
                string code = "incomplete-frame-header";
                string message = "More input is required to complete the Diameter frame header.";
                if (endOfStream)
                {
                    code = "truncated-frame-header";
                    message = "The stream ended within a Diameter frame header.";
                }

                AddFramingIssue(
                    result,
                    DiameterCodecStatus.Incomplete,
                    combined.Length,
                    code,
                    message);
                return result;
            }

            int frameOffset = consumedCount;
            if (combined[frameOffset] != DiameterProtocolConstants.Version)
            {
                result.Status = DiameterCodecStatus.Invalid;
                result.RequiredCount = DiameterProtocolConstants.MessageHeaderSize;
                result.ConsumedCount = consumedCount;
                result.Remainder = CopyFrameBytes(combined, consumedCount, availableCount);
                AddFramingIssue(
                    result,
                    DiameterCodecStatus.Invalid,
                    frameOffset,
                    "unsupported-version",
                    "The Diameter frame version is not supported.");
                return result;
            }

            int frameLength = (int)NetworkByteOrder.ReadUInt24(
                new ReadOnlySpan<byte>(combined, frameOffset + 1, 3));
            if (frameLength < DiameterProtocolConstants.MinimumMessageLength)
            {
                result.Status = DiameterCodecStatus.Invalid;
                result.RequiredCount = DiameterProtocolConstants.MessageHeaderSize;
                result.ConsumedCount = consumedCount;
                result.Remainder = CopyFrameBytes(combined, consumedCount, availableCount);
                AddFramingIssue(
                    result,
                    DiameterCodecStatus.Invalid,
                    frameOffset + 1,
                    "invalid-message-length",
                    "The Diameter frame length is smaller than the message header.");
                return result;
            }

            if (frameLength > DiameterProtocolConstants.MaximumMessageLength)
            {
                result.Status = DiameterCodecStatus.LimitExceeded;
                result.RequiredCount = frameLength;
                result.ConsumedCount = consumedCount;
                result.Remainder = CopyFrameBytes(combined, consumedCount, availableCount);
                AddFramingIssue(
                    result,
                    DiameterCodecStatus.LimitExceeded,
                    frameOffset + 1,
                    "message-length-limit",
                    "The Diameter frame length exceeds the protocol limit.");
                return result;
            }

            if (frameLength % DiameterProtocolConstants.Alignment != 0)
            {
                result.Status = DiameterCodecStatus.Invalid;
                result.RequiredCount = DiameterProtocolConstants.MessageHeaderSize;
                result.ConsumedCount = consumedCount;
                result.Remainder = CopyFrameBytes(combined, consumedCount, availableCount);
                AddFramingIssue(
                    result,
                    DiameterCodecStatus.Invalid,
                    frameOffset + 1,
                    "invalid-message-length",
                    "The Diameter frame length is not aligned.");
                return result;
            }

            if (frameLength > maximumFrameSize)
            {
                result.Status = DiameterCodecStatus.LimitExceeded;
                result.RequiredCount = frameLength;
                result.ConsumedCount = consumedCount;
                result.Remainder = CopyFrameBytes(combined, consumedCount, availableCount);
                AddFramingIssue(
                    result,
                    DiameterCodecStatus.LimitExceeded,
                    frameOffset + 1,
                    "configured-frame-size-limit",
                    "The Diameter frame length exceeds the configured limit.");
                return result;
            }

            if (availableCount < frameLength)
            {
                result.Status = DiameterCodecStatus.Incomplete;
                result.RequiredCount = frameLength;
                result.ConsumedCount = consumedCount;
                result.Remainder = CopyFrameBytes(combined, consumedCount, availableCount);
                string code = "incomplete-frame";
                string message = "More input is required to complete the Diameter frame.";
                if (endOfStream)
                {
                    code = "truncated-frame";
                    message = "The stream ended within a Diameter frame.";
                }

                AddFramingIssue(
                    result,
                    DiameterCodecStatus.Incomplete,
                    combined.Length,
                    code,
                    message);
                return result;
            }

            result.Frames.Add(CopyFrameBytes(combined, frameOffset, frameLength));
            consumedCount = consumedCount + frameLength;
        }

        result.ConsumedCount = consumedCount;
        if (endOfStream)
        {
            result.CleanEndOfStream = true;
        }

        return result;
    }

    internal static byte[] CopyFrameBytes(byte[] source, int offset, int count)
    {
        byte[] data = new byte[count];
        int index = 0;
        while (index < count)
        {
            data[index] = source[offset + index];
            index = index + 1;
        }

        return data;
    }

    internal static DiameterFramingResult CreateFramingFailure(
        DiameterCodecStatus status,
        int requiredCount,
        int offset,
        string code,
        string message,
        bool endOfStream)
    {
        DiameterFramingResult result = new DiameterFramingResult();
        result.Status = status;
        result.RequiredCount = requiredCount;
        result.EndOfStream = endOfStream;
        AddFramingIssue(result, status, offset, code, message);
        return result;
    }

    internal static void AddFramingIssue(
        DiameterFramingResult result,
        DiameterCodecStatus status,
        int offset,
        string code,
        string message)
    {
        DiameterCodecIssue issue = new DiameterCodecIssue();
        if (status == DiameterCodecStatus.Incomplete)
        {
            issue.Severity = DiameterIssueSeverity.Information;
        }
        else
        {
            issue.Severity = DiameterIssueSeverity.Error;
        }

        issue.Status = status;
        issue.Offset = offset;
        issue.Code = code;
        issue.Message = message;
        result.Issues.Add(issue);
    }
}
