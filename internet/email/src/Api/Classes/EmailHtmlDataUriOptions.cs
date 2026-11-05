using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Controls how HTML data URI operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailHtmlDataUriOptions : IEmailHtmlDataUriOptionsContract
{
    /// <summary>
    /// The default maximum media type characters used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumMediaTypeCharacters = 255;
    EmailHtmlLimits limits;
    List<string> allowedMediaTypes;
    int maximumMediaTypeCharacters;
    /// <summary>
    /// Creates a new HTML data URI options object.
    /// </summary>
    public EmailHtmlDataUriOptions()
    {
        limits = new EmailHtmlLimits();
        allowedMediaTypes = new List<string>
        {
            "image/png",
            "image/jpeg",
            "image/gif",
            "image/webp",
            "image/avif"
        };
        maximumMediaTypeCharacters = DefaultMaximumMediaTypeCharacters;
    }

    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    public EmailHtmlLimits GetLimits()
    {
        return limits.Clone();
    }

    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetLimits(EmailHtmlLimits value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        limits = value.Clone();
    }

    /// <summary>
    /// Gets the allowed media types.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current allowed media types.</returns>
    public List<string> GetAllowedMediaTypes()
    {
        return new List<string>(allowedMediaTypes);
    }

    /// <summary>
    /// Sets the allowed media types used by this instance.
    /// </summary>
    /// <param name = "values">The values used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetAllowedMediaTypes(List<string> values)
    {
        if (values == null)
            throw new ArgumentNullException("values");
        List<string> replacements = new List<string>();
        int index = 0;
        while (index < values.Count)
        {
            string normalized = NormalizeMediaType(values[index]);
            if (!replacements.Contains(normalized))
                replacements.Add(normalized);
            index++;
        }

        allowedMediaTypes = replacements;
    }

    /// <summary>
    /// Adds an allowed media type to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void AddAllowedMediaType(string value)
    {
        string normalized = NormalizeMediaType(value);
        if (!allowedMediaTypes.Contains(normalized))
            allowedMediaTypes.Add(normalized);
    }

    /// <summary>
    /// Gets the maximum media type characters.
    /// </summary>
    /// <returns>The current maximum media type characters.</returns>
    public int GetMaximumMediaTypeCharacters()
    {
        return maximumMediaTypeCharacters;
    }

    /// <summary>
    /// Sets the maximum media type characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumMediaTypeCharacters(int value)
    {
        if (value < 1)
            throw new ArgumentOutOfRangeException("value");
        maximumMediaTypeCharacters = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailHtmlDataUriOptions Clone()
    {
        EmailHtmlDataUriOptions copy = new EmailHtmlDataUriOptions();
        copy.SetLimits(limits);
        copy.SetAllowedMediaTypes(allowedMediaTypes);
        copy.SetMaximumMediaTypeCharacters(maximumMediaTypeCharacters);
        return copy;
    }

    static string NormalizeMediaType(string value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        string selected = value.Trim().ToLowerInvariant();
        int slash = selected.IndexOf('/');
        if (slash < 1 || slash != selected.LastIndexOf('/') || slash == selected.Length - 1)
            throw new ArgumentException("An allowed data-URI media type must contain one type/subtype pair.", "value");
        if (!IsToken(selected.Substring(0, slash)) || !IsToken(selected.Substring(slash + 1)))
            throw new ArgumentException("An allowed data-URI media type must contain MIME tokens.", "value");
        return selected;
    }

    static bool IsToken(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current < 33 || current > 126 || current == '(' || current == ')' || current == '<' || current == '>' || current == '@' || current == ',' || current == ';' || current == ':' || current == '\\' || current == '"' || current == '/' || current == '[' || current == ']' || current == '?' || current == '=')
                return false;
            index++;
        }

        return value.Length > 0;
    }
}
