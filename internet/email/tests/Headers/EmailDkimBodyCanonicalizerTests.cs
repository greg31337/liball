using System.Security.Cryptography;
using System.Text;

namespace email.Tests;

public class EmailDkimBodyCanonicalizerTests
{
    [Test]
    public void CanonicalizesEmptyBodiesToRfcHashes()
    {
        EmailDkimBodyCanonicalizationOutput simple = new EmailDkimCanonicalizer().CanonicalizeBodyWithInputAndKind(new byte[0], EmailDkimCanonicalizationKind.Simple);
        EmailDkimBodyCanonicalizationOutput relaxed = new EmailDkimCanonicalizer().CanonicalizeBodyWithInputAndKind(new byte[0], EmailDkimCanonicalizationKind.Relaxed);

        Assert.Multiple(() =>
        {
            Assert.That(Ascii(simple.GetCanonicalizedBody()), Is.EqualTo("\r\n"));
            Assert.That(Convert.ToBase64String(simple.GetResult().GetHash().GetDigestBytes()), Is.EqualTo("frcCV1k9oG9oKj3dpUqdJg1PxRT2RSN/XKdLCPjaYaY="));
            Assert.That(simple.GetResult().GetCanonicalizedByteCount(), Is.EqualTo(2));
            Assert.That(simple.GetResult().GetHashedByteCount(), Is.EqualTo(2));
            Assert.That(relaxed.GetCanonicalizedBody(), Is.Empty);
            Assert.That(Convert.ToBase64String(relaxed.GetResult().GetHash().GetDigestBytes()), Is.EqualTo("47DEQpj8HBSa+/TImW+5JCeuQeRkm5NMpJWZG3hSuFU="));
            Assert.That(relaxed.GetResult().GetCanonicalizedByteCount(), Is.Zero);
            Assert.That(relaxed.GetResult().GetHashedByteCount(), Is.Zero);
        });
    }

    [Test]
    public void AppliesRfcSimpleAndRelaxedBodyRules()
    {
        byte[] source = Bytes(" C \r\nD \t E\r\n\r\n\r\n");

        EmailDkimBodyCanonicalizationOutput simple = new EmailDkimCanonicalizer().CanonicalizeBodyWithInputAndKind(source, EmailDkimCanonicalizationKind.Simple);
        EmailDkimBodyCanonicalizationOutput relaxed = new EmailDkimCanonicalizer().CanonicalizeBodyWithInputAndKind(source, EmailDkimCanonicalizationKind.Relaxed);

        Assert.That(Ascii(simple.GetCanonicalizedBody()), Is.EqualTo(" C \r\nD \t E\r\n"));
        Assert.That(Ascii(relaxed.GetCanonicalizedBody()), Is.EqualTo(" C\r\nD E\r\n"));
        Assert.That(simple.GetResult().GetSourceByteCount(), Is.EqualTo(source.Length));
        Assert.That(relaxed.GetResult().GetSourceByteCount(), Is.EqualTo(source.Length));
        byte[] returned = relaxed.GetCanonicalizedBody();
        returned[0] ^= 1;
        Assert.That(Ascii(relaxed.GetCanonicalizedBody()), Is.EqualTo(" C\r\nD E\r\n"));
    }

    [Test]
    public void ProducesIdenticalOutputAcrossEveryChunkBoundary()
    {
        byte[] source = Bytes("\r\nA\r\n\r\nB \t\r\n\r\n");
        AssertEverySplit(source, EmailDkimCanonicalizationKind.Simple, Bytes("\r\nA\r\n\r\nB \t\r\n"));
        AssertEverySplit(source, EmailDkimCanonicalizationKind.Relaxed, Bytes("\r\nA\r\n\r\nB\r\n"));

        EmailDkimBodyCanonicalizationOptions options = Options(EmailDkimCanonicalizationKind.Relaxed);
        EmailDkimBodyCanonicalizer oneByte = new EmailDkimBodyCanonicalizer(options);
        List<byte> output = new List<byte>();
        foreach (byte value in source) output.AddRange(oneByte.Feed(new byte[] { value }));
        output.AddRange(oneByte.Complete());

        Assert.That(output.ToArray(), Is.EqualTo(Bytes("\r\nA\r\n\r\nB\r\n")));
        Assert.That(oneByte.GetResult().GetHash().GetDigestBytes(), Is.EqualTo(SHA256.HashData(output.ToArray())));
    }

