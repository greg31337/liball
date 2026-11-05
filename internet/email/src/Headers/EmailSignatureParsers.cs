using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailSignatureParserCore
{
    internal static EmailDkimSignatureParseResult ParseDkim(ReadOnlySpan<byte> input, EmailSignatureParserOptions options, EmailSourcePosition? initialPosition)
    {
        Validate(options, initialPosition);
        byte[] source = input.ToArray();
        EmailSignatureParserWorker? worker = CreateWorker(source, options, initialPosition);
        if (worker == null)
        {
            EmailIssueList issues = CreateInputLimitIssue(initialPosition);
            return new EmailDkimSignatureParseResult(null, issues, 0, source.Length);
        }

        EmailSignatureTagList tags = worker.ParseTags(false);
        worker.ValidateDkim(tags, false);
        EmailDkimSignature value = new EmailDkimSignature(EmailSignatureHeaderKind.DkimSignature, tags, source, (initialPosition ?? throw new InvalidOperationException()));
        return new EmailDkimSignatureParseResult(value, worker.GetIssues(), worker.GetConsumedByteCount(), source.Length);
    }

    internal static EmailArcMessageSignatureParseResult ParseArcMessageSignature(ReadOnlySpan<byte> input, EmailSignatureParserOptions options, EmailSourcePosition? initialPosition)
    {
        Validate(options, initialPosition);
        byte[] source = input.ToArray();
        EmailSignatureParserWorker? worker = CreateWorker(source, options, initialPosition);
        if (worker == null)
        {
            EmailIssueList issues = CreateInputLimitIssue(initialPosition);
            return new EmailArcMessageSignatureParseResult(null, issues, 0, source.Length);
        }

        EmailSignatureTagList tags = worker.ParseTags(true);
        worker.ValidateDkim(tags, true);
        EmailDkimSignature value = new EmailDkimSignature(EmailSignatureHeaderKind.ArcMessageSignature, tags, source, (initialPosition ?? throw new InvalidOperationException()));
        return new EmailArcMessageSignatureParseResult(value, worker.GetIssues(), worker.GetConsumedByteCount(), source.Length);
    }

    internal static EmailArcSealParseResult ParseArcSeal(ReadOnlySpan<byte> input, EmailSignatureParserOptions options, EmailSourcePosition? initialPosition)
    {
        Validate(options, initialPosition);
        byte[] source = input.ToArray();
        EmailSignatureParserWorker? worker = CreateWorker(source, options, initialPosition);
        if (worker == null)
        {
            EmailIssueList issues = CreateInputLimitIssue(initialPosition);
            return new EmailArcSealParseResult(null, issues, 0, source.Length);
        }

        EmailSignatureTagList tags = worker.ParseTags(true);
        worker.ValidateArcSeal(tags);
        EmailArcSeal value = new EmailArcSeal(tags, source, (initialPosition ?? throw new InvalidOperationException()));
        return new EmailArcSealParseResult(value, worker.GetIssues(), worker.GetConsumedByteCount(), source.Length);
    }

    internal static EmailArcAuthenticationResultsParseResult ParseArcAuthenticationResults(ReadOnlySpan<byte> input, EmailSignatureParserOptions options, EmailSourcePosition? initialPosition)
    {
        Validate(options, initialPosition);
        byte[] source = input.ToArray();
        EmailSignatureParserOptions snapshot = options.Clone();
        if (source.Length > snapshot.GetMaximumInputBytes())
        {
            EmailIssueList limitIssues = CreateInputLimitIssue(initialPosition);
            return new EmailArcAuthenticationResultsParseResult(null, limitIssues, 0, source.Length);
        }

        EmailIssueList issues = new EmailIssueList();
        int separator = FindArcSeparator(source);
        if (separator < 0)
        {
            AddModeIssue(issues, snapshot.GetMode(), EmailIssueCode.ArcHeaderMalformed, "ARC-Authentication-Results is missing the semicolon after its instance tag.", initialPosition, "No structured value was produced.");
            return new EmailArcAuthenticationResultsParseResult(null, issues, source.Length, source.Length);
        }

        byte[] instanceBytes = Slice(source, 0, separator);
        EmailSignatureParserWorker instanceWorker = new EmailSignatureParserWorker(instanceBytes, snapshot, initialPosition);
        EmailSignatureTagList instanceTags = instanceWorker.ParseTags(true);
        issues.AddRange(instanceWorker.GetIssues());
        EmailSignatureTag? instanceTag = instanceTags.GetFirst("i");
        int instance = ParseInstance(instanceTag);
        if (instanceTag == null || instanceTags.GetCount() != 1 || instance < 1 || instance > 50)
        {
            AddModeIssue(issues, snapshot.GetMode(), EmailIssueCode.ArcHeaderMalformed, "ARC-Authentication-Results requires a leading instance tag from 1 through 50.", initialPosition, "The original bytes were retained when a value could be constructed.");
        }

        int payloadStart = separator + 1;
        EmailSourcePosition payloadPosition = PositionAt(source, payloadStart, (initialPosition ?? throw new InvalidOperationException()));
        EmailAuthenticationParserOptions authenticationOptions = new EmailAuthenticationParserOptions();
        authenticationOptions.SetMode(snapshot.GetMode());
        authenticationOptions.SetMaximumInputBytes(snapshot.GetMaximumInputBytes());
        EmailAuthenticationResultsParseResult authentication = new EmailAuthenticationResultsParser().ParseWithInputAndOptionsAndInitialPosition(new ReadOnlySpan<byte>(source, payloadStart, source.Length - payloadStart), authenticationOptions, payloadPosition);
        issues.AddRange(authentication.GetIssues());
        if (instanceTag == null || authentication.GetValue() == null)
        {
            int consumedFailure = payloadStart + authentication.GetConsumedByteCount();
            return new EmailArcAuthenticationResultsParseResult(null, issues, consumedFailure, source.Length);
        }

        EmailArcAuthenticationResults value = new EmailArcAuthenticationResults(instance, instanceTag, authentication.GetValue(), source, initialPosition);
        int consumed = payloadStart + authentication.GetConsumedByteCount();
        return new EmailArcAuthenticationResultsParseResult(value, issues, consumed, source.Length);
    }

    static EmailSignatureParserWorker? CreateWorker(byte[] source, EmailSignatureParserOptions options, EmailSourcePosition? position)
    {
        EmailSignatureParserOptions snapshot = options.Clone();
        if (source.Length > snapshot.GetMaximumInputBytes())
        {
            return null;
        }

        return new EmailSignatureParserWorker(source, snapshot, position);
    }

    static void Validate(EmailSignatureParserOptions options, EmailSourcePosition? initialPosition)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (initialPosition == null)
        {
            throw new ArgumentNullException("initialPosition");
        }
    }

    static int FindArcSeparator(byte[] source)
    {
        int depth = 0;
        bool escaped = false;
        int index = 0;
        while (index < source.Length)
        {
            byte current = source[index];
            if (escaped)
            {
                escaped = false;
            }
            else if (current == 92 && depth > 0)
            {
                escaped = true;
            }
            else if (current == 40)
            {
                depth++;
            }
            else if (current == 41 && depth > 0)
            {
                depth--;
            }
            else if (current == 59 && depth == 0)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    static int ParseInstance(EmailSignatureTag? tag)
    {
        if (tag == null)
        {
            return 0;
        }

        string text = tag.GetCompactValue();
        if (text.Length == 0 || text.Length > 2)
        {
            return 0;
        }

        int value = 0;
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] < '0' || text[index] > '9')
            {
                return 0;
            }

            value = value * 10 + text[index] - '0';
            index++;
        }

        return value;
    }

    static EmailIssueList CreateInputLimitIssue(EmailSourcePosition? position)
    {
        EmailIssueList issues = new EmailIssueList();
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.SignatureValueLimitExceeded, "The signature field value exceeds the configured input-byte limit.", position, null, null, "No structured value was produced."));
        return issues;
    }

    static void AddModeIssue(EmailIssueList issues, EmailStructuredValueParseMode mode, EmailIssueCode code, string description, EmailSourcePosition? position, string recovery)
    {
        EmailIssueSeverity severity = mode == EmailStructuredValueParseMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, position, null, null, recovery));
    }

    static EmailSourcePosition PositionAt(byte[] source, int offset, EmailSourcePosition initialPosition)
    {
        EmailSourceTracker tracker = new EmailSourceTrackerOperations().CreateWithInitialPosition(initialPosition);
        tracker.Advance(new ReadOnlySpan<byte>(source, 0, offset));
        return tracker.GetPosition();
    }

    static byte[] Slice(byte[] source, int start, int end)
    {
        int length = end - start;
        byte[] result = new byte[length];
        Array.Copy(source, start, result, 0, length);
        return result;
    }
}
internal class EmailSignatureParserWorker
{
    byte[] input;
    EmailSignatureParserOptions options;
    EmailSourcePosition? initialPosition;
    EmailIssueList issues;
    int consumedByteCount;

