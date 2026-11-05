namespace raw;

/// <summary>Describes one exact byte range and its preservation requirements.</summary>
public class RawPayloadBlock
{
    /// <summary>The semantic payload kind.</summary>
    public RawPayloadKind Kind;
    /// <summary>A stable container path or caller-facing label.</summary>
    public string Path;
    /// <summary>The zero-based byte offset in the bounded source.</summary>
    public long Offset;
    /// <summary>The payload length in bytes.</summary>
    public long Length;
    /// <summary>The associated image index, or <c>-1</c>.</summary>
    public int ImageIndex;
    /// <summary>The associated plane index, or <c>-1</c>.</summary>
    public int PlaneIndex;
    /// <summary>The associated strip, tile, or segment index, or <c>-1</c>.</summary>
    public int BlockIndex;
    /// <summary>The compression used by the payload when known.</summary>
    public RawCompression Compression;
    /// <summary>Whether the current library can decode the payload.</summary>
    public bool Decodable;
    /// <summary>Whether a preserving rewrite must retain these bytes exactly.</summary>
    public bool PreserveByteExact;

    /// <summary>Creates a mapped payload block.</summary>
    /// <param name="kind">The semantic payload kind.</param>
    /// <param name="path">The stable container path or label.</param>
    /// <param name="offset">The byte offset.</param>
    /// <param name="length">The byte length.</param>
    /// <param name="imageIndex">The image index, or <c>-1</c>.</param>
    /// <param name="planeIndex">The plane index, or <c>-1</c>.</param>
    /// <param name="blockIndex">The block index, or <c>-1</c>.</param>
    /// <param name="compression">The payload compression.</param>
    /// <param name="decodable">Whether decoding is supported.</param>
    /// <param name="preserveByteExact">Whether exact byte retention is required.</param>
    public RawPayloadBlock(RawPayloadKind kind, string path, long offset, long length, int imageIndex, int planeIndex, int blockIndex, RawCompression compression, bool decodable, bool preserveByteExact)
    {
        Kind = kind;
        Path = path == null ? string.Empty : path;
        Offset = offset;
        Length = length;
        ImageIndex = imageIndex;
        PlaneIndex = planeIndex;
        BlockIndex = blockIndex;
        Compression = compression;
        Decodable = decodable;
        PreserveByteExact = preserveByteExact;
    }

    /// <summary>Returns whether a byte range lies completely inside this block.</summary>
    /// <param name="offset">The candidate byte offset.</param>
    /// <param name="length">The candidate byte length.</param>
    /// <returns><c>true</c> when the complete range is contained.</returns>
    internal bool Contains(long offset, long length)
    {
        if (Offset < 0 || Length < 0 || offset < Offset || length < 0 || Offset > long.MaxValue - Length || offset > long.MaxValue - length)
        {
            return false;
        }
        return offset + length <= Offset + Length;
    }

    /// <summary>Validates the block against an exact bounded source length.</summary>
    /// <param name="sourceLength">The bounded source length.</param>
    /// <returns>All block validation findings.</returns>
    internal RawValidationReport Validate(long sourceLength)
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (Kind <= RawPayloadKind.Unknown || Kind > RawPayloadKind.Authenticity)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "Payload.Kind", "A mapped payload requires a defined semantic kind.");
        }
        if (Path == null || Path.Length == 0)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, "Payload.Path", "The payload path must be nonempty.");
        }
        if (Offset < 0 || Length <= 0 || sourceLength < 0)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "Payload.Range", "Payload and source ranges must be nonnegative and payload length must be positive.");
        }
        else if (Offset > long.MaxValue - Length || Offset + Length > sourceLength)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidOffset, "Payload.Range", "The payload range exceeds the bounded source.");
        }
        if (ImageIndex < -1 || PlaneIndex < -1 || BlockIndex < -1)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, "Payload.Indices", "Payload indices must be nonnegative or minus one when not applicable.");
        }
        if (Compression < RawCompression.Unknown || Compression > RawCompression.NikonHeStar)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "Payload.Compression", "The payload compression is not defined.");
        }
        return new RawValidationReport(true, messages.ToArray());
    }
}

/// <summary>Maps all known byte ranges in one bounded raw input.</summary>
public class RawPayloadMap
{
    /// <summary>The exact bounded input length.</summary>
    public long SourceLength;
    /// <summary>Mapped blocks in deterministic container order.</summary>
    public RawPayloadBlock[] Blocks;

    /// <summary>Creates a payload map.</summary>
    /// <param name="sourceLength">The exact bounded source length.</param>
    /// <param name="blocks">The mapped blocks.</param>
    public RawPayloadMap(long sourceLength, RawPayloadBlock[]? blocks)
    {
        SourceLength = sourceLength;
        Blocks = blocks == null ? System.Array.Empty<RawPayloadBlock>() : blocks;
    }

