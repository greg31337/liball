using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Stores and resolves registered header definitions.
/// </summary>
public class EmailHeaderRegistry : IEmailHeaderRegistryContract
{
    static readonly string[] fieldNames = new string[]
    {
        "Date",
        "From",
        "Sender",
        "Reply-To",
        "To",
        "Cc",
        "Bcc",
        "Message-ID",
        "In-Reply-To",
        "References",
        "Subject",
        "Comments",
        "Keywords",
        "Received",
        "Resent-Date",
        "Resent-From",
        "Resent-Sender",
        "Resent-To",
        "Resent-Cc",
        "Resent-Bcc",
        "Resent-Message-ID",
        "Resent-Reply-To",
        "Content-Type",
        "Content-ID",
        "Content-Description",
        "Content-Disposition",
        "Content-Language",
        "Disposition-Notification-To",
        "Authentication-Results",
        "Original-Authentication-Results",
        "Received-SPF",
        "DKIM-Signature",
        "ARC-Message-Signature",
        "ARC-Seal",
        "ARC-Authentication-Results",
        "Priority",
        "X-Priority",
        "Importance",
        "Sensitivity",
        "Auto-Submitted",
        "List-Help",
        "List-Unsubscribe",
        "List-Subscribe",
        "List-Post",
        "List-Owner",
        "List-Archive",
        "List-Unsubscribe-Post",
        "Return-Path",
        "MIME-Version",
        "Content-Transfer-Encoding",
        "Content-Location",
        "Newsgroups",
        "Followup-To",
        "List-ID",
        "Delivered-To",
        "X-Original-To",
        "Envelope-To",
        "X-Envelope-To",
        "Errors-To",
        "Mail-Reply-To",
        "Mail-Followup-To",
        "X-Originating-IP"
    };
    static readonly EmailHeaderValueKind[] valueKinds = new EmailHeaderValueKind[]
    {
        EmailHeaderValueKind.Date,
        EmailHeaderValueKind.AddressList,
        EmailHeaderValueKind.Mailbox,
        EmailHeaderValueKind.AddressList,
        EmailHeaderValueKind.AddressList,
        EmailHeaderValueKind.AddressList,
        EmailHeaderValueKind.AddressList,
        EmailHeaderValueKind.MessageId,
        EmailHeaderValueKind.InReplyTo,
        EmailHeaderValueKind.References,
        EmailHeaderValueKind.Unstructured,
        EmailHeaderValueKind.Unstructured,
        EmailHeaderValueKind.Keywords,
        EmailHeaderValueKind.Received,
        EmailHeaderValueKind.ResentField,
        EmailHeaderValueKind.ResentField,
        EmailHeaderValueKind.ResentField,
        EmailHeaderValueKind.ResentField,
        EmailHeaderValueKind.ResentField,
        EmailHeaderValueKind.ResentField,
        EmailHeaderValueKind.ResentField,
        EmailHeaderValueKind.ResentField,
        EmailHeaderValueKind.MediaType,
        EmailHeaderValueKind.ContentId,
        EmailHeaderValueKind.Unstructured,
        EmailHeaderValueKind.Disposition,
        EmailHeaderValueKind.ContentLanguage,
        EmailHeaderValueKind.AddressList,
        EmailHeaderValueKind.AuthenticationResults,
        EmailHeaderValueKind.AuthenticationResults,
        EmailHeaderValueKind.ReceivedSpf,
        EmailHeaderValueKind.DkimSignature,
        EmailHeaderValueKind.ArcMessageSignature,
        EmailHeaderValueKind.ArcSeal,
        EmailHeaderValueKind.ArcAuthenticationResults,
        EmailHeaderValueKind.Priority,
        EmailHeaderValueKind.XPriority,
        EmailHeaderValueKind.Importance,
        EmailHeaderValueKind.Sensitivity,
        EmailHeaderValueKind.AutoSubmitted,
        EmailHeaderValueKind.ListHelp,
        EmailHeaderValueKind.ListUnsubscribe,
        EmailHeaderValueKind.ListSubscribe,
        EmailHeaderValueKind.ListPost,
        EmailHeaderValueKind.ListOwner,
        EmailHeaderValueKind.ListArchive,
        EmailHeaderValueKind.ListUnsubscribePost,
        EmailHeaderValueKind.ReturnPath,
        EmailHeaderValueKind.MimeVersion,
        EmailHeaderValueKind.ContentTransferEncoding,
        EmailHeaderValueKind.ContentLocation,
        EmailHeaderValueKind.Newsgroups,
        EmailHeaderValueKind.FollowupTo,
        EmailHeaderValueKind.ListId,
        EmailHeaderValueKind.Trace,
        EmailHeaderValueKind.Trace,
        EmailHeaderValueKind.Trace,
        EmailHeaderValueKind.Trace,
        EmailHeaderValueKind.Trace,
        EmailHeaderValueKind.Trace,
        EmailHeaderValueKind.Trace,
        EmailHeaderValueKind.Trace
    };
    internal static int GetCountImplementation()
    {
        return fieldNames.Length;
    }

    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <returns>The current count.</returns>
    public int GetCount()
    {
        return EmailHeaderRegistry.GetCountImplementation();
    }

