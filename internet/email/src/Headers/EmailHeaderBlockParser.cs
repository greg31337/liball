using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailHeaderBlockParserWorker
{
    const int MaximumInternetHeaderLineBytes = 998;
    static readonly string[] singletonNames = new string[]
    {
        "Return-Path", "Date", "From", "Sender", "Reply-To", "To", "Cc", "Bcc", "Message-ID", "In-Reply-To", "References", "Subject", "MIME-Version", "Content-Type", "Content-Transfer-Encoding", "Content-ID", "Content-Description", "Content-Disposition", "Content-Language", "Priority", "X-Priority", "Importance", "Sensitivity", "Auto-Submitted", "List-Help", "List-Unsubscribe", "List-Subscribe", "List-Post", "List-Owner", "List-Archive", "List-Unsubscribe-Post"
    };

    byte[] input;
    int sourceByteCount;
    EmailHeaderBlockParserOptions options;
    EmailParsingLimits limits;
    EmailSourcePosition initialPosition;
    bool endOfInput;
    List<EmailHeader> headers;
    EmailIssueList issues;
    EmailSourceTracker tracker;
    EmailHeaderBlockBuilder? pending;
    int offset;
    string fieldCountLimitName;
    long reportedFieldCountLimit;
    long reportedFieldCountObserved;

    internal EmailHeaderBlockParserWorker(byte[] input, int sourceByteCount, EmailHeaderBlockParserOptions options, EmailSourcePosition initialPosition, bool endOfInput, string fieldCountLimitName, long reportedFieldCountLimit, long reportedFieldCountObserved)
    {
        this.input = input;
        this.sourceByteCount = sourceByteCount;
        this.options = options;
        limits = options.GetLimits();
        this.initialPosition = initialPosition;
        this.endOfInput = endOfInput;
        this.fieldCountLimitName = fieldCountLimitName;
        this.reportedFieldCountLimit = reportedFieldCountLimit;
        this.reportedFieldCountObserved = reportedFieldCountObserved;
        headers = new List<EmailHeader>();
        issues = new EmailIssueList();
        tracker = new EmailSourceTrackerOperations().CreateWithInitialPosition(initialPosition);
        pending = null;
        offset = 0;
    }

    internal EmailHeaderBlockParseResult Parse()
    {
        while (offset < input.Length)
        {
            EmailHeaderLimitObservation? observation = FindLimitBeforeNextLine();
            if (observation != null)
            {
                FinalizePending();
                AddLimitIssue(observation);
                return CreateResult(EmailHeaderBlockParseStatus.LimitReached, offset, EmailLineEnding.None);
            }

            EmailHeaderPhysicalLine? line = ReadLine();
            if (line == null)
            {
                return CreateResult(EmailHeaderBlockParseStatus.NeedMoreData, offset, EmailLineEnding.None);
            }

            EmailSourcePosition linePosition = tracker.GetPosition();
            if (WouldExceedFieldCount(line))
            {
                FinalizePending();
                long maximum = limits.GetMaxHeaderFieldCount();
                EmailHeaderLimitObservation fieldObservation = new EmailHeaderLimitObservation("MaxHeaderFieldCount", maximum, maximum + 1, linePosition);
                AddLimitIssue(fieldObservation);
                return CreateResult(EmailHeaderBlockParseStatus.LimitReached, offset, EmailLineEnding.None);
            }

            if (line.GetContentLength() == 0)
            {
                FinalizePending();
                AddLineEndingIssue(line, linePosition, null);
                Advance(line);
                return CreateResult(EmailHeaderBlockParseStatus.Complete, offset, line.GetLineEnding());
            }

            if (IsLikelyMissingSeparator(line))
            {
                EmailIssueSeverity severity = GetPolicySeverity(options.GetCompatibilityOptions().GetAllowMissingHeaderBodySeparator());
                EmailIssue issue = new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, EmailIssueCode.HeaderBlockIncomplete, "The header/body separator is missing before a line that appears to begin the message body.", linePosition, null, null, "The preceding fields were retained as headers and this line begins the recovered body.");
                AddIssue(issue, pending);
                FinalizePending();
                return CreateResult(EmailHeaderBlockParseStatus.Recovered, offset, EmailLineEnding.None);
            }

            ProcessLine(line, linePosition);
            Advance(line);
        }

        if (!endOfInput)
        {
            return CreateResult(EmailHeaderBlockParseStatus.NeedMoreData, offset, EmailLineEnding.None);
        }

        FinalizePending();
        EmailSourcePosition position = tracker.GetPosition();
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, EmailIssueCode.HeaderBlockIncomplete, "The header block ended before a blank header/body separator line.", position, null, null, "All complete header fields before end of input were retained."));
        return CreateResult(EmailHeaderBlockParseStatus.EndOfInput, offset, EmailLineEnding.None);
    }

    void ProcessLine(EmailHeaderPhysicalLine line, EmailSourcePosition linePosition)
    {
        byte[] content = line.GetContentBytes();
        if (content[0] == 32 || content[0] == 9)
        {
            ProcessContinuation(line, linePosition, true);
        }
        else
        {
            int colon = FindColon(content);
            if (colon >= 0)
            {
                StartHeader(line, linePosition, colon);
            }
            else if (pending != null)
            {
                ProcessContinuation(line, linePosition, false);
            }
            else
            {
                StartMissingColonHeader(line, linePosition);
            }
        }

        AddLineEndingIssue(line, linePosition, pending);
        AddNullIssue(content, linePosition, pending);
        AddOverlongLineIssue(line, linePosition, pending);
    }

    void ProcessContinuation(EmailHeaderPhysicalLine line, EmailSourcePosition linePosition, bool hasWhitespace)
    {
        if (pending == null)
        {
            EnsureFieldCount();
            pending = EmailHeaderBlockBuilder.CreateOrphan(line.GetContentBytes(), linePosition, GetAbsoluteOffset(line.GetEndOffset()), line.GetLineEnding(), options.GetMode());
            EmailIssueSeverity severity = GetPolicySeverity(options.GetCompatibilityOptions().GetAllowOrphanHeaderContinuations());
            EmailIssue orphan = new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, EmailIssueCode.HeaderContinuationWithoutField, "A header continuation appeared before any field.", linePosition, null, null, "The continuation bytes were retained in a malformed unnamed header.");
            AddIssue(orphan, pending);
            return;
        }

        pending.AddContinuation(line.GetContentBytes(), GetAbsoluteOffset(line.GetEndOffset()), line.GetLineEnding());
        if (!hasWhitespace)
        {
            EmailIssue missing = new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(GetMalformedSeverity(), EmailIssueCode.HeaderMissingColon, "A physical header line has no field-name separator and no leading continuation whitespace.", linePosition, null, pending.GetFieldName(), "The line was retained as a noncanonical continuation of the preceding field.");
            AddIssue(missing, pending);
        }
    }

    void StartHeader(EmailHeaderPhysicalLine line, EmailSourcePosition linePosition, int colon)
    {
        FinalizePending();
        EnsureFieldCount();
        byte[] content = line.GetContentBytes();
        byte[] name = Slice(content, 0, colon);
        byte[] value = Slice(content, colon + 1, content.Length - colon - 1);
        bool validName = IsValidName(name);
        EmailSourcePosition valuePosition = MovePosition(linePosition, colon + 1);
        EmailHeaderValueKind kind = validName ? new EmailHeaderRegistry().GetValueKind(Encoding.ASCII.GetString(name)) : EmailHeaderValueKind.Unknown;
        pending = new EmailHeaderBlockBuilder(name, value, linePosition.GetByteOffset(), GetAbsoluteOffset(line.GetEndOffset()), line.GetLineEnding(), valuePosition, kind, options.GetMode());
        if (!validName)
        {
            EmailIssueSeverity severity = GetPolicySeverity(options.GetCompatibilityOptions().GetAllowMalformedHeaderNames());
            EmailIssue malformed = new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, EmailIssueCode.HeaderNameMalformed, "A header field name is empty or contains a byte outside the RFC field-name range.", linePosition, null, null, "The original field-name bytes and value were retained with an unknown value kind.");
            AddIssue(malformed, pending);
        }

        string? fieldName = pending.GetFieldName();
        if (fieldName != null && IsSingletonName(fieldName) && ContainsHeader(fieldName))
        {
            EmailIssueSeverity severity = GetPolicySeverity(options.GetCompatibilityOptions().GetAllowDuplicateSingletonHeaders());
            EmailIssue duplicate = new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, EmailIssueCode.HeaderDuplicateSingleton, "A singleton header field occurs more than once.", linePosition, null, fieldName, "Every occurrence was retained and the first supplies effective parser metadata.");
            AddIssue(duplicate, pending);
        }
    }

    void StartMissingColonHeader(EmailHeaderPhysicalLine line, EmailSourcePosition linePosition)
    {
        EnsureFieldCount();
        byte[] content = line.GetContentBytes();
        EmailSourcePosition valuePosition = MovePosition(linePosition, content.Length);
        pending = EmailHeaderBlockBuilder.CreateMissingColon(content, linePosition.GetByteOffset(), GetAbsoluteOffset(line.GetEndOffset()), line.GetLineEnding(), valuePosition, options.GetMode());
        EmailIssue missing = new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(GetMalformedSeverity(), EmailIssueCode.HeaderMissingColon, "A physical header line has no field-name separator.", linePosition, null, null, "The line was retained as a malformed header with an empty value.");
        AddIssue(missing, pending);
    }

    void AddNullIssue(byte[] content, EmailSourcePosition linePosition, EmailHeaderBlockBuilder? builder)
    {
        int index = 0;
        while (index < content.Length && content[index] != 0)
        {
            index++;
        }

        if (index < content.Length)
        {
            EmailSourcePosition position = MovePosition(linePosition, index);
            string? fieldName = builder == null ? null : builder.GetFieldName();
            EmailIssue issue = new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(GetMalformedSeverity(), EmailIssueCode.HeaderNullByte, "A header line contains a NUL byte.", position, null, fieldName, "The NUL byte was retained for forensic preservation and must be rejected by safe writing.");
            AddIssue(issue, builder);
        }
    }

    void AddLineEndingIssue(EmailHeaderPhysicalLine line, EmailSourcePosition linePosition, EmailHeaderBlockBuilder? builder)
    {
        EmailLineEnding ending = line.GetLineEnding();
        if (ending == EmailLineEnding.CrLf || ending == EmailLineEnding.None)
        {
            return;
        }

        EmailSourcePosition position = MovePosition(linePosition, line.GetContentLength());
        string? fieldName = builder == null ? null : builder.GetFieldName();
        EmailIssueSeverity severity = GetPolicySeverity(options.GetCompatibilityOptions().GetAllowBareLineEndings());
        EmailIssue issue = new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, EmailIssueCode.HeaderNonCanonicalLineEnding, "A header line uses a bare CR or LF line ending.", position, null, fieldName, "The original line ending was retained.");
        AddIssue(issue, builder);
    }

    void AddOverlongLineIssue(EmailHeaderPhysicalLine line, EmailSourcePosition linePosition, EmailHeaderBlockBuilder? builder)
    {
        if (line.GetContentLength() <= MaximumInternetHeaderLineBytes)
        {
            return;
        }

        EmailSourcePosition position = MovePosition(linePosition, MaximumInternetHeaderLineBytes);
        string? fieldName = builder == null ? null : builder.GetFieldName();
        EmailIssue issue = new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(GetMalformedSeverity(), EmailIssueCode.HeaderLineTooLong, "A header line exceeds the Internet message limit of 998 bytes.", position, null, fieldName, "The complete overlong line was retained within the configured hard parsing limit.");
        AddIssue(issue, builder);
    }

    void AddIssue(EmailIssue issue, EmailHeaderBlockBuilder? builder)
    {
        issues.Add(issue);
        if (builder != null)
        {
            builder.AddIssue(issue);
        }
    }

    void FinalizePending()
    {
        if (pending != null)
        {
            EmailHeader header = pending.Build();
            EmailCompatibilityOptions compatibility = options.GetCompatibilityOptions();
            if (options.GetMode() == EmailStructuredValueParseMode.Strict)
            {
                compatibility.SetAllowBrokenEncodedWords(false);
                compatibility.SetAllowBrokenRfc2231Parameters(false);
            }

            header.SetCompatibilityOptions(compatibility);
            headers.Add(header);
            pending = null;
        }
    }

    void EnsureFieldCount()
    {
        long observed = (long)headers.Count + 1;
        if (observed > limits.GetMaxHeaderFieldCount())
        {
            throw new EmailLimitException(fieldCountLimitName, reportedFieldCountLimit, reportedFieldCountObserved == 0 ? observed : reportedFieldCountObserved);
        }
    }

    EmailHeaderLimitObservation? FindLimitBeforeNextLine()
    {
        if (!options.GetCompatibilityOptions().GetReturnPartialResults())
        {
            return null;
        }

        int index = offset;
        int contentLength = 0;
        while (index < input.Length)
        {
            long headerObserved = (long)index + 1;
            if (headerObserved > limits.GetMaxHeaderBytes())
            {
                EmailSourcePosition position = MovePosition(tracker.GetPosition(), index - offset);
                return new EmailHeaderLimitObservation("MaxHeaderBytes", limits.GetMaxHeaderBytes(), headerObserved, position);
            }

            byte value = input[index];
            if (value == 13)
            {
                if (index + 1 < input.Length && input[index + 1] == 10 && (long)index + 2 > limits.GetMaxHeaderBytes())
                {
                    EmailSourcePosition position = MovePosition(tracker.GetPosition(), index - offset + 1);
                    return new EmailHeaderLimitObservation("MaxHeaderBytes", limits.GetMaxHeaderBytes(), (long)index + 2, position);
                }

                return null;
            }

            if (value == 10)
            {
                return null;
            }

            contentLength++;
            if (contentLength > limits.GetMaxLineBytes())
            {
                EmailSourcePosition position = MovePosition(tracker.GetPosition(), contentLength - 1);
                return new EmailHeaderLimitObservation("MaxLineBytes", limits.GetMaxLineBytes(), contentLength, position);
            }

            index++;
        }

        return null;
    }

    bool WouldExceedFieldCount(EmailHeaderPhysicalLine line)
    {
        if (!options.GetCompatibilityOptions().GetReturnPartialResults() || line.GetContentLength() == 0)
        {
            return false;
        }

        byte[] content = line.GetContentBytes();
        bool newField = false;
        if (content[0] == 32 || content[0] == 9)
        {
            newField = pending == null;
        }
        else if (FindColon(content) >= 0)
        {
            newField = true;
        }
        else
        {
            newField = pending == null;
        }

        if (!newField)
        {
            return false;
        }

        long currentCount = headers.Count;
        if (pending != null)
        {
            currentCount++;
        }

        return currentCount + 1 > limits.GetMaxHeaderFieldCount();
    }

    void AddLimitIssue(EmailHeaderLimitObservation observation)
    {
        string description = "Header parsing stopped at the recoverable " + observation.GetLimitName() + " limit of " + observation.GetLimit() + " after observing " + observation.GetObserved() + ".";
        string recovery = "Every complete header field before the limit was retained in a deterministic partial result.";
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, EmailIssueCode.LimitExceeded, description, observation.GetPosition(), null, null, recovery));
    }

    EmailHeaderPhysicalLine? ReadLine()
    {
        int start = offset;
        int index = start;
        while (index < input.Length)
        {
            EnsureHeaderByteCount((long)index + 1);
            byte value = input[index];
            if (value == 13)
            {
                if (index + 1 < input.Length && input[index + 1] == 10)
                {
                    EnsureHeaderByteCount((long)index + 2);
                    return CreateLine(start, index, index + 2, EmailLineEnding.CrLf);
                }

                if (index + 1 == input.Length && !endOfInput)
                {
                    return null;
                }

                return CreateLine(start, index, index + 1, EmailLineEnding.Cr);
            }

            if (value == 10)
            {
                return CreateLine(start, index, index + 1, EmailLineEnding.Lf);
            }

            int contentLength = index - start + 1;
            if (contentLength > limits.GetMaxLineBytes())
            {
                throw new EmailLimitException("MaxLineBytes", limits.GetMaxLineBytes(), contentLength);
            }

            index++;
        }

        if (!endOfInput)
        {
            return null;
        }

        return CreateLine(start, input.Length, input.Length, EmailLineEnding.None);
    }

    EmailHeaderPhysicalLine CreateLine(int start, int contentEnd, int end, EmailLineEnding ending)
    {
        EnsureHeaderByteCount(end);
        byte[] content = Slice(input, start, contentEnd - start);
        return new EmailHeaderPhysicalLine(content, start, end, ending);
    }

    void EnsureHeaderByteCount(long observed)
    {
        if (observed > limits.GetMaxHeaderBytes())
        {
            throw new EmailLimitException("MaxHeaderBytes", limits.GetMaxHeaderBytes(), observed);
        }
    }

    void Advance(EmailHeaderPhysicalLine line)
    {
        int length = line.GetEndOffset() - offset;
        tracker.Advance(input.AsSpan(offset, length));
        offset = line.GetEndOffset();
    }

    EmailHeaderBlockParseResult CreateResult(EmailHeaderBlockParseStatus status, int consumed, EmailLineEnding separator)
    {
        EmailHeaders parsedHeaders = new EmailHeadersOperations().CreateWithPreservedHeaders(headers);
        return new EmailHeaderBlockParseResult(status, parsedHeaders, issues, consumed, sourceByteCount, separator);
    }

    EmailIssueSeverity GetMalformedSeverity()
    {
        return options.GetMode() == EmailStructuredValueParseMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
    }

    EmailIssueSeverity GetPolicySeverity(bool allowed)
    {
        if (options.GetMode() == EmailStructuredValueParseMode.Strict || !allowed)
        {
            return EmailIssueSeverity.Error;
        }

        return EmailIssueSeverity.Warning;
    }

    bool IsLikelyMissingSeparator(EmailHeaderPhysicalLine line)
    {
        if (!options.GetCompatibilityOptions().GetAllowMissingHeaderBodySeparator())
        {
            return false;
        }

        byte[] content = line.GetContentBytes();
        if (content.Length == 0 || content[0] == 32 || content[0] == 9 || FindColon(content) >= 0)
        {
            return false;
        }

        return pending != null || headers.Count > 0;
    }

    bool ContainsHeader(string name)
    {
        int index = 0;
        while (index < headers.Count)
        {
            if (string.Equals(headers[index].GetName(), name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    long GetAbsoluteOffset(int relativeOffset)
    {
        long baseOffset = initialPosition.GetByteOffset();
        if (relativeOffset > long.MaxValue - baseOffset)
        {
            throw new OverflowException("The header source offset exceeds Int64.MaxValue.");
        }

        return baseOffset + relativeOffset;
    }

    static int FindColon(byte[] content)
    {
        int index = 0;
        while (index < content.Length)
        {
            if (content[index] == 58)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    static bool IsValidName(byte[] name)
    {
        if (name.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < name.Length)
        {
            if (name[index] < 33 || name[index] > 126 || name[index] == 58)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool IsSingletonName(string name)
    {
        int index = 0;
        while (index < singletonNames.Length)
        {
            if (string.Equals(singletonNames[index], name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static EmailSourcePosition MovePosition(EmailSourcePosition position, int byteCount)
    {
        long offset = position.GetByteOffset();
        long column = position.GetColumnNumber();
        if (byteCount > long.MaxValue - offset || byteCount > long.MaxValue - column)
        {
            throw new OverflowException("The header source position exceeds Int64.MaxValue.");
        }

        return new EmailSourcePosition(offset + byteCount, position.GetLineNumber(), column + byteCount);
    }

    static byte[] Slice(byte[] source, int start, int length)
    {
        byte[] result = new byte[length];
        int index = 0;
        while (index < length)
        {
            result[index] = source[start + index];
            index++;
        }

        return result;
    }
}
internal class EmailHeaderLimitObservation
{
    string limitName;
    long limit;
    long observed;
    EmailSourcePosition position;

    internal EmailHeaderLimitObservation(string limitName, long limit, long observed, EmailSourcePosition position)
    {
        this.limitName = limitName;
        this.limit = limit;
        this.observed = observed;
        this.position = position;
    }

    internal string GetLimitName()
    {
        return limitName;
    }

    internal long GetLimit()
    {
        return limit;
    }

    internal long GetObserved()
    {
        return observed;
    }

    internal EmailSourcePosition GetPosition()
    {
        return position;
    }
}
internal class EmailHeaderPhysicalLine
{
    byte[] content;
    int startOffset;
    int endOffset;
    EmailLineEnding lineEnding;

    internal EmailHeaderPhysicalLine(byte[] content, int startOffset, int endOffset, EmailLineEnding lineEnding)
    {
        this.content = content;
        this.startOffset = startOffset;
        this.endOffset = endOffset;
        this.lineEnding = lineEnding;
    }

    internal int GetContentLength()
    {
        return content.Length;
    }

    internal byte[] GetContentBytes()
    {
        byte[] copy = new byte[content.Length];
        content.CopyTo(copy, 0);
        return copy;
    }

    internal int GetStartOffset()
    {
        return startOffset;
    }

    internal int GetEndOffset()
    {
        return endOffset;
    }

    internal EmailLineEnding GetLineEnding()
    {
        return lineEnding;
    }
}
internal class EmailHeaderBlockBuilder
{
    byte[] name;
    List<byte> rawValue;
    long startOffset;
    long endOffset;
    EmailLineEnding lineEnding;
    EmailSourcePosition valuePosition;
    EmailHeaderValueKind valueKind;
    EmailStructuredValueParseMode mode;
    EmailIssueList issues;
    List<byte> originalFieldBytes;

    internal EmailHeaderBlockBuilder(byte[] name, byte[] value, long startOffset, long endOffset, EmailLineEnding lineEnding, EmailSourcePosition valuePosition, EmailHeaderValueKind valueKind, EmailStructuredValueParseMode mode)
    {
        this.name = Copy(name);
        rawValue = new List<byte>(value);
        this.startOffset = startOffset;
        this.endOffset = endOffset;
        this.lineEnding = lineEnding;
        this.valuePosition = valuePosition;
        this.valueKind = valueKind;
        this.mode = mode;
        issues = new EmailIssueList();
        originalFieldBytes = new List<byte>();
        AddBytes(originalFieldBytes, name);
        originalFieldBytes.Add(58);
        AddBytes(originalFieldBytes, value);
        AddLineEnding(originalFieldBytes, lineEnding);
    }

    internal static EmailHeaderBlockBuilder CreateOrphan(byte[] content, EmailSourcePosition position, long endOffset, EmailLineEnding lineEnding, EmailStructuredValueParseMode mode)
    {
        EmailHeaderBlockBuilder builder = new EmailHeaderBlockBuilder(new byte[0], content, position.GetByteOffset(), endOffset, lineEnding, position, EmailHeaderValueKind.Unknown, mode);
        builder.SetOriginalWithoutColon(content, lineEnding);
        return builder;
    }

    internal static EmailHeaderBlockBuilder CreateMissingColon(byte[] content, long startOffset, long endOffset, EmailLineEnding lineEnding, EmailSourcePosition valuePosition, EmailStructuredValueParseMode mode)
    {
        EmailHeaderBlockBuilder builder = new EmailHeaderBlockBuilder(content, new byte[0], startOffset, endOffset, lineEnding, valuePosition, EmailHeaderValueKind.Unknown, mode);
        builder.SetOriginalWithoutColon(content, lineEnding);
        return builder;
    }

    internal string? GetFieldName()
    {
        if (name.Length == 0)
        {
            return null;
        }

        return Encoding.UTF8.GetString(name);
    }

    internal void AddContinuation(byte[] content, long newEndOffset, EmailLineEnding newLineEnding)
    {
        AddLineEnding(rawValue, lineEnding);
        int index = 0;
        while (index < content.Length)
        {
            rawValue.Add(content[index]);
            index++;
        }

        endOffset = newEndOffset;
        lineEnding = newLineEnding;
        AddBytes(originalFieldBytes, content);
        AddLineEnding(originalFieldBytes, newLineEnding);
    }

    internal void AddIssue(EmailIssue issue)
    {
        issues.Add(issue);
    }

    internal EmailHeader Build()
    {
        EmailRange range = new EmailRange(startOffset, endOffset - startOffset);
        return new EmailHeaderOperations().CreateWithOriginalNameBytesAndOriginalRawValueAndLineEndingAndSourceRangeAndValuePositionAndValueKindAndParseModeAndIssuesAndExactOriginalRawFieldBytes(name, rawValue.ToArray(), lineEnding, range, valuePosition, valueKind, mode, issues, originalFieldBytes.ToArray());
    }

    void SetOriginalWithoutColon(byte[] content, EmailLineEnding ending)
    {
        originalFieldBytes.Clear();
        AddBytes(originalFieldBytes, content);
        AddLineEnding(originalFieldBytes, ending);
    }

    static void AddLineEnding(List<byte> target, EmailLineEnding ending)
    {
        if (ending == EmailLineEnding.CrLf || ending == EmailLineEnding.Cr)
        {
            target.Add(13);
        }

        if (ending == EmailLineEnding.CrLf || ending == EmailLineEnding.Lf)
        {
            target.Add(10);
        }
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }

    static void AddBytes(List<byte> target, byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            target.Add(value[index]);
            index++;
        }
    }
}
