namespace xml;

/// <summary>
/// Specifies XSLT package component visibility.
/// </summary>
public enum XsltComponentVisibility
{
    /// <summary>
    /// Makes a component public.
    /// </summary>
    Public = 0,
    /// <summary>
    /// Makes a component private.
    /// </summary>
    Private = 1,
    /// <summary>
    /// Makes a component final.
    /// </summary>
    Final = 2,
    /// <summary>
    /// Makes a component abstract.
    /// </summary>
    Abstract = 3,
    /// <summary>
    /// Hides a component.
    /// </summary>
    Hidden = 4
}
