namespace log;

/// <summary>
/// Defines child logger creation and async-local scope operations.
/// </summary>
public interface ILoggerContext
{
    /// <summary>Creates a logger for a category.</summary>
    Logger For(string name);
    /// <summary>Creates a category logger from normalized CLR or C# type-name text.</summary>
    Logger ForTypeName(string? typeName);
    /// <summary>Creates a category logger with bound fields.</summary>
    Logger ForWithFields(string name, LogField[] fields);
    /// <summary>Creates a logger with one bound value.</summary>
    Logger ForField(string name, object value);
    /// <summary>Creates a logger with one bound field.</summary>
    Logger ForLogField(LogField field);
    /// <summary>Creates a logger with bound fields.</summary>
    Logger ForFields(LogField[] fields);
    /// <summary>Creates a same-category child logger with one bound application value.</summary>
    Logger WithField(string name, object? value);
    /// <summary>Creates a same-category child logger with one bound structured field.</summary>
    Logger WithLogField(LogField? field);
    /// <summary>Creates a same-category child logger with bound structured fields.</summary>
    Logger WithFields(LogField[]? fields);
    /// <summary>Creates a same-category child logger with one named field group.</summary>
    Logger WithFieldGroup(string? name, LogField[]? fields);
    /// <summary>Creates a same-category child logger with one dotted nested field namespace.</summary>
    Logger WithFieldNamespace(string? namespaceName, LogField[]? fields);
    /// <summary>Begins a scope containing fields.</summary>
    LogScope BeginScope(LogField[] fields);
    /// <summary>Begins a scope containing one value.</summary>
    LogScope BeginScopeField(string name, object value);
    /// <summary>Begins a scope containing one field.</summary>
    LogScope BeginScopeLogField(LogField field);
    /// <summary>Begins a scope containing one named field group.</summary>
    LogScope BeginScopeFieldGroup(string? name, LogField[]? fields);
    /// <summary>Begins a scope containing one dotted nested field namespace.</summary>
    LogScope BeginScopeFieldNamespace(string? namespaceName, LogField[]? fields);
}
