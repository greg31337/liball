using System.Collections.Generic;
using System.Text;

namespace jmap;

internal class AddressCommentResult
{
    internal string? Text;
    internal int NextIndex;
}

internal class AddressHeaderParser
{
    internal HeaderValueParser Values = new HeaderValueParser();

    internal List<EmailAddress> ParseAddresses(string? rawValue)
    {
        List<EmailAddress> result = new List<EmailAddress>();
        List<EmailAddressGroup> groups = ParseGroupedAddresses(rawValue);
        foreach (EmailAddressGroup group in groups)
        {
            foreach (EmailAddress address in group.Addresses)
            {
                result.Add(address);
            }
        }

        return result;
    }

    internal List<EmailAddressGroup> ParseGroupedAddresses(string? rawValue)
    {
        List<EmailAddressGroup> result = new List<EmailAddressGroup>();
        if (rawValue == null)
        {
            return result;
        }

        string value = Values.UnfoldValue(rawValue);
        List<EmailAddress> ungrouped = new List<EmailAddress>();
        List<EmailAddress>? grouped = null;
        string? groupName = null;
        int start = 0;
        int index = 0;
        int commentDepth = 0;
        int angleDepth = 0;
        int bracketDepth = 0;
        bool quoted = false;
        bool escaped = false;
        while (index < value.Length)
        {
            char character = value[index];
            if (escaped)
            {
                escaped = false;
            }
            else if (character == '\\' && (quoted || commentDepth > 0))
            {
                escaped = true;
            }
            else if (commentDepth > 0)
            {
                if (character == '(')
                {
                    commentDepth = commentDepth + 1;
                }
                else if (character == ')')
                {
                    commentDepth = commentDepth - 1;
                }
            }
            else if (quoted)
            {
                if (character == '"')
                {
                    quoted = false;
                }
            }
            else if (character == '"')
            {
                quoted = true;
            }
            else if (character == '(')
            {
                commentDepth = 1;
            }
            else if (character == '<')
            {
                angleDepth = angleDepth + 1;
            }
            else if (character == '>' && angleDepth > 0)
            {
                angleDepth = angleDepth - 1;
            }
            else if (character == '[')
            {
                bracketDepth = bracketDepth + 1;
            }
            else if (character == ']' && bracketDepth > 0)
            {
                bracketDepth = bracketDepth - 1;
            }
            else if (angleDepth == 0 && bracketDepth == 0 && character == ':' && grouped == null)
            {
                FlushUngroupedGroup(ungrouped, result);
                groupName = DecodeDisplayName(value.Substring(start, index - start));
                grouped = new List<EmailAddress>();
                start = index + 1;
            }
            else if (angleDepth == 0 && bracketDepth == 0 && character == ',')
            {
                AddMailboxValue(value.Substring(start, index - start), grouped == null ? ungrouped : grouped);
                start = index + 1;
            }
            else if (angleDepth == 0 && bracketDepth == 0 && character == ';')
            {
                if (grouped == null)
                {
                    AddMailboxValue(value.Substring(start, index - start), ungrouped);
                }
                else
                {
                    AddMailboxValue(value.Substring(start, index - start), grouped);
                    AddNamedGroup(groupName, grouped, result);
                    grouped = null;
                    groupName = null;
                }

                start = index + 1;
            }

            index = index + 1;
        }

        if (grouped == null)
        {
            AddMailboxValue(value.Substring(start), ungrouped);
        }
        else
        {
            AddMailboxValue(value.Substring(start), grouped);
            AddNamedGroup(groupName, grouped, result);
        }

        FlushUngroupedGroup(ungrouped, result);
        return result;
    }

    internal void FlushUngroupedGroup(List<EmailAddress> addresses, List<EmailAddressGroup> groups)
    {
        if (addresses.Count == 0)
        {
            return;
        }

        EmailAddressGroup group = new EmailAddressGroup();
        foreach (EmailAddress address in addresses)
        {
            group.Addresses.Add(address);
        }

        groups.Add(group);
        addresses.Clear();
    }

    internal void AddNamedGroup(string? name, List<EmailAddress> addresses, List<EmailAddressGroup> groups)
    {
        EmailAddressGroup group = new EmailAddressGroup();
        group.Name = name;
        group.Addresses = addresses;
        groups.Add(group);
    }

    internal void AddMailboxValue(string value, List<EmailAddress> addresses)
    {
        EmailAddress? address = ParseMailbox(value);
        if (address != null)
        {
            addresses.Add(address);
        }
    }

