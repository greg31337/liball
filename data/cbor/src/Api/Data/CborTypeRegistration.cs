using System;

namespace cbor;

/// <summary>
/// Associates a runtime type with a custom CBOR type adapter.
/// </summary>
public class CborTypeRegistration
{
    /// <summary>The registered runtime type.</summary>
    public Type? RuntimeType;
    /// <summary>The adapter used for the runtime type.</summary>
    public ICborTypeAdapter? Adapter;
}
