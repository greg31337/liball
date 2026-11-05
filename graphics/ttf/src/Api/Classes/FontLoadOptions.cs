namespace ttf;

/// <summary>Defines ownership, validation, preservation, and cancellation choices for loading.</summary>
/// <remarks>
/// File and stream inputs are always copied, regardless of the input ownership
/// value. Retained read-only memory is a trusted-input optimization: the caller
/// must keep that memory alive and unchanged for the lifetime of the loaded
/// package. The default and all predefined profiles copy input bytes.
/// <para>
/// Cancellation is synchronous. Loaders observe the token before I/O and at
/// bounded checkpoints during long parsing or decompression work. Cancellation
/// throws <see cref="System.OperationCanceledException" />, never returns a
/// partial package, never closes a caller stream, and does not rewind bytes
/// already read from that stream.
/// </para>
/// </remarks>
public class FontLoadOptions : IFontLoadOptions
{
    FontLoadMode mode;
    FontInputOwnership inputOwnership;
    bool preserveRawTables;
    FontRepairPolicy repairPolicy;
    FontChecksumPolicy checksumPolicy;
    FontLimits limits;
    System.Threading.CancellationToken cancellationToken;
    string? mediaTypeHint;

    /// <summary>Creates options using the compatible profile with no cancellation.</summary>
    public FontLoadOptions()
    {
        mode = FontLoadMode.Compatible;
        inputOwnership = FontInputOwnership.Copy;
        preserveRawTables = true;
        repairPolicy = FontRepairPolicy.Safe;
        checksumPolicy = FontChecksumPolicy.ReportMismatch;
        limits = FontLimits.CreateCompatible();
        cancellationToken = System.Threading.CancellationToken.None;
        mediaTypeHint = null;
    }

    /// <summary>Creates explicitly configured loading options.</summary>
    /// <param name="mode">The structural compatibility mode.</param>
    /// <param name="inputOwnership">The ownership behavior for memory inputs.</param>
    /// <param name="preserveRawTables">
    /// Whether bounded raw bytes for known, unknown, and unsupported tables remain
    /// available for inspection and eligible for later preservation.
    /// </param>
    /// <param name="repairPolicy">The deterministic repair policy.</param>
    /// <param name="checksumPolicy">The input checksum validation policy.</param>
    /// <param name="limits">The hard resource limits.</param>
    /// <param name="cancellationToken">The token observed by synchronous loading work.</param>
    /// <returns>A new configured option set.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// <c>limits</c> is null.
    /// </exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// An enum value is not defined.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// Strict mode requests repair or does not require valid checksums, or
    /// inspect-only mode requests repair.
    /// </exception>
    internal static FontLoadOptions Create(
        FontLoadMode mode,
        FontInputOwnership inputOwnership,
        bool preserveRawTables,
        FontRepairPolicy repairPolicy,
        FontChecksumPolicy checksumPolicy,
        FontLimits limits,
        System.Threading.CancellationToken cancellationToken)
    {
        ValidateMode(mode);
        ValidateInputOwnership(inputOwnership);
        ValidateRepairPolicy(repairPolicy);
        ValidateChecksumPolicy(checksumPolicy);

        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        if (mode == FontLoadMode.Strict && repairPolicy != FontRepairPolicy.None)
        {
            throw new System.ArgumentException("Strict mode does not permit repairs.", "repairPolicy");
        }

        if (mode == FontLoadMode.Strict && checksumPolicy != FontChecksumPolicy.RequireValid)
        {
            throw new System.ArgumentException("Strict mode requires valid checksums.", "checksumPolicy");
        }

        if (mode == FontLoadMode.InspectOnly && repairPolicy != FontRepairPolicy.None)
        {
            throw new System.ArgumentException("Inspect-only mode does not repair input data.", "repairPolicy");
        }

        FontLoadOptions options = new FontLoadOptions();
        options.mode = mode;
        options.inputOwnership = inputOwnership;
        options.preserveRawTables = preserveRawTables;
        options.repairPolicy = repairPolicy;
        options.checksumPolicy = checksumPolicy;
        options.limits = limits;
        options.cancellationToken = cancellationToken;
        options.mediaTypeHint = null;
        return options;
    }

    /// <summary>Creates explicitly configured loading options with a media-type hint.</summary>
    /// <param name="mode">The structural compatibility mode.</param>
    /// <param name="inputOwnership">The ownership behavior for memory inputs.</param>
    /// <param name="preserveRawTables">Whether bounded raw table bytes remain available.</param>
    /// <param name="repairPolicy">The deterministic repair policy.</param>
    /// <param name="checksumPolicy">The input checksum validation policy.</param>
    /// <param name="limits">The hard resource limits.</param>
    /// <param name="cancellationToken">The token observed by synchronous loading work.</param>
    /// <param name="mediaTypeHint">The caller-supplied media type, or null.</param>
    /// <returns>A new configured option set.</returns>
    internal static FontLoadOptions CreateWithMediaTypeHint(
        FontLoadMode mode,
        FontInputOwnership inputOwnership,
        bool preserveRawTables,
        FontRepairPolicy repairPolicy,
        FontChecksumPolicy checksumPolicy,
        FontLimits limits,
        System.Threading.CancellationToken cancellationToken,
        string? mediaTypeHint)
    {
        FontLoadOptions options = Create(
            mode,
            inputOwnership,
            preserveRawTables,
            repairPolicy,
            checksumPolicy,
            limits,
            cancellationToken);
        options.mediaTypeHint = mediaTypeHint;
        return options;
    }

