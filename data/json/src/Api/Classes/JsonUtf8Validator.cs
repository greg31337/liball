namespace json;

/// <summary>Validates complete UTF-8 JSON input without constructing a document model.</summary>
public class JsonUtf8Validator : IJsonUtf8Validator
{
    /// <summary>Initializes a reusable UTF-8 JSON validator.</summary>
    public JsonUtf8Validator()
    {
    }

    /// <summary>Validates one UTF-8 input using the supplied policies.</summary>
    /// <param name="utf8">The complete UTF-8 input.</param>
    /// <param name="options">The input policies to apply.</param>
    /// <returns>The validation outcome.</returns>
    public JsonOperationResult ValidateUtf8(
        global::System.ReadOnlySpan<byte> utf8,
        JsonReaderOptions options)
    {
        byte[] ownedInput = new byte[utf8.Length];
        utf8.CopyTo(ownedInput);
        JsonUtf8ValidationRunner runner = new JsonUtf8ValidationRunner(ownedInput, options);
        global::System.Threading.Tasks.Task task = new global::System.Threading.Tasks.Task(
            () => runner.ExecuteValidation());
        task.RunSynchronously(global::System.Threading.Tasks.TaskScheduler.Default);
        if (!task.IsFaulted)
        {
            return runner.GetResult();
        }

        global::System.Exception failure = task.Exception?.InnerException
            ?? throw new global::System.InvalidOperationException("Validation failed without an exception.");
        return runner.CreateFailureResult(failure);
    }

    /// <summary>Validates complete UTF-8 JSON from a caller-owned stream.</summary>
    /// <param name="stream">The caller-owned input stream.</param>
    /// <param name="options">The input policies to apply.</param>
    /// <returns>The validation outcome.</returns>
    public JsonOperationResult ValidateStream(
        global::System.IO.Stream stream,
        JsonReaderOptions options)
    {
        JsonStreamValidationRunner runner = new JsonStreamValidationRunner(stream, options);
        global::System.Threading.Tasks.Task task = new global::System.Threading.Tasks.Task(
            () => runner.ExecuteStreamValidation());
        task.RunSynchronously(global::System.Threading.Tasks.TaskScheduler.Default);
        if (!task.IsFaulted)
        {
            return runner.GetStreamValidationResult();
        }

        global::System.Exception failure = task.Exception?.InnerException
            ?? throw new global::System.InvalidOperationException("Stream validation failed without an exception.");
        return runner.CreateStreamFailureResult(failure);
    }
}
