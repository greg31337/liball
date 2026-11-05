using System;

namespace avif;

// AV1 specification section 5.5.
internal static class Av1SequenceHeaderWriter
{
    internal static Av1WriteResult WritePayload(ParsedAv1SequenceHeader? header)
    {
        if (header == null)
        {
            return Fail(AvifResultCode.InvalidArgument);
        }

        byte[] payload = new byte[1024];
        BitWriter writer = BitWriter.Create(payload.Length);
        int profile = header.GetSequenceProfile();
        if (profile < 0 || profile > 2
            || Av1EncoderProfileSelector.ValidateHeader(header)
                != AvifResultCode.Success
            || header.GetOperatingPointCount() < 1
            || header.GetOperatingPointCount() > 32
            || header.GetFrameWidthBits() < 1 || header.GetFrameWidthBits() > 16
            || header.GetFrameHeightBits() < 1 || header.GetFrameHeightBits() > 16
            || header.GetMaximumWidth() < 1 || header.GetMaximumHeight() < 1
            || (ulong)(header.GetMaximumWidth() - 1)
                >= 1UL << header.GetFrameWidthBits()
            || (ulong)(header.GetMaximumHeight() - 1)
                >= 1UL << header.GetFrameHeightBits())
        {
            return Fail(AvifResultCode.InvalidArgument);
        }

        writer.WriteBits(payload, (ulong)profile, 3);
        writer.WriteBit(payload, header.IsStillPicture() ? 1 : 0);
        writer.WriteBit(payload, header.HasReducedStillPictureHeader() ? 1 : 0);
        if (header.HasReducedStillPictureHeader())
        {
            Av1OperatingPoint? point = header.GetOperatingPoint(0);
            if (!header.IsStillPicture() || point == null
                || !Av1SequenceHeaderParser.IsValidLevel(point.Level))
            {
                return Fail(AvifResultCode.InvalidArgument);
            }

            writer.WriteBits(payload, (ulong)point.Level, 5);
        }
        else
        {
            WriteOperatingPoints(payload, writer, header);
        }

        writer.WriteBits(payload, (ulong)(header.GetFrameWidthBits() - 1), 4);
        writer.WriteBits(payload, (ulong)(header.GetFrameHeightBits() - 1), 4);
        writer.WriteBits(
            payload,
            header.GetMaximumWidth() - 1,
            header.GetFrameWidthBits());
        writer.WriteBits(
            payload,
            header.GetMaximumHeight() - 1,
            header.GetFrameHeightBits());
        if (!header.HasReducedStillPictureHeader())
        {
            writer.WriteBit(payload, header.HasFrameIdNumbers() ? 1 : 0);
        }

        if (header.HasFrameIdNumbers())
        {
            if (header.GetDeltaFrameIdLength() < 2
                || header.GetDeltaFrameIdLength() > 17
                || header.GetAdditionalFrameIdLength() < 1
                || header.GetAdditionalFrameIdLength() > 8)
            {
                writer.Code = AvifResultCode.InvalidArgument;
            }
            else
            {
                writer.WriteBits(
                    payload,
                    (ulong)(header.GetDeltaFrameIdLength() - 2),
                    4);
                writer.WriteBits(
                    payload,
                    (ulong)(header.GetAdditionalFrameIdLength() - 1),
                    3);
            }
        }

        writer.WriteBit(payload, header.Uses128x128Superblocks() ? 1 : 0);
        writer.WriteBit(payload, header.IsFilterIntraEnabled() ? 1 : 0);
        writer.WriteBit(payload, header.IsIntraEdgeFilterEnabled() ? 1 : 0);
        if (!header.HasReducedStillPictureHeader())
        {
            WriteGeneralFeatures(payload, writer, header);
        }

        writer.WriteBit(payload, header.IsSuperresEnabled() ? 1 : 0);
        writer.WriteBit(payload, header.IsCdefEnabled() ? 1 : 0);
        writer.WriteBit(payload, header.IsRestorationEnabled() ? 1 : 0);
        WriteColorConfiguration(payload, writer, header);
        writer.WriteBit(payload, header.HasFilmGrainParameters() ? 1 : 0);
        writer.WriteTrailingBits(payload);
        if (writer.Code != AvifResultCode.Success)
        {
            return Fail(writer.Code);
        }

        int byteCount = (int)(writer.BitPosition / 8);
        byte[] compact = new byte[byteCount];
        payload.AsSpan(0, byteCount).CopyTo(compact);
        payload = compact;
        Av1SequenceHeaderParseResult parsed =
            Av1SequenceHeaderParser.ParsePayload(payload);
        if (parsed.Code != AvifResultCode.Success)
        {
            return Fail(parsed.Code);
        }

        return new Av1WriteResult(AvifResultCode.Success, payload);
    }

