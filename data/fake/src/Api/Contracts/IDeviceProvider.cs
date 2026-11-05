namespace fake;
/// <summary>Defines the public operations exposed by <see cref="DeviceProvider"/>.</summary>
public interface IDeviceProvider
{
    /// <summary>Selects a project-fictional device manufacturer.</summary>
    /// <returns>A stable fictional manufacturer name.</returns>
    string Manufacturer();
    /// <summary>Selects a project-fictional device model.</summary>
    /// <returns>A stable fictional model name.</returns>
    string Model();
    /// <summary>Selects a broad device type.</summary>
    /// <returns>A lowercase hardware-type name.</returns>
    string Type();
    /// <summary>Selects an operating-system family from the stable device fixtures.</summary>
    /// <returns>An operating-system family name.</returns>
    string OperatingSystem();
    /// <summary>Selects an operating-system version from the stable device fixtures.</summary>
    /// <remarks>The result is fixture metadata and does not report a current software release.</remarks>
    /// <returns>A dotted numeric version.</returns>
    string OperatingSystemVersion();
    /// <summary>Selects an operating-system family from the stable device fixtures.</summary>
    /// <returns>An operating-system family name.</returns>
    string Os();
    /// <summary>Selects an operating-system version from the stable device fixtures.</summary>
    /// <returns>A dotted numeric fixture version.</returns>
    string OsVersion();
    /// <summary>Selects a screen width from the stable device fixtures.</summary>
    /// <returns>A positive pixel width.</returns>
    int ScreenWidth();
    /// <summary>Selects a screen height from the stable device fixtures.</summary>
    /// <returns>A positive pixel height.</returns>
    int ScreenHeight();
    /// <summary>Selects formatted screen dimensions from the stable device fixtures.</summary>
    /// <returns>Pixel width and height separated by a lowercase x.</returns>
    string ScreenResolution();
    /// <summary>Generates a lowercase hexadecimal hardware-token shape.</summary>
    /// <remarks>The result is not allocated and does not identify hardware or a user.</remarks>
    /// <returns>A 32-character lowercase hexadecimal string.</returns>
    string HardwareToken();
    /// <summary>Generates a lowercase hexadecimal Android-ID shape.</summary>
    /// <remarks>The result is not obtained from Android and identifies no device, app installation, or user.</remarks>
    /// <returns>A 16-character lowercase hexadecimal string.</returns>
    string AndroidId();
    /// <summary>Generates an opaque push-registration-token shape.</summary>
    /// <remarks>The result is not registered with a push service and is not a credential or delivery target.</remarks>
    /// <returns>A 64-character unpadded Base64 URL alphabet string.</returns>
    string PushToken();
    /// <summary>Selects correlated fictional device metadata and token shapes.</summary>
    /// <returns>A device record whose model, type, operating system, and screen agree.</returns>
    DeviceRecord Record();
}