    internal EmailAddress? ParseMailbox(string rawValue)
    {
        string value = TrimHeaderWhitespace(rawValue);
        if (value.Length == 0)
        {
            return null;
        }

        int angleStart = FindAngleStart(value);
        string? name = null;
        string addressText;
        if (angleStart >= 0)
        {
            int angleEnd = FindAngleEnd(value, angleStart + 1);
            name = DecodeDisplayName(value.Substring(0, angleStart));
            int addressEnd = angleEnd < 0 ? value.Length : angleEnd;
            addressText = value.Substring(angleStart + 1, addressEnd - angleStart - 1);
            if (name == null && angleEnd >= 0)
            {
                name = DecodeFollowingComment(value, angleEnd + 1);
            }
        }
        else
        {
            int commentStart = FindCommentStart(value);
            if (commentStart >= 0)
            {
                AddressCommentResult comment = ReadComment(value, commentStart);
                if (HasOnlyTrailingComments(value, comment.NextIndex))
                {
                    name = DecodeCommentName(comment.Text);
                }
            }

            addressText = value;
        }

        string email = CleanAddressSpecification(addressText);
        if (angleStart < 0 && email.Length == 0)
        {
            return null;
        }

        EmailAddress result = new EmailAddress();
        result.Name = name;
        result.Email = email;
        return result;
    }

