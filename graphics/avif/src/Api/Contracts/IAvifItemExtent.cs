namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifItemExtent"/>.</summary>
public interface IAvifItemExtent
{
    /// <summary>Returns the construction-reference extent index.</summary>
    /// <returns>The declared extent index.</returns>
    ulong GetExtentIndex();

    /// <summary>Returns the declared extent offset.</summary>
    /// <returns>The unsigned declared offset.</returns>
    ulong GetOffset();

    /// <summary>Returns the declared extent length.</summary>
    /// <returns>The unsigned declared length.</returns>
    ulong GetLength();

    /// <summary>Reports whether this extent resolves to source bytes.</summary>
    /// <returns><c>true</c> when resolved offset and length values are available.</returns>
    bool IsResolved();

    /// <summary>Returns the resolved source offset.</summary>
    /// <returns>The resolved offset, or zero when unresolved.</returns>
    long GetResolvedOffset();

    /// <summary>Returns the resolved source length.</summary>
    /// <returns>The resolved length, or zero when unresolved.</returns>
    long GetResolvedLength();
}
