namespace odf;

internal static class ManifestEncryptionParser
{
    internal static OdfEncryptionInfo ParseEncryptionData(
        OdfManifestNode node,
        OdfManifestEntry entry,
        OdfManifest manifest)
    {
        OdfEncryptionInfo result = new OdfEncryptionInfo();
        result.Node = node;
        result.Size = entry.Size;
        result.SizeLexical = entry.SizeLexical;
        result.HasSize = entry.HasSize;
        result.SizeIsValid = entry.SizeIsValid;
        result.ChecksumType = GetAttributeValue(node, "checksum-type");
        result.ChecksumLexical = GetAttributeValue(node, "checksum");
        OdfLexicalValue checksum = OdfLexicalParser.ParseBase64(
            result.ChecksumLexical);
        result.Checksum = checksum.BinaryValue;
        result.ChecksumIsValid = HasAttributeValue(node, "checksum")
            && checksum.IsValid;
        if (result.ChecksumType.Length == 0)
        {
            ManifestParser.AddManifestDiagnostic(
                manifest,
                OdfDiagnosticCodes.MissingRequiredContent,
                "Manifest encryption data has no checksum type.");
        }
        if (!result.ChecksumIsValid)
        {
            ManifestParser.AddManifestDiagnostic(
                manifest,
                OdfDiagnosticCodes.InvalidDatatype,
                "Manifest encryption checksum is missing or is not valid base64.");
        }

        OdfManifestNode? algorithm = FindDirectChild(node, "algorithm");
        if (algorithm == null)
        {
            ManifestParser.AddManifestDiagnostic(
                manifest,
                OdfDiagnosticCodes.MissingRequiredContent,
                "Manifest encryption data has no algorithm element.");
        }
        else
        {
            result.Algorithm = ParseAlgorithm(algorithm, manifest);
            result.Method = result.Algorithm.Method;
        }

        OdfManifestNode? startKey = FindDirectChild(
            node,
            "start-key-generation");
        if (startKey != null)
        {
            result.HasStartKeyGeneration = true;
            result.StartKeyGeneration = ParseStartKeyGeneration(
                startKey,
                manifest);
        }

        OdfManifestNode? derivation = FindDirectChild(node, "key-derivation");
        if (derivation == null)
        {
            ManifestParser.AddManifestDiagnostic(
                manifest,
                OdfDiagnosticCodes.MissingRequiredContent,
                "Manifest encryption data has no key-derivation element.");
        }
        else
        {
            result.KeyDerivation = ParseKeyDerivation(derivation, manifest);
        }

        result.IsValid = result.ChecksumType.Length > 0
            && result.ChecksumIsValid
            && result.Algorithm.IsValid
            && (!result.HasStartKeyGeneration
                || result.StartKeyGeneration.IsValid)
            && result.KeyDerivation.IsValid
            && (!result.HasSize || result.SizeIsValid);
        return result;
    }

    internal static OdfEncryptedKey ParseEncryptedKey(
        OdfManifestNode node,
        OdfManifest manifest)
    {
        OdfEncryptedKey result = new OdfEncryptedKey();
        result.Node = node;
        OdfManifestNode? method = FindDirectChild(node, "encryption-method");
        if (method != null)
        {
            result.HasPgpAlgorithm = true;
            result.EncryptionMethodNode = method;
            result.PgpAlgorithm = GetAttributeValue(method, "PGPAlgorithm");
            if (result.PgpAlgorithm.Length == 0)
            {
                ManifestParser.AddManifestDiagnostic(
                    manifest,
                    OdfDiagnosticCodes.MissingRequiredContent,
                    "Manifest encrypted-key encryption-method has no PGPAlgorithm value.");
            }
        }

        OdfManifestNode? keyInfo = FindDirectChild(node, "keyinfo");
        result.KeyInfoNode = keyInfo;
        OdfManifestNode? pgpData = keyInfo == null
            ? null
            : FindDirectChild(keyInfo, "PGPData");
        if (pgpData == null)
        {
            ManifestParser.AddManifestDiagnostic(
                manifest,
                OdfDiagnosticCodes.MissingRequiredContent,
                "Manifest encrypted-key has no PGPData element.");
        }
        else
        {
            result.PgpData = ParsePgpData(pgpData, manifest);
        }

        OdfManifestNode? cipherData = FindDirectChild(node, "CipherData");
        if (cipherData == null)
        {
            ManifestParser.AddManifestDiagnostic(
                manifest,
                OdfDiagnosticCodes.MissingRequiredContent,
                "Manifest encrypted-key has no CipherData element.");
        }
        else
        {
            result.CipherData = ParseCipherData(cipherData, manifest);
        }
        result.IsValid = (!result.HasPgpAlgorithm
                || result.PgpAlgorithm.Length > 0)
            && result.PgpData.IsValid
            && result.CipherData.IsValid;
        return result;
    }

