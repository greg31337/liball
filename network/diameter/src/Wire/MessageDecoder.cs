using System;
using System.Collections.Generic;

namespace diameter;

internal enum DiameterDecodeProfile
{
    Strict = 0,
    Permissive = 1
}

internal static class MessageDecoder
{
    internal static DiameterDecodeResult DecodeMessage(
        ReadOnlySpan<byte> source,
        DiameterDecodeProfile profile,
        DiameterLimitConfiguration? limits)
    {
        if (profile != DiameterDecodeProfile.Strict &&
            profile != DiameterDecodeProfile.Permissive)
        {
            return CreateMessageDecodeFailure(
                DiameterCodecStatus.Invalid,
                DiameterProtocolConstants.MessageHeaderSize,
                0,
                "invalid-decode-profile",
                "The Diameter decode profile is not supported.");
        }

        if (limits == null ||
            limits.MaximumMessageSize < DiameterProtocolConstants.MinimumMessageLength ||
            limits.MaximumMessageSize > DiameterProtocolConstants.MaximumMessageLength ||
            limits.MaximumMessageSize % DiameterProtocolConstants.Alignment != 0 ||
            limits.MaximumAvpCount <= 0 || limits.MaximumCodecIssueCount <= 0)
        {
            return CreateMessageDecodeFailure(
                DiameterCodecStatus.Invalid,
                DiameterProtocolConstants.MessageHeaderSize,
                0,
                "invalid-decode-limits",
                "Valid message-size, AVP-count, and issue-count limits are required.");
        }

        DiameterDecodeResult header = DecodeHeaderWithProfile(source, profile);
        if (header.Status != DiameterCodecStatus.Success || header.Message == null)
        {
            return header;
        }

        int messageLength = header.RequiredCount;
        if (messageLength > limits.MaximumMessageSize)
        {
            return CreateMessageDecodeFailure(
                DiameterCodecStatus.LimitExceeded,
                messageLength,
                1,
                "configured-message-size-limit",
                "The declared Diameter message exceeds the configured size limit.");
        }

        if (source.Length < messageLength)
        {
            return CreateMessageDecodeFailure(
                DiameterCodecStatus.Incomplete,
                messageLength,
                source.Length,
                "incomplete-message",
                "The source does not contain the complete declared Diameter message.");
        }

        DiameterMessage message = header.Message;
        DiameterDecodeResult result = new DiameterDecodeResult();
        result.Status = DiameterCodecStatus.Success;
        result.RequiredCount = messageLength;
        CopyMessageIssues(
            result,
            header.Issues,
            0,
            limits.MaximumCodecIssueCount);

        ReadOnlySpan<byte> frame = source.Slice(0, messageLength);
        int consumedCount = DiameterProtocolConstants.MessageHeaderSize;
        int avpCount = 0;
        while (consumedCount < messageLength)
        {
            if (avpCount >= limits.MaximumAvpCount)
            {
                return CreateMessageDecodeFailure(
                    DiameterCodecStatus.LimitExceeded,
                    messageLength,
                    consumedCount,
                    "message-avp-count-limit",
                    "The Diameter message contains more AVPs than the configured limit.");
            }

            DiameterAvpDecodeResult item = AvpDecoder.DecodeAvpWithProfile(
                frame.Slice(consumedCount),
                profile);
            if (item.Status != DiameterCodecStatus.Success || item.Avp == null)
            {
                DiameterCodecIssue issue = item.Issues[0];
                DiameterCodecStatus status = item.Status;
                string code = issue.Code;
                string messageText = issue.Message;
                if (status == DiameterCodecStatus.Incomplete)
                {
                    status = DiameterCodecStatus.Invalid;
                    code = "invalid-message-avp-sequence";
                    messageText = "The declared Diameter message ends within an AVP.";
                }

                return CreateMessageDecodeFailure(
                    status,
                    messageLength,
                    consumedCount + issue.Offset,
                    code,
                    messageText);
            }

            CopyMessageIssues(
                result,
                item.Issues,
                consumedCount,
                limits.MaximumCodecIssueCount);
            message.Avps.Add(item.Avp);
            consumedCount = consumedCount + item.ConsumedCount;
            avpCount = avpCount + 1;
        }

        result.ConsumedCount = messageLength;
        result.Message = message;
        return result;
    }

    internal static DiameterDecodeResult DecodeHeader(ReadOnlySpan<byte> source)
    {
        return DecodeHeaderWithProfile(source, DiameterDecodeProfile.Strict);
    }

