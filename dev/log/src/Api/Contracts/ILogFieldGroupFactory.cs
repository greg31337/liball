namespace log;

/// <summary>
/// Defines creation of named field groups and dotted nested field namespaces.
/// </summary>
public interface ILogFieldGroupFactory
{
    /// <summary>Creates one named dictionary field from structured child fields.</summary>
    LogField CreateFieldGroup(string? name, LogField[]? fields);
    /// <summary>Creates nested dictionary fields from a dotted namespace and structured child fields.</summary>
    LogField CreateFieldNamespace(string? namespaceName, LogField[]? fields);
}
