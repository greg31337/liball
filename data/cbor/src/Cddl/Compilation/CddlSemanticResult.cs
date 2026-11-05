using System.Collections.Generic;

namespace cbor;

internal class CddlSemanticResult
{
    internal List<CddlDiagnostic> Diagnostics = new List<CddlDiagnostic>();
    internal bool HasErrors;
}
