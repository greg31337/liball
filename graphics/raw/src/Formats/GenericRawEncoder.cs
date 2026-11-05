using System;

namespace raw;

internal enum GenericRawEncodeInput
{
    StoredUnsigned = 0,
    StoredSigned = 1,
    StoredFloat = 2,
    NormalizedUShort = 3,
    NormalizedFloat = 4
}

internal class GenericRawEncodeState
{
    internal RawDescriptor Descriptor;
    internal RawGenericEncodeOptions Options;
    internal GenericRawEncodeInput Input;
    internal uint[]? UnsignedSource;
    internal int[]? SignedSource;
    internal float[]? FloatSource;
    internal ushort[]? UShortSource;
    internal int SourceOffset;
    internal int SourceRowStride;
    internal byte[]? RowBytes;
    internal uint[]? UnsignedRow;
    internal int[]? SignedRow;
    internal float[]? FloatRow;
    internal long OutputPosition;

    internal GenericRawEncodeState(RawDescriptor descriptor, RawGenericEncodeOptions options, GenericRawEncodeInput input, uint[]? unsignedSource, int[]? signedSource, float[]? floatSource, ushort[]? ushortSource, int sourceOffset, int sourceRowStride)
    {
        Descriptor = descriptor;
        Options = options;
        Input = input;
        UnsignedSource = unsignedSource;
        SignedSource = signedSource;
        FloatSource = floatSource;
        UShortSource = ushortSource;
        SourceOffset = sourceOffset;
        SourceRowStride = sourceRowStride;
        RowBytes = null;
        UnsignedRow = null;
        SignedRow = null;
        FloatRow = null;
        OutputPosition = 0;
    }
}

