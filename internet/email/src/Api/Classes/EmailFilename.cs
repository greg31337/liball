using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace email;
/// <summary>
/// Provides operations for working with filename.
/// </summary>
public class EmailFilename : IEmailFilenameContract
{
    internal static string? DecodeImplementation(MimeEntity? entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException("entity");
        }

        EmailRfc2231DecodedParameter? parameter = GetFilenameParameter(entity);
        if (parameter == null || parameter.GetDecodedByteCount() == 0)
        {
            return null;
        }

        byte[] bytes = parameter.GetDecodedBytes();
        if (parameter.HasCharset())
        {
            EmailCharsetLookupResult lookup = new EmailCharsetRegistry().Lookup(parameter.GetCharset());
            if (lookup.IsFound())
            {
                return (lookup.GetEncoding() ?? throw new InvalidOperationException()).GetString(bytes);
            }

            return Encoding.UTF8.GetString(bytes);
        }

        EmailHeaderDecodeResult decoded = EmailHeaderDecoder.Decode(bytes, EmailHeaderValueKind.Unstructured, EmailStructuredValueParseMode.Tolerant, new EmailSourcePosition(0, 1, 1), true);
        return decoded.GetValue();
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public string? Decode(MimeEntity? entity)
    {
        return EmailFilename.DecodeImplementation(entity);
    }

    internal static EmailFilenameResult SanitizeImplementationWithFilename(string? filename)
    {
        return SanitizeImplementationWithFilenameAndOptions(filename, new EmailFilenameOptions());
    }

    /// <summary>
    /// Sanitizes the supplied value.
    /// </summary>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <returns>The filename result produced by the operation.</returns>
    public EmailFilenameResult SanitizeWithFilename(string? filename)
    {
        return EmailFilename.SanitizeImplementationWithFilename(filename);
    }

    internal static EmailFilenameResult SanitizeImplementationWithFilenameAndOptions(string? filename, EmailFilenameOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return SanitizeCore(filename, options.Clone());
    }

    /// <summary>
    /// Sanitizes the supplied value.
    /// </summary>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The filename result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailFilenameResult SanitizeWithFilenameAndOptions(string? filename, EmailFilenameOptions options)
    {
        return EmailFilename.SanitizeImplementationWithFilenameAndOptions(filename, options);
    }

    internal static EmailFilenameResult SuggestImplementationWithFilenameAndExistingFilenames(string? filename, List<string> existingFilenames)
    {
        return SuggestImplementationWithFilenameAndExistingFilenamesAndOptions(filename, existingFilenames, new EmailFilenameOptions());
    }

    /// <summary>
    /// Suggests the supplied value.
    /// </summary>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <param name = "existingFilenames">The existing filenames used by the operation.</param>
    /// <returns>The filename result produced by the operation.</returns>
    public EmailFilenameResult SuggestWithFilenameAndExistingFilenames(string? filename, List<string> existingFilenames)
    {
        return EmailFilename.SuggestImplementationWithFilenameAndExistingFilenames(filename, existingFilenames);
    }

