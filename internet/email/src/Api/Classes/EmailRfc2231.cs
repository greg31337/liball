using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Provides operations for working with rfc 2231.
/// </summary>
public class EmailRfc2231 : IEmailRfc2231Contract
{
    /// <summary>
    /// The default maximum segments used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumSegments = 128;
    /// <summary>
    /// The default maximum decoded bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaximumDecodedBytes = 1048576;
    internal static EmailRfc2231EncodeResult EncodeImplementation(string name, ReadOnlySpan<byte> value, EmailRfc2231GenerationOptions options)
    {
        return new EmailRfc2231Generator().Encode(name, value, options);
    }

    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailRfc2231EncodeResult Encode(string name, ReadOnlySpan<byte> value, EmailRfc2231GenerationOptions options)
    {
        return EmailRfc2231.EncodeImplementation(name, value, options);
    }

    internal static EmailRfc2231EncodeResult EncodeTextImplementation(string name, string value, System.Text.Encoding characterEncoding, EmailRfc2231GenerationOptions options)
    {
        return new EmailRfc2231Generator().EncodeText(name, value, characterEncoding, options);
    }

    /// <summary>
    /// Encodes the text.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "characterEncoding">The character encoding used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailRfc2231EncodeResult EncodeText(string name, string value, System.Text.Encoding characterEncoding, EmailRfc2231GenerationOptions options)
    {
        return EmailRfc2231.EncodeTextImplementation(name, value, characterEncoding, options);
    }

