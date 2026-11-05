using System.Collections.Generic;

namespace avif;

/// <summary>
/// Describes one directional item reference and all of its targets.
/// </summary>
public class AvifItemReference : IAvifItemReference
{
    AvifItemReferenceKind kind;
    string type;
    uint fromItemId;
    List<uint> targetItemIds;

    internal AvifItemReference(
        AvifItemReferenceKind kind,
        string type,
        uint fromItemId,
        List<uint> targetItemIds)
    {
        this.kind = kind;
        this.type = type;
        this.fromItemId = fromItemId;
        this.targetItemIds = new List<uint>(targetItemIds);
    }

    /// <summary>Returns the interpreted relationship kind.</summary>
    /// <returns>The relationship kind.</returns>
    public AvifItemReferenceKind GetKind()
    {
        return kind;
    }

    /// <summary>Returns the original four-character reference type.</summary>
    /// <returns>The reference type.</returns>
    public string GetTypeName()
    {
        return type;
    }

    /// <summary>Returns the source item identifier.</summary>
    /// <returns>The source item identifier.</returns>
    public uint GetFromItemId()
    {
        return fromItemId;
    }

    /// <summary>Returns the number of target item identifiers.</summary>
    /// <returns>The target count.</returns>
    public int GetTargetCount()
    {
        return targetItemIds.Count;
    }

    /// <summary>Returns one target item identifier.</summary>
    /// <param name="index">The zero-based target index.</param>
    /// <returns>The target identifier, or zero for an invalid index.</returns>
    public uint GetTargetItemId(int index)
    {
        if (index < 0 || index >= targetItemIds.Count)
        {
            return 0;
        }

        return targetItemIds[index];
    }
}
