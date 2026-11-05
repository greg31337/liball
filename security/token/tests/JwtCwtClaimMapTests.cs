using token;

namespace token.Tests;

public class JwtCwtClaimMapTests
{
    [TestCase(JwtClaimNames.Issuer, CwtClaimLabels.Issuer)]
    [TestCase(JwtClaimNames.Subject, CwtClaimLabels.Subject)]
    [TestCase(JwtClaimNames.Audience, CwtClaimLabels.Audience)]
    [TestCase(JwtClaimNames.ExpirationTime, CwtClaimLabels.ExpirationTime)]
    [TestCase(JwtClaimNames.NotBefore, CwtClaimLabels.NotBefore)]
    [TestCase(JwtClaimNames.IssuedAt, CwtClaimLabels.IssuedAt)]
    [TestCase(JwtClaimNames.Confirmation, CwtClaimLabels.Confirmation)]
    [TestCase(JwtClaimNames.Scope, CwtClaimLabels.Scope)]
    [TestCase("ace_profile", CwtClaimLabels.AceProfile)]
    [TestCase("ueid", CwtClaimLabels.UniversalEntityIdentifier)]
    [TestCase("geohash", CwtClaimLabels.Geohash)]
    [TestCase("cmw", CwtClaimLabels.CompositeMessageWrapper)]
    [TestCase("wmkeyver", CwtClaimLabels.WatermarkKeyVersion)]
    [TestCase("status_list", CwtClaimLabels.StatusList)]
    [TestCase("ttl", CwtClaimLabels.TimeToLive)]
    [TestCase("status", CwtClaimLabels.Status)]
    public void MapsDeclaredClaimEquivalentsInBothDirections(string jwtName, int cwtLabel)
    {
        ClaimMappingResult fromJwt = JwtCwtClaimMap.FromJwt(jwtName);
        ClaimMappingResult fromCwt = JwtCwtClaimMap.FromCwt(cwtLabel);

        Assert.Multiple(() =>
        {
            Assert.That(fromJwt.Mapped, Is.True);
            Assert.That(fromJwt.JwtName, Is.EqualTo(jwtName));
            Assert.That(fromJwt.CwtLabel, Is.EqualTo(cwtLabel));
            Assert.That(fromCwt.Mapped, Is.True);
            Assert.That(fromCwt.JwtName, Is.EqualTo(jwtName));
            Assert.That(fromCwt.CwtLabel, Is.EqualTo(cwtLabel));
        });
    }

    [Test]
    public void MapsClaimsWhoseJwtAndCwtNamesDiffer()
    {
        ClaimMappingResult identifier = JwtCwtClaimMap.FromJwt(JwtClaimNames.JwtId);
        ClaimMappingResult nonce = JwtCwtClaimMap.FromCwt(CwtClaimLabels.Nonce);

        Assert.Multiple(() =>
        {
            Assert.That(identifier.Mapped, Is.True);
            Assert.That(identifier.CwtLabel, Is.EqualTo(CwtClaimLabels.CwtId));
            Assert.That(CwtClaimLabelsOperations.GetName(identifier.CwtLabel), Is.EqualTo("cti"));
            Assert.That(nonce.Mapped, Is.True);
            Assert.That(nonce.JwtName, Is.EqualTo("eat_nonce"));
            Assert.That(CwtClaimLabelsOperations.GetName(nonce.CwtLabel), Is.EqualTo("Nonce"));
        });
    }

    [TestCase("hcert", CwtClaimLabels.HealthCertificate)]
    [TestCase("EUPHNonce", CwtClaimLabels.EatUnprotectedHeaderNonce)]
    [TestCase("EATMAROEPrefix", CwtClaimLabels.EatMeasuredArtifactPrefix)]
    [TestCase("EAT-FDO", CwtClaimLabels.EatFidoDeviceOnboard)]
    [TestCase("vct", CwtClaimLabels.VerifiableCredentialType)]
    [TestCase("identity-data", CwtClaimLabels.IdentityData)]
    [TestCase("wmver", CwtClaimLabels.WatermarkVersion)]
    public void MapsCrossRegistryNamesDeclaredByCwt(string jwtName, int cwtLabel)
    {
        ClaimMappingResult result = JwtCwtClaimMap.FromJwt(jwtName);

        Assert.Multiple(() =>
        {
            Assert.That(result.Mapped, Is.True);
            Assert.That(result.JwtName, Is.EqualTo(jwtName));
            Assert.That(result.CwtLabel, Is.EqualTo(cwtLabel));
            Assert.That(CwtClaimLabelsOperations.IsRegistered(cwtLabel), Is.True);
        });
    }