    internal int FindAngleStart(string value)
    {
        int index = 0;
        int commentDepth = 0;
        bool quoted = false;
        bool escaped = false;
        while (index < value.Length)
        {
            char character = value[index];
            if (escaped)
            {
                escaped = false;
            }
            else if (character == '\\' && (quoted || commentDepth > 0))
            {
                escaped = true;
            }
            else if (commentDepth > 0)
            {
                if (character == '(')
                {
                    commentDepth = commentDepth + 1;
                }
                else if (character == ')')
                {
                    commentDepth = commentDepth - 1;
                }
            }
            else if (quoted)
            {
                if (character == '"')
                {
                    quoted = false;
                }
            }
            else if (character == '"')
            {
                quoted = true;
            }
            else if (character == '(')
            {
                commentDepth = 1;
            }
            else if (character == '<')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int FindAngleEnd(string value, int start)
    {
        int index = start;
        int commentDepth = 0;
        int bracketDepth = 0;
        bool quoted = false;
        bool escaped = false;
        while (index < value.Length)
        {
            char character = value[index];
            if (escaped)
            {
                escaped = false;
            }
            else if (character == '\\' && (quoted || commentDepth > 0))
            {
                escaped = true;
            }
            else if (commentDepth > 0)
            {
                if (character == '(')
                {
                    commentDepth = commentDepth + 1;
                }
                else if (character == ')')
                {
                    commentDepth = commentDepth - 1;
                }
            }
            else if (quoted)
            {
                if (character == '"')
                {
                    quoted = false;
                }
            }
            else if (character == '"')
            {
                quoted = true;
            }
            else if (character == '(')
            {
                commentDepth = 1;
            }
            else if (character == '[')
            {
                bracketDepth = bracketDepth + 1;
            }
            else if (character == ']' && bracketDepth > 0)
            {
                bracketDepth = bracketDepth - 1;
            }
            else if (character == '>' && bracketDepth == 0)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int FindCommentStart(string value)
    {
        int index = 0;
        int bracketDepth = 0;
        bool quoted = false;
        bool escaped = false;
        while (index < value.Length)
        {
            char character = value[index];
            if (escaped)
            {
                escaped = false;
            }
            else if (character == '\\' && quoted)
            {
                escaped = true;
            }
            else if (quoted)
            {
                if (character == '"')
                {
                    quoted = false;
                }
            }
            else if (character == '"')
            {
                quoted = true;
            }
            else if (character == '[')
            {
                bracketDepth = bracketDepth + 1;
            }
            else if (character == ']' && bracketDepth > 0)
            {
                bracketDepth = bracketDepth - 1;
            }
            else if (character == '(' && bracketDepth == 0)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal AddressCommentResult ReadComment(string value, int start)
    {
        AddressCommentResult result = new AddressCommentResult();
        StringBuilder text = new StringBuilder();
        int depth = 1;
        int index = start + 1;
        bool escaped = false;
        while (index < value.Length && depth > 0)
        {
            char character = value[index];
            if (escaped)
            {
                text.Append(character);
                escaped = false;
            }
            else if (character == '\\')
            {
                escaped = true;
            }
            else if (character == '(')
            {
                depth = depth + 1;
            }
            else if (character == ')')
            {
                depth = depth - 1;
            }
            else
            {
                text.Append(character);
            }

            index = index + 1;
        }

        if (escaped)
        {
            text.Append('\\');
        }

        result.Text = text.ToString();
        result.NextIndex = index;
        return result;
    }

    internal string? DecodeFollowingComment(string value, int start)
    {
        int index = SkipHeaderWhitespace(value, start);
        if (index >= value.Length || value[index] != '(')
        {
            return null;
        }

        AddressCommentResult comment = ReadComment(value, index);
        return DecodeCommentName(comment.Text);
    }

    internal string? DecodeCommentName(string? value)
    {
        if (value == null)
        {
            return null;
        }

        string trimmed = TrimHeaderWhitespace(value);
        string? decoded = Values.DecodeText(trimmed);
        if (decoded == null)
        {
            return null;
        }

        string result = TrimHeaderWhitespace(decoded);
        return result.Length == 0 ? null : result;
    }

    internal string? DecodeDisplayName(string value)
    {
        string withoutComments = RemoveComments(value);
        string trimmed = TrimHeaderWhitespace(withoutComments);
        if (trimmed.Length == 0)
        {
            return null;
        }

        string decoded;
        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[trimmed.Length - 1] == '"')
        {
            decoded = DecodeQuotedPairs(trimmed.Substring(1, trimmed.Length - 2));
        }
        else
        {
            string unescaped = DecodeQuotedPairs(trimmed);
            string? decodedWords = Values.DecodeText(unescaped);
            decoded = decodedWords == null ? unescaped : decodedWords;
        }

        string result = TrimHeaderWhitespace(decoded).Normalize(NormalizationForm.FormC);
        return result.Length == 0 ? null : result;
    }

    internal string DecodeQuotedPairs(string value)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '\\' && index + 1 < value.Length)
            {
                result.Append(value[index + 1]);
                index = index + 2;
            }
            else
            {
                result.Append(value[index]);
                index = index + 1;
            }
        }

        return result.ToString();
    }

    internal string RemoveComments(string value)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        int depth = 0;
        bool quoted = false;
        bool escaped = false;
        while (index < value.Length)
        {
            char character = value[index];
            if (depth > 0)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '(')
                {
                    depth = depth + 1;
                }
                else if (character == ')')
                {
                    depth = depth - 1;
                }
            }
            else if (escaped)
            {
                result.Append(character);
                escaped = false;
            }
            else if (character == '\\' && quoted)
            {
                result.Append(character);
                escaped = true;
            }
            else if (character == '"')
            {
                quoted = !quoted;
                result.Append(character);
            }
            else if (character == '(' && !quoted)
            {
                depth = 1;
            }
            else
            {
                result.Append(character);
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal string CleanAddressSpecification(string value)
    {
        string withoutComments = RemoveComments(value);
        StringBuilder result = new StringBuilder();
        int index = 0;
        int bracketDepth = 0;
        bool quoted = false;
        bool escaped = false;
        while (index < withoutComments.Length)
        {
            char character = withoutComments[index];
            if (escaped)
            {
                result.Append(character);
                escaped = false;
            }
            else if (character == '\\' && quoted)
            {
                result.Append(character);
                escaped = true;
            }
            else if (character == '"')
            {
                quoted = !quoted;
                result.Append(character);
            }
            else if (character == '[' && !quoted)
            {
                bracketDepth = bracketDepth + 1;
                result.Append(character);
            }
            else if (character == ']' && !quoted && bracketDepth > 0)
            {
                bracketDepth = bracketDepth - 1;
                result.Append(character);
            }
            else if (IsHeaderWhitespace(character) && !quoted && bracketDepth == 0)
            {
            }
            else
            {
                result.Append(character);
            }

            index = index + 1;
        }

        string cleaned = TrimHeaderWhitespace(result.ToString());
        if (cleaned.StartsWith("@", global::System.StringComparison.Ordinal))
        {
            int routeEnd = FindRouteEnd(cleaned);
            if (routeEnd >= 0 && routeEnd + 1 < cleaned.Length)
            {
                cleaned = cleaned.Substring(routeEnd + 1);
            }
        }

        return cleaned.Normalize(NormalizationForm.FormC);
    }

    internal int FindRouteEnd(string value)
    {
        int index = 0;
        int bracketDepth = 0;
        bool quoted = false;
        bool escaped = false;
        while (index < value.Length)
        {
            char character = value[index];
            if (escaped)
            {
                escaped = false;
            }
            else if (character == '\\' && quoted)
            {
                escaped = true;
            }
            else if (character == '"')
            {
                quoted = !quoted;
            }
            else if (character == '[' && !quoted)
            {
                bracketDepth = bracketDepth + 1;
            }
            else if (character == ']' && !quoted && bracketDepth > 0)
            {
                bracketDepth = bracketDepth - 1;
            }
            else if (character == ':' && !quoted && bracketDepth == 0)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool HasOnlyTrailingComments(string value, int start)
    {
        int index = start;
        while (index < value.Length)
        {
            index = SkipHeaderWhitespace(value, index);
            if (index >= value.Length)
            {
                return true;
            }

            if (value[index] != '(')
            {
                return false;
            }

            AddressCommentResult comment = ReadComment(value, index);
            index = comment.NextIndex;
        }

        return true;
    }

    internal int SkipHeaderWhitespace(string value, int start)
    {
        int index = start;
        while (index < value.Length && IsHeaderWhitespace(value[index]))
        {
            index = index + 1;
        }

        return index;
    }

    internal string TrimHeaderWhitespace(string value)
    {
        int start = 0;
        while (start < value.Length && IsHeaderWhitespace(value[start]))
        {
            start = start + 1;
        }

        int end = value.Length;
        while (end > start && IsHeaderWhitespace(value[end - 1]))
        {
            end = end - 1;
        }

        return start == 0 && end == value.Length ? value : value.Substring(start, end - start);
    }

    internal bool IsHeaderWhitespace(char character)
    {
        return character == ' ' || character == '\t' || character == '\r' || character == '\n';
    }
}
