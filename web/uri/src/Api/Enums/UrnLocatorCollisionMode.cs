namespace uri;

/// <summary>
/// Identifies how an existing locator component is handled during a URN merge.
/// </summary>
public enum UrnLocatorCollisionMode
{
    /// <summary>Reject the merge when both sources contain the component.</summary>
    Reject = 0,

    /// <summary>Keep the component already present in the locator.</summary>
    KeepLocator = 1,

    /// <summary>Replace the locator component with the URN component.</summary>
    ReplaceLocator = 2
}
