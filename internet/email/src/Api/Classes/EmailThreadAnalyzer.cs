using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Analyzes thread and returns a structured result.
/// </summary>
public class EmailThreadAnalyzer : IEmailThreadAnalyzerContract
{
    internal static EmailThreadGraph BuildGraphImplementationWithMessages(List<EmailMessage> messages)
    {
        return BuildGraphImplementationWithMessagesAndOptions(messages, new EmailThreadOptions());
    }

    /// <summary>
    /// Builds the configured graph.
    /// </summary>
    /// <param name = "messages">The messages used by the operation.</param>
    /// <returns>The thread graph produced by the operation.</returns>
    public EmailThreadGraph BuildGraphWithMessages(List<EmailMessage> messages)
    {
        return EmailThreadAnalyzer.BuildGraphImplementationWithMessages(messages);
    }

    internal static EmailThreadGraph BuildGraphImplementationWithMessagesAndOptions(List<EmailMessage> messages, EmailThreadOptions options)
    {
        if (messages == null)
        {
            throw new ArgumentNullException("messages");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailThreadGraphBuilder builder = new EmailThreadGraphBuilder(options.Clone());
        return builder.Build(messages);
    }

    /// <summary>
    /// Builds the configured graph.
    /// </summary>
    /// <param name = "messages">The messages used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The thread graph produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailThreadGraph BuildGraphWithMessagesAndOptions(List<EmailMessage> messages, EmailThreadOptions options)
    {
        return EmailThreadAnalyzer.BuildGraphImplementationWithMessagesAndOptions(messages, options);
    }

    internal static EmailThreadIdentity ExtractImplementationWithMessage(EmailMessage message)
    {
        return ExtractImplementationWithMessageAndOptions(message, new EmailThreadOptions());
    }

    /// <summary>
    /// Extracts the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The thread identity produced by the operation.</returns>
    public EmailThreadIdentity ExtractWithMessage(EmailMessage message)
    {
        return EmailThreadAnalyzer.ExtractImplementationWithMessage(message);
    }

    internal static EmailThreadIdentity ExtractImplementationWithMessageAndOptions(EmailMessage message, EmailThreadOptions options)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailThreadOptions snapshot = options.Clone();
        EmailThreadExtractionBudget budget = new EmailThreadExtractionBudget(snapshot);
        List<EmailMessageId> messageIdentifiers = new List<EmailMessageId>();
        List<EmailMessageId> references = new List<EmailMessageId>();
        List<EmailMessageId> inReplyTo = new List<EmailMessageId>();
        EmailIssueList issues = new EmailIssueList();
        string? normalizedSubject = null;
        int index = 0;
        while (index < (message.GetHeaders() ?? throw new InvalidOperationException()).GetCount())
        {
            budget.AddHeader();
            EmailHeader header = (message.GetHeaders() ?? throw new InvalidOperationException()).GetAt(index);
            string name = header.GetName();
            if (IsIdentifierField(name))
            {
                byte[] value = header.GetUnfoldedValue();
                budget.AddIdentifierBytes(value.Length);
                EmailMessageIdParseResult parsed = ParseIdentifiers(name, value, header.GetValueSourcePosition(), snapshot);
                issues.AddRange(parsed.GetIssues());
                if (parsed.GetIssues().ContainsCode(EmailIssueCode.MessageIdLimitExceeded))
                {
                    throw new EmailLimitException("MaxThreadIdentifiers", snapshot.GetMaximumIdentifierCount(), (long)snapshot.GetMaximumIdentifierCount() + 1);
                }

                AddParsedIdentifiers(name, parsed.GetIdentifiers(), messageIdentifiers, references, inReplyTo, budget, snapshot);
            }
            else if (normalizedSubject == null && string.Equals(name, "Subject", StringComparison.OrdinalIgnoreCase))
            {
                budget.AddSubjectBytes(header.GetUnfoldedValue().Length);
                normalizedSubject = NormalizeSubjectImplementationWithValueAndOptions(header.GetDecodedValue(), snapshot);
                if (normalizedSubject.Length == 0)
                {
                    normalizedSubject = null;
                }
            }

            index++;
        }

        EmailMessageId? parent = GetParentCandidate(references, inReplyTo);
        EmailThreadIdentitySource source = GetIdentitySource(messageIdentifiers, references, inReplyTo, normalizedSubject, snapshot);
        string? threadKey = GetThreadKey(source, messageIdentifiers, references, inReplyTo, normalizedSubject);
        return new EmailThreadIdentity(messageIdentifiers, references, inReplyTo, parent, normalizedSubject, threadKey, source, issues);
    }

    /// <summary>
    /// Extracts the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The thread identity produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public EmailThreadIdentity ExtractWithMessageAndOptions(EmailMessage message, EmailThreadOptions options)
    {
        return EmailThreadAnalyzer.ExtractImplementationWithMessageAndOptions(message, options);
    }

    internal static string NormalizeSubjectImplementationWithValue(string value)
    {
        return NormalizeSubjectImplementationWithValueAndOptions(value, new EmailThreadOptions());
    }

