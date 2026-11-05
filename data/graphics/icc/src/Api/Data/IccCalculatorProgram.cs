namespace icc;

/// <summary>Represents ICC.2 calculator bytecode and declared execution requirements.</summary>
public class IccCalculatorProgram
{
    /// <summary>Gets or sets the encoded calculator bytecode.</summary>
    public byte[] Bytecode;
    /// <summary>Gets or sets the decoded calculator instructions in encoded order.</summary>
    public IccCalculatorInstruction[] Instructions;
    /// <summary>Gets or sets the decoded instruction count.</summary>
    public int InstructionCount;
    /// <summary>Gets or sets the required operand-stack depth.</summary>
    public int RequiredStackDepth;
    /// <summary>Gets or sets the required storage value count.</summary>
    public int RequiredStorageValues;
    /// <summary>Gets or sets referenced sub-elements.</summary>
    public IccProcessingElement[] SubElements;
    /// <summary>Gets or sets the number of retained unknown or future operations.</summary>
    public int UnknownOperationCount;
    /// <summary>Gets or sets whether every operation and referenced sub-element can be executed.</summary>
    public bool IsExecutable;

    /// <summary>Initializes an empty calculator program.</summary>
    public IccCalculatorProgram()
    {
        Bytecode = new byte[0];
        Instructions = new IccCalculatorInstruction[0];
        InstructionCount = 0;
        RequiredStackDepth = 0;
        RequiredStorageValues = 0;
        SubElements = new IccProcessingElement[0];
        UnknownOperationCount = 0;
        IsExecutable = false;
    }
}
