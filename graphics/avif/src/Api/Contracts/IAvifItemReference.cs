using System.Collections.Generic;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifItemReference"/>.</summary>
public interface IAvifItemReference
{
    /// <summary>Returns the interpreted relationship kind.</summary>
    /// <returns>The relationship kind.</returns>
    AvifItemReferenceKind GetKind();

    /// <summary>Returns the original four-character reference type.</summary>
    /// <returns>The reference type.</returns>
    string GetTypeName();

    /// <summary>Returns the source item identifier.</summary>
    /// <returns>The source item identifier.</returns>
    uint GetFromItemId();

    /// <summary>Returns the number of target item identifiers.</summary>
    /// <returns>The target count.</returns>
    int GetTargetCount();

    /// <summary>Returns one target item identifier.</summary>
    /// <param name="index">The zero-based target index.</param>
    /// <returns>The target identifier, or zero for an invalid index.</returns>
    uint GetTargetItemId(int index);
}
