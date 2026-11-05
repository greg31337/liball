using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace jmap;

internal class EmailQuerySorter
{
    internal ValueValidator Values = new ValueValidator();
    internal HeaderValueParser HeaderValues = new HeaderValueParser();
    internal AddressHeaderParser Addresses = new AddressHeaderParser();

    internal JmapMethodError? ValidateComparators(
        List<EmailComparator>? comparators,
        List<string>? advertisedProperties,
        List<string>? supportedCollations)
    {
        if (comparators == null)
        {
            return null;
        }

        HashSet<string> known = ComparatorProperties();
        HashSet<string>? advertised = advertisedProperties == null ? null :
            new HashSet<string>(advertisedProperties, StringComparer.Ordinal);
        HashSet<string>? collations = supportedCollations == null ? null :
            new HashSet<string>(supportedCollations, StringComparer.Ordinal);
        foreach (EmailComparator comparator in comparators)
        {
            if (comparator == null || comparator.Property == null ||
                !Values.IsValidUnicode(comparator.Property) || comparator.Property.Length == 0)
            {
                return InvalidComparator("An Email comparator requires a property.");
            }

            if (!known.Contains(comparator.Property) || advertised != null && !advertised.Contains(comparator.Property))
            {
                return UnsupportedComparator("The Email comparator property is not advertised as supported.");
            }

            bool keywordProperty = IsKeywordProperty(comparator.Property);
            if (keywordProperty && !Values.IsKeyword(comparator.Keyword) ||
                !keywordProperty && comparator.Keyword != null)
            {
                return InvalidComparator("Keyword comparators require one valid keyword and other comparators forbid it.");
            }

            if (comparator.IsAscending && !comparator.HasIsAscending)
            {
                return InvalidComparator("The comparator direction presence flag is inconsistent.");
            }

            if (comparator.UnknownProperties == null)
            {
                return InvalidComparator("The Email comparator extension data is invalid.");
            }

            if (comparator.UnknownProperties.Count != 0)
            {
                return UnsupportedComparator("The Email comparator contains an unsupported property.");
            }

            if (comparator.Collation != null && IsStringProperty(comparator.Property))
            {
                if (comparator.Collation.Length == 0 || !Values.IsValidUnicode(comparator.Collation) ||
                    collations != null && !collations.Contains(comparator.Collation))
                {
                    return UnsupportedComparator("The requested Email comparator collation is not supported.");
                }
            }
        }

        return null;
    }

    internal List<Email> OrderEmails(
        List<Email>? matchingEmails,
        List<Email>? allEmails,
        List<EmailComparator>? comparators,
        bool collapseThreads)
    {
        ValidateEmailList(matchingEmails, "matchingEmails");
        if (matchingEmails == null)
        {
            return new List<Email>();
        }

        List<Email> complete = allEmails == null ? matchingEmails : allEmails;
        ValidateEmailList(complete, "allEmails");
        ValidateCompleteEmails(matchingEmails, complete);
        Dictionary<string, List<Email>> threads = GroupThreads(complete);
        List<EmailComparator> effective = EffectiveComparators(comparators);
        List<Email> ordered = new List<Email>(matchingEmails);
        int index = 1;
        while (index < ordered.Count)
        {
            Email item = ordered[index];
            int insertion = index;
            while (insertion > 0 && CompareEmails(item, ordered[insertion - 1], effective, threads) < 0)
            {
                ordered[insertion] = ordered[insertion - 1];
                insertion = insertion - 1;
            }

            ordered[insertion] = item;
            index = index + 1;
        }

        if (collapseThreads)
        {
            return CollapseThreads(ordered);
        }

        return ordered;
    }

    internal int CompareEmails(
        Email left,
        Email right,
        List<EmailComparator> comparators,
        Dictionary<string, List<Email>> threads)
    {
        foreach (EmailComparator comparator in comparators)
        {
            int comparison = CompareProperty(left, right, comparator, threads);
            bool ascending = !comparator.HasIsAscending || comparator.IsAscending;
            if (comparison != 0)
            {
                return ascending ? comparison : -comparison;
            }
        }

        string leftId = left.Id == null ? "" : left.Id;
        string rightId = right.Id == null ? "" : right.Id;
        return string.Compare(leftId, rightId, StringComparison.Ordinal);
    }

