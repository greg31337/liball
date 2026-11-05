namespace asn1;

internal static class IrBinaryFormat
{
    internal const int Version = 4;
    internal static readonly byte[] Magic =
        [65, 83, 78, 49, 73, 82, 0, 0];
}

internal class IrNodeMetadata
{
    internal string StableId = "";
    internal AsnSourceSpan? SourceSpan;
    internal LoweredDerivationIr[] Derivations = [];
}
