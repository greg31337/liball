namespace json;

/// <summary>Selects the equality rules used to compare JSON values.</summary>
public enum JsonComparisonMode
{
    /// <summary>Compares exact model representation, order, and number spelling.</summary>
    ExactModel = 0,

    /// <summary>Compares numbers mathematically, arrays in order, and exact-name object members independent of order while retaining duplicate occurrence order.</summary>
    JsonSemantic = 1
}
