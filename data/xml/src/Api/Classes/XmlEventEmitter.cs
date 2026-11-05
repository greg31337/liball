namespace xml;

/// <summary>Provides normalized XML event emission from trees and selections.</summary>
public class XmlEventEmitter : IXmlEventEmitter
{
    internal TreeEmission Emission;

    /// <summary>Creates an XML event emitter.</summary>
    public XmlEventEmitter()
    {
        Emission = new TreeEmission();
    }

    /// <summary>Emits events for a node.</summary>
    public XmlEventBatch EmitXmlNodeEvents(XmlNode node, XmlParserOptions options)
    {
        return Emission.EmitXmlNodeBatch(node, options);
    }

    /// <summary>Emits events for a selection.</summary>
    public XmlEventBatch EmitXmlSelectionEvents(XmlNodeSelection selection, XmlParserOptions options)
    {
        return Emission.EmitXmlSelectionBatch(selection, options);
    }
}
