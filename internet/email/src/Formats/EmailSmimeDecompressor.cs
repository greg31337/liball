using System;
using System.Formats.Asn1;
using System.IO;
using System.IO.Compression;
using System.Numerics;

namespace email;
internal class EmailSmimeCompressedDataLayout
{
    string algorithmObjectIdentifier;
    string contentTypeObjectIdentifier;
    byte[] compressedContent;
    bool permittedZlibParameters;

    internal EmailSmimeCompressedDataLayout(string algorithmObjectIdentifier, string contentTypeObjectIdentifier, byte[] compressedContent, bool permittedZlibParameters)
    {
        this.algorithmObjectIdentifier = algorithmObjectIdentifier;
        this.contentTypeObjectIdentifier = contentTypeObjectIdentifier;
        this.compressedContent = compressedContent;
        this.permittedZlibParameters = permittedZlibParameters;
    }

    internal string GetAlgorithmObjectIdentifier() { return algorithmObjectIdentifier; }
    internal string GetContentTypeObjectIdentifier() { return contentTypeObjectIdentifier; }
    internal byte[] GetCompressedContent() { return compressedContent; }
    internal bool HasPermittedZlibParameters() { return permittedZlibParameters; }
}
