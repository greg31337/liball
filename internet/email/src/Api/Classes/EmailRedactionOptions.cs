using System;
using System.Text;

namespace email;
/// <summary>
/// Controls how redaction operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailRedactionOptions : IEmailRedactionOptionsContract
{
    bool redactAddresses;
    bool redactMessageIds;
    bool removeReceivedHeaders;
    bool redactIpAddresses;
    bool removeAuthenticationHeaders;
    bool redactSubjects;
    bool redactBodies;
    bool redactAttachmentNames;
    bool redactAttachmentContent;
    string pseudonymDomain;
    string subjectReplacement;
    string bodyReplacement;
    string attachmentNamePrefix;
    byte[] attachmentContentReplacement;
    int maximumDepth;
    int maximumEntityCount;
    /// <summary>
    /// Creates a new redaction options object.
    /// </summary>
    public EmailRedactionOptions()
    {
        pseudonymDomain = "redacted.invalid";
        subjectReplacement = "[redacted]";
        bodyReplacement = "[redacted body]\r\n";
        attachmentNamePrefix = "attachment";
        attachmentContentReplacement = new byte[0];
        maximumDepth = EmailTraversalConstants.DefaultMaximumDepth;
        maximumEntityCount = EmailTraversalConstants.DefaultMaximumEntityCount;
    }

    /// <summary>
    /// Gets the redact addresses.
    /// </summary>
    /// <returns><c>true</c> when the redact addresses setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRedactAddresses()
    {
        return redactAddresses;
    }

    /// <summary>
    /// Sets the redact addresses used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the redact addresses behavior.</param>
    public void SetRedactAddresses(bool value)
    {
        redactAddresses = value;
    }

    /// <summary>
    /// Gets the redact message ids.
    /// </summary>
    /// <returns><c>true</c> when the redact message ids setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRedactMessageIds()
    {
        return redactMessageIds;
    }

    /// <summary>
    /// Sets the redact message ids used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the redact message ids behavior.</param>
    public void SetRedactMessageIds(bool value)
    {
        redactMessageIds = value;
    }

    /// <summary>
    /// Gets the remove received headers.
    /// </summary>
    /// <returns><c>true</c> when the remove received headers setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRemoveReceivedHeaders()
    {
        return removeReceivedHeaders;
    }

    /// <summary>
    /// Sets the remove received headers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the remove received headers behavior.</param>
    public void SetRemoveReceivedHeaders(bool value)
    {
        removeReceivedHeaders = value;
    }

    /// <summary>
    /// Gets the redact ip addresses.
    /// </summary>
    /// <returns><c>true</c> when the redact ip addresses setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRedactIpAddresses()
    {
        return redactIpAddresses;
    }

    /// <summary>
    /// Sets the redact ip addresses used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the redact ip addresses behavior.</param>
    public void SetRedactIpAddresses(bool value)
    {
        redactIpAddresses = value;
    }

    /// <summary>
    /// Gets the remove authentication headers.
    /// </summary>
    /// <returns><c>true</c> when the remove authentication headers setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRemoveAuthenticationHeaders()
    {
        return removeAuthenticationHeaders;
    }

    /// <summary>
    /// Sets the remove authentication headers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the remove authentication headers behavior.</param>
    public void SetRemoveAuthenticationHeaders(bool value)
    {
        removeAuthenticationHeaders = value;
    }

    /// <summary>
    /// Gets the redact subjects.
    /// </summary>
    /// <returns><c>true</c> when the redact subjects setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRedactSubjects()
    {
        return redactSubjects;
    }

    /// <summary>
    /// Sets the redact subjects used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the redact subjects behavior.</param>
    public void SetRedactSubjects(bool value)
    {
        redactSubjects = value;
    }

    /// <summary>
    /// Gets the redact bodies.
    /// </summary>
    /// <returns><c>true</c> when the redact bodies setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRedactBodies()
    {
        return redactBodies;
    }

    /// <summary>
    /// Sets the redact bodies used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the redact bodies behavior.</param>
    public void SetRedactBodies(bool value)
    {
        redactBodies = value;
    }

    /// <summary>
    /// Gets the redact attachment names.
    /// </summary>
    /// <returns><c>true</c> when the redact attachment names setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRedactAttachmentNames()
    {
        return redactAttachmentNames;
    }

    /// <summary>
    /// Sets the redact attachment names used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the redact attachment names behavior.</param>
    public void SetRedactAttachmentNames(bool value)
    {
        redactAttachmentNames = value;
    }

    /// <summary>
    /// Gets the redact attachment content.
    /// </summary>
    /// <returns><c>true</c> when the redact attachment content setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRedactAttachmentContent()
    {
        return redactAttachmentContent;
    }

    /// <summary>
    /// Sets the redact attachment content used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the redact attachment content behavior.</param>
    public void SetRedactAttachmentContent(bool value)
    {
        redactAttachmentContent = value;
    }

    /// <summary>
    /// Gets the pseudonym domain.
    /// </summary>
    /// <returns>The current pseudonym domain.</returns>
    public string GetPseudonymDomain()
    {
        return pseudonymDomain;
    }

    /// <summary>
    /// Sets the pseudonym domain used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetPseudonymDomain(string value)
    {
        pseudonymDomain = ValidateDomain(value);
    }

    /// <summary>
    /// Gets the subject replacement.
    /// </summary>
    /// <returns>The current subject replacement.</returns>
    public string GetSubjectReplacement()
    {
        return subjectReplacement;
    }

    /// <summary>
    /// Sets the subject replacement used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetSubjectReplacement(string value)
    {
        subjectReplacement = ValidateHeaderValue(value, "value");
    }

    /// <summary>
    /// Gets the body replacement.
    /// </summary>
    /// <returns>The current body replacement.</returns>
    public string GetBodyReplacement()
    {
        return bodyReplacement;
    }

    /// <summary>
    /// Sets the body replacement used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetBodyReplacement(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 127 || value[index] == 0)
            {
                throw new ArgumentException("A body replacement must contain ASCII text without NUL.", "value");
            }

            index++;
        }

        bodyReplacement = value;
    }

    /// <summary>
    /// Gets the attachment name prefix.
    /// </summary>
    /// <returns>The current attachment name prefix.</returns>
    public string GetAttachmentNamePrefix()
    {
        return attachmentNamePrefix;
    }

    /// <summary>
    /// Sets the attachment name prefix used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetAttachmentNamePrefix(string value)
    {
        attachmentNamePrefix = ValidateToken(value, "value");
    }

    /// <summary>
    /// Gets the attachment content replacement.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current attachment content replacement.</returns>
    public byte[] GetAttachmentContentReplacement()
    {
        byte[] copy = new byte[attachmentContentReplacement.Length];
        attachmentContentReplacement.CopyTo(copy, 0);
        return copy;
    }

    /// <summary>
    /// Sets the attachment content replacement used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetAttachmentContentReplacement(byte[] value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        attachmentContentReplacement = new byte[value.Length];
        value.CopyTo(attachmentContentReplacement, 0);
    }

    /// <summary>
    /// Gets the maximum depth.
    /// </summary>
    /// <returns>The current maximum depth.</returns>
    public int GetMaximumDepth()
    {
        return maximumDepth;
    }

    /// <summary>
    /// Sets the maximum depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumDepth(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumDepth = value;
    }

    /// <summary>
    /// Gets the maximum entity count.
    /// </summary>
    /// <returns>The current maximum entity count.</returns>
    public int GetMaximumEntityCount()
    {
        return maximumEntityCount;
    }

    /// <summary>
    /// Sets the maximum entity count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumEntityCount(int value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumEntityCount = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailRedactionOptions Clone()
    {
        EmailRedactionOptions copy = new EmailRedactionOptions();
        copy.SetRedactAddresses(redactAddresses);
        copy.SetRedactMessageIds(redactMessageIds);
        copy.SetRemoveReceivedHeaders(removeReceivedHeaders);
        copy.SetRedactIpAddresses(redactIpAddresses);
        copy.SetRemoveAuthenticationHeaders(removeAuthenticationHeaders);
        copy.SetRedactSubjects(redactSubjects);
        copy.SetRedactBodies(redactBodies);
        copy.SetRedactAttachmentNames(redactAttachmentNames);
        copy.SetRedactAttachmentContent(redactAttachmentContent);
        copy.SetPseudonymDomain(pseudonymDomain);
        copy.SetSubjectReplacement(subjectReplacement);
        copy.SetBodyReplacement(bodyReplacement);
        copy.SetAttachmentNamePrefix(attachmentNamePrefix);
        copy.SetAttachmentContentReplacement(attachmentContentReplacement);
        copy.SetMaximumDepth(maximumDepth);
        copy.SetMaximumEntityCount(maximumEntityCount);
        return copy;
    }

    internal static EmailRedactionOptions CreateAllImplementation()
    {
        EmailRedactionOptions options = new EmailRedactionOptions();
        options.SetRedactAddresses(true);
        options.SetRedactMessageIds(true);
        options.SetRemoveReceivedHeaders(true);
        options.SetRedactIpAddresses(true);
        options.SetRemoveAuthenticationHeaders(true);
        options.SetRedactSubjects(true);
        options.SetRedactBodies(true);
        options.SetRedactAttachmentNames(true);
        options.SetRedactAttachmentContent(true);
        return options;
    }

    static string ValidateToken(string value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("A nonempty redaction token is required.", parameterName);
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current <= 32 || current >= 127 || current == '\r' || current == '\n')
            {
                throw new ArgumentException("A redaction token must contain visible ASCII without whitespace.", parameterName);
            }

            index++;
        }

        return value;
    }

    static string ValidateHeaderValue(string value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (value.IndexOf('\r') >= 0 || value.IndexOf('\n') >= 0 || value.IndexOf('\0') >= 0)
        {
            throw new ArgumentException("A redaction header value cannot contain a line break or NUL.", parameterName);
        }

        Encoding.UTF8.GetByteCount(value);
        return value;
    }

    static string ValidateDomain(string value)
    {
        ValidateToken(value, "value");
        if (value.Length > 253 || value[0] == '.' || value[value.Length - 1] == '.')
        {
            throw new ArgumentException("A valid pseudonym domain is required.", "value");
        }

        int index = 0;
        bool labelStart = true;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '.')
            {
                if (labelStart || value[index - 1] == '-')
                {
                    throw new ArgumentException("The pseudonym domain contains an invalid label.", "value");
                }

                labelStart = true;
            }
            else
            {
                bool alphaNumeric = current >= 'a' && current <= 'z' || current >= 'A' && current <= 'Z' || current >= '0' && current <= '9';
                if (!alphaNumeric && current != '-' || labelStart && current == '-')
                {
                    throw new ArgumentException("The pseudonym domain contains an invalid character.", "value");
                }

                labelStart = false;
            }

            index++;
        }

        if (labelStart || value[value.Length - 1] == '-')
        {
            throw new ArgumentException("The pseudonym domain contains an invalid label.", "value");
        }

        return value.ToLowerInvariant();
    }
}