    static OdfEncryptionAlgorithm ParseAlgorithm(
        OdfManifestNode node,
        OdfManifest manifest)
    {
        OdfEncryptionAlgorithm result = new OdfEncryptionAlgorithm();
        result.Node = node;
        result.Name = GetAttributeValue(node, "algorithm-name");
        result.Method = MapEncryptionMethod(result.Name);
        result.InitializationVectorLexical = GetAttributeValue(
            node,
            "initialisation-vector");
        OdfLexicalValue vector = OdfLexicalParser.ParseBase64(
            result.InitializationVectorLexical);
        result.InitializationVector = vector.BinaryValue;
        result.InitializationVectorIsValid = HasAttributeValue(
            node,
            "initialisation-vector") && vector.IsValid;
        if (result.Name.Length == 0)
        {
            ManifestParser.AddManifestDiagnostic(
                manifest,
                OdfDiagnosticCodes.MissingRequiredContent,
                "Manifest encryption algorithm has no algorithm name.");
        }
        if (!result.InitializationVectorIsValid)
        {
            ManifestParser.AddManifestDiagnostic(
                manifest,
                OdfDiagnosticCodes.InvalidDatatype,
                "Manifest encryption initialization vector is missing or is not valid base64.");
        }
        result.IsValid = result.Name.Length > 0
            && result.InitializationVectorIsValid;
        return result;
    }

    static OdfStartKeyGeneration ParseStartKeyGeneration(
        OdfManifestNode node,
        OdfManifest manifest)
    {
        OdfStartKeyGeneration result = new OdfStartKeyGeneration();
        result.Node = node;
        result.Name = GetAttributeValue(node, "start-key-generation-name");
        result.HasKeySize = HasAttributeValue(node, "key-size");
        result.KeySizeLexical = GetAttributeValue(node, "key-size");
        result.KeySizeIsValid = !result.HasKeySize;
        if (result.HasKeySize)
        {
            ManifestIntegerValue size = ParseSupportedInteger(
                result.KeySizeLexical);
            result.KeySizeIsValid = size.IsValid;
            if (size.IsValid)
            {
                result.KeySize = (int)size.Value;
            }
        }
        if (result.Name.Length == 0)
        {
            ManifestParser.AddManifestDiagnostic(
                manifest,
                OdfDiagnosticCodes.MissingRequiredContent,
                "Manifest start-key generation has no algorithm name.");
        }
        if (result.HasKeySize && !result.KeySizeIsValid)
        {
            ManifestParser.AddManifestDiagnostic(
                manifest,
                OdfDiagnosticCodes.InvalidDatatype,
                "Manifest start-key size is not a supported non-negative integer.");
        }
        result.IsValid = result.Name.Length > 0 && result.KeySizeIsValid;
        return result;
    }