    internal int CompareProperty(
        Email left,
        Email right,
        EmailComparator comparator,
        Dictionary<string, List<Email>> threads)
    {
        string property = comparator.Property == null ? "" : comparator.Property;
        if (property == JmapProperties.ReceivedAt)
        {
            return CompareDates(left.ReceivedAt, right.ReceivedAt);
        }

        if (property == JmapProperties.Size)
        {
            return CompareLongs(left.Size, right.Size);
        }

        if (property == JmapProperties.From)
        {
            return CompareStrings(FirstAddress(left.From, left.Headers, "From"),
                FirstAddress(right.From, right.Headers, "From"), comparator.Collation);
        }

        if (property == JmapProperties.To)
        {
            return CompareStrings(FirstAddress(left.To, left.Headers, "To"),
                FirstAddress(right.To, right.Headers, "To"), comparator.Collation);
        }

        if (property == JmapProperties.Subject)
        {
            return CompareStrings(BaseSubject(SubjectValue(left)), BaseSubject(SubjectValue(right)),
                comparator.Collation);
        }

        if (property == JmapProperties.SentAt)
        {
            return CompareDates(left.SentAt, right.SentAt);
        }

        string keyword = comparator.Keyword == null ? "" : comparator.Keyword;
        if (property == JmapProperties.HasKeyword)
        {
            return CompareBooleans(HasKeyword(left, keyword), HasKeyword(right, keyword));
        }

        if (property == JmapProperties.SomeInThreadHaveKeyword)
        {
            return CompareBooleans(ThreadHasSomeKeyword(left, keyword, threads),
                ThreadHasSomeKeyword(right, keyword, threads));
        }

        if (property == JmapProperties.AllInThreadHaveKeyword)
        {
            return CompareBooleans(ThreadHasAllKeyword(left, keyword, threads),
                ThreadHasAllKeyword(right, keyword, threads));
        }

        return 0;
    }

    internal List<EmailComparator> EffectiveComparators(List<EmailComparator>? comparators)
    {
        if (comparators != null)
        {
            return new List<EmailComparator>(comparators);
        }

        EmailComparator received = new EmailComparator();
        received.Property = JmapProperties.ReceivedAt;
        received.IsAscending = false;
        received.HasIsAscending = true;
        return new List<EmailComparator> { received };
    }

    internal List<Email> CollapseThreads(List<Email> ordered)
    {
        List<Email> result = new List<Email>();
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        foreach (Email email in ordered)
        {
            if (!Values.IsId(email.ThreadId) || email.ThreadId == null)
            {
                throw ProtocolFailure("Collapsing Email results requires a valid threadId.",
                    "emails/" + index + "/threadId");
            }

            if (seen.Add(email.ThreadId))
            {
                result.Add(email);
            }

            index = index + 1;
        }

        return result;
    }

    internal Dictionary<string, List<Email>> GroupThreads(List<Email> emails)
    {
        Dictionary<string, List<Email>> result = new Dictionary<string, List<Email>>(StringComparer.Ordinal);
        foreach (Email email in emails)
        {
            if (email.ThreadId != null)
            {
                if (!result.ContainsKey(email.ThreadId))
                {
                    result.Add(email.ThreadId, new List<Email>());
                }

                result[email.ThreadId].Add(email);
            }
        }

        return result;
    }

    internal void ValidateEmailList(List<Email>? emails, string path)
    {
        if (emails == null)
        {
            if (path == "matchingEmails")
            {
                return;
            }

            throw ProtocolFailure("Complete Email data is required for sorting.", path);
        }

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        foreach (Email email in emails)
        {
            if (email == null || !Values.IsId(email.Id) || email.Id == null || !ids.Add(email.Id))
            {
                throw ProtocolFailure("Email sorting data contains an invalid or duplicate id.",
                    path + "/" + index + "/id");
            }

            index = index + 1;
        }
    }

    internal void ValidateCompleteEmails(List<Email> matchingEmails, List<Email> completeEmails)
    {
        HashSet<string> completeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (Email email in completeEmails)
        {
            if (email.Id != null)
            {
                completeIds.Add(email.Id);
            }
        }

        int index = 0;
        foreach (Email email in matchingEmails)
        {
            if (email.Id == null || !completeIds.Contains(email.Id))
            {
                throw ProtocolFailure("Complete Email data omits a matching Email.",
                    "matchingEmails/" + index + "/id");
            }

            index = index + 1;
        }
    }

