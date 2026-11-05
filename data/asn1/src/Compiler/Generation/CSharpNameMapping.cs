namespace asn1;

internal class CSharpNameMapping
{
    internal string SourceName = "";
    internal string NormalizedSourceName = "";
    internal string StableId = "";
    internal string GeneratedName = "";
    internal int CollisionIndex;
    internal bool Changed;
}

internal class CSharpNameReservation
{
    internal string Name = "";
    internal int CollisionIndex;
}
