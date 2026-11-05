namespace iso20022;

internal class BuilderTarget
{
    internal MessageIdentifier? Identifier;
    internal string IdentifierText = string.Empty;
    internal string NamespaceUri = string.Empty;
    internal string MessageRootName = string.Empty;
    internal string ErrorCode = string.Empty;
    internal string ErrorMessage = string.Empty;
}
