namespace qrcode;

/// <summary>Identifies the expected relationship between dark and light modules.</summary>
public enum QrPolarity
{
    /// <summary>Examines both supported polarities.</summary>
    Automatic = 0,

    /// <summary>Uses dark modules on a light background.</summary>
    DarkOnLight = 1,

    /// <summary>Uses light modules on a dark background.</summary>
    LightOnDark = 2
}
