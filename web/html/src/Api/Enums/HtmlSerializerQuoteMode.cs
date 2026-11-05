using System;

namespace html;
/// <summary>Selects how serialization quotes attribute values.</summary>
public enum HtmlSerializerQuoteMode
{
    /// <summary>Always surrounds attribute values with double quotes.</summary>
    Double = 0,
    /// <summary>Always surrounds attribute values with single quotes.</summary>
    Single = 1,
    /// <summary>Chooses the valid quoting form that produces the shortest output.</summary>
    Shortest = 2
}
