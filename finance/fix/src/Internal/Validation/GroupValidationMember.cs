namespace fix;

internal class GroupValidationMember
{
    internal int Tag;
    internal int Position = -1;
    internal int WireIndex = -1;
    internal int Sequence;
    internal FixField? Field;
    internal FixGroup? Group;
}
