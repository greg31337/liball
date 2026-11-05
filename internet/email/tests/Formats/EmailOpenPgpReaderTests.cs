using System.Text;
using email;

namespace email.Tests;

public class EmailOpenPgpReaderTests
{
    const string Rfc9580Version4Key = "-----BEGIN PGP PUBLIC KEY BLOCK-----\r\n\r\nxjMEU/NfCxYJKwYBBAHaRw8BAQdAPwmJlL3ZFu1AUxl5NOSofIBzOhKA1i+AEJku\r\nQ+47JAY=\r\n-----END PGP PUBLIC KEY BLOCK-----\r\n";
    const string Rfc9580Version6Certificate = "-----BEGIN PGP PUBLIC KEY BLOCK-----\r\n\r\nxioGY4d/4xsAAAAg+U2nu0jWCmHlZ3BqZYfQMxmZu52JGggkLq2EVD34laPCsQYf\r\nGwoAAABCBYJjh3/jAwsJBwUVCg4IDAIWAAKbAwIeCSIhBssYbE8GCaaX5NUt+mxy\r\nKwwfHifBilZwj2Ul7Ce62azJBScJAgcCAAAAAK0oIBA+LX0ifsDm185Ecds2v8lw\r\ngyU2kCcUmKfvBXbAf6rhRYWzuQOwEn7E/aLwIwRaLsdry0+VcallHhSu4RN6HWaE\r\nQsiPlR4zxP/TP7mhfVEe7XWPxtnMUMtf15OyA51YBM4qBmOHf+MZAAAAIIaTJINn\r\n+eUBXbki+PSAld2nhJh/LVmFsS+60WyvXkQ1wpsGGBsKAAAALAWCY4d/4wKbDCIh\r\nBssYbE8GCaaX5NUt+mxyKwwfHifBilZwj2Ul7Ce62azJAAAAAAQBIKbpGG2dWTX8\r\nj+VjFM21J0hqWlEg+bdiojWnKfA5AQpWUWtnNwDEM0g12vYxoWM8Y81W+bHBw805\r\nI8kWVkXU6vFOi+HWvv/ira7ofJu16NnoUkhclkUrk0mXubZvyl4GBg==\r\n-----END PGP PUBLIC KEY BLOCK-----\r\n";

    [Test]
    public void EncodesAndDecodesAsciiArmorWithCrc24()
    {
        byte[] packet = Packet(11, LiteralBody("mail.txt", "armored body"));
        byte[] armorBytes = new EmailOpenPgpArmor().EncodeWithDataAndKindAndIncludeChecksum(packet, EmailOpenPgpArmorKind.Message, true);
        byte[] defaultArmorBytes = new EmailOpenPgpArmor().EncodeWithDataAndKind(packet, EmailOpenPgpArmorKind.Message);

        EmailOpenPgpArmorResult armor = new EmailOpenPgpArmor().DecodeWithInput(armorBytes);
        EmailOpenPgpReadResult parsed = new EmailOpenPgpReader().ParseWithInput(armorBytes);

        Assert.Multiple(() =>
        {
            Assert.That(new EmailOpenPgpArmor().ComputeCrc24(Bytes("123456789")), Is.EqualTo(0x21CF02));
            Assert.That(armor.IsSuccess(), Is.True);
            Assert.That(armor.GetKind(), Is.EqualTo(EmailOpenPgpArmorKind.Message));
            Assert.That(armor.HasChecksum(), Is.True);
            Assert.That(armor.IsChecksumValid(), Is.True);
            Assert.That(armor.GetData(), Is.EqualTo(packet));
            Assert.That(new EmailOpenPgpArmor().DecodeWithInput(defaultArmorBytes).GetData(), Is.EqualTo(packet));
            Assert.That(armor.GetHeaders(), Is.Empty);
            Assert.That(parsed.IsSuccess(), Is.True);
            Assert.That(parsed.GetPackets().Count, Is.EqualTo(1));
            Assert.That(parsed.GetOriginalBytes(), Is.EqualTo(armorBytes));
            Assert.That(parsed.WasArmored(), Is.True);
            Assert.That(parsed.GetPacketAt(0).GetLiteralData()!.GetFileName(), Is.EqualTo("mail.txt"));
            Assert.That(parsed.GetPacketAt(0).GetLiteralData()!.GetData(), Is.EqualTo(Bytes("armored body")));
        });

        EmailOpenPgpArmorHeader header = new EmailOpenPgpArmorHeader("Version", "test-suite");
        Assert.That(header.GetName(), Is.EqualTo("Version"));
        Assert.That(header.GetValue(), Is.EqualTo("test-suite"));

        byte[] returned = armor.GetData();
        returned[0] = 0;
        Assert.That(armor.GetData(), Is.EqualTo(packet));
    }

