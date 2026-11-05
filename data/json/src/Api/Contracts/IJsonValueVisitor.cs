namespace json;

/// <summary>Receives values during depth-first JSON traversal.</summary>
public interface IJsonValueVisitor
{
    /// <summary>Visits one JSON value.</summary>
    /// <param name="visit">The current traversal context.</param>
    /// <returns>The action the traversal should take.</returns>
    JsonVisitAction VisitValue(JsonVisit visit);
}
