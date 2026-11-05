namespace iso20022;

/// <summary>Accepts foundation data values for caller-defined processing.</summary>
public interface IFoundationDataConsumer
{
    /// <summary>Consumes artifact manifest data.</summary>
    /// <param name="value">The artifact manifest.</param>
    void ConsumeArtifactManifest(ArtifactManifest value);

    /// <summary>Consumes an amount value.</summary>
    /// <param name="value">The amount value.</param>
    void ConsumeAmountValue(IsoAmountValue value);

    /// <summary>Consumes a boolean value.</summary>
    /// <param name="value">The boolean value.</param>
    void ConsumeBooleanValue(IsoBooleanValue value);

    /// <summary>Consumes a code value.</summary>
    /// <param name="value">The code value.</param>
    void ConsumeCodeValue(IsoCodeValue value);

    /// <summary>Consumes a duration value.</summary>
    /// <param name="value">The duration value.</param>
    void ConsumeDurationValue(IsoDurationValue value);

    /// <summary>Consumes a text value.</summary>
    /// <param name="value">The text value.</param>
    void ConsumeTextValue(IsoTextValue value);

    /// <summary>Consumes a namespace declaration.</summary>
    /// <param name="value">The namespace declaration.</param>
    void ConsumeNamespaceDeclaration(NamespaceDeclarationData value);

    /// <summary>Consumes a namespace descriptor.</summary>
    /// <param name="value">The namespace descriptor.</param>
    void ConsumeNamespaceDescriptor(NamespaceDescriptor value);

    /// <summary>Consumes resource limits.</summary>
    /// <param name="value">The resource limits.</param>
    void ConsumeResourceLimits(ResourceLimits value);

    /// <summary>Consumes a source location.</summary>
    /// <param name="value">The source location.</param>
    void ConsumeSourceLocation(SourceLocation value);

    /// <summary>Consumes a version selector.</summary>
    /// <param name="value">The version selector.</param>
    void ConsumeVersionSelector(VersionSelector value);

    /// <summary>Consumes an XML path.</summary>
    /// <param name="value">The XML path.</param>
    void ConsumeXmlPath(XmlPathData value);

    /// <summary>Consumes a generic message document.</summary>
    /// <param name="value">The message document.</param>
    void ConsumeMessageDocument(MessageDocument value);

    /// <summary>Consumes message read options.</summary>
    /// <param name="value">The message read options.</param>
    void ConsumeMessageReadOptions(MessageReadOptions value);

    /// <summary>Consumes message write options.</summary>
    /// <param name="value">The message write options.</param>
    void ConsumeMessageWriteOptions(MessageWriteOptions value);

    /// <summary>Consumes a message write result.</summary>
    /// <param name="value">The message write result.</param>
    void ConsumeMessageWriteResult(MessageWriteResult value);

    /// <summary>Consumes a streamed message item.</summary>
    /// <param name="value">The streamed message item.</param>
    void ConsumeMessageStreamItem(MessageStreamItem value);

    /// <summary>Consumes stream read options.</summary>
    /// <param name="value">The stream read options.</param>
    void ConsumeStreamReadOptions(StreamReadOptions value);

    /// <summary>Consumes stream write options.</summary>
    /// <param name="value">The stream write options.</param>
    void ConsumeStreamWriteOptions(StreamWriteOptions value);

    /// <summary>Consumes metadata extraction options.</summary>
    /// <param name="value">The metadata extraction options.</param>
    void ConsumeMetadataExtractionOptions(MetadataExtractionOptions value);

    /// <summary>Consumes a normalized repository snapshot.</summary>
    /// <param name="value">The repository snapshot.</param>
    void ConsumeRepositorySnapshot(RepositorySnapshot value);

    /// <summary>Consumes a validation request.</summary>
    /// <param name="value">The validation request.</param>
    void ConsumeValidationRequest(ValidationRequest value);

    /// <summary>Consumes a validation report.</summary>
    /// <param name="value">The validation report.</param>
    void ConsumeValidationReport(ValidationReport value);
}
