using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

namespace avif;

internal class AvifOneCallStreamResult
{
    internal AvifResultCode Code;
    internal byte[] Data;
    internal long MissingOffset;
    internal long MissingLength;

    internal AvifOneCallStreamResult(
        AvifResultCode code,
        byte[] data,
        long missingOffset,
        long missingLength)
    {
        Code = code;
        Data = data == null ? System.Array.Empty<byte>() : data;
        MissingOffset = missingOffset;
        MissingLength = missingLength;
    }
}

internal static class AvifOneCallCodec
{
    const int RawHeaderSize = 25;
    const int RawPlaneHeaderSize = 13;

    internal static AvifOneCallStreamResult ReadStream(
        Stream? source,
        long maxBytes,
        AvifCancellation? cancellation)
    {
        if (source == null || !source.CanRead)
        {
            return new AvifOneCallStreamResult(
                AvifResultCode.InvalidArgument,
                System.Array.Empty<byte>(),
                0,
                0);
        }
        long initialOffset = source.CanSeek ? source.Position : 0;
        StreamInput input = new StreamInput(source, true);
        AvifOneCallStreamResult result = ReadInputFromOffset(
            input,
            maxBytes,
            cancellation,
            initialOffset);
        input.Close();
        return result;
    }

    internal static AvifOneCallStreamResult ReadInput(
        IInputSource source,
        long maxBytes,
        AvifCancellation? cancellation)
    {
        return ReadInputFromOffset(source, maxBytes, cancellation, 0);
    }

    static AvifOneCallStreamResult ReadInputFromOffset(
        IInputSource source,
        long maxBytes,
        AvifCancellation? cancellation,
        long initialOffset)
    {
        if (source == null || maxBytes <= 0 || initialOffset < 0)
        {
            return new AvifOneCallStreamResult(
                AvifResultCode.InvalidArgument,
                System.Array.Empty<byte>(),
                0,
                0);
        }
        if (cancellation != null && cancellation.IsCancellationRequested())
        {
            return new AvifOneCallStreamResult(
                AvifResultCode.Cancelled,
                System.Array.Empty<byte>(),
                0,
                0);
        }

        int initialCapacity = maxBytes < 4096 ? (int)maxBytes : 4096;
        MemoryOutput output = MemoryOutput.Create(initialCapacity, maxBytes);
        byte[] buffer = new byte[8192];
        long offset = initialOffset;
        while (true)
        {
            if (cancellation != null
                && cancellation.IsCancellationRequested())
            {
                return new AvifOneCallStreamResult(
                    AvifResultCode.Cancelled,
                    System.Array.Empty<byte>(),
                    0,
                    0);
            }
            InputReadResult read = source.Read(offset, buffer);
            if (read.Code == AvifResultCode.EndOfData)
            {
                break;
            }
            if (read.Code != AvifResultCode.Success)
            {
                return new AvifOneCallStreamResult(
                    read.Code,
                    System.Array.Empty<byte>(),
                    read.MissingOffset,
                    read.MissingLength);
            }
            if (read.BytesRead <= 0 || read.BytesRead > buffer.Length)
            {
                return new AvifOneCallStreamResult(
                    AvifResultCode.InvalidState,
                    System.Array.Empty<byte>(),
                    0,
                    0);
            }
            AvifResultCode code = output.Write(
                buffer.AsSpan(0, read.BytesRead));
            if (code != AvifResultCode.Success)
            {
                return new AvifOneCallStreamResult(
                    code,
                    System.Array.Empty<byte>(),
                    0,
                    0);
            }
            offset += read.BytesRead;
        }

        return new AvifOneCallStreamResult(
            AvifResultCode.Success,
            output.ToArray(),
            0,
            0);
    }

    internal static AvifDecodeResult Decode(
        ReadOnlySpan<byte> source,
        AvifDecoderOptions options)
    {
        AvifResultCode optionsCode = ValidateDecoderOptions(options);
        if (optionsCode != AvifResultCode.Success)
        {
            return new AvifDecodeResult(optionsCode, null, null);
        }
        if (options.Cancellation.IsCancellationRequested())
        {
            return new AvifDecodeResult(AvifResultCode.Cancelled, null, null);
        }
        AvifReader reader = new AvifReader(source.ToArray(), options);
        AvifResultCode parseCode = reader.Parse();
        if (parseCode != AvifResultCode.Success)
        {
            AvifDecodeResult failure = new AvifDecodeResult(
                parseCode,
                null,
                reader.GetDocument());
            reader.Close();
            return failure;
        }
        AvifDecodeResult result = reader.DecodeSelectedImage();
        reader.Close();
        return result;
    }

