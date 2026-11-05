using System;

namespace diameter;

internal static class AvpEncoder
{
    internal static DiameterEncodeResult MeasureAvp(DiameterAvp? avp)
    {
        return ValidateAvp(avp);
    }

    internal static DiameterEncodeResult EncodeAvp(Span<byte> destination, DiameterAvp? avp)
    {
        DiameterEncodeResult result = ValidateAvp(avp);
        if (result.Status != DiameterCodecStatus.Success || avp == null || avp.Data == null)
        {
            return result;
        }

        if (destination.Length < result.RequiredCount)
        {
            return CreateAvpFailure(
                DiameterCodecStatus.DestinationTooSmall,
                result.RequiredCount,
                0,
                "destination-too-small",
                "The destination cannot hold the encoded Diameter AVP.");
        }

        int flagValue = (int)avp.Flags;
        bool vendorSpecific = (flagValue & 128) != 0;
        int headerLength = DiameterProtocolConstants.AvpHeaderSize;
        if (vendorSpecific)
        {
            headerLength = DiameterProtocolConstants.VendorSpecificAvpHeaderSize;
        }

        int avpLength = headerLength + avp.Data.Length;
        NetworkByteOrder.WriteUInt32(destination.Slice(0, 4), avp.Code);
        destination[4] = (byte)flagValue;
        NetworkByteOrder.WriteUInt24(destination.Slice(5, 3), (uint)avpLength);
        if (vendorSpecific)
        {
            NetworkByteOrder.WriteUInt32(destination.Slice(8, 4), avp.VendorId);
        }

        int dataIndex = 0;
        while (dataIndex < avp.Data.Length)
        {
            destination[headerLength + dataIndex] = avp.Data[dataIndex];
            dataIndex = dataIndex + 1;
        }

        int paddingIndex = avpLength;
        while (paddingIndex < result.RequiredCount)
        {
            destination[paddingIndex] = 0;
            paddingIndex = paddingIndex + 1;
        }

        result.WrittenCount = result.RequiredCount;
        return result;
    }

    internal static DiameterEncodeResult ValidateAvp(DiameterAvp? avp)
    {
        if (avp == null)
        {
            return CreateAvpFailure(
                DiameterCodecStatus.Invalid,
                DiameterProtocolConstants.AvpHeaderSize,
                0,
                "missing-avp",
                "A Diameter AVP is required.");
        }

        int flagValue = (int)avp.Flags;
        if (flagValue < 0 || flagValue > 255 ||
            (flagValue & DiameterProtocolConstants.AvpReservedFlagMask) != 0)
        {
            return CreateAvpFailure(
                DiameterCodecStatus.Invalid,
                DiameterProtocolConstants.AvpHeaderSize,
                4,
                "invalid-avp-flags",
                "The Diameter AVP flags contain reserved or illegal bits.");
        }

        bool vendorSpecific = (flagValue & 128) != 0;
        int headerLength = DiameterProtocolConstants.AvpHeaderSize;
        if (vendorSpecific)
        {
            headerLength = DiameterProtocolConstants.VendorSpecificAvpHeaderSize;
        }

        if (vendorSpecific && avp.VendorId == 0U)
        {
            return CreateAvpFailure(
                DiameterCodecStatus.Invalid,
                headerLength,
                8,
                "invalid-vendor-id",
                "A vendor-specific AVP requires a nonzero Vendor-ID.");
        }

        if (!vendorSpecific && avp.VendorId != 0U)
        {
            return CreateAvpFailure(
                DiameterCodecStatus.Invalid,
                headerLength,
                4,
                "missing-vendor-flag",
                "A nonzero Vendor-ID requires the vendor-specific flag.");
        }

        if (avp.Data == null)
        {
            return CreateAvpFailure(
                DiameterCodecStatus.Invalid,
                headerLength,
                headerLength,
                "missing-avp-data",
                "The Diameter AVP data cannot be null.");
        }

        if (avp.Data.Length > DiameterProtocolConstants.MaximumAvpLength - headerLength)
        {
            return CreateAvpFailure(
                DiameterCodecStatus.LimitExceeded,
                headerLength,
                5,
                "avp-length-limit",
                "The Diameter AVP length exceeds the 24-bit limit.");
        }

        int avpLength = headerLength + avp.Data.Length;
        int requiredCount = AlignLength(avpLength);
        DiameterEncodeResult result = new DiameterEncodeResult();
        result.Status = DiameterCodecStatus.Success;
        result.RequiredCount = requiredCount;
        return result;
    }

    internal static int AlignLength(int length)
    {
        int remainder = length % DiameterProtocolConstants.Alignment;
        if (remainder == 0)
        {
            return length;
        }

        return length + DiameterProtocolConstants.Alignment - remainder;
    }

    internal static DiameterEncodeResult CreateAvpFailure(
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
}
