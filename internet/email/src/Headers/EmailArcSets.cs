using System;
using System.Collections.Generic;

namespace email;
internal class EmailArcSetBuilder
{
    int instance;
    List<EmailArcHeaderField> authenticationResultsFields;
    List<EmailArcHeaderField> messageSignatureFields;
    List<EmailArcHeaderField> sealFields;

    internal EmailArcSetBuilder(int instance)
    {
        this.instance = instance;
        authenticationResultsFields = new List<EmailArcHeaderField>();
        messageSignatureFields = new List<EmailArcHeaderField>();
        sealFields = new List<EmailArcHeaderField>();
    }

    internal void Add(EmailArcHeaderField field)
    {
        if (field.GetKind() == EmailSignatureHeaderKind.ArcAuthenticationResults) authenticationResultsFields.Add(field);
        else if (field.GetKind() == EmailSignatureHeaderKind.ArcMessageSignature) messageSignatureFields.Add(field);
        else sealFields.Add(field);
    }

    internal EmailArcSet Build()
    {
        return new EmailArcSet(instance, authenticationResultsFields, messageSignatureFields, sealFields);
    }
}
