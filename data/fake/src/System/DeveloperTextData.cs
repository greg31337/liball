namespace fake;

internal static class DeveloperTextData
{
    internal const string Source = "Project-authored developer-data fixtures";
    internal const string License = "project-license";

    internal static readonly string[] EnvironmentPrefixes = new string[]
    {
        "APP", "CLIENT", "FAKE", "SERVICE", "SYSTEM", "WORKER"
    };

    internal static readonly string[] EnvironmentSuffixes = new string[]
    {
        "CACHE_PATH", "CONFIG_PATH", "LOG_LEVEL", "MAX_RETRIES",
        "OUTPUT_FORMAT", "REQUEST_TIMEOUT", "TEMP_DIRECTORY"
    };

    internal static readonly string[] CommandArguments = new string[]
    {
        "--config=example.json", "--dry-run", "--format=json",
        "--mode=sample", "--output=report.txt", "--profile=example",
        "--verbose", "--workers=4"
    };

    internal static readonly string[] LogCategories = new string[]
    {
        "Example.Api", "Example.Cache", "Example.Queue", "Example.Service",
        "Example.Storage", "Example.Worker"
    };

    internal static readonly string[] LogMessages = new string[]
    {
        "Synthetic item was queued.",
        "Synthetic operation completed.",
        "Synthetic request was processed.",
        "Synthetic retry was scheduled.",
        "Synthetic state was updated.",
        "Synthetic validation completed."
    };

    internal static readonly string[] ExceptionTypes = new string[]
    {
        "System.ArgumentException",
        "System.FormatException",
        "System.IO.IOException",
        "System.InvalidOperationException",
        "System.NotSupportedException",
        "System.TimeoutException"
    };

    internal static readonly string[] ExceptionMessages = new string[]
    {
        "Synthetic argument did not satisfy the required shape.",
        "Synthetic input was not in the expected format.",
        "Synthetic input or output operation could not be completed.",
        "Synthetic operation is not valid in the current state.",
        "Synthetic operation is not supported by this fixture.",
        "Synthetic operation exceeded its allotted duration."
    };

    internal static readonly string[] StackFrames = new string[]
    {
        "Example.Api.RequestHandler.Handle() in /workspace/src/RequestHandler.cs:line 48",
        "Example.Cache.EntryStore.Read() in /workspace/src/EntryStore.cs:line 37",
        "Example.Program.Main(String[] args) in /workspace/src/Program.cs:line 12",
        "Example.Queue.Dispatcher.Send() in /workspace/src/Dispatcher.cs:line 63",
        "Example.Service.Operation.Run() in /workspace/src/Operation.cs:line 29",
        "Example.Worker.Job.Execute() in /workspace/src/Job.cs:line 81"
    };
}
