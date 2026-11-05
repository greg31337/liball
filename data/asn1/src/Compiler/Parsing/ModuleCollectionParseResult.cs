namespace asn1;

internal class ModuleCollectionParseResult
{
    internal ModuleParseStatus Status;
    internal ModuleSyntax[] Modules = [];
    internal int ConsumedTokenCount;
}
