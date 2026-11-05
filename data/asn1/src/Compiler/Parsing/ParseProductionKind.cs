namespace asn1;

internal enum ParseProductionKind
{
    CompilationUnit = 0,
    Module = 1,
    ModuleHeader = 2,
    DefinitiveIdentification = 3,
    ModuleBody = 4,
    ParenthesizedGroup = 5,
    BracedGroup = 6,
    BracketedGroup = 7,
    RetiredMacro = 8
}
