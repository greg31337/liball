namespace icc;

/// <summary>Represents a generic or registered ICC.2 tag structure.</summary>
public class IccTagStructure
{
    /// <summary>Gets or sets the structure signature.</summary>
    public uint StructureSignature;
    /// <summary>Gets or sets ordered structure members.</summary>
    public IccStructureMember[] Members;

    /// <summary>Initializes an empty generic structure.</summary>
    public IccTagStructure()
    {
        StructureSignature = 0;
        Members = new IccStructureMember[0];
    }
}
