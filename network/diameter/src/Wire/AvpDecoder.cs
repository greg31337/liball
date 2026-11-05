using System;
using System.Collections.Generic;

namespace diameter;

internal static class AvpDecoder
{
    internal static DiameterAvpDecodeResult DecodeAvp(ReadOnlySpan<byte> source)
    {
        return DecodeAvpWithProfile(source, DiameterDecodeProfile.Strict);
    }

    internal static DiameterAvpDecodeResult DecodeAvpWithProfile(
        ReadOnlySpan<byte> source,
        DiameterDecodeProfile profile)
    {
        if (profile != DiameterDecodeProfile.Strict &&
            profile != DiameterDecodeProfile.Permissive)
        {
            return CreateAvpFailure(
                DiameterCodecStatus.Invalid,
                DiameterProtocolConstants.AvpHeaderSize,
                0,
                "invalid-decode-profile",
                "The Diameter decode profile is not supported.");
        }

        if (source.Length < DiameterProtocolConstants.AvpHeaderSize)
        {
            return CreateAvpFailure(
                DiameterCodecStatus.Incomplete,
                DiameterProtocolConstants.AvpHeaderSize,
                source.Length,
                "incomplete-avp-header",
                "The source does not contain a complete Diameter AVP header.");
        }

        List<DiameterCodecIssue> warnings = new List<DiameterCodecIssue>();
        int flagValue = source[4];
        if ((flagValue & DiameterProtocolConstants.AvpReservedFlagMask) != 0)
        {
            if (profile == DiameterDecodeProfile.Strict)
            {
                return CreateAvpFailure(
                    DiameterCodecStatus.Invalid,
                    DiameterProtocolConstants.AvpHeaderSize,
                    4,
                    "invalid-avp-flags",
                    "The Diameter AVP flags contain reserved bits.");
            }

            AddAvpWarning(
                warnings,
                4,
                "invalid-avp-flags",
                "The Diameter AVP flags contain reserved bits.");
        }

        bool vendorSpecific = (flagValue & 128) != 0;
        int headerLength = DiameterProtocolConstants.AvpHeaderSize;
        if (vendorSpecific)
        {
            headerLength = DiameterProtocolConstants.VendorSpecificAvpHeaderSize;
        }

        uint encodedLength = NetworkByteOrder.ReadUInt24(source.Slice(5, 3));
        int avpLength = (int)encodedLength;
        if (avpLength < headerLength)
        {
            string code = "invalid-avp-length";
            string message = "The declared Diameter AVP length is smaller than its header.";
            if (vendorSpecific)
            {
                code = "missing-vendor-header";
                message = "The vendor-specific AVP length does not include a Vendor-ID.";
            }

            return CreateAvpFailure(
                DiameterCodecStatus.Invalid,
                headerLength,
                5,
                code,
                message);
        }

        int paddedLength = AvpEncoder.AlignLength(avpLength);
        if (source.Length < paddedLength)
        {
            return CreateAvpFailure(
                DiameterCodecStatus.Incomplete,
                paddedLength,
                source.Length,
                "incomplete-avp",
                "The source does not contain the complete Diameter AVP and padding.");
        }

        uint vendorId = 0U;
        if (vendorSpecific)
        {
            vendorId = NetworkByteOrder.ReadUInt32(source.Slice(8, 4));
            if (vendorId == 0U)
            {
                if (profile == DiameterDecodeProfile.Strict)
                {
                    return CreateAvpFailure(
                        DiameterCodecStatus.Invalid,
                        paddedLength,
                        8,
                        "invalid-vendor-id",
                        "A vendor-specific AVP requires a nonzero Vendor-ID.");
                }

                AddAvpWarning(
                    warnings,
                    8,
                    "invalid-vendor-id",
                    "A vendor-specific AVP requires a nonzero Vendor-ID.");
            }
        }

        int paddingIndex = avpLength;
        bool paddingWarningAdded = false;
        while (paddingIndex < paddedLength)
        {
            if (source[paddingIndex] != 0)
            {
                if (profile == DiameterDecodeProfile.Strict)
                {
                    return CreateAvpFailure(
                        DiameterCodecStatus.Invalid,
                        paddedLength,
                        paddingIndex,
                        "nonzero-avp-padding",
                        "Diameter AVP padding octets must be zero.");
                }

                if (!paddingWarningAdded)
                {
                    AddAvpWarning(
                        warnings,
                        paddingIndex,
                        "nonzero-avp-padding",
                        "Diameter AVP padding octets must be zero.");
                    paddingWarningAdded = true;
                }
            }

            paddingIndex = paddingIndex + 1;
        }

        int dataLength = avpLength - headerLength;
        byte[] data = new byte[dataLength];
        int dataIndex = 0;
        while (dataIndex < dataLength)
        {
            data[dataIndex] = source[headerLength + dataIndex];
            dataIndex = dataIndex + 1;
        }

        DiameterAvp avp = new DiameterAvp();
        avp.Code = NetworkByteOrder.ReadUInt32(source.Slice(0, 4));
        avp.Flags = (DiameterAvpFlags)flagValue;
        avp.VendorId = vendorId;
        avp.Data = data;

        DiameterAvpDecodeResult result = new DiameterAvpDecodeResult();
        result.Status = DiameterCodecStatus.Success;
        result.RequiredCount = paddedLength;
        result.ConsumedCount = paddedLength;
        result.Avp = avp;
        int warningIndex = 0;
        while (warningIndex < warnings.Count)
        {
            result.Issues.Add(warnings[warningIndex]);
            warningIndex = warningIndex + 1;
        }

        return result;
    }

