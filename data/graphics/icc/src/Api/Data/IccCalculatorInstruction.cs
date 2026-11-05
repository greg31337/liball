namespace icc;

/// <summary>Represents one encoded ICC.2 calculator operation and its decoded operand.</summary>
public class IccCalculatorInstruction
{
    /// <summary>Gets or sets the four-byte operation signature.</summary>
    public uint Signature;
    /// <summary>Gets or sets the exact four operand bytes as an unsigned value.</summary>
    public uint Operand;
    /// <summary>Gets or sets the operand interpreted as a float32Number for a data operation.</summary>
    public double FloatingPointValue;
    /// <summary>Gets or sets the first unsigned 16-bit selector.</summary>
    public int FirstSelector;
    /// <summary>Gets or sets the second unsigned 16-bit selector.</summary>
    public int SecondSelector;
    /// <summary>Gets or sets whether the operation signature is defined by ICC.2.</summary>
    public bool IsKnown;
    /// <summary>Gets or sets whether the operand identifies a CMM environment variable.</summary>
    public bool IsEnvironmentReference;
    /// <summary>Gets or sets whether the operand indexes calculator temporary storage.</summary>
    public bool IsTemporaryStorageReference;
    /// <summary>Gets or sets whether the operand indexes a calculator sub-element.</summary>
    public bool IsSubElementReference;

    /// <summary>Initializes an empty calculator instruction.</summary>
    public IccCalculatorInstruction()
    {
        Signature = 0;
        Operand = 0;
        FloatingPointValue = 0.0;
        FirstSelector = 0;
        SecondSelector = 0;
        IsKnown = false;
        IsEnvironmentReference = false;
        IsTemporaryStorageReference = false;
        IsSubElementReference = false;
    }
}