    internal static DiameterDecodeResult DecodeHeaderWithProfile(
        ReadOnlySpan<byte> source,
        DiameterDecodeProfile profile)
    {
        if (profile != DiameterDecodeProfile.Strict &&
            profile != DiameterDecodeProfile.Permissive)
        {
            return CreateHeaderFailure(
                DiameterCodecStatus.Invalid,
                0,
                "invalid-decode-profile",
                "The Diameter decode profile is not supported.");
        }

        if (source.Length < DiameterProtocolConstants.MessageHeaderSize)
        {
            return CreateHeaderFailure(
                DiameterCodecStatus.Incomplete,
                source.Length,
                "incomplete-header",
                "The source does not contain a complete Diameter message header.");
        }

        byte version = source[0];
        if (version != DiameterProtocolConstants.Version)
        {
            return CreateHeaderFailure(
                DiameterCodecStatus.Invalid,
                0,
                "unsupported-version",
                "The Diameter version is not supported.");
        }

        uint encodedLength = NetworkByteOrder.ReadUInt24(source.Slice(1, 3));
        int messageLength = (int)encodedLength;
        if (messageLength < DiameterProtocolConstants.MinimumMessageLength)
        {
            return CreateHeaderFailure(
                DiameterCodecStatus.Invalid,
                1,
                "invalid-message-length",
                "The declared Diameter message length is smaller than the header.");
        }

        if (messageLength > DiameterProtocolConstants.MaximumMessageLength)
        {
            return CreateHeaderFailure(
                DiameterCodecStatus.LimitExceeded,
                1,
                "message-length-limit",
                "The declared Diameter message length exceeds the aligned 24-bit limit.");
        }

        if (messageLength % DiameterProtocolConstants.Alignment != 0)
        {
            return CreateHeaderFailure(
                DiameterCodecStatus.Invalid,
                1,
                "invalid-message-length",
                "The declared Diameter message length is not aligned.");
        }

        int flagValue = source[4];
        bool request = (flagValue & 128) != 0;
        bool error = (flagValue & 32) != 0;
        bool retransmitted = (flagValue & 16) != 0;
        bool invalidFlags =
            (flagValue & DiameterProtocolConstants.MessageReservedFlagMask) != 0 ||
            (request && error) || (!request && retransmitted);
        if (invalidFlags && profile == DiameterDecodeProfile.Strict)
        {
            return CreateHeaderFailure(
                DiameterCodecStatus.Invalid,
                4,
                "invalid-message-flags",
                "The Diameter message flags contain reserved or illegal bits.");
        }

        DiameterMessage message = new DiameterMessage();
        message.Version = version;
        message.Flags = (DiameterMessageFlags)flagValue;
        message.CommandCode = NetworkByteOrder.ReadUInt24(source.Slice(5, 3));
        message.ApplicationId = NetworkByteOrder.ReadUInt32(source.Slice(8, 4));
        message.HopByHopIdentifier = NetworkByteOrder.ReadUInt32(source.Slice(12, 4));
        message.EndToEndIdentifier = NetworkByteOrder.ReadUInt32(source.Slice(16, 4));

        DiameterDecodeResult result = new DiameterDecodeResult();
        result.Status = DiameterCodecStatus.Success;
        result.RequiredCount = messageLength;
        result.ConsumedCount = DiameterProtocolConstants.MessageHeaderSize;
        result.Message = message;
        if (invalidFlags)
        {
            AddMessageWarning(
                result.Issues,
                4,
                "invalid-message-flags",
                "The Diameter message flags contain reserved or illegal bits.");
        }

        return result;
    }

    internal static DiameterDecodeResult CreateHeaderFailure(
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

        DiameterDecodeResult result = new DiameterDecodeResult();
        result.Status = status;
        result.RequiredCount = DiameterProtocolConstants.MessageHeaderSize;
        result.Issues.Add(issue);
        return result;
    }

    internal static DiameterDecodeResult CreateMessageDecodeFailure(
        DiameterCodecStatus status,
        int requiredCount,
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

        DiameterDecodeResult result = new DiameterDecodeResult();
        result.Status = status;
        result.RequiredCount = requiredCount;
        result.Issues.Add(issue);
        return result;
    }

    internal static void CopyMessageIssues(
        DiameterDecodeResult destination,
        List<DiameterCodecIssue> source,
        int baseOffset,
        int maximumCount)
    {
        int index = 0;
        while (index < source.Count && destination.Issues.Count < maximumCount)
        {
            DiameterCodecIssue sourceIssue = source[index];
            DiameterCodecIssue issue = new DiameterCodecIssue();
            issue.Severity = sourceIssue.Severity;
            issue.Status = sourceIssue.Status;
            issue.Offset = baseOffset + sourceIssue.Offset;
            issue.Code = sourceIssue.Code;
            issue.Message = sourceIssue.Message;
            destination.Issues.Add(issue);
            index = index + 1;
        }
    }

    internal static void AddMessageWarning(
        List<DiameterCodecIssue> issues,
        int offset,
        string code,
        string message)
    {
        DiameterCodecIssue issue = new DiameterCodecIssue();
        issue.Severity = DiameterIssueSeverity.Warning;
        issue.Status = DiameterCodecStatus.Success;
        issue.Offset = offset;
        issue.Code = code;
        issue.Message = message;
        issues.Add(issue);
    }
}
