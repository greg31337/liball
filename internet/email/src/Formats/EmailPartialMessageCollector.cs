using System;
using System.Collections.Generic;
using System.IO;

namespace email;
internal class EmailPartialMessageAssemblyWorker
{
    string fragmentId;
    List<EmailPartialMessageFragment> fragments;
    EmailPartialMessageAssemblyOptions options;
    EmailIssueList issues;
    List<long> missing;
    List<long> duplicates;

    internal EmailPartialMessageAssemblyWorker(string fragmentId, List<EmailPartialMessageFragment> fragments, EmailPartialMessageAssemblyOptions options)
    {
        this.fragmentId = fragmentId;
        this.fragments = new List<EmailPartialMessageFragment>(fragments);
        this.options = options;
        issues = new EmailIssueList();
        missing = new List<long>();
        duplicates = new List<long>();
    }

    internal EmailPartialMessageAssemblyResult Assemble()
    {
        SortFragments();
        long expected = DetermineExpectedTotal();
        bool hasExpected = expected > 0;
        ValidateNumbers(expected);
        List<EmailPartialMessageFragmentData> data = MaterializeFragments(expected);
        bool complete = hasExpected && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
        byte[]? innerBytes = complete ? Join(data) : null;
        byte[]? reassembledBytes = null;
        EmailMessage? innerMessage = null;
        EmailMessage? message = null;
        if (innerBytes != null && options.GetReparse())
        {
            EmailPartialMessageReparseResult reparsed = Reparse(innerBytes);
            innerMessage = reparsed.GetInnerMessage();
            message = reparsed.GetMessage();
            reassembledBytes = reparsed.GetReassembledBytes();
        }

        return new EmailPartialMessageAssemblyResult(fragmentId, expected, hasExpected, complete, options.GetReparse(), fragments, missing, duplicates, innerBytes, reassembledBytes, innerMessage, message, issues);
    }

    long DetermineExpectedTotal()
    {
        long expected = 0;
        int index = 0;
        while (index < fragments.Count)
        {
            EmailPartialMessageMetadata? metadata = fragments[index].GetMetadata();
            if ((metadata ?? throw new InvalidOperationException()).HasTotalFragments())
            {
                long total = metadata.GetTotalFragments();
                if (expected == 0) expected = total;
                else if (expected != total) issues.Add(Issue(EmailIssueCode.PartialMessageTotalMismatch, "Fragments with the same id declare inconsistent total values.", "The conflicting totals were retained and no assembly was produced."));
            }

            index++;
        }

        if (expected == 0) issues.Add(Issue(EmailIssueCode.PartialMessageTotalMismatch, "No fragment declares the total number of fragments.", "The group remains incomplete until a final fragment supplies total."));
        return expected;
    }

    void ValidateNumbers(long expected)
    {
        long previous = -1;
        int index = 0;
        while (index < fragments.Count)
        {
            long number = fragments[index].GetFragmentNumber();
            if (number == previous && !Contains(duplicates, number))
            {
                duplicates.Add(number);
                issues.Add(Issue(EmailIssueCode.PartialMessageDuplicateFragment, "Fragment number " + number + " occurs more than once for the same id.", "All duplicates were retained for inspection and no ambiguous assembly was produced."));
            }

            if (expected > 0 && number > expected) issues.Add(Issue(EmailIssueCode.PartialMessageTotalMismatch, "A fragment number exceeds the declared total.", "The out-of-range fragment was retained and assembly was refused."));
            previous = number;
            index++;
        }

        if (expected <= 0) return;
        long numberToFind = 1;
        index = 0;
        while (numberToFind <= expected)
        {
            while (index < fragments.Count && fragments[index].GetFragmentNumber() < numberToFind) index++;
            if (index >= fragments.Count || fragments[index].GetFragmentNumber() != numberToFind)
            {
                missing.Add(numberToFind);
                issues.Add(Issue(EmailIssueCode.PartialMessageMissingFragment, "Fragment number " + numberToFind + " is missing.", "The incomplete ordered set was retained without assembly."));
            }

            numberToFind++;
        }

        EmailPartialMessageFragment? finalFragment = null;
        index = 0;
        while (index < fragments.Count)
        {
            if (fragments[index].GetFragmentNumber() == expected)
            {
                finalFragment = fragments[index];
                break;
            }

            index++;
        }

        if (finalFragment != null && (!finalFragment.HasTotalFragments() || finalFragment.GetTotalFragments() != expected)) issues.Add(Issue(EmailIssueCode.PartialMessageTotalMismatch, "The final fragment does not declare the matching total value.", "The final fragment was retained but the RFC-required total is needed before assembly."));
    }