    /// <summary>Returns all mapped blocks of one semantic kind.</summary>
    /// <param name="kind">The payload kind to select.</param>
    /// <returns>Matching blocks in map order.</returns>
    internal RawPayloadBlock[] Find(RawPayloadKind kind)
    {
        System.Collections.Generic.List<RawPayloadBlock> matches = new System.Collections.Generic.List<RawPayloadBlock>();
        if (Blocks == null)
        {
            return matches.ToArray();
        }
        int index = 0;
        while (index < Blocks.Length)
        {
            RawPayloadBlock block = Blocks[index];
            if (block != null && block.Kind == kind)
            {
                matches.Add(block);
            }
            index++;
        }
        return matches.ToArray();
    }

    /// <summary>Returns the sum of mapped block lengths, including intentionally aliased ranges.</summary>
    /// <returns>The checked sum of mapped byte counts.</returns>
    /// <exception cref="RawFormatException">The map is invalid or the sum overflows.</exception>
    internal long GetMappedByteCount()
    {
        RawValidationReport report = Validate();
        if (!report.IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "A valid payload map is required.", -1);
        }
        long total = 0;
        int index = 0;
        while (index < Blocks.Length)
        {
            if (total > long.MaxValue - Blocks[index].Length)
            {
                throw new RawFormatException(RawDiagnosticCode.ArithmeticOverflow, "The mapped payload byte count overflows.", -1);
            }
            total += Blocks[index].Length;
            index++;
        }
        return total;
    }

    /// <summary>Validates every block against the bounded source.</summary>
    /// <returns>All payload-map validation findings.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        System.Collections.Generic.Dictionary<RawPayloadKind, System.Collections.Generic.HashSet<string>> paths = new System.Collections.Generic.Dictionary<RawPayloadKind, System.Collections.Generic.HashSet<string>>();
        System.Collections.Generic.List<RawPayloadBlock> ranges = new System.Collections.Generic.List<RawPayloadBlock>();
        if (SourceLength < 0)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "PayloadMap.SourceLength", "The payload-map source length cannot be negative.");
        }
        if (Blocks == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, "PayloadMap.Blocks", "The payload block collection cannot be null.");
            return new RawValidationReport(true, messages.ToArray());
        }
        int index = 0;
        while (index < Blocks.Length)
        {
            RawPayloadBlock block = Blocks[index];
            if (block == null)
            {
                RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, "PayloadMap.Blocks", "Every payload-map entry must be populated.");
            }
            else
            {
                RawModelValidation.AddReport(messages, block.Validate(SourceLength));
                if (block.Path != null && block.Path.Length != 0)
                {
                    if (!paths.ContainsKey(block.Kind))
                    {
                        paths[block.Kind] = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
                    }
                    if (!paths[block.Kind].Add(block.Path))
                    {
                        RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "PayloadMap.Paths", "Payload paths must be unique within each semantic kind.");
                    }
                }
                if (block.Offset >= 0 && block.Length > 0 && block.Offset <= long.MaxValue - block.Length)
                {
                    ranges.Add(block);
                }
            }
            index++;
        }
        ranges.Sort((left, right) => CompareRanges(left, right));
        System.Collections.Generic.List<long> activeEnds = new System.Collections.Generic.List<long>();
        index = 0;
        while (index < ranges.Count)
        {
            RawPayloadBlock block = ranges[index];
            long end = block.Offset + block.Length;
            while (activeEnds.Count != 0 && block.Offset >= activeEnds[activeEnds.Count - 1])
            {
                activeEnds.RemoveAt(activeEnds.Count - 1);
            }
            if (activeEnds.Count != 0 && end > activeEnds[activeEnds.Count - 1])
            {
                RawModelValidation.Add(messages, RawDiagnosticCode.InvalidContainer, "PayloadMap.Overlap", "Payload ranges may be disjoint, exact aliases, or nested, but cannot partially cross.");
            }
            activeEnds.Add(end);
            index++;
        }
        return new RawValidationReport(true, messages.ToArray());
    }

    internal static int CompareRanges(RawPayloadBlock left, RawPayloadBlock right)
    {
        if (left.Offset < right.Offset)
        {
            return -1;
        }
        if (left.Offset > right.Offset)
        {
            return 1;
        }
        long leftEnd = left.Offset + left.Length;
        long rightEnd = right.Offset + right.Length;
        if (leftEnd > rightEnd)
        {
            return -1;
        }
        if (leftEnd < rightEnd)
        {
            return 1;
        }
        return 0;
    }

}