    internal EmailSignatureParserWorker(byte[] input, EmailSignatureParserOptions options, EmailSourcePosition? initialPosition)
    {
        this.input = input;
        this.options = options;
        this.initialPosition = initialPosition;
        issues = new EmailIssueList();
        consumedByteCount = input.Length;
    }

    internal EmailSignatureTagList ParseTags(bool allowArcInstanceCfws)
    {
        List<EmailSignatureTag> tags = new List<EmailSignatureTag>();
        int segmentStart = 0;
        int segmentNumber = 0;
        int index = 0;
        while (index <= input.Length)
        {
            bool atEnd = index == input.Length;
            if (!atEnd && input[index] != 59)
            {
                index++;
                continue;
            }

            int trimmedStart = SkipWhitespace(segmentStart, index);
            int trimmedEnd = TrimWhitespaceEnd(trimmedStart, index);
            bool trailingEmpty = atEnd && segmentStart > 0 && trimmedStart == trimmedEnd;
            if (trimmedStart < trimmedEnd)
            {
                if (tags.Count >= options.GetMaximumTags())
                {
                    AddFatal("The signature value exceeds the configured tag limit.", PositionAt(trimmedStart), "Tags parsed before the limit remain available.");
                    consumedByteCount = trimmedStart;
                    break;
                }

                EmailSignatureTag tag = ParseTag(segmentStart, index, allowArcInstanceCfws && segmentNumber == 0);
                tags.Add(tag);
                if (tag.GetName().Length > 0 && Count(tags, tag.GetName()) > 1)
                {
                    AddModeIssue(EmailIssueCode.SignatureTagDuplicate, "The signature value contains a duplicate '" + tag.GetName() + "' tag.", tag.GetSourcePosition(), "All occurrences were retained in source order.");
                }
            }
            else if (!trailingEmpty && segmentStart != input.Length)
            {
                AddModeIssue(EmailIssueCode.SignatureTagMalformed, "The signature value contains an empty tag specification.", PositionAt(segmentStart), "The original bytes were retained.");
            }

            if (atEnd)
            {
                break;
            }

            segmentStart = index + 1;
            segmentNumber++;
            index++;
        }

        return new EmailSignatureTagList(tags, input);
    }

