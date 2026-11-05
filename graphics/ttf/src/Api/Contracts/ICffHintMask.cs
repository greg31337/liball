namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="CffHintMask"/>.</summary>
public interface ICffHintMask
{
    /// <summary>Reports whether the operation is a counter mask.</summary>
    /// <returns>True for <c>cntrmask</c>; false for <c>hintmask</c>.</returns>
    bool IsCounterMask();

    /// <summary>Gets the preserved mask byte count.</summary>
    /// <returns>The mask byte count.</returns>
    int GetLength();

    /// <summary>Gets the normalized path-command index at which the mask occurs.</summary>
    /// <returns>The zero-based path-command index.</returns>
    int GetCommandIndex();

    /// <summary>Copies the preserved mask bytes.</summary>
    /// <returns>A new array containing the mask bits.</returns>
    byte[] CopyBytes();
}