    static OdfKeyDerivation ParseKeyDerivation(
        OdfManifestNode node,
        OdfManifest manifest)
    {
        OdfKeyDerivation result = new OdfKeyDerivation();
        result.Node = node;
        result.Name = GetAttributeValue(node, "key-derivation-name");
        result.IsPgp = result.Name == "PGP";
        result.SaltLexical = GetAttributeValue(node, "salt");
        OdfLexicalValue salt = OdfLexicalParser.ParseBase64(result.SaltLexical);
        result.Salt = salt.BinaryValue;
        result.SaltIsValid = result.IsPgp
            || HasAttributeValue(node, "salt") && salt.IsValid;
        result.HasIterationCount = HasAttributeValue(node, "iteration-count");
        result.IterationCountLexical = GetAttributeValue(node, "iteration-count");
        result.IterationCountIsValid = result.IsPgp;
        if (result.HasIterationCount)
        {
            ManifestIntegerValue iterations = ParseSupportedInteger(
                result.IterationCountLexical);
            result.IterationCountIsValid = iterations.IsValid;
            if (iterations.IsValid)
            {
                result.IterationCount = (int)iterations.Value;
            }
        }
        result.HasKeySize = HasAttributeValue(node, "key-size");
        result.KeySizeLexical = GetAttributeValue(node, "key-size");
        result.KeySizeIsValid = !result.HasKeySize;
        if (result.HasKeySize)
        {
            ManifestIntegerValue size = ParseSupportedInteger(
                result.KeySizeLexical);
            result.KeySizeIsValid = size.IsValid;
            if (size.IsValid)
            {
                result.KeySize = (int)size.Value;
            }
        }
        if (result.Name.Length == 0)
        {
            ManifestParser.AddManifestDiagnostic(
                manifest,
                OdfDiagnosticCodes.MissingRequiredContent,
                "Manifest key derivation has no algorithm name.");
        }
        if (!result.SaltIsValid)
        {
            ManifestParser.AddManifestDiagnostic(
                manifest,
                OdfDiagnosticCodes.InvalidDatatype,
                "Manifest key-derivation salt is missing or is not valid base64.");
        }
        if (!result.IsPgp
            && (!result.HasIterationCount || !result.IterationCountIsValid))
        {
            ManifestParser.AddManifestDiagnostic(
                manifest,
                OdfDiagnosticCodes.InvalidDatatype,
                "Manifest key-derivation iteration count is missing or invalid.");
        }
        if (result.HasKeySize && !result.KeySizeIsValid)
        {
            ManifestParser.AddManifestDiagnostic(
                manifest,
                OdfDiagnosticCodes.InvalidDatatype,
                "Manifest derived key size is not a supported non-negative integer.");
        }
        result.IsValid = result.Name.Length > 0
            && result.SaltIsValid
            && (result.IsPgp
                || result.HasIterationCount && result.IterationCountIsValid)
            && result.KeySizeIsValid;
        return result;
    }

    static OdfPgpData ParsePgpData(
        OdfManifestNode node,
        OdfManifest manifest)
    {
        OdfPgpData result = new OdfPgpData();
        result.Node = node;
        OdfManifestNode? keyId = FindDirectChild(node, "PGPKeyID");
        result.KeyIdNode = keyId;
        if (keyId != null)
        {
            result.KeyIdLexical = keyId.TextContent;
            OdfLexicalValue parsed = OdfLexicalParser.ParseBase64(
                result.KeyIdLexical);
            result.KeyId = parsed.BinaryValue;
            result.KeyIdIsValid = parsed.IsValid;
        }
        if (keyId == null || !result.KeyIdIsValid)
        {
            ManifestParser.AddManifestDiagnostic(
                manifest,
                OdfDiagnosticCodes.InvalidDatatype,
                "Manifest PGPKeyID is missing or is not valid base64.");
        }
        OdfManifestNode? packet = FindDirectChild(node, "PGPKeyPacket");
        result.KeyPacketNode = packet;
        result.HasKeyPacket = packet != null;
        result.KeyPacketIsValid = packet == null;
        if (packet != null)
        {
            result.KeyPacketLexical = packet.TextContent;
            OdfLexicalValue parsed = OdfLexicalParser.ParseBase64(
                result.KeyPacketLexical);
            result.KeyPacket = parsed.BinaryValue;
            result.KeyPacketIsValid = parsed.IsValid;
            if (!result.KeyPacketIsValid)
            {
                ManifestParser.AddManifestDiagnostic(
                    manifest,
                    OdfDiagnosticCodes.InvalidDatatype,
                    "Manifest PGPKeyPacket is not valid base64.");
            }
        }
        result.IsValid = result.KeyIdIsValid && result.KeyPacketIsValid;
        return result;
    }