    internal static EmailFilenameResult SuggestImplementationWithFilenameAndExistingFilenamesAndOptions(string? filename, List<string> existingFilenames, EmailFilenameOptions options)
    {
        if (existingFilenames == null)
        {
            throw new ArgumentNullException("existingFilenames");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        ValidateExisting(existingFilenames);
        EmailFilenameOptions snapshot = options.Clone();
        EmailFilenameResult sanitized = SanitizeCore(filename, snapshot);
        string candidate = sanitized.GetFilename();
        if (!ContainsCollision(existingFilenames, candidate, snapshot))
        {
            return sanitized;
        }

        int collisionIndex = 2;
        while (collisionIndex <= existingFilenames.Count + 2)
        {
            string suffix = " (" + collisionIndex.ToString(CultureInfo.InvariantCulture) + ")";
            candidate = AddSuffix(sanitized.GetFilename(), suffix, snapshot.GetMaximumLength());
            if (!ContainsCollision(existingFilenames, candidate, snapshot))
            {
                bool suffixTruncated = sanitized.GetFilename().Length + suffix.Length > snapshot.GetMaximumLength();
                return new EmailFilenameResult(filename, candidate, sanitized.UsedFallback(), sanitized.WasPathStripped(), sanitized.WereCharactersRemoved(), sanitized.WereCharactersReplaced(), sanitized.WasUnicodeNormalized(), sanitized.WasReservedNameAdjusted(), sanitized.WasTruncated() || suffixTruncated, collisionIndex);
            }

            collisionIndex++;
        }

        throw new InvalidOperationException("A collision-free filename could not be generated.");
    }

    /// <summary>
    /// Suggests the supplied value.
    /// </summary>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <param name = "existingFilenames">The existing filenames used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The filename result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public EmailFilenameResult SuggestWithFilenameAndExistingFilenamesAndOptions(string? filename, List<string> existingFilenames, EmailFilenameOptions options)
    {
        return EmailFilename.SuggestImplementationWithFilenameAndExistingFilenamesAndOptions(filename, existingFilenames, options);
    }

    internal static bool IsForbiddenFilenameCharacterImplementation(char value)
    {
        return value == '<' || value == '>' || value == ':' || value == '"' || value == '/' || value == '\\' || value == '|' || value == '?' || value == '*';
    }

    /// <summary>
    /// Determines whether the current value or state is forbidden filename character.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is forbidden filename character; otherwise, <c>false</c>.</returns>
    public bool IsForbiddenFilenameCharacter(char value)
    {
        return EmailFilename.IsForbiddenFilenameCharacterImplementation(value);
    }

    static EmailFilenameResult SanitizeCore(string? filename, EmailFilenameOptions options)
    {
        string? original = filename;
        bool usedFallback = filename == null || filename.Length == 0;
        string? value = usedFallback ? options.GetFallbackFilename() : filename;
        int separator = LastPathSeparator(value);
        bool pathStripped = separator >= 0;
        if (pathStripped)
        {
            value = (value ?? throw new InvalidOperationException()).Substring(separator + 1);
        }

        EmailFilenameCleaned cleaned = CleanCharacters(value, options.GetReplacementCharacter());
        value = cleaned.GetValue();
        bool removed = cleaned.WereCharactersRemoved();
        bool replaced = cleaned.WereCharactersReplaced();
        bool normalized = false;
        if (options.GetNormalization() != EmailFilenameNormalization.None)
        {
            NormalizationForm form = options.GetNormalization() == EmailFilenameNormalization.Nfc ? NormalizationForm.FormC : NormalizationForm.FormKC;
            string normalizedValue = value.Normalize(form);
            normalized = !string.Equals(value, normalizedValue, StringComparison.Ordinal);
            value = normalizedValue;
            int normalizedSeparator = LastPathSeparator(value);
            if (normalizedSeparator >= 0)
            {
                pathStripped = true;
                value = value.Substring(normalizedSeparator + 1);
            }

            EmailFilenameCleaned normalizedCleaned = CleanCharacters(value, options.GetReplacementCharacter());
            value = normalizedCleaned.GetValue();
            removed = removed || normalizedCleaned.WereCharactersRemoved();
            replaced = replaced || normalizedCleaned.WereCharactersReplaced();
        }

        string trimmed = TrimUnsafeEdges(value);
        removed = removed || trimmed.Length != value.Length;
        value = trimmed;
        if (value.Length == 0 || value == "." || value == "..")
        {
            usedFallback = true;
            EmailFilenameCleaned fallback = CleanCharacters(options.GetFallbackFilename(), options.GetReplacementCharacter());
            value = TrimUnsafeEdges(fallback.GetValue());
            removed = removed || fallback.WereCharactersRemoved();
            if (value.Length == 0 || value == "." || value == "..")
            {
                value = "attachment";
            }

            if (options.GetNormalization() != EmailFilenameNormalization.None)
            {
                NormalizationForm fallbackForm = options.GetNormalization() == EmailFilenameNormalization.Nfc ? NormalizationForm.FormC : NormalizationForm.FormKC;
                string normalizedFallback = value.Normalize(fallbackForm);
                normalized = normalized || !string.Equals(value, normalizedFallback, StringComparison.Ordinal);
                value = normalizedFallback;
            }
        }

        bool reserved = IsReservedName(value);
        if (reserved)
        {
            value = options.GetReplacementCharacter() + value;
        }

        bool truncated = value.Length > options.GetMaximumLength();
        if (truncated)
        {
            value = TruncatePreservingExtension(value, options.GetMaximumLength());
        }

        return new EmailFilenameResult(original, value, usedFallback, pathStripped, removed, replaced, normalized, reserved, truncated, 0);
    }

    static EmailRfc2231DecodedParameter? GetFilenameParameter(MimeEntity entity)
    {
        EmailRfc2231DecodedParameter? parameter = null;
        if (entity.HasDisposition())
        {
            parameter = (entity.GetDisposition() ?? throw new InvalidOperationException()).GetParameters().GetDecoded("filename");
            if (parameter != null && parameter.GetDecodedByteCount() == 0)
            {
                parameter = null;
            }
        }

        if (parameter == null)
        {
            parameter = (entity.GetMediaType() ?? throw new InvalidOperationException()).GetParameters().GetDecoded("name");
        }

        return parameter;
    }

    static EmailFilenameCleaned CleanCharacters(string? value, char replacement)
    {
        StringBuilder builder = new StringBuilder();
        bool removed = false;
        bool replaced = false;
        int index = 0;
        while (index < (value ?? throw new InvalidOperationException()).Length)
        {
            char current = value[index];
            bool pair = char.IsHighSurrogate(current) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]);
            UnicodeCategory category = pair ? CharUnicodeInfo.GetUnicodeCategory(value, index) : char.GetUnicodeCategory(current);
            if (category == UnicodeCategory.Control || category == UnicodeCategory.Format || category == UnicodeCategory.Surrogate || category == UnicodeCategory.PrivateUse || category == UnicodeCategory.OtherNotAssigned || category == UnicodeCategory.LineSeparator || category == UnicodeCategory.ParagraphSeparator)
            {
                removed = true;
            }
            else if (IsForbiddenFilenameCharacterImplementation(current))
            {
                builder.Append(replacement);
                replaced = true;
            }
            else
            {
                builder.Append(current);
                if (pair)
                {
                    builder.Append(value[index + 1]);
                }
            }

            index += pair ? 2 : 1;
        }