    [TestCase(JwtClaimNames.Nonce)]
    [TestCase(JwtClaimNames.AuthorizedParty)]
    [TestCase(JwtClaimNames.AuthenticationTime)]
    [TestCase(JwtClaimNames.ClientId)]
    [TestCase("roles")]
    public void JwtClaimsWithoutDeclaredCwtEquivalentAreNotMapped(string jwtName)
    {
        ClaimMappingResult result = JwtCwtClaimMap.FromJwt(jwtName);

        Assert.Multiple(() =>
        {
            Assert.That(JwtClaimNamesOperations.IsRegistered(jwtName), Is.True);
            Assert.That(result.Mapped, Is.False);
            Assert.That(result.JwtName, Is.EqualTo(jwtName));
            Assert.That(result.CwtLabel, Is.Zero);
        });
    }

    [TestCase(CwtClaimLabels.GlobalPlatformComponent)]
    [TestCase(308)]
    [TestCase(323)]
    [TestCase(2394)]
    [TestCase(2400)]
    public void CwtClaimsMarkedWithoutJwtEquivalentAreNotMapped(int cwtLabel)
    {
        ClaimMappingResult result = JwtCwtClaimMap.FromCwt(cwtLabel);

        Assert.Multiple(() =>
        {
            Assert.That(CwtClaimLabelsOperations.IsRegistered(cwtLabel), Is.True);
            Assert.That(result.Mapped, Is.False);
            Assert.That(result.JwtName, Is.Null);
            Assert.That(result.CwtLabel, Is.EqualTo(cwtLabel));
        });
    }

    [Test]
    public void MappingIsCaseSensitiveAndHandlesUnknownIdentifiers()
    {
        ClaimMappingResult upperIssuer = JwtCwtClaimMap.FromJwt("ISS");
        ClaimMappingResult lowerEatNonce = JwtCwtClaimMap.FromJwt("euphnonce");
        ClaimMappingResult nullName = JwtCwtClaimMap.FromJwt(null);
        ClaimMappingResult unknownLabel = JwtCwtClaimMap.FromCwt(999999);

        Assert.Multiple(() =>
        {
            Assert.That(upperIssuer.Mapped, Is.False);
            Assert.That(lowerEatNonce.Mapped, Is.False);
            Assert.That(nullName.Mapped, Is.False);
            Assert.That(nullName.JwtName, Is.Null);
            Assert.That(unknownLabel.Mapped, Is.False);
            Assert.That(unknownLabel.JwtName, Is.Null);
            Assert.That(unknownLabel.CwtLabel, Is.EqualTo(999999));
        });
    }

    [Test]
    public void MappingSnapshotIsCompleteUniqueAndRoundTrips()
    {
        string[] jwtNames = JwtCwtClaimMap.GetJwtClaims();
        int[] cwtLabels = JwtCwtClaimMap.GetCwtClaims();

        Assert.Multiple(() =>
        {
            Assert.That(jwtNames, Has.Length.EqualTo(52).And.Unique);
            Assert.That(cwtLabels, Has.Length.EqualTo(52).And.Unique);
            Assert.That(cwtLabels, Has.Length.EqualTo(jwtNames.Length));
        });

        int index = 0;
        while (index < jwtNames.Length)
        {
            ClaimMappingResult forward = JwtCwtClaimMap.FromJwt(jwtNames[index]);
            ClaimMappingResult reverse = JwtCwtClaimMap.FromCwt(cwtLabels[index]);
            Assert.Multiple(() =>
            {
                Assert.That(CwtClaimLabelsOperations.IsRegistered(cwtLabels[index]), Is.True);
                Assert.That(forward.Mapped, Is.True);
                Assert.That(forward.CwtLabel, Is.EqualTo(cwtLabels[index]));
                Assert.That(reverse.Mapped, Is.True);
                Assert.That(reverse.JwtName, Is.EqualTo(jwtNames[index]));
            });
            index += 1;
        }
    }

    [Test]
    public void MappingSnapshotsAndResultsAreDefensive()
    {
        string[] jwtNames = JwtCwtClaimMap.GetJwtClaims();
        int[] cwtLabels = JwtCwtClaimMap.GetCwtClaims();
        ClaimMappingResult result = JwtCwtClaimMap.FromJwt(JwtClaimNames.Issuer);

        jwtNames[0] = "changed";
        cwtLabels[0] = 999999;
        result.Mapped = false;
        result.JwtName = "changed";
        result.CwtLabel = 999999;

        ClaimMappingResult fresh = JwtCwtClaimMap.FromJwt(JwtClaimNames.Issuer);
        Assert.Multiple(() =>
        {
            Assert.That(JwtCwtClaimMap.FromJwt("changed").Mapped, Is.False);
            Assert.That(JwtCwtClaimMap.FromCwt(999999).Mapped, Is.False);
            Assert.That(fresh.Mapped, Is.True);
            Assert.That(fresh.JwtName, Is.EqualTo(JwtClaimNames.Issuer));
            Assert.That(fresh.CwtLabel, Is.EqualTo(CwtClaimLabels.Issuer));
        });
    }
}