    /// <summary>
    /// Normalizes the subject.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The string produced by the operation.</returns>
    public string NormalizeSubjectWithValue(string value)
    {
        return EmailThreadAnalyzer.NormalizeSubjectImplementationWithValue(value);
    }

    internal static string NormalizeSubjectImplementationWithValueAndOptions(string? value, EmailThreadOptions options)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailThreadOptions snapshot = options.Clone();
        if (value.Length > snapshot.GetMaximumSubjectCharacters())
        {
            throw new EmailLimitException("MaxThreadSubjectCharacters", snapshot.GetMaximumSubjectCharacters(), value.Length);
        }

        string selected = CollapseWhitespace(value.Normalize(NormalizationForm.FormKC));
        bool changed = true;
        while (changed && selected.Length > 0)
        {
            string previous = selected;
            selected = RemoveLeadingResponsePrefix(selected);
            selected = RemoveForwardWrapper(selected);
            selected = RemoveTrailingForwardMarker(selected);
            selected = CollapseWhitespace(selected);
            changed = !string.Equals(previous, selected, StringComparison.Ordinal);
        }

        selected = selected.ToLowerInvariant();
        if (selected.Length > snapshot.GetMaximumSubjectCharacters())
        {
            throw new EmailLimitException("MaxThreadSubjectCharacters", snapshot.GetMaximumSubjectCharacters(), selected.Length);
        }

