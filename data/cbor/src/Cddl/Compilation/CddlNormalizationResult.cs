using System.Collections.Generic;

namespace cbor;

internal class CddlNormalizationResult
{
    internal CddlNormalizedRule? RootRule;
    internal List<CddlNormalizedRule> Rules = new List<CddlNormalizedRule>();
    internal List<CddlNormalizedNode> Nodes = new List<CddlNormalizedNode>();
    internal List<CddlSourceSpan> SourceMap = new List<CddlSourceSpan>();
    internal List<CddlDiagnostic> Diagnostics = new List<CddlDiagnostic>();
    internal bool HasErrors;
}