    static void WriteOperatingPoints(
        byte[] payload,
        BitWriter writer,
        ParsedAv1SequenceHeader header)
    {
        Av1TimingInfo? timing = header.GetTimingInfo();
        Av1DecoderModelInfo? model = header.GetDecoderModelInfo();
        writer.WriteBit(payload, timing == null ? 0 : 1);
        if (timing != null)
        {
            if (timing.NumUnitsInDisplayTick == 0 || timing.TimeScale == 0)
            {
                writer.Code = AvifResultCode.InvalidArgument;
                return;
            }

            writer.WriteBits(payload, timing.NumUnitsInDisplayTick, 32);
            writer.WriteBits(payload, timing.TimeScale, 32);
            writer.WriteBit(payload, timing.EqualPictureInterval ? 1 : 0);
            if (timing.EqualPictureInterval)
            {
                writer.WriteUvlc(payload, timing.NumTicksPerPictureMinusOne);
            }

            writer.WriteBit(payload, model == null ? 0 : 1);
            if (model != null)
            {
                if (model.BufferDelayLength < 1 || model.BufferDelayLength > 32
                    || model.BufferRemovalTimeLength < 1
                    || model.BufferRemovalTimeLength > 32
                    || model.FramePresentationTimeLength < 1
                    || model.FramePresentationTimeLength > 32
                    || model.NumUnitsInDecodingTick == 0)
                {
                    writer.Code = AvifResultCode.InvalidArgument;
                    return;
                }

                writer.WriteBits(payload, (ulong)(model.BufferDelayLength - 1), 5);
                writer.WriteBits(payload, model.NumUnitsInDecodingTick, 32);
                writer.WriteBits(
                    payload,
                    (ulong)(model.BufferRemovalTimeLength - 1),
                    5);
                writer.WriteBits(
                    payload,
                    (ulong)(model.FramePresentationTimeLength - 1),
                    5);
            }
        }
        else if (model != null)
        {
            writer.Code = AvifResultCode.InvalidArgument;
            return;
        }

        writer.WriteBit(payload, header.HasInitialDisplayDelay() ? 1 : 0);
        writer.WriteBits(payload, (ulong)(header.GetOperatingPointCount() - 1), 5);
        int index = 0;
        while (index < header.GetOperatingPointCount())
        {
            Av1OperatingPoint? point = header.GetOperatingPoint(index);
            if (point == null || point.Idc < 0 || point.Idc > 4095
                || !Av1SequenceHeaderParser.IsValidLevel(point.Level)
                || point.Tier < 0 || point.Tier > 1)
            {
                writer.Code = AvifResultCode.InvalidArgument;
                return;
            }

            writer.WriteBits(payload, (ulong)point.Idc, 12);
            writer.WriteBits(payload, (ulong)point.Level, 5);
            if (point.Level > 7)
            {
                writer.WriteBit(payload, point.Tier);
            }

            if (model != null)
            {
                writer.WriteBit(payload, point.DecoderModelPresent ? 1 : 0);
                if (point.DecoderModelPresent)
                {
                    if (!FitsUnsigned(point.DecoderBufferDelay, model.BufferDelayLength)
                        || !FitsUnsigned(
                            point.EncoderBufferDelay,
                            model.BufferDelayLength))
                    {
                        writer.Code = AvifResultCode.InvalidArgument;
                        return;
                    }

                    writer.WriteBits(
                        payload,
                        point.DecoderBufferDelay,
                        model.BufferDelayLength);
                    writer.WriteBits(
                        payload,
                        point.EncoderBufferDelay,
                        model.BufferDelayLength);
                    writer.WriteBit(payload, point.LowDelayMode ? 1 : 0);
                }
            }

            if (header.HasInitialDisplayDelay())
            {
                writer.WriteBit(
                    payload,
                    point.InitialDisplayDelayPresent ? 1 : 0);
                if (point.InitialDisplayDelayPresent)
                {
                    if (point.InitialDisplayDelayMinusOne < 0
                        || point.InitialDisplayDelayMinusOne > 15)
                    {
                        writer.Code = AvifResultCode.InvalidArgument;
                        return;
                    }

                    writer.WriteBits(
                        payload,
                        (ulong)point.InitialDisplayDelayMinusOne,
                        4);
                }
            }

            index++;
        }
    }

