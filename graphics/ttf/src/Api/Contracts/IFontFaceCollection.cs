namespace ttf;

/// <summary>Provides indexed access to an ordered collection of loaded font faces.</summary>
/// <remarks>
/// Implementations may create faces lazily. Callers should obtain the count once
/// and access each index at most once when taking a snapshot.
/// </remarks>
public interface IFontFaceCollection
{
    /// <summary>Gets the number of faces in the collection.</summary>
    /// <returns>The face count.</returns>
    int GetFaceCount();

    /// <summary>Gets one face in collection order.</summary>
    /// <param name="index">The zero-based face index.</param>
    /// <returns>The loaded face.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the collection's face range.
    /// </exception>
    FontFace GetFace(int index);
}
