using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace token;

internal class SelectiveDisclosureProcessingState
{
    internal SelectiveDisclosure[]? Disclosures;
    internal byte[][] DisclosureDigests;
    internal bool[] UsedDisclosures;
    internal List<byte[]> EmbeddedDigests;
    internal string HashAlgorithm;
    internal TokenError? Error;

    internal SelectiveDisclosureProcessingState(
        SelectiveDisclosure[]? disclosures,
        byte[][] disclosureDigests,
        string hashAlgorithm)
    {
        Disclosures = disclosures;
        DisclosureDigests = disclosureDigests;
        UsedDisclosures = new bool[(disclosures ?? throw new InvalidOperationException("A required value is null.")).Length];
        EmbeddedDigests = new List<byte[]>();
        HashAlgorithm = hashAlgorithm;
        Error = null;
    }
}
