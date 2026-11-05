namespace sdp.Tests.Api;

public class SdpValidatorTests
{
    [Test]
    public void Validator_ExposesResultBasedValidationAndSnapshotsOptions()
    {
        ISdpValidator validator = new SdpValidator();
        SdpSessionDescription description = new SdpSessionDescription();
        SdpSessionDescription offer = new SdpSessionDescription();
        SdpValidationOptions options = new SdpValidationOptions
        {
            Profile = SdpValidationProfile.Ice,
            NegotiationRole = SdpNegotiationRole.Offerer,
            OfferDescription = offer,
            MinimumSeverity = SdpValidationSeverity.Warning,
            MaximumDiagnosticCount = 17
        };
        options.SuppressedDiagnosticCodes.Add(SdpDiagnosticCodes.ValidationAttributeScope);
        options.PromotedDiagnosticCodes.Add(SdpDiagnosticCodes.ValidationIce);

        SdpValidationResult result = validator.ValidateDescription(description, options);
        options.Profile = SdpValidationProfile.Rfc8866;
        options.NegotiationRole = SdpNegotiationRole.Unknown;
        options.OfferDescription = null;
        options.SuppressedDiagnosticCodes.Clear();
        options.PromotedDiagnosticCodes.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedOptions, Is.Not.SameAs(options));
            Assert.That(result.AppliedOptions.Profile, Is.EqualTo(SdpValidationProfile.Ice));
            Assert.That(result.AppliedOptions.NegotiationRole, Is.EqualTo(SdpNegotiationRole.Offerer));
            Assert.That(result.AppliedOptions.OfferDescription, Is.SameAs(offer));
            Assert.That(result.AppliedOptions.MinimumSeverity, Is.EqualTo(SdpValidationSeverity.Warning));
            Assert.That(result.AppliedOptions.MaximumDiagnosticCount, Is.EqualTo(17));
            Assert.That(result.AppliedOptions.SuppressedDiagnosticCodes, Is.EqualTo(new[] { SdpDiagnosticCodes.ValidationAttributeScope }));
            Assert.That(result.AppliedOptions.PromotedDiagnosticCodes, Is.EqualTo(new[] { SdpDiagnosticCodes.ValidationIce }));
        });
    }

    [Test]
    public void Validator_RejectsNullArgumentsAndInvalidOptions()
    {
        ISdpValidator validator = new SdpValidator();
        SdpSessionDescription description = new SdpSessionDescription();
        SdpValidationOptions options = new SdpValidationOptions { Profile = SdpValidationProfile.Unknown };

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => validator.ValidateDescription(null!, new SdpValidationOptions()));
            Assert.Throws<ArgumentNullException>(() => validator.ValidateDescription(description, null!));
            Assert.Throws<ArgumentOutOfRangeException>(() => validator.ValidateDescription(description, options));
            Assert.Throws<ArgumentOutOfRangeException>(() => validator.ValidateDescription(description, new SdpValidationOptions { NegotiationRole = (SdpNegotiationRole)99 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => validator.ValidateDescription(description, new SdpValidationOptions { MinimumSeverity = SdpValidationSeverity.Unknown }));
            Assert.Throws<ArgumentOutOfRangeException>(() => validator.ValidateDescription(description, new SdpValidationOptions { MaximumDiagnosticCount = -1 }));
        });
    }
}
