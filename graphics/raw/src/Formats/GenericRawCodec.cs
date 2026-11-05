using System;

namespace raw;

internal enum GenericRawDecodeOutput
{
    StoredUnsigned = 0,
    StoredSigned = 1,
    StoredFloat = 2,
    NormalizedUShort = 3,
    NormalizedFloat = 4
}

internal class GenericRawDecodeState
{
    internal BoundedInput Input;
    internal RawDescriptor Descriptor;
    internal RawDecodeRegion Region;
    internal RawDecodeOptions Options;
    internal GenericRawDecodeOutput Output;
    internal uint[]? UnsignedDestination;
    internal int[]? SignedDestination;
    internal float[]? FloatDestination;
    internal ushort[]? UShortDestination;
    internal byte[]? RowBytes;
    internal uint[]? UnsignedRow;
    internal int[]? SignedRow;
    internal float[]? FloatRow;

    internal GenericRawDecodeState(BoundedInput input, RawDescriptor descriptor, RawDecodeRegion region, RawDecodeOptions options, GenericRawDecodeOutput output, uint[]? unsignedDestination, int[]? signedDestination, float[]? floatDestination, ushort[]? ushortDestination)
    {
        Input = input;
        Descriptor = descriptor;
        Region = region;
        Options = options;
        Output = output;
        UnsignedDestination = unsignedDestination;
        SignedDestination = signedDestination;
        FloatDestination = floatDestination;
        UShortDestination = ushortDestination;
        RowBytes = null;
        UnsignedRow = null;
        SignedRow = null;
        FloatRow = null;
    }
}

/// <summary>Decodes descriptor-driven generic raw regions into caller-owned stored or normalized sample buffers.</summary>
/// <remarks>Destination rows are pixel-interleaved in requested channel order even when source storage is planar.</remarks>
internal static class GenericRawCodec
{
    /// <summary>Decodes unsigned integer samples without numeric normalization.</summary>
    /// <param name="source">The exact bounded generic raw input.</param>
    /// <param name="descriptor">The authoritative source descriptor.</param>
    /// <param name="region">The source region and destination layout.</param>
    /// <param name="destination">The caller-owned unsigned sample buffer.</param>
    /// <param name="options">Decode limits and cancellation state.</param>
    public static void DecodeStored(System.ReadOnlyMemory<byte> source, RawDescriptor descriptor, RawDecodeRegion region, uint[] destination, RawDecodeOptions options)
    {
        Decode(CreateInput(source, descriptor, options), descriptor, region, options, GenericRawDecodeOutput.StoredUnsigned, destination, null, null, null);
    }

    /// <summary>Decodes signed integer samples without numeric normalization.</summary>
    /// <param name="source">The exact bounded generic raw input.</param>
    /// <param name="descriptor">The authoritative source descriptor.</param>
    /// <param name="region">The source region and destination layout.</param>
    /// <param name="destination">The caller-owned signed sample buffer.</param>
    /// <param name="options">Decode limits and cancellation state.</param>
    public static void DecodeStored(System.ReadOnlyMemory<byte> source, RawDescriptor descriptor, RawDecodeRegion region, int[] destination, RawDecodeOptions options)
    {
        Decode(CreateInput(source, descriptor, options), descriptor, region, options, GenericRawDecodeOutput.StoredSigned, null, destination, null, null);
    }

    /// <summary>Decodes half- or single-precision samples into their exact floating values.</summary>
    /// <param name="source">The exact bounded generic raw input.</param>
    /// <param name="descriptor">The authoritative source descriptor.</param>
    /// <param name="region">The source region and destination layout.</param>
    /// <param name="destination">The caller-owned floating sample buffer.</param>
    /// <param name="options">Decode limits and cancellation state.</param>
    public static void DecodeStored(System.ReadOnlyMemory<byte> source, RawDescriptor descriptor, RawDecodeRegion region, float[] destination, RawDecodeOptions options)
    {
        Decode(CreateInput(source, descriptor, options), descriptor, region, options, GenericRawDecodeOutput.StoredFloat, null, null, destination, null);
    }