/// <summary>Encodes pixel-interleaved caller buffers into descriptor-driven generic raw payloads.</summary>
internal static class GenericRawEncoder
{
    /// <summary>Encodes exact unsigned integer samples into a new byte array.</summary>
    /// <param name="descriptor">The authoritative output descriptor.</param>
    /// <param name="source">The caller-owned unsigned samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">Encoding policies and limits.</param>
    /// <returns>The complete encoded generic raw payload.</returns>
    public static byte[] EncodeStored(RawDescriptor descriptor, uint[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        GenericRawEncodeState state = CreateState(descriptor, options, GenericRawEncodeInput.StoredUnsigned, source, null, null, null, sourceOffset, sourceRowStride);
        return EncodeArray(state);
    }

    /// <summary>Encodes exact signed integer samples into a new byte array.</summary>
    /// <param name="descriptor">The authoritative output descriptor.</param>
    /// <param name="source">The caller-owned signed samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">Encoding policies and limits.</param>
    /// <returns>The complete encoded generic raw payload.</returns>
    public static byte[] EncodeStored(RawDescriptor descriptor, int[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        GenericRawEncodeState state = CreateState(descriptor, options, GenericRawEncodeInput.StoredSigned, null, source, null, null, sourceOffset, sourceRowStride);
        return EncodeArray(state);
    }

    /// <summary>Encodes exact half- or single-precision floating values into a new byte array.</summary>
    /// <param name="descriptor">The authoritative output descriptor.</param>
    /// <param name="source">The caller-owned floating samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">Encoding policies and limits.</param>
    /// <returns>The complete encoded generic raw payload.</returns>
    public static byte[] EncodeStored(RawDescriptor descriptor, float[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        GenericRawEncodeState state = CreateState(descriptor, options, GenericRawEncodeInput.StoredFloat, null, null, source, null, sourceOffset, sourceRowStride);
        return EncodeArray(state);
    }

    /// <summary>Encodes normalized unsigned sixteen-bit samples into a new byte array.</summary>
    /// <param name="descriptor">The authoritative output descriptor.</param>
    /// <param name="source">The caller-owned normalized samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">Encoding policies and limits.</param>
    /// <returns>The complete encoded generic raw payload.</returns>
    public static byte[] EncodeNormalized(RawDescriptor descriptor, ushort[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        GenericRawEncodeState state = CreateState(descriptor, options, GenericRawEncodeInput.NormalizedUShort, null, null, null, source, sourceOffset, sourceRowStride);
        return EncodeArray(state);
    }

    /// <summary>Encodes normalized floating samples into a new byte array.</summary>
    /// <param name="descriptor">The authoritative output descriptor.</param>
    /// <param name="source">The caller-owned normalized samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">Encoding policies and limits.</param>
    /// <returns>The complete encoded generic raw payload.</returns>
    public static byte[] EncodeNormalized(RawDescriptor descriptor, float[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        GenericRawEncodeState state = CreateState(descriptor, options, GenericRawEncodeInput.NormalizedFloat, null, null, source, null, sourceOffset, sourceRowStride);
        return EncodeArray(state);
    }

    internal static RawWritePlan PlanStoredUnsigned(RawDescriptor descriptor, uint[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        return CreatePlan(CreateState(descriptor, options, GenericRawEncodeInput.StoredUnsigned, source, null, null, null, sourceOffset, sourceRowStride));
    }

    internal static RawWritePlan PlanStoredSigned(RawDescriptor descriptor, int[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        return CreatePlan(CreateState(descriptor, options, GenericRawEncodeInput.StoredSigned, null, source, null, null, sourceOffset, sourceRowStride));
    }

    internal static RawWritePlan PlanStoredFloating(RawDescriptor descriptor, float[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        return CreatePlan(CreateState(descriptor, options, GenericRawEncodeInput.StoredFloat, null, null, source, null, sourceOffset, sourceRowStride));
    }

    internal static RawWritePlan PlanNormalizedUnsigned(RawDescriptor descriptor, ushort[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        return CreatePlan(CreateState(descriptor, options, GenericRawEncodeInput.NormalizedUShort, null, null, null, source, sourceOffset, sourceRowStride));
    }

    internal static RawWritePlan PlanNormalizedFloating(RawDescriptor descriptor, float[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        return CreatePlan(CreateState(descriptor, options, GenericRawEncodeInput.NormalizedFloat, null, null, source, null, sourceOffset, sourceRowStride));
    }

    /// <summary>Encodes exact unsigned integer samples to a caller-owned stream without closing it.</summary>
    /// <param name="output">The writable caller-owned stream.</param>
    /// <param name="descriptor">The authoritative output descriptor.</param>
    /// <param name="source">The caller-owned unsigned samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">Encoding policies and limits.</param>
    public static void EncodeStored(System.IO.Stream output, RawDescriptor descriptor, uint[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        EncodeStream(output, CreateState(descriptor, options, GenericRawEncodeInput.StoredUnsigned, source, null, null, null, sourceOffset, sourceRowStride));
    }

    /// <summary>Encodes exact signed integer samples to a caller-owned stream without closing it.</summary>
    /// <param name="output">The writable caller-owned stream.</param>
    /// <param name="descriptor">The authoritative output descriptor.</param>
    /// <param name="source">The caller-owned signed samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">Encoding policies and limits.</param>
    public static void EncodeStored(System.IO.Stream output, RawDescriptor descriptor, int[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        EncodeStream(output, CreateState(descriptor, options, GenericRawEncodeInput.StoredSigned, null, source, null, null, sourceOffset, sourceRowStride));
    }

    /// <summary>Encodes exact floating samples to a caller-owned stream without closing it.</summary>
    /// <param name="output">The writable caller-owned stream.</param>
    /// <param name="descriptor">The authoritative output descriptor.</param>
    /// <param name="source">The caller-owned floating samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">Encoding policies and limits.</param>
    public static void EncodeStored(System.IO.Stream output, RawDescriptor descriptor, float[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        EncodeStream(output, CreateState(descriptor, options, GenericRawEncodeInput.StoredFloat, null, null, source, null, sourceOffset, sourceRowStride));
    }

    /// <summary>Encodes normalized unsigned sixteen-bit samples to a caller-owned stream without closing it.</summary>
    /// <param name="output">The writable caller-owned stream.</param>
    /// <param name="descriptor">The authoritative output descriptor.</param>
    /// <param name="source">The caller-owned normalized samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">Encoding policies and limits.</param>
    public static void EncodeNormalized(System.IO.Stream output, RawDescriptor descriptor, ushort[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        EncodeStream(output, CreateState(descriptor, options, GenericRawEncodeInput.NormalizedUShort, null, null, null, source, sourceOffset, sourceRowStride));
    }

    /// <summary>Encodes normalized floating samples to a caller-owned stream without closing it.</summary>
    /// <param name="output">The writable caller-owned stream.</param>
    /// <param name="descriptor">The authoritative output descriptor.</param>
    /// <param name="source">The caller-owned normalized samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">Encoding policies and limits.</param>
    public static void EncodeNormalized(System.IO.Stream output, RawDescriptor descriptor, float[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        EncodeStream(output, CreateState(descriptor, options, GenericRawEncodeInput.NormalizedFloat, null, null, source, null, sourceOffset, sourceRowStride));
    }

    internal static GenericRawEncodeState CreateState(RawDescriptor descriptor, RawGenericEncodeOptions options, GenericRawEncodeInput input, uint[]? unsignedSource, int[]? signedSource, float[]? floatSource, ushort[]? ushortSource, int sourceOffset, int sourceRowStride)
    {
        GenericRawEncodeState state = new GenericRawEncodeState(descriptor, options, input, unsignedSource, signedSource, floatSource, ushortSource, sourceOffset, sourceRowStride);
        ValidateState(state);
        AllocateRows(state);
        PreflightSource(state);
        return state;
    }

    internal static RawWritePlan CreatePlan(GenericRawEncodeState state)
    {
        long length = CheckedMath.Add(state.Descriptor.DataOffset, state.Descriptor.DataLength);
        RawWriteChange change = new RawWriteChange(RawWriteChangeKind.Added, "GenericRaw/Data", "Encode the complete descriptor-driven generic raw output.", -1, 0, 0, length, false);
        return new RawWritePlan(true, length, string.Empty, new RawWriteChange[] { change }, null);
    }

    internal static void ValidateState(GenericRawEncodeState state)
    {
        if (state.Options == null)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "Generic raw encoding options are required.", -1);
        }
        RawValidationReport optionsReport = state.Options.Validate();
        if (!optionsReport.IsValid())
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "Generic raw encoding options are invalid.", -1);
        }
        state.Options.CheckCancellation();
        if (state.Descriptor == null || !state.Descriptor.Validate(state.Options.Limits).IsValid())
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidDescriptor, "A valid generic raw output descriptor is required.", -1);
        }
        long outputLength = CheckedMath.Add(state.Descriptor.DataOffset, state.Descriptor.DataLength);
        CheckedMath.FileSize(outputLength, state.Options.Limits);
        long trailing = state.Descriptor.GetTrailingByteCount();
        if (trailing != 0 && state.Options.TrailingBytePolicy == RawTrailingBytePolicy.Reject)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidLength, "The generic raw descriptor declares trailing bytes but the selected policy rejects them.", -1);
        }
        if (state.Options.TrailingBytePolicy == RawTrailingBytePolicy.Preserve && state.Options.PreservedBytes.LongLength != outputLength)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidLength, "Preserving generic raw nonsample bytes requires an exact output-sized template.", -1);
        }
        ValidateInputKind(state);
        long rowSamples = CheckedMath.Multiply(state.Descriptor.Width, state.Descriptor.ChannelCount);
        if (state.SourceOffset < 0 || state.SourceRowStride < rowSamples)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidLength, "The generic raw source offset or row stride is invalid.", -1);
        }
        long preceding = CheckedMath.Multiply(state.Descriptor.Height - 1, state.SourceRowStride);
        long required = CheckedMath.Add(state.SourceOffset, CheckedMath.Add(preceding, rowSamples));
        if (required > SourceLength(state))
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidLength, "The generic raw source buffer is too short for the descriptor geometry and stride.", -1);
        }
        int elementSize = state.Input == GenericRawEncodeInput.NormalizedUShort ? 2 : 4;
        CheckedMath.DecodedSize(CheckedMath.Multiply(rowSamples, state.Descriptor.Height), elementSize, state.Options.Limits);
    }

    internal static void ValidateInputKind(GenericRawEncodeState state)
    {
        bool valid = state.Input == GenericRawEncodeInput.StoredUnsigned && state.Descriptor.SampleKind == RawSampleKind.UnsignedInteger && state.UnsignedSource != null;
        valid = valid || state.Input == GenericRawEncodeInput.StoredSigned && state.Descriptor.SampleKind == RawSampleKind.SignedInteger && state.SignedSource != null;
        valid = valid || state.Input == GenericRawEncodeInput.StoredFloat && (state.Descriptor.SampleKind == RawSampleKind.Half || state.Descriptor.SampleKind == RawSampleKind.Single) && state.FloatSource != null;
        valid = valid || state.Input == GenericRawEncodeInput.NormalizedUShort && state.UShortSource != null;
        valid = valid || state.Input == GenericRawEncodeInput.NormalizedFloat && state.FloatSource != null;
        if (!valid)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "The generic raw source type does not match the selected stored or normalized encoding operation.", -1);
        }
    }

    internal static int SourceLength(GenericRawEncodeState state)
    {
        if (state.UnsignedSource != null)
        {
            return state.UnsignedSource.Length;
        }
        if (state.SignedSource != null)
        {
            return state.SignedSource.Length;
        }
        if (state.UShortSource != null)
        {
            return state.UShortSource.Length;
        }
        return state.FloatSource == null ? -1 : state.FloatSource.Length;
    }

    internal static void AllocateRows(GenericRawEncodeState state)
    {
        long rowBytes = state.Descriptor.GetMinimumRowBytes();
        CheckedMath.AllocationSize(rowBytes, 1, state.Options.Limits);
        int rowSamples = state.Descriptor.Width;
        if (state.Descriptor.Layout == RawSampleLayout.Interleaved)
        {
            rowSamples = CheckedMath.ToInt32(CheckedMath.Multiply(rowSamples, state.Descriptor.ChannelCount));
        }
        CheckedMath.AllocationSize(rowSamples, 4, state.Options.Limits);
        long workingBytes = CheckedMath.Add(rowBytes, CheckedMath.Multiply(rowSamples, 4));
        if (workingBytes > state.Options.Limits.MaxDecodedBytes)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "Generic raw encoding row storage exceeds the decoded-data limit.", -1);
        }
        state.RowBytes = new byte[CheckedMath.ToInt32(rowBytes)];
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

    internal static void PreflightSource(GenericRawEncodeState state)
    {
        int row = 0;
        while (row < state.Descriptor.Height)
        {
            state.Options.CheckCancellation();
            int pixel = 0;
            while (pixel < state.Descriptor.Width)
            {
                int channel = 0;
                while (channel < state.Descriptor.ChannelCount)
                {
                    int index = state.SourceOffset + row * state.SourceRowStride + pixel * state.Descriptor.ChannelCount + channel;
                    ValidateSourceSample(state, index);
                    channel++;
                }
                pixel++;
            }
            row++;
        }
    }

    internal static void ValidateSourceSample(GenericRawEncodeState state, int index)
    {
        if (state.Input == GenericRawEncodeInput.StoredUnsigned)
        {
            if ((state.UnsignedSource ?? throw new System.InvalidOperationException("A required value is unavailable."))[index] > GenericRawCodec.MaximumUnsigned(state.Descriptor.BitDepth))
            {
                throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A stored unsigned source sample exceeds the descriptor bit depth.", -1);
            }
            return;
        }
        if (state.Input == GenericRawEncodeInput.StoredSigned)
        {
            int value = (state.SignedSource ?? throw new System.InvalidOperationException("A required value is unavailable."))[index];
            if (value < GenericRawCodec.MinimumSigned(state.Descriptor.BitDepth) || value > GenericRawCodec.MaximumSigned(state.Descriptor.BitDepth))
            {
                throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A stored signed source sample exceeds the descriptor bit depth.", -1);
            }
            return;
        }
        if (state.Input == GenericRawEncodeInput.StoredFloat)
        {
            PrepareStoredFloat(state, (state.FloatSource ?? throw new System.InvalidOperationException("A required value is unavailable."))[index]);
            return;
        }
        if (state.Input == GenericRawEncodeInput.NormalizedFloat)
        {
            PrepareNormalizedFloat(state, (state.FloatSource ?? throw new System.InvalidOperationException("A required value is unavailable."))[index]);
        }
    }

    internal static byte[] EncodeArray(GenericRawEncodeState state)
    {
        long length = CheckedMath.Add(state.Descriptor.DataOffset, state.Descriptor.DataLength);
        CheckedMath.AllocationSize(length, 1, state.Options.Limits);
        byte[] result = new byte[CheckedMath.ToInt32(length)];
        using (System.IO.MemoryStream output = new System.IO.MemoryStream(result, true))
        {
            EncodePrepared(output, state);
        }
        return result;
    }

    internal static void EncodeStream(System.IO.Stream output, GenericRawEncodeState state)
    {
        if (output == null || !output.CanWrite)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A writable generic raw output stream is required.", -1);
        }
        EncodePrepared(output, state);
    }

    internal static void EncodePrepared(System.IO.Stream output, GenericRawEncodeState state)
    {
        state.OutputPosition = 0;
        WriteNonsample(output, state, state.Descriptor.DataOffset, state.Options.PaddingByte);
        if (state.Descriptor.Layout == RawSampleLayout.Interleaved)
        {
            EncodeInterleaved(output, state);
        }
        else
        {
            EncodePlanar(output, state);
        }
        long trailing = state.Descriptor.GetTrailingByteCount();
        byte trailingByte = state.Options.TrailingBytePolicy == RawTrailingBytePolicy.ZeroFill ? (byte)0 : state.Options.PaddingByte;
        WriteNonsample(output, state, trailing, trailingByte);
        long expected = CheckedMath.Add(state.Descriptor.DataOffset, state.Descriptor.DataLength);
        if (state.OutputPosition != expected)
        {
            throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "The generic raw encoder wrote an unexpected byte count.", state.OutputPosition);
        }
    }

    internal static void EncodeInterleaved(System.IO.Stream output, GenericRawEncodeState state)
    {
        int row = 0;
        while (row < state.Descriptor.Height)
        {
            state.Options.CheckCancellation();
            FillInterleavedRow(state, row);
            EncodeRow(state);
            WriteBytes(output, state, state.RowBytes, (state.RowBytes ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length);
            WriteNonsample(output, state, state.Descriptor.RowStrideBytes - state.RowBytes.Length, state.Options.PaddingByte);
            row++;
        }
    }

    internal static void EncodePlanar(System.IO.Stream output, GenericRawEncodeState state)
    {
        int plane = 0;
        while (plane < state.Descriptor.ChannelCount)
        {
            state.Options.CheckCancellation();
            int row = 0;
            while (row < state.Descriptor.Height)
            {
                state.Options.CheckCancellation();
                FillPlanarRow(state, row, plane);
                EncodeRow(state);
                WriteBytes(output, state, state.RowBytes, (state.RowBytes ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length);
                WriteNonsample(output, state, state.Descriptor.RowStrideBytes - state.RowBytes.Length, state.Options.PaddingByte);
                row++;
            }
            if (plane + 1 < state.Descriptor.ChannelCount)
            {
                long planeContent = CheckedMath.Multiply(state.Descriptor.RowStrideBytes, state.Descriptor.Height);
                WriteNonsample(output, state, state.Descriptor.PlaneStrideBytes - planeContent, state.Options.PaddingByte);
            }
            plane++;
        }
    }

    internal static void FillInterleavedRow(GenericRawEncodeState state, int row)
    {
        int count = state.Descriptor.Width * state.Descriptor.ChannelCount;
        int index = 0;
        while (index < count)
        {
            int sourceIndex = state.SourceOffset + row * state.SourceRowStride + index;
            SetRowSample(state, index, sourceIndex);
            index++;
        }
    }

    internal static void FillPlanarRow(GenericRawEncodeState state, int row, int plane)
    {
        int pixel = 0;
        while (pixel < state.Descriptor.Width)
        {
            int sourceIndex = state.SourceOffset + row * state.SourceRowStride + pixel * state.Descriptor.ChannelCount + plane;
            SetRowSample(state, pixel, sourceIndex);
            pixel++;
        }
    }

    internal static void SetRowSample(GenericRawEncodeState state, int rowIndex, int sourceIndex)
    {
        if (state.Descriptor.SampleKind == RawSampleKind.UnsignedInteger)
        {
            (state.UnsignedRow ?? throw new System.InvalidOperationException("A required value is unavailable."))[rowIndex] = SourceUnsigned(state, sourceIndex);
        }
        else if (state.Descriptor.SampleKind == RawSampleKind.SignedInteger)
        {
            (state.SignedRow ?? throw new System.InvalidOperationException("A required value is unavailable."))[rowIndex] = SourceSigned(state, sourceIndex);
        }
        else
        {
            (state.FloatRow ?? throw new System.InvalidOperationException("A required value is unavailable."))[rowIndex] = SourceFloat(state, sourceIndex);
        }
    }

    internal static uint SourceUnsigned(GenericRawEncodeState state, int index)
    {
        if (state.Input == GenericRawEncodeInput.StoredUnsigned)
        {
            return (state.UnsignedSource ?? throw new System.InvalidOperationException("A required value is unavailable."))[index];
        }
        double normalized = NormalizedSource(state, index);
        return (uint)Quantize(normalized * GenericRawCodec.MaximumUnsigned(state.Descriptor.BitDepth), state.Options.RoundingMode);
    }

    internal static int SourceSigned(GenericRawEncodeState state, int index)
    {
        if (state.Input == GenericRawEncodeInput.StoredSigned)
        {
            return (state.SignedSource ?? throw new System.InvalidOperationException("A required value is unavailable."))[index];
        }
        double normalized = NormalizedSource(state, index);
        if (state.Options.SignedPolicy == RawSignedEncodingPolicy.NonNegative)
        {
            return (int)Quantize(normalized * GenericRawCodec.MaximumSigned(state.Descriptor.BitDepth), state.Options.RoundingMode);
        }
        long minimum = GenericRawCodec.MinimumSigned(state.Descriptor.BitDepth);
        double range = GenericRawCodec.MaximumUnsigned(state.Descriptor.BitDepth);
        return (int)(minimum + Quantize(normalized * range, state.Options.RoundingMode));
    }

    internal static float SourceFloat(GenericRawEncodeState state, int index)
    {
        if (state.Input == GenericRawEncodeInput.StoredFloat)
        {
            return PrepareStoredFloat(state, (state.FloatSource ?? throw new System.InvalidOperationException("A required value is unavailable."))[index]);
        }
        return (float)NormalizedSource(state, index);
    }

    internal static double NormalizedSource(GenericRawEncodeState state, int index)
    {
        if (state.Input == GenericRawEncodeInput.NormalizedUShort)
        {
            return (double)(state.UShortSource ?? throw new System.InvalidOperationException("A required value is unavailable."))[index] / 65535.0;
        }
        return PrepareNormalizedFloat(state, (state.FloatSource ?? throw new System.InvalidOperationException("A required value is unavailable."))[index]);
    }

    internal static double Quantize(double value, RawSampleRoundingMode mode)
    {
        if (mode == RawSampleRoundingMode.TowardZero)
        {
            return System.Math.Truncate(value);
        }
        return System.Math.Floor(value + 0.5);
    }

    internal static float PrepareNormalizedFloat(GenericRawEncodeState state, float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            if (state.Options.NonFinitePolicy == RawNonFinitePolicy.Preserve && (state.Descriptor.SampleKind == RawSampleKind.Half || state.Descriptor.SampleKind == RawSampleKind.Single))
            {
                return value;
            }
            if (state.Options.NonFinitePolicy != RawNonFinitePolicy.Clamp)
            {
                throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A normalized non-finite sample is not permitted by the selected policy.", -1);
            }
            if (float.IsNaN(value) || float.IsNegativeInfinity(value))
            {
                return 0.0F;
            }
            return 1.0F;
        }
        if (value < 0.0F)
        {
            if (state.Options.RangePolicy == RawSampleRangePolicy.Reject)
            {
                throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A normalized sample is below zero.", -1);
            }
            return 0.0F;
        }
        if (value > 1.0F)
        {
            if (state.Options.RangePolicy == RawSampleRangePolicy.Reject)
            {
                throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A normalized sample is above one.", -1);
            }
            return 1.0F;
        }
        return value;
    }

    internal static float PrepareStoredFloat(GenericRawEncodeState state, float value)
    {
        float maximum = state.Descriptor.SampleKind == RawSampleKind.Half ? 65504.0F : float.MaxValue;
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            if (state.Options.NonFinitePolicy == RawNonFinitePolicy.Preserve)
            {
                return value;
            }
            if (state.Options.NonFinitePolicy == RawNonFinitePolicy.Reject)
            {
                throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A stored non-finite sample is not permitted by the selected policy.", -1);
            }
            if (float.IsNaN(value))
            {
                return 0.0F;
            }
            return float.IsNegativeInfinity(value) ? -maximum : maximum;
        }
        if (value < -maximum || value > maximum)
        {
            if (state.Options.RangePolicy == RawSampleRangePolicy.Reject)
            {
                throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A stored floating sample exceeds the destination representation.", -1);
            }
            return value < 0.0F ? -maximum : maximum;
        }
        return value;
    }

    internal static void EncodeRow(GenericRawEncodeState state)
    {
        if (state.Descriptor.Packing == RawPacking.ByteAligned)
        {
            EncodeByteAlignedRow(state);
        }
        else if (state.Descriptor.Packing == RawPacking.PackedBits)
        {
            if (state.Descriptor.SampleKind == RawSampleKind.UnsignedInteger)
            {
                BitPacking.PackUnsigned(state.UnsignedRow, state.Descriptor.BitDepth, state.Descriptor.BitOrder, state.RowBytes);
            }
            else
            {
                BitPacking.PackSigned(state.SignedRow, state.Descriptor.BitDepth, state.Descriptor.BitOrder, state.RowBytes);
            }
        }
        else
        {
            BitPacking.PackNamed(state.UnsignedRow, state.Descriptor.Packing, state.RowBytes);
        }
    }

    internal static void EncodeByteAlignedRow(GenericRawEncodeState state)
    {
        int bytesPerWord = state.Descriptor.StoredWordBits / 8;
        int count = (state.RowBytes ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length / bytesPerWord;
        int index = 0;
        while (index < count)
        {
            System.Span<byte> word = state.RowBytes.AsSpan(index * bytesPerWord, bytesPerWord);
            if (state.Descriptor.SampleKind == RawSampleKind.UnsignedInteger)
            {
                WriteUnsignedWord(word, (state.UnsignedRow ?? throw new System.InvalidOperationException("A required value is unavailable."))[index], state.Descriptor.StoredWordBits, state.Descriptor.ByteOrder);
            }
            else if (state.Descriptor.SampleKind == RawSampleKind.SignedInteger)
            {
                WriteSignedWord(word, (state.SignedRow ?? throw new System.InvalidOperationException("A required value is unavailable."))[index], state.Descriptor.StoredWordBits, state.Descriptor.ByteOrder);
            }
            else if (state.Descriptor.SampleKind == RawSampleKind.Half)
            {
                EndianWriter.WriteHalf(word, (System.Half)(state.FloatRow ?? throw new System.InvalidOperationException("A required value is unavailable."))[index], state.Descriptor.ByteOrder);
            }
            else
            {
                EndianWriter.WriteSingle(word, (state.FloatRow ?? throw new System.InvalidOperationException("A required value is unavailable."))[index], state.Descriptor.ByteOrder);
            }
            index++;
        }
    }

    internal static void WriteUnsignedWord(System.Span<byte> word, uint value, int storedWordBits, RawByteOrder byteOrder)
    {
        if (storedWordBits == 8)
        {
            EndianWriter.WriteUInt8(word, (byte)value);
        }
        else if (storedWordBits == 16)
        {
            EndianWriter.WriteUInt16(word, (ushort)value, byteOrder);
        }
        else
        {
            EndianWriter.WriteUInt32(word, value, byteOrder);
        }
    }

    internal static void WriteSignedWord(System.Span<byte> word, int value, int storedWordBits, RawByteOrder byteOrder)
    {
        if (storedWordBits == 8)
        {
            EndianWriter.WriteInt8(word, (sbyte)value);
        }
        else if (storedWordBits == 16)
        {
            EndianWriter.WriteInt16(word, (short)value, byteOrder);
        }
        else
        {
            EndianWriter.WriteInt32(word, value, byteOrder);
        }
    }

    internal static void WriteBytes(System.IO.Stream output, GenericRawEncodeState state, byte[]? bytes, int count)
    {
        if (bytes == null)
        {
            throw new System.InvalidOperationException("The encode buffer is unavailable.");
        }
        output.Write(bytes, 0, count);
        state.OutputPosition = CheckedMath.Add(state.OutputPosition, count);
    }

    internal static void WriteNonsample(System.IO.Stream output, GenericRawEncodeState state, long count, byte value)
    {
        while (count > 0)
        {
            int chunk = (state.RowBytes ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length;
            if (count < chunk)
            {
                chunk = (int)count;
            }
            if (state.Options.TrailingBytePolicy == RawTrailingBytePolicy.Preserve)
            {
                System.Array.Copy(state.Options.PreservedBytes, (int)state.OutputPosition, state.RowBytes, 0, chunk);
            }
            else
            {
                state.RowBytes.AsSpan(0, chunk).Fill(value);
            }
            WriteBytes(output, state, state.RowBytes, chunk);
            count -= chunk;
        }
    }
}