    internal bool ThreadHasSomeKeyword(
        Email email,
        string keyword,
        Dictionary<string, List<Email>> threads)
    {
        List<Email> thread = RequireThread(email, threads);
        foreach (Email item in thread)
        {
            if (HasKeyword(item, keyword))
            {
                return true;
            }
        }

        return false;
    }

    internal bool ThreadHasAllKeyword(
        Email email,
        string keyword,
        Dictionary<string, List<Email>> threads)
    {
        List<Email> thread = RequireThread(email, threads);
        foreach (Email item in thread)
        {
            if (!HasKeyword(item, keyword))
            {
                return false;
            }
        }

        return true;
    }

    internal List<Email> RequireThread(Email email, Dictionary<string, List<Email>> threads)
    {
        if (!Values.IsId(email.ThreadId) || email.ThreadId == null || !threads.ContainsKey(email.ThreadId))
        {
            throw ProtocolFailure("Thread keyword sorting requires complete Thread Email data.", "threadId");
        }

        return threads[email.ThreadId];
    }

    internal bool HasKeyword(Email email, string keyword)
    {
        return email.Keywords != null && email.Keywords.ContainsKey(keyword) && email.Keywords[keyword];
    }

    internal string FirstAddress(
        List<EmailAddress>? addresses,
        List<EmailHeader>? headers,
        string headerName)
    {
        List<EmailAddress>? values = addresses;
        if ((values == null || values.Count == 0) && headers != null)
        {
            foreach (EmailHeader header in headers)
            {
                if (header != null && header.Name != null &&
                    string.Equals(header.Name, headerName, StringComparison.OrdinalIgnoreCase))
                {
                    values = Addresses.ParseAddresses(header.Value);
                    break;
                }
            }
        }

        if (values == null || values.Count == 0 || values[0] == null)
        {
            return "";
        }

        string? address = values[0].Email;
        return address == null ? "" : NormalizeText(address);
    }

    internal string SubjectValue(Email email)
    {
        if (email.Subject != null)
        {
            return email.Subject;
        }

        if (email.Headers != null)
        {
            foreach (EmailHeader header in email.Headers)
            {
                if (header != null && header.Name != null &&
                    string.Equals(header.Name, "Subject", StringComparison.OrdinalIgnoreCase))
                {
                    string? decoded = HeaderValues.DecodeText(header.Value);
                    return decoded == null ? "" : decoded;
                }
            }
        }

        return "";
    }

    internal string BaseSubject(string? subject)
    {
        string value = NormalizeWhitespace(subject);
        bool changed = true;
        while (changed && value.Length != 0)
        {
            changed = false;
            string trailer = RemoveForwardTrailer(value);
            if (trailer != value)
            {
                value = trailer;
                changed = true;
            }

            string wrapper = RemoveForwardWrapper(value);
            if (wrapper != value)
            {
                value = wrapper;
                changed = true;
            }

            string leader = RemoveSubjectLeader(value);
            if (leader != value)
            {
                value = leader;
                changed = true;
            }

            else
            {
                string blob = RemoveLeadingBlob(value);
                if (blob != value)
                {
                    value = blob;
                    changed = true;
                }
            }
        }

        return value.Trim();
    }