    /// <summary>Decodes samples normalized to the complete unsigned sixteen-bit range.</summary>
    /// <remarks>Integer domains map linearly to zero through 65535. Floating input is clamped to zero through one and rounded to nearest; NaN is rejected.</remarks>
    /// <param name="source">The exact bounded generic raw input.</param>
    /// <param name="descriptor">The authoritative source descriptor.</param>
    /// <param name="region">The source region and destination layout.</param>
    /// <param name="destination">The caller-owned normalized sample buffer.</param>
    /// <param name="options">Decode limits and cancellation state.</param>
    public static void DecodeNormalized(System.ReadOnlyMemory<byte> source, RawDescriptor descriptor, RawDecodeRegion region, ushort[] destination, RawDecodeOptions options)
    {
        Decode(CreateInput(source, descriptor, options), descriptor, region, options, GenericRawDecodeOutput.NormalizedUShort, null, null, null, destination);
    }

    /// <summary>Decodes integer samples normalized to zero through one or retains floating source values.</summary>
    /// <param name="source">The exact bounded generic raw input.</param>
    /// <param name="descriptor">The authoritative source descriptor.</param>
    /// <param name="region">The source region and destination layout.</param>
    /// <param name="destination">The caller-owned normalized floating sample buffer.</param>
    /// <param name="options">Decode limits and cancellation state.</param>
    public static void DecodeNormalized(System.ReadOnlyMemory<byte> source, RawDescriptor descriptor, RawDecodeRegion region, float[] destination, RawDecodeOptions options)
    {
        Decode(CreateInput(source, descriptor, options), descriptor, region, options, GenericRawDecodeOutput.NormalizedFloat, null, null, destination, null);
    }

    /// <summary>Decodes unsigned integer samples from a bounded caller-owned stream.</summary>
    /// <param name="source">The readable caller-owned stream.</param>
    /// <param name="sourceOffset">The bounded input start.</param>
    /// <param name="sourceLength">The exact bounded input length.</param>
    /// <param name="descriptor">The authoritative source descriptor.</param>
    /// <param name="region">The source region and destination layout.</param>
    /// <param name="destination">The caller-owned unsigned sample buffer.</param>
    /// <param name="options">Decode limits and cancellation state.</param>
    public static void DecodeStored(System.IO.Stream source, long sourceOffset, long sourceLength, RawDescriptor descriptor, RawDecodeRegion region, uint[] destination, RawDecodeOptions options)
    {
        Decode(CreateInput(source, sourceOffset, sourceLength, descriptor, options), descriptor, region, options, GenericRawDecodeOutput.StoredUnsigned, destination, null, null, null);
    }

    /// <summary>Decodes signed integer samples from a bounded caller-owned stream.</summary>
    /// <param name="source">The readable caller-owned stream.</param>
    /// <param name="sourceOffset">The bounded input start.</param>
    /// <param name="sourceLength">The exact bounded input length.</param>
    /// <param name="descriptor">The authoritative source descriptor.</param>
    /// <param name="region">The source region and destination layout.</param>
    /// <param name="destination">The caller-owned signed sample buffer.</param>
    /// <param name="options">Decode limits and cancellation state.</param>
    public static void DecodeStored(System.IO.Stream source, long sourceOffset, long sourceLength, RawDescriptor descriptor, RawDecodeRegion region, int[] destination, RawDecodeOptions options)
    {
        Decode(CreateInput(source, sourceOffset, sourceLength, descriptor, options), descriptor, region, options, GenericRawDecodeOutput.StoredSigned, null, destination, null, null);
    }

    /// <summary>Decodes floating samples from a bounded caller-owned stream.</summary>
    /// <param name="source">The readable caller-owned stream.</param>
    /// <param name="sourceOffset">The bounded input start.</param>
    /// <param name="sourceLength">The exact bounded input length.</param>
    /// <param name="descriptor">The authoritative source descriptor.</param>
    /// <param name="region">The source region and destination layout.</param>
    /// <param name="destination">The caller-owned floating sample buffer.</param>
    /// <param name="options">Decode limits and cancellation state.</param>
    public static void DecodeStored(System.IO.Stream source, long sourceOffset, long sourceLength, RawDescriptor descriptor, RawDecodeRegion region, float[] destination, RawDecodeOptions options)
    {
        Decode(CreateInput(source, sourceOffset, sourceLength, descriptor, options), descriptor, region, options, GenericRawDecodeOutput.StoredFloat, null, null, destination, null);
    }

