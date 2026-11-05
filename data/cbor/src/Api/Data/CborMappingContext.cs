using System.Collections.Generic;

namespace cbor;

/// <summary>
/// Holds the current state passed to custom runtime type adapters.
/// </summary>
public class CborMappingContext
{
    /// <summary>The mapping policies and limits.</summary>
    public CborMappingOptions Options = new CborMappingOptions();
    /// <summary>The current nested mapping depth.</summary>
    public int Depth;
    /// <summary>The total number of values processed so far.</summary>
    public int ItemCount;
    /// <summary>The native objects tracked for reference preservation.</summary>
    public List<object> NativeReferences = new List<object>();
    /// <summary>The CBOR values corresponding to <see cref="NativeReferences"/>.</summary>
    public List<CborValue> ValueReferences = new List<CborValue>();
}