    internal static EmailRfc2231DecodeResult DecodeImplementationWithParametersAndMode(List<EmailRfc2231Parameter> parameters, EmailRfc2231DecodeMode mode)
    {
        return DecodeImplementationWithParametersAndModeAndMaximumSegmentsAndMaximumDecodedBytes(parameters, mode, DefaultMaximumSegments, DefaultMaximumDecodedBytes);
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "parameters">The parameters used by the operation.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailRfc2231DecodeResult DecodeWithParametersAndMode(List<EmailRfc2231Parameter> parameters, EmailRfc2231DecodeMode mode)
    {
        return EmailRfc2231.DecodeImplementationWithParametersAndMode(parameters, mode);
    }

    internal static EmailRfc2231DecodeResult DecodeImplementationWithParametersAndModeAndMaximumSegmentsAndMaximumDecodedBytes(List<EmailRfc2231Parameter> parameters, EmailRfc2231DecodeMode mode, int maximumSegments, long maximumDecodedBytes)
    {
        ValidateArguments(parameters, mode, maximumSegments, maximumDecodedBytes);
        EmailIssueList issues = new EmailIssueList();
        List<EmailRfc2231Group> groups = CreateGroups(parameters, mode, issues);
        List<EmailRfc2231DecodedParameter> decoded = new List<EmailRfc2231DecodedParameter>();
        long decodedByteCount = 0;
        int index = 0;
        while (index < groups.Count)
        {
            EmailRfc2231Group group = groups[index];
            AddGroupIssues(group, mode, issues);
            long remaining = maximumDecodedBytes - decodedByteCount;
            EmailRfc2231ResolvedValue? value = ResolveGroup(group, mode, maximumSegments, remaining, issues);
            if (value != null)
            {
                byte[] bytes = value.GetBytes();
                decoded.Add(new EmailRfc2231DecodedParameter(group.GetName(), bytes, value.GetCharset(), value.HasCharset(), value.GetLanguage(), value.HasLanguage(), value.IsExtended(), value.IsContinued(), value.GetSegmentCount(), group.GetSourceParameters()));
                decodedByteCount = new EmailSafeArithmetic().AddNonNegative(decodedByteCount, bytes.Length);
                if (value.HasFatalError())
                {
                    break;
                }
            }

            index++;
        }

        return new EmailRfc2231DecodeResult(decoded, parameters, issues, decodedByteCount);
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "parameters">The parameters used by the operation.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <param name = "maximumSegments">The maximum segments used by the operation.</param>
    /// <param name = "maximumDecodedBytes">The source bytes or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailRfc2231DecodeResult DecodeWithParametersAndModeAndMaximumSegmentsAndMaximumDecodedBytes(List<EmailRfc2231Parameter> parameters, EmailRfc2231DecodeMode mode, int maximumSegments, long maximumDecodedBytes)
    {
        return EmailRfc2231.DecodeImplementationWithParametersAndModeAndMaximumSegmentsAndMaximumDecodedBytes(parameters, mode, maximumSegments, maximumDecodedBytes);
    }

    static void ValidateArguments(List<EmailRfc2231Parameter> parameters, EmailRfc2231DecodeMode mode, int maximumSegments, long maximumDecodedBytes)
    {
        if (parameters == null)
        {
            throw new ArgumentNullException("parameters");
        }

        if (mode < EmailRfc2231DecodeMode.Strict || mode > EmailRfc2231DecodeMode.Tolerant)
        {
            throw new ArgumentOutOfRangeException("mode");
        }

        if (maximumSegments < 1)
        {
            throw new ArgumentOutOfRangeException("maximumSegments");
        }

        if (maximumDecodedBytes < 0)
        {
            throw new ArgumentOutOfRangeException("maximumDecodedBytes");
        }

        int index = 0;
        while (index < parameters.Count)
        {
            if (parameters[index] == null)
            {
                throw new ArgumentException("RFC 2231 parameter lists cannot contain null.", "parameters");
            }

            index++;
        }
    }

    static List<EmailRfc2231Group> CreateGroups(List<EmailRfc2231Parameter> parameters, EmailRfc2231DecodeMode mode, EmailIssueList issues)
    {
        List<EmailRfc2231Group> groups = new List<EmailRfc2231Group>();
        Dictionary<string, EmailRfc2231Group> lookup = new Dictionary<string, EmailRfc2231Group>(StringComparer.OrdinalIgnoreCase);
        int index = 0;
        while (index < parameters.Count)
        {
            EmailRfc2231Name name = ParseName(parameters[index]);
            if (name.GetKind() == EmailRfc2231NameKind.Malformed)
            {
                AddIssue(issues, mode, EmailIssueCode.Rfc2231MalformedName, "A parameter name does not use valid RFC 2231 attribute or section syntax.", parameters[index].GetSourcePosition(), "The exact name was treated as a regular parameter name.");
            }
            else if (name.HasLeadingZero())
            {
                AddIssue(issues, mode, EmailIssueCode.Rfc2231MalformedName, "A continuation section number contains a forbidden leading zero.", parameters[index].GetSourcePosition(), "The decimal section number was used for recovery.");
            }

            string key = name.GetBaseName() ?? throw new InvalidOperationException();
            EmailRfc2231Group group;
            if (!lookup.ContainsKey(key))
            {
                group = new EmailRfc2231Group(key);
                lookup.Add(key, group);
                groups.Add(group);
            }
            else
            {
                group = lookup[key];
            }

            group.Add(name);
            index++;
        }

        return groups;
    }

    static EmailRfc2231Name ParseName(EmailRfc2231Parameter parameter)
    {
        string? name = parameter.GetName();
        int star = FindCharacter(name, '*');
        if (star < 0)
        {
            if (IsAttribute(name))
            {
                return new EmailRfc2231Name(parameter, name, EmailRfc2231NameKind.Regular, -1, false, false);
            }

            return new EmailRfc2231Name(parameter, name, EmailRfc2231NameKind.Malformed, -1, false, false);
        }

        string baseName = (name ?? throw new InvalidOperationException()).Substring(0, star);
        if (!IsAttribute(baseName))
        {
            return new EmailRfc2231Name(parameter, name, EmailRfc2231NameKind.Malformed, -1, false, false);
        }

        if (star == name.Length - 1)
        {
            return new EmailRfc2231Name(parameter, baseName, EmailRfc2231NameKind.Extended, -1, true, false);
        }

        int end = name.Length;
        bool encoded = false;
        if (name[end - 1] == '*')
        {
            encoded = true;
            end--;
        }

        if (end <= star + 1)
        {
            return new EmailRfc2231Name(parameter, name, EmailRfc2231NameKind.Malformed, -1, false, false);
        }

        bool leadingZero = end - star - 1 > 1 && name[star + 1] == '0';
        long section = 0;
        int index = star + 1;
        while (index < end)
        {
            char character = name[index];
            if (character < '0' || character > '9')
            {
                return new EmailRfc2231Name(parameter, name, EmailRfc2231NameKind.Malformed, -1, false, false);
            }

            section = section * 10 + character - '0';
            if (section > int.MaxValue)
            {
                return new EmailRfc2231Name(parameter, name, EmailRfc2231NameKind.Malformed, -1, false, false);
            }

            index++;
        }

        return new EmailRfc2231Name(parameter, baseName, EmailRfc2231NameKind.Continuation, (int)section, encoded, leadingZero);
    }

    static void AddGroupIssues(EmailRfc2231Group group, EmailRfc2231DecodeMode mode, EmailIssueList issues)
    {
        EmailSourcePosition? position = group.GetSourceParameters()[0].GetSourcePosition();
        if (group.GetCount(EmailRfc2231NameKind.Regular) > 1 || group.GetCount(EmailRfc2231NameKind.Extended) > 1 || group.GetCount(EmailRfc2231NameKind.Malformed) > 1)
        {
            AddIssue(issues, mode, EmailIssueCode.Rfc2231Duplicate, "A logical parameter has duplicate values of the same form.", position, "The first value of each duplicated form was used.");
        }

        if (group.GetCount(EmailRfc2231NameKind.Extended) > 0 && group.GetCount(EmailRfc2231NameKind.Continuation) > 0)
        {
            AddIssue(issues, mode, EmailIssueCode.Rfc2231ConflictingForms, "A logical parameter contains both a single extended value and continuation sections.", position, "A continuation beginning at section zero was preferred; otherwise the single extended value was used.");
        }

        List<EmailRfc2231Name> names = group.GetAll();
        int first = 0;
        while (first < names.Count)
        {
            if (names[first].GetKind() == EmailRfc2231NameKind.Continuation)
            {
                int second = first + 1;
                while (second < names.Count)
                {
                    if (names[second].GetKind() == EmailRfc2231NameKind.Continuation && names[second].GetSection() == names[first].GetSection())
                    {
                        AddIssue(issues, mode, EmailIssueCode.Rfc2231Duplicate, "A continuation section number appears more than once.", names[second].GetParameter().GetSourcePosition(), "The first occurrence of the section was used.");
                        break;
                    }

                    second++;
                }
            }

            first++;
        }

        if (group.GetCount(EmailRfc2231NameKind.Continuation) > 0 && group.GetContinuation(0) == null)
        {
            AddIssue(issues, mode, EmailIssueCode.Rfc2231ContinuationGap, "A continued parameter does not contain section zero.", position, "A complete single value or regular fallback was preferred; otherwise the lowest contiguous section run was used.");
        }
    }

    static EmailRfc2231ResolvedValue? ResolveGroup(EmailRfc2231Group group, EmailRfc2231DecodeMode mode, int maximumSegments, long maximumDecodedBytes, EmailIssueList issues)
    {
        EmailRfc2231Name? selected;
        if (group.GetContinuation(0) != null)
        {
            return ResolveContinuation(group, 0, mode, maximumSegments, maximumDecodedBytes, issues);
        }

        selected = group.GetFirst(EmailRfc2231NameKind.Extended);
        if (selected != null)
        {
            return ResolveSingle(selected, true, mode, maximumDecodedBytes, issues);
        }

        selected = group.GetFirst(EmailRfc2231NameKind.Regular);
        if (selected != null)
        {
            return ResolveSingle(selected, false, mode, maximumDecodedBytes, issues);
        }

        selected = group.GetFirst(EmailRfc2231NameKind.Malformed);
        if (selected != null)
        {
            return ResolveSingle(selected, false, mode, maximumDecodedBytes, issues);
        }

        int lowest = group.GetLowestContinuationSection();
        if (lowest >= 0)
        {
            return ResolveContinuation(group, lowest, mode, maximumSegments, maximumDecodedBytes, issues);
        }

        return null;
    }

    static EmailRfc2231ResolvedValue ResolveSingle(EmailRfc2231Name name, bool extended, EmailRfc2231DecodeMode mode, long maximumDecodedBytes, EmailIssueList issues)
    {
        byte[] raw = name.GetParameter().GetValueBytes();
        if (!extended)
        {
            List<byte> regular = new List<byte>();
            bool fatal = AppendRaw(regular, raw, maximumDecodedBytes, issues, name.GetParameter().GetSourcePosition());
            return new EmailRfc2231ResolvedValue(regular.ToArray(), null, false, null, false, false, false, 1, fatal);
        }

        return DecodeExtendedValue(raw, name.GetParameter().GetSourcePosition(), mode, maximumDecodedBytes, false, 1, issues);
    }

    static EmailRfc2231ResolvedValue ResolveContinuation(EmailRfc2231Group group, int startSection, EmailRfc2231DecodeMode mode, int maximumSegments, long maximumDecodedBytes, EmailIssueList issues)
    {
        List<byte> output = new List<byte>();
        string? charset = null;
        bool charsetPresent = false;
        string? language = null;
        bool languagePresent = false;
        bool extended = false;
        bool fatal = false;
        int segmentCount = 0;
        int highest = group.GetHighestContinuationSection();
        int section = startSection;
        while (section <= highest)
        {
            EmailRfc2231Name? name = group.GetContinuation(section);
            if (name == null)
            {
                AddIssue(issues, mode, EmailIssueCode.Rfc2231ContinuationGap, "A continued parameter contains a gap in its section numbers.", group.GetSourceParameters()[0].GetSourcePosition(), "Only the contiguous prefix before the gap was decoded.");
                break;
            }

            if (segmentCount >= maximumSegments)
            {
                issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.Rfc2231SegmentLimitExceeded, "A continued parameter exceeds the configured segment limit.", name.GetParameter().GetSourcePosition(), null, null, "Only the segments within the configured limit were decoded."));
                fatal = true;
                break;
            }

            long remaining = maximumDecodedBytes - output.Count;
            byte[] raw = name.GetParameter().GetValueBytes();
            if (name.IsEncoded())
            {
                extended = true;
                if (segmentCount == 0 && section == 0)
                {
                    EmailRfc2231ResolvedValue first = DecodeExtendedValue(raw, name.GetParameter().GetSourcePosition(), mode, remaining, true, 1, issues);
                    AppendAll(output, first.GetBytes());
                    charset = first.GetCharset();
                    charsetPresent = first.HasCharset();
                    language = first.GetLanguage();
                    languagePresent = first.HasLanguage();
                    if (first.HasFatalError())
                    {
                        fatal = true;
                    }
                }
                else
                {
                    if (HasInvalidExtendedPayload(raw, 0))
                    {
                        AddIssue(issues, mode, EmailIssueCode.Rfc2231MalformedExtendedValue, "An encoded continuation contains bytes outside the RFC 2231 extended-value alphabet.", name.GetParameter().GetSourcePosition(), "The bytes were retained or percent-decoded for recovery.");
                    }

                    EmailPercentDecodeResult part = new EmailPercentEncoding().DecodeWithInputAndModeAndMaximumDecodedBytesAndInitialPosition(raw, mode, remaining, name.GetParameter().GetSourcePosition());
                    issues.AddRange(part.GetIssues());
                    AppendAll(output, part.GetDecodedBytes());
                    if (part.HasFatalError())
                    {
                        fatal = true;
                    }
                }
            }
            else
            {
                if (AppendRaw(output, raw, maximumDecodedBytes, issues, name.GetParameter().GetSourcePosition()))
                {
                    fatal = true;
                }
            }

            segmentCount++;
            if (fatal || section == int.MaxValue)
            {
                break;
            }

            section++;
        }

        return new EmailRfc2231ResolvedValue(output.ToArray(), charset, charsetPresent, language, languagePresent, extended, true, segmentCount, fatal);
    }

