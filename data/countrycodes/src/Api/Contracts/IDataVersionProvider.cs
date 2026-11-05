namespace countrycodes;

/// <summary>
/// Defines access to compiled dataset version metadata.
/// </summary>
public interface IDataVersionProvider
{
    /// <summary>
    /// Gets a detached snapshot of the compiled dataset version metadata.
    /// </summary>
    /// <returns>The compiled dataset version metadata.</returns>
    DataVersion GetDataVersion();
}
