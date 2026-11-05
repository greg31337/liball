using System.Collections.Generic;

namespace cbor;

/// <summary>Holds a parsed CDDL source document and its ordered syntax data.</summary>
public class CddlDocument
{
    /// <summary>The optional logical source name.</summary>
    public string? SourceName;
    /// <summary>The complete source text.</summary>
    public string SourceText = "";
    /// <summary>The rules in source order.</summary>
    public List<CddlRule> Rules = new List<CddlRule>();
    /// <summary>Comments and whitespace retained when requested.</summary>
    public List<string> Trivia = new List<string>();
    /// <summary>Source ranges corresponding to <see cref="Trivia"/>.</summary>
    public List<CddlSourceSpan> TriviaSpans = new List<CddlSourceSpan>();
    /// <summary>Recoverable diagnostics associated with the partial document.</summary>
    public List<CddlDiagnostic> Diagnostics = new List<CddlDiagnostic>();
    /// <summary>The first base rule name selected as the default root.</summary>
    public string? RootRuleName;
}
