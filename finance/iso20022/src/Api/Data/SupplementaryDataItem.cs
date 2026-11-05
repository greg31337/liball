using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes one discovered supplementary envelope, payload, and resolved location.</summary>
public class SupplementaryDataItem
{
    /// <summary>The source <c>PlaceAndName</c> XPath expression.</summary>
    public string PlaceAndName = string.Empty;
    /// <summary>The resolved target path in the containing message.</summary>
    public XmlPathData LocationPath = new XmlPathData();
    /// <summary>All resolved target paths in document order.</summary>
    public List<XmlPathData> LocationPaths = new List<XmlPathData>();
    /// <summary>The source path of the supplementary container.</summary>
    public XmlPathData ContainerPath = new XmlPathData();
    /// <summary>The qualified supplementary envelope name.</summary>
    public QualifiedNameData EnvelopeName = new QualifiedNameData();
    /// <summary>The qualified payload root name when known.</summary>
    public QualifiedNameData PayloadName = new QualifiedNameData();
    /// <summary>The exact supplementary payload namespace URI.</summary>
    public string NamespaceUri = string.Empty;
    /// <summary>The preserved namespace-complete supplementary envelope.</summary>
    public RawXmlFragment? Envelope;
    /// <summary>The preserved namespace-complete extension payload.</summary>
    public RawXmlFragment? Payload;
    /// <summary>The selected supplementary schema when one was resolved.</summary>
    public SchemaArtifact? Schema;
    /// <summary>The discovery and validation outcome for this item.</summary>
    public SupplementaryValidationStatus Status;
    /// <summary>The zero-based occurrence among items at the same resolved location.</summary>
    public int OccurrenceIndex;
    /// <summary>Whether the item used a legacy <c>Extension1</c> or <c>Extension2</c> form.</summary>
    public bool IsLegacyForm;
    /// <summary>Whether the supplementary container is local to a nested message component.</summary>
    public bool IsLocal;
    /// <summary>Whether caller policy permits an unprocessed payload to be ignored.</summary>
    public bool CanIgnore;
    /// <summary>Whether caller policy requires the payload to be understood.</summary>
    public bool MustUnderstand;
}
