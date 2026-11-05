using System.Collections.Generic;

namespace fix;

/// <summary>Contains the explicit outcome of loading, writing, or composing a dictionary.</summary>
public class DictionaryLoadResult
{
    /// <summary>Whether the requested operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>The resulting dictionary when one is available.</summary>
    public FixDictionary? Dictionary;
    /// <summary>The ordered diagnostics.</summary>
    public List<DictionaryDiagnostic> Diagnostics = [];
}
