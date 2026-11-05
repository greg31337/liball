namespace iso20022;

/// <summary>Controls whether namespace processing accepts configured repairs.</summary>
public enum NamespaceMode
{
    /// <summary>Require the namespace to conform exactly.</summary>
    Strict = 0,
    /// <summary>Allow configured namespace repairs and record them.</summary>
    Lenient = 1
}
