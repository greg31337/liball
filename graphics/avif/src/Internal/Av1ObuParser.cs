using System;
using System.Collections.Generic;

namespace avif;

internal class Av1LengthParseResult
{
    internal AvifResultCode Code;
    internal ulong Value;
    internal int ByteCount;

    internal Av1LengthParseResult(AvifResultCode code, ulong value, int byteCount)
    {
        Code = code;
        Value = value;
        ByteCount = byteCount;
    }
}

internal class Av1HeaderParseResult
{
    internal AvifResultCode Code;
    internal Av1ObuHeader? Header;

    internal Av1HeaderParseResult(AvifResultCode code, Av1ObuHeader? header)
    {
        Code = code;
        Header = header;
    }
}

internal class Av1SingleObuParseResult
{
    internal AvifResultCode Code;
    internal Av1Obu? Obu;
    internal int ConsumedBytes;

    internal Av1SingleObuParseResult(
        AvifResultCode code,
        Av1Obu? obu,
        int consumedBytes)
    {
        Code = code;
        Obu = obu;
        ConsumedBytes = consumedBytes;
    }
}

internal class Av1ObuParserState
{
    internal AvifResultCode Code;
    internal List<Av1TemporalUnit> TemporalUnits;
    internal List<Av1Obu> Obus;
    internal int DroppedObuCount;
    internal ResourceGuard Resources;
    internal DiagnosticLog Diagnostics;

    internal Av1ObuParserState(AvifSecurityLimits? limits, int sourceLength)
    {
        TemporalUnits = new List<Av1TemporalUnit>();
        Obus = new List<Av1Obu>();
        DroppedObuCount = 0;
        Resources = new ResourceGuard(limits);
        Diagnostics = new DiagnosticLog();
        Code = Resources.Code;
        if (Code == AvifResultCode.Success)
        {
            Code = Resources.CountSourceBytes(sourceLength);
        }
    }

    internal Av1ObuParseResult ToResult()
    {
        return new Av1ObuParseResult(
            Code,
            TemporalUnits,
            Obus,
            DroppedObuCount,
            Resources,
            Diagnostics);
    }
}

// AV1 specification sections 5.2 through 5.3 and Annex B.
internal static class Av1ObuParser
{
    internal static Av1ObuParseResult ParseLowOverhead(
        ReadOnlySpan<byte> source,
        AvifSecurityLimits? limits,
        int operatingPointIdc)
    {
        Av1ObuParserState state = new Av1ObuParserState(limits, source.Length);
        if (state.Code != AvifResultCode.Success)
        {
            ReportInitializationFailure(state);
            return state.ToResult();
        }

        if (!IsValidOperatingPointIdc(operatingPointIdc))
        {
            Fail(
                state,
                AvifResultCode.InvalidArgument,
                "The operating point identifier must be a 12-bit value.",
                0,
                "AV1 section 6.4.1");
            return state.ToResult();
        }

        int cursor = 0;
        Av1TemporalUnit? currentTemporalUnit = null;
        while (cursor < source.Length)
        {
            Av1SingleObuParseResult parsed = ParseObu(
                source,
                cursor,
                source.Length - cursor,
                true,
                false,
                AvifResultCode.TruncatedData,
                operatingPointIdc,
                state.Resources);
            if (parsed.Code != AvifResultCode.Success)
            {
                Fail(
                    state,
                    parsed.Code,
                    "The low-overhead OBU is malformed or truncated.",
                    cursor,
                    "AV1 sections 5.2 and 5.3");
                return state.ToResult();
            }

            Av1Obu? obu = parsed.Obu;
            if ((obu ?? throw new InvalidOperationException("Invalid internal state.")).Header.Type == Av1ObuType.TemporalDelimiter
                || currentTemporalUnit == null)
            {
                currentTemporalUnit = new Av1TemporalUnit(obu.Offset, 0);
                state.TemporalUnits.Add(currentTemporalUnit);
            }

            AddObu(state, currentTemporalUnit, null, obu);
            currentTemporalUnit.Size += parsed.ConsumedBytes;
            cursor += parsed.ConsumedBytes;
        }

        return state.ToResult();
    }

