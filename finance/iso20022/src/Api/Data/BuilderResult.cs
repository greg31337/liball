using System;
using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains one constructed message together with its exact target identity and validation.</summary>
public class BuilderResult
{
    /// <summary>Whether construction and requested validation completed successfully.</summary>
    public bool Success;
    /// <summary>The semantic-template identifier that was applied.</summary>
    public string TemplateIdentifier = string.Empty;
    /// <summary>The exact constructed target message identifier.</summary>
    public MessageIdentifier? TargetMessageIdentifier;
    /// <summary>The exact constructed target message namespace URI.</summary>
    public string TargetNamespaceUri = string.Empty;
    /// <summary>The constructed generic message document.</summary>
    public MessageDocument? Document;
    /// <summary>The constructed generated message model when available.</summary>
    public object? Model;
    /// <summary>The constructed XML bytes when writing was requested.</summary>
    public byte[] Xml = Array.Empty<byte>();
    /// <summary>The complete target validation report when validation was requested and reached.</summary>
    public ValidationReport? Validation;
    /// <summary>The ordered construction diagnostics.</summary>
    public List<ValidationDiagnostic> Diagnostics = new List<ValidationDiagnostic>();
    /// <summary>A stable operation error code when construction could not complete.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable operation error message when construction could not complete.</summary>
    public string ErrorMessage = string.Empty;
}