        return new EmailFilenameCleaned(builder.ToString(), removed, replaced);
    }

    static string TrimUnsafeEdges(string value)
    {
        int start = 0;
        while (start < value.Length && char.IsWhiteSpace(value[start]))
        {
            start++;
        }

        int end = value.Length;
        while (end > start && (char.IsWhiteSpace(value[end - 1]) || value[end - 1] == '.'))
        {
            end--;
        }

        return value.Substring(start, end - start);
    }

    static int LastPathSeparator(string? value)
    {
        int selected = -1;
        int index = 0;
        while (index < (value ?? throw new InvalidOperationException()).Length)
        {
            if (value[index] == '/' || value[index] == '\\')
            {
                selected = index;
            }

            index++;
        }

        return selected;
    }

    static bool IsReservedName(string value)
    {
        int dot = value.IndexOf('.');
        string stem = dot < 0 ? value : value.Substring(0, dot);
        string upper = stem.ToUpperInvariant();
        if (upper == "CON" || upper == "PRN" || upper == "AUX" || upper == "NUL" || upper == "CLOCK$")
        {
            return true;
        }

        if (upper.Length == 4 && (upper.StartsWith("COM", StringComparison.Ordinal) || upper.StartsWith("LPT", StringComparison.Ordinal)))
        {
            return upper[3] >= '1' && upper[3] <= '9';
        }

        return false;
    }

    static string TruncatePreservingExtension(string value, int maximumLength)
    {
        int extensionIndex = GetExtensionIndex(value);
        if (extensionIndex > 0)
        {
            string extension = value.Substring(extensionIndex);
            if (extension.Length < maximumLength)
            {
                int baseLength = maximumLength - extension.Length;
                return SafePrefix(value, baseLength) + extension;
            }
        }

        return SafePrefix(value, maximumLength);
    }

    static string AddSuffix(string value, string suffix, int maximumLength)
    {
        int extensionIndex = GetExtensionIndex(value);
        string extension = extensionIndex > 0 ? value.Substring(extensionIndex) : "";
        string basename = extensionIndex > 0 ? value.Substring(0, extensionIndex) : value;
        int baseLength = maximumLength - suffix.Length - extension.Length;
        if (baseLength < 1)
        {
            extension = "";
            baseLength = maximumLength - suffix.Length;
        }

        return SafePrefix(basename, baseLength) + suffix + extension;
    }

    static int GetExtensionIndex(string value)
    {
        int index = value.LastIndexOf('.');
        return index > 0 && index + 1 < value.Length ? index : -1;
    }

    static string SafePrefix(string value, int length)
    {
        int selected = length < value.Length ? length : value.Length;
        if (selected > 0 && selected < value.Length && char.IsHighSurrogate(value[selected - 1]) && char.IsLowSurrogate(value[selected]))
        {
            selected--;
        }

        return value.Substring(0, selected);
    }

    static bool ContainsCollision(List<string> existingFilenames, string candidate, EmailFilenameOptions options)
    {
        StringComparison comparison = options.GetCaseInsensitiveCollisions() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        int index = 0;
        while (index < existingFilenames.Count)
        {
            string existing = SanitizeCore(existingFilenames[index], options).GetFilename();
            if (string.Equals(existing, candidate, comparison))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static void ValidateExisting(List<string> values)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("Existing filename lists cannot contain null entries.", "existingFilenames");
            }

            index++;
        }
    }
}