    List<EmailPartialMessageFragmentData> MaterializeFragments(long expected)
    {
        List<EmailPartialMessageFragmentData> result = new List<EmailPartialMessageFragmentData>();
        long totalBytes = 0;
        int index = 0;
        while (index < fragments.Count)
        {
            EmailPartialMessageFragment fragment = fragments[index];
            MimeEntity entity = fragment.GetEntity();
            if (!entity.HasContent())
            {
                issues.Add(Issue(EmailIssueCode.PartialMessageAssemblyMalformed, "A collected fragment has no content bytes.", "The empty fragment was retained and assembly was refused."));
                result.Add(new EmailPartialMessageFragmentData(fragment, new byte[0]));
                index++;
                continue;
            }

            if (entity.HasTransferEncoding() && !string.Equals(entity.GetTransferEncodingName(), "7bit", StringComparison.OrdinalIgnoreCase)) issues.Add(Issue(EmailIssueCode.PartialMessageAssemblyMalformed, "A message/partial fragment uses a content-transfer-encoding other than 7bit.", "The raw fragment bytes were retained without decoding and assembly was refused."));
            byte[] bytes = ReadFragment(entity.GetContent());
            totalBytes = new EmailSafeArithmetic().AddNonNegative(totalBytes, bytes.Length);
            if (totalBytes > options.GetMaximumReassembledBytes()) throw new EmailLimitException("MaxReassembledBytes", options.GetMaximumReassembledBytes(), totalBytes);
            if (expected > 0 && fragment.GetFragmentNumber() < expected && !EndsWithCrlf(bytes)) issues.Add(Issue(EmailIssueCode.PartialMessageFragmentBoundaryInvalid, "A non-final fragment does not end at a CRLF line boundary.", "The fragment bytes were retained and assembly was refused."));
            result.Add(new EmailPartialMessageFragmentData(fragment, bytes));
            index++;
        }

        return result;
    }

    byte[] ReadFragment(EmailContent? content)
    {
        using (MemoryStream output = new MemoryStream())
        {
            new EmailDecodedContent().CopyToWithContentAndOutputAndMaximumBytesAndLimitNameAndReportedLimitAndReportedObserved(content, output, options.GetMaximumFragmentBytes(), "MaxPartialFragmentBytes", options.GetMaximumFragmentBytes(), (long)options.GetMaximumFragmentBytes() + 1);

            return output.ToArray();
        }
    }

    byte[] Join(List<EmailPartialMessageFragmentData> data)
    {
        using (MemoryStream output = new MemoryStream())
        {
            int index = 0;
            while (index < data.Count)
            {
                byte[] bytes = data[index].GetBytes();
                output.Write(bytes, 0, bytes.Length);
                index++;
            }

            return output.ToArray();
        }
    }

    EmailPartialMessageReparseResult Reparse(byte[] innerBytes)
    {
        EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingFunc(() => new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(innerBytes, options.GetParseOptions()));
        if (!captured.IsSuccess())
        {
            EmailLimitException? exception = captured.GetException() as EmailLimitException;
            if (exception == null) throw (captured.GetException() ?? throw new InvalidOperationException());
            issues.Add(Issue(EmailIssueCode.PartialMessageReparseFailed, "The reconstructed inner message exceeded a configured parser limit: " + exception.GetLimitName() + ".", "The ordered inner bytes remain available without a parsed message."));
            return new EmailPartialMessageReparseResult(null, null, null);
        }
        EmailParseResult? parsed = (EmailParseResult?)captured.GetValue();

        issues.AddRange((parsed ?? throw new InvalidOperationException()).GetIssues());
        if (!parsed.IsSuccess() || !parsed.HasMessage())
        {
            issues.Add(Issue(EmailIssueCode.PartialMessageReparseFailed, "The reconstructed inner bytes are not a complete valid message under the selected parse policy.", "The ordered inner bytes remain available for inspection."));
            return new EmailPartialMessageReparseResult(parsed.GetMessage(), null, null);
        }

        EmailMessage? inner = parsed.GetMessage();
        EmailMessage merged = MergeHeaders(inner);
        EmailWriteOptions writeOptions = new EmailWriteOptionsOperations().CreateCanonicalSmtpUtf8();
        writeOptions.SetMaximumOutputBytes(options.GetMaximumReassembledBytes());
        EmailWriteResult written = new EmailWriter().WriteMessageWithMessageAndOptions(merged, writeOptions);
        issues.AddRange(written.GetIssues());
        if (!written.IsSuccess())
        {
            issues.Add(Issue(EmailIssueCode.PartialMessageAssemblyMalformed, "The RFC header merge could not be serialized within the selected policy and size limits.", "The parsed inner message remains available without final reassembled bytes."));
            return new EmailPartialMessageReparseResult(inner, null, null);
        }

        return new EmailPartialMessageReparseResult(inner, merged, written.GetBytes());
    }

