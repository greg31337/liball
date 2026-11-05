namespace log;

/// <summary>
/// Defines the lifecycle operation for a logging scope.
/// </summary>
public interface ILogScope
{
    /// <summary>Ends the scope.</summary>
    void Dispose();
}
