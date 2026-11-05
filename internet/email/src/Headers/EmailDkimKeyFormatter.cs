using System;
using System.Collections.Generic;
using System.Text;

namespace email;

internal class EmailDkimKeyFormatTag
{
    string name;
    string value;
    EmailSourcePosition position;

    internal EmailDkimKeyFormatTag(string name, string value, EmailSourcePosition position)
    {
        this.name = name;
        this.value = value;
        this.position = position;
    }

    internal string GetName()
    {
        return name;
    }

    internal string GetValue()
    {
        return value;
    }

    internal EmailSourcePosition GetPosition()
    {
        return position;
    }
}

internal class EmailDkimKeyFormatterWorker
{
    EmailValueFormatOptions options;
    List<byte> output;
    EmailIssueList issues;
    int column;
    int foldCount;
    bool stopped;

    internal EmailDkimKeyFormatterWorker(EmailValueFormatOptions options)
    {
        this.options = options;
        output = new List<byte>();
        issues = new EmailIssueList();
        column = options.GetFirstLineColumn();
        foldCount = 0;
        stopped = false;
    }

    internal void FormatCanonical(EmailDkimKey value)
    {
        List<EmailDkimKeyFormatTag>? tags = value.GetSourceKind() == EmailDkimKeySourceKind.DnsRecord ? CreateCanonicalTags(value.GetTags()) : CreateImportedTags(value);
        if (tags == null)
        {
            return;
        }

        int index = 0;
        while (index < tags.Count && !stopped)
        {
            AppendTag(tags[index], index > 0, index + 1 < tags.Count);
            index++;
        }
    }

    internal void FormatPreserving(EmailDkimKey value)
    {
        if (value.GetSourceKind() != EmailDkimKeySourceKind.DnsRecord)
        {
            FormatCanonical(value);
            return;
        }

        byte[] raw = value.GetRawBytes();
        EmailSourcePosition position = value.GetSourcePosition();
        if (raw.Length > options.GetMaximumOutputBytes())
        {
            AddOutputLimit(position);
            return;
        }

        int candidateColumn = column;
        int candidateFolds = 0;
        int index = 0;
        while (index < raw.Length)
        {
            byte current = raw[index];
            if (current == 13)
            {
                if (index + 2 >= raw.Length || raw[index + 1] != 10 || raw[index + 2] != 32 && raw[index + 2] != 9)
                {
                    AddInvalid("The original DKIM key record contains an unsafe carriage return.", position);
                    return;
                }

                if (candidateColumn > options.GetMaximumLineLength())
                {
                    AddInvalid("The original DKIM key record exceeds the configured line length.", position);
                    return;
                }

                candidateColumn = 0;
                candidateFolds++;
                index += 2;
                continue;
            }

            if (current == 10 || current == 0 || current < 32 && current != 9 || current > 126)
            {
                AddInvalid("The original DKIM key record contains an unsafe control or non-ASCII byte.", position);
                return;
            }

            candidateColumn++;
            if (candidateColumn > options.GetMaximumLineLength())
            {
                AddInvalid("The original DKIM key record exceeds the configured line length.", position);
                return;
            }

            index++;
        }

        output.AddRange(raw);
        column = candidateColumn;
        foldCount = candidateFolds;
    }

    internal EmailValueFormatResult CreateResult()
    {
        if (stopped)
        {
            return new EmailValueFormatResult(new byte[0], issues, 0, options.GetFirstLineColumn());
        }

        return new EmailValueFormatResult(output.ToArray(), issues, foldCount, column);
    }

