using System;

namespace log;

internal class LogBuiltInEnricher
{
    internal bool IsEnabled;
    internal bool IncludeRuntimeMetadata;
    internal bool HasRuntimeOverride;
    internal int ThreadId;
    internal int ProcessId;
    internal string MachineName = "";
    internal string ApplicationName = "";
    internal LogField[] StaticFields = new LogField[0];

    internal LogBuiltInEnricher()
    {
        IsEnabled = true;
        IncludeRuntimeMetadata = true;
        HasRuntimeOverride = false;
        ThreadId = 0;
        ProcessId = 0;
        MachineName = "";
        ApplicationName = "";
        StaticFields = new LogField[0];
    }

    internal void Enrich(LogEventBuilder builder)
    {
        if (!IsEnabled)
        {
            return;
        }

        if (builder == null)
        {
            return;
        }

        if (IncludeRuntimeMetadata)
        {
            if (HasRuntimeOverride)
            {
                builder.SetRuntime(ThreadId, ProcessId, MachineName, ApplicationName);
            }
            else
            {
                builder.SetRuntime(Environment.CurrentManagedThreadId, Environment.ProcessId, Environment.MachineName, AppDomain.CurrentDomain.FriendlyName);
            }
        }

        builder.AddEnricherFields(StaticFields);
    }

    internal void SetRuntime(int threadId, int processId, string? machineName, string? applicationName)
    {
        HasRuntimeOverride = true;
        ThreadId = LogEvent.NormalizeNonNegative(threadId);
        ProcessId = LogEvent.NormalizeNonNegative(processId);
        MachineName = LogResult.NormalizeText(machineName);
        ApplicationName = LogResult.NormalizeText(applicationName);
    }

    internal void ClearRuntimeOverride()
    {
        HasRuntimeOverride = false;
        ThreadId = 0;
        ProcessId = 0;
        MachineName = "";
        ApplicationName = "";
    }

    internal void AddStaticField(string name, object value)
    {
        AddStaticLogField(LogField.ValueField(name, LogTemplateBinder.ToLogValue(value)));
    }

    internal void AddStaticLogField(LogField field)
    {
        StaticFields = LogEventBuilder.AppendField(StaticFields, field);
    }

    internal void SetStaticFields(LogField[] fields)
    {
        StaticFields = Logger.CopyFields(fields);
    }

    internal void ClearStaticFields()
    {
        StaticFields = new LogField[0];
    }
}
