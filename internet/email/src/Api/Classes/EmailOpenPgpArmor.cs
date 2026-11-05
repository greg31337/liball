using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Provides operations for working with openPGP armor.
/// </summary>
public class EmailOpenPgpArmor : IEmailOpenPgpArmorContract
{
    internal static EmailOpenPgpArmorResult DecodeImplementationWithInput(ReadOnlySpan<byte> input)
    {
        return DecodeImplementationWithInputAndOptions(input, new EmailOpenPgpReaderOptions());
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailOpenPgpArmorResult DecodeWithInput(ReadOnlySpan<byte> input)
    {
        return EmailOpenPgpArmor.DecodeImplementationWithInput(input);
    }

    internal static EmailOpenPgpArmorResult DecodeImplementationWithInputAndOptions(ReadOnlySpan<byte> input, EmailOpenPgpReaderOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        EmailOpenPgpReaderOptions snapshot = options.Clone();
        EmailCryptographicLimits limits = snapshot.GetLimits();
        if (input.Length > limits.GetMaxInputBytes())
            throw new EmailLimitException("MaxOpenPgpInputBytes", limits.GetMaxInputBytes(), input.Length);
        byte[] original = input.ToArray();
        EmailIssueList issues = new EmailIssueList();
        List<string> lines = SplitLines(original, snapshot.GetMaximumArmorLineBytes());
        if (lines == null || lines.Count == 0)
            return Failure(EmailOpenPgpArmorKind.Message, issues, "The ASCII armor is empty or contains an overlong line.");
        string begin = TrimTrailingWhitespace(lines[0]);
        int beginKind = ParseBegin(begin);
        if (beginKind < 0)
            return Failure(EmailOpenPgpArmorKind.Message, issues, "The ASCII armor begin delimiter is missing or unsupported.");
        EmailOpenPgpArmorKind kind = (EmailOpenPgpArmorKind)beginKind;
        string label = Label(kind);
        int index = 1;
        List<EmailOpenPgpArmorHeader> headers = new List<EmailOpenPgpArmorHeader>();
        while (index < lines.Count && lines[index].Trim().Length != 0)
        {
            if (headers.Count >= snapshot.GetMaximumArmorHeaderCount())
                throw new EmailLimitException("MaxOpenPgpArmorHeaderCount", snapshot.GetMaximumArmorHeaderCount(), headers.Count + 1);
            string line = lines[index];
            int colon = line.IndexOf(": ", StringComparison.Ordinal);
            if (colon <= 0)
            {
                issues.Add(Issue(EmailIssueSeverity.Warning, EmailIssueCode.OpenPgpArmorMalformed, "An improperly formatted armor header was ignored.", "Packet data decoding continued."));
            }
            else
            {
                headers.Add(new EmailOpenPgpArmorHeader(line.Substring(0, colon), line.Substring(colon + 2)));
            }

            index++;
        }

        if (index >= lines.Count)
            return Failure(kind, issues, "The ASCII armor has no blank separator before its data.");
        index++;
        string end = "-----END " + label + "-----";
        int endIndex = -1;
        int scan = index;
        while (scan < lines.Count)
        {
            if (string.Equals(TrimTrailingWhitespace(lines[scan]), end, StringComparison.Ordinal))
            {
                endIndex = scan;
                break;
            }

            scan++;
        }

        if (endIndex < 0)
            return Failure(kind, issues, "The ASCII armor end delimiter is missing or does not match its begin delimiter.");
        scan = endIndex + 1;
        while (scan < lines.Count)
        {
            if (lines[scan].Trim().Length != 0)
                return Failure(kind, issues, "Non-whitespace data follows the ASCII armor end delimiter.");
            scan++;
        }

        bool checksumPresent = false;
        bool checksumValid = false;
        string? checksumText = null;
        if (endIndex > index && lines[endIndex - 1].StartsWith("=", StringComparison.Ordinal))
        {
            checksumPresent = true;
            checksumText = lines[endIndex - 1].Substring(1).Trim();
            endIndex--;
        }

        StringBuilder base64 = new StringBuilder();
        while (index < endIndex)
        {
            string line = lines[index].Trim();
            if (line.Length == 0)
                return Failure(kind, issues, "Blank lines are not permitted inside ASCII-armored base64 data.");
            if (line.Length > 76)
                return Failure(kind, issues, "An ASCII-armored base64 line exceeds 76 characters.");
            base64.Append(line);
            index++;
        }

        string base64Text = base64.ToString();
        byte[] decodedBuffer = new byte[(base64Text.Length / 4 + 1) * 3];
        int decodedCount;
        if (!Convert.TryFromBase64String(base64Text, decodedBuffer, out decodedCount))
            return Failure(kind, issues, "The ASCII-armored packet data is not valid base64.");
        byte[] decoded = EmailOpenPgpMimeUtility.Slice(decodedBuffer, 0, decodedCount);
        if (decoded.LongLength > limits.GetMaxInputBytes())
            throw new EmailLimitException("MaxOpenPgpDecodedBytes", limits.GetMaxInputBytes(), decoded.LongLength);
        if (checksumPresent)
        {
            byte[] checksumBuffer = new byte[3];
            int checksumCount;
            byte[]? checksum = Convert.TryFromBase64String((checksumText ?? throw new InvalidOperationException()), checksumBuffer, out checksumCount) ? EmailOpenPgpMimeUtility.Slice(checksumBuffer, 0, checksumCount) : null;
            int expected = ComputeCrc24Implementation(decoded);
            checksumValid = checksum != null && checksum.Length == 3 && checksum[0] == (byte)(expected >> 16) && checksum[1] == (byte)(expected >> 8 & 255) && checksum[2] == (byte)(expected & 255);
            if (!checksumValid)
                issues.Add(Issue(EmailIssueSeverity.Warning, EmailIssueCode.OpenPgpArmorChecksumMismatch, "The optional ASCII armor CRC-24 checksum is malformed or does not match.", "The decoded packet data was retained as required for interoperability."));
        }

        return new EmailOpenPgpArmorResult(kind, decoded, headers, checksumPresent, checksumValid, issues);
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public EmailOpenPgpArmorResult DecodeWithInputAndOptions(ReadOnlySpan<byte> input, EmailOpenPgpReaderOptions options)
    {
        return EmailOpenPgpArmor.DecodeImplementationWithInputAndOptions(input, options);
    }

    internal static byte[] EncodeImplementationWithDataAndKind(ReadOnlySpan<byte> data, EmailOpenPgpArmorKind kind)
    {
        return EncodeImplementationWithDataAndKindAndIncludeChecksum(data, kind, false);
    }

    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public byte[] EncodeWithDataAndKind(ReadOnlySpan<byte> data, EmailOpenPgpArmorKind kind)
    {
        return EmailOpenPgpArmor.EncodeImplementationWithDataAndKind(data, kind);
    }

    internal static byte[] EncodeImplementationWithDataAndKindAndIncludeChecksum(ReadOnlySpan<byte> data, EmailOpenPgpArmorKind kind, bool includeChecksum)
    {
        if (kind < EmailOpenPgpArmorKind.Message || kind > EmailOpenPgpArmorKind.Signature)
            throw new ArgumentOutOfRangeException("kind");
        byte[] bytes = data.ToArray();
        string label = Label(kind);
        StringBuilder output = new StringBuilder();
        output.Append("-----BEGIN ").Append(label).Append("-----\r\n\r\n");
        string encoded = Convert.ToBase64String(bytes);
        int index = 0;
        while (index < encoded.Length)
        {
            int count = Math.Min(64, encoded.Length - index);
            output.Append(encoded, index, count).Append("\r\n");
            index += count;
        }

        if (includeChecksum)
        {
            int checksum = ComputeCrc24Implementation(bytes);
            byte[] checksumBytes = [(byte)(checksum >> 16), (byte)(checksum >> 8 & 255), (byte)(checksum & 255)];
            output.Append('=').Append(Convert.ToBase64String(checksumBytes)).Append("\r\n");
        }

        output.Append("-----END ").Append(label).Append("-----\r\n");
        return Encoding.ASCII.GetBytes(output.ToString());
    }

    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "includeChecksum">Whether to enable the include checksum behavior.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public byte[] EncodeWithDataAndKindAndIncludeChecksum(ReadOnlySpan<byte> data, EmailOpenPgpArmorKind kind, bool includeChecksum)
    {
        return EmailOpenPgpArmor.EncodeImplementationWithDataAndKindAndIncludeChecksum(data, kind, includeChecksum);
    }

    internal static int ComputeCrc24Implementation(ReadOnlySpan<byte> data)
    {
        int crc = 0xB704CE;
        int index = 0;
        while (index < data.Length)
        {
            crc ^= data[index] << 16;
            int bit = 0;
            while (bit < 8)
            {
                crc <<= 1;
                if ((crc & 0x1000000) != 0)
                    crc = (crc & 0xFFFFFF) ^ 0x864CFB;
                bit++;
            }

            index++;
        }

        return crc & 0xFFFFFF;
    }

    /// <summary>
    /// Computes the crc 24.
    /// </summary>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <returns>The computed count or size.</returns>
    public int ComputeCrc24(ReadOnlySpan<byte> data)
    {
        return EmailOpenPgpArmor.ComputeCrc24Implementation(data);
    }

    static List<string> SplitLines(byte[] input, int maximumLineBytes)
    {
        List<string> lines = new List<string>();
        int start = 0;
        int index = 0;
        while (index < input.Length)
        {
            if (input[index] != 13 && input[index] != 10)
            {
                index++;
                continue;
            }

            int length = index - start;
            if (length > maximumLineBytes)
                throw new EmailLimitException("MaxOpenPgpArmorLineBytes", maximumLineBytes, length);
            lines.Add(Encoding.UTF8.GetString(input, start, length));
            if (input[index] == 13 && index + 1 < input.Length && input[index + 1] == 10)
                index++;
            index++;
            start = index;
        }

        int finalLength = input.Length - start;
        if (finalLength > maximumLineBytes)
            throw new EmailLimitException("MaxOpenPgpArmorLineBytes", maximumLineBytes, finalLength);
        if (finalLength > 0 || input.Length == 0)
            lines.Add(Encoding.UTF8.GetString(input, start, finalLength));
        return lines;
    }

    static int ParseBegin(string line)
    {
        string[] labels = ["PGP MESSAGE", "PGP PUBLIC KEY BLOCK", "PGP PRIVATE KEY BLOCK", "PGP SIGNATURE"];
        int index = 0;
        while (index < labels.Length)
        {
            if (string.Equals(line, "-----BEGIN " + labels[index] + "-----", StringComparison.Ordinal))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    static string Label(EmailOpenPgpArmorKind kind)
    {
        if (kind == EmailOpenPgpArmorKind.Message)
            return "PGP MESSAGE";
        if (kind == EmailOpenPgpArmorKind.PublicKey)
            return "PGP PUBLIC KEY BLOCK";
        if (kind == EmailOpenPgpArmorKind.PrivateKey)
            return "PGP PRIVATE KEY BLOCK";
        return "PGP SIGNATURE";
    }

    static string TrimTrailingWhitespace(string value)
    {
        return value.TrimEnd(' ', '\t');
    }

    static EmailOpenPgpArmorResult Failure(EmailOpenPgpArmorKind kind, EmailIssueList issues, string description)
    {
        issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.OpenPgpArmorMalformed, description, "No armored packet data was released."));
        return new EmailOpenPgpArmorResult(kind, new byte[0], new List<EmailOpenPgpArmorHeader>(), false, false, issues);
    }

    static EmailIssue Issue(EmailIssueSeverity severity, EmailIssueCode code, string description, string recovery)
    {
        return new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, null, null, null, recovery);
    }
}
