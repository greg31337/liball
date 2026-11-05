using System.Diagnostics;

namespace log;

internal class LogActivityEnricher
{
    internal bool IsEnabled;

    internal LogActivityEnricher()
    {
        IsEnabled = true;
    }

    internal void Enrich(LogEventBuilder? builder)
    {
        if (!IsEnabled)
        {
            return;
        }

        if (builder == null)
        {
            return;
        }

        Activity? activity = Activity.Current;
        if (activity == null)
        {
            return;
        }

        string traceId = LogActivityEnricher.NormalizeTraceId(activity.TraceId.ToString());
        string spanId = LogActivityEnricher.NormalizeSpanId(activity.SpanId.ToString());
        string parentSpanId = LogActivityEnricher.NormalizeSpanId(activity.ParentSpanId.ToString());

        if (traceId.Length == 0 && spanId.Length == 0 && parentSpanId.Length == 0)
        {
            return;
        }

        builder.SetActivity(traceId, spanId, parentSpanId);
    }

    internal static string NormalizeTraceId(string traceId)
    {
        string normalized = LogResult.NormalizeText(traceId);
        if (normalized == "00000000000000000000000000000000")
        {
            return "";
        }

        return normalized;
    }

    internal static string NormalizeSpanId(string spanId)
    {
        string normalized = LogResult.NormalizeText(spanId);
        if (normalized == "0000000000000000")
        {
            return "";
        }

        return normalized;
    }
}
