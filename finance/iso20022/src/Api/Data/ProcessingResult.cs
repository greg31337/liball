using System;
using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains every reached processing-stage result with exact source and target identity.</summary>
public class ProcessingResult
{
    /// <summary>Whether every selected stage completed successfully.</summary>
    public bool Success;
    /// <summary>The processing stages requested by the caller in deterministic order.</summary>
    public List<ProcessingStage> RequestedStages = new List<ProcessingStage>();
    /// <summary>The processing stages completed successfully in deterministic order.</summary>
    public List<ProcessingStage> CompletedStages = new List<ProcessingStage>();
    /// <summary>The stage at which processing stopped, or <c>Unknown</c> after complete success.</summary>
    public ProcessingStage FailedStage;
    /// <summary>The exact resolved source message identifier.</summary>
    public MessageIdentifier? SourceMessageIdentifier;
    /// <summary>The exact resolved source namespace URI.</summary>
    public string SourceNamespaceUri = string.Empty;
    /// <summary>The exact produced target message identifier when migration or mapping was selected.</summary>
    public MessageIdentifier? TargetMessageIdentifier;
    /// <summary>The exact produced target namespace URI.</summary>
    public string TargetNamespaceUri = string.Empty;
    /// <summary>The complete detection-stage result when that stage was reached.</summary>
    public MessageDetectionResult? Detection;
    /// <summary>The complete reading-stage result when that stage was reached.</summary>
    public MessageReadResult? Read;
    /// <summary>The complete direct validation-stage report when that stage was reached.</summary>
    public ValidationReport? Validation;
    /// <summary>The extracted metadata when metadata extraction was reached.</summary>
    public MessageMetadata? Metadata;
    /// <summary>The complete migration-stage result when that stage was reached.</summary>
    public MigrationResult? Migration;
    /// <summary>The complete mapping-stage result when that stage was reached.</summary>
    public MappingResult? Mapping;
    /// <summary>The complete writing-stage result when that stage was reached.</summary>
    public MessageWriteResult? Write;
    /// <summary>Every direct or target validation report reached across selected stages.</summary>
    public List<ValidationReport> ValidationReports = new List<ValidationReport>();
    /// <summary>The final generic message document when materialized.</summary>
    public MessageDocument? Document;
    /// <summary>The final generated message or canonical model when materialized.</summary>
    public object? Model;
    /// <summary>The final XML bytes when reading, migration, mapping, or writing produced them.</summary>
    public byte[] Xml = Array.Empty<byte>();
    /// <summary>The ordered diagnostics aggregated without discarding their stage reports.</summary>
    public List<ValidationDiagnostic> Diagnostics = new List<ValidationDiagnostic>();
    /// <summary>A stable operation error code when processing could not complete.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable operation error message when processing could not complete.</summary>
    public string ErrorMessage = string.Empty;
}