    static EmailRfc2231ResolvedValue DecodeExtendedValue(byte[] raw, EmailSourcePosition? position, EmailRfc2231DecodeMode mode, long maximumDecodedBytes, bool continued, int segmentCount, EmailIssueList issues)
    {
        int firstQuote = FindByte(raw, 0, 39);
        int secondQuote = -1;
        if (firstQuote >= 0)
        {
            secondQuote = FindByte(raw, firstQuote + 1, 39);
        }

        string? charset = null;
        bool charsetPresent = false;
        string? language = null;
        bool languagePresent = false;
        int payloadOffset = 0;
        if (firstQuote >= 0 && secondQuote >= 0)
        {
            charset = GetAscii(raw, 0, firstQuote);
            language = GetAscii(raw, firstQuote + 1, secondQuote - firstQuote - 1);
            charsetPresent = true;
            languagePresent = true;
            payloadOffset = secondQuote + 1;
            if (!IsCharset(charset) || !IsLanguage(language))
            {
                AddIssue(issues, mode, EmailIssueCode.Rfc2231MalformedExtendedValue, "An extended parameter has an invalid charset or language field.", position, "The declared metadata and recoverable value bytes were retained.");
            }
        }
        else
        {
            AddIssue(issues, mode, EmailIssueCode.Rfc2231MalformedExtendedValue, "An initial extended parameter value is missing charset or language delimiters.", position, "The entire value was percent-decoded without charset or language metadata.");
        }

        if (HasInvalidExtendedPayload(raw, payloadOffset))
        {
            AddIssue(issues, mode, EmailIssueCode.Rfc2231MalformedExtendedValue, "An extended parameter contains bytes outside the RFC 2231 extended-value alphabet.", ShiftPosition(position, raw, payloadOffset), "The bytes were retained or percent-decoded for recovery.");
        }

        EmailSourcePosition payloadPosition = ShiftPosition(position, raw, payloadOffset);
        EmailPercentDecodeResult decoded = new EmailPercentEncoding().DecodeWithInputAndModeAndMaximumDecodedBytesAndInitialPosition(new ReadOnlySpan<byte>(raw, payloadOffset, raw.Length - payloadOffset), mode, maximumDecodedBytes, payloadPosition);
        issues.AddRange(decoded.GetIssues());
        return new EmailRfc2231ResolvedValue(decoded.GetDecodedBytes(), charset, charsetPresent, language, languagePresent, true, continued, segmentCount, decoded.HasFatalError());
    }