    internal void ValidateDkim(EmailSignatureTagList tags, bool arcMessageSignature)
    {
        if (arcMessageSignature)
        {
            ValidateInstance(tags);
        }
        else
        {
            Require(tags, "v", EmailIssueCode.DkimSignatureMalformed);
            EmailSignatureTag? version = tags.GetFirst("v");
            if (version != null && version.GetCompactValue() != "1")
            {
                AddModeIssue(EmailIssueCode.DkimSignatureMalformed, "DKIM-Signature supports only version 1.", version.GetSourcePosition(), "The original version tag was retained.");
            }

            if (version != null && tags.GetCount() > 0 && tags.GetAt(0) != version)
            {
                AddModeIssue(EmailIssueCode.DkimSignatureMalformed, "The DKIM version tag must be the first tag.", version.GetSourcePosition(), "The original tag order was retained.");
            }
        }

        Require(tags, "a", arcMessageSignature ? EmailIssueCode.ArcHeaderMalformed : EmailIssueCode.DkimSignatureMalformed);
        Require(tags, "b", arcMessageSignature ? EmailIssueCode.ArcHeaderMalformed : EmailIssueCode.DkimSignatureMalformed);
        Require(tags, "bh", arcMessageSignature ? EmailIssueCode.ArcHeaderMalformed : EmailIssueCode.DkimSignatureMalformed);
        Require(tags, "d", arcMessageSignature ? EmailIssueCode.ArcHeaderMalformed : EmailIssueCode.DkimSignatureMalformed);
        Require(tags, "h", arcMessageSignature ? EmailIssueCode.ArcHeaderMalformed : EmailIssueCode.DkimSignatureMalformed);
        Require(tags, "s", arcMessageSignature ? EmailIssueCode.ArcHeaderMalformed : EmailIssueCode.DkimSignatureMalformed);
        ValidateAlgorithm(tags.GetFirst("a"), arcMessageSignature);
        ValidateBase64(tags.GetFirst("b"), arcMessageSignature);
        ValidateBase64(tags.GetFirst("bh"), arcMessageSignature);
        ValidateCanonicalization(tags.GetFirst("c"), arcMessageSignature);
        ValidateNonEmpty(tags.GetFirst("d"), "The signing-domain tag cannot be empty.", arcMessageSignature);
        ValidateHeaderList(tags.GetFirst("h"), arcMessageSignature);
        ValidateNonEmpty(tags.GetFirst("s"), "The selector tag cannot be empty.", arcMessageSignature);
        ValidateUnsigned(tags.GetFirst("l"), 76, arcMessageSignature);
        ValidateUnsigned(tags.GetFirst("t"), 12, arcMessageSignature);
        ValidateUnsigned(tags.GetFirst("x"), 12, arcMessageSignature);
    }

