using System;

namespace diameter;

internal static class MessageEncoder
{
    internal static DiameterEncodeResult MeasureMessage(DiameterMessage? message)
    {
        DiameterEncodeResult header = ValidateHeader(
            message,
            DiameterProtocolConstants.MessageHeaderSize);
        if (header.Status != DiameterCodecStatus.Success || message == null)
        {
            return header;
        }

        if (message.Avps == null)
        {
            return CreateMessageFailure(
                DiameterCodecStatus.Invalid,
                DiameterProtocolConstants.MessageHeaderSize,
                DiameterProtocolConstants.MessageHeaderSize,
                "missing-message-avps",
                "The Diameter message AVP list cannot be null.");
        }

        int requiredCount = DiameterProtocolConstants.MessageHeaderSize;
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterEncodeResult item = AvpEncoder.MeasureAvp(message.Avps[index]);
            if (item.Status != DiameterCodecStatus.Success)
            {
                return CopyAvpMeasurementFailure(item, requiredCount);
            }

            int attemptedCount = AddMessageCounts(requiredCount, item.RequiredCount);
            if (attemptedCount > DiameterProtocolConstants.MaximumMessageLength)
            {
                return CreateMessageFailure(
                    DiameterCodecStatus.LimitExceeded,
                    attemptedCount,
                    1,
                    "message-length-limit",
                    "The encoded Diameter message exceeds the 24-bit aligned limit.");
            }

            requiredCount = attemptedCount;
            index = index + 1;
        }

        DiameterEncodeResult validation = ValidateHeader(message, requiredCount);
        if (validation.Status != DiameterCodecStatus.Success)
        {
            return validation;
        }

