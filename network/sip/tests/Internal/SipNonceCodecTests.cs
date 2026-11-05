namespace sip.Tests;

public class SipNonceCodecTests
{
    [Test]
    public void CreatesOpaqueRandomNonceAndOpaqueData()
    {
        NonceRecordingRandomSource random = new NonceRecordingRandomSource();
        SipNonceCodec codec = new SipNonceCodec(random,
            new NonceManualTimeSource(1_700_000_000));
        SipNonceOptions options = CreateOptions("example.com", 1);

        string firstNonce = codec.CreateNonce(options);
        string opaque = codec.CreateOpaqueData();
        string secondNonce = codec.CreateNonce(options);

        Assert.Multiple(() =>
        {
            Assert.That(firstNonce, Does.Match("^[A-Za-z0-9_-]{76}$"));
            Assert.That(opaque, Does.Match("^[A-Za-z0-9_-]{32}$"));
            Assert.That(secondNonce, Does.Match("^[A-Za-z0-9_-]{76}$"));
            Assert.That(secondNonce, Is.Not.EqualTo(firstNonce));
            Assert.That(firstNonce, Does.Not.Contain("example.com"));
            Assert.That(random.RequestedByteCounts,
                Is.EqualTo(new int[] { 16, 24, 16 }));
        });
    }

    [Test]
    public void ValidatesAuthenticNonceWithinLifetime()
    {
        NonceManualTimeSource time = new NonceManualTimeSource(1000);
        SipNonceCodec codec = new SipNonceCodec(
            new NonceRecordingRandomSource(), time);
        SipNonceOptions options = CreateOptions("example.com", 2);
        string nonce = codec.CreateNonce(options);

        time.UnixTimeSeconds = 1200;

        Assert.That(codec.ValidateNonce(nonce, options),
            Is.EqualTo(SipAuthenticationStatus.Success));
    }

