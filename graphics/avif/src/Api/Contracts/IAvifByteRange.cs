namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifByteRange"/>.</summary>
public interface IAvifByteRange
{
    /// <summary>Returns the absolute source offset.</summary>
    /// <returns>The byte offset.</returns>
    ulong GetOffset();

    /// <summary>Returns the range length.</summary>
    /// <returns>The byte count.</returns>
    ulong GetLength();

    /// <summary>Returns the exclusive end offset.</summary>
    /// <returns>The offset immediately following the range.</returns>
    ulong GetEndOffset();
}
