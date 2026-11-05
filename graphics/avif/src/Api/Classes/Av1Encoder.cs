using System;
using System.Collections.Generic;

namespace avif;

/// <summary>Encodes caller-owned images into a managed low-overhead AV1 stream.</summary>
public class Av1Encoder : IAv1Encoder, IAvifDiagnosticSource
{
    Av1EncoderOptions? options;
    List<byte[]> frames;
    List<long> durations;
    List<AvifMetadataObu> metadataObus;
    byte[] finishedData;
    AvifResultCode code;
    bool finished;
    bool closed;

    /// <summary>Creates an encoder configured with default AV1 options.</summary>
    public Av1Encoder()
    {
        options = null;
        frames = new List<byte[]>();
        durations = new List<long>();
        metadataObus = new List<AvifMetadataObu>();
        finishedData = System.Array.Empty<byte>();
        code = AvifResultCode.Success;
        finished = false;
        closed = false;
        Configure(new Av1EncoderOptions());
    }

    /// <summary>Returns the outcome of the most recent encoder operation.</summary>
    /// <returns>The stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return closed ? AvifResultCode.Closed : code;
    }

    /// <summary>Returns diagnostics for the most recent low-level AV1 encode operation.</summary>
    /// <returns>The structured AV1 encoding diagnostics.</returns>
    public AvifDiagnosticReport GetDiagnostics()
    {
        return AvifDiagnosticReport.FromCode(
            GetCode(),
            AvifDiagnosticCategory.Encoding,
            "The AV1 encode operation did not complete successfully.");
    }

    /// <summary>Replaces encoder settings before any frame has been added.</summary>
    /// <param name="requestedOptions">The options and limits to snapshot.</param>
    /// <returns>The operation result.</returns>
    public AvifResultCode Configure(Av1EncoderOptions requestedOptions)
    {
        if (closed)
        {
            return AvifResultCode.Closed;
        }
        if (frames.Count != 0 || finished)
        {
            code = AvifResultCode.InvalidState;
            return code;
        }
        code = ValidateOptions(requestedOptions);
        if (code == AvifResultCode.Success)
        {
            options = CopyOptions(requestedOptions);
        }
        return code;
    }

    /// <summary>Returns an independent copy of the active encoder controls.</summary>
    /// <returns>The active options, or null after closure.</returns>
    public Av1EncoderOptions? GetOptions()
    {
        return closed || options == null ? null : CopyOptions(options);
    }

    /// <summary>Snapshots and encodes one frame with a positive caller-defined duration.</summary>
    /// <param name="image">The image to encode immediately.</param>
    /// <param name="duration">The positive duration associated with the frame.</param>
    /// <returns>The operation result.</returns>
    public AvifResultCode AddFrame(AvifImage image, long duration)
    {
        AvifResultCode state = CheckBuildingState();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        if (image == null || image.GetCode() != AvifResultCode.Success
            || duration <= 0)
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        if (frames.Count >= (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits.MaxFrameCount)
        {
            code = AvifResultCode.LimitExceeded;
            return code;
        }

        AvifEncodeResult encoded = AvifOneCallCodec.EncodeManagedPayload(
            image,
            options);
        code = encoded.GetCode();
        if (code != AvifResultCode.Success)
        {
            return code;
        }
        byte[] payload = encoded.GetData();
        if (payload.Length == 0)
        {
            code = AvifResultCode.EncodeFailed;
            return code;
        }

        long total = payload.Length;
        int frameIndex = 0;
        while (frameIndex < frames.Count)
        {
            total += frames[frameIndex].Length;
            if (total > options.Limits.MaxSourceBytes || total > int.MaxValue)
            {
                code = AvifResultCode.LimitExceeded;
                return code;
            }
            frameIndex++;
        }
        frames.Add((byte[])payload.Clone());
        durations.Add(duration);
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Appends one opaque metadata OBU to the authored stream.</summary>
    /// <param name="metadata">The unknown metadata value to snapshot.</param>
    /// <returns>Success, InvalidArgument, InvalidState, or Closed.</returns>
    public AvifResultCode AddMetadataObu(AvifMetadataObu metadata)
    {
        AvifResultCode state = CheckBuildingState();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        if (metadata == null
            || metadata.GetCode() != AvifResultCode.Success)
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        metadataObus.Add(new AvifMetadataObu(
            metadata.GetMetadataType(),
            metadata.GetData()));
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Finalizes all queued frames into one low-overhead AV1 byte stream.</summary>
    /// <returns>The encoded result.</returns>
    public AvifEncodeResult Finish()
    {
        if (closed)
        {
            return new AvifEncodeResult(
                AvifResultCode.Closed,
                System.Array.Empty<byte>());
        }
        if (finished)
        {
            return new AvifEncodeResult(AvifResultCode.Success, finishedData);
        }
        if (code != AvifResultCode.Success)
        {
            return new AvifEncodeResult(code, System.Array.Empty<byte>());
        }
        if (frames.Count == 0)
        {
            code = AvifResultCode.InvalidState;
            return new AvifEncodeResult(code, System.Array.Empty<byte>());
        }

        MemoryOutput output = MemoryOutput.Create(256, (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits.MaxSourceBytes);
        int frameIndex = 0;
        while (frameIndex < frames.Count && output.Code == AvifResultCode.Success)
        {
            output.Write(frames[frameIndex]);
            frameIndex++;
        }
        int metadataIndex = 0;
        while (metadataIndex < metadataObus.Count
            && output.Code == AvifResultCode.Success)
        {
            output.Write(metadataObus[metadataIndex].GetEncodedObu());
            metadataIndex++;
        }
        code = output.Code;
        if (code == AvifResultCode.Success)
        {
            finishedData = output.ToArray();
            finished = true;
        }
        return new AvifEncodeResult(code, finishedData);
    }

    /// <summary>Releases queued frames and encoded output.</summary>
    /// <returns><c>Success</c>; repeated calls are harmless.</returns>
    public AvifResultCode Close()
    {
        if (!closed)
        {
            frames.Clear();
            durations.Clear();
            metadataObus.Clear();
            finishedData = System.Array.Empty<byte>();
            finished = false;
            closed = true;
            code = AvifResultCode.Closed;
        }
        return AvifResultCode.Success;
    }

    AvifResultCode CheckBuildingState()
    {
        if (closed)
        {
            code = AvifResultCode.Closed;
        }
        else if (finished)
        {
            code = AvifResultCode.InvalidState;
        }
        return code;
    }

    static Av1EncoderOptions CopyOptions(Av1EncoderOptions source)
    {
        Av1EncoderOptions copy = new Av1EncoderOptions();
        copy.Mode = source.Mode;
        copy.Preset = source.Preset;
        copy.Quality = source.Quality;
        copy.Quantizer = source.Quantizer;
        copy.TargetSizeBytes = source.TargetSizeBytes;
        copy.TargetBitrate = source.TargetBitrate;
        copy.KeyframeInterval = source.KeyframeInterval;
        copy.SequencePolicy = source.SequencePolicy;
        copy.FrameLag = source.FrameLag;
        copy.EnableShowExistingFrames = source.EnableShowExistingFrames;
        copy.EnableGlobalMotion = source.EnableGlobalMotion;
        copy.EnableWarpedMotion = source.EnableWarpedMotion;
        copy.FilmGrainParametersPresent = source.FilmGrainParametersPresent;
        copy.TemporalLayerCount = source.TemporalLayerCount;
        copy.SpatialLayers = CopySpatialLayers(source.SpatialLayers);
        copy.OperatingPoints = CopyOperatingPoints(source.OperatingPoints);
        copy.ProgressiveOutput = source.ProgressiveOutput;
        copy.ReducedStillPictureHeader = source.ReducedStillPictureHeader;
        copy.AutoTiling = source.AutoTiling;
        copy.TileRowsLog2 = source.TileRowsLog2;
        copy.TileColumnsLog2 = source.TileColumnsLog2;
        copy.WorkerCount = source.WorkerCount;
        copy.Deterministic = source.Deterministic;
        copy.Limits = ResourceGuard.CopyLimits(source.Limits);
        return copy;
    }

    static AvifResultCode ValidateOptions(Av1EncoderOptions source)
    {
        if (source == null || source.Limits == null
            || ResourceGuard.ValidateLimits(source.Limits)
                != AvifResultCode.Success
            || source.Mode < AvifEncoderMode.ConstantQuality
            || source.Mode > AvifEncoderMode.TargetBitrate
            || source.Preset < AvifEncoderPreset.Fastest
            || source.Preset > AvifEncoderPreset.Best
            || source.Quality < 0 || source.Quality > 100
            || source.Quantizer < -1 || source.Quantizer > 255
            || source.TargetSizeBytes < 0 || source.TargetBitrate < 0
            || source.KeyframeInterval < 0
            || source.SequencePolicy < AvifSequenceEncodingPolicy.Automatic
            || source.SequencePolicy > AvifSequenceEncodingPolicy.InterFrame
            || source.FrameLag < 0 || source.FrameLag > 16
            || source.TemporalLayerCount < 1 || source.TemporalLayerCount > 8
            || !HasValidSpatialLayers(source.SpatialLayers)
            || !HasValidOperatingPoints(source.OperatingPoints)
            || source.SequencePolicy == AvifSequenceEncodingPolicy.StillPicture
                && (source.FrameLag != 0 || source.TemporalLayerCount != 1
                    || source.SpatialLayers.Length > 1)
            || source.TileRowsLog2 < 0 || source.TileRowsLog2 > 6
            || source.TileColumnsLog2 < 0 || source.TileColumnsLog2 > 6
            || source.WorkerCount < 1
            || source.WorkerCount > source.Limits.MaxWorkerCount)
        {
            return AvifResultCode.InvalidArgument;
        }
        return AvifResultCode.Success;
    }

    static Av1SpatialLayerOptions[] CopySpatialLayers(
        Av1SpatialLayerOptions[] source)
    {
        if (source == null)
        {
            return System.Array.Empty<Av1SpatialLayerOptions>();
        }
        Av1SpatialLayerOptions[] result =
            new Av1SpatialLayerOptions[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            Av1SpatialLayerOptions value = source[index];
            Av1SpatialLayerOptions copy = new Av1SpatialLayerOptions();
            if (value != null)
            {
                copy.Width = value.Width;
                copy.Height = value.Height;
                copy.Quality = value.Quality;
            }
            result[index] = copy;
            index++;
        }
        return result;
    }

    static Av1OperatingPointOptions[] CopyOperatingPoints(
        Av1OperatingPointOptions[] source)
    {
        if (source == null)
        {
            return System.Array.Empty<Av1OperatingPointOptions>();
        }
        Av1OperatingPointOptions[] result =
            new Av1OperatingPointOptions[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            Av1OperatingPointOptions value = source[index];
            Av1OperatingPointOptions copy = new Av1OperatingPointOptions();
            if (value != null)
            {
                copy.Idc = value.Idc;
                copy.Level = value.Level;
                copy.Tier = value.Tier;
            }
            result[index] = copy;
            index++;
        }
        return result;
    }

    static bool HasValidSpatialLayers(Av1SpatialLayerOptions[] values)
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
}