    internal void ValidateArcSeal(EmailSignatureTagList tags)
    {
        ValidateInstance(tags);
        Require(tags, "a", EmailIssueCode.ArcHeaderMalformed);
        Require(tags, "b", EmailIssueCode.ArcHeaderMalformed);
        Require(tags, "cv", EmailIssueCode.ArcHeaderMalformed);
        Require(tags, "d", EmailIssueCode.ArcHeaderMalformed);
        Require(tags, "s", EmailIssueCode.ArcHeaderMalformed);
        Require(tags, "t", EmailIssueCode.ArcHeaderMalformed);
        ValidateAlgorithm(tags.GetFirst("a"), true);
        ValidateBase64(tags.GetFirst("b"), true);
        ValidateNonEmpty(tags.GetFirst("d"), "The ARC-Seal signing-domain tag cannot be empty.", true);
        ValidateNonEmpty(tags.GetFirst("s"), "The ARC-Seal selector tag cannot be empty.", true);
        ValidateUnsigned(tags.GetFirst("t"), 12, true);

        EmailSignatureTag? cv = tags.GetFirst("cv");
        if (cv != null)
        {
            string value = cv.GetCompactValue().ToLowerInvariant();
            if (value != "none" && value != "pass" && value != "fail")
            {
                AddModeIssue(EmailIssueCode.ArcHeaderMalformed, "ARC-Seal cv must be none, pass, or fail.", cv.GetSourcePosition(), "The unknown chain status was retained.");
            }
        }

        string[] prohibited = new string[] { "bh", "c", "h", "l", "q", "v", "x", "z" };
        int index = 0;
        while (index < prohibited.Length)
        {
            EmailSignatureTag? tag = tags.GetFirst(prohibited[index]);
            if (tag != null)
            {
                AddModeIssue(EmailIssueCode.ArcHeaderMalformed, "ARC-Seal does not permit the '" + prohibited[index] + "' tag.", tag.GetSourcePosition(), "The unsupported tag was retained.");
            }

            index++;
        }
    }

