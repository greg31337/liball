namespace fake;

/// <summary>Represents correlated project-fictional device hardware and software metadata.</summary>
/// <remarks>Names and versions are stable fixtures. Tokens do not identify hardware, installations, users, or registered push destinations.</remarks>
public class DeviceProfile
{
    /// <summary>The project-fictional manufacturer name.</summary>
    public string Manufacturer = string.Empty;

    /// <summary>The project-fictional model name.</summary>
    public string Model = string.Empty;

    /// <summary>The broad hardware type.</summary>
    public string Type = string.Empty;

    /// <summary>The operating-system family associated with the fixture.</summary>
    public string OperatingSystem = string.Empty;

    /// <summary>The stable fixture version associated with the operating-system family.</summary>
    public string OperatingSystemVersion = string.Empty;

    /// <summary>The screen width in pixels.</summary>
    public int ScreenWidth;

    /// <summary>The screen height in pixels.</summary>
    public int ScreenHeight;

    /// <summary>The screen dimensions formatted as width, a lowercase x, and height.</summary>
    public string ScreenResolution = string.Empty;

    /// <summary>A lowercase hexadecimal hardware-token shape with no allocation claim.</summary>
    public string HardwareToken = string.Empty;

    /// <summary>Whether this fixture uses the Android operating-system family.</summary>
    public bool UsesAndroid;

    /// <summary>A lowercase 64-bit Android-ID shape, or an empty string for a non-Android fixture.</summary>
    public string AndroidId = string.Empty;

    /// <summary>An opaque push-registration-token shape that is not registered with a service.</summary>
    public string PushToken = string.Empty;

    internal static DeviceProfile FromDeviceRecord(DeviceRecord source)
    {
        DeviceProfile value = new DeviceProfile();
        value.Manufacturer = source.Manufacturer;
        value.Model = source.Model;
        value.Type = source.Type;
        value.OperatingSystem = source.OperatingSystem;
        value.OperatingSystemVersion = source.OperatingSystemVersion;
        value.ScreenWidth = source.ScreenWidth;
        value.ScreenHeight = source.ScreenHeight;
        value.ScreenResolution = source.ScreenResolution;
        value.HardwareToken = source.HardwareToken;
        value.UsesAndroid = source.UsesAndroid;
        value.AndroidId = source.AndroidId;
        value.PushToken = source.PushToken;
        return value;
    }
}