    /// <summary>Decodes normalized unsigned sixteen-bit samples from a bounded caller-owned stream.</summary>
    /// <param name="source">The readable caller-owned stream.</param>
    /// <param name="sourceOffset">The bounded input start.</param>
    /// <param name="sourceLength">The exact bounded input length.</param>
    /// <param name="descriptor">The authoritative source descriptor.</param>
    /// <param name="region">The source region and destination layout.</param>
    /// <param name="destination">The caller-owned normalized sample buffer.</param>
    /// <param name="options">Decode limits and cancellation state.</param>
    public static void DecodeNormalized(System.IO.Stream source, long sourceOffset, long sourceLength, RawDescriptor descriptor, RawDecodeRegion region, ushort[] destination, RawDecodeOptions options)
    {
        Decode(CreateInput(source, sourceOffset, sourceLength, descriptor, options), descriptor, region, options, GenericRawDecodeOutput.NormalizedUShort, null, null, null, destination);
    }

    /// <summary>Decodes normalized floating samples from a bounded caller-owned stream.</summary>
    /// <param name="source">The readable caller-owned stream.</param>
    /// <param name="sourceOffset">The bounded input start.</param>
    /// <param name="sourceLength">The exact bounded input length.</param>
    /// <param name="descriptor">The authoritative source descriptor.</param>
    /// <param name="region">The source region and destination layout.</param>
    /// <param name="destination">The caller-owned normalized floating sample buffer.</param>
    /// <param name="options">Decode limits and cancellation state.</param>
    public static void DecodeNormalized(System.IO.Stream source, long sourceOffset, long sourceLength, RawDescriptor descriptor, RawDecodeRegion region, float[] destination, RawDecodeOptions options)
    {
        Decode(CreateInput(source, sourceOffset, sourceLength, descriptor, options), descriptor, region, options, GenericRawDecodeOutput.NormalizedFloat, null, null, destination, null);
    }

    internal static BoundedInput CreateInput(System.ReadOnlyMemory<byte> source, RawDescriptor descriptor, RawDecodeOptions options)
    {
        ValidateOptions(options);
        ValidateDescriptor(descriptor, source.Length, options.Limits);
        return new BoundedInput(source, 0, source.Length, options.Limits);
    }

    internal static BoundedInput CreateInput(System.IO.Stream source, long sourceOffset, long sourceLength, RawDescriptor descriptor, RawDecodeOptions options)
    {
        ValidateOptions(options);
        ValidateDescriptor(descriptor, sourceLength, options.Limits);
        return BoundedInput.CreateStream(source, sourceOffset, sourceLength, options.Limits);
    }

    internal static void Decode(BoundedInput input, RawDescriptor descriptor, RawDecodeRegion region, RawDecodeOptions options, GenericRawDecodeOutput output, uint[]? unsignedDestination, int[]? signedDestination, float[]? floatDestination, ushort[]? ushortDestination)
    {
        GenericRawDecodeState state = new GenericRawDecodeState(input, descriptor, region, options, output, unsignedDestination, signedDestination, floatDestination, ushortDestination);
        ValidateRequest(state);
        AllocateRows(state);
        options.CheckCancellation();
        if (descriptor.Layout == RawSampleLayout.Interleaved)
        {
            DecodeInterleaved(state);
        }
        else
        {
            DecodePlanar(state);
        }
    }

    internal static void ValidateOptions(RawDecodeOptions options)
    {
        if (options == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Generic raw decode options are required.", -1);
        }
        CheckedMath.RequireLimits(options.Limits);
        CheckedMath.RequirePositiveLimit((options.Limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxDecodedBytes, "The configured decoded-data limit must be greater than zero.");
        CheckedMath.RequirePositiveLimit(options.Limits.MaxAllocationBytes, "The configured allocation limit must be greater than zero.");
        options.CheckCancellation();
    }

    internal static void ValidateDescriptor(RawDescriptor descriptor, long sourceLength, RawLimits? limits)
    {
        if (descriptor == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A generic raw descriptor is required.", -1);
        }
        RawValidationReport report = descriptor.ValidateSourceLength(sourceLength, limits);
        if (!report.IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "The generic raw descriptor or bounded source length is invalid.", -1);
        }
    }