    internal static DiameterValueResult DecodeAvpSequence(ReadOnlySpan<byte> source)
    {
        DiameterValueResult result = new DiameterValueResult();
        result.Status = DiameterCodecStatus.Success;
        int consumedCount = 0;
        while (consumedCount < source.Length)
        {
            DiameterAvpDecodeResult item = DecodeAvp(source.Slice(consumedCount));
            if (item.Status != DiameterCodecStatus.Success || item.Avp == null)
            {
                result.Status = item.Status;
                if (item.RequiredCount > int.MaxValue - consumedCount)
                {
                    result.RequiredCount = int.MaxValue;
                }
                else
                {
                    result.RequiredCount = consumedCount + item.RequiredCount;
                }

                result.ConsumedCount = consumedCount;
                CopyAdjustedIssues(result, item, consumedCount);
                return result;
            }

            result.Avps.Add(item.Avp);
            consumedCount = consumedCount + item.ConsumedCount;
        }

        result.RequiredCount = consumedCount;
        result.ConsumedCount = consumedCount;
        return result;
    }

    internal static void CopyAdjustedIssues(
        DiameterValueResult destination,
        DiameterAvpDecodeResult source,
        int baseOffset)
    {
        int issueIndex = 0;
        while (issueIndex < source.Issues.Count)
        {
            DiameterCodecIssue sourceIssue = source.Issues[issueIndex];
            DiameterCodecIssue issue = new DiameterCodecIssue();
            issue.Severity = sourceIssue.Severity;
            issue.Status = sourceIssue.Status;
            issue.Offset = baseOffset + sourceIssue.Offset;
            issue.Code = sourceIssue.Code;
            issue.Message = sourceIssue.Message;
            destination.Issues.Add(issue);
            issueIndex = issueIndex + 1;
        }
    }

    internal static DiameterAvpDecodeResult CreateAvpFailure(
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

        DiameterAvpDecodeResult result = new DiameterAvpDecodeResult();
        result.Status = status;
        result.RequiredCount = requiredCount;
        result.Issues.Add(issue);
        return result;
    }

    internal static void AddAvpWarning(
        List<DiameterCodecIssue> warnings,
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
        warnings.Add(issue);
    }
}
