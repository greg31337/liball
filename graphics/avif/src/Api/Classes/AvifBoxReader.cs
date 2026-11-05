using System;
using System.Collections.Generic;

namespace avif;

/// <summary>Reads an ISO box hierarchy from a private copy of a complete byte source.</summary>
public class AvifBoxReader : IAvifBoxReader
{
    byte[]? source;
    List<AvifBox>? boxes;
    List<AvifBoxReaderFrame>? frames;
    int index;
    AvifBox? current;
    AvifResultCode code;
    bool closed;

    /// <summary>Creates a box reader using default resource limits.</summary>
    /// <param name="source">The complete ISO box byte source to copy.</param>
    /// <returns>The initialized box reader.</returns>
    internal static AvifBoxReader Create(byte[] source)
    {
        return new AvifBoxReader(source, new AvifSecurityLimits());
    }

    /// <summary>Creates a box reader using a snapshot of caller-selected limits.</summary>
    /// <param name="source">The complete ISO box byte source to copy.</param>
    /// <param name="limits">The resource limits to snapshot.</param>
    public AvifBoxReader(byte[] source, AvifSecurityLimits limits)
    {
        Initialize(source, limits);
    }

    /// <summary>Creates a box reader using caller-selected limits.</summary>
    /// <returns>The initialized box reader.</returns>
    internal static AvifBoxReader Create(
        byte[] source,
        AvifSecurityLimits limits)
    {
        return new AvifBoxReader(source, limits);
    }

    /// <summary>Returns the outcome of the most recent reader operation.</summary>
    /// <returns>The stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return closed ? AvifResultCode.Closed : code;
    }

    /// <summary>Advances to the next box at the current hierarchy level.</summary>
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
        if (index >= (boxes ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            current = null;
            code = AvifResultCode.EndOfData;
            return code;
        }

        current = boxes[index];
        index++;
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Returns the current box selected by <c>ReadNext</c>.</summary>
    /// <returns>The immutable box description, or null when no box is selected.</returns>
    public AvifBox? GetCurrentBox()
    {
        return closed ? null : current;
    }

    /// <summary>Returns a copy of the current box payload bytes.</summary>
    /// <returns>The payload, or an empty array when no box is selected.</returns>
    public byte[] GetPayload()
    {
        if (closed || current == null)
        {
            return System.Array.Empty<byte>();
        }
        long payloadOffset = current.GetOffset() + current.GetHeaderSize();
        long payloadLength = current.GetSize() - current.GetHeaderSize();
        if (payloadOffset < 0 || payloadLength < 0
            || payloadOffset > (source ?? throw new InvalidOperationException("Invalid internal state.")).Length
            || payloadLength > source.Length - payloadOffset
            || payloadOffset > int.MaxValue || payloadLength > int.MaxValue)
        {
            return System.Array.Empty<byte>();
        }
        return source.AsSpan((int)payloadOffset, (int)payloadLength).ToArray();
    }

    /// <summary>Enters the immediate children of the current box.</summary>
    /// <returns>The operation result.</returns>
    public AvifResultCode Enter()
    {
        if (closed)
        {
            return AvifResultCode.Closed;
        }
        if (current == null || current.GetChildCount() == 0)
        {
            code = AvifResultCode.InvalidState;
            return code;
        }

        (frames ?? throw new InvalidOperationException("Invalid internal state.")).Add(new AvifBoxReaderFrame(boxes, index));
        boxes = new List<AvifBox>();
        int childIndex = 0;
        while (childIndex < current.GetChildCount())
        {
            boxes.Add(current.GetChild(childIndex)
                ?? throw new InvalidOperationException("Invalid internal state."));
            childIndex++;
        }
        index = 0;
        current = null;
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Leaves the current child level and resumes its parent level.</summary>
    /// <returns>The operation result.</returns>
    public AvifResultCode Leave()
    {
        if (closed)
        {
            return AvifResultCode.Closed;
        }
        if ((frames ?? throw new InvalidOperationException("Invalid internal state.")).Count == 0)
        {
            code = AvifResultCode.InvalidState;
            return code;
        }

        int frameIndex = frames.Count - 1;
        AvifBoxReaderFrame frame = frames[frameIndex];
        frames.RemoveAt(frameIndex);
        boxes = frame.Boxes;
        index = frame.Index;
        current = null;
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Clears the current box selection without entering its children.</summary>
    /// <returns>The operation result.</returns>
    public AvifResultCode Skip()
    {
        if (closed)
        {
            return AvifResultCode.Closed;
        }
        if (current == null)
        {
            code = AvifResultCode.InvalidState;
            return code;
        }
        current = null;
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Releases retained source and traversal state.</summary>
    /// <returns><c>Success</c>; repeated calls are harmless.</returns>
    public AvifResultCode Close()
    {
        if (!closed)
        {
            source = System.Array.Empty<byte>();
            (boxes ?? throw new InvalidOperationException("Invalid internal state.")).Clear();
            (frames ?? throw new InvalidOperationException("Invalid internal state.")).Clear();
            current = null;
            closed = true;
            code = AvifResultCode.Closed;
        }
        return AvifResultCode.Success;
    }

    void Initialize(byte[] requestedSource, AvifSecurityLimits requestedLimits)
    {
        source = System.Array.Empty<byte>();
        boxes = new List<AvifBox>();
        frames = new List<AvifBoxReaderFrame>();
        index = 0;
        current = null;
        closed = false;
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
        IsoBoxParseResult parsed = IsoBoxReader.Parse(source, limits, true);
        code = parsed.Code;
        if (code != AvifResultCode.Success)
        {
            return;
        }
        List<AvifBox> flat = new List<AvifBox>();
        int boxIndex = 0;
        while (boxIndex < parsed.Boxes.Count)
        {
            boxes.Add(AvifDocumentBuilder.CreateBox(
                source,
                parsed.Boxes[boxIndex],
                string.Empty,
                flat));
            boxIndex++;
        }
    }
}