    static bool AppendRaw(List<byte> output, byte[] value, long maximumDecodedBytes, EmailIssueList issues, EmailSourcePosition? position)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (output.Count < maximumDecodedBytes)
            {
                output.Add(value[index]);
            }
            else
            {
                EmailSourcePosition issuePosition = ShiftPosition(position, value, index);
                issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.Rfc2231DecodedByteLimitExceeded, "Decoded parameter content exceeds the configured byte limit.", issuePosition, null, null, "Only the prefix within the configured limit was retained."));
                return true;
            }

            index++;
        }

        return false;
    }

    static void AppendAll(List<byte> output, byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            output.Add(value[index]);
            index++;
        }
    }

    static bool HasInvalidExtendedPayload(byte[] value, int start)
    {
        int index = start;
        while (index < value.Length)
        {
            if (value[index] == 37)
            {
                index++;
                if (index < value.Length)
                {
                    index++;
                }

                if (index < value.Length)
                {
                    index++;
                }

                continue;
            }

            if (!IsAttributeCharacter(value[index]))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static bool IsAttribute(string? value)
    {
        if ((value ?? throw new InvalidOperationException()).Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character > 127 || !IsAttributeCharacter((byte)character))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool IsAttributeCharacter(byte value)
    {
        if (value < 33 || value > 126 || value == 42 || value == 39 || value == 37)
        {
            return false;
        }

        return value != 40 && value != 41 && value != 60 && value != 62 && value != 64 && value != 44 && value != 59 && value != 58 && value != 92 && value != 34 && value != 47 && value != 91 && value != 93 && value != 63 && value != 61;
    }

    static bool IsCharset(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < 33 || character > 126 || character == 39 || character == 37)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool IsLanguage(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool alphaNumeric = character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z' || character >= '0' && character <= '9';
            if (!alphaNumeric && character != '-')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static EmailSourcePosition ShiftPosition(EmailSourcePosition? position, byte[] value, int count)
    {
        EmailSourceTracker tracker = new EmailSourceTrackerOperations().CreateWithInitialPosition(position);
        if (count > 0)
        {
            tracker.Advance(new ReadOnlySpan<byte>(value, 0, count));
        }

        return tracker.GetPosition();
    }

    static string GetAscii(byte[] value, int start, int count)
    {
        char[] characters = new char[count];
        int index = 0;
        while (index < count)
        {
            characters[index] = (char)value[start + index];
            index++;
        }

        return new string (characters);
    }

    static int FindCharacter(string? value, char character)
    {
        int index = 0;
        while (index < (value ?? throw new InvalidOperationException()).Length)
        {
            if (value[index] == character)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    static int FindByte(byte[] value, int start, byte target)
    {
        int index = start;
        while (index < value.Length)
        {
            if (value[index] == target)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    static void AddIssue(EmailIssueList issues, EmailRfc2231DecodeMode mode, EmailIssueCode code, string description, EmailSourcePosition? position, string recovery)
    {
        EmailIssueSeverity severity = EmailIssueSeverity.Error;
        if (mode == EmailRfc2231DecodeMode.Tolerant)
        {
            severity = EmailIssueSeverity.Warning;
        }

        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, position, null, null, recovery));
    }
}
