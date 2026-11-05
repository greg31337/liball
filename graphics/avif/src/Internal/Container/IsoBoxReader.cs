using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace avif;

internal static class IsoBoxReader
{
    internal static IsoBoxParseResult Parse(
        ReadOnlySpan<byte> source,
        AvifSecurityLimits limits,
        bool preserveUnknown)
    {
        ResourceGuard resources = new ResourceGuard(limits);
        DiagnosticLog diagnostics = new DiagnosticLog();
        List<IsoBox> boxes = new List<IsoBox>();
        if (resources.Code != AvifResultCode.Success)
        {
            Report(
                diagnostics,
                resources.Code,
                AvifDiagnosticCategory.Resource,
                "The configured resource limits are invalid.",
                0);
            return new IsoBoxParseResult(resources.Code, boxes, resources, diagnostics);
        }

        AvifResultCode sourceCode = resources.CountSourceBytes(source.Length);
        if (sourceCode != AvifResultCode.Success)
        {
            Report(
                diagnostics,
                sourceCode,
                AvifDiagnosticCategory.Resource,
                "The source exceeds the configured byte limit.",
                0);
            return new IsoBoxParseResult(sourceCode, boxes, resources, diagnostics);
        }

        AvifResultCode code = ParseRange(
            source,
            0,
            source.Length,
            source.Length,
            boxes,
            resources,
            diagnostics,
            preserveUnknown,
            string.Empty);
        return new IsoBoxParseResult(code, boxes, resources, diagnostics);
    }

    internal static AvifResultCode ParseRange(
        ReadOnlySpan<byte> source,
        long start,
        long end,
        long sourceEnd,
        List<IsoBox> boxes,
        ResourceGuard resources,
        DiagnosticLog diagnostics,
        bool preserveUnknown,
        string parentType)
    {
        long cursor = start;
        while (cursor < end)
        {
            long remaining = end - cursor;
            if (remaining < 8)
            {
                Report(
                    diagnostics,
                    AvifResultCode.TruncatedData,
                    AvifDiagnosticCategory.Container,
                    "The remaining bytes cannot contain an ISO box header.",
                    cursor);
                return AvifResultCode.TruncatedData;
            }

            AvifResultCode enterCode = resources.EnterBox();
            if (enterCode != AvifResultCode.Success)
            {
                Report(
                    diagnostics,
                    enterCode,
                    AvifDiagnosticCategory.Resource,
                    "The ISO box count or nesting depth exceeds its configured limit.",
                    cursor);
                return enterCode;
            }

            int headerOffset = (int)cursor;
            uint size32 = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(headerOffset, 4));
            uint typeValue = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(headerOffset + 4, 4));
            string type = new FourCc(typeValue).Text;
            diagnostics.Context.PushBox(GetPathSegment(type));

            int headerSize = 8;
            bool usesLargeSize = size32 == 1;
            bool extendsToEnd = size32 == 0;
            ulong declaredSize = size32;
            if (usesLargeSize)
            {
                if (remaining < 16)
                {
                    return FailBox(
                        resources,
                        diagnostics,
                        AvifResultCode.TruncatedData,
                        "The large-size ISO box header is truncated.",
                        cursor);
                }

                declaredSize = BinaryPrimitives.ReadUInt64BigEndian(source.Slice(headerOffset + 8, 8));
                headerSize = 16;
            }

            bool isUuid = typeValue == IsoBoxType.Uuid;
            byte[] userType = System.Array.Empty<byte>();
            if (isUuid)
            {
                if (remaining < headerSize + 16)
                {
                    return FailBox(
                        resources,
                        diagnostics,
                        AvifResultCode.TruncatedData,
                        "The UUID ISO box header is truncated.",
                        cursor);
                }

                userType = source.Slice(headerOffset + headerSize, 16).ToArray();
                headerSize += 16;
            }

            long boxSize;
            if (extendsToEnd)
            {
                boxSize = end - cursor;
            }
            else
            {
                if (declaredSize > long.MaxValue)
                {
                    return FailBox(
                        resources,
                        diagnostics,
                        AvifResultCode.LimitExceeded,
                        "The declared ISO box size cannot be represented.",
                        cursor);
                }

                boxSize = (long)declaredSize;
            }

            if (boxSize < headerSize)
            {
                return FailBox(
                    resources,
                    diagnostics,
                    AvifResultCode.InvalidContainer,
                    "The declared ISO box size is smaller than its header.",
                    cursor);
            }

            CheckedIntegerResult boxEndResult = CheckedMath.AddInt64(cursor, boxSize);
            if (!boxEndResult.Success)
            {
                return FailBox(
                    resources,
                    diagnostics,
                    boxEndResult.Code,
                    "The declared ISO box end offset overflows.",
                    cursor);
            }