    /// <summary>Creates options using the compatible profile with no cancellation.</summary>
    /// <returns>A new compatible option set.</returns>
    internal static FontLoadOptions Create()
    {
        return new FontLoadOptions();
    }

    /// <summary>Creates strict options with no cancellation.</summary>
    /// <returns>A new strict option set.</returns>
    internal static FontLoadOptions CreateStrict()
    {
        return CreateStrict(System.Threading.CancellationToken.None);
    }

    /// <summary>Creates strict options with a cancellation token.</summary>
    /// <param name="cancellationToken">The token observed by synchronous loading work.</param>
    /// <returns>A new strict option set.</returns>
    internal static FontLoadOptions CreateStrict(System.Threading.CancellationToken cancellationToken)
    {
        return FontLoadOptions.Create(
            FontLoadMode.Strict,
            FontInputOwnership.Copy,
            true,
            FontRepairPolicy.None,
            FontChecksumPolicy.RequireValid,
            FontLimits.CreateStrict(),
            cancellationToken);
    }

    /// <summary>Creates compatible options with no cancellation.</summary>
    /// <returns>A new compatible option set.</returns>
    internal static FontLoadOptions CreateCompatible()
    {
        return FontLoadOptions.Create();
    }

    /// <summary>Creates compatible options with a cancellation token.</summary>
    /// <param name="cancellationToken">The token observed by synchronous loading work.</param>
    /// <returns>A new compatible option set.</returns>
    internal static FontLoadOptions CreateCompatible(System.Threading.CancellationToken cancellationToken)
    {
        return FontLoadOptions.Create(
            FontLoadMode.Compatible,
            FontInputOwnership.Copy,
            true,
            FontRepairPolicy.Safe,
            FontChecksumPolicy.ReportMismatch,
            FontLimits.CreateCompatible(),
            cancellationToken);
    }

    /// <summary>Creates inspect-only options with no cancellation.</summary>
    /// <returns>A new inspect-only option set.</returns>
    internal static FontLoadOptions CreateInspectOnly()
    {
        return CreateInspectOnly(System.Threading.CancellationToken.None);
    }

    /// <summary>Creates inspect-only options with a cancellation token.</summary>
    /// <param name="cancellationToken">The token observed by synchronous loading work.</param>
    /// <returns>A new inspect-only option set.</returns>
    internal static FontLoadOptions CreateInspectOnly(System.Threading.CancellationToken cancellationToken)
    {
        return FontLoadOptions.Create(
            FontLoadMode.InspectOnly,
            FontInputOwnership.Copy,
            true,
            FontRepairPolicy.None,
            FontChecksumPolicy.ReportMismatch,
            FontLimits.CreateInspectOnly(),
            cancellationToken);
    }

    /// <summary>Gets the structural compatibility mode.</summary>
    /// <returns>The structural compatibility mode.</returns>
    public FontLoadMode GetMode()
    {
        return mode;
    }

    /// <summary>Gets the ownership behavior requested for memory inputs.</summary>
    /// <returns>The input ownership behavior.</returns>
    public FontInputOwnership GetInputOwnership()
    {
        return inputOwnership;
    }

    /// <summary>Reports whether bounded raw table bytes should remain available.</summary>
    /// <returns>True when raw tables should remain available; otherwise false.</returns>
    public bool ShouldPreserveRawTables()
    {
        return preserveRawTables;
    }

    /// <summary>Gets the deterministic repair policy.</summary>
    /// <returns>The repair policy.</returns>
    public FontRepairPolicy GetRepairPolicy()
    {
        return repairPolicy;
    }

    /// <summary>Gets the input checksum validation policy.</summary>
    /// <returns>The checksum policy.</returns>
    public FontChecksumPolicy GetChecksumPolicy()
    {
        return checksumPolicy;
    }

    /// <summary>Gets the immutable resource limits.</summary>
    /// <returns>The selected resource limits.</returns>
    public FontLimits GetLimits()
    {
        return limits;
    }

    /// <summary>Gets the token observed by synchronous loading work.</summary>
    /// <returns>The cancellation token.</returns>
    public System.Threading.CancellationToken GetCancellationToken()
    {
        return cancellationToken;
    }

    /// <summary>Gets the caller-supplied media-type hint.</summary>
    /// <returns>The media-type hint, or null when none was supplied.</returns>
    public string? GetMediaTypeHint()
    {
        return mediaTypeHint;
    }

    static void ValidateMode(FontLoadMode value)
    {
        if (value != FontLoadMode.Strict &&
            value != FontLoadMode.Compatible &&
            value != FontLoadMode.InspectOnly)
        {
            throw new System.ArgumentOutOfRangeException("mode");
        }
    }

    static void ValidateInputOwnership(FontInputOwnership value)
    {
        if (value != FontInputOwnership.Copy &&
            value != FontInputOwnership.RetainReadOnlyMemory)
        {
            throw new System.ArgumentOutOfRangeException("inputOwnership");
        }
    }

    static void ValidateRepairPolicy(FontRepairPolicy value)
    {
        if (value != FontRepairPolicy.None && value != FontRepairPolicy.Safe)
        {
            throw new System.ArgumentOutOfRangeException("repairPolicy");
        }
    }

    static void ValidateChecksumPolicy(FontChecksumPolicy value)
    {
        if (value != FontChecksumPolicy.RequireValid &&
            value != FontChecksumPolicy.ReportMismatch &&
            value != FontChecksumPolicy.Ignore)
        {
            throw new System.ArgumentOutOfRangeException("checksumPolicy");
        }
    }
}
