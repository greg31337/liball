using System;
using System.Buffers.Binary;

namespace avif;

internal class FileTypeProbeResult
{
    internal AvifResultCode Code;
    internal FileTypeBox? FileType;
    internal BrandCapabilityReport Capabilities;
    internal BrandValidationResult? Validation;

    internal FileTypeProbeResult(
        AvifResultCode code,
        FileTypeBox? fileType,
        BrandCapabilityReport capabilities,
        BrandValidationResult? validation)
    {
        Code = code;
        FileType = fileType;
        Capabilities = capabilities;
        Validation = validation;
    }
}

internal static class FileTypeProbe
{
    internal static FileTypeProbeResult Probe(
        ReadOnlySpan<byte> source,
        AvifSecurityLimits limits)
    {
        BrandCapabilityReport emptyCapabilities = new BrandCapabilityReport();
        if (ResourceGuard.ValidateLimits(limits) != AvifResultCode.Success)
        {
            return new FileTypeProbeResult(
                AvifResultCode.InvalidArgument,
                null,
                emptyCapabilities,
                null);
        }

        if (source.Length > limits.MaxSourceBytes)
        {
            return new FileTypeProbeResult(
                AvifResultCode.LimitExceeded,
                null,
                emptyCapabilities,
                null);
        }

        if (source.Length < 8)
        {
            return new FileTypeProbeResult(
                AvifResultCode.NeedMoreData,
                null,
                emptyCapabilities,
                null);
        }

        uint size32 = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(0, 4));
        uint typeValue = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(4, 4));
        if (typeValue != IsoBoxType.GetValue("ftyp"))
        {
            return new FileTypeProbeResult(
                AvifResultCode.NotFound,
                null,
                emptyCapabilities,
                null);
        }

        int headerSize = 8;
        bool usesLargeSize = size32 == 1;
        bool extendsToEnd = size32 == 0;
        ulong declaredSize = size32;
        if (usesLargeSize)
        {
            if (source.Length < 16)
            {
                return new FileTypeProbeResult(
                    AvifResultCode.NeedMoreData,
                    null,
                    emptyCapabilities,
                    null);
            }

            headerSize = 16;
            declaredSize = BinaryPrimitives.ReadUInt64BigEndian(source.Slice(8, 8));
        }

        long boxSize;
        if (extendsToEnd)
        {
            boxSize = source.Length;
        }
        else
        {
            if (declaredSize > long.MaxValue
                || declaredSize > (ulong)limits.MaxSourceBytes)
            {
                return new FileTypeProbeResult(
                    AvifResultCode.LimitExceeded,
                    null,
                    emptyCapabilities,
                    null);
            }

            boxSize = (long)declaredSize;
        }

        if (boxSize < headerSize + 8 || (boxSize - headerSize - 8) % 4 != 0)
        {
            return new FileTypeProbeResult(
                AvifResultCode.InvalidContainer,
                null,
                emptyCapabilities,
                null);
        }

        if (boxSize > source.Length)
        {
            return new FileTypeProbeResult(
                AvifResultCode.NeedMoreData,
                null,
                emptyCapabilities,
                null);
        }

        IsoBox box = new IsoBox();
        box.TypeValue = typeValue;
        box.Type = "ftyp";
        box.Offset = 0;
        box.Size = boxSize;
        box.HeaderSize = headerSize;
        box.PayloadOffset = headerSize;
        box.PayloadSize = boxSize - headerSize;
        box.EndOffset = boxSize;
        box.UsesLargeSize = usesLargeSize;
        box.ExtendsToEnd = extendsToEnd;
        box.IsUnknown = true;

        ResourceGuard resources = new ResourceGuard(limits);
        FileTypeBox fileType = FileTypeBox.Parse(source, box, resources);
        if (fileType.Code != AvifResultCode.Success)
        {
            return new FileTypeProbeResult(
                fileType.Code,
                fileType,
                emptyCapabilities,
                null);
        }

        BrandCapabilityReport capabilities = BrandResolver.Resolve(fileType);
        BrandValidationResult validation = BrandValidator.Validate(fileType, null);
        return new FileTypeProbeResult(
            AvifResultCode.Success,
            fileType,
            capabilities,
            validation);
    }
}