    EmailMessage MergeHeaders(EmailMessage? inner)
    {
        EmailHeaders merged = new EmailHeadersOperations().CreateWith();
        EmailHeaders outer = fragments[0].GetOuterHeaders();
        int index = 0;
        while (index < outer.GetCount())
        {
            EmailHeader header = outer.GetAt(index);
            if (!IsInnerHeader(header.GetName())) merged.Add(header.Clone());
            index++;
        }

        EmailHeaders? innerHeaders = (inner ?? throw new InvalidOperationException()).GetHeaders();
        index = 0;
        while (index < (innerHeaders ?? throw new InvalidOperationException()).GetCount())
        {
            EmailHeader header = innerHeaders.GetAt(index);
            if (IsInnerHeader(header.GetName())) merged.Add(header.Clone());
            index++;
        }

        MimeEntity? body = inner.HasBody() ? (inner.GetBody() ?? throw new InvalidOperationException()).Clone() : null;
        return new EmailMessageOperations().CreateWithHeadersAndBodyAndEnvelopeAndSourceInfoAndIssues(merged, body, fragments[0].GetEnvelope(), null, new EmailIssueList());
    }

    void SortFragments()
    {
        int index = 1;
        while (index < fragments.Count)
        {
            EmailPartialMessageFragment value = fragments[index];
            int destination = index - 1;
            while (destination >= 0 && ComesAfter(fragments[destination], value))
            {
                fragments[destination + 1] = fragments[destination];
                destination--;
            }

            fragments[destination + 1] = value;
            index++;
        }
    }

    static bool ComesAfter(EmailPartialMessageFragment left, EmailPartialMessageFragment right)
    {
        if (left.GetFragmentNumber() != right.GetFragmentNumber()) return left.GetFragmentNumber() > right.GetFragmentNumber();
        return left.GetInsertionIndex() > right.GetInsertionIndex();
    }

    static bool IsInnerHeader(string name)
    {
        if (name.StartsWith("Content-", StringComparison.OrdinalIgnoreCase)) return true;
        return string.Equals(name, "Subject", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Message-ID", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Encrypted", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "MIME-Version", StringComparison.OrdinalIgnoreCase);
    }

    static bool EndsWithCrlf(byte[] value)
    {
        return value.Length >= 2 && value[value.Length - 2] == 13 && value[value.Length - 1] == 10;
    }

    static bool Contains(List<long> values, long value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value) return true;
            index++;
        }

        return false;
    }

    static EmailIssue Issue(EmailIssueCode code, string description, string recovery)
    {
        return new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, code, description, null, null, "Content-Type", recovery);
    }
}
internal class EmailPartialMessageFragmentData
{
    EmailPartialMessageFragment fragment;
    byte[] bytes;

    internal EmailPartialMessageFragmentData(EmailPartialMessageFragment fragment, byte[] bytes)
    {
        this.fragment = fragment;
        this.bytes = bytes;
    }

    internal EmailPartialMessageFragment GetFragment() { return fragment; }
    internal byte[] GetBytes() { return bytes; }
}
internal class EmailPartialMessageReparseResult
{
    EmailMessage? innerMessage;
    EmailMessage? message;
    byte[]? reassembledBytes;

    internal EmailPartialMessageReparseResult(EmailMessage? innerMessage, EmailMessage? message, byte[]? reassembledBytes)
    {
        this.innerMessage = innerMessage;
        this.message = message;
        this.reassembledBytes = reassembledBytes;
    }

    internal EmailMessage? GetInnerMessage() { return innerMessage; }
    internal EmailMessage? GetMessage() { return message; }
    internal byte[]? GetReassembledBytes() { return reassembledBytes; }
}
