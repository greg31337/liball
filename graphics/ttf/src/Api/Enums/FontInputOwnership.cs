namespace ttf;

/// <summary>Controls whether a memory input is copied or retained.</summary>
public enum FontInputOwnership
{
    /// <summary>Copy input bytes into immutable library-owned storage.</summary>
    Copy = 0,

    /// <summary>
    /// Retain caller-provided read-only memory whose lifetime and immutability the
    /// caller guarantees.
    /// </summary>
    RetainReadOnlyMemory = 1
}
