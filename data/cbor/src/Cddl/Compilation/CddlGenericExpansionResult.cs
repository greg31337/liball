using System.Collections.Generic;

namespace cbor;

internal class CddlGenericExpansionResult
{
    internal List<CddlGenericInstantiation> Instantiations =
        new List<CddlGenericInstantiation>();
    internal List<CddlDiagnostic> Diagnostics = new List<CddlDiagnostic>();
    internal bool HasErrors;
}
