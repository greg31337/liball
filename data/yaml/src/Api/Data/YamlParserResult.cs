using System.Collections.Generic;

namespace yaml;

/// <summary>
/// Contains parser events and diagnostics produced from a token sequence.
/// </summary>
public class YamlParserResult
{
    internal YamlSecurityOptions? Security;
    internal List<YamlEventKind> SecurityCollectionKinds;
    internal List<int> SecurityCollectionNodeCounts;
    internal int SecurityAliasCount;
    internal bool SecurityLimitExceeded;
    /// <summary>
    /// Whether the operation completed without errors.
    /// </summary>
    public bool Success;
    /// <summary>
    /// The parsed events in stream order.
    /// </summary>
    public List<YamlEvent> Events;
    /// <summary>
    /// The diagnostics in the order they were reported.
    /// </summary>
    public List<YamlDiagnostic> Diagnostics;

    /// <summary>
    /// Initializes a new YAML parser result with empty or default state.
    /// </summary>
    public YamlParserResult()
    {
        Security = null;
        SecurityCollectionKinds = new List<YamlEventKind>();
        SecurityCollectionNodeCounts = new List<int>();
        SecurityAliasCount = 0;
        SecurityLimitExceeded = false;
        Success = true;
        Events = new List<YamlEvent>();
        Diagnostics = new List<YamlDiagnostic>();
    }

    internal void ConfigureSecurity(YamlSecurityOptions? options)
    {
        Security = options;
    }

    internal void AddEvent(YamlEvent yamlEvent)
    {
        if (yamlEvent == null)
        {
            return;
        }

        CheckSecurityLimit(yamlEvent);
        Events.Add(yamlEvent);
    }

    internal void InsertEvent(int index, YamlEvent yamlEvent)
    {
        if (yamlEvent == null)
        {
            return;
        }

        Events.Insert(index, yamlEvent);
        RecheckSecurityLimits();
    }

    internal void AddDiagnostic(YamlDiagnostic diagnostic)
    {
        if (diagnostic == null)
        {
            return;
        }

        Diagnostics.Add(diagnostic);
        Events.Add(YamlEvent.DiagnosticEvent(diagnostic));
        if (diagnostic.IsError())
        {
            Success = false;
        }
    }

    internal void AddError(string code, string message, YamlSourceSpan span)
    {
        AddDiagnostic(YamlDiagnostic.Error(code, message, span));
    }

    internal void CheckSecurityLimit(YamlEvent yamlEvent)
    {
        if (Security == null || !Security.EnforceLimits || SecurityLimitExceeded)
        {
            return;
        }

        bool nodeStart = yamlEvent.Kind == YamlEventKind.Scalar
            || yamlEvent.Kind == YamlEventKind.Alias
            || yamlEvent.Kind == YamlEventKind.SequenceStart
            || yamlEvent.Kind == YamlEventKind.MappingStart;
        if (nodeStart && !Security.AllowsDepth(SecurityCollectionKinds.Count + 1))
        {
            AddSecurityError("YAMLS003", "YAML node depth exceeds the configured security limit.", yamlEvent.Span);
            return;
        }

        if (nodeStart && SecurityCollectionKinds.Count > 0)
        {
            int parent = SecurityCollectionKinds.Count - 1;
            int nodeCount = SecurityCollectionNodeCounts[parent] + 1;
            SecurityCollectionNodeCounts[parent] = nodeCount;
            int collectionSize = nodeCount;
            if (SecurityCollectionKinds[parent] == YamlEventKind.MappingStart)
            {
                collectionSize = (nodeCount + 1) / 2;
            }

            if (!Security.AllowsCollectionSize(collectionSize))
            {
                AddSecurityError("YAMLS005", "YAML collection size exceeds the configured security limit.", yamlEvent.Span);
                return;
            }
        }

        if (yamlEvent.Kind == YamlEventKind.Scalar && !Security.AllowsScalarLength(yamlEvent.Value.Length))
        {
            AddSecurityError("YAMLS002", "YAML scalar length exceeds the configured security limit.", yamlEvent.Span);
            return;
        }

        if (yamlEvent.Kind == YamlEventKind.Alias)
        {
            SecurityAliasCount = SecurityAliasCount + 1;
            if (!Security.AllowsAliasCount(SecurityAliasCount))
            {
                AddSecurityError("YAMLS004", "YAML alias count exceeds the configured security limit.", yamlEvent.Span);
                return;
            }
        }

        if (yamlEvent.Kind == YamlEventKind.SequenceStart || yamlEvent.Kind == YamlEventKind.MappingStart)
        {
            SecurityCollectionKinds.Add(yamlEvent.Kind);
            SecurityCollectionNodeCounts.Add(0);
        }
        else if ((yamlEvent.Kind == YamlEventKind.SequenceEnd || yamlEvent.Kind == YamlEventKind.MappingEnd)
            && SecurityCollectionKinds.Count > 0)
        {
            SecurityCollectionKinds.RemoveAt(SecurityCollectionKinds.Count - 1);
            SecurityCollectionNodeCounts.RemoveAt(SecurityCollectionNodeCounts.Count - 1);
        }
    }

    internal void RecheckSecurityLimits()
    {
        if (Security == null || !Security.EnforceLimits || SecurityLimitExceeded)
        {
            return;
        }

        SecurityCollectionKinds.Clear();
        SecurityCollectionNodeCounts.Clear();
        SecurityAliasCount = 0;
        int index = 0;
        while (index < Events.Count && !SecurityLimitExceeded)
        {
            CheckSecurityLimit(Events[index]);
            index = index + 1;
        }
    }

    internal void AddSecurityError(string code, string message, YamlSourceSpan span)
    {
        SecurityLimitExceeded = true;
        AddDiagnostic(YamlDiagnostic.Error(code, message, span));
    }
}
