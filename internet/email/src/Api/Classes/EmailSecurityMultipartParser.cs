using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Parses security multipart into library objects.
/// </summary>
/// <remarks>
/// Malformed email data is normally represented by status and issue information rather than by exceptions. Configured limits still bound the amount of work performed.
/// </remarks>
public class EmailSecurityMultipartParser : IEmailSecurityMultipartParserContract
{
    internal static EmailSecurityMultipartParseResult ParseImplementationWithMessage(EmailMessage message)
    {
        if (message == null)
            throw new ArgumentNullException("message");
        if (!message.HasBody())
        {
            EmailIssueList missingIssues = new EmailIssueList();
            missingIssues.Add(CreateIssue(EmailIssueCode.SecurityMultipartStructureMalformed, "The message has no MIME body to inspect as a security multipart.", "No security multipart was produced.", null));
            return new EmailSecurityMultipartParseResult(null, missingIssues);
        }

        return ParseImplementationWithRoot(message.GetBody() ?? throw new InvalidOperationException());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailSecurityMultipartParseResult ParseWithMessage(EmailMessage message)
    {
        return EmailSecurityMultipartParser.ParseImplementationWithMessage(message);
    }

    internal static EmailSecurityMultipartParseResult ParseImplementationWithRoot(MimeEntity root)
    {
        if (root == null)
            throw new ArgumentNullException("root");
        EmailIssueList issues = new EmailIssueList();
        bool signed = root.GetKind() == MimeEntityKind.Signed && (root.GetMediaType() ?? throw new InvalidOperationException()).IsType("multipart", "signed");
        bool encrypted = root.GetKind() == MimeEntityKind.Encrypted && (root.GetMediaType() ?? throw new InvalidOperationException()).IsType("multipart", "encrypted");
        if (!signed && !encrypted)
        {
            issues.Add(CreateIssue(EmailIssueCode.SecurityMultipartStructureMalformed, "The MIME entity is not multipart/signed or multipart/encrypted.", "No security multipart was produced.", root));
            return new EmailSecurityMultipartParseResult(null, issues);
        }

        bool valid = true;
        if (root.GetChildCount() != 2 || root.GetOwnIssues().ContainsCode(EmailIssueCode.MimeStructureMalformed))
        {
            issues.Add(CreateIssue(EmailIssueCode.SecurityMultipartStructureMalformed, "A security multipart must contain exactly two original body parts.", "The available ordered parts were retained, but the security framing is invalid.", root));
            valid = false;
        }

        MimeEntity? first = root.GetChildCount() > 0 ? root.GetChildAt(0) : null;
        MimeEntity? second = root.GetChildCount() > 1 ? root.GetChildAt(1) : null;
        MimeEntity? contentPart = signed ? first : second;
        MimeEntity? securityPart = signed ? second : first;
        EmailMediaType? protocolType = GetProtocol(root, issues);
        string? protocol = protocolType == null ? null : protocolType.GetNormalizedTopLevelType() + "/" + protocolType.GetNormalizedSubtype();
        if (protocolType == null)
        {
            valid = false;
        }
        else if (securityPart == null || !MediaTypesMatch(protocolType, securityPart.GetMediaType()))
        {
            issues.Add(CreateIssue(EmailIssueCode.SecurityMultipartProtocolMismatch, "The protocol parameter does not match the security control or signature part media type.", "Preserve both declarations and reject cryptographic processing until they agree.", root));
            valid = false;
        }

        List<string> micAlgorithms = new List<string>();
        if (signed && !GetMicAlgorithms(root, micAlgorithms, issues))
        {
            valid = false;
        }

        EmailContent? exactSignedOctets = null;
        if (signed && contentPart != null)
        {
            if (root.HasCurrentOriginalEntityContent() && contentPart.HasCurrentOriginalEntityContent())
            {
                exactSignedOctets = contentPart.GetOriginalEntityContent();
            }
            else
            {
                issues.Add(CreateIssue(EmailIssueCode.SecurityMultipartSignedContentUnavailable, "Exact original signed octets are unavailable or the MIME tree has changed.", "Reparse from preserved source bytes before attempting detached-signature verification.", root));
                valid = false;
            }
        }

        EmailSecurityMultipart security = new EmailSecurityMultipart(root, protocol, micAlgorithms, contentPart, securityPart, exactSignedOctets, valid);
        return new EmailSecurityMultipartParseResult(security, issues);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailSecurityMultipartParseResult ParseWithRoot(MimeEntity root)
    {
        return EmailSecurityMultipartParser.ParseImplementationWithRoot(root);
    }

    static EmailMediaType? GetProtocol(MimeEntity root, EmailIssueList issues)
    {
        EmailParameterList parameters = (root.GetMediaType() ?? throw new InvalidOperationException()).GetParameters();
        byte[]? value = GetParameterBytes(parameters, "protocol");
        if (value == null)
        {
            issues.Add(CreateIssue(EmailIssueCode.SecurityMultipartProtocolMissing, "The security multipart has no protocol parameter.", "Declare the media type of the security control or signature part.", root));
            return null;
        }

        if (parameters.HasDuplicate("protocol") || !IsAscii(value))
        {
            issues.Add(CreateIssue(EmailIssueCode.SecurityMultipartProtocolMalformed, "The security multipart protocol parameter is duplicated or is not an ASCII media type.", "Retain one valid type/subtype protocol value.", root));
            return null;
        }

        EmailMediaTypeParseResult parsed = new EmailMediaTypeParser().ParseWithInputAndMode(value, EmailStructuredValueParseMode.Strict);
        if (!parsed.IsSuccess() || !parsed.IsComplete() || parsed.GetValue().GetParameters().GetCount() != 0)
        {
            issues.Add(CreateIssue(EmailIssueCode.SecurityMultipartProtocolMalformed, "The security multipart protocol parameter is not a complete media type without parameters.", "Use a protocol value such as application/pgp-signature.", root));
            return null;
        }

        return parsed.GetValue();
    }

    static bool GetMicAlgorithms(MimeEntity root, List<string> algorithms, EmailIssueList issues)
    {
        EmailParameterList parameters = (root.GetMediaType() ?? throw new InvalidOperationException()).GetParameters();
        byte[]? value = GetParameterBytes(parameters, "micalg");
        if (value == null)
        {
            issues.Add(CreateIssue(EmailIssueCode.SecurityMultipartMicAlgorithmMissing, "The multipart/signed content type has no micalg parameter.", "Declare the message integrity check algorithm used by the detached signature.", root));
            return false;
        }

        if (parameters.HasDuplicate("micalg") || !IsAscii(value))
        {
            issues.Add(CreateIssue(EmailIssueCode.SecurityMultipartMicAlgorithmMalformed, "The multipart/signed micalg parameter is duplicated or contains non-ASCII data.", "Retain one comma-separated list of valid algorithm tokens.", root));
            return false;
        }

        string[] values = Encoding.ASCII.GetString(value).Split(',');
        int index = 0;
        while (index < values.Length)
        {
            string algorithm = values[index].Trim();
            if (!IsToken(algorithm))
            {
                issues.Add(CreateIssue(EmailIssueCode.SecurityMultipartMicAlgorithmMalformed, "The multipart/signed micalg parameter contains an invalid algorithm token.", "Use one or more comma-separated MIME tokens.", root));
                return false;
            }

            algorithms.Add(algorithm.ToLowerInvariant());
            index++;
        }

        return algorithms.Count > 0;
    }

    static byte[]? GetParameterBytes(EmailParameterList parameters, string name)
    {
        EmailRfc2231DecodedParameter? decoded = parameters.GetDecoded(name);
        if (decoded != null)
            return decoded.GetDecodedBytes();
        EmailParameter? physical = parameters.GetFirst(name);
        return physical == null ? null : physical.GetValueBytes();
    }

    static bool MediaTypesMatch(EmailMediaType expected, EmailMediaType? actual)
    {
        return string.Equals(expected.GetTopLevelType(), (actual ?? throw new InvalidOperationException()).GetTopLevelType(), StringComparison.OrdinalIgnoreCase) && string.Equals(expected.GetSubtype(), actual.GetSubtype(), StringComparison.OrdinalIgnoreCase);
    }

    static bool IsAscii(byte[] value)
    {
        if (value.Length == 0)
            return false;
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 127)
                return false;
            index++;
        }

        return true;
    }

    static bool IsToken(string value)
    {
        if (value.Length == 0)
            return false;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current < 33 || current > 126 || current == '(' || current == ')' || current == '<' || current == '>' || current == '@' || current == ',' || current == ';' || current == ':' || current == '\\' || current == '"' || current == '/' || current == '[' || current == ']' || current == '?' || current == '=')
                return false;
            index++;
        }

        return true;
    }

    static EmailIssue CreateIssue(EmailIssueCode code, string description, string recovery, MimeEntity? root)
    {
        EmailSourcePosition? position = root == null ? null : (root.GetMediaType() ?? throw new InvalidOperationException()).GetSourcePosition();
        return new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, code, description, position, null, "Content-Type", recovery);
    }
}