    [Test]
    public void HandlesTrailingLinesWhitespaceAndUnterminatedBodies()
    {
        Assert.That(Canonical("", EmailDkimCanonicalizationKind.Simple), Is.EqualTo("\r\n"));
        Assert.That(Canonical("\r\n\r\n", EmailDkimCanonicalizationKind.Simple), Is.EqualTo("\r\n"));
        Assert.That(Canonical("abc", EmailDkimCanonicalizationKind.Simple), Is.EqualTo("abc\r\n"));
        Assert.That(Canonical("abc\r\n\r\n", EmailDkimCanonicalizationKind.Simple), Is.EqualTo("abc\r\n"));
        Assert.That(Canonical(" \t", EmailDkimCanonicalizationKind.Simple), Is.EqualTo(" \t\r\n"));
        Assert.That(Canonical(" \t\r\n\r\n", EmailDkimCanonicalizationKind.Relaxed), Is.Empty);
        Assert.That(Canonical(" A \t", EmailDkimCanonicalizationKind.Relaxed), Is.EqualTo(" A\r\n"));
        Assert.That(Canonical("abc\n", EmailDkimCanonicalizationKind.Simple), Is.EqualTo("abc\n\r\n"));
        Assert.That(Canonical(" A \t\n B \t", EmailDkimCanonicalizationKind.Relaxed), Is.EqualTo(" A \n B\r\n"));
    }

    [Test]
    public void HashesOnlyTheCanonicalizedBodyLengthPrefix()
    {
        byte[] source = Bytes(" A  B \r\n");
        EmailDkimBodyCanonicalizationOptions limitedOptions = Options(EmailDkimCanonicalizationKind.Relaxed);
        limitedOptions.SetBodyLengthLimit(3);
        EmailDkimBodyCanonicalizationOutput limited = new EmailDkimCanonicalizer().CanonicalizeBodyWithInputAndOptions(source, limitedOptions);
        EmailDkimBodyCanonicalizationOptions zeroOptions = Options(EmailDkimCanonicalizationKind.Relaxed);
        zeroOptions.SetBodyLengthLimit(0);
        zeroOptions.SetMaximumHashedBytes(0);
        EmailDkimBodyCanonicalizationOutput zero = new EmailDkimCanonicalizer().CanonicalizeBodyWithInputAndOptions(source, zeroOptions);
        EmailDkimBodyCanonicalizationOptions oversizedOptions = Options(EmailDkimCanonicalizationKind.Relaxed);
        oversizedOptions.SetBodyLengthLimit(100);
        EmailDkimBodyCanonicalizationOutput oversized = new EmailDkimCanonicalizer().CanonicalizeBodyWithInputAndOptions(source, oversizedOptions);

        Assert.That(Ascii(limited.GetCanonicalizedBody()), Is.EqualTo(" A B\r\n"));
        Assert.Multiple(() =>
        {
            Assert.That(limited.GetResult().GetHash().GetDigestBytes(), Is.EqualTo(SHA256.HashData(Bytes(" A "))));
            Assert.That(limited.GetResult().GetHashedByteCount(), Is.EqualTo(3));
            Assert.That(limited.GetResult().WasBodyLimitedForHash(), Is.True);
            Assert.That(limited.GetResult().IsBodyLengthLimitSatisfied(), Is.True);
            Assert.That(limited.GetResult().GetUnsignedCanonicalizedByteCount(), Is.EqualTo(3));
            Assert.That(zero.GetResult().GetHash().GetDigestBytes(), Is.EqualTo(SHA256.HashData(new byte[0])));
            Assert.That(zero.GetResult().GetHashedByteCount(), Is.Zero);
            Assert.That(zero.GetResult().GetUnsignedCanonicalizedByteCount(), Is.EqualTo(6));
            Assert.That(oversized.GetResult().GetHashedByteCount(), Is.EqualTo(6));
            Assert.That(oversized.GetResult().IsBodyLengthLimitSatisfied(), Is.False);
            Assert.That(oversized.GetResult().WasBodyLimitedForHash(), Is.False);
        });
    }

    [Test]
    public void EnforcesIndependentInputCanonicalAndHashLimits()
    {
        EmailDkimBodyCanonicalizationOptions inputOptions = Options(EmailDkimCanonicalizationKind.Simple);
        inputOptions.SetMaximumInputBytes(2);
        EmailDkimBodyCanonicalizer input = new EmailDkimBodyCanonicalizer(inputOptions);
        EmailLimitException inputException = Assert.Throws<EmailLimitException>(() => input.Feed(Bytes("abc")));
        input.Close();
        EmailDkimBodyCanonicalizationOptions canonicalOptions = Options(EmailDkimCanonicalizationKind.Simple);
        canonicalOptions.SetMaximumCanonicalizedBytes(1);
        EmailDkimBodyCanonicalizer canonical = new EmailDkimBodyCanonicalizer(canonicalOptions);
        EmailLimitException canonicalException = Assert.Throws<EmailLimitException>(() => canonical.Complete());
        canonical.Close();
        EmailDkimBodyCanonicalizationOptions hashOptions = Options(EmailDkimCanonicalizationKind.Simple);
        hashOptions.SetMaximumHashedBytes(1);
        EmailDkimBodyCanonicalizer hash = new EmailDkimBodyCanonicalizer(hashOptions);
        EmailLimitException hashException = Assert.Throws<EmailLimitException>(() => hash.Complete());
        hash.Close();

        Assert.Multiple(() =>
        {
            Assert.That(inputException.GetLimitName(), Is.EqualTo("MaxDkimBodyInputBytes"));
            Assert.That(canonicalException.GetLimitName(), Is.EqualTo("MaxDkimCanonicalizedBodyBytes"));
            Assert.That(hashException.GetLimitName(), Is.EqualTo("MaxDkimHashedBodyBytes"));
        });
    }

