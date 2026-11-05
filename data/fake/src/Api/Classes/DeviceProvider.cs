namespace fake;
/// <summary>
/// Provides project-fictional device metadata and opaque device-token shapes.
/// </summary>
public class DeviceProvider : IDeviceProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    internal DeviceProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("device");
    }

    /// <summary>Selects a project-fictional device manufacturer.</summary>
    /// <returns>A stable fictional manufacturer name.</returns>
    public string Manufacturer()
    {
        return DeviceGenerator.Manufacturer(RandomValue, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Selects a project-fictional device model.</summary>
    /// <returns>A stable fictional model name.</returns>
    public string Model()
    {
        return DeviceGenerator.Model(RandomValue, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Selects a broad device type.</summary>
    /// <returns>A lowercase hardware-type name.</returns>
    public string Type()
    {
        return DeviceGenerator.Type(RandomValue, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Selects an operating-system family from the stable device fixtures.</summary>
    /// <returns>An operating-system family name.</returns>
    public string OperatingSystem()
    {
        return DeviceGenerator.OperatingSystem(RandomValue, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Selects an operating-system version from the stable device fixtures.</summary>
    /// <remarks>The result is fixture metadata and does not report a current software release.</remarks>
    /// <returns>A dotted numeric version.</returns>
    public string OperatingSystemVersion()
    {
        return DeviceGenerator.OperatingSystemVersion(RandomValue, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Selects an operating-system family from the stable device fixtures.</summary>
    /// <returns>An operating-system family name.</returns>
    public string Os()
    {
        return OperatingSystem();
    }

    /// <summary>Selects an operating-system version from the stable device fixtures.</summary>
    /// <returns>A dotted numeric fixture version.</returns>
    public string OsVersion()
    {
        return OperatingSystemVersion();
    }

    /// <summary>Selects a screen width from the stable device fixtures.</summary>
    /// <returns>A positive pixel width.</returns>
    public int ScreenWidth()
    {
        return DeviceGenerator.ScreenWidth(RandomValue);
    }

    /// <summary>Selects a screen height from the stable device fixtures.</summary>
    /// <returns>A positive pixel height.</returns>
    public int ScreenHeight()
    {
        return DeviceGenerator.ScreenHeight(RandomValue);
    }

    /// <summary>Selects formatted screen dimensions from the stable device fixtures.</summary>
    /// <returns>Pixel width and height separated by a lowercase x.</returns>
    public string ScreenResolution()
    {
        return DeviceGenerator.ScreenResolution(RandomValue, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a lowercase hexadecimal hardware-token shape.</summary>
    /// <remarks>The result is not allocated and does not identify hardware or a user.</remarks>
    /// <returns>A 32-character lowercase hexadecimal string.</returns>
    public string HardwareToken()
    {
        return DeviceGenerator.HardwareToken(RandomValue, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a lowercase hexadecimal Android-ID shape.</summary>
    /// <remarks>The result is not obtained from Android and identifies no device, app installation, or user.</remarks>
    /// <returns>A 16-character lowercase hexadecimal string.</returns>
    public string AndroidId()
    {
        return DeviceGenerator.AndroidId(RandomValue, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates an opaque push-registration-token shape.</summary>
    /// <remarks>The result is not registered with a push service and is not a credential or delivery target.</remarks>
    /// <returns>A 64-character unpadded Base64 URL alphabet string.</returns>
    public string PushToken()
    {
        return DeviceGenerator.PushToken(RandomValue, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Selects correlated fictional device metadata and token shapes.</summary>
    /// <returns>A device record whose model, type, operating system, and screen agree.</returns>
    public DeviceRecord Record()
    {
        return DeviceGenerator.Record(RandomValue, Context.CapturedOptions.MaximumOutputSize);
    }
}