    static void WriteGeneralFeatures(
        byte[] payload,
        BitWriter writer,
        ParsedAv1SequenceHeader header)
    {
        writer.WriteBit(payload, header.IsInterIntraCompoundEnabled() ? 1 : 0);
        writer.WriteBit(payload, header.IsMaskedCompoundEnabled() ? 1 : 0);
        writer.WriteBit(payload, header.IsWarpedMotionEnabled() ? 1 : 0);
        writer.WriteBit(payload, header.IsDualFilterEnabled() ? 1 : 0);
        writer.WriteBit(payload, header.IsOrderHintEnabled() ? 1 : 0);
        if (header.IsOrderHintEnabled())
        {
            writer.WriteBit(payload, header.IsJointCompoundEnabled() ? 1 : 0);
            writer.WriteBit(
                payload,
                header.AreReferenceFrameMotionVectorsEnabled() ? 1 : 0);
        }

        writer.WriteBit(payload, header.ChoosesScreenContentTools() ? 1 : 0);
        if (!header.ChoosesScreenContentTools())
        {
            writer.WriteBit(payload, header.GetScreenContentToolsMode());
        }

        if (header.GetScreenContentToolsMode() > 0)
        {
            writer.WriteBit(payload, header.ChoosesIntegerMotionVectors() ? 1 : 0);
            if (!header.ChoosesIntegerMotionVectors())
            {
                writer.WriteBit(payload, header.GetIntegerMotionVectorMode());
            }
        }

        if (header.IsOrderHintEnabled())
        {
            if (header.GetOrderHintBits() < 1 || header.GetOrderHintBits() > 8)
            {
                writer.Code = AvifResultCode.InvalidArgument;
                return;
            }

            writer.WriteBits(payload, (ulong)(header.GetOrderHintBits() - 1), 3);
        }
    }

    static void WriteColorConfiguration(
        byte[] payload,
        BitWriter writer,
        ParsedAv1SequenceHeader header)
    {
        int profile = header.GetSequenceProfile();
        writer.WriteBit(payload, header.IsHighBitDepth() ? 1 : 0);
        if (profile == 2 && header.IsHighBitDepth())
        {
            writer.WriteBit(payload, header.IsTwelveBit() ? 1 : 0);
        }

        if (profile != 1)
        {
            writer.WriteBit(payload, header.IsMonochrome() ? 1 : 0);
        }
        else if (header.IsMonochrome())
        {
            writer.Code = AvifResultCode.InvalidArgument;
            return;
        }

        writer.WriteBit(payload, header.HasColorDescription() ? 1 : 0);
        if (header.HasColorDescription())
        {
            if (header.GetColorPrimaries() < 0 || header.GetColorPrimaries() > 255
                || header.GetTransferCharacteristics() < 0
                || header.GetTransferCharacteristics() > 255
                || header.GetMatrixCoefficients() < 0
                || header.GetMatrixCoefficients() > 255)
            {
                writer.Code = AvifResultCode.InvalidArgument;
                return;
            }

            writer.WriteBits(payload, (ulong)header.GetColorPrimaries(), 8);
            writer.WriteBits(
                payload,
                (ulong)header.GetTransferCharacteristics(),
                8);
            writer.WriteBits(payload, (ulong)header.GetMatrixCoefficients(), 8);
        }

        bool identityColor = header.GetColorPrimaries() == 1
            && header.GetTransferCharacteristics() == 13
            && header.GetMatrixCoefficients() == 0;
        if (header.IsMonochrome())
        {
            writer.WriteBit(payload, header.IsFullRange() ? 1 : 0);
            if (!header.HasChromaSubsamplingX()
                || !header.HasChromaSubsamplingY())
            {
                writer.Code = AvifResultCode.InvalidArgument;
            }
        }
        else if (identityColor)
        {
            if (!header.IsFullRange() || header.HasChromaSubsamplingX()
                || header.HasChromaSubsamplingY())
            {
                writer.Code = AvifResultCode.InvalidArgument;
            }
        }
        else
        {
            writer.WriteBit(payload, header.IsFullRange() ? 1 : 0);
            if (profile == 0)
            {
                if (!header.HasChromaSubsamplingX()
                    || !header.HasChromaSubsamplingY())
                {
                    writer.Code = AvifResultCode.InvalidArgument;
                }
            }
            else if (profile == 1)
            {
                if (header.HasChromaSubsamplingX()
                    || header.HasChromaSubsamplingY())
                {
                    writer.Code = AvifResultCode.InvalidArgument;
                }
            }
            else if (header.GetBitDepth() == 12)
            {
                writer.WriteBit(payload, header.HasChromaSubsamplingX() ? 1 : 0);
                if (header.HasChromaSubsamplingX())
                {
                    writer.WriteBit(
                        payload,
                        header.HasChromaSubsamplingY() ? 1 : 0);
                }
                else if (header.HasChromaSubsamplingY())
                {
                    writer.Code = AvifResultCode.InvalidArgument;
                }
            }
            else if (!header.HasChromaSubsamplingX()
                || header.HasChromaSubsamplingY())
            {
                writer.Code = AvifResultCode.InvalidArgument;
            }

            if (header.HasChromaSubsamplingX()
                && header.HasChromaSubsamplingY())
            {
                if (header.GetChromaSamplePosition() < 0
                    || header.GetChromaSamplePosition() > 3)
                {
                    writer.Code = AvifResultCode.InvalidArgument;
                }
                else
                {
                    writer.WriteBits(
                        payload,
                        (ulong)header.GetChromaSamplePosition(),
                        2);
                }
            }
        }

        if (!header.IsMonochrome())
        {
            writer.WriteBit(
                payload,
                header.HasSeparateUvDeltaQuantizer() ? 1 : 0);
        }
    }

