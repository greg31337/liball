using System;
using System.Collections.Generic;

namespace iso20022;

/// <summary>Specifies one raw, generic, or typed message validation operation and its complete policy context.</summary>
public class ValidationRequest
{
    /// <summary>The raw XML bytes to validate, or an empty array when validating a materialized representation.</summary>
    public byte[] Xml = Array.Empty<byte>();
    /// <summary>The generic message document to validate when supplied.</summary>
    public MessageDocument? Document;
    /// <summary>The generated strongly typed message model to validate when supplied.</summary>
    public object? Model;
    /// <summary>The expected exact message identifier when the input must match a caller selection.</summary>
    public MessageIdentifier? ExpectedIdentifier;
    /// <summary>The expected exact namespace URI when the input must match a caller selection.</summary>
    public string ExpectedNamespaceUri = string.Empty;
    /// <summary>The exact local entry-point schema selected by the caller when supplied.</summary>
    public SchemaArtifact? Schema;
    /// <summary>The XSD validator used by the XSD pipeline stage when supplied.</summary>
    public IXsdValidator? XsdValidator;
    /// <summary>The local schema selection, closure, and compilation options used by the XSD stage.</summary>
    public SchemaRegistryOptions SchemaOptions = new SchemaRegistryOptions();
    /// <summary>The validation stage, limit, code-edition, unknown-content, timezone, and signature policy.</summary>
    public ValidationPolicy Policy = new ValidationPolicy();
    /// <summary>The resolved identity and caller-owned semantic-rule state.</summary>
    public ValidationContext Context = new ValidationContext();
    /// <summary>The market-practice or caller profile overlay when supplied directly.</summary>
    public ProfileDefinition? Profile;
    /// <summary>The internal and external code-set provider used by code validation stages when supplied.</summary>
    public ICodeSetProvider? CodeSetProvider;
    /// <summary>The caller-supplied executable semantic rules composed by stable descriptor identity.</summary>
    public List<ISemanticRule> SemanticRules = new List<ISemanticRule>();
    /// <summary>The provider used to resolve inherited validation profiles when supplied.</summary>
    public IProfileProvider? ProfileProvider;
}
