namespace json;

/// <summary>Compares two JSON values using an explicit equality mode.</summary>
public interface IJsonValueComparer
{
    /// <summary>Compares two JSON values.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <param name="mode">The equality rules to apply.</param>
    /// <returns><c>true</c> when the values are equal under the selected mode; otherwise, <c>false</c>.</returns>
    bool CompareValues(JsonValue left, JsonValue right, JsonComparisonMode mode);
}
