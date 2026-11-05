using System;

namespace log;

internal class LogEventBuilderCore
{
    internal ILogClock Clock = LogClock.SystemClock();
    internal LogLevel Level;
    internal string LoggerName = "";
    internal int EventId;
    internal string EventName = "";
    internal string MessageTemplate = "";
    internal string RenderedMessage = "";
    internal LogField[] Fields = new LogField[0];
    internal LogField[] ScopeFields = new LogField[0];
    internal LogField[] EnricherFields = new LogField[0];
    internal Exception? ExceptionValue;
    internal string TraceId = "";
    internal string SpanId = "";
    internal string ParentSpanId = "";
    internal bool HasRuntimeMetadata;
    internal bool SuppressRuntimeMetadata;
    internal int ThreadId;
    internal int ProcessId;
    internal string MachineName = "";
    internal string ApplicationName = "";
    internal string SourceFile = "";
    internal string SourceMemberName = "";
    internal int SourceLineNumber;
    internal string[] RedactedFieldNames = new string[0];

    internal LogEventBuilderCore(ILogClock? clock)
    {
        Initialize(clock);
    }

    internal static LogEventBuilderCore Create(ILogClock? clock)
    {
        return new LogEventBuilderCore(clock);
    }

    internal void Initialize(ILogClock? clock)
    {
        if (clock == null)
        {
            Clock = LogClock.SystemClock();
        }
        else
        {
            Clock = clock;
        }

        Level = LogLevel.Info;
        LoggerName = "";
        EventId = 0;
        EventName = "";
        MessageTemplate = "";
        RenderedMessage = "";
        Fields = new LogField[0];
        ScopeFields = new LogField[0];
        EnricherFields = new LogField[0];
        ExceptionValue = null;
        TraceId = "";
        SpanId = "";
        ParentSpanId = "";
        HasRuntimeMetadata = false;
        SuppressRuntimeMetadata = false;
        ThreadId = 0;
        ProcessId = 0;
        MachineName = "";
        ApplicationName = "";
        SourceFile = "";
        SourceMemberName = "";
        SourceLineNumber = 0;
        RedactedFieldNames = new string[0];
    }

    internal LogEventBuilderCore SetLevel(LogLevel level)
    {
        Level = level;
        return this;
    }

    internal LogEventBuilderCore SetLoggerName(string loggerName)
    {
        LoggerName = LogName.NormalizeLoggerName(loggerName);
        return this;
    }

    internal LogEventBuilderCore SetEvent(int eventId, string eventName)
    {
        EventId = eventId;
        EventName = LogResult.NormalizeText(eventName);
        return this;
    }

    internal LogEventBuilderCore SetMessage(string messageTemplate, string renderedMessage)
    {
        MessageTemplate = LogResult.NormalizeText(messageTemplate);
        RenderedMessage = LogResult.NormalizeText(renderedMessage);
        return this;
    }

    internal LogEventBuilderCore SetMessageTemplate(string messageTemplate)
    {
        MessageTemplate = LogResult.NormalizeText(messageTemplate);
        RenderedMessage = "";
        return this;
    }

    internal LogEventBuilderCore SetException(Exception? exceptionValue)
    {
        ExceptionValue = exceptionValue;
        return this;
    }

    internal LogEventBuilderCore AddField(LogField? field)
    {
        Fields = LogEventBuilder.AppendField(Fields, field);
        return this;
    }

    internal LogEventBuilderCore AddFields(LogField[] fields)
    {
        Fields = LogEventBuilder.AppendFields(Fields, fields);
        return this;
    }

    internal LogEventBuilderCore AddScopeField(LogField? field)
    {
        ScopeFields = LogEventBuilder.AppendField(ScopeFields, field);
        return this;
    }

    internal LogEventBuilderCore AddScopeFields(LogField[] fields)
    {
        ScopeFields = LogEventBuilder.AppendFields(ScopeFields, fields);
        return this;
    }

    internal LogEventBuilderCore AddEnricherField(LogField? field)
    {
        EnricherFields = LogEventBuilder.AppendField(EnricherFields, field);
        return this;
    }

    internal LogEventBuilderCore AddEnricherFields(LogField[] fields)
    {
        EnricherFields = LogEventBuilder.AppendFields(EnricherFields, fields);
        return this;
    }

    internal LogEventBuilderCore SetActivity(string traceId, string spanId, string parentSpanId)
    {
        TraceId = LogResult.NormalizeText(traceId);
        SpanId = LogResult.NormalizeText(spanId);
        ParentSpanId = LogResult.NormalizeText(parentSpanId);
        return this;
    }

