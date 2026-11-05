using System.Collections.Generic;

namespace cbor;

internal class CddlResolutionResult
{
    internal CddlRule? RootRule;
    internal List<CddlResolvedReference> References =
        new List<CddlResolvedReference>();
    internal List<CddlRule> ExternalRules = new List<CddlRule>();
    internal List<CddlDiagnostic> Diagnostics = new List<CddlDiagnostic>();
    internal bool HasErrors;
}
