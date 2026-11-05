using System;
using System.Collections.Generic;
using System.IO;

namespace email;
internal class EmailReportFieldBlockParserWorker
{
    byte[] input;
    EmailReportParseOptions options;
    EmailSourcePosition initialPosition;
    List<EmailReportFieldBlock> blocks;
    EmailIssueList issues;
    int totalFieldCount;

    internal EmailReportFieldBlockParserWorker(byte[] input, EmailReportParseOptions options, EmailSourcePosition initialPosition)
    {
        this.input = input;
        this.options = options;
        this.initialPosition = initialPosition;
        blocks = new List<EmailReportFieldBlock>();
        issues = new EmailIssueList();
        totalFieldCount = 0;
    }

    internal EmailReportFieldBlockParseResult Parse()
    {
        EmailSourceTracker tracker = new EmailSourceTrackerOperations().CreateWithInitialPosition(initialPosition);
        EmailSourcePosition blockPosition = tracker.GetPosition();
        int blockStart = 0;
        int lineStart = 0;
        int index = 0;
        while (index < input.Length)
        {
            int lineEnd = FindLineEnd(index);
            if (lineEnd < 0)
            {
                index++;
                continue;
            }

            int contentEnd = input[index] == 13 || input[index] == 10 ? index : lineEnd;
            CheckLineLength(lineStart, lineEnd);
            tracker.Advance(new ReadOnlySpan<byte>(input, lineStart, lineEnd - lineStart));
            if (contentEnd == lineStart)
            {
                if (lineStart > blockStart)
                {
                    AddBlock(blockStart, lineStart, lineStart, lineEnd, blockPosition);
                }

                blockStart = lineEnd;
                blockPosition = tracker.GetPosition();
            }

            index = lineEnd;
            lineStart = lineEnd;
        }

        if (lineStart < input.Length)
        {
            CheckLineLength(lineStart, input.Length);
            tracker.Advance(new ReadOnlySpan<byte>(input, lineStart, input.Length - lineStart));
        }

        if (blockStart < input.Length)
        {
            AddBlock(blockStart, input.Length, input.Length, input.Length, blockPosition);
        }

        return new EmailReportFieldBlockParseResult(blocks, issues, input.Length);
    }

    int FindLineEnd(int index)
    {
        if (input[index] == 13)
        {
            if (index + 1 < input.Length && input[index + 1] == 10) return index + 2;
            return index + 1;
        }

        return input[index] == 10 ? index + 1 : -1;
    }

    void CheckLineLength(int start, int end)
    {
        int length = end - start;
        if (length > options.GetMaximumLineBytes()) throw new EmailLimitException("MaxReportLineBytes", options.GetMaximumLineBytes(), length);
    }

    void AddBlock(int start, int end, int separatorStart, int separatorEnd, EmailSourcePosition position)
    {
        int length = end - start;
        if (length > options.GetMaximumFieldBlockBytes()) throw new EmailLimitException("MaxReportFieldBlockBytes", options.GetMaximumFieldBlockBytes(), length);
        if (blocks.Count >= options.GetMaximumFieldBlockCount()) throw new EmailLimitException("MaxReportFieldBlocks", options.GetMaximumFieldBlockCount(), blocks.Count + 1);
        byte[] raw = Slice(start, length);
        byte[] separator = Slice(separatorStart, separatorEnd - separatorStart);
        byte[] parserInput = CreateParserInput(raw, separator);
        EmailHeaderBlockParserOptions parserOptions = CreateHeaderOptions(parserInput.Length);
        EmailHeaderBlockParseResult parsed = new EmailHeaderBlockParser().ParseWithInputAndOptionsAndInitialPositionAndEndOfInputAndFieldCountLimitNameAndReportedFieldCountLimitAndReportedFieldCountObserved(parserInput, parserOptions, position, true, "MaxReportFieldsPerBlock", options.GetMaximumFieldsPerBlock(), (long)options.GetMaximumFieldsPerBlock() + 1);

        if (parsed.WasLimitReached())
        {
            throw new EmailLimitException("MaxReportFieldsPerBlock", options.GetMaximumFieldsPerBlock(), (long)options.GetMaximumFieldsPerBlock() + 1);
        }

        int observedFields = parsed.GetHeaders().GetCount();
        long total = (long)totalFieldCount + observedFields;
        if (total > options.GetMaximumFieldCount()) throw new EmailLimitException("MaxReportFields", options.GetMaximumFieldCount(), total);
        totalFieldCount = (int)total;
        EmailIssueList blockIssues = parsed.GetIssues();
        if (HasMalformedFieldIssue(blockIssues))
        {
            EmailIssueSeverity severity = options.GetMode() == EmailStructuredValueParseMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
            blockIssues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, EmailIssueCode.ReportFieldBlockMalformed, "A report field block contains malformed field syntax.", position, null, null, "Usable fields and their original bytes were retained."));
        }

        if (!parsed.IsComplete())
        {
            blockIssues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, EmailIssueCode.ReportFieldBlockMalformed, "A report field block could not be parsed completely.", position, null, null, "All complete fields were retained."));
        }

        issues.AddRange(blockIssues);
        blocks.Add(new EmailReportFieldBlock(parsed.GetHeaders(), raw, separator, position, blockIssues));
    }

    static bool HasMalformedFieldIssue(EmailIssueList values)
    {
        return values.ContainsCode(EmailIssueCode.HeaderMalformed)
            || values.ContainsCode(EmailIssueCode.HeaderNameMalformed)
            || values.ContainsCode(EmailIssueCode.HeaderContinuationWithoutField)
            || values.ContainsCode(EmailIssueCode.HeaderNullByte)
            || values.ContainsCode(EmailIssueCode.HeaderMissingColon);
    }

    EmailHeaderBlockParserOptions CreateHeaderOptions(int parserInputBytes)
    {
        EmailParsingLimits limits = new EmailParsingLimits();
        limits.SetMaxHeaderBytes(Math.Max(1, parserInputBytes));
        limits.SetMaxHeaderFieldCount(options.GetMaximumFieldsPerBlock());
        limits.SetMaxLineBytes(options.GetMaximumLineBytes());
        EmailHeaderBlockParserOptions result = new EmailHeaderBlockParserOptions();
        result.SetLimits(limits);
        result.SetMode(options.GetMode());
        return result;
    }

    static byte[] CreateParserInput(byte[] raw, byte[] separator)
    {
        if (separator.Length > 0) return Join(raw, separator);
        if (raw.Length >= 2 && raw[raw.Length - 2] == 13 && raw[raw.Length - 1] == 10) return Join(raw, new byte[] { 13, 10 });
        if (raw.Length >= 1 && raw[raw.Length - 1] == 10) return Join(raw, new byte[] { 10 });
        if (raw.Length >= 1 && raw[raw.Length - 1] == 13) return Join(raw, new byte[] { 13 });
        return Join(raw, new byte[] { 13, 10, 13, 10 });
    }

    byte[] Slice(int start, int length)
    {
        byte[] result = new byte[length];
        Array.Copy(input, start, result, 0, length);
        return result;
    }

    static byte[] Join(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        first.CopyTo(result, 0);
        second.CopyTo(result, first.Length);
        return result;
    }
}
