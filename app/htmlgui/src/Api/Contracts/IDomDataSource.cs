namespace htmlgui;

/// <summary>Provides renderer-neutral DOM snapshots and ordered mutation payloads.</summary>
public interface IDomDataSource
{
    /// <summary>Gets the current renderer-neutral DOM snapshot.</summary>
    /// <returns>The current snapshot.</returns>
    DomSnapshot GetDomSnapshot();

    /// <summary>Gets the ordered mutations currently available from the source.</summary>
    /// <returns>The mutations in application order.</returns>
    DomMutation[] GetDomMutations();
}