    internal static string GetNameAtImplementation(int index)
    {
        ValidateIndex(index);
        return fieldNames[index];
    }

    /// <summary>
    /// Gets the name at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested name.</returns>
    public string GetNameAt(int index)
    {
        return EmailHeaderRegistry.GetNameAtImplementation(index);
    }

    internal static EmailHeaderValueKind GetValueKindAtImplementation(int index)
    {
        ValidateIndex(index);
        return valueKinds[index];
    }

    /// <summary>
    /// Gets the value kind at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested value kind.</returns>
    public EmailHeaderValueKind GetValueKindAt(int index)
    {
        return EmailHeaderRegistry.GetValueKindAtImplementation(index);
    }

    internal static List<string> GetNamesImplementation()
    {
        return new List<string>(fieldNames);
    }

    /// <summary>
    /// Gets the names.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current names.</returns>
    public List<string> GetNames()
    {
        return EmailHeaderRegistry.GetNamesImplementation();
    }

    internal static bool ContainsImplementation(string fieldName)
    {
        return GetIndex(fieldName) >= 0;
    }

    /// <summary>
    /// Determines whether the collection contains the supplied value.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool Contains(string fieldName)
    {
        return EmailHeaderRegistry.ContainsImplementation(fieldName);
    }

    internal static EmailHeaderValueKind GetValueKindImplementation(string fieldName)
    {
        int index = GetIndex(fieldName);
        return index < 0 ? EmailHeaderValueKind.Unknown : valueKinds[index];
    }

    /// <summary>
    /// Gets the value kind.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <returns>The current value kind.</returns>
    public EmailHeaderValueKind GetValueKind(string fieldName)
    {
        return EmailHeaderRegistry.GetValueKindImplementation(fieldName);
    }

    static int GetIndex(string fieldName)
    {
        if (fieldName == null)
        {
            throw new ArgumentNullException("fieldName");
        }

        int index = 0;
        while (index < fieldNames.Length)
        {
            if (NameEquals(fieldNames[index], fieldName))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    static bool NameEquals(string registeredName, string fieldName)
    {
        if (registeredName.Length != fieldName.Length)
        {
            return false;
        }

        int index = 0;
        while (index < registeredName.Length)
        {
            int registeredCharacter = registeredName[index];
            int fieldCharacter = fieldName[index];
            if (fieldCharacter > 127)
            {
                return false;
            }

            if (registeredCharacter >= 65 && registeredCharacter <= 90)
            {
                registeredCharacter += 32;
            }

            if (fieldCharacter >= 65 && fieldCharacter <= 90)
            {
                fieldCharacter += 32;
            }

            if (registeredCharacter != fieldCharacter)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static void ValidateIndex(int index)
    {
        if (index < 0 || index >= fieldNames.Length)
        {
            throw new ArgumentOutOfRangeException("index");
        }
    }
}
