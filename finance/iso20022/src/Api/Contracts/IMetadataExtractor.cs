namespace iso20022;

/// <summary>Extracts stable business metadata from generic ISO 20022 message content.</summary>
public interface IMetadataExtractor
{
    /// <summary>Extracts bounded header, payload, identity, amount, date, and reference metadata.</summary>
    /// <param name="document">The generic message document to inspect.</param>
    /// <param name="options">The header, payload, redaction, path, and resource policies.</param>
    /// <returns>A new metadata value containing the selected fields.</returns>
    MessageMetadata ExtractMetadata(
        MessageDocument? document,
        MetadataExtractionOptions? options);
}
