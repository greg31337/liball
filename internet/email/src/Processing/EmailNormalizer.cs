using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailNormalizationContent
{
    EmailContent? rawContent;
    EmailContent? decodedContent;
    string? transferEncodingName;

    internal EmailNormalizationContent(EmailContent? rawContent, EmailContent? decodedContent, string? transferEncodingName)
    {
        this.rawContent = rawContent;
        this.decodedContent = decodedContent;
        this.transferEncodingName = transferEncodingName;
    }

    internal EmailContent? GetRawContent() { return rawContent; }
    internal EmailContent? GetDecodedContent() { return decodedContent; }
    internal string? GetTransferEncodingName() { return transferEncodingName; }
}
