using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace jmap;

internal class EmailFilterEvaluator
{
    internal EmailTextMatcher Text = new EmailTextMatcher();
    internal HeaderPropertySelector Headers = new HeaderPropertySelector();
    internal ValueValidator Values = new ValueValidator();

    internal JmapMethodError? ValidateFilter(EmailFilter? filter, int maximumDepth)
    {
        if (maximumDepth <= 0)
        {
            return MethodError(JmapErrorCodes.UnsupportedFilter, JmapMethodErrorKind.UnsupportedFilter,
                "The supported Email filter depth must be positive.");
        }

        FilterTraversalBudget budget = new FilterTraversalBudget();
        return ValidateFilterNode(filter, 1, maximumDepth, budget);
    }

    internal JmapMethodError? ValidateFilterNode(
        EmailFilter? filter,
        int depth,
        int maximumDepth,
        FilterTraversalBudget budget)
    {
        if (filter == null)
        {
            return null;
        }

        budget.Nodes = budget.Nodes + 1;
        if (budget.Nodes > SecurityLimits.MaximumFilterNodes)
        {
            return MethodError(JmapErrorCodes.UnsupportedFilter,
                JmapMethodErrorKind.UnsupportedFilter,
                "The Email filter contains too many conditions.");
        }

        if (depth > maximumDepth)
        {
            return MethodError(JmapErrorCodes.UnsupportedFilter, JmapMethodErrorKind.UnsupportedFilter,
                "The Email filter exceeds the supported recursion depth.");
        }

        if (filter.HasOperator)
        {
            if (!IsOperator(filter.Operator) || filter.Conditions == null || HasConditionValues(filter))
            {
                return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                    "The Email filter operator is malformed.");
            }

            if (filter.UnknownProperties == null)
            {
                return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                    "The Email filter contains invalid extension data.");
            }

            if (filter.UnknownProperties.Count != 0)
            {
                return MethodError(JmapErrorCodes.UnsupportedFilter, JmapMethodErrorKind.UnsupportedFilter,
                    "The Email filter contains an unsupported property.");
            }