        DiameterEncodeResult result = new DiameterEncodeResult();
        result.Status = DiameterCodecStatus.Success;
        result.RequiredCount = requiredCount;
        return result;
    }

    internal static DiameterEncodeResult EncodeMessage(
        Span<byte> destination,
        DiameterMessage? message)
    {
        DiameterEncodeResult measurement = MeasureMessage(message);
        if (measurement.Status != DiameterCodecStatus.Success || message == null)
        {
            return measurement;
        }

        if (destination.Length < measurement.RequiredCount)
        {
            return CreateMessageFailure(
                DiameterCodecStatus.DestinationTooSmall,
                measurement.RequiredCount,
                0,
                "destination-too-small",
                "The destination cannot hold the encoded Diameter message.");
        }

        DiameterEncodeResult header = EncodeHeader(
            destination,
            message,
            measurement.RequiredCount);
        if (header.Status != DiameterCodecStatus.Success)
        {
            return header;
        }

        int writtenCount = DiameterProtocolConstants.MessageHeaderSize;
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterEncodeResult item = AvpEncoder.EncodeAvp(
                destination.Slice(writtenCount),
                message.Avps[index]);
            if (item.Status != DiameterCodecStatus.Success)
            {
                return CopyAvpMeasurementFailure(item, writtenCount);
            }

            writtenCount = writtenCount + item.WrittenCount;
            index = index + 1;
        }

        DiameterEncodeResult result = new DiameterEncodeResult();
        result.Status = DiameterCodecStatus.Success;
        result.RequiredCount = measurement.RequiredCount;
        result.WrittenCount = writtenCount;
        return result;
    }

    internal static DiameterEncodeResult EncodeMessageOwned(DiameterMessage? message)
    {
        DiameterEncodeResult measurement = MeasureMessage(message);
        if (measurement.Status != DiameterCodecStatus.Success)
        {
            return measurement;
        }

        byte[] data = new byte[measurement.RequiredCount];
        DiameterEncodeResult encoding = EncodeMessage(data, message);
        if (encoding.Status != DiameterCodecStatus.Success)
        {
            return encoding;
        }

        encoding.Data = data;
        return encoding;
    }

    internal static DiameterEncodeResult MeasureHeader(DiameterMessage? message, int messageLength)
    {
        return ValidateHeader(message, messageLength);
    }

    internal static DiameterEncodeResult EncodeHeader(
        Span<byte> destination,
        DiameterMessage? message,
        int messageLength)
    {
        DiameterEncodeResult result = ValidateHeader(message, messageLength);
        if (result.Status != DiameterCodecStatus.Success || message == null)
        {
            return result;
        }

        if (destination.Length < DiameterProtocolConstants.MessageHeaderSize)
        {
            return CreateHeaderFailure(
                DiameterCodecStatus.DestinationTooSmall,
                0,
                "destination-too-small",
                "The destination cannot hold a Diameter message header.");
        }

        destination[0] = message.Version;
        NetworkByteOrder.WriteUInt24(destination.Slice(1, 3), (uint)messageLength);
        destination[4] = (byte)message.Flags;
        NetworkByteOrder.WriteUInt24(destination.Slice(5, 3), message.CommandCode);
        NetworkByteOrder.WriteUInt32(destination.Slice(8, 4), message.ApplicationId);
        NetworkByteOrder.WriteUInt32(destination.Slice(12, 4), message.HopByHopIdentifier);
        NetworkByteOrder.WriteUInt32(destination.Slice(16, 4), message.EndToEndIdentifier);
        result.WrittenCount = DiameterProtocolConstants.MessageHeaderSize;
        return result;
    }

    internal static DiameterEncodeResult ValidateHeader(DiameterMessage? message, int messageLength)
    {
        if (message == null)
        {
            return CreateHeaderFailure(
                DiameterCodecStatus.Invalid,
                0,
                "missing-message",
                "A Diameter message is required.");
        }

        if (message.Version != DiameterProtocolConstants.Version)
        {
            return CreateHeaderFailure(
                DiameterCodecStatus.Invalid,
                0,
                "unsupported-version",
                "The Diameter version is not supported.");
        }

        if (messageLength < DiameterProtocolConstants.MinimumMessageLength)
        {
            return CreateHeaderFailure(
                DiameterCodecStatus.Invalid,
                1,
                "invalid-message-length",
                "The Diameter message length is too small or is not aligned.");
        }

        if (messageLength > DiameterProtocolConstants.MaximumMessageLength)
        {
            return CreateHeaderFailure(
                DiameterCodecStatus.LimitExceeded,
                1,
                "message-length-limit",
                "The Diameter message length exceeds the 24-bit aligned limit.");
        }

        if (messageLength % DiameterProtocolConstants.Alignment != 0)
        {
            return CreateHeaderFailure(
                DiameterCodecStatus.Invalid,
                1,
                "invalid-message-length",
                "The Diameter message length is too small or is not aligned.");
        }

        int flagValue = (int)message.Flags;
        bool request = (flagValue & 128) != 0;
        bool error = (flagValue & 32) != 0;
        bool retransmitted = (flagValue & 16) != 0;
        if (flagValue < 0 || flagValue > 255 ||
            (flagValue & DiameterProtocolConstants.MessageReservedFlagMask) != 0 ||
            (request && error) || (!request && retransmitted))
        {
            return CreateHeaderFailure(
                DiameterCodecStatus.Invalid,
                4,
                "invalid-message-flags",
                "The Diameter message flags contain reserved or illegal bits.");
        }

        if (message.CommandCode > DiameterProtocolConstants.MaximumCommandCode)
        {
            return CreateHeaderFailure(
                DiameterCodecStatus.LimitExceeded,
                5,
                "command-code-limit",
                "The Diameter command code exceeds the 24-bit limit.");
        }

        DiameterEncodeResult result = new DiameterEncodeResult();
        result.Status = DiameterCodecStatus.Success;
        result.RequiredCount = DiameterProtocolConstants.MessageHeaderSize;
        return result;
    }

    internal static DiameterEncodeResult CreateHeaderFailure(
        DiameterCodecStatus status,
        int offset,
        string code,
        string message)
    {
        DiameterCodecIssue issue = new DiameterCodecIssue();
        issue.Severity = DiameterIssueSeverity.Error;
        issue.Status = status;
        issue.Offset = offset;
        issue.Code = code;
        issue.Message = message;

        DiameterEncodeResult result = new DiameterEncodeResult();
        result.Status = status;
        result.RequiredCount = DiameterProtocolConstants.MessageHeaderSize;
        result.Issues.Add(issue);
        return result;
    }

    internal static DiameterEncodeResult CopyAvpMeasurementFailure(
        DiameterEncodeResult source,
        int baseOffset)
    {
        DiameterCodecIssue sourceIssue = source.Issues[0];
        return CreateMessageFailure(
            source.Status,
            AddMessageCounts(baseOffset, source.RequiredCount),
            AddMessageCounts(baseOffset, sourceIssue.Offset),
            sourceIssue.Code,
            sourceIssue.Message);
    }

    internal static DiameterEncodeResult CreateMessageFailure(
        DiameterCodecStatus status,
        int requiredCount,
        int offset,
        string code,
        string message)
    {
        DiameterCodecIssue issue = new DiameterCodecIssue();
        issue.Severity = DiameterIssueSeverity.Error;
        issue.Status = status;
        issue.Offset = offset;
        issue.Code = code;
        issue.Message = message;

        DiameterEncodeResult result = new DiameterEncodeResult();
        result.Status = status;
        result.RequiredCount = requiredCount;
        result.Issues.Add(issue);
        return result;
    }

    internal static int AddMessageCounts(int first, int second)
    {
        if (first > int.MaxValue - second)
        {
            return int.MaxValue;
        }

        return first + second;
    }
}
