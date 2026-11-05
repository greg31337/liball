using token;

namespace token.Tests;

public class CoreTypesTests
{
    [Test]
    public void TokenErrorDefaultsToNoPosition()
    {
        TokenError error = TokenError.CreateEmpty();

        Assert.Multiple(() =>
        {
            Assert.That(error.Code, Is.Empty);
            Assert.That(error.Message, Is.Empty);
            Assert.That(error.Position, Is.EqualTo(-1));
        });
    }

    [Test]
    public void TokenErrorStoresFailureDetails()
    {
        TokenError error = new TokenError("invalid_token", "The token is invalid.", 12);

        Assert.Multiple(() =>
        {
            Assert.That(error.Code, Is.EqualTo("invalid_token"));
            Assert.That(error.Message, Is.EqualTo("The token is invalid."));
            Assert.That(error.Position, Is.EqualTo(12));
        });
    }

    [Test]
    public void ResultContainersDefaultToUnsuccessfulAndEmpty()
    {
        TokenResult result = new TokenResult();
        TokenBytesResult bytesResult = new TokenBytesResult();
        TokenTextResult textResult = new TokenTextResult();
        TokenKindResult kindResult = new TokenKindResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Null);
            Assert.That(bytesResult.Success, Is.False);
            Assert.That(bytesResult.Error, Is.Null);
            Assert.That(bytesResult.Bytes, Is.Null);
            Assert.That(textResult.Success, Is.False);
            Assert.That(textResult.Error, Is.Null);
            Assert.That(textResult.Text, Is.Null);
            Assert.That(kindResult.Success, Is.False);
            Assert.That(kindResult.Error, Is.Null);
            Assert.That(kindResult.Kind, Is.EqualTo(TokenKind.Unknown));
        });
    }

    [Test]
    public void ResultContainersStoreOperationValues()
    {
        TokenError error = new TokenError("failure", "Failed.", -1);
        TokenBytesResult bytesResult = new TokenBytesResult();
        TokenTextResult textResult = new TokenTextResult();
        TokenKindResult kindResult = new TokenKindResult();

        bytesResult.Error = error;
        bytesResult.Bytes = new byte[] { 1, 2, 3 };
        textResult.Success = true;
        textResult.Text = "value";
        kindResult.Success = true;
        kindResult.Kind = TokenKind.JweCompact;

        Assert.Multiple(() =>
        {
            Assert.That(bytesResult.Error, Is.SameAs(error));
            Assert.That(bytesResult.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(textResult.Success, Is.True);
            Assert.That(textResult.Text, Is.EqualTo("value"));
            Assert.That(kindResult.Success, Is.True);
            Assert.That(kindResult.Kind, Is.EqualTo(TokenKind.JweCompact));
        });
    }

    [Test]
    public void TokenPolicyUsesSafeDefaults()
    {
        TokenPolicy policy = new TokenPolicy();

        Assert.Multiple(() =>
        {
            Assert.That(policy.MaximumTokenSizeInBytes, Is.EqualTo(1048576));
            Assert.That(policy.MaximumJsonDepth, Is.EqualTo(64));
            Assert.That(policy.MaximumCborDepth, Is.EqualTo(64));
            Assert.That(policy.ClockSkewSeconds, Is.EqualTo(300));
            Assert.That(policy.AllowUnsecuredTokens, Is.False);
            Assert.That(policy.AllowLegacyRsaPkcs1Encryption, Is.False);
            Assert.That(policy.AllowUnauthenticatedDecode, Is.False);
            Assert.That(policy.AllowCompression, Is.False);
            Assert.That(policy.MaximumDecompressedSizeInBytes,
                Is.EqualTo(1048576));
            Assert.That(policy.MaximumCompressionRatio, Is.EqualTo(100));
            Assert.That(policy.AllowRemoteKeys, Is.False);
            Assert.That(policy.AllowedRemoteKeyHosts, Is.Empty);
            Assert.That(policy.MaximumRemoteKeySetSizeInBytes, Is.EqualTo(1048576));
            Assert.That(policy.MaximumRemoteKeyCacheEntries, Is.EqualTo(16));
            Assert.That(policy.RemoteKeyRefreshSeconds, Is.EqualTo(300));
            Assert.That(policy.RemoteKeyStaleIfErrorSeconds, Is.EqualTo(300));
            Assert.That(policy.RemoteKeyTimeoutSeconds, Is.EqualTo(15));
            Assert.That(policy.AllowedAlgorithms, Is.Empty);
            Assert.That(policy.UnderstoodCriticalHeaders, Is.Empty);
            Assert.That(policy.RequiredClaims, Is.Empty);
        });
    }
}
