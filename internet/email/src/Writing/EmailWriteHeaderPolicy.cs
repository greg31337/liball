using System;
using System.Collections.Generic;

namespace email;

internal class EmailWriteHeaderPolicy
{
    internal static bool HasBlindRecipientHeaders(EmailHeaders? headers)
    {
        if (headers == null)
        {
            throw new ArgumentNullException("headers");
        }

        return headers.Contains("Bcc") || headers.Contains("Resent-Bcc");
    }

    internal static EmailHeaders Apply(EmailHeaders? headers, EmailWriteOptions options)
    {
        if (headers == null)
        {
            throw new ArgumentNullException("headers");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (options.GetIncludeBlindRecipientHeaders())
        {
            return headers.Clone();
        }

        List<EmailHeader> visible = new List<EmailHeader>();
        int index = 0;
        while (index < headers.GetCount())
        {
            EmailHeader header = headers.GetAt(index);
            string name = header.GetName();
            if (!string.Equals(name, "Bcc", StringComparison.OrdinalIgnoreCase) && !string.Equals(name, "Resent-Bcc", StringComparison.OrdinalIgnoreCase))
            {
                visible.Add(header);
            }

            index++;
        }

        return new EmailHeadersOperations().CreateWithPreservedHeaders(visible);
    }
}
