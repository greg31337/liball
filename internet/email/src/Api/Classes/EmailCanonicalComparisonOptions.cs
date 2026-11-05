using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Controls how canonical comparison operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailCanonicalComparisonOptions : IEmailCanonicalComparisonOptionsContract
{
    EmailHashAlgorithm hashAlgorithm;
    bool ignoreReceivedHeaders;
    bool ignoreAuthenticationHeaders;
    bool ignoreDateHeaders;
    bool ignoreMessageIdentifiers;
    bool ignoreSubjectHeaders;
    bool ignoreHeaderOrder;
    List<string> ignoredHeaderNames;
    byte[] deterministicSeed;
    long maximumOutputBytes;
    int maximumDepth;
    int maximumEntityCount;
    /// <summary>
    /// Creates a new canonical comparison options object.
    /// </summary>
    public EmailCanonicalComparisonOptions()
    {
        hashAlgorithm = EmailHashAlgorithm.Sha256;
        ignoredHeaderNames = new List<string>();
        deterministicSeed = Encoding.ASCII.GetBytes("email-semantic-fingerprint-v1");
        maximumOutputBytes = EmailParsingLimitsConstants.DefaultMaxMessageBytes;
        maximumDepth = EmailTraversalConstants.DefaultMaximumDepth;
        maximumEntityCount = EmailTraversalConstants.DefaultMaximumEntityCount;
    }

    /// <summary>
    /// Gets the hash algorithm.
    /// </summary>
    /// <returns>The current hash algorithm.</returns>
    public EmailHashAlgorithm GetHashAlgorithm()
    {
        return hashAlgorithm;
    }

    /// <summary>
    /// Sets the hash algorithm used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetHashAlgorithm(EmailHashAlgorithm value)
    {
        if (value < EmailHashAlgorithm.Sha256 || value > EmailHashAlgorithm.Sha512)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        hashAlgorithm = value;
    }

    /// <summary>
    /// Gets the ignore received headers.
    /// </summary>
    /// <returns><c>true</c> when the ignore received headers setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetIgnoreReceivedHeaders()
    {
        return ignoreReceivedHeaders;
    }

    /// <summary>
    /// Sets the ignore received headers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the ignore received headers behavior.</param>
    public void SetIgnoreReceivedHeaders(bool value)
    {
        ignoreReceivedHeaders = value;
    }

    /// <summary>
    /// Gets the ignore authentication headers.
    /// </summary>
    /// <returns><c>true</c> when the ignore authentication headers setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetIgnoreAuthenticationHeaders()
    {
        return ignoreAuthenticationHeaders;
    }

    /// <summary>
    /// Sets the ignore authentication headers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the ignore authentication headers behavior.</param>
    public void SetIgnoreAuthenticationHeaders(bool value)
    {
        ignoreAuthenticationHeaders = value;
    }

    /// <summary>
    /// Gets the ignore date headers.
    /// </summary>
    /// <returns><c>true</c> when the ignore date headers setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetIgnoreDateHeaders()
    {
        return ignoreDateHeaders;
    }

    /// <summary>
    /// Sets the ignore date headers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the ignore date headers behavior.</param>
    public void SetIgnoreDateHeaders(bool value)
    {
        ignoreDateHeaders = value;
    }

    /// <summary>
    /// Gets the ignore message identifiers.
    /// </summary>
    /// <returns><c>true</c> when the ignore message identifiers setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetIgnoreMessageIdentifiers()
    {
        return ignoreMessageIdentifiers;
    }

    /// <summary>
    /// Sets the ignore message identifiers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the ignore message identifiers behavior.</param>
    public void SetIgnoreMessageIdentifiers(bool value)
    {
        ignoreMessageIdentifiers = value;
    }

    /// <summary>
    /// Gets the ignore subject headers.
    /// </summary>
    /// <returns><c>true</c> when the ignore subject headers setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetIgnoreSubjectHeaders()
    {
        return ignoreSubjectHeaders;
    }

    /// <summary>
    /// Sets the ignore subject headers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the ignore subject headers behavior.</param>
    public void SetIgnoreSubjectHeaders(bool value)
    {
        ignoreSubjectHeaders = value;
    }

    /// <summary>
    /// Gets the ignore header order.
    /// </summary>
    /// <returns><c>true</c> when the ignore header order setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetIgnoreHeaderOrder()
    {
        return ignoreHeaderOrder;
    }

    /// <summary>
    /// Sets the ignore header order used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the ignore header order behavior.</param>
    public void SetIgnoreHeaderOrder(bool value)
    {
        ignoreHeaderOrder = value;
    }

    /// <summary>
    /// Adds an ignored header name to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void AddIgnoredHeaderName(string value)
    {
        ValidateHeaderName(value);
        if (!ContainsIgnoredHeaderName(value))
        {
            ignoredHeaderNames.Add(value);
        }
    }

    /// <summary>
    /// Removes the matching ignored header name value or values.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool RemoveIgnoredHeaderName(string value)
    {
        ValidateHeaderName(value);
        int index = 0;
        while (index < ignoredHeaderNames.Count)
        {
            if (string.Equals(ignoredHeaderNames[index], value, StringComparison.OrdinalIgnoreCase))
            {
                ignoredHeaderNames.RemoveAt(index);
                return true;
            }

            index++;
        }

        return false;
    }

    /// <summary>
    /// Clears the stored ignored header names value or values.
    /// </summary>
    public void ClearIgnoredHeaderNames()
    {
        ignoredHeaderNames.Clear();
    }

    /// <summary>
    /// Gets the ignored header name count.
    /// </summary>
    /// <returns>The current ignored header name count.</returns>
    public int GetIgnoredHeaderNameCount()
    {
        return ignoredHeaderNames.Count;
    }

    /// <summary>
    /// Gets the ignored header name at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested ignored header name.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public string GetIgnoredHeaderNameAt(int index)
    {
        if (index < 0 || index >= ignoredHeaderNames.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return ignoredHeaderNames[index];
    }

    /// <summary>
    /// Gets the ignored header names.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current ignored header names.</returns>
    public List<string> GetIgnoredHeaderNames()
    {
        return new List<string>(ignoredHeaderNames);
    }

    /// <summary>
    /// Gets the deterministic seed.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current deterministic seed.</returns>
    public byte[] GetDeterministicSeed()
    {
        byte[] copy = new byte[deterministicSeed.Length];
        deterministicSeed.CopyTo(copy, 0);
        return copy;
    }

    /// <summary>
    /// Sets the deterministic seed used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetDeterministicSeed(byte[] value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        deterministicSeed = new byte[value.Length];
        value.CopyTo(deterministicSeed, 0);
    }

    /// <summary>
    /// Gets the maximum output bytes.
    /// </summary>
    /// <returns>The current maximum output bytes.</returns>
    public long GetMaximumOutputBytes()
    {
        return maximumOutputBytes;
    }

    /// <summary>
    /// Sets the maximum output bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumOutputBytes(long value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumOutputBytes = value;
    }

    /// <summary>
    /// Gets the maximum depth.
    /// </summary>
    /// <returns>The current maximum depth.</returns>
    public int GetMaximumDepth()
    {
        return maximumDepth;
    }

    /// <summary>
    /// Sets the maximum depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumDepth(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumDepth = value;
    }

    /// <summary>
    /// Gets the maximum entity count.
    /// </summary>
    /// <returns>The current maximum entity count.</returns>
    public int GetMaximumEntityCount()
    {
        return maximumEntityCount;
    }

    /// <summary>
    /// Sets the maximum entity count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumEntityCount(int value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumEntityCount = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailCanonicalComparisonOptions Clone()
    {
        EmailCanonicalComparisonOptions copy = new EmailCanonicalComparisonOptions();
        copy.SetHashAlgorithm(hashAlgorithm);
        copy.SetIgnoreReceivedHeaders(ignoreReceivedHeaders);
        copy.SetIgnoreAuthenticationHeaders(ignoreAuthenticationHeaders);
        copy.SetIgnoreDateHeaders(ignoreDateHeaders);
        copy.SetIgnoreMessageIdentifiers(ignoreMessageIdentifiers);
        copy.SetIgnoreSubjectHeaders(ignoreSubjectHeaders);
        copy.SetIgnoreHeaderOrder(ignoreHeaderOrder);
        int index = 0;
        while (index < ignoredHeaderNames.Count)
        {
            copy.AddIgnoredHeaderName(ignoredHeaderNames[index]);
            index++;
        }

        copy.SetDeterministicSeed(deterministicSeed);
        copy.SetMaximumOutputBytes(maximumOutputBytes);
        copy.SetMaximumDepth(maximumDepth);
        copy.SetMaximumEntityCount(maximumEntityCount);
        return copy;
    }

    /// <summary>
    /// Determines whether the collection contains ignored header name.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool ContainsIgnoredHeaderName(string name)
    {
        if (name == null)
        {
            return false;
        }

        int index = 0;
        while (index < ignoredHeaderNames.Count)
        {
            if (string.Equals(ignoredHeaderNames[index], name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static void ValidateHeaderName(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("An ignored header name is required.", "value");
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current < 33 || current > 126 || current == ':')
            {
                throw new ArgumentException("An ignored header name contains an invalid character.", "value");
            }

            index++;
        }
    }
}