    static OdfCipherData ParseCipherData(
        OdfManifestNode node,
        OdfManifest manifest)
    {
        OdfCipherData result = new OdfCipherData();
        result.Node = node;
        OdfManifestNode? value = FindDirectChild(node, "CipherValue");
        result.ValueNode = value;
        if (value != null)
        {
            result.ValueLexical = value.TextContent;
            OdfLexicalValue parsed = OdfLexicalParser.ParseBase64(
                result.ValueLexical);
            result.Value = parsed.BinaryValue;
            result.ValueIsValid = parsed.IsValid;
        }
        if (value == null || !result.ValueIsValid)
        {
            ManifestParser.AddManifestDiagnostic(
                manifest,
                OdfDiagnosticCodes.InvalidDatatype,
                "Manifest CipherValue is missing or is not valid base64.");
        }
        result.IsValid = result.ValueIsValid;
        return result;
    }

    static OdfEncryptionMethod MapEncryptionMethod(string name)
    {
        if (name == OdfAlgorithmIdentifiers.BlowfishCfbName
            || name == OdfAlgorithmIdentifiers.BlowfishCfb)
        {
            return OdfEncryptionMethod.BlowfishCfb;
        }
        if (name == OdfAlgorithmIdentifiers.Aes128Cbc)
        {
            return OdfEncryptionMethod.Aes128Cbc;
        }
        if (name == OdfAlgorithmIdentifiers.Aes192Cbc)
        {
            return OdfEncryptionMethod.Aes192Cbc;
        }
        if (name == OdfAlgorithmIdentifiers.Aes256Cbc)
        {
            return OdfEncryptionMethod.Aes256Cbc;
        }
        return name.Length == 0
            ? OdfEncryptionMethod.Unknown
            : OdfEncryptionMethod.Unsupported;
    }

    static OdfManifestNode? FindDirectChild(
        OdfManifestNode node,
        string localName)
    {
        foreach (OdfManifestNode child in node.Children)
        {
            if (child.Name.NamespaceName == OdfNamespaces.Manifest
                && child.Name.LocalName == localName)
            {
                return child;
            }
        }
        return null;
    }

    static string GetAttributeValue(
        OdfManifestNode node,
        string localName)
    {
        foreach (OdfAttribute attribute in node.Attributes)
        {
            if (attribute.Name.NamespaceName == OdfNamespaces.Manifest
                && attribute.Name.LocalName == localName)
            {
                return attribute.Value;
            }
        }
        return "";
    }

    static bool HasAttributeValue(
        OdfManifestNode node,
        string localName)
    {
        foreach (OdfAttribute attribute in node.Attributes)
        {
            if (attribute.Name.NamespaceName == OdfNamespaces.Manifest
                && attribute.Name.LocalName == localName)
            {
                return true;
            }
        }
        return false;
    }

    static ManifestIntegerValue ParseSupportedInteger(string lexicalValue)
    {
        ManifestIntegerValue result = new ManifestIntegerValue();
        if (lexicalValue.Length == 0)
        {
            return result;
        }
        long value = 0;
        int index = 0;
        while (index < lexicalValue.Length)
        {
            int digit = lexicalValue[index] - '0';
            if (digit < 0 || digit > 9)
            {
                return result;
            }
            if (value > (int.MaxValue - digit) / 10)
            {
                return result;
            }
            value = value * 10 + digit;
            index = index + 1;
        }
        result.Value = value;
        result.IsValid = true;
        return result;
    }
}
