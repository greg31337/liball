namespace ttf;

/// <summary>Contains one preserved CFF hint-mask or counter-mask operation.</summary>
/// <remarks>
/// Mask bits and their path-command position are retained for inspection only.
/// They are not interpreted during outline normalization or rasterization.
/// </remarks>
public class CffHintMask : ICffHintMask
{
    bool counterMask;
    byte[] bytes;
    int commandIndex;

    internal CffHintMask(Type2HintMask mask)
    {
        counterMask = mask.IsCounterMask();
        bytes = mask.CopyBytes();
        commandIndex = mask.GetCommandIndex();
    }

    /// <summary>Reports whether the operation is a counter mask.</summary>
    /// <returns>True for <c>cntrmask</c>; false for <c>hintmask</c>.</returns>
    public bool IsCounterMask() { return counterMask; }

    /// <summary>Gets the preserved mask byte count.</summary>
    /// <returns>The mask byte count.</returns>
    public int GetLength() { return bytes.Length; }

    /// <summary>Gets the normalized path-command index at which the mask occurs.</summary>
    /// <returns>The zero-based path-command index.</returns>
    public int GetCommandIndex() { return commandIndex; }

    /// <summary>Copies the preserved mask bytes.</summary>
    /// <returns>A new array containing the mask bits.</returns>
    public byte[] CopyBytes() { return bytes.AsSpan().ToArray(); }
}
