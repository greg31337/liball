using System.IO;

namespace fix;

/// <summary>Loads deterministic section-based FIX session configuration text.</summary>
public class TextSessionSettingsLoader : ISessionSettingsLoader
{
    /// <summary>Creates a text session settings loader.</summary>
    public TextSessionSettingsLoader()
    {
    }

    /// <summary>Loads UTF-8 <c>[DEFAULT]</c> and <c>[SESSION]</c> sections in source order.</summary>
    /// <param name="source">The readable settings stream, which remains open after loading.</param>
    /// <returns>The fully validated session settings in source order.</returns>
    /// <exception cref="FixSessionException">Thrown with a stable settings diagnostic when the text is invalid.</exception>
    public SessionSettings[] LoadSessionSettings(Stream source)
    {
        return SessionSettingsTextParser.ParseSessionSettings(source);
    }
}
