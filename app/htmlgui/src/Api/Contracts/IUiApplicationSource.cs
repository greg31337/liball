namespace htmlgui;

/// <summary>Provides the root portable application model to a host or processing pipeline.</summary>
public interface IUiApplicationSource
{
    /// <summary>Gets the current root application description.</summary>
    /// <returns>The portable application model.</returns>
    UiApplication GetApplication();
}