    static bool FitsUnsigned(ulong value, int bitCount)
    {
        return bitCount == 64 || value < 1UL << bitCount;
    }

    internal static Av1WriteResult WriteObu(ParsedAv1SequenceHeader header)
    {
        Av1WriteResult payload = WritePayload(header);
        if (payload.Code != AvifResultCode.Success)
        {
            return payload;
        }

        return Av1ObuWriter.WriteObu(
            Av1ObuType.SequenceHeader,
            payload.Data,
            false,
            0,
            0);
    }

    static Av1WriteResult Fail(AvifResultCode code)
    {
        return new Av1WriteResult(code, System.Array.Empty<byte>());
    }
}

// AV1 specification sections 5.9.2 through 5.9.30.
internal static class Av1FrameHeaderWriter
{
    internal static Av1WriteResult WritePayload(
        ParsedAv1FrameHeader header,
        bool trailingBits)
    {
        if (header == null || header.SourcePayload == null
            || header.BitsConsumed < 0
            || header.BitsConsumed > (long)header.SourcePayload.Length * 8)
        {
            return Fail(AvifResultCode.InvalidArgument);
        }

        long suffixBits = trailingBits
            ? 1 + (7 - header.BitsConsumed % 8)
            : (8 - header.BitsConsumed % 8) % 8;
        long totalBits = header.BitsConsumed + suffixBits;
        if (totalBits > (long)int.MaxValue * 8)
        {
            return Fail(AvifResultCode.LimitExceeded);
        }

        byte[] payload = new byte[(int)(totalBits / 8)];
        BitWriter writer = BitWriter.Create(payload.Length);
        long bit = 0;
        while (bit < header.BitsConsumed)
        {
            int value = header.SourcePayload[bit >> 3]
                >> (7 - (int)(bit & 7)) & 1;
            writer.WriteBit(payload, value);
            bit++;
        }

        if (trailingBits)
        {
            writer.WriteTrailingBits(payload);
        }
        else
        {
            writer.AlignToByte(payload, 0);
        }

        if (writer.Code != AvifResultCode.Success)
        {
            return Fail(writer.Code);
        }

        return new Av1WriteResult(AvifResultCode.Success, payload);
    }

    internal static Av1WriteResult WriteObu(
        ParsedAv1FrameHeader header,
        bool hasExtension)
    {
        Av1WriteResult payload = WritePayload(header, true);
        if (payload.Code != AvifResultCode.Success)
        {
            return payload;
        }

        return Av1ObuWriter.WriteObu(
            Av1ObuType.FrameHeader,
            payload.Data,
            hasExtension,
            header.TemporalId,
            header.SpatialId);
    }

    static Av1WriteResult Fail(AvifResultCode code)
    {
        return new Av1WriteResult(code, System.Array.Empty<byte>());
    }
}
