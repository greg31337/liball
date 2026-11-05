using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Parses header block into library objects.
/// </summary>
/// <remarks>
/// Malformed email data is normally represented by status and issue information rather than by exceptions. Configured limits still bound the amount of work performed.
/// </remarks>
public class EmailHeaderBlockParser : IEmailHeaderBlockParserContract
{
    internal static EmailHeaderBlockParseResult ParseImplementationWithInput(ReadOnlySpan<byte> input)
    {
        return ParseImplementationWithInputAndOptionsAndInitialPositionAndEndOfInput(input, new EmailHeaderBlockParserOptions(), new EmailSourcePosition(0, 1, 1), true);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailHeaderBlockParseResult ParseWithInput(ReadOnlySpan<byte> input)
    {
        return EmailHeaderBlockParser.ParseImplementationWithInput(input);
    }

    internal static EmailHeaderBlockParseResult ParseImplementationWithInputAndEndOfInput(ReadOnlySpan<byte> input, bool endOfInput)
    {
        return ParseImplementationWithInputAndOptionsAndInitialPositionAndEndOfInput(input, new EmailHeaderBlockParserOptions(), new EmailSourcePosition(0, 1, 1), endOfInput);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "endOfInput">Whether to enable the end of input behavior.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailHeaderBlockParseResult ParseWithInputAndEndOfInput(ReadOnlySpan<byte> input, bool endOfInput)
    {
        return EmailHeaderBlockParser.ParseImplementationWithInputAndEndOfInput(input, endOfInput);
    }

    internal static EmailHeaderBlockParseResult ParseImplementationWithInputAndOptionsAndEndOfInput(ReadOnlySpan<byte> input, EmailHeaderBlockParserOptions options, bool endOfInput)
    {
        return ParseImplementationWithInputAndOptionsAndInitialPositionAndEndOfInput(input, options, new EmailSourcePosition(0, 1, 1), endOfInput);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "endOfInput">Whether to enable the end of input behavior.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailHeaderBlockParseResult ParseWithInputAndOptionsAndEndOfInput(ReadOnlySpan<byte> input, EmailHeaderBlockParserOptions options, bool endOfInput)
    {
        return EmailHeaderBlockParser.ParseImplementationWithInputAndOptionsAndEndOfInput(input, options, endOfInput);
    }

    internal static EmailHeaderBlockParseResult ParseImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailHeaderBlockParserOptions options, EmailSourcePosition initialPosition)
    {
        return ParseImplementationWithInputAndOptionsAndInitialPositionAndEndOfInput(input, options, initialPosition, true);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailHeaderBlockParseResult ParseWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailHeaderBlockParserOptions options, EmailSourcePosition initialPosition)
    {
        return EmailHeaderBlockParser.ParseImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailHeaderBlockParseResult ParseImplementationWithInputAndOptionsAndInitialPositionAndEndOfInput(ReadOnlySpan<byte> input, EmailHeaderBlockParserOptions options, EmailSourcePosition initialPosition, bool endOfInput)
    {
        long maximum = options == null ? 0 : options.GetLimits().GetMaxHeaderFieldCount();
        return ParseImplementationWithInputAndOptionsAndInitialPositionAndEndOfInputAndFieldCountLimitNameAndReportedFieldCountLimitAndReportedFieldCountObserved(input, options, initialPosition, endOfInput, "MaxHeaderFieldCount", maximum, 0);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <param name = "endOfInput">Whether to enable the end of input behavior.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailHeaderBlockParseResult ParseWithInputAndOptionsAndInitialPositionAndEndOfInput(ReadOnlySpan<byte> input, EmailHeaderBlockParserOptions options, EmailSourcePosition initialPosition, bool endOfInput)
    {
        return EmailHeaderBlockParser.ParseImplementationWithInputAndOptionsAndInitialPositionAndEndOfInput(input, options, initialPosition, endOfInput);
    }

    internal static EmailHeaderBlockParseResult ParseImplementationWithInputAndOptionsAndInitialPositionAndEndOfInputAndFieldCountLimitNameAndReportedFieldCountLimitAndReportedFieldCountObserved(ReadOnlySpan<byte> input, EmailHeaderBlockParserOptions? options, EmailSourcePosition initialPosition, bool endOfInput, string fieldCountLimitName, long reportedFieldCountLimit, long reportedFieldCountObserved)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (initialPosition == null)
        {
            throw new ArgumentNullException("initialPosition");
        }

        EmailHeaderBlockParserOptions snapshot = options.Clone();
        EmailParsingLimits limits = snapshot.GetLimits();
        byte[] prefix = CopyRequiredPrefix(input, limits.GetMaxHeaderBytes(), endOfInput);
        EmailHeaderBlockParserWorker worker = new EmailHeaderBlockParserWorker(prefix, input.Length, snapshot, initialPosition, endOfInput, fieldCountLimitName, reportedFieldCountLimit, reportedFieldCountObserved);
        return worker.Parse();
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <param name = "endOfInput">Whether to enable the end of input behavior.</param>
    /// <param name = "fieldCountLimitName">The field count limit name used by the operation.</param>
    /// <param name = "reportedFieldCountLimit">The reported field count limit used by the operation.</param>
    /// <param name = "reportedFieldCountObserved">The reported field count observed used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailHeaderBlockParseResult ParseWithInputAndOptionsAndInitialPositionAndEndOfInputAndFieldCountLimitNameAndReportedFieldCountLimitAndReportedFieldCountObserved(ReadOnlySpan<byte> input, EmailHeaderBlockParserOptions? options, EmailSourcePosition initialPosition, bool endOfInput, string fieldCountLimitName, long reportedFieldCountLimit, long reportedFieldCountObserved)
    {
        return EmailHeaderBlockParser.ParseImplementationWithInputAndOptionsAndInitialPositionAndEndOfInputAndFieldCountLimitNameAndReportedFieldCountLimitAndReportedFieldCountObserved(input, options, initialPosition, endOfInput, fieldCountLimitName, reportedFieldCountLimit, reportedFieldCountObserved);
    }

    static byte[] CopyRequiredPrefix(ReadOnlySpan<byte> input, long maxHeaderBytes, bool endOfInput)
    {
        int scanLength = input.Length;
        if (maxHeaderBytes < input.Length)
        {
            scanLength = (int)maxHeaderBytes + 1;
        }

        int separatorEnd = FindSeparatorEnd(input, scanLength, endOfInput);
        int copyLength = separatorEnd >= 0 ? separatorEnd : scanLength;
        return input.Slice(0, copyLength).ToArray();
    }

    static int FindSeparatorEnd(ReadOnlySpan<byte> input, int scanLength, bool endOfInput)
    {
        int lineStart = 0;
        int index = 0;
        while (index < scanLength)
        {
            if (input[index] == 13)
            {
                int end = index + 1;
                if (end < input.Length && input[end] == 10)
                {
                    if (end >= scanLength)
                    {
                        return -1;
                    }

                    end++;
                }
                else if (end == input.Length && !endOfInput)
                {
                    return -1;
                }

                if (index == lineStart)
                {
                    return end;
                }

                lineStart = end;
                index = end;
            }
            else if (input[index] == 10)
            {
                int end = index + 1;
                if (index == lineStart)
                {
                    return end;
                }

                lineStart = end;
                index = end;
            }
            else
            {
                index++;
            }
        }

        return -1;
    }
}
