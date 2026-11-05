namespace avif;

/// <summary>
/// Describes one declared and optionally resolved item-data extent.
/// </summary>
public class AvifItemExtent : IAvifItemExtent
{
    ulong extentIndex;
    ulong offset;
    ulong length;
    bool resolved;
    long resolvedOffset;
    long resolvedLength;

    internal AvifItemExtent(
        ulong extentIndex,
        ulong offset,
        ulong length,
        bool resolved,
        long resolvedOffset,
        long resolvedLength)
    {
        this.extentIndex = extentIndex;
        this.offset = offset;
        this.length = length;
        this.resolved = resolved;
        this.resolvedOffset = resolvedOffset;
        this.resolvedLength = resolvedLength;
    }

    /// <summary>Returns the construction-reference extent index.</summary>
    /// <returns>The declared extent index.</returns>
    public ulong GetExtentIndex()
    {
        return extentIndex;
    }

    /// <summary>Returns the declared extent offset.</summary>
    /// <returns>The unsigned declared offset.</returns>
    public ulong GetOffset()
    {
        return offset;
    }

    /// <summary>Returns the declared extent length.</summary>
    /// <returns>The unsigned declared length.</returns>
    public ulong GetLength()
    {
        return length;
    }

    /// <summary>Reports whether this extent resolves to source bytes.</summary>
    /// <returns><c>true</c> when resolved offset and length values are available.</returns>
    public bool IsResolved()
    {
        return resolved;
    }

    /// <summary>Returns the resolved source offset.</summary>
    /// <returns>The resolved offset, or zero when unresolved.</returns>
    public long GetResolvedOffset()
    {
        return resolvedOffset;
    }

    /// <summary>Returns the resolved source length.</summary>
    /// <returns>The resolved length, or zero when unresolved.</returns>
    public long GetResolvedLength()
    {
        return resolvedLength;
    }
}
