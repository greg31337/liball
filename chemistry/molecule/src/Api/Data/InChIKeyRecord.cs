namespace molecule;


/// <summary>
/// Stores the parsed fields for one inchikey record.
/// </summary>
public class InChIKeyRecord
{
    /// <summary>
    /// The original text.
    /// </summary>
    public string OriginalText;
    /// <summary>
    /// The normalized text.
    /// </summary>
    public string NormalizedText;
    /// <summary>
    /// The first block.
    /// </summary>
    public string FirstBlock;
    /// <summary>
    /// The second block.
    /// </summary>
    public string SecondBlock;
    /// <summary>
    /// The remaining hash block.
    /// </summary>
    public string RemainingHashBlock;
    /// <summary>
    /// The version block.
    /// </summary>
    public string VersionBlock;
    /// <summary>
    /// The version character.
    /// </summary>
    public string VersionCharacter;
    /// <summary>
    /// The standard character.
    /// </summary>
    public string StandardCharacter;
    /// <summary>
    /// The protonation indicator.
    /// </summary>
    public string ProtonationIndicator;
    /// <summary>
    /// Diagnostics produced while processing the value.
    /// </summary>
    public MoleculeReport Report;
    /// <summary>
    /// Whether the operation completed without an error diagnostic.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Initializes a new instance of the <see cref="InChIKeyRecord"/> class.
    /// </summary>
    public InChIKeyRecord()
    {
        OriginalText = "";
        NormalizedText = "";
        FirstBlock = "";
        SecondBlock = "";
        RemainingHashBlock = "";
        VersionBlock = "";
        VersionCharacter = "";
        StandardCharacter = "";
        ProtonationIndicator = "";
        Report = new MoleculeReport();
        Success = true;
    }

    /// <summary>
    /// Determines whether the value has first block.
    /// </summary>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasFirstBlock()
    {
        return FirstBlock.Length > 0;
    }

    /// <summary>
    /// Determines whether the value has second block.
    /// </summary>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasSecondBlock()
    {
        return SecondBlock.Length > 0;
    }

    /// <summary>
    /// Determines whether the value has version.
    /// </summary>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasVersion()
    {
        return VersionBlock.Length > 0;
    }

    /// <summary>
    /// Determines whether the value has protonation indicator.
    /// </summary>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasProtonationIndicator()
    {
        return ProtonationIndicator.Length > 0;
    }

    /// <summary>
    /// Determines whether the value has same first block.
    /// </summary>
    /// <param name="other">The other.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasSameFirstBlock(InChIKeyRecord? other)
    {
        if (other == null)
        {
            return false;
        }

        return FirstBlock == other.FirstBlock;
    }

    /// <summary>
    /// Determines whether the value has same second block.
    /// </summary>
    /// <param name="other">The other.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasSameSecondBlock(InChIKeyRecord? other)
    {
        if (other == null)
        {
            return false;
        }

        return SecondBlock == other.SecondBlock;
    }

    /// <summary>
    /// Returns the block.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    internal string GetBlock(string name)
    {
        string normalized = NormalizeString(name);
        if (normalized == "first")
        {
            return FirstBlock;
        }

        if (normalized == "connectivity")
        {
            return FirstBlock;
        }

        if (normalized == "second")
        {
            return SecondBlock;
        }

        if (normalized == "remaining")
        {
            return RemainingHashBlock;
        }

        if (normalized == "version")
        {
            return VersionBlock;
        }

        if (normalized == "protonation")
        {
            return ProtonationIndicator;
        }

        return "";
    }

    internal string NormalizeString(string? value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }
}