    internal static void ValidateRequest(GenericRawDecodeState state)
    {
        RawDecodeRegion region = state.Region;
        RawDescriptor descriptor = state.Descriptor;
        if (region == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A generic raw decode region is required.", -1);
        }
        if (region.X < 0 || region.Y < 0 || region.Width <= 0 || region.Height <= 0 || region.X > descriptor.Width - region.Width || region.Y > descriptor.Height - region.Height)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The generic raw decode region lies outside the descriptor image.", -1);
        }
        if (region.FirstChannel < 0 || region.ChannelCount <= 0 || region.FirstChannel > descriptor.ChannelCount - region.ChannelCount)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The generic raw channel range lies outside the descriptor channels.", -1);
        }
        long rowSamples = CheckedMath.Multiply(region.Width, region.ChannelCount);
        if (region.DestinationOffset < 0 || region.DestinationRowStride < rowSamples)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The generic raw destination offset or row stride is invalid.", -1);
        }
        long preceding = CheckedMath.Multiply(region.Height - 1, region.DestinationRowStride);
        long required = CheckedMath.Add(region.DestinationOffset, CheckedMath.Add(preceding, rowSamples));
        int destinationLength = DestinationLength(state);
        if (required > destinationLength)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The generic raw destination buffer is too short for the requested region and stride.", -1);
        }
        int elementSize = state.Output == GenericRawDecodeOutput.NormalizedUShort ? 2 : 4;
        long logicalSamples = CheckedMath.Multiply(CheckedMath.Multiply(region.Width, region.Height), region.ChannelCount);
        CheckedMath.DecodedSize(logicalSamples, elementSize, state.Options.Limits);
        ValidateOutputKind(state);
    }

    internal static int DestinationLength(GenericRawDecodeState state)
    {
        if (state.Output == GenericRawDecodeOutput.StoredUnsigned)
        {
            return state.UnsignedDestination == null ? -1 : state.UnsignedDestination.Length;
        }
        if (state.Output == GenericRawDecodeOutput.StoredSigned)
        {
            return state.SignedDestination == null ? -1 : state.SignedDestination.Length;
        }
        if (state.Output == GenericRawDecodeOutput.NormalizedUShort)
        {
            return state.UShortDestination == null ? -1 : state.UShortDestination.Length;
        }
        return state.FloatDestination == null ? -1 : state.FloatDestination.Length;
    }

    internal static void ValidateOutputKind(GenericRawDecodeState state)
    {
        RawSampleKind kind = state.Descriptor.SampleKind;
        bool valid = state.Output == GenericRawDecodeOutput.StoredUnsigned && kind == RawSampleKind.UnsignedInteger;
        valid = valid || state.Output == GenericRawDecodeOutput.StoredSigned && kind == RawSampleKind.SignedInteger;
        valid = valid || state.Output == GenericRawDecodeOutput.StoredFloat && (kind == RawSampleKind.Half || kind == RawSampleKind.Single);
        valid = valid || state.Output == GenericRawDecodeOutput.NormalizedUShort || state.Output == GenericRawDecodeOutput.NormalizedFloat;
        if (!valid)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The stored destination type does not match the descriptor sample kind.", -1);
        }
    }

    internal static void AllocateRows(GenericRawDecodeState state)
    {
        long rowByteCount = state.Descriptor.GetMinimumRowBytes();
        CheckedMath.AllocationSize(rowByteCount, 1, state.Options.Limits);
        int rowSamples = state.Descriptor.Width;
        if (state.Descriptor.Layout == RawSampleLayout.Interleaved)
        {
            rowSamples = CheckedMath.ToInt32(CheckedMath.Multiply(rowSamples, state.Descriptor.ChannelCount));
        }
        CheckedMath.AllocationSize(rowSamples, 4, state.Options.Limits);
        long workingBytes = CheckedMath.Add(rowByteCount, CheckedMath.Multiply(rowSamples, 4));
        if (workingBytes > (state.Options.Limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxDecodedBytes)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "Generic raw row working storage exceeds the decoded-data limit.", -1);
        }
        state.RowBytes = new byte[CheckedMath.ToInt32(rowByteCount)];
        if (state.Descriptor.SampleKind == RawSampleKind.UnsignedInteger)
        {
            state.UnsignedRow = new uint[rowSamples];
        }
        else if (state.Descriptor.SampleKind == RawSampleKind.SignedInteger)
        {
            state.SignedRow = new int[rowSamples];
        }
        else
        {
            state.FloatRow = new float[rowSamples];
        }
    }

    internal static void DecodeInterleaved(GenericRawDecodeState state)
    {
        int outputRow = 0;
        while (outputRow < state.Region.Height)
        {
            state.Options.CheckCancellation();
            int sourceRow = state.Region.Y + outputRow;
            long rowOffset = CheckedMath.Add(state.Descriptor.DataOffset, CheckedMath.Multiply(sourceRow, state.Descriptor.RowStrideBytes));
            ReadAndDecodeRow(state, rowOffset);
            ValidateSelectedFloat(state, false, 0);
            CopyInterleaved(state, outputRow);
            outputRow++;
        }
    }

    internal static void DecodePlanar(GenericRawDecodeState state)
    {
        int selectedChannel = 0;
        while (selectedChannel < state.Region.ChannelCount)
        {
            state.Options.CheckCancellation();
            int sourceChannel = state.Region.FirstChannel + selectedChannel;
            long planeOffset = CheckedMath.Add(state.Descriptor.DataOffset, CheckedMath.Multiply(sourceChannel, state.Descriptor.PlaneStrideBytes));
            int outputRow = 0;
            while (outputRow < state.Region.Height)
            {
                state.Options.CheckCancellation();
                int sourceRow = state.Region.Y + outputRow;
                long rowOffset = CheckedMath.Add(planeOffset, CheckedMath.Multiply(sourceRow, state.Descriptor.RowStrideBytes));
                ReadAndDecodeRow(state, rowOffset);
                ValidateSelectedFloat(state, true, selectedChannel);
                CopyPlanar(state, outputRow, selectedChannel);
                outputRow++;
            }
            selectedChannel++;
        }
    }

    internal static void ReadAndDecodeRow(GenericRawDecodeState state, long offset)
    {
        ReadRow(state.Input, offset, state.RowBytes);
        if (state.Descriptor.Packing == RawPacking.ByteAligned)
        {
            DecodeByteAlignedRow(state);
        }
        else if (state.Descriptor.Packing == RawPacking.PackedBits)
        {
            if (state.Descriptor.SampleKind == RawSampleKind.UnsignedInteger)
            {
                BitPacking.UnpackUnsigned(state.RowBytes, (state.UnsignedRow ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length, state.Descriptor.BitDepth, state.Descriptor.BitOrder, state.UnsignedRow);
            }
            else
            {
                BitPacking.UnpackSigned(state.RowBytes, (state.SignedRow ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length, state.Descriptor.BitDepth, state.Descriptor.BitOrder, state.SignedRow);
            }
        }
        else
        {
            BitPacking.UnpackNamed(state.RowBytes, (state.UnsignedRow ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length, state.Descriptor.Packing, state.UnsignedRow);
        }
    }

    internal static void ReadRow(BoundedInput input, long offset, byte[]? destination)
    {
        if (input.UsesMemory || input.Seekable)
        {
            input.ReadAt(offset, destination);
            return;
        }
        if (offset < input.GetPosition())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidOffset, "A non-seekable generic raw decode requested data before the current stream position.", offset);
        }
        AdvanceInput(input, offset - input.GetPosition(), destination);
        input.ReadExactly(destination);
    }

    internal static void AdvanceInput(BoundedInput input, long count, byte[]? scratch)
    {
        long target = CheckedMath.RangeEnd(input.GetPosition(), count, input.GetLength());
        while (input.GetPosition() < target)
        {
            long remaining = target - input.GetPosition();
            int chunk = (scratch ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length;
            if (remaining < chunk)
            {
                chunk = (int)remaining;
            }
            int read = input.Read(scratch.AsSpan(0, chunk));
            if (read == 0)
            {
                throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "The input ended before the exact advance completed.", input.GetAbsolutePosition());
            }
        }
    }

    internal static void DecodeByteAlignedRow(GenericRawDecodeState state)
    {
        int bytesPerWord = state.Descriptor.StoredWordBits / 8;
        int sampleCount = (state.RowBytes ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length / bytesPerWord;
        int index = 0;
        while (index < sampleCount)
        {
            int offset = index * bytesPerWord;
            System.ReadOnlySpan<byte> word = state.RowBytes.AsSpan(offset, bytesPerWord);
            if (state.Descriptor.SampleKind == RawSampleKind.UnsignedInteger)
            {
                uint value = ReadUnsignedWord(word, state.Descriptor.StoredWordBits, state.Descriptor.ByteOrder);
                uint maximum = MaximumUnsigned(state.Descriptor.BitDepth);
                if (value > maximum)
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A byte-aligned unsigned sample exceeds the declared bit depth.", offset);
                }
                (state.UnsignedRow ?? throw new System.InvalidOperationException("A required value is unavailable."))[index] = value;
            }
            else if (state.Descriptor.SampleKind == RawSampleKind.SignedInteger)
            {
                int value = ReadSignedWord(word, state.Descriptor.StoredWordBits, state.Descriptor.ByteOrder);
                long minimum = MinimumSigned(state.Descriptor.BitDepth);
                long maximum = MaximumSigned(state.Descriptor.BitDepth);
                if (value < minimum || value > maximum)
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A byte-aligned signed sample exceeds the declared bit depth.", offset);
                }
                (state.SignedRow ?? throw new System.InvalidOperationException("A required value is unavailable."))[index] = value;
            }
            else if (state.Descriptor.SampleKind == RawSampleKind.Half)
            {
                (state.FloatRow ?? throw new System.InvalidOperationException("A required value is unavailable."))[index] = (float)EndianReader.ReadHalf(word, state.Descriptor.ByteOrder);
            }
            else
            {
                (state.FloatRow ?? throw new System.InvalidOperationException("A required value is unavailable."))[index] = EndianReader.ReadSingle(word, state.Descriptor.ByteOrder);
            }
            index++;
        }
    }

    internal static uint ReadUnsignedWord(System.ReadOnlySpan<byte> word, int storedWordBits, RawByteOrder byteOrder)
    {
        if (storedWordBits == 8)
        {
            return EndianReader.ReadUInt8(word);
        }
        if (storedWordBits == 16)
        {
            return EndianReader.ReadUInt16(word, byteOrder);
        }
        return EndianReader.ReadUInt32(word, byteOrder);
    }

    internal static int ReadSignedWord(System.ReadOnlySpan<byte> word, int storedWordBits, RawByteOrder byteOrder)
    {
        if (storedWordBits == 8)
        {
            return EndianReader.ReadInt8(word);
        }
        if (storedWordBits == 16)
        {
            return EndianReader.ReadInt16(word, byteOrder);
        }
        return EndianReader.ReadInt32(word, byteOrder);
    }

    internal static void ValidateSelectedFloat(GenericRawDecodeState state, bool planar, int selectedChannel)
    {
        if (state.Output != GenericRawDecodeOutput.NormalizedUShort || state.FloatRow == null)
        {
            return;
        }
        int pixel = 0;
        while (pixel < state.Region.Width)
        {
            if (planar)
            {
                RequireNotNaN(state.FloatRow[state.Region.X + pixel]);
            }
            else
            {
                int channel = 0;
                while (channel < state.Region.ChannelCount)
                {
                    int sourceIndex = (state.Region.X + pixel) * state.Descriptor.ChannelCount + state.Region.FirstChannel + channel;
                    RequireNotNaN(state.FloatRow[sourceIndex]);
                    channel++;
                }
            }
            pixel++;
        }
    }

    internal static void RequireNotNaN(float value)
    {
        if (float.IsNaN(value))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "NaN floating input cannot be converted to normalized unsigned sixteen-bit samples.", -1);
        }
    }

    internal static void CopyInterleaved(GenericRawDecodeState state, int outputRow)
    {
        int destinationRow = state.Region.DestinationOffset + outputRow * state.Region.DestinationRowStride;
        int pixel = 0;
        while (pixel < state.Region.Width)
        {
            int channel = 0;
            while (channel < state.Region.ChannelCount)
            {
                int sourceIndex = (state.Region.X + pixel) * state.Descriptor.ChannelCount + state.Region.FirstChannel + channel;
                int destinationIndex = destinationRow + pixel * state.Region.ChannelCount + channel;
                CopySample(state, sourceIndex, destinationIndex);
                channel++;
            }
            pixel++;
        }
    }

    internal static void CopyPlanar(GenericRawDecodeState state, int outputRow, int selectedChannel)
    {
        int destinationRow = state.Region.DestinationOffset + outputRow * state.Region.DestinationRowStride;
        int pixel = 0;
        while (pixel < state.Region.Width)
        {
            int sourceIndex = state.Region.X + pixel;
            int destinationIndex = destinationRow + pixel * state.Region.ChannelCount + selectedChannel;
            CopySample(state, sourceIndex, destinationIndex);
            pixel++;
        }
    }

    internal static void CopySample(GenericRawDecodeState state, int sourceIndex, int destinationIndex)
    {
        if (state.Output == GenericRawDecodeOutput.StoredUnsigned)
        {
            (state.UnsignedDestination ?? throw new System.InvalidOperationException("A required value is unavailable."))[destinationIndex] = (state.UnsignedRow ?? throw new System.InvalidOperationException("A required value is unavailable."))[sourceIndex];
        }
        else if (state.Output == GenericRawDecodeOutput.StoredSigned)
        {
            (state.SignedDestination ?? throw new System.InvalidOperationException("A required value is unavailable."))[destinationIndex] = (state.SignedRow ?? throw new System.InvalidOperationException("A required value is unavailable."))[sourceIndex];
        }
        else if (state.Output == GenericRawDecodeOutput.StoredFloat)
        {
            (state.FloatDestination ?? throw new System.InvalidOperationException("A required value is unavailable."))[destinationIndex] = (state.FloatRow ?? throw new System.InvalidOperationException("A required value is unavailable."))[sourceIndex];
        }
        else if (state.Output == GenericRawDecodeOutput.NormalizedUShort)
        {
            (state.UShortDestination ?? throw new System.InvalidOperationException("A required value is unavailable."))[destinationIndex] = NormalizedUShort(state, sourceIndex);
        }
        else
        {
            (state.FloatDestination ?? throw new System.InvalidOperationException("A required value is unavailable."))[destinationIndex] = NormalizedFloat(state, sourceIndex);
        }
    }

    internal static ushort NormalizedUShort(GenericRawDecodeState state, int index)
    {
        if (state.Descriptor.SampleKind == RawSampleKind.UnsignedInteger)
        {
            ulong value = (state.UnsignedRow ?? throw new System.InvalidOperationException("A required value is unavailable."))[index];
            ulong maximum = MaximumUnsigned(state.Descriptor.BitDepth);
            return (ushort)((value * 65535UL + maximum / 2UL) / maximum);
        }
        if (state.Descriptor.SampleKind == RawSampleKind.SignedInteger)
        {
            long minimum = MinimumSigned(state.Descriptor.BitDepth);
            ulong biased = (ulong)((long)(state.SignedRow ?? throw new System.InvalidOperationException("A required value is unavailable."))[index] - minimum);
            ulong range = MaximumUnsigned(state.Descriptor.BitDepth);
            return (ushort)((biased * 65535UL + range / 2UL) / range);
        }
        double valueFloat = (state.FloatRow ?? throw new System.InvalidOperationException("A required value is unavailable."))[index];
        if (valueFloat <= 0.0)
        {
            return 0;
        }
        if (valueFloat >= 1.0)
        {
            return 65535;
        }
        return (ushort)(valueFloat * 65535.0 + 0.5);
    }

    internal static float NormalizedFloat(GenericRawDecodeState state, int index)
    {
        if (state.Descriptor.SampleKind == RawSampleKind.UnsignedInteger)
        {
            return (float)((double)(state.UnsignedRow ?? throw new System.InvalidOperationException("A required value is unavailable."))[index] / MaximumUnsigned(state.Descriptor.BitDepth));
        }
        if (state.Descriptor.SampleKind == RawSampleKind.SignedInteger)
        {
            long minimum = MinimumSigned(state.Descriptor.BitDepth);
            double biased = (long)(state.SignedRow ?? throw new System.InvalidOperationException("A required value is unavailable."))[index] - minimum;
            return (float)(biased / MaximumUnsigned(state.Descriptor.BitDepth));
        }
        return (state.FloatRow ?? throw new System.InvalidOperationException("A required value is unavailable."))[index];
    }

    internal static uint MaximumUnsigned(int bitDepth)
    {
        if (bitDepth == 32)
        {
            return uint.MaxValue;
        }
        return (1U << bitDepth) - 1U;
    }

    internal static long MinimumSigned(int bitDepth)
    {
        if (bitDepth == 32)
        {
            return int.MinValue;
        }
        return -(1L << (bitDepth - 1));
    }

    internal static long MaximumSigned(int bitDepth)
    {
        if (bitDepth == 32)
        {
            return int.MaxValue;
        }
        return (1L << (bitDepth - 1)) - 1L;
    }
}
