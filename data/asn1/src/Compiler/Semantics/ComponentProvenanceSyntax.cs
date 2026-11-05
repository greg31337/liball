namespace asn1;

internal class ComponentProvenanceSyntax
{
    internal SymbolTableEntry? Declaration;
    internal ParsedComponentSyntax? ComponentSyntax;
    internal bool ComponentsOf;
    internal bool Extension;
    internal bool AfterExtensionEnd;
    internal int ExtensionGroupIndex = -1;
    internal bool HasVersionNumber;
    internal System.Numerics.BigInteger VersionNumber;
}
