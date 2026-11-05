using System;

namespace log;

/// <summary>
/// Incrementally constructs a structured event using an injectable UTC clock.
/// </summary>
/// <remarks>
/// Message values are bound positionally into rendered text and structured fields. Redaction is applied when Build is called, before the returned event can be passed to ILogger.WriteEvent.
/// </remarks>
public class LogEventBuilder : ILogEventBuilder
{
    internal LogEventBuilderCore Core;

    /// <summary>
    /// Creates an event builder using the supplied clock, or the system UTC clock when the clock is null.
    /// </summary>
    /// <param name="clock">The UTC clock used when Build captures the event timestamp.</param>
    public LogEventBuilder(ILogClock? clock)
    {
        Core = LogEventBuilderCore.Create(clock);
    }

    internal static LogEventBuilder Create(ILogClock? clock)
    {
        return new LogEventBuilder(clock);
    }

    /// <summary>Sets the event severity.</summary>
    /// <param name="level">The event severity.</param>
    /// <returns>This builder.</returns>
    public LogEventBuilder SetLevel(LogLevel level)
    {
        Core.SetLevel(level);
        return this;
    }

    /// <summary>Sets the normalized dotted logger name.</summary>
    /// <param name="loggerName">The logger name, or null for the root name.</param>
    /// <returns>This builder.</returns>
    public LogEventBuilder SetLoggerName(string? loggerName)
    {
        Core.SetLoggerName(LogResult.NormalizeText(loggerName));
        return this;
    }

    /// <summary>Sets the application event identifier and name.</summary>
    /// <param name="eventId">The numeric event identifier.</param>
    /// <param name="eventName">The event name, or null for no name.</param>
    /// <returns>This builder.</returns>
    public LogEventBuilder SetEvent(int eventId, string? eventName)
    {
        Core.SetEvent(eventId, LogResult.NormalizeText(eventName));
        return this;
    }

    /// <summary>Parses a message template, binds its positional values, and stores the rendered message and structured fields.</summary>
    /// <param name="messageTemplate">The message template, or null for an empty template.</param>
    /// <param name="values">Values bound positionally to template placeholders.</param>
    /// <returns>This builder.</returns>
    public LogEventBuilder SetMessageTemplateAndValues(string? messageTemplate, object?[]? values)
    {
        string normalizedTemplate = LogResult.NormalizeText(messageTemplate);
        LogTemplate template = LogTemplateParser.ParseTemplate(normalizedTemplate);
        object?[] normalizedValues = Logger.NormalizeValues(values);
        LogField[] fields = LogTemplateBinder.BindTemplate(template, normalizedValues);
        LogTemplateRenderer renderer = new LogTemplateRenderer();
        Core.SetMessage(normalizedTemplate, renderer.RenderFields(template, fields));
        Core.AddFields(fields);
        return this;
    }

    /// <summary>Sets the exception attached to the event.</summary>
    /// <param name="exceptionValue">The exception, or null for no exception.</param>
    /// <returns>This builder.</returns>
    public LogEventBuilder SetException(Exception? exceptionValue)
    {
        Core.SetException(exceptionValue);
        return this;
    }

    /// <summary>Adds one structured field.</summary>
    /// <param name="field">The field, or null to record one invalid field.</param>
    /// <returns>This builder.</returns>
    public LogEventBuilder AddField(LogField? field)
    {
        Core.AddField(field);
        return this;
    }

    /// <summary>Adds structured fields in their supplied order.</summary>
    /// <param name="fields">The fields, or null to leave the builder unchanged.</param>
    /// <returns>This builder.</returns>
    public LogEventBuilder AddFields(LogField[]? fields)
    {
        if (fields != null)
        {
            Core.AddFields(fields);
        }

        return this;
    }

    /// <summary>Sets activity trace, span, and parent span identifiers.</summary>
    /// <param name="traceId">The trace identifier.</param>
    /// <param name="spanId">The span identifier.</param>
    /// <param name="parentSpanId">The parent span identifier.</param>
    /// <returns>This builder.</returns>
    public LogEventBuilder SetTraceContext(string? traceId, string? spanId, string? parentSpanId)
    {
        Core.SetActivity(LogResult.NormalizeText(traceId), LogResult.NormalizeText(spanId), LogResult.NormalizeText(parentSpanId));
        return this;
    }

    /// <summary>Overrides the runtime thread, process, machine, and application metadata.</summary>
    /// <param name="threadId">The managed thread identifier; negative values become zero.</param>
    /// <param name="processId">The process identifier; negative values become zero.</param>
    /// <param name="machineName">The machine name.</param>
    /// <param name="applicationName">The application name.</param>
    /// <returns>This builder.</returns>
    public LogEventBuilder SetRuntimeMetadata(int threadId, int processId, string? machineName, string? applicationName)
    {
        Core.SetRuntime(threadId, processId, LogResult.NormalizeText(machineName), LogResult.NormalizeText(applicationName));
        return this;
    }

