using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace email;
internal class EmailSmimeSignedDataLayout
{
    string contentType;
    byte[]? embeddedContent;
    EmailCmsAsn1Node? certificateNode;
    EmailCmsAsn1Node signerInfos;
    HashSet<string> digestOids;

    internal EmailSmimeSignedDataLayout(string contentType, byte[]? embeddedContent, EmailCmsAsn1Node? certificateNode, EmailCmsAsn1Node signerInfos, HashSet<string> digestOids)
    {
        this.contentType = contentType;
        this.embeddedContent = embeddedContent;
        this.certificateNode = certificateNode;
        this.signerInfos = signerInfos;
        this.digestOids = digestOids;
    }

    internal string GetContentType() { return contentType; }
    internal bool HasEmbeddedContent() { return embeddedContent != null; }
    internal byte[]? GetEmbeddedContent() { return embeddedContent; }
    internal EmailCmsAsn1Node? GetCertificateNode() { return certificateNode; }
    internal EmailCmsAsn1Node GetSignerInfos() { return signerInfos; }
    internal bool ContainsDigestOid(string? value) { return value != null && digestOids.Contains(value); }
}
internal class EmailSmimeAttributeValues
{
    byte[] encodedSet;
    byte[] messageDigest;

    internal EmailSmimeAttributeValues(byte[] encodedSet, byte[] messageDigest)
    {
        this.encodedSet = encodedSet;
        this.messageDigest = messageDigest;
    }

    internal byte[] GetEncodedSet() { return encodedSet; }
    internal byte[] GetMessageDigest() { return messageDigest; }
}
