using System;
using System.Collections.Generic;

namespace avif;

/// <summary>Decodes managed image frames carried by a low-overhead AV1 stream.</summary>
public class Av1Decoder : IAv1Decoder, IAvifDiagnosticSource
{
    Av1DecoderOptions? options;
    byte[] source;
    List<Av1DecoderFrame> frames;
    AvifResultCode code;
    DiagnosticLog diagnostics;
    int frameIndex;
    bool loaded;
    bool closed;

    /// <summary>Creates a decoder configured with default AV1 options.</summary>
    public Av1Decoder()
    {
        options = null;
        source = System.Array.Empty<byte>();
        frames = new List<Av1DecoderFrame>();
        code = AvifResultCode.Success;
        diagnostics = new DiagnosticLog();
        frameIndex = 0;
        loaded = false;
        closed = false;
        Configure(new Av1DecoderOptions());
    }

    /// <summary>Returns the outcome of the most recent decoder operation.</summary>
    /// <returns>The stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return closed ? AvifResultCode.Closed : code;
    }

    /// <summary>Returns diagnostics for the most recent low-level AV1 decode operation.</summary>
    /// <returns>The structured AV1 diagnostics.</returns>
    public AvifDiagnosticReport GetDiagnostics()
    {
        return AvifDiagnosticReport.FromLog(
            diagnostics,
            GetCode(),
            AvifDiagnosticCategory.Av1,
            "The AV1 decode operation did not complete successfully.");
    }

    /// <summary>Replaces decoder settings before a stream is loaded.</summary>
    /// <param name="requestedOptions">The options and limits to snapshot.</param>
    /// <returns>The operation result.</returns>
    public AvifResultCode Configure(Av1DecoderOptions requestedOptions)
    {
        if (closed)
        {
            return AvifResultCode.Closed;
        }
        if (loaded)
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

    /// <summary>Copies a complete stream and decodes its first supported frame.</summary>
    /// <param name="encoded">The complete low-overhead AV1 stream.</param>
    /// <returns>The first decoded frame result.</returns>
    public AvifDecodeResult Decode(ReadOnlySpan<byte> encoded)
    {
        if (closed)
        {
            return Fail(AvifResultCode.Closed);
        }
        if (loaded)
        {
            return Fail(AvifResultCode.InvalidState);
        }
        if (options == null)
        {
            return Fail(AvifResultCode.InvalidArgument);
        }
        if (options.Cancellation.IsCancellationRequested())
        {
            return Fail(AvifResultCode.Cancelled);
        }
        if (encoded.Length > options.Limits.MaxSourceBytes)
        {
            return Fail(AvifResultCode.LimitExceeded);
        }

        source = encoded.ToArray();
        Av1ObuParseResult parsed = Av1ObuParser.ParseLowOverhead(
            source,
            options.Limits,
            0);
        diagnostics = parsed.Diagnostics;
        code = parsed.Code;
        if (code != AvifResultCode.Success)
        {
            source = System.Array.Empty<byte>();
            return Fail(code);
        }
        if (options.Cancellation.IsCancellationRequested())
        {
            source = System.Array.Empty<byte>();
            return Fail(AvifResultCode.Cancelled);
        }

        Av1LayerSelection? selection = null;
        int sequenceIndex = 0;
        while (sequenceIndex < parsed.Obus.Count)
        {
            Av1Obu sequenceObu = parsed.Obus[sequenceIndex];
            if (sequenceObu.Header.Type == Av1ObuType.SequenceHeader)
            {
                Av1SequenceHeaderParseResult sequence =
                    Av1SequenceHeaderParser.ParsePayload(
                        sequenceObu.GetPayload());
                if (sequence.Code != AvifResultCode.Success
                    || sequence.Header == null)
                {
                    source = System.Array.Empty<byte>();
                    AvifResultCode sequenceCode =
                        sequence.Code == AvifResultCode.Success
                            ? AvifResultCode.InvalidAv1Bitstream
                            : sequence.Code;
                    return Fail(sequenceCode);
                }
                selection = Av1LayerSelector.Resolve(
                    sequence.Header,
                    options);
                if (selection.Code != AvifResultCode.Success)
                {
                    source = System.Array.Empty<byte>();
                    return Fail(selection.Code);
                }
                break;
            }
            sequenceIndex++;
        }
        if (selection == null
            && (options.OperatingPoint >= 0
                || options.SpatialLayer >= 0
                || options.TemporalLayer >= 0))
        {
            source = System.Array.Empty<byte>();
            return Fail(AvifResultCode.InvalidAv1Bitstream);
        }

        frames.Clear();
        int obuIndex = 0;
        while (obuIndex < parsed.Obus.Count)
        {
            if (options.Cancellation.IsCancellationRequested())
            {
                source = System.Array.Empty<byte>();
                frames.Clear();
                return Fail(AvifResultCode.Cancelled);
            }
            Av1Obu obu = parsed.Obus[obuIndex];
            if (obu.Header.Type == Av1ObuType.Padding
                && (selection == null
                    || Av1LayerSelector.ShouldDecodeObu(
                        selection,
                        obu.Header))
                && HasManagedImageMagic(obu.GetPayload()))
            {
                frames.Add(new Av1DecoderFrame(obu.Offset, obu.TotalSize));
            }
            obuIndex++;
        }
        if (frames.Count == 0)
        {
            source = System.Array.Empty<byte>();
            return Fail(AvifResultCode.UnsupportedFeature);
        }

        frameIndex = 0;
        loaded = true;
        code = AvifResultCode.Success;
        return DecodeNext();
    }

    /// <summary>Decodes the next supported frame from the loaded stream.</summary>
    /// <returns>The decoded frame result or <c>EndOfData</c>.</returns>
    public AvifDecodeResult DecodeNext()
    {
        if (closed)
        {
            return Fail(AvifResultCode.Closed);
        }
        if (!loaded)
        {
            return Fail(AvifResultCode.InvalidState);
        }
        if ((options ?? throw new InvalidOperationException("Invalid internal state.")).Cancellation.IsCancellationRequested())
        {
            return Fail(AvifResultCode.Cancelled);
        }
        if (frameIndex >= frames.Count)
        {
            return Fail(AvifResultCode.EndOfData);
        }

        Av1DecoderFrame frame = frames[frameIndex];
        if (options.Cancellation.IsCancellationRequested())
        {
            return Fail(AvifResultCode.Cancelled);
        }
        AvifImage? image = AvifOneCallCodec.DecodeManagedPayload(
            source.AsSpan(frame.Offset, frame.Size),
            options.Limits);
        if (image == null)
        {
            return Fail(AvifResultCode.DecodeFailed);
        }
        if (options.Cancellation.IsCancellationRequested())
        {
            image.Close();
            return Fail(AvifResultCode.Cancelled);
        }
        frameIndex++;
        code = image.GetCode();
        return new AvifDecodeResult(code, image, null);
    }

    /// <summary>Selects an AV1 operating-point index for subsequent input.</summary>
    /// <param name="operatingPoint">The nonnegative index, or negative one for automatic selection.</param>
    /// <returns>The operation result.</returns>
    public AvifResultCode SelectOperatingPoint(int operatingPoint)
    {
        if (closed)
        {
            return AvifResultCode.Closed;
        }
        if (loaded)
        {
            code = AvifResultCode.InvalidState;
            return code;
        }
        if (operatingPoint < -1)
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        (options ?? throw new InvalidOperationException("Invalid internal state.")).OperatingPoint = operatingPoint;
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Discards the loaded stream while retaining configured options.</summary>
    /// <returns>The operation result.</returns>
    public AvifResultCode Reset()
    {
        if (closed)
        {
            return AvifResultCode.Closed;
        }
        source = System.Array.Empty<byte>();
        frames.Clear();
        frameIndex = 0;
        loaded = false;
        diagnostics.Clear();
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Releases the loaded stream and decoder state.</summary>
    /// <returns><c>Success</c>; repeated calls are harmless.</returns>
    public AvifResultCode Close()
    {
        if (!closed)
        {
            source = System.Array.Empty<byte>();
            frames.Clear();
            frameIndex = 0;
            loaded = false;
            closed = true;
            code = AvifResultCode.Closed;
        }
        return AvifResultCode.Success;
    }

    AvifDecodeResult Fail(AvifResultCode resultCode)
    {
        code = resultCode;
        return new AvifDecodeResult(resultCode, null, null);
    }

    static bool HasManagedImageMagic(byte[] payload)
    {
        return payload != null && payload.Length >= 8
            && payload[0] == (byte)'A' && payload[1] == (byte)'V'
            && payload[2] == (byte)'I' && payload[3] == (byte)'F'
            && payload[4] == (byte)'R' && payload[5] == (byte)'A'
            && payload[6] == (byte)'W' && payload[7] == (byte)'1';
    }

    static Av1DecoderOptions CopyOptions(Av1DecoderOptions sourceOptions)
    {
        Av1DecoderOptions copy = new Av1DecoderOptions();
        copy.OperatingPoint = sourceOptions.OperatingPoint;
        copy.SpatialLayer = sourceOptions.SpatialLayer;
        copy.TemporalLayer = sourceOptions.TemporalLayer;
        copy.AllowProgressive = sourceOptions.AllowProgressive;
        copy.DecodeLargeScaleTiles = sourceOptions.DecodeLargeScaleTiles;
        copy.AllowIncremental = sourceOptions.AllowIncremental;
        copy.ApplyFilmGrain = sourceOptions.ApplyFilmGrain;
        copy.Strict = sourceOptions.Strict;
        copy.WorkerCount = sourceOptions.WorkerCount;
        copy.Limits = ResourceGuard.CopyLimits(sourceOptions.Limits);
        copy.Cancellation = sourceOptions.Cancellation;
        return copy;
    }

    static AvifResultCode ValidateOptions(Av1DecoderOptions sourceOptions)
    {
        if (sourceOptions == null || sourceOptions.Limits == null
            || sourceOptions.Cancellation == null
            || ResourceGuard.ValidateLimits(sourceOptions.Limits)
                != AvifResultCode.Success
            || sourceOptions.OperatingPoint < -1
            || sourceOptions.SpatialLayer < -1
            || sourceOptions.TemporalLayer < -1
            || sourceOptions.WorkerCount < 1
            || sourceOptions.WorkerCount > sourceOptions.Limits.MaxWorkerCount)
        {
            return AvifResultCode.InvalidArgument;
        }
        return AvifResultCode.Success;
    }
}
