using System.Collections.Generic;

namespace fix;

internal class CSharpEmissionResult
{
    internal List<GeneratedFile> Files = [];
    internal List<GenerationDiagnostic> Diagnostics = [];
}