        return selected;
    }

    /// <summary>
    /// Normalizes the subject.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public string NormalizeSubjectWithValueAndOptions(string? value, EmailThreadOptions options)
    {
        return EmailThreadAnalyzer.NormalizeSubjectImplementationWithValueAndOptions(value, options);
    }

    internal static string CanonicalizeIdentifierImplementation(EmailMessageId? identifier)
    {
        if (identifier == null)
        {
            throw new ArgumentNullException("identifier");
        }

        if (!new EmailMessageIdFormatter().FormatWithIdentifier(identifier).IsSuccess())
        {
            throw new ArgumentException("The message identifier cannot be represented safely.", "identifier");
        }

        string right = identifier.GetIdRight().ToLowerInvariant();
        if (identifier.IsRightDomainLiteral())
        {
            right = "[" + right + "]";
        }

        return "<" + identifier.GetIdLeft() + "@" + right + ">";
    }

    /// <summary>
    /// Determines whether the instance can onicalize identifier.
    /// </summary>
    /// <param name = "identifier">The identifier used by the operation.</param>
    /// <returns>The string produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public string CanonicalizeIdentifier(EmailMessageId? identifier)
    {
        return EmailThreadAnalyzer.CanonicalizeIdentifierImplementation(identifier);
    }

    static EmailMessageIdParseResult ParseIdentifiers(string name, byte[] value, EmailSourcePosition? position, EmailThreadOptions options)
    {
        EmailMessageIdParserOptions parserOptions = new EmailMessageIdParserOptions();
        parserOptions.SetMode(EmailMessageIdParseMode.Tolerant);
        parserOptions.SetMaximumIdentifiers(options.GetMaximumIdentifierCount());
        parserOptions.SetMaximumInputBytes(options.GetMaximumIdentifierBytes());
        if (string.Equals(name, "Message-ID", StringComparison.OrdinalIgnoreCase))
        {
            return new EmailMessageIdParser().ParseMessageIdWithInputAndOptionsAndInitialPosition(value, parserOptions, position);
        }

        if (string.Equals(name, "References", StringComparison.OrdinalIgnoreCase))
        {
            return new EmailMessageIdParser().ParseReferencesWithInputAndOptionsAndInitialPosition(value, parserOptions, position);
        }

        return new EmailMessageIdParser().ParseInReplyToWithInputAndOptionsAndInitialPosition(value, parserOptions, position);
    }

    static void AddParsedIdentifiers(string name, EmailMessageIdList parsed, List<EmailMessageId> messageIdentifiers, List<EmailMessageId> references, List<EmailMessageId> inReplyTo, EmailThreadExtractionBudget budget, EmailThreadOptions options)
    {
        List<EmailMessageId> destination = messageIdentifiers;
        if (string.Equals(name, "References", StringComparison.OrdinalIgnoreCase))
        {
            destination = references;
        }
        else if (string.Equals(name, "In-Reply-To", StringComparison.OrdinalIgnoreCase))
        {
            destination = inReplyTo;
        }

        int index = 0;
        while (index < parsed.GetCount())
        {
            budget.AddIdentifier();
            EmailMessageId identifier = parsed.GetAt(index);
            if ((options.GetAllowRecoveredIdentifiers() || !identifier.WasRecovered()) && new EmailMessageIdFormatter().FormatWithIdentifier(identifier).IsSuccess() && !ContainsIdentifier(destination, identifier))
            {
                destination.Add(identifier);
            }

            index++;
        }
    }

    static EmailMessageId? GetParentCandidate(List<EmailMessageId> references, List<EmailMessageId> inReplyTo)
    {
        if (references.Count > 0)
        {
            return references[references.Count - 1];
        }

        return inReplyTo.Count == 1 ? inReplyTo[0] : null;
    }

    static EmailThreadIdentitySource GetIdentitySource(List<EmailMessageId> messageIdentifiers, List<EmailMessageId> references, List<EmailMessageId> inReplyTo, string? normalizedSubject, EmailThreadOptions options)
    {
        if (references.Count > 0)
        {
            return EmailThreadIdentitySource.References;
        }

        if (inReplyTo.Count == 1)
        {
            return EmailThreadIdentitySource.InReplyTo;
        }

        if (messageIdentifiers.Count > 0)
        {
            return EmailThreadIdentitySource.MessageId;
        }

        if (options.GetEnableSubjectFallback() && normalizedSubject != null)
        {
            return EmailThreadIdentitySource.NormalizedSubject;
        }

        return EmailThreadIdentitySource.None;
    }

    static string? GetThreadKey(EmailThreadIdentitySource source, List<EmailMessageId> messageIdentifiers, List<EmailMessageId> references, List<EmailMessageId> inReplyTo, string? normalizedSubject)
    {
        if (source == EmailThreadIdentitySource.References)
        {
            return "id:" + CanonicalizeIdentifierImplementation(references[0]);
        }

        if (source == EmailThreadIdentitySource.InReplyTo)
        {
            return "id:" + CanonicalizeIdentifierImplementation(inReplyTo[0]);
        }

        if (source == EmailThreadIdentitySource.MessageId)
        {
            return "id:" + CanonicalizeIdentifierImplementation(messageIdentifiers[0]);
        }

        return source == EmailThreadIdentitySource.NormalizedSubject ? "subject:" + normalizedSubject : null;
    }

    static bool ContainsIdentifier(List<EmailMessageId> values, EmailMessageId expected)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (IdentifierEquals(values[index], expected))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static bool IdentifierEquals(EmailMessageId first, EmailMessageId second)
    {
        return string.Equals(first.GetIdLeft(), second.GetIdLeft(), StringComparison.Ordinal) && string.Equals(first.GetIdRight(), second.GetIdRight(), StringComparison.OrdinalIgnoreCase) && first.IsRightDomainLiteral() == second.IsRightDomainLiteral();
    }

    static bool IsIdentifierField(string name)
    {
        return string.Equals(name, "Message-ID", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "References", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "In-Reply-To", StringComparison.OrdinalIgnoreCase);
    }

    static string RemoveLeadingResponsePrefix(string value)
    {
        int index = 0;
        while (index < value.Length && IsAsciiLetter(value[index]))
        {
            index++;
        }

        if (index == 0 || !IsResponseToken(value.Substring(0, index)))
        {
            return value;
        }

        while (index < value.Length && char.IsWhiteSpace(value[index]))
        {
            index++;
        }

        if (index < value.Length && (value[index] == '[' || value[index] == '('))
        {
            char close = value[index] == '[' ? ']' : ')';
            index++;
            int digitStart = index;
            while (index < value.Length && value[index] >= '0' && value[index] <= '9')
            {
                index++;
            }

            if (index == digitStart || index >= value.Length || value[index] != close)
            {
                return value;
            }

            index++;
            while (index < value.Length && char.IsWhiteSpace(value[index]))
            {
                index++;
            }
        }

        if (index >= value.Length || value[index] != ':')
        {
            return value;
        }

        return value.Substring(index + 1).TrimStart();
    }

    static string RemoveForwardWrapper(string value)
    {
        if (value.Length > 6 && value[0] == '[' && value[value.Length - 1] == ']')
        {
            int colon = value.IndexOf(':');
            if (colon > 1 && IsForwardToken(value.Substring(1, colon - 1).Trim()))
            {
                return value.Substring(colon + 1, value.Length - colon - 2).Trim();
            }
        }

        return value;
    }

    static string RemoveTrailingForwardMarker(string value)
    {
        const string marker = "(fwd)";
        if (value.Length >= marker.Length && string.Equals(value.Substring(value.Length - marker.Length), marker, StringComparison.OrdinalIgnoreCase))
        {
            return value.Substring(0, value.Length - marker.Length).TrimEnd();
        }

        return value;
    }

    static bool IsResponseToken(string value)
    {
        return string.Equals(value, "re", StringComparison.OrdinalIgnoreCase) || IsForwardToken(value) || string.Equals(value, "aw", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "sv", StringComparison.OrdinalIgnoreCase);
    }

    static bool IsForwardToken(string value)
    {
        return string.Equals(value, "fw", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "fwd", StringComparison.OrdinalIgnoreCase);
    }

    static bool IsAsciiLetter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }

    static string CollapseWhitespace(string value)
    {
        StringBuilder output = new StringBuilder();
        bool pendingSpace = false;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            bool unsafeControl = current == 0 || current < 32 && current != '\t' || current == 127;
            if (unsafeControl || char.IsWhiteSpace(current))
            {
                if (output.Length > 0)
                {
                    pendingSpace = true;
                }
            }
            else
            {
                if (pendingSpace)
                {
                    output.Append(' ');
                    pendingSpace = false;
                }

                output.Append(current);
            }

            index++;
        }

        return output.ToString();
    }
}