    List<EmailDkimKeyFormatTag>? CreateCanonicalTags(EmailSignatureTagList source)
    {
        List<EmailDkimKeyFormatTag> tags = new List<EmailDkimKeyFormatTag>();
        int index = 0;
        while (index < source.GetCount())
        {
            EmailSignatureTag sourceTag = source.GetAt(index);
            string name = sourceTag.GetNormalizedName();
            string? safeValue = NormalizeFoldingWhitespace(sourceTag.GetValue());
            if (sourceTag.IsMalformed() || !IsTagName(name) || safeValue == null)
            {
                AddInvalid("A DKIM key tag cannot be formatted safely.", sourceTag.GetSourcePosition());
                return null;
            }

            string value = GetCanonicalValue(sourceTag, safeValue);
            if (!IsSafeCanonicalValue(value))
            {
                AddInvalid("A DKIM key tag value contains an unsafe character.", sourceTag.GetSourcePosition());
                return null;
            }

            InsertOrdered(tags, new EmailDkimKeyFormatTag(name, value, sourceTag.GetSourcePosition()));
            index++;
        }

        return tags;
    }

    List<EmailDkimKeyFormatTag> CreateImportedTags(EmailDkimKey value)
    {
        List<EmailDkimKeyFormatTag> tags = new List<EmailDkimKeyFormatTag>();
        EmailSourcePosition position = value.GetSourcePosition();
        tags.Add(new EmailDkimKeyFormatTag("v", "DKIM1", position));
        tags.Add(new EmailDkimKeyFormatTag("k", value.GetKeyType().ToLowerInvariant(), position));
        tags.Add(new EmailDkimKeyFormatTag("p", Convert.ToBase64String(value.GetPublicKeyData()), position));
        return tags;
    }

    void AppendTag(EmailDkimKeyFormatTag tag, bool hasPrevious, bool hasFollowing)
    {
        string name = tag.GetName();
        string value = tag.GetValue();
        bool splittable = name == "p";
        int reserve = hasFollowing ? 1 : 0;
        int required = Encoding.ASCII.GetByteCount(name) + 1 + Encoding.ASCII.GetByteCount(value) + reserve;
        if (hasPrevious)
        {
            Append(";", tag.GetPosition());
            if (stopped)
            {
                return;
            }

            if (column + 1 + required <= options.GetMaximumLineLength())
            {
                Append(" ", tag.GetPosition());
            }
            else
            {
                AppendFold(tag.GetPosition());
            }
        }
        else if (column + required > options.GetMaximumLineLength())
        {
            AppendFold(tag.GetPosition());
        }

        int prefixLength = Encoding.ASCII.GetByteCount(name) + 1;
        if (!splittable && column + prefixLength + Encoding.ASCII.GetByteCount(value) + reserve > options.GetMaximumLineLength())
        {
            AddInvalid("A DKIM key tag cannot fit within the configured line length without changing its value.", tag.GetPosition());
            return;
        }

        Append(name + "=", tag.GetPosition());
        if (splittable)
        {
            AppendPublicKey(value, reserve, tag.GetPosition());
        }
        else
        {
            Append(value, tag.GetPosition());
        }
    }

    void AppendPublicKey(string value, int reserve, EmailSourcePosition position)
    {
        int index = 0;
        while (index < value.Length && !stopped)
        {
            int available = options.GetMaximumLineLength() - column - reserve;
            if (available <= 0)
            {
                AppendFold(position);
                available = options.GetMaximumLineLength() - column - reserve;
            }

            int count = value.Length - index;
            if (count > available)
            {
                count = available;
            }

            if (count <= 0)
            {
                AddInvalid("The configured line length cannot accommodate DKIM public-key data.", position);
                return;
            }

            Append(value.Substring(index, count), position);
            index += count;
            if (index < value.Length)
            {
                AppendFold(position);
            }
        }
    }

    void AppendFold(EmailSourcePosition position)
    {
        Append("\r\n\t", position);
    }

    void Append(string value, EmailSourcePosition position)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        if ((long)output.Count + bytes.Length > options.GetMaximumOutputBytes())
        {
            AddOutputLimit(position);
            return;
        }

