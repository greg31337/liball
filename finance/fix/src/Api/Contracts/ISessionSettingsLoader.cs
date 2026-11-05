using System.IO;

namespace fix;

/// <summary>Loads deterministic session settings from text.</summary>
public interface ISessionSettingsLoader
{
    /// <summary>Loads all session sections from a stream.</summary>
    /// <param name="source">The readable settings stream.</param>
    /// <returns>The settings in source order.</returns>
    SessionSettings[] LoadSessionSettings(Stream source);
}
