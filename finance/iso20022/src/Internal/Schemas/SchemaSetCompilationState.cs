using System.Collections.Generic;

namespace iso20022;

internal class SchemaSetCompilationState
{
    internal SchemaCompilationResult Result = new SchemaCompilationResult();
    internal SchemaRegistryOptions Options = new SchemaRegistryOptions();
    internal List<SchemaCompilationUnit> Units = new List<SchemaCompilationUnit>();
    internal bool Success = true;
    internal bool DiagnosticLimitExceeded;
}