    [Test]
    public void ClonesOptionsAndEnforcesLifecycle()
    {
        EmailDkimBodyCanonicalizationOptions options = Options(EmailDkimCanonicalizationKind.Relaxed);
        options.SetHashAlgorithm(EmailHashAlgorithm.Sha512);
        options.SetMaximumInputBytes(50);
        options.SetMaximumCanonicalizedBytes(51);
        options.SetMaximumHashedBytes(40);
        options.SetBodyLengthLimit(20);
        EmailDkimBodyCanonicalizationOptions clone = options.Clone();
        options.ClearBodyLengthLimit();
        EmailDkimBodyCanonicalizer canonicalizer = new EmailDkimBodyCanonicalizer(clone);
        canonicalizer.Feed(Bytes("body"));
        canonicalizer.Complete();

        Assert.Multiple(() =>
        {
            Assert.That(clone.GetKind(), Is.EqualTo(EmailDkimCanonicalizationKind.Relaxed));
            Assert.That(clone.GetHashAlgorithm(), Is.EqualTo(EmailHashAlgorithm.Sha512));
            Assert.That(clone.GetMaximumInputBytes(), Is.EqualTo(50));
            Assert.That(clone.GetMaximumCanonicalizedBytes(), Is.EqualTo(51));
            Assert.That(clone.GetMaximumHashedBytes(), Is.EqualTo(40));
            Assert.That(clone.HasBodyLengthLimit(), Is.True);
            Assert.That(clone.GetBodyLengthLimit(), Is.EqualTo(20));
            Assert.That(canonicalizer.GetResult().GetHash().GetAlgorithm(), Is.EqualTo(EmailHashAlgorithm.Sha512));
            Assert.Throws<InvalidOperationException>(() => canonicalizer.Feed(Bytes("again")));
            Assert.Throws<InvalidOperationException>(() => canonicalizer.Complete());
            Assert.Throws<ArgumentOutOfRangeException>(() => clone.SetKind(EmailDkimCanonicalizationKind.Extension));
            Assert.Throws<ArgumentOutOfRangeException>(() => clone.SetBodyLengthLimit(-1));
        });
    }

    static void AssertEverySplit(byte[] source, EmailDkimCanonicalizationKind kind, byte[] expected)
    {
        EmailDkimBodyCanonicalizationOutput whole = new EmailDkimCanonicalizer().CanonicalizeBodyWithInputAndKind(source, kind);
        Assert.That(whole.GetCanonicalizedBody(), Is.EqualTo(expected));
        int split = 0;
        while (split <= source.Length)
        {
            EmailDkimBodyCanonicalizer streaming = new EmailDkimBodyCanonicalizer(Options(kind));
            List<byte> output = new List<byte>();
            output.AddRange(streaming.Feed(new ReadOnlySpan<byte>(source, 0, split)));
            output.AddRange(streaming.Feed(new ReadOnlySpan<byte>(source, split, source.Length - split)));
            output.AddRange(streaming.Complete());
            Assert.That(output.ToArray(), Is.EqualTo(expected), "split " + split);
            Assert.That(streaming.GetResult().GetHash().GetDigestBytes(), Is.EqualTo(whole.GetResult().GetHash().GetDigestBytes()), "hash split " + split);
            split++;
        }
    }

    static EmailDkimBodyCanonicalizationOptions Options(EmailDkimCanonicalizationKind kind)
    {
        EmailDkimBodyCanonicalizationOptions options = new EmailDkimBodyCanonicalizationOptions();
        options.SetKind(kind);
        return options;
    }

    static string Canonical(string source, EmailDkimCanonicalizationKind kind)
    {
        return Ascii(new EmailDkimCanonicalizer().CanonicalizeBodyWithInputAndKind(Bytes(source), kind).GetCanonicalizedBody());
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    static string Ascii(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }
}
