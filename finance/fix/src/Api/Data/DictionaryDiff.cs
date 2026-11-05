using System.Collections.Generic;

namespace fix;

/// <summary>Contains an ordered deterministic comparison of two dictionaries.</summary>
public class DictionaryDiff
{
    /// <summary>The identity of the earlier dictionary.</summary>
    public FixDictionaryIdentity BeforeIdentity = new FixDictionaryIdentity();
    /// <summary>The identity of the later dictionary.</summary>
    public FixDictionaryIdentity AfterIdentity = new FixDictionaryIdentity();
    /// <summary>The ordered changes.</summary>
    public List<DictionaryChange> Changes = [];
    /// <summary>Diagnostics produced while comparing definitions.</summary>
    public List<DictionaryDiagnostic> Diagnostics = [];
}