            long boxEnd = boxEndResult.Value;
            if (boxEnd > end)
            {
                AvifResultCode rangeCode = boxEnd > sourceEnd
                    ? AvifResultCode.TruncatedData
                    : AvifResultCode.InvalidContainer;
                return FailBox(
                    resources,
                    diagnostics,
                    rangeCode,
                    "The declared ISO box extends beyond its containing range.",
                    cursor);
            }

            IsoBox box = new IsoBox();
            box.TypeValue = typeValue;
            box.Type = type;
            box.Offset = cursor;
            box.Size = boxSize;
            box.HeaderSize = headerSize;
            box.PayloadOffset = cursor + headerSize;
            box.PayloadSize = boxSize - headerSize;
            box.EndOffset = boxEnd;
            box.UsesLargeSize = usesLargeSize;
            box.ExtendsToEnd = extendsToEnd;
            box.IsUuid = isUuid;
            box.UserType = userType;
            box.IsContainer = parentType != "ipco"
                && parentType != "iref"
                && parentType != "dref"
                && parentType != "grpl"
                && IsoBoxType.IsContainer(type);
            box.IsUnknown = !box.IsContainer;
            if (box.IsUnknown && preserveUnknown)
            {
                AvifResultCode preserveCode = resources.ReserveTemporaryBytes(boxSize);
                if (preserveCode != AvifResultCode.Success)
                {
                    return FailBox(
                        resources,
                        diagnostics,
                        preserveCode,
                        "Preserving the opaque ISO box exceeds the temporary byte limit.",
                        cursor);
                }

                box.RawData = source.Slice((int)cursor, (int)boxSize).ToArray();
            }

            boxes.Add(box);

            long childOffset = box.PayloadOffset;
            if (box.IsContainer && IsoBoxType.IsFullBoxContainer(type))
            {
                IsoFullBox fullBox = IsoFullBox.Parse(source, box);
                if (fullBox.Code != AvifResultCode.Success)
                {
                    return FailBox(
                        resources,
                        diagnostics,
                        fullBox.Code,
                        "The FullBox fields are truncated or invalid.",
                        box.PayloadOffset);
                }

                box.HasFullBoxFields = true;
                box.Version = fullBox.Version;
                box.Flags = fullBox.Flags;
                childOffset = fullBox.PayloadOffset;

                if (type == "iinf" || type == "dref")
                {
                    int countSize;
                    if (type == "dref")
                    {
                        countSize = 4;
                    }
                    else if (fullBox.Version == 0)
                    {
                        countSize = 2;
                    }
                    else if (fullBox.Version == 1)
                    {
                        countSize = 4;
                    }
                    else
                    {
                        return FailBox(
                            resources,
                            diagnostics,
                            AvifResultCode.UnsupportedFeature,
                            "The ItemInfoBox version is not supported.",
                            box.PayloadOffset);
                    }

                    if (fullBox.PayloadSize < countSize)
                    {
                        return FailBox(
                            resources,
                            diagnostics,
                            AvifResultCode.InvalidContainer,
                            "The ItemInfoBox entry count is truncated.",
                            fullBox.PayloadOffset);
                    }

                    childOffset += countSize;
                }
            }

            if (box.IsContainer)
            {
                AvifResultCode childCode = ParseRange(
                    source,
                    childOffset,
                    boxEnd,
                    sourceEnd,
                    box.Children,
                    resources,
                    diagnostics,
                    preserveUnknown,
                    type);
                if (childCode != AvifResultCode.Success)
                {
                    diagnostics.Context.PopBox();
                    resources.LeaveBox();
                    return childCode;
                }
            }

            diagnostics.Context.PopBox();
            resources.LeaveBox();
            cursor = boxEnd;
        }

        return AvifResultCode.Success;
    }

    internal static AvifResultCode FailBox(
        ResourceGuard resources,
        DiagnosticLog diagnostics,
        AvifResultCode code,
        string message,
        long offset)
    {
        AvifDiagnosticCategory category = code == AvifResultCode.LimitExceeded
            ? AvifDiagnosticCategory.Resource
            : AvifDiagnosticCategory.Container;
        Report(diagnostics, code, category, message, offset);
        diagnostics.Context.PopBox();
        resources.LeaveBox();
        return code;
    }

    internal static void Report(
        DiagnosticLog diagnostics,
        AvifResultCode code,
        AvifDiagnosticCategory category,
        string message,
        long offset)
    {
        diagnostics.Context.SetLocation(offset, AvifDiagnostic.UnknownBitOffset);
        diagnostics.Add(code, AvifDiagnosticSeverity.Error, category, message);
        diagnostics.Context.ClearLocation();
    }

    internal static string GetPathSegment(string type)
    {
        return type.Replace("/", "%2F");
    }
}