    internal LogEventBuilderCore SetRuntime(int threadId, int processId, string machineName, string applicationName)
    {
        HasRuntimeMetadata = true;
        ThreadId = LogEvent.NormalizeNonNegative(threadId);
        ProcessId = LogEvent.NormalizeNonNegative(processId);
        MachineName = LogResult.NormalizeText(machineName);
        ApplicationName = LogResult.NormalizeText(applicationName);
        return this;
    }

    internal LogEventBuilderCore SuppressRuntime(bool suppressRuntimeMetadata)
    {
        SuppressRuntimeMetadata = suppressRuntimeMetadata;
        return this;
    }

    internal LogEventBuilderCore SetSource(string sourceFile, string sourceMemberName, int sourceLineNumber)
    {
        SourceFile = LogResult.NormalizeText(sourceFile);
        SourceMemberName = LogResult.NormalizeText(sourceMemberName);
        SourceLineNumber = LogEvent.NormalizeNonNegative(sourceLineNumber);
        return this;
    }

    internal LogEventBuilderCore AddRedactedFieldName(string fieldName)
    {
        RedactedFieldNames = LogEventBuilder.AppendName(RedactedFieldNames, fieldName);
        return this;
    }

    internal LogEventBuilderCore SetRedactedFieldNames(string[] fieldNames)
    {
        RedactedFieldNames = LogEventBuilder.CopyNames(fieldNames);
        return this;
    }

    internal LogEvent Build()
    {
        LogField[] mergedFields = LogEventBuilder.MergeFields(ScopeFields, Fields, EnricherFields);
        mergedFields = LogRedactor.RedactFields(mergedFields, RedactedFieldNames);
        string renderedMessage = RenderedMessage;
        if (renderedMessage.Length == 0)
        {
            renderedMessage = MessageTemplate;
        }

        int threadId = Environment.CurrentManagedThreadId;
        int processId = Environment.ProcessId;
        string machineName = Environment.MachineName;
        string applicationName = AppDomain.CurrentDomain.FriendlyName;

        if (SuppressRuntimeMetadata)
        {
            threadId = 0;
            processId = 0;
            machineName = "";
            applicationName = "";
        }

        if (HasRuntimeMetadata)
        {
            threadId = ThreadId;
            processId = ProcessId;
            machineName = MachineName;
            applicationName = ApplicationName;
        }

        return new LogEvent(Clock.UtcNow(), Level, LoggerName, EventId, EventName, MessageTemplate, renderedMessage, mergedFields, ExceptionValue, TraceId, SpanId, ParentSpanId, threadId, processId, machineName, applicationName, SourceFile, SourceMemberName, SourceLineNumber);
    }

    internal static LogField[] AppendField(LogField[]? fields, LogField? field)
    {
        LogField storedField = new LogField("", null);
        if (field != null)
        {
            storedField = field;
        }

        if (fields == null)
        {
            LogField[] single = new LogField[1];
            single[0] = storedField;
            return single;
        }

        LogField[] copy = new LogField[fields.Length + 1];
        int index = 0;
        while (index < fields.Length)
        {
            copy[index] = fields[index];
            index = index + 1;
        }

        copy[fields.Length] = storedField;
        return copy;
    }

    internal static LogField[] AppendFields(LogField[] target, LogField[] fields)
    {
        LogField[] result = target;
        if (result == null)
        {
            result = new LogField[0];
        }

        if (fields == null)
        {
            return result;
        }

        foreach (LogField field in fields)
        {
            result = LogEventBuilder.AppendField(result, field);
        }

        return result;
    }

    internal static LogField[] MergeFields(LogField[] scopeFields, LogField[] fields, LogField[] enricherFields)
    {
        LogField[] merged = new LogField[0];
        merged = LogEventBuilder.AppendFields(merged, scopeFields);
        merged = LogEventBuilder.AppendFields(merged, fields);
        merged = LogEventBuilder.AppendFields(merged, enricherFields);
        return merged;
    }

    internal static string[] AppendName(string[] names, string name)
    {
        string normalizedName = LogResult.NormalizeText(name);
        if (normalizedName.Length == 0)
        {
            return LogEventBuilder.CopyNames(names);
        }

        string[] source = names;
        if (source == null)
        {
            source = new string[0];
        }

        string[] copy = new string[source.Length + 1];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        copy[source.Length] = normalizedName;
        return copy;
    }

    internal static string[] CopyNames(string[] names)
    {
        if (names == null)
        {
            return new string[0];
        }

        string[] copy = new string[names.Length];
        int index = 0;
        while (index < names.Length)
        {
            copy[index] = LogResult.NormalizeText(names[index]);
            index = index + 1;
        }

        return copy;
    }
}