    /// <summary>Controls whether ambient runtime metadata is captured when no explicit override is supplied.</summary>
    /// <param name="enabled">true to capture ambient runtime metadata; otherwise, false.</param>
    /// <returns>This builder.</returns>
    public LogEventBuilder SetRuntimeMetadataEnabled(bool enabled)
    {
        Core.SuppressRuntime(!enabled);
        return this;
    }

    /// <summary>Sets source file, member, and line hints.</summary>
    /// <param name="sourceFile">The source file hint.</param>
    /// <param name="sourceMemberName">The source member hint.</param>
    /// <param name="sourceLineNumber">The one-based source line hint; negative values become zero.</param>
    /// <returns>This builder.</returns>
    public LogEventBuilder SetSourceHints(string? sourceFile, string? sourceMemberName, int sourceLineNumber)
    {
        Core.SetSource(LogResult.NormalizeText(sourceFile), LogResult.NormalizeText(sourceMemberName), sourceLineNumber);
        return this;
    }

    /// <summary>Adds one field name to redact during event construction.</summary>
    /// <param name="fieldName">The field name; null and empty names are ignored.</param>
    /// <returns>This builder.</returns>
    public LogEventBuilder AddRedactedFieldName(string? fieldName)
    {
        Core.AddRedactedFieldName(LogResult.NormalizeText(fieldName));
        return this;
    }

    /// <summary>Replaces the field names to redact during event construction.</summary>
    /// <param name="fieldNames">The field names, or null to clear redaction.</param>
    /// <returns>This builder.</returns>
    public LogEventBuilder SetRedactedFieldNames(string[]? fieldNames)
    {
        if (fieldNames == null)
        {
            Core.SetRedactedFieldNames(new string[0]);
        }
        else
        {
            Core.SetRedactedFieldNames(fieldNames);
        }

        return this;
    }

    /// <summary>Creates the immutable-by-convention event snapshot.</summary>
    /// <returns>The constructed event.</returns>
    public LogEvent Build()
    {
        return Core.Build();
    }

    internal LogEventBuilder SetMessage(string messageTemplate, string renderedMessage)
    {
        Core.SetMessage(messageTemplate, renderedMessage);
        return this;
    }

    internal LogEventBuilder SetMessageTemplate(string messageTemplate)
    {
        Core.SetMessageTemplate(messageTemplate);
        return this;
    }

    internal LogEventBuilder AddScopeField(LogField? field)
    {
        Core.AddScopeField(field);
        return this;
    }

    internal LogEventBuilder AddScopeFields(LogField[] fields)
    {
        Core.AddScopeFields(fields);
        return this;
    }

    internal LogEventBuilder AddEnricherField(LogField? field)
    {
        Core.AddEnricherField(field);
        return this;
    }

    internal LogEventBuilder AddEnricherFields(LogField[] fields)
    {
        Core.AddEnricherFields(fields);
        return this;
    }

    internal LogEventBuilder SetActivity(string traceId, string spanId, string parentSpanId)
    {
        Core.SetActivity(traceId, spanId, parentSpanId);
        return this;
    }

    internal LogEventBuilder SetRuntime(int threadId, int processId, string machineName, string applicationName)
    {
        Core.SetRuntime(threadId, processId, machineName, applicationName);
        return this;
    }

    internal LogEventBuilder SuppressRuntime(bool suppressRuntimeMetadata)
    {
        Core.SuppressRuntime(suppressRuntimeMetadata);
        return this;
    }

    internal LogEventBuilder SetSource(string sourceFile, string sourceMemberName, int sourceLineNumber)
    {
        Core.SetSource(sourceFile, sourceMemberName, sourceLineNumber);
        return this;
    }

    internal static LogField[] AppendField(LogField[]? fields, LogField? field)
    {
        return LogEventBuilderCore.AppendField(fields, field);
    }

    internal static LogField[] AppendFields(LogField[] target, LogField[] fields)
    {
        return LogEventBuilderCore.AppendFields(target, fields);
    }

    internal static LogField[] MergeFields(LogField[] scopeFields, LogField[] fields, LogField[] enricherFields)
    {
        return LogEventBuilderCore.MergeFields(scopeFields, fields, enricherFields);
    }

    internal static string[] AppendName(string[] names, string name)
    {
        return LogEventBuilderCore.AppendName(names, name);
    }

    internal static string[] CopyNames(string[] names)
    {
        return LogEventBuilderCore.CopyNames(names);
    }
}
