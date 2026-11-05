using System;

namespace email;
/// <summary>
/// Controls how generation operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailGenerationOptions : IEmailGenerationOptionsContract
{
    bool generateDate;
    bool generateMessageId;
    bool generateContentIds;
    bool generateMimeVersion;
    bool generateBoundaries;
    bool deterministicOutput;
    string identifierDomain;
    byte[] deterministicSeed;
    EmailDateTime? generationDate;
    int maximumDepth;
    int maximumEntityCount;
    /// <summary>
    /// Creates a new generation options object.
    /// </summary>
    public EmailGenerationOptions()
    {
        identifierDomain = "local.invalid";
        deterministicSeed = new byte[0];
        generationDate = null;
        maximumDepth = EmailTraversalConstants.DefaultMaximumDepth;
        maximumEntityCount = EmailTraversalConstants.DefaultMaximumEntityCount;
    }

    /// <summary>
    /// Gets the generate date.
    /// </summary>
    /// <returns><c>true</c> when the generate date setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetGenerateDate()
    {
        return generateDate;
    }

    /// <summary>
    /// Sets the generate date used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the generate date behavior.</param>
    public void SetGenerateDate(bool value)
    {
        generateDate = value;
    }

    /// <summary>
    /// Gets the generate message ID.
    /// </summary>
    /// <returns><c>true</c> when the generate message ID setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetGenerateMessageId()
    {
        return generateMessageId;
    }

    /// <summary>
    /// Sets the generate message ID used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the generate message ID behavior.</param>
    public void SetGenerateMessageId(bool value)
    {
        generateMessageId = value;
    }

    /// <summary>
    /// Gets the generate content ids.
    /// </summary>
    /// <returns><c>true</c> when the generate content ids setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetGenerateContentIds()
    {
        return generateContentIds;
    }

    /// <summary>
    /// Sets the generate content ids used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the generate content ids behavior.</param>
    public void SetGenerateContentIds(bool value)
    {
        generateContentIds = value;
    }

    /// <summary>
    /// Gets the generate MIME version.
    /// </summary>
    /// <returns><c>true</c> when the generate MIME version setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetGenerateMimeVersion()
    {
        return generateMimeVersion;
    }

    /// <summary>
    /// Sets the generate MIME version used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the generate MIME version behavior.</param>
    public void SetGenerateMimeVersion(bool value)
    {
        generateMimeVersion = value;
    }

    /// <summary>
    /// Gets the generate boundaries.
    /// </summary>
    /// <returns><c>true</c> when the generate boundaries setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetGenerateBoundaries()
    {
        return generateBoundaries;
    }

    /// <summary>
    /// Sets the generate boundaries used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the generate boundaries behavior.</param>
    public void SetGenerateBoundaries(bool value)
    {
        generateBoundaries = value;
    }

    /// <summary>
    /// Gets the deterministic output.
    /// </summary>
    /// <returns><c>true</c> when the deterministic output setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetDeterministicOutput()
    {
        return deterministicOutput;
    }

    /// <summary>
    /// Sets the deterministic output used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the deterministic output behavior.</param>
    public void SetDeterministicOutput(bool value)
    {
        deterministicOutput = value;
    }

    /// <summary>
    /// Gets the identifier domain.
    /// </summary>
    /// <returns>The current identifier domain.</returns>
    public string GetIdentifierDomain()
    {
        return identifierDomain;
    }

    /// <summary>
    /// Sets the identifier domain used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetIdentifierDomain(string value)
    {
        ValidateDomain(value);
        identifierDomain = value.ToLowerInvariant();
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
    /// Determines whether the generation date is present or available.
    /// </summary>
    /// <returns><c>true</c> when the generation date is available; otherwise, <c>false</c>.</returns>
    public bool HasGenerationDate()
    {
        return generationDate != null;
    }

    /// <summary>
    /// Gets the generation date.
    /// </summary>
    /// <returns>The current generation date.</returns>
    public EmailDateTime? GetGenerationDate()
    {
        return generationDate;
    }

    /// <summary>
    /// Sets the generation date used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetGenerationDate(EmailDateTime value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        generationDate = value;
    }

    /// <summary>
    /// Clears the stored generation date value or values.
    /// </summary>
    public void ClearGenerationDate()
    {
        generationDate = null;
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
    public EmailGenerationOptions Clone()
    {
        EmailGenerationOptions copy = new EmailGenerationOptions();
        copy.SetGenerateDate(generateDate);
        copy.SetGenerateMessageId(generateMessageId);
        copy.SetGenerateContentIds(generateContentIds);
        copy.SetGenerateMimeVersion(generateMimeVersion);
        copy.SetGenerateBoundaries(generateBoundaries);
        copy.SetDeterministicOutput(deterministicOutput);
        copy.SetIdentifierDomain(identifierDomain);
        copy.SetDeterministicSeed(deterministicSeed);
        if (generationDate != null)
        {
            copy.SetGenerationDate(generationDate);
        }

        copy.SetMaximumDepth(maximumDepth);
        copy.SetMaximumEntityCount(maximumEntityCount);
        return copy;
    }

    internal static EmailGenerationOptions CreateAllImplementation()
    {
        EmailGenerationOptions options = new EmailGenerationOptions();
        options.SetGenerateDate(true);
        options.SetGenerateMessageId(true);
        options.SetGenerateContentIds(true);
        options.SetGenerateMimeVersion(true);
        options.SetGenerateBoundaries(true);
        return options;
    }

    static void ValidateDomain(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.Length == 0 || value.Length > 253 || value[0] == '.' || value[value.Length - 1] == '.')
        {
            throw new ArgumentException("An identifier domain is required.", "value");
        }

        int index = 0;
        bool labelStart = true;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '.')
            {
                if (labelStart || value[index - 1] == '-')
                {
                    throw new ArgumentException("The identifier domain contains an invalid label.", "value");
                }

                labelStart = true;
            }
            else
            {
                bool alphaNumeric = current >= 'a' && current <= 'z' || current >= 'A' && current <= 'Z' || current >= '0' && current <= '9';
                if (!alphaNumeric && current != '-' || labelStart && current == '-')
                {
                    throw new ArgumentException("The identifier domain contains an invalid character.", "value");
                }

                labelStart = false;
            }

            index++;
        }

        if (labelStart || value[value.Length - 1] == '-')
        {
            throw new ArgumentException("The identifier domain contains an invalid label.", "value");
        }
    }
}