            if (filter.Operator == JmapFilterOperator.Not && filter.Conditions.Count != 1)
            {
                return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                    "A NOT Email filter must contain exactly one condition.");
            }

            foreach (EmailFilter condition in filter.Conditions)
            {
                if (condition == null)
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                        "An Email filter operator contains an invalid condition.");
                }

                JmapMethodError? error = ValidateFilterNode(
                    condition, depth + 1, maximumDepth, budget);
                if (error != null)
                {
                    return error;
                }
            }

            return null;
        }

        if (filter.Conditions != null)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "An Email filter condition cannot contain nested conditions.");
        }

        if (filter.UnknownProperties == null)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The Email filter contains invalid extension data.");
        }

        if (filter.UnknownProperties.Count != 0)
        {
            return MethodError(JmapErrorCodes.UnsupportedFilter, JmapMethodErrorKind.UnsupportedFilter,
                "The Email filter contains an unsupported property.");
        }

        return ValidateCondition(filter);
    }

    internal JmapMethodError? ValidateCondition(EmailFilter filter)
    {
        if (filter.InMailbox != null && !Values.IsId(filter.InMailbox))
        {
            return InvalidCondition("inMailbox must be an Id.");
        }

        if (filter.InMailboxOtherThan != null)
        {
            foreach (string mailboxId in filter.InMailboxOtherThan)
            {
                if (!Values.IsId(mailboxId))
                {
                    return InvalidCondition("inMailboxOtherThan must contain only Id values.");
                }
            }
        }

        if (filter.Before != null && !Values.IsUtcDate(filter.Before) ||
            filter.After != null && !Values.IsUtcDate(filter.After))
        {
            return InvalidCondition("before and after must be UTCDate values.");
        }

        if (filter.MinSize != 0L && !filter.HasMinSize || filter.MaxSize != 0L && !filter.HasMaxSize ||
            filter.HasMinSize && !Values.IsUnsignedInt(filter.MinSize) ||
            filter.HasMaxSize && !Values.IsUnsignedInt(filter.MaxSize))
        {
            return InvalidCondition("minSize and maxSize must be UnsignedInt values with consistent flags.");
        }

        if (!IsOptionalKeyword(filter.AllInThreadHaveKeyword) ||
            !IsOptionalKeyword(filter.SomeInThreadHaveKeyword) ||
            !IsOptionalKeyword(filter.NoneInThreadHaveKeyword) ||
            !IsOptionalKeyword(filter.HasKeyword) || !IsOptionalKeyword(filter.NotKeyword))
        {
            return InvalidCondition("Email keyword filter values must be valid keywords.");
        }

        if (filter.HasAttachment && !filter.HasHasAttachment)
        {
            return InvalidCondition("The hasAttachment presence flag is inconsistent.");
        }

        if (!IsOptionalText(filter.Text) || !IsOptionalText(filter.From) || !IsOptionalText(filter.To) ||
            !IsOptionalText(filter.Cc) || !IsOptionalText(filter.Bcc) || !IsOptionalText(filter.Subject) ||
            !IsOptionalText(filter.Body))
        {
            return InvalidCondition("Email text filter values must contain valid Unicode.");
        }

        if (filter.Header != null)
        {
            if ((filter.Header.Count != 1 && filter.Header.Count != 2) ||
                filter.Header[0] == null ||
                !Headers.IsHeaderName(filter.Header[0]))
            {
                return InvalidCondition("header must contain a field name and optional value.");
            }

            if (filter.Header.Count == 2 && !Values.IsValidUnicode(filter.Header[1]))
            {
                return InvalidCondition("The header filter value must contain valid Unicode.");
            }
        }

        return null;
    }

    internal bool Matches(Email? email, List<Email>? threadEmails, EmailFilter? filter)
    {
        if (email == null)
        {
            return false;
        }

        if (filter == null)
        {
            return true;
        }

        if (filter.HasOperator)
        {
            return MatchesOperator(email, threadEmails, filter);
        }

        return MatchesCondition(email, threadEmails, filter);
    }

    internal bool MatchesOperator(Email email, List<Email>? threadEmails, EmailFilter filter)
    {
        if (filter.Conditions == null)
        {
            return false;
        }

        if (filter.Operator == JmapFilterOperator.And)
        {
            foreach (EmailFilter condition in filter.Conditions)
            {
                if (!Matches(email, threadEmails, condition))
                {
                    return false;
                }
            }

            return true;
        }

        if (filter.Operator == JmapFilterOperator.Or)
        {
            foreach (EmailFilter condition in filter.Conditions)
            {
                if (Matches(email, threadEmails, condition))
                {
                    return true;
                }
            }

            return false;
        }

        if (filter.Operator == JmapFilterOperator.Not && filter.Conditions.Count == 1)
        {
            return !Matches(email, threadEmails, filter.Conditions[0]);
        }

        return false;
    }

    internal bool MatchesCondition(Email email, List<Email>? threadEmails, EmailFilter filter)
    {
        if (filter.InMailbox != null && !HasMapValue(email.MailboxIds, filter.InMailbox))
        {
            return false;
        }

        if (filter.InMailboxOtherThan != null &&
            !HasMailboxOtherThan(email.MailboxIds, filter.InMailboxOtherThan))
        {
            return false;
        }

        if (filter.Before != null &&
            (email.ReceivedAt == null || !Values.IsUtcDate(email.ReceivedAt) ||
                CompareUtcDates(email.ReceivedAt, filter.Before) >= 0))
        {
            return false;
        }

        if (filter.After != null &&
            (email.ReceivedAt == null || !Values.IsUtcDate(email.ReceivedAt) ||
                CompareUtcDates(email.ReceivedAt, filter.After) < 0))
        {
            return false;
        }

        if (filter.HasMinSize && email.Size < filter.MinSize ||
            filter.HasMaxSize && email.Size >= filter.MaxSize)
        {
            return false;
        }

        if (filter.HasKeyword != null && !HasMapValue(email.Keywords, filter.HasKeyword) ||
            filter.NotKeyword != null && HasMapValue(email.Keywords, filter.NotKeyword))
        {
            return false;
        }

        if (!MatchesThreadKeywords(email, threadEmails, filter))
        {
            return false;
        }

        if (filter.HasHasAttachment && email.HasAttachment != filter.HasAttachment)
        {
            return false;
        }

        if (filter.Text != null && !Text.MatchEmailSearchText(email, filter.Text).IsMatch ||
            filter.From != null && !MatchesAddressField(email.From, email.Headers, "From", filter.From) ||
            filter.To != null && !MatchesAddressField(email.To, email.Headers, "To", filter.To) ||
            filter.Cc != null && !MatchesAddressField(email.Cc, email.Headers, "Cc", filter.Cc) ||
            filter.Bcc != null && !MatchesAddressField(email.Bcc, email.Headers, "Bcc", filter.Bcc) ||
            filter.Subject != null && !MatchesTextField(email.Subject, email.Headers, "Subject", filter.Subject) ||
            filter.Body != null && !Text.MatchEmailBodyText(email, filter.Body).IsMatch)
        {
            return false;
        }

        if (filter.Header != null)
        {
            string? value = filter.Header.Count == 2 ? filter.Header[1] : null;
            if (!Text.MatchesHeaderList(email.Headers, filter.Header[0], value))
            {
                return false;
            }
        }

        return true;
    }

    internal bool MatchesThreadKeywords(Email email, List<Email>? threadEmails, EmailFilter filter)
    {
        bool hasThreadFilter = filter.AllInThreadHaveKeyword != null ||
            filter.SomeInThreadHaveKeyword != null || filter.NoneInThreadHaveKeyword != null;
        if (!hasThreadFilter)
        {
            return true;
        }

        ValidateThreadEmails(email, threadEmails);
        if (threadEmails == null)
        {
            return false;
        }

        if (filter.AllInThreadHaveKeyword != null)
        {
            foreach (Email threadEmail in threadEmails)
            {
                if (!HasMapValue(threadEmail.Keywords, filter.AllInThreadHaveKeyword))
                {
                    return false;
                }
            }
        }

        if (filter.SomeInThreadHaveKeyword != null)
        {
            bool found = false;
            foreach (Email threadEmail in threadEmails)
            {
                if (HasMapValue(threadEmail.Keywords, filter.SomeInThreadHaveKeyword))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        if (filter.NoneInThreadHaveKeyword != null)
        {
            foreach (Email threadEmail in threadEmails)
            {
                if (HasMapValue(threadEmail.Keywords, filter.NoneInThreadHaveKeyword))
                {
                    return false;
                }
            }
        }

        return true;
    }

    internal void ValidateThreadEmails(Email email, List<Email>? threadEmails)
    {
        if (threadEmails == null || threadEmails.Count == 0)
        {
            throw ProtocolFailure("Thread keyword filters require complete Thread Email data.", "threadEmails");
        }

        bool containsEmail = false;
        int index = 0;
        foreach (Email threadEmail in threadEmails)
        {
            if (threadEmail == null || threadEmail.ThreadId != email.ThreadId)
            {
                throw ProtocolFailure("Thread keyword filter data contains an Email from another Thread.",
                    "threadEmails/" + index);
            }

            if (threadEmail.Id != null && threadEmail.Id == email.Id)
            {
                containsEmail = true;
            }

            index = index + 1;
        }

        if (!containsEmail)
        {
            throw ProtocolFailure("Thread keyword filter data omits the candidate Email.", "threadEmails");
        }
    }

    internal bool MatchesAddressField(
        List<EmailAddress>? addresses,
        List<EmailHeader>? headers,
        string name,
        string query)
    {
        StringBuilder value = new StringBuilder();
        if (addresses != null)
        {
            foreach (EmailAddress address in addresses)
            {
                if (address != null)
                {
                    Text.AppendTextValue(value, address.Name);
                    Text.AppendTextValue(value, address.Email);
                }
            }
        }

        if (Text.MatchPlainText(value.ToString(), query).IsMatch)
        {
            return true;
        }

        return Text.MatchesHeaderList(headers, name, query);
    }

    internal bool MatchesTextField(string? value, List<EmailHeader>? headers, string name, string query)
    {
        return Text.MatchPlainText(value, query).IsMatch || Text.MatchesHeaderList(headers, name, query);
    }

    internal bool HasMailboxOtherThan(Dictionary<string, bool>? mailboxes, List<string> excluded)
    {
        if (mailboxes == null)
        {
            return false;
        }

        HashSet<string> excludedSet = new HashSet<string>(excluded, StringComparer.Ordinal);
        foreach (KeyValuePair<string, bool> mailbox in mailboxes)
        {
            if (mailbox.Value && !excludedSet.Contains(mailbox.Key))
            {
                return true;
            }
        }

        return false;
    }

    internal bool HasMapValue(Dictionary<string, bool>? values, string key)
    {
        return values != null && values.ContainsKey(key) && values[key];
    }

    internal bool HasConditionValues(EmailFilter filter)
    {
        return filter.InMailbox != null || filter.InMailboxOtherThan != null || filter.Before != null ||
            filter.After != null || filter.HasMinSize || filter.MinSize != 0L || filter.HasMaxSize ||
            filter.MaxSize != 0L || filter.AllInThreadHaveKeyword != null ||
            filter.SomeInThreadHaveKeyword != null || filter.NoneInThreadHaveKeyword != null ||
            filter.HasKeyword != null || filter.NotKeyword != null || filter.HasHasAttachment ||
            filter.HasAttachment || filter.Text != null || filter.From != null || filter.To != null ||
            filter.Cc != null || filter.Bcc != null || filter.Subject != null || filter.Body != null ||
            filter.Header != null;
    }

    internal bool IsOptionalKeyword(string? value)
    {
        return value == null || Values.IsKeyword(value);
    }

    internal bool IsOptionalText(string? value)
    {
        return value == null || Values.IsValidUnicode(value);
    }

    internal bool IsOperator(JmapFilterOperator value)
    {
        return value == JmapFilterOperator.And || value == JmapFilterOperator.Or ||
            value == JmapFilterOperator.Not;
    }

    internal int CompareUtcDates(string left, string right)
    {
        int index = 0;
        while (index < 19)
        {
            if (left[index] < right[index])
            {
                return -1;
            }

            if (left[index] > right[index])
            {
                return 1;
            }

            index = index + 1;
        }

        int leftIndex = left[19] == '.' ? 20 : left.Length - 1;
        int rightIndex = right[19] == '.' ? 20 : right.Length - 1;
        int leftEnd = left.Length - 1;
        int rightEnd = right.Length - 1;
        while (leftIndex < leftEnd || rightIndex < rightEnd)
        {
            char leftDigit = leftIndex < leftEnd ? left[leftIndex] : '0';
            char rightDigit = rightIndex < rightEnd ? right[rightIndex] : '0';
            if (leftDigit < rightDigit)
            {
                return -1;
            }

            if (leftDigit > rightDigit)
            {
                return 1;
            }

            leftIndex = leftIndex + 1;
            rightIndex = rightIndex + 1;
        }

        return 0;
    }

    internal JmapMethodError InvalidCondition(string description)
    {
        return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, description);
    }

    internal JmapMethodError MethodError(string type, JmapMethodErrorKind kind, string description)
    {
        JmapMethodError error = new JmapMethodError();
        error.Type = type;
        error.Kind = kind;
        error.Description = description;
        return error;
    }

    internal JmapProtocolException ProtocolFailure(string message, string path)
    {
        return new JmapProtocolException(message, path, default(JsonElement));
    }
}
