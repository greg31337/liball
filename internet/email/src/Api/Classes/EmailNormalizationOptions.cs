using System;

namespace email;
/// <summary>
/// Controls how normalization operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailNormalizationOptions : IEmailNormalizationOptionsContract
{
    bool normalizeLineEndings;
    bool refoldHeaders;
    bool repairMimeVersion;
    bool repairContentTypes;
    bool repairTransferEncodings;
    bool repairBoundaries;
    bool normalizeCharsets;
    bool normalizeFilenames;
    int preferredHeaderLineLength;
    long maximumContentBytes;
    byte[] deterministicBoundarySeed;
    /// <summary>
    /// Creates a new normalization options object.
    /// </summary>
    public EmailNormalizationOptions()
    {
        preferredHeaderLineLength = 78;
        maximumContentBytes = EmailDecodingLimitsConstants.DefaultMaxDecodedBytes;
        deterministicBoundarySeed = new byte[0];
    }

    /// <summary>
    /// Gets the normalize line endings.
    /// </summary>
    /// <returns><c>true</c> when the normalize line endings setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetNormalizeLineEndings()
    {
        return normalizeLineEndings;
    }

    /// <summary>
    /// Sets the normalize line endings used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the normalize line endings behavior.</param>
    public void SetNormalizeLineEndings(bool value)
    {
        normalizeLineEndings = value;
    }

    /// <summary>
    /// Gets the refold headers.
    /// </summary>
    /// <returns><c>true</c> when the refold headers setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRefoldHeaders()
    {
        return refoldHeaders;
    }

    /// <summary>
    /// Sets the refold headers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the refold headers behavior.</param>
    public void SetRefoldHeaders(bool value)
    {
        refoldHeaders = value;
    }

    /// <summary>
    /// Gets the repair MIME version.
    /// </summary>
    /// <returns><c>true</c> when the repair MIME version setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRepairMimeVersion()
    {
        return repairMimeVersion;
    }

    /// <summary>
    /// Sets the repair MIME version used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the repair MIME version behavior.</param>
    public void SetRepairMimeVersion(bool value)
    {
        repairMimeVersion = value;
    }

    /// <summary>
    /// Gets the repair content types.
    /// </summary>
    /// <returns><c>true</c> when the repair content types setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRepairContentTypes()
    {
        return repairContentTypes;
    }

    /// <summary>
    /// Sets the repair content types used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the repair content types behavior.</param>
    public void SetRepairContentTypes(bool value)
    {
        repairContentTypes = value;
    }

    /// <summary>
    /// Gets the repair transfer encodings.
    /// </summary>
    /// <returns><c>true</c> when the repair transfer encodings setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRepairTransferEncodings()
    {
        return repairTransferEncodings;
    }

    /// <summary>
    /// Sets the repair transfer encodings used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the repair transfer encodings behavior.</param>
    public void SetRepairTransferEncodings(bool value)
    {
        repairTransferEncodings = value;
    }

    /// <summary>
    /// Gets the repair boundaries.
    /// </summary>
    /// <returns><c>true</c> when the repair boundaries setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRepairBoundaries()
    {
        return repairBoundaries;
    }

    /// <summary>
    /// Sets the repair boundaries used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the repair boundaries behavior.</param>
    public void SetRepairBoundaries(bool value)
    {
        repairBoundaries = value;
    }

    /// <summary>
    /// Gets the normalize charsets.
    /// </summary>
    /// <returns><c>true</c> when the normalize charsets setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetNormalizeCharsets()
    {
        return normalizeCharsets;
    }

    /// <summary>
    /// Sets the normalize charsets used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the normalize charsets behavior.</param>
    public void SetNormalizeCharsets(bool value)
    {
        normalizeCharsets = value;
    }

    /// <summary>
    /// Gets the normalize filenames.
    /// </summary>
    /// <returns><c>true</c> when the normalize filenames setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetNormalizeFilenames()
    {
        return normalizeFilenames;
    }

    /// <summary>
    /// Sets the normalize filenames used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the normalize filenames behavior.</param>
    public void SetNormalizeFilenames(bool value)
    {
        normalizeFilenames = value;
    }

    /// <summary>
    /// Gets the preferred header line length.
    /// </summary>
    /// <returns>The current preferred header line length.</returns>
    public int GetPreferredHeaderLineLength()
    {
        return preferredHeaderLineLength;
    }

    /// <summary>
    /// Sets the preferred header line length used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetPreferredHeaderLineLength(int value)
    {
        if (value < 20 || value > 998)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        preferredHeaderLineLength = value;
    }

    /// <summary>
    /// Gets the maximum content bytes.
    /// </summary>
    /// <returns>The current maximum content bytes.</returns>
    public long GetMaximumContentBytes()
    {
        return maximumContentBytes;
    }

    /// <summary>
    /// Sets the maximum content bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumContentBytes(long value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumContentBytes = value;
    }

    /// <summary>
    /// Gets the deterministic boundary seed.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current deterministic boundary seed.</returns>
    public byte[] GetDeterministicBoundarySeed()
    {
        byte[] copy = new byte[deterministicBoundarySeed.Length];
        deterministicBoundarySeed.CopyTo(copy, 0);
        return copy;
    }

    /// <summary>
    /// Sets the deterministic boundary seed used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetDeterministicBoundarySeed(byte[] value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        deterministicBoundarySeed = new byte[value.Length];
        value.CopyTo(deterministicBoundarySeed, 0);
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailNormalizationOptions Clone()
    {
        EmailNormalizationOptions copy = new EmailNormalizationOptions();
        copy.SetNormalizeLineEndings(normalizeLineEndings);
        copy.SetRefoldHeaders(refoldHeaders);
        copy.SetRepairMimeVersion(repairMimeVersion);
        copy.SetRepairContentTypes(repairContentTypes);
        copy.SetRepairTransferEncodings(repairTransferEncodings);
        copy.SetRepairBoundaries(repairBoundaries);
        copy.SetNormalizeCharsets(normalizeCharsets);
        copy.SetNormalizeFilenames(normalizeFilenames);
        copy.SetPreferredHeaderLineLength(preferredHeaderLineLength);
        copy.SetMaximumContentBytes(maximumContentBytes);
        copy.SetDeterministicBoundarySeed(deterministicBoundarySeed);
        return copy;
    }

    internal static EmailNormalizationOptions CreateAllImplementation()
    {
        EmailNormalizationOptions options = new EmailNormalizationOptions();
        options.SetNormalizeLineEndings(true);
        options.SetRefoldHeaders(true);
        options.SetRepairMimeVersion(true);
        options.SetRepairContentTypes(true);
        options.SetRepairTransferEncodings(true);
        options.SetRepairBoundaries(true);
        options.SetNormalizeCharsets(true);
        options.SetNormalizeFilenames(true);
        return options;
    }
}
