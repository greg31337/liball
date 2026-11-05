namespace icc;

/// <summary>Represents one named member of an ICC.2 tag structure.</summary>
public class IccStructureMember
{
    /// <summary>Gets or sets the member signature.</summary>
    public uint Signature;
    /// <summary>Gets or sets the nested member data.</summary>
    public IccTagData Data;

    /// <summary>Initializes an unassigned member.</summary>
    public IccStructureMember()
    {
        Signature = 0;
        Data = new IccTagData();
    }
}
