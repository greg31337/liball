namespace asn1;

internal class ResolvedComponentValueSyntax
{
    internal string Name = "";
    internal ResolvedComponentSyntax? Component;
    internal ResolvedValueSyntax? Value;
    internal ParsedValueSyntax? SourceSyntax;
    internal int SourceOrder = -1;
    internal bool Present;
    internal bool Defaulted;
    internal bool PresentWithDefaultValue;
    internal bool Extension;
    internal bool UnknownExtension;
}
