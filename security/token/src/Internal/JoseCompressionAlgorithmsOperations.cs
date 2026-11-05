using System;
namespace token;

/// <summary>
/// Provides registered JWE compression algorithm values.
/// </summary>
internal static class JoseCompressionAlgorithmsOperations
{
    /// <summary>The DEFLATE compression algorithm.</summary>
    internal const string Deflate = JoseCompressionAlgorithms.Deflate;

    static readonly string[] all = new string[] { Deflate };

    /// <summary>
    /// Determines whether a case-sensitive JWE compression value is registered.
    /// </summary>
    /// <param name="name">The compression algorithm value.</param>
    /// <returns><c>true</c> when the value is registered; otherwise, <c>false</c>.</returns>
    internal static bool IsRegistered(string name)
    {
        return RegistryLookup.Contains(all, name, false);
    }

    /// <summary>
    /// Returns all registered JWE compression values known by this library.
    /// </summary>
    /// <returns>A new array containing the compression values.</returns>
    internal static string[] GetAll()
    {
        return RegistryLookup.Copy(all);
    }
}
