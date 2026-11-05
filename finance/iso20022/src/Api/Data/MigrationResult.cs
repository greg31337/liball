using System;
using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains migrated output, applied steps, retained unmapped data, and target validation.</summary>
public class MigrationResult
{
    /// <summary>Whether migration and requested target validation completed successfully.</summary>
    public bool Success;
    /// <summary>The exact source message identifier.</summary>
    public MessageIdentifier? SourceMessageIdentifier;
    /// <summary>The exact target message identifier.</summary>
    public MessageIdentifier? TargetMessageIdentifier;
    /// <summary>The migration plan that was executed.</summary>
    public MigrationPlan? Plan;
    /// <summary>The migrated raw XML bytes when materialized.</summary>
    public byte[] Xml = Array.Empty<byte>();
    /// <summary>The migrated generic message when materialized.</summary>
    public MessageDocument? Document;
    /// <summary>The migrated generated strongly typed message when materialized.</summary>
    public object? Model;
    /// <summary>The steps applied successfully in deterministic order.</summary>
    public List<MigrationStep> AppliedSteps = new List<MigrationStep>();
    /// <summary>The steps that could not complete.</summary>
    public List<MigrationStep> FailedSteps = new List<MigrationStep>();
    /// <summary>The source paths that were not mapped into the target.</summary>
    public List<XmlPathData> UnmappedSourcePaths = new List<XmlPathData>();
    /// <summary>The preserved unmapped source fragments retained for reporting or fallback.</summary>
    public List<RawXmlFragment> UnmappedContent = new List<RawXmlFragment>();
    /// <summary>Whether authorized supplementary-data fallback was used.</summary>
    public bool SupplementaryFallbackUsed;
    /// <summary>Whether applied steps intentionally lost source information.</summary>
    public bool DataLoss;
    /// <summary>The target validation report when validation was requested and reached.</summary>
    public ValidationReport? TargetValidation;
    /// <summary>The ordered located migration diagnostics.</summary>
    public List<ValidationDiagnostic> Diagnostics = new List<ValidationDiagnostic>();
    /// <summary>A stable operation error code when migration could not complete.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable operation error message when migration could not complete.</summary>
    public string ErrorMessage = string.Empty;
}
