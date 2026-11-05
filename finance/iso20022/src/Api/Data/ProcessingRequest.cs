using System;
using System.Collections.Generic;
using System.Threading;

namespace iso20022;

/// <summary>Requests explicit composition of selected high-level message processing stages.</summary>
public class ProcessingRequest
{
    /// <summary>The raw source XML bytes, or an empty array for a materialized input.</summary>
    public byte[] Xml = Array.Empty<byte>();
    /// <summary>The generic source message when supplied.</summary>
    public MessageDocument? Document;
    /// <summary>The generated source message or caller canonical model when supplied.</summary>
    public object? Model;
    /// <summary>The deterministic processing stages selected by the caller.</summary>
    public List<ProcessingStage> Stages = new List<ProcessingStage>();
    /// <summary>The expected exact source message identifier when supplied.</summary>
    public MessageIdentifier? ExpectedSourceMessageIdentifier;
    /// <summary>The expected exact source namespace URI when supplied.</summary>
    public string ExpectedSourceNamespaceUri = string.Empty;
    /// <summary>The exact requested target message identifier for migration, mapping, or writing.</summary>
    public MessageIdentifier? TargetMessageIdentifier;
    /// <summary>The exact requested target namespace URI.</summary>
    public string TargetNamespaceUri = string.Empty;
    /// <summary>The message reading and detection policy.</summary>
    public MessageReadOptions ReadOptions = new MessageReadOptions();
    /// <summary>The complete validation request used by the validation stage.</summary>
    public ValidationRequest? Validation;
    /// <summary>The metadata selection and redaction policy used by metadata extraction.</summary>
    public MetadataExtractionOptions MetadataOptions = new MetadataExtractionOptions();
    /// <summary>The complete migration request used by the migration stage.</summary>
    public MigrationRequest? Migration;
    /// <summary>The complete mapping request used by the mapping stage.</summary>
    public MappingRequest? Mapping;
    /// <summary>The message writing policy used by the writing stage.</summary>
    public MessageWriteOptions WriteOptions = new MessageWriteOptions();
    /// <summary>The resource bounds applied across all selected stages.</summary>
    public ResourceLimits Limits = new ResourceLimits();
    /// <summary>The cancellation request observed between bounded processing stages.</summary>
    public CancellationToken CancellationToken;
}