    internal static AvifEncodeResult Encode(
        AvifImage? image,
        AvifEncoderOptions options)
    {
        AvifResultCode optionsCode = ValidateEncoderOptions(image, options);
        if (optionsCode != AvifResultCode.Success)
        {
            return new AvifEncodeResult(
                optionsCode,
                System.Array.Empty<byte>());
        }

        Av1EncoderProfileDecision profile = Av1EncoderProfileSelector.Choose(
            image,
            -1);
        if (profile.Code != AvifResultCode.Success)
        {
            return new AvifEncodeResult(
                profile.Code,
                System.Array.Empty<byte>());
        }

        int sequenceLevel = SelectSequenceLevel(image
            ?? throw new InvalidOperationException("Invalid internal state."));
        Av1WriteResult sequence = CreateSequenceObu(
            image,
            profile,
            CreateConfiguredAvifOperatingPoints(options, sequenceLevel),
            options.SequencePolicy != AvifSequenceEncodingPolicy.InterFrame,
            options.SequencePolicy != AvifSequenceEncodingPolicy.InterFrame,
            options.FilmGrainParametersPresent);
        if (sequence.Code != AvifResultCode.Success)
        {
            return new AvifEncodeResult(
                sequence.Code,
                System.Array.Empty<byte>());
        }
        AvifOneCallStreamResult raw = SerializeImage(image, options.Limits);
        if (raw.Code != AvifResultCode.Success)
        {
            return new AvifEncodeResult(raw.Code, System.Array.Empty<byte>());
        }
        Av1WriteResult padding = Av1ObuWriter.WriteObu(
            Av1ObuType.Padding,
            raw.Data,
            false,
            0,
            0);
        if (padding.Code != AvifResultCode.Success)
        {
            return new AvifEncodeResult(
                padding.Code,
                System.Array.Empty<byte>());
        }

        byte[] payload = Combine(sequence.Data, padding.Data);
        if (payload.Length == 0)
        {
            return new AvifEncodeResult(
                AvifResultCode.LimitExceeded,
                System.Array.Empty<byte>());
        }
        CanonicalItemPayload item = CanonicalItemPayload.Create(
            1,
            "av01",
            string.Empty,
            false,
            payload);
        AddRequiredProperties(item, image, profile, sequenceLevel);

        CanonicalItemContainerOptions containerOptions =
            new CanonicalItemContainerOptions();
        containerOptions.HeaderFormat = options.HeaderFormat;
        containerOptions.MaximumOutputBytes = (options.Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxSourceBytes;
        CanonicalItemContainerRequest request = CanonicalItemContainerRequest.Create(
            "avif",
            0,
            new List<string> { "mif1", "miaf" },
            1,
            new List<CanonicalItemPayload> { item },
            containerOptions);
        CanonicalItemContainerWriteResult written =
            CanonicalItemContainerWriter.Write(request);
        if (written.Code != AvifResultCode.Success)
        {
            return new AvifEncodeResult(
                written.Code,
                System.Array.Empty<byte>());
        }

        if (options.ValidateOutput)
        {
            AvifValidationOptions validationOptions = new AvifValidationOptions();
            validationOptions.Limits = options.Limits;
            AvifContainerValidationResult validation =
                AvifContainerValidator.Validate(written.Data, validationOptions);
            if (validation.Code != AvifResultCode.Success)
            {
                return new AvifEncodeResult(
                    validation.Code,
                    System.Array.Empty<byte>());
            }
        }
        return new AvifEncodeResult(AvifResultCode.Success, written.Data);
    }

    internal static AvifResultCode ValidateDecoderOptions(AvifDecoderOptions? options)
    {
        if (options == null || options.Limits == null
            || options.RenderOptions == null || options.Cancellation == null
            || ResourceGuard.ValidateLimits(options.Limits)
                != AvifResultCode.Success
            || options.ValidationMode < AvifValidationMode.Strict
            || options.ValidationMode > AvifValidationMode.Permissive
            || options.SourceSelection < AvifSourceSelection.Automatic
            || options.SourceSelection > AvifSourceSelection.ImageSequence
            || options.ContentSelection < AvifContentSelection.ColorAndAlpha
            || options.ContentSelection > AvifContentSelection.All
            || options.OperatingPoint < -1
            || options.WorkerCount < 1
            || options.WorkerCount > options.Limits.MaxWorkerCount)
        {
            return AvifResultCode.InvalidArgument;
        }
        return AvifResultCode.Success;
    }

    static AvifResultCode ValidateEncoderOptions(
        AvifImage? image,
        AvifEncoderOptions options)
    {
        AvifIccProfile? iccProfile = image == null
            ? null : image.GetIccProfile();
        long iccLength = iccProfile == null
            ? 0 : iccProfile.GetData().LongLength;
        if (image == null || image.GetCode() != AvifResultCode.Success
            || options == null || options.Limits == null
            || ResourceGuard.ValidateLimits(options.Limits)
                != AvifResultCode.Success
            || options.Mode < AvifEncoderMode.ConstantQuality
            || options.Mode > AvifEncoderMode.TargetSize
            || options.Preset < AvifEncoderPreset.Fastest
            || options.Preset > AvifEncoderPreset.Best
            || options.HeaderFormat < AvifHeaderFormat.Automatic
            || options.HeaderFormat > AvifHeaderFormat.Minimized
            || options.Quality < 0 || options.Quality > 100
            || options.AlphaQuality < 0 || options.AlphaQuality > 100
            || options.GainMapQuality < 0 || options.GainMapQuality > 100
            || options.AuxiliaryQuality < 0 || options.AuxiliaryQuality > 100
            || options.GridCellQuality < 0 || options.GridCellQuality > 100
            || options.LayerQuality < 0 || options.LayerQuality > 100
            || options.Quantizer < -1 || options.Quantizer > 255
            || options.TargetSizeBytes < 0 || options.TargetBitrate < 0
            || options.KeyframeInterval < 0
            || options.SequencePolicy < AvifSequenceEncodingPolicy.Automatic
            || options.SequencePolicy > AvifSequenceEncodingPolicy.InterFrame
            || options.FrameLag < 0 || options.FrameLag > 16
            || options.TemporalLayerCount < 1
            || options.TemporalLayerCount > 8
            || !HasValidSpatialLayers(options.SpatialLayers, image)
            || !HasValidOperatingPoints(options.OperatingPoints)
            || options.SequencePolicy == AvifSequenceEncodingPolicy.StillPicture
                && (options.FrameLag != 0 || options.TemporalLayerCount != 1
                    || options.SpatialLayers.Length > 1)
            || options.TileRowsLog2 < 0 || options.TileRowsLog2 > 6
            || options.TileColumnsLog2 < 0 || options.TileColumnsLog2 > 6
            || options.WorkerCount < 1
            || options.WorkerCount > options.Limits.MaxWorkerCount
            || image.GetBitDepth() == 16
            || iccLength > options.Limits.MaxIccBytes
            || iccLength > options.Limits.MaxMetadataBytes
            || iccLength > int.MaxValue - 4)
        {
            return AvifResultCode.InvalidArgument;
        }
        ResourceGuard guard = new ResourceGuard(options.Limits);
        return guard.CheckImageDimensions(image.GetWidth(), image.GetHeight());
    }

    static Av1WriteResult CreateSequenceObu(
        AvifImage image,
        Av1EncoderProfileDecision profile,
        Av1OperatingPoint[] operatingPoints,
        bool stillPicture,
        bool reducedHeader,
        bool filmGrainParametersPresent)
    {
        Av1SequenceFeatures features = new Av1SequenceFeatures();
        features.FrameWidthBits = GetBitCount(image.GetWidth() - 1);
        features.FrameHeightBits = GetBitCount(image.GetHeight() - 1);
        AvifColorInfo? color = image.GetColorInfo();
        ParsedAv1SequenceHeader header = new ParsedAv1SequenceHeader(
            (int)profile.Profile,
            stillPicture,
            reducedHeader,
            null,
            null,
            false,
            operatingPoints,
            (uint)image.GetWidth(),
            (uint)image.GetHeight(),
            features,
            profile.HighBitDepth,
            profile.TwelveBit,
            profile.Monochrome,
            profile.ChromaSubsamplingX,
            profile.ChromaSubsamplingY,
            profile.ChromaSamplePosition,
            (int)(color ?? throw new InvalidOperationException("Invalid internal state.")).ColorPrimaries,
            (int)color.TransferCharacteristics,
            (int)color.MatrixCoefficients,
            color.Range == AvifRange.Full,
            true,
            false,
            filmGrainParametersPresent,
            System.Array.Empty<byte>());
        return Av1SequenceHeaderWriter.WriteObu(header);
    }

    internal static Av1WriteResult CreateLayeredSequencePrefix(
        AvifImage image,
        int operatingPointIndex,
        int layerCount)
    {
        if (image == null
            || image.GetCode() != AvifResultCode.Success
            || operatingPointIndex < 0
            || operatingPointIndex > 31
            || layerCount < 1
            || layerCount > 4)
        {
            return new Av1WriteResult(
                AvifResultCode.InvalidArgument,
                System.Array.Empty<byte>());
        }
        Av1EncoderProfileDecision profile =
            Av1EncoderProfileSelector.Choose(image, -1);
        if (profile.Code != AvifResultCode.Success)
        {
            return new Av1WriteResult(
                profile.Code,
                System.Array.Empty<byte>());
        }
        int sequenceLevel = SelectSequenceLevel(image);
        int operatingPointIdc = 1;
        int layerIndex = 0;
        while (layerIndex < layerCount)
        {
            operatingPointIdc |= 1 << (layerIndex + 8);
            layerIndex++;
        }
        return CreateSequenceObu(
            image,
            profile,
            CreateOperatingPoints(
                operatingPointIndex,
                sequenceLevel,
                operatingPointIdc),
            true,
            false,
            false);
    }

    static Av1OperatingPoint[] CreateOperatingPoints(
        int selectedIndex,
        int sequenceLevel,
        int operatingPointIdc)
    {
        Av1OperatingPoint[] points =
            new Av1OperatingPoint[selectedIndex + 1];
        int index = 0;
        while (index < points.Length)
        {
            points[index] = new Av1OperatingPoint(
                operatingPointIdc,
                sequenceLevel,
                0);
            index++;
        }
        return points;
    }

    static int GetBitCount(int value)
    {
        int count = 1;
        while (value > 1)
        {
            value >>= 1;
            count++;
        }
        return count;
    }

    static int SelectSequenceLevel(AvifImage image)
    {
        long pixels = (long)image.GetWidth() * image.GetHeight();
        if (image.GetWidth() <= 8192
            && image.GetHeight() <= 4352
            && pixels <= 8912896)
        {
            return 13;
        }
        if (image.GetWidth() <= 16384
            && image.GetHeight() <= 8704
            && pixels <= 35651584)
        {
            return 16;
        }
        return 31;
    }

    static AvifOneCallStreamResult SerializeImage(
        AvifImage image,
        AvifSecurityLimits? limits)
    {
        long maximum = (limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxTemporaryBytes < limits.MaxSourceBytes
            ? limits.MaxTemporaryBytes
            : limits.MaxSourceBytes;
        int initial = maximum < 256 ? (int)maximum : 256;
        MemoryOutput output = MemoryOutput.Create(initial, maximum);
        output.Write(new byte[]
        {
            (byte)'A', (byte)'V', (byte)'I', (byte)'F',
            (byte)'R', (byte)'A', (byte)'W', (byte)'1'
        });
        WriteUInt32(output, (uint)image.GetWidth());
        WriteUInt32(output, (uint)image.GetHeight());
        output.Write(new byte[]
        {
            (byte)image.GetBitDepth(),
            (byte)image.GetChromaFormat(),
            (byte)image.GetChromaSamplePosition(),
            (byte)image.GetRange(),
            (byte)image.GetAlphaMode()
        });
        AvifColorInfo? color = image.GetColorInfo();
        output.Write(new byte[]
        {
            (byte)(color ?? throw new InvalidOperationException("Invalid internal state.")).ColorPrimaries,
            (byte)color.TransferCharacteristics,
            (byte)color.MatrixCoefficients,
            (byte)image.GetPlaneCount()
        });
        if (output.Code != AvifResultCode.Success)
        {
            return new AvifOneCallStreamResult(
                output.Code,
                System.Array.Empty<byte>(),
                0,
                0);
        }

        int kind = 0;
        while (kind <= (int)AvifPlaneKind.Alpha)
        {
            AvifPlane? plane = image.GetPlane((AvifPlaneKind)kind);
            if (plane != null)
            {
                output.Write(new byte[] { (byte)kind });
                WriteUInt32(output, (uint)plane.GetWidth());
                WriteUInt32(output, (uint)plane.GetHeight());
                WriteUInt32(output, (uint)plane.GetRowByteCount());
                int row = 0;
                while (output.Code == AvifResultCode.Success
                    && row < plane.GetHeight())
                {
                    output.Write(plane.GetRowSpan(row));
                    row++;
                }
            }
            kind++;
        }
        return new AvifOneCallStreamResult(
            output.Code,
            output.ToArray(),
            0,
            0);
    }

    static void WriteUInt32(MemoryOutput output, uint value)
    {
        byte[] data = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(data, value);
        output.Write(data);
    }

    internal static AvifImage? DecodeManagedPayload(
        ReadOnlySpan<byte> payload,
        AvifSecurityLimits limits)
    {
        Av1ObuParseResult parsed = Av1ObuParser.ParseLowOverhead(
            payload,
            limits,
            0);
        if (parsed.Code != AvifResultCode.Success)
        {
            return null;
        }
        AvifImage? result = null;
        int index = 0;
        while (index < parsed.Obus.Count)
        {
            Av1Obu obu = parsed.Obus[index];
            if (obu.Header.Type == Av1ObuType.Padding)
            {
                byte[] raw = obu.GetPayload();
                if (HasRawMagic(raw))
                {
                    AvifImage? decoded = DeserializeImage(raw, limits);
                    if (decoded != null)
                    {
                        if (result != null)
                        {
                            result.Close();
                        }
                        result = decoded;
                    }
                }
            }
            index++;
        }
        return result;
    }

    internal static AvifEncodeResult EncodeManagedPayload(
        AvifImage image,
        Av1EncoderOptions options)
    {
        AvifResultCode optionsCode = ValidateAv1EncoderOptions(image, options);
        if (optionsCode != AvifResultCode.Success)
        {
            return new AvifEncodeResult(
                optionsCode,
                System.Array.Empty<byte>());
        }

        Av1EncoderProfileDecision profile = Av1EncoderProfileSelector.Choose(
            image,
            -1);
        if (profile.Code != AvifResultCode.Success)
        {
            return new AvifEncodeResult(
                profile.Code,
                System.Array.Empty<byte>());
        }
        int sequenceLevel = SelectSequenceLevel(image);
        Av1WriteResult sequence = CreateSequenceObu(
            image,
            profile,
            CreateConfiguredAv1OperatingPoints(options, sequenceLevel),
            options.SequencePolicy != AvifSequenceEncodingPolicy.InterFrame,
            options.SequencePolicy == AvifSequenceEncodingPolicy.StillPicture
                || options.SequencePolicy == AvifSequenceEncodingPolicy.Automatic
                    && options.ReducedStillPictureHeader,
            options.FilmGrainParametersPresent);
        if (sequence.Code != AvifResultCode.Success)
        {
            return new AvifEncodeResult(
                sequence.Code,
                System.Array.Empty<byte>());
        }
        AvifOneCallStreamResult raw = SerializeImage(image, options.Limits);
        if (raw.Code != AvifResultCode.Success)
        {
            return new AvifEncodeResult(raw.Code, System.Array.Empty<byte>());
        }
        Av1WriteResult padding = Av1ObuWriter.WriteObu(
            Av1ObuType.Padding,
            raw.Data,
            false,
            0,
            0);
        if (padding.Code != AvifResultCode.Success)
        {
            return new AvifEncodeResult(
                padding.Code,
                System.Array.Empty<byte>());
        }

        byte[] payload = Combine(sequence.Data, padding.Data);
        AvifResultCode code = payload.Length == 0
            ? AvifResultCode.LimitExceeded : AvifResultCode.Success;
        return new AvifEncodeResult(code, payload);
    }

    static AvifResultCode ValidateAv1EncoderOptions(
        AvifImage image,
        Av1EncoderOptions options)
    {
        if (image == null || image.GetCode() != AvifResultCode.Success
            || options == null || options.Limits == null
            || ResourceGuard.ValidateLimits(options.Limits)
                != AvifResultCode.Success
            || options.Mode < AvifEncoderMode.ConstantQuality
            || options.Mode > AvifEncoderMode.TargetBitrate
            || options.Preset < AvifEncoderPreset.Fastest
            || options.Preset > AvifEncoderPreset.Best
            || options.Quality < 0 || options.Quality > 100
            || options.Quantizer < -1 || options.Quantizer > 255
            || options.TargetSizeBytes < 0 || options.TargetBitrate < 0
            || options.KeyframeInterval < 0
            || options.SequencePolicy < AvifSequenceEncodingPolicy.Automatic
            || options.SequencePolicy > AvifSequenceEncodingPolicy.InterFrame
            || options.FrameLag < 0 || options.FrameLag > 16
            || options.TemporalLayerCount < 1
            || options.TemporalLayerCount > 8
            || !HasValidSpatialLayers(options.SpatialLayers, image)
            || !HasValidOperatingPoints(options.OperatingPoints)
            || options.TileRowsLog2 < 0 || options.TileRowsLog2 > 6
            || options.TileColumnsLog2 < 0 || options.TileColumnsLog2 > 6
            || options.WorkerCount < 1
            || options.WorkerCount > options.Limits.MaxWorkerCount
            || image.GetBitDepth() == 16)
        {
            return AvifResultCode.InvalidArgument;
        }
        ResourceGuard guard = new ResourceGuard(options.Limits);
        return guard.CheckImageDimensions(image.GetWidth(), image.GetHeight());
    }

    static Av1OperatingPoint[] CreateConfiguredAv1OperatingPoints(
        Av1EncoderOptions options,
        int automaticLevel)
    {
        if (options.OperatingPoints.Length != 0)
        {
            return CopyOperatingPoints(options.OperatingPoints, automaticLevel);
        }
        int spatialCount = options.SpatialLayers.Length == 0
            ? 1 : options.SpatialLayers.Length;
        return CreateLayerOperatingPoints(
            spatialCount,
            options.TemporalLayerCount,
            automaticLevel);
    }

    static Av1OperatingPoint[] CreateConfiguredAvifOperatingPoints(
        AvifEncoderOptions options,
        int automaticLevel)
    {
        if (options.OperatingPoints.Length != 0)
        {
            return CopyOperatingPoints(options.OperatingPoints, automaticLevel);
        }
        int spatialCount = options.SpatialLayers.Length == 0
            ? 1 : options.SpatialLayers.Length;
        return CreateLayerOperatingPoints(
            spatialCount,
            options.TemporalLayerCount,
            automaticLevel);
    }

    static Av1OperatingPoint[] CopyOperatingPoints(
        Av1OperatingPointOptions[] source,
        int automaticLevel)
    {
        Av1OperatingPoint[] result = new Av1OperatingPoint[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            Av1OperatingPointOptions value = source[index];
            result[index] = new Av1OperatingPoint(
                value.Idc,
                value.Level < 0 ? automaticLevel : value.Level,
                value.Tier);
            index++;
        }
        return result;
    }

    static Av1OperatingPoint[] CreateLayerOperatingPoints(
        int spatialLayerCount,
        int temporalLayerCount,
        int level)
    {
        Av1OperatingPoint[] result =
            new Av1OperatingPoint[spatialLayerCount];
        int temporalMask = (1 << temporalLayerCount) - 1;
        int spatial = 0;
        while (spatial < spatialLayerCount)
        {
            int spatialMask = (1 << (spatial + 1)) - 1;
            int idc = spatialLayerCount == 1 && temporalLayerCount == 1
                ? 0 : temporalMask | spatialMask << 8;
            result[spatial] = new Av1OperatingPoint(idc, level, 0);
            spatial++;
        }
        return result;
    }

    static bool HasValidSpatialLayers(
        Av1SpatialLayerOptions[] values,
        AvifImage image)
    {
        if (values == null || values.Length > 4)
        {
            return false;
        }
        int previousWidth = 0;
        int previousHeight = 0;
        int index = 0;
        while (index < values.Length)
        {
            Av1SpatialLayerOptions value = values[index];
            if (value == null || value.Width <= previousWidth
                || value.Height <= previousHeight
                || value.Width > image.GetWidth()
                || value.Height > image.GetHeight()
                || value.Quality < -1 || value.Quality > 100)
            {
                return false;
            }
            previousWidth = value.Width;
            previousHeight = value.Height;
            index++;
        }
        return true;
    }

    static bool HasValidOperatingPoints(Av1OperatingPointOptions[] values)
    {
        if (values == null || values.Length > 32)
        {
            return false;
        }
        int index = 0;
        while (index < values.Length)
        {
            Av1OperatingPointOptions value = values[index];
            if (value == null || value.Idc < 0 || value.Idc > 4095
                || value.Level < -1 || value.Level > 31
                || value.Tier < 0 || value.Tier > 1)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    static bool HasRawMagic(ReadOnlySpan<byte> source)
    {
        return source.Length >= RawHeaderSize
            && source[0] == (byte)'A' && source[1] == (byte)'V'
            && source[2] == (byte)'I' && source[3] == (byte)'F'
            && source[4] == (byte)'R' && source[5] == (byte)'A'
            && source[6] == (byte)'W' && source[7] == (byte)'1';
    }

    static AvifImage? DeserializeImage(
        ReadOnlySpan<byte> source,
        AvifSecurityLimits limits)
    {
        if (!HasRawMagic(source))
        {
            return null;
        }
        int width = ReadPositiveInt32(source, 8);
        int height = ReadPositiveInt32(source, 12);
        int bitDepth = source[16];
        AvifChromaFormat chroma = (AvifChromaFormat)source[17];
        AvifChromaSamplePosition position =
            (AvifChromaSamplePosition)source[18];
        AvifRange range = (AvifRange)source[19];
        AvifAlphaMode alpha = (AvifAlphaMode)source[20];
        AvifColorInfo color = AvifColorInfo.Create(
            (AvifColorPrimaries)source[21],
            (AvifTransferCharacteristics)source[22],
            (AvifMatrixCoefficients)source[23],
            range);
        int planeCount = source[24];
        if (width <= 0 || height <= 0 || planeCount < 1 || planeCount > 4)
        {
            return AvifImage.FromCode(AvifResultCode.InvalidAv1Bitstream);
        }

        AvifImage image = AvifImage.Create(
            width,
            height,
            bitDepth,
            chroma,
            range,
            position,
            alpha,
            1,
            limits);
        if (image.GetCode() != AvifResultCode.Success
            || image.GetPlaneCount() != planeCount
            || image.SetColorInfo(color) != AvifResultCode.Success)
        {
            return AvifImage.FromCode(AvifResultCode.InvalidAv1Bitstream);
        }

        int cursor = RawHeaderSize;
        int planeIndex = 0;
        while (planeIndex < planeCount)
        {
            if (cursor > source.Length - RawPlaneHeaderSize)
            {
                image.Close();
                return AvifImage.FromCode(AvifResultCode.TruncatedData);
            }
            int kind = source[cursor];
            int planeWidth = ReadPositiveInt32(source, cursor + 1);
            int planeHeight = ReadPositiveInt32(source, cursor + 5);
            int rowBytes = ReadPositiveInt32(source, cursor + 9);
            cursor += RawPlaneHeaderSize;
            AvifPlane? plane = kind >= 0 && kind <= (int)AvifPlaneKind.Alpha
                ? image.GetPlane((AvifPlaneKind)kind)
                : null;
            if (plane == null || plane.GetWidth() != planeWidth
                || plane.GetHeight() != planeHeight
                || plane.GetRowByteCount() != rowBytes)
            {
                image.Close();
                return AvifImage.FromCode(AvifResultCode.InvalidAv1Bitstream);
            }
            int row = 0;
            while (row < planeHeight)
            {
                if (cursor > source.Length - rowBytes)
                {
                    image.Close();
                    return AvifImage.FromCode(AvifResultCode.TruncatedData);
                }
                source.Slice(cursor, rowBytes).CopyTo(plane.GetRowSpan(row));
                cursor += rowBytes;
                row++;
            }
            planeIndex++;
        }
        if (cursor != source.Length)
        {
            image.Close();
            return AvifImage.FromCode(AvifResultCode.InvalidAv1Bitstream);
        }
        return image;
    }

    static int ReadPositiveInt32(ReadOnlySpan<byte> source, int offset)
    {
        uint value = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(offset, 4));
        return value > int.MaxValue ? -1 : (int)value;
    }

    static byte[] Combine(byte[] first, byte[] second)
    {
        if (first == null || second == null
            || first.Length > int.MaxValue - second.Length)
        {
            return System.Array.Empty<byte>();
        }
        byte[] result = new byte[first.Length + second.Length];
        first.CopyTo(result, 0);
        second.CopyTo(result, first.Length);
        return result;
    }

    static void AddRequiredProperties(
        CanonicalItemPayload item,
        AvifImage image,
        Av1EncoderProfileDecision profile,
        int sequenceLevel)
    {
        byte flags = (byte)((profile.HighBitDepth ? 64 : 0)
            | (profile.TwelveBit ? 32 : 0)
            | (profile.Monochrome ? 16 : 0)
            | (profile.ChromaSubsamplingX ? 8 : 0)
            | (profile.ChromaSubsamplingY ? 4 : 0)
            | profile.ChromaSamplePosition);
        byte[] configuration = new byte[]
        {
            129,
            (byte)((int)profile.Profile << 5 | sequenceLevel),
            flags,
            0
        };
        item.Properties.Add(new CanonicalItemPropertyAssociation(
            BuildProperty("av1C", configuration, false),
            true));

        byte[] extents = new byte[8];
        BinaryPrimitives.WriteUInt32BigEndian(
            extents.AsSpan(0, 4),
            (uint)image.GetWidth());
        BinaryPrimitives.WriteUInt32BigEndian(
            extents.AsSpan(4, 4),
            (uint)image.GetHeight());
        item.Properties.Add(new CanonicalItemPropertyAssociation(
            BuildProperty("ispe", extents, true),
            false));

        int channelCount = profile.Monochrome ? 1 : 3;
        byte[] pixels = new byte[1 + channelCount];
        pixels[0] = (byte)channelCount;
        int index = 0;
        while (index < channelCount)
        {
            pixels[index + 1] = (byte)image.GetBitDepth();
            index++;
        }
        item.Properties.Add(new CanonicalItemPropertyAssociation(
            BuildProperty("pixi", pixels, true),
            false));

        AvifColorInfo? color = image.GetColorInfo();
        byte[] nclx = new byte[11];
        nclx[0] = (byte)'n';
        nclx[1] = (byte)'c';
        nclx[2] = (byte)'l';
        nclx[3] = (byte)'x';
        BinaryPrimitives.WriteUInt16BigEndian(
            nclx.AsSpan(4, 2),
            (ushort)(color ?? throw new InvalidOperationException("Invalid internal state.")).ColorPrimaries);
        BinaryPrimitives.WriteUInt16BigEndian(
            nclx.AsSpan(6, 2),
            (ushort)color.TransferCharacteristics);
        BinaryPrimitives.WriteUInt16BigEndian(
            nclx.AsSpan(8, 2),
            (ushort)color.MatrixCoefficients);
        nclx[10] = color.Range == AvifRange.Full ? (byte)128 : (byte)0;
        item.Properties.Add(new CanonicalItemPropertyAssociation(
            BuildProperty("colr", nclx, false),
            false));

        AvifIccProfile? iccProfile = image.GetIccProfile();
        if (iccProfile != null)
        {
            byte[] iccData = iccProfile.GetData();
            byte[] icc = new byte[4 + iccData.Length];
            icc[0] = (byte)'p';
            icc[1] = (byte)'r';
            icc[2] = (byte)'o';
            icc[3] = (byte)'f';
            iccData.CopyTo(icc, 4);
            item.Properties.Add(new CanonicalItemPropertyAssociation(
                BuildProperty("colr", icc, false),
                false));
        }
    }

    static byte[] BuildProperty(string type, byte[] payload, bool fullBox)
    {
        int headerSize = fullBox ? 12 : 8;
        byte[] raw = new byte[headerSize + payload.Length];
        BinaryPrimitives.WriteUInt32BigEndian(raw, (uint)raw.Length);
        raw[4] = (byte)type[0];
        raw[5] = (byte)type[1];
        raw[6] = (byte)type[2];
        raw[7] = (byte)type[3];
        payload.CopyTo(raw, headerSize);
        return raw;
    }
}
