namespace xml;

/// <summary>
/// Emits normalized event batches from trees and node selections.
/// </summary>
public interface IXmlEventEmitter
{
    /// <summary>
    /// Emits events for a node and its descendants.
    /// </summary>
    /// <param name="node">The subtree root.</param>
    /// <param name="options">The preservation and namespace options.</param>
    /// <returns>The normalized events.</returns>
    XmlEventBatch EmitXmlNodeEvents(XmlNode node, XmlParserOptions options);

    /// <summary>
    /// Emits events for an ordered node selection.
    /// </summary>
    /// <param name="selection">The selected nodes.</param>
    /// <param name="options">The preservation and namespace options.</param>
    /// <returns>The normalized events.</returns>
    XmlEventBatch EmitXmlSelectionEvents(XmlNodeSelection selection, XmlParserOptions options);
}

