namespace log;

/// <summary>
/// Describes built-in data that a logger should add while constructing each event.
/// </summary>
internal class LogEnricher
{
    internal const string FieldKind = "field";
    internal const string RuntimeKind = "runtime";
    internal const string ActivityKind = "activity";
    internal const string InterfaceKind = "interface";

    internal string Kind = "";
    internal LogField FieldValue = LogField.String("", "");
    internal ILogEnricher? InterfaceEnricher;

    internal LogEnricher()
    {
        Kind = "";
        FieldValue = LogField.String("", "");
        InterfaceEnricher = null;
    }

    /// <summary>
    /// Creates an enricher that adds a normalized field to every event.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The configured enricher.</returns>
    internal static LogEnricher Field(string name, object value)
    {
        return LogEnricher.FieldValueEnricher(LogField.ValueField(name, LogTemplateBinder.ToLogValue(value)));
    }

    /// <summary>
    /// Creates an enricher that adds an existing structured field to every event.
    /// </summary>
    /// <param name="field">The structured field to add.</param>
    /// <returns>The configured enricher.</returns>
    internal static LogEnricher FieldValueEnricher(LogField field)
    {
        LogEnricher enricher = new LogEnricher();
        enricher.Kind = FieldKind;
        if (field == null)
        {
            enricher.FieldValue = LogField.String("", "");
        }
        else
        {
            enricher.FieldValue = field;
        }

        return enricher;
    }

    /// <summary>
    /// Creates an enricher that captures the current managed thread identifier.
    /// </summary>
    /// <returns>The configured enricher.</returns>
    internal static LogEnricher Thread()
    {
        return LogEnricher.Runtime();
    }

    /// <summary>
    /// Creates an enricher that captures the current process identifier.
    /// </summary>
    /// <returns>The configured enricher.</returns>
    internal static LogEnricher Process()
    {
        return LogEnricher.Runtime();
    }

    /// <summary>
    /// Creates an enricher that captures the current machine name.
    /// </summary>
    /// <returns>The configured enricher.</returns>
    internal static LogEnricher Machine()
    {
        return LogEnricher.Runtime();
    }

    /// <summary>
    /// Creates an enricher that captures thread, process, machine, and application data.
    /// </summary>
    /// <returns>The configured enricher.</returns>
    internal static LogEnricher Runtime()
    {
        LogEnricher enricher = new LogEnricher();
        enricher.Kind = RuntimeKind;
        return enricher;
    }

    /// <summary>
    /// Creates an enricher that captures identifiers from the current System.Diagnostics.Activity.
    /// </summary>
    /// <returns>The configured enricher.</returns>
    internal static LogEnricher Activity()
    {
        LogEnricher enricher = new LogEnricher();
        enricher.Kind = ActivityKind;
        return enricher;
    }

    /// <summary>
    /// Wraps an interface-based enricher for use by concrete configuration collections.
    /// </summary>
    /// <param name="enricher">The dynamic field provider to wrap.</param>
    /// <returns>A concrete adapter that installs the supplied enricher on created loggers.</returns>
    internal static LogEnricher Wrap(ILogEnricher enricher)
    {
        LogEnricher adapter = new LogEnricher();
        adapter.Kind = InterfaceKind;
        adapter.InterfaceEnricher = enricher;
        return adapter;
    }

    internal void Apply(Logger logger)
    {
        if (logger == null)
        {
            return;
        }

        if (Kind == FieldKind)
        {
            logger.BuiltInEnricher.AddStaticLogField(FieldValue);
            return;
        }

        if (Kind == RuntimeKind)
        {
            logger.BuiltInEnricher.IncludeRuntimeMetadata = true;
            logger.BuiltInEnricher.IsEnabled = true;
            return;
        }

        if (Kind == ActivityKind)
        {
            logger.ActivityEnricher.IsEnabled = true;
            return;
        }

        if (Kind == InterfaceKind && InterfaceEnricher != null)
        {
            logger.AddEnricher(InterfaceEnricher);
        }
    }

    internal static LogEnricher[] AppendEnricher(LogEnricher[] enrichers, LogEnricher enricher)
    {
        LogEnricher[] source = enrichers;
        if (source == null)
        {
            source = new LogEnricher[0];
        }

        if (enricher == null)
        {
            return LogEnricher.CopyEnrichers(source);
        }

        LogEnricher[] copy = new LogEnricher[source.Length + 1];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        copy[source.Length] = enricher;
        return copy;
    }

    internal static LogEnricher[] CopyEnrichers(LogEnricher[] enrichers)
    {
        if (enrichers == null)
        {
            return new LogEnricher[0];
        }

        LogEnricher[] copy = new LogEnricher[enrichers.Length];
        int index = 0;
        while (index < enrichers.Length)
        {
            copy[index] = enrichers[index];
            index = index + 1;
        }

        return copy;
    }
}
