using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class Rfc9052And9053InteropTests
{
    const string FullInitializationVectorMessage =
        "D08343A1010AA1054D89F52F65A1C580933B5261A78C581C5974E1B99A3A4CC09A659AA2E9E7FFF161D38CE71CB45CE460FFB569";
    const string PartialInitializationVectorMessage =
        "D08343A1010AA1064261A7581C252A8911D465C125B6764739700F0141ED09192DE139E053BD09ABCA";
    const string Sign1Message =
        "D28443A10126A10442313154546869732069732074686520636F6E74656E742E58408EB33E4CA31D1C465AB05AAC34CC6B23D58FEF5C083106C4D25A91AEF0B0117E2AF9A291AA32E14AB834DC56ED2A223444547E01F11D3B0916E5A4C345CACB36";

    [Test]
    public void Sign1MatchesRfc9052AppendixC21AndRfc9053Ecdsa()
    {
        JwkResult keyResult = JsonWebKey.Read(
            "{\"kty\":\"EC\",\"kid\":\"11\",\"crv\":\"P-256\","
            + "\"x\":\"usWxHK2PmfnHKwXPS54m0kTcGJ90UiglWiGahtagnv8\","
            + "\"y\":\"IBOL-C3BttVivg-lSreASjpkttcsz-1rb7btKLv8EX4\","
            + "\"alg\":\"ES256\"}");
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = new string[] { "ES256" };

        CoseSign1Result verified = CoseSign1Operations.Verify(
            Convert.FromHexString(Sign1Message),
            keyResult.Key,
            true,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(keyResult.Success, Is.True);
            Assert.That(verified.Success, Is.True,
                verified.Error == null ? "no error" : verified.Error.Code);
            Assert.That(verified.Message.SignatureVerified, Is.True);
            Assert.That(Encoding.UTF8.GetString(verified.Message.Payload),
                Is.EqualTo("This is the content."));
            Assert.That(verified.Message.Header.KeyId,
                Is.EqualTo(Encoding.ASCII.GetBytes("11")));
        });

        keyResult.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void Encrypt0MatchesRfc9052AppendixC41AndRfc9053AesCcm()
    {
        RequireAesCcm();
        TokenKey key = Key();
        CoseHeader header = Header();
        header.SetUnprotected(
            CoseHeaderLabels.InitializationVector,
            Bytes(Convert.FromHexString(
                "89F52F65A1C580933B5261A78C")));

        TokenBytesResult created = CoseEncrypt0Operations.Create(
            Encoding.UTF8.GetBytes("This is the content."),
            header,
            key,
            Policy());
        CoseEncrypt0Result decrypted = CoseEncrypt0Operations.Decrypt(
            Convert.FromHexString(FullInitializationVectorMessage),
            key,
            Policy());

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True,
                created.Error == null ? "no error" : created.Error.Code);
            Assert.That(created.Bytes, Is.EqualTo(Convert.FromHexString(
                FullInitializationVectorMessage)));
            AssertVerified(decrypted);
        });

        key.ReleaseOwnedMaterial();
    }

    [Test]
    public void Encrypt0MatchesRfc9052AppendixC42PartialIvVector()
    {
        RequireAesCcm();
        TokenKey key = Key();
        key.BaseInitializationVector = Convert.FromHexString(
            "89F52F65A1C580930000000000");
        CoseHeader header = Header();
        header.SetUnprotected(
            CoseHeaderLabels.PartialInitializationVector,
            Bytes(Convert.FromHexString("61A7")));

        TokenBytesResult created = CoseEncrypt0Operations.Create(
            Encoding.UTF8.GetBytes("This is the content."),
            header,
            key,
            Policy());
        CoseEncrypt0Result decrypted = CoseEncrypt0Operations.Decrypt(
            Convert.FromHexString(PartialInitializationVectorMessage),
            key,
            Policy());

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True,
                created.Error == null ? "no error" : created.Error.Code);
            Assert.That(created.Bytes, Is.EqualTo(Convert.FromHexString(
                PartialInitializationVectorMessage)));
            Assert.That(CoseEncrypt0.Read(created.Bytes).Message.Header
                .InitializationVector, Is.Null);
            Assert.That(CoseEncrypt0.Read(created.Bytes).Message.Header
                .PartialInitializationVector,
                Is.EqualTo(Convert.FromHexString("61A7")));
            AssertVerified(decrypted);
        });

        key.ReleaseOwnedMaterial();
    }

    static TokenKey Key()
    {
        return SymmetricKeyImporterOperations.ImportCose(
            Convert.FromHexString(
                "849B5786457C1491BE3A76DCEA6C4271"),
            CoseAlgorithms.AesCcm16_64_128).Key;
    }

    static CoseHeader Header()
    {
        CoseHeader header = new CoseHeader();
        header.SetProtected(
            CoseHeaderLabels.Algorithm,
            Int(CoseAlgorithms.AesCcm16_64_128));
        return header;
    }

    static TokenPolicy Policy()
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = new string[]
        {
            CoseAlgorithmsOperations.GetName(CoseAlgorithms.AesCcm16_64_128)
        };
        return policy;
    }

    static CborItem Int(int value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.UnsignedInteger;
        item.IntegerArgument = (ulong)value;
        return item;
    }

    static CborItem Bytes(byte[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = value;
        return item;
    }

    static void AssertVerified(CoseEncrypt0Result result)
    {
        Assert.That(result.Success, Is.True,
            result.Error == null ? "no error" : result.Error.Code);
        Assert.That(result.Message.AuthenticationTagVerified, Is.True);
        Assert.That(Encoding.UTF8.GetString(result.Message.Plaintext),
            Is.EqualTo("This is the content."));
    }

    static void RequireAesCcm()
    {
        if (!AesCcm.IsSupported)
        {
            Assert.Ignore("AES-CCM is unavailable on this platform.");
        }
    }
}