    internal static Av1ObuParseResult ParseLengthDelimited(
        ReadOnlySpan<byte> source,
        AvifSecurityLimits limits,
        int operatingPointIdc)
    {
        Av1ObuParserState state = new Av1ObuParserState(limits, source.Length);
        if (state.Code != AvifResultCode.Success)
        {
            ReportInitializationFailure(state);
            return state.ToResult();
        }

        if (!IsValidOperatingPointIdc(operatingPointIdc))
        {
            Fail(
                state,
                AvifResultCode.InvalidArgument,
                "The operating point identifier must be a 12-bit value.",
                0,
                "AV1 section 6.4.1");
            return state.ToResult();
        }

        int cursor = 0;
        while (cursor < source.Length)
        {
            Av1LengthParseResult temporalLength = ReadLength(
                source,
                cursor,
                source.Length,
                AvifResultCode.TruncatedData);
            if (temporalLength.Code != AvifResultCode.Success)
            {
                Fail(
                    state,
                    temporalLength.Code,
                    "The temporal unit length is malformed or truncated.",
                    cursor,
                    "AV1 Annex B");
                return state.ToResult();
            }

            cursor += temporalLength.ByteCount;
            int temporalSize = GetBoundedSize(
                temporalLength.Value,
                source.Length - cursor);
            if (temporalSize < 0)
            {
                AvifResultCode sizeCode = temporalLength.Value > int.MaxValue
                    ? AvifResultCode.LimitExceeded
                    : AvifResultCode.TruncatedData;
                Fail(
                    state,
                    sizeCode,
                    "The declared temporal unit exceeds the available source.",
                    cursor,
                    "AV1 Annex B");
                return state.ToResult();
            }

            if (temporalSize == 0)
            {
                Fail(
                    state,
                    AvifResultCode.InvalidAv1Bitstream,
                    "A temporal unit must contain at least one frame unit.",
                    cursor,
                    "AV1 Annex B");
                return state.ToResult();
            }

            int temporalEnd = cursor + temporalSize;
            Av1TemporalUnit temporalUnit = new Av1TemporalUnit(cursor, temporalSize);
            int frameIndex = 0;
            while (cursor < temporalEnd)
            {
                Av1LengthParseResult frameLength = ReadLength(
                    source,
                    cursor,
                    temporalEnd,
                    AvifResultCode.InvalidAv1Bitstream);
                if (frameLength.Code != AvifResultCode.Success)
                {
                    Fail(
                        state,
                        frameLength.Code,
                        "The frame unit length is malformed or crosses its temporal unit.",
                        cursor,
                        "AV1 Annex B");
                    return state.ToResult();
                }

                cursor += frameLength.ByteCount;
                int frameSize = GetBoundedSize(
                    frameLength.Value,
                    temporalEnd - cursor);
                if (frameSize <= 0)
                {
                    AvifResultCode frameCode = frameLength.Value > int.MaxValue
                        ? AvifResultCode.LimitExceeded
                        : AvifResultCode.InvalidAv1Bitstream;
                    Fail(
                        state,
                        frameCode,
                        "The frame unit is empty or exceeds its temporal unit.",
                        cursor,
                        "AV1 Annex B");
                    return state.ToResult();
                }

                AvifResultCode frameCountCode = state.Resources.CountFrames(1);
                if (frameCountCode != AvifResultCode.Success)
                {
                    Fail(
                        state,
                        frameCountCode,
                        "The frame unit count exceeds the configured frame limit.",
                        cursor,
                        "AV1 Annex B");
                    return state.ToResult();
                }

                int frameEnd = cursor + frameSize;
                Av1FrameUnit frameUnit = new Av1FrameUnit(cursor, frameSize);
                int obuIndex = 0;
                while (cursor < frameEnd)
                {
                    Av1LengthParseResult obuLength = ReadLength(
                        source,
                        cursor,
                        frameEnd,
                        AvifResultCode.InvalidAv1Bitstream);
                    if (obuLength.Code != AvifResultCode.Success)
                    {
                        Fail(
                            state,
                            obuLength.Code,
                            "The OBU length is malformed or crosses its frame unit.",
                            cursor,
                            "AV1 Annex B");
                        return state.ToResult();
                    }

                    cursor += obuLength.ByteCount;
                    int obuSize = GetBoundedSize(
                        obuLength.Value,
                        frameEnd - cursor);
                    if (obuSize <= 0)
                    {
                        AvifResultCode obuCode = obuLength.Value > int.MaxValue
                            ? AvifResultCode.LimitExceeded
                            : AvifResultCode.InvalidAv1Bitstream;
                        Fail(
                            state,
                            obuCode,
                            "The OBU is empty or exceeds its frame unit.",
                            cursor,
                            "AV1 Annex B");
                        return state.ToResult();
                    }

                    Av1SingleObuParseResult parsed = ParseObu(
                        source,
                        cursor,
                        obuSize,
                        false,
                        true,
                        AvifResultCode.InvalidAv1Bitstream,
                        operatingPointIdc,
                        state.Resources);
                    if (parsed.Code != AvifResultCode.Success)
                    {
                        Fail(
                            state,
                            parsed.Code,
                            "The length-delimited OBU is malformed.",
                            cursor,
                            "AV1 sections 5.3 and Annex B");
                        return state.ToResult();
                    }

                    bool firstObu = frameIndex == 0 && obuIndex == 0;
                    bool isTemporalDelimiter =
                        (parsed.Obu ?? throw new InvalidOperationException("Invalid internal state.")).Header.Type == Av1ObuType.TemporalDelimiter;
                    if (firstObu != isTemporalDelimiter)
                    {
                        Fail(
                            state,
                            AvifResultCode.InvalidAv1Bitstream,
                            "A temporal delimiter must appear only as the first OBU of a temporal unit.",
                            cursor,
                            "AV1 Annex B");
                        return state.ToResult();
                    }

                    AddObu(state, temporalUnit, frameUnit, parsed.Obu);
                    cursor += obuSize;
                    obuIndex++;
                }

                temporalUnit.FrameUnits.Add(frameUnit);
                frameIndex++;
            }

            state.TemporalUnits.Add(temporalUnit);
        }

        return state.ToResult();
    }

