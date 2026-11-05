using System;

namespace csv;

/// <summary>
/// Wraps an optional callback that receives rows rejected by validation.
/// </summary>
public class CsvRejectHandler : ICsvRejectHandler
{
    /// <summary>
    /// Callback receiving each rejected row and diagnostic.
    /// </summary>
    internal Action<CsvRow, CsvDiagnostic> HandleCallback;
    /// <summary>
    /// Whether reject handling invokes its callback.
    /// </summary>
    internal bool HasHandleCallback;

    /// <summary>
    /// Initializes a reject handler with a callback.
    /// </summary>
    /// <param name="handleCallback">The callback invoked for rejected rows.</param>
    public CsvRejectHandler(Action<CsvRow, CsvDiagnostic> handleCallback)
    {
        HandleCallback = handleCallback;
        HasHandleCallback = true;
    }

    /// <summary>
    /// Creates an instance populated with library defaults.
    /// </summary>
    /// <returns>The initialized result.</returns>
    internal static CsvRejectHandler CreateDefault()
    {
        CsvRejectHandler handler = new CsvRejectHandler(HandleDefault);
        handler.HasHandleCallback = false;
        return handler;
    }

    /// <summary>
    /// Invokes the reject callback when enabled.
    /// </summary>
    /// <param name="row">The row to process.</param>
    /// <param name="diagnostic">The diagnostic to process.</param>
    public void Handle(CsvRow row, CsvDiagnostic diagnostic)
    {
        if (HasHandleCallback)
        {
            HandleCallback(row, diagnostic);
        }
    }

    internal static void HandleDefault(CsvRow row, CsvDiagnostic diagnostic)
    {
    }
}
