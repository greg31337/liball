namespace json;

/// <summary>Traverses JSON model graphs without recursive calls.</summary>
public interface IJsonTraversal
{
    /// <summary>Visits values in depth-first order.</summary>
    /// <param name="root">The root value.</param>
    /// <param name="visitor">The traversal callback.</param>
    /// <returns>The terminal visitor action.</returns>
    JsonVisitAction TraverseValues(JsonValue root, IJsonValueVisitor visitor);

    /// <summary>Publishes a model graph as depth-first JSON tokens.</summary>
    /// <param name="root">The root value.</param>
    /// <param name="handler">The token callback.</param>
    /// <returns>The publication outcome.</returns>
    JsonOperationResult PublishValueTokens(JsonValue root, IJsonTokenHandler handler);
}