    internal static bool ShouldIncludeObu(Av1ObuHeader header, int operatingPointIdc)
    {
        if (header == null || !IsValidOperatingPointIdc(operatingPointIdc))
        {
            return false;
        }

        if (header.Type == Av1ObuType.SequenceHeader
            || header.Type == Av1ObuType.TemporalDelimiter
            || operatingPointIdc == 0
            || !header.HasExtension)
        {
            return true;
        }

        int temporalMask = 1 << header.TemporalId;
        int spatialMask = 1 << (header.SpatialId + 8);
        return (operatingPointIdc & temporalMask) != 0
            && (operatingPointIdc & spatialMask) != 0;
    }

    static Av1SingleObuParseResult ParseObu(
        ReadOnlySpan<byte> source,
        int offset,
        int availableLength,
        bool requireSizeField,
        bool requireExactLength,
        AvifResultCode boundaryCode,
        int operatingPointIdc,
        ResourceGuard resources)
    {
        Av1HeaderParseResult headerResult = ParseHeader(
            source,
            offset,
            availableLength,
            boundaryCode);
        if (headerResult.Code != AvifResultCode.Success)
        {
            return new Av1SingleObuParseResult(headerResult.Code, null, 0);
        }

        Av1ObuHeader? header = headerResult.Header;
        if (requireSizeField && !(header ?? throw new InvalidOperationException("Invalid internal state.")).HasSizeField)
        {
            return new Av1SingleObuParseResult(
                AvifResultCode.InvalidAv1Bitstream,
                null,
                0);
        }

        int payloadSize;
        int sizeFieldSize = 0;
        if ((header ?? throw new InvalidOperationException("Invalid internal state.")).HasSizeField)
        {
            Av1LengthParseResult sizeResult = ReadLength(
                source,
                offset + header.HeaderSize,
                offset + availableLength,
                boundaryCode);
            if (sizeResult.Code != AvifResultCode.Success)
            {
                return new Av1SingleObuParseResult(sizeResult.Code, null, 0);
            }

            if (sizeResult.Value > int.MaxValue)
            {
                return new Av1SingleObuParseResult(
                    AvifResultCode.LimitExceeded,
                    null,
                    0);
            }

            payloadSize = (int)sizeResult.Value;
            sizeFieldSize = sizeResult.ByteCount;
        }
        else
        {
            payloadSize = availableLength - header.HeaderSize;
        }

        long totalSizeLong = (long)header.HeaderSize + sizeFieldSize + payloadSize;
        if (totalSizeLong > availableLength)
        {
            return new Av1SingleObuParseResult(boundaryCode, null, 0);
        }

        int totalSize = (int)totalSizeLong;
        if (requireExactLength && totalSize != availableLength)
        {
            return new Av1SingleObuParseResult(
                AvifResultCode.InvalidAv1Bitstream,
                null,
                0);
        }

        int payloadOffset = offset + header.HeaderSize + sizeFieldSize;
        AvifResultCode reserveCode = resources.ReserveTemporaryBytes(payloadSize);
        if (reserveCode != AvifResultCode.Success)
        {
            return new Av1SingleObuParseResult(reserveCode, null, 0);
        }
        bool include = ShouldIncludeObu(header, operatingPointIdc);
        Av1Obu obu = new Av1Obu(
            header,
            offset,
            sizeFieldSize,
            payloadOffset,
            payloadSize,
            totalSize,
            include,
            source.Slice(payloadOffset, payloadSize));

        if (header.Type == Av1ObuType.TemporalDelimiter
            && obu.ValidateTrailingBits(0) != AvifResultCode.Success)
        {
            return new Av1SingleObuParseResult(
                AvifResultCode.InvalidAv1Bitstream,
                null,
                0);
        }

        return new Av1SingleObuParseResult(AvifResultCode.Success, obu, totalSize);
    }

