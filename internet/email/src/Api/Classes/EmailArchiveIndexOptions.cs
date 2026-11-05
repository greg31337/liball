using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Controls how archive index operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailArchiveIndexOptions : IEmailArchiveIndexOptionsContract
{
    /// <summary>
    /// The default maximum selected header count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumSelectedHeaderCount = 100;
    /// <summary>
    /// The default maximum selected header characters used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumSelectedHeaderCharacters = 65536;
    List<string> selectedHeaderNames;
    EmailHashAlgorithm hashAlgorithm;
    long maximumHashBytes;
    bool includeSemanticHash;
    int maximumSelectedHeaderCount;
    int maximumSelectedHeaderCharacters;
    EmailMessageAnalysisOptions analysisOptions;
    EmailCanonicalComparisonOptions semanticOptions;
    /// <summary>
    /// Creates a new archive index options object.
    /// </summary>
    public EmailArchiveIndexOptions()
    {
        selectedHeaderNames = new List<string>();
        AddSelectedHeaderName("Date");
        AddSelectedHeaderName("From");
        AddSelectedHeaderName("To");
        AddSelectedHeaderName("Cc");
        AddSelectedHeaderName("Subject");
        AddSelectedHeaderName("Message-ID");
        hashAlgorithm = EmailHashAlgorithm.Sha256;
        maximumHashBytes = EmailParsingLimitsConstants.DefaultMaxMessageBytes;
        includeSemanticHash = true;
        maximumSelectedHeaderCount = DefaultMaximumSelectedHeaderCount;
        maximumSelectedHeaderCharacters = DefaultMaximumSelectedHeaderCharacters;
        analysisOptions = new EmailMessageAnalysisOptions();
        semanticOptions = new EmailCanonicalComparisonOptions();
    }

    /// <summary>
    /// Gets the selected header name count.
    /// </summary>
    /// <returns>The current selected header name count.</returns>
    public int GetSelectedHeaderNameCount()
    {
        return selectedHeaderNames.Count;
    }

    /// <summary>
    /// Gets the selected header name at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested selected header name.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public string GetSelectedHeaderNameAt(int index)
    {
        if (index < 0 || index >= selectedHeaderNames.Count)
            throw new ArgumentOutOfRangeException("index");
        return selectedHeaderNames[index];
    }

    /// <summary>
    /// Gets the selected header names.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current selected header names.</returns>
    public List<string> GetSelectedHeaderNames()
    {
        return new List<string>(selectedHeaderNames);
    }

    /// <summary>
    /// Adds a selected header name to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void AddSelectedHeaderName(string value)
    {
        ValidateHeaderName(value);
        if (!ContainsSelectedHeaderName(value))
            selectedHeaderNames.Add(value);
    }

    /// <summary>
    /// Removes the matching selected header name value or values.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool RemoveSelectedHeaderName(string value)
    {
        ValidateHeaderName(value);
        int index = 0;
        while (index < selectedHeaderNames.Count)
        {
            if (string.Equals(selectedHeaderNames[index], value, StringComparison.OrdinalIgnoreCase))
            {
                selectedHeaderNames.RemoveAt(index);
                return true;
            }

            index++;
        }

        return false;
    }

    /// <summary>
    /// Clears the stored selected header names value or values.
    /// </summary>
    public void ClearSelectedHeaderNames()
    {
        selectedHeaderNames.Clear();
    }

    /// <summary>
    /// Determines whether the collection contains selected header name.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool ContainsSelectedHeaderName(string value)
    {
        if (value == null)
            return false;
        int index = 0;
        while (index < selectedHeaderNames.Count)
        {
            if (string.Equals(selectedHeaderNames[index], value, StringComparison.OrdinalIgnoreCase))
                return true;
            index++;
        }

        return false;
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
            throw new ArgumentOutOfRangeException("value");
        hashAlgorithm = value;
    }

    /// <summary>
    /// Gets the maximum hash bytes.
    /// </summary>
    /// <returns>The current maximum hash bytes.</returns>
    public long GetMaximumHashBytes()
    {
        return maximumHashBytes;
    }

    /// <summary>
    /// Sets the maximum hash bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumHashBytes(long value)
    {
        maximumHashBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt64AndString(value, "value");
    }

    /// <summary>
    /// Gets the include semantic hash.
    /// </summary>
    /// <returns><c>true</c> when the include semantic hash setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetIncludeSemanticHash()
    {
        return includeSemanticHash;
    }

    /// <summary>
    /// Sets the include semantic hash used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include semantic hash behavior.</param>
    public void SetIncludeSemanticHash(bool value)
    {
        includeSemanticHash = value;
    }

    /// <summary>
    /// Gets the maximum selected header count.
    /// </summary>
    /// <returns>The current maximum selected header count.</returns>
    public int GetMaximumSelectedHeaderCount()
    {
        return maximumSelectedHeaderCount;
    }

    /// <summary>
    /// Sets the maximum selected header count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumSelectedHeaderCount(int value)
    {
        maximumSelectedHeaderCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum selected header characters.
    /// </summary>
    /// <returns>The current maximum selected header characters.</returns>
    public int GetMaximumSelectedHeaderCharacters()
    {
        return maximumSelectedHeaderCharacters;
    }

    /// <summary>
    /// Sets the maximum selected header characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumSelectedHeaderCharacters(int value)
    {
        maximumSelectedHeaderCharacters = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the analysis options.
    /// </summary>
    /// <returns>The current analysis options.</returns>
    public EmailMessageAnalysisOptions GetAnalysisOptions()
    {
        return analysisOptions.Clone();
    }

    /// <summary>
    /// Sets the analysis options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetAnalysisOptions(EmailMessageAnalysisOptions value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        analysisOptions = value.Clone();
    }

    /// <summary>
    /// Gets the semantic options.
    /// </summary>
    /// <returns>The current semantic options.</returns>
    public EmailCanonicalComparisonOptions GetSemanticOptions()
    {
        return semanticOptions.Clone();
    }

    /// <summary>
    /// Sets the semantic options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetSemanticOptions(EmailCanonicalComparisonOptions value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        semanticOptions = value.Clone();
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailArchiveIndexOptions Clone()
    {
        EmailArchiveIndexOptions copy = new EmailArchiveIndexOptions();
        copy.ClearSelectedHeaderNames();
        int index = 0;
        while (index < selectedHeaderNames.Count)
        {
            copy.AddSelectedHeaderName(selectedHeaderNames[index]);
            index++;
        }

        copy.SetHashAlgorithm(hashAlgorithm);
        copy.SetMaximumHashBytes(maximumHashBytes);
        copy.SetIncludeSemanticHash(includeSemanticHash);
        copy.SetMaximumSelectedHeaderCount(maximumSelectedHeaderCount);
        copy.SetMaximumSelectedHeaderCharacters(maximumSelectedHeaderCharacters);
        copy.SetAnalysisOptions(analysisOptions);
        copy.SetSemanticOptions(semanticOptions);
        return copy;
    }

    static void ValidateHeaderName(string value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        if (value.Length == 0)
            throw new ArgumentException("A selected header name cannot be empty.", "value");
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current < 33 || current > 126 || current == ':')
                throw new ArgumentException("A selected header name contains an invalid character.", "value");
            index++;
        }
    }
}
