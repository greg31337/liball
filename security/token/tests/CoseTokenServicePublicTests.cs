using token;
using System.Security.Cryptography;

namespace token.Tests;

public class CoseTokenServicePublicTests
{
    [Test]
    public void HeadersPreserveExactParsedProtectedBytesUntilChanged()
    {
        ICoseTokenOperations service = new CoseTokenService();
        TokenPolicy policy = new TokenPolicy();
        byte[] nonPreferred = Convert.FromHexString(
            "8245BF011801FFA0");

        CoseHeaderResult parsed = service.ReadCoseHeader(
            nonPreferred,
            policy);
        TokenBytesResult exact = service.GetCoseProtectedBytes(
            parsed.Header,
            policy);
        TokenBytesResult written = service.WriteCoseHeader(
            parsed.Header,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Header.HasAlgorithm, Is.True);
            Assert.That(parsed.Header.Algorithm, Is.EqualTo(1));
            Assert.That(parsed.Header.ProtectedBytes,
                Is.EqualTo(Convert.FromHexString("A10101")));
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.Bytes,
                Is.EqualTo(Convert.FromHexString("BF011801FF")));
            Assert.That(written.Success, Is.True);
            Assert.That(written.Bytes,
                Is.EqualTo(Convert.FromHexString("8243A10101A0")));
        });
    }

    [Test]
    public void CriticalExtensionsRequireProtectedCallerUnderstanding()
    {
        ICoseTokenOperations service = new CoseTokenService();
        CoseHeader header = new CoseHeader();
        header.ProtectedParameters = new CborMapEntry[]
        {
            Entry(CoseHeaderLabels.Critical, Array(Int(99))),
            Entry(99, Text("processed"))
        };

        TokenResult understood = service.ValidateCoseCriticalHeaders(
            header,
            new CborItem[] { Int(99) });
        TokenResult unknown = service.ValidateCoseCriticalHeaders(
            header,
            System.Array.Empty<CborItem>());

        Assert.Multiple(() =>
        {
            Assert.That(understood.Success, Is.True);
            Assert.That(unknown.Success, Is.False);
            Assert.That(unknown.Error.Code,
                Is.EqualTo("unknown_cose_critical_header"));
        });
    }

    [Test]
    public void MessageDetectionSupportsRegisteredTagsAndUntaggedContext()
    {
        ICoseTokenOperations service = new CoseTokenService();
        TokenPolicy policy = new TokenPolicy();
        byte[] tagged = Convert.FromHexString("D28440A04040");
        byte[] untagged = Convert.FromHexString("8440A04040");

        CoseMessageResult taggedResult = service.DetectCoseMessage(
            tagged,
            CoseMessageKind.Unknown,
            policy);
        CoseMessageResult untaggedResult = service.DetectCoseMessage(
            untagged,
            CoseMessageKind.Sign1,
            policy);
        CoseMessageResult missingContext = service.DetectCoseMessage(
            untagged,
            CoseMessageKind.Unknown,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(taggedResult.Success, Is.True);
            Assert.That(taggedResult.Kind,
                Is.EqualTo(CoseMessageKind.Sign1));
            Assert.That(taggedResult.Tagged, Is.True);
            Assert.That(taggedResult.Tag, Is.EqualTo(18));
            Assert.That(untaggedResult.Success, Is.True);
            Assert.That(untaggedResult.Tagged, Is.False);
            Assert.That(missingContext.Success, Is.False);
            Assert.That(missingContext.Error.Code,
                Is.EqualTo("untagged_cose_message_type_required"));
        });
    }

    [Test]
    public void AuthenticatedStructuresBindExactBytesExternalDataAndDetachedContent()
    {
        ICoseTokenOperations service = new CoseTokenService();
        TokenPolicy policy = new TokenPolicy();
        byte[] protectedBytes = Convert.FromHexString("A10126");
        byte[] externalData = Convert.FromHexString("AABB");
        byte[] detachedPayload = new byte[] { 1, 2, 3 };

        TokenBytesResult sign1 = service.EncodeCoseSign1Structure(
            protectedBytes,
            externalData,
            detachedPayload,
            policy);
        TokenBytesResult sign = service.EncodeCoseSignStructure(
            protectedBytes,
            Convert.FromHexString("A10127"),
            externalData,
            detachedPayload,
            policy);
        TokenBytesResult encryption =
            service.EncodeCoseEncryptionStructure(
                CoseEncStructureContext.Encrypt0,
                protectedBytes,
                externalData,
                policy);
        TokenBytesResult mac = service.EncodeCoseMacStructure(
            protectedBytes,
            externalData,
            detachedPayload,
            policy);
        TokenBytesResult mac0 = service.EncodeCoseMac0Structure(
            protectedBytes,
            externalData,
            detachedPayload,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(sign1.Bytes, Is.EqualTo(Convert.FromHexString(
                "846A5369676E61747572653143A1012642AABB43010203")));
            Assert.That(sign.Success, Is.True);
            Assert.That(Read(sign.Bytes).ArrayValue[0].Text,
                Is.EqualTo("Signature"));
            Assert.That(Read(sign.Bytes).ArrayValue[4].Bytes,
                Is.EqualTo(detachedPayload));
            Assert.That(encryption.Bytes, Is.EqualTo(Convert.FromHexString(
                "8368456E63727970743043A1012642AABB")));
            Assert.That(mac.Bytes, Is.EqualTo(Convert.FromHexString(
                "84634D414343A1012642AABB43010203")));
            Assert.That(mac0.Bytes, Is.EqualTo(Convert.FromHexString(
                "84644D41433043A1012642AABB43010203")));
        });
    }

    [Test]
    public void CounterSignatureAndKdfStructuresUseTheirRfcContexts()
    {
        ICoseTokenOperations service = new CoseTokenService();
        TokenPolicy policy = new TokenPolicy();
        TokenBytesResult full =
            service.EncodeCoseCounterSignatureStructure(
                new byte[] { 1 },
                new byte[] { 2 },
                new byte[] { 3 },
                new byte[] { 4 },
                new byte[][] { new byte[] { 5 } },
                policy);
        TokenBytesResult abbreviated =
            service.EncodeCoseAbbreviatedCounterSignatureStructure(
                new byte[] { 1 },
                new byte[] { 3 },
                new byte[] { 4 },
                new byte[][] { new byte[] { 5 } },
                policy);
        CoseKdfContextParameters parameters =
            new CoseKdfContextParameters(-25, 128);
        parameters.ProtectedParameters = Convert.FromHexString("A10101");
        TokenBytesResult kdf = service.EncodeCoseKdfContext(
            parameters,
            policy);

        CborItem fullItem = Read(full.Bytes);
        CborItem abbreviatedItem = Read(abbreviated.Bytes);
        CborItem kdfItem = Read(kdf.Bytes);
        Assert.Multiple(() =>
        {
            Assert.That(full.Success, Is.True);
            Assert.That(fullItem.ArrayValue[0].Text,
                Is.EqualTo("CounterSignatureV2"));
            Assert.That(fullItem.ArrayValue[5].ArrayValue[0].Bytes,
                Is.EqualTo(new byte[] { 5 }));
            Assert.That(abbreviated.Success, Is.True);
            Assert.That(abbreviatedItem.ArrayValue[0].Text,
                Is.EqualTo("CounterSignature0V2"));
            Assert.That(kdf.Success, Is.True);
            Assert.That(kdfItem.ArrayValue[0].Kind,
                Is.EqualTo(TokenCborValueKind.NegativeInteger));
            Assert.That(kdfItem.ArrayValue[3].ArrayValue[0].IntegerArgument,
                Is.EqualTo(128));
            Assert.That(kdfItem.ArrayValue[3].ArrayValue[1].Bytes,
                Is.EqualTo(parameters.ProtectedParameters));
        });
    }

    [Test]
    public void NestedRecipientsRoundTripAsStandaloneStructures()
    {
        ICoseTokenOperations service = new CoseTokenService();
        TokenPolicy policy = new TokenPolicy();
        CoseRecipient parent = new CoseRecipient();
        parent.Header = service.ReadCoseHeader(
            Convert.FromHexString("8240A10122"),
            policy).Header;
        parent.EncryptedKey = new byte[] { 1, 2 };
        CoseRecipient child = new CoseRecipient();
        child.Header = service.ReadCoseHeader(
            Convert.FromHexString("8240A10125"),
            policy).Header;
        child.EncryptedKeyAbsent = true;
        child.EncryptedKey = null;
        parent.Recipients = new CoseRecipient[] { child };

        TokenBytesResult written = service.WriteCoseRecipient(
            parent,
            policy);
        CoseRecipientResult parsed = service.ReadCoseRecipient(
            written.Bytes,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(written.Bytes, Is.EqualTo(Convert.FromHexString(
                "8440A10122420102818340A10125F6")));
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Recipient.Header.Algorithm,
                Is.EqualTo(CoseAlgorithms.Aes128KeyWrap));
            Assert.That(parsed.Recipient.EncryptedKey,
                Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(parsed.Recipient.Recipients, Has.Length.EqualTo(1));
            Assert.That(parsed.Recipient.Recipients[0].Header.Algorithm,
                Is.EqualTo(CoseAlgorithms.Direct));
            Assert.That(parsed.Recipient.Recipients[0].EncryptedKeyAbsent,
                Is.True);
            Assert.That(parsed.Recipient.Recipients[0].EncryptedKey,
                Is.Null);
        });
    }

    [Test]
    public void CoseKeysRoundTripThroughTheFoundationService()
    {
        ICoseTokenOperations service = new CoseTokenService();
        TokenKeyResult imported = new TokenKeyService()
            .ImportCoseSymmetricKey(
                Enumerable.Repeat((byte)7, 32).ToArray(),
                CoseAlgorithms.HmacSha256);

        TokenBytesResult written = service.WriteCoseKey(imported.Key);
        CoseKeyResult parsed = service.ReadCoseKey(
            written.Bytes,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(imported.Success, Is.True);
            Assert.That(written.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Key.CoseKeyType,
                Is.EqualTo(CoseKeyLabels.KeyTypeSymmetric));
            Assert.That(parsed.Key.CoseAlgorithm,
                Is.EqualTo(CoseAlgorithms.HmacSha256));
            Assert.That(parsed.Key.SymmetricKey,
                Is.EqualTo(imported.Key.SymmetricKey));
            Assert.That(parsed.Key.SymmetricKey,
                Is.Not.SameAs(imported.Key.SymmetricKey));
        });
    }

    [Test]
    public void DetachedEncryptionReturnsASeparatedAuthenticatedCiphertext()
    {
        ICoseTokenOperations service = new CoseTokenService();
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = new string[] { "A128GCM", "direct" };
        CoseEncrypt message = new CoseEncrypt();
        message.Header = service.ReadCoseHeader(
            Convert.FromHexString("8243A10101A0"),
            policy).Header;
        CoseRecipient recipient = new CoseRecipient();
        recipient.Header = service.ReadCoseHeader(
            Convert.FromHexString("8240A20125044101"),
            policy).Header;
        message.Recipients = new CoseRecipient[] { recipient };
        TokenKeyResult imported = new TokenKeyService()
            .ImportCoseSymmetricKey(
                Enumerable.Repeat((byte)11, 16).ToArray(),
                CoseAlgorithms.Direct);
        imported.Key.Use = TokenKeyUses.Encryption;
        byte[] plaintext = new byte[] { 1, 2, 3, 4 };

        CoseDetachedEncryptionResult encrypted =
            service.EncryptDetachedCose(
                plaintext,
                message,
                new TokenKey[] { imported.Key },
                policy);
        CoseMessageResult detected = service.DetectCoseMessage(
            encrypted.Bytes,
            CoseMessageKind.Encrypt,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            Assert.That(encrypted.Ciphertext,
                Has.Length.EqualTo(plaintext.Length + 16));
            Assert.That(detected.Success, Is.True);
            Assert.That(detected.Item.ArrayValue[2].Kind,
                Is.EqualTo(TokenCborValueKind.Null));
            Assert.That(detected.Item.ArrayValue[3].ArrayValue,
                Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void CounterSignatureCreationUsesTheSuppliedTargetAndHeader()
    {
        ICoseTokenOperations service = new CoseTokenService();
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = new string[] { "ES256" };
        CoseHeader header = service.ReadCoseHeader(
            Convert.FromHexString("8243A10128A0"),
            policy).Header;
        CoseCounterSignature model = new CoseCounterSignature();
        model.Header = header;
        model.Tagged = false;
        CoseCounterSignatureTarget target =
            new CoseCounterSignatureTarget();
        target.BodyProtected = Convert.FromHexString("A10101");
        target.Payload = new byte[] { 1, 2, 3 };
        using ECDsa native = ECDsa.Create(
            ECCurve.NamedCurves.nistP256);
        TokenKeyResult imported = new TokenKeyService()
            .ImportCoseEc2Key(
                native,
                CoseAlgorithms.EcdsaP256Sha256,
                true);

        CoseCounterSignatureResult result =
            service.CreateCoseCounterSignature(
                target,
                model,
                imported.Key,
                policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.CounterSignature.Tagged, Is.False);
            Assert.That(result.CounterSignature.Signature,
                Has.Length.EqualTo(64));
            Assert.That(result.CounterSignature.Header.ProtectedBytes,
                Is.EqualTo(Convert.FromHexString("A10128")));
        });
    }

    static CborItem Read(byte[] bytes)
    {
        return new TokenCborCodec().ReadCbor(
            bytes,
            bytes.Length,
            16).Item;
    }

    static CborMapEntry Entry(int label, CborItem value)
    {
        return new CborMapEntry(Int(label), value);
    }

    static CborItem Int(int value)
    {
        CborItem item = new CborItem();
        if (value >= 0)
        {
            item.Kind = TokenCborValueKind.UnsignedInteger;
            item.IntegerArgument = (ulong)value;
        }
        else
        {
            item.Kind = TokenCborValueKind.NegativeInteger;
            item.IntegerArgument = (ulong)(-(long)value - 1L);
        }

        return item;
    }

    static CborItem Text(string value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.TextString;
        item.Text = value;
        return item;
    }

    static CborItem Array(params CborItem[] values)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Array;
        item.ArrayValue = values;
        return item;
    }
}