    internal string RemoveForwardTrailer(string value)
    {
        string trimmed = value.TrimEnd();
        if (trimmed.EndsWith("(fwd)", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed.Substring(0, trimmed.Length - 5).Trim();
        }

        return value;
    }

    internal string RemoveForwardWrapper(string value)
    {
        string trimmed = value.Trim();
        if (trimmed.Length > 6 && trimmed[0] == '[' && trimmed[trimmed.Length - 1] == ']' &&
            trimmed.StartsWith("[fwd:", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed.Substring(5, trimmed.Length - 6).Trim();
        }

        return value;
    }

    internal string RemoveSubjectLeader(string value)
    {
        string trimmed = value.TrimStart();
        int index = 0;
        while (index < trimmed.Length && trimmed[index] == '[')
        {
            int close = trimmed.IndexOf(']', index + 1);
            if (close < 0)
            {
                return value;
            }

            index = close + 1;
            while (index < trimmed.Length && char.IsWhiteSpace(trimmed[index]))
            {
                index = index + 1;
            }
        }

        int wordStart = index;
        while (index < trimmed.Length && char.IsLetter(trimmed[index]))
        {
            index = index + 1;
        }

        string word = trimmed.Substring(wordStart, index - wordStart);
        if (!string.Equals(word, "re", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(word, "fw", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(word, "fwd", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        while (index < trimmed.Length && char.IsWhiteSpace(trimmed[index]))
        {
            index = index + 1;
        }

        if (index < trimmed.Length && trimmed[index] == '[')
        {
            int close = trimmed.IndexOf(']', index + 1);
            if (close < 0)
            {
                return value;
            }

            index = close + 1;
            while (index < trimmed.Length && char.IsWhiteSpace(trimmed[index]))
            {
                index = index + 1;
            }
        }

        if (index >= trimmed.Length || trimmed[index] != ':')
        {
            return value;
        }

        return trimmed.Substring(index + 1).Trim();
    }

    internal string RemoveLeadingBlob(string value)
    {
        string trimmed = value.TrimStart();
        if (trimmed.Length == 0 || trimmed[0] != '[')
        {
            return value;
        }

        int close = trimmed.IndexOf(']', 1);
        if (close < 0)
        {
            return value;
        }

        return trimmed.Substring(close + 1).Trim();
    }

    internal string NormalizeWhitespace(string? value)
    {
        if (value == null)
        {
            return "";
        }

        string normalized = value.Normalize(NormalizationForm.FormC);
        StringBuilder result = new StringBuilder();
        bool spacing = true;
        int index = 0;
        while (index < normalized.Length)
        {
            char character = normalized[index];
            if (char.IsWhiteSpace(character))
            {
                if (!spacing)
                {
                    result.Append(' ');
                    spacing = true;
                }
            }
            else if (!char.IsControl(character))
            {
                result.Append(character);
                spacing = false;
            }

            index = index + 1;
        }

        if (result.Length != 0 && result[result.Length - 1] == ' ')
        {
            result.Length = result.Length - 1;
        }

        return result.ToString();
    }

    internal string NormalizeText(string value)
    {
        return NormalizeWhitespace(value);
    }

    internal int CompareStrings(string left, string right, string? collation)
    {
        if (collation == "i;ascii-casemap")
        {
            return CompareAsciiCase(left, right);
        }

        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }

    internal int CompareAsciiCase(string left, string right)
    {
        int count = left.Length < right.Length ? left.Length : right.Length;
        int index = 0;
        while (index < count)
        {
            char leftCharacter = FoldAscii(left[index]);
            char rightCharacter = FoldAscii(right[index]);
            if (leftCharacter < rightCharacter)
            {
                return -1;
            }

            if (leftCharacter > rightCharacter)
            {
                return 1;
            }

            index = index + 1;
        }

        return CompareLongs(left.Length, right.Length);
    }

    internal char FoldAscii(char value)
    {
        return value >= 'A' && value <= 'Z' ? (char)(value + 32) : value;
    }

    internal int CompareDates(string? left, string? right)
    {
        if (left == null)
        {
            return right == null ? 0 : -1;
        }

        if (right == null)
        {
            return 1;
        }

        if (!Values.IsUtcDate(left) || !Values.IsUtcDate(right))
        {
            return string.Compare(left, right, StringComparison.Ordinal);
        }

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

    internal int CompareLongs(long left, long right)
    {
        return left < right ? -1 : left > right ? 1 : 0;
    }

    internal int CompareBooleans(bool left, bool right)
    {
        return left == right ? 0 : left ? 1 : -1;
    }

    internal HashSet<string> ComparatorProperties()
    {
        return new HashSet<string>(new List<string>
        {
            JmapProperties.ReceivedAt, JmapProperties.Size, JmapProperties.From, JmapProperties.To,
            JmapProperties.Subject, JmapProperties.SentAt, JmapProperties.HasKeyword,
            JmapProperties.SomeInThreadHaveKeyword, JmapProperties.AllInThreadHaveKeyword
        }, StringComparer.Ordinal);
    }

    internal bool IsKeywordProperty(string property)
    {
        return property == JmapProperties.HasKeyword || property == JmapProperties.SomeInThreadHaveKeyword ||
            property == JmapProperties.AllInThreadHaveKeyword;
    }

    internal bool IsStringProperty(string property)
    {
        return property == JmapProperties.From || property == JmapProperties.To ||
            property == JmapProperties.Subject;
    }

    internal JmapMethodError InvalidComparator(string description)
    {
        return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, description);
    }

    internal JmapMethodError UnsupportedComparator(string description)
    {
        return MethodError(JmapErrorCodes.UnsupportedSort, JmapMethodErrorKind.UnsupportedSort, description);
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
