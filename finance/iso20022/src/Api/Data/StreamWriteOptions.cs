namespace iso20022;

/// <summary>Controls writing a sequence of messages into a standard or custom envelope.</summary>
public class StreamWriteOptions
{
    /// <summary>The options applied to each written message.</summary>
    public MessageWriteOptions MessageOptions = new MessageWriteOptions();
    /// <summary>The envelope layout to write.</summary>
    public EnvelopeKind EnvelopeKind = EnvelopeKind.BusinessFileEnvelope;
    /// <summary>The qualified caller-configured wrapper name when applicable.</summary>
    public QualifiedNameData? CustomWrapperName;
    /// <summary>Whether the caller supplied a precomputed message count.</summary>
    public bool HasPrecomputedMessageCount;
    /// <summary>The precomputed message count.</summary>
    public int PrecomputedMessageCount;
    /// <summary>Whether the caller supplied a precomputed control total.</summary>
    public bool HasPrecomputedControlTotal;
    /// <summary>The precomputed control total.</summary>
    public IsoDecimalValue PrecomputedControlTotal = new IsoDecimalValue();
    /// <summary>Whether count and total fields may be finalized after item writing.</summary>
    public bool UseDeferredTotals;
}
