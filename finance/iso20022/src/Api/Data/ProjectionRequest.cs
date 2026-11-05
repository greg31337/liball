using System;
using System.Threading;

namespace iso20022;

/// <summary>Requests projection of one exact source message into a stable task-oriented view.</summary>
public class ProjectionRequest
{
    /// <summary>The raw source XML bytes, or an empty array for a materialized input.</summary>
    public byte[] Xml = Array.Empty<byte>();
    /// <summary>The generic source message when supplied.</summary>
    public MessageDocument? Document;
    /// <summary>The generated source message when supplied.</summary>
    public object? Model;
    /// <summary>The expected exact source message identifier when supplied.</summary>
    public MessageIdentifier? ExpectedSourceMessageIdentifier;
    /// <summary>The expected exact source namespace URI when supplied.</summary>
    public string ExpectedSourceNamespaceUri = string.Empty;
    /// <summary>The stable task-oriented projection identifier selected by the caller.</summary>
    public string ProjectionIdentifier = string.Empty;
    /// <summary>The inspectable projection mapping when supplied explicitly.</summary>
    public MappingDefinition? ProjectionDefinition;
    /// <summary>The repository snapshot that supplies source definition metadata when required.</summary>
    public RepositorySnapshot? Repository;
    /// <summary>Whether the source must be validated before projection.</summary>
    public bool ValidateSource;
    /// <summary>The validator used for optional source validation.</summary>
    public IMessageValidator? Validator;
    /// <summary>The exact source validation stage and policy selection.</summary>
    public ValidationPolicy ValidationPolicy = new ValidationPolicy();
    /// <summary>The caller context supplied to source validation.</summary>
    public ValidationContext ValidationContext = new ValidationContext();
    /// <summary>The metadata selection and redaction policy applied during projection.</summary>
    public MetadataExtractionOptions MetadataOptions = new MetadataExtractionOptions();
    /// <summary>The resource bounds applied during reading, validation, and projection.</summary>
    public ResourceLimits Limits = new ResourceLimits();
    /// <summary>The cancellation request observed between bounded projection stages.</summary>
    public CancellationToken CancellationToken;
}