    [Test]
    public void AppliesLifetimeAndClockSkewBoundaries()
    {
        NonceManualTimeSource time = new NonceManualTimeSource(1000);
        SipNonceCodec codec = new SipNonceCodec(
            new NonceRecordingRandomSource(), time);
        SipNonceOptions options = CreateOptions("example.com", 3);
        options.LifetimeSeconds = 300;
        options.ClockSkewSeconds = 30;
        string nonce = codec.CreateNonce(options);

        time.UnixTimeSeconds = 1330;
        SipAuthenticationStatus lastValid = codec.ValidateNonce(nonce, options);
        time.UnixTimeSeconds = 1331;
        SipAuthenticationStatus stale = codec.ValidateNonce(nonce, options);
        time.UnixTimeSeconds = 970;
        SipAuthenticationStatus earliestValid = codec.ValidateNonce(nonce, options);
        time.UnixTimeSeconds = 969;
        SipAuthenticationStatus future = codec.ValidateNonce(nonce, options);

        Assert.Multiple(() =>
        {
            Assert.That(lastValid, Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(stale, Is.EqualTo(SipAuthenticationStatus.StaleNonce));
            Assert.That(earliestValid, Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(future, Is.EqualTo(SipAuthenticationStatus.Failed));
        });
    }

    [Test]
    public void RejectsTamperingThroughoutNonce()
    {
        SipNonceOptions options = CreateOptions("example.com", 4);
        SipNonceCodec codec = new SipNonceCodec(
            new NonceRecordingRandomSource(), new NonceManualTimeSource(1000));
        string nonce = codec.CreateNonce(options);
        int[] positions = new int[] { 0, 8, 24, 43, 75 };

        int index = 0;
        while (index < positions.Length)
        {
            string tampered = ChangeCharacter(nonce, positions[index]);
            Assert.That(codec.ValidateNonce(tampered, options),
                Is.EqualTo(SipAuthenticationStatus.Failed));
            index = index + 1;
        }
    }

    [Test]
    public void RejectsEveryAuthenticationTagMutation()
    {
        SipNonceOptions options = CreateOptions("example.com", 14);
        SipNonceCodec codec = new SipNonceCodec(
            new NonceRecordingRandomSource(), new NonceManualTimeSource(1000));
        string nonce = codec.CreateNonce(options);
        byte[] decoded = DecodeBase64Url(nonce);

        int index = 25;
        while (index < decoded.Length)
        {
            byte[] changed = (byte[])decoded.Clone();
            changed[index] = (byte)(changed[index] ^ 1);
            string tampered = EncodeBase64Url(changed);
            Assert.That(codec.ValidateNonce(tampered, options),
                Is.EqualTo(SipAuthenticationStatus.Failed),
                "Authentication byte " + (index - 25));
            index = index + 1;
        }
    }

    [Test]
    public void RejectsAuthenticallySignedInvalidNoncePayloads()
    {
        NonceManualTimeSource time = new NonceManualTimeSource(1000);
        SipNonceCodec codec = new SipNonceCodec(
            new NonceRecordingRandomSource(), time);
        SipNonceOptions options = CreateOptions("example.com", 15);
        string valid = CreateAuthenticatedNonce(
            1, 1000UL, options.CurrentKey, "example.com");
        string unknownVersion = CreateAuthenticatedNonce(
            2, 1000UL, options.CurrentKey, "example.com");
        string overflowingTime = CreateAuthenticatedNonce(
            1, 9223372036854775808UL, options.CurrentKey, "example.com");

        Assert.Multiple(() =>
        {
            Assert.That(codec.ValidateNonce(valid, options),
                Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(codec.ValidateNonce(unknownVersion, options),
                Is.EqualTo(SipAuthenticationStatus.Failed));
            Assert.That(codec.ValidateNonce(overflowingTime, options),
                Is.EqualTo(SipAuthenticationStatus.Failed));
        });
    }

    [Test]
    public void RejectsNegativeTimeFromClock()
    {
        NonceManualTimeSource time = new NonceManualTimeSource(1000);
        SipNonceCodec codec = new SipNonceCodec(
            new NonceRecordingRandomSource(), time);
        SipNonceOptions options = CreateOptions("example.com", 16);
        string nonce = codec.CreateNonce(options);

        time.UnixTimeSeconds = -1;

        Assert.Multiple(() =>
        {
            Assert.That(codec.ValidateNonce(nonce, options),
                Is.EqualTo(SipAuthenticationStatus.Failed));
            Assert.Throws<System.InvalidOperationException>(
                () => codec.CreateNonce(options));
        });
    }

    [Test]
    public void RealmMismatchHasSameResultAsMalformedOrTamperedNonce()
    {
        SipNonceCodec codec = new SipNonceCodec(
            new NonceRecordingRandomSource(), new NonceManualTimeSource(1000));
        SipNonceOptions original = CreateOptions("one.example", 5);
        string nonce = codec.CreateNonce(original);
        SipNonceOptions otherRealm = CreateOptions("two.example", 5);

        SipAuthenticationStatus wrongRealm = codec.ValidateNonce(nonce, otherRealm);
        SipAuthenticationStatus malformed = codec.ValidateNonce("not-a-nonce", original);
        SipAuthenticationStatus tampered = codec.ValidateNonce(
            ChangeCharacter(nonce, 75), original);

        Assert.Multiple(() =>
        {
            Assert.That(wrongRealm, Is.EqualTo(SipAuthenticationStatus.Failed));
            Assert.That(malformed, Is.EqualTo(SipAuthenticationStatus.Failed));
            Assert.That(tampered, Is.EqualTo(SipAuthenticationStatus.Failed));
        });
    }

    [Test]
    public void AcceptsPreviousRotationKeyButCreatesWithCurrentKey()
    {
        NonceManualTimeSource time = new NonceManualTimeSource(1000);
        SipNonceCodec codec = new SipNonceCodec(
            new NonceRecordingRandomSource(), time);
        SipNonceOptions oldOptions = CreateOptions("example.com", 6);
        string oldNonce = codec.CreateNonce(oldOptions);

        SipNonceOptions rotated = CreateOptions("example.com", 7);
        rotated.PreviousKey = CreateKey(6);
        string newNonce = codec.CreateNonce(rotated);
        SipNonceOptions currentOnly = CreateOptions("example.com", 7);

        Assert.Multiple(() =>
        {
            Assert.That(codec.ValidateNonce(oldNonce, rotated),
                Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(codec.ValidateNonce(oldNonce, currentOnly),
                Is.EqualTo(SipAuthenticationStatus.Failed));
            Assert.That(codec.ValidateNonce(newNonce, currentOnly),
                Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(codec.ValidateNonce(newNonce, oldOptions),
                Is.EqualTo(SipAuthenticationStatus.Failed));
        });
    }

    [Test]
    public void RejectsInvalidNonceOptions()
    {
        SipNonceCodec codec = new SipNonceCodec(
            new NonceRecordingRandomSource(), new NonceManualTimeSource(1000));
        SipNonceOptions missingRealm = CreateOptions("example.com", 8);
        missingRealm.Realm = null;
        SipNonceOptions shortCurrentKey = CreateOptions("example.com", 8);
        shortCurrentKey.CurrentKey = new byte[31];
        SipNonceOptions shortPreviousKey = CreateOptions("example.com", 8);
        shortPreviousKey.PreviousKey = new byte[31];
        SipNonceOptions zeroLifetime = CreateOptions("example.com", 8);
        zeroLifetime.LifetimeSeconds = 0;
        SipNonceOptions negativeSkew = CreateOptions("example.com", 8);
        negativeSkew.ClockSkewSeconds = -1;

        Assert.Multiple(() =>
        {
            Assert.Throws<System.ArgumentNullException>(() => codec.CreateNonce(null));
            Assert.Throws<System.ArgumentException>(() => codec.CreateNonce(missingRealm));
            Assert.Throws<System.ArgumentException>(() => codec.CreateNonce(shortCurrentKey));
            Assert.Throws<System.ArgumentException>(() => codec.CreateNonce(shortPreviousKey));
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => codec.CreateNonce(zeroLifetime));
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => codec.CreateNonce(negativeSkew));
        });
    }

    [Test]
    public void CryptographicGenerationDoesNotRepeatValues()
    {
        SipNonceCodec codec = new SipNonceCodec(
            new SipCryptographicRandomSource(), new NonceManualTimeSource(1000));
        SipNonceOptions options = CreateOptions("example.com", 9);
        System.Collections.Generic.HashSet<string> nonces =
            new System.Collections.Generic.HashSet<string>();
        System.Collections.Generic.HashSet<string> opaqueValues =
            new System.Collections.Generic.HashSet<string>();

        int index = 0;
        while (index < 128)
        {
            nonces.Add(codec.CreateNonce(options));
            opaqueValues.Add(codec.CreateOpaqueData());
            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(nonces, Has.Count.EqualTo(128));
            Assert.That(opaqueValues, Has.Count.EqualTo(128));
        });
    }

    static SipNonceOptions CreateOptions(string realm, byte keySeed)
    {
        SipNonceOptions options = new SipNonceOptions();
        options.Realm = realm;
        options.CurrentKey = CreateKey(keySeed);
        return options;
    }

    static byte[] CreateKey(byte seed)
    {
        byte[] key = new byte[32];
        int index = 0;
        while (index < key.Length)
        {
            key[index] = (byte)(seed + index);
            index = index + 1;
        }
        return key;
    }

    static string ChangeCharacter(string value, int index)
    {
        char[] characters = value.ToCharArray();
        characters[index] = characters[index] == 'A' ? 'B' : 'A';
        return new string(characters);
    }

    static string CreateAuthenticatedNonce(byte version, ulong issuedAt,
        byte[] key, string realm)
    {
        byte[] payload = new byte[25];
        payload[0] = version;
        WriteUInt64(payload, 1, issuedAt);
        int randomIndex = 9;
        while (randomIndex < payload.Length)
        {
            payload[randomIndex] = (byte)randomIndex;
            randomIndex = randomIndex + 1;
        }
        byte[] realmBytes = System.Text.Encoding.UTF8.GetBytes(realm);
        byte[] authenticated = new byte[payload.Length + realmBytes.Length];
        System.Array.Copy(payload, authenticated, payload.Length);
        System.Array.Copy(realmBytes, 0, authenticated, payload.Length,
            realmBytes.Length);
        byte[] authentication =
            System.Security.Cryptography.HMACSHA256.HashData(key, authenticated);
        byte[] nonce = new byte[payload.Length + authentication.Length];
        System.Array.Copy(payload, nonce, payload.Length);
        System.Array.Copy(authentication, 0, nonce, payload.Length,
            authentication.Length);
        return EncodeBase64Url(nonce);
    }

    static byte[] DecodeBase64Url(string value)
    {
        string base64 = value.Replace('-', '+').Replace('_', '/');
        return System.Convert.FromBase64String(base64);
    }

    static string EncodeBase64Url(byte[] value)
    {
        return System.Convert.ToBase64String(value)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    static void WriteUInt64(byte[] destination, int offset, ulong value)
    {
        int index = 7;
        while (index >= 0)
        {
            destination[offset + index] = (byte)(value & 255UL);
            value = value >> 8;
            index = index - 1;
        }
    }
}

class NonceManualTimeSource : ISipUnixTimeSource
{
    public long UnixTimeSeconds;

    public NonceManualTimeSource(long unixTimeSeconds)
    {
        UnixTimeSeconds = unixTimeSeconds;
    }

    public long GetUnixTimeSeconds()
    {
        return UnixTimeSeconds;
    }
}

class NonceRecordingRandomSource : ISipRandomSource
{
    public System.Collections.Generic.List<int> RequestedByteCounts =
        new System.Collections.Generic.List<int>();
    int nextValue;

    public void FillBytes(byte[] bytes)
    {
        RequestedByteCounts.Add(bytes.Length);
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)nextValue;
            nextValue = (nextValue + 1) % 256;
            index = index + 1;
        }
    }
}
