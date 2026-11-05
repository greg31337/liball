using System;
using System.Collections.Generic;

namespace jmap;

internal class SubmissionEnvelopeResult
{
    internal EmailSubmissionEnvelope Envelope = new EmailSubmissionEnvelope();
    internal List<string> InvalidEmailProperties = new List<string>();
}

internal class SubmissionEnvelopeProcessor
{
    internal SubmissionValidator Validator = new SubmissionValidator();

    internal SubmissionEnvelopeResult DeriveEnvelope(Email? email, Identity? identity)
    {
        SubmissionEnvelopeResult result = new SubmissionEnvelopeResult();
        if (email == null)
        {
            result.InvalidEmailProperties.Add("");
            return result;
        }

        string? mailFrom = SelectMailFrom(email, result.InvalidEmailProperties);
        if (mailFrom != null && identity != null && identity.Email != null &&
            Validator.IsSmtpMailbox(identity.Email, false) && !IsAllowedByIdentity(mailFrom, identity.Email))
        {
            mailFrom = identity.Email;
        }

        EmailSubmissionAddress fromAddress = new EmailSubmissionAddress();
        fromAddress.Email = mailFrom;
        result.Envelope.MailFrom = fromAddress;

        HashSet<string> recipients = new HashSet<string>(StringComparer.Ordinal);
        AddRecipients(email.To, "to", recipients, result);
        AddRecipients(email.Cc, "cc", recipients, result);
        AddRecipients(email.Bcc, "bcc", recipients, result);
        return result;
    }

    internal string? SelectMailFrom(Email email, List<string> invalid)
    {
        int senderHeaders = CountHeaders(email.Headers, "Sender");
        int fromHeaders = CountHeaders(email.Headers, "From");
        bool duplicateHeaders = false;
        if (senderHeaders > 1)
        {
            invalid.Add("sender");
            duplicateHeaders = true;
        }

        if (fromHeaders > 1)
        {
            invalid.Add("from");
            duplicateHeaders = true;
        }

        if (duplicateHeaders)
        {
            return null;
        }

        bool hasSender = senderHeaders > 0 || email.Sender != null;
        if (hasSender)
        {
            string? selectedSender = SelectSingleAddress(email.Sender, "sender", invalid);
            if (email.From != null && email.From.Count > 1)
            {
                invalid.Add("from");
                return null;
            }

            return selectedSender;
        }

        return SelectSingleAddress(email.From, "from", invalid);
    }

    internal string? SelectSingleAddress(List<EmailAddress>? addresses, string path, List<string> invalid)
    {
        if (addresses == null || addresses.Count != 1 || addresses[0] == null || !Validator.IsSmtpMailbox(addresses[0].Email, false))
        {
            invalid.Add(path);
            return null;
        }

        return addresses[0].Email;
    }

    internal int CountHeaders(List<EmailHeader>? headers, string name)
    {
        if (headers == null)
        {
            return 0;
        }

        int count = 0;
        foreach (EmailHeader header in headers)
        {
            if (header != null && header.Name != null && string.Equals(header.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal void AddRecipients(List<EmailAddress>? addresses, string path, HashSet<string> seen, SubmissionEnvelopeResult result)
    {
        if (addresses == null)
        {
            return;
        }

        int index = 0;
        while (index < addresses.Count)
        {
            EmailAddress address = addresses[index];
            if (address == null || !Validator.IsSmtpMailbox(address.Email, false))
            {
                result.InvalidEmailProperties.Add(path + "/" + index);
            }
            else if (address.Email != null && seen.Add(RecipientKey(address.Email)))
            {
                EmailSubmissionAddress recipient = new EmailSubmissionAddress();
                recipient.Email = address.Email;
                result.Envelope.RcptTo.Add(recipient);
            }

            index = index + 1;
        }
    }

    internal string RecipientKey(string address)
    {
        int separator = address.LastIndexOf('@');
        return address.Substring(0, separator + 1) + address.Substring(separator + 1).ToUpperInvariant();
    }

    internal bool IsAllowedByIdentity(string address, string identityAddress)
    {
        if (string.Equals(address, identityAddress, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (identityAddress.Length < 3 || identityAddress[0] != '*' || identityAddress[1] != '@')
        {
            return false;
        }

        int separator = address.LastIndexOf('@');
        return separator > 0 && string.Equals(address.Substring(separator), identityAddress.Substring(1), StringComparison.OrdinalIgnoreCase);
    }
}
