namespace json;

/// <summary>Compares JSON value graphs using exact-model or JSON-semantic rules.</summary>
public class JsonValueComparer : IJsonValueComparer
{
    /// <summary>Initializes a reusable JSON value comparer.</summary>
    public JsonValueComparer()
    {
    }

    /// <summary>Compares two JSON values.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <param name="mode">The equality rules to apply.</param>
    /// <returns><c>true</c> when the values are equal under the selected mode; otherwise, <c>false</c>.</returns>
    public bool CompareValues(
        JsonValue left,
        JsonValue right,
        JsonComparisonMode mode)
    {
        return JsonDeepComparer.CompareValueGraphs(left, right, mode);
    }
}
