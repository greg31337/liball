namespace config;

/// <summary>Records how a source value participated in configuration processing.</summary>
public class ConfigTrace
{
    /// <summary>Names the processing action, such as adding or overriding a value.</summary>
    public string Action;
    /// <summary>Names the source that supplied the value.</summary>
    public string SourceName;
    /// <summary>Identifies the affected configuration path, or is empty for the root.</summary>
    public string Path;
    /// <summary>Provides additional processing detail.</summary>
    public string Message;
    /// <summary>Optionally groups the participating source, or is empty when no group label was configured.</summary>
    public string SourceGroup;
    /// <summary>Records the one-based source precedence rank, or zero when the trace is not associated with a ranked source.</summary>
    public int PrecedenceRank;
    /// <summary>Identifies the merge decision, or <see cref="ConfigMergeDecision.None"/> for a general trace.</summary>
    public ConfigMergeDecision Decision;

    internal ConfigTrace(string action, string sourceName, string path, string message)
    {
        Action = action;
        SourceName = sourceName;
        Path = path;
        Message = message;
        SourceGroup = "";
        PrecedenceRank = 0;
        Decision = ConfigMergeDecision.None;
    }

    /// <summary>Formats the trace for logs or developer output.</summary>
    /// <returns>A single-line trace description.</returns>
    internal string ToDisplayString()
    {
        return "trace " + Action + " " + SourceLabel() + " " + PathLabel() + ": " + Message;
    }

    /// <summary>Formats the trace while masking its message when the path is considered secret.</summary>
    /// <param name="redactor">The secret-path matcher to apply.</param>
    /// <returns>A single-line redacted trace description.</returns>
    internal string ToRedactedDisplayString(ConfigRedactor redactor)
    {
        return "trace " + Action + " " + SourceLabel() + " " + PathLabel() + ": " + redactor.RedactDiagnosticMessage(Path, Message);
    }

    internal string SourceLabel()
    {
        if (SourceName.Length == 0)
        {
            return "(unknown)";
        }

        return SourceName;
    }

    internal string PathLabel()
    {
        if (Path.Length == 0)
        {
            return "(root)";
        }

        return Path;
    }
}
