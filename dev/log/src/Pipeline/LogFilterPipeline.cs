namespace log;

internal class LogFilterPipeline
{
    internal LogFilter[] Filters = new LogFilter[0];

    internal LogFilterPipeline()
    {
        Filters = new LogFilter[0];
    }

    internal void AddFilter(LogFilter filter)
    {
        Filters = LogFilterPipeline.AppendFilter(Filters, filter);
    }

    internal void AddInterfaceFilter(ILogFilter filter)
    {
        AddFilter(LogFilter.Wrap(filter));
    }

    internal void SetFilters(LogFilter[] filters)
    {
        Filters = LogFilterPipeline.CopyFilters(filters);
    }

    internal void SetInterfaceFilters(ILogFilter[] filters)
    {
        ClearFilters();
        if (filters == null)
        {
            return;
        }

        foreach (ILogFilter filter in filters)
        {
            AddInterfaceFilter(filter);
        }
    }

    internal void ClearFilters()
    {
        Filters = new LogFilter[0];
    }

    internal LogDecision Decide(LogEvent logEvent)
    {
        return LogFilter.DecideAll(Filters, logEvent);
    }

    internal static LogFilter[] AppendFilter(LogFilter[] filters, LogFilter filter)
    {
        LogFilter storedFilter = filter;
        if (storedFilter == null)
        {
            return LogFilterPipeline.CopyFilters(filters);
        }

        LogFilter[] source = filters;
        if (source == null)
        {
            source = new LogFilter[0];
        }

        LogFilter[] copy = new LogFilter[source.Length + 1];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        copy[source.Length] = storedFilter;
        return copy;
    }

    internal static LogFilter[] CopyFilters(LogFilter[] filters)
    {
        if (filters == null)
        {
            return new LogFilter[0];
        }

        LogFilter[] copy = new LogFilter[filters.Length];
        int index = 0;
        while (index < filters.Length)
        {
            copy[index] = filters[index];
            index = index + 1;
        }

        return copy;
    }
}
