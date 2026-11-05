namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontLoadOptions"/>.</summary>
public interface IFontLoadOptions
{
    /// <summary>Gets the structural compatibility mode.</summary>
    /// <returns>The structural compatibility mode.</returns>
    FontLoadMode GetMode();

    /// <summary>Gets the ownership behavior requested for memory inputs.</summary>
    /// <returns>The input ownership behavior.</returns>
    FontInputOwnership GetInputOwnership();

    /// <summary>Reports whether bounded raw table bytes should remain available.</summary>
    /// <returns>True when raw tables should remain available; otherwise false.</returns>
    bool ShouldPreserveRawTables();

    /// <summary>Gets the deterministic repair policy.</summary>
    /// <returns>The repair policy.</returns>
    FontRepairPolicy GetRepairPolicy();

    /// <summary>Gets the input checksum validation policy.</summary>
    /// <returns>The checksum policy.</returns>
    FontChecksumPolicy GetChecksumPolicy();

    /// <summary>Gets the immutable resource limits.</summary>
    /// <returns>The selected resource limits.</returns>
    FontLimits GetLimits();

    /// <summary>Gets the token observed by synchronous loading work.</summary>
    /// <returns>The cancellation token.</returns>
    System.Threading.CancellationToken GetCancellationToken();

    /// <summary>Gets the caller-supplied media-type hint.</summary>
    /// <returns>The media-type hint, or null when none was supplied.</returns>
    string? GetMediaTypeHint();
}