    internal EmailIssueList GetIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(issues);
        return copy;
    }

    internal int GetConsumedByteCount()
    {
        return consumedByteCount;
    }

    EmailSignatureTag ParseTag(int rawStart, int rawEnd, bool allowCfws)
    {
        int cursor = allowCfws ? SkipCfws(rawStart, rawEnd) : SkipWhitespace(rawStart, rawEnd);
        int nameStart = cursor;
        if (cursor < rawEnd && IsAlpha(input[cursor]))
        {
            cursor++;
            while (cursor < rawEnd && IsNameByte(input[cursor]))
            {
                cursor++;
            }
        }

        int nameEnd = cursor;
        cursor = allowCfws ? SkipCfws(cursor, rawEnd) : SkipWhitespace(cursor, rawEnd);
        bool syntaxValid = nameEnd > nameStart && cursor < rawEnd && input[cursor] == 61;
        int equals = cursor;
        if (cursor < rawEnd && input[cursor] == 61)
        {
            cursor++;
        }

        cursor = allowCfws ? SkipCfws(cursor, rawEnd) : SkipWhitespace(cursor, rawEnd);
        int valueStart = cursor;
        int valueEnd = allowCfws ? TrimCfwsEnd(valueStart, rawEnd) : TrimWhitespaceEnd(valueStart, rawEnd);
        if (valueEnd < valueStart)
        {
            valueEnd = valueStart;
        }

        if (!syntaxValid || !ValidateValueBytes(valueStart, valueEnd))
        {
            AddModeIssue(EmailIssueCode.SignatureTagMalformed, "A signature tag specification is malformed.", PositionAt(nameStart), "The malformed tag and original bytes were retained.");
            syntaxValid = false;
        }

        string name = nameEnd > nameStart ? Encoding.ASCII.GetString(input, nameStart, nameEnd - nameStart) : "";
        string value = NormalizeValue(valueStart, valueEnd, false);
        string compact = NormalizeValue(valueStart, valueEnd, true);
        byte[] raw = Slice(rawStart, rawEnd);
        byte[] rawValue = equals < rawEnd && input[equals] == 61 ? Slice(equals + 1, rawEnd) : new byte[0];
        return new EmailSignatureTag(name, value, compact, raw, rawValue, PositionAt(nameStart), !syntaxValid);
    }

    void ValidateInstance(EmailSignatureTagList tags)
    {
        EmailSignatureTag? tag = tags.GetFirst("i");
        Require(tags, "i", EmailIssueCode.ArcHeaderMalformed);
        if (tag == null)
        {
            return;
        }

        if (tags.GetCount() == 0 || tags.GetAt(0) != tag)
        {
            AddModeIssue(EmailIssueCode.ArcHeaderMalformed, "The ARC instance tag must be first.", tag.GetSourcePosition(), "The original tag order was retained.");
        }

        string value = tag.GetCompactValue();
        bool valid = value.Length >= 1 && value.Length <= 2 && IsDigits(value);
        int number = valid ? ParseSmallNumber(value) : 0;
        if (!valid || number < 1 || number > 50)
        {
            AddModeIssue(EmailIssueCode.ArcHeaderMalformed, "The ARC instance must be an integer from 1 through 50.", tag.GetSourcePosition(), "The original instance value was retained.");
        }
    }

    void ValidateAlgorithm(EmailSignatureTag? tag, bool arc)
    {
        if (tag == null)
        {
            return;
        }

        string value = tag.GetCompactValue();
        int separator = value.IndexOf('-');
        if (separator <= 0 || separator == value.Length - 1 || !IsHyphenatedWord(value))
        {
            AddHeaderIssue(arc, "The signing algorithm tag is malformed.", tag.GetSourcePosition());
        }
    }

    void ValidateBase64(EmailSignatureTag? tag, bool arc)
    {
        if (tag == null)
        {
            return;
        }

        string value = tag.GetCompactValue();
        bool valid = value.Length > 0;
        bool paddingStarted = false;
        int padding = 0;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '=')
            {
                paddingStarted = true;
                padding++;
                if (padding > 2)
                {
                    valid = false;
                }
            }
            else
            {
                if (paddingStarted || !IsBase64Character(current))
                {
                    valid = false;
                }
            }

            index++;
        }

        if (value.Length % 4 != 0)
        {
            valid = false;
        }

        if (!valid)
        {
            AddHeaderIssue(arc, "A signature base64 tag is malformed.", tag.GetSourcePosition());
        }
    }

    void ValidateCanonicalization(EmailSignatureTag? tag, bool arc)
    {
        if (tag == null)
        {
            return;
        }

        string value = tag.GetCompactValue();
        int slash = value.IndexOf('/');
        bool valid;
        if (slash < 0)
        {
            valid = IsHyphenatedWord(value);
        }
        else
        {
            valid = slash > 0 && slash < value.Length - 1 && value.IndexOf('/', slash + 1) < 0 && IsHyphenatedWord(value.Substring(0, slash)) && IsHyphenatedWord(value.Substring(slash + 1));
        }

        if (!valid)
        {
            AddHeaderIssue(arc, "The canonicalization tag is malformed.", tag.GetSourcePosition());
        }
    }

    void ValidateHeaderList(EmailSignatureTag? tag, bool arc)
    {
        if (tag == null)
        {
            return;
        }

        string value = tag.GetValue();
        string[] names = value.Split(':');
        bool valid = names.Length > 0;
        int index = 0;
        while (index < names.Length)
        {
            if (!IsFieldName(names[index].Trim()))
            {
                valid = false;
            }

            index++;
        }

        if (!valid)
        {
            AddHeaderIssue(arc, "The signed-header list is malformed.", tag.GetSourcePosition());
        }
    }

    void ValidateUnsigned(EmailSignatureTag? tag, int maximumDigits, bool arc)
    {
        if (tag == null)
        {
            return;
        }

        string value = tag.GetCompactValue();
        if (value.Length == 0 || value.Length > maximumDigits || !IsDigits(value))
        {
            AddHeaderIssue(arc, "A numeric signature tag is malformed.", tag.GetSourcePosition());
        }
    }

    void ValidateNonEmpty(EmailSignatureTag? tag, string description, bool arc)
    {
        if (tag != null && tag.GetCompactValue().Length == 0)
        {
            AddHeaderIssue(arc, description, tag.GetSourcePosition());
        }
    }

    void Require(EmailSignatureTagList tags, string name, EmailIssueCode code)
    {
        if (!tags.Contains(name))
        {
            AddModeIssue(code, "The required '" + name + "' tag is missing.", initialPosition, "The partial structured value and original bytes were retained.");
        }
    }

    void AddHeaderIssue(bool arc, string description, EmailSourcePosition position)
    {
        AddModeIssue(arc ? EmailIssueCode.ArcHeaderMalformed : EmailIssueCode.DkimSignatureMalformed, description, position, "The original tag value was retained.");
    }

    void AddModeIssue(EmailIssueCode code, string description, EmailSourcePosition? position, string recovery)
    {
        EmailIssueSeverity severity = options.GetMode() == EmailStructuredValueParseMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, position, null, null, recovery));
    }

    void AddFatal(string description, EmailSourcePosition position, string recovery)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.SignatureValueLimitExceeded, description, position, null, null, recovery));
    }

    string NormalizeValue(int start, int end, bool compact)
    {
        StringBuilder result = new StringBuilder();
        bool whitespace = false;
        int index = start;
        while (index < end)
        {
            byte current = input[index];
            if (IsWhitespace(current))
            {
                whitespace = true;
            }
            else
            {
                if (whitespace && !compact && result.Length > 0)
                {
                    result.Append(' ');
                }

                if (current < 128)
                {
                    result.Append((char)current);
                }
                else
                {
                    int sequenceEnd = index + 1;
                    while (sequenceEnd < end && input[sequenceEnd] >= 128)
                    {
                        sequenceEnd++;
                    }

                    result.Append(Encoding.UTF8.GetString(input, index, sequenceEnd - index));
                    index = sequenceEnd - 1;
                }

                whitespace = false;
            }

            index++;
        }

        return result.ToString();
    }

    bool ValidateValueBytes(int start, int end)
    {
        int index = start;
        while (index < end)
        {
            byte current = input[index];
            if (current == 13)
            {
                if (index + 2 >= end || input[index + 1] != 10 || input[index + 2] != 32 && input[index + 2] != 9)
                {
                    return false;
                }

                index += 2;
            }
            else if (current == 10 || current < 32 && current != 9 || current == 59 || current == 127)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    int SkipCfws(int start, int end)
    {
        int cursor = start;
        bool moved = true;
        while (moved)
        {
            moved = false;
            int whitespaceEnd = SkipWhitespace(cursor, end);
            if (whitespaceEnd != cursor)
            {
                cursor = whitespaceEnd;
                moved = true;
            }

            if (cursor < end && input[cursor] == 40)
            {
                int commentEnd = SkipComment(cursor, end);
                if (commentEnd > cursor)
                {
                    cursor = commentEnd;
                    moved = true;
                }
            }
        }

        return cursor;
    }

    int TrimCfwsEnd(int start, int end)
    {
        int cursor = TrimWhitespaceEnd(start, end);
        while (cursor > start && input[cursor - 1] == 41)
        {
            int opening = FindCommentOpening(start, cursor);
            if (opening < start)
            {
                break;
            }

            cursor = TrimWhitespaceEnd(start, opening);
        }

        return cursor;
    }

    int SkipComment(int start, int end)
    {
        int depth = 0;
        bool escaped = false;
        int cursor = start;
        while (cursor < end)
        {
            byte current = input[cursor];
            if (escaped)
            {
                escaped = false;
            }
            else if (current == 92)
            {
                escaped = true;
            }
            else if (current == 40)
            {
                depth++;
            }
            else if (current == 41)
            {
                depth--;
                if (depth == 0)
                {
                    return cursor + 1;
                }
            }

            cursor++;
        }

        return start;
    }

    int FindCommentOpening(int start, int end)
    {
        int depth = 0;
        int cursor = end - 1;
        while (cursor >= start)
        {
            if (input[cursor] == 41)
            {
                depth++;
            }
            else if (input[cursor] == 40)
            {
                depth--;
                if (depth == 0)
                {
                    return cursor;
                }
            }

            cursor--;
        }

        return -1;
    }

    int SkipWhitespace(int start, int end)
    {
        int cursor = start;
        while (cursor < end && IsWhitespace(input[cursor]))
        {
            cursor++;
        }

        return cursor;
    }

    int TrimWhitespaceEnd(int start, int end)
    {
        int cursor = end;
        while (cursor > start && IsWhitespace(input[cursor - 1]))
        {
            cursor--;
        }

        return cursor;
    }

    EmailSourcePosition PositionAt(int offset)
    {
        EmailSourceTracker tracker = new EmailSourceTrackerOperations().CreateWithInitialPosition(initialPosition);
        tracker.Advance(new ReadOnlySpan<byte>(input, 0, offset));
        return tracker.GetPosition();
    }

    byte[] Slice(int start, int end)
    {
        byte[] value = new byte[end - start];
        Array.Copy(input, start, value, 0, value.Length);
        return value;
    }

    static int Count(List<EmailSignatureTag> tags, string name)
    {
        int count = 0;
        int index = 0;
        while (index < tags.Count)
        {
            if (string.Equals(tags[index].GetName(), name, StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }

            index++;
        }

        return count;
    }

    static bool IsWhitespace(byte value)
    {
        return value == 32 || value == 9 || value == 13 || value == 10;
    }

    static bool IsAlpha(byte value)
    {
        return value >= 65 && value <= 90 || value >= 97 && value <= 122;
    }

    static bool IsNameByte(byte value)
    {
        return IsAlpha(value) || value >= 48 && value <= 57 || value == 95;
    }

    static bool IsBase64Character(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z' || value >= '0' && value <= '9' || value == '+' || value == '/';
    }

    static bool IsDigits(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static int ParseSmallNumber(string value)
    {
        int number = 0;
        int index = 0;
        while (index < value.Length)
        {
            number = number * 10 + value[index] - '0';
            index++;
        }

        return number;
    }

    static bool IsHyphenatedWord(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (!(current >= 'A' && current <= 'Z' || current >= 'a' && current <= 'z' || current >= '0' && current <= '9' || current == '-'))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool IsFieldName(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current < 33 || current > 126 || current == ':')
            {
                return false;
            }

            index++;
        }

        return true;
    }
}
