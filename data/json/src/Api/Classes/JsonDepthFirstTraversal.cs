namespace json;

/// <summary>Traverses JSON model graphs iteratively in depth-first order.</summary>
public class JsonDepthFirstTraversal : IJsonTraversal
{
    /// <summary>Initializes a reusable depth-first traversal service.</summary>
    public JsonDepthFirstTraversal()
    {
    }

    /// <summary>Visits values in depth-first order.</summary>
    /// <param name="root">The root value.</param>
    /// <param name="visitor">The traversal callback.</param>
    /// <returns>The terminal visitor action.</returns>
    public JsonVisitAction TraverseValues(JsonValue root, IJsonValueVisitor visitor)
    {
        return JsonGraphWalker.TraverseGraph(root, visitor);
    }

    /// <summary>Publishes a model graph as depth-first JSON tokens.</summary>
    /// <param name="root">The root value.</param>
    /// <param name="handler">The token callback.</param>
    /// <returns>The publication outcome.</returns>
    public JsonOperationResult PublishValueTokens(
        JsonValue root,
        IJsonTokenHandler handler)
    {
        return JsonGraphWalker.PublishGraphTokens(root, handler);
    }
}
