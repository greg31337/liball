using System;

namespace log;

/// <summary>
/// Defines incremental construction of a structured event before it is passed to a logger.
/// </summary>
public interface ILogEventBuilder
{
    /// <summary>Sets the event severity.</summary>
    LogEventBuilder SetLevel(LogLevel level);
    /// <summary>Sets the normalized dotted logger name.</summary>
    LogEventBuilder SetLoggerName(string? loggerName);
    /// <summary>Sets the application event identifier and name.</summary>
    LogEventBuilder SetEvent(int eventId, string? eventName);
    /// <summary>Parses a message template, binds its positional values, and stores the rendered message and structured fields.</summary>
    LogEventBuilder SetMessageTemplateAndValues(string? messageTemplate, object?[]? values);
    /// <summary>Sets the exception attached to the event.</summary>
    LogEventBuilder SetException(Exception? exceptionValue);
    /// <summary>Adds one structured field.</summary>
    LogEventBuilder AddField(LogField? field);
    /// <summary>Adds structured fields in their supplied order.</summary>
    LogEventBuilder AddFields(LogField[]? fields);
    /// <summary>Sets activity trace, span, and parent span identifiers.</summary>
    LogEventBuilder SetTraceContext(string? traceId, string? spanId, string? parentSpanId);
    /// <summary>Overrides the runtime thread, process, machine, and application metadata.</summary>
    LogEventBuilder SetRuntimeMetadata(int threadId, int processId, string? machineName, string? applicationName);
    /// <summary>Controls whether ambient runtime metadata is captured when no explicit override is supplied.</summary>
    LogEventBuilder SetRuntimeMetadataEnabled(bool enabled);
    /// <summary>Sets source file, member, and line hints.</summary>
    LogEventBuilder SetSourceHints(string? sourceFile, string? sourceMemberName, int sourceLineNumber);
    /// <summary>Adds one field name to redact during event construction.</summary>
    LogEventBuilder AddRedactedFieldName(string? fieldName);
    /// <summary>Replaces the field names to redact during event construction.</summary>
    LogEventBuilder SetRedactedFieldNames(string[]? fieldNames);
    /// <summary>Creates the immutable-by-convention event snapshot.</summary>
    LogEvent Build();
}