    [Test]
    public void ReportsButDoesNotRejectBadOptionalArmorChecksum()
    {
        string armored = Encoding.ASCII.GetString(new EmailOpenPgpArmor().EncodeWithDataAndKindAndIncludeChecksum(Packet(13, Bytes("user")), EmailOpenPgpArmorKind.PublicKey, true));
        int checksum = armored.IndexOf("=", armored.IndexOf("\r\n\r\n", StringComparison.Ordinal) + 4, StringComparison.Ordinal);
        char replacement = armored[checksum + 1] == 'A' ? 'B' : 'A';
        armored = armored.Substring(0, checksum + 1) + replacement + armored.Substring(checksum + 2);

        EmailOpenPgpReadResult result = new EmailOpenPgpReader().ParseWithInput(Bytes(armored));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetPacketCount(), Is.EqualTo(1));
            Assert.That(result.GetArmor()!.HasChecksum(), Is.True);
            Assert.That(result.GetArmor()!.IsChecksumValid(), Is.False);
            Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.OpenPgpArmorChecksumMismatch), Is.True);
        });
    }

    [Test]
    public void ParsesRfc9580Version4KeyAndFingerprint()
    {
        EmailOpenPgpReadResult result = new EmailOpenPgpReader().ParseWithInput(Bytes(Rfc9580Version4Key));
        EmailOpenPgpPacket packet = result.GetPacketAt(0);
        EmailOpenPgpKeyPacket key = packet.GetKey()!;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetArmor()!.GetKind(), Is.EqualTo(EmailOpenPgpArmorKind.PublicKey));
            Assert.That(packet.GetKind(), Is.EqualTo(EmailOpenPgpPacketKind.PublicKey));
            Assert.That(packet.HasLegacyHeader(), Is.False);
            Assert.That(key!.GetVersion(), Is.EqualTo(4));
            Assert.That(key.GetCreationTime(), Is.GreaterThan(0));
            Assert.That(key.GetPublicKeyAlgorithm(), Is.EqualTo(22));
            Assert.That(key.GetPublicKeyMaterial(), Is.Not.Empty);
            Assert.That(key.HasFingerprint(), Is.True);
            Assert.That(key.IsSecret(), Is.False);
            Assert.That(Convert.ToHexString(key.GetFingerprint()), Is.EqualTo("C959BDBAFA32A2F89A153B678CFDE12197965A9A"));
            Assert.That(Convert.ToHexString(key.GetKeyIdentifier()), Is.EqualTo("8CFDE12197965A9A"));
        });
    }

    [Test]
    public void ParsesRfc9580Version6CertificateFingerprintsAndSignatures()
    {
        EmailOpenPgpReadResult result = new EmailOpenPgpReader().ParseWithInput(Bytes(Rfc9580Version6Certificate));
        EmailOpenPgpKeyPacket primary = result.GetPacketAt(0).GetKey()!;
        EmailOpenPgpSignaturePacket directSignature = result.GetPacketAt(1).GetSignature()!;
        EmailOpenPgpKeyPacket subkey = result.GetPacketAt(2).GetKey()!;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetPacketCount(), Is.EqualTo(4));
            Assert.That(primary!.GetVersion(), Is.EqualTo(6));
            Assert.That(primary.GetPublicKeyAlgorithm(), Is.EqualTo(27));
            Assert.That(Convert.ToHexString(primary.GetFingerprint()), Is.EqualTo("CB186C4F0609A697E4D52DFA6C722B0C1F1E27C18A56708F6525EC27BAD9ACC9"));
            Assert.That(Convert.ToHexString(primary.GetKeyIdentifier()), Is.EqualTo("CB186C4F0609A697"));
            Assert.That(directSignature!.GetVersion(), Is.EqualTo(6));
            Assert.That(directSignature.GetSignatureType(), Is.EqualTo(0x1F));
            Assert.That(directSignature.GetSalt().Length, Is.EqualTo(32));
            Assert.That(subkey!.IsSubkey(), Is.True);
            Assert.That(Convert.ToHexString(subkey.GetFingerprint()), Is.EqualTo("12C83F1E706F6308FE151A417743A1F033790E93E9978488D1DB378DA9930885"));
        });
    }

    [Test]
    public void ParsesOneTwoAndFiveOctetPacketLengths()
    {
        byte[] small = Packet(13, new byte[191]);
        byte[] medium = Packet(13, new byte[192]);
        byte[] large = Packet(13, new byte[8384]);
        byte[] sequence = Join(small, medium, large);

        EmailOpenPgpReadResult result = new EmailOpenPgpReader().ParseWithInput(sequence);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetPacketCount(), Is.EqualTo(3));
            Assert.That(result.GetPacketAt(0).GetBody().Length, Is.EqualTo(191));
            Assert.That(result.GetPacketAt(1).GetBody().Length, Is.EqualTo(192));
            Assert.That(result.GetPacketAt(2).GetBody().Length, Is.EqualTo(8384));
            Assert.That(result.GetPacketAt(0).GetEncodedBytes(), Is.EqualTo(small));
            Assert.That(result.GetPacketAt(2).GetOffset(), Is.EqualTo(small.Length + medium.Length));
        });
    }

    [Test]
    public void ReassemblesPermittedPartialLiteralBodyLengths()
    {
        byte[] body = LiteralBody(string.Empty, new string('x', 1024));
        Assert.That(body.Length, Is.EqualTo(1030));
        byte[] encoded = new byte[1 + 1 + 512 + 1 + 512 + 1 + 6];
        int position = 0;
        encoded[position++] = 0xCB;
        encoded[position++] = 0xE9;
        Array.Copy(body, 0, encoded, position, 512);
        position += 512;
        encoded[position++] = 0xE9;
        Array.Copy(body, 512, encoded, position, 512);
        position += 512;
        encoded[position++] = 6;
        Array.Copy(body, 1024, encoded, position, 6);

        EmailOpenPgpReadResult result = new EmailOpenPgpReader().ParseWithInput(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetPacketAt(0).UsesPartialLengths(), Is.True);
            Assert.That(result.GetPacketAt(0).GetBody(), Is.EqualTo(body));
            Assert.That(result.GetPacketAt(0).GetEncodedBytes(), Is.EqualTo(encoded));
            Assert.That(result.GetPacketAt(0).GetLiteralData()!.GetData().Length, Is.EqualTo(1024));
        });
    }

    [Test]
    public void ExposesLiteralCompressedSignatureAndSessionPacketFields()
    {
        byte[] literal = Packet(11, LiteralBody("a.txt", "hello"));
        byte[] compressed = Packet(8, [2, 9, 8, 7]);
        byte[] signature = Packet(2, [4, 0, 1, 8, 0, 3, 2, 0x82, 1, 0, 2, 1, 16, 0x12, 0x34, 0, 8, 1]);
        byte[] publicSession = Packet(1, [3, 1, 2, 3, 4, 5, 6, 7, 8, 1, 0xAA, 0xBB]);
        byte[] symmetricSession = Packet(3, [4, 9, 0, 8, 1, 2]);
        byte[] recipientFingerprint = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
        byte[] publicVersion6Body = Join([6, 33, 6], recipientFingerprint, [25, 0xCC]);
        byte[] publicVersion6 = Packet(1, publicVersion6Body);
        byte[] symmetricVersion6 = Packet(3, [6, 6, 9, 2, 2, 1, 8, 0, 0xDD]);

        EmailOpenPgpReadResult result = new EmailOpenPgpReader().ParseWithInput(Join(literal, compressed, signature, publicSession, symmetricSession, publicVersion6, symmetricVersion6));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetPacketCount(), Is.EqualTo(7));
            Assert.That(result.GetPacketAt(0).GetLiteralData()!.GetTimestamp(), Is.EqualTo(0x01020304));
            Assert.That(result.GetPacketAt(0).GetLiteralData()!.GetFormat(), Is.EqualTo((int)'b'));
            Assert.That(result.GetPacketAt(0).GetLiteralData()!.GetFileNameBytes(), Is.EqualTo(Bytes("a.txt")));
            Assert.That(result.GetPacketAt(0).GetLiteralData()!.GetData(), Is.EqualTo(Bytes("hello")));
            Assert.That(result.GetPacketAt(1).GetCompressedData()!.GetAlgorithm(), Is.EqualTo(2));
            Assert.That(result.GetPacketAt(1).GetCompressedData()!.GetCompressedData(), Is.EqualTo(new byte[] { 9, 8, 7 }));
            Assert.That(result.GetPacketAt(2).GetSignature()!.GetHashedSubpackets(), Is.EqualTo(new byte[] { 2, 0x82, 1 }));
            Assert.That(result.GetPacketAt(2).GetSignature()!.GetUnhashedSubpackets(), Is.EqualTo(new byte[] { 1, 16 }));
            Assert.That(result.GetPacketAt(2).GetSignature()!.GetHashPrefix(), Is.EqualTo(new byte[] { 0x12, 0x34 }));
            Assert.That(result.GetPacketAt(2).GetSignature()!.GetIssuerKeyIdentifier(), Is.Empty);
            Assert.That(result.GetPacketAt(2).GetSignature()!.GetParsedHashedSubpackets()[0].GetTypeIdentifier(), Is.EqualTo(2));
            Assert.That(result.GetPacketAt(2).GetSignature()!.GetParsedHashedSubpackets()[0].IsCritical(), Is.True);
            Assert.That(result.GetPacketAt(2).GetSignature()!.GetParsedHashedSubpackets()[0].GetData(), Is.EqualTo(new byte[] { 1 }));
            Assert.That(result.GetPacketAt(2).GetSignature()!.GetParsedHashedSubpackets()[0].GetEncodedBytes(), Is.EqualTo(new byte[] { 2, 0x82, 1 }));
            Assert.That(result.GetPacketAt(3).GetSessionKey()!.IsPublicKeyEncrypted(), Is.True);
            Assert.That(result.GetPacketAt(3).GetSessionKey()!.GetRecipientIdentifier(), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
            Assert.That(result.GetPacketAt(3).GetSessionKey()!.GetPublicKeyAlgorithm(), Is.EqualTo(1));
            Assert.That(result.GetPacketAt(4).GetSessionKey()!.GetSymmetricAlgorithm(), Is.EqualTo(9));
            Assert.That(result.GetPacketAt(4).GetSessionKey()!.GetStringToKeySpecifier(), Is.EqualTo(new byte[] { 0, 8 }));
            Assert.That(result.GetPacketAt(4).GetSessionKey()!.GetEncryptedSessionKey(), Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(result.GetPacketAt(5).GetSessionKey()!.GetVersion(), Is.EqualTo(6));
            Assert.That(result.GetPacketAt(5).GetSessionKey()!.GetRecipientIdentifier(), Is.EqualTo(recipientFingerprint));
            Assert.That(result.GetPacketAt(5).GetSessionKey()!.GetPublicKeyAlgorithm(), Is.EqualTo(25));
            Assert.That(result.GetPacketAt(6).GetSessionKey()!.GetSymmetricAlgorithm(), Is.EqualTo(9));
            Assert.That(result.GetPacketAt(6).GetSessionKey()!.GetAeadAlgorithm(), Is.EqualTo(2));
            Assert.That(result.GetPacketAt(6).GetSessionKey()!.GetStringToKeySpecifier(), Is.EqualTo(new byte[] { 1, 8 }));
            Assert.That(result.GetPacketAt(6).GetSessionKey()!.GetEncryptedSessionKey(), Is.EqualTo(new byte[] { 0xDD }));
        });
    }

    [Test]
    public void SupportsBoundedLegacyPacketsAndRejectsMalformedFraming()
    {
        byte[] legacyLiteralBody = LiteralBody(string.Empty, "legacy");
        byte[] legacy = new byte[2 + legacyLiteralBody.Length];
        legacy[0] = 0xAC;
        legacy[1] = (byte)legacyLiteralBody.Length;
        Array.Copy(legacyLiteralBody, 0, legacy, 2, legacyLiteralBody.Length);
        EmailOpenPgpReadResult accepted = new EmailOpenPgpReader().ParseWithInput(legacy);
        EmailOpenPgpReaderOptions disabledOptions = new EmailOpenPgpReaderOptions();
        disabledOptions.SetAllowLegacyPacketHeaders(false);
        disabledOptions.SetMaximumArmorHeaderCount(3);
        EmailOpenPgpReadResult disabled = new EmailOpenPgpReader().ParseWithInputAndOptions(legacy, disabledOptions);
        EmailOpenPgpReadResult truncated = new EmailOpenPgpReader().ParseWithInput([0xCB, 10, 1, 2]);
        EmailOpenPgpReadResult unfinishedPartial = new EmailOpenPgpReader().ParseWithInput(Join([0xCB, 0xE9], new byte[512]));

        EmailCryptographicLimits countLimits = new EmailCryptographicLimits();
        countLimits.SetMaxOpenPgpPacketCount(1);
        EmailOpenPgpReaderOptions countOptions = new EmailOpenPgpReaderOptions();
        countOptions.SetLimits(countLimits);
        EmailLimitException count = Assert.Throws<EmailLimitException>(() => new EmailOpenPgpReader().ParseWithInputAndOptions(Join(Packet(13, [1]), Packet(13, [2])), countOptions));

        Assert.Multiple(() =>
        {
            Assert.That(accepted.IsSuccess(), Is.True);
            Assert.That(accepted.GetPacketAt(0).HasLegacyHeader(), Is.True);
            Assert.That(accepted.GetPacketAt(0).GetLiteralData()!.GetData(), Is.EqualTo(Bytes("legacy")));
            Assert.That(disabled.IsSuccess(), Is.False);
            Assert.That(disabledOptions.GetMaximumArmorHeaderCount(), Is.EqualTo(3));
            Assert.That(truncated.IsSuccess(), Is.False);
            Assert.That(truncated.GetIssues().ContainsCode(EmailIssueCode.OpenPgpPacketMalformed), Is.True);
            Assert.That(unfinishedPartial.IsSuccess(), Is.False);
            Assert.That(count.GetLimitName(), Is.EqualTo("MaxOpenPgpPacketCount"));
        });
    }

    static byte[] LiteralBody(string fileName, string data)
    {
        byte[] name = Bytes(fileName);
        byte[] content = Bytes(data);
        byte[] body = new byte[6 + name.Length + content.Length];
        body[0] = (byte)'b';
        body[1] = (byte)name.Length;
        Array.Copy(name, 0, body, 2, name.Length);
        int time = 2 + name.Length;
        body[time] = 1;
        body[time + 1] = 2;
        body[time + 2] = 3;
        body[time + 3] = 4;
        Array.Copy(content, 0, body, time + 4, content.Length);
        return body;
    }

    static byte[] Packet(int type, byte[] body)
    {
        List<byte> result = new List<byte> { (byte)(0xC0 | type) };
        int length = body.Length;
        if (length < 192) result.Add((byte)length);
        else if (length <= 8383)
        {
            int adjusted = length - 192;
            result.Add((byte)((adjusted >> 8) + 192));
            result.Add((byte)adjusted);
        }
        else
        {
            result.Add(255);
            result.Add((byte)(length >> 24));
            result.Add((byte)(length >> 16));
            result.Add((byte)(length >> 8));
            result.Add((byte)length);
        }
        result.AddRange(body);
        return result.ToArray();
    }

    static byte[] Join(params byte[][] values)
    {
        int length = values.Sum(value => value.Length);
        byte[] result = new byte[length];
        int offset = 0;
        foreach (byte[] value in values) { Array.Copy(value, 0, result, offset, value.Length); offset += value.Length; }
        return result;
    }

    static byte[] Bytes(string value) { return Encoding.ASCII.GetBytes(value); }
}