    static Av1HeaderParseResult ParseHeader(
        ReadOnlySpan<byte> source,
        int offset,
        int availableLength,
        AvifResultCode boundaryCode)
    {
        if (offset < 0 || availableLength < 1 || offset >= source.Length)
        {
            return new Av1HeaderParseResult(boundaryCode, null);
        }

        byte first = source[offset];
        if ((first & 128) != 0 || (first & 1) != 0)
        {
            return new Av1HeaderParseResult(AvifResultCode.InvalidAv1Bitstream, null);
        }

        Av1ObuType type = (Av1ObuType)((first >> 3) & 15);
        bool hasExtension = (first & 4) != 0;
        bool hasSizeField = (first & 2) != 0;
        int temporalId = 0;
        int spatialId = 0;
        int headerSize = 1;
        if (hasExtension)
        {
            if (availableLength < 2 || offset > source.Length - 2)
            {
                return new Av1HeaderParseResult(boundaryCode, null);
            }

            byte extension = source[offset + 1];
            if ((extension & 7) != 0)
            {
                return new Av1HeaderParseResult(
                    AvifResultCode.InvalidAv1Bitstream,
                    null);
            }

            temporalId = extension >> 5;
            spatialId = extension >> 3 & 3;
            headerSize = 2;
        }

        if (hasExtension
            && (type == Av1ObuType.SequenceHeader
                || type == Av1ObuType.TemporalDelimiter
                || type == Av1ObuType.TileList))
        {
            return new Av1HeaderParseResult(AvifResultCode.InvalidAv1Bitstream, null);
        }

        return new Av1HeaderParseResult(
            AvifResultCode.Success,
            new Av1ObuHeader(
                type,
                hasExtension,
                hasSizeField,
                temporalId,
                spatialId,
                headerSize));
    }

    static Av1LengthParseResult ReadLength(
        ReadOnlySpan<byte> source,
        int offset,
        int limit,
        AvifResultCode boundaryCode)
    {
        if (offset < 0 || limit < offset || limit > source.Length)
        {
            return new Av1LengthParseResult(AvifResultCode.InvalidArgument, 0, 0);
        }

        ByteReader reader = new ByteReader(limit);
        reader.Position = offset;
        Av1Leb128ParseResult parsed = Av1SequenceHeaderParser.ReadLeb128(source, reader);
        if (parsed.Code != AvifResultCode.Success)
        {
            AvifResultCode code = parsed.Code;
            if (code == AvifResultCode.TruncatedData
                && boundaryCode != AvifResultCode.TruncatedData)
            {
                code = boundaryCode;
            }

            return new Av1LengthParseResult(code, 0, 0);
        }

        return new Av1LengthParseResult(
            AvifResultCode.Success,
            parsed.Value,
            reader.Position - offset);
    }

    static int GetBoundedSize(ulong value, int availableLength)
    {
        if (value > int.MaxValue || value > (ulong)availableLength)
        {
            return -1;
        }

        return (int)value;
    }

    static bool IsValidOperatingPointIdc(int operatingPointIdc)
    {
        return operatingPointIdc >= 0 && operatingPointIdc <= 4095;
    }

    static void AddObu(
        Av1ObuParserState state,
        Av1TemporalUnit temporalUnit,
        Av1FrameUnit? frameUnit,
        Av1Obu obu)
    {
        state.Obus.Add(obu);
        temporalUnit.Obus.Add(obu);
        if (frameUnit != null)
        {
            frameUnit.Obus.Add(obu);
        }

        if (!obu.InOperatingPoint)
        {
            state.DroppedObuCount++;
        }
    }

    static void ReportInitializationFailure(Av1ObuParserState state)
    {
        AvifDiagnosticCategory category = state.Code == AvifResultCode.LimitExceeded
            ? AvifDiagnosticCategory.Resource
            : AvifDiagnosticCategory.Api;
        state.Diagnostics.Context.SetLocation(0, 0);
        state.Diagnostics.Context.SetStandardReference("AV1 section 5.2");
        state.Diagnostics.Add(
            state.Code,
            AvifDiagnosticSeverity.Error,
            category,
            "The configured limits are invalid or the source exceeds them.");
    }

    static void Fail(
        Av1ObuParserState state,
        AvifResultCode code,
        string message,
        int offset,
        string standardReference)
    {
        state.Code = code;
        state.Diagnostics.Context.SetLocation(offset, 0);
        state.Diagnostics.Context.SetStandardReference(standardReference);
        AvifDiagnosticCategory category = code == AvifResultCode.LimitExceeded
            ? AvifDiagnosticCategory.Resource
            : AvifDiagnosticCategory.Av1;
        state.Diagnostics.Add(
            code,
            AvifDiagnosticSeverity.Error,
            category,
            message);
    }
}
