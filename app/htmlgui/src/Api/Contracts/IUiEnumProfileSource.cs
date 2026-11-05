namespace htmlgui;

/// <summary>Provides a categorical compatibility profile for an API endpoint or stored model.</summary>
public interface IUiEnumProfileSource
{
    /// <summary>Gets the endpoint or model's categorical compatibility profile.</summary>
    /// <returns>A compatibility profile whose fields can be inspected by the caller.</returns>
    UiEnumProfile GetEnumProfile();
}
