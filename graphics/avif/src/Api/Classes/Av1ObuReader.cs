using System;
using System.Collections.Generic;

namespace avif;

/// <summary>Reads low-overhead AV1 open bitstream units from a private source copy.</summary>
public class Av1ObuReader : IAv1ObuReader, IAvifDiagnosticSource
{
    byte[]? source;
    List<Av1Obu>? obus;
    Av1Obu? current;
    AvifResultCode code;
    DiagnosticLog? diagnostics;
    int index;
    bool closed;

    /// <summary>Creates an OBU reader using default resource limits.</summary>
    /// <param name="source">The complete low-overhead AV1 bitstream to copy.</param>
    /// <returns>The initialized OBU reader.</returns>
    internal static Av1ObuReader Create(byte[] source)
    {
        return new Av1ObuReader(source, new AvifSecurityLimits());
    }

    /// <summary>Creates an OBU reader using a snapshot of caller-selected limits.</summary>
    /// <param name="source">The complete low-overhead AV1 bitstream to copy.</param>
    /// <param name="limits">The resource limits to snapshot.</param>
    public Av1ObuReader(byte[] source, AvifSecurityLimits limits)
    {
        Initialize(source, limits);
    }

    /// <summary>Creates an OBU reader using caller-selected limits.</summary>
    /// <returns>The initialized OBU reader.</returns>
    internal static Av1ObuReader Create(
        byte[] source,
        AvifSecurityLimits limits)
    {
        return new Av1ObuReader(source, limits);
    }

    /// <summary>Returns the outcome of the most recent reader operation.</summary>
    /// <returns>The stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return closed ? AvifResultCode.Closed : code;
    }

    /// <summary>Returns diagnostics produced while parsing the AV1 bitstream.</summary>
    /// <returns>The structured AV1 diagnostics.</returns>
    public AvifDiagnosticReport GetDiagnostics()
    {
        return AvifDiagnosticReport.FromLog(
            diagnostics,
            GetCode(),
            AvifDiagnosticCategory.Av1,
            "The AV1 OBU reader operation did not complete successfully.");
    }

    /// <summary>Advances to the next parsed OBU.</summary>
    /// <returns><c>Success</c>, <c>EndOfData</c>, or a parse or lifecycle failure.</returns>
    public AvifResultCode ReadNext()
    {
        if (closed)
        {
            return AvifResultCode.Closed;
        }
        if (code != AvifResultCode.Success && code != AvifResultCode.EndOfData)
        {
            return code;
        }
        if (index >= (obus ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            current = null;
            code = AvifResultCode.EndOfData;
            return code;
        }

        current = obus[index];
        index++;
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Returns the numeric AV1 OBU type of the current unit.</summary>
    /// <returns>The four-bit OBU type, or zero when no unit is selected.</returns>
    public int GetObuType()
    {
        return closed || current == null ? 0 : (int)current.Header.Type;
    }

    /// <summary>Reports whether the current unit carries an extension header.</summary>
    /// <returns><c>true</c> when temporal and spatial identifiers are present.</returns>
    public bool HasExtension()
    {
        return !closed && current != null && current.Header.HasExtension;
    }

    /// <summary>Returns the current unit temporal identifier.</summary>
    /// <returns>The temporal identifier, or zero when unavailable.</returns>
    public int GetTemporalId()
    {
        return closed || current == null ? 0 : current.Header.TemporalId;
    }

    /// <summary>Returns the current unit spatial identifier.</summary>
    /// <returns>The spatial identifier, or zero when unavailable.</returns>
    public int GetSpatialId()
    {
        return closed || current == null ? 0 : current.Header.SpatialId;
    }

    /// <summary>Returns the current unit byte offset in the retained stream.</summary>
    /// <returns>The zero-based offset, or negative one when unavailable.</returns>
    public int GetOffset()
    {
        return closed || current == null ? -1 : current.Offset;
    }

    /// <summary>Returns the complete encoded size of the current unit.</summary>
    /// <returns>The byte size, or zero when unavailable.</returns>
    public int GetSize()
    {
        return closed || current == null ? 0 : current.TotalSize;
    }

    /// <summary>Returns a copy of the current OBU payload.</summary>
    /// <returns>The payload, or an empty array when no unit is selected.</returns>
    public byte[] GetPayload()
    {
        return closed || current == null
            ? System.Array.Empty<byte>() : current.GetPayload();
    }

    /// <summary>Returns the current sequence-header payload.</summary>
    /// <returns>A payload copy, or an empty array when the current unit is not a sequence header.</returns>
    public byte[] GetSequenceHeader()
    {
        return closed || current == null
            || current.Header.Type != Av1ObuType.SequenceHeader
            ? System.Array.Empty<byte>() : current.GetPayload();
    }

    /// <summary>Returns the current metadata OBU payload.</summary>
    /// <returns>A payload copy, or an empty array when the current unit is not metadata.</returns>
    public byte[] GetMetadata()
    {
        return closed || current == null
            || current.Header.Type != Av1ObuType.Metadata
            ? System.Array.Empty<byte>() : current.GetPayload();
    }

    /// <summary>Rewinds traversal to the first parsed OBU.</summary>
    /// <returns>The operation result.</returns>
    public AvifResultCode Reset()
    {
        if (closed)
        {
            return AvifResultCode.Closed;
        }
        if (code != AvifResultCode.Success && code != AvifResultCode.EndOfData)
        {
            return code;
        }
        index = 0;
        current = null;
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Releases retained source and parsed units.</summary>
    /// <returns><c>Success</c>; repeated calls are harmless.</returns>
    public AvifResultCode Close()
    {
        if (!closed)
        {
            source = System.Array.Empty<byte>();
            (obus ?? throw new InvalidOperationException("Invalid internal state.")).Clear();
            current = null;
            index = 0;
            closed = true;
            code = AvifResultCode.Closed;
        }
        return AvifResultCode.Success;
    }

    void Initialize(byte[] requestedSource, AvifSecurityLimits requestedLimits)
    {
        source = System.Array.Empty<byte>();
        obus = new List<Av1Obu>();
        current = null;
        index = 0;
        closed = false;
        diagnostics = new DiagnosticLog();
        if (requestedSource == null || requestedLimits == null)
        {
            code = AvifResultCode.InvalidArgument;
            return;
        }

        AvifSecurityLimits limits = ResourceGuard.CopyLimits(requestedLimits);
        ResourceGuard resources = new ResourceGuard(limits);
        code = resources.CountSourceBytes(requestedSource.LongLength);
        if (code != AvifResultCode.Success)
        {
            return;
        }
        source = (byte[])requestedSource.Clone();
        Av1ObuParseResult parsed = Av1ObuParser.ParseLowOverhead(source, limits, 0);
        diagnostics = parsed.Diagnostics;
        code = parsed.Code;
        if (code == AvifResultCode.Success)
        {
            obus.AddRange(parsed.Obus);
        }
    }
}
