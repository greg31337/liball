namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfToUnicodeMapping"/>.</summary>
public interface IPdfToUnicodeMapping
{
    /// <summary>Gets the fixed-width PDF character code.</summary>
    /// <returns>A value from zero through 65535.</returns>
    int GetCharacterCode();

    /// <summary>Gets the number of mapped Unicode scalars.</summary>
    /// <returns>At least one.</returns>
    int GetUnicodeScalarCount();

    /// <summary>Gets one mapped Unicode scalar.</summary>
    /// <param name="index">The zero-based scalar index.</param>
    /// <returns>A valid Unicode scalar value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the sequence.
    /// </exception>
    int GetUnicodeScalar(int index);

    /// <summary>Copies the mapped Unicode sequence.</summary>
    /// <returns>A new array containing valid Unicode scalars.</returns>
    int[] GetUnicodeSequence();
}