        int index = 0;
        while (index < bytes.Length)
        {
            byte current = bytes[index];
            output.Add(current);
            if (current == 13 && index + 1 < bytes.Length && bytes[index + 1] == 10)
            {
                output.Add(10);
                column = 0;
                foldCount++;
                index += 2;
                continue;
            }

            column = current == 10 ? 0 : column + 1;
            index++;
        }
    }

    void AddInvalid(string description, EmailSourcePosition position)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.DkimKeyFormattingInvalid, description, position, null, null, "No partial DKIM key record was returned."));
        stopped = true;
    }

    void AddOutputLimit(EmailSourcePosition position)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.DkimKeyOutputLimitExceeded, "The formatted DKIM key record exceeds the configured output-byte limit.", position, null, null, "No partial DKIM key record was returned."));
        stopped = true;
    }

    static void InsertOrdered(List<EmailDkimKeyFormatTag> tags, EmailDkimKeyFormatTag value)
    {
        int valueRank = GetRank(value.GetName());
        int index = 0;
        while (index < tags.Count)
        {
            EmailDkimKeyFormatTag current = tags[index];
            int currentRank = GetRank(current.GetName());
            if (valueRank < currentRank || valueRank == currentRank && valueRank == 7 && string.Compare(value.GetName(), current.GetName(), StringComparison.Ordinal) < 0)
            {
                break;
            }

            index++;
        }

        tags.Insert(index, value);
    }

    static int GetRank(string name)
    {
        if (name == "v")
        {
            return 0;
        }

        if (name == "k")
        {
            return 1;
        }

        if (name == "h")
        {
            return 2;
        }

        if (name == "s")
        {
            return 3;
        }

        if (name == "t")
        {
            return 4;
        }

        if (name == "n")
        {
            return 5;
        }

        return name == "p" ? 6 : 7;
    }

    static string GetCanonicalValue(EmailSignatureTag tag, string safeValue)
    {
        string name = tag.GetNormalizedName();
        if (name == "p")
        {
            return tag.GetCompactValue();
        }

        if (name == "v")
        {
            string compact = tag.GetCompactValue();
            return string.Equals(compact, "DKIM1", StringComparison.OrdinalIgnoreCase) ? "DKIM1" : compact;
        }

        if (name == "k" || name == "h" || name == "s" || name == "t")
        {
            return tag.GetCompactValue().ToLowerInvariant();
        }

        return TrimAsciiWhitespace(safeValue);
    }

    static string? NormalizeFoldingWhitespace(string value)
    {
        StringBuilder normalized = new StringBuilder();
        bool pendingSpace = false;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '\r')
            {
                if (index + 2 >= value.Length || value[index + 1] != '\n' || value[index + 2] != ' ' && value[index + 2] != '\t')
                {
                    return null;
                }

                pendingSpace = true;
                index += 2;
            }
            else if (current == ' ' || current == '\t')
            {
                pendingSpace = true;
            }
            else
            {
                if (current == '\n' || current < 32 || current > 126 || current == ';')
                {
                    return null;
                }

                if (pendingSpace && normalized.Length > 0)
                {
                    normalized.Append(' ');
                }

                pendingSpace = false;
                normalized.Append(current);
            }

            index++;
        }

        return normalized.ToString();
    }

    static bool IsTagName(string value)
    {
        if (value.Length == 0 || !IsAlpha(value[0]))
        {
            return false;
        }

        int index = 1;
        while (index < value.Length)
        {
            char current = value[index];
            if (!IsAlpha(current) && (current < '0' || current > '9') && current != '_')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool IsAlpha(char value)
    {
        return value >= 'a' && value <= 'z' || value >= 'A' && value <= 'Z';
    }

    static bool IsSafeCanonicalValue(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current < 32 || current > 126 || current == ';')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static string TrimAsciiWhitespace(string value)
    {
        int start = 0;
        while (start < value.Length && value[start] == ' ')
        {
            start++;
        }

        int end = value.Length;
        while (end > start && value[end - 1] == ' ')
        {
            end--;
        }

        return value.Substring(start, end - start);
    }
}
