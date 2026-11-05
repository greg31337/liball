using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

/// <summary>
/// Represents a message and its JMAP mail-store metadata, headers, and MIME body views.
/// </summary>
public class Email
{
    /// <summary>The immutable server-assigned Email identifier.</summary>
    public string? Id;

    /// <summary>The identifier for the raw RFC 5322 message bytes.</summary>
    public string? BlobId;

    /// <summary>The identifier of the Thread containing this Email.</summary>
    public string? ThreadId;

    /// <summary>The set of Mailbox identifiers containing this Email, or null for unavailable parsed metadata.</summary>
    public Dictionary<string, bool>? MailboxIds;

    /// <summary>The set of lowercase keywords applied to this Email, or null for unavailable parsed metadata.</summary>
    public Dictionary<string, bool>? Keywords;

    /// <summary>The size of the raw RFC 5322 message in octets.</summary>
    public long Size;

    /// <summary>The UTC date-time at which the message store received the Email.</summary>
    public string? ReceivedAt;

    /// <summary>All message header fields in original order, or null when not projected.</summary>
    public List<EmailHeader>? Headers;

    /// <summary>Maps requested dynamic <c>header:*</c> property names to their parsed or raw JSON values.</summary>
    public Dictionary<string, JsonElement> HeaderProperties = new Dictionary<string, JsonElement>();

    /// <summary>The parsed Message-ID header values, or null when absent or unparsable.</summary>
    public List<string>? MessageId;

    /// <summary>The parsed In-Reply-To header values, or null when absent or unparsable.</summary>
    public List<string>? InReplyTo;

    /// <summary>The parsed References header values, or null when absent or unparsable.</summary>
    public List<string>? References;

    /// <summary>The parsed Sender header addresses, or null when absent or unparsable.</summary>
    public List<EmailAddress>? Sender;

    /// <summary>The parsed From header addresses, or null when absent or unparsable.</summary>
    public List<EmailAddress>? From;

    /// <summary>The parsed To header addresses, or null when absent or unparsable.</summary>
    public List<EmailAddress>? To;

    /// <summary>The parsed Cc header addresses, or null when absent or unparsable.</summary>
    public List<EmailAddress>? Cc;

    /// <summary>The parsed Bcc header addresses, or null when absent or unparsable.</summary>
    public List<EmailAddress>? Bcc;

    /// <summary>The parsed Reply-To header addresses, or null when absent or unparsable.</summary>
    public List<EmailAddress>? ReplyTo;

    /// <summary>The decoded Subject header text, or null when absent.</summary>
    public string? Subject;

    /// <summary>The parsed Date header value, or null when absent or unparsable.</summary>
    public string? SentAt;

    /// <summary>The complete MIME body structure, or null when not projected.</summary>
    public EmailBodyPart? BodyStructure;

    /// <summary>Maps part identifiers to decoded text values, or null when not projected.</summary>
    public Dictionary<string, EmailBodyValue>? BodyValues;

    /// <summary>The sequential plain-text-preferred body parts, or null when not projected.</summary>
    public List<EmailBodyPart>? TextBody;

    /// <summary>The sequential HTML-preferred body parts, or null when not projected.</summary>
    public List<EmailBodyPart>? HtmlBody;

    /// <summary>The depth-first attachment body parts, or null when not projected.</summary>
    public List<EmailBodyPart>? Attachments;

    /// <summary>Whether the Email has a user-downloadable attachment.</summary>
    public bool HasAttachment;

    /// <summary>The server-generated plain-text preview.</summary>
    public string? Preview;

    /// <summary>Stores unrecognized Email properties other than dynamic header properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Represents one parsed mailbox address from an Email header field.
/// </summary>
public class EmailAddress
{
    /// <summary>The decoded display name, or null when none is present.</summary>
    public string? Name;

    /// <summary>The address specification, retained best-effort for malformed messages.</summary>
    public string? Email;

    /// <summary>Stores unrecognized EmailAddress properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Preserves a named address group or a consecutive set of ungrouped addresses.
/// </summary>
public class EmailAddressGroup
{
    /// <summary>The decoded group name, or null for addresses outside a named group.</summary>
    public string? Name;

    /// <summary>The mailbox addresses belonging to this group.</summary>
    public List<EmailAddress> Addresses = new List<EmailAddress>();

    /// <summary>Stores unrecognized EmailAddressGroup properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Represents one raw Email header field while preserving its name and order.
/// </summary>
public class EmailHeader
{
    /// <summary>The header field name with its original capitalization.</summary>
    public string? Name;

    /// <summary>The original header field value in raw form, including any folding line breaks.</summary>
    public string? Value;

    /// <summary>Stores unrecognized EmailHeader properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Represents one node in an Email's recursive MIME body structure.
/// </summary>
public class EmailBodyPart
{
    /// <summary>The identifier scoped to the Email, or null for a multipart node.</summary>
    public string? PartId;

    /// <summary>The identifier for the decoded part bytes, or null for a multipart node.</summary>
    public string? BlobId;

    /// <summary>The decoded part size in octets.</summary>
    public long Size;

    /// <summary>Whether the size property was present.</summary>
    public bool HasSize;

    /// <summary>The part header fields in original order, or null when not projected.</summary>
    public List<EmailHeader>? Headers;

    /// <summary>The decoded filename, or null when none is specified.</summary>
    public string? Name;

    /// <summary>The media type without parameters.</summary>
    public string? Type;

    /// <summary>The declared or implicit charset, or null for a non-text part.</summary>
    public string? Charset;

    /// <summary>The content disposition without parameters, or null when absent.</summary>
    public string? Disposition;

    /// <summary>The Content-ID without surrounding angle brackets, or null when absent.</summary>
    public string? Cid;

    /// <summary>The Content-Language tags, or null when absent.</summary>
    public List<string>? Language;

    /// <summary>The Content-Location URI, or null when absent.</summary>
    public string? Location;

    /// <summary>The child parts for a multipart node, or null for a leaf.</summary>
    public List<EmailBodyPart>? SubParts;

    /// <summary>Maps dynamic <c>header:*</c> part properties to their parsed or raw JSON values.</summary>
    public Dictionary<string, JsonElement> HeaderProperties = new Dictionary<string, JsonElement>();

    /// <summary>Stores unrecognized body-part properties other than dynamic header properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the decoded Unicode value of one textual Email body part.
/// </summary>
public class EmailBodyValue
{
    /// <summary>The decoded value with line endings normalized to line feeds.</summary>
    public string? Value;

    /// <summary>Whether decoding encountered malformed data or an unknown encoding.</summary>
    public bool IsEncodingProblem;

    /// <summary>Whether the returned value was truncated.</summary>
    public bool IsTruncated;

    /// <summary>Stores unrecognized EmailBodyValue properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}
